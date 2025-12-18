using System.ComponentModel;

using Kai.Common.StdDll_Common;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models;

namespace Kai.Client.CallCenter.MVVM.ViewModels;
#nullable disable
public class VmOrder_StatusPage_Order : INotifyPropertyChanged
{
    #region Variables
    public TbOrder tbOrder;
    #endregion

    #region Property Event
    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    #endregion

    #region 생성자
    public VmOrder_StatusPage_Order(TbOrder tb)//, int nTernNum)
    {
        tbOrder = tb;
    }
    #endregion

    #region Properties
    // Key
    public long KeyCode
    {
        get => tbOrder.KeyCode;
        set => OnPropertyChanged(nameof(KeyCode));
    }

    // 상태
    //private string _OrderState;
    public string OrderState
    {
        get => tbOrder.OrderState;
        set => OnPropertyChanged(nameof(OrderState));
    }

    // 생성시간
    public string RegTime
    {
        get => tbOrder.DtRegist.ToString(StdConst_Var.DTFORMAT_EXCEPT_YEARSEC);
        set => OnPropertyChanged(nameof(RegTime));
    }

    // 의뢰자 이전소속
    public string CallCustFrom
    {
        get => tbOrder.CallCustFrom;
        set => OnPropertyChanged(nameof(CallCustFrom));
    }

    // 인성1
    public string Insung1
    {
        get => tbOrder.Insung1;
        set => OnPropertyChanged(nameof(Insung1));
    }

    // 인성1
    public string Insung2
    {
        get => tbOrder.Insung2;
        set => OnPropertyChanged(nameof(Insung2));
    }

    // 화물24시
    public string Cargo24
    {
        get => tbOrder.Cargo24;
        set => OnPropertyChanged(nameof(Cargo24));
    }

    // 원콜
    public string Onecall
    {
        get => tbOrder.Onecall;
        set => OnPropertyChanged(nameof(Onecall));
    }

    public string CallCompName
    {
        get => tbOrder.CallCompName;
        set => OnPropertyChanged(nameof(CallCompName));
    }

    // 고객명
    public string CallCustName
    {
        get => tbOrder.CallCustName;
        set => OnPropertyChanged(nameof(CallCustName));
    }

    // 고객부서명
    public string CallDeptName
    {
        get => tbOrder.CallDeptName;
        set => OnPropertyChanged(nameof(CallDeptName));
    }

    // 고객담당
    public string CallChargeName
    {
        get => tbOrder.CallChargeName;
        set => OnPropertyChanged(nameof(CallChargeName));
    }

    // 고객전화번호
    public string CallTelNo
    {
        get => StdConvert.ToPhoneNumberFormat(tbOrder.CallTelNo);
        set => OnPropertyChanged(nameof(CallTelNo));
    }

    // 출발동
    public string StartDongBasic
    {
        get => tbOrder.StartDongBasic;
        set => OnPropertyChanged(nameof(StartDongBasic));
    }

    // 도착동
    public string DestDongBasic
    {
        get => tbOrder.DestDongBasic;
        set => OnPropertyChanged(nameof(DestDongBasic));
    }

    // 기본요금
    public string FeeBasic
    {
        get => $"{tbOrder.FeeBasic:##,###}";
        set => OnPropertyChanged(nameof(FeeBasic));
    }

    // 추가요금
    public string FeePlus
    {
        get => $"{tbOrder.FeePlus:##,###}";
        set => OnPropertyChanged(nameof(FeePlus));
    }

    // 할인요금
    public string FeeMinus
    {
        get => $"{tbOrder.FeeMinus:##,###}";
        set => OnPropertyChanged(nameof(FeeMinus));
    }

    // 탁송료
    public string FeeConn
    {
        get => $"{tbOrder.FeeConn:##,###}";
        set => OnPropertyChanged(nameof(FeeConn));
    }

    // 요금합계
    public string sFeeTotal
    {
        get => $"{tbOrder.FeeTotal:##,###}";
        set => OnPropertyChanged(nameof(sFeeTotal));
    }

    // 기사요금
    public string FeeDriver
    {
        get => $"{tbOrder.FeeDriver:##,###}";
        set => OnPropertyChanged(nameof(FeeDriver));
    }

    // 기사처리비

    // 차량종류(CarType)
    public string CarType
    {
        get
        {
            if (tbOrder.CarType == "오토") return "";
            if (tbOrder.CarType == "다마") return "다마스";
            return tbOrder.CarType;
        }
        set => OnPropertyChanged(nameof(CarType));
    }

    // 요금타입(FeeType)
    public string FeeType
    {
        get => tbOrder.FeeType == "선불" ? "" : tbOrder.FeeType;
        set => OnPropertyChanged(nameof(FeeType));
    }

    // 배송타입(DeliverType)
    public string DeliverType
    {
        get => tbOrder.DeliverType == "편도" ? "" : tbOrder.DeliverType;
        set => OnPropertyChanged(nameof(DeliverType));
    }

    // 적요
    public string Remarks
    {
        get => tbOrder.OrderRemarks;
        set => OnPropertyChanged(nameof(Remarks));
    }

    // 오더메모
    public string OrderMemo
    {
        get => tbOrder.OrderMemo;
        set => OnPropertyChanged(nameof(OrderMemo));
    }

    // 공유
    public bool OrderShare
    {
        get => tbOrder.Share;
        set => OnPropertyChanged(nameof(OrderShare));
    }

    // 세금게산서
    public bool TaxBill
    {
        get => tbOrder.TaxBill;
        set => OnPropertyChanged(nameof(TaxBill));
    }

    // 접수시간
    public string ReceiptTime
    {
        get => tbOrder.ReceiptTime?.ToString(@"HH\:mm") ?? "00:00";
        set => OnPropertyChanged(nameof(ReceiptTime));
    }

    // 배차시간
    public string AllocTime
    {
        get => tbOrder.AllocTime?.ToString(@"HH\:mm") ?? "00:00";
        set => OnPropertyChanged(nameof(AllocTime));
    }

    // 운행시간
    public string RunTime
    {
        get => tbOrder.RunTime?.ToString(@"HH\:mm") ?? "00:00";
        set => OnPropertyChanged(nameof(RunTime));
    }

    // 완료시간
    public string FinishTime
    {
        get => tbOrder.FinishTime?.ToString(@"HH\:mm") ?? "00:00";
        set => OnPropertyChanged(nameof(FinishTime));
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