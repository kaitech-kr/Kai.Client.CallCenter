using Kai.Client.CallCenter.Classes;
using Kai.Client.CallCenter.Classes.Class_Master;
using Kai.Client.CallCenter.MVVM.ViewModels;
using Kai.Client.CallCenter.MVVM.ViewServices;
using Kai.Client.CallCenter.Windows;
using Kai.Common.NetDll_WpfCtrl.NetUtils;
using Kai.Common.NetDll_WpfCtrl.NetWnds;
using Kai.Common.StdDll_Common;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Results;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Services;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using static Kai.Client.CallCenter.Classes.CommonVars;
using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgWnd;
using Wnd = System.Windows;

namespace Kai.Client.CallCenter.Pages;
#nullable disable
public partial class Order_StatusPage : Page
{
    #region Variables
    public static StdEnum_OrderStatus FilterBtnStatus = 0;
    private DispatcherTimer MinuteTimer;
    private bool bInhibitTotBtnEvent = false;
    //public static int s_nUpdateSeqnoForCatch = -1;
    #endregion

    #region Basics
    public Order_StatusPage()
    {
        InitializeComponent();

        // DataGrid의 ItemsSource를 설정
        DGridOrder.ItemsSource = VsOrder_StatusPage.oc_VmOrdersWith;
        DGridTel.ItemsSource = VsOrder_StatusPage.oc_VmOrder_StatusPage_Tel070;

        // Order Seq Watch Timer
        MinuteTimer = new DispatcherTimer();
        MinuteTimer.Interval = TimeSpan.FromMinutes(1);  // ~분마다 실행
        MinuteTimer.Tick += MinuteTimer_Tick;
        MinuteTimer.Start();

        // SignalR - Local Client
        SrLocalClient.SrLocalClient_Tel070_AnswerEvent += SrLocalClient_Tel070_AnswerEvent;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            s_Order_StatusPage = this;

            ////첫윈도 생성시 늦으므로 미리 만든다 - 보류
            //Order_ReceiptWnd wnd = new Order_ReceiptWnd();

            //Load TelRings
            StdResult_Error resultErr = await VsOrder_StatusPage.Tel070_LoadDataAsync();
            if (resultErr != null)
            {
                ErrMsgBox(resultErr.sErr, resultErr.sErrNPos);
            }

            //전체버튼 클릭
            await Dispatcher.InvokeAsync(() => // Dispatcher를 사용해 UI가 완전히 그려진 이후 실행
            {
                TogBtnTotal.IsChecked = true;
            }, DispatcherPriority.Background);  // 또는 DispatcherPriority.Loaded
        }
        finally
        {
            //Load TodayOrder
            BtnOrderSearch.Opacity = (double)Wnd.Application.Current.FindResource("AppOpacity_Enabled");
            BtnOrderSearch_Click(null, null); // 조회버튼 클릭

            NetLoadingWnd.HideLoading();
        }
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        s_Order_StatusPage = null;
        MinuteTimer.Stop();
    }

    // 1분마다 서버 SendingSeq와 로컬 LastSeq 동기화 체크
    private async void MinuteTimer_Tick(object sender, EventArgs e)
    {
        StdResult_Int result = await s_SrGClient.SrResult_Order_SelectSendingSeqOnlyAsync_CenterCode();

        if (result.nResult < 0) // Error
        {
            ErrMsgBox(result.sErr);
            return;
        }

        TBlkMsgSmall.Text = $"[{DateTime.Now}] SendingSeq: This={VsOrder_StatusPage.s_nLastSeq}, DB={result.nResult}";

        if (VsOrder_StatusPage.s_nLastSeq != result.nResult)
        {
            VsOrder_StatusPage.s_nLastSeq = result.nResult;
            Debug.WriteLine($"[Order_StatusPage] SendingSeq 불일치 감지 - Local={VsOrder_StatusPage.s_nLastSeq}, DB={result.nResult}");
            //TODO: 필요시 데이터 재조회 로직 추가
        }
    }
    #endregion

    #region 주요버튼 Events
    // 신규 주문 등록 버튼 (자동배차 일시정지 후 표시 및 재개)
    private void BtnOrderNew_Click(object sender, RoutedEventArgs e)
    {
        // 자동배차 실행 중이면 일시정지
        bool bNeedResume = false;
        var externalAppCtrl = s_MainWnd?.m_MasterManager?.ExternalAppController;

        if (externalAppCtrl?.IsAutoAllocRunning == true)
        {
            externalAppCtrl.PauseAutoAlloc();
            bNeedResume = true;
        }

        // 신규 주문 등록 창 표시
        Order_ReceiptWnd wnd = new Order_ReceiptWnd();
        SafeShowDialog.WithMainWindowToOwner(wnd, s_MainWnd);

        //자동배차 재개
        if (bNeedResume)
        {
            externalAppCtrl.ResumeAutoAlloc();
        }
    }

    // 주문 검색 버튼 클릭
    public async void BtnOrderSearch_Click(object sender, RoutedEventArgs e)
    {
        //MsgBox("BtnOrderSearch_Click"); // Test

        // 1. 날짜 유효성 체크
        if (DatePickerStart.SelectedDate == null || DatePickerEnd.SelectedDate == null)
        {
            ErrMsgBox("검색할 날짜가 없습니다.");
            return;
        }

        DateTime dtStart = (DateTime)DatePickerStart.SelectedDate;
        DateTime dtEnd = (DateTime)DatePickerEnd.SelectedDate;

        // 2. 오늘 주문 검색
        if (dtStart.Date == DateTime.Today && dtEnd.Date == DateTime.Today)
        {
            //2 - 1.중복 검색 확인(이미 검색했으면)
            bool isFirstSearch = BtnOrderSearch.Opacity == (double)Wnd.Application.Current.FindResource("AppOpacity_Enabled");
            if (!isFirstSearch)
            {
                MessageBoxResult resultMsg = Wnd.MessageBox.Show(
                    "조회할 필요가 없는데도 조회 하시겠습니까?", "조회확인", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (resultMsg != MessageBoxResult.Yes) return;
            }

            // 2-2. 오늘 주문 검색
            bool success = await SearchTodayOrdersAsync();
            if (!success) return;

            // 2-3. 첫 검색이면 초기화 작업 (자동배차 시작 등)
            if (isFirstSearch)
            {
                InitializeAfterFirstSearch();
            }
        }
        else //3.범위 주문 검색
        {
            await SearchRangeOrdersAsync(dtStart, dtEnd);
        }
    }
    #endregion

    #region 상태버튼 Events - Checked, Unchecked
    // 접수 버튼
    private async void TogBtnReceipt_Checked(object sender, RoutedEventArgs e)
    {
        FilterBtnStatus |= StdEnum_OrderStatus.접수;

        //전체 버튼
        CheckedTotBtnIfAllBtnChecked();

        //오늘 오더만 버튼에 즉각반응
        if (IsTodaySearchStatus())
            await VsOrder_StatusPage.Order_LoadDataAsync(this, VsOrder_StatusPage.s_listTbOrderToday, FilterBtnStatus);  // 오늘오더만 연결한다.
    }
    private async void TogBtnReceipt_Unchecked(object sender, RoutedEventArgs e)
    {
         FilterBtnStatus &= ~StdEnum_OrderStatus.접수;

        // 전체 버튼
        UncheckedTotBtnIfChecked();

        // 오늘 오더만 버튼에 즉각반응
        if (IsTodaySearchStatus())
            await VsOrder_StatusPage.Order_LoadDataAsync(this, VsOrder_StatusPage.s_listTbOrderToday, FilterBtnStatus);  // 오늘오더만 연결한다.
    }

    // 대기 버튼
    private async void TogBtnWait_Checked(object sender, RoutedEventArgs e)
    {
         FilterBtnStatus |= StdEnum_OrderStatus.대기;

        // 전체 버튼
        CheckedTotBtnIfAllBtnChecked();

        // 오늘 오더만 버튼에 즉각반응
        if (IsTodaySearchStatus())
            await VsOrder_StatusPage.Order_LoadDataAsync(this, VsOrder_StatusPage.s_listTbOrderToday, FilterBtnStatus);  // 오늘오더만 연결한다.
    }
    private async void TogBtnWait_Unchecked(object sender, RoutedEventArgs e)
    {
         FilterBtnStatus &= ~StdEnum_OrderStatus.대기;

        // 전체 버튼
        UncheckedTotBtnIfChecked();

        // 오늘 오더만 버튼에 즉각반응
        if (IsTodaySearchStatus())
            await VsOrder_StatusPage.Order_LoadDataAsync(this, VsOrder_StatusPage.s_listTbOrderToday, FilterBtnStatus);  // 오늘오더만 연결한다.
    }

    // 배차 버튼
    private async void TogBtnAllocate_Checked(object sender, RoutedEventArgs e)
    {
         FilterBtnStatus |= StdEnum_OrderStatus.배차;

        // 전체 버튼
        CheckedTotBtnIfAllBtnChecked();

        // 오늘 오더만 버튼에 즉각반응
        if (IsTodaySearchStatus())
            await VsOrder_StatusPage.Order_LoadDataAsync(this, VsOrder_StatusPage.s_listTbOrderToday, FilterBtnStatus);  // 오늘오더만 연결한다.
    }
    private async void TogBtnAllocate_Unchecked(object sender, RoutedEventArgs e)
    {
         FilterBtnStatus &= ~StdEnum_OrderStatus.배차;

        // 전체 버튼
        UncheckedTotBtnIfChecked();

        // 오늘 오더만 버튼에 즉각반응
        if (IsTodaySearchStatus())
            await VsOrder_StatusPage.Order_LoadDataAsync(this, VsOrder_StatusPage.s_listTbOrderToday, FilterBtnStatus);  // 오늘오더만 연결한다.
    }

    // 예약 버튼
    private async void TogBtnReserve_Checked(object sender, RoutedEventArgs e)
    {
        FilterBtnStatus |= StdEnum_OrderStatus.예약;

        // 전체 버튼
        CheckedTotBtnIfAllBtnChecked();

        // 오늘 오더만 버튼에 즉각반응
        if (IsTodaySearchStatus())
            await VsOrder_StatusPage.Order_LoadDataAsync(this, VsOrder_StatusPage.s_listTbOrderToday, FilterBtnStatus);  // 오늘오더만 연결한다.
    }

    private async void TogBtnReserve_Unchecked(object sender, RoutedEventArgs e)
    {
        FilterBtnStatus &= ~StdEnum_OrderStatus.예약;

        // 전체 버튼
        UncheckedTotBtnIfChecked();

        // 오늘 오더만 버튼에 즉각반응
        if (IsTodaySearchStatus())
            await VsOrder_StatusPage.Order_LoadDataAsync(this, VsOrder_StatusPage.s_listTbOrderToday, FilterBtnStatus);  // 오늘오더만 연결한다.
    }

    // 운행 버튼
    private async void TogBtnRun_Checked(object sender, RoutedEventArgs e)
    {
         FilterBtnStatus |= StdEnum_OrderStatus.운행;

        // 전체 버튼
        CheckedTotBtnIfAllBtnChecked();

        // 오늘 오더만 버튼에 즉각반응
        if (IsTodaySearchStatus())
            await VsOrder_StatusPage.Order_LoadDataAsync(this, VsOrder_StatusPage.s_listTbOrderToday, FilterBtnStatus);  // 오늘오더만 연결한다.
    }
    private async void TogBtnRun_Unchecked(object sender, RoutedEventArgs e)
    {
         FilterBtnStatus &= ~StdEnum_OrderStatus.운행;

        // 전체 버튼
        UncheckedTotBtnIfChecked();

        // 오늘 오더만 버튼에 즉각반응
        if (IsTodaySearchStatus())
            await VsOrder_StatusPage.Order_LoadDataAsync(this, VsOrder_StatusPage.s_listTbOrderToday, FilterBtnStatus);  // 오늘오더만 연결한다.
    }

    // 완료 버튼
    private async void TogBtnFinish_Checked(object sender, RoutedEventArgs e)
    {
         FilterBtnStatus |= StdEnum_OrderStatus.완료;

        // 전체 버튼
        CheckedTotBtnIfAllBtnChecked();

        // 오늘 오더만 버튼에 즉각반응
        if (IsTodaySearchStatus())
            await VsOrder_StatusPage.Order_LoadDataAsync(this, VsOrder_StatusPage.s_listTbOrderToday, FilterBtnStatus);  // 오늘오더만 연결한다.
    }
    private async void TogBtnFinish_Unchecked(object sender, RoutedEventArgs e)
    {
         FilterBtnStatus &= ~StdEnum_OrderStatus.완료;

        // 전체 버튼
        UncheckedTotBtnIfChecked();

        // 오늘 오더만 버튼에 즉각반응
        if (IsTodaySearchStatus())
            await VsOrder_StatusPage.Order_LoadDataAsync(this, VsOrder_StatusPage.s_listTbOrderToday, FilterBtnStatus);  // 오늘오더만 연결한다.
    }

    // 취소 버튼
    private async void TogBtnCancel_Checked(object sender, RoutedEventArgs e)
    {
        FilterBtnStatus |= StdEnum_OrderStatus.취소;

        // 전체 버튼
        CheckedTotBtnIfAllBtnChecked();

        // 오늘 오더만 버튼에 즉각반응
        if (IsTodaySearchStatus())
            await VsOrder_StatusPage.Order_LoadDataAsync(this, VsOrder_StatusPage.s_listTbOrderToday, FilterBtnStatus);  // 오늘오더만 연결한다.
    }
    private async void TogBtnCancel_Unchecked(object sender, RoutedEventArgs e)
    {
        FilterBtnStatus &= ~StdEnum_OrderStatus.취소;

        // 전체 버튼
        UncheckedTotBtnIfChecked();

        // 오늘 오더만 버튼에 즉각반응
        if (IsTodaySearchStatus())
            await VsOrder_StatusPage.Order_LoadDataAsync(this, VsOrder_StatusPage.s_listTbOrderToday, FilterBtnStatus);  // 오늘오더만 연결한다.
    }

    // 전체 버튼
    private async void TogBtnTotal_Checked(object sender, RoutedEventArgs e)
    {
        if (bInhibitTotBtnEvent) return; // 외부에서 실행은 무시.  

        FilterBtnStatus |= StdEnum_OrderStatus.전체;
        TogBtnReceipt.IsChecked = TogBtnWait.IsChecked = TogBtnAllocate.IsChecked = TogBtnReserve.IsChecked =
        TogBtnRun.IsChecked = TogBtnFinish.IsChecked = TogBtnCancel.IsChecked = true;

        // 오늘 오더만 버튼에 즉각반응
        if (IsTodaySearchStatus())
            await VsOrder_StatusPage.Order_LoadDataAsync(this, VsOrder_StatusPage.s_listTbOrderToday, FilterBtnStatus);  // 오늘오더만 연결한다.
    }
    private async void TogBtnTotal_Unchecked(object sender, RoutedEventArgs e)
    {
        if (bInhibitTotBtnEvent) return; // 외부에서 실행은 무시.        

        FilterBtnStatus &= ~StdEnum_OrderStatus.전체;
        TogBtnReceipt.IsChecked = TogBtnWait.IsChecked = TogBtnAllocate.IsChecked = TogBtnReserve.IsChecked =
        TogBtnRun.IsChecked = TogBtnFinish.IsChecked = TogBtnCancel.IsChecked = false;

        // 오늘 오더만 버튼에 즉각반응
        if (IsTodaySearchStatus())
            await VsOrder_StatusPage.Order_LoadDataAsync(this, VsOrder_StatusPage.s_listTbOrderToday, FilterBtnStatus);  // 오늘오더만 연결한다.
    }
    #endregion End Checked, Unchecked - 상태버튼    

    #region ComboBox(DatePicker) Events 
    private void DatePicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        //if (VsOrder_StatusPage.s_listTbOrderToday == null) return;

        //if (DatePickerStart.SelectedDate.Value.Date == DateTime.Today && DatePickerEnd.SelectedDate.Value.Date == DateTime.Today)
        //    BtnOrderSearch.Opacity = (double)Wnd.Application.Current.FindResource("AppOpacity_Disabled");
        //else
        //    BtnOrderSearch.Opacity = (double)Wnd.Application.Current.FindResource("AppOpacity_Enabled");

        //Debug.WriteLine($"VsOrder_StatusPage.s_listTbOrderToday: {VsOrder_StatusPage.s_listTbOrderToday.Count}, {BtnOrderSearch.Opacity}, {DatePickerStart.SelectedDate}, {DatePickerEnd.SelectedDate}"); // Test
    }
    // 시작 날짜 선택 해제 시 - 오늘이 아니면 "직접입력"으로 변경
    private void DatePickerStart_LostFocus(object sender, RoutedEventArgs e)
    {
        if (DatePickerStart.SelectedDate.HasValue && DatePickerStart.SelectedDate.Value.Date != DateTime.Today)
        {
            //CommonFuncs.SetComboBoxItemByContent(CmbBoxDateSelect, "직접입력");
        }
    }

    // 종료 날짜 선택 해제 시 - 오늘이 아니면 "직접입력"으로 변경
    private void DatePickerEnd_LostFocus(object sender, RoutedEventArgs e)
    {
        if (DatePickerEnd.SelectedDate.HasValue && DatePickerEnd.SelectedDate.Value.Date != DateTime.Today)
        {
            //CommonFuncs.SetComboBoxItemByContent(CmbBoxDateSelect, "직접입력");
        }
    }

    private async void CmbBoxDateSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        //var selectedItem = (ComboBoxItem)CmbBoxDateSelect.SelectedItem as ComboBoxItem;
        //DateTime now = DateTime.Now;

        //CultureInfo culture = CultureInfo.CurrentCulture;
        //DayOfWeek firstDayOfWeek = culture.DateTimeFormat.FirstDayOfWeek;
        //int diff = 0, diffStart = 0, diffEnd = 0;

        //switch (selectedItem.Content)
        //{
        //    case "오늘만":
        //        DatePickerStart.SelectedDate = DatePickerEnd.SelectedDate = now;
        //        await VsOrder_StatusPage.Order_LoadDataAsync(this, VsOrder_StatusPage.s_listTbOrderToday, FilterBtnStatus);  // 오늘전용 리스트에서 상태에 따라 로드한다
        //        break;

        //    case "어제만":
        //        DatePickerStart.SelectedDate = DatePickerEnd.SelectedDate = now.AddDays(-1);
        //        break;

        //    case "어제까지":
        //        DatePickerStart.SelectedDate = now.AddDays(-1);
        //        DatePickerEnd.SelectedDate = now;
        //        break;

        //    case "그제만":
        //        DatePickerStart.SelectedDate = DatePickerEnd.SelectedDate = now.AddDays(-2);
        //        break;

        //    case "그제까지":
        //        DatePickerStart.SelectedDate = now.AddDays(-2);
        //        DatePickerEnd.SelectedDate = now;
        //        break;

        //    case "금주만":
        //        diff = (7 + (now.DayOfWeek - DayOfWeek.Sunday)) % 7;
        //        DatePickerStart.SelectedDate = now.AddDays(-diff);
        //        DatePickerEnd.SelectedDate = now;
        //        break;

        //    case "전주만":
        //        diffStart = ((7 + (now.DayOfWeek - DayOfWeek.Sunday)) % 7) + 7;
        //        DatePickerStart.SelectedDate = now.AddDays(-diffStart);
        //        diffEnd = diffStart - 6;
        //        DatePickerEnd.SelectedDate = now.AddDays(-diffEnd);
        //        break;

        //    case "전주까지":
        //        diffStart = ((7 + (now.DayOfWeek - DayOfWeek.Sunday)) % 7) + 7;
        //        DatePickerStart.SelectedDate = now.AddDays(-diffStart);
        //        DatePickerEnd.SelectedDate = now;
        //        break;

        //    case "금월만":
        //        DatePickerStart.SelectedDate = new DateTime(now.Year, now.Month, 1);
        //        DatePickerEnd.SelectedDate = now;
        //        break;

        //    case "전월만":
        //        DatePickerStart.SelectedDate = new DateTime(now.Year, now.Month, 1).AddMonths(-1);
        //        DatePickerEnd.SelectedDate = new DateTime(now.Year, now.Month, 1).AddDays(-1);
        //        break;

        //    case "전월까지":
        //        DatePickerStart.SelectedDate = new DateTime(now.Year, now.Month, 1).AddMonths(-1);
        //        DatePickerEnd.SelectedDate = now;
        //        break;

        //    case "직접입력":
        //        break;
        //}
    }
    #endregion

    #region Datagrid Events
    // LoadingRow 이벤트는 DataGrid의 행이 로드될 때마다 호출됩니다.
    private void DGridOrder_LoadingRow(object sender, DataGridRowEventArgs e) // xmal에서 HeadersVisibility="Column"
    {
         e.Row.Header = (e.Row.GetIndex() + 1).ToString(); // 행 번호 설정
    }
    private void DGridTel_LoadingRow(object sender, DataGridRowEventArgs e) // xmal에서 HeadersVisibility="Column"
    {
         e.Row.Header = (DGridTel.Items.Count - e.Row.GetIndex()).ToString(); // 행 번호 역으로 설정
    }

    // MouseLeftButtonUp
    // 070 전화 목록 클릭 시 - 해당 전화번호로 새 주문 접수
    private async void DGridTelMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        //// 선택된 항목 확인
        //if (DGridTel.SelectedItem == null) return;

        //var selectedRow = (DataGridRow)DGridTel.ItemContainerGenerator.ContainerFromItem(DGridTel.SelectedItem);
        //if (selectedRow == null) return;

        //// 클릭이 Row 영역 내에서 발생했는지 확인
        //Point mousePos = e.GetPosition(selectedRow);
        //Rect bounds = new Rect(0, 0, selectedRow.ActualWidth, selectedRow.ActualHeight);

        //if (!bounds.Contains(mousePos)) return;

        //// 선택된 전화 수신 정보 가져오기
        // if (DGridTel.SelectedItem is not VmOrder_StatusPage_Tel070 selectedTelRing) return;

        //// 전화번호로 새 주문 접수 창 열기
        //string phoneNumber = StdConvert.MakePhoneNumberToDigit(selectedTelRing.YourTelNum);
        //await OpenNewOrderWindowAsync(phoneNumber);
    }

    // DoubleClick
    // 주문 목록 더블클릭 시 - 주문 상세 창 열기
    private async void DGridOrderMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        //// 선택된 항목 확인
        // if (DGridOrder.SelectedItem == null) return;

        //var selectedRow = (DataGridRow)DGridOrder.ItemContainerGenerator.ContainerFromItem(DGridOrder.SelectedItem);
        //if (selectedRow == null) return;

        //// 더블클릭이 Row 영역 내에서 발생했는지 확인
        // Point mousePos = e.GetPosition(selectedRow);
        //Rect bounds = new Rect(0, 0, selectedRow.ActualWidth, selectedRow.ActualHeight);

        // if (!bounds.Contains(mousePos)) return;

        //// 선택된 주문 가져오기
        // if (DGridOrder.SelectedItem is not VmOrder_StatusPage_Order selectedOrder) return;

        //// 주문 상세 창 열기
        // await OpenOrderDetailWindowAsync(selectedOrder.tbOrder);
    }

    // 주문 상세 창 열기 (자동배차 일시정지 처리 포함)
    //private async Task OpenOrderDetailWindowAsync(TbOrder order)
    //{
    //    var externalAppController = s_MainWnd?.m_MasterManager?.ExternalAppController;
    //    bool shouldPauseAutoAlloc = externalAppController != null && externalAppController.IsAutoAllocRunning;

    //    try
    //    {
    //        // 자동배차 실행 중이면 일시정지
    //        if (shouldPauseAutoAlloc)
    //        {
    //            externalAppController.PauseAutoAlloc();
    //            await Task.Delay(100); // 일시정지 처리 대기
    //        }

    //        // 주문 상세 창 열기
    //        Order_ReceiptWnd wnd = new Order_ReceiptWnd(order);
    //        SafeShowDialog.WithMainWindowToOwner(wnd, s_MainWnd);
    //    }
    //    finally
    //    {
    //        // 자동배차 재개
    //        if (shouldPauseAutoAlloc)
    //        {
    //            externalAppController.ResumeAutoAlloc();
    //        }
    //    }
    //}

    // RightButtonDown
    private void DGridOrder_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var point = e.GetPosition(DGridOrder);
        var hit = VisualTreeHelper.HitTest(DGridOrder, point);
        if (hit == null) return;

        DependencyObject obj = hit.VisualHit;

        // 먼저 DataGridCell을 찾는다
        DataGridCell cell = null;
        DependencyObject tmp = obj;
        while (tmp != null && cell == null)
        {
            if (tmp is DataGridCell)
                cell = tmp as DataGridCell;
            else
                tmp = VisualTreeHelper.GetParent(tmp);
        }

        // 컬럼 헤더명 확인
        if (cell == null) return;

        string columnHeader = cell.Column.Header?.ToString();
         string sMenuName = (columnHeader == "기사" || columnHeader == "라이더") ? "DriverContextMenu" : "OrderContextMenu";

        // DataGridRow 추적
        while (obj != null && !(obj is DataGridRow)) obj = VisualTreeHelper.GetParent(obj);

        if (obj is DataGridRow row)
        {
            // 포커스 & 선택 처리
            row.Focus();
            row.IsSelected = true;
            DGridOrder.SelectedItem = row.Item;

            //  ContextMenu 가져오기
            var contextMenu = this.FindResource(sMenuName) as ContextMenu;
            if (contextMenu != null)
            {
                // 이 Row에 붙여서 열기
                row.ContextMenu = contextMenu;
                contextMenu.PlacementTarget = row;
                contextMenu.IsOpen = true;

                e.Handled = true; // 기본 우클릭 동작 막기
            }
        }
    }
    #endregion

    #region Order ContextMenu Events
    // 상태변경 - 접수상태로
    private async void OrderContext_ToReceiptState_Click(object sender, RoutedEventArgs e)
    {
        //if (DGridOrder.SelectedItem is not VmOrder_StatusPage_Order selectedOrder) return;

        //TbOrder tb = selectedOrder.tbOrder;

        //switch (tb.OrderState)
        //{
        //    case "접수":
        //        return; // 이미 접수 상태

        //    case "취소":
        //    case "대기":
        //        //await s_SrGClient.SrMsgBox_OnlyOrderState_UpdateRowAsync_Today(tb, "접수");
        //        break;

        //    default:
        //        WarnMsgBox($"코딩해야하는 Case: {tb.OrderState}", "OrderContext_ToReceiptState_Click");
        //        break;
        //}
    }

    // 상태변경 - 대기상태로
    private async void OrderContext_ToWaitState_Click(object sender, RoutedEventArgs e)
    {
        //if (DGridOrder.SelectedItem is not VmOrder_StatusPage_Order selectedOrder) return;

        //TbOrder tb = selectedOrder.tbOrder;

        //switch (tb.OrderState)
        //{
        //    case "대기":
        //        return; // 이미 대기 상태

        //    case "취소":
        //    case "접수":
        //        //await s_SrGClient.SrMsgBox_OnlyOrderState_UpdateRowAsync_Today(tb, "대기");
        //        break;

        //    default:
        //        WarnMsgBox($"코딩해야하는 Case: {tb.OrderState}", "OrderContext_ToWaitState_Click");
        //        break;
        //}
    }

    // 상태변경 - 취소상태로
    private async void OrderContext_ToCancelState_Click(object sender, RoutedEventArgs e)
    {
        //if (DGridOrder.SelectedItem is not VmOrder_StatusPage_Order selectedOrder) return;

        //TbOrder tb = selectedOrder.tbOrder;

        //switch (tb.OrderState)
        //{
        //    case "취소":
        //        return; // 이미 취소 상태

        //    case "접수":
        //    case "배차":
        //    case "대기":
        //    case "운행":
        //    case "완료":
        //        //await s_SrGClient.SrMsgBox_OnlyOrderState_UpdateRowAsync_Today(tb, "취소");
        //        break;

        //    default:
        //        WarnMsgBox($"코딩해야하는 Case: {tb.OrderState}", "OrderContext_ToCancelState_Click");
        //        break;
        //}
    }

    // 콜복사 - 접수 상태로 새로운 주문 생성
    private async void OrderContext_CopyToReceipt_Click(object sender, RoutedEventArgs e)
    {
        //if (DGridOrder.SelectedItem is not VmOrder_StatusPage_Order selectedOrder) return;

        //TbOrder tb = selectedOrder.tbOrder;
        //TbOrder tbNew = NetUtil.DeepCopyFrom(tb);

        //// Empty Some Data
        //tbNew.KeyCode = 0; // 새 주문이므로 KeyCode 초기화
        //tbNew.Insung1 = "";
        //tbNew.Insung2 = "";
        //tbNew.Cargo24 = "";
        //tbNew.Onecall = "";

        //tbNew.OrderState = "접수";

        ////StdResult_Long result = await s_SrGClient.SrResult_Order_InsertRowAsync_Today(tbNew);
        //StdResult_Long result = new StdResult_Long(1);
        //if (result.lResult <= 0)
        //{
        //    ErrMsgBox($"접수 콜복사 실패\n{result.sErrNPos}", "OrderContext_CopyToReceipt_Click");
        //}
    }

    // 콜복사 - 대기 상태로 새로운 주문 생성
    private async void OrderContext_CopyToWait_Click(object sender, RoutedEventArgs e)
    {
        //if (DGridOrder.SelectedItem is not VmOrder_StatusPage_Order selectedOrder) return;

        //TbOrder tb = selectedOrder.tbOrder;
        //TbOrder tbNew = NetUtil.DeepCopyFrom(tb);

        //// Empty Some Data
        //tbNew.KeyCode = 0; // 새 주문이므로 KeyCode 초기화
        //tbNew.Insung1 = "";
        //tbNew.Insung2 = "";
        //tbNew.Cargo24 = "";
        //tbNew.Onecall = "";

        //tbNew.OrderState = "대기";

        ////StdResult_Long result = await s_SrGClient.SrResult_Order_InsertRowAsync_Today(tbNew);
        //StdResult_Long result = new StdResult_Long(1);
        //if (result.lResult <= 0)
        //{
        //    ErrMsgBox($"대기 콜복사 실패\n{result.sErrNPos}", "OrderContext_CopyToWait_Click");
        //}
    }
    #endregion

    #region Driver ContextMenu Events
    // Driver ContextMenu
    private void DriverContext_CallDriver_Click(object sender, RoutedEventArgs e)
    {
        // MsgBox("기사에게 전화걸기");
    }
    #endregion

    // 테스트용
    private void BtnOrderInit_Click(object sender, RoutedEventArgs e)
    {
        //bool found = VsOrder_StatusPage.Find("", 0);
        //MsgBox($"Result: {DateTime.Today}"); // Debug용
    }

    #region 자작 이벤트
    // 070 전화 응답 이벤트 핸들러 - 전화번호로 새 주문 접수 창 자동 열기
    public async void SrLocalClient_Tel070_AnswerEvent(string telNo)
    {
        if (string.IsNullOrWhiteSpace(telNo)) return;

        string phoneNumber = StdConvert.MakePhoneNumberToDigit(telNo);
        //await OpenNewOrderWindowAsync(phoneNumber);
        MsgBox($"Not Coded: {telNo}");
    }
    #endregion

    #region From Insungs
    // 인성1 접수/배차 → 운행 처리 (40초 경과 시, DB 업데이트 및 타 앱 취소)
    //public async Task<CommonResult_AutoAllocProcess> Insung01배차To운행Async(AutoAllocModel item, CancelTokenControl ctrl)
    //{
    //    try
    //    {
    //        Debug.WriteLine($" ----------------[Insung01배차To운행] 시작 - KeyCode={item.KeyCode}");
    //        Debug.WriteLine($"  ===== 기사 정보 =====");
    //        Debug.WriteLine($"    주문상태: '{item.NewOrder.OrderState}'");
    //        Debug.WriteLine($"    기사번호: '{item.NewOrder.DriverId}'");
    //        Debug.WriteLine($"    기사이름: '{item.NewOrder.DriverName}'");
    //        Debug.WriteLine($"    기사소속: '{item.NewOrder.DriverCenterName}'");
    //        Debug.WriteLine($"    기사전번(원본): '{item.DriverPhone}'");
    //        Debug.WriteLine($"    기사전번(숫자): '{item.NewOrder.DriverTelNo}'");

    //        // 1. Kai DB 업데이트 (접수 → 운행)
    //        Debug.WriteLine($"  → [DB 업데이트] 시작: KeyCode={item.KeyCode}");
    //        Debug.WriteLine($"      OrderState: '{item.NewOrder.OrderState}', DriverTelNo: '{item.NewOrder.DriverTelNo}'");

    //        //StdResult_Int resultUpdate = await CommonVars.s_SrGClient.SrResult_Order_UpdateRowAsync_Today_WithRequestId(item.NewOrder);
    //        StdResult_Int resultUpdate = new StdResult_Int(1);

    //        if (resultUpdate.nResult <= 0 || !string.IsNullOrEmpty(resultUpdate.sErr))
    //        {
    //            Debug.WriteLine($"  → [DB 업데이트 실패] nResult={resultUpdate.nResult}, Err={resultUpdate.sErr}, Pos={resultUpdate.sPos}");
    //            return CommonResult_AutoAllocProcess.FailureAndRetry(
    //                $"Kai DB 업데이트 실패: {resultUpdate.sErr}",
    //                $"Insung01배차To운행_UpdateFail_{resultUpdate.sPos}");
    //        }

    //        Debug.WriteLine($"  → [DB 업데이트 성공] nResult={resultUpdate.nResult}");

    //        // 2. 배차중인 다른 앱 선별
    //        // TODO: item.NewOrder.Insung2, Cargo24, OneCall 필드 확인
    //        Debug.WriteLine($"  → TODO: 배차중인 다른 앱 선별");

    //        // 3. 다른 앱 취소 처리
    //        // TODO: 운행 상태 확인 및 취소 명령
    //        Debug.WriteLine($"  → TODO: 다른 앱 취소 처리");

    //        // 4. NotChanged 상태로 재적재 (다음 루프에서 스킵, SignalR 이벤트 대기)
    //        Debug.WriteLine($"  → [완료] NotChanged 상태로 재적재");
    //        return CommonResult_AutoAllocProcess.SuccessAndReEnqueue(item, PostgService_Common_OrderState.NotChanged);
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.WriteLine($"[Insung01배차To운행] 예외: {ex.Message}");
    //        return CommonResult_AutoAllocProcess.FailureAndRetry($"40초 처리 예외: {ex.Message}", "Insung01배차To운행_Exception");
    //    }
    //}

    // 인성1 운행 → 완료 처리 (DB 업데이트, 타이머 리셋, 큐 제거)
    //public async Task<CommonResult_AutoAllocProcess> Insung01운행To완료Async(AutoAllocModel item, CancelTokenControl ctrl)
    //{
    //    try
    //    {
    //        Debug.WriteLine($" ----------------[Insung01운행To완료] 시작 - KeyCode={item.KeyCode}");
    //        Debug.WriteLine($"  현재 Kai 상태: '{item.NewOrder.OrderState}' → 완료");

    //        // 1. Kai DB 업데이트 (운행 → 완료)
    //        Debug.WriteLine($"  → [DB 업데이트] 시작: KeyCode={item.KeyCode}");

    //        string originalState = item.NewOrder.OrderState;
    //        item.NewOrder.OrderState = "완료";

    //        //StdResult_Int resultUpdate = await CommonVars.s_SrGClient.SrResult_Order_UpdateRowAsync_Today_WithRequestId(item.NewOrder);
    //        StdResult_Int resultUpdate = new StdResult_Int(1);

    //        if (resultUpdate.nResult <= 0 || !string.IsNullOrEmpty(resultUpdate.sErr))
    //        {
    //            Debug.WriteLine($"  → [DB 업데이트 실패] nResult={resultUpdate.nResult}, Err={resultUpdate.sErr}, Pos={resultUpdate.sPos}");
    //            item.NewOrder.OrderState = originalState; // 원복
    //            return CommonResult_AutoAllocProcess.FailureAndRetry(
    //                $"Kai DB 업데이트 실패: {resultUpdate.sErr}",
    //                $"Insung01운행To완료_UpdateFail_{resultUpdate.sPos}");
    //        }

    //        Debug.WriteLine($"  → [DB 업데이트 성공] {originalState} → 완료");

    //        // 2. 타이머 리셋 (혹시 남아있을 경우)
    //        if (item.RunStartTime != null)
    //        {
    //            Debug.WriteLine($"  → 타이머 리셋");
    //            item.RunStartTime = null;
    //        }

    //        // 3. 큐에서 제거 (완료 처리)
    //        Debug.WriteLine($"  → [완료] 큐에서 제거 (완료 처리 성공: {originalState} → 완료)");
    //        return CommonResult_AutoAllocProcess.SuccessAndDestroy(item);
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.WriteLine($"[Insung01운행To완료] 예외: {ex.Message}");
    //        return CommonResult_AutoAllocProcess.FailureAndRetry($"완료 처리 예외: {ex.Message}", "Insung01운행To완료_Exception");
    //    }
    //}

    ////public async Task<StdResult_Error> Insung01접수Or배차To운행Async(TbOrder tb, AutoAlloc kaiCpy, AutoAllocResult_Datagrid dgInfo, CancelTokenControl ctrl)
    ////{
    ////    // 무시용 리스트에 무시할 SeqNo기입하고
    ////    StdResult_Int result = await s_SrGClient.SrResult_Order_SelectSendingSeqOnlyAsync_CenterCode();
    ////    int nNextNum = VsOrder_StatusPage.s_nLastSeq + 1;
    ////    s_SrGClient.m_ListIgnoreSeqno.Add(nNextNum);
    ////    Debug.WriteLine($"무시용 리스트에 무시할 SeqNo기입: {nNextNum} <-> {result.nResult + 1}");

    ////    // Kai를 상태변경 시키고.
    ////     StdResult_Int resultInt = await s_SrGClient.SrResult_Order_UpdateRowAsync_Today(tb);
    ////    if (resultInt.nResult <= 0) return new StdResult_Error(resultInt.sErr + resultInt.sPos, "Insung01접수Or배차To운행Async_01");

    ////    // 외주오더를 완료 처리 (다른 외주사에서 처리됨)
    ////    AutoAllocCtrl.SetOrgFlagFromCopiedItem(AutoAllocCtrl.listForInsung02, PostgService_Common_OrderState.CompletedExternal, kaiCpy); // Insung02
    ////    AutoAllocCtrl.SetOrgFlagFromCopiedItem(AutoAllocCtrl.listForCargo24, PostgService_Common_OrderState.CompletedExternal, kaiCpy); // Cargo24
    ////    AutoAllocCtrl.SetOrgFlagFromCopiedItem(AutoAllocCtrl.listForOnecall, PostgService_Common_OrderState.CompletedExternal, kaiCpy); // Onecall

    ////    return null;
    ////}

    ////public async Task<StdResult_Error> Insung01접수To대기Async(AutoAlloc kaiCopy, InsungsInfo_Mem.OrderBasic isBasic, CancelTokenControl ctrl)
    ////{
    ////    //StdResult_Int resultInt = await s_SrGClient.SrResult_OnlyOrderState_UpdateRowAsync_Today(kaiCopy.TbNewOrder, "대기");
    ////    //if (resultInt.nResult >= 0) return null;
    ////    //else return new StdResult_Error(resultInt.sErr + resultInt.sPos, "Insung01접수To대기Async_01");

    ////    return new StdResult_Error("Insung01접수To대기Async: 코딩해야함.", "Insung01접수To대기Async_700");
    ////}

    ////public async Task<StdResult_Error> Insung01취소To대기Async(AutoAlloc kaiCopy, InsungsInfo_Mem.OrderBasic isBasic, CancelTokenControl ctrl)
    ////{
    ////    //StdResult_Int resultInt = await s_SrGClient.SrResult_OnlyOrderState_UpdateRowAsync_Today(kaiCopy.TbNewOrder, "취소");
    ////    //if (resultInt.nResult >= 0) return null;
    ////    //else return new StdResult_Error(resultInt.sErr + resultInt.sPos, "OrderStateChanged_FromInsung1Async_01");

    ////    return new StdResult_Error("Insung01취소To대기Async: 코딩해야함.", "Insung01취소To대기Async_700");
    ////}
    #endregion From Insungs 끝

    #region From Cargo24
    //public static async Task<StdResult_Status> RegistedCarOrder_FromCargo24Async(TbOrder tb, string sOrderSeq)
    //{
    //    tb.Cargo24 = sOrderSeq;

    //    // Update To Database
    //    StdResult_Bool resultBool = await s_SrGClient.SrResult_OrderToday_UpdateRow(tb);
    //    //Debug.WriteLine($"Update Result: {tb.OrderState}"); // Debug용

    //    // 에러가 발생하면
    //    if (!string.IsNullOrEmpty(resultBool.sErr)) return new StdResult_Status(
    //        StdResult.Retry, new Exception(resultBool.sErr), "Order_StatusPage/RegistedCarOrder_FromCargo24_01"); 

    //    // 통신량 개선여지 있음
    //    if (s_Order_StatusPage != null) s_Order_StatusPage.BtnOrderSearch_Click(null, null); // 조회버튼 클릭

    //    return new StdResult_Status(StdResult.Success);
    //}
    //public static async Task<StdResult_Status> ReceiptToRunStatus_FromCargo24Async(/*기사정보*/)
    //{

    //    return new StdResult_Status(StdResult.Success);
    //}
    //public static async Task<StdResult_Status> ReceiptToCancelStatus_FromCargo24Async(/*기사정보*/)
    //{

    //    return new StdResult_Status(StdResult.Success);
    //}
    #endregion  From Cargo24 끝

    #region 임시 이벤트
    // 선택된 주문을 취소 상태로 변경
    private async void BtnOrderCancel_Click(object sender, RoutedEventArgs e)
    {
        //if (DGridOrder.SelectedItem is not VmOrder_StatusPage_Order selectedOrder)
        //{
        //    WarnMsgBox("취소할 주문을 선택해주세요.", "BtnOrderCancel_Click");
        //    return;
        //}

        //TbOrder tb = selectedOrder.tbOrder;

        //// 이미 취소 상태인 경우
        //if (tb.OrderState == "취소")
        //{
        //    ErrMsgBox("이미 취소된 주문입니다.", "BtnOrderCancel_Click");
        //    return;
        //}

        //// 취소 가능한 상태 확인
        //if (tb.OrderState != "접수" && tb.OrderState != "배차" &&
        //tb.OrderState != "대기" && tb.OrderState != "운행" && tb.OrderState != "완료")
        //{
        //    ErrMsgBox($"취소할 수 없는 상태입니다: {tb.OrderState}", "BtnOrderCancel_Click");
        //    return;
        //}

        //// 주문 취소 처리
        // //await s_SrGClient.SrMsgBox_OnlyOrderState_UpdateRowAsync_Today(tb, "취소");
    }

    private void BtnOrderDriver_Click(object sender, RoutedEventArgs e)
    {

    }

    private void BtnOrderDelay_Click(object sender, RoutedEventArgs e)
    {

    }

    private void BtnOrderShare_Click(object sender, RoutedEventArgs e)
    {

    }

    private void BtnOrderDrvTelCopy_Click(object sender, RoutedEventArgs e)
    {

    }

    private void BtnOrderPastReg_Click(object sender, RoutedEventArgs e)
    {

    }

    private void TBoxCustName_LostFocus(object sender, RoutedEventArgs e)
    {

    }

    private void _TBoxCustName_GotFocus(object sender, RoutedEventArgs e)
    {

    }

    private void TBoxCustTel_LostFocus(object sender, RoutedEventArgs e)
    {

    }

    private void _TBoxCustTel_GotFocus(object sender, RoutedEventArgs e)
    {

    }

    private void TBoxInternalDriver_LostFocus(object sender, RoutedEventArgs e)
    {

    }

    private void _TBoxInternalDriver_GotFocus(object sender, RoutedEventArgs e)
    {

    }

    private void TBoxExternalDriver_LostFocus(object sender, RoutedEventArgs e)
    {

    }

    private void _TBoxExternalDriver_GotFocus(object sender, RoutedEventArgs e)
    {

    }

    private void TBoxStartDong_LostFocus(object sender, RoutedEventArgs e)
    {

    }

    private void _TBoxStartDong_GotFocus(object sender, RoutedEventArgs e)
    {

    }

    private void TBoxEndDong_LostFocus(object sender, RoutedEventArgs e)
    {

    }

    private void _TBoxEndDong_GotFocus(object sender, RoutedEventArgs e)
    {

    }

    private void TBoxRemarks_LostFocus(object sender, RoutedEventArgs e)
    {

    }

    private void _TBoxRemarks_GotFocus(object sender, RoutedEventArgs e)
    {

    }

    private void BtnOrderAlloc_Click(object sender, RoutedEventArgs e)
    {

    }

    private void BtnOrderReceipt_Click(object sender, RoutedEventArgs e)
    {

    }

    private void BtnOrderExcel_Click(object sender, RoutedEventArgs e)
    {

    }

    private void GridMessage_SizeChanged(object sender, SizeChangedEventArgs e)
    {

    }

    private void DGridOrder_SizeChanged(object sender, SizeChangedEventArgs e)
    {

    }

    private void BtnPDA_Click(object sender, RoutedEventArgs e)
    {

    }

    private void BtnDenyAlloc_Click(object sender, RoutedEventArgs e)
    {

    }

    private void BtnDriverState_Click(object sender, RoutedEventArgs e)
    {

    }

    private void BtnAllocState_Click(object sender, RoutedEventArgs e)
    {

    }

    private void BtnShareState_Click(object sender, RoutedEventArgs e)
    {

    }

    #endregion

    #region 1차 Funcs
    private bool IsTodaySearchStatus()
    {
        if (DatePickerStart.SelectedDate == null || DatePickerEnd.SelectedDate == null) return false;

        DateTime dtStart = (DateTime)DatePickerStart.SelectedDate;
        DateTime dtEnd = (DateTime)DatePickerEnd.SelectedDate;

        // 오늘 오더
        return (dtStart.Date == DateTime.Today && dtEnd.Date == DateTime.Today);
    }

    private void CheckedTotBtnIfAllBtnChecked()
    {
        if ((int)FilterBtnStatus >= 127)
        {
            bInhibitTotBtnEvent = true;
            TogBtnTotal.IsChecked = true;
            bInhibitTotBtnEvent = false;
        }
    }

    private void UncheckedTotBtnIfChecked()
    {
        if (TogBtnTotal.IsChecked == true)
        {
            bInhibitTotBtnEvent = true;
            TogBtnTotal.IsChecked = false;
            bInhibitTotBtnEvent = false;
        }
    }

    // 기존 주문을 자동배차 대상으로 등록
    private static void MakeExistedAutoAlloc()
    {
        if (VsOrder_StatusPage.s_listTbOrderToday == null || VsOrder_StatusPage.s_listTbOrderToday.Count == 0)
        {
            Debug.WriteLine("[MakeExistedAutoAlloc] 로드할 기존 주문이 없습니다. ===============>");
            return;
        }

        // ExternalAppController에 기존 주문 목록 전달
        if (s_MainWnd?.m_MasterManager?.ExternalAppController != null)
        {
            s_MainWnd.m_MasterManager.ExternalAppController.LoadExistingOrders(VsOrder_StatusPage.s_listTbOrderToday);
        }
        else
        {
            Debug.WriteLine("[MakeExistedAutoAlloc] ExternalAppController가 초기화되지 않았습니다.");
        }
    }

    // 새 주문 접수 창 열기 (전화번호 지정, 자동배차 일시정지 처리 포함)
    //private async Task OpenNewOrderWindowAsync(string phoneNumber)
    //{
    //    var externalAppController = s_MainWnd?.m_MasterManager?.ExternalAppController;
    //    bool shouldPauseAutoAlloc = externalAppController != null && externalAppController.IsAutoAllocRunning;

    //    try
    //    {
    //        // 자동배차 실행 중이면 일시정지
    //        if (shouldPauseAutoAlloc)
    //        {
    //            externalAppController.PauseAutoAlloc();
    //            await Task.Delay(100); // 일시정지 처리 대기
    //        }

    //        // 전화번호로 새 주문 접수 창 열기
    //        Order_ReceiptWnd wnd = new Order_ReceiptWnd(phoneNumber);
    //        SafeShowDialog.WithMainWindowToOwner(wnd, s_MainWnd);
    //    }
    //    finally
    //    {
    //        // 자동배차 재개
    //        if (shouldPauseAutoAlloc)
    //        {
    //            externalAppController.ResumeAutoAlloc();
    //        }
    //    }
    //}

    #endregion

    #region 2차 Funcs
    // 오늘 주문 검색 및 로드
    private async Task<bool> SearchTodayOrdersAsync()
    {
        PostgResult_TbOrderList result = await s_SrGClient.SrResult_Order_SelectRowsAsync_Today_CenterCode();
        if (!string.IsNullOrEmpty(result.sErr))
        {
            ErrMsgBox($"오늘 주문 조회 실패: {result.sErr}", "Order_StatusPage/SearchTodayOrdersAsync");
            return false;
        }

        if (result.listTb == null) return false;

        VsOrder_StatusPage.s_listTbOrderToday = result.listTb;
        await VsOrder_StatusPage.Order_LoadDataAsync(this, VsOrder_StatusPage.s_listTbOrderToday, FilterBtnStatus);

        return true;
    }

    // 범위 주문 검색 및 로드
    private async Task<bool> SearchRangeOrdersAsync(DateTime dtStart, DateTime dtEnd)
    {
        PostgResult_TbOrderList result =
            await s_SrGClient.SrResult_Order_SelectRowsAsync_CenterCode_Range_OrderStatus(dtStart.Date, dtEnd.Date, FilterBtnStatus);

        if (!string.IsNullOrEmpty(result.sErr))
        {
            ErrMsgBox($"범위 주문 조회 실패: {result.sErr}", "Order_StatusPage/SearchRangeOrdersAsync");
            return false;
        }

        VsOrder_StatusPage.s_listTbOrderMixed = result.listTb;
        await VsOrder_StatusPage.Order_LoadDataAsync(this, VsOrder_StatusPage.s_listTbOrderMixed);

        return true;
    }

    // 첫 검색 후 초기화 작업 (자동배차 시작 등)
    private void InitializeAfterFirstSearch()
    {
        // 버튼 비활성화 (중복 검색 방지)
        BtnOrderSearch.Opacity = (double)Wnd.Application.Current.FindResource("AppOpacity_Disabled");

        // 기존 주문 자동배차 리스트 생성
        MakeExistedAutoAlloc();

        // 자동배차 시작 (Master 모드이고 설정이 켜져 있으면)
        if (s_bAutoAlloc && s_MainWnd?.m_MasterManager?.ExternalAppController != null)
        {
            s_MainWnd.m_MasterManager.ExternalAppController.StartAutoAlloc();
        }
    }
    #endregion

    #region Helper Methods
    // ComboBox의 선택된 인덱스를 스레드 안전하게 가져옵니다
    public static int GetComboBoxSelectedIndex(ComboBox comboBox)
    {
        return System.Windows.Application.Current.Dispatcher.Invoke(() => comboBox.SelectedIndex);
    }

    //public static string GetCarWeightString(string sCarType, string sCarWeight)
    //{
    //    switch (sCarType)
    //    {
    //        case "다마": return "다마";
    //        case "라보": return "라보";
    //        case "트럭":
    //            switch (sCarWeight)
    //            {
    //                case "1t": return "1t";
    //                case "1.4t": return "1.4t";
    //                case "2.5t": return "2.5t";
    //                case "3.5t": return "3.5t";
    //                case "5t": return "5t";
    //                case "8t": return "8t";
    //                case "9.5t": return "9.5t";
    //                case "11t": return "11t";
    //                case "14t": return "14t";
    //                case "15t": return "15t";
    //                case "18t": return "18t";
    //                case "22t": return "22t";
    //                case "25t": return "25t";

    //                default: return "";
    //            }
    //        default: return "";
    //    }
    //}

    //public static string GetTruckDetailString(string sCarType, string sTruckDetail)
    //{
    //    switch (sCarType)
    //    {
    //        case "다마": return "다마";
    //        case "라보": return "라보";
    //        case "트럭":
    //            switch (sTruckDetail)
    //            {
    //                // 다른 텍스트
    //                case "전체": return "전체"; //
    //                case "카고/윙": return "카고/윙"; // 
    //                case "카고": return "카고"; // 
    //                //case "플러스카고": return "플러스카고";
    //                //case "축카고": return "축카고";

    //                //case "플축카고": return "플축카고";
    //                case "리프트카고": return "리프트카고"; // 
    //                //case "플러스리": return "플러스리";
    //                //case "플축리": return "플축리";
    //                case "윙바디": return "윙바디"; // 

    //                case "플러스윙": return "플러스윙"; //
    //                //case "윙축": return "윙축";
    //                //case "플축윙": return "플축윙";
    //                case "리프트윙": return "리프트윙"; // 
    //                //case "플러스윙리": return "플러스윙리";

    //                //case "플축윙리": return "플축윙리";
    //                case "탑": return "탑"; // 
    //                case "리프트탑": return "리프트탑"; // 
    //                case "호루": return "호루"; // 
    //                case "리프트호루": return "리프트호루"; // 

    //                //case "자바라": return "자바라";
    //                //case "리프트자바라": return "리프트자바라";
    //                case "냉동탑": return "냉동탑"; // 
    //                case "냉장탑": return "냉장탑"; // 
    //                //case "냉동윙": return "냉동윙";

    //                case "냉장윙": return "냉장윙"; // 
    //                //case "냉동탑리": return "냉동탑리";
    //                //case "냉장탑리": return "냉장탑리";
    //                case "냉동플축윙": return "냉동플축윙";
    //                case "냉장플축윙": return "냉장플축윙";

    //                case "냉동플축윙리": return "냉동플축윙리";
    //                case "냉장플축윙리": return "냉장플축윙리";
    //                case "평카": return "평카";
    //                case "초장축": return "초장축"; // 
    //                case "무진동": return "무진동"; // 

    //                default: return ""; 
    //            }

    //        default: return "";
    //    }
    //}
    #endregion
}
#nullable enable