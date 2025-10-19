//using System.IO;
//using System.Diagnostics;

//using Kai.Common.StdDll_Common;
//using Kai.Common.FrmDll_FormCtrl;
//using static Kai.Common.FrmDll_FormCtrl.FormFuncs;

//using Kai.Client.CallCenter.Networks;

//namespace Kai.Client.CallCenter.Classes;
//#nullable disable
//public class CtrlOtherApps : IDisposable
//{
//    #region Dispose
//    private bool disposedValue;

//    protected virtual void Dispose(bool disposing)
//    {
//        if (!disposedValue)
//        {
//            if (disposing)
//            {
//                // TODO: 관리형 상태(관리형 개체)를 삭제합니다.
//            }

//            // TODO: 비관리형 리소스(비관리형 개체)를 해제하고 종료자를 재정의합니다.
//            // TODO: 큰 필드를 null로 설정합니다.
//            disposedValue = true;
//        }
//    }

//    // // TODO: 비관리형 리소스를 해제하는 코드가 'Dispose(bool disposing)'에 포함된 경우에만 종료자를 재정의합니다.
//    // ~NwCargo24()
//    // {
//    //     // 이 코드를 변경하지 마세요. 'Dispose(bool disposing)' 메서드에 정리 코드를 입력합니다.
//    //     Dispose(disposing: false);
//    // }

//    public void Dispose()
//    {
//        // 이 코드를 변경하지 마세요. 'Dispose(bool disposing)' 메서드에 정리 코드를 입력합니다.
//        Dispose(disposing: true);
//        GC.SuppressFinalize(this);
//    }
//    #endregion

//    #region Normal Variables
//    private static CtrlOtherApps instance; // For Multi-Thread Safe Singleton
//    private static readonly object lockObject = new object(); // For Multi-Thread Safe Singleton
//    public static CancelTokenControl ctrlCancelToken = new CancelTokenControl();

//    private StdResult_Status result = null;
//    public static long lAutoAllocCount = 0;
//    #endregion

//    #region Instance
//    public CtrlOtherApps()
//    {
//    }

//    public static CtrlOtherApps Instance // Multi-Thread Safe Singleton
//    {
//        get
//        {
//            if (instance == null)
//            {
//                lock (lockObject)
//                {
//                    if (instance == null)
//                        instance = new CtrlOtherApps();
//                }
//            }
//            return instance;
//        }
//    }
//    #endregion

//    #region Normal Method
//    public async Task<StdResult_Status> StartAsync()
//    {
//        //// 잘못된 모니터 
//        //if (LocalCommon_Vars.s_Screens.m_WorkingMonitor == null)
//        //    return LocalCommon_StdResult.ErrMsgResult_Status(StdResult.Fail, "잘못된 모니터: null", "CtrlOtherApps/StartAsync_01");

//        //// NwInsung01 - Create
//        //if (NwInsung01.s_AppStatus == NwCommon_AppStatus.Use)
//        //{
//        //    result = await NwInsung01.CreateAsync();

//        //    if (!string.IsNullOrEmpty(result.sErr))
//        //        return LocalCommon_StdResult.ErrMsgResult_Status(result.Result, result.sErrNPos, "CtrlOtherApps/StartAsync_10");
//        //}

//        //// Network_Insung02
//        //Debug.WriteLine($"StartAsync: {NwInsung02.c_sAppName}"); // Test
//        //if (NwInsung02.s_AppStatus == NwCommon_AppStatus.Use)
//        //{
//        //    result = await NwInsung02.CreateAsync();

//        //    if (!string.IsNullOrEmpty(result.sErr))
//        //        return LocalCommon_StdResult.ErrMsgResult_Status(result.Result, result.sErrNPos, "CtrlOtherApps/StartAsync_20");
//        //}

