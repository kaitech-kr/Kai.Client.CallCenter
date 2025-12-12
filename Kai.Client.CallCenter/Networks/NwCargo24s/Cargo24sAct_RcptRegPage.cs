using Kai.Client.CallCenter.Classes;
using Kai.Client.CallCenter.Classes.Class_Master;
using Kai.Client.CallCenter.OfrWorks;
using Kai.Client.CallCenter.Windows;
using Kai.Common.NetDll_WpfCtrl.NetOFR;
using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Services;
using Microsoft.VisualBasic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using static Kai.Client.CallCenter.Classes.CommonVars;
using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgBox;
using Draw = System.Drawing;

namespace Kai.Client.CallCenter.Networks.NwCargo24s;
#nullable disable

/// <summary>
/// 화물24시 접수등록 페이지 초기화 및 제어 담당 클래스
/// Context 패턴 사용: Cargo24Context를 통해 모든 정보에 접근
/// </summary>
public partial class Cargo24sAct_RcptRegPage
{
    #region Variables
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
    // 컬럼 인덱스
    public const int c_nCol순번 = 0;
    public const int c_nCol상태 = 1;
    public const int c_nCol화물번호 = 2;
    public const int c_nColForClick = 3;  // 클릭용 컬럼 (처리시간)

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

    #region 초기화용 함수들
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

            // 조회 버튼 밝기 저장 (로딩 완료 판단용)
            m_RcptPage.CmdBtn_nBrightness조회 = OfrService.GetPixelBrightnessFrmWndHandle(
                hWnd조회, m_FileInfo.접수등록Page_CmdBtn_ptChkRel조회L);

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
                    Draw.Rectangle rcTmp = new Draw.Rectangle(listLW[i].nLeft, TARGET_ROW, listLW[i].nWidth, OFR_HEIGHT);
                    var result = await OfrWork_Common.OfrStr_ComplexCharSetAsync(bmpDG, rcTmp, bInvertRgb: false, bTextSave: true, 0.9, bEdit: bEdit);

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

                var resultChSet = await OfrWork_Common.OfrStr_ComplexCharSetAsync(bmpHeader, rcTmp, bInvertRgb: false, bTextSave: true, 0.9, bEdit: bEdit);

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

    #region 자동배차 - Kai신규 관련함수들
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
            case "접수": // 신규 주문 팝업창 열기 → 입력 → 닫기 → 성공 확인               
                return await OpenNewOrderPopupAsync(item, ctrl);

            case "취소": // 무시 - 비적재
            case "대기":
                return CommonResult_AutoAllocProcess.SuccessAndDestroy(item);

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

