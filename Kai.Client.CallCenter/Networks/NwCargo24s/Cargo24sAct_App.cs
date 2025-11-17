using Draw = System.Drawing;
using System.Diagnostics;

using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;

using Kai.Client.CallCenter.Classes;
using static Kai.Client.CallCenter.Classes.CommonVars;

namespace Kai.Client.CallCenter.Networks.NwCargo24s;
#nullable disable

/// <summary>
/// 화물24시 앱 실행 및 스플래시 처리 담당 클래스
/// Context 패턴 사용: Cargo24Context를 통해 모든 정보에 접근
/// </summary>
public class Cargo24sAct_App
{
    #region Context Reference
    /// <summary>
    /// Context에 대한 읽기 전용 참조
    /// </summary>
    private readonly Cargo24Context m_Context;

    /// <summary>
    /// 편의를 위한 로컬 참조들
    /// </summary>
    private Cargo24sInfo_File m_FileInfo => m_Context.FileInfo;
    private Cargo24sInfo_Mem m_MemInfo => m_Context.MemInfo;
    #endregion

    #region Constructor
    /// <summary>
    /// 생성자 - Context를 받아서 초기화
    /// </summary>
    public Cargo24sAct_App(Cargo24Context context)
    {
        m_Context = context ?? throw new ArgumentNullException(nameof(context));
        Debug.WriteLine($"[Cargo24sAct_App] 생성자 호출: AppName={m_Context.AppName}");
    }
    #endregion

