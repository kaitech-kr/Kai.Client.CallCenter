using System.Threading.Tasks;
using Draw = System.Drawing;

using Kai.Common.StdDll_Common.StdWin32;

namespace Kai.Client.CallCenter.Classes;

/// <summary>
/// 마우스 시뮬레이션 헬퍼 클래스 (외부 입력 차단 포함)
/// </summary>
public static class Simulation_Mouse
{
    #region Blocking
    private static int s_nBlockCount = 0;  // 중첩 호출 카운터

    /// <summary>
    /// 외부 입력 차단 시작 (BlockInput API 호출)
    /// 중첩 호출 지원: 여러 번 호출 가능, 같은 횟수만큼 Stop 호출 필요
    /// </summary>
    public static void SafeBlockInputStart()
    {
        s_nBlockCount++;
        if (s_nBlockCount == 1)  // 최초 호출 시에만 실제 차단
        {
            StdWin32.BlockInput(true);
        }
    }

    /// <summary>
    /// 외부 입력 차단 해제
    /// 중첩 호출 지원: Start와 Stop 호출 횟수가 같아야 실제 해제
    /// </summary>
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

    /// <summary>
    /// 외부 입력 차단 강제 해제 (예외 처리용)
    /// 중첩 카운터 무시하고 무조건 해제
    /// </summary>
    public static void SafeBlockInputForceStop()
    {
        s_nBlockCount = 0;
        StdWin32.BlockInput(false);
    }
    #endregion

    #region Event - Click
    /// <summary>
    /// 상대좌표 기준 좌클릭 (외부 입력 차단, 커서 복원)
    /// </summary>
    /// <param name="hWnd">대상 윈도우 핸들</param>
    /// <param name="ptClickRel">상대 좌표</param>
    /// <param name="bBkCursor">커서 위치 복원 여부</param>
    /// <param name="nDelay">클릭 후 대기 시간 (ms)</param>
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
    /// <summary>
    /// 절대좌표 기준 좌클릭 드래그 (스무스 이동)
    /// </summary>
    /// <param name="ptStartAbs">시작 절대좌표</param>
    /// <param name="ptTargetAbs">목표 절대좌표</param>
    /// <param name="bBkCursor">커서 위치 복원 여부</param>
    /// <param name="nMiliSec">드래그 소요 시간 (ms)</param>
    /// <returns>목표 위치 도달 성공 여부</returns>
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

    /// <summary>
    /// 상대좌표 기준 좌클릭 드래그 (기준점 + 상대좌표)
    /// </summary>
    /// <param name="ptBasic">기준 절대좌표 (윈도우 Left, Top)</param>
    /// <param name="ptStartRel">시작 상대좌표</param>
    /// <param name="ptTargetRel">목표 상대좌표</param>
    /// <param name="bBkCursor">커서 위치 복원 여부</param>
    /// <param name="nMiliSec">드래그 소요 시간 (ms)</param>
    public static async Task<bool> SafeMouseEvent_DragLeft_SmoothAsync(
        Draw.Point ptBasic, Draw.Point ptStartRel, Draw.Point ptTargetRel,
        bool bBkCursor = true, int nMiliSec = 100)
    {
        Draw.Point ptStartAbs = new Draw.Point(ptBasic.X + ptStartRel.X, ptBasic.Y + ptStartRel.Y);
        Draw.Point ptTargetAbs = new Draw.Point(ptBasic.X + ptTargetRel.X, ptBasic.Y + ptTargetRel.Y);

        return await SafeMouseEvent_DragLeft_SmoothAsync(ptStartAbs, ptTargetAbs, bBkCursor, nMiliSec);
    }

    /// <summary>
    /// 윈도우 핸들 기준 좌클릭 드래그 (상대좌표)
    /// </summary>
    /// <param name="hWnd">대상 윈도우 핸들</param>
    /// <param name="ptStartRel">시작 상대좌표</param>
    /// <param name="ptTargetRel">목표 상대좌표</param>
    /// <param name="bBkCursor">커서 위치 복원 여부</param>
    /// <param name="nMiliSec">드래그 소요 시간 (ms)</param>
    public static async Task<bool> SafeMouseEvent_DragLeft_SmoothAsync(
        IntPtr hWnd, Draw.Point ptStartRel, Draw.Point ptTargetRel,
        bool bBkCursor = true, int nMiliSec = 100)
    {
        Draw.Rectangle rc = Std32Window.GetWindowRect_DrawAbs(hWnd);
        Draw.Point ptBasic = new Draw.Point(rc.Left, rc.Top);

        return await SafeMouseEvent_DragLeft_SmoothAsync(ptBasic, ptStartRel, ptTargetRel, bBkCursor, nMiliSec);
    }

    /// <summary>
    /// 수직 드래그 (컬럼 우측 이동용)
    /// </summary>
    /// <param name="hWnd">대상 윈도우 핸들</param>
    /// <param name="ptStartRel">시작 상대좌표</param>
    /// <param name="dy">수직 이동 거리 (음수: 위로, 양수: 아래로)</param>
    /// <param name="bBkCursor">커서 위치 복원 여부</param>
    /// <param name="nMiliSec">드래그 소요 시간 (ms)</param>
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

    /// <summary>
    /// 수평 드래그 (컬럼 너비 조정용)
    /// </summary>
    /// <param name="hWnd">대상 윈도우 핸들</param>
    /// <param name="ptStartRel">시작 상대좌표</param>
    /// <param name="dx">수평 이동 거리 (음수: 왼쪽, 양수: 오른쪽)</param>
    /// <param name="bBkCursor">커서 위치 복원 여부</param>
    /// <param name="nMiliSec">드래그 소요 시간 (ms)</param>
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
}
