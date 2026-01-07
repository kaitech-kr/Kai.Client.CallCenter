// Xceed Extended WPF Toolkit Nuget - For DateTimePicker(3.8.1 만 무료)

using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;

using Kai.Common.StdDll_Common;
// using Kai.Common.FrmDll_FormCtrl;
using Kai.Common.NetDll_WpfCtrl.NetUtils;
using Kai.Common.NetDll_WpfCtrl.NetWnds;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Results;

using static Kai.Client.CallCenter.Classes.CommonVars;
using static Kai.Client.CallCenter.Classes.CommonFuncs;
using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgBox;
using System.Diagnostics;
using Kai.Client.CallCenter.Classes;
using Kai.Client.CallCenter.MVVM.ViewModels;

namespace Kai.Client.CallCenter.Windows;
#nullable disable
public partial class Order_ReceiptWnd : Window
{
    #region Variables
    private VmOrder_StatusPage_Order vmOrder = null;  // ViewModel (UI와 바인딩)

    public int FeeBasic = 0; // 기본요금
    public int FeePlus = 0; // 추가요금
    public int FeeMinus = 0; // 할인요금
    public int FeeConn = 0; // 탁송요금
    public int FeeTot = 0;
    public int FeeDrv = 0; // 기사금액

    private string sStatus_OrderSave = "";
    #endregion

    #region Basics
    public Order_ReceiptWnd(TbOrder tbOrder = null) // 테이블 유: 수정, 무: 신규
    {
        InitializeComponent();

        TBlkRegister.Text = s_CenterCharge.Id;
        TBlkDrNow.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // 모드에 따라 버튼그룹 보이기
        if (tbOrder == null) // 신규 등록 모드
        {
            vmOrder = new VmOrder_StatusPage_Order(); // 빈 ViewModel 생성
            ColumnRegist.Visibility = Visibility.Visible;
            ColumnModify.Visibility = Visibility.Collapsed;
        }
        else // 수정 모드 - 고객정보도 같이 와야함
        {
            vmOrder = new VmOrder_StatusPage_Order(tbOrder); // 기존 데이터로 ViewModel 생성
            ColumnRegist.Visibility = Visibility.Collapsed;
            ColumnModify.Visibility = Visibility.Visible;
        }

        // DataContext 설정 (XAML 바인딩용 - 전체 Window)
        this.DataContext = vmOrder;
    }

    public Order_ReceiptWnd(string sTelNo) // 전화에 의한 오더등록
    {
        InitializeComponent();

        TBoxHeader_Search.Text = sTelNo;

        // 신규 등록 모드
        vmOrder = new VmOrder_StatusPage_Order(); // 빈 ViewModel 생성
        ColumnRegist.Visibility = Visibility.Visible;
        ColumnModify.Visibility = Visibility.Collapsed;

        // DataContext 설정 (XAML 바인딩용 - 전체 Window)
        this.DataContext = vmOrder;

        ChkBox_편도.IsChecked = true;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        TBoxHeader_Search.Focus();

        if (vmOrder.KeyCode > 0) // 업데이트 모드면 오더정보 로드 (KeyCode가 있으면 기존 오더)
        {
            TbOrderOrgToUiData();
            의뢰자정보Mode(vmOrder.CallCustCodeK);
        }
        else // 신규 등록 모드
        {
            if (string.IsNullOrEmpty(TBoxHeader_Search.Text))
            {
                의뢰자정보Mode(0);
            }
            else
            {
                var ee = new KeyEventArgs(
                    Keyboard.PrimaryDevice,
                    PresentationSource.FromVisual(TBoxHeader_Search),
                    0,
                    Key.Enter)
                {
                    RoutedEvent = Keyboard.KeyDownEvent
                };
                TBoxHeader_Search_KeyDown(TBoxHeader_Search, ee);
            }
        }

    }

    private void Window_Unloaded(object sender, RoutedEventArgs e)
    {
    }

