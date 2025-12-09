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
                    hFind = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, fInfo.접수등록Page_접수_화물정보_ptChkRelS);
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
                    hFind = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, fInfo.접수등록Page_접수_화물정보_ptChkRelS);
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

                var ofrResult = await OfrWork_Common.OfrStr_ComplexCharSetAsync(bmpVerify, i == c_nRepeatShort);

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
    #endregion
}
#nullable restore
