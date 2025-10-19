using Kai.Common.NetDll_WpfCtrl.NetUtils;
using Kai.Common.StdDll_Common;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Results;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Kai.Client.CallCenter.Class_Common.CommonVars;
using static Kai.Common.FrmDll_FormCtrl.FormFuncs;

namespace Kai.Client.CallCenter.Classes;
#nullable disable

#region Enums
//[Flags]
//public enum AutoAlloc_Manage
//{
//    None = 0,
//    Createed = 1,
//    Restored = 2,
//    Checking = 4,
//    Finished = 8
//}

//public enum AutoAlloc_CustSearch
//{
//    Null = 0,
//    None = 1,
//    One = 2,
//    Multi = 3,
//}

//public enum AutoAlloc_StateResult
//{
//    Error = 0,
//    Done_NoDelete = 1,
//    Done_NeedRefresh = 2,
//    Done_DoDelete = 4,

//    Done_NoDelete_NeedRefresh = Done_NoDelete | Done_NeedRefresh,
//    Done_DoDelete_NeedRefresh = Done_DoDelete | Done_NeedRefresh,
//}
#endregion

//[Serializable] // 기본생성자 만드는거 잊지말기
//public class AutoAllocResult_State : StdResult_Error
//{
//    public AutoAlloc_StateResult Result { get; set; }

//    #region 생성자
//    public AutoAllocResult_State() : base() { } // 역직렬화시 필요
//    public AutoAllocResult_State(AutoAlloc_StateResult result) : base()
//    {
//        this.Result = result;
//    }
//    public AutoAllocResult_State(AutoAlloc_StateResult result, string err, string pos, string logDirPath = "")
//       : base(err, pos, logDirPath)
//    {
//        this.Result = result;
//    }
//    #endregion
//}

//[Serializable] // 기본생성자 만드는거 잊지말기
//public class AutoAllocResult_Datagrid : StdResult_Error
//{
//    public int nIndex { get; set; } = -1;

//    public string sStatus { get; set; }


//    #region 생성자
//    public AutoAllocResult_Datagrid() : base() { } // 역직렬화시 필요
//    public AutoAllocResult_Datagrid(int index, string status) : base()
//    {
//        this.nIndex = index;
//        this.sStatus = status;
//    }
//    public AutoAllocResult_Datagrid(string err, string pos, string logDirPath = "")
//       : base(err, pos, logDirPath)
//    {
//    }
//    #endregion
//}

#region AutoAlloc
//public class AutoAlloc //: NetAutoIncrement
//{
//    //public const int c_nCustSearch_None = 1;
//    //public const int c_nCustSearch_One = 2;
//    //public const int c_nCustSearch_Multi = 3;

//    public int nKeyCode { get; set; } = -1;
//    public PostgService_Common_OrderState StateFlag { get; set; } = PostgService_Common_OrderState.Empty; // 테이블 오더 데이터 상태
//    public TbOrder TbNewOrder { get; set; } = null;
//    public TbOrder TbOldOrder { get; set; } = null;

//    public AutoAlloc() { }

//    public void Update(PostgService_Common_OrderState flag, TbOrder tbNewOrder, TbOrder tbOldOrder = null)
//    {
//        this.StateFlag = flag;
//        this.TbNewOrder = tbNewOrder;
//        this.TbOldOrder = tbOldOrder;
//    }

//    public override string ToString()
//    {
//        return $"StateFlag: {StateFlag}, TbNewOrder: {TbNewOrder.KeyCode}, TbOldOrder: {TbOldOrder.KeyCode}";
//    }
//}
#endregion

#region AutoAlloc_SearchTypeResult
//public class AutoAlloc_SearchTypeResult
//{
//    public AutoAlloc_CustSearch resultTye = AutoAlloc_CustSearch.Null;
//    public IntPtr hWndResult = IntPtr.Zero;

//    public AutoAlloc_SearchTypeResult(AutoAlloc_CustSearch resultTye, IntPtr hWndResult)
//    {
//        this.resultTye = resultTye;
//        this.hWndResult = hWndResult;
//    }
//}
#endregion

