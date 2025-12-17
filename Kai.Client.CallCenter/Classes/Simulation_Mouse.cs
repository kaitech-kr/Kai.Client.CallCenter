using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kai.Common.StdDll_Common;
//using Kai.Common.StdDll_Common.StdVar;
using Draw = System.Drawing;

namespace Kai.Client.CallCenter.Classes
{
    public class Simulation_Mouse
    {
        /*
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

        #region Event - DoubleClick
        /// <summary>
        /// 상대좌표 기준 좌클릭 더블클릭 (외부 입력 차단, 커서 복원)
        /// </summary>
        /// <param name="hWnd">대상 윈도우 핸들</param>
        /// <param name="ptClickRel">상대 좌표</param>
        /// <param name="betweenClickDelay">첫 번째와 두 번째 클릭 사이 딜레이 (ms)</param>
        /// <param name="bBkCursor">커서 위치 복원 여부</param>
        /// <param name="nDelay">더블클릭 후 대기 시간 (ms)</param>
        public static async Task SafeMouseSend_DblClickLeft_ptRelAsync(IntPtr hWnd, Draw.Point ptClickRel, int betweenClickDelay = 0, bool bBkCursor = true)
        {
            Draw.Point ptBk = Std32Cursor.GetCursorPos_AbsDrawPt(); // 커서 백업
            IntPtr hWndFocusBk = StdWin32.GetForegroundWindow(); // 포커스 핸들 백업
            IntPtr lParam = StdUtil.MakeIntPtrLParam(ptClickRel.X, ptClickRel.Y);

            try
            {
                // 첫 번째 클릭
                SafeBlockInputStart();
                Std32Cursor.SetCursorPos_RelDrawPt(hWnd, ptClickRel); // 커서 이동
                StdWin32.SendMessage(hWnd, StdWin32.WM_LBUTTONDOWN, 1, (ulong)lParam);
                StdWin32.PostMessage(hWnd, StdWin32.WM_LBUTTONUP, 0, lParam);

                await Task.Delay(betweenClickDelay);

                // 두 번째 클릭 (DBLCLK 메시지)
                StdWin32.SendMessage(hWnd, StdWin32.WM_LBUTTONDBLCLK, 1, (ulong)lParam);
                StdWin32.PostMessage(hWnd, StdWin32.WM_LBUTTONUP, 0, lParam);
                SafeBlockInputStop();

                await Task.Delay(CommonVars.c_nWaitVeryShort);
            }
            finally
            {
                if (bBkCursor) Std32Cursor.SetCursorPos_AbsDrawPt(ptBk); // 커서 복원
                if (hWndFocusBk != IntPtr.Zero) StdWin32.SetForegroundWindow(hWndFocusBk); // 포커스 복원
            }
        }
        #endregion

        #region Post - Click with Selection Wait
        /// <summary>
        /// Post 방식 좌클릭 후 선택 상태 대기 (외부 입력 차단 없음)
        /// - 첫 행 선택 등 Selection 변화 감지용
        /// - nOrgBrightness는 미리 계산해서 전달 (매번 계산 방지)
        /// </summary>
        /// <param name="hWnd">대상 윈도우 핸들</param>
        /// <param name="ptRelClick">클릭할 상대 좌표</param>
        /// <param name="ptRelCheck">선택 체크할 상대 좌표</param>
        /// <param name="nOrgBrightness">원래 밝기값 (미리 계산해서 전달)</param>
        /// <param name="nRepeatCount">최대 반복 횟수 (기본 50 = 약 2.5초)</param>
        /// <param name="nBrightGap">밝기 변화 임계값</param>
        /// <returns>선택 상태 감지 성공 여부</returns>
        public static async Task<bool> MousePost_ClickLeft_WaitSelectionAsync(
            IntPtr hWnd, Draw.Point ptRelClick, Draw.Point ptRelCheck, int nOrgBrightness, int nRepeatCount = 50, int nBrightGap = 10)
        {
            try
            {
                // 클릭 (Post 방식 - 외부 입력 차단 없음)
                await Std32Mouse_Post.MousePostAsync_ClickLeft_ptRel(hWnd, ptRelClick);

                // 선택 상태 대기 (클릭 직후 바로 체크 → 실패 시 대기 후 재시도)
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
        /// <summary>
        /// 체크박스 상태 설정 (현재 상태 읽고, 다르면 클릭)
        /// </summary>
        /// <param name="hWnd">체크박스 핸들</param>
        /// <param name="targetState">목표 상태 (true=체크, false=해제)</param>
        /// <param name="nRepeat">클릭 재시도 횟수</param>
        public static async Task<StdResult_Status> SetCheckBtnStatusAsync(IntPtr hWnd, bool targetState, int nRepeat = c_nRepeatShort)
        {
            try
            {
                // 1. 현재 상태 읽기
                uint uCurrentStatus = Std32Msg_Send.GetCheckStatus(hWnd);
                bool currentState = (uCurrentStatus == StdCommon32.BST_CHECKED);

                // 2. 같으면 바로 성공
                if (currentState == targetState)
                {
                    return new StdResult_Status(StdResult.Success);
                }

                // 3. 다르면 클릭 후 검증 (2중 루프)
                for (int i = 0; i < nRepeat; i++)
                {
                    await Std32Mouse_Post.MousePostAsync_ClickLeft_Center(hWnd);

                    for (int j = 0; j < c_nRepeatShort; j++)
                    {
                        await Task.Delay(c_nWaitUltraShort);

                        // 검증
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
        */
    }
}
