using System.IO;
using Newtonsoft.Json;
using System.Diagnostics;

using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;
using Kai.Common.NetDll_WpfCtrl.NetWnds;
using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgBox;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Services;

using Kai.Client.CallCenter.Classes;
using Kai.Client.CallCenter.Classes.Class_Master;
using Kai.Client.CallCenter.Networks.NwInsungs;
using static Kai.Client.CallCenter.Classes.CommonVars;

namespace Kai.Client.CallCenter.Networks;
#nullable disable
public class NwInsung01 : IExternalApp
{
    #region Static Configuration (appsettings.json에서 로드)
    public static bool s_Use { get; set; } = false;
    public static string s_Id { get; set; } = string.Empty;
    public static string s_Pw { get; set; } = string.Empty;
    public static string s_AppPath { get; set; } = string.Empty;
    #endregion

    #region Constants
    public const string APP_NAME = StdConst_Network.INSUNG1;
    public const string INFO_FILE_NAME = "Insung01_FileInfo.txt";
    #endregion

    #region Context
    /// <summary>
    /// 인성1의 모든 공용 데이터를 담는 Context
    /// </summary>
    private InsungContext m_Context = null;

    /// <summary>
    /// Context 읽기 전용 접근
    /// </summary>
    public InsungContext Context => m_Context;
    #endregion

    #region AutoAlloc Variables
    /// <summary>
    /// 자동배차 할일 없음 카운터 (60회마다 조회버튼 클릭)
    /// </summary>
    private long m_lRestCount = 0;
    #endregion

    #region Dispose
    private bool disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // 관리형 리소스 해제
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    #endregion

    #region IExternalApp 구현
    public bool IsUsed => s_Use;
    public string AppName => "Insung01";