#region AutoAllocCtrl
//public class AutoAllocCtrl
//{
//    #region Variables
//    public static List<AutoAlloc> listForInsung01 = new List<AutoAlloc>();
//    public static List<AutoAlloc> listForInsung02 = new List<AutoAlloc>();
//    public static List<AutoAlloc> listForCargo24 = new List<AutoAlloc>();
//    public static List<AutoAlloc> listForOnecall = new List<AutoAlloc>();
//    #endregion

//    #region AutoAlloc Work
//    public static AutoAlloc CopyItemFromOrg(AutoAlloc org)
//    {
//        AutoAlloc item = new AutoAlloc();

//        item.nKeyCode = org.nKeyCode;
//        item.StateFlag = org.StateFlag;
//        item.TbNewOrder = org.TbNewOrder;
//        item.TbOldOrder = org.TbOldOrder;

//        return item;
//    }
//    #endregion

//    #region List Work
//    public static AutoAlloc CreateListItem(List<AutoAlloc> list, PostgService_Common_OrderState flag, TbOrder tbNewOrder, TbOrder tbOldOrder = null)
//    {
//        AutoAlloc item = new AutoAlloc();

//        item.nKeyCode = list.Count;
//        item.StateFlag = flag;
//        item.TbNewOrder = tbNewOrder;
//        item.TbOldOrder = tbOldOrder;

//        return item;
//    }
//    public static void AppendToList(List<AutoAlloc> list, PostgService_Common_OrderState flag, TbOrder tbNewOrder, TbOrder tbOldOrder = null)
//    {
//        list.Add(CreateListItem(list, flag, tbNewOrder, tbOldOrder));
//        //Debug.WriteLine($"AppendToList: {tbNewOrder.Insung1} <- {flag}");
//    }
//    public static void EditList_UpdateOrAdd(List<AutoAlloc> list, PostgService_Common_OrderState flag, TbOrder tbNewOrder, TbOrder tbOldOrder = null)
//    {
//        int index = list.FindIndex(x => x.TbNewOrder.KeyCode == tbNewOrder.KeyCode);

//        AutoAlloc c = CreateListItem(list, flag, tbNewOrder, tbOldOrder);

//        if (index < 0) list.Add(c);
//        else list[index] = c;

//        //Debug.WriteLine($"EditList: {tbNewOrder.Insung1} <- {flag}");
//    }
//    public static void AppendToList_IfNotFound(List<AutoAlloc> list, PostgService_Common_OrderState flag, AutoAlloc kaiCopy)
//    {
//        var found = list.FirstOrDefault(x => x.TbNewOrder.KeyCode == kaiCopy.TbNewOrder.KeyCode);

//        if (found == null)
//        {
//            AppendToList(list, flag, kaiCopy.TbNewOrder, kaiCopy.TbOldOrder);
//            Debug.WriteLine($"AppendToList_IfNotFound: {kaiCopy.TbNewOrder.Insung1} <- {flag}");
//        }
//    }
//    public static AutoAlloc GetOrgItem(List<AutoAlloc> list, AutoAlloc item)
//    {
//        AutoAlloc org = list.FirstOrDefault(x => x.nKeyCode == item.nKeyCode);
//        //Debug.WriteLine($"copy={item.nKeyCode}, org={org.nKeyCode}");

//        return org;
//    }

//    public static PostgService_Common_OrderState SetOrgFlagFromCopiedItem(List<AutoAlloc> list, PostgService_Common_OrderState flag, AutoAlloc item)
//    {
//        AutoAlloc org = list.FirstOrDefault(x => x.nKeyCode == item.nKeyCode);
//        if (org == null) return flag;

//        org.StateFlag = flag;

//        return org.StateFlag;
//    }
//    public static PostgService_Common_OrderState AndOrgFlagFromCopiedItem(List<AutoAlloc> list, PostgService_Common_OrderState flag, AutoAlloc item)
//    {
//        AutoAlloc org = list.FirstOrDefault(x => x.nKeyCode == item.nKeyCode);
//        if (org == null) return flag;

//        org.StateFlag &= ~flag;

//        return org.StateFlag;
//    }
//    public static PostgService_Common_OrderState OrOrgFlagFromCopiedItem(List<AutoAlloc> list, PostgService_Common_OrderState flag, AutoAlloc item)
//    {
//        AutoAlloc org = list.FirstOrDefault(x => x.nKeyCode == item.nKeyCode);
//        if (org == null) return flag;