    // 퀵타입 선택 (오토, 밴, 플렉스)
    private void RadioBtn_QuickType_Checked(object sender, RoutedEventArgs e)
    {
        if (GridQuickInfo == null || GridCargoInfo == null) return;
        GridQuickInfo.Visibility = Visibility.Visible;
        GridCargoInfo.Visibility = Visibility.Collapsed;
        if (ChkBox_혼적 != null) ChkBox_혼적.IsEnabled = false;
    }

    // 화물타입 선택 (다마스, 라보, 트럭)
    private void RadioBtn_CargoType_Checked(object sender, RoutedEventArgs e)
    {
        if (GridQuickInfo == null || GridCargoInfo == null) return;
        GridQuickInfo.Visibility = Visibility.Collapsed;
        GridCargoInfo.Visibility = Visibility.Visible;
        if (ChkBox_혼적 != null) ChkBox_혼적.IsEnabled = true;
    }
    #endregion

    #region 주버튼그룹
    #region 신규등록용 버튼들
    // 접수저장
    private async void BtnReg_SaveReceipt_Click(object sender, RoutedEventArgs e)
    {
        bool success = await SaveOrderAsync("접수", "접수저장");
        if (success)
        {
            DialogResult = true;
            Close();
        }
    }

    // 대기저장
    private async void BtnReg_SaveWait_Click(object sender, RoutedEventArgs e)
    {
        bool success = await SaveOrderAsync("대기", "대기저장");
        if (success)
        {
            DialogResult = true;
            Close();
        }
    }
    #endregion 신규등록용 버튼들 - 끝

    #region 업데이트용 버튼들
    // 배차
    private void BtnMod_Allocation_Click(object sender, RoutedEventArgs e)
    {

    }

    // 처리완료
    private void BtnMod_Finish_Click(object sender, RoutedEventArgs e)
    {

    }

    // 대기
    private void BtnMod_Wait_Click(object sender, RoutedEventArgs e)
    {
        if (vmOrder != null) vmOrder.OrderState = "대기";
    }

    // 주문취소
    private void BtnMod_Cancel_Click(object sender, RoutedEventArgs e)
    {
        if (vmOrder != null) vmOrder.OrderState = "취소";
    }

    // 접수상태
    private void BtnMod_Receipt_Click(object sender, RoutedEventArgs e)
    {
        if (vmOrder != null) vmOrder.OrderState = "접수";
    }

    // 저장
    private async void BtnMod_Save_Click(object sender, RoutedEventArgs e)
    {
        bool success = await UpdateOrderAsync();
        if (success)
        {
            DialogResult = true;
            Close();
        }
    }
    #endregion 업데이트용 버튼들 - 끝

    #region 공용 버튼들
    // 고객등록
    private void BtnReg_CustRegist_Click(object sender, RoutedEventArgs e)
    {

    }

    // 고객수정 버튼
    private void BtnCommon_CustUpdate_Click(object sender, RoutedEventArgs e)
    {
        if (vmOrder.CallCustCodeK == 0) return; // 의뢰자 정보가 없으면 리턴

        CustMain_RegEditWnd wnd = new CustMain_RegEditWnd(vmOrder.CallCustCodeK);
        bool result = (bool)SafeShowDialog.WithMainWindowToOwner(wnd, this);
        if (!result) return;

        // Table To 의뢰자
        TbAllTo의뢰자(wnd.tbAllWithNew);
    }

    // 요금저장
    private void BtnCommon_SaveFee_Click(object sender, RoutedEventArgs e)
    {

    }

    // 거래처요금
    private void BtnCommon_CompFee_Click(object sender, RoutedEventArgs e)
    {

    }

    // 오더복사
    private void BtnCommon_CopyOrder_Click(object sender, RoutedEventArgs e)
    {

    }

    // 마일리지
    private void BtnCommon_Mileage_Click(object sender, RoutedEventArgs e)
    {

    }

    // 변경이력
    private void BtnCommon_ChangeHistory_Click(object sender, RoutedEventArgs e)
    {

    }

    // 닫기
    private void BtnCommon_Close_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
    #endregion 공용 버튼들 - 끝

