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

/// <summary>
/// 원콜 앱 제어 (프로세스 시작, 로그인 처리 등)
/// </summary>
public class OnecallAct_App
{
    #region Private Fields
    //private readonly OnecallContext m_Context;
    //private OnecallInfo_File fInfo => m_Context.FileInfo;
    //private OnecallInfo_Mem mInfo => m_Context.MemInfo;
    //private string AppName => m_Context.AppName;
    #endregion

    #region 생성자
    //public OnecallAct_App(OnecallContext context)
    //{
    //    m_Context = context;
    //}
    #endregion

    #region UpdaterWorkAsync
    ///// <summary>
    ///// 앱 실행 및 Splash 윈도우 대기
    ///// </summary>
    //public async Task<StdResult_Error> UpdaterWorkAsync(string sPath)
    //{
    //    try
    //    {
    //        //Debug.WriteLine($"[{AppName}] UpdaterWorkAsync 시작: {sPath}");

    //        // 1. 기존 프로세스 종료
    //        await Application.Current.Dispatcher.InvokeAsync(() =>
    //        {
    //            bool b = NwCommon.CloseSplash(null, fInfo.Splash_TopWnd_sWndName);
    //            if (!b)
    //            {
    //                Debug.WriteLine($"[{AppName}] 기존 프로세스 종료 실패 (무시)");
    //            }
    //        });

    //        // 2. 앱 실행
    //        ProcessStartInfo processInfo = new ProcessStartInfo
    //        {
    //            UseShellExecute = true,
    //            FileName = sPath
    //        };
    //        Process procExec = Process.Start(processInfo);
    //        if (procExec == null)
    //            return new StdResult_Error($"[{AppName}] 실행실패: procExec == null", "OnecallAct_App/UpdaterWorkAsync_01");

    //        // 3. Splash 윈도우 대기 (5분)
    //        procExec.EnableRaisingEvents = true;
    //        bool bClosed = false;
    //        procExec.Exited += (sender, e) => { bClosed = true; };

    //        for (int i = 0; i < 3000; i++) // 300초
    //        {
    //            await Task.Delay(c_nWaitNormal);

    //            if (bClosed) break;

    //            mInfo.Splash.TopWnd_hWnd = Std32Window.FindWindow(null, fInfo.Splash_TopWnd_sWndName);
    //            if (mInfo.Splash.TopWnd_hWnd != IntPtr.Zero) break;
    //        }

    //        if (!bClosed && mInfo.Splash.TopWnd_hWnd == IntPtr.Zero)
    //            return new StdResult_Error($"[{AppName}] 업데이터 (5분안에)종료실패", "OnecallAct_App/UpdaterWorkAsync_02");

    //        //Debug.WriteLine($"[{AppName}] UpdaterWorkAsync 완료");
    //        return null;
    //    }
    //    catch (Exception ex)
    //    {
    //        return new StdResult_Error($"[{AppName}] UpdaterWorkAsync 예외: {ex.Message}", "OnecallAct_App/UpdaterWorkAsync_99");
    //    }
    //}
    #endregion

    #region SplashWorkAsync
    ///// <summary>
    ///// Splash 윈도우 로그인 처리
    ///// </summary>
    //public async Task<StdResult_Error> SplashWorkAsync(string sAppName, string sId, string sPw)
    //{
    //    OnecallInfo_Mem.SplashWnd mSplash = mInfo.Splash;
    //    Draw.Rectangle rcCur = StdUtil.s_rcDrawEmpty;

    //    try
    //    {
    //        //Debug.WriteLine($"[{AppName}] SplashWorkAsync 시작");

    //        // 1. Splash 윈도우 확인
    //        if (mSplash.TopWnd_hWnd == IntPtr.Zero)
    //        {
    //            return new StdResult_Error($"[{AppName}] 스플래쉬윈도 찾기실패[{fInfo.Splash_TopWnd_sWndName}]",
    //                "OnecallAct_App/SplashWorkAsync_01");
    //        }

    //        await Task.Delay(c_nWaitVeryLong);

    //        // 2. Splash 윈도우를 화면 중앙으로 이동
    //        rcCur = Std32Window.GetWindowRect_DrawAbs(mSplash.TopWnd_hWnd);
    //        Draw.Rectangle rcNew = s_Screens.m_WorkingMonitor.GetCenterDrawRectangle(rcCur);
    //        StdWin32.MoveWindow(mSplash.TopWnd_hWnd, rcNew.X, rcNew.Y, rcNew.Width, rcNew.Height, true);

    //        for (int i = 0; i < c_nRepeatNormal; i++)
    //        {
    //            rcCur = Std32Window.GetWindowRect_DrawAbs(mSplash.TopWnd_hWnd);
    //            if (rcCur == rcNew) break;
    //            await Task.Delay(c_nWaitNormal);
    //        }

    //        if (rcCur != rcNew)
    //            return new StdResult_Error($"[{AppName}] 스플래쉬윈도 위치이동실패", "OnecallAct_App/SplashWorkAsync_02");

    //        // 3. TopMost 설정
    //        Std32Window.SetWindowTopMost(mSplash.TopWnd_hWnd, true);
    //        await Task.Delay(1000); // 1초 대기

    //        // 4. 공지사항 팝업 닫기
    //        IntPtr hWndNotice = Std32Window.FindWindow(null, "공지사항");
    //        if (hWndNotice != IntPtr.Zero)
    //        {
    //            Std32Window.PostCloseTwiceWindow(hWndNotice);
    //            await Task.Delay(c_nWaitVeryLong);
    //        }

