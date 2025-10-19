using System.Windows;
using System.Diagnostics;
using System.Windows.Controls;
using System.Text.RegularExpressions;

using Kai.Common.FrmDll_FormCtrl;
using Kai.Common.StdDll_Common;
using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgBox;

using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Results;

using Kai.Client.CallCenter.Classes;
using Kai.Client.CallCenter.MVVM.ViewModels;
using Kai.Client.CallCenter.MVVM.ViewServices;
using static Kai.Client.CallCenter.Class_Common.CommonVars;

namespace Kai.Client.CallCenter.Windows;
#nullable disable
public partial class CustMain_RegEditWnd : Window
{
    #region Variables
    private TbAllWith tbAllWithOrg = null;
    public TbAllWith tbAllWithNew = null;
    private long lCallCustKeyOrg = 0;
    #endregion

    #region Basics
    public CustMain_RegEditWnd(TbAllWith tbAllWith = null)
    {
        InitializeComponent();

        tbAllWithOrg = tbAllWith;
    }
    public CustMain_RegEditWnd(long lCustKey)
    {
        InitializeComponent();

        lCallCustKeyOrg = lCustKey;
    }
    public CustMain_RegEditWnd(string sCustName)
    {
        InitializeComponent();

        TBoxCustName.Text = sCustName;
    }
    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (this.DialogResult == null)
        {
            this.DialogResult = false;
        }

