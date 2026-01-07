using System.ComponentModel;

using Kai.Common.StdDll_Common;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models;
using Kai.Client.CallCenter.Classes;

namespace Kai.Client.CallCenter.MVVM.ViewModels;
#nullable disable
public class VmOrder_StatusPage_Order : INotifyPropertyChanged, IViewModelBase
{
    #region Variables
    public TbOrder tbOrder;
    private TbOrder _backupedOrder;
    #endregion

    #region Property Event
    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    #endregion

    #region 생성자
    // 수정용 생성자
    public VmOrder_StatusPage_Order(TbOrder tb)
    {
        tbOrder = tb;
        _backupedOrder = CloneTbOrder(tb);
    }

    // 등록용 생성자 (빈 TbOrder 생성)
    public VmOrder_StatusPage_Order()
    {
        tbOrder = new TbOrder
        {
            KeyCode = 0,
            MemberCode = 0,
            CenterCode = 0,
            UserCode = 0,
            UserName = string.Empty,
            DtRegist = DateTime.Now,
            DtUpdateLast = DateTime.Now,
            OrderState = string.Empty,
            OrderStateOld = string.Empty,
            CancelReason = string.Empty,
            Share = false,
            TaxBill = false,
            CallCompCode = 0,
            CallCompName = string.Empty,
            CallCustFrom = string.Empty,
            CallCustCodeE = 0,
            CallCustCodeK = 0,
            CallCustName = string.Empty,
            CallTelNo = string.Empty,
            CallTelNo2 = string.Empty,
            CallDeptName = string.Empty,
            CallChargeName = string.Empty,
            CallDongBasic = string.Empty,
            CallAddress = string.Empty,
            CallDetailAddr = string.Empty,
            CallRemarks = string.Empty,
            CallMemo = string.Empty,
            StartCustCodeE = 0,
            StartCustCodeK = 0,
            StartCustName = string.Empty,
            StartTelNo = string.Empty,
            StartTelNo2 = string.Empty,
            StartDeptName = string.Empty,
            StartChargeName = string.Empty,
            StartDongBasic = string.Empty,
            StartAddress = string.Empty,
            StartDetailAddr = string.Empty,
            StartSiDo = string.Empty,
            StartGunGu = string.Empty,
            StartDongRi = string.Empty,
            StartLon = 0,
            StartLat = 0,
            DestCustCodeE = 0,
            DestCustCodeK = 0,
            DestCustName = string.Empty,
            DestTelNo = string.Empty,
            DestTelNo2 = string.Empty,
            DestDeptName = string.Empty,
            DestChargeName = string.Empty,
            DestDongBasic = string.Empty,
            DestAddress = string.Empty,
            DestDetailAddr = string.Empty,
            DestSiDo = string.Empty,
            DestGunGu = string.Empty,
            DestDongRi = string.Empty,
            DestLon = 0,
            DestLat = 0,
            Reserve = false,
            ReserveBreakMinute = 0,
            FeeBasic = 0,
            FeeTotal = 0,
            FeeCommi = 0,
            FeeDriver = 0,
            FeePlus = 0,
            FeeMinus = 0,
            FeeConn = 0,
            FeeType = string.Empty,
            MovilityFlag = string.Empty,
            DeliverFlag = string.Empty,
            StartDateFlag = string.Empty,
            StartDateDetail = string.Empty,
            DestDateFlag = string.Empty,
            DestDateDetail = string.Empty,
            CarWeightFlag = string.Empty,
            TruckDetailFlag = string.Empty,
            StartLoadFlag = string.Empty,
            DestUnloadFlag = string.Empty,
            DeliverMemo = string.Empty,
            DriverCode = 0,
            DriverId = string.Empty,
            DriverName = string.Empty,
            DriverTelNo = string.Empty,
            DriverMemberCode = 0,
            DriverCenterId = string.Empty,
            DriverCenterName = string.Empty,
            DriverBusinessNo = string.Empty,
            Insung1SeqNo = string.Empty,
            Insung1State = string.Empty,
            Insung2SeqNo = string.Empty,
            Insung2State = string.Empty,
            Cargo24SeqNo = string.Empty,
            Cargo24State = string.Empty,
            OnecallSeqNo = string.Empty,
            OnecallState = string.Empty
        };
        _backupedOrder = CloneTbOrder(tbOrder);
    }
    #endregion

