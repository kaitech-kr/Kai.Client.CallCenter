using System.Windows;
using System.Diagnostics;
using System.Collections.ObjectModel;

using Kai.Common.StdDll_Common;
using Kai.Common.FrmDll_FormCtrl;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Results;
using static Kai.Common.FrmDll_FormCtrl.FormFuncs;

using Kai.Client.CallCenter.Pages;
using Kai.Client.CallCenter.MVVM.ViewModels;
using static Kai.Client.CallCenter.Class_Common.CommonVars;

namespace Kai.Client.CallCenter.MVVM.ViewServices;
#nullable disable
public class VsOrder_StatusPage
{
    #region Variables - Order
    public static ObservableCollection<VmOrder_StatusPage_Order> oc_VmOrdersWith { get; set; } = new ObservableCollection<VmOrder_StatusPage_Order>();
    public static int s_nReceipt = 0; // 접수 갯수
    public static int s_nWait = 0; // 대기 갯수
    public static int s_nAlloc = 0; // 배차중 갯수
    public static int s_nReserve = 0; // 예약 갯수
    public static int s_nRun = 0; // 운행중 갯수
    public static int s_nFinish = 0; // 완료 갯수
    public static int s_nCancel = 0; // 취소 갯수
    public static int s_nTotCount = 0; // 전체 갯수
    public static int s_nTotAmount = 0; // 합계 금액

    // 오늘오더 전용변수.
    public static List<TbOrder> s_listTbOrderToday = null;
    public static List<TbOrder> s_listTbOrderMixed = null;
    public static int s_nLastSeq = 0;
    #endregion

    #region Variables - Tel070
    public static ObservableCollection<VmOrder_StatusPage_Tel070> oc_VmOrder_StatusPage_Tel070 { get; set; } = new ObservableCollection<VmOrder_StatusPage_Tel070>();
    public static List<TbTelMainRing> curListTelMainRing = null;
    #endregion

    #region Funcs - Order
    //public static void Order_SetUpdateCatch()
    //{
    //    if (s_nUpdateSeqnoForCatch != -1)
    //    {
    //        ErrMsgBox("초기화오류: s_nUpdateSeqnoForCatch != -1)", "Order_SetUpdateCatch_01");
    //    }

    //    new Thread(() =>
    //    {
    //        s_nUpdateSeqnoForCatch = VsOrder_StatusPage.s_nLastSeq + 1;
    //    }).Start();
    //}
    //public static void Order_ResetUpdateCatch()
    //{
    //    new Thread(() =>
    //    {
    //        s_nUpdateSeqnoForCatch = -1;
    //    }).Start();
    //}

