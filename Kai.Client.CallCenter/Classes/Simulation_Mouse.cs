using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Draw = System.Drawing;

using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;
using Kai.Common.NetDll_WpfCtrl.NetOFR;

using static Kai.Client.CallCenter.Classes.CommonVars;

namespace Kai.Client.CallCenter.Classes;

// 마우스 시뮬레이션 헬퍼 클래스 (순수 시뮬레이션 - 차단 로직 전면 배제 테스트)
public static class Simulation_Mouse
{
    #region Blocking Control (무력화 상태)
    public static bool IsMouseHookLocked => false; 
    public static bool IsSimulating { get; set; } = false;

    public static void SafeBlockInputStart() { }
    public static void SafeBlockInputStop() { }
    public static void SafeBlockInputForceStop() { }
    public static void SafeBlockMouseHookStart() { }
    public static void SafeBlockMouseHookStop() { }
    public static void SafeBlockMouseHookForceStop() { }
    #endregion

    #region Mouse Events - Click
    public static async Task SafeMouseEvent_ClickLeft_ptRelAsync(
        IntPtr hWnd, Draw.Point ptClickRel, bool bBkCursor = true, int nDelay = 50)
    {
        Draw.Point ptBk = Std32Cursor.GetCursorPos_AbsDrawPt();
        try
        {
            Std32Cursor.SetCursorPos_RelDrawPt(hWnd, ptClickRel);
            Std32Mouse_Event.MouseEvent_LeftBtnDown();
            await Task.Delay(30);
            Std32Mouse_Event.MouseEvent_LeftBtnUp();
        }
        finally
        {
            if (bBkCursor) Std32Cursor.SetCursorPos_AbsDrawPt(ptBk);
            await Task.Delay(nDelay);
        }
    }
    #endregion

    #region Mouse Events - Drag (Precision)
        // 변수값을 변경하지 말라 // Do not modify variable values
    public static async Task<bool> SafeMouseEvent_DragLeft_SmoothAsync(
        Draw.Point ptStartAbs, Draw.Point ptTargetAbs, bool bBkCursor = true, int nMiliSec = 100)
    {
        Draw.Point ptBk = Std32Cursor.GetCursorPos_AbsDrawPt();
        try
        {
            Std32Cursor.SetCursorPos_AbsDrawPt(ptStartAbs);
            Std32Mouse_Event.MouseEvent_LeftBtnDown();
            await Task.Delay(100); 

            await MoveSmooth_PrecisionAsync(ptStartAbs, ptTargetAbs, nMiliSec);
            
            Std32Mouse_Event.MouseEvent_LeftBtnUp();
            await Task.Delay(50);
        }
        finally
        {
            if (bBkCursor) Std32Cursor.SetCursorPos_AbsDrawPt(ptBk);
        }
        return Std32Cursor.GetCursorPos_AbsDrawPt() == ptTargetAbs;
    }

    public static async Task<bool> SafeMouseEvent_DragLeft_SmoothAsync(
        IntPtr hWnd, Draw.Point ptStartRel, Draw.Point ptTargetRel, bool bBkCursor = true, int nMiliSec = 100)
    {
        Draw.Rectangle rc = Std32Window.GetWindowRect_DrawAbs(hWnd);
        Draw.Point ptStartAbs = new Draw.Point(rc.Left + ptStartRel.X, rc.Top + ptStartRel.Y);
        Draw.Point ptTargetAbs = new Draw.Point(rc.Left + ptTargetRel.X, rc.Top + ptTargetRel.Y);
        return await SafeMouseEvent_DragLeft_SmoothAsync(ptStartAbs, ptTargetAbs, bBkCursor, nMiliSec);
    }

    public static async Task SafeMouseEvent_DragLeft_Smooth_VerticalAsync(
        IntPtr hWnd, Draw.Point ptStartRel, int dy, bool bBkCursor = true, int nMiliSec = 100)
    {
        Draw.Point ptBk = Std32Cursor.GetCursorPos_AbsDrawPt();
        try
        {
            Std32Window.SetForegroundWindow(hWnd);
            Std32Cursor.SetCursorPos_RelDrawPt(hWnd, ptStartRel);
            Draw.Point ptCurAbs = Std32Cursor.GetCursorPos_AbsDrawPt();
            Draw.Point ptTargetAbs = new Draw.Point(ptCurAbs.X, ptCurAbs.Y + dy);

            Std32Mouse_Event.MouseEvent_LeftBtnDown();
            await Task.Delay(100); 

            await MoveSmooth_PrecisionAsync(ptCurAbs, ptTargetAbs, nMiliSec);
            
            Std32Mouse_Event.MouseEvent_LeftBtnUp();
            await Task.Delay(30);
        }
        finally
        {
            if (bBkCursor) Std32Cursor.SetCursorPos_AbsDrawPt(ptBk);
        }
    }

    public static async Task SafeMouseEvent_DragLeft_Smooth_HorizonAsync(
        IntPtr hWnd, Draw.Point ptStartRel, int dx, bool bBkCursor = true, int nMiliSec = 100)
    {
        Draw.Point ptBk = Std32Cursor.GetCursorPos_AbsDrawPt();
        try
        {
            Std32Window.SetForegroundWindow(hWnd);
            Std32Cursor.SetCursorPos_RelDrawPt(hWnd, ptStartRel);
            Draw.Point ptCurAbs = Std32Cursor.GetCursorPos_AbsDrawPt();
            Draw.Point ptTargetAbs = new Draw.Point(ptCurAbs.X + dx, ptCurAbs.Y);

            Std32Mouse_Event.MouseEvent_LeftBtnDown();
            await Task.Delay(100); 

            await MoveSmooth_PrecisionAsync(ptCurAbs, ptTargetAbs, nMiliSec);
            
            Std32Mouse_Event.MouseEvent_LeftBtnUp();
            await Task.Delay(30);
        }
        finally
        {
            if (bBkCursor) Std32Cursor.SetCursorPos_AbsDrawPt(ptBk);
        }
    }

