using Kai.Client.CallCenter.Classes;
using Kai.Common.StdDll_Common;
using System.Windows.Controls;
using Draw = System.Drawing;

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
    public Draw.Rectangle 접수등록Page_접수_상차지권역_rcChkRelS { get; set; } = new Draw.Rectangle(106, 48, 101, 20); // {X=105,Y=45,Width=103,Height=26}
    public Draw.Point 접수등록Page_접수_상차지주소_ptChkRelS { get; set; } = new Draw.Point(332, 58); // 

    // 하차지
    public Draw.Rectangle 접수등록Page_접수_하차지권역_rcChkRelS { get; set; } = new Draw.Rectangle(106, 79, 101, 20); // {X=105,Y=76,Width=103,Height=26}
    public Draw.Point 접수등록Page_접수_하차지주소_ptChkRelS { get; set; } = new Draw.Point(332, 90); // {X=332,Y=90}

    // 화물정보
    public Draw.Point 접수등록Page_접수_화물정보_ptChkRelS { get; set; } = new Draw.Point(375, 121); // {X=375,Y=121}

    // 운임
    public Draw.Point 접수등록Page_접수_총운임_ptChkRelS { get; set; } = new Draw.Point(147, 153); // {X=147,Y=153}
    public Draw.Point 접수등록Page_접수_수수료_ptChkRelS { get; set; } = new Draw.Point(331, 153); // {X=331,Y=153}

    // 차량정보
    public Draw.Rectangle 접수등록Page_접수_톤수_rcChkRelS { get; set; } = new Draw.Rectangle(108, 176, 80, 20); // {X=105,Y=173,Width=103,Height=26}
    public Draw.Rectangle 접수등록Page_접수_차종_rcChkRelS { get; set; } = new Draw.Rectangle(278, 176, 89, 20); // {X=275,Y=173,Width=112,Height=26}
    public Draw.Rectangle 접수등록Page_접수_대수_rcChkRelS { get; set; } = new Draw.Rectangle(436, 176, 44, 20); // {X=433,Y=173,Width=67,Height=26}
    public Draw.Rectangle 접수등록Page_접수_결재_rcChkRelS { get; set; } = new Draw.Rectangle(552, 176, 72, 20); // {X=549,Y=173,Width=95,Height=26}

    // 차량톤수
    public CommonModel_ComboBox[] 접수등록Page_접수_톤수Open = new CommonModel_ComboBox[]
    {
        new CommonModel_ComboBox("", "미지정", new Draw.Point(45, 8)), // 미지정
        new CommonModel_ComboBox("다마", "0.3t", new Draw.Point(45, 30)), // 0.3t
        new CommonModel_ComboBox("라보", "0.5t", new Draw.Point(45, 52)), // 0.5t
        new CommonModel_ComboBox("1t", "1t", new Draw.Point(45, 74)), // 1t
        //new CommonModel_ComboBox("", "1t초장축", new Draw.Point(45, 96)), // 1t초장축

        new CommonModel_ComboBox("1.4t", "1.4t", new Draw.Point(45, 118)), // 1.4t
        //new CommonModel_ComboBox("", "1.4t초장축", new Draw.Point(45, 140)), // 1.4t초장축
        new CommonModel_ComboBox("2.5t", "2.5t", new Draw.Point(45, 162)), // 2.5t
        new CommonModel_ComboBox("3.5t", "3.5t", new Draw.Point(45, 184)), // 3.5t
        //new CommonModel_ComboBox("", "3.5t광폭", new Draw.Point(45, 206)), // 3.5t광폭

        //new CommonModel_ComboBox("", "4t", new Draw.Point(45, 228)), // 4t
        new CommonModel_ComboBox("5t", "5t", new Draw.Point(45, 250)), // 5t
        //new CommonModel_ComboBox("", "5t플러스", new Draw.Point(45, 272)), // 5t플러스
        //new CommonModel_ComboBox("", "5t축", new Draw.Point(45, 294)), // 5t축
        //new CommonModel_ComboBox("", "5t플축", new Draw.Point(45, 316)), // 5t플축

        new CommonModel_ComboBox("8t", "8t", new Draw.Point(45, 338)), // 8t
        new CommonModel_ComboBox("", "9.5t", new Draw.Point(45, 360)), // 9.5t
        new CommonModel_ComboBox("11t", "11t", new Draw.Point(45, 382)), // 11t
        new CommonModel_ComboBox("14t", "14t", new Draw.Point(45, 404)), // 14t
        //new CommonModel_ComboBox("", "16t", new Draw.Point(45, 426)), // 16t

        new CommonModel_ComboBox("18t", "18t", new Draw.Point(45, 448)), // 18t
        new CommonModel_ComboBox("", "22t", new Draw.Point(45, 470)), // 22t
        new CommonModel_ComboBox("25t", "25t", new Draw.Point(45, 492)), // 25t
    };

    public CommonModel_ComboBox[] 접수등록Page_접수_차종Open = new CommonModel_ComboBox[]
    {
          new CommonModel_ComboBox("전체", "차종무관", new Draw.Point(45, 8)),    // 차종무관
          new CommonModel_ComboBox("카고", "카고", new Draw.Point(45, 30)),           // 카고
          new CommonModel_ComboBox("윙바디", "윙바디", new Draw.Point(45, 52)),         // 윙바디
          new CommonModel_ComboBox("탑", "탑", new Draw.Point(45, 74)),             // 탑
          new CommonModel_ComboBox("냉장탑", "냉장탑", new Draw.Point(45, 96)),         // 냉장탑
       
          new CommonModel_ComboBox("카고/윙", "카고/윙", new Draw.Point(45, 118)),       // 카고/윙
          //new CommonModel_ComboBox("", "윙바디/탑", new Draw.Point(45, 140)),     // 윙바디/탑
          new CommonModel_ComboBox("리프트카고", "리프트", new Draw.Point(45, 162)),        // 리프트
          new CommonModel_ComboBox("리프트윙", "리프트윙", new Draw.Point(45, 184)),      // 리프트윙
          new CommonModel_ComboBox("리프트호루", "호로리프트", new Draw.Point(45, 206)),    // 호로리프트
       
          new CommonModel_ComboBox("플러스윙", "윙플러스", new Draw.Point(45, 228)),      // 윙플러스
          new CommonModel_ComboBox("호루", "호로", new Draw.Point(45, 250)),          // 호로
          new CommonModel_ComboBox("냉동탑", "냉동탑", new Draw.Point(45, 272)),        // 냉동탑
          new CommonModel_ComboBox("리프트탑", "탑리프트", new Draw.Point(45, 294)),      // 탑리프트
          new CommonModel_ComboBox("츄레라", "추레라", new Draw.Point(45, 316)),        // 추레라
       
          new CommonModel_ComboBox("다마", "다마스", new Draw.Point(45, 338)),        // 다마스
          new CommonModel_ComboBox("라보", "라보", new Draw.Point(45, 360)),          // 라보
          new CommonModel_ComboBox("리프트카고", "카고리프트", new Draw.Point(45, 382)),    // 카고리프트
          new CommonModel_ComboBox("무진동", "무진동", new Draw.Point(45, 404)),        // 무진동
          new CommonModel_ComboBox("냉장윙", "냉장윙", new Draw.Point(45, 426)),        // 냉장윙
       
          new CommonModel_ComboBox("초장축", "초장축", new Draw.Point(45, 448)),        // 초장축
    };

    public CommonModel_ComboBox[] 접수등록Page_접수_결재Open = new CommonModel_ComboBox[] // 화물24시에 맞게 조정
    {
          new CommonModel_ComboBox("신용", "인수증", new Draw.Point(45, 8)),    // 인수증
          new CommonModel_ComboBox("카드", "인수증", new Draw.Point(45, 8)),    // 인수증
          new CommonModel_ComboBox("송금", "인수증", new Draw.Point(45, 8)),    // 인수증

          new CommonModel_ComboBox("선불", "선착불", new Draw.Point(45, 30)),   // 선착불
          new CommonModel_ComboBox("착불", "선착불", new Draw.Point(45, 30)),   // 선착불

          //new CommonModel_ComboBox("착불", "착불", new Draw.Point(45, 52)),   // 착불
          //new CommonModel_ComboBox("선불", "선불", new Draw.Point(45, 74)),   // 선불
          //new CommonModel_ComboBox("카드", "카드", new Draw.Point(45, 96)),   // 카드   
    };

    // 화물중량
    public Draw.Point 접수등록Page_접수_화물중량_ptChkRelS { get; set; } = new Draw.Point(156, 216); // {X=156,Y=216}

    // 구분
    public Draw.Rectangle 접수등록Page_구분_독차Part_rcChkRelM { get; set; } = new Draw.Rectangle(767, 19, 16, 16); // {X=767,Y=15,Width=55,Height=26}
    public Draw.Rectangle 접수등록Page_구분_혼적Part_rcChkRelM { get; set; } = new Draw.Rectangle(845, 19, 16, 16); // {X=845,Y=15,Width=55,Height=26}
    public Draw.Rectangle 접수등록Page_구분_긴급Part_rcChkRelM { get; set; } = new Draw.Rectangle(1093, 20, 16, 16); // {X=1093,Y=16,Width=55,Height=26}
    public Draw.Rectangle 접수등록Page_구분_왕복Part_rcChkRelM { get; set; } = new Draw.Rectangle(1154, 20, 16, 16); // {X=1154,Y=16,Width=55,Height=26}
    public Draw.Rectangle 접수등록Page_구분_경유Part_rcChkRelM { get; set; } = new Draw.Rectangle(1218, 20, 16, 16); // {X=1218,Y=16,Width=55,Height=26}

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
