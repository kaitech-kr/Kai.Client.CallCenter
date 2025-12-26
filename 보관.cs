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

    public const int IDC_ARROW = 32512;
    public const int IDC_SIZEWE = 32644; // ↔
    public const int IDC_SIZENS = 32645; // ↕
    public const int IDC_NO = 32648;     // 🚫 (X자 모양)
    public const int CURSOR_SHOWING = 0x00000001;

    #region NEW Universal Precision Drag Engine (Clean Naming)

    // [스나이퍼 엔진] 수평 정밀 드래그 (재시도 + 실시간 Crawl + 이탈 감지 통합), ptTargetRel: 목표 좌표 (null이면 dx 기반), gripCheck: 드래그 중 이탈 감지 로직
    public static async Task<bool> DragAsync_Horizontal_FromBoundary(IntPtr hWnd, Draw.Point ptStartRel,
        Draw.Point? ptTargetRel = null, int dx = 0, Func<bool> gripCheck = null, int nRetryCount = 5, int nMiliSec = 100, int nSafetyMargin = 5, int nDelayAtSafety = 20)
    {
        // 목표 좌표 결정 (좌표 우선, 없으면 dx 기반 - Y축은 시작점 유지)
        Draw.Point targetPoint = ptTargetRel ?? new Draw.Point(ptStartRel.X + dx, ptStartRel.Y);

        for (int retry = 1; retry <= nRetryCount; retry++)
        {
            Draw.Point ptBk = Std32Cursor.GetCursorPos_AbsDrawPt();
            bool bSuccess = false;
            try
            {
                Std32Window.SetForegroundWindow(hWnd);

                // 1. 절대 좌표 타겟팅
                Std32Cursor.SetCursorPos_RelDrawPt(hWnd, ptStartRel);
                Draw.Point ptFirstAbs = Std32Cursor.GetCursorPos_AbsDrawPt();

                Std32Cursor.SetCursorPos_RelDrawPt(hWnd, targetPoint);
                Draw.Point ptTargetAbs = Std32Cursor.GetCursorPos_AbsDrawPt();

                // 시작점 복귀
                Std32Cursor.SetCursorPos_AbsDrawPt(ptFirstAbs);
                await Task.Delay(50);

                // 초기 그립 확인 (눌러보기 전)
                if (gripCheck != null && !gripCheck()) { bSuccess = false; continue; }

                Std32Mouse_Event.MouseEvent_LeftBtnDown();
                await Task.Delay(100);

                // 2. 질주 구간 (Time-based Glide) - 수평 이동만
                Stopwatch sw = Stopwatch.StartNew();
                bool bInGrip = true;

                int totalDx = ptTargetAbs.X - ptFirstAbs.X;
                int nDirX = Math.Sign(totalDx);
                int safetyOffsetX = nDirX * nSafetyMargin;
                int intermediateTargetX = ptTargetAbs.X - safetyOffsetX;
                int intermediateDx = intermediateTargetX - ptFirstAbs.X;

                while (sw.ElapsedMilliseconds < nMiliSec)
                {
                    double ratio = (double)sw.ElapsedMilliseconds / nMiliSec;
                    int moveX = (int)(intermediateDx * ratio);
                    Std32Cursor.SetCursorPos_AbsDrawPt(new Draw.Point(ptFirstAbs.X + moveX, ptFirstAbs.Y));

                    if (bInGrip && gripCheck != null && !gripCheck()) bInGrip = false;
                    await Task.Delay(10);
                }

                // 3. 정밀 안착 구간 (Real-time Closed-loop Crawl) - 수평만
                if (nSafetyMargin > 0)
                {
                    if (nDelayAtSafety > 0) await Task.Delay(nDelayAtSafety);

                    int maxSteps = 50;
                    while (maxSteps-- > 0)
                    {
                        Draw.Point ptCur = Std32Cursor.GetCursorPos_AbsDrawPt();
                        if (ptCur.X == ptTargetAbs.X) break;

                        int cDirX = Math.Sign(ptTargetAbs.X - ptCur.X);
                        Std32Cursor.SetCursorPos_AbsDrawPt(new Draw.Point(ptCur.X + cDirX, ptFirstAbs.Y));
                        await Task.Delay(5);

                        if (bInGrip && gripCheck != null && !gripCheck()) bInGrip = false;
                    }
                }

                // 4. 최종 안착 및 해제
                Std32Cursor.SetCursorPos_AbsDrawPt(new Draw.Point(ptTargetAbs.X, ptFirstAbs.Y));
                await Task.Delay(20);

                Std32Mouse_Event.MouseEvent_LeftBtnUp();
                await Task.Delay(30);

                bSuccess = bInGrip;
            }
            catch { bSuccess = false; }
            finally { Std32Cursor.SetCursorPos_AbsDrawPt(ptBk); }

            if (bSuccess) return true;

            Debug.WriteLine($"[DRAG RETRY] 드래그 이탈 감지됨. 재시도 중... ({retry}/{nRetryCount})");
            await Task.Delay(200);
        }
        return false;
    }

    #endregion

    // [원콜 전용] 심해(100px) 주행 및 커서 기반 유실 감지 엔진
    // ptStartRel: 드래그 시작 좌표, ptTargetRel: 목표 좌표 (dx 우선), nRetryCount: 재시도 횟수
    public static async Task<bool> DragAsync_Horizontal_FromCenter(IntPtr hWnd, Draw.Point ptStartRel,
        Draw.Point? ptTargetRel = null, int dx = 0, Func<bool> gripCheck = null, int nRetryCount = 5, int nMiliSec = 500, int nSafetyMargin = 5, int nDelayAtSafety = 20, int nBackgroundBright = 0)
    {
        Draw.Point targetPointRel = ptTargetRel ?? new Draw.Point(ptStartRel.X + dx, ptStartRel.Y);

        for (int retry = 1; retry <= nRetryCount; retry++)
        {
            try
            {
                // 1. 출발지로 커서 이동
                Std32Window.SetForegroundWindow(hWnd);
                Std32Cursor.SetCursorPos_RelDrawPt(hWnd, ptStartRel);
                Draw.Point ptFirstAbs = Std32Cursor.GetCursorPos_AbsDrawPt();

                Std32Cursor.SetCursorPos_RelDrawPt(hWnd, targetPointRel);
                Draw.Point ptTargetAbs = Std32Cursor.GetCursorPos_AbsDrawPt();

                // 출발지 재안착
                Std32Cursor.SetCursorPos_AbsDrawPt(ptFirstAbs);
                await Task.Delay(50);

                // 2. 좌측 버튼 다운
                Std32Mouse_Event.MouseEvent_LeftBtnDown();
                await Task.Delay(20);

                // [Phase 1] 100px 수직 하강 (초정밀 견인)
                int deepGlideY = ptFirstAbs.Y + 100;
                for (int vStep = 1; vStep <= 10; vStep++)
                {
                    Std32Cursor.SetCursorPos_AbsDrawPt(new Draw.Point(ptFirstAbs.X, ptFirstAbs.Y + (vStep * 10)));
                    await Task.Delay(15);
                }
                await Task.Delay(80); // 심해 안착 대기 (완전하게)

                // 4. 심해 주행 (Ease-In-Out) - 거리 비례 속도 최적화
                int totalDx = ptTargetAbs.X - ptFirstAbs.X;
                int nDirX = Math.Sign(totalDx);
                int crawlMargin = 5;
                if (Math.Abs(totalDx) <= crawlMargin) crawlMargin = 0;

                int mainMoveDx = totalDx - (nDirX * crawlMargin);

                // [스마트 타임] 거리에 비례하여 지능적으로 시간 산출 (1.5ms/px + 기본 200ms)
                long moveTime = (long)(Math.Abs(totalDx) * 1.5) + 200;

                Stopwatch sw = Stopwatch.StartNew();
                IntPtr hArrow = LoadCursor(IntPtr.Zero, IDC_ARROW);

                while (sw.ElapsedMilliseconds < moveTime)
                {
                    // 주행 중 유실 감지
                    if (GetCurrentCursorHandle() == hArrow)
                    {
                        Debug.WriteLine($"[DragCenter] 주행 중 유실 (Arrow 복귀). 재시도...");
                        throw new Exception("Drag Grip Lost");
                    }

                    double ratio = (double)sw.ElapsedMilliseconds / moveTime;
                    double easeRatio = (1 - Math.Cos(ratio * Math.PI)) / 2;
                    int currentDx = (int)(mainMoveDx * easeRatio);

                    Std32Cursor.SetCursorPos_AbsDrawPt(new Draw.Point(ptFirstAbs.X + currentDx, deepGlideY));
                    await Task.Delay(5);
                }

                // 5. 정밀 안착 구간 (Deep Sea Crawl)
                int maxCrawlSteps = 50;
                while (maxCrawlSteps-- > 0)
                {
                    Draw.Point ptCur = Std32Cursor.GetCursorPos_AbsDrawPt();
                    if (Math.Abs(ptCur.X - ptTargetAbs.X) <= 1) break;

                    int moveDir = Math.Sign(ptTargetAbs.X - ptCur.X);
                    Std32Cursor.SetCursorPos_AbsDrawPt(new Draw.Point(ptCur.X + moveDir, deepGlideY));
                    await Task.Delay(5);
                }

                // [Phase 3] 수직 상승 및 드랍 (deepGlideY -> 15px)
                for (int vStep = 3; vStep >= 0; vStep--)
                {
                    int stepY = ptFirstAbs.Y + (vStep * 25);
                    Std32Cursor.SetCursorPos_AbsDrawPt(new Draw.Point(ptTargetAbs.X, stepY));
                    await Task.Delay(15);
                }

                await Task.Delay(200);
                Std32Mouse_Event.MouseEvent_LeftBtnUp();
                await Task.Delay(200);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DragCenter] 오류 발생 ({retry}/{nRetryCount}): {ex.Message}");
            }

            // 실패 시 버튼 떼고 대기 후 재시도
            Std32Mouse_Event.MouseEvent_LeftBtnUp();
            await Task.Delay(200);
        }
        return false;
    }

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
        CURSORINFO pci = new CURSORINFO();
        pci.cbSize = Marshal.SizeOf(typeof(CURSORINFO));
        if (GetCursorInfo(out pci))
        {
            IntPtr hResize = LoadCursor(IntPtr.Zero, IDC_SIZEWE);
            return pci.hCursor == hResize;
        }
        return false;
    }

    public static IntPtr GetCurrentCursorHandle()
    {
        CURSORINFO pci = new CURSORINFO();
        pci.cbSize = Marshal.SizeOf(typeof(CURSORINFO));
        if (GetCursorInfo(out pci))
        {
            return pci.hCursor;
        }
        return IntPtr.Zero;
    }

    // 정밀 이동 재시도 버전 (폭 조절용)
    public static async Task<bool> Drag_Precision_RetryAsync(IntPtr hWnd, Draw.Point ptStartRel, int dx, Func<bool> gripCheck, int nRetryCount = 5, int nMiliSec = 100, int nSafetyMargin = 5, int nDelayAtSafety = 20)
    {
        return await DragAsync_Horizontal_FromBoundary(hWnd, ptStartRel, null, dx, gripCheck, nRetryCount, nMiliSec, nSafetyMargin, nDelayAtSafety);
    }
    #endregion
}