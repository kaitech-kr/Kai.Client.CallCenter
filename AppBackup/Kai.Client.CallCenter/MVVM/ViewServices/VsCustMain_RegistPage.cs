using System.Windows;
using System.Collections.ObjectModel;

using Kai.Client.CallCenter.MVVM.ViewModels;
using Kai.Common.NetDll_WpfCtrl.NetWnds;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Results;

namespace Kai.Client.CallCenter.MVVM.ViewServices;
#nullable disable
public class VsCustMain_RegistPage
{
    public static ObservableCollection<VmCustMain_RegistPage> oc_VmCustMainForPage { get; set; } = new ObservableCollection<VmCustMain_RegistPage>();

    public static void LoadData(Window owner, List<TbAllWith> list)
    {
        oc_VmCustMainForPage.Clear();
        foreach (var tbWith in list) oc_VmCustMainForPage.Add(new VmCustMain_RegistPage(tbWith)); 
    }

    public static void LoadData(Window owner, List<TbAllWith> list,
        bool? use, string custName, string deptName, string chargeName, string telNo, string dongDetail, string internetID, string compName)
    {
        oc_VmCustMainForPage.Clear();

        var filteredList = list.Where(x =>
            (use == null || x.custMain.Using == use) &&
            (string.IsNullOrWhiteSpace(custName) || (x.custMain.CustName?.Contains(custName, StringComparison.OrdinalIgnoreCase) ?? false)) &&
            (string.IsNullOrWhiteSpace(deptName) || (x.custMain.DeptName?.Contains(deptName, StringComparison.OrdinalIgnoreCase) ?? false)) &&
            (string.IsNullOrWhiteSpace(chargeName) || (x.custMain.ChargeName?.Contains(chargeName, StringComparison.OrdinalIgnoreCase) ?? false)) &&
            (string.IsNullOrWhiteSpace(telNo) ||
                (x.custMain.TelNo1?.Contains(telNo, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (x.custMain.TelNo2?.Contains(telNo, StringComparison.OrdinalIgnoreCase) ?? false)) &&
            (string.IsNullOrWhiteSpace(dongDetail) || (x.custMain.DetailAddr?.Contains(dongDetail, StringComparison.OrdinalIgnoreCase) ?? false)) &&
            (string.IsNullOrWhiteSpace(internetID) || (x.custMain.CustId?.Contains(internetID, StringComparison.OrdinalIgnoreCase) ?? false)) && 
            (string.IsNullOrWhiteSpace(compName) || (x.company.CompName?.Contains(compName, StringComparison.OrdinalIgnoreCase) ?? false))
        );


        foreach (var tb in filteredList)
        {
            oc_VmCustMainForPage.Add(new VmCustMain_RegistPage(tb));
        }
    }

    public static int GetIndexByKeyCode(long keyCode)
    {
        VmCustMain_RegistPage vm = VsCustMain_RegistPage.oc_VmCustMainForPage.FirstOrDefault(x => x.KeyCode == keyCode);
        return VsCustMain_RegistPage.oc_VmCustMainForPage.IndexOf(vm);
    }
}
#nullable disable