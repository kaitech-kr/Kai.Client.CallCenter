using System.Runtime.InteropServices;
using Draw = System.Drawing;
using Wnds = System.Windows;

using Kai.Common.NetDll_WpfCtrl.NetOFR;
using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;

using static Kai.Common.StdDll_Common.StdWin32.StdCommon32;

namespace Kai.Client.CallCenter.Classes;
#nullable disable
//public class Simulation_Mouse
//{
//    #region Blocking
//    public static bool s_bBlocked = false;
//    public static void SafeBlockInputStart() // 외부차단
//    {
//        s_bBlocked = true;
//        StdWin32.BlockInput(s_bBlocked);
//    }
//    public static void SafeBlockInputStop() // 외부차단 해제
//    {
//        s_bBlocked = false;
//        StdWin32.BlockInput(s_bBlocked);
//    }
//    #endregion

//    #region Send
//    public static void SafeMouseSend_DblClkLeft_ptRel(IntPtr hWnd, Draw.Point ptRel, int nDelay = 30)
//    {
//        Draw.Point ptBk = Std32Cursor.GetCursorPos_AbsDrawPt(); // 원래위치 백업
//        int x = ptRel.X;
//        int y = ptRel.Y;
//        ulong lParam = (ulong)StdUtil.MakeIntPtrLParam(x, y);

//        try
//        {
//            // Click
//            SafeBlockInputStart(); // 외부차단
//            StdWin32.SendMessage(hWnd, StdCommon32.WM_LBUTTONDOWN, 1, lParam);
//            StdWin32.SendMessage(hWnd, StdCommon32.WM_LBUTTONUP, 0, lParam);
//            Thread.Sleep(nDelay);

//            // DblClick
//            Std32Cursor.SetCursorPos_RelDrawPos(hWnd, x, y); // 커서이동
//            StdWin32.SendMessage(hWnd, StdCommon32.WM_LBUTTONDBLCLK, 1, lParam);
//            StdWin32.SendMessage(hWnd, StdCommon32.WM_LBUTTONUP, 0, lParam);
//        }
//        finally
//        {
//            Std32Cursor.SetCursorPos_AbsDrawPt(ptBk);
//            SafeBlockInputStop();
//            Thread.Sleep(50);
//        }
//    }
//    #endregion

//    #region Post
//    public static async Task<bool> SafeMousePost_ClickLeft_ptRel_WaitBrightChange(
//        IntPtr hWnd, Draw.Point ptRelClk, Draw.Point ptRelChk, int nOrgBrightness, int nRepeatCount = 50, int nBrightGab = 10)
//    {
//        try
//        {
//            SafeBlockInputStart();
//            Std32Mouse_Post.MousePost_ClickLeft_ptRel(hWnd, ptRelClk); // 클릭
//            //Std32Cursor.SetCursorPos_RelDrawPt(hWnd, ptRelChk); // Test
//            SafeBlockInputStop();

//            // Check
//            int curBrightness = 255;
//            bool bResult = false;
//            for (int i = 0; i < nRepeatCount; i++)
//            {
//                await Task.Delay(50); // 무조건 대기

//                curBrightness = OfrService.GetPixelBrightnessFrmWndHandle(hWnd, ptRelChk);
//                //Debug.WriteLine($"Brightness: {curBrightness} <- Org: {nOrgBrightness}"); // 디버그용

//                if (Math.Abs(curBrightness - nOrgBrightness) > nBrightGab)
//                {
//                    bResult = true;
//                    break;
//                }
//            }

//            return bResult;
//        }
//        finally
//        {
//            SafeBlockInputStop();
//            Thread.Sleep(50);
//        }
//    }
//    //public static async Task<bool> SafeMousePost_ClickLeft_ptRel_WaitBrightChange(IntPtr hWnd, Draw.Rectangle rcRel, int nDelayCount = 50)
//    //{
//    //    try
//    //    {
//    //        Draw.Rectangle rcUsing = new Draw.Rectangle(rcRel.Left + 1, rcRel.Top + 1, 1, 1); // 속도를 위하여 작은 영역을
//    //        Bitmap bmpBk = OfrService.CaptureScreenRect_InWndHandle(hWnd, rcUsing);//, $"{s_sImgFilesPath}\\오더TEST.png");
//    //        if (bmpBk == null) return false; // 캡쳐 실패시 종료

//    //        Draw.Point ptClick = StdUtil.GetCenterDrawPoint(rcRel);
//    //        Draw.Point ptCheck = new Draw.Point(5, 5); // 항상 배경인 위치를 고른다
//    //        int bkBright = OfrService.GetBrightness_PerPixel(bmpBk, ptCheck);
//    //        int curBright = bkBright;

