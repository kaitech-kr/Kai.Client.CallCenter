using Draw = System.Drawing;

using Kai.Common.StdDll_Common;

namespace Kai.Client.CallCenter.Networks.NwCargo24s;

#nullable disable
[Serializable]
public class Cargo24sInfo_File
{
    #region App
    public string App_sPredictFolder { get; set; } = @"C:\Cargo24";
    public string App_sExeFileName { get; set; } = "Cargo24.exe";
    #endregion

    #region Splash
    // TopWnd
    public string Splash_TopWnd_sClassName { get; set; } = "TfrmCargoLogin"; // ClassName
    public string Splash_TopWnd_sWndName { get; set; } = "로그인";   // WndName

    public Draw.Point Splash_IdWnd_ptChk { get; set; } = new Draw.Point(272, 266);  // ID Wnd
    public Draw.Point Splash_PwWnd_ptChk { get; set; } = new Draw.Point(272, 316); // PW Wnd
    public Draw.Point Splash_LoginBtn_ptChk { get; set; } = new Draw.Point(450, 285); // Login Btn
    public Draw.Point Splash_CloseBtn_ptChk { get; set; } = new Draw.Point(450, 355); // Close Btn
    #endregion

    #region Main
    // TopWnd
    public string Main_TopWnd_sClassName { get; set; } = "TfrmCargoMain"; // ClassName // ClassName
    public string Main_TopWnd_sWndNameReduct { get; set; } = " ::::: 대한민국대표콜센터";   // WndName - Spase주의

    // MainMenu_Rect (BarMenu와 동일 - 화물24시는 MainMenu=BarMenu)
    public Draw.Rectangle Main_MainMenu_rcRel { get; set; } = new Draw.Rectangle(8, 51, 1920, 27);  // MainMenu_Title Wnd Rectangle

    // BarMenu
    public Draw.Rectangle Main_BarMenu_rcRel { get; set; } = new Draw.Rectangle(8, 51, 1920, 27);  // BarMenu Wnd Rectangle
    public string Main_BarMenu_ClassName { get; set; } = "TAdvPanel";

    // MdiClient
    public Draw.Rectangle Main_MdiClient_rcRel { get; set; } = new Draw.Rectangle(8, 78, 1920, 943); // MdiClient Wnd Rectangle
    public string Main_MdiClient_ClassName { get; set; } = "MDIClient"; // ClassName
    #endregion

    #region 접수(화물)등록Page
    // TopWnd
    public string 접수등록Page_TopWnd_sClassName { get; set; } = "TfrmCargoOrder"; // ClassName
    public string 접수등록Page_TopWnd_sWndName { get; set; } = "오더접수관리";   // WndName

    public int 접수등록Page_PushBtn_FocusBright { get; set; } = 233; // Brightness
    public Draw.Point 접수등록Page_SearchRange_ptChkRelFrom { get; set; } = new Draw.Point(340, 140); // Check Point - From Date(임시)

    // StatusBtns - For Find
    public Draw.Point 접수등록Page_StatusBtn_ptChkRel접수 { get; set; } = new Draw.Point(60, 130); // Check Point
    public string 접수등록Page_StatusBtn_sWndName접수 { get; set; } = "접수"; // Check Window Name

    public Draw.Point 접수등록Page_StatusBtn_ptChkRel운행 { get; set; } = new Draw.Point(60, 155); // Check Point
    public string 접수등록Page_StatusBtn_sWndName운행 { get; set; } = "운행"; // Check Window Name

    public Draw.Point 접수등록Page_StatusBtn_ptChkRel취소 { get; set; } = new Draw.Point(60, 180); // Check Point
    public string 접수등록Page_StatusBtn_sWndName취소 { get; set; } = "취소"; // Check Window Name

    public Draw.Point 접수등록Page_StatusBtn_ptChkRel완료 { get; set; } = new Draw.Point(130, 130); // Check Point
    public string 접수등록Page_StatusBtn_sWndName완료 { get; set; } = "완료"; // Check Window Name

