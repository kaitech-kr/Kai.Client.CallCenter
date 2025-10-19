using System.Windows;
using System.Windows.Controls;

using Kai.Common.StdDll_Common;
using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgBox;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Results;

using Kai.Client.CallCenter.Classes;
using Kai.Client.CallCenter.MVVM.ViewModels;
using Kai.Client.CallCenter.MVVM.ViewServices;
using static Kai.Client.CallCenter.Class_Common.CommonVars;

namespace Kai.Client.CallCenter.Pages;
#nullable disable
public partial class Company_RegistPage : Page
{
    #region Variables
    private List<TbCompany> curListCompany = null; 
    #endregion

    #region Basic
    public Company_RegistPage()
    {
        InitializeComponent();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {

    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {

    }
    #endregion

    #region Click - BigButtons
    // 닫기
    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        // TmpHide
        //if (s_MainWnd != null)
        //{
        //    s_MainWnd.RemoveTab(s_MainWnd.Company_CompRegistTab);
        //}
    }

    // 저장
    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        // TmpHide
        //if (string.IsNullOrEmpty(TBoxWrite_CompName.Text))
        //{
        //    ErrMsgBox("거래처명이 없읍니다.");
        //    return;
        //}
        //if (string.IsNullOrEmpty(TBoxWrite_CEOName.Text))
        //{
        //    ErrMsgBox("대표자명이 없읍니다.");
        //    return;
        //}
        //if (string.IsNullOrEmpty(TBoxWrite_TelNo.Text))
        //{
        //    ErrMsgBox("전화번호가 없읍니다.");
        //    return;
        //}

        //if (DGridCompany.SelectedIndex >= 0) // Update Mode
        //{
        //    VmCompany_RegistPage_Comp comp = VsCompany_RegistPage.oc_VmCompany_RegistPage_Comp[DGridCompany.SelectedIndex];
        //    if (!IsDataChanged(comp))
        //    {
        //        ErrMsgBox("변경된 데이타가 없읍니다.");
        //        return;
        //    }

        //    TbCompany tbOld = comp.TbCompany;
        //    TbCompany tbNew = new TbCompany();

        //    // Not Changable
        //    tbNew.KeyCode = tbOld.KeyCode;
        //    tbNew.CenterCode = tbOld.CenterCode;
        //    tbNew.DtRegist = tbOld.DtRegist;
        //    tbNew.DtUpdate = DateTime.Now;
        //    tbNew.Etc1 = tbOld.Etc2;
        //    tbNew.Etc2 = tbOld.Etc2;
        //    tbNew.BeforeBelong = tbOld.BeforeBelong;
        //    tbNew.Using = tbOld.Using;

        //    // From UI
        //    tbNew.CompName = TBoxWrite_CompName.Text;
        //    tbNew.TelNo = StdConvert.MakePhoneNumberToDigit(TBoxWrite_TelNo.Text);
        //    tbNew.FaxNo = StdConvert.MakePhoneNumberToDigit(TBoxWrite_FaxNo.Text);
        //    tbNew.Owner = TBoxWrite_CEOName.Text;
        //    tbNew.ChargeName = TBoxWrite_ChargeNmae.Text;
        //    tbNew.Lon = 0; // ...........................................
        //    tbNew.Lat = 0; // ...........................................
        //    tbNew.DiscountType = GetSelectedComboBoxContent(CmbBoxWrite_DiscountType);
        //    tbNew.TradeType = GetSelectedComboBoxContent(CmbBoxWrite_TradeType);
        //    tbNew.Register = s_CenterCharge.Id;
        //    tbNew.Memo = TBoxWrite_Memo.Text;

        //    StdResult_Int result = await s_SrGClient.SrResult_Company_UpdateRowAsync(tbNew);
        //    if (result.nResult < 0)
        //    {
        //        ErrMsgBox($"거래처({tbNew.CompName}) 수정실패: {result.sErr}");
        //        return;
        //    }

        //    int index = curListCompany.FindIndex(x => x.KeyCode == tbNew.KeyCode);
        //    if (index < 0) return;

        //    curListCompany[index] = tbNew;
        //    VsCompany_RegistPage.LoadData_Comp(s_MainWnd, curListCompany);
        //    DGridCompany.SelectedIndex = VsCompany_RegistPage.oc_VmCompany_RegistPage_Comp.ToList().FindIndex(x => x.KeyCode == tbNew.KeyCode);
        //    DGridCompany.Focus();
        //}
        //else // Regist Mode
        //{
        //    TbCompany tbNew = new TbCompany();

        //    // Basic
        //    tbNew.KeyCode = 0;
        //    tbNew.CenterCode = s_CenterCharge.CenterCode;
        //    tbNew.DtRegist = DateTime.Now;
        //    tbNew.DtUpdate = DateTime.Now;
        //    tbNew.Etc1 = "";
        //    tbNew.Etc2 = "";
        //    tbNew.BeforeBelong = "";
        //    tbNew.Using = true;

        //    // From UI
        //    tbNew.CompName = TBoxWrite_CompName.Text;
        //    tbNew.TelNo = StdConvert.MakePhoneNumberToDigit(TBoxWrite_TelNo.Text);
        //    tbNew.FaxNo = StdConvert.MakePhoneNumberToDigit(TBoxWrite_FaxNo.Text);
        //    tbNew.Owner = TBoxWrite_CEOName.Text;
        //    tbNew.ChargeName = TBoxWrite_ChargeNmae.Text;
        //    tbNew.Lon = 0; // ...........................................
        //    tbNew.Lat = 0; // ...........................................
        //    tbNew.DiscountType = GetSelectedComboBoxContent(CmbBoxWrite_DiscountType);
        //    tbNew.TradeType = GetSelectedComboBoxContent(CmbBoxWrite_TradeType);
        //    tbNew.Register = s_CenterCharge.Id;
        //    tbNew.Memo = TBoxWrite_Memo.Text;

        //    StdResult_Long result = await s_SrGClient.SrResult_Company_InsertRowAsync(tbNew);
        //    if (result.lResult <= 0)
        //    {
        //        ErrMsgBox($"거래처({tbNew.CompName}) 등록실패: {result.sErr}");
        //        return;
        //    }
        //    tbNew.KeyCode = result.lResult;

        //    curListCompany.Add(tbNew);
        //    VsCompany_RegistPage.LoadData_Comp(s_MainWnd, curListCompany);
        //    DGridCompany.SelectedIndex = VsCompany_RegistPage.oc_VmCompany_RegistPage_Comp.ToList().FindIndex(x => x.KeyCode == tbNew.KeyCode);
        //    DGridCompany.Focus();
        //}
    }

