using System.IO;
using System.Windows;
using System.Diagnostics;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using Ctrl = System.Windows.Controls;

using Kai.Common.FrmDll_FormCtrl;
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
//using Kai.Client.CallCenter.Networks.NwInsungs;
using Kai.Client.CallCenter.Pythons;
using static Kai.Client.CallCenter.Classes.CommonVars;
using static Kai.Client.CallCenter.Classes.SrGlobalClient;
using static Kai.Client.CallCenter.Classes.SrLocalClient;
using static Kai.Client.CallCenter.Pythons.Py309Common;
using Kai.Common.NetDll_WpfCtrl.NetMsgs;

namespace Kai.Client.CallCenter.Windows;
#nullable disable
public partial class MainWnd : Window
{
    #region variables
    // Local
    private bool _isShuttingDown = false; // 중복 close 방지

    // AllPages
    public TabItem Customer_CustRegistTab = null;
    public TabItem Company_CompRegistTab = null;
    public TabItem Order_StatusTab = null;


    //AllWindows
    public Config_CtrlAppWnd m_ConfigRegistryWnd = null; // 설정 - Ctrl OtherApp
    //public Config_TelMainWnd m_ConfigTelMainWnd = null;  // 인터넷전화관리 - 대표전화
    public VirtualMonitorWnd m_WndForVirtualMonitor = null; // 가상모니터 를 보기위한 윈도
    public MasterModeManager m_MasterManager = null; // Master 모드 관리자
    #endregion

    #region Basic
    public MainWnd()
    {
        // CommonFuncs 초기화 (SplashWnd를 건너뛴 경우 대비)
        if (string.IsNullOrEmpty(s_sKaiLogId))
        {
            CommonFuncs.Init();
            Debug.WriteLine("[MainWnd] CommonFuncs.Init() 호출 (SplashWnd 건너뜀)");
        }

        InitializeComponent();

        #region 폴더, 파일 체크 
        if (!File.Exists("Kai.Common.CppDll_Common.dll"))
        {
            FormFuncs.ErrMsgBox($"현재 디렉토리({s_sCurDir})에서 Kai.Common.CppDll_Common.dll를 찾을 수 없습니다.", "MainWnd/MainWnd_01");
            Application.Current.Shutdown();
            return;
        }

        // 현재 작업(bin)디렉토리에 Kai.X86ComHostClient.exe 없으면 종료
        if (!File.Exists("Kai.Client.X86ComBroker.exe"))
        {
            FormFuncs.ErrMsgBox($"현재 디렉토리({s_sCurDir})에서 Kai.Client.X86ComBroker.exe를 찾을 수 없습니다.", "MainWnd/MainWnd_02");
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
                FormFuncs.ErrMsgBox($"[{s_sX86ProcName}]를 종료할수 없읍니다.", "MainWnd/MainWnd_03");
                Application.Current.Shutdown();
                return;
            }
        }

        // 현재 작업(bin)디렉토리에 VirtualMonitor ExeFolder(usbmmidd_v2)가 없으면 종료
        if (Directory.Exists(s_sCurDir + "\\" + FrmVirtualMonitor.c_sExeFolder) == false)
        {
            FormFuncs.ErrMsgBox($"현재 디렉토리에서 {FrmVirtualMonitor.c_sExeFolder}를 찾을 수 없습니다.", "MainWnd/MainWnd_04");
            Application.Current.Shutdown();
            return;
        }

        // 현재 작업(bin)디렉토리에 Data가 없으면 종료
        if (Directory.Exists(s_sCurDir + "\\" + "Data") == false)
        {
            FormFuncs.ErrMsgBox("현재 디렉토리에서 Data폴더를 찾을 수 없습니다.", "MainWnd/MainWnd_05");
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
            FormFuncs.ErrMsgBox("Alarm.wav를 찾을 수 없습니다.", "MainWnd/MainWnd_05_1");
            Application.Current.Shutdown();
            return;
        }
        #endregion 폴더, 파일 체크

        #region Action
        // Python
        StdResult_Bool result = Py309Common.Create();
        if (!result.bResult)
        {
            FormFuncs.ErrMsgBox($"{result}", "MainWnd/MainWnd_06");
            return;
        }

