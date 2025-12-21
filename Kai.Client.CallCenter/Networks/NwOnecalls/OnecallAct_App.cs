using System.Windows;
using Draw = System.Drawing;
using System.Diagnostics;

using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;
using static Kai.Common.StdDll_Common.StdWin32.StdCommon32;
using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgBox;

using Kai.Client.CallCenter.Classes;
using static Kai.Client.CallCenter.Classes.CommonVars;

namespace Kai.Client.CallCenter.Networks.NwOnecalls;
#nullable disable

// 원콜 앱 제어 (프로세스 시작, 로그인 처리, 종료 등)
public class OnecallAct_App
{
    #region Private Fields
    private readonly OnecallContext m_Context;
    private OnecallInfo_File fInfo => m_Context.FileInfo;
    private OnecallInfo_Mem mInfo => m_Context.MemInfo;
    private string AppName => m_Context.AppName;
    
    private HashSet<IntPtr> m_HandledPopups = new HashSet<IntPtr>();
    #endregion

    #region 생성자
    public OnecallAct_App(OnecallContext context)
    {
        m_Context = context;
    }
    #endregion

    #region UpdaterWorkAsync
    public async Task<StdResult_Status> UpdaterWorkAsync(string sPath)
    {
        Process procExec = null;
        EventHandler exitHandler = null;
        bool bClosed = false;

        try
        {
            Debug.WriteLine($"[{AppName}] UpdaterWork 시작: Path={sPath}");

            if (!System.IO.File.Exists(sPath))
            {
                string err = $"업데이터 경로가 존재하지 않습니다: {sPath}";
                return new StdResult_Status(StdResult.Fail, err, "OnecallAct_App/UpdaterWorkAsync_NotFound");
            }

            procExec = Process.Start(new ProcessStartInfo { UseShellExecute = true, FileName = sPath });
            if (procExec == null) return new StdResult_Status(StdResult.Fail, $"[{AppName}] 실행 실패", "OnecallAct_App/UpdaterWorkAsync_01");

            procExec.EnableRaisingEvents = true;
            exitHandler = (sender, e) => { bClosed = true; };
            procExec.Exited += exitHandler;

            for (int i = 0; i < 3000; i++)
            {
                if (s_GlobalCancelToken.Token.IsCancellationRequested) return new StdResult_Status(StdResult.Skip, "취소됨", "OnecallAct_App/UpdaterWorkAsync_Cancel");
                await Task.Delay(c_nWaitShort);
                if (bClosed) break;
                mInfo.Splash.TopWnd_hWnd = Std32Window.FindWindow(null, fInfo.Splash_TopWnd_sWndName);
                if (mInfo.Splash.TopWnd_hWnd != IntPtr.Zero) break;
            }

            if (mInfo.Splash.TopWnd_hWnd == IntPtr.Zero) return new StdResult_Status(StdResult.Fail, "스플래시 대기 타임아웃", "OnecallAct_App/UpdaterWorkAsync_Timeout");

            return new StdResult_Status(StdResult.Success);
        }
        finally
        {
            if (procExec != null)
            {
                if (exitHandler != null) procExec.Exited -= exitHandler;
                procExec.Dispose();
            }
        }
    }
    #endregion