    public Draw.Point 접수등록Page_StatusBtn_ptChkRel정산 { get; set; } = new Draw.Point(130, 155); // Check Point
    public string 접수등록Page_StatusBtn_sWndName정산 { get; set; } = "정산"; // Check Window Name

    public Draw.Point 접수등록Page_StatusBtn_ptChkRel전체 { get; set; } = new Draw.Point(130, 180); // Check Point
    public string 접수등록Page_StatusBtn_sWndName전체 { get; set; } = "전체"; // Check Window Name

    // 오늘하루동안 감추기
    public Draw.Point 접수등록Page_안내문_ptChkRel오늘하루동안감추기 { get; set; } = new Draw.Point(717, 506); // Check Point
    public string 접수등록Page_안내문_sWndName오늘하루동안감추기 { get; set; } = "오늘하루동안 감추기"; // Check Window Name

    // CommandBtns GroupBox
    public Draw.Point 접수등록Page_CmdBtn_ptChkRel신규M { get; set; } = new Draw.Point(818, 96); // Check Point
    public string 접수등록Page_CmdBtn_sWndName신규 { get; set; } = "신규오더(F3)"; // Check Window Name

    public Draw.Point 접수등록Page_CmdBtn_ptChkRel조회M { get; set; } = new Draw.Point(93, 96); // Check Point
    public string 접수등록Page_CmdBtn_sWndName조회 { get; set; } = "조회(F2)"; // Check Window Name
    public Draw.Point 접수등록Page_CmdBtn_ptChkRel조회L { get; set; } = new Draw.Point(5, 15);  // Check Point For Brightness

    // 접수 Datagrid
    public Draw.Point 접수등록Page_DG오더_ptChkRel { get; set; } = new Draw.Point(300, 547); // Check Point - Center(968, 547) From Loading Panel(rkqus)
    public Draw.Rectangle 접수등록Page_DG오더_rcRel { get; set; } = new Draw.Rectangle(10, 229, 1916, 637); // Check Point

    public string 접수등록Page_DG오더_sClassName { get; set; } = "TRealDBGrid"; // Check Window Name
    public int 접수등록Page_DG오더_headerHeight { get; set; } = 32; // HeaderRow Height
    public int 접수등록Page_DG오더_rowHeight { get; set; } = 23; // DataRow Height
    public int 접수등록Page_DG오더_rowCount { get; set; } = 25; // DataRow Count
    public string[] 접수등록Page_DG오더_colOrgTexts { get; set; } = new string[]
    {
        "0", "화물번호", "SMS", "상태", "처리시간", "공유", "혼적", "고객명", "고객전화", "상차지",
        "하차지", "화물정보", "운송료", "수수료", "요금구분", "톤수", "차종", "차량번호", "차주이름", "차주전화",
        "적재옵션", "차량종류", "차량톤수", "차주사업자번호", "게산서발행일", "계산서금액", "도착지연락처", "하차일시", "접수경로", "등록자전화번호",
        "등록자명", "화주명", "차주실적지위", "차주사업자명", "대표자명", "산재보험료차주분", "송금할운임", "산재적용", "상차일"
    };
    public Draw.Point 접수등록Page_DG오더_ptClkRel스크롤PageDown { get; set; } = new Draw.Point(1905, 600); // 스크롤 다운버튼 바로위 - Page다운 스크롤 하기 위해 - Datagrid기준
    public Draw.Point 접수등록Page_DG오더_ptClkRel스크롤StepDown { get; set; } = new Draw.Point(1905, 610); // 스크롤 다운버튼 바로위 - Page다운 스크롤 하기 위해 - Datagrid기준
    public Draw.Point 접수등록Page_DG오더_ptClkRel스크롤Up { get; set; } = new Draw.Point(1905, 16); // 스크롤 업버튼 바로아래 - Page업 스크롤 하기 위해 - Datagrid기준

    public Draw.Point 접수등록Page_리스트항목_ptChkRel순서저장 { get; set; } = new Draw.Point(90, 216); // Check Point {X=90,Y=216}
    public string 접수등록Page_리스트항목_sWndName순서저장 { get; set; } = "리스트항목 순서 저장"; // Check Window Name