        #region Python Test
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
        //SrGlobalClient_LoginEvent += OnSrGlobalClient_LoginAgain; // Reserved
        //SrGlobalClient_MultiLoginEvent += OnSrGlobalClient_MultiLogin; // Reserved
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
        s_X86Proc = StdProcess.OpenProcess(s_sX86ExecPath);
        if (s_X86Proc == null)
        {
            FormFuncs.ErrMsgBox($"{s_sX86ExecPath} 실행실패", "MainWnd/MainWnd_09");
            Application.Current.Shutdown();
            goto ERR_EXIT;
        }
        await s_SrLClient.ConnectAsync();

        ////WindowProc
        //CtrlCppFuncs.GetHWndSource(this).AddHook(WindowProc);
        //if (!CtrlCppFuncs.SetMouseHook(s_hWndMain, MYMSG_MOUSEHOOK)) // Start Mouse Hooking
        //{
        //    FormFuncs.ErrMsgBox("마우스후킹 실패.", "MainWnd/MainWnd_07");
        //    goto ERR_EXIT;
        //}
        //if (!CtrlCppFuncs.SetKeyboardHook(s_hWndMain, MYMSG_KEYBOARDHOOK)) // Start Keyboard Hooking
        //{
        //    FormFuncs.ErrMsgBox("키보드후킹 실패.", "MainWnd/MainWnd_08");
        //    goto ERR_EXIT;
        //}
        #endregion

        //#region if AutoAlloc Or Not
        //if (s_bAutoAlloc)
        //{
        //    List<MonitorInfo> listOrg = await s_Screens.MonitorInfosToListAsync();
        //    if (s_Screens.m_VirtualMonitor == null) // 가상모니터가 없으면 생성
        //    {
        //        //if (s_Screens.m_WorkingMonitor != s_Screens.m_PrimaryMonitor) // 작업 모니터가 기본 모니터면 LoadingPanel을 사용하지 않는다.
        //        //NetLoadingWnd.ShowLoading(s_MainWnd, "   인성2 초기화 작업중입니다, \n     입력작업을 하지 마세요...   ");
        //        // ShowLoading
        //        NetLoadingWnd.ShowLoading(s_MainWnd, "가상모니터를 생성중 입니다.");

        //        //Test
        //        for (int i = 0; i < listOrg.Count; i++)
        //        {
        //            Debug.WriteLine($"Org: {i}, {listOrg[i]}");
        //        }

        //        int nOldCount = s_Screens.m_ListMonitorInfo.Count;
        //        //MessageBox.Show($"Monitor Count: {nOldCount}"); // Test

        //        StdResult_Bool resultBool = await FrmVirtualMonitor.MakeVirtualMonitorAsync();
        //        if (!resultBool.bResult)
        //        {
        //            FormFuncs.ErrMsgBox("가상모니터 생성실패", "MainWnd/MainWnd_10");
        //            goto ERR_EXIT;
        //        }

        //        await Task.Delay(1000);
        //        List<MonitorInfo> listNew = await s_Screens.MonitorInfosToListAsync();
        //        int nNewCount = s_Screens.m_ListMonitorInfo.Count;
        //        if (nOldCount == nNewCount)
        //        {
        //            FormFuncs.ErrMsgBox($"가상모니터 생성실패: 전({nOldCount})개 -> 후({nNewCount})개", "MainWnd/MainWnd_11");
        //            goto ERR_EXIT;
        //        }

        //        // Check Virtual Monitor Resolution
        //        if (!FrmVirtualMonitor.AdjustVirtualMonitorPosAndSize(s_Screens)) // 1920 x 1080 으로 변경
        //        {
        //            FormFuncs.ErrMsgBox("가상모니터 해상도 조정실패", "MainWnd/MainWnd_12");
        //            goto ERR_EXIT;
        //        }

        //        // 원상복귀
        //        int lastX = 0;
        //        for (int i = 0; i < listOrg.Count; i++)
        //        {
        //            lastX = i * 1920;
        //            s_Screens.ChangePosition(listOrg[i].DeviceName, lastX, 0);
        //            int index = listNew.FindIndex(x => x.DeviceName == listOrg[i].DeviceName);
        //            if (index >= 0) listNew.RemoveAt(index);
        //        }

        //        if (listNew.Count != 1)
        //        {
        //            FormFuncs.ErrMsgBox("가상모니터 생성실패", "MainWnd/MainWnd_12_1");
        //            goto ERR_EXIT;
        //        }
        //        lastX += (1920);
        //        s_Screens.ChangePosition(listNew[0].DeviceName, lastX, 1080);
        //        await s_Screens.MonitorInfosToListAsync();

