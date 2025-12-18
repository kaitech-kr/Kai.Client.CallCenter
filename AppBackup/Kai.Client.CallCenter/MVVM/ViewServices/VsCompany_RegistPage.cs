using System.Windows;
using System.Collections.ObjectModel;

using Kai.Client.CallCenter.MVVM.ViewModels;
using Kai.Common.NetDll_WpfCtrl.NetWnds;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Results;

namespace Kai.Client.CallCenter.MVVM.ViewServices;
#nullable disable
public class VsCompany_RegistPage
{
    public static ObservableCollection<VmCompany_RegistPage_Comp> oc_VmCompany_RegistPage_Comp { get; set; } = new ObservableCollection<VmCompany_RegistPage_Comp>();
    public static ObservableCollection<VmCompany_RegistPage_Cust> oc_VmCompany_RegistPage_Cust { get; set; } = new ObservableCollection<VmCompany_RegistPage_Cust>();

    public static void LoadData_Comp(Window owner, List<TbCompany> list)
    {
        oc_VmCompany_RegistPage_Comp.Clear();
        foreach (var tbCompany in list) oc_VmCompany_RegistPage_Comp.Add(new VmCompany_RegistPage_Comp(tbCompany));
    }
    public static void LoadData_Cust(Window owner, List<TbCustMain> list)
    {
        oc_VmCompany_RegistPage_Cust.Clear();
        foreach (var tbCustMain in list) oc_VmCompany_RegistPage_Cust.Add(new VmCompany_RegistPage_Cust(tbCustMain));
    }
}
#nullable enable