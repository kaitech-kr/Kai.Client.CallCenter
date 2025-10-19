using System.Diagnostics;
using System.Windows.Media;
using System.ComponentModel;
using System.Windows.Threading;
using Microsoft.AspNetCore.SignalR.Client; // Nuget: Microsoft.AspNetCore.SignalR.Client - v8.0.8

using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;
using Kai.Common.NetDll_WpfCtrl.NetUtils;
using static Kai.Common.FrmDll_WpfCtrl.FrmSystemDisplays;
using static Kai.Common.FrmDll_FormCtrl.FormFuncs;
using static Kai.Common.StdDll_Common.StdDelegate;
using static Kai.Common.StdDll_Common.StdWin32.StdCommon32;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Results;

using Kai.Client.CallCenter.Classes;
using Kai.Client.CallCenter.Networks;
using Kai.Client.CallCenter.Pages;
using Kai.Client.CallCenter.Pythons;
//using Kai.Client.CallCenter.Networks.NwInsungs;
using Kai.Client.CallCenter.MVVM.ViewServices;
using static Kai.Client.CallCenter.Class_Common.CommonVars;
using static Kai.Client.CallCenter.Pythons.Py309Common;

namespace Kai.Client.CallCenter.Class_Common;
#nullable disable
public class SrGlobalClient : IDisposable, INotifyPropertyChanged
{
    #region Dispose
    private bool disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // 관리형 리소스 해제
                //SrGlobalClient_ReLoginEvent -= OnSignalRReLogin;

                if (HubConn != null)
                {
                    HubConn.StopAsync().Wait();
                    HubConn.DisposeAsync().AsTask().Wait();
                    HubConn = null;
                }
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    #endregion

    #region PropertyChanged
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    #endregion

    #region Delegates
    public static event ExceptionEventHandler SrGlobalClient_ClosedEvent;
    public static event BoolEventHandler SrGlobalClient_LoginEvent;
    #endregion

    #region Variables
    //private static string s_sSrGlobalHubHttps = "https://localhost:5001/KaiHub"; // 자체게시시 인증문제 같음
    // http://gs012.iptime.org:17004/KaiWork/Pages/TestPostgresDB // 테스트 Site
    private static string s_sSrGlobalHubHttp = "http://gs012.iptime.org:17004/KaiHub";
    //private static string s_sSrGlobalHubHttp = "http://localhost:5000/KaiHub";
    //private string s_sSrGlobalHubAzure = "https://klogisapp.azurewebsites.net/KaiHub"; 
    public HubConnection HubConn = null;
    private const int c_nReconnectDelay = 5000; // 5초마다 재시도
    public bool IsConnected => HubConn?.State == HubConnectionState.Connected;
    private bool m_bStopReconnect = false; // 재접속 중지 플래그

    private bool _bLoginSignalR;
    public bool m_bLoginSignalR
    {
        get { return _bLoginSignalR; }
        set
        {
            _bLoginSignalR = value;
            OnPropertyChanged(nameof(m_bLoginSignalR));
        }
    }
    public string m_sLoginSignalR
    {
        get
        {
            if (_bLoginSignalR) return "연결";
            else return "해제";
        }
    }

    public List<int> m_ListIgnoreSeqno = new List<int>();
    #endregion

    #region Property
    private bool _connecting = false;
    public bool Connecting
    {
        get => _connecting;
        set => _connecting = value;
    }
    #endregion

    #region 생성자
    public SrGlobalClient()
    {
        //SrGlobalClient_ReLoginEvent += OnSignalRReLogin;
    }
    #endregion