        //        //Test
        //        for (int i = 0; i < s_Screens.m_ListMonitorInfo.Count; i++)
        //        {
        //            Debug.WriteLine($"Updated: {i}, {s_Screens.m_ListMonitorInfo[i]}");
        //        }

        //        // Hide
        //        NetLoadingWnd.HideLoading();

        //        // 앱 제어용 클래스를 생성한다. - VirtualMonitor가 있다는 전제하에 생성한다.
        //        if (s_Screens.m_VirtualMonitor != null)
        //        {
        //            s_Screens.m_WorkingMonitor = s_Screens.m_ListMonitorInfo[1];  // m_VirtualMonitor, m_PrimaryMonitor, m_ListMonitorInfo[0] 
        //            //s_Screens.m_WorkingMonitor = s_Screens.m_VirtualMonitor;  // m_VirtualMonitor, m_PrimaryMonitor, m_ListMonitorInfo[0] 
        //            StdResult_Status resultSt = await CtrlOtherApps.Instance.StartAsync();

        //            switch (resultSt.Result)
        //            {
        //                //case StdResult.Success: // Test
        //                //    // CtrlOtherApps 생성 성공
        //                //    ThreadMsgBox($"CtrlOtherApps 생성성공: {resultSt}"); // Test
        //                //    break;

        //                case StdResult.Fail:
        //                    FormFuncs.ErrMsgBox($"CtrlOtherApps 생성실패: {resultSt}", "MainWnd/MainWnd_13");
        //                    goto ERR_EXIT;

        //                case StdResult.Retry:
        //                    FormFuncs.MsgBox($"CtrlOtherApps 결과: {resultSt}", "MainWnd/MainWnd_14");
        //                    break;

        //                case StdResult.Exit:
        //                    FormFuncs.ErrMsgBox($"CtrlOtherApps 생성오류: {resultSt}", "MainWnd/MainWnd_15");
        //                    goto ERR_EXIT;

        //                default:
        //                    break;
        //            }
        //        }
        //    }
        //}
        //else
        //{

        //}
        //#endregion if AutoAlloc Or Not끝

        //// 가상모니터 를 보기위한 윈도 - 너무 일찍 만들어지지 않게한다(Timer Error)
        //m_WndForVirtualMonitor = new VirtualMonitorWnd();