    // 삭제
    private async void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        // TmpHide
        //if (BtnDelete.Opacity < 1) return;
        //if (DGridCompany.SelectedIndex < 0) return;

        //string compName = VsCompany_RegistPage.oc_VmCompany_RegistPage_Comp[DGridCompany.SelectedIndex].CompName;
        //MessageBoxResult resultMsg = MessageBox.Show($"거래처({compName})를 삭제하시겠읍니까?", "확인", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
        //if (resultMsg != MessageBoxResult.OK) return;

        //long keyCode = VsCompany_RegistPage.oc_VmCompany_RegistPage_Comp[DGridCompany.SelectedIndex].KeyCode;
        //StdResult_Bool result = await s_SrGClient.SrResult_Company_DeleteRowAsync_KeyCode(keyCode);
        //if (!result.bResult)
        //{
        //    ErrMsgBox($"거래처({compName}) 삭제실패: {result.sErr}");
        //    return;
        //}

        //curListCompany.RemoveAll(x => x.KeyCode == keyCode);
        //VsCompany_RegistPage.LoadData_Comp(s_MainWnd, curListCompany);
        //DGridCompany.SelectedIndex = -1;
        //Grid_Right_Upper.IsEnabled = false;
    }

    // 신규
    private void BtnNewCust_Click(object sender, RoutedEventArgs e)
    {
        DGridCompany.SelectedIndex = -1;
        Grid_Right_Upper.IsEnabled = true;
        BtnSave.Opacity = (double)Application.Current.FindResource("AppOpacity_Enabled");
    }

    // 검색
    private async void BtnSearch_Click(object sender, RoutedEventArgs e)
    {
        // TmpHide
        //PostgResult_TbCompanyList result = null;
        //bool? bUsing = GetUsingType();
        //string sTradeType = GetTradeType();

        //if (bUsing == null && string.IsNullOrEmpty(sTradeType) && string.IsNullOrEmpty(TBoxSearch_CompName.Text))
        //    result = await s_SrGClient.SrResult_Company_SelectRowsAsync_CenterCode();
        //else
        //    result = await s_SrGClient.
        //        SrResult_Company_SelectRowsAsync_CenterCode_CompName_TradType_Using(TBoxSearch_CompName.Text, sTradeType, bUsing);

        //curListCompany = result.listTb;

        //VsCompany_RegistPage.LoadData_Comp(s_MainWnd, curListCompany);
        ////MsgBox($"result={result.listTb.Count}"); // Test
    }

    // 엑셀
    private void BtnExcel_Click(object sender, RoutedEventArgs e)
    {

    }
    #endregion

    #region Datagrid Events
    // CustBelong
    private void DGridCustBelong_LoadingRow(object sender, DataGridRowEventArgs e)
    {
        e.Row.Header = (e.Row.GetIndex() + 1).ToString(); // 행 번호 설정
    }

    // Company
    private void DGridCompany_LoadingRow(object sender, DataGridRowEventArgs e)
    {
        e.Row.Header = (e.Row.GetIndex() + 1).ToString(); // 행 번호 설정
    }
    private async void DGridCompany_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // TmpHide
        //if (DGridCompany.SelectedIndex < 0) // UI 청소
        //{
        //    ClearUI();
        //    BtnSave.Opacity = (double)Application.Current.FindResource("AppOpacity_Disabled");
        //    BtnDelete.Opacity = (double)Application.Current.FindResource("AppOpacity_Disabled");
        //    //Grid_Right_Upper.IsEnabled = false;
        //}
        //else
        //{
        //    VmCompany_RegistPage_Comp comp = VsCompany_RegistPage.oc_VmCompany_RegistPage_Comp[DGridCompany.SelectedIndex];
        //    long lCompCode = ClassToUI(comp);
        //    PostgResult_TbCustMainList result = await s_SrGClient.SrResult_CustMain_SelectRowsAsync_CenterCode_CompCode(lCompCode);

        //    VsCompany_RegistPage.LoadData_Cust(s_MainWnd, result.listTb);
        //    BtnSave.Opacity = (double)Application.Current.FindResource("AppOpacity_Enabled");
        //    BtnDelete.Opacity = (double)Application.Current.FindResource("AppOpacity_Enabled");
        //    Grid_Right_Upper.IsEnabled = true;
        //    //MsgBox($"result={result.listTb.Count}");
        //}
    }
    #endregion

    private void RdoBtnTotCust_Checked(object sender, RoutedEventArgs e)
    {

    }

    private void RdoBtnUseCust_Checked(object sender, RoutedEventArgs e)
    {

    }

    private void RdoBtnNotUseCust_Checked(object sender, RoutedEventArgs e)
    {

    }

    private void ChBoxWrite_AllChange_Click(object sender, RoutedEventArgs e)
    {

    }

    private void ChkBoxWrite_ShowDrvTotFee_Click(object sender, RoutedEventArgs e)
    {

    }

    #region 1차 Funcs
    private void EnableImageBtn(Button btn, bool enable)
    {
        if (enable)
        {
            btn.Style = (Style)FindResource("EnabledBigImageBtnStyle");
        }
        else   
        {
            btn.Style = (Style)FindResource("DisabledBigImageBtnStyle");
        }
    }

    //private bool? GetUsingType()
    //{
    //    if (StdConvert.NullableBoolToBool(RdoBtnSearch_UseOnly.IsChecked)) return true;
    //    else if (StdConvert.NullableBoolToBool(RdoBtnSearch_NotUse.IsChecked)) return false;

    //    return null;
    //}

    //private string GetTradeType()
    //{
    //    string result = GetSelectedComboBoxContent(CmbBoxSearch_TradeType);
    //    if (result == "전체") return "";
    //    else return result;
    //}

    //private long ClassToUI(VmCompany_RegistPage_Comp comp)
    //{
    //    TBoxWrite_CompName.Text = comp.CompName;
    //    TBoxWrite_CEOName.Text = comp.Owner;
    //    TBoxWrite_ChargeNmae.Text = comp.TbCompany.ChargeName;
    //    TBoxWrite_TelNo.Text = comp.TelNo;
    //    TBoxWrite_FaxNo.Text = StdConvert.ToPhoneNumberFormat(comp.TbCompany.FaxNo);
    //    //TBoxWrite_BusinessNo.Text = comp.TbCompany.BusinessNo;
    //    CmbBoxWrite_DiscountType.SelectedIndex = 2; // .......................
    //    //TBoxWrite_DiscountBasic.Text = comp.TbCompany.DiscountBasic.ToString();
    //    //TBoxWrite_DiscountPer.Text = comp.TbCompany.DiscountPer.ToString();
    //    CmbBoxWrite_WonOrRate.SelectedIndex = 0; // .............................
    //    CmbBoxWrite_TradeType.SelectedIndex = 0; // .............................
    //    //TBoxWrite_업태.Text = comp.TbCompany.업태;
    //    //TBoxWrite_업종.Text = comp.TbCompany.업종;
    //    //TBoxWrite_DiscountSum.Text = comp.TbCompany.DiscountSum.ToString();
    //    //TBoxWrite_Address.Text = comp.TbCompany.Address;
    //    //TBoxWrite_DongBasic.Text = comp.TbCompany.DongBasic;
    //    //TBoxWrite_DetailAddr.Text = comp.TbCompany.DetailAddr;
    //    TBoxWrite_Memo.Text = comp.TbCompany.Memo;
    //    //TBoxWrite_Remarks.Text = comp.Remarks;
    //    //TBoxWrite_StartPlace.Text = comp.TbCompany.BasicStartPlace;
    //    CmbBoxWrite_CustomerType.SelectedIndex = 0; // ................................
    //    //TBoxWrite_CreditLimit.Text = comp.TbCompany.CreditLimit;
    //    CmbBoxWrite_ResetDay.SelectedIndex = 0; // ..................................
    //    TBoxWrite_RegDate.Text = comp.DtRegist;
    //    TBoxWrite_Register.Text = comp.TbCompany.Register;

    //    return comp.KeyCode;
    //}
    //private void ClearUI()
    //{
    //    TBoxWrite_CompName.Text = "";
    //    TBoxWrite_CEOName.Text = "";
    //    TBoxWrite_ChargeNmae.Text = "";
    //    TBoxWrite_TelNo.Text = "";
    //    TBoxWrite_FaxNo.Text = "";
    //    TBoxWrite_BusinessNo.Text = "";
    //    CmbBoxWrite_DiscountType.SelectedIndex = 2;
    //    TBoxWrite_DiscountBasic.Text = "";
    //    TBoxWrite_DiscountPer.Text = "";
    //    CmbBoxWrite_WonOrRate.SelectedIndex = 0;
    //    CmbBoxWrite_TradeType.SelectedIndex = 0;
    //    TBoxWrite_업태.Text = "";
    //    TBoxWrite_업종.Text = "";
    //    TBoxWrite_DiscountSum.Text = "";
    //    TBoxWrite_Address.Text = "";
    //    TBoxWrite_DongBasic.Text = "";
    //    TBoxWrite_DetailAddr.Text = "";
    //    TBoxWrite_Memo.Text = "";
    //    TBoxWrite_Remarks.Text = "";
    //    TBoxWrite_StartPlace.Text = "";
    //    CmbBoxWrite_CustomerType.SelectedIndex = 0; 
    //    TBoxWrite_CreditLimit.Text = "";
    //    CmbBoxWrite_ResetDay.SelectedIndex = 0; 
    //    TBoxWrite_RegDate.Text = "";
    //    TBoxWrite_Register.Text = "";
    //}
    //private bool IsDataChanged(VmCompany_RegistPage_Comp comp)
    //{
    //    if(TBoxWrite_CompName.Text != comp.CompName) return true;
    //    if (TBoxWrite_CEOName.Text != comp.Owner) return true;
    //    if (TBoxWrite_ChargeNmae.Text != comp.TbCompany.ChargeName) return true;
    //    if (TBoxWrite_TelNo.Text != comp.TelNo) return true;
    //    if (TBoxWrite_FaxNo.Text != StdConvert.ToPhoneNumberFormat(comp.TbCompany.FaxNo)) return true;
    //    //if(TBoxWrite_BusinessNo.Text != comp.TbCompany.BusinessNo) return true;
    //    if (CmbBoxWrite_DiscountType.SelectedIndex != 2) return true; // .......................
    //    //if(TBoxWrite_DiscountBasic.Text != comp.TbCompany.DiscountBasic.ToString()) return true;
    //    //if(TBoxWrite_DiscountPer.Text != comp.TbCompany.DiscountPer.ToString()) return true;
    //    if (CmbBoxWrite_WonOrRate.SelectedIndex != 0) return true; // .............................
    //    if (CmbBoxWrite_TradeType.SelectedIndex != 0) return true; // .............................
    //    //if(TBoxWrite_업태.Text != comp.TbCompany.업태) return true;
    //    //if(TBoxWrite_업종.Text != comp.TbCompany.업종) return true;
    //    //if(TBoxWrite_DiscountSum.Text != comp.TbCompany.DiscountSum.ToString()) return true;
    //    //if(TBoxWrite_Address.Text != comp.TbCompany.Address) return true;
    //    //if(TBoxWrite_DongBasic.Text != comp.TbCompany.DongBasic) return true;
    //    //if(TBoxWrite_DetailAddr.Text = comp.TbCompany.DetailAddr) return true;
    //    if (TBoxWrite_Memo.Text != comp.TbCompany.Memo) return true;
    //    //if(TBoxWrite_Remarks.Text != comp.Remarks) return true;
    //    //if(TBoxWrite_StartPlace.Text != comp.TbCompany.BasicStartPlace) return true;
    //    if (CmbBoxWrite_CustomerType.SelectedIndex != 0) return true; // ................................
    //    //if(TBoxWrite_CreditLimit.Text != comp.TbCompany.CreditLimit) return true;
    //    if (CmbBoxWrite_ResetDay.SelectedIndex != 0) return true; // ..................................
    //    if (TBoxWrite_RegDate.Text != comp.DtRegist) return true;
    //    if (TBoxWrite_Register.Text != comp.TbCompany.Register) return true;

    //    return false;
    //}
    #endregion


}
#nullable enable