    #region IViewModelBase 구현
    public bool IsChanged
    {
        get
        {
            var o = _backupedOrder;
            return tbOrder.KeyCode != o.KeyCode
                || tbOrder.MemberCode != o.MemberCode
                || tbOrder.CenterCode != o.CenterCode
                || tbOrder.UserCode != o.UserCode
                || tbOrder.UserName != o.UserName
                || tbOrder.DtRegist != o.DtRegist
                || tbOrder.DtUpdateLast != o.DtUpdateLast
                || tbOrder.ReceiptTime != o.ReceiptTime
                || tbOrder.AllocTime != o.AllocTime
                || tbOrder.RunTime != o.RunTime
                || tbOrder.FinishTime != o.FinishTime
                || tbOrder.OrderState != o.OrderState
                || tbOrder.OrderStateOld != o.OrderStateOld
                || tbOrder.CancelReason != o.CancelReason
                || tbOrder.Share != o.Share
                || tbOrder.TaxBill != o.TaxBill
                || tbOrder.CallCompCode != o.CallCompCode
                || tbOrder.CallCompName != o.CallCompName
                || tbOrder.CallCustFrom != o.CallCustFrom
                || tbOrder.CallCustCodeE != o.CallCustCodeE
                || tbOrder.CallCustCodeK != o.CallCustCodeK
                || tbOrder.CallCustName != o.CallCustName
                || tbOrder.CallTelNo != o.CallTelNo
                || tbOrder.CallTelNo2 != o.CallTelNo2
                || tbOrder.CallDeptName != o.CallDeptName
                || tbOrder.CallChargeName != o.CallChargeName
                || tbOrder.CallDongBasic != o.CallDongBasic
                || tbOrder.CallAddress != o.CallAddress
                || tbOrder.CallDetailAddr != o.CallDetailAddr
                || tbOrder.CallRemarks != o.CallRemarks
                || tbOrder.CallMemo != o.CallMemo
                || tbOrder.StartCustCodeE != o.StartCustCodeE
                || tbOrder.StartCustCodeK != o.StartCustCodeK
                || tbOrder.StartCustName != o.StartCustName
                || tbOrder.StartTelNo != o.StartTelNo
                || tbOrder.StartTelNo2 != o.StartTelNo2
                || tbOrder.StartDeptName != o.StartDeptName
                || tbOrder.StartChargeName != o.StartChargeName
                || tbOrder.StartDongBasic != o.StartDongBasic
                || tbOrder.StartAddress != o.StartAddress
                || tbOrder.StartDetailAddr != o.StartDetailAddr
                || tbOrder.StartSiDo != o.StartSiDo
                || tbOrder.StartGunGu != o.StartGunGu
                || tbOrder.StartDongRi != o.StartDongRi
                || tbOrder.StartLon != o.StartLon
                || tbOrder.StartLat != o.StartLat
                || tbOrder.StartSignImgUrl != o.StartSignImgUrl
                || tbOrder.StartDtSign != o.StartDtSign
                || tbOrder.DestCustCodeE != o.DestCustCodeE
                || tbOrder.DestCustCodeK != o.DestCustCodeK
                || tbOrder.DestCustName != o.DestCustName
                || tbOrder.DestTelNo != o.DestTelNo
                || tbOrder.DestTelNo2 != o.DestTelNo2
                || tbOrder.DestDeptName != o.DestDeptName
                || tbOrder.DestChargeName != o.DestChargeName
                || tbOrder.DestDongBasic != o.DestDongBasic
                || tbOrder.DestAddress != o.DestAddress
                || tbOrder.DestDetailAddr != o.DestDetailAddr
                || tbOrder.DestSiDo != o.DestSiDo
                || tbOrder.DestGunGu != o.DestGunGu
                || tbOrder.DestDongRi != o.DestDongRi
                || tbOrder.DestLon != o.DestLon
                || tbOrder.DestLat != o.DestLat
                || tbOrder.DestSignImgUrl != o.DestSignImgUrl
                || tbOrder.DestDtSign != o.DestDtSign
                || tbOrder.Reserve != o.Reserve
                || tbOrder.DtReserve != o.DtReserve
                || tbOrder.ReserveBreakMinute != o.ReserveBreakMinute
                || tbOrder.FeeBasic != o.FeeBasic
                || tbOrder.FeeTotal != o.FeeTotal
                || tbOrder.FeeCommi != o.FeeCommi
                || tbOrder.FeeDriver != o.FeeDriver
                || tbOrder.FeePlus != o.FeePlus
                || tbOrder.FeeMinus != o.FeeMinus
                || tbOrder.FeeConn != o.FeeConn
                || tbOrder.FeeType != o.FeeType
                || tbOrder.MovilityFlag != o.MovilityFlag
                || tbOrder.DeliverFlag != o.DeliverFlag
                || tbOrder.StartDateFlag != o.StartDateFlag
                || tbOrder.StartDateDetail != o.StartDateDetail
                || tbOrder.DestDateFlag != o.DestDateFlag
                || tbOrder.DestDateDetail != o.DestDateDetail
                || tbOrder.CarWeightFlag != o.CarWeightFlag
                || tbOrder.TruckDetailFlag != o.TruckDetailFlag
                || tbOrder.StartLoadFlag != o.StartLoadFlag
                || tbOrder.DestUnloadFlag != o.DestUnloadFlag
                || tbOrder.DeliverMemo != o.DeliverMemo
                || tbOrder.DriverCode != o.DriverCode
                || tbOrder.DriverId != o.DriverId
                || tbOrder.DriverName != o.DriverName
                || tbOrder.DriverTelNo != o.DriverTelNo
                || tbOrder.DriverMemberCode != o.DriverMemberCode
                || tbOrder.DriverCenterId != o.DriverCenterId
                || tbOrder.DriverCenterName != o.DriverCenterName
                || tbOrder.DriverBusinessNo != o.DriverBusinessNo
                || tbOrder.Insung1SeqNo != o.Insung1SeqNo
                || tbOrder.Insung1State != o.Insung1State
                || tbOrder.Insung2SeqNo != o.Insung2SeqNo
                || tbOrder.Insung2State != o.Insung2State
                || tbOrder.Cargo24SeqNo != o.Cargo24SeqNo
                || tbOrder.Cargo24State != o.Cargo24State
                || tbOrder.OnecallSeqNo != o.OnecallSeqNo
                || tbOrder.OnecallState != o.OnecallState;
        }
    }
    #endregion