    #region Connections
    public async Task ConnectAsync()
    {
        try
        {
            if (Connecting) return;
            Connecting = true;

            await DisconnectAsync();

            Debug.WriteLine("HubConnection 생성 중...");
            HubConn = new HubConnectionBuilder()
                .WithUrl(s_sSrGlobalHubHttp)
                .Build();

            // Basic Event
            HubConn.Closed += OnClosedAsync;

            #region Custom Event
            // Connections
            HubConn.On<string>(StdConst_FuncName.SrReport.ConnectedAsync, (connectedID) => SrReport_ConnectedAsync(connectedID));
            //HubConn.On(StdConst_FuncName.SrReport_MultiConnected, () => SrReport_MultiConnected());

            // Tel070
            //HubConn.On<TbTelMainRing, int>(StdConst_FuncName.SrReport.TelMainRingAsync, (tb, count) => SrReport_TelMainRingAsync(tb, count));

            // Order
            //HubConn.On<TbOrder, int>(StdConst_FuncName.SrReport.Order_InsertedRowAsync_Today, (tb, seq) => SrReport_Order_InsertedRowAsync_Today(tb, seq));
            //HubConn.On<TbOrder, int>(StdConst_FuncName.SrReport.Order_UpdatedRowAsync_Today, (tb, seq) => SrReport_Order_UpdatedRowAsync_Today(tb, seq));
            #endregion

            Debug.WriteLine($"연결 시도 시작...");
            // 연결될 때까지 무한 재시도
            while (!m_bStopReconnect)
            {
                // MainWindow가 닫히고 있으면 재접속 중지
                try
                {
                    bool shouldStop = false;
                    await System.Windows.Application.Current?.Dispatcher.InvokeAsync(() =>
                    {
                        if (System.Windows.Application.Current?.MainWindow != null &&
                            !System.Windows.Application.Current.MainWindow.IsLoaded)
                        {
                            shouldStop = true;
                        }
                    });

                    if (shouldStop)
                    {
                        Debug.WriteLine("MainWindow 종료 감지, 재접속 중지");
                        return;
                    }
                }
                catch (Exception)
                {
                    // Dispatcher 접근 실패 시 무시하고 계속
                }

                try
                {
                    Debug.WriteLine("HubConn.StartAsync() 호출...");
                    await HubConn.StartAsync();

                    if (HubConn.State == HubConnectionState.Connected)
                    {
                        Debug.WriteLine($"SignalR 연결 성공");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"===== SignalR 연결 실패 상세정보 =====");
                    Debug.WriteLine($"예외 타입: {ex.GetType().Name}");
                    Debug.WriteLine($"메시지: {ex.Message}");
                    Debug.WriteLine($"HResult: {ex.HResult}");
                    if (ex.InnerException != null)
                    {
                        Debug.WriteLine($"내부 예외: {ex.InnerException.Message}");
                    }
                    Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                    Debug.WriteLine($"===== {c_nReconnectDelay / 1000}초 후 재시도... =====");

                    //switch (ex.HResult)
                    //{
                    //    case -2146233088: // 서버 없음(연결거부)
                    //    case -2147467259:
                    //    case -2146233079:
                    //        // 종료시
                    //        //if (Application.Current.MainWindow != null && !Application.Current.MainWindow.IsLoaded) return;
                    //        //await Task.Delay(nMiliSec); // 머무 지체됨...
                    //        continue;
                    //    default:
                    //        //await WriteExceptionToFileAsync("", ex.Message);
                    //        return;
                    //}
                }

                await Task.Delay(c_nReconnectDelay);
            }
        }
        catch (Exception ex)
        {
            ErrMsgBox(StdUtil.GetExceptionMessage(ex), "SrGlobalClient/ConnectAsync_999");
        }
        finally
        {
            Connecting = false;
        }
    }

    public async Task DisconnectAsync()
    {
        if (HubConn is not null)
        {
            await HubConn.StopAsync();
            await HubConn.DisposeAsync();
            HubConn = null;
        }
    }

    private async Task OnClosedAsync(Exception ex)
    {
        //MsgBox("Exit"); // Window 닫는 중에 MsgBox 호출 금지
        Debug.WriteLine("SignalR OnClosedAsync called");

        if (ex == null) return; // 정상 종료시 ex는 null

        m_bLoginSignalR = false;
        SrGlobalClient_ClosedEvent?.Invoke(this, new Common.StdDll_Common.StdDelegate.ExceptionEventArgs(ex));

        Debug.WriteLine($"SignalR 연결 끊김: {ex.Message}, 자동 재접속 시작...");
        await ConnectAsync();
    }
    #endregion

    #region SrReport
    public async Task SrReport_ConnectedAsync(string connectedID)
    {
        if (m_bLoginSignalR) return;
        BoolEventArgs boolEventArgs = new BoolEventArgs(true);

        try
        {
            if (HubConn == null)
            {
                boolEventArgs.bValue = false;
                boolEventArgs.e = new Exception("HubConn is null");
                return;
            }

            // 로그인 - 콜센터 담당자 정보 얻기
            PostgResult_AllWith result = await HubConn
                .InvokeCoreAsync<PostgResult_AllWith>(StdConst_FuncName.SrResult.CallCenter.LoginAsync, new[] { s_sKaiLogId, s_sKaiLogPw });

            if (!string.IsNullOrEmpty(result.sErr))
            {
                boolEventArgs.bValue = false;
                boolEventArgs.e = new Exception(result.sErr);
                return;
            }

            s_CenterCharge = result.tbAll.centerCharge;
            if (s_CenterCharge == null)
            {
                boolEventArgs.bValue = false;
                boolEventArgs.e = new Exception("콜센터 담당자 정보없슴.");
                return;
            }
            else
            {
                s_CallCenter = result.tbAll.callCenter;
                s_CallMember = result.tbAll.callMember;
                m_bLoginSignalR = true;

                // App Mode
                if (result.nLogCount == 1) s_AppMode = KaiEnum_AppMode.Master; // 기본은 Sub
            }
        }
        catch (Exception ex)
        {

            boolEventArgs.bValue = false;
            boolEventArgs.e = ex;
        }
        finally
        {
            Debug.WriteLine($"boolEventArgs={boolEventArgs}");
            SrGlobalClient_LoginEvent?.Invoke(this, boolEventArgs);
        }
    }

    //public void OnSignalRReLogin(object sender, BoolEventArgs e)
    //{
    //    if (m_bLoginSignalR) return;

    //    if (e.bValue)
    //    {
    //        Debug.WriteLine($"SignalR 재로그인 성공: {s_CenterCharge.KeyCode}");
    //    }
    //}

    //public async Task SrReport_TelMainRingAsync(TbTelMainRing tbRing, int nRowsCount)
    //{
    //    if (tbRing == null) return;

    //    if (nRowsCount != (VsOrder_StatusPage.oc_VmOrder_StatusPage_Tel070.Count + 1))
    //    {
    //        await Application.Current.Dispatcher.InvokeAsync(async () =>
    //        {
    //            await VsOrder_StatusPage.Tel070_LoadDataAsync();
    //        });
    //    }
    //    else
    //    {
    //        await Application.Current.Dispatcher.InvokeAsync(() =>
    //        {
    //            //VsOrder_StatusPage.Tel070_AppendData(tbRing);
    //        });
    //    }
    //}

