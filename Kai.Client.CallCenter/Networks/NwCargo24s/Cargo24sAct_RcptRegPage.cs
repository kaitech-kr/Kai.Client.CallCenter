using System.Diagnostics;
using System.Linq;
using System.Windows.Media;
using Draw = System.Drawing;

using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;
using Kai.Common.NetDll_WpfCtrl.NetOFR;
using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgBox;

using Kai.Client.CallCenter.Classes;
using Kai.Client.CallCenter.Classes.Class_Master;
using Kai.Client.CallCenter.Windows;
using Kai.Client.CallCenter.OfrWorks;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Services;
using static Kai.Client.CallCenter.Classes.CommonVars;
using Microsoft.VisualBasic;

namespace Kai.Client.CallCenter.Networks.NwCargo24s;
#nullable disable

/// <summary>
/// 화물24시 접수등록 페이지 초기화 및 제어 담당 클래스
/// Context 패턴 사용: Cargo24Context를 통해 모든 정보에 접근
/// </summary>
public class Cargo24sAct_RcptRegPage
{
    #region Datagrid Column Header Info
    /// <summary>
    /// Datagrid 컬럼 헤더 정보 배열 (22개)
    /// </summary>
    public readonly NwCommon_DgColumnHeader[] m_ReceiptDgHeaderInfos = new NwCommon_DgColumnHeader[]
    {
        new NwCommon_DgColumnHeader() { sName = "0", bOfrSeq = true, nWidth = 0 },
        new NwCommon_DgColumnHeader() { sName = "상태", bOfrSeq = false, nWidth = 40 },
        new NwCommon_DgColumnHeader() { sName = "화물번호", bOfrSeq = true, nWidth = 80 },
        new NwCommon_DgColumnHeader() { sName = "처리시간", bOfrSeq = true, nWidth = 70 },
        new NwCommon_DgColumnHeader() { sName = "고객명", bOfrSeq = false, nWidth = 120 },
        new NwCommon_DgColumnHeader() { sName = "고객전화", bOfrSeq = true, nWidth = 90 },
        new NwCommon_DgColumnHeader() { sName = "차주전화", bOfrSeq = true, nWidth = 90 },
        new NwCommon_DgColumnHeader() { sName = "상차지", bOfrSeq = false, nWidth = 100 },
        new NwCommon_DgColumnHeader() { sName = "하차지", bOfrSeq = false, nWidth = 100 },
        new NwCommon_DgColumnHeader() { sName = "운송료", bOfrSeq = true, nWidth = 50 },
        new NwCommon_DgColumnHeader() { sName = "수수료", bOfrSeq = true, nWidth = 50 },
        new NwCommon_DgColumnHeader() { sName = "공유", bOfrSeq = false, nWidth = 40 },
        new NwCommon_DgColumnHeader() { sName = "SMS", bOfrSeq = false, nWidth = 40 },
        new NwCommon_DgColumnHeader() { sName = "혼적", bOfrSeq = false, nWidth = 40 },
        new NwCommon_DgColumnHeader() { sName = "요금구분", bOfrSeq = false, nWidth = 60 },
        new NwCommon_DgColumnHeader() { sName = "계산서금액", bOfrSeq = true, nWidth = 70 },
        new NwCommon_DgColumnHeader() { sName = "차량톤수", bOfrSeq = true, nWidth = 60 },
        new NwCommon_DgColumnHeader() { sName = "톤수", bOfrSeq = false, nWidth = 50 },
        new NwCommon_DgColumnHeader() { sName = "차종", bOfrSeq = false, nWidth = 60 },
        new NwCommon_DgColumnHeader() { sName = "적재옵션", bOfrSeq = false, nWidth = 100 },
        new NwCommon_DgColumnHeader() { sName = "차량종류", bOfrSeq = false, nWidth = 60 },
        new NwCommon_DgColumnHeader() { sName = "화물정보", bOfrSeq = false, nWidth = 150 },
    };

    #region Constants
    /// <summary>
    /// 컬럼 너비 허용 오차 (픽셀)
    /// </summary>
    private const int COLUMN_WIDTH_TOLERANCE = 1;

    // 헤더/캡처 관련
    private const int HEADER_HEIGHT = 30;
    private const int TARGET_ROW = 2;
    private const int HEADER_GAB = 7;
    private const int OFR_HEIGHT = 20;
    private const int MIN_COLUMN_WIDTH = 30;
    private const int BRIGHTNESS_OFFSET = 2;

    // 재시도/대기 관련
    private const int MAX_RETRY = 3;
    private const int DELAY_AFTER_INIT = 1000;
    private const int DELAY_AFTER_DRAG = 150;
    private const int DELAY_RETRY = 500;
    private const int DELAY_DIALOG_CHECK = 50;

    // Step 2 특수 컬럼 처리
    private const int SPECIAL_COL_START = 24;
    private const int SPECIAL_COL_END = 26;
    private const int SPECIAL_COL_OFFSET = 30;
    #endregion
    #endregion

    #region Context Reference
    /// <summary>
    /// Context에 대한 읽기 전용 참조
    /// </summary>
    private readonly Cargo24Context m_Context;

    /// <summary>
    /// 편의를 위한 로컬 참조들
    /// </summary>
    private Cargo24sInfo_File m_FileInfo => m_Context.FileInfo;
    private Cargo24sInfo_Mem m_MemInfo => m_Context.MemInfo;
    private Cargo24sInfo_Mem.MainWnd m_Main => m_MemInfo.Main;
    private Cargo24sInfo_Mem.SplashWnd m_Splash => m_MemInfo.Splash;
    private Cargo24sInfo_Mem.RcptRegPage m_RcptPage => m_MemInfo.RcptPage;
    #endregion

    #region Constructor
    /// <summary>
    /// 생성자 - Context를 받아서 초기화
    /// </summary>
    public Cargo24sAct_RcptRegPage(Cargo24Context context)
    {
        m_Context = context ?? throw new ArgumentNullException(nameof(context));
        //Debug.WriteLine($"[Cargo24sAct_RcptRegPage] 생성자 호출: AppName={m_Context.AppName}");
    }
    #endregion

