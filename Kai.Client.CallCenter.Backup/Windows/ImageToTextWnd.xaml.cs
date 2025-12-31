using System.Windows;
using Draw = System.Drawing;
using Wnd = System.Windows;

using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgWnd;

namespace Kai.Client.CallCenter.Windows;
#nullable disable
public partial class ImageToTextWnd : Window
{
    #region Variables
    //public bool bResult = false;
    //public string sResult = null;
    //public TbText tbText = null;
    //public Draw.Bitmap bmpCapture = null;

    //public OfrResult_TbText ofrResult = null;
    #endregion

    #region Basic
    //public ImageToTextWnd(string sPos, OfrResult_TbText result)
    //{
    //    InitializeComponent();

    //    this.Topmost = true;
    //    this.TBoxPos.Text = sPos;

    //    ofrResult = result;

    //    BitmapImage bmpImage = OfrService.ConvertBitmap_ToBitmapImage(ofrResult.analyText.bmpText);
    //    ImgDisplay.Source = bmpImage;
    //}

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // TmpHide
        //OfrModel_BmpTextAnalysis analyInfo = ofrResult.analyText;

        //TBoxInfo01.Text = $"nWidth: {analyInfo.nWidth}\n" +
        //                  $"nHeight: {analyInfo.nHeight}\n" +
        //                  $"trueRate: {analyInfo.trueRate}\n" +
        //                  $"sHexArray: {analyInfo.sHexArray}";

        //TbText tbText = ofrResult.tbText;

        //if (tbText != null)
        //{
        //    TBoxInfo02.Text = $"KeyCode: {tbText.KeyCode}\n" +
        //                      $"Text: {tbText.Text}\n" +
        //                      $"HexStrValue: {tbText.HexStrValue}\n" +
        //                      $"Threshold: {tbText.Threshold}\n" +
        //                      $"Searched: {tbText.Searched}\n" +
        //                      $"Width: {tbText.Width}\n" +
        //                      $"Height: {tbText.Height}\n" +
        //                      $"Reserved: {tbText.Reserved}";
        //}

        //// OCR 영역마다 사각형 추가
        //var rect = new System.Windows.Shapes.Rectangle
        //{
        //    Width = ImgDisplay.ActualWidth,
        //    Height = ImgDisplay.ActualHeight,
        //    Stroke = Brushes.Red,
        //    StrokeThickness = 1,
        //    Fill = Brushes.Transparent
        //};

        //Canvas.SetLeft(rect, 0);
        //Canvas.SetTop(rect, 0);
        //OverlayCanvas.Children.Add(rect);
    }
    #endregion

    #region Buttons
    private void BtnExex_Click(object sender, RoutedEventArgs e)
    {
        //if (string.IsNullOrEmpty(TBoxText.Text))
        //{
        // MessageBox.Show("TEXT가 없습니다.");
        // return;
        //}

        //Close();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        // Close();
    }
    #endregion
}
#nullable enable