    #endregion 주버튼그룹 - 전부 끝

    #region 헤더
    // 수신거부
    private void BtnNoRcvTel_Click(object sender, RoutedEventArgs e)
    {

    }

    // 의뢰자 검색
    private async void TBoxHeader_Search_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (string.IsNullOrEmpty(TBoxHeader_Search.Text)) return;

        PostgResult_AllWithList result = await s_SrGClient.SrResult_CustMainWith_SelectRowsAsync_BySlash(TBoxHeader_Search.Text, null);
        if (!string.IsNullOrEmpty(result.sErr))
        {
            ErrMsgBox(result.sErr, "의뢰자검색");
            return;
        }

        if (result.listTbAll.Count == 0) // 없음 - 고객등록
        {
            CustMain_RegEditWnd wnd = new CustMain_RegEditWnd();
            if (SafeShowDialog.WithMainWindowToOwner(wnd, this) == true)
            {
                TbAllTo의뢰자(wnd.tbAllWithNew);
                의뢰자정보Mode(wnd.tbAllWithNew.custMain.KeyCode);
                Keyboard.Focus(Start_TBoxSearch);
            }
        }
        else if (result.listTbAll.Count == 1) // 1개 - 무조건 선택
        {
            TbAllTo의뢰자(result.listTbAll[0]);
            의뢰자정보Mode(result.listTbAll[0].custMain.KeyCode);
            Keyboard.Focus(Start_TBoxSearch);
        }
        else // 여러개 - 하나 선택하기
        {
            CustMain_SearchedWnd wnd = new CustMain_SearchedWnd(result.listTbAll);
            if (SafeShowDialog.WithMainWindowToOwner(wnd, this) == true)
            {
                TbAllTo의뢰자(wnd.SelectedVM.tbAllWith);
                의뢰자정보Mode(wnd.SelectedVM.tbAllWith.custMain.KeyCode);
                Keyboard.Focus(Start_TBoxSearch);
            }
        }
    }
    #endregion

    #region 의뢰자

    #endregion

    #region 출발지
    // 출발지 검색
    private async void Start_TBoxSearch_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (string.IsNullOrEmpty(Start_TBoxSearch.Text))
        {
            의뢰자CopyTo출발지();
            Keyboard.Focus(Dest_TBoxSearch);
            return;
        }

        PostgResult_AllWithList result = await s_SrGClient.SrResult_CustMainWith_SelectRowsAsync_BySlash(Start_TBoxSearch.Text, true);
        if (!string.IsNullOrEmpty(result.sErr))
        {
            ErrMsgBox(result.sErr, "출발지검색");
            return;
        }

        if (result.listTbAll.Count == 0) // 없음
        {
            CustMain_RegEditWnd wnd = new CustMain_RegEditWnd();
            if (SafeShowDialog.WithMainWindowToOwner(wnd, this) == true)
            {
                TbAllTo출발지(wnd.tbAllWithNew);
                Keyboard.Focus(Dest_TBoxSearch);
            }
        }
        else if (result.listTbAll.Count == 1) // 1개
        {
            TbAllTo출발지(result.listTbAll[0]);
            Keyboard.Focus(Dest_TBoxSearch);
        }
        else // 여러개
        {
            CustMain_SearchedWnd wnd = new CustMain_SearchedWnd(result.listTbAll);
            if (SafeShowDialog.WithMainWindowToOwner(wnd, this) == true)
            {
                TbAllTo출발지(wnd.SelectedVM.tbAllWith);
                Keyboard.Focus(Dest_TBoxSearch);
            }
        }
    }

    // 출발지 지우기
    private void Start_BtnErase_Click(object sender, RoutedEventArgs e)
    {
        // vmOrder에 직접 설정 (바인딩으로 UI 자동 업데이트)
        vmOrder.StartCustCodeE = 0;
        vmOrder.StartCustCodeK = 0;
        vmOrder.StartCustName = "";
        vmOrder.StartChargeName = "";
        vmOrder.StartDeptName = "";
        vmOrder.StartDongBasic = "";
        vmOrder.StartTelNo = "";
        vmOrder.StartTelNo2 = "";
        vmOrder.StartAddress = "";
        vmOrder.StartDetailAddr = "";
    }

    // 출발지 거래처등록
    private void Start_BtnRegComp_Click(object sender, RoutedEventArgs e)
    {

    }

    // 출도착지 전환
    private void StartDest_BtnSwap_Click(object sender, RoutedEventArgs e)
    {
        // 출발지 보관
        long startCustCodeK = vmOrder.StartCustCodeK;
        long startCustCodeE = vmOrder.StartCustCodeE;
        string startCustName = vmOrder.StartCustName;
        string startChargeName = vmOrder.StartChargeName;
        string startDeptName = vmOrder.StartDeptName;
        string startDongBasic = vmOrder.StartDongBasic;
        string startTelNo = vmOrder.StartTelNo;
        string startTelNo2 = vmOrder.StartTelNo2;
        string startAddress = vmOrder.StartAddress;
        string startDetailAddr = vmOrder.StartDetailAddr;

        // 도착지 → 출발지
        vmOrder.StartCustCodeK = vmOrder.DestCustCodeK;
        vmOrder.StartCustCodeE = vmOrder.DestCustCodeE;
        vmOrder.StartCustName = vmOrder.DestCustName;
        vmOrder.StartChargeName = vmOrder.DestChargeName;
        vmOrder.StartDeptName = vmOrder.DestDeptName;
        vmOrder.StartDongBasic = vmOrder.DestDongBasic;
        vmOrder.StartTelNo = vmOrder.DestTelNo;
        vmOrder.StartTelNo2 = vmOrder.DestTelNo2;
        vmOrder.StartAddress = vmOrder.DestAddress;
        vmOrder.StartDetailAddr = vmOrder.DestDetailAddr;

        // 보관 → 도착지
        vmOrder.DestCustCodeK = startCustCodeK;
        vmOrder.DestCustCodeE = startCustCodeE;
        vmOrder.DestCustName = startCustName;
        vmOrder.DestChargeName = startChargeName;
        vmOrder.DestDeptName = startDeptName;
        vmOrder.DestDongBasic = startDongBasic;
        vmOrder.DestTelNo = startTelNo;
        vmOrder.DestTelNo2 = startTelNo2;
        vmOrder.DestAddress = startAddress;
        vmOrder.DestDetailAddr = startDetailAddr;
    }
    #endregion

    #region 도착지
    // 도착지 검색
    private async void Dest_TBoxSearch_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (string.IsNullOrEmpty(Dest_TBoxSearch.Text))
        {
            의뢰자CopyTo도착지();
            Keyboard.Focus(TBox_FeeBasic);
            return;
        }

        PostgResult_AllWithList result = await s_SrGClient.SrResult_CustMainWith_SelectRowsAsync_BySlash(Dest_TBoxSearch.Text, true);
        if (!string.IsNullOrEmpty(result.sErr))
        {
            ErrMsgBox(result.sErr, "도착지검색");
            return;
        }

        if (result.listTbAll.Count == 0) // 없음
        {
            CustMain_RegEditWnd wnd = new CustMain_RegEditWnd();
            if (SafeShowDialog.WithMainWindowToOwner(wnd, this) == true)
            {
                TbAllTo도착지(wnd.tbAllWithNew);
                Keyboard.Focus(TBox_FeeBasic);
            }
        }
        else if (result.listTbAll.Count == 1) // 1개
        {
            TbAllTo도착지(result.listTbAll[0]);
            Keyboard.Focus(TBox_FeeBasic);
        }
        else // 여러개
        {
            CustMain_SearchedWnd wnd = new CustMain_SearchedWnd(result.listTbAll);
            if (SafeShowDialog.WithMainWindowToOwner(wnd, this) == true)
            {
                TbAllTo도착지(wnd.SelectedVM.tbAllWith);
                Keyboard.Focus(TBox_FeeBasic);
            }
        }
    }

    // 도착지지우기
    private void Dest_BtnErase_Click(object sender, RoutedEventArgs e)
    {
        // vmOrder에 직접 설정 (바인딩으로 UI 자동 업데이트)
        vmOrder.DestCustCodeK = 0;
        vmOrder.DestCustCodeE = 0;
        vmOrder.DestCustName = "";
        vmOrder.DestChargeName = "";
        vmOrder.DestDeptName = "";
        vmOrder.DestDongBasic = "";
        vmOrder.DestTelNo = "";
        vmOrder.DestTelNo2 = "";
        vmOrder.DestAddress = "";
        vmOrder.DestDetailAddr = "";
    }

    // 도착지 거래처등록
    private void Dest_BtnRegComp_Click(object sender, RoutedEventArgs e)
    {

    }
    #endregion

    #region 우상(예약, 요금, 차량, 배송, 출발, 도착)
    #region 예약
    // 예약해제 LostFocus
    private void TBoxReserveBreakMin_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tbox && int.TryParse(tbox.Text, out int value))
        {
            tbox.Text = value.ToString(); // "010" → "10"
        }
    }

    // 예약여부 클릭
    private void ChkBoxReserve_Click(object sender, RoutedEventArgs e)
    {
        if ((bool)ChkBoxReserve.IsChecked) // 예약 체크시
        {
            DtPickerReserve.Value = DateTime.Now; // 예약일시를 현재로 설정
            DtPickerReserve.IsEnabled = true;
            TBoxReserveBreakMin.IsEnabled = true;
            // 예약모드로 변경
            if (vmOrder != null) vmOrder.OrderState = "예약";
        }
        else
        {
            DtPickerReserve.Value = null; // 예약일시를 비움
            TBoxReserveBreakMin.Text = "0";        // 예약시간 텍스트박스 비움
            DtPickerReserve.IsEnabled = false;
            TBoxReserveBreakMin.IsEnabled = false;
            // 접수모드로 변경
            if (vmOrder != null) vmOrder.OrderState = "접수";
        }
    }

    // DateTimePicker
    //private void DtPickerReserve_PreviewKeyDown(object sender, KeyEventArgs e)
    //{
    //    if (e.Key == Key.Enter)
    //    {
    //        DtPickerReserve.IsOpen = false; // Enter키를 누르면 DateTimePicker 닫기
    //    }
    //} 
    #endregion 예약 - 끝

    #region 배송
    private void ChkBox_편도_Checked(object sender, RoutedEventArgs e)
    {
        if (ChkBox_왕복 != null)
            ChkBox_왕복.IsChecked = false;
    }

    private void ChkBox_왕복_Checked(object sender, RoutedEventArgs e)
    {
        if (ChkBox_편도 != null)
            ChkBox_편도.IsChecked = false;
    }
    #endregion 배송 - 끝

    #region 출발
    private void SetStartTimeComboBoxEnabled(bool enabled)
    {
        if (CmbBox_출발일 != null) CmbBox_출발일.IsEnabled = enabled;
        if (CmbBox_출발시 != null) CmbBox_출발시.IsEnabled = enabled;
        if (CmbBox_출발분 != null) CmbBox_출발분.IsEnabled = enabled;
    }

    private void RadioBtn_즉시_Checked(object sender, RoutedEventArgs e)
    {
        SetStartTimeComboBoxEnabled(false);
        if (CmbBox_출발일 != null) CmbBox_출발일.SelectedIndex = -1;
        if (CmbBox_출발시 != null) CmbBox_출발시.SelectedIndex = -1;
        if (CmbBox_출발분 != null) CmbBox_출발분.SelectedIndex = -1;
    }

    private void RadioBtn_오늘_Checked(object sender, RoutedEventArgs e)
    {
        SetStartTimeComboBoxEnabled(true);
        int today = DateTime.Now.Day;
        if (CmbBox_출발일 != null) CmbBox_출발일.SelectedIndex = today - 1;
    }

    private void RadioBtn_날짜_Checked(object sender, RoutedEventArgs e)
    {
        SetStartTimeComboBoxEnabled(true);
    }
    #endregion 출발 - 끝

    #region 도착
    private void SetDestTimeComboBoxEnabled(bool enabled)
    {
        if (CmbBox_도착일 != null) CmbBox_도착일.IsEnabled = enabled;
        if (CmbBox_도착시 != null) CmbBox_도착시.IsEnabled = enabled;
        if (CmbBox_도착분 != null) CmbBox_도착분.IsEnabled = enabled;
    }

    private void RadioBtn_즉시도착_Checked(object sender, RoutedEventArgs e)
    {
        SetDestTimeComboBoxEnabled(false);
        if (CmbBox_도착일 != null) CmbBox_도착일.SelectedIndex = -1;
        if (CmbBox_도착시 != null) CmbBox_도착시.SelectedIndex = -1;
        if (CmbBox_도착분 != null) CmbBox_도착분.SelectedIndex = -1;
    }

    private void RadioBtn_오늘도착_Checked(object sender, RoutedEventArgs e)
    {
        SetDestTimeComboBoxEnabled(true);
        int today = DateTime.Now.Day;
        if (CmbBox_도착일 != null) CmbBox_도착일.SelectedIndex = today - 1;
    }

    private void RadioBtn_날짜도착_Checked(object sender, RoutedEventArgs e)
    {
        SetDestTimeComboBoxEnabled(true);
    }
    #endregion 도착 - 끝
    #endregion 우상(예약, 요금, 차량, 배송, 출발, 도착) - 모두 끝

    #region 퀵, 화물 전환섹터

    #endregion

    #region 요금
    // BasicFee
    private void Fee_TBoxBasic_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Keyboard.Focus(TBox_FeePlus); // 추가요금으로 이동
            e.Handled = true;
        }
    }

    // FeePlus
    private void Fee_TBoxPlus_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Keyboard.Focus(TBox_FeeMinus); // 할인요금으로 이동
            e.Handled = true;
        }
    }

    // FeeMinus
    private void Fee_TBoxMinus_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Keyboard.Focus(TBox_FeeConn); // 탁송요금으로 이동
            e.Handled = true;
        }
    }

    // FeeConn
    private void Fee_TBoxConn_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            // 신규 등록 모드
            if (vmOrder.KeyCode == 0) Keyboard.Focus(BtnReg_SaveReceipt); // 접수저장 버튼으로 이동
            else Keyboard.Focus(BtnMod_Save); // 저장 버튼으로 이동

            e.Handled = true; // 이벤트가 더 이상 상위/하위로 전달되지 않음
        }
    }
    #endregion

    #region 기사

    #endregion

    #region 공용이벤트(Tel)
    // GotFocus - Tel
    private void Tel_TBoxAll_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.Dispatcher.InvokeAsync(() =>
            {
                textBox.Text = textBox.Text.Replace("-", "");
                textBox.SelectAll();
            });
        }
    }

    // LostFocus - Tel
    private void Tel_TBoxAll_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.Dispatcher.InvokeAsync(() =>
            {
                textBox.Text = StdConvert.ToPhoneNumberFormat(textBox.Text);
            });
        }
    }

    #endregion

    #region 공용이벤트(Fee)
    // GotFocus - Fee
    private void Fee_TBoxAll_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.Dispatcher.InvokeAsync(() =>
            {
                textBox.Text = textBox.Text.Replace(",", "");
                textBox.SelectAll();
            });
        }
    }

    // LostFocus - Fee
    private void Fee_TBoxBasic_LostFocus(object sender, RoutedEventArgs e)
    {
        int FeeBasic = StdConvert.StringWonFormatToInt(TBox_FeeBasic.Text);
        int FeePlus = StdConvert.StringWonFormatToInt(TBox_FeePlus.Text);
        int FeeMinus = StdConvert.StringWonFormatToInt(TBox_FeeMinus.Text);
        int FeeConn = StdConvert.StringWonFormatToInt(TBox_FeeConn.Text);

        int FeeTot = FeeBasic + FeePlus - FeeMinus + FeeConn;

        vmOrder.FeeBasic = FeeBasic;
        vmOrder.FeeTotal = FeeTot;
        TBox_FeeBasic.Text = StdConvert.IntToStringWonFormat(FeeBasic);
        TBox_FeeTot.Text = StdConvert.IntToStringWonFormat(FeeTot);
    }
    private void Fee_TBoxPlus_LostFocus(object sender, RoutedEventArgs e)
    {
        int FeeBasic = StdConvert.StringWonFormatToInt(TBox_FeeBasic.Text);
        int FeePlus = StdConvert.StringWonFormatToInt(TBox_FeePlus.Text);
        int FeeMinus = StdConvert.StringWonFormatToInt(TBox_FeeMinus.Text);
        int FeeConn = StdConvert.StringWonFormatToInt(TBox_FeeConn.Text);

        int FeeTot = FeeBasic + FeePlus - FeeMinus + FeeConn;

        vmOrder.FeePlus = FeePlus;
        vmOrder.FeeTotal = FeeTot;
        TBox_FeePlus.Text = StdConvert.IntToStringWonFormat(FeePlus);
        TBox_FeeTot.Text = StdConvert.IntToStringWonFormat(FeeTot);
    }
    private void Fee_TBoxMinus_LostFocus(object sender, RoutedEventArgs e)
    {
        int FeeBasic = StdConvert.StringWonFormatToInt(TBox_FeeBasic.Text);
        int FeePlus = StdConvert.StringWonFormatToInt(TBox_FeePlus.Text);
        int FeeMinus = StdConvert.StringWonFormatToInt(TBox_FeeMinus.Text);
        int FeeConn = StdConvert.StringWonFormatToInt(TBox_FeeConn.Text);

        int FeeTot = FeeBasic + FeePlus - FeeMinus + FeeConn;

        vmOrder.FeeMinus = FeeMinus;
        vmOrder.FeeTotal = FeeTot;
        TBox_FeeMinus.Text = StdConvert.IntToStringWonFormat(FeeMinus);
        TBox_FeeTot.Text = StdConvert.IntToStringWonFormat(FeeTot);
    }
    private void Fee_TBoxConn_LostFocus(object sender, RoutedEventArgs e)
    {
        int FeeBasic = StdConvert.StringWonFormatToInt(TBox_FeeBasic.Text);
        int FeePlus = StdConvert.StringWonFormatToInt(TBox_FeePlus.Text);
        int FeeMinus = StdConvert.StringWonFormatToInt(TBox_FeeMinus.Text);
        int FeeConn = StdConvert.StringWonFormatToInt(TBox_FeeConn.Text);

        int FeeTot = FeeBasic + FeePlus - FeeMinus + FeeConn;

        vmOrder.FeeConn = FeeConn;
        vmOrder.FeeTotal = FeeTot;
        TBox_FeeConn.Text = StdConvert.IntToStringWonFormat(FeeConn);
        TBox_FeeTot.Text = StdConvert.IntToStringWonFormat(FeeTot);
    }
    #endregion

    #region 공용이벤트(Etc)
    // TextBox 숫자 입력 제한 - PreviewTextInput 이벤트
    private void TBoxOnlyNum_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        // 정규식: 숫자가 아닌 문자가 있으면 입력 차단
        e.Handled = s_RegexOnlyNum.IsMatch(e.Text);
    }

    // TextBox 숫자 입력 제한 - 붙여넣기 이벤트
    private void TBoxOnlyNum_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(DataFormats.Text))
        {
            string text = e.DataObject.GetData(DataFormats.Text) as string;
            if (string.IsNullOrEmpty(text) || s_RegexOnlyNum.IsMatch(text)) // 숫자 아니면 차단
            {
                e.CancelCommand();
            }
        }
        else
        {
            e.CancelCommand();
        }
    }

    #endregion


}
#nullable enable