//    //        // Click
//    //        Std32Mouse_Post.MousePost_ClickLeft_ptRel(hWnd, ptClick);

//    //        // Check
//    //        for (int i = 0; i < nDelayCount; i++)
//    //        {
//    //            await Task.Delay(50); // 무조건 대기

//    //            Bitmap bmpCur = OfrService.CaptureScreenRect_InWndHandle(hWnd, rcUsing);
//    //            if (bmpCur == null) return false; // 캡쳐 실패시 종료
//    //            curBright = OfrService.GetBrightness_PerPixel(bmpCur, ptCheck);

//    //            if (curBright != bkBright) break;
//    //        }

//    //        return curBright != bkBright;
//    //    }
//    //    finally
//    //    {
//    //        SafeBlockInputStop();
//    //        Thread.Sleep(50);
//    //    }
//    //}

//    public static async Task<bool> SafeMousePost_ClickLeft_ptRel_WaitDie(IntPtr hWndTop, Draw.Point ptRel, int nDelayCount = 50)
//    {
//        try
//        {
//            // Get hWnd
//            IntPtr hWndFind = Std32Window.GetWndHandle_FromRelDrawPt(hWndTop, ptRel);
//            if (hWndFind == IntPtr.Zero) return false;

//            // Click
//            SafeBlockInputStart(); // 외부차단
//            Std32Mouse_Post.MousePost_ClickLeft(hWndFind);
//            SafeBlockInputStop();

//            // Check
//            return await Wnds.Application.Current.Dispatcher.Invoke(async () =>
//            {
//                bool bVisible = true;
//                for (int i = 0; i < nDelayCount; i++)
//                {
//                    await Task.Delay(50); // 무조건 대기

//                    bVisible = Std32Window.IsWindowVisible(hWndTop);
//                    if (!bVisible) break; ;
//                }

//                return !bVisible;
//            });
//        }
//        finally
//        {
//            SafeBlockInputStop();
//            Thread.Sleep(50);
//        }
//    }
//    public static async Task<bool> SafeMousePost_ClickLeft_WaitDie(IntPtr hWndBtn, IntPtr hWndTop, int nDelayCount = 50)
//    {
//        try
//        {
//            // Click
//            SafeBlockInputStart(); // 외부차단
//            Std32Mouse_Post.MousePost_ClickLeft(hWndBtn);
//            //SafeBlockInputStop();

//            // Check
//            return await Wnds.Application.Current.Dispatcher.Invoke(async () =>
//            {
//                bool bVisible = true;
//                for (int i = 0; i < nDelayCount; i++)
//                {
//                    await Task.Delay(50); // 무조건 대기

//                    bVisible = Std32Window.IsWindowVisible(hWndTop);
//                    if (!bVisible) break; ;
//                }

//                return !bVisible;
//            });
//        }
//        finally
//        {
//            SafeBlockInputStop();
//            Thread.Sleep(50);
//        }
//    }

//    public static void SafeMousePost_ClickLeft_ptRel(IntPtr hWnd, Draw.Point ptClickRel)
//    {
//        try
//        {
//            SafeBlockInputStart(); // 외부차단
//            Std32Mouse_Post.MousePost_ClickLeft_ptRel(hWnd, ptClickRel);
//        }
//        finally
//        {
//            SafeBlockInputStop();
//            Thread.Sleep(50);
//        }
//    }
//    public static bool SafeMousePost_ClickLeft(IntPtr hWnd)
//    {
//        try
//        {
//            SafeBlockInputStart(); // 외부차단
//            Std32Mouse_Post.MousePost_ClickLeft(hWnd);

//            return true;
//        }
//        finally
//        {
//            SafeBlockInputStop();
//            Thread.Sleep(50);
//        }
//    }
//    public static async Task<bool> SafeMousePost_ClickLeft_CenterAsync(IntPtr hWnd, int nDelay = c_nWaitShort)
//    {
//        try
//        {
//            SafeBlockInputStart(); // 외부차단
//            Std32Mouse_Post.MousePost_ClickLeft_Center(hWnd);

