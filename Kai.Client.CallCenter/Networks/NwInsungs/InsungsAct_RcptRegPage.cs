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

using Kai.Client.CallCenter.Classes;
using Kai.Client.CallCenter.Classes.Class_Master;
using Kai.Client.CallCenter.OfrWorks;
using Kai.Client.CallCenter.Windows;
using static Kai.Client.CallCenter.Classes.CommonVars;

namespace Kai.Client.CallCenter.Networks.NwInsungs;
#nullable disable

/// <summary>
/// Datagrid 검증 이슈 플래그 (비트 조합 가능)
/// </summary>
[Flags]
public enum DgValidationIssue
{
    None = 0,               // 문제 없음
    InvalidColumnCount = 1, // 컬럼 개수 틀림
    InvalidColumn = 2,      // 필요 없는 컬럼 존재
    WrongOrder = 4,         // 컬럼 순서 틀림
    WrongWidth = 8          // 컬럼 너비 틀림 (허용 오차 초과)
}

/// <summary>
/// 인성 앱 접수등록 페이지 초기화 및 제어 담당 클래스
/// Context 패턴 사용: InsungContext를 통해 모든 정보에 접근
/// </summary>
public class InsungsAct_RcptRegPage
{
    #region Region 4 Helper Classes
    /// <summary>
    /// 고객 검색 결과 타입
    /// </summary>
    private enum AutoAlloc_CustSearch
    {
        Null = 0,   // 검색 실패
        None = 1,   // 검색 결과 없음 (신규 고객)
        One = 2,    // 검색 결과 1개 (정상)
        Multi = 3   // 검색 결과 복수 (수동 처리 필요)
    }

    /// <summary>
    /// 고객 검색 타입 결과
    /// </summary>
    private class AutoAlloc_SearchTypeResult
    {
        public AutoAlloc_CustSearch resultTye { get; }
        public IntPtr hWndResult { get; }

        public AutoAlloc_SearchTypeResult(AutoAlloc_CustSearch resultTye, IntPtr hWndResult)
        {
            this.resultTye = resultTye;
            this.hWndResult = hWndResult;
        }
    }
    #endregion

    #region Variables
    /// <summary>
    /// 컬럼 너비 허용 오차 (픽셀)
    /// </summary>
    private const int COLUMN_WIDTH_TOLERANCE = 2;

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
    public const int c_nCol오더메모 = 18;

    /// <summary>
    /// 접수등록 Datagrid 컬럼 헤더 정보 (20개 컬럼)
    /// </summary>
    public readonly NwCommon_DgColumnHeader[] m_ReceiptDgHeaderInfos = new NwCommon_DgColumnHeader[]
    {
        new NwCommon_DgColumnHeader() { sName = "No", bOfrSeq = false, bSaveObject = true, nWidth = 60 },
        new NwCommon_DgColumnHeader() { sName = "상태", bOfrSeq = false, bSaveObject = true,  nWidth = 70 },

        new NwCommon_DgColumnHeader() { sName = "주문번호", bOfrSeq = true, bSaveObject = true, nWidth = 80  },
        new NwCommon_DgColumnHeader() { sName = "최초접수시간", bOfrSeq = true, bSaveObject = true, nWidth = 90 },
        new NwCommon_DgColumnHeader() { sName = "접수시간", bOfrSeq = true, bSaveObject = true, nWidth = 90  },

        new NwCommon_DgColumnHeader() { sName = "고객명", bOfrSeq = false, bSaveObject = true, nWidth = 100 },
        new NwCommon_DgColumnHeader() { sName = "담당자", bOfrSeq = false, bSaveObject = true, nWidth = 100 },

        new NwCommon_DgColumnHeader() { sName = "전화번호", bOfrSeq = true, bSaveObject = true, nWidth = 100 },

        new NwCommon_DgColumnHeader() { sName = "출발동", bOfrSeq = false, bSaveObject = true, nWidth = 120 },
        new NwCommon_DgColumnHeader() { sName = "도착동", bOfrSeq = false, bSaveObject = true, nWidth = 120 },

        new NwCommon_DgColumnHeader() { sName = "요금", bOfrSeq = true, bSaveObject = true, nWidth = 62 },

        new NwCommon_DgColumnHeader() { sName = "지급", bOfrSeq = false, bSaveObject = true, nWidth = 40 },
        new NwCommon_DgColumnHeader() { sName = "형태", bOfrSeq = false, bSaveObject = true, nWidth = 40 },
        new NwCommon_DgColumnHeader() { sName = "차량", bOfrSeq = false, bSaveObject = true, nWidth = 60 },
        new NwCommon_DgColumnHeader() { sName = "계산서", bOfrSeq = false, bSaveObject = true, nWidth = 50 },
        new NwCommon_DgColumnHeader() { sName = "왕복", bOfrSeq = false, bSaveObject = true, nWidth = 40 },
        new NwCommon_DgColumnHeader() { sName = "공유", bOfrSeq = false, bSaveObject = true, nWidth = 40 },
        new NwCommon_DgColumnHeader() { sName = "라이더", bOfrSeq = false, bSaveObject = true, nWidth = 120 },
        new NwCommon_DgColumnHeader() { sName = "오더메모", bOfrSeq = false, bSaveObject = true, nWidth = 100 },
        new NwCommon_DgColumnHeader() { sName = "적요", bOfrSeq = false, bSaveObject = true, nWidth = 340 },
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

                // 컬럼 개수가 20개가 아니면 즉시 InitDG오더Async 호출하여 강제 초기화
                if (columns != 20)
                {
                    Debug.WriteLine($"[InsungsAct_RcptRegPage] 컬럼 개수 불일치: 검출={columns}개, 예상=20개 (재시도 {retry}/{c_nRepeatShort})");

                    bmpDG?.Dispose();

                    // InitDG오더Async 호출하여 Datagrid 강제 초기화
                    StdResult_Error initResult = await InitDG오더Async(
                        DgValidationIssue.InvalidColumnCount,
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
                                $"[{m_Context.AppName}/RcptRegPage]컬럼 개수 불일치: 검출={columns}개, 예상=20개\n상세: {initResult.sErr}\n(재시도 {c_nRepeatShort}회 초과)",
                                "InsungsAct_RcptRegPage/SetDG오더RectsAsync_05", bWrite, bShowMsgBox);
                        }
                    }

