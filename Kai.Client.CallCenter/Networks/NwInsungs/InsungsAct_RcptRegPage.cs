using System.Linq;
using System.Diagnostics;
using Draw = System.Drawing;
using DrawImg = System.Drawing.Imaging;

using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;
using Kai.Common.NetDll_WpfCtrl.NetOFR;
using static Kai.Common.FrmDll_FormCtrl.FormFuncs;
//using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgBox;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Services;

using Kai.Client.CallCenter.Classes;
using Kai.Client.CallCenter.Classes.Class_Master;
using Kai.Client.CallCenter.OfrWorks;
using Kai.Client.CallCenter.Pages;
using Kai.Client.CallCenter.Windows;
using Kai.Client.CallCenter.MVVM.ViewServices;
using static Kai.Client.CallCenter.Classes.CommonVars;

namespace Kai.Client.CallCenter.Networks.NwInsungs;
#nullable disable

/// <summary>
/// 인성 앱 접수등록 페이지 초기화 및 제어 담당 클래스
/// Context 패턴 사용: InsungContext를 통해 모든 정보에 접근
/// </summary>
public partial class InsungsAct_RcptRegPage
{
    #region Region 4 Helper Classes
    // Helper classes moved to CommonResults.cs:
    // - CommonResult_SearchType (고객 검색 타입 결과)
    #endregion

    #region Variables
    /// <summary>
    /// 컬럼 너비 허용 오차 (픽셀)
    /// </summary>
    private const int COLUMN_WIDTH_TOLERANCE = 1;

    /// <summary>
    /// Datagrid 헤더 상단 여백 (텍스트 없는 영역)
    /// </summary>
    private const int HEADER_GAB = 7;

    /// <summary>
    /// Datagrid 헤더 텍스트 영역 높이
    /// </summary>
    private const int HEADER_TEXT_HEIGHT = 18;

    /// <summary>
    /// Datagrid 컬럼 인덱스 상수 (변경 시 수동으로 같이 변경 필요)
    /// </summary>
    public const int c_nCol번호 = 0;
    public const int c_nCol상태 = 1;
    public const int c_nCol주문번호 = 2;
    public const int c_nCol기사전번 = 18;
    public const int c_nCol오더메모 = 19;

    /// <summary>
    /// 접수등록 Datagrid 컬럼 헤더 정보 (20개 컬럼)
    /// </summary>
    public readonly NwCommon_DgColumnHeader[] m_ReceiptDgHeaderInfos = new NwCommon_DgColumnHeader[]
    {
        new NwCommon_DgColumnHeader() { sName = "No", bOfrSeq = false, nWidth = 60 },
        new NwCommon_DgColumnHeader() { sName = "상태", bOfrSeq = false, nWidth = 70 },

        new NwCommon_DgColumnHeader() { sName = "주문번호", bOfrSeq = true, nWidth = 80  },
        new NwCommon_DgColumnHeader() { sName = "최초접수시간", bOfrSeq = true, nWidth = 90 },
        new NwCommon_DgColumnHeader() { sName = "접수시간", bOfrSeq = true, nWidth = 90  },

        new NwCommon_DgColumnHeader() { sName = "고객명", bOfrSeq = false, nWidth = 100 },
        new NwCommon_DgColumnHeader() { sName = "담당자", bOfrSeq = false, nWidth = 100 },

        new NwCommon_DgColumnHeader() { sName = "전화번호", bOfrSeq = true, nWidth = 100 },

        new NwCommon_DgColumnHeader() { sName = "출발동", bOfrSeq = false, nWidth = 120 },
        new NwCommon_DgColumnHeader() { sName = "도착동", bOfrSeq = false, nWidth = 120 },

        new NwCommon_DgColumnHeader() { sName = "요금", bOfrSeq = true, nWidth = 62 },

        new NwCommon_DgColumnHeader() { sName = "지급", bOfrSeq = false, nWidth = 40 },
        new NwCommon_DgColumnHeader() { sName = "형태", bOfrSeq = false, nWidth = 40 },
        new NwCommon_DgColumnHeader() { sName = "차량", bOfrSeq = false, nWidth = 60 },
        new NwCommon_DgColumnHeader() { sName = "계산서", bOfrSeq = false, nWidth = 50 },
        new NwCommon_DgColumnHeader() { sName = "왕복", bOfrSeq = false, nWidth = 40 },
        new NwCommon_DgColumnHeader() { sName = "공유", bOfrSeq = false, nWidth = 40 },
        new NwCommon_DgColumnHeader() { sName = "라이더", bOfrSeq = false, nWidth = 120 },
        new NwCommon_DgColumnHeader() { sName = "기사전번", bOfrSeq = true, nWidth = 100 },

        new NwCommon_DgColumnHeader() { sName = "오더메모", bOfrSeq = false, nWidth = 100 },

        new NwCommon_DgColumnHeader() { sName = "적요", bOfrSeq = false, nWidth = 240 },

    #region Temp Save
    //new NwCommon_DgColumnHeader() { sName = "배차", bOfrSeq = true },
    //new NwCommon_DgColumnHeader() { sName = "픽업", bOfrSeq = true },
    //new NwCommon_DgColumnHeader() { sName = "완료", bOfrSeq = true },

    //new NwCommon_DgColumnHeader() { sName = "기본", bOfrSeq = true },
    //new NwCommon_DgColumnHeader() { sName = "추가", bOfrSeq = true },
    //new NwCommon_DgColumnHeader() { sName = "할인", bOfrSeq = true },
    //new NwCommon_DgColumnHeader() { sName = "탁송", bOfrSeq = true },
    //new NwCommon_DgColumnHeader() { sName = "부가세", bOfrSeq = true },
    //new NwCommon_DgColumnHeader() { sName = "지급일", bOfrSeq = true },
    //new NwCommon_DgColumnHeader() { sName = "Km", bOfrSeq = true },
    //new NwCommon_DgColumnHeader() { sName = "국선번호", bOfrSeq = true },
    //new NwCommon_DgColumnHeader() { sName = "오더일자", bOfrSeq = true },
    //new NwCommon_DgColumnHeader() { sName = "산재보험", bOfrSeq = true },
    //new NwCommon_DgColumnHeader() { sName = "묶음", bOfrSeq = true },
    //new NwCommon_DgColumnHeader() { sName = "출", bOfrSeq = false },
    //new NwCommon_DgColumnHeader() { sName = "착",  bOfrSeq = false },

    //new NwCommon_DgColumnHeader() { sName = "접수자", bOfrSeq = false },
    //new NwCommon_DgColumnHeader() { sName = "부서명", bOfrSeq = false },
    //new NwCommon_DgColumnHeader() { sName = "출발지역", bOfrSeq = false },
    //new NwCommon_DgColumnHeader() { sName = "도착지역", bOfrSeq = false },
    //new NwCommon_DgColumnHeader() { sName = "기사그룹", bOfrSeq = false },
    //new NwCommon_DgColumnHeader() { sName = "결재", bOfrSeq = false },
    //new NwCommon_DgColumnHeader() { sName = "출발지", bOfrSeq = false },
    //new NwCommon_DgColumnHeader() { sName = "도착지", bOfrSeq = false },
    //new NwCommon_DgColumnHeader() { sName = "거래처명", bOfrSeq = false },
    //new NwCommon_DgColumnHeader() { sName = "고객메모", bOfrSeq = false },
    //new NwCommon_DgColumnHeader() { sName = "접수처", bOfrSeq = false },
    //new NwCommon_DgColumnHeader() { sName = "인수증", bOfrSeq = false },
    //new NwCommon_DgColumnHeader() { sName = "차량번호", bOfrSeq = false },
    #endregion Temp Save 끝
    };
    #endregion

    #region Context Reference
    /// <summary>
    /// Context에 대한 읽기 전용 참조
    /// </summary>
    private readonly InsungContext m_Context;

    /// <summary>
    /// 편의를 위한 로컬 참조들
    /// </summary>
    private InsungsInfo_File m_FileInfo => m_Context.FileInfo;
    private InsungsInfo_Mem m_MemInfo => m_Context.MemInfo;
    private InsungsInfo_Mem.MainWnd m_Main => m_MemInfo.Main;
    private InsungsInfo_Mem.RcptRegPage m_RcptPage => m_MemInfo.RcptPage;
    #endregion

    #region Constructor
    /// <summary>
    /// 생성자 - Context를 받아서 초기화
    /// </summary>
    public InsungsAct_RcptRegPage(InsungContext context)
    {
        m_Context = context ?? throw new ArgumentNullException(nameof(context));
        Debug.WriteLine($"[InsungsAct_RcptRegPage] 생성자 호출: AppName={m_Context.AppName}");
    }
    #endregion

    #region 초기화용 함수들
    /// <summary>
    /// 접수등록 페이지 초기화
    /// </summary>
    public async Task<StdResult_Error> InitializeAsync(bool bEdit = true, bool bWrite = true, bool bMsgBox = true)
    {
        try
        {
            Debug.WriteLine($"[InsungsAct_RcptRegPage] 초기화 시작");

            // 1. 바메뉴 클릭 - 접수등록 페이지 열기
            await m_Context.MainWndAct.ClickAsync접수등록();
            await Task.Delay(500); // 페이지가 열릴 때까지 대기
            Debug.WriteLine($"[InsungsAct_RcptRegPage] 접수등록 바메뉴 클릭 완료");

            // 2. TopWnd 찾기 - MdiClient의 자식으로 "접수현황" 찾기
            for (int i = 0; i < 100; i++) // 10초 동안 대기
            {
                m_RcptPage.TopWnd_hWnd = Std32Window.FindWindowEx(
                    m_Main.WndInfo_MdiClient.hWnd,
                    IntPtr.Zero,
                    null,
                    m_FileInfo.접수등록Page_TopWnd_sWndName
                );

                if (m_RcptPage.TopWnd_hWnd != IntPtr.Zero) break;
                await Task.Delay(100);
            }

            if (m_RcptPage.TopWnd_hWnd == IntPtr.Zero)
            {
                return CommonFuncs_StdResult.ErrMsgResult_Error(
                    $"[{m_Context.AppName}/RcptRegPage]접수등록Page 찾기실패: {m_FileInfo.접수등록Page_TopWnd_sWndName}",
                    "InsungsAct_RcptRegPage/InitializeAsync_01", bWrite, bMsgBox);
            }
            Debug.WriteLine($"[InsungsAct_RcptRegPage] 접수등록Page 찾음: {m_RcptPage.TopWnd_hWnd:X}");

            // 3. StatusBtn 찾기 - 첫/마지막 버튼으로 로딩 확인
            // 3-1. 접수 버튼 찾기 (텍스트 검증으로 페이지 로딩 시작 확인)
            var (hWnd접수, error접수) = await FindStatusButtonAsync(
                "접수", m_FileInfo.접수등록Page_StatusBtn_ptChkRel접수M,
                "InsungsAct_RcptRegPage/InitializeAsync_02", bWrite, bMsgBox);
            if (error접수 != null) return error접수;
            m_RcptPage.StatusBtn_hWnd접수 = hWnd접수;

            // 3-2. 버튼 로딩 대기
            await Task.Delay(CommonVars.c_nWaitNormal);

            // 3-3. 전체 버튼 찾기 (텍스트 검증으로 페이지 로딩 완료 확인)
            var (hWnd전체, error전체) = await FindStatusButtonAsync(
                "전체", m_FileInfo.접수등록Page_StatusBtn_ptChkRel전체M,
                "InsungsAct_RcptRegPage/InitializeAsync_03", bWrite, bMsgBox);
            if (error전체 != null) return error전체;
            m_RcptPage.StatusBtn_hWnd전체 = hWnd전체;

            // 3-4. 중간 StatusBtn 찾기 (페이지 로딩 완료됨, 텍스트 검증 생략)
            var (hWnd배차, error배차) = await FindStatusButtonAsync(
                "배차", m_FileInfo.접수등록Page_StatusBtn_ptChkRel배차M,
                "InsungsAct_RcptRegPage/InitializeAsync_04", bWrite, bMsgBox, withTextValidation: false);
            if (error배차 != null) return error배차;
            m_RcptPage.StatusBtn_hWnd배차 = hWnd배차;

            var (hWnd운행, error운행) = await FindStatusButtonAsync(
                "운행", m_FileInfo.접수등록Page_StatusBtn_ptChkRel운행M,
                "InsungsAct_RcptRegPage/InitializeAsync_05", bWrite, bMsgBox, withTextValidation: false);
            if (error운행 != null) return error운행;
            m_RcptPage.StatusBtn_hWnd운행 = hWnd운행;

            var (hWnd완료, error완료) = await FindStatusButtonAsync(
                "완료", m_FileInfo.접수등록Page_StatusBtn_ptChkRel완료M,
                "InsungsAct_RcptRegPage/InitializeAsync_06", bWrite, bMsgBox, withTextValidation: false);
            if (error완료 != null) return error완료;
            m_RcptPage.StatusBtn_hWnd완료 = hWnd완료;

            var (hWnd취소, error취소) = await FindStatusButtonAsync(
                "취소", m_FileInfo.접수등록Page_StatusBtn_ptChkRel취소M,
                "InsungsAct_RcptRegPage/InitializeAsync_07", bWrite, bMsgBox, withTextValidation: false);
            if (error취소 != null) return error취소;
            m_RcptPage.StatusBtn_hWnd취소 = hWnd취소;

            // TODO: 3-1. StatusBtn 이미지 매칭으로 확인 (Up 상태) - OCR 사용시 BlockInput 필요

            // 4. StatusBtn - 전체버튼 클릭 (Down 상태로 변경되도록 여러 번 시도)
            bool bClickSuccess = false;
            for (int i = 1; i <= CommonVars.c_nRepeatShort; i++)
            {
                bool bLastAttempt = (i == CommonVars.c_nRepeatShort);

                // 4-1. 현재 상태 확인 (1회) - 이미 Down 상태인지 확인
                StdResult_NulBool currentState = await OfrWork_Insungs.OfrIsMatchedImage_DrawRelRectAsync(
                    m_RcptPage.StatusBtn_hWnd전체, HEADER_GAB, "Img_전체버튼_Down", bLastAttempt, bLastAttempt, bLastAttempt);

                if (StdConvert.NullableBoolToBool(currentState.bResult))
                {
                    // 이미 Down 상태 - 클릭 불필요
                    bClickSuccess = true;
                    Debug.WriteLine($"[InsungsAct_RcptRegPage] 전체버튼 이미 Down 상태 - 클릭 생략");
                    break;
                }

                // 4-2. Up 상태 확정 - 클릭 필요
                Debug.WriteLine($"[InsungsAct_RcptRegPage] 전체버튼 클릭 시도 {i}/{CommonVars.c_nRepeatShort}");
                Debug.WriteLine($"[InsungsAct_RcptRegPage] 클릭 전 대기 시작");
                await Task.Delay(CommonVars.c_nWaitNormal); // 클릭 전 안정화 대기
                Debug.WriteLine($"[InsungsAct_RcptRegPage] 클릭 전 대기 완료, 클릭 실행");
                await Std32Mouse_Post.MousePostAsync_ClickLeft(m_RcptPage.StatusBtn_hWnd전체);
                Debug.WriteLine($"[InsungsAct_RcptRegPage] 클릭 완료, 반영 대기 시작");
                await Task.Delay(CommonVars.c_nWaitLong); // 클릭 반영 대기
                Debug.WriteLine($"[InsungsAct_RcptRegPage] 클릭 반영 대기 완료");

                // 4-3. 클릭 후 Down 상태 확인 (이미지 대기)
                Debug.WriteLine($"[InsungsAct_RcptRegPage] Down 상태 대기 시작 (최대 3초)");
                StdResult_NulBool resultWait = await OfrWork_Common.OfrWaitUntilImageAppearsAsync(
                    "Img_전체버튼_Down", m_RcptPage.StatusBtn_hWnd전체, HEADER_GAB,
                    bEdit: true, checkInterval: 50, maxWaitTime: 3000);

                if (StdConvert.NullableBoolToBool(resultWait.bResult))
                {
                    bClickSuccess = true;
                    Debug.WriteLine($"[InsungsAct_RcptRegPage] 전체버튼 Down 상태 확인됨 - 클릭 성공");
                }
                else
                {
                    Debug.WriteLine($"[InsungsAct_RcptRegPage] Down 상태 대기 타임아웃: {resultWait.sErr}");
                }

                if (bClickSuccess)
                {
                    break; // 외부 루프 종료
                }

                Debug.WriteLine($"[InsungsAct_RcptRegPage] 전체버튼 Down 상태 미확인 - 재시도");
            }

            if (!bClickSuccess)
            {
                Debug.WriteLine($"[InsungsAct_RcptRegPage] 전체버튼 클릭 실패 - Down 상태로 전환되지 않음 (계속 진행)");
                // 에러는 발생시키지 않고 경고만 출력
            }

            // 4-4. 핸들 유효성 재확인
            string textCheck = Std32Window.GetWindowText(m_RcptPage.StatusBtn_hWnd전체);
            if (!textCheck.Contains("전체"))
            {
                return CommonFuncs_StdResult.ErrMsgResult_Error(
                    $"[{m_Context.AppName}/RcptRegPage]전체버튼 핸들 검증 실패: 텍스트={textCheck}",
                    "InsungsAct_RcptRegPage/InitializeAsync_04_1", bWrite, bMsgBox);
            }
            Debug.WriteLine($"[InsungsAct_RcptRegPage] 전체버튼 핸들 검증 완료");

            // 4-5. StatusBtn Down 상태 OFR 확인 (전체버튼 클릭 후 나머지 버튼들 상태 확인)
            // 전체버튼 클릭 성공 후 100ms 딜레이
            await Task.Delay(100);

            // 4-5-1. 접수버튼 Down 상태 대기
            StdResult_NulBool resultWait접수 = await OfrWork_Common.OfrWaitUntilImageAppearsAsync(
                "Img_접수버튼_Down", m_RcptPage.StatusBtn_hWnd접수, HEADER_GAB,
                bEdit: true, checkInterval: 50, maxWaitTime: 3000);
            if (StdConvert.NullableBoolToBool(resultWait접수.bResult))
                Debug.WriteLine($"[InsungsAct_RcptRegPage] 접수버튼 Down 상태 확인 완료");
            else
                Debug.WriteLine($"[InsungsAct_RcptRegPage] 접수버튼 Down 상태 OFR 실패: {resultWait접수.sErr}");

            // 4-5-2. 배차버튼 Down 상태 대기
            StdResult_NulBool resultWait배차 = await OfrWork_Common.OfrWaitUntilImageAppearsAsync(
                "Img_배차버튼_Down", m_RcptPage.StatusBtn_hWnd배차, HEADER_GAB,
                bEdit: true, checkInterval: 50, maxWaitTime: 3000);
            if (StdConvert.NullableBoolToBool(resultWait배차.bResult))
                Debug.WriteLine($"[InsungsAct_RcptRegPage] 배차버튼 Down 상태 확인 완료");
            else
                Debug.WriteLine($"[InsungsAct_RcptRegPage] 배차버튼 Down 상태 OFR 실패: {resultWait배차.sErr}");

            // 4-5-3. 운행버튼 Down 상태 대기
            StdResult_NulBool resultWait운행 = await OfrWork_Common.OfrWaitUntilImageAppearsAsync(
                "Img_운행버튼_Down", m_RcptPage.StatusBtn_hWnd운행, HEADER_GAB,
                bEdit: true, checkInterval: 50, maxWaitTime: 3000);
            if (StdConvert.NullableBoolToBool(resultWait운행.bResult))
                Debug.WriteLine($"[InsungsAct_RcptRegPage] 운행버튼 Down 상태 확인 완료");
            else
                Debug.WriteLine($"[InsungsAct_RcptRegPage] 운행버튼 Down 상태 OFR 실패: {resultWait운행.sErr}");

            // 4-5-4. 완료버튼 Down 상태 대기
            StdResult_NulBool resultWait완료 = await OfrWork_Common.OfrWaitUntilImageAppearsAsync(
                "Img_완료버튼_Down", m_RcptPage.StatusBtn_hWnd완료, HEADER_GAB,
                bEdit: true, checkInterval: 50, maxWaitTime: 3000);
            if (StdConvert.NullableBoolToBool(resultWait완료.bResult))
                Debug.WriteLine($"[InsungsAct_RcptRegPage] 완료버튼 Down 상태 확인 완료");
            else
                Debug.WriteLine($"[InsungsAct_RcptRegPage] 완료버튼 Down 상태 OFR 실패: {resultWait완료.sErr}");

            // 4-5-5. 취소버튼 Down 상태 대기
            StdResult_NulBool resultWait취소 = await OfrWork_Common.OfrWaitUntilImageAppearsAsync(
                "Img_취소버튼_Down", m_RcptPage.StatusBtn_hWnd취소, HEADER_GAB,
                bEdit: true, checkInterval: 50, maxWaitTime: 3000);
            if (StdConvert.NullableBoolToBool(resultWait취소.bResult))
                Debug.WriteLine($"[InsungsAct_RcptRegPage] 취소버튼 Down 상태 확인 완료");
            else
                Debug.WriteLine($"[InsungsAct_RcptRegPage] 취소버튼 Down 상태 OFR 실패: {resultWait취소.sErr}");

            // 4-5-6. 전체버튼 Down 상태 재확인
            StdResult_NulBool resultWait전체 = await OfrWork_Common.OfrWaitUntilImageAppearsAsync(
                "Img_전체버튼_Down", m_RcptPage.StatusBtn_hWnd전체, HEADER_GAB,
                bEdit: true, checkInterval: 50, maxWaitTime: 3000);
            if (StdConvert.NullableBoolToBool(resultWait전체.bResult))
                Debug.WriteLine($"[InsungsAct_RcptRegPage] 전체버튼 Down 상태 재확인 완료");
            else
                Debug.WriteLine($"[InsungsAct_RcptRegPage] 전체버튼 Down 상태 OFR 실패: {resultWait전체.sErr}");

            Debug.WriteLine($"[InsungsAct_RcptRegPage] StatusBtn Down 상태 확인 완료");

            // 5. CommandBtn 찾기 및 OFR 검증 (신규, 조회, 기사)
            var (hWnd신규, error신규) = await FindCommandButtonWithOfrAsync(
                "신규", m_FileInfo.접수등록Page_CmdBtn_ptChkRel신규M, "Img_신규버튼",
                "InsungsAct_RcptRegPage/InitializeAsync_08", bEdit, bWrite, bMsgBox);
            if (error신규 != null) return error신규;
            m_RcptPage.CmdBtn_hWnd신규 = hWnd신규;

            var (hWnd조회, error조회) = await FindCommandButtonWithOfrAsync(
                "조회", m_FileInfo.접수등록Page_CmdBtn_ptChkRel조회M, "Img_조회버튼",
                "InsungsAct_RcptRegPage/InitializeAsync_09", bEdit, bWrite, bMsgBox);
            if (error조회 != null) return error조회;
            m_RcptPage.CmdBtn_hWnd조회 = hWnd조회;

            var (hWnd기사, error기사) = await FindCommandButtonWithOfrAsync(
                "기사", m_FileInfo.접수등록Page_CmdBtn_ptChkRel기사M, "Img_기사버튼",
                "InsungsAct_RcptRegPage/InitializeAsync_10", bEdit, bWrite, bMsgBox);
            if (error기사 != null) return error기사;
            m_RcptPage.CmdBtn_hWnd기사 = hWnd기사;

            // 6. CallCount 핸들 찾기 (접수, 운행, 취소, 완료, 총계)
            m_RcptPage.CallCount_hWnd접수 = FindCallCountControl(
                "접수", m_FileInfo.접수등록Page_CallCount_ptChkRel접수M,
                "InsungsAct_RcptRegPage/InitializeAsync_11", bWrite, bMsgBox, out StdResult_Error error접수Count);
            if (error접수Count != null) return error접수Count;

            m_RcptPage.CallCount_hWnd운행 = FindCallCountControl(
                "운행", m_FileInfo.접수등록Page_CallCount_ptChkRel운행M,
                "InsungsAct_RcptRegPage/InitializeAsync_12", bWrite, bMsgBox, out StdResult_Error error운행Count);
            if (error운행Count != null) return error운행Count;

            m_RcptPage.CallCount_hWnd취소 = FindCallCountControl(
                "취소", m_FileInfo.접수등록Page_CallCount_ptChkRel취소M,
                "InsungsAct_RcptRegPage/InitializeAsync_13", bWrite, bMsgBox, out StdResult_Error error취소Count);
            if (error취소Count != null) return error취소Count;

            m_RcptPage.CallCount_hWnd완료 = FindCallCountControl(
                "완료", m_FileInfo.접수등록Page_CallCount_ptChkRel완료M,
                "InsungsAct_RcptRegPage/InitializeAsync_14", bWrite, bMsgBox, out StdResult_Error error완료Count);
            if (error완료Count != null) return error완료Count;

            m_RcptPage.CallCount_hWnd총계 = FindCallCountControl(
                "총계", m_FileInfo.접수등록Page_CallCount_ptChkRel총계M,
                "InsungsAct_RcptRegPage/InitializeAsync_15", bWrite, bMsgBox, out StdResult_Error error총계Count);
            if (error총계Count != null) return error총계Count;

            // 7. 오더 Datagrid 초기화
            // 7-1. Datagrid 핸들 찾기
            m_RcptPage.DG오더_hWnd = Std32Window.GetWndHandle_FromRelDrawPt(
                m_Main.TopWnd_hWnd, m_FileInfo.접수등록Page_DG오더_ptCenterRelM
            );
            if (m_RcptPage.DG오더_hWnd == IntPtr.Zero)
            {
                return CommonFuncs_StdResult.ErrMsgResult_Error(
                    $"[{m_Context.AppName}/RcptRegPage]Datagrid 찾기실패: {m_FileInfo.접수등록Page_DG오더_ptCenterRelM}",
                    "InsungsAct_RcptRegPage/InitializeAsync_16", bWrite, bMsgBox);
            }
            Debug.WriteLine($"[InsungsAct_RcptRegPage] Datagrid 찾음: {m_RcptPage.DG오더_hWnd:X}");

            // 7-2. Datagrid 크기 확인
            m_RcptPage.DG오더_AbsRect = Std32Window.GetWindowRect_DrawAbs(m_RcptPage.DG오더_hWnd);
            if (m_RcptPage.DG오더_AbsRect.Width != m_FileInfo.접수등록Page_DG오더_rcRel.Width ||
                m_RcptPage.DG오더_AbsRect.Height != m_FileInfo.접수등록Page_DG오더_rcRel.Height)
            {
                return CommonFuncs_StdResult.ErrMsgResult_Error(
                    $"[{m_Context.AppName}/RcptRegPage]Datagrid 크기 불일치: " +
                    $"Expected({m_FileInfo.접수등록Page_DG오더_rcRel.Width}x{m_FileInfo.접수등록Page_DG오더_rcRel.Height}), " +
                    $"Actual({m_RcptPage.DG오더_AbsRect.Width}x{m_RcptPage.DG오더_AbsRect.Height})",
                    "InsungsAct_RcptRegPage/InitializeAsync_17", bWrite, bMsgBox);
            }
            Debug.WriteLine($"[InsungsAct_RcptRegPage] Datagrid 크기 확인 완료: {m_RcptPage.DG오더_AbsRect.Width}x{m_RcptPage.DG오더_AbsRect.Height}");

            // 7-3. 수직스크롤바 핸들 찾기 (MainWnd 기준 상대좌표)
            m_RcptPage.DG오더_hWnd수직스크롤 = Std32Window.GetWndHandle_FromRelDrawPt(
                m_Main.TopWnd_hWnd, m_FileInfo.접수등록Page_DG오더_ptChkRel수직스크롤M
            );

            if (m_RcptPage.DG오더_hWnd수직스크롤 == IntPtr.Zero)
            {
                Debug.WriteLine($"[InsungsAct_RcptRegPage] 수직스크롤바 찾기 실패 (경고): {m_FileInfo.접수등록Page_DG오더_ptChkRel수직스크롤M}");
                // 스크롤바가 없을 수도 있으므로 경고만 출력하고 진행
            }
            else
            {
                Debug.WriteLine($"[InsungsAct_RcptRegPage] 수직스크롤바 찾음: {m_RcptPage.DG오더_hWnd수직스크롤:X}");
            }

            // 7-4. Datagrid 상세 정보 설정 (컬럼 헤더, RelChildRects)
            StdResult_Error resultDG = await SetDG오더RectsAsync(bEdit, bWrite, bMsgBox);
            if (resultDG != null)
                return resultDG; // 에러 발생 시 반환

            Debug.WriteLine($"[InsungsAct_RcptRegPage] 초기화 완료");
            return null; // 성공
        }
        catch (Exception ex)
        {
            return CommonFuncs_StdResult.ErrMsgResult_Error(
                $"[{m_Context.AppName}/RcptRegPage]예외발생: {ex.Message}",
                "InsungsAct_RcptRegPage/InitializeAsync_999", bWrite, bMsgBox);
        }
    }

