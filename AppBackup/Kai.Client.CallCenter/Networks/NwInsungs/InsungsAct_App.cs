using System.Diagnostics;
using Draw = System.Drawing;

using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;
using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgWnd;

using Kai.Client.CallCenter.Classes;
using static Kai.Client.CallCenter.Classes.CommonVars;

namespace Kai.Client.CallCenter.Networks.NwInsungs;
#nullable disable

/// <summary>
/// 인성 앱 실행 및 스플래시 처리 담당 클래스
/// Context 패턴 사용: InsungContext를 통해 모든 정보에 접근
/// </summary>
public class InsungsAct_App
{
    #region Context Reference
    /// <summary>
    /// Context에 대한 읽기 전용 참조
    /// </summary>
    private readonly InsungContext m_Context;

    /// <summary>
    /// 편의를 위한 로컬 참조들
    /// </summary>뭐가 보여야해
    private InsungsInfo_File m_FileInfo => m_Context.FileInfo;
    private InsungsInfo_Mem m_MemInfo => m_Context.MemInfo;
    //private InsungsInfo_Mem.MainWnd m_Main => m_MemInfo.Main;
    //private InsungsInfo_Mem.SplashWnd m_Splash => m_MemInfo.Splash;
    #endregion

    #region Constructor
    /// <summary>
    /// 생성자 - Context를 받아서 초기화
    /// </summary>
    public InsungsAct_App(InsungContext context)
    {
        m_Context = context ?? throw new ArgumentNullException(nameof(context));
        //Debug.WriteLine($"[InsungsAct_App] 생성자 호출: AppName={m_Context.AppName}");
    }
    #endregion

    #region BeforeWork
    /// <summary>
    /// 앱 실행 전 준비 작업
    /// - Registry에서 앱 경로 찾기 (없으면 예측 경로 또는 사용자 선택)
    /// - 기존 프로세스 종료 (옵션)
    /// </summary>
    public async Task<StdResult_String> BeforeWorkAsync(string sRegNameOfAppPath, string sFolder, string sExecFile)
    {
        MsgBox($"{sRegNameOfAppPath}, {sFolder}, {sExecFile}");

        // AppPath 구하기 - 틀리면 Registry에 새로 찾은 경로를 저장한다
        StdResult_String result = await Task.Run(() =>
            NwCommon.GetAppPath(sRegNameOfAppPath, sFolder, sExecFile)
        );

        if (string.IsNullOrEmpty(result.strResult)) // 경로가 없으면 종료
            return result;

        // 혹시 이미 있는 인성 프로그램 죽이기 (필요시 주석 해제)
        // Splash Window가 있으면 죽인다
        //NwCommon.CloseSplash(null, m_FileInfo.Splash_TopWnd_sWndName);

        // Main Window가 있으면 죽인다
        //IntPtr hWnd = Std32Window.FindMainWindow_Reduct(null, m_FileInfo.Main_TopWnd_sWndNameReduct);
        //if (hWnd != IntPtr.Zero)
        //{
        //    Std32Window.PostCloseTwiceWindow(hWnd); // MainWindow를 닫는다
        //    await Task.Delay(1000); // 1초 대기 - 너무 짧으면 안됨
        //}

        return result;
    }
    #endregion

