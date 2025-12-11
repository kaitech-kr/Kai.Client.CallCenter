using System.Diagnostics;
using Draw = System.Drawing;

using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;
using Kai.Common.NetDll_WpfCtrl.NetOFR;
using Kai.Client.CallCenter.OfrWorks;

using Kai.Client.CallCenter.Classes;
using Kai.Client.CallCenter.Pages;
using Kai.Client.CallCenter.Classes.Class_Master;
using static Kai.Client.CallCenter.Classes.CommonVars;

namespace Kai.Client.CallCenter.Networks.NwOnecalls;
#nullable disable

public partial class OnecallAct_RcptRegPage
{
    #region Helper Methods
    /// <summary>
    /// Onecall SeqNo 가져오기
    /// </summary>
    private string GetOnecallSeqno(AutoAllocModel item) => item.NewOrder.Onecall;

    /// <summary>
    /// 포커스 탈출 (빈 영역 클릭)
    /// </summary>
    private async Task EscapeFocusAsync(CancellationToken ct = default, int nDelay = c_nWaitVeryShort)
    {
        await Std32Mouse_Post.MousePostAsync_ClickLeft(mRcpt.검색섹션_hWnd포커스탈출);
        await Task.Delay(nDelay, ct);
    }
    #endregion

    #region DG오더 확장/축소 상태 관리
    /// <summary>
    /// 데이터그리드 확장 상태 확인 (높이 기준)
    /// </summary>
    public bool IsDG오더Expanded()
    {
        Draw.Rectangle rc = Std32Window.GetWindowRect_DrawAbs(mRcpt.DG오더_hWndTop);
        bool bExpanded = rc.Height >= fInfo.접수등록Page_DG오더_nExpandedHeight;
        Debug.WriteLine($"[{AppName}] IsDG오더Expanded: Height={rc.Height}, Threshold={fInfo.접수등록Page_DG오더_nExpandedHeight}, Result={bExpanded}");
        return bExpanded;
    }

    /// <summary>
    /// 데이터그리드 확장 (축소 상태일 때만)
    /// </summary>
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
        for (int i = 0; i < c_nRepeatNormal; i++)
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

    /// <summary>
    /// 데이터그리드 축소 (확장 상태일 때만)
    /// </summary>
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
        for (int i = 0; i < c_nRepeatNormal; i++)
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

    #region 주소 입력 공용함수
    /// <summary>
    /// 상세주소 입력 공용함수 (상차지/하차지)
    /// </summary>
    private async Task<StdResult_Status> Set상세주소Async(IntPtr hWnd주소, Draw.Rectangle rc권역, string detailAddr, CancelTokenControl ctrl)
    {
        Draw.Bitmap bmpCheck = null;
        try
        {
            // 1. 상세주소에서 숫자 전 텍스트 분할
            var match = System.Text.RegularExpressions.Regex.Match(detailAddr, @"^(.*?)(\d.*)$");
            string captionSimple = match.Success ? match.Groups[1].Value : detailAddr;

            IntPtr hFind = IntPtr.Zero;

            for (int repeat = 1; repeat <= c_nRepeatShort; repeat++)
            {
                // 2. 숫자제외 텍스트 입력 + 엔터
                Std32Window.SetWindowCaption(hWnd주소, captionSimple);
                await Task.Delay(c_nWaitVeryShort);
                Std32Key_Msg.KeyPost_Click(hWnd주소, StdCommon32.VK_RETURN);
                await Task.Delay(c_nWaitVeryShort);

                // 3. 리스트박스 뜰때까지 대기 + 엔터로 선택
                for (int i = 0; i < c_nRepeatMany; i++)
                {
                    await Task.Delay(c_nWaitShort);
                    hFind = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, fInfo.접수등록Page_접수_화물정보_ptChkRelM);
                    if (hFind != mRcpt.접수섹션_hWnd화물정보)
                    {
                        await Task.Delay(c_nWaitShort);
                        Std32Key_Msg.KeyPost_Click(hFind, StdCommon32.VK_RETURN);
                        await Task.Delay(c_nWaitShort);
                        break;
                    }
                }
                if (hFind == mRcpt.접수섹션_hWnd화물정보) return new StdResult_Status(StdResult.Fail, "리스트박스 못찾음");

                // 4. 리스트박스 사라질때까지 대기 + 전체 상세주소 입력
                for (int i = 0; i < c_nRepeatMany; i++)
                {
                    await Task.Delay(c_nWaitShort);
                    hFind = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, fInfo.접수등록Page_접수_화물정보_ptChkRelM);
                    if (hFind == mRcpt.접수섹션_hWnd화물정보)
                    {
                        await Task.Delay(c_nWaitShort);
                        Std32Window.SetWindowCaption(hWnd주소, detailAddr);
                        await Task.Delay(c_nWaitNormal);
                        break;
                    }
                }

                // 5. 권역 OFR로 검증
                StdResult_String resultStr = null;
                for (int i = 1; i <= c_nRepeatShort; i++)
                {
                    await Task.Delay(c_nWaitNormal);
                    bmpCheck = OfrService.CaptureScreenRect_InWndHandle(mRcpt.접수섹션_hWndTop, rc권역);
                    if (bmpCheck == null) continue;

                    resultStr = await OfrWork_Common.OfrStr_ComplexCharSetAsync(bmpCheck, i == c_nRepeatShort);
                    if (!string.IsNullOrEmpty(resultStr.sErr)) continue;

                    if (resultStr.strResult.Length > 1) break;
                }

                if (resultStr != null && resultStr.strResult.Length > 1) return new StdResult_Status(StdResult.Success);
            }