    //public async Task SrReport_Order_InsertedRowAsync_Today(TbOrder tbOrder, int nSeq)
    //{
    //    if (tbOrder == null) return;

    //    if (nSeq != (VsOrder_StatusPage.s_nLastSeq + 1) && VsOrder_StatusPage.s_nLastSeq != 0)
    //    {
    //        if (s_Order_StatusPage != null)
    //        {
    //            await Application.Current.Dispatcher.InvokeAsync(() =>
    //            {
    //                s_Order_StatusPage.BtnOrderSearch_Click(null, null);
    //            });
    //        }
    //    }
    //    else
    //    {
    //        if (VsOrder_StatusPage.s_listTbOrderToday == null) return;

    //        VsOrder_StatusPage.s_listTbOrderToday.Insert(0, tbOrder);

    //        //AutoAllocCtrl.AppendToList_NotExisted(PostgService_Common_OrderState.Created, tbOrder);

    //        if (s_Order_StatusPage == null) return;

    //        int index = GetComboBoxSelectedIndex(s_Order_StatusPage.CmbBoxDateSelect);
    //        if (index == 0) await VsOrder_StatusPage.Order_LoadDataAsync(s_Order_StatusPage, VsOrder_StatusPage.s_listTbOrderToday, Order_StatusPage.FilterBtnStatus);
    //    }

    //    VsOrder_StatusPage.s_nLastSeq = nSeq;
    //}

    //public async Task SrReport_Order_UpdatedRowAsync_Today(TbOrder tbNewOrder, int nSeq)
    //{
    //    if (tbNewOrder == null) return;

    //    TbOrder tbOldOrder = VsOrder_StatusPage.s_listTbOrderToday.FirstOrDefault(o => o.KeyCode == tbNewOrder.KeyCode);
    //    if (tbOldOrder == null)
    //    {
    //        ErrMsgBox($"오더를 찾을 수 없습니다: {tbNewOrder.KeyCode}", "SrGlobalClient/SrReport_Order_UpdatedRow_Today_01");
    //        return;
    //    }

    //    PostgService_Common_OrderState changedFlag = PostgService_TbOrder.CompareTable(tbNewOrder, tbOldOrder);
    //    if (changedFlag == PostgService_Common_OrderState.Empty) changedFlag = PostgService_Common_OrderState.Updated_AnyWay;

    //    if (nSeq != (VsOrder_StatusPage.s_nLastSeq + 1) && VsOrder_StatusPage.s_nLastSeq != 0)
    //    {
    //        if (s_Order_StatusPage != null)
    //        {
    //            await Application.Current.Dispatcher.InvokeAsync(() =>
    //            {
    //                s_Order_StatusPage.BtnOrderSearch_Click(null, null);
    //            });
    //        }
    //    }
    //    else
    //    {
    //        TbOrder tbBackup = NetUtil.DeepCopyFrom(tbOldOrder);
    //        NetUtil.DeepCopyTo(tbNewOrder, tbOldOrder);

    //        if (s_Order_StatusPage != null)
    //        {
    //            int index = GetComboBoxSelectedIndex(s_Order_StatusPage.CmbBoxDateSelect);
    //            if (index == 0) await VsOrder_StatusPage.Order_LoadDataAsync(s_Order_StatusPage, VsOrder_StatusPage.s_listTbOrderToday, Order_StatusPage.FilterBtnStatus);
    //        }

    //        int nFind = m_ListIgnoreSeqno.IndexOf(nSeq);
    //        if (nFind < 0)
    //        {
    //            //AutoAllocCtrl.EditList_UpdateOrAdd(changedFlag, tbNewOrder, tbBackup, nSeq);
    //        }
    //        else
    //        {
    //            m_ListIgnoreSeqno.RemoveAt(nFind);
    //            Debug.WriteLine($"무시리스트에서 삭제: Seqno={nSeq}, OrderNum={tbNewOrder.KeyCode}");
    //        }
    //    }

    //    VsOrder_StatusPage.s_nLastSeq = nSeq;
    //}
    #endregion

    #region SrResult - Tel070
    // Tel
    public async Task<PostgResult_TbTel070InfoList> SrResult_070TelInfo_GetListByCharge()
    {
        return await HubConn.InvokeCoreAsync<PostgResult_TbTel070InfoList>(StdConst_FuncName.SrResult.TelServer.Tel070Info_SelectRowsAsync_Charge,
                                   new[] { (object)s_CenterCharge.CenterCode });
    }
    public async Task<PostgResult_TbTel070InfoList> SrResult_Tel070Info_SelectRowsAsync_Charge_NotMainTel()
    {
        return await HubConn.InvokeCoreAsync<PostgResult_TbTel070InfoList>(StdConst_FuncName.SrResult.CallCenter.Tel070Info_SelectRowsAsync_Charge_NotMainTel,
                                   new[] { (object)s_CenterCharge.CenterCode });
    }
    //public async Task<ClsResult_Tel070Info> SrResult_070TelInfo_GetLocalsByCenterCharge()
    //{
    //    //if(HubConn == null) ErrMsgBox("HubConn is null");
    //    //else ThreadMsgBox(s_CenterCharge.KeyCode.ToString());

    //    return await HubConn.InvokeCoreAsync<ClsResult_Tel070Info>(StdFuncName.SrResult_070TelInfo_GetLocalsByCharge,
    //                    new[] { (object)s_CenterCharge.KeyCode });
    //}

