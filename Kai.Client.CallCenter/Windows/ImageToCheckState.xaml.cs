using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Draw = System.Drawing;
using Wnd = System.Windows;

using Kai.Client.CallCenter.OfrWorks;
using Kai.Common.NetDll_WpfCtrl.NetOFR;
using static Kai.Common.FrmDll_FormCtrl.FormFuncs;
using Kai.Server.Main.KaiWork.DBs.Postgres.CharDB.Models;

using static Kai.Client.CallCenter.Classes.CommonVars;
using Kai.Server.Main.KaiWork.DBs.Postgres.CharDB.Services;
using Kai.Common.StdDll_Common;

namespace Kai.Client.CallCenter.Windows;
#nullable disable
public partial class ImageToCheckState : Window // 고대역폭의 저장이 필요함
{
    #region Variables
    //public OfrResult_TbText ofrResult = null;
    //public Draw.Bitmap bmpCapture = null;
    #endregion

    #region Basic
    //public ImageToCheckState(string sPos, OfrResult_TbText ofrResult) // 고대역폭의 저장이 필요함
    //{
    //    InitializeComponent();

    //    this.Topmost = true;
    //    TBoxPos.Text = sPos;

    //    this.ofrResult = ofrResult;

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
    private void BtnExex_Click(object sender, RoutedEventArgs e) // 고대역폭의 저장이 필요함
    {
        // TmpHide
        //string sSave = GetSelectedComboBoxContent(CmbBoxSave);

        //MessageBoxResult result = MessageBox.Show($"[{sSave}]로 저장합니다.", "확인", MessageBoxButton.OKCancel, MessageBoxImage.Information);
        //if (result != MessageBoxResult.OK) return;

        //OfrModel_BmpTextAnalysis analyInfo = ofrResult.analyText;
        //if (analyInfo != null) // OfrModel_BitmapAnalysis정보가 있는 경우
        //{
        //    TbText tb = new TbText();

        //    tb.Text = sSave;
        //    tb.Width = analyInfo.nWidth;
        //    tb.Height = analyInfo.nHeight;
        //    tb.HexStrValue = analyInfo.sHexArray?.Trim() ?? ""; // 트림 필수
        //    tb.Threshold = analyInfo.threshold;
        //    tb.Searched = 2;
        //    tb.Reserved = "";

        //    StdResult_Long resultLong = PgService_TbText.InsertRow(tb);
        //    if (resultLong.lResult > 0)
        //    {
        //        ofrResult.tbText = tb;
        //        //MsgBox($"{resultLong.lResult}");

        //        TBoxInfo02.Text = $"KeyCode: {resultLong.lResult}\n" +
        //          $"Text: {tb.Text}\n" +
        //          $"HexStrValue: {tb.HexStrValue}\n" +
        //          $"Threshold: {tb.Threshold}\n" +
        //          $"Searched: {tb.Searched}\n" +
        //          $"Width: {tb.Width}\n" +
        //          $"Height: {tb.Height}\n" +
        //          $"Reserved: {tb.Reserved}";

        //        //Close();
        //    }
        //    else
        //    {
        //        ErrMsgBox("디비저장 실패입니다.", "ImageToCheckState/BtnExex_Click_01");
        //        return;
        //    }
        //}
        //else // 없는경우 OCR
        //{
        //    MsgBox("OfrModel_BitmapAnalysis가 없는 경우는 코딩해야 합니다.");
        //    return;
        //}
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
    #endregion
}
#nullable enable