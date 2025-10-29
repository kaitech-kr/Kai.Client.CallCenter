using Draw = System.Drawing;

using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;
using Kai.Common.NetDll_WpfCtrl.NetOFR;
using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgBox;

namespace Kai.Client.CallCenter.Networks.NwInsungs;
#nullable disable
public class InsungsInfo_Mem
{
    #region Variables - 1개만 있고 사용빈도가 있는 Wnd, Page는 미리 할당한다 - 편의성
    public SplashWnd Splash = null;
    public MainWnd Main = null;
    public RcptRegPage RcptPage = null;
    //public CustRegPage CustPage = null;
    #endregion Variables 끝

    #region 생성자
    public InsungsInfo_Mem()
    {
        Splash = new SplashWnd();
        Main = new MainWnd();
        RcptPage = new RcptRegPage();
        //CustPage = new CustRegPage();
    }
    #endregion 생성자 끝

    #region Windows
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

    public class MainWnd
    {
        // TopWnd
        public IntPtr TopWnd_hWnd;  // hWnd

        // ListSonWnd
        public List<StdCommon32_WndInfo> FirstLayer_ChildWnds;
        public StdCommon32_WndInfo WndInfo_MainMenu;
        public StdCommon32_WndInfo WndInfo_BarMenu;
        public StdCommon32_WndInfo WndInfo_MdiClient;
    }

    public class RcptWnd_Common
    {
        // Header
        public string Header_sOrderNo;  // Text
        public string Header_sOrderStatus;  // Text

        // 의뢰자
        public long 의뢰자_l고객번호;  // long
        public string 의뢰자_s고객명;  // Text
        public string 의뢰자_s동명;  // Text
        public string 의뢰자_s전화1;  // Text
        public string 의뢰자_s전화2;  // Text
        public string 의뢰자_s부서;  // Text
        public string 의뢰자_s담당;  // Text
        public string 의뢰자_s주소;  // Text
        public string 의뢰자_s위치;  // Text
        public string 의뢰자_s적요;  // Text

        public IntPtr 의뢰자_hWnd의뢰자Top;  // Handle
        public IntPtr 의뢰자_hWnd고객명;  // Handle
        public IntPtr 의뢰자_hWnd동명;  // Handle
        public IntPtr 의뢰자_hWnd전화1;  // Handle
        public IntPtr 의뢰자_hWnd전화2;  // Handle
        public IntPtr 의뢰자_hWnd부서;  // Handle
        public IntPtr 의뢰자_hWnd담당;  // Handle
        public IntPtr 의뢰자_hWnd주소;  // Handle
        public IntPtr 의뢰자_hWnd위치;  // Handle
        public IntPtr 의뢰자_hWnd적요;  // Handle

        // 출발지
        public long 출발지_l고객번호;  // long
        public string 출발지_s고객명;  // Text
        public string 출발지_s동명;  // Text
        public string 출발지_s전화1;  // Text
        public string 출발지_s전화2;  // Text
        public string 출발지_s부서;  // Text
        public string 출발지_s담당;  // Text
        public string 출발지_s주소;  // Text
        public string 출발지_s위치;  // Text

        public IntPtr 출발지_hWnd출발지Top;  // Handle
        public IntPtr 출발지_hWnd고객명;  // Handle
        public IntPtr 출발지_hWnd동명;  // Handle
        public IntPtr 출발지_hWnd전화1;  // Handle
        public IntPtr 출발지_hWnd전화2;  // Handle
        public IntPtr 출발지_hWnd부서;  // Handle
        public IntPtr 출발지_hWnd담당;  // Handle
        public IntPtr 출발지_hWnd주소;  // Handle
        public IntPtr 출발지_hWnd위치;  // Handle

        // 도착지
        public long 도착지_l고객번호;  // long
        public string 도착지_s고객명;  // Text
        public string 도착지_s동명;  // Text
        public string 도착지_s전화1;  // Text
        public string 도착지_s전화2;  // Text
        public string 도착지_s부서;  // Text
        public string 도착지_s담당;  // Text
        public string 도착지_s주소;  // Text
        public string 도착지_s위치;  // Text

        public IntPtr 도착지_hWnd도착지Top;  // Handle
        public IntPtr 도착지_hWnd고객명;  // Handle
        public IntPtr 도착지_hWnd동명;  // Handle
        public IntPtr 도착지_hWnd전화1;  // Handle
        public IntPtr 도착지_hWnd전화2;  // Handle
        public IntPtr 도착지_hWnd부서;  // Handle
        public IntPtr 도착지_hWnd담당;  // Handle
        public IntPtr 도착지_hWnd주소;  // Handle
        public IntPtr 도착지_hWnd위치;  // Handle

