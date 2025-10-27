using System.Windows;

using Kai.Common.StdDll_Common;
using Kai.Client.CallCenter.Networks;
using static Kai.Client.CallCenter.Classes.CommonVars;

namespace Kai.Client.CallCenter.Windows;
public partial class Config_CtrlAppWnd : Window
{
    #region Variables
    public static bool s_bLoaded = false;
    public static bool s_bAnyUse = false;

    private const string Insung01_bUse = "Insung01_bUse";
    private const string Insung02_bUse = "Insung02_bUse";
    private const string Cargo24_bUse = "Cargo24_bUse";
    private const string Onecall_bUse = "Onecall_bUse";

    private const string Insung01_sId = "Insung01_sId";
    private const string Insung02_sId = "Insung02_sId";
    private const string Cargo24_sId = "Cargo24_sId";
    private const string Onecall_sId = "Onecall_sId";

    private const string Insung01_sPw = "Insung01_sPw";
    private const string Insung02_sPw = "Insung02_sPw";
    private const string Cargo24_sPw = "Cargo24_sPw";
    private const string Onecall_sPw = "Onecall_sPw";
    #endregion

    #region Basics
    //public Config_CtrlAppWnd()
    //{
    //    InitializeComponent();
    //    this.Owner = s_MainWnd;
    //}

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // TmpHide
        //// Use
        //ChkBoxInsung01.IsChecked = NwInsung01.s_AppStatus == NwCommon_AppStatus.Use ? true : false;
        //ChkBoxInsung02.IsChecked = NwInsung02.s_AppStatus == NwCommon_AppStatus.Use ? true : false;
        //ChkBoxCargo24.IsChecked = NwCargo24.s_AppStatus == NwCommon_AppStatus.Use ? true : false;
        //ChkBoxOnecall.IsChecked = NwOnecall.s_AppStatus == NwCommon_AppStatus.Use ? true : false;

        //// ID
        //TBoxIdInsung01.Text = NwInsung01.s_sId;
        //TBoxIdInsung02.Text = NwInsung02.s_sId;
        //TBoxIdCargo24.Text = NwCargo24.s_sId;
        //TBoxIdOnecall.Text = NwOnecall.s_sId;

        //// PW
        //TBoxPwInsung01.Text = NwInsung01.s_sPw;
        //TBoxPwInsung02.Text = NwInsung02.s_sPw;
        //TBoxPwCargo24.Text = NwCargo24.s_sPw;
        //TBoxIdOnecall.Text = NwOnecall.s_sPw;
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
    }
    #endregion

    #region Events
    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        // this.Close();
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        // TmpHide
        //// Use
        //bool b = StdConvert.NullableBoolToBool(ChkBoxInsung01.IsChecked);
        //NwInsung01.s_AppStatus = b ? NwCommon_AppStatus.Use : NwCommon_AppStatus.NotUse;
        //s_KaiReg.SetValue(Insung01_bUse, b);

        //b = StdConvert.NullableBoolToBool(ChkBoxInsung02.IsChecked);
        //NwInsung02.s_AppStatus = b ? NwCommon_AppStatus.Use : NwCommon_AppStatus.NotUse;
        //s_KaiReg.SetValue(Insung02_bUse, b);

        //b = StdConvert.NullableBoolToBool(ChkBoxCargo24.IsChecked);
        //NwCargo24.s_AppStatus = b ? NwCommon_AppStatus.Use : NwCommon_AppStatus.NotUse;
        //s_KaiReg.SetValue(Cargo24_bUse, b);

        //b = StdConvert.NullableBoolToBool(ChkBoxOnecall.IsChecked);
        //NwOnecall.s_AppStatus = b ? NwCommon_AppStatus.Use : NwCommon_AppStatus.NotUse;
        //s_KaiReg.SetValue(Onecall_bUse, b);

        //// ID
        //s_KaiReg.SetValue(Insung01_sId, NwInsung01.s_sId = TBoxIdInsung01.Text);
        //s_KaiReg.SetValue(Insung02_sId, NwInsung02.s_sId = TBoxIdInsung02.Text);
        //s_KaiReg.SetValue(Cargo24_sId, NwCargo24.s_sId = TBoxIdCargo24.Text);
        //s_KaiReg.SetValue(Onecall_sId, NwOnecall.s_sId = TBoxIdOnecall.Text);

        //// PW
        //s_KaiReg.SetValue(Insung01_sPw, NwInsung01.s_sPw = TBoxPwInsung01.Text);
        //s_KaiReg.SetValue(Insung02_sPw, NwInsung02.s_sPw = TBoxPwInsung02.Text);
        //s_KaiReg.SetValue(Cargo24_sPw, NwCargo24.s_sPw = TBoxPwCargo24.Text);
        //s_KaiReg.SetValue(Onecall_sPw, NwOnecall.s_sPw = TBoxPwOnecall.Text);

        //// Set Flag - 종료하는데 뭘 굳이...
        //s_bLoaded = true;
        //s_bAnyUse = IsAnyUse();

        //this.Close();
    }
    #endregion

    #region MyMethods
    //public static void LoadRegistry()
    //{
    //    //s_AppStatus
    //    NwInsung01.s_AppStatus = s_KaiReg.GetBoolValue(Insung01_bUse) ? NwCommon_AppStatus.Use : NwCommon_AppStatus.NotUse;
    //    NwInsung02.s_AppStatus = s_KaiReg.GetBoolValue(Insung02_bUse) ? NwCommon_AppStatus.Use : NwCommon_AppStatus.NotUse;
    //    NwCargo24.s_AppStatus = s_KaiReg.GetBoolValue(Cargo24_bUse) ? NwCommon_AppStatus.Use : NwCommon_AppStatus.NotUse;
    //    NwOnecall.s_AppStatus = s_KaiReg.GetBoolValue(Onecall_bUse) ? NwCommon_AppStatus.Use : NwCommon_AppStatus.NotUse;

    //    // ID
    //    NwInsung01.s_sId = s_KaiReg.GetStringValue(Insung01_sId);
    //    NwInsung02.s_sId = s_KaiReg.GetStringValue(Insung02_sId);
    //    NwCargo24.s_sId = s_KaiReg.GetStringValue(Cargo24_sId);
    //    NwOnecall.s_sId = s_KaiReg.GetStringValue(Onecall_sId);

    //    // PW
    //    NwInsung01.s_sPw = s_KaiReg.GetStringValue(Insung01_sPw);
    //    NwInsung02.s_sPw = s_KaiReg.GetStringValue(Insung02_sPw);
    //    NwCargo24.s_sPw = s_KaiReg.GetStringValue(Cargo24_sPw);
    //    NwOnecall.s_sPw = s_KaiReg.GetStringValue(Onecall_sPw);

    //    // Set Flag
    //    s_bLoaded = true;
    //    s_bAnyUse = IsAnyUse();

    //    //MsgBox($"NwInsung01.s_AppStatus: {NwInsung01.s_AppStatus}"); // Test
    //}

    //public static bool IsAnyUse()
    //{
    //    if (NwInsung01.s_AppStatus == NwCommon_AppStatus.Use) return true;
    //    if (NwInsung02.s_AppStatus == NwCommon_AppStatus.Use) return true;
    //    if (NwCargo24.s_AppStatus == NwCommon_AppStatus.Use) return true;
    //    if (NwOnecall.s_AppStatus == NwCommon_AppStatus.Use) return true;

    //    return false;
    //}
    #endregion
}