                    await Task.Delay(200);
                    continue; // 재시도
                }

                // 3. 컬럼 헤더 OFR 인식 (전체 20개 컬럼)
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
                        Draw.Rectangle rcSpare = new Draw.Rectangle(0, 0, bmpExact.Width, bmpExact.Height);

                        OfrResult_TbCharSetList resultCharSet = await OfrWork_Common.OfrStr_ComplexCharSetAsync(
                            bmpExact,
                            rcSpare,
                            bSaveToTbText: false,  // 컬럼 헤더는 앱마다 달라 저장 안 함
                            bEdit,
                            bWrite,
                            bMsgBox: false         // 에러 메시지 표시 안 함
                        );

                        bmpExact?.Dispose();

                        // 3-7. 결과 저장
                        if (resultCharSet != null && !string.IsNullOrEmpty(resultCharSet.strResult))
                        {
                            m_RcptPage.DG오더_ColumnTexts[i] = resultCharSet.strResult;
                            //Debug.WriteLine($"[InsungsAct_RcptRegPage] 컬럼[{i}] OFR 성공: '{m_RcptPage.DG오더_ColumnTexts[i]}'");
                        }
                        else
                        {
                            m_RcptPage.DG오더_ColumnTexts[i] = null;
                            //Debug.WriteLine($"[InsungsAct_RcptRegPage] 컬럼[{i}] OFR 실패: {resultCharSet?.sErr}");
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
                DgValidationIssue validationIssues = ValidateDatagridState(m_RcptPage.DG오더_ColumnTexts, listLW);

                if (validationIssues != DgValidationIssue.None)
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
                for (int i = 0; i < m_FileInfo.접수등록Page_DG오더_dataRowCount; i++)
                {
                    listTH.Add(new Kai.Common.NetDll_WpfCtrl.NetOFR.OfrModel_TopHeight(
                        curRowTop + 1,
                        dataRowHeight
                    ));
                    curRowTop += m_FileInfo.접수등록Page_DG오더_dataRowHeight;
                }
                Debug.WriteLine($"[InsungsAct_RcptRegPage] Data Rows 추가 완료: 총 {m_FileInfo.접수등록Page_DG오더_dataRowCount}개");

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
    /// <param name="bWrite">로그 작성 여부</param>
    /// <param name="bMsgBox">메시지박스 표시 여부</param>
    /// <returns>에러 발생 시 StdResult_Error, 성공 시 null</returns>
    private async Task<StdResult_Error> InitDG오더Async(DgValidationIssue issues, bool bEdit = true, bool bWrite = true, bool bMsgBox = true)
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

            for (int iteration = 0; iteration < 15; iteration++)
            {
                //Debug.WriteLine($"[InitDG오더] 2-{iteration+1}. 불필요 컬럼 검사 (반복 {iteration+1}/15)");

                // 2-1. 헤더 캡처
                Draw.Bitmap bmpHeader = OfrService.CaptureScreenRect_InWndHandle(
                    m_RcptPage.DG오더_hWnd, rcHeader);
                if (bmpHeader == null)
                {
                    return CommonFuncs_StdResult.ErrMsgResult_Error(
                        $"[InitDG오더]헤더 캡처 실패 (반복 {iteration + 1})",
                        "InsungsAct_RcptRegPage/InitDG오더Async_05", bWrite, true);
                }

                // 2-2. 컬럼 경계 검출
                byte byteMinBrightness = OfrService.GetMinBrightnessAtRow_FromColorBitmapFast(
                    bmpHeader, gab);
                byteMinBrightness += 2; // 확실한 경계를 위해 +2

                bool[] resultArr = OfrService.GetBoolArray_FromColorBitmapRowFast(
                    bmpHeader, gab, byteMinBrightness, 2);
                List<OfrModel_LeftWidth> listLW = OfrService.GetLeftWidthList_FromBool1Array(
                    resultArr, byteMinBrightness);
                int columns = listLW.Count - 1; // 마지막 컬럼은 잘리므로 제외

                //Debug.WriteLine($"[InitDG오더] 2-{iteration+1}. 검출된 컬럼 개수: {columns}");

                // 2-3. 각 컬럼 텍스트 인식 (SetDG오더RectsAsync 방식 사용)
                string[] texts = new string[columns];
                Draw.Rectangle[] rcHeaders = new Draw.Rectangle[columns];

                for (int x = 0; x < columns; x++)
                {
                    // 컬럼 헤더 영역 (상하 여백 제외)
                    rcHeaders[x] = new Draw.Rectangle(
                        listLW[x].nLeft, gab, listLW[x].nWidth, height);
                    Draw.Rectangle rcColHeader = rcHeaders[x];

                    // 비트맵 추출
                    Draw.Bitmap bmpColHeader = OfrService.GetBitmapInBitmapFast(
                        bmpHeader, rcColHeader);

                    if (bmpColHeader == null)
                    {
                        texts[x] = null;
                        continue;
                    }

                    // 평균 밝기
                    byte avgBrightness = OfrService.GetAverageBrightness_FromColorBitmapFast(
                        bmpColHeader);

                    // 전경 영역 추출
                    Draw.Rectangle? rcForeground = OfrService.GetForeGroundDrawRectangle_FromColorBitmapFast(
                        bmpColHeader, avgBrightness, 0);

                    if (rcForeground == null || rcForeground.Value.Width < 1)
                    {
                        bmpColHeader?.Dispose();
                        texts[x] = null;
                        continue;
                    }

                    // Exact 비트맵 추출
                    Draw.Bitmap bmpExact = OfrService.GetBitmapInBitmapFast(
                        bmpColHeader, rcForeground.Value);
                    bmpColHeader?.Dispose();

                    if (bmpExact == null)
                    {
                        texts[x] = null;
                        continue;
                    }

                    // OFR 수행
                    Draw.Rectangle rcSpare = new Draw.Rectangle(0, 0, bmpExact.Width, bmpExact.Height);
                    OfrResult_TbCharSetList resultCharSet = await OfrWork_Common.OfrStr_ComplexCharSetAsync(
                        bmpExact, rcSpare, bSaveToTbText: false, bEdit, bWrite, bMsgBox: false);

                    bmpExact?.Dispose();

                    texts[x] = resultCharSet?.strResult;
                    //Debug.WriteLine($"[InitDG오더] 2-{iteration+1}. 컬럼[{x}] 인식: '{texts[x]}'");
                }

                // 2-4. 우측에서 좌측으로 검사하여 불필요한 컬럼 이동
                int workCount = 0;
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
                        workCount++;
                        //Debug.WriteLine(
                        //    $"[InitDG오더] 2-{iteration+1}. 불필요 컬럼 발견: [{x}]{texts[x]} → 우측 이동");

                        // 수직 드래그로 우측 이동 (-50픽셀, 위로 드래그)
                        Draw.Point ptCenter = StdUtil.GetCenterDrawPoint(rcHeaders[x]);
                        await Simulation_Mouse.SafeMouseEvent_DragLeft_Smooth_VerticalAsync(
                            m_RcptPage.DG오더_hWnd, ptCenter, -50, false);

                        await Task.Delay(50);
                    }
                }

                bmpHeader.Dispose();

                // 2-5. 이동한 컬럼이 없으면 종료
                if (workCount == 0)
                {
                    //Debug.WriteLine($"[InitDG오더] 2-{iteration+1}. 이동할 컬럼 없음, Step 2 완료");
                    break;
                }

                //Debug.WriteLine($"[InitDG오더] 2-{iteration+1}. {workCount}개 컬럼 이동 완료");
            }

            Debug.WriteLine("[InitDG오더] Step 2 완료");

            // Step 3: 컬럼 순서 조정
            Debug.WriteLine("[InitDG오더] Step 3: 컬럼 순서 조정 시작");

            for (int x = 0; x < m_ReceiptDgHeaderInfos.Length; x++)
            {
                //Debug.WriteLine($"[InitDG오더] 3-{x+1}. 목표 컬럼: [{x}]{m_ReceiptDgHeaderInfos[x].sName}");

                // 3-1. 헤더 캡처
                Draw.Bitmap bmpHeader = OfrService.CaptureScreenRect_InWndHandle(
                    m_RcptPage.DG오더_hWnd, rcHeader);
                if (bmpHeader == null)
                {
                    return CommonFuncs_StdResult.ErrMsgResult_Error(
                        $"[InitDG오더]Step 3 헤더 캡처 실패 (컬럼 {x})",
                        "InsungsAct_RcptRegPage/InitDG오더Async_07", bWrite, true);
                }

                // 3-2. 컬럼 경계 검출
                byte byteMinBrightness = OfrService.GetMinBrightnessAtRow_FromColorBitmapFast(
                    bmpHeader, gab);
                byteMinBrightness += 2;

                bool[] resultArr = OfrService.GetBoolArray_FromColorBitmapRowFast(
                    bmpHeader, gab, byteMinBrightness, 2);
                List<OfrModel_LeftWidth> listLW = OfrService.GetLeftWidthList_FromBool1Array(
                    resultArr, byteMinBrightness);
                int columns = listLW.Count - 1;

                // 3-3. 목표 컬럼 텍스트 찾기
                string targetText = m_ReceiptDgHeaderInfos[x].sName;
                int index = -1;

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

                    Draw.Rectangle rcSpare = new Draw.Rectangle(0, 0, bmpExact.Width, bmpExact.Height);
                    OfrResult_TbCharSetList resultCharSet = await OfrWork_Common.OfrStr_ComplexCharSetAsync(
                        bmpExact, rcSpare, bSaveToTbText: false, bEdit, bWrite, bMsgBox: false);

                    bmpExact?.Dispose();

                    if (resultCharSet?.strResult == targetText)
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

            // Step 4: 컬럼 너비 조정
            Debug.WriteLine("[InitDG오더] Step 4: 컬럼 너비 조정 시작");

            for (int x = 0; x < m_ReceiptDgHeaderInfos.Length; x++)
            {
                //Debug.WriteLine($"[InitDG오더] 4-{x+1}. 컬럼 너비 조정: [{x}]{m_ReceiptDgHeaderInfos[x].sName}");

                // 4-1. 헤더 캡처
                Draw.Bitmap bmpHeader = OfrService.CaptureScreenRect_InWndHandle(
                    m_RcptPage.DG오더_hWnd, rcHeader);
                if (bmpHeader == null)
                {
                    return CommonFuncs_StdResult.ErrMsgResult_Error(
                        $"[InitDG오더]Step 4 헤더 캡처 실패 (컬럼 {x})",
                        "InsungsAct_RcptRegPage/InitDG오더Async_09", bWrite, true);
                }

                // 4-2. 컬럼 경계 검출
                byte byteMinBrightness = OfrService.GetMinBrightnessAtRow_FromColorBitmapFast(
                    bmpHeader, gab);
                byteMinBrightness += 2;

                bool[] resultArr = OfrService.GetBoolArray_FromColorBitmapRowFast(
                    bmpHeader, gab, byteMinBrightness, 2);
                List<OfrModel_LeftWidth> listLW = OfrService.GetLeftWidthList_FromBool1Array(
                    resultArr, byteMinBrightness);

                bmpHeader.Dispose();

                // 4-3. 현재 너비와 목표 너비 비교
                int currentWidth = listLW[x].nWidth;
                int targetWidth = m_ReceiptDgHeaderInfos[x].nWidth;
                int dx = targetWidth - currentWidth;

                if (dx == 0)
                {
                    //Debug.WriteLine($"[InitDG오더] 4-{x+1}. 너비 일치: {currentWidth}px");
                    continue; // 이미 원하는 너비
                }

                //Debug.WriteLine($"[InitDG오더] 4-{x+1}. 너비 조정: {currentWidth}px → {targetWidth}px (dx={dx})");

                // 4-4. 컬럼 오른쪽 경계를 dx만큼 드래그
                Draw.Point ptStart = new Draw.Point(listLW[x]._nRight + 1, gab);
                Draw.Point ptTarget = new Draw.Point(ptStart.X + dx, ptStart.Y);

                await Simulation_Mouse.SafeMouseEvent_DragLeft_SmoothAsync(
                    m_RcptPage.DG오더_hWnd, ptStart, ptTarget, bBkCursor: false, nMiliSec: 150);

                await Task.Delay(150);
            }

            Debug.WriteLine("[InitDG오더] Step 4 완료");

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
            if ((issues & DgValidationIssue.WrongOrder) != 0 ||
                (issues & DgValidationIssue.WrongWidth) != 0)
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

    #region 자동배차용 함수들
    /// <summary>
    /// 신규 주문 팝업창 열기
    /// - 신규 버튼 클릭 (최대 3회 재시도)
    /// - 팝업창 대기 및 검증 (최대 5초)
    /// - 성공 시 RegistOrderToPopupAsync 호출
    /// </summary>
    public async Task<StdResult_Status> OpenNewOrderPopupAsync(AutoAllocModel item, CancelTokenControl ctrl)
    {
        IntPtr hWndPopup = IntPtr.Zero;
        bool bFound = false;

        try
        {
            // 신규 버튼 클릭 시도 (최대 3번)
            for (int retry = 1; retry <= c_nRepeatShort; retry++)
            {
                await ctrl.WaitIfPausedOrCancelledAsync();

                // 신규 버튼 클릭
                await Std32Mouse_Post.MousePostAsync_ClickLeft(m_RcptPage.CmdBtn_hWnd신규);
                Debug.WriteLine($"[{m_Context.AppName}] 신규버튼 클릭 완료 (시도 {retry}/{CommonVars.c_nRepeatShort})");

                for (int k = 0; k < CommonVars.c_nRepeatMany; k++)
                {
                    await Task.Delay(CommonVars.c_nWaitNormal);

                    // 팝업창 찾기
                    hWndPopup = Std32Window.FindMainWindow_NotTransparent(
                        m_Context.MemInfo.Splash.TopWnd_uProcessId, m_Context.FileInfo.접수등록Wnd_TopWnd_sWndName_Reg);
                    if (hWndPopup == IntPtr.Zero) continue;

                    // 닫기 버튼 검증
                    IntPtr hWndClose = Std32Window.GetWndHandle_FromRelDrawPt(
                        hWndPopup, m_Context.FileInfo.접수등록Wnd_신규버튼그룹_ptChkRel닫기);
                    if (hWndClose == IntPtr.Zero) continue;

                    string closeText = Std32Window.GetWindowCaption(hWndClose);
                    if (closeText.StartsWith(m_Context.FileInfo.접수등록Wnd_버튼그룹_sWndName닫기))
                    {
                        bFound = true;
                        Debug.WriteLine($"[{m_Context.AppName}] 신규주문 팝업창 열림: {hWndPopup:X}");
                        break;
                    }
                }

                if (bFound) break;
            }

            if (bFound) return await RegistOrderToPopupAsync(item, hWndPopup, ctrl);
            else return new StdResult_Status(StdResult.Fail,
                "신규 버튼 클릭 후 팝업창이 열리지 않음", "OpenNewOrderPopupAsync_01");
        }
        catch (Exception ex)
        {
            return new StdResult_Status(StdResult.Fail, StdUtil.GetExceptionMessage(ex), "OpenNewOrderPopupAsync_999");
        }
    }

    /// <summary>
    /// 팝업창에 주문 정보 입력 및 등록
    /// - TopMost 설정 (포커스 유지)
    /// - TODO: 입력 작업 (현재는 MessageBox.Show만)
    /// - 닫기 버튼 클릭
    /// - 창 닫힘 확인 (성공 판단)
    /// </summary>
    public async Task<StdResult_Status> RegistOrderToPopupAsync(AutoAllocModel item, IntPtr hWndPopup, CancelTokenControl ctrl)
    {
        Draw.Bitmap bmpWnd = null;

        try
        {
            await ctrl.WaitIfPausedOrCancelledAsync();

            // 팝업창 TopMost 설정 (포커스 유지)
            await Std32Window.SetFocusWithForegroundAsync(hWndPopup);

            // Local Variables, Instances
            TbOrder tbOrder = item.NewOrder;
            var wndRcpt = new InsungsInfo_Mem.RcptWnd_New(hWndPopup, m_Context.FileInfo);
            wndRcpt.SetWndHandles(m_Context.FileInfo);

            #region ===== 1. 의뢰자 정보 입력 =====
            Debug.WriteLine($"[{m_Context.AppName}] 1. 의뢰자 정보 입력...");
            var result = await SearchAndSelectCustomerAsync(
                wndRcpt.의뢰자_hWnd고객명, wndRcpt.의뢰자_hWnd동명, tbOrder.CallCustName, tbOrder.CallChargeName, "의뢰자", ctrl);
            if (result.Result != StdResult.Success) goto EXIT;

            // 의뢰자 전화1
            result = await WriteAndVerifyEditBoxAsync(wndRcpt.의뢰자_hWnd전화1, tbOrder.CallTelNo ?? "", "의뢰자_전화1", ctrl);
            if (result.Result != StdResult.Success) goto EXIT;

            // 의뢰자 전화2
            result = await WriteAndVerifyEditBoxAsync(wndRcpt.의뢰자_hWnd전화2, tbOrder.CallTelNo2 ?? "", "의뢰자_전화2", ctrl);
            if (result.Result != StdResult.Success) goto EXIT;

            // 의뢰자 부서
            result = await WriteAndVerifyEditBoxAsync(wndRcpt.의뢰자_hWnd부서, tbOrder.CallDeptName ?? "", "의뢰자_부서", ctrl);
            if (result.Result != StdResult.Success) goto EXIT;

            // 의뢰자 담당
            result = await WriteAndVerifyEditBoxAsync(wndRcpt.의뢰자_hWnd담당, tbOrder.CallChargeName ?? "", "의뢰자_담당", ctrl);
            if (result.Result != StdResult.Success) goto EXIT;
            #endregion

            #region ===== 2. 출발지 정보 입력 =====
            Debug.WriteLine($"[{m_Context.AppName}] 2. 출발지 정보 입력...");

            // 출발지 = 의뢰자인 경우 vs 다른 경우 분기
            if (tbOrder.StartCustCodeK == tbOrder.CallCustCodeK)
            {
                // 출발지 = 의뢰자: Enter만 치고 동명 확인
                Debug.WriteLine($"[{m_Context.AppName}]   출발지 = 의뢰자: Enter로 자동 입력");
                Std32Key_Msg.KeyPost_Down(wndRcpt.출발지_hWnd고객명, StdCommon32.VK_RETURN);
                await Task.Delay(CommonVars.c_nWaitNormal, ctrl.Token);  // 100ms

                string 동명 = Std32Window.GetWindowCaption(wndRcpt.출발지_hWnd동명);
                if (string.IsNullOrEmpty(동명))
                {
                    Debug.WriteLine($"[{m_Context.AppName}] 출발지 동명 확인 실패");
                    result = new StdResult_Status(StdResult.Fail, "출발지 동명 확인 실패", "RegistOrderToPopupAsync_10");
                    goto EXIT;
                }
            }
            else
            {
                // 출발지 ≠ 의뢰자: 고객 검색
                Debug.WriteLine($"[{m_Context.AppName}]   출발지 ≠ 의뢰자: 고객 검색");
                result = await SearchAndSelectCustomerAsync(wndRcpt.출발지_hWnd고객명, wndRcpt.출발지_hWnd동명, tbOrder.StartCustName, tbOrder.StartChargeName, "출발지", ctrl);
                if (result.Result != StdResult.Success) goto EXIT;
            }

            // 출발지 동명
            result = await WriteAndVerifyEditBoxAsync(wndRcpt.출발지_hWnd동명, tbOrder.StartDongBasic ?? "", "출발지_동명", ctrl);
            if (result.Result != StdResult.Success) goto EXIT;

            // 출발지 전화1
            result = await WriteAndVerifyEditBoxAsync(wndRcpt.출발지_hWnd전화1, tbOrder.StartTelNo ?? "", "출발지_전화1", ctrl);
            if (result.Result != StdResult.Success) goto EXIT;

            // 출발지 전화2
            result = await WriteAndVerifyEditBoxAsync(wndRcpt.출발지_hWnd전화2, tbOrder.StartTelNo2 ?? "", "출발지_전화2", ctrl);
            if (result.Result != StdResult.Success) goto EXIT;

            // 출발지 부서
            result = await WriteAndVerifyEditBoxAsync(wndRcpt.출발지_hWnd부서, tbOrder.StartDeptName ?? "", "출발지_부서", ctrl);
            if (result.Result != StdResult.Success) goto EXIT;

            // 출발지 담당
            result = await WriteAndVerifyEditBoxAsync(wndRcpt.출발지_hWnd담당, tbOrder.StartChargeName ?? "", "출발지_담당", ctrl);
            if (result.Result != StdResult.Success) goto EXIT;

            // 출발지 위치
            result = await WriteAndVerifyEditBoxAsync(wndRcpt.출발지_hWnd위치, tbOrder.StartDetailAddr ?? "", "출발지_위치", ctrl);
            if (result.Result != StdResult.Success) goto EXIT;
            #endregion

            #region ===== 3. 도착지 정보 입력 =====
            Debug.WriteLine($"[{m_Context.AppName}] 3. 도착지 정보 입력...");

            // 도착지 = 의뢰자인 경우 vs 다른 경우 분기
            if (tbOrder.DestCustCodeK == tbOrder.CallCustCodeK)
            {
                // 도착지 = 의뢰자: Enter만 치고 동명 확인
                Debug.WriteLine($"[{m_Context.AppName}]   도착지 = 의뢰자: Enter로 자동 입력");
                Std32Key_Msg.KeyPost_Down(wndRcpt.도착지_hWnd고객명, StdCommon32.VK_RETURN);
                await Task.Delay(CommonVars.c_nWaitNormal, ctrl.Token);  // 100ms

                string 동명 = Std32Window.GetWindowCaption(wndRcpt.도착지_hWnd동명);
                if (string.IsNullOrEmpty(동명))
                {
                    Debug.WriteLine($"[{m_Context.AppName}] 도착지 동명 확인 실패");
                    result = new StdResult_Status(StdResult.Fail, "도착지 동명 확인 실패", "RegistOrderToPopupAsync_20");
                    goto EXIT;
                }
            }
            else
            {
                // 도착지 ≠ 의뢰자: 고객 검색
                Debug.WriteLine($"[{m_Context.AppName}]   도착지 ≠ 의뢰자: 고객 검색");
                result = await SearchAndSelectCustomerAsync(wndRcpt.도착지_hWnd고객명, wndRcpt.도착지_hWnd동명, tbOrder.DestCustName, tbOrder.DestChargeName, "도착지", ctrl);
                if (result.Result != StdResult.Success) goto EXIT;
            }

            // 도착지 동명
            result = await WriteAndVerifyEditBoxAsync(wndRcpt.도착지_hWnd동명, tbOrder.DestDongBasic ?? "", "도착지_동명", ctrl);
            if (result.Result != StdResult.Success) goto EXIT;

            // 도착지 전화1
            result = await WriteAndVerifyEditBoxAsync(wndRcpt.도착지_hWnd전화1, tbOrder.DestTelNo ?? "", "도착지_전화1", ctrl);
            if (result.Result != StdResult.Success) goto EXIT;

            // 도착지 전화2
            result = await WriteAndVerifyEditBoxAsync(wndRcpt.도착지_hWnd전화2, tbOrder.DestTelNo2 ?? "", "도착지_전화2", ctrl);
            if (result.Result != StdResult.Success) goto EXIT;

            // 도착지 부서
            result = await WriteAndVerifyEditBoxAsync(wndRcpt.도착지_hWnd부서, tbOrder.DestDeptName ?? "", "도착지_부서", ctrl);
            if (result.Result != StdResult.Success) goto EXIT;

            // 도착지 담당
            result = await WriteAndVerifyEditBoxAsync(wndRcpt.도착지_hWnd담당, tbOrder.DestChargeName ?? "", "도착지_담당", ctrl);
            if (result.Result != StdResult.Success) goto EXIT;

            // 도착지 위치
            result = await WriteAndVerifyEditBoxAsync(wndRcpt.도착지_hWnd위치, tbOrder.DestDetailAddr ?? "", "도착지_위치", ctrl);
            if (result.Result != StdResult.Success) goto EXIT;
            #endregion

            #region ===== 3. 우측상단 섹션 입력 =====
            // 4-1. 적요 (OrderRemarks)
            result = await WriteAndVerifyEditBoxAsync(wndRcpt.우측상단_hWnd적요, tbOrder.OrderRemarks ?? "", "적요", ctrl);
            if (result.Result != StdResult.Success) goto EXIT;

            // 4-2. 공유 (Share) - CheckBox (OFR 이미지 처리)
            Debug.WriteLine($"[{m_Context.AppName}] 4-2. 공유 CheckBox 처리...");

            // 화면 캡처
            bmpWnd = OfrService.CaptureScreenRect_InWndHandle(wndRcpt.TopWnd_hWnd, 0);

            // 현재 CheckBox 상태 읽기
            StdResult_NulBool resultShare = await OfrWork_Insungs.OfrImgChkValue_RectInBitmapAsync(bmpWnd, m_Context.FileInfo.접수등록Wnd_우측상단_rcChkRel공유);

            if (resultShare.bResult == null)
            {
                result = new StdResult_Status(StdResult.Fail, "공유 CheckBox 인식 실패", "RegistOrderToPopupAsync_30");
                goto EXIT;
            }

            bool currentShare = StdConvert.NullableBoolToBool(resultShare.bResult);

            // 상태 변경 필요 시
            if (tbOrder.Share != currentShare)
            {
                for (int i = 0; i < CommonVars.c_nRepeatShort; i++)  // 3회
                {
                    await ctrl.WaitIfPausedOrCancelledAsync();

                    StdResult_Error resultError =
                        await OfrWork_Common.SetCheckBox_StatusAsync(wndRcpt.TopWnd_hWnd, m_Context.FileInfo.접수등록Wnd_우측상단_rcChkRel공유, tbOrder.Share, "공유");

                    if (resultError == null) break;
                    if (i == CommonVars.c_nRepeatShort - 1)  // 마지막 시도
                    {
                        result = new StdResult_Status(StdResult.Fail, "공유 CheckBox 변경 실패", "RegistOrderToPopupAsync_31");
                        goto EXIT;
                    }
                    await Task.Delay(CommonVars.c_nWaitLong, ctrl.Token);  // 250ms
                }
            }

            Debug.WriteLine($"[{m_Context.AppName}]   공유: {tbOrder.Share}");

            // 4-3. 요금종류 (FeeType) - RadioButton OFR (bmpWnd 재사용)
            Debug.WriteLine($"[{m_Context.AppName}] 4-3. 요금종류 RadioButton 처리...");
            await ctrl.WaitIfPausedOrCancelledAsync();

            StdResult_NulBool resultFeeType = await IsChecked요금종류Async(bmpWnd, wndRcpt.우측상단_btns요금종류, tbOrder.FeeType);

            if (resultFeeType.bResult == null)
            {
                result = new StdResult_Status(StdResult.Fail, "요금종류 RadioButton 인식 실패", "RegistOrderToPopupAsync_40");
                goto EXIT;
            }

            // 현재 상태와 목표 상태가 다르면 변경
            if (!StdConvert.NullableBoolToBool(resultFeeType.bResult))
            {
                for (int i = 0; i < CommonVars.c_nRepeatShort; i++)  // 3회
                {
                    await ctrl.WaitIfPausedOrCancelledAsync();

                    result = await SetGroupFeeTypeAsync(bmpWnd, wndRcpt.우측상단_btns요금종류, tbOrder.FeeType, ctrl);
                    if (result.Result == StdResult.Success) break;

                    if (i == CommonVars.c_nRepeatShort - 1)  // 마지막 시도
                    {
                        result = new StdResult_Status(StdResult.Fail, $"요금종류 설정 실패: {tbOrder.FeeType}", "RegistOrderToPopupAsync_41");
                        goto EXIT;
                    }
                    await Task.Delay(CommonVars.c_nWaitNormal, ctrl.Token);  // 100ms
                }
            }

            Debug.WriteLine($"[{m_Context.AppName}]   요금종류: {tbOrder.FeeType}");

            // 4-4. 차량종류 (CarType) - RadioButton OFR (bmpWnd 재사용)
            Debug.WriteLine($"[{m_Context.AppName}] 4-4. 차량종류 RadioButton 처리...");
            await ctrl.WaitIfPausedOrCancelledAsync();

            // 트럭인 경우: RadioButton 클릭 + ComboBox 처리 (신규이므로 항상 디폴트인 오토에서 시작)
            if (tbOrder.CarType == "트럭")
            {
                for (int i = 0; i < CommonVars.c_nRepeatShort; i++)  // 3회
                {
                    await ctrl.WaitIfPausedOrCancelledAsync();

                    result = await SetGroupCarTypeAsync_트럭(bmpWnd, wndRcpt.우측상단_btns차량종류, tbOrder, ctrl);
                    if (result.Result == StdResult.Success) break;

                    if (i == CommonVars.c_nRepeatShort - 1)
                    {
                        result = new StdResult_Status(StdResult.Fail, $"트럭 설정 실패", "RegistOrderToPopupAsync_51");
                        goto EXIT;
                    }
                    await Task.Delay(CommonVars.c_nWaitNormal, ctrl.Token);
                }

                Debug.WriteLine($"[{m_Context.AppName}]   차량종류: {tbOrder.CarType}");
                Debug.WriteLine($"[{m_Context.AppName}]   차량무게: {tbOrder.CarWeight}");
                Debug.WriteLine($"[{m_Context.AppName}]   트럭상세: {tbOrder.TruckDetail}");
            }
            else
            {
                // 일반 차량: 요금종류와 동일한 패턴
                StdResult_NulBool resultCarType = await IsChecked차량종류Async(bmpWnd, wndRcpt.우측상단_btns차량종류, tbOrder.CarType);

                if (resultCarType.bResult == null)
                {
                    result = new StdResult_Status(StdResult.Fail, "차량종류 RadioButton 인식 실패", "RegistOrderToPopupAsync_52");
                    goto EXIT;
                }

                // 현재 상태와 목표 상태가 다르면 변경
                if (!StdConvert.NullableBoolToBool(resultCarType.bResult))
                {
                    for (int i = 0; i < CommonVars.c_nRepeatShort; i++)  // 3회
                    {
                        await ctrl.WaitIfPausedOrCancelledAsync();

                        int index = GetCarTypeIndex(tbOrder.CarType);
                        result = await SetCheckRadioBtn_InGroupAsync(bmpWnd, wndRcpt.우측상단_btns차량종류, index, ctrl);
                        if (result.Result == StdResult.Success) break;

                        if (i == CommonVars.c_nRepeatShort - 1)
                        {
                            result = new StdResult_Status(StdResult.Fail, $"차량종류 설정 실패: {tbOrder.CarType}", "RegistOrderToPopupAsync_53");
                            goto EXIT;
                        }
                        await Task.Delay(CommonVars.c_nWaitNormal, ctrl.Token);
                    }
                }

                Debug.WriteLine($"[{m_Context.AppName}]   차량종류: {tbOrder.CarType}");
            }

            // 4-5. 배송타입 (DeliverType) - RadioButton OFR (bmpWnd 재사용)
            Debug.WriteLine($"[{m_Context.AppName}] 4-5. 배송타입 RadioButton 처리...");
            await ctrl.WaitIfPausedOrCancelledAsync();

            StdResult_NulBool resultDeliverType = await IsChecked배송타입Async(bmpWnd, wndRcpt.우측상단_btns배송종류, tbOrder.DeliverType);

            if (resultDeliverType.bResult == null)
            {
                result = new StdResult_Status(StdResult.Fail, "배송타입 RadioButton 인식 실패", "RegistOrderToPopupAsync_60");
                goto EXIT;
            }

            // 현재 상태와 목표 상태가 다르면 변경
            if (!StdConvert.NullableBoolToBool(resultDeliverType.bResult))
            {
                for (int i = 0; i < CommonVars.c_nRepeatShort; i++)  // 3회
                {
                    await ctrl.WaitIfPausedOrCancelledAsync();

                    result = await SetGroupDeliverTypeAsync(bmpWnd, wndRcpt.우측상단_btns배송종류, tbOrder.DeliverType, ctrl);
                    if (result.Result == StdResult.Success) break;

                    if (i == CommonVars.c_nRepeatShort - 1)  // 마지막 시도
                    {
                        result = new StdResult_Status(StdResult.Fail, $"배송타입 설정 실패: {tbOrder.DeliverType}", "RegistOrderToPopupAsync_61");
                        goto EXIT;
                    }
                    await Task.Delay(CommonVars.c_nWaitNormal, ctrl.Token);  // 100ms
                }
            }

            Debug.WriteLine($"[{m_Context.AppName}]   배송타입: {tbOrder.DeliverType}");

            // 4-6. 계산서 (TaxBill) - CheckBox OFR (bmpWnd 재사용)
            Debug.WriteLine($"[{m_Context.AppName}] 4-6. 계산서 CheckBox 처리...");

            // 현재 CheckBox 상태 읽기
            StdResult_NulBool resultTaxBill = await OfrWork_Insungs.OfrImgChkValue_RectInBitmapAsync(bmpWnd, m_Context.FileInfo.접수등록Wnd_우측상단_rcChkRel계산서);

            if (resultTaxBill.bResult == null)
            {
                result = new StdResult_Status(StdResult.Fail, "계산서 CheckBox 인식 실패", "RegistOrderToPopupAsync_70");
                goto EXIT;
            }

            bool currentTaxBill = StdConvert.NullableBoolToBool(resultTaxBill.bResult);

            // 상태 변경 필요 시
            if (tbOrder.TaxBill != currentTaxBill)
            {
                for (int i = 0; i < CommonVars.c_nRepeatShort; i++)  // 3회
                {
                    await ctrl.WaitIfPausedOrCancelledAsync();

                    StdResult_Error resultError =
                        await OfrWork_Common.SetCheckBox_StatusAsync(wndRcpt.TopWnd_hWnd, m_Context.FileInfo.접수등록Wnd_우측상단_rcChkRel계산서, tbOrder.TaxBill, "계산서");

                    if (resultError == null) break;
                    if (i == CommonVars.c_nRepeatShort - 1)  // 마지막 시도
                    {
                        result = new StdResult_Status(StdResult.Fail, "계산서 CheckBox 변경 실패", "RegistOrderToPopupAsync_71");
                        goto EXIT;
                    }
                    await Task.Delay(CommonVars.c_nWaitLong, ctrl.Token);  // 250ms
                }
            }

            Debug.WriteLine($"[{m_Context.AppName}]   계산서: {tbOrder.TaxBill}");
            #endregion

            #region ===== 4. 요금 그룹 입력 =====
            Debug.WriteLine($"[{m_Context.AppName}] 4-7. 요금 그룹 입력...");
            await ctrl.WaitIfPausedOrCancelledAsync();

            // 기본요금
            result = await ResgistAndVerify요금Async(wndRcpt.요금그룹_hWnd기본요금, tbOrder.FeeBasic, "기본요금", ctrl);
            if (result.Result != StdResult.Success) goto EXIT;

            // 추가금액
            result = await ResgistAndVerify요금Async(wndRcpt.요금그룹_hWnd추가금액, tbOrder.FeePlus, "추가금액", ctrl);
            if (result.Result != StdResult.Success) goto EXIT;

            // 할인금액
            result = await ResgistAndVerify요금Async(wndRcpt.요금그룹_hWnd할인금액, tbOrder.FeeMinus, "할인금액", ctrl);
            if (result.Result != StdResult.Success) goto EXIT;

            // 탁송료
            result = await ResgistAndVerify요금Async(wndRcpt.요금그룹_hWnd탁송료, tbOrder.FeeConn, "탁송료", ctrl);
            if (result.Result != StdResult.Success) goto EXIT;
            #endregion

            #region ===== 4. 오더메모 그룹 입력 =====
            Debug.WriteLine($"[{m_Context.AppName}] 4-8. 오더메모 입력...");
            await ctrl.WaitIfPausedOrCancelledAsync();

            // 오더메모 (KeyCode/OrderMemo 형식)
            string orderMemo = $"{tbOrder.KeyCode}/{tbOrder.OrderMemo}";
            result = await WriteAndVerifyEditBoxAsync(wndRcpt.우측하단_hWnd오더메모, orderMemo, "오더메모", ctrl);
            if (result.Result != StdResult.Success) goto EXIT;
            #endregion

            #region Region.기사 - 직접입력 불가
            //bkDrvCode = tbOrder.DriverCode;
            //await Simulation_Keyboard.KeyPost_TextAsync(wndRcpt.기사그룹_hWnd기사이름, tbOrder.DriverId, true); // DriverId
            //await Simulation_Keyboard.KeyPost_TextAsync(wndRcpt.기사그룹_hWnd기사소속, tbOrder.DriverCenterId, true); // DriverCenterId
            //await Simulation_Keyboard.KeyPost_TextAsync(wndRcpt.기사그룹_hWnd기사전화, tbOrder.DriverTelNo, true); // DriverTelNo

            //resultBool = OfrWork_Common.WriteEditBox_ToHndleAsync(wndRcpt.기사그룹_hWnd기사이름, tbOrder.DriverId); // DriverId
            //if (resultBool == null || !resultBool.bResult)
            //    return new StdResult_Status(StdResult.Retry, $"텍스트 입력실패: {resultStr.sErr}", "NwIsAct_ReceiptPage/접수Wnd_RegistOrderAsync_29", s_sLogDir);

            //resultBool = OfrWork_Common.WriteEditBox_ToHndleAsync(wndRcpt.기사그룹_hWnd기사소속, tbOrder.DriverCenterId); // DriverCenterId
            //if (resultBool == null || !resultBool.bResult)
            //    return new StdResult_Status(StdResult.Retry, $"텍스트 입력실패: {resultStr.sErr}", "NwIsAct_ReceiptPage/접수Wnd_RegistOrderAsync_30", s_sLogDir);

            //resultBool = OfrWork_Common.WriteEditBox_ToHndleAsync(wndRcpt.기사그룹_hWnd기사전화, tbOrder.DriverTelNo); // DriverTelNo
            //if (resultBool == null || !resultBool.bResult)
            //    return new StdResult_Status(StdResult.Retry, $"텍스트 입력실패: {resultStr.sErr}", "NwIsAct_ReceiptPage/접수Wnd_RegistOrderAsync_31", s_sLogDir);
            #endregion End - Region.기사

            EXIT:

            #region ===== 저장작업 =====

            if (result.Result == StdResult.Success)
            {
                #region 입력 성공 시: 저장 버튼 클릭

                IntPtr hWndBtn;
                string btnName;
                bool bClosed = false;

                // Step 1: 저장 버튼 선택 (접수 or 대기)
                if (tbOrder.OrderState == "접수")
                {
                    hWndBtn = wndRcpt.Btn_hWnd접수저장;
                    btnName = "접수저장";
                }
                else
                {
                    hWndBtn = wndRcpt.Btn_hWnd대기저장;
                    btnName = "대기저장";
                }

                Debug.WriteLine($"[{m_Context.AppName}] 입력 성공 → {btnName} 버튼 클릭 시도");

                // Step 2: 저장 버튼 클릭 및 창 닫힘 확인
                bClosed = await ClickNWaitWindowChangedAsync(hWndBtn, wndRcpt.TopWnd_hWnd, ctrl);


                if (bClosed)
                {
                    // ==== 테스트: 조회, 첫 로우 선택, Seqno OFR ====
                    await Task.Delay(CommonVars.c_nWaitLong, ctrl.Token); // 창 닫힌 후 UI 안정화 대기

                    // 1. 조회 버튼 클릭 (DB refresh)
                    StdResult_Status resultQuery = await Click조회버튼Async(ctrl);
                    if (resultQuery.Result != StdResult.Success)
                    {
                        return new StdResult_Status(StdResult.Fail, $"조회 실패: {resultQuery.sErr}");
                    }

                    // 2. 첫 로우 클릭 및 선택 검증
                    bool bClicked = await ClickFirstRowAsync(ctrl);
                    if (!bClicked)
                    {
                        return new StdResult_Status(StdResult.Fail, "첫 로우 선택 실패");
                    }

                    // 3. Seqno OFR (첫 로우 선택 상태에서 RGB 반전 후 인식)
                    StdResult_String resultSeqno = await GetFirstRowSeqnoAsync(ctrl);
                    if (string.IsNullOrEmpty(resultSeqno.strResult))
                    {
                        return new StdResult_Status(StdResult.Fail, $"Seqno 획득 실패: {resultSeqno.sErr}");
                    }

                    Debug.WriteLine($"[{m_Context.AppName}] 주문 등록 완료 - Seqno: {resultSeqno.strResult}");

                    // 4. Kai DB의 Insung1 필드 업데이트
                    // 4-1. 사전 체크
                    if (item.NewOrder.KeyCode <= 0)
                    {
                        Debug.WriteLine($"[{m_Context.AppName}] KeyCode 없음 - Kai DB에 없는 주문");
                        return new StdResult_Status(StdResult.Fail, "Kai DB에 없는 주문입니다");
                    }

                    if (!string.IsNullOrEmpty(item.NewOrder.Insung1))
                    {
                        Debug.WriteLine($"[{m_Context.AppName}] 이미 등록된 주문번호: {item.NewOrder.Insung1}");
                        return new StdResult_Status(StdResult.Skip, "이미 Insung1 번호가 등록되어 있습니다");
                    }

                    if (s_SrGClient == null || !s_SrGClient.m_bLoginSignalR)
                    {
                        Debug.WriteLine($"[{m_Context.AppName}] SignalR 연결 안됨");
                        return new StdResult_Status(StdResult.Fail, "서버 연결이 끊어졌습니다");
                    }

                    // 4-2. 업데이트 실행
                    item.NewOrder.Insung1 = resultSeqno.strResult;
                    StdResult_Int resultUpdate = await s_SrGClient.SrResult_Order_UpdateRowAsync_Today(item.NewOrder);

                    if (resultUpdate.nResult < 0 || !string.IsNullOrEmpty(resultUpdate.sErr))
                    {
                        Debug.WriteLine($"[{m_Context.AppName}] Kai DB 업데이트 실패: {resultUpdate.sErr}");
                        return new StdResult_Status(StdResult.Fail, $"Kai DB 업데이트 실패: {resultUpdate.sErr}");
                    }

                    Debug.WriteLine($"[{m_Context.AppName}] Kai DB 업데이트 성공 - Insung1: {resultSeqno.strResult}");

                    return new StdResult_Status(StdResult.Success, $"{btnName} 완료 (Seqno: {resultSeqno.strResult})");
                }

                // Step 3: 저장 버튼으로 안 닫혔으면 닫기 버튼 시도
                Debug.WriteLine($"[{m_Context.AppName}] {btnName} 버튼으로 창이 안 닫힘 → 닫기 버튼 시도");
                bClosed = await ClickNWaitWindowChangedAsync(wndRcpt.Btn_hWnd닫기, wndRcpt.TopWnd_hWnd, ctrl);

                if (bClosed)
                {
                    Debug.WriteLine($"[{m_Context.AppName}] 닫기 버튼으로 창 닫힘 → 저장 실패 가능성");
                    return new StdResult_Status(StdResult.Retry, $"{btnName} 버튼 클릭 후 창이 안 닫혀서 닫기 버튼으로 닫음. 저장 확인 필요.", "InsungsAct_RcptRegPage/접수Wnd_RegistOrderAsync_Exit01");
                }
                else
                {
                    Debug.WriteLine($"[{m_Context.AppName}] 닫기 버튼으로도 창이 안 닫힘 → 치명적 에러");
                    return new StdResult_Status(StdResult.Fail, $"입력 완료 후 창을 닫을 수 없음. {btnName} 버튼과 닫기 버튼 모두 실패.", "InsungsAct_RcptRegPage/접수Wnd_RegistOrderAsync_Exit02");
                }

                #endregion
            }
            else
            {
                #region 입력 실패 시: 닫기 버튼 클릭

                Debug.WriteLine($"[{m_Context.AppName}] 입력 실패 → 닫기 버튼으로 창 닫기 시도");
                Debug.WriteLine($"[{m_Context.AppName}] 실패 원인: {result.sErr}");

                // Step 1: 닫기 버튼 클릭 및 창 닫힘 확인
                bool bClosed = await ClickNWaitWindowChangedAsync(wndRcpt.Btn_hWnd닫기, wndRcpt.TopWnd_hWnd, ctrl);

                if (bClosed)
                {
                    Debug.WriteLine($"[{m_Context.AppName}] 닫기 버튼으로 창 닫힘 확인 → 입력 실패 결과 반환");
                    return result; // 원래 입력 실패 결과 그대로 반환
                }
                else
                {
                    Debug.WriteLine($"[{m_Context.AppName}] 닫기 버튼으로도 창이 안 닫힘 → 치명적 에러");
                    return new StdResult_Status(StdResult.Fail, $"입력 실패 후 창도 닫을 수 없음. 원인: {result.sErr}", "InsungsAct_RcptRegPage/접수Wnd_RegistOrderAsync_Exit03");
                }

                #endregion
            }
            #endregion
        }
        catch (Exception ex)
        {
            return new StdResult_Status(StdResult.Fail, StdUtil.GetExceptionMessage(ex), "RegistOrderToPopupAsync_999");
        }
        finally
        {
            Std32Window.SetWindowTopMost(hWndPopup, false);
            bmpWnd?.Dispose();
        }
    }
    #endregion

    #region 자동배차 Helper 함수들
    /// <summary>
    /// 신규 주문 등록 확인 (Kai에만 존재, 인성에 없음)
    /// </summary>
    public async Task<StdResult_Status> CheckIsOrderAsync_AssumeKaiNewOrder(AutoAllocModel item, CancelTokenControl ctrl)
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
                return await OpenNewOrderPopupAsync(item, ctrl);

            case "배차":
            case "운행":
            case "완료":
            case "예약":
                return new StdResult_Status(StdResult.Fail,
                    $"미구현 상태: {kaiState}", "CheckIsOrderAsync_AssumeKaiNewOrder_TODO", CommonVars.s_sLogDir);

            default:
                return new StdResult_Status(StdResult.Fail,
                    $"알 수 없는 Kai 주문 상태: {kaiState}", "CheckIsOrderAsync_AssumeKaiNewOrder_800", CommonVars.s_sLogDir);
        }
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
    private async Task<(IntPtr hWnd, StdResult_Error error)> FindStatusButtonAsync(
        string buttonName, Draw.Point checkPoint, string errorCode, bool bWrite, bool bMsgBox, bool withTextValidation = true)
    {
        for (int i = 0; i < CommonVars.c_nRepeatVeryMany; i++)
        {
            IntPtr hWnd = Std32Window.GetWndHandle_FromRelDrawPt(m_Main.TopWnd_hWnd, checkPoint);

            if (hWnd != IntPtr.Zero)
            {
                if (withTextValidation)
                {
                    string text = Std32Window.GetWindowText(hWnd);
                    if (text.Contains(buttonName))
                    {
                        Debug.WriteLine($"[InsungsAct_RcptRegPage] {buttonName}버튼 찾음: {hWnd:X}, 텍스트: {text}");
                        return (hWnd, null);
                    }
                }
                else
                {
                    Debug.WriteLine($"[InsungsAct_RcptRegPage] {buttonName}버튼 찾음: {hWnd:X}");
                    return (hWnd, null);
                }
            }

            await Task.Delay(CommonVars.c_nWaitNormal);
        }

        // 찾기 실패
        var error = CommonFuncs_StdResult.ErrMsgResult_Error(
            $"[{m_Context.AppName}/RcptRegPage]{buttonName}버튼 찾기실패: {checkPoint}",
            errorCode, bWrite, bMsgBox);
        return (IntPtr.Zero, error);
    }

    /// <summary>
    /// 버튼 클릭 후 윈도우가 닫힐 때까지 대기
    /// </summary>
    /// <param name="hWndClick">클릭할 버튼 핸들</param>
    /// <param name="hWndOrg">닫혀야 할 윈도우 핸들</param>
    /// <param name="ctrl">취소 토큰 컨트롤</param>
    /// <returns>윈도우가 닫혔으면 true, 실패하면 false</returns>
    private async Task<bool> ClickNWaitWindowChangedAsync(IntPtr hWndClick, IntPtr hWndOrg, CancelTokenControl ctrl)
    {
        for (int i = 1; i <= c_nRepeatShort; i++)
        {
            await ctrl.WaitIfPausedOrCancelledAsync();

            // 버튼 클릭
            await Std32Mouse_Post.MousePostAsync_ClickLeft_Center(hWndClick);
            Debug.WriteLine($"[{m_Context.AppName}] 버튼 클릭 시도 {i}/{c_nRepeatShort}");

            // 윈도우가 닫힐 때까지 대기 (최대 5초: 100회 × 50ms)
            for (int j = 0; j < c_nRepeatVeryMany; j++)
            {
                await Task.Delay(c_nWaitShort, ctrl.Token);

                // 윈도우가 닫혔는지 확인
                if (!Std32Window.IsWindow(hWndOrg))
                {
                    Debug.WriteLine($"[{m_Context.AppName}] 윈도우 닫힘 확인 (시도 {i}, 대기 {j * c_nWaitShort}ms)");
                    return true;
                }
            }

            Debug.WriteLine($"[{m_Context.AppName}] {i}번째 시도 실패 - 윈도우가 닫히지 않음");
        }

        Debug.WriteLine($"[{m_Context.AppName}] 모든 시도 실패 - 윈도우가 닫히지 않음");
        return false;
    }

    /// <summary>
    /// CommandBtn(OFR 검증 포함) 찾기 헬퍼 메서드
    /// </summary>
    private async Task<(IntPtr hWnd, StdResult_Error error)> FindCommandButtonWithOfrAsync(
        string buttonName, Draw.Point checkPoint, string ofrImageKey, string errorCode, bool bEdit, bool bWrite, bool bMsgBox)
    {
        // 1. 버튼 핸들 찾기
        IntPtr hWnd = Std32Window.GetWndHandle_FromRelDrawPt(m_Main.TopWnd_hWnd, checkPoint);
        if (hWnd == IntPtr.Zero)
        {
            var error = CommonFuncs_StdResult.ErrMsgResult_Error(
                $"[{m_Context.AppName}/RcptRegPage]{buttonName}버튼 찾기실패: {checkPoint}",
                errorCode, bWrite, bMsgBox);
            return (IntPtr.Zero, error);
        }
        Debug.WriteLine($"[InsungsAct_RcptRegPage] {buttonName}버튼 찾음: {hWnd:X}");

        // 2. OFR 이미지 매칭으로 검증
        StdResult_NulBool resultOfr = await OfrWork_Insungs.OfrIsMatchedImage_DrawRelRectAsync(
            hWnd, 0, ofrImageKey, bEdit, bWrite, false);
        if (!StdConvert.NullableBoolToBool(resultOfr.bResult))
        {
            Debug.WriteLine($"[InsungsAct_RcptRegPage] {buttonName}버튼 OFR 검증 실패 (무시): {resultOfr.sErr}");
            // OFR 검증 실패는 경고만 출력 (실패해도 진행)
        }

        return (hWnd, null);
    }

    /// <summary>
    /// CallCount 컨트롤 찾기 헬퍼 메서드
    /// </summary>
    private IntPtr FindCallCountControl(string controlName, Draw.Point checkPoint, string errorCode, bool bWrite, bool bMsgBox, out StdResult_Error error)
    {
        IntPtr hWnd = Std32Window.GetWndHandle_FromRelDrawPt(m_Main.TopWnd_hWnd, checkPoint);
        if (hWnd == IntPtr.Zero)
        {
            error = CommonFuncs_StdResult.ErrMsgResult_Error(
                $"[{m_Context.AppName}/RcptRegPage]{controlName}CallCount 찾기실패: {checkPoint}",
                errorCode, bWrite, bMsgBox);
            return IntPtr.Zero;
        }
        Debug.WriteLine($"[InsungsAct_RcptRegPage] {controlName}CallCount 찾음: {hWnd:X}");
        error = null;
        return hWnd;
    }

    /// <summary>
    /// Datagrid 상태 검증 (컬럼 개수, 순서, 너비 확인)
    /// </summary>
    /// <param name="columnTexts">현재 읽은 컬럼 헤더 텍스트 배열</param>
    /// <param name="listLW">컬럼 Left/Width 리스트</param>
    /// <returns>검증 이슈 플래그 (None이면 정상)</returns>
    private DgValidationIssue ValidateDatagridState(string[] columnTexts, List<OfrModel_LeftWidth> listLW)
    {
        DgValidationIssue issues = DgValidationIssue.None;

        // 1. 컬럼 개수 체크
        if (columnTexts == null || columnTexts.Length != m_ReceiptDgHeaderInfos.Length)
        {
            issues |= DgValidationIssue.InvalidColumnCount;
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
                issues |= DgValidationIssue.InvalidColumn;
                Debug.WriteLine($"[ValidateDatagridState] 유효하지 않은 컬럼[{x}]: '{columnText}'");
                continue; // 다음 컬럼 검사
            }

            // 2-2. 컬럼 순서가 맞는지
            if (index != x)
            {
                issues |= DgValidationIssue.WrongOrder;
                Debug.WriteLine($"[ValidateDatagridState] 컬럼 순서 불일치[{x}]: '{columnText}' (예상 위치={index})");
            }

            // 2-3. 컬럼 너비가 맞는지 (허용 오차 이내인지)
            int actualWidth = listLW[x].nWidth;
            int expectedWidth = m_ReceiptDgHeaderInfos[index].nWidth;
            int widthDiff = Math.Abs(actualWidth - expectedWidth);

            if (widthDiff > COLUMN_WIDTH_TOLERANCE)
            {
                issues |= DgValidationIssue.WrongWidth;
                Debug.WriteLine($"[ValidateDatagridState] 컬럼 너비 불일치[{x}]: '{columnText}', 실제={actualWidth}, 예상={expectedWidth}, 오차={widthDiff}");
            }
        }

        if (issues == DgValidationIssue.None)
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
            Debug.WriteLine($"[{m_Context.AppName}] 로딩 패널 출현 대기 시작");

            for (int i = 0; i < CommonVars.c_nWaitLong; i++) // 250ms
            {
                await ctrl.WaitIfPausedOrCancelledAsync();

                hWndFind = Std32Window.GetWndHandle_FromRelDrawPt(hWndDG, ptCheckPan);
                if (hWndFind != hWndDG)
                {
                    Debug.WriteLine($"[{m_Context.AppName}] 로딩 패널 출현 확인 ({i}ms)");
                    break;
                }
                await Task.Delay(1, ctrl.Token);
            }

            if (hWndFind == hWndDG)
            {
                Debug.WriteLine($"[{m_Context.AppName}] 로딩 패널 미출현 → Skip (이미 로딩 완료)");
                return new StdResult_Status(StdResult.Skip);
            }

            // Phase 2: 로딩 패널 사라짐 대기 (최대 timeoutSec초)
            Debug.WriteLine($"[{m_Context.AppName}] 로딩 패널 사라짐 대기 시작 (최대 {timeoutSec}초)");

            int iterations = timeoutSec * 10; // 100ms 단위
            for (int i = 0; i < iterations; i++)
            {
                await ctrl.WaitIfPausedOrCancelledAsync();

                hWndFind = Std32Window.GetWndHandle_FromRelDrawPt(hWndDG, ptCheckPan);
                if (hWndFind == hWndDG)
                {
                    int elapsedMs = i * 100;
                    Debug.WriteLine($"[{m_Context.AppName}] 로딩 완료 ({elapsedMs}ms)");
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
    /// 비트맵 RGB 반전 (선택된 셀의 파란 배경 + 흰 텍스트 → 일반 상태로 변환)
    /// - 반전 후 기존 OFR 로직 그대로 사용 가능
    /// </summary>
    /// <param name="source">원본 비트맵</param>
    /// <returns>RGB 반전된 비트맵</returns>
    private static Draw.Bitmap InvertBitmap(Draw.Bitmap source)
    {
        int width = source.Width;
        int height = source.Height;

        // PixelFormat을 명시적으로 지정하여 생성
        Draw.Bitmap result = new Draw.Bitmap(width, height, DrawImg.PixelFormat.Format24bppRgb);

        DrawImg.BitmapData srcData = source.LockBits(
            new Draw.Rectangle(0, 0, width, height),
            DrawImg.ImageLockMode.ReadOnly,
            DrawImg.PixelFormat.Format24bppRgb);

        DrawImg.BitmapData dstData = result.LockBits(
            new Draw.Rectangle(0, 0, width, height),
            DrawImg.ImageLockMode.WriteOnly,
            DrawImg.PixelFormat.Format24bppRgb);

        try
        {
            int bytes = Math.Abs(srcData.Stride) * height;
            byte[] srcBytes = new byte[bytes];
            byte[] dstBytes = new byte[bytes];

            System.Runtime.InteropServices.Marshal.Copy(srcData.Scan0, srcBytes, 0, bytes);

            // RGB 반전: 255 - 원래값
            for (int i = 0; i < bytes; i++)
            {
                dstBytes[i] = (byte)(255 - srcBytes[i]);
            }

            System.Runtime.InteropServices.Marshal.Copy(dstBytes, 0, dstData.Scan0, bytes);
        }
        finally
        {
            source.UnlockBits(srcData);
            result.UnlockBits(dstData);
        }

        return result;
    }

    /// <summary>
    /// 첫 번째 데이터 로우의 주문번호(Seqno) OFR
    /// - 첫 로우가 선택된 상태에서 주문번호 셀 캡처
    /// - RGB 반전 후 단음소 OFR (TbCharBackup 사용)
    /// </summary>
    /// <param name="ctrl">취소 토큰</param>
    /// <param name="retryCount">재시도 횟수</param>
    /// <returns>성공: strResult에 Seqno, 실패: sErr에 에러 메시지</returns>
    private async Task<StdResult_String> GetFirstRowSeqnoAsync(CancelTokenControl ctrl, int retryCount = 3)
    {
        try
        {
            // 주문번호 컬럼 인덱스 (상수 사용)
            int seqnoColIndex = c_nCol주문번호;

            Debug.WriteLine($"[{m_Context.AppName}] 주문번호 컬럼 인덱스: {seqnoColIndex}");

            for (int i = 1; i <= retryCount; i++)
            {
                await ctrl.WaitIfPausedOrCancelledAsync();

                Debug.WriteLine($"[{m_Context.AppName}] ===== Seqno OFR 시도 {i}/{retryCount} =====");

                // 1. 주문번호 셀 위치: [seqnoColIndex, 2] (y=2: 첫 데이터 로우)
                Draw.Rectangle rectSeqnoCell = m_RcptPage.DG오더_RelChildRects[seqnoColIndex, 2];

                Debug.WriteLine($"[{m_Context.AppName}] 주문번호 셀 위치 [{seqnoColIndex}, 2]: {rectSeqnoCell}");

                // 2. 셀 캡처 (선택 상태 - 파란 배경에 흰 텍스트)
                Draw.Bitmap bmpCell = OfrService.CaptureScreenRect_InWndHandle(
                    m_RcptPage.DG오더_hWnd, rectSeqnoCell);

                if (bmpCell == null)
                {
                    Debug.WriteLine($"[{m_Context.AppName}] 주문번호 셀 캡처 실패 (시도 {i}/{retryCount})");
                    if (i < retryCount) await Task.Delay(200, ctrl.Token);
                    continue;
                }

                Draw.Bitmap bmpInverted = null;

                try
                {
                    // 3. RGB 반전 (파란 배경 + 흰 텍스트 → 일반 상태로)
                    bmpInverted = InvertBitmap(bmpCell);
                    bmpCell.Dispose();
                    bmpCell = null;

                    Debug.WriteLine($"[{m_Context.AppName}] RGB 반전 완료");

                    // 4. 단음소 OFR (TbCharBackup 사용)
                    StdResult_String resultSeqno = await OfrWork_Common.OfrStr_SeqCharAsync(bmpInverted);

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
                    bmpCell?.Dispose();
                    bmpInverted?.Dispose();
                }

                if (i < retryCount) await Task.Delay(200, ctrl.Token);
            }

            return new StdResult_String($"Seqno OFR 실패 ({retryCount}회 시도)", "GetFirstRowSeqnoAsync_99");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{m_Context.AppName}] GetFirstRowSeqnoAsync 예외: {ex.Message}");
            return new StdResult_String(StdUtil.GetExceptionMessage(ex), "GetFirstRowSeqnoAsync_999");
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

                Debug.WriteLine($"[{m_Context.AppName}] 조회 버튼 클릭 시도 {i}/{retryCount}");

                // 조회 버튼 클릭
                await Std32Mouse_Post.MousePostAsync_ClickLeft(m_RcptPage.CmdBtn_hWnd조회);

                // 로딩 패널 대기 - 이것만으로 성공 여부 판단
                StdResult_Status resultSts = await WaitPanLoadedAsync(m_RcptPage.DG오더_hWnd, ctrl);

                if (resultSts.Result == StdResult.Success || resultSts.Result == StdResult.Skip)
                {
                    Debug.WriteLine($"[{m_Context.AppName}] 조회 완료 (시도 {i}회, 결과: {resultSts.Result})");
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
    #endregion

    #region UI용 함수들
    /// <summary>
    /// 공통 함수 #2: 고객 검색 및 선택 (의뢰자, 출발지, 도착지 공통)
    /// - 입력 검증, 예외 처리, CancelToken 지원 포함
    /// - TODO: Multi(복수 고객), None(신규 고객) 케이스 처리 필요
    /// </summary>
    private async Task<StdResult_Status> SearchAndSelectCustomerAsync(IntPtr hWnd고객명, IntPtr hWnd동명, string custName, string chargeName, string fieldName, CancelTokenControl ctrl)
    {
        try
        {
            // 0. 입력 검증
            await ctrl.WaitIfPausedOrCancelledAsync();

            if (string.IsNullOrWhiteSpace(custName))
            {
                Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 고객명이 비어있습니다.");
                return new StdResult_Status(StdResult.Fail,
                    $"{fieldName} 고객명이 비어있습니다.",
                    "SearchAndSelectCustomerAsync_00");
            }

            if (hWnd고객명 == IntPtr.Zero)
            {
                Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 고객명 EditBox 핸들이 유효하지 않습니다.");
                return new StdResult_Status(StdResult.Fail,
                    $"{fieldName} 고객명 EditBox를 찾을 수 없습니다.",
                    "SearchAndSelectCustomerAsync_00_1");
            }

            // 1. 검색어 생성 (상호/담당)
            string searchText = NwCommon.GetInsungTextForSearch(custName, chargeName);
            Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 검색어: \"{searchText}\"");

            await ctrl.WaitIfPausedOrCancelledAsync();

            // 2. 고객명 EditBox에 입력 (OfrWork_Common 사용 - 기존 로직과 동일)
            var resultBool = await OfrWork_Common.WriteEditBox_ToHndleAsync(hWnd고객명, searchText);
            if (resultBool == null || !resultBool.bResult)
            {
                Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 고객명 입력 실패");
                return new StdResult_Status(StdResult.Fail,
                    $"{fieldName} 고객명 입력 실패",
                    "SearchAndSelectCustomerAsync_01");
            }

            await ctrl.WaitIfPausedOrCancelledAsync();

            // 3. 검색 실행 및 결과 타입 확인 (GetCustSearchTypeAsync - Enter 키 포함)
            var searchResult = await GetCustSearchTypeAsync(hWnd고객명, hWnd동명, ctrl);

            // 4. 검색 결과 처리
            switch (searchResult.resultTye)
            {
                case AutoAlloc_CustSearch.One:
                    // 1개 검색 성공 - 그대로 진행
                    Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 검색 성공 (1개)");
                    return new StdResult_Status(StdResult.Success);

                case AutoAlloc_CustSearch.Multi:
                    // TODO: 복수 고객 검색창 처리 필요
                    Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 복수 검색 결과 - TODO 처리 필요");
                    return new StdResult_Status(StdResult.Fail,
                        $"{fieldName} 복수 검색됨 (TODO: 고객검색창 처리 필요)",
                        "SearchAndSelectCustomerAsync_02");

                case AutoAlloc_CustSearch.None:
                    // TODO: 신규 고객 등록창 처리 필요
                    Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 신규 고객 - TODO 처리 필요");
                    return new StdResult_Status(StdResult.Fail,
                        $"{fieldName} 신규 고객 (TODO: 고객등록창 처리 필요)",
                        "SearchAndSelectCustomerAsync_03");

                case AutoAlloc_CustSearch.Null:
                default:
                    Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 검색 타임아웃 또는 알 수 없는 결과");
                    return new StdResult_Status(StdResult.Fail,
                        $"{fieldName} 검색 타임아웃",
                        "SearchAndSelectCustomerAsync_04");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 검색 중 예외 발생: {ex.Message}");
            return new StdResult_Status(StdResult.Fail,
                StdUtil.GetExceptionMessage(ex),
                "SearchAndSelectCustomerAsync_999");
        }
    }

    /// <summary>
    /// 고객 검색 결과 타입 확인
    /// - Enter 키 전송 후 검색 결과가 1개인지, 복수인지, 신규인지 확인
    /// - CommonVars 상수 사용: c_nRepeatMany, c_nWaitVeryShort, c_nWaitShort
    /// </summary>
    private async Task<AutoAlloc_SearchTypeResult> GetCustSearchTypeAsync(IntPtr hWnd고객명, IntPtr hWnd동명, CancelTokenControl ctrl)
    {
        try
        {
            // 0. 입력 검증
            if (hWnd고객명 == IntPtr.Zero || hWnd동명 == IntPtr.Zero)
            {
                Debug.WriteLine($"[{m_Context.AppName}] GetCustSearchTypeAsync: 유효하지 않은 핸들 (고객명={hWnd고객명:X}, 동명={hWnd동명:X})");
                return new AutoAlloc_SearchTypeResult(AutoAlloc_CustSearch.Null, IntPtr.Zero);
            }

            // 1. EnterKey 전송 (검색 실행)
            Std32Key_Msg.KeyPost_Down(hWnd고객명, StdCommon32.VK_RETURN);
            Debug.WriteLine($"[{m_Context.AppName}] 고객 검색 Enter 키 전송");

            // 2. 검색 결과 확인 (최대 50번, 1.5초)
            for (int j = 0; j < CommonVars.c_nRepeatMany; j++)
            {
                await ctrl.WaitIfPausedOrCancelledAsync();
                await Task.Delay(CommonVars.c_nWaitVeryShort, ctrl.Token);

                // 2-1. 동명 확인 → 1개 검색 성공
                string dongName = Std32Window.GetWindowCaption(hWnd동명);
                if (!string.IsNullOrEmpty(dongName))
                {
                    Debug.WriteLine($"[{m_Context.AppName}] 고객 검색 성공: 1개 (동명={dongName})");
                    return new AutoAlloc_SearchTypeResult(AutoAlloc_CustSearch.One, IntPtr.Zero);
                }

                // 2-2. 고객등록창 확인 → 신규 고객
                IntPtr hWnd고객등록 = Std32Window.FindMainWindow(
                    m_Context.MemInfo.Splash.TopWnd_uProcessId, null, m_Context.FileInfo.고객등록Wnd_TopWnd_sWndName);
                if (hWnd고객등록 != IntPtr.Zero)
                {
                    Debug.WriteLine($"[{m_Context.AppName}] 신규 고객 등록창 발견: {hWnd고객등록:X}");
                    return new AutoAlloc_SearchTypeResult(AutoAlloc_CustSearch.None, hWnd고객등록);
                }

                // 2-3. 고객검색창 확인 → 복수 고객 (또는 단수 고객이 자동 닫힘)
                IntPtr hWnd고객검색 = Std32Window.FindMainWindow(
                    m_Context.MemInfo.Splash.TopWnd_uProcessId, null, m_Context.FileInfo.고객검색Wnd_TopWnd_sWndName);
                if (hWnd고객검색 != IntPtr.Zero)
                {
                    // 고객검색창이 떴을 때, 단수 고객이면 자동으로 닫힘
                    // 1.5초 동안 기다리면서 창이 닫히는지 확인
                    Debug.WriteLine($"[{m_Context.AppName}] 고객검색창 발견: {hWnd고객검색:X}, 닫힘 대기 중...");

                    for (int k = 0; k < CommonVars.c_nRepeatNormal * 3; k++)  // 30회 (10*3)
                    {
                        await ctrl.WaitIfPausedOrCancelledAsync();
                        await Task.Delay(CommonVars.c_nWaitShort, ctrl.Token);  // 50ms

                        hWnd고객검색 = Std32Window.FindMainWindow(
                            m_Context.MemInfo.Splash.TopWnd_uProcessId, null, m_Context.FileInfo.고객검색Wnd_TopWnd_sWndName);

                        if (hWnd고객검색 == IntPtr.Zero)
                        {
                            Debug.WriteLine($"[{m_Context.AppName}] 고객검색창 자동 닫힘 → 단수 고객, 동명 재확인 중...");
                            break;  // 창이 닫힘 → 단수 고객, 다음 루프에서 동명 확인
                        }
                    }

                    // 30번 반복 후에도 창이 살아있으면 복수 고객
                    if (hWnd고객검색 != IntPtr.Zero)
                    {
                        Debug.WriteLine($"[{m_Context.AppName}] 고객 검색 결과: 복수 (고객검색창={hWnd고객검색:X})");
                        return new AutoAlloc_SearchTypeResult(AutoAlloc_CustSearch.Multi, hWnd고객검색);
                    }
                    // 창이 닫혔으면 다음 루프에서 동명 확인으로 이동
                }
            }

            // 3. 검색 실패 (타임아웃)
            Debug.WriteLine($"[{m_Context.AppName}] GetCustSearchTypeAsync 실패: 타임아웃 (최대 {CommonVars.c_nRepeatMany}회 시도)");
            return new AutoAlloc_SearchTypeResult(AutoAlloc_CustSearch.Null, IntPtr.Zero);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{m_Context.AppName}] GetCustSearchTypeAsync 예외: {ex.Message}");
            return new AutoAlloc_SearchTypeResult(AutoAlloc_CustSearch.Null, IntPtr.Zero);
        }
    }

    /// <summary>
    /// EditBox 입력 및 검증 (재시도 포함)
    /// - CommonVars 상수 사용: c_nRepeatShort, c_nWaitLong
    /// - CancelToken 완전 지원
    /// - 입력 검증 및 예외 처리 포함
    /// </summary>
    private async Task<StdResult_Status> WriteAndVerifyEditBoxAsync(IntPtr hWnd, string expectedValue, string fieldName, CancelTokenControl ctrl, Func<string, string>? normalizeFunc = null)
    {
        try
        {
            // 0. 입력 검증
            await ctrl.WaitIfPausedOrCancelledAsync();

            if (hWnd == IntPtr.Zero)
            {
                Debug.WriteLine($"[{m_Context.AppName}] {fieldName} EditBox 핸들이 유효하지 않습니다.");
                return new StdResult_Status(StdResult.Fail,
                    $"{fieldName} EditBox를 찾을 수 없습니다.",
                    "WriteAndVerifyEditBoxAsync_00");
            }

            if (expectedValue == null)
                expectedValue = "";

            // 1. 재시도 루프 (최대 3번)
            for (int i = 0; i < CommonVars.c_nRepeatShort; i++)  // 3회
            {
                await ctrl.WaitIfPausedOrCancelledAsync();

                // 1-1. 쓰기 (대기 포함 버전 사용)
                string writtenValue = await OfrWork_Common.WriteEditBox_ToHndleAsyncWait(hWnd, expectedValue);

                // 1-2. 정규화 (필요시)
                if (normalizeFunc != null)
                    writtenValue = normalizeFunc(writtenValue);

                // 1-3. 검증
                if (expectedValue == writtenValue)
                {
                    Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 입력 성공: \"{expectedValue}\"");
                    return new StdResult_Status(StdResult.Success);
                }

                Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 불일치 (재시도 {i + 1}/{CommonVars.c_nRepeatShort}): " +
                              $"예상=\"{expectedValue}\", 실제=\"{writtenValue}\"");

                await Task.Delay(CommonVars.c_nWaitLong, ctrl.Token);  // 250ms
            }

            // 2. 최종 실패 (재시도 후에도 불일치)
            string finalValue = Std32Window.GetWindowCaption(hWnd);
            if (normalizeFunc != null)
                finalValue = normalizeFunc(finalValue);

            Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 입력 실패: 예상=\"{expectedValue}\", 실제=\"{finalValue}\"");
            return new StdResult_Status(StdResult.Fail,
                $"{fieldName} 입력 실패: 예상=\"{expectedValue}\", 실제=\"{finalValue}\"",
                "WriteAndVerifyEditBoxAsync_01");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 입력 중 예외 발생: {ex.Message}");
            return new StdResult_Status(StdResult.Fail,
                StdUtil.GetExceptionMessage(ex),
                "WriteAndVerifyEditBoxAsync_999");
        }
    }

    /// <summary>
    /// 공통 함수 #4: 요금종류 문자열을 인덱스로 변환
    /// - 선불=0, 착불=1, 신용=2, 송금=3, 수금=4, 카드=5
    /// - 미지원 타입은 0 (선불) 반환
    /// </summary>
    private int GetFeeTypeIndex(string sFeeType)
    {
        switch (sFeeType)
        {
            case "착불": return 1;
            case "신용": return 2;
            case "송금": return 3;
            case "수금": return 4;
            case "카드": return 5;
            default: return 0;  // 선불 (또는 미지원 타입)
        }
    }

    /// <summary>
    /// 차량종류 문자열을 인덱스로 변환
    /// - 오토=0, 밴=1, 트럭=2, 플렉스=3, 다마=4, 라보=5, 지하=6
    /// - 미지원 타입은 -1 반환
    /// </summary>
    private int GetCarTypeIndex(string sCarType)
    {
        return sCarType switch
        {
            "오토" => 0,
            "밴" => 1,
            "트럭" => 2,
            "플렉스" => 3,
            "다마" => 4,
            "라보" => 5,
            "지하" => 6,
            _ => -1
        };
    }

    /// <summary>
    /// 배송타입 문자열을 인덱스로 변환
    /// - 편도=0, 왕복=1, 경유=2, 긴급=3
    /// - 미지원 타입은 0 (편도) 반환
    /// </summary>
    private int GetDeliverTypeIndex(string sDeliverType)
    {
        return sDeliverType switch
        {
            "왕복" => 1,
            "경유" => 2,
            "긴급" => 3,
            _ => 0  // 편도 (또는 미지원 타입)
        };
    }

    /// <summary>
    /// 차량톤수 문자열을 ComboBox 인덱스로 변환
    /// </summary>
    private int Get차량톤수Index(string sCarWeight)
    {
        return sCarWeight switch
        {
            "1t" => 1,
            "1.4t" => 2,
            "1t화물" => 3,
            "1.4t화물" => 4,
            "2.5t" => 5,
            "3.5t" => 6,
            "5t" => 7,
            "8t" => 8,
            "11t" => 9,
            "14t" => 10,
            "15t" => 11,
            "18t" => 12,
            "25t" => 13,
            _ => 0
        };
    }

    /// <summary>
    /// 트럭상세 문자열을 ComboBox 인덱스로 변환
    /// </summary>
    private int Get트럭상세Index(string sTruckDetail)
    {
        return sTruckDetail switch
        {
            "카고/윙" => 1,
            "카고" => 2,
            "플러스카고" => 3,
            "축카고" => 4,
            "플축카고" => 5,
            "리프트카고" => 6,
            "플러스리" => 7,
            "플축리" => 8,
            "윙바디" => 9,
            "플러스윙" => 10,
            "축윙" => 11,
            "플축윙" => 12,
            "리프트윙" => 13,
            "플러스윙리" => 14,
            "플축윙리" => 15,
            "탑" => 16,
            "리프트탑" => 17,
            "호루" => 18,
            "리프트호루" => 19,
            "자바라" => 20,
            "리프트자바라" => 21,
            "냉동탑" => 22,
            "냉장탑" => 23,
            "냉동윙" => 24,
            "냉장윙" => 25,
            "냉동탑리" => 26,
            "냉장탑리" => 27,
            "냉동플축윙" => 28,
            "냉장플축윙" => 29,
            "냉동플축리" => 30,
            "냉장플축리" => 31,
            "평카" => 32,
            "로브이" => 33,
            "츄레라" => 34,
            "로베드" => 35,
            "사다리" => 36,
            "초장축" => 37,
            _ => 0
        };
    }

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
    private async Task<StdResult_Status> Select트럭ComboBoxItemAsync(
        IntPtr hWndParent, Draw.Point ptCheckAbs, IntPtr hWndBefore, int itemIndex, string sFieldName, int maxAttempts, CancelTokenControl ctrl)
    {
        try
        {
            Debug.WriteLine($"[{m_Context.AppName}] Select트럭ComboBoxItemAsync 진입: sFieldName={sFieldName}, itemIndex={itemIndex}, hWndBefore={hWndBefore:X}, ptCheckAbs={ptCheckAbs}");

            // 1. ComboBox 자동 열림 대기 (Handle 변경 감지)
            IntPtr hWndDropDown = IntPtr.Zero;
            for (int i = 0; i < maxAttempts; i++)
            {
                await ctrl.WaitIfPausedOrCancelledAsync();
                await Task.Delay(30, ctrl.Token);

                IntPtr hWndCurrent = Std32Window.GetWndHandle_FromAbsDrawPt(ptCheckAbs);

                if (i % 10 == 0) // 10번마다 로그
                {
                    Debug.WriteLine($"[{m_Context.AppName}] {sFieldName} ComboBox 대기 중... ({i + 1}번째 시도) - 현재 Handle: {hWndCurrent:X}");
                }

                if (hWndCurrent != hWndBefore && hWndCurrent != IntPtr.Zero)
                {
                    hWndDropDown = hWndCurrent;
                    Debug.WriteLine($"[{m_Context.AppName}] {sFieldName} ComboBox 열림 감지 ({i + 1}번째 시도) - 새 Handle: {hWndDropDown:X}");
                    break;
                }
            }

            if (hWndDropDown == IntPtr.Zero)
            {
                return new StdResult_Status(StdResult.Fail, $"{sFieldName} ComboBox 열림 실패", $"Select트럭ComboBoxItemAsync_{sFieldName}_01");
            }

            // 2. ComboBox 항목 선택
            await ctrl.WaitIfPausedOrCancelledAsync();

            Draw.Point ptItem = m_Context.FileInfo.접수등록Wnd_Common_ptComboBox[itemIndex];
            await Std32Mouse_Post.MousePostAsync_ClickLeft_ptRel(hWndDropDown, ptItem);

            Debug.WriteLine($"[{m_Context.AppName}] {sFieldName} ComboBox 항목 선택 완료: index={itemIndex}");

            await Task.Delay(100, ctrl.Token);

            return new StdResult_Status(StdResult.Success);
        }
        catch (Exception ex)
        {
            return new StdResult_Status(StdResult.Fail, StdUtil.GetExceptionMessage(ex), $"Select트럭ComboBoxItemAsync_{sFieldName}_999");
        }
    }

    /// <summary>
    /// 공통 함수 #5: 요금종류 RadioButton 현재 상태 확인
    /// - bmpOrg: 화면 캡처 비트맵
    /// - btns: RadioButton 그룹
    /// - sFeeType: 확인할 요금종류 ("선불", "착불", 등)
    /// </summary>
    private async Task<StdResult_NulBool> IsChecked요금종류Async(Draw.Bitmap bmpOrg, OfrModel_RadioBtns btns, string sFeeType)
    {
        int index = GetFeeTypeIndex(sFeeType);
        OfrModel_RadioBtn btn = btns.Btns[index];
        return await OfrWork_Insungs.OfrImgChkValue_RectInBitmapAsync(bmpOrg, btn.rcRelPartsT);
    }

    /// <summary>
    /// 배송타입 RadioButton 현재 상태 확인
    /// - bmpOrg: 화면 캡처 비트맵
    /// - btns: RadioButton 그룹
    /// - sDeliverType: 확인할 배송타입 ("편도", "왕복", "경유", "긴급")
    /// </summary>
    private async Task<StdResult_NulBool> IsChecked배송타입Async(Draw.Bitmap bmpOrg, OfrModel_RadioBtns btns, string sDeliverType)
    {
        int index = GetDeliverTypeIndex(sDeliverType);
        OfrModel_RadioBtn btn = btns.Btns[index];
        return await OfrWork_Insungs.OfrImgChkValue_RectInBitmapAsync(bmpOrg, btn.rcRelPartsT);
    }

    /// <summary>
    /// 차량종류 RadioButton 현재 상태 확인
    /// - bmpOrg: 화면 캡처 비트맵
    /// - btns: RadioButton 그룹
    /// - sCarType: 확인할 차량종류 ("오토", "밴", "트럭", 등)
    /// </summary>
    private async Task<StdResult_NulBool> IsChecked차량종류Async(Draw.Bitmap bmpOrg, OfrModel_RadioBtns btns, string sCarType)
    {
        int index = GetCarTypeIndex(sCarType);
        if (index == -1)
        {
            return new StdResult_NulBool(null, $"지원하지 않는 차량종류: {sCarType}", "IsChecked차량종류Async_01");
        }

        OfrModel_RadioBtn btn = btns.Btns[index];
        return await OfrWork_Insungs.OfrImgChkValue_RectInBitmapAsync(bmpOrg, btn.rcRelPartsT);
    }

    /// <summary>
    /// 공통 함수 #6: 요금종류 RadioButton 설정
    /// - bmpOrg: 화면 캡처 비트맵
    /// - btns: RadioButton 그룹
    /// - sFeeType: 설정할 요금종류
    /// - ctrl: CancelToken 제어
    /// </summary>
    private async Task<StdResult_Status> SetGroupFeeTypeAsync(Draw.Bitmap bmpOrg, OfrModel_RadioBtns btns, string sFeeType, CancelTokenControl ctrl)
    {
        int index = GetFeeTypeIndex(sFeeType);
        return await SetCheckRadioBtn_InGroupAsync(bmpOrg, btns, index, ctrl);
    }

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

    #endregion

    #region Tmp

    /// <summary>
    /// 공통 함수 #2: 고객 검색 및 선택 (의뢰자, 출발지, 도착지 공통)
    /// TODO: GetCustSearchTypeAsync, 고객검색Wnd 등 복잡한 로직 통합 필요
    /// </summary>
    //     private async Task<RegistResult> SearchAndSelectCustomerAsync(
    //         IntPtr hWnd고객명,
    //         IntPtr hWnd동명,
    //         string custName,
    //         string chargeName,
    //         string fieldName,
    //         CancelTokenControl ctrl)
    //     {
    //         // 1. 검색어 생성 (상호/담당)
    //         string searchText = NwCommon.GetInsungTextForSearch(custName, chargeName);

    //         // 2. 고객명 EditBox에 입력 (OfrWork_Common 사용 - 기존 로직과 동일)
    //         var resultBool = await OfrWork_Common.WriteEditBox_ToHndleAsync(hWnd고객명, searchText);
    //         if (resultBool == null || !resultBool.bResult)
    //             return RegistResult.ErrorResult($"{fieldName} 고객명 입력 실패");

    //         // 3. 검색 실행 및 결과 타입 확인 (GetCustSearchTypeAsync - Enter 키 포함)
    //         var searchResult = await GetCustSearchTypeAsync(hWnd고객명, hWnd동명);

    //         // 4. 검색 결과 처리
    //         switch (searchResult.resultTye)
    //         {
    //             case AutoAlloc_CustSearch.One:
    //                 // 1개 검색 성공 - 그대로 진행
    //                 Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 검색 성공 (1개)");
    //                 return RegistResult.SuccessResult();

    //             case AutoAlloc_CustSearch.Multi:
    //                 // TODO: 복수 고객 검색창 처리 필요
    //                 Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 복수 검색 결과 - TODO 처리 필요");
    //                 return RegistResult.ErrorResult($"{fieldName} 복수 검색됨 (TODO: 고객검색창 처리 필요)");

    //             case AutoAlloc_CustSearch.None:
    //                 // TODO: 신규 고객 등록창 처리 필요
    //                 Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 신규 고객 - TODO 처리 필요");
    //                 return RegistResult.ErrorResult($"{fieldName} 신규 고객 (TODO: 고객등록창 처리 필요)");

    //             case AutoAlloc_CustSearch.Null:
    //             default:
    //                 return RegistResult.ErrorResult($"{fieldName} 검색 타임아웃");
    //         }
    //     }

    /// <summary>
    /// 공통 함수 #3: CheckBox 설정 및 검증
    /// TODO: OFR 기반 체크박스 찾기 및 클릭 구현 필요
    /// </summary>
    //     private async Task<RegistResult> SetAndVerifyCheckBoxAsync(
    //         IntPtr hWndParent,
    //         string checkBoxName,
    //         bool shouldCheck,
    //         string fieldName,
    //         CancelTokenControl ctrl)
    //     {
    //         // TODO: OFR로 체크박스 이미지 찾아서 상태 확인 후 클릭
    //         Debug.WriteLine($"[{m_Context.AppName}] TODO: SetAndVerifyCheckBoxAsync 구현 필요 - {fieldName}");
    //         return RegistResult.SuccessResult(); // 임시
    //     }

    /// <summary>
    /// 공통 함수 #4: RadioButton 설정 및 검증
    /// TODO: OFR 기반 라디오버튼 찾기 및 클릭 구현 필요
    /// </summary>
    //     private async Task<RegistResult> SetAndVerifyRadioButtonAsync(
    //         IntPtr hWndParent,
    //         string radioGroupName,
    //         string radioValue,
    //         string fieldName,
    //         CancelTokenControl ctrl)
    //     {
    //         // TODO: OFR로 라디오버튼 그룹 찾아서 원하는 값 클릭
    //         Debug.WriteLine($"[{m_Context.AppName}] TODO: SetAndVerifyRadioButtonAsync 구현 필요 - {fieldName}={radioValue}");
    //         return RegistResult.SuccessResult(); // 임시
    //     }

    /// <summary>
    /// 공통 함수 #5: ComboBox 아이템 선택
    /// TODO: OFR 기반 콤보박스 열기 및 아이템 선택 구현 필요
    /// </summary>
    //     private async Task<RegistResult> SelectComboBoxItemAsync(
    //         IntPtr hWndParent,
    //         string comboBoxName,
    //         string itemValue,
    //         string fieldName,
    //         CancelTokenControl ctrl)
    //     {
    //         // TODO: OFR로 콤보박스 찾아서 열고 아이템 클릭
    //         Debug.WriteLine($"[{m_Context.AppName}] TODO: SelectComboBoxItemAsync 구현 필요 - {fieldName}={itemValue}");
    //         return RegistResult.SuccessResult(); // 임시
    //     }

    /// <summary>
    /// 공통 함수: 숫자 필드 입력 및 검증 (포커스 설정 + SetWindowText 시도 → 실패 시 키보드 폴백)
    /// - 0차: SetFocusWithForegroundAsync로 포커스 설정 (재시도 포함)
    /// - 1차: SetWindowText로 값 설정 시도
    /// - 2차: 실패 시 키보드 시뮬레이션 (HOME + DELETE + 숫자 입력)
    /// - Enter 키로 인성 앱의 합계 재계산 트리거
    /// </summary>

    /// <summary>
    /// Datagrid 컬럼 너비 조정 (드래그 방식)
    /// </summary>
    /// <param name="rcHeader">헤더 영역 Rectangle (MainWnd 기준 상대좌표)</param>
    /// <param name="ptDgTopLeft">Datagrid 좌상단 좌표 (절대좌표)</param>
    /// <param name="indexCol">조정할 컬럼 인덱스</param>
    /// <returns>에러 발생 시 StdResult_Error, 성공 시 null</returns>
    //     private StdResult_Error AdjustColumnWidth(
    //         Draw.Rectangle rcHeader,
    //         Draw.Point ptDgTopLeft,
    //         int indexCol)
    //     {
    //         Draw.Bitmap bmpHeader = null;

    //         try
    //         {
    //             // 1. 헤더 캡처
    //             bmpHeader = OfrService.CaptureScreenRect_InWndHandle(
    //                 m_Main.TopWnd_hWnd,
    //                 rcHeader
    //             );

    //             if (bmpHeader == null)
    //             {
    //                 return new StdResult_Error(
    //                     $"헤더 캡처 실패: indexCol={indexCol}",
    //                     "InsungsAct_RcptRegPage/AdjustColumnWidth_01");
    //             }

    //             // 2. 컬럼 경계 검출
    //             byte minBrightness = OfrService.GetMinBrightnessAtRow_FromColorBitmapFast(
    //                 bmpHeader, HEADER_GAB
    //             );
    //             minBrightness += 2; // 확실한 경계를 위해

    //             bool[] boolArr = OfrService.GetBoolArray_FromColorBitmapRowFast(
    //                 bmpHeader, HEADER_GAB, minBrightness, 2
    //             );

    //             List<OfrModel_LeftWidth> listLW =
    //                 OfrService.GetLeftWidthList_FromBool1Array(boolArr, minBrightness);

    //             if (listLW == null || indexCol >= listLW.Count)
    //             {
    //                 bmpHeader?.Dispose();
    //                 return new StdResult_Error(
    //                     $"컬럼 경계 검출 실패: indexCol={indexCol}, listLW.Count={listLW?.Count}",
    //                     "InsungsAct_RcptRegPage/AdjustColumnWidth_02");
    //             }

    //             // 3. 너비 차이 계산
    //             int actualWidth = listLW[indexCol].nWidth;
    //             int expectedWidth = m_ReceiptDgHeaderInfos[indexCol].nWidth;
    //             int dx = expectedWidth - actualWidth;

    //             if (dx == 0)
    //             {
    //                 Debug.WriteLine($"[AdjustColumnWidth] 컬럼[{indexCol}] 너비 조정 불필요: {actualWidth}px");
    //                 bmpHeader?.Dispose();
    //                 return null; // 이미 맞음
    //             }

    //             Debug.WriteLine($"[AdjustColumnWidth] 컬럼[{indexCol}] '{m_ReceiptDgHeaderInfos[indexCol].sName}' 너비 조정: {actualWidth} → {expectedWidth} (dx={dx})");

    //             // 4. 드래그로 너비 조정
    //             // ptStart: 컬럼 오른쪽 경계 (헤더 기준 상대좌표)
    //             Draw.Point ptStartRel = new Draw.Point(listLW[indexCol]._nRight + 1, HEADER_GAB);
    //             Draw.Point ptEndRel = new Draw.Point(ptStartRel.X + dx, ptStartRel.Y);

    //             // MainWnd 기준 상대좌표로 변환
    //             Draw.Point ptStartMainRel = new Draw.Point(
    //                 rcHeader.Left + ptStartRel.X,
    //                 rcHeader.Top + ptStartRel.Y
    //             );
    //             Draw.Point ptEndMainRel = new Draw.Point(
    //                 rcHeader.Left + ptEndRel.X,
    //                 rcHeader.Top + ptEndRel.Y
    //             );

    //             // 절대좌표로 변환
    //             Draw.Point ptStartAbs = new Draw.Point(
    //                 ptDgTopLeft.X + ptStartMainRel.X,
    //                 ptDgTopLeft.Y + ptStartMainRel.Y
    //             );
    //             Draw.Point ptEndAbs = new Draw.Point(
    //                 ptDgTopLeft.X + ptEndMainRel.X,
    //                 ptDgTopLeft.Y + ptEndMainRel.Y
    //             );

    //             // Drag 수행 (Simulation.cs:457-478 참고, 라이브러리 조합 방식)
    //             Std32Cursor.SetCursorPos_AbsDrawPt(ptStartAbs);
    //             Std32Mouse_Event.MouseEvent_LeftBtnDown();
    //             Std32Mouse_Send.MouseSet_MoveSmooth_ptAbs(ptStartAbs, ptEndAbs, 150);
    //             Std32Mouse_Event.MouseEvent_LeftBtnUp();

    //             bmpHeader?.Dispose();
    //             return null; // 성공
    //         }
    //         catch (Exception ex)
    //         {
    //             bmpHeader?.Dispose();
    //             return new StdResult_Error(
    //                 $"AdjustColumnWidth 예외: {ex.Message}",
    //                 "InsungsAct_RcptRegPage/AdjustColumnWidth_999");
    //         }
    //     }

    /// <summary>
    /// 텍스트 입력 (재시도 포함)
    /// </summary>
    //     private async Task<bool> InputTextAsync(IntPtr hWnd, string text)
    //     {
    //         const int MAX_RETRY = 3;
    //         const int WAIT_MS = 100;

    //         for (int i = 0; i < MAX_RETRY; i++)
    //         {
    //             await OfrWork_Common.WriteEditBox_ToHndleAsync(hWnd, text);

    //             string current = Std32Window.GetWindowCaption(hWnd);
    //             if (current == text)
    //                 return true;

    //             await Task.Delay(WAIT_MS);
    //         }

    //         Debug.WriteLine($"[{m_Context.AppName}] 텍스트 입력 실패: 원하는={text}, 현재={Std32Window.GetWindowCaption(hWnd)}");
    //         return false;
    //     }

    /// <summary>
    /// 전화번호 입력 (Enter 키 포함, 재시도)
    /// </summary>
    //     private async Task<bool> InputPhoneAsync(IntPtr hWnd, string phoneNo)
    //     {
    //         const int MAX_RETRY = 3;
    //         const int WAIT_MS = 100;

    //         for (int i = 0; i < MAX_RETRY; i++)
    //         {
    //             // 전화번호 입력
    //             Std32Window.SetWindowCaption(hWnd, phoneNo);
    //             await Task.Delay(WAIT_MS);

    //             // Enter 키 전송
    //             await Std32Key_Msg.KeyPostAsync_MouseClickNDown(hWnd, StdCommon32.VK_RETURN);
    //             await Task.Delay(WAIT_MS);

    //             string current = StdConvert.MakePhoneNumberToDigit(
    //                 Std32Window.GetWindowCaption(hWnd));

    //             if (current == phoneNo)
    //                 return true;

    //             await Task.Delay(WAIT_MS);
    //         }

    //         Debug.WriteLine($"[{m_Context.AppName}] 전화번호 입력 실패: 원하는={phoneNo}, 현재={StdConvert.MakePhoneNumberToDigit(Std32Window.GetWindowCaption(hWnd))}");
    //         return false;
    //     }

    /// <summary>
    /// 고객명 입력 (검색 포함)
    /// </summary>
    //     private async Task<CustomerResult> InputCustomerAsync(
    //         IntPtr hWndName, IntPtr hWndDong,
    //         string custName, string chargeName, string region)
    //     {
    //         // 고객명 입력
    //         string searchText = NwCommon.GetInsungTextForSearch(custName, chargeName);
    //         bool inputOk = await InputTextAsync(hWndName, searchText);

    //         if (!inputOk)
    //             return CustomerResult.ErrorResult($"{region} 고객명 입력 실패");

    //         // 검색 결과 확인
    //         var searchResult = await GetCustSearchTypeAsync(hWndName, hWndDong);

    //         switch (searchResult.resultTye)
    //         {
    //             case AutoAlloc_CustSearch.One:
    //                 // 1개 검색 → 성공
    //                 Debug.WriteLine($"[{m_Context.AppName}] {region} 고객 검색 성공: {custName}");
    //                 return CustomerResult.SuccessResult();

    //             case AutoAlloc_CustSearch.Multi:
    //                 // 여러 개 → 수동 처리 필요
    //                 Debug.WriteLine($"[{m_Context.AppName}] {region} 고객 복수 검색됨: {custName} (수동 처리 필요)");
    //                 return CustomerResult.ErrorResult($"{region} 고객 복수 검색됨 (수동 처리 필요)");

    //             case AutoAlloc_CustSearch.None:
    //                 // 없음 → 고객 등록 필요
    //                 Debug.WriteLine($"[{m_Context.AppName}] {region} 고객 없음: {custName} (등록 필요)");
    //                 return CustomerResult.ErrorResult($"{region} 고객 없음 (등록 필요)");

    //             default:
    //                 return CustomerResult.ErrorResult($"{region} 고객 검색 실패");
    //         }
    //     }

    /// <summary>
    /// 고객 검색 결과 타입 확인
    /// - 검색 결과가 1개인지, 복수인지, 없는지 확인
    /// </summary>
    //     private async Task<AutoAlloc_SearchTypeResult> GetCustSearchTypeAsync(IntPtr hWnd고객명, IntPtr hWnd동명)
    //     {
    //         IntPtr hWndTmp = IntPtr.Zero;
    //         string sTmp = "";

    //         // EnterKey 전송
    //         Std32Key_Msg.KeyPost_Down(hWnd고객명, StdCommon32.VK_RETURN);

    //         // 검색 결과 확인 (최대 50번, 1.5초)
    //         for (int j = 0; j < 50; j++)
    //         {
    //             await Task.Delay(30);

    //             // 1개 검색됨 - 동명에 텍스트가 들어옴
    //             sTmp = Std32Window.GetWindowCaption(hWnd동명);
    //             if (!string.IsNullOrEmpty(sTmp))
    //             {
    //                 return new AutoAlloc_SearchTypeResult(AutoAlloc_CustSearch.One, IntPtr.Zero);
    //             }

    //             // 신규 고객 - 고객등록창이 뜸
    //             hWndTmp = Std32Window.FindMainWindow(m_Context.MemInfo.Splash.TopWnd_uProcessId, null,
    //                 m_Context.FileInfo.고객등록Wnd_TopWnd_sWndName);
    //             if (hWndTmp != IntPtr.Zero)
    //             {
    //                 return new AutoAlloc_SearchTypeResult(AutoAlloc_CustSearch.None, hWndTmp);
    //             }

    //             // 복수 고객 - 고객검색창이 뜸
    //             hWndTmp = Std32Window.FindMainWindow(m_Context.MemInfo.Splash.TopWnd_uProcessId, null,
    //                 m_Context.FileInfo.고객검색Wnd_TopWnd_sWndName);
    //             if (hWndTmp != IntPtr.Zero)
    //             {
    //                 // 고객검색창이 떴을 때, 단수 고객이면 자동으로 닫힘
    //                 // 1.5초 동안 기다리면서 창이 닫히는지 확인
    //                 for (int k = 0; k < 30; k++)
    //                 {
    //                     hWndTmp = Std32Window.FindMainWindow(m_Context.MemInfo.Splash.TopWnd_uProcessId, null,
    //                         m_Context.FileInfo.고객검색Wnd_TopWnd_sWndName);
    //                     if (hWndTmp == IntPtr.Zero) break; // 창이 닫힘 → 단수 고객

    //                     await Task.Delay(50);
    //                 }

    //                 // 30번 반복 후에도 창이 살아있으면 복수 고객
    //                 if (hWndTmp != IntPtr.Zero)
    //                 {
    //                     return new AutoAlloc_SearchTypeResult(AutoAlloc_CustSearch.Multi, hWndTmp);
    //                 }
    //                 // 창이 닫혔으면 계속 루프 (동명 확인으로)
    //             }
    //         }

    //         // 검색 실패
    //         Debug.WriteLine($"[{m_Context.AppName}] GetCustSearchTypeAsync 실패: 타임아웃");
    //         return new AutoAlloc_SearchTypeResult(AutoAlloc_CustSearch.Null, IntPtr.Zero);
    //     }
    /// <summary>
    /// 테스트: Selection 있는 상태 vs 없는 상태 OFR 비교
    /// 목적: EmptyRow 클릭 없이도 OFR이 정상 작동하는지 확인
    /// </summary>
    //public async Task<StdResult_String> Test_CompareOFR_WithAndWithoutSelectionAsync()
    //{
    //    try
    //    {
    //        Debug.WriteLine("[Test_CompareOFR] ===== 테스트 시작 =====");

    //        // 1. 현재 상태 그대로 캡처 (Selection 있을 수 있음)
    //        Debug.WriteLine("[Test_CompareOFR] 1. 현재 상태 캡처 (Selection 있을 수 있음)");
    //        Draw.Bitmap bmpWithSelection = OfrService.CaptureScreenRect_InWndHandle(m_RcptPage.DG오더_hWnd);
    //        if (bmpWithSelection == null)
    //        {
    //            return new StdResult_String("캡처 실패 (Selection 있는 상태)", "Test_CompareOFR/01");
    //        }

    //        // 2. 현재 상태에서 밝기 측정
    //        Draw.Rectangle[,] rects = m_RcptPage.DG오더_RelChildRects;
    //        if (rects == null || rects.GetLength(1) < 3)
    //        {
    //            return new StdResult_String("RelChildRects 없음", "Test_CompareOFR/02");
    //        }

    //        // 첫 번째 데이터 행의 밝기 측정 (y=2, 헤더는 0,1)
    //        int x = rects[0, 2].Left + 5;
    //        int y = rects[0, 2].Top + 6;
    //        int brightness_WithSel = OfrService.GetBrightness_PerPixel(bmpWithSelection, x, y);

    //        Debug.WriteLine($"[Test_CompareOFR] 2. Selection 있는 상태 밝기: {brightness_WithSel}");
    //        Debug.WriteLine($"[Test_CompareOFR]    배경 밝기 기준: {m_RcptPage.DG오더_nBackgroundBright}");

    //        // 3. 첫 번째 행 클릭 (Selection 이동)
    //        Debug.WriteLine("[Test_CompareOFR] 3. 첫 번째 행 클릭 (Selection 이동)");
    //        Draw.Point ptFirstRow = StdUtil.GetDrawPoint(rects[0, 2], 3, 3);
    //        await Simulation_Mouse.SafeMouseEvent_ClickLeft_ptRelAsync(m_RcptPage.DG오더_hWnd, ptFirstRow, true, 100);
    //        await Task.Delay(200);

    //        // 4. 첫 번째 행 선택 상태에서 캡처
    //        Debug.WriteLine("[Test_CompareOFR] 4. 첫 번째 행 선택 상태 캡처");
    //        Draw.Bitmap bmpFirstRowSel = OfrService.CaptureScreenRect_InWndHandle(m_RcptPage.DG오더_hWnd);
    //        if (bmpFirstRowSel == null)
    //        {
    //            return new StdResult_String("캡처 실패 (첫 행 선택)", "Test_CompareOFR/03");
    //        }

    //        int brightness_FirstRowSel = OfrService.GetBrightness_PerPixel(bmpFirstRowSel, x, y);
    //        Debug.WriteLine($"[Test_CompareOFR]    첫 행 선택 밝기: {brightness_FirstRowSel}");

    //        // 5. 두 번째 행의 밝기 비교 (선택되지 않은 행)
    //        int x2 = rects[0, 3].Left + 5;
    //        int y2 = rects[0, 3].Top + 6;
    //        int brightness_SecondRow = OfrService.GetBrightness_PerPixel(bmpFirstRowSel, x2, y2);
    //        Debug.WriteLine($"[Test_CompareOFR]    두 번째 행 밝기 (선택 안됨): {brightness_SecondRow}");

    //        // 6. 결과 분석
    //        string result = $"===== OFR Selection 테스트 결과 =====\n";
    //        result += $"배경 밝기 기준: {m_RcptPage.DG오더_nBackgroundBright}\n";
    //        result += $"현재 상태 밝기: {brightness_WithSel}\n";
    //        result += $"첫 행 선택 밝기: {brightness_FirstRowSel}\n";
    //        result += $"두 번째 행 밝기: {brightness_SecondRow}\n";
    //        result += $"\n";
    //        result += $"선택된 행 vs 배경: {Math.Abs(brightness_FirstRowSel - m_RcptPage.DG오더_nBackgroundBright)}\n";
    //        result += $"선택 안된 행 vs 배경: {Math.Abs(brightness_SecondRow - m_RcptPage.DG오더_nBackgroundBright)}\n";
    //        result += $"\n";

    //        // 7. 결론
    //        int threshold = 10; // 밝기 차이 임계값
    //        bool isFirstRowSelected = Math.Abs(brightness_FirstRowSel - m_RcptPage.DG오더_nBackgroundBright) > threshold;
    //        bool isSecondRowNormal = Math.Abs(brightness_SecondRow - m_RcptPage.DG오더_nBackgroundBright) < threshold;

    //        if (isFirstRowSelected && isSecondRowNormal)
    //        {
    //            result += "✅ 결론: 선택된 행만 밝기가 다릅니다.\n";
    //            result += "   → OFR 시 선택되지 않은 행들은 정상 인식 가능!\n";
    //            result += "   → EmptyRow 클릭 불필요 (첫 행 선택으로 대체 가능)\n";
    //        }
    //        else if (!isFirstRowSelected && !isSecondRowNormal)
    //        {
    //            result += "⚠️ 경고: 선택 여부와 무관하게 밝기 차이가 없습니다.\n";
    //            result += "   → 추가 테스트 필요\n";
    //        }
    //        else
    //        {
    //            result += "❌ 문제: 예상과 다른 밝기 패턴입니다.\n";
    //            result += "   → 수동 확인 필요\n";
    //        }

    //        Debug.WriteLine($"[Test_CompareOFR] {result}");
    //        Debug.WriteLine("[Test_CompareOFR] ===== 테스트 종료 =====");

    //        // Bitmap 해제
    //        bmpWithSelection?.Dispose();
    //        bmpFirstRowSel?.Dispose();

    //        return new StdResult_String(result);
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.WriteLine($"[Test_CompareOFR] 예외 발생: {ex.Message}");
    //        return new StdResult_String(ex.Message, "Test_CompareOFR/999");
    //    }
    //}

    /// <summary>
    /// Datagrid 로딩 완료 대기 (Pan 상태 변화 감지)
    /// </summary>
    /// <param name="hWndDG">Datagrid 윈도우 핸들</param>
    /// <param name="Elpase">최대 대기 시간 (밀리초, 기본 500ms)</param>
    /// <returns>성공: Success, 시간 초과: Fail, Pan 없음: Skip</returns>
    //     private async Task<StdResult_Status> WaitPanLoadedAsync(IntPtr hWndDG, int Elpase = 500)
    //     {
    //         IntPtr hWndFind = IntPtr.Zero;

    //         // 1. Pan이 나타날 때까지 대기 (최대 100ms)
    //         for (int i = 0; i < 100; i++)
    //         {
    //             hWndFind = Std32Window.GetWndHandle_FromRelDrawPt(hWndDG, m_FileInfo.접수등록Page_DG오더_ptChkRelPanL);
    //             if (hWndFind != hWndDG) break;  // Pan이 나타남
    //             await Task.Delay(1);
    //         }

    //         if (hWndFind == hWndDG)  // Pan이 안 나타남 (이미 로딩 완료)
    //             return new StdResult_Status(StdResult.Skip);

    //         // 2. Pan이 사라질 때까지 대기 (로딩 완료)
    //         for (int i = 0; i < Elpase; i++)
    //         {
    //             hWndFind = Std32Window.GetWndHandle_FromRelDrawPt(hWndDG, m_FileInfo.접수등록Page_DG오더_ptChkRelPanL);
    //             if (hWndFind == hWndDG) break;  // Pan이 사라짐 (로딩 완료)
    //             await Task.Delay(100);
    //         }

    //         if (hWndFind != hWndDG)
    //         {
    //             Debug.WriteLine($"[{m_Context.AppName}/RcptRegPage] WaitPanLoadedAsync 시간 초과");
    //             return CommonFuncs_StdResult.ErrMsgResult_Status(StdResult.Fail,
    //                 "Datagrid 로딩 대기 시간 초과",
    //                 $"{m_Context.AppName}/RcptRegPage/WaitPanLoadedAsync_01");
    //         }

    //         return new StdResult_Status(StdResult.Success);
    //     }

    // TODO: Simulation_Mouse 메서드 구현 후 주석 해제
    ///// <summary>
    ///// 조회 버튼 클릭 후 총계 읽기
    ///// </summary>
    ///// <param name="ctrl">취소 토큰 컨트롤</param>
    ///// <returns>총계 문자열 (실패 시 빈 문자열 또는 null)</returns>
    //public async Task<StdResult_String> Click조회버튼Async(CancelTokenControl ctrl)
    //{
    //    try
    //    {
    //        string str = "";
    //        StdResult_Status resultSts = null;

    //        // 조회 버튼 클릭 후 총계 읽기 반복 시도
    //        for (int i = 0; i < CommonVars.c_nRepeatShort; i++)
    //        {
    //            await ctrl.WaitIfPausedOrCancelledAsync();

    //            // 1. 조회 버튼 클릭
    //            Simulation_Mouse.SafeMousePost_ClickLeft(m_RcptPage.CmdBtn_hWnd조회);

    //            // 2. Datagrid 로딩 대기
    //            resultSts = await WaitPanLoadedAsync(m_RcptPage.DG오더_hWnd);
    //            if (resultSts.Result == StdResult.Fail) continue;

    //            // 3. 총계 읽기
    //            str = Std32Window.GetWindowCaption(m_RcptPage.CallCount_hWnd총계);
    //            if (!string.IsNullOrEmpty(str))
    //            {
    //                Debug.WriteLine($"[{m_Context.AppName}/RcptRegPage] 조회 버튼 클릭 완료, 총계: {str}");
    //                break;
    //            }

    //            await Task.Delay(CommonVars.c_nWaitNormal, ctrl.Token);
    //        }

    //        return new StdResult_String(str);
    //    }
    //    catch (Exception ex)
    //    {
    //        return new StdResult_String(StdUtil.GetExceptionMessage(ex),
    //            $"{m_Context.AppName}/RcptRegPage/Click조회버튼Async_999");
    //    }
    //}

    ///// <summary>
    ///// Empty Row 클릭 (선택 해제용)
    ///// </summary>
    ///// <param name="ctrl">취소 토큰 컨트롤</param>
    ///// <returns>클릭 성공 여부</returns>
    //public async Task<bool> ClickEmptyRowAsync(CancelTokenControl ctrl)
    //{
    //    bool bClicked = false;

    //    try
    //    {
    //        // Empty Row는 [0, 1] 셀 (첫 번째 컬럼, 두 번째 행)
    //        Draw.Point ptRel = StdUtil.GetDrawPoint(m_RcptPage.DG오더_RelChildRects[0, 1], 3, 3);

    //        for (int i = 0; i < CommonVars.c_nRepeatShort; i++)
    //        {
    //            await ctrl.WaitIfPausedOrCancelledAsync();

    //            // 밝기 변화 감지로 클릭 확인
    //            bClicked = await Simulation_Mouse
    //                .SafeMousePost_ClickLeft_ptRel_WaitBrightChange(
    //                    m_RcptPage.DG오더_hWnd,
    //                    ptRel,
    //                    ptRel,
    //                    m_RcptPage.DG오더_nBackgroundBright);

    //            if (bClicked)
    //            {
    //                Debug.WriteLine($"[{m_Context.AppName}/RcptRegPage] Empty Row 클릭 완료");
    //                break;
    //            }

    //            await Task.Delay(100, ctrl.Token);
    //        }

    //        return bClicked;
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.WriteLine($"[{m_Context.AppName}/RcptRegPage] ClickEmptyRowAsync 예외: {ex.Message}");
    //        return false;
    //    }
    //}
    #endregion
}
#nullable enable