    #region Private Methods
    private TbOrder CloneTbOrder(TbOrder source)
    {
        return new TbOrder
        {
            KeyCode = source.KeyCode,
            MemberCode = source.MemberCode,
            CenterCode = source.CenterCode,
            UserCode = source.UserCode,
            UserName = source.UserName,
            DtRegist = source.DtRegist,
            DtUpdateLast = source.DtUpdateLast,
            ReceiptTime = source.ReceiptTime,
            AllocTime = source.AllocTime,
            RunTime = source.RunTime,
            FinishTime = source.FinishTime,
            OrderState = source.OrderState,
            OrderStateOld = source.OrderStateOld,
            CancelReason = source.CancelReason,
            Share = source.Share,
            TaxBill = source.TaxBill,
            CallCompCode = source.CallCompCode,
            CallCompName = source.CallCompName,
            CallCustFrom = source.CallCustFrom,
            CallCustCodeE = source.CallCustCodeE,
            CallCustCodeK = source.CallCustCodeK,
            CallCustName = source.CallCustName,
            CallTelNo = source.CallTelNo,
            CallTelNo2 = source.CallTelNo2,
            CallDeptName = source.CallDeptName,
            CallChargeName = source.CallChargeName,
            CallDongBasic = source.CallDongBasic,
            CallAddress = source.CallAddress,
            CallDetailAddr = source.CallDetailAddr,
            CallRemarks = source.CallRemarks,
            CallMemo = source.CallMemo,
            StartCustCodeE = source.StartCustCodeE,
            StartCustCodeK = source.StartCustCodeK,
            StartCustName = source.StartCustName,
            StartTelNo = source.StartTelNo,
            StartTelNo2 = source.StartTelNo2,
            StartDeptName = source.StartDeptName,
            StartChargeName = source.StartChargeName,
            StartDongBasic = source.StartDongBasic,
            StartAddress = source.StartAddress,
            StartDetailAddr = source.StartDetailAddr,
            StartSiDo = source.StartSiDo,
            StartGunGu = source.StartGunGu,
            StartDongRi = source.StartDongRi,
            StartLon = source.StartLon,
            StartLat = source.StartLat,
            StartSignImgUrl = source.StartSignImgUrl,
            StartDtSign = source.StartDtSign,
            DestCustCodeE = source.DestCustCodeE,
            DestCustCodeK = source.DestCustCodeK,
            DestCustName = source.DestCustName,
            DestTelNo = source.DestTelNo,
            DestTelNo2 = source.DestTelNo2,
            DestDeptName = source.DestDeptName,
            DestChargeName = source.DestChargeName,
            DestDongBasic = source.DestDongBasic,
            DestAddress = source.DestAddress,
            DestDetailAddr = source.DestDetailAddr,
            DestSiDo = source.DestSiDo,
            DestGunGu = source.DestGunGu,
            DestDongRi = source.DestDongRi,
            DestLon = source.DestLon,
            DestLat = source.DestLat,
            DestSignImgUrl = source.DestSignImgUrl,
            DestDtSign = source.DestDtSign,
            Reserve = source.Reserve,
            DtReserve = source.DtReserve,
            ReserveBreakMinute = source.ReserveBreakMinute,
            FeeBasic = source.FeeBasic,
            FeeTotal = source.FeeTotal,
            FeeCommi = source.FeeCommi,
            FeeDriver = source.FeeDriver,
            FeePlus = source.FeePlus,
            FeeMinus = source.FeeMinus,
            FeeConn = source.FeeConn,
            FeeType = source.FeeType,
            MovilityFlag = source.MovilityFlag,
            DeliverFlag = source.DeliverFlag,
            StartDateFlag = source.StartDateFlag,
            StartDateDetail = source.StartDateDetail,
            DestDateFlag = source.DestDateFlag,
            DestDateDetail = source.DestDateDetail,
            CarWeightFlag = source.CarWeightFlag,
            TruckDetailFlag = source.TruckDetailFlag,
            StartLoadFlag = source.StartLoadFlag,
            DestUnloadFlag = source.DestUnloadFlag,
            DeliverMemo = source.DeliverMemo,
            DriverCode = source.DriverCode,
            DriverId = source.DriverId,
            DriverName = source.DriverName,
            DriverTelNo = source.DriverTelNo,
            DriverMemberCode = source.DriverMemberCode,
            DriverCenterId = source.DriverCenterId,
            DriverCenterName = source.DriverCenterName,
            DriverBusinessNo = source.DriverBusinessNo,
            Insung1SeqNo = source.Insung1SeqNo,
            Insung1State = source.Insung1State,
            Insung2SeqNo = source.Insung2SeqNo,
            Insung2State = source.Insung2State,
            Cargo24SeqNo = source.Cargo24SeqNo,
            Cargo24State = source.Cargo24State,
            OnecallSeqNo = source.OnecallSeqNo,
            OnecallState = source.OnecallState
        };
    }
    #endregion