//        //// Cargo24SiNetwork
//        ////ThreadMsgBox($"{NwCargo24.s_AppStatus}, {NwCargo24.s_sId}, {NwCargo24.s_sPw}"); // Test
//        //if (NwCargo24.s_AppStatus == NwCommon_AppStatus.Use && !token.IsCancellationRequested)
//        //{
//        //    result = await NwCargo24.CreateAsync();

//        //    if (result == null) return new StdResult_Status(StdResult.Exit);
//        //    if (result.Result != StdResult.Success)
//        //    {
//        //        return new StdResult_Status(result.Result, result.sErr, "CtrlOtherApps.StartAsync_03", s_sLogDir, true);
//        //    }
//        //}

//        //// Network_Onecall
//        ////ThreadMsgBox($"{NwOnecall.s_AppStatus}, {NwOnecall.s_sId}, {NwOnecall.s_sPw}"); // Test
//        //if (NwOnecall.s_AppStatus == NwCommon_AppStatus.Use && !token.IsCancellationRequested)
//        //{
//        //    result = await NwOnecall.CreateAsync();

//        //    if (result == null) return new StdResult_Status(StdResult.Exit);
//        //    if (result.Result != StdResult.Success)
//        //    {
//        //        return new StdResult_Status(result.Result, result.sPos + result.sErr, "CtrlOtherApps.StartAsync_04", s_sLogDir, true);
//        //    }
//        //}

//        //// 외부에서 고객정보를 가져왔으면 거래처정보로 보충.
//        //if (NwCommon.s_bCopiedExtCusts)
//        //{
//        //    result = await NwCommon.MakeCompanyByExtCust();

//        //    if (!string.IsNullOrEmpty(result.sErr)) return LocalCommon_StdResult.ErrMsgResult_Status(result);
//        //    return result;
//        //}

//        //_ = Task.Run(() => AutoAllocLoopAsync());
//        return new StdResult_Status(StdResult.Success); // Test
//    }

//    public async Task AutoAllocLoopAsync() // Order_StatusPage 에서 시작함
//    {
//        Stopwatch stopwatch = new();
//        const int nMinWorkingMiliSec = 5000; // 10000
//        int nDelay = 0;
//        StdResult_Status resultSts = null;

//        //Debug.WriteLine("AutoAllocLoopAsync: Start"); // Test

//        for (lAutoAllocCount = 1; ; lAutoAllocCount++)
//        {
//            try
//            {
//                stopwatch.Restart();

//                //#region ========== NwInsung01 ==========
//                //// Pause/Cancel 체크
//                //await ctrlCancelToken.WaitIfPausedOrCancelledAsync(); // Pause/Cancellation 체크

//                //var insung1 = NwInsung01.Instance;
//                //if (insung1 != null)
//                //{
//                //    resultSts = await insung1.AutoAllocAsync(lAutoAllocCount, ctrlCancelToken);
//                //}
//                //#endregion End - NwInsung01

//                //#region ========== NwInsung02 ==========
//                //// Pause/Cancel 체크
//                //await ctrlCancelToken.WaitIfPausedOrCancelledAsync(); // Pause/Cancellation 체크

//                //var insung2 = NwInsung02.Instance;
//                //if (insung2 != null)
//                //{
//                //    resultSts = await insung2.AutoAllocAsync(lAutoAllocCount, ctrlCancelToken);
//                //}
//                //#endregion End - NwInsung02

//                //// ========== Delay 보정 ==========
//                //stopwatch.Stop();
//                //nDelay = stopwatch.ElapsedMilliseconds < nMinWorkingMiliSec
//                //    ? nMinWorkingMiliSec - (int)stopwatch.ElapsedMilliseconds
//                //    : 0;

//                //if (nDelay > 0)
//                //    await Task.Delay(nDelay, ctrlCancelToken.Token).ConfigureAwait(false);

