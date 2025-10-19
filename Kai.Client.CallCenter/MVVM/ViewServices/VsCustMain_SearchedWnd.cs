using System.Collections.ObjectModel;


using Kai.Common.NetDll_WpfCtrl.NetWnds;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models;

using Kai.Client.CallCenter.MVVM.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Kai.Client.CallCenter.MVVM.ViewServices;
public class VsCustMain_SearchedWnd
{
    public static ObservableCollection<VmCustMain_SearchedWnd> oc_VmCustMainsForWnd { get; set; } = new ObservableCollection<VmCustMain_SearchedWnd>();

    public static void LoadData(Window owner, List<TbAllWith> list, bool bWithNoUsing, TextBlock textBlock)
    {
        NetLoadingWnd.ShowLoading(owner);

        // 역순으로 데이터를 추가
        oc_VmCustMainsForWnd.Clear();
        if (bWithNoUsing)
        {
            foreach (var tbWith in list) oc_VmCustMainsForWnd.Add(new VmCustMain_SearchedWnd(tbWith));
        }
        else
        {
            foreach (var tbWith in list)
                if (tbWith.custMain.Using) oc_VmCustMainsForWnd.Add(new VmCustMain_SearchedWnd(tbWith));
        }

        textBlock.Text = $"합계: {oc_VmCustMainsForWnd.Count}"; // 검색된 고객 수 표시

        NetLoadingWnd.HideLoading();
    }
}
