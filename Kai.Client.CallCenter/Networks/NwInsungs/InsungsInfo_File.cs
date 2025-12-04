using Draw = System.Drawing;

using Kai.Common.NetDll_WpfCtrl.NetOFR;
using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgBox;

namespace Kai.Client.CallCenter.Networks.NwInsungs;
#nullable disable
[Serializable]
public class InsungsInfo_File
{
    #region App
    public string App_sPredictFolder { get; set; } = string.Empty;
    public string App_sExeFileName { get; set; } = string.Empty;
    #endregion

    #region Splash
    // TopWnd
    public string Splash_TopWnd_sWndName { get; set; } = string.Empty;   // WndName  

    // EditBox
    public Draw.Point Splash_IdWnd_ptChk { get; set; } = new Draw.Point(421, 219); // ID Wnd   
    public Draw.Point Splash_PwWnd_ptChk { get; set; } = new Draw.Point(421, 262); // PW Wnd
    #endregion

    #region MainWnd
    //// TopWnd
    public string Main_TopWnd_sWndNameReduct { get; set; } = "인성 퀵 서비스 [";  // WndName
    public string Main_AnyMenu_sClassName { get; set; } = "WindowsForms10.Window.20808.app.0.13965fa_r"; // "WindowsForms10.Window.20808.app.0.13965fa_r7_ad1";
    public string Main_AnyMenu_sWndName { get; set; } = "";

    // MainMenu_Title
    public Draw.Rectangle Main_MainMenu_rcRel { get; set; } = new Draw.Rectangle(8, 0, 1920, 63); // MainMenu_Title Wnd Rectangle

    // BarMenu
    public Draw.Rectangle Main_BarMenu_rcRel { get; set; } = new Draw.Rectangle(8, 63, 1920, 28);  // BarMenu Wnd Rectangle
    public Draw.Point Main_BarMenu_pt접수등록 { get; set; } = new Draw.Point(55, 15);  // 접수등록 // BarMenu - 접수등록
    public Draw.Point Main_BarMenu_pt고객등록 { get; set; } = new Draw.Point(155, 15); // BarMenu - 고객등록
    public Draw.Point Main_BarMenu_pt전화수신내역 { get; set; } = new Draw.Point(255, 15);  // BarMenu - 전화수신내역
    public Draw.Point Main_BarMenu_pt게시판 { get; set; } = new Draw.Point(345, 15); // BarMenu - 게시판
    public Draw.Point Main_BarMenu_pt기사메세지전송 { get; set; } = new Draw.Point(455, 15); // BarMenu - 기사메세지전송
    public Draw.Point Main_BarMenu_pt메세지전송 { get; set; } = new Draw.Point(555, 15); // BarMenu - 메세지전송
    public Draw.Point Main_BarMenu_pt계산기 { get; set; } = new Draw.Point(640, 15); // BarMenu - 계산기
    public Draw.Point Main_BarMenu_pt기사관제 { get; set; } = new Draw.Point(720, 15); // BarMenu - 기사관제
    public Draw.Point Main_BarMenu_pt픽업지알림 { get; set; } = new Draw.Point(830, 15); // BarMenu - 픽업지알림
    public Draw.Point Main_BarMenu_pt고객지원게시판 { get; set; } = new Draw.Point(935, 15); // BarMenu - 고객지원게시판
    public Draw.Point Main_BarMenu_pt원격지원요청 { get; set; } = new Draw.Point(1030, 15); // BarMenu - 원격지원요청
    public Draw.Point Main_BarMenu_pt관재맵설치 { get; set; } = new Draw.Point(1105, 15); // BarMenu - 관재맵설치

    // MdiClient
    public Draw.Rectangle Main_MdiClient_rcRel { get; set; } = new Draw.Rectangle(24, 91, 1904, 949); // MdiClient Wnd Rectangle
    #endregion

    #region 고객등록(관리)Page
    // CheckBox - 전체선택
    public Draw.Point 고객등록Page_ChkBoxTotal_ptChkRelM { get; set; } = new Draw.Point(557, 136); // Check Point {X=557,Y=136}

    // GroupBox - 사용여부
    public Draw.Point 고객등록Page_GroupBoxUse_ptChkRelM { get; set; } = new Draw.Point(567, 187); // {X=567,Y=187}
    public Draw.Point 고객등록Page_GroupBoxTot_ptClkRel { get; set; } = new Draw.Point(12, 14); // Check Point {X=12,Y=9}
    public Draw.Point 고객등록Page_GroupBoxUse_ptClkRel { get; set; } = new Draw.Point(81, 14); // Check Point {X=81,Y=14}
    public Draw.Point 고객등록Page_GroupBoxNot_ptClkRel { get; set; } = new Draw.Point(151, 14); // {X=151,Y=14}

