using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Text.RegularExpressions;

using Kai.Common.StdDll_Common;
using Kai.Common.NetDll_WpfCtrl.NetWnds;
using Kai.Common.NetDll_WpfCtrl.NetUtils;
using static Kai.Common.FrmDll_FormCtrl.FormFuncs;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Results;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Services;

using static Kai.Client.CallCenter.Classes.CommonVars;
using Kai.Client.CallCenter.Windows;
using Kai.Client.CallCenter.MVVM.ViewServices;
using Kai.Client.CallCenter.MVVM.ViewModels;

namespace Kai.Client.CallCenter.Pages;
#nullable disable

public partial class CustMain_RegistPage : Page
{
    private enum CanSearchType
    {
        None = 0,
        CustName = 1,
        DeptName = 2,
        ChargeName = 4,
        TelNo = 8,
        DongDetail = 16,
        InternetID = 32,
        CompName = 64,
        Total = 128,
    }

    #region Variables
    private int m_nFlagInput = 0; // 유효한 입력이 있나...
    private bool? m_bUsing = null; // 사용여부 체크박스(null: 전체, true: 사용, false: 미사용)
    public List<TbAllWith> listTbAllWith = null;
    #endregion

    #region Basics
    public CustMain_RegistPage()
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

    #region CheckBox - Click
    // 전체 체크박스 클릭
    private void ChBoxTotal_Click(object sender, RoutedEventArgs e)
    {
        if (ChBoxTotal.IsChecked == true)
        {
            m_nFlagInput |= (int)CanSearchType.Total;

            TBoxCustName.Text = "";
            TBoxDeptName.Text = "";
            TBoxChargeName.Text = "";
            TBoxTelNo.Text = "";
            TBoxDongDetail.Text = "";
            TBoxInternetID.Text = "";
            TBoxCompName.Text = "";
        }
        else
        {
            m_nFlagInput = 0;
        }
    }

    // 인터넷아이디 체크박스 클릭
    private void ChBoxInternetID_Click(object sender, RoutedEventArgs e)
    {

    }
    #endregion

    #region 라디오 버튼 - Checked, Click
    // 전체 버튼
    private void RdoBtnUseOrNot_Checked(object sender, RoutedEventArgs e)
    {
        m_bUsing = null;
    }
    // 사용 체크박스 클릭
    private void RdoBtnUseOnly_Checked(object sender, RoutedEventArgs e)
    {
        m_bUsing = true;
    }
    // 미사용 체크박스 클릭
    private void RdoBtnNotUse_Checked(object sender, RoutedEventArgs e)
    {
        m_bUsing = false;
    }
    #endregion

