using System.IO;
using System.Windows;
using System.Diagnostics;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using Ctrl = System.Windows.Controls;

using Kai.Common.FrmDll_WpfCtrl;
using Kai.Common.NetDll_WpfCtrl;
using Kai.Common.NetDll_WpfCtrl.NetWnds;
using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;
using static Kai.Common.FrmDll_WpfCtrl.FrmSystemDisplays;
using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgBox;
using static Kai.Common.StdDll_Common.StdDelegate;
using static Kai.Common.StdDll_Common.StdWin32.StdCommon32;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Results;

using Kai.Client.CallCenter.Classes;
using Kai.Client.CallCenter.Classes.Class_Master;
using Kai.Client.CallCenter.Pythons;
using static Kai.Client.CallCenter.Classes.CommonVars;
using static Kai.Client.CallCenter.Classes.SrGlobalClient;
using static Kai.Client.CallCenter.Classes.SrLocalClient;
using static Kai.Client.CallCenter.Pythons.Py309Common;
using Kai.Common.NetDll_WpfCtrl.NetMsgs;
using System.Diagnostics.Eventing.Reader;

namespace Kai.Client.CallCenter.Windows;
#nullable disable
public partial class MainWnd : Window
{
    #region variables
    // Local
    private bool _isShuttingDown = false; // 중복 close 방지

    // Page Cache - Dictionary 기반 O(1) 검색 + LRU
    private const int MaxCachedTabs = 5; // 최대 캐시 탭 개수
    private readonly Dictionary<string, TabItem> _pageCache = new();
    private readonly LinkedList<string> _pageLruList = new(); // LRU 순서 추적

    // AllPages
    public TabItem Customer_CustRegistTab = null;
    public TabItem Company_CompRegistTab = null;
    public TabItem Order_StatusTab = null;