        // 예약, SMS, 적요, 공유...
        public bool 우측상단_b예약여부;  // bool
        public string 우측상단_s예약일시;  // Text
        public string 우측상단_s예약해제;  // Text
        public string 우측상단_s적요;  // Text
        public bool 우측상단_b공유;  // bool
        public bool 우측상단_b계산서;  // bool
        public bool 우측상단_b수수무;  // bool
        public string 우측상단_s물품종류;  // Text
        public string 우측상단_s요금종류;  // Text
        public string 우측상단_s차량종류;  // Text
        public string 우측상단_s차량톤수;  // Text
        public string 우측상단_s트럭상세;  // Text
        public bool 우측상단_b플럭제외;  // bool
        public bool 우측상단_b인수증필;  // bool
        public bool 우측상단_b배송지정;  // bool
        public bool 우측상단_b당일택배;  // bool
        public string 우측상단_s배송종류;  // Text
        public string 우측상단_s배송조건;  // Text

        //public IntPtr 우측상단_hWnd예약여부;  // Handle
        //public IntPtr 우측상단_hWnd예약일시;  // Handle
        //public IntPtr 우측상단_hWnd예약해제;  // Handle
        public IntPtr 우측상단_hWnd적요;  // Handle
        public IntPtr 우측상단_hWnd공유;  // IntPtr
        public IntPtr 우측상단_hWnd계산서;  // IntPtr
        //public IntPtr 우측상단_hWnd수수무;  // IntPtr
        //public IntPtr 우측상단_hWnd물품종류;  // Handle

        public OfrModel_RadioBtns 우측상단_btns요금종류;  // Instance
        public OfrModel_RadioBtns 우측상단_btns차량종류;  // Instance
        public IntPtr 우측상단_hWnd차량톤수;  // Handle
        public IntPtr 우측상단_hWnd트럭상세;  // Handle
        public IntPtr 우측상단_hWnd플럭제외;  // IntPtr
        public IntPtr 우측상단_hWnd인수증필;  // IntPtr
        //public IntPtr 우측상단_hWnd배송지정;  // IntPtr
        //public IntPtr 우측상단_hWnd당일택배;  // IntPtr
        public OfrModel_RadioBtns 우측상단_btns배송종류;  // Instance
        //public IntPtr 우측상단_hWnd배송조건;  // Handle

        // 요금그룹
        public bool 요금그룹_b부가세;  // bool
        public string 요금그룹_s부가세액;  // Text
        public string 요금그룹_s기본요금;  // Text
        public string 요금그룹_s추가금액;  // Text
        public string 요금그룹_s할인금액;  // Text
        public string 요금그룹_s탁송료;  // Text
        public string 요금그룹_s합계금액;  // Text
        public string 요금그룹_s기사금액;  // Text
        public string 요금그룹_s마일리지;  // Text
        public bool 요금그룹_b기사처리비여부;  // bool
        public string 요금그룹_s기사처리비액;  // Text

        // 기사
        public string 기사그룹_s기사번호;  // Text
        public string 기사그룹_s기사이름;  // Text
        public string 기사그룹_s기사소속;  // Text
        public string 기사그룹_s기사타입;  // Text
        public string 기사그룹_s기사전화;  // Text
        public string 기사그룹_s세금계산서;  // Text

        //public IntPtr 요금그룹_hWnd부가세;  // IntPtr
        //public IntPtr 요금그룹_hWnd부가세액;  // IntPtr
        public IntPtr 요금그룹_hWnd기본요금;  // IntPtr
        public IntPtr 요금그룹_hWnd추가금액;  // IntPtr
        public IntPtr 요금그룹_hWnd할인금액;  // IntPtr
        public IntPtr 요금그룹_hWnd탁송료;  // IntPtr
        //public IntPtr 요금그룹_hWnd합계금액;  // IntPtr
        //public IntPtr 요금그룹_hWnd기사금액;  // IntPtr
        //public IntPtr 요금그룹_hWnd마일리지;  // IntPtr
        //public IntPtr 요금그룹_hWnd기사처리비여부;  // IntPtr
        //public IntPtr 요금그룹_hWnd기사처리비액;  // IntPtr

