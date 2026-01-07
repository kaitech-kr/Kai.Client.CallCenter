using Kai.Client.CallCenter.Networks;
using Kai.Client.CallCenter.OfrWorks;
using Kai.Common.NetDll_WpfCtrl.NetMsgs;
using Kai.Common.NetDll_WpfCtrl.NetOFR;
// using Kai.Common.FrmDll_FormCtrl;
using Kai.Common.StdDll_Common;
using Kai.Server.Main.KaiWork.DBs.Postgres.CharDB.Models;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Draw = System.Drawing;

namespace Kai.Client.CallCenter.Classes;
#nullable disable
public class CommonFuncs : CommonVars
{
    #region Basics
    public static void Init()
    {
        // 콘솔 출력 인코딩 설정 (한글 깨짐 방지)
        try
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.InputEncoding = System.Text.Encoding.UTF8;
        }
        catch { /* 인코딩 설정 실패해도 무시 */ }

        try
        {
            // 로그 파일 경로 설정 및 디렉토리 생성
            string logFilePath = @"D:\CodeWork\WithVs2022\KaiWork\Kai.Client\Kai.Client.CallCenter\Log..txt";
            string logDir = Path.GetDirectoryName(logFilePath);
            if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);

            // TraceListener 설정 (공유 권한 부여하여 다른 앱과 충돌 방지)
            // DefaultTraceListener의 LogFileName을 수동으로 설정하면 파일 잠금이 발생할 수 있으므로 
            // FileStream을 직접 생성하여 공유 모드(ReadWrite)를 지원합니다.
            FileStream fs = new FileStream(logFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
            StreamWriter sw = new StreamWriter(fs) { AutoFlush = true };
            
            TextWriterTraceListener fileListener = new TextWriterTraceListener(sw);
            Trace.Listeners.Add(fileListener);
            Trace.AutoFlush = true;
            Debug.AutoFlush = true;

            Debug.WriteLine($"\n[SYSTEM] 로그 기록 시작: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            s_sKaiLogId = JsonFileManager.GetValue("Kaitech_LoginInfo:Kaitech_sId");
            s_sKaiLogPw = JsonFileManager.GetValue("Kaitech_LoginInfo:Kaitech_sPw");

            // Load ExternalApps Configuration
            LoadExternalAppsConfig();

            // Init Chars
            //InitListChars();

#if DEBUG
            CommonVars.s_bDebugMode = true;
#endif
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[CommonFuncs] Init 오류: {ex.Message}");
            Debug.WriteLine($"[CommonFuncs] StackTrace: {ex.StackTrace}");
        }
    }