    // Pan Wnd
    public string 고객등록Page_PanWnd_sWndName { get; set; } = string.Empty; // WndName
    public Draw.Point 고객등록Page_PanWnd_ptRelM { get; set; } = new Draw.Point(919, 604); // Check Point {X=919,Y=604}

    // 버튼 - 조회
    public string 고객등록Page_BtnSerach_sWndName { get; set; } = "조회";  // WndName
    public Draw.Point 고객등록Page_BtnSerach_ptChkRelM { get; set; } = new Draw.Point(1686, 151); // Check Point {X=1686,Y=151}

    // 버튼 - 엑셀저장
    public string 고객등록Page_BtnExcel_sWndName { get; set; } = "엑셀"; // WndName
    public Draw.Point 고객등록Page_BtnExcel_ptRelM { get; set; } = new Draw.Point(1814, 151); // Check Point {X=1814,Y=153}

    // 버튼 - 닫기
    public string 고객등록Page_BtnClose_sWndName { get; set; } = "닫기(ESC)"; // WndName
    public Draw.Point 고객등록Page_BtnClose_ptRelM { get; set; } = new Draw.Point(1878, 151); // Check Point {X=1878,Y=153}
    #endregion MainWnd

    #region 고객등록Wnd
    // TopWnd
    public string 고객등록Wnd_TopWnd_sWndName { get; set; } = "고객관리";   // WndName

    // 닫기버튼
    public Draw.Point 고객등록Wnd_ptChkRel닫기버튼 { get; set; } = new Draw.Point(409, 58); // {X=409,Y=58}

    // 고객코드
    public Draw.Point 고객등록Wnd_rcChkRel오더번호 { get; set; } = new Draw.Point(120, 100); // Check Point
    public Draw.Rectangle 고객등록Wnd_rcChkRel고객코드 { get; set; } = new Draw.Rectangle(81, 93, 77, 15);

    // 동명
    public Draw.Point 고객등록Wnd_ptChkRel동명 { get; set; } = new Draw.Point(127, 224); // Check Point {X=127,Y=224}
    #endregion

    #region 접수(오더)등록Page
    // TopWnd
    public string 접수등록Page_TopWnd_sWndName { get; set; } = "접수현황";  // WndName

    // 검색범위 - For Find 메인윈도 기준(Test상 필요)
    //public Draw.Point 접수등록Page_SearchDay_ptChkRelStartM = new Draw.Point(361, 135); // Check Point
    //public Draw.Point 접수등록Page_SearchDay_ptChkRelEndM = new Draw.Point(480, 135); // Check Point 

    // StatusBtns - For Find 메인윈도 기준
    public Draw.Point 접수등록Page_StatusBtn_ptChkRel접수M { get; set; } = new Draw.Point(65, 140); // Check Point {X=65,Y=140}
    public Draw.Point 접수등록Page_StatusBtn_ptChkRel배차M { get; set; } = new Draw.Point(141, 140); // Check Point {X=141,Y=140}
    public Draw.Point 접수등록Page_StatusBtn_ptChkRel운행M { get; set; } = new Draw.Point(217, 140); // Check Point {X=217,Y=140}
    public Draw.Point 접수등록Page_StatusBtn_ptChkRel완료M { get; set; } = new Draw.Point(65, 181); // Check Point  {X=65,Y=181}
    public Draw.Point 접수등록Page_StatusBtn_ptChkRel취소M { get; set; } = new Draw.Point(141, 181); // Check Point {X=141,Y=181}
    public Draw.Point 접수등록Page_StatusBtn_ptChkRel전체M { get; set; } = new Draw.Point(217, 181); // Check Point {X=217,Y=181}

    // CommandBtns GroupBox
    public Draw.Point 접수등록Page_CmdBtn_ptChkRel신규M { get; set; } = new Draw.Point(867, 143); // Check Point  {X=867,Y=143}
    public Draw.Point 접수등록Page_CmdBtn_ptChkRel조회M { get; set; } = new Draw.Point(961, 143); // Check Point  {X=961,Y=143}
    public Draw.Point 접수등록Page_CmdBtn_ptChkRel기사M { get; set; } = new Draw.Point(1122, 143); // Check Point {X=1122,Y=143}

    // CallCount
    public Draw.Point 접수등록Page_CallCount_ptChkRel접수M { get; set; } = new Draw.Point(157, 888); // Check Point {X=157,Y=888}
    public Draw.Point 접수등록Page_CallCount_ptChkRel운행M { get; set; } = new Draw.Point(157, 915); // Check Point {X=157,Y=915}
    public Draw.Point 접수등록Page_CallCount_ptChkRel취소M { get; set; } = new Draw.Point(157, 942); // Check Point {X=157,Y=942}
    public Draw.Point 접수등록Page_CallCount_ptChkRel완료M { get; set; } = new Draw.Point(157, 969); // Check Point {X=157,Y=969}
    public Draw.Point 접수등록Page_CallCount_ptChkRel총계M { get; set; } = new Draw.Point(157, 995); // Check Point {X=157,Y=995}