    public Draw.Point 접수등록Page_리스트항목_ptChkRel원래대로 { get; set; } = new Draw.Point(260, 216); // Check Point {X=260,Y=216}
    public string 접수등록Page_리스트항목_sWndName원래대로 { get; set; } = "리스트항목  원래대로"; // Check Window Name
    #endregion

    #region 접수(화물)등록Wnd
    // TopWnd
    public string 접수등록Wnd_TopWnd_sClassName { get; set; } = "TfrmCargoOrderIns"; // ClassName
    public string 접수등록Wnd_TopWnd_sWndName { get; set; } = "화물등록";   // WndName

    // PushButtons
    public Draw.Point 접수등록Wnd_CmnBtn_ptRel닫기 { get; set; } = new Draw.Point(1074, 453); // 닫기 버튼
    public Draw.Point 접수등록Wnd_CmnBtn_ptRel대기저장 { get; set; } = new Draw.Point(1074, 223); // 닫기 버튼
    public Draw.Point 접수등록Wnd_CmnBtn_ptRel접수저장 { get; set; } = new Draw.Point(1074, 396); // 닫기 버튼

    // 의뢰자
    public Draw.Point 접수등록Wnd_의뢰자_ptRel고객명 { get; set; } = new Draw.Point(186, 90); // 의뢰자 이름
    public Draw.Point 접수등록Wnd_의뢰자_ptRel고객전화 { get; set; } = new Draw.Point(342, 90); // 의뢰자 전화
    public Draw.Point 접수등록Wnd_의뢰자_ptRel담당자 { get; set; } = new Draw.Point(158, 153); // 의뢰자 담당명

    // 상차지
    public Draw.Point 접수등록Wnd_상차지_ptRel조회버튼 { get; set; } = new Draw.Point(262, 284); // 의뢰자 조회버튼
    public Draw.Point 접수등록Wnd_상차지_ptRel위치 { get; set; } = new Draw.Point(251, 350); // 상차지 위치
    public Draw.Point 접수등록Wnd_상차지_ptRel고객명 { get; set; } = new Draw.Point(176, 386); // 상차지 고객명
    public Draw.Point 접수등록Wnd_상차지_ptRel전화 { get; set; } = new Draw.Point(373, 386); // 상차지 전화
    public Draw.Point 접수등록Wnd_상차지_ptRel부서명 { get; set; } = new Draw.Point(176, 413); // 상차지 부서명
    public Draw.Point 접수등록Wnd_상차지_ptRel담당자 { get; set; } = new Draw.Point(373, 413); // 상차지 담당자

    // 하차지
    public Draw.Point 접수등록Wnd_하차지_ptRel조회버튼 { get; set; } = new Draw.Point(262, 457); // 의뢰자 조회버튼
    public Draw.Point 접수등록Wnd_하차지_ptRel위치 { get; set; } = new Draw.Point(251, 521); // 하차지 위치
    public Draw.Point 접수등록Wnd_하차지_ptRel고객명 { get; set; } = new Draw.Point(176, 557); // 하차지 고객명
    public Draw.Point 접수등록Wnd_하차지_ptRel전화 { get; set; } = new Draw.Point(373, 558); // 하차지 전화
    public Draw.Point 접수등록Wnd_하차지_ptRel부서명 { get; set; } = new Draw.Point(176, 584); // 하차지 부서명
    public Draw.Point 접수등록Wnd_하차지_ptRel담당자 { get; set; } = new Draw.Point(373, 584); // 하차지 담당자

    // 배송타입 - CheckBoxes
    public Draw.Point 접수등록Wnd_배송ChkBoxes_ptRel공유 { get; set; } = new Draw.Point(730, 91); // 공유
    public Draw.Point 접수등록Wnd_배송ChkBoxes_ptRel중요오더 { get; set; } = new Draw.Point(838, 91); // 중요오더
    public Draw.Point 접수등록Wnd_배송ChkBoxes_ptRel예약 { get; set; } = new Draw.Point(934, 91); // 예약
    public Draw.Point 접수등록Wnd_배송ChkBoxes_ptRel긴급 { get; set; } = new Draw.Point(731, 113); // 긴급
    public Draw.Point 접수등록Wnd_배송ChkBoxes_ptRel왕복 { get; set; } = new Draw.Point(824, 113); // 왕복
    public Draw.Point 접수등록Wnd_배송ChkBoxes_ptRel경유 { get; set; } = new Draw.Point(934, 113); // 경유

