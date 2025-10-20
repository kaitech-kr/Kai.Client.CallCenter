using Kai.Common.StdDll_Common;
using Kai.Client.CallCenter.Classes;

namespace Kai.Client.CallCenter.Networks;

/// <summary>
/// 외부 네트워크 앱 공통 인터페이스
/// </summary>
public interface IExternalApp : IDisposable
{
    /// <summary>
    /// 앱 사용 여부
    /// </summary>
    bool IsUsed { get; }

    /// <summary>
    /// 앱 이름
    /// </summary>
    string AppName { get; }

    /// <summary>
    /// 초기화 (앱 실행, 로그인, 윈도우 찾기 등)
    /// </summary>
    Task<StdResult_Status> InitializeAsync();

    /// <summary>
    /// 자동배차 실행 (한 사이클)
    /// </summary>
    /// <param name="lAllocCount">배차 카운트 (로깅용)</param>
    /// <param name="ctrl">취소/일시정지 컨트롤</param>
    /// <returns>실행 결과</returns>
    Task<StdResult_Status> AutoAllocAsync(long lAllocCount, CancelTokenControl ctrl);

    /// <summary>
    /// 종료 (앱 닫기, 리소스 정리)
    /// </summary>
    void Shutdown();
}