    public async Task<PostgResult_TbTelMainRingList> SrResult_TelMainRing_SelectRowsAsync_CenterCode()
    {
        return await HubConn.InvokeCoreAsync<PostgResult_TbTelMainRingList>(StdConst_FuncName.SrResult.Common_TelMainRing_SelectRowsAsync_CenterCode,
                        new[] { (object)s_CenterCharge.CenterCode });
    }
    #endregion End SrResult - Tel070

    //#region SrResult - Company
    //// Insert
    //public async Task<StdResult_Long> SrResult_Company_InsertRowAsync(TbCompany tb)
    //{
    //    try
    //    {
    //        return await HubConn.InvokeCoreAsync<StdResult_Long>(StdConst_FuncName.SrResult.CallCenter.Company_InsertRowAsync, new[] { tb });
    //    }
    //    catch (Exception ex)
    //    {
    //        return new StdResult_Long(0, StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_Company_InsertRowAsync_999");
    //    }
    //}

    //// Select
    //public async Task<StdResult_Int> SrResult_Company_SelectCountAsync_CenterCode()
    //{
    //    try
    //    {
    //        return await HubConn.InvokeCoreAsync<StdResult_Int>(
    //            StdConst_FuncName.SrResult.CallCenter.Company_SelectCountAsync_CenterCode, new[] { (object)s_CenterCharge.CenterCode });
    //    }
    //    catch (Exception ex)
    //    {
    //        return new StdResult_Int(-1, StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_Company_SelectCount_999");
    //    }
    //}
    //public async Task<PostgResult_TbCompanyList> SrResult_Company_SelectRowsAsync_CenterCode()
    //{
    //    try
    //    {
    //        return await HubConn.InvokeCoreAsync<PostgResult_TbCompanyList>(
    //            StdConst_FuncName.SrResult.CallCenter.Company_SelectRowsAsync_CenterCode, new[] { (object)s_CenterCharge.CenterCode });
    //    }
    //    catch (Exception ex)
    //    {
    //        return new PostgResult_TbCompanyList(StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_Company_SelectRowsAsync_CenterCode_999");
    //    }
    //}
    //public async Task<PostgResult_TbCompanyList> SrResult_Company_SelectRowsAsync_CenterCode_CompName_TradType_Using(
    //    string sCompName, string sTradeType, bool? bUsing)
    //{
    //    try
    //    {
    //        return await HubConn.InvokeCoreAsync<PostgResult_TbCompanyList>(
    //            StdConst_FuncName.SrResult.CallCenter.Company_SelectRowsAsync_CenterCode_CompName_TradType_Using, 
    //            new[] { (object)s_CenterCharge.CenterCode, sCompName, sTradeType, (object)bUsing });
    //    }
    //    catch (Exception ex)
    //    {
    //        return new PostgResult_TbCompanyList(StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_Company_SelectRowsAsync_CenterCode_999");
    //    }
    //}

    //// Update
    //public async Task<StdResult_Int> SrResult_Company_UpdateRowAsync(TbCompany tb)
    //{
    //    try
    //    {
    //        return await HubConn.InvokeCoreAsync<StdResult_Int>(
    //            StdConst_FuncName.SrResult.CallCenter.Company_UpdateRowAsync, new[] { (object)tb });
    //    }
    //    catch (Exception ex)
    //    {
    //        return new StdResult_Int(-1, StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_Company_UpdateRowAsync_999");
    //    }
    //}

    //// Delete
    //public async Task<StdResult_Bool> SrResult_Company_DeleteRowAsync_KeyCode(long keyCode)
    //{
    //    try
    //    {
    //        return await HubConn.InvokeCoreAsync<StdResult_Bool>(
    //            StdConst_FuncName.SrResult.CallCenter.Company_DeleteRowAsync_KeyCode, new[] { (object)keyCode });
    //    }
    //    catch (Exception ex)
    //    {
    //        return new StdResult_Bool(StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_Company_DeleteRowAsync_KeyCode_999");
    //    }
    //}
    //#endregion End SrResult - Company

    //#region SrResult - TbCustMain
    //// Insert
    //public async Task<StdResult_Long> SrResult_CustMain_InsertRowAsync(TbCustMain tb)
    //{
    //    try
    //    {
    //        return await HubConn.InvokeCoreAsync<StdResult_Long>(StdConst_FuncName.SrResult.CallCenter.CustMain_InsertRowAsync, new[] { tb });
    //    }
    //    catch (Exception ex)
    //    {
    //        return new StdResult_Long(0, StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_CustMain_Insert_999");
    //    }
    //}
    //public async Task<StdResult_Long> SrResult_CustMain_InsertRowAsync_ByCopy(TbCustMain tb)
    //{
    //    try
    //    {
    //        return await HubConn.InvokeCoreAsync<StdResult_Long>(StdConst_FuncName.SrResult.CallCenter.CustMain_InsertRowAsync_ByCopy, new[] { tb });
    //    }
    //    catch (Exception ex)
    //    {
    //        return new StdResult_Long(0, StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_CustMain_InsertByCopy_999");
    //    }
    //}