    #region SplashWorkAsync
    // Splash 윈도우 처리 및 로그인
    public async Task<StdResult_Status> SplashWorkAsync(bool bEdit = true, bool bWrite = true, bool bMsgBox = true)
    {
        OnecallInfo_Mem.SplashWnd mSplash = mInfo.Splash;
        try
        {
            Debug.WriteLine($"[{AppName}] SplashWork 시작");
            if (mSplash.TopWnd_hWnd == IntPtr.Zero) return new StdResult_Status(StdResult.Fail, "핸들 없음", "OnecallAct_App/SplashWorkAsync_01");

            // 1. 창 활성화 및 중앙 이동
            Std32Window.SetForegroundWindow(mSplash.TopWnd_hWnd);
            Std32Window.SetWindowTopMost(mSplash.TopWnd_hWnd, true);
            var rcCur = Std32Window.GetWindowRect_DrawAbs(mSplash.TopWnd_hWnd);
            var rcNew = s_Screens.m_WorkingMonitor.GetCenterDrawRectangle(rcCur);
            StdWin32.MoveWindow(mSplash.TopWnd_hWnd, rcNew.X, rcNew.Y, rcNew.Width, rcNew.Height, true);
            await Task.Delay(500);

            // 2. 정보 및 자식 핸들 취득
            mSplash.TopWnd_uThreadId = Std32Window.GetWindowThreadProcessId(mSplash.TopWnd_hWnd, out mSplash.TopWnd_uProcessId);
            mSplash.IdWnd_hWnd = Std32Window.GetWndHandle_FromRelDrawPt(mSplash.TopWnd_hWnd, fInfo.Splash_IdWnd_ptChk);
            mSplash.PwWnd_hWnd = Std32Window.GetWndHandle_FromRelDrawPt(mSplash.TopWnd_hWnd, fInfo.Splash_PwWnd_ptChk);
            mSplash.LoginBtn_hWnd = Std32Window.GetWndHandle_FromRelDrawPt(mSplash.TopWnd_hWnd, fInfo.Splash_LoginBtn_ptChk);

            if (mSplash.LoginBtn_hWnd == IntPtr.Zero) return new StdResult_Status(StdResult.Fail, "로그인 버튼 찾기 실패", "OnecallAct_App/SplashWorkAsync_03");

            // 3. 로그인 정보 입력
            StdWin32.BlockInput(true);
            try
            {
                await Task.Delay(500);
                Std32Window.SetWindowCaption(mSplash.IdWnd_hWnd, m_Context.Id);
                await Task.Delay(500);
                Std32Window.SetWindowCaption(mSplash.PwWnd_hWnd, m_Context.Pw);
                await Task.Delay(500);

                // 로그인 버튼 마우스 클릭
                Debug.WriteLine($"[{AppName}] 로그인 버튼 직접 클릭 시도");
                await Std32Mouse_Post.MousePostAsync_ClickLeft(mSplash.LoginBtn_hWnd);
                await Task.Delay(300);
            }
            finally { StdWin32.BlockInput(false); }

            // [초고속] 클릭 후 로그인 처리가 시작될 시간을 최소화하여 메인 창 감시를 빨리 시작합니다.
            await Task.Delay(100);

            // 4. 클릭 후 즉시 은폐 및 추방
            if (Std32Window.IsWindow(mSplash.TopWnd_hWnd))
            {
                StdWin32.ShowWindow(mSplash.TopWnd_hWnd, (int)StdCommon32.SW_HIDE);
                StdWin32.MoveWindow(mSplash.TopWnd_hWnd, -20000, -20000, 100, 100, true);
            }

            // 5. 스플래시 창이 사라질 때까지 감시 및 강제 은폐 유지
            for (int i = 0; i < 100; i++)
            {
                if (s_GlobalCancelToken.Token.IsCancellationRequested) break;
                
                if (Std32Window.IsWindow(mSplash.TopWnd_hWnd))
                {
                    StdWin32.ShowWindow(mSplash.TopWnd_hWnd, (int)StdCommon32.SW_HIDE);
                    StdWin32.MoveWindow(mSplash.TopWnd_hWnd, -20000, -20000, 100, 100, true);
                }
                else break;

                await Task.Delay(50);
            }

            // 6. [최적화] 메인 로딩 대기를 여기서 하지 않고 즉시 반환 (MainWndAct.InitAsync에서 대기함)
            // 팝업 정리는 백그라운드에서 병렬로 수행
            _ = ClosePopupsAsync(mSplash.TopWnd_uProcessId); 

            Debug.WriteLine($"[{AppName}] SplashWork 완료 (즉시 반환)");
            return new StdResult_Status(StdResult.Success);
        }
        catch (Exception ex)
        {
            return new StdResult_Status(StdResult.Fail, $"예외: {ex.Message}", "OnecallAct_App/SplashWorkAsync_99");
        }
    }

    // 팝업 감시 및 정리
    private async Task ClosePopupsAsync(uint uProcessId)
    {
        for (int i = 0; i < 10; i++) 
        {
            var lstWnds = Std32Window.FindMainWindows_SameProcessId(uProcessId);
            foreach (var hWnd in lstWnds)
            {
                if (m_HandledPopups.Contains(hWnd)) continue;
                if (hWnd == mInfo.Splash.TopWnd_hWnd || hWnd == mInfo.Main.TopWnd_hWnd) continue;

                string caption = Std32Window.GetWindowCaption(hWnd);
                if (StdUtil.ContainsHangul(caption) && !caption.Contains("원콜")) 
                {
                    m_HandledPopups.Add(hWnd);
                    StdWin32.ShowWindow(hWnd, (int)StdCommon32.SW_HIDE);
                    if (Std32Window.IsWindow(hWnd)) StdWin32.PostMessage(hWnd, 0x0010, 0, IntPtr.Zero); // WM_CLOSE
                }
            }
            await Task.Delay(500);
        }
    }
    #endregion

    #region Close
    // 원콜 앱 종료 (MainWindow 닫기 시도 후 프로세스 강제 종료 fallback)
    public StdResult_Status Close(int nDelayMiliSec = 100)
    {
        try
        {
            Debug.WriteLine($"[{AppName}] Close 시작");

            // 1. 메인 윈도우 닫기 시도
            if (mInfo.Main.TopWnd_hWnd != IntPtr.Zero && Std32Window.IsWindow(mInfo.Main.TopWnd_hWnd))
            {
                StdWin32.PostMessage(mInfo.Main.TopWnd_hWnd, StdCommon32.WM_SYSCOMMAND, (uint)StdCommon32.SC_CLOSE, IntPtr.Zero);
                Debug.WriteLine($"[{AppName}] 메인윈도우 종료 메시지 전송");
                Thread.Sleep(500); // 윈도우가 닫힐 시간 대기
            }

            // 2. 프로세스 강제 종료 (확실한 정리를 위해)
            uint uPid = mInfo.Splash.TopWnd_uProcessId;
            if (uPid != 0)
            {
                try
                {
                    var process = Process.GetProcessById((int)uPid);
                    if (!process.HasExited)
                    {
                        process.Kill();
                        Debug.WriteLine($"[{AppName}] 프로세스 강제 종료됨 (PID: {uPid})");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[{AppName}] 프로세스 종료 시도 중 오류: {ex.Message}");
                }
            }

            return new StdResult_Status(StdResult.Success);
        }
        catch (Exception ex)
        {
            return new StdResult_Status(StdResult.Fail, $"Close 예외: {ex.Message}", "OnecallAct_App/Close_99");
        }
    }
    #endregion
}
#nullable restore