    #region Properties - 기본정보
    public long KeyCode
    {
        get => tbOrder.KeyCode;
        set { tbOrder.KeyCode = value; OnPropertyChanged(nameof(KeyCode)); }
    }

    public long MemberCode
    {
        get => tbOrder.MemberCode;
        set { tbOrder.MemberCode = value; OnPropertyChanged(nameof(MemberCode)); }
    }

    public long CenterCode
    {
        get => tbOrder.CenterCode;
        set { tbOrder.CenterCode = value; OnPropertyChanged(nameof(CenterCode)); }
    }

    public long UserCode
    {
        get => tbOrder.UserCode;
        set { tbOrder.UserCode = value; OnPropertyChanged(nameof(UserCode)); }
    }

    public string UserName
    {
        get => tbOrder.UserName;
        set { tbOrder.UserName = value; OnPropertyChanged(nameof(UserName)); }
    }

    public DateTime DtRegist
    {
        get => tbOrder.DtRegist;
        set { tbOrder.DtRegist = value; OnPropertyChanged(nameof(DtRegist)); }
    }

    public DateTime DtUpdateLast
    {
        get => tbOrder.DtUpdateLast;
        set { tbOrder.DtUpdateLast = value; OnPropertyChanged(nameof(DtUpdateLast)); }
    }

    public string OrderState
    {
        get => tbOrder.OrderState;
        set { tbOrder.OrderState = value; OnPropertyChanged(nameof(OrderState)); }
    }

    public string OrderStateOld
    {
        get => tbOrder.OrderStateOld;
        set { tbOrder.OrderStateOld = value; OnPropertyChanged(nameof(OrderStateOld)); }
    }

    public string CancelReason
    {
        get => tbOrder.CancelReason;
        set { tbOrder.CancelReason = value; OnPropertyChanged(nameof(CancelReason)); }
    }

    public bool Share
    {
        get => tbOrder.Share;
        set { tbOrder.Share = value; OnPropertyChanged(nameof(Share)); }
    }

    public bool TaxBill
    {
        get => tbOrder.TaxBill;
        set { tbOrder.TaxBill = value; OnPropertyChanged(nameof(TaxBill)); }
    }
    #endregion

    #region Properties - 시간
    public TimeOnly? ReceiptTime
    {
        get => tbOrder.ReceiptTime;
        set { tbOrder.ReceiptTime = value; OnPropertyChanged(nameof(ReceiptTime)); }
    }

    public TimeOnly? AllocTime
    {
        get => tbOrder.AllocTime;
        set { tbOrder.AllocTime = value; OnPropertyChanged(nameof(AllocTime)); }
    }

    public TimeOnly? RunTime
    {
        get => tbOrder.RunTime;
        set { tbOrder.RunTime = value; OnPropertyChanged(nameof(RunTime)); }
    }

    public TimeOnly? FinishTime
    {
        get => tbOrder.FinishTime;
        set { tbOrder.FinishTime = value; OnPropertyChanged(nameof(FinishTime)); }
    }
    #endregion

    #region Properties - 의뢰자(Call)
    public long CallCompCode
    {
        get => tbOrder.CallCompCode;
        set { tbOrder.CallCompCode = value; OnPropertyChanged(nameof(CallCompCode)); }
    }

    public string CallCompName
    {
        get => tbOrder.CallCompName;
        set { tbOrder.CallCompName = value; OnPropertyChanged(nameof(CallCompName)); }
    }

    public string CallCustFrom
    {
        get => tbOrder.CallCustFrom;
        set { tbOrder.CallCustFrom = value; OnPropertyChanged(nameof(CallCustFrom)); }
    }

    public long CallCustCodeE
    {
        get => tbOrder.CallCustCodeE;
        set { tbOrder.CallCustCodeE = value; OnPropertyChanged(nameof(CallCustCodeE)); }
    }