        // Master 모드일 때만 초기화
        if (IsMasterMode)
        {
            Debug.WriteLine("[MainWnd] Master 모드 - MasterModeManager 초기화 시작");
            m_MasterManager = new MasterModeManager();
            StdResult_Status result = await m_MasterManager.InitializeAsync();

            if (result.Result != StdResult.Success)
            {
                FormFuncs.ErrMsgBox($"Master 모드 초기화 실패: {result}", "MainWnd/Window_Loaded_Master");
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

        // 마무리
        return;

    // 에러로 인한 종료
    ERR_EXIT:;
        Close();
        return;
    }

    private void Window_Unloaded(object sender, RoutedEventArgs e) // 이거 잘못사용하면 디버그종료 안함.
    {
        Debug.WriteLine("=== Window_Unloaded 호출됨!!! ===");
        //MsgBox("Window_Unloaded");

        // SignalR - Local Client
        SrLocalClient_ConnectedEvent -= OnSrLocalClient_Connected;
        SrLocalClient_ClosedEvent -= OnSrLocalClient_Closed; // Reserved

        // SignalR - Global Client
        SrGlobalClient_ClosedEvent -= OnSrGlobalClient_Closed; // Reserved

        // Python 종료
        try
        {
            Debug.WriteLine("Python 엔진 종료 시작");
            Py309Common.Destroy();
            Debug.WriteLine("Python 엔진 종료 완료");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Python 종료 실패: {ex.Message}");
        }

        // Close NetMsgWnd
        CommonFuncs.CloseExtMsgWndSimple();

        //if (Application.Current != null)
        //{
        //    // 혹시 모르는 윈도 Close
        //    foreach (Window win in Application.Current.Windows)
        //    {
        //        win.Close();
        //    }

        //    Application.Current.Shutdown();
        //}

        // ShutdownMode를 OnExplicitShutdown으로 설정했으므로 명시적으로 종료
        Debug.WriteLine("Application.Current.Shutdown() 호출");
        Application.Current.Shutdown();

        Debug.WriteLine("Environment.Exit(0) 호출로 강제 종료");
        Environment.Exit(0);
    }  
    
    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        Debug.WriteLine("=== Window_Closing 시작 ===");

        // 이미 종료 절차 진행 중이면 그대로 통과 (두 번째 호출부터는 취소하지 않음)
        if (_isShuttingDown)
        {
            Debug.WriteLine("_isShuttingDown = true, 그대로 통과");
            return;
        }

        // 1) 창 닫기 일단 막고
        Debug.WriteLine("e.Cancel = true 설정");
        e.Cancel = true;
        _isShuttingDown = true;

        try
        {
            // 사용자에게 종료 중 안내 (가능하면 비모달/오버레이 권장)
            CommonFuncs.ShowExtMsgWndSimple(s_MainWnd, "종료중 입니다...");

            // SignalR 연결 종료 (동기적으로)
            Debug.WriteLine("SignalR 연결 종료 시작");
            try
            {
                if (s_SrGClient != null)
                {
                    s_SrGClient.DisconnectAsync().Wait(2000); // 2초 타임아웃
                    Debug.WriteLine("SrGlobalClient 종료 완료");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SrGlobalClient 종료 실패: {ex.Message}");
            }

            try
            {
                if (s_SrLClient != null)
                {
                    s_SrLClient.DisconnectAsync().Wait(2000); // 2초 타임아웃
                    Debug.WriteLine("SrLocalClient 종료 완료");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SrLocalClient 종료 실패: {ex.Message}");
            }

            // Close s_X86Proc
            try
            {
                if (s_X86Proc != null)
                {
                    var r = s_SrLClient.SrResult_ComBroker_Close();
                    if (!r.bResult)
                    {
                        string err = StdProcess.Kill(s_sX86ProcName);
                        if (!string.IsNullOrEmpty(err)) ErrMsgBox(err);
                        else s_X86Proc = null;
                    }
                    else
                    {
                        s_X86Proc = null;
                    }
                }
            }
            catch (Exception ex) { Debug.WriteLine($"X86Proc close error: {ex.Message}"); }

            //// 5) CtrlOtherApps 정리 (토큰/스레드 해제)
            //try
            //{
            //    if (CtrlOtherApps.Instance != null)
            //    {
            //        await CtrlOtherApps.ctrlCancelToken.StopAsync();

            //        CtrlOtherApps.Instance.Close(); // 내부에서 _cts Dispose 등 정리
            //    }
            //}
            //catch (Exception ex) { Debug.WriteLine($"CtrlOtherApps.Close error: {ex.Message}"); }

            //// 4) 서브 윈도우/클라이언트 정리
            //try { m_WndForVirtualMonitor?.Close(); } catch (Exception ex) { Debug.WriteLine(ex); }
            //try { s_TransparentWnd?.Close(); } catch (Exception ex) { Debug.WriteLine(ex); }

            //try { s_SrGClient?.Dispose(); } catch (Exception ex) { Debug.WriteLine(ex); }

            //// 6) 훅/리소스 해제 (필요 시 주석 해제)
            //// try { CtrlCppFuncs.ReleaseMouseHook(); } catch {}
            //// try { CtrlCppFuncs.ReleaseKeyboardHook(); } catch {}
            //// try { CtrlCppFuncs.GetHWndSource(this).RemoveHook(WindowProc); } catch {}

            //// 7) Python 등 기타 리소스 종료
            //try { Py309Common.Destroy(); } catch (Exception ex) { Debug.WriteLine(ex); }
        }
        finally
        {
            // Master 모드 리소스 정리 - 가상 모니터 제거 (있을 때만)
            try
            {
                if (m_MasterManager != null)
                {
                    Debug.WriteLine("[MainWnd] MasterModeManager 정리 시작");
                    m_MasterManager.Shutdown();
                    m_MasterManager.Dispose();
                    m_MasterManager = null;
                    Debug.WriteLine("[MainWnd] MasterModeManager 정리 완료");
                }
            }
            catch (Exception ex) { Debug.WriteLine($"MasterModeManager 정리 실패: {ex.Message}"); }

            //// 가상 모니터 제거 (있을 때만)
            //if (s_Screens?.m_VirtualMonitor != null) FrmVirtualMonitor.DeleteVirtualMonitor();

            // 이벤트 핸들러를 제거하고 Dispatcher로 다시 Close 호출
            Debug.WriteLine("Closing 이벤트 제거 후 Dispatcher.BeginInvoke로 Close() 재호출");
            this.Closing -= Window_Closing;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                Debug.WriteLine("Dispatcher.BeginInvoke에서 Close() 호출");
                this.Close();
            }), System.Windows.Threading.DispatcherPriority.Normal);
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

    // 테스트 메뉴
    public void Menu_TmpTest_Click(object sender, RoutedEventArgs e)
    {
        //Debug.WriteLine("Menu_TmpTest_Click");
        //await CtrlOtherApps.ctrlCancelToken.PauseAsync();
        //MsgBox("PauseAsync");
        //CtrlOtherApps.ctrlCancelToken.Resume();
    }
    #endregion

    #region TabControl Events
    private void CloseAnyTabButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Parent is StackPanel panel && panel.Parent is TabItem tab)
        {
            MainTabCtrl.Items.Remove(tab);
        }
    }
    #endregion

    #region WindowProc
    //private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    //{
    //    switch ((uint)msg) 
    //    {
    //        //case 0x0020: // WM_SETCURSOR
    //        //    handled = true;
    //        //    Debug.WriteLine($"커서변경: {msg}, {wParam:X}, {lParam:X}"); // Test
    //        //    break;

    //        case LocalCommon.MYMSG_MOUSEHOOK: // Replace with your custom message ID MOUSE_MOVE 0x200
    //            handled = true;
    //            StdCommon32.MOUSEHOOKSTRUCT? m = Marshal.PtrToStructure<StdCommon32.MOUSEHOOKSTRUCT>(lParam);

    //            //Debug.WriteLine($"msg: {msg}, {wParam:X}, {lParam:X}"); // Test
    //            break;

    //        case LocalCommon.MYMSG_KEYBOARDHOOK: // Replace with your custom message ID
    //            handled = true;
    //            //Debug.WriteLine($"msg: {msg}, {wParam:X}, {lParam:X}"); // Test
    //            break;
    //    }

    //    // Return the result from the default window procedure
    //    return IntPtr.Zero;
    //}
    #endregion

    #region SignalR Events
    public async void OnSrLocalClient_Connected() // Local SignalRServer에 연결되면...
    {
        #region Tel070 - DB에서 Local 인터넷전화 정보 가져와서 s_sX86ProcName에 정보설정.
        PostgResult_TbTel070InfoList result = await s_SrGClient.SrResult_Tel070Info_SelectRowsAsync_Charge_NotMainTel();
        if (string.IsNullOrEmpty(result.sErr)) // 로컬 070전화 등록
        {
            s_ListTel070Info = result.listTb;
            //MsgBox($"No Err: {s_ListTel070Info.Count}, {s_ListTel070Info[0].TelNum}"); // Test
            await s_SrLClient.SrReport_Tel070Info_SetLocalsAsync(s_ListTel070Info);
        }
        else
        {
            ThreadErrMsgBox($"Err: OnSrLocalClient_Connected: {s_ListTel070Info.Count}, {result.sErr}",
                "MainWnd/OnSrLocalClient_Connected_01"); // Test {result}
        }
        #endregion
    }
    public void OnSrLocalClient_Closed(object sender, StdDelegate.ExceptionEventArgs e)
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
                    TblockConnLocal.Text = s_SrLClient.m_sConnSignslR;
                }
            });
        }
        finally
        {
            //await Task.Delay(1); // 잠시 대기
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

    //    FormFuncs.ErrMsgBox("Login At SignalRServer In MainWindow: Not Coded...");
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
    // TabItem
    public TabItem AddOrFocusPageInMainTabCtrl(string header, string pageName)
    {
        // Check if the page is already opened in the TabControl
        foreach (TabItem existingTab in MainTabCtrl.Items)
        {
            if (existingTab.Tag != null && existingTab.Tag.ToString() == pageName)
            {
                // If found, focus the existing tab
                existingTab.Focus();
                return existingTab; // Return the focused tab
            }
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

            this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                MainTabCtrl.Items.Add(tabItem);
                tabItem.Focus();
            }));

            return tabItem;
        }
        catch (Exception ex)
        {
            FormFuncs.ErrMsgBox(ex.Message, "에러: 탭콘트롤에 추가실패");
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