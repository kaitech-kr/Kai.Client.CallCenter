using System.Diagnostics;
using Kai.Common.StdDll_Common;
using Kai.Client.CallCenter.Networks;
using Kai.Client.CallCenter.Class_Common;

namespace Kai.Client.CallCenter.Class_Master;

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

            if (NwInsung02.s_Use)
            {
                Debug.WriteLine($"[ExternalAppController] Insung02 생성: Id={NwInsung02.s_Id}");
                Insung02 = new NwInsung02();
                m_ListApps.Add(Insung02);
            }
            else
            {
                Debug.WriteLine("[ExternalAppController] Insung02 사용 안함 (s_Use=false)");
            }

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
