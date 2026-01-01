using Draw = System.Drawing;
using System.Diagnostics;

using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;

using Kai.Client.CallCenter.Classes;
using static Kai.Client.CallCenter.Classes.CommonVars;

namespace Kai.Client.CallCenter.Networks.NwCargo24s;
#nullable disable

// 화물24시 앱 실행 및 스플래시 처리 담당 클래스
public class Cargo24sAct_App
{
    #region 2. Context Reference - 컨텍스트 참조
    // Context 참조
    private readonly Cargo24Context m_Context;

    // 편의를 위한 로컬 참조들
    private Cargo24sInfo_File m_FileInfo => m_Context.FileInfo;
    private Cargo24sInfo_Mem m_MemInfo => m_Context.MemInfo;
    #endregion

    #region 3. Constructor - 생성자
    // 생성자 - Context를 받아서 초기화
    public Cargo24sAct_App(Cargo24Context context)
    {
        m_Context = context ?? throw new ArgumentNullException(nameof(context));
    }
    #endregion

    #region 4. UpdaterWork - 앱 실행 및 스플래시 대기
    // Updater 실행 및 Splash 윈도우 대기 (화물24시 앱 실행)
    public async Task<StdResult_Status> UpdaterWorkAsync(string sPath, bool bEdit = true, bool bWrite = true, bool bMsgBox = true)
    {
        Process procExec = null;
        EventHandler exitHandler = null;
        bool bClosed = false;

        try
        {
            Debug.WriteLine($"[{m_Context.AppName}/App] UpdaterWork 시작: Path={sPath}");

            // 1. 실행 파일 존재 확인 (인성 로직 반영)
            if (!System.IO.File.Exists(sPath))
            {
                string err = $"업데이터 경로가 존재하지 않습니다: {sPath}";
                Debug.WriteLine($"[{m_Context.AppName}/App] {err}");
                return new StdResult_Status(StdResult.Fail, err, "Cargo24sAct_App/UpdaterWorkAsync_NotFound");
            }

            // 2. Updater 실행
            procExec = Process.Start(new ProcessStartInfo { UseShellExecute = true, FileName = sPath });
            if (procExec == null)
            {
                return new StdResult_Status(StdResult.Fail, $"[{m_Context.AppName}/App] 실행 실패: procExec == null", "Cargo24sAct_App/UpdaterWorkAsync_01");
            }

            Debug.WriteLine($"[{m_Context.AppName}/App] Updater 프로세스 시작됨: PID={procExec.Id}");

            procExec.EnableRaisingEvents = true;
            exitHandler = (sender, e) =>
            {
                bClosed = true;
                Debug.WriteLine($"[{m_Context.AppName}/App] Updater 프로세스 종료됨");
            };
            procExec.Exited += exitHandler;

            for (int i = 0; i < 6000; i++) // 5분
            {
                if (s_GlobalCancelToken.Token.IsCancellationRequested)
                {
                    return new StdResult_Status(StdResult.Skip, "사용자의 요청으로 작업이 취소되었습니다.", "Cargo24sAct_App/UpdaterWorkAsync_Cancel");
                }

                await Task.Delay(c_nWaitShort);
                if (bClosed) break;

                m_MemInfo.Splash.TopWnd_hWnd = Std32Window.FindWindow(m_FileInfo.Splash_TopWnd_sClassName, m_FileInfo.Splash_TopWnd_sWndName);
                if (m_MemInfo.Splash.TopWnd_hWnd != IntPtr.Zero) break;
            }

            // 3. 결과 확인
            if (m_MemInfo.Splash.TopWnd_hWnd == IntPtr.Zero)
            {
                if (bClosed)
                {
                    return new StdResult_Status(StdResult.Fail, $"[{m_Context.AppName}/App] 업데이터가 종료되었으나 스플래시 창을 찾을 수 없습니다.", "Cargo24sAct_App/UpdaterWorkAsync_ClosedNoWnd");
                }
                return new StdResult_Status(StdResult.Fail, $"[{m_Context.AppName}/App] 스플래시 창 대기 타임아웃 (5분)", "Cargo24sAct_App/UpdaterWorkAsync_Timeout");
            }

            Debug.WriteLine($"[{m_Context.AppName}/App] UpdaterWork 완료: SplashWnd={m_MemInfo.Splash.TopWnd_hWnd}");
            return new StdResult_Status(StdResult.Success);
        }
        catch (Exception ex)
        {
            return new StdResult_Status(StdResult.Fail, $"[{m_Context.AppName}/App] 예외발생: {ex.Message}", "Cargo24sAct_App/UpdaterWorkAsync_99");
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

    // Splash 윈도우 처리 및 로그인 (중앙 이동, 정보 취득, 로그인 시도)
    public async Task<StdResult_Status> SplashWorkAsync(bool bEdit = true, bool bWrite = true, bool bMsgBox = true)
    {
        Cargo24sInfo_Mem.SplashWnd mSplash = m_MemInfo.Splash;
        IntPtr hWndTmp = IntPtr.Zero;
        Draw.Rectangle rcCur = StdUtil.s_rcDrawEmpty;

        try
        {
            Debug.WriteLine($"[{m_Context.AppName}/Splash] SplashWork 시작");

            // 1. SplashWindow 확인
            if (mSplash.TopWnd_hWnd == IntPtr.Zero)
            {
                return new StdResult_Status(StdResult.Fail, $"[{m_Context.AppName}/Splash] 스플래쉬윈도 찾기 실패", "Cargo24sAct_App/SplashWorkAsync_01");
            }

            // 2. 창을 최상위로 올리고 중앙으로 이동
            Std32Window.SetWindowTopMost(mSplash.TopWnd_hWnd, true);
            rcCur = Std32Window.GetWindowRect_DrawAbs(mSplash.TopWnd_hWnd);
            Draw.Rectangle rcNew = s_Screens.m_WorkingMonitor.GetCenterDrawRectangle(rcCur);
            StdWin32.MoveWindow(mSplash.TopWnd_hWnd, rcNew.X, rcNew.Y, rcNew.Width, rcNew.Height, true);

            // 이동 완료 대기 (최대 10초)
            for (int i = 0; i < 200; i++) // 200회 * 50ms = 10초
            {
                if (s_GlobalCancelToken.Token.IsCancellationRequested) return new StdResult_Status(StdResult.Fail, "작업 취소됨", "Cargo24sAct_App/SplashWorkAsync_Cancel1");
                hWndTmp = Std32Window.GetWndHandle_FromAbsDrawPos(rcNew.X, rcNew.Y);
                if (hWndTmp != IntPtr.Zero && hWndTmp == mSplash.TopWnd_hWnd) break;
                await Task.Delay(c_nWaitShort);
            }

            if (hWndTmp != mSplash.TopWnd_hWnd)
            {
                return new StdResult_Status(StdResult.Fail, $"[{m_Context.AppName}/Splash] 스플래쉬윈도 위치 이동 실패", "Cargo24sAct_App/SplashWorkAsync_02");
            }
            await Task.Delay(c_nWaitUltraLong); // 1000ms 안정화 대기

            // 3. ThreadId, ProcessId 취득
            mSplash.TopWnd_uThreadId = Std32Window.GetWindowThreadProcessId(mSplash.TopWnd_hWnd, out mSplash.TopWnd_uProcessId);
            if (mSplash.TopWnd_uThreadId == 0)
            {
                return new StdResult_Status(StdResult.Fail, $"[{m_Context.AppName}/Splash] 프로세스 정보 취득 실패", "Cargo24sAct_App/SplashWorkAsync_03");
            }

            // 4. 입력창(ID/PW) 핸들 획득
            mSplash.IdWnd_hWnd = Std32Window.GetWndHandle_FromRelDrawPt(mSplash.TopWnd_hWnd, m_FileInfo.Splash_IdWnd_ptChk);
            mSplash.PwWnd_hWnd = Std32Window.GetWndHandle_FromRelDrawPt(mSplash.TopWnd_hWnd, m_FileInfo.Splash_PwWnd_ptChk);

            if (mSplash.IdWnd_hWnd == IntPtr.Zero || mSplash.PwWnd_hWnd == IntPtr.Zero)
            {
                return new StdResult_Status(StdResult.Fail, $"[{m_Context.AppName}/Splash] 입력창 핸들 찾기 실패", "Cargo24sAct_App/SplashWorkAsync_04");
            }

            // 5. 로그인 작업 시도
            if (s_GlobalCancelToken.Token.IsCancellationRequested) return new StdResult_Status(StdResult.Fail, "작업 취소됨", "Cargo24sAct_App/SplashWorkAsync_Cancel2");

            StdWin32.BlockInput(true); // 입력 차단
            try
            {
                string id = Std32Window.GetWindowCaption(mSplash.IdWnd_hWnd);
                string pw = Std32Window.GetWindowCaption(mSplash.PwWnd_hWnd);

                if (id != m_Context.Id) Std32Window.SetWindowCaption(mSplash.IdWnd_hWnd, m_Context.Id);
                if (pw != m_Context.Pw) Std32Window.SetWindowCaption(mSplash.PwWnd_hWnd, m_Context.Pw);

                await Task.Delay(c_nWaitLong); // 250ms
                Std32Key_Msg.KeyPost_Click(mSplash.PwWnd_hWnd, StdCommon32.VK_RETURN); // 엔터키 전송
                await Task.Delay(c_nWaitLong); // 250ms
            }
            finally
            {
                StdWin32.BlockInput(false); // 입력 차단 해제
            }

            Debug.WriteLine($"[{m_Context.AppName}/Splash] 로그인 처리 완료");
            return new StdResult_Status(StdResult.Success);
        }
        catch (OperationCanceledException)
        {
            return new StdResult_Status(StdResult.Skip, "사용자 요청으로 취소됨", "SplashWorkAsync_Cancel");
        }
        catch (Exception ex)
        {
            return new StdResult_Status(StdResult.Fail, $"[{m_Context.AppName}/Splash] 예외발생: {ex.Message}", "Cargo24sAct_App/SplashWorkAsync_99");
        }
        finally
        {
            StdWin32.BlockInput(false);
            if (mSplash.TopWnd_hWnd != IntPtr.Zero) Std32Window.SetWindowTopMost(mSplash.TopWnd_hWnd, false);
        }
    }

    #region Close
    // 화물24시 앱 종료 - MainWindow 닫기 시도, 예외시 프로세스 강제 종료
    public StdResult_Status Close(int nDelayMiliSec = 50)
    {
        Cargo24sInfo_Mem.MainWnd mMain = m_MemInfo.Main;
        Cargo24sInfo_Mem.SplashWnd mSplash = m_MemInfo.Splash;

        try
        {
            Debug.WriteLine($"[{m_Context.AppName}/App] Close 시작");

            // 1. MainWindow 닫기 시도
            if (mMain.TopWnd_hWnd != IntPtr.Zero)
            {
                StdWin32.PostMessage(mMain.TopWnd_hWnd, StdCommon32.WM_SYSCOMMAND, (uint)StdCommon32.SC_CLOSE, IntPtr.Zero);
                Debug.WriteLine($"[{m_Context.AppName}/App] MainWindow 닫기 메시지 전송");

                // 종료 확인 다이아로그 대기 (최대 1초)
                IntPtr hWndDlg = IntPtr.Zero;
                for (int i = 0; i < c_nRepeatVeryShort; i++) // 10회 * 50ms = 0.5초
                {
                    hWndDlg = Std32Window.FindMainWindow_Reduct(mSplash.TopWnd_uProcessId, "TMessageForm", "Confirm");
                    if (hWndDlg == IntPtr.Zero) hWndDlg = Std32Window.FindWindow("TMessageForm", "Confirm");

                    if (hWndDlg != IntPtr.Zero) break;
                    Thread.Sleep(c_nWaitShort);
                }

                if (hWndDlg != IntPtr.Zero)
                {
                    Debug.WriteLine($"[{m_Context.AppName}/App] 종료 확인창 발견: {hWndDlg}");
                    IntPtr hWndBtn = Std32Window.FindWindowEx(hWndDlg, IntPtr.Zero, "TButton", "&Yes");
                    if (hWndBtn != IntPtr.Zero)
                    {
                        IntPtr lParam = StdUtil.MakeIntPtrLParam(3, 3);
                        StdWin32.PostMessage(hWndBtn, StdCommon32.WM_LBUTTONDOWN, 1, lParam);
                        Thread.Sleep(c_nWaitVeryShort);
                        StdWin32.PostMessage(hWndBtn, StdCommon32.WM_LBUTTONUP, 0, lParam);
                        Thread.Sleep(c_nWaitVeryShort);
                        Debug.WriteLine($"[{m_Context.AppName}/App] YES 버튼 클릭됨");
                    }
                }

                // 윈도우 닫힘 대기 (최대 약 2.5초)
                for (int i = 0; i < c_nRepeatShort; i++) // 50회 * 50ms = 2.5초 (근사값)
                {
                    if (!Std32Window.IsWindowVisible(mMain.TopWnd_hWnd))
                    {
                        Debug.WriteLine($"[{m_Context.AppName}/App] MainWindow 종료 확인됨");
                        return new StdResult_Status(StdResult.Success);
                    }
                    Thread.Sleep(c_nWaitShort);
                }

                // 안 닫히면 강제 종료
                try
                {
                    var process = Process.GetProcessById((int)mSplash.TopWnd_uProcessId);
                    process.Kill();
                    Debug.WriteLine($"[{m_Context.AppName}/App] 프로세스 강제 종료됨 (PID: {mSplash.TopWnd_uProcessId})");
                    return new StdResult_Status(StdResult.Success);
                }
                catch (Exception ex)
                {
                    return new StdResult_Status(StdResult.Fail, $"[{m_Context.AppName}/App] 강제 종료 실패: {ex.Message}", "Cargo24sAct_App/Close_01");
                }
            }

            // 2. SplashWindow만 남은 경우 처리
            if (mSplash.TopWnd_hWnd != IntPtr.Zero)
            {
                Std32Window.PostCloseTwiceWindow(mSplash.TopWnd_hWnd);
                for (int i = 0; i < c_nRepeatShort; i++)
                {
                    if (!Std32Window.IsWindowVisible(mSplash.TopWnd_hWnd)) break;
                    Thread.Sleep(c_nWaitShort);
                }
            }

            Debug.WriteLine($"[{m_Context.AppName}/App] Close 완료");
            return new StdResult_Status(StdResult.Success);
        }
        catch (Exception ex)
        {
            return new StdResult_Status(StdResult.Fail, $"[{m_Context.AppName}/App] 예외발생: {ex.Message}", "Cargo24sAct_App/Close_99");
        }
    }
    #endregion
}
#nullable restore
