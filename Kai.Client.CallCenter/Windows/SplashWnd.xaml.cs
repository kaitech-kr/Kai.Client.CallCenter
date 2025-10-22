using Kai.Common.StdDll_Common;
using System.Windows;
using System.Windows.Input;
using static Kai.Client.CallCenter.Classes.CommonVars;
using static Kai.Client.CallCenter.Classes.SrGlobalClient;
using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgBox;
using static Kai.Common.StdDll_Common.StdDelegate;

namespace Kai.Client.CallCenter.Windows;
public partial class SplashWnd : Window
{
    #region Variables
    private int m_nRetryCount = 0;
    #endregion

    #region Basic
    public SplashWnd()
    {
        InitializeComponent();

        SrGlobalClient.SrGlobalClient_LoginEvent += OnSignalRLogin;
        //SrGlobalClient.SrGlobalClient_ClosedEvent += OnSignalRClosed;
    }

    private void Splash_Loaded(object sender, RoutedEventArgs e)
    {
        s_SplashWnd = this;

        // Init ListChars
        CommonFuncs.Init();

        TBoxID.Text = s_sKaiLogId;
        PwBoxPW.Password = s_sKaiLogPw;

        ChBoxID.IsChecked = true;
        TBoxID.IsEnabled = false;
        PwBoxPW.IsEnabled = false;

        // 5초 후 취소 버튼 표시
        _ = Task.Run(async () =>
        {
            await Task.Delay(5000);

            if (Application.Current?.Dispatcher != null)
            {
                await Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (BtnCancel != null)
                        BtnCancel.Visibility = Visibility.Visible;
                });
            }
        });

        // 백그라운드에서 연결 시도
        _ = Task.Run(async () =>
        {
            await s_SrGClient.ConnectAsync();
        });
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        SrGlobalClient_LoginEvent -= OnSignalRLogin;
        SrGlobalClient_ClosedEvent -= OnSignalRClosed;
    }
    #endregion

    #region Status Update
    private void UpdateStatus(string message)
    {
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            TBlockStatus.Text = message;
        });
    }
    #endregion

    #region SignalR Event
    public async void OnSignalRLogin(object sender, BoolEventArgs e)
    {
        await Application.Current.Dispatcher.BeginInvoke(() =>
        {
            SrGlobalClient_LoginEvent -= OnSignalRLogin;

            if (!e.bValue)
            {
                UpdateStatus($"로그인 실패: {StdUtil.GetExceptionMessage(e.e)}");
                MsgBox($"SignalR 로그인 실패: {StdUtil.GetExceptionMessage(e.e)}");
                this.Close();
                return;
            }

            UpdateStatus("로그인 성공! 메인 화면 여는 중...");

            MainWnd wnd = new MainWnd();
            wnd.Show();

            this.Close();
        });
    }

    public void OnSignalRClosed(object sender, ExceptionEventArgs e)
    {
        m_nRetryCount++;
        UpdateStatus($"서버 연결 끊김. 재시도 중... ({m_nRetryCount}번째)");
    }
    #endregion

    #region Normal Events
    private void TBoxID_KeyDown(object sender, KeyEventArgs e)
    {
        MsgBox("TBoxID_KeyDown");
    }
    private void ChBoxID_Click(object sender, RoutedEventArgs e)
    {
        MsgBox("ChBoxID_Click");
    }
    private void PwBoxPW_KeyDown(object sender, KeyEventArgs e)
    {
        MsgBox("PwBoxPW_KeyDown");
    }

    private async void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        // 잊지않기 위해 취소할까요(수정해야할 코드있음)
        UpdateStatus("연결 취소 중...");
        await s_SrGClient.DisconnectAsync();
        this.Close();
        Application.Current.Shutdown();
    }
    #endregion
}
