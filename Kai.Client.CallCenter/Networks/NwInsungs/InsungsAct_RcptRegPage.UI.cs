using System.Diagnostics;
using Draw = System.Drawing;

using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;
using Kai.Common.NetDll_WpfCtrl.NetOFR;
using Kai.Common.FrmDll_FormCtrl;
using static Kai.Common.FrmDll_FormCtrl.FormFuncs;

using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Services;

using Kai.Client.CallCenter.Classes;
using Kai.Client.CallCenter.Classes.Class_Master;
using Kai.Client.CallCenter.OfrWorks;
using static Kai.Client.CallCenter.Classes.CommonVars;

namespace Kai.Client.CallCenter.Networks.NwInsungs;

#nullable disable

/// <summary>
/// InsungsAct_RcptRegPage의 UI 관련 함수들
/// - Datagrid 읽기/검증/탐색
/// - 팝업 처리
/// - UI 컨트롤 조작
/// </summary>
public partial class InsungsAct_RcptRegPage
{
    #region UI 헬퍼 함수들

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
            "다마" or "다마스" => 4,
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

    private async Task<StdResult_Status> SetGroupFeeTypeAsync(Draw.Bitmap bmpOrg, OfrModel_RadioBtns btns, string sFeeType, CancelTokenControl ctrl)
    {
        int index = GetFeeTypeIndex(sFeeType);
        return await SetCheckRadioBtn_InGroupAsync(bmpOrg, btns, index, ctrl);
    }
    #endregion

    #region 창/팝업 제어 함수들

