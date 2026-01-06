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
        tbOrderNew.Reserve = ChkBoxReserve.IsChecked == true; // UiData
        tbOrderNew.DtReserve = DtPickerReserve.Value; // UiData
        tbOrderNew.ReserveBreakMinute = StdConvert.StringToInt(TBoxReserveBreakMin.Text); // UiData
        tbOrderNew.FeeBasic = StdConvert.StringWonFormatToInt(TBox_FeeBasic.Text); // UiData
        tbOrderNew.FeePlus = StdConvert.StringWonFormatToInt(TBox_FeePlus.Text); // UiData
        tbOrderNew.FeeMinus = StdConvert.StringWonFormatToInt(TBox_FeeMinus.Text); // UiData
        tbOrderNew.FeeConn = StdConvert.StringWonFormatToInt(TBox_FeeConn.Text); // UiData
        tbOrderNew.FeeCommi = StdConvert.StringWonFormatToInt(TBox_FeeCharge.Text); // UiData
        tbOrderNew.FeeDriver = StdConvert.StringWonFormatToInt(TBox_FeeDriver.Text); // UiData
        tbOrderNew.FeeTotal = StdConvert.StringWonFormatToInt(TBox_FeeTot.Text); // UiData
        tbOrderNew.FeeType = GetFeeTypeFromUI(); // UiData (선불/착불/신용/송금)
        tbOrderNew.MovilityFlag = GetCarTypeFromUI(); // UiData
        tbOrderNew.DeliverFlag = GetDeliverFlagFromUI(); // UiData (비트플래그: 편도=1,왕복=2,경유=4,긴급=8,혼적=16)
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
        tbOrderNew.Reserve = false; // UiData
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
    private void TbOrderOrgToUiData()
    {
        if (tbOrderOrg == null) return;

        // 1. Header, 공용
        SetHeaderInfoToUI();

        // 2. 위치 정보 설정
        SetLocationDataToUi(tbOrderOrg, "의뢰자");
        SetLocationDataToUi(tbOrderOrg, "출발지");
        SetLocationDataToUi(tbOrderOrg, "도착지");

        // 3. 의뢰자 전용 필드
        Caller_TBoxRemarks.Text = tbOrderOrg.CallRemarks;
        // OrderMemoExt 삭제됨 - 필요시 CallMemo 사용
        TBlkCallCustMemoExt.Text = "";
        CallCustFrom = tbOrderOrg.CallCustFrom;
        CallCompCode = tbOrderOrg.CallCompCode;
        CallCompName = tbOrderOrg.CallCompName;

        // 4. 예약 정보
        SetReserveInfoToUI();

        // 5. 차량/배송 타입
        SetVehicleInfoToUI();

        // 6. 요금 정보
        SetFeeInfoToUI();

        // 7. 공유/세금계산서 - UI 만들어야 함.
        //ChkBoxShareOrder.IsChecked = tbOrderOrg.Share;
        //ChkBoxTaxBill.IsChecked = tbOrderOrg.TaxBill;
    }

    // Header 정보 로드
    private void SetHeaderInfoToUI()
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
    private void SetReserveInfoToUI()
    {
        ChkBoxReserve.IsChecked = tbOrderOrg.Reserve;
        if (tbOrderOrg.Reserve)
        {
            DtPickerReserve.Value = tbOrderOrg.DtReserve;
            TBoxReserveBreakMin.Text = tbOrderOrg.ReserveBreakMinute.ToString();
        }
    }

    // 차량/배송 타입 정보 로드
    private void SetVehicleInfoToUI()
    {
        SetFeeTypeToUI(tbOrderOrg.FeeType);
        SetCarTypeToUI(tbOrderOrg.MovilityFlag);
        SetDeliverFlagToUI(tbOrderOrg.DeliverFlag);
    }

    // 요금 정보 로드
    private void SetFeeInfoToUI()
    {
        TBox_FeeBasic.Text = StdConvert.IntToStringWonFormat(tbOrderOrg.FeeBasic);
        TBox_FeePlus.Text = StdConvert.IntToStringWonFormat(tbOrderOrg.FeePlus);
        TBox_FeeMinus.Text = StdConvert.IntToStringWonFormat(tbOrderOrg.FeeMinus);
        TBox_FeeConn.Text = StdConvert.IntToStringWonFormat(tbOrderOrg.FeeConn);
        TBox_FeeCharge.Text = StdConvert.IntToStringWonFormat(tbOrderOrg.FeeCommi);
        TBox_FeeDriver.Text = StdConvert.IntToStringWonFormat(tbOrderOrg.FeeDriver);
        TBox_FeeTot.Text = StdConvert.IntToStringWonFormat(tbOrderOrg.FeeTotal);
    }









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

    // TbOrder → UI 로드 (오버로드)
    private void SetLocationDataToUi(TbOrder tb, string sWhere)
    {
        Action<long> setCustCodeK;
        Action<long> setCustCodeE;
        (TextBox tboxCustName, TextBox tboxDongBasic, TextBox tboxTelNo1, TextBox tboxTelNo2,
         TextBox tboxDeptName, TextBox tboxChargeName, TextBox tboxDongAddr) controls;

        long custCodeK, custCodeE;
        string custName, dongBasic, telNo1, telNo2, deptName, chargeName, dongAddr;

        switch (sWhere)
        {
            case "의뢰자":
                setCustCodeK = (val) => CallCustCodeK = val;
                setCustCodeE = (val) => CallCustCodeE = val;
                controls = (Caller_TBoxCustName, Caller_TBoxDongBasic, Caller_TBoxTelNo1, Caller_TBoxTelNo2,
                            Caller_TBoxDeptName, Caller_TBoxChargeName, Caller_TBoxDongAddr);
                custCodeK = StdConvert.NullableLongToLong(tb.CallCustCodeK);
                custCodeE = StdConvert.NullableLongToLong(tb.CallCustCodeE);
                custName = tb.CallCustName;
                dongBasic = tb.CallDongBasic;
                telNo1 = StdConvert.ToPhoneNumberFormat(tb.CallTelNo);
                telNo2 = StdConvert.ToPhoneNumberFormat(tb.CallTelNo2);
                deptName = tb.CallDeptName;
                chargeName = tb.CallChargeName;
                dongAddr = tb.CallAddress;
                break;
            case "출발지":
                setCustCodeK = (val) => StartCustCodeK = val;
                setCustCodeE = (val) => StartCustCodeE = val;
                controls = (Start_TBoxCustName, Start_TBoxDongBasic, Start_TBoxTelNo1, Start_TBoxTelNo2,
                            Start_TBoxDeptName, Start_TBoxChargeName, Start_TBoxDongAddr);
                custCodeK = StdConvert.NullableLongToLong(tb.StartCustCodeK);
                custCodeE = StdConvert.NullableLongToLong(tb.StartCustCodeE);
                custName = tb.StartCustName;
                dongBasic = tb.StartDongBasic;
                telNo1 = StdConvert.ToPhoneNumberFormat(tb.StartTelNo);
                telNo2 = StdConvert.ToPhoneNumberFormat(tb.StartTelNo2);
                deptName = tb.StartDeptName;
                chargeName = tb.StartChargeName;
                dongAddr = tb.StartAddress;
                break;
            case "도착지":
                setCustCodeK = (val) => DestCustCodeK = val;
                setCustCodeE = (val) => DestCustCodeE = val;
                controls = (Dest_TBoxCustName, Dest_TBoxDongBasic, Dest_TBoxTelNo1, Dest_TBoxTelNo2,
                            Dest_TBoxDeptName, Dest_TBoxChargeName, Dest_TBoxDongAddr);
                custCodeK = StdConvert.NullableLongToLong(tb.DestCustCodeK);
                custCodeE = StdConvert.NullableLongToLong(tb.DestCustCodeE);
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

        setCustCodeK(custCodeK);
        setCustCodeE(custCodeE);
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