        // 기사
        public IntPtr 기사그룹_hWnd기사번호;  // IntPtr
        public IntPtr 기사그룹_hWnd기사이름;  // IntPtr
        public IntPtr 기사그룹_hWnd기사소속;  // IntPtr
        public IntPtr 기사그룹_hWnd기사타입;  // IntPtr
        public IntPtr 기사그룹_hWnd기사전화;  // IntPtr
        public IntPtr 기사그룹_hWnd세금계산서;  // IntPtr

        // 기타
        public string 우측하단_s오더메모;  // Text

        public IntPtr 우측하단_hWnd오더메모;  // IntPtr

        protected void SetWndHandles(IntPtr TopWnd_hWnd, InsungsInfo_File fInfo)
        {
            // 의뢰지
            의뢰자_hWnd의뢰자Top = Std32Window.GetWndHandle_FromRelDrawPt(TopWnd_hWnd, fInfo.접수등록Wnd_의뢰자_ptChkRel의뢰자);
            의뢰자_hWnd고객명 = Std32Window.GetWndHandle_FromRelDrawPt(TopWnd_hWnd, fInfo.접수등록Wnd_의뢰자_ptChkRel고객명);
            의뢰자_hWnd동명 = Std32Window.GetWndHandle_FromRelDrawPt(TopWnd_hWnd, fInfo.접수등록Wnd_의뢰자_ptChkRel동명);
            의뢰자_hWnd전화1 = Std32Window.GetWndHandle_FromRelDrawPt(TopWnd_hWnd, fInfo.접수등록Wnd_의뢰자_ptChkRel전화1);
            의뢰자_hWnd전화2 = Std32Window.GetWndHandle_FromRelDrawPt(TopWnd_hWnd, fInfo.접수등록Wnd_의뢰자_ptChkRel전화2);
            의뢰자_hWnd부서 = Std32Window.GetWndHandle_FromRelDrawPt(TopWnd_hWnd, fInfo.접수등록Wnd_의뢰자_ptChkRel부서);
            의뢰자_hWnd담당 = Std32Window.GetWndHandle_FromRelDrawPt(TopWnd_hWnd, fInfo.접수등록Wnd_의뢰자_ptChkRel담당);
            의뢰자_hWnd주소 = Std32Window.GetWndHandle_FromRelDrawPt(TopWnd_hWnd, fInfo.접수등록Wnd_의뢰자_ptChkRel주소);
            의뢰자_hWnd위치 = Std32Window.GetWndHandle_FromRelDrawPt(TopWnd_hWnd, fInfo.접수등록Wnd_의뢰자_ptChkRel위치);
            의뢰자_hWnd적요 = Std32Window.GetWndHandle_FromRelDrawPt(TopWnd_hWnd, fInfo.접수등록Wnd_의뢰자_ptChkRel적요);

            // 출발지
            출발지_hWnd출발지Top = Std32Window.GetWndHandle_FromRelDrawPt(TopWnd_hWnd, fInfo.접수등록Wnd_출발지_ptChkRel출발지);
            출발지_hWnd고객명 = Std32Window.GetWndHandle_FromRelDrawPt(TopWnd_hWnd, fInfo.접수등록Wnd_출발지_ptChkRel고객명);
            출발지_hWnd동명 = Std32Window.GetWndHandle_FromRelDrawPt(TopWnd_hWnd, fInfo.접수등록Wnd_출발지_ptChkRel동명);
            출발지_hWnd전화1 = Std32Window.GetWndHandle_FromRelDrawPt(TopWnd_hWnd, fInfo.접수등록Wnd_출발지_ptChkRel전화1);
            출발지_hWnd전화2 = Std32Window.GetWndHandle_FromRelDrawPt(TopWnd_hWnd, fInfo.접수등록Wnd_출발지_ptChkRel전화2);
            출발지_hWnd부서 = Std32Window.GetWndHandle_FromRelDrawPt(TopWnd_hWnd, fInfo.접수등록Wnd_출발지_ptChkRel부서);
            출발지_hWnd담당 = Std32Window.GetWndHandle_FromRelDrawPt(TopWnd_hWnd, fInfo.접수등록Wnd_출발지_ptChkRel담당);
            출발지_hWnd주소 = Std32Window.GetWndHandle_FromRelDrawPt(TopWnd_hWnd, fInfo.접수등록Wnd_출발지_ptChkRel주소); // 위치정보 검색한 상태에서만 인에이블
            출발지_hWnd위치 = Std32Window.GetWndHandle_FromRelDrawPt(TopWnd_hWnd, fInfo.접수등록Wnd_출발지_ptChkRel위치);

            // 도착지
            도착지_hWnd도착지Top = Std32Window.GetWndHandle_FromRelDrawPt(TopWnd_hWnd, fInfo.접수등록Wnd_도착지_ptChkRel도착지);
            도착지_hWnd고객명 = Std32Window.GetWndHandle_FromRelDrawPt(TopWnd_hWnd, fInfo.접수등록Wnd_도착지_ptChkRel고객명);
            도착지_hWnd동명 = Std32Window.GetWndHandle_FromRelDrawPt(TopWnd_hWnd, fInfo.접수등록Wnd_도착지_ptChkRel동명);
            도착지_hWnd전화1 = Std32Window.GetWndHandle_FromRelDrawPt(TopWnd_hWnd, fInfo.접수등록Wnd_도착지_ptChkRel전화1);
            도착지_hWnd전화2 = Std32Window.GetWndHandle_FromRelDrawPt(TopWnd_hWnd, fInfo.접수등록Wnd_도착지_ptChkRel전화2);
            도착지_hWnd부서 = Std32Window.GetWndHandle_FromRelDrawPt(TopWnd_hWnd, fInfo.접수등록Wnd_도착지_ptChkRel부서);
            도착지_hWnd담당 = Std32Window.GetWndHandle_FromRelDrawPt(TopWnd_hWnd, fInfo.접수등록Wnd_도착지_ptChkRel담당);
            도착지_hWnd주소 = Std32Window.GetWndHandle_FromRelDrawPt(TopWnd_hWnd, fInfo.접수등록Wnd_도착지_ptChkRel주소); // 위치정보 검색한 상태에서만 인에이블
            도착지_hWnd위치 = Std32Window.GetWndHandle_FromRelDrawPt(TopWnd_hWnd, fInfo.접수등록Wnd_도착지_ptChkRel위치);

            //MsgBox($"{fInfo.접수등록Wnd_도착지_ptChkRel동명}, {도착지_hWnd동명:X}");

            // 예약, SMS, 적요, 공유...
            우측상단_hWnd적요 = Std32Window.GetWndHandle_FromRelDrawPt(TopWnd_hWnd, fInfo.접수등록Wnd_우측상단_ptChkRel적요);
            //우측상단_hWnd공유 = Std32Window.GetWndHandle_FromRelDrawRect(TopWnd_hWnd, fInfo.접수등록Wnd_우측상단_rcChkRel공유);
            //우측상단_hWnd계산서 = Std32Window.GetWndHandle_FromRelDrawRect(TopWnd_hWnd, fInfo.접수등록Wnd_우측상단_rcChkRel계산서);

            우측상단_btns요금종류 = new OfrModel_RadioBtns(TopWnd_hWnd, fInfo.접수등록Wnd_우측상단_요금그룹);
            우측상단_btns차량종류 = new OfrModel_RadioBtns(TopWnd_hWnd, fInfo.접수등록Wnd_우측상단_차량그룹);
            우측상단_btns배송종류 = new OfrModel_RadioBtns(TopWnd_hWnd, fInfo.접수등록Wnd_우측상단_배송그룹);
            우측상단_hWnd차량톤수 = Std32Window.GetWndHandle_FromRelDrawRect(TopWnd_hWnd, fInfo.접수등록Wnd_우측상단_rcChkRel차량톤수);
            우측상단_hWnd트럭상세 = Std32Window.GetWndHandle_FromRelDrawRect(TopWnd_hWnd, fInfo.접수등록Wnd_우측상단_rcChkRel트럭상세);
            우측상단_hWnd플럭제외 = Std32Window.GetWndHandle_FromRelDrawRect(TopWnd_hWnd, fInfo.접수등록Wnd_우측상단_rcChkRel플럭제외); // 차량톤수 콤보박스 체크용
            우측상단_hWnd인수증필 = Std32Window.GetWndHandle_FromRelDrawRect(TopWnd_hWnd, fInfo.접수등록Wnd_우측상단_rcChkRel인수증필);  // 트럭상세) 콤보박스 체크용

            // 요금
            요금그룹_hWnd기본요금 = Std32Window.GetWndHandle_FromRelDrawRectCenter(TopWnd_hWnd, fInfo.접수등록Wnd_요금그룹_rcChkRel기본요금);
            요금그룹_hWnd추가금액 = Std32Window.GetWndHandle_FromRelDrawRectCenter(TopWnd_hWnd, fInfo.접수등록Wnd_요금그룹_rcChkRel추가금액);
            요금그룹_hWnd할인금액 = Std32Window.GetWndHandle_FromRelDrawRectCenter(TopWnd_hWnd, fInfo.접수등록Wnd_요금그룹_rcChkRel할인금액);
            요금그룹_hWnd탁송료 = Std32Window.GetWndHandle_FromRelDrawRectCenter(TopWnd_hWnd, fInfo.접수등록Wnd_요금그룹_rcChkRel탁송료);

            // 기사
            기사그룹_hWnd기사번호 = Std32Window.GetWndHandle_FromRelDrawRectCenter(TopWnd_hWnd, fInfo.접수등록Wnd_기사그룹_rcChkRel기사번호);
            기사그룹_hWnd기사이름 = Std32Window.GetWndHandle_FromRelDrawRectCenter(TopWnd_hWnd, fInfo.접수등록Wnd_기사그룹_rcChkRel기사이름);
            기사그룹_hWnd기사소속 = Std32Window.GetWndHandle_FromRelDrawRectCenter(TopWnd_hWnd, fInfo.접수등록Wnd_기사그룹_rcChkRel기사소속);
            //기사그룹_hWnd기사타입 = Std32Window.GetWndHandle_FromRelDrawRectCenter(TopWnd_hWnd, fInfo.접수등록Wnd_기사그룹_rcChkRel기사타입);
            기사그룹_hWnd기사전화 = Std32Window.GetWndHandle_FromRelDrawRectCenter(TopWnd_hWnd, fInfo.접수등록Wnd_기사그룹_rcChkRel기사전화);
            //기사그룹_hWnd세금계산서 = Std32Window.GetWndHandle_FromRelDrawRectCenter(TopWnd_hWnd, fInfo.접수등록Wnd_기사그룹_rcChkRel세금계산서);

            // 오더메모
            우측하단_hWnd오더메모 = Std32Window.GetWndHandle_FromRelDrawPt(TopWnd_hWnd, fInfo.접수등록Wnd_ptChkRel오더메모);
        }
    }
    public class RcptWnd_New : RcptWnd_Common
    {
        // TopWnd
        public IntPtr TopWnd_hWnd;  // hWnd

