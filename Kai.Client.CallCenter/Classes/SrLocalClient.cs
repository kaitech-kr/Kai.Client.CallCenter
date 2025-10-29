using System.Diagnostics;
using System.Windows;
using System.ComponentModel;
using System.Windows.Threading;
using Microsoft.AspNetCore.SignalR.Client;

using Kai.Common.StdDll_Common;
using static Kai.Common.StdDll_Common.StdDelegate;
using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgBox;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models;

using static Kai.Client.CallCenter.Classes.CommonVars;

namespace Kai.Client.CallCenter.Classes;
#nullable disable
public class SrLocalClient : IDisposable, INotifyPropertyChanged
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
    public static event ExceptionEventHandler SrLocalClient_ClosedEvent;
    public static event VoidDelegate SrLocalClient_ConnectedEvent;
    public static event StringDelegate SrLocalClient_Tel070_AnswerEvent;
    #endregion

    #region Variables
    private static string s_sSrLocalHubHttp = $"{StdConst_Var.LOCAL_SR_URL}/LocalHub";
    public HubConnection HubConn = null;
    private const int nReconnectDelay = 20000; // 20초
    private const int c_nReconnectDelay = 5000; // 5초
    private bool m_bStopReconnect = false;
    private bool IsConnected => HubConn?.State == HubConnectionState.Connected;
    #endregion

    #region Property
    private bool _connecting = false;
    public bool Connecting
    {
        get => _connecting;
        set
        {
            new Thread(() => { _connecting = value; }).Start();
        }
    }

    private bool _bConnSignslR = false;
    public bool m_bConnSignslR
    {
        get => _bConnSignslR;
        set
        {
            _bConnSignslR = value;
            OnPropertyChanged(nameof(m_bConnSignslR));
        }
    }
    public string m_sConnSignslR
    {
        get
        {
            if (m_bConnSignslR) return "연결";
            else return "해제";
        }
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
                .WithUrl(s_sSrLocalHubHttp)
                .Build();

            // Basic Event
            HubConn.Closed += OnClosedAsync;

            #region Custom Event
            // Connections
            HubConn.On<string>(StdConst_FuncName.SrReport.ConnectedAsync, (connectedID) => SrReport_ConnectedAsync(connectedID));

            // Tel070
            HubConn.On<string, string>(StdConst_FuncName.SrReport.Tel070_RingAsync, (sMyNum, sYourNum) => SrReport_Tel070_RingAsync(sMyNum, sYourNum));
            HubConn.On<string, string>(StdConst_FuncName.SrReport.Tel070_AnswerAsync, (sMyNum, sYourNum) => SrReport_Tel070_AnswerAsync(sMyNum, sYourNum));
            HubConn.On<string>(StdConst_FuncName.SrReport.Tel070_HangupAsync, (sMyNum) => SrReport_Tel070_HangupAsync(sMyNum));
            #endregion

            Debug.WriteLine($"연결 시도시작...");
            // 연결될 때까지 무한 재시도
            while (!m_bStopReconnect)
            {
                // MainWindow가 닫히고 있으면 재접속 중지
                try
                {
                    bool shouldStop = false;
                    await Application.Current?.Dispatcher.InvokeAsync(() =>
                    {
                        if (Application.Current?.MainWindow != null &&
                            !Application.Current.MainWindow.IsLoaded)
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
            ErrMsgBox(StdUtil.GetExceptionMessage(ex), "SrLocalClient/ConnectAsync_999");
        }
        finally
        {
            Connecting = false;
        }
    }

    public void StopReconnection()
    {
        m_bStopReconnect = true;
        Debug.WriteLine("SrLocalClient 재접속 중지 플래그 설정");
    }

    public async Task DisconnectAsync() // Active Disconnect
    {
        if (HubConn is not null)
        {
            await HubConn.StopAsync();
            await HubConn.DisposeAsync();
            HubConn = null;
        }
    }

    private Task OnClosedAsync(Exception ex) // When Disconnecting
    {
        if (m_bStopReconnect)
        {
            Debug.WriteLine("재접속 중지 플래그가 설정됨, 이벤트 발생하지 않음");
            return Task.CompletedTask;
        }

        m_bConnSignslR = false;
        SrLocalClient_ClosedEvent?.Invoke(this, new ExceptionEventArgs(ex));
        return Task.CompletedTask;
    }
    #endregion

    #region Report Funcs
    public async Task<StdResult_Error> SrReport_ConnectedAsync(string connectedID)
    {
        try
        {
            m_bConnSignslR = true;
            s_MainWnd.TblockConnLocal.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                s_MainWnd.TblockConnLocal.Text = m_sConnSignslR;
            }));
            //MsgBox("SrReport_Connected"); // Test

            // 로그인 이벤트 발생시킴. - MainWnd에서 받음
            SrLocalClient_ConnectedEvent?.Invoke();

            await Task.CompletedTask;
            return null;
        }
        catch (Exception ex)
        {
            return new StdResult_Error(ex.Message, "SrLocalClient/SrReport_Connected_999");
        }
    }

    public async Task SrReport_Tel070_RingAsync(string sMyNum, string sYourNum)
    {
        //ThreadMsgBox($"SrReport_070Tel_Ring: {sMyNum}, {sYourNum}"); // Test
        await Task.Delay(1);
    }

    public async Task SrReport_Tel070_AnswerAsync(string sMyNum, string sYourNum)
    {
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            SrLocalClient_Tel070_AnswerEvent?.Invoke(sYourNum);
        });
    }

    public async Task SrReport_Tel070_HangupAsync(string sMyNum)
    {
        //ThreadMsgBox($"SrReport_Tel070_HangupAsync: {sMyNum}"); // Test
        await Task.Delay(1);
    }

    public async Task SrReport_Tel070Info_SetLocalsAsync(List<TbTel070Info> listTel070)
    {
        //ThreadMsgBox($"SrReport_Tel070Info_SetLocalsAsync: {listTel070.Count}"); // Test
        await HubConn.InvokeCoreAsync<List<TbTel070Info>>(StdConst_FuncName.SrReport.Tel070Info_SetLocalsAsync, new[] { (object)listTel070 });
    }
    #endregion

    #region Result Funcs
    public async Task<StdResult_Bool> SrResult_ComBroker_CloseAsync()
    {
        try
        {
            if (HubConn != null && HubConn.State == HubConnectionState.Connected)
            {
                return await HubConn.InvokeCoreAsync<StdResult_Bool>(StdConst_FuncName.SrResult.X86ComBroker.Close, new object[0]);
            }

            return new StdResult_Bool("HubConn이 널이거나, 연결이 안된상태 입니다.", "SrResult_ComBroker_Close_01");
        }
        catch (Exception ex)
        {
            return new StdResult_Bool(StdUtil.GetExceptionMessage(ex), "SrLocalClient/SrResult_ComBroker_Close");
        }
    }
    public StdResult_Bool SrResult_ComBroker_Close()
    {
        try
        {
            if (HubConn != null && HubConn.State == HubConnectionState.Connected)
            {
                var task = HubConn.InvokeCoreAsync<StdResult_Bool>(StdConst_FuncName.SrResult.X86ComBroker.Close, new object[0]);

                if (!task.Wait(TimeSpan.FromSeconds(5))) // 5초 타임아웃
                {
                    return new StdResult_Bool("ComBroker 닫기 타임아웃", "SrResult_ComBroker_Close_Timeout");
                }

                return task.Result;
            }

            return new StdResult_Bool("HubConn이 널이거나, 연결이 안된상태 입니다.", "SrResult_ComBroker_Close_01");
        }
        catch (Exception ex)
        {
            return new StdResult_Bool(StdUtil.GetExceptionMessage(ex), "SrLocalClient/SrResult_ComBroker_Close");
        }
    }
    #endregion
}
#nullable restore
