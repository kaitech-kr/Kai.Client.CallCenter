using Kai.Common.StdDll_Common;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Results;
using System.ComponentModel;

namespace Kai.Client.CallCenter.MVVM.ViewModels;
#nullable disable
public class VmCustMain_SearchedWnd : INotifyPropertyChanged
{
    #region Property Event
    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    #endregion

    #region 생성자
    public VmCustMain_SearchedWnd(TbAllWith tbWith)
    {
        _tbAllWith = tbWith;
    }
    #endregion end - 생성자

    #region Table Properties
    // TbAllWith
    private TbAllWith _tbAllWith;
    public TbAllWith tbAllWith
    {
        get => _tbAllWith;
        set => _tbAllWith = value;
    }

    // TbCustMain
    public TbCustMain tbCustMain => _tbAllWith.custMain;

    // TbCallCenter
    public TbCallCenter tbCallCenter => _tbAllWith.callCenter;

    // TbCompany
    public TbCompany tbCompany => _tbAllWith.company;
    #endregion end - Table Properties

    #region TbCallCenter Column Properties
    // CenterName
    public string CenterName
    {
        get => tbCallCenter.CenterName;
        set => OnPropertyChanged(nameof(CenterName));
    }
    #endregion end - TbCallCenter Column Properties

    #region TbCustMain Column Properties
    // CustName
    //private string _CustName;
    public string CustName
    {
        get => tbCustMain.CustName;
        set => OnPropertyChanged(nameof(CustName)); 
    }

    // DeptName
    public string CustFrom
    {
        get => tbCustMain.BeforeBelong;
        set => OnPropertyChanged(nameof(CustFrom));
    }

    // DeptName
    public string DeptName
    {
        get => tbCustMain.DeptName;
        set => OnPropertyChanged(nameof(DeptName));
    }

    // ChargeName
    public string ChargeName
    {
        get => tbCustMain.ChargeName;
        set => OnPropertyChanged(nameof(ChargeName));
    }

    // TelNo1
    public string TelNo1
    {
        get => StdConvert.ToPhoneNumberFormat(tbCustMain.TelNo1);
        set => OnPropertyChanged(nameof(TelNo1));
    }

    // TelNo2
    public string TelNo2
    {
        get => StdConvert.ToPhoneNumberFormat(tbCustMain.TelNo2);
        set => OnPropertyChanged(nameof(TelNo2));
    }

    // TradeType
    public string TradeType
    {
        get => tbCustMain.TradeType;
        set => OnPropertyChanged(nameof(TradeType));
    }

    // DongBasic
    public string DongBasic
    {
        get => tbCustMain.DongBasic;
        set => OnPropertyChanged(nameof(DongBasic));
    }

    // DetailAddr
    public string DetailAddr
    {
        get => tbCustMain.DetailAddr;
        set => OnPropertyChanged(nameof(DetailAddr));
    }

    // Using
    public string Using
    {
        get => tbCustMain.Using ? "Y" : "N";
        set => OnPropertyChanged(nameof(Using));
    }

    // KeyCode
    public long KeyCode
    {
        get => tbCustMain.KeyCode;
        set => OnPropertyChanged(nameof(KeyCode));
    }

    // 거래처명
    public string CompName
    {
        get => tbCompany?.CompName ?? string.Empty;
        set => OnPropertyChanged(nameof(CompName));
    }

    // CustID
    public string CustId
    {
        get => tbCustMain.CustId;
        set => OnPropertyChanged(nameof(CustId));
    }
    #endregion end - Tb_TbCustMain Column Properties
}
#nullable enable