    #region UpdaterWork
    /// <summary>
    /// Updater 실행 및 종료 대기
    /// - Updater 프로세스 실행
    /// - 5분 동안 종료 대기
    /// </summary>
    public async Task<StdResult_Error> UpdaterWorkAsync(string sPath, bool bEdit = true, bool bWrite = true, bool bMsgBox = true)
    {
        // Updater 실행
        ProcessStartInfo processInfo = new ProcessStartInfo
        {
            UseShellExecute = true,
            Verb = "open",
            FileName = sPath
        };
        Process procExec = Process.Start(processInfo);
        if (procExec == null) return CommonFuncs_StdResult.ErrMsgResult_Status(StdResult.Fail,
                $"[{m_Context.AppName}/UpdaterWork]실행실패: procExec == null", "InsungsAct_App/UpdaterWorkAsync_01", bWrite, bMsgBox);

        // 프로세스가 종료될 때 이벤트를 받기 위해 EnableRaisingEvents를 true로 설정
        procExec.EnableRaisingEvents = true;
        bool bClosed = false;
        procExec.Exited += (sender, e) =>
        {
            bClosed = true;
        };

        // 300초(5분) 동안 대기
        for (int i = 0; i < 3000; i++)
        {
            await Task.Delay(c_nWaitNormal);
            if (bClosed) break;
        }
        await Task.Delay(c_nWaitNormal);

        // 프로세스가 종료되지 않았으면 에러
        if (!bClosed) return new StdResult_Error(
                $"[{m_Context.AppName}/UpdaterWork]업데이터 (5분안에)종료실패", "InsungsAct_App/UpdaterWorkAsync_02", bWrite, bMsgBox);

        return null;
    }
    #endregion

