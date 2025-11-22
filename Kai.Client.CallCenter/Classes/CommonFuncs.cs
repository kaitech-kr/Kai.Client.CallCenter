using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Text.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using Draw = System.Drawing;

using Kai.Common.FrmDll_FormCtrl;
using Kai.Common.StdDll_Common;
using Kai.Common.NetDll_WpfCtrl.NetMsgs;
using Kai.Common.NetDll_WpfCtrl.NetOFR;
using Kai.Server.Main.KaiWork.DBs.Postgres.CharDB.Models;
using Kai.Client.CallCenter.Networks;
using Kai.Client.CallCenter.OfrWorks;

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
            s_sKaiLogId = JsonFileManager.GetValue("Kaitech_LoginInfo:Kaitech_sId");
            s_sKaiLogPw = JsonFileManager.GetValue("Kaitech_LoginInfo:Kaitech_sPw");

            // Load ExternalApps Configuration
            LoadExternalAppsConfig();

            // Init Chars
            InitListChars();

#if DEBUG
            CommonVars.s_bDebugMode = true;
#endif
            //FormFuncs.MsgBox("Here2");
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

    private static bool InitListChars()
    {
        // 파일이 존재하는지 확인
        if (!File.Exists(s_sHanPath))
        {
            MessageBox.Show("File not found: " + s_sHanPath);
            return false;
        }

        if (!File.Exists(s_sEngPath))
        {
            MessageBox.Show("File not found: " + s_sEngPath);
            return false;
        }

        if (!File.Exists(s_sNumPath))
        {
            MessageBox.Show("File not found: " + s_sNumPath);
            return false;
        }

        if (!File.Exists(s_sSpecialPath))
        {
            MessageBox.Show("File not found: " + s_sSpecialPath);
            return false;
        }

        // ������ �о� ����Ʈ�� ����
        s_ListHan = File.ReadAllText(s_sHanPath).ToCharArray().ToList();
        s_ListEng = File.ReadAllText(s_sEngPath).ToCharArray().ToList();
        s_ListNum = File.ReadAllText(s_sNumPath).ToCharArray().ToList();
        s_ListSpecial = File.ReadAllText(s_sSpecialPath).ToCharArray().ToList();

        s_ListCharGroup.Add(s_ListHan);
        s_ListCharGroup.Add(s_ListEng);
        s_ListCharGroup.Add(s_ListNum);
        s_ListCharGroup.Add(s_ListSpecial);

        return true;
    }

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

    #region WPF Controls
    /// <summary>
    /// ComboBox에서 선택된 항목의 텍스트 가져오기
    /// </summary>
    /// <param name="comboBox">대상 ComboBox</param>
    /// <returns>선택된 항목의 텍스트, 없으면 빈 문자열</returns>
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

    /// <summary>
    /// ComboBox에서 특정 텍스트를 가진 항목의 인덱스 찾기
    /// </summary>
    /// <param name="comboBox">대상 ComboBox</param>
    /// <param name="targetValue">찾을 텍스트</param>
    /// <returns>항목의 인덱스, 찾지 못하면 -1</returns>
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

    /// <summary>
    /// ComboBox에서 특정 텍스트를 가진 항목을 선택
    /// </summary>
    /// <param name="comboBox">대상 ComboBox</param>
    /// <param name="content">선택할 항목의 텍스트</param>
    /// <returns>설정된 인덱스, 찾지 못하면 -1</returns>
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

    /// <summary>
    /// Button의 Opacity를 설정하여 활성화/비활성화 표시
    /// </summary>
    /// <param name="btn">대상 Button</param>
    /// <param name="bEnable">true면 활성화, false면 비활성화</param>
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

// CommonFuncs_StdResult
public class CommonFuncs_StdResult
{
    // ErrMsgResult_Bool
    public static StdResult_Bool ErrMsgResult_Bool(string sErr, string sPos, bool bWrite = true, bool bMsgBox = true)
    {
        if (bWrite) Debug.WriteLine($"sErr: {sErr}, sPos: {sPos}");
        if (bMsgBox) FormFuncs.ErrMsgBox(sErr, sPos);

        return new StdResult_Bool(sErr, sPos, CommonVars.s_sLogDir);
    }
    public static StdResult_Error ErrMsgResult_Bool(StdResult_Status result, bool bWrite = true, bool bMsgBox = true)
    {
        if (bWrite) Debug.WriteLine($"sErr: {result.sErr}, sPos: {result.sPos}");
        if (bMsgBox) FormFuncs.ErrMsgBox(result.sErr, result.sPos);

        return result;
    }

