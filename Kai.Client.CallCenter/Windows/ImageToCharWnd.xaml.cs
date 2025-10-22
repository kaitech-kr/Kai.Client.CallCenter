using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Draw = System.Drawing;

using static Kai.Common.FrmDll_FormCtrl.FormFuncs;
using Kai.Common.NetDll_WpfCtrl.NetOFR;
using Kai.Common.StdDll_Common;
using Kai.Server.Main.KaiWork.DBs.Postgres.CharDB.Models;
using Kai.Server.Main.KaiWork.DBs.Postgres.CharDB.Results;

using Kai.Client.CallCenter.OfrWorks;
using static Kai.Client.CallCenter.Classes.CommonVars;


namespace Kai.Client.CallCenter.Windows;
#nullable disable
public partial class ImageToCharWnd : Window
{
    #region Variables
    //public OfrModel_BmpCharAnalysis modelAnaly = null;
    //public TbChar tbChar = null;
    #endregion

    #region Basic
    //public ImageToCharWnd(OfrModel_BmpCharAnalysis model)
    //{
    //    InitializeComponent();

    //    modelAnaly = model;

    //    Draw.Bitmap bmpTmp = modelAnaly.bmpExact;
    //    int len = OfrService.GetLongerLen(bmpTmp);
    //    double scale = (double)100 / len;
    //    if (scale > 3) scale = 3;
    //    if (scale != 1) bmpTmp = OfrService.ConvertSizeBitmap(bmpTmp, scale);

    //    ImgChar.Source = OfrService.ConvertBitmap_ToBitmapImage(bmpTmp);
    //}

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        this.Topmost = true;

        MsgBox("코딩해야 합니다", "ImageToCharWnd/Window_Loaded_01");
    }
    #endregion

    #region Button Events
    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (TBoxCharType.Text.Length != 1) return;

        MessageBoxResult response = MessageBox.Show(
            $"문자: {TBoxChar.Text}\n타입: {TBoxCharType.Text}", "확인", MessageBoxButton.YesNo, MessageBoxImage.Information);

        if (response == MessageBoxResult.No) return;

        //tbChar = 

        Close();
    }
    private void BtnJump_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BtnCancelAll_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
    #endregion

    #region Etc Events
    private void TBoxChar_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
        if (TBoxChar.Text.Length >= 2)
        {
            e.Handled = true; // 입력 차단
        }
    }

    private void TBoxChar_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (TBoxChar.Text.Length == 0)
        {
            TBoxCharType.Text = "";
            BtnSave.IsEnabled = false;
            return;
        }

        if (TBoxChar.Text.Length == 1)
        {
            MsgBox("코딩 해야함.", "TBoxChar_TextChanged_002");

            //m_tpCharIndex = StdUtil.FindIndexIn2DList(s_ListCharGroup, TBoxChar.Text);
            //if (m_tpCharIndex.Item1 != -1 && m_tpCharIndex.Item2 != -1)
            //{
            //    TBoxCharType.Text = s_sCharTypes[m_tpCharIndex.Item1];
            //    BtnSave.IsEnabled = true;
            //    return;
            //}
        }

        if (TBoxChar.Text.Length == 2)
        {
            TBoxCharType.Text = "L";
            BtnSave.IsEnabled = true;
            return;
        }
    }
    #endregion
}
#nullable disable
