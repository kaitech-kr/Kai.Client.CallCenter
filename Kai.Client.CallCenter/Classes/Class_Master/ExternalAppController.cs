using System.Diagnostics;


using Kai.Common.StdDll_Common;
using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgWnd;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Services;

using Kai.Client.CallCenter.Networks;
using Kai.Client.CallCenter.Classes;

namespace Kai.Client.CallCenter.Classes.Class_Master;

#nullable disable
/// <summary>
/// 외부 앱(인성1, 인성2, 화물24시, 원콜) 제어 컨트롤러
/// </summary>
public class ExternalAppController : IDisposable
{
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

    #region Variables
    // 개별 참조 (필요시 직접 접근용)
    public NwInsung01 Insung01 { get; private set; }
    public NwInsung02 Insung02 { get; private set; }
    public NwCargo24 Cargo24 { get; private set; }
    public NwOnecall Onecall { get; private set; }

    // 리스트로 관리 (반복 처리용)
    private List<IExternalApp> m_ListApps = new List<IExternalApp>();

    /// <summary>
    /// 사용 중인 앱 리스트 (읽기 전용)
    /// </summary>
    public IReadOnlyList<IExternalApp> Apps => m_ListApps.AsReadOnly();

    // 자동배차 관련
    private CancelTokenControl m_CtrlCancelToken = new CancelTokenControl();
    private long m_lAutoAllocCount = 0;
    private Task m_TaskAutoAlloc = null;

    /// <summary>
    /// 자동배차 큐 관리자 (Phase 1: Queue 기반) - Static으로 관리
    /// </summary>
    public static QueueController QueueManager { get; private set; } = new QueueController();

    /// <summary>
    /// 자동배차 실행 중 여부
    /// </summary>
    public bool IsAutoAllocRunning => m_TaskAutoAlloc != null && !m_TaskAutoAlloc.IsCompleted;
    #endregion

    #region 기본
    public ExternalAppController()
    {
        Debug.WriteLine("[ExternalAppController] 생성자 호출");
        // QueueManager는 static으로 자동 초기화됨
    }

    /// <summary>
    /// 리소스 정리
    /// </summary>
    public async Task ShutdownAsync()
    {
        try
        {
            Debug.WriteLine("[ExternalAppController] Shutdown 시작");

            // SignalR 연결 끊김 이벤트 구독 해제
            SrGlobalClient.SrGlobalClient_ClosedEvent -= OnSignalRDisconnected;
            Debug.WriteLine("[ExternalAppController] SignalR 연결 끊김 이벤트 구독 해제 완료");

            // AutoAlloc 루프 중단
            if (m_CtrlCancelToken != null)
            {
                m_CtrlCancelToken.Cancel();
            }

            // Task 완료 대기
            if (m_TaskAutoAlloc != null)
            {
                try
                {
                    await m_TaskAutoAlloc;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ExternalAppController] AutoAlloc Task 대기 중 예외 (무시): {ex.Message}");
                }
                m_TaskAutoAlloc = null;
            }

            // 리스트의 모든 앱 종료
            foreach (var app in m_ListApps)
            {
                try
                {
                    Debug.WriteLine($"[ExternalAppController] {app.AppName} 종료 중...");
                    app.Shutdown();
                    app.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ExternalAppController] {app.AppName} 종료 실패 (무시): {ex.Message}");
                }
            }

            m_ListApps.Clear();

