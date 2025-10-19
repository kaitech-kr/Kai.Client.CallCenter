using Kai.Client.CallCenter.Classes;
using Kai.Client.CallCenter.MVVM.ViewModels;
using Kai.Client.CallCenter.MVVM.ViewServices;
//using Kai.Client.CallCenter.Networks.NwInsungs;
using Kai.Client.CallCenter.Windows;
using Kai.Common.NetDll_WpfCtrl.NetUtils;
using Kai.Common.NetDll_WpfCtrl.NetWnds;
using Kai.Common.StdDll_Common;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Results;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Services;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using static Kai.Client.CallCenter.Class_Common.CommonVars;
using static Kai.Common.FrmDll_FormCtrl.FormFuncs;
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

        // TmpHide
        //// DataGrid의 ItemsSource를 설정
        //DGridOrder.ItemsSource = VsOrder_StatusPage.oc_VmOrdersWith;
        //DGridTel.ItemsSource = VsOrder_StatusPage.oc_VmOrder_StatusPage_Tel070;

        //// Order Seq Watch Timer
        //MinuteTimer = new DispatcherTimer();
        //MinuteTimer.Interval = TimeSpan.FromMinutes(1);  // ~분마다 실행
        //MinuteTimer.Tick += MinuteTimer_Tick;
        //MinuteTimer.Start();

        //// SignalR - Local Client
        //SrLocalClient.SrLocalClient_Tel070_AnswerEvent += SrLocalClient_Tel070_AnswerEvent;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        // TmpHide
        //try
        //{
        //    s_Order_StatusPage = this;

        //    // 첫윈도 생성시 늦으므로 미리 만든다 - 보류
        //    //Order_ReceiptWnd wnd = new Order_ReceiptWnd();

        //    // Load TelRings
        //    await VsOrder_StatusPage.Tel070_LoadDataAsync();

        //    // 전체버튼 클릭       
        //    await Dispatcher.InvokeAsync(() => // Dispatcher를 사용해 UI가 완전히 그려진 이후 실행
        //    {
        //        TogBtnTotal.IsChecked = true;
        //    }, DispatcherPriority.Background);  // 또는 DispatcherPriority.Loaded
        //}
        //finally
        //{
        //    // Load TodayOrder
        //    BtnOrderSearch.Opacity = (double)Wnd.Application.Current.FindResource("AppOpacity_Enabled");
        //    BtnOrderSearch_Click(null, null); // 조회버튼 클릭

        //    NetLoadingWnd.HideLoading();
        //}
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        // TmpHide
        //s_Order_StatusPage = null;
        //MinuteTimer.Stop();
    }

    //private async void MinuteTimer_Tick(object sender, EventArgs e) // 동기화
    //{
    //    StdResult_Int result = await s_SrGClient.SrResult_Order_SelectSendingSeqOnlyAsync_CenterCode();

    //    if (result.nResult < 0) // Error
    //    {
    //        ErrMsgBox(result.sErr);
    //        return;
    //    }

    //    TBlkMsgSmall.Text = $"[{DateTime.Now}] SendingSeq: This={VsOrder_StatusPage.s_nLastSeq}, DB={result.nResult}";

    //    if (VsOrder_StatusPage.s_nLastSeq == 0) return; // 비교할 필요 없는 상태.
    //    if (VsOrder_StatusPage.s_nLastSeq != result.nResult)
    //    {
    //        ErrMsgBox("코드 보강(VsOrder_StatusPage.s_nLastSeq != result.nResult): 다시 Select해야함.");
    //    }
    //}
    private async void MinuteTimer_Tick(object sender, EventArgs e) // 동기화
    {
        // TmpHide
        //StdResult_Int result = await s_SrGClient.SrResult_Order_SelectSendingSeqOnlyAsync_CenterCode();

        //if (result.nResult < 0) // Error
        //{
        //    ErrMsgBox(result.sErr);
        //    return;
        //}

        //TBlkMsgSmall.Text = $"[{DateTime.Now}] SendingSeq: This={VsOrder_StatusPage.s_nLastSeq}, DB={result.nResult}";

        //if (VsOrder_StatusPage.s_nLastSeq != result.nResult)
        //{
        //    VsOrder_StatusPage.s_nLastSeq = result.nResult;
        //    Debug.WriteLine($"보강 해야함: Local SeqNo={VsOrder_StatusPage.s_nLastSeq}, DbSeqNo={result.nResult}");
        //}
    }
    #endregion

    #region 주요버튼 Events
    //신규버튼
    private async void BtnOrderNew_Click(object sender, RoutedEventArgs e)
    {
        //// 일시정지 상태가 아니면
        //if (!CtrlOtherApps.ctrlCancelToken.IsPaused && CtrlOtherApps.ctrlCancelToken.Token.IsCancellationRequested)
        //{
        //    // Close NetMsgWnd
        //    await CtrlOtherApps.ctrlCancelToken.PauseAsync(); // Pause AutoAlloc

        //    Order_ReceiptWnd wnd = new Order_ReceiptWnd();
        //    SafeShowDialog.WithMainWindowToOwner(wnd, s_MainWnd);

        //    CtrlOtherApps.ctrlCancelToken.Resume(); // Resume AutoAlloc
        //}
        //else
        //{
        //    Order_ReceiptWnd wnd = new Order_ReceiptWnd();
        //    SafeShowDialog.WithMainWindowToOwner(wnd, s_MainWnd);
        //}
    }

    // 조회버튼
    public async void BtnOrderSearch_Click(object sender, RoutedEventArgs e)
    {
        if (DatePickerStart.SelectedDate == null || DatePickerEnd.SelectedDate == null)
        {
            ErrMsgBox("검색할 날짜가 없읍니다.");
            return;
        }

        DateTime dtStart = (DateTime)DatePickerStart.SelectedDate;
        DateTime dtEnd = (DateTime)DatePickerEnd.SelectedDate;

        //// 오늘 오더
        //if (dtStart.Date == DateTime.Today && dtEnd.Date == DateTime.Today)
        //{
        //    if (BtnOrderSearch.Opacity != (double)Wnd.Application.Current.FindResource("AppOpacity_Enabled"))
        //    {
        //        MessageBoxResult resultMsg = Wnd.MessageBox.Show("조회할 필요가 없는데도 조회 하시겠읍니까?", "조회확인", MessageBoxButton.YesNo, MessageBoxImage.Question);
        //        if (resultMsg != MessageBoxResult.Yes) return;
        //    }

        //    PostgResult_TbOrderList result = await s_SrGClient.SrResult_Order_SelectRowsAsync_Today_CenterCode();
        //    if (result.listTb == null) return;

        //    VsOrder_StatusPage.s_listTbOrderToday = result.listTb;
        //    await VsOrder_StatusPage.Order_LoadDataAsync(this, VsOrder_StatusPage.s_listTbOrderToday, FilterBtnStatus);  // 오늘전용 리스트에서 상태에 따라 로드한다

        //    if (BtnOrderSearch.Opacity == (double)Wnd.Application.Current.FindResource("AppOpacity_Enabled")) // 처음 한번만...
        //    {
        //        BtnOrderSearch.Opacity = (double)Wnd.Application.Current.FindResource("AppOpacity_Disabled");
        //        Order_StatusPage.MakeExistedAutoAlloc();

        //        //Thread t = new Thread(async () => // Test
        //        //{
        //        //    for (; ; )
        //        //    {
        //        //        Thread.Sleep(30000);
        //        //        Debug.WriteLine("-------------------------------------- Test ----------------------------------------");

        //        //        foreach (var tb in VsOrder_StatusPage.s_listTbOrderToday)
        //        //        {
        //        //            if (tb.OrderState == "접수")
        //        //            {
        //        //                await s_SrGClient.SrResult_OnlyOrderState_UpdateRowAsync_Today(tb, "대기");
        //        //            }
        //        //            if (tb.OrderState == "대기")
        //        //            {
        //        //                await s_SrGClient.SrResult_OnlyOrderState_UpdateRowAsync_Today(tb, "취소");
        //        //            }
        //        //            else if (tb.OrderState == "취소")
        //        //            {
        //        //                await s_SrGClient.SrResult_OnlyOrderState_UpdateRowAsync_Today(tb, "접수");
        //        //            }

        //        //            //Thread.Sleep(1000);
        //        //        }
        //        //    }
        //        //});
        //        //t.IsBackground = true;
        //        //t.Start();

        //        // 자동배차
        //        if (s_bAutoAlloc) _ = Task.Run(() => CtrlOtherApps.Instance.AutoAllocLoopAsync());
        //        //MsgBox($"NewCount: {VsOrder_StatusPage.s_listTbOrderToday.Count}"); // Test
        //    }
        //}
        //else // 복합 오더
        //{
        //    PostgResult_TbOrderList result =
        //        await s_SrGClient.SrResult_Order_SelectRowsAsync_CenterCode_Range_OrderStatus(dtStart.Date, dtEnd.Date, FilterBtnStatus);
        //    VsOrder_StatusPage.s_listTbOrderMixed = result.listTb;
        //    await VsOrder_StatusPage.Order_LoadDataAsync(this, VsOrder_StatusPage.s_listTbOrderMixed);  // 범위지정 리스트에서 모두로드한다

        //    //MsgBox($"Count: {VsOrder_StatusPage.s_listTbOrderMixed.Count}"); // Test
        //}
    }
    #endregion

    #region 상태버튼 Events - Checked, Unchecked
    // 접수 버튼
    private async void TogBtnReceipt_Checked(object sender, RoutedEventArgs e)
    {
        FilterBtnStatus |= StdEnum_OrderStatus.접수;

        // 전체 버튼
        CheckedTotBtnIfAllBtnChecked();

        // 오늘 오더만 버튼에 즉각반응
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

        //FilterBtnStatus |= StdEnum_OrderStatus.전체;
        TogBtnReceipt.IsChecked = TogBtnWait.IsChecked = TogBtnAllocate.IsChecked = TogBtnReserve.IsChecked =
            TogBtnRun.IsChecked = TogBtnFinish.IsChecked = TogBtnCancel.IsChecked = true;

        // 오늘 오더만 버튼에 즉각반응
        if (IsTodaySearchStatus())
            await VsOrder_StatusPage.Order_LoadDataAsync(this, VsOrder_StatusPage.s_listTbOrderToday, FilterBtnStatus);  // 오늘오더만 연결한다.
    }
    private async void TogBtnTotal_Unchecked(object sender, RoutedEventArgs e)
    {
        if (bInhibitTotBtnEvent) return; // 외부에서 실행은 무시.        

        //FilterBtnStatus &= ~StdEnum_OrderStatus.전체;
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
        if (VsOrder_StatusPage.s_listTbOrderToday == null) return;

        if (DatePickerStart.SelectedDate.Value.Date == DateTime.Today && DatePickerEnd.SelectedDate.Value.Date == DateTime.Today)
            BtnOrderSearch.Opacity = (double)Wnd.Application.Current.FindResource("AppOpacity_Disabled");
        else
            BtnOrderSearch.Opacity = (double)Wnd.Application.Current.FindResource("AppOpacity_Enabled");

        //Debug.WriteLine($"VsOrder_StatusPage.s_listTbOrderToday: {VsOrder_StatusPage.s_listTbOrderToday.Count}, {BtnOrderSearch.Opacity}, {DatePickerStart.SelectedDate}, {DatePickerEnd.SelectedDate}"); // Test
    }
    private void DatePickerStart_LostFocus(object sender, RoutedEventArgs e)
    {
        // TmpHide
        //if (DatePickerStart.SelectedDate != DateTime.Today)
        //    SetComboBoxItemByContent(CmbBoxDateSelect, "직접입력");
    }
    private void DatePickerEnd_LostFocus(object sender, RoutedEventArgs e)
    {
        // TmpHide
        //if (DatePickerEnd.SelectedDate != DateTime.Today)
        //    SetComboBoxItemByContent(CmbBoxDateSelect, "직접입력");
    }

    private async void CmbBoxDateSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selectedItem = (ComboBoxItem)CmbBoxDateSelect.SelectedItem as ComboBoxItem;
        DateTime now = DateTime.Now;

        CultureInfo culture = CultureInfo.CurrentCulture;
        DayOfWeek firstDayOfWeek = culture.DateTimeFormat.FirstDayOfWeek;
        int diff = 0, diffStart = 0, diffEnd = 0;

        switch (selectedItem.Content)
        {
            case "오늘만":
                DatePickerStart.SelectedDate = DatePickerEnd.SelectedDate = now;
                await VsOrder_StatusPage.Order_LoadDataAsync(this, VsOrder_StatusPage.s_listTbOrderToday, FilterBtnStatus);  // 오늘전용 리스트에서 상태에 따라 로드한다
                break;

            case "어제만":
                DatePickerStart.SelectedDate = DatePickerEnd.SelectedDate = now.AddDays(-1);
                break;

            case "어제까지":
                DatePickerStart.SelectedDate = now.AddDays(-1);
                DatePickerEnd.SelectedDate = now;
                break;

            case "그제만":
                DatePickerStart.SelectedDate = DatePickerEnd.SelectedDate = now.AddDays(-2);
                break;

            case "그제까지":
                DatePickerStart.SelectedDate = now.AddDays(-2);
                DatePickerEnd.SelectedDate = now;
                break;

            case "금주만":
                diff = (7 + (now.DayOfWeek - DayOfWeek.Sunday)) % 7;
                DatePickerStart.SelectedDate = now.AddDays(-diff);
                DatePickerEnd.SelectedDate = now;
                break;

            case "전주만":
                diffStart = ((7 + (now.DayOfWeek - DayOfWeek.Sunday)) % 7) + 7;
                DatePickerStart.SelectedDate = now.AddDays(-diffStart);
                diffEnd = diffStart - 6;
                DatePickerEnd.SelectedDate = now.AddDays(-diffEnd);
                break;

            case "전주까지":
                diffStart = ((7 + (now.DayOfWeek - DayOfWeek.Sunday)) % 7) + 7;
                DatePickerStart.SelectedDate = now.AddDays(-diffStart);
                DatePickerEnd.SelectedDate = now;
                break;

            case "금월만":
                DatePickerStart.SelectedDate = new DateTime(now.Year, now.Month, 1);
                DatePickerEnd.SelectedDate = now;
                break;

            case "전월만":
                DatePickerStart.SelectedDate = new DateTime(now.Year, now.Month, 1).AddMonths(-1);
                DatePickerEnd.SelectedDate = new DateTime(now.Year, now.Month, 1).AddDays(-1);
                break;

            case "전월까지":
                DatePickerStart.SelectedDate = new DateTime(now.Year, now.Month, 1).AddMonths(-1);
                DatePickerEnd.SelectedDate = now;
                break;

            case "직접입력":
                break;
        }
        //MsgBox($"CmbBoxDateSelect: {firstDayOfWeek}, {selectedItem.Content}"); // Test
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
    private void DGridTelMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        // TmpHide
        //// 현재 선택된 행 가져오기
        //if (DGridTel.SelectedItem == null) return;

        //var selectedRow = (DataGridRow)DGridTel.ItemContainerGenerator.ContainerFromItem(DGridTel.SelectedItem);
        //if (selectedRow == null) return;

        //// 현재 마우스 위치
        //Point mousePos = e.GetPosition(selectedRow);

        //// 선택된 Row의 렌더링 크기 확인
        //Rect bounds = new Rect(0, 0, selectedRow.ActualWidth, selectedRow.ActualHeight);

        //if (bounds.Contains(mousePos)) // 더블클릭이 SelectedRow 안에서 발생한 경우
        //{
        //    if (DGridTel.SelectedItem is VmOrder_StatusPage_Tel070 selectedTelRing)
        //    {
        //        Order_ReceiptWnd wnd = new Order_ReceiptWnd(StdConvert.MakePhoneNumberToDigit(selectedTelRing.YourTelNum));
        //        SafeShowDialog.WithMainWindowToOwner(wnd, s_MainWnd);
        //    }
        //}
    }

    // DoubleClick
    private async void DGridOrderMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        //// 현재 선택된 행 가져오기
        //if (DGridOrder.SelectedItem == null) return;

        //var selectedRow = (DataGridRow)DGridOrder.ItemContainerGenerator.ContainerFromItem(DGridOrder.SelectedItem);
        //if (selectedRow == null) return;

        //// 현재 마우스 위치
        //Point mousePos = e.GetPosition(selectedRow);

        //// 선택된 Row의 렌더링 크기 확인
        //Rect bounds = new Rect(0, 0, selectedRow.ActualWidth, selectedRow.ActualHeight);

        //if (bounds.Contains(mousePos)) // 더블클릭이 SelectedRow 안에서 발생한 경우
        //{
        //    if (DGridOrder.SelectedItem is VmOrder_StatusPage_Order selectedOrder)
        //    {
        //        // 일시정지 상태가 아니면
        //        if (!CtrlOtherApps.ctrlCancelToken.IsPaused && CtrlOtherApps.ctrlCancelToken.Token.IsCancellationRequested)
        //        {
        //            // Close NetMsgWnd
        //            await CtrlOtherApps.ctrlCancelToken.PauseAsync(); // Pause AutoAlloc

        //            Order_ReceiptWnd wnd = new Order_ReceiptWnd(selectedOrder.tbOrder);
        //            SafeShowDialog.WithMainWindowToOwner(wnd, s_MainWnd);

        //            CtrlOtherApps.ctrlCancelToken.Resume(); // Resume AutoAlloc
        //        }
        //        else
        //        {
        //            Order_ReceiptWnd wnd = new Order_ReceiptWnd(selectedOrder.tbOrder);
        //            SafeShowDialog.WithMainWindowToOwner(wnd, s_MainWnd);
        //        }
        //    }
        //}
    }

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

        // ✅ 컬럼 헤더명 확인
        if (cell == null) return;

        string columnHeader = cell.Column.Header?.ToString();
        string sMenuName = (columnHeader == "기사" || columnHeader == "라이더") ? "DriverContextMenu" : "OrderContextMenu";

        // DataGridRow 추적
        while (obj != null && !(obj is DataGridRow))
            obj = VisualTreeHelper.GetParent(obj);

        if (obj is DataGridRow row)
        {
            // ✅ 포커스 & 선택 처리
            row.Focus();
            row.IsSelected = true;
            DGridOrder.SelectedItem = row.Item;

            // ✅ ContextMenu 가져오기
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
        // TmpHide
        //if (DGridOrder.SelectedItem is VmOrder_StatusPage_Order selectedOrder)
        //{
        //    TbOrder tb = selectedOrder.tbOrder;

        //    switch (tb.OrderState)
        //    {
        //        case "접수": return;

        //        case "취소":
        //        case "대기": await s_SrGClient.SrMsgBox_OnlyOrderState_UpdateRowAsync_Today(tb, "접수"); break; // 접수상태로

        //        default:
        //            WarnMsgBox($"코딩해야하는 Case: {tb.OrderState}", "OrderContext_ToReceiptState_Click_01");
        //            break;
        //    }
        //}
    }
    // 상태변경 - 대기상태로
    private async void OrderContext_ToWaitState_Click(object sender, RoutedEventArgs e)
    {
        // TmpHide
        //if (DGridOrder.SelectedItem is VmOrder_StatusPage_Order selectedOrder)
        //{
        //    TbOrder tb = selectedOrder.tbOrder;

        //    switch (tb.OrderState)
        //    {
        //        case "대기": return;

        //        case "취소":
        //        case "접수": await s_SrGClient.SrMsgBox_OnlyOrderState_UpdateRowAsync_Today(tb, "대기"); break; // 접수상태로

        //        default:
        //            WarnMsgBox($"코딩해야하는 Case: {tb.OrderState}", "OrderContext_ToWaitState_Click_01");
        //            break;
        //    }
        //}
    }
    // 상태변경 - 취소상태로
    private async void OrderContext_ToCancelState_Click(object sender, RoutedEventArgs e)
    {
        // TmpHide
        //if (DGridOrder.SelectedItem is VmOrder_StatusPage_Order selectedOrder)
        //{
        //    TbOrder tb = selectedOrder.tbOrder;

        //    switch (tb.OrderState)
        //    {
        //        case "취소": return;

        //        case "접수":
        //        case "배차":
        //        case "대기":
        //        case "운행":
        //        case "완료": await s_SrGClient.SrMsgBox_OnlyOrderState_UpdateRowAsync_Today(tb, "취소"); break; // 접수상태로

        //        default:
        //            WarnMsgBox($"코딩해야하는 Case: {tb.OrderState}", "OrderContext_ToCancelState_Click_01");
        //            break;
        //    }
        //}
    }

    // 콜복사 - 접수로
    private async void OrderContext_CopyToReceipt_Click(object sender, RoutedEventArgs e)
    {
        // TmpHide
        //await Application.Current.Dispatcher.InvokeAsync(async () =>
        //{
        //    if (DGridOrder.SelectedItem is VmOrder_StatusPage_Order selectedOrder)
        //    {
        //        TbOrder tb = selectedOrder.tbOrder;
        //        TbOrder tbNew = NetUtil.DeepCopyFrom(tb);
        //        tbNew.KeyCode = 0;
        //        tbNew.OrderState = "접수";

        //        StdResult_Long resultLong = await s_SrGClient.SrResult_Order_InsertRowAsync_Today(tbNew);
        //        if (resultLong.lResult <= 0)
        //            MessageBox.Show($"접수 콜복사 실패: {resultLong.sErr}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //});
    }

    // 콜복사 - 대기로
    private void OrderContext_CopyToWait_Click(object sender, RoutedEventArgs e)
    {
        // TmpHide
        //if (DGridOrder.SelectedItem is VmOrder_StatusPage_Order selectedOrder)
        //{
        //    TbOrder tb = selectedOrder.tbOrder;
        //    TbOrder tbNew = NetUtil.DeepCopyFrom(tb);
        //    tbNew.KeyCode = 0;
        //    tbNew.OrderState = "대기";

        //    // 메뉴 먼저 닫히게 하고, 작업은 백그라운드에서 처리
        //    Task.Run(async () =>
        //    {
        //        var resultLong = await s_SrGClient.SrResult_Order_InsertRowAsync_Today(tbNew);

        //        if (resultLong.lResult <= 0)
        //        {
        //            Application.Current.Dispatcher.Invoke(() =>
        //            {
        //                MessageBox.Show($"대기 콜복사 실패: {resultLong.sErr}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        //            });
        //        }
        //    });
        //}
    }

    #endregion

    #region Driver ContextMenu Events
    // Driver ContextMenu
    private void DriverContext_CallDriver_Click(object sender, RoutedEventArgs e)
    {
        MsgBox("기사에게 전화걸기");
    }
    #endregion

    // 테스트용
    private void BtnOrderInit_Click(object sender, RoutedEventArgs e)
    {
        //bool found = VsOrder_StatusPage.Find("", 0);
        MsgBox($"Result: {DateTime.Today}"); // Debug용
        
    }

    #region 자작 이벤트
    public void SrLocalClient_Tel070_AnswerEvent(string telNo)
    {
        // TmpHide
        //Order_ReceiptWnd wnd = new Order_ReceiptWnd(telNo);
        //SafeShowDialog.WithMainWindowToOwner(wnd, s_MainWnd);
    }
    #endregion

    #region From Insungs 
    //public async Task<StdResult_Error> Insung01접수Or배차To운행Async(TbOrder tb, AutoAlloc kaiCpy, AutoAllocResult_Datagrid dgInfo, CancelTokenControl ctrl)
    //{
    //    // 무시용 리스트에 무시할 SeqNo기입하고
    //    StdResult_Int result = await s_SrGClient.SrResult_Order_SelectSendingSeqOnlyAsync_CenterCode();
    //    int nNextNum = VsOrder_StatusPage.s_nLastSeq + 1;
    //    s_SrGClient.m_ListIgnoreSeqno.Add(nNextNum);
    //    Debug.WriteLine($"무시용 리스트에 무시할 SeqNo기입: {nNextNum} <-> {result.nResult + 1}");

    //    // Kai를 상태변경 시키고.
    //     StdResult_Int resultInt = await s_SrGClient.SrResult_Order_UpdateRowAsync_Today(tb);
    //    if (resultInt.nResult <= 0) return new StdResult_Error(resultInt.sErr + resultInt.sPos, "Insung01접수Or배차To운행Async_01");

    //    // 외주오더를 취소시키고, 리스트에서 삭제
    //    AutoAllocCtrl.SetOrgFlagFromCopiedItem(AutoAllocCtrl.listForInsung02, PostgService_Common_OrderState.Change_ToCancel_DoDelete, kaiCpy); // Insung02
    //    AutoAllocCtrl.SetOrgFlagFromCopiedItem(AutoAllocCtrl.listForCargo24, PostgService_Common_OrderState.Change_ToCancel_DoDelete, kaiCpy); // Cargo24
    //    AutoAllocCtrl.SetOrgFlagFromCopiedItem(AutoAllocCtrl.listForOnecall, PostgService_Common_OrderState.Change_ToCancel_DoDelete, kaiCpy); // Onecall

    //    return null;
    //}

    //public async Task<StdResult_Error> Insung01접수To대기Async(AutoAlloc kaiCopy, InsungsInfo_Mem.OrderBasic isBasic, CancelTokenControl ctrl)
    //{
    //    //StdResult_Int resultInt = await s_SrGClient.SrResult_OnlyOrderState_UpdateRowAsync_Today(kaiCopy.TbNewOrder, "대기");
    //    //if (resultInt.nResult >= 0) return null;
    //    //else return new StdResult_Error(resultInt.sErr + resultInt.sPos, "Insung01접수To대기Async_01");

    //    return new StdResult_Error("Insung01접수To대기Async: 코딩해야함.", "Insung01접수To대기Async_700");
    //}

    //public async Task<StdResult_Error> Insung01취소To대기Async(AutoAlloc kaiCopy, InsungsInfo_Mem.OrderBasic isBasic, CancelTokenControl ctrl)
    //{
    //    //StdResult_Int resultInt = await s_SrGClient.SrResult_OnlyOrderState_UpdateRowAsync_Today(kaiCopy.TbNewOrder, "취소");
    //    //if (resultInt.nResult >= 0) return null;
    //    //else return new StdResult_Error(resultInt.sErr + resultInt.sPos, "OrderStateChanged_FromInsung1Async_01");

    //    return new StdResult_Error("Insung01취소To대기Async: 코딩해야함.", "Insung01취소To대기Async_700");
    //}
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
    private async void BtnOrderCancel_Click(object sender, RoutedEventArgs e)
    {
        // TmpHide
        //StdResult_Int result = await s_SrGClient.SrResult_Order_SelectSendingSeqOnlyAsync_CenterCode();
        //MsgBox($"SrResult_Order_SelectSendingSeqOnly: {result}");
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

    //private static void MakeExistedAutoAlloc()
    //{
    //    foreach (TbOrder tb in VsOrder_StatusPage.s_listTbOrderToday)
    //    {
    //        //AddAutoAlloc(PostgService_Common_OrderState.None, tb);
    //        AutoAllocCtrl.AppendToList_WithExisted(tb);
    //    }
    //}
    #endregion

    #region 2차 Funcs
    #endregion


}
#nullable enable