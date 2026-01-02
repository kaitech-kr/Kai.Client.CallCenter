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

namespace Kai.Client.CallCenter.Windows;
#nullable disable
public partial class Order_ReceiptWnd : Window
{
    #region Variables
    //public VmOrder_StatusPage_Order m_VmOrder_StatusPage_Order = null;
    private TbOrder tbOrderOrg = null;
    private TbOrder tbOrderNew = null;

    private long CallCustCodeK = 0, CallCustCodeE = 0;
    private long StartCustCodeK = 0, StartCustCodeE = 0;
    private long DestCustCodeK = 0, DestCustCodeE = 0;

    private long CallCompCode = 0;
    private string CallCustFrom = "", CallCompName = "";

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

        tbOrderOrg = tbOrder;

        // 모드에 따라 버튼그룹 보이기
        if (tbOrderOrg == null) // 신규 등록 모드
        {
            ColumnRegist.Visibility = Visibility.Visible;
            ColumnModify.Visibility = Visibility.Collapsed;
        }
        else // 수정 모드 - 고객정보도 같이 와야함
        {
            ColumnRegist.Visibility = Visibility.Collapsed;
            ColumnModify.Visibility = Visibility.Visible;
        }
    }

    public Order_ReceiptWnd(string sTelNo) // 전화에 의한 오더등록
    {
        InitializeComponent();

        TBoxHeader_Search.Text = sTelNo;

        // 신규 등록 모드
        ColumnRegist.Visibility = Visibility.Visible;
        ColumnModify.Visibility = Visibility.Collapsed;

        ChkBox퀵_편도.IsChecked = true;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        TBoxHeader_Search.Focus();

        if (tbOrderOrg != null) // 업데이트 모드면 오더정보 로드
        {
            TbOrderOrgToUiData();
            의뢰자정보Mode(tbOrderOrg.CallCustCodeK);
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
    }

    // 화물타입 선택 (다마스, 라보, 트럭)
    private void RadioBtn_CargoType_Checked(object sender, RoutedEventArgs e)
    {
        if (GridQuickInfo == null || GridCargoInfo == null) return;
        GridQuickInfo.Visibility = Visibility.Collapsed;
        GridCargoInfo.Visibility = Visibility.Visible;
    }
    #endregion

    #region Click - 주버튼(공용)
    // 의뢰자 고객수정 버튼
    private void BtnCommon_CustUpdate_Click(object sender, RoutedEventArgs e)
    {
        if (CallCustCodeK == 0) return; // 의뢰자 정보가 없으면 리턴

        CustMain_RegEditWnd wnd = new CustMain_RegEditWnd(CallCustCodeK);
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

    #endregion

    #region Button Clicks - Commented

    private async void BtnReg_SaveReceipt_Click(object sender, RoutedEventArgs e)
    {
        bool success = await SaveOrderAsync("접수", "접수저장");
        if (success)
        {
            DialogResult = true;
            Close();
        }
    }

    private async void BtnReg_SaveWait_Click(object sender, RoutedEventArgs e)
    {
        bool success = await SaveOrderAsync("대기", "대기저장");
        if (success)
        {
            DialogResult = true;
            Close();
        }
    }

    private async void BtnMod_Save_Click(object sender, RoutedEventArgs e)
    {
        bool success = await UpdateOrderAsync();
        if (success)
        {
            DialogResult = true;
            Close();
        }
    }

    #endregion

    #region Click - 서브버튼
    // 수신거부
    private void BtnNoRcvTel_Click(object sender, RoutedEventArgs e)
    {

    }

    // 출발지 지우기
    private void Start_BtnErase_Click(object sender, RoutedEventArgs e)
    {
        // Non UI
        StartCustCodeE = 0;
        StartCustCodeK = 0;

        // UI
        Start_TBoxCustName.Text = "";
        Start_TBoxChargeName.Text = "";
        Start_TBoxDeptName.Text = "";
        Start_TBoxDongBasic.Text = "";
        Start_TBoxTelNo1.Text = "";
        Start_TBoxTelNo2.Text = "";
        Start_TBoxDongAddr.Text = "";
        Start_TBoxDetailAddr.Text = "";
    }

    // 출발지 거래처등록
    private void Start_BtnRegComp_Click(object sender, RoutedEventArgs e)
    {

    }

    // 출도착지 전환
    private void StartDest_BtnSwap_Click(object sender, RoutedEventArgs e)
    {
        // 보관 - 필요
        long startCustCodeK = StartCustCodeK;
        long startCustCodeE = StartCustCodeE;

        string startCustName = Start_TBoxCustName.Text;
        string startChargeName = Start_TBoxChargeName.Text;
        string startDeptName = Start_TBoxDeptName.Text;
        string startDongBasic = Start_TBoxDongBasic.Text;
        string startTelNo1 = Start_TBoxTelNo1.Text;
        string startTelNo2 = Start_TBoxTelNo2.Text;
        string startDongAddr = Start_TBoxDongAddr.Text;
        string startDetailAddr = Start_TBoxDetailAddr.Text;

        StartCustCodeK = DestCustCodeK;
        StartCustCodeE = DestCustCodeE;

        Start_TBoxCustName.Text = Dest_TBoxCustName.Text;
        Start_TBoxChargeName.Text = Dest_TBoxChargeName.Text;
        Start_TBoxDeptName.Text = Dest_TBoxDeptName.Text;
        Start_TBoxDongBasic.Text = Dest_TBoxDongBasic.Text;
        Start_TBoxTelNo1.Text = Dest_TBoxTelNo1.Text;
        Start_TBoxTelNo2.Text = Dest_TBoxTelNo2.Text;
        Start_TBoxDongAddr.Text = Dest_TBoxDongAddr.Text;
        Start_TBoxDetailAddr.Text = Dest_TBoxDetailAddr.Text;

        DestCustCodeK = startCustCodeK;
        DestCustCodeE = startCustCodeE;

        Dest_TBoxCustName.Text = startCustName;
        Dest_TBoxChargeName.Text = startChargeName;
        Dest_TBoxDeptName.Text = startDeptName;
        Dest_TBoxDongBasic.Text = startDongBasic;
        Dest_TBoxTelNo1.Text = startTelNo1;
        Dest_TBoxTelNo2.Text = startTelNo2;
        Dest_TBoxDongAddr.Text = startDongAddr;
        Dest_TBoxDetailAddr.Text = startDetailAddr;
    }

    // 도착지지우기
    private void Dest_BtnErase_Click(object sender, RoutedEventArgs e)
    {
        // Non UI
        DestCustCodeK = 0;
        DestCustCodeE = 0;

        // UI
        Dest_TBoxCustName.Text = "";
        Dest_TBoxChargeName.Text = "";
        Dest_TBoxDeptName.Text = "";
        Dest_TBoxDongBasic.Text = "";
        Dest_TBoxTelNo1.Text = "";
        Dest_TBoxTelNo2.Text = "";
        Dest_TBoxDongAddr.Text = "";
        Dest_TBoxDetailAddr.Text = "";
    }

    // 도착지 거래처등록
    private void Dest_BtnRegComp_Click(object sender, RoutedEventArgs e)
    {

    }

    // 오더적요 지우기
    private void BtnErase_OrderRemarks_Click(object sender, RoutedEventArgs e)
    {
         //TBoxOrderRemarks.Text = string.Empty;
    }
    #endregion

    #region Click - CheckBox 
    private void ChkBoxReserve_Click(object sender, RoutedEventArgs e)
    {
        if ((bool)ChkBoxReserve.IsChecked)
        {
            DtPickerReserve.Value = DateTime.Now; // 예약일시를 현재로 설정
        }
        else
        {
            DtPickerReserve.Value = null; // 예약일시를 비움
            TBoxReserveBreakMin.Text = "";        // 예약시간 텍스트박스 비움
        }
    }
    #endregion

    #region KeyDown Events
    // DateTimePicker
    //private void DtPickerReserve_PreviewKeyDown(object sender, KeyEventArgs e)
    //{
    //    if (e.Key == Key.Enter)
    //    {
    //        DtPickerReserve.IsOpen = false; // Enter키를 누르면 DateTimePicker 닫기
    //    }
    //}

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
    //#endregion

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
            if (tbOrderOrg == null) Keyboard.Focus(BtnReg_SaveReceipt); // 접수저장 버튼으로 이동
            else Keyboard.Focus(BtnMod_Save); // 저장 버튼으로 이동

            e.Handled = true; // 이벤트가 더 이상 상위/하위로 전달되지 않음
        }
    }
    #endregion

    #region Focus Events
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

    // LostFocus - Fee
    private void Fee_TBoxBasic_LostFocus(object sender, RoutedEventArgs e)
    {
        int FeeBasic = StdConvert.StringWonFormatToInt(TBox_FeeBasic.Text);
        int FeePlus = StdConvert.StringWonFormatToInt(TBox_FeePlus.Text);
        int FeeMinus = StdConvert.StringWonFormatToInt(TBox_FeeMinus.Text);
        int FeeConn = StdConvert.StringWonFormatToInt(TBox_FeeConn.Text);

        int FeeTot = FeeBasic + FeePlus - FeeMinus + FeeConn;

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

        TBox_FeeConn.Text = StdConvert.IntToStringWonFormat(FeeConn);
        TBox_FeeTot.Text = StdConvert.IntToStringWonFormat(FeeTot);
    }
    #endregion

    #region RadioBtn Events
    //private void RadioBtn_트럭_Checked(object sender, RoutedEventArgs e)
    //{
    //    CmbBoxCarWeight.IsEnabled = true;
    //    CmbBoxCarWeight.SelectedIndex = 1; // 강제로 1톤으로...
    //    CmbBoxTruckDetail.IsEnabled = true;
    //}

    //private void RadioBtn_트럭_Unchecked(object sender, RoutedEventArgs e)
    //{
    //    CmbBoxCarWeight.IsEnabled = false;
    //    CmbBoxCarWeight.SelectedIndex = 0;
    //    CmbBoxTruckDetail.IsEnabled = false;
    //    CmbBoxTruckDetail.SelectedIndex = 0;
    //} 
    #endregion

    #region EtcEvents
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
            string? text = e.DataObject.GetData(DataFormats.Text) as string;
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

    #region Funcs

    private void Set화물DeliverTypeToUI(string sDeliver)
    {
        ChkBox화물_독차.IsChecked = false;
        ChkBox화물_혼적.IsChecked = false;
        ChkBox화물_왕복.IsChecked = false;
        ChkBox화물_경유.IsChecked = false;

        switch (sDeliver)
        {
            case "독차": ChkBox화물_독차.IsChecked = true; break;
            case "혼적": ChkBox화물_혼적.IsChecked = true; break;
            case "왕복": ChkBox화물_왕복.IsChecked = true; break;
            case "경유": ChkBox화물_경유.IsChecked = true; break;
        }
    }




    //// 배송타입
    //private string GetDeliverType()
    //{
    //    if (RadioBtn_편도.IsChecked == true) return "편도";
    //    if (RadioBtn_왕복.IsChecked == true) return "왕복";
    //    if (RadioBtn_경유.IsChecked == true) return "경유";
    //    if (RadioBtn_긴급.IsChecked == true) return "긴급";

    //    return "";
    //}
    //private void SetDeliverType(string sDeliver)
    //{
    //    switch (sDeliver) // 배송타입
    //    {
    //        case "편도": RadioBtn_편도.IsChecked = true; break;
    //        case "왕복": RadioBtn_왕복.IsChecked = true; break;
    //        case "경유": RadioBtn_경유.IsChecked = true; break;
    //        case "긴급": RadioBtn_긴급.IsChecked = true; break;
    //    }
    //}

    // TbOrder → UI 데이터 로드 (수정 모드)
    private void TbOrderOrgToUiData()
    {
        if (tbOrderOrg == null) return;

        // 1. Header, 공용
        LoadHeaderInfo();

        //// 2. 위치 정보 설정 (LocationData 헬퍼 사용)
        //LoadLocationDataToUi(LocationData.FromTbOrder_Caller(tbOrderOrg), CEnum_Kai_LocationType.Caller);
        //LoadLocationDataToUi(LocationData.FromTbOrder_Start(tbOrderOrg), CEnum_Kai_LocationType.Start);
        //LoadLocationDataToUi(LocationData.FromTbOrder_Dest(tbOrderOrg), CEnum_Kai_LocationType.Dest);

        //// 3. 의뢰자 전용 필드
        //Caller_TBoxRemarks.Text = tbOrderOrg.CallRemarks;
        //TBoxOrderRemarks.Text = tbOrderOrg.OrderRemarks;
        //TBoxOrderMemo.Text = tbOrderOrg.OrderMemo; // 오더메모 로드 추가
        //TBlkCallCustMemoExt.Text = tbOrderOrg.OrderMemoExt;
        //CallCustFrom = tbOrderOrg.CallCustFrom;
        //CallCompCode = tbOrderOrg.CallCompCode;
        //CallCompName = tbOrderOrg.CallCompName;

        //// 4. 예약 정보
        //LoadReserveInfo();

        //// 5. 차량/배송 타입
        //LoadVehicleInfo();

        //// 6. 요금 정보
        //LoadFeeInfo();

        //// 7. 공유/세금계산서
        //ChkBoxShareOrder.IsChecked = tbOrderOrg.Share;
        //ChkBoxTaxBill.IsChecked = tbOrderOrg.TaxBill;
    }

    // Header 정보 로드
    private void LoadHeaderInfo()
    {
        TBlkOrderState.Text = tbOrderOrg.OrderState;
        GridHeaderInfo.Background = tbOrderOrg.OrderState switch
        {
            "접수" => (Brush)Application.Current.Resources["AppBrushLightReceipt"],
            "대기" => (Brush)Application.Current.Resources["AppBrushLightWait"],
            "배차" => (Brush)Application.Current.Resources["AppBrushLightAlloc"],
            "예약" => (Brush)Application.Current.Resources["AppBrushLightReserve"],
            "운행" => (Brush)Application.Current.Resources["AppBrushLightRun"],
            "완료" => (Brush)Application.Current.Resources["AppBrushLightFinish"],
            "취소" => (Brush)Application.Current.Resources["AppBrushLightCancel"],
            _ => (Brush)Application.Current.Resources["AppBrushLightTotal"], // 전체
        };
        TBlkSeqNo.Text = tbOrderOrg.KeyCode.ToString();
    }

    // 예약 정보 로드
    private void LoadReserveInfo()
    {
        if (tbOrderOrg.DtReserve != null)
        {
            ChkBoxReserve.IsChecked = true;
            DtPickerReserve.Value = tbOrderOrg.DtReserve;
            TBoxReserveBreakMin.Text = tbOrderOrg.ReserveBreakMinute.ToString();
        }
    }

    // 차량/배송 타입 정보 로드
    //private void LoadVehicleInfo()
    //{
    //    SetFeeType(tbOrderOrg.FeeType);
    //    SetCarType(tbOrderOrg.CarType);
    //    //SetComboBoxItemByContent(CmbBoxCarWeight, tbOrderOrg.CarWeight);
    //    //SetComboBoxItemByContent(CmbBoxTruckDetail, tbOrderOrg.TruckDetail);
    //    SetDeliverType(tbOrderOrg.DeliverType);
    //}

    // 요금 정보 로드
    //private void LoadFeeInfo()
    //{
    //    TBox_FeeBasic.Text = StdConvert.IntToStringWonFormat(tbOrderOrg.FeeBasic);
    //    TBox_FeePlus.Text = StdConvert.IntToStringWonFormat(tbOrderOrg.FeePlus);
    //    TBox_FeeMinus.Text = StdConvert.IntToStringWonFormat(tbOrderOrg.FeeMinus);
    //    TBox_FeeConn.Text = StdConvert.IntToStringWonFormat(tbOrderOrg.FeeConn);
    //    TBox_FeeDrvCharge.Text = StdConvert.IntToStringWonFormat(tbOrderOrg.FeeCommi);
    //    TBox_FeeTot.Text = StdConvert.IntToStringWonFormat(tbOrderOrg.FeeTotal);
    //}









    //private void MakeCopiedNewTbOrder() // UI데이타 빼고 복사
    //{
    //    if (tbOrderOrg == null)
    //    {
    //        ErrMsgBox("복사할 테이블이 없읍니다.");
    //        return;
    //    }

    //    tbOrderNew = new TbOrder();

    //    tbOrderNew.KeyCode = tbOrderOrg.KeyCode;
    //    tbOrderNew.MemberCode = s_CenterCharge.MemberCode;
    //    tbOrderNew.CenterCode = s_CenterCharge.CenterCode;
    //    tbOrderNew.DtRegist = tbOrderOrg.DtRegist;
    //    tbOrderNew.OrderState = tbOrderOrg.OrderState;
    //    tbOrderNew.OrderStateOld = tbOrderOrg.OrderStateOld;
    //    tbOrderNew.OrderRemarks = tbOrderOrg.OrderRemarks; // UiData
    //    tbOrderNew.OrderMemo = tbOrderOrg.OrderMemo; // UiData
    //    tbOrderNew.OrderMemoExt = tbOrderOrg.OrderMemoExt; // UiData
    //    tbOrderNew.UserCode = tbOrderOrg.UserCode; // 필요 없을것 같음.
    //    tbOrderNew.UserName = tbOrderOrg.UserName; // 필요 없을것 같음.
    //    tbOrderNew.Updater = s_CenterCharge.Id;
    //    tbOrderNew.UpdateDate = DateTime.Now.ToString(StdConst_Var.DTFORMAT_EXCEPT_SEC);

    //    tbOrderNew.CallCompCode = tbOrderOrg.CallCompCode;
    //    tbOrderNew.CallCompName = tbOrderOrg.CallCompName;
    //    tbOrderNew.CallCustFrom = tbOrderOrg.CallCustFrom;
    //    tbOrderNew.CallCustCodeE = tbOrderOrg.CallCustCodeE;
    //    tbOrderNew.CallCustCodeK = CallCustCodeK;
    //    if (tbOrderNew.CallCustCodeK <= 0) ErrMsgBox($"의뢰자코드가 0입니다.");
    //    //tbOrderNew.CallCustName = ; // UiData
    //    //tbOrderNew.CallTelNo = ; // UiData
    //    //tbOrderNew.CallTelNo2 = ; // UiData
    //    //tbOrderNew.CallDeptName = ; // UiData
    //    //tbOrderNew.CallChargeName = ; // UiData
    //    //tbOrderNew.CallDongBasic = ; // UiData
    //    //tbOrderNew.CallAddress = ; // UiData
    //    //tbOrderNew.CallDetailAddr = ; // UiData
    //    //tbOrderNew.CallRemarks = ; // UiData
    //    tbOrderNew.StartCustCodeE = 0;
    //    tbOrderNew.StartCustCodeK = StartCustCodeK;
    //    //tbOrderNew.StartCustName = ; // UiData
    //    //tbOrderNew.StartTelNo = ; // UiData
    //    //tbOrderNew.StartTelNo2 = ; // UiData
    //    //tbOrderNew.StartDeptName = ; // UiData
    //    //tbOrderNew.StartChargeName = ; // UiData
    //    //tbOrderNew.StartDongBasic = ; // UiData
    //    //tbOrderNew.StartAddress = ; // UiData
    //    //tbOrderNew.StartDetailAddr = ; // UiData
    //    tbOrderNew.StartSiDo = tbOrderOrg.StartSiDo; // UiData
    //    tbOrderNew.StartGunGu = tbOrderOrg.StartGunGu; // UiData
    //    tbOrderNew.StartDongRi = tbOrderOrg.StartDongRi; // UiData
    //    tbOrderNew.StartLon = tbOrderOrg.StartLon; // 연구과제
    //    tbOrderNew.StartLat = tbOrderOrg.StartLat; // 연구과제
    //    tbOrderNew.StartSignImg = tbOrderOrg.StartSignImg; // 연구과제
    //    tbOrderNew.StartDtSign = tbOrderOrg.StartDtSign; // 연구과제
    //    tbOrderNew.DestCustCodeE = 0;
    //    tbOrderNew.DestCustCodeK = DestCustCodeK;
    //    //tbOrderNew.DestCustName = ; // UiData
    //    //tbOrderNew.DestTelNo = ; // UiData
    //    //tbOrderNew.DestTelNo2 = ; // UiData
    //    //tbOrderNew.DestDeptName = ; // UiData
    //    //tbOrderNew.DestChargeName = ; // UiData
    //    //tbOrderNew.DestDongBasic = ; // UiData
    //    //tbOrderNew.DestAddress = ; // UiData
    //    //tbOrderNew.DestDetailAddr = ; // UiData
    //    tbOrderNew.DestSiDo = tbOrderOrg.DestSiDo; // UiData
    //    tbOrderNew.DestGunGu = tbOrderOrg.DestGunGu; // UiData
    //    tbOrderNew.DestDongRi = tbOrderOrg.DestDongRi; // UiData
    //    tbOrderNew.DestLon = tbOrderOrg.DestLon; // 연구과제
    //    tbOrderNew.DestLat = tbOrderOrg.DestLat; // 연구과제
    //    tbOrderNew.DestSignImg = tbOrderOrg.DestSignImg; // 연구과제
    //    tbOrderNew.DestDtSign = tbOrderOrg.DestDtSign; // 연구과제
    //    tbOrderNew.DtReserve = tbOrderOrg.DtReserve; // UiData
    //    tbOrderNew.ReserveBreakMinute = tbOrderOrg.ReserveBreakMinute; // UiData
    //    tbOrderNew.FeeBasic = tbOrderOrg.FeeBasic; // UiData
    //    tbOrderNew.FeePlus = tbOrderOrg.FeePlus; // UiData
    //    tbOrderNew.FeeMinus = tbOrderOrg.FeeMinus; // UiData
    //    tbOrderNew.FeeConn = tbOrderOrg.FeeConn; // UiData
    //    tbOrderNew.FeeDriver = tbOrderOrg.FeeDriver; // UiData
    //    tbOrderNew.FeeCharge = tbOrderOrg.FeeCharge; // UiData
    //    tbOrderNew.FeeTotal = tbOrderOrg.FeeTotal; // UiData
    //    tbOrderNew.FeeType = tbOrderOrg.FeeType; // UiData
    //    tbOrderNew.CarType = tbOrderOrg.CarType; // UiData
    //    tbOrderNew.CarWeight = tbOrderOrg.CarWeight; // UiData
    //    tbOrderNew.TruckDetail = tbOrderOrg.TruckDetail; // UiData
    //    tbOrderNew.DeliverType = tbOrderOrg.DeliverType; // UiData
    //    tbOrderNew.DriverCode = tbOrderOrg.DriverCode; // UiData
    //    tbOrderNew.DriverId = tbOrderOrg.DriverId; // UiData
    //    tbOrderNew.DriverName = tbOrderOrg.DriverName; // UiData
    //    tbOrderNew.DriverTelNo = tbOrderOrg.DriverTelNo; // UiData
    //    tbOrderNew.DriverMemberCode = tbOrderOrg.DriverMemberCode; // UiData
    //    tbOrderNew.DriverCenterId = tbOrderOrg.DriverCenterId; // UiData
    //    tbOrderNew.DriverCenterName = tbOrderOrg.DriverCenterName; // UiData
    //    tbOrderNew.DriverBusinessNo = tbOrderOrg.DriverBusinessNo; // UiData
    //    tbOrderNew.Insung1 = tbOrderOrg.Insung1;
    //    tbOrderNew.Insung2 = tbOrderOrg.Insung2;
    //    tbOrderNew.Cargo24 = tbOrderOrg.Cargo24;
    //    tbOrderNew.Onecall = tbOrderOrg.Onecall;
    //    tbOrderNew.Share = tbOrderOrg.Share;
    //    tbOrderNew.TaxBill = tbOrderOrg.TaxBill;
    //    tbOrderNew.ReceiptTime = tbOrderOrg.ReceiptTime;
    //    tbOrderNew.AllocTime = tbOrderOrg.AllocTime;
    //    tbOrderNew.RunTime = tbOrderOrg.RunTime;
    //    tbOrderNew.FinishTime = tbOrderOrg.FinishTime;
    //}

    //private int WhereUpdatableChanged()
    //{
    //    // 변경되면 안되는 항목
    //    if (tbOrderNew.KeyCode != tbOrderOrg.KeyCode) return -1;
    //    if (tbOrderNew.MemberCode != tbOrderOrg.MemberCode) return -3;
    //    if (tbOrderNew.CenterCode != tbOrderOrg.CenterCode) return -4;
    //    //if (tbOrderNew.DtRegOrder != tbOrderOrg.DtRegOrder) return -5;
    //    if (tbOrderNew.OrderStateOld != tbOrderOrg.OrderStateOld) return -7;

    //    // 변경되면 인정되는 항목
    //    if (tbOrderNew.OrderRemarks != tbOrderOrg.OrderRemarks) return 1;
    //    if (tbOrderNew.OrderMemo != tbOrderOrg.OrderMemo) return 2;
    //    if (tbOrderNew.OrderMemoExt != tbOrderOrg.OrderMemoExt) return 3;
    //    if (tbOrderNew.UserCode != tbOrderOrg.UserCode) return 4;
    //    if (tbOrderNew.OrderState != tbOrderOrg.OrderState) return 5;
    //    //if (tbOrderNew.UserName != tbOrderOrg.UserName) return 5;
    //    //if (tbOrderNew.Updater != tbOrderOrg.Updater) return true; // UiData
    //    //if (tbOrderNew.UpdateDate != tbOrderOrg.UpdateDate) return true; // UiData
    //    if (tbOrderNew.CallCustCodeE != tbOrderOrg.CallCustCodeE) return 6;
    //    if (tbOrderNew.CallCustCodeK != tbOrderOrg.CallCustCodeK) return 7;
    //    if (tbOrderNew.CallCustName != tbOrderOrg.CallCustName) return 8;
    //    //MsgBox($"{tbOrderNew.CallTelNo}, {tbOrderOrg.CallTelNo}"); // Test
    //    if (tbOrderNew.CallTelNo != tbOrderOrg.CallTelNo) return 9;
    //    if (tbOrderNew.CallTelNo2 != tbOrderOrg.CallTelNo2) return 10;
    //    if (tbOrderNew.CallDeptName != tbOrderOrg.CallDeptName) return 11;
    //    if (tbOrderNew.CallChargeName != tbOrderOrg.CallChargeName) return 12;
    //    if (tbOrderNew.CallDongBasic != tbOrderOrg.CallDongBasic) return 13;
    //    if (tbOrderNew.CallAddress != tbOrderOrg.CallAddress) return 14;
    //    if (tbOrderNew.CallDetailAddr != tbOrderOrg.CallDetailAddr) return 15;
    //    if (tbOrderNew.CallRemarks != tbOrderOrg.CallRemarks) return 16;
    //    if (tbOrderNew.StartCustCodeE != tbOrderOrg.StartCustCodeE) return 17;
    //    if (tbOrderNew.StartCustCodeK != tbOrderOrg.StartCustCodeK) return 18;
    //    if (tbOrderNew.StartCustName != tbOrderOrg.StartCustName) return 19;
    //    if (tbOrderNew.StartTelNo != tbOrderOrg.StartTelNo) return 20;
    //    if (tbOrderNew.StartTelNo2 != tbOrderOrg.StartTelNo2) return 21;
    //    if (tbOrderNew.StartDeptName != tbOrderOrg.StartDeptName) return 22;
    //    if (tbOrderNew.StartChargeName != tbOrderOrg.StartChargeName) return 23;
    //    if (tbOrderNew.StartDongBasic != tbOrderOrg.StartDongBasic) return 24;
    //    if (tbOrderNew.StartAddress != tbOrderOrg.StartAddress) return 25;
    //    if (tbOrderNew.StartDetailAddr != tbOrderOrg.StartDetailAddr) return 26;
    //    //MsgBox($"/{tbOrderNew.StartSiDo}:{tbOrderNew.StartSiDo.Length}/<->/{tbOrderOrg.StartSiDo}:{tbOrderOrg.StartSiDo.Length}/"); // Test
    //    if (tbOrderNew.StartSiDo != tbOrderOrg.StartSiDo) return 27;
    //    if (tbOrderNew.StartGunGu != tbOrderOrg.StartGunGu) return 28;
    //    if (tbOrderNew.StartDongRi != tbOrderOrg.StartDongRi) return 29;
    //    if (tbOrderNew.StartLon != tbOrderOrg.StartLon) return 30;
    //    if (tbOrderNew.StartLat != tbOrderOrg.StartLat) return 31;
    //    if (tbOrderNew.StartSignImg != tbOrderOrg.StartSignImg) return 32;
    //    if (tbOrderNew.StartDtSign != tbOrderOrg.StartDtSign) return 33;
    //    if (tbOrderNew.DestCustCodeE != tbOrderOrg.DestCustCodeE) return 34;
    //    if (tbOrderNew.DestCustCodeK != tbOrderOrg.DestCustCodeK) return 35;
    //    if (tbOrderNew.DestCustName != tbOrderOrg.DestCustName) return 36;
    //    if (tbOrderNew.DestTelNo != tbOrderOrg.DestTelNo) return 37;
    //    if (tbOrderNew.DestTelNo2 != tbOrderOrg.DestTelNo2) return 38;
    //    if (tbOrderNew.DestDeptName != tbOrderOrg.DestDeptName) return 39;
    //    if (tbOrderNew.DestChargeName != tbOrderOrg.DestChargeName) return 40;
    //    if (tbOrderNew.DestDongBasic != tbOrderOrg.DestDongBasic) return 41;
    //    if (tbOrderNew.DestAddress != tbOrderOrg.DestAddress) return 42;
    //    if (tbOrderNew.DestDetailAddr != tbOrderOrg.DestDetailAddr) return 43;
    //    if (tbOrderNew.DestSiDo != tbOrderOrg.DestSiDo) return 44;
    //    if (tbOrderNew.DestGunGu != tbOrderOrg.DestGunGu) return 45;
    //    if (tbOrderNew.DestDongRi != tbOrderOrg.DestDongRi) return 46;
    //    if (tbOrderNew.DestLon != tbOrderOrg.DestLon) return 47;
    //    if (tbOrderNew.DestLat != tbOrderOrg.DestLat) return 48;
    //    if (tbOrderNew.DestSignImg != tbOrderOrg.DestSignImg) return 49;
    //    if (tbOrderNew.DestDtSign != tbOrderOrg.DestDtSign) return 50;
    //    if (tbOrderNew.DtReserve != tbOrderOrg.DtReserve) return 51;
    //    if (tbOrderNew.ReserveBreakMinute != tbOrderOrg.ReserveBreakMinute) return 52;
    //    if (tbOrderNew.FeeBasic != tbOrderOrg.FeeBasic) return 53;
    //    if (tbOrderNew.FeePlus != tbOrderOrg.FeePlus) return 54;
    //    if (tbOrderNew.FeeMinus != tbOrderOrg.FeeMinus) return 55;
    //    if (tbOrderNew.FeeConn != tbOrderOrg.FeeConn) return 56;
    //    if (tbOrderNew.FeeDriver != tbOrderOrg.FeeDriver) return 57;
    //    if (tbOrderNew.FeeCharge != tbOrderOrg.FeeCharge) return 58;
    //    if (tbOrderNew.FeeTotal != tbOrderOrg.FeeTotal) return 59;
    //    if (tbOrderNew.FeeType != tbOrderOrg.FeeType) return 60;
    //    if (tbOrderNew.CarType != tbOrderOrg.CarType) return 61;
    //    if (tbOrderNew.CarWeight != tbOrderOrg.CarWeight) return 62;
    //    if (tbOrderNew.TruckDetail != tbOrderOrg.TruckDetail) return 63;
    //    if (tbOrderNew.DeliverType != tbOrderOrg.DeliverType) return 64;
    //    if (tbOrderNew.DriverCode != tbOrderOrg.DriverCode) return 65;
    //    if (tbOrderNew.DriverId != tbOrderOrg.DriverId) return 66;
    //    if (tbOrderNew.DriverName != tbOrderOrg.DriverName) return 67;
    //    if (tbOrderNew.DriverTelNo != tbOrderOrg.DriverTelNo) return 68;
    //    if (tbOrderNew.DriverMemberCode != tbOrderOrg.DriverMemberCode) return 69;
    //    if (tbOrderNew.DriverCenterId != tbOrderOrg.DriverCenterId) return 70;
    //    if (tbOrderNew.DriverCenterName != tbOrderOrg.DriverCenterName) return 71;
    //    if (tbOrderNew.DriverBusinessNo != tbOrderOrg.DriverBusinessNo) return 72;
    //    if (tbOrderNew.Insung1 != tbOrderOrg.Insung1) return 74;
    //    if (tbOrderNew.Insung2 != tbOrderOrg.Insung2) return 75;
    //    if (tbOrderNew.Cargo24 != tbOrderOrg.Cargo24) return 76;
    //    if (tbOrderNew.Onecall != tbOrderOrg.Onecall) return 77;
    //    if (tbOrderNew.Share != tbOrderOrg.Share) return 78;
    //    if (tbOrderNew.TaxBill != tbOrderOrg.TaxBill) return 79;
    //    if (tbOrderNew.ReceiptTime != tbOrderOrg.ReceiptTime) return 80;
    //    if (tbOrderNew.AllocTime != tbOrderOrg.AllocTime) return 81;
    //    if (tbOrderNew.RunTime != tbOrderOrg.RunTime) return 82;
    //    if (tbOrderNew.FinishTime != tbOrderOrg.FinishTime) return 83;

    //    if (tbOrderNew.CallCustFrom != tbOrderOrg.CallCustFrom) return 84;
    //    if (tbOrderNew.CallCompCode != tbOrderOrg.CallCompCode) return 85;
    //    if (tbOrderNew.CallCompName != tbOrderOrg.CallCompName) return 86;

    //    return 0;
    //}

    // 고객정보
    private void TbAllTo의뢰자(TbAllWith tb)
    {
        TbCustMain tbCustMain = tb.custMain;
        TbCompany tbCompany = tb.company;
        TbCallCenter tbCallCenter = tb.callCenter;

        SetLocationDataToUi(tbCustMain, "의뢰자");

        // 의뢰자 전용 필드 설정
        CallCustFrom = tbCustMain.BeforeBelong;
        CallCompCode = tbCustMain.CompCode;
        if (tbCompany == null)
        {
            CallCompName = "";
        }
        else
        {
            CallCompName = tbCompany.CompName;
            SetFeeTypeToUI(tbCompany.TradeType);
        }

        TBlkCallCustMemoExt.Text = tbCustMain.Memo;
    }

    //private void TbAllTo출발지(TbAllWith tb)
    //{
    //    TbCustMain tbCustMain = tb.custMain;

    //    // LocationData 헬퍼 사용
    //    var locationData = LocationData.FromTbCustMain(tbCustMain);
    //    LoadLocationDataToUi(locationData, CEnum_Kai_LocationType.Start);
    //}
    //private void 의뢰자CopyTo출발지()
    //{
    //    // LocationData 헬퍼 사용 - 의뢰자 → 출발지 복사
    //    CopyLocationData(CEnum_Kai_LocationType.Caller, CEnum_Kai_LocationType.Start);
    //}

    //private void TbAllTo도착지(TbAllWith tb)
    //{
    //    TbCustMain tbCustMain = tb.custMain;

    //    // LocationData 헬퍼 사용
    //    var locationData = LocationData.FromTbCustMain(tbCustMain);
    //    LoadLocationDataToUi(locationData, CEnum_Kai_LocationType.Dest);
    //}

    //private void 의뢰자CopyTo도착지()
    //{
    //    // LocationData 헬퍼 사용 - 의뢰자 → 도착지 복사
    //    CopyLocationData(CEnum_Kai_LocationType.Caller, CEnum_Kai_LocationType.Dest);
    //}

    private void 의뢰자정보Mode(long lCustKey = 0)
    {
        if (lCustKey == 0) // 없음
        {
            BorderCustNew.Visibility = Visibility.Visible;
        }
        else // 있음
        {
            BtnReg_CustRegist.Visibility = Visibility.Collapsed;
            BtnReg_CustUpdate.Visibility = Visibility.Visible;

            BorderCustNew.Visibility = Visibility.Collapsed;

            // 고객정보 로드
        }
    }
    private void 의뢰자정보Mode(TbCustMain tbCust = null)
    {
        if (tbCust == null) // 없음
        {
            BorderCustNew.Visibility = Visibility.Visible;
        }
        else // 있음
        {
            BtnReg_CustRegist.Visibility = Visibility.Collapsed;
            BtnReg_CustUpdate.Visibility = Visibility.Visible;

            BorderCustNew.Visibility = Visibility.Collapsed;

            // 고객정보 로드
        }
    }
    #endregion

    #region LocationData Helper - 위치 정보 공통 처리
    //// 위치 정보 데이터 (의뢰자/출발지/도착지 공통)


    //// TbOrder → LocationData 변환 (의뢰자)
    //    public static LocationData FromTbOrder_Caller(TbOrder tb)
    //    {
    //        return new LocationData
    //        {
    //            CustCodeK = StdConvert.NullableLongToLong(tb.CallCustCodeK),
    //            CustCodeE = StdConvert.NullableLongToLong(tb.CallCustCodeE),
    //            CustName = tb.CallCustName,
    //            DongBasic = tb.CallDongBasic,
    //            TelNo1 = StdConvert.ToPhoneNumberFormat(tb.CallTelNo),
    //            TelNo2 = StdConvert.ToPhoneNumberFormat(tb.CallTelNo2),
    //            DeptName = tb.CallDeptName,
    //            ChargeName = tb.CallChargeName,
    //            DongAddr = tb.CallAddress,
    //            DetailAddr = tb.CallDetailAddr
    //        };
    //    }

    //// TbOrder → LocationData 변환 (출발지)
    //    public static LocationData FromTbOrder_Start(TbOrder tb)
    //    {
    //        return new LocationData
    //        {
    //            CustCodeK = StdConvert.NullableLongToLong(tb.StartCustCodeK),
    //            CustCodeE = StdConvert.NullableLongToLong(tb.StartCustCodeE),
    //            CustName = tb.StartCustName,
    //            DongBasic = tb.StartDongBasic,
    //            TelNo1 = StdConvert.ToPhoneNumberFormat(tb.StartTelNo),
    //            TelNo2 = StdConvert.ToPhoneNumberFormat(tb.StartTelNo2),
    //            DeptName = tb.StartDeptName,
    //            ChargeName = tb.StartChargeName,
    //            DongAddr = tb.StartAddress,
    //            DetailAddr = tb.StartDetailAddr
    //        };
    //    }

    //// TbOrder → LocationData 변환 (도착지)
    //    public static LocationData FromTbOrder_Dest(TbOrder tb)
    //    {
    //        return new LocationData
    //        {
    //            CustCodeK = StdConvert.NullableLongToLong(tb.DestCustCodeK),
    //            CustCodeE = StdConvert.NullableLongToLong(tb.DestCustCodeE),
    //            CustName = tb.DestCustName,
    //            DongBasic = tb.DestDongBasic,
    //            TelNo1 = StdConvert.ToPhoneNumberFormat(tb.DestTelNo),
    //            TelNo2 = StdConvert.ToPhoneNumberFormat(tb.DestTelNo2),
    //            DeptName = tb.DestDeptName,
    //            ChargeName = tb.DestChargeName,
    //            DongAddr = tb.DestAddress,
    //            DetailAddr = tb.DestDetailAddr
    //        };
    //    }
    //}

    // LocationData → UI 로드 (공통 헬퍼)
    private void SetLocationDataToUi(TbCustMain tbCustMain, string sWhere)
    {
        // Define Action delegates to set the non-UI properties
        Action<long> setCustCodeK;
        Action<long> setCustCodeE;

        // Use a tuple to hold the UI controls
        (TextBox tboxCustName, TextBox tboxDongBasic, TextBox tboxTelNo1, TextBox tboxTelNo2,
         TextBox tboxDeptName, TextBox tboxChargeName, TextBox tboxDongAddr) controls;

        // Use a switch statement to assign the delegates and controls based on sWhere
        switch (sWhere)
        {
            case "의뢰자":
                setCustCodeK = (val) => CallCustCodeK = val;
                setCustCodeE = (val) => CallCustCodeE = val;
                controls = (Caller_TBoxCustName, Caller_TBoxDongBasic, Caller_TBoxTelNo1, Caller_TBoxTelNo2,
                            Caller_TBoxDeptName, Caller_TBoxChargeName, Caller_TBoxDongAddr);
                break;
            case "출발지":
                setCustCodeK = (val) => StartCustCodeK = val;
                setCustCodeE = (val) => StartCustCodeE = val;
                controls = (Start_TBoxCustName, Start_TBoxDongBasic, Start_TBoxTelNo1, Start_TBoxTelNo2,
                            Start_TBoxDeptName, Start_TBoxChargeName, Start_TBoxDongAddr);
                break;
            case "도착지":
                setCustCodeK = (val) => DestCustCodeK = val;
                setCustCodeE = (val) => DestCustCodeE = val;
                controls = (Dest_TBoxCustName, Dest_TBoxDongBasic, Dest_TBoxTelNo1, Dest_TBoxTelNo2,
                            Dest_TBoxDeptName, Dest_TBoxChargeName, Dest_TBoxDongAddr);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(sWhere));
        }

        // Now, perform the assignments in one place, directly from tbCustMain
        setCustCodeK(tbCustMain.KeyCode);
        setCustCodeE(StdConvert.NullableLongToLong(tbCustMain.BeforeCustKey));
        controls.tboxCustName.Text = tbCustMain.CustName;
        controls.tboxDongBasic.Text = tbCustMain.DongBasic;
        controls.tboxTelNo1.Text = tbCustMain.TelNo1;
        controls.tboxTelNo2.Text = tbCustMain.TelNo2;
        controls.tboxDeptName.Text = tbCustMain.DeptName;
        controls.tboxChargeName.Text = tbCustMain.ChargeName;
        controls.tboxDongAddr.Text = tbCustMain.DongAddr;
    }

    //// UI → LocationData 읽기 (공통 헬퍼)
    //private LocationData GetLocationDataFromUi(CEnum_Kai_LocationType locType)
    //{
    //    return locType switch
    //    {
    //        CEnum_Kai_LocationType.Caller => new LocationData
    //        {
    //            CustCodeK = CallCustCodeK,
    //            CustCodeE = CallCustCodeE,
    //            CustName = Caller_TBoxCustName.Text,
    //            DongBasic = Caller_TBoxDongBasic.Text,
    //            TelNo1 = Caller_TBoxTelNo1.Text,
    //            TelNo2 = Caller_TBoxTelNo2.Text,
    //            DeptName = Caller_TBoxDeptName.Text,
    //            ChargeName = Caller_TBoxChargeName.Text,
    //            DongAddr = Caller_TBoxDongAddr.Text,
    //            DetailAddr = Caller_TBoxDetailAddr.Text
    //        },
    //        CEnum_Kai_LocationType.Start => new LocationData
    //        {
    //            CustCodeK = StartCustCodeK,
    //            CustCodeE = StartCustCodeE,
    //            CustName = Start_TBoxCustName.Text,
    //            DongBasic = Start_TBoxDongBasic.Text,
    //            TelNo1 = Start_TBoxTelNo1.Text,
    //            TelNo2 = Start_TBoxTelNo2.Text,
    //            DeptName = Start_TBoxDeptName.Text,
    //            ChargeName = Start_TBoxChargeName.Text,
    //            DongAddr = Start_TBoxDongAddr.Text,
    //            DetailAddr = Start_TBoxDetailAddr.Text
    //        },
    //        CEnum_Kai_LocationType.Dest => new LocationData
    //        {
    //            CustCodeK = DestCustCodeK,
    //            CustCodeE = DestCustCodeE,
    //            CustName = Dest_TBoxCustName.Text,
    //            DongBasic = Dest_TBoxDongBasic.Text,
    //            TelNo1 = Dest_TBoxTelNo1.Text,
    //            TelNo2 = Dest_TBoxTelNo2.Text,
    //            DeptName = Dest_TBoxDeptName.Text,
    //            ChargeName = Dest_TBoxChargeName.Text,
    //            DongAddr = Dest_TBoxDongAddr.Text,
    //            DetailAddr = Dest_TBoxDetailAddr.Text
    //        },
    //        _ => throw new ArgumentException($"Invalid CEnum_Kai_LocationType: {locType}")
    //    };
    //}

    //// 위치 정보 복사 (UI → UI)
    //private void CopyLocationData(CEnum_Kai_LocationType from, CEnum_Kai_LocationType to)
    //{
    //    var data = GetLocationDataFromUi(from);
    //    LoadLocationDataToUi(data, to);
    //}
    #endregion

    #region Empty Stubs for XAML
    //private void TBoxHeader_Search_KeyDown(object sender, KeyEventArgs e) { }
    //private void Start_TBoxSearch_KeyDown(object sender, KeyEventArgs e) { }
    //private void Dest_TBoxSearch_KeyDown(object sender, KeyEventArgs e) { }
    //private void BtnReg_CustRegist_Click(object sender, RoutedEventArgs e) { }
    //private void BtnReg_SaveReceipt_Click(object sender, RoutedEventArgs e) { }
    //private void BtnReg_SaveWait_Click(object sender, RoutedEventArgs e) { }
    //private void BtnMod_Ask_Click(object sender, RoutedEventArgs e) { }
    //private void BtnMod_Allocation_Click(object sender, RoutedEventArgs e) { }
    //private void BtnMod_PrintBill_Click(object sender, RoutedEventArgs e) { }
    //private void BtnMod_Finish_Click(object sender, RoutedEventArgs e) { }
    //private void BtnMod_Wait_Click(object sender, RoutedEventArgs e) { }
    //private void BtnMod_Cancel_Click(object sender, RoutedEventArgs e) { }
    //private void BtnMod_Receipt_Click(object sender, RoutedEventArgs e) { }
    //private void BtnMod_Save_Click(object sender, RoutedEventArgs e) { }
    #endregion
}
#nullable enable


