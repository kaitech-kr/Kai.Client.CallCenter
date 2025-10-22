using System.Diagnostics;
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
    /// 자동배차 실행 중 여부
    /// </summary>
    public bool IsAutoAllocRunning => m_TaskAutoAlloc != null && !m_TaskAutoAlloc.IsCompleted;
    #endregion

    #region 생성자
    public ExternalAppController()
    {
        Debug.WriteLine("[ExternalAppController] 생성자 호출");
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
    /// </summary>
    /// <param name="orders">기존 주문 목록</param>
    public void LoadExistingOrders(List<TbOrder> orders)
    {
        if (orders == null || orders.Count == 0)
        {
            Debug.WriteLine("[ExternalAppController] 로드할 기존 주문이 없습니다.");
            return;
        }

        Debug.WriteLine($"[ExternalAppController] 기존 주문 {orders.Count}개 로드");

        // TODO: 필요 시 각 앱별로 주문을 분류하여 내부 리스트에 추가
        // 예: Insung01, Insung02, Cargo24, Onecall 등에 맞는 주문 분류
        // 현재는 로깅만 수행하고, 자동배차 루프에서 처리
    }

    /// <summary>
    /// 새로 생성된 주문 추가 (자동배차 대상으로 등록)
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

        // TODO: 필요 시 각 앱별로 주문을 분류하여 내부 리스트에 추가
        // 예: 주문 상태나 기타 조건에 따라 Insung01, Insung02 등에 할당
        // 현재는 로깅만 수행하고, 자동배차 루프에서 처리
    }

    /// <summary>
    /// 주문 업데이트 알림 (자동배차 시스템에 변경 사항 전달)
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

        Debug.WriteLine($"[ExternalAppController] 주문 업데이트: KeyCode={newOrder.KeyCode}, ChangedFlag={changedFlag}");

        // TODO: 필요 시 각 앱별로 주문 업데이트 처리
        // 예: 주문 상태가 변경되었을 때 자동배차 리스트에서 제거/추가
        // 현재는 로깅만 수행하고, 자동배차 루프에서 처리
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
