using System.Threading.Tasks;

using Kai.Common.StdDll_Common.StdWin32;

namespace Kai.Client.CallCenter.Classes;

/// <summary>
/// 키보드 시뮬레이션 헬퍼 클래스
/// - Modifier 키(Ctrl, Shift, Alt)와 함께 키 입력
/// </summary>
public static class Simulation_Keyboard
{
    /// <summary>
    /// Modifier 키(Ctrl, Shift, Alt) + 일반 키 조합 입력
    /// 예: Ctrl+A, Shift+Delete 등
    /// </summary>
    /// <param name="hWnd">대상 윈도우 핸들</param>
    /// <param name="vkModifier">Modifier 키 (VK_CONTROL, VK_SHIFT, VK_MENU 등)</param>
    /// <param name="vkKey">일반 키 (VK_A, VK_DELETE 등)</param>
    /// <param name="delay">키 입력 후 대기 시간 (ms)</param>
    public static async Task KeyPost_ModifierWithKeyAsync(IntPtr hWnd, uint vkModifier, uint vkKey, int delay = 30)
    {
        // 1. Modifier 키 Down
        Std32Key_Msg.KeyPost_Down(hWnd, vkModifier);
        await Task.Delay(delay);

        // 2. 일반 키 Down
        Std32Key_Msg.KeyPost_Down(hWnd, vkKey);
        await Task.Delay(delay);

        // 3. 일반 키 Up (PostMessage는 Down만 있어도 동작하지만 명시적으로)
        // Note: Std32Key_Msg.KeyPost_Up가 없으면 생략 가능

        // 4. Modifier 키 Up
        // Note: Std32Key_Msg.KeyPost_Up가 없으면 생략 가능

        await Task.Delay(delay);
    }

    /// <summary>
    /// Ctrl+A (전체 선택) 단축키
    /// </summary>
    public static async Task KeyPost_CtrlA_SelectAllAsync(IntPtr hWnd, int delay = 30)
    {
        await KeyPost_ModifierWithKeyAsync(hWnd, StdCommon32.VK_CONTROL, 0x41, delay); // 0x41 = 'A'
    }

    /// <summary>
    /// Ctrl+C (복사) 단축키
    /// </summary>
    public static async Task KeyPost_CtrlC_CopyAsync(IntPtr hWnd, int delay = 30)
    {
        await KeyPost_ModifierWithKeyAsync(hWnd, StdCommon32.VK_CONTROL, 0x43, delay); // 0x43 = 'C'
    }

    /// <summary>
    /// Ctrl+V (붙여넣기) 단축키
    /// </summary>
    public static async Task KeyPost_CtrlV_PasteAsync(IntPtr hWnd, int delay = 30)
    {
        await KeyPost_ModifierWithKeyAsync(hWnd, StdCommon32.VK_CONTROL, 0x56, delay); // 0x56 = 'V'
    }

    /// <summary>
    /// Ctrl+X (잘라내기) 단축키
    /// </summary>
    public static async Task KeyPost_CtrlX_CutAsync(IntPtr hWnd, int delay = 30)
    {
        await KeyPost_ModifierWithKeyAsync(hWnd, StdCommon32.VK_CONTROL, 0x58, delay); // 0x58 = 'X'
    }
}
