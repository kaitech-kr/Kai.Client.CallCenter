using Kai.Client.CallCenter.Classes;
using Kai.Client.CallCenter.Classes.Class_Master;
using Kai.Client.CallCenter.OfrWorks;
using Kai.Client.CallCenter.Pages;
using Kai.Client.CallCenter.Windows;
using Kai.Common.NetDll_WpfCtrl.NetOFR;
using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static Kai.Client.CallCenter.Classes.CommonVars;
using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgWnd;
using Draw = System.Drawing;
using Media = System.Windows.Media;

namespace Kai.Client.CallCenter.Networks.NwOnecalls;
#nullable disable

public partial class OnecallAct_RcptRegPage
{
    #region 1. Helpers - 공용 헬퍼
    // Onecall SeqNo 가져오기
    private string GetOnecallSeqno(AutoAllocModel item) => item.NewOrder.Onecall;

    // 포커스 탈출 (빈 영역 클릭)
    private async Task EscapeFocusAsync(CancellationToken ct = default, int nDelay = c_nWaitVeryShort)
    {
        await Std32Mouse_Post.MousePostAsync_ClickLeft(mRcpt.검색섹션_hWnd포커스탈출);
        await Task.Delay(nDelay, ct);
    }

    public static bool IsHorizontalResizeCursor()
    {
        StdCommon32.CURSORINFO pci = new StdCommon32.CURSORINFO();
        pci.cbSize = Marshal.SizeOf(typeof(StdCommon32.CURSORINFO));
        if (StdWin32.GetCursorInfo(out pci))
        {
            IntPtr hResize = StdWin32.LoadCursor(IntPtr.Zero, StdCommon32.IDC_SIZEWE);
            return pci.hCursor == hResize;
        }
        return false;
    }

    // 데이터그리드 로우 클릭 (원콜 DG오더_rcRelSmallCells는 row=0부터 데이터)
    /// <param name="nRowIndex">로우 인덱스 (0-based)</param>
    //public async Task<bool> ClickDatagridRowAsync(int nRowIndex)
    //{
    //    Draw.Rectangle[,] rects = mRcpt.DG오더_rcRelSmallCells;
    //    Draw.Rectangle rcRow = rects[c_nCol클릭, nRowIndex];
    //    Draw.Point ptClick = new Draw.Point(rcRow.Left + 4, rcRow.Top + 4);

    //    await Std32Mouse_Post.MousePostAsync_ClickLeft_ptRel(mRcpt.DG오더_hWndTop, ptClick);
    //    await Task.Delay(c_nWaitUltraShort);
    //    await Std32Mouse_Post.MousePostAsync_ClickLeft(mRcpt.검색섹션_hWnd포커스탈출);
    //    await Task.Delay(c_nWaitShort);

    //    int brightness = Std32Pixcel.GetBrightness_PerPixel(mRcpt.DG오더_hWndTop, ptClick.X, ptClick.Y);

    //    return brightness < fInfo.접수등록Page_DG오더_nSelectdBright;
    //}

    // 로우 선택 여부 확인 (명도 기반)
    //public bool IsSelectedRow(int nRowIndex)
    //{
    //    Draw.Rectangle[,] rects = mRcpt.DG오더_rcRelSmallCells;
    //    Draw.Rectangle rcRow = rects[c_nCol클릭, nRowIndex];
    //    Draw.Point ptCheck = new Draw.Point(rcRow.Left + 4, rcRow.Top + 4);

    //    int brightness = Std32Pixcel.GetBrightness_PerPixel(mRcpt.DG오더_hWndTop, ptCheck.X, ptCheck.Y);

    //    return brightness < fInfo.접수등록Page_DG오더_nSelectdBright;
    //}

    // 버튼 클릭 → 확인창("예") 처리 → 버튼 Disabled 대기
    //private async Task<bool> Click버튼WaitDisableAsync(IntPtr hTarget, string buttonName, CancelTokenControl ctrl)
    //{
    //    // 1. 버튼 클릭
    //    await Std32Mouse_Post.MousePostAsync_ClickLeft(hTarget);

    //    // 2. 확인창 찾기 → "예" 클릭
    //    (IntPtr hWndParent, IntPtr hWndYesBtn) = (IntPtr.Zero, IntPtr.Zero);
    //    for (int i = 0; i < c_nRepeatShort; i++)
    //    {
    //        await Task.Delay(c_nWaitShort, ctrl.Token);
    //        (hWndParent, hWndYesBtn) = Std32Window.FindMainWindow_EmptyCaption_HavingChildButton(mInfo.Splash.TopWnd_uProcessId, "예");
    //        if (hWndYesBtn != IntPtr.Zero) break;
    //    }
    //    if (hWndYesBtn == IntPtr.Zero)
    //    {
    //        Debug.WriteLine($"[{AppName}] {buttonName} 확인창 찾기 실패");
    //        return false;
    //    }

    //    await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndYesBtn);

    //    // 3. 버튼 Disabled 대기
    //    for (int i = 0; i < c_nRepeatShort; i++)
    //    {
    //        await Task.Delay(c_nWaitShort, ctrl.Token);
    //        if (!Std32Window.IsWindowEnabled(hTarget))
    //        {
    //            Debug.WriteLine($"[{AppName}] {buttonName} 성공 확인 (버튼 Disabled)");
    //            return true;
    //        }
    //    }

    //    Debug.WriteLine($"[{AppName}] {buttonName} 버튼 Disabled 대기 실패");
    //    return false;
    //}

    // 화물중량 설정 (검증 포함, 최대 3회 반복)
    //private async Task<StdResult_Status> Set화물중량Async(string maxWeight, CancelTokenControl ctrl, int nRepeate = c_nRepeatShort)
    //{
    //    for (int retry = 0; retry < nRepeate; retry++)
    //    {
    //        // 1. 맨 앞에 클릭
    //        await Std32Mouse_Post.MousePostAsync_ClickLeft_RightBottom(mRcpt.접수섹션_hWnd화물중량, 1, 1);

    //        // 2. Ctrl+A 전체 선택
    //        await Task.Delay(c_nWaitUltraShort);
    //        //await Simulation_Keyboard.KeyPost_CtrlA_SelectAllAsync(mRcpt.접수섹션_hWnd화물중량);

    //        // 3. VK로 문자열 입력
    //        await Task.Delay(c_nWaitUltraShort);
    //        await Std32Key_Msg.PostKeyDown_VkStringAsync(mRcpt.접수섹션_hWnd화물중량, maxWeight);

    //        // 4. 검증
    //        await Task.Delay(c_nWaitShort);
    //        string current = Std32Window.GetWindowCaption(mRcpt.접수섹션_hWnd화물중량) ?? "";
    //        if (current == maxWeight)
    //        {
    //            Debug.WriteLine($"[{AppName}] 화물중량 설정 성공: {maxWeight}");
    //            return new StdResult_Status(StdResult.Success);
    //        }

    //        Debug.WriteLine($"[{AppName}] 화물중량 검증 실패 ({retry + 1}/3): 예상={maxWeight}, 실제={current}");
    //    }

    //    return new StdResult_Status(StdResult.Fail, $"화물중량 설정 실패: {maxWeight}");
    //}

    // 저장 버튼 클릭 → (확인창 있으면 "예" 클릭) → 상/하차지 클리어 대기
    //private async Task<StdResult_Status> SaveOrderAsync(CancelTokenControl ctrl)
    //{
    //    // 1. 저장 버튼 클릭
    //    await ctrl.WaitIfPausedOrCancelledAsync();
    //    await Std32Mouse_Post.MousePostAsync_ClickLeft(mRcpt.접수섹션_hWnd저장버튼);

    //    // 2. 상/하차지 클리어 대기 (+ 확인창 처리)
    //    for (int i = 0; i < c_nRepeatShort; i++)
    //    {
    //        await Task.Delay(c_nWaitShort, ctrl.Token);

    //        // 확인창 있으면 "예" 클릭
    //        var (hWndParent, hWndYesBtn) = Std32Window.FindMainWindow_EmptyCaption_HavingChildButton(
    //            mInfo.Splash.TopWnd_uProcessId, "예");
    //        if (hWndYesBtn != IntPtr.Zero)
    //        {
    //            await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndYesBtn);
    //            await Task.Delay(c_nWaitShort, ctrl.Token);
    //        }

    //        // 상/하차지 클리어 확인
    //        string caption상차 = Std32Window.GetWindowCaption(mRcpt.접수섹션_hWnd상차지주소);
    //        string caption하차 = Std32Window.GetWindowCaption(mRcpt.접수섹션_hWnd하차지주소);
    //        if (string.IsNullOrEmpty(caption상차) && string.IsNullOrEmpty(caption하차))
    //        {
    //            Debug.WriteLine($"[{AppName}] 저장 성공 확인");
    //            return new StdResult_Status(StdResult.Success);
    //        }
    //    }

    //    return new StdResult_Status(StdResult.Fail, "저장 확인 실패");
    //}

    // Edit 필드 비교 → 다르면 수정 → 검증
    //private async Task<(bool changed, StdResult_Status result)> UpdateEditIfChangedAsync(IntPtr hWnd, string dbValue, string fieldName, CancelTokenControl ctrl)
    //{
    //    string currentValue = Std32Window.GetWindowCaption(hWnd) ?? "";
    //    if (currentValue == dbValue)
    //        return (false, new StdResult_Status(StdResult.Success));

    //    // 수정
    //    Std32Window.SetWindowCaption(hWnd, dbValue ?? "");
    //    await Task.Delay(c_nWaitShort, ctrl.Token);

    //    // 검증
    //    string afterValue = Std32Window.GetWindowCaption(hWnd) ?? "";
    //    if (afterValue != (dbValue ?? ""))
    //    {
    //        Debug.WriteLine($"[{AppName}] {fieldName} 입력 검증 실패: 예상={dbValue}, 실제={afterValue}");
    //        return (true, new StdResult_Status(StdResult.Fail, $"{fieldName} 입력 검증 실패"));
    //    }

    //    Debug.WriteLine($"[{AppName}] {fieldName} 수정: {currentValue} → {dbValue}");
    //    return (true, new StdResult_Status(StdResult.Success));
    //}

    // ComboBox 필드 비교 → 다르면 수정 (SelectComboBoxItemAsync가 검증 포함)
    //private async Task<(bool changed, StdResult_Status result)> UpdateComboIfChangedAsync(IntPtr hWnd, CModel_ComboBox model, IntPtr hWndTop, Draw.Rectangle rcVerify, string fieldName, CancelTokenControl ctrl)
    //{
    //    // 현재값 OFR로 읽기
    //    using (Draw.Bitmap bmpVerify = OfrService.CaptureScreenRect_InWndHandle(hWndTop, rcVerify))
    //    {
    //        if (bmpVerify != null)
    //        {
    //            var ofrResult = await OfrWork_Common.OfrStr_ComplexCharSetAsync(bmpVerify, false, dWeight: c_dOfrWeight, false);
    //            if (ofrResult?.strResult == model.sYourName)
    //                return (false, new StdResult_Status(StdResult.Success));
    //        }
    //    }

