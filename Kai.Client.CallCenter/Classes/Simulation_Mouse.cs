using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Draw = System.Drawing;

using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;
using Kai.Common.NetDll_WpfCtrl.NetOFR;

using static Kai.Client.CallCenter.Classes.CommonVars;

namespace Kai.Client.CallCenter.Classes;

// 마우스 시뮬레이션 헬퍼 클래스 (외부 입력 차단 포함)
public static class Simulation_Mouse
{
    #region Blocking Control
    private static int s_nBlockCount = 0;  // 중첩 호출 카운터

    // 외부 입력 차단 시작 (BlockInput API 호출, 중첩 지원)
    public static void SafeBlockInputStart()
    {
        s_nBlockCount++;
        if (s_nBlockCount == 1)  // 최초 호출 시에만 실제 차단
        {
            StdWin32.BlockInput(true);
        }
    }

    // 외부 입력 차단 해제 (중첩 카운트 0일 때 실제 해제)
    public static void SafeBlockInputStop()
    {
        if (s_nBlockCount > 0)
        {
            s_nBlockCount--;
            if (s_nBlockCount == 0)  // 모든 호출이 해제되면 실제 차단 해제
            {
                StdWin32.BlockInput(false);
            }
        }
    }

    // 외부 입력 차단 강제 해제 (예외 처리용)
    public static void SafeBlockInputForceStop()
    {
        s_nBlockCount = 0;
        StdWin32.BlockInput(false);
        Debug.WriteLine("[Simulation_Mouse] BlockInput Force Disabled");
    }
    #endregion

    #region Mouse Events - Click (Blocking)
    // 상대좌표 기준 좌클릭 (외부 입력 차단, 커서 복원)
    public static async Task SafeMouseEvent_ClickLeft_ptRelAsync(
        IntPtr hWnd, Draw.Point ptClickRel, bool bBkCursor = true, int nDelay = 50)
    {
        Draw.Point ptBk = Std32Cursor.GetCursorPos_AbsDrawPt();

        try
        {
            SafeBlockInputStart();
            Std32Cursor.SetCursorPos_RelDrawPt(hWnd, ptClickRel);
            Std32Mouse_Event.MouseEvent_LeftBtnDown();
            await Task.Delay(30);
            Std32Mouse_Event.MouseEvent_LeftBtnUp();
        }
        finally
        {
            if (bBkCursor) Std32Cursor.SetCursorPos_AbsDrawPt(ptBk);
            SafeBlockInputStop();
            await Task.Delay(nDelay);
        }
    }
    #endregion

    #region Mouse Events - Drag (Blocking, Precision)
    // 절대좌표 기준 좌클릭 드래그 (스무스 이동, 정밀 제어용)
    public static async Task<bool> SafeMouseEvent_DragLeft_SmoothAsync(
        Draw.Point ptStartAbs, Draw.Point ptTargetAbs, bool bBkCursor = true, int nMiliSec = 100)
    {
        Draw.Point ptBk = Std32Cursor.GetCursorPos_AbsDrawPt();

        try
        {
            SafeBlockInputStart();
            Std32Cursor.SetCursorPos_AbsDrawPt(ptStartAbs);
            Std32Mouse_Event.MouseEvent_LeftBtnDown();
            await MoveSmooth_PrecisionAsync(ptStartAbs, ptTargetAbs, nMiliSec);
            Std32Mouse_Event.MouseEvent_LeftBtnUp();
            await Task.Delay(50);
        }
        finally
        {
            if (bBkCursor) Std32Cursor.SetCursorPos_AbsDrawPt(ptBk);
            SafeBlockInputStop();
        }

        return Std32Cursor.GetCursorPos_AbsDrawPt() == ptTargetAbs;
    }

    // 윈도우 핸들 기준 좌클릭 드래그 (상대좌표 기반)
    public static async Task<bool> SafeMouseEvent_DragLeft_SmoothAsync(
        IntPtr hWnd, Draw.Point ptStartRel, Draw.Point ptTargetRel,
        bool bBkCursor = true, int nMiliSec = 100)
    {
        Draw.Rectangle rc = Std32Window.GetWindowRect_DrawAbs(hWnd);
        Draw.Point ptStartAbs = new Draw.Point(rc.Left + ptStartRel.X, rc.Top + ptStartRel.Y);
        Draw.Point ptTargetAbs = new Draw.Point(rc.Left + ptTargetRel.X, rc.Top + ptTargetRel.Y);

        return await SafeMouseEvent_DragLeft_SmoothAsync(ptStartAbs, ptTargetAbs, bBkCursor, nMiliSec);
    }