//                //Debug.WriteLine($"[{lAutoAllocCount}]: Elapsed={stopwatch.ElapsedMilliseconds}ms, Delay={nDelay}");
//            }
//            catch (OperationCanceledException)
//            {
//                Debug.WriteLine("AutoAllocLoopAsync cancelled via token.");
//                return;
//            }
//            catch (IOException ioEx)
//            {
//                // Resume 시 닫힌 소켓 접근 등 일시 오류 → 다음 루프로 재시도
//                Debug.WriteLine($"IOException 발생: {ioEx.Message}");
//                continue;
//            }
//            catch (Exception ex)
//            {
//                FormFuncs.ErrMsgBox($"AutoAllocLoopAsync 예외 발생\n\n{ex.Message}", "CtrlOtherApps");
//            }
//        }
//    }

//    public void Close()
//    {
//        // TmpHide
//        //if (NwInsung01.Instance != null) { NwInsung01.Instance.Close(); NwInsung01.Instance = null; }
//        //if (NwInsung02.Instance != null) { NwInsung02.Instance.Close(); NwInsung02.Instance = null; }
//        //if (NwCargo24.Instance != null) { NwCargo24.Instance.Close(); NwCargo24.Instance = null; }
//        //if (NwOnecall.Instance != null) { NwOnecall.Instance.Close(); NwInsung01.Instance = null; }

//        Dispose();
//    }
//    #endregion

//    #region Tmp
//    //public async Task<StdResult_Status> SyncronizeAsync(CancellationToken token)
//    //{
//    //    #region Insung01
//    //    if (NwInsung01.Instance != null && !token.IsCancellationRequested)
//    //    {
//    //        await Application.Current.Dispatcher.Invoke(async () =>
//    //        {
//    //            result = await NwInsung01.Instance.m_RcptRegPage.SyncronizeToMeAsync(bSaveTextIfNotFind, bSaveCharIfNotFind);
//    //        });

//    //        if (result.Result != StdResult.Success)
//    //        {
//    //            FormFuncs.ErrMsgBox(result.sErr, "CtrlOtherApps/SyncronizeAsync_01");
//    //            return result;
//    //        }
//    //    }
//    //    #endregion Insung01

//    //    #region Insung02
//    //    if (NwInsung02.Instance != null && !token.IsCancellationRequested)
//    //    {
//    //        await Application.Current.Dispatcher.Invoke(async () =>
//    //        {
//    //            result = await NwInsung02.Instance.m_RcptRegPage.SyncronizeToMeAsync(bSaveTextIfNotFind, bSaveCharIfNotFind);
//    //        });

//    //        if (result.Result != StdResult.Success)
//    //        {
//    //            FormFuncs.ErrMsgBox(result.sErr, "CtrlOtherApps/SyncronizeAsync_02");
//    //            return result;
//    //        }
//    //    }
//    //    #endregion Insung02

//    //    // 캔슬토큰을 설정한다.
//    //    //s_cts = new CancellationTokenSource();
//    //    //_ = Task.Run(() => InfiniteLoop(s_cts.Token));

//    //    return new StdResult_Status(StdResult.Success);
//    //}

//    //private async Task InfiniteLoop(CancellationToken token)
//    //{
//    //    Stopwatch stopwatch = new Stopwatch();
//    //    int nMinWorkingMiliSec = 5000;
//    //    int nDelay = 0;

//    //    while (!token.IsCancellationRequested)
//    //    {
//    //        stopwatch.Reset();
//    //        stopwatch.Start();

//    //        #region Insung01
//    //        if (NwInsung01.Instance != null)
//    //        {
//    //            await Application.Current.Dispatcher.Invoke(async () =>
//    //            {
//    //                result = await NwInsung01.Instance.
//    //                    m_RcptRegPage.OrderCheckToMeAsync(bSaveTextIfNotFind, bSaveCharIfNotFind);
//    //            });

//    //            if (result == null) return;
//    //            if (result.Result != StdResult.Success)
//    //            {
//    //                FormFuncs.ErrMsgBox(result.sErr, "CtrlOtherApps/Thread_FromOtherAppToMe_01");
//    //                return;
//    //            }
//    //        }
//    //        #endregion Insung01