    /// <summary>
    /// Datagrid 상세 영역 설정 (컬럼 헤더 읽기 + RelChildRects 계산 + 상태 검증)
    /// 상태가 올바르지 않으면 InitDG오더Async 호출 후 재시도
    /// </summary>
    private async Task<StdResult_Error> SetDG오더RectsAsync(bool bEdit = true, bool bWrite = true, bool bMsgBox = true)
    {
        Draw.Bitmap bmpDG = null;

        try
        {
            Debug.WriteLine($"[InsungsAct_RcptRegPage] SetDG오더RectsAsync 시작");

            // 재시도 루프 (goto 대신 for 사용)
            for (int retry = 0; retry < c_nRepeatShort; retry++)
            {
                // 중간 재시도에서는 메시지박스 표시 안 함, 마지막 재시도에서만 표시
                bool bShowMsgBox = (retry >= c_nRepeatShort) && bMsgBox;

                if (retry > 0)
                {
                    Debug.WriteLine($"[InsungsAct_RcptRegPage] Datagrid 재시도 {retry}/{c_nRepeatShort}");
                    await Task.Delay(500); // 재시도 전 대기
                }

                // 1. Datagrid 비트맵 캡처 (MainWnd 기준 상대좌표로 캡처)
                bmpDG = OfrService.CaptureScreenRect_InWndHandle(
                    m_Main.TopWnd_hWnd,
                    m_FileInfo.접수등록Page_DG오더_rcRel);

                if (bmpDG == null)
                {
                    if (retry < c_nRepeatShort)
                    {
                        Debug.WriteLine($"[InsungsAct_RcptRegPage] DG오더 캡처 실패 (재시도 {retry}/{c_nRepeatShort})");
                        await Task.Delay(200);
                        continue; // 재시도
                    }
                    return CommonFuncs_StdResult.ErrMsgResult_Error(
                        $"[{m_Context.AppName}/RcptRegPage]DG오더 캡처 실패: rcRel={m_FileInfo.접수등록Page_DG오더_rcRel}",
                        "InsungsAct_RcptRegPage/SetDG오더RectsAsync_01", bWrite, bShowMsgBox);
                }

                Debug.WriteLine($"[InsungsAct_RcptRegPage] DG오더 캡처 성공: {bmpDG.Width}x{bmpDG.Height}");

                // 2. 컬럼 경계 검출
                // 2-1. 헤더 상단 여백(텍스트 없는 영역)에서 최소 밝기 검출
                const int headerGab = 7; // 헤더 상단 여백
                int headerHeight = m_FileInfo.접수등록Page_DG오더_headerHeight;
                int targetRow = headerGab; // 텍스트가 없는 Y 위치 (경계선만 검출)

                byte minBrightness = OfrService.GetMinBrightnessAtRow_FromColorBitmapFast(
                    bmpDG, targetRow);

                if (minBrightness == 255) // 검출 실패
                {
                    bmpDG?.Dispose();
                    if (retry < c_nRepeatShort)
                    {
                        Debug.WriteLine($"[InsungsAct_RcptRegPage] 헤더 행 최소 밝기 검출 실패 (재시도 {retry}/{c_nRepeatShort})");
                        await Task.Delay(200);
                        continue; // 재시도
                    }
                    return CommonFuncs_StdResult.ErrMsgResult_Error(
                        $"[{m_Context.AppName}/RcptRegPage]헤더 행 최소 밝기 검출 실패: targetRow={targetRow}",
                        "InsungsAct_RcptRegPage/SetDG오더RectsAsync_02", bWrite, bShowMsgBox);
                }

                minBrightness += 2; // 확실한 경계를 위해 약간 밝게 조정 (백업 파일 방식)

                Debug.WriteLine($"[InsungsAct_RcptRegPage] 헤더 행 최소 밝기 검출: targetRow={targetRow}, minBrightness={minBrightness}");

                // 2-2. Bool 배열 생성 (true=검은색, false=흰색)
                bool[] boolArr = OfrService.GetBoolArray_FromColorBitmapRowFast(
                    bmpDG, targetRow, minBrightness, 2); // 마진 2픽셀 (백업 파일 방식)

                if (boolArr == null || boolArr.Length == 0)
                {
                    bmpDG?.Dispose();
                    if (retry < c_nRepeatShort)
                    {
                        Debug.WriteLine($"[InsungsAct_RcptRegPage] Bool 배열 생성 실패 (재시도 {retry}/{c_nRepeatShort})");
                        await Task.Delay(200);
                        continue; // 재시도
                    }
                    return CommonFuncs_StdResult.ErrMsgResult_Error(
                        $"[{m_Context.AppName}/RcptRegPage]Bool 배열 생성 실패: targetRow={targetRow}",
                        "InsungsAct_RcptRegPage/SetDG오더RectsAsync_03", bWrite, bShowMsgBox);
                }

                Debug.WriteLine($"[InsungsAct_RcptRegPage] Bool 배열 생성 완료: Length={boolArr.Length}");

                // 2-3. 컬럼 경계 리스트 추출
                List<OfrModel_LeftWidth> listLW =
                    OfrService.GetLeftWidthList_FromBool1Array(boolArr, minBrightness);

                if (listLW == null || listLW.Count == 0)
                {
                    bmpDG?.Dispose();
                    if (retry < c_nRepeatShort)
                    {
                        Debug.WriteLine($"[InsungsAct_RcptRegPage] 컬럼 경계 검출 실패 (재시도 {retry}/{c_nRepeatShort})");
                        await Task.Delay(200);
                        continue; // 재시도
                    }
                    return CommonFuncs_StdResult.ErrMsgResult_Error(
                        $"[{m_Context.AppName}/RcptRegPage]컬럼 경계 검출 실패: 검출된 리스트 수=0",
                        "InsungsAct_RcptRegPage/SetDG오더RectsAsync_04", bWrite, bShowMsgBox);
                }

                // 첫 번째와 마지막 항목 제거 (테두리 명도 + 오른쪽 끝 경계)
                if (listLW.Count >= 2)
                {
                    listLW.RemoveAt(0); // 첫 번째 제거 (테두리 명도 섞임)
                    listLW.RemoveAt(listLW.Count - 1); // 마지막 제거 (오른쪽 끝 경계)
                    Debug.WriteLine($"[InsungsAct_RcptRegPage] 첫/마지막 항목 제거 완료");
                }

                int columns = listLW.Count; // 제거 후 남은 개수 = 실제 컬럼 개수

                Debug.WriteLine($"[InsungsAct_RcptRegPage] 컬럼 경계 검출 완료: 실제 컬럼={columns}");

                // [디버깅] listLW 첫 3개 값 출력
                string debugInfo = "[디버깅] listLW 첫 3개:\n";
                for (int dbg = 0; dbg < Math.Min(3, listLW.Count); dbg++)
                {
                    debugInfo += $"[{dbg}] Left={listLW[dbg].nLeft}, Width={listLW[dbg].nWidth}\n";
                }
                Debug.WriteLine(debugInfo);

                // 컬럼 개수가 원하는 개수가 아니면 즉시 InitDG오더Async 호출하여 강제 초기화
                if (columns != m_ReceiptDgHeaderInfos.Length)
                {
                    Debug.WriteLine($"[InsungsAct_RcptRegPage] 컬럼 개수 불일치: 검출={columns}개, 예상={m_ReceiptDgHeaderInfos.Length}개 (재시도 {retry}/{c_nRepeatShort})");

                    bmpDG?.Dispose();

                    // InitDG오더Async 호출하여 Datagrid 강제 초기화
                    StdResult_Error initResult = await InitDG오더Async(
                        CEnum_DgValidationIssue.InvalidColumnCount,
                        bEdit, bWrite,
                        bMsgBox: false  // 중간 에러는 메시지박스 표시 안 함
                    );

                    if (initResult != null)
                    {
                        Debug.WriteLine($"[InsungsAct_RcptRegPage] InitDG오더Async 실패: {initResult.sErr}");

                        // 최대 재시도 횟수 도달 시에만 에러 반환
                        if (retry >= c_nRepeatShort)
                        {
                            return CommonFuncs_StdResult.ErrMsgResult_Error(
                                $"[{m_Context.AppName}/RcptRegPage]컬럼 개수 불일치: 검출={columns}개, 예상={m_ReceiptDgHeaderInfos.Length}개\n상세: {initResult.sErr}\n(재시도 {c_nRepeatShort}회 초과)",
                                "InsungsAct_RcptRegPage/SetDG오더RectsAsync_05", bWrite, bShowMsgBox);
                        }
                    }

                    await Task.Delay(200);
                    continue; // 재시도
                }

                // 3. 컬럼 헤더 OFR 인식 (전체 컬럼)
                m_RcptPage.DG오더_ColumnTexts = new string[listLW.Count];

                for (int i = 0; i < listLW.Count; i++)
                {
                    try
                    {
                        // 3-1. 컬럼 헤더 영역 Rectangle 생성 (테두리 제외 - 텍스트 영역만)
                        Draw.Rectangle rcColHeader = new Draw.Rectangle(
                            listLW[i].nLeft,
                            headerGab,                                  // 상단 여백만큼 아래
                            listLW[i].nWidth,
                            headerHeight - (headerGab * 2)              // 상하 여백 제외
                        );

                        //Debug.WriteLine($"[InsungsAct_RcptRegPage] 컬럼[{i}] 헤더 영역: {rcColHeader}");

                        // 3-2. 컬럼 헤더 비트맵 추출
                        Draw.Bitmap bmpColHeader = OfrService.GetBitmapInBitmapFast(
                            bmpDG, rcColHeader
                        );

                        if (bmpColHeader == null)
                        {
                            //Debug.WriteLine($"[InsungsAct_RcptRegPage] 컬럼[{i}] 비트맵 추출 실패");
                            m_RcptPage.DG오더_ColumnTexts[i] = null;
                            continue;
                        }

                        // 3-3. 평균 밝기 계산
                        byte avgBrightness = OfrService.GetAverageBrightness_FromColorBitmapFast(bmpColHeader);

                        // 3-4. 전경 영역 추출
                        Draw.Rectangle? rcForeground = OfrService.GetForeGroundDrawRectangle_FromColorBitmapFast(
                            bmpColHeader, avgBrightness, 0
                        );

                        if (rcForeground == null || rcForeground.Value.Width < 1 || rcForeground.Value.Height < 1)
                        {
                            //Debug.WriteLine($"[InsungsAct_RcptRegPage] 컬럼[{i}] 전경 영역 추출 실패");
                            bmpColHeader?.Dispose();
                            m_RcptPage.DG오더_ColumnTexts[i] = null;
                            continue;
                        }

                        // 3-5. Exact 비트맵 추출
                        Draw.Bitmap bmpExact = OfrService.GetBitmapInBitmapFast(
                            bmpColHeader, rcForeground.Value
                        );
                        bmpColHeader?.Dispose();

                        if (bmpExact == null)
                        {
                            //Debug.WriteLine($"[InsungsAct_RcptRegPage] 컬럼[{i}] Exact 비트맵 추출 실패");
                            m_RcptPage.DG오더_ColumnTexts[i] = null;
                            continue;
                        }

                        // 3-6. TEXT OFR 수행 - OfrStr_ComplexCharSetAsync (범용 함수 - 한글/영문/숫자 모두 처리)
                        // bmpExact는 이미 전경 영역만 추출된 상태, rcSpare는 전체 영역
                        StdResult_String result = await OfrWork_Common.OfrStr_ComplexCharSetAsync(bmpExact, bEdit);

                        bmpExact?.Dispose();

                        // 3-7. 결과 저장
                        if (result != null && !string.IsNullOrEmpty(result.strResult))
                        {
                            m_RcptPage.DG오더_ColumnTexts[i] = result.strResult;
                            //Debug.WriteLine($"[InsungsAct_RcptRegPage] 컬럼[{i}] OFR 성공: '{m_RcptPage.DG오더_ColumnTexts[i]}'");
                        }
                        else
                        {
                            m_RcptPage.DG오더_ColumnTexts[i] = null;
                            //Debug.WriteLine($"[InsungsAct_RcptRegPage] 컬럼[{i}] OFR 실패: {result?.sErr}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[InsungsAct_RcptRegPage] 컬럼[{i}] OFR 예외: {ex.Message}");
                        m_RcptPage.DG오더_ColumnTexts[i] = null;
                    }
                }

                // 3-8. 컬럼 헤더 OFR 결과 요약
                int successCount = m_RcptPage.DG오더_ColumnTexts.Count(t => !string.IsNullOrEmpty(t));
                Debug.WriteLine($"[InsungsAct_RcptRegPage] 컬럼 헤더 OFR 완료: 성공={successCount}/{listLW.Count}");

                // 3-9. Datagrid 상태 검증
                Debug.WriteLine("[SetDG오더RectsAsync] Datagrid 상태 검증 시작");
                Debug.WriteLine($"[SetDG오더RectsAsync] 검출 컬럼({listLW.Count}): {string.Join(", ", m_RcptPage.DG오더_ColumnTexts.Where(t => !string.IsNullOrEmpty(t)))}");

                CEnum_DgValidationIssue validationIssues = ValidateDatagridState(m_RcptPage.DG오더_ColumnTexts, listLW);

                if (validationIssues != CEnum_DgValidationIssue.None)
                {
                    Debug.WriteLine($"[InsungsAct_RcptRegPage] Datagrid 상태 검증 실패: {validationIssues} (재시도 {retry}/{c_nRepeatShort})");

                    // InitDG오더Async 호출하여 Datagrid 강제 초기화
                    // 중간 단계에서는 메시지박스 표시 안 함 (조용히 재시도)
                    StdResult_Error initResult = await InitDG오더Async(
                        validationIssues,
                        bEdit, bWrite,
                        bMsgBox: false  // 중간 에러는 메시지박스 표시 안 함
                    );

                    if (initResult != null)
                    {
                        Debug.WriteLine($"[InsungsAct_RcptRegPage] InitDG오더Async 실패: {initResult.sErr}");

                        // 최대 재시도 횟수 도달 시에만 에러 반환 (메시지박스 표시)
                        if (retry >= c_nRepeatShort)
                        {
                            bmpDG?.Dispose();
                            return CommonFuncs_StdResult.ErrMsgResult_Error(
                                $"[{m_Context.AppName}/RcptRegPage]Datagrid 초기화 실패: {validationIssues}\n상세: {initResult.sErr}\n(재시도 {c_nRepeatShort}회 초과)",
                                "InsungsAct_RcptRegPage/SetDG오더RectsAsync_Validation", bWrite, bShowMsgBox);
                        }
                    }

                    bmpDG?.Dispose(); // 재시도 전 비트맵 해제
                    bmpDG = null;
                    continue; // 재시도
                }

                // 검증 성공 - 계속 진행
                Debug.WriteLine($"[InsungsAct_RcptRegPage] Datagrid 상태 검증 성공");

                // 4. RelChildRects 계산
                // 4-1. 행 정보 계산 (Header + EmptyRow + DataRows)
                List<Kai.Common.NetDll_WpfCtrl.NetOFR.OfrModel_TopHeight> listTH =
                    new List<Kai.Common.NetDll_WpfCtrl.NetOFR.OfrModel_TopHeight>();

                int curRowTop = 0;
                int dataRowHeight = m_FileInfo.접수등록Page_DG오더_dataRowHeight - 2; // 실제 텍스트 영역 높이 (테두리 제외)

                // 4-1-1. 헤더 행
                listTH.Add(new Kai.Common.NetDll_WpfCtrl.NetOFR.OfrModel_TopHeight(
                    curRowTop + headerGab,
                    headerHeight - (headerGab * 2)
                ));
                Debug.WriteLine($"[InsungsAct_RcptRegPage] 헤더 행 추가: Top={curRowTop + headerGab}, Height={headerHeight - (headerGab * 2)}");

                // 4-1-2. Empty Row
                curRowTop += m_FileInfo.접수등록Page_DG오더_headerHeight;
                listTH.Add(new Kai.Common.NetDll_WpfCtrl.NetOFR.OfrModel_TopHeight(
                    curRowTop + 1,
                    dataRowHeight
                ));
                Debug.WriteLine($"[InsungsAct_RcptRegPage] Empty Row 추가: Top={curRowTop + 1}, Height={dataRowHeight}");

                // 4-1-3. Data Rows
                curRowTop += m_FileInfo.접수등록Page_DG오더_emptyRowHeight;
                for (int i = 0; i < InsungsInfo_File.접수등록Page_DG오더_dataRowCount; i++)
                {
                    listTH.Add(new Kai.Common.NetDll_WpfCtrl.NetOFR.OfrModel_TopHeight(
                        curRowTop + 1,
                        dataRowHeight
                    ));
                    curRowTop += m_FileInfo.접수등록Page_DG오더_dataRowHeight;
                }
                Debug.WriteLine($"[InsungsAct_RcptRegPage] Data Rows 추가 완료: 총 {InsungsInfo_File.접수등록Page_DG오더_dataRowCount}개");

                // 4-2. RelChildRects 2차원 배열 생성 [열, 행]
                int rows = listTH.Count;
                m_RcptPage.DG오더_RelChildRects = new Draw.Rectangle[columns, rows];

                Draw.Rectangle rcDG_Rel = m_FileInfo.접수등록Page_DG오더_rcRel; // MainWnd 기준 상대좌표 (참고용)

                // Left offset: 첫 번째 컬럼의 Left 값을 빼서 0 기준으로 조정
                int leftOffset = listLW[0].nLeft;
                Debug.WriteLine($"[InsungsAct_RcptRegPage] Left offset 적용: {leftOffset}");

                // RelChildRects를 DG 기준 좌표로 저장 (rcDG_Rel을 더하지 않음)
                // 셀 영역 조정:
                // - 로우(Top, Height): 헤더(y=0)는 Top 그대로/Height -1, 데이터(y>=1)는 Top -1/Height -1
                // - 셀(Left, Width): 첫 셀(x=0)은 Left +2, 나머지(x>0)는 Left +1, 모든 셀 Width -1
                for (int y = 0; y < rows; y++)
                {
                    for (int x = 0; x < columns; x++)
                    {
                        int baseLeft = listLW[x].nLeft - leftOffset;  // offset 적용
                        int adjustedLeft = (x == 0) ? baseLeft + 2 : baseLeft + 1;  // 첫 셀 +2, 나머지 +1
                        int adjustedTop = (y == 0) ? listTH[y].nTop : listTH[y].nTop - 1;  // 헤더는 그대로, 데이터는 -1
                        int adjustedWidth = listLW[x].nWidth - 1;  // 모든 셀 Width -1
                        int adjustedHeight = listTH[y].nHeight - 1;  // 모든 행 Height -1

                        m_RcptPage.DG오더_RelChildRects[x, y] = new Draw.Rectangle(
                            adjustedLeft,
                            adjustedTop,
                            adjustedWidth,
                            adjustedHeight
                        );
                    }
                }

                Debug.WriteLine($"[InsungsAct_RcptRegPage] RelChildRects 생성 완료: {columns}열 x {rows}행");

                // 4-3. Background Brightness 계산 (첫 번째 데이터 행의 한 점에서 측정)
                if (rows >= 2)
                {
                    // Empty Row의 샘플 포인트 (DG 기준 좌표, 비트맵도 DG 기준)
                    Draw.Point ptSampleRel = StdUtil.GetDrawPoint(m_RcptPage.DG오더_RelChildRects[0, 1], 8, 8);

                    m_RcptPage.DG오더_nBackgroundBright = OfrService.GetPixelBrightness(bmpDG, ptSampleRel);

                    Debug.WriteLine($"[InsungsAct_RcptRegPage] Background Brightness: {m_RcptPage.DG오더_nBackgroundBright} (샘플 위치: {ptSampleRel})");
                }

                // 모든 처리 완료 - 루프 탈출
                Debug.WriteLine($"[InsungsAct_RcptRegPage] SetDG오더RectsAsync 완료");
                break;

            } // for (retry) 끝

            // 성공
            return null;
        }
        catch (Exception ex)
        {
            return CommonFuncs_StdResult.ErrMsgResult_Error(
                $"[{m_Context.AppName}/RcptRegPage]SetDG오더RectsAsync 예외발생: {ex.Message}",
                "InsungsAct_RcptRegPage/SetDG오더RectsAsync_999", bWrite, bMsgBox);
        }
        finally
        {
            // 리소스 정리
            bmpDG?.Dispose();
        }
    }

    /// <summary>
    /// Datagrid 강제 초기화 (Context 메뉴 → "접수화면초기화" 클릭 → 컬럼 조정)
    /// </summary>
    /// <param name="issues">검증 이슈 플래그</param>
    /// <param name="bEdit">편집 허용 여부</param>
    /// <summary>
    /// 헤더 캡처 및 컬럼 경계 검출 헬퍼
    /// </summary>
    private (Draw.Bitmap bmpHeader, List<OfrModel_LeftWidth> listLW, int columns) CaptureAndDetectColumnBoundaries(
        Draw.Rectangle rcHeader, int gab)
    {
        Draw.Bitmap bmpHeader = OfrService.CaptureScreenRect_InWndHandle(m_RcptPage.DG오더_hWnd, rcHeader);
        if (bmpHeader == null)
            return (null, null, 0);

        byte byteMinBrightness = OfrService.GetMinBrightnessAtRow_FromColorBitmapFast(bmpHeader, gab);
        byteMinBrightness += 2;
        bool[] resultArr = OfrService.GetBoolArray_FromColorBitmapRowFast(bmpHeader, gab, byteMinBrightness, 2);
        List<OfrModel_LeftWidth> listLW = OfrService.GetLeftWidthList_FromBool1Array(resultArr, byteMinBrightness);
        int columns = listLW.Count - 1;

        return (bmpHeader, listLW, columns);
    }

    /// <summary>
    /// 모든 컬럼 OFR 헬퍼
    /// </summary>
    private async Task<string[]> OfrAllColumnsAsync(
        Draw.Bitmap bmpHeader, List<OfrModel_LeftWidth> listLW, int columns, int gab, int height, bool bEdit)
    {
        string[] texts = new string[columns];

        for (int x = 0; x < columns; x++)
        {
            Draw.Rectangle rcColHeader = new Draw.Rectangle(listLW[x].nLeft, gab, listLW[x].nWidth, height);
            Draw.Bitmap bmpColHeader = OfrService.GetBitmapInBitmapFast(bmpHeader, rcColHeader);
            if (bmpColHeader == null)
            {
                texts[x] = null;
                continue;
            }

            byte avgBrightness = OfrService.GetAverageBrightness_FromColorBitmapFast(bmpColHeader);
            Draw.Rectangle? rcForeground = OfrService.GetForeGroundDrawRectangle_FromColorBitmapFast(
                bmpColHeader, avgBrightness, 0);

            if (rcForeground == null || rcForeground.Value.Width < 1)
            {
                bmpColHeader?.Dispose();
                texts[x] = null;
                continue;
            }

            Draw.Bitmap bmpExact = OfrService.GetBitmapInBitmapFast(bmpColHeader, rcForeground.Value);
            bmpColHeader?.Dispose();

            if (bmpExact == null)
            {
                texts[x] = null;
                continue;
            }

            StdResult_String result = await OfrWork_Common.OfrStr_ComplexCharSetAsync(bmpExact, bEdit);
            bmpExact?.Dispose();
            texts[x] = result?.strResult;
        }

        return texts;
    }

    /// <param name="bWrite">로그 작성 여부</param>
    /// <param name="bMsgBox">메시지박스 표시 여부</param>
    /// <returns>에러 발생 시 StdResult_Error, 성공 시 null</returns>
    private async Task<StdResult_Error> InitDG오더Async(CEnum_DgValidationIssue issues, bool bEdit = true, bool bWrite = true, bool bMsgBox = true)
    {
        // 마우스 커서 위치 백업 (작업 완료 후 복원용)
        Draw.Point ptCursorBackup = Std32Cursor.GetCursorPos_AbsDrawPt();

        try
        {
            // 초기화 전체 기간 동안 외부 입력 차단
            Simulation_Mouse.SafeBlockInputStart();

            // DG오더_hWnd 기준으로 헤더 영역 정의
            Draw.Rectangle rcDG = Std32Window.GetWindowRect_DrawAbs(m_RcptPage.DG오더_hWnd);
            Draw.Rectangle rcHeader = new Draw.Rectangle(
                0, 0,  // DG 윈도우 내부 시작점
                rcDG.Width,
                m_FileInfo.접수등록Page_DG오더_headerHeight
            );
            //Debug.WriteLine($"[InitDG오더] rcHeader: {rcHeader}");

            // Step 1: 사전작업 - "접수화면초기화" 클릭
            Debug.WriteLine("[InitDG오더] Step 1: 접수화면초기화 시작");

            // 1-1. 우클릭
            await Std32Mouse_Post.MousePostAsync_ClickRight(m_RcptPage.DG오더_hWnd);
            //Debug.WriteLine("[InitDG오더] 1-1. 우클릭 완료");

            // 1-2. Context 메뉴 대기 (100회 폴링, 2초)
            IntPtr hWndMenu = IntPtr.Zero;
            for (int i = 0; i < 100; i++)
            {
                await Task.Delay(20);
                hWndMenu = Std32Window.FindMainWindow_StartsWith(
                    m_FileInfo.Main_AnyMenu_sClassName,
                    m_FileInfo.Main_AnyMenu_sWndName);
                if (hWndMenu != IntPtr.Zero) break;
            }

            if (hWndMenu == IntPtr.Zero)
            {
                return CommonFuncs_StdResult.ErrMsgResult_Error(
                    "[InitDG오더]Context 메뉴 찾기 실패",
                    "InsungsAct_RcptRegPage/InitDG오더Async_01", bWrite, true);
            }

            //Debug.WriteLine($"[InitDG오더] 1-2. Context 메뉴 찾음: {hWndMenu:X}");

            // 1-3. "접수화면초기화" 메뉴 클릭 (2개 서브메뉴 중 위쪽, 좌클릭)
            await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndMenu, 10, 12);
            //Debug.WriteLine("[InitDG오더] 1-3. 접수화면초기화 메뉴 클릭 완료");

            // 1-4. 확인 다이얼로그 대기 (10회 폴링, 1초)
            IntPtr hWndDialog = IntPtr.Zero;
            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(100);
                hWndDialog = Std32Window.FindWindow("#32770", "확인");
                if (hWndDialog != IntPtr.Zero)
                {
                    // "예(&Y)" 버튼 찾기
                    IntPtr hWndBtn = Std32Window.FindWindowEx(
                        hWndDialog, IntPtr.Zero, "Button", "예(&Y)");
                    if (hWndBtn != IntPtr.Zero)
                    {
                        //Debug.WriteLine("[InitDG오더] 1-4. '예' 버튼 클릭");
                        // 10회 재시도 (기존 로직과 동일)
                        for (int j = 0; j < 10; j++)
                        {
                            await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndBtn);
                            await Task.Delay(50);

                            // 다이얼로그가 사라졌는지 확인
                            if (Std32Window.FindWindow("#32770", "확인") == IntPtr.Zero)
                                break;
                        }
                        break;
                    }
                    else
                    {
                        return CommonFuncs_StdResult.ErrMsgResult_Error(
                            "[InitDG오더]'예' 버튼 찾기 실패",
                            "InsungsAct_RcptRegPage/InitDG오더Async_02",
                            bWrite, true);
                    }
                }
            }

