//using System.Diagnostics;

//namespace Kai.Client.CallCenter.Classes;
//#nullable disable
//public class CancelTokenControl
//{
//    private readonly ManualResetEventSlim _pauseEvent = new(true);
//    private TaskCompletionSource<bool> _pauseEnteredTcs;

//    private volatile bool _isPaused = false;
//    private Task _loopTask;

//    public CancellationTokenSource TokenSource { get; } = new();
//    public CancellationToken Token => TokenSource.Token;

//    public bool IsPaused => _isPaused;

//    public void AttachLoopTask(Task loopTask)
//    {
//        _loopTask = loopTask;
//    }

//    public async Task PauseAsync()
//    {
//        if (_isPaused) return;

//        _isPaused = true;
//        _pauseEvent.Reset();
//        _pauseEnteredTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

//        Debug.WriteLine("[ExecutionControl] Pausing...");
//        LocalFuncs.ShowExtMsgWndSimple(LocalVars.s_MainWnd, "자동배차를 정지중 입니다.");

//        try
//        {
//            // 루프에서 Pause에 진입하면 TrySetResult 호출 → 여기서 풀림
//            await _pauseEnteredTcs.Task.WaitAsync(Token);

//            Debug.WriteLine("[ExecutionControl] Paused");

//            // UI 닫기

//        }
//        catch (OperationCanceledException)
//        {
//            Debug.WriteLine("[ExecutionControl] PauseAsync 취소됨");
//        }
//        finally
//        {
//            LocalFuncs.CloseExtMsgWndSimple();
//        }
//    }

//    public void Resume()
//    {
//        if (!_isPaused) return;

//        Debug.WriteLine("[ExecutionControl] Resume 호출됨");

//        _isPaused = false;   // 플래그 해제
//        _pauseEvent.Set();   // 대기 중인 루프 깨움
//    }

//    public async Task WaitIfPausedOrCancelledAsync()
//    {
//        bool signaled = false;

//        while (_isPaused)
//        {
//            Token.ThrowIfCancellationRequested();

//            if (!signaled)
//            {
//                // Pause 진입 시점 알림
//                _pauseEnteredTcs?.TrySetResult(true);
//                signaled = true;
//            }

//            await Task.Run(() => _pauseEvent.Wait(200, Token));
//        }

//        Token.ThrowIfCancellationRequested();
//    }

//    public void Cancel()
//    {
//        if (!TokenSource.IsCancellationRequested)
//            TokenSource.Cancel();
//    }

//    /// ✅ StopAsync: Cancel + 루프 종료 대기
//    public async Task StopAsync()
//    {
//        if (_loopTask == null)
//        {
//            Debug.WriteLine("[ExecutionControl] 작업이 Attach되지 않았습니다.");
//            return;
//        }

//        Cancel(); // 취소 요청

//        try
//        {
//            await _loopTask; // 루프가 종료되기를 기다림
//            Debug.WriteLine("[ExecutionControl] 루프 종료됨");
//        }
//        catch (OperationCanceledException)
//        {
//            Debug.WriteLine("[ExecutionControl] 루프 작업이 취소됨");
//        }
//    }
//}
//#nullable enable