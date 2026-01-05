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
        if (StdConvert.StringWonFormatToInt(TBox_FeeTot.Text) == 0) // 요금
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

        // 2. 새 주문 객체 생성 및 데이터 설정
        MakeEmptyBasicNewTbOrder(orderState);
        UpdateNewTbOrderByUiData();

        // 3. 서버에 저장
        StdResult_Long result = await s_SrGClient.SrResult_Order_InsertRowAsync_Today(tbOrderNew);
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

        // 2. 원본 복사 및 UI 데이터 업데이트
        tbOrderNew = NetUtil.DeepCopyFrom(tbOrderOrg);
        UpdateNewTbOrderByUiData(sStatus_OrderSave);

        // 3. DtUpdateLast 정보 설정
        tbOrderNew.DtUpdateLast = DateTime.Now;

        // 4. 서버로 업데이트 전송
        StdResult_Int result = await s_SrGClient.SrResult_Order_UpdateRowAsync_Today(tbOrderNew);
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
        //tbOrderNew.KeyCode = 0; // MakeEmptyBasicNewTable에서 미리 작성
        //tbOrderNew.TodayCode = 1;  // MakeEmptyBasicNewTable에서 미리 작성
        //tbOrderNew.MemberCode = s_CenterCharge.MemberCode; // MakeEmptyBasicNewTable에서 미리 작성
        //tbOrderNew.CenterCode = s_CenterCharge.CenterCode; // MakeEmptyBasicNewTable에서 미리 작성
        //tbOrderNew.DtRegDate = ; // DB에서 자동입력
        if (!string.IsNullOrEmpty(orderState)) tbOrderNew.OrderState = orderState;  // MakeEmptyBasicNewTable에서 미리 작성
                                                                                    //tbOrderNew.OrderStateOld = ""; // MakeEmptyBasicNewTable에서 미리 작성
        // OrderMemoExt 삭제됨 - 필요시 CallMemo 사용
        //tbOrderNew.UserCode = ; // 필요 없을것 같음.
        //tbOrderNew.UserName = ; // 필요 없을것 같음.
        //tbOrderNew.Updater = s_CenterCharge.Id; // MakeEmptyBasicNewTable에서 미리 작성
        //tbOrderNew.UpdateDate = null; // 신규등록시엔 필요없음.
        //tbOrderNew.CallCustCodeE = 0; // MakeEmptyBasicNewTable에서 미리 작성

        // Tmp-01
        tbOrderNew.CallCompCode = CallCompCode;
        tbOrderNew.CallCompName = CallCompName;
        tbOrderNew.CallCustCodeK = CallCustCodeK; // MakeEmptyBasicNewTable에서 미리 작성
        tbOrderNew.CallCustFrom = CallCustFrom;
        tbOrderNew.CallCustName = Caller_TBoxCustName.Text; // UiData
        tbOrderNew.CallTelNo = StdConvert.MakePhoneNumberToDigit(Caller_TBoxTelNo1.Text); // UiData
        tbOrderNew.CallTelNo2 = StdConvert.MakePhoneNumberToDigit(Caller_TBoxTelNo2.Text); // UiData
        tbOrderNew.CallDeptName = Caller_TBoxDeptName.Text; // UiData
        tbOrderNew.CallChargeName = Caller_TBoxChargeName.Text; // UiData
        tbOrderNew.CallDongBasic = Caller_TBoxDongBasic.Text; // UiData
        tbOrderNew.CallAddress = Caller_TBoxDongAddr.Text; // UiData
        tbOrderNew.CallRemarks = Caller_TBoxRemarks.Text?.Trim(); // UiData
                                                                  //tbOrderNew.StartCustCodeE = 0; // MakeEmptyBasicNewTable에서 미리 작성
        tbOrderNew.StartCustCodeK = StartCustCodeK; // MakeEmptyBasicNewTable에서 미리 작성
        tbOrderNew.StartCustName = Start_TBoxCustName.Text;
        tbOrderNew.StartTelNo = StdConvert.MakePhoneNumberToDigit(Start_TBoxTelNo1.Text); // UiData
        tbOrderNew.StartTelNo2 = StdConvert.MakePhoneNumberToDigit(Start_TBoxTelNo2.Text); // UiData
        tbOrderNew.StartDeptName = Start_TBoxDeptName.Text; // UiData
        tbOrderNew.StartChargeName = Start_TBoxChargeName.Text; // UiData
        tbOrderNew.StartDongBasic = Start_TBoxDongBasic.Text; // UiData
        tbOrderNew.StartAddress = Start_TBoxDongAddr.Text; // UiData
        tbOrderNew.StartDetailAddr = Start_TBoxDetailAddr.Text; // UiData
        //tbOrderNew.StartSiDo = ; // 연구과제
        //tbOrderNew.StartGunGu = ; // 연구과제
        //tbOrderNew.StartDongRi = ; // 연구과제
        //tbOrderNew.StartLon = ; // 연구과제
        //tbOrderNew.StartLat = ; // 연구과제
        //tbOrderNew.StartSign = ; // 연구과제
        //tbOrderNew.StartSignDayTime = ; // 연구과제
        tbOrderNew.DestCustCodeE = 0;
        tbOrderNew.DestCustCodeK = DestCustCodeK;
        tbOrderNew.DestCustName = Dest_TBoxCustName.Text; // UiData
        tbOrderNew.DestTelNo = StdConvert.MakePhoneNumberToDigit(Dest_TBoxTelNo1.Text); // UiData
        tbOrderNew.DestTelNo2 = StdConvert.MakePhoneNumberToDigit(Dest_TBoxTelNo2.Text); // UiData
        tbOrderNew.DestDeptName = Dest_TBoxDeptName.Text; // UiData
        tbOrderNew.DestChargeName = Dest_TBoxChargeName.Text; // UiData
        tbOrderNew.DestDongBasic = Dest_TBoxDongBasic.Text; // UiData
        tbOrderNew.DestAddress = Dest_TBoxDongAddr.Text; // UiData
        tbOrderNew.DestDetailAddr = Dest_TBoxDetailAddr.Text; // UiData
        //tbOrderNew.DestSiDo = ; // 연구과제
        //tbOrderNew.DestGunGu = ; // 연구과제
        //tbOrderNew.DestDongRi = ; // 연구과제
        //tbOrderNew.DestLon = ; // 연구과제
        //tbOrderNew.DestLat = ; // 연구과제
        //tbOrderNew.DestSign = ; // 연구과제
        //tbOrderNew.DestSignDayTime = ; // 연구과제
        tbOrderNew.DtReserve = DtPickerReserve.Value; // UiData
        tbOrderNew.ReserveBreakMinute = StdConvert.StringToInt(TBoxReserveBreakMin.Text); // UiData
        tbOrderNew.FeeBasic = StdConvert.StringWonFormatToInt(TBox_FeeBasic.Text); // UiData
        tbOrderNew.FeePlus = StdConvert.StringWonFormatToInt(TBox_FeePlus.Text); // UiData
        tbOrderNew.FeeMinus = StdConvert.StringWonFormatToInt(TBox_FeeMinus.Text); // UiData
        tbOrderNew.FeeConn = StdConvert.StringWonFormatToInt(TBox_FeeConn.Text); // UiData
        tbOrderNew.FeeCommi = StdConvert.StringWonFormatToInt(TBox_FeeCharge.Text); // UiData
        tbOrderNew.FeeDriver = StdConvert.StringWonFormatToInt(TBox_FeeDriver.Text); // UiData
        tbOrderNew.FeeTotal = StdConvert.StringWonFormatToInt(TBox_FeeTot.Text); // UiData
        tbOrderNew.FeeType = Get퀵DeliverTypeFromUI(); // UiData
        tbOrderNew.MovilityFlag = GetCarTypeFromUI(); // UiData
        tbOrderNew.DeliverFlag = Get화물DeliverTypeFromUI(); // UiData
        //tbOrderNew.DriverCode = ; // UiData - 연구과제
        //tbOrderNew.DriverId = ; // UiData - 연구과제
        //tbOrderNew.DriverName = ; // UiData - 연구과제
        //tbOrderNew.DriverTelNo = ; // UiData - 연구과제
        //tbOrderNew.DriverMemberCode = ; // UiData
        //tbOrderNew.DriverCenterId = ; // UiData - 연구과제
        //tbOrderNew.DriverCenterName = ; // UiData - 연구과제
        //tbOrderNew.DriverBusinessNo = ; // UiData - 연구과제
        //tbOrderNew.CustFrom = ""; // MakeEmptyBasicNewTable에서 미리 작성
        //tbOrderNew.OrderFrom = ""; // MakeEmptyBasicNewTable에서 미리 작성
        //tbOrderNew.Insung1 = ""; // MakeEmptyBasicNewTable에서 미리 작성
        //tbOrderNew.Insung2 = ""; // MakeEmptyBasicNewTable에서 미리 작성
        //tbOrderNew.Cargo24 = ""; // MakeEmptyBasicNewTable에서 미리 작성
        //tbOrderNew.Onecall = ""; // MakeEmptyBasicNewTable에서 미리 작성
    }

    private void MakeEmptyBasicNewTbOrder(string sOrderState)
    {
        tbOrderNew = new TbOrder();

        tbOrderNew.KeyCode = 0;
        tbOrderNew.MemberCode = s_CenterCharge.MemberCode;
        tbOrderNew.CenterCode = s_CenterCharge.CenterCode;
        //tbOrderNew.DtRegDate = ; // DB에서 자동입력
        tbOrderNew.ReceiptTime = TimeOnly.FromDateTime(DateTime.Now);
        tbOrderNew.OrderState = sOrderState;
        tbOrderNew.OrderStateOld = "";
        tbOrderNew.DeliverMemo = ""; // UiData
        tbOrderNew.CallMemo = ""; // UiData
        tbOrderNew.UserCode = 0; // 필요 없을것 같음.
        tbOrderNew.UserName = ""; // 필요 없을것 같음.
        // DtUpdateLast - 신규등록시엔 기본값 사용
        //tbOrderNew.UpdateDate = null;
        tbOrderNew.CallCompCode = 0;
        tbOrderNew.CallCompName = "";
        tbOrderNew.CallCustFrom = "";
        tbOrderNew.CallCustCodeE = 0;
        tbOrderNew.CallCustCodeK = CallCustCodeK;
        if (tbOrderNew.CallCustCodeK <= 0) ErrMsgBox($"의뢰자코드가 0입니다.");
        tbOrderNew.CallCustName = ""; // UiData
        tbOrderNew.CallTelNo = ""; // UiData
        tbOrderNew.CallTelNo2 = ""; // UiData
        tbOrderNew.CallDeptName = ""; // UiData
        tbOrderNew.CallChargeName = ""; // UiData
        tbOrderNew.CallDongBasic = ""; // UiData
        tbOrderNew.CallAddress = ""; // UiData
        tbOrderNew.CallDetailAddr = ""; // UiData
        tbOrderNew.CallRemarks = ""; // UiData
        tbOrderNew.StartCustCodeE = 0;
        tbOrderNew.StartCustCodeK = StartCustCodeK;
        tbOrderNew.StartCustName = ""; // UiData
        tbOrderNew.StartTelNo = ""; // UiData
        tbOrderNew.StartTelNo2 = ""; // UiData
        tbOrderNew.StartDeptName = ""; // UiData
        tbOrderNew.StartChargeName = ""; // UiData
        tbOrderNew.StartDongBasic = ""; // UiData
        tbOrderNew.StartAddress = ""; // UiData
        tbOrderNew.StartDetailAddr = ""; // UiData
        tbOrderNew.StartSiDo = ""; // UiData
        tbOrderNew.StartGunGu = ""; // UiData
        tbOrderNew.StartDongRi = ""; // UiData
        tbOrderNew.StartLon = 0; // 연구과제
        tbOrderNew.StartLat = 0; // 연구과제
                                 //tbOrderNew.StartSignImg = null; // 연구과제
                                 //tbOrderNew.StartDtSign = null; // 연구과제
        tbOrderNew.DestCustCodeE = 0;
        tbOrderNew.DestCustCodeK = DestCustCodeK;
        tbOrderNew.DestCustName = ""; // UiData
        tbOrderNew.DestTelNo = ""; // UiData
        tbOrderNew.DestTelNo2 = ""; // UiData
        tbOrderNew.DestDeptName = ""; // UiData
        tbOrderNew.DestChargeName = ""; // UiData
        tbOrderNew.DestDongBasic = ""; // UiData
        tbOrderNew.DestAddress = ""; // UiData
        tbOrderNew.DestDetailAddr = ""; // UiData
        tbOrderNew.DestSiDo = ""; // UiData
        tbOrderNew.DestGunGu = ""; // UiData
        tbOrderNew.DestDongRi = ""; // UiData
        tbOrderNew.DestLon = 0; // 연구과제
        tbOrderNew.DestLat = 0; // 연구과제
                                //tbOrderNew.DestSignImg = null; // 연구과제
                                //tbOrderNew.DestDtSign = null; // 연구과제
                                //tbOrderNew.DtReserve = null; // UiData
        tbOrderNew.ReserveBreakMinute = 0; // UiData
        tbOrderNew.FeeBasic = 0; // UiData
        tbOrderNew.FeePlus = 0; // UiData
        tbOrderNew.FeeMinus = 0; // UiData
        tbOrderNew.FeeConn = 0; // UiData
        tbOrderNew.FeeCommi = 0; // UiData
        tbOrderNew.FeeTotal = 0; // UiData
        tbOrderNew.FeeType = ""; // UiData
        tbOrderNew.MovilityFlag = ""; // UiData
        tbOrderNew.CarWeightFlag = ""; // UiData
        tbOrderNew.TruckDetailFlag = ""; // UiData
        tbOrderNew.DeliverFlag = ""; // UiData
        tbOrderNew.DriverCode = 0; // UiData
        tbOrderNew.DriverId = ""; // UiData
        tbOrderNew.DriverName = ""; // UiData
        tbOrderNew.DriverTelNo = ""; // UiData
        tbOrderNew.DriverMemberCode = 0; // UiData
        tbOrderNew.DriverCenterId = ""; // UiData
        tbOrderNew.DriverCenterName = ""; // UiData
        tbOrderNew.DriverBusinessNo = ""; // UiData
        tbOrderNew.Insung1SeqNo = "";
        tbOrderNew.Insung2SeqNo = "";
        tbOrderNew.Cargo24SeqNo = "";
        tbOrderNew.OnecallSeqNo = "";
        tbOrderNew.Share = false;
        tbOrderNew.TaxBill = false;
        //tbOrderNew.ReceiptTime = null;
        //tbOrderNew.AllocTime = null;
        //tbOrderNew.RunTime = null;
        //tbOrderNew.FinishTime = null;
    }
    #endregion

    #region 요금타입
    private string GetFeeTypeFromUI()
    {
        if (RadioBtn_선불.IsChecked == true) return "선불";
        if (RadioBtn_착불.IsChecked == true) return "착불";
        if (RadioBtn_신용.IsChecked == true) return "신용";
        if (RadioBtn_송금.IsChecked == true) return "송금";
        if (RadioBtn_수금.IsChecked == true) return "수금";
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
            case "수금": RadioBtn_수금.IsChecked = true; break;
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

    #region 배송타입
    private string Get화물DeliverTypeFromUI()
    {
        // 화물 배송타입 체크박스 삭제됨
        return "";
    }

    private void Set화물DeliverTypeToUI(string sDeliver)
    {
        // 화물 배송타입 체크박스 삭제됨
    }

    private string Get퀵DeliverTypeFromUI()
    {
        if (ChkBox퀵_편도.IsChecked == true) return "편도";
        if (ChkBox퀵_왕복.IsChecked == true) return "왕복";
        if (ChkBox퀵_경유.IsChecked == true) return "경유";
        if (ChkBox퀵_긴급.IsChecked == true) return "긴급";
        return "";
    }
    private void Set퀵DeliverTypeToUI()
    {
        if (tbOrderOrg == null) return;

        ChkBox퀵_편도.IsChecked = false;
        ChkBox퀵_왕복.IsChecked = false;
        ChkBox퀵_경유.IsChecked = false;
        ChkBox퀵_긴급.IsChecked = false;

        switch (tbOrderOrg.FeeType)
        {
            case "편도": ChkBox퀵_편도.IsChecked = true; break;
            case "왕복": ChkBox퀵_왕복.IsChecked = true; break;
            case "경유": ChkBox퀵_경유.IsChecked = true; break;
            case "긴급": ChkBox퀵_긴급.IsChecked = true; break;
        }
    }
    #endregion

    #region 출발지/도착지 데이터 복사
    private void TbAllTo출발지(TbAllWith tbAllWith)
    {
        if (tbAllWith == null || tbAllWith.custMain == null) return;

        var cust = tbAllWith.custMain;
        StartCustCodeK = cust.KeyCode;
        Start_TBoxSearch.Text = cust.CustName;
        Start_TBoxCustName.Text = cust.CustName;
        Start_TBoxTelNo1.Text = StdConvert.ToPhoneNumberFormat(cust.TelNo1);
        Start_TBoxTelNo2.Text = StdConvert.ToPhoneNumberFormat(cust.TelNo2);
        Start_TBoxDeptName.Text = cust.DeptName;
        Start_TBoxChargeName.Text = cust.ChargeName;
        Start_TBoxDongBasic.Text = cust.DongBasic;
        Start_TBoxDongAddr.Text = cust.DongAddr;
        Start_TBoxDetailAddr.Text = cust.DetailAddr;
    }

    private void 의뢰자CopyTo출발지()
    {
        StartCustCodeK = CallCustCodeK;
        Start_TBoxSearch.Text = Caller_TBoxCustName.Text;
        Start_TBoxCustName.Text = Caller_TBoxCustName.Text;
        Start_TBoxTelNo1.Text = Caller_TBoxTelNo1.Text;
        Start_TBoxTelNo2.Text = Caller_TBoxTelNo2.Text;
        Start_TBoxDeptName.Text = Caller_TBoxDeptName.Text;
        Start_TBoxChargeName.Text = Caller_TBoxChargeName.Text;
        Start_TBoxDongBasic.Text = Caller_TBoxDongBasic.Text;
        Start_TBoxDongAddr.Text = Caller_TBoxDongAddr.Text;
        Start_TBoxDetailAddr.Text = ""; // 의뢰자에는 DetailAddr 없음
    }

    private void TbAllTo도착지(TbAllWith tbAllWith)
    {
        if (tbAllWith == null || tbAllWith.custMain == null) return;

        var cust = tbAllWith.custMain;
        DestCustCodeK = cust.KeyCode;
        Dest_TBoxSearch.Text = cust.CustName;
        Dest_TBoxCustName.Text = cust.CustName;
        Dest_TBoxTelNo1.Text = StdConvert.ToPhoneNumberFormat(cust.TelNo1);
        Dest_TBoxTelNo2.Text = StdConvert.ToPhoneNumberFormat(cust.TelNo2);
        Dest_TBoxDeptName.Text = cust.DeptName;
        Dest_TBoxChargeName.Text = cust.ChargeName;
        Dest_TBoxDongBasic.Text = cust.DongBasic;
        Dest_TBoxDongAddr.Text = cust.DongAddr;
        Dest_TBoxDetailAddr.Text = cust.DetailAddr;
    }

    private void 의뢰자CopyTo도착지()
    {
        DestCustCodeK = CallCustCodeK;
        Dest_TBoxSearch.Text = Caller_TBoxCustName.Text;
        Dest_TBoxCustName.Text = Caller_TBoxCustName.Text;
        Dest_TBoxTelNo1.Text = Caller_TBoxTelNo1.Text;
        Dest_TBoxTelNo2.Text = Caller_TBoxTelNo2.Text;
        Dest_TBoxDeptName.Text = Caller_TBoxDeptName.Text;
        Dest_TBoxChargeName.Text = Caller_TBoxChargeName.Text;
        Dest_TBoxDongBasic.Text = Caller_TBoxDongBasic.Text;
        Dest_TBoxDongAddr.Text = Caller_TBoxDongAddr.Text;
        Dest_TBoxDetailAddr.Text = ""; // 의뢰자에는 DetailAddr 없음
    }
    #endregion
}
#nullable enable