//            return true;
//        }
//        finally
//        {
//            SafeBlockInputStop();
//            await Task.Delay(nDelay);
//        }
//    }
//    public static async Task<bool> SafeMousePost_ChkNclickLeft(IntPtr hWnd, int nDelay = c_nWaitShort)
//    {
//        try
//        {
//            IntPtr hWndCheck = Std32Window.GetWndHandle_FromRelDrawPos(hWnd, 3, 3);
//            if (hWndCheck != hWnd)
//            {
//                Std32Window.PostCloseWindow(hWndCheck);
//                for (int i = 0; i < 10; i++)
//                {
//                    await Task.Delay(nDelay);
//                    hWndCheck = Std32Window.GetWndHandle_FromRelDrawPos(hWnd, 3, 3);
//                    if (hWndCheck == hWnd) break;
//                }

//                if (hWndCheck != hWnd) return false;
//            }

//            SafeBlockInputStart(); // 외부차단
//            Std32Mouse_Post.MousePost_ClickLeft(hWnd);

//            return true;
//        }
//        finally
//        {
//            SafeBlockInputStop();
//            await Task.Delay(nDelay);
//        }
//    }

//    public static async Task<bool> SafeMousePost_ChkNclickLeft_CenterAsync(IntPtr hWnd, int nDelay = c_nWaitShort)
//    {
//        try
//        {
//            Draw.Rectangle rc = Std32Window.GetWindowRect_DrawAbs(hWnd);
//            Draw.Point pt = StdUtil.GetCenterDrawPoint(rc);

//            IntPtr hWndCheck = Std32Window.GetWndHandle_FromAbsDrawPt(pt);
//            if (hWndCheck != hWnd)
//            {
//                Std32Window.PostCloseWindow(hWndCheck);
//                for (int i = 0; i < 10; i++)
//                {
//                    await Task.Delay(nDelay);
//                    hWndCheck = Std32Window.GetWndHandle_FromAbsDrawPt(pt);
//                    if (hWndCheck == hWnd) break;
//                }

//                if (hWndCheck != hWnd) return false;
//            }

//            int x = rc.Width / 2;
//            int y = rc.Height / 2;

//            SafeBlockInputStart(); // 외부차단
//            Std32Mouse_Post.MousePost_ClickLeft(hWnd, x, y);

//            return true;
//        }
//        finally
//        {
//            SafeBlockInputStop();
//            await Task.Delay(nDelay);
//        }
//    }
//    public static bool SafeMousePost_ClearNclickLeft_ptRel(IntPtr hWnd, Draw.Point ptClick)
//    {
//        try
//        {
//            IntPtr hWndCheck = Std32Window.GetWndHandle_FromRelDrawPt(hWnd, ptClick);
//            if (hWndCheck != hWnd)
//            {
//                Std32Window.PostCloseWindow(hWndCheck);
//                for (int i = 0; i < 10; i++)
//                {
//                    Thread.Sleep(100);
//                    hWndCheck = Std32Window.GetWndHandle_FromRelDrawPt(hWnd, ptClick);
//                    if (hWndCheck == hWnd) break;
//                }

//                if (hWndCheck != hWnd) return false;
//            }

//            SafeBlockInputStart(); // 외부차단
//            Std32Mouse_Post.MousePost_ClickLeft_ptRel(hWnd, ptClick);

//            return true;
//        }
//        finally
//        {
//            SafeBlockInputStop();
//            Thread.Sleep(50);
//        }
//    }

//    public static void SafeMousePost_ClickRight(IntPtr hWnd, int xGab = 3, int yGab = 3)
//    {
//        try
//        {
//            SafeBlockInputStart(); // 외부차단
//            Std32Mouse_Post.MousePost_ClickRight(hWnd, xGab, yGab); // 클릭
//        }
//        finally
//        {
//            SafeBlockInputStop();
//            Thread.Sleep(50);
//        }
//    }

//    //[Obsolete("사용하지 마세요. MouseSend_DblClkLeft_ptRel 사용하세요.")]
//    //public static void MousePost_DblClkLeft_ptRel(IntPtr hWnd, Draw.Point ptRel)
//    //{
//    //    Draw.Point ptBk = Std32Cursor.GetCursorPos_AbsDrawPt(); // 원래위치 백업
//    //    int x = ptRel.X;
//    //    int y = ptRel.Y;
//    //    IntPtr lParam = (y << 16) | x; // y 좌표를 상위 16비트, x를 하위 16비트

//    //    try
//    //    {
//    //        // DblClick
//    //        SafeBlockInputStart(); // 외부차단
//    //        Std32Cursor.SetCursorPos_RelDrawPos(hWnd, x, y); // 커서이동
//    //        StdWin32.PostMessage(hWnd, StdCommon32.WM_LBUTTONDBLCLK, 1, lParam);
//    //    }
//    //    finally
//    //    {
//    //        //Std32Cursor.SetCursorPos_AbsDrawPt(ptBk); // 원래위치 복구
//    //        SafeBlockInputStop();
//    //        Thread.Sleep(50);
//    //    }
//    //}
//    #endregion