        base.OnClosing(e);
    }


    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // TmpHide
        //// 공용
        //WindowStartupLocation = WindowStartupLocation.CenterOwner;
        //TBoxRegister.Text = s_CenterCharge.Id;

        //// 모드에 따라
        //if (tbAllWithOrg == null) // 신규 등록모드
        //{
        //    if (lCallCustKeyOrg > 0 && tbAllWithOrg == null) // DB에서 고객정보를 가져옴
        //    {
        //        PostgResult_TbCustMain result = await s_SrGClient.SrResult_CustMain_SelectRowAsync_CenterCode_KeyCode(lCallCustKeyOrg);
        //        if (result == null || result.tb == null)
        //        {
        //            FormFuncs.ErrMsgBox($"고객정보를 찾을 수 없습니다. 고객키: {lCallCustKeyOrg}, 센터코드: {s_CenterCharge.CenterCode}");
        //            this.Close();
        //            return;
        //        }
        //    }
        //}

        //else // 수정모드
        //{
        //    if (lCallCustKeyOrg != 0) // 테이블은 없지만 키가 있으니 디비검색
        //    {
        //        if (tbAllWithOrg == null)
        //        {
        //            // DB에서 고객정보를 가져옴
        //            PostgResult_AllWith result = await s_SrGClient.SrResult_CustMainWith_Cust_Center_Comp_SelectRowAsync_CenterCode_KeyCode(lCallCustKeyOrg);
        //            if (result == null || result.tbAll == null)
        //            {
        //                FormFuncs.ErrMsgBox($"고객정보를 찾을 수 없습니다. 고객키: {lCallCustKeyOrg}, 센터코드: {s_CenterCharge.CenterCode}");
        //                this.Close();
        //                return;
        //            }

        //            tbAllWithOrg = result.tbAll;

        //            MsgBox($"{tbAllWithOrg.custMain.CustName}");
        //        }
        //    }
        //    //else if (m_sBeforeBelong != "")
        //    //{
        //    //    PostgResult_TbCustMain result = await s_SrGClient.SrResult_CustMain_SelectRowAsync_CenterCode_BefBelong_BefKey(m_sBeforeBelong, m_lExtCustKey);
        //    //    if (result.tb == null)
        //    //    {
        //    //        FormFuncs.ErrMsgBox($"고객정보를 찾을 수 없습니다. 주문에서 호출된 고객키: {m_lExtCustKey}, 고객From: {m_sBeforeBelong}, {result.sErr}");
        //    //        this.Close();
        //    //        return;
        //    //    }

        //    //    m_TbCustMainBK = result.tb;
        //    //    m_lCustKey = m_TbCustMainBK.KeyCode;
        //    //}

        //    // 테이블로 부터 
        //    OrgTableToUiData();
        //}
    }

    private void Window_Unloaded(object sender, RoutedEventArgs e)
    {
    }
    #endregion

    #region Button Events
    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        // TmpHide
        //#region 필수입력 체크
        //if (string.IsNullOrEmpty(TBoxCustName.Text))
        //{
        //    ErrMsgBox("고객명을 입력하세요.");
        //    return;
        //}
        //if (string.IsNullOrEmpty(TBoxTelNo1.Text))
        //{
        //    ErrMsgBox("전화번호를 입력하세요.");
        //    return;
        //}
        //if (string.IsNullOrEmpty(TBoxDongBasic.Text))
        //{
        //    ErrMsgBox("동명을 입력하세요.");
        //    return;
        //}
        //#endregion

        //// 모드에 따라
        //if (tbAllWithOrg == null) // 신규등록
        //{
        //    CreateEmptyNewTable();
        //    UpdateNewTableByUi();

        //    // DB에 저장
        //    StdResult_Long result = await s_SrGClient.SrResult_CustMain_InsertRowAsync(tbAllWithNew.custMain);
        //    if (!string.IsNullOrEmpty(result.sErr))
        //    {
        //        ErrMsgBox($"신규저장 에러발생: {result.sErr}");
        //        return;
        //    }
        //    if (result.lResult <= 0)
        //    {
        //        ErrMsgBox($"신규저장에 실패했습니다.");
        //        return;
        //    }
        //    tbAllWithNew.custMain.KeyCode = result.lResult;
        //    MsgBox($"{tbAllWithNew.custMain.KeyCode}"); // Test
        //}
        //else // 수정
        //{
        //    CreateNewTableCopyFromOrg(); // Backup Table에서 복사해서
        //    UpdateNewTableByUi(); // UI데이터를 NewTable로

        //    if (!IsCanUpdateTable()) // 변경된 데이터가 있나 체크
        //    {
        //        ErrMsgBox($"수정한 데이타가 없습니다.");
        //        return;
        //    }

        //    // DB에 저장
        //    StdResult_Int result = await s_SrGClient.SrResult_CustMain_UpdateRowAsync(tbAllWithNew.custMain);
        //    if (!string.IsNullOrEmpty(result.sErr))
        //    {
        //        ErrMsgBox($"에러발생: {result.sErr}");
        //        return;
        //    }
        //    if (result.nResult == 0)
        //    {
        //        ErrMsgBox($"수정에 실패했습니다.");
        //        return;
        //    }
        //}

        //// 성공
        //this.DialogResult = true;
        //this.Close();
    }

    private void BtnToExcel_Click(object sender, RoutedEventArgs e)
    {

    }
    #endregion

    #region TextBox Events
    // 공용 - TBoxOnlyNum
    private void TBoxOnlyNum_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
        // TmpHide
        //// 정규식: 숫자만 허용
        //e.Handled = s_RegexOnlyNum.IsMatch(e.Text);  // true면 입력 차단
    }

    // TextChanged - Basic
    private void TBoxCustName_TextChanged(object sender, TextChangedEventArgs e)
    {
        IsCanBasicSave();
    }

    private void TBoxTelNo1_TextChanged(object sender, TextChangedEventArgs e)
    {
        IsCanBasicSave();
    }

    private void TBoxDongBasic_TextChanged(object sender, TextChangedEventArgs e)
    {
        IsCanBasicSave();
    }

    //private void TBoxOnlyNum_Pasting(object sender, DataObjectPastingEventArgs e)
    //{
    //    if (e.DataObject.GetDataPresent(DataFormats.Text))
    //    {
    //        string text = e.DataObject.GetData(DataFormats.Text) as string;
    //        if (!Regex.IsMatch(text, "^[0-9]*$")) // 숫자만 허용
    //        {
    //            e.CancelCommand();
    //        } 
    //    }
    //    else
    //    {
    //        e.CancelCommand();
    //    }
    //}
    #endregion

    #region Datagrid Events
    private void DGridOrder_LoadingRow(object sender, DataGridRowEventArgs e)
    {
        e.Row.Header = (e.Row.GetIndex() + 1).ToString(); // 행 번호 설정
    } 
    #endregion

    #region Funcs
    private string GetTradeType()
    {
        if (Rdo선불.IsChecked == true) return "선불";
        if (Rdo신용.IsChecked == true) return "신용";
        if (Rdo카드.IsChecked == true) return "카드";
        if (Rdo착불.IsChecked == true) return "착불";
        if (Rdo송금.IsChecked == true) return "송금";
        if (Rdo수금.IsChecked == true) return "수금";

        return "";
    }
    private void SetTradeType(string sTrage)
    {
        Rdo선불.IsChecked = false;
        Rdo신용.IsChecked = false;
        Rdo카드.IsChecked = false;
        Rdo착불.IsChecked = false;
        Rdo송금.IsChecked = false;
        Rdo수금.IsChecked = false;

        switch (sTrage)
        {
            case "선불":
                Rdo선불.IsChecked = true;
                break;
            case "신용":
                Rdo신용.IsChecked = true;
                break;
            case "카드":
                Rdo카드.IsChecked = true;
                break;
            case "착불":
                Rdo착불.IsChecked = true;
                break;
            case "송금":
                Rdo송금.IsChecked = true;
                break;
            case "수금":
                Rdo수금.IsChecked = true;
                break;
        }
    }

    private string GetDiscountType()
    {
        if (Rdo할인.IsChecked == true) return "할인";
        if (Rdo마일리지.IsChecked == true) return "마일리지";

        return "";
    }
    private void SetDiscountType(string sDiscount)
    {
        Rdo할인.IsChecked = false;
        Rdo마일리지.IsChecked = false;

        switch (sDiscount)
        {
            case "할인":
                Rdo할인.IsChecked = true;
                break;
            case "마일리지":
                Rdo마일리지.IsChecked = true;
                break;
        }
    }

    private bool IsCanBasicSave()
    {
        if (!string.IsNullOrEmpty(TBoxCustName.Text) && !string.IsNullOrEmpty(TBoxTelNo1.Text) && !string.IsNullOrEmpty(TBoxDongBasic.Text))
        {
            BtnSave.Opacity = (double)Application.Current.FindResource("AppOpacity_Enabled");
            return true;
        }
        else
        {
            BtnSave.Opacity = (double)Application.Current.FindResource("AppOpacity_Disabled");
            return false;
        }
    }
    //private void CreateEmptyNewTable()
    //{
    //    tbAllWithNew = new TbAllWith();

    //    TbCustMain tbCustMainNew = tbAllWithNew.custMain = new TbCustMain();

    //    // UI Data 빼고 기본정보 채우기
    //    tbCustMainNew.KeyCode = 0;
    //    tbCustMainNew.MemberCode = s_CenterCharge.MemberCode;
    //    tbCustMainNew.CenterCode = s_CenterCharge.CenterCode;
    //    tbCustMainNew.CompCode = 0;
    //    //tbCustMainNew.CustName = "";
    //    //tbCustMainNew.TelNo1 = "";
    //    //tbCustMainNew.TelNo2 = "";
    //    //tbCustMainNew.DongBasic = "";
    //    //tbCustMainNew.DongAddr = "";
    //    //tbCustMainNew.DetailAddr = "";
    //    //tbCustMainNew.DeptName = "";
    //    //tbCustMainNew.ChargeName = "";
    //    //tbCustMainNew.TradeType = "";
    //    //tbCustMainNew.Remarks = "";
    //    //tbCustMainNew.Memo = "";
    //    //tbCustMainNew.Register = "";
    //    //tbCustMainNew.RegDate = ; // Insert시 DB에서 처리.
    //    //tbCustMainNew.EditDate = ; // 신규시 자료 없슴
    //    //tbCustMainNew.Lon = 0;
    //    //tbCustMainNew.Lat = 0;
    //    //tbCustMainNew.SiDoName = "";
    //    //tbCustMainNew.GunGuName = "";
    //    //tbCustMainNew.DongRiName = "";
    //    //tbCustMainNew.DiscountType = "";
    //    //tbCustMainNew.FaxNo = "";
    //    //tbCustMainNew.Email = "";
    //    //tbCustMainNew.CustId = "";
    //    //tbCustMainNew.CustPw = "";
    //    //tbCustMainNew.Etc01 = "";
    //    //tbCustMainNew.Etc02 = "";
    //    tbCustMainNew.HappyCall = false;
    //    tbCustMainNew.BeforeCustKey = 0;
    //    tbCustMainNew.BeforeBelong = "";
    //    tbCustMainNew.BeforeCompName = "";
    //    tbCustMainNew.Using = true;
    //}
    //private void OrgTableToUiData() // 누락된 부분 보강해야함.
    //{      
    //    TbCustMain tbCustMainBK = tbAllWithOrg.custMain;
    //    TbCompany tbCompanyBK = tbAllWithOrg.company;
    //    TbCallCenter tbCallCenterBK = tbAllWithOrg.callCenter;

    //    TBoxCustCode.Text = tbCustMainBK.KeyCode.ToString();
    //    // ??? = tbCustMainBK.Working;
    //    // ??? = tbCustMainBK.MemberCode;
    //    // ??? = tbCustMainBK.CenterCode;
    //    // ??? = tbCustMainBK.CompCode;
    //    TBoxCustName.Text = tbCustMainBK.CustName;
    //    TBoxTelNo1.Text = tbCustMainBK.TelNo1;
    //    TBoxTelNo2.Text = tbCustMainBK.TelNo2;
    //    TBoxDongBasic.Text = tbCustMainBK.DongBasic;
    //    TBoxDongAddr.Text = tbCustMainBK.DongAddr;
    //    TBoxDetailAddr.Text = tbCustMainBK.DetailAddr;
    //    TBoxDeptName.Text = tbCustMainBK.DeptName;
    //    TBoxChargeName.Text = tbCustMainBK.ChargeName;
    //    SetTradeType(tbCustMainBK.TradeType);
    //    TBoxRemarks.Text = tbCustMainBK.Remarks;
    //    TBoxMemo.Text = tbCustMainBK.Memo;
    //    TBoxRegister.Text = tbCustMainBK.Register;
    //    TBoxRegDate.Text = tbCustMainBK.RegDate == null ? "" : ((DateTime)tbCustMainBK.RegDate).ToString(StdConst_Var.DTFORMAT_DATEONLY); // RegDate
    //    // ??? = tbCustMainBK.EditDate;
    //    // ??? = tbCustMainBK.Lon;
    //    // ??? = tbCustMainBK.Lat;
    //    // ??? = tbCustMainBK.SiDoName;
    //    // ??? = tbCustMainBK.GunGuName;
    //    // ??? = tbCustMainBK.DongRiName;
    //    //CmbBoxWonPercent.SelectedIndex = tbCustMainBK.??? ? 1 : 0;
    //    SetDiscountType(tbCustMainBK.DiscountType);
    //    TBoxFaxNo.Text = tbCustMainBK.FaxNo;
    //    TBoxEmail.Text = tbCustMainBK.Email;
    //    TBoxUserId.Text = tbCustMainBK.CustId;
    //    TBoxUserPw.Text = tbCustMainBK.CustPw;
    //    TBoxEtc01.Text = tbCustMainBK.Etc01;
    //    // ??? = tbCustMainBK.Etc02;
    //    CmbBoxHappyCall.SelectedIndex = tbCustMainBK.HappyCall ? 1 : 0;
    //    // ??? = tbCustMainBK.BeforeCustKey;
    //    //m_sBeforeBelong = tbCustMainBK.BeforeBelong;
    //    // ??? = tbCustMainBK.BeforeCompKey;
    //    //tbSave.Using = true;
    //    CmbBoxUseType.SelectedIndex = tbCustMainBK.Using ? 0 : 1;
    //}
    //private void CreateNewTableCopyFromOrg()
    //{
    //    tbAllWithNew = new TbAllWith();
    //    TbCustMain tbCustMainNew = tbAllWithNew.custMain = new TbCustMain();
    //    TbCustMain tbCustMainOrg = tbAllWithOrg.custMain;

    //    tbCustMainNew.KeyCode = tbCustMainOrg.KeyCode;
    //    tbCustMainNew.MemberCode = tbCustMainOrg.MemberCode;
    //    tbCustMainNew.CenterCode = tbCustMainOrg.CenterCode;
    //    tbCustMainNew.CompCode = tbCustMainOrg.CompCode;
    //    tbCustMainNew.CustName = tbCustMainOrg.CustName;
    //    tbCustMainNew.TelNo1 = tbCustMainOrg.TelNo1;
    //    tbCustMainNew.TelNo2 = tbCustMainOrg.TelNo2;
    //    tbCustMainNew.DongBasic = tbCustMainOrg.DongBasic;
    //    tbCustMainNew.DongAddr = tbCustMainOrg.DongAddr;
    //    tbCustMainNew.DetailAddr = tbCustMainOrg.DetailAddr;
    //    tbCustMainNew.DeptName = tbCustMainOrg.DeptName;
    //    tbCustMainNew.ChargeName = tbCustMainOrg.ChargeName;
    //    tbCustMainNew.TradeType = tbCustMainOrg.TradeType;
    //    tbCustMainNew.Remarks = tbCustMainOrg.Remarks;
    //    tbCustMainNew.Memo = tbCustMainOrg.Memo;
    //    tbCustMainNew.Register = tbCustMainOrg.Register;
    //    tbCustMainNew.RegDate = tbCustMainOrg.RegDate;
    //    //tbCustMainNew.EditDate = tbCustMainOrg.EditDate;
    //    tbCustMainNew.Lon = tbCustMainOrg.Lon;
    //    tbCustMainNew.Lat = tbCustMainOrg.Lat;
    //    tbCustMainNew.SiDoName = tbCustMainOrg.SiDoName;
    //    tbCustMainNew.GunGuName = tbCustMainOrg.GunGuName;
    //    tbCustMainNew.DongRiName = tbCustMainOrg.DongRiName;
    //    tbCustMainNew.DiscountType = tbCustMainOrg.DiscountType;
    //    tbCustMainNew.FaxNo = tbCustMainOrg.FaxNo;
    //    tbCustMainNew.Email = tbCustMainOrg.Email;
    //    tbCustMainNew.CustId = tbCustMainOrg.CustId;
    //    tbCustMainNew.CustPw = tbCustMainOrg.CustPw;
    //    tbCustMainNew.Etc01 = tbCustMainOrg.Etc01;
    //    tbCustMainNew.Etc02 = tbCustMainOrg.Etc02;
    //    tbCustMainNew.HappyCall = tbCustMainOrg.HappyCall;
    //    tbCustMainNew.BeforeCustKey = tbCustMainOrg.BeforeCustKey;
    //    tbCustMainNew.BeforeBelong = tbCustMainOrg.BeforeBelong;
    //    tbCustMainNew.BeforeCompName = tbCustMainOrg.BeforeCompName;
    //    tbCustMainNew.Using = tbCustMainOrg.Using;
    //}
    //private void UpdateNewTableByUi() // UI Data에서 table로
    //{
    //    TbCustMain tbCustMainNew = tbAllWithNew.custMain;
    //    //TbCompany tbCompanyNew = tbAllWithNew.company;

    //    tbCustMainNew.CustName = TBoxCustName.Text;
    //    tbCustMainNew.TelNo1 = TBoxTelNo1.Text;
    //    tbCustMainNew.TelNo2 = TBoxTelNo2.Text;
    //    tbCustMainNew.DongBasic = TBoxDongBasic.Text;
    //    tbCustMainNew.DongAddr = TBoxDongAddr.Text;
    //    tbCustMainNew.DetailAddr = TBoxDetailAddr.Text;
    //    tbCustMainNew.DeptName = TBoxDeptName.Text;
    //    tbCustMainNew.ChargeName = TBoxChargeName.Text;
    //    tbCustMainNew.TradeType = GetTradeType();
    //    tbCustMainNew.Remarks = TBoxRemarks.Text;
    //    tbCustMainNew.Memo = TBoxMemo.Text;
    //    tbCustMainNew.Register = s_CenterCharge.Id;
    //    //tbCustMainNew.RegDate = 
    //    tbCustMainNew.EditDate = DateTime.Now;
    //    //tbCustMainNew.Lon = ; // 나중에 구현...
    //    //tbCustMainNew.Lat = ; // 나중에 구현...
    //    //tbCustMainNew.SiDoName = ; // 나중에 구현 하거나 삭제...
    //    //tbCustMainNew.GunGuName = ; // 나중에 구현 하거나 삭제...
    //    //tbCustMainNew.DongRiName = ; // 나중에 구현 하거나 삭제...
    //    tbCustMainNew.DiscountType = GetDiscountType(); // 나중에 구현 하거나 삭제...
    //    tbCustMainNew.FaxNo = TBoxFaxNo.Text;
    //    tbCustMainNew.Email = TBoxEmail.Text;
    //    tbCustMainNew.CustId = TBoxUserId.Text;
    //    tbCustMainNew.CustPw = TBoxUserPw.Text;
    //    tbCustMainNew.Etc01 = TBoxEtc01.Text;
    //    //tbCustMainNew.Etc02 = ""; // Reserved
    //    tbCustMainNew.HappyCall = CmbBoxHappyCall.SelectedIndex == 0 ? false : true; // 나중에 구현...
    //    //tbCustMainNew.BeforeCustKey = lExtKeyCode; // 자체 작성한 고객정보
    //    //tbCustMainNew.BeforeBelong = sBefBeleong;
    //    //tbCustMainNew.BeforeCompName = ; // Reserved
    //    tbCustMainNew.Using = CmbBoxUseType.SelectedIndex == 0 ? true : false;
    //}
    //private bool IsCanUpdateTable()
    //{
    //    //// 변경되면 안되는 데이타들
    //    //Debug.WriteLine($"KeyCode: {tbCustMainOrg.KeyCode} -> {tbCustMainNew.KeyCode}"); // Test
    //    //Debug.WriteLine($"Working: {tbCustMainOrg.Working} -> {tbCustMainNew.Working}"); // Test
    //    //Debug.WriteLine($"MemberCode: {tbCustMainOrg.MemberCode} -> {tbCustMainNew.MemberCode}"); // Test
    //    //Debug.WriteLine($"CenterCode: {tbCustMainOrg.CenterCode} -> {tbCustMainNew.CenterCode}"); // Test
    //    //Debug.WriteLine($"CompCode: {tbCustMainOrg.CompCode} -> {tbCustMainNew.CompCode}"); // Test

    //    TbCustMain tbCustMainOrg = tbAllWithOrg.custMain;
    //    TbCustMain tbCustMainNew = tbAllWithNew.custMain;

    //    if (tbCustMainOrg.KeyCode != tbCustMainNew.KeyCode) return false;
    //    if (tbCustMainOrg.MemberCode != tbCustMainNew.MemberCode) return false;
    //    if (tbCustMainOrg.CenterCode != tbCustMainNew.CenterCode) return false;
    //    if (tbCustMainOrg.CompCode != tbCustMainNew.CompCode) return false;

    //    // UI 데이타들
    //    if (tbCustMainOrg.CustName != tbCustMainNew.CustName) return true;
    //    if (tbCustMainOrg.TelNo1 != tbCustMainNew.TelNo1) return true;
    //    if (tbCustMainOrg.TelNo2 != tbCustMainNew.TelNo2) return true;
    //    if (tbCustMainOrg.DongBasic != tbCustMainNew.DongBasic) return true;
    //    if (tbCustMainOrg.DongAddr != tbCustMainNew.DongAddr) return true;
    //    if (tbCustMainOrg.DetailAddr != tbCustMainNew.DetailAddr) return true;
    //    if (tbCustMainOrg.DeptName != tbCustMainNew.DeptName) return true;
    //    if (tbCustMainOrg.ChargeName != tbCustMainNew.ChargeName) return true;
    //    if (tbCustMainOrg.TradeType != tbCustMainNew.TradeType) return true;
    //    if (tbCustMainOrg.Remarks != tbCustMainNew.Remarks) return true;
    //    if (tbCustMainOrg.Memo != tbCustMainNew.Memo) return true;
    //    if (tbCustMainOrg.Register != tbCustMainNew.Register) return true; // 심층연구과제
    //    if (tbCustMainOrg.RegDate != tbCustMainNew.RegDate) return true; // 심층연구과제
    //    //if (tbCustMainOrg.EditDate != tbCustMainNew.EditDate) return true; // 변경 되야 함.
    //    if (tbCustMainOrg.Lon != tbCustMainNew.Lon) return true;
    //    if (tbCustMainOrg.Lat != tbCustMainNew.Lat) return true;
    //    if (tbCustMainOrg.SiDoName != tbCustMainNew.SiDoName) return true; // 나중에 구현 하거나 삭제...
    //    if (tbCustMainOrg.GunGuName != tbCustMainNew.GunGuName) return true; // 나중에 구현 하거나 삭제...
    //    if (tbCustMainOrg.DongRiName != tbCustMainNew.DongRiName) return true; // 나중에 구현 하거나 삭제...
    //    if (tbCustMainOrg.DiscountType != tbCustMainNew.DiscountType) return true;
    //    if (tbCustMainOrg.FaxNo != tbCustMainNew.FaxNo) return true;
    //    if (tbCustMainOrg.Email != tbCustMainNew.Email) return true;
    //    if (tbCustMainOrg.CustId != tbCustMainNew.CustId) return true;
    //    if (tbCustMainOrg.CustPw != tbCustMainNew.CustPw) return true;
    //    if (tbCustMainOrg.Etc01 != tbCustMainNew.Etc01) return true;
    //    if (tbCustMainOrg.Etc02 != tbCustMainNew.Etc02) return true; // Reserved
    //    if (tbCustMainOrg.HappyCall != tbCustMainNew.HappyCall) return true; // 나중에 구현...
    //    if (tbCustMainOrg.BeforeCustKey != tbCustMainNew.BeforeCustKey) return true; // 자체 작성한 고객정보
    //    if (tbCustMainOrg.BeforeBelong != tbCustMainNew.BeforeBelong) return true; // 자체 작성한 고객정보
    //    if (tbCustMainOrg.BeforeCompName != tbCustMainNew.BeforeCompName) return true; // 자체 작성한 고객정보
    //    if (tbCustMainOrg.Using != tbCustMainNew.Using) return true;

    //    return false;
    //}

    #endregion


}
#nullable restore