//    //        #region Insung02
//    //        if (NwInsung02.Instance != null)
//    //        {
//    //            await Application.Current.Dispatcher.Invoke(async () =>
//    //            {
//    //                result = await NwInsung02.Instance.
//    //                    m_RcptRegPage.OrderCheckToMeAsync(bSaveTextIfNotFind, bSaveCharIfNotFind);
//    //            });

//    //            if (result == null) return;
//    //            if (result.Result != StdResult.Success)
//    //            {
//    //                FormFuncs.ErrMsgBox(result.sErr, "CtrlOtherApps/Thread_FromOtherAppToMe_02");
//    //                return;
//    //            }
//    //        }
//    //        #endregion Insung02

//    //        stopwatch.Stop();
//    //        nDelay = 0;
//    //        if (stopwatch.ElapsedMilliseconds < nMinWorkingMiliSec)
//    //        {
//    //            nDelay = nMinWorkingMiliSec - (int)stopwatch.ElapsedMilliseconds;
//    //            await Task.Delay(nDelay);
//    //        }
//    //        //Debug.WriteLine($"Elapsed={stopwatch.ElapsedMilliseconds}ms, Delay={nDelay}"); // Test
//    //    }
//    //}

//    //public async Task AutoAllocLoopAsync()
//    //{
//    //    Stopwatch stopwatch = new();
//    //    const int nMinWorkingMiliSec = 5000;
//    //    int nDelay = 0;

//    //    for (lAutoAllocCount = 1; ; lAutoAllocCount++)
//    //    {
//    //        try
//    //        {
//    //            // **Pause 또는 취소 상태 체크**
//    //            if (!await WaitIfPausedOrCancelledAsync())
//    //                return;

//    //            stopwatch.Restart();

//    //            // ========== NwInsung01 ==========
//    //            var insung1 = NwInsung01.Instance;
//    //            if (insung1 != null)
//    //            {
//    //                await Application.Current.Dispatcher.Invoke(async () =>
//    //                {
//    //                    result = await insung1.AutoAllocAsync(lAutoAllocCount, bSaveTextIfNotFind, bSaveCharIfNotFind, _cts.Token);
//    //                });

//    //                if (result == null || result.Result < StdResult.Success)
//    //                {
//    //                    FormFuncs.ErrMsgBox(result?.sErr ?? "Unknown error", "CtrlOtherApps/TempAllocLoop_01");
//    //                    return;
//    //                }
//    //            }

//    //            // **Pause 또는 취소 상태 체크**
//    //            //if (!await WaitIfPausedOrCancelledAsync())
//    //            //    return;

//    //            // ========== NwInsung02 ==========
//    //            var insung2 = NwInsung02.Instance;
//    //            if (insung2 != null)
//    //            {
//    //                await Application.Current.Dispatcher.Invoke(async () =>
//    //                {
//    //                    result = await insung2.AutoAllocAsync(lAutoAllocCount, bSaveTextIfNotFind, bSaveCharIfNotFind, _cts.Token);
//    //                });

//    //                if (result == null || result.Result < StdResult.Success)
//    //                {
//    //                    FormFuncs.ErrMsgBox(result?.sErr ?? "Unknown error", "CtrlOtherApps/TempAllocLoop_02");
//    //                    return;
//    //                }
//    //            }

//    //            // ========== Delay 보정 ==========
//    //            stopwatch.Stop();
//    //            nDelay = stopwatch.ElapsedMilliseconds < nMinWorkingMiliSec
//    //                ? nMinWorkingMiliSec - (int)stopwatch.ElapsedMilliseconds
//    //                : 0;

//    //            if (nDelay > 0)
//    //                await Task.Delay(nDelay, _cts.Token);