    // 수직 드래그 (컬럼 제거/이동용)
    public static async Task SafeMouseEvent_DragLeft_Smooth_VerticalAsync(
        IntPtr hWnd, Draw.Point ptStartRel, int dy, bool bBkCursor = true, int nMiliSec = 100)
    {
        Draw.Point ptBk = Std32Cursor.GetCursorPos_AbsDrawPt();

        try
        {
            SafeBlockInputStart();

            Std32Cursor.SetCursorPos_RelDrawPt(hWnd, ptStartRel);
            Draw.Point ptCurAbs = Std32Cursor.GetCursorPos_AbsDrawPt();
            Draw.Point ptTargetAbs = new Draw.Point(ptCurAbs.X, ptCurAbs.Y + dy);

            Std32Mouse_Event.MouseEvent_LeftBtnDown();
            await MoveSmooth_PrecisionAsync(ptCurAbs, ptTargetAbs, nMiliSec);
            Std32Mouse_Event.MouseEvent_LeftBtnUp();
            await Task.Delay(50);
        }
        finally
        {
            if (bBkCursor) Std32Cursor.SetCursorPos_AbsDrawPt(ptBk);
            SafeBlockInputStop();
        }
    }

    // 수평 드래그 (컬럼 너비 조정용)
    public static async Task SafeMouseEvent_DragLeft_Smooth_HorizonAsync(
        IntPtr hWnd, Draw.Point ptStartRel, int dx, bool bBkCursor = true, int nMiliSec = 100)
    {
        Draw.Point ptBk = Std32Cursor.GetCursorPos_AbsDrawPt();

        try
        {
            SafeBlockInputStart();

            Std32Cursor.SetCursorPos_RelDrawPt(hWnd, ptStartRel);
            Draw.Point ptCurAbs = Std32Cursor.GetCursorPos_AbsDrawPt();
            Draw.Point ptTargetAbs = new Draw.Point(ptCurAbs.X + dx, ptCurAbs.Y);

            Std32Mouse_Event.MouseEvent_LeftBtnDown();
            await MoveSmooth_PrecisionAsync(ptCurAbs, ptTargetAbs, nMiliSec);
            Std32Mouse_Event.MouseEvent_LeftBtnUp();
            await Task.Delay(50);
        }
        finally
        {
            if (bBkCursor) Std32Cursor.SetCursorPos_AbsDrawPt(ptBk);
            SafeBlockInputStop();
        }
    }

    // 마우스 이동 정밀도 향상 로직 (가속 후 도착지 감속 및 픽셀 단위 정밀 이동)
    private static async Task MoveSmooth_PrecisionAsync(Draw.Point ptStart, Draw.Point ptTarget, int nMiliSec)
    {
        int dx = ptTarget.X - ptStart.X;
        int dy = ptTarget.Y - ptStart.Y;
        double dist = Math.Sqrt(dx * dx + dy * dy);

        // 거리가 10픽셀 이상인 경우에만 정밀 도착 로직 적용
        if (dist > 10)
        {
            // 1. 도착 3픽셀 전 지점까지는 기존 Smooth 방식으로 이동
            double ratio = (dist - 3) / dist;
            Draw.Point ptNear = new Draw.Point(
                ptStart.X + (int)(dx * ratio),
                ptStart.Y + (int)(dy * ratio)
            );

            // 약 80%의 시간을 소요하며 중간 지점까지 이동
            await Std32Mouse_Send.MouseSet_MoveSmooth_ptAbsAsync(ptStart, ptNear, (int)(nMiliSec * 0.8));

            // 2. 마지막 3픽셀 구간: 1픽셀 단위 이동 + 딜레이
            // 도착지 좌표를 윈도우가 확실히 인식하게 하기 위해 30ms씩 3번 끊어서 이동
            for (int i = 1; i <= 3; i++)
            {
                double stepRatio = (double)i / 3;
                Draw.Point ptStep = new Draw.Point(
                    ptNear.X + (int)((ptTarget.X - ptNear.X) * stepRatio),
                    ptNear.Y + (int)((ptTarget.Y - ptNear.Y) * stepRatio)
                );
                Std32Cursor.SetCursorPos_AbsDrawPt(ptStep);
                await Task.Delay(30); 
            }
        }
        else
        {
            // 짧은 거리는 기존 로직 사용
            await Std32Mouse_Send.MouseSet_MoveSmooth_ptAbsAsync(ptStart, ptTarget, nMiliSec);
        }
    }
    #endregion