    //    // 수정 (SelectComboBoxItemAsync가 검증 포함)
    //    await EscapeFocusAsync(ctrl.Token);
    //    var result = await SelectComboBoxItemAsync(hWnd, model, hWndTop, rcVerify);
    //    if (result.Result != StdResult.Success)
    //    {
    //        Debug.WriteLine($"[{AppName}] {fieldName} 선택 실패: {result.sErr}");
    //        return (true, result);
    //    }

    //    Debug.WriteLine($"[{AppName}] {fieldName} 수정: → {model.sYourName}");
    //    return (true, new StdResult_Status(StdResult.Success));
    //}

    // CheckBox 필드 비교 → 다르면 수정 (SetCheckBoxAsync가 검증 포함)
    //private async Task<(bool changed, StdResult_Status result)> UpdateCheckBoxIfChangedAsync(IntPtr hWnd, Draw.Rectangle rcOfrRelS, bool dbValue, string fieldName, CancelTokenControl ctrl)
    //{
    //    // 현재 상태 읽기 (기존 SetCheckBoxAsync 로직 활용)
    //    // 여기서는 OFR로 읽지 않고 SetCheckBoxAsync 내부에서 처리하도록 위임하거나
    //    // 직접 읽어서 비교 후 호출. 
    //    // 효율성을 위해 먼저 읽고 다를 때만 SetCheckBoxAsync 호출

    //    // 1. 현재 상태 읽기
    //    var resultChk = await OfrWork_Insungs.OfrImgReChkValue_RectInHWndAsync(mRcpt.접수섹션_hWndTop, rcOfrRelS, true);
    //    if (resultChk.bResult == dbValue)
    //        return (false, new StdResult_Status(StdResult.Success));

    //    // 2. 수정 (SetCheckBoxAsync가 검증 포함)
    //    var result = await SetCheckBoxAsync(hWnd, rcOfrRelS, dbValue, fieldName);
    //    if (result.Result != StdResult.Success)
    //    {
    //        Debug.WriteLine($"[{AppName}] {fieldName} 설정 실패: {result.sErr}");
    //        return (true, result);
    //    }

    //    Debug.WriteLine($"[{AppName}] {fieldName} 수정: → {dbValue}");
    //    return (true, new StdResult_Status(StdResult.Success));
    //}

    // 헤더 캡처 및 컬럼 경계 검출 헬퍼
    private (Draw.Bitmap bmpHeader, List<OfrModel_LeftWidth> listLW, int columns) CaptureAndDetectColumnBoundaries(Draw.Rectangle rcHeader, int targetRow)
    {
        Draw.Bitmap bmpHeader = OfrService.CaptureScreenRect_InWndHandle(mRcpt.DG오더_hWndTop, rcHeader);
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

    // 모든 컬럼 OFR 헬퍼
    private async Task<string[]> OfrAllColumnsAsync(Draw.Bitmap bmpHeader, List<OfrModel_LeftWidth> listLW, int columns, int gab, int height, bool bEdit = false)
    {
        string[] texts = new string[columns];

        for (int x = 0; x < columns; x++)
        {
            Draw.Rectangle rcColHeader = new Draw.Rectangle(listLW[x].nLeft, gab, listLW[x].nWidth, height);

            var result = await OfrWork_Common.OfrStr_ComplexCharSetAsync(bmpHeader, rcColHeader, bInvertRgb: false, bTextSave: true, dWeight: c_dOfrWeight);

            texts[x] = result?.strResult;
        }

        return texts;
    }

    // Datagrid 상태 검증 (컬럼 개수 -> 너비 -> 컬럼명 유효성 및 순서 확인)
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

        // 2. 각 컬럼 검증 (너비 -> 이름/순서)
        for (int x = 0; x < m_ReceiptDgHeaderInfos.Length; x++)
        {
            // 2-1. 컬럼 너비 체크 (물리 위치 기준)
            int actualWidth = listLW[x].nWidth;
            int expectedWidth = m_ReceiptDgHeaderInfos[x].nWidth;
            int widthDiff = Math.Abs(actualWidth - expectedWidth);

            if (widthDiff > COLUMN_WIDTH_TOLERANCE)
            {
                issues |= CEnum_DgValidationIssue.WrongWidth;
                Debug.WriteLine($"[ValidateDatagridState] 너비 불일치[{x}]: 실제={actualWidth}, 예상={expectedWidth}, 오차={widthDiff} (컬럼명: {columnTexts[x]})");
            }

            // 2-2. 컬럼명 유효성 및 순서 체크
            string columnText = columnTexts[x];
            string expectedName = m_ReceiptDgHeaderInfos[x].sName;

            if (columnText != expectedName)
            {
                // 정석 위치의 이름과 다르면, 다른 위치에라도 있는지 확인
                int index = Array.FindIndex(m_ReceiptDgHeaderInfos, h => h.sName == columnText);
                if (index < 0)
                {
                    issues |= CEnum_DgValidationIssue.InvalidColumn;
                    Debug.WriteLine($"[ValidateDatagridState] 유효하지 않은 컬럼[{x}]: '{columnText}' (예상: '{expectedName}')");
                }
                else
                {
                    issues |= CEnum_DgValidationIssue.WrongOrder;
                    Debug.WriteLine($"[ValidateDatagridState] 순서 불일치[{x}]: '{columnText}' (정석 위치: {index}, 현재 위치: {x})");
                }
            }
        }

        if (issues == CEnum_DgValidationIssue.None)
        {
            Debug.WriteLine($"[ValidateDatagridState] Datagrid 상태 정상");
        }

        return issues;
    }
    #endregion

    #region 2. DG State - DG오더 UI 상태
    // 데이터그리드 확장 상태 확인 (접수섹션 가시성 기준)
    public bool IsDG오더Expanded()
    {
        bool bVisible = Std32Window.IsWindowVisible(mRcpt.접수섹션_hWndTop);
        bool bExpanded = !bVisible;
        Debug.WriteLine($"[{AppName}] IsDG오더Expanded: 접수섹션Visible={bVisible}, Result={bExpanded}");
        return bExpanded;
    }

    // 데이터그리드 확장 (축소 상태일 때만)
    public async Task<bool> ExpandDG오더Async()
    {
        if (IsDG오더Expanded())
        {
            Debug.WriteLine($"[{AppName}] ExpandDG오더Async: 이미 확장 상태");
            return true;
        }

        Debug.WriteLine($"[{AppName}] ExpandDG오더Async: 확장 시도");
        await Std32Mouse_Post.MousePostAsync_ClickLeft(mRcpt.검색섹션_hWnd확장버튼);

        // 확장 대기 (높이 체크)
        for (int i = 0; i < c_nRepeatVeryShort; i++)
        {
            await Task.Delay(c_nWaitShort);
            if (IsDG오더Expanded())
            {
                Debug.WriteLine($"[{AppName}] ExpandDG오더Async: 확장 성공");
                return true;
            }
        }
        Debug.WriteLine($"[{AppName}] ExpandDG오더Async: 확장 실패");
        return false;
    }

    // 데이터그리드 축소 (확장 상태일 때만)
    public async Task<bool> CollapseDG오더Async()
    {
        if (!IsDG오더Expanded())
        {
            Debug.WriteLine($"[{AppName}] CollapseDG오더Async: 이미 축소 상태");
            return true;
        }

        Debug.WriteLine($"[{AppName}] CollapseDG오더Async: 축소 시도");
        await Std32Mouse_Post.MousePostAsync_ClickLeft(mRcpt.검색섹션_hWnd확장버튼);

        // 축소 대기 (높이 체크)
        for (int i = 0; i < c_nRepeatVeryShort; i++)
        {
            await Task.Delay(c_nWaitShort);
            if (!IsDG오더Expanded())
            {
                Debug.WriteLine($"[{AppName}] CollapseDG오더Async: 축소 성공");
                return true;
            }
        }
        Debug.WriteLine($"[{AppName}] CollapseDG오더Async: 축소 실패");
        return false;
    }
    #endregion

    #region 3. Input Helpers - 입력 공용함수
    // 상세주소 입력 공용함수 (상차지/하차지)
    //private async Task<StdResult_Status> Set상세주소Async(IntPtr hWnd주소, Draw.Rectangle rc권역, string detailAddr, CancelTokenControl ctrl)
    //{
    //    Draw.Bitmap bmpCheck = null;
    //    try
    //    {
    //        // 1. 상세주소에서 숫자 전 텍스트 분할
    //        var match = System.Text.RegularExpressions.Regex.Match(detailAddr, @"^(.*?)(\d.*)$");
    //        string captionSimple = match.Success ? match.Groups[1].Value : detailAddr;
    //
    //        IntPtr hFind = IntPtr.Zero;
    //
    //        for (int repeat = 1; repeat <= c_nRepeatShort; repeat++)
    //        {
    //            // 2. 숫자제외 텍스트 입력 + 엔터
    //            Std32Window.SetWindowCaption(hWnd주소, captionSimple);
    //            await Task.Delay(c_nWaitVeryShort);
    //            Std32Key_Msg.KeyPost_Click(hWnd주소, StdCommon32.VK_RETURN);
    //            await Task.Delay(c_nWaitVeryShort);
    //
    //            // 3. 리스트박스 뜰때까지 대기 + 엔터로 선택
    //            for (int i = 0; i < c_nRepeatShort; i++)
    //            {
    //                await Task.Delay(c_nWaitShort);
    //                hFind = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, fInfo.접수등록Page_접수_화물정보_ptChkRelM);
    //                if (hFind != mRcpt.접수섹션_hWnd화물정보)
    //                {
    //                    await Task.Delay(c_nWaitShort);
    //                    Std32Key_Msg.KeyPost_Click(hFind, StdCommon32.VK_RETURN);
    //                    await Task.Delay(c_nWaitShort);
    //                    break;
    //                }
    //            }
    //            if (hFind == mRcpt.접수섹션_hWnd화물정보) return new StdResult_Status(StdResult.Fail, "리스트박스 못찾음");
    //
    //            // 4. 리스트박스 사라질때까지 대기 + 전체 상세주소 입력
    //            for (int i = 0; i < c_nRepeatShort; i++)
    //            {
    //                await Task.Delay(c_nWaitShort);
    //                hFind = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, fInfo.접수등록Page_접수_화물정보_ptChkRelM);
    //                if (hFind == mRcpt.접수섹션_hWnd화물정보)
    //                {
    //                    await Task.Delay(c_nWaitShort);
    //                    Std32Window.SetWindowCaption(hWnd주소, detailAddr);
    //                    await Task.Delay(c_nWaitNormal);
    //                    break;
    //                }
    //            }
    //
    //            // 5. 권역 OFR로 검증
    //            StdResult_String resultStr = null;
    //            for (int i = 1; i <= c_nRepeatShort; i++)
    //            {
    //                await Task.Delay(c_nWaitNormal);
    //                bmpCheck = OfrService.CaptureScreenRect_InWndHandle(mRcpt.접수섹션_hWndTop, rc권역);
    //                if (bmpCheck == null) continue;
    //
    //                resultStr = await OfrWork_Common.OfrStr_ComplexCharSetAsync(bmpCheck, i == c_nRepeatShort, dWeight: c_dOfrWeight, i == c_nRepeatShort);
    //                if (!string.IsNullOrEmpty(resultStr.sErr)) continue;
    //
    //                if (resultStr.strResult.Length > 1) break;
    //            }
    //
    //            if (resultStr != null && resultStr.strResult.Length > 1) return new StdResult_Status(StdResult.Success);
    //        }
    //
    //        return new StdResult_Status(StdResult.Fail, "권역 읽기실패");
    //    }
    //    finally
    //    {
    //        bmpCheck?.Dispose();
    //    }
    //}