        // 버튼들
        public IntPtr Btn_hWnd닫기;  // 닫기 - 공용

        public IntPtr Btn_hWnd고객등록; // 수정?
        public IntPtr Btn_hWnd접수저장;
        public IntPtr Btn_hWnd대기저장;

        public RcptWnd_New(IntPtr hWnd, InsungsInfo_File fInfo)
        {
            TopWnd_hWnd = hWnd;

            // 버튼들
            Btn_hWnd닫기 = Std32Window.GetWndHandle_FromRelDrawPt(hWnd, fInfo.접수등록Wnd_신규버튼그룹_ptChkRel닫기);
        }

        public void SetWndHandles(InsungsInfo_File fInfo)
        {
            Btn_hWnd고객등록 = Std32Window.GetWndHandle_FromRelDrawPt(TopWnd_hWnd, fInfo.접수등록Wnd_신규버튼그룹_ptChkRel고객등록);
            Btn_hWnd접수저장 = Std32Window.GetWndHandle_FromRelDrawPt(TopWnd_hWnd, fInfo.접수등록Wnd_신규버튼그룹_ptChkRel접수저장);
            Btn_hWnd대기저장 = Std32Window.GetWndHandle_FromRelDrawPt(TopWnd_hWnd, fInfo.접수등록Wnd_신규버튼그룹_ptChkRel대기저장);

            base.SetWndHandles(TopWnd_hWnd, fInfo);
        }
    }
    //public class RcptWnd_Edit : RcptWnd_Common
    //{
    //    // TopWnd
    //    public IntPtr TopWnd_hWnd;  // hWnd 
                                    
