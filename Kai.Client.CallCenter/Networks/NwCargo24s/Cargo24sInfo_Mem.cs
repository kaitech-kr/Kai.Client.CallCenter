using Draw = System.Drawing;

using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;
using Kai.Common.NetDll_WpfCtrl.NetOFR;
using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgBox;

namespace Kai.Client.CallCenter.Networks.NwCargo24s;

#nullable disable
public class Cargo24sInfo_Mem
{
    #region Variables - 1개만 있고 사용빈도가 있는 Wnd, Page는 미리 할당한다 - 편의성
    //public List<string> DelWndNames { get; set; } = new List<string> // 삭제된 윈도우 이름들
    //{
    //    //"]님으로 부터의 메세지",
    //};
    //public SplashWnd Splash { get; set; } = null;
    //public MainWnd Main { get; set; } = null;
    //public RcptRegPage RcptPage { get; set; } = null;
    ////public RcptRegWnd RcptWnd { get; set; } = null;
    #endregion Variables 끝

    #region 생성자
    //public Cargo24sInfo_Mem()
    //{
    //    Splash = new SplashWnd();
    //    Main = new MainWnd();
    //    RcptPage = new RcptRegPage();
    //}
    #endregion 생성자 끝

    #region Windows
    //public class SplashWnd
    //{
    //    // TopWnd
    //    public IntPtr TopWnd_hWnd;  // hWnd
    //    public uint TopWnd_uProcessId = 0; // ProcessId
    //    public uint TopWnd_uThreadId = 0;  // ThreadId

    //    // Sons
    //    public IntPtr IdWnd_hWnd;  // hWnd
    //    public IntPtr PwWnd_hWnd;  // hWnd
    //    public IntPtr LoginBtn_hWnd;  // hWnd
    //    public IntPtr CloseBtn_hWnd; // hWnd
    //}

    //public class MainWnd
    //{
    //    // TopWnd
    //    public IntPtr TopWnd_hWnd;  // hWnd

    //    // Sons
    //    public IntPtr CloseBtn_hWnd; // hWnd

    //    // ListSonWnd
    //    public List<StdCommon32_WndInfo> FirstLayer_ChildWnds;
    //    public StdCommon32_WndInfo WndInfo_MainMenu;
    //    public StdCommon32_WndInfo WndInfo_BarMenu;
    //    public StdCommon32_WndInfo WndInfo_MdiClient;
    //}

    //public class RcptRegWnd
    //{
    //    // TopWnd
    //    public IntPtr TopWnd_hWnd;  // hWnd

    //    public RcptRegWnd(IntPtr hWnd)
    //    {
    //        TopWnd_hWnd = hWnd;
    //    }
    //}
    #endregion Windows 끝

    #region Pages
    //public class RcptRegPage
    //{
    //    // TopWnd
    //    public IntPtr TopWnd_hWnd;  // hWnd

    //    // StatusBtn
    //    public IntPtr StatusBtn_hWnd접수;
    //    public IntPtr StatusBtn_hWnd운행;
    //    public IntPtr StatusBtn_hWnd취소;
    //    public IntPtr StatusBtn_hWnd완료;
    //    public IntPtr StatusBtn_hWnd정산;
    //    public IntPtr StatusBtn_hWnd전체;

    //    // CommandBtns GroupBox
    //    public IntPtr CmdBtn_hWnd신규;
    //    public IntPtr CmdBtn_hWnd조회;
    //    public int CmdBtn_nBrightness조회;

    //    // Datagrid
    //    public IntPtr DG오더_hWnd;
    //    public Draw.Rectangle DG오더_AbsRect;
    //    public Draw.Rectangle[,] DG오더_rcRelCells;
    //    public Draw.Point[] DG오더_ptRelChkRows;
    //    //public string[] DG오더_ColumnTexts;
    //    public int DG오더_nBackgroundBright = 0;

    //    public IntPtr 리스트항목_hWnd순서저장;
    //    public IntPtr 리스트항목_hWnd원래대로;

    //    // DateFrom
    //}
    //#endregion Pages 끝

    //#region 다수 있는 클래스
    //public class OrderBasic
    //{
    //    public int nTernNo = 0; // TernNo
    //    public string sStatus = string.Empty; // 상태
    //    public string sSeqNo = string.Empty; // SeqNo
    //}
    #endregion
}
#nullable restore