    //// Select
    //public async Task<PostgResult_TbCustMain> SrResult_CustMain_SelectRowAsync_CenterCode_KeyCode(long lKeyCode)
    //{
    //    try
    //    {
    //        return await HubConn.InvokeCoreAsync<PostgResult_TbCustMain>(
    //            StdConst_FuncName.SrResult.CallCenter.CustMain_SelectRowAsync_CenterCode_KeyCode, new[] { (object)s_CenterCharge.CenterCode, (object)lKeyCode });
    //    }
    //    catch (Exception ex)
    //    {
    //        return new PostgResult_TbCustMain(null, StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_CustMain_SelectRowAsync_KeyCode_999");
    //    }
    //}
    ////public async Task<StdResult_Long> SrResult_CustMain_SelectKeyCode_BeforeInfo(string beforeBelong, long lBeforeKeyCode)
    ////{
    ////    try
    ////    {
    ////        return await HubConn.InvokeCoreAsync<StdResult_Long>(
    ////            StdConst_FuncName.SrResult.CustMain_SelectKeyCodeAsync_BeforeInfo, new[] { (object)s_CenterCharge.CenterCode, beforeBelong, (object)lBeforeKeyCode });
    ////    }
    ////    catch (Exception ex)
    ////    {
    ////        return new StdResult_Long(0, StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_CustMain_SelectRow_BeforeInfo");
    ////    }
    ////}
    //public async Task<PostgResult_TbCustMain> SrResult_CustMain_SelectRowAsync_CenterCode_BefBelong_BefKey(string beforeBelong, long lBeforeKeyCode)
    //{
    //    try
    //    {
    //        return await HubConn.InvokeCoreAsync<PostgResult_TbCustMain>(
    //            StdConst_FuncName.SrResult.CallCenter.CustMain_SelectRowAsync_CenterCode_BefBelong_BefKey, 
    //            new[] { (object)s_CenterCharge.CenterCode, beforeBelong, (object)lBeforeKeyCode });
    //    }
    //    catch (Exception ex)
    //    {
    //        return new PostgResult_TbCustMain(null, StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_CustMain_SelectRow_BeforeInfo");
    //    }
    //}
    //public async Task<StdResult_Long> SrResult_CustMain_SelectMaxBeforeCodeAsync_CenterCode_Network(string sNetwork)
    //{
    //    try
    //    {
    //        return await HubConn.InvokeCoreAsync<StdResult_Long>(
    //            StdConst_FuncName.SrResult.CallCenter.CustMain_SelectMaxBeforeCodeAsync_CenterCode_Network, new[] { (object)s_CenterCharge.CenterCode, sNetwork });
    //    }
    //    catch (Exception ex)
    //    {
    //        return new StdResult_Long(0, StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_CustMain_SelectRowAsync_MaxBeforeCode_999");
    //    }
    //}
    //public async Task<StdResult_StringList> SrResult_CustMain_SelectBefCompNameListAsync_CenterCode()
    //{
    //    try
    //    {
    //        return await HubConn.InvokeCoreAsync<StdResult_StringList>(
    //            StdConst_FuncName.SrResult.CallCenter.CustMain_SelectBefCompNameListAsync_CenterCode, new[] { (object)s_CenterCharge.CenterCode });
    //    }
    //    catch (Exception ex)
    //    {
    //        return new StdResult_StringList(StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_CustMain_SelectBeforeCompNames_999");
    //    }
    //}
    //public async Task<PostgResult_TbCustMainList> SrResult_CustMain_SelectRowsAsync_CenterCode_BefCompName(string sBefCompName)
    //{
    //    try
    //    {
    //        return await HubConn.InvokeCoreAsync<PostgResult_TbCustMainList>(
    //            StdConst_FuncName.SrResult.CallCenter.CustMain_SelectRowsAsync_CenterCode_BefCompName, new[] { (object)s_CenterCharge.CenterCode, sBefCompName });
    //    }
    //    catch (Exception ex)
    //    {
    //        return new PostgResult_TbCustMainList(StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_CustMain_SelectRows_BefCompName_999");
    //    }
    //}
    //public async Task<PostgResult_TbCustMainList> SrResult_CustMain_SelectRowsAsync_CenterCode_CompCode(long lCompCode)
    //{
    //    try
    //    {
    //        return await HubConn.InvokeCoreAsync<PostgResult_TbCustMainList>(
    //            StdConst_FuncName.SrResult.CallCenter.CustMain_SelectRowsAsync_CenterCode_CompCode, new[] { (object)s_CenterCharge.CenterCode, (object)lCompCode });
    //    }
    //    catch (Exception ex)
    //    {
    //        return new PostgResult_TbCustMainList(StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_CustMain_SelectRowsAsync_CenterCode_CompCode_999");
    //    }
    //}

    //// Update
    //public async Task<StdResult_Int> SrResult_CustMain_UpdateRowAsync(TbCustMain tb)
    //{
    //    try
    //    {
    //        return await HubConn.InvokeCoreAsync<StdResult_Int>(StdConst_FuncName.SrResult.CallCenter.CustMain_UpdateRowAsync, new[] { tb });
    //    }
    //    catch (Exception ex)
    //    {
    //        return new StdResult_Int(-1, StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_CustMain_Update_999");
    //    }
    //}

    //// Delete
    //public async Task<StdResult_Bool> SrResult_CustMain_DeleteRowAsync_CenterCode_KeyCode(long keyCode)
    //{
    //    try
    //    {
    //        return await HubConn.InvokeCoreAsync<StdResult_Bool>(
    //            StdConst_FuncName.SrResult.CallCenter.CustMain_DeleteRowAsync_CenterCode_KeyCode, new[] { (object)s_CenterCharge.CenterCode, (object)keyCode });
    //    }
    //    catch (Exception ex)
    //    {
    //        return new StdResult_Bool(StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_CustMain_DeleteRowAsync_CenterCode_KeyCodet_999");
    //    }
    //}