    //AllWindows
    public Config_CtrlAppWnd m_ConfigRegistryWnd = null; // 설정 - Ctrl OtherApp
    //public Config_TelMainWnd m_ConfigTelMainWnd = null;  // 인터넷전화관리 - 대표전화
    public VirtualMonitorWnd m_WndForVirtualMonitor = null; // 가상모니터 를 보기위한 윈도
    public MasterModeManager m_MasterManager = null; // Master 모드 관리자
    private AnimatedShutdownWnd _shutdownWnd = null; // 애니메이션 종료 화면
    
    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public int vkCode;
        public int scanCode;
        public int flags;
        public int time;
        public IntPtr dwExtraInfo;
    }
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);
    #endregion

    #region Basic
    public MainWnd()
    {
        // CommonFuncs 초기화 (SplashWnd를 건너뛴 경우 대비)
        if (string.IsNullOrEmpty(s_sKaiLogId))
        {
            //CommonFuncs.Init();
            Debug.WriteLine("[MainWnd] CommonFuncs.Init() 호출 (SplashWnd 건너뜀)");
        }

        InitializeComponent();

        #region 폴더, 파일 체크 
        if (!File.Exists("Kai.Common.CppDll_Common.dll"))
        {
            ErrMsgBox($"현재 디렉토리({s_sCurDir})에서 Kai.Common.CppDll_Common.dll를 찾을 수 없습니다.", "MainWnd/MainWnd_01");
            Application.Current.Shutdown();
            return;
        }

        // 현재 작업(bin)디렉토리에 Kai.X86ComHostClient.exe 없으면 종료
        if (!File.Exists("Kai.Client.X86ComBroker.exe"))
        {
            ErrMsgBox($"현재 디렉토리({s_sCurDir})에서 Kai.Client.X86ComBroker.exe를 찾을 수 없습니다.", "MainWnd/MainWnd_02");
            Application.Current.Shutdown();
            return;
        }

        // 사전작업 - X86ComHostClient 만약 있으면 종료
        if (StdProcess.Find(s_sX86ProcName))
        {
            new Thread(() =>
            {
                StdProcess.Kill(s_sX86ProcName);
            }).Start();

            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(100);
                if (!StdProcess.Find(s_sX86ProcName)) break;
            }

            if (StdProcess.Find(s_sX86ProcName))
            {
                ErrMsgBox($"[{s_sX86ProcName}]를 종료할수 없읍니다.", "MainWnd/MainWnd_03");
                Application.Current.Shutdown();
                return;
            }
        }

        // 현재 작업(bin)디렉토리에 VirtualMonitor ExeFolder(usbmmidd_v2)가 없으면 종료
        if (Directory.Exists(s_sCurDir + "\\" + FrmVirtualMonitor.c_sExeFolder) == false)
        {
            ErrMsgBox($"현재 디렉토리에서 {FrmVirtualMonitor.c_sExeFolder}를 찾을 수 없습니다.", "MainWnd/MainWnd_04");
            Application.Current.Shutdown();
            return;
        }

        // 현재 작업(bin)디렉토리에 Data가 없으면 종료
        if (Directory.Exists(s_sCurDir + "\\" + "Data") == false)
        {
            ErrMsgBox("현재 디렉토리에서 Data폴더를 찾을 수 없습니다.", "MainWnd/MainWnd_05");
            Application.Current.Shutdown();
            return;
        }

        // 현재 작업(bin)디렉토리에 s_sLogDir가 없으면 만든다
        if (Directory.Exists(s_sLogDir) == false)
        {
            Directory.CreateDirectory(s_sLogDir);
        }

        if (File.Exists(@"D:\CodeWork\WithVs2022\KaiWork\Kai.Client\Kai.Client.CallCenter\Kai.Client.CallCenter\Resources\Sounds\Alim.wav") == false)
        {
            ErrMsgBox("Alarm.wav를 찾을 수 없습니다.", "MainWnd/MainWnd_05_1");
            Application.Current.Shutdown();
            return;
        }
        #endregion 폴더, 파일 체크

        #region Action
        // Python
        StdResult_Bool result = Py309Common.Create();
        if (!result.bResult)
        {
            ErrMsgBox($"{result}", "MainWnd/MainWnd_06");
            return;
        }

        #region Python Test - 주석처리
        //s_PyTest.Test();
        //s_PyMouse.MoveTo(0, 0, 3); // Python Function Test 
        //PyResult_MonitorNum_Coordinate pyResult = PyImage.GetMonitorNumAndCoordinates(-1920, 0);
        //if (pyResult.bSuccess)
        //{
        //    FrmForm.ThreadMsgBox($"Python: {pyResult.bSuccess}, {pyResult.nMonitorNum}, {pyResult.ptCoordinate}, {pyResult.ptCoordinate}");
        //}
        //Draw.Rectangle rcAbs = new Draw.Rectangle(0, 0, 1000, 1000);
        //PyResult_Object pyResult = PyImage.ScreenShotOfMonitor(0, rcAbs, "Test001.png");
        //PyResult_Object pyResult = PyImage.ScreenShotByAbsCoordinate(rcAbs, "Test003.png");
        //FrmForm.ThreadMsgBox($"Python: {pyResult.bSuccess}, {pyResult.pyObjData}");
        #endregion

        // SignalR - Local Client
        SrLocalClient_ConnectedEvent += OnSrLocalClient_Connected;
        SrLocalClient_ClosedEvent += OnSrLocalClient_Closed; // Reserved

        // SignalR - Global Client
        SrGlobalClient_ClosedEvent += OnSrGlobalClient_Closed; // Reserved 
        #endregion
    }
    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        #region Pointer
        s_MainWnd = this;
        s_hWndMain = new WindowInteropHelper(this).Handle;
        TblockConnGlobal.Text = s_SrGClient.m_sLoginSignalR; // 메인윈도가 생성전이므로 생성후에 처리
        #endregion

        #region 실행
        Debug.WriteLine($"[MainWnd] 프로세스 실행 시도: {s_sX86ExecPath}");
        s_X86Proc = StdProcess.OpenProcess(s_sX86ExecPath);
        if (s_X86Proc == null)
        {
            Debug.WriteLine($"[MainWnd] 프로세스 실행 실패: {s_sX86ExecPath}");
            ErrMsgBox($"{s_sX86ExecPath} 실행실패", "MainWnd/MainWnd_09");
            Application.Current.Shutdown();
            goto ERR_EXIT;
        }
        Debug.WriteLine($"[MainWnd] 프로세스 실행 성공. SignalR 연결 시도...");
        await s_SrLClient.ConnectAsync();
        Debug.WriteLine($"[MainWnd] SignalR ConnectAsync 호출 완료");

        // WindowProc 메시지 루프를 통해 전역 키 감지
        HwndSource source = HwndSource.FromHwnd(s_hWndMain);
        source.AddHook(new HwndSourceHook(WindowProc));
        #endregion

        // 가상모니터 를 보기위한 윈도 - 너무 일찍 만들어지지 않게한다(Timer Error)
        m_WndForVirtualMonitor = new VirtualMonitorWnd();

        // Master 모드일 때만 초기화
        if (IsMasterMode)
        {
            m_MasterManager = new MasterModeManager();
            StdResult_Status result = await m_MasterManager.InitializeAsync();

            if (result.Result != StdResult.Success)
            {
                // 취소 중 알림창이 떠있다면 닫기
                CommonFuncs.CloseExtMsgWndSimple();

                if (result.Result != StdResult.Skip) // 능동적으로 작업취소하는 경우가 아니라면
                    ErrMsgBox($"Master 모드 초기화 실패: {result.sErr}\n\n위치: {result.sErrNPos}", "MainWnd/Window_Loaded_Master");

                goto ERR_EXIT;
            }
            Debug.WriteLine("[MainWnd] Master 모드 초기화 완료");
        }
        else
        {
            Debug.WriteLine("[MainWnd] Sub 모드 - Master 기능 비활성화");
        }

        // 모드 표시 업데이트
        TblockAppMode.Text = IsMasterMode ? "Master" : "Sub";
        TblockAppMode.Foreground = IsMasterMode ? Brushes.Green : Brushes.Blue;

        // 최대화
        await this.Dispatcher.BeginInvoke((Action)(() =>
        {
            if (this.WindowState != WindowState.Maximized) this.WindowState = WindowState.Maximized;
            MenuOrderMng_ReceiptStatus_Click(null, null);
        }));

        return;

    // 에러로 인한 종료
    ERR_EXIT:;
        Close();
        return;
    }

    private void Window_Unloaded(object sender, RoutedEventArgs e) // 이거 잘못사용하면 디버그종료 안함.
    {
        //SignalR - Local Client
        SrLocalClient_ConnectedEvent -= OnSrLocalClient_Connected;
        SrLocalClient_ClosedEvent -= OnSrLocalClient_Closed; // Reserved

        //SignalR - Global Client
        SrGlobalClient_ClosedEvent -= OnSrGlobalClient_Closed; // Reserved

        // Python 종료
        try
        {
            Py309Common.Destroy();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Python 종료 실패: {ex.Message}");
        }

        // Close NetMsgWnd
         //CommonFuncs.CloseExtMsgWndSimple();

        if (Application.Current != null)
        {
            // 혹시 모르는 윈도 Close
            foreach (Window win in Application.Current.Windows)
            {
                win.Close();
            }

            Application.Current.Shutdown();
        }

        // ShutdownMode를 OnExplicitShutdown으로 설정했으므로 명시적으로 종료
         Application.Current.Shutdown();
         Environment.Exit(0);
    }  
    
    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        Debug.WriteLine("=== Window_Closing 시작 (안전 모드 복구) ===");

        if (_isShuttingDown) return;
        
        // 1) 창 닫기 일단 막고 플래그 설정
        e.Cancel = true;
        _isShuttingDown = true;

        try
        {
            // 사용자에게 안내 (메시지 창 표시)
            // 안내 창이 화면에 그려지도록 유도
            CommonFuncs.ShowExtMsgWndSimple(s_MainWnd, "종료 중입니다. 잠시만 기다려 주세요...");
            
            // 화면 갱신을 위해 아주 잠깐 대기 (메시지 창이 뜰 포커스를 확보)
            System.Windows.Forms.Application.DoEvents(); 
            Thread.Sleep(100);

            // 2) SignalR 연결 종료 (동기 대기)
            if (s_SrGClient != null)
            {
                s_SrGClient.StopReconnection();
                s_SrGClient.DisconnectAsync().Wait(1000);
            }

            if (s_SrLClient != null)
            {
                s_SrLClient.StopReconnection();
                s_SrLClient.DisconnectAsync().Wait(1000);
                
                // COM 브로커 종료 시도
                try { s_SrLClient.SrResult_ComBroker_Close(); } catch { }
            }

            // 3) 외부 프로세스(X86 Broker) 확실히 종료
            if (StdProcess.Find(s_sX86ProcName))
            {
                StdProcess.Kill(s_sX86ProcName);
            }

            // 4) Python 리소스 종료
            try { Py309Common.Destroy(); } catch { }

            // 5) Master Mode 정리
            if (m_MasterManager != null)
            {
                m_MasterManager.ShutdownAsync().Wait(2000);
                m_MasterManager.Dispose();
                m_MasterManager = null;
            }

            // 6) 기타 윈도우 및 가상 모니터 정리
            if (s_Screens?.m_VirtualMonitor != null) FrmVirtualMonitor.DeleteVirtualMonitor();
            m_WndForVirtualMonitor?.Close();
            s_TransparentWnd?.Close();
            
            if (_shutdownWnd != null)
            {
                _shutdownWnd.Close();
                _shutdownWnd = null;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"종료 정리 중 에러 발생: {ex.Message}");
        }
        finally
        {
            // 7) 이벤트 핸들러 제거 후 프로세스 강제 종료
            this.Closing -= Window_Closing;
            
            Debug.WriteLine("=== 모든 정리 완료, 프로세스 종료 ===");
            
            // 모든 윈도우 닫기
            if (Application.Current != null)
            {
                foreach (Window win in Application.Current.Windows)
                {
                    try { if (win != this) win.Close(); } catch { }
                }
            }

            // 확실한 종료를 위해 Environment.Exit 호출
            Debug.WriteLine("[MainWnd] 키보드 전역 후킹 해제");
            CtrlCppFuncs.ReleaseKeyboardHook();

            Environment.Exit(0);
        }
    }
    #endregion

    #region MainMenu Events
    #region 고객관리
    // 고객등록  
    private void MenuCustomerMng_CustMainRegist_Click(object sender, RoutedEventArgs e)
    {
        Customer_CustRegistTab = AddOrFocusPageInMainTabCtrl("고객등록", "CustMain_RegistPage"); // 고객등록=임의, CustMain_RegistPage=클래스명
    }

    // 거래처 등록
    private void Menu_CompMng_Regist_Click(object sender, RoutedEventArgs e)
    {
        Company_CompRegistTab = AddOrFocusPageInMainTabCtrl("거래처등록", "Company_RegistPage");
    }
    #endregion End 고객관리

    #region 오더관리
    // 접수상황
    private void MenuOrderMng_ReceiptStatus_Click(object sender, RoutedEventArgs e)
    {
        Order_StatusTab = AddOrFocusPageInMainTabCtrl("오더상황", "Order_StatusPage");
    }
    #endregion
    #endregion

    #region BarMenu Events
    // 가상 모니터
    public void Menu_VirtualMonitor_Click(object sender, RoutedEventArgs e)
    {
        m_WndForVirtualMonitor = new VirtualMonitorWnd();
        m_WndForVirtualMonitor.ShowDialog();
        m_WndForVirtualMonitor = null; // Clear reference
    }

    public async void Menu_TmpTest_Click(object sender, RoutedEventArgs e)
    {
        s_GlobalCancelToken.Reset(); 

        Debug.WriteLine("[MainWnd] 마우스 후킹 테스트 시작 (20초간 잠금)");
        StdWin32.BlockInput(true);

        CommonFuncs.SetKeyboardHook();


        try
        {
            await Task.Delay(20000, s_GlobalCancelToken.Token);
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("[MainWnd] ESC에 의해 테스트가 중단되었습니다.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MainWnd] 테스트 중 예외: {ex.Message}");
        }
        finally
        {
            StdWin32.BlockInput(false);
            CommonFuncs.ReleaseKeyboardHook();

            Debug.WriteLine("[MainWnd] 마우스 후킹 테스트 종료 및 해제 완료");
        }
    }
    #endregion

    #region TabControl Events
    private void CloseAnyTabButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Parent is StackPanel panel && panel.Parent is TabItem tab)
        {
            // 캐시 및 LRU 리스트에서 제거
            var pageName = tab.Tag?.ToString();
            if (pageName != null)
            {
                _pageCache.Remove(pageName);
                _pageLruList.Remove(pageName);
                Debug.WriteLine($"[MainWnd] 페이지 캐시 제거: {pageName} (캐시 크기: {_pageCache.Count}/{MaxCachedTabs})");
            }
            
            MainTabCtrl.Items.Remove(tab);
        }
    }
    #endregion

    #region WindowProc
    private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch ((uint)msg) 
        {
            case CommonVars.MYMSG_MOUSEHOOK:
                break;

            case CommonVars.MYMSG_KEYBOARDHOOK:
                {
                    try
                    {
                        int vkCode = (int)wParam;
                        if (vkCode == 0x1B) // VK_ESCAPE
                        {
                            Debug.WriteLine("[WindowProc] ESC 감지 - 자동화 중단 및 후킹 해제");
                            
                            // 즉시 취소 안내창 표시 (루프가 길어서 피드백이 느린 점 보완)
                            CommonFuncs.ShowExtMsgWndSimple(this, "초기화를 취소 중입니다...", "작업 중단");

                            CommonVars.s_GlobalCancelToken.Cancel();
                            CommonFuncs.ReleaseKeyboardHook();
                            Kai.Common.StdDll_Common.StdWin32.StdWin32.BlockInput(false);
                            handled = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[WindowProc] 키보드 메시지 분석 중 예외: {ex.Message}");
                    }
                }
                break;
        }

        return IntPtr.Zero;
    }
    #endregion

    #region SignalR Events
    public async void OnSrLocalClient_Connected() // Local SignalRServer에 연결되면...
    {
        #region Tel070 - DB에서 Local 인터넷전화 정보 가져와서 s_sX86ProcName에 정보설정.
        //PostgResult_TbTel070InfoList result = await s_SrGClient.SrResult_Tel070Info_SelectRowsAsync_Charge_NotMainTel();
        PostgResult_TbTel070InfoList result = new PostgResult_TbTel070InfoList();
        if (string.IsNullOrEmpty(result.sErr)) // 로컬 070전화 등록
        {
            s_ListTel070Info = result.listTb;
            //MsgBox($"No Err: {s_ListTel070Info.Count}, {s_ListTel070Info[0].TelNum}"); // Test
            //await s_SrLClient.SrReport_Tel070Info_SetLocalsAsync(s_ListTel070Info);
        }
        else
        {
            ThreadErrMsgBox($"Err: OnSrLocalClient_Connected: {s_ListTel070Info.Count}, {result.sErr}",
                "MainWnd/OnSrLocalClient_Connected_01"); // Test {result}
        }
        #endregion
    }
    public async void OnSrLocalClient_Closed(object sender, StdDelegate.ExceptionEventArgs e)
    {
        try
        {
            if (Application.Current == null) return;
            if (Application.Current.Dispatcher == null) return;

            _ = Application.Current.Dispatcher.BeginInvoke(() =>
            {
                if (s_MainWnd == null || !s_MainWnd.IsLoaded) return;

                if (TblockConnLocal != null && s_SrLClient != null)
                {
                    TblockConnLocal.Text = ""; //s_SrLClient.m_sConnSignalR;
                }
            });
        }
        finally
        {
            await Task.Delay(1); // 잠시 대기
        }
    }
    #endregion

    #region SignalR Events - Global Client
    // Login
    //public async void OnSrGlobalClient_LoginAgain(object sender, BoolEventArgs e)
    //{
    //    // 처음 로그인은 SpalshWnd에서 발생하지만 재접속에 의한 호출에 대비...
    //    await Application.Current.Dispatcher.BeginInvoke(() =>
    //    {
    //        this.Title = $"Login At SignalRServer In MainWindow: Not Coded...";
    //    });

    //    ErrMsgBox("Login At SignalRServer In MainWindow: Not Coded...");
    //}

    //// MultiLogin - Reserved
    //public async void OnSrGlobalClient_MultiLogin(object sender, StringEventArgs e)
    //{
    //    ErrMsgBox(e.sValue);

    //    await Application.Current.Dispatcher.BeginInvoke(() =>
    //    {
    //        Close();
    //    });
    //}

    // Closed - Only By Self - Reserved
    public async void OnSrGlobalClient_Closed(object sender, StdDelegate.ExceptionEventArgs e)
    {
        if (Application.Current == null || Application.Current.Dispatcher == null) return;

        try
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (TblockConnGlobal != null && s_SrGClient != null)
                    TblockConnGlobal.Text = s_SrGClient.m_sLoginSignalR; // 수동으로 써준다 - 몇개 안되서.
            });
        }
        catch
        {
        }
    }
    #endregion

    #region Methods
    // TabItem - Dictionary 기반 O(1) 검색 + LRU
    public TabItem AddOrFocusPageInMainTabCtrl(string header, string pageName)
    {
        // Dictionary 캐시에서 O(1) 검색
        if (_pageCache.TryGetValue(pageName, out var existingTab))
        {
            // LRU: 최근 사용으로 이동
            _pageLruList.Remove(pageName);
            _pageLruList.AddLast(pageName);
            
            MainTabCtrl.SelectedItem = existingTab;
            Debug.WriteLine($"[MainWnd] 캐시에서 페이지 재사용: {pageName}");
            return existingTab;
        }

        // If the page is not already open, create a new tab for it
        try
        {
            TabItem tabItem = new TabItem();
            tabItem.Tag = pageName; // Set tag for later identification

            StackPanel headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            TextBlock headerText = new TextBlock
            {
                Text = header,
                Margin = new Thickness(0, 0, 5, 0)
            };
            headerPanel.Children.Add(headerText);

            Button closeButton = new Button
            {
                Width = 20,
                Height = 20,
                Background = Brushes.Transparent
            };

            Ctrl.Image closeImage = new Ctrl.Image();
            closeImage.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/Close001.png"));
            closeButton.Content = closeImage;

            closeButton.Click += CloseAnyTabButton_Click;
            headerPanel.Children.Add(closeButton);

            tabItem.Header = headerPanel;
            tabItem.Content = new Frame { Source = new Uri($"Pages/{pageName}.xaml", UriKind.RelativeOrAbsolute) };

            // LRU: 최대 개수 초과 시 가장 오래된 탭 제거
            if (_pageCache.Count >= MaxCachedTabs)
            {
                var oldestPageName = _pageLruList.First?.Value;
                if (oldestPageName != null)
                {
                    if (_pageCache.TryGetValue(oldestPageName, out var oldestTab))
                    {
                        _pageCache.Remove(oldestPageName);
                        _pageLruList.RemoveFirst();
                        MainTabCtrl.Items.Remove(oldestTab);
                        Debug.WriteLine($"[MainWnd] LRU: 오래된 탭 제거 - {oldestPageName}");
                    }
                }
            }
            
            // 캐시에 추가
            _pageCache[pageName] = tabItem;
            _pageLruList.AddLast(pageName);
            
            // TabControl에 추가 및 선택
            MainTabCtrl.Items.Add(tabItem);
            MainTabCtrl.SelectedItem = tabItem;
            
            Debug.WriteLine($"[MainWnd] 새 페이지 생성 및 캐시 추가: {pageName} (캐시 크기: {_pageCache.Count}/{MaxCachedTabs})");

            return tabItem;
        }
        catch (Exception ex)
        {
            ErrMsgBox(ex.Message, "에러: 탭콘트롤에 추가실패");
            return null;
        }
    }
    public void RemovePageByHeader(string header)
    {
        for (int i = 0; i < MainTabCtrl.Items.Count; i++)
        {
            TabItem item = (TabItem)MainTabCtrl.Items.GetItemAt(i);
            if ((string)item.Header == header) MainTabCtrl.Items.RemoveAt(i);
        }
    }
    public void RemoveTab(TabItem tabItem)
    {
        if (tabItem != null) MainTabCtrl.Items.Remove(tabItem);
    }
    #endregion

    #region Thread Methods
    //public async Task Thread_Test()
    //{
    //    for (; ; )
    //    {
    //        await Task.Delay(1000);
    //        await CtrlOtherApps.Instance.TestAsync();
    //    }
    //} 
    #endregion

    #region Tmp Events
    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {

    }

    private void Menu_BasicInfo_UserMng_Click(object sender, RoutedEventArgs e)
    {

    }

    private void Menu_InetTelMng_MainTelMng_Click(object sender, RoutedEventArgs e)
    {

    }

    private void Menu_InetTelMng_LocalTelMng_Click(object sender, RoutedEventArgs e)
    {

    }

    private void Menu_Config_CtrlApp_Click(object sender, RoutedEventArgs e)
    {

    }




    private void Menu_Order_Process_Click(object sender, RoutedEventArgs e)
    {

    }

    private void Menu_Order_MapDriver_Click(object sender, RoutedEventArgs e)
    {

    }
    #endregion
}
#nullable enable