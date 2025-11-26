using System.Diagnostics;
using System.Windows;
using Draw = System.Drawing;
using Media = System.Windows.Media;

using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;
using Kai.Common.NetDll_WpfCtrl.NetOFR;
using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgBox;

using Kai.Client.CallCenter.Classes;
using Kai.Client.CallCenter.OfrWorks;
using Kai.Client.CallCenter.Windows;
using static Kai.Client.CallCenter.Classes.CommonVars;

namespace Kai.Client.CallCenter.Networks.NwOnecalls;
#nullable disable

/// <summary>
/// 원콜 접수등록 페이지 제어
/// </summary>
public class OnecallAct_RcptRegPage
{
    #region Datagrid Column Header Info
    /// <summary>
    /// Datagrid 컬럼 헤더 정보 배열 (21개)
    /// </summary>
    public readonly NwCommon_DgColumnHeader[] m_ReceiptDgHeaderInfos = new NwCommon_DgColumnHeader[]
    {
        new NwCommon_DgColumnHeader() { sName = "순번", bOfrSeq = true, nWidth = 50 },
        new NwCommon_DgColumnHeader() { sName = "처리상태", bOfrSeq = false, nWidth = 65 },
        new NwCommon_DgColumnHeader() { sName = "오더번호", bOfrSeq = true, nWidth = 100 },
        new NwCommon_DgColumnHeader() { sName = "처리일자", bOfrSeq = true, nWidth = 105 },
        new NwCommon_DgColumnHeader() { sName = "처리시간", bOfrSeq = true, nWidth = 85 },
        new NwCommon_DgColumnHeader() { sName = "상차지", bOfrSeq = false, nWidth = 125 },
        new NwCommon_DgColumnHeader() { sName = "하차지", bOfrSeq = false, nWidth = 125 },
        new NwCommon_DgColumnHeader() { sName = "결제방법", bOfrSeq = false, nWidth = 65 },
        new NwCommon_DgColumnHeader() { sName = "운임", bOfrSeq = true, nWidth = 80 },
        new NwCommon_DgColumnHeader() { sName = "수수료", bOfrSeq = true, nWidth = 75 },
        new NwCommon_DgColumnHeader() { sName = "차종", bOfrSeq = false, nWidth = 70 },
        new NwCommon_DgColumnHeader() { sName = "톤수", bOfrSeq = false, nWidth = 50 },
        new NwCommon_DgColumnHeader() { sName = "혼적", bOfrSeq = false, nWidth = 60 },
        new NwCommon_DgColumnHeader() { sName = "차주명", bOfrSeq = false, nWidth = 80 },
        new NwCommon_DgColumnHeader() { sName = "차주전화", bOfrSeq = true, nWidth = 135 },
        new NwCommon_DgColumnHeader() { sName = "담당자번호", bOfrSeq = true, nWidth = 135 },
        new NwCommon_DgColumnHeader() { sName = "적재옵션", bOfrSeq = false, nWidth = 130 },
        new NwCommon_DgColumnHeader() { sName = "화물정보", bOfrSeq = false, nWidth = 170 },
        new NwCommon_DgColumnHeader() { sName = "인수증", bOfrSeq = false, nWidth = 60 },
        new NwCommon_DgColumnHeader() { sName = "상차일", bOfrSeq = true, nWidth = 70 },
        new NwCommon_DgColumnHeader() { sName = "하차일", bOfrSeq = true, nWidth = 70 },
    };
    #endregion

    #region Constants
    private const int COLUMN_WIDTH_TOLERANCE = 1;  // 컬럼 너비 허용 오차 (픽셀)
    private const int MAX_RETRY = 3;               // 최대 재시도 횟수
    private const int DELAY_RETRY = 500;           // 재시도 대기 시간 (ms)
    #endregion

    #region Private Fields
    private readonly OnecallContext m_Context;
    private OnecallInfo_File fInfo => m_Context.FileInfo;
    private OnecallInfo_Mem mInfo => m_Context.MemInfo;
    private OnecallInfo_Mem.MainWnd mMain => mInfo.Main;
    private OnecallInfo_Mem.RcptRegPage mRcpt => mInfo.RcptPage;
    private string AppName => m_Context.AppName;
    #endregion

    #region 생성자
    public OnecallAct_RcptRegPage(OnecallContext context)
    {
        m_Context = context;
    }
    #endregion