    //        // 5. ThreadId, ProcessId 얻기
    //        mSplash.TopWnd_uThreadId = Std32Window.GetWindowThreadProcessId(mSplash.TopWnd_hWnd, out mSplash.TopWnd_uProcessId);
    //        if (mSplash.TopWnd_uThreadId == 0)
    //        {
    //            return new StdResult_Error($"[{AppName}] 프로세스 찾기실패", "OnecallAct_App/SplashWorkAsync_03");
    //        }

    //        // 6. ID 입력창 찾기
    //        mSplash.IdWnd_hWnd = Std32Window.GetWndHandle_FromRelDrawPt(mSplash.TopWnd_hWnd, fInfo.Splash_IdWnd_ptChk);
    //        if (mSplash.IdWnd_hWnd == IntPtr.Zero)
    //        {
    //            return new StdResult_Error($"[{AppName}] 아이디 입력창 찾기실패", "OnecallAct_App/SplashWorkAsync_04");
    //        }

    //        // 7. PW 입력창 찾기
    //        mSplash.PwWnd_hWnd = Std32Window.GetWndHandle_FromRelDrawPt(mSplash.TopWnd_hWnd, fInfo.Splash_PwWnd_ptChk);
    //        if (mSplash.PwWnd_hWnd == IntPtr.Zero)
    //        {
    //            return new StdResult_Error($"[{AppName}] 비밀번호 입력창 찾기실패", "OnecallAct_App/SplashWorkAsync_05");
    //        }

    //        // 8. 로그인 처리
    //        StdWin32.BlockInput(true);

    //        string id = Std32Window.GetWindowCaption(mSplash.IdWnd_hWnd);
    //        string pw = Std32Window.GetWindowCaption(mSplash.PwWnd_hWnd);

    //        if (id != sId) Std32Window.SetWindowCaption(mSplash.IdWnd_hWnd, sId);
    //        if (pw != sPw) Std32Window.SetWindowCaption(mSplash.PwWnd_hWnd, sPw);

    //        // 9. 로그인 버튼 클릭
    //        IntPtr hWndLogin = Std32Window.GetWndHandle_FromRelDrawPt(mSplash.TopWnd_hWnd, fInfo.Splash_LoginBtn_ptChk);
    //        if (hWndLogin == IntPtr.Zero)
    //        {
    //            return new StdResult_Error($"[{AppName}] 로그인 버튼 찾기실패", "OnecallAct_App/SplashWorkAsync_06");
    //        }

    //        await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndLogin);
    //        StdWin32.BlockInput(false);

    //        //Debug.WriteLine($"[{AppName}] SplashWorkAsync 완료");
    //    }
    //    catch (Exception ex)
    //    {
    //        return new StdResult_Error($"[{AppName}] SplashWorkAsync 예외: {ex.Message}", "OnecallAct_App/SplashWorkAsync_99");
    //    }
    //    finally
    //    {
    //        StdWin32.BlockInput(false);
    //        Std32Window.SetWindowTopMost(mSplash.TopWnd_hWnd, false);
    //        await Task.Delay(c_nWaitNormal);
    //    }

    //    return null;
    //}
    #endregion

    #region Close
    ///// <summary>
    ///// 앱 종료
    ///// </summary>
    //public StdResult_Error Close(int nDelayMiliSec = 100)
    //{
    //    OnecallInfo_Mem.MainWnd mMain = mInfo.Main;
    //    OnecallInfo_Mem.SplashWnd mSplash = mInfo.Splash;

    //    try
    //    {
    //        Debug.WriteLine($"[{AppName}] Close 시작");

    //        // MainWindow 종료
    //        if (mMain.TopWnd_hWnd != IntPtr.Zero)
    //        {
    //            if (!Std32Window.IsWindowVisible(mMain.TopWnd_hWnd)) return null;

    //            StdWin32.PostMessage(mMain.TopWnd_hWnd, WM_SYSCOMMAND, SC_CLOSE, 0);

    //            for (int i = 0; i < 30; i++)
    //            {
    //                if (!StdWin32.IsWindowVisible(mMain.TopWnd_hWnd))
    //                {
    //                    Debug.WriteLine($"[{AppName}] MainWnd 종료 성공");
    //                    return null;
    //                }
    //                Thread.Sleep(c_nWaitNormal);
    //            }
    //        }

    //        // SplashWnd 종료
    //        if (mSplash.TopWnd_hWnd != IntPtr.Zero)
    //        {
    //            Std32Window.PostCloseTwiceWindow(mSplash.TopWnd_hWnd);

    //            bool bShow = true;
    //            for (int i = 0; i < 50; i++)
    //            {
    //                bShow = Std32Window.IsWindowVisible(mSplash.TopWnd_hWnd);
    //                if (!bShow) break;
    //                Thread.Sleep(c_nWaitNormal);
    //            }

    //            if (bShow)
    //            {
    //                return new StdResult_Error($"[{AppName}] 스플래쉬윈도 종료실패", "OnecallAct_App/Close_01");
    //            }
    //        }

    //        Debug.WriteLine($"[{AppName}] Close 완료");
    //        return null;
    //    }
    //    catch (Exception ex)
    //    {
    //        return new StdResult_Error($"[{AppName}] Close 예외: {ex.Message}", "OnecallAct_App/Close_99");
    //    }
    //}
    #endregion
}
#nullable restore
