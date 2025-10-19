using Kai.Common.StdDll_Common;

namespace Kai.Client.CallCenter.Networks;

/// <summary>
/// 외부 네트워크 앱 공통 인터페이스
/// </summary>
public interface IExternalApp : IDisposable
{
    Task<StdResult_Status> InitializeAsync();
    void Shutdown();
    bool IsUsed { get; }
    string AppName { get; }
}
