using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Kai.Client.CallCenter;
public partial class App : Application
{
    [DllImport("imm32.dll")]
    private static extern IntPtr ImmGetContext(IntPtr hWnd);

    [DllImport("imm32.dll")]
    private static extern bool ImmGetConversionStatus(IntPtr hIMC, out int lpfdwConversion, out int lpfdwSentence);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    const int VK_HANGUL = 0x15;  // 한/영 키 값
    const int KEYEVENTF_KEYDOWN = 0x0000;
    const int KEYEVENTF_KEYUP = 0x0002;

    // 한글로 전환
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        EventManager.RegisterClassHandler(typeof(TextBox),
            UIElement.GotFocusEvent,
            new RoutedEventHandler(TextBox_GotFocus));
    }

    private void TextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb)
        {
            // 한국어 키보드 레이아웃 설정
            InputLanguageManager.SetInputLanguage(tb, new CultureInfo("ko-KR"));

            var source = PresentationSource.FromVisual(tb) as System.Windows.Interop.HwndSource;
            if (source != null)
            {
                IntPtr hIMC = ImmGetContext(source.Handle);

                ImmGetConversionStatus(hIMC, out int conversion, out int sentence);

                if ((conversion & 1) == 0)  // 영문 모드이면
                {
                    // 한/영 키 누르기 (IME 전환)
                    keybd_event(VK_HANGUL, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                    keybd_event(VK_HANGUL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                }
            }
        }
    }
}

