using Draw = System.Drawing;

using Kai.Common.StdDll_Common.StdWin32;

namespace Kai.Client.CallCenter.Networks.NwOnecalls;
#nullable disable
public class OnecallInfo_Mem
{
    #region 1개만 있고 사용빈도가 있는 Wnd, Page는 미리 할당한다 - 편의성
    public List<string> DelWndNames = new List<string> // 삭제된 윈도우 이름들
    {
        //"]님으로 부터의 메세지",
    };
    public SplashWnd Splash = null;
    public MainWnd Main = null;
    public RcptRegPage RcptPage = null;
    //public RcptRegWnd RcptWnd = null;

    public OnecallInfo_Mem()
    {
        Splash = new SplashWnd();
        Main = new MainWnd();
        RcptPage = new RcptRegPage();
    }
    #endregion

    #region SplashWnd
    public class SplashWnd
    {
        // TopWnd
        public IntPtr TopWnd_hWnd;  // hWnd
        public uint TopWnd_uProcessId = 0; // ProcessId
        public uint TopWnd_uThreadId = 0;  // ThreadId

        // Sons
        public IntPtr IdWnd_hWnd;  // hWnd
        public IntPtr PwWnd_hWnd;  // hWnd 
    }
    #endregion

    #region MainWnd
    public class MainWnd
    {
        // TopWnd
        public IntPtr TopWnd_hWnd;  // hWnd

        // Sons
        //public IntPtr CloseBtn_hWnd; // hWnd

        // ListSonWnd
        public List<StdCommon32_WndInfo> FirstLayer_ChildWnds;
        public StdCommon32_WndInfo WndInfo_MainMenu;
        public StdCommon32_WndInfo WndInfo_BarMenu;
        public StdCommon32_WndInfo WndInfo_MdiClient;
    }
    #endregion


    #region 접수(화물)등록Page
    public class RcptRegPage
    {
        #region TopWnd
        // 최상위
        public IntPtr TopWnd_hWnd;  // hWnd 
        #endregion

        #region 접수영역
        // 최상위 - 접수영역
        public IntPtr 접수섹션_hWndTop;  // hWnd

        // 버튼들
        public IntPtr 접수섹션_hWnd신규버튼;  // 버튼
        public IntPtr 접수섹션_hWnd저장버튼;  // 버튼
        public IntPtr 접수섹션_hWnd취소버튼;  // 버튼
        public IntPtr 접수섹션_hWnd복사버튼;  // 버튼

        // 상차지
        public IntPtr 접수섹션_hWnd상차지권역; // Edit
        public IntPtr 접수섹션_hWnd상차지주소; // Edit

        // 하차지
        public IntPtr 접수섹션_hWnd하차지권역; // Edit
        public IntPtr 접수섹션_hWnd하차지주소; // Edit 

        // 화물정보
        public IntPtr 접수섹션_hWnd화물정보; // Edit

        // 운임
        public IntPtr 접수섹션_hWnd총운임; // Edit
        public IntPtr 접수섹션_hWnd수수료; // Edit

        // 차량정보
        public IntPtr 접수섹션_차량_hWnd톤수; // Combo
        public IntPtr 접수섹션_차량_hWnd차종; // Combo
        public IntPtr 접수섹션_차량_hWnd대수; // Combo
        public IntPtr 접수섹션_차량_hWnd결재; // Combo

        // 화물중량
        public IntPtr 접수섹션_hWnd화물중량; // Edit

        // 구분
        public IntPtr 접수섹션_구분_hWnd독차; // 체크박스 
        public IntPtr 접수섹션_구분_hWnd혼적; // 체크박스 
        public IntPtr 접수섹션_구분_hWnd긴급; // 체크박스 
        public IntPtr 접수섹션_구분_hWnd왕복; // 체크박스 
        public IntPtr 접수섹션_구분_hWnd경유; // 체크박스 

        // 상차방법
        public IntPtr 접수섹션_상차방법_hWnd지게차; // 체크박스 
        public IntPtr 접수섹션_상차방법_hWn호이스트; // 체크박스 
        public IntPtr 접수섹션_상차방법_hWnd수해줌; // 체크박스 
        public IntPtr 접수섹션_상차방법_hWnd수작업; // 체크박스 
        public IntPtr 접수섹션_상차방법_hWnd크레인; // 체크박스 

        // 상차일시
        public IntPtr 접수섹션_상차일시_hWnd당상; // 체크박스 
        public IntPtr 접수섹션_상차일시_hWnd낼상; // 체크박스 
        public IntPtr 접수섹션_상차일시_hWnd월상; // 체크박스 

