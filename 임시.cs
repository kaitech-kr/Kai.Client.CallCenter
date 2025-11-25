using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

private async Task<StdResult_Error> SetDG오더RectsAsync(bool bEdit = true, bool bWrite = true, bool bMsgBox = true)
{
    Draw.Bitmap bmpDG = null;

    try
    {
        Debug.WriteLine($"[InsungsAct_RcptRegPage] SetDG오더RectsAsync 시작");

        // 재시도 루프
        for (int retry = 1; retry <= c_nRepeatShort; retry++)
        {
            bool bShowMsgBox = (retry == c_nRepeatShort) && bMsgBox;

            if (retry > 1)
            {
                Debug.WriteLine($"[InsungsAct_RcptRegPage] Datagrid 재시도 {retry}/{c_nRepeatShort}");
                await Task.Delay(c_nWaitVeryLong);
            }

            // 1. DG오더_hWnd 기준으로 헤더 영역만 캡처 (원콜 방식)
            Draw.Rectangle rcDG_Abs = Std32Window.GetWindowRect_DrawAbs(m_RcptPage.DG오더_hWnd);
            int headerHeight = m_FileInfo.접수등록Page_DG오더_headerHeight;
            Draw.Rectangle rcHeader = new Draw.Rectangle(0, 0, rcDG_Abs.Width, headerHeight);

            bmpDG = OfrService.CaptureScreenRect_InWndHandle(m_RcptPage.DG오더_hWnd, rcHeader);

            if (bmpDG == null)
            {
                if (retry != c_nRepeatShort)
                {
                    Debug.WriteLine($"[InsungsAct_RcptRegPage] DG오더 캡처 실패 (재시도 {retry}/{c_nRepeatShort})");
                    await Task.Delay(200);
                    continue;
                }
                return CommonFuncs_StdResult.ErrMsgResult_Error(
                    $"[{m_Context.AppName}/RcptRegPage]DG오더 캡처 실패",
                    "InsungsAct_RcptRegPage/SetDG오더RectsAsync_01", bWrite, bShowMsgBox);
            }

            Debug.WriteLine($"[InsungsAct_RcptRegPage] DG오더 캡처 성공: {bmpDG.Width}x{bmpDG.Height}");

            // 2. 컬럼 경계 검출 (원콜 방식: targetRow = headerGab + textHeight)
            const int headerGab = 7;
            int textHeight = headerHeight - (headerGab * 2);
            int targetRow = headerGab + textHeight;

            byte minBrightness = OfrService.GetMinBrightnessAtRow_FromColorBitmapFast(bmpDG, targetRow);

            if (minBrightness == 255)
            {
                bmpDG?.Dispose();
                if (retry != c_nRepeatShort)
                {
                    Debug.WriteLine($"[InsungsAct_RcptRegPage] 최소 밝기 검출 실패 (재시도 {retry}/{c_nRepeatShort})");
                    await Task.Delay(200);
                    continue;
                }
                return CommonFuncs_StdResult.ErrMsgResult_Error(
                    $"[{m_Context.AppName}/RcptRegPage]최소 밝기 검출 실패",
                    "InsungsAct_RcptRegPage/SetDG오더RectsAsync_02", bWrite, bShowMsgBox);
            }

            minBrightness += 2;

            // 2-2. Bool 배열 생성
            bool[] boolArr = OfrService.GetBoolArray_FromColorBitmapRowFast(bmpDG, targetRow, minBrightness, 2);

            if (boolArr == null || boolArr.Length == 0)
            {
                bmpDG?.Dispose();
                if (retry != c_nRepeatShort)
                {
                    Debug.WriteLine($"[InsungsAct_RcptRegPage] Bool 배열 생성 실패 (재시도 {retry}/{c_nRepeatShort})");
                    await Task.Delay(200);
                    continue;
                }
                return CommonFuncs_StdResult.ErrMsgResult_Error(
                    $"[{m_Context.AppName}/RcptRegPage]Bool 배열 생성 실패",
                    "InsungsAct_RcptRegPage/SetDG오더RectsAsync_03", bWrite, bShowMsgBox);
            }

            // 2-3. 컬럼 경계 리스트 추출
            List<OfrModel_LeftWidth> listLW = OfrService.GetLeftWidthList_FromBool1Array(boolArr, minBrightness);

            if (listLW == null || listLW.Count < 2)
            {
                bmpDG?.Dispose();
                if (retry != c_nRepeatShort)
                {
                    Debug.WriteLine($"[InsungsAct_RcptRegPage] 컬럼 경계 검출 실패 (재시도 {retry}/{c_nRepeatShort})");
                    await Task.Delay(200);
                    continue;
                }
                return CommonFuncs_StdResult.ErrMsgResult_Error(
                    $"[{m_Context.AppName}/RcptRegPage]컬럼 경계 검출 실패: Count={listLW?.Count ?? 0}",
                    "InsungsAct_RcptRegPage/SetDG오더RectsAsync_04", bWrite, bShowMsgBox);
            }

            // 마지막 항목 제거 (오른쪽 끝 경계)
            listLW.RemoveAt(listLW.Count - 1);

            int columns = listLW.Count;
            Debug.WriteLine($"[InsungsAct_RcptRegPage] 컬럼 검출: {columns}개 (목표: {m_ReceiptDgHeaderInfos.Length}개)");

            // 컬럼 개수 확인
            if (columns != m_ReceiptDgHeaderInfos.Length)
            {
                Debug.WriteLine($"[InsungsAct_RcptRegPage] 컬럼 개수 불일치: 검출={columns}개, 예상={m_ReceiptDgHeaderInfos.Length}개 (재시도 {retry}/{c_nRepeatShort})");

                bmpDG?.Dispose();

                StdResult_Error initResult = await InitDG오더Async(
                    CEnum_DgValidationIssue.InvalidColumnCount,
                    bEdit, bWrite, bMsgBox: false);

                if (initResult != null)
                {
                    if (retry == c_nRepeatShort)
                    {
                        return CommonFuncs_StdResult.ErrMsgResult_Error(
                            $"[{m_Context.AppName}/RcptRegPage]컬럼 개수 불일치: 검출={columns}개, 예상={m_ReceiptDgHeaderInfos.Length}개 (재시도 {c_nRepeatShort}회 초과)",
                            "InsungsAct_RcptRegPage/SetDG오더RectsAsync_05", bWrite, bShowMsgBox);
                    }
                }

                await Task.Delay(200);
                continue;
            }

            // 3. 컬럼 헤더 OFR (원콜 방식: OfrStr_ComplexCharSetAsync 사용)
            m_RcptPage.DG오더_ColumnTexts = new string[columns];

            for (int i = 0; i < columns; i++)
            {
                Draw.Rectangle rcColHeader = new Draw.Rectangle(
                    listLW[i].nLeft, headerGab, listLW[i].nWidth, textHeight);

                var result = await OfrWork_Common.OfrStr_ComplexCharSetAsync(
                    bmpDG, rcColHeader, bInvertRgb: false, bEdit: false);

                m_RcptPage.DG오더_ColumnTexts[i] = result?.strResult ?? string.Empty;
            }

            // 4. Datagrid 상태 검증
            Debug.WriteLine($"[InsungsAct_RcptRegPage] Datagrid 상태 검증 시작");
            CEnum_DgValidationIssue validationIssues = ValidateDatagridState(m_RcptPage.DG오더_ColumnTexts, listLW);

            if (validationIssues != CEnum_DgValidationIssue.None)
            {
                Debug.WriteLine($"[InsungsAct_RcptRegPage] Datagrid 상태 검증 실패: {validationIssues} (재시도 {retry}/{c_nRepeatShort})");

                bmpDG?.Dispose();

                StdResult_Error initResult = await InitDG오더Async(validationIssues, bEdit, bWrite, bMsgBox: false);

                if (initResult != null)
                {
                    if (retry == c_nRepeatShort)
                    {
                        return CommonFuncs_StdResult.ErrMsgResult_Error(
                            $"[{m_Context.AppName}/RcptRegPage]Datagrid 상태 검증 실패: {validationIssues} (재시도 {c_nRepeatShort}회 초과)",
                            "InsungsAct_RcptRegPage/SetDG오더RectsAsync_Validation", bWrite, bShowMsgBox);
                    }
                }

                await Task.Delay(200);
                continue;
            }

            // 5. RelChildRects 생성
            Debug.WriteLine($"[InsungsAct_RcptRegPage] 모든 컬럼 검증 완료!");

            bmpDG?.Dispose();
            bmpDG = null;

            int rows = InsungsInfo_File.접수등록Page_DG오더_dataRowCount;
            int rowHeight = m_FileInfo.접수등록Page_DG오더_dataRowHeight;

            m_RcptPage.DG오더_RelChildRects = new Draw.Rectangle[columns, rows];

            for (int col = 0; col < columns; col++)
            {
                for (int row = 0; row < rows; row++)
                {
                    int cellY = headerHeight + (row * rowHeight);

                    m_RcptPage.DG오더_RelChildRects[col, row] = new Draw.Rectangle(
                        listLW[col].nLeft + 1,
                        cellY,
                        listLW[col].nWidth - 2,
                        rowHeight
                    );
                }
            }

            Debug.WriteLine($"[InsungsAct_RcptRegPage] RelChildRects 생성 완료: {columns}열 x {rows}행");

            // 6. Background Brightness 계산
            if (rows >= 2)
            {
                // Empty Row 캡처를 위해 다시 DG 캡처
                Draw.Rectangle rcFullDG = new Draw.Rectangle(0, 0, rcDG_Abs.Width, rcDG_Abs.Height);
                bmpDG = OfrService.CaptureScreenRect_InWndHandle(m_RcptPage.DG오더_hWnd, rcFullDG);

                if (bmpDG != null)
                {
                    Draw.Point ptSampleRel = StdUtil.GetDrawPoint(m_RcptPage.DG오더_RelChildRects[0, 1], 8, 8);
                    m_RcptPage.DG오더_nBackgroundBright = OfrService.GetPixelBrightness(bmpDG, ptSampleRel);
                    Debug.WriteLine($"[InsungsAct_RcptRegPage] Background Brightness: {m_RcptPage.DG오더_nBackgroundBright}");
                }
            }

            Debug.WriteLine($"[InsungsAct_RcptRegPage] SetDG오더RectsAsync 완료");
            break;
        }

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
        bmpDG?.Dispose();
    }
}