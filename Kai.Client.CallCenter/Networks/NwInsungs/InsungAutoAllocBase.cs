using System.IO;
using Newtonsoft.Json;
using System.Diagnostics;
using Draw = System.Drawing;
using System.Windows.Media;

using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;
using Kai.Common.NetDll_WpfCtrl.NetWnds;
using Kai.Common.NetDll_WpfCtrl.NetOFR;
using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgBox;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Services;

using Kai.Client.CallCenter.Classes;
using Kai.Client.CallCenter.Classes.Class_Master;
using Kai.Client.CallCenter.Networks.NwInsungs;
using Kai.Client.CallCenter.Windows;
using Kai.Client.CallCenter.OfrWorks;
using static Kai.Client.CallCenter.Classes.CommonVars;

namespace Kai.Client.CallCenter.Networks.NwInsungs;
#nullable disable

/// <summary>
/// 인성1, 인성2 공통 로직을 담는 추상 베이스 클래스
/// </summary>
public abstract class InsungAutoAllocBase : IExternalApp
{
    #region Abstract Members (상속 클래스에서 구현)
    /// <summary>
    /// 앱 이름 (INSUNG1 또는 INSUNG2)
    /// </summary>
    protected abstract string APP_NAME { get; }

    /// <summary>
    /// FileInfo 파일 이름
    /// </summary>
    protected abstract string INFO_FILE_NAME { get; }

    /// <summary>
    /// Static 설정 - Use 플래그 (Get)
    /// </summary>
    protected abstract bool GetStaticUse();

    /// <summary>
    /// Static 설정 - Use 플래그 (Set)
    /// </summary>
    protected abstract void SetStaticUse(bool value);

    /// <summary>
    /// Static 설정 - ID
    /// </summary>
    protected abstract string GetStaticId();

    /// <summary>
    /// Static 설정 - Password
    /// </summary>
    protected abstract string GetStaticPw();

    /// <summary>
    /// Static 설정 - AppPath
    /// </summary>
    protected abstract string GetStaticAppPath();
    #endregion

    #region Context
    /// <summary>
    /// 인성의 모든 공용 데이터를 담는 Context
    /// </summary>
    protected InsungContext m_Context = null;

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

    /// <summary>
    /// Datagrid 연속 실패 카운터 (N회 연속 실패 시 앱 비활성화)
    /// </summary>
    private int m_nDatagridFailCount = 0;
    private const int MAX_DATAGRID_FAIL_COUNT = 3; // 3회 연속 실패 시 비활성화
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

    #region Helper Methods - Field Access
    /// <summary>
    /// AutoAllocModel에서 인성 주문번호 가져오기 (AppName에 따라 Insung1 또는 Insung2)
    /// </summary>
    protected string GetInsungSeqno(AutoAllocModel item)
    {
        return m_Context.AppName == StdConst_Network.INSUNG1
            ? item.NewOrder.Insung1
            : item.NewOrder.Insung2;
    }

    /// <summary>
    /// AutoAllocModel에 인성 주문번호 설정하기 (AppName에 따라 Insung1 또는 Insung2)
    /// </summary>
    protected void SetInsungSeqno(AutoAllocModel item, string seqno)
    {
        if (m_Context.AppName == StdConst_Network.INSUNG1)
            item.NewOrder.Insung1 = seqno;
        else
            item.NewOrder.Insung2 = seqno;
    }

    /// <summary>
    /// 큐 이름 가져오기 (AppName 기반)
    /// </summary>
    protected string GetQueueName()
    {
        return m_Context.AppName;
    }
    #endregion

    #region IExternalApp 구현
    public bool IsUsed => GetStaticUse();
    public string AppName => APP_NAME;

