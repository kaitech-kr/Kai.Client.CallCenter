using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;
using System.Threading.Tasks;
using static Kai.Client.CallCenter.Classes.CommonVars;

namespace Kai.Client.CallCenter.Classes;

/// <summary>
/// 키보드 시뮬레이션 헬퍼 클래스
/// - Modifier 키(Ctrl, Shift, Alt)와 함께 키 입력
/// </summary>
public static class Simulation_Keyboard
{
    /// <summary>
    /// Ctrl+A (전체 선택) 단축키
    /// </summary>
    public static async Task KeyPost_CtrlA_SelectAllAsync(IntPtr hWnd, int delay = 20)
    {
        StdWin32.PostMessage(hWnd, StdCommon32.WM_CHAR, 0x01, IntPtr.Zero);
        await Task.Delay(delay);
    }

    /// <summary>
    /// Ctrl+C (복사) 단축키
    /// </summary>
    public static async Task KeyPost_CtrlC_CopyAsync(IntPtr hWnd, int delay = 20)
    {
        StdWin32.PostMessage(hWnd, StdCommon32.WM_CHAR, 0x03, IntPtr.Zero);
        await Task.Delay(delay);
    }

    /// <summary>
    /// Ctrl+V (붙여넣기) 단축키
    /// </summary>
    public static async Task KeyPost_CtrlV_PasteAsync(IntPtr hWnd, int delay = 20)
    {
        StdWin32.PostMessage(hWnd, StdCommon32.WM_CHAR, 0x16, IntPtr.Zero);
        await Task.Delay(delay);
    }

    /// <summary>
    /// Ctrl+X (잘라내기) 단축키
    /// </summary>
    public static async Task KeyPost_CtrlX_CutAsync(IntPtr hWnd, int delay = 20)
    {
        StdWin32.PostMessage(hWnd, StdCommon32.WM_CHAR, 0x18, IntPtr.Zero);
        await Task.Delay(delay);
    }

    /// <summary>
    /// Ctrl+Z (취소) 단축키
    /// </summary>
    public static async Task KeyPost_CtrlZ_BackAsync(IntPtr hWnd, int delay = 20)
    {
        StdWin32.PostMessage(hWnd, StdCommon32.WM_CHAR, 0x1A, IntPtr.Zero);
        await Task.Delay(delay);
    }

    /// <summary>
    /// WM_CHAR로 문자열 전송 후 캡션 검증
    /// - SetFocus로 포커스 설정 후 WM_CHAR로 각 문자 전송
    /// - 캡션이 target과 일치하는지 검증
    /// </summary>
    public static async Task<bool> PostCharStringWithVerifyAsync(IntPtr hWnd, string sTarget, int nDelay = 20, int nRepeat = c_nRepeatShort)
    {
        if (hWnd == IntPtr.Zero || string.IsNullOrEmpty(sTarget)) return false;

        // 캡션 검증 (반복)
        for (int i = 0; i < nRepeat; i++)
        {
            // WM_CHAR로 문자열 전송
            bool bResult = await Std32Key_Msg.PostCharStringAsync(hWnd, sTarget, nDelay);
            if (!bResult)
            {
                await Task.Delay(c_nWaitNormal);
                continue;
            }

            string sCaption = Std32Window.GetWindowCaption(hWnd) ?? "";
            if (sCaption == sTarget) return true;

            await Task.Delay(c_nWaitNormal);
        }

        return false;
    }

    public static async Task<bool> PostFeeWithVerifyAsync(IntPtr hWnd, int nTarget, int nDelay = 20, int nRepeat = c_nRepeatShort)
    {
        if (hWnd == IntPtr.Zero) return false;

        string sTarget = nTarget.ToString();

        // 캡션 검증 (반복)
        for (int i = 0; i < nRepeat; i++)
        {
            // WM_CHAR로 문자열 전송
            bool bResult = await Std32Key_Msg.PostCharStringAsync(hWnd, sTarget, nDelay);
            if (!bResult)
            {
                await Task.Delay(c_nWaitNormal);
                continue;
            }

            string sCaption = Std32Window.GetWindowCaption(hWnd) ?? "";
            if (StdConvert.StringWonFormatToInt(sCaption) == nTarget) return true;

            await Task.Delay(c_nWaitNormal);
        }

        return false;
    }
}
