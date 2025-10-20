using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Text.RegularExpressions;

using Kai.Common.StdDll_Common;
using Kai.Common.NetDll_WpfCtrl.NetWnds;
using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgBox;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Results;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Services;

using Kai.Client.CallCenter.Classes;
using static Kai.Client.CallCenter.Class_Common.CommonVars;
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
    private void DGridCustMouseDoubleClick(object sender, MouseButtonEventArgs e) // 고객수정
    {
        // TmpHide
        //if (DGridCustMain.SelectedItem is VmCustMain_RegistPage selectedItem)
        //{
        //    CustMain_RegEditWnd custMain_EditWnd = new CustMain_RegEditWnd(selectedItem.tbAllWith);
        //    bool result = (bool)SafeShowDialog.WithMainWindowToOwner(custMain_EditWnd, s_MainWnd);
        //    if (result == true)
        //    {
        //        PostgService_TbCustMain.CopyTo(custMain_EditWnd.tbAllWithNew.custMain, selectedItem.tbAllWith.custMain);
        //        int diff = PostgService_TbCustMain.GetDiffrentPos(custMain_EditWnd.tbAllWithNew.custMain, selectedItem.tbAllWith.custMain, true);
        //        if (diff != 0)
        //        {
        //            ErrMsgBox($"복사한 테이블이 내용이 다릅니다: {diff}");
        //            return;
        //        }

        //        // 수정된 데이터로 갱신 - 더쉬운 방법이 없을까?
        //        int index = DGridCustMain.SelectedIndex;
        //        if (index >= 0)
        //        {
        //            VsCustMain_RegistPage.oc_VmCustMainForPage[index] = new VmCustMain_RegistPage(selectedItem.tbAllWith);
        //        }
        //    }
        //}
    }
    private void DGridCust_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // TmpHide
        //ButtonEnable(BtnDelete, DGridCustMain.SelectedIndex >= 0);
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

    // 신규 고객
    private async void BtnNewCust_Click(object sender, RoutedEventArgs e)
    {
        // TmpHide
        //CustMain_RegEditWnd wnd = new CustMain_RegEditWnd();
        //bool result = (bool)SafeShowDialog.WithMainWindowToOwner(wnd, s_MainWnd);
        //if (!result) return;

        //// 다시 조회
        //await SearchAsync();

        //VmCustMain_RegistPage vm = VsCustMain_RegistPage.oc_VmCustMainForPage.FirstOrDefault(x => x.KeyCode == wnd.tbAllWithNew.custMain.KeyCode);
        //int index = VsCustMain_RegistPage.GetIndexByKeyCode(wnd.tbAllWithNew.custMain.KeyCode);
        //DGridCustMain.SelectedIndex = index;
        ////MsgBox($"KeyCode = {wnd.m_TbCustMainNew.KeyCode}, {DGridCustMain.SelectedIndex}"); // Test
    }

    // 조회버튼
    private async void BtnSearch_Click(object sender, RoutedEventArgs e)
    {
        // TmpHide
        //await SearchAsync();
    }

    // 엑셀
    private void BtnExcel_Click(object sender, RoutedEventArgs e)
    {

    }

    // 닫기버튼
    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        // TmpHide
        //if (s_MainWnd != null)
        //{
        //    s_MainWnd.RemoveTab(s_MainWnd.Customer_CustRegistTab);
        //}
    }
    #endregion

    #region Click - Etc Buttons
    // 검색내 조회
    private void BtnSearchInFind_Click(object sender, RoutedEventArgs e)
    {
        // TmpHide
        //if (listTbAllWith.Count == 0) return;

        //try
        //{
        //    NetLoadingWnd.ShowLoading(s_MainWnd, "검색내에서 필터링 중입니다..."); // 로딩창 표시

        //    bool? use = GetUsingType();
        //    VsCustMain_RegistPage.LoadData(s_MainWnd, listTbAllWith,
        //        use, TBoxCustName.Text, TBoxDeptName.Text, TBoxChargeName.Text, TBoxTelNo.Text, TBoxDongDetail.Text, TBoxInternetID.Text, TBoxCompName.Text);
        //    this.UpdateDatagridCount();
        //}
        //finally
        //{
        //    NetLoadingWnd.HideLoading();
        //}
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

    //private async Task SearchAsync()
    //{
    //    PostgResult_AllWithList result = null;

    //    try
    //    {
    //        bool? use = GetUsingType();

    //        if (ChBoxTotal.IsChecked == true) // 전체 검색
    //        {
    //            NetLoadingWnd.ShowLoading(s_MainWnd, "고객정보를 로딩중입니다...");
    //            result = await s_SrGClient.SrResult_CustMainWith_SelectRowsAsync_CenterCode_Using(use);
    //        }
    //        else // 부분 검색
    //        {
    //            if (!CanSearch())
    //            {
    //                ErrMsgBox("고객 검색조건이 없읍니다.");
    //                return; // ⬅ 검색 중단
    //            }

    //            NetLoadingWnd.ShowLoading(s_MainWnd, "고객정보를 로딩중입니다...");
    //            result = await s_SrGClient.SrResult_CustMainWith_SelectRowsAsync_CenterCode_CustNameWith11(
    //                use,
    //                TBoxCustName.Text,
    //                TBoxDeptName.Text,
    //                TBoxChargeName.Text,
    //                TBoxTelNo.Text,
    //                TBoxDongDetail.Text,
    //                TBoxInternetID.Text,
    //                TBoxCompName.Text);

    //            // TODO: 보완사항 유지
    //            // MsgBox("보완사항: TBoxCompName을 Key로 변경해야함");
    //        }

    //        // 결과 검증
    //        if (result == null)
    //        {
    //            ErrMsgBox("검색 결과가 없습니다.");
    //            return;
    //        }

    //        if (!string.IsNullOrEmpty(result.sErr))
    //        {
    //            ErrMsgBox($"고객검색에러: {result.sErr}");
    //            return;
    //        }

    //        // DataGrid 바인딩 소스 갱신
    //        listTbAllWith = result.listTbAll;
    //        VsCustMain_RegistPage.LoadData(s_MainWnd, listTbAllWith);
    //        this.UpdateDatagridCount();
    //    }
    //    catch (Exception ex)
    //    {
    //        ErrMsgBox($"고객검색에러: {StdUtil.GetExceptionMessage(ex)}");
    //    }
    //    finally
    //    {
    //        NetLoadingWnd.HideLoading();
    //    }
    //}
    #endregion
}
#nullable restore