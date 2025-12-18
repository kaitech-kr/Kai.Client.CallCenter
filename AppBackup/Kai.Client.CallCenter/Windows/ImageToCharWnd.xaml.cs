using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using Draw = System.Drawing;

using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgWnd;
using Kai.Common.NetDll_WpfCtrl.NetOFR;
using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;

namespace Kai.Client.CallCenter.Windows;
#nullable disable
public partial class ImageToCharWnd : Window
{
    #region Variables
    private Draw.Bitmap _bmpSource;     // 전체 비트맵 (ImgString용)
    private Draw.Rectangle _rcChar;     // 개별 문자 영역 (ImgChar용)
    private string _failReason;         // 실패 사유

    // 출력 데이터
    public string UserInput { get; private set; } = null;
    public bool IsConfirmed { get; private set; } = false;  // 확인=true, 건너뜀/취소=false
    #endregion

    #region Basic
    public ImageToCharWnd(Draw.Bitmap bmpSource, Draw.Rectangle rcChar, string failReason = "")
    {
        InitializeComponent();

        _bmpSource = bmpSource;
        _rcChar = rcChar;
        _failReason = failReason;

        this.Topmost = true;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine("[ImageToCharWnd] Window_Loaded 시작");

        // 외부 입력 차단 해제 (OFR 처리 중 BlockInput이 활성화 상태일 수 있음)
        StdWin32.BlockInput(false);

        // 커서 명시적으로 표시
        Mouse.OverrideCursor = null;
        this.Cursor = Cursors.Arrow;

        // 이미지 표시
        DisplayBitmaps();

        // 실패 사유 표시
        if (!string.IsNullOrEmpty(_failReason))
            LabelReason.Content = $"인식 실패: {_failReason}";

        // 포커스 및 캐럿 표시
        TBoxChar.Focus();
    }
    #endregion

    #region Button Events
    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TBoxChar.Text))
        {
            MessageBox.Show("문자를 입력하세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (TBoxChar.Text.Trim().Length != 1)
        {
            MessageBox.Show("1글자만 입력하세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        UserInput = TBoxChar.Text.Trim();
        IsConfirmed = true;
        Close();
    }

    private void BtnJump_Click(object sender, RoutedEventArgs e)
    {
        // 건너뜀
        IsConfirmed = false;
        Close();
    }

    private void BtnCancelAll_Click(object sender, RoutedEventArgs e)
    {
        // 전부 취소
        IsConfirmed = false;
        Close();
    }
    #endregion

    #region Etc Events
    private void TBoxChar_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
        // 2글자 이상 입력 차단
        if (TBoxChar.Text.Length >= 1)
        {
            e.Handled = true;
        }
    }

    private void TBoxChar_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        // 저장 버튼 활성화 제어
        BtnSave.IsEnabled = !string.IsNullOrWhiteSpace(TBoxChar.Text);
    }
    #endregion

    #region Helper Functions
    private void DisplayBitmaps()
    {
        try
        {
            // 1. ImgString: 전체 비트맵 표시 (문맥 제공) + 문자 영역 표시
            if (_bmpSource != null)
            {
                // 전체 비트맵 복사 후 문자 영역에 빨간 사각형 그리기
                Draw.Bitmap bmpWithRect = new Draw.Bitmap(_bmpSource);
                if (_rcChar != Draw.Rectangle.Empty)
                {
                    using (Draw.Graphics g = Draw.Graphics.FromImage(bmpWithRect))
                    using (Draw.Pen pen = new Draw.Pen(Draw.Color.Red, 1))
                    {
                        // 문자 영역 주변에 사각형 그리기 (1픽셀 바깥쪽)
                        Draw.Rectangle rcDraw = new Draw.Rectangle(
                            Math.Max(0, _rcChar.X - 1),
                            Math.Max(0, _rcChar.Y - 1),
                            Math.Min(_rcChar.Width + 2, bmpWithRect.Width - _rcChar.X),
                            Math.Min(_rcChar.Height + 2, bmpWithRect.Height - _rcChar.Y));
                        g.DrawRectangle(pen, rcDraw);
                    }
                }

                Draw.Bitmap bmpString = ScaleBitmap(bmpWithRect, 360, 80);
                ImgString.Source = OfrService.ConvertBitmap_ToBitmapImage(bmpString);

                if (bmpString != bmpWithRect)
                    bmpString.Dispose();
                bmpWithRect.Dispose();
            }

            // 2. ImgChar: 개별 문자만 확대 표시
            if (_bmpSource != null && _rcChar != Draw.Rectangle.Empty)
            {
                Draw.Bitmap bmpChar = OfrService.GetBitmapInBitmapFast(_bmpSource, _rcChar);
                if (bmpChar != null)
                {
                    Draw.Bitmap bmpCharScaled = ScaleBitmap(bmpChar, 140, 140);
                    ImgChar.Source = OfrService.ConvertBitmap_ToBitmapImage(bmpCharScaled);

                    if (bmpCharScaled != bmpChar)
                        bmpCharScaled.Dispose();
                    bmpChar.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ImageToCharWnd] DisplayBitmaps 오류: {ex.Message}");
        }
    }

    private Draw.Bitmap ScaleBitmap(Draw.Bitmap bmp, int maxWidth, int maxHeight)
    {
        if (bmp == null) return null;

        int maxLen = Math.Max(bmp.Width, bmp.Height);
        double scaleW = (double)maxWidth / bmp.Width;
        double scaleH = (double)maxHeight / bmp.Height;
        double scale = Math.Min(scaleW, scaleH);

        // 너무 작으면 확대 (최대 3배)
        if (maxLen < 50)
        {
            scale = Math.Min(3.0, scale);
        }

        if (Math.Abs(scale - 1.0) < 0.01)
            return bmp;

        return OfrService.ConvertSizeBitmap(bmp, scale);
    }
    #endregion
}
#nullable disable
