using Kai.Common.StdDll_Common;
using System.Windows;
using System.Windows.Input;
using Kai.Client.CallCenter.Classes;
using static Kai.Client.CallCenter.Classes.CommonVars;
using static Kai.Client.CallCenter.Classes.SrGlobalClient;
using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgBox;
using static Kai.Common.StdDll_Common.StdDelegate;

namespace Kai.Client.CallCenter.Windows;
public partial class SplashWnd : Window
{
    #region Variables
    #endregion

    #region Basic
    public SplashWnd()
    {
        InitializeComponent();

        SrGlobalClient.SrGlobalClient_LoginEvent += OnSignalRLogin;
        SrGlobalClient.SrGlobalClient_RetryEvent += OnSignalRClosed;
    }

    private void Splash_Loaded(object sender, RoutedEventArgs e)
    {
        s_SplashWnd = this;

        //Init ListChars
         //CommonFuncs.Init();

        TBoxID.Text = s_sKaiLogId;
        PwBoxPW.Password = s_sKaiLogPw;

        ChBoxID.IsChecked = true;
        TBoxID.IsEnabled = false;
        PwBoxPW.IsEnabled = false;

        //5초 후 취소 버튼 표시
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

        //백그라운드에서 연결 시도
        _ = Task.Run(async () =>
        {
            //await s_SrGClient.ConnectAsync();
        });
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // SrGlobalClient_LoginEvent -= OnSignalRLogin;
         SrGlobalClient_RetryEvent -= OnSignalRClosed;
    }
    #endregion

    #region Status Update
    public void UpdateStatus(string message)
    {
        if (Application.Current?.Dispatcher == null) return;

        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            TBlockStatus2.Text = message;
        });
    }
    #endregion

    #region SignalR Event
    public async void OnSignalRLogin(object sender, BoolEventArgs e)
    {
        await Application.Current.Dispatcher.BeginInvoke(() =>
        {
            if (!e.bValue)
            {
                UpdateStatus($"로그인 재시도 중... ({SrGlobalClient.s_nLoginRetryCount}번째)");
                return;
            }

            SrGlobalClient_LoginEvent -= OnSignalRLogin;
            UpdateStatus("로그인 성공! 메인 화면 여는 중...");

            MainWnd wnd = new MainWnd();
            wnd.Show();

            this.Close();
        });
    }

    public void OnSignalRClosed(object sender, IntEventArgs e)
    {
         UpdateStatus($"서버 연결 재시도 중... ({e.nValue}번째)");
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
        //await s_SrGClient.DisconnectAsync();
        this.Close();
        Application.Current.Shutdown();
    }
    #endregion
}