    private static async Task MoveSmooth_PrecisionAsync(Draw.Point ptStart, Draw.Point ptTarget, int nMiliSec)
    {
        int dx = ptTarget.X - ptStart.X;
        int dy = ptTarget.Y - ptStart.Y;
        double dist = Math.Sqrt(dx * dx + dy * dy);

        if (dist > 10)
        {
            double ratio = (dist - 4) / dist;
            Draw.Point ptNear = new Draw.Point(ptStart.X + (int)(dx * ratio), ptStart.Y + (int)(dy * ratio));

            await Std32Mouse_Send.MouseSet_MoveSmooth_ptAbsAsync(ptStart, ptNear, (int)(nMiliSec * 0.8));
            await Task.Delay(30); 

            for (int i = 1; i <= 4; i++)
            {
                double stepRatio = (double)i / 4;
                Draw.Point ptStep = new Draw.Point(
                    ptNear.X + (int)((ptTarget.X - ptNear.X) * stepRatio),
                    ptNear.Y + (int)((ptTarget.Y - ptNear.Y) * stepRatio)
                );
                Std32Cursor.SetCursorPos_AbsDrawPt(ptStep);
                await Task.Delay(30); 
            }
            await Task.Delay(50); 
        }
        else
        {
            await Std32Mouse_Send.MouseSet_MoveSmooth_ptAbsAsync(ptStart, ptTarget, nMiliSec);
            await Task.Delay(50); 
        }
    }
    #endregion

    #region Mouse Events - DoubleClick
    public static async Task SafeMouseSend_DblClickLeft_ptRelAsync(IntPtr hWnd, Draw.Point ptClickRel, int betweenClickDelay = 0, bool bBkCursor = true)
    {
        Draw.Point ptBk = Std32Cursor.GetCursorPos_AbsDrawPt();
        IntPtr hWndFocusBk = StdWin32.GetForegroundWindow();
        IntPtr lParam = StdUtil.MakeIntPtrLParam(ptClickRel.X, ptClickRel.Y);

        try
        {
            Std32Cursor.SetCursorPos_RelDrawPt(hWnd, ptClickRel);
            StdWin32.SendMessage(hWnd, StdWin32.WM_LBUTTONDOWN, 1, (ulong)lParam);
            StdWin32.PostMessage(hWnd, StdWin32.WM_LBUTTONUP, 0, lParam);

            if (betweenClickDelay > 0) await Task.Delay(betweenClickDelay);

            StdWin32.SendMessage(hWnd, StdWin32.WM_LBUTTONDBLCLK, 1, (ulong)lParam);
            StdWin32.PostMessage(hWnd, StdWin32.WM_LBUTTONUP, 0, lParam);
        }
        finally
        {
            if (bBkCursor) Std32Cursor.SetCursorPos_AbsDrawPt(ptBk);
            if (hWndFocusBk != IntPtr.Zero) StdWin32.SetForegroundWindow(hWndFocusBk);
        }
    }
    #endregion

    #region Other Helpers (Non-Blocking)
    public static async Task<bool> MousePost_ClickLeft_WaitSelectionAsync(
        IntPtr hWnd, Draw.Point ptRelClick, Draw.Point ptRelCheck, int nOrgBrightness, int nRepeatCount = 50, int nBrightGap = 10)
    {
        try
        {
            await Std32Mouse_Post.MousePostAsync_ClickLeft_ptRel(hWnd, ptRelClick);
            for (int i = 0; i < nRepeatCount; i++)
            {
                await s_GlobalCancelToken.WaitIfPausedOrCancelledAsync();
                int curBrightness = OfrService.GetPixelBrightnessFrmWndHandle(hWnd, ptRelCheck);
                if (Math.Abs(curBrightness - nOrgBrightness) > nBrightGap) return true;
                await Task.Delay(50);
            }
            return false;
        }
        catch { return false; }
    }

    public static async Task<StdResult_Status> SetCheckBtnStatusAsync(IntPtr hWnd, bool targetState, int nRepeat = c_nRepeatShort)
    {
        try
        {
            uint uCurrentStatus = Std32Msg_Send.GetCheckStatus(hWnd);
            if ((uCurrentStatus == StdCommon32.BST_CHECKED) == targetState) return new StdResult_Status(StdResult.Success);

            for (int i = 0; i < nRepeat; i++)
            {
                await s_GlobalCancelToken.WaitIfPausedOrCancelledAsync(); 
                await Std32Mouse_Post.MousePostAsync_ClickLeft_Center(hWnd);
                for (int j = 0; j < c_nRepeatMany; j++)
                {
                    await s_GlobalCancelToken.WaitIfPausedOrCancelledAsync(); 
                    await Task.Delay(c_nWaitUltraShort);
                    uCurrentStatus = Std32Msg_Send.GetCheckStatus(hWnd);
                    if ((uCurrentStatus == StdCommon32.BST_CHECKED) == targetState) return new StdResult_Status(StdResult.Success);
                }
            }
            return new StdResult_Status(StdResult.Fail, "체크박스 상태 변경 실패", "SetCheckBtnStatusAsync");
        }
        catch (Exception ex) { return new StdResult_Status(StdResult.Fail, StdUtil.GetExceptionMessage(ex), "SetCheckBtnStatusAsync_Ex"); }
    }
    #endregion
}