    // 접수 Datagrid
    public Draw.Point 접수등록Page_DG오더_ptCenterRelM { get; set; } = new Draw.Point(300, 550); // Center Point {X=968,Y=550} avoid Loading Panel
    public Draw.Rectangle 접수등록Page_DG오더_rcRel { get; set; } = new Draw.Rectangle(26, 231, 1885, 639); // DataGrid Rectangle X=26,Y=231,Width=1885,Height=639}
    public int 접수등록Page_DG오더_headerHeight { get; set; } = 30; // HeaderRow Height
    public int 접수등록Page_DG오더_emptyRowHeight { get; set; } = 25; // EmptyRow Height
    public int 접수등록Page_DG오더_dataRowHeight { get; set; } = 20; // DataRow Height
    public int 접수등록Page_DG오더_dataGab { get; set; } = 3; // 셀 상하단 gap
    public const int 접수등록Page_DG오더_dataRowCount = 28; // DataRow Count

    // 스크롤 바
    public Draw.Point 접수등록Page_DG오더_ptChkRel수직스크롤M { get; set; } = new Draw.Point(1900, 542); // 스크롤 중간 - MainWnd 기준
    public Draw.Point 접수등록Page_DG오더_ptClkRel페이지Down { get; set; } = new Draw.Point(8, 600); // 스크롤 다운버튼 바로위 - Page다운 스크롤 하기 위해 - VScrollBar기준
    public Draw.Point 접수등록Page_DG오더_ptClkRel페이지Up { get; set; } = new Draw.Point(8, 20); // 스크롤 업버튼 바로아래 - Page업 스크롤 하기 위해 - VScrollBar기준
    public Draw.Point 접수등록Page_DG오더_ptClkRel버튼Down { get; set; } = new Draw.Point(8, 607); // 스크롤 다운버튼 바로위 - Row다운 스크롤 하기 위해 - VScrollBar기준
    public Draw.Point 접수등록Page_DG오더_ptClkRel버튼Up { get; set; } = new Draw.Point(8, 6); // 스크롤 업버튼 바로아래 - Row업 스크롤 하기 위해 - VScrollBar기준

    // 로딩패널
    public Draw.Point 접수등록Page_DG오더_ptChkRelPanL { get; set; } = new Draw.Point(950, 359); // {X=984,Y=590}
    public string 접수등록Page_DG오더_sWndPan { get; set; } = "progressPanel1";   // WndName - Reserved
    #endregion 접수(오더)등록Page

    #region 접수(오더)등록Wnd
    // TopWnd
    public string 접수등록Wnd_TopWnd_sWndName_Reg { get; set; } = "오더접수(신규) ";   // New Regist WndName - Space조심
    public string 접수등록Wnd_TopWnd_sWndStartsWith { get; set; } = "[";   // Update WndName
    public string 접수등록Wnd_TopWnd_sWndContains { get; set; } = "--->";   // Update WndName

    // Header
    public Draw.Point 접수등록Wnd_Header_ptChkRel오더번호 { get; set; } = new Draw.Point(194, 86); // Check Point
    public Draw.Point 접수등록Wnd_Header_ptChkRel오더상태 { get; set; } = new Draw.Point(49, 96); // Check Point

