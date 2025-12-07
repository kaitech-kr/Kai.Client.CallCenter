using System.Diagnostics;
using Draw = System.Drawing;

using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;
using Kai.Common.NetDll_WpfCtrl.NetOFR;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Services;
using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgWnd;

using Kai.Client.CallCenter.Networks.NwOnecalls;
using Kai.Client.CallCenter.Classes;
using Kai.Client.CallCenter.Classes.Class_Master;
using Kai.Client.CallCenter.OfrWorks;
using Kai.Client.CallCenter.Windows;
using static Kai.Client.CallCenter.Classes.CommonVars;

namespace Kai.Client.CallCenter.Networks;
#nullable disable

/// <summary>
/// 원콜 앱 (IExternalApp 구현)
/// </summary>
public class NwOnecall : IExternalApp
{
    #region Static Configuration (appsettings.json에서 로드)
    public static bool s_Use { get; set; } = false;
    public static string s_Id { get; set; } = string.Empty;
    public static string s_Pw { get; set; } = string.Empty;
    public static string s_AppPath { get; set; } = string.Empty;
    #endregion

    #region Private Fields
    private OnecallContext m_Context;
    private int m_nDatagridFailCount = 0;
    private const int MAX_DATAGRID_FAIL_COUNT = 3;

    /// <summary>
    /// Onecall SeqNo 가져오기
    /// </summary>
    private string GetOnecallSeqno(AutoAllocModel item) => item.NewOrder.Onecall;
    #endregion

    #region IExternalApp 구현
    public bool IsUsed => s_Use;
    public string AppName => StdConst_Network.ONECALL;