    // 콤보박스 항목 선택 공용함수 (OFR 검증 포함)
    private async Task<StdResult_Status> SelectComboBoxItemAsync(IntPtr hWndComboBox, CModel_ComboBox model, IntPtr hWndTop, Draw.Rectangle rcVerifyRelS)
    {
        Debug.WriteLine($"[{AppName}] SelectComboBoxItemAsync 시작: target={model.sYourName}, hWnd={hWndComboBox:X}");
        var sw = System.Diagnostics.Stopwatch.StartNew();

        for (int i = 1; i <= c_nRepeatShort; i++)
        {
            // 1. 콤보박스 아래 위치에서 핸들 백업
            Draw.Point ptCheck = StdUtil.GetAbsDrawPoint_BottomBelow(hWndComboBox);
            IntPtr hWndBefore = Std32Window.GetWndHandle_FromAbsDrawPt(ptCheck);
            IntPtr hWndDropdown = IntPtr.Zero;

            // 2. 콤보박스 클릭 (드롭다운 열기)
            await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndComboBox);

            // 3. 드롭다운 열릴 때까지 대기
            for (int j = 0; j < c_nRepeatShort; j++)
            {
                await Task.Delay(c_nWaitShort);
                hWndDropdown = Std32Window.GetWndHandle_FromAbsDrawPt(ptCheck);
                if (hWndDropdown != hWndBefore) break;
            }
            if (hWndDropdown == hWndBefore)
            {
                Debug.WriteLine($"[{AppName}] SelectComboBoxItemAsync [{i}] 드롭다운 열기 실패");
                continue;
            }

            // 4. 항목 클릭
            await Std32Mouse_Post.MousePostAsync_ClickLeft_ptRel(hWndDropdown, model.ptPos);

            // 5. 드롭다운 닫힐 때까지 대기
            for (int j = 0; j < c_nRepeatShort; j++)
            {
                await Task.Delay(c_nWaitShort);
                IntPtr hWndCurrent = Std32Window.GetWndHandle_FromAbsDrawPt(ptCheck);
                if (hWndCurrent != hWndDropdown) break;
            }

            // 6. OFR 검증
            await Task.Delay(c_nWaitShort);
            using (Draw.Bitmap bmpVerify = OfrService.CaptureScreenRect_InWndHandle(hWndTop, rcVerifyRelS))
            {
                if (bmpVerify == null)
                {
                    Debug.WriteLine($"[{AppName}] SelectComboBoxItemAsync [{i}] 캡처 실패");
                    continue;
                }

                var ofrResult = await OfrWork_Common.OfrStr_ComplexCharSetAsync(bmpVerify, true, dWeight: c_dOfrWeight);

                if (ofrResult == null || string.IsNullOrEmpty(ofrResult.strResult))
                {
                    Debug.WriteLine($"[{AppName}] SelectComboBoxItemAsync [{i}] OFR 실패");
                    continue;
                }

                if (ofrResult.strResult != model.sYourName)
                {
                    Debug.WriteLine($"[{AppName}] SelectComboBoxItemAsync [{i}] OFR 불일치: '{ofrResult.strResult}' != '{model.sYourName}'");
                    continue;
                }

                Debug.WriteLine($"[{AppName}] SelectComboBoxItemAsync 성공: {model.sYourName} ({sw.ElapsedMilliseconds}ms)");
                return new StdResult_Status(StdResult.Success);
            }
        }