//    #region Event
//    public static void SafeMouseEvent_ClickLeft_ptRel(IntPtr hWnd, Draw.Point ptClickRel, bool bBkCursor = true, int nDelay = 50)
//    {
//        Draw.Point ptBk = Std32Cursor.GetCursorPos_AbsDrawPt(); // 원래위치 백업

//        try
//        {
//            SafeBlockInputStart(); // 외부차단
//            Std32Cursor.SetCursorPos_RelDrawPt(hWnd, ptClickRel); // 커서이동
//            Std32Mouse_Event.MouseEvent_ClickLeft_AtCurPos(nDelay);
//        }
//        finally
//        {
//            if (bBkCursor) Std32Cursor.SetCursorPos_AbsDrawPt(ptBk);
//            SafeBlockInputStop();
//            Thread.Sleep(50);
//        }
//    }
//    //public static void SafeMouseEvent_ClickLeft_posRel(IntPtr hWnd, int x, int y, bool bBkCursor = true, int nDelay = 30)
//    //{
//    //    Draw.Point ptBk = Std32Cursor.GetCursorPos_AbsDrawPt(); // 원래위치 백업

//    //    try
//    //    {
//    //        SafeBlockInputStart(); // 외부차단
//    //        Std32Cursor.SetCursorPos_RelDrawPos(hWnd, x, y); // 커서이동
//    //        Std32Mouse_Event.MouseEvent_ClickLeft_AtCurPos(nDelay);
//    //    }
//    //    finally
//    //    {
//    //        if (bBkCursor) Std32Cursor.SetCursorPos_AbsDrawPt(ptBk);
//    //        SafeBlockInputStop();
//    //        Thread.Sleep(50);
//    //    }
//    //}
//    //public static async Task<bool> SafeMouseEvent_ClickLeft_posRel_WaitBrightDark(IntPtr hWnd, int x, int y, int nLimitBright, bool bBkCursor = true)
//    //{
//    //    Draw.Point ptBk = Std32Cursor.GetCursorPos_AbsDrawPt(); // 원래위치 백업

//    //    try
//    //    {
//    //        SafeBlockInputStart(); // 외부차단
//    //        Std32Cursor.SetCursorPos_RelDrawPos(hWnd, x, y); // 커서이동
//    //        Std32Mouse_Event.MouseEvent_LeftBtnDown(); // 좌측버튼 누르고

//    //        bool bClicked = false;
//    //        for (int i = 0; i < 100; i++)
//    //        {
//    //            if (OfrService.GetPixelBrightnessFrmWndHandle(hWnd, x, y) < nLimitBright)
//    //            {
//    //                bClicked = true; // 클릭되었다
//    //                break;
//    //            }
//    //            await Task.Delay(20); // 무조건 대기
//    //        }

//    //        return bClicked;
//    //    }
//    //    finally
//    //    {
//    //        if (bBkCursor) Std32Cursor.SetCursorPos_AbsDrawPt(ptBk);
//    //        Std32Mouse_Event.MouseEvent_LeftBtnUp(); // 좌측버튼 떼고
//    //        SafeBlockInputStop();
//    //        Thread.Sleep(50);
//    //    }
//    //}
//    public static void SafeMouseEvent_DblClkLeft_ptRel(IntPtr hWnd, Draw.Point ptClickRel, bool bBkCursor = true, int nDelay = 30)
//    {
//        Draw.Point ptBk = Std32Cursor.GetCursorPos_AbsDrawPt(); // 원래위치 백업

//        try
//        {
//            SafeBlockInputStart(); // 외부차단
//            Std32Cursor.SetCursorPos_RelDrawPt(hWnd, ptClickRel); // 커서이동
//            Std32Mouse_Event.MouseEvent_DblClkLeft_AtCurPos(nDelay);
//        }
//        finally
//        {
//            if (bBkCursor) Std32Cursor.SetCursorPos_AbsDrawPt(ptBk);
//            SafeBlockInputStop();
//            Thread.Sleep(50);
//        }
//    }

//    public static bool SafeMouseEvent_DragLeft_Smooth(Draw.Point ptStartAbs, Draw.Point ptTargetAbs, bool bBkCursor = true, int nMiliSec = 100)
//    {
//        Draw.Point ptBk = Std32Cursor.GetCursorPos_AbsDrawPt(); // 원래위치 백업
//        Draw.Point ptCur = ptBk;