    public async Task<StdResult_Status> InitializeAsync()
    {
        try
        {
            //Debug.WriteLine($"[{AppName}] InitializeAsync 시작: Id={s_Id}");

            // 1. UpdaterWorkAsync - 앱 실행 및 Splash 윈도우 찾기
            //Debug.WriteLine($"[{AppName}] UpdaterWorkAsync 호출...");
            var resultErr = await m_Context.AppAct.UpdaterWorkAsync(s_AppPath);
            if (resultErr != null)
            {
                Debug.WriteLine($"[{AppName}] UpdaterWorkAsync 실패: {resultErr.sErrNPos}");
                return new StdResult_Status(StdResult.Fail, resultErr.sErrNPos, $"{AppName}/InitializeAsync_01");
            }
            Debug.WriteLine($"[{AppName}] UpdaterWorkAsync 성공");

            // 2. SplashWorkAsync - 로그인 처리
            //Debug.WriteLine($"[{AppName}] SplashWorkAsync 호출...");
            resultErr = await m_Context.AppAct.SplashWorkAsync(AppName, s_Id, s_Pw);
            if (resultErr != null)
            {
                Debug.WriteLine($"[{AppName}] SplashWorkAsync 실패: {resultErr.sErrNPos}");
                return new StdResult_Status(StdResult.Fail, resultErr.sErrNPos, $"{AppName}/InitializeAsync_02");
            }
            Debug.WriteLine($"[{AppName}] SplashWorkAsync 성공");

            // 3. MainWndAct 생성
            m_Context.MainWndAct = new OnecallAct_MainWnd(m_Context);
            //Debug.WriteLine($"[{AppName}] MainWndAct 생성 완료");

            // 4. InitAsync - 메인 윈도우 초기화 (찾기 + 이동 + 최대화 + 자식 윈도우)
            //Debug.WriteLine($"[{AppName}] MainWnd InitAsync 호출...");
            resultErr = await m_Context.MainWndAct.InitAsync();
            if (resultErr != null)
            {
                Debug.WriteLine($"[{AppName}] MainWnd InitAsync 실패: {resultErr.sErrNPos}");
                return new StdResult_Status(StdResult.Fail, resultErr.sErrNPos, $"{AppName}/InitializeAsync_03");
            }
            Debug.WriteLine($"[{AppName}] MainWnd InitAsync 성공");

            // 5. RcptRegPageAct 생성 및 초기화
            m_Context.RcptRegPageAct = new OnecallAct_RcptRegPage(m_Context);
            //Debug.WriteLine($"[{AppName}] RcptRegPageAct 생성 완료");

            //Debug.WriteLine($"[{AppName}] RcptRegPage InitializeAsync 호출...");
            resultErr = await m_Context.RcptRegPageAct.InitializeAsync();
            if (resultErr != null)
            {
                Debug.WriteLine($"[{AppName}] RcptRegPage InitializeAsync 실패: {resultErr.sErrNPos}");
                return new StdResult_Status(StdResult.Fail, resultErr.sErrNPos, $"{AppName}/InitializeAsync_04");
            }
            Debug.WriteLine($"[{AppName}] RcptRegPage InitializeAsync 성공");

            Debug.WriteLine($"[{AppName}] InitializeAsync 완료");
            return new StdResult_Status(StdResult.Success, string.Empty, $"{AppName}/InitializeAsync");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{AppName}] InitializeAsync 예외 발생: {ex.Message}");
            return new StdResult_Status(StdResult.Fail, $"예외 발생: {ex.Message}", $"{AppName}/InitializeAsync_999");
        }
    }

    public async Task<StdResult_Status> AutoAllocAsync(long lAllocCount, CancelTokenControl ctrl)
    {
        try
        {
            Debug.WriteLine($"\n-----------------[{AppName}] AutoAllocAsync 시작 - Count={lAllocCount}--------------------------");

            // Cancel/Pause 체크 - Region 2 진입 전
            await ctrl.WaitIfPausedOrCancelledAsync();

            #region 1. 사전작업
            // TopMost 설정 - 원콜 메인 창을 최상위로
            await Std32Window.SetWindowTopMostAndReleaseAsync(m_Context.MemInfo.Main.TopWnd_hWnd, CommonVars.c_nWaitShort);
            Debug.WriteLine($"[{AppName}] TopMost 설정 완료");
            #endregion

            #region 2. Local Variables 초기화
            // 큐에서 주문 가져오기
            string queueName = StdConst_Network.ONECALL;
            List<AutoAllocModel> listFromController = ExternalAppController.QueueManager.DequeueAllToList(queueName);
            Debug.WriteLine($"[{AppName}] 큐에서 가져온 주문 개수: {listFromController.Count}");

            // 작업잔량 리스트 복사
            var listOnecall = new List<AutoAllocModel>(listFromController);

            // 신규(대기→접수 포함) 판별 함수
            Func<AutoAllocModel, bool> isNewFromWaiting = item => item.OldOrder?.OrderState == "대기" && item.NewOrder.OrderState == "접수" && string.IsNullOrEmpty(item.NewOrder.Onecall);

            // listCreated 분류 (신규)
            var listCreated = listOnecall
                .Where(item => item.StateFlag.HasFlag(PostgService_Common_OrderState.Created) ||
                               item.StateFlag.HasFlag(PostgService_Common_OrderState.Existed_NonSeqno) ||
                               isNewFromWaiting(item))
                .OrderByDescending(item => GetOnecallSeqno(item))
                .Select(item => item.Clone())
                .ToList();

            // listEtcGroup 분류 (기존)
            var listEtcGroup = listOnecall
                .Where(item => !(item.StateFlag.HasFlag(PostgService_Common_OrderState.Created) ||
                               item.StateFlag.HasFlag(PostgService_Common_OrderState.Existed_NonSeqno) ||
                               isNewFromWaiting(item)))
                .OrderByDescending(item => GetOnecallSeqno(item))
                .Select(item => item.Clone())
                .ToList();

            // 상세 로깅
            Debug.WriteLine($"[{AppName}] listCreated={listCreated.Count}, listEtcGroup={listEtcGroup.Count}");

            // 할일 체크
            int tot = listCreated.Count + listEtcGroup.Count;
            if (tot == 0)
            {
                Debug.WriteLine($"[{AppName}] 자동배차 할일 없음");
                return new StdResult_Status(StdResult.Success);
            }
            #endregion

            #region 3. Check Datagrid
            bool bDatagridExists = false;
            for (int i = 0; i < c_nRepeatShort; i++)
            {
                await ctrl.WaitIfPausedOrCancelledAsync();

                await Task.Delay(c_nWaitNormal, ctrl.Token);

                if (m_Context.MemInfo.RcptPage.DG오더_hWndTop != IntPtr.Zero && Std32Window.IsWindow(m_Context.MemInfo.RcptPage.DG오더_hWndTop))
                {
                    bDatagridExists = true;
                    break;
                }
            }

            if (!bDatagridExists)
            {
                m_nDatagridFailCount++;
                Debug.WriteLine($"[{AppName}] Datagrid 윈도우를 찾을 수 없음 (연속 실패: {m_nDatagridFailCount}/{MAX_DATAGRID_FAIL_COUNT})");

                ExternalAppController.QueueManager.ClearQueue(queueName);
                Debug.WriteLine($"[{AppName}] 큐 비움");

                if (m_nDatagridFailCount >= MAX_DATAGRID_FAIL_COUNT)
                {
                    Debug.WriteLine($"[{AppName}] {MAX_DATAGRID_FAIL_COUNT}회 연속 실패 → 앱 비활성화");
                    s_Use = false;
                    return new StdResult_Status(StdResult.Fail, $"Datagrid {MAX_DATAGRID_FAIL_COUNT}회 연속 실패로 앱 비활성화", $"{AppName}/AutoAllocAsync_03_Disabled");
                }

                return new StdResult_Status(StdResult.Skip, "Datagrid 윈도우를 찾을 수 없습니다.", $"{AppName}/AutoAllocAsync_03");
            }

            m_nDatagridFailCount = 0;
            #endregion

            #region 4. Created Order 처리 (신규)
            if (listCreated.Count > 0)
            {
                Debug.WriteLine($"[{AppName}] Region 4: 신규 주문 처리 시작 (총 {listCreated.Count}건)");

                for (int i = listCreated.Count; i > 0; i--)
                {
                    await ctrl.WaitIfPausedOrCancelledAsync();

                    int index = i - 1;
                    if (index < 0) break;

                    AutoAllocModel item = listCreated[index];
                    Debug.WriteLine($"[{AppName}]   [{i}/{listCreated.Count}] 신규 주문 처리: KeyCode={item.KeyCode}, 상태={item.NewOrder.OrderState}");

                    CommonResult_AutoAllocProcess resultAuto = await m_Context.RcptRegPageAct.CheckOcOrderAsync_AssumeKaiNewOrder(item, ctrl);

                    switch (resultAuto.ResultType)
                    {
                        case CEnum_AutoAllocProcessResult.SuccessAndReEnqueue:
                            Debug.WriteLine($"[{AppName}]   [{i}] 신규 주문 등록 성공: {item.KeyCode}");
                            ExternalAppController.QueueManager.ReEnqueue(item, queueName, item.StateFlag);
                            break;

                        case CEnum_AutoAllocProcessResult.SuccessAndComplete:
                            Debug.WriteLine($"[{AppName}]   [{i}] 신규 주문 비적재: {item.KeyCode}");
                            break;

                        case CEnum_AutoAllocProcessResult.FailureAndDiscard:
                            Debug.WriteLine($"[{AppName}]   [{i}] 신규 주문 등록 실패: {item.KeyCode} - {resultAuto.sErr}");
                            // TODO: 치명적 오류 시 Environment.Exit(1) 호출 여부 결정
                            break;

                        default:
                            Debug.WriteLine($"[{AppName}]   [{i}] 예상 못한 결과: {resultAuto.ResultType}");
                            break;
                    }
                }

                Debug.WriteLine($"[{AppName}] Region 4 완료");
                listCreated.Clear();
            }
            #endregion

            #region 5. 기존주문 처리 (listEtcGroup)
            if (listEtcGroup.Count > 0)
            {
                Debug.WriteLine($"[{AppName}] Region 5: 기존 주문 처리 시작 (총 {listEtcGroup.Count}건)");

                #region 5-1. 조회버튼 클릭 + 총계 확인
                // TODO: Click조회버튼Async 구현 후 호출
                // TODO: 총계 OFR
                int nThisTotCount = 0; // TODO: 실제 총계로 대체
                #endregion

                #region 5-2. 페이지 산정
                // TODO: 페이지 계산 로직
                #endregion

                #region 5-3. 페이지별 리스트 검사
                // TODO: CheckOcOrderAsync_AssumeKaiUpdated 구현 후 호출
                #endregion

                Debug.WriteLine($"[{AppName}] Region 5 완료");
                listEtcGroup.Clear();
            }
            #endregion

            return new StdResult_Status(StdResult.Success, string.Empty, $"{AppName}/AutoAllocAsync");
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine($"[{AppName}] AutoAllocAsync 취소됨");
            return new StdResult_Status(StdResult.Skip, "작업 취소됨", $"{AppName}/AutoAllocAsync_Cancel");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{AppName}] AutoAllocAsync 예외 발생: {ex.Message}");
            return new StdResult_Status(StdResult.Fail, $"예외 발생: {ex.Message}", $"{AppName}/AutoAllocAsync_999");
        }
    }

    public void Shutdown()
    {
        try
        {
            Debug.WriteLine($"[{AppName}] Shutdown 시작");

            // AppAct.Close() 호출 - Onecall 앱 종료
            if (m_Context?.AppAct != null)
            {
                StdResult_Error resultClose = m_Context.AppAct.Close();
                if (resultClose != null)
                {
                    Debug.WriteLine($"[{AppName}] Close 실패: {resultClose.sErrNPos}");
                }
                else
                {
                    Debug.WriteLine($"[{AppName}] Close 성공");
                }
            }

            Debug.WriteLine($"[{AppName}] Shutdown 완료");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{AppName}] Shutdown 예외 발생: {ex.Message}");
        }
    }
    #endregion

    #region Dispose
    private bool disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                Debug.WriteLine($"[{AppName}] Dispose 호출");
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

    #region 생성자
    /// <summary>
    /// 원콜 생성자
    /// </summary>
    public NwOnecall()
    {
        Debug.WriteLine($"[{AppName}] 생성자 호출");
        m_Context = new OnecallContext(StdConst_Network.ONECALL, s_Id, s_Pw);
        m_Context.AppAct = new OnecallAct_App(m_Context);
    }
    #endregion
}
#nullable restore
