using Kai.Client.CallCenter.Classes;
using Kai.Common.StdDll_Common;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Results;
using System.ComponentModel;

namespace Kai.Client.CallCenter.MVVM.ViewModels;
#nullable disable
public class VmCompany_RegistPage_Comp : INotifyPropertyChanged
{
    #region Property Event
    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    #endregion

    #region 생성자
    public VmCompany_RegistPage_Comp(TbCompany company)
    {
        _TbCompany = company;
    }
    #endregion end - 생성자

    #region TbCompany
    // TbCompany
    private TbCompany _TbCompany;
    public TbCompany TbCompany
    {
        get => _TbCompany;
        set => _TbCompany = value;
    }

    // 코드
    public long KeyCode
    {
        get => _TbCompany.KeyCode;
        set
        {
            if (_TbCompany != null)
            {
                _TbCompany.KeyCode = value;
            }
            OnPropertyChanged(nameof(KeyCode));
        }
    }

    // 거래처명
    public string CompName
    {
        get => _TbCompany?.CompName ?? ""; // null이면 "" 반환
        set
        {
            if (_TbCompany != null)
            {
                _TbCompany.CompName = value;
            }
            OnPropertyChanged(nameof(CompName));
        }
    }

    // TelNo
    private string _TelNo;
    public string TelNo
    {
        get => StdConvert.ToPhoneNumberFormat(_TbCompany.TelNo);
        set
        {
            _TelNo = StdConvert.MakePhoneNumberToDigit(_TbCompany.TelNo);
            OnPropertyChanged(nameof(TelNo));
        }
    }

    // 거래타입
    private string _TradeType;
    public string TradeType
    {
        get => StdConvert.ToPhoneNumberFormat(_TbCompany.TradeType);
        set
        {
            _TradeType = StdConvert.MakePhoneNumberToDigit(_TbCompany.TradeType);
            OnPropertyChanged(nameof(TradeType));
        }
    }

    // 대표자
    private string _Owner;
    public string Owner
    {
        get => StdConvert.ToPhoneNumberFormat(_TbCompany.Owner);
        set
        {
            _Owner = StdConvert.MakePhoneNumberToDigit(_TbCompany.Owner);
            OnPropertyChanged(nameof(Owner));
        }
    }

    // 메모
    private string _Memo;
    public string Memo
    {
        get => StdConvert.ToPhoneNumberFormat(_TbCompany.Memo);
        set
        {
            _Memo = StdConvert.MakePhoneNumberToDigit(_TbCompany.Memo);
            OnPropertyChanged(nameof(Memo));
        }
    }

    // 적요
    //private string _Remarks;
    //public string Remarks
    //{
    //    get => StdConvert.ToPhoneNumberFormat(_TbCompany.Remarks);
    //    set
    //    {
    //        _Remarks = StdConvert.MakePhoneNumberToDigit(_TbCompany.Remarks);
    //        OnPropertyChanged(nameof(Remarks));
    //    }
    //}

    // 등록일자
    public string DtRegist
    {
        get => _TbCompany.DtRegist.ToString(StdConst_Var.DTFORMAT_DATEONLY);
        set => OnPropertyChanged(nameof(DtRegist));
    }
    #endregion End TbCompany
}

public class VmCompany_RegistPage_Cust : INotifyPropertyChanged
{
    #region Property Event
    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    #endregion

    #region 생성자
    public VmCompany_RegistPage_Cust(TbCustMain cust)
    {
        _TbCustMain = cust;
    }
    #endregion end - 생성자

    #region TbCustMain
    // TbCustMain
    private TbCustMain _TbCustMain;
    public TbCustMain TbCustMain
    {
        get => _TbCustMain;
        set => _TbCustMain = value;
    }

    // 고객명
    public string CustName
    {
        get => _TbCustMain?.CustName ?? ""; // null이면 "" 반환
        set
        {
            if (_TbCustMain != null)
            {
                _TbCustMain.CustName = value;
            }
            OnPropertyChanged(nameof(CustName));
        }
    }

    // TelNo
    private string _TelNo;
    public string TelNo
    {
        get => StdConvert.ToPhoneNumberFormat(_TbCustMain.TelNo1);
        set
        {
            _TelNo = StdConvert.MakePhoneNumberToDigit(_TbCustMain.TelNo1);
            OnPropertyChanged(nameof(TelNo));
        }
    }

    // 담당/직위
    public string ChargeName
    {
        get => _TbCustMain?.ChargeName ?? ""; // null이면 "" 반환
        set
        {
            _TbCustMain.ChargeName = value;
            OnPropertyChanged(nameof(ChargeName));
        }
    }

    // 거래타입
    public string TradeType
    {
        get => _TbCustMain?.TradeType ?? ""; // null이면 "" 반환
        set
        {
            _TbCustMain.TradeType = value;
            OnPropertyChanged(nameof(TradeType));
        }
    }

    // 메모
    private string _Memo;
    public string Memo
    {
        get => StdConvert.ToPhoneNumberFormat(_TbCustMain.Memo);
        set
        {
            _Memo = StdConvert.MakePhoneNumberToDigit(_TbCustMain.Memo);
            OnPropertyChanged(nameof(Memo));
        }
    }

    // 사용여부
    private bool _Using;
    public bool Using
    {
        get => _TbCustMain.Using;
        set
        {
            _Using = _TbCustMain.Using;
            OnPropertyChanged(nameof(Memo));
        }
    }

    #endregion end - TbCustMain
}
#nullable enable