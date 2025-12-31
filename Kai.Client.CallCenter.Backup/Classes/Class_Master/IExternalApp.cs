using Kai.Common.StdDll_Common;

namespace Kai.Client.CallCenter.Classes.Class_Master;

// 외부 네트워크 앱 공통 인터페이스
public interface IExternalApp : IDisposable
{
    // 앱 사용 여부
    bool IsUsed { get; }

    // 앱 이름
    string AppName { get; }

    // 초기화 (앱 실행, 로그인, 윈도우 찾기 등)
    Task<StdResult_Status> InitializeAsync();

    // 자동배차 실행 (한 사이클)
    Task<StdResult_Status> AutoAllocAsync(long lAllocCount, CancelTokenControl ctrl);

    // 종료 (앱 닫기, 리소스 정리)
    void Shutdown();
}