    private static void LoadExternalAppsConfig()
    {
        Debug.WriteLine("[CommonFuncs] LoadExternalAppsConfig 실행");

        // AutoAlloc 설정 로드
        string sAutoAlloc = JsonFileManager.GetValue("ExternalApps:AutoAlloc");
        s_bAutoAlloc = StdConvert.StringToBool(sAutoAlloc);
        Debug.WriteLine($"[CommonFuncs] AutoAlloc 설정 로드: {s_bAutoAlloc}");

        // Insung01 설정 로드
        string sUse = JsonFileManager.GetValue("ExternalApps:Insung01:Use");
        Debug.WriteLine($"[CommonFuncs] sUse 확인: '{sUse}'");

        NwInsung01.s_Use = StdConvert.StringToBool(sUse);
        NwInsung01.s_Id = JsonFileManager.GetValue("ExternalApps:Insung01:Id");
        NwInsung01.s_Pw = JsonFileManager.GetValue("ExternalApps:Insung01:Pw");
        NwInsung01.s_AppPath = JsonFileManager.GetValue("ExternalApps:Insung01:AppPath");

        Debug.WriteLine($"[CommonFuncs] Insung01 설정 로드: Use={NwInsung01.s_Use}, Id={NwInsung01.s_Id}, AppPath={NwInsung01.s_AppPath} -----------------------");

        // Insung02 설정 로드
        string sUse02 = JsonFileManager.GetValue("ExternalApps:Insung02:Use");
        NwInsung02.s_Use = StdConvert.StringToBool(sUse02);
        NwInsung02.s_Id = JsonFileManager.GetValue("ExternalApps:Insung02:Id");
        NwInsung02.s_Pw = JsonFileManager.GetValue("ExternalApps:Insung02:Pw");
        NwInsung02.s_AppPath = JsonFileManager.GetValue("ExternalApps:Insung02:AppPath");

        Debug.WriteLine($"[CommonFuncs] Insung02 설정 로드: Use={NwInsung02.s_Use}, Id={NwInsung02.s_Id}, AppPath={NwInsung02.s_AppPath} -----------------------");

        // Cargo24 설정 로드
        string sUseCargo24 = JsonFileManager.GetValue("ExternalApps:Cargo24:Use");
        NwCargo24.s_Use = StdConvert.StringToBool(sUseCargo24);
        NwCargo24.s_Id = JsonFileManager.GetValue("ExternalApps:Cargo24:Id");
        NwCargo24.s_Pw = JsonFileManager.GetValue("ExternalApps:Cargo24:Pw");
        NwCargo24.s_AppPath = JsonFileManager.GetValue("ExternalApps:Cargo24:AppPath");

        Debug.WriteLine($"[CommonFuncs] Cargo24 설정 로드: Use={NwCargo24.s_Use}, Id={NwCargo24.s_Id}, AppPath={NwCargo24.s_AppPath} -----------------------");

        // Onecall 설정 로드
        string sUseOnecall = JsonFileManager.GetValue("ExternalApps:Onecall:Use");
        NwOnecall.s_Use = StdConvert.StringToBool(sUseOnecall);
        NwOnecall.s_Id = JsonFileManager.GetValue("ExternalApps:Onecall:Id");
        NwOnecall.s_Pw = JsonFileManager.GetValue("ExternalApps:Onecall:Pw");
        NwOnecall.s_AppPath = JsonFileManager.GetValue("ExternalApps:Onecall:AppPath");

        Debug.WriteLine($"[CommonFuncs] Onecall 설정 로드: Use={NwOnecall.s_Use}, Id={NwOnecall.s_Id}, AppPath={NwOnecall.s_AppPath} -----------------------");
    }

    //private static bool InitListChars()
    //{
    //    // 파일이 존재하는지 확인
    //    if (!File.Exists(s_sHanPath))
    //    {
    //        MessageBox.Show("File not found: " + s_sHanPath);
    //        return false;
    //    }

    //    if (!File.Exists(s_sEngPath))
    //    {
    //        MessageBox.Show("File not found: " + s_sEngPath);
    //        return false;
    //    }

    //    if (!File.Exists(s_sNumPath))
    //    {
    //        MessageBox.Show("File not found: " + s_sNumPath);
    //        return false;
    //    }

    //    if (!File.Exists(s_sSpecialPath))
    //    {
    //        MessageBox.Show("File not found: " + s_sSpecialPath);
    //        return false;
    //    }

    //    폰트 로드(변수가 주석 처리되어 있으므로 여기도 주석 처리해야 함)
    //    s_ListHan = File.ReadAllText(s_sHanPath).ToCharArray().ToList();
    //    s_ListEng = File.ReadAllText(s_sEngPath).ToCharArray().ToList();
    //    s_ListNum = File.ReadAllText(s_sNumPath).ToCharArray().ToList();
    //    s_ListSpecial = File.ReadAllText(s_sSpecialPath).ToCharArray().ToList();

    //    s_ListCharGroup.Add(s_ListHan);
    //    s_ListCharGroup.Add(s_ListEng);
    //    s_ListCharGroup.Add(s_ListNum);
    //    s_ListCharGroup.Add(s_ListSpecial);

    //    return true;
    //}

    // 마스터모드, 서브모드 결정
    public static string LoadAppMode()
    {
        if (!File.Exists("appsettings.json")) return "Sub";

        string json = File.ReadAllText("appsettings.json");
        using JsonDocument doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty("AppMode", out JsonElement mode))
            return mode.GetString() ?? "Sub";