    public async Task<StdResult_Status> InitializeAsync()
    {
        try
        {
            Debug.WriteLine($"[NwInsung01] InitializeAsync 시작: Id={s_Id}");

            // 1. FileInfo 파일에서 설정 로드
            StdResult_Error resultErr = await ReadInfoFileAsync();
            if (resultErr != null)
            {
                Debug.WriteLine($"[NwInsung01] FileInfo 로드 실패: {resultErr.sErrNPos}");
                return new StdResult_Status(StdResult.Fail, resultErr.sErrNPos, "NwInsung01/InitializeAsync_01");
            }

            // 2. 앱 경로 확인
            if (string.IsNullOrEmpty(s_AppPath))
            {
                Debug.WriteLine($"[NwInsung01] AppPath가 설정되지 않았습니다.");
                return new StdResult_Status(StdResult.Fail, "AppPath가 appsettings.json에 설정되지 않았습니다.", "NwInsung01/InitializeAsync_02");
            }
            //Debug.WriteLine($"[NwInsung01] 앱 경로: {s_AppPath}");

            // Show Loding
            if (s_Screens.m_WorkingMonitor != s_Screens.m_PrimaryMonitor) // 작업 모니터가 기본 모니터면 LoadingPanel을 사용하지 않는다.
                NetLoadingWnd.ShowLoading(s_MainWnd, "   인성1 초기화 작업중입니다, \n     입력작업을 하지 마세요...   ");

            // 3. UpdaterWork - Updater 실행 및 종료 대기
            StdResult_Error resultUpdater = await m_Context.AppAct.UpdaterWorkAsync(s_AppPath);
            if (resultUpdater != null)
            {
                Debug.WriteLine($"[NwInsung01] UpdaterWork 실패: {resultUpdater.sErrNPos}");
                return new StdResult_Status(StdResult.Fail, resultUpdater.sErrNPos, "NwInsung01/InitializeAsync_03");
            }
            //Debug.WriteLine($"[NwInsung01] Updater 종료 완료");

            // 4. SplashWork - 스플래시 창 처리 및 로그인
            StdResult_Error resultSplash = await m_Context.AppAct.SplashWorkAsync();
            if (resultSplash != null)
            {
                Debug.WriteLine($"[NwInsung01] SplashWork 실패: {resultSplash.sErrNPos}");
                return new StdResult_Status(StdResult.Fail, resultSplash.sErrNPos, "NwInsung01/InitializeAsync_04");
            }
            //Debug.WriteLine($"[NwInsung01] 스플래시 로그인 완료");

            // 5. MainWnd 초기화
            StdResult_Error resultMainWnd = await m_Context.MainWndAct.InitializeAsync();
            if (resultMainWnd != null)
            {
                Debug.WriteLine($"[NwInsung01] MainWnd 초기화 실패: {resultMainWnd.sErrNPos}");
                return new StdResult_Status(StdResult.Fail, resultMainWnd.sErrNPos, "NwInsung01/InitializeAsync_05");
            }
            //Debug.WriteLine($"[NwInsung01] MainWnd 초기화 완료");

            // 6. RcptRegPage 초기화
            StdResult_Error resultRcptRegPage = await m_Context.RcptRegPageAct.InitializeAsync();
            if (resultRcptRegPage != null)
            {
                Debug.WriteLine($"[NwInsung01] RcptRegPage 초기화 실패: {resultRcptRegPage.sErrNPos}");
                return new StdResult_Status(StdResult.Fail, resultRcptRegPage.sErrNPos, "NwInsung01/InitializeAsync_06");
            }
            Debug.WriteLine($"[NwInsung01] RcptRegPage 초기화 완료");

            Debug.WriteLine("[NwInsung01] InitializeAsync 완료");
            return new StdResult_Status(StdResult.Success);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NwInsung01] InitializeAsync 실패: {ex.Message}");
            return new StdResult_Status(StdResult.Fail, ex.Message, "NwInsung01/InitializeAsync_99");
        }
        finally
        {
            NetLoadingWnd.HideLoading();
        }
    }

    public void Shutdown()
    {
        try
        {
            Debug.WriteLine("[NwInsung01] Shutdown 시작");

            // AppAct.Close() 호출 - 인성 앱 종료
            if (m_Context?.AppAct != null)
            {
                StdResult_Error resultClose = m_Context.AppAct.Close();
                if (resultClose != null)
                {
                    Debug.WriteLine($"[NwInsung01] Close 실패: {resultClose.sErrNPos}");
                }
                else
                {
                    Debug.WriteLine($"[NwInsung01] Close 성공");
                }
            }

            Debug.WriteLine("[NwInsung01] Shutdown 완료");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NwInsung01] Shutdown 실패: {ex.Message}");
        }
    }

    public async Task<StdResult_Status> AutoAllocAsync(long lAllocCount, CancelTokenControl ctrl)
    {
        try
        {
            Debug.WriteLine($"\n-----------------[NwInsung01] AutoAllocAsync 시작 - Count={lAllocCount}--------------------------");

            #region 1. 사전작업
            // TopMost 설정 - 인성1 메인 창을 최상위로
            await Std32Window.SetWindowTopMostAndReleaseAsync(m_Context.MemInfo.Main.TopWnd_hWnd, c_nWaitShort);
            Debug.WriteLine($"[NwInsung01] TopMost 설정 완료");
            #endregion

            // Cancel/Pause 체크 - Region 2 진입 전
            await ctrl.WaitIfPausedOrCancelledAsync();

            #region 2. Local Variables 초기화
            // 컨트롤러 큐에서 주문 리스트 가져오기 (DequeueAllToList로 큐 비우기)
            List<AutoAlloc> listFromController = ExternalAppController.QueueManager.DequeueAllToList(StdConst_Network.INSUNG1);
            Debug.WriteLine($"[{APP_NAME}] 큐에서 가져온 주문 개수: {listFromController.Count}");

            // 작업잔량 파악 리스트 (원본 복사)
            var listInsung = new List<AutoAlloc>(listFromController);
            // 큐에서 이미 꺼냈으므로 Clear 불필요

            // 처리 완료된 항목을 담을 리스트 (Region 4, 5에서 사용)
            var listProcessed = new List<AutoAlloc>();

            var listCreated = listInsung
                .Where(item => item.StateFlag.HasFlag(PostgService_Common_OrderState.Created) ||
                               item.StateFlag.HasFlag(PostgService_Common_OrderState.Existed_NonSeqno))
                .OrderByDescending(item => item.NewOrder.Insung1) // Insung1 KeyCode 역순 정렬 (큰 값 우선)
                .Select(item => item.Clone())
                .ToList();

            var listEtcGroup = listInsung
                .Where(item => !(item.StateFlag.HasFlag(PostgService_Common_OrderState.Created) ||
                               item.StateFlag.HasFlag(PostgService_Common_OrderState.Existed_NonSeqno)))
                .OrderByDescending(item => item.NewOrder.Insung1) // Insung1 KeyCode 역순 정렬 (큰 값 우선)
                .Select(item => item.Clone())
                .ToList();

            // ===== 상세 로깅: listCreated 내용 출력 =====
            Debug.WriteLine($"[{APP_NAME}] ===== listCreated (신규접수용) 상세 정보 =====");
            Debug.WriteLine($"[{APP_NAME}] listCreated 개수: {listCreated.Count}");
            for (int i = 0; i < listCreated.Count; i++)
            {
                var item = listCreated[i];
                Debug.WriteLine($"[{APP_NAME}]   [{i}] KeyCode={item.KeyCode}, " +
                              $"StateFlag={item.StateFlag}, " +
                              $"SeqNo={item.NewOrder.Insung1 ?? "(없음)"}, " +
                              $"CarType={item.NewOrder.CarType}, " +
                              $"CallCustFrom={item.NewOrder.CallCustFrom}, " +
                              $"출발={item.NewOrder.StartDongBasic}");
            }
            Debug.WriteLine($"[{APP_NAME}] ==========================================");

            // ===== 상세 로깅: listEtcGroup 내용 출력 =====
            Debug.WriteLine($"[{APP_NAME}] ===== listEtcGroup (기존주문관리용) 상세 정보 =====");
            Debug.WriteLine($"[{APP_NAME}] listEtcGroup 개수: {listEtcGroup.Count}");
            for (int i = 0; i < listEtcGroup.Count; i++)
            {
                var item = listEtcGroup[i];
                Debug.WriteLine($"[{APP_NAME}]   [{i}] KeyCode={item.KeyCode}, " +
                              $"StateFlag={item.StateFlag}, " +
                              $"SeqNo={item.NewOrder.Insung1 ?? "(없음)"}, " +
                              $"CarType={item.NewOrder.CarType}");
            }
            Debug.WriteLine($"[{APP_NAME}] ==========================================");

            // 할일 갯수 체크
            int tot = listCreated.Count + listEtcGroup.Count;

            if (tot == 0)
            {
                m_lRestCount += 1;
                if (m_lRestCount % 60 == 0) // 5 ~ 10분 정도
                {
                    // TODO: Helper 함수 구현 필요 (세션 3에서 설계됨)
                    // await m_Context.RcptRegPageAct.Click조회버튼Async(ctrl);
                    await Task.Delay(c_nWaitLong, ctrl.Token);
                }

                Debug.WriteLine($"[{APP_NAME}] 자동배차 할일 없음: lAllocCount={lAllocCount}");
                return new StdResult_Status(StdResult.Success); // 할일 없으면 돌아간다
            }
            else
            {
                m_lRestCount = 0;
                Debug.WriteLine($"[{APP_NAME}] 자동배차 할일 있음: lAllocCount={lAllocCount}, tot={tot}, listCreated={listCreated.Count}, listEtcGroup={listEtcGroup.Count}");
            }
            #endregion

            #region 3. Check Datagrid
            // Datagrid 윈도우 존재 확인 (최대 c_nRepeatShort회 재시도)
            bool bDatagridExists = false;
            for (int i = 0; i < c_nRepeatShort; i++)
            {
                await ctrl.WaitIfPausedOrCancelledAsync();

                // Datagrid 핸들이 유효하고 윈도우가 존재하는지 확인
                if (m_Context.MemInfo.RcptPage.DG오더_hWnd != IntPtr.Zero &&
                    Std32Window.IsWindow(m_Context.MemInfo.RcptPage.DG오더_hWnd))
                {
                    bDatagridExists = true;
                    Debug.WriteLine($"[{APP_NAME}] Datagrid 윈도우 확인 완료 (시도 {i + 1}회)");
                    break;
                }

                // 마지막 시도가 아닐 때만 대기 (불필요한 지연 방지)
                if (i < c_nRepeatShort - 1)
                {
                    await Task.Delay(c_nWaitNormal, ctrl.Token);
                }
            }

            if (!bDatagridExists)
            {
                Debug.WriteLine($"[{APP_NAME}] Datagrid 윈도우를 찾을 수 없음");
                return new StdResult_Status(StdResult.Fail, "Datagrid 윈도우를 찾을 수 없습니다.", "NwInsung01/AutoAllocAsync_03");
            }
            #endregion

            #region 4. Created Order 처리 (신규)
            if (listCreated.Count > 0)
            {
                Debug.WriteLine($"[{APP_NAME}] Region 4: 신규 주문 처리 시작 (총 {listCreated.Count}건)");

                // 역순으로 처리 (삭제를 위해)
                for (int i = listCreated.Count; i > 0; i--)
                {
                    await ctrl.WaitIfPausedOrCancelledAsync();

                    int index = i - 1;
                    if (index < 0) break;

                    AutoAlloc item = listCreated[index];
                    Debug.WriteLine($"[{APP_NAME}]   [{i}/{listCreated.Count}] 신규 주문 처리: " +
                                  $"KeyCode={item.KeyCode}, 상태={item.NewOrder.OrderState}");

                    // 신규 주문 등록 시도
                    StdResult_Status resultState = await m_Context.RcptRegPageAct.CheckIsOrderAsync_AssumeKaiNewOrder(item, ctrl);

                    switch (resultState.Result)
                    {
                        case StdResult.Success:
                            // 성공: 큐에 재적재 (다음 사이클에 관리 대상으로 분류됨)
                            Debug.WriteLine($"[{APP_NAME}]   [{i}] 신규 주문 등록 성공: {item.KeyCode}");

                            // NotChanged 상태로 변경
                            item.StateFlag = PostgService_Common_OrderState.NotChanged;

                            // 큐에 재적재 (다음 사이클에 listEtcGroup으로 분류)
                            ExternalAppController.QueueManager.ReEnqueue(item, StdConst_Network.INSUNG1);
                            break;

                        case StdResult.Fail:
                            // 신규 등록 실패는 치명적 에러 → 앱 종료
                            Debug.WriteLine($"[{APP_NAME}]   [{i}] 신규 주문 등록 실패 (치명적): {item.KeyCode} - {resultState.sErr}");

                            ErrMsgBox(
                                $"신규 주문 등록 실패 (앱 종료)\n주문: {item.KeyCode}\n\n{resultState.sErr}",
                                resultState.sPos);

                            Environment.Exit(1);
                            break;

                        default:
                            // 예상 못한 결과 → 앱 종료
                            Debug.WriteLine($"[{APP_NAME}]   [{i}] 예상 못한 결과: {resultState.Result}");
                            Environment.Exit(1);
                            break;
                    }
                }

                // Region 4 완료: listCreated 일괄 정리
                Debug.WriteLine($"[{APP_NAME}] Region 4 완료");
                listCreated.Clear();
            }
            #endregion

            #region 5. Updated, NotChanged Order 처리 (기존)
            if (listEtcGroup.Count > 0)
            {
                Debug.WriteLine($"[{APP_NAME}] Region 5: 기존 주문 관리 시작 (총 {listEtcGroup.Count}건)");

                #region 5-1. 조회버튼 클릭 + 총계 확인
                StdResult_Status resultQuery = await m_Context.RcptRegPageAct.Click조회버튼Async(ctrl);
                if (resultQuery.Result != StdResult.Success)
                {
                    Debug.WriteLine($"[{APP_NAME}] 조회 버튼 클릭 실패: {resultQuery.sErr}");
                    return new StdResult_Status(StdResult.Retry, $"조회 버튼 클릭 실패: {resultQuery.sErr}", "NwInsung01/AutoAllocAsync_50");
                }

                string sThisTotCount = Std32Window.GetWindowCaption(m_Context.MemInfo.RcptPage.CallCount_hWnd총계);
                if (string.IsNullOrEmpty(sThisTotCount))
                {
                    Debug.WriteLine($"[{APP_NAME}] 접수상황판 총계 읽기 실패");
                    return new StdResult_Status(StdResult.Retry, "접수상황판 총계 읽기 실패", "NwInsung01/AutoAllocAsync_51");
                }

                Debug.WriteLine($"[{APP_NAME}] 총계: {sThisTotCount}");
                #endregion

                #region 5-2. 오더 총갯수/페이지 산정
                await ctrl.WaitIfPausedOrCancelledAsync();

                int nTotPage = 1;
                int nThisTotCount = StdConvert.StringToInt(sThisTotCount, -1);
                if (nThisTotCount < 0)
                {
                    Debug.WriteLine($"[{APP_NAME}] 총계가 음수: {sThisTotCount}");
                    return new StdResult_Status(StdResult.Retry, $"접수상황판 총계가 음수입니다: {sThisTotCount}", "NwInsung01/AutoAllocAsync_52");
                }

                // 페이지 계산
                if (nThisTotCount > m_Context.FileInfo.접수등록Page_DG오더_dataRowCount)
                {
                    nTotPage = nThisTotCount / m_Context.FileInfo.접수등록Page_DG오더_dataRowCount;
                    if ((nThisTotCount % m_Context.FileInfo.접수등록Page_DG오더_dataRowCount) > 0)
                        nTotPage += 1;
                }

                Debug.WriteLine($"[{APP_NAME}] 총 데이터: {nThisTotCount}개, 총 페이지: {nTotPage}");
                #endregion

                #region 5-3. Kai갯수만큼 작업
                // TODO: listEtcGroup 순회하며 StateFlag별 처리
                #endregion

                // Region 5 완료
                Debug.WriteLine($"[{APP_NAME}] Region 5 완료");
                listEtcGroup.Clear();
            }
            #endregion

            #region 6. 처리 완료된 항목을 큐에 재적재
            //if (listProcessed.Count > 0)
            //{
            //    foreach (var item in listProcessed)
            //    {
            //        ExternalAppController.QueueManager.ReEnqueue(item, StdConst_Network.INSUNG1);
            //    }
            //    Debug.WriteLine($"[{APP_NAME}] 처리 완료된 항목 {listProcessed.Count}개를 큐에 재적재");
            //}
            #endregion

            Debug.WriteLine($"[NwInsung01] AutoAllocAsync 완료 - Count={lAllocCount}");
            return new StdResult_Status(StdResult.Success);
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("[NwInsung01] AutoAllocAsync 취소됨");
            return new StdResult_Status(StdResult.Skip, "작업 취소됨", "NwInsung01/AutoAllocAsync_Cancel");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NwInsung01] AutoAllocAsync 예외: {ex.Message}");
            return new StdResult_Status(StdResult.Fail, ex.Message, "NwInsung01/AutoAllocAsync_999");
        }
    }
    #endregion

    #region Helper Methods
    /// <summary>
    /// FileInfo를 JSON 파일에서 읽어서 Context.FileInfo에 로드
    /// </summary>
    private async Task<StdResult_Error> ReadInfoFileAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                string sFilePath = Path.Combine(s_sDataDir, INFO_FILE_NAME);

                // 파일 존재 체크
                if (!File.Exists(sFilePath))
                {
                    return new StdResult_Error(
                        $"[{APP_NAME}] FileInfo 파일이 없습니다: {sFilePath}",
                        "NwInsung01/ReadInfoFileAsync_01");
                }

                // 파일 읽기
                string jsonContent;
                using (StreamReader reader = new StreamReader(sFilePath))
                {
                    jsonContent = reader.ReadToEnd();
                }

                // JSON 역직렬화
                InsungsInfo_File fileInfo = JsonConvert.DeserializeObject<InsungsInfo_File>(jsonContent);
                if (fileInfo == null)
                {
                    return new StdResult_Error(
                        $"[{APP_NAME}] FileInfo 파일 역직렬화 실패: {sFilePath}",
                        "NwInsung01/ReadInfoFileAsync_02");
                }

                // Context의 FileInfo에 덮어씌우기
                m_Context.FileInfo = fileInfo;

                Debug.WriteLine($"[NwInsung01] FileInfo 파일 로드 완료: {sFilePath}");
                return null; // 성공
            }
            catch (Exception ex)
            {
                return new StdResult_Error(
                    $"[{APP_NAME}] FileInfo 파일 읽기 예외: {ex.Message}",
                    "NwInsung01/ReadInfoFileAsync_99");
            }
        });
    }

    /// <summary>
    /// Context.FileInfo를 JSON 파일로 저장 (테스트/디버깅용)
    /// </summary>
    private void WriteInfoToFile_AtFirst()
    {
        try
        {
            // 이미 Context.FileInfo가 초기화되어 있으므로 그대로 사용
            InsungsInfo_File info = m_Context.FileInfo;

            // TODO: 필요시 기본값 설정 (현재는 InsungsInfo_File 생성자에서 설정됨)
            // 예: info.App_sPredictFolder = @"C:\Program Files (x86)\INSUNGDATA\인성퀵화물통합솔루션_KN";

            // JSON 직렬화
            string json = JsonConvert.SerializeObject(info, Formatting.Indented);
            string sFilePath = Path.Combine(s_sDataDir, INFO_FILE_NAME);

            // Data 폴더 생성 (없을 경우)
            string dataDir = Path.GetDirectoryName(sFilePath);
            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }

            // 파일 저장
            using (StreamWriter writer = new StreamWriter(sFilePath))
            {
                writer.Write(json);
            }

            Debug.WriteLine($"[NwInsung01] FileInfo 파일 저장 완료: {sFilePath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NwInsung01] FileInfo 파일 저장 실패: {ex.Message}");
        }
    }
    #endregion

    #region 생성자
    public NwInsung01()
    {
        Debug.WriteLine($"[NwInsung01] 생성자 호출: Id={s_Id}, Use={s_Use} --------------------------------------------------------");

        // Context 생성
        m_Context = new InsungContext(APP_NAME, s_Id, s_Pw);

        // AppAct 생성
        m_Context.AppAct = new InsungsAct_App(m_Context);

        // MainWndAct 생성
        m_Context.MainWndAct = new InsungsAct_MainWnd(m_Context);

        // RcptRegPageAct 생성
        m_Context.RcptRegPageAct = new InsungsAct_RcptRegPage(m_Context);

        Debug.WriteLine($"[NwInsung01] Context 생성 완료: AppName={m_Context.AppName}");
    }
    #endregion
}
#nullable restore