        Debug.WriteLine($"[{AppName}] SelectComboBoxItemAsync 실패: {model.sYourName} ({sw.ElapsedMilliseconds}ms)");
        return new StdResult_Status(StdResult.Fail, $"콤보박스 선택 실패: {model.sYourName}");
    }

    // 체크박스 상태 설정 (OFR 기반)
    //private async Task<StdResult_Status> SetCheckBoxAsync(IntPtr hWndCheckBox, Draw.Rectangle rcOfrRelS, bool bWantChecked, string name)
    //{
    //    for (int i = 1; i <= c_nRepeatShort; i++)
    //    {
    //        await Task.Delay(c_nWaitNormal);
    //
    //        // 1. 현재 상태 읽기
    //        var resultChk = await OfrWork_Insungs.OfrImgReChkValue_RectInHWndAsync(mRcpt.접수섹션_hWndTop, rcOfrRelS, i == c_nRepeatShort);
    //        if (resultChk.bResult == null)
    //        {
    //            if (i < c_nRepeatShort) continue;
    //            else return new StdResult_Status(StdResult.Fail, $"{name} 인식 실패");
    //        }
    //
    //        // 2. 이미 원하는 상태면 성공
    //        if (resultChk.bResult == bWantChecked)
    //        {
    //            Debug.WriteLine($"[{AppName}] SetCheckBoxAsync: {name} 이미 {(bWantChecked ? "Checked" : "Unchecked")}");
    //            return new StdResult_Status(StdResult.Success);
    //        }
    //
    //        await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndCheckBox);
    //        for (int j = 1; j <= c_nRepeatShort; j++)
    //        {
    //            await Task.Delay(c_nWaitNormal);
    //
    //            // 상태 변경 확인
    //            resultChk = await OfrWork_Insungs.OfrImgUntilChkValue_RectInHWndAsync(mRcpt.접수섹션_hWndTop, bWantChecked, rcOfrRelS);
    //            if (resultChk.bResult == true)
    //            {
    //                Debug.WriteLine($"[{AppName}] SetCheckBoxAsync: {name} → {(bWantChecked ? "Checked" : "Unchecked")} 성공");
    //                return new StdResult_Status(StdResult.Success);
    //            }
    //        }
    //    }
    //
    //    return new StdResult_Status(StdResult.Fail, $"{name} 상태 변경 실패");
    //}
    #endregion

    #region 4. Converters - 데이터 변환
    //private CModel_ComboBox GetCarWeightResult(string sCarType, string sCarWeight)
    //{
    //    string my톤수 = Order_StatusPage.GetCarWeightString(sCarType, sCarWeight);
    //    if (string.IsNullOrEmpty(my톤수)) return fInfo.접수등록Page_접수_톤수Open[0];
    //
    //    foreach (var item in fInfo.접수등록Page_접수_톤수Open)
    //    {
    //        if (item.sMyName == my톤수) return item;
    //    }
    //
    //    return fInfo.접수등록Page_접수_톤수Open[0];
    //}
    //private string GetMaxCarWeight(CModel_ComboBox c)
    //{
    //    switch (c.sMyName)
    //    {
    //        case ""    : return "0.00";
    //        case "다마": return "0.30";
    //        case "라보": return "0.50";
    //        case "1t"  : return "1.00";
    //        case "1.4t": return "1.40";
    //        case "2.5t": return "2.50";
    //        case "3.5t": return "3.50";
    //        case "5t"  : return "5.00";
    //        case "8t"  : return "8.00";
    //        case "9.5t": return "9.50";
    //        case "11t": return "11.00";
    //        case "14t": return "14.00";
    //        case "15t": return "15.00";
    //        case "18t": return "18.00";
    //        case "22t": return "22.00";
    //        case "25t": return "25.00";
    //
    //        default: return "0.00";
    //    }
    //}
    //
    //private CModel_ComboBox GetTruckDetailResult(string sCarType, string sCarWeight)
    //{
    //    string myDetail = Order_StatusPage.GetTruckDetailString(sCarType, sCarWeight);
    //    if (string.IsNullOrEmpty(myDetail)) return fInfo.접수등록Page_접수_차종Open[0];
    //
    //    foreach (var item in fInfo.접수등록Page_접수_차종Open)
    //    {
    //        if (item.sMyName == myDetail) return item;
    //    }
    //
    //    return fInfo.접수등록Page_접수_차종Open[0];
    //}
    //
    //private CModel_ComboBox GetFeeTypeResult(string sFeeType)
    //{
    //    if (string.IsNullOrEmpty(sFeeType)) return fInfo.접수등록Page_접수_결재Open[4];
    //
    //    foreach (var item in fInfo.접수등록Page_접수_결재Open)
    //    {
    //        if (item.sMyName == sFeeType) return item;
    //    }
    //
    //    return fInfo.접수등록Page_접수_결재Open[4];
    //}
    //
    private CModel_ComboBox GetAutoRefreshResult(string sTime)
    {
        if (string.IsNullOrEmpty(sTime)) return fInfo.접수등록Page_검색_자동조회Open[0];

        foreach (var item in fInfo.접수등록Page_검색_자동조회Open)
        {
            if (item.sMyName == sTime) return item;
        }

        return fInfo.접수등록Page_검색_자동조회Open[0];
    }
    #endregion

    #region 5. OFR - 오더번호/상태 읽기
    // 지정된 로우의 오더번호 OFR - 셀 캡처 후 단음소 OFR
    //private async Task<StdResult_String> Get오더번호Async(int rowIndex, CancelTokenControl ctrl, int retryCount = c_nRepeatShort)
    //{
    //    try
    //    {
    //        const int COL_오더번호 = 2;
    //
    //        Draw.Rectangle rect오더번호Cell = mRcpt.DG오더_rcRelSmallCells[COL_오더번호, rowIndex];
    //        Debug.WriteLine($"[{AppName}] 오더번호 OFR - rowIndex={rowIndex}, 셀위치={rect오더번호Cell}");
    //
    //        for (int i = 1; i <= retryCount; i++)
    //        {
    //            await ctrl.WaitIfPausedOrCancelledAsync();
    //            Debug.WriteLine($"[{AppName}] ===== 오더번호 OFR 시도 {i}/{retryCount} =====");
    //
    //            Draw.Bitmap bmpCell = OfrService.CaptureScreenRect_InWndHandle(mRcpt.DG오더_hWndTop, rect오더번호Cell);
    //            if (bmpCell == null)
    //            {
    //                Debug.WriteLine($"[{AppName}] 오더번호 셀 캡처 실패 (시도 {i}/{retryCount})");
    //                if (i < retryCount) await Task.Delay(c_nWaitLong, ctrl.Token);
    //                continue;
    //            }
    //
    //            try
    //            {
    //                // 마지막 시도에서만 bEdit=true (수동 입력 대화상자)
    //                StdResult_String resultSeqno = await OfrWork_Common.OfrStr_SeqCharAsync(bmpCell, c_dOfrWeight, i == retryCount);
    //
    //                // ☒ 없는 완전한 결과만 성공
    //                if (!string.IsNullOrEmpty(resultSeqno.strResult) && !resultSeqno.strResult.Contains('☒'))
    //                {
    //                    Debug.WriteLine($"[{AppName}] 오더번호 획득 성공: '{resultSeqno.strResult}' (시도 {i}/{retryCount})");
    //                    return new StdResult_String(resultSeqno.strResult);
    //                }
    //                else
    //                {
    //                    Debug.WriteLine($"[{AppName}] OFR 실패: '{resultSeqno.strResult ?? resultSeqno.sErr}' (시도 {i}/{retryCount})");
    //                }
    //            }
    //            finally
    //            {
    //                bmpCell?.Dispose();
    //            }
    //
    //            if (i < retryCount) await Task.Delay(c_nWaitLong, ctrl.Token);
    //        }
    //
    //        return new StdResult_String($"오더번호 OFR 실패 ({retryCount}회 시도)", "Get오더번호Async_99");
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.WriteLine($"[{AppName}] Get오더번호Async 예외: {ex.Message}");
    //        return new StdResult_String(StdUtil.GetExceptionMessage(ex), "Get오더번호Async_999");
    //    }
    //}
    #endregion

    #region 6. Refresh & Query - 새로고침/조회
    // 새로고침 버튼 클릭 (포커스 탈출 → 클릭 → 클릭 확인 → 딜레이)
    //public async Task<StdResult_Status> Click새로고침버튼Async(CancelTokenControl ctrl, int retryCount = c_nRepeatShort)
    //{
    //    try
    //    {
    //        for (int i = 1; i <= retryCount; i++)
    //        {
    //            await ctrl.WaitIfPausedOrCancelledAsync();
    //
    //            // 1. 포커스 탈출
    //            await EscapeFocusAsync(ctrl.Token);
    //
    //            // 2. 새로고침 버튼 클릭
    //            await Std32Mouse_Post.MousePostAsync_ClickLeft(mRcpt.검색섹션_hWnd새로고침버튼);
    //
    //            // 3. 클릭 확인 (명도 <= 10 대기, 최대 500ms)
    //            bool bClicked = false;
    //            for (int j = 0; j < 500; j++)
    //            {
    //                int brightness = OfrService.GetPixelBrightnessFrmWndHandle(
    //                    mRcpt.검색섹션_hWnd새로고침버튼, fInfo.접수등록Page_검색_Focused_ptChkRelS);
    //                if (brightness <= 10)
    //                {
    //                    bClicked = true;
    //                    break;
    //                }
    //                await Task.Delay(1, ctrl.Token);
    //            }
    //
    //            if (!bClicked)
    //            {
    //                Debug.WriteLine($"[{AppName}] 새로고침 버튼 클릭 확인 실패 (시도 {i}/{retryCount})");
    //                continue;
    //            }
    //
    //            // 4. 오더량 기반 딜레이 (100개 단위, 최소 100ms)
    //            int delay = ((m_nLastTotalCount / 100) + 1) * 100;
    //            Debug.WriteLine($"[{AppName}] 새로고침 딜레이: {delay}ms (총계: {m_nLastTotalCount})");
    //            await Task.Delay(delay, ctrl.Token);
    //
    //            return new StdResult_Status(StdResult.Success, "새로고침 완료");
    //        }
    //
    //        return new StdResult_Status(StdResult.Fail, $"새로고침 버튼 클릭 {retryCount}회 모두 실패", "Click새로고침버튼Async_01");
    //    }
    //    catch (Exception ex)
    //    {
    //        return new StdResult_Status(StdResult.Fail, StdUtil.GetExceptionMessage(ex), "Click새로고침버튼Async_999");
    //    }
    //}
    //
    // 총계 OFR (확장상태에 따라 Small/Large 영역 선택)
    public async Task<StdResult_Int> Get총계Async(CancelTokenControl ctrl, int retryCount = c_nRepeatShort)
    {
        try
        {
            Draw.Rectangle rcTotal = IsDG오더Expanded()
                ? fInfo.접수등록Page_DG오더Large_rcTotalS
                : fInfo.접수등록Page_DG오더Small_rcTotalS;

            for (int i = 1; i <= retryCount; i++)
            {
                await ctrl.WaitIfPausedOrCancelledAsync();

                Draw.Bitmap bmpTotal = OfrService.CaptureScreenRect_InWndHandle(mRcpt.DG오더_hWndTop, rcTotal);
                if (bmpTotal == null)
                {
                    Debug.WriteLine($"[{AppName}] 총계 캡처 실패 (시도 {i}/{retryCount})");
                    if (i < retryCount) await Task.Delay(c_nWaitNormal, ctrl.Token);
                    continue;
                }

                try
                {
                    var result = await OfrWork_Common.OfrStr_ComplexCharSetAsync(bmpTotal, bTextSave: false, dWeight: c_dOfrWeight);
                    int nTotal = int.TryParse(new string(result.strResult?.Where(char.IsDigit).ToArray() ?? Array.Empty<char>()), out int n) ? n : -1;

                    if (nTotal >= 0)
                    {
                        Debug.WriteLine($"[{AppName}] 총계 OFR 성공: {nTotal} (시도 {i}/{retryCount})");
                        return new StdResult_Int(nTotal);
                    }
                }
                finally
                {
                    bmpTotal.Dispose();
                }

                if (i < retryCount) await Task.Delay(c_nWaitNormal, ctrl.Token);
            }

            return new StdResult_Int(-1, $"총계 OFR 실패 ({retryCount}회 시도)", "Get총계Async_99");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{AppName}] Get총계Async 예외: {ex.Message}");
            return new StdResult_Int(-1, StdUtil.GetExceptionMessage(ex), "Get총계Async_999");
        }
    }
    //
    // DG오더의 유효 로우 수 반환 (Small 모드 고정) - 배경보다 밝으면 데이터 있는 로우로 판단
    //public StdResult_Int GetValidRowCount()
    //{
    //    try
    //    {
    //        Draw.Rectangle[,] rects = mRcpt.DG오더_rcRelSmallCells;
    //        int maxRows = fInfo.접수등록Page_DG오더Small_RowsCount;
    //
    //        if (rects == null)
    //            return new StdResult_Int("DG오더_rcRelSmallCells 미초기화", "GetValidRowCount_01");
    //
    //        int nThreshold = mRcpt.DG오더_nBkMarginedBright; // 배경 밝기 + 10 마진
    //        int nValidRows = 0;
    //
    //        for (int row = 0; row < maxRows; row++)
    //        {
    //            // [col, row] 순서 - 첫 번째 열의 중앙 위치에서 밝기 체크
    //            int nCurBright = OfrService.GetPixelBrightnessFrmWndHandle(
    //                mRcpt.DG오더_hWndTop,
    //                rects[0, row].Right,
    //                rects[0, row].Top + 6);
    //
    //            // 배경보다 밝으면 데이터 있음
    //            if (nCurBright > nThreshold)
    //                nValidRows++;
    //            else
    //                break;
    //        }
    //
    //        return new StdResult_Int(nValidRows);
    //    }
    //    catch (Exception ex)
    //    {
    //        return new StdResult_Int(StdUtil.GetExceptionMessage(ex), "GetValidRowCount_999");
    //    }
    //}
    #endregion

    #region 7. Page Navigation - 페이지 관리
    // 페이지별 예상 첫 로우 번호 계산 (0-based 페이지 인덱스)
    /// <param name="nTotRows">총 행 수</param>
    /// <param name="nRowsPerPage">페이지당 행 수</param>
    /// <param name="pageIdx">페이지 인덱스 (0-based)</param>
    /// <returns>예상 첫 로우 번호</returns>
    //public static int GetExpectedFirstRowNum(int nTotRows, int nRowsPerPage, int pageIdx)
    //{
    //    // 총 페이지 수 계산
    //    int nTotPage = 1;
    //    if (nTotRows > nRowsPerPage)
    //    {
    //        nTotPage = nTotRows / nRowsPerPage;
    //        if (nTotRows % nRowsPerPage > 0)
    //            nTotPage += 1;
    //    }
    //
    //    int nCurPage = pageIdx + 1;
    //    int nNum = (nRowsPerPage * pageIdx) + 1;
    //
    //    if (nTotPage == 1) return 1;
    //
    //    if (nCurPage < nTotPage) return nNum;
    //
    //    // 마지막 페이지 특수 처리: 나머지 행이 있는 경우
    //    if (nTotRows % nRowsPerPage == 0) return nNum;
    //    else return nNum - nRowsPerPage + (nTotRows % nRowsPerPage);
    //}
    //
    // GetExpectedFirstRowNum 테스트 (무한루프 → 메시지박스 → 결과)
    //public async Task Test_GetExpectedFirstRowNumAsync()
    //{
    //    var ctrl = new CancelTokenControl();
    //    int nRowsPerPage = fInfo.접수등록Page_DG오더Small_RowsCount;
    //
    //    while (true)
    //    {
    //        // 1. 시작 확인
    //        var result시작 = System.Windows.MessageBox.Show(
    //            "GetExpectedFirstRowNum 테스트\n[예] 실행 / [아니오] 종료",
    //            "테스트", System.Windows.MessageBoxButton.YesNo);
    //        if (result시작 != System.Windows.MessageBoxResult.Yes) break;
    //
    //        // 2. 축소모드 강제
    //        await CollapseDG오더Async();
    //
    //        // 3. 새로고침 클릭
    //        var result새로고침 = await Click새로고침버튼Async(ctrl);
    //        if (result새로고침.Result != StdResult.Success)
    //        {
    //            System.Windows.MessageBox.Show($"새로고침 실패: {result새로고침.sErr}", "오류");
    //            continue;
    //        }
    //
    //        // 4. 총계 OFR
    //        var result총계 = await Get총계Async(ctrl);
    //        if (result총계.nResult < 0)
    //        {
    //            System.Windows.MessageBox.Show($"총계 OFR 실패: {result총계.sErr}", "오류");
    //            continue;
    //        }
    //
    //        int nTotRows = result총계.nResult;
    //
    //        // 5. 총 페이지 수 계산
    //        int nTotPage = 1;
    //        if (nTotRows > nRowsPerPage)
    //        {
    //            nTotPage = nTotRows / nRowsPerPage;
    //            if (nTotRows % nRowsPerPage > 0) nTotPage += 1;
    //        }
    //
    //        // 6. 각 페이지별 예상 첫 번호 계산
    //        var sb = new System.Text.StringBuilder();
    //        sb.AppendLine($"총계: {nTotRows}");
    //        sb.AppendLine($"페이지당: {nRowsPerPage}");
    //        sb.AppendLine($"총 페이지: {nTotPage}");
    //        sb.AppendLine("─────────────");
    //
    //        for (int pageIdx = 0; pageIdx < nTotPage; pageIdx++)
    //        {
    //            int expectedFirst = GetExpectedFirstRowNum(nTotRows, nRowsPerPage, pageIdx);
    //            sb.AppendLine($"페이지[{pageIdx}]: 첫 번호 = {expectedFirst}");
    //        }
    //
    //        System.Windows.MessageBox.Show(sb.ToString(), "GetExpectedFirstRowNum 결과");
    //    }
    //}
    //
    // 순번 컬럼(col=0)에서 첫 로우 번호 읽기 (OFR)
    ///// <returns>첫 로우 번호 (실패 시 -1)</returns>
    //public async Task<int> ReadFirstRowNumAsync()
    //{
    //    const int COL_순번 = 0;
    //    IntPtr hWndDG = mRcpt.DG오더_hWndTop;
    //    Draw.Rectangle[,] rects = mRcpt.DG오더_rcRelSmallCells;
    //
    //    if (rects == null)
    //    {
    //        Debug.WriteLine($"[{AppName}] ReadFirstRowNumAsync: DG오더_rcRelSmallCells 미초기화");
    //        return -1;
    //    }
    //
    //    int maxRows = fInfo.접수등록Page_DG오더Small_RowsCount;
    //    int firstNum = -1;
    //
    //    for (int rowIdx = 0; rowIdx < maxRows; rowIdx++)
    //    {
    //        // 1. 순번 컬럼(col=0) 캡처
    //        Draw.Rectangle rcNo = rects[COL_순번, rowIdx];
    //        Draw.Bitmap bmpNo = OfrService.CaptureScreenRect_InWndHandle(hWndDG, rcNo);
    //        if (bmpNo == null) continue;
    //
    //        // 2. OFR (숫자 - 단음소)
    //        StdResult_String resultNo = await OfrWork_Common.OfrStr_SeqCharAsync(bmpNo, c_dOfrWeight, false);
    //        bmpNo.Dispose();
    //
    //        if (!string.IsNullOrEmpty(resultNo.strResult))
    //        {
    //            int curNum = StdConvert.StringToInt(resultNo.strResult, -1);
    //            if (curNum >= 1)
    //            {
    //                firstNum = curNum - rowIdx;
    //                Debug.WriteLine($"[{AppName}] ReadFirstRowNumAsync: rowIdx={rowIdx}, curNum={curNum}, firstNum={firstNum}");
    //                break;
    //            }
    //        }
    //    }
    //
    //    return firstNum;
    //}
    //
    // ReadFirstRowNumAsync 테스트 (2중 루프: 외부=새로고침, 내부=페이지별 자동 검증)
    //public async Task Test_ReadFirstRowNumAsync()
    //{
    //    var ctrl = new CancelTokenControl();
    //    int nRowsPerPage = fInfo.접수등록Page_DG오더Small_RowsCount;
    //
    //    // 외부 루프: 새로고침 단위
    //    while (true)
    //    {
    //        // 1. 시작 확인
    //        var result시작 = System.Windows.MessageBox.Show(
    //            "ReadFirstRowNumAsync 테스트\n[예] 새로고침 후 시작 / [아니오] 종료",
    //            "테스트", System.Windows.MessageBoxButton.YesNo);
    //        if (result시작 != System.Windows.MessageBoxResult.Yes) break;
    //
    //        // 2. 축소모드 강제
    //        await CollapseDG오더Async();
    //
    //        // 3. 새로고침 클릭
    //        var result새로고침 = await Click새로고침버튼Async(ctrl);
    //        if (result새로고침.Result != StdResult.Success)
    //        {
    //            System.Windows.MessageBox.Show($"새로고침 실패: {result새로고침.sErr}", "오류");
    //            continue;
    //        }
    //
    //        // 4. 총계 OFR
    //        var result총계 = await Get총계Async(ctrl);
    //        if (result총계.nResult < 0)
    //        {
    //            System.Windows.MessageBox.Show($"총계 OFR 실패: {result총계.sErr}", "오류");
    //            continue;
    //        }
    //
    //        int nTotRows = result총계.nResult;
    //
    //        // 5. 총 페이지 수 계산
    //        int nTotPage = 1;
    //        if (nTotRows > nRowsPerPage)
    //        {
    //            nTotPage = nTotRows / nRowsPerPage;
    //            if (nTotRows % nRowsPerPage > 0) nTotPage += 1;
    //        }
    //
    //        // 6. 결과 수집
    //        var sb = new System.Text.StringBuilder();
    //        sb.AppendLine($"총계: {nTotRows}, 페이지당: {nRowsPerPage}, 총 페이지: {nTotPage}");
    //        sb.AppendLine("─────────────────────────────");
    //
    //        bool bAllMatch = true;
    //
    //        // 내부 루프: 페이지별 자동 검증
    //        for (int pageIdx = 0; pageIdx < nTotPage; pageIdx++)
    //        {
    //            // 페이지 이동 (첫 페이지 제외)
    //            if (pageIdx > 0)
    //            {
    //                await ScrollPageDownAsync();
    //            }
    //
    //            // 예상 vs 실제 비교
    //            int expectedFirst = GetExpectedFirstRowNum(nTotRows, nRowsPerPage, pageIdx);
    //            int actualFirst = await ReadFirstRowNumAsync();
    //            bool bMatch = (expectedFirst == actualFirst);
    //            if (!bMatch) bAllMatch = false;
    //
    //            sb.AppendLine($"페이지[{pageIdx}]: 예상={expectedFirst}, 실제={actualFirst} {(bMatch ? "✓" : "✗")}");
    //        }
    //
    //        sb.AppendLine("─────────────────────────────");
    //        sb.AppendLine($"결과: {(bAllMatch ? "모두 일치 ✓" : "불일치 있음 ✗")}");
    //
    //        System.Windows.MessageBox.Show(sb.ToString(), "ReadFirstRowNumAsync 결과");
    //    }
    //}
    //
    // 스크롤바 핸들 얻기 (1페이지 초과일 때만 호출)
    //private IntPtr GetVScrollBarHandle()
    //{
    //    return Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.DG오더_hWndTop, fInfo.접수등록Page_DG오더VScroll_BarCenter_ptChkRelM);
    //}
    //
    // 다음 페이지로 이동 (Page Down 클릭)
    //public async Task ScrollPageDownAsync()
    //{
    //    IntPtr hWndScroll = GetVScrollBarHandle();
    //    if (hWndScroll == IntPtr.Zero)
    //    {
    //        Debug.WriteLine($"[{AppName}] ScrollPageDownAsync: 스크롤바 핸들 없음");
    //        return;
    //    }
    //
    //    await Std32Mouse_Post.MousePostAsync_ClickLeft_ptRel(hWndScroll, fInfo.접수등록Page_DG오더VScroll_Down페이지_ptChkRelS);
    //    await Task.Delay(c_nWaitNormal);
    //}
    //
    // 이전 페이지로 이동 (Page Up 클릭)
    //public async Task ScrollPageUpAsync()
    //{
    //    IntPtr hWndScroll = GetVScrollBarHandle();
    //    if (hWndScroll == IntPtr.Zero)
    //    {
    //        Debug.WriteLine($"[{AppName}] ScrollPageUpAsync: 스크롤바 핸들 없음");
    //        return;
    //    }
    //
    //    await Std32Mouse_Post.MousePostAsync_ClickLeft_ptRel(hWndScroll, fInfo.접수등록Page_DG오더VScroll_Up페이지_ptChkRelS);
    //    await Task.Delay(c_nWaitNormal);
    //}
    //
    // 1로우 아래로 이동 (Row Down 클릭)
    //public async Task ScrollRowDownAsync()
    //{
    //    IntPtr hWndScroll = GetVScrollBarHandle();
    //    if (hWndScroll == IntPtr.Zero)
    //    {
    //        Debug.WriteLine($"[{AppName}] ScrollRowDownAsync: 스크롤바 핸들 없음");
    //        return;
    //    }
    //
    //    await Std32Mouse_Post.MousePostAsync_ClickLeft_ptRel(hWndScroll, fInfo.접수등록Page_DG오더VScroll_Down버튼_ptChkRelS);
    //    await Task.Delay(c_nWaitShort);
    //}
    //
    // 1로우 위로 이동 (Row Up 클릭)
    //public async Task ScrollRowUpAsync()
    //{
    //    IntPtr hWndScroll = GetVScrollBarHandle();
    //    if (hWndScroll == IntPtr.Zero)
    //    {
    //        Debug.WriteLine($"[{AppName}] ScrollRowUpAsync: 스크롤바 핸들 없음");
    //        return;
    //    }
    //
    //    await Std32Mouse_Post.MousePostAsync_ClickLeft_ptRel(hWndScroll, fInfo.접수등록Page_DG오더VScroll_Up버튼_ptChkRelS);
    //    await Task.Delay(c_nWaitShort);
    //}
    //
    // 페이지 검증 및 자동 조정
    ///// <param name="nExpectedFirstNum">예상 첫 번호</param>
    ///// <param name="ctrl">취소 토큰</param>
    ///// <param name="nRetryCount">재시도 횟수 (기본값: 3)</param>
    ///// <returns>성공/실패</returns>
    //public async Task<StdResult_Status> VerifyAndAdjustPageAsync(int nExpectedFirstNum, CancelTokenControl ctrl, int nRetryCount = c_nRepeatShort)
    //{
    //    int nRowsPerPage = fInfo.접수등록Page_DG오더Small_RowsCount;
    //
    //    for (int retry = 0; retry < nRetryCount; retry++)
    //    {
    //        await ctrl.WaitIfPausedOrCancelledAsync();
    //
    //        // 1. 실제 번호 OFR
    //        int nActualFirstNum = await ReadFirstRowNumAsync();
    //
    //        // 2. 일치하면 성공
    //        if (nExpectedFirstNum == nActualFirstNum)
    //        {
    //            return new StdResult_Status(StdResult.Success);
    //        }
    //
    //        Debug.WriteLine($"[{AppName}] VerifyAndAdjustPageAsync: 불일치 (시도 {retry + 1}/{nRetryCount}) - 예상={nExpectedFirstNum}, 실제={nActualFirstNum}");
    //
    //        // 3. 불일치 → 조정 (마지막 시도 아니면)
    //        if (retry < nRetryCount - 1)
    //        {
    //            // 차이 계산
    //            int diff = nActualFirstNum - nExpectedFirstNum;
    //            int absDiff = Math.Abs(diff);
    //
    //            int pageClicks = absDiff / nRowsPerPage;
    //            int rowClicks = absDiff % nRowsPerPage;
    //
    //            // 최적화: rowClicks > 절반이면 역방향이 더 효율적
    //            bool bReverse = false;
    //            if (rowClicks > nRowsPerPage / 2)
    //            {
    //                pageClicks += 1;
    //                rowClicks = nRowsPerPage - rowClicks;
    //                bReverse = true;
    //            }
    //
    //            // 방향 결정: diff > 0 (실제가 더 큼) → 위로 스크롤, diff < 0 → 아래로 스크롤
    //            bool bScrollUp = (diff > 0 && !bReverse) || (diff < 0 && bReverse);
    //
    //            Debug.WriteLine($"[{AppName}] 스크롤 조정: {(bScrollUp ? "UP" : "DOWN")} - {pageClicks}페이지 + {rowClicks}로우");
    //
    //            // 페이지 스크롤
    //            for (int i = 0; i < pageClicks; i++)
    //            {
    //                if (bScrollUp)
    //                    await ScrollPageUpAsync();
    //                else
    //                    await ScrollPageDownAsync();
    //            }
    //
    //            // 로우 스크롤
    //            for (int i = 0; i < rowClicks; i++)
    //            {
    //                if (bScrollUp)
    //                    await ScrollRowUpAsync();
    //                else
    //                    await ScrollRowDownAsync();
    //            }
    //
    //            await Task.Delay(c_nWaitNormal, ctrl.Token);
    //        }
    //    }
    //
    //    return new StdResult_Status(StdResult.Fail, $"페이지 조정 {nRetryCount}회 모두 실패", "VerifyAndAdjustPageAsync");
    //}
    #endregion

    #region 8. Row OFR - DG Row 데이터 읽기
    // 데이터그리드 Row에서 오더번호 읽기 (숫자 OFR - 단음소)
    /// <param name="bmpPage">전체 페이지 비트맵 (재사용)</param>
    /// <param name="rectSeqno">오더번호 셀 Rectangle</param>
    /// <param name="bInvertRgb">RGB 반전 여부 (선택된 행인 경우 true)</param>
    /// <param name="ctrl">취소 토큰</param>
    /// <returns>StdResult_String (오더번호)</returns>
    //public async Task<StdResult_String> GetRowSeqnoAsync(Draw.Bitmap bmpPage, Draw.Rectangle rectSeqno, bool bInvertRgb, CancelTokenControl ctrl)
    //{
    //    await ctrl.WaitIfPausedOrCancelledAsync();
    //    return await OfrWork_Common.OfrStr_SeqCharAsync(bmpPage, rectSeqno, bInvertRgb, c_dOfrWeight); // 영역추출 못할시 가중치조정
    //}
    //
    // 데이터그리드 Row에서 상태 읽기 (한글 OFR - 다음소)
    ///// <param name="bmpPage">전체 페이지 비트맵 (재사용)</param>
    ///// <param name="rowIdx">로우 인덱스</param>
    ///// <returns>상태 문자열</returns>
    //public async Task<StdResult_String> GetRowStatusAsync(Draw.Bitmap bmpPage, int rowIdx)
    //{
    //    Draw.Rectangle rectStatus = mRcpt.DG오더_rcRelSmallCells[c_nCol처리상태, rowIdx];
    //    return await OfrWork_Common.OfrStr_ComplexCharSetAsync(bmpPage, rectStatus, bInvertRgb: false, bTextSave: true, c_dOfrWeight, bEdit: true);
    //}
    #endregion

    #region 9. 마우스 드래그 Methods

    // [스나이퍼 엔진] 수평 정밀 드래그 (재시도 + 실시간 Crawl + 이탈 감지 통합), ptTargetRel: 목표 좌표 (null이면 dx 기반), gripCheck: 드래그 중 이탈 감지 로직
    public static async Task<bool> DragAsync_Horizontal_FromBoundary(IntPtr hWnd, Draw.Point ptStartRel,
        Draw.Point? ptTargetRel = null, int dx = 0, Func<bool> gripCheck = null, int nRetryCount = 5, int nMiliSec = 100, int nSafetyMargin = 5, int nDelayAtSafety = 20)
    {
        // 목표 좌표 결정 (좌표 우선, 없으면 dx 기반 - Y축은 시작점 유지)
        Draw.Point targetPoint = ptTargetRel ?? new Draw.Point(ptStartRel.X + dx, ptStartRel.Y);

        for (int retry = 1; retry <= nRetryCount; retry++)
        {
            Draw.Point ptBk = Std32Cursor.GetCursorPos_AbsDrawPt();
            bool bSuccess = false;
            try
            {
                Std32Window.SetForegroundWindow(hWnd);

                // 1. 절대 좌표 타겟팅
                Std32Cursor.SetCursorPos_RelDrawPt(hWnd, ptStartRel);
                Draw.Point ptFirstAbs = Std32Cursor.GetCursorPos_AbsDrawPt();

                Std32Cursor.SetCursorPos_RelDrawPt(hWnd, targetPoint);
                Draw.Point ptTargetAbs = Std32Cursor.GetCursorPos_AbsDrawPt();

                // 시작점 복귀
                Std32Cursor.SetCursorPos_AbsDrawPt(ptFirstAbs);
                await Task.Delay(50);

                // 초기 그립 확인 (눌러보기 전)
                if (gripCheck != null && !gripCheck()) { bSuccess = false; continue; }

                Std32Mouse_Event.MouseEvent_LeftBtnDown();
                await Task.Delay(100);

                // 2. 질주 구간 (Time-based Glide) - 수평 이동만
                Stopwatch sw = Stopwatch.StartNew();
                bool bInGrip = true;

                int totalDx = ptTargetAbs.X - ptFirstAbs.X;
                int nDirX = Math.Sign(totalDx);
                int safetyOffsetX = nDirX * nSafetyMargin;
                int intermediateTargetX = ptTargetAbs.X - safetyOffsetX;
                int intermediateDx = intermediateTargetX - ptFirstAbs.X;

                while (sw.ElapsedMilliseconds < nMiliSec)
                {
                    double ratio = (double)sw.ElapsedMilliseconds / nMiliSec;
                    int moveX = (int)(intermediateDx * ratio);
                    Std32Cursor.SetCursorPos_AbsDrawPt(new Draw.Point(ptFirstAbs.X + moveX, ptFirstAbs.Y));

                    if (bInGrip && gripCheck != null && !gripCheck()) bInGrip = false;
                    await Task.Delay(10);
                }

                // 3. 정밀 안착 구간 (Real-time Closed-loop Crawl) - 수평만
                if (nSafetyMargin > 0)
                {
                    if (nDelayAtSafety > 0) await Task.Delay(nDelayAtSafety);

                    int maxSteps = 50;
                    while (maxSteps-- > 0)
                    {
                        Draw.Point ptCur = Std32Cursor.GetCursorPos_AbsDrawPt();
                        if (ptCur.X == ptTargetAbs.X) break;

                        int cDirX = Math.Sign(ptTargetAbs.X - ptCur.X);
                        Std32Cursor.SetCursorPos_AbsDrawPt(new Draw.Point(ptCur.X + cDirX, ptFirstAbs.Y));
                        await Task.Delay(5);

                        if (bInGrip && gripCheck != null && !gripCheck()) bInGrip = false;
                    }
                }

                // 4. 최종 안착 및 해제
                Std32Cursor.SetCursorPos_AbsDrawPt(new Draw.Point(ptTargetAbs.X, ptFirstAbs.Y));
                await Task.Delay(20);

                Std32Mouse_Event.MouseEvent_LeftBtnUp();
                await Task.Delay(30);

                bSuccess = bInGrip;
            }
            catch { bSuccess = false; }
            finally { Std32Cursor.SetCursorPos_AbsDrawPt(ptBk); }

            if (bSuccess) return true;

            Debug.WriteLine($"[DRAG RETRY] 드래그 이탈 감지됨. 재시도 중... ({retry}/{nRetryCount})");
            await Task.Delay(200);
        }
        return false;
    }

    // [원콜 전용] 심해(100px) 주행 및 커서 기반 유실 감지 엔진
    // ptStartRel: 드래그 시작 좌표, ptTargetRel: 목표 좌표 (dx 우선), nRetryCount: 재시도 횟수
    public static async Task<bool> DragAsync_Horizontal_FromCenter(IntPtr hWnd, Draw.Point ptStartRel,
        Draw.Point? ptTargetRel = null, int dx = 0, Func<bool> gripCheck = null, int nRetryCount = 5, int nMiliSec = 500, int nSafetyMargin = 5, int nDelayAtSafety = 20, int nBackgroundBright = 0)
    {
        Draw.Point targetPointRel = ptTargetRel ?? new Draw.Point(ptStartRel.X + dx, ptStartRel.Y);

        for (int retry = 1; retry <= nRetryCount; retry++)
        {
            try
            {
                // 1. 출발지로 커서 이동
                Std32Window.SetForegroundWindow(hWnd);
                Std32Cursor.SetCursorPos_RelDrawPt(hWnd, ptStartRel);
                Draw.Point ptFirstAbs = Std32Cursor.GetCursorPos_AbsDrawPt();

                Std32Cursor.SetCursorPos_RelDrawPt(hWnd, targetPointRel);
                Draw.Point ptTargetAbs = Std32Cursor.GetCursorPos_AbsDrawPt();

                // 출발지 재안착
                Std32Cursor.SetCursorPos_AbsDrawPt(ptFirstAbs);
                await Task.Delay(50);

                // 2. 좌측 버튼 다운
                Std32Mouse_Event.MouseEvent_LeftBtnDown();
                await Task.Delay(20);

                // [Phase 1] 100px 수직 하강 (초정밀 견인)
                int deepGlideY = ptFirstAbs.Y + 100;
                for (int vStep = 1; vStep <= 10; vStep++)
                {
                    Std32Cursor.SetCursorPos_AbsDrawPt(new Draw.Point(ptFirstAbs.X, ptFirstAbs.Y + (vStep * 10)));
                    await Task.Delay(15);
                }
                await Task.Delay(80); // 심해 안착 대기 (완전하게)

                // 4. 심해 주행 (Ease-In-Out) - 거리 비례 속도 최적화
                int totalDx = ptTargetAbs.X - ptFirstAbs.X;
                int nDirX = Math.Sign(totalDx);
                int crawlMargin = 5;
                if (Math.Abs(totalDx) <= crawlMargin) crawlMargin = 0;

                int mainMoveDx = totalDx - (nDirX * crawlMargin);

                // [스마트 타임] 거리에 비례하여 지능적으로 시간 산출 (1.5ms/px + 기본 200ms)
                long moveTime = (long)(Math.Abs(totalDx) * 1.5) + 200;

                Stopwatch sw = Stopwatch.StartNew();
                IntPtr hArrow = StdWin32.LoadCursor(IntPtr.Zero, StdCommon32.IDC_ARROW);

                while (sw.ElapsedMilliseconds < moveTime)
                {
                    // 주행 중 유실 감지
                    if (Std32Cursor.GetCurrentCursorHandle() == hArrow)
                    {
                        Debug.WriteLine($"[DragCenter] 주행 중 유실 (Arrow 복귀). 재시도...");
                        throw new Exception("Drag Grip Lost");
                    }

                    double ratio = (double)sw.ElapsedMilliseconds / moveTime;
                    double easeRatio = (1 - Math.Cos(ratio * Math.PI)) / 2;
                    int currentDx = (int)(mainMoveDx * easeRatio);

                    Std32Cursor.SetCursorPos_AbsDrawPt(new Draw.Point(ptFirstAbs.X + currentDx, deepGlideY));
                    await Task.Delay(5);
                }

                // 5. 정밀 안착 구간 (Deep Sea Crawl)
                int maxCrawlSteps = 50;
                while (maxCrawlSteps-- > 0)
                {
                    Draw.Point ptCur = Std32Cursor.GetCursorPos_AbsDrawPt();
                    if (Math.Abs(ptCur.X - ptTargetAbs.X) <= 1) break;

                    int moveDir = Math.Sign(ptTargetAbs.X - ptCur.X);
                    Std32Cursor.SetCursorPos_AbsDrawPt(new Draw.Point(ptCur.X + moveDir, deepGlideY));
                    await Task.Delay(5);
                }

                // [Phase 3] 수직 상승 및 드랍 (deepGlideY -> 15px)
                for (int vStep = 3; vStep >= 0; vStep--)
                {
                    int stepY = ptFirstAbs.Y + (vStep * 25);
                    Std32Cursor.SetCursorPos_AbsDrawPt(new Draw.Point(ptTargetAbs.X, stepY));
                    await Task.Delay(15);
                }

                await Task.Delay(200);
                Std32Mouse_Event.MouseEvent_LeftBtnUp();
                await Task.Delay(200);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DragCenter] 오류 발생 ({retry}/{nRetryCount}): {ex.Message}");
            }

            // 실패 시 버튼 떼고 대기 후 재시도
            Std32Mouse_Event.MouseEvent_LeftBtnUp();
            await Task.Delay(200);
        }
        return false;
    }

    #endregion

    #region 10. Test Methods - 테스트 함수
    // DG오더 셀 영역 시각화 테스트 (TransparantWnd 활용)
    //public void Test_DrawLargeCellRects()
    //{
    //    try
    //    {
    //        Debug.WriteLine($"[{AppName}] Test_DrawAllCellRects 시작");
    //
    //        // 1. DG오더 핸들 체크
    //        if (mRcpt.DG오더_hWndTop == IntPtr.Zero)
    //        {
    //            System.Windows.MessageBox.Show("DG오더_hWnd가 초기화되지 않았습니다.", "오류");
    //            return;
    //        }
    //
    //        // 2. Cell Rect 배열 체크
    //        if (mRcpt.DG오더_rcRelLargeCells == null)
    //        {
    //            System.Windows.MessageBox.Show("DG오더_rcRelLargeCells가 초기화되지 않았습니다.", "오류");
    //            return;
    //        }
    //
    //        int colCount = mRcpt.DG오더_rcRelLargeCells.GetLength(0);  // [col, row] 순서
    //        int rowCount = mRcpt.DG오더_rcRelLargeCells.GetLength(1);
    //        Debug.WriteLine($"[{AppName}] Cell 배열: {colCount}열 x {rowCount}행");
    //
    //        // 3. TransparantWnd 오버레이 생성 (DG오더 위치 기준)
    //        TransparantWnd.CreateOverlay(mRcpt.DG오더_hWndTop);
    //        TransparantWnd.ClearBoxes();
    //
    //        // 5. 모든 셀 영역 그리기 (두께 1, 빨간색)
    //        int cellCount = 0;
    //        for (int row = 0; row < rowCount; row++)
    //        {
    //            for (int col = 0; col < colCount; col++)
    //            {
    //                Draw.Rectangle rc = mRcpt.DG오더_rcRelLargeCells[col, row];
    //                TransparantWnd.DrawBoxAsync(rc, strokeColor: Media.Colors.Red, thickness: 1);
    //                cellCount++;
    //            }
    //        }
    //
    //        Debug.WriteLine($"[{AppName}] {cellCount}개 셀 영역 그리기 완료");
    //
    //        // 5. MsgBox 표시 (확인 후 오버레이 삭제)
    //        System.Windows.MessageBox.Show(
    //            $"원콜 DG오더 셀 영역 테스트\n\n" +
    //            $"행: {rowCount}\n" +
    //            $"열: {colCount}\n" +
    //            $"총 셀: {cellCount}개\n\n" +
    //            $"확인을 누르면 오버레이가 제거됩니다.",
    //            "셀 영역 테스트");
    //
    //        // 6. 오버레이 삭제
    //        TransparantWnd.DeleteOverlay();
    //        Debug.WriteLine($"[{AppName}] Test_DrawAllCellRects 완료");
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.WriteLine($"[{AppName}] 예외 발생: {ex.Message}");
    //        System.Windows.MessageBox.Show($"테스트 중 오류 발생:\n{ex.Message}", "오류");
    //        TransparantWnd.DeleteOverlay();
    //    }
    //}
    //
    // Small 셀 영역 시각화 테스트
    //public void Test_DrawSmallCellRects()
    //{
    //    try
    //    {
    //        Debug.WriteLine($"[{AppName}] Test_DrawSmallCellRects 시작");
    //
    //        if (mRcpt.DG오더_hWndTop == IntPtr.Zero)
    //        {
    //            System.Windows.MessageBox.Show("DG오더_hWnd가 초기화되지 않았습니다.", "오류");
    //            return;
    //        }
    //
    //        if (mRcpt.DG오더_rcRelSmallCells == null)
    //        {
    //            System.Windows.MessageBox.Show("DG오더_rcRelSmallCells가 초기화되지 않았습니다.", "오류");
    //            return;
    //        }
    //
    //        int rowCount = mRcpt.DG오더_rcRelSmallCells.GetLength(0);
    //        int colCount = mRcpt.DG오더_rcRelSmallCells.GetLength(1);
    //
    //        System.Windows.MessageBox.Show("Small 셀 영역 그리기 시작", "Debug");
    //        TransparantWnd.CreateOverlay(mRcpt.DG오더_hWndTop);
    //        TransparantWnd.ClearBoxes();
    //
    //        for (int row = 0; row < rowCount; row++)
    //        {
    //            for (int col = 0; col < colCount; col++)
    //            {
    //                Draw.Rectangle rc = mRcpt.DG오더_rcRelSmallCells[col, row];
    //                TransparantWnd.DrawBoxAsync(rc, strokeColor: Media.Colors.Red, thickness: 1);
    //            }
    //        }
    //
    //        System.Windows.MessageBox.Show($"Small 셀 영역 완료\n{rowCount}행 x {colCount}열", "Debug");
    //        TransparantWnd.DeleteOverlay();
    //        Debug.WriteLine($"[{AppName}] Test_DrawSmallCellRects 완료");
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.WriteLine($"[{AppName}] 예외 발생: {ex.Message}");
    //        System.Windows.MessageBox.Show($"테스트 중 오류 발생:\n{ex.Message}", "오류");
    //        TransparantWnd.DeleteOverlay();
    //    }
    //}
    //
    // 총계 OFR 영역 시각화 테스트
    //public void Test_Draw총계영역()
    //{
    //    if (mRcpt.DG오더_hWndTop == IntPtr.Zero)
    //    {
    //        System.Windows.MessageBox.Show("DG오더_hWndTop가 초기화되지 않았습니다.", "오류");
    //        return;
    //    }
    //
    //    Draw.Rectangle rcTotalS = fInfo.접수등록Page_DG오더Small_rcTotalS;
    //
    //    TransparantWnd.CreateOverlay(mRcpt.DG오더_hWndTop);
    //    TransparantWnd.DrawBoxAsync(rcTotalS, Media.Colors.Red, 1);
    //
    //    System.Windows.MessageBox.Show($"총계 영역: {rcTotalS}", "총계 영역 테스트");
    //
    //    TransparantWnd.DeleteOverlay();
    //}
    //
    // 총계 OFR 영역 시각화 테스트 (Large/확장 상태)
    //public void Test_Draw총계영역Large()
    //{
    //    if (mRcpt.DG오더_hWndTop == IntPtr.Zero)
    //    {
    //        System.Windows.MessageBox.Show("DG오더_hWndTop가 초기화되지 않았습니다.", "오류");
    //        return;
    //    }
    //
    //    Draw.Rectangle rcTotalL = fInfo.접수등록Page_DG오더Large_rcTotalS;
    //
    //    TransparantWnd.CreateOverlay(mRcpt.DG오더_hWndTop);
    //    TransparantWnd.DrawBoxAsync(rcTotalL, Media.Colors.Red, 1);
    //
    //    System.Windows.MessageBox.Show($"총계 영역 (Large): {rcTotalL}", "총계 영역 테스트");
    //
    //    TransparantWnd.DeleteOverlay();
    //}
    //
    // 컬럼헤더 셀영역 시각화 테스트
    //public async System.Threading.Tasks.Task Test_DrawColumnHeaderRectsAsync()
    //{
    //    try
    //    {
    //        Debug.WriteLine($"[{AppName}] Test_DrawColumnHeaderRectsAsync 시작");
    //
    //        if (mRcpt.DG오더_hWndTop == IntPtr.Zero)
    //        {
    //            System.Windows.MessageBox.Show("DG오더_hWnd가 초기화되지 않았습니다.", "오류");
    //            return;
    //        }
    //
    //        // 1. DG 헤더 캡처
    //        Draw.Rectangle rcDG_Abs = Std32Window.GetWindowRect_DrawAbs(mRcpt.DG오더_hWndTop);
    //        int headerHeight = fInfo.접수등록Page_DG오더_headerHeight;
    //        Draw.Rectangle rcHeader = new Draw.Rectangle(0, 0, rcDG_Abs.Width, headerHeight);
    //        Draw.Bitmap bmpDG = OfrService.CaptureScreenRect_InWndHandle(mRcpt.DG오더_hWndTop, rcHeader);
    //        if (bmpDG == null)
    //        {
    //            System.Windows.MessageBox.Show("DG 헤더 캡처 실패", "오류");
    //            return;
    //        }
    //
    //        // 2. 컬럼 경계 검출 (현재 로직)
    //        const int headerGab = 6;
    //        int textHeight = headerHeight - (headerGab * 2);
    //        int targetRow = headerGab + textHeight;
    //
    //        byte minBrightness = OfrService.GetMinBrightnessAtRow_FromColorBitmapFast(bmpDG, targetRow);
    //        minBrightness += 2;
    //
    //        bool[] boolArr = OfrService.GetBoolArray_FromColorBitmapRowFast(bmpDG, targetRow, minBrightness, 2);
    //        List<OfrModel_LeftWidth> listLW = OfrService.GetLeftWidthList_FromBool1Array(boolArr, minBrightness);
    //
    //        if (listLW == null || listLW.Count < 2)
    //        {
    //            bmpDG?.Dispose();
    //            System.Windows.MessageBox.Show($"컬럼 경계 검출 실패: Count={listLW?.Count ?? 0}", "오류");
    //            return;
    //        }
    //
    //        // 마지막 항목 제거
    //        listLW.RemoveAt(listLW.Count - 1);
    //        int columns = listLW.Count;
    //
    //        Debug.WriteLine($"[{AppName}] 컬럼 검출: {columns}개, minBrightness={minBrightness}, targetRow={targetRow}");
    //
    //        // 3. TransparantWnd로 컬럼 헤더 영역 그리기
    //        TransparantWnd.CreateOverlay(mRcpt.DG오더_hWndTop);
    //        TransparantWnd.ClearBoxes();
    //
    //        for (int i = 0; i < columns; i++)// 컬럼헤더의 배경이 경계명도가 달라서 좌, 우로 1줄임 - 어두운 명도를 기준으로 하면 안줄여도 될걸로 예상
    //        {
    //            Draw.Rectangle rc = new Draw.Rectangle(listLW[i].nLeft + 1, headerGab, listLW[i].nWidth - 2, textHeight);
    //            TransparantWnd.DrawBoxAsync(rc, strokeColor: Media.Colors.Red, thickness: 1);
    //        }
    //
    //        bmpDG?.Dispose();
    //
    //        // 4. MsgBox
    //        System.Windows.MessageBox.Show(
    //            $"원콜 컬럼헤더 셀영역 테스트\n\n" +
    //            $"컬럼 수: {columns}\n" +
    //            $"headerHeight: {headerHeight}\n" +
    //            $"targetRow: {targetRow}\n" +
    //            $"minBrightness: {minBrightness}\n\n" +
    //            $"확인을 누르면 오버레이가 제거됩니다.",
    //            "컬럼헤더 테스트");
    //
    //        TransparantWnd.DeleteOverlay();
    //        Debug.WriteLine($"[{AppName}] Test_DrawColumnHeaderRectsAsync 완료");
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.WriteLine($"[{AppName}] 예외 발생: {ex.Message}");
    //        System.Windows.MessageBox.Show($"테스트 중 오류 발생:\n{ex.Message}", "오류");
    //        TransparantWnd.DeleteOverlay();
    //    }
    //}

    // 상차방법 체크박스 영역 시각화 테스트
    //public void Test_Draw상차방법Rects()
    //{
    //    try
    //    {
    //        if (mRcpt.접수섹션_hWndTop == IntPtr.Zero) return;
    //
    //        TransparantWnd.CreateOverlay(mRcpt.접수섹션_hWndTop);
    //        TransparantWnd.ClearBoxes();
    //
    //        TransparantWnd.DrawBoxAsync(fInfo.접수등록Page_상차방법_지게차Part_rcChkRelM, strokeColor: Media.Colors.Red, thickness: 1);
    //        TransparantWnd.DrawBoxAsync(fInfo.접수등록Page_상차방법_호이스트Part_rcChkRelM, strokeColor: Media.Colors.Red, thickness: 1);
    //        TransparantWnd.DrawBoxAsync(fInfo.접수등록Page_상차방법_수해줌Part_rcChkRelM, strokeColor: Media.Colors.Red, thickness: 1);
    //        TransparantWnd.DrawBoxAsync(fInfo.접수등록Page_상차방법_수작업Part_rcChkRelM, strokeColor: Media.Colors.Red, thickness: 1);
    //        TransparantWnd.DrawBoxAsync(fInfo.접수등록Page_상차방법_크레인Part_rcChkRelM, strokeColor: Media.Colors.Red, thickness: 1);
    //
    //        System.Windows.MessageBox.Show("상차방법 체크박스 영역 테스트\n확인 후 오버레이 제거됨", "테스트");
    //
    //        TransparantWnd.DeleteOverlay();
    //    }
    //    catch (Exception ex)
    //    {
    //        System.Windows.MessageBox.Show($"오류: {ex.Message}", "오류");
    //        TransparantWnd.DeleteOverlay();
    //    }
    //}
    //
    // 상차일시 체크박스 영역 시각화 테스트
    //public void Test_Draw상차일시Rects()
    //{
    //    try
    //    {
    //        if (mRcpt.접수섹션_hWndTop == IntPtr.Zero) return;
    //
    //        TransparantWnd.CreateOverlay(mRcpt.접수섹션_hWndTop);
    //        TransparantWnd.ClearBoxes();
    //
    //        TransparantWnd.DrawBoxAsync(fInfo.접수등록Page_상차일시_당상Part_rcChkRelM, strokeColor: Media.Colors.Red, thickness: 1);
    //        TransparantWnd.DrawBoxAsync(fInfo.접수등록Page_상차일시_낼상Part_rcChkRelM, strokeColor: Media.Colors.Red, thickness: 1);
    //        TransparantWnd.DrawBoxAsync(fInfo.접수등록Page_상차일시_월상Part_rcChkRelM, strokeColor: Media.Colors.Red, thickness: 1);
    //
    //        System.Windows.MessageBox.Show("상차일시 체크박스 영역 테스트\n확인 후 오버레이 제거됨", "테스트");
    //
    //        TransparantWnd.DeleteOverlay();
    //    }
    //    catch (Exception ex)
    //    {
    //        System.Windows.MessageBox.Show($"오류: {ex.Message}", "오류");
    //        TransparantWnd.DeleteOverlay();
    //    }
    //}
    //
    // 하차방법/하차일시 체크박스 영역 시각화 테스트 (9개)
    //public void Test_Draw하차Rects()
    //{
    //    try
    //    {
    //        if (mRcpt.접수섹션_hWndTop == IntPtr.Zero) return;
    //
    //        TransparantWnd.CreateOverlay(mRcpt.접수섹션_hWndTop);
    //        TransparantWnd.ClearBoxes();
    //
    //        // 하차방법 5개
    //        TransparantWnd.DrawBoxAsync(fInfo.접수등록Page_하차방법_지게차Part_rcChkRelM, strokeColor: Media.Colors.Red, thickness: 1);
    //        TransparantWnd.DrawBoxAsync(fInfo.접수등록Page_하차방법_호이스트Part_rcChkRelM, strokeColor: Media.Colors.Red, thickness: 1);
    //        TransparantWnd.DrawBoxAsync(fInfo.접수등록Page_하차방법_수해줌Part_rcChkRelM, strokeColor: Media.Colors.Red, thickness: 1);
    //        TransparantWnd.DrawBoxAsync(fInfo.접수등록Page_하차방법_수작업Part_rcChkRelM, strokeColor: Media.Colors.Red, thickness: 1);
    //        TransparantWnd.DrawBoxAsync(fInfo.접수등록Page_하차방법_크레인Part_rcChkRelM, strokeColor: Media.Colors.Red, thickness: 1);
    //
    //        // 하차일시 4개
    //        TransparantWnd.DrawBoxAsync(fInfo.접수등록Page_하차일시_당착Part_rcChkRelM, strokeColor: Media.Colors.Red, thickness: 1);
    //        TransparantWnd.DrawBoxAsync(fInfo.접수등록Page_하차일시_낼착Part_rcChkRelM, strokeColor: Media.Colors.Red, thickness: 1);
    //        TransparantWnd.DrawBoxAsync(fInfo.접수등록Page_하차일시_월착Part_rcChkRelM, strokeColor: Media.Colors.Red, thickness: 1);
    //        TransparantWnd.DrawBoxAsync(fInfo.접수등록Page_하차일시_당_내착Part_rcChkRelM, strokeColor: Media.Colors.Red, thickness: 1);
    //
    //        System.Windows.MessageBox.Show("하차방법/하차일시 영역 테스트 (9개)\n확인 후 오버레이 제거됨", "테스트");
    //
    //        TransparantWnd.DeleteOverlay();
    //    }
    //    catch (Exception ex)
    //    {
    //        System.Windows.MessageBox.Show($"오류: {ex.Message}", "오류");
    //        TransparantWnd.DeleteOverlay();
    //    }
    //}
    //
    // 자동조회 콤보박스 영역 테스트
    //public void Test_Draw자동조회Rect()
    //{
    //    if (mRcpt.검색섹션_hWndTop == IntPtr.Zero)
    //    {
    //        System.Windows.MessageBox.Show("검색섹션_hWndTop이 초기화되지 않았습니다.", "오류");
    //        return;
    //    }
    //
    //    TransparantWnd.CreateOverlay(mRcpt.검색섹션_hWndTop);
    //    TransparantWnd.DrawBoxAsync(fInfo.접수등록Page_검색_자동조회_rcChkRelM, Media.Colors.Red, 1);
    //
    //    System.Windows.MessageBox.Show($"자동조회 영역: {fInfo.접수등록Page_검색_자동조회_rcChkRelM}", "자동조회 영역 테스트");
    //
    //    TransparantWnd.DeleteOverlay();
    //}
    //
    // 새로고침버튼 테두리 명도 테스트 (점선 패턴 위치)
    //public void Test_새로고침버튼_테두리명도()
    //{
    //    if (mRcpt.검색섹션_hWnd새로고침버튼 == IntPtr.Zero)
    //    {
    //        System.Windows.MessageBox.Show("검색섹션_hWnd새로고침버튼이 초기화되지 않았습니다.", "오류");
    //        return;
    //    }
    //
    //    System.Windows.MessageBox.Show("확인 후 명도 측정", "대기");
    //
    //    var sb = new System.Text.StringBuilder();
    //    for (int y = 1; y <= 4; y++)
    //    {
    //        for (int x = 1; x <= 5; x++)
    //        {
    //            int b = OfrService.GetPixelBrightnessFrmWndHandle(mRcpt.검색섹션_hWnd새로고침버튼, new Draw.Point(x, y));
    //            sb.Append($"{b}\t");
    //        }
    //        sb.AppendLine();
    //    }
    //
    //    System.Windows.MessageBox.Show(sb.ToString(), "테두리 명도");
    //}
    //
    // 의뢰자 상호 영역 시각화 테스트
    //public void Test_Draw의뢰자Rects()
    //{
    //    try
    //    {
    //        if (mRcpt.접수섹션_hWndTop == IntPtr.Zero) return;
    //
    //        TransparantWnd.CreateOverlay(mRcpt.접수섹션_hWndTop);
    //        TransparantWnd.ClearBoxes();
    //
    //        TransparantWnd.DrawBoxAsync(fInfo.접수등록Page_의뢰자_상호_rcChkRelM, strokeColor: Media.Colors.Red, thickness: 1);
    //
    //        System.Windows.MessageBox.Show("의뢰자 상호 영역 테스트\n확인 후 오버레이 제거됨", "테스트");
    //
    //        TransparantWnd.DeleteOverlay();
    //    }
    //    catch (Exception ex)
    //    {
    //        System.Windows.MessageBox.Show($"오류: {ex.Message}", "오류");
    //        TransparantWnd.DeleteOverlay();
    //    }
    //}

    #endregion
}
#nullable restore
