using System.Diagnostics;

using Kai.Common.StdDll_Common;
using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgWnd;
using Kai.Common.NetDll_WpfCtrl.NetWnds;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Services;

using Kai.Client.CallCenter.Networks;
using Kai.Client.CallCenter.Classes;

using static Kai.Client.CallCenter.Classes.CommonVars;

namespace Kai.Client.CallCenter.Classes.Class_Master;

#nullable disable
// 외부 앱(인성1, 인성2, 화물24시, 원콜) 제어 컨트롤러
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
                SrGlobalClient.SrGlobalClient_ClosedEvent -= OnSignalRDisconnected;
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

    // 사용 중인 앱 리스트 (읽기 전용)
    public IReadOnlyList<IExternalApp> Apps => m_ListApps.AsReadOnly();

    // 자동배차 관련
    private CancelTokenControl m_CtrlCancelToken = new CancelTokenControl();
    private long m_lAutoAllocCount = 0;
    private Task m_TaskAutoAlloc = null;

    // 자동배차 큐 관리자 (Phase 1: Queue 기반) - Static으로 관리
    public static QueueController QueueManager { get; private set; } = new QueueController();

    // 자동배차 실행 중 여부
    public bool IsAutoAllocRunning => m_TaskAutoAlloc != null && !m_TaskAutoAlloc.IsCompleted;
    #endregion

    #region 기본
    public ExternalAppController()
    {
        Debug.WriteLine("[ExternalAppController] 생성자 호출");
        // QueueManager는 static으로 자동 초기화됨
    }

    // 리소스 정리
    public async Task ShutdownAsync()
    {
        try
        {
            Debug.WriteLine("[ExternalAppController] Shutdown 시퀀스 시작 (안전 종료 대기 15초)");

            // 1. 서버 이벤트 구독 해제 (중복 알림 방지)
            SrGlobalClient.SrGlobalClient_ClosedEvent -= OnSignalRDisconnected;

            // 2. 자동배차 중단 신호 전송
            if (m_CtrlCancelToken != null)
            {
                m_CtrlCancelToken.Cancel();
                Debug.WriteLine("[ExternalAppController] 자동배차 취소(Cancel) 신호 전송 완료");
            }

            // 3. 배차 Task 대기 (OCR 및 데이터 저장 완료 보장 위해 넉넉히 15초 대기)
            if (m_TaskAutoAlloc != null)
            {
                var timeoutTask = Task.Delay(15000); // 15초 타이머
                var completedTask = await Task.WhenAny(m_TaskAutoAlloc, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    // 🚨 타임아웃 발생 시 강한 경고 로그 (향후 슬랙 등 원격 알림 연동 포인트)
                    Debug.WriteLine("======================================================================");
                    Debug.WriteLine("[ExternalAppController] !!! CRITICAL WARNING !!!");
                    Debug.WriteLine("[ExternalAppController] 자동배차 작업이 15초 이내에 종료되지 않았습니다.");
                    Debug.WriteLine("[ExternalAppController] OCR 분석 또는 DB 저장이 지연되어 강제 종료될 수 있습니다.");
                    Debug.WriteLine("======================================================================");
                }
                else
                {
                    Debug.WriteLine("[ExternalAppController] 자동배차 Task 정상 종료 확인");
                }
                m_TaskAutoAlloc = null;
            }

            // 4. 모든 외부 앱 순차 종료 및 리소스 해제
            foreach (var app in m_ListApps)
            {
                try
                {
                    Debug.WriteLine($"[ExternalAppController] {app.AppName} 종료 및 Dispose 시도...");
                    app.Shutdown();
                    app.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ExternalAppController] {app.AppName} 종료 중 예외 (무시): {ex.Message}");
                }
            }

            m_ListApps.Clear();
            Debug.WriteLine("[ExternalAppController] 모든 리소스 정리 완료");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ExternalAppController] Shutdown 시퀀스 도중 치명적 오류: {ex.Message}");
        }
    }
    #endregion

    #region 초기화
    // 외부 앱들 초기화
    public async Task<StdResult_Status> InitializeAsync()
    {
        try
        {
            NetLoadingWnd.ShowLoading(s_MainWnd, "외부 앱(인성/원콜/24시) 통합 초기화 중입니다. 잠시만 기다려 주세요...");
            Debug.WriteLine("[ExternalAppController] InitializeAsync 시작 (인성1 테스트 모드)");

            // 1. 앱 인스턴스 생성 (인성1만 우선 적용)
            if (NwInsung01.s_Use)
            {
                Insung01 = new NwInsung01();
                m_ListApps.Add(Insung01);
                Debug.WriteLine($"[ExternalAppController] Insung01 인스턴스 생성 완료");
            }

            Debug.WriteLine($"[ExternalAppController] 생성된 앱 개수: {m_ListApps.Count}");

            // 2. 리스트의 모든 앱 초기화 (현재는 인성1만 포함됨)
            foreach (var app in m_ListApps)
            {
                Debug.WriteLine($"[ExternalAppController] {app.AppName} 초기화 시퀀스 시작...");
                var result = await app.InitializeAsync();
                
                if (result.Result != StdResult.Success)
                {
                    string tracePos = $"{result.sErrNPos} -> ExternalAppController/InitializeAsync";
                    Debug.WriteLine($"[ExternalAppController] {app.AppName} 초기화 최종 실패: {tracePos}");
                    return result; // 원래 결과(Fail 또는 Skip)를 그대로 반환
                }
                
                Debug.WriteLine($"[ExternalAppController] {app.AppName} 초기화 성공");
            }

            // 3. SignalR 연결 끊김 이벤트 구독
            SrGlobalClient.SrGlobalClient_ClosedEvent += OnSignalRDisconnected;
            Debug.WriteLine("[ExternalAppController] SignalR 연결 끊김 이벤트 구독 완료");

            Debug.WriteLine("[ExternalAppController] InitializeAsync 전체 완료");
            return new StdResult_Status(StdResult.Success);
        }
        catch (OperationCanceledException)
        {
            string errPos = "ExternalAppController/InitializeAsync_Cancel";
            Debug.WriteLine($"[ExternalAppController] {errPos}");
            return new StdResult_Status(StdResult.Skip, "사용자의 요청으로 종료합니다...", errPos);
        }
        catch (Exception ex)
        {
            string errPos = "ExternalAppController/InitializeAsync_Exception";
            Debug.WriteLine($"[ExternalAppController] {errPos}: {ex.Message}");
            return new StdResult_Status(StdResult.Fail, ex.Message, errPos);
        }
        finally
        {
            NetLoadingWnd.HideLoading();
        }
    }

    // 새로 생성된 주문 추가 (자동배차 대상으로 등록)
    // SignalR OnOrderCreated에서 호출됨
    public void AddNewOrder(TbOrder order)
    {
        // ...
    }

    // 주문 업데이트 알림 (자동배차 시스템에 변경 사항 전달)
    // SignalR OnOrderUpdated에서 호출됨
    public void UpdateOrder(PostgService_Common_OrderState changedFlag, TbOrder newOrder, TbOrder oldOrder, int seqNo)
    {
        // ...
    }
    #endregion

    #region 자동배차 실행
    // 자동배차 무한 루프 (private)
    private async Task AutoAllocLoopAsync()
    {
        //    System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        //    const int nMinWorkingMiliSec = 5000; // 최소 ~초

        //    Debug.WriteLine("[ExternalAppController] AutoAllocLoopAsync 시작");

        //    for (m_lAutoAllocCount = 1; ; m_lAutoAllocCount++)
        //    {
        //        try
        //        {
        //            stopwatch.Restart();

        //            // ✅ 원칙 2: 리스트 활용 (확장 가능)
        //            foreach (var app in m_ListApps)
        //            {
        //                // ✅ 원칙 1: 각 앱 처리 전 Cancel/Pause 체크
        //                await m_CtrlCancelToken.WaitIfPausedOrCancelledAsync();

        //                try
        //                {
        //                    var result = await app.AutoAllocAsync(m_lAutoAllocCount, m_CtrlCancelToken);

        //                    // ✅ 원칙 3: 결과 처리
        //                    switch (result.Result)
        //                    {
        //                        case StdResult.Success:
        //                            // 성공 - 계속 진행
        //                            break;

        //                        case StdResult.Skip:
        //                            // 스킵 - 계속 진행
        //                            break;

        //                        case StdResult.Retry:
        //                            // 재시도 - 로그만 출력하고 계속
        //                            Debug.WriteLine($"[ExternalAppController] {app.AppName} AutoAlloc 재시도 필요: {result.sErrNPos}");
        //                            break;

        //                        case StdResult.Fail:
        //                            // 실패 - 에러 메시지 출력 후 루프 탈출
        //                            ErrMsgBox($"[ExternalAppController] {app.AppName} AutoAlloc 실패 - 루프 중단: {result.sErrNPos}");
        //                            return;

        //                        default:
        //                            ErrMsgBox($"[ExternalAppController] {app.AppName} 알 수 없는 결과: {result.Result}");
        //                            break;
        //                    }
        //                }
        //                catch (Exception ex)
        //                {
        //                    ErrMsgBox($"[ExternalAppController] {app.AppName} AutoAlloc 예외: {ex.Message}");
        //                    // 예외 발생해도 다음 앱 계속 진행
        //                }
        //            }

        //            stopwatch.Stop();

        //            // Delay 보정 (최소 5초 유지)
        //            int nDelay = stopwatch.ElapsedMilliseconds < nMinWorkingMiliSec ? nMinWorkingMiliSec - (int)stopwatch.ElapsedMilliseconds : 0;

        //            if (nDelay > 0)
        //            {
        //                // ✅ 원칙 4: Task.Delay에 Token 전달
        //                await Task.Delay(nDelay, m_CtrlCancelToken.Token);
        //            }

        //            Debug.WriteLine($"-----------[ExternalAppController] AutoAlloc [{m_lAutoAllocCount}] 완료 - Elapsed={stopwatch.ElapsedMilliseconds}ms, Delay={nDelay}ms");
        //        }
        //        catch (OperationCanceledException)
        //        {
        //            Debug.WriteLine("[ExternalAppController] AutoAllocLoopAsync 취소됨");
        //            return;
        //        }
        //        catch (Exception ex)
        //        {
        //            Debug.WriteLine($"[ExternalAppController] AutoAllocLoopAsync 예외: {ex.Message}");
        //            // 예외 발생해도 루프 계속 (로깅만 하고 진행)
        //        }
        //    }
    }

    // 기존 주문 목록 로드 (자동배차 대상으로 등록) 4개 외부앱별로 분류하여 큐에 적재
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

    #endregion

    #region 큐 콘트롤
    // 주문이 속할 큐 목록 반환 (분류 로직 기반)
    private List<string> GetTargetQueues(TbOrder order)
    {
        var queues = new List<string>();

        // 차량 타입 판단 (Enum 기반으로 변경)
        bool isMotorcycle = order.CarWeightCode == (int)CarWts.Motorcycle;
        bool isFlex = order.CarTypeCode == (int)CarTypes.Flex;
        bool isLargeTruck = order.CarWeightCode > (int)CarWts.W1_4;

        bool isForInsung = !isLargeTruck;
        bool isForCargo24Onecall = !isMotorcycle && !isFlex;

        // 인성1, 인성2
        if (isForInsung)
        {
            if (order.CallCustFrom != StdConst_Network.INSUNG2)
                queues.Add(StdConst_Network.INSUNG1);

            if (order.CallCustFrom != StdConst_Network.INSUNG1)
                queues.Add(StdConst_Network.INSUNG2);
        }

        // 화물24시, 원콜
        if (isForCargo24Onecall)
        {
            queues.Add(StdConst_Network.CARGO24);
            queues.Add(StdConst_Network.ONECALL);
        }

        return queues;
    }

    // 주문을 특정 앱의 큐에 추가
    private void EnqueueToApp(TbOrder order, string networkName, PostgService_Common_OrderState? overrideFlag = null, bool isNewOrder = false)
    {
        // SeqNo 확인
        string seqNo = QueueManager.GetSeqNoByNetwork(order, networkName);
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

    // 주문을 4개 외부앱별로 분류하여 큐에 추가
    private void ClassifyAndEnqueueOrder(TbOrder order, PostgService_Common_OrderState? stateFlag = null, bool isNewOrder = false)
    {
        // ...
    }
    #endregion

    #region 자동배차 제어용 함수
    // 자동배차 시작 (백그라운드 태스크)
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

    // 자동배차 일시정지
    public void PauseAutoAlloc()
    {
        Debug.WriteLine("[ExternalAppController] 자동배차 일시정지");
        m_CtrlCancelToken.Pause();
    }

    // 자동배차 재개
    public void ResumeAutoAlloc()
    {
        Debug.WriteLine("[ExternalAppController] 자동배차 재개");
        m_CtrlCancelToken.Resume();
    }

    // 자동배차 중지
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
    #endregion

    #region SignalR 연결 끊김 처리
    // SignalR 연결 끊김 시 자동배차 일시정지
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
