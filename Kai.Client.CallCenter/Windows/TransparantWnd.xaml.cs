using Kai.Common.StdDll_Common.StdWin32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static Kai.Common.StdDll_Common.StdWin32.StdCommon32;
using Draw = System.Drawing;
using Shape = System.Windows.Shapes;
using Wnd = System.Windows;

namespace Kai.Client.CallCenter.Windows;
#nullable disable
public partial class TransparantWnd : Wnd.Window, IDisposable
{
    #region Dispose
    private bool disposedValue;
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: 관리형 상태(관리형 개체)를 삭제합니다.
            }

            // TODO: 비관리형 리소스(비관리형 개체)를 해제하고 종료자를 재정의합니다.
            // TODO: 큰 필드를 null로 설정합니다.
            disposedValue = true;
        }
    }

    // // TODO: 비관리형 리소스를 해제하는 코드가 'Dispose(bool disposing)'에 포함된 경우에만 종료자를 재정의합니다.
    // ~TransparantWnd()
    // {
    //     // 이 코드를 변경하지 마세요. 'Dispose(bool disposing)' 메서드에 정리 코드를 입력합니다.
    //     Dispose(disposing: false);
    // } 

    public void Dispose()
    {
        // 이 코드를 변경하지 마세요. 'Dispose(bool disposing)' 메서드에 정리 코드를 입력합니다.
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    #endregion

    #region Variables
    private IntPtr _targetHwnd = IntPtr.Zero;
    private static TransparantWnd _instance;
    private static Thread _uiThread;
    private static bool _isReady = false;
    #endregion

    #region 생성자
    public TransparantWnd(IntPtr targetHwnd)
    {
        InitializeComponent();
        //_targetHwnd = targetHwnd;

        //Loaded += (_, __) =>
        //{
        //    MakeClickThrough();
        //    UpdatePosition(targetHwnd);   // HWND용 위치 맞춤
        //    _isReady = true;
        //};
    }

    public TransparantWnd(Draw.Bitmap bmp)
    {
        InitializeComponent();

        //Loaded += (_, __) =>
        //{
        //    MakeClickThrough();
        //    // Bitmap -> WPF ImageSource 변환
        //    var hBitmap = bmp.GetHbitmap();
        //    try
        //    {
        //        var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
        //            hBitmap,
        //            IntPtr.Zero,
        //            Int32Rect.Empty,
        //            BitmapSizeOptions.FromEmptyOptions());

        //        var img = new System.Windows.Controls.Image
        //        {
        //            Source = bitmapSource,
        //            Stretch = Stretch.None
        //        };
        //        OverlayCanvas.Children.Add(img);
        //    }
        //    finally
        //    {
        //        // GDI 리소스 해제 (메모리 누수 방지)
        //        StdGdi32.DeleteObject(hBitmap);
        //    }

        //    UpdatePosition(bmp);          // Bitmap 크기로 맞춤
        //    _isReady = true;
        //};
    }
    #endregion

    #region 내부용 함수들
//     private void UpdatePosition(IntPtr hwnd)
//     {
//         if (!StdWin32.GetWindowRect(hwnd, out RECT rect)) return;

//         var dpi = VisualTreeHelper.GetDpi(this);
//         double fx = 96.0 / dpi.PixelsPerInchX;
//         double fy = 96.0 / dpi.PixelsPerInchY;

//         Left = rect.Left * fx;
//         Top = rect.Top * fy;
//         Width = (rect.Right - rect.Left) * fx;
//         Height = (rect.Bottom - rect.Top) * fy;
//     }

//     private void UpdatePosition(Draw.Bitmap bmp)
//     {
//         var dpi = VisualTreeHelper.GetDpi(this);
//         double fx = 96.0 / dpi.PixelsPerInchX;
//         double fy = 96.0 / dpi.PixelsPerInchY;

//         Left = 0;
//         Top = 0;
//         Width = bmp.Width * fx;
//         Height = bmp.Height * fy;
//     }

//     private void MakeClickThrough()
//     {
//         var hwnd = new WindowInteropHelper(this).Handle;
//         int exStyle = StdWin32.GetWindowLong(hwnd, GWL_EXSTYLE);
//         exStyle |= WS_EX_TRANSPARENT | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW;
//         StdWin32.SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);
//     }
    #endregion

    #region Overlay 생성/제거 API
    //public static void CreateOverlay(IntPtr targetHwnd)
    //{
    //    CreateOverlayInternal(() => new TransparantWnd(targetHwnd));
    //}

    //public static void CreateOverlay(Draw.Bitmap bmp)
    //{
    //    CreateOverlayInternal(() => new TransparantWnd(bmp));
    //}

//     private static void CreateOverlayInternal(Func<TransparantWnd> factory)
//     {
//         if (_instance != null)
//             DeleteOverlay();

//         _isReady = false;

//         _uiThread = new Thread(() =>
//         {
//             _instance = factory();
//             _instance.ShowActivated = false;
//             _instance.Show();
//             System.Windows.Threading.Dispatcher.Run();
//         });

//         _uiThread.SetApartmentState(ApartmentState.STA);
//         _uiThread.IsBackground = true;
//         _uiThread.Start();

//         while (!_isReady) Thread.Sleep(50);
//     }

    public static void DeleteOverlay()
    {
        if (_instance == null) return;

        _instance.Dispatcher.Invoke(() => _instance.Close());
        _instance.Dispose();
        _instance = null;
        _uiThread = null;
        _isReady = false;
    }
    #endregion

    #region Overlay 그리기/제어 API
    public static void ClearBoxes()
    {
        _instance?.Dispatcher.Invoke(() => _instance.OverlayCanvas.Children.Clear());
    }

    public static void TestDrawStatic()
    {
        _instance?.Dispatcher.Invoke(() =>
        {
            var rect = new System.Windows.Shapes.Rectangle
            {
                Width = 200,
                Height = 100,
                Stroke = Brushes.Red,
                StrokeThickness = 4,
                Fill = Brushes.Transparent
            };

            Canvas.SetLeft(rect, 50);
            Canvas.SetTop(rect, 50);
            _instance.OverlayCanvas.Children.Add(rect);

            Wnd.MessageBox.Show($"TestDrawStatic 실행됨\nChildren.Count = {_instance.OverlayCanvas.Children.Count}");
        });
    }

//     private static async Task BlinkAsync(UIElement element, int periodMs)
//     {
//         try
//         {
//             while (_instance != null && _instance.OverlayCanvas.Children.Contains(element))
//             {
//                 element.Visibility = element.Visibility == Visibility.Visible
//                     ? Visibility.Hidden
//                     : Visibility.Visible;

//                 await Task.Delay(periodMs);
//             }
//         }
//         catch { }
//     }

    //public static void DrawBoxAsync(int x, int y, int width, int height,
    //    Color? strokeColor = null, double thickness = 1, bool dashed = false, bool blink = false, int blinkPeriodMs = 500)
    //{
    //    _instance?.Dispatcher.BeginInvoke(new Action(() =>
    //    {
    //        var rect = new System.Windows.Shapes.Rectangle
    //        {
    //            Width = width,
    //            Height = height,
    //            Stroke = new SolidColorBrush(strokeColor ?? Colors.Black),
    //            StrokeThickness = thickness,
    //            Fill = Brushes.Transparent,
    //            StrokeDashArray = dashed ? new DoubleCollection { 4, 2 } : null
    //        };

    //        Canvas.SetLeft(rect, x);
    //        Canvas.SetTop(rect, y);
    //        _instance.OverlayCanvas.Children.Add(rect);

    //        if (blink) _ = BlinkAsync(rect, blinkPeriodMs);
    //    }));
    //}


    //public static void DrawBoxAsync(Draw.Rectangle rc,
    //    Color? strokeColor = null, double thickness = 1, bool dashed = false, bool blink = false, int blinkPeriodMs = 500)
    //{
    //    _instance?.Dispatcher.BeginInvoke(new Action(() =>
    //    {
    //        var rect = new System.Windows.Shapes.Rectangle
    //        {
    //            Width = rc.Width,
    //            Height = rc.Height,
    //            Stroke = new SolidColorBrush(strokeColor ?? Colors.Black),
    //            StrokeThickness = thickness,
    //            Fill = Brushes.Transparent,
    //            StrokeDashArray = dashed ? new DoubleCollection { 4, 2 } : null
    //        };

    //        Canvas.SetLeft(rect, rc.Left);
    //        Canvas.SetTop(rect, rc.Top);
    //        _instance.OverlayCanvas.Children.Add(rect);

    //        if (blink) _ = BlinkAsync(rect, blinkPeriodMs);
    //    }));
    //}

    //public static void DrawBoxAsync(int offset, 
    //    Color? strokeColor = null, double thickness = 1, bool dashed = false, bool blink = false, int blinkPeriodMs = 500)
    //{
    //    _instance?.Dispatcher.BeginInvoke(new Action(() =>
    //    {
    //        // Overlay 전체 크기 가져오기
    //        double x = 0 - offset;
    //        double y = 0 - offset;
    //        double w = _instance.OverlayCanvas.ActualWidth + (offset * 2);
    //        double h = _instance.OverlayCanvas.ActualHeight + (offset * 2);

    //        var rect = new System.Windows.Shapes.Rectangle
    //        {
    //            Width = w,
    //            Height = h,
    //            Stroke = new SolidColorBrush(strokeColor ?? Colors.Black),
    //            StrokeThickness = thickness,
    //            Fill = Brushes.Transparent,
    //            StrokeDashArray = dashed ? new DoubleCollection { 4, 2 } : null
    //        };

    //        Canvas.SetLeft(rect, x);
    //        Canvas.SetTop(rect, y);
    //        _instance.OverlayCanvas.Children.Add(rect);

    //        if (blink)
    //            _ = BlinkAsync(rect, blinkPeriodMs);
    //    }));
    //}

    #endregion
}
#nullable enable