            Debug.WriteLine("[ExternalAppController] Shutdown 완료");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ExternalAppController] Shutdown 실패: {ex.Message}");
        }
    }
    #endregion

    #region 초기화
    /// <summary>
    /// 외부 앱들 초기화
    /// </summary>
    public async Task<StdResult_Status> InitializeAsync()
    {
        try
        {
            Debug.WriteLine("[ExternalAppController] InitializeAsync 시작");

            //// 1. 앱 인스턴스 생성 (s_Use가 true인 것만) - 주석처리하면 자동배차도 안한답니다.
            //if (NwInsung01.s_Use)
            //{
            //    //Debug.WriteLine($"[ExternalAppController] Insung01 생성: Id={NwInsung01.s_Id}");
            //    Insung01 = new NwInsung01();
            //    m_ListApps.Add(Insung01);
            //}
            //else
            //{
            //    Debug.WriteLine("[ExternalAppController] Insung01 사용 안함 (s_Use=false)");
            //}

            //if (NwInsung02.s_Use)
            //{
            //    //Debug.WriteLine($"[ExternalAppController] Insung02 생성: Id={NwInsung02.s_Id}");
            //    Insung02 = new NwInsung02();
            //    m_ListApps.Add(Insung02);
            //}
            //else
            //{
            //    Debug.WriteLine("[ExternalAppController] Insung02 사용 안함 (s_Use=false)");
            //}

            //if (NwCargo24.s_Use)
            //{
            //    //Debug.WriteLine($"[ExternalAppController] Cargo24 생성: Id={NwCargo24.s_Id}");
            //    Cargo24 = new NwCargo24();
            //    m_ListApps.Add(Cargo24);
            //}
            //else
            //{
            //    Debug.WriteLine("[ExternalAppController] Cargo24 사용 안함 (s_Use=false)");
            //}

            if (NwOnecall.s_Use)
            {
                //Debug.WriteLine($"[ExternalAppController] Onecall 생성: Id={NwOnecall.s_Id}");
                Onecall = new NwOnecall();
                m_ListApps.Add(Onecall);
            }
            else
            {
                Debug.WriteLine("[ExternalAppController] Onecall 사용 안함 (s_Use=false)");
            }

            Debug.WriteLine($"[ExternalAppController] 생성된 앱 개수: {m_ListApps.Count}");

            // 2. 리스트의 모든 앱 초기화
            foreach (var app in m_ListApps)
            {
                Debug.WriteLine($"[ExternalAppController] {app.AppName} 초기화 중...");
                var result = await app.InitializeAsync();
                if (result.Result != StdResult.Success)
                {
                    Debug.WriteLine($"[ExternalAppController] {app.AppName} 초기화 실패: {result.sErrNPos}");
                    return new StdResult_Status(StdResult.Fail, $"{app.AppName} 초기화 실패", "ExternalAppController/InitializeAsync");
                }
            }

            // 3. SignalR 연결 끊김 이벤트 구독
            SrGlobalClient.SrGlobalClient_ClosedEvent += OnSignalRDisconnected;
            Debug.WriteLine("[ExternalAppController] SignalR 연결 끊김 이벤트 구독 완료");

            Debug.WriteLine("[ExternalAppController] InitializeAsync 완료");
            return new StdResult_Status(StdResult.Success);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ExternalAppController] InitializeAsync 실패: {ex.Message}");
            return new StdResult_Status(StdResult.Fail, ex.Message, "ExternalAppController/InitializeAsync");
        }
    }

    /// <summary>
    /// 새로 생성된 주문 추가 (자동배차 대상으로 등록)
    /// SignalR OnOrderCreated에서 호출
    /// </summary>
    /// <param name="order">새로 생성된 주문</param>
    public void AddNewOrder(TbOrder order)
    {
        if (order == null)
        {
            Debug.WriteLine("[ExternalAppController] 추가할 주문이 null입니다.");
            return;
        }

        Debug.WriteLine($"[ExternalAppController] 새 주문 추가: KeyCode={order.KeyCode}");

        // 4개 외부앱별로 분류하여 큐에 적재 (Created 상태)
        ClassifyAndEnqueueOrder(order, isNewOrder: true);

        // 큐 상태 출력
        QueueManager.PrintQueueStatus();
    }

    /// <summary>
    /// 주문 업데이트 알림 (자동배차 시스템에 변경 사항 전달)
    /// SignalR OnOrderUpdated에서 호출
    /// </summary>
    /// <param name="changedFlag">변경 플래그</param>
    /// <param name="newOrder">새로운 주문 정보</param>
    /// <param name="oldOrder">이전 주문 정보</param>
    /// <param name="seqNo">시퀀스 번호</param>
    public void UpdateOrder(PostgService_Common_OrderState changedFlag, TbOrder newOrder, TbOrder oldOrder, int seqNo)
    {
        if (newOrder == null)
        {
            Debug.WriteLine("[ExternalAppController] 업데이트할 주문이 null입니다.");
            return;
        }

        Debug.WriteLine($"[ExternalAppController] ===== 주문 업데이트 =====");
        Debug.WriteLine($"  KeyCode: {newOrder.KeyCode}");
        Debug.WriteLine($"  ChangedFlag: {changedFlag}");
        Debug.WriteLine($"  SeqNo: {seqNo}");

        // 변경 내용 상세 로깅
        if (oldOrder != null)
        {
            if (oldOrder.StartDongBasic != newOrder.StartDongBasic)
                Debug.WriteLine($"  출발지 변경: {oldOrder.StartDongBasic} → {newOrder.StartDongBasic}");

            if (oldOrder.FeeBasic != newOrder.FeeBasic)
                Debug.WriteLine($"  요금 변경: {oldOrder.FeeBasic} → {newOrder.FeeBasic}");

            if (oldOrder.CallCustFrom != newOrder.CallCustFrom)
                Debug.WriteLine($"  접수처 변경: {oldOrder.CallCustFrom} → {newOrder.CallCustFrom}");
        }

        Debug.WriteLine($"[ExternalAppController] =========================================");

        // ✅ 모든 큐에서 주문 업데이트 또는 제거 (인스턴스 재사용)
        // - 분류 규칙에 맞으면: 기존 AutoAllocModel의 NewOrder, StateFlag만 업데이트
        // - 분류 규칙에 안 맞으면: 큐에서 제거
        // - 기존 인스턴스 재사용으로 RunStartTime, LastDriverNo 등 유지됨
        QueueManager.UpdateOrRemoveInQueues(newOrder.KeyCode, newOrder, changedFlag);
    }

    /// <summary>
    /// 네트워크별 SeqNo 필드 가져오기
    /// </summary>
    private string GetSeqNoByNetwork(TbOrder order, string networkName)
    {
        return networkName switch
        {
            StdConst_Network.INSUNG1 => order.Insung1,
            StdConst_Network.INSUNG2 => order.Insung2,
            StdConst_Network.CARGO24 => order.Cargo24,
            StdConst_Network.ONECALL => order.Onecall,
            _ => null
        };
    }


    #endregion

    #region 자동배차
    /// <summary>
    /// 자동배차 무한 루프 (private)
    /// </summary>
    private async Task AutoAllocLoopAsync()
    {
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        const int nMinWorkingMiliSec = 5000; // 최소 ~초

        Debug.WriteLine("[ExternalAppController] AutoAllocLoopAsync 시작");

        for (m_lAutoAllocCount = 1; ; m_lAutoAllocCount++)
        {
            try
            {
                stopwatch.Restart();

                // ✅ 원칙 2: 리스트 활용 (확장 가능)
                foreach (var app in m_ListApps)
                {
                    // ✅ 원칙 1: 각 앱 처리 전 Cancel/Pause 체크
                    await m_CtrlCancelToken.WaitIfPausedOrCancelledAsync();

                    try
                    {
                        var result = await app.AutoAllocAsync(m_lAutoAllocCount, m_CtrlCancelToken);

                        // ✅ 원칙 3: 결과 처리
                        switch (result.Result)
                        {
                            case StdResult.Success:
                                // 성공 - 계속 진행
                                break;

                            case StdResult.Skip:
                                // 스킵 - 계속 진행
                                break;

                            case StdResult.Retry:
                                // 재시도 - 로그만 출력하고 계속
                                Debug.WriteLine($"[ExternalAppController] {app.AppName} AutoAlloc 재시도 필요: {result.sErrNPos}");
                                break;

                            case StdResult.Fail:
                                // 실패 - 에러 메시지 출력 후 루프 탈출
                                ErrMsgBox($"[ExternalAppController] {app.AppName} AutoAlloc 실패 - 루프 중단: {result.sErrNPos}");
                                return;

                            default:
                                ErrMsgBox($"[ExternalAppController] {app.AppName} 알 수 없는 결과: {result.Result}");
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrMsgBox($"[ExternalAppController] {app.AppName} AutoAlloc 예외: {ex.Message}");
                        // 예외 발생해도 다음 앱 계속 진행
                    }
                }

                stopwatch.Stop();

                // Delay 보정 (최소 5초 유지)
                int nDelay = stopwatch.ElapsedMilliseconds < nMinWorkingMiliSec ? nMinWorkingMiliSec - (int)stopwatch.ElapsedMilliseconds : 0;

                if (nDelay > 0)
                {
                    // ✅ 원칙 4: Task.Delay에 Token 전달
                    await Task.Delay(nDelay, m_CtrlCancelToken.Token);
                }

                Debug.WriteLine($"-----------[ExternalAppController] AutoAlloc [{m_lAutoAllocCount}] 완료 - Elapsed={stopwatch.ElapsedMilliseconds}ms, Delay={nDelay}ms");
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("[ExternalAppController] AutoAllocLoopAsync 취소됨");
                return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ExternalAppController] AutoAllocLoopAsync 예외: {ex.Message}");
                // 예외 발생해도 루프 계속 (로깅만 하고 진행)
            }
        }
    }

    /// <summary>
    /// 자동배차 중지
    /// </summary>
    public async Task StopAutoAllocAsync()
    {
        Debug.WriteLine("[ExternalAppController] 자동배차 중지 요청");
        m_CtrlCancelToken.Cancel();

        if (m_TaskAutoAlloc != null)
        {
            try
            {
                await m_TaskAutoAlloc;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("[ExternalAppController] 자동배차 정상 취소됨");
            }
        }

        m_TaskAutoAlloc = null;
        Debug.WriteLine("[ExternalAppController] 자동배차 중지 완료");
    }

    /// <summary>
    /// 기존 주문 목록 로드 (자동배차 대상으로 등록)
    /// 4개 외부앱별로 분류하여 큐에 적재
    /// </summary>
    /// <param name="orders">기존 주문 목록</param>
    public void LoadExistingOrders(List<TbOrder> orders)
    {
        if (orders == null || orders.Count == 0)
        {
            Debug.WriteLine("[ExternalAppController] 로드할 기존 주문이 없습니다.");
            return;
        }

        Debug.WriteLine($"[ExternalAppController] 기존 주문 {orders.Count}개 로드 시작");

        // 각 주문을 4개 외부앱별로 분류
        foreach (var order in orders)
        {
            ClassifyAndEnqueueOrder(order, isNewOrder: false);
        }

        // 큐 상태 출력
        QueueManager.PrintQueueStatus();
    }

    /// <summary>
    /// 주문을 4개 외부앱별로 분류하여 큐에 추가
    /// 참조: 주문_분류_로직_확정.md
    /// </summary>
    /// <param name="order">분류할 주문</param>
    /// <param name="stateFlag">StateFlag (지정 시 직접 사용, null이면 isNewOrder 기반으로 자동 결정)</param>
    /// <param name="isNewOrder">신규 주문 여부 (stateFlag가 null일 때만 사용)</param>
    private void ClassifyAndEnqueueOrder(TbOrder order, PostgService_Common_OrderState? stateFlag = null, bool isNewOrder = false)
    {
        // Step 1: 차량 타입 판단 (제외 로직)
        bool isMotorcycle = order.CarType == "오토";
        bool isFlex = order.CarType == "플렉스";
        bool isLargeTruck = order.CarType == "트럭" && order.CarWeight != "1t" && order.CarWeight != "1.4t";

        // 인성1, 인성2: 대형트럭만 제외 (오토, 플렉스, 다마, 라보, 밴, 트럭1t, 1.4t 모두 포함)
        bool isForInsung = !isLargeTruck;

        // 화물24시, 원콜: 오토, 플렉스만 제외 (다마, 라보, 밴, 트럭 모두 포함)
        bool isForCargo24Onecall = !isMotorcycle && !isFlex;

        Debug.WriteLine($"[분류] KeyCode={order.KeyCode}, CarType={order.CarType}, " +
                        $"CarWeight={order.CarWeight}, 인성={isForInsung}, 화물/원콜={isForCargo24Onecall}");

        // Step 2: 외부앱별 분배
        // 인성1, 인성2: 오토, 플렉스, 다마, 라보, 밴, 트럭(~1.4t)
        if (isForInsung)
        {
            // 인성1리스트 대상 - 의뢰자가 인성2신용업체가 아닌 퀵오더
            // TODO: 향후 FeeType을 CustTradeType(업체 거래타입)으로 변경 예정
            if (!(order.CallCustFrom == StdConst_Network.INSUNG2 && order.FeeType == "신용"))
            {
                EnqueueToApp(order, StdConst_Network.INSUNG1, stateFlag, isNewOrder);
            }

            // 인성2리스트 대상 - 의뢰자가 인성1신용업체가 아닌 퀵오더
            // TODO: 향후 FeeType을 CustTradeType(업체 거래타입)으로 변경 예정
            if (!(order.CallCustFrom == StdConst_Network.INSUNG1 && order.FeeType == "신용"))
            {
                EnqueueToApp(order, StdConst_Network.INSUNG2, stateFlag, isNewOrder);
            }
        }

        // 화물24시, 원콜: 오토, 플렉스 제외한 모든 차량
        if (isForCargo24Onecall)
        {
            EnqueueToApp(order, StdConst_Network.CARGO24, stateFlag, isNewOrder);
            EnqueueToApp(order, StdConst_Network.ONECALL, stateFlag, isNewOrder);
        }
    }

    /// <summary>
    /// 주문을 특정 앱의 큐에 추가
    /// </summary>
    /// <param name="order">추가할 주문</param>
    /// <param name="networkName">네트워크 이름</param>
    /// <param name="overrideFlag">StateFlag 직접 지정 (null이면 isNewOrder 기반으로 자동 결정)</param>
    /// <param name="isNewOrder">신규 주문 여부 (overrideFlag가 null일 때만 사용)</param>
    private void EnqueueToApp(TbOrder order, string networkName, PostgService_Common_OrderState? overrideFlag = null, bool isNewOrder = false)
    {
        // SeqNo 확인
        string seqNo = GetSeqNoByNetwork(order, networkName);
        bool hasSeqNo = !string.IsNullOrEmpty(seqNo);

        // StateFlag 결정
        PostgService_Common_OrderState stateFlag;
        if (overrideFlag.HasValue)
        {
            // 직접 지정된 Flag 사용
            stateFlag = overrideFlag.Value;
        }
        else if (isNewOrder)
        {
            stateFlag = PostgService_Common_OrderState.Created;
        }
        else
        {
            stateFlag = hasSeqNo
                ? PostgService_Common_OrderState.Existed_WithSeqno
                : PostgService_Common_OrderState.Existed_NonSeqno;
        }

        // AutoAlloc 생성 및 큐에 추가
        var autoAlloc = new AutoAllocModel(stateFlag, order);
        QueueManager.Enqueue(autoAlloc, networkName);

        Debug.WriteLine($"  → {networkName} 큐 추가: SeqNo={seqNo ?? "(없음)"}, Flag={stateFlag}");
    }

    /// <summary>
    /// 주문이 속할 큐 목록 반환 (분류 로직 기반)
    /// </summary>
    //private List<string> GetTargetQueues(TbOrder order)
    //{
    //    var queues = new List<string>();

    //    // 차량 타입 판단
    //    bool isMotorcycle = order.CarType == "오토";
    //    bool isFlex = order.CarType == "플렉스";
    //    bool isLargeTruck = order.CarType == "트럭" && order.CarWeight != "1t" && order.CarWeight != "1.4t";

    //    bool isForInsung = !isLargeTruck;
    //    bool isForCargo24Onecall = !isMotorcycle && !isFlex;

    //    // 인성1, 인성2
    //    if (isForInsung)
    //    {
    //        if (order.CallCustFrom != StdConst_Network.INSUNG2)
    //            queues.Add(StdConst_Network.INSUNG1);

    //        if (order.CallCustFrom != StdConst_Network.INSUNG1)
    //            queues.Add(StdConst_Network.INSUNG2);
    //    }

    //    // 화물24시, 원콜
    //    if (isForCargo24Onecall)
    //    {
    //        queues.Add(StdConst_Network.CARGO24);
    //        queues.Add(StdConst_Network.ONECALL);
    //    }

    //    return queues;
    //}

    #endregion

    #region 자동배차 제어
    /// <summary>
    /// 자동배차 시작 (백그라운드 태스크)
    /// </summary>
    public void StartAutoAlloc()
    {
        if (IsAutoAllocRunning)
        {
            Debug.WriteLine("[ExternalAppController] 자동배차가 이미 실행 중입니다.");
            return;
        }

        Debug.WriteLine("[ExternalAppController] 자동배차 시작");
        m_CtrlCancelToken = new CancelTokenControl();
        m_TaskAutoAlloc = Task.Run(() => AutoAllocLoopAsync());
    }

    /// <summary>
    /// 자동배차 일시정지
    /// </summary>
    public void PauseAutoAlloc()
    {
        Debug.WriteLine("[ExternalAppController] 자동배차 일시정지");
        m_CtrlCancelToken.Pause();
    }

    /// <summary>
    /// 자동배차 재개
    /// </summary>
    public void ResumeAutoAlloc()
    {
        Debug.WriteLine("[ExternalAppController] 자동배차 재개");
        m_CtrlCancelToken.Resume();
    }
    #endregion

    #region SignalR 연결 끊김 처리
    /// <summary>
    /// SignalR 연결 끊김 시 자동배차 일시정지
    /// </summary>
    private void OnSignalRDisconnected(object sender, Common.StdDll_Common.StdDelegate.ExceptionEventArgs e)
    {
        Debug.WriteLine($"[ExternalAppController] SignalR 연결 끊김 감지: {e.e?.Message}");
        Debug.WriteLine("[ExternalAppController] 자동배차를 일시정지(Pause) 상태로 전환합니다.");

        // 자동배차 일시정지
        if (m_CtrlCancelToken != null)
        {
            m_CtrlCancelToken.Pause();
            Debug.WriteLine("[ExternalAppController] 자동배차 Pause 완료");
        }

        // 사용자 알림 (메시지박스)
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            ErrMsgBox($"SignalR 서버 연결이 끊겼습니다.\n\n자동배차를 일시정지합니다.\n\n에러: {e.e?.Message ?? "알 수 없는 오류"}");
        });
    }
    #endregion
}
#nullable restore