    public long CallCustCodeK
    {
        get => tbOrder.CallCustCodeK;
        set { tbOrder.CallCustCodeK = value; OnPropertyChanged(nameof(CallCustCodeK)); }
    }

    public string CallCustName
    {
        get => tbOrder.CallCustName;
        set { tbOrder.CallCustName = value; OnPropertyChanged(nameof(CallCustName)); }
    }

    public string CallTelNo
    {
        get => tbOrder.CallTelNo;
        set { tbOrder.CallTelNo = value; OnPropertyChanged(nameof(CallTelNo)); }
    }

    public string CallTelNo2
    {
        get => tbOrder.CallTelNo2;
        set { tbOrder.CallTelNo2 = value; OnPropertyChanged(nameof(CallTelNo2)); }
    }

    public string CallDeptName
    {
        get => tbOrder.CallDeptName;
        set { tbOrder.CallDeptName = value; OnPropertyChanged(nameof(CallDeptName)); }
    }

    public string CallChargeName
    {
        get => tbOrder.CallChargeName;
        set { tbOrder.CallChargeName = value; OnPropertyChanged(nameof(CallChargeName)); }
    }

    public string CallDongBasic
    {
        get => tbOrder.CallDongBasic;
        set { tbOrder.CallDongBasic = value; OnPropertyChanged(nameof(CallDongBasic)); }
    }

    public string CallAddress
    {
        get => tbOrder.CallAddress;
        set { tbOrder.CallAddress = value; OnPropertyChanged(nameof(CallAddress)); }
    }

    public string CallDetailAddr
    {
        get => tbOrder.CallDetailAddr;
        set { tbOrder.CallDetailAddr = value; OnPropertyChanged(nameof(CallDetailAddr)); }
    }

    public string CallRemarks
    {
        get => tbOrder.CallRemarks;
        set { tbOrder.CallRemarks = value; OnPropertyChanged(nameof(CallRemarks)); }
    }

    public string CallMemo
    {
        get => tbOrder.CallMemo;
        set { tbOrder.CallMemo = value; OnPropertyChanged(nameof(CallMemo)); }
    }
    #endregion

    #region Properties - 출발지(Start)
    public long StartCustCodeE
    {
        get => tbOrder.StartCustCodeE;
        set { tbOrder.StartCustCodeE = value; OnPropertyChanged(nameof(StartCustCodeE)); }
    }

    public long StartCustCodeK
    {
        get => tbOrder.StartCustCodeK;
        set { tbOrder.StartCustCodeK = value; OnPropertyChanged(nameof(StartCustCodeK)); }
    }

    public string StartCustName
    {
        get => tbOrder.StartCustName;
        set { tbOrder.StartCustName = value; OnPropertyChanged(nameof(StartCustName)); }
    }

    public string StartTelNo
    {
        get => tbOrder.StartTelNo;
        set { tbOrder.StartTelNo = value; OnPropertyChanged(nameof(StartTelNo)); }
    }

    public string StartTelNo2
    {
        get => tbOrder.StartTelNo2;
        set { tbOrder.StartTelNo2 = value; OnPropertyChanged(nameof(StartTelNo2)); }
    }

    public string StartDeptName
    {
        get => tbOrder.StartDeptName;
        set { tbOrder.StartDeptName = value; OnPropertyChanged(nameof(StartDeptName)); }
    }

    public string StartChargeName
    {
        get => tbOrder.StartChargeName;
        set { tbOrder.StartChargeName = value; OnPropertyChanged(nameof(StartChargeName)); }
    }

    public string StartDongBasic
    {
        get => tbOrder.StartDongBasic;
        set { tbOrder.StartDongBasic = value; OnPropertyChanged(nameof(StartDongBasic)); }
    }

    public string StartAddress
    {
        get => tbOrder.StartAddress;
        set { tbOrder.StartAddress = value; OnPropertyChanged(nameof(StartAddress)); }
    }

    public string StartDetailAddr
    {
        get => tbOrder.StartDetailAddr;
        set { tbOrder.StartDetailAddr = value; OnPropertyChanged(nameof(StartDetailAddr)); }
    }

    public string StartSiDo
    {
        get => tbOrder.StartSiDo;
        set { tbOrder.StartSiDo = value; OnPropertyChanged(nameof(StartSiDo)); }
    }

    public string StartGunGu
    {
        get => tbOrder.StartGunGu;
        set { tbOrder.StartGunGu = value; OnPropertyChanged(nameof(StartGunGu)); }
    }

    public string StartDongRi
    {
        get => tbOrder.StartDongRi;
        set { tbOrder.StartDongRi = value; OnPropertyChanged(nameof(StartDongRi)); }
    }

    public int StartLon
    {
        get => tbOrder.StartLon;
        set { tbOrder.StartLon = value; OnPropertyChanged(nameof(StartLon)); }
    }

    public int StartLat
    {
        get => tbOrder.StartLat;
        set { tbOrder.StartLat = value; OnPropertyChanged(nameof(StartLat)); }
    }

