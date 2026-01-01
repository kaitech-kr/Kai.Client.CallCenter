using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

using Kai.Common.StdDll_Common;
using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgBox;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Results;

using Kai.Client.CallCenter.MVVM.ViewModels;
using Kai.Client.CallCenter.MVVM.ViewServices;
using static Kai.Client.CallCenter.Classes.CommonVars;
using static Kai.Client.CallCenter.Classes.CommonFuncs;

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
    // 닫기 버튼 클릭 - 거래처 조회 탭 닫기
    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine("[Company_RegistPage] 닫기 버튼 클릭");
        s_MainWnd?.RemoveTab(s_MainWnd.Company_CompRegistTab);
    }

    // 저장 버튼 클릭 - 거래처 정보 등록/수정
    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        //// 필수 입력 항목 검증
        //if (string.IsNullOrWhiteSpace(TBoxWrite_CompName.Text))
        //{
        //    ErrMsgBox("거래처명이 없습니다.", "BtnSave_Click");
        //    Debug.WriteLine("[Company_RegistPage] 저장 실패: 거래처명 없음");
        //    return;
        //}
        //if (string.IsNullOrWhiteSpace(TBoxWrite_CEOName.Text))
        //{
        //    ErrMsgBox("대표자명이 없습니다.", "BtnSave_Click");
        //    Debug.WriteLine("[Company_RegistPage] 저장 실패: 대표자명 없음");
        //    return;
        //}
        //if (string.IsNullOrWhiteSpace(TBoxWrite_TelNo.Text))
        //{
        //    ErrMsgBox("전화번호가 없습니다.", "BtnSave_Click");
        //    Debug.WriteLine("[Company_RegistPage] 저장 실패: 전화번호 없음");
        //    return;
        //}

        //if (DGridCompany.SelectedIndex >= 0) // Update Mode
        //{
        //    Debug.WriteLine($"[Company_RegistPage] 거래처 수정 모드: Index={DGridCompany.SelectedIndex}");

        //    // 선택된 항목 유효성 검증
        //    if (DGridCompany.SelectedIndex >= VsCompany_RegistPage.oc_VmCompany_RegistPage_Comp.Count)
        //    {
        //        ErrMsgBox("선택된 거래처 정보가 유효하지 않습니다.", "BtnSave_Click");
        //        Debug.WriteLine($"[Company_RegistPage] 인덱스 범위 초과: {DGridCompany.SelectedIndex} >= {VsCompany_RegistPage.oc_VmCompany_RegistPage_Comp.Count}");
        //        return;
        //    }

        //    VmCompany_RegistPage_Comp comp = VsCompany_RegistPage.oc_VmCompany_RegistPage_Comp[DGridCompany.SelectedIndex];
        //    if (!IsDataChanged(comp))
        //    {
        //        ErrMsgBox("변경된 데이터가 없습니다.", "BtnSave_Click");
        //        Debug.WriteLine("[Company_RegistPage] 저장 실패: 변경사항 없음");
        //        return;
        //    }

        //    TbCompany tbOld = comp.TbCompany;
        //    TbCompany tbNew = new TbCompany();

        //    //변경 불가 항목 복사
        //    tbNew.KeyCode = tbOld.KeyCode;
        //    tbNew.CenterCode = tbOld.CenterCode;
        //    tbNew.DtRegist = tbOld.DtRegist;
        //    tbNew.DtUpdate = DateTime.Now;
        //    tbNew.Etc1 = tbOld.Etc1;
        //    tbNew.Etc2 = tbOld.Etc2;
        //    tbNew.BeforeBelong = tbOld.BeforeBelong;
        //    tbNew.Using = tbOld.Using;

        //    // UI 입력값 복사
        //    PopulateTbCompanyFromUI(tbNew);

        //    Debug.WriteLine($"[Company_RegistPage] 거래처 수정 요청: KeyCode={tbNew.KeyCode}, CompName={tbNew.CompName}");

        //    //StdResult_Int result = await s_SrGClient.SrResult_Company_UpdateRowAsync(tbNew);
        //    StdResult_Int result = new StdResult_Int(1);
        //    if (result.nResult < 0)
        //    {
        //        ErrMsgBox($"거래처({tbNew.CompName}) 수정 실패\n{result.sErr}", "BtnSave_Click");
        //        Debug.WriteLine($"[Company_RegistPage] 수정 실패: sErr={result.sErr}");
        //        return;
        //    }

        //    // 리스트에서 해당 항목 찾아 업데이트
        //    int index = curListCompany.FindIndex(x => x.KeyCode == tbNew.KeyCode);
        //    if (index < 0)
        //    {
        //        Debug.WriteLine($"[Company_RegistPage] 리스트에서 거래처를 찾을 수 없습니다: KeyCode={tbNew.KeyCode}");
        //        return;
        //    }

        //    curListCompany[index] = tbNew;
        //    RefreshCompanyListAndSelect(tbNew.KeyCode);

        //    Debug.WriteLine($"[Company_RegistPage] 거래처 수정 완료: KeyCode={tbNew.KeyCode}, CompName={tbNew.CompName}");
        //}
        //else // Regist Mode
        //{
        //    Debug.WriteLine("[Company_RegistPage] 거래처 등록 모드");

        //     TbCompany tbNew = new TbCompany();

        //    // 기본값 설정
        //    tbNew.KeyCode = 0;
        //    tbNew.CenterCode = s_CenterCharge.CenterCode;
        //    tbNew.DtRegist = DateTime.Now;
        //    tbNew.DtUpdate = DateTime.Now;
        //    tbNew.Etc1 = "";
        //    tbNew.Etc2 = "";
        //    tbNew.BeforeBelong = "";
        //    tbNew.Using = true;

        //    // UI 입력값 복사
        //     PopulateTbCompanyFromUI(tbNew);

        //    //StdResult_Long result = await s_SrGClient.SrResult_Company_InsertRowAsync(tbNew);
        //    StdResult_Long result = new StdResult_Long(1);
        //    if (result.lResult <= 0)
        //    {
        //        ErrMsgBox($"거래처({tbNew.CompName}) 등록 실패\n{result.sErr}", "BtnSave_Click");
        //        Debug.WriteLine($"[Company_RegistPage] 등록 실패: sErr={result.sErr}");
        //        return;
        //    }
        //    tbNew.KeyCode = result.lResult;

        //    curListCompany.Add(tbNew);
        //    RefreshCompanyListAndSelect(tbNew.KeyCode);

        //    Debug.WriteLine($"[Company_RegistPage] 거래처 등록 완료: KeyCode={tbNew.KeyCode}, CompName={tbNew.CompName}");
        //}
    }

    // 삭제 버튼 클릭 - 선택된 거래처 삭제
    private async void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        //// 버튼 활성화 여부 확인
        //if (!BtnDelete.IsEnabled)
        //{
        //    Debug.WriteLine("[Company_RegistPage] 삭제 버튼 비활성화 상태");
        //    return;
        //}

        //// 선택된 항목 확인
        //if (DGridCompany.SelectedIndex < 0)
        //{
        //    Debug.WriteLine("[Company_RegistPage] 삭제: 선택된 항목 없음");
        //    return;
        //}

        //// 인덱스 유효성 검증
        //if (DGridCompany.SelectedIndex >= VsCompany_RegistPage.oc_VmCompany_RegistPage_Comp.Count)
        //{
        //    Debug.WriteLine($"[Company_RegistPage] 삭제 실패: 인덱스 범위 초과 {DGridCompany.SelectedIndex} >= {VsCompany_RegistPage.oc_VmCompany_RegistPage_Comp.Count}");
        //    return;
        //}

        //string compName = VsCompany_RegistPage.oc_VmCompany_RegistPage_Comp[DGridCompany.SelectedIndex].CompName;
        //long keyCode = VsCompany_RegistPage.oc_VmCompany_RegistPage_Comp[DGridCompany.SelectedIndex].KeyCode;

        //// 삭제 확인 메시지
        //MessageBoxResult resultMsg = MessageBox.Show(
        //$"거래처({compName})를 삭제하시겠습니까?", "확인", MessageBoxButton.OKCancel, MessageBoxImage.Warning);

        //if (resultMsg != MessageBoxResult.OK)
        //{
        //    Debug.WriteLine($"[Company_RegistPage] 거래처 삭제 취소: {compName}");
        //    return;
        //}

        //// Debug.WriteLine($"[Company_RegistPage] 거래처 삭제 요청: KeyCode={keyCode}, CompName={compName}");

        //// 서버에 삭제 요청
        ////StdResult_Bool result = await s_SrGClient.SrResult_Company_DeleteRowAsync_KeyCode(keyCode);
        //StdResult_Bool result = new StdResult_Bool(true);
        //if (!result.bResult)
        //{
        //    ErrMsgBox($"거래처({compName}) 삭제 실패\n{result.sErrNPos}", "BtnDelete_Click");
        //    Debug.WriteLine($"[Company_RegistPage] 삭제 실패: sErrNPos={result.sErrNPos}");
        //    return;
        //}

        //// 로컬 리스트에서 제거 및 UI 갱신
        //curListCompany.RemoveAll(x => x.KeyCode == keyCode);
        //VsCompany_RegistPage.LoadData_Comp(s_MainWnd, curListCompany);
        //DGridCompany.SelectedIndex = -1;
        //Grid_Right_Upper.IsEnabled = false;

        //Debug.WriteLine($"[Company_RegistPage] 거래처 삭제 완료: KeyCode={keyCode}, CompName={compName}");
    }

    // 신규
    private void BtnNewCust_Click(object sender, RoutedEventArgs e)
    {
        DGridCompany.SelectedIndex = -1;
        Grid_Right_Upper.IsEnabled = true;
        //SetButtonOpacity(BtnSave, true);
    }

    // 검색 버튼 클릭 - 거래처 검색 (조건별)
    private async void BtnSearch_Click(object sender, RoutedEventArgs e)
    {
        //try
        //{
        //    bool? bUsing = GetUsingType();
        //    string sTradeType = GetTradeType();
        //    string sCompName = TBoxSearch_CompName.Text;

        //    // 검색 조건 확인
        //    bool hasUsingCondition = bUsing != null;
        //    bool hasTradeTypeCondition = !string.IsNullOrWhiteSpace(sTradeType);
        //    bool hasCompNameCondition = !string.IsNullOrWhiteSpace(sCompName);

        //    // 검색 조건이 없으면 안내 메시지 표시
        //    if (!hasUsingCondition && !hasTradeTypeCondition && !hasCompNameCondition)
        //    {
        //        MessageBox.Show("검색 조건을 입력해주세요.", "안내", MessageBoxButton.OK, MessageBoxImage.Information);
        //        Debug.WriteLine("[Company_RegistPage] 검색 조건 없음");
        //        return;
        //    }

        //    // Debug.WriteLine($"[Company_RegistPage] 거래처 검색: CompName={sCompName}, TradeType={sTradeType}, Using={bUsing}");

        //    //PostgResult_TbCompanyList result = await s_SrGClient.SrResult_Company_SelectRowsAsync_CenterCode_CompName_TradType_Using(
        //    //    sCompName, sTradeType, bUsing);
        //    PostgResult_TbCompanyList result = new PostgResult_TbCompanyList();

        //    // 에러 확인 (sErrNPos 형식: "sErr: {에러메시지}\nsPos: {위치}")
        //    // "sErr: \nsPos: " 같은 빈 형식은 에러가 아님
        //    if (!string.IsNullOrWhiteSpace(result.sErrNPos))
        //    {
        //        string cleanedError = result.sErrNPos
        //            .Replace("sErr:", "")
        //            .Replace("sPos:", "")
        //            .Replace("\n", "")
        //            .Replace("\r", "")
        //            .Trim();

        //        if (!string.IsNullOrEmpty(cleanedError))
        //        {
        //            ErrMsgBox($"거래처 검색 실패\n{result.sErrNPos}", "BtnSearch_Click");
        //            Debug.WriteLine($"[Company_RegistPage] 검색 실패: sErrNPos={result.sErrNPos}");
        //            return;
        //        }
        //        else
        //        {
        //            Debug.WriteLine("[Company_RegistPage] sErrNPos에 형식 문자열만 있고 실제 에러 없음 (정상)");
        //        }
        //    }

        //    // 결과 바인딩 (null 안전성 강화)
        //    curListCompany = result?.listTb ?? new List<TbCompany>();
        //    VsCompany_RegistPage.LoadData_Comp(s_MainWnd, curListCompany);

        //    Debug.WriteLine($"[Company_RegistPage] 거래처 검색 완료: {curListCompany.Count}건");
        //}
        //catch (Exception ex)
        //{
        //    ErrMsgBox($"거래처 검색 오류\n{StdUtil.GetExceptionMessage(ex)}", "BtnSearch_Click");
        //    Debug.WriteLine($"[Company_RegistPage] 검색 예외: {ex.Message}");
        //}
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
    // DataGrid 선택 변경 - 선택된 거래처 정보를 UI에 표시하고 해당 고객 목록 조회
    private async void DGridCompany_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        //if (DGridCompany.SelectedIndex < 0) // 선택 항목 없음 - UI 초기화
        //{
        //    Debug.WriteLine("[Company_RegistPage] DataGrid 선택 해제 - UI 초기화");
        //    ClearUI();
        //    //SetButtonOpacity(BtnSave, false);
        //    //SetButtonOpacity(BtnDelete, false);
        //    return;
        //}

        //// 인덱스 유효성 검증
        //if (DGridCompany.SelectedIndex >= VsCompany_RegistPage.oc_VmCompany_RegistPage_Comp.Count)
        //{
        //    Debug.WriteLine($"[Company_RegistPage] 인덱스 범위 초과: {DGridCompany.SelectedIndex} >= {VsCompany_RegistPage.oc_VmCompany_RegistPage_Comp.Count}");
        //    return;
        //}

        //try
        //{
        //    VmCompany_RegistPage_Comp comp = VsCompany_RegistPage.oc_VmCompany_RegistPage_Comp[DGridCompany.SelectedIndex];
        //    long lCompCode = ClassToUI(comp);

        //    Debug.WriteLine($"[Company_RegistPage] 거래처 선택: KeyCode={comp.KeyCode}, CompName={comp.CompName}");

        //    //해당 거래처의 고객 목록 조회
        //    //PostgResult_TbCustMainList result = await s_SrGClient.SrResult_CustMain_SelectRowsAsync_CenterCode_CompCode(lCompCode);
        //    PostgResult_TbCustMainList result = new PostgResult_TbCustMainList();

        //    //에러 확인
        //    if (!string.IsNullOrWhiteSpace(result.sErrNPos))
        //    {
        //        Debug.WriteLine($"[Company_RegistPage] 고객 목록 조회 실패: sErrNPos={result.sErrNPos}");
        //        VsCompany_RegistPage.LoadData_Cust(s_MainWnd, new List<TbCustMain>());
        //    }
        //    else
        //    {
        //        VsCompany_RegistPage.LoadData_Cust(s_MainWnd, result.listTb ?? new List<TbCustMain>());
        //        Debug.WriteLine($"[Company_RegistPage] 고객 목록 조회 완료: {result.listTb?.Count ?? 0}건");
        //    }

        //    //SetButtonOpacity(BtnSave, true);
        //    //SetButtonOpacity(BtnDelete, true);
        //    Grid_Right_Upper.IsEnabled = true;
        //}
        //catch (Exception ex)
        //{
        //    Debug.WriteLine($"[Company_RegistPage] SelectionChanged 예외: {ex.Message}");
        //    ErrMsgBox($"거래처 정보 로드 오류\n{StdUtil.GetExceptionMessage(ex)}", "DGridCompany_SelectionChanged");
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

    // 전화번호 입력 제한 - 숫자만 입력 가능
    private void TelNo_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
        // 숫자만 허용
        e.Handled = !char.IsDigit(e.Text, 0);
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

    ///// <summary>
    ///// 거래처 목록 갱신 및 선택
    ///// </summary>
    ///// <param name="keyCode">선택할 거래처 KeyCode</param>
    //private void RefreshCompanyListAndSelect(long keyCode)
    //{
    //    VsCompany_RegistPage.LoadData_Comp(s_MainWnd, curListCompany);
    //    DGridCompany.SelectedIndex = VsCompany_RegistPage.oc_VmCompany_RegistPage_Comp.ToList()
    //        .FindIndex(x => x.KeyCode == keyCode);
    //    DGridCompany.Focus();
    //}

    // UI 입력값을 TbCompany 객체에 복사하는 헬퍼 메서드
    private void PopulateTbCompanyFromUI(TbCompany tb)
    {
        tb.CompName = TBoxWrite_CompName.Text;
        tb.TelNo = StdConvert.MakePhoneNumberToDigit(TBoxWrite_TelNo.Text);
        tb.FaxNo = StdConvert.MakePhoneNumberToDigit(TBoxWrite_FaxNo.Text);
        tb.Owner = TBoxWrite_CEOName.Text;
        tb.ChargeName = TBoxWrite_ChargeNmae.Text;

        // 위치 정보 (향후 구현 예정 - 지도 API 연동 필요)
        tb.Lon = 0;
        tb.Lat = 0;

        tb.DiscountType = null; //GetSelectedComboBoxContent(CmbBoxWrite_DiscountType);
        tb.TradeType = null; //GetSelectedComboBoxContent(CmbBoxWrite_TradeType);
        tb.Register = s_CenterCharge.Id;
        tb.Memo = TBoxWrite_Memo.Text;
    }

    // 사용 여부 라디오버튼 선택값 가져오기
    private bool? GetUsingType()
    {
        if (StdConvert.NullableBoolToBool(RdoBtnSearch_UseOnly.IsChecked)) return true;
        else if (StdConvert.NullableBoolToBool(RdoBtnSearch_NotUse.IsChecked)) return false;

        return null;
    }

    // 거래 유형 ComboBox 선택값 가져오기
    private string GetTradeType()
    {
        string result = "전체"; //GetSelectedComboBoxContent(CmbBoxSearch_TradeType);
        if (result == "전체") return "";
        return result;
    }

    ///// <summary>
    ///// ViewModel 데이터를 UI에 표시
    ///// </summary>
    ///// <returns>거래처 KeyCode</returns>
    //private long ClassToUI(VmCompany_RegistPage_Comp comp)
    //{
    //    TBoxWrite_CompName.Text = comp.CompName;
    //    TBoxWrite_CEOName.Text = comp.Owner;
    //    TBoxWrite_ChargeNmae.Text = comp.TbCompany.ChargeName;
    //    TBoxWrite_TelNo.Text = comp.TelNo;
    //    TBoxWrite_FaxNo.Text = StdConvert.ToPhoneNumberFormat(comp.TbCompany.FaxNo);
    //    TBoxWrite_Memo.Text = comp.TbCompany.Memo;
    //    TBoxWrite_RegDate.Text = comp.DtRegist;
    //    TBoxWrite_Register.Text = comp.TbCompany.Register;

    //    // ComboBox 설정
    //    //SetComboBoxItemByContent(CmbBoxWrite_DiscountType, comp.TbCompany.DiscountType ?? "없음");
    //    //SetComboBoxItemByContent(CmbBoxWrite_TradeType, comp.TbCompany.TradeType ?? "");

    //    return comp.KeyCode;
    //}

    ///// <summary>
    ///// UI 입력 필드 초기화
    ///// </summary>
    //private void ClearUI()
    //{
    //    TBoxWrite_CompName.Text = "";
    //    TBoxWrite_CEOName.Text = "";
    //    TBoxWrite_ChargeNmae.Text = "";
    //    TBoxWrite_TelNo.Text = "";
    //    TBoxWrite_FaxNo.Text = "";
    //    TBoxWrite_Memo.Text = "";
    //    TBoxWrite_RegDate.Text = "";
    //    TBoxWrite_Register.Text = "";

    //    // ComboBox 초기화
    //    CmbBoxWrite_DiscountType.SelectedIndex = -1;
    //    CmbBoxWrite_TradeType.SelectedIndex = -1;
    //}

    ///// <summary>
    ///// UI 입력값이 원본 데이터와 다른지 확인
    ///// </summary>
    //private bool IsDataChanged(VmCompany_RegistPage_Comp comp)
    //{
    //    if (TBoxWrite_CompName.Text != comp.CompName) return true;
    //    if (TBoxWrite_CEOName.Text != comp.Owner) return true;
    //    if (TBoxWrite_ChargeNmae.Text != comp.TbCompany.ChargeName) return true;
    //    if (TBoxWrite_TelNo.Text != comp.TelNo) return true;
    //    if (TBoxWrite_FaxNo.Text != StdConvert.ToPhoneNumberFormat(comp.TbCompany.FaxNo)) return true;
    //    if (TBoxWrite_Memo.Text != comp.TbCompany.Memo) return true;

    //    // 등록일자, 등록자는 읽기 전용이므로 비교 제외
    //    // TODO: 필요시 수정일, 수정자 추가 고려

    //    // ComboBox 비교
    //    string currentDiscountType = ""; //GetSelectedComboBoxContent(CmbBoxWrite_DiscountType);
    //    if (currentDiscountType != comp.TbCompany.DiscountType) return true;

    //    string currentTradeType = ""; //GetSelectedComboBoxContent(CmbBoxWrite_TradeType);
    //    if (currentTradeType != comp.TbCompany.TradeType) return true;

    //    return false;
    //}
    #endregion
}
#nullable enable
