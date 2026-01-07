// Xceed Extended WPF Toolkit Nuget - For DateTimePicker(3.8.1 만 무료)

using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;

using Kai.Common.StdDll_Common;
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
    #region 저장, 수정, 생성
    // 필수입력 체크
    private bool CanSave()
    {
        // 의뢰자
        if (vmOrder.CallCustCodeK == 0) // 의뢰자 KeyCode
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
        if (vmOrder.FeeTotal == 0)
        {
            ErrMsgBox("요금을 입력하세요.");
            return false;
        }

        return true;
    }

    // 주문 저장 공통 헬퍼 (접수/대기 저장 등)
    private async Task<bool> SaveOrderAsync(string orderState, string actionName)
    {
        // 1. 필수입력 체크
        if (!CanSave()) return false;

        // 2. 예약 유효성 체크
        if (!IsValidReserve())
        {
            ErrMsgBox("예약시간이 현재시간보다 이후여야 합니다.", "Order_ReceiptWnd/SaveOrderAsync");
            return false;
        }

        // 3. 기본 필드 설정 및 UI 데이터 반영
        SetBasicFieldsForNewOrder(orderState);
        UpdateNewTbOrderByUiData();

        // 3. 서버에 저장
        StdResult_Long result = await s_SrGClient.SrResult_Order_InsertRowAsync_Today(vmOrder.tbOrder);
        if (!string.IsNullOrEmpty(result.sErr))
        {
            ErrMsgBox($"{actionName} 실패: {result.sErr}", $"Order_ReceiptWnd/SaveOrderAsync_{orderState}");
            return false;
        }

        // 4. 성공
        return true;
    }

    // 주문 수정 저장 헬퍼 메서드
    private async Task<bool> UpdateOrderAsync()
    {
        // 1. 필수 입력 체크
        if (!CanSave()) return false;

        // 2. 예약 유효성 체크
        if (!IsValidReserve())
        {
            ErrMsgBox("예약시간이 현재시간보다 이후여야 합니다.", "Order_ReceiptWnd/UpdateOrderAsync");
            return false;
        }

        // 3. UI 데이터를 vmOrder에 반영 (바인딩 안된 필드들)
        UpdateNewTbOrderByUiData(sStatus_OrderSave);

        // 4. 변경 여부 확인
        if (!IsChanged())
        {
            return true; // 변경 없으면 저장 안함
        }

        // 5. DtUpdateLast 정보 설정
        vmOrder.DtUpdateLast = DateTime.Now;

        // 6. 서버로 업데이트 전송
        StdResult_Int result = await s_SrGClient.SrResult_Order_UpdateRowAsync_Today(vmOrder.tbOrder);
        if (result.nResult < 0)
        {
            ErrMsgBox($"수정 저장 실패: {result.sErrNPos}", "Order_ReceiptWnd/UpdateOrderAsync");
            return false;
        }

        // 5. 성공
        return true;
    }

    private void UpdateNewTbOrderByUiData(string orderState = "")
    {
        if (!string.IsNullOrEmpty(orderState)) vmOrder.OrderState = orderState;

        // CodeK 필드들은 vmOrder에 직접 저장되므로 복사 불필요

        // 예약 관련 (DtReserve, ReserveBreakMinute는 바인딩 안됨)
        vmOrder.DtReserve = DtPickerReserve.Value;
        vmOrder.ReserveBreakMinute = StdConvert.StringToInt(TBoxReserveBreakMin.Text);
        Debug.WriteLine($"[저장] Reserve={vmOrder.Reserve}, DtReserve={vmOrder.DtReserve}, BreakMin={vmOrder.ReserveBreakMinute}");

        // 요금 관련 (바인딩 안된 필드)
        vmOrder.FeeCommi = StdConvert.StringWonFormatToInt(TBox_FeeCharge.Text);
        vmOrder.FeeDriver = StdConvert.StringWonFormatToInt(TBox_FeeDriver.Text);
        vmOrder.FeeType = GetFeeTypeFromUI();
        vmOrder.MovilityFlag = GetCarTypeFromUI();
        vmOrder.DeliverFlag = GetDeliverFlagFromUI();
    }

    // 신규 등록 시 기본값 설정 (vmOrder는 이미 생성자에서 생성됨, UI 바인딩 유지)
    private void SetBasicFieldsForNewOrder(string sOrderState)
    {
        if (vmOrder.CallCustCodeK <= 0) ErrMsgBox($"의뢰자코드가 0입니다.");

        // 기본 필드 설정 (UI 바인딩 안된 필드들)
        vmOrder.KeyCode = 0;
        vmOrder.MemberCode = s_CenterCharge.MemberCode;
        vmOrder.CenterCode = s_CenterCharge.CenterCode;
        vmOrder.ReceiptTime = TimeOnly.FromDateTime(DateTime.Now);
        vmOrder.OrderState = sOrderState;
        vmOrder.OrderStateOld = "";
        vmOrder.UserCode = 0;
        vmOrder.UserName = "";

        // 인성 등 외부연동 필드 초기화
        vmOrder.Insung1SeqNo = "";
        vmOrder.Insung2SeqNo = "";
        vmOrder.Cargo24SeqNo = "";
        vmOrder.OnecallSeqNo = "";
        vmOrder.Share = false;
        vmOrder.TaxBill = false;

        // 기사 정보 초기화 (신규 등록 시 배차 전)
        vmOrder.DriverCode = 0;
        vmOrder.DriverId = "";
        vmOrder.DriverName = "";
        vmOrder.DriverTelNo = "";
        vmOrder.DriverMemberCode = 0;
        vmOrder.DriverCenterId = "";
        vmOrder.DriverCenterName = "";
        vmOrder.DriverBusinessNo = "";

        // UI 바인딩된 필드들은 건드리지 않음 (이미 사용자가 입력한 값 유지)
    }
    #endregion

    #region 변경 비교
    private bool IsChanged()
    {
        if (vmOrder == null) return true;
        return vmOrder.IsChanged;
    }
    #endregion

    #region 예약
    private bool IsValidReserve()
    {
        if (ChkBoxReserve.IsChecked != true) return true;

        if (DtPickerReserve.Value == null || DtPickerReserve.Value <= DateTime.Now)
            return false;

        return true;
    }
    #endregion

    #region 요금타입
    private string GetFeeTypeFromUI()
    {
        if (RadioBtn_선불.IsChecked == true) return "선불";
        if (RadioBtn_착불.IsChecked == true) return "착불";
        if (RadioBtn_신용.IsChecked == true) return "신용";
        if (RadioBtn_송금.IsChecked == true) return "송금";
        if (RadioBtn_카드.IsChecked == true) return "카드";

        return "";
    }
    private void SetFeeTypeToUI(string sFee)
    {
        switch (sFee)
        {
            case "착불": RadioBtn_착불.IsChecked = true; break;
            case "신용": RadioBtn_신용.IsChecked = true; break;
            case "송금": RadioBtn_송금.IsChecked = true; break;
            case "카드": RadioBtn_카드.IsChecked = true; break;
            default: RadioBtn_선불.IsChecked = true; break;
        }
    }
    #endregion

    #region 차량타입
    private string GetCarTypeFromUI()
    {
        if (RadioBtn_오토.IsChecked == true) return "오토";
        if (RadioBtn_밴.IsChecked == true) return "밴";
        if (RadioBtn_플렉.IsChecked == true) return "플렉스";
        if (RadioBtn_다마.IsChecked == true) return "다마스";
        if (RadioBtn_라보.IsChecked == true) return "라보";
        if (RadioBtn_트럭.IsChecked == true) return "트럭";

        return "";
    }

    private void SetCarTypeToUI(string sCar)
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
    #endregion

    #region 배송타입 (비트 플래그: 편도=1, 왕복=2, 경유=4, 긴급=8, 혼적=16)
    private const int FLAG_편도 = 1;
    private const int FLAG_왕복 = 2;
    private const int FLAG_경유 = 4;
    private const int FLAG_긴급 = 8;
    private const int FLAG_혼적 = 16;

    private string GetDeliverFlagFromUI()
    {
        int flag = 0;
        if (ChkBox_편도.IsChecked == true) flag |= FLAG_편도;
        if (ChkBox_왕복.IsChecked == true) flag |= FLAG_왕복;
        if (ChkBox_경유.IsChecked == true) flag |= FLAG_경유;
        if (ChkBox_긴급.IsChecked == true) flag |= FLAG_긴급;
        if (ChkBox_혼적.IsChecked == true) flag |= FLAG_혼적;
        return flag.ToString();
    }

    private void SetDeliverFlagToUI(string sFlag)
    {
        int flag = int.TryParse(sFlag, out int result) ? result : 0;

        ChkBox_편도.IsChecked = (flag & FLAG_편도) != 0;
        ChkBox_왕복.IsChecked = (flag & FLAG_왕복) != 0;
        ChkBox_경유.IsChecked = (flag & FLAG_경유) != 0;
        ChkBox_긴급.IsChecked = (flag & FLAG_긴급) != 0;
        ChkBox_혼적.IsChecked = (flag & FLAG_혼적) != 0;
    }
    #endregion

    #region 출발지/도착지 데이터 복사
    private void TbAllTo출발지(TbAllWith tbAllWith)
    {
        if (tbAllWith == null || tbAllWith.custMain == null) return;

        SetLocationDataToUi(tbAllWith.custMain, "출발지");
        Start_TBoxSearch.Text = tbAllWith.custMain.CustName;
    }

    private void 의뢰자CopyTo출발지()
    {
        // 의뢰자 데이터를 출발지로 복사
        vmOrder.StartCustCodeK = vmOrder.CallCustCodeK;
        vmOrder.StartCustCodeE = vmOrder.CallCustCodeE;
        vmOrder.StartCustName = vmOrder.CallCustName;
        vmOrder.StartDongBasic = vmOrder.CallDongBasic;
        vmOrder.StartTelNo = vmOrder.CallTelNo;
        vmOrder.StartTelNo2 = vmOrder.CallTelNo2;
        vmOrder.StartDeptName = vmOrder.CallDeptName;
        vmOrder.StartChargeName = vmOrder.CallChargeName;
        vmOrder.StartAddress = vmOrder.CallAddress;
        vmOrder.StartDetailAddr = vmOrder.CallDetailAddr;
        Start_TBoxSearch.Text = vmOrder.CallCustName;
    }

    private void TbAllTo도착지(TbAllWith tbAllWith)
    {
        if (tbAllWith == null || tbAllWith.custMain == null) return;

        SetLocationDataToUi(tbAllWith.custMain, "도착지");
        Dest_TBoxSearch.Text = tbAllWith.custMain.CustName;
    }

    private void 의뢰자CopyTo도착지()
    {
        // 의뢰자 데이터를 도착지로 복사
        vmOrder.DestCustCodeK = vmOrder.CallCustCodeK;
        vmOrder.DestCustCodeE = vmOrder.CallCustCodeE;
        vmOrder.DestCustName = vmOrder.CallCustName;
        vmOrder.DestDongBasic = vmOrder.CallDongBasic;
        vmOrder.DestTelNo = vmOrder.CallTelNo;
        vmOrder.DestTelNo2 = vmOrder.CallTelNo2;
        vmOrder.DestDeptName = vmOrder.CallDeptName;
        vmOrder.DestChargeName = vmOrder.CallChargeName;
        vmOrder.DestAddress = vmOrder.CallAddress;
        vmOrder.DestDetailAddr = vmOrder.CallDetailAddr;
        Dest_TBoxSearch.Text = vmOrder.CallCustName;
    }
    #endregion





    #region Funcs
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
    // DataContext 바인딩으로 대부분 자동 표시, 바인딩 안된 필드만 수동 설정
    private void TbOrderOrgToUiData()
    {
        if (vmOrder == null) return;

        // 1. Header (KeyCode 표시)
        SetHeaderInfoToUI();

        // 2. 의뢰자/출발지/도착지 - DataContext 바인딩으로 자동 표시

        // 3. 의뢰자 메모 (바인딩 없음)
        TBlkCallCustMemoExt.Text = "";

        // 4. 예약 정보 (일부 바인딩 없음)
        SetReserveInfoToUI();

        // 5. 차량/배송 타입 (RadioButton - 바인딩 없음)
        SetVehicleInfoToUI();

        // 6. 요금 정보 (바인딩 없음)
        SetFeeInfoToUI();
    }

    // Header 정보 로드
    private void SetHeaderInfoToUI()
    {
        TBlkSeqNo.Text = vmOrder.KeyCode.ToString();
    }

    // 예약 정보 로드
    private void SetReserveInfoToUI()
    {
        Debug.WriteLine($"[로드] Reserve={vmOrder.Reserve}, DtReserve={vmOrder.DtReserve}, BreakMin={vmOrder.ReserveBreakMinute}");
        ChkBoxReserve.DataContext = vmOrder; // 바인딩 연결
        if (vmOrder.Reserve)
        {
            DtPickerReserve.Value = vmOrder.DtReserve;
            TBoxReserveBreakMin.Text = vmOrder.ReserveBreakMinute.ToString();
            DtPickerReserve.IsEnabled = true;
            TBoxReserveBreakMin.IsEnabled = true;
        }
        else
        {
            DtPickerReserve.IsEnabled = false;
            TBoxReserveBreakMin.IsEnabled = false;
        }
    }

    // 차량/배송 타입 정보 로드
    private void SetVehicleInfoToUI()
    {
        SetFeeTypeToUI(vmOrder.FeeType);
        SetCarTypeToUI(vmOrder.MovilityFlag);
        SetDeliverFlagToUI(vmOrder.DeliverFlag);
    }

    // 요금 정보 로드
    private void SetFeeInfoToUI()
    {
        TBox_FeeBasic.Text = StdConvert.IntToStringWonFormat(vmOrder.FeeBasic);
        TBox_FeePlus.Text = StdConvert.IntToStringWonFormat(vmOrder.FeePlus);
        TBox_FeeMinus.Text = StdConvert.IntToStringWonFormat(vmOrder.FeeMinus);
        TBox_FeeConn.Text = StdConvert.IntToStringWonFormat(vmOrder.FeeConn);
        TBox_FeeCharge.Text = StdConvert.IntToStringWonFormat(vmOrder.FeeCommi);
        TBox_FeeDriver.Text = StdConvert.IntToStringWonFormat(vmOrder.FeeDriver);
        TBox_FeeTot.Text = StdConvert.IntToStringWonFormat(vmOrder.FeeTotal);
    }









    //private void MakeCopiedNewTbOrder() // UI데이타 빼고 복사
    //{
    //    if (vmOrder == null)
    //    {
    //        ErrMsgBox("복사할 테이블이 없읍니다.");
    //        return;
    //    }

    //    vmOrder = new TbOrder();

    //    vmOrder.KeyCode = vmOrder.KeyCode;
    //    vmOrder.MemberCode = s_CenterCharge.MemberCode;
    //    vmOrder.CenterCode = s_CenterCharge.CenterCode;
    //    vmOrder.DtRegist = vmOrder.DtRegist;
    //    vmOrder.OrderState = vmOrder.OrderState;
    //    vmOrder.OrderStateOld = vmOrder.OrderStateOld;
    //    vmOrder.OrderRemarks = vmOrder.OrderRemarks; // UiData
    //    vmOrder.OrderMemo = vmOrder.OrderMemo; // UiData
    //    vmOrder.OrderMemoExt = vmOrder.OrderMemoExt; // UiData
    //    vmOrder.UserCode = vmOrder.UserCode; // 필요 없을것 같음.
    //    vmOrder.UserName = vmOrder.UserName; // 필요 없을것 같음.
    //    vmOrder.Updater = s_CenterCharge.Id;
    //    vmOrder.UpdateDate = DateTime.Now.ToString(StdConst_Var.DTFORMAT_EXCEPT_SEC);

    //    vmOrder.CallCompCode = vmOrder.CallCompCode;
    //    vmOrder.CallCompName = vmOrder.CallCompName;
    //    vmOrder.CallCustFrom = vmOrder.CallCustFrom;
    //    vmOrder.CallCustCodeE = vmOrder.CallCustCodeE;
    //    vmOrder.CallCustCodeK = CallCustCodeK;
    //    if (vmOrder.CallCustCodeK <= 0) ErrMsgBox($"의뢰자코드가 0입니다.");
    //    //vmOrder.CallCustName = ; // UiData
    //    //vmOrder.CallTelNo = ; // UiData
    //    //vmOrder.CallTelNo2 = ; // UiData
    //    //vmOrder.CallDeptName = ; // UiData
    //    //vmOrder.CallChargeName = ; // UiData
    //    //vmOrder.CallDongBasic = ; // UiData
    //    //vmOrder.CallAddress = ; // UiData
    //    //vmOrder.CallDetailAddr = ; // UiData
    //    //vmOrder.CallRemarks = ; // UiData
    //    vmOrder.StartCustCodeE = 0;
    //    vmOrder.StartCustCodeK = StartCustCodeK;
    //    //vmOrder.StartCustName = ; // UiData
    //    //vmOrder.StartTelNo = ; // UiData
    //    //vmOrder.StartTelNo2 = ; // UiData
    //    //vmOrder.StartDeptName = ; // UiData
    //    //vmOrder.StartChargeName = ; // UiData
    //    //vmOrder.StartDongBasic = ; // UiData
    //    //vmOrder.StartAddress = ; // UiData
    //    //vmOrder.StartDetailAddr = ; // UiData
    //    vmOrder.StartSiDo = vmOrder.StartSiDo; // UiData
    //    vmOrder.StartGunGu = vmOrder.StartGunGu; // UiData
    //    vmOrder.StartDongRi = vmOrder.StartDongRi; // UiData
    //    vmOrder.StartLon = vmOrder.StartLon; // 연구과제
    //    vmOrder.StartLat = vmOrder.StartLat; // 연구과제
    //    vmOrder.StartSignImg = vmOrder.StartSignImg; // 연구과제
    //    vmOrder.StartDtSign = vmOrder.StartDtSign; // 연구과제
    //    vmOrder.DestCustCodeE = 0;
    //    vmOrder.DestCustCodeK = DestCustCodeK;
    //    //vmOrder.DestCustName = ; // UiData
    //    //vmOrder.DestTelNo = ; // UiData
    //    //vmOrder.DestTelNo2 = ; // UiData
    //    //vmOrder.DestDeptName = ; // UiData
    //    //vmOrder.DestChargeName = ; // UiData
    //    //vmOrder.DestDongBasic = ; // UiData
    //    //vmOrder.DestAddress = ; // UiData
    //    //vmOrder.DestDetailAddr = ; // UiData
    //    vmOrder.DestSiDo = vmOrder.DestSiDo; // UiData
    //    vmOrder.DestGunGu = vmOrder.DestGunGu; // UiData
    //    vmOrder.DestDongRi = vmOrder.DestDongRi; // UiData
    //    vmOrder.DestLon = vmOrder.DestLon; // 연구과제
    //    vmOrder.DestLat = vmOrder.DestLat; // 연구과제
    //    vmOrder.DestSignImg = vmOrder.DestSignImg; // 연구과제
    //    vmOrder.DestDtSign = vmOrder.DestDtSign; // 연구과제
    //    vmOrder.DtReserve = vmOrder.DtReserve; // UiData
    //    vmOrder.ReserveBreakMinute = vmOrder.ReserveBreakMinute; // UiData
    //    vmOrder.FeeBasic = vmOrder.FeeBasic; // UiData
    //    vmOrder.FeePlus = vmOrder.FeePlus; // UiData
    //    vmOrder.FeeMinus = vmOrder.FeeMinus; // UiData
    //    vmOrder.FeeConn = vmOrder.FeeConn; // UiData
    //    vmOrder.FeeDriver = vmOrder.FeeDriver; // UiData
    //    vmOrder.FeeCharge = vmOrder.FeeCharge; // UiData
    //    vmOrder.FeeTotal = vmOrder.FeeTotal; // UiData
    //    vmOrder.FeeType = vmOrder.FeeType; // UiData
    //    vmOrder.CarType = vmOrder.CarType; // UiData
    //    vmOrder.CarWeight = vmOrder.CarWeight; // UiData
    //    vmOrder.TruckDetail = vmOrder.TruckDetail; // UiData
    //    vmOrder.DeliverType = vmOrder.DeliverType; // UiData
    //    vmOrder.DriverCode = vmOrder.DriverCode; // UiData
    //    vmOrder.DriverId = vmOrder.DriverId; // UiData
    //    vmOrder.DriverName = vmOrder.DriverName; // UiData
    //    vmOrder.DriverTelNo = vmOrder.DriverTelNo; // UiData
    //    vmOrder.DriverMemberCode = vmOrder.DriverMemberCode; // UiData
    //    vmOrder.DriverCenterId = vmOrder.DriverCenterId; // UiData
    //    vmOrder.DriverCenterName = vmOrder.DriverCenterName; // UiData
    //    vmOrder.DriverBusinessNo = vmOrder.DriverBusinessNo; // UiData
    //    vmOrder.Insung1 = vmOrder.Insung1;
    //    vmOrder.Insung2 = vmOrder.Insung2;
    //    vmOrder.Cargo24 = vmOrder.Cargo24;
    //    vmOrder.Onecall = vmOrder.Onecall;
    //    vmOrder.Share = vmOrder.Share;
    //    vmOrder.TaxBill = vmOrder.TaxBill;
    //    vmOrder.ReceiptTime = vmOrder.ReceiptTime;
    //    vmOrder.AllocTime = vmOrder.AllocTime;
    //    vmOrder.RunTime = vmOrder.RunTime;
    //    vmOrder.FinishTime = vmOrder.FinishTime;
    //}

    //private int WhereUpdatableChanged()
    //{
    //    // 변경되면 안되는 항목
    //    if (vmOrder.KeyCode != vmOrder.KeyCode) return -1;
    //    if (vmOrder.MemberCode != vmOrder.MemberCode) return -3;
    //    if (vmOrder.CenterCode != vmOrder.CenterCode) return -4;
    //    //if (vmOrder.DtRegOrder != vmOrder.DtRegOrder) return -5;
    //    if (vmOrder.OrderStateOld != vmOrder.OrderStateOld) return -7;

    //    // 변경되면 인정되는 항목
    //    if (vmOrder.OrderRemarks != vmOrder.OrderRemarks) return 1;
    //    if (vmOrder.OrderMemo != vmOrder.OrderMemo) return 2;
    //    if (vmOrder.OrderMemoExt != vmOrder.OrderMemoExt) return 3;
    //    if (vmOrder.UserCode != vmOrder.UserCode) return 4;
    //    if (vmOrder.OrderState != vmOrder.OrderState) return 5;
    //    //if (vmOrder.UserName != vmOrder.UserName) return 5;
    //    //if (vmOrder.Updater != vmOrder.Updater) return true; // UiData
    //    //if (vmOrder.UpdateDate != vmOrder.UpdateDate) return true; // UiData
    //    if (vmOrder.CallCustCodeE != vmOrder.CallCustCodeE) return 6;
    //    if (vmOrder.CallCustCodeK != vmOrder.CallCustCodeK) return 7;
    //    if (vmOrder.CallCustName != vmOrder.CallCustName) return 8;
    //    //MsgBox($"{vmOrder.CallTelNo}, {vmOrder.CallTelNo}"); // Test
    //    if (vmOrder.CallTelNo != vmOrder.CallTelNo) return 9;
    //    if (vmOrder.CallTelNo2 != vmOrder.CallTelNo2) return 10;
    //    if (vmOrder.CallDeptName != vmOrder.CallDeptName) return 11;
    //    if (vmOrder.CallChargeName != vmOrder.CallChargeName) return 12;
    //    if (vmOrder.CallDongBasic != vmOrder.CallDongBasic) return 13;
    //    if (vmOrder.CallAddress != vmOrder.CallAddress) return 14;
    //    if (vmOrder.CallDetailAddr != vmOrder.CallDetailAddr) return 15;
    //    if (vmOrder.CallRemarks != vmOrder.CallRemarks) return 16;
    //    if (vmOrder.StartCustCodeE != vmOrder.StartCustCodeE) return 17;
    //    if (vmOrder.StartCustCodeK != vmOrder.StartCustCodeK) return 18;
    //    if (vmOrder.StartCustName != vmOrder.StartCustName) return 19;
    //    if (vmOrder.StartTelNo != vmOrder.StartTelNo) return 20;
    //    if (vmOrder.StartTelNo2 != vmOrder.StartTelNo2) return 21;
    //    if (vmOrder.StartDeptName != vmOrder.StartDeptName) return 22;
    //    if (vmOrder.StartChargeName != vmOrder.StartChargeName) return 23;
    //    if (vmOrder.StartDongBasic != vmOrder.StartDongBasic) return 24;
    //    if (vmOrder.StartAddress != vmOrder.StartAddress) return 25;
    //    if (vmOrder.StartDetailAddr != vmOrder.StartDetailAddr) return 26;
    //    //MsgBox($"/{vmOrder.StartSiDo}:{vmOrder.StartSiDo.Length}/<->/{vmOrder.StartSiDo}:{vmOrder.StartSiDo.Length}/"); // Test
    //    if (vmOrder.StartSiDo != vmOrder.StartSiDo) return 27;
    //    if (vmOrder.StartGunGu != vmOrder.StartGunGu) return 28;
    //    if (vmOrder.StartDongRi != vmOrder.StartDongRi) return 29;
    //    if (vmOrder.StartLon != vmOrder.StartLon) return 30;
    //    if (vmOrder.StartLat != vmOrder.StartLat) return 31;
    //    if (vmOrder.StartSignImg != vmOrder.StartSignImg) return 32;
    //    if (vmOrder.StartDtSign != vmOrder.StartDtSign) return 33;
    //    if (vmOrder.DestCustCodeE != vmOrder.DestCustCodeE) return 34;
    //    if (vmOrder.DestCustCodeK != vmOrder.DestCustCodeK) return 35;
    //    if (vmOrder.DestCustName != vmOrder.DestCustName) return 36;
    //    if (vmOrder.DestTelNo != vmOrder.DestTelNo) return 37;
    //    if (vmOrder.DestTelNo2 != vmOrder.DestTelNo2) return 38;
    //    if (vmOrder.DestDeptName != vmOrder.DestDeptName) return 39;
    //    if (vmOrder.DestChargeName != vmOrder.DestChargeName) return 40;
    //    if (vmOrder.DestDongBasic != vmOrder.DestDongBasic) return 41;
    //    if (vmOrder.DestAddress != vmOrder.DestAddress) return 42;
    //    if (vmOrder.DestDetailAddr != vmOrder.DestDetailAddr) return 43;
    //    if (vmOrder.DestSiDo != vmOrder.DestSiDo) return 44;
    //    if (vmOrder.DestGunGu != vmOrder.DestGunGu) return 45;
    //    if (vmOrder.DestDongRi != vmOrder.DestDongRi) return 46;
    //    if (vmOrder.DestLon != vmOrder.DestLon) return 47;
    //    if (vmOrder.DestLat != vmOrder.DestLat) return 48;
    //    if (vmOrder.DestSignImg != vmOrder.DestSignImg) return 49;
    //    if (vmOrder.DestDtSign != vmOrder.DestDtSign) return 50;
    //    if (vmOrder.DtReserve != vmOrder.DtReserve) return 51;
    //    if (vmOrder.ReserveBreakMinute != vmOrder.ReserveBreakMinute) return 52;
    //    if (vmOrder.FeeBasic != vmOrder.FeeBasic) return 53;
    //    if (vmOrder.FeePlus != vmOrder.FeePlus) return 54;
    //    if (vmOrder.FeeMinus != vmOrder.FeeMinus) return 55;
    //    if (vmOrder.FeeConn != vmOrder.FeeConn) return 56;
    //    if (vmOrder.FeeDriver != vmOrder.FeeDriver) return 57;
    //    if (vmOrder.FeeCharge != vmOrder.FeeCharge) return 58;
    //    if (vmOrder.FeeTotal != vmOrder.FeeTotal) return 59;
    //    if (vmOrder.FeeType != vmOrder.FeeType) return 60;
    //    if (vmOrder.CarType != vmOrder.CarType) return 61;
    //    if (vmOrder.CarWeight != vmOrder.CarWeight) return 62;
    //    if (vmOrder.TruckDetail != vmOrder.TruckDetail) return 63;
    //    if (vmOrder.DeliverType != vmOrder.DeliverType) return 64;
    //    if (vmOrder.DriverCode != vmOrder.DriverCode) return 65;
    //    if (vmOrder.DriverId != vmOrder.DriverId) return 66;
    //    if (vmOrder.DriverName != vmOrder.DriverName) return 67;
    //    if (vmOrder.DriverTelNo != vmOrder.DriverTelNo) return 68;
    //    if (vmOrder.DriverMemberCode != vmOrder.DriverMemberCode) return 69;
    //    if (vmOrder.DriverCenterId != vmOrder.DriverCenterId) return 70;
    //    if (vmOrder.DriverCenterName != vmOrder.DriverCenterName) return 71;
    //    if (vmOrder.DriverBusinessNo != vmOrder.DriverBusinessNo) return 72;
    //    if (vmOrder.Insung1 != vmOrder.Insung1) return 74;
    //    if (vmOrder.Insung2 != vmOrder.Insung2) return 75;
    //    if (vmOrder.Cargo24 != vmOrder.Cargo24) return 76;
    //    if (vmOrder.Onecall != vmOrder.Onecall) return 77;
    //    if (vmOrder.Share != vmOrder.Share) return 78;
    //    if (vmOrder.TaxBill != vmOrder.TaxBill) return 79;
    //    if (vmOrder.ReceiptTime != vmOrder.ReceiptTime) return 80;
    //    if (vmOrder.AllocTime != vmOrder.AllocTime) return 81;
    //    if (vmOrder.RunTime != vmOrder.RunTime) return 82;
    //    if (vmOrder.FinishTime != vmOrder.FinishTime) return 83;

    //    if (vmOrder.CallCustFrom != vmOrder.CallCustFrom) return 84;
    //    if (vmOrder.CallCompCode != vmOrder.CallCompCode) return 85;
    //    if (vmOrder.CallCompName != vmOrder.CallCompName) return 86;

    //    return 0;
    //}

    // 고객정보
    private void TbAllTo의뢰자(TbAllWith tb)
    {
        TbCustMain tbCustMain = tb.custMain;
        TbCompany tbCompany = tb.company;
        TbCallCenter tbCallCenter = tb.callCenter;

        SetLocationDataToUi(tbCustMain, "의뢰자");

        // 의뢰자 전용 필드 설정 - vmOrder 직접 사용
        vmOrder.CallCustFrom = tbCustMain.BeforeBelong;
        vmOrder.CallCompCode = tbCustMain.CompCode;
        if (tbCompany == null)
        {
            vmOrder.CallCompName = "";
        }
        else
        {
            vmOrder.CallCompName = tbCompany.CompName;
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
        }
        else // 있음
        {
            BtnReg_CustRegist.Visibility = Visibility.Collapsed;
            BtnReg_CustUpdate.Visibility = Visibility.Visible;

            // 고객정보 로드
        }
    }









    private void 의뢰자정보Mode(TbCustMain tbCust = null)
    {
        if (tbCust == null) // 없음
        {
        }
        else // 있음
        {
            BtnReg_CustRegist.Visibility = Visibility.Collapsed;
            BtnReg_CustUpdate.Visibility = Visibility.Visible;

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

    // LocationData → vmOrder 설정 (공통 헬퍼) - TbCustMain 버전
    // DataContext 바인딩으로 UI 자동 업데이트
    private void SetLocationDataToUi(TbCustMain tbCustMain, string sWhere)
    {
        switch (sWhere)
        {
            case "의뢰자":
                vmOrder.CallCustCodeK = tbCustMain.KeyCode;
                vmOrder.CallCustCodeE = StdConvert.NullableLongToLong(tbCustMain.BeforeCustKey);
                vmOrder.CallCustName = tbCustMain.CustName;
                vmOrder.CallDongBasic = tbCustMain.DongBasic;
                vmOrder.CallTelNo = tbCustMain.TelNo1;
                vmOrder.CallTelNo2 = tbCustMain.TelNo2;
                vmOrder.CallDeptName = tbCustMain.DeptName;
                vmOrder.CallChargeName = tbCustMain.ChargeName;
                vmOrder.CallAddress = tbCustMain.DongAddr;
                vmOrder.CallDetailAddr = tbCustMain.DetailAddr;
                vmOrder.CallRemarks = tbCustMain.Remarks;
                break;
            case "출발지":
                vmOrder.StartCustCodeK = tbCustMain.KeyCode;
                vmOrder.StartCustCodeE = StdConvert.NullableLongToLong(tbCustMain.BeforeCustKey);
                vmOrder.StartCustName = tbCustMain.CustName;
                vmOrder.StartDongBasic = tbCustMain.DongBasic;
                vmOrder.StartTelNo = tbCustMain.TelNo1;
                vmOrder.StartTelNo2 = tbCustMain.TelNo2;
                vmOrder.StartDeptName = tbCustMain.DeptName;
                vmOrder.StartChargeName = tbCustMain.ChargeName;
                vmOrder.StartAddress = tbCustMain.DongAddr;
                vmOrder.StartDetailAddr = tbCustMain.DetailAddr;
                break;
            case "도착지":
                vmOrder.DestCustCodeK = tbCustMain.KeyCode;
                vmOrder.DestCustCodeE = StdConvert.NullableLongToLong(tbCustMain.BeforeCustKey);
                vmOrder.DestCustName = tbCustMain.CustName;
                vmOrder.DestDongBasic = tbCustMain.DongBasic;
                vmOrder.DestTelNo = tbCustMain.TelNo1;
                vmOrder.DestTelNo2 = tbCustMain.TelNo2;
                vmOrder.DestDeptName = tbCustMain.DeptName;
                vmOrder.DestChargeName = tbCustMain.ChargeName;
                vmOrder.DestAddress = tbCustMain.DongAddr;
                vmOrder.DestDetailAddr = tbCustMain.DetailAddr;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(sWhere));
        }
    }

    // TbOrder → UI 로드 (오버로드) - 로드 시 vmOrder 데이터를 UI에 표시
    private void SetLocationDataToUi(TbOrder tb, string sWhere)
    {
        (TextBox tboxCustName, TextBox tboxDongBasic, TextBox tboxTelNo1, TextBox tboxTelNo2,
         TextBox tboxDeptName, TextBox tboxChargeName, TextBox tboxDongAddr) controls;

        string custName, dongBasic, telNo1, telNo2, deptName, chargeName, dongAddr;

        switch (sWhere)
        {
            case "의뢰자":
                controls = (Caller_TBoxCustName, Caller_TBoxDongBasic, Caller_TBoxTelNo1, Caller_TBoxTelNo2,
                            Caller_TBoxDeptName, Caller_TBoxChargeName, Caller_TBoxDongAddr);
                custName = tb.CallCustName;
                dongBasic = tb.CallDongBasic;
                telNo1 = StdConvert.ToPhoneNumberFormat(tb.CallTelNo);
                telNo2 = StdConvert.ToPhoneNumberFormat(tb.CallTelNo2);
                deptName = tb.CallDeptName;
                chargeName = tb.CallChargeName;
                dongAddr = tb.CallAddress;
                break;
            case "출발지":
                controls = (Start_TBoxCustName, Start_TBoxDongBasic, Start_TBoxTelNo1, Start_TBoxTelNo2,
                            Start_TBoxDeptName, Start_TBoxChargeName, Start_TBoxDongAddr);
                custName = tb.StartCustName;
                dongBasic = tb.StartDongBasic;
                telNo1 = StdConvert.ToPhoneNumberFormat(tb.StartTelNo);
                telNo2 = StdConvert.ToPhoneNumberFormat(tb.StartTelNo2);
                deptName = tb.StartDeptName;
                chargeName = tb.StartChargeName;
                dongAddr = tb.StartAddress;
                break;
            case "도착지":
                controls = (Dest_TBoxCustName, Dest_TBoxDongBasic, Dest_TBoxTelNo1, Dest_TBoxTelNo2,
                            Dest_TBoxDeptName, Dest_TBoxChargeName, Dest_TBoxDongAddr);
                custName = tb.DestCustName;
                dongBasic = tb.DestDongBasic;
                telNo1 = StdConvert.ToPhoneNumberFormat(tb.DestTelNo);
                telNo2 = StdConvert.ToPhoneNumberFormat(tb.DestTelNo2);
                deptName = tb.DestDeptName;
                chargeName = tb.DestChargeName;
                dongAddr = tb.DestAddress;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(sWhere));
        }

        // UI에 표시만 (vmOrder 값은 이미 로드됨)
        controls.tboxCustName.Text = custName;
        controls.tboxDongBasic.Text = dongBasic;
        controls.tboxTelNo1.Text = telNo1;
        controls.tboxTelNo2.Text = telNo2;
        controls.tboxDeptName.Text = deptName;
        controls.tboxChargeName.Text = chargeName;
        controls.tboxDongAddr.Text = dongAddr;
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