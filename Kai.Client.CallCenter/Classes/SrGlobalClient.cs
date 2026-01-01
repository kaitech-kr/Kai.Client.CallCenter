using System.Diagnostics;
using System.Windows;
using System.ComponentModel;
using Microsoft.AspNetCore.SignalR.Client; // Nuget: Microsoft.AspNetCore.SignalR.Client - v8.0.8

using Kai.Common.StdDll_Common;
using Kai.Common.NetDll_WpfCtrl.NetUtils;
using Kai.Common.NetDll_WpfCtrl.NetWnds;
using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgWnd;
using static Kai.Common.StdDll_Common.StdDelegate;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Results;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Services;

using Kai.Client.CallCenter.Pages;
using Kai.Client.CallCenter.MVVM.ViewServices;
using static Kai.Client.CallCenter.Classes.CommonVars;

namespace Kai.Client.CallCenter.Classes;
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
    public static event IntEventHandler SrGlobalClient_RetryEvent;
    public static event BoolEventHandler SrGlobalClient_LoginEvent;
    #endregion

    #region Variables
    //private static string s_sSrGlobalHubHttp = "http://localhost:5000/KaiHub"; // 자체게시시 
    //http://gs012.iptime.org:17004/KaiWork/Pages/TestPostgresDB // 테스트 Site
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
    public static int s_nLoginRetryCount = 0; // 로그인 재시도 횟수

    // Request ID 관리 (자동배차 업데이트 필터링용)
    private HashSet<string> m_PendingRequestIds = new HashSet<string>();
    private readonly object m_LockRequestIds = new object();
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
                .WithAutomaticReconnect() // 자동 재연결
                .Build();

            // 타임아웃 설정 (대용량 데이터 처리를 위해)
            HubConn.ServerTimeout = TimeSpan.FromMinutes(5); // 5분 (기본값: 30초)
            HubConn.HandshakeTimeout = TimeSpan.FromSeconds(30); // 30초 (기본값: 15초)
            Debug.WriteLine($"HubConnection 타임아웃 설정: ServerTimeout={HubConn.ServerTimeout}, HandshakeTimeout={HubConn.HandshakeTimeout}");

            // Basic Event
            HubConn.Closed += OnClosedAsync;

            #region Custom Event
            // Connections
            HubConn.On<string>(StdConst_FuncName.SrReport.ConnectedAsync, (connectedID) => SrReport_ConnectedAsync(connectedID));
            //HubConn.On(StdConst_FuncName.SrReport_MultiConnected, () => SrReport_MultiConnected());

            // Tel070
            HubConn.On<TbTelMainRing, int>(StdConst_FuncName.SrReport.TelMainRingAsync, (tb, count) => SrReport_TelMainRingAsync(tb, count));

            // Order
            HubConn.On<TbOrder, int>(StdConst_FuncName.SrReport.Order_InsertedRowAsync_Today, (tb, seq) => SrReport_Order_InsertedRowAsync_Today(tb, seq));
            //HubConn.On<TbOrder, int, string>(StdConst_FuncName.SrReport.Order_UpdatedRowAsync_Today, (tb, seq, requestId) => SrReport_Order_UpdatedRowAsync_Today(tb, seq, requestId));
            #endregion

            Debug.WriteLine($"연결 시도 시작...");
            // 연결될 때까지 무한 재시도
            int retryCount = 0;
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
                    retryCount++;
                    Debug.WriteLine($"===== SignalR 연결 실패 상세정보 =====");
                    Debug.WriteLine($"예외 타입: {ex.GetType().Name}");
                    Debug.WriteLine($"메시지: {ex.Message}");
                    Debug.WriteLine($"HResult: {ex.HResult}");
                    if (ex.InnerException != null)
                    {
                        Debug.WriteLine($"내부 예외: {ex.InnerException.Message}");
                    }
                    Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                    Debug.WriteLine($"===== {c_nReconnectDelay / 1000}초 후 재시도... ({retryCount}번째) =====");

                    // 재시도 이벤트 발생 (재시도 횟수 전달)
                    SrGlobalClient_RetryEvent?.Invoke(this, new Common.StdDll_Common.StdDelegate.IntEventArgs(retryCount));

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

    public void StopReconnection()
    {
        m_bStopReconnect = true;
        Debug.WriteLine("SrGlobalClient 재접속 중지 플래그 설정");
    }

    public async Task DisconnectAsync()
    {
        var conn = HubConn;
        if (conn is not null)
        {
            HubConn = null; // 먼저 null로 설정하여 중복 호출 방지
            try
            {
                await conn.StopAsync();
                await conn.DisposeAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DisconnectAsync 오류 (무시): {ex.Message}");
            }
        }
    }

    private async Task OnClosedAsync(Exception ex)
    {
        //MsgBox("Exit"); // Window 닫는 중에 MsgBox 호출 금지
        Debug.WriteLine("SignalR OnClosedAsync called");

        if (ex == null) return; // 정상 종료시 ex는 null

        if (m_bStopReconnect)
        {
            Debug.WriteLine("재접속 중지 플래그가 설정됨, 재접속하지 않음");
            return;
        }

        m_bLoginSignalR = false;
        SrGlobalClient_ClosedEvent?.Invoke(this, new Common.StdDll_Common.StdDelegate.ExceptionEventArgs(ex));

        Debug.WriteLine($"SignalR 연결 끊김: {ex.Message}, {c_nReconnectDelay / 1000}초 후 재접속 시작...");
        await Task.Delay(c_nReconnectDelay); // 5초 대기
        await ConnectAsync();
    }
    #endregion

    #region SrReport
    public async Task SrReport_ConnectedAsync(string connectedID)
    {
        if (m_bLoginSignalR) return;

        Debug.WriteLine($"***** SrReport_ConnectedAsync 시작: connectedID={connectedID} *****");

        BoolEventArgs boolEventArgs = new BoolEventArgs(true);

        try
        {
            if (HubConn == null)
            {
                Debug.WriteLine("***** 로그인 실패: HubConn is null *****");
                boolEventArgs.bValue = false;
                boolEventArgs.e = new Exception("HubConn is null");
                return;
            }

            // ⭐ 연결 상태 확인 추가
            if (HubConn.State != HubConnectionState.Connected)
            {
                Debug.WriteLine($"***** 로그인 실패: HubConn 상태 = {HubConn.State} (Connected가 아님) *****");
                boolEventArgs.bValue = false;
                boolEventArgs.e = new Exception($"SignalR 연결 상태가 올바르지 않음: {HubConn.State}");
                return;
            }

            Debug.WriteLine($"***** HubConn 연결 상태 확인 완료: {HubConn.State} *****");
            Debug.WriteLine($"***** 로그인 요청 시작: ID={s_sKaiLogId} *****");

            // 로그인 - 콜센터 담당자 정보 얻기
            PostgResult_AllWith result;
            try
            {
                result = await HubConn
                    .InvokeCoreAsync<PostgResult_AllWith>(StdConst_FuncName.SrResult.CallCenter.LoginAsync, new[] { s_sKaiLogId, s_sKaiLogPw });
                Debug.WriteLine($"***** 로그인 요청 완료 *****");
            }
            catch (TaskCanceledException)
            {
                Debug.WriteLine($"***** 로그인 실패: 서버 응답 타임아웃 (TaskCanceledException) *****");
                boolEventArgs.bValue = false;
                boolEventArgs.e = new Exception("서버 응답 타임아웃 - 재연결이 필요합니다.");
                return;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine($"***** 로그인 실패: 작업 취소됨 (OperationCanceledException) *****");
                boolEventArgs.bValue = false;
                boolEventArgs.e = new Exception("로그인 작업이 취소되었습니다 - 재연결이 필요합니다.");
                return;
            }

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
                if (result.nLogCount == 1) s_AppMode = CEnum_Kai_AppMode.Master; // 기본은 Sub
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"***** 로그인 예외 발생: {ex.GetType().Name} - {ex.Message} *****");
            Debug.WriteLine($"***** StackTrace: {ex.StackTrace} *****");

            boolEventArgs.bValue = false;
            boolEventArgs.e = ex;
        }
        finally
        {
            // 로그인 실패시 재시도 횟수 증가
            if (!boolEventArgs.bValue)
            {
                s_nLoginRetryCount++;
                Debug.WriteLine($"***** 로그인 실패! 재시도 횟수: {s_nLoginRetryCount}번째 *****");
            }
            else
            {
                s_nLoginRetryCount = 0; // 성공시 초기화
                Debug.WriteLine($"***** 로그인 성공! 재시도 횟수 초기화 *****");
            }

            Debug.WriteLine($"boolEventArgs={boolEventArgs}");
            SrGlobalClient_LoginEvent?.Invoke(this, boolEventArgs);
        }
    }

    // 070 전화 수신 시그널 처리
    public async Task SrReport_TelMainRingAsync(TbTelMainRing tbRing, int nRowsCount)
    {
        if (tbRing == null) return;

        // 행 개수가 맞지 않으면 전체 재로드
        if (nRowsCount != (VsOrder_StatusPage.oc_VmOrder_StatusPage_Tel070.Count + 1))
        {
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await VsOrder_StatusPage.Tel070_LoadDataAsync();
            });
        }
        // 행 개수가 정확하면 추가만 수행
        else
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                VsOrder_StatusPage.Tel070_AppendData(tbRing);
            });
        }
    }

    // 오늘 날짜 주문 삽입 시그널 처리
    public async Task SrReport_Order_InsertedRowAsync_Today(TbOrder tbOrder, int nSeq)
    {
        if (tbOrder == null) return;

        // 시퀀스가 맞지 않으면 전체 재조회
        if (nSeq != (VsOrder_StatusPage.s_nLastSeq + 1) && VsOrder_StatusPage.s_nLastSeq != 0)
        {
            if (s_Order_StatusPage != null)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    s_Order_StatusPage.BtnOrderSearch_Click(null, null);
                });
            }
        }
        // 시퀀스가 정상이면 리스트에 추가
        else
        {
            if (VsOrder_StatusPage.s_listTbOrderToday == null) return;

            // 리스트 맨 앞에 추가
            VsOrder_StatusPage.s_listTbOrderToday.Insert(0, tbOrder);

            // 자동배차 시스템에 새 주문 등록
            if (s_MainWnd?.m_MasterManager?.ExternalAppController != null)
            {
                s_MainWnd.m_MasterManager.ExternalAppController.AddNewOrder(tbOrder);
            }

            if (s_Order_StatusPage == null) return;

            // 오늘 날짜 선택 시에만 UI 업데이트
            int index = Order_StatusPage.GetComboBoxSelectedIndex(s_Order_StatusPage.CmbBoxDateSelect);
            if (index == 0)
            {
                await VsOrder_StatusPage.Order_LoadDataAsync(s_Order_StatusPage, VsOrder_StatusPage.s_listTbOrderToday, Order_StatusPage.FilterBtnStatus);
            }
        }

        VsOrder_StatusPage.s_nLastSeq = nSeq;
    }

    // 오늘 날짜 주문 업데이트 시그널 처리
    //public async Task SrReport_Order_UpdatedRowAsync_Today(TbOrder tbNewOrder, int nSeq, string requestId)
    //{
    //    if (tbNewOrder == null) return;

    //    // ========== 1. 자신의 업데이트 체크 (무한 루프 방지) ==========
    //    if (CheckAndRemoveRequestId(requestId))
    //    {
    //        Debug.WriteLine($"[SrGlobalClient] 로컬 업데이트 감지 - UI 갱신: KeyCode={tbNewOrder.KeyCode}, Seq={nSeq}");
    //        VsOrder_StatusPage.s_nLastSeq = nSeq;

    //        // 기존 주문 찾아서 업데이트
    //        TbOrder tbOldOrder = VsOrder_StatusPage.s_listTbOrderToday.FirstOrDefault(o => o.KeyCode == tbNewOrder.KeyCode);
    //        if (tbOldOrder != null)
    //        {
    //            NetUtil.DeepCopyTo(tbNewOrder, tbOldOrder);
    //        }

    //        // UI 갱신 ("오늘" 선택된 경우만)
    //        if (s_Order_StatusPage != null)
    //        {
    //            int index = Order_StatusPage.GetComboBoxSelectedIndex(s_Order_StatusPage.CmbBoxDateSelect);
    //            if (index == 0)
    //            {
    //                await VsOrder_StatusPage.Order_LoadDataAsync(s_Order_StatusPage, VsOrder_StatusPage.s_listTbOrderToday, Order_StatusPage.FilterBtnStatus);
    //            }
    //        }
    //        return;
    //    }

    //    // ========== 2. 기존 주문 찾기 ==========
    //    TbOrder tbOldOrder2 = VsOrder_StatusPage.s_listTbOrderToday.FirstOrDefault(o => o.KeyCode == tbNewOrder.KeyCode);
    //    if (tbOldOrder2 == null)
    //    {
    //        ErrMsgBox($"오더를 찾을 수 없습니다: {tbNewOrder.KeyCode}", "SrGlobalClient/SrReport_Order_UpdatedRow_Today_01");
    //        return;
    //    }

    //    // ========== 3. 변경 플래그 계산 ==========
    //    PostgService_Common_OrderState changedFlag = PostgService_TbOrder.CompareTable(tbNewOrder, tbOldOrder2);
    //    if (changedFlag == PostgService_Common_OrderState.Empty)
    //    {
    //        changedFlag = PostgService_Common_OrderState.Updated_AnyWay;
    //    }

    //    // ========== 4. 시퀀스 체크 ==========
    //    bool isSequenceValid = (nSeq == VsOrder_StatusPage.s_nLastSeq + 1) || (VsOrder_StatusPage.s_nLastSeq == 0);

    //    if (!isSequenceValid)
    //    {
    //        // 시퀀스 불일치 → 전체 재조회
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
    //        // ========== 5. 시퀀스 정상 → 주문 업데이트 ==========
    //        // 백업 후 업데이트
    //        TbOrder tbBackup = NetUtil.DeepCopyFrom(tbOldOrder2);
    //        NetUtil.DeepCopyTo(tbNewOrder, tbOldOrder2);

    //        // UI 갱신 ("오늘" 선택된 경우만)
    //        if (s_Order_StatusPage != null)
    //        {
    //            int index = Order_StatusPage.GetComboBoxSelectedIndex(s_Order_StatusPage.CmbBoxDateSelect);
    //            if (index == 0)
    //            {
    //                await VsOrder_StatusPage.Order_LoadDataAsync(s_Order_StatusPage, VsOrder_StatusPage.s_listTbOrderToday, Order_StatusPage.FilterBtnStatus);
    //            }
    //        }

    //        // ========== 6. 자동배차 시스템 알림 ==========
    //        // 무시 리스트 확인
    //        int nFind = m_ListIgnoreSeqno.IndexOf(nSeq);
    //        if (nFind < 0)
    //        {
    //            // 자동배차 시스템에 알림
    //            if (s_MainWnd?.m_MasterManager?.ExternalAppController != null)
    //            {
    //                s_MainWnd.m_MasterManager.ExternalAppController.UpdateOrder(changedFlag, tbNewOrder, tbBackup, nSeq);
    //            }
    //        }
    //        else
    //        {
    //            // 무시 리스트에서 제거
    //            m_ListIgnoreSeqno.RemoveAt(nFind);
    //            Debug.WriteLine($"무시리스트에서 삭제: Seqno={nSeq}, OrderNum={tbNewOrder.KeyCode}");
    //        }
    //    }

    //    // ========== 7. 시퀀스 업데이트 ==========
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

    #region SrResult - Company
    // 거래처 등록 - 새 거래처 정보를 서버에 등록
    public async Task<StdResult_Long> SrResult_Company_InsertRowAsync(TbCompany tb)
    {
        try
        {
            return await HubConn.InvokeCoreAsync<StdResult_Long>(StdConst_FuncName.SrResult.CallCenter.Company_InsertRowAsync, new[] { tb });
        }
        catch (Exception ex)
        {
            return new StdResult_Long(0, StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_Company_InsertRowAsync");
        }
    }

    // 거래처 개수 조회 - 센터별 거래처 총 개수 조회
    public async Task<StdResult_Int> SrResult_Company_SelectCountAsync_CenterCode()
    {
        try
        {
            return await HubConn.InvokeCoreAsync<StdResult_Int>(
                StdConst_FuncName.SrResult.CallCenter.Company_SelectCountAsync_CenterCode, new[] { (object)s_CenterCharge.CenterCode });
        }
        catch (Exception ex)
        {
            return new StdResult_Int(-1, StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_Company_SelectCountAsync_CenterCode");
        }
    }

    // 거래처 목록 조회 - 센터별 전체 거래처 목록 조회
    public async Task<PostgResult_TbCompanyList> SrResult_Company_SelectRowsAsync_CenterCode()
    {
        try
        {
            return await HubConn.InvokeCoreAsync<PostgResult_TbCompanyList>(
                StdConst_FuncName.SrResult.CallCenter.Company_SelectRowsAsync_CenterCode, new[] { (object)s_CenterCharge.CenterCode });
        }
        catch (Exception ex)
        {
            return new PostgResult_TbCompanyList(StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_Company_SelectRowsAsync_CenterCode");
        }
    }

    // 거래처 목록 조회 - 다중 조건 검색 (거래처명, 거래 유형, 사용 여부)
    public async Task<PostgResult_TbCompanyList> SrResult_Company_SelectRowsAsync_CenterCode_CompName_TradType_Using(
        string sCompName, string sTradeType, bool? bUsing)
    {
        try
        {
            return await HubConn.InvokeCoreAsync<PostgResult_TbCompanyList>(
                StdConst_FuncName.SrResult.CallCenter.Company_SelectRowsAsync_CenterCode_CompName_TradType_Using,
                new[] { (object)s_CenterCharge.CenterCode, sCompName, sTradeType, (object)bUsing });
        }
        catch (Exception ex)
        {
            return new PostgResult_TbCompanyList(StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_Company_SelectRowsAsync_CenterCode_CompName_TradType_Using");
        }
    }

    // 거래처 수정 - 기존 거래처 정보 업데이트
    public async Task<StdResult_Int> SrResult_Company_UpdateRowAsync(TbCompany tb)
    {
        try
        {
            return await HubConn.InvokeCoreAsync<StdResult_Int>(
                StdConst_FuncName.SrResult.CallCenter.Company_UpdateRowAsync, new[] { (object)tb });
        }
        catch (Exception ex)
        {
            return new StdResult_Int(-1, StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_Company_UpdateRowAsync");
        }
    }

    // 거래처 삭제 - KeyCode로 거래처 삭제
    public async Task<StdResult_Bool> SrResult_Company_DeleteRowAsync_KeyCode(long keyCode)
    {
        try
        {
            return await HubConn.InvokeCoreAsync<StdResult_Bool>(
                StdConst_FuncName.SrResult.CallCenter.Company_DeleteRowAsync_KeyCode, new[] { (object)keyCode });
        }
        catch (Exception ex)
        {
            return new StdResult_Bool(StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_Company_DeleteRowAsync_KeyCode");
        }
    }
    #endregion End SrResult - Company

    #region SrResult - TbCustMain
    // Insert
    // 고객 정보 신규 저장
    public async Task<StdResult_Long> SrResult_CustMain_InsertRowAsync(TbCustMain tb)
    {
        try
        {
            return await HubConn.InvokeCoreAsync<StdResult_Long>(StdConst_FuncName.SrResult.CallCenter.CustMain_InsertRowAsync, new[] { tb });
        }
        catch (Exception ex)
        {
            return new StdResult_Long(0, StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_CustMain_InsertRowAsync");
        }
    }
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
    // 고객 목록 조회 - 특정 거래처에 속한 고객 목록 조회
    public async Task<PostgResult_TbCustMainList> SrResult_CustMain_SelectRowsAsync_CenterCode_CompCode(long lCompCode)
    {
        try
        {
            return await HubConn.InvokeCoreAsync<PostgResult_TbCustMainList>(
                StdConst_FuncName.SrResult.CallCenter.CustMain_SelectRowsAsync_CenterCode_CompCode,
                new[] { (object)s_CenterCharge.CenterCode, (object)lCompCode });
        }
        catch (Exception ex)
        {
            return new PostgResult_TbCustMainList(StdUtil.GetExceptionMessage(ex),
                "SrGlobalClient/SrResult_CustMain_SelectRowsAsync_CenterCode_CompCode");
        }
    }

    // Update
    // 고객 정보 수정 저장
    public async Task<StdResult_Int> SrResult_CustMain_UpdateRowAsync(TbCustMain tb)
    {
        try
        {
            return await HubConn.InvokeCoreAsync<StdResult_Int>(StdConst_FuncName.SrResult.CallCenter.CustMain_UpdateRowAsync, new[] { tb });
        }
        catch (Exception ex)
        {
            return new StdResult_Int(-1, StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_CustMain_UpdateRowAsync");
        }
    }

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

    // 고객 정보를 삭제 테이블로 이동
    public async Task<StdResult_Long> SrResult_CustMain_MoveToDeletedAsync(TbCustMain tb)
    {
        try
        {
            return await HubConn.InvokeCoreAsync<StdResult_Long>(StdConst_FuncName.SrResult.CallCenter.CustMain_MoveToDeletedAsync, new[] { tb });
        }
        catch (Exception ex)
        {
            return new StdResult_Long(0, StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_CustMain_MoveToDeletedAsync");
        }
    }

    #endregion End SrResult - TbCustMain

    #region SrResult - TbCustMainDeleted
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

    #endregion

    #region SrResult - TbCustMainWith
    // 고객 정보 단건 조회 (고객, 센터, 회사 정보 포함)
    public async Task<PostgResult_AllWith> SrResult_CustMainWith_Cust_Center_Comp_SelectRowAsync_CenterCode_KeyCode(long lKeyCode)
    {
        try
        {
            return await HubConn.InvokeCoreAsync<PostgResult_AllWith>(StdConst_FuncName.SrResult.CallCenter
                .CustMainWith_SelectRowAsync_CenterCode_KeyCode, new[] { (object)s_CenterCharge.CenterCode, (object)lKeyCode });
        }
        catch (Exception ex)
        {
            return new PostgResult_AllWith(
                null, StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_CustMainWith_Cust_Center_Comp_SelectRowAsync_CenterCode_KeyCode");
        }
    }

    // 고객 정보 목록 조회 - 사용 여부별 전체 조회 (고객, 센터, 회사 정보 포함)
    public async Task<PostgResult_AllWithList> SrResult_CustMainWith_SelectRowsAsync_CenterCode_Using(bool? bUsing)
    {
        try
        {
            return await HubConn.InvokeCoreAsync<PostgResult_AllWithList>(StdConst_FuncName.SrResult.CallCenter.
                CustMainWith_SelectRowsAsync_CenterCode_Using, new[] { (object)s_CenterCharge.CenterCode, (object)bUsing });
        }
        catch (Exception ex)
        {
            return new PostgResult_AllWithList(
                StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_CustMainWith_SelectRowsAsync_CenterCode_Using");
        }
    }

    // 고객 정보 목록 조회 - 다중 조건 검색 (고객, 센터, 회사 정보 포함)
    public async Task<PostgResult_AllWithList> SrResult_CustMainWith_SelectRowsAsync_CenterCode_CustNameWith11(
        bool? bUsing, string sCustName, string sDeptName, string sChargeName, string sTelNo, string sDongDetail, string sInetID, string sCompName)
    {
        try
        {
            return await HubConn.InvokeCoreAsync<PostgResult_AllWithList>(StdConst_FuncName.SrResult.CallCenter.CustMainWith_SelectRowsAsync_CenterCode_CustNameWith11, 
                new[] { (object)s_CenterCharge.CenterCode, (object)bUsing, sCustName, sDeptName, sChargeName, sTelNo, sDongDetail, sInetID, sCompName });
        }
        catch (Exception ex)
        {
            return new PostgResult_AllWithList(
                StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_CustMainWith_SelectRowsAsync_CenterCode_CustNameWith11");
        }
    }
    // 고객 검색 (전화번호 / 상호 / 상호+담당 통합 검색)
    public async Task<PostgResult_AllWithList> SrResult_CustMainWith_SelectRowsAsync_BySlash(string sSlash, bool? bUsing)
    {
        try
        {
            // 1. 전화번호 검색 (숫자만 입력된 경우)
            if (sSlash.All(char.IsDigit))
            {
                return await HubConn.InvokeCoreAsync<PostgResult_AllWithList>(
                    "SrResult_CustMainWith_SelectRowsAsync_CenterCode_LikeTelNo_Using", new object[] { StdEnum_WithTable.CenterComp, s_CenterCharge.CenterCode, sSlash, bUsing });
            }

            // 2. 슬래시(/) 포함 여부 확인
            int slashIndex = sSlash.IndexOf('/');
            if (slashIndex < 0)
            {
                // 2-1. 슬래시 없음 → 상호만 검색
                return await HubConn.InvokeCoreAsync<PostgResult_AllWithList>(
                    "SrResult_CustMainWith_SelectRowsAsync_CenterCode_LikeCustName_Using", new object[] { StdEnum_WithTable.CenterComp, s_CenterCharge.CenterCode, sSlash, bUsing });
            }

            // 3. 슬래시 있음 → 상호/담당 분리
            string custName = sSlash.Substring(0, slashIndex);
            string deptOrCharge = sSlash.Substring(slashIndex + 1);

            // 3-1. 상호 + 담당/부서 모두 입력된 경우
            if (!string.IsNullOrEmpty(custName) && !string.IsNullOrEmpty(deptOrCharge))
            {
                return await HubConn.InvokeCoreAsync<PostgResult_AllWithList>("SrResult_CustMainWith_SelectRowsAsync_CenterCode_LikeCustName_LikeDeptOrCharge_Using", 
                    new object[] { StdEnum_WithTable.CenterComp, s_CenterCharge.CenterCode, custName, deptOrCharge, bUsing });
            }

            // 3-2. 담당/부서만 입력된 경우 (예: "/홍길동")
            if (!string.IsNullOrEmpty(deptOrCharge))
            {
                return await HubConn.InvokeCoreAsync<PostgResult_AllWithList>("SrResult_CustMainWith_SelectRowsAsync_CenterCode_LikeDeptOrCharge_Using", 
                    new object[] { StdEnum_WithTable.CenterComp, s_CenterCharge.CenterCode, deptOrCharge, bUsing });
            }

            // 있을수 없는 경우
            return new PostgResult_AllWithList("있을수 없는 경우", "KaiCallCenterHub/SrResult_CustMainWith_SelectRowsAsync_BySlash_01");
        }
        catch (Exception ex)
        {
            return new PostgResult_AllWithList(
                StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_CustMainWith_SelectRowsAsync_BySlash_999");
        }
    }

    //public async Task Test()
    //{
    //    string s = await HubConn.InvokeCoreAsync<string>("Test", new object[] { StdEnum_WithTable.CenterComp } );
    //    MsgBox($"s={s}");
    //}
    #endregion End SrResult - TbCustMainWith

    #region SrResult - Order
    //Insert
    public async Task<StdResult_Long> SrResult_Order_InsertRowAsync_Today(TbOrder tb)
    {
        const int MAX_RETRY_COUNT = 60; // 최대 60회 (30초)
        bool showedLoading = false;

        try
        {
            while (true)
            {
                try
                {
                    // 주문 Insert 시도
                    return await HubConn.InvokeCoreAsync<StdResult_Long>("SrResult_Order_InsertRowAsync_Today", new[] { tb });
                }
                catch (Exception ex)
                {
                    // 1. 로그인 상태이면 즉시 에러 반환 (서버 에러)
                    if (m_bLoginSignalR)
                    {
                        return new StdResult_Long(0,
                            StdUtil.GetExceptionMessage(ex),
                            "SrGlobalClient/SrResult_Order_InsertRowAsync_Today_Logged");
                    }

                    // 2. 재접속 중지 상태면 에러 반환
                    if (m_bStopReconnect)
                    {
                        return new StdResult_Long(0,
                            StdUtil.GetExceptionMessage(ex),
                            "SrGlobalClient/SrResult_Order_InsertRowAsync_Today_StopReconn");
                    }

                    // 3. 재접속 대기 (로딩창 표시)
                    if (!showedLoading)
                    {
                        NetLoadingWnd.ShowLoading(s_MainWnd, "서버와 접속 해제되어\n재접속 중입니다...");
                        showedLoading = true;
                    }

                    await Task.Delay(100); // 변수 변경 대기

                    // 4. 재접속 대기 루프 (최대 30초)
                    for (int i = 0; i < MAX_RETRY_COUNT; i++)
                    {
                        await Task.Delay(500);

                        if (m_bLoginSignalR)
                        {
                            // 재접속 성공 → 다시 시도
                            break;
                        }

                        if (i >= MAX_RETRY_COUNT - 1)
                        {
                            // 재접속 실패 (타임아웃)
                            return new StdResult_Long(0, "재접속 실패 (타임아웃)", "SrGlobalClient/SrResult_Order_InsertRowAsync_Today_Timeout");
                        }
                    }
                }
            }
        }
        finally
        {
            if (showedLoading)
            {
                NetLoadingWnd.HideLoading();
            }
        }
    }

    //// Select
    public async Task<PostgResult_TbOrderList> SrResult_Order_SelectRowsAsync_Today_CenterCode()
    {
        try
        {
            return await HubConn.InvokeCoreAsync<PostgResult_TbOrderList>(
                StdConst_FuncName.SrResult.CallCenter.Order_SelectRowsAsync_Today_CenterCode, new[] { (object)s_CenterCharge.CenterCode });
        }
        catch (Exception ex)
        {
            return new PostgResult_TbOrderList(StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_OrderToday_SelectRows_All_999");
        }
    }
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
    public async Task<PostgResult_TbOrderList> SrResult_Order_SelectRowsAsync_CenterCode_Range_OrderStatus(
        DateTime dtStart, DateTime dtEnd, StdEnum_OrderStatus enumStatus)
    {
        try
        {
            if (enumStatus == StdEnum_OrderStatus.None) return new PostgResult_TbOrderList();

            return await HubConn.InvokeCoreAsync<PostgResult_TbOrderList>(
                StdConst_FuncName.SrResult.CallCenter.Order_SelectRowsAsync_CenterCode_Range_OrderStatus,
                new[] { (object)s_CenterCharge.CenterCode, (object)dtStart, (object)dtEnd, (object)enumStatus });
        }
        catch (Exception ex)
        {
            return new PostgResult_TbOrderList(StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_Order_SelectRowsAsync_CenterCode_Range_OrderStatus_999");
        }
    }

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
    // 서버에서 현재 SendingSeq 조회 (센터 코드 기준)
    /// <returns>성공: SendingSeq, 실패: -1</returns>
    public async Task<StdResult_Int> SrResult_Order_SelectSendingSeqOnlyAsync_CenterCode()
    {
        try
        {
            return await HubConn.InvokeCoreAsync<StdResult_Int>(
                StdConst_FuncName.SrResult.CallCenter.Order_SelectSendingSeqOnlyAsync_CenterCode, new[] { (object)s_CenterCharge.CenterCode });
        }
        catch (Exception ex)
        {
            return new StdResult_Int(-1, StdUtil.GetExceptionMessage(ex), "SrGlobalClient/Order_SelectSendingSeqOnly");
        }
    }

    //// Update 
    // 오늘 날짜 주문 업데이트
    public async Task<StdResult_Int> SrResult_Order_UpdateRowAsync_Today(TbOrder tb)
    {
        const int MAX_RETRY_COUNT = 60;
        bool showedLoading = false;

        try
        {
            while (true)
            {
                try
                {
                    return await HubConn.InvokeCoreAsync<StdResult_Int>(
                        StdConst_FuncName.SrResult.CallCenter.Order_UpdateRowAsync_Today,
                        new object[] { tb, null });
                }
                catch (Exception ex)
                {
                    if (m_bLoginSignalR)
                    {
                        return new StdResult_Int(-1, StdUtil.GetExceptionMessage(ex),
                            "SrGlobalClient/SrResult_Order_UpdateRowAsync_Today_Logged");
                    }

                    if (m_bStopReconnect)
                    {
                        return new StdResult_Int(-1, StdUtil.GetExceptionMessage(ex),
                            "SrGlobalClient/SrResult_Order_UpdateRowAsync_Today_StopReconn");
                    }

                    if (!showedLoading)
                    {
                        NetLoadingWnd.ShowLoading(s_MainWnd, "서버와 접속 해제되어\n재접속 중입니다...");
                        showedLoading = true;
                    }

                    await Task.Delay(100);

                    for (int i = 0; i < MAX_RETRY_COUNT; i++)
                    {
                        await Task.Delay(500);
                        if (m_bLoginSignalR) break;

                        if (i >= MAX_RETRY_COUNT - 1)
                        {
                            return new StdResult_Int(-1, "재접속 실패 (타임아웃)",
                                "SrGlobalClient/SrResult_Order_UpdateRowAsync_Today_Timeout");
                        }
                    }
                }
            }
        }
        finally
        {
            if (showedLoading) NetLoadingWnd.HideLoading();
        }
    }

    // Request ID 등록 (자동배차 업데이트 필터링용)
    public void RegisterRequestId(string requestId)
    {
        lock (m_LockRequestIds)
        {
            m_PendingRequestIds.Add(requestId);
            Debug.WriteLine($"[SrGlobalClient] RequestId 등록: {requestId}");
        }
    }

    // Request ID 확인 및 제거
    private bool CheckAndRemoveRequestId(string requestId)
    {
        if (string.IsNullOrEmpty(requestId)) return false;
        lock (m_LockRequestIds)
        {
            if (m_PendingRequestIds.Contains(requestId))
            {
                m_PendingRequestIds.Remove(requestId);
                Debug.WriteLine($"[SrGlobalClient] 자신의 RequestId 감지 - 무시: {requestId}");
                return true;
            }
        }
        return false;
    }

    // 오늘 날짜 주문 업데이트 (Request ID 사용 - 자동배차 전용)
    public async Task<StdResult_Int> SrResult_Order_UpdateRowAsync_Today_WithRequestId(TbOrder tb)
    {
        string requestId = Guid.NewGuid().ToString();
        RegisterRequestId(requestId);

        const int MAX_RETRY_COUNT = 60;
        bool showedLoading = false;

        try
        {
            while (true)
            {
                try
                {
                    return await HubConn.InvokeCoreAsync<StdResult_Int>(StdConst_FuncName.SrResult.CallCenter.Order_UpdateRowAsync_Today, new object[] { tb, requestId });
                }
                catch (Exception ex)
                {
                    if (m_bLoginSignalR)
                    {
                        return new StdResult_Int(-1, StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_Order_UpdateRowAsync_Today_WithRequestId_Logged");
                    }

                    if (m_bStopReconnect)
                    {
                        return new StdResult_Int(-1, StdUtil.GetExceptionMessage(ex), "SrGlobalClient/SrResult_Order_UpdateRowAsync_Today_WithRequestId_StopReconn");
                    }

                    if (!showedLoading)
                    {
                        NetLoadingWnd.ShowLoading(s_MainWnd, "서버와 접속 해제되어\n재접속 중입니다...");
                        showedLoading = true;
                    }

                    await Task.Delay(100);

                    for (int i = 0; i < MAX_RETRY_COUNT; i++)
                    {
                        await Task.Delay(500);
                        if (m_bLoginSignalR) break;

                        if (i >= MAX_RETRY_COUNT - 1)
                        {
                            return new StdResult_Int(-1, "재접속 실패 (타임아웃)", "SrGlobalClient/SrResult_Order_UpdateRowAsync_Today_WithRequestId_Timeout");
                        }
                    }
                }
            }
        }
        finally
        {
            if (showedLoading) NetLoadingWnd.HideLoading();
        }
    }

    // 주문 상태만 변경하여 업데이트 (결과 반환)
    public async Task<StdResult_Int> SrResult_OnlyOrderState_UpdateRowAsync_Today(TbOrder tbOld, string orderState)
    {
        TbOrder tbNew = NetUtil.DeepCopyFrom(tbOld);
        tbNew.OrderState = orderState;
        tbNew.Updater = s_CenterCharge.Id;
        tbNew.UpdateDate = DateTime.Now.ToString(StdConst_Var.DTFORMAT_EXCEPT_SEC);

        return await SrResult_Order_UpdateRowAsync_Today(tbNew);
    }

    // 주문 상태만 변경하여 업데이트 (에러 시 메시지 박스 표시)
    public async Task SrMsgBox_OnlyOrderState_UpdateRowAsync_Today(TbOrder tbOld, string orderState)
    {
        StdResult_Int result = await SrResult_OnlyOrderState_UpdateRowAsync_Today(tbOld, orderState);

        if (result.nResult <= 0)
        {
            ErrMsgBox($"주문 상태 변경 실패: {tbOld.OrderState} → {orderState}\n{result.sErrNPos}",
                "SrGlobalClient/SrMsgBox_OnlyOrderState_UpdateRowAsync_Today");
        }
    }

    ////public async Task SrMsgBox_OnlyExtSeqNo_UpdateRowAsync_Today(TbOrder tbOld, string appName, string seqNo)
    ////{
    ////    TbOrder tbNew = NetUtil.DeepCopyFrom(tbOld);

    ////    if (appName == StdConst_Network.INSUNG1) tbNew.Insung1 = seqNo;
    ////    else if (appName == StdConst_Network.INSUNG2) tbNew.Insung2 = seqNo;

    ////    StdResult_Int resultint = await s_SrGClient.SrResult_Order_UpdateRowAsync_Today(tbNew);

    ////    if (resultint.nResult < 0) 
    ////        ErrMsgBox($"ExtNetwork 변경실패: {tbOld.Insung1} -> {tbNew.Insung1}", "SrGlobalClient/SrMsgBox_OnlyExtSeqNo_UpdateRowAsync_Today_01");
    ////}
    #endregion End SrResult - Order
}
#nullable enable