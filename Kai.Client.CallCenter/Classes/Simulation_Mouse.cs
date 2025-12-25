using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Draw = System.Drawing;

using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;
using Kai.Common.NetDll_WpfCtrl.NetOFR;
using System.Runtime.InteropServices;

using static Kai.Client.CallCenter.Classes.CommonVars;

namespace Kai.Client.CallCenter.Classes;

// 마우스 시뮬레이션 헬퍼 클래스 (범용 정밀 엔진 통합 버전)
#nullable disable
public static class Simulation_Mouse
{
    #region 레거시 및 기타 엔진 (범용)
    /* [기존 주석 코드 보관]
    public static async Task<bool> DragAsync_Horizontal_FromCenter_Legacy(...) { ... }
    */

    // [레거시] 수직 드래그
    public static async Task<bool> SafeMouseEvent_DragLeft_Smooth_VerticalAsync(IntPtr hWnd, Draw.Point ptStartRel, int dy, bool bCheckGrip = false, int nMiliSec = 100)
    {
        try
        {
            Std32Window.SetForegroundWindow(hWnd);
            Std32Cursor.SetCursorPos_RelDrawPt(hWnd, ptStartRel);
            Draw.Point ptStartAbs = Std32Cursor.GetCursorPos_AbsDrawPt();
            Draw.Point ptEndAbs = new Draw.Point(ptStartAbs.X, ptStartAbs.Y + dy);

            Std32Mouse_Event.MouseEvent_LeftBtnDown();
            await Task.Delay(50);

            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < nMiliSec)
            {
                double ratio = (double)sw.ElapsedMilliseconds / nMiliSec;
                int currentDy = (int)(dy * ratio);
                Std32Cursor.SetCursorPos_AbsDrawPt(new Draw.Point(ptStartAbs.X, ptStartAbs.Y + currentDy));
                await Task.Delay(10);
            }

            Std32Cursor.SetCursorPos_AbsDrawPt(ptEndAbs);
            await Task.Delay(50);
            Std32Mouse_Event.MouseEvent_LeftBtnUp();
            await Task.Delay(200);
            return true;
        }
        catch { return false; }
    }

    // [레거시] 수평 드래그 (델타값 기반)
    public static async Task<bool> SafeMouseEvent_DragLeft_Smooth_HorizonAsync(IntPtr hWnd, Draw.Point ptStartRel, int dx, bool bCheckGrip = false, int nMiliSec = 100)
    {
        try
        {
            Std32Window.SetForegroundWindow(hWnd);
            Std32Cursor.SetCursorPos_RelDrawPt(hWnd, ptStartRel);
            Draw.Point ptStartAbs = Std32Cursor.GetCursorPos_AbsDrawPt();
            Draw.Point ptEndAbs = new Draw.Point(ptStartAbs.X + dx, ptStartAbs.Y);

            Std32Mouse_Event.MouseEvent_LeftBtnDown();
            await Task.Delay(50);

            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < nMiliSec)
            {
                double ratio = (double)sw.ElapsedMilliseconds / nMiliSec;
                int currentDx = (int)(dx * ratio);
                Std32Cursor.SetCursorPos_AbsDrawPt(new Draw.Point(ptStartAbs.X + currentDx, ptStartAbs.Y));
                await Task.Delay(10);
            }

            Std32Cursor.SetCursorPos_AbsDrawPt(ptEndAbs);
            await Task.Delay(50);
            Std32Mouse_Event.MouseEvent_LeftBtnUp();
            await Task.Delay(200);
            return true;
        }
        catch { return false; }
    }

    // [레거시] 범용 드래그 (시작점/끝점 기반)
    public static async Task<bool> SafeMouseEvent_DragLeft_SmoothAsync(IntPtr hWnd, Draw.Point ptStartAbs, Draw.Point ptEndAbs, bool bCheckGrip = false, int nMiliSec = 100)
    {
        try
        {
            Std32Window.SetForegroundWindow(hWnd);
            Std32Cursor.SetCursorPos_AbsDrawPt(ptStartAbs);
            
            Std32Mouse_Event.MouseEvent_LeftBtnDown();
            await Task.Delay(50);

            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < nMiliSec)
            {
                double ratio = (double)sw.ElapsedMilliseconds / nMiliSec;
                int curX = ptStartAbs.X + (int)((ptEndAbs.X - ptStartAbs.X) * ratio);
                int curY = ptStartAbs.Y + (int)((ptEndAbs.Y - ptStartAbs.Y) * ratio);
                Std32Cursor.SetCursorPos_AbsDrawPt(new Draw.Point(curX, curY));
                await Task.Delay(10);
            }

            Std32Cursor.SetCursorPos_AbsDrawPt(ptEndAbs);
            await Task.Delay(50);
            Std32Mouse_Event.MouseEvent_LeftBtnUp();
            await Task.Delay(200);
            return true;
        }
        catch { return false; }
    }

    public static bool IsHorizontalResizeCursor()
    {
        StdCommon32.CURSORINFO pci = new StdCommon32.CURSORINFO();
        pci.cbSize = Marshal.SizeOf(typeof(StdCommon32.CURSORINFO));
        if (StdWin32.GetCursorInfo(out pci))
        {
            IntPtr hResize = StdWin32.LoadCursor(IntPtr.Zero, StdCommon32.IDC_SIZEWE);
            return pci.hCursor == hResize;
        }
        return false;
    }

    public static IntPtr GetCurrentCursorHandle()
    {
        StdCommon32.CURSORINFO pci = new StdCommon32.CURSORINFO();
        pci.cbSize = Marshal.SizeOf(typeof(StdCommon32.CURSORINFO));
        if (StdWin32.GetCursorInfo(out pci))
        {
            return pci.hCursor;
        }
        return IntPtr.Zero;
    }

    // 정밀 이동 재시도 버전 (폭 조절용)
    //public static async Task<bool> Drag_Precision_RetryAsync(IntPtr hWnd, Draw.Point ptStartRel, 
    //    int dx, Func<bool> gripCheck, int nRetryCount = 5, int nMiliSec = 100, int nSafetyMargin = 5, int nDelayAtSafety = 20)
    //{
    //    return await DragAsync_Horizontal_FromBoundary(hWnd, ptStartRel, null, dx, gripCheck, nRetryCount, nMiliSec, nSafetyMargin, nDelayAtSafety);
    //}
    #endregion
}