//        try
//        {
//            SafeBlockInputStart(); // 외부차단           
//            Std32Cursor.SetCursorPos_AbsDrawPt(ptStartAbs); // 시작 위치로 가서           
//            Std32Mouse_Event.MouseEvent_LeftBtnDown(); // 좌측버튼 누르고    
//            Std32Mouse_Send.MouseSet_MoveSmooth_ptAbs(ptStartAbs, ptTargetAbs, nMiliSec); // 이동           
//            Std32Mouse_Event.MouseEvent_LeftBtnUp(); // 좌측버튼 떼고           
//        }
//        finally
//        {
//            if (bBkCursor) Std32Cursor.SetCursorPos_AbsDrawPt(ptBk);
//            SafeBlockInputStop();
//            Thread.Sleep(50);
//        }

//        return Std32Cursor.GetCursorPos_AbsDrawPt() == ptTargetAbs;
//    }
//    public static bool SafeMouseEvent_DragLeft_Smooth(Draw.Point ptBasic, Draw.Point ptStartRel, Draw.Point ptTargetRel, bool bBkCursor = true, int nMiliSec = 100)
//    {
//        Draw.Point ptStartAbs = new Draw.Point(ptBasic.X + ptStartRel.X, ptBasic.Y + ptStartRel.Y);
//        Draw.Point ptTargetAbs = new Draw.Point(ptBasic.X + ptTargetRel.X, ptBasic.Y + ptTargetRel.Y);

//        return SafeMouseEvent_DragLeft_Smooth(ptStartAbs, ptTargetAbs, bBkCursor, nMiliSec);
//    }
//    public static bool SafeMouseEvent_DragLeft_Smooth(IntPtr hWnd, Draw.Point ptStartRel, Draw.Point ptTargetRel, bool bBkCursor = true, int nMiliSec = 100)
//    {
//        Draw.Rectangle rc = Std32Window.GetWindowRect_DrawAbs(hWnd);
//        Draw.Point ptBasic = new Draw.Point(rc.Left, rc.Top);

//        return SafeMouseEvent_DragLeft_Smooth(ptBasic, ptStartRel, ptTargetRel, bBkCursor, nMiliSec);
//    }

//    public static void SafeMouseEvent_DragLeft_Smooth_Vertical(IntPtr hWnd, Draw.Point ptStartRel, int dy, bool bBkCursor = true, int nMiliSec = 100)
//    {
//        Draw.Point ptBk = Std32Cursor.GetCursorPos_AbsDrawPt(); // 원래위치 백업
//        Draw.Point ptCurAbs, ptTargetAbs;

//        try
//        {
//            SafeBlockInputStart(); // 외부차단

//            // 시작 위치 및 목표 위치 설정
//            Std32Cursor.SetCursorPos_RelDrawPt(hWnd, ptStartRel);
//            ptCurAbs = Std32Cursor.GetCursorPos_AbsDrawPt();
//            ptTargetAbs = new Draw.Point(ptCurAbs.X, ptCurAbs.Y + dy);

//            Std32Mouse_Event.MouseEvent_LeftBtnDown(); // 좌측버튼 누르고
//            Std32Mouse_Send.MouseSet_MoveSmooth_ptAbs(ptCurAbs, ptTargetAbs, nMiliSec);  // 이동          
//            Std32Mouse_Event.MouseEvent_LeftBtnUp();  // 좌측버튼 떼고
//        }
//        finally
//        {
//            if (bBkCursor) Std32Cursor.SetCursorPos_AbsDrawPt(ptBk);
//            SafeBlockInputStop();
//            Thread.Sleep(100);
//        }
//    }
//    public static void SafeMouseEvent_DragLeft_Smooth_Horizon(IntPtr hWnd, Draw.Point ptStartRel, int dx, bool bBkCursor = true, int nMiliSec = 100)
//    {
//        Draw.Point ptBk = Std32Cursor.GetCursorPos_AbsDrawPt(); // 원래위치 백업

//        try
//        {
//            SafeBlockInputStart(); // 외부차단

//            // 시작 위치 및 목표 위치 설정
//            Std32Cursor.SetCursorPos_RelDrawPt(hWnd, ptStartRel);
//            Draw.Point ptCurAbs = Std32Cursor.GetCursorPos_AbsDrawPt();
//            Draw.Point ptTargetAbs = new Draw.Point(ptCurAbs.X + dx, ptCurAbs.Y);