    public async Task<StdResult_Status> InitializeAsync()
    {
        try
        {
            Debug.WriteLine($"[{APP_NAME}] InitializeAsync 시작: Id={GetStaticId()}");

            // 1. FileInfo 파일에서 설정 로드
            StdResult_Error resultErr = await ReadInfoFileAsync();
            if (resultErr != null)
            {
                Debug.WriteLine($"[{APP_NAME}] FileInfo 로드 실패: {resultErr.sErrNPos}");
                return new StdResult_Status(StdResult.Fail, resultErr.sErrNPos, $"{APP_NAME}/InitializeAsync_01");
            }

            // 2. 앱 경로 확인
            if (string.IsNullOrEmpty(GetStaticAppPath()))
            {
                Debug.WriteLine($"[{APP_NAME}] AppPath가 설정되지 않았습니다.");
                return new StdResult_Status(StdResult.Fail, "AppPath가 appsettings.json에 설정되지 않았습니다.", $"{APP_NAME}/InitializeAsync_02");
            }

            // Show Loading
            if (s_Screens.m_WorkingMonitor != s_Screens.m_PrimaryMonitor) // 작업 모니터가 기본 모니터면 LoadingPanel을 사용하지 않는다.
                NetLoadingWnd.ShowLoading(s_MainWnd, $"   {APP_NAME} 초기화 작업중입니다, \n     입력작업을 하지 마세요...   ");

            // 3. UpdaterWork - Updater 실행 및 종료 대기
            StdResult_Error resultUpdater = await m_Context.AppAct.UpdaterWorkAsync(GetStaticAppPath());
            if (resultUpdater != null)
            {
                Debug.WriteLine($"[{APP_NAME}] UpdaterWork 실패: {resultUpdater.sErrNPos}");
                return new StdResult_Status(StdResult.Fail, resultUpdater.sErrNPos, $"{APP_NAME}/InitializeAsync_03");
            }

            // 4. SplashWork - 스플래시 창 처리 및 로그인
            StdResult_Error resultSplash = await m_Context.AppAct.SplashWorkAsync();
            if (resultSplash != null)
            {
                Debug.WriteLine($"[{APP_NAME}] SplashWork 실패: {resultSplash.sErrNPos}");
                return new StdResult_Status(StdResult.Fail, resultSplash.sErrNPos, $"{APP_NAME}/InitializeAsync_04");
            }

            // 5. MainWnd 초기화
            StdResult_Error resultMainWnd = await m_Context.MainWndAct.InitializeAsync();
            if (resultMainWnd != null)
            {
                Debug.WriteLine($"[{APP_NAME}] MainWnd 초기화 실패: {resultMainWnd.sErrNPos}");
                return new StdResult_Status(StdResult.Fail, resultMainWnd.sErrNPos, $"{APP_NAME}/InitializeAsync_05");
            }

            // 6. RcptRegPage 초기화
            StdResult_Error resultRcptRegPage = await m_Context.RcptRegPageAct.InitializeAsync();
            if (resultRcptRegPage != null)
            {
                Debug.WriteLine($"[{APP_NAME}] RcptRegPage 초기화 실패: {resultRcptRegPage.sErrNPos}");
                return new StdResult_Status(StdResult.Fail, resultRcptRegPage.sErrNPos, $"{APP_NAME}/InitializeAsync_06");
            }

            Debug.WriteLine($"[{APP_NAME}] InitializeAsync 완료");
            return new StdResult_Status(StdResult.Success);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{APP_NAME}] InitializeAsync 실패: {ex.Message}");
            return new StdResult_Status(StdResult.Fail, ex.Message, $"{APP_NAME}/InitializeAsync_99");
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
            Debug.WriteLine($"[{APP_NAME}] Shutdown 시작");

            // AppAct.Close() 호출 - 인성 앱 종료
            if (m_Context?.AppAct != null)
            {
                StdResult_Error resultClose = m_Context.AppAct.Close();
                if (resultClose != null)
                {
                    Debug.WriteLine($"[{APP_NAME}] Close 실패: {resultClose.sErrNPos}");
                }
                else
                {
                    Debug.WriteLine($"[{APP_NAME}] Close 성공");
                }
            }

            Debug.WriteLine($"[{APP_NAME}] Shutdown 완료");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{APP_NAME}] Shutdown 실패: {ex.Message}");
        }
    }

    public async Task<StdResult_Status> AutoAllocAsync(long lAllocCount, CancelTokenControl ctrl)
    {
        Draw.Bitmap bmpPage = null;

        try
        {
            Debug.WriteLine($"\n-----------------[{APP_NAME}] AutoAllocAsync 시작 - Count={lAllocCount}--------------------------");

            // Cancel/Pause 체크 - Region 2 진입 전
            await ctrl.WaitIfPausedOrCancelledAsync();

            #region 1. 사전작업
            // TopMost 설정 - 인성 메인 창을 최상위로
            await Std32Window.SetWindowTopMostAndReleaseAsync(m_Context.MemInfo.Main.TopWnd_hWnd, c_nWaitShort);
            Debug.WriteLine($"[{APP_NAME}] TopMost 설정 완료");
            #endregion

            #region 2. Local Variables 초기화
            // 컨트롤러 큐에서 주문 리스트 가져오기 (DequeueAllToList로 큐 비우기)
            string queueName = GetQueueName();
            List<AutoAllocModel> listFromController = ExternalAppController.QueueManager.DequeueAllToList(queueName);
            Debug.WriteLine($"[{APP_NAME}] 큐에서 가져온 주문 개수: {listFromController.Count}");

            // 작업잔량 파악 리스트 (원본 복사)
            var listInsung = new List<AutoAllocModel>(listFromController);

            // 처리 완료된 항목을 담을 리스트 (Region 4, 5에서 사용)
            var listCreated = listInsung
                .Where(item => item.StateFlag.HasFlag(PostgService_Common_OrderState.Created) ||
                               item.StateFlag.HasFlag(PostgService_Common_OrderState.Existed_NonSeqno))
                .OrderByDescending(item => GetInsungSeqno(item)) // 인성 KeyCode 역순 정렬 (큰 값 우선)
                .Select(item => item.Clone())
                .ToList();

            var listEtcGroup = listInsung
                .Where(item => !(item.StateFlag.HasFlag(PostgService_Common_OrderState.Created) ||
                               item.StateFlag.HasFlag(PostgService_Common_OrderState.Existed_NonSeqno)))
                .OrderByDescending(item => GetInsungSeqno(item)) // 인성 KeyCode 역순 정렬 (큰 값 우선)
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
                              $"SeqNo={GetInsungSeqno(item) ?? "(없음)"}, " +
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
                              $"SeqNo={GetInsungSeqno(item) ?? "(없음)"}, " +
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
                    await m_Context.RcptRegPageAct.Click조회버튼Async(ctrl);
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
                    //Debug.WriteLine($"[{APP_NAME}] Datagrid 윈도우 확인 완료 (시도 {i + 1}회)");
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
                m_nDatagridFailCount++;
                Debug.WriteLine($"[{APP_NAME}] Datagrid 윈도우를 찾을 수 없음 (연속 실패: {m_nDatagridFailCount}/{MAX_DATAGRID_FAIL_COUNT})");

                // 큐 비우기
                ExternalAppController.QueueManager.ClearQueue(queueName);
                Debug.WriteLine($"[{APP_NAME}] 큐 비움");

                // N회 연속 실패 시 앱 비활성화
                if (m_nDatagridFailCount >= MAX_DATAGRID_FAIL_COUNT)
                {
                    Debug.WriteLine($"[{APP_NAME}] {MAX_DATAGRID_FAIL_COUNT}회 연속 실패 → 앱 비활성화");
                    SetStaticUse(false);
                    return new StdResult_Status(StdResult.Fail, $"Datagrid {MAX_DATAGRID_FAIL_COUNT}회 연속 실패로 앱 비활성화", $"{APP_NAME}/AutoAllocAsync_03_Disabled");
                }

                return new StdResult_Status(StdResult.Skip, "Datagrid 윈도우를 찾을 수 없습니다.", $"{APP_NAME}/AutoAllocAsync_03");
            }

            // Datagrid 찾았으면 실패 카운터 리셋
            m_nDatagridFailCount = 0;
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

                    AutoAllocModel item = listCreated[index];
                    Debug.WriteLine($"[{APP_NAME}]   [{i}/{listCreated.Count}] 신규 주문 처리: " +
                                  $"KeyCode={item.KeyCode}, 상태={item.NewOrder.OrderState}");

                    // 신규 주문 등록 시도
                    CommonResult_AutoAllocProcess resultAuto = await m_Context.RcptRegPageAct.CheckIsOrderAsync_AssumeKaiNewOrder(item, ctrl);

                    switch (resultAuto.ResultType)
                    {
                        case CEnum_AutoAllocProcessResult.SuccessAndReEnqueue:
                            // 성공: 함수 내부에서 이미 StateFlag가 NotChanged로 설정됨
                            Debug.WriteLine($"[{APP_NAME}]   [{i}] 신규 주문 등록 성공: {item.KeyCode}");
                            ExternalAppController.QueueManager.ReEnqueue(item, queueName, item.StateFlag);
                            break;

                        case CEnum_AutoAllocProcessResult.FailureAndDiscard:
                            // 신규 등록 실패는 치명적 에러 → 앱 종료
                            Debug.WriteLine($"[{APP_NAME}]   [{i}] 신규 주문 등록 실패 (치명적): {item.KeyCode} - {resultAuto.sErr}");
                            // ErrMsgBox는 이미 생성자에서 호출됨 (디버그 모드)
                            Environment.Exit(1);
                            break;

                        default:
                            // 예상 못한 결과 → 앱 종료
                            Debug.WriteLine($"[{APP_NAME}]   [{i}] 예상 못한 결과: {resultAuto.ResultType}");
                            Environment.Exit(1);
                            break;
                    }
                }

                // Region 4 완료: listCreated 일괄 정리
                Debug.WriteLine($"[{APP_NAME}] Region 4 완료");
                listCreated.Clear();
            }
            #endregion

            #region 5. 기존주문 처리 (listEtcGroup)
            if (listEtcGroup.Count > 0)
            {
                #region 5-1. 조회버튼 클릭 + 총계 확인
                string sThisTotCount = string.Empty;
                for (int i = 0; i < c_nRepeatNormal; i++)
                {
                    await ctrl.WaitIfPausedOrCancelledAsync();

                    StdResult_Status resultQuery = await m_Context.RcptRegPageAct.Click조회버튼Async(ctrl);
                    if (resultQuery.Result == StdResult.Fail) continue;

                    sThisTotCount = Std32Window.GetWindowCaption(m_Context.MemInfo.RcptPage.CallCount_hWnd총계);
                    if (!string.IsNullOrEmpty(sThisTotCount))
                    {
                        //Debug.WriteLine($"[{APP_NAME}] 총계 읽기 성공 (시도 {i + 1}회): {sThisTotCount}");
                        break;
                    }

                    await Task.Delay(c_nWaitNormal, ctrl.Token);
                }

                if (string.IsNullOrEmpty(sThisTotCount))
                {
                    Debug.WriteLine($"[{APP_NAME}] 접수상황판 총계 읽기 실패 ({c_nRepeatNormal}회 시도)");
                    return new StdResult_Status(StdResult.Retry, "접수상황판 총계 읽기 실패", $"{APP_NAME}/AutoAllocAsync_51");
                }

                // 총계 > 0 확인
                int nThisTotCount = StdConvert.StringToInt(sThisTotCount, -1);
                if (nThisTotCount <= 0)
                {
                    Debug.WriteLine($"[{APP_NAME}] 데이터 없음 (총계: {sThisTotCount}) - Region 5 스킵");
                    return new StdResult_Status(StdResult.Success);
                }

                //Debug.WriteLine($"[{APP_NAME}] 데이터 있음 (총계: {nThisTotCount}건)");
                #endregion

                #region 5-2. 페이지 산정
                await ctrl.WaitIfPausedOrCancelledAsync();

                int nTotPage = 1;
                // 페이지 계산
                if (nThisTotCount > InsungsInfo_File.접수등록Page_DG오더_dataRowCount)
                {
                    nTotPage = nThisTotCount / InsungsInfo_File.접수등록Page_DG오더_dataRowCount;
                    if ((nThisTotCount % InsungsInfo_File.접수등록Page_DG오더_dataRowCount) > 0)
                        nTotPage += 1;
                }

                // 여러 페이지면 스크롤 핸들 다시 얻어야함
                if (nTotPage > 1)
                    m_Context.MemInfo.RcptPage.DG오더_hWnd수직스크롤 =
                        Std32Window.GetWndHandle_FromRelDrawPt(m_Context.MemInfo.Main.TopWnd_hWnd, m_Context.FileInfo.접수등록Page_DG오더_ptChkRel수직스크롤M);
                #endregion

                #region 5-3. 페이지별 리스트 검사 및 처리
                for (int pageIdx = 0; pageIdx < nTotPage; pageIdx++)
                {
                    #region 1. 사전작업
                    await ctrl.WaitIfPausedOrCancelledAsync();

                    // 현재 페이지가 맞는지 체크
                    int nExpectedFirstNum = InsungsAct_RcptRegPage.GetExpectedFirstRowNum(nThisTotCount, pageIdx);
                    //Debug.WriteLine($"[{APP_NAME}] 페이지 {pageIdx + 1}/{nTotPage} → 예상 첫 번호: {nExpectedFirstNum}");

                    // 페이지 검증 및 자동 조정
                    StdResult_Status resultVerify = await m_Context.RcptRegPageAct.VerifyAndAdjustPageAsync(nExpectedFirstNum, ctrl);
                    if (resultVerify.Result == StdResult.Fail)
                        return resultVerify;

                    // 페이지 캡처
                    for (int j = 0; j < CommonVars.c_nRepeatShort; j++)
                    {
                        bmpPage = OfrService.CaptureScreenRect_InWndHandle(m_Context.MemInfo.RcptPage.DG오더_hWnd);
                        if (bmpPage != null) break;
                        await Task.Delay(CommonVars.c_nWaitShort, ctrl.Token);
                    }
                    if (bmpPage == null)
                    {
                        Debug.WriteLine($"[{APP_NAME}] 페이지 {pageIdx + 1} 캡처 실패 ({CommonVars.c_nRepeatShort}회 시도)");
                        return new StdResult_Status(StdResult.Fail, $"페이지 {pageIdx + 1} DG 캡처 실패", $"{APP_NAME}/AutoAllocAsync_Region5_3_Capture");
                    }

                    // 유효 로우 갯수 얻기
                    StdResult_Int resultInt = await m_Context.RcptRegPageAct.GetValidRowCountAsync(bmpPage);
                    if (resultInt.nResult == 0)
                    {
                        Debug.WriteLine($"[{APP_NAME}] 페이지 {pageIdx + 1} 유효 로우 갯수 얻기 실패: {resultInt.sErr}");
                        return new StdResult_Status(StdResult.Fail, resultInt.sErr, resultInt.sPos);
                    }
                    Debug.WriteLine($"[{APP_NAME}] 페이지 {pageIdx + 1}/{nTotPage}: 유효 로우 {resultInt.nResult}개");

                    Draw.Rectangle[,] rects = m_Context.MemInfo.RcptPage.DG오더_RelChildRects;

                    // 마지막 페이지의 경우 시작 인덱스 계산 (페이지가 2개 이상일 때만)
                    int remainder = nThisTotCount % InsungsInfo_File.접수등록Page_DG오더_dataRowCount;
                    int startIndex = (nTotPage > 1 && pageIdx == nTotPage - 1 && remainder != 0) ? InsungsInfo_File.접수등록Page_DG오더_dataRowCount - remainder : 0;

                    //Debug.WriteLine($"[{APP_NAME}] 페이지 {pageIdx + 1}/{nTotPage}: startIndex={startIndex}, remainder={remainder}, resultInt.nResult={resultInt.nResult}");
                    #endregion

                    #region 2. 로우별 처리
                    for (int i = startIndex, y = i + 2; i < resultInt.nResult; i++, y++)
                    {
                        #region 찾기
                        bool bInvertRgb = false;

                        // 첫 페이지 첫 로우면 선택 확인 및 처리
                        if (pageIdx == 0 && i == startIndex)
                        {
                            // bmpPage에서 직접 반전 여부 확인 (별도 캡처 불필요)
                            Draw.Rectangle rectVerify = rects[1, y];
                            bool isSelected = OfrService.IsInvertedSelection(bmpPage, rectVerify);

                            if (isSelected)
                            {
                                // 이미 선택됨 → RGB 반전 OFR
                                bInvertRgb = true;
                                //Debug.WriteLine($"[{APP_NAME}] 첫 로우 이미 선택됨 (RGB 반전 OFR)");
                            }
                            else
                            {
                                // 선택 안됨 → 클릭 후 재캡처
                                Debug.WriteLine($"[{APP_NAME}] 첫 로우 선택 안됨 → 클릭 후 재캡처");

                                Draw.Rectangle rcFirstRow = rects[3, y]; // 4번째 컬럼
                                Draw.Point ptClick = new Draw.Point(rcFirstRow.Left + 5, rcFirstRow.Top + 5);
                                await Std32Mouse_Post.MousePostAsync_ClickLeft_ptRel(m_Context.MemInfo.RcptPage.DG오더_hWnd, ptClick);
                                await Task.Delay(100, ctrl.Token);

                                // 재캡처
                                bmpPage?.Dispose();
                                bmpPage = OfrService.CaptureScreenRect_InWndHandle(m_Context.MemInfo.RcptPage.DG오더_hWnd);

                                if (bmpPage == null)
                                {
                                    Debug.WriteLine($"[{APP_NAME}] 첫 로우 클릭 후 재캡처 실패");
                                    return new StdResult_Status(StdResult.Fail, "첫 로우 클릭 후 재캡처 실패", $"{APP_NAME}/AutoAllocAsync_Region5_3_ReCapture");
                                }

                                bInvertRgb = true;
                                Debug.WriteLine($"[{APP_NAME}] 첫 로우 선택 후 재캡처 완료 (RGB 반전 OFR)");
                            }
                        }

                        //Debug.WriteLine($"[{APP_NAME}] 페이지 {pageIdx + 1}, y={y}, bInvertRgb={bInvertRgb}");

                        // 로우에서 주문번호 읽기
                        Draw.Rectangle rectSeqno = rects[InsungsAct_RcptRegPage.c_nCol주문번호, y];

                        // 주문번호 읽기 (숫자 - 단음소)
                        StdResult_String resultSeqno = await m_Context.RcptRegPageAct.GetRowSeqnoAsync(bmpPage, rectSeqno, bInvertRgb, ctrl);
                        if (string.IsNullOrEmpty(resultSeqno.strResult))
                        {
                            Debug.WriteLine($"[{APP_NAME}] 페이지 {pageIdx + 1}, y={y}, 주문번호 읽기 실패: {resultSeqno.sErr}");
                            return new StdResult_Status(StdResult.Fail, resultSeqno.sErr, resultSeqno.sPos);
                        }

                        string seqno = resultSeqno.strResult;

                        //주문번호로 listEtcGroup에서 아이템 찾기 (GetInsungSeqno 사용)
                        var foundItem = listEtcGroup.FirstOrDefault(item => GetInsungSeqno(item) == seqno);
                        if (foundItem == null) continue;

                        // 찾았으면 상태 읽기 (모든 케이스에서 필요)
                        Draw.Rectangle rectStatus = rects[InsungsAct_RcptRegPage.c_nCol상태, y];
                        StdResult_String resultStatus = await m_Context.RcptRegPageAct.GetRowStatusAsync(bmpPage, rectStatus, bInvertRgb, ctrl);
                        if (string.IsNullOrEmpty(resultStatus.strResult))
                        {
                            Debug.WriteLine($"[{APP_NAME}] 페이지 {pageIdx + 1}, y={y}, 상태 읽기 실패: {resultStatus.sErr}");
                            return new StdResult_Status(StdResult.Fail, resultStatus.sErr, resultStatus.sPos);
                        }

                        string status = resultStatus.strResult.Substring(0, 2); // 앞 2글자만 추출 ("접수[1]" → "접수")
                        Debug.WriteLine($"[{APP_NAME}] 페이지 {pageIdx + 1}, y={y}, status={status}");
                        #endregion

                        #region Race condition 방지: 큐에서 최신 StateFlag 확인
                        // AutoAllocAsync 시작 후 SignalR 업데이트가 발생할 수 있음
                        // 처리 직전에 큐에서 최신 상태를 확인하여 반영
                        var latestItem = ExternalAppController.QueueManager.FindLatestInQueue(queueName, foundItem.KeyCode);
                        if (latestItem != null)
                        {
                            foundItem.StateFlag = latestItem.StateFlag;
                            foundItem.NewOrder = latestItem.NewOrder;
                            Debug.WriteLine($"[{APP_NAME}] 최신 StateFlag 적용: KeyCode={foundItem.KeyCode}, StateFlag={foundItem.StateFlag}");
                        }
                        #endregion

                        #region StateFlag별 분기
                        CommonResult_AutoAllocProcess resultAuto;

                        // 비트 플래그 체크를 위해 if-else if 사용 (백업 로직과 동일)
                        if (foundItem.StateFlag == PostgService_Common_OrderState.NotChanged)
                        {
                            // Kai는 변화 없음 → Insung 상태 변경 확인
                            // TODO: StateTransitionRules 적용
                            var dgInfoNotChanged = new CommonResult_AutoAllocDatagrid(i, status, bInvertRgb, bmpPage);
                            resultAuto = await m_Context.RcptRegPageAct.CheckIsOrderAsync_InsungOrderManage(foundItem, dgInfoNotChanged, ctrl);
                        }
                        else if ((foundItem.StateFlag & PostgService_Common_OrderState.Existed_WithSeqno) != 0 ||
                            (foundItem.StateFlag & PostgService_Common_OrderState.Updated_Assume) != 0)
                        {
                            // Updated_Assume 플래그 포함 (Updated_Status, Updated_Etc 등) → Insung을 Kai에 맞춰 업데이트
                            var dgInfo = new CommonResult_AutoAllocDatagrid(i, status, bInvertRgb, bmpPage);
                            resultAuto = await m_Context.RcptRegPageAct.CheckIsOrderAsync_AssumeKaiUpdated(foundItem, dgInfo, ctrl);
                        }
                        //else if (foundItem.StateFlag == PostgService_Common_OrderState.CompletedExternal)
                        //{
                        //    // TODO: 외부 완료 처리 (취소 명령 실행)
                        //    // resultAuto = await m_Context.RcptRegPageAct.Command_CancelExternal(foundItem, ctrl);
                        //    resultAuto = CommonResult_AutoAllocProcess.SuccessAndComplete(); // 임시 (취소 성공 → 비적재)
                        //}
                        else
                        {
                            // 알 수 없는 StateFlag → 에러
                            Debug.WriteLine($"[{APP_NAME}] 알 수 없는 StateFlag: {foundItem.StateFlag}");
                            return new StdResult_Status(StdResult.Fail, $"알 수 없는 StateFlag: {foundItem.StateFlag}", $"{APP_NAME}/AutoAllocAsync_Region5_3_StateFlag");
                        }
                        #endregion

                        #region 통합 결과 처리
                        switch (resultAuto.ResultType)
                        {
                            case CEnum_AutoAllocProcessResult.SuccessAndReEnqueue:
                                // 성공 + 재적재 (계속 관리) - NotChanged 플래그로 재적재
                                Debug.WriteLine($"[{APP_NAME}] 처리 완료 (재적재): seqno={seqno}, 기존 StateFlag={foundItem.StateFlag} → NotChanged로 재적재");
                                Debug.WriteLine($"[{APP_NAME}] 재적재 전 foundItem 상태: RunStartTime={foundItem.RunStartTime?.ToString("HH:mm:ss") ?? "null"}, DriverPhone={foundItem.DriverPhone ?? "null"}");
                                ExternalAppController.QueueManager.ReEnqueue(foundItem, queueName, PostgService_Common_OrderState.NotChanged);
                                break;

                            case CEnum_AutoAllocProcessResult.SuccessAndComplete:
                                // 성공 + 비적재 (완료)
                                Debug.WriteLine($"[{APP_NAME}] 처리 완료 (비적재): seqno={seqno}, Message={resultAuto.sErr}");
                                break;

                            case CEnum_AutoAllocProcessResult.FailureAndRetry:
                                // 실패 + 재적재 (재시도)
                                Debug.WriteLine($"[{APP_NAME}] 처리 실패 (재적재): seqno={seqno}, Error={resultAuto.sErrNPos}");
                                ExternalAppController.QueueManager.ReEnqueue(foundItem, queueName, foundItem.StateFlag);
                                break;

                            case CEnum_AutoAllocProcessResult.FailureAndDiscard:
                                // 실패 + 비적재 (복구 불가능)
                                Debug.WriteLine($"[{APP_NAME}] 처리 실패 (비적재): seqno={seqno}, Error={resultAuto.sErrNPos}");
                                break;
                        }
                        #endregion

                        #region 정리작업
                        // **무조건 삭제** (모든 케이스) - KeyCode 기반으로 제거
                        listEtcGroup.RemoveAll(item => item.KeyCode == foundItem.KeyCode);

                        // **조기 탈출: listEtcGroup 비면 종료**
                        if (listEtcGroup.Count == 0)
                        {
                            //Debug.WriteLine($"[{APP_NAME}] listEtcGroup 모두 처리 완료 → 페이지 {pageIdx + 1}, 로우 {y}에서 for 루프 종료");
                            break; // for 로우 루프 탈출
                        }
                        #endregion
                    }
                    #endregion

                    #region 3. 완료작업
                    bmpPage?.Dispose();

                    // 조기 탈출: 모든 항목을 처리했으면 (로우 루프에서 이미 체크됨)
                    if (listEtcGroup.Count == 0)
                    {
                        //Debug.WriteLine($"[{APP_NAME}] listEtcGroup 모두 처리 완료 → 페이지 {pageIdx + 1}/{nTotPage}에서 페이지 루프 종료");
                        break; // 페이지 루프 탈출
                    }

                    if (pageIdx < nTotPage - 1)
                    {
                        // 다음 페이지로 이동
                        await Std32Mouse_Post.MousePostAsync_ClickLeft_ptRel(
                            m_Context.MemInfo.RcptPage.DG오더_hWnd수직스크롤, m_Context.FileInfo.접수등록Page_DG오더_ptClkRel페이지Down);
                        await Task.Delay(c_nWaitNormal, ctrl.Token);
                        Debug.WriteLine($"[{APP_NAME}] 페이지 {pageIdx + 1} -> {pageIdx + 2} 이동");
                    } 
                    #endregion
                }
                #endregion

                #region 5-4. 통합 결과 처리
                //Debug.WriteLine($"[{APP_NAME}] Region 5 완료");
                #endregion

                #region 5-5. 정리작업
                // 처리 못한 항목 (DG에서 찾지 못함 = 이미 배차/완료됨)
                if (listEtcGroup.Count > 0)
                {
                    Debug.WriteLine($"[{APP_NAME}] DG에서 찾지 못한 항목: {listEtcGroup.Count}건 - 상세 정보:");
                    for (int idx = 0; idx < listEtcGroup.Count && idx < 20; idx++)
                    {
                        var item = listEtcGroup[idx];
                        Debug.WriteLine($"[{APP_NAME}]   [{idx}] KeyCode={item.KeyCode}, StateFlag={item.StateFlag}, Seqno={GetInsungSeqno(item) ?? "(null)"}");
                    }

                    Debug.WriteLine($"[{APP_NAME}] DG에서 찾지 못한 항목: {listEtcGroup.Count}건 (배차/완료된 것으로 추정, 재적재 안 함)");
                    // 재적재 안 함 (버림)
                }
                listEtcGroup.Clear();
                #endregion
            }

            #endregion

            return new StdResult_Status(StdResult.Success);
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine($"[{APP_NAME}] AutoAllocAsync 취소됨");
            return new StdResult_Status(StdResult.Skip, "작업 취소됨", $"{APP_NAME}/AutoAllocAsync_Cancel");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{APP_NAME}] AutoAllocAsync 예외: {ex.Message}");
            return new StdResult_Status(StdResult.Fail, ex.Message, $"{APP_NAME}/AutoAllocAsync_999");
        }
        finally
        {
            // Bitmap 해제
            bmpPage?.Dispose();
        }
    }
    #endregion

    #region Helper Methods - File I/O
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
                        $"{APP_NAME}/ReadInfoFileAsync_01");
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
                        $"{APP_NAME}/ReadInfoFileAsync_02");
                }

                // Context의 FileInfo에 덮어씌우기
                m_Context.FileInfo = fileInfo;

                Debug.WriteLine($"[{APP_NAME}] FileInfo 파일 로드 완료: {sFilePath}");
                return null; // 성공
            }
            catch (Exception ex)
            {
                return new StdResult_Error(
                    $"[{APP_NAME}] FileInfo 파일 읽기 예외: {ex.Message}",
                    $"{APP_NAME}/ReadInfoFileAsync_99");
            }
        });
    }

    /// <summary>
    /// Context.FileInfo를 JSON 파일로 저장 (테스트/디버깅용)
    /// </summary>
    protected void WriteInfoToFile_AtFirst()
    {
        try
        {
            // 이미 Context.FileInfo가 초기화되어 있으므로 그대로 사용
            InsungsInfo_File info = m_Context.FileInfo;

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

            Debug.WriteLine($"[{APP_NAME}] FileInfo 파일 저장 완료: {sFilePath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{APP_NAME}] FileInfo 파일 저장 실패: {ex.Message}");
        }
    }
    #endregion

    #region 생성자
    /// <summary>
    /// 베이스 생성자: Context 및 Act 객체 초기화
    /// </summary>
    protected InsungAutoAllocBase()
    {
        Debug.WriteLine($"[{APP_NAME}] 생성자 호출: Id={GetStaticId()}, Use={GetStaticUse()} --------------------------------------------------------");

        // Context 생성
        m_Context = new InsungContext(APP_NAME, GetStaticId(), GetStaticPw());

        // AppAct 생성
        m_Context.AppAct = new InsungsAct_App(m_Context);

        // MainWndAct 생성
        m_Context.MainWndAct = new InsungsAct_MainWnd(m_Context);

        // RcptRegPageAct 생성
        m_Context.RcptRegPageAct = new InsungsAct_RcptRegPage(m_Context);

        Debug.WriteLine($"[{APP_NAME}] Context 생성 완료: AppName={m_Context.AppName}");
    }
    #endregion
}
#nullable restore
