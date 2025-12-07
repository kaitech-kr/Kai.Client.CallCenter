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

    #region #region 접수등록Page - 도킹상태 접수등록Page_TopWnd 기준

    #region TopWnd
    public string 접수등록Page_TopWnd_sWndName = "화물등록";   // WndName 
    #endregion

    #region 접수 Section
    // Top
    public Draw.Point 접수등록Page_접수섹션_ptChkRelT { get; set; } = new Draw.Point(1800, 255);

    // 버튼들
    public Draw.Point 접수등록Page_접수_목록초기화Btn_ptRelS { get; set; } = new Draw.Point(1084, 374); // 목록초기화Btn 
    public Draw.Point 접수등록Page_접수_신규Btn_ptChkRelS { get; set; } = new Draw.Point(54, 374); // 신규Btn {X=54,Y=374}
    public Draw.Point 접수등록Page_접수_저장Btn_ptChkRelS { get; set; } = new Draw.Point(178, 374); // 저장Btn {X=178,Y=374}

    // 상차지
    public Draw.Point 접수등록Page_접수_상차지권역_ptChkRelS { get; set; } = new Draw.Point(156, 58); // {X=156,Y=58}
    public Draw.Point 접수등록Page_접수_상차지주소_ptChkRelS { get; set; } = new Draw.Point(332, 58); // {X=332,Y=58}

    // 하차지
    public Draw.Point 접수등록Page_접수_하차지권역_ptChkRelS { get; set; } = new Draw.Point(156, 89); // {X=156,Y=89}
    public Draw.Point 접수등록Page_접수_하차지주소_ptChkRelS { get; set; } = new Draw.Point(332, 90); // {X=332,Y=90}

    // 화물정보
    public Draw.Point 접수등록Page_접수_화물정보_ptChkRelS { get; set; } = new Draw.Point(375, 121); // {X=375,Y=121}
    #endregion

    #region 검색 Section
    public Draw.Point 접수등록Page_검색섹션_ptChkRelT { get; set; } = new Draw.Point(1800, 515); // {X=1800,Y=515}
    public Draw.Point 접수등록Page_검색_새로고침Btn_ptChkRelS { get; set; } = new Draw.Point(597, 18); // 새로고침Btn {X=597,Y=18}
    public Draw.Point 접수등록Page_검색ExpandBtn_ptChkRelS { get; set; } = new Draw.Point(1892, 18); // ExpandBtn 
    #endregion

    #region Datagrid Section
    public int 접수등록Page_DG오더_nExpandedHeight = 874; // ExpandBtn Width
    public Draw.Point 접수등록Page_DG오더_ptChkRelT { get; set; } = new Draw.Point(1800, 975); // {X=1800,Y=975}

    // 접수 Datagrid
    public int 접수등록Page_DG오더_headerHeight = 30; // HeaderRow Height
    public int 접수등록Page_DG오더_dataRowHeight = 23; // DataRow Height
    public int 접수등록Page_DG오더_dataGab = 1; // 셀 상하단 gap
    public int 접수등록Page_DG오더_smallRowsCount = 17; // DataRow Count
    public int 접수등록Page_DG오더_largeRowsCount = 34; // DataRow Count

    public string[] 접수등록Page_DG오더_colOrgTexts = new string[]
    {
        "순번", "처리일자", "처리시간", "처리상태", "상차일", "하차일", "상차지", "하차지", "혼적", "화물정보",
        "운임", "수수료", "결제방법", "산재보험료 주선사부담", "산재보험료 차주부담", "톤수", "차종", "차량번호", "차주명", "차주전화",
        "차주_톤수", "차주_차종", "적재옵션", "차주_사업자상호", "차주_사업자구분", "차주_사업자번호", "인수증", "매입계산서 발행일", "상차완료보고시간", "하차완료보고시간",
        "담당자번호", "오더번호"
    };
    #endregion
    #endregion #endregion 접수등록Page - 도킹상태 접수등록Page_TopWnd 기준
}
#nullable enable