//            Std32Mouse_Event.MouseEvent_LeftBtnDown(); // 좌측버튼 누르고
//            Std32Mouse_Send.MouseSet_MoveSmooth_ptAbs(ptCurAbs, ptTargetAbs, nMiliSec); // 이동           
//            Std32Mouse_Event.MouseEvent_LeftBtnUp(); // 좌측버튼 떼고
//        }
//        finally
//        {
//            if (bBkCursor) Std32Cursor.SetCursorPos_AbsDrawPt(ptBk);
//            SafeBlockInputStop();
//            Thread.Sleep(100);
//        }
//    }
//    #endregion

//    #region Input
//    //public static void SafeMouseInput_DblClkLeft_ptRel(IntPtr hWnd, Draw.Point ptClickRel, bool bBkCursor = true, int nDelay = 30)
//    //{
//    //    Draw.Point ptBk = Std32Cursor.GetCursorPos_AbsDrawPt(); // 원래위치 백업

//    //    try
//    //    {
//    //        SafeBlockInputStart(); // 외부차단
//    //        Std32Cursor.SetCursorPos_RelDrawPt(hWnd, ptClickRel); // 커서이동
//    //        Std32Mouse_Input.MouseInput_ClickLeftHere();
//    //        Thread.Sleep(nDelay);
//    //        Std32Mouse_Input.MouseInput_ClickLeftHere();
//    //        Thread.Sleep(nDelay);
//    //    }
//    //    finally
//    //    {
//    //        if (bBkCursor) Std32Cursor.SetCursorPos_AbsDrawPt(ptBk);
//    //        SafeBlockInputStop();
//    //        Thread.Sleep(50);
//    //    }
//    //}
//    #endregion
//}

//public static class Simulation_Keyboard
//{
//    #region 한글 매핑

//    private static readonly Dictionary<char, string> _jamoToKey = new()
//    {
//        ['ㄱ'] = "r", ['ㄲ'] = "R", ['ㄴ'] = "s", ['ㄷ'] = "e", ['ㄸ'] = "E", ['ㄹ'] = "f", ['ㅁ'] = "a", ['ㅂ'] = "q", ['ㅃ'] = "Q", ['ㅅ'] = "t",
//        ['ㅆ'] = "T", ['ㅇ'] = "d", ['ㅈ'] = "w", ['ㅉ'] = "W", ['ㅊ'] = "c", ['ㅋ'] = "z", ['ㅌ'] = "x", ['ㅍ'] = "v", ['ㅎ'] = "g",
//        ['ㅏ'] = "k", ['ㅐ'] = "o", ['ㅑ'] = "i", ['ㅒ'] = "O", ['ㅓ'] = "j", ['ㅔ'] = "p", ['ㅕ'] = "u", ['ㅖ'] = "P", ['ㅗ'] = "h",
//        ['ㅘ'] = "hk", ['ㅙ'] = "ho", ['ㅚ'] = "hl", ['ㅛ'] = "y", ['ㅜ'] = "n", ['ㅝ'] = "nj", ['ㅞ'] = "np", ['ㅟ'] = "nl", ['ㅠ'] = "b",
//        ['ㅡ'] = "m", ['ㅢ'] = "ml", ['ㅣ'] = "l", ['ㄳ'] = "rt", ['ㄵ'] = "sw", ['ㄶ'] = "sg", ['ㄺ'] = "fr", ['ㄻ'] = "fa", ['ㄼ'] = "fq",
//        ['ㄽ'] = "ft", ['ㄾ'] = "fx", ['ㄿ'] = "fv", ['ㅀ'] = "fg", ['ㅄ'] = "qt"
//    };

//    private static List<char> DecomposeHangul(char syllable)
//    {
//        int baseCode = syllable - 0xAC00;
//        int cho = baseCode / (21 * 28);
//        int jung = (baseCode % (21 * 28)) / 28;
//        int jong = baseCode % 28;

//        char[] CHO = "ㄱㄲㄴㄷㄸㄹㅁㅂㅃㅅㅆㅇㅈㅉㅊㅋㅌㅍㅎ".ToCharArray();
//        char[] JUNG = "ㅏㅐㅑㅒㅓㅔㅕㅖㅗㅘㅙㅚㅛㅜㅝㅞㅟㅠㅡㅢㅣ".ToCharArray();
//        char[] JONG = "\0ㄱㄲㄳㄴㄵㄶㄷㄹㄺㄻㄼㄽㄾㄿㅀㅁㅂㅄㅅㅆㅇㅈㅊㅋㅌㅍㅎ".ToCharArray();