    public string StartSignImgUrl
    {
        get => tbOrder.StartSignImgUrl;
        set { tbOrder.StartSignImgUrl = value; OnPropertyChanged(nameof(StartSignImgUrl)); }
    }

    public DateTime? StartDtSign
    {
        get => tbOrder.StartDtSign;
        set { tbOrder.StartDtSign = value; OnPropertyChanged(nameof(StartDtSign)); }
    }
    #endregion

    #region Properties - 도착지(Dest)
    public long DestCustCodeE
    {
        get => tbOrder.DestCustCodeE;
        set { tbOrder.DestCustCodeE = value; OnPropertyChanged(nameof(DestCustCodeE)); }
    }

    public long DestCustCodeK
    {
        get => tbOrder.DestCustCodeK;
        set { tbOrder.DestCustCodeK = value; OnPropertyChanged(nameof(DestCustCodeK)); }
    }

    public string DestCustName
    {
        get => tbOrder.DestCustName;
        set { tbOrder.DestCustName = value; OnPropertyChanged(nameof(DestCustName)); }
    }

    public string DestTelNo
    {
        get => tbOrder.DestTelNo;
        set { tbOrder.DestTelNo = value; OnPropertyChanged(nameof(DestTelNo)); }
    }

    public string DestTelNo2
    {
        get => tbOrder.DestTelNo2;
        set { tbOrder.DestTelNo2 = value; OnPropertyChanged(nameof(DestTelNo2)); }
    }

    public string DestDeptName
    {
        get => tbOrder.DestDeptName;
        set { tbOrder.DestDeptName = value; OnPropertyChanged(nameof(DestDeptName)); }
    }

    public string DestChargeName
    {
        get => tbOrder.DestChargeName;
        set { tbOrder.DestChargeName = value; OnPropertyChanged(nameof(DestChargeName)); }
    }

    public string DestDongBasic
    {
        get => tbOrder.DestDongBasic;
        set { tbOrder.DestDongBasic = value; OnPropertyChanged(nameof(DestDongBasic)); }
    }

    public string DestAddress
    {
        get => tbOrder.DestAddress;
        set { tbOrder.DestAddress = value; OnPropertyChanged(nameof(DestAddress)); }
    }

    public string DestDetailAddr
    {
        get => tbOrder.DestDetailAddr;
        set { tbOrder.DestDetailAddr = value; OnPropertyChanged(nameof(DestDetailAddr)); }
    }

    public string DestSiDo
    {
        get => tbOrder.DestSiDo;
        set { tbOrder.DestSiDo = value; OnPropertyChanged(nameof(DestSiDo)); }
    }

    public string DestGunGu
    {
        get => tbOrder.DestGunGu;
        set { tbOrder.DestGunGu = value; OnPropertyChanged(nameof(DestGunGu)); }
    }

    public string DestDongRi
    {
        get => tbOrder.DestDongRi;
        set { tbOrder.DestDongRi = value; OnPropertyChanged(nameof(DestDongRi)); }
    }

    public int DestLon
    {
        get => tbOrder.DestLon;
        set { tbOrder.DestLon = value; OnPropertyChanged(nameof(DestLon)); }
    }

    public int DestLat
    {
        get => tbOrder.DestLat;
        set { tbOrder.DestLat = value; OnPropertyChanged(nameof(DestLat)); }
    }

    public string DestSignImgUrl
    {
        get => tbOrder.DestSignImgUrl;
        set { tbOrder.DestSignImgUrl = value; OnPropertyChanged(nameof(DestSignImgUrl)); }
    }

    public DateTime? DestDtSign
    {
        get => tbOrder.DestDtSign;
        set { tbOrder.DestDtSign = value; OnPropertyChanged(nameof(DestDtSign)); }
    }
    #endregion

    #region Properties - 예약
    public bool Reserve
    {
        get => tbOrder.Reserve;
        set { tbOrder.Reserve = value; OnPropertyChanged(nameof(Reserve)); }
    }

    public DateTime? DtReserve
    {
        get => tbOrder.DtReserve;
        set { tbOrder.DtReserve = value; OnPropertyChanged(nameof(DtReserve)); }
    }

    public int ReserveBreakMinute
    {
        get => tbOrder.ReserveBreakMinute;
        set { tbOrder.ReserveBreakMinute = value; OnPropertyChanged(nameof(ReserveBreakMinute)); }
    }
    #endregion

    #region Properties - 요금
    public int FeeBasic
    {
        get => tbOrder.FeeBasic;
        set { tbOrder.FeeBasic = value; OnPropertyChanged(nameof(FeeBasic)); }
    }

    public int FeeTotal
    {
        get => tbOrder.FeeTotal;
        set { tbOrder.FeeTotal = value; OnPropertyChanged(nameof(FeeTotal)); }
    }

