using System.Diagnostics;

namespace Kai.Client.CallCenter.Classes;

#nullable disable
// 취소/일시정지 제어를 위한 토큰 컨트롤러
public class CancelTokenControl
{
    private readonly ManualResetEventSlim _pauseEvent = new(true);
    private volatile bool _isPaused = false;

    public CancellationTokenSource TokenSource { get; private set; } = new();
    public CancellationToken Token => TokenSource.Token;

    public bool IsPaused => _isPaused;

    // 일시정지 (동기)
    public void Pause()
    {
        if (_isPaused) return;

        Debug.WriteLine("[CancelTokenControl] Pause 호출됨");
        _isPaused = true;
        _pauseEvent.Reset();
    }

    // 재개 (동기)
    public void Resume()
    {
        if (!_isPaused) return;

        Debug.WriteLine("[CancelTokenControl] Resume 호출됨");

        _isPaused = false;   // 플래그 해제
        _pauseEvent.Set();   // 대기 중인 루프 깨움
    }

    // 일시정지/취소 상태 대기 (async) - Pause면 Resume까지 대기, Cancel이면 예외 발생
    public async Task WaitIfPausedOrCancelledAsync()
    {
        while (_isPaused)
        {
            Token.ThrowIfCancellationRequested();
            await Task.Run(() => _pauseEvent.Wait(200, Token));
        }

        Token.ThrowIfCancellationRequested();
    }

    // 취소 요청
    public void Cancel()
    {
        if (!TokenSource.IsCancellationRequested)
        {
            Debug.WriteLine("[CancelTokenControl] Cancel 호출됨");
            TokenSource.Cancel();
        }
    }

    // 상태 초기화 (재시작용)
    public void Reset()
    {
        Debug.WriteLine("[CancelTokenControl] Reset 호출됨 (토큰 재생성)");
        
        // 일시정지 해제
        Resume();

        // 기존 토큰 소스 정리 및 새 소스 생성
        // 주의: 기존 토큰을 구독 중인 작업들은 취소 상태로 남음
        try { TokenSource?.Dispose(); } catch { }
        
        // 새로운 인스턴스로 교체
        TokenSource = new CancellationTokenSource();
    }
}
#nullable enable