//        var result = new List<char> { CHO[cho], JUNG[jung] };
//        if (jong != 0)
//            result.Add(JONG[jong]);

//        return result;
//    }

//    private static ushort CharToScan(char eng, out bool shift)
//    {
//        short vk = StdWin32.VkKeyScan(eng);
//        shift = ((vk >> 8) & 1) != 0;
//        byte vkCode = (byte)(vk & 0xff);
//        return (ushort)StdWin32.MapVirtualKey(vkCode, 0);
//    }
//    private static List<(ushort scan, bool shift)> ToScanCodes(char c)
//    {
//        var result = new List<(ushort, bool)>();

//        if (c >= 0xAC00 && c <= 0xD7A3) // 한글 음절
//        {
//            var jamos = DecomposeHangul(c);
//            foreach (var j in jamos)
//            {
//                if (_jamoToKey.TryGetValue(j, out var keys))
//                {
//                    foreach (var k in keys)
//                    {
//                        bool shift;
//                        ushort scan = CharToScan(k, out shift);
//                        if (scan != 0)
//                            result.Add((scan, shift));
//                    }
//                }
//            }
//        }
//        else if (char.IsDigit(c) || char.IsPunctuation(c) || char.IsSymbol(c))
//        {
//            bool shift;
//            ushort scan = CharToScan(c, out shift);
//            if (scan != 0)
//                result.Add((scan, shift));
//        }

//        return result;
//    }

//    public static async Task KeyPost_HanScanOnlyTextAsync(IntPtr hWnd, string sData, bool bLastEnter = false, int delay = 50)
//    {
//        var ptBk = Std32Cursor.GetCursorPos_AbsDrawPt();

//        try
//        {
//            Std32Window.SetForegroundWindow(hWnd);
//            Std32Window.SetFocus(hWnd);
//            await Task.Delay(delay);

//            foreach (char c in sData)
//            {
//                var scans = ToScanCodes(c);
//                foreach (var (scan, shift) in scans)
//                {
//                    if (shift) Std32Key_Input.SendScanKey_Down(0x2A);
//                    Std32Key_Input.SendScanKey_Click(scan);
//                    if (shift) Std32Key_Input.SendScanKey_Up(0x2A);
//                    await Task.Delay(delay);
//                }
//            }

//            if (bLastEnter)
//                Std32Key_Input.SendScanKey_Click(0x1C); // Enter
//        }
//        finally
//        {
//            Std32Cursor.SetCursorPos_AbsDrawPt(ptBk);
//        }
//    }
//    #endregion

//    #region ASCII 전용
//    private static List<(char ch, int vk, int mod)> TextToVkCodes(string text)
//    {
//        var result = new List<(char, int, int)>();
//        foreach (char c in text)
//        {
//            short code = StdWin32.VkKeyScan(c);
//            int vk = code & 0xFF;
//            int mod = (code >> 8) & 0xFF;
//            result.Add((c, vk, mod));
//        }
//        return result;
//    }

//    private static async Task SendVirtualKeyWithModAsync(IntPtr hWnd, int vk, int mod, int delay)
//    {
//        if ((mod & 1) != 0) Std32Key_Msg.KeyPost_Down(hWnd, StdCommon32.VK_SHIFT);
//        if ((mod & 2) != 0) Std32Key_Msg.KeyPost_Down(hWnd, StdCommon32.VK_CONTROL);
//        if ((mod & 4) != 0) Std32Key_Msg.KeyPost_Down(hWnd, StdCommon32.VK_MENU);

//        Std32Key_Msg.KeyPost_Down(hWnd, (uint)vk);
//        await Task.Delay(delay);

//        if ((mod & 1) != 0) Std32Key_Msg.KeyPost_Up(hWnd, StdCommon32.VK_SHIFT);
//        if ((mod & 2) != 0) Std32Key_Msg.KeyPost_Up(hWnd, StdCommon32.VK_CONTROL);
//        if ((mod & 4) != 0) Std32Key_Msg.KeyPost_Up(hWnd, StdCommon32.VK_MENU);
//    }

//    public static async Task KeyPost_AsciiTextAsync(IntPtr hWnd, uint uBefKey, string sData, bool bLastEnter = false, int delay = 50)
//    {
//        var ptBk = Std32Cursor.GetCursorPos_AbsDrawPt();
//        Std32Mouse_Post.MousePost_ClickLeft(hWnd);