            return new StdResult_Status(StdResult.Fail, "권역 읽기실패");
        }
        finally
        {
            bmpCheck?.Dispose();
        }
    }
    #endregion

    #region 콤보박스 공용함수
    /// <summary>
    /// 콤보박스 항목 선택 공용함수 (OFR 검증 포함)
    /// </summary>
    /// <param name="hWndComboBox">콤보박스 핸들</param>
    /// <param name="model">선택할 항목 정보 (ptPos, sYourName)</param>
    /// <param name="hWndTop">검증용 캡처 기준 핸들</param>
    /// <param name="rcVerifyRelS">검증용 캡처 영역 (hWndTop 기준 상대좌표)</param>
    /// <returns>성공/실패</returns>
    private async Task<StdResult_Status> SelectComboBoxItemAsync(IntPtr hWndComboBox, CommonModel_ComboBox model, IntPtr hWndTop, Draw.Rectangle rcVerifyRelS)
    {
        for (int i = 1; i <= c_nRepeatShort; i++)
        {
            // 1. 콤보박스 아래 위치에서 핸들 백업
            Draw.Point ptCheck = StdUtil.GetAbsDrawPoint_BottomBelow(hWndComboBox);
            IntPtr hWndBefore = Std32Window.GetWndHandle_FromAbsDrawPt(ptCheck);
            IntPtr hWndDropdown = IntPtr.Zero;

            // 2. 콤보박스 클릭 (드롭다운 열기)
            await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndComboBox);

            // 3. 드롭다운 열릴 때까지 대기
            for (int j = 0; j < c_nRepeatMany; j++)
            {
                await Task.Delay(c_nWaitShort);
                hWndDropdown = Std32Window.GetWndHandle_FromAbsDrawPt(ptCheck);
                if (hWndDropdown != hWndBefore) break;
            }
            if (hWndDropdown == hWndBefore) continue; // 재시도

            // 4. 항목 클릭
            await Std32Mouse_Post.MousePostAsync_ClickLeft_ptRel(hWndDropdown, model.ptPos);

            // 5. 드롭다운 닫힐 때까지 대기
            for (int j = 0; j < c_nRepeatMany; j++)
            {
                await Task.Delay(c_nWaitShort);
                IntPtr hWndCurrent = Std32Window.GetWndHandle_FromAbsDrawPt(ptCheck);
                if (hWndCurrent != hWndDropdown) break;
            }

            // 6. OFR 검증
            await Task.Delay(c_nWaitShort);
            using (Draw.Bitmap bmpVerify = OfrService.CaptureScreenRect_InWndHandle(hWndTop, rcVerifyRelS))
            {
                if (bmpVerify == null) continue; // 재시도

                var ofrResult = await OfrWork_Common.OfrStr_ComplexCharSetAsync(bmpVerify, true, i == c_nRepeatShort);

                if (ofrResult == null || string.IsNullOrEmpty(ofrResult.strResult)) continue; // 재시도

                if (ofrResult.strResult != model.sYourName) continue; // 재시도

                Debug.WriteLine($"[{AppName}] SelectComboBoxItemAsync 성공: {model.sYourName}");
                return new StdResult_Status(StdResult.Success);
            }
        }

        return new StdResult_Status(StdResult.Fail, $"콤보박스 선택 실패: {model.sYourName}");
    }
    #endregion

    #region 체크박스 공용함수
    /// <summary>
    /// 체크박스 상태 설정 (OFR 기반)
    /// </summary>
    /// <param name="hWndCheckBox">체크박스 핸들 (클릭용)</param>
    /// <param name="rcOfrRelS">OFR 영역 (접수섹션Top 기준)</param>
    /// <param name="bWantChecked">원하는 상태 (true=Checked)</param>
    /// <param name="name">체크박스 이름 (로그용)</param>
    private async Task<StdResult_Status> SetCheckBoxAsync(IntPtr hWndCheckBox, Draw.Rectangle rcOfrRelS, bool bWantChecked, string name)
    {
        for (int i = 1; i <= c_nRepeatShort; i++)
        {
            await Task.Delay(c_nWaitNormal);

            // 1. 현재 상태 읽기
            var resultChk = await OfrWork_Insungs.OfrImgReChkValue_RectInHWndAsync(mRcpt.접수섹션_hWndTop, rcOfrRelS, i == c_nRepeatShort);
            if (resultChk.bResult == null)
            {
                if (i < c_nRepeatShort) continue;
                else return new StdResult_Status(StdResult.Fail, $"{name} 인식 실패");
            }

            // 2. 이미 원하는 상태면 성공
            if (resultChk.bResult == bWantChecked)
            {
                Debug.WriteLine($"[{AppName}] SetCheckBoxAsync: {name} 이미 {(bWantChecked ? "Checked" : "Unchecked")}");
                return new StdResult_Status(StdResult.Success);
            }

            await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndCheckBox);
            for (int j = 1; j <= c_nRepeatShort; j++)
            {
                await Task.Delay(c_nWaitNormal);

                // 상태 변경 확인
                resultChk = await OfrWork_Insungs.OfrImgUntilChkValue_RectInHWndAsync(mRcpt.접수섹션_hWndTop, bWantChecked, rcOfrRelS);
                if (resultChk.bResult == true)
                {
                    Debug.WriteLine($"[{AppName}] SetCheckBoxAsync: {name} → {(bWantChecked ? "Checked" : "Unchecked")} 성공");
                    return new StdResult_Status(StdResult.Success);
                }
            }
        }

        return new StdResult_Status(StdResult.Fail, $"{name} 상태 변경 실패");
    }
    #endregion

    #region 접수/수정 창 관련함수들
    private CommonModel_ComboBox GetCarWeightResult(string sCarType, string sCarWeight)
    {
        string my톤수 = Order_StatusPage.GetCarWeightString(sCarType, sCarWeight);
        if (string.IsNullOrEmpty(my톤수)) return fInfo.접수등록Page_접수_톤수Open[0];

        foreach (var item in fInfo.접수등록Page_접수_톤수Open)
        {
            if (item.sMyName == my톤수) return item;
        }

        return fInfo.접수등록Page_접수_톤수Open[0];
    }
    private string GetMaxCarWeight(CommonModel_ComboBox c)
    {
        switch (c.sMyName)
        {
            case ""    : return "0.00";
            case "다마": return "0.30";
            case "라보": return "0.50";
            case "1t"  : return "1.00";
            case "1.4t": return "1.40";
            case "2.5t": return "2.50";
            case "3.5t": return "3.50";
            case "5t"  : return "5.00";
            case "8t"  : return "8.00";
            case "9.5t": return "9.50";
            case "11t": return "11.00";
            case "14t": return "14.00";
            case "15t": return "15.00";
            case "18t": return "18.00";
            case "22t": return "22.00";
            case "25t": return "25.00";

            default: return "0.00";
        }
    }

    private CommonModel_ComboBox GetTruckDetailResult(string sCarType, string sCarWeight)
    {
        string myDetail = Order_StatusPage.GetTruckDetailString(sCarType, sCarWeight);
        if (string.IsNullOrEmpty(myDetail)) return fInfo.접수등록Page_접수_차종Open[0];

        foreach (var item in fInfo.접수등록Page_접수_차종Open)
        {
            if (item.sMyName == myDetail) return item;
        }

        return fInfo.접수등록Page_접수_차종Open[0];
    }

    private CommonModel_ComboBox GetFeeTypeResult(string sFeeType)
    {
        if (string.IsNullOrEmpty(sFeeType)) return fInfo.접수등록Page_접수_결재Open[4];

        foreach (var item in fInfo.접수등록Page_접수_결재Open)
        {
            if (item.sMyName == sFeeType) return item;
        }

        return fInfo.접수등록Page_접수_결재Open[4];
    }

    private CommonModel_ComboBox GetAutoRefreshResult(string sTime)
    {
        if (string.IsNullOrEmpty(sTime)) return fInfo.접수등록Page_검색_자동조회Open[0];

        foreach (var item in fInfo.접수등록Page_검색_자동조회Open)
        {
            if (item.sMyName == sTime) return item;
        }

        return fInfo.접수등록Page_검색_자동조회Open[0];
    }
    #endregion

    #region 오더번호 OFR
    /// <summary>
    /// 지정된 로우의 오더번호 OFR
    /// - 셀 캡처 후 단음소 OFR
    /// - 원콜은 RGB 반전 불필요
    /// </summary>
    private async Task<StdResult_String> Get오더번호Async(int rowIndex, CancelTokenControl ctrl, int retryCount = c_nRepeatShort)
    {
        try
        {
            const int COL_오더번호 = 2;

            Draw.Rectangle rect오더번호Cell = mRcpt.DG오더_rcRelSmallCells[COL_오더번호, rowIndex];
            Debug.WriteLine($"[{AppName}] 오더번호 OFR - rowIndex={rowIndex}, 셀위치={rect오더번호Cell}");

            for (int i = 1; i <= retryCount; i++)
            {
                await ctrl.WaitIfPausedOrCancelledAsync();
                Debug.WriteLine($"[{AppName}] ===== 오더번호 OFR 시도 {i}/{retryCount} =====");

                Draw.Bitmap bmpCell = OfrService.CaptureScreenRect_InWndHandle(mRcpt.DG오더_hWndTop, rect오더번호Cell);
                if (bmpCell == null)
                {
                    Debug.WriteLine($"[{AppName}] 오더번호 셀 캡처 실패 (시도 {i}/{retryCount})");
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
                        Debug.WriteLine($"[{AppName}] 오더번호 획득 성공: '{resultSeqno.strResult}' (시도 {i}/{retryCount})");
                        return new StdResult_String(resultSeqno.strResult);
                    }
                    else
                    {
                        Debug.WriteLine($"[{AppName}] OFR 실패: '{resultSeqno.strResult ?? resultSeqno.sErr}' (시도 {i}/{retryCount})");
                    }
                }
                finally
                {
                    bmpCell?.Dispose();
                }

                if (i < retryCount) await Task.Delay(c_nWaitLong, ctrl.Token);
            }

            return new StdResult_String($"오더번호 OFR 실패 ({retryCount}회 시도)", "Get오더번호Async_99");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{AppName}] Get오더번호Async 예외: {ex.Message}");
            return new StdResult_String(StdUtil.GetExceptionMessage(ex), "Get오더번호Async_999");
        }
    }
    #endregion

    #region Click새로고침버튼Async
    /// <summary>
    /// 새로고침 버튼 클릭 (포커스 탈출 → 클릭 → 클릭 확인 → 딜레이)
    /// </summary>
    public async Task<StdResult_Status> Click새로고침버튼Async(CancelTokenControl ctrl, int retryCount = c_nRepeatShort)
    {
        try
        {
            for (int i = 1; i <= retryCount; i++)
            {
                await ctrl.WaitIfPausedOrCancelledAsync();

                // 1. 포커스 탈출
                await EscapeFocusAsync(ctrl.Token);

                // 2. 새로고침 버튼 클릭
                await Std32Mouse_Post.MousePostAsync_ClickLeft(mRcpt.검색섹션_hWnd새로고침버튼);

                // 3. 클릭 확인 (명도 <= 10 대기, 최대 500ms)
                bool bClicked = false;
                for (int j = 0; j < 500; j++)
                {
                    int brightness = OfrService.GetPixelBrightnessFrmWndHandle(
                        mRcpt.검색섹션_hWnd새로고침버튼, fInfo.접수등록Page_검색_Focused_ptChkRelS);
                    if (brightness <= 10)
                    {
                        bClicked = true;
                        break;
                    }
                    await Task.Delay(1, ctrl.Token);
                }

                if (!bClicked)
                {
                    Debug.WriteLine($"[{AppName}] 새로고침 버튼 클릭 확인 실패 (시도 {i}/{retryCount})");
                    continue;
                }

                // 4. 오더량 기반 딜레이 (100개 단위, 최소 100ms)
                int delay = ((m_nLastTotalCount / 100) + 1) * 100;
                Debug.WriteLine($"[{AppName}] 새로고침 딜레이: {delay}ms (총계: {m_nLastTotalCount})");
                await Task.Delay(delay, ctrl.Token);

                return new StdResult_Status(StdResult.Success, "새로고침 완료");
            }

            return new StdResult_Status(StdResult.Fail, $"새로고침 버튼 클릭 {retryCount}회 모두 실패", "Click새로고침버튼Async_01");
        }
        catch (Exception ex)
        {
            return new StdResult_Status(StdResult.Fail, StdUtil.GetExceptionMessage(ex), "Click새로고침버튼Async_999");
        }
    }
    #endregion

    #region Get총계Async
    /// <summary>
    /// 총계 OFR (DG오더_hWndTop 기준, 확장상태에 따라 Small/Large 영역 선택)
    /// </summary>
    public async Task<StdResult_Int> Get총계Async(CancelTokenControl ctrl, int retryCount = c_nRepeatShort)
    {
        try
        {
            Draw.Rectangle rcTotal = IsDGExpanded()
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
                    var result = await OfrWork_Common.OfrStr_ComplexCharSetAsync(bmpTotal, bTextSave: false, bEdit: i == retryCount);
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
    #endregion

    #region IsDGExpanded
    /// <summary>
    /// DG오더 확장 상태 확인 (접수섹션이 안보이면 확장 상태)
    /// </summary>
    /// <returns>true: 확장(Large, 34행), false: 축소(Small, 17행)</returns>
    public bool IsDGExpanded()
    {
        return !Std32Window.IsWindowVisible(mRcpt.접수섹션_hWndTop);
    }
    #endregion

    #region GetValidRowCount
    /// <summary>
    /// DG오더의 유효 로우 수 반환 (Small 모드 고정)
    /// - 배경 밝기(255)보다 어두우면 데이터 있는 로우로 판단
    /// </summary>
    public StdResult_Int GetValidRowCount()
    {
        try
        {
            Draw.Rectangle[,] rects = mRcpt.DG오더_rcRelSmallCells;
            int maxRows = fInfo.접수등록Page_DG오더Small_RowsCount;

            if (rects == null)
                return new StdResult_Int("DG오더_rcRelSmallCells 미초기화", "GetValidRowCount_01");

            int nBackgroundBright = 255;  // 원콜 빈 셀 배경은 흰색
            int nThreshold = nBackgroundBright - 1;
            int nValidRows = 0;

            for (int row = 0; row < maxRows; row++)
            {
                // [col, row] 순서 - 첫 번째 열의 중앙 위치에서 밝기 체크
                int nCurBright = OfrService.GetPixelBrightnessFrmWndHandle(
                    mRcpt.DG오더_hWndTop,
                    rects[0, row].Right,
                    rects[0, row].Top + 6);

                if (nCurBright < nThreshold)
                    nValidRows++;
                else
                    break;
            }

            return new StdResult_Int(nValidRows);
        }
        catch (Exception ex)
        {
            return new StdResult_Int(StdUtil.GetExceptionMessage(ex), "GetValidRowCount_999");
        }
    }
    #endregion
}
#nullable restore
