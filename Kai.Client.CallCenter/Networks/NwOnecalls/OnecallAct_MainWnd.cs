using System.Diagnostics;
using Draw = System.Drawing;

using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;
using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgBox;

using Kai.Client.CallCenter.Classes;
using static Kai.Client.CallCenter.Classes.CommonVars;

namespace Kai.Client.CallCenter.Networks.NwOnecalls;
#nullable disable

/// <summary>
/// 원콜 메인 윈도우 제어
/// </summary>
public class OnecallAct_MainWnd
{
    #region Private Fields
    //private readonly OnecallContext m_Context;
    //private OnecallInfo_File fInfo => m_Context.FileInfo;
    //private OnecallInfo_Mem mInfo => m_Context.MemInfo;
    //private OnecallInfo_Mem.MainWnd mMain => mInfo.Main;
    //private OnecallInfo_Mem.SplashWnd mSplash => mInfo.Splash;
    //private string AppName => m_Context.AppName;
    
    //private List<IntPtr> m_SavedWnds = new List<IntPtr>();
    #endregion


    #region 생성자
    //public OnecallAct_MainWnd(OnecallContext context)
    //{
    //    m_Context = context;
    //}
    #endregion

    #region InitAsync
    ///// <summary>
    ///// 메인 윈도우 초기화 (찾기, 이동, 최대화, 자식 윈도우)
    ///// </summary>
    //public async Task<StdResult_Error> InitAsync()
    //{
    //    try
    //    {
    //        // 1. 메인 윈도우 찾기 (10초)
    //        for (int i = 0; i < 100; i++)
    //        {
    //            await Task.Run(() =>
    //            {
    //                mMain.TopWnd_hWnd = Std32Window.FindMainWindow_Reduct(
    //                    mSplash.TopWnd_uProcessId, null, fInfo.Main_TopWnd_sWndNameReduct);
    //            });

    //            await Task.Delay(c_nWaitNormal);
    //            if (mMain.TopWnd_hWnd != IntPtr.Zero) break;
    //        }

    //        if (mMain.TopWnd_hWnd == IntPtr.Zero)
    //            return new StdResult_Error($"[{AppName}] 메인윈도 찾기실패", "OnecallAct_MainWnd/InitAsync_01");

    //        // 2. 추가 팝업 윈도우 처리 (1초 동안 새 윈도우 최소화/닫기)
    //        await HandlePopupWindowsAsync();

    //        // 3. 메인 윈도우 이동 및 최대화
    //        var moveResult = await MoveAndMaximizeMainWindowAsync();
    //        if (moveResult != null) return moveResult;

    //        // 4. 자식 윈도우 찾기
    //        mMain.FirstLayer_ChildWnds = Std32Window.GetChildWindows_FirstLayer(mMain.TopWnd_hWnd);
    //        if (mMain.FirstLayer_ChildWnds.Count == 0)
    //            return new StdResult_Error($"[{AppName}] 자식윈도 못찾음", "OnecallAct_MainWnd/InitAsync_03");

    //        // 5. MainMenu 찾기
    //        mMain.WndInfo_MainMenu = mMain.FirstLayer_ChildWnds.FirstOrDefault(x => x.rcRel == fInfo.Main_MainMenu_rcRelF);
    //        if (mMain.WndInfo_MainMenu == null)
    //            return new StdResult_Error($"[{AppName}] 메인메뉴 못찾음", "OnecallAct_MainWnd/InitAsync_04");

    //        // 6. MdiClient 찾기
    //        mMain.WndInfo_MdiClient = mMain.FirstLayer_ChildWnds.FirstOrDefault(x => x.rcRel == fInfo.Main_MdiClient_rcRelF);
    //        if (mMain.WndInfo_MdiClient == null)
    //            return new StdResult_Error($"[{AppName}] MdiClient 못찾음", "OnecallAct_MainWnd/InitAsync_05");