    // 차량톤수
    public Draw.Point 접수등록Wnd_톤수RdoBtns_ptRel0C3 { get; set; } = new Draw.Point(570, 150); // 0.3t
    public Draw.Point 접수등록Wnd_톤수RdoBtns_ptRel0C5 { get; set; } = new Draw.Point(630, 150); // 0.5t
    public Draw.Point 접수등록Wnd_톤수RdoBtns_ptRel1C0 { get; set; } = new Draw.Point(690, 150); // 1t
    public Draw.Point 접수등록Wnd_톤수RdoBtns_ptRel1C4 { get; set; } = new Draw.Point(752, 150); // 1.4t
    public Draw.Point 접수등록Wnd_톤수RdoBtns_ptRel2C5 { get; set; } = new Draw.Point(810, 150); // 2.5t
    public Draw.Point 접수등록Wnd_톤수RdoBtns_ptRel3C5 { get; set; } = new Draw.Point(570, 170); // 3.5t
    public Draw.Point 접수등록Wnd_톤수RdoBtns_ptRel4 { get; set; } = new Draw.Point(630, 170); // 4t
    public Draw.Point 접수등록Wnd_톤수RdoBtns_ptRel5 { get; set; } = new Draw.Point(690, 170); // 5t
    public Draw.Point 접수등록Wnd_톤수RdoBtns_ptRel8 { get; set; } = new Draw.Point(752, 170); // 8t
    public Draw.Point 접수등록Wnd_톤수RdoBtns_ptRel9C5 { get; set; } = new Draw.Point(810, 170); // 9.5t
    public Draw.Point 접수등록Wnd_톤수RdoBtns_ptRel11 { get; set; } = new Draw.Point(570, 190); // 11t
    public Draw.Point 접수등록Wnd_톤수RdoBtns_ptRel14 { get; set; } = new Draw.Point(630, 190); // 14t
    public Draw.Point 접수등록Wnd_톤수RdoBtns_ptRel15 { get; set; } = new Draw.Point(690, 190); // 15t
    public Draw.Point 접수등록Wnd_톤수RdoBtns_ptRel18 { get; set; } = new Draw.Point(752, 190); // 18t
    public Draw.Point 접수등록Wnd_톤수RdoBtns_ptRel25 { get; set; } = new Draw.Point(810, 190); // 25t

    public Draw.Point 접수등록Wnd_톤수Edit_ptRel몇톤 { get; set; } = new Draw.Point(891, 180); // 몇톤
    public Draw.Point 접수등록Wnd_톤수ChkBox_ptRel이하 { get; set; } = new Draw.Point(966, 180); // 이하

    public Draw.Point 접수등록Wnd_차종Combo_ptRel차종확인 { get; set; } = new Draw.Point(602, 216); // 확인
    public Draw.Point 접수등록Wnd_Combo_ptRel전자세금게산서 { get; set; } = new Draw.Point(863, 216); // 전자세금게산서

    public Draw.Point 접수등록Wnd_옵션ChkBox_ptRel독차 { get; set; } = new Draw.Point(571, 245); // 독차
    public Draw.Point 접수등록Wnd_옵션ChkBox_ptRel혼적 { get; set; } = new Draw.Point(630, 245); // 혼적
    public Draw.Point 접수등록Wnd_옵션Combo_ptRel혼적길이 { get; set; } = new Draw.Point(712, 245); // 혼적길이

    // 운송비구분
    public Draw.Point 접수등록Wnd_운송비RdoBtns_ptRel선착불 { get; set; } = new Draw.Point(578, 273); // 선착불
    public Draw.Point 접수등록Wnd_운송비RdoBtns_ptRel수수료확인 { get; set; } = new Draw.Point(660, 273); // 수수료확인
    public Draw.Point 접수등록Wnd_운송비RdoBtns_ptRel인수증 { get; set; } = new Draw.Point(743, 273); // 인수증
    public Draw.Point 접수등록Wnd_운송비RdoBtns_ptRel카드 { get; set; } = new Draw.Point(804, 273); // 카드