    public int FeeCommi
    {
        get => tbOrder.FeeCommi;
        set { tbOrder.FeeCommi = value; OnPropertyChanged(nameof(FeeCommi)); }
    }

    public int FeeDriver
    {
        get => tbOrder.FeeDriver;
        set { tbOrder.FeeDriver = value; OnPropertyChanged(nameof(FeeDriver)); }
    }

    public int FeePlus
    {
        get => tbOrder.FeePlus;
        set { tbOrder.FeePlus = value; OnPropertyChanged(nameof(FeePlus)); }
    }

    public int FeeMinus
    {
        get => tbOrder.FeeMinus;
        set { tbOrder.FeeMinus = value; OnPropertyChanged(nameof(FeeMinus)); }
    }

    public int FeeConn
    {
        get => tbOrder.FeeConn;
        set { tbOrder.FeeConn = value; OnPropertyChanged(nameof(FeeConn)); }
    }

    public string FeeType
    {
        get => tbOrder.FeeType;
        set { tbOrder.FeeType = value; OnPropertyChanged(nameof(FeeType)); }
    }
    #endregion

    #region Properties - 배송 Flag
    public string MovilityFlag
    {
        get => tbOrder.MovilityFlag;
        set { tbOrder.MovilityFlag = value; OnPropertyChanged(nameof(MovilityFlag)); }
    }

    public string DeliverFlag
    {
        get => tbOrder.DeliverFlag;
        set { tbOrder.DeliverFlag = value; OnPropertyChanged(nameof(DeliverFlag)); }
    }

    public string StartDateFlag
    {
        get => tbOrder.StartDateFlag;
        set { tbOrder.StartDateFlag = value; OnPropertyChanged(nameof(StartDateFlag)); }
    }

    public string StartDateDetail
    {
        get => tbOrder.StartDateDetail;
        set { tbOrder.StartDateDetail = value; OnPropertyChanged(nameof(StartDateDetail)); }
    }

    public string DestDateFlag
    {
        get => tbOrder.DestDateFlag;
        set { tbOrder.DestDateFlag = value; OnPropertyChanged(nameof(DestDateFlag)); }
    }

    public string DestDateDetail
    {
        get => tbOrder.DestDateDetail;
        set { tbOrder.DestDateDetail = value; OnPropertyChanged(nameof(DestDateDetail)); }
    }

    public string CarWeightFlag
    {
        get => tbOrder.CarWeightFlag;
        set { tbOrder.CarWeightFlag = value; OnPropertyChanged(nameof(CarWeightFlag)); }
    }

    public string TruckDetailFlag
    {
        get => tbOrder.TruckDetailFlag;
        set { tbOrder.TruckDetailFlag = value; OnPropertyChanged(nameof(TruckDetailFlag)); }
    }

    public string StartLoadFlag
    {
        get => tbOrder.StartLoadFlag;
        set { tbOrder.StartLoadFlag = value; OnPropertyChanged(nameof(StartLoadFlag)); }
    }

    public string DestUnloadFlag
    {
        get => tbOrder.DestUnloadFlag;
        set { tbOrder.DestUnloadFlag = value; OnPropertyChanged(nameof(DestUnloadFlag)); }
    }

    public string DeliverMemo
    {
        get => tbOrder.DeliverMemo;
        set { tbOrder.DeliverMemo = value; OnPropertyChanged(nameof(DeliverMemo)); }
    }
    #endregion

    #region Properties - 기사정보
    public long DriverCode
    {
        get => tbOrder.DriverCode;
        set { tbOrder.DriverCode = value; OnPropertyChanged(nameof(DriverCode)); }
    }

    public string DriverId
    {
        get => tbOrder.DriverId;
        set { tbOrder.DriverId = value; OnPropertyChanged(nameof(DriverId)); }
    }

    public string DriverName
    {
        get => tbOrder.DriverName;
        set { tbOrder.DriverName = value; OnPropertyChanged(nameof(DriverName)); }
    }

    public string DriverTelNo
    {
        get => tbOrder.DriverTelNo;
        set { tbOrder.DriverTelNo = value; OnPropertyChanged(nameof(DriverTelNo)); }
    }

    public long DriverMemberCode
    {
        get => tbOrder.DriverMemberCode;
        set { tbOrder.DriverMemberCode = value; OnPropertyChanged(nameof(DriverMemberCode)); }
    }

    public string DriverCenterId
    {
        get => tbOrder.DriverCenterId;
        set { tbOrder.DriverCenterId = value; OnPropertyChanged(nameof(DriverCenterId)); }
    }

    public string DriverCenterName
    {
        get => tbOrder.DriverCenterName;
        set { tbOrder.DriverCenterName = value; OnPropertyChanged(nameof(DriverCenterName)); }
    }

    public string DriverBusinessNo
    {
        get => tbOrder.DriverBusinessNo;
        set { tbOrder.DriverBusinessNo = value; OnPropertyChanged(nameof(DriverBusinessNo)); }
    }
    #endregion

