using Kai.Common.FrmDll_WpfCtrl;
using System.Windows;
using System.Windows.Threading;

using static Kai.Client.CallCenter.Classes.CommonVars;

namespace Kai.Client.CallCenter.Windows;
#nullable disable
public partial class VirtualMonitorWnd : Window//, IDisposable
{
    #region Dispose
    //private bool disposedValue = false;
    //protected virtual void Dispose(bool disposing)
    //{
    //    if (!disposedValue)
    //    {
    //        if (disposing)
    //        {
    //            // TODO: 관리형 상태(관리형 개체)를 삭제합니다.
    //        }

    //        // TODO: 비관리형 리소스(비관리형 개체)를 해제하고 종료자를 재정의합니다.
    //        // TODO: 큰 필드를 null로 설정합니다.
    //        disposedValue = true;
    //        if (Timer != null)
    //        {
    //            Timer.Stop();
    //            Timer.Tick -= Timer_Tick;
    //        }
    //        if (s_MainWnd != null)
    //        {
    //            s_MainWnd.m_WndForVirtualMonitor = null;
    //        }
    //    }
    //}
    //public void Dispose()
    //{
    //    // 이 코드를 변경하지 마세요. 'Dispose(bool disposing)' 메서드에 정리 코드를 입력합니다.
    //    Dispose(disposing: true);
    //    GC.SuppressFinalize(this);
    //}
    #endregion

    #region Variables
    private DispatcherTimer Timer = null;
    #endregion

    #region Basic Funcs
    public VirtualMonitorWnd(int nRefreahDelayMiliSec = 100)
    {
        InitializeComponent();
        ////StartScreenCapture();
        //this.Owner = s_MainWnd;

        ////if (s_Screens.m_VirtualMonitor == null)
        ////{
        ////    MessageBox.Show($"가상모니터가 없읍니다.", "에러", MessageBoxButton.OK, MessageBoxImage.Error);
        ////    return;
        ////}
    }
    ~VirtualMonitorWnd()
    {
        // TmpHide
        //// 이 코드를 변경하지 마세요. 'Dispose(bool disposing)' 메서드에 정리 코드를 입력합니다.
        //Dispose(disposing: false);
    }
    #endregion

    #region Basic Events
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // ImgDisplay.Source = FrmVirtualMonitor.CaptureScreen().imgResult;

        // if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
        // {
        // Timer = new DispatcherTimer();
        // Timer.Interval = TimeSpan.FromMilliseconds(100); // Adjust as needed
        // Timer.Tick += Timer_Tick;
        // Timer.Start();
        // }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
    }
    private void Window_Closed(object sender, EventArgs e)
    {
        // TmpHide
        //Dispose();
    }

    //Timer_Tick
    private void Timer_Tick(object sender, EventArgs e)
    {
        // ImgDisplay.Source = FrmVirtualMonitor.CaptureScreen().imgResult;
    }
    #endregion
}
#nullable enable