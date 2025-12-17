using System.Windows;
using System.Diagnostics;
using System.Collections.ObjectModel;

using Kai.Common.StdDll_Common;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Results;
using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgWnd;

using Kai.Client.CallCenter.Pages;
using Kai.Client.CallCenter.MVVM.ViewModels;
using static Kai.Client.CallCenter.Classes.CommonVars;

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
    //public static void Order_ClearData()
    //{
    //    oc_VmOrdersWith.Clear();
    //}

    ///// <summary>
    ///// 주문 항목 처리 (상태별 카운트 및 금액 계산)
    ///// </summary>
    ///// <param name="order">주문 객체</param>
    ///// <param name="statusFilter">상태 필터 (null이면 전체)</param>
    ///// <param name="shouldAdd">ViewModel에 추가 여부 (out)</param>
    //private static void ProcessOrderItem(TbOrder order, StdEnum_OrderStatus? statusFilter, out bool shouldAdd)
    //{
    //    shouldAdd = true;

    //    switch (order.OrderState)
    //    {
    //        case "접수":
    //            if (statusFilter.HasValue && (statusFilter.Value & StdEnum_OrderStatus.접수) != StdEnum_OrderStatus.접수)
    //                shouldAdd = false;
    //            else
    //                s_nReceipt++;
    //            break;

    //        case "대기":
    //            if (statusFilter.HasValue && (statusFilter.Value & StdEnum_OrderStatus.대기) != StdEnum_OrderStatus.대기)
    //                shouldAdd = false;
    //            else
    //                s_nWait++;
    //            break;

    //        case "배차":
    //            if (statusFilter.HasValue && (statusFilter.Value & StdEnum_OrderStatus.배차) != StdEnum_OrderStatus.배차)
    //                shouldAdd = false;
    //            else
    //                s_nAlloc++;
    //            break;

    //        case "예약":
    //            if (statusFilter.HasValue && (statusFilter.Value & StdEnum_OrderStatus.예약) != StdEnum_OrderStatus.예약)
    //                shouldAdd = false;
    //            else
    //                s_nReserve++;
    //            break;

    //        case "운행":
    //            if (statusFilter.HasValue && (statusFilter.Value & StdEnum_OrderStatus.운행) != StdEnum_OrderStatus.운행)
    //                shouldAdd = false;
    //            else
    //            {
    //                s_nRun++;
    //                s_nTotAmount += order.FeeTotal;
    //            }
    //            break;

    //        case "완료":
    //            if (statusFilter.HasValue && (statusFilter.Value & StdEnum_OrderStatus.완료) != StdEnum_OrderStatus.완료)
    //                shouldAdd = false;
    //            else
    //            {
    //                s_nFinish++;
    //                s_nTotAmount += order.FeeTotal;
    //            }
    //            break;

    //        case "취소":
    //            if (statusFilter.HasValue && (statusFilter.Value & StdEnum_OrderStatus.취소) != StdEnum_OrderStatus.취소)
    //                shouldAdd = false;
    //            else
    //                s_nCancel++;
    //            break;
    //    }

    //    s_nTotCount++;
    //}

    ///// <summary>
    ///// 집계 라벨 업데이트
    ///// </summary>
    //private static void UpdateSummaryLabels(Order_StatusPage owner)
    //{
    //    owner.LblSumsReceipt.Content = $"{s_nReceipt}";
    //    owner.LblSumsWait.Content = $"{s_nWait}";
    //    owner.LblSumsAlloc.Content = $"{s_nAlloc}";
    //    owner.LblSumsReserve.Content = $"{s_nReserve}";
    //    owner.LblSumsRun.Content = $"{s_nRun}";
    //    owner.LblSumsFinish.Content = $"{s_nFinish}";
    //    owner.LblSumsCancel.Content = $"{s_nCancel}";
    //    owner.LblSumsTot.Content = $"{s_nTotCount}";
    //    owner.LblFeeTot.Content = $"{s_nTotAmount:#,0} 원";
    //}

    ///// <summary>
    ///// 주문 데이터 로드 (전체)
    ///// </summary>
    //public static async Task Order_LoadDataAsync(Order_StatusPage owner, List<TbOrder> list)
    //{
    //    try
    //    {
    //        if (list == null) return;

    //        await Application.Current.Dispatcher.InvokeAsync(() =>
    //        {
    //            // 초기화
    //            oc_VmOrdersWith.Clear();
    //            s_nReceipt = s_nWait = s_nAlloc = s_nReserve = s_nRun = s_nFinish = s_nCancel = s_nTotCount = s_nTotAmount = 0;

    //            // 주문 항목 처리
    //            foreach (var order in list)
    //            {
    //                ProcessOrderItem(order, null, out bool shouldAdd);
    //                if (shouldAdd)
    //                {
    //                    oc_VmOrdersWith.Add(new VmOrder_StatusPage_Order(order));
    //                }
    //            }

    //            // 집계 라벨 업데이트
    //            UpdateSummaryLabels(owner);
    //        });
    //    }
    //    finally
    //    {
    //        //NetLoadingWnd.HideLoading();
    //    }
    //}

    ///// <summary>
    ///// 주문 데이터 로드 (상태 필터 적용)
    ///// </summary>
    //public static async Task Order_LoadDataAsync(Order_StatusPage owner, List<TbOrder> list, StdEnum_OrderStatus status)
    //{
    //    try
    //    {
    //        if (list == null) return;

    //        await Application.Current.Dispatcher.InvokeAsync(() =>
    //        {
    //            // 초기화
    //            oc_VmOrdersWith.Clear();
    //            s_nReceipt = s_nWait = s_nAlloc = s_nReserve = s_nRun = s_nFinish = s_nCancel = s_nTotCount = s_nTotAmount = 0;

    //            // 주문 항목 처리 (필터 적용)
    //            foreach (var order in list)
    //            {
    //                ProcessOrderItem(order, status, out bool shouldAdd);
    //                if (shouldAdd)
    //                {
    //                    oc_VmOrdersWith.Add(new VmOrder_StatusPage_Order(order));
    //                }
    //            }

    //            // 집계 라벨 업데이트
    //            UpdateSummaryLabels(owner);
    //        });
    //    }
    //    finally
    //    {
    //        //NetLoadingWnd.HideLoading();
    //    }
    //}

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
    //public static List<TbOrder> Order_GetTodayTempAllocList()
    //{
    //    return s_listTbOrderToday.Where(u => u.OrderState == "접수" || u.OrderState == "배차" || u.OrderState == "운행").ToList();
    //}
    //public static List<TbOrder> Order_GetTodayCarOrderList()
    //{
    //    // 현재 화면에 표시된 오더들만 가져온다.
    //    List<TbOrder> list = VsOrder_StatusPage.s_listTbOrderToday.Where(u => u.CarType == "트럭").ToList();
    //    return list;
    //}
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

    // 070 전화 수신 데이터 로드
    public static async Task<StdResult_Error> Tel070_LoadDataAsync()
    {
        try
        {
            // 서버에서 전화 수신 데이터 조회
            //PostgResult_TbTelMainRingList resultList = await s_SrGClient.SrResult_TelMainRing_SelectRowsAsync_CenterCode();
            PostgResult_TbTelMainRingList resultList = new PostgResult_TbTelMainRingList();

            // 에러 체크
            if (!string.IsNullOrEmpty(resultList.sErr))
            {
                return new StdResult_Error(
                    $"Tel070 데이터 로드 실패: {resultList.sErr}",
                    "VsOrder_StatusPage/Tel070_LoadDataAsync_01");
            }

            // null 체크 및 초기화
            if (resultList.listTb == null)
            {
                curListTelMainRing = new List<TbTelMainRing>();
            }
            else
            {
                curListTelMainRing = resultList.listTb;
            }

            // ViewModel에 로드
            Tel070_LoadFromList();

            return null; // 성공
        }
        catch (Exception ex)
        {
            return new StdResult_Error(
                $"Tel070 데이터 로드 예외: {ex.Message}",
                "VsOrder_StatusPage/Tel070_LoadDataAsync_99");
        }
    }

    // 070 전화 수신 데이터 추가
    public static void Tel070_AppendData(TbTelMainRing tbRing)
    {
        try
        {
            // null 체크 및 초기화
            if (curListTelMainRing == null)
            {
                curListTelMainRing = new List<TbTelMainRing>();
            }

            // 데이터 추가 및 ViewModel 업데이트
            curListTelMainRing.Add(tbRing);
            Tel070_LoadFromList();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[VsOrder_StatusPage] Tel070_AppendData 예외: {ex.Message}");
        }
    }
    #endregion
}
#nullable enable