            if (hWndDialog == IntPtr.Zero)
            {
                return CommonFuncs_StdResult.ErrMsgResult_Error(
                    "[InitDG오더]확인 다이얼로그 찾기 실패",
                    "InsungsAct_RcptRegPage/InitDG오더Async_03", bWrite, true);
            }

            // 1-5. 확인 다이얼로그 사라질 때까지 대기 (10회 폴링, 1초)
            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(100);
                hWndDialog = Std32Window.FindWindow("#32770", "확인");
                if (hWndDialog == IntPtr.Zero) break;
            }

            if (hWndDialog != IntPtr.Zero)
            {
                return CommonFuncs_StdResult.ErrMsgResult_Error(
                    "[InitDG오더]확인 다이얼로그 사라지기 실패",
                    "InsungsAct_RcptRegPage/InitDG오더Async_04", bWrite, true);
            }

            //Debug.WriteLine("[InitDG오더] 1-5. 접수화면초기화 완료");

            // 1-6. 초기화 반영 대기
            await Task.Delay(500);

            // Step 2: 불필요한 컬럼을 우측으로 이동 (15회 반복)
            Debug.WriteLine("[InitDG오더] Step 2: 불필요한 컬럼 우측 이동 시작");

            const int gab = 7;      // 헤더에서 텍스트 추출할 y 위치
            const int height = 18;  // 헤더 텍스트 높이
            const int center = 15;     // 대충 가운데

            try
            {
            for (int iteration = 0; iteration < 15; iteration++)
            {
                // 2-1. 헤더 캡처 및 컬럼 경계 검출
                var (bmpHeader, listLW, columns) = CaptureAndDetectColumnBoundaries(rcHeader, gab);
                if (bmpHeader == null)
                {
                    return CommonFuncs_StdResult.ErrMsgResult_Error(
                        $"[InitDG오더]헤더 캡처 실패 (반복 {iteration + 1})",
                        "InsungsAct_RcptRegPage/InitDG오더Async_05", bWrite, true);
                }

                // 2-2. 각 컬럼 텍스트 인식
                string[] texts = await OfrAllColumnsAsync(bmpHeader, listLW, columns, gab, height, bEdit);
                Draw.Rectangle[] rcHeaders = new Draw.Rectangle[columns];
                for (int x = 0; x < columns; x++)
                {
                    rcHeaders[x] = new Draw.Rectangle(listLW[x].nLeft, gab, listLW[x].nWidth, height);
                }

                // 2-4. 우측에서 좌측으로 검사하여 불필요한 컬럼 제거
                int removedCount = 0;
                for (int x = columns - 1; x >= 0; x--)
                {
                    // m_ReceiptDgHeaderInfos에 존재하는지 확인
                    int index = m_ReceiptDgHeaderInfos
                        .Select((value, idx) => new { value, idx })
                        .Where(z => z.value.sName == texts[x])
                        .Select(z => z.idx)
                        .DefaultIfEmpty(-1)
                        .First();

                    if (index < 0) // 필요없는 컬럼 발견
                    {
                        removedCount++;
                        //Debug.WriteLine(
                        //    $"[InitDG오더] 2-{iteration+1}. 불필요 컬럼 발견: [{x}]{texts[x]} → 제거");

                        // 수직 드래그로 우측 이동 (위로 드래그하여 제거)
                        Draw.Point ptCenter = StdUtil.GetCenterDrawPoint(rcHeaders[x]);
                        await Simulation_Mouse.SafeMouseEvent_DragLeft_Smooth_VerticalAsync(
                            m_RcptPage.DG오더_hWnd, ptCenter, -50, false);

                        await Task.Delay(50);
                    }
                }

                bmpHeader.Dispose();

                // 2-5. 종료 조건: 원하는 컬럼이 모두 화면에 보일 때
                bool allColumnsVisible = true;
                string missingColumn = "";
                for (int i = 0; i < m_ReceiptDgHeaderInfos.Length; i++)
                {
                    if (!texts.Contains(m_ReceiptDgHeaderInfos[i].sName))
                    {
                        allColumnsVisible = false;
                        missingColumn = m_ReceiptDgHeaderInfos[i].sName;
                        Debug.WriteLine($"[InitDG오더] 2-{iteration+1}. 누락 컬럼: '{missingColumn}'");
                        break;
                    }
                }

                if (allColumnsVisible)
                {
                    Debug.WriteLine($"[InitDG오더] 2-{iteration+1}. 원하는 컬럼 모두 표시됨, Step 2 완료");
                    break;
                }

                //Debug.WriteLine($"[InitDG오더] 2-{iteration+1}. {workCount}개 컬럼 이동 완료");
            }
            }
            finally
            {
                // 외부입력차단 강제 해제 (예외 발생 시 안전장치)
                Simulation_Mouse.SafeBlockInputForceStop();
            }

            // Step 2-끝: 컬럼 폭 드래그 조정 (반복 - 원하는 컬럼 수까지)
            Debug.WriteLine("[InitDG오더] Step 2-끝: 컬럼 폭 조정 시작");

            try
            {
            for (int widthIter = 0; widthIter < 10; widthIter++)
            {
                // 첫 번째 반복 시 캡처 전 안정화 대기
                if (widthIter == 0)
                {
                    await Task.Delay(CommonVars.c_nWaitLong);
                }

                // 1. 캡처 및 경계선 검출
                var (bmpHeader, listLW, columns) = CaptureAndDetectColumnBoundaries(rcHeader, gab);
                if (bmpHeader == null)
                {
                    return new StdResult_Error("헤더 캡쳐 실패", "InsungsAct_RcptRegPage/InitDG오더Async_Step2End_01");
                }

                Debug.WriteLine($"[InitDG오더] Step 2-끝-{widthIter + 1}. 현재 컬럼 수: {columns}, 목표: {m_ReceiptDgHeaderInfos.Length}");

                // 2. 모든 컬럼 텍스트 인식
                string[] texts = await OfrAllColumnsAsync(bmpHeader, listLW, columns, gab, height, true);
                Draw.Rectangle[] rcHeaders = new Draw.Rectangle[columns];
                for (int x = 0; x < columns; x++)
                {
                    rcHeaders[x] = new Draw.Rectangle(listLW[x].nLeft, gab, listLW[x].nWidth, height);
                    Debug.WriteLine($"[InitDG오더] Step 2-끝-{widthIter + 1}. 컬럼[{x}] 인식: '{texts[x]}'");
                }

                // 3. 불필요한 컬럼 제거
                int removedCount = 0;
                for (int x = columns - 1; x >= 0; x--)
                {
                    // m_ReceiptDgHeaderInfos에 존재하는지 확인
                    int index = m_ReceiptDgHeaderInfos
                        .Select((value, idx) => new { value, idx })
                        .Where(z => z.value.sName == texts[x])
                        .Select(z => z.idx)
                        .DefaultIfEmpty(-1)
                        .First();

                    if (index < 0) // 필요없는 컬럼 발견
                    {
                        removedCount++;
                        Debug.WriteLine($"[InitDG오더] Step 2-끝-{widthIter + 1}. 불필요 컬럼 발견: [{x}]{texts[x]} → 제거");

                        // 수직 드래그로 우측 이동 (위로 드래그하여 제거)
                        Draw.Point ptCenter = StdUtil.GetCenterDrawPoint(rcHeaders[x]);
                        await Simulation_Mouse.SafeMouseEvent_DragLeft_Smooth_VerticalAsync(
                            m_RcptPage.DG오더_hWnd, ptCenter, -50, false);

                        await Task.Delay(50);
                    }
                }

                if (removedCount > 0)
                {
                    Debug.WriteLine($"[InitDG오더] Step 2-끝-{widthIter + 1}. {removedCount}개 불필요 컬럼 제거 완료");
                }

                // 4. 폭 조정
                // 우측에서 좌측으로 처리
                for (int x = columns - 1; x >= 0; x--)
                {
                    // 경계선 위치 = (x+1)번 경계선의 Left
                    int boundaryX = listLW[x + 1].nLeft;

                    // texts[x]로 m_ReceiptDgHeaderInfos에서 매칭하여 문자수 구하기
                    int textLength = 0;
                    string columnName = texts[x];

                    if (!string.IsNullOrEmpty(columnName))
                    {
                        var matched = m_ReceiptDgHeaderInfos
                            .FirstOrDefault(h => h.sName == columnName);

                        if (matched != null)
                        {
                            textLength = matched.sName.Length;
                        }
                        else
                        {
                            textLength = columnName.Length; // 매칭 안 되면 실제 텍스트 길이 사용
                        }
                    }
                    else
                    {
                        textLength = 3; // 기본값
                    }

                    // 목표 폭 = 컬럼 문자수 * 18
                    int targetWidth = textLength * 18;
                    int targetX = listLW[x].nLeft + targetWidth;

                    // 드래그 거리 계산
                    int dx = targetX - boundaryX;

                    Draw.Point ptStart = new Draw.Point(boundaryX, center);

                    // 드래그로 폭 조정
                    await Simulation_Mouse.SafeMouseEvent_DragLeft_Smooth_HorizonAsync(
                        m_RcptPage.DG오더_hWnd, ptStart, dx, bBkCursor: false, nMiliSec: 100);

                    await Task.Delay(100);

                }

                bmpHeader.Dispose();
                Debug.WriteLine($"[InitDG오더] Step 2-끝-{widthIter + 1}. {columns}개 컬럼 폭 조정 완료");

                // 5. 폭 조정 후 다시 캡처하여 원하는 컬럼 개수 확인
                var (bmpHeader2, listLW2, columns2) = CaptureAndDetectColumnBoundaries(rcHeader, gab);
                if (bmpHeader2 == null)
                {
                    return new StdResult_Error("폭 조정 후 헤더 캡쳐 실패", "InsungsAct_RcptRegPage/InitDG오더Async_Step2End_02");
                }

                string[] texts2 = await OfrAllColumnsAsync(bmpHeader2, listLW2, columns2, gab, height, true);

                // 원하는 컬럼 개수 확인
                int matchedCount = 0;
                List<string> matchedColumns = new List<string>();
                List<string> missingColumns = new List<string>();

                for (int i = 0; i < m_ReceiptDgHeaderInfos.Length; i++)
                {
                    bool found = false;
                    for (int x = 0; x < columns2; x++)
                    {
                        if (texts2[x] == m_ReceiptDgHeaderInfos[i].sName)
                        {
                            matchedCount++;
                            matchedColumns.Add(m_ReceiptDgHeaderInfos[i].sName);
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        missingColumns.Add(m_ReceiptDgHeaderInfos[i].sName);
                    }
                }

                Debug.WriteLine($"[InitDG오더] Step 2-끝-{widthIter + 1}. 원하는 컬럼 획득: {matchedCount}/{m_ReceiptDgHeaderInfos.Length}");
                if (missingColumns.Count > 0)
                {
                    Debug.WriteLine($"[InitDG오더] - 누락 컬럼({missingColumns.Count}): {string.Join(", ", missingColumns)}");
                }
                Debug.WriteLine($"[InitDG오더] - 현재 검출된 모든 컬럼({columns2}): {string.Join(", ", texts2.Where(t => !string.IsNullOrEmpty(t)))}");

                bmpHeader2.Dispose();

                // 원하는 컬럼을 모두 얻었으면 종료
                if (matchedCount >= m_ReceiptDgHeaderInfos.Length)
                {
                    Debug.WriteLine($"[InitDG오더] Step 2-끝: 목표 컬럼 모두 획득, 종료");
                    break;
                }
            }

            // Step 2-끝 루프 종료 후 검증
            Debug.WriteLine("[InitDG오더] Step 2-끝: 반복 루프 종료");
            {
                // 최종 확인을 위해 다시 캡처 및 컬럼 검출
                var (bmpCheck, listLW, checkColumns) = CaptureAndDetectColumnBoundaries(rcHeader, gab);
                if (bmpCheck == null)
                {
                    return new StdResult_Error("Step 2-끝 검증 캡처 실패", "InsungsAct_RcptRegPage/InitDG오더Async_Step2End_Check");
                }

                // OFR로 컬럼명 확인
                string[] checkTexts = await OfrAllColumnsAsync(bmpCheck, listLW, checkColumns, gab, height, true);
                bmpCheck.Dispose();

                // 원하는 컬럼 개수 확인
                int checkMatchedCount = 0;
                List<string> checkMissingColumns = new List<string>();
                for (int i = 0; i < m_ReceiptDgHeaderInfos.Length; i++)
                {
                    bool found = false;
                    for (int x = 0; x < checkColumns; x++)
                    {
                        if (checkTexts[x] == m_ReceiptDgHeaderInfos[i].sName)
                        {
                            checkMatchedCount++;
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        checkMissingColumns.Add(m_ReceiptDgHeaderInfos[i].sName);
                    }
                }

                Debug.WriteLine($"[InitDG오더] Step 2-끝 검증: 원하는 컬럼 획득 {checkMatchedCount}/{m_ReceiptDgHeaderInfos.Length}");

                if (checkMatchedCount < m_ReceiptDgHeaderInfos.Length)
                {
                    string errorMsg = $"[InitDG오더] Step 2-끝: 10회 반복 후에도 목표 컬럼 획득 실패\n\n";
                    errorMsg += $"획득 컬럼: {checkMatchedCount}/{m_ReceiptDgHeaderInfos.Length}\n";
                    errorMsg += $"누락 컬럼({checkMissingColumns.Count}): {string.Join(", ", checkMissingColumns)}\n\n";
                    errorMsg += $"현재 검출된 컬럼({checkColumns}): {string.Join(", ", checkTexts.Where(t => !string.IsNullOrEmpty(t)))}";

                    return new StdResult_Error(errorMsg, "InsungsAct_RcptRegPage/InitDG오더Async_Step2End_Failed");
                }
            }
            }
            finally
            {
                // 외부입력차단 강제 해제 (예외 발생 시 안전장치)
                Simulation_Mouse.SafeBlockInputForceStop();
            }

            Debug.WriteLine("[InitDG오더] Step 2 완료");


            // Step 3: 컬럼 순서 조정
            Debug.WriteLine("[InitDG오더] Step 3: 컬럼 순서 조정 시작");

            for (int x = 0; x < m_ReceiptDgHeaderInfos.Length; x++)
            {
                //Debug.WriteLine($"[InitDG오더] 3-{x+1}. 목표 컬럼: [{x}]{m_ReceiptDgHeaderInfos[x].sName}");

                // 3-1. 헤더 캡처 및 컬럼 경계 검출
                var (bmpHeader, listLW, columns) = CaptureAndDetectColumnBoundaries(rcHeader, gab);
                if (bmpHeader == null)
                {
                    return CommonFuncs_StdResult.ErrMsgResult_Error(
                        $"[InitDG오더]Step 3 헤더 캡처 실패 (컬럼 {x})",
                        "InsungsAct_RcptRegPage/InitDG오더Async_07", bWrite, true);
                }

                // 3-3. 목표 컬럼 텍스트 찾기
                string targetText = m_ReceiptDgHeaderInfos[x].sName;
                int index = -1;

                // 디버그: 검출 컬럼 수 vs 기대 컬럼 수
                if (targetText == "기사전번")
                {
                    Debug.WriteLine($"[InitDG오더] Step 3-{x+1}. '{targetText}' 찾기 시작");
                    Debug.WriteLine($"[InitDG오더] - 검출 컬럼 수: {columns} (listLW.Count={listLW.Count})");
                    Debug.WriteLine($"[InitDG오더] - 기대 컬럼 수: {m_ReceiptDgHeaderInfos.Length}");
                    Debug.WriteLine($"[InitDG오더] - 스캔 범위: 0~{columns - 1}");
                }

                for (int tx = 0; tx < columns; tx++)
                {
                    Draw.Rectangle rcColHeader = new Draw.Rectangle(
                        listLW[tx].nLeft, gab, listLW[tx].nWidth, height);

                    // OFR 인식 (Step 2-3과 동일)
                    Draw.Bitmap bmpColHeader = OfrService.GetBitmapInBitmapFast(
                        bmpHeader, rcColHeader);
                    if (bmpColHeader == null) continue;

                    byte avgBrightness = OfrService.GetAverageBrightness_FromColorBitmapFast(
                        bmpColHeader);
                    Draw.Rectangle? rcForeground = OfrService.GetForeGroundDrawRectangle_FromColorBitmapFast(
                        bmpColHeader, avgBrightness, 0);

                    if (rcForeground == null || rcForeground.Value.Width < 1)
                    {
                        bmpColHeader?.Dispose();
                        continue;
                    }

                    Draw.Bitmap bmpExact = OfrService.GetBitmapInBitmapFast(
                        bmpColHeader, rcForeground.Value);
                    bmpColHeader?.Dispose();

                    if (bmpExact == null) continue;

                    StdResult_String result = await OfrWork_Common.OfrStr_ComplexCharSetAsync(bmpExact, bEdit);

                    bmpExact?.Dispose();

                    // 디버그: "기사전번" 찾기 중 상세 로그
                    if (targetText == "기사전번")
                    {
                        Debug.WriteLine($"[InitDG오더] - 컬럼[{tx}] OFR 결과: '{result?.strResult}'");
                    }

                    if (result?.strResult == targetText)
                    {
                        index = tx;
                        //Debug.WriteLine($"[InitDG오더] 3-{x+1}. 찾음: '{targetText}' at [{tx}]");
                        break;
                    }
                }

                bmpHeader.Dispose();

                // 3-4. 에러 체크
                if (index < 0)
                {
                    return CommonFuncs_StdResult.ErrMsgResult_Error(
                        $"[InitDG오더]목표 컬럼 '{targetText}' 찾기 실패",
                        "InsungsAct_RcptRegPage/InitDG오더Async_08", bWrite, true);
                }

                // 3-5. 컬럼 순서가 틀리면 드래그로 이동
                if (index != x)
                {
                    //Debug.WriteLine($"[InitDG오더] 3-{x+1}. 이동: [{index}] → [{x}]");

                    Draw.Rectangle rcStart = new Draw.Rectangle(
                        listLW[index].nLeft, gab, listLW[index].nWidth, height);
                    Draw.Rectangle rcTarget = new Draw.Rectangle(
                        listLW[x].nLeft, gab, listLW[x].nWidth, height);

                    Draw.Point ptStart = StdUtil.GetCenterDrawPoint(rcStart);
                    Draw.Point ptTarget = new Draw.Point(rcTarget.Left, ptStart.Y);

                    await Simulation_Mouse.SafeMouseEvent_DragLeft_SmoothAsync(
                        m_RcptPage.DG오더_hWnd, ptStart, ptTarget, bBkCursor: false, nMiliSec: 150);

                    await Task.Delay(150);
                }
            }

            Debug.WriteLine("[InitDG오더] Step 3 완료");

            // [확인용] Step 3 완료 후 컬럼 개수 확인
            {
                var (bmpCheck, _, checkColumns) = CaptureAndDetectColumnBoundaries(rcHeader, gab);
                if (bmpCheck != null)
                {
                    Debug.WriteLine($"[InitDG오더] Step 3 완료 후 검출 컬럼 수: {checkColumns}");
                    bmpCheck.Dispose();
                }
            }

            // Step 4: 컬럼 너비 조정
            Debug.WriteLine("[InitDG오더] Step 4: 컬럼 너비 조정 시작");

            for (int x = 0; x < m_ReceiptDgHeaderInfos.Length; x++)
            {
                Debug.WriteLine($"[InitDG오더] 4-{x+1}. 컬럼 너비 조정 시작: [{x}]{m_ReceiptDgHeaderInfos[x].sName}");

                // 4-1. 헤더 캡처 및 컬럼 경계 검출
                var (bmpHeader, listLW, currentColumns) = CaptureAndDetectColumnBoundaries(rcHeader, gab);
                if (bmpHeader == null)
                {
                    return CommonFuncs_StdResult.ErrMsgResult_Error(
                        $"[InitDG오더]Step 4 헤더 캡처 실패 (컬럼 {x})",
                        "InsungsAct_RcptRegPage/InitDG오더Async_09", bWrite, true);
                }

                Debug.WriteLine($"[InitDG오더] 4-{x+1}. 조정 전 검출 컬럼 수: {currentColumns}");

                bmpHeader.Dispose();

                // 4-3. 현재 너비와 목표 너비 비교
                int currentWidth = listLW[x].nWidth;
                int targetWidth = m_ReceiptDgHeaderInfos[x].nWidth;
                int dx = targetWidth - currentWidth;

                if (dx == 0)
                {
                    Debug.WriteLine($"[InitDG오더] 4-{x+1}. 너비 일치: {currentWidth}px, 스킵");
                    continue; // 이미 원하는 너비
                }

                Debug.WriteLine($"[InitDG오더] 4-{x+1}. 너비 조정: {currentWidth}px → {targetWidth}px (dx={dx})");

                // 4-4. 컬럼 오른쪽 경계를 dx만큼 드래그
                Draw.Point ptStart = new Draw.Point(listLW[x]._nRight + 1, gab);
                Draw.Point ptTarget = new Draw.Point(ptStart.X + dx, ptStart.Y);

                await Simulation_Mouse.SafeMouseEvent_DragLeft_SmoothAsync(
                    m_RcptPage.DG오더_hWnd, ptStart, ptTarget, bBkCursor: false, nMiliSec: 150);

                await Task.Delay(150);

                // [확인용] 조정 후 컬럼 개수 확인
                var (bmpAfter, _, afterColumns) = CaptureAndDetectColumnBoundaries(rcHeader, gab);
                if (bmpAfter != null)
                {
                    Debug.WriteLine($"[InitDG오더] 4-{x+1}. 조정 후 검출 컬럼 수: {afterColumns}");

                    if (afterColumns < currentColumns)
                    {
                        Debug.WriteLine($"[InitDG오더] 4-{x+1}. 경고: 컬럼 감소 {currentColumns} → {afterColumns}");
                    }

                    bmpAfter.Dispose();
                }
            }

            Debug.WriteLine("[InitDG오더] Step 4 완료");

            // [확인용] Step 4 완료 후 최종 상태 확인
            {
                Debug.WriteLine("[InitDG오더] Step 4 완료 후 최종 확인 시작");

                var (bmpFinal, listLW, finalColumns) = CaptureAndDetectColumnBoundaries(rcHeader, gab);
                if (bmpFinal != null)
                {
                    Debug.WriteLine($"[InitDG오더] 최종 컬럼 수: {finalColumns}");

                    // 각 컬럼 OFR 확인
                    string[] finalTexts = await OfrAllColumnsAsync(bmpFinal, listLW, finalColumns, gab, height, bEdit);

                    // 최종 컬럼 출력
                    Debug.WriteLine($"[InitDG오더] 최종 컬럼 목록({finalColumns}): {string.Join(", ", finalTexts.Where(t => !string.IsNullOrEmpty(t)))}");

                    // 원하는 컬럼과 비교
                    int matchCount = 0;
                    List<string> missingList = new List<string>();
                    for (int i = 0; i < m_ReceiptDgHeaderInfos.Length; i++)
                    {
                        bool found = false;
                        for (int x = 0; x < finalColumns; x++)
                        {
                            if (finalTexts[x] == m_ReceiptDgHeaderInfos[i].sName)
                            {
                                found = true;
                                matchCount++;
                                break;
                            }
                        }
                        if (!found)
                        {
                            missingList.Add(m_ReceiptDgHeaderInfos[i].sName);
                        }
                    }

                    Debug.WriteLine($"[InitDG오더] 원하는 컬럼 매칭: {matchCount}/{m_ReceiptDgHeaderInfos.Length}");
                    if (missingList.Count > 0)
                    {
                        Debug.WriteLine($"[InitDG오더] 누락 컬럼: {string.Join(", ", missingList)}");
                    }

                    // 순서 확인
                    bool orderCorrect = true;
                    for (int i = 0; i < m_ReceiptDgHeaderInfos.Length && i < finalColumns; i++)
                    {
                        if (finalTexts[i] != m_ReceiptDgHeaderInfos[i].sName)
                        {
                            Debug.WriteLine($"[InitDG오더] 순서 불일치 [{i}]: 실제='{finalTexts[i]}', 예상='{m_ReceiptDgHeaderInfos[i].sName}'");
                            orderCorrect = false;
                        }
                    }
                    if (orderCorrect)
                    {
                        Debug.WriteLine("[InitDG오더] 순서 일치: OK");
                    }

                    bmpFinal.Dispose();
                }

                Debug.WriteLine("[InitDG오더] Step 4 완료 후 최종 확인 종료");
            }

            return null; // 성공
        }
        catch (Exception ex)
        {
            return CommonFuncs_StdResult.ErrMsgResult_Error(
                $"[InitDG오더]예외발생: {ex.Message}",
                "InsungsAct_RcptRegPage/InitDG오더Async_999", bWrite, true);
        }
        finally
        {
            // 외부 입력 차단 강제 해제 (예외 발생 시에도 보장, 중첩 카운터 무시)
            Simulation_Mouse.SafeBlockInputForceStop();

            // 마우스 커서 위치 복원
            Std32Cursor.SetCursorPos_AbsDrawPt(ptCursorBackup);
            //Debug.WriteLine("[InitDG오더] 커서 위치 복원 완료");
        }

        #region 주석처리 - 단계별 확인 중
        /*
        Draw.Bitmap bmpHeader = null;
        Draw.Point ptCursorBackup = Std32Cursor.GetCursorPos_AbsDrawPt(); // 커서 위치 백업
        bool initSuccess = false;

        try
        {
            Debug.WriteLine($"[InitDG오더Async] Datagrid 강제 초기화 시작: issues={issues}");

            // 1. 접수화면초기화 시도 (최대 3회 재시도)
            const int MAX_INIT_RETRY = 3;
            for (int initRetry = 0; initRetry < MAX_INIT_RETRY; initRetry++)
            {
                if (initRetry > 0)
                {
                    Debug.WriteLine($"[InitDG오더Async] 접수화면초기화 재시도 {initRetry + 1}/{MAX_INIT_RETRY}");
                    await Task.Delay(300); // 재시도 전 대기
                }

                // 1-1. Context 메뉴 → "접수화면초기화" 클릭
                await Std32Mouse_Post.MousePostAsync_ClickRight(m_RcptPage.DG오더_hWnd);
                await Task.Delay(100);

                // Context 메뉴 대기 (원하는 결과가 나올 때까지 폴링)
                IntPtr hWndMenu = IntPtr.Zero;
                for (int i = 0; i < 100; i++) // 2초 대기
                {
                    await Task.Delay(20);
                    hWndMenu = Std32Window.FindMainWindow_StartsWith(
                        m_FileInfo.Main_AnyMenu_sClassName,
                        m_FileInfo.Main_AnyMenu_sWndName
                    );
                    if (hWndMenu != IntPtr.Zero) break;
                }

                if (hWndMenu == IntPtr.Zero)
                {
                    Debug.WriteLine($"[InitDG오더Async] Context 메뉴 찾기 실패 (시도 {initRetry + 1}/{MAX_INIT_RETRY})");
                    continue; // 다시 우클릭부터 재시도
                }

                Debug.WriteLine($"[InitDG오더Async] Context 메뉴 찾음: {hWndMenu:X}");

                // "접수화면초기화" 메뉴 클릭 (2개 서브메뉴 중 위쪽)
                await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndMenu, 10, 12);
                await Task.Delay(100);

                // 확인 다이얼로그 대기 (원하는 결과가 나올 때까지 폴링)
                IntPtr hWndDialog = IntPtr.Zero;
                bool dialogHandled = false;

                for (int i = 0; i < 10; i++) // 1초 대기
                {
                    await Task.Delay(100);
                    hWndDialog = Std32Window.FindWindow("#32770", "확인");
                    if (hWndDialog != IntPtr.Zero)
                    {
                        // "예(&Y)" 버튼 찾기
                        IntPtr hWndBtn = Std32Window.FindWindowEx(hWndDialog, IntPtr.Zero, "Button", "예(&Y)");
                        if (hWndBtn != IntPtr.Zero)
                        {
                            Debug.WriteLine($"[InitDG오더Async] 확인 다이얼로그 '예' 버튼 클릭");
                            await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndBtn);
                            dialogHandled = true;
                            break;
                        }
                    }
                }

                if (!dialogHandled)
                {
                    Debug.WriteLine($"[InitDG오더Async] 확인 다이얼로그 처리 실패 (시도 {initRetry + 1}/{MAX_INIT_RETRY})");
                    continue; // 다시 우클릭부터 재시도
                }

                // 확인 다이얼로그 사라질 때까지 대기 (원하는 결과가 나올 때까지 폴링)
                for (int i = 0; i < 10; i++) // 1초 대기
                {
                    await Task.Delay(100);
                    hWndDialog = Std32Window.FindWindow("#32770", "확인");
                    if (hWndDialog == IntPtr.Zero) break;
                }

                if (hWndDialog != IntPtr.Zero)
                {
                    Debug.WriteLine($"[InitDG오더Async] 확인 다이얼로그가 사라지지 않음 (시도 {initRetry + 1}/{MAX_INIT_RETRY})");
                    continue; // 다시 우클릭부터 재시도
                }

                Debug.WriteLine($"[InitDG오더Async] 접수화면초기화 완료");
                initSuccess = true;
                await Task.Delay(500); // 초기화 반영 대기
                break; // 성공 시 탈출
            }

            // 접수화면초기화 실패 시 경고만 출력 (컬럼 조정은 시도)
            if (!initSuccess)
            {
                Debug.WriteLine($"[InitDG오더Async] 경고: 접수화면초기화 {MAX_INIT_RETRY}회 실패 - 컬럼 조정만 시도");
            }

            // 2. 컬럼 조정 (WrongOrder 또는 WrongWidth 이슈가 있을 때만)
            if ((issues & CEnum_DgValidationIssue.WrongOrder) != 0 ||
                (issues & CEnum_DgValidationIssue.WrongWidth) != 0)
            {
                Debug.WriteLine($"[InitDG오더Async] 컬럼 조정 시작");

                // Datagrid 절대좌표
                Draw.Point ptDgTopLeft = new Draw.Point(
                    m_RcptPage.DG오더_AbsRect.Left,
                    m_RcptPage.DG오더_AbsRect.Top
                );

                // m_ReceiptDgHeaderInfos.Length만큼 반복 (20개 컬럼)
                for (int repeatIdx = 0; repeatIdx < m_ReceiptDgHeaderInfos.Length; repeatIdx++)
                {
                    // 2-1. 헤더 캡처
                    bmpHeader = OfrService.CaptureScreenRect_InWndHandle(
                        m_Main.TopWnd_hWnd,
                        rcHeader
                    );

                    if (bmpHeader == null)
                    {
                        Debug.WriteLine($"[InitDG오더Async] 경고: 헤더 캡처 실패 (repeat={repeatIdx}) - 다음 반복 시도");
                        continue; // return 하지 않고 다음 반복
                    }

                    // 2-2. 컬럼 경계 검출
                    byte minBrightness = OfrService.GetMinBrightnessAtRow_FromColorBitmapFast(
                        bmpHeader, HEADER_GAB
                    );
                    minBrightness += 2; // 확실한 경계를 위해

                    bool[] boolArr = OfrService.GetBoolArray_FromColorBitmapRowFast(
                        bmpHeader, HEADER_GAB, minBrightness, 2
                    );

                    List<OfrModel_LeftWidth> listLW =
                        OfrService.GetLeftWidthList_FromBool1Array(boolArr, minBrightness);

                    if (listLW == null || listLW.Count == 0)
                    {
                        bmpHeader?.Dispose();
                        continue; // 다음 반복
                    }

                    int columns = listLW.Count - 1; // 오른쪽 끝 경계 제외
                    if (columns <= 0)
                    {
                        bmpHeader?.Dispose();
                        continue;
                    }

                    // 2-3. 컬럼 헤더 OFR
                    string[] columnTexts = new string[columns];
                    Draw.Rectangle[] rcHeaders = new Draw.Rectangle[columns];

                    for (int x = 0; x < columns; x++)
                    {
                        rcHeaders[x] = new Draw.Rectangle(
                            listLW[x].nLeft,
                            HEADER_GAB,
                            listLW[x].nWidth,
                            HEADER_TEXT_HEIGHT
                        );

                        // OFR 수행
                        OfrResult_TbCharSetList resultChSet = await OfrWork_Common.OfrStr_ComplexCharSetAsync(
                            bmpHeader,
                            rcHeaders[x],
                            bSaveToTbText: false,
                            bEdit,
                            bWrite,
                            bMsgBox: false
                        );

                        if (resultChSet != null && !string.IsNullOrEmpty(resultChSet.strResult))
                        {
                            columnTexts[x] = resultChSet.strResult;
                        }
                        else
                        {
                            columnTexts[x] = "";
                        }
                    }

                    bmpHeader?.Dispose();
                    bmpHeader = null;

                    // 2-4. 각 컬럼 위치 확인 및 조정
                    for (int x = 0; x < columns && x < m_ReceiptDgHeaderInfos.Length; x++)
                    {
                        string targetText = m_ReceiptDgHeaderInfos[x].sName;

                        // 현재 컬럼 찾기
                        int index = Array.FindIndex(columnTexts, t => t == targetText);

                        if (index < 0)
                        {
                            Debug.WriteLine($"[InitDG오더Async] 컬럼[{x}] '{targetText}' 찾기 실패");
                            continue; // 다음 컬럼
                        }

                        // 컬럼 순서가 틀리면 드래그로 이동
                        if (index != x)
                        {
                            Debug.WriteLine($"[InitDG오더Async] 컬럼[{x}] '{targetText}' 순서 조정: {index} → {x}");

                            Draw.Point ptStart = StdUtil.GetCenterDrawPoint(rcHeaders[index]);
                            Draw.Point ptEnd = new Draw.Point(rcHeaders[x].Left, ptStart.Y);

                            // 절대좌표로 변환
                            Draw.Point ptStartAbs = new Draw.Point(
                                ptDgTopLeft.X + rcHeader.Left + ptStart.X,
                                ptDgTopLeft.Y + rcHeader.Top + ptStart.Y
                            );
                            Draw.Point ptEndAbs = new Draw.Point(
                                ptDgTopLeft.X + rcHeader.Left + ptEnd.X,
                                ptDgTopLeft.Y + rcHeader.Top + ptEnd.Y
                            );

                            // Drag 수행 (Simulation.cs:457-478 참고, 라이브러리 조합 방식)
                            Std32Cursor.SetCursorPos_AbsDrawPt(ptStartAbs);
                            Std32Mouse_Event.MouseEvent_LeftBtnDown();
                            Std32Mouse_Send.MouseSet_MoveSmooth_ptAbs(ptStartAbs, ptEndAbs, 150);
                            Std32Mouse_Event.MouseEvent_LeftBtnUp();
                            await Task.Delay(150);
                        }
                    }

                    // 2-5. 컬럼 너비 조정
                    for (int x = 0; x < columns && x < m_ReceiptDgHeaderInfos.Length; x++)
                    {
                        StdResult_Error adjustResult = AdjustColumnWidth(rcHeader, ptDgTopLeft, x);
                        if (adjustResult != null)
                        {
                            Debug.WriteLine($"[InitDG오더Async] 컬럼[{x}] 너비 조정 실패: {adjustResult.sErr}");
                            // 실패해도 계속 진행
                        }

                        await Task.Delay(150);
                    }

                    // 모든 컬럼 처리 완료 - 루프 탈출
                    Debug.WriteLine($"[InitDG오더Async] 컬럼 조정 완료");
                    break;
                }
            }

            Debug.WriteLine($"[InitDG오더Async] Datagrid 강제 초기화 완료");
            return null; // 성공
        }
        catch (Exception ex)
        {
            return CommonFuncs_StdResult.ErrMsgResult_Error(
                $"[{m_Context.AppName}/InitDG오더]예외발생: {ex.Message}",
                "InsungsAct_RcptRegPage/InitDG오더Async_999", bWrite, bMsgBox);
        }
        finally
        {
            bmpHeader?.Dispose();
            Std32Cursor.SetCursorPos_AbsDrawPt(ptCursorBackup); // 커서 원위치
        }
        */
        #endregion
    }

    /// <summary>
    /// 접수등록 페이지가 초기화되었는지 확인 (간단 체크)
    /// </summary>
    public bool IsInitialized()
    {
        // 핵심 핸들 체크
        if (m_RcptPage.TopWnd_hWnd == IntPtr.Zero) return false;
        if (m_RcptPage.DG오더_hWnd == IntPtr.Zero) return false;

        // 핵심 데이터 구조 체크
        if (m_RcptPage.DG오더_RelChildRects == null) return false;
        if (m_RcptPage.DG오더_ColumnTexts == null) return false;

        return true;
    }

    #endregion


    #region 자동배차 Helper 함수들
    /// <summary>
    /// 신규 주문 등록 확인 (Kai에만 존재, 인성에 없음)
    /// </summary>
    public async Task<CommonResult_AutoAllocProcess> CheckIsOrderAsync_AssumeKaiNewOrder(AutoAllocModel item, CancelTokenControl ctrl)
    {
        // Cancel/Pause 체크 - 긴 작업 전
        await ctrl.WaitIfPausedOrCancelledAsync();

        string kaiState = item.NewOrder.OrderState;

        switch (kaiState)
        {
            case "접수":
            case "취소":
            case "대기":
                // 신규 주문 팝업창 열기 → 입력 → 닫기 → 성공 확인
                StdResult_Status result = await OpenNewOrderPopupAsync(item, ctrl);

                if (result.Result == StdResult.Success)
                {
                    // 성공: NotChanged로 재적재 (다음 사이클에서 관리 대상으로 분류됨)
                    return CommonResult_AutoAllocProcess.SuccessAndReEnqueue(item, PostgService_Common_OrderState.NotChanged);
                }
                else
                {
                    // 실패: 치명적 에러 (신규 등록 실패)
                    return CommonResult_AutoAllocProcess.FailureAndDiscard(result.sErr, result.sPos);
                }

            case "배차":
            case "운행":
            case "완료":
            case "예약":
                return CommonResult_AutoAllocProcess.FailureAndDiscard($"미구현 상태: {kaiState}", "CheckIsOrderAsync_AssumeKaiNewOrder_TODO");

            default:
                return CommonResult_AutoAllocProcess.FailureAndDiscard($"알 수 없는 Kai 주문 상태: {kaiState}", "CheckIsOrderAsync_AssumeKaiNewOrder_800");
        }
    }

    /// <summary>
    /// Insung 주문 상태 관리 및 모니터링 (NotChanged 상황 처리)
    /// - Insung 상태를 primary switch로 분기
    /// - 각 Insung 상태별 handler 함수 호출
    /// - 로그만 출력 (DB 업데이트, 앱 취소 작업 없음)
    /// </summary>
    public async Task<CommonResult_AutoAllocProcess> CheckIsOrderAsync_InsungOrderManage(AutoAllocModel item, CommonResult_AutoAllocDatagrid dgInfo, CancelTokenControl ctrl)
    {
        // Cancel/Pause 체크
        await ctrl.WaitIfPausedOrCancelledAsync();

        string kaiState = item.NewOrder.OrderState;
        string insungState = dgInfo.sStatus;

        Debug.WriteLine($"[CheckIsOrderAsync_InsungOrderManage] KeyCode={item.KeyCode}, Kai={kaiState}, Insung={insungState}");

        // Insung 상태별로 handler 함수 호출 (2중 switch 방지)
        switch (insungState)
        {
            case "접수":
            case "배차":
                return await InsungOrderManage_접수Or배차Async(item, kaiState, dgInfo, ctrl);
            case "운행":
                return await InsungOrderManage_운행Async(item, kaiState, dgInfo, ctrl);
            case "완료":
                return await InsungOrderManage_완료Async(item, kaiState, dgInfo, ctrl);
            case "취소":
                return await InsungOrderManage_취소Async(item, kaiState, dgInfo, ctrl);
            default:
                Debug.WriteLine($"  → 미정의 Insung 상태: {insungState}");
                return CommonResult_AutoAllocProcess.SuccessAndReEnqueue();
        }
    }

    /// <summary>
    /// Insung "접수" 또는 "배차" 상태 처리 - Kai 상태별 로깅
    /// </summary>
    private async Task<CommonResult_AutoAllocProcess> InsungOrderManage_접수Or배차Async(AutoAllocModel item, string kaiState, CommonResult_AutoAllocDatagrid dgInfo, CancelTokenControl ctrl)
    {
        string insungState = dgInfo.sStatus;

        switch (kaiState)
        {
            case "접수":
            case "배차":
                Debug.WriteLine($"  → StateFlag를 NotChanged로 변경 후 재적재 요청");
                return CommonResult_AutoAllocProcess.SuccessAndReEnqueue(item, PostgService_Common_OrderState.NotChanged);
            //case "운행":
            //    Debug.WriteLine($"  → [{insungState}/{kaiState}] 비정상 - Kai는 운행인데 Insung은 {insungState}");
            //    return CommonResult_AutoAllocProcess.FailureAndRetry($"비정상 상태: Kai={kaiState}, Insung={insungState}", "InsungOrderManage_접수Or배차Async_01");
            //case "완료":
            //    Debug.WriteLine($"  → [{insungState}/{kaiState}] 비정상 - Kai는 완료인데 Insung은 {insungState}");
            //    return CommonResult_AutoAllocProcess.FailureAndRetry($"비정상 상태: Kai={kaiState}, Insung={insungState}", "InsungOrderManage_접수Or배차Async_02");
            //case "취소":
            //    Debug.WriteLine($"  → [{insungState}/{kaiState}] 비정상 - Kai는 취소인데 Insung은 {insungState}");
            //    return CommonResult_AutoAllocProcess.FailureAndRetry($"비정상 상태: Kai={kaiState}, Insung={insungState}", "InsungOrderManage_접수Or배차Async_03");
            default:
                Debug.WriteLine($"  → [{insungState}/?] 미정의 Kai 상태: {kaiState}");
                return CommonResult_AutoAllocProcess.FailureAndRetry($"미정의 Kai 상태: {kaiState}, Insung={insungState}", "InsungOrderManage_접수Or배차Async_999");
        }
    }

    /// <summary>
    /// Insung "운행" 상태 처리 - 40초 타이머 + Kai 상태별 로깅
    /// </summary>
    private async Task<CommonResult_AutoAllocProcess> InsungOrderManage_운행Async(AutoAllocModel item, string kaiState, CommonResult_AutoAllocDatagrid dgInfo, CancelTokenControl ctrl)
    {
        Debug.WriteLine($"  → [InsungOrderManage_운행Async] 진입 - KeyCode={item.KeyCode}, RunStartTime={item.RunStartTime?.ToString("HH:mm:ss") ?? "null"}, DriverPhone={item.DriverPhone ?? "null"}");

        switch (kaiState)
        {
            case "접수":
                // 타이머 시작 체크
                if (item.RunStartTime == null)
                {
                    item.RunStartTime = DateTime.Now;
                    Debug.WriteLine($"  → [운행/접수] 운행 진입 - 타이머 시작 ({item.RunStartTime:HH:mm:ss}) - 경과: 0.0초 / 40초");

                    // 기사전번 읽기 (캡처된 페이지 이미지 재사용)
                    if (dgInfo.BmpPage == null)
                    {
                        Debug.WriteLine($"  → [운행/접수] 심각한 오류: BmpPage가 null - 자동배차 루프에서 페이지 캡처 실패");
                        return CommonResult_AutoAllocProcess.FailureAndRetry("BmpPage가 null - 페이지 캡처 실패", "InsungOrderManage_운행Async_BmpPageNull");
                    }

                    int yIndex = dgInfo.nIndex + 2;  // 헤더 2줄 추가
                    Draw.Rectangle rectDriverPhNo = m_RcptPage.DG오더_RelChildRects[c_nCol기사전번, yIndex];
                    StdResult_String resultDriverPhNo = await GetRowDriverPhNoAsync(dgInfo.BmpPage, rectDriverPhNo, dgInfo.bInvertRgb, ctrl);

                    if (string.IsNullOrEmpty(resultDriverPhNo.strResult))
                    {
                        Debug.WriteLine($"  → [운행/접수] 심각한 오류: 기사전번 획득 실패 - 운행 상태인데 기사 정보 없음: {resultDriverPhNo.sErr}");
                        return CommonResult_AutoAllocProcess.FailureAndRetry($"기사전번 OFR 실패: {resultDriverPhNo.sErr}", "InsungOrderManage_운행Async_DriverPhNoFail");
                    }

                    item.DriverPhone = resultDriverPhNo.strResult;
                    Debug.WriteLine($"  → [운행/접수] 기사전번 획득: {item.DriverPhone}");

                    return CommonResult_AutoAllocProcess.SuccessAndReEnqueue(item);  // item 업데이트하며 재적재
                }

                // 타이머 체크
                TimeSpan elapsed = DateTime.Now - item.RunStartTime.Value;
                Debug.WriteLine($"  → [운행/접수] 운행 중 - 경과 시간: {elapsed.TotalSeconds:F1}초");

                // 기사전번 다시 OFR (기사 변경 감지용)
                if (dgInfo.BmpPage == null)
                {
                    Debug.WriteLine($"  → [운행/접수] 심각한 오류: BmpPage가 null - 자동배차 루프에서 페이지 캡처 실패");
                    return CommonResult_AutoAllocProcess.FailureAndRetry("BmpPage가 null - 페이지 캡처 실패", "InsungOrderManage_운행Async_BmpPageNull2");
                }

                int yIndexCheck = dgInfo.nIndex + 2;  // 헤더 2줄 추가
                Draw.Rectangle rectDriverPhNoCheck = m_RcptPage.DG오더_RelChildRects[c_nCol기사전번, yIndexCheck];
                StdResult_String resultDriverPhNoCheck = await GetRowDriverPhNoAsync(dgInfo.BmpPage, rectDriverPhNoCheck, dgInfo.bInvertRgb, ctrl);

                if (string.IsNullOrEmpty(resultDriverPhNoCheck.strResult))
                {
                    Debug.WriteLine($"  → [운행/접수] 심각한 오류: 기사전번 획득 실패 - 운행 상태인데 기사 정보 없음: {resultDriverPhNoCheck.sErr}");
                    return CommonResult_AutoAllocProcess.FailureAndRetry($"기사전번 OFR 실패: {resultDriverPhNoCheck.sErr}", "InsungOrderManage_운행Async_DriverPhNoFail2");
                }

                // 기사 변경 체크
                if (resultDriverPhNoCheck.strResult != item.DriverPhone)
                {
                    Debug.WriteLine($"  → [운행/접수] 기사 변경 감지! 기존: {item.DriverPhone} → 새: {resultDriverPhNoCheck.strResult}");
                    item.DriverPhone = resultDriverPhNoCheck.strResult;
                    item.RunStartTime = DateTime.Now;
                    Debug.WriteLine($"  → [운행/접수] 타이머 리셋 - 새로운 40초 대기 시작 ({item.RunStartTime:HH:mm:ss})");
                    return CommonResult_AutoAllocProcess.SuccessAndReEnqueue(item);  // item 업데이트하며 재적재
                }

                // 40초 경과 체크 (기사 변경 없음)
                if (elapsed.TotalSeconds < 40)
                {
                    // 40초 미만 - 계속 대기
                    Debug.WriteLine($"  → [운행/접수] 40초 대기 중 - 경과: {elapsed.TotalSeconds:F1}초 / 40초");
                    return CommonResult_AutoAllocProcess.SuccessAndReEnqueue(item);  // item 유지하며 재적재
                }

                // 40초 이상 경과 - 기사 확정
                Debug.WriteLine($"  → [운행/접수] 40초 경과! 기사 확정 상태 (기사전번: {item.DriverPhone})");

                // 타이머 파괴
                item.RunStartTime = null;

                // 1. 기사 정보 읽기용 팝업 열기 (DG 더블클릭 → 3초 대기 → 닫기)
                Debug.WriteLine($"  → [운행/접수] 기사 정보 읽기용 팝업 열기 시작");
                StdResult_Status resultPopup = await OpenReadPopupAsync(dgInfo.nIndex, item, ctrl);
                if (resultPopup.Result != StdResult.Success)
                {
                    Debug.WriteLine($"  → [운행/접수] 기사 정보 읽기 실패: {resultPopup.sErr}");
                    return CommonResult_AutoAllocProcess.FailureAndRetry(resultPopup.sErr, resultPopup.sPos);
                }
                Debug.WriteLine($"  → [운행/접수] 기사 정보 읽기 성공");

                // 2. Order_StatusPage에서 처리 (DB 업데이트, 다른 앱 취소)
                return await CommonVars.s_Order_StatusPage.Insung01배차To운행Async(item, ctrl);

            case "운행": // 같은상태 
                return CommonResult_AutoAllocProcess.SuccessAndReEnqueue();

            default:
                Debug.WriteLine($"  → [운행/{kaiState}] 미정의 Kai 상태: {kaiState}");
                return CommonResult_AutoAllocProcess.FailureAndRetry($"미정의 Kai 상태: {kaiState}, Insung=운행", "InsungOrderManage_운행Async_999");
        }
    }

    /// <summary>
    /// Insung "완료" 상태 처리 - Kai 상태별 로깅
    /// </summary>
    private async Task<CommonResult_AutoAllocProcess> InsungOrderManage_완료Async(AutoAllocModel item, string kaiState, CommonResult_AutoAllocDatagrid dgInfo, CancelTokenControl ctrl)
    {
        switch (kaiState)
        {
            case "접수":
                Debug.WriteLine($"  → [완료/접수] 비정상 - Kai는 접수인데 Insung은 완료");
                Debug.WriteLine($"  → TODO: Kai DB 업데이트 (접수 → 완료)");
                break;

            case "배차":
                Debug.WriteLine($"  → [완료/배차] 정상 상태 - 배송 완료");
                Debug.WriteLine($"  → TODO: Kai DB 업데이트 (배차 → 완료)");
                break;

            case "운행":
                Debug.WriteLine($"  → [완료/운행] 비정상 - Kai는 운행인데 Insung은 완료");
                Debug.WriteLine($"  → TODO: Kai DB 업데이트 (운행 → 완료)");
                break;

            case "완료":
                Debug.WriteLine($"  → [완료/완료] 정상 상태 - 이미 완료됨");
                break;

            case "취소":
                Debug.WriteLine($"  → [완료/취소] 비정상 - Kai는 취소인데 Insung은 완료");
                break;

            default:
                Debug.WriteLine($"  → [완료/?] 미정의 Kai 상태: {kaiState}");
                break;
        }

        // 타이머 리셋
        if (item.RunStartTime != null)
        {
            Debug.WriteLine($"  → 타이머 리셋 (완료 상태)");
            item.RunStartTime = null;
        }

        return CommonResult_AutoAllocProcess.SuccessAndReEnqueue();
    }

    /// <summary>
    /// Insung "취소" 상태 처리 - Kai 상태별 로깅
    /// </summary>
    private async Task<CommonResult_AutoAllocProcess> InsungOrderManage_취소Async(AutoAllocModel item, string kaiState, CommonResult_AutoAllocDatagrid dgInfo, CancelTokenControl ctrl)
    {
        switch (kaiState)
        {
            case "접수":
                Debug.WriteLine($"  → [취소/접수] 인성에서 주문 취소됨");
                Debug.WriteLine($"  → TODO: Kai DB 업데이트 (접수 → 취소)");
                Debug.WriteLine($"  → TODO: 다른 앱 취소 (Insung2, 화물24시, 원콜)");
                break;

            case "배차":
                Debug.WriteLine($"  → [취소/배차] 인성에서 주문 취소됨 (배차 후 취소)");
                Debug.WriteLine($"  → TODO: Kai DB 업데이트 (배차 → 취소)");
                Debug.WriteLine($"  → TODO: 다른 앱 취소 (Insung2, 화물24시, 원콜)");
                break;

            case "운행":
                Debug.WriteLine($"  → [취소/운행] 비정상 - Kai는 운행인데 Insung은 취소");
                Debug.WriteLine($"  → TODO: Kai DB 업데이트 (운행 → 취소)");
                break;

            case "완료":
                Debug.WriteLine($"  → [취소/완료] 비정상 - Kai는 완료인데 Insung은 취소");
                break;

            case "취소":
                Debug.WriteLine($"  → [취소/취소] 정상 상태 - 이미 취소됨");
                break;

            default:
                Debug.WriteLine($"  → [취소/?] 미정의 Kai 상태: {kaiState}");
                break;
        }

        // 타이머 리셋
        if (item.RunStartTime != null)
        {
            Debug.WriteLine($"  → 타이머 리셋 (취소 상태)");
            item.RunStartTime = null;
        }

        return CommonResult_AutoAllocProcess.SuccessAndReEnqueue();
    }


    #endregion

    #region 공용 Helper Methods
    /// <summary>
    /// 상태 버튼 찾기 (텍스트 검증 포함)
    /// </summary>
    /// <param name="buttonName">버튼 이름 (예: "접수", "전체")</param>
    /// <param name="checkPoint">체크 포인트 (MainWnd 기준 상대좌표)</param>
    /// <param name="errorCode">에러 코드</param>
    /// <param name="bWrite">에러 로그 작성 여부</param>
    /// <param name="bMsgBox">메시지박스 표시 여부</param>
    /// <param name="withTextValidation">텍스트 검증 여부 (true면 텍스트 확인, false면 핸들만 확인)</param>
    /// <returns>성공 시 핸들, 실패 시 에러</returns>

    /// <summary>
    /// 버튼 클릭 후 윈도우가 닫힐 때까지 대기
    /// </summary>
    /// <param name="hWndClick">클릭할 버튼 핸들</param>
    /// <param name="hWndOrg">닫혀야 할 윈도우 핸들</param>
    /// <param name="ctrl">취소 토큰 컨트롤</param>
    /// <returns>윈도우가 닫혔으면 true, 실패하면 false</returns>

    /// <summary>
    /// 확인창 닫기 (WM_SYSCOMMAND SC_CLOSE 전송)
    /// </summary>
    /// <param name="ctrl">취소 토큰 컨트롤</param>
    /// <param name="sClassName">확인창 클래스명 (기본값: "#32770")</param>
    /// <param name="sCaption">확인창 캡션 (기본값: "확인")</param>
    /// <returns>확인창을 찾아서 닫았으면 true, 없으면 false</returns>

    /// <summary>
    /// 저장 버튼 클릭 후 윈도우가 닫히거나 확인창이 나타날 때까지 대기
    /// </summary>
    /// <param name="hWndClick">클릭할 버튼 핸들</param>
    /// <param name="hWndOrg">닫혀야 할 윈도우 핸들</param>
    /// <param name="ctrl">취소 토큰 컨트롤</param>
    /// <param name="sClassName">확인창 클래스명 (기본값: "#32770")</param>
    /// <param name="sCaption">확인창 캡션 (기본값: "확인")</param>
    /// <returns>윈도우가 정상적으로 닫혔으면 true, 확인창이 나타나면 false</returns>

    /// <summary>
    /// CommandBtn(OFR 검증 포함) 찾기 헬퍼 메서드
    /// </summary>

    /// <summary>
    /// CallCount 컨트롤 찾기 헬퍼 메서드
    /// </summary>

    /// <summary>
    /// Datagrid 상태 검증 (컬럼 개수, 순서, 너비 확인)
    /// </summary>
    /// <param name="columnTexts">현재 읽은 컬럼 헤더 텍스트 배열</param>
    /// <param name="listLW">컬럼 Left/Width 리스트</param>
    /// <returns>검증 이슈 플래그 (None이면 정상)</returns>
    private CEnum_DgValidationIssue ValidateDatagridState(string[] columnTexts, List<OfrModel_LeftWidth> listLW)
    {
        CEnum_DgValidationIssue issues = CEnum_DgValidationIssue.None;

        // 1. 컬럼 개수 체크
        if (columnTexts == null || columnTexts.Length != m_ReceiptDgHeaderInfos.Length)
        {
            issues |= CEnum_DgValidationIssue.InvalidColumnCount;
            Debug.WriteLine($"[ValidateDatagridState] 컬럼 개수 불일치: 실제={columnTexts?.Length}, 예상={m_ReceiptDgHeaderInfos.Length}");
            return issues; // 개수가 다르면 더 이상 체크 불가
        }

        // 2. 각 컬럼 검증
        for (int x = 0; x < columnTexts.Length; x++)
        {
            string columnText = columnTexts[x];

            // 2-1. 컬럼명이 유효한지 (m_ReceiptDgHeaderInfos에 존재하는지)
            int index = Array.FindIndex(m_ReceiptDgHeaderInfos, h => h.sName == columnText);

            if (index < 0) // 존재하지 않는 컬럼
            {
                issues |= CEnum_DgValidationIssue.InvalidColumn;
                Debug.WriteLine($"[ValidateDatagridState] 유효하지 않은 컬럼[{x}]: '{columnText}'");
                continue; // 다음 컬럼 검사
            }

            // 2-2. 컬럼 순서가 맞는지
            if (index != x)
            {
                issues |= CEnum_DgValidationIssue.WrongOrder;
                Debug.WriteLine($"[ValidateDatagridState] 컬럼 순서 불일치[{x}]: '{columnText}' (예상 위치={index})");
            }

            // 2-3. 컬럼 너비가 맞는지 (허용 오차 이내인지)
            int actualWidth = listLW[x].nWidth;
            int expectedWidth = m_ReceiptDgHeaderInfos[index].nWidth;
            int widthDiff = Math.Abs(actualWidth - expectedWidth);

            if (widthDiff > COLUMN_WIDTH_TOLERANCE)
            {
                issues |= CEnum_DgValidationIssue.WrongWidth;
            }

            Debug.WriteLine($"[ValidateDatagridState] 컬럼 너비[{x}]: '{columnText}', 실제={actualWidth}, 예상={expectedWidth}, 오차={widthDiff}");
        }

        if (issues == CEnum_DgValidationIssue.None)
        {
            Debug.WriteLine($"[ValidateDatagridState] Datagrid 상태 정상");
        }
        else
        {
            Debug.WriteLine($"[ValidateDatagridState] Datagrid 검증 실패: {issues}");
        }

        return issues;
    }
    private async Task<StdResult_Status> ResgistAndVerify요금Async(IntPtr hWnd, int value, string fieldName, CancelTokenControl ctrl, int retryCount = 3)
    {
        bool bResult = false;

        for (int i = 1; i <= retryCount; i++)
        {
            await ctrl.WaitIfPausedOrCancelledAsync();
            await Task.Delay(CommonVars.c_nWaitShort, ctrl.Token);

            // ===== 0차: 포커스 설정 =====
            bool focusResult = await Std32Window.SetFocusWithForegroundAsync(hWnd);
            if (!focusResult) continue;

            Std32Key_Msg.KeyPost_Digit(hWnd, (uint)value);
            await Task.Delay(CommonVars.c_nWaitNormal, ctrl.Token);

            int readInt = StdConvert.StringWonFormatToInt(Std32Window.GetWindowCaption(hWnd));
            if (readInt == value)
            {
                bResult = true;
                Std32Key_Msg.KeyPost_Click(hWnd, StdCommon32.VK_RETURN);
                break;
            }
        }

        if (bResult) return new StdResult_Status(StdResult.Success);
        else return new StdResult_Status(StdResult.Fail, $"{fieldName} 입력 실패", "ResgistAndVerify요금Async_01");
    }

    /// <summary>
    /// 로딩 패널 대기 (조회 시 데이터 로딩 확인)
    /// - Phase 1: 로딩 패널 출현 대기 (최대 250ms)
    /// - Phase 2: 로딩 패널 사라짐 대기 (최대 timeoutSec초)
    /// </summary>
    /// <param name="hWndDG">Datagrid 핸들</param>
    /// <param name="ctrl">취소 토큰</param>
    /// <param name="timeoutSec">Phase 2 타임아웃 (초)</param>
    /// <returns>Success: 로딩 완료, Skip: 이미 완료, Fail: 타임아웃</returns>
    private async Task<StdResult_Status> WaitPanLoadedAsync(IntPtr hWndDG, CancelTokenControl ctrl, int timeoutSec = 50)
    {
        try
        {
            IntPtr hWndFind = IntPtr.Zero;
            Draw.Point ptCheckPan = m_FileInfo.접수등록Page_DG오더_ptChkRelPanL;

            // Phase 1: 로딩 패널 출현 대기 (최대 250ms)
            //Debug.WriteLine($"[{m_Context.AppName}] 로딩 패널 출현 대기 시작");

            for (int i = 0; i < CommonVars.c_nWaitLong; i++) // 250ms
            {
                await ctrl.WaitIfPausedOrCancelledAsync();

                hWndFind = Std32Window.GetWndHandle_FromRelDrawPt(hWndDG, ptCheckPan);
                if (hWndFind != hWndDG)
                {
                    //Debug.WriteLine($"[{m_Context.AppName}] 로딩 패널 출현 확인 ({i}ms)");
                    break;
                }
                await Task.Delay(1, ctrl.Token);
            }

            if (hWndFind == hWndDG)
            {
                //Debug.WriteLine($"[{m_Context.AppName}] 로딩 패널 미출현 → Skip (이미 로딩 완료)");
                return new StdResult_Status(StdResult.Skip);
            }

            // Phase 2: 로딩 패널 사라짐 대기 (최대 timeoutSec초)
            //Debug.WriteLine($"[{m_Context.AppName}] 로딩 패널 사라짐 대기 시작 (최대 {timeoutSec}초)");

            int iterations = timeoutSec * 10; // 100ms 단위
            for (int i = 0; i < iterations; i++)
            {
                await ctrl.WaitIfPausedOrCancelledAsync();

                hWndFind = Std32Window.GetWndHandle_FromRelDrawPt(hWndDG, ptCheckPan);
                if (hWndFind == hWndDG)
                {
                    int elapsedMs = i * 100;
                    //Debug.WriteLine($"[{m_Context.AppName}] 로딩 완료 ({elapsedMs}ms)");
                    return new StdResult_Status(StdResult.Success);
                }

                // 5초마다 진행 상황 로그
                if (i > 0 && i % 50 == 0)
                {
                    int elapsedSec = i / 10;
                    Debug.WriteLine($"[{m_Context.AppName}] 로딩 대기 중... ({elapsedSec}초 경과)");
                }

                await Task.Delay(100, ctrl.Token);
            }

            // 타임아웃
            Debug.WriteLine($"[{m_Context.AppName}] 로딩 패널 타임아웃 ({timeoutSec}초)");
            return new StdResult_Status(StdResult.Fail, $"로딩 대기 시간 초과 ({timeoutSec}초)", "InsungsAct_RcptRegPage/WaitPanLoadedAsync_01");
        }
        catch (Exception ex)
        {
            return new StdResult_Status(StdResult.Fail, StdUtil.GetExceptionMessage(ex), "InsungsAct_RcptRegPage/WaitPanLoadedAsync_999");
        }
    }

    /// <summary>
    /// 첫 번째 데이터 로우 클릭 및 선택 검증
    /// - 첫 로우의 [0,2] 셀 클릭
    /// - 첫 로우의 [1,2] 셀에서 선택 검증 (4코너 기반)
    /// </summary>
    /// <param name="ctrl">취소 토큰</param>
    /// <param name="retryCount">재시도 횟수</param>
    /// <returns>true: 선택 검증 성공, false: 실패</returns>
    private async Task<bool> ClickFirstRowAsync(CancelTokenControl ctrl, int retryCount = 3)
    {
        try
        {
            // 클릭용 셀: [0, 2] (첫 번째 데이터 로우의 첫 번째 셀)
            Draw.Rectangle rectClickCell = m_RcptPage.DG오더_RelChildRects[0, 2];

            // 검증용 셀: [1, 2] (첫 번째 데이터 로우의 두 번째 셀)
            Draw.Rectangle rectVerifyCell = m_RcptPage.DG오더_RelChildRects[1, 2];

            for (int i = 1; i <= retryCount; i++)
            {
                await ctrl.WaitIfPausedOrCancelledAsync();

                Debug.WriteLine($"[{m_Context.AppName}] ===== 첫 로우 클릭 시도 {i}/{retryCount} =====");

                // 클릭 포인트
                Draw.Point ptRelDG = StdUtil.GetDrawPoint(rectClickCell, 3, 3);

                // 클릭
                await Std32Mouse_Post.MousePostAsync_ClickLeft_ptRel(m_RcptPage.DG오더_hWnd, ptRelDG);
                await Task.Delay(200, ctrl.Token);

                Debug.WriteLine($"[{m_Context.AppName}] 첫 로우 클릭 완료, 선택 검증 시작");

                // 검증용 셀 캡처
                Draw.Bitmap bmpCell = OfrService.CaptureScreenRect_InWndHandle(m_RcptPage.DG오더_hWnd, rectVerifyCell);
                if (bmpCell == null)
                {
                    Debug.WriteLine($"[{m_Context.AppName}] 검증 셀 캡처 실패 (시도 {i}/{retryCount})");
                    if (i < retryCount) await Task.Delay(200, ctrl.Token);
                    continue;
                }

                try
                {
                    // 캡처된 비트맵은 (0,0) 기준이므로 Rectangle 재생성
                    Draw.Rectangle rectInBitmap = new Draw.Rectangle(0, 0, rectVerifyCell.Width, rectVerifyCell.Height);

                    // 선택 검증 (4코너 기반)
                    bool isSelected = OfrService.IsInvertedSelection(bmpCell, rectInBitmap);

                    Debug.WriteLine($"[{m_Context.AppName}] 선택 여부: {(isSelected ? "선택됨" : "선택 안됨")}");

                    if (isSelected)
                    {
                        Debug.WriteLine($"[{m_Context.AppName}] 첫 로우 선택 검증 성공 (시도 {i}/{retryCount})");
                        return true;
                    }
                }
                finally
                {
                    bmpCell?.Dispose();
                }

                if (i < retryCount)
                {
                    Debug.WriteLine($"[{m_Context.AppName}] 선택 실패, 재시도...");
                    await Task.Delay(200, ctrl.Token);
                }
            }

            Debug.WriteLine($"[{m_Context.AppName}] 첫 로우 선택 검증 실패 ({retryCount}회 시도)");
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{m_Context.AppName}] ClickFirstRowAsync 예외: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 지정된 로우의 주문번호(Seqno) OFR
    /// - 셀 캡처 후 단음소 OFR (TbCharBackup 사용)
    /// </summary>
    /// <param name="rowIndex">데이터 로우 인덱스 (0부터 시작)</param>
    /// <param name="bInvertRgb">RGB 반전 여부 (선택된 로우 = true)</param>
    /// <param name="ctrl">취소 토큰</param>
    /// <param name="retryCount">재시도 횟수</param>
    /// <returns>성공: strResult에 Seqno, 실패: sErr에 에러 메시지</returns>
    private async Task<StdResult_String> GetSeqnoAsync(int rowIndex, bool bInvertRgb, CancelTokenControl ctrl, int retryCount = 3)
    {
        try
        {
            // 주문번호 컬럼 인덱스
            int seqnoColIndex = c_nCol주문번호;
            int yIndex = rowIndex + 2; // 헤더 2줄 추가

            Debug.WriteLine($"[{m_Context.AppName}] 주문번호 OFR - rowIndex={rowIndex}, yIndex={yIndex}, InvertRgb={bInvertRgb}");

            for (int i = 1; i <= retryCount; i++)
            {
                await ctrl.WaitIfPausedOrCancelledAsync();

                Debug.WriteLine($"[{m_Context.AppName}] ===== Seqno OFR 시도 {i}/{retryCount} =====");

                // 1. 주문번호 셀 위치
                Draw.Rectangle rectSeqnoCell = m_RcptPage.DG오더_RelChildRects[seqnoColIndex, yIndex];

                Debug.WriteLine($"[{m_Context.AppName}] 주문번호 셀 위치 [{seqnoColIndex}, {yIndex}]: {rectSeqnoCell}");

                // 2. 셀 캡처
                Draw.Bitmap bmpCell = OfrService.CaptureScreenRect_InWndHandle(
                    m_RcptPage.DG오더_hWnd, rectSeqnoCell);

                if (bmpCell == null)
                {
                    Debug.WriteLine($"[{m_Context.AppName}] 주문번호 셀 캡처 실패 (시도 {i}/{retryCount})");
                    if (i < retryCount) await Task.Delay(200, ctrl.Token);
                    continue;
                }

                Draw.Bitmap bmpForOcr = null;

                try
                {
                    // 3. RGB 반전 (선택된 로우인 경우)
                    if (bInvertRgb)
                    {
                        bmpForOcr = OfrService.InvertBitmap(bmpCell);
                        bmpCell.Dispose();
                        Debug.WriteLine($"[{m_Context.AppName}] RGB 반전 완료");
                    }
                    else
                    {
                        bmpForOcr = bmpCell;
                    }

                    // 4. 단음소 OFR (TbCharBackup 사용)
                    StdResult_String resultSeqno = await OfrWork_Common.OfrStr_SeqCharAsync(bmpForOcr);

                    // ===== 성능 측정: 10회 반복 테스트 ===== (주석처리)
                    //List<long> times = new List<long>();
                    //for (int testIdx = 1; testIdx <= 10; testIdx++)
                    //{
                    //    Stopwatch sw = Stopwatch.StartNew();
                    //    resultSeqno = await OfrWork_Common.OfrStr_SeqCharAsync(bmpInverted);
                    //    sw.Stop();
                    //    times.Add(sw.ElapsedMilliseconds);
                    //    Debug.WriteLine($"[성능 테스트 {testIdx}/10] '{resultSeqno.strResult}' - {sw.ElapsedMilliseconds}ms");
                    //}

                    //// 성능 통계 출력
                    //long firstTime = times[0];
                    //long avgTime = (long)times.Skip(1).Average();
                    //long improvement = firstTime - avgTime;
                    //double improvementPercent = firstTime > 0 ? (improvement * 100.0 / firstTime) : 0;

                    //Debug.WriteLine($"[성능 통계]");
                    //Debug.WriteLine($"  첫 실행(캐시 MISS): {firstTime}ms");
                    //Debug.WriteLine($"  평균(캐시 HIT): {avgTime}ms");
                    //Debug.WriteLine($"  속도 향상: {improvement}ms ({improvementPercent:F1}%)");
                    // ===== 성능 측정 끝 =====

                    // 5. Seqno 반환
                    if (!string.IsNullOrEmpty(resultSeqno.strResult))
                    {
                        Debug.WriteLine($"[{m_Context.AppName}] Seqno 획득 성공: '{resultSeqno.strResult}' (시도 {i}/{retryCount})");
                        return new StdResult_String(resultSeqno.strResult);
                    }
                    else
                    {
                        Debug.WriteLine($"[{m_Context.AppName}] OFR 실패: {resultSeqno.sErr} (시도 {i}/{retryCount})");
                    }
                }
                finally
                {
                    if (bInvertRgb)
                    {
                        bmpForOcr?.Dispose();
                    }
                    else
                    {
                        // bmpForOcr는 bmpCell을 참조하므로 별도 Dispose 불필요
                    }
                }

                if (i < retryCount) await Task.Delay(200, ctrl.Token);
            }

            return new StdResult_String($"Seqno OFR 실패 ({retryCount}회 시도)", "GetSeqnoAsync_99");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{m_Context.AppName}] GetSeqnoAsync 예외: {ex.Message}");
            return new StdResult_String(StdUtil.GetExceptionMessage(ex), "GetSeqnoAsync_999");
        }
    }

    /// <summary>
    /// 지정된 로우의 기사전번 OFR
    /// - 셀 캡처 후 단음소 OFR (TbCharBackup 사용)
    /// </summary>
    /// <param name="rowIndex">데이터 로우 인덱스 (0부터 시작)</param>
    /// <param name="bInvertRgb">RGB 반전 여부 (선택된 로우 = true)</param>
    /// <param name="ctrl">취소 토큰</param>
    /// <param name="retryCount">재시도 횟수</param>
    /// <returns>성공: strResult에 기사전번, 실패: sErr에 에러 메시지</returns>
    private async Task<StdResult_String> GetDriverPhNoAsync(int rowIndex, bool bInvertRgb, CancelTokenControl ctrl, int retryCount = 3)
    {
        try
        {
            // 기사전번 컬럼 인덱스
            int driverPhNoColIndex = c_nCol기사전번;
            int yIndex = rowIndex + 2; // 헤더 2줄 추가

            Debug.WriteLine($"[{m_Context.AppName}] 기사전번 OFR - rowIndex={rowIndex}, yIndex={yIndex}, InvertRgb={bInvertRgb}");

            for (int i = 1; i <= retryCount; i++)
            {
                await ctrl.WaitIfPausedOrCancelledAsync();

                Debug.WriteLine($"[{m_Context.AppName}] ===== 기사전번 OFR 시도 {i}/{retryCount} =====");

                // 1. 기사전번 셀 위치
                Draw.Rectangle rectCell = m_RcptPage.DG오더_RelChildRects[driverPhNoColIndex, yIndex];

                Debug.WriteLine($"[{m_Context.AppName}] 기사전번 셀 위치 [{driverPhNoColIndex}, {yIndex}]: {rectCell}");

                // 2. 셀 캡처
                Draw.Bitmap bmpCell = OfrService.CaptureScreenRect_InWndHandle(
                    m_RcptPage.DG오더_hWnd, rectCell);

                if (bmpCell == null)
                {
                    Debug.WriteLine($"[{m_Context.AppName}] 기사전번 셀 캡처 실패 (시도 {i}/{retryCount})");
                    if (i < retryCount) await Task.Delay(200, ctrl.Token);
                    continue;
                }

                Draw.Bitmap bmpForOcr = null;

                try
                {
                    // 3. RGB 반전 (선택된 로우인 경우)
                    if (bInvertRgb)
                    {
                        bmpForOcr = OfrService.InvertBitmap(bmpCell);
                        bmpCell.Dispose();
                        Debug.WriteLine($"[{m_Context.AppName}] RGB 반전 완료");
                    }
                    else
                    {
                        bmpForOcr = bmpCell;
                    }

                    // 4. 단음소 OFR (TbCharBackup 사용)
                    StdResult_String resultDriverPhNo = await OfrWork_Common.OfrStr_SeqCharAsync(bmpForOcr);

                    // 5. 기사전번 반환
                    if (!string.IsNullOrEmpty(resultDriverPhNo.strResult))
                    {
                        Debug.WriteLine($"[{m_Context.AppName}] 기사전번 획득 성공: '{resultDriverPhNo.strResult}' (시도 {i}/{retryCount})");
                        return new StdResult_String(resultDriverPhNo.strResult);
                    }
                    else
                    {
                        Debug.WriteLine($"[{m_Context.AppName}] OFR 실패: {resultDriverPhNo.sErr} (시도 {i}/{retryCount})");
                    }
                }
                finally
                {
                    if (bInvertRgb)
                    {
                        bmpForOcr?.Dispose();
                    }
                    else
                    {
                        // bmpForOcr는 bmpCell을 참조하므로 별도 Dispose 불필요
                    }
                }

                if (i < retryCount) await Task.Delay(200, ctrl.Token);
            }

            return new StdResult_String($"기사전번 OFR 실패 ({retryCount}회 시도)", "GetDriverPhNoAsync_99");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{m_Context.AppName}] GetDriverPhNoAsync 예외: {ex.Message}");
            return new StdResult_String(StdUtil.GetExceptionMessage(ex), "GetDriverPhNoAsync_999");
        }
    }

    /// <summary>
    /// 조회 버튼 클릭 및 데이터 로딩 대기
    /// - WaitPanLoadedAsync로 로딩 완료 확인
    /// - 재시도 로직 포함
    /// </summary>
    /// <param name="ctrl">취소 토큰</param>
    /// <param name="retryCount">재시도 횟수</param>
    /// <returns>Success: 조회 완료, Fail: 조회 실패</returns>
    public async Task<StdResult_Status> Click조회버튼Async(CancelTokenControl ctrl, int retryCount = 3)
    {
        try
        {
            for (int i = 1; i <= retryCount; i++)
            {
                await ctrl.WaitIfPausedOrCancelledAsync();

                //Debug.WriteLine($"[{m_Context.AppName}] 조회 버튼 클릭 시도 {i}/{retryCount}");

                // 조회 버튼 클릭
                await Std32Mouse_Post.MousePostAsync_ClickLeft(m_RcptPage.CmdBtn_hWnd조회);

                // 로딩 패널 대기 - 이것만으로 성공 여부 판단
                StdResult_Status resultSts = await WaitPanLoadedAsync(m_RcptPage.DG오더_hWnd, ctrl);

                if (resultSts.Result == StdResult.Success || resultSts.Result == StdResult.Skip)
                {
                    //Debug.WriteLine($"[{m_Context.AppName}] 조회 완료 (시도 {i}회, 결과: {resultSts.Result})");
                    return new StdResult_Status(StdResult.Success, "조회 완료");
                }

                // Fail = 타임아웃 → 재시도
                Debug.WriteLine($"[{m_Context.AppName}] 조회 실패 (시도 {i}회): 타임아웃");
                await Task.Delay(CommonVars.c_nWaitNormal, ctrl.Token);
            }

            return new StdResult_Status(StdResult.Fail, $"조회 버튼 클릭 {retryCount}회 모두 실패", "InsungsAct_RcptRegPage/Click조회버튼Async_01");
        }
        catch (Exception ex)
        {
            return new StdResult_Status(StdResult.Fail, StdUtil.GetExceptionMessage(ex), "InsungsAct_RcptRegPage/Click조회버튼Async_999");
        }
    }

    /// <summary>
    /// 페이지별 예상 첫 로우 번호 계산 (0-based 페이지 인덱스)
    /// </summary>
    /// <param name="nTotRows">총 행 수</param>
    /// <param name="pageIdx">페이지 인덱스 (0-based)</param>
    /// <returns>예상 첫 로우 번호</returns>
    public static int GetExpectedFirstRowNum(int nTotRows, int pageIdx)
    {
        int nRowsPerPage = InsungsInfo_File.접수등록Page_DG오더_dataRowCount;

        // 총 페이지 수 계산
        int nTotPage = 1;
        if (nTotRows > nRowsPerPage)
        {
            nTotPage = nTotRows / nRowsPerPage;
            if (nTotRows % nRowsPerPage > 0)
                nTotPage += 1;
        }

        int nCurPage = pageIdx + 1;
        int nNum = (nRowsPerPage * pageIdx) + 1;

        if (nTotPage == 1) return 1;

        if (nCurPage < nTotPage) return nNum;

        // 마지막 페이지 특수 처리: 나머지 행이 있는 경우
        if (nTotRows % nRowsPerPage == 0) return nNum;
        else return nNum - nRowsPerPage + (nTotRows % nRowsPerPage);
    }

    /// <summary>
    /// 번호 컬럼(x=0)에서 첫 로우 번호 읽기
    /// </summary>
    /// <param name="bEdit">OFR 실패 시 수동 입력 다이얼로그 표시 여부 (기본값: false)</param>
    /// <returns>첫 로우 번호 (실패 시 -1)</returns>
    public async Task<int> ReadFirstRowNumAsync(bool bEdit = false)
    {
        IntPtr hWndDG = m_RcptPage.DG오더_hWnd;
        Draw.Rectangle[,] rects = m_RcptPage.DG오더_RelChildRects;
        int firstNum = -1;

        for (int y = 2; y < 2 + InsungsInfo_File.접수등록Page_DG오더_dataRowCount; y++)
        {
            // 1. 번호 컬럼(x=0) 캡처
            Draw.Rectangle rcNo = rects[0, y];
            Draw.Bitmap bmpNo = OfrService.CaptureScreenRect_InWndHandle(hWndDG, rcNo);
            if (bmpNo == null) continue;

            // 2. OFR (bEdit=false이면 대화상자 안 띄움)
            StdResult_String resultNo = await OfrWork_Common.OfrStr_SeqCharAsync(bmpNo, bEdit);
            bmpNo.Dispose();

            if (!string.IsNullOrEmpty(resultNo.strResult))
            {
                int curNum = StdConvert.StringToInt(resultNo.strResult, -1);
                if (curNum >= 1)
                {
                    firstNum = curNum - (y - 2);
                    //Debug.WriteLine($"[InsungsAct_RcptRegPage] ReadFirstRowNum: y={y}, curNum={curNum}, firstNum={firstNum}");
                    break;
                }
            }
        }

        return firstNum;
    }

    /// <summary>
    /// 페이지 검증 및 자동 조정
    /// </summary>
    /// <param name="nExpectedFirstNum">예상 첫 번호</param>
    /// <param name="ctrl">취소 토큰</param>
    /// <param name="nRetryCount">재시도 횟수 (기본값: 3)</param>
    /// <returns>성공/실패</returns>
    public async Task<StdResult_Status> VerifyAndAdjustPageAsync(int nExpectedFirstNum, CancelTokenControl ctrl, int nRetryCount = CommonVars.c_nRepeatShort)
    {
        for (int retry = 0; retry < nRetryCount; retry++)
        {
            await ctrl.WaitIfPausedOrCancelledAsync();

            // OFR로 실제 번호 읽기
            int nActualFirstNum = await ReadFirstRowNumAsync();
            //Debug.WriteLine($"[InsungsAct_RcptRegPage] OFR 결과 (시도 {retry + 1}/{nRetryCount}) - 예상={nExpectedFirstNum}, 실제={nActualFirstNum}");

            if (nExpectedFirstNum == nActualFirstNum)
            {
                //Debug.WriteLine($"[InsungsAct_RcptRegPage] ✓ 페이지 검증 성공 (시도 {retry + 1}회)");
                return new StdResult_Status(StdResult.Success);
            }

            Debug.WriteLine($"[InsungsAct_RcptRegPage] ✗ 페이지 검증 실패 (시도 {retry + 1}/{nRetryCount})");

            if (retry < nRetryCount - 1)  // 마지막 시도가 아니면 조정
            {
                // 바로잡기: 차이 계산
                int diff = nActualFirstNum - nExpectedFirstNum;
                int absDiff = Math.Abs(diff);
                int dataRowCount = InsungsInfo_File.접수등록Page_DG오더_dataRowCount;

                int pageClicks = absDiff / dataRowCount;
                int rowClicks = absDiff % dataRowCount;

                // 최적화: rowClicks > dataRowCount/2 이면 역방향이 더 효율적
                bool bReverse = false;
                if (rowClicks > dataRowCount / 2)
                {
                    pageClicks += 1;
                    rowClicks = dataRowCount - rowClicks;
                    bReverse = true;
                }

                // 방향 결정
                Draw.Point ptPage, ptRow;
                if ((diff > 0 && !bReverse) || (diff < 0 && bReverse))  // 위로
                {
                    ptPage = m_FileInfo.접수등록Page_DG오더_ptClkRel스크롤Up;
                    ptRow = m_FileInfo.접수등록Page_DG오더_ptClkRel버튼Up;
                    Debug.WriteLine($"[InsungsAct_RcptRegPage] 스크롤 조정: UP - {pageClicks}페이지 + {rowClicks}로우");
                }
                else  // 아래로
                {
                    ptPage = m_FileInfo.접수등록Page_DG오더_ptClkRel스크롤Down;
                    ptRow = m_FileInfo.접수등록Page_DG오더_ptClkRel버튼Down;
                    Debug.WriteLine($"[InsungsAct_RcptRegPage] 스크롤 조정: DOWN - {pageClicks}페이지 + {rowClicks}로우");
                }

                // 페이지 스크롤
                for (int i = 0; i < pageClicks; i++)
                {
                    await Std32Mouse_Post.MousePostAsync_ClickLeft_ptRel(m_RcptPage.DG오더_hWnd수직스크롤, ptPage);
                }

                // 로우 스크롤
                for (int i = 0; i < rowClicks; i++)
                {
                    await Std32Mouse_Post.MousePostAsync_ClickLeft_ptRel(m_RcptPage.DG오더_hWnd수직스크롤, ptRow);
                }

                await Task.Delay(CommonVars.c_nWaitNormal, ctrl.Token);
            }
        }

        return new StdResult_Status(StdResult.Fail, $"페이지 조정 {nRetryCount}회 모두 실패", "InsungsAct_RcptRegPage/VerifyAndAdjustPageAsync");
    }

    /// <summary>
    /// 유효 로우 갯수 얻기
    /// - 밝기 측정으로 실제 데이터가 있는 행 갯수 확인
    /// - 배경 밝기 - 1을 임계값으로 사용
    /// </summary>
    /// <param name="bmpPage">캡처된 데이터그리드 이미지</param>
    /// <returns>StdResult_Int (nResult: 유효 로우 갯수)</returns>
    public async Task<StdResult_Int> GetValidRowCountAsync(Draw.Bitmap bmpPage)
    {
        await Task.CompletedTask;

        Draw.Rectangle[,] rects = m_RcptPage.DG오더_RelChildRects;
        int nBackgroundBright = m_RcptPage.DG오더_nBackgroundBright;

        if (rects == null)
            return new StdResult_Int(0, "RelChildRects가 null입니다", "InsungsAct_RcptRegPage/GetValidRowCountAsync_01");

        if (nBackgroundBright <= 0 || nBackgroundBright > 255)
            return new StdResult_Int(0, $"배경 밝기 값이 유효하지 않음: {nBackgroundBright}", "InsungsAct_RcptRegPage/GetValidRowCountAsync_02");

        int nThreshold = nBackgroundBright - 1;
        int nValidRows = 0;

        for (int y = 2; y < rects.GetLength(1); y++)
        {
            Draw.Point ptCheck = new Draw.Point(rects[0, y].Right, rects[0, y].Top + 6);
            int nCurBright = OfrService.GetPixelBrightness(bmpPage, ptCheck);

            if (nCurBright < nThreshold)
                nValidRows++;
            else
                break;
        }

        //Debug.WriteLine($"[InsungsAct_RcptRegPage] 유효 로우: {nValidRows}개 (배경 밝기: {nBackgroundBright}, 임계값: {nThreshold})");
        return new StdResult_Int(nValidRows);
    }

    /// <summary>
    /// 캡처된 페이지 이미지에서 특정 로우의 주문번호 읽기
    /// </summary>
    /// <param name="bmpPage">캡처된 데이터그리드 전체 이미지</param>
    /// <param name="rectSeqno">주문번호 셀 Rectangle</param>
    /// <param name="bInvertRgb">RGB 반전 여부 (선택된 행인 경우 true)</param>
    /// <param name="ctrl">취소 토큰</param>
    /// <returns>StdResult_String (주문번호)</returns>
    public async Task<StdResult_String> GetRowSeqnoAsync(Draw.Bitmap bmpPage, Draw.Rectangle rectSeqno, bool bInvertRgb, CancelTokenControl ctrl)
    {
        await ctrl.WaitIfPausedOrCancelledAsync();
        return await OfrWork_Common.OfrStr_SeqCharAsync(bmpPage, rectSeqno, bInvertRgb);
    }

    /// <summary>
    /// 데이터그리드 Row에서 상태 읽기 (한글 OFR - 다음소)
    /// </summary>
    /// <param name="bmpPage">전체 페이지 비트맵 (재사용)</param>
    /// <param name="rectStatus">상태 컬럼 영역</param>
    /// <param name="bInvertRgb">RGB 반전 여부</param>
    /// <param name="ctrl">취소 토큰</param>
    /// <returns>상태 문자열 (앞 2글자: "접수", "배차", "취소" 등)</returns>
    public async Task<StdResult_String> GetRowStatusAsync(Draw.Bitmap bmpPage, Draw.Rectangle rectStatus, bool bInvertRgb, CancelTokenControl ctrl)
    {
        await ctrl.WaitIfPausedOrCancelledAsync();
        return await OfrWork_Common.OfrStr_ComplexCharSetAsync(bmpPage, rectStatus, bInvertRgb);
    }

    /// <summary>
    /// 캡처된 페이지 이미지에서 특정 로우의 기사전번 읽기 (단음소 OFR)
    /// </summary>
    /// <param name="bmpPage">캡처된 데이터그리드 전체 이미지</param>
    /// <param name="rectDriverPhNo">기사전번 셀 Rectangle</param>
    /// <param name="bInvertRgb">RGB 반전 여부 (선택된 행인 경우 true)</param>
    /// <param name="ctrl">취소 토큰</param>
    /// <returns>StdResult_String (기사전번)</returns>
    public async Task<StdResult_String> GetRowDriverPhNoAsync(Draw.Bitmap bmpPage, Draw.Rectangle rectDriverPhNo, bool bInvertRgb, CancelTokenControl ctrl)
    {
        await ctrl.WaitIfPausedOrCancelledAsync();
        return await OfrWork_Common.OfrStr_SeqCharAsync(bmpPage, rectDriverPhNo, bInvertRgb);
    }
    #endregion


    /// <summary>
    /// EditBox 입력 및 검증 (재시도 포함)
    /// - CommonVars 상수 사용: c_nRepeatShort, c_nWaitLong
    /// - CancelToken 완전 지원
    /// - 입력 검증 및 예외 처리 포함
    /// </summary>

    /// <summary>
    /// 공통 함수 #4: 요금종류 문자열을 인덱스로 변환
    /// - 선불=0, 착불=1, 신용=2, 송금=3, 수금=4, 카드=5
    /// - 미지원 타입은 0 (선불) 반환
    /// </summary>

    /// <summary>
    /// 차량종류 문자열을 인덱스로 변환
    /// - 오토=0, 밴=1, 트럭=2, 플렉스=3, 다마=4, 라보=5, 지하=6
    /// - 미지원 타입은 -1 반환
    /// </summary>

    /// <summary>
    /// 배송타입 문자열을 인덱스로 변환
    /// - 편도=0, 왕복=1, 경유=2, 긴급=3
    /// - 미지원 타입은 0 (편도) 반환
    /// </summary>

    /// <summary>
    /// 차량톤수 문자열을 ComboBox 인덱스로 변환
    /// </summary>

    /// <summary>
    /// 트럭상세 문자열을 ComboBox 인덱스로 변환
    /// </summary>

    /// <summary>
    /// 트럭 ComboBox 처리 헬퍼 - ComboBox 자동 열림 대기 및 항목 선택
    /// - hWndParent: 부모 윈도우 핸들
    /// - ptCheckAbs: ComboBox 열림 감지를 위한 절대 좌표 체크 포인트
    /// - hWndBefore: 트럭 클릭 전 저장한 Handle (변경 감지용)
    /// - itemIndex: 선택할 항목 인덱스
    /// - sFieldName: 필드명 (로그용)
    /// - maxAttempts: 최대 대기 횟수
    /// - ctrl: CancelToken 제어
    /// </summary>

    /// <summary>
    /// 공통 함수 #5: 요금종류 RadioButton 현재 상태 확인
    /// - bmpOrg: 화면 캡처 비트맵
    /// - btns: RadioButton 그룹
    /// - sFeeType: 확인할 요금종류 ("선불", "착불", 등)
    /// </summary>

    /// <summary>
    /// 배송타입 RadioButton 현재 상태 확인
    /// - bmpOrg: 화면 캡처 비트맵
    /// - btns: RadioButton 그룹
    /// - sDeliverType: 확인할 배송타입 ("편도", "왕복", "경유", "긴급")
    /// </summary>

    /// <summary>
    /// 차량종류 RadioButton 현재 상태 확인
    /// - bmpOrg: 화면 캡처 비트맵
    /// - btns: RadioButton 그룹
    /// - sCarType: 확인할 차량종류 ("오토", "밴", "트럭", 등)
    /// </summary>

    /// <summary>
    /// 공통 함수 #6: 요금종류 RadioButton 설정
    /// - bmpOrg: 화면 캡처 비트맵
    /// - btns: RadioButton 그룹
    /// - sFeeType: 설정할 요금종류
    /// - ctrl: CancelToken 제어
    /// </summary>


    /// <summary>
    /// 배송타입 RadioButton 설정
    /// - bmpOrg: 화면 캡처 비트맵
    /// - btns: RadioButton 그룹
    /// - sDeliverType: 설정할 배송타입
    /// - ctrl: CancelToken 제어
    /// </summary>
    private async Task<StdResult_Status> SetGroupDeliverTypeAsync(Draw.Bitmap bmpOrg, OfrModel_RadioBtns btns, string sDeliverType, CancelTokenControl ctrl)
    {
        int index = GetDeliverTypeIndex(sDeliverType);
        return await SetCheckRadioBtn_InGroupAsync(bmpOrg, btns, index, ctrl);
    }

    /// <summary>
    /// 트럭 RadioButton 클릭 + ComboBox 처리 (현재가 트럭 아닐 때)
    /// - bmpOrg: 화면 캡처 비트맵
    /// - btns: RadioButton 그룹
    /// - tbOrder: 주문 정보 (CarWeight, TruckDetail 사용)
    /// - ctrl: CancelToken 제어
    /// </summary>
    private async Task<StdResult_Status> SetGroupCarTypeAsync_트럭(Draw.Bitmap bmpOrg, OfrModel_RadioBtns btns, TbOrder tbOrder, CancelTokenControl ctrl)
    {
        try
        {
            // 1. 트럭 클릭 전 ComboBox 체크 위치의 Handle 저장
            Debug.WriteLine($"[{m_Context.AppName}] 트럭 선택 예정 → 차량무게, 트럭상세 ComboBox 체크 위치 Handle 저장...");

            IntPtr hWndParent = btns.hWndTop;
            Debug.WriteLine($"[{m_Context.AppName}] btns.hWndTop={btns.hWndTop:X}, btns.hWndMid={btns.hWndMid:X}");

            // 차량무게 체크 위치: 상대→절대 좌표 변환 및 Handle 저장
            Draw.Point ptCheckCarWeightComboOpen = StdUtil.GetAbsDrawPointFromRel(hWndParent, m_Context.FileInfo.접수등록Wnd_우측상단_ptChkRel차량톤수Open);
            IntPtr hWndCheckCarWeightComboOpen = Std32Window.GetWndHandle_FromAbsDrawPt(ptCheckCarWeightComboOpen);
            Debug.WriteLine($"[{m_Context.AppName}] 차량무게 - 상대좌표: {m_Context.FileInfo.접수등록Wnd_우측상단_ptChkRel차량톤수Open}, 절대좌표: {ptCheckCarWeightComboOpen}, Handle={hWndCheckCarWeightComboOpen:X}");

            // 트럭상세 체크 위치: 상대→절대 좌표 변환 및 Handle 저장
            Draw.Point ptCheckTruckDetailComboOpen = StdUtil.GetAbsDrawPointFromRel(hWndParent, m_Context.FileInfo.접수등록Wnd_우측상단_ptChkRel트럭상세Open);
            IntPtr hWndCheckTruckDetailComboOpen = Std32Window.GetWndHandle_FromAbsDrawPt(ptCheckTruckDetailComboOpen);
            Debug.WriteLine($"[{m_Context.AppName}] 트럭상세 - 상대좌표: {m_Context.FileInfo.접수등록Wnd_우측상단_ptChkRel트럭상세Open}, 절대좌표: {ptCheckTruckDetailComboOpen}, Handle={hWndCheckTruckDetailComboOpen:X}");

            // 2. 트럭 RadioButton 클릭
            int index = GetCarTypeIndex("트럭");
            StdResult_Status result = await SetCheckRadioBtn_InGroupAsync(bmpOrg, btns, index, ctrl);
            if (result.Result != StdResult.Success)
            {
                return new StdResult_Status(StdResult.Fail, "트럭 RadioButton 클릭 실패", "SetGroupCarTypeAsync_트럭_01");
            }
            Debug.WriteLine($"[{m_Context.AppName}] 트럭 선택 완료 → 차량무게 ComboBox 열림 대기...");

            // 3. 차량무게 ComboBox 처리
            Debug.WriteLine($"[{m_Context.AppName}]   차량무게: {tbOrder.CarWeight}");

            int indexCarWeight = Get차량톤수Index(tbOrder.CarWeight);
            Debug.WriteLine($"[{m_Context.AppName}] Get차량톤수Index(\"{tbOrder.CarWeight}\") 결과: indexCarWeight={indexCarWeight}");

            if (indexCarWeight == 0)
            {
                return new StdResult_Status(StdResult.Fail, $"지원하지 않는 차량무게: {tbOrder.CarWeight}", "SetGroupCarTypeAsync_트럭_02");
            }

            result = await Select트럭ComboBoxItemAsync(hWndParent, ptCheckCarWeightComboOpen, hWndCheckCarWeightComboOpen, indexCarWeight, "차량무게", 100, ctrl);

            if (result.Result != StdResult.Success)
            {
                return new StdResult_Status(StdResult.Fail, $"차량무게 ComboBox 처리 실패: {result.sErr}", "SetGroupCarTypeAsync_트럭_03");
            }

            // 4. 트럭상세 ComboBox 처리
            Debug.WriteLine($"[{m_Context.AppName}] 차량무게 선택 완료 → 트럭상세 ComboBox 열림 대기...");

            int indexTruckDetail = Get트럭상세Index(tbOrder.TruckDetail);
            Debug.WriteLine($"[{m_Context.AppName}] Get트럭상세Index(\"{tbOrder.TruckDetail}\") 결과: indexTruckDetail={indexTruckDetail}");

            if (indexTruckDetail == 0)
            {
                return new StdResult_Status(StdResult.Fail, $"지원하지 않는 트럭상세: {tbOrder.TruckDetail}", "SetGroupCarTypeAsync_트럭_04");
            }

            result = await Select트럭ComboBoxItemAsync(hWndParent, ptCheckTruckDetailComboOpen, hWndCheckTruckDetailComboOpen, indexTruckDetail, "트럭상세", 50, ctrl);

            if (result.Result != StdResult.Success)
            {
                return new StdResult_Status(StdResult.Fail, $"트럭상세 ComboBox 처리 실패: {result.sErr}", "SetGroupCarTypeAsync_트럭_05");
            }

            Debug.WriteLine($"[{m_Context.AppName}] 트럭 ComboBox 처리 완료");

            return new StdResult_Status(StdResult.Success);
        }
        catch (Exception ex)
        {
            return new StdResult_Status(StdResult.Fail, StdUtil.GetExceptionMessage(ex), "SetGroupCarTypeAsync_트럭_999");
        }
    }

    /// <summary>
    /// 공통 함수 #7: RadioButton 그룹에서 특정 버튼 클릭 (OFR 기반)
    /// - bmpOrg: 화면 캡처 비트맵
    /// - btns: RadioButton 그룹
    /// - index: 클릭할 RadioButton 인덱스
    /// - ctrl: CancelToken 제어
    /// </summary>
    private async Task<StdResult_Status> SetCheckRadioBtn_InGroupAsync(Draw.Bitmap bmpOrg, OfrModel_RadioBtns btns, int index, CancelTokenControl ctrl)
    {
        try
        {
            await ctrl.WaitIfPausedOrCancelledAsync();
            OfrModel_RadioBtn btn = btns.Btns[index];

            // 1. 이미 선택되어 있는지 확인
            StdResult_NulBool resultRadio = await OfrWork_Insungs.OfrImgChkValue_RectInBitmapAsync(bmpOrg, btn.rcRelPartsT);

            if (resultRadio.bResult == null)
            {
                return new StdResult_Status(StdResult.Fail, "요금종류 RadioButton 인식 실패", "SetCheckRadioBtn_InGroupAsync_01");
            }

            if (StdConvert.NullableBoolToBool(resultRadio.bResult))
            {
                return new StdResult_Status(StdResult.Success);  // 이미 선택됨
            }

            // 2. 선택되지 않았으면 클릭 (재시도 10회)
            for (int j = 0; j < CommonVars.c_nRepeatNormal; j++)  // 10회
            {
                await ctrl.WaitIfPausedOrCancelledAsync();

                await Std32Mouse_Post.MousePostAsync_ClickLeft_ptRel(btns.hWndMid, btn._ptRelPartsM);

                resultRadio = await OfrWork_Insungs.OfrImgUntilChkValue_RectInHWndAsync(btns.hWndMid, true, btn.rcRelPartsM);
                if (StdConvert.NullableBoolToBool(resultRadio.bResult))
                {
                    return new StdResult_Status(StdResult.Success);  // 클릭 성공
                }

                await Task.Delay(CommonVars.c_nWaitNormal, ctrl.Token);  // 100ms
            }

            // 3. 최종 확인
            resultRadio = await OfrWork_Insungs.OfrImgUntilChkValue_RectInHWndAsync(btns.hWndMid, true, btn.rcRelPartsM);
            if (StdConvert.NullableBoolToBool(resultRadio.bResult))
            {
                return new StdResult_Status(StdResult.Success);
            }

            return new StdResult_Status(StdResult.Fail, "요금종류 RadioButton 클릭 실패", "SetCheckRadioBtn_InGroupAsync_02");
        }
        catch (Exception ex)
        {
            return new StdResult_Status(StdResult.Fail, StdUtil.GetExceptionMessage(ex), "SetCheckRadioBtn_InGroupAsync_999");
        }
    }

    #region Region 6. 주문 업데이트 (CheckIsOrderAsync_AssumeKaiUpdated)

    /// <summary>
    /// Kai DB에서 업데이트된 주문을 인성 앱에 반영
    /// </summary>
    public async Task<CommonResult_AutoAllocProcess> CheckIsOrderAsync_AssumeKaiUpdated(AutoAllocModel item, CommonResult_AutoAllocDatagrid dgInfo, CancelTokenControl ctrl)
    {
        await ctrl.WaitIfPausedOrCancelledAsync();

        string kaiState = item.NewOrder.OrderState;
        string isState = dgInfo.sStatus;

        Debug.WriteLine($"[{m_Context.AppName}] CheckIsOrderAsync_AssumeKaiUpdated 호출됨");
        Debug.WriteLine($"  - KeyCode: {item.KeyCode}, Insung1: {item.NewOrder.Insung1 ?? "(없음)"}");
        Debug.WriteLine($"  - Kai 상태: {kaiState}, IS 상태: {isState}");
        Debug.WriteLine($"  - DG Index: {dgInfo.nIndex}");

        // 상태가 같은 경우: 필드만 업데이트
        if (kaiState == isState)
        {
            Debug.WriteLine($"  → 같은 상태: 필드만 업데이트 시도");
            var result = await UpdateOrderSameStateAsync(item, dgInfo, ctrl);
            Debug.WriteLine($"[{m_Context.AppName}] CheckIsOrderAsync_AssumeKaiUpdated 반환: ResultType={result.ResultType}, Message={result.sErr}");
            return result;
        }
        // 상태가 다른 경우: 필드 업데이트 + 상태 전환
        else
        {
            Debug.WriteLine($"  → 다른 상태: 필드 업데이트 + 상태 전환 시도 ({isState} → {kaiState})");
            var result = await UpdateOrderDiffStateAsync(item, dgInfo, kaiState, isState, ctrl);
            Debug.WriteLine($"[{m_Context.AppName}] CheckIsOrderAsync_AssumeKaiUpdated 반환: ResultType={result.ResultType}, Message={result.sErr}");
            return result;
        }
    }

    /// <summary>
    /// 같은 상태: 필드만 선별 업데이트
    /// </summary>
    private async Task<CommonResult_AutoAllocProcess> UpdateOrderSameStateAsync(AutoAllocModel item, CommonResult_AutoAllocDatagrid dgInfo, CancelTokenControl ctrl)
    {
        string isState = dgInfo.sStatus;
        bool useRepeat;

        // 인성 앱 특성: 상태가 변경되면 저장 안 됨 → 같은 상태 버튼 클릭 필요
        // 대기/취소: 외부에서 상태 변경 불가 → 반복 불필요
        // 접수/배차: 외부에서 상태 변경 가능 → 타이밍 이슈 대비 반복 필요
        switch (isState)
        {
            case "취소":
            case "대기":
                useRepeat = false;  // 외부 변경 없음 → 1번만
                break;

            case "접수":
            case "배차":
                useRepeat = true;   // 외부 변경 가능 → 10번 재시도
                break;

            default:
                return CommonResult_AutoAllocProcess.FailureAndDiscard($"미구현 상태(SameState): Kai={isState}, IS={isState}", "UpdateOrderSameStateAsync_01");
        }

        // 팝업 열기 → 필드 업데이트 → 같은 상태 버튼 클릭 → 저장/닫기
        var result = await UpdateOrderInPopupAsync("", item, dgInfo, useRepeat, ctrl);
        Debug.WriteLine($"[{m_Context.AppName}] UpdateOrderSameStateAsync 반환: ResultType={result.ResultType}, Message={result.sErr}");
        return result;
    }

    /// <summary>
    /// 다른 상태: 필드 업데이트 + 상태 전환
    /// </summary>
    private async Task<CommonResult_AutoAllocProcess> UpdateOrderDiffStateAsync(AutoAllocModel item, CommonResult_AutoAllocDatagrid dgInfo, string kaiState, string isState, CancelTokenControl ctrl)
    {
        string wantState = kaiState; // Kai DB의 목표 상태로 전환
        bool useRepeat;

        // 상태 전환 규칙에 따라 반복 횟수 결정
        switch (kaiState)
        {
            case "접수":
                switch (isState)
                {
                    case "취소": // 취소 → 접수
                    case "대기": // 대기 → 접수
                        useRepeat = true; // 10번 재시도
                        break;

                    case "운행": // 운행 → 접수
                        Debug.WriteLine($"  → StateFlag를 NotChanged로 변경 후 재적재 요청");
                        return CommonResult_AutoAllocProcess.SuccessAndReEnqueue(item, PostgService_Common_OrderState.NotChanged);

                    default:
                        return CommonResult_AutoAllocProcess.FailureAndDiscard($"미구현 전환: Kai=접수, IS={isState}", "UpdateOrderDiffStateAsync_01");
                }
                break;

            case "대기":
                switch (isState)
                {
                    case "취소": // 취소 → 대기
                        useRepeat = false; // 1번만
                        break;
                    case "접수": // 접수 → 대기
                    case "배차": // 배차 → 대기
                        useRepeat = true; // 10번 재시도
                        break;
                    default:
                        return CommonResult_AutoAllocProcess.FailureAndDiscard($"미구현 전환: Kai=대기, IS={isState}", "UpdateOrderDiffStateAsync_02");
                }
                break;

            case "취소":
                switch (isState)
                {
                    case "접수": // 접수 → 취소
                    case "배차": // 배차 → 취소
                    case "운행": // 운행 → 취소
                        useRepeat = true; // 10번 재시도
                        break;
                    case "예약": // 예약 → 취소
                    case "완료": // 완료 → 취소
                    case "대기": // 대기 → 취소
                        useRepeat = false; // 1번만
                        break;
                    default:
                        return CommonResult_AutoAllocProcess.FailureAndDiscard($"미구현 전환: Kai=취소, IS={isState}", "UpdateOrderDiffStateAsync_03");
                }
                break;

            default:
                return CommonResult_AutoAllocProcess.FailureAndDiscard($"미구현 상태(DiffState): Kai={kaiState}, IS={isState}", "UpdateOrderDiffStateAsync_04");
        }

        // 팝업 열기 → 필드 업데이트 → 상태 전환 → 저장/닫기
        return await UpdateOrderInPopupAsync(wantState, item, dgInfo, useRepeat, ctrl);
    }

    /// <summary>
    /// 팝업 내 주문 업데이트 실행 (핵심 로직)
    /// </summary>
    private async Task<CommonResult_AutoAllocProcess> UpdateOrderInPopupAsync(string wantState, AutoAllocModel item, CommonResult_AutoAllocDatagrid dgInfo, bool useRepeat, CancelTokenControl ctrl)
    {
        int repeatCount = useRepeat ? CommonVars.c_nRepeatNormal : 1;

        Debug.WriteLine($"[{m_Context.AppName}] UpdateOrderInPopupAsync 시작");
        Debug.WriteLine($"  - wantState: '{wantState}' (비어있으면 상태 버튼 안 누름)");
        Debug.WriteLine($"  - useRepeat: {useRepeat} (반복 횟수: {repeatCount})");

        for (int attempt = 0; attempt < repeatCount; attempt++)
        {
            await ctrl.WaitIfPausedOrCancelledAsync();

            // 1. 팝업 열기
            Debug.WriteLine($"[{m_Context.AppName}] 1단계: 팝업 열기 시도 (시도 {attempt + 1}/{repeatCount})");
            var (wnd, openError) = await OpenEditPopupAsync(dgInfo.nIndex, ctrl);

            if (openError != null)
            {
                Debug.WriteLine($"[{m_Context.AppName}] 팝업 열기 실패: {openError.sErr}");

                if (attempt < repeatCount - 1)
                {
                    Debug.WriteLine($"[{m_Context.AppName}] 재시도 예정 ({attempt + 1}/{repeatCount})");
                    continue;
                }

                // 모든 시도 실패 - 재시도 큐로
                Debug.WriteLine($"[{m_Context.AppName}] 팝업 열기 실패 (KeyCode: {item.KeyCode})");
                return CommonResult_AutoAllocProcess.SuccessAndReEnqueue();
            }

            Debug.WriteLine($"[{m_Context.AppName}] 팝업 열기 성공");

            // 2. 의뢰자 영역 업데이트
            Debug.WriteLine($"[{m_Context.AppName}] 2단계: 의뢰자 영역 업데이트 (KeyCode: {item.KeyCode})");
            var (changeCount, updateError) = await Update의뢰자영역Async(wnd, item.NewOrder, ctrl);

            if (updateError != null)
            {
                Debug.WriteLine($"[{m_Context.AppName}] 의뢰자 영역 업데이트 실패 (KeyCode: {item.KeyCode}): {updateError.sErr}");
                // 팝업 닫고 재시도
                await CloseEditPopupAsync(wnd, shouldSave: false, ctrl);
                return CommonResult_AutoAllocProcess.SuccessAndReEnqueue();
            }

            Debug.WriteLine($"[{m_Context.AppName}] 의뢰자 영역 업데이트 완료 (변경: {changeCount}개)");

            // 2-2. 출발지 영역 업데이트
            Debug.WriteLine($"[{m_Context.AppName}] 2-2단계: 출발지 영역 업데이트 (KeyCode: {item.KeyCode})");
            var (changeCount출발, updateError출발) = await Update출발지영역Async(wnd, item.NewOrder, ctrl);

            if (updateError출발 != null)
            {
                Debug.WriteLine($"[{m_Context.AppName}] 출발지 영역 업데이트 실패 (KeyCode: {item.KeyCode}): {updateError출발.sErr}");
                // 팝업 닫고 재시도
                await CloseEditPopupAsync(wnd, shouldSave: false, ctrl);
                return CommonResult_AutoAllocProcess.SuccessAndReEnqueue();
            }

            Debug.WriteLine($"[{m_Context.AppName}] 출발지 영역 업데이트 완료 (변경: {changeCount출발}개)");

            // 2-3. 도착지 영역 업데이트
            Debug.WriteLine($"[{m_Context.AppName}] 2-3단계: 도착지 영역 업데이트 (KeyCode: {item.KeyCode})");
            var (changeCount도착, updateError도착) = await Update도착지영역Async(wnd, item.NewOrder, ctrl);

            if (updateError도착 != null)
            {
                Debug.WriteLine($"[{m_Context.AppName}] 도착지 영역 업데이트 실패 (KeyCode: {item.KeyCode}): {updateError도착.sErr}");
                // 팝업 닫고 재시도
                await CloseEditPopupAsync(wnd, shouldSave: false, ctrl);
                return CommonResult_AutoAllocProcess.SuccessAndReEnqueue();
            }

            Debug.WriteLine($"[{m_Context.AppName}] 도착지 영역 업데이트 완료 (변경: {changeCount도착}개)");

            // 2-4. 우측상단 섹션 업데이트 (적요, 공유, 요금종류, 차량종류, 배송타입, 계산서)
            Debug.WriteLine($"[{m_Context.AppName}] 2-4단계: 우측상단 섹션 업데이트 시작");
            var (changeCount우측, updateError우측) = await Update우측상단영역Async(wnd, item.NewOrder, ctrl);

            if (updateError우측 != null)
            {
                Debug.WriteLine($"[{m_Context.AppName}] 우측상단 섹션 업데이트 실패 (KeyCode: {item.KeyCode}): {updateError우측.sErr}");
                await CloseEditPopupAsync(wnd, shouldSave: false, ctrl);
                return CommonResult_AutoAllocProcess.SuccessAndReEnqueue();
            }

            Debug.WriteLine($"[{m_Context.AppName}] 우측상단 섹션 업데이트 완료 (변경: {changeCount우측}개)");

            // 2-5. 요금 영역 업데이트 (순서: 우측상단 다음)
            Debug.WriteLine($"[{m_Context.AppName}] 2-5단계: 요금 영역 업데이트 시작");
            var (changeCount요금, updateError요금) = await Update요금영역Async(wnd, item.NewOrder, ctrl);

            if (updateError요금 != null)
            {
                Debug.WriteLine($"[{m_Context.AppName}] 요금 영역 업데이트 실패 (KeyCode: {item.KeyCode}): {updateError요금.sErr}");
                await CloseEditPopupAsync(wnd, shouldSave: false, ctrl);
                return CommonResult_AutoAllocProcess.SuccessAndReEnqueue();
            }

            Debug.WriteLine($"[{m_Context.AppName}] 요금 영역 업데이트 완료 (변경: {changeCount요금}개)");

            // 2-6. 오더메모 영역 업데이트
            Debug.WriteLine($"[{m_Context.AppName}] 2-6단계: 오더메모 영역 업데이트 시작");
            var (changeCount메모, updateError메모) = await Update오더메모영역Async(wnd, item.NewOrder, ctrl);

            if (updateError메모 != null)
            {
                Debug.WriteLine($"[{m_Context.AppName}] 오더메모 영역 업데이트 실패 (KeyCode: {item.KeyCode}): {updateError메모.sErr}");
                await CloseEditPopupAsync(wnd, shouldSave: false, ctrl);
                return CommonResult_AutoAllocProcess.SuccessAndReEnqueue();
            }

            Debug.WriteLine($"[{m_Context.AppName}] 오더메모 영역 업데이트 완료 (변경: {changeCount메모}개)");

            // 2-7. 상태 버튼 클릭 (wantState가 비어있지 않으면)
            int changeCount상태 = 0;
            if (!string.IsNullOrEmpty(wantState))
            {
                Debug.WriteLine($"[{m_Context.AppName}] 2-7단계: 상태 버튼 클릭 (목표 상태: {wantState})");

                IntPtr hWndStateBtn = wantState switch
                {
                    "접수" => wnd.Btn_hWnd접수상태,
                    "완료" => wnd.Btn_hWnd처리완료,
                    "대기" => wnd.Btn_hWnd대기,
                    "취소" => wnd.Btn_hWnd주문취소,
                    _ => IntPtr.Zero
                };

                if (hWndStateBtn == IntPtr.Zero)
                {
                    Debug.WriteLine($"[{m_Context.AppName}] 알 수 없는 상태 (wantState: {wantState}, KeyCode: {item.KeyCode})");
                    await CloseEditPopupAsync(wnd, shouldSave: false, ctrl);
                    return CommonResult_AutoAllocProcess.SuccessAndReEnqueue();
                }

                await Std32Mouse_Post.MousePostAsync_ClickLeft_Center(hWndStateBtn);
                Debug.WriteLine($"[{m_Context.AppName}] 상태 버튼 클릭 완료: {wantState}");

                // 상태 버튼 클릭은 항상 변경건으로 취급 (백업 로직: nChanged = 101~104)
                changeCount상태 = 1;

                await Task.Delay(CommonVars.c_nWaitNormal, ctrl.Token);
            }

            int totalChangeCount = changeCount + changeCount출발 + changeCount도착 + changeCount우측 + changeCount요금 + changeCount메모 + changeCount상태;
            Debug.WriteLine($"[{m_Context.AppName}] 전체 업데이트 완료 (총 변경: {totalChangeCount}개, 상태버튼: {changeCount상태})");

            // 3. 팝업 닫기 (변경사항 있으면 저장, 없으면 그냥 닫기)
            bool shouldSave = totalChangeCount > 0;
            Debug.WriteLine($"[{m_Context.AppName}] 3단계: 팝업 닫기 시도 (shouldSave: {shouldSave})");
            bool closed = await CloseEditPopupAsync(wnd, shouldSave, ctrl);

            if (!closed)
            {
                Debug.WriteLine($"[{m_Context.AppName}] 팝업 닫기 실패 - 재시도 (KeyCode: {item.KeyCode})");
                return CommonResult_AutoAllocProcess.SuccessAndReEnqueue();
            }

            Debug.WriteLine($"[{m_Context.AppName}] 팝업 닫기 성공 - 의뢰자+출발지+도착지+우측상단+요금+메모 영역 업데이트 완료");

            // 취소 상태로 전환: 비적재 (더 이상 모니터링 불필요)
            // 그 외: 재적재 (계속 모니터링)
            if (wantState == "취소")
            {
                Debug.WriteLine($"[{m_Context.AppName}] 취소 상태 전환 완료 - 큐에서 제거 (변경: {totalChangeCount}개, KeyCode: {item.KeyCode})");
                return CommonResult_AutoAllocProcess.SuccessAndComplete();
            }
            else
            {
                Debug.WriteLine($"[{m_Context.AppName}] 모든 영역 업데이트 완료 (변경: {totalChangeCount}개, KeyCode: {item.KeyCode})");
                return CommonResult_AutoAllocProcess.SuccessAndReEnqueue();
            }
        }

        // 모든 재시도 실패 (정상적으로는 위에서 return되므로 여기 도달 안 함)
        Debug.WriteLine($"[{m_Context.AppName}] 모든 재시도 실패 (KeyCode: {item.KeyCode})");
        return CommonResult_AutoAllocProcess.SuccessAndReEnqueue();
    }

    #region 팝업 제어

    /// <summary>
    /// 수정 팝업 열기 (Datagrid 로우 더블클릭)
    /// </summary>
    private async Task<(InsungsInfo_Mem.RcptWnd_Edit wnd, StdResult_Error error)> OpenEditPopupAsync(int rowIndex, CancelTokenControl ctrl)
    {
        try
        {
            await ctrl.WaitIfPausedOrCancelledAsync();

            // 1. DG 로우 더블클릭 (헤더 2줄 추가)
            int selIndex = rowIndex + 2;
            Draw.Point ptRel = StdUtil.GetCenterDrawPoint(m_RcptPage.DG오더_RelChildRects[c_nCol번호, selIndex]);

            Debug.WriteLine($"[{m_Context.AppName}] 팝업 열기 시도: rowIndex={rowIndex}, selIndex={selIndex}, ptRel={ptRel}");

            // 2. 팝업창 찾기 (최대 c_nRepeatNormal번 시도)
            bool bFind = false;
            IntPtr hWndPopup = IntPtr.Zero;

            for (int j = 0; j < CommonVars.c_nRepeatNormal; j++)
            {
                await ctrl.WaitIfPausedOrCancelledAsync();

                // 더블클릭 (SendMessage 방식, BlockInput 사용)
                await Simulation_Mouse.SafeMouseSend_DblClickLeft_ptRelAsync(m_RcptPage.DG오더_hWnd, ptRel);
                Debug.WriteLine($"[{m_Context.AppName}] 더블클릭 실행 (시도 {j + 1}/{CommonVars.c_nRepeatNormal})");

                // 팝업창 나타날 때까지 대기 (최대 5초)
                for (int k = 0; k < 100; k++)
                {
                    await Task.Delay(50, ctrl.Token);

                    // 팝업창 찾기
                    hWndPopup = Std32Window.FindStartWithMainWindow_NotTransparent(m_MemInfo.Splash.TopWnd_uProcessId, m_FileInfo.접수등록Wnd_TopWnd_sWndStartsWith);
                    if (hWndPopup == IntPtr.Zero) continue;

                    // 닫기 버튼으로 팝업 검증
                    IntPtr hWndClose = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_신규버튼그룹_ptChkRel닫기);
                    if (hWndClose == IntPtr.Zero) continue;

                    string closeBtnText = Std32Window.GetWindowCaption(hWndClose);
                    if (closeBtnText.StartsWith(m_FileInfo.접수등록Wnd_버튼그룹_sWndName닫기))
                    {
                        bFind = true;
                        Debug.WriteLine($"[{m_Context.AppName}] 팝업창 찾음: hWnd={hWndPopup:X}, 닫기버튼={closeBtnText}");
                        break;
                    }
                }

                if (bFind) break;
                await Task.Delay(100, ctrl.Token);
            }

            if (!bFind)
            {
                Debug.WriteLine($"[{m_Context.AppName}] 팝업창 찾기 실패");
                return (null, new StdResult_Error($"[{m_Context.AppName}] 주문번호 클릭 후 팝업창이 안뜸", "OpenEditPopupAsync_01"));
            }

            await Task.Delay(100, ctrl.Token);

            // 3. RcptWnd_Edit 생성
            var wndRcpt = new InsungsInfo_Mem.RcptWnd_Edit(hWndPopup, m_FileInfo);
            Debug.WriteLine($"[{m_Context.AppName}] RcptWnd_Edit 생성 완료");

            return (wndRcpt, null);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{m_Context.AppName}] OpenEditPopupAsync 예외: {ex.Message}");
            return (null, new StdResult_Error(StdUtil.GetExceptionMessage(ex), "OpenEditPopupAsync_999"));
        }
    }

    /// <summary>
    /// 팝업 닫기 (저장 또는 취소)
    /// </summary>
    private async Task<bool> CloseEditPopupAsync(InsungsInfo_Mem.RcptWnd_Edit wnd, bool shouldSave, CancelTokenControl ctrl)
    {
        try
        {
            await ctrl.WaitIfPausedOrCancelledAsync();

            bool bClosed;

            if (shouldSave)
            {
                // 변경사항이 있으니 저장
                Debug.WriteLine($"[{m_Context.AppName}] 변경내용이 있습니다 - 저장 버튼 클릭");
                await Task.Delay(CommonVars.c_nWaitNormal, ctrl.Token);

                bClosed = await ClickNWaitWindowChangedAsync_OrFind확인창(wnd.Btn_hWnd저장, wnd.TopWnd_hWnd, ctrl);

                if (!bClosed)
                {
                    Debug.WriteLine($"[{m_Context.AppName}] 저장 실패 (확인창 나타남) - 확인창 닫기 시도");

                    // 1. 확인창 닫기
                    bool confirmClosed = await CloseConfirmWindowAsync(ctrl);
                    if (confirmClosed)
                    {
                        Debug.WriteLine($"[{m_Context.AppName}] 확인창 닫기 성공");
                    }
                    else
                    {
                        Debug.WriteLine($"[{m_Context.AppName}] 확인창 닫기 실패 또는 확인창 없음");
                    }

                    // 2. 팝업 닫기 버튼 클릭
                    await Task.Delay(CommonVars.c_nWaitShort, ctrl.Token);
                    bClosed = await ClickNWaitWindowChangedAsync(wnd.Btn_hWnd닫기, wnd.TopWnd_hWnd, ctrl);

                    if (bClosed)
                    {
                        Debug.WriteLine($"[{m_Context.AppName}] 저장 실패 후 팝업 닫기 성공 - 재시도 필요");
                    }
                    else
                    {
                        Debug.WriteLine($"[{m_Context.AppName}] 저장 실패 후 팝업 닫기도 실패");
                    }

                    // 3. 재시도를 위해 false 반환
                    return false;
                }

                Debug.WriteLine($"[{m_Context.AppName}] 저장 성공 - 팝업 닫힘");
            }
            else
            {
                // 변경사항이 없으니 그냥 닫기
                Debug.WriteLine($"[{m_Context.AppName}] 변경내용이 없습니다 - 닫기 버튼 클릭");
                Debug.WriteLine($"[{m_Context.AppName}] 닫기 버튼 핸들: {wnd.Btn_hWnd닫기:X}, 팝업 핸들: {wnd.TopWnd_hWnd:X}");

                Debug.WriteLine($"[{m_Context.AppName}] ClickNWaitWindowChangedAsync 호출 전");
                bClosed = await ClickNWaitWindowChangedAsync(wnd.Btn_hWnd닫기, wnd.TopWnd_hWnd, ctrl);
                Debug.WriteLine($"[{m_Context.AppName}] ClickNWaitWindowChangedAsync 호출 후: bClosed={bClosed}");

                if (!bClosed)
                {
                    Debug.WriteLine($"[{m_Context.AppName}] 닫기 실패 (윈도우 안 닫힘)");
                    return false;
                }

                Debug.WriteLine($"[{m_Context.AppName}] 닫기 성공 - 팝업 닫힘");
            }

            await Task.Delay(CommonVars.c_nWaitNormal, ctrl.Token); // 확실히 대기
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{m_Context.AppName}] CloseEditPopupAsync 예외: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 기사 정보 읽기용 팝업 열기 (DG 더블클릭 → 3초 대기 → 닫기)
    /// - Phase 1: 팝업 열고 3초 대기 후 닫기만 (골격 구현)
    /// - Phase 2: 기사 정보 OFR 추가 예정
    /// </summary>
    /// <param name="rowIndex">DG 로우 인덱스 (0-based)</param>
    /// <param name="item">기사 정보를 저장할 AutoAllocModel (나중에 사용)</param>
    /// <param name="ctrl">CancelToken 컨트롤</param>
    /// <returns>StdResult_Status (성공/실패)</returns>
    public async Task<StdResult_Status> OpenReadPopupAsync(int rowIndex, AutoAllocModel item, CancelTokenControl ctrl)
    {
        try
        {
            await ctrl.WaitIfPausedOrCancelledAsync();

            // 1. DG 로우 더블클릭 (헤더 2줄 추가)
            int selIndex = rowIndex + 2;
            Draw.Point ptRel = StdUtil.GetCenterDrawPoint(m_RcptPage.DG오더_RelChildRects[c_nCol번호, selIndex]);

            Debug.WriteLine($"[{m_Context.AppName}] 기사정보 읽기용 팝업 열기 시도: rowIndex={rowIndex}, selIndex={selIndex}, ptRel={ptRel}");

            // 2. 팝업창 찾기 (최대 c_nRepeatNormal번 시도)
            bool bFind = false;
            IntPtr hWndPopup = IntPtr.Zero;

            for (int j = 0; j < CommonVars.c_nRepeatNormal; j++)
            {
                await ctrl.WaitIfPausedOrCancelledAsync();

                // 더블클릭 (SendMessage 방식, BlockInput 사용)
                await Simulation_Mouse.SafeMouseSend_DblClickLeft_ptRelAsync(m_RcptPage.DG오더_hWnd, ptRel);
                Debug.WriteLine($"[{m_Context.AppName}] 더블클릭 실행 (시도 {j + 1}/{CommonVars.c_nRepeatNormal})");

                // 팝업창 나타날 때까지 대기 (최대 5초)
                for (int k = 0; k < 100; k++)
                {
                    await Task.Delay(50, ctrl.Token);

                    // 팝업창 찾기
                    hWndPopup = Std32Window.FindStartWithMainWindow_NotTransparent(m_MemInfo.Splash.TopWnd_uProcessId, m_FileInfo.접수등록Wnd_TopWnd_sWndStartsWith);
                    if (hWndPopup == IntPtr.Zero) continue;

                    // 닫기 버튼으로 팝업 검증
                    IntPtr hWndClose = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_신규버튼그룹_ptChkRel닫기);
                    if (hWndClose == IntPtr.Zero) continue;

                    string closeBtnText = Std32Window.GetWindowCaption(hWndClose);
                    if (closeBtnText.StartsWith(m_FileInfo.접수등록Wnd_버튼그룹_sWndName닫기))
                    {
                        bFind = true;
                        Debug.WriteLine($"[{m_Context.AppName}] 팝업창 찾음: hWnd={hWndPopup:X}, 닫기버튼={closeBtnText}");
                        break;
                    }
                }

                if (bFind) break;
                await Task.Delay(100, ctrl.Token);
            }

            if (!bFind)
            {
                Debug.WriteLine($"[{m_Context.AppName}] 팝업창 찾기 실패");
                return new StdResult_Status(StdResult.Fail, "DG 더블클릭 후 팝업창이 열리지 않음", "OpenReadPopupAsync_01");
            }

            await Task.Delay(100, ctrl.Token);

            // 3. RcptWnd_Edit 생성
            var wndRcpt = new InsungsInfo_Mem.RcptWnd_Edit(hWndPopup, m_FileInfo);
            Debug.WriteLine($"[{m_Context.AppName}] RcptWnd_Edit 생성 완료");

            // 4. Phase 2: 기사 정보 OFR
            Debug.WriteLine($"[{m_Context.AppName}] 기사 정보 OFR 시작");

            // 4-1. 기사번호 OFR (단음소)
            Draw.Rectangle rectDriverId = m_FileInfo.접수등록Wnd_기사그룹_rcChkRel기사번호;
            Draw.Bitmap bmpDriverId = OfrService.CaptureScreenRect_InWndHandle(hWndPopup, rectDriverId);
            StdResult_String resultDriverId = await OfrWork_Common.OfrStr_SeqCharAsync(bmpDriverId);
            string driverId = resultDriverId.strResult ?? "";
            bmpDriverId?.Dispose();
            Debug.WriteLine($"[{m_Context.AppName}] 기사번호 OFR: '{driverId}'");

            // 4-2. 기사이름 OFR (다음소)
            Draw.Rectangle rectDriverName = m_FileInfo.접수등록Wnd_기사그룹_rcChkRel기사이름;
            Draw.Bitmap bmpDriverName = OfrService.CaptureScreenRect_InWndHandle(hWndPopup, rectDriverName);
            StdResult_String resultDriverName = await OfrWork_Common.OfrStr_ComplexCharSetAsync(bmpDriverName, bEdit: false);
            string driverName = resultDriverName.strResult ?? "";
            bmpDriverName?.Dispose();
            Debug.WriteLine($"[{m_Context.AppName}] 기사이름 OFR: '{driverName}'");

            // 4-3. 기사소속 OFR (다음소)
            Draw.Rectangle rectDriverCenter = m_FileInfo.접수등록Wnd_기사그룹_rcChkRel기사소속;
            Draw.Bitmap bmpDriverCenter = OfrService.CaptureScreenRect_InWndHandle(hWndPopup, rectDriverCenter);
            StdResult_String resultDriverCenter = await OfrWork_Common.OfrStr_ComplexCharSetAsync(bmpDriverCenter, bEdit: false);
            string driverCenter = resultDriverCenter.strResult ?? "";
            bmpDriverCenter?.Dispose();
            Debug.WriteLine($"[{m_Context.AppName}] 기사소속 OFR: '{driverCenter}'");

            // 4-4. item.NewOrder에 저장
            item.NewOrder.DriverId = driverId;
            item.NewOrder.DriverName = driverName;
            item.NewOrder.DriverCenterName = driverCenter;
            item.NewOrder.DriverTelNo = StdConvert.MakePhoneNumberToDigit(item.DriverPhone);
            item.NewOrder.OrderState = "운행";
            // item.DriverPhone은 이미 DG에서 읽어서 저장됨

            //Debug.WriteLine($"[{m_Context.AppName}] ===== 기사 정보 OFR 완료 =====");
            //Debug.WriteLine($"  기사번호: '{driverId}'");
            //Debug.WriteLine($"  기사이름: '{driverName}'");
            //Debug.WriteLine($"  기사소속: '{driverCenter}'");
            //Debug.WriteLine($"  기사전번: '{item.DriverPhone}' (DG에서 획득)");

            // 5. 팝업 닫기 (변경사항 없으므로 shouldSave: false)
            Debug.WriteLine($"[{m_Context.AppName}] 팝업 닫기 시작");
            bool bClosed = await CloseEditPopupAsync(wndRcpt, shouldSave: false, ctrl);

            if (!bClosed)
            {
                Debug.WriteLine($"[{m_Context.AppName}] 팝업 닫기 실패");
                return new StdResult_Status(StdResult.Fail, "팝업 닫기 실패", "OpenReadPopupAsync_CloseError");
            }

            Debug.WriteLine($"[{m_Context.AppName}] 기사정보 읽기용 팝업 처리 완료");
            return new StdResult_Status(StdResult.Success, "", "");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{m_Context.AppName}] OpenReadPopupAsync 예외: {ex.Message}");
            return new StdResult_Status(StdResult.Fail, StdUtil.GetExceptionMessage(ex), "OpenReadPopupAsync_999");
        }
    }

    #endregion


    #region 영역별 업데이트 함수

    /// <summary>
    /// 의뢰자 영역 업데이트 (고객명, 전화1, 전화2, 부서, 담당)
    /// </summary>
    private async Task<(int changeCount, StdResult_Error error)> Update의뢰자영역Async(InsungsInfo_Mem.RcptWnd_Edit wnd, TbOrder order, CancelTokenControl ctrl)
    {
        int changeCount = 0;
        StdResult_Status result = null;

        // 1. 고객명 OR 담당자명 확인 → 둘 중 하나라도 변경 시 검색
        string current고객명 = Std32Window.GetWindowCaption(wnd.의뢰자_hWnd고객명);
        string current담당 = Std32Window.GetWindowCaption(wnd.의뢰자_hWnd담당);
        bool did고객검색 = (order.CallCustName != current고객명) || (order.CallChargeName != current담당);

        if (did고객검색)
        {
            Debug.WriteLine($"[{m_Context.AppName}]   의뢰자 고객 검색 필요 (고객명 변경={order.CallCustName != current고객명}, 담당자명 변경={order.CallChargeName != current담당})");
            result = await SearchAndSelectCustomerAsync(wnd.의뢰자_hWnd고객명, wnd.의뢰자_hWnd동명, order.CallCustName, order.CallChargeName, "의뢰자", ctrl);
            if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));
            changeCount++;
            Debug.WriteLine($"[{m_Context.AppName}]   의뢰자 고객 검색 완료");
        }

        if (did고객검색)
        {
            // 2-1. 고객 검색한 경우: 관련 필드 무조건 Kai DB 데이터로 덮어쓰기
            Debug.WriteLine($"[{m_Context.AppName}]   의뢰자 관련 필드 무조건 업데이트 (Kai DB 데이터로 교체)");

            result = await WriteAndVerifyEditBoxAsync(wnd.의뢰자_hWnd전화1, order.CallTelNo ?? "", "의뢰자_전화1", ctrl);
            if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));

            result = await WriteAndVerifyEditBoxAsync(wnd.의뢰자_hWnd전화2, order.CallTelNo2 ?? "", "의뢰자_전화2", ctrl);
            if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));

            result = await WriteAndVerifyEditBoxAsync(wnd.의뢰자_hWnd부서, order.CallDeptName ?? "", "의뢰자_부서", ctrl);
            if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));

            result = await WriteAndVerifyEditBoxAsync(wnd.의뢰자_hWnd담당, order.CallChargeName ?? "", "의뢰자_담당", ctrl);
            if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));
        }
        else
        {
            // 2-2. 고객 검색 안 한 경우: 변경된 필드만 개별 업데이트
            string current전화1 = StdConvert.MakePhoneNumberToDigit(Std32Window.GetWindowCaption(wnd.의뢰자_hWnd전화1));
            string target전화1 = StdConvert.MakePhoneNumberToDigit(order.CallTelNo ?? "");
            if (target전화1 != current전화1)
            {
                Debug.WriteLine($"[{m_Context.AppName}]   의뢰자 전화1 업데이트");
                result = await WriteAndVerifyEditBoxAsync(wnd.의뢰자_hWnd전화1, order.CallTelNo ?? "", "의뢰자_전화1", ctrl);
                if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));
                changeCount++;
            }

            string current전화2 = StdConvert.MakePhoneNumberToDigit(Std32Window.GetWindowCaption(wnd.의뢰자_hWnd전화2));
            string target전화2 = StdConvert.MakePhoneNumberToDigit(order.CallTelNo2 ?? "");
            if (target전화2 != current전화2)
            {
                Debug.WriteLine($"[{m_Context.AppName}]   의뢰자 전화2 업데이트");
                result = await WriteAndVerifyEditBoxAsync(wnd.의뢰자_hWnd전화2, order.CallTelNo2 ?? "", "의뢰자_전화2", ctrl);
                if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));
                changeCount++;
            }

            string current부서 = Std32Window.GetWindowCaption(wnd.의뢰자_hWnd부서);
            if ((order.CallDeptName ?? "") != current부서)
            {
                Debug.WriteLine($"[{m_Context.AppName}]   의뢰자 부서 업데이트");
                result = await WriteAndVerifyEditBoxAsync(wnd.의뢰자_hWnd부서, order.CallDeptName ?? "", "의뢰자_부서", ctrl);
                if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));
                changeCount++;
            }

            current담당 = Std32Window.GetWindowCaption(wnd.의뢰자_hWnd담당);
            if ((order.CallChargeName ?? "") != current담당)
            {
                Debug.WriteLine($"[{m_Context.AppName}]   의뢰자 담당 업데이트");
                result = await WriteAndVerifyEditBoxAsync(wnd.의뢰자_hWnd담당, order.CallChargeName ?? "", "의뢰자_담당", ctrl);
                if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));
                changeCount++;
            }
        }

        Debug.WriteLine($"[{m_Context.AppName}]   의뢰자 영역 업데이트 완료 (변경: {changeCount}개)");
        return (changeCount, null);
    }

    /// <summary>
    /// 출발지 영역 업데이트 (고객명, 동명, 전화1, 전화2, 부서, 담당, 위치)
    /// </summary>
    private async Task<(int changeCount, StdResult_Error error)> Update출발지영역Async(InsungsInfo_Mem.RcptWnd_Edit wnd, TbOrder order, CancelTokenControl ctrl)
    {
        int changeCount = 0;
        StdResult_Status result = null;

        // 1. 출발지 = 의뢰자 여부 확인
        bool is출발지Eq의뢰자 = (order.StartCustCodeK == order.CallCustCodeK);
        bool did고객검색 = false;

        if (is출발지Eq의뢰자)
        {
            // 출발지 = 의뢰자: 고객명/담당자 검색 Skip (의뢰자와 연동되므로 건드리지 않음)
            Debug.WriteLine($"[{m_Context.AppName}]   출발지 = 의뢰자: 고객 검색 Skip");
        }
        else
        {
            // 출발지 ≠ 의뢰자: 고객명 OR 담당자명 변경 확인 → 검색
            string current고객명 = Std32Window.GetWindowCaption(wnd.출발지_hWnd고객명);
            string current담당 = Std32Window.GetWindowCaption(wnd.출발지_hWnd담당);
            did고객검색 = (order.StartCustName != current고객명) || (order.StartChargeName != current담당);

            if (did고객검색)
            {
                Debug.WriteLine($"[{m_Context.AppName}]   출발지 고객 검색 필요 (고객명 변경={order.StartCustName != current고객명}, 담당자명 변경={order.StartChargeName != current담당})");
                result = await SearchAndSelectCustomerAsync(wnd.출발지_hWnd고객명, wnd.출발지_hWnd동명, order.StartCustName, order.StartChargeName, "출발지", ctrl);
                if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));
                changeCount++;
                Debug.WriteLine($"[{m_Context.AppName}]   출발지 고객 검색 완료");
            }
        }

        // 2. 필드 업데이트
        if (did고객검색)
        {
            // 2-1. 고객 검색한 경우: 관련 필드 무조건 Kai DB 데이터로 덮어쓰기
            Debug.WriteLine($"[{m_Context.AppName}]   출발지 관련 필드 무조건 업데이트 (Kai DB 데이터로 교체)");

            result = await WriteAndVerifyEditBoxAsync(wnd.출발지_hWnd동명, order.StartDongBasic ?? "", "출발지_동명", ctrl);
            if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));

            result = await WriteAndVerifyEditBoxAsync(wnd.출발지_hWnd전화1, order.StartTelNo ?? "", "출발지_전화1", ctrl);
            if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));

            result = await WriteAndVerifyEditBoxAsync(wnd.출발지_hWnd전화2, order.StartTelNo2 ?? "", "출발지_전화2", ctrl);
            if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));

            result = await WriteAndVerifyEditBoxAsync(wnd.출발지_hWnd부서, order.StartDeptName ?? "", "출발지_부서", ctrl);
            if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));

            result = await WriteAndVerifyEditBoxAsync(wnd.출발지_hWnd담당, order.StartChargeName ?? "", "출발지_담당", ctrl);
            if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));

            result = await WriteAndVerifyEditBoxAsync(wnd.출발지_hWnd위치, order.StartDetailAddr ?? "", "출발지_위치", ctrl);
            if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));
        }
        else
        {
            // 2-2. 고객 검색 안 한 경우: 변경된 필드만 개별 업데이트
            string current동명 = Std32Window.GetWindowCaption(wnd.출발지_hWnd동명);
            if ((order.StartDongBasic ?? "") != current동명)
            {
                Debug.WriteLine($"[{m_Context.AppName}]   출발지 동명 업데이트");
                result = await WriteAndVerifyEditBoxAsync(wnd.출발지_hWnd동명, order.StartDongBasic ?? "", "출발지_동명", ctrl);
                if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));
                changeCount++;
            }

            string current전화1 = StdConvert.MakePhoneNumberToDigit(Std32Window.GetWindowCaption(wnd.출발지_hWnd전화1));
            string target전화1 = StdConvert.MakePhoneNumberToDigit(order.StartTelNo ?? "");
            if (target전화1 != current전화1)
            {
                Debug.WriteLine($"[{m_Context.AppName}]   출발지 전화1 업데이트");
                result = await WriteAndVerifyEditBoxAsync(wnd.출발지_hWnd전화1, order.StartTelNo ?? "", "출발지_전화1", ctrl);
                if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));
                changeCount++;
            }

            string current전화2 = StdConvert.MakePhoneNumberToDigit(Std32Window.GetWindowCaption(wnd.출발지_hWnd전화2));
            string target전화2 = StdConvert.MakePhoneNumberToDigit(order.StartTelNo2 ?? "");
            if (target전화2 != current전화2)
            {
                Debug.WriteLine($"[{m_Context.AppName}]   출발지 전화2 업데이트");
                result = await WriteAndVerifyEditBoxAsync(wnd.출발지_hWnd전화2, order.StartTelNo2 ?? "", "출발지_전화2", ctrl);
                if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));
                changeCount++;
            }

            string current부서 = Std32Window.GetWindowCaption(wnd.출발지_hWnd부서);
            if ((order.StartDeptName ?? "") != current부서)
            {
                Debug.WriteLine($"[{m_Context.AppName}]   출발지 부서 업데이트");
                result = await WriteAndVerifyEditBoxAsync(wnd.출발지_hWnd부서, order.StartDeptName ?? "", "출발지_부서", ctrl);
                if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));
                changeCount++;
            }

            string current담당 = Std32Window.GetWindowCaption(wnd.출발지_hWnd담당);
            if ((order.StartChargeName ?? "") != current담당)
            {
                Debug.WriteLine($"[{m_Context.AppName}]   출발지 담당 업데이트");
                result = await WriteAndVerifyEditBoxAsync(wnd.출발지_hWnd담당, order.StartChargeName ?? "", "출발지_담당", ctrl);
                if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));
                changeCount++;
            }

            string current위치 = Std32Window.GetWindowCaption(wnd.출발지_hWnd위치);
            if ((order.StartDetailAddr ?? "") != current위치)
            {
                Debug.WriteLine($"[{m_Context.AppName}]   출발지 위치 업데이트");
                result = await WriteAndVerifyEditBoxAsync(wnd.출발지_hWnd위치, order.StartDetailAddr ?? "", "출발지_위치", ctrl);
                if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));
                changeCount++;
            }
        }

        Debug.WriteLine($"[{m_Context.AppName}]   출발지 영역 업데이트 완료 (변경: {changeCount}개)");
        return (changeCount, null);
    }

    /// <summary>
    /// 도착지 영역 업데이트 (고객명, 동명, 전화1, 전화2, 부서, 담당, 위치)
    /// </summary>
    private async Task<(int changeCount, StdResult_Error error)> Update도착지영역Async(InsungsInfo_Mem.RcptWnd_Edit wnd, TbOrder order, CancelTokenControl ctrl)
    {
        int changeCount = 0;
        StdResult_Status result = null;

        // 1. 도착지 = 의뢰자 여부 확인
        bool is도착지Eq의뢰자 = (order.DestCustCodeK == order.CallCustCodeK);
        bool did고객검색 = false;

        if (is도착지Eq의뢰자)
        {
            // 도착지 = 의뢰자: 고객명/담당자 검색 Skip (의뢰자와 연동되므로 건드리지 않음)
            Debug.WriteLine($"[{m_Context.AppName}]   도착지 = 의뢰자: 고객 검색 Skip");
        }
        else
        {
            // 도착지 ≠ 의뢰자: 고객명 OR 담당자명 변경 확인 → 검색
            string current고객명 = Std32Window.GetWindowCaption(wnd.도착지_hWnd고객명);
            string current담당 = Std32Window.GetWindowCaption(wnd.도착지_hWnd담당);
            did고객검색 = (order.DestCustName != current고객명) || (order.DestChargeName != current담당);

            if (did고객검색)
            {
                Debug.WriteLine($"[{m_Context.AppName}]   도착지 고객 검색 필요 (고객명 변경={order.DestCustName != current고객명}, 담당자명 변경={order.DestChargeName != current담당})");
                result = await SearchAndSelectCustomerAsync(wnd.도착지_hWnd고객명, wnd.도착지_hWnd동명, order.DestCustName, order.DestChargeName, "도착지", ctrl);
                if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));
                changeCount++;
                Debug.WriteLine($"[{m_Context.AppName}]   도착지 고객 검색 완료");
            }
        }

        // 2. 필드 업데이트
        if (did고객검색)
        {
            // 2-1. 고객 검색한 경우: 관련 필드 무조건 Kai DB 데이터로 덮어쓰기
            Debug.WriteLine($"[{m_Context.AppName}]   도착지 관련 필드 무조건 업데이트 (Kai DB 데이터로 교체)");

            result = await WriteAndVerifyEditBoxAsync(wnd.도착지_hWnd동명, order.DestDongBasic ?? "", "도착지_동명", ctrl);
            if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));

            result = await WriteAndVerifyEditBoxAsync(wnd.도착지_hWnd전화1, order.DestTelNo ?? "", "도착지_전화1", ctrl);
            if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));

            result = await WriteAndVerifyEditBoxAsync(wnd.도착지_hWnd전화2, order.DestTelNo2 ?? "", "도착지_전화2", ctrl);
            if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));

            result = await WriteAndVerifyEditBoxAsync(wnd.도착지_hWnd부서, order.DestDeptName ?? "", "도착지_부서", ctrl);
            if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));

            result = await WriteAndVerifyEditBoxAsync(wnd.도착지_hWnd담당, order.DestChargeName ?? "", "도착지_담당", ctrl);
            if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));

            result = await WriteAndVerifyEditBoxAsync(wnd.도착지_hWnd위치, order.DestDetailAddr ?? "", "도착지_위치", ctrl);
            if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));
        }
        else
        {
            // 2-2. 고객 검색 안 한 경우: 변경된 필드만 개별 업데이트
            string current동명 = Std32Window.GetWindowCaption(wnd.도착지_hWnd동명);
            if ((order.DestDongBasic ?? "") != current동명)
            {
                Debug.WriteLine($"[{m_Context.AppName}]   도착지 동명 업데이트");
                result = await WriteAndVerifyEditBoxAsync(wnd.도착지_hWnd동명, order.DestDongBasic ?? "", "도착지_동명", ctrl);
                if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));
                changeCount++;
            }

            string current전화1 = StdConvert.MakePhoneNumberToDigit(Std32Window.GetWindowCaption(wnd.도착지_hWnd전화1));
            string target전화1 = StdConvert.MakePhoneNumberToDigit(order.DestTelNo ?? "");
            if (target전화1 != current전화1)
            {
                Debug.WriteLine($"[{m_Context.AppName}]   도착지 전화1 업데이트");
                result = await WriteAndVerifyEditBoxAsync(wnd.도착지_hWnd전화1, order.DestTelNo ?? "", "도착지_전화1", ctrl);
                if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));
                changeCount++;
            }

            string current전화2 = StdConvert.MakePhoneNumberToDigit(Std32Window.GetWindowCaption(wnd.도착지_hWnd전화2));
            string target전화2 = StdConvert.MakePhoneNumberToDigit(order.DestTelNo2 ?? "");
            if (target전화2 != current전화2)
            {
                Debug.WriteLine($"[{m_Context.AppName}]   도착지 전화2 업데이트");
                result = await WriteAndVerifyEditBoxAsync(wnd.도착지_hWnd전화2, order.DestTelNo2 ?? "", "도착지_전화2", ctrl);
                if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));
                changeCount++;
            }

            string current부서 = Std32Window.GetWindowCaption(wnd.도착지_hWnd부서);
            if ((order.DestDeptName ?? "") != current부서)
            {
                Debug.WriteLine($"[{m_Context.AppName}]   도착지 부서 업데이트");
                result = await WriteAndVerifyEditBoxAsync(wnd.도착지_hWnd부서, order.DestDeptName ?? "", "도착지_부서", ctrl);
                if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));
                changeCount++;
            }

            string current담당 = Std32Window.GetWindowCaption(wnd.도착지_hWnd담당);
            if ((order.DestChargeName ?? "") != current담당)
            {
                Debug.WriteLine($"[{m_Context.AppName}]   도착지 담당 업데이트");
                result = await WriteAndVerifyEditBoxAsync(wnd.도착지_hWnd담당, order.DestChargeName ?? "", "도착지_담당", ctrl);
                if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));
                changeCount++;
            }

            string current위치 = Std32Window.GetWindowCaption(wnd.도착지_hWnd위치);
            if ((order.DestDetailAddr ?? "") != current위치)
            {
                Debug.WriteLine($"[{m_Context.AppName}]   도착지 위치 업데이트");
                result = await WriteAndVerifyEditBoxAsync(wnd.도착지_hWnd위치, order.DestDetailAddr ?? "", "도착지_위치", ctrl);
                if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));
                changeCount++;
            }
        }

        Debug.WriteLine($"[{m_Context.AppName}]   도착지 영역 업데이트 완료 (변경: {changeCount}개)");
        return (changeCount, null);
    }

    /// <summary>
    /// 요금 영역 업데이트 (기본요금, 추가금액, 할인금액, 탁송료)
    /// </summary>
    private async Task<(int changeCount, StdResult_Error error)> Update요금영역Async(InsungsInfo_Mem.RcptWnd_Edit wnd, TbOrder order, CancelTokenControl ctrl)
    {
        int changeCount = 0;
        StdResult_Status result = null;

        // 1. 기본요금 확인 및 업데이트
        int current기본요금 = StdConvert.StringWonFormatToInt(Std32Window.GetWindowCaption(wnd.요금그룹_hWnd기본요금));
        if (order.FeeBasic != current기본요금)
        {
            result = await ResgistAndVerify요금Async(wnd.요금그룹_hWnd기본요금, order.FeeBasic, "기본요금", ctrl);
            if (result.Result != StdResult.Success)
                return (changeCount, new StdResult_Error(result.sErr, result.sPos));
            changeCount++;
        }

        // 2. 추가금액 확인 및 업데이트
        int current추가금액 = StdConvert.StringWonFormatToInt(Std32Window.GetWindowCaption(wnd.요금그룹_hWnd추가금액));
        if (order.FeePlus != current추가금액)
        {
            result = await ResgistAndVerify요금Async(wnd.요금그룹_hWnd추가금액, order.FeePlus, "추가금액", ctrl);
            if (result.Result != StdResult.Success)
                return (changeCount, new StdResult_Error(result.sErr, result.sPos));
            changeCount++;
        }

        // 3. 할인금액 확인 및 업데이트
        int current할인금액 = StdConvert.StringWonFormatToInt(Std32Window.GetWindowCaption(wnd.요금그룹_hWnd할인금액));
        if (order.FeeMinus != current할인금액)
        {
            result = await ResgistAndVerify요금Async(wnd.요금그룹_hWnd할인금액, order.FeeMinus, "할인금액", ctrl);
            if (result.Result != StdResult.Success)
                return (changeCount, new StdResult_Error(result.sErr, result.sPos));
            changeCount++;
        }

        // 4. 탁송료 확인 및 업데이트
        int current탁송료 = StdConvert.StringWonFormatToInt(Std32Window.GetWindowCaption(wnd.요금그룹_hWnd탁송료));
        if (order.FeeConn != current탁송료)
        {
            result = await ResgistAndVerify요금Async(wnd.요금그룹_hWnd탁송료, order.FeeConn, "탁송료", ctrl);
            if (result.Result != StdResult.Success)
                return (changeCount, new StdResult_Error(result.sErr, result.sPos));
            changeCount++;
        }

        Debug.WriteLine($"[{m_Context.AppName}]   요금 영역 업데이트 완료 (변경: {changeCount}개)");
        return (changeCount, null);
    }

    /// <summary>
    /// 우측상단 섹션 업데이트 (적요, 공유, 요금종류, 차량종류, 배송타입, 계산서)
    /// </summary>
    private async Task<(int changeCount, StdResult_Error error)> Update우측상단영역Async(InsungsInfo_Mem.RcptWnd_Edit wnd, TbOrder order, CancelTokenControl ctrl)
    {
        int changeCount = 0;
        StdResult_Status result = null;
        Draw.Bitmap bmpWnd = null;

        try
        {
            // 화면 캡처 (OFR 사용 위해 전체창 1회 캡처)
            bmpWnd = OfrService.CaptureScreenRect_InWndHandle(wnd.TopWnd_hWnd, 0);
            if (bmpWnd == null)
                return (0, new StdResult_Error("화면 캡처 실패", "Update우측상단영역Async_00"));

            // 1. 적요 (EditBox)
            string current적요 = Std32Window.GetWindowCaption(wnd.우측상단_hWnd적요);
            if ((order.OrderRemarks ?? "") != current적요)
            {
                result = await WriteAndVerifyEditBoxAsync(wnd.우측상단_hWnd적요, order.OrderRemarks ?? "", "적요", ctrl);
                if (result.Result != StdResult.Success)
                    return (changeCount, new StdResult_Error(result.sErr, result.sPos));
                changeCount++;
            }

            // 2. 공유 (CheckBox - OFR)
            StdResult_NulBool resultShare = await OfrWork_Insungs.OfrImgChkValue_RectInBitmapAsync(
                bmpWnd, m_Context.FileInfo.접수등록Wnd_우측상단_rcChkRel공유);

            if (resultShare.bResult == null)
                return (changeCount, new StdResult_Error("공유 CheckBox 인식 실패", "Update우측상단영역Async_01"));

            bool current공유 = StdConvert.NullableBoolToBool(resultShare.bResult);
            if (order.Share != current공유)
            {
                StdResult_Error resultError = await OfrWork_Common.SetCheckBox_StatusAsync(
                    wnd.TopWnd_hWnd, m_Context.FileInfo.접수등록Wnd_우측상단_rcChkRel공유, order.Share, "공유");

                if (resultError != null)
                    return (changeCount, new StdResult_Error($"공유 CheckBox 변경 실패: {resultError.sErr}", resultError.sPos));
                changeCount++;
            }

            // 3. 요금종류 (RadioButton - OFR)
            StdResult_NulBool resultFeeType = await IsChecked요금종류Async(bmpWnd, wnd.우측상단_btns요금종류, order.FeeType);

            if (resultFeeType.bResult == null)
                return (changeCount, new StdResult_Error("요금종류 RadioButton 인식 실패", "Update우측상단영역Async_02"));

            if (!StdConvert.NullableBoolToBool(resultFeeType.bResult))
            {
                result = await SetGroupFeeTypeAsync(bmpWnd, wnd.우측상단_btns요금종류, order.FeeType, ctrl);
                if (result.Result != StdResult.Success)
                    return (changeCount, new StdResult_Error($"요금종류 설정 실패: {order.FeeType}", result.sPos));
                changeCount++;
            }

            // 4. 차량종류 (RadioButton - OFR, 트럭은 특별 처리)
            if (order.CarType == "트럭")
            {
                // 트럭: RadioButton + ComboBox 처리
                result = await SetGroupCarTypeAsync_트럭(bmpWnd, wnd.우측상단_btns차량종류, order, ctrl);
                if (result.Result != StdResult.Success)
                    return (changeCount, new StdResult_Error($"트럭 설정 실패", result.sPos));
                changeCount++;
            }
            else
            {
                // 일반 차량
                StdResult_NulBool resultCarType = await IsChecked차량종류Async(bmpWnd, wnd.우측상단_btns차량종류, order.CarType);

                if (resultCarType.bResult == null)
                    return (changeCount, new StdResult_Error("차량종류 RadioButton 인식 실패", "Update우측상단영역Async_03"));

                if (!StdConvert.NullableBoolToBool(resultCarType.bResult))
                {
                    int index = GetCarTypeIndex(order.CarType);
                    if (index == -1)
                        return (changeCount, new StdResult_Error($"지원하지 않는 차량종류: {order.CarType}", "Update우측상단영역Async_03_1"));

                    result = await SetCheckRadioBtn_InGroupAsync(bmpWnd, wnd.우측상단_btns차량종류, index, ctrl);
                    if (result.Result != StdResult.Success)
                        return (changeCount, new StdResult_Error($"차량종류 설정 실패: {order.CarType}", result.sPos));
                    changeCount++;
                }
            }

            // 5. 배송타입 (RadioButton - OFR)
            StdResult_NulBool resultDeliverType = await IsChecked배송타입Async(bmpWnd, wnd.우측상단_btns배송종류, order.DeliverType);

            if (resultDeliverType.bResult == null)
                return (changeCount, new StdResult_Error("배송타입 RadioButton 인식 실패", "Update우측상단영역Async_04"));

            if (!StdConvert.NullableBoolToBool(resultDeliverType.bResult))
            {
                result = await SetGroupDeliverTypeAsync(bmpWnd, wnd.우측상단_btns배송종류, order.DeliverType, ctrl);
                if (result.Result != StdResult.Success)
                    return (changeCount, new StdResult_Error($"배송타입 설정 실패: {order.DeliverType}", result.sPos));
                changeCount++;
            }

            // 6. 계산서 (CheckBox - OFR)
            StdResult_NulBool resultTaxBill = await OfrWork_Insungs.OfrImgChkValue_RectInBitmapAsync(
                bmpWnd, m_Context.FileInfo.접수등록Wnd_우측상단_rcChkRel계산서);

            if (resultTaxBill.bResult == null)
                return (changeCount, new StdResult_Error("계산서 CheckBox 인식 실패", "Update우측상단영역Async_05"));

            bool current계산서 = StdConvert.NullableBoolToBool(resultTaxBill.bResult);
            if (order.TaxBill != current계산서)
            {
                StdResult_Error resultError = await OfrWork_Common.SetCheckBox_StatusAsync(
                    wnd.TopWnd_hWnd, m_Context.FileInfo.접수등록Wnd_우측상단_rcChkRel계산서, order.TaxBill, "계산서");

                if (resultError != null)
                    return (changeCount, new StdResult_Error($"계산서 CheckBox 변경 실패: {resultError.sErr}", resultError.sPos));
                changeCount++;
            }

            Debug.WriteLine($"[{m_Context.AppName}]   우측상단 섹션 업데이트 완료 (변경: {changeCount}개)");
            return (changeCount, null);
        }
        finally
        {
            bmpWnd?.Dispose(); // 메모리 해제
        }
    }

    /// <summary>
    /// 오더메모 영역 업데이트 (KeyCode/OrderMemo 형식)
    /// </summary>
    private async Task<(int changeCount, StdResult_Error error)> Update오더메모영역Async(InsungsInfo_Mem.RcptWnd_Edit wnd, TbOrder order, CancelTokenControl ctrl)
    {
        int changeCount = 0;
        StdResult_Status result = null;

        // 오더메모 형식: "{KeyCode}/{OrderMemo}"
        string target오더메모 = $"{order.KeyCode}/{order.OrderMemo}";
        string current오더메모 = Std32Window.GetWindowCaption(wnd.우측하단_hWnd오더메모);

        if (target오더메모 != current오더메모)
        {
            result = await WriteAndVerifyEditBoxAsync(wnd.우측하단_hWnd오더메모, target오더메모, "오더메모", ctrl);
            if (result.Result != StdResult.Success)
                return (changeCount, new StdResult_Error(result.sErr, result.sPos));
            changeCount++;
        }

        Debug.WriteLine($"[{m_Context.AppName}]   오더메모 영역 업데이트 완료 (변경: {changeCount}개)");
        return (changeCount, null);
    }

    #endregion

    #endregion
}
#nullable enable