    // 의뢰자
    public Draw.Point 접수등록Wnd_의뢰자_ptChkRel의뢰자 { get; set; } = new Draw.Point(39, 130); // Check Point - 의뢰자그룹의 TopWnd
    public Draw.Point 접수등록Wnd_의뢰자_ptChkRel고객명 { get; set; } = new Draw.Point(133, 174); // Check Point
    public int[] 접수등록Wnd_의뢰자_고객명SonIndex { get; set; } = new int[] { 6, 1 }; // Check Array ChildNum
    public Draw.Point 접수등록Wnd_의뢰자_ptChkRel동명 { get; set; } = new Draw.Point(330, 174); // Check Point
    public int[] 접수등록Wnd_의뢰자_동명SonIndex { get; set; } = new int[] { 26, 1 }; // Check Array ChildNum
    public Draw.Point 접수등록Wnd_의뢰자_ptChkRel전화1 { get; set; } = new Draw.Point(113, 194); // Check Point
    public int[] 접수등록Wnd_의뢰자_전화1SonIndex { get; set; } = new int[] { 10, 1, 1 }; // Check Array ChildNum
    public Draw.Point 접수등록Wnd_의뢰자_ptChkRel전화2 { get; set; } = new Draw.Point(330, 194); // Check Point
    public int[] 접수등록Wnd_의뢰자_전화2SonIndex { get; set; } = new int[] { 24, 1 }; // Check Array ChildNum
    public Draw.Point 접수등록Wnd_의뢰자_ptChkRel부서 { get; set; } = new Draw.Point(133, 214); // Check Point
    public int[] 접수등록Wnd_의뢰자_부서SonIndex { get; set; } = new int[] { 9, 1 }; // Check Array ChildNum
    public Draw.Point 접수등록Wnd_의뢰자_ptChkRel담당 { get; set; } = new Draw.Point(330, 214); // Check Point
    public int[] 접수등록Wnd_의뢰자_담당SonIndex { get; set; } = new int[] { 21, 1 }; // Check Array ChildNum
    public Draw.Point 접수등록Wnd_의뢰자_ptChkRel주소 { get; set; } = new Draw.Point(208, 234); // Check Point
    public Draw.Rectangle 접수등록Wnd_의뢰자_rcChkRel주소 { get; set; } = new Draw.Rectangle(52, 225, 312, 18);
    public Draw.Point 접수등록Wnd_의뢰자_ptChkRel위치 { get; set; } = new Draw.Point(227, 261); // Check Point
    public Draw.Point 접수등록Wnd_의뢰자_ptChkRel적요 { get; set; } = new Draw.Point(227, 293); // Check Point

    // 출발지
    public Draw.Point 접수등록Wnd_출발지_ptChkRel검색 { get; set; } = new Draw.Point(250, 323); // Check Point {X=250,Y=323}
    public Draw.Point 접수등록Wnd_출발지_ptChkRel출발지 { get; set; } = new Draw.Point(55, 322); // Check Point
    public Draw.Point 접수등록Wnd_출발지_ptChkRel고객버튼 { get; set; } = new Draw.Point(27, 364); // Check Point {X=27,Y=364}
    public Draw.Point 접수등록Wnd_출발지_ptChkRel고객명 { get; set; } = new Draw.Point(133, 364); // Check Point
    public int[] 접수등록Wnd_출발지_고객명SonIndex { get; set; } = new int[] { 18, 1 }; // Check Array ChildNum
    public Draw.Point 접수등록Wnd_출발지_ptChkRel동명 { get; set; } = new Draw.Point(330, 384); // Check Point
    public int[] 접수등록Wnd_출발지_동명SonIndex { get; set; } = new int[] { 16, 1 }; // Check Array ChildNum
    public Draw.Point 접수등록Wnd_출발지_ptChkRel전화1 { get; set; } = new Draw.Point(112, 404); // Check Point
    public int[] 접수등록Wnd_출발지_전화1SonIndex { get; set; } = new int[] { 24, 1 }; // Check Array ChildNum
    public Draw.Point 접수등록Wnd_출발지_ptChkRel전화2 { get; set; } = new Draw.Point(330, 404); // Check Point
    public int[] 접수등록Wnd_출발지_전화2SonIndex { get; set; } = new int[] { 9, 1 }; // Check Array ChildNum
    public Draw.Point 접수등록Wnd_출발지_ptChkRel부서 { get; set; } = new Draw.Point(133, 384); // Check Point
    public int[] 접수등록Wnd_출발지_부서SonIndex { get; set; } = new int[] { 14, 1 }; // Check Array ChildNum
    public Draw.Point 접수등록Wnd_출발지_ptChkRel담당 { get; set; } = new Draw.Point(330, 364); // Check Point
    public int[] 접수등록Wnd_출발지_담당SonIndex { get; set; } = new int[] { 12, 1 }; // Check Array ChildNum
    public Draw.Point 접수등록Wnd_출발지_ptChkRel주소 { get; set; } = new Draw.Point(208, 424); // Check Point
    public Draw.Rectangle 접수등록Wnd_출발지_rcChkRel주소 { get; set; } = new Draw.Rectangle(52, 415, 312, 18);
    public Draw.Point 접수등록Wnd_출발지_ptChkRel위치 { get; set; } = new Draw.Point(227, 460); // Check Point

