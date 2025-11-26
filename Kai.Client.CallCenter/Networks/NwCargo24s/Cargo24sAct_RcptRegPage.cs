using System.Diagnostics;
using System.Linq;
using System.Windows.Media;
using Draw = System.Drawing;

using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;
using Kai.Common.NetDll_WpfCtrl.NetOFR;

using Kai.Client.CallCenter.Classes;
using Kai.Client.CallCenter.Windows;
using Kai.Client.CallCenter.OfrWorks;
using static Kai.Client.CallCenter.Classes.CommonVars;

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
        Debug.WriteLine($"[Cargo24sAct_RcptRegPage] 생성자 호출: AppName={m_Context.AppName}");
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
                Debug.WriteLine($"[Cargo24sAct_RcptRegPage] 버튼 찾음: {hWndTmp:X}, 텍스트={sTmp}");

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
            Debug.WriteLine($"[Cargo24sAct_RcptRegPage] StatusBtn 찾기 시작");

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

                m_RcptPage.DG오더_rcRelCells = new Draw.Rectangle[rowCount, columns];
                m_RcptPage.DG오더_ptRelChkRows = new Draw.Point[rowCount];

                for (int row = 0; row < rowCount; row++)
                {
                    int cellY = HEADER_HEIGHT + (row * rowHeight);

                    // Row check point (첫번째 컬럼 중앙)
                    m_RcptPage.DG오더_ptRelChkRows[row] = new Draw.Point(
                        listLW[0].nLeft + (listLW[0].nWidth / 2),
                        cellY + (rowHeight / 2)
                    );

                    // Cell rects (경계에서 계산하여 미세조정 적용)
                    for (int col = 0; col < columns; col++)
                    {
                        m_RcptPage.DG오더_rcRelCells[row, col] = new Draw.Rectangle(
                            listLW[col].nLeft + 1,
                            cellY,
                            listLW[col].nWidth - 2,
                            rowHeight
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
        Draw.Bitmap? bmpHeader = null;

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
                        Debug.WriteLine($"[Cargo24sAct_RcptRegPage] {buttonName}버튼 찾음: {hWnd:X}, 텍스트: {text}");
                        return (hWnd, null);
                    }
                }
                else
                {
                    Debug.WriteLine($"[Cargo24sAct_RcptRegPage] {buttonName}버튼 찾음: {hWnd:X}");
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
}
#nullable restore