    #region InitializeAsync
    /// <summary>
    /// 접수등록 페이지 초기화
    /// </summary>
    public async Task<StdResult_Error> InitializeAsync()
    {
        try
        {
            Debug.WriteLine($"[{AppName}] RcptRegPage InitializeAsync 시작");

            // 1. 접수등록Page 윈도우 찾기 (10초)
            for (int i = 0; i < c_nRepeatVeryMany; i++)
            {
                mRcpt.TopWnd_hWnd = Std32Window.FindWindowEx(mMain.WndInfo_MdiClient.hWnd, IntPtr.Zero, null, fInfo.접수등록Page_TopWnd_sWndName);
                if (mRcpt.TopWnd_hWnd != IntPtr.Zero) break;
                await Task.Delay(c_nWaitNormal);
            }

            if (mRcpt.TopWnd_hWnd == IntPtr.Zero)
                return new StdResult_Error($"[{AppName}] 접수등록Page 찾기실패: {fInfo.접수등록Page_TopWnd_sWndName}", "OnecallAct_RcptRegPage/InitializeAsync_01");

            Debug.WriteLine($"[{AppName}] 접수등록Page 찾음: {mRcpt.TopWnd_hWnd:X}");
            await Task.Delay(1000); // 윈도우 로드 대기

            // 2. 자식윈도우 찾기
            List<StdCommon32_WndInfo> lst = Std32Window.GetChildWindows_FirstLayer(mRcpt.TopWnd_hWnd);
            if (lst.Count == 0)
                return new StdResult_Error($"[{AppName}] 접수등록Page 자식윈도 찾기실패", "OnecallAct_RcptRegPage/InitializeAsync_02");
            Debug.WriteLine($"[{AppName}] 접수등록Page 자식윈도 찾음: {lst.Count}개");
            for (int i = 0; i < lst.Count; i++)
            {
                Debug.WriteLine($"  [{i}] rcRel={lst[i].rcRel}, wndName={lst[i].wndName}");
            }

            // 3. 접수영역 찾기
            StdCommon32_WndInfo item = lst.FirstOrDefault(x => x.rcRel == fInfo.접수등록Page_접수영역_rcChkRel);
            if (item == null)
                return new StdResult_Error($"[{AppName}] 접수영역 찾기실패: {fInfo.접수등록Page_접수영역_rcChkRel}", "OnecallAct_RcptRegPage/InitializeAsync_03");
            mRcpt.접수영역_hWnd = item.hWnd;
            Debug.WriteLine($"[{AppName}] 접수영역 찾음: {mRcpt.접수영역_hWnd:X}");

            // 4. 검색영역 찾기
            item = lst.FirstOrDefault(x => x.wndName == fInfo.접수등록Page_검색영역_sWndName);
            if (item == null)
                return new StdResult_Error($"[{AppName}] 검색영역 찾기실패: {fInfo.접수등록Page_검색영역_sWndName}", "OnecallAct_RcptRegPage/InitializeAsync_04");
            mRcpt.검색영역_hWnd = item.hWnd;
            Debug.WriteLine($"[{AppName}] 검색영역 찾음: {mRcpt.검색영역_hWnd:X}");

            // 5. DG오더 찾기
            item = lst.FirstOrDefault(x => x.rcRel == fInfo.접수등록Page_DG오더_rcRelFirst);
            if (item == null)
                return new StdResult_Error($"[{AppName}] DG오더 찾기실패: {fInfo.접수등록Page_DG오더_rcRelFirst}", "OnecallAct_RcptRegPage/InitializeAsync_05");
            mRcpt.DG오더_hWnd = item.hWnd;
            Debug.WriteLine($"[{AppName}] DG오더 찾음: {mRcpt.DG오더_hWnd:X}");

            // 6. 확장 전 컬럼 검증/초기화
            var resultRects = await SetDG오더LargeRectsAsync();
            if (resultRects != null) return resultRects;

            // 7. 검색영역 확장버튼 찾기
            mRcpt.접수영역_확장버튼_hWnd = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.검색영역_hWnd, fInfo.접수등록Page_검색ExpandBtn_ptChkRelL);
            if (mRcpt.접수영역_확장버튼_hWnd == IntPtr.Zero)
                return new StdResult_Error($"[{AppName}] 확장버튼 찾기실패: {fInfo.접수등록Page_검색ExpandBtn_ptChkRelL}", "OnecallAct_RcptRegPage/InitializeAsync_06");
            Debug.WriteLine($"[{AppName}] 확장버튼 찾음: {mRcpt.접수영역_확장버튼_hWnd:X}");

            // 8. 확장버튼 클릭 및 DG오더 확장 대기
            await Std32Mouse_Post.MousePostAsync_ClickLeft(mRcpt.접수영역_확장버튼_hWnd);
            Draw.Rectangle rcTmp = StdUtil.s_rcDrawEmpty;
            for (int i = 0; i < c_nRepeatNormal; i++)
            {
                rcTmp = Std32Window.GetWindowRect_DrawAbs(mRcpt.DG오더_hWnd);
                if (rcTmp.Height == fInfo.접수등록Page_DG오더_nExpandedHeight) break;
                await Task.Delay(c_nWaitShort);
            }
            if (rcTmp.Height != fInfo.접수등록Page_DG오더_nExpandedHeight)
                return new StdResult_Error($"[{AppName}] DG오더 확장실패: {rcTmp.Height} != {fInfo.접수등록Page_DG오더_nExpandedHeight}", "OnecallAct_RcptRegPage/InitializeAsync_07");
            Debug.WriteLine($"[{AppName}] DG오더 확장완료: {rcTmp.Height}");

            Debug.WriteLine($"[{AppName}] RcptRegPage InitializeAsync 완료");
            return null;
        }
        catch (Exception ex)
        {
            return new StdResult_Error($"[{AppName}] RcptRegPage 예외: {ex.Message}", "OnecallAct_RcptRegPage/InitializeAsync_99");
        }
    }
    #endregion

    #region SetDG오더LargeRectsAsync - 확장 후 사용
    /// <summary>
    /// Datagrid 확장 후 Cell Rect 배열 계산 (36행)
    /// </summary>
    private async Task<StdResult_Error> SetDG오더LargeRectsAsync(bool bEdit = true)
    {
        Draw.Bitmap bmpDG = null;
        List<OfrModel_LeftWidth> listLW = null;
        int columns = 0;

        try
        {
            Debug.WriteLine($"[{AppName}] SetDG오더LargeRectsAsync 시작");

            // 재시도 루프
            for (int retry = 1; retry <= MAX_RETRY; retry++)
            {
                // 1. DG 헤더 캡처
                Draw.Rectangle rcDG_Abs = Std32Window.GetWindowRect_DrawAbs(mRcpt.DG오더_hWnd);
                int headerHeight = fInfo.접수등록Page_DG오더_headerHeight;
                Draw.Rectangle rcHeader = new Draw.Rectangle(0, 0, rcDG_Abs.Width, headerHeight);
                bmpDG = OfrService.CaptureScreenRect_InWndHandle(mRcpt.DG오더_hWnd, rcHeader);
                if (bmpDG == null)
                    return new StdResult_Error($"[{AppName}] DG 캡처 실패", "OnecallAct_RcptRegPage/SetDG오더LargeRectsAsync_01");
                Debug.WriteLine($"[{AppName}] DG 캡처 완료: {rcHeader.Width}x{rcHeader.Height}");

                // 2. 컬럼 경계 검출 (MinBrightness 방식)
                const int headerGab = 6;
                int textHeight = fInfo.접수등록Page_DG오더_headerHeight - (headerGab * 2);
                int targetRow = headerGab + textHeight;

                byte minBrightness = OfrService.GetMinBrightnessAtRow_FromColorBitmapFast(bmpDG, targetRow);
                if (minBrightness == 255)
                    return new StdResult_Error($"[{AppName}] 최소 밝기 검출 실패", "OnecallAct_RcptRegPage/SetDG오더LargeRectsAsync_02");
                minBrightness += 2;

                bool[] boolArr = OfrService.GetBoolArray_FromColorBitmapRowFast(bmpDG, targetRow, minBrightness, 2);
                if (boolArr == null || boolArr.Length == 0)
                    return new StdResult_Error($"[{AppName}] Bool 배열 생성 실패", "OnecallAct_RcptRegPage/SetDG오더LargeRectsAsync_03");

                listLW = OfrService.GetLeftWidthList_FromBool1Array(boolArr, minBrightness);
                if (listLW == null || listLW.Count < 2)
                    return new StdResult_Error($"[{AppName}] 컬럼 경계 검출 실패: Count={listLW?.Count ?? 0}", "OnecallAct_RcptRegPage/SetDG오더LargeRectsAsync_04");

                // 마지막 항목 제거 (오른쪽 끝 경계)
                listLW.RemoveAt(listLW.Count - 1);

                columns = listLW.Count;
                Debug.WriteLine($"[{AppName}] 컬럼 검출: {columns}개 (목표: {m_ReceiptDgHeaderInfos.Length}개)");

                // 평가 1: 컬럼 개수 확인
                if (columns < m_ReceiptDgHeaderInfos.Length)
                {
                    Debug.WriteLine($"[{AppName}] 컬럼 개수 불일치: 검출={columns}개, 예상={m_ReceiptDgHeaderInfos.Length}개 (재시도 {retry}/{MAX_RETRY})");

                    bmpDG?.Dispose();
                    bmpDG = null;

                    StdResult_Error initResult = await InitDG오더Async(
                        CEnum_DgValidationIssue.InvalidColumnCount);

                    if (initResult != null)
                    {
                        if (retry == MAX_RETRY)
                            return new StdResult_Error(
                                $"[{AppName}] 컬럼 개수 부족: 검출={columns}개, 예상={m_ReceiptDgHeaderInfos.Length}개 (재시도 {MAX_RETRY}회 초과)",
                                "OnecallAct_RcptRegPage/SetDG오더LargeRectsAsync_05");

                        await Task.Delay(DELAY_RETRY);
                        continue; // 재시도
                    }

                    await Task.Delay(DELAY_RETRY);
                    continue;
                }

                // 평가 2: 컬럼 헤더 OFR
                Debug.WriteLine($"[{AppName}] 평가 2: 컬럼 헤더 OFR 시작");
                string[] columnTexts = new string[m_ReceiptDgHeaderInfos.Length];

                for (int i = 0; i < m_ReceiptDgHeaderInfos.Length; i++)
                {
                    Draw.Rectangle rcTmp = new Draw.Rectangle(
                        listLW[i].nLeft, headerGab, listLW[i].nWidth, textHeight);

                    var result = await OfrWork_Common.OfrStr_ComplexCharSetAsync(
                        bmpDG, rcTmp, bInvertRgb: false, bEdit: bEdit);

                    columnTexts[i] = result?.strResult ?? string.Empty;
                }

                // 평가 3: Datagrid 상태 검증
                Debug.WriteLine($"[{AppName}] 평가 3: Datagrid 상태 검증 시작");
                CEnum_DgValidationIssue validationIssues = ValidateDatagridState(columnTexts, listLW);

                if (validationIssues != CEnum_DgValidationIssue.None)
                {
                    Debug.WriteLine($"[{AppName}] Datagrid 상태 검증 실패: {validationIssues} (재시도 {retry}/{MAX_RETRY})");

                    bmpDG?.Dispose();
                    bmpDG = null;

                    StdResult_Error initResult = await InitDG오더Async(validationIssues);

                    if (initResult != null)
                    {
                        if (retry == MAX_RETRY)
                            return new StdResult_Error(
                                $"[{AppName}] Datagrid 상태 검증 실패: {validationIssues} (재시도 {MAX_RETRY}회 초과)",
                                "OnecallAct_RcptRegPage/SetDG오더LargeRectsAsync_Validation");
                    }

                    await Task.Delay(DELAY_RETRY);
                    continue; // 재시도
                }

                // 모든 평가 통과
                Debug.WriteLine($"[{AppName}] 모든 컬럼 검증 완료!");

                // Cell Rect 배열 생성
                bmpDG?.Dispose();
                bmpDG = null;

                int rowCount = fInfo.접수등록Page_DG오더_largeRowsCount;
                int rowHeight = fInfo.접수등록Page_DG오더_dataRowHeight;

                mRcpt.DG오더_rcRelLargeCells = new Draw.Rectangle[rowCount, columns];
                mRcpt.DG오더_ptRelChkLargeRows = new Draw.Point[rowCount];

                for (int row = 0; row < rowCount; row++)
                {
                    int cellY = headerHeight + (row * rowHeight);

                    // Row check point (첫번째 컬럼 중앙)
                    mRcpt.DG오더_ptRelChkLargeRows[row] = new Draw.Point(
                        listLW[0].nLeft + (listLW[0].nWidth / 2),
                        cellY + (rowHeight / 2)
                    );

                    // Cell rects (경계에서 계산하여 미세조정 적용)
                    for (int col = 0; col < columns; col++)
                    {
                        mRcpt.DG오더_rcRelLargeCells[row, col] = new Draw.Rectangle(
                            listLW[col].nLeft + 1,
                            cellY,
                            listLW[col].nWidth - 2,
                            rowHeight
                        );
                    }
                }

                Debug.WriteLine($"[{AppName}] Rect 배열 생성 완료: {rowCount}행 x {columns}열");

                // Background Brightness 계산 (데이터그리드 중심 위치)
                mRcpt.DG오더_nBackgroundBright = OfrService.GetCenterPixelBrightnessFrmWndHandle(mRcpt.DG오더_hWnd);
                Debug.WriteLine($"[{AppName}] Background Brightness: {mRcpt.DG오더_nBackgroundBright}");

                Debug.WriteLine($"[{AppName}] SetDG오더LargeRectsAsync 완료");
                return null; // 성공
            }

            // 최대 재시도 초과
            return new StdResult_Error(
                $"[{AppName}] Datagrid 초기화 실패 (재시도 {MAX_RETRY}회 초과)",
                "OnecallAct_RcptRegPage/SetDG오더LargeRectsAsync_MaxRetry");
        }
        catch (Exception ex)
        {
            return new StdResult_Error($"[{AppName}] SetDG오더LargeRectsAsync 예외: {ex.Message}", "OnecallAct_RcptRegPage/SetDG오더LargeRectsAsync_99");
        }
        finally
        {
            bmpDG?.Dispose();
        }
    }
    #endregion

    #region ValidateDatagridState
    /// <summary>
    /// Datagrid 상태 검증 (컬럼 개수, 순서, 너비 확인)
    /// </summary>
    private CEnum_DgValidationIssue ValidateDatagridState(string[] columnTexts, List<OfrModel_LeftWidth> listLW)
    {
        CEnum_DgValidationIssue issues = CEnum_DgValidationIssue.None;

        // 1. 컬럼 개수 체크
        if (columnTexts == null || columnTexts.Length != m_ReceiptDgHeaderInfos.Length)
        {
            issues |= CEnum_DgValidationIssue.InvalidColumnCount;
            Debug.WriteLine($"[ValidateDatagridState] 컬럼 개수 불일치: 실제={columnTexts?.Length}, 예상={m_ReceiptDgHeaderInfos.Length}");
            return issues;
        }

        // 2. 각 컬럼 검증
        for (int x = 0; x < columnTexts.Length; x++)
        {
            string columnText = columnTexts[x];

            // 2-1. 컬럼명이 유효한지
            int index = Array.FindIndex(m_ReceiptDgHeaderInfos, h => h.sName == columnText);

            if (index < 0)
            {
                issues |= CEnum_DgValidationIssue.InvalidColumn;
                Debug.WriteLine($"[ValidateDatagridState] 유효하지 않은 컬럼[{x}]: '{columnText}'");
                continue;
            }

            // 2-2. 컬럼 순서가 맞는지
            if (index != x)
            {
                issues |= CEnum_DgValidationIssue.WrongOrder;
                Debug.WriteLine($"[ValidateDatagridState] 순서 불일치[{x}]: '{columnText}' (예상 위치={index})");
            }

            // 2-3. 컬럼 너비가 맞는지
            int actualWidth = listLW[x].nWidth;
            int expectedWidth = m_ReceiptDgHeaderInfos[index].nWidth;
            int widthDiff = Math.Abs(actualWidth - expectedWidth);

            if (widthDiff > COLUMN_WIDTH_TOLERANCE)
            {
                issues |= CEnum_DgValidationIssue.WrongWidth;
                Debug.WriteLine($"[ValidateDatagridState] 너비 불일치[{x}]: '{columnText}', 실제={actualWidth}, 예상={expectedWidth}, 오차={widthDiff}");
            }
        }

        if (issues == CEnum_DgValidationIssue.None)
        {
            Debug.WriteLine($"[ValidateDatagridState] Datagrid 상태 정상");
        }

        return issues;
    }
    #endregion

    #region InitDG오더Async - Datagrid 강제 초기화
    /// <summary>
    /// Datagrid 강제 초기화 (항목초기화 → 컬럼 삭제 → 순서 조정 → 폭 조정)
    /// </summary>
    private async Task<StdResult_Error> InitDG오더Async(CEnum_DgValidationIssue issues)
    {
        // 마우스 커서 위치 백업 (작업 완료 후 복원용)
        Draw.Point ptCursorBackup = Std32Cursor.GetCursorPos_AbsDrawPt();

        try
        {
            Debug.WriteLine($"[{AppName}] InitDG오더Async 시작: issues={issues}");

            // 상수 정의
            const int headerGab = 6;
            int headerHeight = fInfo.접수등록Page_DG오더_headerHeight;
            int textHeight = headerHeight - (headerGab * 2);
            int targetRow = headerGab + textHeight;

            // DG오더_hWnd 기준으로 헤더 영역 정의
            Draw.Rectangle rcDG = Std32Window.GetWindowRect_DrawAbs(mRcpt.DG오더_hWnd);
            Draw.Rectangle rcHeader = new Draw.Rectangle(0, 0, rcDG.Width, headerHeight);

            #region Step 1 - 목록초기화
            Debug.WriteLine($"[{AppName}] Step 1: 목록초기화 시작");
            await Std32Mouse_Post.MousePostAsync_ClickLeft(
                Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.TopWnd_hWnd, fInfo.접수등록Page_접수Btn목록초기화_ptRelL));
            await Task.Delay(c_nWaitVeryLong);
            Debug.WriteLine($"[{AppName}] Step 1 완료");
            #endregion

            #region Step 2 - 컬럼 폭 조정 및 불필요한 컬럼 삭제
            Debug.WriteLine($"[{AppName}] Step 2: 컬럼 폭 조정 및 삭제 시작");

            int center = headerGab + (textHeight / 2);

            for (int iteration = 0; iteration < 5; iteration++)
            {
                await Task.Delay(c_nWaitNormal);

                // 2-1. 헤더 캡처 및 컬럼 경계 검출
                var (bmpHeader, listLW, columns) = CaptureAndDetectColumnBoundaries(rcHeader, targetRow);
                if (bmpHeader == null)
                {
                    return new StdResult_Error($"[{AppName}] Step 2 헤더 캡처 실패 (반복 {iteration + 1})",
                        "OnecallAct_RcptRegPage/InitDG오더Async_01");
                }

                Debug.WriteLine($"[{AppName}] 반복 {iteration + 1}: 컬럼 {columns}개 검출 (목표: {m_ReceiptDgHeaderInfos.Length}개)");

                // 2-2. 각 컬럼 텍스트 인식
                string[] texts = await OfrAllColumnsAsync(bmpHeader, listLW, columns, headerGab, textHeight, bEdit: true);
                Draw.Rectangle[] rcHeaders = new Draw.Rectangle[columns];
                for (int x = 0; x < columns; x++)
                {
                    rcHeaders[x] = new Draw.Rectangle(listLW[x].nLeft, headerGab, listLW[x].nWidth, textHeight);
                }

                // 2-3. 불필요한 컬럼 삭제 (우측에서 좌측으로)
                int removedCount = 0;
                for (int x = columns - 1; x >= 0; x--)
                {
                    bool isNeeded = Array.Exists(m_ReceiptDgHeaderInfos, h => h.sName == texts[x]);
                    if (!isNeeded)
                    {
                        removedCount++;
                        Debug.WriteLine($"[{AppName}] 불필요 컬럼 발견: [{x}]'{texts[x]}' → 제거");

                        // 수직 드래그로 제거 (위로 드래그)
                        Draw.Point ptCenter = StdUtil.GetCenterDrawPoint(rcHeaders[x]);
                        await Simulation_Mouse.SafeMouseEvent_DragLeft_Smooth_VerticalAsync(
                            mRcpt.DG오더_hWnd, ptCenter, -50, false);

                        await Task.Delay(c_nWaitShort);
                    }
                }

                if (removedCount > 0)
                {
                    Debug.WriteLine($"[{AppName}] {removedCount}개 불필요 컬럼 제거 완료");
                    await Task.Delay(c_nWaitLong);

                    // 컬럼 제거 후 다시 캡처
                    bmpHeader.Dispose();
                    var result = CaptureAndDetectColumnBoundaries(rcHeader, targetRow);
                    bmpHeader = result.bmpHeader;
                    listLW = result.listLW;
                    columns = result.columns;

                    if (bmpHeader == null)
                    {
                        return new StdResult_Error($"[{AppName}] 컬럼 제거 후 재캡처 실패",
                            "OnecallAct_RcptRegPage/InitDG오더Async_01b");
                    }

                    texts = await OfrAllColumnsAsync(bmpHeader, listLW, columns, headerGab, textHeight, bEdit: true);
                    Debug.WriteLine($"[{AppName}] 재캡처 후 컬럼 수: {columns}");
                }

                // 2-4. 폭 조정 (우측에서 좌측으로) - 숨겨진 컬럼 드러내기
                for (int x = columns - 1; x >= 0; x--)
                {
                    // 경계선 위치 = (x+1)번 경계선의 Left
                    int boundaryX = listLW[x + 1].nLeft;

                    // 컬럼명으로 문자수 구하기
                    int textLength = 0;
                    string columnName = texts[x];

                    if (!string.IsNullOrEmpty(columnName))
                    {
                        var matched = m_ReceiptDgHeaderInfos.FirstOrDefault(h => h.sName == columnName);
                        if (matched != null)
                        {
                            textLength = matched.sName.Length;
                        }
                        else
                        {
                            textLength = columnName.Length;
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
                        mRcpt.DG오더_hWnd, ptStart, dx, bBkCursor: false, nMiliSec: 100);

                    await Task.Delay(c_nWaitNormal);
                }

                bmpHeader.Dispose();
                Debug.WriteLine($"[{AppName}] {columns}개 컬럼 폭 조정 완료");

                // 2-5. 폭 조정 후 원하는 컬럼 개수 확인
                await Task.Delay(c_nWaitShort);
                var (bmpHeader2, listLW2, columns2) = CaptureAndDetectColumnBoundaries(rcHeader, targetRow);
                if (bmpHeader2 == null)
                {
                    return new StdResult_Error($"[{AppName}] 폭 조정 후 캡처 실패",
                        "OnecallAct_RcptRegPage/InitDG오더Async_02");
                }

                string[] texts2 = await OfrAllColumnsAsync(bmpHeader2, listLW2, columns2, headerGab, textHeight, bEdit: true);

                // 원하는 컬럼 개수 확인
                int matchedCount = 0;
                List<string> missingColumns = new List<string>();

                for (int i = 0; i < m_ReceiptDgHeaderInfos.Length; i++)
                {
                    bool found = false;
                    for (int x = 0; x < columns2; x++)
                    {
                        if (texts2[x] == m_ReceiptDgHeaderInfos[i].sName)
                        {
                            matchedCount++;
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        missingColumns.Add(m_ReceiptDgHeaderInfos[i].sName);
                    }
                }

                Debug.WriteLine($"[{AppName}] 원하는 컬럼 획득: {matchedCount}/{m_ReceiptDgHeaderInfos.Length}");
                if (missingColumns.Count > 0)
                {
                    Debug.WriteLine($"[{AppName}] - 누락 컬럼({missingColumns.Count}): {string.Join(", ", missingColumns)}");
                }

                bmpHeader2.Dispose();

                // 원하는 컬럼을 모두 얻었으면 종료
                if (matchedCount >= m_ReceiptDgHeaderInfos.Length)
                {
                    Debug.WriteLine($"[{AppName}] Step 2 완료: 목표 컬럼 모두 획득");
                    break;
                }
            }
            #endregion

            #region Step 3 - 컬럼 순서 조정
            // Step 3: 컬럼 순서 조정
            Debug.WriteLine($"[{AppName}] Step 3: 컬럼 순서 조정 시작");

            for (int x = 0; x < m_ReceiptDgHeaderInfos.Length; x++)
            {
                await Task.Delay(250);  // 캡처 전 안정화

                // 3-1. 헤더 전역 캡처 → 경계선 검출
                var (bmpHeader3, listLW3, columns3) = CaptureAndDetectColumnBoundaries(rcHeader, targetRow);
                if (bmpHeader3 == null)
                {
                    return new StdResult_Error($"[{AppName}] Step 3 헤더 캡처 실패 (컬럼 {x})", "OnecallAct_RcptRegPage/InitDG오더Async_03");
                }

                // 3-2. 전체 컬럼 OFR
                string[] texts3 = await OfrAllColumnsAsync(bmpHeader3, listLW3, columns3, headerGab, textHeight, bEdit: true);
                bmpHeader3.Dispose();

                // 3-3. 목표 컬럼 위치 찾기
                string targetText = m_ReceiptDgHeaderInfos[x].sName;
                int find = Array.IndexOf(texts3, targetText);

                if (find < 0)
                {
                    return new StdResult_Error($"[{AppName}] 목표 컬럼 '{targetText}' 찾기 실패", "OnecallAct_RcptRegPage/InitDG오더Async_04");
                }

                if (find == x) continue; // 같은 위치면 패스

                Debug.WriteLine($"[{AppName}] Step 3. 순서 조정: [{find}]'{targetText}' → [{x}]");

                // 3-4. 드래그
                Draw.Point ptStart = new Draw.Point(listLW3[find].nLeft + 10, headerGab + (textHeight / 2));
                Draw.Point ptTarget = new Draw.Point(listLW3[x].nLeft, ptStart.Y);

                await Simulation_Mouse.SafeMouseEvent_DragLeft_SmoothAsync(mRcpt.DG오더_hWnd, ptStart, ptTarget, bBkCursor: false, nMiliSec: 150);

                await Task.Delay(50);  // 드래그 후
            }

            Debug.WriteLine($"[{AppName}] Step 3 완료");
            #endregion

            #region Step 4 - 컬럼 너비 조정
            // Step 4: 컬럼 너비 조정
            Debug.WriteLine($"[{AppName}] Step 4: 컬럼 너비 조정 시작");

            await Task.Delay(250);  // 캡처 전 안정화

            // 4-1. 헤더 캡처 → 경계선 검출
            var (bmpHeader4, listLW4, columns4) = CaptureAndDetectColumnBoundaries(rcHeader, targetRow);
            if (bmpHeader4 == null)
            {
                return new StdResult_Error($"[{AppName}] Step 4 헤더 캡처 실패", "OnecallAct_RcptRegPage/InitDG오더Async_05");
            }
            bmpHeader4.Dispose();

            // 4-2. 뒤에서 앞으로 폭 조정
            for (int x = m_ReceiptDgHeaderInfos.Length; x > 0; x--)
            {
                int col = x - 1;
                Draw.Point ptStart = new Draw.Point(listLW4[col].nLeft + listLW4[col].nWidth, headerGab + (textHeight / 2));
                int dx = m_ReceiptDgHeaderInfos[col].nWidth - listLW4[col].nWidth;

                Debug.WriteLine($"[{AppName}] Step 4. 너비 조정: [{col}]'{m_ReceiptDgHeaderInfos[col].sName}' {listLW4[col].nWidth}px → {m_ReceiptDgHeaderInfos[col].nWidth}px (dx={dx})");

                await Simulation_Mouse.SafeMouseEvent_DragLeft_Smooth_HorizonAsync(mRcpt.DG오더_hWnd, ptStart, dx, bBkCursor: false, nMiliSec: 100);
                await Task.Delay(c_nWaitNormal);
            }

            Debug.WriteLine($"[{AppName}] Step 4 완료");
            #endregion
            Debug.WriteLine($"[{AppName}] InitDG오더Async 완료");
            return null;
        }
        catch (Exception ex)
        {
            return new StdResult_Error($"[{AppName}] InitDG오더Async 예외: {ex.Message}", "OnecallAct_RcptRegPage/InitDG오더Async_99");
        }
        finally
        {
            // 마우스 커서 위치 복원
            Std32Cursor.SetCursorPos_AbsDrawPt(ptCursorBackup);
        }
    }
    #endregion

    #region Helper Methods
    /// <summary>
    /// 헤더 캡처 및 컬럼 경계 검출 헬퍼
    /// </summary>
    private (Draw.Bitmap bmpHeader, List<OfrModel_LeftWidth> listLW, int columns) CaptureAndDetectColumnBoundaries(Draw.Rectangle rcHeader, int targetRow)
    {
        Draw.Bitmap bmpHeader = OfrService.CaptureScreenRect_InWndHandle(mRcpt.DG오더_hWnd, rcHeader);
        if (bmpHeader == null) return (null, null, 0);

        byte minBrightness = OfrService.GetMinBrightnessAtRow_FromColorBitmapFast(bmpHeader, targetRow);
        minBrightness += 2;

        bool[] boolArr = OfrService.GetBoolArray_FromColorBitmapRowFast(bmpHeader, targetRow, minBrightness, 2);
        List<OfrModel_LeftWidth> listLW = OfrService.GetLeftWidthList_FromBool1Array(boolArr, minBrightness);

        if (listLW == null || listLW.Count < 2)
            return (bmpHeader, listLW, 0);

        // 마지막 경계선 유지 (폭 조정에 필요)
        int columns = listLW.Count - 1;
        return (bmpHeader, listLW, columns);
    }

    /// <summary>
    /// 모든 컬럼 OFR 헬퍼
    /// </summary>
    private async Task<string[]> OfrAllColumnsAsync(Draw.Bitmap bmpHeader, List<OfrModel_LeftWidth> listLW, int columns, int gab, int height, bool bEdit = false)
    {
        string[] texts = new string[columns];

        for (int x = 0; x < columns; x++)
        {
            Draw.Rectangle rcColHeader = new Draw.Rectangle(listLW[x].nLeft, gab, listLW[x].nWidth, height);

            var result = await OfrWork_Common.OfrStr_ComplexCharSetAsync(bmpHeader, rcColHeader, bInvertRgb: false, bEdit: bEdit);

            texts[x] = result?.strResult;
        }

        return texts;
    }
    #endregion
}

#nullable restore