    #region Properties - 외부연동
    public string Insung1SeqNo
    {
        get => tbOrder.Insung1SeqNo;
        set { tbOrder.Insung1SeqNo = value; OnPropertyChanged(nameof(Insung1SeqNo)); }
    }

    public string Insung1State
    {
        get => tbOrder.Insung1State;
        set { tbOrder.Insung1State = value; OnPropertyChanged(nameof(Insung1State)); }
    }

    public string Insung2SeqNo
    {
        get => tbOrder.Insung2SeqNo;
        set { tbOrder.Insung2SeqNo = value; OnPropertyChanged(nameof(Insung2SeqNo)); }
    }

    public string Insung2State
    {
        get => tbOrder.Insung2State;
        set { tbOrder.Insung2State = value; OnPropertyChanged(nameof(Insung2State)); }
    }

    public string Cargo24SeqNo
    {
        get => tbOrder.Cargo24SeqNo;
        set { tbOrder.Cargo24SeqNo = value; OnPropertyChanged(nameof(Cargo24SeqNo)); }
    }

    public string Cargo24State
    {
        get => tbOrder.Cargo24State;
        set { tbOrder.Cargo24State = value; OnPropertyChanged(nameof(Cargo24State)); }
    }

    public string OnecallSeqNo
    {
        get => tbOrder.OnecallSeqNo;
        set { tbOrder.OnecallSeqNo = value; OnPropertyChanged(nameof(OnecallSeqNo)); }
    }

    public string OnecallState
    {
        get => tbOrder.OnecallState;
        set { tbOrder.OnecallState = value; OnPropertyChanged(nameof(OnecallState)); }
    }
    #endregion
}

public class VmOrder_StatusPage_Tel070 : INotifyPropertyChanged
{
    #region Property Event
    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    #endregion

    #region Variables
    private TbTelMainRing _TbTelMainRing;
    #endregion

    #region 생성자
    public VmOrder_StatusPage_Tel070(TbTelMainRing tbTelMainRing)
    {
        _TbTelMainRing = tbTelMainRing;
    }
    #endregion

    #region Properties - 공용
    // 수신시간
    public string RingDate
    {
        get => _TbTelMainRing.RingDate?.ToString(StdConst_Var.DTFORMAT_EXCEPT_YEAR);
        set => OnPropertyChanged(nameof(RingDate));
    }

    // 상대전화
    public string YourTelNum
    {
        get => StdConvert.ToPhoneNumberFormat(_TbTelMainRing.YourTelNum);
        set
        {
            _TbTelMainRing.YourTelNum = value;
            OnPropertyChanged(nameof(YourTelNum));
        }
    }

    // 고객명
    public string CustName
    {
        get => _TbTelMainRing.CustName ?? string.Empty;
        set
        {
            _TbTelMainRing.CustName = value;
            OnPropertyChanged(nameof(CustName));
        }
    }

    // 담당자
    public string ChargeName
    {
        get => _TbTelMainRing.ChargeName ?? string.Empty;
        set
        {
            _TbTelMainRing.ChargeName = value;
            OnPropertyChanged(nameof(ChargeName));
        }
    }

    // 부서명
    public string DeptName
    {
        get => _TbTelMainRing.DeptName ?? string.Empty;
        set
        {
            _TbTelMainRing.DeptName = value;
            OnPropertyChanged(nameof(DeptName));
        }
    }

    // 거래처명
    public string CompanyName
    {
        get => _TbTelMainRing.CompanyName ?? string.Empty;
        set
        {
            _TbTelMainRing.CompanyName = value;
            OnPropertyChanged(nameof(CompanyName));
        }
    }

    // 콜센터명
    public string CenterName
    {
        get => _TbTelMainRing.CenterName ?? string.Empty;
        set
        {
            _TbTelMainRing.CenterName = value;
            OnPropertyChanged(nameof(CenterName));
        }
    }

    // 내전화 별명
    public string Alias
    {
        get => _TbTelMainRing.Alias ?? string.Empty;
        set
        {
            _TbTelMainRing.Alias = value;
            OnPropertyChanged(nameof(Alias));
        }
    }

    // 내전화
    public string MyTelNum
    {
        get => StdConvert.ToPhoneNumberFormat(_TbTelMainRing.MyTelNum);
        set
        {
            _TbTelMainRing.MyTelNum = value;
            OnPropertyChanged(nameof(MyTelNum));
        }
    }

    // 기준동
    public string BasicDong
    {
        get => _TbTelMainRing.BasicDong ?? string.Empty;
        set
        {
            _TbTelMainRing.BasicDong = value;
            OnPropertyChanged(nameof(BasicDong));
        }
    }

    // 메모
    public string Memo
    {
        get => _TbTelMainRing.Memo ?? string.Empty;
        set
        {
            _TbTelMainRing.Memo = value;
            OnPropertyChanged(nameof(Memo));
        }
    }
    #endregion
}
#nullable enable