    /// <summary>
    /// 버튼 클릭 후 윈도우가 닫힐 때까지 대기
    /// </summary>
    /// <param name="hWndClick">클릭할 버튼 핸들</param>
    /// <param name="hWndOrg">닫혀야 할 윈도우 핸들</param>
    /// <param name="ctrl">취소 토큰 컨트롤</param>
    /// <returns>윈도우가 닫혔으면 true, 실패하면 false</returns>
    private async Task<bool> ClickNWaitWindowChangedAsync(IntPtr hWndClick, IntPtr hWndOrg, CancelTokenControl ctrl)
    {
        Debug.WriteLine($"[{m_Context.AppName}] ClickNWaitWindowChangedAsync 시작: hWndClick={hWndClick:X}, hWndOrg={hWndOrg:X}");

        for (int i = 1; i <= c_nRepeatShort; i++)
        {
            await ctrl.WaitIfPausedOrCancelledAsync();
            Debug.WriteLine($"[{m_Context.AppName}] 버튼 클릭 시도 {i}/{c_nRepeatShort} - 클릭 전");

            // 버튼 클릭
            await Std32Mouse_Post.MousePostAsync_ClickLeft_Center(hWndClick);
            Debug.WriteLine($"[{m_Context.AppName}] 버튼 클릭 완료 {i}/{c_nRepeatShort}");

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
    /// 확인창 닫기 (WM_SYSCOMMAND SC_CLOSE 전송)
    /// </summary>
    /// <param name="ctrl">취소 토큰 컨트롤</param>
    /// <param name="sClassName">확인창 클래스명 (기본값: "#32770")</param>
    /// <param name="sCaption">확인창 캡션 (기본값: "확인")</param>
    /// <returns>확인창을 찾아서 닫았으면 true, 없으면 false</returns>
    private async Task<bool> CloseConfirmWindowAsync(CancelTokenControl ctrl, string sClassName = "#32770", string sCaption = "확인")
    {
        await ctrl.WaitIfPausedOrCancelledAsync();

        // 1. 확인창 찾기
        IntPtr hWndConfirm = Std32Window.FindMainWindow(m_MemInfo.Splash.TopWnd_uProcessId, sClassName, sCaption);
        if (hWndConfirm == IntPtr.Zero)
        {
            Debug.WriteLine($"[{m_Context.AppName}] 확인창을 찾을 수 없음");
            return false;
        }

        Debug.WriteLine($"[{m_Context.AppName}] 확인창 발견: hWnd={hWndConfirm:X}");

        // 2. 확인창 닫기 (WM_SYSCOMMAND SC_CLOSE 전송)
        Std32Window.SendCloseWindow(hWndConfirm);
        Debug.WriteLine($"[{m_Context.AppName}] 확인창에 WM_SYSCOMMAND SC_CLOSE 전송 완료");

        // 3. 확인창이 닫힐 때까지 대기
        for (int i = 0; i < 50; i++)
        {
            await Task.Delay(50, ctrl.Token);
            if (!Std32Window.IsWindow(hWndConfirm))
            {
                Debug.WriteLine($"[{m_Context.AppName}] 확인창 닫힘 확인 (대기 {i * 50}ms)");
                return true;
            }
        }

        Debug.WriteLine($"[{m_Context.AppName}] 확인창이 닫히지 않음");
        return false;
    }

    /// <summary>
    /// 저장 버튼 클릭 후 윈도우가 닫히거나 확인창이 나타날 때까지 대기
    /// </summary>
    /// <param name="hWndClick">클릭할 버튼 핸들</param>
    /// <param name="hWndOrg">닫혀야 할 윈도우 핸들</param>
    /// <param name="ctrl">취소 토큰 컨트롤</param>
    /// <param name="sClassName">확인창 클래스명 (기본값: "#32770")</param>
    /// <param name="sCaption">확인창 캡션 (기본값: "확인")</param>
    /// <returns>윈도우가 정상적으로 닫혔으면 true, 확인창이 나타나면 false</returns>
    private async Task<bool> ClickNWaitWindowChangedAsync_OrFind확인창(IntPtr hWndClick, IntPtr hWndOrg, CancelTokenControl ctrl, string sClassName = "#32770", string sCaption = "확인")
    {
        for (int i = 1; i <= CommonVars.c_nRepeatNormal; i++)
        {
            await ctrl.WaitIfPausedOrCancelledAsync();

            // 버튼 클릭
            await Std32Mouse_Post.MousePostAsync_ClickLeft_Center(hWndClick);
            Debug.WriteLine($"[{m_Context.AppName}] 저장 버튼 클릭 시도 {i}/{CommonVars.c_nRepeatNormal}");

            // 윈도우가 닫히거나 확인창이 나타날 때까지 대기 (최대 5초: 100회 × 50ms)
            for (int j = 0; j < CommonVars.c_nRepeatVeryMany; j++)
            {
                await Task.Delay(CommonVars.c_nWaitShort, ctrl.Token);

                // 윈도우가 정상적으로 닫혔는지 확인
                if (!Std32Window.IsWindow(hWndOrg))
                {
                    Debug.WriteLine($"[{m_Context.AppName}] 윈도우 정상 닫힘 확인 (시도 {i}, 대기 {j * CommonVars.c_nWaitShort}ms)");
                    return true;
                }

                // 확인창이 나타났는지 확인 (저장 실패 상황)
                IntPtr hWndConfirm = Std32Window.FindMainWindow(m_MemInfo.Splash.TopWnd_uProcessId, sClassName, sCaption);
                if (hWndConfirm != IntPtr.Zero)
                {
                    Debug.WriteLine($"[{m_Context.AppName}] 확인창 발견 - 저장 실패 (시도 {i}, 대기 {j * CommonVars.c_nWaitShort}ms)");
                    return false;
                }
            }

            Debug.WriteLine($"[{m_Context.AppName}] {i}번째 시도 실패 - 윈도우가 닫히지 않음, 확인창도 없음");
        }

        Debug.WriteLine($"[{m_Context.AppName}] 모든 시도 실패 - 윈도우가 닫히지 않음");
        return false;
    }

    #endregion

    #region 버튼/컨트롤 찾기 함수들

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

    #endregion

    #region EditBox/ComboBox 입력 함수들

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

    #endregion

    #region 접수창 관련 함수들

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
            switch (searchResult.resultType)
            {
                case CEnum_CustSearchCount.One:
                    // 1개 검색 성공 - 그대로 진행
                    Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 검색 성공 (1개)");
                    return new StdResult_Status(StdResult.Success);

                case CEnum_CustSearchCount.Multi:
                    // TODO: 복수 고객 검색창 처리 필요
                    Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 복수 검색 결과 - TODO 처리 필요");
                    return new StdResult_Status(StdResult.Fail,
                        $"{fieldName} 복수 검색됨 (TODO: 고객검색창 처리 필요)",
                        "SearchAndSelectCustomerAsync_02");

                case CEnum_CustSearchCount.None:
                    // TODO: 신규 고객 등록창 처리 필요
                    Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 신규 고객 - TODO 처리 필요");
                    return new StdResult_Status(StdResult.Fail,
                        $"{fieldName} 신규 고객 (TODO: 고객등록창 처리 필요)",
                        "SearchAndSelectCustomerAsync_03");

                case CEnum_CustSearchCount.Null:
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
    private async Task<CommonResult_SearchType> GetCustSearchTypeAsync(IntPtr hWnd고객명, IntPtr hWnd동명, CancelTokenControl ctrl)
    {
        try
        {
            // 0. 입력 검증
            if (hWnd고객명 == IntPtr.Zero || hWnd동명 == IntPtr.Zero)
            {
                Debug.WriteLine($"[{m_Context.AppName}] GetCustSearchTypeAsync: 유효하지 않은 핸들 (고객명={hWnd고객명:X}, 동명={hWnd동명:X})");
                return new CommonResult_SearchType(CEnum_CustSearchCount.Null, IntPtr.Zero);
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
                    return new CommonResult_SearchType(CEnum_CustSearchCount.One, IntPtr.Zero);
                }

                // 2-2. 고객등록창 확인 → 신규 고객
                IntPtr hWnd고객등록 = Std32Window.FindMainWindow(
                    m_Context.MemInfo.Splash.TopWnd_uProcessId, null, m_Context.FileInfo.고객등록Wnd_TopWnd_sWndName);
                if (hWnd고객등록 != IntPtr.Zero)
                {
                    Debug.WriteLine($"[{m_Context.AppName}] 신규 고객 등록창 발견: {hWnd고객등록:X}");
                    return new CommonResult_SearchType(CEnum_CustSearchCount.None, hWnd고객등록);
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
                        return new CommonResult_SearchType(CEnum_CustSearchCount.Multi, hWnd고객검색);
                    }
                    // 창이 닫혔으면 다음 루프에서 동명 확인으로 이동
                }
            }

            // 3. 검색 실패 (타임아웃)
            Debug.WriteLine($"[{m_Context.AppName}] GetCustSearchTypeAsync 실패: 타임아웃 (최대 {CommonVars.c_nRepeatMany}회 시도)");
            return new CommonResult_SearchType(CEnum_CustSearchCount.Null, IntPtr.Zero);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{m_Context.AppName}] GetCustSearchTypeAsync 예외: {ex.Message}");
            return new CommonResult_SearchType(CEnum_CustSearchCount.Null, IntPtr.Zero);
        }
    }

    /// <summary>
    /// 신규 버튼 클릭 후 접수등록 팝업창 열기
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
                    StdResult_String resultSeqno = await GetSeqnoAsync(0, bInvertRgb: true, ctrl);
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

                    // 4-2. 업데이트 실행 (Request ID 사용)
                    item.NewOrder.Insung1 = resultSeqno.strResult;
                    StdResult_Int resultUpdate = await s_SrGClient.SrResult_Order_UpdateRowAsync_Today_WithRequestId(item.NewOrder);

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
}

#nullable restore