    // 도착지
    public Draw.Point 접수등록Wnd_도착지_ptChkRel검색 { get; set; } = new Draw.Point(250, 501); // Check Point {X=250,Y=501}
    public Draw.Point 접수등록Wnd_도착지_ptChkRel도착지 { get; set; } = new Draw.Point(55, 495); // Check Point
    public Draw.Point 접수등록Wnd_도착지_ptChkRel고객버튼 { get; set; } = new Draw.Point(27, 542); // Check Point {X=27,Y=542}
    public Draw.Point 접수등록Wnd_도착지_ptChkRel고객명 { get; set; } = new Draw.Point(133, 542); // Check Point
    public int[] 접수등록Wnd_도착지_고객명SonIndex { get; set; } = new int[] { 19, 1 }; // Check Array ChildNum
    public Draw.Point 접수등록Wnd_도착지_ptChkRel동명 { get; set; } = new Draw.Point(330, 562); // Check Point
    public int[] 접수등록Wnd_도착지_동명SonIndex { get; set; } = new int[] { 17, 1 }; // Check Array ChildNum
    public Draw.Point 접수등록Wnd_도착지_ptChkRel전화1 { get; set; } = new Draw.Point(112, 582); // Check Point
    public int[] 접수등록Wnd_도착지_전화1SonIndex { get; set; } = new int[] { 25, 1 }; // Check Array ChildNum
    public Draw.Point 접수등록Wnd_도착지_ptChkRel전화2 { get; set; } = new Draw.Point(330, 582); // Check Point
    public int[] 접수등록Wnd_도착지_전화2SonIndex { get; set; } = new int[] { 11, 1 }; // Check Array ChildNum
    public Draw.Point 접수등록Wnd_도착지_ptChkRel부서 { get; set; } = new Draw.Point(133, 562); // Check Point
    public int[] 접수등록Wnd_도착지_부서SonIndex { get; set; } = new int[] { 15, 1 }; // Check Array ChildNum
    public Draw.Point 접수등록Wnd_도착지_ptChkRel담당 { get; set; } = new Draw.Point(330, 542); // Check Point
    public int[] 접수등록Wnd_도착지_담당SonIndex { get; set; } = new int[] { 13, 1 }; // Check Array ChildNum
    public Draw.Point 접수등록Wnd_도착지_ptChkRel주소 { get; set; } = new Draw.Point(208, 602); // Check Point
    public Draw.Rectangle 접수등록Wnd_도착지_rcChkRel주소 { get; set; } = new Draw.Rectangle(52, 593, 312, 18);
    public Draw.Point 접수등록Wnd_도착지_ptChkRel위치 { get; set; } = new Draw.Point(227, 640); // Check Point

    // 예약, SMS, 적요, 공유...
    public Draw.Rectangle 접수등록Wnd_우측상단_rcChkRel예약여부 { get; set; } = new Draw.Rectangle(471, 126, 18, 18); // Check Rect
    public Draw.Rectangle 접수등록Wnd_우측상단_rcChkRel예약일시 { get; set; } = new Draw.Rectangle(492, 126, 183, 18); // Check Rect - {X=493,Y=129,Width=181,Height=12}
    public Draw.Rectangle 접수등록Wnd_우측상단_rcChkRel예약해제 { get; set; } = new Draw.Rectangle(697, 126, 51, 18); // Check Point - {X=698,Y=129,Width=49,Height=12}
    public Draw.Point 접수등록Wnd_우측상단_ptChkRel적요 { get; set; } = new Draw.Point(618, 197);// Check Point
    public Draw.Rectangle 접수등록Wnd_우측상단_rcChkRel공유 { get; set; } = new Draw.Rectangle(471, 252, 18, 18); // Check Rect - X=469,Y=248,Width=118,Height=26
    //public Draw.Point 접수등록Wnd_우측상단_ptChkRel공유 { get; set; } = new Draw.Point(480, 261); // Check Rect - X=469,Y=248,Width=118,Height=26
    public Draw.Rectangle 접수등록Wnd_우측상단_rcChkRel계산서 { get; set; } = new Draw.Rectangle(587, 252, 18, 18); // Check Rect - X=585,Y=248,Width=86,Height=26
    //public Draw.Point 접수등록Wnd_우측상단_ptChkRel계산서 { get; set; } = new Draw.Point(596, 261); // Check Rect - X=585,Y=248,Width=86,Height=26
    public Draw.Rectangle 접수등록Wnd_우측상단_rcChkRel수수무 { get; set; } = new Draw.Rectangle(757, 252, 18, 18); // Check Rect - X=755,Y=248,Width=86,Height=26
    public Draw.Rectangle 접수등록Wnd_우측상단_rcChkRel물품종류 { get; set; } = new Draw.Rectangle(640, 275, 95, 21); // Check Rect - X=638,Y=273,Width=118,Height=25
    
    public Draw.Rectangle 접수등록Wnd_우측상단_rcChkRel차량톤수 { get; set; } = new Draw.Rectangle(696, 323, 50, 21); // Check Rect - X=638,Y=273,Width=118,Height=25
    public Draw.Point 접수등록Wnd_우측상단_ptChkRel차량톤수Open { get; set; } = new Draw.Point(721, 354); // Check - Combo Button (Center X=696+50/2=721, Bottom+10=323+21+10=354)

