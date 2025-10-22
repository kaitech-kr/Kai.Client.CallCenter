using System.Windows;
using System.Diagnostics;
using System.Windows.Controls;
using System.Text.RegularExpressions;

using Kai.Common.FrmDll_FormCtrl;
using Kai.Common.StdDll_Common;
using Kai.Common.NetDll_WpfCtrl.NetUtils;
using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgBox;

using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Results;

using Kai.Client.CallCenter.MVVM.ViewModels;
using Kai.Client.CallCenter.MVVM.ViewServices;
using static Kai.Client.CallCenter.Classes.CommonVars;
using static Kai.Client.CallCenter.Classes.CommonFuncs;

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


    /// <summary>
    /// 윈도우 로드 시 초기화 및 데이터 로드
    /// </summary>
    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // 공용 초기화
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        TBoxRegister.Text = s_CenterCharge.Id;

        // 신규 등록모드
        if (tbAllWithOrg == null)
        {
            // 키가 있으면 DB에서 고객정보 조회 (수정모드로 전환)
            if (lCallCustKeyOrg > 0)
            {
                bool loaded = await LoadCustomerForEditModeAsync();
                if (!loaded) return; // 로드 실패 시 창 닫힘
            }
            // 키가 없으면 신규 등록모드 유지
        }
        // 수정모드
        else
        {
            // 이미 테이블이 있으면 UI에 반영
            OrgTableToUiData();
        }
    }

    /// <summary>
    /// 수정모드로 고객정보 로드 (DB 조회)
    /// </summary>
    private async Task<bool> LoadCustomerForEditModeAsync()
    {
        // DB에서 고객정보 조회
        PostgResult_AllWith result = await s_SrGClient.SrResult_CustMainWith_Cust_Center_Comp_SelectRowAsync_CenterCode_KeyCode(lCallCustKeyOrg);
        if (result == null || result.tbAll == null)
        {
            ErrMsgBox($"고객정보를 찾을 수 없습니다.\n고객키: {lCallCustKeyOrg}\n센터코드: {s_CenterCharge.CenterCode}", "LoadCustomerForEditModeAsync");
            this.Close();
            return false;
        }

        // 조회 성공 - 수정모드로 전환
        tbAllWithOrg = result.tbAll;
        OrgTableToUiData();
        return true;
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

    /// <summary>
    /// 저장 버튼 클릭 - 신규 등록 또는 수정
    /// </summary>
    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        // 필수입력 체크
        if (!ValidateRequiredInputs()) return;

        bool success = false;

        // 모드에 따라 처리
        if (tbAllWithOrg == null) // 신규등록
        {
            success = await SaveNewCustomerAsync();
        }
        else // 수정
        {
            success = await UpdateExistingCustomerAsync();
        }

        // 성공 시 창 닫기
        if (success)
        {
            this.DialogResult = true;
            this.Close();
        }
    }

    private void BtnToExcel_Click(object sender, RoutedEventArgs e)
    {

    }
    #endregion

    #region TextBox Events
    /// <summary>
    /// 숫자만 입력 가능하도록 제한 (전화번호 입력용)
    /// </summary>
    private void TBoxOnlyNum_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
        // 정규식: 숫자가 아닌 문자가 있으면 true 반환 → 입력 차단
        e.Handled = s_RegexOnlyNum.IsMatch(e.Text);
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
            SetButtonOpacity(BtnSave, true);
            return true;
        }
        else
        {
            SetButtonOpacity(BtnSave, false);
            return false;
        }
    }
    /// <summary>
    /// 필수 입력 검증
    /// </summary>
    private bool ValidateRequiredInputs()
    {
        if (string.IsNullOrWhiteSpace(TBoxCustName.Text))
        {
            ErrMsgBox("고객명을 입력하세요.", "ValidateRequiredInputs");
            TBoxCustName.Focus();
            return false;
        }
        if (string.IsNullOrWhiteSpace(TBoxTelNo1.Text))
        {
            ErrMsgBox("전화번호를 입력하세요.", "ValidateRequiredInputs");
            TBoxTelNo1.Focus();
            return false;
        }
        if (string.IsNullOrWhiteSpace(TBoxDongBasic.Text))
        {
            ErrMsgBox("동명을 입력하세요.", "ValidateRequiredInputs");
            TBoxDongBasic.Focus();
            return false;
        }
        return true;
    }

    /// <summary>
    /// 신규 고객 저장
    /// </summary>
    private async Task<bool> SaveNewCustomerAsync()
    {
        CreateEmptyNewTable();
        UpdateNewTableByUi();

        // DB에 저장
        StdResult_Long result = await s_SrGClient.SrResult_CustMain_InsertRowAsync(tbAllWithNew.custMain);
        if (result.lResult <= 0)
        {
            ErrMsgBox($"신규 저장 실패\n{result.sErrNPos}", "SaveNewCustomerAsync");
            return false;
        }

        tbAllWithNew.custMain.KeyCode = result.lResult;
        return true;
    }

    /// <summary>
    /// 기존 고객 수정
    /// </summary>
    private async Task<bool> UpdateExistingCustomerAsync()
    {
        // 원본에서 복사
        tbAllWithNew = new TbAllWith();
        tbAllWithNew.custMain = NetUtil.DeepCopyFrom(tbAllWithOrg.custMain);

        // UI 데이터 반영
        UpdateNewTableByUi();

        // 변경사항 확인
        if (!HasChanges())
        {
            ErrMsgBox("수정한 데이터가 없습니다.", "UpdateExistingCustomerAsync");
            return false;
        }

        // DB에 저장
        StdResult_Int result = await s_SrGClient.SrResult_CustMain_UpdateRowAsync(tbAllWithNew.custMain);
        if (result.nResult <= 0)
        {
            ErrMsgBox($"수정 저장 실패\n{result.sErrNPos}", "UpdateExistingCustomerAsync");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 빈 새 테이블 생성 (신규 등록용)
    /// </summary>
    private void CreateEmptyNewTable()
    {
        tbAllWithNew = new TbAllWith();
        TbCustMain tbCustMainNew = tbAllWithNew.custMain = new TbCustMain();

        // 기본정보 채우기
        tbCustMainNew.KeyCode = 0;
        tbCustMainNew.MemberCode = s_CenterCharge.MemberCode;
        tbCustMainNew.CenterCode = s_CenterCharge.CenterCode;
        tbCustMainNew.CompCode = 0;
        tbCustMainNew.HappyCall = false;
        tbCustMainNew.BeforeCustKey = 0;
        tbCustMainNew.BeforeBelong = "";
        tbCustMainNew.BeforeCompName = "";
        tbCustMainNew.Using = true;
    }
    /// <summary>
    /// 원본 테이블 데이터를 UI에 반영
    /// </summary>
    private void OrgTableToUiData()
    {
        if (tbAllWithOrg == null) return;

        TbCustMain tbCustMainBK = tbAllWithOrg.custMain;

        TBoxCustCode.Text = tbCustMainBK.KeyCode.ToString();
        TBoxCustName.Text = tbCustMainBK.CustName ?? "";
        TBoxTelNo1.Text = tbCustMainBK.TelNo1 ?? "";
        TBoxTelNo2.Text = tbCustMainBK.TelNo2 ?? "";
        TBoxDongBasic.Text = tbCustMainBK.DongBasic ?? "";
        TBoxDongAddr.Text = tbCustMainBK.DongAddr ?? "";
        TBoxDetailAddr.Text = tbCustMainBK.DetailAddr ?? "";
        TBoxDeptName.Text = tbCustMainBK.DeptName ?? "";
        TBoxChargeName.Text = tbCustMainBK.ChargeName ?? "";
        SetTradeType(tbCustMainBK.TradeType);
        TBoxRemarks.Text = tbCustMainBK.Remarks ?? "";
        TBoxMemo.Text = tbCustMainBK.Memo ?? "";
        TBoxRegister.Text = tbCustMainBK.Register ?? "";
        TBoxRegDate.Text = tbCustMainBK.RegDate.HasValue
            ? tbCustMainBK.RegDate.Value.ToString(StdConst_Var.DTFORMAT_DATEONLY)
            : "";
        SetDiscountType(tbCustMainBK.DiscountType);
        TBoxFaxNo.Text = tbCustMainBK.FaxNo ?? "";
        TBoxEmail.Text = tbCustMainBK.Email ?? "";
        TBoxUserId.Text = tbCustMainBK.CustId ?? "";
        TBoxUserPw.Text = tbCustMainBK.CustPw ?? "";
        TBoxEtc01.Text = tbCustMainBK.Etc01 ?? "";
        CmbBoxHappyCall.SelectedIndex = tbCustMainBK.HappyCall ? 1 : 0;
        CmbBoxUseType.SelectedIndex = tbCustMainBK.Using ? 0 : 1;
    }

    /// <summary>
    /// UI 데이터를 테이블로 반영
    /// </summary>
    private void UpdateNewTableByUi()
    {
        TbCustMain tbCustMainNew = tbAllWithNew.custMain;

        tbCustMainNew.CustName = TBoxCustName.Text;
        tbCustMainNew.TelNo1 = TBoxTelNo1.Text;
        tbCustMainNew.TelNo2 = TBoxTelNo2.Text;
        tbCustMainNew.DongBasic = TBoxDongBasic.Text;
        tbCustMainNew.DongAddr = TBoxDongAddr.Text;
        tbCustMainNew.DetailAddr = TBoxDetailAddr.Text;
        tbCustMainNew.DeptName = TBoxDeptName.Text;
        tbCustMainNew.ChargeName = TBoxChargeName.Text;
        tbCustMainNew.TradeType = GetTradeType();
        tbCustMainNew.Remarks = TBoxRemarks.Text;
        tbCustMainNew.Memo = TBoxMemo.Text;
        tbCustMainNew.Register = s_CenterCharge.Id;
        tbCustMainNew.EditDate = DateTime.Now;
        tbCustMainNew.DiscountType = GetDiscountType();
        tbCustMainNew.FaxNo = TBoxFaxNo.Text;
        tbCustMainNew.Email = TBoxEmail.Text;
        tbCustMainNew.CustId = TBoxUserId.Text;
        tbCustMainNew.CustPw = TBoxUserPw.Text;
        tbCustMainNew.Etc01 = TBoxEtc01.Text;
        tbCustMainNew.HappyCall = CmbBoxHappyCall.SelectedIndex == 1;
        tbCustMainNew.Using = CmbBoxUseType.SelectedIndex == 0;
    }
    /// <summary>
    /// 변경사항 확인 (수정 모드용)
    /// </summary>
    private bool HasChanges()
    {
        TbCustMain orgCust = tbAllWithOrg.custMain;
        TbCustMain newCust = tbAllWithNew.custMain;

        // 변경되면 안 되는 필드 검증
        if (orgCust.KeyCode != newCust.KeyCode ||
            orgCust.MemberCode != newCust.MemberCode ||
            orgCust.CenterCode != newCust.CenterCode ||
            orgCust.CompCode != newCust.CompCode)
        {
            return false; // 무결성 오류
        }

        // UI에서 수정 가능한 필드 변경 감지
        return orgCust.CustName != newCust.CustName ||
               orgCust.TelNo1 != newCust.TelNo1 ||
               orgCust.TelNo2 != newCust.TelNo2 ||
               orgCust.DongBasic != newCust.DongBasic ||
               orgCust.DongAddr != newCust.DongAddr ||
               orgCust.DetailAddr != newCust.DetailAddr ||
               orgCust.DeptName != newCust.DeptName ||
               orgCust.ChargeName != newCust.ChargeName ||
               orgCust.TradeType != newCust.TradeType ||
               orgCust.Remarks != newCust.Remarks ||
               orgCust.Memo != newCust.Memo ||
               orgCust.DiscountType != newCust.DiscountType ||
               orgCust.FaxNo != newCust.FaxNo ||
               orgCust.Email != newCust.Email ||
               orgCust.CustId != newCust.CustId ||
               orgCust.CustPw != newCust.CustPw ||
               orgCust.Etc01 != newCust.Etc01 ||
               orgCust.HappyCall != newCust.HappyCall ||
               orgCust.Using != newCust.Using;
    }

    #endregion


}
#nullable restore
