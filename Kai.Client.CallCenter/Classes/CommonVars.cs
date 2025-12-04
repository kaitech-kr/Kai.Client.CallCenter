using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;

using Kai.Client.CallCenter.Pages;
using Kai.Client.CallCenter.Windows;
using Kai.Common.FrmDll_WpfCtrl;
using Kai.Common.NetDll_WpfCtrl.NetWnds;
using static Kai.Common.StdDll_Common.StdDelegate;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models;

namespace Kai.Client.CallCenter.Classes;
#nullable disable
public class CommonVars
{
    #region Variables
    // Constants
    //public const uint MYMSG_MOUSEHOOK = 1027; // 지금은 W
    //public const uint MYMSG_KEYBOARDHOOK = 1028;
    public const int c_nRepeatShort = 3; // 일반적인 반복횟수 - 3회
    public const int c_nRepeatNormal = 10; // 많은 반복횟수 - 10회
    public const int c_nRepeatMany = 50; // 많은 반복횟수 - 50회
    public const int c_nRepeatVeryMany = 100; // 많은 반복횟수 - 100회
    public const int c_nRepeatUltraMany = 250; // 많은 반복횟수 - 250회

    public const int c_nWaitUltraShort = 20;
    public const int c_nWaitVeryShort = 30;
    public const int c_nWaitShort = 50;
    public const int c_nWaitNormal = 100;
    public const int c_nWaitLong = 250;
    public const int c_nWaitVeryLong = 500;

    // Events
    public static LongNIntEventDelegate s_OrderUpdatedEventHandler = null; // OrderUpdated
    public static LongEventDelegate s_OrderRegistedEventHandler = null; // OrderRegisted

    // Enum
    public static CEnum_KaiAppMode s_AppMode = CEnum_KaiAppMode.Sub; // 기본은 Sub

    // ReadOnly
    public static readonly string s_sX86ProcName = "Kai.Client.X86ComBroker";
    public static readonly string s_sCurDir = Directory.GetCurrentDirectory();
    public static readonly string s_sDataDir = Path.Combine(s_sCurDir, "Data");
    public static readonly string s_sLogDir = Path.Combine(s_sCurDir, "Log");
    public static readonly string s_sX86ExecPath = //$"{s_sX86ProcName}.exe";
        $@"D:\CodeWork\WithVs2022\KaiWork\Kai.Client\Kai.Client.X86ComBroker\Kai.Client.X86ComBroker\bin\x86\Debug\net8.0-windows\{s_sX86ProcName}.exe";
    public static readonly string s_sImgFilesPath = @"D:\CodeWork\Common\Resource\StrImages";
    public static readonly Regex s_RegexOnlyNum = new Regex("[^0-9]+"); // 숫자가 아니면 true

    public static readonly string s_sCharSetDir = @"D:\Database\CharSetForDB";
    public static readonly string s_sHanPath = $"{s_sCharSetDir}\\chars_han.txt";
    public static readonly string s_sEngPath = $"{s_sCharSetDir}\\chars_eng.txt";
    public static readonly string s_sNumPath = $"{s_sCharSetDir}\\chars_num.txt";
    public static readonly string s_sSpecialPath = $"{s_sCharSetDir}\\chars_special.txt";

    public static readonly string[] s_sCharTypes = { "H", "E", "N", "S" };
    public static List<List<char>> s_ListCharGroup = new List<List<char>>();
    public static List<char> s_ListHan = new List<char>();
    public static List<char> s_ListEng = new List<char>();
    public static List<char> s_ListNum = new List<char>();
    public static List<char> s_ListSpecial = new List<char>();

    // Simple Static Variables
    public static bool s_bDebugMode = false;
    public static string s_sKaiLogId = "";
    public static string s_sKaiLogPw = "";
    //public static bool s_bAutoReceipt = true; // 나중에 적재방식 수정...
    public static bool s_bAutoAlloc = false; // appsettings.json으로 이동 (LoadExternalAppsConfig에서 로드)

    // Pointer
    public static SplashWnd s_SplashWnd = null; //  SplashWnd 
    public static IntPtr s_hWndMain = IntPtr.Zero;            // Main Window Handle
    public static MainWnd s_MainWnd = null;     // Main Window
    public static Process s_X86Proc = null; // x86 Client
    public static TransparantWnd s_TransparentWnd = null;// Transparent Window
    public static Order_StatusPage s_Order_StatusPage = null;
    public static TbCenterCharge s_CenterCharge = null; // 센터별 접속한 담당자정보 
    public static TbCallCenter s_CallCenter = null; // 센터별 접속한 콜센터정보 
    public static TbCallMember s_CallMember = null; // 센터별 접속한 회원사정보 

    // Instance
    public static readonly SrGlobalClient s_SrGClient = new SrGlobalClient();  // SignalR - Global
    public static readonly SrLocalClient s_SrLClient = new SrLocalClient();  // SignalR - Local
    public static List<TbTel070Info> s_ListTel070Info = new List<TbTel070Info>(); // 전화번호 정보
    public static readonly FrmRegistry s_KaiReg = new FrmRegistry("Kai.Client.CallCenter"); // Registry
    public static FrmSystemDisplays s_Screens = new FrmSystemDisplays(); // 시스템(화면)정보를 얻는다
    public static NetWndSimple s_WndMsgSimple = null;

    // Properties
    public static bool IsMasterMode => s_AppMode == CEnum_KaiAppMode.Master;
    public static bool IsSubMode => s_AppMode == CEnum_KaiAppMode.Sub;
    #endregion
}
#nullable restore