    public Draw.Rectangle 접수등록Wnd_우측상단_rcChkRel트럭상세 { get; set; } = new Draw.Rectangle(768, 323, 52, 21); // Check Rect - X=638,Y=273,Width=118,Height=25
    public Draw.Point 접수등록Wnd_우측상단_ptChkRel트럭상세Open { get; set; } = new Draw.Point(794, 354); // Check - Combo Button (Center X=768+52/2=794, Bottom+10=323+21+10=354)

    // 요금종류 RadioBtn 그룹 
    public OfrModel_RadioBtn[] 접수등록Wnd_우측상단_요금그룹 { get; set; } = new OfrModel_RadioBtn[]
    {
        new OfrModel_RadioBtn("선불", new Draw.Rectangle(473, 301, 18, 18), new Draw.Rectangle(492, 301, 27, 19)),
        new OfrModel_RadioBtn("착불", new Draw.Rectangle(521, 301, 18, 18), new Draw.Rectangle(540, 301, 27, 19)),
        new OfrModel_RadioBtn("신용", new Draw.Rectangle(569, 301, 18, 18), new Draw.Rectangle(588, 301, 27, 19)),
        new OfrModel_RadioBtn("송금", new Draw.Rectangle(617, 301, 18, 18), new Draw.Rectangle(636, 301, 27, 19)),
        new OfrModel_RadioBtn("수금", new Draw.Rectangle(665, 301, 18, 18), new Draw.Rectangle(684, 301, 27, 19)),
        new OfrModel_RadioBtn("카드", new Draw.Rectangle(713, 301, 18, 18), new Draw.Rectangle(732, 301, 27, 19))
    };

    // 4-4. 차량종류 RadioButton 그룹
    public OfrModel_RadioBtn[] 접수등록Wnd_우측상단_차량그룹 { get; set; } = new OfrModel_RadioBtn[]
    {
        new OfrModel_RadioBtn("오토", new Draw.Rectangle(473, 326, 18, 18), new Draw.Rectangle(492, 326, 27, 19)),
        new OfrModel_RadioBtn("밴", new Draw.Rectangle(526, 326, 18, 18), new Draw.Rectangle(545, 326, 18, 19)),
        new OfrModel_RadioBtn("트럭", new Draw.Rectangle(580, 326, 18, 18), new Draw.Rectangle(599, 326, 27, 19)),
        new OfrModel_RadioBtn("플렉스", new Draw.Rectangle(635, 326, 18, 18), new Draw.Rectangle(654, 326, 40, 19)),
        new OfrModel_RadioBtn("다마", new Draw.Rectangle(473, 345, 18, 18), new Draw.Rectangle(492, 345, 27, 19)),
        new OfrModel_RadioBtn("라보", new Draw.Rectangle(526, 345, 18, 18), new Draw.Rectangle(545, 345, 27, 19)),
        new OfrModel_RadioBtn("지하", new Draw.Rectangle(580, 345, 18, 18), new Draw.Rectangle(599, 345, 27, 19))
    };

    public Draw.Rectangle 접수등록Wnd_우측상단_rcChkRel플럭제외 { get; set; } = new Draw.Rectangle(696, 348, 18, 18); // Check Rect - X=694,Y=345,Width=73,Height=25
    public Draw.Rectangle 접수등록Wnd_우측상단_rcChkRel인수증필 { get; set; } = new Draw.Rectangle(768, 348, 18, 18); // Check Rect - X=766,Y=345,Width=75,Height=25
    public Draw.Rectangle 접수등록Wnd_우측상단_rcChkRel배송지정 { get; set; } = new Draw.Rectangle(471, 372, 19, 19); // Check Rect - X=469,Y=369,Width=51,Height=25
    public Draw.Rectangle 접수등록Wnd_우측상단_rcChkRel당일택배 { get; set; } = new Draw.Rectangle(521, 372, 19, 19); // Check Rect - X=519,Y=369,Width=74,Height=25

    // qothd종류 RadioButton 그룹
    public OfrModel_RadioBtn[] 접수등록Wnd_우측상단_배송그룹 { get; set; } = new OfrModel_RadioBtn[]
    {
        new OfrModel_RadioBtn("편도", new Draw.Rectangle(596, 373, 18, 18), new Draw.Rectangle(615, 373, 27, 19)),
        new OfrModel_RadioBtn("왕복", new Draw.Rectangle(641, 373, 18, 18), new Draw.Rectangle(660, 373, 27, 19)),
        new OfrModel_RadioBtn("경유", new Draw.Rectangle(686, 373, 18, 18), new Draw.Rectangle(705, 373, 27, 19)),
        new OfrModel_RadioBtn("긴급", new Draw.Rectangle(731, 373, 19, 18), new Draw.Rectangle(750, 373, 27, 19))
    };

    public Draw.Rectangle 접수등록Wnd_우측상단_rcChkRel배송조건 { get; set; } = new Draw.Rectangle(784, 371, 36, 21); // Check Rect

