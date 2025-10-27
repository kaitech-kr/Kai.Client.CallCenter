using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using Kai.Common.StdDll_Common;
using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgBox;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Results;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Services;

using Kai.Client.CallCenter.MVVM.ViewModels;
using Kai.Client.CallCenter.MVVM.ViewServices;
using static Kai.Client.CallCenter.Classes.CommonVars;

namespace Kai.Client.CallCenter.Windows;
#nullable disable
public partial class CustMain_SearchedWnd : Window
{
    #region Variables
    private List<TbAllWith> m_ListTbAllWith = null;
    public VmCustMain_SearchedWnd SelectedVM = null;
    #endregion

    #region Basics
    public CustMain_SearchedWnd(List<TbAllWith> listTbAllWith)
    {
        InitializeComponent();

        m_ListTbAllWith = listTbAllWith;
        TBlkCount.Text = "합계: 0"; // 검색된 고객 수 표시
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // WindowStartupLocation = WindowStartupLocation.CenterOwner;
        // VsCustMain_SearchedWnd.LoadData(this, m_ListTbAllWith, (bool)ChkBoxWithNoUsing.IsChecked, TBlkCount);

        // 0번 Row에 포커스 주기
        // if (DGridCust.Items.Count > 0)
        // {
        // DGridCust.SelectedIndex = 0; // 첫 행 선택
        // DGridCust.UpdateLayout(); // (중요) 시각적 요소 갱신

        // var row = (DataGridRow)DGridCust.ItemContainerGenerator.ContainerFromIndex(0);
        // if (row != null)
        // row.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            // 또는 row.Focus();
        // }
    }

    private void Window_Unloaded(object sender, RoutedEventArgs e)
    {
    }
    #endregion

    #region Events
    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        // this.Close();
    }

    private void ChkBoxWithNoUsing_Click(object sender, RoutedEventArgs e)
    {
        // VsCustMain_SearchedWnd.LoadData(this, m_ListTbAllWith, (bool)ChkBoxWithNoUsing.IsChecked, TBlkCount);
    }
    private void DGridCust_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // if (e.Key == Key.Enter)
        // {
            // 엔터키 눌렀을 때 처리
        // DGridCustMouseDoubleClick(null, null);
        // e.Handled = true; // 필요하면 이벤트 전파 중단
        // }
    }
    #endregion

    #region Datagrid Events
    // LoadingRow
    private void DGridCust_LoadingRow(object sender, System.Windows.Controls.DataGridRowEventArgs e) // xmal에서 HeadersVisibility="Column"
    {
        // e.Row.Header = (e.Row.GetIndex() + 1).ToString(); // 행 번호 설정
    }

    // MouseDoubleClick
    private void DGridCustMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // if (DGridCust.SelectedItem is VmCustMain_SearchedWnd selectedItem)
        // {
        // SelectedVM = selectedItem;
            //MsgBox($"{SelectedVM.TbCustMain.CustName}"); // Test

        // this.DialogResult = true;
        // this.Close();
        // }
    }

    /// <summary>
    /// DataGrid 우클릭 - 컨텍스트 메뉴 표시 (삭제 기능)
    /// </summary>
    private void DGridCustMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        // 클릭한 Row 찾기
        // DependencyObject dep = (DependencyObject)e.OriginalSource;
        // while (dep != null && !(dep is DataGridRow))
        // {
        // dep = VisualTreeHelper.GetParent(dep);
        // }

        // if (dep is DataGridRow row)
        // {
        // row.IsSelected = true;
        // row.Focus();

            // Row에 바인딩된 ViewModel 가져오기
        // if (row.DataContext is VmCustMain_SearchedWnd selectedItem)
        // {
        // SelectedVM = selectedItem; // 필드/프로퍼티에 저장
        // }

            // ContextMenu 생성
        // ContextMenu menu = new ContextMenu();

        // MenuItem itemDelete = new MenuItem();
        // itemDelete.Header = "삭제";
        // itemDelete.Click += async (s, args) =>
        // {
        // if (SelectedVM == null)
        // {
        // Debug.WriteLine("[CustMain_SearchedWnd] 우클릭 삭제: SelectedVM이 null입니다");
        // return;
        // }

        // MessageBoxResult resultDlg = MessageBox.Show(
        // $"삭제 확인: {SelectedVM.CustName}",
        // "삭제 확인",
        // MessageBoxButton.OKCancel,
        // MessageBoxImage.Warning);

        // if (resultDlg != MessageBoxResult.OK)
        // {
        // Debug.WriteLine($"[CustMain_SearchedWnd] 삭제 취소: {SelectedVM.CustName}");
        // return;
        // }

        // Debug.WriteLine($"[CustMain_SearchedWnd] 고객 삭제 요청: KeyCode={SelectedVM.tbCustMain.KeyCode}, CustName={SelectedVM.CustName}");

        // StdResult_Long resultLong = await s_SrGClient.SrResult_CustMain_MoveToDeletedAsync(SelectedVM.tbCustMain);
        // if (resultLong.lResult <= 0)
        // {
        // ErrMsgBox($"고객 정보 이동 실패\n{resultLong.sErrNPos}", "DGridCustMouseRightButtonDown");
        // Debug.WriteLine($"[CustMain_SearchedWnd] 삭제 실패: sErrNPos={resultLong.sErrNPos}");
        // return;
        // }

        // int nResult = m_ListTbAllWith.RemoveAll(x => x.custMain.KeyCode == resultLong.lResult);
        // VsCustMain_SearchedWnd.LoadData(this, m_ListTbAllWith, (bool)ChkBoxWithNoUsing.IsChecked, TBlkCount);

        // if (nResult > 0)
        // {
        // MsgBox("삭제하였습니다.");
        // Debug.WriteLine($"[CustMain_SearchedWnd] 고객 삭제 완료: KeyCode={resultLong.lResult}, 제거된 항목 수={nResult}");
        // }
        // };
        // menu.Items.Add(itemDelete);

            // 현재 클릭 위치에 ContextMenu 표시
        // menu.IsOpen = true;
        // }
    }

    #endregion
}
#nullable restore
