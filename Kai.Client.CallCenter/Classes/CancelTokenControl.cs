using System.Diagnostics;

namespace Kai.Client.CallCenter.Classes;

#nullable disable
/// <summary>
/// 취소/일시정지 제어를 위한 토큰 컨트롤러
/// - UI 의존성 제거: 상위 레이어에서 UI 처리
/// - 단순화: 핵심 기능만 제공 (Pause/Resume/Cancel/Wait)
/// - 명확한 책임: 토큰 상태 관리만 담당
/// </summary>
public class CancelTokenControl
{
    private readonly ManualResetEventSlim _pauseEvent = new(true);
    private volatile bool _isPaused = false;

    public CancellationTokenSource TokenSource { get; } = new();
    public CancellationToken Token => TokenSource.Token;

    public bool IsPaused => _isPaused;

    /// <summary>
    /// 일시정지 (동기)
    /// </summary>
    public void Pause()
    {
        if (_isPaused) return;

        Debug.WriteLine("[CancelTokenControl] Pause 호출됨");
        _isPaused = true;
        _pauseEvent.Reset();
    }

    /// <summary>
    /// 재개 (동기)
    /// </summary>
    public void Resume()
    {
        if (!_isPaused) return;

        Debug.WriteLine("[CancelTokenControl] Resume 호출됨");

        _isPaused = false;   // 플래그 해제
        _pauseEvent.Set();   // 대기 중인 루프 깨움
    }

    /// <summary>
    /// 일시정지/취소 상태 대기 (async)
    /// - Pause 상태면 Resume 될 때까지 대기
    /// - Cancel 상태면 OperationCanceledException 발생
    /// </summary>
    public async Task WaitIfPausedOrCancelledAsync()
    {
        while (_isPaused)
        {
            Token.ThrowIfCancellationRequested();
            await Task.Run(() => _pauseEvent.Wait(200, Token));
        }

        Token.ThrowIfCancellationRequested();
    }

    /// <summary>
    /// 취소 요청
    /// </summary>
    public void Cancel()
    {
        if (!TokenSource.IsCancellationRequested)
        {
            Debug.WriteLine("[CancelTokenControl] Cancel 호출됨");
            TokenSource.Cancel();
        }
    }
}
#nullable enable