    // 요금
    public Draw.Point 접수등록Wnd_요금그룹_ptChkRel요금 { get; set; } = new Draw.Point(450, 405); // Check Point
    public Draw.Rectangle 접수등록Wnd_요금그룹_rcChkRel부가세여부 { get; set; } = new Draw.Rectangle(471, 400, 18, 18); // Check Rect 
    public Draw.Rectangle 접수등록Wnd_요금그룹_rcChkRel부가세액 { get; set; } = new Draw.Rectangle(471, 424, 71, 18); // Check Rect - X=410,Y=421,Width=60,Height=22 (항상 Disable상태라 라벨위치 참조)
    public Draw.Rectangle 접수등록Wnd_요금그룹_rcChkRel기본요금 { get; set; } = new Draw.Rectangle(471, 445, 165, 18); // // Check Rect
    public Draw.Rectangle 접수등록Wnd_요금그룹_rcChkRel추가금액 { get; set; } = new Draw.Rectangle(710, 445, 129, 18); // Check Rect - {X=708,Y=442,Width=133,Height=22}
    public Draw.Rectangle 접수등록Wnd_요금그룹_rcChkRel할인금액 { get; set; } = new Draw.Rectangle(471, 466, 165, 18); // Check Rect - {X=469,Y=463,Width=169,Height=22}
    public Draw.Rectangle 접수등록Wnd_요금그룹_rcChkRel탁송료 { get; set; } = new Draw.Rectangle(710, 466, 129, 18); // Check Rect 
    public Draw.Rectangle 접수등록Wnd_요금그룹_rcChkRel합계금액 { get; set; } = new Draw.Rectangle(471, 487, 165, 18); // Check Rect - (항상 Disable상태라 위를 참조)
    public Draw.Rectangle 접수등록Wnd_요금그룹_rcChkRel기사금액 { get; set; } = new Draw.Rectangle(710, 487, 129, 18); // Check Rect - (항상 Disable상태라 위를 참조)
    public Draw.Rectangle 접수등록Wnd_요금그룹_rcChkRel마일리지 { get; set; } = new Draw.Rectangle(471, 508, 146, 18); // Check Rect 
    public Draw.Rectangle 접수등록Wnd_요금그룹_rcChkRel기사처리비여부 { get; set; } = new Draw.Rectangle(710, 508, 18, 18); // Check Rect 
    public Draw.Rectangle 접수등록Wnd_요금그룹_rcChkRel기사처리비액 { get; set; } = new Draw.Rectangle(732, 508, 107, 18); // Check Rect 

    // 기사
    public Draw.Rectangle 접수등록Wnd_기사그룹_rcChkRel기사번호 { get; set; } = new Draw.Rectangle(532, 579, 37, 18); // Check Rect - (항상 Disable상태라 라벨위치 참조)
    public Draw.Rectangle 접수등록Wnd_기사그룹_rcChkRel기사이름 { get; set; } = new Draw.Rectangle(572, 579, 64, 18); // Check Rect - (항상 Disable상태라 라벨위치 참조)
    public Draw.Rectangle 접수등록Wnd_기사그룹_rcChkRel기사소속 { get; set; } = new Draw.Rectangle(702, 579, 137, 18); // Check Rect - (항상 Disable상태라 라벨위치 참조)
    public Draw.Rectangle 접수등록Wnd_기사그룹_rcChkRel기사타입 { get; set; } = new Draw.Rectangle(532, 600, 104, 18); // Check Rect - (항상 Disable상태라 라벨위치 참조)
    public Draw.Rectangle 접수등록Wnd_기사그룹_rcChkRel기사전화 { get; set; } = new Draw.Rectangle(702, 600, 115, 18); // Check Rect - (항상 Disable상태라 라벨위치 참조)
    public Draw.Rectangle 접수등록Wnd_기사그룹_rcChkRel세금계산서 { get; set; } = new Draw.Rectangle(702, 621, 64, 18); // Check Rect - (항상 Disable상태라 라벨위치 참조)

    // 오더메모
    public Draw.Point 접수등록Wnd_ptChkRel오더메모 { get; set; } = new Draw.Point(655, 655); // Check Point - {X=655,Y=655}

    // 버튼들 - 공용
    public string 접수등록Wnd_버튼그룹_sWndName닫기 { get; set; } = "닫"; // Check Point - 창 확인용

