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
    #region Blocking
    private static int s_nBlockCount = 0;  // 중첩 호출 카운터

    // 외부 입력 차단 시작 (BlockInput API, 중첩 호출 지원)
    public static void SafeBlockInputStart()
    {
        s_nBlockCount++;
        if (s_nBlockCount == 1)
        {
            StdWin32.BlockInput(true);
        }
    }

    // 외부 입력 차단 해제 (중첩 호출 지원)
    public static void SafeBlockInputStop()
    {
        if (s_nBlockCount > 0)
        {
            s_nBlockCount--;
            if (s_nBlockCount == 0)
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
    }
    #endregion

    #region Event - Click
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

    #region Event - Drag
    // 절대좌표 기준 좌클릭 드래그 (스무스 이동)
    public static async Task<bool> SafeMouseEvent_DragLeft_SmoothAsync(
        Draw.Point ptStartAbs, Draw.Point ptTargetAbs, bool bBkCursor = true, int nMiliSec = 100)
    {
        Draw.Point ptBk = Std32Cursor.GetCursorPos_AbsDrawPt();

        try
        {
            SafeBlockInputStart();
            Std32Cursor.SetCursorPos_AbsDrawPt(ptStartAbs);
            Std32Mouse_Event.MouseEvent_LeftBtnDown();
            await Std32Mouse_Send.MouseSet_MoveSmooth_ptAbsAsync(ptStartAbs, ptTargetAbs, nMiliSec);
            Std32Mouse_Event.MouseEvent_LeftBtnUp();
        }
        finally
        {
            if (bBkCursor) Std32Cursor.SetCursorPos_AbsDrawPt(ptBk);
            SafeBlockInputStop();
            await Task.Delay(50);
        }

        return Std32Cursor.GetCursorPos_AbsDrawPt() == ptTargetAbs;
    }

    // 상대좌표 기준 좌클릭 드래그 (기준점 + 상대좌표)
    public static async Task<bool> SafeMouseEvent_DragLeft_SmoothAsync(
        Draw.Point ptBasic, Draw.Point ptStartRel, Draw.Point ptTargetRel,
        bool bBkCursor = true, int nMiliSec = 100)
    {
        Draw.Point ptStartAbs = new Draw.Point(ptBasic.X + ptStartRel.X, ptBasic.Y + ptStartRel.Y);
        Draw.Point ptTargetAbs = new Draw.Point(ptBasic.X + ptTargetRel.X, ptBasic.Y + ptTargetRel.Y);

        return await SafeMouseEvent_DragLeft_SmoothAsync(ptStartAbs, ptTargetAbs, bBkCursor, nMiliSec);
    }

    // 윈도우 핸들 기준 좌클릭 드래그 (상대좌표)
    public static async Task<bool> SafeMouseEvent_DragLeft_SmoothAsync(
        IntPtr hWnd, Draw.Point ptStartRel, Draw.Point ptTargetRel,
        bool bBkCursor = true, int nMiliSec = 100)
    {
        Draw.Rectangle rc = Std32Window.GetWindowRect_DrawAbs(hWnd);
        Draw.Point ptBasic = new Draw.Point(rc.Left, rc.Top);

        return await SafeMouseEvent_DragLeft_SmoothAsync(ptBasic, ptStartRel, ptTargetRel, bBkCursor, nMiliSec);
    }

    // 수직 드래그 (dy: 음수=위, 양수=아래)
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
            await Std32Mouse_Send.MouseSet_MoveSmooth_ptAbsAsync(ptCurAbs, ptTargetAbs, nMiliSec);
            Std32Mouse_Event.MouseEvent_LeftBtnUp();
        }
        finally
        {
            if (bBkCursor) Std32Cursor.SetCursorPos_AbsDrawPt(ptBk);
            SafeBlockInputStop();
            await Task.Delay(100);
        }
    }

    // 수평 드래그 (dx: 음수=왼쪽, 양수=오른쪽)
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
            await Std32Mouse_Send.MouseSet_MoveSmooth_ptAbsAsync(ptCurAbs, ptTargetAbs, nMiliSec);
            Std32Mouse_Event.MouseEvent_LeftBtnUp();
        }
        finally
        {
            if (bBkCursor) Std32Cursor.SetCursorPos_AbsDrawPt(ptBk);
            SafeBlockInputStop();
            await Task.Delay(100);
        }
    }
    #endregion

    #region Event - DoubleClick
    // 상대좌표 기준 더블클릭 (외부 입력 차단, 커서/포커스 복원)
    public static async Task SafeMouseSend_DblClickLeft_ptRelAsync(IntPtr hWnd, Draw.Point ptClickRel, int betweenClickDelay = 0, bool bBkCursor = true)
    {
        Draw.Point ptBk = Std32Cursor.GetCursorPos_AbsDrawPt();
        IntPtr hWndFocusBk = StdWin32.GetForegroundWindow();
        IntPtr lParam = StdUtil.MakeIntPtrLParam(ptClickRel.X, ptClickRel.Y);

        try
        {
            SafeBlockInputStart();
            Std32Cursor.SetCursorPos_RelDrawPt(hWnd, ptClickRel);
            StdWin32.SendMessage(hWnd, StdWin32.WM_LBUTTONDOWN, 1, (ulong)lParam);
            StdWin32.PostMessage(hWnd, StdWin32.WM_LBUTTONUP, 0, lParam);

            await Task.Delay(betweenClickDelay);

            StdWin32.SendMessage(hWnd, StdWin32.WM_LBUTTONDBLCLK, 1, (ulong)lParam);
            StdWin32.PostMessage(hWnd, StdWin32.WM_LBUTTONUP, 0, lParam);
            SafeBlockInputStop();

            await Task.Delay(CommonVars.c_nWaitVeryShort);
        }
        finally
        {
            if (bBkCursor) Std32Cursor.SetCursorPos_AbsDrawPt(ptBk);
            if (hWndFocusBk != IntPtr.Zero) StdWin32.SetForegroundWindow(hWndFocusBk);
        }
    }
    #endregion

    #region Post - Click with Selection Wait
    // Post 방식 좌클릭 후 선택 상태 대기 (밝기 변화 감지)
    public static async Task<bool> MousePost_ClickLeft_WaitSelectionAsync(
        IntPtr hWnd, Draw.Point ptRelClick, Draw.Point ptRelCheck, int nOrgBrightness, int nRepeatCount = 50, int nBrightGap = 10)
    {
        try
        {
            await Std32Mouse_Post.MousePostAsync_ClickLeft_ptRel(hWnd, ptRelClick);

            for (int i = 0; i < nRepeatCount; i++)
            {
                int curBrightness = OfrService.GetPixelBrightnessFrmWndHandle(hWnd, ptRelCheck);

                if (Math.Abs(curBrightness - nOrgBrightness) > nBrightGap)
                {
                    return true;
                }

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

    #region Send - Set CheckStatus
    // 체크박스 상태 설정 (현재 상태 읽고, 다르면 클릭)
    public static async Task<StdResult_Status> SetCheckBtnStatusAsync(IntPtr hWnd, bool targetState, int nRepeat = c_nRepeatShort)
    {
        try
        {
            uint uCurrentStatus = Std32Msg_Send.GetCheckStatus(hWnd);
            bool currentState = (uCurrentStatus == StdCommon32.BST_CHECKED);

            if (currentState == targetState)
            {
                return new StdResult_Status(StdResult.Success);
            }

            for (int i = 0; i < nRepeat; i++)
            {
                await Std32Mouse_Post.MousePostAsync_ClickLeft_Center(hWnd);

                for (int j = 0; j < c_nRepeatMany; j++)
                {
                    await Task.Delay(c_nWaitUltraShort);

                    uCurrentStatus = Std32Msg_Send.GetCheckStatus(hWnd);
                    currentState = (uCurrentStatus == StdCommon32.BST_CHECKED);

                    if (currentState == targetState)
                    {
                        return new StdResult_Status(StdResult.Success);
                    }
                }
            }

            return new StdResult_Status(StdResult.Fail, "체크박스 상태 변경 실패", "SetCheckBtnStatusAsync_01");
        }
        catch (Exception ex)
        {
            return new StdResult_Status(StdResult.Fail, StdUtil.GetExceptionMessage(ex), "SetCheckBtnStatusAsync_99");
        }
    }
    #endregion
}
