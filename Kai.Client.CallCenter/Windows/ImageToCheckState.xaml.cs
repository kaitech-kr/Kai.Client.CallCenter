using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Draw = System.Drawing;
using Wnd = System.Windows;

using Kai.Client.CallCenter.OfrWorks;
using Kai.Common.NetDll_WpfCtrl.NetOFR;
using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgWnd;
using Kai.Server.Main.KaiWork.DBs.Postgres.CharDB.Models;

using static Kai.Client.CallCenter.Classes.CommonFuncs;
using Kai.Server.Main.KaiWork.DBs.Postgres.CharDB.Services;
using Kai.Common.StdDll_Common;

namespace Kai.Client.CallCenter.Windows;
#nullable disable
public partial class ImageToCheckState : Window // 고대역폭의 저장이 필요함
{
    #region Variables
    public OfrResult_TbText ofrResult = null;
    #endregion

    #region Basic
    public ImageToCheckState(string sPos, OfrResult_TbText ofrResult)
    {
        InitializeComponent();

        this.Topmost = true;
        TBoxPos.Text = sPos;

        this.ofrResult = ofrResult;

        Debug.WriteLine($"[ImageToCheckState] ofrResult={ofrResult != null}, analyText={ofrResult?.analyText != null}, bmpExact={ofrResult?.analyText?.bmpExact != null}");

        if (ofrResult?.analyText?.bmpExact != null)
        {
            Debug.WriteLine($"[ImageToCheckState] bmpExact Size: {ofrResult.analyText.bmpExact.Width}x{ofrResult.analyText.bmpExact.Height}");
            BitmapImage bmpImage = OfrService.ConvertBitmap_ToBitmapImage(ofrResult.analyText.bmpExact);
            ImgDisplay.Source = bmpImage;
        }
        else
        {
            Debug.WriteLine($"[ImageToCheckState] ERROR: bmpExact is null!");
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        OfrModel_BitmapAnalysis analyInfo = ofrResult?.analyText;

        if (analyInfo != null)
        {
            TBoxInfo01.Text = $"nWidth: {analyInfo.nWidth}\n" +
                              $"nHeight: {analyInfo.nHeight}\n" +
                              $"trueRate: {analyInfo.trueRate}\n" +
                              $"threshold: {analyInfo.threshold}\n" +
                              $"sHexArray: {analyInfo.sHexArray}";
        }

        TbText tbText = ofrResult?.tbText;

        if (tbText != null)
        {
            TBoxInfo02.Text = $"KeyCode: {tbText.KeyCode}\n" +
                              $"Text: {tbText.Text}\n" +
                              $"HexStrValue: {tbText.HexStrValue}\n" +
                              $"Searched: {tbText.Searched}\n" +
                              $"Width: {tbText.Width}\n" +
                              $"Height: {tbText.Height}\n" +
                              $"Reserved: {tbText.Reserved}";
        }

        // OCR 영역마다 사각형 추가
        var rect = new System.Windows.Shapes.Rectangle
        {
            Width = ImgDisplay.ActualWidth,
            Height = ImgDisplay.ActualHeight,
            Stroke = Brushes.Red,
            StrokeThickness = 1,
            Fill = Brushes.Transparent
        };

        Canvas.SetLeft(rect, 0);
        Canvas.SetTop(rect, 0);
        OverlayCanvas.Children.Add(rect);
    }
    #endregion

    #region Buttons
    private async void BtnExex_Click(object sender, RoutedEventArgs e)
    {
        string sSave = GetSelectedComboBoxContent(CmbBoxSave);

        MessageBoxResult result = MessageBox.Show($"[{sSave}]로 저장합니다.", "확인", MessageBoxButton.OKCancel, MessageBoxImage.Information);
        if (result != MessageBoxResult.OK) return;

        OfrModel_BitmapAnalysis analyInfo = ofrResult?.analyText;
        if (analyInfo != null) // OfrModel_BitmapAnalysis 정보가 있는 경우
        {
            TbText tb = new TbText();

            tb.Text = sSave;
            tb.Width = analyInfo.nWidth;
            tb.Height = analyInfo.nHeight;
            tb.HexStrValue = analyInfo.sHexArray?.Trim() ?? ""; // 트림 필수
            tb.Threshold = analyInfo.threshold;
            tb.Searched = 2;
            tb.Reserved = "";

            StdResult_Long resultLong = await PgService_TbText.InsertRowAsync(tb);
            if (resultLong.lResult > 0)
            {
                tb.KeyCode = resultLong.lResult;
                ofrResult.tbText = tb;

                TBoxInfo02.Text = $"KeyCode: {tb.KeyCode}\n" +
                                  $"Text: {tb.Text}\n" +
                                  $"HexStrValue: {tb.HexStrValue}\n" +
                                  $"Threshold: {tb.Threshold}\n" +
                                  $"Searched: {tb.Searched}\n" +
                                  $"Width: {tb.Width}\n" +
                                  $"Height: {tb.Height}\n" +
                                  $"Reserved: {tb.Reserved}";

                //MessageBox.Show($"DB 저장 성공: KeyCode={tb.KeyCode}", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            else
            {
                MessageBox.Show("디비저장 실패입니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }
        else
        {
            MessageBox.Show("OfrModel_BitmapAnalysis가 없는 경우는 코딩해야 합니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
    #endregion
}
#nullable enable