    //// Etc
    //public async Task<StdResult_Long> SrResult_CustMain_MoveToDeletedAsync(TbCustMain tb)
    //{
    //    try
    //    {
    //        return await HubConn.InvokeCoreAsync<StdResult_Long>(StdConst_FuncName.SrResult.CallCenter.CustMain_MoveToDeletedAsync, new[] { tb });
    //    }
    //    catch (Exception ex)
    //    {
    //        return new StdResult_Long(0, StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_CustMain_MoveToDeletedAsync_999");
    //    }
    //}

    //#endregion End SrResult - TbCustMain

    //#region SrResult - TbCustMainDeleted
    //public async Task<StdResult_Long> SrResult_CustMainDeleted_InsertRowAsync(TbCustMainDeleted tb)
    //{
    //    try
    //    {
    //        return await HubConn.InvokeCoreAsync<StdResult_Long>(StdConst_FuncName.SrResult.CallCenter.CustMainDeleted_InsertRowAsync, new[] { tb });
    //    }
    //    catch (Exception ex)
    //    {
    //        return new StdResult_Long(0, StdUtil.GetExceptionMessage(ex), "SrResult_CustMainDeleted_InsertRowAsync_999");
    //    }
    //}
    //public async Task<StdResult_Long> SrResult_CustMainDeleted_RestoreAsync(long keyCode)
    //{
    //    try
    //    {
    //        return await HubConn.InvokeCoreAsync<StdResult_Long>(StdConst_FuncName.SrResult.CallCenter.CustMainDeleted_RestoreAsync, new[] {(object) keyCode });
    //    }
    //    catch (Exception ex)
    //    {
    //        return new StdResult_Long(0, StdUtil.GetExceptionMessage(ex), "CustMainDeleted_RestoreAsync_999");
    //    }
    //}

    //#endregion

    //#region SrResult - TbCustMainWith
    //public async Task<PostgResult_AllWith> SrResult_CustMainWith_Cust_Center_Comp_SelectRowAsync_CenterCode_KeyCode(long lKeyCode)
    //{
    //    try
    //    {
    //        return await HubConn.InvokeCoreAsync<PostgResult_AllWith>(
    //            StdConst_FuncName.SrResult.CallCenter.CustMainWith_Cust_Center_Comp_SelectRowAsync_CenterCode_KeyCode, 
    //            new[] { (object)s_CenterCharge.CenterCode, (object)lKeyCode });
    //    }
    //    catch (Exception ex)
    //    {
    //        return new PostgResult_AllWith(null, StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_CustMain_SelectRowAsync_KeyCode_999");
    //    }
    //}

    //public async Task<PostgResult_AllWithList> SrResult_CustMainWith_SelectRowsAsync_CenterCode_Using(bool? bUsing)
    //{
    //    try
    //    {
    //        return await HubConn.InvokeCoreAsync<PostgResult_AllWithList>(
    //            StdConst_FuncName.SrResult.CallCenter.CustMainWith_Cust_Center_Comp_SelectRowsAsync_CenterCode_Using, 
    //            new[] { (object)s_CenterCharge.CenterCode, (object)bUsing });
    //    }
    //    catch (Exception ex)
    //    {
    //        return new PostgResult_AllWithList(
    //            StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_CustMainWithForPage_SelectRows_ByUsing_999");
    //    }
    //}
    //public async Task<PostgResult_AllWithList> SrResult_CustMainWith_SelectRowsAsync_CenterCode_CustNameWith11(
    //    bool? bUsing, string sCustName, string sDeptName, string sChargeName, string sTelNo, string sDongDetail, string sInetID, string sCompName)
    //{
    //    try
    //    {
    //        return await HubConn.InvokeCoreAsync<PostgResult_AllWithList>(
    //            StdConst_FuncName.SrResult.CallCenter.CustMainWith_Cust_Center_Comp_SelectRowsAsync_CenterCode_CustNameWith11,
    //            new[] { (object)s_CenterCharge.CenterCode, (object)bUsing, sCustName, sDeptName, sChargeName, sTelNo, sDongDetail, sInetID, sCompName });
    //    }
    //    catch (Exception ex)
    //    {
    //        return new PostgResult_AllWithList(
    //            StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_CustMainWithForPage_SelectRows_ByMultiple_999");
    //    }
    //}
    //public async Task<PostgResult_AllWithList> SrResult_CustMainWith_Cust_Center_Comp_SelectRowsAsync_BySlash(string sSlash, bool? bUsing)
    //{
    //    try
    //    {
    //        // 숫자로만 이루어진 경우 - 전화번호 검색
    //        bool isNumberOnly = sSlash.All(char.IsDigit);
    //        if (isNumberOnly)
    //        {
    //            //MsgBox("전화번호 검색");
    //            // 전화번호 검색
    //            return await HubConn.InvokeCoreAsync<PostgResult_AllWithList>(StdConst_FuncName.SrResult.CallCenter.
    //                CustMainWith_Cust_Center_Comp_SelectRowsAsync_CenterCode_LikeTelNo_Using, new[] { (object)s_CenterCharge.CenterCode, sSlash, (object)bUsing });
    //        }
    //        else // 숫자로만 이루어지지 않았으면
    //        {
    //            int idx = sSlash.IndexOf('/');
    //            if (idx >= 0) // /가 포함된 경우
    //            {
    //                string strCustName = sSlash.Substring(0, idx);
    //                string strDeptOrCharge = sSlash.Substring(idx + 1);