    #region SplashWork
    /// <summary>
    /// 스플래시 창 처리 및 로그인
    /// 1. 스플래시 윈도우 찾기
    /// 2. 화면 중앙으로 이동
    /// 3. 프로세스/스레드 ID 얻기
    /// 4. ID/PW 입력창 찾기
    /// 5. 로그인 처리
    /// 6. 팝업 다이얼로그 처리
    /// </summary>
    public async Task<StdResult_Error> SplashWorkAsync(bool bEdit = true, bool bWrite = true, bool bMsgBox = true)
    {
        IntPtr hWndTmp = IntPtr.Zero;
        Draw.Rectangle rcCur = StdUtil.s_rcDrawEmpty;

        try
        {
            // 1. 스플래시 윈도우 찾기 (10초 대기)
            for (int i = 0; i < c_nRepeatVeryMany; i++) // 10초 동안
            {
                m_Context.MemInfo.Splash.TopWnd_hWnd = StdWin32.FindWindow(null, m_Context.FileInfo.Splash_TopWnd_sWndName);
                await Task.Delay(c_nWaitNormal); // 무조건 대기
                if (m_Context.MemInfo.Splash.TopWnd_hWnd != IntPtr.Zero) break;
            }

            if (m_Context.MemInfo.Splash.TopWnd_hWnd == IntPtr.Zero)
            {
                return new StdResult_Error(
                    $"[{m_Context.AppName}/SplashWork]스플래시윈도 찾기실패[{m_Context.FileInfo.Splash_TopWnd_sWndName}]",
                    "InsungsAct_App/SplashWorkAsync_01", bWrite, bMsgBox);
            }
            //Debug.WriteLine($"[InsungsAct_App] 스플래시 윈도우 찾음: {m_Context.MemInfo.Splash.TopWnd_hWnd}");

            // 2. TopMost 설정 및 화면 중앙으로 이동
            Std32Window.SetWindowTopMost(m_Context.MemInfo.Splash.TopWnd_hWnd, true);

            rcCur = Std32Window.GetWindowRect_DrawAbs(m_Context.MemInfo.Splash.TopWnd_hWnd);
            Draw.Rectangle rcNew = s_Screens.m_WorkingMonitor.GetCenterDrawRectangle(rcCur);
            StdWin32.MoveWindow(m_Context.MemInfo.Splash.TopWnd_hWnd, rcNew.X, rcNew.Y, rcNew.Width, rcNew.Height, true);

            // 위치 이동 확인 (10초 대기)
            for (int i = 0; i < c_nRepeatVeryMany; i++)
            {
                hWndTmp = Std32Window.GetWndHandle_FromAbsDrawPos(rcNew.X, rcNew.Y);
                if (hWndTmp != IntPtr.Zero && hWndTmp == m_Context.MemInfo.Splash.TopWnd_hWnd) break;
                await Task.Delay(c_nWaitNormal);
            }

            if (hWndTmp != m_Context.MemInfo.Splash.TopWnd_hWnd)
            {
                return new StdResult_Error(
                    $"[{m_Context.AppName}/SplashWork]스플래시윈도 위치이동실패", "InsungsAct_App/SplashWorkAsync_02", bWrite, bMsgBox);
            }
            await Task.Delay(1000); // 1초 대기
            //Debug.WriteLine($"[InsungsAct_App] 스플래시 윈도우 중앙 이동 완료");

            // 3. ThreadId, ProcessId 얻기
            m_Context.MemInfo.Splash.TopWnd_uThreadId = Std32Window.GetWindowThreadProcessId(
                m_Context.MemInfo.Splash.TopWnd_hWnd,
                out m_Context.MemInfo.Splash.TopWnd_uProcessId);

            if (m_Context.MemInfo.Splash.TopWnd_uThreadId == 0)
            {
                return new StdResult_Error(
                    $"[{m_Context.AppName}/SplashWork]프로세스 찾기실패",
                    "InsungsAct_App/SplashWorkAsync_03", bWrite, bMsgBox);
            }
            //Debug.WriteLine($"[InsungsAct_App] ThreadId={m_Context.MemInfo.Splash.TopWnd_uThreadId}, ProcessId={m_Context.MemInfo.Splash.TopWnd_uProcessId}");

            // 4. ID/PW 입력창 핸들 얻기
            // 아이디 텍스트박스의 핸들을 얻는다
            m_Context.MemInfo.Splash.IdWnd_hWnd =
                Std32Window.GetWndHandle_FromRelDrawPt(m_Context.MemInfo.Splash.TopWnd_hWnd, m_Context.FileInfo.Splash_IdWnd_ptChk);
            //Std32Cursor.SetCursorPos_RelDrawPos(m_Context.MemInfo.Splash.IdWnd_hWnd, 0, 0); // Test
            if (m_Context.MemInfo.Splash.IdWnd_hWnd == IntPtr.Zero)
            {
                return new StdResult_Error(
                    $"[{m_Context.AppName}/SplashWork]아이디 입력창 찾기실패",
                    "InsungsAct_App/SplashWorkAsync_04", bWrite, bMsgBox);
            }
            //Debug.WriteLine($"[InsungsAct_App] 아이디 입력창 찾음: {m_Context.MemInfo.Splash.IdWnd_hWnd}");

            // 비밀번호 텍스트박스의 핸들을 얻는다
            m_Context.MemInfo.Splash.PwWnd_hWnd =
                Std32Window.GetWndHandle_FromRelDrawPt(m_Context.MemInfo.Splash.TopWnd_hWnd, m_Context.FileInfo.Splash_PwWnd_ptChk);
            //Std32Cursor.SetCursorPos_RelDrawPos(m_Context.MemInfo.Splash.PwWnd_hWnd, 0, 0); // Test
            if (m_Context.MemInfo.Splash.PwWnd_hWnd == IntPtr.Zero)
            {
                return new StdResult_Error(
                    $"[{m_Context.AppName}/SplashWork]비밀번호 입력창 찾기실패",
                    "InsungsAct_App/SplashWorkAsync_05", bWrite, bMsgBox);
            }
            //Debug.WriteLine($"[InsungsAct_App] 비밀번호 입력창 찾음: {m_Context.MemInfo.Splash.PwWnd_hWnd}");

            #region 5. 로그인 작업           
            StdWin32.BlockInput(true); // BlockInput - 마우스와 키보드 입력을 차단한다
            string id = Std32Window.GetWindowCaption(m_Context.MemInfo.Splash.IdWnd_hWnd);
            string pw = Std32Window.GetWindowCaption(m_Context.MemInfo.Splash.PwWnd_hWnd);
            //MsgBox($"[{m_Context.AppName}/SplashWork]: {id}, {pw}"); // Test
            if (id != m_Context.Id) Std32Window.SetWindowCaption(m_Context.MemInfo.Splash.IdWnd_hWnd, m_Context.Id); // 아이디를 쓴다
            if (pw != m_Context.Pw) Std32Window.SetWindowCaption(m_Context.MemInfo.Splash.PwWnd_hWnd, m_Context.Pw); // 비밀번호를 쓴다
            //Std32Window.SetWindowTopMost(m_Context.MemInfo.Splash.TopWnd_hWnd, false); // TopMost를 해제한다
            //Std32Key_Msg.KeyPost_Click(m_Context.MemInfo.Splash.PwWnd_hWnd, StdCommon32.VK_RETURN); // 아래의 LoginBtn을 클릭해도 됨
            await Std32Key_Msg.KeyPost_DownAsync(m_Context.MemInfo.Splash.PwWnd_hWnd, StdCommon32.VK_RETURN); // 엔터키를 누른다
            //Std32Mouse_Post.MousePost_ChkNclickLeft_ptRel(m_Context.MemInfo.Splash.hWndLoginBtn); // 이 방식도 됨
            StdWin32.BlockInput(false); // BlockInput - 마우스와 키보드 입력을 해제한다
            await Task.Delay(300); // 0.3초 대기 - 너무 짧으면 안됨
            Debug.WriteLine($"[InsungsAct_App] 로그인 처리 완료");
            #endregion

            #region 6. MapSDK 다이얼로그 죽이기 (백그라운드 스레드)
            Thread t = new Thread(async () =>
            {
                IntPtr hWndMapSDK = IntPtr.Zero;
                IntPtr hWndBtn = IntPtr.Zero;

                for (int i = 0; i < c_nRepeatVeryMany; i++) // 10초 동안
                {
                    Thread.Sleep(c_nWaitNormal);

                    hWndMapSDK = Std32Window.FindMainWindow(m_Context.MemInfo.Splash.TopWnd_uProcessId, "#32770", "");
                    if (hWndMapSDK != IntPtr.Zero)
                    {
                        hWndBtn = Std32Window.FindWindowEx(hWndMapSDK, IntPtr.Zero, "Button", "확인");
                        if (hWndBtn != IntPtr.Zero)
                        {
                            await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndBtn);
                            StdWin32.SendMessage(hWndMapSDK, StdCommon32.WM_SYSCOMMAND, Std32Window.SC_CLOSE, 0);
                            StdWin32.SendMessage(hWndMapSDK, StdCommon32.WM_CLOSE, 0, 0);
                            break;
                        }
                    }

                    hWndTmp = Std32Window.FindMainWindow(m_Context.MemInfo.Splash.TopWnd_uProcessId, "#32770", "확인");
                    if (hWndTmp != IntPtr.Zero)
                    {
                        hWndBtn = StdWin32.FindWindowEx(hWndTmp, IntPtr.Zero, "Button", "확인");
                        if (hWndBtn != IntPtr.Zero)
                        {
                            await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndBtn);

                            Thread tt = new Thread(async () =>
                            {
                                for (int i = 0; i < 15; i++) // 1.5초 동안
                                {
                                    Thread.Sleep(c_nWaitNormal);
                                    hWndTmp = StdWin32.FindWindow(null, "비밀번호변경");
                                    if (hWndTmp != IntPtr.Zero)
                                    {
                                        hWndBtn = StdWin32.FindWindowEx(hWndTmp, IntPtr.Zero, null, "나중에 변경");
                                        if (hWndBtn != IntPtr.Zero)
                                        {
                                            await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndBtn);

                                            Thread ttt = new Thread(async () =>
                                            {
                                                Thread.Sleep(c_nWaitNormal);
                                                hWndTmp = Std32Window.FindMainWindow(m_Context.MemInfo.Splash.TopWnd_uProcessId, "#32770", "확인");
                                                if (hWndTmp != IntPtr.Zero)
                                                {
                                                    hWndBtn = StdWin32.FindWindowEx(hWndTmp, IntPtr.Zero, "Button", "확인");
                                                    if (hWndBtn != IntPtr.Zero) await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndBtn);
                                                }
                                            });

                                            ttt.IsBackground = true;
                                            ttt.Start();
                                        }
                                    }
                                }
                            });
                            tt.IsBackground = true;
                            tt.Start();
                        }
                    }
                }
            });
            t.IsBackground = true;
            t.Start();
            #endregion

            #region 7. 기타 팝업 다이얼로그 처리 (백그라운드 스레드)
            // 필요없을땐 주석처리 - 음성메세지(VMS)서비스 이용 안내 등
            Thread t2 = new Thread(() =>
            {
                IntPtr hWndTmp2 = IntPtr.Zero;

                for (int i = 0; i < 100; i++) // 5초 동안
                {
                    Thread.Sleep(c_nWaitShort);

                    hWndTmp2 = StdWin32.FindWindow(null, "오토바이 신규기사 범죄이력 조회 업데이트 안내");
                    if (hWndTmp2 != IntPtr.Zero)
                        StdWin32.SendMessage(hWndTmp2, StdCommon32.WM_SYSCOMMAND, StdCommon32.SC_CLOSE, 0);
                }
            });
            t2.IsBackground = true;
            t2.Start();
            #endregion

            return null; // 성공
        }
        catch (Exception ex)
        {
            return new StdResult_Error(
                $"[{m_Context.AppName}/SplashWork]예외발생: {ex.Message}", "InsungsAct_App/SplashWorkAsync_999", bWrite, bMsgBox);
        }
        finally
        {
            StdWin32.BlockInput(false); // BlockInput 해제
            // TopMost 해제는 나중에 추가
            await Task.Delay(c_nWaitNormal);
        }
    }
    #endregion

    #region Close
    /// <summary>
    /// 인성 앱 종료
    /// - MainWindow 닫기 시도
    /// - SplashWindow 강제 종료
    /// </summary>
    public StdResult_Error Close(int nDelayMiliSec = 100)
    {
        try
        {
            // MainWindow가 Null이 아니면
            if (m_Context.MemInfo.Main.TopWnd_hWnd != IntPtr.Zero)
            {
                if (!StdWin32.IsWindowVisible(m_Context.MemInfo.Main.TopWnd_hWnd))
                {
                    Debug.WriteLine($"[InsungsAct_App] MainWindow가 이미 닫혔습니다.");
                    return null; // 이미 닫혔으면 리턴
                }

                StdWin32.PostMessage(m_Context.MemInfo.Main.TopWnd_hWnd, StdCommon32.WM_SYSCOMMAND, Std32Window.SC_CLOSE, 0); // 닫기
                Debug.WriteLine($"[InsungsAct_App] MainWindow 닫기 메시지 전송");
                //MsgBox($"Close"); // Test

                #region 만약 막히면 이방법을 쓴다
                //Draw.Point ptChk = s_Screens.m_WorkingMonitor._ptLeftTop;

                //// 해당위치의 윈도가 원하는 윈도인지
                //IntPtr hWndFind = Std32Window.GetParentWndHandle_FromAbsDrawPt(ptChk);
                ////MsgBox($"[{m_Context.AppName}/Close] 메인윈도 찾기: {hWndFind}, {hWndMain}"); // Test

                //if (hWndFind != m_Context.MemInfo.Main.TopWnd_hWnd)
                //{
                //    new Thread(() =>
                //    {
                //       //Std32Window.SetWindowTopMost(hWndFind, false);
                //        StdWin32.ShowWindow(m_Context.MemInfo.Main.TopWnd_hWnd, StdCommon32.SW_SHOW);
                //       //Std32Window.SetWindowTopMost(m_Context.MemInfo.Main.TopWnd_hWnd, true);
                //    }).Start();
                //}

                //for (int i = 0; i < 50; i++)
                //{
                //    hWndFind = Std32Window.GetParentWndHandle_FromAbsDrawPt(ptChk);
                //    if (hWndFind == m_Context.MemInfo.Main.TopWnd_hWnd) break;
                //    Thread.Sleep(100);
                //}
                //if (hWndFind != m_Context.MemInfo.Main.TopWnd_hWnd)
                //{
                //    ErrMsgBox($"[{m_Context.AppName}/Close] 메인윈도 찾기 실패");
                //    return;
                //}
                ////MsgBox($"[{m_Context.AppName}/Close] 메인윈도 찾음: {hWndFind}, {hWndMain}"); // Test

                //CtrlCppFuncs.SetMouseLock(true);
                //await Task.Delay(nDelayMiliSec);
                //MyWin32.MouseSend_ClickLeft_Rel_ToTopRightPost(m_Context.MemInfo.Main.TopWnd_hWnd, true, 30, 20);
                //CtrlCppFuncs.SetMouseLock(false);

                //    // 3초 동안 확인 다이아로그 찾음
                //    IntPtr hWndDlg = IntPtr.Zero;
                //    IntPtr hWndBtn = IntPtr.Zero;

                //    for (int i = 0; i < 30; i++)
                //    {
                //        hWndDlg = MyWin32.FindMainWindow(m_Context.MemInfo.Splash.TopWnd_uProcessId, "#32770", "종료");
                //        Thread.Sleep(100);
                //        if (hWndDlg != IntPtr.Zero) break;
                //    }
                //    if (hWndDlg == IntPtr.Zero)
                //    {
                //        ErrMsgBox($"[{m_Context.AppName}/Close] 종료 다이아로그 찾기 실패");
                //        return;
                //    }
                //    //MsgBox($"[{m_Context.AppName}/Close] 종료 다이아로그 찾음: {hWndDlg}"); // Test

                //    for (int i = 0; i < 30; i++)
                //    {
                //        hWndBtn = MyWin32.FindWindowEx(hWndDlg, IntPtr.Zero, "Button", "예(&Y)");
                //        Thread.Sleep(100);
                //        if (hWndBtn != IntPtr.Zero) break;
                //    }
                //    if (hWndBtn == IntPtr.Zero)
                //    {
                //        ErrMsgBox($"[{m_Context.AppName}/Close] 종료 다이아로그 - YES버튼 찾기 실패");
                //        return;
                //    }

                //    CtrlCppFuncs.SetMouseLock(true);
                //    await Task.Delay(100);
                //    MyWin32.MousePost_ClickLeft_Rel(hWndBtn);
                //    CtrlCppFuncs.SetMouseLock(false);
                //    for (int i = 0; i < 30; i++)
                //    {
                //        hWndFind = MyWin32.GetParentWndHandle_FromAbsDrawPt(ptChk);
                //        if (hWndFind != m_Context.MemInfo.Main.TopWnd_hWnd) break;
                //        Thread.Sleep(100);
                //    }
                #endregion
            }

            // SplashWnd가 Null이 아니면
            Thread.Sleep(nDelayMiliSec);
            if (m_Context.MemInfo.Splash.TopWnd_hWnd != IntPtr.Zero)
            {
                // Close Insung - 필요하다면 Process를 죽이는 방법도 있음(화물24시 같이)
                Std32Window.PostCloseTwiceWindow(m_Context.MemInfo.Splash.TopWnd_hWnd);
                Debug.WriteLine($"[InsungsAct_App] SplashWindow 닫기 메시지 전송");

                // 5초 동안 죽은거 확인
                bool bShow = true;
                for (int i = 0; i < c_nRepeatMany; i++)
                {
                    bShow = Std32Window.IsWindowVisible(m_Context.MemInfo.Splash.TopWnd_hWnd);
                    if (!bShow) break;
                    Thread.Sleep(c_nWaitNormal);
                }

                if (bShow)
                {
                    return new StdResult_Error(
                        $"[{m_Context.AppName}/Close]스플래시윈도 종료실패", "InsungsAct_App/Close_01");
                }

                Debug.WriteLine($"[InsungsAct_App] SplashWindow 종료 확인");
                return null;
            }

            return null;
        }
        catch (Exception ex)
        {
            return new StdResult_Error(
                $"[{m_Context.AppName}/Close]예외발생: {ex.Message}", "InsungsAct_App/Close_99");
        }
    }
    #endregion
}
#nullable enable