    /// <summary>
    /// 신규 버튼 클릭 → 팝업창 열기 → RegistOrderToPopupAsync 호출
    /// </summary>
    public async Task<CommonResult_AutoAllocProcess> OpenNewOrderPopupAsync(AutoAllocModel item, CancelTokenControl ctrl)
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
            else return CommonResult_AutoAllocProcess.FailureAndDiscard(
                "신규 버튼 클릭 후 팝업창이 열리지 않음", "OpenNewOrderPopupAsync_01");
        }
        catch (Exception ex)
        {
            return CommonResult_AutoAllocProcess.FailureAndDiscard(StdUtil.GetExceptionMessage(ex), "OpenNewOrderPopupAsync_999");
        }
    }

    /// <summary>
    /// 팝업창에 주문 정보 입력 및 등록
    /// - TopMost 설정 (포커스 유지)
    /// - TODO: 입력 작업
    /// - 닫기 버튼 클릭
    /// - 창 닫힘 확인 (성공 판단)
    /// </summary>
    public async Task<CommonResult_AutoAllocProcess> RegistOrderToPopupAsync(AutoAllocModel item, IntPtr hWndPopup, CancelTokenControl ctrl)
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
                return CommonResult_AutoAllocProcess.FailureAndDiscard(
                    "상차지 주소가 없습니다.", "RegistOrderToPopupAsync_Pre01");

            // 하차지 주소 체크 (상세주소 기준)
            if (string.IsNullOrEmpty(tbOrder.DestDetailAddr))
                return CommonResult_AutoAllocProcess.FailureAndDiscard(
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
                return CommonResult_AutoAllocProcess.FailureAndDiscard(result.sErr, result.sPos);

            // 의뢰자 전화
            IntPtr hWnd의뢰자전화 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_의뢰자_ptRel고객전화);
            result = await WriteAndVerifyEditBoxAsync(hWnd의뢰자전화, tbOrder.CallTelNo ?? "", "의뢰자_전화", ctrl);
            if (result.Result != StdResult.Success)
                return CommonResult_AutoAllocProcess.FailureAndDiscard(result.sErr, result.sPos);

            // 의뢰자 담당자
            IntPtr hWnd의뢰자담당 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_의뢰자_ptRel담당자);
            result = await WriteAndVerifyEditBoxAsync(hWnd의뢰자담당, tbOrder.CallChargeName ?? "", "의뢰자_담당자", ctrl);
            if (result.Result != StdResult.Success)
                return CommonResult_AutoAllocProcess.FailureAndDiscard(result.sErr, result.sPos);

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
                return CommonResult_AutoAllocProcess.FailureAndDiscard(result.sErr, result.sPos);

            // 상차지 위치 (상세주소)
            IntPtr hWnd상차위치 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_상차지_ptRel위치);
            result = await WriteAndVerifyEditBoxAsync(hWnd상차위치, tbOrder.StartDetailAddr ?? "", "상차지_위치", ctrl);
            if (result.Result != StdResult.Success)
                return CommonResult_AutoAllocProcess.FailureAndDiscard(result.sErr, result.sPos);

            // 상차지 고객명
            IntPtr hWnd상차고객명 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_상차지_ptRel고객명);
            result = await WriteAndVerifyEditBoxAsync(hWnd상차고객명, tbOrder.StartCustName ?? "", "상차지_고객명", ctrl);
            if (result.Result != StdResult.Success)
                return CommonResult_AutoAllocProcess.FailureAndDiscard(result.sErr, result.sPos);

            // 상차지 전화
            IntPtr hWnd상차전화 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_상차지_ptRel전화);
            result = await WriteAndVerifyEditBoxAsync(hWnd상차전화, tbOrder.StartTelNo ?? "", "상차지_전화", ctrl);
            if (result.Result != StdResult.Success)
                return CommonResult_AutoAllocProcess.FailureAndDiscard(result.sErr, result.sPos);

            // 상차지 부서명
            IntPtr hWnd상차부서 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_상차지_ptRel부서명);
            result = await WriteAndVerifyEditBoxAsync(hWnd상차부서, tbOrder.StartDeptName ?? "", "상차지_부서명", ctrl);
            if (result.Result != StdResult.Success)
                return CommonResult_AutoAllocProcess.FailureAndDiscard(result.sErr, result.sPos);

            // 상차지 담당자
            IntPtr hWnd상차담당 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_상차지_ptRel담당자);
            result = await WriteAndVerifyEditBoxAsync(hWnd상차담당, tbOrder.StartChargeName ?? "", "상차지_담당자", ctrl);
            if (result.Result != StdResult.Success)
                return CommonResult_AutoAllocProcess.FailureAndDiscard(result.sErr, result.sPos);

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
                return CommonResult_AutoAllocProcess.FailureAndDiscard(result.sErr, result.sPos);

            // 하차지 위치 (상세주소)
            IntPtr hWnd하차위치 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_하차지_ptRel위치);
            result = await WriteAndVerifyEditBoxAsync(hWnd하차위치, tbOrder.DestDetailAddr ?? "", "하차지_위치", ctrl);
            if (result.Result != StdResult.Success)
                return CommonResult_AutoAllocProcess.FailureAndDiscard(result.sErr, result.sPos);

            // 하차지 고객명
            IntPtr hWnd하차고객명 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_하차지_ptRel고객명);
            result = await WriteAndVerifyEditBoxAsync(hWnd하차고객명, tbOrder.DestCustName ?? "", "하차지_고객명", ctrl);
            if (result.Result != StdResult.Success)
                return CommonResult_AutoAllocProcess.FailureAndDiscard(result.sErr, result.sPos);

            // 하차지 전화
            IntPtr hWnd하차전화 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_하차지_ptRel전화);
            result = await WriteAndVerifyEditBoxAsync(hWnd하차전화, tbOrder.DestTelNo ?? "", "하차지_전화", ctrl);
            if (result.Result != StdResult.Success)
                return CommonResult_AutoAllocProcess.FailureAndDiscard(result.sErr, result.sPos);

            // 하차지 부서명
            IntPtr hWnd하차부서 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_하차지_ptRel부서명);
            result = await WriteAndVerifyEditBoxAsync(hWnd하차부서, tbOrder.DestDeptName ?? "", "하차지_부서명", ctrl);
            if (result.Result != StdResult.Success)
                return CommonResult_AutoAllocProcess.FailureAndDiscard(result.sErr, result.sPos);

            // 하차지 담당자
            IntPtr hWnd하차담당 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_하차지_ptRel담당자);
            result = await WriteAndVerifyEditBoxAsync(hWnd하차담당, tbOrder.DestChargeName ?? "", "하차지_담당자", ctrl);
            if (result.Result != StdResult.Success)
                return CommonResult_AutoAllocProcess.FailureAndDiscard(result.sErr, result.sPos);

            Debug.WriteLine($"[{m_Context.AppName}] 2. 하차지 정보 입력 완료");
            #endregion

            #region ===== 3. 배송타입 (체크박스 - 복수 선택 가능) =====
            Debug.WriteLine($"[{m_Context.AppName}] 3. 배송타입 입력... DeliverType={tbOrder.DeliverType}");

            // 오더번호, 오더상태를 얻으려면 여기에서...
            CEnum_Cg24OrderStatus status = Get오더타입FlagsFromKaiTable(tbOrder);

            result = await Set오더타입Async(hWndPopup, status, ctrl);
            if (result.Result != StdResult.Success) return CommonResult_AutoAllocProcess.FailureAndDiscard(result.sErr, result.sPos);

            Debug.WriteLine($"[{m_Context.AppName}] 3. 배송타입 입력 완료");
            #endregion

            #region ===== 4. 차량정보 =====
            Debug.WriteLine($"[{m_Context.AppName}] 4. 차량정보 입력... CarType={tbOrder.CarType}, CarWeight={tbOrder.CarWeight}, TruckDetail={tbOrder.TruckDetail}");

            // 4-1. 차량톤수 (라디오버튼)
            var ptTon = GetCarWeightWithPoint(tbOrder.CarType, tbOrder.CarWeight);
            if (!string.IsNullOrEmpty(ptTon.sErr))
                return CommonResult_AutoAllocProcess.FailureAndDiscard(ptTon.sErr, "RegistOrderToPopupAsync_04_01");

            IntPtr hWndTon = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, ptTon.ptResult);
            if (hWndTon == IntPtr.Zero)
                return CommonResult_AutoAllocProcess.FailureAndDiscard("톤수 라디오버튼 핸들 획득 실패", "RegistOrderToPopupAsync_04_02");

            result = await Simulation_Mouse.SetCheckBtnStatusAsync(hWndTon, true);
            if (result.Result != StdResult.Success)
                return CommonResult_AutoAllocProcess.FailureAndDiscard("톤수 라디오버튼 설정 실패", "RegistOrderToPopupAsync_04_03");

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
                return CommonResult_AutoAllocProcess.FailureAndDiscard(strTruck.sErr, "RegistOrderToPopupAsync_04_04");

            IntPtr hWndTruck = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_차종Combo_ptRel차종확인);
            if (hWndTruck == IntPtr.Zero)
                return CommonResult_AutoAllocProcess.FailureAndDiscard("차종 콤보박스 핸들 획득 실패", "RegistOrderToPopupAsync_04_05");

            Std32Window.SetWindowCaption(hWndTruck, strTruck.strResult);

            Debug.WriteLine($"[{m_Context.AppName}] 4-2. 차종 설정 완료: {tbOrder.TruckDetail} → {strTruck.strResult}");
            Debug.WriteLine($"[{m_Context.AppName}] 4. 차량정보 입력 완료");
            #endregion

            #region ===== 5. 운송비 =====
            Debug.WriteLine($"[{m_Context.AppName}] 5. 운송비 입력... FeeType={tbOrder.FeeType}, FeeTotal={tbOrder.FeeTotal}");
            var (_, feeResult) = await Set운송비Async(hWndPopup, tbOrder, false, ctrl);
            if (feeResult.Result != StdResult.Success) return CommonResult_AutoAllocProcess.FailureAndDiscard(feeResult.sErr, feeResult.sPos);
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
            // 저장 버튼 선택: 공유이면 접수저장, 아니면 대기저장
            bool bReceiptState = tbOrder.Share;
            Draw.Point ptRelSave = bReceiptState
                ? m_FileInfo.접수등록Wnd_CmnBtn_ptRel접수저장
                : m_FileInfo.접수등록Wnd_CmnBtn_ptRel대기저장;

            Debug.WriteLine($"[{m_Context.AppName}] 종료 작업... Share={tbOrder.Share}, 저장모드={(bReceiptState ? "접수저장" : "대기저장")}");

            // 저장 버튼 핸들 얻기
            IntPtr hWndSave = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, ptRelSave);
            if (hWndSave == IntPtr.Zero)
            {
                Debug.WriteLine($"[{m_Context.AppName}] 저장 버튼 핸들 못찾음");
                return CommonResult_AutoAllocProcess.FailureAndDiscard("저장 버튼 핸들 못찾음", "RegistOrderToPopupAsync_06_01");
            }

            // 저장 버튼 클릭 및 창 닫힘 확인
            bool bClosed = await SaveAndWaitClosedAsync(hWndSave, hWndPopup, "저장", ctrl);

            if (bClosed)
            {
                Debug.WriteLine($"[{m_Context.AppName}] 저장 완료, 팝업창 닫힘 확인");
                await Task.Delay(CommonVars.c_nWaitLong, ctrl.Token); // 창 닫힌 후 UI 안정화 대기

                // 1. 조회 버튼 클릭 (DB refresh)
                StdResult_Status resultQuery = await Click조회버튼Async(ctrl);
                if (resultQuery.Result != StdResult.Success)
                {
                    return CommonResult_AutoAllocProcess.FailureAndDiscard($"조회 실패: {resultQuery.sErr}", "RegistOrderToPopupAsync_06_03");
                }

                // 2. 첫 로우 클릭 및 선택 검증
                bool bClicked = await ClickFirstRowAsync(ctrl);
                if (!bClicked)
                {
                    return CommonResult_AutoAllocProcess.FailureAndDiscard("첫 로우 선택 실패", "RegistOrderToPopupAsync_06_04");
                }

                // 3. 화물번호 OFR
                StdResult_String resultSeqno = await Get화물번호Async(0, ctrl);
                if (string.IsNullOrEmpty(resultSeqno.strResult))
                {
                    return CommonResult_AutoAllocProcess.FailureAndDiscard($"화물번호 획득 실패: {resultSeqno.sErr}", "RegistOrderToPopupAsync_06_05");
                }

                Debug.WriteLine($"[{m_Context.AppName}] 주문 등록 완료 - 화물번호: {resultSeqno.strResult}");

                // 4. Kai DB의 Cargo24 필드 업데이트
                // 4-1. 사전 체크
                if (item.NewOrder.KeyCode <= 0)
                {
                    Debug.WriteLine($"[{m_Context.AppName}] KeyCode 없음 - Kai DB에 없는 주문");
                    return CommonResult_AutoAllocProcess.FailureAndDiscard("Kai DB에 없는 주문입니다", "RegistOrderToPopupAsync_06_06");
                }

                if (!string.IsNullOrEmpty(item.NewOrder.Cargo24))
                {
                    Debug.WriteLine($"[{m_Context.AppName}] 이미 등록된 화물번호: {item.NewOrder.Cargo24}");
                    return CommonResult_AutoAllocProcess.SuccessAndDestroy(item);
                }

                if (CommonVars.s_SrGClient == null || !CommonVars.s_SrGClient.m_bLoginSignalR)
                {
                    Debug.WriteLine($"[{m_Context.AppName}] SignalR 연결 안됨");
                    return CommonResult_AutoAllocProcess.FailureAndDiscard("서버 연결이 끊어졌습니다", "RegistOrderToPopupAsync_06_07");
                }

                // 4-2. 업데이트 실행
                item.NewOrder.Cargo24 = resultSeqno.strResult;

                StdResult_Int resultUpdate = await CommonVars.s_SrGClient.SrResult_Order_UpdateRowAsync_Today_WithRequestId(item.NewOrder);

                if (resultUpdate.nResult < 0 || !string.IsNullOrEmpty(resultUpdate.sErr))
                {
                    Debug.WriteLine($"[{m_Context.AppName}] Kai DB 업데이트 실패: {resultUpdate.sErr}");
                    return CommonResult_AutoAllocProcess.FailureAndDiscard($"Kai DB 업데이트 실패: {resultUpdate.sErr}", "RegistOrderToPopupAsync_06_08");
                }

                Debug.WriteLine($"[{m_Context.AppName}] Kai DB 업데이트 성공 - Cargo24: {resultSeqno.strResult}");

                return CommonResult_AutoAllocProcess.SuccessAndReEnqueue(item, PostgService_Common_OrderState.NotChanged);
            }
            else
            {
                Debug.WriteLine($"[{m_Context.AppName}] 팝업창 닫기 실패");
                return CommonResult_AutoAllocProcess.FailureAndDiscard("팝업창 닫기 실패", "RegistOrderToPopupAsync_06_02");
            }
            #endregion
        }
        catch (Exception ex)
        {
            return CommonResult_AutoAllocProcess.FailureAndDiscard(StdUtil.GetExceptionMessage(ex), "RegistOrderToPopupAsync_999");
        }
    }
    #endregion

    #region 자동배차 - Kai변경 관련함수들
    /// <summary>
    /// Kai DB에서 업데이트된 주문을 Cargo24 앱에 반영
    /// </summary>
    public async Task<CommonResult_AutoAllocProcess> CheckIsOrderAsync_AssumeKaiUpdated(AutoAllocModel item, CommonResult_AutoAllocDatagrid dgInfo, CancelTokenControl ctrl)
    {
        string kaiState = item.NewOrder.OrderState;
        string Cg24State = dgInfo.sStatus;
        Debug.WriteLine($"[{m_Context.AppName}] CheckIsOrderAsync_AssumeKaiUpdated: KeyCode={item.KeyCode}, kaiState={kaiState}");

        await ctrl.WaitIfPausedOrCancelledAsync();

        switch (kaiState)
        {
            case "대기":
            case "취소": // 취소가 아니면 화물취소 시키고 비적재
                if (Cg24State != "취소") return await OpenEditPopupAsync(item, dgInfo.nIndex, "취소", null, ctrl);
                else return CommonResult_AutoAllocProcess.SuccessAndDestroy(item);

            case "접수": // 같은 접수상태면 
                if (Cg24State == "접수")
                {
                    return await OpenEditPopupAsync(item, dgInfo.nIndex, null, item.NewOrder, ctrl);
                }
                else
                {
                    MsgBox("화물24시가 접수이외의 상태이니, 연구가 필요합니다");
                    return CommonResult_AutoAllocProcess.SuccessAndDestroy(item);
                }

            default:
                System.Windows.MessageBox.Show($"[TODO] kaiState={kaiState}, Cg24State={Cg24State}", "CheckIsOrderAsync_AssumeKaiUpdated");
                break;
        }

        return CommonResult_AutoAllocProcess.SuccessAndReEnqueue(item, PostgService_Common_OrderState.NotChanged);
    }

    /// <summary>
    /// 수정 팝업 열기 (Datagrid 로우 더블클릭) → UpdateOrderToPopupAsync 호출
    /// </summary>
    public async Task<CommonResult_AutoAllocProcess> OpenEditPopupAsync(
        AutoAllocModel item, int rowIndex, string targetState, TbOrder order, CancelTokenControl ctrl)
    {
        try
        {
            await ctrl.WaitIfPausedOrCancelledAsync();

            // 0. 파라미터 검증
            if (string.IsNullOrEmpty(targetState) && order == null)
                return CommonResult_AutoAllocProcess.FailureAndDiscard("targetState와 order 둘 다 null입니다.", "OpenEditPopupAsync_00");

            // 1. DG 로우 더블클릭 좌표 계산
            Draw.Point ptRel = StdUtil.GetCenterDrawPoint(m_RcptPage.DG오더_rcRelCells[c_nColForClick, rowIndex]);
            Debug.WriteLine($"[{m_Context.AppName}] 팝업 열기 시도: rowIndex={rowIndex}, ptRel={ptRel}");

            // 2. 더블클릭 + 팝업창 찾기 (c_nRepeatShort번 시도)
            bool bFind = false;
            IntPtr hWndPopup = IntPtr.Zero;

            for (int j = 0; j < c_nRepeatShort; j++)
            {
                await ctrl.WaitIfPausedOrCancelledAsync();

                // 더블클릭
                await Simulation_Mouse.SafeMouseSend_DblClickLeft_ptRelAsync(m_RcptPage.DG오더_hWnd, ptRel);
                Debug.WriteLine($"[{m_Context.AppName}] 더블클릭 실행 (시도 {j + 1}/{c_nRepeatShort})");

                // 팝업창 나타날 때까지 대기 (최대 5초)
                for (int k = 0; k < c_nRepeatVeryMany; k++)
                {
                    await Task.Delay(c_nWaitShort, ctrl.Token);

                    // 팝업창 찾기
                    hWndPopup = Std32Window.FindMainWindow_NotTransparent(
                        m_Splash.TopWnd_uProcessId, m_FileInfo.접수등록Wnd_TopWnd_sWndName);
                    if (hWndPopup == IntPtr.Zero) continue;

                    // ClassName 검증
                    string className = Std32Window.GetWindowClassName(hWndPopup);
                    if (className == m_FileInfo.접수등록Wnd_TopWnd_sClassName)
                    {
                        bFind = true;
                        Debug.WriteLine($"[{m_Context.AppName}] 팝업창 찾음: hWnd={hWndPopup:X}");
                        break;
                    }
                }

                if (bFind) break;
                await Task.Delay(c_nWaitNormal, ctrl.Token);
            }

            if (!bFind)
            {
                Debug.WriteLine($"[{m_Context.AppName}] 팝업창 찾기 실패");
                return CommonResult_AutoAllocProcess.FailureAndDiscard("더블클릭 후 팝업창이 안뜸", "OpenEditPopupAsync_01");
            }

            await Task.Delay(c_nWaitNormal, ctrl.Token);

            // 3. UpdateOrderToPopupAsync 호출
            return await UpdateOrderToPopupAsync(item, hWndPopup, targetState, order, ctrl);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{m_Context.AppName}] OpenEditPopupAsync 예외: {ex.Message}");
            return CommonResult_AutoAllocProcess.FailureAndDiscard(StdUtil.GetExceptionMessage(ex), "OpenEditPopupAsync_999");
        }
    }

    /// <summary>
    /// 수정 팝업에서 주문 수정
    /// - targetState: 상태 버튼 클릭 (null이면 스킵)
    /// - order: 내용 수정 (null이면 스킵)
    /// - 공통: 저장 버튼 클릭
    /// </summary>
    public async Task<CommonResult_AutoAllocProcess> UpdateOrderToPopupAsync(
        AutoAllocModel item, IntPtr hWndPopup, string targetState, TbOrder order, CancelTokenControl ctrl)
    {
        try
        {
            await ctrl.WaitIfPausedOrCancelledAsync();
            Debug.WriteLine($"[{m_Context.AppName}] UpdateOrderToPopupAsync 진입: KeyCode={item.KeyCode}, targetState={targetState}, order={(order != null ? "있음" : "없음")}");

            // 팝업창 TopMost 설정 후 해제
            await Std32Window.SetWindowTopMostAndReleaseAsync(hWndPopup);

            // 1. 상태 버튼 클릭 (targetState가 있으면)
            if (!string.IsNullOrEmpty(targetState))
            {
                bool bClosed;

                if (targetState == "취소")
                {
                    // 화물취소: 버튼 클릭 → 다이얼로그 처리 → 버튼 Disabled 대기 → 닫기 클릭
                    IntPtr hWndCancel = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_CmnBtn_ptRel화물취소);
                    IntPtr hWndSave = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_CmnBtn_ptRel저장);
                    IntPtr hWndClose = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_CmnBtn_ptRel닫기);

                    if (hWndCancel == IntPtr.Zero)
                        return CommonResult_AutoAllocProcess.FailureAndDiscard("화물취소 버튼 핸들 획득 실패", "UpdateOrderToPopupAsync_02_01");
                    if (hWndSave == IntPtr.Zero)
                        return CommonResult_AutoAllocProcess.FailureAndDiscard("저장 버튼 핸들 획득 실패", "UpdateOrderToPopupAsync_02_02");
                    if (hWndClose == IntPtr.Zero)
                        return CommonResult_AutoAllocProcess.FailureAndDiscard("닫기 버튼 핸들 획득 실패", "UpdateOrderToPopupAsync_02_03");

                    bClosed = await CancelAndWaitClosedAsync(hWndCancel, hWndPopup, hWndSave, hWndClose, ctrl);
                    if (!bClosed)
                        return CommonResult_AutoAllocProcess.FailureAndDiscard("화물취소 처리 실패", "UpdateOrderToPopupAsync_03");

                    Debug.WriteLine($"[{m_Context.AppName}] 화물취소 완료");
                    return CommonResult_AutoAllocProcess.SuccessAndDestroy(item);
                }
                else if (targetState == "접수")
                {
                    // 배차취소: 기존 방식 (팝업 자동 닫힘)
                    IntPtr hWndBtn = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_CmnBtn_ptRel배차취소);
                    if (hWndBtn == IntPtr.Zero)
                        return CommonResult_AutoAllocProcess.FailureAndDiscard("배차취소 버튼 핸들 획득 실패", "UpdateOrderToPopupAsync_02");

                    bClosed = await SaveAndWaitClosedAsync(hWndBtn, hWndPopup, "배차취소", ctrl);
                    if (!bClosed)
                        return CommonResult_AutoAllocProcess.FailureAndDiscard("배차취소 처리 실패", "UpdateOrderToPopupAsync_03");

                    Debug.WriteLine($"[{m_Context.AppName}] 배차취소 완료 → 재적재");
                    return CommonResult_AutoAllocProcess.SuccessAndReEnqueue(item, PostgService_Common_OrderState.NotChanged);
                }
                else
                {
                    return CommonResult_AutoAllocProcess.FailureAndDiscard($"지원하지 않는 targetState: {targetState}", "UpdateOrderToPopupAsync_01");
                }
            }

            // 2. TbOrder를 전달 받았으면 - 현재 화면 값과 비교하여 선택적 수정
            if (order != null)
            {
                int changeCount = 0;
                StdResult_Status result;

                #region ===== 0. 의뢰자 정보 선택적 수정 =====
                // 의뢰자 고객명
                IntPtr hWnd의뢰자고객명 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_의뢰자_ptRel고객명);
                string current고객명 = Std32Window.GetWindowCaption(hWnd의뢰자고객명);
                Debug.WriteLine($"[{m_Context.AppName}] 의뢰자_고객명 비교: 화면=\"{current고객명}\", DB=\"{order.CallCustName ?? ""}\"");
                if ((order.CallCustName ?? "") != current고객명)
                {
                    result = await WriteAndVerifyEditBoxAsync(hWnd의뢰자고객명, order.CallCustName ?? "", "의뢰자_고객명", ctrl);
                    if (result.Result != StdResult.Success) return CommonResult_AutoAllocProcess.FailureAndDiscard(result.sErr, result.sPos);
                    changeCount++;
                }

                // 의뢰자 전화 (숫자만 비교)
                IntPtr hWnd의뢰자전화 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_의뢰자_ptRel고객전화);
                string current전화 = Std32Window.GetWindowCaption(hWnd의뢰자전화);
                string normalized화면전화 = StdConvert.MakePhoneNumberToDigit(current전화);
                string normalizedDB전화 = StdConvert.MakePhoneNumberToDigit(order.CallTelNo ?? "");
                Debug.WriteLine($"[{m_Context.AppName}] 의뢰자_전화 비교: 화면=\"{normalized화면전화}\", DB=\"{normalizedDB전화}\"");
                if (normalizedDB전화 != normalized화면전화)
                {
                    result = await WriteAndVerifyEditBoxAsync(hWnd의뢰자전화, order.CallTelNo ?? "", "의뢰자_전화", ctrl);
                    if (result.Result != StdResult.Success) return CommonResult_AutoAllocProcess.FailureAndDiscard(result.sErr, result.sPos);
                    changeCount++;
                }

                // 의뢰자 담당자 - 수정안된(아마 고객수정을 해야되지 않을까?)
                //IntPtr hWnd의뢰자담당 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_의뢰자_ptRel담당자);
                //string current담당 = Std32Window.GetWindowCaption(hWnd의뢰자담당);
                //Debug.WriteLine($"[{m_Context.AppName}] 의뢰자_담당자 비교: 화면=\"{current담당}\", DB=\"{order.CallChargeName ?? ""}\"");
                //if ((order.CallChargeName ?? "") != current담당)
                //{
                //    result = await WriteAndVerifyEditBoxAsync(hWnd의뢰자담당, order.CallChargeName ?? "", "의뢰자_담당자", ctrl);
                //    if (result.Result != StdResult.Success) return CommonResult_AutoAllocProcess.FailureAndDiscard(result.sErr, result.sPos);
                //    changeCount++;
                //}

                Debug.WriteLine($"[{m_Context.AppName}] 의뢰자 영역 업데이트 완료 (변경: {changeCount}개)");
                #endregion

                #region ===== 1. 상차지 정보 선택적 수정 =====
                // 1-1. 주소(위치) 변경 여부 확인
                IntPtr hWnd상차위치 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_상차지_ptRel위치);
                string current상차위치 = Std32Window.GetWindowCaption(hWnd상차위치);
                bool need상차주소검색 = (order.StartDetailAddr ?? "") != current상차위치;
                Debug.WriteLine($"[{m_Context.AppName}] 상차지_위치 비교: 화면=\"{current상차위치}\", DB=\"{order.StartDetailAddr ?? ""}\" → 주소검색={need상차주소검색}");

                if (need상차주소검색)
                {
                    // 주소 변경 시: 조회버튼 클릭 → 주소검색 → 선택 → 관련 필드 덮어쓰기
                    result = await SearchAndSelectAddressAsync(
                        hWndPopup,
                        m_FileInfo.접수등록Wnd_상차지_ptRel조회버튼,
                        order.StartDetailAddr,
                        "상차지",
                        ctrl);
                    if (result.Result != StdResult.Success) return CommonResult_AutoAllocProcess.FailureAndDiscard(result.sErr, result.sPos);
                    changeCount++;

                    // 위치 (상세주소)
                    result = await WriteAndVerifyEditBoxAsync(hWnd상차위치, order.StartDetailAddr ?? "", "상차지_위치", ctrl);
                    if (result.Result != StdResult.Success) return CommonResult_AutoAllocProcess.FailureAndDiscard(result.sErr, result.sPos);
                }

                // 주소 미변경 시: 개별 필드만 선택적 수정
                // 고객명
                IntPtr hWnd상차고객명 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_상차지_ptRel고객명);
                string current상차고객명 = Std32Window.GetWindowCaption(hWnd상차고객명);
                Debug.WriteLine($"[{m_Context.AppName}] 상차지_고객명 비교: 화면=\"{current상차고객명}\", DB=\"{order.StartCustName ?? ""}\"");
                if ((order.StartCustName ?? "") != current상차고객명)
                {
                    result = await WriteAndVerifyEditBoxAsync(hWnd상차고객명, order.StartCustName ?? "", "상차지_고객명", ctrl);
                    if (result.Result != StdResult.Success) return CommonResult_AutoAllocProcess.FailureAndDiscard(result.sErr, result.sPos);
                    changeCount++;
                }

                // 전화 (숫자만 비교)
                IntPtr hWnd상차전화 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_상차지_ptRel전화);
                string current상차전화 = Std32Window.GetWindowCaption(hWnd상차전화);
                string normalized상차화면전화 = StdConvert.MakePhoneNumberToDigit(current상차전화);
                string normalized상차DB전화 = StdConvert.MakePhoneNumberToDigit(order.StartTelNo ?? "");
                Debug.WriteLine($"[{m_Context.AppName}] 상차지_전화 비교: 화면=\"{normalized상차화면전화}\", DB=\"{normalized상차DB전화}\"");
                if (normalized상차DB전화 != normalized상차화면전화)
                {
                    result = await WriteAndVerifyEditBoxAsync(hWnd상차전화, order.StartTelNo ?? "", "상차지_전화", ctrl);
                    if (result.Result != StdResult.Success) return CommonResult_AutoAllocProcess.FailureAndDiscard(result.sErr, result.sPos);
                    changeCount++;
                }

                // 부서명
                IntPtr hWnd상차부서 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_상차지_ptRel부서명);
                string current상차부서 = Std32Window.GetWindowCaption(hWnd상차부서);
                Debug.WriteLine($"[{m_Context.AppName}] 상차지_부서명 비교: 화면=\"{current상차부서}\", DB=\"{order.StartDeptName ?? ""}\"");
                if ((order.StartDeptName ?? "") != current상차부서)
                {
                    result = await WriteAndVerifyEditBoxAsync(hWnd상차부서, order.StartDeptName ?? "", "상차지_부서명", ctrl);
                    if (result.Result != StdResult.Success) return CommonResult_AutoAllocProcess.FailureAndDiscard(result.sErr, result.sPos);
                    changeCount++;
                }

                // 담당자 - TODO: 수정 안될 수 있음 (의뢰자처럼)
                IntPtr hWnd상차담당 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_상차지_ptRel담당자);
                string current상차담당 = Std32Window.GetWindowCaption(hWnd상차담당);
                Debug.WriteLine($"[{m_Context.AppName}] 상차지_담당자 비교: 화면=\"{current상차담당}\", DB=\"{order.StartChargeName ?? ""}\"");
                if ((order.StartChargeName ?? "") != current상차담당)
                {
                    result = await WriteAndVerifyEditBoxAsync(hWnd상차담당, order.StartChargeName ?? "", "상차지_담당자", ctrl);
                    if (result.Result != StdResult.Success) return CommonResult_AutoAllocProcess.FailureAndDiscard(result.sErr, result.sPos);
                    changeCount++;
                }

                Debug.WriteLine($"[{m_Context.AppName}] 상차지 영역 업데이트 완료 (변경: {changeCount}개)");
                #endregion

                #region ===== 2. 하차지 정보 선택적 수정 =====
                // 2-1. 주소(위치) 변경 여부 확인
                IntPtr hWnd하차위치 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_하차지_ptRel위치);
                string current하차위치 = Std32Window.GetWindowCaption(hWnd하차위치);
                bool need하차주소검색 = (order.DestDetailAddr ?? "") != current하차위치;
                Debug.WriteLine($"[{m_Context.AppName}] 하차지_위치 비교: 화면=\"{current하차위치}\", DB=\"{order.DestDetailAddr ?? ""}\" → 주소검색={need하차주소검색}");

                if (need하차주소검색)
                {
                    // 주소 변경 시: 조회버튼 클릭 → 주소검색 → 선택 → 관련 필드 덮어쓰기
                    result = await SearchAndSelectAddressAsync(
                        hWndPopup,
                        m_FileInfo.접수등록Wnd_하차지_ptRel조회버튼,
                        order.DestDetailAddr,
                        "하차지",
                        ctrl);
                    if (result.Result != StdResult.Success) return CommonResult_AutoAllocProcess.FailureAndDiscard(result.sErr, result.sPos);
                    changeCount++;

                    // 위치 (상세주소)
                    result = await WriteAndVerifyEditBoxAsync(hWnd하차위치, order.DestDetailAddr ?? "", "하차지_위치", ctrl);
                    if (result.Result != StdResult.Success) return CommonResult_AutoAllocProcess.FailureAndDiscard(result.sErr, result.sPos);
                }

                // 개별 필드 선택적 수정
                // 고객명
                IntPtr hWnd하차고객명 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_하차지_ptRel고객명);
                string current하차고객명 = Std32Window.GetWindowCaption(hWnd하차고객명);
                Debug.WriteLine($"[{m_Context.AppName}] 하차지_고객명 비교: 화면=\"{current하차고객명}\", DB=\"{order.DestCustName ?? ""}\"");
                if ((order.DestCustName ?? "") != current하차고객명)
                {
                    result = await WriteAndVerifyEditBoxAsync(hWnd하차고객명, order.DestCustName ?? "", "하차지_고객명", ctrl);
                    if (result.Result != StdResult.Success) return CommonResult_AutoAllocProcess.FailureAndDiscard(result.sErr, result.sPos);
                    changeCount++;
                }

                // 전화 (숫자만 비교)
                IntPtr hWnd하차전화 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_하차지_ptRel전화);
                string current하차전화 = Std32Window.GetWindowCaption(hWnd하차전화);
                string normalized하차화면전화 = StdConvert.MakePhoneNumberToDigit(current하차전화);
                string normalized하차DB전화 = StdConvert.MakePhoneNumberToDigit(order.DestTelNo ?? "");
                Debug.WriteLine($"[{m_Context.AppName}] 하차지_전화 비교: 화면=\"{normalized하차화면전화}\", DB=\"{normalized하차DB전화}\"");
                if (normalized하차DB전화 != normalized하차화면전화)
                {
                    result = await WriteAndVerifyEditBoxAsync(hWnd하차전화, order.DestTelNo ?? "", "하차지_전화", ctrl);
                    if (result.Result != StdResult.Success) return CommonResult_AutoAllocProcess.FailureAndDiscard(result.sErr, result.sPos);
                    changeCount++;
                }

                // 부서명
                IntPtr hWnd하차부서 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_하차지_ptRel부서명);
                string current하차부서 = Std32Window.GetWindowCaption(hWnd하차부서);
                Debug.WriteLine($"[{m_Context.AppName}] 하차지_부서명 비교: 화면=\"{current하차부서}\", DB=\"{order.DestDeptName ?? ""}\"");
                if ((order.DestDeptName ?? "") != current하차부서)
                {
                    result = await WriteAndVerifyEditBoxAsync(hWnd하차부서, order.DestDeptName ?? "", "하차지_부서명", ctrl);
                    if (result.Result != StdResult.Success) return CommonResult_AutoAllocProcess.FailureAndDiscard(result.sErr, result.sPos);
                    changeCount++;
                }

                // 담당자
                IntPtr hWnd하차담당 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_하차지_ptRel담당자);
                string current하차담당 = Std32Window.GetWindowCaption(hWnd하차담당);
                Debug.WriteLine($"[{m_Context.AppName}] 하차지_담당자 비교: 화면=\"{current하차담당}\", DB=\"{order.DestChargeName ?? ""}\"");
                if ((order.DestChargeName ?? "") != current하차담당)
                {
                    result = await WriteAndVerifyEditBoxAsync(hWnd하차담당, order.DestChargeName ?? "", "하차지_담당자", ctrl);
                    if (result.Result != StdResult.Success) return CommonResult_AutoAllocProcess.FailureAndDiscard(result.sErr, result.sPos);
                    changeCount++;
                }

                Debug.WriteLine($"[{m_Context.AppName}] 하차지 영역 업데이트 완료 (변경: {changeCount}개)");
                #endregion

                #region ===== 3. 배송타입 선택적 수정 (체크박스) =====
                CEnum_Cg24OrderStatus status = Get오더타입FlagsFromKaiTable(order);
                var (deliverChangeCount, deliverResult) = await Set오더타입Async(hWndPopup, status, true, ctrl);
                if (deliverResult.Result != StdResult.Success) return CommonResult_AutoAllocProcess.FailureAndDiscard(deliverResult.sErr, deliverResult.sPos);
                changeCount += deliverChangeCount;
                Debug.WriteLine($"[{m_Context.AppName}] 배송타입 영역 업데이트 완료 (변경: {deliverChangeCount}개)");
                #endregion

                #region ===== 4. 차량정보 선택적 수정 =====
                // 4-1. 차량톤수 (라디오버튼) - DB 톤수에 해당하는 버튼이 체크되어 있는지 확인
                var ptTon = GetCarWeightWithPoint(order.CarType, order.CarWeight);
                if (!string.IsNullOrEmpty(ptTon.sErr))
                    return CommonResult_AutoAllocProcess.FailureAndDiscard(ptTon.sErr, "UpdateOrderToPopupAsync_04_01");

                IntPtr hWndTon = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, ptTon.ptResult);
                if (hWndTon == IntPtr.Zero)
                    return CommonResult_AutoAllocProcess.FailureAndDiscard("톤수 라디오버튼 핸들 획득 실패", "UpdateOrderToPopupAsync_04_02");

                bool currentTonChecked = Std32Msg_Send.GetCheckStatus(hWndTon) == 1;
                Debug.WriteLine($"[{m_Context.AppName}] 차량톤수 비교: 화면체크={currentTonChecked}, DB={order.CarWeight}");

                if (!currentTonChecked)
                {
                    // 톤수 라디오버튼 설정
                    await Simulation_Mouse.SetCheckBtnStatusAsync(hWndTon, true);

                    // 몇톤 Edit에 최대적재량 설정
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

                    changeCount++;
                    Debug.WriteLine($"[{m_Context.AppName}] 차량톤수 변경: {order.CarWeight}");
                }

                // 4-2. 차종 (콤보박스) - 현재 텍스트와 DB 값 비교
                var strTruck = GetTruckDetailStringFromInsung(order.TruckDetail);
                if (!string.IsNullOrEmpty(strTruck.sErr))
                    return CommonResult_AutoAllocProcess.FailureAndDiscard(strTruck.sErr, "UpdateOrderToPopupAsync_04_03");

                IntPtr hWndTruck = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_차종Combo_ptRel차종확인);
                if (hWndTruck == IntPtr.Zero)
                    return CommonResult_AutoAllocProcess.FailureAndDiscard("차종 콤보박스 핸들 획득 실패", "UpdateOrderToPopupAsync_04_04");

                string current차종 = Std32Window.GetWindowCaption(hWndTruck) ?? "";
                string target차종 = strTruck.strResult ?? "";
                Debug.WriteLine($"[{m_Context.AppName}] 차종 비교: 화면=\"{current차종}\", DB=\"{target차종}\"");

                if (current차종 != target차종)
                {
                    // WM_CHAR로 콤보박스에 텍스트 전송 + 검증
                    bool bResult = await Simulation_Keyboard.PostCharStringWithVerifyAsync(hWndTruck, target차종);
                    Debug.WriteLine($"[{m_Context.AppName}] 차종 변경: {current차종} → {target차종}, 결과={bResult}");
                    if (bResult)
                        changeCount++;
                }
                #endregion

                #region ===== 5. 운송비 선택적 수정 =====
                var (feeChangeCount, feeResult) = await Set운송비Async(hWndPopup, order, true, ctrl);
                if (feeResult.Result != StdResult.Success) return CommonResult_AutoAllocProcess.FailureAndDiscard(feeResult.sErr, feeResult.sPos);
                changeCount += feeChangeCount;
                #endregion

                #region ===== 6. 결과 처리 =====
                if (changeCount > 0)
                {
                    // 저장 버튼 클릭 후 확인창/보고창 처리 및 창 닫힘 대기
                    IntPtr hWndSave = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_CmnBtn_ptRel저장);
                    bool bSaved = await SaveAndWaitClosedAsync(hWndSave, hWndPopup, "저장", ctrl);
                    if (!bSaved)
                        return CommonResult_AutoAllocProcess.FailureAndDiscard("저장 실패", "UpdateOrderToPopupAsync_Save_01");

                    Debug.WriteLine($"[{m_Context.AppName}] 수정 저장 완료 (변경: {changeCount}개)");
                    return CommonResult_AutoAllocProcess.SuccessAndReEnqueue(item, PostgService_Common_OrderState.NotChanged);
                }
                else
                {
                    // 닫기 버튼 클릭
                    IntPtr hWndClose = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_CmnBtn_ptRel닫기);
                    await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndClose);
                    Debug.WriteLine($"[{m_Context.AppName}] 수정 없음 → 닫기");
                    //System.Windows.MessageBox.Show($"[경고] 수정 없음: KeyCode={item.KeyCode}", "UpdateOrderToPopupAsync"); // 보류
                    return CommonResult_AutoAllocProcess.SuccessAndReEnqueue(item, PostgService_Common_OrderState.NotChanged);
                }
                #endregion
            }

            return CommonResult_AutoAllocProcess.SuccessAndReEnqueue(item, PostgService_Common_OrderState.NotChanged);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{m_Context.AppName}] UpdateOrderToPopupAsync 예외: {ex.Message}");
            return CommonResult_AutoAllocProcess.FailureAndDiscard(StdUtil.GetExceptionMessage(ex), "UpdateOrderToPopupAsync_999");
        }
    }
    #endregion

    #region 자동배차 - 하물24시 상태관리 관련함수들
    #endregion
}
#nullable restore