//        org.StateFlag |= flag;

//        return org.StateFlag;
//    }
//    #endregion

//    #region AllList Work
//    public static void AppendToList_NotExisted(PostgService_Common_OrderState flag, TbOrder tb)
//    {
//        if ((flag & PostgService_Common_OrderState.Existed_NonSeqno) != 0 || (flag & PostgService_Common_OrderState.Existed_WithSeqno) != 0)
//        {
//            ErrMsgBox($"사용할수 없음: {flag}");
//            return;
//        }

//        // 화물오더 체크
//        if (!string.IsNullOrEmpty(tb.CarWeight) && tb.CarWeight != "1t" && tb.CarWeight != "1.4t")
//        {
//            // 화물24시, 원콜 리스트 대상 - 모든 화물오더

//            AppendToList(listForOnecall, flag, tb);
//        }
//        else // 1.4톤 이하는 퀵일
//        {
//            // 인성1리스트 대상 - 의뢰자가 인성2신용업체가 아닌 퀵오더 
//            if (!(tb.CallCustFrom == StdConst_Network.INSUNG2 && tb.FeeType == "신용"))
//                AppendToList(listForInsung01, flag, tb);

//            // 인성2리스트 대상 - 의뢰자가 인성1신용업체가 아닌 퀵오더 
//            if (!(tb.CallCustFrom == StdConst_Network.INSUNG1 && tb.FeeType == "신용"))
//                AppendToList(listForInsung02, flag, tb);
//        }
//    }
//    public static void AppendToList_WithExisted(TbOrder tb)
//    {
//        // 화물오더 체크
//        if (!string.IsNullOrEmpty(tb.CarWeight) && tb.CarWeight != "1t" && tb.CarWeight != "1.4t")
//        {
//            // 화물24시, 원콜 리스트 대상 - 모든 화물오더
//            if (string.IsNullOrEmpty(tb.Cargo24))
//                AppendToList(listForCargo24, PostgService_Common_OrderState.Existed_NonSeqno, tb);
//            else
//                AppendToList(listForCargo24, PostgService_Common_OrderState.Existed_WithSeqno, tb);

//            if (string.IsNullOrEmpty(tb.Onecall))
//                AppendToList(listForOnecall, PostgService_Common_OrderState.Existed_NonSeqno, tb);
//            else
//                AppendToList(listForOnecall, PostgService_Common_OrderState.Existed_WithSeqno, tb);
//        }
//        else // 1.4톤 이하는 퀵일
//        {
//            // 인성1리스트 대상 - 의뢰자가 인성2신용업체가 아닌 퀵오더 
//            if (!(tb.CallCustFrom == StdConst_Network.INSUNG2 && tb.FeeType == "신용"))
//            {
//                if (string.IsNullOrEmpty(tb.Insung1))
//                    AppendToList(listForInsung01, PostgService_Common_OrderState.Existed_NonSeqno, tb);
//                else
//                    AppendToList(listForInsung01, PostgService_Common_OrderState.Existed_WithSeqno, tb);

//                //Debug.WriteLine($"Count={listForInsung01.Count}"); // Test
//            }

//            // 인성2리스트 대상 - 의뢰자가 인성1신용업체가 아닌 퀵오더 
//            if (!(tb.CallCustFrom == StdConst_Network.INSUNG1 && tb.FeeType == "신용"))
//            {
//                if (string.IsNullOrEmpty(tb.Insung2))
//                    AppendToList(listForInsung02, PostgService_Common_OrderState.Existed_NonSeqno, tb);
//                else
//                    AppendToList(listForInsung02, PostgService_Common_OrderState.Existed_WithSeqno, tb);
//            }
//        }
//    }
//    public static void AppendToList_Update(PostgService_Common_OrderState flag, TbOrder tbNew, TbOrder tbBackup, int nSeq)
//    {
//        // 화물오더 체크
//        if (!string.IsNullOrEmpty(tbNew.CarWeight) && tbNew.CarWeight != "1t" && tbNew.CarWeight != "1.4t")
//        {
//            // 화물24시, 원콜 리스트 대상 - 모든 화물오더
//            AppendToList(listForCargo24, flag, tbNew, tbBackup);
//            AppendToList(listForOnecall, flag, tbNew, tbBackup);
//        }
//        else // 1.4톤 이하는 퀵일
//        {
//            // 인성1리스트 대상 - 의뢰자가 인성2신용업체가 아닌 퀵오더 
//            if (!(tbNew.CallCustFrom == StdConst_Network.INSUNG2 && tbNew.FeeType == "신용"))
//                AppendToList(listForInsung01, flag, tbNew, tbBackup);