    // ErrMsgResult_NulBool
    public static StdResult_NulBool ErrMsgResult_NulBool(string sErr, string sPos, bool bWrite = true, bool bMsgBox = true)
    {
        if (bWrite) Debug.WriteLine($"sErr: {sErr}, sPos: {sPos}");
        if (bMsgBox) FormFuncs.ErrMsgBox(sErr, sPos);

        return new StdResult_NulBool(sErr, sPos, CommonVars.s_sLogDir);
    }
    public static StdResult_NulBool ErrMsgResult_NulBool(StdResult_NulBool result, bool bWrite = true, bool bMsgBox = true)
    {
        if (bWrite) Debug.WriteLine($"sErr: {result.sErr}, sPos: {result.sPos}");
        if (bMsgBox) FormFuncs.ErrMsgBox(result.sErr, result.sPos);

        return result;
    }

    // ErrMsgResult_Error
    public static StdResult_Error ErrMsgResult_Error(string sErr, string sPos, bool bWrite = true, bool bMsgBox = true)
    {
        if (bWrite) Debug.WriteLine($"sErr: {sErr}, sPos: {sPos}");
        if (bMsgBox) FormFuncs.ErrMsgBox(sErr, sPos);

        return new StdResult_Error(sErr, sPos, CommonVars.s_sLogDir);
    }
    public static StdResult_Error ErrMsgResult_Error(StdResult_Status result, bool bWrite = true, bool bMsgBox = true)
    {
        if (bWrite) Debug.WriteLine($"sErr: {result.sErr}, sPos: {result.sPos}");
        if (bMsgBox) FormFuncs.ErrMsgBox(result.sErr, result.sPos);

        return result;
    }

    // ErrMsgResult_Status
    public static StdResult_Status ErrMsgResult_Status(StdResult result, string sErr, string sPos, bool bWrite = true, bool bMsgBox = true)
    {
        if (bWrite) Debug.WriteLine($"sErr: {sErr}, sPos: {sPos}");
        if (bMsgBox) FormFuncs.ErrMsgBox(sErr, sPos);

        return new StdResult_Status(result, sErr, sPos, CommonVars.s_sLogDir);
    }
    public static StdResult_Status ErrMsgResult_Status(StdResult_Status result, bool bWrite = true, bool bMsgBox = true)
    {
        if (bWrite) Debug.WriteLine($"sErr: {result.sErr}, sPos: {result.sPos}");
        if (bMsgBox) FormFuncs.ErrMsgBox(result.sErr, result.sPos);

        return result;
    }

    // ErrMsgResult_Object
    public static StdResult_Object ErrMsgResult_Object(string sErr, string sPos, bool bWrite = true, bool bMsgBox = true)
    {
        if (bWrite) Debug.WriteLine($"sErr: {sErr}, sPos: {sPos}");
        if (bMsgBox) FormFuncs.ErrMsgBox(sErr, sPos);

        return new StdResult_Object(sErr, sPos, CommonVars.s_sLogDir);
    }
    public static StdResult_Object ErrMsgResult_Object(StdResult_Object result, bool bWrite = true, bool bMsgBox = true)
    {
        if (bWrite) Debug.WriteLine($"sErr: {result.sErr}, sPos: {result.sPos}");
        if (bMsgBox) FormFuncs.ErrMsgBox(result.sErr, result.sPos);

        return result;
    }