        return "Sub";
    }
    #endregion

    #region Hooking
    public static void SetKeyboardHook()
    {
        CtrlCppFuncs.SetKeyboardHook(s_hWndMain, CommonVars.MYMSG_KEYBOARDHOOK);
    }
    public static void ReleaseKeyboardHook()
    {
        CtrlCppFuncs.ReleaseKeyboardHook();
    }

    public static async Task CheckCancelAndThrowAsync()
    {
        if (s_GlobalCancelToken.Token.IsCancellationRequested)
            throw new OperationCanceledException(s_GlobalCancelToken.Token);
        await s_GlobalCancelToken.WaitIfPausedOrCancelledAsync();
    }
    #endregion

    #region WPF Controls
    // ComboBox에서 선택된 항목의 텍스트 가져오기
    public static string GetSelectedComboBoxContent(ComboBox comboBox)
    {
        return Application.Current.Dispatcher.Invoke(() =>
        {
            if (comboBox == null)
            {
                Debug.WriteLine("[CommonFuncs] GetSelectedComboBoxContent: comboBox가 null입니다");
                return "";
            }

            if (comboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                return selectedItem.Content?.ToString() ?? "";
            }

            return "";
        });
    }

    // ComboBox에서 특정 텍스트를 가진 항목의 인덱스 찾기
    public static int GetComboBoxItemIndex(ComboBox comboBox, string targetValue)
    {
        return Application.Current.Dispatcher.Invoke(() =>
        {
            if (comboBox == null || targetValue == null)
                return -1;

            for (int i = 0; i < comboBox.Items.Count; i++)
            {
                object item = comboBox.Items[i];

                string value = item switch
                {
                    ComboBoxItem cbi => cbi.Content?.ToString(),
                    string str => str,
                    _ => item?.ToString()
                };

                if (value == targetValue)
                    return i;
            }

            return -1;
        });
    }

    // ComboBox에서 특정 텍스트를 가진 항목을 선택 (설정된 인덱스 반환, 실패 시 -1)
    public static int SetComboBoxItemByContent(ComboBox comboBox, string content)
    {
        return Application.Current.Dispatcher.Invoke(() =>
        {
            if (comboBox == null || content == null)
                return -1;

            int index = GetComboBoxItemIndex(comboBox, content);
            if (index >= 0)
            {
                comboBox.SelectedIndex = index;
            }

            return index;
        });
    }

    // Button의 Opacity를 설정하여 활성화/비활성화 표시
    public static void SetButtonOpacity(Button btn, bool bEnable)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (btn == null)
            {
                Debug.WriteLine("[CommonFuncs] SetButtonOpacity: btn이 null입니다");
                return;
            }

            if (bEnable)
                btn.Opacity = (double)Application.Current.FindResource("AppOpacity_Enabled");
            else
                btn.Opacity = (double)Application.Current.FindResource("AppOpacity_Disabled");
        });
    }
    #endregion

    #region NetMsgWnd
    public static void ShowExtMsgWndSimple(Window wndBase, string msg, string title = "")
    {
        if (s_WndMsgSimple != null) CloseExtMsgWndSimple();

        Application.Current.Dispatcher.Invoke(() =>
        {
            s_WndMsgSimple = NetMsgWnd.ShowExtMsgWndSimple(wndBase, msg, title);
        });
    }
    public static void CloseExtMsgWndSimple()
    {
        if (s_WndMsgSimple != null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                NetMsgWnd.CloseExtMsgWndSimple(s_WndMsgSimple);
            });

            s_WndMsgSimple = null;
        }
    }
    #endregion
}

// JSON 설정 파일 관리 클래스
public class JsonFileManager
{
    private static readonly string s_ConfigPath = "appsettings.json";

    // 읽기
    public static string GetValue(string key)
    {
        if (!File.Exists(s_ConfigPath))
            return string.Empty;

        string json = File.ReadAllText(s_ConfigPath);
        using JsonDocument doc = JsonDocument.Parse(json);

        var keys = key.Split(':');
        JsonElement current = doc.RootElement;

        foreach (var k in keys)
        {
            if (!current.TryGetProperty(k, out current))
                return string.Empty;
        }

        return current.ToString();
    }

    // 쓰기
    public static void SetValue(string key, string value)
    {
        string json = File.Exists(s_ConfigPath) ? File.ReadAllText(s_ConfigPath) : "{}";

        var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        dict ??= new Dictionary<string, object>();

        var keys = key.Split(':');
        // 간단하게 1레벨만 지원
        dict[keys[0]] = value;

        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(s_ConfigPath, JsonSerializer.Serialize(dict, options));
    }
}
#nullable enable