//            // 인성2리스트 대상 - 의뢰자가 인성1신용업체가 아닌 퀵오더 
//            if (!(tbNew.CallCustFrom == StdConst_Network.INSUNG1 && tbNew.FeeType == "신용"))
//                AppendToList(listForInsung02, flag, tbNew, tbBackup);
//        }
//    }
//    public static void EditList_UpdateOrAdd(PostgService_Common_OrderState flag, TbOrder tbNew, TbOrder tbBackup, int nSeq)
//    {
//        // 화물오더 체크
//        if (!string.IsNullOrEmpty(tbNew.CarWeight) && tbNew.CarWeight != "1t" && tbNew.CarWeight != "1.4t")
//        {
//            // 화물24시, 원콜 리스트 대상 - 모든 화물오더
//            EditList_UpdateOrAdd(listForCargo24, flag, tbNew, tbBackup);
//            EditList_UpdateOrAdd(listForOnecall, flag, tbNew, tbBackup);
//        }
//        else // 1.4톤 이하는 퀵일
//        {
//            // 인성1리스트 대상 - 의뢰자가 인성2신용업체가 아닌 퀵오더 
//            if (!(tbNew.CallCustFrom == StdConst_Network.INSUNG2 && tbNew.FeeType == "신용"))
//                EditList_UpdateOrAdd(listForInsung01, flag, tbNew, tbBackup);

//            // 인성2리스트 대상 - 의뢰자가 인성1신용업체가 아닌 퀵오더 
//            if (!(tbNew.CallCustFrom == StdConst_Network.INSUNG1 && tbNew.FeeType == "신용"))
//                EditList_UpdateOrAdd(listForInsung02, flag, tbNew, tbBackup);
//        }
//    }
//    #endregion

//    #region Normal Method
//    public static int[] GetPageIndexArray(int pageCount, int curIndex)
//    {
//        if (curIndex >= 0 && curIndex < pageCount)
//        {
//            int[] arr = new int[pageCount];

//            int j = 0;
//            for (int i = curIndex; i < pageCount; i++, j++)
//            {
//                arr[j] = i;
//            }

//            int k = 0;
//            for (int i = j; j < pageCount - j; i++, k++)
//            {
//                arr[i] = k;
//            }

//            //int[] arr2 = new int[pageCount - 1];
//            //for(int i = 0; i < arr2.Length; i++) arr2[i] = arr[i + 1];

//            return arr;
//        }

//        return null;
//    }
//    public static int[] GetPageFirstNumArray(int totItemCount, int countPerPage)
//    {
//        if (totItemCount == 0) return null;
//        if (totItemCount <= countPerPage) return new int[1] { 1 };

//        int pageCount = totItemCount / countPerPage;
//        int remain = totItemCount % countPerPage;

//        if (remain == 0)
//        {
//            int[] arr = new int[pageCount];

//            for (int i = 0; i < pageCount; i++) arr[i] = (i * countPerPage) + 1;

//            return arr;
//        }
//        else
//        {
//            int[] arr = new int[pageCount + 1];

//            for (int i = 0; i < pageCount; i++)
//            {
//                arr[i] = (i * countPerPage) + 1;
//            }

//            arr[pageCount] = arr[pageCount - 1] + remain;

//            return arr;
//        }
//    }

//    //public static void ShowList()
//    //{
//    //    ThreadMsgBox($"Insung1: {ListForInsung01.Count}, Insung2: {ListForInsung02.Count}, 화물24시: {ListForCargo24.Count}, 원콜: {ListForOnecall.Count}");
//    //} 
//    #endregion
//} 
#endregion
#nullable enable