    #region RcptRegPage Initialize
    /// <summary>
    /// 접수등록 페이지 초기화
    /// Cargo24는 로그인 후 자동으로 접수등록Page를 열므로 바메뉴 클릭 불필요
    /// 1. 팝업 처리 (안내문 - "오늘하루동안 감추기" 클릭) - 메인윈도우 기준
    /// 2. StatusBtn 찾기 (접수/운행/취소/완료/정산/전체)
    /// 3. CmdBtn 찾기 (신규/조회)
    /// 4. Datagrid 찾기 및 초기화
    /// </summary>
    public async Task<StdResult_Error> InitializeAsync(bool bEdit = true, bool bWrite = true, bool bMsgBox = true)
    {
        IntPtr hWndMain = m_Main.TopWnd_hWnd; // 메인윈도우 기준으로 모든 UI 요소 찾기
        IntPtr hWndTmp = IntPtr.Zero;
        string sTmp = string.Empty;

        try
        {
            Debug.WriteLine($"[Cargo24sAct_RcptRegPage] 초기화 시작");

            #region 오늘하루동안감추기 처리
            // 메인윈도우 기준으로 "오늘하루동안 감추기" 버튼 찾기
            hWndTmp = Std32Window.GetWndHandle_FromRelDrawPt(hWndMain, m_FileInfo.접수등록Page_안내문_ptChkRel오늘하루동안감추기);
            if (hWndTmp == IntPtr.Zero)
            {
                Debug.WriteLine($"[Cargo24sAct_RcptRegPage] 오늘하루동안감추기 버튼 없음 - 스킵 (정상)");
            }
            else
            {
                // 버튼 텍스트 확인
                sTmp = Std32Window.GetWindowCaption(hWndTmp);
                //Debug.WriteLine($"[Cargo24sAct_RcptRegPage] 버튼 찾음: {hWndTmp:X}, 텍스트={sTmp}");

                if (sTmp == m_FileInfo.접수등록Page_안내문_sWndName오늘하루동안감추기)
                {
                    Debug.WriteLine($"[Cargo24sAct_RcptRegPage] 버튼 클릭 시도");

                    // 클릭
                    await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndTmp);

                    // 창 닫힘 대기 (최대 5초)
                    for (int i = 0; i < c_nRepeatMany; i++)
                    {
                        if (!StdWin32.IsWindow(hWndTmp)) break;
                        await Task.Delay(c_nWaitShort);
                    }

                    await Task.Delay(c_nWaitNormal);
                    Debug.WriteLine($"[Cargo24sAct_RcptRegPage] 팝업 처리 완료");
                }
            }
            #endregion

            #region StatusBtn 찾기 (접수/운행/취소/완료/정산/전체)
            //Debug.WriteLine($"[Cargo24sAct_RcptRegPage] StatusBtn 찾기 시작");

            // 접수 버튼
            var (hWnd접수, err접수) = await FindStatusButtonAsync(m_FileInfo.접수등록Page_StatusBtn_sWndName접수, m_FileInfo.접수등록Page_StatusBtn_ptChkRel접수, "Cargo24sAct_RcptRegPage/InitializeAsync_01", bWrite, bMsgBox);
            if (err접수 != null) return err접수;
            m_RcptPage.StatusBtn_hWnd접수 = hWnd접수;

            // 운행 버튼
            var (hWnd운행, err운행) = await FindStatusButtonAsync(m_FileInfo.접수등록Page_StatusBtn_sWndName운행, m_FileInfo.접수등록Page_StatusBtn_ptChkRel운행, "Cargo24sAct_RcptRegPage/InitializeAsync_02", bWrite, bMsgBox);
            if (err운행 != null) return err운행;
            m_RcptPage.StatusBtn_hWnd운행 = hWnd운행;

            // 취소 버튼
            var (hWnd취소, err취소) = await FindStatusButtonAsync(m_FileInfo.접수등록Page_StatusBtn_sWndName취소, m_FileInfo.접수등록Page_StatusBtn_ptChkRel취소, "Cargo24sAct_RcptRegPage/InitializeAsync_03", bWrite, bMsgBox);
            if (err취소 != null) return err취소;
            m_RcptPage.StatusBtn_hWnd취소 = hWnd취소;

            // 완료 버튼
            var (hWnd완료, err완료) = await FindStatusButtonAsync(m_FileInfo.접수등록Page_StatusBtn_sWndName완료, m_FileInfo.접수등록Page_StatusBtn_ptChkRel완료, "Cargo24sAct_RcptRegPage/InitializeAsync_04", bWrite, bMsgBox);
            if (err완료 != null) return err완료;
            m_RcptPage.StatusBtn_hWnd완료 = hWnd완료;

            // 정산 버튼
            var (hWnd정산, err정산) = await FindStatusButtonAsync(m_FileInfo.접수등록Page_StatusBtn_sWndName정산, m_FileInfo.접수등록Page_StatusBtn_ptChkRel정산, "Cargo24sAct_RcptRegPage/InitializeAsync_05", bWrite, bMsgBox);
            if (err정산 != null) return err정산;
            m_RcptPage.StatusBtn_hWnd정산 = hWnd정산;

            // 전체 버튼
            var (hWnd전체, err전체) = await FindStatusButtonAsync(m_FileInfo.접수등록Page_StatusBtn_sWndName전체, m_FileInfo.접수등록Page_StatusBtn_ptChkRel전체, "Cargo24sAct_RcptRegPage/InitializeAsync_06", bWrite, bMsgBox);
            if (err전체 != null) return err전체;
            m_RcptPage.StatusBtn_hWnd전체 = hWnd전체;

            Debug.WriteLine($"[Cargo24sAct_RcptRegPage] StatusBtn 찾기 완료");
            #endregion

            #region CmdBtn 찾기 (신규/조회)
            Debug.WriteLine($"[Cargo24sAct_RcptRegPage] CmdBtn 찾기 시작");

            // 신규 버튼
            var (hWnd신규, err신규) = await FindStatusButtonAsync(m_FileInfo.접수등록Page_CmdBtn_sWndName신규, m_FileInfo.접수등록Page_CmdBtn_ptChkRel신규M, "Cargo24sAct_RcptRegPage/InitializeAsync_07", bWrite, bMsgBox);
            if (err신규 != null) return err신규;
            m_RcptPage.CmdBtn_hWnd신규 = hWnd신규;

            // 조회 버튼
            var (hWnd조회, err조회) = await FindStatusButtonAsync(m_FileInfo.접수등록Page_CmdBtn_sWndName조회, m_FileInfo.접수등록Page_CmdBtn_ptChkRel조회M, "Cargo24sAct_RcptRegPage/InitializeAsync_08", bWrite, bMsgBox);
            if (err조회 != null) return err조회;
            m_RcptPage.CmdBtn_hWnd조회 = hWnd조회;

            Debug.WriteLine($"[Cargo24sAct_RcptRegPage] CmdBtn 찾기 완료");
            #endregion

            #region 리스트항목 버튼 찾기 (순서저장/원래대로)
            Debug.WriteLine($"[Cargo24sAct_RcptRegPage] 리스트항목 버튼 찾기 시작");

            // 순서저장 버튼
            var (hWnd순서저장, err순서저장) = await FindStatusButtonAsync(m_FileInfo.접수등록Page_리스트항목_sWndName순서저장, m_FileInfo.접수등록Page_리스트항목_ptChkRel순서저장, "Cargo24sAct_RcptRegPage/InitializeAsync_09", bWrite, bMsgBox);
            if (err순서저장 != null) return err순서저장;
            m_RcptPage.리스트항목_hWnd순서저장 = hWnd순서저장;

            // 원래대로 버튼
            var (hWnd원래대로, err원래대로) = await FindStatusButtonAsync(m_FileInfo.접수등록Page_리스트항목_sWndName원래대로, m_FileInfo.접수등록Page_리스트항목_ptChkRel원래대로, "Cargo24sAct_RcptRegPage/InitializeAsync_10", bWrite, bMsgBox);
            if (err원래대로 != null) return err원래대로;
            m_RcptPage.리스트항목_hWnd원래대로 = hWnd원래대로;

            Debug.WriteLine($"[Cargo24sAct_RcptRegPage] 리스트항목 버튼 찾기 완료");
            #endregion

            #region Datagrid 찾기 및 초기화
            Debug.WriteLine($"[Cargo24sAct_RcptRegPage] Datagrid 초기화 시작");

            var resultErr = await SetDG오더RectsAsync(bEdit, bWrite, bMsgBox);
            if (resultErr != null) return resultErr;

            Debug.WriteLine($"[Cargo24sAct_RcptRegPage] Datagrid 초기화 완료");
            #endregion

            Debug.WriteLine($"[Cargo24sAct_RcptRegPage] 초기화 완료");
            return null; // 성공
        }
        catch (Exception ex)
        {
            return CommonFuncs_StdResult.ErrMsgResult_Error(
                $"[{m_Context.AppName}/RcptRegPage]예외발생: {ex.Message}",
                "Cargo24sAct_RcptRegPage/InitializeAsync_999", bWrite, bMsgBox);
        }
    }
    #endregion

    #region Datagrid Methods
    /// <summary>
    /// Datagrid 초기화 및 Cell 좌표 계산
    /// Step 3: Datagrid 핸들 찾기 및 기본 검증
    /// Step 4-1, 4-2: Datagrid 캡처 및 헤더 최소 밝기 검출
    /// </summary>
    public async Task<StdResult_Error> SetDG오더RectsAsync(bool bEdit = true, bool bWrite = true, bool bMsgBox = true)
    {
        IntPtr hWndMain = m_Main.TopWnd_hWnd;
        string sTmp = string.Empty;
        Draw.Bitmap bmpDG = null;

        try
        {
            Debug.WriteLine($"[Cargo24sAct_RcptRegPage] SetDG오더RectsAsync 시작");

            // Datagrid 핸들 찾기
            m_RcptPage.DG오더_hWnd = Std32Window.GetWndHandle_FromRelDrawPt(hWndMain, m_FileInfo.접수등록Page_DG오더_ptChkRel);
            if (m_RcptPage.DG오더_hWnd == IntPtr.Zero)
            {
                return CommonFuncs_StdResult.ErrMsgResult_Error($"[{m_Context.AppName}/SetDG오더]Datagrid 핸들 못찾음", "Cargo24sAct_RcptRegPage/SetDG오더RectsAsync_01", bWrite, bMsgBox);
            }
            Debug.WriteLine($"[Cargo24sAct_RcptRegPage] Datagrid 핸들 찾음: {m_RcptPage.DG오더_hWnd:X}");

            // ClassName 검증
            sTmp = Std32Window.GetWindowClassName(m_RcptPage.DG오더_hWnd);
            if (sTmp != m_FileInfo.접수등록Page_DG오더_sClassName)
            {
                return CommonFuncs_StdResult.ErrMsgResult_Error($"[{m_Context.AppName}/SetDG오더]Datagrid 클래스명 불일치: {sTmp} != {m_FileInfo.접수등록Page_DG오더_sClassName}", "Cargo24sAct_RcptRegPage/SetDG오더RectsAsync_02", bWrite, bMsgBox);
            }
            Debug.WriteLine($"[Cargo24sAct_RcptRegPage] Datagrid ClassName 검증 완료: {sTmp}");

            // AbsRect 계산
            m_RcptPage.DG오더_AbsRect = Std32Window.GetWindowRect_DrawAbs(m_RcptPage.DG오더_hWnd);
            Debug.WriteLine($"[Cargo24sAct_RcptRegPage] Datagrid AbsRect: {m_RcptPage.DG오더_AbsRect}");

            // 평가 및 재시도 루프
            for (int retry = 1; retry <= MAX_RETRY; retry++)
            {
                // Step 4-1: 헤더 영역만 캡처
                Draw.Rectangle rcHeader = new Draw.Rectangle(0, 0, m_RcptPage.DG오더_AbsRect.Width, HEADER_HEIGHT);
                bmpDG = OfrService.CaptureScreenRect_InWndHandle(m_RcptPage.DG오더_hWnd, rcHeader);

                if (bmpDG == null)
                {
                    return CommonFuncs_StdResult.ErrMsgResult_Error($"[{m_Context.AppName}/SetDG오더]헤더 영역 캡처 실패", "Cargo24sAct_RcptRegPage/SetDG오더RectsAsync_03", bWrite, bMsgBox);
                }
                Debug.WriteLine($"[Cargo24/SetDG오더] 헤더 영역 캡처 성공: {bmpDG.Width}x{bmpDG.Height}");

                // Step 4-2: 헤더에서 최대 밝기 검출 (배경 밝기)
                byte maxBrightness = OfrService.GetMaxBrightnessAtRow_FromColorBitmapFast(bmpDG, TARGET_ROW);
                if (maxBrightness == 0)
                {
                    bmpDG?.Dispose();
                    return CommonFuncs_StdResult.ErrMsgResult_Error($"[{m_Context.AppName}/SetDG오더]헤더 행 최대 밝기 검출 실패: targetRow={TARGET_ROW}", "Cargo24sAct_RcptRegPage/SetDG오더RectsAsync_04", bWrite, bMsgBox);
                }

                maxBrightness -= BRIGHTNESS_OFFSET; // 배경보다 약간 어두운 것을 경계로 인식

                // Step 4-3: Bool 배열 생성 및 컬럼 경계 검출
                bool[] boolArr = OfrService.GetBoolArray_FromColorBitmapRowFast(bmpDG, TARGET_ROW, maxBrightness, 2);
                if (boolArr == null || boolArr.Length == 0)
                {
                    bmpDG?.Dispose();
                    return CommonFuncs_StdResult.ErrMsgResult_Error($"[{m_Context.AppName}/SetDG오더]Bool 배열 생성 실패: targetRow={TARGET_ROW}", "Cargo24sAct_RcptRegPage/SetDG오더RectsAsync_05", bWrite, bMsgBox);
                }

                // 4-3-2. 컬럼 경계 리스트 추출
                List<OfrModel_LeftWidth> listLW = OfrService.GetLeftWidthList_FromBool1Array(boolArr, maxBrightness);
                if (listLW == null || listLW.Count == 0)
                {
                    bmpDG?.Dispose();
                    return CommonFuncs_StdResult.ErrMsgResult_Error($"[{m_Context.AppName}/SetDG오더]컬럼 경계 검출 실패: 검출된 리스트 수=0", "Cargo24sAct_RcptRegPage/SetDG오더RectsAsync_06", bWrite, bMsgBox);
                }

                // 4-3-3. 마지막 항목 제거 (오른쪽 끝 경계)
                if (listLW.Count >= 1)
                {
                    listLW.RemoveAt(listLW.Count - 1);
                }

                int columns = listLW.Count;

                // 평가 1: 컬럼 개수 확인 (최소 개수 이상이어야 함)
                if (columns < m_ReceiptDgHeaderInfos.Length)
                {
                    Debug.WriteLine($"[Cargo24/SetDG오더] 컬럼 개수 불일치: 검출={columns}개, 예상={m_ReceiptDgHeaderInfos.Length}개 (재시도 {retry}/{MAX_RETRY})");

                    bmpDG?.Dispose();

                    StdResult_Error initResult = await InitDG오더Async(
                        CEnum_DgValidationIssue.InvalidColumnCount,
                        bEdit, bWrite, bMsgBox: false);

                    if (initResult != null)
                    {
                        // 사용자 취소 시 즉시 종료 (강제종료)
                        if (initResult.sErr.Contains("사용자 취소"))
                        {
                            return initResult;
                        }

                        if (retry == MAX_RETRY)
                        {
                            return CommonFuncs_StdResult.ErrMsgResult_Error(
                                $"[{m_Context.AppName}/SetDG오더]컬럼 개수 부족: 검출={columns}개, 예상={m_ReceiptDgHeaderInfos.Length}개\n상세: {initResult.sErr}\n(재시도 {MAX_RETRY}회 초과)",
                                "Cargo24sAct_RcptRegPage/SetDG오더RectsAsync_07", bWrite, bMsgBox);
                        }
                    }

                    await Task.Delay(DELAY_RETRY);
                    continue; // 재시도
                }

                // 평가 2: 컬럼 헤더 OFR (처음 22개만)
                Debug.WriteLine($"[Cargo24/SetDG오더] 평가 2: 컬럼 헤더 OFR 시작");
                string[] columnTexts = new string[m_ReceiptDgHeaderInfos.Length];

                for (int i = 1; i < m_ReceiptDgHeaderInfos.Length; i++)
                {
                    Draw.Rectangle rcTmp = new Draw.Rectangle(
                        listLW[i].nLeft, TARGET_ROW, listLW[i].nWidth, OFR_HEIGHT);

                    var result = await OfrWork_Common.OfrStr_ComplexCharSetAsync(
                        bmpDG, rcTmp, bInvertRgb: false, bEdit: bEdit);

                    columnTexts[i] = result?.strResult ?? string.Empty;
                }

                // 평가 3: Datagrid 상태 검증 (순서/너비)
                Debug.WriteLine($"[Cargo24/SetDG오더] 평가 3: Datagrid 상태 검증 시작");
                CEnum_DgValidationIssue validationIssues = ValidateDatagridState(columnTexts, listLW);

                if (validationIssues != CEnum_DgValidationIssue.None)
                {
                    Debug.WriteLine($"[Cargo24/SetDG오더] Datagrid 상태 검증 실패: {validationIssues} (재시도 {retry}/{MAX_RETRY})");

                    bmpDG?.Dispose();

                    StdResult_Error initResult = await InitDG오더Async(
                        validationIssues,
                        bEdit, bWrite, bMsgBox: false);

                    if (initResult != null)
                    {
                        if (initResult.sErr.Contains("사용자 취소"))
                        {
                            return initResult;
                        }

                        if (retry == MAX_RETRY)
                        {
                            return CommonFuncs_StdResult.ErrMsgResult_Error(
                                $"[{m_Context.AppName}/SetDG오더]Datagrid 상태 검증 실패: {validationIssues}\n상세: {initResult.sErr}\n(재시도 {MAX_RETRY}회 초과)",
                                "Cargo24sAct_RcptRegPage/SetDG오더RectsAsync_Validation", bWrite, bMsgBox);
                        }
                    }

                    await Task.Delay(DELAY_RETRY);
                    continue; // 재시도
                }

                // 모든 평가 통과 - 성공
                bmpDG?.Dispose();

                // Step 5 - Cell 좌표 배열 생성
                int rowCount = m_FileInfo.접수등록Page_DG오더_rowCount;
                int rowHeight = m_FileInfo.접수등록Page_DG오더_rowHeight;
                int gab = m_FileInfo.접수등록Page_DG오더_dataGab;
                int textHeight = rowHeight - gab - gab;

                m_RcptPage.DG오더_rcRelCells = new Draw.Rectangle[columns, rowCount]; // [col, row] 인성 패턴
                m_RcptPage.DG오더_ptRelChkRows = new Draw.Point[rowCount];

                for (int row = 0; row < rowCount; row++)
                {
                    int cellY = HEADER_HEIGHT + (row * rowHeight) + 1;

                    // Row check point (첫번째 컬럼 중앙)
                    m_RcptPage.DG오더_ptRelChkRows[row] = new Draw.Point(
                        listLW[0].nLeft + (listLW[0].nWidth / 2), cellY + (rowHeight / 2)
                    );

                    // Cell rects (경계에서 계산하여 미세조정 적용)
                    for (int col = 0; col < columns; col++)
                    {
                        m_RcptPage.DG오더_rcRelCells[col, row] = new Draw.Rectangle(
                            listLW[col].nLeft,
                            cellY + gab,
                            listLW[col].nWidth,
                            textHeight
                        );
                    }
                }

                Debug.WriteLine($"[Cargo24/SetDG오더] Rect 배열 생성 완료: {rowCount}행 x {columns}열");

                // Background Brightness 계산 (데이터그리드 중심 위치)
                m_RcptPage.DG오더_nBackgroundBright = OfrService.GetCenterPixelBrightnessFrmWndHandle(m_RcptPage.DG오더_hWnd);
                Debug.WriteLine($"[Cargo24/SetDG오더] Background Brightness: {m_RcptPage.DG오더_nBackgroundBright}");

                Debug.WriteLine($"[Cargo24/SetDG오더] SetDG오더RectsAsync 완료");
                return null; // 성공
            }

            // 최대 재시도 초과
            return CommonFuncs_StdResult.ErrMsgResult_Error(
                $"[{m_Context.AppName}/SetDG오더]Datagrid 초기화 실패 (재시도 {MAX_RETRY}회 초과)",
                "Cargo24sAct_RcptRegPage/SetDG오더RectsAsync_MaxRetry", bWrite, bMsgBox);
        }
        catch (Exception ex)
        {
            return CommonFuncs_StdResult.ErrMsgResult_Error($"[{m_Context.AppName}/SetDG오더]예외발생: {ex.Message}", "Cargo24sAct_RcptRegPage/SetDG오더RectsAsync_999", bWrite, bMsgBox);
        }
        finally
        {
            // 리소스 정리
            bmpDG?.Dispose();
        }
    }

    /// <summary>
    /// Datagrid 강제 초기화 (원래대로 버튼 → 컬럼 조정 → 순서 저장)
    /// </summary>
    public async Task<StdResult_Error> InitDG오더Async(CEnum_DgValidationIssue issues, bool bEdit = true, bool bWrite = true, bool bMsgBox = true)
    {
        Draw.Bitmap bmpHeader = null;

        try
        {
            Debug.WriteLine($"[Cargo24/InitDG오더] InitDG오더Async 시작: issues={issues}");

            // Step 1: "원래대로" 버튼 클릭 (초기화)
            Debug.WriteLine($"[Cargo24/InitDG오더] Step 1: 원래대로 버튼 클릭");

            if (m_RcptPage.리스트항목_hWnd원래대로 == IntPtr.Zero)
            {
                return CommonFuncs_StdResult.ErrMsgResult_Error(
                    $"[{m_Context.AppName}/InitDG오더]원래대로 버튼 핸들 없음",
                    "Cargo24sAct_RcptRegPage/InitDG오더Async_01", bWrite, bMsgBox);
            }

            await Std32Mouse_Post.MousePostAsync_ClickLeft(m_RcptPage.리스트항목_hWnd원래대로);
            await Task.Delay(DELAY_AFTER_INIT); // 초기화 반영 대기

            Debug.WriteLine($"[Cargo24/InitDG오더] Step 1 완료: 원래대로 버튼 클릭됨");

            // Step 2: 불필요한 컬럼 제거 (수직 드래그)
            Debug.WriteLine($"[Cargo24/InitDG오더] Step 2: 불필요한 컬럼 제거 시작");

            // 2-1. 헤더 캡처 및 컬럼 경계 검출
            Draw.Rectangle rcHeader = new Draw.Rectangle(0, 0, m_RcptPage.DG오더_AbsRect.Width, HEADER_HEIGHT);
            bmpHeader = OfrService.CaptureScreenRect_InWndHandle(m_RcptPage.DG오더_hWnd, rcHeader);
            if (bmpHeader == null)
            {
                return CommonFuncs_StdResult.ErrMsgResult_Error(
                    $"[{m_Context.AppName}/InitDG오더]헤더 캡처 실패",
                    "Cargo24sAct_RcptRegPage/InitDG오더Async_Step2_01", bWrite, bMsgBox);
            }

            byte maxBrightness = OfrService.GetMaxBrightnessAtRow_FromColorBitmapFast(bmpHeader, TARGET_ROW);
            maxBrightness -= BRIGHTNESS_OFFSET;

            bool[] boolArr = OfrService.GetBoolArray_FromColorBitmapRowFast(bmpHeader, TARGET_ROW, maxBrightness, 2);
            List<OfrModel_LeftWidth> listLW = OfrService.GetLeftWidthList_FromBool1Array(boolArr, maxBrightness);

            // 마지막 경계는 제외 (백업 패턴)
            int columns = listLW.Count - 1;
            Debug.WriteLine($"[Cargo24/InitDG오더] Step 2. 현재 컬럼 수: {columns}");

            // 2-2. 우측에서 좌측으로 모든 컬럼 폭 줄이기 (백업: 한 번에 최대한 줄여서 39개 보이게)
            for (int x = columns; x > 1; x--)
            {
                Draw.Point ptStart = new Draw.Point(listLW[x].nLeft, HEADER_GAB);
                Draw.Point ptEnd = new Draw.Point(listLW[x - 1].nLeft + MIN_COLUMN_WIDTH, ptStart.Y);
                if (x >= SPECIAL_COL_START && x <= SPECIAL_COL_END) ptEnd.X += SPECIAL_COL_OFFSET;

                int dx = ptEnd.X - ptStart.X;
                Debug.WriteLine($"[Cargo24/InitDG오더] Step 2. 컬럼[{x}] 드래그: {ptStart.X} → {ptEnd.X} (dx={dx})");

                await Simulation_Mouse.SafeMouseEvent_DragLeft_Smooth_HorizonAsync(
                    m_RcptPage.DG오더_hWnd, ptStart, dx, bBkCursor: false, nMiliSec: 100);
                await Task.Delay(DELAY_AFTER_DRAG);
            }

            bmpHeader?.Dispose();
            Debug.WriteLine($"[Cargo24/InitDG오더] Step 2 완료");

            // Step 3: 컬럼 원하는 위치로 이동
            Debug.WriteLine($"[Cargo24/InitDG오더] Step 3: 컬럼 위치 이동 시작");

            string[] orgColArr = (string[])m_FileInfo.접수등록Page_DG오더_colOrgTexts.Clone(); // 복사본 사용

            for (int x = 1; x < m_ReceiptDgHeaderInfos.Length; x++)
            {
                // 3-1. 매번 헤더 캡처 및 컬럼 경계 재검출
                bmpHeader = OfrService.CaptureScreenRect_InWndHandle(m_RcptPage.DG오더_hWnd, rcHeader);
                if (bmpHeader == null)
                {
                    return CommonFuncs_StdResult.ErrMsgResult_Error(
                        $"[{m_Context.AppName}/InitDG오더]Step3 헤더 캡처 실패",
                        "Cargo24sAct_RcptRegPage/InitDG오더Async_Step3_01", bWrite, bMsgBox);
                }

                boolArr = OfrService.GetBoolArray_FromColorBitmapRowFast(bmpHeader, TARGET_ROW, maxBrightness, 2);
                listLW = OfrService.GetLeftWidthList_FromBool1Array(boolArr, maxBrightness);
                columns = listLW.Count - 1;
                bmpHeader?.Dispose();

                // 3-2. orgColArr에서 원하는 컬럼 위치 찾기
                int find = orgColArr
                    .Select((value, idx) => new { value, idx })
                    .Where(z => z.value == m_ReceiptDgHeaderInfos[x].sName)
                    .Select(z => z.idx)
                    .DefaultIfEmpty(-1)
                    .First();

                if (find < 0)
                {
                    return CommonFuncs_StdResult.ErrMsgResult_Error(
                        $"[{m_Context.AppName}/InitDG오더]컬럼[{x}] '{m_ReceiptDgHeaderInfos[x].sName}' 못찾음",
                        "Cargo24sAct_RcptRegPage/InitDG오더Async_Step3_02", bWrite, bMsgBox);
                }

                if (find == x) continue; // 같은 위치면 패스

                Debug.WriteLine($"[Cargo24/InitDG오더] Step 3. 컬럼[{x}] '{m_ReceiptDgHeaderInfos[x].sName}': {find} → {x}");

                // 3-3. 드래그 (find 위치 → x 위치)
                Draw.Point ptStart = new Draw.Point(listLW[find].nLeft + 10, HEADER_GAB);
                Draw.Point ptEnd = new Draw.Point(listLW[x].nLeft + 10, ptStart.Y);
                int dx = ptEnd.X - ptStart.X;

                await Simulation_Mouse.SafeMouseEvent_DragLeft_Smooth_HorizonAsync(
                    m_RcptPage.DG오더_hWnd, ptStart, dx, bBkCursor: false, nMiliSec: 100);

                // 3-4. orgColArr 배열 업데이트 (컬럼 밀기)
                string temp = orgColArr[find];
                for (int m = find; m > x; m--) orgColArr[m] = orgColArr[m - 1];
                orgColArr[x] = temp;

                await Task.Delay(DELAY_AFTER_DRAG);
            }

            Debug.WriteLine($"[Cargo24/InitDG오더] Step 3 완료: 컬럼 위치 이동됨");

            // Step 4: 컬럼 원하는 폭으로 확장
            Debug.WriteLine($"[Cargo24/InitDG오더] Step 4: 컬럼 폭 조정 시작");

            // 4-1. 현재 컬럼 경계 재검출
            bmpHeader = OfrService.CaptureScreenRect_InWndHandle(m_RcptPage.DG오더_hWnd, rcHeader);
            if (bmpHeader == null)
            {
                return CommonFuncs_StdResult.ErrMsgResult_Error(
                    $"[{m_Context.AppName}/InitDG오더]Step4 헤더 캡처 실패",
                    "Cargo24sAct_RcptRegPage/InitDG오더Async_Step4_01", bWrite, bMsgBox);
            }

            boolArr = OfrService.GetBoolArray_FromColorBitmapRowFast(bmpHeader, TARGET_ROW, maxBrightness, BRIGHTNESS_OFFSET);
            listLW = OfrService.GetLeftWidthList_FromBool1Array(boolArr, maxBrightness);
            columns = listLW.Count - 1;
            bmpHeader?.Dispose();

            // 4-2. 뒤에서 앞으로 원하는 폭으로 확장 (백업: 재검출 없음)
            for (int x = m_ReceiptDgHeaderInfos.Length - 1; x > 0; x--)
            {
                Draw.Point ptStart = new Draw.Point(listLW[x + 1].nLeft, HEADER_GAB);
                int dx = m_ReceiptDgHeaderInfos[x].nWidth - listLW[x].nWidth;

                Debug.WriteLine($"[Cargo24/InitDG오더] Step 4. 컬럼[{x}] '{m_ReceiptDgHeaderInfos[x].sName}': 폭 {listLW[x].nWidth} → {m_ReceiptDgHeaderInfos[x].nWidth} (dx={dx})");

                await Simulation_Mouse.SafeMouseEvent_DragLeft_Smooth_HorizonAsync(
                    m_RcptPage.DG오더_hWnd, ptStart, dx, bBkCursor: false, nMiliSec: 100);
                await Task.Delay(DELAY_AFTER_DRAG);
            }

            Debug.WriteLine($"[Cargo24/InitDG오더] Step 4 완료: 컬럼 폭 조정됨");

            // 확인작업: 컬럼 수 및 텍스트 검증 (백업 패턴)
            Debug.WriteLine($"[Cargo24/InitDG오더] 확인작업 시작");

            bmpHeader = OfrService.CaptureScreenRect_InWndHandle(m_RcptPage.DG오더_hWnd, rcHeader);
            if (bmpHeader == null)
            {
                return CommonFuncs_StdResult.ErrMsgResult_Error(
                    $"[{m_Context.AppName}/InitDG오더]확인작업 헤더 캡처 실패", "Cargo24sAct_RcptRegPage/InitDG오더Async_Verify_01", bWrite, bMsgBox);
            }

            boolArr = OfrService.GetBoolArray_FromColorBitmapRowFast(bmpHeader, TARGET_ROW, maxBrightness, BRIGHTNESS_OFFSET);
            listLW = OfrService.GetLeftWidthList_FromBool1Array(boolArr, maxBrightness);
            columns = listLW.Count - 1;

            // 컬럼 수 확인
            if (m_ReceiptDgHeaderInfos.Length > columns)
            {
                bmpHeader?.Dispose();
                return CommonFuncs_StdResult.ErrMsgResult_Error(
                    $"[{m_Context.AppName}/InitDG오더]컬럼 수 부족: 예상={m_ReceiptDgHeaderInfos.Length}, 검출={columns}",
                    "Cargo24sAct_RcptRegPage/InitDG오더Async_Verify_02", bWrite, bMsgBox);
            }

            // OFR로 각 컬럼 텍스트 검증
            for (int x = 1; x < m_ReceiptDgHeaderInfos.Length; x++)
            {
                Draw.Rectangle rcTmp = new Draw.Rectangle(listLW[x].nLeft, HEADER_GAB, listLW[x].nWidth, OFR_HEIGHT);

                var resultChSet = await OfrWork_Common.OfrStr_ComplexCharSetAsync(
                    bmpHeader, rcTmp, bInvertRgb: false, bEdit: bEdit);

                if (string.IsNullOrEmpty(resultChSet.strResult))
                {
                    bmpHeader?.Dispose();
                    return CommonFuncs_StdResult.ErrMsgResult_Error(
                        $"[{m_Context.AppName}/InitDG오더]컬럼[{x}] OFR 실패: {resultChSet.sErr}", "Cargo24sAct_RcptRegPage/InitDG오더Async_Verify_03", bWrite, bMsgBox);
                }

                if (resultChSet.strResult != m_ReceiptDgHeaderInfos[x].sName)
                {
                    bmpHeader?.Dispose();
                    return CommonFuncs_StdResult.ErrMsgResult_Error(
                        $"[{m_Context.AppName}/InitDG오더]컬럼[{x}] 불일치: 예상={m_ReceiptDgHeaderInfos[x].sName}, 검출={resultChSet.strResult}",
                        "Cargo24sAct_RcptRegPage/InitDG오더Async_Verify_04", bWrite, bMsgBox);
                }

                Debug.WriteLine($"[Cargo24/InitDG오더] 확인: 컬럼[{x}] '{resultChSet.strResult}' OK");
            }

            bmpHeader?.Dispose();
            Debug.WriteLine($"[Cargo24/InitDG오더] 확인작업 완료");

            // Step 5: 순서 저장
            Debug.WriteLine($"[Cargo24/InitDG오더] Step 5: 순서 저장 시작");

            const string Dlg저장확인_sClassName = "TMessageForm";
            const string Dlg저장확인_sWndName1 = "Information";
            const string Dlg저장확인_sWndName2 = "Cargo24";
            const string Btn저장확인_sClassName = "TButton";
            const string Btn저장확인_sWndName = "OK";

            // 5-1. 순서 저장 버튼 클릭
            await Std32Mouse_Post.MousePostAsync_ClickLeft(m_RcptPage.리스트항목_hWnd순서저장);

            // 5-2. 대화상자 찾기 및 OK 클릭
            IntPtr hWndDlg = IntPtr.Zero;
            IntPtr hWndBtn = IntPtr.Zero;

            for (int i = 0; i < 50; i++)
            {
                await Task.Delay(DELAY_DIALOG_CHECK);

                hWndDlg = StdWin32.FindWindow(Dlg저장확인_sClassName, Dlg저장확인_sWndName1);
                if (hWndDlg == IntPtr.Zero)
                {
                    hWndDlg = StdWin32.FindWindow(Dlg저장확인_sClassName, Dlg저장확인_sWndName2);
                }

                if (hWndDlg != IntPtr.Zero)
                {
                    await Task.Delay(DELAY_DIALOG_CHECK);

                    hWndBtn = StdWin32.FindWindowEx(hWndDlg, IntPtr.Zero, Btn저장확인_sClassName, Btn저장확인_sWndName);
                    if (hWndBtn == IntPtr.Zero) break;

                    Debug.WriteLine($"[Cargo24/InitDG오더] Step 5. 저장 대화상자 찾음: {hWndDlg:X}, 버튼: {hWndBtn:X}");

                    await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndBtn);

                    // 대화상자 사라질때까지 대기
                    for (int j = 0; j < 50; j++)
                    {
                        await Task.Delay(DELAY_DIALOG_CHECK);
                        if (!StdWin32.IsWindow(hWndDlg)) break;
                    }

                    if (!StdWin32.IsWindow(hWndDlg)) break;
                }
            }

            if (hWndDlg == IntPtr.Zero)
            {
                return CommonFuncs_StdResult.ErrMsgResult_Error(
                    $"[{m_Context.AppName}/InitDG오더]저장 대화상자 못찾음", "Cargo24sAct_RcptRegPage/InitDG오더Async_Step5_01", bWrite, bMsgBox);
            }

            if (StdWin32.IsWindow(hWndDlg))
            {
                return CommonFuncs_StdResult.ErrMsgResult_Error(
                    $"[{m_Context.AppName}/InitDG오더]저장 대화상자 사라지지 않음", "Cargo24sAct_RcptRegPage/InitDG오더Async_Step5_02", bWrite, bMsgBox);
            }

            Debug.WriteLine($"[Cargo24/InitDG오더] Step 5 완료: 순서 저장됨");

            Debug.WriteLine($"[Cargo24/InitDG오더] InitDG오더Async 완료 (Step 1~5)");
            return null; // 성공
        }
        catch (Exception ex)
        {
            return CommonFuncs_StdResult.ErrMsgResult_Error(
                $"[{m_Context.AppName}/InitDG오더]예외발생: {ex.Message}", "Cargo24sAct_RcptRegPage/InitDG오더Async_999", bWrite, bMsgBox);
        }
        finally
        {
            bmpHeader?.Dispose();
        }
    }
    #endregion

    #region Helper Methods
    /// <summary>
    /// StatusBtn 찾기 헬퍼 메서드 (인성 패턴 참고)
    /// </summary>
    private async Task<(IntPtr hWnd, StdResult_Error error)> FindStatusButtonAsync(
        string buttonName, Draw.Point checkPoint, string errorCode, bool bWrite, bool bMsgBox, bool withTextValidation = true)
    {
        for (int i = 0; i < c_nRepeatVeryMany; i++)
        {
            IntPtr hWnd = Std32Window.GetWndHandle_FromRelDrawPt(m_Main.TopWnd_hWnd, checkPoint);

            if (hWnd != IntPtr.Zero)
            {
                if (withTextValidation)
                {
                    string text = Std32Window.GetWindowCaption(hWnd);
                    if (text.Contains(buttonName))
                    {
                        //Debug.WriteLine($"[Cargo24sAct_RcptRegPage] {buttonName}버튼 찾음: {hWnd:X}, 텍스트: {text}");
                        return (hWnd, null);
                    }
                }
                else
                {
                    //Debug.WriteLine($"[Cargo24sAct_RcptRegPage] {buttonName}버튼 찾음: {hWnd:X}");
                    return (hWnd, null);
                }
            }

            await Task.Delay(c_nWaitNormal);
        }

        // 찾기 실패
        var error = CommonFuncs_StdResult.ErrMsgResult_Error(
            $"[{m_Context.AppName}/RcptRegPage]{buttonName}버튼 찾기실패: {checkPoint}",
            errorCode, bWrite, bMsgBox);
        return (IntPtr.Zero, error);
    }

    /// <summary>
    /// Datagrid 상태 검증 (컬럼 순서, 너비 확인)
    /// </summary>
    /// <param name="columnTexts">OFR로 읽어온 컬럼 헤더 텍스트 배열</param>
    /// <param name="listLW">컬럼 Left/Width 리스트</param>
    /// <returns>검증 이슈 플래그 (None이면 정상)</returns>
    private CEnum_DgValidationIssue ValidateDatagridState(string[] columnTexts, List<OfrModel_LeftWidth> listLW)
    {
        CEnum_DgValidationIssue issues = CEnum_DgValidationIssue.None;

        // 각 컬럼 검증 (1부터 시작 - 0은 미사용)
        for (int x = 1; x < m_ReceiptDgHeaderInfos.Length; x++)
        {
            string columnText = columnTexts[x];

            // 1. 컬럼명이 m_ReceiptDgHeaderInfos에 존재하는지
            int index = Array.FindIndex(m_ReceiptDgHeaderInfos, h => h.sName == columnText);

            if (index < 0)
            {
                issues |= CEnum_DgValidationIssue.InvalidColumn;
                Debug.WriteLine($"[ValidateDatagridState] 유효하지 않은 컬럼[{x}]: '{columnText}'");
                continue;
            }

            // 2. 컬럼 순서가 맞는지
            if (index != x)
            {
                issues |= CEnum_DgValidationIssue.WrongOrder;
                Debug.WriteLine($"[ValidateDatagridState] 순서 불일치[{x}]: '{columnText}' (예상 위치={index})");
            }

            // 3. 컬럼 너비가 맞는지
            int actualWidth = listLW[x].nWidth;
            int expectedWidth = m_ReceiptDgHeaderInfos[index].nWidth;
            int widthDiff = Math.Abs(actualWidth - expectedWidth);

            if (widthDiff > COLUMN_WIDTH_TOLERANCE)
            {
                issues |= CEnum_DgValidationIssue.WrongWidth;
            }
        }

        if (issues == CEnum_DgValidationIssue.None)
        {
            Debug.WriteLine($"[ValidateDatagridState] Datagrid 상태 정상");
        }

        return issues;
    }
    #endregion

    #region 조회 버튼
    /// <summary>
    /// 조회 버튼 클릭 (재시도 루프 방식)
    /// - 모래시계 커서 출현/사라짐으로 로딩 완료 판단
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

                // 조회 버튼 클릭
                await Std32Mouse_Post.MousePostAsync_ClickLeft(m_RcptPage.CmdBtn_hWnd조회);

                // 모래시계 커서 대기
                StdResult_Status resultSts = await WaitCursorLoadedAsync(ctrl);

                if (resultSts.Result == StdResult.Success || resultSts.Result == StdResult.Skip)
                {
                    return new StdResult_Status(StdResult.Success, "조회 완료");
                }

                // Fail = 타임아웃 → 재시도
                Debug.WriteLine($"[{m_Context.AppName}] 조회 실패 (시도 {i}회): 타임아웃");
                await Task.Delay(c_nWaitNormal, ctrl.Token);
            }

            return new StdResult_Status(StdResult.Fail, $"조회 버튼 클릭 {retryCount}회 모두 실패", "Cargo24sAct_RcptRegPage/Click조회버튼Async_01");
        }
        catch (Exception ex)
        {
            return new StdResult_Status(StdResult.Fail, StdUtil.GetExceptionMessage(ex), "Cargo24sAct_RcptRegPage/Click조회버튼Async_999");
        }
    }

    /// <summary>
    /// 모래시계 커서 대기 (로딩 완료 판단)
    /// Phase 1: 모래시계 출현 대기 (최대 250ms)
    /// Phase 2: 모래시계 사라짐 대기 (최대 timeoutSec초)
    /// </summary>
    private async Task<StdResult_Status> WaitCursorLoadedAsync(CancelTokenControl ctrl, int timeoutSec = 50)
    {
        try
        {
            // Phase 1: 모래시계 출현 대기 (최대 250ms)
            bool bWaitCursorAppeared = false;
            for (int i = 0; i < c_nWaitLong; i++) // 250ms
            {
                await ctrl.WaitIfPausedOrCancelledAsync();

                if (Std32Cursor.IsWaitCursor())
                {
                    bWaitCursorAppeared = true;
                    break;
                }
                await Task.Delay(1, ctrl.Token);
            }

            if (!bWaitCursorAppeared)
            {
                // 모래시계 미출현 → Skip (이미 로딩 완료)
                return new StdResult_Status(StdResult.Skip);
            }

            // Phase 2: 모래시계 사라짐 대기 (최대 timeoutSec초)
            for (int i = 0; i < timeoutSec * 10; i++) // 100ms 간격
            {
                await ctrl.WaitIfPausedOrCancelledAsync();

                if (!Std32Cursor.IsWaitCursor())
                {
                    return new StdResult_Status(StdResult.Success);
                }
                await Task.Delay(100, ctrl.Token);
            }

            return new StdResult_Status(StdResult.Fail, $"로딩 대기 시간 초과 ({timeoutSec}초)", "Cargo24sAct_RcptRegPage/WaitCursorLoadedAsync_01");
        }
        catch (Exception ex)
        {
            return new StdResult_Status(StdResult.Fail, StdUtil.GetExceptionMessage(ex), "Cargo24sAct_RcptRegPage/WaitCursorLoadedAsync_999");
        }
    }
    #endregion

    #region 자동배차 - Kai신규 관련함수들
    /// <summary>
    /// 신규 버튼 클릭 → 팝업창 열기 → RegistOrderToPopupAsync 호출
    /// </summary>
    public async Task<StdResult_Status> OpenNewOrderPopupAsync(AutoAllocModel item, CancelTokenControl ctrl)
    {
        IntPtr hWndPopup = IntPtr.Zero;
        bool bFound = false;

        try
        {
            // 1. 신규 버튼 클릭 시도 (최대 3번)
            for (int retry = 1; retry <= c_nRepeatShort; retry++)
            {
                await ctrl.WaitIfPausedOrCancelledAsync();

                // 신규 버튼 클릭
                await Std32Mouse_Post.MousePostAsync_ClickLeft(m_RcptPage.CmdBtn_hWnd신규);
                Debug.WriteLine($"[{m_Context.AppName}] 신규버튼 클릭 완료 (시도 {retry}/{c_nRepeatShort})");

                // 2. 팝업창 찾기 (반복 대기)
                for (int k = 0; k < c_nRepeatMany; k++)
                {
                    await Task.Delay(c_nWaitNormal);

                    // 팝업창 찾기 ("화물등록")
                    hWndPopup = Std32Window.FindMainWindow_NotTransparent(
                        m_Splash.TopWnd_uProcessId, m_FileInfo.접수등록Wnd_TopWnd_sWndName);
                    if (hWndPopup == IntPtr.Zero) continue;

                    // 3. 팝업창 ClassName 검증 ("TfrmCargoOrderIns")
                    string className = Std32Window.GetWindowClassName(hWndPopup);
                    if (className != m_FileInfo.접수등록Wnd_TopWnd_sClassName) continue;

                    // 닫기 버튼 핸들 검증 (추가 안전장치)
                    IntPtr hWndClose = Std32Window.GetWndHandle_FromRelDrawPt(
                        hWndPopup, m_FileInfo.접수등록Wnd_CmnBtn_ptRel닫기);
                    if (hWndClose == IntPtr.Zero) continue;

                    bFound = true;
                    Debug.WriteLine($"[{m_Context.AppName}] 신규주문 팝업창 열림: {hWndPopup:X}, ClassName={className}");
                    break;
                }

                if (bFound) break;
            }

            // 4. 결과 처리
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
    /// - TODO: 입력 작업
    /// - 닫기 버튼 클릭
    /// - 창 닫힘 확인 (성공 판단)
    /// </summary>
    public async Task<StdResult_Status> RegistOrderToPopupAsync(AutoAllocModel item, IntPtr hWndPopup, CancelTokenControl ctrl)
    {
        try
        {
            await ctrl.WaitIfPausedOrCancelledAsync();

            Debug.WriteLine($"[{m_Context.AppName}] RegistOrderToPopupAsync 진입: KeyCode={item.KeyCode}, hWndPopup={hWndPopup:X}");

            TbOrder tbOrder = item.NewOrder;
            StdResult_Status result;

            #region ===== 사전작업 =====
            // 주소 필드 확인용 로그
            Debug.WriteLine($"[{m_Context.AppName}] 상차지: DongBasic={tbOrder.StartDongBasic}, Address={tbOrder.StartAddress}, DetailAddr={tbOrder.StartDetailAddr}");
            Debug.WriteLine($"[{m_Context.AppName}] 하차지: DongBasic={tbOrder.DestDongBasic}, Address={tbOrder.DestAddress}, DetailAddr={tbOrder.DestDetailAddr}");

            // 상차지 주소 체크 (상세주소 기준)
            if (string.IsNullOrEmpty(tbOrder.StartDetailAddr))
                return new StdResult_Status(StdResult.Fail,
                    "상차지 주소가 없습니다.", "RegistOrderToPopupAsync_Pre01");

            // 하차지 주소 체크 (상세주소 기준)
            if (string.IsNullOrEmpty(tbOrder.DestDetailAddr))
                return new StdResult_Status(StdResult.Fail,
                    "하차지 주소가 없습니다.", "RegistOrderToPopupAsync_Pre02");

            // 팝업창 TopMost 설정 후 해제
            await Std32Window.SetWindowTopMostAndReleaseAsync(hWndPopup);
            #endregion

            #region ===== 0. 의뢰자 정보 입력 =====
            Debug.WriteLine($"[{m_Context.AppName}] 0. 의뢰자 정보 입력...");

            // 의뢰자 고객명
            IntPtr hWnd의뢰자고객명 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_의뢰자_ptRel고객명);
            result = await WriteAndVerifyEditBoxAsync(hWnd의뢰자고객명, tbOrder.CallCustName ?? "", "의뢰자_고객명", ctrl);
            if (result.Result != StdResult.Success)
                return result;

            // 의뢰자 전화
            IntPtr hWnd의뢰자전화 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_의뢰자_ptRel고객전화);
            result = await WriteAndVerifyEditBoxAsync(hWnd의뢰자전화, tbOrder.CallTelNo ?? "", "의뢰자_전화", ctrl);
            if (result.Result != StdResult.Success)
                return result;

            // 의뢰자 담당자
            IntPtr hWnd의뢰자담당 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_의뢰자_ptRel담당자);
            result = await WriteAndVerifyEditBoxAsync(hWnd의뢰자담당, tbOrder.CallChargeName ?? "", "의뢰자_담당자", ctrl);
            if (result.Result != StdResult.Success)
                return result;

            Debug.WriteLine($"[{m_Context.AppName}] 0. 의뢰자 정보 입력 완료");
            #endregion

            #region ===== 1. 상차지 정보 입력 =====
            Debug.WriteLine($"[{m_Context.AppName}] 1. 상차지 정보 입력...");

            // 상차지 조회버튼 클릭 → 주소검색창 열기 → 검색 → 선택
            result = await SearchAndSelectAddressAsync(
                hWndPopup,
                m_FileInfo.접수등록Wnd_상차지_ptRel조회버튼,
                tbOrder.StartDetailAddr,
                "상차지",
                ctrl);
            if (result.Result != StdResult.Success)
                return result;

            // 상차지 위치 (상세주소)
            IntPtr hWnd상차위치 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_상차지_ptRel위치);
            result = await WriteAndVerifyEditBoxAsync(hWnd상차위치, tbOrder.StartDetailAddr ?? "", "상차지_위치", ctrl);
            if (result.Result != StdResult.Success)
                return result;

            // 상차지 고객명
            IntPtr hWnd상차고객명 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_상차지_ptRel고객명);
            result = await WriteAndVerifyEditBoxAsync(hWnd상차고객명, tbOrder.StartCustName ?? "", "상차지_고객명", ctrl);
            if (result.Result != StdResult.Success)
                return result;

            // 상차지 전화
            IntPtr hWnd상차전화 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_상차지_ptRel전화);
            result = await WriteAndVerifyEditBoxAsync(hWnd상차전화, tbOrder.StartTelNo ?? "", "상차지_전화", ctrl);
            if (result.Result != StdResult.Success)
                return result;

            // 상차지 부서명
            IntPtr hWnd상차부서 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_상차지_ptRel부서명);
            result = await WriteAndVerifyEditBoxAsync(hWnd상차부서, tbOrder.StartDeptName ?? "", "상차지_부서명", ctrl);
            if (result.Result != StdResult.Success)
                return result;

            // 상차지 담당자
            IntPtr hWnd상차담당 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_상차지_ptRel담당자);
            result = await WriteAndVerifyEditBoxAsync(hWnd상차담당, tbOrder.StartChargeName ?? "", "상차지_담당자", ctrl);
            if (result.Result != StdResult.Success)
                return result;

            Debug.WriteLine($"[{m_Context.AppName}] 1. 상차지 정보 입력 완료");
            #endregion

            #region ===== 2. 하차지 정보 입력 =====
            Debug.WriteLine($"[{m_Context.AppName}] 2. 하차지 정보 입력...");

            // 하차지 조회버튼 클릭 → 주소검색창 열기 → 검색 → 선택
            result = await SearchAndSelectAddressAsync(
                hWndPopup,
                m_FileInfo.접수등록Wnd_하차지_ptRel조회버튼,
                tbOrder.DestDetailAddr,
                "하차지",
                ctrl);
            if (result.Result != StdResult.Success)
                return result;

            // 하차지 위치 (상세주소)
            IntPtr hWnd하차위치 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_하차지_ptRel위치);
            result = await WriteAndVerifyEditBoxAsync(hWnd하차위치, tbOrder.DestDetailAddr ?? "", "하차지_위치", ctrl);
            if (result.Result != StdResult.Success)
                return result;

            // 하차지 고객명
            IntPtr hWnd하차고객명 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_하차지_ptRel고객명);
            result = await WriteAndVerifyEditBoxAsync(hWnd하차고객명, tbOrder.DestCustName ?? "", "하차지_고객명", ctrl);
            if (result.Result != StdResult.Success)
                return result;

            // 하차지 전화
            IntPtr hWnd하차전화 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_하차지_ptRel전화);
            result = await WriteAndVerifyEditBoxAsync(hWnd하차전화, tbOrder.DestTelNo ?? "", "하차지_전화", ctrl);
            if (result.Result != StdResult.Success)
                return result;

            // 하차지 부서명
            IntPtr hWnd하차부서 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_하차지_ptRel부서명);
            result = await WriteAndVerifyEditBoxAsync(hWnd하차부서, tbOrder.DestDeptName ?? "", "하차지_부서명", ctrl);
            if (result.Result != StdResult.Success)
                return result;

            // 하차지 담당자
            IntPtr hWnd하차담당 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_하차지_ptRel담당자);
            result = await WriteAndVerifyEditBoxAsync(hWnd하차담당, tbOrder.DestChargeName ?? "", "하차지_담당자", ctrl);
            if (result.Result != StdResult.Success)
                return result;

            Debug.WriteLine($"[{m_Context.AppName}] 2. 하차지 정보 입력 완료");
            #endregion

            #region ===== 3. 배송타입 (체크박스 - 복수 선택 가능) =====
            Debug.WriteLine($"[{m_Context.AppName}] 3. 배송타입 입력... DeliverType={tbOrder.DeliverType}");

            // 오더번호, 오더상태를 얻으려면 여기에서...
            CEnum_Cg24OrderStatus status = Get오더타입FlagsFromKaiTable(tbOrder);

            result = await Set오더타입Async(hWndPopup, status, ctrl);
            if (result.Result != StdResult.Success) return result;

            Debug.WriteLine($"[{m_Context.AppName}] 3. 배송타입 입력 완료");
            #endregion

            #region ===== 4. 차량정보 =====
            Debug.WriteLine($"[{m_Context.AppName}] 4. 차량정보 입력... CarType={tbOrder.CarType}, CarWeight={tbOrder.CarWeight}, TruckDetail={tbOrder.TruckDetail}");

            // 4-1. 차량톤수 (라디오버튼)
            var ptTon = GetCarWeightWithPoint(tbOrder.CarType, tbOrder.CarWeight);
            if (!string.IsNullOrEmpty(ptTon.sErr))
                return new StdResult_Status(StdResult.Fail, ptTon.sErr, "RegistOrderToPopupAsync_04_01");

            IntPtr hWndTon = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, ptTon.ptResult);
            if (hWndTon == IntPtr.Zero)
                return new StdResult_Status(StdResult.Fail, "톤수 라디오버튼 핸들 획득 실패", "RegistOrderToPopupAsync_04_02");

            result = await Simulation_Mouse.SetCheckBtnStatusAsync(hWndTon, true);
            if (result.Result != StdResult.Success)
                return new StdResult_Status(StdResult.Fail, "톤수 라디오버튼 설정 실패", "RegistOrderToPopupAsync_04_03");

            // 몇톤 Edit에 최대적재량 설정 (1.1배)
            IntPtr hWndTonEdit = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_톤수Edit_ptRel몇톤);
            if (hWndTonEdit != IntPtr.Zero)
            {
                string sMaxWeight = GetMaxCargoWeightString(hWndTon);
                await OfrWork_Common.WriteEditBox_ToHndleAsyncUpdate(hWndTonEdit, sMaxWeight);
            }

            // 이하 체크박스 설정
            IntPtr hWndTonChk = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_톤수ChkBox_ptRel이하);
            if (hWndTonChk != IntPtr.Zero)
                await Simulation_Mouse.SetCheckBtnStatusAsync(hWndTonChk, true);

            Debug.WriteLine($"[{m_Context.AppName}] 4-1. 차량톤수 설정 완료: {tbOrder.CarWeight}, 핸들캡션={Std32Window.GetWindowCaption(hWndTon)}");

            // 4-2. 차종 (콤보박스)
            var strTruck = GetTruckDetailStringFromInsung(tbOrder.TruckDetail);
            if (!string.IsNullOrEmpty(strTruck.sErr))
                return new StdResult_Status(StdResult.Fail, strTruck.sErr, "RegistOrderToPopupAsync_04_04");

            IntPtr hWndTruck = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_차종Combo_ptRel차종확인);
            if (hWndTruck == IntPtr.Zero)
                return new StdResult_Status(StdResult.Fail, "차종 콤보박스 핸들 획득 실패", "RegistOrderToPopupAsync_04_05");

            Std32Window.SetWindowCaption(hWndTruck, strTruck.strResult);

            Debug.WriteLine($"[{m_Context.AppName}] 4-2. 차종 설정 완료: {tbOrder.TruckDetail} → {strTruck.strResult}");
            Debug.WriteLine($"[{m_Context.AppName}] 4. 차량정보 입력 완료");
            #endregion

            #region ===== 5. 운송비 =====
            Debug.WriteLine($"[{m_Context.AppName}] 5. 운송비 입력... FeeType={tbOrder.FeeType}, FeeTotal={tbOrder.FeeTotal}");

            // 5-1. 운송비구분 (라디오버튼)
            var ptFee = GetFeeTypePoint(tbOrder.FeeType);
            if (!string.IsNullOrEmpty(ptFee.sErr))
                return new StdResult_Status(StdResult.Fail, ptFee.sErr, "RegistOrderToPopupAsync_05_01");

            IntPtr hWndFeeType = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, ptFee.ptResult);
            if (hWndFeeType == IntPtr.Zero)
                return new StdResult_Status(StdResult.Fail, "운송비구분 라디오버튼 핸들 획득 실패", "RegistOrderToPopupAsync_05_02");

            result = await Simulation_Mouse.SetCheckBtnStatusAsync(hWndFeeType, true);
            if (result.Result != StdResult.Success)
                return new StdResult_Status(StdResult.Fail, "운송비구분 라디오버튼 설정 실패", "RegistOrderToPopupAsync_05_03");

            Debug.WriteLine($"[{m_Context.AppName}] 5-1. 운송비구분 설정 완료: {tbOrder.FeeType}");

            // 5-2. 운송비 합계 (Edit) - 합계만 입력하면 나머지 자동계산
            IntPtr hWndFeeTotal = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_운송비Edit_ptRel합계);
            if (hWndFeeTotal == IntPtr.Zero)
                return new StdResult_Status(StdResult.Fail, "운송비합계 Edit 핸들 획득 실패", "RegistOrderToPopupAsync_05_04");

            //Std32Key_Msg.KeyPost_Digit(hWndFeeTotal, (uint)tbOrder.FeeTotal); // 2 입력 - 이걸로 해도 됨
            await OfrWork_Common.WriteEditBox_ToHndleAsyncUpdate(hWndFeeTotal, tbOrder.FeeTotal.ToString());
            Std32Key_Msg.KeyPost_Click(hWndFeeTotal, StdCommon32.VK_RETURN); // Enter → 자동계산 트리거

            // 수수료 - 추후 구현예정

            Debug.WriteLine($"[{m_Context.AppName}] 5. 운송비 입력 완료");
            #endregion

            #region 상차정보 - Reserved
            #endregion 상차정보 끝

            #region 하차정보  - Reserved
            #endregion 하차정보 끝

            #region 추가정보  - Reserved
            #endregion 추가정보 끝

            #region 기사정보  - Reserved
            #endregion 기사정보 끝

            #region ===== 종료 작업 =====
            // 저장 버튼 선택: OrderState가 "접수"이면 접수저장, 나머지는 대기저장
            bool bReceiptState = (tbOrder.OrderState == "접수");
            Draw.Point ptRelSave = bReceiptState
                ? m_FileInfo.접수등록Wnd_CmnBtn_ptRel접수저장
                : m_FileInfo.접수등록Wnd_CmnBtn_ptRel대기저장;

            Debug.WriteLine($"[{m_Context.AppName}] 종료 작업... OrderState={tbOrder.OrderState}, 저장모드={(bReceiptState ? "접수저장" : "대기저장")}");

            // 저장 버튼 핸들 얻기
            IntPtr hWndSave = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, ptRelSave);
            if (hWndSave == IntPtr.Zero)
            {
                Debug.WriteLine($"[{m_Context.AppName}] 저장 버튼 핸들 못찾음");
                return new StdResult_Status(StdResult.Fail, "저장 버튼 핸들 못찾음", "RegistOrderToPopupAsync_06_01");
            }

            // 저장 버튼 클릭 및 창 닫힘 확인
            bool bClosed = await ClickNWaitWindowClosedAsync(hWndSave, hWndPopup, ctrl, bReceiptState);

            if (bClosed)
            {
                Debug.WriteLine($"[{m_Context.AppName}] 저장 완료, 팝업창 닫힘 확인");
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

                // 3. 화물번호 OFR
                StdResult_String resultSeqno = await Get화물번호Async(0, ctrl);
                if (string.IsNullOrEmpty(resultSeqno.strResult))
                {
                    return new StdResult_Status(StdResult.Fail, $"화물번호 획득 실패: {resultSeqno.sErr}");
                }

                Debug.WriteLine($"[{m_Context.AppName}] 주문 등록 완료 - 화물번호: {resultSeqno.strResult}");

                // 4. Kai DB의 Cargo24 필드 업데이트
                // 4-1. 사전 체크
                if (item.NewOrder.KeyCode <= 0)
                {
                    Debug.WriteLine($"[{m_Context.AppName}] KeyCode 없음 - Kai DB에 없는 주문");
                    return new StdResult_Status(StdResult.Fail, "Kai DB에 없는 주문입니다");
                }

                if (!string.IsNullOrEmpty(item.NewOrder.Cargo24))
                {
                    Debug.WriteLine($"[{m_Context.AppName}] 이미 등록된 화물번호: {item.NewOrder.Cargo24}");
                    return new StdResult_Status(StdResult.Skip, "이미 Cargo24 번호가 등록되어 있습니다");
                }

                if (CommonVars.s_SrGClient == null || !CommonVars.s_SrGClient.m_bLoginSignalR)
                {
                    Debug.WriteLine($"[{m_Context.AppName}] SignalR 연결 안됨");
                    return new StdResult_Status(StdResult.Fail, "서버 연결이 끊어졌습니다");
                }

                // 4-2. 업데이트 실행
                item.NewOrder.Cargo24 = resultSeqno.strResult;

                StdResult_Int resultUpdate = await CommonVars.s_SrGClient.SrResult_Order_UpdateRowAsync_Today_WithRequestId(item.NewOrder);

                if (resultUpdate.nResult < 0 || !string.IsNullOrEmpty(resultUpdate.sErr))
                {
                    Debug.WriteLine($"[{m_Context.AppName}] Kai DB 업데이트 실패: {resultUpdate.sErr}");
                    return new StdResult_Status(StdResult.Fail, $"Kai DB 업데이트 실패: {resultUpdate.sErr}");
                }

                Debug.WriteLine($"[{m_Context.AppName}] Kai DB 업데이트 성공 - Cargo24: {resultSeqno.strResult}");

                return new StdResult_Status(StdResult.Success, $"저장 완료 (화물번호: {resultSeqno.strResult})");
            }
            else
            {
                Debug.WriteLine($"[{m_Context.AppName}] 팝업창 닫기 실패");
                return new StdResult_Status(StdResult.Fail, "팝업창 닫기 실패", "RegistOrderToPopupAsync_06_02");
            }

            //MsgBox("Here");
            return new StdResult_Status(StdResult.Success);
            #endregion
        }
        catch (Exception ex)
        {
            return new StdResult_Status(StdResult.Fail, StdUtil.GetExceptionMessage(ex), "RegistOrderToPopupAsync_999");
        }
    }

    /// <summary>
    /// TbOrder에서 CEnum_Cg24OrderStatus 플래그 생성
    /// </summary>
    private static CEnum_Cg24OrderStatus Get오더타입FlagsFromKaiTable(TbOrder tbOrder)
    {
        CEnum_Cg24OrderStatus status = CEnum_Cg24OrderStatus.None;

        //if (tbOrder.Share) status |= CEnum_Cg24OrderStatus.공유; // 화물24시는 무조건 접수=공유, 대기=미공유
        if (tbOrder.OrderState == "접수" && tbOrder.Share) status |= CEnum_Cg24OrderStatus.공유;
        if (tbOrder.DtReserve != null) status |= CEnum_Cg24OrderStatus.예약;
        if (tbOrder.DeliverType == "긴급") status |= CEnum_Cg24OrderStatus.긴급;
        if (tbOrder.DeliverType == "왕복") status |= CEnum_Cg24OrderStatus.왕복;
        if (tbOrder.DeliverType == "경유") status |= CEnum_Cg24OrderStatus.경유;

        return status;
    }

    /// <summary>
    /// 차량톤수에 따른 라디오버튼 좌표 반환
    /// </summary>
    private StdResult_Point GetCarWeightWithPoint(string sCarType, string sCarWeight)
    {
        switch (sCarType)
        {
            case "다마": return new StdResult_Point(m_FileInfo.접수등록Wnd_톤수RdoBtns_ptRel0C3);
            case "라보": return new StdResult_Point(m_FileInfo.접수등록Wnd_톤수RdoBtns_ptRel0C5);
            case "트럭":
                switch (sCarWeight)
                {
                    case "1t":
                    case "1t화물": return new StdResult_Point(m_FileInfo.접수등록Wnd_톤수RdoBtns_ptRel1C0);

                    case "1.4t":
                    case "1.4t화물": return new StdResult_Point(m_FileInfo.접수등록Wnd_톤수RdoBtns_ptRel1C4);

                    case "2.5t": return new StdResult_Point(m_FileInfo.접수등록Wnd_톤수RdoBtns_ptRel2C5);
                    case "3.5t": return new StdResult_Point(m_FileInfo.접수등록Wnd_톤수RdoBtns_ptRel3C5);
                    case "5t": return new StdResult_Point(m_FileInfo.접수등록Wnd_톤수RdoBtns_ptRel5);
                    case "8t": return new StdResult_Point(m_FileInfo.접수등록Wnd_톤수RdoBtns_ptRel8);
                    case "11t": return new StdResult_Point(m_FileInfo.접수등록Wnd_톤수RdoBtns_ptRel11);
                    case "14t": return new StdResult_Point(m_FileInfo.접수등록Wnd_톤수RdoBtns_ptRel14);
                    case "15t": return new StdResult_Point(m_FileInfo.접수등록Wnd_톤수RdoBtns_ptRel15);
                    case "18t": return new StdResult_Point(m_FileInfo.접수등록Wnd_톤수RdoBtns_ptRel18);
                    case "25t": return new StdResult_Point(m_FileInfo.접수등록Wnd_톤수RdoBtns_ptRel25);

                    default: return new StdResult_Point($"모르는 톤수[{sCarWeight}]", "GetCarWeightWithPoint_01");
                }
            default: return new StdResult_Point($"모르는 차량종류[{sCarType}]", "GetCarWeightWithPoint_02");
        }
    }

    /// <summary>
    /// 인성 트럭종류 → 화물24시 트럭종류 변환
    /// </summary>
    private StdResult_String GetTruckDetailStringFromInsung(string sTruckDetail)
    {
        switch (sTruckDetail)
        {
            // 다른 텍스트
            case "카고/윙": return new StdResult_String("카/윙");
            case "플러스카고": return new StdResult_String("플러스카");
            case "리프트카고": return new StdResult_String("리프트");
            case "플축리": return new StdResult_String("플축카리");
            case "축윙": return new StdResult_String("윙축");
            case "리프트호루": return new StdResult_String("리프트호");

            // 없는 차량종류
            case "자바라":
            case "리프트자바라":
            case "냉동플축리":
            case "냉장플축리":
            case "평카":
            case "로브이":
            case "츄레라":
            case "로베드":
            case "사다리":
            case "초장축":
                return new StdResult_String($"화물24시에는 없는 트럭종류[{sTruckDetail}]", "GetTruckDetailStringFromInsung_01");

            // 같은 텍스트 (전체, 카고, 축카고, 플축카고, 윙바디, 탑, 호루, 냉동탑, 냉장탑 등)
            default: return new StdResult_String(sTruckDetail);
        }
    }

    /// <summary>
    /// 운송비구분에 따른 라디오버튼 좌표 반환
    /// </summary>
    private StdResult_Point GetFeeTypePoint(string sFeeType)
    {
        switch (sFeeType)
        {
            case "선불":
            case "착불": return new StdResult_Point(m_FileInfo.접수등록Wnd_운송비RdoBtns_ptRel선착불);

            case "신용":
            case "송금": return new StdResult_Point(m_FileInfo.접수등록Wnd_운송비RdoBtns_ptRel인수증);

            case "카드": return new StdResult_Point(m_FileInfo.접수등록Wnd_운송비RdoBtns_ptRel카드);

            // case "수수료확인": return new StdResult_Point(m_FileInfo.접수등록Wnd_운송비RdoBtns_ptRel수수료확인);

            default: return new StdResult_Point($"모르는 요금타입[{sFeeType}]", "GetFeeTypePoint_01");
        }
    }

    /// <summary>
    /// 핸들 캡션에서 최대적재량 계산 (1.1배, 소수점 2자리)
    /// </summary>
    private static string GetMaxCargoWeightString(IntPtr hWnd)
    {
        string sCaption = Std32Window.GetWindowCaption(hWnd);
        if (float.TryParse(sCaption, out float fWeight))
        {
            float fMax = fWeight * 1.1f;
            return fMax.ToString("F2");
        }
        return sCaption; // 변환 실패 시 원본 반환
    }

    /// <summary>
    /// 오더상태 체크박스 일괄 설정 (Flags enum 사용)
    /// </summary>
    private async Task<StdResult_Status> Set오더타입Async(IntPtr hWndTop, CEnum_Cg24OrderStatus status, CancelTokenControl ctrl)
    {
        var checkItems = new (CEnum_Cg24OrderStatus flag, Draw.Point pt, string name)[]
        {
            (CEnum_Cg24OrderStatus.공유, m_FileInfo.접수등록Wnd_배송ChkBoxes_ptRel공유, "공유"),
            (CEnum_Cg24OrderStatus.중요오더, m_FileInfo.접수등록Wnd_배송ChkBoxes_ptRel중요오더, "중요오더"),
            (CEnum_Cg24OrderStatus.예약, m_FileInfo.접수등록Wnd_배송ChkBoxes_ptRel예약, "예약"),
            (CEnum_Cg24OrderStatus.긴급, m_FileInfo.접수등록Wnd_배송ChkBoxes_ptRel긴급, "긴급"),
            (CEnum_Cg24OrderStatus.왕복, m_FileInfo.접수등록Wnd_배송ChkBoxes_ptRel왕복, "왕복"),
            (CEnum_Cg24OrderStatus.경유, m_FileInfo.접수등록Wnd_배송ChkBoxes_ptRel경유, "경유"),
        };

        foreach (var (flag, pt, name) in checkItems)
        {
            await ctrl.WaitIfPausedOrCancelledAsync();

            IntPtr hWnd = Std32Window.GetWndHandle_FromRelDrawPt(hWndTop, pt);
            if (hWnd == IntPtr.Zero)
                return new StdResult_Status(StdResult.Fail, $"{name} CheckBox 핸들 획득 실패", "Set오더상태StatusAsync_00");

            bool shouldCheck = status.HasFlag(flag);
            var result = await Simulation_Mouse.SetCheckBtnStatusAsync(hWnd, shouldCheck);
            if (result.Result != StdResult.Success)
                return new StdResult_Status(StdResult.Fail, $"{name} 체크박스 설정 실패", "Set오더상태StatusAsync_01");

            Debug.WriteLine($"[{m_Context.AppName}] {name} 체크박스 설정 완료 (목표={shouldCheck})");
        }

        return new StdResult_Status(StdResult.Success);
    }

    /// <summary>
    /// 주소 검색창 열기 → 검색 → 선택 헬퍼
    /// </summary>
    private async Task<StdResult_Status> SearchAndSelectAddressAsync(
        IntPtr hWndPopup, Draw.Point ptRel조회버튼, string searchAddr, string fieldName, CancelTokenControl ctrl)
    {
        try
        {
            await ctrl.WaitIfPausedOrCancelledAsync();

            // 1. 조회버튼 클릭
            IntPtr hWnd조회버튼 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, ptRel조회버튼);
            if (hWnd조회버튼 == IntPtr.Zero)
            {
                return new StdResult_Status(StdResult.Fail,
                    $"{fieldName} 조회버튼을 찾을 수 없습니다.",
                    "SearchAndSelectAddressAsync_01");
            }

            await Std32Mouse_Post.MousePostAsync_ClickLeft(hWnd조회버튼);
            Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 조회버튼 클릭");

            // 2. 주소검색창 찾기 (최대 50회 대기)
            IntPtr hWndSearch = IntPtr.Zero;
            for (int i = 0; i < c_nRepeatMany; i++)
            {
                await Task.Delay(c_nWaitNormal, ctrl.Token);
                hWndSearch = Std32Window.FindMainWindow_NotTransparent(
                    m_Splash.TopWnd_uProcessId, m_FileInfo.주소검색Wnd_TopWnd_sWndName);

                if (hWndSearch != IntPtr.Zero)
                {
                    string className = Std32Window.GetWindowClassName(hWndSearch);
                    if (className == m_FileInfo.주소검색Wnd_TopWnd_sClassName)
                        break;
                    hWndSearch = IntPtr.Zero;
                }
            }

            if (hWndSearch == IntPtr.Zero)
            {
                return new StdResult_Status(StdResult.Fail,
                    $"{fieldName} 주소검색창이 열리지 않았습니다.",
                    "SearchAndSelectAddressAsync_02");
            }

            Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 주소검색창 열림: {hWndSearch:X}");

            // 3. 검색어 입력
            IntPtr hWnd검색어 = Std32Window.GetWndHandle_FromRelDrawPt(hWndSearch, m_FileInfo.주소검색Wnd_Search_ptRel검색어);
            if (hWnd검색어 == IntPtr.Zero)
            {
                return new StdResult_Status(StdResult.Fail,
                    $"{fieldName} 검색어 입력창을 찾을 수 없습니다.",
                    "SearchAndSelectAddressAsync_03");
            }

            await OfrWork_Common.WriteEditBox_ToHndleAsyncWait(hWnd검색어, searchAddr);
            Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 검색어 입력: {searchAddr}");

            // 4. 조회버튼 클릭
            IntPtr hWnd검색조회 = Std32Window.GetWndHandle_FromRelDrawPt(hWndSearch, m_FileInfo.주소검색Wnd_Search_ptRel조회버튼);
            if (hWnd검색조회 == IntPtr.Zero)
            {
                return new StdResult_Status(StdResult.Fail,
                    $"{fieldName} 검색 조회버튼을 찾을 수 없습니다.",
                    "SearchAndSelectAddressAsync_04");
            }

            await Std32Mouse_Post.MousePostAsync_ClickLeft(hWnd검색조회);
            await Task.Delay(c_nWaitLong, ctrl.Token); // 검색 결과 대기
            Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 검색 조회버튼 클릭");

            // 5. 결과 선택 (첫 번째 행 더블클릭)
            IntPtr hWndDg = Std32Window.GetWndHandle_FromRelDrawPt(hWndSearch, m_FileInfo.주소검색Wnd_Datagrid_ptRelChk);
            if (hWndDg == IntPtr.Zero)
            {
                return new StdResult_Status(StdResult.Fail,
                    $"{fieldName} 검색결과 Datagrid를 찾을 수 없습니다.",
                    "SearchAndSelectAddressAsync_05");
            }

            // 첫 번째 행 더블클릭
            await Simulation_Mouse.SafeMouseSend_DblClickLeft_ptRelAsync(hWndDg, m_FileInfo.주소검색Wnd_Datagrid_rcRelFirstRow.Location);
            Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 첫 번째 행 더블클릭");

            // 6. 주소검색창 닫힘 대기
            for (int i = 0; i < c_nRepeatMany; i++)
            {
                await Task.Delay(c_nWaitNormal, ctrl.Token);
                if (!Std32Window.IsWindow(hWndSearch))
                {
                    Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 주소검색창 닫힘 확인");
                    return new StdResult_Status(StdResult.Success);
                }
            }

            // 창이 안 닫히면 닫기 버튼 클릭 시도
            IntPtr hWnd닫기 = Std32Window.GetWndHandle_FromRelDrawPt(hWndSearch, m_FileInfo.주소검색Wnd_Search_ptRel닫기버튼);
            if (hWnd닫기 != IntPtr.Zero)
            {
                await Std32Mouse_Post.MousePostAsync_ClickLeft(hWnd닫기);
                await Task.Delay(c_nWaitNormal, ctrl.Token);
            }

            return new StdResult_Status(StdResult.Success);
        }
        catch (Exception ex)
        {
            return new StdResult_Status(StdResult.Fail,
                StdUtil.GetExceptionMessage(ex),
                "SearchAndSelectAddressAsync_999");
        }
    }

    /// <summary>
    /// EditBox에 값 입력 및 검증 헬퍼 (INSUNG 패턴 참조)
    /// </summary>
    private async Task<StdResult_Status> WriteAndVerifyEditBoxAsync(IntPtr hWnd, string expectedValue, string fieldName, CancelTokenControl ctrl)
    {
        try
        {
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

            // 재시도 루프 (최대 3번)
            for (int i = 0; i < c_nRepeatShort; i++)
            {
                await ctrl.WaitIfPausedOrCancelledAsync();

                // 쓰기 (대기 포함 버전 사용)
                string writtenValue = await OfrWork_Common.WriteEditBox_ToHndleAsyncWait(hWnd, expectedValue);

                // 검증
                if (expectedValue == writtenValue)
                {
                    Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 입력 성공: \"{expectedValue}\"");
                    return new StdResult_Status(StdResult.Success);
                }

                Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 불일치 (재시도 {i + 1}/{c_nRepeatShort}): " +
                              $"예상=\"{expectedValue}\", 실제=\"{writtenValue}\"");

                await Task.Delay(c_nWaitLong, ctrl.Token);
            }

            // 최대 재시도 초과
            Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 입력 실패: 최대 재시도 초과");
            return new StdResult_Status(StdResult.Fail,
                $"{fieldName} 입력 검증 실패: 최대 재시도 초과",
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
    /// 버튼 클릭 후 윈도우 닫힘 대기
    /// - 확인창(TMessageForm) 처리 포함
    /// - ProcessId로 정확한 창 식별
    /// - bReceiptSave: 접수저장인 경우 보고창(OK) 처리
    /// </summary>
    private async Task<bool> ClickNWaitWindowClosedAsync(IntPtr hWndClick, IntPtr hWndOrg, CancelTokenControl ctrl, bool bReceiptSave)
    {
        const string ConfirmDlg_sClassName = "TMessageForm";
        const string ConfirmDlg_sCaption = "Information";
        const string ConfirmBtn_sClassName = "TButton";
        const string ConfirmBtn_sCaption = "&Yes";

        //const string ReportDlg_sClassName = "TMessageForm";
        const string ReportBtn_sCaption = "OK";

        await ctrl.WaitIfPausedOrCancelledAsync();

        // 1. 저장 버튼 클릭
        await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndClick);
        Debug.WriteLine($"[{m_Context.AppName}] 저장 버튼 클릭");

        // 2. 확인창(Yes) 기다렸다 처리
        bool bConfirmHandled = false;
        for (int j = 0; j < c_nRepeatVeryMany; j++)
        {
            try
            {
                await Task.Delay(c_nWaitShort, ctrl.Token);
                IntPtr hWndConfirm = Std32Window.FindMainWindow(m_Splash.TopWnd_uProcessId, ConfirmDlg_sClassName, ConfirmDlg_sCaption);
                if (hWndConfirm != IntPtr.Zero)
                {
                    Debug.WriteLine($"[{m_Context.AppName}] 확인창 발견: {hWndConfirm:X}");
                    IntPtr hWndYes = Std32Window.FindChildWindow(hWndConfirm, ConfirmBtn_sClassName, ConfirmBtn_sCaption);
                    if (hWndYes != IntPtr.Zero)
                    {
                        Debug.WriteLine($"[{m_Context.AppName}] Yes 버튼 클릭");
                        await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndYes);
                        bConfirmHandled = true;
                        break;
                    }
                }
            }
            catch { /* 창 열거 중 예외 무시 */ }
        }
        if (!bConfirmHandled)
        {
            Debug.WriteLine($"[{m_Context.AppName}] 확인창 처리 실패");
            return false;
        }

        // 3. 접수저장이면 보고창(OK) 기다렸다 처리
        if (bReceiptSave)
        {
            for (int j = 0; j < c_nRepeatMany; j++)
            {
                try
                {
                    await Task.Delay(c_nWaitShort, ctrl.Token);
                    IntPtr hWndReport = Std32Window.FindMainWindow(m_Splash.TopWnd_uProcessId, ConfirmDlg_sClassName, ConfirmDlg_sCaption);
                    if (hWndReport != IntPtr.Zero)
                    {
                        Debug.WriteLine($"[{m_Context.AppName}] 보고창 발견: {hWndReport:X}");
                        IntPtr hWndOk = Std32Window.FindChildWindow(hWndReport, ConfirmBtn_sClassName, ReportBtn_sCaption);
                        if (hWndOk != IntPtr.Zero)
                        {
                            Debug.WriteLine($"[{m_Context.AppName}] OK 버튼 클릭");
                            await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndOk);
                            break;
                        }
                    }
                }
                catch { /* 창 열거 중 예외 무시 */ }
            }
        }

        // 4. 메인창 사라지기 기다림
        bool bMainClosed = false;
        for (int j = 0; j < c_nRepeatVeryMany; j++)
        {
            await Task.Delay(c_nWaitShort, ctrl.Token);
            if (!Std32Window.IsWindow(hWndOrg))
            {
                Debug.WriteLine($"[{m_Context.AppName}] 등록창 닫힘 확인");
                bMainClosed = true;
                break;
            }
        }
        if (!bMainClosed)
        {
            Debug.WriteLine($"[{m_Context.AppName}] 등록창 닫힘 실패");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 첫 로우 클릭 및 선택 검증 (명도 변화로 판단)
    /// </summary>
    private async Task<bool> ClickFirstRowAsync(CancelTokenControl ctrl)
    {
        const int COL_FOR_CLICK = 3; // 처리시간 컬럼 (OFR 컬럼 피함)
        const int COL_FOR_VERIFY = 1; // 상태 컬럼 (선택 시 명도 변화 확인용)
        const int DATA_ROW_INDEX = 0; // 첫 번째 데이터 로우 (헤더 바로 아래)

        try
        {
            Draw.Rectangle rectClickCell = m_RcptPage.DG오더_rcRelCells[COL_FOR_CLICK, DATA_ROW_INDEX];
            Draw.Rectangle rectVerifyCell = m_RcptPage.DG오더_rcRelCells[COL_FOR_VERIFY, DATA_ROW_INDEX];
            Draw.Point ptClick = StdUtil.GetDrawPoint(rectClickCell, 3, 3);

            // 검증용 셀 배경 좌표 (텍스트 피함)
            Draw.Point ptVerify = new Draw.Point(rectVerifyCell.X + 2, rectVerifyCell.Y + 2);

            // 클릭 전 명도 측정
            int brightBefore = OfrService.GetPixelBrightnessFrmWndHandle(m_RcptPage.DG오더_hWnd, ptVerify);
            Debug.WriteLine($"[{m_Context.AppName}] 클릭 전 명도: {brightBefore}");

            for (int i = 1; i <= c_nRepeatShort; i++)
            {
                await ctrl.WaitIfPausedOrCancelledAsync();
                Debug.WriteLine($"[{m_Context.AppName}] 첫 로우 클릭 시도 {i}/{c_nRepeatShort}");

                await Std32Mouse_Post.MousePostAsync_ClickLeft_ptRel(m_RcptPage.DG오더_hWnd, ptClick);

                // 명도 변화 대기 (어두워지면 선택됨)
                for (int j = 0; j < c_nRepeatMany; j++)
                {
                    await Task.Delay(c_nWaitShort, ctrl.Token);
                    int brightAfter = OfrService.GetPixelBrightnessFrmWndHandle(m_RcptPage.DG오더_hWnd, ptVerify);

                    if (brightAfter < brightBefore - 5)
                    {
                        Debug.WriteLine($"[{m_Context.AppName}] 첫 로우 선택 성공 (명도: {brightBefore} → {brightAfter})");
                        return true;
                    }
                }

                await Task.Delay(c_nWaitNormal, ctrl.Token);
            }

            Debug.WriteLine($"[{m_Context.AppName}] 첫 로우 선택 실패");
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{m_Context.AppName}] ClickFirstRowAsync 예외: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 지정된 로우의 화물번호 OFR
    /// - 셀 캡처 후 단음소 OFR
    /// - 화물24시는 RGB 반전 불필요
    /// - 마지막 재시도에서만 수동 입력 대화상자 (bEdit=true)
    /// </summary>
    private async Task<StdResult_String> Get화물번호Async(int rowIndex, CancelTokenControl ctrl, int retryCount = c_nRepeatShort)
    {
        try
        {
            const int COL_화물번호 = 2;

            Draw.Rectangle rectSeqnoCell = m_RcptPage.DG오더_rcRelCells[COL_화물번호, rowIndex];
            Debug.WriteLine($"[{m_Context.AppName}] 화물번호 OFR - rowIndex={rowIndex}, 셀위치={rectSeqnoCell}");

            for (int i = 1; i <= retryCount; i++)
            {
                await ctrl.WaitIfPausedOrCancelledAsync();
                Debug.WriteLine($"[{m_Context.AppName}] ===== 화물번호 OFR 시도 {i}/{retryCount} =====");

                Draw.Bitmap bmpCell = OfrService.CaptureScreenRect_InWndHandle(m_RcptPage.DG오더_hWnd, rectSeqnoCell);
                if (bmpCell == null)
                {
                    Debug.WriteLine($"[{m_Context.AppName}] 화물번호 셀 캡처 실패 (시도 {i}/{retryCount})");
                    if (i < retryCount) await Task.Delay(c_nWaitLong, ctrl.Token);
                    continue;
                }

                try
                {
                    // 마지막 시도에서만 bEdit=true (수동 입력 대화상자)
                    StdResult_String resultSeqno = await OfrWork_Common.OfrStr_SeqCharAsync(bmpCell, 0.7, i == retryCount);

                    // ☒ 없는 완전한 결과만 성공
                    if (!string.IsNullOrEmpty(resultSeqno.strResult) && !resultSeqno.strResult.Contains('☒'))
                    {
                        Debug.WriteLine($"[{m_Context.AppName}] 화물번호 획득 성공: '{resultSeqno.strResult}' (시도 {i}/{retryCount})");
                        return new StdResult_String(resultSeqno.strResult);
                    }
                    else
                    {
                        Debug.WriteLine($"[{m_Context.AppName}] OFR 실패: '{resultSeqno.strResult ?? resultSeqno.sErr}' (시도 {i}/{retryCount})");
                    }
                }
                finally
                {
                    bmpCell?.Dispose();
                }

                if (i < retryCount) await Task.Delay(c_nWaitLong, ctrl.Token);
            }

            return new StdResult_String($"화물번호 OFR 실패 ({retryCount}회 시도)", "Get화물번호Async_99");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{m_Context.AppName}] Get화물번호Async 예외: {ex.Message}");
            return new StdResult_String(StdUtil.GetExceptionMessage(ex), "Get화물번호Async_999");
        }
    }

    /// <summary>
    /// 신규 주문 등록 확인 (Kai에만 존재, 화물24시에 없음)
    /// </summary>
    public async Task<CommonResult_AutoAllocProcess> CheckCg24OrderAsync_AssumeKaiNewOrder(AutoAllocModel item, CancelTokenControl ctrl)
    {
        // Cancel/Pause 체크 - 긴 작업 전
        await ctrl.WaitIfPausedOrCancelledAsync();

        string kaiState = item.NewOrder.OrderState;
        Debug.WriteLine($"[{m_Context.AppName}] CheckCg24OrderAsync_AssumeKaiNewOrder: KeyCode={item.KeyCode}, kaiState={kaiState}");

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
                Debug.WriteLine($"[{m_Context.AppName}] 미구현 상태: {kaiState}");
                return CommonResult_AutoAllocProcess.FailureAndDiscard($"미구현 상태: {kaiState}", "CheckCg24OrderAsync_AssumeKaiNewOrder_TODO");

            default:
                Debug.WriteLine($"[{m_Context.AppName}] 알 수 없는 Kai 주문 상태: {kaiState}");
                return CommonResult_AutoAllocProcess.FailureAndDiscard($"알 수 없는 Kai 주문 상태: {kaiState}", "CheckCg24OrderAsync_AssumeKaiNewOrder_800");
        }
    }
    #endregion

    #region Utility Methods
    /// <summary>
    /// 접수등록 페이지가 초기화되었는지 확인
    /// </summary>
    public bool IsInitialized()
    {
        return m_RcptPage.TopWnd_hWnd != IntPtr.Zero;
    }

    /// <summary>
    /// 접수등록 페이지 핸들 가져오기
    /// </summary>
    public IntPtr GetHandle()
    {
        return m_RcptPage.TopWnd_hWnd;
    }
    #endregion

    #region Test Methods
    /// <summary>
    /// DG오더 셀 영역 시각화 테스트
    /// TransparantWnd를 사용하여 모든 셀 영역을 두께 1로 그리고 MsgBox 표시
    /// </summary>
    public void Test_DrawAllCellRects()
    {
        try
        {
            Debug.WriteLine($"[Cargo24/Test] Test_DrawAllCellRects 시작");

            // 1. DG오더 핸들 체크
            if (m_RcptPage.DG오더_hWnd == IntPtr.Zero)
            {
                System.Windows.MessageBox.Show("DG오더_hWnd가 초기화되지 않았습니다.", "오류");
                return;
            }

            // 2. Cell Rect 배열 체크
            if (m_RcptPage.DG오더_rcRelCells == null)
            {
                System.Windows.MessageBox.Show("DG오더_rcRelCells가 초기화되지 않았습니다.", "오류");
                return;
            }

            int colCount = m_RcptPage.DG오더_rcRelCells.GetLength(0);
            int rowCount = m_RcptPage.DG오더_rcRelCells.GetLength(1);
            Debug.WriteLine($"[Cargo24/Test] Cell 배열: {rowCount}행 x {colCount}열");

            // 3. TransparantWnd 오버레이 생성 (DG오더 위치 기준)
            TransparantWnd.CreateOverlay(m_RcptPage.DG오더_hWnd);
            TransparantWnd.ClearBoxes();

            // 4-1. 헤더 셀 그리기 (두께 1, 파란색)
            int cellCount = 0;
            for (int col = 0; col < colCount; col++)
            {
                var rcData = m_RcptPage.DG오더_rcRelCells[col, 0]; // 첫 데이터 로우에서 x, width 가져옴
                Draw.Rectangle rcHeader = new Draw.Rectangle(rcData.X, 4, rcData.Width, HEADER_HEIGHT - 8);
                TransparantWnd.DrawBoxAsync(rcHeader, strokeColor: Colors.Blue, thickness: 1);
                cellCount++;
            }

            // 4-2. 데이터 셀 그리기 (두께 1, 빨간색)
            for (int row = 0; row < rowCount; row++)
            {
                for (int col = 0; col < colCount; col++)
                {
                    Draw.Rectangle rc = m_RcptPage.DG오더_rcRelCells[col, row];
                    TransparantWnd.DrawBoxAsync(rc, strokeColor: Colors.Red, thickness: 1);
                    cellCount++;
                }
            }

            Debug.WriteLine($"[Cargo24/Test] {cellCount}개 셀 영역 그리기 완료");

            // 5. MsgBox 표시 (확인 후 오버레이 삭제)
            System.Windows.MessageBox.Show(
                $"화물24시 DG오더 셀 영역 테스트\n\n" +
                $"행: {rowCount}\n" +
                $"열: {colCount}\n" +
                $"총 셀: {cellCount}개\n\n" +
                $"확인을 누르면 오버레이가 제거됩니다.",
                "셀 영역 테스트");

            // 6. 오버레이 삭제
            TransparantWnd.DeleteOverlay();
            Debug.WriteLine($"[Cargo24/Test] Test_DrawAllCellRects 완료");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Cargo24/Test] 예외 발생: {ex.Message}");
            System.Windows.MessageBox.Show($"테스트 중 오류 발생:\n{ex.Message}", "오류");
            TransparantWnd.DeleteOverlay();
        }
    }
    #endregion

    #region 스크롤 함수
    /// <summary>
    /// Page Down 스크롤 (VK_NEXT)
    /// </summary>
    public async Task ScrollPageDownAsync(int count = 1, int delayMs = 100)
    {
        for (int i = 0; i < count; i++)
        {
            Std32Key_Msg.KeyPost_Click(m_RcptPage.DG오더_hWnd, StdCommon32.VK_NEXT);
            if (i < count - 1) await Task.Delay(delayMs);
        }
    }

    /// <summary>
    /// Page Up 스크롤 (VK_PRIOR)
    /// </summary>
    public async Task ScrollPageUpAsync(int count = 1, int delayMs = 100)
    {
        for (int i = 0; i < count; i++)
        {
            Std32Key_Msg.KeyPost_Click(m_RcptPage.DG오더_hWnd, StdCommon32.VK_PRIOR);
            if (i < count - 1) await Task.Delay(delayMs);
        }
    }

    /// <summary>
    /// Row Down 스크롤 (VK_DOWN)
    /// </summary>
    public async Task ScrollRowDownAsync(int count = 1, int delayMs = 100)
    {
        for (int i = 0; i < count; i++)
        {
            Std32Key_Msg.KeyPost_Click(m_RcptPage.DG오더_hWnd, StdCommon32.VK_DOWN);
            if (i < count - 1) await Task.Delay(delayMs);
        }
    }

    /// <summary>
    /// Row Up 스크롤 (VK_UP)
    /// </summary>
    public async Task ScrollRowUpAsync(int count = 1, int delayMs = 100)
    {
        for (int i = 0; i < count; i++)
        {
            Std32Key_Msg.KeyPost_Click(m_RcptPage.DG오더_hWnd, StdCommon32.VK_UP);
            if (i < count - 1) await Task.Delay(delayMs);
        }
    }
    #endregion

    #region 로우 셀렉트 관련
    /// <summary>
    /// 특정 로우가 셀렉트되었는지 확인 (순번 컬럼에 숫자가 없으면 셀렉트됨)
    /// </summary>
    /// <param name="rowIndex">로우 인덱스 (0-based)</param>
    /// <returns>true: 셀렉트됨, false: 셀렉트 안됨, null: 판단 불가</returns>
    public async Task<bool?> IsRowSelectedAsync(int rowIndex)
    {
        var rcCell = m_RcptPage.DG오더_rcRelCells[0, rowIndex]; // [col=0, row] 순번 컬럼
        var bmp = OfrService.CaptureScreenRect_InWndHandle(m_RcptPage.DG오더_hWnd, rcCell);
        if (bmp == null) return null;

        var result = await OfrWork_Common.OfrStr_SeqCharAsync(bmp, 0.7, bEdit: false);
        bmp.Dispose();

        if (string.IsNullOrEmpty(result.strResult)) return null;

        // 숫자가 있으면 셀렉트 안됨, 없으면 셀렉트됨 (화살표)
        string digits = new string(result.strResult.Where(char.IsDigit).ToArray());
        return string.IsNullOrEmpty(digits);
    }

    /// <summary>
    /// 특정 로우를 클릭하여 셀렉트
    /// </summary>
    /// <param name="rowIndex">로우 인덱스 (0-based)</param>
    public async Task SelectRowAsync(int rowIndex)
    {
        var rcCell = m_RcptPage.DG오더_rcRelCells[0, rowIndex]; // [col=0, row]
        Draw.Point ptClick = new Draw.Point(rcCell.Left + rcCell.Width / 2, rcCell.Top + rcCell.Height / 2);
        await Std32Mouse_Post.MousePostAsync_ClickLeft_ptRel(m_RcptPage.DG오더_hWnd, ptClick);
        await Task.Delay(50);
    }

    /// <summary>
    /// 첫 로우가 셀렉트되어 있는지 확인하고, 아니면 셀렉트
    /// </summary>
    public async Task EnsureFirstRowSelectedAsync()
    {
        bool? isSelected = await IsRowSelectedAsync(0);
        if (isSelected != true)
        {
            await SelectRowAsync(0);
        }
    }

    /// <summary>
    /// 마지막 로우가 셀렉트되어 있는지 확인하고, 아니면 셀렉트
    /// </summary>
    /// <param name="lastRowIndex">마지막 로우 인덱스 (0-based, 기본값 24)</param>
    public async Task EnsureLastRowSelectedAsync(int lastRowIndex = 24)
    {
        bool? isSelected = await IsRowSelectedAsync(lastRowIndex);
        if (isSelected != true)
        {
            await SelectRowAsync(lastRowIndex);
        }
    }
    #endregion

    #region 첫 로우 순번 읽기
    /// <summary>
    /// 현재 페이지의 첫 로우 순번을 읽습니다.
    /// 선택된 로우는 화살표가 표시되므로, 다른 로우를 읽어서 계산합니다.
    /// </summary>
    /// <param name="nValidRowCount">현재 페이지의 유효 로우 수 (총계가 rowCount보다 작으면 총계)</param>
    /// <returns>첫 로우 순번 (-1: 실패)</returns>
    public async Task<int> ReadFirstRowNumAsync(int nValidRowCount)
    {
        // 1건만 있는 경우 → 첫 로우 = 1
        if (nValidRowCount == 1)
        {
            Debug.WriteLine($"[Cargo24/ReadFirstRowNum] 1건만 있음 → 첫 로우 = 1");
            return 1;
        }

        // 여러 행이 있는 경우 → 숫자가 있는 셀을 찾아서 계산
        for (int y = 0; y < nValidRowCount; y++)
        {
            var rcCell = m_RcptPage.DG오더_rcRelCells[0, y]; // [col=0, row=y] 순번 컬럼
            var bmp = OfrService.CaptureScreenRect_InWndHandle(m_RcptPage.DG오더_hWnd, rcCell);
            if (bmp == null) continue;

            var result = await OfrWork_Common.OfrStr_SeqCharAsync(bmp, 0.7, bEdit: false);
            bmp.Dispose();

            if (string.IsNullOrEmpty(result.strResult)) continue;

            // 숫자만 추출
            string digits = new string(result.strResult.Where(char.IsDigit).ToArray());
            if (string.IsNullOrEmpty(digits)) continue; // 화살표 등 숫자가 아닌 경우 skip

            if (int.TryParse(digits, out int curNum))
            {
                // 첫 로우 순번 계산: curNum - y (y는 0-based index)
                int firstNum = curNum - y;
                Debug.WriteLine($"[Cargo24/ReadFirstRowNum] y={y}, curNum={curNum} → 첫 로우 = {firstNum}");
                return firstNum;
            }
        }

        Debug.WriteLine($"[Cargo24/ReadFirstRowNum] 유효한 순번을 찾지 못함");
        return -1;
    }

    /// <summary>
    /// 페이지별 예상 첫 로우 번호 계산 (0-based 페이지 인덱스)
    /// - 인성 로직 인용
    /// </summary>
    /// <param name="nTotRows">총 행 수</param>
    /// <param name="nRowsPerPage">페이지당 행 수</param>
    /// <param name="pageIdx">페이지 인덱스 (0-based)</param>
    /// <returns>예상 첫 로우 번호</returns>
    public static int GetExpectedFirstRowNum(int nTotRows, int nRowsPerPage, int pageIdx)
    {
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
    #endregion
}
#nullable restore