    //    // 버튼들 
    //    public IntPtr Btn_hWnd닫기;  

    //    public IntPtr Btn_hWnd고객수정;
    //    public IntPtr Btn_hWnd배차;
    //    public IntPtr Btn_hWnd처리완료;
    //    public IntPtr Btn_hWnd대기;
    //    public IntPtr Btn_hWnd주문취소;
    //    public IntPtr Btn_hWnd접수상태;
    //    public IntPtr Btn_hWnd저장;

    //    public RcptWnd_Edit(IntPtr hWnd, InsungsInfo_File fInfo)
    //    {
    //        TopWnd_hWnd = hWnd;

    //        // 버튼들
    //        Btn_hWnd닫기 = Std32Window.GetWndHandle_FromRelDrawPt(hWnd, fInfo.접수등록Wnd_수정버튼그룹_ptChkRel닫기);

    //        this.SetWndHandle( fInfo);
    //    }

    //    public void SetWndHandle(InsungsInfo_File fInfo) // 확인필요
    //    {
    //        Btn_hWnd고객수정 = Std32Window.GetWndHandle_FromRelDrawPt(TopWnd_hWnd, fInfo.접수등록Wnd_신규버튼그룹_ptChkRel고객수정);
    //        Btn_hWnd배차 = Std32Window.GetWndHandle_FromRelDrawPt(TopWnd_hWnd, fInfo.접수등록Wnd_수정버튼그룹_ptChkRel배차);
    //        Btn_hWnd처리완료 = Std32Window.GetWndHandle_FromRelDrawPt(TopWnd_hWnd, fInfo.접수등록Wnd_수정버튼그룹_ptChkRel처리완료);
    //        Btn_hWnd대기 = Std32Window.GetWndHandle_FromRelDrawPt(TopWnd_hWnd, fInfo.접수등록Wnd_수정버튼그룹_ptChkRel대기);
    //        Btn_hWnd주문취소 = Std32Window.GetWndHandle_FromRelDrawPt(TopWnd_hWnd, fInfo.접수등록Wnd_수정버튼그룹_ptChkRel주문취소);
    //        Btn_hWnd접수상태 = Std32Window.GetWndHandle_FromRelDrawPt(TopWnd_hWnd, fInfo.접수등록Wnd_수정버튼그룹_ptChkRel접수상태);
    //        Btn_hWnd저장 = Std32Window.GetWndHandle_FromRelDrawPt(TopWnd_hWnd, fInfo.접수등록Wnd_수정버튼그룹_ptChkRel저장);