    // ErrMsgResult_String
    public static StdResult_String ErrMsgResult_String(string sErr, string sPos, bool bWrite = true, bool bMsgBox = true)
    {
        if (bWrite) Debug.WriteLine($"sErr: {sErr}, sPos: {sPos}");
        if (bMsgBox) FormFuncs.ErrMsgBox(sErr, sPos);

        return new StdResult_String(sErr, sPos, CommonVars.s_sLogDir);
    }
    public static StdResult_String ErrMsgResult_String(StdResult_String result, bool bWrite = true, bool bMsgBox = true)
    {
        if (bWrite) Debug.WriteLine($"sErr: {result.sErr}, sPos: {result.sPos}");
        if (bMsgBox) FormFuncs.ErrMsgBox(result.sErr, result.sPos);

        return result;
    }
}

// CommonFuncs_PostgResult
public class CommonFuncs_PostgResult
{
    // ErrMsgResult_Status
    public static StdResult_Status ErrMsgResult_Status(StdResult result, string sErr, string sPos, bool bWrite = true, bool bMsgBox = true)
    {
        if (bWrite) Debug.WriteLine($"sErr: {sErr}, sPos: {sPos}");
        if (bMsgBox) FormFuncs.ErrMsgBox(sErr, sPos);

        return new StdResult_Status(result, sErr, sPos, CommonVars.s_sLogDir);
    }
    public static StdResult_Status ErrMsgResult_Status(StdResult_Status result, bool bWrite = true, bool bMsgBox = true)
    {
        if (bWrite) Debug.WriteLine($"sErr: {result.sErr}, sPos: {result.sPos}");
        if (bMsgBox) FormFuncs.ErrMsgBox(result.sErr, result.sPos);

        return result;
    }
}

// CommonFuncs_OfrResult
public class CommonFuncs_OfrResult
{
    // ErrMsgResult_TbText
    public static OfrResult_TbText ErrMsgResult_TbText(TbText tb, OfrModel_BitmapAnalysis analy, string sErr, string sPos, bool bWrite = true, bool bMsgBox = true)
    {
        if (bWrite) Debug.WriteLine($"sErr: {sErr}, sPos: {sPos}");
        if (bMsgBox) FormFuncs.ErrMsgBox(sErr, sPos);

        return new OfrResult_TbText(analy, sErr, sPos, CommonVars.s_sLogDir);
    }

    // ErrMsgResult_TbCharSet
    public static OfrResult_TbCharSetList ErrMsgResult_TbCharSetList(Draw.Bitmap bmpCapture, string sErr, string sPos, bool bWrite = true, bool bMsgBox = true)
    {
        if (bWrite) Debug.WriteLine($"sErr: {sErr}, sPos: {sPos}");
        if (bMsgBox) FormFuncs.ErrMsgBox(sErr, sPos);

        return new OfrResult_TbCharSetList(bmpCapture, sErr, sPos, CommonVars.s_sLogDir);
    }
    public static OfrResult_TbCharSetList ErrMsgResult_TbCharSetList(OfrResult_TbCharSetList result, bool bWrite = true, bool bMsgBox = true)
    {
        if (bWrite) Debug.WriteLine($"sErr: {result.sErr}, sPos: {result.sPos}");
        if (bMsgBox) FormFuncs.ErrMsgBox(result.sErr, result.sPos);

        return result;
    }
}

//// CommonFuncs_AutoAllocResult
//public class CommonFuncs_AutoAllocResult
//{
//    public static AutoAllocResult_State ErrMsgResult_AutoAllocState(AutoAlloc_StateResult result, string sErr, string sPos, bool bWrite = true, bool bMsgBox = true)
//    {
//        if (bWrite) Debug.WriteLine($"sErr: {sErr}, sPos: {sPos}");
//        if (bMsgBox) FormFuncs.ErrMsgBox(sErr, sPos);

//        return new AutoAllocResult_State(result, sErr, sPos, CommonVars.s_sLogDir);
//    }

//    public static AutoAllocResult_Datagrid ErrMsgResult_AutoAllocDGrid(string sErr, string sPos, bool bWrite = true, bool bMsgBox = true)
//    {
//        if (bWrite) Debug.WriteLine($"sErr: {sErr}, sPos: {sPos}");
//        if (bMsgBox) FormFuncs.ErrMsgBox(sErr, sPos);

//        return new AutoAllocResult_Datagrid(sErr, sPos, CommonVars.s_sLogDir);
//    }
//}
#nullable enable