    #region TextBox - TextChanged
    // 고객명
    private void TBoxCustName_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(TBoxCustName.Text))
        {
            m_nFlagInput &= ~(int)CanSearchType.CustName;
        }
        else
        {
            m_nFlagInput |= (int)CanSearchType.CustName;
            if ((m_nFlagInput & (int)CanSearchType.Total) != 0) ChBoxTotal.IsChecked = false;
        }
    }

    // 부서명
    private void TBoxDeptName_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(TBoxDeptName.Text))
        {
            m_nFlagInput &= ~(int)CanSearchType.DeptName;
        }
        else
        {
            m_nFlagInput |= (int)CanSearchType.DeptName;
            if ((m_nFlagInput & (int)CanSearchType.Total) != 0) ChBoxTotal.IsChecked = false;
        }
    }

    // 담당자
    private void TBoxChargeName_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(TBoxChargeName.Text))
        {
            m_nFlagInput &= ~(int)CanSearchType.ChargeName;
        }
        else
        {
            m_nFlagInput |= (int)CanSearchType.ChargeName;
            if ((m_nFlagInput & (int)CanSearchType.Total) != 0) ChBoxTotal.IsChecked = false;
        }
    }

    // 전화번호
    private void TBoxTelNo_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(TBoxTelNo.Text))
        {
            m_nFlagInput &= ~(int)CanSearchType.TelNo;
        }
        else
        {
            m_nFlagInput |= (int)CanSearchType.TelNo;
            if ((m_nFlagInput & (int)CanSearchType.Total) != 0) ChBoxTotal.IsChecked = false;
        }
    }

    // 위치
    private void TBoxDongDetail_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(TBoxDongDetail.Text))
        {
            m_nFlagInput &= ~(int)CanSearchType.DongDetail;
        }
        else
        {
            m_nFlagInput |= (int)CanSearchType.DongDetail;
            if ((m_nFlagInput & (int)CanSearchType.Total) != 0) ChBoxTotal.IsChecked = false;
        }
    }

    // 인터넷ID
    private void TBoxInternetID_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(TBoxInternetID.Text))
        {
            m_nFlagInput &= ~(int)CanSearchType.InternetID;
        }
        else
        {
            m_nFlagInput |= (int)CanSearchType.InternetID;
            if ((m_nFlagInput & (int)CanSearchType.Total) != 0) ChBoxTotal.IsChecked = false;
        }
    }

    // 거래처명
    private void TBoxCompName_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(TBoxCompName.Text))
        {
            m_nFlagInput &= ~(int)CanSearchType.CompName;
        }
        else
        {
            m_nFlagInput |= (int)CanSearchType.CompName;
            if ((m_nFlagInput & (int)CanSearchType.Total) != 0) ChBoxTotal.IsChecked = false;
        }
    }
    #endregion

    #region TextBox - Etc
    private void TBoxOnlyNum_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        // 정규식: 숫자만 허용
        Regex regex = new Regex("[^0-9]+"); // 숫자가 아니면 true
        e.Handled = regex.IsMatch(e.Text);  // true면 입력 차단
    }
    #endregion

    #region Datagrid Events
    /// <summary>
    /// DataGrid 더블클릭 - 선택된 고객 정보 수정 창 열기
    /// </summary>
    private void DGridCustMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DGridCustMain.SelectedItem is not VmCustMain_RegistPage selectedItem)
        {
            Debug.WriteLine("[CustMain_RegistPage] DataGrid 더블클릭: 선택된 항목 없음");
            return;
        }

        Debug.WriteLine($"[CustMain_RegistPage] 고객 수정 창 열기: KeyCode={selectedItem.tbAllWith.custMain.KeyCode}");

        // 고객 수정 창 열기
        CustMain_RegEditWnd custMain_EditWnd = new CustMain_RegEditWnd(selectedItem.tbAllWith);
        bool? dialogResult = SafeShowDialog.WithMainWindowToOwner(custMain_EditWnd, s_MainWnd);

        // 취소 또는 실패 시 종료
        if (dialogResult != true)
        {
            Debug.WriteLine("[CustMain_RegistPage] 고객 수정 취소됨");
            return;
        }

        // 수정된 데이터를 원본에 복사
        selectedItem.tbAllWith.custMain = NetUtil.DeepCopyFrom(custMain_EditWnd.tbAllWithNew.custMain);

        // DataGrid 갱신 (ViewModel 재생성)
        int index = DGridCustMain.SelectedIndex;
        if (index >= 0)
        {
            VsCustMain_RegistPage.oc_VmCustMainForPage[index] = new VmCustMain_RegistPage(selectedItem.tbAllWith);
            Debug.WriteLine($"[CustMain_RegistPage] 고객 정보 수정 완료: Index={index}, KeyCode={selectedItem.tbAllWith.custMain.KeyCode}");
        }
        else
        {
            Debug.WriteLine("[CustMain_RegistPage] DataGrid 갱신 실패: Index가 유효하지 않음");
        }
    }
    private void DGridCust_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
    }
    private void DGridCust_SizeChanged(object sender, SizeChangedEventArgs e)
    {
    }
    private void DGridCustMain_LoadingRow(object sender, DataGridRowEventArgs e) // xmal에서 HeadersVisibility="Column"
    {
        e.Row.Header = (e.Row.GetIndex() + 1).ToString(); // 행 번호 설정
    }
    #endregion

    #region Click - 주 버튼들.
    // 고객 삭제
    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (!BtnDelete.IsEnabled || DGridCustMain.SelectedIndex < 0) return;

        if (MessageBox.Show("삭제하시겠습니까?", "삭제", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            int index = DGridCustMain.SelectedIndex;
            long keyCode = VsCustMain_RegistPage.oc_VmCustMainForPage[index].tbCustMain.KeyCode;

            // 삭제
            VsCustMain_RegistPage.oc_VmCustMainForPage.RemoveAt(index);
            this.UpdateDatagridCount();
        }
    }

    /// <summary>
    /// 신규 고객 등록 버튼 - 고객 등록 창을 열고 저장 후 목록을 갱신하여 선택
    /// </summary>
    private async void BtnNewCust_Click(object sender, RoutedEventArgs e)
    {
        // 고객 등록 창 열기
        CustMain_RegEditWnd wnd = new CustMain_RegEditWnd();
        bool? dialogResult = SafeShowDialog.WithMainWindowToOwner(wnd, s_MainWnd);

        // 취소 또는 실패 시 종료
        if (dialogResult != true)
        {
            Debug.WriteLine("[CustMain_RegistPage] 신규 고객 등록 취소됨");
            return;
        }

        // 저장 성공 - 새로 등록된 고객의 KeyCode 확인
        long newKeyCode = wnd.tbAllWithNew?.custMain?.KeyCode ?? 0;
        if (newKeyCode <= 0)
        {
            Debug.WriteLine("[CustMain_RegistPage] 신규 고객 등록 완료되었으나 KeyCode가 유효하지 않습니다.");
            return;
        }

        Debug.WriteLine($"[CustMain_RegistPage] 신규 고객 등록 완료: KeyCode={newKeyCode}");

        // TODO: SearchAsync 활성화 필요 (현재 Line 432-494에 주석 처리됨)
        // 목록 갱신 후 신규 등록된 고객을 선택하려면 SearchAsync 호출 필요
        // await SearchAsync();

        // 신규 등록된 고객을 DataGrid에서 찾아 선택
        int index = VsCustMain_RegistPage.GetIndexByKeyCode(newKeyCode);
        if (index >= 0)
        {
            DGridCustMain.SelectedIndex = index;
            Debug.WriteLine($"[CustMain_RegistPage] 신규 고객 선택 완료: Index={index}");
        }
        else
        {
            Debug.WriteLine($"[CustMain_RegistPage] 신규 고객을 목록에서 찾을 수 없습니다: KeyCode={newKeyCode} (SearchAsync 호출 필요)");
        }
    }

    /// <summary>
    /// 조회 버튼 클릭 - 입력된 검색 조건으로 고객 정보 조회
    /// </summary>
    private async void BtnSearch_Click(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine("[CustMain_RegistPage] 조회 버튼 클릭");
        await SearchAsync();
    }

    // 엑셀
    private void BtnExcel_Click(object sender, RoutedEventArgs e)
    {

    }

    /// <summary>
    /// 닫기 버튼 클릭 - 고객 조회 탭 닫기
    /// </summary>
    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine("[CustMain_RegistPage] 닫기 버튼 클릭");
        s_MainWnd?.RemoveTab(s_MainWnd.Customer_CustRegistTab);
    }
    #endregion

    #region Click - Etc Buttons
    /// <summary>
    /// 검색내 조회 버튼 클릭 - 이미 조회된 결과에서 추가 필터링
    /// </summary>
    private void BtnSearchInFind_Click(object sender, RoutedEventArgs e)
    {
        // 조회 결과 확인
        if (listTbAllWith == null || listTbAllWith.Count == 0)
        {
            Debug.WriteLine("[CustMain_RegistPage] 검색내 조회: 조회 결과 없음");
            return;
        }

        Debug.WriteLine($"[CustMain_RegistPage] 검색내 조회 시작: 전체 {listTbAllWith.Count}건");

        try
        {
            NetLoadingWnd.ShowLoading(s_MainWnd, "검색내에서 필터링 중입니다...");

            bool? use = GetUsingType();
            VsCustMain_RegistPage.LoadData(s_MainWnd, listTbAllWith,
                use, TBoxCustName.Text, TBoxDeptName.Text, TBoxChargeName.Text, TBoxTelNo.Text, TBoxDongDetail.Text, TBoxInternetID.Text, TBoxCompName.Text);
            this.UpdateDatagridCount();

            Debug.WriteLine($"[CustMain_RegistPage] 검색내 조회 완료: 필터링 결과 {VsCustMain_RegistPage.oc_VmCustMainForPage.Count}건");
        }
        catch (Exception ex)
        {
            ErrMsgBox($"검색내 조회 오류\n{StdUtil.GetExceptionMessage(ex)}", "BtnSearchInFind_Click");
            Debug.WriteLine($"[CustMain_RegistPage] 검색내 조회 예외: {ex.Message}");
        }
        finally
        {
            NetLoadingWnd.HideLoading();
        }
    }

    // 화면지우기
    private void BtnResetScreen_Click(object sender, RoutedEventArgs e)
    {
        TBoxCustName.Text = "";
        TBoxDeptName.Text = "";
        TBoxChargeName.Text = "";
        TBoxTelNo.Text = "";
        TBoxDongDetail.Text = "";
        TBoxCompName.Text = "";
        TBoxInternetID.Text = "";

        ChBoxInternetID.IsChecked = false;
        ChBoxTotal.IsChecked = false;

        RdoBtnUseOrNot.IsChecked = true;
    }

    // 거래처 검색
    private void BtnCompSearch_Click(object sender, RoutedEventArgs e)
    {
        Company_SearchedWnd wnd = new Company_SearchedWnd();
        wnd.ShowDialog();
    }

    #endregion

    #region 1차 Funcs
    private bool? GetUsingType()
    {
        bool? result = null;

        if ((bool)RdoBtnUseOnly.IsChecked) result = true;
        else if ((bool)RdoBtnNotUse.IsChecked) result = false;

        return result;
    }
    private bool CanSearch()
    {
        return Dispatcher.Invoke(() =>
        {
            if (!string.IsNullOrEmpty(TBoxCustName.Text)) return true;
            if (!string.IsNullOrEmpty(TBoxDeptName.Text)) return true;
            if (!string.IsNullOrEmpty(TBoxChargeName.Text)) return true;
            if (!string.IsNullOrEmpty(TBoxTelNo.Text)) return true;
            if (!string.IsNullOrEmpty(TBoxDongDetail.Text)) return true;
            if (!string.IsNullOrEmpty(TBoxCompName.Text)) return true;
            if (!string.IsNullOrEmpty(TBoxInternetID.Text)) return true;

            if (ChBoxInternetID.IsChecked ?? false) return true;
            if (ChBoxTotal.IsChecked ?? false) return true;

            return false;
        });
    }
    private object[] GetSearchObjectArray()
    {
        return Dispatcher.Invoke(() =>
        {
            object[] objects = new object[10];

            objects[0] = TBoxCustName.Text;
            objects[1] = TBoxDeptName.Text;
            objects[2] = ChBoxInternetID.IsChecked ?? false;
            objects[3] = ChBoxTotal.IsChecked ?? false;
            objects[4] = TBoxChargeName.Text;
            objects[5] = TBoxTelNo.Text;
            objects[6] = TBoxDongDetail.Text;
            objects[7] = TBoxCompName.Text;
            objects[8] = TBoxInternetID.Text;
            objects[9] = GetUsingType();

            return objects;
        });
    }
    public void UpdateDatagridCount()
    {
        LblSum.Content = $"합계: {VsCustMain_RegistPage.oc_VmCustMainForPage.Count:##,###}개";
    }

    /// <summary>
    /// 고객 정보 검색 - 전체 검색 또는 조건별 검색 수행
    /// </summary>
    private async Task SearchAsync()
    {
        try
        {
            bool? use = GetUsingType();
            PostgResult_AllWithList result;

            // 전체 검색
            if (ChBoxTotal.IsChecked == true)
            {
                Debug.WriteLine("[CustMain_RegistPage] 전체 고객 검색 시작");
                NetLoadingWnd.ShowLoading(s_MainWnd, "고객정보를 로딩중입니다...");
                result = await s_SrGClient.SrResult_CustMainWith_SelectRowsAsync_CenterCode_Using(use);
            }
            // 조건 검색
            else
            {
                // 검색 조건 확인
                if (!CanSearch())
                {
                    MessageBox.Show("검색 조건을 입력해주세요.", "안내", MessageBoxButton.OK, MessageBoxImage.Information);
                    Debug.WriteLine("[CustMain_RegistPage] 검색 조건 없음");
                    return;
                }

                Debug.WriteLine($"[CustMain_RegistPage] 조건별 고객 검색 시작: 고객명={TBoxCustName.Text}, 전화번호={TBoxTelNo.Text}");
                NetLoadingWnd.ShowLoading(s_MainWnd, "고객정보를 로딩중입니다...");

                result = await s_SrGClient.SrResult_CustMainWith_SelectRowsAsync_CenterCode_CustNameWith11(
                    use,
                    TBoxCustName.Text,
                    TBoxDeptName.Text,
                    TBoxChargeName.Text,
                    TBoxTelNo.Text,
                    TBoxDongDetail.Text,
                    TBoxInternetID.Text,
                    TBoxCompName.Text);

                // TODO: TBoxCompName을 CompCode(Key)로 변경 필요
            }

            // 결과 검증
            if (result == null)
            {
                ErrMsgBox("검색 결과가 없습니다.", "SearchAsync");
                Debug.WriteLine("[CustMain_RegistPage] 검색 결과 null");
                return;
            }

            // 에러 메시지 확인 (공백 문자 포함 체크)
            Debug.WriteLine($"[CustMain_RegistPage] sErrNPos 값 확인: '{result.sErrNPos}', Length={result.sErrNPos?.Length ?? 0}, IsNullOrWhiteSpace={string.IsNullOrWhiteSpace(result.sErrNPos)}");

            // sErrNPos 형식: "sErr: {에러메시지}\nsPos: {위치}" 형태인 경우 실제 에러 내용 확인
            // "sErr: \nsPos: " 같은 빈 형식은 에러가 아님
            if (!string.IsNullOrWhiteSpace(result.sErrNPos))
            {
                string cleanedError = result.sErrNPos
                    .Replace("sErr:", "")
                    .Replace("sPos:", "")
                    .Replace("\n", "")
                    .Replace("\r", "")
                    .Trim();

                if (!string.IsNullOrEmpty(cleanedError))
                {
                    ErrMsgBox($"고객 검색 실패\n{result.sErrNPos}", "SearchAsync");
                    Debug.WriteLine($"[CustMain_RegistPage] 검색 실패: sErrNPos='{result.sErrNPos}'");
                    return;
                }
                else
                {
                    Debug.WriteLine("[CustMain_RegistPage] sErrNPos에 형식 문자열만 있고 실제 에러 없음 (정상)");
                }
            }

            // 결과 데이터 확인 및 바인딩
            if (result.listTbAll == null || result.listTbAll.Count == 0)
            {
                Debug.WriteLine("[CustMain_RegistPage] 검색 결과 0건");
                listTbAllWith = new List<TbAllWith>();
                VsCustMain_RegistPage.LoadData(s_MainWnd, listTbAllWith);
                this.UpdateDatagridCount();
                return;
            }

            // DataGrid 바인딩 소스 갱신
            listTbAllWith = result.listTbAll;
            VsCustMain_RegistPage.LoadData(s_MainWnd, listTbAllWith);
            this.UpdateDatagridCount();

            Debug.WriteLine($"[CustMain_RegistPage] 검색 완료: {result.listTbAll.Count}건");
        }
        catch (Exception ex)
        {
            ErrMsgBox($"고객 검색 오류\n{StdUtil.GetExceptionMessage(ex)}", "SearchAsync");
            Debug.WriteLine($"[CustMain_RegistPage] 검색 예외: {ex.Message}");
        }
        finally
        {
            NetLoadingWnd.HideLoading();
        }
    }
    #endregion
}
#nullable restore