    //                if (!string.IsNullOrEmpty(strCustName) && !string.IsNullOrEmpty(strDeptOrCharge))
    //                {
    //                    // 상호, 담당/부서 검색
    //                    return await HubConn.InvokeCoreAsync<PostgResult_AllWithList>(StdConst_FuncName.SrResult.CallCenter.
    //                        CustMainWith_Cust_Center_Comp_SelectRowsAsync_CenterCode_LikeCustName_LikeDeptOrCharge_Using,
    //                        new[] { (object)s_CenterCharge.CenterCode, strCustName, strDeptOrCharge, (object)bUsing });
    //                }
    //                else
    //                {
    //                    if (!string.IsNullOrEmpty(strDeptOrCharge))
    //                    {
    //                        // 담당/부서 검색
    //                        return await HubConn.InvokeCoreAsync<PostgResult_AllWithList>(StdConst_FuncName.SrResult.CallCenter.
    //                            CustMainWith_Cust_Center_Comp_SelectRowsAsync_CenterCode_LikeDeptOrCharge_Using,
    //                            new[] { (object)s_CenterCharge.CenterCode, strDeptOrCharge, (object)bUsing });
    //                    }
    //                    else
    //                    {
    //                        // 상호검색
    //                        return await HubConn.InvokeCoreAsync<PostgResult_AllWithList>(StdConst_FuncName.SrResult.CallCenter.
    //                            CustMainWith_Cust_Center_Comp_SelectRowsAsync_CenterCode_LikeCustName_Using,
    //                            new[] { (object)s_CenterCharge.CenterCode, strCustName, (object)bUsing });
    //                    }
    //                }
    //            }
    //            else // /가 없는 경우
    //            {
    //                // 상호검색
    //                return await HubConn.InvokeCoreAsync<PostgResult_AllWithList>(StdConst_FuncName.SrResult.CallCenter.
    //                    CustMainWith_Cust_Center_Comp_SelectRowsAsync_CenterCode_LikeCustName_Using,
    //                    new[] { (object)s_CenterCharge.CenterCode, sSlash, (object)bUsing });
    //            }
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        return new PostgResult_AllWithList(StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_CustMainWithForWnd_SelectRows_BySlash_999");
    //    }
    //}
    //#endregion End SrResult - TbCustMainWith

    //#region SrResult - Order
    //// Insert
    //public async Task<StdResult_Long> SrResult_Order_InsertRowAsync_Today(TbOrder tb)
    //{
    //RETRY:;
    //    try
    //    {
    //        return await HubConn.InvokeCoreAsync<StdResult_Long>(StdConst_FuncName.SrResult.CallCenter.Order_InsertRowAsync_Today, new[] { tb });
    //    }
    //    catch (Exception ex)
    //    {
    //        if (m_bLoginSignslR)
    //            return new StdResult_Long(0, StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_OrderToday_InsertRow_999_1");
    //        else 
    //        {
    //            await Task.Delay(100); // 변수 변경을 위해 잠시 대기

    //            if (m_bReConnection)
    //            {
    //                NetLoadingWnd.ShowLoading(s_MainWnd, "서버와 접속해재되어\n 재접속중 입니다..."); // 로딩창 표시

    //                for (int i = 0; ; i++) // 재시도
    //                {
    //                    await Task.Delay(500);
    //                    if (m_bLoginSignslR) goto RETRY; // 로그인 성공시 다시 시도
    //                }
    //            }
    //            else
    //            {
    //                return new StdResult_Long(0, StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_OrderToday_InsertRow_999_2");
    //            }
    //        }
    //    }
    //    finally
    //    {
    //        NetLoadingWnd.HideLoading(); // 로딩창 숨김
    //    }
    //}

    //// Select
    //public async Task<PostgResult_TbOrderList> SrResult_Order_SelectRowsAsync_Today_CenterCode()
    //{
    //    try
    //    {
    //        return await HubConn.InvokeCoreAsync<PostgResult_TbOrderList>(
    //            StdConst_FuncName.SrResult.CallCenter.Order_SelectRowsAsync_Today_CenterCode, new[] { (object)s_CenterCharge.CenterCode });
    //    }
    //    catch (Exception ex)
    //    {
    //        return new PostgResult_TbOrderList(StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_OrderToday_SelectRows_All_999");
    //    }
    //}
    ////public async Task<PostgResult_TbOrder> SrResult_Order_SelectRow_TodayByExternOrder(long lExternOrderKey, string sNetwork)
    ////{
    ////    try
    ////    {
    ////        return await HubConn.InvokeCoreAsync<PostgResult_TbOrder>(StdConst_FuncName.SrResult.
    ////            Order_SelectRow_TodayByExternOrder, new[] { (object)s_CenterCharge.CenterCode, (object)lExternOrderKey, sNetwork });
    ////    }
    ////    catch (Exception ex)
    ////    {
    ////        return new PostgResult_TbOrder(StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_OrderToday_SelectRow_ByExternOrder_999");
    ////    }
    ////}
    //public async Task<PostgResult_TbOrderList> SrResult_Order_SelectRowsAsync_CenterCode_Range_OrderStatus(
    //    DateTime dtStart, DateTime dtEnd, StdEnum_OrderStatus enumStatus)
    //{
    //    try
    //    {
    //        if (enumStatus == StdEnum_OrderStatus.None) return new PostgResult_TbOrderList();

    //        return await HubConn.InvokeCoreAsync<PostgResult_TbOrderList>(
    //            StdConst_FuncName.SrResult.CallCenter.Order_SelectRowsAsync_CenterCode_Range_OrderStatus, 
    //            new[] { (object)s_CenterCharge.CenterCode, (object)dtStart, (object)dtEnd, (object)enumStatus });
    //    }
    //    catch (Exception ex)
    //    {
    //        return new PostgResult_TbOrderList(StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_Order_SelectRowsAsync_CenterCode_Range_OrderStatus_999");
    //    }
    //}

    //// Select
    //public async Task<StdResult_Bool> SrResult_Order_SelectBool_CenterCode_Today_OrderSeq(string sOrderSeq)
    //{
    //    try
    //    {
    //        return await HubConn.InvokeCoreAsync<StdResult_Bool>(StdConst_FuncName.SrResult.CallCenter.
    //            Order_SelectBoolAsync_CenterCode_Today_OrderSeq, new[] { (object)s_CenterCharge.CenterCode, sOrderSeq });
    //    }
    //    catch (Exception ex)
    //    {
    //        return new StdResult_Bool(StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_OrderToday_FindRow_ByExternOrder_999");
    //    }
    //}
    //public async Task<StdResult_Int> SrResult_Order_SelectSendingSeqOnlyAsync_CenterCode()
    //{
    //    try
    //    {
    //        return await HubConn.InvokeCoreAsync<StdResult_Int>(
    //            StdConst_FuncName.SrResult.CallCenter.Order_SelectSendingSeqOnlyAsync_CenterCode, new[] { (object)s_CenterCharge.CenterCode });
    //    }
    //    catch (Exception ex)
    //    {
    //        return new StdResult_Int(-1, StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_Order_SelectSendingSeqOnlyAsync_CenterCode");
    //    }
    //}

    //// Update 
    //public async Task<StdResult_Int> SrResult_Order_UpdateRowAsync_Today(TbOrder tb)
    //{
    //RETRY:;
    //    try
    //    {
    //        return await HubConn.InvokeCoreAsync<StdResult_Int>(
    //            StdConst_FuncName.SrResult.CallCenter.Order_UpdateRowAsync_Today, new[] { tb });
    //    }
    //    catch (Exception ex)
    //    {
    //        if (m_bLoginSignslR)
    //            return new StdResult_Int(-1, StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_OrderToday_UpdateRow_999_1");
    //        else
    //        {
    //            await Task.Delay(100); // 변수 변경을 위해 잠시 대기

    //            if (m_bReConnection)
    //            {
    //                NetLoadingWnd.ShowLoading(s_MainWnd, "서버와 접속해재되어\n 재접속중 입니다..."); // 로딩창 표시

    //                for (int i = 0; ; i++) // 재시도
    //                {
    //                    await Task.Delay(500);
    //                    if (m_bLoginSignslR) goto RETRY; // 로그인 성공시 다시 시도
    //                }
    //            }
    //            else
    //            {
    //                return new StdResult_Int(-1, StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_OrderToday_UpdateRow_999_2");
    //            }
    //        }
    //    }
    //    finally
    //    {
    //        NetLoadingWnd.HideLoading(); // 로딩창 숨김
    //    }
    //}

    //public async Task<StdResult_Int> SrResult_OnlyOrderState_UpdateRowAsync_Today(TbOrder tbOld, string orderState)
    //{
    //    TbOrder tbNew = NetUtil.DeepCopyFrom(tbOld);
    //    tbNew.OrderState = orderState;

    //    return await s_SrGClient.SrResult_Order_UpdateRowAsync_Today(tbNew);
    //}

    //public async Task SrMsgBox_OnlyOrderState_UpdateRowAsync_Today(TbOrder tbOld, string orderState)
    //{
    //    TbOrder tbNew = NetUtil.DeepCopyFrom(tbOld);
    //    tbNew.OrderState = orderState;

    //    StdResult_Int resultint = await s_SrGClient.SrResult_Order_UpdateRowAsync_Today(tbNew);
    //    //MsgBox($"{resultint.nResult}"); // Test

    //    if (resultint.nResult <= 0)
    //        ErrMsgBox($"오더상태 변경실패: {tbOld.OrderState} -> {tbNew.OrderState}", "SrGlobalClient/SrResult_OrderState_UpdateRowAsync_Today_01");
    //}

    ////public async Task SrMsgBox_OnlyExtSeqNo_UpdateRowAsync_Today(TbOrder tbOld, string appName, string seqNo)
    ////{
    ////    TbOrder tbNew = NetUtil.DeepCopyFrom(tbOld);

    ////    if (appName == StdConst_Network.INSUNG1) tbNew.Insung1 = seqNo;
    ////    else if (appName == StdConst_Network.INSUNG2) tbNew.Insung2 = seqNo;

    ////    StdResult_Int resultint = await s_SrGClient.SrResult_Order_UpdateRowAsync_Today(tbNew);

    ////    if (resultint.nResult < 0) 
    ////        ErrMsgBox($"ExtNetwork 변경실패: {tbOld.Insung1} -> {tbNew.Insung1}", "SrGlobalClient/SrMsgBox_OnlyExtSeqNo_UpdateRowAsync_Today_01");
    ////}
    //#endregion End SrResult - Order
}
#nullable enable