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
public static class Simulation_Mouse
{
    // Win32 API
    [DllImport("user32.dll")]
    public static extern IntPtr SetCapture(IntPtr hWnd);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ReleaseCapture();

    [StructLayout(LayoutKind.Sequential)]
    public struct CURSORINFO
    {
        public Int32 cbSize;
        public Int32 flags;
        public IntPtr hCursor;
        public Draw.Point ptScreenPos;
    }

    [DllImport("user32.dll")]
    public static extern bool GetCursorInfo(out CURSORINFO pci);

    [DllImport("user32.dll")]
    public static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);

    public const int IDC_SIZEWE = 32644; // ↔
    public const int IDC_SIZENS = 32645; // ↕
    public const int CURSOR_SHOWING = 0x00000001;

    #region Mouse Events - Click
    public static async Task SafeMouseEvent_ClickLeft_ptRelAsync(IntPtr hWnd, Draw.Point ptClickRel, bool bBkCursor = true, int nDelay = 50)
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

    #region NEW Universal Precision Drag Engine (Clean Naming)

    /// <summary>
    /// [스나이퍼 엔진] 전 방향 정밀 드래그 (재시도 + 실시간 Crawl + 이탈 감지 통합)
    /// </summary>
    /// <param name="ptTargetRel">목표 좌표 (null이면 dx/dy 기반 계산)</param>
    /// <param name="gripCheck">드래그 중 이탈 감지 로직 (예: IsHorizontalResizeCursor)</param>
    public static async Task<bool> Drag_Precision_RetryAsync(
        IntPtr hWnd, Draw.Point ptStartRel, Draw.Point? ptTargetRel = null, int dx = 0, int dy = 0, 
        Func<bool> gripCheck = null, int nRetryCount = 5, int nMiliSec = 100, int nSafetyMargin = 5, int nDelayAtSafety = 20)
    {
        // 목표 좌표 결정 (좌표 우선, 없으면 이동량 기반)
        Draw.Point targetPoint = ptTargetRel ?? new Draw.Point(ptStartRel.X + dx, ptStartRel.Y + dy);

        for (int retry = 1; retry <= nRetryCount; retry++)
        {
            bool bSuccess = await Drag_Core_InternalAsync(
                hWnd, ptStartRel, targetPoint, gripCheck, nMiliSec, nSafetyMargin, nDelayAtSafety);

            if (bSuccess) return true;

            Debug.WriteLine($"[DRAG RETRY] 드래그 이탈 감지됨. 재시도 중... ({retry}/{nRetryCount})");
            await Task.Delay(200);
        }
        return false;
    }

    /// <summary>
    /// 1회 정밀 드래그 수행 (실시간 현위치 보정 Crawl 포함)
    /// </summary>
    private static async Task<bool> Drag_Core_InternalAsync(
        IntPtr hWnd, Draw.Point ptStartRel, Draw.Point ptTargetRel, Func<bool> gripCheck, 
        int nMiliSec, int nSafetyMargin, int nDelayAtSafety)
    {
        Draw.Point ptBk = Std32Cursor.GetCursorPos_AbsDrawPt();
        try
        {
            Std32Window.SetForegroundWindow(hWnd);
            
            // 1. 절대 좌표 타겟팅
            Std32Cursor.SetCursorPos_RelDrawPt(hWnd, ptStartRel);
            Draw.Point ptFirstAbs = Std32Cursor.GetCursorPos_AbsDrawPt();
            
            Std32Cursor.SetCursorPos_RelDrawPt(hWnd, ptTargetRel);
            Draw.Point ptTargetAbs = Std32Cursor.GetCursorPos_AbsDrawPt();

            // 시작점 복귀
            Std32Cursor.SetCursorPos_AbsDrawPt(ptFirstAbs);
            await Task.Delay(50);
            
            // 초기 그립 확인 (눌러보기 전)
            if (gripCheck != null && !gripCheck()) return false;

            Std32Mouse_Event.MouseEvent_LeftBtnDown();
            await Task.Delay(100); 

            // 2. 질주 구간 (Time-based Glide)
            Stopwatch sw = Stopwatch.StartNew();
            bool bInGrip = true;
            
            Draw.Point totalD = new Draw.Point(ptTargetAbs.X - ptFirstAbs.X, ptTargetAbs.Y - ptFirstAbs.Y);
            int nDirX = Math.Sign(totalD.X);
            int nDirY = Math.Sign(totalD.Y);
            
            Draw.Point safetyOffset = new Draw.Point(nDirX * nSafetyMargin, nDirY * nSafetyMargin);
            Draw.Point intermediateTargetAbs = new Draw.Point(ptTargetAbs.X - safetyOffset.X, ptTargetAbs.Y - safetyOffset.Y);
            Draw.Point intermediateD = new Draw.Point(intermediateTargetAbs.X - ptFirstAbs.X, intermediateTargetAbs.Y - ptFirstAbs.Y);

            while (sw.ElapsedMilliseconds < nMiliSec)
            {
                double ratio = (double)sw.ElapsedMilliseconds / nMiliSec;
                int moveX = (int)(intermediateD.X * ratio);
                int moveY = (int)(intermediateD.Y * ratio);
                Std32Cursor.SetCursorPos_AbsDrawPt(new Draw.Point(ptFirstAbs.X + moveX, ptFirstAbs.Y + moveY));

                if (bInGrip && gripCheck != null && !gripCheck()) bInGrip = false; 
                await Task.Delay(10);
            }
            
            // 3. 정밀 안착 구간 (Real-time Closed-loop Crawl)
            if (nSafetyMargin > 0)
            {
                if (nDelayAtSafety > 0) await Task.Delay(nDelayAtSafety);

                int maxSteps = 50; 
                while (maxSteps-- > 0)
                {
                    Draw.Point ptCur = Std32Cursor.GetCursorPos_AbsDrawPt();
                    if (ptCur.X == ptTargetAbs.X && ptCur.Y == ptTargetAbs.Y) break; 

                    int cDirX = Math.Sign(ptTargetAbs.X - ptCur.X);
                    int cDirY = Math.Sign(ptTargetAbs.Y - ptCur.Y);
                    
                    Std32Cursor.SetCursorPos_AbsDrawPt(new Draw.Point(ptCur.X + cDirX, ptCur.Y + cDirY));
                    await Task.Delay(5); 

                    if (bInGrip && gripCheck != null && !gripCheck()) bInGrip = false;
                }
            }

            // 4. 최종 안착 및 해제
            Std32Cursor.SetCursorPos_AbsDrawPt(ptTargetAbs);
            await Task.Delay(20); 
            Std32Mouse_Event.MouseEvent_LeftBtnUp();
            await Task.Delay(30);
            
            return bInGrip;
        }
        catch { return false; }
        finally { Std32Cursor.SetCursorPos_AbsDrawPt(ptBk); }
    }

    #endregion

    #region Legacy Safe Methods (Maintain compatibility, reduced internal logic)

    public static async Task<bool> SafeMouseEvent_DragLeft_Smooth_Horizon_WatchAsync(
        IntPtr hWnd, Draw.Point ptStartRel, int dx, bool bBkCursor = true, int nMiliSec = 100,
        int nSafetyMargin = 0, int nDelayAtSafety = 0)
    {
        // 신형 엔진으로 우회 호출
        return await Drag_Precision_RetryAsync(hWnd, ptStartRel, null, dx, 0, IsHorizontalResizeCursor, 3, nMiliSec, nSafetyMargin, nDelayAtSafety);
    }

    public static async Task<bool> SafeMouseEvent_DragLeft_SmoothAsync(
        IntPtr hWnd, Draw.Point ptStartRel, Draw.Point ptTargetRel, bool bBkCursor = true, int nMiliSec = 100)
    {
        return await Drag_Precision_RetryAsync(hWnd, ptStartRel, ptTargetRel, 0, 0, null, 1, nMiliSec, 0, 0);
    }

    public static async Task SafeMouseEvent_DragLeft_Smooth_VerticalAsync(
        IntPtr hWnd, Draw.Point ptStartRel, int dy, bool bBkCursor = true, int nMiliSec = 100)
    {
        await Drag_Precision_RetryAsync(hWnd, ptStartRel, null, 0, dy, null, 1, nMiliSec, 0, 0);
    }

    // 기타 레거시 함수들... (중략 없이 유지)
    public static async Task<bool> SafeMouseEvent_DragLeft_SmoothAsync(Draw.Point ptStartAbs, Draw.Point ptTargetAbs, bool bBkCursor = true, int nMiliSec = 100)
    {
        Draw.Point ptBk = Std32Cursor.GetCursorPos_AbsDrawPt();
        try {
            Std32Cursor.SetCursorPos_AbsDrawPt(ptStartAbs);
            Std32Mouse_Event.MouseEvent_LeftBtnDown(); await Task.Delay(100); 
            await MoveSmooth_PrecisionAsync(ptStartAbs, ptTargetAbs, nMiliSec);
            Std32Mouse_Event.MouseEvent_LeftBtnUp(); await Task.Delay(50);
        } finally { if (bBkCursor) Std32Cursor.SetCursorPos_AbsDrawPt(ptBk); }
        return Std32Cursor.GetCursorPos_AbsDrawPt() == ptTargetAbs;
    }

    public static async Task SafeMouseEvent_DragLeft_Smooth_HorizonAsync(IntPtr hWnd, Draw.Point ptStartRel, int dx, bool bBkCursor = true, int nMiliSec = 100)
    {
        await SafeMouseEvent_DragLeft_Smooth_Horizon_WatchAsync(hWnd, ptStartRel, dx, bBkCursor, nMiliSec);
    }

    #endregion

    #region Cursor Helpers
    public static bool IsHorizontalResizeCursor()
    {
        CURSORINFO pci = new CURSORINFO { cbSize = Marshal.SizeOf(typeof(CURSORINFO)) };
        if (GetCursorInfo(out pci) && pci.flags == CURSOR_SHOWING)
        {
            return pci.hCursor == LoadCursor(IntPtr.Zero, IDC_SIZEWE);
        }
        return false;
    }

    public static bool IsVerticalResizeCursor()
    {
        CURSORINFO pci = new CURSORINFO { cbSize = Marshal.SizeOf(typeof(CURSORINFO)) };
        if (GetCursorInfo(out pci) && pci.flags == CURSOR_SHOWING)
        {
            return pci.hCursor == LoadCursor(IntPtr.Zero, IDC_SIZENS);
        }
        return false;
    }
    #endregion

    #region Other Original Helpers (SmartAim, MoveSmooth, etc.)
    public static async Task<int> SmartAimBoundaryAsync(IntPtr hWnd, int startX, int y, int range = 3)
    {
        for (int i = 0; i <= range; i++) {
            int[] offsets = i == 0 ? new int[] { 0 } : new int[] { i, -i };
            foreach (int offset in offsets) {
                int targetX = startX + offset;
                Std32Cursor.SetCursorPos_RelDrawPt(hWnd, new Draw.Point(targetX, y));
                await Task.Delay(30); 
                if (IsHorizontalResizeCursor()) return targetX;
            }
        }
        return -1;
    }

    private static async Task MoveSmooth_PrecisionAsync(Draw.Point ptStart, Draw.Point ptTarget, int nMiliSec)
    {
        int dx = ptTarget.X - ptStart.X; int dy = ptTarget.Y - ptStart.Y;
        double dist = Math.Sqrt(dx * dx + dy * dy);
        if (dist > 10) {
            double ratio = (dist - 4) / dist;
            Draw.Point ptNear = new Draw.Point(ptStart.X + (int)(dx * ratio), ptStart.Y + (int)(dy * ratio));
            await Std32Mouse_Send.MouseSet_MoveSmooth_ptAbsAsync(ptStart, ptNear, (int)(nMiliSec * 0.8));
            await Task.Delay(30); 
            for (int i = 1; i <= 4; i++) {
                double stepRatio = (double)i / 4;
                Draw.Point ptStep = new Draw.Point(ptNear.X + (int)((ptTarget.X - ptNear.X) * stepRatio), ptNear.Y + (int)((ptTarget.Y - ptNear.Y) * stepRatio));
                Std32Cursor.SetCursorPos_AbsDrawPt(ptStep); await Task.Delay(30); 
            }
            await Task.Delay(50); 
        } else { await Std32Mouse_Send.MouseSet_MoveSmooth_ptAbsAsync(ptStart, ptTarget, nMiliSec); await Task.Delay(50); }
    }

    public static async Task SafeMouseSend_DblClickLeft_ptRelAsync(IntPtr hWnd, Draw.Point ptClickRel, int betweenClickDelay = 0, bool bBkCursor = true)
    {
        Draw.Point ptBk = Std32Cursor.GetCursorPos_AbsDrawPt(); IntPtr hWndFocusBk = StdWin32.GetForegroundWindow();
        try {
            Std32Cursor.SetCursorPos_RelDrawPt(hWnd, ptClickRel);
            StdWin32.SendMessage(hWnd, StdWin32.WM_LBUTTONDOWN, 1, (ulong)StdUtil.MakeIntPtrLParam(ptClickRel.X, ptClickRel.Y));
            StdWin32.PostMessage(hWnd, StdWin32.WM_LBUTTONUP, 0, StdUtil.MakeIntPtrLParam(ptClickRel.X, ptClickRel.Y));
            if (betweenClickDelay > 0) await Task.Delay(betweenClickDelay);
            StdWin32.SendMessage(hWnd, StdWin32.WM_LBUTTONDBLCLK, 1, (ulong)StdUtil.MakeIntPtrLParam(ptClickRel.X, ptClickRel.Y));
            StdWin32.PostMessage(hWnd, StdWin32.WM_LBUTTONUP, 0, StdUtil.MakeIntPtrLParam(ptClickRel.X, ptClickRel.Y));
        } finally { if (bBkCursor) Std32Cursor.SetCursorPos_AbsDrawPt(ptBk); if (hWndFocusBk != IntPtr.Zero) StdWin32.SetForegroundWindow(hWndFocusBk); }
    }

    public static async Task<bool> MousePost_ClickLeft_WaitSelectionAsync(IntPtr hWnd, Draw.Point ptRelClick, Draw.Point ptRelCheck, int nOrgBrightness, int nRepeatCount = 50, int nBrightGap = 10)
    {
        try {
            await Std32Mouse_Post.MousePostAsync_ClickLeft_ptRel(hWnd, ptRelClick);
            for (int i = 0; i < nRepeatCount; i++) {
                await s_GlobalCancelToken.WaitIfPausedOrCancelledAsync();
                if (Math.Abs(OfrService.GetPixelBrightnessFrmWndHandle(hWnd, ptRelCheck) - nOrgBrightness) > nBrightGap) return true;
                await Task.Delay(50);
            } return false;
        } catch { return false; }
    }

    public static async Task<StdResult_Status> SetCheckBtnStatusAsync(IntPtr hWnd, bool targetState, int nRepeat = c_nRepeatShort)
    {
        try {
            uint uCurrentStatus = Std32Msg_Send.GetCheckStatus(hWnd);
            if ((uCurrentStatus == StdCommon32.BST_CHECKED) == targetState) return new StdResult_Status(StdResult.Success);
            for (int i = 0; i < nRepeat; i++) {
                await s_GlobalCancelToken.WaitIfPausedOrCancelledAsync(); await Std32Mouse_Post.MousePostAsync_ClickLeft_Center(hWnd);
                for (int j = 0; j < c_nRepeatMany; j++) {
                    await s_GlobalCancelToken.WaitIfPausedOrCancelledAsync(); await Task.Delay(c_nWaitUltraShort);
                    uCurrentStatus = Std32Msg_Send.GetCheckStatus(hWnd);
                    if ((uCurrentStatus == StdCommon32.BST_CHECKED) == targetState) return new StdResult_Status(StdResult.Success);
                }
            } return new StdResult_Status(StdResult.Fail, "체크박스 상태 변경 실패");
        } catch (Exception ex) { return new StdResult_Status(StdResult.Fail, StdUtil.GetExceptionMessage(ex)); }
    }
    #endregion
}