    #region UpdaterWork
    /// <summary>
    /// Updater 실행 및 Splash 윈도우 대기
    /// - Cargo24.exe 실행
    /// - Updater 종료 또는 Splash 윈도우 나타날 때까지 대기 (최대 5분)
    /// </summary>
    public async Task<StdResult_Error> UpdaterWorkAsync(string sPath, bool bEdit = true, bool bWrite = true, bool bMsgBox = true)
    {
        try
        {
            Debug.WriteLine($"[Cargo24sAct_App] UpdaterWork 시작: Path={sPath}");

            // Updater 실행
            ProcessStartInfo processInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = sPath
            };
            Process procExec = Process.Start(processInfo);
            if (procExec == null)
            {
                return CommonFuncs_StdResult.ErrMsgResult_Error(
                    $"[{m_Context.AppName}/UpdaterWork]실행실패: procExec == null",
                    "Cargo24sAct_App/UpdaterWorkAsync_01", bWrite, bMsgBox);
            }

            Debug.WriteLine($"[Cargo24sAct_App] Updater 프로세스 시작됨: PID={procExec.Id}");

            // 프로세스가 종료될 때 이벤트를 받기 위해 EnableRaisingEvents를 true로 설정
            procExec.EnableRaisingEvents = true;
            bool bClosed = false;
            procExec.Exited += (sender, e) =>
            {
                bClosed = true;
                Debug.WriteLine($"[Cargo24sAct_App] Updater 프로세스 종료됨");
            };

            // 300초(5분) 동안 Updater 종료 또는 Splash 창 나타나기 대기
            for (int i = 0; i < 3000; i++)
            {
                await Task.Delay(100);

                if (bClosed) break;

                m_MemInfo.Splash.TopWnd_hWnd =
                    Std32Window.FindWindow(m_FileInfo.Splash_TopWnd_sClassName, m_FileInfo.Splash_TopWnd_sWndName);
                Thread.Sleep(100); // 무조건 대기
                if (m_MemInfo.Splash.TopWnd_hWnd != IntPtr.Zero) break;
            }

            // 프로세스가 종료되지않거나 SplashWnd가 안보이면..
            if (!bClosed && m_MemInfo.Splash.TopWnd_hWnd == IntPtr.Zero)
            {
                return CommonFuncs_StdResult.ErrMsgResult_Error(
                    $"[{m_Context.AppName}/UpdaterWork]업데이터 (5분안에)종료실패",
                    "Cargo24sAct_App/UpdaterWorkAsync_02", bWrite, bMsgBox);
            }

            Debug.WriteLine($"[Cargo24sAct_App] UpdaterWork 완료: SplashWnd={m_MemInfo.Splash.TopWnd_hWnd}");
            return null;
        }
        catch (Exception ex)
        {
            return CommonFuncs_StdResult.ErrMsgResult_Error(
                $"[{m_Context.AppName}/UpdaterWork]예외발생: {ex.Message}",
                "Cargo24sAct_App/UpdaterWorkAsync_99", bWrite, bMsgBox);
        }
    }
    #endregion

    #region SplashWork
    /// <summary>
    /// Splash 윈도우 처리 및 로그인
    /// - Splash 윈도우 중앙 이동
    /// - ProcessId, ThreadId 취득
    /// - 아이디/비밀번호 입력창 찾기
    /// - 로그인 처리
    /// </summary>
    public async Task<StdResult_Error> SplashWorkAsync(bool bEdit = true, bool bWrite = true, bool bMsgBox = true)
    {
        Cargo24sInfo_Mem.SplashWnd mSplash = m_MemInfo.Splash;
        IntPtr hWndTmp = IntPtr.Zero; // 임시 핸들
        Draw.Rectangle rcCur = StdUtil.s_rcDrawEmpty;

        try
        {
            Debug.WriteLine($"[Cargo24sAct_App] SplashWork 시작");

            // Find SplashWindow
            if (mSplash.TopWnd_hWnd == IntPtr.Zero)
            {
                return CommonFuncs_StdResult.ErrMsgResult_Error(
                    $"[{m_Context.AppName}/SplashWork]스플래쉬윈도 찾기실패[{m_FileInfo.Splash_TopWnd_sClassName}, {m_FileInfo.Splash_TopWnd_sWndName}]",
                    "Cargo24sAct_App/SplashWorkAsync_01", bWrite, bMsgBox);
            }

            Debug.WriteLine($"[Cargo24sAct_App] 스플래시 윈도우 확인: {mSplash.TopWnd_hWnd}");

            // Make TopWindow ...
            Std32Window.SetWindowTopMost(mSplash.TopWnd_hWnd, true);

            // Move SplashWindow to the center of the screen
            rcCur = Std32Window.GetWindowRect_DrawAbs(mSplash.TopWnd_hWnd);
            Draw.Rectangle rcNew = s_Screens.m_WorkingMonitor.GetCenterDrawRectangle(rcCur);
            StdWin32.MoveWindow(mSplash.TopWnd_hWnd, rcNew.X, rcNew.Y, rcNew.Width, rcNew.Height, true);

            for (int i = 0; i < 100; i++) // 10초 동안..
            {
                hWndTmp = Std32Window.GetWndHandle_FromAbsDrawPos(rcNew.X, rcNew.Y);
                if (hWndTmp != IntPtr.Zero && hWndTmp == mSplash.TopWnd_hWnd) break; // 위치가 맞으면 break
                await Task.Delay(100); // 무조건 대기
            }

            if (hWndTmp != mSplash.TopWnd_hWnd)
            {
                return CommonFuncs_StdResult.ErrMsgResult_Error(
                    $"[{m_Context.AppName}/SplashWork]스플래쉬윈도 위치이동실패",
                    "Cargo24sAct_App/SplashWorkAsync_02", bWrite, bMsgBox);
            }

            await Task.Delay(1000); // ~초 대기 - 너무 짧으면 안됨
            Debug.WriteLine($"[Cargo24sAct_App] 스플래시 윈도우 중앙 이동 완료");

            // Get ThreadId, ProcessId
            mSplash.TopWnd_uThreadId = Std32Window.GetWindowThreadProcessId(mSplash.TopWnd_hWnd, out mSplash.TopWnd_uProcessId);
            if (mSplash.TopWnd_uThreadId == 0)
            {
                return CommonFuncs_StdResult.ErrMsgResult_Error(
                    $"[{m_Context.AppName}/SplashWork]프로세스 찾기실패",
                    "Cargo24sAct_App/SplashWorkAsync_03", bWrite, bMsgBox);
            }

            Debug.WriteLine($"[Cargo24sAct_App] ThreadId={mSplash.TopWnd_uThreadId}, ProcessId={mSplash.TopWnd_uProcessId}");

            // 아이디 텍스트박스의 핸들을 얻는다.
            mSplash.IdWnd_hWnd =
                Std32Window.GetWndHandle_FromRelDrawPt(mSplash.TopWnd_hWnd, m_FileInfo.Splash_IdWnd_ptChk);
            if (mSplash.IdWnd_hWnd == IntPtr.Zero)
            {
                return CommonFuncs_StdResult.ErrMsgResult_Error(
                    $"[{m_Context.AppName}/SplashWork]아이디 입력창 찾기실패",
                    "Cargo24sAct_App/SplashWorkAsync_04", bWrite, bMsgBox);
            }

            Debug.WriteLine($"[Cargo24sAct_App] 아이디 입력창 찾음: {mSplash.IdWnd_hWnd}");

            // 비밀번호 텍스트박스의 핸들을 얻는다.
            mSplash.PwWnd_hWnd =
                Std32Window.GetWndHandle_FromRelDrawPt(mSplash.TopWnd_hWnd, m_FileInfo.Splash_PwWnd_ptChk);
            if (mSplash.PwWnd_hWnd == IntPtr.Zero)
            {
                return CommonFuncs_StdResult.ErrMsgResult_Error(
                    $"[{m_Context.AppName}/SplashWork]비밀번호 입력창 찾기실패",
                    "Cargo24sAct_App/SplashWorkAsync_05", bWrite, bMsgBox);
            }

            Debug.WriteLine($"[Cargo24sAct_App] 비밀번호 입력창 찾음: {mSplash.PwWnd_hWnd}");

            #region Login 작업
            // Disable
            StdWin32.BlockInput(true); // BlockInput - 마우스와 키보드 입력을 차단한다
            string id = Std32Window.GetWindowCaption(mSplash.IdWnd_hWnd);
            string pw = Std32Window.GetWindowCaption(mSplash.PwWnd_hWnd);
            if (id != m_Context.Id) Std32Window.SetWindowCaption(mSplash.IdWnd_hWnd, m_Context.Id); // 아이디를 쓴다 // 경우의 수가 좀 있어서 우선 간편하게 처리
            if (pw != m_Context.Pw) Std32Window.SetWindowCaption(mSplash.PwWnd_hWnd, m_Context.Pw); // 비밀번호를 쓴다 // 경우의 수가 좀 있어서 우선 간편하게
                                                                                  // 엔터키를 누른다 - 이 방식도 됨
            Std32Key_Msg.KeyPost_Click(mSplash.PwWnd_hWnd, StdCommon32.VK_RETURN); // LoginBtn을 클릭해도 됨 - CheckReserved
            StdWin32.BlockInput(false); // BlockInput - 마우스와 키보드 입력을 해제한다
            await Task.Delay(300); // ~초 대기 - 너무 짧으면 안됨
            #endregion

            Debug.WriteLine($"[Cargo24sAct_App] 로그인 처리 완료");
            return null;
        }
        catch (Exception ex)
        {
            return CommonFuncs_StdResult.ErrMsgResult_Error(
                $"[{m_Context.AppName}/SplashWork]예외발생: {ex.Message}",
                "Cargo24sAct_App/SplashWorkAsync_99", bWrite, bMsgBox);
        }
        finally
        {
            // Enable
            StdWin32.BlockInput(false); // BlockInput - 마우스와 키보드 입력을 해제한다
            Std32Window.SetWindowTopMost(mSplash.TopWnd_hWnd, false); // TopMost를 해제한다
            await Task.Delay(100); // ~초 대기 - 너무 짧으면 안됨
        }
    }
    #endregion

    #region Close
    /// <summary>
    /// 화물24시 앱 종료
    /// - MainWindow 닫기 시도
    /// - SplashWindow 강제 종료
    /// </summary>
    public StdResult_Error Close(int nDelayMiliSec = 100)
    {
        Cargo24sInfo_Mem.MainWnd mMain = m_MemInfo.Main;
        Cargo24sInfo_Mem.SplashWnd mSplash = m_MemInfo.Splash;

        try
        {
            Debug.WriteLine($"[Cargo24sAct_App] Close 시작");

            // MainWindow가 Null이 아니면
            if (mMain.TopWnd_hWnd != IntPtr.Zero)
            {
                StdWin32.PostMessage(mMain.TopWnd_hWnd, StdCommon32.WM_SYSCOMMAND, StdCommon32.SC_CLOSE, 0); // 닫기
                Debug.WriteLine($"[Cargo24sAct_App] MainWindow 닫기 메시지 전송");

                // 3초 동안 확인 다이아로그 찾음
                IntPtr hWndDlg = IntPtr.Zero;
                IntPtr hWndBtn = IntPtr.Zero;

                for (int i = 0; i < 30; i++)
                {
                    hWndDlg = Std32Window.FindMainWindow(mSplash.TopWnd_uProcessId, "TMessageForm", "Confirm");
                    Thread.Sleep(100);
                    if (hWndDlg != IntPtr.Zero) break;
                }

                if (hWndDlg == IntPtr.Zero)
                {
                    return new StdResult_Error($"[{m_Context.AppName}/Close] 종료 다이아로그 찾기 실패", "Cargo24sAct_App/Close_01");
                }

                Debug.WriteLine($"[Cargo24sAct_App] 종료 다이아로그 찾음: {hWndDlg}");

                for (int i = 0; i < 30; i++)
                {
                    hWndBtn = Std32Window.FindWindowEx(hWndDlg, IntPtr.Zero, "TButton", "&Yes");
                    Thread.Sleep(100);
                    if (hWndBtn != IntPtr.Zero) break;
                }

                if (hWndBtn == IntPtr.Zero)
                {
                    return new StdResult_Error(
                        $"[{m_Context.AppName}/Close] 종료 다이아로그 - YES버튼 찾기 실패",
                        "Cargo24sAct_App/Close_02");
                }

                Debug.WriteLine($"[Cargo24sAct_App] YES 버튼 찾음: {hWndBtn}");

                Thread.Sleep(100);
                Std32Mouse_Post.MousePostAsync_ClickLeft(hWndBtn).GetAwaiter().GetResult();

                for (int i = 0; i < 30; i++) // 윈도가 없어지는지 체크
                {
                    if (!Std32Window.IsWindowVisible(mMain.TopWnd_hWnd))
                    {
                        Debug.WriteLine($"[Cargo24sAct_App] MainWindow 종료 확인됨");
                        return null;
                    }
                    Thread.Sleep(100);
                }

                return new StdResult_Error($"[{m_Context.AppName}/Close] 메인윈도 종료실패: {mMain.TopWnd_hWnd:X}", "Cargo24sAct_App/Close_03");
            }

            // SplashWnd가 Null이 아니면
            if (mSplash.TopWnd_hWnd != IntPtr.Zero)
            {
                // Close Insung - 필요하다면 Process를 죽이는 방법도 있음(화물24시 같이)
                Std32Window.PostCloseTwiceWindow(m_MemInfo.Splash.TopWnd_hWnd);
                Debug.WriteLine($"[Cargo24sAct_App] SplashWindow 닫기 메시지 전송");

                // 5초 동안 죽은거 확인..
                bool bShow = true;
                for (int i = 0; i < 50; i++)
                {
                    bShow = Std32Window.IsWindowVisible(m_MemInfo.Splash.TopWnd_hWnd);
                    if (!bShow) break;
                    Thread.Sleep(100);
                }

                if (bShow)
                {
                    return new StdResult_Error($"[{m_Context.AppName}/Close]스플래쉬윈도 종료실패", "Cargo24sAct_App/Close_04");
                }

                Debug.WriteLine($"[Cargo24sAct_App] SplashWindow 종료 확인됨");
                return null;
            }

            Debug.WriteLine($"[Cargo24sAct_App] Close 완료 (닫을 윈도우 없음)");
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Cargo24sAct_App] Close 실패: {ex.Message}");
            return new StdResult_Error($"[{m_Context.AppName}/Close]예외발생: {ex.Message}", "Cargo24sAct_App/Close_99");
        }
    }
    #endregion
}
#nullable restore
