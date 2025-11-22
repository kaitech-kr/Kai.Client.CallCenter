using Draw = System.Drawing;

using Kai.Common.StdDll_Common;

namespace Kai.Client.CallCenter.Networks.NwOnecalls;
#nullable disable
public class OnecallInfo_File
{
    #region App
    public string App_sPredictFolder = @"C:\Program Files\원콜(주선사)";
    public string App_sExeFileName = "OneCallShipper.exe";
    #endregion

    #region Splash
    // TopWnd
    //public string Splash_TopWnd_sClassName = "WindowsForms10.Window.20008.app.0.34f5582_r7_ad1"; // ClassName
    public string Splash_TopWnd_sWndName = " ";   // WndName
   
    public Draw.Point Splash_IdWnd_ptChk = new Draw.Point(549, 342); // ID Wnd   
    public Draw.Point Splash_PwWnd_ptChk = new Draw.Point(549, 375); // PW Wnd   
    public Draw.Point Splash_LoginBtn_ptChk = new Draw.Point(684, 359); // Login Btn   
    public Draw.Point Splash_CloseBtn_ptChk = new Draw.Point(684, 417); // Close Btn
    #endregion

    #region Main
    // TopWnd
    //public string Main_TopWnd_sClassName = string.Empty; // ClassName
    public string Main_TopWnd_sWndNameReduct = "(주)원콜";   // WndName

    // MainMenu_Title
    public Draw.Rectangle Main_MainMenu_rcRel = new Draw.Rectangle(8, 31, 1920, 45); // MainMenu_Title Wnd Rectangle

    // MainMenu

    // BarMenu

    // MdiClient
    public Draw.Rectangle Main_MdiClient_rcRel = new Draw.Rectangle(8, 76, 1920, 937); // MdiClient Wnd Rectangle
    public string Main_MdiClient_ClassName = "WindowsForms10.MDICLIENT.app.0.34f5582_r7_ad1"; // ClassName
    #endregion

    #region 접수등록Page - 도킹상태 접수등록Page_TopWnd 기준
    // TopWnd
    //public string 접수등록Page_TopWnd_sClassName = "WindowsForms10.Window.8.app.0.34f5582_r7_ad1"; // ClassName
    public string 접수등록Page_TopWnd_sWndName = "화물등록";   // WndName

    // 접수 Section
    public Draw.Rectangle 접수등록Page_접수영역_rcChkRel = new Draw.Rectangle(0, 0, 1916, 400); // Check Point rcRel: {X=0,Y=0,Width=1916,Height=400}

    public Draw.Point 접수등록Page_접수Btn목록초기화_ptRelL = new Draw.Point(1084, 374); // 접수등록Page(화물등록) 기준

    // 검색 Section
    public string 접수등록Page_검색영역_sWndName = "검색"; // Check Text
    public Draw.Point 접수등록Page_검색Range_ptChkRelFromL = new Draw.Point(115, 18); // Check Point - From 
    public Draw.Point 접수등록Page_검색ExpandBtn_ptChkRelL = new Draw.Point(1892, 18); // Check Point - ExpandBtn


    // Datagrid Section
    public Draw.Rectangle 접수등록Page_DG오더_rcRelFirst = new Draw.Rectangle(0, 436, 1916, 474); // Check Point rcRel: {X=0,Y=436,Width=1916,Height=474}
    public int 접수등록Page_DG오더_nExpandedHeight = 874; // ExpandBtn Width

    //// 접수 Datagrid
    //public Draw.Point 접수등록Page_DG오더_ptCheckRel = new Draw.Point(892, 774); // Check Point - Center
    ////public Draw.Rectangle 접수등록Page_DG오더_rcCheckRel = new Draw.Rectangle(10, 229, 1916, 637); // Check Point

    //public string 접수등록Page_DG오더_sClassName = "WindowsForms10.Window.8.app.0.34f5582_r7_ad1"; // Check Window Name
    public int 접수등록Page_DG오더_headerHeight = 30; // HeaderRow Height
    public int 접수등록Page_DG오더_dataRowHeight = 23; // DataRow Height
    public int 접수등록Page_DG오더_smallRowsCount = 19; // DataRow Count
    public int 접수등록Page_DG오더_largeRowsCount = 36; // DataRow Count - 일단 오더가 없어서 눈대중으로 

    public string[] 접수등록Page_DG오더_colOrgTexts = new string[]
{
    "순번", "처리일자", "처리시간", "처리상태", "상차일", "하차일", "상차지", "하차지", "혼적", "화물정보",
    "운임", "수수료", "결제방법", "산재보험료 주선사부담", "산재보험료 차주부담", "톤수", "차종", "차량번호", "차주명", "차주전화", 
    "차주_톤수", "차주_차종", "적재옵션", "차주_사업자상호", "차주_사업자구분", "차주_사업자번호", "인수증", "매입계산서 발행일", "상차완료보고시간", "하차완료보고시간",
    "담당자번호", "오더번호"
};
    #endregion
}
#nullable enable