    public Draw.Point 접수등록Wnd_운송비Edit_ptRel합계 { get; set; } = new Draw.Point(593, 304); // 합계
    public Draw.Point 접수등록Wnd_운송비Edit_ptRel운송료 { get; set; } = new Draw.Point(779, 304); // 운송료
    public Draw.Point 접수등록Wnd_운송비Edit_ptRel수수료 { get; set; } = new Draw.Point(962, 304); // 수수료
    public Draw.Point 접수등록Wnd_운송비Edit_ptRel할인액 { get; set; } = new Draw.Point(593, 333); // 할인액

    public Draw.Point 접수등록Wnd_상차정보RdoBtns_ptRel없음 { get; set; } = new Draw.Point(577, 360); // 없음
    public Draw.Point 접수등록Wnd_상차정보RdoBtns_ptRel지게차 { get; set; } = new Draw.Point(655, 360); // 지게차
    public Draw.Point 접수등록Wnd_상차정보RdoBtns_ptRel수작업 { get; set; } = new Draw.Point(728, 360); // 수작업
    public Draw.Point 접수등록Wnd_상차정보RdoBtns_ptRel호이스트 { get; set; } = new Draw.Point(809, 360); // 호이스트
    public Draw.Point 접수등록Wnd_상차정보RdoBtns_ptRel크레인 { get; set; } = new Draw.Point(895, 360); // 크레인
    public Draw.Point 접수등록Wnd_상차정보RdoBtns_ptRel컨베이어 { get; set; } = new Draw.Point(967, 360); // 컨베이어

    public Draw.Point 접수등록Wnd_상차정보ChkBoxes_ptRel지금 { get; set; } = new Draw.Point(572, 384); // 지금
    public Draw.Point 접수등록Wnd_상차정보ChkBoxes_ptRel당일 { get; set; } = new Draw.Point(624, 384); // 당일
    public Draw.Point 접수등록Wnd_상차정보ChkBoxes_ptRel내일 { get; set; } = new Draw.Point(678, 384); // 내일

    public Draw.Point 접수등록Wnd_하차정보RdoBtns_ptRel없음 { get; set; } = new Draw.Point(577, 411); // 없음
    public Draw.Point 접수등록Wnd_하차정보RdoBtns_ptRel지게차 { get; set; } = new Draw.Point(655, 411); // 지게차
    public Draw.Point 접수등록Wnd_하차정보RdoBtns_ptRel수작업 { get; set; } = new Draw.Point(728, 411); // 수작업
    public Draw.Point 접수등록Wnd_하차정보RdoBtns_ptRel호이스트 { get; set; } = new Draw.Point(809, 411); // 호이스트
    public Draw.Point 접수등록Wnd_하차정보RdoBtns_ptRel크레인 { get; set; } = new Draw.Point(895, 411); // 크레인
    public Draw.Point 접수등록Wnd_하차정보RdoBtns_ptRel컨베이어 { get; set; } = new Draw.Point(967, 411); // 컨베이어

    public Draw.Point 접수등록Wnd_하차정보ChkBoxes_ptRel당일 { get; set; } = new Draw.Point(572, 435); // 당일
    public Draw.Point 접수등록Wnd_하차정보ChkBoxes_ptRel내일 { get; set; } = new Draw.Point(627, 435); // 내일
    public Draw.Point 접수등록Wnd_하차정보ChkBoxes_ptRel월착 { get; set; } = new Draw.Point(681, 435); // 월착
    public Draw.Point 접수등록Wnd_하차정보ChkBoxes_ptRel당착내착 { get; set; } = new Draw.Point(598, 456); // 당착/내착

    public Draw.Point 접수등록Wnd_추가정보Edit_ptRel { get; set; } = new Draw.Point(770, 488); // 추가정보
    #endregion

    #region 주소/고객 검색창
    // TopWnd
    public string 주소검색Wnd_TopWnd_sClassName { get; set; } = "TfrmAddrSearchXP"; // ClassName
    public string 주소검색Wnd_TopWnd_sWndName { get; set; } = "주소/고객 검색";   // WndName