    //        return null;
    //    }
    //    catch (Exception ex)
    //    {
    //        return new StdResult_Error($"[{AppName}] MainWnd 예외: {ex.Message}", "OnecallAct_MainWnd/InitAsync_99");
    //    }
    //}
    #endregion

    #region Private Methods
    ///// <summary>
    ///// 팝업 윈도우 처리 (3초 동안 새 윈도우 닫기)
    ///// </summary>
    //private async Task HandlePopupWindowsAsync()
    //{
    //    Std32Process.GetWindowThreadProcessId(mMain.TopWnd_hWnd, out uint uProcessId);
    //    DateTime last = DateTime.Now;

    //    for (int i = 0; i < 100; i++)
    //    {
    //        await Task.Delay(100);
    //        if ((DateTime.Now - last).TotalSeconds > 1) break;

    //        List<IntPtr> lstCurWnds = Std32Window.FindMainWindows_SameProcessId(uProcessId);

    //        foreach (IntPtr hWnd in lstCurWnds)
    //        {
    //            if (m_SavedWnds.Contains(hWnd)) continue;
    //            if (hWnd == mMain.TopWnd_hWnd || hWnd == mSplash.TopWnd_hWnd) continue;

    //            string caption = Std32Window.GetWindowCaption(hWnd);
    //            if (!StdUtil.ContainsHangul(caption)) continue;

    //            m_SavedWnds.Add(hWnd);

    //            Thread t = new Thread(() =>
    //            {
    //                Thread.Sleep(2000);
    //                StdWin32.PostMessage(hWnd, StdCommon32.WM_SYSCOMMAND, StdCommon32.SC_CLOSE, 0);
    //            });
    //            t.IsBackground = true;
    //            t.Start();

    //            last = DateTime.Now;
    //        }
    //    }
    //}

    ///// <summary>
    ///// 메인 윈도우 이동 및 최대화
    ///// </summary>
    //private async Task<StdResult_Error> MoveAndMaximizeMainWindowAsync()
    //{
    //    IntPtr hWndFind = IntPtr.Zero;
    //    Draw.Point ptTarget = s_Screens.m_WorkingMonitor._ptLeftTop;

    //    await Task.Run(async () =>
    //    {
    //        // Normal Size로 변경
    //        StdWin32.ShowWindow(mMain.TopWnd_hWnd, (int)StdCommon32.SW_NORMAL);
    //        Draw.Rectangle rcMain = Std32Window.GetWindowRect_DrawAbs(mMain.TopWnd_hWnd);

    //        // 작업 모니터로 이동
    //        StdWin32.MoveWindow(mMain.TopWnd_hWnd,
    //            s_Screens.m_WorkingMonitor.PositionX, s_Screens.m_WorkingMonitor.PositionY,
    //            rcMain.Width, rcMain.Height, true);

    //        // 최대화 및 확인
    //        for (int i = 0; i < c_nRepeatNormal; i++)
    //        {
    //            StdWin32.PostMessage(mMain.TopWnd_hWnd, StdCommon32.WM_SYSCOMMAND, StdCommon32.SC_MAXIMIZE, IntPtr.Zero);
    //            await Task.Delay(c_nWaitNormal);

    //            hWndFind = Std32Window.GetParentWndHandle_FromAbsDrawPt(ptTarget);
    //            if (hWndFind == mMain.TopWnd_hWnd) break;
    //        }
    //    });

    //    if (hWndFind != mMain.TopWnd_hWnd)
    //    {
    //        string capMain = Std32Window.GetWindowCaption(mMain.TopWnd_hWnd);
    //        string capFind = Std32Window.GetWindowCaption(hWndFind);
    //        return new StdResult_Error(
    //            $"[{AppName}] 이동실패: {mMain.TopWnd_hWnd:X}, {hWndFind:X}, {ptTarget}, {capMain}, {capFind}",
    //            "OnecallAct_MainWnd/InitAsync_02");
    //    }

    //    await Task.Delay(500);
    //    return null;
    //}
    #endregion
}
#nullable restore
