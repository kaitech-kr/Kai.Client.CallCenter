using System;
using System.Collections.Generic;

namespace Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models;

public partial class TbOrder
{
    #region 키
    public long KeyCode { get; set; }

    public long MemberCode { get; set; }

    public long CenterCode { get; set; }
    #endregion

    #region DateTime
    public DateTime DtRegist { get; set; }

    public TimeOnly? ReceiptTime { get; set; }

    public TimeOnly? AllocTime { get; set; }

    public TimeOnly? RunTime { get; set; }

    public TimeOnly? FinishTime { get; set; }
    #endregion

    #region OrderInfo
    public string OrderState { get; set; } = null!;

    public string OrderStateOld { get; set; } = null!;

    public string OrderRemarks { get; set; } = null!;

    public string OrderMemo { get; set; } = null!;

    public string OrderMemoExt { get; set; } = null!;

    public string CancelReason { get; set; } = null!;

    public bool Share { get; set; }

    public bool TaxBill { get; set; }
    #endregion

    #region WriterInfo
    public long UserCode { get; set; }

    public string UserName { get; set; } = null!;

    public string Updater { get; set; } = null!;

    public string? UpdateDate { get; set; }
    #endregion

    #region 의뢰자 정보
    public long CallCompCode { get; set; }

    public string CallCompName { get; set; } = null!;

    public string CallCustFrom { get; set; } = null!;

    public long CallCustCodeE { get; set; }

    public long CallCustCodeK { get; set; }

    public string CallCustName { get; set; } = null!;

    public string CallTelNo { get; set; } = null!;

    public string CallTelNo2 { get; set; } = null!;

    public string CallDeptName { get; set; } = null!;

    public string CallChargeName { get; set; } = null!;

    public string CallDongBasic { get; set; } = null!;

    public string CallAddress { get; set; } = null!;

    public string CallDetailAddr { get; set; } = null!;

    public string CallRemarks { get; set; } = null!;
    #endregion

    #region 출발지 정보
    public long StartCustCodeE { get; set; }

    public long StartCustCodeK { get; set; }

    public string StartCustName { get; set; } = null!;

    public string StartTelNo { get; set; } = null!;

    public string StartTelNo2 { get; set; } = null!;

    public string StartDeptName { get; set; } = null!;

    public string StartChargeName { get; set; } = null!;

    public string StartDongBasic { get; set; } = null!;

    public string StartAddress { get; set; } = null!;

    public string StartDetailAddr { get; set; } = null!;

    public string StartSiDo { get; set; } = null!;

    public string StartGunGu { get; set; } = null!;

    public string StartDongRi { get; set; } = null!;

    public int StartLon { get; set; }

    public int StartLat { get; set; }

    public string? StartSignImgUrl { get; set; }

    public DateTime? StartDtSign { get; set; }
    #endregion

    #region 도착지 정보
    public long DestCustCodeE { get; set; }

    public long DestCustCodeK { get; set; }

    public string DestCustName { get; set; } = null!;

    public string DestTelNo { get; set; } = null!;

    public string DestTelNo2 { get; set; } = null!;

    public string DestDeptName { get; set; } = null!;

    public string DestChargeName { get; set; } = null!;

    public string DestDongBasic { get; set; } = null!;

    public string DestAddress { get; set; } = null!;

    public string DestDetailAddr { get; set; } = null!;

    public string DestSiDo { get; set; } = null!;

    public string DestGunGu { get; set; } = null!;

    public string DestDongRi { get; set; } = null!;

    public int DestLon { get; set; }

    public int DestLat { get; set; }

    public string? DestSignImgUrl { get; set; }

    public DateTime? DestDtSign { get; set; }
    #endregion

    #region 예약
    public DateTime? DtReserve { get; set; }

    public int ReserveBreakMinute { get; set; }
    #endregion

    #region 요금
    public int FeeBasic { get; set; }

    public int FeeTotal { get; set; }

    public int FeeCommi { get; set; }

    public int FeeDriver { get; set; }

    public int FeePlus { get; set; }

    public int FeeMinus { get; set; }

    public int FeeConn { get; set; }

    public string FeeType { get; set; } = null!;
    #endregion

    #region 배송타입, 차량타입, 출, 도착 일시
    public string DeliverFlag { get; set; } = null!;

    public string MovilityFlag { get; set; } = null!;

    public string StartDateFlag { get; set; } = null!;

    public string StartDateDetail { get; set; } = null!;

    public string DestDateFlag { get; set; } = null!;

    public string DestDateDetail { get; set; } = null!;
    #endregion

    #region 화물 정보
    public string CarWeightFlag { get; set; } = null!;

    public string TruckDetailFlag { get; set; } = null!;

    public string StartLoadFlag { get; set; } = null!;

    public string DestUnloadFlag { get; set; } = null!;
    #endregion

    #region 화물, 퀵 공용정보
    public string DeliverMemo { get; set; }
    #endregion

    #region 기사 정보
    public long DriverCode { get; set; }

    public string DriverId { get; set; } = null!;

    public string DriverName { get; set; } = null!;

    public string DriverTelNo { get; set; } = null!;

    public long DriverMemberCode { get; set; }

    public string DriverCenterId { get; set; } = null!;

    public string DriverCenterName { get; set; } = null!;

    public string DriverBusinessNo { get; set; } = null!;
    #endregion

    #region 외부 앱
    public string Insung1SeqNo { get; set; } = null!;

    public string Insung1State { get; set; }

    public string Insung2SeqNo { get; set; } = null!;

    public string Insung2State { get; set; }

    public string Cargo24SeqNo { get; set; } = null!;

    public string Cargo24State { get; set; }

    public string OnecallSeqNo { get; set; } = null!;

    public string OnecallState { get; set; } 
    #endregion

}