    public Draw.Point 주소검색Wnd_Search_ptRel검색어 { get; set; } = new Draw.Point(275, 48); // 검색 EditBox
    public Draw.Point 주소검색Wnd_Search_ptRel조회버튼 { get; set; } = new Draw.Point(525, 48); // 검색 버튼
    public Draw.Point 주소검색Wnd_Search_ptRel닫기버튼 { get; set; } = new Draw.Point(750, 48); // 닫기 버튼

    public Draw.Point 주소검색Wnd_TabCtrl_ptRelChk { get; set; } = new Draw.Point(330, 120); // TabCtrl
    public Draw.Point 주소검색Wnd_TabCtrl_ptRel인터넷검색결과 { get; set; } = new Draw.Point(158, 8); // 인터넷검색결과 TabControl

    public Draw.Point 주소검색Wnd_Datagrid_ptRelChk { get; set; } = new Draw.Point(407, 262); // 고객검색결과 Datagrid
    public Draw.Point 주소검색Wnd_Datagrid_ptRelFirstRow { get; set; } = new Draw.Point(407, 262); // 고객검색결과 Datagrid - First Row
    public Draw.Rectangle 주소검색Wnd_Datagrid_rcRelFirstRow { get; set; } = new Draw.Rectangle(2, 23, 18, 17); // FirstRow/TernNo Datagrid Rectangle
    #endregion

    ////#region 고객등록Page
    ////// TopWnd
    ////public string 고객등록Page_TopWnd_sClassName { get; set; } = string.Empty; // ClassName
    ////public string 고객등록Page_TopWnd_sWndName { get; set; } = string.Empty;   // WndName

    ////// CheckBox - 전체선택
    ////public string 고객등록Page_ChkBoxTotal_sClassName { get; set; } = string.Empty; // ClassName
    ////public Draw.Point 고객등록Page_ChkBoxTotal_ptCheckRel { get; set; } = StdUtil.s_ptDrawInvalid; // Check Point

    ////// GroupBox - 사용여부
    ////public string 고객등록Page_GroupBoxUse_sClassName { get; set; } = string.Empty; // ClassName
    ////public Draw.Point 고객등록Page_GroupBoxUse_ptCheckRel { get; set; } = StdUtil.s_ptDrawInvalid; // Check Point
    ////public Draw.Point 고객등록Page_GroupBoxUse_ptClickRel { get; set; } = StdUtil.s_ptDrawInvalid; // Click Point

    ////// 버튼 - 조회
    ////public string 고객등록Page_BtnSerach_sWndName { get; set; } = string.Empty; // WndName
    ////public Draw.Point 고객등록Page_BtnSerach_ptCheckRel { get; set; } = StdUtil.s_ptDrawInvalid; // Check Point

    ////// Pan Wnd
    ////public string 고객등록Page_PanWnd_sClassName { get; set; } = string.Empty; // ClassName
    ////public string 고객등록Page_PanWnd_sWndName { get; set; } = string.Empty; // WndName
    ////public Draw.Point 고객등록Page_PanWnd_ptCheckRel { get; set; } = StdUtil.s_ptDrawInvalid; // Check Point

    ////// 버튼 - 엑셀저장
    ////public string 고객등록Page_BtnExcel_sWndName { get; set; } = string.Empty; // WndName
    ////public Draw.Point 고객등록Page_BtnExcel_ptCheckRel { get; set; } = StdUtil.s_ptDrawInvalid; // Check Point

    ////// 버튼 - 닫기
    ////public string 고객등록Page_BtnClose_sWndName { get; set; } = string.Empty; // WndName
    ////public Draw.Point 고객등록Page_BtnClose_ptCheckRel { get; set; } = StdUtil.s_ptDrawInvalid; // Check Point
    ////#endregion

    //#region 안내문Page
    //public string 안내문Page_TopWnd_sClassName { get; set; } = "TfrmHomePage"; // ClassName
    //public string 안내문Page_TopWnd_sWndName { get; set; } = "안내문";   // WndName
    //#endregion
}
#nullable restore