//        if (uBefKey > 0)
//        {
//            await Task.Delay(delay);
//            Std32Key_Msg.KeyPost_Down(hWnd, uBefKey);
//        }

//        var list = TextToVkCodes(sData);
//        try
//        {
//            //Simulation_Mouse.SafeBlockInputStart(); // 이거 문제 생김 - 키보드에선 쓰지만말자

//            foreach (var (ch, vk, mod) in list)
//                await SendVirtualKeyWithModAsync(hWnd, vk, mod, delay);

//            if (bLastEnter)
//                Std32Key_Msg.KeyPost_Down(hWnd, StdCommon32.VK_RETURN);
//        }
//        finally
//        {
//            Std32Cursor.SetCursorPos_AbsDrawPt(ptBk);
//            //Simulation_Mouse.SafeBlockInputStop();
//            await Task.Delay(delay);
//        }
//    }

//    public static async Task KeyPost_DeleteNAsciiTextAsync(IntPtr hWnd, string sData, bool bLastEnter = false, int delay = 50)
//    {
//        var ptBk = Std32Cursor.GetCursorPos_AbsDrawPt();
//        Std32Mouse_Post.MousePost_ClickLeft(hWnd);

//        Std32Key_Msg.KeyPost_Down(hWnd, VK_HOME);
//        for(int i = 0; i < 50; i++) Std32Key_Msg.KeyPost_Down(hWnd, VK_DELETE);

//        var list = TextToVkCodes(sData);
//        try
//        {
//            //Simulation_Mouse.SafeBlockInputStart(); // 이거 문제 생김 - 키보드에선 쓰지만말자

//            foreach (var (ch, vk, mod) in list)
//                await SendVirtualKeyWithModAsync(hWnd, vk, mod, delay);

//            if (bLastEnter)
//                Std32Key_Msg.KeyPost_Down(hWnd, StdCommon32.VK_RETURN);
//        }
//        finally
//        {
//            Std32Cursor.SetCursorPos_AbsDrawPt(ptBk);
//            //Simulation_Mouse.SafeBlockInputStop();
//            await Task.Delay(delay);
//        }
//    }

//    //private static async Task KeyPost_AsciiCharAsync(IntPtr hWnd, char ch, int delay)
//    //{
//    //    var list = TextToVkCodes(ch.ToString());
//    //    foreach (var (chx, vk, mod) in list)
//    //        await SendVirtualKeyWithModAsync(hWnd, vk, mod, delay);
//    //}
//    #endregion

//    #region Unicode
//    public static async Task KeyInput_UnicodeTextAsync(IntPtr hWnd, string sData, bool bLastEnter = false, int delay = 30)
//    {
//        var ptBk = Std32Cursor.GetCursorPos_AbsDrawPt();

//        try
//        {
//            Std32Window.SetForegroundWindow(hWnd);
//            Std32Window.SetFocus(hWnd);
//            await Task.Delay(delay);

//            foreach (char ch in sData)
//            {
//                INPUT[] inputs = new INPUT[2];

//                // Key Down
//                inputs[0].type = INPUT_KEYBOARD;
//                inputs[0].u.ki.wScan = ch;
//                inputs[0].u.ki.wVk = 0;
//                inputs[0].u.ki.dwFlags = KEYEVENTF_UNICODE;
//                inputs[0].u.ki.time = 0;
//                inputs[0].u.ki.dwExtraInfo = IntPtr.Zero;

//                // Key Up
//                inputs[1].type = INPUT_KEYBOARD;
//                inputs[1].u.ki.wScan = ch;
//                inputs[1].u.ki.wVk = 0;
//                inputs[1].u.ki.dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP;
//                inputs[1].u.ki.time = 0;
//                inputs[1].u.ki.dwExtraInfo = IntPtr.Zero;

//                StdWin32.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
//                await Task.Delay(delay);
//            }

//            if (bLastEnter)
//            {
//                Std32Key_Input.SendScanKey_Click(0x1C); // Enter
//            }
//        }
//        finally
//        {
//            Std32Cursor.SetCursorPos_AbsDrawPt(ptBk);
//        }
//    }

//    public static async Task KeyInput_UnicodeTextAsync(IntPtr hWndTop, Draw.Point ptRel, string sData, bool bLastEnter = false, int delay = 30)
//    {
//        IntPtr hWndTarget = Std32Window.GetWndHandle_FromRelDrawPt(hWndTop, ptRel);
//        await KeyInput_UnicodeTextAsync(hWndTarget, sData, bLastEnter, delay);
//    }
//    #endregion
//}
#nullable enable