    // 버튼들 - 신규등록 그룹
    public Draw.Point 접수등록Wnd_신규버튼그룹_ptChkRel닫기 { get; set; } = new Draw.Point(889, 663); // Check Point
    public Draw.Point 접수등록Wnd_신규버튼그룹_ptChkRel고객등록 { get; set; } = new Draw.Point(889, 55); // Check Point
    public Draw.Point 접수등록Wnd_신규버튼그룹_ptChkRel접수저장 { get; set; } = new Draw.Point(889, 310); // Check Point {X=889,Y=310}
    public Draw.Point 접수등록Wnd_신규버튼그룹_ptChkRel대기저장 { get; set; } = new Draw.Point(889, 423); // Check Point {X=889,Y=423}

    // 버튼들 - 수정 그룹
    public Draw.Point 접수등록Wnd_수정버튼그룹_ptChkRel닫기 { get; set; } = new Draw.Point(889, 679); // Check Point
    public Draw.Point 접수등록Wnd_신규버튼그룹_ptChkRel고객수정 { get; set; } = new Draw.Point(889, 55); // Check Point
    public Draw.Point 접수등록Wnd_수정버튼그룹_ptChkRel배차 { get; set; } = new Draw.Point(889, 278); // Check Point {X=889,Y=408}
    public Draw.Point 접수등록Wnd_수정버튼그룹_ptChkRel처리완료 { get; set; } = new Draw.Point(889, 358); // Check Point {X=889,Y=408}
    public Draw.Point 접수등록Wnd_수정버튼그룹_ptChkRel대기 { get; set; } = new Draw.Point(889, 403); // Check Point {X=889,Y=408}
    public Draw.Point 접수등록Wnd_수정버튼그룹_ptChkRel주문취소 { get; set; } = new Draw.Point(889, 453); // Check Point {X=889,Y=408}
    public Draw.Point 접수등록Wnd_수정버튼그룹_ptChkRel접수상태 { get; set; } = new Draw.Point(889, 508); // Check Point {X=889,Y=408}
    public Draw.Point 접수등록Wnd_수정버튼그룹_ptChkRel저장 { get; set; } = new Draw.Point(889, 563); // Check Point {X=889,Y=476}

    // 버튼들 - 완료 상태 팝업 (완료 상태일 때 버튼 레이아웃)
    public Draw.Point 접수등록Wnd_완료상태_ptChkRel닫기 { get; set; } = new Draw.Point(889, 679); // TODO: 실제 좌표 확인 필요
    public Draw.Point 접수등록Wnd_완료상태_ptChkRel주문취소 { get; set; } = new Draw.Point(889, 408); // 저장 위치를 취소로 사용
    public Draw.Point 접수등록Wnd_완료상태_ptChkRel저장 { get; set; } = new Draw.Point(889, 476); // TODO: 실제 좌표 확인 필요

    // 공용 콤보박스 내부 행 클릭 포인트
    public Draw.Point[] 접수등록Wnd_Common_ptComboBox = new Draw.Point[]
    {
        new Draw.Point(35, 7),
        new Draw.Point(35, 21),
        new Draw.Point(35, 35),
        new Draw.Point(35, 49),
        new Draw.Point(35, 63),
        new Draw.Point(35, 77),
        new Draw.Point(35, 91),
        new Draw.Point(35, 105),
        new Draw.Point(35, 119),
        new Draw.Point(35, 133),
        new Draw.Point(35, 147),
        new Draw.Point(35, 161),
        new Draw.Point(35, 175),
        new Draw.Point(35, 189),
        new Draw.Point(35, 203),
        new Draw.Point(35, 217),
        new Draw.Point(35, 231),
        new Draw.Point(35, 245),
        new Draw.Point(35, 259),
        new Draw.Point(35, 273),
        new Draw.Point(35, 287),
        new Draw.Point(35, 301),
        new Draw.Point(35, 315),
        new Draw.Point(35, 329),
        new Draw.Point(35, 343),
        new Draw.Point(35, 357),
        new Draw.Point(35, 371),
        new Draw.Point(35, 385),
        new Draw.Point(35, 399),
        new Draw.Point(35, 413),
        new Draw.Point(35, 427),
        new Draw.Point(35, 441),
        new Draw.Point(35, 455),
        new Draw.Point(35, 469),
        new Draw.Point(35, 483),
        new Draw.Point(35, 497),
        new Draw.Point(35, 511),
        new Draw.Point(35, 525),
    };
    #endregion

    #region 이용내역 List(오더접수 윈도의 서브 윈도)
    public string 이용내역List_TopWnd_sWndName { get; set; } = "이용내역 List";   // WndName
    #endregion

    #region 고객검색Wnd
    // TopWnd
    public string 고객검색Wnd_TopWnd_sWndName { get; set; } = "고객검색";  // WndName

    // Btn
    public Draw.Point 고객검색Wnd_버튼_ptChkRel닫기 { get; set; } = new Draw.Point(889, 43); // Check Point{X=889,Y=43}
    public string 고객검색Wnd_버튼_sWndName닫기 { get; set; } = "";
    #endregion
}
#nullable restore