    //        base.SetWndHandles(TopWnd_hWnd, fInfo);
    //    }
    //}

    //public class CustRegWnd
    //{
    //    // TopWnd
    //    public IntPtr TopWnd_hWnd;  // hWnd 

    //    public IntPtr hWndBtnClose;    // 버튼 - 닫기

    //    // Data
    //    public long lKeyCode;  // Text
    //    public string s동명;  // Text

    //    public CustRegWnd(IntPtr hWnd, InsungsInfo_File fInfo)
    //    {
    //        TopWnd_hWnd = hWnd;
    //        hWndBtnClose = Std32Window.GetWndHandle_FromRelDrawPt(hWnd, fInfo.고객등록Wnd_ptChkRel닫기버튼);
    //    }
    //}

    //public class CustSearch
    //{
    //    // TopWnd
    //    public IntPtr TopWnd_hWnd;  // hWnd 

    //    public IntPtr hWndBtnClose;    // 버튼 - 닫기

    //    // Data
    //    public CustSearch(IntPtr hWnd, InsungsInfo_File fInfo)
    //    {
    //        TopWnd_hWnd = hWnd;
    //        hWndBtnClose = Std32Window.GetWndHandle_FromRelDrawPt(hWnd, fInfo.고객등록Wnd_ptChkRel닫기버튼);
    //    }
    //}
    #endregion Windows 끝

    #region Pages
    public class RcptRegPage
    {
        // TopWnd
        public IntPtr TopWnd_hWnd;  // hWnd

        // StatusBtn
        public IntPtr StatusBtn_hWnd접수;
        public IntPtr StatusBtn_hWnd배차;
        public IntPtr StatusBtn_hWnd운행;
        public IntPtr StatusBtn_hWnd완료;
        public IntPtr StatusBtn_hWnd취소;
        public IntPtr StatusBtn_hWnd전체;

        // CommandBtns GroupBox
        public IntPtr CmdBtn_hWnd신규;
        public IntPtr CmdBtn_hWnd조회;
        public IntPtr CmdBtn_hWnd기사;

        // CallCount
        public IntPtr CallCount_hWnd접수;
        public IntPtr CallCount_hWnd운행;
        public IntPtr CallCount_hWnd취소;
        public IntPtr CallCount_hWnd완료;
        public IntPtr CallCount_hWnd총계;

        // Datagrid
        public IntPtr DG오더_hWnd;
        public Draw.Rectangle DG오더_AbsRect;
        public Draw.Rectangle[,] DG오더_RelChildRects;
        public string[] DG오더_ColumnTexts;
        public int DG오더_nBackgroundBright = 0;

        public IntPtr DG오더_hWnd수직스크롤;
    }
    #endregion Pages 끝
}
#nullable restore