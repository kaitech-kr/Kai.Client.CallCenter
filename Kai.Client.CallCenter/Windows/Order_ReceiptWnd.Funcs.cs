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
        if (tbOrderOrg.CallCustCodeK == 0) // 의뢰자 KeyCode
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
        StdResult_Long result = await s_SrGClient.SrResult_Order_InsertRowAsync_Today(tbOrderOrg);
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

        // 3. UI 데이터를 tbOrderOrg에 반영 (바인딩 안된 필드들)
        UpdateNewTbOrderByUiData(sStatus_OrderSave);

        // 4. 변경 여부 확인
        if (!IsChanged())
        {
            return true; // 변경 없으면 저장 안함
        }

        // 5. DtUpdateLast 정보 설정
        tbOrderOrg.DtUpdateLast = DateTime.Now;

        // 6. 서버로 업데이트 전송
        StdResult_Int result = await s_SrGClient.SrResult_Order_UpdateRowAsync_Today(tbOrderOrg);
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
        if (!string.IsNullOrEmpty(orderState)) tbOrderOrg.OrderState = orderState;

        // CodeK 필드들은 tbOrderOrg에 직접 저장되므로 복사 불필요

        // 예약 관련 (DtReserve, ReserveBreakMinute는 바인딩 안됨)
        tbOrderOrg.DtReserve = DtPickerReserve.Value;
        tbOrderOrg.ReserveBreakMinute = StdConvert.StringToInt(TBoxReserveBreakMin.Text);
        Debug.WriteLine($"[저장] Reserve={tbOrderOrg.Reserve}, DtReserve={tbOrderOrg.DtReserve}, BreakMin={tbOrderOrg.ReserveBreakMinute}");

        // 요금 관련 (바인딩 안된 필드)
        tbOrderOrg.FeeCommi = StdConvert.StringWonFormatToInt(TBox_FeeCharge.Text);
        tbOrderOrg.FeeDriver = StdConvert.StringWonFormatToInt(TBox_FeeDriver.Text);
        tbOrderOrg.FeeType = GetFeeTypeFromUI();
        tbOrderOrg.MovilityFlag = GetCarTypeFromUI();
        tbOrderOrg.DeliverFlag = GetDeliverFlagFromUI();
    }

    // 신규 등록 시 기본값 설정 (tbOrderOrg는 이미 생성자에서 생성됨, UI 바인딩 유지)
    private void SetBasicFieldsForNewOrder(string sOrderState)
    {
        if (tbOrderOrg.CallCustCodeK <= 0) ErrMsgBox($"의뢰자코드가 0입니다.");

        // 기본 필드 설정 (UI 바인딩 안된 필드들)
        tbOrderOrg.KeyCode = 0;
        tbOrderOrg.MemberCode = s_CenterCharge.MemberCode;
        tbOrderOrg.CenterCode = s_CenterCharge.CenterCode;
        tbOrderOrg.ReceiptTime = TimeOnly.FromDateTime(DateTime.Now);
        tbOrderOrg.OrderState = sOrderState;
        tbOrderOrg.OrderStateOld = "";
        tbOrderOrg.UserCode = 0;
        tbOrderOrg.UserName = "";

        // 인성 등 외부연동 필드 초기화
        tbOrderOrg.Insung1SeqNo = "";
        tbOrderOrg.Insung2SeqNo = "";
        tbOrderOrg.Cargo24SeqNo = "";
        tbOrderOrg.OnecallSeqNo = "";
        tbOrderOrg.Share = false;
        tbOrderOrg.TaxBill = false;

        // 기사 정보 초기화 (신규 등록 시 배차 전)
        tbOrderOrg.DriverCode = 0;
        tbOrderOrg.DriverId = "";
        tbOrderOrg.DriverName = "";
        tbOrderOrg.DriverTelNo = "";
        tbOrderOrg.DriverMemberCode = 0;
        tbOrderOrg.DriverCenterId = "";
        tbOrderOrg.DriverCenterName = "";
        tbOrderOrg.DriverBusinessNo = "";

        // UI 바인딩된 필드들은 건드리지 않음 (이미 사용자가 입력한 값 유지)
    }
    #endregion

    #region 변경 비교
    private bool IsChanged()
    {
        if (tbOrderOrg == null || tbOrderBK == null) return true;

        var o = tbOrderBK;
        return tbOrderOrg.KeyCode != o.KeyCode
            || tbOrderOrg.MemberCode != o.MemberCode
            || tbOrderOrg.CenterCode != o.CenterCode
            || tbOrderOrg.UserCode != o.UserCode
            || tbOrderOrg.UserName != o.UserName
            || tbOrderOrg.DtRegist != o.DtRegist
            || tbOrderOrg.DtUpdateLast != o.DtUpdateLast
            || tbOrderOrg.ReceiptTime != o.ReceiptTime
            || tbOrderOrg.AllocTime != o.AllocTime
            || tbOrderOrg.RunTime != o.RunTime
            || tbOrderOrg.FinishTime != o.FinishTime
            || tbOrderOrg.OrderState != o.OrderState
            || tbOrderOrg.OrderStateOld != o.OrderStateOld
            || tbOrderOrg.CancelReason != o.CancelReason
            || tbOrderOrg.Share != o.Share
            || tbOrderOrg.TaxBill != o.TaxBill
            || tbOrderOrg.CallCompCode != o.CallCompCode
            || tbOrderOrg.CallCompName != o.CallCompName
            || tbOrderOrg.CallCustFrom != o.CallCustFrom
            || tbOrderOrg.CallCustCodeE != o.CallCustCodeE
            || tbOrderOrg.CallCustCodeK != o.CallCustCodeK
            || tbOrderOrg.CallCustName != o.CallCustName
            || tbOrderOrg.CallTelNo != o.CallTelNo
            || tbOrderOrg.CallTelNo2 != o.CallTelNo2
            || tbOrderOrg.CallDeptName != o.CallDeptName
            || tbOrderOrg.CallChargeName != o.CallChargeName
            || tbOrderOrg.CallDongBasic != o.CallDongBasic
            || tbOrderOrg.CallAddress != o.CallAddress
            || tbOrderOrg.CallDetailAddr != o.CallDetailAddr
            || tbOrderOrg.CallRemarks != o.CallRemarks
            || tbOrderOrg.CallMemo != o.CallMemo
            || tbOrderOrg.StartCustCodeE != o.StartCustCodeE
            || tbOrderOrg.StartCustCodeK != o.StartCustCodeK
            || tbOrderOrg.StartCustName != o.StartCustName
            || tbOrderOrg.StartTelNo != o.StartTelNo
            || tbOrderOrg.StartTelNo2 != o.StartTelNo2
            || tbOrderOrg.StartDeptName != o.StartDeptName
            || tbOrderOrg.StartChargeName != o.StartChargeName
            || tbOrderOrg.StartDongBasic != o.StartDongBasic
            || tbOrderOrg.StartAddress != o.StartAddress
            || tbOrderOrg.StartDetailAddr != o.StartDetailAddr
            || tbOrderOrg.StartSiDo != o.StartSiDo
            || tbOrderOrg.StartGunGu != o.StartGunGu
            || tbOrderOrg.StartDongRi != o.StartDongRi
            || tbOrderOrg.StartLon != o.StartLon
            || tbOrderOrg.StartLat != o.StartLat
            || tbOrderOrg.StartSignImgUrl != o.StartSignImgUrl
            || tbOrderOrg.StartDtSign != o.StartDtSign
            || tbOrderOrg.DestCustCodeE != o.DestCustCodeE
            || tbOrderOrg.DestCustCodeK != o.DestCustCodeK
            || tbOrderOrg.DestCustName != o.DestCustName
            || tbOrderOrg.DestTelNo != o.DestTelNo
            || tbOrderOrg.DestTelNo2 != o.DestTelNo2
            || tbOrderOrg.DestDeptName != o.DestDeptName
            || tbOrderOrg.DestChargeName != o.DestChargeName
            || tbOrderOrg.DestDongBasic != o.DestDongBasic
            || tbOrderOrg.DestAddress != o.DestAddress
            || tbOrderOrg.DestDetailAddr != o.DestDetailAddr
            || tbOrderOrg.DestSiDo != o.DestSiDo
            || tbOrderOrg.DestGunGu != o.DestGunGu
            || tbOrderOrg.DestDongRi != o.DestDongRi
            || tbOrderOrg.DestLon != o.DestLon
            || tbOrderOrg.DestLat != o.DestLat
            || tbOrderOrg.DestSignImgUrl != o.DestSignImgUrl
            || tbOrderOrg.DestDtSign != o.DestDtSign
            || tbOrderOrg.Reserve != o.Reserve
            || tbOrderOrg.DtReserve != o.DtReserve
            || tbOrderOrg.ReserveBreakMinute != o.ReserveBreakMinute
            || tbOrderOrg.FeeBasic != o.FeeBasic
            || tbOrderOrg.FeeTotal != o.FeeTotal
            || tbOrderOrg.FeeCommi != o.FeeCommi
            || tbOrderOrg.FeeDriver != o.FeeDriver
            || tbOrderOrg.FeePlus != o.FeePlus
            || tbOrderOrg.FeeMinus != o.FeeMinus
            || tbOrderOrg.FeeConn != o.FeeConn
            || tbOrderOrg.FeeType != o.FeeType
            || tbOrderOrg.MovilityFlag != o.MovilityFlag
            || tbOrderOrg.DeliverFlag != o.DeliverFlag
            || tbOrderOrg.StartDateFlag != o.StartDateFlag
            || tbOrderOrg.StartDateDetail != o.StartDateDetail
            || tbOrderOrg.DestDateFlag != o.DestDateFlag
            || tbOrderOrg.DestDateDetail != o.DestDateDetail
            || tbOrderOrg.CarWeightFlag != o.CarWeightFlag
            || tbOrderOrg.TruckDetailFlag != o.TruckDetailFlag
            || tbOrderOrg.StartLoadFlag != o.StartLoadFlag
            || tbOrderOrg.DestUnloadFlag != o.DestUnloadFlag
            || tbOrderOrg.DeliverMemo != o.DeliverMemo
            || tbOrderOrg.DriverCode != o.DriverCode
            || tbOrderOrg.DriverId != o.DriverId
            || tbOrderOrg.DriverName != o.DriverName
            || tbOrderOrg.DriverTelNo != o.DriverTelNo
            || tbOrderOrg.DriverMemberCode != o.DriverMemberCode
            || tbOrderOrg.DriverCenterId != o.DriverCenterId
            || tbOrderOrg.DriverCenterName != o.DriverCenterName
            || tbOrderOrg.DriverBusinessNo != o.DriverBusinessNo
            || tbOrderOrg.Insung1SeqNo != o.Insung1SeqNo
            || tbOrderOrg.Insung1State != o.Insung1State
            || tbOrderOrg.Insung2SeqNo != o.Insung2SeqNo
            || tbOrderOrg.Insung2State != o.Insung2State
            || tbOrderOrg.Cargo24SeqNo != o.Cargo24SeqNo
            || tbOrderOrg.Cargo24State != o.Cargo24State
            || tbOrderOrg.OnecallSeqNo != o.OnecallSeqNo
            || tbOrderOrg.OnecallState != o.OnecallState;
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
        tbOrderOrg.StartCustCodeK = tbOrderOrg.CallCustCodeK;
        tbOrderOrg.StartCustCodeE = tbOrderOrg.CallCustCodeE;
        tbOrderOrg.StartCustName = tbOrderOrg.CallCustName;
        tbOrderOrg.StartDongBasic = tbOrderOrg.CallDongBasic;
        tbOrderOrg.StartTelNo = tbOrderOrg.CallTelNo;
        tbOrderOrg.StartTelNo2 = tbOrderOrg.CallTelNo2;
        tbOrderOrg.StartDeptName = tbOrderOrg.CallDeptName;
        tbOrderOrg.StartChargeName = tbOrderOrg.CallChargeName;
        tbOrderOrg.StartAddress = tbOrderOrg.CallAddress;
        tbOrderOrg.StartDetailAddr = tbOrderOrg.CallDetailAddr;
        Start_TBoxSearch.Text = tbOrderOrg.CallCustName;
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
        tbOrderOrg.DestCustCodeK = tbOrderOrg.CallCustCodeK;
        tbOrderOrg.DestCustCodeE = tbOrderOrg.CallCustCodeE;
        tbOrderOrg.DestCustName = tbOrderOrg.CallCustName;
        tbOrderOrg.DestDongBasic = tbOrderOrg.CallDongBasic;
        tbOrderOrg.DestTelNo = tbOrderOrg.CallTelNo;
        tbOrderOrg.DestTelNo2 = tbOrderOrg.CallTelNo2;
        tbOrderOrg.DestDeptName = tbOrderOrg.CallDeptName;
        tbOrderOrg.DestChargeName = tbOrderOrg.CallChargeName;
        tbOrderOrg.DestAddress = tbOrderOrg.CallAddress;
        tbOrderOrg.DestDetailAddr = tbOrderOrg.CallDetailAddr;
        Dest_TBoxSearch.Text = tbOrderOrg.CallCustName;
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
        if (tbOrderOrg == null) return;

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
        TBlkSeqNo.Text = tbOrderOrg.KeyCode.ToString();
    }

    // 예약 정보 로드
    private void SetReserveInfoToUI()
    {
        Debug.WriteLine($"[로드] Reserve={tbOrderOrg.Reserve}, DtReserve={tbOrderOrg.DtReserve}, BreakMin={tbOrderOrg.ReserveBreakMinute}");
        ChkBoxReserve.DataContext = tbOrderOrg; // 바인딩 연결
        if (tbOrderOrg.Reserve)
        {
            DtPickerReserve.Value = tbOrderOrg.DtReserve;
            TBoxReserveBreakMin.Text = tbOrderOrg.ReserveBreakMinute.ToString();
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

    //    tbOrderOrg = new TbOrder();

    //    tbOrderOrg.KeyCode = tbOrderOrg.KeyCode;
    //    tbOrderOrg.MemberCode = s_CenterCharge.MemberCode;
    //    tbOrderOrg.CenterCode = s_CenterCharge.CenterCode;
    //    tbOrderOrg.DtRegist = tbOrderOrg.DtRegist;
    //    tbOrderOrg.OrderState = tbOrderOrg.OrderState;
    //    tbOrderOrg.OrderStateOld = tbOrderOrg.OrderStateOld;
    //    tbOrderOrg.OrderRemarks = tbOrderOrg.OrderRemarks; // UiData
    //    tbOrderOrg.OrderMemo = tbOrderOrg.OrderMemo; // UiData
    //    tbOrderOrg.OrderMemoExt = tbOrderOrg.OrderMemoExt; // UiData
    //    tbOrderOrg.UserCode = tbOrderOrg.UserCode; // 필요 없을것 같음.
    //    tbOrderOrg.UserName = tbOrderOrg.UserName; // 필요 없을것 같음.
    //    tbOrderOrg.Updater = s_CenterCharge.Id;
    //    tbOrderOrg.UpdateDate = DateTime.Now.ToString(StdConst_Var.DTFORMAT_EXCEPT_SEC);

    //    tbOrderOrg.CallCompCode = tbOrderOrg.CallCompCode;
    //    tbOrderOrg.CallCompName = tbOrderOrg.CallCompName;
    //    tbOrderOrg.CallCustFrom = tbOrderOrg.CallCustFrom;
    //    tbOrderOrg.CallCustCodeE = tbOrderOrg.CallCustCodeE;
    //    tbOrderOrg.CallCustCodeK = CallCustCodeK;
    //    if (tbOrderOrg.CallCustCodeK <= 0) ErrMsgBox($"의뢰자코드가 0입니다.");
    //    //tbOrderOrg.CallCustName = ; // UiData
    //    //tbOrderOrg.CallTelNo = ; // UiData
    //    //tbOrderOrg.CallTelNo2 = ; // UiData
    //    //tbOrderOrg.CallDeptName = ; // UiData
    //    //tbOrderOrg.CallChargeName = ; // UiData
    //    //tbOrderOrg.CallDongBasic = ; // UiData
    //    //tbOrderOrg.CallAddress = ; // UiData
    //    //tbOrderOrg.CallDetailAddr = ; // UiData
    //    //tbOrderOrg.CallRemarks = ; // UiData
    //    tbOrderOrg.StartCustCodeE = 0;
    //    tbOrderOrg.StartCustCodeK = StartCustCodeK;
    //    //tbOrderOrg.StartCustName = ; // UiData
    //    //tbOrderOrg.StartTelNo = ; // UiData
    //    //tbOrderOrg.StartTelNo2 = ; // UiData
    //    //tbOrderOrg.StartDeptName = ; // UiData
    //    //tbOrderOrg.StartChargeName = ; // UiData
    //    //tbOrderOrg.StartDongBasic = ; // UiData
    //    //tbOrderOrg.StartAddress = ; // UiData
    //    //tbOrderOrg.StartDetailAddr = ; // UiData
    //    tbOrderOrg.StartSiDo = tbOrderOrg.StartSiDo; // UiData
    //    tbOrderOrg.StartGunGu = tbOrderOrg.StartGunGu; // UiData
    //    tbOrderOrg.StartDongRi = tbOrderOrg.StartDongRi; // UiData
    //    tbOrderOrg.StartLon = tbOrderOrg.StartLon; // 연구과제
    //    tbOrderOrg.StartLat = tbOrderOrg.StartLat; // 연구과제
    //    tbOrderOrg.StartSignImg = tbOrderOrg.StartSignImg; // 연구과제
    //    tbOrderOrg.StartDtSign = tbOrderOrg.StartDtSign; // 연구과제
    //    tbOrderOrg.DestCustCodeE = 0;
    //    tbOrderOrg.DestCustCodeK = DestCustCodeK;
    //    //tbOrderOrg.DestCustName = ; // UiData
    //    //tbOrderOrg.DestTelNo = ; // UiData
    //    //tbOrderOrg.DestTelNo2 = ; // UiData
    //    //tbOrderOrg.DestDeptName = ; // UiData
    //    //tbOrderOrg.DestChargeName = ; // UiData
    //    //tbOrderOrg.DestDongBasic = ; // UiData
    //    //tbOrderOrg.DestAddress = ; // UiData
    //    //tbOrderOrg.DestDetailAddr = ; // UiData
    //    tbOrderOrg.DestSiDo = tbOrderOrg.DestSiDo; // UiData
    //    tbOrderOrg.DestGunGu = tbOrderOrg.DestGunGu; // UiData
    //    tbOrderOrg.DestDongRi = tbOrderOrg.DestDongRi; // UiData
    //    tbOrderOrg.DestLon = tbOrderOrg.DestLon; // 연구과제
    //    tbOrderOrg.DestLat = tbOrderOrg.DestLat; // 연구과제
    //    tbOrderOrg.DestSignImg = tbOrderOrg.DestSignImg; // 연구과제
    //    tbOrderOrg.DestDtSign = tbOrderOrg.DestDtSign; // 연구과제
    //    tbOrderOrg.DtReserve = tbOrderOrg.DtReserve; // UiData
    //    tbOrderOrg.ReserveBreakMinute = tbOrderOrg.ReserveBreakMinute; // UiData
    //    tbOrderOrg.FeeBasic = tbOrderOrg.FeeBasic; // UiData
    //    tbOrderOrg.FeePlus = tbOrderOrg.FeePlus; // UiData
    //    tbOrderOrg.FeeMinus = tbOrderOrg.FeeMinus; // UiData
    //    tbOrderOrg.FeeConn = tbOrderOrg.FeeConn; // UiData
    //    tbOrderOrg.FeeDriver = tbOrderOrg.FeeDriver; // UiData
    //    tbOrderOrg.FeeCharge = tbOrderOrg.FeeCharge; // UiData
    //    tbOrderOrg.FeeTotal = tbOrderOrg.FeeTotal; // UiData
    //    tbOrderOrg.FeeType = tbOrderOrg.FeeType; // UiData
    //    tbOrderOrg.CarType = tbOrderOrg.CarType; // UiData
    //    tbOrderOrg.CarWeight = tbOrderOrg.CarWeight; // UiData
    //    tbOrderOrg.TruckDetail = tbOrderOrg.TruckDetail; // UiData
    //    tbOrderOrg.DeliverType = tbOrderOrg.DeliverType; // UiData
    //    tbOrderOrg.DriverCode = tbOrderOrg.DriverCode; // UiData
    //    tbOrderOrg.DriverId = tbOrderOrg.DriverId; // UiData
    //    tbOrderOrg.DriverName = tbOrderOrg.DriverName; // UiData
    //    tbOrderOrg.DriverTelNo = tbOrderOrg.DriverTelNo; // UiData
    //    tbOrderOrg.DriverMemberCode = tbOrderOrg.DriverMemberCode; // UiData
    //    tbOrderOrg.DriverCenterId = tbOrderOrg.DriverCenterId; // UiData
    //    tbOrderOrg.DriverCenterName = tbOrderOrg.DriverCenterName; // UiData
    //    tbOrderOrg.DriverBusinessNo = tbOrderOrg.DriverBusinessNo; // UiData
    //    tbOrderOrg.Insung1 = tbOrderOrg.Insung1;
    //    tbOrderOrg.Insung2 = tbOrderOrg.Insung2;
    //    tbOrderOrg.Cargo24 = tbOrderOrg.Cargo24;
    //    tbOrderOrg.Onecall = tbOrderOrg.Onecall;
    //    tbOrderOrg.Share = tbOrderOrg.Share;
    //    tbOrderOrg.TaxBill = tbOrderOrg.TaxBill;
    //    tbOrderOrg.ReceiptTime = tbOrderOrg.ReceiptTime;
    //    tbOrderOrg.AllocTime = tbOrderOrg.AllocTime;
    //    tbOrderOrg.RunTime = tbOrderOrg.RunTime;
    //    tbOrderOrg.FinishTime = tbOrderOrg.FinishTime;
    //}

    //private int WhereUpdatableChanged()
    //{
    //    // 변경되면 안되는 항목
    //    if (tbOrderOrg.KeyCode != tbOrderOrg.KeyCode) return -1;
    //    if (tbOrderOrg.MemberCode != tbOrderOrg.MemberCode) return -3;
    //    if (tbOrderOrg.CenterCode != tbOrderOrg.CenterCode) return -4;
    //    //if (tbOrderOrg.DtRegOrder != tbOrderOrg.DtRegOrder) return -5;
    //    if (tbOrderOrg.OrderStateOld != tbOrderOrg.OrderStateOld) return -7;

    //    // 변경되면 인정되는 항목
    //    if (tbOrderOrg.OrderRemarks != tbOrderOrg.OrderRemarks) return 1;
    //    if (tbOrderOrg.OrderMemo != tbOrderOrg.OrderMemo) return 2;
    //    if (tbOrderOrg.OrderMemoExt != tbOrderOrg.OrderMemoExt) return 3;
    //    if (tbOrderOrg.UserCode != tbOrderOrg.UserCode) return 4;
    //    if (tbOrderOrg.OrderState != tbOrderOrg.OrderState) return 5;
    //    //if (tbOrderOrg.UserName != tbOrderOrg.UserName) return 5;
    //    //if (tbOrderOrg.Updater != tbOrderOrg.Updater) return true; // UiData
    //    //if (tbOrderOrg.UpdateDate != tbOrderOrg.UpdateDate) return true; // UiData
    //    if (tbOrderOrg.CallCustCodeE != tbOrderOrg.CallCustCodeE) return 6;
    //    if (tbOrderOrg.CallCustCodeK != tbOrderOrg.CallCustCodeK) return 7;
    //    if (tbOrderOrg.CallCustName != tbOrderOrg.CallCustName) return 8;
    //    //MsgBox($"{tbOrderOrg.CallTelNo}, {tbOrderOrg.CallTelNo}"); // Test
    //    if (tbOrderOrg.CallTelNo != tbOrderOrg.CallTelNo) return 9;
    //    if (tbOrderOrg.CallTelNo2 != tbOrderOrg.CallTelNo2) return 10;
    //    if (tbOrderOrg.CallDeptName != tbOrderOrg.CallDeptName) return 11;
    //    if (tbOrderOrg.CallChargeName != tbOrderOrg.CallChargeName) return 12;
    //    if (tbOrderOrg.CallDongBasic != tbOrderOrg.CallDongBasic) return 13;
    //    if (tbOrderOrg.CallAddress != tbOrderOrg.CallAddress) return 14;
    //    if (tbOrderOrg.CallDetailAddr != tbOrderOrg.CallDetailAddr) return 15;
    //    if (tbOrderOrg.CallRemarks != tbOrderOrg.CallRemarks) return 16;
    //    if (tbOrderOrg.StartCustCodeE != tbOrderOrg.StartCustCodeE) return 17;
    //    if (tbOrderOrg.StartCustCodeK != tbOrderOrg.StartCustCodeK) return 18;
    //    if (tbOrderOrg.StartCustName != tbOrderOrg.StartCustName) return 19;
    //    if (tbOrderOrg.StartTelNo != tbOrderOrg.StartTelNo) return 20;
    //    if (tbOrderOrg.StartTelNo2 != tbOrderOrg.StartTelNo2) return 21;
    //    if (tbOrderOrg.StartDeptName != tbOrderOrg.StartDeptName) return 22;
    //    if (tbOrderOrg.StartChargeName != tbOrderOrg.StartChargeName) return 23;
    //    if (tbOrderOrg.StartDongBasic != tbOrderOrg.StartDongBasic) return 24;
    //    if (tbOrderOrg.StartAddress != tbOrderOrg.StartAddress) return 25;
    //    if (tbOrderOrg.StartDetailAddr != tbOrderOrg.StartDetailAddr) return 26;
    //    //MsgBox($"/{tbOrderOrg.StartSiDo}:{tbOrderOrg.StartSiDo.Length}/<->/{tbOrderOrg.StartSiDo}:{tbOrderOrg.StartSiDo.Length}/"); // Test
    //    if (tbOrderOrg.StartSiDo != tbOrderOrg.StartSiDo) return 27;
    //    if (tbOrderOrg.StartGunGu != tbOrderOrg.StartGunGu) return 28;
    //    if (tbOrderOrg.StartDongRi != tbOrderOrg.StartDongRi) return 29;
    //    if (tbOrderOrg.StartLon != tbOrderOrg.StartLon) return 30;
    //    if (tbOrderOrg.StartLat != tbOrderOrg.StartLat) return 31;
    //    if (tbOrderOrg.StartSignImg != tbOrderOrg.StartSignImg) return 32;
    //    if (tbOrderOrg.StartDtSign != tbOrderOrg.StartDtSign) return 33;
    //    if (tbOrderOrg.DestCustCodeE != tbOrderOrg.DestCustCodeE) return 34;
    //    if (tbOrderOrg.DestCustCodeK != tbOrderOrg.DestCustCodeK) return 35;
    //    if (tbOrderOrg.DestCustName != tbOrderOrg.DestCustName) return 36;
    //    if (tbOrderOrg.DestTelNo != tbOrderOrg.DestTelNo) return 37;
    //    if (tbOrderOrg.DestTelNo2 != tbOrderOrg.DestTelNo2) return 38;
    //    if (tbOrderOrg.DestDeptName != tbOrderOrg.DestDeptName) return 39;
    //    if (tbOrderOrg.DestChargeName != tbOrderOrg.DestChargeName) return 40;
    //    if (tbOrderOrg.DestDongBasic != tbOrderOrg.DestDongBasic) return 41;
    //    if (tbOrderOrg.DestAddress != tbOrderOrg.DestAddress) return 42;
    //    if (tbOrderOrg.DestDetailAddr != tbOrderOrg.DestDetailAddr) return 43;
    //    if (tbOrderOrg.DestSiDo != tbOrderOrg.DestSiDo) return 44;
    //    if (tbOrderOrg.DestGunGu != tbOrderOrg.DestGunGu) return 45;
    //    if (tbOrderOrg.DestDongRi != tbOrderOrg.DestDongRi) return 46;
    //    if (tbOrderOrg.DestLon != tbOrderOrg.DestLon) return 47;
    //    if (tbOrderOrg.DestLat != tbOrderOrg.DestLat) return 48;
    //    if (tbOrderOrg.DestSignImg != tbOrderOrg.DestSignImg) return 49;
    //    if (tbOrderOrg.DestDtSign != tbOrderOrg.DestDtSign) return 50;
    //    if (tbOrderOrg.DtReserve != tbOrderOrg.DtReserve) return 51;
    //    if (tbOrderOrg.ReserveBreakMinute != tbOrderOrg.ReserveBreakMinute) return 52;
    //    if (tbOrderOrg.FeeBasic != tbOrderOrg.FeeBasic) return 53;
    //    if (tbOrderOrg.FeePlus != tbOrderOrg.FeePlus) return 54;
    //    if (tbOrderOrg.FeeMinus != tbOrderOrg.FeeMinus) return 55;
    //    if (tbOrderOrg.FeeConn != tbOrderOrg.FeeConn) return 56;
    //    if (tbOrderOrg.FeeDriver != tbOrderOrg.FeeDriver) return 57;
    //    if (tbOrderOrg.FeeCharge != tbOrderOrg.FeeCharge) return 58;
    //    if (tbOrderOrg.FeeTotal != tbOrderOrg.FeeTotal) return 59;
    //    if (tbOrderOrg.FeeType != tbOrderOrg.FeeType) return 60;
    //    if (tbOrderOrg.CarType != tbOrderOrg.CarType) return 61;
    //    if (tbOrderOrg.CarWeight != tbOrderOrg.CarWeight) return 62;
    //    if (tbOrderOrg.TruckDetail != tbOrderOrg.TruckDetail) return 63;
    //    if (tbOrderOrg.DeliverType != tbOrderOrg.DeliverType) return 64;
    //    if (tbOrderOrg.DriverCode != tbOrderOrg.DriverCode) return 65;
    //    if (tbOrderOrg.DriverId != tbOrderOrg.DriverId) return 66;
    //    if (tbOrderOrg.DriverName != tbOrderOrg.DriverName) return 67;
    //    if (tbOrderOrg.DriverTelNo != tbOrderOrg.DriverTelNo) return 68;
    //    if (tbOrderOrg.DriverMemberCode != tbOrderOrg.DriverMemberCode) return 69;
    //    if (tbOrderOrg.DriverCenterId != tbOrderOrg.DriverCenterId) return 70;
    //    if (tbOrderOrg.DriverCenterName != tbOrderOrg.DriverCenterName) return 71;
    //    if (tbOrderOrg.DriverBusinessNo != tbOrderOrg.DriverBusinessNo) return 72;
    //    if (tbOrderOrg.Insung1 != tbOrderOrg.Insung1) return 74;
    //    if (tbOrderOrg.Insung2 != tbOrderOrg.Insung2) return 75;
    //    if (tbOrderOrg.Cargo24 != tbOrderOrg.Cargo24) return 76;
    //    if (tbOrderOrg.Onecall != tbOrderOrg.Onecall) return 77;
    //    if (tbOrderOrg.Share != tbOrderOrg.Share) return 78;
    //    if (tbOrderOrg.TaxBill != tbOrderOrg.TaxBill) return 79;
    //    if (tbOrderOrg.ReceiptTime != tbOrderOrg.ReceiptTime) return 80;
    //    if (tbOrderOrg.AllocTime != tbOrderOrg.AllocTime) return 81;
    //    if (tbOrderOrg.RunTime != tbOrderOrg.RunTime) return 82;
    //    if (tbOrderOrg.FinishTime != tbOrderOrg.FinishTime) return 83;

    //    if (tbOrderOrg.CallCustFrom != tbOrderOrg.CallCustFrom) return 84;
    //    if (tbOrderOrg.CallCompCode != tbOrderOrg.CallCompCode) return 85;
    //    if (tbOrderOrg.CallCompName != tbOrderOrg.CallCompName) return 86;

    //    return 0;
    //}

    // 고객정보
    private void TbAllTo의뢰자(TbAllWith tb)
    {
        TbCustMain tbCustMain = tb.custMain;
        TbCompany tbCompany = tb.company;
        TbCallCenter tbCallCenter = tb.callCenter;

        SetLocationDataToUi(tbCustMain, "의뢰자");

        // 의뢰자 전용 필드 설정 - tbOrderOrg 직접 사용
        tbOrderOrg.CallCustFrom = tbCustMain.BeforeBelong;
        tbOrderOrg.CallCompCode = tbCustMain.CompCode;
        if (tbCompany == null)
        {
            tbOrderOrg.CallCompName = "";
        }
        else
        {
            tbOrderOrg.CallCompName = tbCompany.CompName;
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

    // LocationData → tbOrderOrg 설정 (공통 헬퍼) - TbCustMain 버전
    // DataContext 바인딩으로 UI 자동 업데이트
    private void SetLocationDataToUi(TbCustMain tbCustMain, string sWhere)
    {
        switch (sWhere)
        {
            case "의뢰자":
                tbOrderOrg.CallCustCodeK = tbCustMain.KeyCode;
                tbOrderOrg.CallCustCodeE = StdConvert.NullableLongToLong(tbCustMain.BeforeCustKey);
                tbOrderOrg.CallCustName = tbCustMain.CustName;
                tbOrderOrg.CallDongBasic = tbCustMain.DongBasic;
                tbOrderOrg.CallTelNo = tbCustMain.TelNo1;
                tbOrderOrg.CallTelNo2 = tbCustMain.TelNo2;
                tbOrderOrg.CallDeptName = tbCustMain.DeptName;
                tbOrderOrg.CallChargeName = tbCustMain.ChargeName;
                tbOrderOrg.CallAddress = tbCustMain.DongAddr;
                tbOrderOrg.CallDetailAddr = tbCustMain.DetailAddr;
                tbOrderOrg.CallRemarks = tbCustMain.Remarks;
                break;
            case "출발지":
                tbOrderOrg.StartCustCodeK = tbCustMain.KeyCode;
                tbOrderOrg.StartCustCodeE = StdConvert.NullableLongToLong(tbCustMain.BeforeCustKey);
                tbOrderOrg.StartCustName = tbCustMain.CustName;
                tbOrderOrg.StartDongBasic = tbCustMain.DongBasic;
                tbOrderOrg.StartTelNo = tbCustMain.TelNo1;
                tbOrderOrg.StartTelNo2 = tbCustMain.TelNo2;
                tbOrderOrg.StartDeptName = tbCustMain.DeptName;
                tbOrderOrg.StartChargeName = tbCustMain.ChargeName;
                tbOrderOrg.StartAddress = tbCustMain.DongAddr;
                tbOrderOrg.StartDetailAddr = tbCustMain.DetailAddr;
                break;
            case "도착지":
                tbOrderOrg.DestCustCodeK = tbCustMain.KeyCode;
                tbOrderOrg.DestCustCodeE = StdConvert.NullableLongToLong(tbCustMain.BeforeCustKey);
                tbOrderOrg.DestCustName = tbCustMain.CustName;
                tbOrderOrg.DestDongBasic = tbCustMain.DongBasic;
                tbOrderOrg.DestTelNo = tbCustMain.TelNo1;
                tbOrderOrg.DestTelNo2 = tbCustMain.TelNo2;
                tbOrderOrg.DestDeptName = tbCustMain.DeptName;
                tbOrderOrg.DestChargeName = tbCustMain.ChargeName;
                tbOrderOrg.DestAddress = tbCustMain.DongAddr;
                tbOrderOrg.DestDetailAddr = tbCustMain.DetailAddr;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(sWhere));
        }
    }

    // TbOrder → UI 로드 (오버로드) - 로드 시 tbOrderOrg 데이터를 UI에 표시
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

        // UI에 표시만 (tbOrderOrg 값은 이미 로드됨)
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