    #region Mouse Events - DoubleClick (Blocking)
    // 상대좌표 기준 좌클릭 더블클릭 (외부 입력 차단, 커서/포커스 복원)
    public static async Task SafeMouseSend_DblClickLeft_ptRelAsync(IntPtr hWnd, Draw.Point ptClickRel, int betweenClickDelay = 0, bool bBkCursor = true)
    {
        Draw.Point ptBk = Std32Cursor.GetCursorPos_AbsDrawPt();
        IntPtr hWndFocusBk = StdWin32.GetForegroundWindow();
        IntPtr lParam = StdUtil.MakeIntPtrLParam(ptClickRel.X, ptClickRel.Y);

        try
        {
            SafeBlockInputStart();
            Std32Cursor.SetCursorPos_RelDrawPt(hWnd, ptClickRel);
            
            // 첫 번째 클릭
            StdWin32.SendMessage(hWnd, StdWin32.WM_LBUTTONDOWN, 1, (ulong)lParam);
            StdWin32.PostMessage(hWnd, StdWin32.WM_LBUTTONUP, 0, lParam);

            if (betweenClickDelay > 0) await Task.Delay(betweenClickDelay);

            // 두 번째 클릭 (DBLCLK)
            StdWin32.SendMessage(hWnd, StdWin32.WM_LBUTTONDBLCLK, 1, (ulong)lParam);
            StdWin32.PostMessage(hWnd, StdWin32.WM_LBUTTONUP, 0, lParam);
        }
        finally
        {
            if (bBkCursor) Std32Cursor.SetCursorPos_AbsDrawPt(ptBk);
            if (hWndFocusBk != IntPtr.Zero) StdWin32.SetForegroundWindow(hWndFocusBk);
            SafeBlockInputStop();
        }
    }
    #endregion

    #region Mouse Post - Wait Selection (Non-Blocking)
    // Post 방식 좌클릭 후 선택 상태 대기 (외부 입력 차단 없음 - 비정밀 작업용)
    public static async Task<bool> MousePost_ClickLeft_WaitSelectionAsync(
        IntPtr hWnd, Draw.Point ptRelClick, Draw.Point ptRelCheck, int nOrgBrightness, int nRepeatCount = 50, int nBrightGap = 10)
    {
        try
        {
            await Std32Mouse_Post.MousePostAsync_ClickLeft_ptRel(hWnd, ptRelClick);

            for (int i = 0; i < nRepeatCount; i++)
            {
                await s_GlobalCancelToken.WaitIfPausedOrCancelledAsync(); // ESC 중단 체크
                int curBrightness = OfrService.GetPixelBrightnessFrmWndHandle(hWnd, ptRelCheck);
                if (Math.Abs(curBrightness - nOrgBrightness) > nBrightGap) return true;
                await Task.Delay(50);
            }
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MousePost_ClickLeft_WaitSelectionAsync] 예외: {ex.Message}");
            return false;
        }
    }
    #endregion

    #region Checkbox Control
    // 체크박스 상태 설정 (현재 상태 확인 후 클릭)
    public static async Task<StdResult_Status> SetCheckBtnStatusAsync(IntPtr hWnd, bool targetState, int nRepeat = c_nRepeatShort)
    {
        try
        {
            uint uCurrentStatus = Std32Msg_Send.GetCheckStatus(hWnd);
            bool currentState = (uCurrentStatus == StdCommon32.BST_CHECKED);

            if (currentState == targetState) return new StdResult_Status(StdResult.Success);

            for (int i = 0; i < nRepeat; i++)
            {
                await s_GlobalCancelToken.WaitIfPausedOrCancelledAsync(); // ESC 중단 체크
                await Std32Mouse_Post.MousePostAsync_ClickLeft_Center(hWnd);

                for (int j = 0; j < c_nRepeatMany; j++)
                {
                    await s_GlobalCancelToken.WaitIfPausedOrCancelledAsync(); // ESC 중단 체크
                    await Task.Delay(c_nWaitUltraShort);
                    uCurrentStatus = Std32Msg_Send.GetCheckStatus(hWnd);
                    if ((uCurrentStatus == StdCommon32.BST_CHECKED) == targetState) return new StdResult_Status(StdResult.Success);
                }
            }

            return new StdResult_Status(StdResult.Fail, "체크박스 상태 변경 실패", "SetCheckBtnStatusAsync");
        }
        catch (Exception ex)
        {
            return new StdResult_Status(StdResult.Fail, StdUtil.GetExceptionMessage(ex), "SetCheckBtnStatusAsync_Ex");
        }
    }
    #endregion
}