        // 하차방법
        public IntPtr 접수섹션_하차방법_hWnd지게차; // 체크박스 
        public IntPtr 접수섹션_하차방법_hWn호이스트; // 체크박스 
        public IntPtr 접수섹션_하차방법_hWnd수해줌; // 체크박스 
        public IntPtr 접수섹션_하차방법_hWnd수작업; // 체크박스 
        public IntPtr 접수섹션_하차방법_hWnd크레인; // 체크박스 
                              
        // 하차일시
        public IntPtr 접수섹션_하차일시_hWnd당착; // 체크박스 
        public IntPtr 접수섹션_하차일시_hWnd낼착; // 체크박스 
        public IntPtr 접수섹션_하차일시_hWnd월착; // 체크박스 
        public IntPtr 접수섹션_하차일시_hWnd당_내착; // 체크박스 

        // 화물메모
        public IntPtr 접수섹션_hWnd화물메모; // Edit

        // 의뢰자
        public IntPtr 접수섹션_의뢰자_hWnd상호; // Combo
        public IntPtr 접수섹션_의뢰자_hWnd전화; // Edit
        #endregion

        #region 검색영역
        // 최상위 - 검색영역
        public IntPtr 검색섹션_hWndTop;  // hWnd
        public IntPtr 검색섹션_hWnd포커스탈출;  // hWnd
        public IntPtr 검색섹션_hWnd자동조회;  // hWnd

        // 버튼들
        public IntPtr 검색섹션_hWnd새로고침버튼;  // hWnd - 
        public IntPtr 검색섹션_hWnd확장버튼;  // hWnd - ExpandBtn 
        #endregion

        #region Datagrid
        // 최상위 - Datagrid
        public IntPtr DG오더_hWndTop;  // hWnd
        public bool bExpandMode = false;

        // 영역
        public Draw.Rectangle[,] DG오더_rcRelLargeCells;
        public Draw.Point[] DG오더_ptRelChkLargeRows;
        public Draw.Rectangle[,] DG오더_rcRelSmallCells;
        public Draw.Point[] DG오더_ptRelChkSmallRows;

        // 명도
        public int DG오더_nBkMarginedBright = 0; 
        #endregion
    }
    #endregion

    //#region 접수등록Page
    //// TopWnd
    //public IntPtr 접수등록Page_TopWnd_hWnd;  // hWnd 
    ////public Draw.Rectangle 접수등록Page_TopWnd_rcAbs; // Bound

    //// ListSonWnd
    ////public List<StdCommon32_WndInfo> 접수등록Page_WndInfoList;
    ////public StdCommon32_WndInfo 접수등록Page_WndInfo_CmdBtnGroupBox;

    //// StatusBtn 
    ////public IntPtr 접수등록Page_StatusBtn_hWnd접수;
    ////public IntPtr 접수등록Page_StatusBtn_hWnd운행;
    ////public IntPtr 접수등록Page_StatusBtn_hWnd취소;
    ////public IntPtr 접수등록Page_StatusBtn_hWnd완료;
    ////public IntPtr 접수등록Page_StatusBtn_hWnd정산;
    ////public IntPtr 접수등록Page_StatusBtn_hWnd전체;

    //// CommandBtns GroupBox
    //public IntPtr 접수등록Page_CmdBtn_hWnd신규;
    ////public IntPtr 접수등록Page_CmdBtn_hWnd조회;

    //// Datagrid
    //public IntPtr 접수등록Page_DG오더_hWnd;
    //public Draw.Rectangle 접수등록Page_DG오더_AbsRect;
    //public Draw.Rectangle 접수등록Page_DG오더_RelRect;
    //public Draw.Rectangle[,] 접수등록Page_DG오더_RelChildRects;
    ////public string[] 접수등록Page_DG오더_ColumnTexts;
    //public NwCommon_DgColumnHeader[] 접수등록Page_DG오더_ColumnInfos;
    //public Draw.Point[] 접수등록Page_DG오더_RelChkRows;
    ////public Draw.Point 접수등록Page_DG오더_ChkRelPt_Select;
    ////public Draw.Color 접수등록Page_DG오더_ChkColor_Select;
    //#endregion

    #region 접수(화물)등록Wnd
    public class RcptRegWnd
    {
        // TopWnd
        public IntPtr TopWnd_hWnd;  // hWnd 

        public RcptRegWnd(IntPtr hWnd)
        {
            TopWnd_hWnd = hWnd;
        }
    }
    #endregion


    #region 다수 있는 클래스
    public class OrderBasic
    {
        public int nTernNo = 0; // TernNo
        public string sStatus = string.Empty; // 상태
        //public string sSeqNo = string.Empty; // SeqNo
    }
    #endregion
}
#nullable enable