//    //            Debug.WriteLine($"[{lAutoAllocCount}]: Elapsed={stopwatch.ElapsedMilliseconds}ms, Delay={nDelay}");
//    //        }
//    //        catch (OperationCanceledException)
//    //        {
//    //            Debug.WriteLine("AutoAllocLoopAsync cancelled via token.");
//    //            return;
//    //        }
//    //        catch (Exception ex)
//    //        {
//    //            FormFuncs.ErrMsgBox($"AutoAllocLoopAsync 예외 발생\n\n{ex.Message}", "CtrlOtherApps");
//    //        }
//    //    }
//    //}

//    //public async Task AutoAllocLoopAsync()
//    //{
//    //    Stopwatch stopwatch = new Stopwatch();
//    //    const int nMinWorkingMiliSec = 5000;
//    //    int nDelay = 0;

//    //    for (lAutoAllocCount = 1; ; lAutoAllocCount++)
//    //    {
//    //        try
//    //        {
//    //            // 취소 요청 체크
//    //            if (_cts.Token.IsCancellationRequested)
//    //            {
//    //                Debug.WriteLine("AutoAllocLoop cancelled.");
//    //                return;
//    //            }

//    //            // Pause 상태라면 비동기로 대기
//    //            if (!_pauseEvent.IsSet)
//    //            {
//    //                _pauseReachedTcs?.TrySetResult(true);

//    //                await Task.Run(() =>
//    //                {
//    //                    _pauseEvent.Wait(_cts.Token);  // 취소 지원
//    //                }, _cts.Token);
//    //            }

//    //            stopwatch.Restart();
//    //            //Debug.WriteLine($"AutoAllocLoopAsync: {lAutoAllocCount}");

//    //            #region NwInsung01
//    //            var insung1 = NwInsung01.Instance;
//    //            if (insung1 != null)
//    //            {
//    //                await Application.Current.Dispatcher.Invoke(async () =>
//    //                {
//    //                    result = await insung1.AutoAllocAsync(lAutoAllocCount, bSaveTextIfNotFind, bSaveCharIfNotFind);
//    //                });

//    //                if (result == null) return;
//    //                if (result.Result < StdResult.Success)
//    //                {
//    //                    FormFuncs.ErrMsgBox(result.sErr, "CtrlOtherApps/TempAllocLoop_01");
//    //                    return;
//    //                }
//    //            }
//    //            #endregion

//    //            #region NwInsung02
//    //            var insung2 = NwInsung02.Instance;
//    //            if (insung2 != null)
//    //            {
//    //                await Application.Current.Dispatcher.Invoke(async () =>
//    //                {
//    //                    result = await insung2.AutoAllocAsync(lAutoAllocCount, bSaveTextIfNotFind, bSaveCharIfNotFind);
//    //                });

//    //                if (result == null) return;
//    //                if (result.Result < StdResult.Success)
//    //                {
//    //                    FormFuncs.ErrMsgBox(result.sErr, "CtrlOtherApps/TempAllocLoop_02");
//    //                    return;
//    //                }
//    //            }
//    //            #endregion

//    //            // 다른 네트워크 처리부(NwCargo24, NwOnecall)는 동일한 패턴으로 리팩토링 가능

//    //            stopwatch.Stop();
//    //            nDelay = 0;
//    //            if (stopwatch.ElapsedMilliseconds < nMinWorkingMiliSec)
//    //            {
//    //                nDelay = nMinWorkingMiliSec - (int)stopwatch.ElapsedMilliseconds;
//    //                await Task.Delay(nDelay);
//    //            }

//    //            Debug.WriteLine($"[{lAutoAllocCount}]: Elapsed={stopwatch.ElapsedMilliseconds}ms, Delay={nDelay}");
//    //        }
//    //        catch (Exception ex)
//    //        {
//    //            //Logger.LogError(ex, "AutoAllocLoopAsync 예외 발생");
//    //            FormFuncs.ErrMsgBox($"AutoAllocLoopAsync 예외 발생\n\n{ex.Message}", "CtrlOtherApps");
//    //        }
//    //    }
//    #endregion
//}
//#nullable restore