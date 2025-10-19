// Xceed Extended WPF Toolkit Nuget - For DateTimePicker(3.8.1 만 무료)

using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;

using Kai.Common.StdDll_Common;
using Kai.Common.FrmDll_FormCtrl;
using Kai.Common.NetDll_WpfCtrl.NetWnds;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Results;

using Kai.Client.CallCenter.Classes;
using static Kai.Client.CallCenter.Class_Common.CommonVars;
using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgBox;
using System.Diagnostics;

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
    //public Order_ReceiptWnd(TbOrder tbOrder = null)
    //{
    //    InitializeComponent();

    //    TBlkRegister.Text = s_CenterCharge.Id;
    //    TBlkRegistPlace.Text = s_CallCenter.CenterName;
    //    tbOrderOrg = tbOrder;

    //    // 모드에 따라 버튼그룹 보이기
    //    if (tbOrderOrg == null) // 신규 등록 모드
    //    {
    //        ColumnRegist.Visibility = Visibility.Visible;
    //        ColumnModify.Visibility = Visibility.Collapsed;

    //        RadioBtn_편도.IsChecked = true; // 체크 대상
    //    }
    //    else // 수정 모드
    //    {
    //        ColumnRegist.Visibility = Visibility.Collapsed;
    //        ColumnModify.Visibility = Visibility.Visible;
    //    }
    //}
    //public Order_ReceiptWnd(string sTelNo)
    //{
    //    InitializeComponent();

    //    TBoxHeader_Search.Text = sTelNo;

    //    // 신규 등록 모드
    //    ColumnRegist.Visibility = Visibility.Visible;
    //    ColumnModify.Visibility = Visibility.Collapsed;

    //    RadioBtn_편도.IsChecked = true; // 체크 대상
    //}

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // TmpHide
        //this.TBoxHeader_Search.Focus();

        //if (tbOrderOrg != null) // 업데이트 모드면 오더정보 로드
        //{
        //    TbOrderOrgToUiData();
        //    의뢰자정보Mode(tbOrderOrg.CallCustCodeK);
        //}
        //else // 신규 등록 모드
        //{
        //    if (string.IsNullOrEmpty(TBoxHeader_Search.Text)) 의뢰자정보Mode(0);
        //    else
        //    {
        //        var ee = new KeyEventArgs(
        //            Keyboard.PrimaryDevice,
        //            PresentationSource.FromVisual(TBoxHeader_Search), // 대상 컨트롤 기준
        //            0,
        //            Key.Enter
        //        )
        //        {
        //            RoutedEvent = Keyboard.KeyDownEvent
        //        };

        //        // 이벤트 핸들러 직접 호출
        //        TBoxHeader_Search_KeyDown(TBoxHeader_Search, ee);
        //    }
        //}
    }

    private void Window_Unloaded(object sender, RoutedEventArgs e)
    {
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

    #region Click - 주버튼(등록그룹)
    // 의뢰자 고객등록 버튼
    private void BtnReg_CustRegist_Click(object sender, RoutedEventArgs e)
    {

    }

    // 접수저장
    private async void BtnReg_SaveReceipt_Click(object sender, RoutedEventArgs e)
    {
        // TmpHide
        //if (!CanSave()) return; // 필수입력 체크

        //MakeEmptyBasicNewTbOrder("접수");
        //UpdateNewTbOrderByUiData();

        //StdResult_Long result = await s_SrGClient.SrResult_Order_InsertRowAsync_Today(tbOrderNew);
        //if (!string.IsNullOrEmpty(result.sErr)) // 에러가 발생하면
        //{
        //    FormFuncs.ErrMsgBox($"접수저장 실패: {result.sErr}", "Order_ReceiptWnd/BtnSaveReceipt_Click_01");
        //    return;
        //}
        ////MsgBox($"접수 저장되었습니다: {result.lResult}", "접수저장"); // Test

        //this.Close();
    }

    // 대기저장
    private async void BtnReg_SaveWait_Click(object sender, RoutedEventArgs e)
    {
        // TmpHide
        //if (!CanSave()) return; // 필수입력 체크

        //MakeEmptyBasicNewTbOrder("대기");
        //UpdateNewTbOrderByUiData();

        //StdResult_Long result = await s_SrGClient.SrResult_Order_InsertRowAsync_Today(tbOrderNew);
        //if (!string.IsNullOrEmpty(result.sErr)) // 에러가 발생하면
        //{
        //    ErrMsgBox($"대기저장 실패: {result.sErr}");
        //    return;
        //}
        ////MsgBox($"대기 저장되었습니다: {result.lResult}", "접수저장"); // Test

        //this.Close();
    }

    // 문의
    private void BtnMod_Ask_Click(object sender, RoutedEventArgs e)
    {

    }
    #endregion

    #region Click - 주버튼(수정그룹)
    // 배차
    private void BtnMod_Allocation_Click(object sender, RoutedEventArgs e)
    {

    }

    // 송장인쇄
    private void BtnMod_PrintBill_Click(object sender, RoutedEventArgs e)
    {

    }

    // 처리완료
    private void BtnMod_Finish_Click(object sender, RoutedEventArgs e)
    {
        if (tbOrderOrg == null || tbOrderOrg.DriverCode == 0)
        {
            ErrMsgBox("기사정보가 없읍니다.");
            return;
        }

        sStatus_OrderSave = "완료";
        GridOrderState.Background = (Brush)FindResource("AppBrushLightFinish");
    }

    // 대기
    private void BtnMod_Wait_Click(object sender, RoutedEventArgs e)
    {
        sStatus_OrderSave = "대기";
        GridOrderState.Background = (Brush)FindResource("AppBrushLightWait");
    }

    // BtnMod_Cancel_Click
    private void BtnMod_Cancel_Click(object sender, RoutedEventArgs e)
    {
        sStatus_OrderSave = "취소";
        GridOrderState.Background = (Brush)FindResource("AppBrushLightCancel");
    }

    // 접수상태
    private void BtnMod_Receipt_Click(object sender, RoutedEventArgs e)
    {
        sStatus_OrderSave = "접수";
        GridOrderState.Background = (Brush)FindResource("AppBrushLightReceipt");
    }

    // 저장
    private async void BtnMod_Save_Click(object sender, RoutedEventArgs e)
    {
        // TmpHide
        //if (!CanSave()) return; // 필수입력 체크

        //MakeCopiedNewTbOrder();
        //UpdateNewTbOrderByUiData(sStatus_OrderSave);

        //int resultChk = WhereUpdatableChanged();
        //if (resultChk <= 0)
        //{
        //    WarnMsgBox($"수정한 데이타가 없거나, 오류({resultChk})가 있읍니다.");
        //    return;
        //}
        ////MsgBox($"수정한 데이타가 있읍니다: {resultChk}"); // Test

        //StdResult_Int result = await s_SrGClient.SrResult_Order_UpdateRowAsync_Today(tbOrderNew);
        //if (result.nResult < 0) // 에러가 발생하면
        //{
        //    FormFuncs.ErrMsgBox($"접수 에러발생: {result.sErrNPos}", "BtnMod_Save_Click_01");
        //    return;
        //}
        ////MsgBox($"수정 저장되었습니다: {result.bResult}, TaxBill={tbOrderNew.TaxBill}", "수정저장"); // Test

        //this.Close();
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
        TBoxOrderRemarks.Text = string.Empty;
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
    private void DtPickerReserve_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            DtPickerReserve.IsOpen = false; // Enter키를 누르면 DateTimePicker 닫기
        }
    }

    // 의뢰자
    private async void TBoxHeader_Search_KeyDown(object sender, KeyEventArgs e)
    {
        // TmpHide
        //if (e.Key != Key.Enter) return; // Enter키가 눌러야 한다.
        //if (string.IsNullOrEmpty(TBoxHeader_Search.Text)) return; // 검색어가 없으면 리턴

        //// 고객정보를 검색한다.
        //PostgResult_AllWithList result = await s_SrGClient.SrResult_CustMainWith_Cust_Center_Comp_SelectRowsAsync_BySlash(TBoxHeader_Search.Text, null);
        //if (!string.IsNullOrEmpty(result.sErr)) // 에러가 발생하면
        //{
        //    ErrMsgBox($"에러발생: {result.sErr}", "Order_ReceiptWnd/TBoxHeader_Search_KeyDown_01");
        //    return;
        //}
        ////MsgBox($"검색결과: {result.listTbAll.Count}건, {result.listTbAll[0].custMain.CustName}, {result.listTbAll[0].company.CompName},{result.listTbAll[0].company.KeyCode}"); // Test
        ////MsgBox($"검색결과: {result.listTbAll.Count}건, {result.listTbAll[0].custMain.CustName}, {result.listTbAll[0].custMain.ChargeName},{result.listTbAll[0].custMain.CompCode}"); // Test


        //long lCallCustCode = 0;

        //// 검색결과
        //if (result.listTbAll == null || result.listTbAll.Count == 0) // 검색된 고객이 없으면 신규 고객등록
        //{
        //    CustMain_RegEditWnd wnd = new CustMain_RegEditWnd();
        //    bool bResult = (bool)SafeShowDialog.WithMainWindowToOwner(wnd, this);

        //    if (bResult != true) return; // 취소했으면 리턴

        //    // 신규 고객등록이 완료되면 정보를 가져온다.
        //    TbAllTo의뢰자(wnd.tbAllWithNew);
        //    lCallCustCode = wnd.tbAllWithNew.custMain.KeyCode;
        //}
        //else
        //{
        //    if (result.listTbAll.Count == 1) // 검색된 고객이 하나면.
        //    {
        //        // 첫번째 고객정보를 가져온다.
        //        TbAllWith tbAll = result.listTbAll[0];
        //        TbAllTo의뢰자(tbAll);
        //        lCallCustCode = tbAll.custMain.KeyCode;
        //    }
        //    else // 검색된 고객이 여러개면 - 선택하게 한다
        //    {
        //        CustMain_SearchedWnd wnd = new CustMain_SearchedWnd(result.listTbAll);
        //        bool bResult = (bool)SafeShowDialog.WithMainWindowToOwner(wnd, this);

        //        if (bResult != true) return; // 고객정보를 선택 안했으면

        //        // 선택된 고객정보를 가져온다.
        //        TbAllWith tbAll = wnd.SelectedVM.tbAllWith;
        //        //MsgBox($"검색결과: {tbAll.custMain.CustName}, {tbAll.company.CompName},{tbAll.company.KeyCode}, {tbAll.custMain.CompCode}"); // Test

        //        TbAllTo의뢰자(tbAll);
        //        lCallCustCode = tbAll.custMain.KeyCode;
        //    }
        //}

        //의뢰자정보Mode(lCallCustCode);
        //Keyboard.Focus(Start_TBoxSearch);
    }

    // 출발지
    private async void Start_TBoxSearch_KeyDown(object sender, KeyEventArgs e)
    {
        // TmpHide
        //if (e.Key != Key.Enter) return; // Enter키가 눌러야 한다.
        //if (string.IsNullOrEmpty(Start_TBoxSearch.Text)) // 검색어가 없으면 의뢰자정보
        //{
        //    의뢰자CopyTo출발지();
        //    Keyboard.Focus(Dest_TBoxSearch);

        //    return;
        //}

        //// 고객정보를 검색한다.
        //PostgResult_AllWithList result = await s_SrGClient.SrResult_CustMainWith_Cust_Center_Comp_SelectRowsAsync_BySlash(Start_TBoxSearch.Text, true);
        //if (!string.IsNullOrEmpty(result.sErr)) // 에러가 발생하면
        //{
        //    ErrMsgBox($"에러발생: {result.sErr}", "Order_ReceiptWnd/Start_TBoxSearch_KeyDown_02");
        //    return;
        //}

        //long lStartCustCode = 0;

        //// 검색결과
        //if (result.listTbAll.Count == 0) // 검색된 고객이 없으면 신규 고객등록
        //{
        //    CustMain_RegEditWnd wnd = new CustMain_RegEditWnd();
        //    bool bResult = (bool)SafeShowDialog.WithMainWindowToOwner(wnd, this);

        //    if (bResult != true) return; // 취소했으면 리턴

        //    // 신규 고객등록이 완료되면 정보를 가져온다.
        //    TbAllTo출발지(wnd.tbAllWithNew);
        //    lStartCustCode = wnd.tbAllWithNew.custMain.KeyCode;
        //}
        //else
        //{
        //    if (result.listTbAll.Count == 1) // 검색된 고객이 하나면.
        //    {
        //        // 첫번째 고객정보를 가져온다.
        //        TbAllWith tbAll = result.listTbAll[0];
        //        TbAllTo출발지(tbAll);
        //        lStartCustCode = tbAll.custMain.KeyCode;
        //    }
        //    else // 검색된 고객이 여러개면 - 선택하게 한다
        //    {
        //        CustMain_SearchedWnd wnd = new CustMain_SearchedWnd(result.listTbAll);
        //        bool bResult = (bool)SafeShowDialog.WithMainWindowToOwner(wnd, this);

        //        if (bResult != true) return; // 고객정보를 선택 안했으면

        //        // 선택된 고객정보를 가져온다.
        //        TbAllWith tbAll = wnd.SelectedVM.tbAllWith;
        //        TbAllTo출발지(tbAll);
        //        lStartCustCode = tbAll.custMain.KeyCode;
        //    }
        //}

        //Keyboard.Focus(Dest_TBoxSearch);
    }

    // 도착지
    private async void Dest_TBoxSearch_KeyDown(object sender, KeyEventArgs e)
    {
        // TmpHide
        //if (e.Key != Key.Enter) return; // Enter키가 눌러야 한다.
        //if (string.IsNullOrEmpty(Dest_TBoxSearch.Text)) // 검색어가 없으면 의뢰자정보
        //{
        //    의뢰자CopyTo도착지();
        //    Keyboard.Focus(TBoxFee_Basic); // 기본요금으로 이동

        //    return;
        //}

        //// 고객정보를 검색한다.
        //PostgResult_AllWithList result = await s_SrGClient.SrResult_CustMainWith_Cust_Center_Comp_SelectRowsAsync_BySlash(Dest_TBoxSearch.Text, true);
        //if (!string.IsNullOrEmpty(result.sErr)) // 에러가 발생하면
        //{
        //    ErrMsgBox($"에러발생: {result.sErr}", "Order_ReceiptWnd/TBoxHeader_Search_KeyDown_03");
        //    return;
        //}

        //long lDestCustCode = 0;

        //// 검색결과
        //if (result.listTbAll.Count == 0) // 검색된 고객이 없으면 신규 고객등록
        //{
        //    CustMain_RegEditWnd wnd = new CustMain_RegEditWnd();
        //    bool bResult = (bool)SafeShowDialog.WithMainWindowToOwner(wnd, this);

        //    if (bResult != true) return; // 취소했으면 리턴

        //    // 신규 고객등록이 완료되면 정보를 가져온다.
        //    TbAllTo도착지(wnd.tbAllWithNew);
        //    lDestCustCode = wnd.tbAllWithNew.custMain.KeyCode;
        //}
        //else
        //{
        //    if (result.listTbAll.Count == 1) // 검색된 고객이 하나면.
        //    {
        //        // 첫번째 고객정보를 가져온다.
        //        TbAllWith tbAll = result.listTbAll[0];
        //        TbAllTo도착지(tbAll);
        //        lDestCustCode = tbAll.custMain.KeyCode;
        //    }
        //    else // 검색된 고객이 여러개면 - 선택하게 한다
        //    {
        //        CustMain_SearchedWnd wnd = new CustMain_SearchedWnd(result.listTbAll);
        //        bool bResult = (bool)SafeShowDialog.WithMainWindowToOwner(wnd, this);

        //        if (bResult != true) return; // 고객정보를 선택 안했으면

        //        // 선택된 고객정보를 가져온다.
        //        TbAllWith tbAll = wnd.SelectedVM.tbAllWith;
        //        TbAllTo도착지(tbAll);
        //        lDestCustCode = tbAll.custMain.KeyCode;
        //    }
        //}

        //Keyboard.Focus(TBoxFee_Basic); // 기본요금으로 이동
    }

    // BasicFee
    private void Fee_TBoxBasic_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Keyboard.Focus(TBoxFee_Plus); // 추가요금으로 이동
            e.Handled = true; // 이벤트가 더 이상 상위/하위로 전달되지 않음
        }
    }

    // FeePlus
    private void Fee_TBoxPlus_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Keyboard.Focus(TBoxFee_Minus); // 할인요금으로 이동
            e.Handled = true; // 이벤트가 더 이상 상위/하위로 전달되지 않음
        }
    }

    // FeePlus
    private void Fee_TBoxMinus_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Keyboard.Focus(TBoxFee_Conn); // 탁송요금으로 이동
            e.Handled = true; // 이벤트가 더 이상 상위/하위로 전달되지 않음
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
            // 렌더링이 끝난 뒤 SelectAll을 실행
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
            // 렌더링이 끝난 뒤 SelectAll을 실행
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
        int FeeBasic = StdConvert.StringWonFormatToInt(TBoxFee_Basic.Text); // 기본요금
        int FeePlus = StdConvert.StringWonFormatToInt(TBoxFee_Plus.Text); // 추가요금
        int FeeMinus = StdConvert.StringWonFormatToInt(TBoxFee_Minus.Text); // 할인요금
        int FeeConn = StdConvert.StringWonFormatToInt(TBoxFee_Conn.Text); // 탁송요금

        int FeeTot = FeeBasic + FeePlus - FeeMinus + FeeConn;

        TBoxFee_Basic.Text = StdConvert.IntToStringWonFormat(FeeBasic);
        TBoxFee_Tot.Text = StdConvert.IntToStringWonFormat(FeeTot);
    }
    private void Fee_TBoxPlus_LostFocus(object sender, RoutedEventArgs e)
    {
        int FeeBasic = StdConvert.StringWonFormatToInt(TBoxFee_Basic.Text); // 기본요금
        int FeePlus = StdConvert.StringWonFormatToInt(TBoxFee_Plus.Text); // 추가요금
        int FeeMinus = StdConvert.StringWonFormatToInt(TBoxFee_Minus.Text); // 할인요금
        int FeeConn = StdConvert.StringWonFormatToInt(TBoxFee_Conn.Text); // 탁송요금

        int FeeTot = FeeBasic + FeePlus - FeeMinus + FeeConn;

        TBoxFee_Plus.Text = StdConvert.IntToStringWonFormat(FeePlus);
        TBoxFee_Tot.Text = StdConvert.IntToStringWonFormat(FeeTot);
    }
    private void Fee_TBoxMinus_LostFocus(object sender, RoutedEventArgs e)
    {
        int FeeBasic = StdConvert.StringWonFormatToInt(TBoxFee_Basic.Text); // 기본요금
        int FeePlus = StdConvert.StringWonFormatToInt(TBoxFee_Plus.Text); // 추가요금
        int FeeMinus = StdConvert.StringWonFormatToInt(TBoxFee_Minus.Text); // 할인요금
        int FeeConn = StdConvert.StringWonFormatToInt(TBoxFee_Conn.Text); // 탁송요금

        int FeeTot = FeeBasic + FeePlus - FeeMinus + FeeConn;

        TBoxFee_Minus.Text = StdConvert.IntToStringWonFormat(FeeMinus);
        TBoxFee_Tot.Text = StdConvert.IntToStringWonFormat(FeeTot);
    }
    private void Fee_TBoxConn_LostFocus(object sender, RoutedEventArgs e)
    {
        int FeeBasic = StdConvert.StringWonFormatToInt(TBoxFee_Basic.Text); // 기본요금
        int FeePlus = StdConvert.StringWonFormatToInt(TBoxFee_Plus.Text); // 추가요금
        int FeeMinus = StdConvert.StringWonFormatToInt(TBoxFee_Minus.Text); // 할인요금
        int FeeConn = StdConvert.StringWonFormatToInt(TBoxFee_Conn.Text); // 탁송요금

        int FeeTot = FeeBasic + FeePlus - FeeMinus + FeeConn;

        TBoxFee_Conn.Text = StdConvert.IntToStringWonFormat(FeeConn);
        TBoxFee_Tot.Text = StdConvert.IntToStringWonFormat(FeeTot);
    }
    #endregion

    #region RadioBtn Events
    private void RadioBtn_트럭_Checked(object sender, RoutedEventArgs e)
    {
        CmbBoxCarWeight.IsEnabled = true;
        CmbBoxCarWeight.SelectedIndex = 1; // 강제로 1톤으로...
        CmbBoxTruckDetail.IsEnabled = true;
    }

    private void RadioBtn_트럭_Unchecked(object sender, RoutedEventArgs e)
    {
        CmbBoxCarWeight.IsEnabled = false;
        CmbBoxCarWeight.SelectedIndex = 0;
        CmbBoxTruckDetail.IsEnabled = false;
        CmbBoxTruckDetail.SelectedIndex = 0;
    } 
    #endregion

    #region Tmp
    private void BtnAppend_OrderRemarks_Click(object sender, RoutedEventArgs e)
    {

    }

    #endregion

    #region EtcEvents
    private void TBoxOnlyNum_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        // TmpHide
        //// 정규식: 숫자만 허용
        //e.Handled = s_RegexOnlyNum.IsMatch(e.Text);  // true면 입력 차단
    }

    private void TBoxOnlyNum_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        // TmpHide
        //if (e.DataObject.GetDataPresent(DataFormats.Text))
        //{
        //    string text = e.DataObject.GetData(DataFormats.Text) as string;
        //    if (!s_RegexOnlyNum.IsMatch(text)) // 숫자만 허용
        //    {
        //        e.CancelCommand();
        //    }
        //}
        //else
        //{
        //    e.CancelCommand();
        //}
    }
    #endregion

    #region Funcs
    // 필수입력 체크
    private bool CanSave()
    {
        // 의뢰자
        if (CallCustCodeK == 0) // 의뢰자 KeyCode
        {
            ErrMsgBox("등록된 의뢰자가 아닙니다.");
            return false;
        }
        if (string.IsNullOrEmpty(Caller_TBoxCustName.Text)) // 의뢰자 고객명
        {
            ErrMsgBox("의뢰자 고객명을 입력하세요.");
            return false;
        }
        if (string.IsNullOrEmpty(Caller_TBoxDongBasic.Text)) // 의뢰자 동명
        {
            ErrMsgBox("의뢰자 동명을 입력하세요.");
            return false;
        }
        if (string.IsNullOrEmpty(Caller_TBoxTelNo1.Text) && string.IsNullOrEmpty(Caller_TBoxTelNo2.Text)) // 의뢰자 전화번호
        {
            ErrMsgBox("의뢰자 전화번호를 입력하세요.");
            return false;
        }
        if (Caller_TBoxTelNo1.Text == Caller_TBoxTelNo2.Text) // 의뢰자 전화번호
        {
            ErrMsgBox("의뢰자 전화번호가 중복되었습니다.");
            return false;
        }

        // 출발지
        if (string.IsNullOrEmpty(Start_TBoxCustName.Text)) // 출발지 고객명
        {
            ErrMsgBox("출발지 고객명을 입력하세요.");
            return false;
        }
        if (string.IsNullOrEmpty(Start_TBoxDongBasic.Text)) // 출발지 동명
        {
            ErrMsgBox("출발지 동명을 입력하세요.");
            return false;
        }
        if (string.IsNullOrEmpty(Start_TBoxTelNo1.Text) && string.IsNullOrEmpty(Start_TBoxTelNo2.Text)) // 출발지 전화번호
        {
            ErrMsgBox("출발지 전화번호를 입력하세요.");
            return false;
        }

        // 도착지
        if (string.IsNullOrEmpty(Dest_TBoxDongBasic.Text)) // 도착지 동명
        {
            ErrMsgBox("도착지 동명을 입력하세요.");
            return false;
        }

        // 요금
        if (StdConvert.StringWonFormatToInt(TBoxFee_Tot.Text) == 0) // 요금
        {
            ErrMsgBox("요금을 입력하세요.");
            return false;
        }

        return true;
    }

    // 요금타입
    private string GetFeeType()
    {
        if (RadioBtn_선불.IsChecked == true) return "선불";
        if (RadioBtn_착불.IsChecked == true) return "착불";
        if (RadioBtn_신용.IsChecked == true) return "신용";
        if (RadioBtn_송금.IsChecked == true) return "송금";
        if (RadioBtn_수금.IsChecked == true) return "수금";
        if (RadioBtn_카드.IsChecked == true) return "카드";

        return "";
    }
    private void SetFeeType(string sFee)
    {
        switch (sFee)
        {
            case "착불": RadioBtn_착불.IsChecked = true; break;
            case "신용": RadioBtn_신용.IsChecked = true; break;
            case "송금": RadioBtn_송금.IsChecked = true; break;
            case "수금": RadioBtn_수금.IsChecked = true; break;
            case "카드": RadioBtn_카드.IsChecked = true; break;
            default: RadioBtn_선불.IsChecked = true; break;
        }
    }

    // 차량타입
    private string GetCarType()
    {
        if (RadioBtn_오토.IsChecked == true) return "오토";
        if (RadioBtn_밴.IsChecked == true) return "밴";
        if (RadioBtn_플렉.IsChecked == true) return "플렉스";
        if (RadioBtn_다마.IsChecked == true) return "다마스";
        if (RadioBtn_라보.IsChecked == true) return "라보";
        if (RadioBtn_트럭.IsChecked == true) return "트럭";

        return "";
    }
    private void SetCarType(string sCar)
    {
        //Debug.WriteLine($"SetCarType: {sCar}");

        switch (sCar)
        {
            case "오토": RadioBtn_오토.IsChecked = true; break;
            case "밴": RadioBtn_밴.IsChecked = true; break;
            case "플렉": 
            case "플렉스": RadioBtn_플렉.IsChecked = true; break;
            case "다마":
            case "다마스": RadioBtn_다마.IsChecked = true; break;
            case "라보": RadioBtn_라보.IsChecked = true; break;
            case "트럭": RadioBtn_트럭.IsChecked = true; break;
        }
    }

    // 배송타입
    private string GetDeliverType()
    {
        if (RadioBtn_편도.IsChecked == true) return "편도";
        if (RadioBtn_왕복.IsChecked == true) return "왕복";
        if (RadioBtn_경유.IsChecked == true) return "경유";
        if (RadioBtn_긴급.IsChecked == true) return "긴급";

        return "";
    }
    private void SetDeliverType(string sDeliver)
    {
        switch (sDeliver) // 배송타입
        {
            case "편도": RadioBtn_편도.IsChecked = true; break;
            case "왕복": RadioBtn_왕복.IsChecked = true; break;
            case "경유": RadioBtn_경유.IsChecked = true; break;
            case "긴급": RadioBtn_긴급.IsChecked = true; break;
        }
    }

    //private void TbOrderOrgToUiData()
    //{
    //    // Header, 공용
    //    TBlkOrderState.Text = tbOrderOrg.OrderState;
    //    GridOrderState.Background = tbOrderOrg.OrderState switch
    //    {
    //        "접수" => (Brush)Application.Current.Resources["AppBrushLightReceipt"],
    //        "대기" => (Brush)Application.Current.Resources["AppBrushLightWait"],
    //        "배차" => (Brush)Application.Current.Resources["AppBrushLightAlloc"],
    //        "예약" => (Brush)Application.Current.Resources["AppBrushLightReserve"],
    //        "운행" => (Brush)Application.Current.Resources["AppBrushLightRun"],
    //        "완료" => (Brush)Application.Current.Resources["AppBrushLightFinish"],
    //        "취소" => (Brush)Application.Current.Resources["AppBrushLightCancel"],
    //        _ => (Brush)Application.Current.Resources["AppBrushLightTotal"], // 전체 - 해당없음.
    //    };

    //    // 의뢰자 정보 설정
    //    CallCustCodeK = StdConvert.NullableLongToLong(tbOrderOrg.CallCustCodeK);
    //    CallCustCodeE = StdConvert.NullableLongToLong(tbOrderOrg.CallCustCodeE);
    //    TBlkSeqNo.Text = tbOrderOrg.KeyCode.ToString();
    //    Caller_TBoxCustName.Text = tbOrderOrg.CallCustName;
    //    Caller_TBoxChargeName.Text = tbOrderOrg.CallChargeName;
    //    Caller_TBoxDeptName.Text = tbOrderOrg.CallDeptName;
    //    Caller_TBoxDongBasic.Text = tbOrderOrg.CallDongBasic;
    //    Caller_TBoxTelNo1.Text = StdConvert.ToPhoneNumberFormat(tbOrderOrg.CallTelNo);
    //    Caller_TBoxTelNo2.Text = StdConvert.ToPhoneNumberFormat(tbOrderOrg.CallTelNo2);
    //    Caller_TBoxDongAddr.Text = tbOrderOrg.CallAddress;
    //    Caller_TBoxDetailAddr.Text = tbOrderOrg.CallDetailAddr;
    //    Caller_TBoxRemarks.Text = tbOrderOrg.CallRemarks;

    //    TBoxOrderRemarks.Text = tbOrderOrg.OrderRemarks;
    //    TBlkCallCustMemoExt.Text = tbOrderOrg.OrderMemoExt;

    //    // 출발지 정보 설정
    //    StartCustCodeK = StdConvert.NullableLongToLong(tbOrderOrg.StartCustCodeK);
    //    StartCustCodeE = StdConvert.NullableLongToLong(tbOrderOrg.StartCustCodeE);
    //    Start_TBoxCustName.Text = tbOrderOrg.StartCustName;
    //    Start_TBoxChargeName.Text = tbOrderOrg.StartChargeName;
    //    Start_TBoxDeptName.Text = tbOrderOrg.StartDeptName;
    //    Start_TBoxDongBasic.Text = tbOrderOrg.StartDongBasic;
    //    Start_TBoxTelNo1.Text = StdConvert.ToPhoneNumberFormat(tbOrderOrg.StartTelNo);
    //    Start_TBoxTelNo2.Text = StdConvert.ToPhoneNumberFormat(tbOrderOrg.StartTelNo2);
    //    Start_TBoxDongAddr.Text = tbOrderOrg.StartAddress;
    //    Start_TBoxDetailAddr.Text = tbOrderOrg.StartDetailAddr;

    //    // 도착지 정보 설정
    //    DestCustCodeK = StdConvert.NullableLongToLong(tbOrderOrg.DestCustCodeK);
    //    DestCustCodeE = StdConvert.NullableLongToLong(tbOrderOrg.DestCustCodeE);
    //    Dest_TBoxCustName.Text = tbOrderOrg.DestCustName;
    //    Dest_TBoxChargeName.Text = tbOrderOrg.DestChargeName;
    //    Dest_TBoxDeptName.Text = tbOrderOrg.DestDeptName;
    //    Dest_TBoxDongBasic.Text = tbOrderOrg.DestDongBasic;
    //    Dest_TBoxTelNo1.Text = StdConvert.ToPhoneNumberFormat(tbOrderOrg.DestTelNo);
    //    Dest_TBoxTelNo2.Text = StdConvert.ToPhoneNumberFormat(tbOrderOrg.DestTelNo2);
    //    Dest_TBoxDongAddr.Text = tbOrderOrg.DestAddress;
    //    Dest_TBoxDetailAddr.Text = tbOrderOrg.DestDetailAddr;

    //    // 우측 상단
    //    if (tbOrderOrg.DtReserve != null) // 예약일시가 있으면
    //    {
    //        ChkBoxReserve.IsChecked = true;
    //        DtPickerReserve.Value = tbOrderOrg.DtReserve;
    //        TBoxReserveBreakMin.Text = tbOrderOrg.ReserveBreakMinute.ToString();
    //    }

    //    SetFeeType(tbOrderOrg.FeeType); // 요금타입
    //    SetCarType(tbOrderOrg.CarType);// 차량타입

    //    SetComboBoxItemByContent(CmbBoxCarWeight, tbOrderOrg.CarWeight); // 차량중량
    //    SetComboBoxItemByContent(CmbBoxTruckDetail, tbOrderOrg.TruckDetail); // 차량상세

    //    SetDeliverType(tbOrderOrg.DeliverType); // 배송타입

    //    // 우측 중간 - 요금
    //    TBoxFee_Basic.Text = StdConvert.IntToStringWonFormat(tbOrderOrg.FeeBasic);
    //    TBoxFee_Plus.Text = StdConvert.IntToStringWonFormat(tbOrderOrg.FeePlus);
    //    TBoxFee_Minus.Text = StdConvert.IntToStringWonFormat(tbOrderOrg.FeeMinus);
    //    TBoxFee_Conn.Text = StdConvert.IntToStringWonFormat(tbOrderOrg.FeeConn);
    //    TBoxFee_Driver.Text = StdConvert.IntToStringWonFormat(tbOrderOrg.FeeDriver);
    //    TBoxFee_DrvCharge.Text = StdConvert.IntToStringWonFormat(tbOrderOrg.FeeCharge);
    //    TBoxFee_Tot.Text = StdConvert.IntToStringWonFormat(tbOrderOrg.FeeTotal);

    //    ChkBoxShareOrder.IsChecked = tbOrderOrg.Share;
    //    ChkBoxTaxBill.IsChecked = tbOrderOrg.TaxBill;

    //    CallCustFrom = tbOrderOrg.CallCustFrom;
    //    CallCompCode = tbOrderOrg.CallCompCode;
    //    CallCompName = tbOrderOrg.CallCompName;
    //}

    //private void UpdateNewTbOrderByUiData(string orderState = "")
    //{
    //    //tbOrderNew.KeyCode = 0; // MakeEmptyBasicNewTable에서 미리 작성
    //    //tbOrderNew.TodayCode = 1;  // MakeEmptyBasicNewTable에서 미리 작성
    //    //tbOrderNew.MemberCode = s_CenterCharge.MemberCode; // MakeEmptyBasicNewTable에서 미리 작성
    //    //tbOrderNew.CenterCode = s_CenterCharge.CenterCode; // MakeEmptyBasicNewTable에서 미리 작성
    //    //tbOrderNew.DtRegDate = ; // DB에서 자동입력
    //   if (!string.IsNullOrEmpty(orderState)) tbOrderNew.OrderState = orderState;  // MakeEmptyBasicNewTable에서 미리 작성
    //    //tbOrderNew.OrderStateOld = ""; // MakeEmptyBasicNewTable에서 미리 작성
    //    tbOrderNew.OrderRemarks = TBoxOrderRemarks.Text; // UiData
    //    tbOrderNew.OrderMemo = TBoxOrderMemo.Text; // UiData
    //    tbOrderNew.OrderMemoExt = TBlkCallCustMemoExt.Text; // UiData - 지금은 넘어감.
    //    //tbOrderNew.UserCode = ; // 필요 없을것 같음.
    //    //tbOrderNew.UserName = ; // 필요 없을것 같음.
    //    //tbOrderNew.Updater = s_CenterCharge.Id; // MakeEmptyBasicNewTable에서 미리 작성
    //    //tbOrderNew.UpdateDate = null; // 신규등록시엔 필요없음.
    //    //tbOrderNew.CallCustCodeE = 0; // MakeEmptyBasicNewTable에서 미리 작성

    //    // Tmp-01
    //    tbOrderNew.CallCompCode = CallCompCode;
    //    tbOrderNew.CallCompName = CallCompName;
    //    tbOrderNew.CallCustCodeK = CallCustCodeK; // MakeEmptyBasicNewTable에서 미리 작성
    //    tbOrderNew.CallCustFrom = CallCustFrom;
    //    tbOrderNew.CallCustName = Caller_TBoxCustName.Text; // UiData
    //    tbOrderNew.CallTelNo = StdConvert.MakePhoneNumberToDigit(Caller_TBoxTelNo1.Text); // UiData
    //    tbOrderNew.CallTelNo2 = StdConvert.MakePhoneNumberToDigit(Caller_TBoxTelNo2.Text); // UiData
    //    tbOrderNew.CallDeptName = Caller_TBoxDeptName.Text; // UiData
    //    tbOrderNew.CallChargeName = Caller_TBoxChargeName.Text; // UiData
    //    tbOrderNew.CallDongBasic = Caller_TBoxDongBasic.Text; // UiData
    //    tbOrderNew.CallAddress = Caller_TBoxDongAddr.Text; // UiData
    //    tbOrderNew.CallDetailAddr = Caller_TBoxDetailAddr.Text; // UiData
    //    tbOrderNew.CallRemarks = Caller_TBoxRemarks.Text; // UiData
    //    //tbOrderNew.StartCustCodeE = 0; // MakeEmptyBasicNewTable에서 미리 작성
    //    tbOrderNew.StartCustCodeK = StartCustCodeK; // MakeEmptyBasicNewTable에서 미리 작성
    //    tbOrderNew.StartCustName = Start_TBoxCustName.Text;
    //    tbOrderNew.StartTelNo = StdConvert.MakePhoneNumberToDigit(Start_TBoxTelNo1.Text); // UiData
    //    tbOrderNew.StartTelNo2 = StdConvert.MakePhoneNumberToDigit(Start_TBoxTelNo2.Text); // UiData
    //    tbOrderNew.StartDeptName = Start_TBoxDeptName.Text; // UiData
    //    tbOrderNew.StartChargeName = Start_TBoxChargeName.Text; // UiData
    //    tbOrderNew.StartDongBasic = Start_TBoxDongBasic.Text; // UiData
    //    tbOrderNew.StartAddress = Start_TBoxDongAddr.Text; // UiData
    //    tbOrderNew.StartDetailAddr = Start_TBoxDetailAddr.Text; // UiData
    //    //tbOrderNew.StartSiDo = ; // 연구과제
    //    //tbOrderNew.StartGunGu = ; // 연구과제
    //    //tbOrderNew.StartDongRi = ; // 연구과제
    //    //tbOrderNew.StartLon = ; // 연구과제
    //    //tbOrderNew.StartLat = ; // 연구과제
    //    //tbOrderNew.StartSign = ; // 연구과제
    //    //tbOrderNew.StartSignDayTime = ; // 연구과제
    //    tbOrderNew.DestCustCodeE = 0; 
    //    tbOrderNew.DestCustCodeK = DestCustCodeK; 
    //    tbOrderNew.DestCustName = Dest_TBoxCustName.Text; // UiData
    //    tbOrderNew.DestTelNo = StdConvert.MakePhoneNumberToDigit(Dest_TBoxTelNo1.Text); // UiData
    //    tbOrderNew.DestTelNo2 = StdConvert.MakePhoneNumberToDigit(Dest_TBoxTelNo2.Text); // UiData
    //    tbOrderNew.DestDeptName = Dest_TBoxDeptName.Text; // UiData
    //    tbOrderNew.DestChargeName = Dest_TBoxChargeName.Text; // UiData
    //    tbOrderNew.DestDongBasic = Dest_TBoxDongBasic.Text; // UiData
    //    tbOrderNew.DestAddress = Dest_TBoxDongAddr.Text; // UiData
    //    tbOrderNew.DestDetailAddr = Dest_TBoxDetailAddr.Text; // UiData
    //    //tbOrderNew.DestSiDo = ; // 연구과제
    //    //tbOrderNew.DestGunGu = ; // 연구과제
    //    //tbOrderNew.DestDongRi = ; // 연구과제
    //    //tbOrderNew.DestLon = ; // 연구과제
    //    //tbOrderNew.DestLat = ; // 연구과제
    //    //tbOrderNew.DestSign = ; // 연구과제
    //    //tbOrderNew.DestSignDayTime = ; // 연구과제
    //    tbOrderNew.DtReserve = DtPickerReserve.Value; // UiData
    //    tbOrderNew.ReserveBreakMinute = StdConvert.StringToInt(TBoxReserveBreakMin.Text); // UiData
    //    tbOrderNew.FeeBasic = StdConvert.StringWonFormatToInt(TBoxFee_Basic.Text); // UiData
    //    tbOrderNew.FeePlus = StdConvert.StringWonFormatToInt(TBoxFee_Plus.Text); // UiData
    //    tbOrderNew.FeeMinus = StdConvert.StringWonFormatToInt(TBoxFee_Minus.Text); // UiData
    //    tbOrderNew.FeeConn = StdConvert.StringWonFormatToInt(TBoxFee_Conn.Text); // UiData
    //    tbOrderNew.FeeDriver = StdConvert.StringToInt(TBoxFee_Driver.Text); // UiData
    //    tbOrderNew.FeeCharge = StdConvert.StringWonFormatToInt(TBoxFee_DrvCharge.Text); // UiData
    //    tbOrderNew.FeeTotal = StdConvert.StringWonFormatToInt(TBoxFee_Tot.Text); // UiData
    //    tbOrderNew.FeeType = GetFeeType(); // UiData
    //    tbOrderNew.CarType = GetCarType(); // UiData
    //    tbOrderNew.CarWeight = GetSelectedComboBoxContent(CmbBoxCarWeight); // UiData
    //    tbOrderNew.TruckDetail = GetSelectedComboBoxContent(CmbBoxTruckDetail); // UiData
    //    tbOrderNew.DeliverType = GetDeliverType(); // UiData
    //    //tbOrderNew.DriverCode = ; // UiData - 연구과제
    //    //tbOrderNew.DriverId = ; // UiData - 연구과제
    //    //tbOrderNew.DriverName = ; // UiData - 연구과제
    //    //tbOrderNew.DriverTelNo = ; // UiData - 연구과제
    //    //tbOrderNew.DriverMemberCode = ; // UiData
    //    //tbOrderNew.DriverCenterId = ; // UiData - 연구과제
    //    //tbOrderNew.DriverCenterName = ; // UiData - 연구과제
    //    //tbOrderNew.DriverBusinessNo = ; // UiData - 연구과제
    //    //tbOrderNew.CustFrom = ""; // MakeEmptyBasicNewTable에서 미리 작성
    //    //tbOrderNew.OrderFrom = ""; // MakeEmptyBasicNewTable에서 미리 작성
    //    //tbOrderNew.Insung1 = ""; // MakeEmptyBasicNewTable에서 미리 작성
    //    //tbOrderNew.Insung2 = ""; // MakeEmptyBasicNewTable에서 미리 작성
    //    //tbOrderNew.Cargo24 = ""; // MakeEmptyBasicNewTable에서 미리 작성
    //    //tbOrderNew.Onecall = ""; // MakeEmptyBasicNewTable에서 미리 작성

    //    tbOrderNew.Share = (bool)ChkBoxShareOrder.IsChecked;
    //    tbOrderNew.TaxBill = (bool)ChkBoxTaxBill.IsChecked;
    //}
    //private void MakeEmptyBasicNewTbOrder(string sOrderState)
    //{
    //    tbOrderNew = new TbOrder();

    //    tbOrderNew.KeyCode = 0;
    //    tbOrderNew.MemberCode = s_CenterCharge.MemberCode;
    //    tbOrderNew.CenterCode = s_CenterCharge.CenterCode;
    //    //tbOrderNew.DtRegDate = ; // DB에서 자동입력
    //    tbOrderNew.ReceiptTime = TimeOnly.FromDateTime(DateTime.Now);
    //    tbOrderNew.OrderState = sOrderState;
    //    tbOrderNew.OrderStateOld = "";
    //    tbOrderNew.OrderRemarks = ""; // UiData
    //    tbOrderNew.OrderMemo = ""; // UiData
    //    tbOrderNew.OrderMemoExt = ""; // UiData
    //    tbOrderNew.UserCode = 0; // 필요 없을것 같음.
    //    tbOrderNew.UserName = ""; // 필요 없을것 같음.
    //    tbOrderNew.Updater = s_CenterCharge.Id;
    //    //tbOrderNew.UpdateDate = null;
    //    tbOrderNew.CallCompCode = 0;
    //    tbOrderNew.CallCompName = "";
    //    tbOrderNew.CallCustFrom = "";
    //    tbOrderNew.CallCustCodeE = 0;
    //    tbOrderNew.CallCustCodeK = CallCustCodeK;
    //    if (tbOrderNew.CallCustCodeK <= 0) ErrMsgBox($"의뢰자코드가 0입니다.");
    //    tbOrderNew.CallCustName = ""; // UiData
    //    tbOrderNew.CallTelNo = ""; // UiData
    //    tbOrderNew.CallTelNo2 = ""; // UiData
    //    tbOrderNew.CallDeptName = ""; // UiData
    //    tbOrderNew.CallChargeName = ""; // UiData
    //    tbOrderNew.CallDongBasic = ""; // UiData
    //    tbOrderNew.CallAddress = ""; // UiData
    //    tbOrderNew.CallDetailAddr = ""; // UiData
    //    tbOrderNew.CallRemarks = ""; // UiData
    //    tbOrderNew.StartCustCodeE = 0;
    //    tbOrderNew.StartCustCodeK = StartCustCodeK;
    //    tbOrderNew.StartCustName = ""; // UiData
    //    tbOrderNew.StartTelNo = ""; // UiData
    //    tbOrderNew.StartTelNo2 = ""; // UiData
    //    tbOrderNew.StartDeptName = ""; // UiData
    //    tbOrderNew.StartChargeName = ""; // UiData
    //    tbOrderNew.StartDongBasic = ""; // UiData
    //    tbOrderNew.StartAddress = ""; // UiData
    //    tbOrderNew.StartDetailAddr = ""; // UiData
    //    tbOrderNew.StartSiDo = ""; // UiData
    //    tbOrderNew.StartGunGu = ""; // UiData
    //    tbOrderNew.StartDongRi = ""; // UiData
    //    tbOrderNew.StartLon = 0; // 연구과제
    //    tbOrderNew.StartLat = 0; // 연구과제
    //    //tbOrderNew.StartSignImg = null; // 연구과제
    //    //tbOrderNew.StartDtSign = null; // 연구과제
    //    tbOrderNew.DestCustCodeE = 0;
    //    tbOrderNew.DestCustCodeK = DestCustCodeK;
    //    tbOrderNew.DestCustName = ""; // UiData
    //    tbOrderNew.DestTelNo = ""; // UiData
    //    tbOrderNew.DestTelNo2 = ""; // UiData
    //    tbOrderNew.DestDeptName = ""; // UiData
    //    tbOrderNew.DestChargeName = ""; // UiData
    //    tbOrderNew.DestDongBasic = ""; // UiData
    //    tbOrderNew.DestAddress = ""; // UiData
    //    tbOrderNew.DestDetailAddr = ""; // UiData
    //    tbOrderNew.DestSiDo = ""; // UiData
    //    tbOrderNew.DestGunGu = ""; // UiData
    //    tbOrderNew.DestDongRi = ""; // UiData
    //    tbOrderNew.DestLon = 0; // 연구과제
    //    tbOrderNew.DestLat = 0; // 연구과제
    //    //tbOrderNew.DestSignImg = null; // 연구과제
    //    //tbOrderNew.DestDtSign = null; // 연구과제
    //    //tbOrderNew.DtReserve = null; // UiData
    //    tbOrderNew.ReserveBreakMinute = 0; // UiData
    //    tbOrderNew.FeeBasic = 0; // UiData
    //    tbOrderNew.FeePlus = 0; // UiData
    //    tbOrderNew.FeeMinus = 0; // UiData
    //    tbOrderNew.FeeConn = 0; // UiData
    //    tbOrderNew.FeeDriver = 0; // UiData
    //    tbOrderNew.FeeCharge = 0; // UiData
    //    tbOrderNew.FeeTotal = 0; // UiData
    //    tbOrderNew.FeeType = ""; // UiData
    //    tbOrderNew.CarType = ""; // UiData
    //    tbOrderNew.CarWeight = ""; // UiData
    //    tbOrderNew.TruckDetail = ""; // UiData
    //    tbOrderNew.DeliverType = ""; // UiData
    //    tbOrderNew.DriverCode = 0; // UiData
    //    tbOrderNew.DriverId = ""; // UiData
    //    tbOrderNew.DriverName = ""; // UiData
    //    tbOrderNew.DriverTelNo = ""; // UiData
    //    tbOrderNew.DriverMemberCode = 0; // UiData
    //    tbOrderNew.DriverCenterId = ""; // UiData
    //    tbOrderNew.DriverCenterName = ""; // UiData
    //    tbOrderNew.DriverBusinessNo = ""; // UiData
    //    tbOrderNew.Insung1 = "";
    //    tbOrderNew.Insung2 = "";
    //    tbOrderNew.Cargo24 = "";
    //    tbOrderNew.Onecall = "";
    //    tbOrderNew.Share = false;
    //    tbOrderNew.TaxBill = false;
    //    //tbOrderNew.ReceiptTime = null;
    //    //tbOrderNew.AllocTime = null;
    //    //tbOrderNew.RunTime = null;
    //    //tbOrderNew.FinishTime = null;
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

        CallCustCodeK = tbCustMain.KeyCode;
        CallCustCodeE = StdConvert.NullableLongToLong(tbCustMain.BeforeCustKey);
        CallCustFrom = tbCustMain.BeforeBelong;
        CallCompCode = tbCustMain.CompCode;
        if (tbCompany == null) CallCompName = "";
        else
        {
            CallCompName = tbCompany.CompName;
            SetFeeType(tbCompany.TradeType);
        }

        Caller_TBoxCustName.Text = tbCustMain.CustName;
        Caller_TBoxDongBasic.Text = tbCustMain.DongBasic;
        Caller_TBoxTelNo1.Text = tbCustMain.TelNo1;
        Caller_TBoxTelNo2.Text = tbCustMain.TelNo2;
        Caller_TBoxDeptName.Text = tbCustMain.DeptName;
        Caller_TBoxChargeName.Text = tbCustMain.ChargeName;
        Caller_TBoxDongAddr.Text = tbCustMain.DongAddr;
        Caller_TBoxDetailAddr.Text = tbCustMain.DetailAddr;
        TBoxOrderRemarks.Text = Caller_TBoxRemarks.Text = tbCustMain.Remarks;

        TBlkCallCustMemoExt.Text = tbCustMain.Memo;
    }

    private void TbAllTo출발지(TbAllWith tb)
    {
        TbCustMain tbCustMain = tb.custMain;
        TbCompany tbCompany = tb.company;
        TbCallCenter tbCallCenter = tb.callCenter;

        StartCustCodeK = tbCustMain.KeyCode;
        StartCustCodeE = StdConvert.NullableLongToLong(tbCustMain.BeforeCustKey);

        Start_TBoxCustName.Text = tbCustMain.CustName;
        Start_TBoxDongBasic.Text = tbCustMain.DongBasic;
        Start_TBoxTelNo1.Text = tbCustMain.TelNo1;
        Start_TBoxTelNo2.Text = tbCustMain.TelNo2;
        Start_TBoxDeptName.Text = tbCustMain.DeptName;
        Start_TBoxChargeName.Text = tbCustMain.ChargeName;
        Start_TBoxDongAddr.Text = tbCustMain.DongAddr;
        Start_TBoxDetailAddr.Text = tbCustMain.DetailAddr;
    }
    private void 의뢰자CopyTo출발지()
    {
        StartCustCodeK = CallCustCodeK;
        StartCustCodeE = CallCustCodeE;

        Start_TBoxCustName.Text = Caller_TBoxCustName.Text;
        Start_TBoxChargeName.Text = Caller_TBoxChargeName.Text;
        Start_TBoxDeptName.Text = Caller_TBoxDeptName.Text;
        Start_TBoxDongBasic.Text = Caller_TBoxDongBasic.Text;
        Start_TBoxTelNo1.Text = Caller_TBoxTelNo1.Text;
        Start_TBoxTelNo2.Text = Caller_TBoxTelNo2.Text;
        Start_TBoxDongAddr.Text = Caller_TBoxDongAddr.Text;
        Start_TBoxDetailAddr.Text = Caller_TBoxDetailAddr.Text;
    }

    private void TbAllTo도착지(TbAllWith tb)
    {
        TbCustMain tbCustMain = tb.custMain;
        TbCompany tbCompany = tb.company;
        TbCallCenter tbCallCenter = tb.callCenter;

        DestCustCodeK = tbCustMain.KeyCode;
        DestCustCodeE = StdConvert.NullableLongToLong(tbCustMain.BeforeCustKey);

        Dest_TBoxCustName.Text = tbCustMain.CustName;
        Dest_TBoxDongBasic.Text = tbCustMain.DongBasic;
        Dest_TBoxTelNo1.Text = tbCustMain.TelNo1;
        Dest_TBoxTelNo2.Text = tbCustMain.TelNo2;
        Dest_TBoxDeptName.Text = tbCustMain.DeptName;
        Dest_TBoxChargeName.Text = tbCustMain.ChargeName;
        Dest_TBoxDongAddr.Text = tbCustMain.DongAddr;
        Dest_TBoxDetailAddr.Text = tbCustMain.DetailAddr;
    }
    private void 의뢰자CopyTo도착지()
    {
        DestCustCodeK = CallCustCodeK;
        DestCustCodeE = CallCustCodeE;

        Dest_TBoxCustName.Text = Caller_TBoxCustName.Text;
        Dest_TBoxChargeName.Text = Caller_TBoxChargeName.Text;
        Dest_TBoxDeptName.Text = Caller_TBoxDeptName.Text;
        Dest_TBoxDongBasic.Text = Caller_TBoxDongBasic.Text;
        Dest_TBoxTelNo1.Text = Caller_TBoxTelNo1.Text;
        Dest_TBoxTelNo2.Text = Caller_TBoxTelNo2.Text;
        Dest_TBoxDongAddr.Text = Caller_TBoxDongAddr.Text;
        Dest_TBoxDetailAddr.Text = Caller_TBoxDetailAddr.Text;
    }

    private void 의뢰자정보Mode(long lCustKey = 0)
    {
        if (lCustKey == 0) // 없음
        {
            GridCustInfo.Visibility = Visibility.Collapsed;
            BorderCustNew.Visibility = Visibility.Visible;
        }
        else // 있음
        {
            BtnReg_CustRegist.Visibility = Visibility.Collapsed;
            BtnReg_CustUpdate.Visibility = Visibility.Visible;

            GridCustInfo.Visibility = Visibility.Visible;
            BorderCustNew.Visibility = Visibility.Collapsed;

            // 고객정보 로드
        }
    }
    private void 의뢰자정보Mode(TbCustMain tbCust = null)
    {
        if (tbCust == null) // 없음
        {
            GridCustInfo.Visibility = Visibility.Collapsed;
            BorderCustNew.Visibility = Visibility.Visible;
        }
        else // 있음
        {
            BtnReg_CustRegist.Visibility = Visibility.Collapsed;
            BtnReg_CustUpdate.Visibility = Visibility.Visible;

            GridCustInfo.Visibility = Visibility.Visible;
            BorderCustNew.Visibility = Visibility.Collapsed;

            // 고객정보 로드
        }
    }
    #endregion
}
#nullable enable


