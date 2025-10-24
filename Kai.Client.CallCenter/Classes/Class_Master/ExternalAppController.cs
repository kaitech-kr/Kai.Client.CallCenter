using System.Diagnostics;
using System.Linq;
using Kai.Common.StdDll_Common;
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
    // 정적 변수 - 자동배차 주문 리스트 (Master 모드 전용)
    public static List<AutoAlloc> listForInsung01 = new List<AutoAlloc>();
    public static List<AutoAlloc> listForInsung02 = new List<AutoAlloc>();
    // public static List<AutoAlloc> listForCargo24 = new List<AutoAlloc>();
    // public static List<AutoAlloc> listForOnecall = new List<AutoAlloc>();

    // 개별 참조 (필요시 직접 접근용)
    public NwInsung01 Insung01 { get; private set; }
    public NwInsung02 Insung02 { get; private set; }
    // public NwCargo24 Cargo24 { get; private set; }
    // public NwOnecall Onecall { get; private set; }

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
    public static AutoAllocQueueManager QueueManager { get; private set; } = new AutoAllocQueueManager();

    /// <summary>
    /// 자동배차 실행 중 여부
    /// </summary>
    public bool IsAutoAllocRunning => m_TaskAutoAlloc != null && !m_TaskAutoAlloc.IsCompleted;
    #endregion

    #region 생성자
    public ExternalAppController()
    {
        Debug.WriteLine("[ExternalAppController] 생성자 호출");
        // QueueManager는 static으로 자동 초기화됨
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// 외부 앱들 초기화
    /// </summary>
    public async Task<StdResult_Status> InitializeAsync()
    {
        try
        {
            Debug.WriteLine("[ExternalAppController] InitializeAsync 시작");

            // 1. 앱 인스턴스 생성 (s_Use가 true인 것만)
            if (NwInsung01.s_Use)
            {
                Debug.WriteLine($"[ExternalAppController] Insung01 생성: Id={NwInsung01.s_Id}");
                Insung01 = new NwInsung01();
                m_ListApps.Add(Insung01);
            }
            else
            {
                Debug.WriteLine("[ExternalAppController] Insung01 사용 안함 (s_Use=false)");
            }

            //if (NwInsung02.s_Use)
            //{
            //    Debug.WriteLine($"[ExternalAppController] Insung02 생성: Id={NwInsung02.s_Id}");
            //    Insung02 = new NwInsung02();
            //    m_ListApps.Add(Insung02);
            //}
            //else
            //{
            //    Debug.WriteLine("[ExternalAppController] Insung02 사용 안함 (s_Use=false)");
            //}

            // if (NwCargo24.s_Use)
            // {
            //     Cargo24 = new NwCargo24();
            //     m_ListApps.Add(Cargo24);
            // }
            // if (NwOnecall.s_Use)
            // {
            //     Onecall = new NwOnecall();
            //     m_ListApps.Add(Onecall);
            // }

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
    /// 주문을 4개 외부앱별로 분류하여 큐에 적재
    /// 참조: 주문_분류_로직_확정.md
    /// </summary>
    /// <param name="order">분류할 주문</param>
    /// <param name="isNewOrder">신규 주문 여부 (true=Created, false=Existed)</param>
    private void ClassifyAndEnqueueOrder(TbOrder order, bool isNewOrder)
    {
        // Step 1: 차량 타입 판단 (절대 기준)
        bool isMotorcycle = order.CarType == "오토";
        bool isSmallTruck = order.CarType == "트럭" &&
                            (order.CarWeight == "1t" || order.CarWeight == "1.4t");
        bool isLargeTruck = order.CarType == "트럭" &&
                            order.CarWeight != "1t" &&
                            order.CarWeight != "1.4t";

        Debug.WriteLine($"[분류] KeyCode={order.KeyCode}, CarType={order.CarType}, " +
                        $"CarWeight={order.CarWeight}, 오토={isMotorcycle}, " +
                        $"소형={isSmallTruck}, 대형={isLargeTruck}");

        // Step 2: 외부앱별 분배
        // 오토바이 또는 1.4톤 이하 트럭 → 인성1, 인성2
        if (isMotorcycle || isSmallTruck)
        {
            // 인성1: 인성2 신용업체 무조건 제외 (현금/신용 무관)
            // 이유: 결제 방법이 도중에 변경되어도 회계 일관성 유지
            if (order.CallCustFrom != StdConst_Network.INSUNG2)
            {
                EnqueueToApp(order, StdConst_Network.INSUNG1, isNewOrder);
            }

            // 인성2: 인성1 신용업체 무조건 제외 (현금/신용 무관)
            if (order.CallCustFrom != StdConst_Network.INSUNG1)
            {
                EnqueueToApp(order, StdConst_Network.INSUNG2, isNewOrder);
            }
        }

        // 1.4톤 이하 또는 초과 트럭 → 화물24시, 원콜
        if (isSmallTruck || isLargeTruck)
        {
            EnqueueToApp(order, StdConst_Network.CARGO24, isNewOrder);
            EnqueueToApp(order, StdConst_Network.ONECALL, isNewOrder);
        }
    }

    /// <summary>
    /// 주문을 특정 앱의 큐에 추가
    /// </summary>
    private void EnqueueToApp(TbOrder order, string networkName, bool isNewOrder)
    {
        // SeqNo 확인
        string seqNo = GetSeqNoByNetwork(order, networkName);
        bool hasSeqNo = !string.IsNullOrEmpty(seqNo);

        // StateFlag 결정
        PostgService_Common_OrderState stateFlag;
        if (isNewOrder)
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
        var autoAlloc = new AutoAlloc(stateFlag, order);
        QueueManager.Enqueue(autoAlloc, networkName);

        Debug.WriteLine($"  → {networkName} 큐 추가: SeqNo={seqNo ?? "(없음)"}, Flag={stateFlag}");
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

            // TbOrder에 EndDongBasic 속성이 없음 - TODO: 실제 속성명 확인 필요
            // if (oldOrder.EndDongBasic != newOrder.EndDongBasic)
            //     Debug.WriteLine($"  도착지 변경: {oldOrder.EndDongBasic} → {newOrder.EndDongBasic}");

            if (oldOrder.FeeBasic != newOrder.FeeBasic)
                Debug.WriteLine($"  요금 변경: {oldOrder.FeeBasic} → {newOrder.FeeBasic}");

            // TbOrder에 Status 또는 StatusFlag 속성이 없음 - TODO: 실제 속성명 확인 필요
            // if (oldOrder.StatusFlag != newOrder.StatusFlag)
            //     Debug.WriteLine($"  상태 변경: {oldOrder.StatusFlag} → {newOrder.StatusFlag}");

            if (oldOrder.CallCustFrom != newOrder.CallCustFrom)
                Debug.WriteLine($"  접수처 변경: {oldOrder.CallCustFrom} → {newOrder.CallCustFrom}");
        }

        Debug.WriteLine($"[ExternalAppController] =========================================");

        // 참조 공유로 인해 s_listTbOrderToday의 TbOrder 객체가 업데이트되면
        // 큐의 AutoAlloc.NewOrder도 같은 객체를 참조하므로 자동으로 반영됨!
        //
        // 다음 AutoAllocAsync() 루프에서 최신 데이터 사용됨
        // (할일 많으면 거의 즉시, 없으면 최대 5초 내)
    }

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
    /// 자동배차 무한 루프 (private)
    /// </summary>
    private async Task AutoAllocLoopAsync()
    {
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        const int nMinWorkingMiliSec = 5000; // 최소 5초

        Debug.WriteLine("[ExternalAppController] AutoAllocLoopAsync 시작");

        for (m_lAutoAllocCount = 1; ; m_lAutoAllocCount++)
        {
            try
            {
                // ✅ 원칙 1: 루프 시작 시 한 번만 체크
                await m_CtrlCancelToken.WaitIfPausedOrCancelledAsync();

                stopwatch.Restart();

                // ✅ 원칙 2: 리스트 활용 (확장 가능)
                foreach (var app in m_ListApps)
                {
                    try
                    {
                        var result = await app.AutoAllocAsync(m_lAutoAllocCount, m_CtrlCancelToken);

                        // ✅ 원칙 3: 에러 로깅
                        if (result.Result != StdResult.Success && result.Result != StdResult.Skip)
                        {
                            Debug.WriteLine($"[ExternalAppController] {app.AppName} AutoAlloc 실패: {result.sErrNPos}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ExternalAppController] {app.AppName} AutoAlloc 예외: {ex.Message}");
                        // 예외 발생해도 다음 앱 계속 진행
                    }
                }

                stopwatch.Stop();

                // Delay 보정 (최소 5초 유지)
                int nDelay = stopwatch.ElapsedMilliseconds < nMinWorkingMiliSec
                    ? nMinWorkingMiliSec - (int)stopwatch.ElapsedMilliseconds
                    : 0;

                if (nDelay > 0)
                {
                    // ✅ 원칙 4: Task.Delay에 Token 전달
                    await Task.Delay(nDelay, m_CtrlCancelToken.Token);
                }

                Debug.WriteLine($"[ExternalAppController] AutoAlloc [{m_lAutoAllocCount}] 완료 - Elapsed={stopwatch.ElapsedMilliseconds}ms, Delay={nDelay}ms");
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
    #endregion

    /// <summary>
    /// 리소스 정리
    /// </summary>
    public void Shutdown()
    {
        try
        {
            Debug.WriteLine("[ExternalAppController] Shutdown 시작");

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
}
#nullable restore