    public static void Order_ClearData()
    {
        oc_VmOrdersWith.Clear();
    }
    public static async Task Order_LoadDataAsync(Order_StatusPage owner, List<TbOrder> list)
    {
        try
        {
            if (list == null) return; // Filtering 
            //NetLoadingWnd.ShowLoading(owner);

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // 정순으로 데이터를 추가
                oc_VmOrdersWith.Clear();
                s_nReceipt = s_nWait = s_nAlloc = s_nReserve = s_nRun = s_nFinish = s_nCancel = s_nTotCount = s_nTotAmount = 0;
                for (int i = 0; i < list.Count; i++)//int i = list.Count - 1, j = 1; i >= 0; i--, j++)
                {
                    oc_VmOrdersWith.Add(new VmOrder_StatusPage_Order(list[i]));
                    switch (list[i].OrderState)
                    {
                        case "접수":
                            s_nReceipt++;
                            break;
                        case "대기":
                            s_nWait++;
                            break;
                        case "배차":
                            s_nAlloc++;
                            break;
                        case "예약":
                            s_nReserve++;
                            break;
                        case "운행":
                            s_nRun++;
                            s_nTotAmount += list[i].FeeTotal;
                            break;
                        case "완료":
                            s_nFinish++;
                            s_nTotAmount += list[i].FeeTotal;
                            break;
                        case "취소":
                            s_nCancel++;
                            break;
                    }
                    s_nTotCount++;
                }

                owner.LblSumsReceipt.Content = $"{VsOrder_StatusPage.s_nReceipt}";
                owner.LblSumsWait.Content = $"{VsOrder_StatusPage.s_nWait}";
                owner.LblSumsAlloc.Content = $"{VsOrder_StatusPage.s_nAlloc}";
                owner.LblSumsReserve.Content = $"{VsOrder_StatusPage.s_nReserve}";
                owner.LblSumsRun.Content = $"{VsOrder_StatusPage.s_nRun}";
                owner.LblSumsFinish.Content = $"{VsOrder_StatusPage.s_nFinish}";
                owner.LblSumsCancel.Content = $"{VsOrder_StatusPage.s_nCancel}";
                owner.LblSumsTot.Content = $"{VsOrder_StatusPage.s_nTotCount}";
                owner.LblFeeTot.Content = $"{VsOrder_StatusPage.s_nTotAmount:#,0} 원";
            });
        }
        finally
        {
            //NetLoadingWnd.HideLoading();
        }
    }
    public static async Task Order_LoadDataAsync(Order_StatusPage owner, List<TbOrder> list, StdEnum_OrderStatus status)
    {
        try
        {
            if (list == null) return; // Filtering 

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // 역순으로 데이터를 추가
                oc_VmOrdersWith.Clear();
                s_nReceipt = s_nWait = s_nAlloc = s_nReserve = s_nRun = s_nFinish = s_nCancel = s_nTotCount = s_nTotAmount = 0;
                for (int i = 0; i < list.Count; i++)//int i = list.Count - 1, j = 1; i >= 0; i--, j++)
                {
                    switch (list[i].OrderState)
                    {
                        case "접수":
                            if ((status & StdEnum_OrderStatus.접수) == StdEnum_OrderStatus.접수)
                            {
                                oc_VmOrdersWith.Add(new VmOrder_StatusPage_Order(list[i]));
                                s_nReceipt++;
                            }
                            break;

                        case "대기":
                            if ((status & StdEnum_OrderStatus.대기) == StdEnum_OrderStatus.대기)
                            {
                                oc_VmOrdersWith.Add(new VmOrder_StatusPage_Order(list[i]));
                                s_nWait++;
                            }
                            break;

                        case "배차":
                            if ((status & StdEnum_OrderStatus.배차) == StdEnum_OrderStatus.배차)
                            {
                                oc_VmOrdersWith.Add(new VmOrder_StatusPage_Order(list[i]));
                                s_nAlloc++;
                            }
                            break;

                        case "예약":
                            if ((status & StdEnum_OrderStatus.예약) == StdEnum_OrderStatus.예약)
                            {
                                oc_VmOrdersWith.Add(new VmOrder_StatusPage_Order(list[i]));
                                s_nReserve++;
                            }
                            break;

                        case "운행":
                            if ((status & StdEnum_OrderStatus.운행) == StdEnum_OrderStatus.운행)
                            {
                                oc_VmOrdersWith.Add(new VmOrder_StatusPage_Order(list[i]));
                                s_nRun++;
                                s_nTotAmount += list[i].FeeTotal;
                            }
                            break;

                        case "완료":
                            if ((status & StdEnum_OrderStatus.완료) == StdEnum_OrderStatus.완료)
                            {
                                oc_VmOrdersWith.Add(new VmOrder_StatusPage_Order(list[i]));
                                s_nFinish++;
                                s_nTotAmount += list[i].FeeTotal;
                            }
                            break;

                        case "취소":
                            if ((status & StdEnum_OrderStatus.취소) == StdEnum_OrderStatus.취소)
                            {
                                oc_VmOrdersWith.Add(new VmOrder_StatusPage_Order(list[i]));
                                s_nCancel++;
                            }
                            break;
                    }
                    s_nTotCount++;
                }

                owner.LblSumsReceipt.Content = $"{VsOrder_StatusPage.s_nReceipt}";
                owner.LblSumsWait.Content = $"{VsOrder_StatusPage.s_nWait}";
                owner.LblSumsAlloc.Content = $"{VsOrder_StatusPage.s_nAlloc}";
                owner.LblSumsReserve.Content = $"{VsOrder_StatusPage.s_nReserve}";
                owner.LblSumsRun.Content = $"{VsOrder_StatusPage.s_nRun}";
                owner.LblSumsFinish.Content = $"{VsOrder_StatusPage.s_nFinish}";
                owner.LblSumsCancel.Content = $"{VsOrder_StatusPage.s_nCancel}";
                owner.LblSumsTot.Content = $"{VsOrder_StatusPage.s_nTotCount}";
                owner.LblFeeTot.Content = $"{VsOrder_StatusPage.s_nTotAmount:#,0} 원";
            });
        }
        finally
        {
            //NetLoadingWnd.HideLoading();
        }
    }
    //public static async Task Order_LoadTodayDataAsync(Order_StatusPage owner, StdEnum_OrderStatus status)
    //{
    //    try
    //    {
    //        if (s_listTbOrderToday == null) return; // Filtering 

    //        await Application.Current.Dispatcher.InvokeAsync(() =>
    //        {
    //            // 역순으로 데이터를 추가
    //            oc_VmOrdersWith.Clear();
    //            s_nReceipt = s_nWait = s_nAlloc = s_nReserve = s_nRun = s_nFinish = s_nCancel = s_nTotCount = s_nTotAmount = 0;
    //            for (int i = 0; i < s_listTbOrderToday.Count; i++)//int i = list.Count - 1, j = 1; i >= 0; i--, j++)
    //            {
    //                switch (s_listTbOrderToday[i].OrderState)
    //                {
    //                    case "접수":
    //                        if ((status & StdEnum_OrderStatus.접수) == StdEnum_OrderStatus.접수)
    //                        {
    //                            oc_VmOrdersWith.Add(new VmOrder_StatusPage_Order(s_listTbOrderToday[i]));
    //                            s_nReceipt++;
    //                        }
    //                        break;

    //                    case "대기":
    //                        if ((status & StdEnum_OrderStatus.대기) == StdEnum_OrderStatus.대기)
    //                        {
    //                            oc_VmOrdersWith.Add(new VmOrder_StatusPage_Order(s_listTbOrderToday[i]));
    //                            s_nWait++;
    //                        }
    //                        break;

    //                    case "배차":
    //                        if ((status & StdEnum_OrderStatus.배차) == StdEnum_OrderStatus.배차)
    //                        {
    //                            oc_VmOrdersWith.Add(new VmOrder_StatusPage_Order(s_listTbOrderToday[i]));
    //                            s_nAlloc++;
    //                        }
    //                        break;

    //                    case "예약":
    //                        if ((status & StdEnum_OrderStatus.예약) == StdEnum_OrderStatus.예약)
    //                        {
    //                            oc_VmOrdersWith.Add(new VmOrder_StatusPage_Order(s_listTbOrderToday[i]));
    //                            s_nReserve++;
    //                        }
    //                        break;

    //                    case "운행":
    //                        if ((status & StdEnum_OrderStatus.운행) == StdEnum_OrderStatus.운행)
    //                        {
    //                            oc_VmOrdersWith.Add(new VmOrder_StatusPage_Order(s_listTbOrderToday[i]));
    //                            s_nRun++;
    //                            s_nTotAmount += s_listTbOrderToday[i].FeeTotal;
    //                        }
    //                        break;

    //                    case "완료":
    //                        if ((status & StdEnum_OrderStatus.완료) == StdEnum_OrderStatus.완료)
    //                        {
    //                            oc_VmOrdersWith.Add(new VmOrder_StatusPage_Order(s_listTbOrderToday[i]));
    //                            s_nFinish++;
    //                            s_nTotAmount += s_listTbOrderToday[i].FeeTotal;
    //                        }
    //                        break;

    //                    case "취소":
    //                        if ((status & StdEnum_OrderStatus.취소) == StdEnum_OrderStatus.취소)
    //                        {
    //                            oc_VmOrdersWith.Add(new VmOrder_StatusPage_Order(s_listTbOrderToday[i]));
    //                            s_nCancel++;
    //                        }
    //                        break;
    //                }
    //                s_nTotCount++;
    //            }

    //            owner.LblSumsReceipt.Content = $"{VsOrder_StatusPage.s_nReceipt}";
    //            owner.LblSumsWait.Content = $"{VsOrder_StatusPage.s_nWait}";
    //            owner.LblSumsAlloc.Content = $"{VsOrder_StatusPage.s_nAlloc}";
    //            owner.LblSumsReserve.Content = $"{VsOrder_StatusPage.s_nReserve}";
    //            owner.LblSumsRun.Content = $"{VsOrder_StatusPage.s_nRun}";
    //            owner.LblSumsFinish.Content = $"{VsOrder_StatusPage.s_nFinish}";
    //            owner.LblSumsCancel.Content = $"{VsOrder_StatusPage.s_nCancel}";
    //            owner.LblSumsTot.Content = $"{VsOrder_StatusPage.s_nTotCount}";
    //            owner.LblFeeTot.Content = $"{VsOrder_StatusPage.s_nTotAmount:#,0} 원";
    //        });
    //    }
    //    finally
    //    {
    //        //NetLoadingWnd.HideLoading();
    //    }
    //}

    //public static TbOrder Order_GetEmptyTodayOrder(string sNetwork, long orderKey)
    //{
    //    TbOrder tb = new TbOrder();

    //    tb.MemberCode = s_CenterCharge.MemberCode;
    //    tb.CenterCode = s_CenterCharge.CenterCode;
    //    tb.UserCode = s_CenterCharge.KeyCode;
    //    tb.UserName = s_CenterCharge.Id;
    //    if (sNetwork == StdConst_Network.INSUNG1) tb.Insung1 = orderKey.ToString();
    //    else if (sNetwork == StdConst_Network.INSUNG2) tb.Insung2 = orderKey.ToString();

    //    return tb;
    //}
    public static List<TbOrder> Order_GetTodayTempAllocList()
    {
        return s_listTbOrderToday.Where(u => u.OrderState == "접수" || u.OrderState == "배차" || u.OrderState == "운행").ToList();
    }
    public static List<TbOrder> Order_GetTodayCarOrderList()
    {
        // 현재 화면에 표시된 오더들만 가져온다.
        List<TbOrder> list = VsOrder_StatusPage.s_listTbOrderToday.Where(u => u.CarType == "트럭").ToList();
        return list;
    }
    #endregion


    #region Funcs - Tel070
    private static void Tel070_LoadFromList()
    {
        try
        {
            if (curListTelMainRing == null) return;
            oc_VmOrder_StatusPage_Tel070.Clear();
            foreach (var item in curListTelMainRing.AsEnumerable().Reverse())
            {
                oc_VmOrder_StatusPage_Tel070.Add(new VmOrder_StatusPage_Tel070(item));
            }
        }
        finally
        {
            //NetLoadingWnd.CloseLoading();
        }
    }
    //public static async Task Tel070_LoadDataAsync()
    //{
    //    try
    //    {
    //        PostgResult_TbTelMainRingList resultList = await s_SrGClient.SrResult_TelMainRing_SelectRowsAsync_CenterCode();
    //        if (!string.IsNullOrEmpty(resultList.sErr))
    //        {
    //            FormFuncs.ErrMsgBox($"Error: {resultList.sErr}"); // 에러 메시지 출력
    //            return;
    //        }

    //        // TelMainRingList를 ViewModel에 설정
    //        curListTelMainRing = resultList.listTb;
    //        VsOrder_StatusPage.Tel070_LoadFromList();
    //    }
    //    finally
    //    {
    //        //NetLoadingWnd.CloseLoading();
    //    }
    //}
    public static void Tel070_AppendData(TbTelMainRing tbRing)
    {
        try
        {
            curListTelMainRing.Add(tbRing);
            VsOrder_StatusPage.Tel070_LoadFromList();
        }
        catch //(Exception ex)
        {
        }
        finally
        {
            //NetLoadingWnd.CloseLoading();
        }
    } 
    #endregion
}
#nullable enable