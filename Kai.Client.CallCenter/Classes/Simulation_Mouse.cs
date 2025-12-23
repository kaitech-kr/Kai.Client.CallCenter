using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Draw = System.Drawing;

using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;
using Kai.Common.NetDll_WpfCtrl.NetOFR;
using System.Runtime.InteropServices; // 추가

using static Kai.Client.CallCenter.Classes.CommonVars;

namespace Kai.Client.CallCenter.Classes;

// 마우스 시뮬레이션 헬퍼 클래스 (순수 시뮬레이션 - 차단 로직 전면 배제 테스트)
public static class Simulation_Mouse
{
    // Win32 API 추가
    [DllImport("user32.dll")]
    public static extern IntPtr SetCapture(IntPtr hWnd);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ReleaseCapture();

    // 커서 상태 확인용 API/상수
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

    public const int IDC_SIZEWE = 32644; // 좌우 화살표 (↔)
    public const int CURSOR_SHOWING = 0x00000001;

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

    public static async Task<bool> SafeMouseEvent_DragLeft_Smooth_Horizon_WatchAsync(
        IntPtr hWnd, Draw.Point ptStartRel, int dx, bool bBkCursor = true, int nMiliSec = 100,
        int nSafetyMargin = 0, int nDelayAtSafety = 0)
    {
        Draw.Point ptBk = Std32Cursor.GetCursorPos_AbsDrawPt();
        try
        {
            Std32Window.SetForegroundWindow(hWnd);
            Std32Cursor.SetCursorPos_RelDrawPt(hWnd, ptStartRel);
            Draw.Point ptFirstAbs = Std32Cursor.GetCursorPos_AbsDrawPt();
            Draw.Point ptTargetAbs = new Draw.Point(ptFirstAbs.X + dx, ptFirstAbs.Y);
            
            Std32Mouse_Event.MouseEvent_LeftBtnDown();
            await Task.Delay(100); 

            // 이동 루프 (감시 포함)
            Stopwatch sw = Stopwatch.StartNew();
            bool bSuccess = true;
            
            // 1. 목표 nSafetyMargin 픽셀 전까지만 루프 이동
            int nDir = Math.Sign(dx);
            int safetyOffset = nDir * nSafetyMargin;
            int intermediateDx = (Math.Abs(dx) > nSafetyMargin) ? dx - safetyOffset : dx;

            while (sw.ElapsedMilliseconds < nMiliSec)
            {
                double ratio = (double)sw.ElapsedMilliseconds / nMiliSec;
                int moveX = (int)(intermediateDx * ratio);
                Std32Cursor.SetCursorPos_AbsDrawPt(new Draw.Point(ptFirstAbs.X + moveX, ptFirstAbs.Y));

                if (bSuccess && !IsHorizontalResizeCursor())
                {
                    Debug.WriteLine($"[DROP DETECTED] 이탈 발생 (재시도 필요)");
                    bSuccess = false; 
                }
                await Task.Delay(10);
            }
            
            // 2. 조착 및 정밀 crawl (실시간 현위치 기반 1픽셀 이동)
            if (nSafetyMargin > 0)
            {
                // 조착 지점 딜레이
                if (nDelayAtSafety > 0) await Task.Delay(nDelayAtSafety);

                // 계속 현위치를 얻으면서 비교하여 목표 도달 (Closed-loop)
                int maxCrawlSteps = 20; // 안전장치: 최대 20픽셀까지만 정밀 이동 허용
                while (maxCrawlSteps-- > 0)
                {
                    Draw.Point ptCur = Std32Cursor.GetCursorPos_AbsDrawPt();
                    if (ptCur.X == ptTargetAbs.X) break; // 목표 도달 시 즉시 종료

                    int crawlDir = Math.Sign(ptTargetAbs.X - ptCur.X);
                    // 현재 마우스 위치에서 목표 방향으로 딱 1픽셀만 이동
                    Std32Cursor.SetCursorPos_AbsDrawPt(new Draw.Point(ptCur.X + crawlDir, ptTargetAbs.Y));
                    
                    await Task.Delay(5); // 픽셀당 5ms의 아주 정밀한 이동
                }
            }

            // 3. 최종 안착 확인 및 해제
            Std32Cursor.SetCursorPos_AbsDrawPt(ptTargetAbs);
            await Task.Delay(20); // 윈도우가 위치를 완전히 인지할 시간 확보
            Std32Mouse_Event.MouseEvent_LeftBtnUp();
            await Task.Delay(30);
            
            return bSuccess;
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
            await Task.Delay(50); // 500ms는 너무 비효율적이므로 50ms로 현실화 (가볍게 꽉 쥐기)

            await MoveSmooth_PrecisionAsync(ptCurAbs, ptTargetAbs, nMiliSec);
            
            Std32Mouse_Event.MouseEvent_LeftBtnUp();
            await Task.Delay(30);
        }
        finally
        {
            if (bBkCursor) Std32Cursor.SetCursorPos_AbsDrawPt(ptBk);
        }
    }

    /// <summary>
    /// 현재 마우스 커서가 좌우 리사이즈(↔) 모양인지 확인
    /// </summary>
    public static bool IsHorizontalResizeCursor()
    {
        CURSORINFO pci = new CURSORINFO();
        pci.cbSize = Marshal.SizeOf(pci);
        if (GetCursorInfo(out pci))
        {
            if (pci.flags == CURSOR_SHOWING)
            {
                // 시스템의 기본 ↔ 커서 핸들과 비교
                IntPtr hResize = LoadCursor(IntPtr.Zero, IDC_SIZEWE);
                return pci.hCursor == hResize;
            }
        }
        return false;
    }

    /// <summary>
    /// 목표 좌표 근처에서 커서가 ↔로 변하는 지점을 정밀 스캔
    /// </summary>
    public static async Task<int> SmartAimBoundaryAsync(IntPtr hWnd, int startX, int y, int range = 3)
    {
        for (int i = 0; i <= range; i++)
        {
            // 제자리 -> +1 -> -1 -> +2 -> -2 순으로 스캔
            int[] offsets = i == 0 ? new int[] { 0 } : new int[] { i, -i };
            foreach (int offset in offsets)
            {
                int targetX = startX + offset;
                Std32Cursor.SetCursorPos_RelDrawPt(hWnd, new Draw.Point(targetX, y));
                await Task.Delay(30); // 커서 모양 변경 대기

                if (IsHorizontalResizeCursor())
                {
                    Debug.WriteLine($"[SmartAim] 리사이즈 커서 발견! Offset: {offset}");
                    return targetX;
                }
            }
        }
        return -1; // 발견 실패
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
