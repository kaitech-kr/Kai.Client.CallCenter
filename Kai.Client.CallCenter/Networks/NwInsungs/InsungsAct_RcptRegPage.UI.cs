using Kai.Client.CallCenter.Classes;
using Kai.Client.CallCenter.Classes.Class_Master;
using Kai.Client.CallCenter.OfrWorks;
using Kai.Client.CallCenter.Windows;
using Kai.Common.NetDll_WpfCtrl.NetOFR;
using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models;
using System.Diagnostics;
using static Kai.Client.CallCenter.Classes.CommonVars;
using Draw = System.Drawing;

namespace Kai.Client.CallCenter.Networks.NwInsungs;

#nullable disable

// InsungsAct_RcptRegPage의 UI 관련 함수 (Datagrid, 팝업, 컨트롤 조작)
public partial class InsungsAct_RcptRegPage
{
    #region 1. Helpers - UI 헬퍼
    // CallCount 컨트롤 찾기 헬퍼 메서드
    private IntPtr FindCallCountControl(string controlName, Draw.Point checkPoint, string errorCode, out StdResult_Error error)
    {
        IntPtr hWnd = Std32Window.GetWndHandle_FromRelDrawPt(m_Main.TopWnd_hWnd, checkPoint);
        if (hWnd == IntPtr.Zero)
        {
            error = new StdResult_Error(
                $"[{m_Context.AppName}/RcptRegPage]{controlName}CallCount 찾기실패: {checkPoint}", "InsungsAct_RcptRegPage/FindCallCountControl");
            return IntPtr.Zero;
        }
        //Debug.WriteLine($"[InsungsAct_RcptRegPage] {controlName}CallCount 찾음: {hWnd:X}");
        error = null;
        return hWnd;
    }
    // 요금종류 문자열을 인덱스로 변환 (선불=0, 착불=1, 신용=2, 송금=3, 수금=4, 카드=5)
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

    // 차량종류 문자열을 인덱스로 변환 (오토=0, 밴=1, 트럭=2, 플렉스=3, 다마=4, 라보=5, 지하=6)
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

    // 배송타입 비트플래그를 인덱스로 변환 (인성1은 단일선택이므로 우선순위: 긴급 > 경유 > 왕복 > 편도)
    // 비트플래그: 편도=1, 왕복=2, 경유=4, 긴급=8, 혼적=16
    private int GetDeliverTypeIndex(string sDeliverFlag)
    {
        if (!int.TryParse(sDeliverFlag, out int flag))
            return 0; // 파싱 실패 시 편도

        if ((flag & 8) != 0) return 3;  // 긴급
        if ((flag & 4) != 0) return 2;  // 경유
        if ((flag & 2) != 0) return 1;  // 왕복
        return 0;  // 편도 (또는 미선택)
    }

    // 차량톤수 문자열을 ComboBox 인덱스로 변환
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

    // 트럭상세 문자열을 ComboBox 인덱스로 변환
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

    // 요금종류 RadioButton 설정
    private async Task<StdResult_Status> SetGroupFeeTypeAsync(Draw.Bitmap bmpOrg, OfrModel_RadioBtns btns, string sFeeType, CancelTokenControl ctrl)
    {
        int index = GetFeeTypeIndex(sFeeType);
        return await SetCheckRadioBtn_InGroupAsync(bmpOrg, btns, index, ctrl);
    }

    // 배송타입 RadioButton 설정
    private async Task<StdResult_Status> SetGroupDeliverTypeAsync(Draw.Bitmap bmpOrg, OfrModel_RadioBtns btns, string sDeliverType, CancelTokenControl ctrl)
    {
        int index = GetDeliverTypeIndex(sDeliverType);
        return await SetCheckRadioBtn_InGroupAsync(bmpOrg, btns, index, ctrl);
    }

    // 트럭 RadioButton 클릭 + ComboBox 처리 (현재가 트럭 아닐 때)
    private async Task<StdResult_Status> SetGroupCarTypeAsync_트럭(Draw.Bitmap bmpOrg, OfrModel_RadioBtns btns, TbOrder tbOrder, CancelTokenControl ctrl)
    {
        try
        {
            // 1. 트럭 클릭 전 ComboBox 체크 위치의 Handle 저장
            Debug.WriteLine($"[{m_Context.AppName}] 트럭 선택 예정 → 차량무게, 트럭상세 ComboBox 체크 위치 Handle 저장...");

            IntPtr hWndParent = btns.hWndTop;
            Debug.WriteLine($"[{m_Context.AppName}] btns.hWndTop={btns.hWndTop:X}, btns.hWndMid={btns.hWndMid:X}");

            // 차량무게 체크 위치: 상대→절대 좌표 변환 및 Handle 저장
            Draw.Point ptCheckCarWeightComboOpen = StdUtil.GetAbsDrawPointFromRel(hWndParent, m_Context.FileInfo.접수등록Wnd_우측상단_차량톤수Open_ptChkRelT);
            IntPtr hWndCheckCarWeightComboOpen = Std32Window.GetWndHandle_FromAbsDrawPt(ptCheckCarWeightComboOpen);
            Debug.WriteLine($"[{m_Context.AppName}] 차량무게 - 상대좌표: {m_Context.FileInfo.접수등록Wnd_우측상단_차량톤수Open_ptChkRelT}, 절대좌표: {ptCheckCarWeightComboOpen}, Handle={hWndCheckCarWeightComboOpen:X}");

            // 트럭상세 체크 위치: 상대→절대 좌표 변환 및 Handle 저장
            Draw.Point ptCheckTruckDetailComboOpen = StdUtil.GetAbsDrawPointFromRel(hWndParent, m_Context.FileInfo.접수등록Wnd_우측상단_트럭상세Open_ptChkRelT);
            IntPtr hWndCheckTruckDetailComboOpen = Std32Window.GetWndHandle_FromAbsDrawPt(ptCheckTruckDetailComboOpen);
            Debug.WriteLine($"[{m_Context.AppName}] 트럭상세 - 상대좌표: {m_Context.FileInfo.접수등록Wnd_우측상단_트럭상세Open_ptChkRelT}, 절대좌표: {ptCheckTruckDetailComboOpen}, Handle={hWndCheckTruckDetailComboOpen:X}");

            // 2. 트럭 RadioButton 클릭
            int index = GetCarTypeIndex("트럭");
            StdResult_Status result = await SetCheckRadioBtn_InGroupAsync(bmpOrg, btns, index, ctrl);
            if (result.Result != StdResult.Success)
            {
                return new StdResult_Status(StdResult.Fail, "트럭 RadioButton 클릭 실패", "SetGroupCarTypeAsync_트럭_01");
            }
            Debug.WriteLine($"[{m_Context.AppName}] 트럭 선택 완료 → 차량무게 ComboBox 열림 대기...");

            // 3. 차량무게 ComboBox 처리
            Debug.WriteLine($"[{m_Context.AppName}]   차량무게: {tbOrder.CarWeightFlag}");

            int indexCarWeight = Get차량톤수Index(tbOrder.CarWeightFlag);
            Debug.WriteLine($"[{m_Context.AppName}] Get차량톤수Index(\"{tbOrder.CarWeightFlag}\") 결과: indexCarWeight={indexCarWeight}");

            if (indexCarWeight == 0)
            {
                return new StdResult_Status(StdResult.Fail, $"지원하지 않는 차량무게: {tbOrder.CarWeightFlag}", "SetGroupCarTypeAsync_트럭_02");
            }

            result = await Select트럭ComboBoxItemAsync(hWndParent, ptCheckCarWeightComboOpen, hWndCheckCarWeightComboOpen, indexCarWeight, "차량무게", 100, ctrl);

            if (result.Result != StdResult.Success)
            {
                return new StdResult_Status(StdResult.Fail, $"차량무게 ComboBox 처리 실패: {result.sErr}", "SetGroupCarTypeAsync_트럭_03");
            }

            // 4. 트럭상세 ComboBox 처리
            Debug.WriteLine($"[{m_Context.AppName}] 차량무게 선택 완료 → 트럭상세 ComboBox 열림 대기...");

            int indexTruckDetail = Get트럭상세Index(tbOrder.TruckDetailFlag);
            Debug.WriteLine($"[{m_Context.AppName}] Get트럭상세Index(\"{tbOrder.TruckDetailFlag}\") 결과: indexTruckDetail={indexTruckDetail}");

            //if (indexTruckDetail == 0) // 이거 연구과제
            //{
            //    return new StdResult_Status(StdResult.Fail, $"지원하지 않는 트럭상세: {tbOrder.TruckDetailFlag}", "SetGroupCarTypeAsync_트럭_04");
            //}

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

    // RadioButton 그룹에서 특정 버튼 클릭 (OFR 기반)
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
            for (int j = 0; j < CommonVars.c_nRepeatVeryShort; j++)  // 10회
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

    #region 2. Window Control - 창/팝업 제어
    // 로딩 패널 대기 - Phase1: 출현대기(250ms), Phase2: 사라짐대기(timeoutSec초) → Success/Skip/Fail
    private async Task<StdResult_Status> WaitPanLoadedAsync(IntPtr hWndDG, CancelTokenControl ctrl, int timeoutSec = 50)
    {
        try
        {
            IntPtr hWndFind = IntPtr.Zero;
            Draw.Point ptCheckPan = m_FileInfo.접수등록Page_DG오더_LoadPan_ptChkRelL;

            // Phase 1: 로딩 패널 출현 대기 (최대 250ms)
            //Debug.WriteLine($"[{m_Context.AppName}] 로딩 패널 출현 대기 시작");

            for (int i = 0; i < CommonVars.c_nWaitLong; i++) // 250ms
            {
                await ctrl.WaitIfPausedOrCancelledAsync();

                hWndFind = Std32Window.GetWndHandle_FromRelDrawPt(hWndDG, ptCheckPan);
                if (hWndFind != hWndDG)
                {
                    //Debug.WriteLine($"[{m_Context.AppName}] 로딩 패널 출현 확인 ({i}ms)");
                    break;
                }
                await Task.Delay(1, ctrl.Token);
            }

            if (hWndFind == hWndDG)
            {
                //Debug.WriteLine($"[{m_Context.AppName}] 로딩 패널 미출현 → Skip (이미 로딩 완료)");
                return new StdResult_Status(StdResult.Skip);
            }

            // Phase 2: 로딩 패널 사라짐 대기 (최대 timeoutSec초)
            //Debug.WriteLine($"[{m_Context.AppName}] 로딩 패널 사라짐 대기 시작 (최대 {timeoutSec}초)");

            int iterations = timeoutSec * 10; // 100ms 단위
            for (int i = 0; i < iterations; i++)
            {
                await ctrl.WaitIfPausedOrCancelledAsync();

                hWndFind = Std32Window.GetWndHandle_FromRelDrawPt(hWndDG, ptCheckPan);
                if (hWndFind == hWndDG)
                {
                    int elapsedMs = i * 100;
                    //Debug.WriteLine($"[{m_Context.AppName}] 로딩 완료 ({elapsedMs}ms)");
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

    // 버튼 클릭 후 윈도우가 닫힐 때까지 대기
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
            for (int j = 0; j < c_nRepeatNormal; j++)
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

    // 확인창의 예 버튼 클릭 (등록창 확인창과 동일) - [2025-12-04] Cargo24 작업 중 추가, 미사용
    private async Task<bool> ClickConfirmYesButtonAsync(CancelTokenControl ctrl, string sClassName = "#32770", string sCaption = "확인")
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

        // 2. "예(&Y)" 버튼 찾기
        IntPtr hWndBtn = Std32Window.FindWindowEx(hWndConfirm, IntPtr.Zero, "Button", "예(&Y)");
        if (hWndBtn == IntPtr.Zero)
        {
            Debug.WriteLine($"[{m_Context.AppName}] '예' 버튼을 찾을 수 없음");
            return false;
        }

        Debug.WriteLine($"[{m_Context.AppName}] '예' 버튼 발견: hWnd={hWndBtn:X}");

        // 3. 예 버튼 클릭
        await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndBtn, 5, 5, 50);
        await Task.Delay(200, ctrl.Token);

        // 4. 확인창이 닫힐 때까지 대기
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

    // 확인창 닫기 (WM_SYSCOMMAND SC_CLOSE 전송)
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

    // 저장 버튼 클릭 후 윈도우가 닫히거나 확인창이 나타날 때까지 대기
    private async Task<bool> ClickNWaitWindowChangedAsync_OrFind확인창(IntPtr hWndClick, IntPtr hWndOrg, CancelTokenControl ctrl, string sClassName = "#32770", string sCaption = "확인")
    {
        for (int i = 1; i <= CommonVars.c_nRepeatVeryShort; i++)
        {
            await ctrl.WaitIfPausedOrCancelledAsync();

            // 버튼 클릭
            await Std32Mouse_Post.MousePostAsync_ClickLeft_Center(hWndClick);
            Debug.WriteLine($"[{m_Context.AppName}] 저장 버튼 클릭 시도 {i}/{CommonVars.c_nRepeatVeryShort}");

            // 윈도우가 닫히거나 확인창이 나타날 때까지 대기 (최대 5초: 100회 × 50ms)
            for (int j = 0; j < CommonVars.c_nRepeatNormal; j++)
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

    #region 3. Buttons - 버튼 함수
    // 조회 버튼 클릭 및 데이터 로딩 대기
    public async Task<StdResult_Status> Click조회버튼Async(CancelTokenControl ctrl, int retryCount = 3)
    {
        try
        {
            for (int i = 1; i <= retryCount; i++)
            {
                await ctrl.WaitIfPausedOrCancelledAsync();

                //Debug.WriteLine($"[{m_Context.AppName}] 조회 버튼 클릭 시도 {i}/{retryCount}");

                // 조회 버튼 클릭
                await Std32Mouse_Post.MousePostAsync_ClickLeft(m_RcptPage.CmdBtn_hWnd조회);

                // 로딩 패널 대기 - 이것만으로 성공 여부 판단
                StdResult_Status resultSts = await WaitPanLoadedAsync(m_RcptPage.DG오더_hWnd, ctrl);

                if (resultSts.Result == StdResult.Success || resultSts.Result == StdResult.Skip)
                {
                    //Debug.WriteLine($"[{m_Context.AppName}] 조회 완료 (시도 {i}회, 결과: {resultSts.Result})");
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
    // 
    //     // 상태 버튼 찾기 (텍스트 검증 포함)
    //     private async Task<(IntPtr hWnd, StdResult_Error error)> FindStatusButtonAsync(
    //         string buttonName, Draw.Point checkPoint, string errorCode, bool bWrite, bool bMsgBox, bool withTextValidation = true)
    //     {
    //         for (int i = 0; i < CommonVars.c_nRepeatNormal; i++)
    //         {
    //             IntPtr hWnd = Std32Window.GetWndHandle_FromRelDrawPt(m_Main.TopWnd_hWnd, checkPoint);
    // 
    //             if (hWnd != IntPtr.Zero)
    //             {
    //                 if (withTextValidation)
    //                 {
    //                     string text = Std32Window.GetWindowText(hWnd);
    //                     if (text.Contains(buttonName))
    //                     {
    //                         //Debug.WriteLine($"[InsungsAct_RcptRegPage] {buttonName}버튼 찾음: {hWnd:X}, 텍스트: {text}");
    //                         return (hWnd, null);
    //                     }
    //                 }
    //                 else
    //                 {
    //                     //Debug.WriteLine($"[InsungsAct_RcptRegPage] {buttonName}버튼 찾음: {hWnd:X}");
    //                     return (hWnd, null);
    //                 }
    //             }
    // 
    //             await Task.Delay(CommonVars.c_nWaitNormal);
    //         }
    // 
    //         // 찾기 실패
    //         var error = new StdResult_Error(
    //             $"[{m_Context.AppName}/RcptRegPage]{buttonName}버튼 찾기실패: {checkPoint}",
    //             errorCode, bWrite, bMsgBox);
    //         return (IntPtr.Zero, error);
    //     }
    // 
    // CommandBtn(OFR 검증 포함) 찾기 헬퍼 메서드 (재시도 로직 포함)
    private async Task<(IntPtr hWnd, StdResult_Error error)> FindCommandButtonWithOfrAsync(string buttonName, Draw.Point checkPoint, string ofrImageKey, string errorCode, bool bEdit, bool bWrite, bool bMsgBox)
    {
        IntPtr hWnd = IntPtr.Zero;

        for (int i = 1; i <= CommonVars.c_nRepeatVeryShort; i++)
        {
            // 1. 버튼 핸들 찾기
            hWnd = Std32Window.GetWndHandle_FromRelDrawPt(m_Main.TopWnd_hWnd, checkPoint);

            if (hWnd != IntPtr.Zero)
            {
                // 2. OFR 이미지 매칭으로 검증
                StdResult_NulBool resultOfr = await OfrWork_Insungs.OfrIsMatchedImage_DrawRelRectAsync(hWnd, 0, ofrImageKey, bEdit);

                if (StdConvert.NullableBoolToBool(resultOfr.bResult))
                {
                    if (bWrite) Debug.WriteLine($"[{m_Context.AppName}/UI] {buttonName}버튼 확인 완료: hWnd={hWnd:X} (시도={i}/{CommonVars.c_nRepeatVeryShort})");
                    return (hWnd, null);
                }

                if (bWrite) Debug.WriteLine($"[{m_Context.AppName}/UI] {buttonName}버튼 이미지 매칭 대기 중... ({ofrImageKey}, 시도={i}/{CommonVars.c_nRepeatVeryShort})");
            }
            else
            {
                if (bWrite) Debug.WriteLine($"[{m_Context.AppName}/UI] {buttonName}버튼 핸들 대기 중... (시도={i}/{CommonVars.c_nRepeatVeryShort})");
            }

            if (i < CommonVars.c_nRepeatVeryShort)
                await Task.Delay(CommonVars.c_nWaitNormal);
        }

        // 최종 실패 시 에러 반환
        var error = new StdResult_Error($"[{m_Context.AppName}/RcptRegPage] {buttonName}버튼 찾기 또는 검증 실패", "InsungsAct_RcptRegPage/FindCommandButtonWithOfrAsync");
        return (IntPtr.Zero, error);
    }

    #endregion

    #region 4. Input Helpers - EditBox/ComboBox 입력

    // EditBox 입력 및 검증 (재시도 포함)
    private async Task<StdResult_Status> WriteAndVerifyEditBoxAsync(IntPtr hWnd, string expectedValue, string fieldName, CancelTokenControl ctrl, Func<string, string> normalizeFunc = null)
    {
        try
        {
            await ctrl.WaitIfPausedOrCancelledAsync();

            if (hWnd == IntPtr.Zero)
            {
                return new StdResult_Status(StdResult.Fail, $"{fieldName} EditBox를 찾을 수 없습니다.", "WriteAndVerifyEditBoxAsync_00");
            }

            if (expectedValue == null) expectedValue = "";

            for (int i = 0; i < CommonVars.c_nRepeatShort; i++)
            {
                await ctrl.WaitIfPausedOrCancelledAsync();

                string writtenValue = await OfrWork_Common.WriteEditBox_ToHndleAsyncWait(hWnd, expectedValue);

                if (normalizeFunc != null) writtenValue = normalizeFunc(writtenValue);

                if (expectedValue == writtenValue)
                {
                    return new StdResult_Status(StdResult.Success);
                }

                Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 입력 불일치 (재시도 {i + 1}/{CommonVars.c_nRepeatShort}): 예상='{expectedValue}', 실제='{writtenValue}'");
                await Task.Delay(CommonVars.c_nWaitLong, ctrl.Token);
            }

            string finalValue = Std32Window.GetWindowCaption(hWnd);
            if (normalizeFunc != null) finalValue = normalizeFunc(finalValue);

            return new StdResult_Status(StdResult.Fail, $"{fieldName} 입력 실패: 예상='{expectedValue}', 실제='{finalValue}'", "WriteAndVerifyEditBoxAsync_01");
        }
        catch (Exception ex)
        {
            return new StdResult_Status(StdResult.Fail, StdUtil.GetExceptionMessage(ex), "WriteAndVerifyEditBoxAsync_999");
        }
    }

    // 트럭 ComboBox 처리 헬퍼 (항목 선택)
    private async Task<StdResult_Status> Select트럭ComboBoxItemAsync(
        IntPtr hWndParent, Draw.Point ptCheckAbs, IntPtr hWndBefore, int itemIndex, string sFieldName, int maxAttempts, CancelTokenControl ctrl)
    {
        try
        {
            IntPtr hWndDropDown = IntPtr.Zero;
            for (int i = 0; i < maxAttempts; i++)
            {
                await ctrl.WaitIfPausedOrCancelledAsync();
                await Task.Delay(30, ctrl.Token);

                IntPtr hWndCurrent = Std32Window.GetWndHandle_FromAbsDrawPt(ptCheckAbs);

                if (hWndCurrent != hWndBefore && hWndCurrent != IntPtr.Zero)
                {
                    hWndDropDown = hWndCurrent;
                    break;
                }
            }

            if (hWndDropDown == IntPtr.Zero)
            {
                return new StdResult_Status(StdResult.Fail, $"{sFieldName} ComboBox 열림 실패", $"Select트럭ComboBoxItemAsync_{sFieldName}_01");
            }

            await ctrl.WaitIfPausedOrCancelledAsync();
            Draw.Point ptItem = m_Context.FileInfo.접수등록Wnd_Common_ptComboBox[itemIndex];
            await Std32Mouse_Post.MousePostAsync_ClickLeft_ptRel(hWndDropDown, ptItem);

            await Task.Delay(100, ctrl.Token);
            return new StdResult_Status(StdResult.Success);
        }
        catch (Exception ex)
        {
            return new StdResult_Status(StdResult.Fail, StdUtil.GetExceptionMessage(ex), $"Select트럭ComboBoxItemAsync_{sFieldName}_999");
        }
    }

    // 요금종류 RadioButton 현재 상태 확인
    private async Task<StdResult_NulBool> IsChecked요금종류Async(Draw.Bitmap bmpOrg, OfrModel_RadioBtns btns, string sFeeType)
    {
        int index = GetFeeTypeIndex(sFeeType);
        OfrModel_RadioBtn btn = btns.Btns[index];
        return await OfrWork_Insungs.OfrImgChkValue_RectInBitmapAsync(bmpOrg, btn.rcRelPartsT);
    }

    private async Task<StdResult_NulBool> IsChecked배송타입Async(Draw.Bitmap bmpOrg, OfrModel_RadioBtns btns, string sDeliverType)
    {
        int index = GetDeliverTypeIndex(sDeliverType);
        OfrModel_RadioBtn btn = btns.Btns[index];
        return await OfrWork_Insungs.OfrImgChkValue_RectInBitmapAsync(bmpOrg, btn.rcRelPartsT);
    }

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

    #region 5. Receipt Window - 접수창 관련
    // 신규 버튼 클릭 후 접수등록 팝업창 열기
    public async Task<StdResult_Status> OpenNewOrderPopupAsync(AutoAllocModel item, CancelTokenControl ctrl)
    {
        IntPtr hWndPopup = IntPtr.Zero;
        bool bFound = false;

        try
        {
            for (int retry = 1; retry <= c_nRepeatShort; retry++)
            {
                await ctrl.WaitIfPausedOrCancelledAsync();

                await Std32Mouse_Post.MousePostAsync_ClickLeft(m_RcptPage.CmdBtn_hWnd신규);
                Debug.WriteLine($"[{m_Context.AppName}] OpenNewOrderPopupAsync - 신규버튼 클릭 시도 {retry}/{c_nRepeatShort}");

                for (int k = 0; k < CommonVars.c_nRepeatShort; k++)
                {
                    await Task.Delay(CommonVars.c_nWaitNormal, ctrl.Token);

                    hWndPopup = Std32Window.FindMainWindow_NotTransparent(
                        m_Context.MemInfo.Splash.TopWnd_uProcessId, m_Context.FileInfo.접수등록Wnd_TopWnd_sWndName_Reg);
                    if (hWndPopup == IntPtr.Zero) continue;

                    IntPtr hWndClose = Std32Window.GetWndHandle_FromRelDrawPt(
                        hWndPopup, m_Context.FileInfo.접수등록Wnd_신규버튼그룹_닫기_ptChkRelT);
                    if (hWndClose == IntPtr.Zero) continue;

                    string closeText = Std32Window.GetWindowCaption(hWndClose);
                    if (closeText.StartsWith(m_Context.FileInfo.접수등록Wnd_공용버튼그룹_닫기_sWndName))
                    {
                        bFound = true;
                        Debug.WriteLine($"[{m_Context.AppName}] 신규주문 팝업창 활성화 확인: {hWndPopup:X}");
                        break;
                    }
                }

                if (bFound) break;
            }

            if (bFound) return await RegistOrderToPopupAsync(item, hWndPopup, ctrl);
            else return new StdResult_Status(StdResult.Fail, "신규 버튼 클릭 후 팝업창이 열리지 않음", "OpenNewOrderPopupAsync_01");
        }
        catch (Exception ex)
        {
            return new StdResult_Status(StdResult.Fail, StdUtil.GetExceptionMessage(ex), "OpenNewOrderPopupAsync_999");
        }
    }


    // 팝업창에 주문 정보 입력 및 등록 (TopMost 설정, 입력, 닫기 버튼 클릭, 창 닫힘 확인)
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
                await Std32Key_Msg.KeyPost_DownAsync(wndRcpt.출발지_hWnd고객명, StdCommon32.VK_RETURN);
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
                await Std32Key_Msg.KeyPost_DownAsync(wndRcpt.도착지_hWnd고객명, StdCommon32.VK_RETURN);
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
            result = await WriteAndVerifyEditBoxAsync(wndRcpt.우측상단_hWnd적요, tbOrder.DeliverMemo ?? "", "적요", ctrl);
            if (result.Result != StdResult.Success) goto EXIT;

            // 4-2. 공유 (Share) - CheckBox (OFR 이미지 처리)
            Debug.WriteLine($"[{m_Context.AppName}] 4-2. 공유 CheckBox 처리...");

            // 화면 캡처
            bmpWnd = OfrService.CaptureScreenRect_InWndHandle(wndRcpt.TopWnd_hWnd, 0);

            // 현재 CheckBox 상태 읽기
            StdResult_NulBool resultShare = await OfrWork_Insungs.OfrImgChkValue_RectInBitmapAsync(bmpWnd, m_Context.FileInfo.접수등록Wnd_우측상단_공유_rcChkRelT);

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
                        await OfrWork_Common.SetCheckBox_StatusAsync(wndRcpt.TopWnd_hWnd, m_Context.FileInfo.접수등록Wnd_우측상단_공유_rcChkRelT, tbOrder.Share, "공유");

                    if (resultError == null) break;
                    if (i == CommonVars.c_nRepeatUltraShort- 1)  // 마지막 시도
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

                    if (i == CommonVars.c_nRepeatUltraShort- 1)  // 마지막 시도
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
            if (tbOrder.MovilityFlag == "트럭")
            {
                for (int i = 0; i < CommonVars.c_nRepeatShort; i++)  // 3회
                {
                    await ctrl.WaitIfPausedOrCancelledAsync();

                    result = await SetGroupCarTypeAsync_트럭(bmpWnd, wndRcpt.우측상단_btns차량종류, tbOrder, ctrl);
                    if (result.Result == StdResult.Success) break;

                    if (i == CommonVars.c_nRepeatUltraShort- 1)
                    {
                        result = new StdResult_Status(StdResult.Fail, $"트럭 설정 실패", "RegistOrderToPopupAsync_51");
                        goto EXIT;
                    }
                    await Task.Delay(CommonVars.c_nWaitNormal, ctrl.Token);
                }

                Debug.WriteLine($"[{m_Context.AppName}]   차량종류: {tbOrder.MovilityFlag}");
                Debug.WriteLine($"[{m_Context.AppName}]   차량무게: {tbOrder.CarWeightFlag}");
                Debug.WriteLine($"[{m_Context.AppName}]   트럭상세: {tbOrder.TruckDetailFlag}");
            }
            else
            {
                // 일반 차량: 요금종류와 동일한 패턴
                StdResult_NulBool resultCarType = await IsChecked차량종류Async(bmpWnd, wndRcpt.우측상단_btns차량종류, tbOrder.MovilityFlag);

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

                        int index = GetCarTypeIndex(tbOrder.MovilityFlag);
                        result = await SetCheckRadioBtn_InGroupAsync(bmpWnd, wndRcpt.우측상단_btns차량종류, index, ctrl);
                        if (result.Result == StdResult.Success) break;

                        if (i == CommonVars.c_nRepeatUltraShort- 1)
                        {
                            result = new StdResult_Status(StdResult.Fail, $"차량종류 설정 실패: {tbOrder.MovilityFlag}", "RegistOrderToPopupAsync_53");
                            goto EXIT;
                        }
                        await Task.Delay(CommonVars.c_nWaitNormal, ctrl.Token);
                    }
                }

                Debug.WriteLine($"[{m_Context.AppName}]   차량종류: {tbOrder.MovilityFlag}");
            }

            // 4-5. 배송타입 (DeliverType) - RadioButton OFR (bmpWnd 재사용)
            Debug.WriteLine($"[{m_Context.AppName}] 4-5. 배송타입 RadioButton 처리...");
            await ctrl.WaitIfPausedOrCancelledAsync();

            StdResult_NulBool resultDeliverType = await IsChecked배송타입Async(bmpWnd, wndRcpt.우측상단_btns배송종류, tbOrder.DeliverFlag);

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

                    result = await SetGroupDeliverTypeAsync(bmpWnd, wndRcpt.우측상단_btns배송종류, tbOrder.DeliverFlag, ctrl);
                    if (result.Result == StdResult.Success) break;

                    if (i == CommonVars.c_nRepeatUltraShort- 1)  // 마지막 시도
                    {
                        result = new StdResult_Status(StdResult.Fail, $"배송타입 설정 실패: {tbOrder.DeliverFlag}", "RegistOrderToPopupAsync_61");
                        goto EXIT;
                    }
                    await Task.Delay(CommonVars.c_nWaitNormal, ctrl.Token);  // 100ms
                }
            }

            Debug.WriteLine($"[{m_Context.AppName}]   배송타입: {tbOrder.DeliverFlag}");

            // 4-6. 계산서 (TaxBill) - CheckBox OFR (bmpWnd 재사용)
            Debug.WriteLine($"[{m_Context.AppName}] 4-6. 계산서 CheckBox 처리...");

            // 현재 CheckBox 상태 읽기
            StdResult_NulBool resultTaxBill = await OfrWork_Insungs.OfrImgChkValue_RectInBitmapAsync(bmpWnd, m_Context.FileInfo.접수등록Wnd_우측상단_계산서_rcChkRelT);

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
                        await OfrWork_Common.SetCheckBox_StatusAsync(wndRcpt.TopWnd_hWnd, m_Context.FileInfo.접수등록Wnd_우측상단_계산서_rcChkRelT, tbOrder.TaxBill, "계산서");

                    if (resultError == null) break;
                    if (i == CommonVars.c_nRepeatUltraShort- 1)  // 마지막 시도
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

            // 오더메모 (KeyCode/CallMemo 형식)
            string orderMemo = $"{tbOrder.KeyCode}/{tbOrder.CallMemo}";
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

                    // AppName에 따라 Insung1SeqNo 또는 Insung2SeqNo 체크
                    string existingSeqno = m_Context.AppName == StdConst_Network.INSUNG1
                        ? item.NewOrder.Insung1SeqNo
                        : item.NewOrder.Insung2SeqNo;

                    if (!string.IsNullOrEmpty(existingSeqno))
                    {
                        Debug.WriteLine($"[{m_Context.AppName}] 이미 등록된 주문번호: {existingSeqno}");
                        return new StdResult_Status(StdResult.Skip, $"이미 {m_Context.AppName} 번호가 등록되어 있습니다");
                    }

                    if (false) //s_SrGClient == null || !s_SrGClient.m_bLoginSignalR
                    {
                        Debug.WriteLine($"[{m_Context.AppName}] SignalR 연결 안됨");
                        return new StdResult_Status(StdResult.Fail, "서버 연결이 끊어졌습니다");
                    }

                    // 4-2. 업데이트 실행 (Request ID 사용) - AppName에 따라 분기
                    if (m_Context.AppName == StdConst_Network.INSUNG1)
                        item.NewOrder.Insung1SeqNo = resultSeqno.strResult;
                    else
                        item.NewOrder.Insung2SeqNo = resultSeqno.strResult;

                    StdResult_Int resultUpdate = await s_SrGClient.SrResult_Order_UpdateRowAsync_Today_WithRequestId(item.NewOrder);

                    if (resultUpdate.nResult < 0 || !string.IsNullOrEmpty(resultUpdate.sErr))
                    {
                        Debug.WriteLine($"[{m_Context.AppName}] Kai DB 업데이트 실패: {resultUpdate.sErr}");
                        return new StdResult_Status(StdResult.Fail, $"Kai DB 업데이트 실패: {resultUpdate.sErr}");
                    }

                    Debug.WriteLine($"[{m_Context.AppName}] Kai DB 업데이트 성공 - {m_Context.AppName}: {resultSeqno.strResult}");

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

    // Datagrid 상태 검증 (컬럼 개수, 순서, 너비 확인)
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

    // 업데이트
    // 팝업 내 주문 업데이트 실행 (핵심 로직)
    private async Task<CommonResult_AutoAllocProcess> UpdateOrderWidelyAsync(
        string wantState, AutoAllocModel item, CommonResult_AutoAllocDatagrid dgInfo, bool useRepeat, CancelTokenControl ctrl)
    {
        int repeatCount = useRepeat ? CommonVars.c_nRepeatVeryShort : 1;

        Debug.WriteLine($"[{m_Context.AppName}] UpdateOrderWidelyAsync 시작");
        Debug.WriteLine($"  - wantState: '{wantState}' (비어있으면 상태 버튼 안 누름)");
        Debug.WriteLine($"  - useRepeat: {useRepeat} (반복 횟수: {repeatCount})");

        for (int attempt = 0; attempt < repeatCount; attempt++)
        {
            await ctrl.WaitIfPausedOrCancelledAsync();

            // 1. 팝업 열기
            Debug.WriteLine($"[{m_Context.AppName}] 1단계: 팝업 열기 시도 (시도 {attempt + 1}/{repeatCount})");
            var (wnd, openError) = await OpenEditPopupAsync(dgInfo.nIndex, ctrl);

            if (openError != null)
            {
                Debug.WriteLine($"[{m_Context.AppName}] 팝업 열기 실패: {openError.sErr}");

                if (attempt < repeatCount - 1)
                {
                    Debug.WriteLine($"[{m_Context.AppName}] 재시도 예정 ({attempt + 1}/{repeatCount})");
                    continue;
                }

                // 모든 시도 실패 - 재시도 큐로
                Debug.WriteLine($"[{m_Context.AppName}] 팝업 열기 실패 (KeyCode: {item.KeyCode})");
                return CommonResult_AutoAllocProcess.SuccessAndReEnqueue();
            }

            Debug.WriteLine($"[{m_Context.AppName}] 팝업 열기 성공");

            // 2. 의뢰자 영역 업데이트
            Debug.WriteLine($"[{m_Context.AppName}] 2단계: 의뢰자 영역 업데이트 (KeyCode: {item.KeyCode})");
            var (changeCount, updateError) = await Update의뢰자영역Async(wnd, item.NewOrder, ctrl);

            if (updateError != null)
            {
                Debug.WriteLine($"[{m_Context.AppName}] 의뢰자 영역 업데이트 실패 (KeyCode: {item.KeyCode}): {updateError.sErr}");
                // 팝업 닫고 재시도
                await CloseEditPopupAsync(wnd, shouldSave: false, ctrl);
                return CommonResult_AutoAllocProcess.SuccessAndReEnqueue();
            }

            Debug.WriteLine($"[{m_Context.AppName}] 의뢰자 영역 업데이트 완료 (변경: {changeCount}개)");

            // 2-2. 출발지 영역 업데이트
            Debug.WriteLine($"[{m_Context.AppName}] 2-2단계: 출발지 영역 업데이트 (KeyCode: {item.KeyCode})");
            var (changeCount출발, updateError출발) = await Update출발지영역Async(wnd, item.NewOrder, ctrl);

            if (updateError출발 != null)
            {
                Debug.WriteLine($"[{m_Context.AppName}] 출발지 영역 업데이트 실패 (KeyCode: {item.KeyCode}): {updateError출발.sErr}");
                // 팝업 닫고 재시도
                await CloseEditPopupAsync(wnd, shouldSave: false, ctrl);
                return CommonResult_AutoAllocProcess.SuccessAndReEnqueue();
            }

            Debug.WriteLine($"[{m_Context.AppName}] 출발지 영역 업데이트 완료 (변경: {changeCount출발}개)");

            // 2-3. 도착지 영역 업데이트
            Debug.WriteLine($"[{m_Context.AppName}] 2-3단계: 도착지 영역 업데이트 (KeyCode: {item.KeyCode})");
            var (changeCount도착, updateError도착) = await Update도착지영역Async(wnd, item.NewOrder, ctrl);

            if (updateError도착 != null)
            {
                Debug.WriteLine($"[{m_Context.AppName}] 도착지 영역 업데이트 실패 (KeyCode: {item.KeyCode}): {updateError도착.sErr}");
                // 팝업 닫고 재시도
                await CloseEditPopupAsync(wnd, shouldSave: false, ctrl);
                return CommonResult_AutoAllocProcess.SuccessAndReEnqueue();
            }

            Debug.WriteLine($"[{m_Context.AppName}] 도착지 영역 업데이트 완료 (변경: {changeCount도착}개)");

            // 2-4. 우측상단 섹션 업데이트 (적요, 공유, 요금종류, 차량종류, 배송타입, 계산서)
            Debug.WriteLine($"[{m_Context.AppName}] 2-4단계: 우측상단 섹션 업데이트 시작");
            var (changeCount우측, updateError우측) = await Update우측상단영역Async(wnd, item.NewOrder, ctrl);

            if (updateError우측 != null)
            {
                Debug.WriteLine($"[{m_Context.AppName}] 우측상단 섹션 업데이트 실패 (KeyCode: {item.KeyCode}): {updateError우측.sErr}");
                await CloseEditPopupAsync(wnd, shouldSave: false, ctrl);
                return CommonResult_AutoAllocProcess.SuccessAndReEnqueue();
            }

            Debug.WriteLine($"[{m_Context.AppName}] 우측상단 섹션 업데이트 완료 (변경: {changeCount우측}개)");

            // 2-5. 요금 영역 업데이트 (순서: 우측상단 다음)
            Debug.WriteLine($"[{m_Context.AppName}] 2-5단계: 요금 영역 업데이트 시작");
            var (changeCount요금, updateError요금) = await Update요금영역Async(wnd, item.NewOrder, ctrl);

            if (updateError요금 != null)
            {
                Debug.WriteLine($"[{m_Context.AppName}] 요금 영역 업데이트 실패 (KeyCode: {item.KeyCode}): {updateError요금.sErr}");
                await CloseEditPopupAsync(wnd, shouldSave: false, ctrl);
                return CommonResult_AutoAllocProcess.SuccessAndReEnqueue();
            }

            Debug.WriteLine($"[{m_Context.AppName}] 요금 영역 업데이트 완료 (변경: {changeCount요금}개)");

            // 2-6. 오더메모 영역 업데이트
            Debug.WriteLine($"[{m_Context.AppName}] 2-6단계: 오더메모 영역 업데이트 시작");
            var (changeCount메모, updateError메모) = await Update오더메모영역Async(wnd, item.NewOrder, ctrl);

            if (updateError메모 != null)
            {
                Debug.WriteLine($"[{m_Context.AppName}] 오더메모 영역 업데이트 실패 (KeyCode: {item.KeyCode}): {updateError메모.sErr}");
                await CloseEditPopupAsync(wnd, shouldSave: false, ctrl);
                return CommonResult_AutoAllocProcess.SuccessAndReEnqueue();
            }

            Debug.WriteLine($"[{m_Context.AppName}] 오더메모 영역 업데이트 완료 (변경: {changeCount메모}개)");

            // 2-7. 상태 버튼 클릭 (wantState가 비어있지 않으면)
            int changeCount상태 = 0;
            if (!string.IsNullOrEmpty(wantState))
            {
                Debug.WriteLine($"[{m_Context.AppName}] 2-7단계: 상태 버튼 클릭 (목표 상태: {wantState})");

                IntPtr hWndStateBtn = wantState switch
                {
                    "접수" => wnd.Btn_hWnd접수상태,
                    "완료" => wnd.Btn_hWnd처리완료,
                    "대기" => wnd.Btn_hWnd대기,
                    "취소" => wnd.Btn_hWnd주문취소,
                    _ => IntPtr.Zero
                };

                if (hWndStateBtn == IntPtr.Zero)
                {
                    Debug.WriteLine($"[{m_Context.AppName}] 알 수 없는 상태 (wantState: {wantState}, KeyCode: {item.KeyCode})");
                    await CloseEditPopupAsync(wnd, shouldSave: false, ctrl);
                    return CommonResult_AutoAllocProcess.SuccessAndReEnqueue();
                }

                // 상태 버튼 클릭 전 텍스트 확인
                string beforeState = Std32Window.GetWindowCaption(wnd.Header_hWnd오더상태)?.Trim() ?? "";
                Debug.WriteLine($"[{m_Context.AppName}] 상태 버튼 클릭 전 Header_hWnd오더상태: '{beforeState}' (핸들: {wnd.Header_hWnd오더상태:X})");

                await Std32Mouse_Post.MousePostAsync_ClickLeft_Center(hWndStateBtn);
                Debug.WriteLine($"[{m_Context.AppName}] 상태 버튼 클릭 완료: {wantState}");

                // 2-7-1. 상태 변경 확인 (폴링 방식: 100회 * 50ms = 5초)
                string currentState = "";
                for (int i = 0; i < CommonVars.c_nRepeatNormal; i++)
                {
                    await Task.Delay(CommonVars.c_nWaitShort, ctrl.Token);
                    currentState = Std32Window.GetWindowCaption(wnd.Header_hWnd오더상태)?.Trim() ?? "";
                    if (currentState == wantState) break;
                }
                Debug.WriteLine($"[{m_Context.AppName}] 폴링 완료: 현재='{currentState}', 목표='{wantState}'");

                if (currentState != wantState)
                {
                    Debug.WriteLine($"[{m_Context.AppName}] 상태 변경 실패: 현재={currentState}, 목표={wantState}");
                    await CloseEditPopupAsync(wnd, shouldSave: false, ctrl);
                    Debug.WriteLine($"[{m_Context.AppName}] 상태 변경 실패로 팝업 닫음 (KeyCode: {item.KeyCode})");
                    return CommonResult_AutoAllocProcess.SuccessAndReEnqueue();
                }

                Debug.WriteLine($"[{m_Context.AppName}] 상태 변경 확인 완료: {wantState}");

                // 상태 버튼 클릭은 항상 변경건으로 취급 (백업 로직: nChanged = 101~104)
                changeCount상태 = 1;
            }

            int totalChangeCount = changeCount + changeCount출발 + changeCount도착 + changeCount우측 + changeCount요금 + changeCount메모 + changeCount상태;
            Debug.WriteLine($"[{m_Context.AppName}] 전체 업데이트 완료 (총 변경: {totalChangeCount}개, 상태버튼: {changeCount상태})");

            // 3. 팝업 닫기 (변경사항 있으면 저장, 없으면 그냥 닫기)
            bool shouldSave = totalChangeCount > 0;
            Debug.WriteLine($"[{m_Context.AppName}] 3단계: 팝업 닫기 시도 (shouldSave: {shouldSave})");
            bool closed = await CloseEditPopupAsync(wnd, shouldSave, ctrl);

            if (!closed)
            {
                Debug.WriteLine($"[{m_Context.AppName}] 팝업 닫기 실패 - 재시도 (KeyCode: {item.KeyCode})");
                return CommonResult_AutoAllocProcess.SuccessAndReEnqueue();
            }

            Debug.WriteLine($"[{m_Context.AppName}] 팝업 닫기 성공 - 의뢰자+출발지+도착지+우측상단+요금+메모 영역 업데이트 완료");

            // 취소 상태로 전환: 비적재 (더 이상 모니터링 불필요)
            // 그 외: 재적재 (계속 모니터링)
            if (wantState == "취소")
            {
                Debug.WriteLine($"[{m_Context.AppName}] 취소 상태 전환 완료 - 큐에서 제거 (변경: {totalChangeCount}개, KeyCode: {item.KeyCode})");
                return CommonResult_AutoAllocProcess.SuccessAndDestroy(item);
            }
            else
            {
                Debug.WriteLine($"[{m_Context.AppName}] 모든 영역 업데이트 완료 (변경: {totalChangeCount}개, KeyCode: {item.KeyCode})");
                return CommonResult_AutoAllocProcess.SuccessAndReEnqueue();
            }
        }

        // 모든 재시도 실패 (정상적으로는 위에서 return되므로 여기 도달 안 함)
        Debug.WriteLine($"[{m_Context.AppName}] 모든 재시도 실패 (KeyCode: {item.KeyCode})");
        return CommonResult_AutoAllocProcess.SuccessAndReEnqueue();
    }

    // 팝업 내 주문 상태만 업데이트 (필드는 건드리지 않음)
    private async Task<CommonResult_AutoAllocProcess> UpdateOrderStateOnlyAsync(
        string wantState, AutoAllocModel item, CommonResult_AutoAllocDatagrid dgInfo, bool useRepeat, CancelTokenControl ctrl)
    {
        if (string.IsNullOrEmpty(wantState))
        {
            Debug.WriteLine($"[{m_Context.AppName}] UpdateOrderStateOnlyAsync: wantState가 비어있음");
            return CommonResult_AutoAllocProcess.SuccessAndReEnqueue();
        }

        int repeatCount = useRepeat ? CommonVars.c_nRepeatVeryShort : 1;

        Debug.WriteLine($"[{m_Context.AppName}] UpdateOrderStateOnlyAsync 시작");
        Debug.WriteLine($"  - wantState: '{wantState}'");
        Debug.WriteLine($"  - useRepeat: {useRepeat} (반복 횟수: {repeatCount})");

        for (int attempt = 0; attempt < repeatCount; attempt++)
        {
            await ctrl.WaitIfPausedOrCancelledAsync();

            // 1. 팝업 열기
            Debug.WriteLine($"[{m_Context.AppName}] 1단계: 팝업 열기 시도 (시도 {attempt + 1}/{repeatCount})");
            var (wnd, openError) = await OpenEditPopupAsync(dgInfo.nIndex, ctrl);

            if (openError != null)
            {
                Debug.WriteLine($"[{m_Context.AppName}] 팝업 열기 실패: {openError.sErr}");

                if (attempt < repeatCount - 1)
                {
                    Debug.WriteLine($"[{m_Context.AppName}] 재시도 예정 ({attempt + 1}/{repeatCount})");
                    continue;
                }

                // 모든 시도 실패 - 재시도 큐로
                Debug.WriteLine($"[{m_Context.AppName}] 팝업 열기 실패 (KeyCode: {item.KeyCode})");
                return CommonResult_AutoAllocProcess.SuccessAndReEnqueue();
            }

            Debug.WriteLine($"[{m_Context.AppName}] 팝업 열기 성공");

            // 2. 상태 버튼 클릭 (현재 상태가 완료면 RcptWnd_Completed 사용)
            Debug.WriteLine($"[{m_Context.AppName}] 2단계: 상태 버튼 클릭 (목표 상태: {wantState})");
            Debug.WriteLine($"[{m_Context.AppName}] 현재 인성 상태: '{dgInfo.sStatus}'");

            IntPtr hWndStateBtn;
            bool isCompletedState = dgInfo.sStatus?.StartsWith("완료") == true;

            if (isCompletedState)
            {
                // 완료 상태 → RcptWnd_Completed 사용
                Debug.WriteLine($"[{m_Context.AppName}] 완료 상태 감지 → RcptWnd_Completed 사용");
                var wndCompleted = new InsungsInfo_Mem.RcptWnd_Completed(wnd.TopWnd_hWnd, m_FileInfo);
                hWndStateBtn = wndCompleted.Btn_hWnd주문취소;
            }
            else
            {
                // 그 외 상태 → 일반 수정 팝업
                Debug.WriteLine($"[{m_Context.AppName}] 일반 상태 → RcptWnd_Edit 사용");
                hWndStateBtn = wantState switch
                {
                    "접수" => wnd.Btn_hWnd접수상태,
                    "완료" => wnd.Btn_hWnd처리완료,
                    "대기" => wnd.Btn_hWnd대기,
                    "취소" => wnd.Btn_hWnd주문취소,
                    _ => IntPtr.Zero
                };
            }

            if (hWndStateBtn == IntPtr.Zero)
            {
                Debug.WriteLine($"[{m_Context.AppName}] 알 수 없는 상태 (wantState: {wantState}, KeyCode: {item.KeyCode})");
                await CloseEditPopupAsync(wnd, shouldSave: false, ctrl);
                return CommonResult_AutoAllocProcess.SuccessAndReEnqueue();
            }

            await Std32Mouse_Post.MousePostAsync_ClickLeft_Center(hWndStateBtn);
            Debug.WriteLine($"[{m_Context.AppName}] 상태 버튼 클릭 완료: {wantState}");

            // 2-1. 상태 변경 확인 (폴링 방식: 100회 * 50ms = 5초)
            string currentState = "";
            for (int i = 0; i < CommonVars.c_nRepeatNormal; i++)
            {
                await Task.Delay(CommonVars.c_nWaitShort, ctrl.Token);
                currentState = Std32Window.GetWindowCaption(wnd.Header_hWnd오더상태)?.Trim() ?? "";
                if (currentState == wantState) break;
            }
            Debug.WriteLine($"[{m_Context.AppName}] 폴링 완료: 현재='{currentState}', 목표='{wantState}'");

            if (currentState != wantState)
            {
                Debug.WriteLine($"[{m_Context.AppName}] 상태 변경 실패: 현재={currentState}, 목표={wantState}");
                await CloseEditPopupAsync(wnd, shouldSave: false, ctrl);

                if (attempt < repeatCount - 1)
                {
                    Debug.WriteLine($"[{m_Context.AppName}] 재시도 ({attempt + 2}/{repeatCount})");
                    continue;
                }
                else
                {
                    Debug.WriteLine($"[{m_Context.AppName}] 모든 재시도 실패");
                    return CommonResult_AutoAllocProcess.SuccessAndReEnqueue();
                }
            }

            Debug.WriteLine($"[{m_Context.AppName}] 상태 변경 확인 완료: {wantState}");

            // 3. 팝업 닫기 (상태 버튼을 눌렀으므로 항상 저장)
            Debug.WriteLine($"[{m_Context.AppName}] 3단계: 팝업 닫기 시도 (저장)");

            bool closed;
            if (isCompletedState)
            {
                // 완료 상태일 때는 RcptWnd_Completed의 저장 버튼 클릭
                var wndCompleted = new InsungsInfo_Mem.RcptWnd_Completed(wnd.TopWnd_hWnd, m_FileInfo);
                Debug.WriteLine($"[{m_Context.AppName}] 완료 상태 팝업 저장 시작");
                await Task.Delay(CommonVars.c_nWaitNormal, ctrl.Token);
                closed = await ClickNWaitWindowChangedAsync_OrFind확인창(wndCompleted.Btn_hWnd저장, wnd.TopWnd_hWnd, ctrl);

                if (!closed)
                {
                    Debug.WriteLine($"[{m_Context.AppName}] 저장 실패 (확인창 나타남) - 확인창 닫기 후 팝업 닫기 시도");
                    bool confirmClosed = await CloseConfirmWindowAsync(ctrl);
                    if (confirmClosed)
                    {
                        Debug.WriteLine($"[{m_Context.AppName}] 확인창 닫기 성공");
                    }
                    await Task.Delay(CommonVars.c_nWaitShort, ctrl.Token);
                    closed = await ClickNWaitWindowChangedAsync(wndCompleted.Btn_hWnd닫기, wnd.TopWnd_hWnd, ctrl);
                    if (!closed)
                    {
                        Debug.WriteLine($"[{m_Context.AppName}] 완료 상태 팝업 닫기 실패 - 재시도 필요");
                        return CommonResult_AutoAllocProcess.SuccessAndReEnqueue();
                    }
                }
                else
                {
                    Debug.WriteLine($"[{m_Context.AppName}] 완료 상태 팝업 저장 성공 - 팝업 닫힘");
                }
            }
            else
            {
                // 일반 상태일 때는 기존 CloseEditPopupAsync 사용
                closed = await CloseEditPopupAsync(wnd, shouldSave: true, ctrl);
            }

            if (!closed)
            {
                Debug.WriteLine($"[{m_Context.AppName}] 팝업 닫기 실패 - 재시도 (KeyCode: {item.KeyCode})");
                return CommonResult_AutoAllocProcess.SuccessAndReEnqueue();
            }

            Debug.WriteLine($"[{m_Context.AppName}] 팝업 닫기 성공 - 상태 변경 완료");

            // 취소 상태로 전환: 비적재 (더 이상 모니터링 불필요)
            // 그 외: 재적재 (계속 모니터링)
            if (wantState == "취소")
            {
                Debug.WriteLine($"[{m_Context.AppName}] 취소 상태 전환 완료 - 큐에서 제거 (KeyCode: {item.KeyCode})");
                return CommonResult_AutoAllocProcess.SuccessAndDestroy(item);
            }
            else
            {
                Debug.WriteLine($"[{m_Context.AppName}] 상태 변경 완료 (KeyCode: {item.KeyCode})");
                return CommonResult_AutoAllocProcess.SuccessAndReEnqueue();
            }
        }

        // 모든 재시도 실패 (정상적으로는 위에서 return되므로 여기 도달 안 함)
        Debug.WriteLine($"[{m_Context.AppName}] 모든 재시도 실패 (KeyCode: {item.KeyCode})");
        return CommonResult_AutoAllocProcess.SuccessAndReEnqueue();
    }

    // 의뢰자 영역 업데이트 (고객명, 전화1, 전화2, 부서, 담당)
    private async Task<(int changeCount, StdResult_Error error)> Update의뢰자영역Async(InsungsInfo_Mem.RcptWnd_Edit wnd, TbOrder order, CancelTokenControl ctrl)
    {
        int changeCount = 0;
        StdResult_Status result = null;

        // 1. 고객명 OR 담당자명 확인 → 둘 중 하나라도 변경 시 검색
        string current고객명 = Std32Window.GetWindowCaption(wnd.의뢰자_hWnd고객명);
        string current담당 = Std32Window.GetWindowCaption(wnd.의뢰자_hWnd담당);
        bool did고객검색 = (order.CallCustName != current고객명) || (order.CallChargeName != current담당);

        if (did고객검색)
        {
            Debug.WriteLine($"[{m_Context.AppName}]   의뢰자 고객 검색 필요 (고객명 변경={order.CallCustName != current고객명}, 담당자명 변경={order.CallChargeName != current담당})");
            result = await SearchAndSelectCustomerAsync(wnd.의뢰자_hWnd고객명, wnd.의뢰자_hWnd동명, order.CallCustName, order.CallChargeName, "의뢰자", ctrl);
            if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));
            changeCount++;
            Debug.WriteLine($"[{m_Context.AppName}]   의뢰자 고객 검색 완료");
        }

        if (did고객검색)
        {
            // 2-1. 고객 검색한 경우: 관련 필드 무조건 Kai DB 데이터로 덮어쓰기
            Debug.WriteLine($"[{m_Context.AppName}]   의뢰자 관련 필드 무조건 업데이트 (Kai DB 데이터로 교체)");

            result = await WriteAndVerifyEditBoxAsync(wnd.의뢰자_hWnd전화1, order.CallTelNo ?? "", "의뢰자_전화1", ctrl);
            if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));

            result = await WriteAndVerifyEditBoxAsync(wnd.의뢰자_hWnd전화2, order.CallTelNo2 ?? "", "의뢰자_전화2", ctrl);
            if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));

            result = await WriteAndVerifyEditBoxAsync(wnd.의뢰자_hWnd부서, order.CallDeptName ?? "", "의뢰자_부서", ctrl);
            if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));

            result = await WriteAndVerifyEditBoxAsync(wnd.의뢰자_hWnd담당, order.CallChargeName ?? "", "의뢰자_담당", ctrl);
            if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));
        }
        else
        {
            // 2-2. 고객 검색 안 한 경우: 변경된 필드만 개별 업데이트
            string current전화1 = StdConvert.MakePhoneNumberToDigit(Std32Window.GetWindowCaption(wnd.의뢰자_hWnd전화1));
            string target전화1 = StdConvert.MakePhoneNumberToDigit(order.CallTelNo ?? "");
            if (target전화1 != current전화1)
            {
                Debug.WriteLine($"[{m_Context.AppName}]   의뢰자 전화1 업데이트");
                result = await WriteAndVerifyEditBoxAsync(wnd.의뢰자_hWnd전화1, order.CallTelNo ?? "", "의뢰자_전화1", ctrl);
                if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));
                changeCount++;
            }

            string current전화2 = StdConvert.MakePhoneNumberToDigit(Std32Window.GetWindowCaption(wnd.의뢰자_hWnd전화2));
            string target전화2 = StdConvert.MakePhoneNumberToDigit(order.CallTelNo2 ?? "");
            if (target전화2 != current전화2)
            {
                Debug.WriteLine($"[{m_Context.AppName}]   의뢰자 전화2 업데이트");
                result = await WriteAndVerifyEditBoxAsync(wnd.의뢰자_hWnd전화2, order.CallTelNo2 ?? "", "의뢰자_전화2", ctrl);
                if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));
                changeCount++;
            }

            string current부서 = Std32Window.GetWindowCaption(wnd.의뢰자_hWnd부서);
            if ((order.CallDeptName ?? "") != current부서)
            {
                Debug.WriteLine($"[{m_Context.AppName}]   의뢰자 부서 업데이트");
                result = await WriteAndVerifyEditBoxAsync(wnd.의뢰자_hWnd부서, order.CallDeptName ?? "", "의뢰자_부서", ctrl);
                if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));
                changeCount++;
            }

            current담당 = Std32Window.GetWindowCaption(wnd.의뢰자_hWnd담당);
            if ((order.CallChargeName ?? "") != current담당)
            {
                Debug.WriteLine($"[{m_Context.AppName}]   의뢰자 담당 업데이트");
                result = await WriteAndVerifyEditBoxAsync(wnd.의뢰자_hWnd담당, order.CallChargeName ?? "", "의뢰자_담당", ctrl);
                if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));
                changeCount++;
            }
        }

        Debug.WriteLine($"[{m_Context.AppName}]   의뢰자 영역 업데이트 완료 (변경: {changeCount}개)");
        return (changeCount, null);
    }

    // 출발지 영역 업데이트 (고객명, 동명, 전화1, 전화2, 부서, 담당, 위치)
    private async Task<(int changeCount, StdResult_Error error)> Update출발지영역Async(InsungsInfo_Mem.RcptWnd_Edit wnd, TbOrder order, CancelTokenControl ctrl)
    {
        int changeCount = 0;
        StdResult_Status result = null;

        // 1. 출발지 = 의뢰자 여부 확인
        bool is출발지Eq의뢰자 = (order.StartCustCodeK == order.CallCustCodeK);
        bool did고객검색 = false;

        if (is출발지Eq의뢰자)
        {
            // 출발지 = 의뢰자: 고객명/담당자 검색 Skip (의뢰자와 연동되므로 건드리지 않음)
            Debug.WriteLine($"[{m_Context.AppName}]   출발지 = 의뢰자: 고객 검색 Skip");
        }
        else
        {
            // 출발지 ≠ 의뢰자: 고객명 OR 담당자명 변경 확인 → 검색
            string current고객명 = Std32Window.GetWindowCaption(wnd.출발지_hWnd고객명);
            string current담당 = Std32Window.GetWindowCaption(wnd.출발지_hWnd담당);
            did고객검색 = (order.StartCustName != current고객명) || (order.StartChargeName != current담당);

            if (did고객검색)
            {
                Debug.WriteLine($"[{m_Context.AppName}]   출발지 고객 검색 필요 (고객명 변경={order.StartCustName != current고객명}, 담당자명 변경={order.StartChargeName != current담당})");
                result = await SearchAndSelectCustomerAsync(wnd.출발지_hWnd고객명, wnd.출발지_hWnd동명, order.StartCustName, order.StartChargeName, "출발지", ctrl);
                if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));
                changeCount++;
                Debug.WriteLine($"[{m_Context.AppName}]   출발지 고객 검색 완료");
            }
        }

        // 2. 필드 업데이트
        if (did고객검색)
        {
            // 2-1. 고객 검색한 경우: 관련 필드 무조건 Kai DB 데이터로 덮어쓰기
            Debug.WriteLine($"[{m_Context.AppName}]   출발지 관련 필드 무조건 업데이트 (Kai DB 데이터로 교체)");

            result = await WriteAndVerifyEditBoxAsync(wnd.출발지_hWnd동명, order.StartDongBasic ?? "", "출발지_동명", ctrl);
            if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));

            result = await WriteAndVerifyEditBoxAsync(wnd.출발지_hWnd전화1, order.StartTelNo ?? "", "출발지_전화1", ctrl);
            if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));

            result = await WriteAndVerifyEditBoxAsync(wnd.출발지_hWnd전화2, order.StartTelNo2 ?? "", "출발지_전화2", ctrl);
            if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));

            result = await WriteAndVerifyEditBoxAsync(wnd.출발지_hWnd부서, order.StartDeptName ?? "", "출발지_부서", ctrl);
            if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));

            result = await WriteAndVerifyEditBoxAsync(wnd.출발지_hWnd담당, order.StartChargeName ?? "", "출발지_담당", ctrl);
            if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));

            result = await WriteAndVerifyEditBoxAsync(wnd.출발지_hWnd위치, order.StartDetailAddr ?? "", "출발지_위치", ctrl);
            if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));
        }
        else
        {
            // 2-2. 고객 검색 안 한 경우: 변경된 필드만 개별 업데이트
            string current동명 = Std32Window.GetWindowCaption(wnd.출발지_hWnd동명);
            if ((order.StartDongBasic ?? "") != current동명)
            {
                Debug.WriteLine($"[{m_Context.AppName}]   출발지 동명 업데이트");
                result = await WriteAndVerifyEditBoxAsync(wnd.출발지_hWnd동명, order.StartDongBasic ?? "", "출발지_동명", ctrl);
                if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));
                changeCount++;
            }

            string current전화1 = StdConvert.MakePhoneNumberToDigit(Std32Window.GetWindowCaption(wnd.출발지_hWnd전화1));
            string target전화1 = StdConvert.MakePhoneNumberToDigit(order.StartTelNo ?? "");
            if (target전화1 != current전화1)
            {
                Debug.WriteLine($"[{m_Context.AppName}]   출발지 전화1 업데이트");
                result = await WriteAndVerifyEditBoxAsync(wnd.출발지_hWnd전화1, order.StartTelNo ?? "", "출발지_전화1", ctrl);
                if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));
                changeCount++;
            }

            string current전화2 = StdConvert.MakePhoneNumberToDigit(Std32Window.GetWindowCaption(wnd.출발지_hWnd전화2));
            string target전화2 = StdConvert.MakePhoneNumberToDigit(order.StartTelNo2 ?? "");
            if (target전화2 != current전화2)
            {
                Debug.WriteLine($"[{m_Context.AppName}]   출발지 전화2 업데이트");
                result = await WriteAndVerifyEditBoxAsync(wnd.출발지_hWnd전화2, order.StartTelNo2 ?? "", "출발지_전화2", ctrl);
                if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));
                changeCount++;
            }

            string current부서 = Std32Window.GetWindowCaption(wnd.출발지_hWnd부서);
            if ((order.StartDeptName ?? "") != current부서)
            {
                Debug.WriteLine($"[{m_Context.AppName}]   출발지 부서 업데이트");
                result = await WriteAndVerifyEditBoxAsync(wnd.출발지_hWnd부서, order.StartDeptName ?? "", "출발지_부서", ctrl);
                if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));
                changeCount++;
            }

            string current담당 = Std32Window.GetWindowCaption(wnd.출발지_hWnd담당);
            if ((order.StartChargeName ?? "") != current담당)
            {
                Debug.WriteLine($"[{m_Context.AppName}]   출발지 담당 업데이트");
                result = await WriteAndVerifyEditBoxAsync(wnd.출발지_hWnd담당, order.StartChargeName ?? "", "출발지_담당", ctrl);
                if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));
                changeCount++;
            }

            string current위치 = Std32Window.GetWindowCaption(wnd.출발지_hWnd위치);
            if ((order.StartDetailAddr ?? "") != current위치)
            {
                Debug.WriteLine($"[{m_Context.AppName}]   출발지 위치 업데이트");
                result = await WriteAndVerifyEditBoxAsync(wnd.출발지_hWnd위치, order.StartDetailAddr ?? "", "출발지_위치", ctrl);
                if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));
                changeCount++;
            }
        }

        Debug.WriteLine($"[{m_Context.AppName}]   출발지 영역 업데이트 완료 (변경: {changeCount}개)");
        return (changeCount, null);
    }

    // 도착지 영역 업데이트 (고객명, 동명, 전화1, 전화2, 부서, 담당, 위치)
    private async Task<(int changeCount, StdResult_Error error)> Update도착지영역Async(InsungsInfo_Mem.RcptWnd_Edit wnd, TbOrder order, CancelTokenControl ctrl)
    {
        int changeCount = 0;
        StdResult_Status result = null;

        // 1. 도착지 = 의뢰자 여부 확인
        bool is도착지Eq의뢰자 = (order.DestCustCodeK == order.CallCustCodeK);
        bool did고객검색 = false;

        if (is도착지Eq의뢰자)
        {
            // 도착지 = 의뢰자: 고객명/담당자 검색 Skip (의뢰자와 연동되므로 건드리지 않음)
            Debug.WriteLine($"[{m_Context.AppName}]   도착지 = 의뢰자: 고객 검색 Skip");
        }
        else
        {
            // 도착지 ≠ 의뢰자: 고객명 OR 담당자명 변경 확인 → 검색
            string current고객명 = Std32Window.GetWindowCaption(wnd.도착지_hWnd고객명);
            string current담당 = Std32Window.GetWindowCaption(wnd.도착지_hWnd담당);
            did고객검색 = (order.DestCustName != current고객명) || (order.DestChargeName != current담당);

            if (did고객검색)
            {
                Debug.WriteLine($"[{m_Context.AppName}]   도착지 고객 검색 필요 (고객명 변경={order.DestCustName != current고객명}, 담당자명 변경={order.DestChargeName != current담당})");
                result = await SearchAndSelectCustomerAsync(wnd.도착지_hWnd고객명, wnd.도착지_hWnd동명, order.DestCustName, order.DestChargeName, "도착지", ctrl);
                if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));
                changeCount++;
                Debug.WriteLine($"[{m_Context.AppName}]   도착지 고객 검색 완료");
            }
        }

        // 2. 필드 업데이트
        if (did고객검색)
        {
            // 2-1. 고객 검색한 경우: 관련 필드 무조건 Kai DB 데이터로 덮어쓰기
            Debug.WriteLine($"[{m_Context.AppName}]   도착지 관련 필드 무조건 업데이트 (Kai DB 데이터로 교체)");

            result = await WriteAndVerifyEditBoxAsync(wnd.도착지_hWnd동명, order.DestDongBasic ?? "", "도착지_동명", ctrl);
            if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));

            result = await WriteAndVerifyEditBoxAsync(wnd.도착지_hWnd전화1, order.DestTelNo ?? "", "도착지_전화1", ctrl);
            if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));

            result = await WriteAndVerifyEditBoxAsync(wnd.도착지_hWnd전화2, order.DestTelNo2 ?? "", "도착지_전화2", ctrl);
            if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));

            result = await WriteAndVerifyEditBoxAsync(wnd.도착지_hWnd부서, order.DestDeptName ?? "", "도착지_부서", ctrl);
            if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));

            result = await WriteAndVerifyEditBoxAsync(wnd.도착지_hWnd담당, order.DestChargeName ?? "", "도착지_담당", ctrl);
            if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));

            result = await WriteAndVerifyEditBoxAsync(wnd.도착지_hWnd위치, order.DestDetailAddr ?? "", "도착지_위치", ctrl);
            if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));
        }
        else
        {
            // 2-2. 고객 검색 안 한 경우: 변경된 필드만 개별 업데이트
            string current동명 = Std32Window.GetWindowCaption(wnd.도착지_hWnd동명);
            if ((order.DestDongBasic ?? "") != current동명)
            {
                Debug.WriteLine($"[{m_Context.AppName}]   도착지 동명 업데이트");
                result = await WriteAndVerifyEditBoxAsync(wnd.도착지_hWnd동명, order.DestDongBasic ?? "", "도착지_동명", ctrl);
                if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));
                changeCount++;
            }

            string current전화1 = StdConvert.MakePhoneNumberToDigit(Std32Window.GetWindowCaption(wnd.도착지_hWnd전화1));
            string target전화1 = StdConvert.MakePhoneNumberToDigit(order.DestTelNo ?? "");
            if (target전화1 != current전화1)
            {
                Debug.WriteLine($"[{m_Context.AppName}]   도착지 전화1 업데이트");
                result = await WriteAndVerifyEditBoxAsync(wnd.도착지_hWnd전화1, order.DestTelNo ?? "", "도착지_전화1", ctrl);
                if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));
                changeCount++;
            }

            string current전화2 = StdConvert.MakePhoneNumberToDigit(Std32Window.GetWindowCaption(wnd.도착지_hWnd전화2));
            string target전화2 = StdConvert.MakePhoneNumberToDigit(order.DestTelNo2 ?? "");
            if (target전화2 != current전화2)
            {
                Debug.WriteLine($"[{m_Context.AppName}]   도착지 전화2 업데이트");
                result = await WriteAndVerifyEditBoxAsync(wnd.도착지_hWnd전화2, order.DestTelNo2 ?? "", "도착지_전화2", ctrl);
                if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));
                changeCount++;
            }

            string current부서 = Std32Window.GetWindowCaption(wnd.도착지_hWnd부서);
            if ((order.DestDeptName ?? "") != current부서)
            {
                Debug.WriteLine($"[{m_Context.AppName}]   도착지 부서 업데이트");
                result = await WriteAndVerifyEditBoxAsync(wnd.도착지_hWnd부서, order.DestDeptName ?? "", "도착지_부서", ctrl);
                if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));
                changeCount++;
            }

            string current담당 = Std32Window.GetWindowCaption(wnd.도착지_hWnd담당);
            if ((order.DestChargeName ?? "") != current담당)
            {
                Debug.WriteLine($"[{m_Context.AppName}]   도착지 담당 업데이트");
                result = await WriteAndVerifyEditBoxAsync(wnd.도착지_hWnd담당, order.DestChargeName ?? "", "도착지_담당", ctrl);
                if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));
                changeCount++;
            }

            string current위치 = Std32Window.GetWindowCaption(wnd.도착지_hWnd위치);
            if ((order.DestDetailAddr ?? "") != current위치)
            {
                Debug.WriteLine($"[{m_Context.AppName}]   도착지 위치 업데이트");
                result = await WriteAndVerifyEditBoxAsync(wnd.도착지_hWnd위치, order.DestDetailAddr ?? "", "도착지_위치", ctrl);
                if (result.Result != StdResult.Success) return (changeCount, new StdResult_Error(result.sErr, result.sPos));
                changeCount++;
            }
        }

        Debug.WriteLine($"[{m_Context.AppName}]   도착지 영역 업데이트 완료 (변경: {changeCount}개)");
        return (changeCount, null);
    }

    // 요금 영역 업데이트 (기본요금, 추가금액, 할인금액, 탁송료)
    private async Task<(int changeCount, StdResult_Error error)> Update요금영역Async(InsungsInfo_Mem.RcptWnd_Edit wnd, TbOrder order, CancelTokenControl ctrl)
    {
        int changeCount = 0;
        StdResult_Status result = null;

        // 1. 기본요금 확인 및 업데이트
        int current기본요금 = StdConvert.StringWonFormatToInt(Std32Window.GetWindowCaption(wnd.요금그룹_hWnd기본요금));
        if (order.FeeBasic != current기본요금)
        {
            result = await ResgistAndVerify요금Async(wnd.요금그룹_hWnd기본요금, order.FeeBasic, "기본요금", ctrl);
            if (result.Result != StdResult.Success)
                return (changeCount, new StdResult_Error(result.sErr, result.sPos));
            changeCount++;
        }

        // 2. 추가금액 확인 및 업데이트
        int current추가금액 = StdConvert.StringWonFormatToInt(Std32Window.GetWindowCaption(wnd.요금그룹_hWnd추가금액));
        if (order.FeePlus != current추가금액)
        {
            result = await ResgistAndVerify요금Async(wnd.요금그룹_hWnd추가금액, order.FeePlus, "추가금액", ctrl);
            if (result.Result != StdResult.Success)
                return (changeCount, new StdResult_Error(result.sErr, result.sPos));
            changeCount++;
        }

        // 3. 할인금액 확인 및 업데이트
        int current할인금액 = StdConvert.StringWonFormatToInt(Std32Window.GetWindowCaption(wnd.요금그룹_hWnd할인금액));
        if (order.FeeMinus != current할인금액)
        {
            result = await ResgistAndVerify요금Async(wnd.요금그룹_hWnd할인금액, order.FeeMinus, "할인금액", ctrl);
            if (result.Result != StdResult.Success)
                return (changeCount, new StdResult_Error(result.sErr, result.sPos));
            changeCount++;
        }

        // 4. 탁송료 확인 및 업데이트
        int current탁송료 = StdConvert.StringWonFormatToInt(Std32Window.GetWindowCaption(wnd.요금그룹_hWnd탁송료));
        if (order.FeeConn != current탁송료)
        {
            result = await ResgistAndVerify요금Async(wnd.요금그룹_hWnd탁송료, order.FeeConn, "탁송료", ctrl);
            if (result.Result != StdResult.Success)
                return (changeCount, new StdResult_Error(result.sErr, result.sPos));
            changeCount++;
        }

        Debug.WriteLine($"[{m_Context.AppName}]   요금 영역 업데이트 완료 (변경: {changeCount}개)");
        return (changeCount, null);
    }

    // 우측상단 섹션 업데이트 (공유, 요금종류, 차량종류, 배송타입, 계산서)
    private async Task<(int changeCount, StdResult_Error error)> Update우측상단영역Async(InsungsInfo_Mem.RcptWnd_Edit wnd, TbOrder order, CancelTokenControl ctrl)
    {
        int changeCount = 0;
        StdResult_Status result = null;
        Draw.Bitmap bmpWnd = null;

        try
        {
            // 화면 캡처 (OFR 사용 위해 전체창 1회 캡처)
            bmpWnd = OfrService.CaptureScreenRect_InWndHandle(wnd.TopWnd_hWnd, 0);
            if (bmpWnd == null)
                return (0, new StdResult_Error("화면 캡처 실패", "Update우측상단영역Async_00"));

            // 1. 공유 (CheckBox - OFR)
            StdResult_NulBool resultShare = await OfrWork_Insungs.OfrImgChkValue_RectInBitmapAsync(
                bmpWnd, m_Context.FileInfo.접수등록Wnd_우측상단_공유_rcChkRelT);

            if (resultShare.bResult == null)
                return (changeCount, new StdResult_Error("공유 CheckBox 인식 실패", "Update우측상단영역Async_01"));

            bool current공유 = StdConvert.NullableBoolToBool(resultShare.bResult);
            if (order.Share != current공유)
            {
                StdResult_Error resultError = await OfrWork_Common.SetCheckBox_StatusAsync(
                    wnd.TopWnd_hWnd, m_Context.FileInfo.접수등록Wnd_우측상단_공유_rcChkRelT, order.Share, "공유");

                if (resultError != null)
                    return (changeCount, new StdResult_Error($"공유 CheckBox 변경 실패: {resultError.sErr}", resultError.sPos));
                changeCount++;
            }

            // 3. 요금종류 (RadioButton - OFR)
            Debug.WriteLine($"[{m_Context.AppName}] 요금종류 처리 시작 - order.FeeType='{order.FeeType}'");
            StdResult_NulBool resultFeeType = await IsChecked요금종류Async(bmpWnd, wnd.우측상단_btns요금종류, order.FeeType);
            Debug.WriteLine($"[{m_Context.AppName}] 요금종류 현재상태 - bResult={resultFeeType.bResult}, 일치여부={StdConvert.NullableBoolToBool(resultFeeType.bResult)}");

            if (resultFeeType.bResult == null)
                return (changeCount, new StdResult_Error("요금종류 RadioButton 인식 실패", "Update우측상단영역Async_02"));

            if (!StdConvert.NullableBoolToBool(resultFeeType.bResult))
            {
                Debug.WriteLine($"[{m_Context.AppName}] 요금종류 변경 필요 → SetGroupFeeTypeAsync 호출");
                result = await SetGroupFeeTypeAsync(bmpWnd, wnd.우측상단_btns요금종류, order.FeeType, ctrl);
                Debug.WriteLine($"[{m_Context.AppName}] 요금종류 변경 결과 - Result={result.Result}, sErr={result.sErr}");
                if (result.Result != StdResult.Success)
                    return (changeCount, new StdResult_Error($"요금종류 설정 실패: {order.FeeType}", result.sPos));
                changeCount++;
            }
            else
            {
                Debug.WriteLine($"[{m_Context.AppName}] 요금종류 이미 일치 - 변경 불필요");
            }

            // 4. 차량종류 (RadioButton - OFR, 트럭은 특별 처리)
            if (order.MovilityFlag == "트럭")
            {
                // 4-1. 트럭 RadioButton 이미 선택되어 있는지 확인
                StdResult_NulBool resultTruck = await IsChecked차량종류Async(bmpWnd, wnd.우측상단_btns차량종류, "트럭");
                if (resultTruck.bResult == null)
                    return (changeCount, new StdResult_Error("차량종류 RadioButton 인식 실패", "Update우측상단영역Async_03_트럭"));

                bool bAlreadyTruck = StdConvert.NullableBoolToBool(resultTruck.bResult);
                bool bNeedUpdate = true;  // 기본값: 업데이트 필요

                if (bAlreadyTruck)
                {
                    // 이미 트럭이면 콤보박스 현재값 OFR로 읽기
                    // 콤보박스 포커스 제거 (점선 방지)
                    await Std32Window.SetFocusWithForegroundAsync(wnd.의뢰자_hWnd의뢰자Top);
                    await Task.Delay(30);

                    // 현재 화면 다시 캡처
                    Draw.Bitmap bmpCapture = OfrService.CaptureScreenRect_InWndHandle(wnd.TopWnd_hWnd, 0);
                    if (bmpCapture != null)
                    {
                        // 차량톤수 OFR (전체)
                        var result차량 = await OfrWork_Common.OfrStr_ComplexCharSetAsync(
                            bmpCapture, m_Context.FileInfo.접수등록Wnd_우측상단_차량톤수_rcChkRelT, false, bTextSave: true, dWeight: c_dOfrWeight);
                        string current차량톤수 = result차량.strResult ?? "";

                        // 트럭상세 OFR (좌측 4문자)
                        var result트럭 = await OfrWork_Common.OfrStr_ComplexCharSetAsync(
                            bmpCapture, m_Context.FileInfo.접수등록Wnd_우측상단_트럭상세_rcChkRelT, false, bTextSave: true, dWeight: c_dOfrWeight, maxCharCount: 4);
                        string current트럭상세 = result트럭.strResult ?? "";

                        bmpCapture.Dispose();

                        // 비교: 무게와 상세가 모두 같으면 Skip (트럭상세는 OFR 4문자 기준)
                        string orderTruckDetail4 = order.TruckDetailFlag?.Length > 4
                            ? order.TruckDetailFlag.Substring(0, 4)
                            : order.TruckDetailFlag ?? "";
                        bNeedUpdate = (order.CarWeightFlag != current차량톤수) || (orderTruckDetail4 != current트럭상세);
                        Debug.WriteLine($"[{m_Context.AppName}] 트럭 현재값 비교 - 차량톤수: [{current차량톤수}] vs [{order.CarWeightFlag}], 트럭상세: [{current트럭상세}] vs [{orderTruckDetail4}], 변경필요={bNeedUpdate}");
                    }
                }

                if (bNeedUpdate)
                {
                    if (bAlreadyTruck)
                    {
                        // 이미 트럭이면 오토 클릭 먼저 (콤보박스 자동 열림 위해)
                        Debug.WriteLine($"[{m_Context.AppName}] 이미 트럭 → 오토 클릭 후 트럭 재설정");
                        int indexOto = GetCarTypeIndex("오토바이");
                        await SetCheckRadioBtn_InGroupAsync(bmpWnd, wnd.우측상단_btns차량종류, indexOto, ctrl);
                        await Task.Delay(50);
                    }

                    // 트럭: RadioButton + ComboBox 처리
                    result = await SetGroupCarTypeAsync_트럭(bmpWnd, wnd.우측상단_btns차량종류, order, ctrl);
                    if (result.Result != StdResult.Success)
                        return (changeCount, new StdResult_Error($"트럭 설정 실패", result.sPos));
                    changeCount++;
                }
                else
                {
                    Debug.WriteLine($"[{m_Context.AppName}] 트럭 설정 Skip (현재값 = 원하는값)");
                }
            }
            else
            {
                // 일반 차량
                StdResult_NulBool resultCarType = await IsChecked차량종류Async(bmpWnd, wnd.우측상단_btns차량종류, order.MovilityFlag);

                if (resultCarType.bResult == null)
                    return (changeCount, new StdResult_Error("차량종류 RadioButton 인식 실패", "Update우측상단영역Async_03"));

                if (!StdConvert.NullableBoolToBool(resultCarType.bResult))
                {
                    int index = GetCarTypeIndex(order.MovilityFlag);
                    if (index == -1)
                        return (changeCount, new StdResult_Error($"지원하지 않는 차량종류: {order.MovilityFlag}", "Update우측상단영역Async_03_1"));

                    result = await SetCheckRadioBtn_InGroupAsync(bmpWnd, wnd.우측상단_btns차량종류, index, ctrl);
                    if (result.Result != StdResult.Success)
                        return (changeCount, new StdResult_Error($"차량종류 설정 실패: {order.MovilityFlag}", result.sPos));
                    changeCount++;
                }
            }

            // 5. 배송타입 (RadioButton - OFR)
            StdResult_NulBool resultDeliverType = await IsChecked배송타입Async(bmpWnd, wnd.우측상단_btns배송종류, order.DeliverFlag);

            if (resultDeliverType.bResult == null)
                return (changeCount, new StdResult_Error("배송타입 RadioButton 인식 실패", "Update우측상단영역Async_04"));

            if (!StdConvert.NullableBoolToBool(resultDeliverType.bResult))
            {
                result = await SetGroupDeliverTypeAsync(bmpWnd, wnd.우측상단_btns배송종류, order.DeliverFlag, ctrl);
                if (result.Result != StdResult.Success)
                    return (changeCount, new StdResult_Error($"배송타입 설정 실패: {order.DeliverFlag}", result.sPos));
                changeCount++;
            }

            // 6. 계산서 (CheckBox - OFR)
            StdResult_NulBool resultTaxBill = await OfrWork_Insungs.OfrImgChkValue_RectInBitmapAsync(
                bmpWnd, m_Context.FileInfo.접수등록Wnd_우측상단_계산서_rcChkRelT);

            if (resultTaxBill.bResult == null)
                return (changeCount, new StdResult_Error("계산서 CheckBox 인식 실패", "Update우측상단영역Async_05"));

            bool current계산서 = StdConvert.NullableBoolToBool(resultTaxBill.bResult);
            if (order.TaxBill != current계산서)
            {
                StdResult_Error resultError = await OfrWork_Common.SetCheckBox_StatusAsync(
                    wnd.TopWnd_hWnd, m_Context.FileInfo.접수등록Wnd_우측상단_계산서_rcChkRelT, order.TaxBill, "계산서");

                if (resultError != null)
                    return (changeCount, new StdResult_Error($"계산서 CheckBox 변경 실패: {resultError.sErr}", resultError.sPos));
                changeCount++;
            }

            Debug.WriteLine($"[{m_Context.AppName}]   우측상단 섹션 업데이트 완료 (변경: {changeCount}개)");
            return (changeCount, null);
        }
        finally
        {
            bmpWnd?.Dispose(); // 메모리 해제
        }
    }

    // 오더메모 영역 업데이트 (KeyCode/DeliverMemo 형식)
    private async Task<(int changeCount, StdResult_Error error)> Update오더메모영역Async(InsungsInfo_Mem.RcptWnd_Edit wnd, TbOrder order, CancelTokenControl ctrl)
    {
        int changeCount = 0;
        StdResult_Status result = null;

        // 오더메모 형식: "{KeyCode}/{OrderMemo}"
        string target오더메모 = $"{order.KeyCode}/{order.DeliverMemo}";
        string current오더메모 = Std32Window.GetWindowCaption(wnd.우측하단_hWnd오더메모);

        if (target오더메모 != current오더메모)
        {
            result = await WriteAndVerifyEditBoxAsync(wnd.우측하단_hWnd오더메모, target오더메모, "오더메모", ctrl);
            if (result.Result != StdResult.Success)
                return (changeCount, new StdResult_Error(result.sErr, result.sPos));
            changeCount++;
        }

        Debug.WriteLine($"[{m_Context.AppName}]   오더메모 영역 업데이트 완료 (변경: {changeCount}개)");
        return (changeCount, null);
    }

    // 검색
    // 기사 정보 읽기용 팝업 열기 (DG 더블클릭 → 3초 대기 → 닫기)
    public async Task<StdResult_Status> OpenReadPopupAsync(int rowIndex, AutoAllocModel item, CancelTokenControl ctrl)
    {
        try
        {
            await ctrl.WaitIfPausedOrCancelledAsync();

            // 1. DG 로우 더블클릭 (헤더 2줄 추가)
            int selIndex = rowIndex + 2;
            Draw.Point ptRel = StdUtil.GetCenterDrawPoint(m_RcptPage.DG오더_RelChildRects[c_nCol번호, selIndex]);

            Debug.WriteLine($"[{m_Context.AppName}] 기사정보 읽기용 팝업 열기 시도: rowIndex={rowIndex}, selIndex={selIndex}, ptRel={ptRel}");

            // 2. 팝업창 찾기 (최대 c_nRepeatVeryShort번 시도)
            bool bFind = false;
            IntPtr hWndPopup = IntPtr.Zero;

            for (int j = 0; j < CommonVars.c_nRepeatVeryShort; j++)
            {
                await ctrl.WaitIfPausedOrCancelledAsync();

                // 더블클릭 (SendMessage 방식, BlockInput 사용)
                //await Simulation_Mouse.SafeMouseSend_DblClickLeft_ptRelAsync(m_RcptPage.DG오더_hWnd, ptRel);
                Debug.WriteLine($"[{m_Context.AppName}] 더블클릭 실행 (시도 {j + 1}/{CommonVars.c_nRepeatVeryShort})");

                // 팝업창 나타날 때까지 대기 (최대 5초)
                for (int k = 0; k < 100; k++)
                {
                    await Task.Delay(50, ctrl.Token);

                    // 팝업창 찾기
                    hWndPopup = Std32Window.FindStartWithMainWindow_NotTransparent(m_MemInfo.Splash.TopWnd_uProcessId, m_FileInfo.접수등록Wnd_TopWnd_sWndStartsWith);
                    if (hWndPopup == IntPtr.Zero) continue;

                    // 닫기 버튼으로 팝업 검증
                    IntPtr hWndClose = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_신규버튼그룹_닫기_ptChkRelT);
                    if (hWndClose == IntPtr.Zero) continue;

                    string closeBtnText = Std32Window.GetWindowCaption(hWndClose);
                    if (closeBtnText.StartsWith(m_FileInfo.접수등록Wnd_공용버튼그룹_닫기_sWndName))
                    {
                        bFind = true;
                        Debug.WriteLine($"[{m_Context.AppName}] 팝업창 찾음: hWnd={hWndPopup:X}, 닫기버튼={closeBtnText}");
                        break;
                    }
                }

                if (bFind) break;
                await Task.Delay(100, ctrl.Token);
            }

            if (!bFind)
            {
                Debug.WriteLine($"[{m_Context.AppName}] 팝업창 찾기 실패");
                return new StdResult_Status(StdResult.Fail, "DG 더블클릭 후 팝업창이 열리지 않음", "OpenReadPopupAsync_01");
            }

            await Task.Delay(100, ctrl.Token);

            // 3. RcptWnd_Edit 생성
            var wndRcpt = new InsungsInfo_Mem.RcptWnd_Edit(hWndPopup, m_FileInfo);
            Debug.WriteLine($"[{m_Context.AppName}] RcptWnd_Edit 생성 완료");

            // 4. Phase 2: 기사 정보 OFR
            Debug.WriteLine($"[{m_Context.AppName}] 기사 정보 OFR 시작");

            // 4-1. 기사번호 OFR (단음소)
            Draw.Rectangle rectDriverId = m_FileInfo.접수등록Wnd_기사그룹_기사번호_rcChkRelT;
            Draw.Bitmap bmpDriverId = OfrService.CaptureScreenRect_InWndHandle(hWndPopup, rectDriverId);
            StdResult_String resultDriverId = await OfrWork_Common.OfrStr_SeqCharAsync(bmpDriverId, c_dOfrWeight); // 영역추출 못할시 가중치조정
            string driverId = resultDriverId.strResult ?? "";
            bmpDriverId?.Dispose();
            Debug.WriteLine($"[{m_Context.AppName}] 기사번호 OFR: '{driverId}'");

            // 4-2. 기사이름 OFR (다음소)
            Draw.Rectangle rectDriverName = m_FileInfo.접수등록Wnd_기사그룹_기사이름_rcChkRelT;
            Draw.Bitmap bmpDriverName = OfrService.CaptureScreenRect_InWndHandle(hWndPopup, rectDriverName);
            StdResult_String resultDriverName = await OfrWork_Common.OfrStr_ComplexCharSetAsync(bmpDriverName, bTextSave: true, dWeight: c_dOfrWeight);
            string driverName = resultDriverName.strResult ?? "";
            bmpDriverName?.Dispose();
            Debug.WriteLine($"[{m_Context.AppName}] 기사이름 OFR: '{driverName}'");

            // 4-3. 기사소속 OFR (다음소)
            Draw.Rectangle rectDriverCenter = m_FileInfo.접수등록Wnd_기사그룹_기사소속_rcChkRelT;
            Draw.Bitmap bmpDriverCenter = OfrService.CaptureScreenRect_InWndHandle(hWndPopup, rectDriverCenter);
            StdResult_String resultDriverCenter = await OfrWork_Common.OfrStr_ComplexCharSetAsync(bmpDriverCenter, bTextSave: true, dWeight: c_dOfrWeight);
            string driverCenter = resultDriverCenter.strResult ?? "";
            bmpDriverCenter?.Dispose();
            Debug.WriteLine($"[{m_Context.AppName}] 기사소속 OFR: '{driverCenter}'");

            // 4-4. item.NewOrder에 저장
            item.NewOrder.DriverId = driverId;
            item.NewOrder.DriverName = driverName;
            item.NewOrder.DriverCenterName = driverCenter;
            item.NewOrder.DriverTelNo = StdConvert.MakePhoneNumberToDigit(item.DriverPhone);
            item.NewOrder.OrderState = "운행";
            // item.DriverPhone은 이미 DG에서 읽어서 저장됨

            //Debug.WriteLine($"[{m_Context.AppName}] ===== 기사 정보 OFR 완료 =====");
            //Debug.WriteLine($"  기사번호: '{driverId}'");
            //Debug.WriteLine($"  기사이름: '{driverName}'");
            //Debug.WriteLine($"  기사소속: '{driverCenter}'");
            //Debug.WriteLine($"  기사전번: '{item.DriverPhone}' (DG에서 획득)");

            // 5. 팝업 닫기 (변경사항 없으므로 shouldSave: false)
            Debug.WriteLine($"[{m_Context.AppName}] 팝업 닫기 시작");
            bool bClosed = await CloseEditPopupAsync(wndRcpt, shouldSave: false, ctrl);

            if (!bClosed)
            {
                Debug.WriteLine($"[{m_Context.AppName}] 팝업 닫기 실패");
                return new StdResult_Status(StdResult.Fail, "팝업 닫기 실패", "OpenReadPopupAsync_CloseError");
            }

            Debug.WriteLine($"[{m_Context.AppName}] 기사정보 읽기용 팝업 처리 완료");
            return new StdResult_Status(StdResult.Success, "", "");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{m_Context.AppName}] OpenReadPopupAsync 예외: {ex.Message}");
            return new StdResult_Status(StdResult.Fail, StdUtil.GetExceptionMessage(ex), "OpenReadPopupAsync_999");
        }
    }

    // 
    // 고객 검색 및 선택 (의뢰자, 출발지, 도착지 공통 - 입력 검증, 예외 처리, CancelToken 지원)
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

    // 고객 검색 결과 타입 확인 (Enter 키 전송 후 결과 확인)
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
            await Std32Key_Msg.KeyPost_DownAsync(hWnd고객명, StdCommon32.VK_RETURN);
            Debug.WriteLine($"[{m_Context.AppName}] 고객 검색 Enter 키 전송");

            // 2. 검색 결과 확인 (최대 50번, 1.5초)
            for (int j = 0; j < CommonVars.c_nRepeatShort; j++)
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

                    for (int k = 0; k < CommonVars.c_nRepeatVeryShort * 3; k++)  // 30회 (10*3)
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
            Debug.WriteLine($"[{m_Context.AppName}] GetCustSearchTypeAsync 실패: 타임아웃 (최대 {CommonVars.c_nRepeatShort}회 시도)");
            return new CommonResult_SearchType(CEnum_CustSearchCount.Null, IntPtr.Zero);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{m_Context.AppName}] GetCustSearchTypeAsync 예외: {ex.Message}");
            return new CommonResult_SearchType(CEnum_CustSearchCount.Null, IntPtr.Zero);
        }
    }

    // 닫기
    // 팝업 닫기 (저장 또는 취소)
    private async Task<bool> CloseEditPopupAsync(InsungsInfo_Mem.RcptWnd_Edit wnd, bool shouldSave, CancelTokenControl ctrl)
    {
        try
        {
            await ctrl.WaitIfPausedOrCancelledAsync();

            bool bClosed;

            if (shouldSave)
            {
                // 변경사항이 있으니 저장
                Debug.WriteLine($"[{m_Context.AppName}] 변경내용이 있습니다 - 저장 버튼 클릭");
                await Task.Delay(CommonVars.c_nWaitNormal, ctrl.Token);

                bClosed = await ClickNWaitWindowChangedAsync_OrFind확인창(wnd.Btn_hWnd저장, wnd.TopWnd_hWnd, ctrl);

                if (!bClosed)
                {
                    Debug.WriteLine($"[{m_Context.AppName}] 저장 실패 (확인창 나타남) - 확인창 닫기 시도");

                    // 1. 확인창 닫기
                    bool confirmClosed = await CloseConfirmWindowAsync(ctrl);
                    if (confirmClosed)
                    {
                        Debug.WriteLine($"[{m_Context.AppName}] 확인창 닫기 성공");
                    }
                    else
                    {
                        Debug.WriteLine($"[{m_Context.AppName}] 확인창 닫기 실패 또는 확인창 없음");
                    }

                    // 2. 팝업 닫기 버튼 클릭
                    await Task.Delay(CommonVars.c_nWaitShort, ctrl.Token);
                    bClosed = await ClickNWaitWindowChangedAsync(wnd.Btn_hWnd닫기, wnd.TopWnd_hWnd, ctrl);

                    if (bClosed)
                    {
                        Debug.WriteLine($"[{m_Context.AppName}] 저장 실패 후 팝업 닫기 성공 - 재시도 필요");
                    }
                    else
                    {
                        Debug.WriteLine($"[{m_Context.AppName}] 저장 실패 후 팝업 닫기도 실패");
                    }

                    // 3. 재시도를 위해 false 반환
                    return false;
                }

                Debug.WriteLine($"[{m_Context.AppName}] 저장 성공 - 팝업 닫힘");
            }
            else
            {
                // 변경사항이 없으니 그냥 닫기
                Debug.WriteLine($"[{m_Context.AppName}] 변경내용이 없습니다 - 닫기 버튼 클릭");
                Debug.WriteLine($"[{m_Context.AppName}] 닫기 버튼 핸들: {wnd.Btn_hWnd닫기:X}, 팝업 핸들: {wnd.TopWnd_hWnd:X}");

                Debug.WriteLine($"[{m_Context.AppName}] ClickNWaitWindowChangedAsync 호출 전");
                bClosed = await ClickNWaitWindowChangedAsync(wnd.Btn_hWnd닫기, wnd.TopWnd_hWnd, ctrl);
                Debug.WriteLine($"[{m_Context.AppName}] ClickNWaitWindowChangedAsync 호출 후: bClosed={bClosed}");

                if (!bClosed)
                {
                    Debug.WriteLine($"[{m_Context.AppName}] 닫기 실패 (윈도우 안 닫힘)");
                    return false;
                }

                Debug.WriteLine($"[{m_Context.AppName}] 닫기 성공 - 팝업 닫힘");
            }

            await Task.Delay(CommonVars.c_nWaitNormal, ctrl.Token); // 확실히 대기
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{m_Context.AppName}] CloseEditPopupAsync 예외: {ex.Message}");
            return false;
        }
    }
    #endregion

    #region 6. Datagrid - 데이터그리드 관련

    // Datagrid 상태 검증 (컬럼 개수, 순서, 너비 확인) → 검증 이슈 플래그 반환 (None이면 정상)
    private CEnum_DgValidationIssue ValidateDatagridState(string[] columnTexts, List<OfrModel_LeftWidth> listLW)
    {
        CEnum_DgValidationIssue issues = CEnum_DgValidationIssue.None;

        // 1. 컬럼 개수 체크
        if (columnTexts == null || columnTexts.Length != m_ReceiptDgHeaderInfos.Length)
        {
            issues |= CEnum_DgValidationIssue.InvalidColumnCount;
            Debug.WriteLine($"[{m_Context.AppName}] ValidateDatagridState - 컬럼 개수 불일치: 실제={columnTexts?.Length}, 예상={m_ReceiptDgHeaderInfos.Length}");
            return issues;
        }

        // 2. 컬럼 너비 확인 (물리적 속성 우선)
        for (int x = 0; x < m_ReceiptDgHeaderInfos.Length; x++)
        {
            int actualWidth = listLW[x].nWidth;
            int expectedWidth = m_ReceiptDgHeaderInfos[x].nWidth;
            int widthDiff = Math.Abs(actualWidth - expectedWidth);

            if (widthDiff > COLUMN_WIDTH_TOLERANCE)
            {
                issues |= CEnum_DgValidationIssue.WrongWidth;
                Debug.WriteLine($"[{m_Context.AppName}] ValidateDatagridState - 컬럼 너비 불일치[{x}]: 실제={actualWidth}, 예상={expectedWidth}");
            }
        }

        // 3. 컬럼 명칭 및 순서 확인
        for (int x = 0; x < columnTexts.Length; x++)
        {
            string columnText = columnTexts[x];

            // 3-1. 컬럼명이 유효한지
            int index = Array.FindIndex(m_ReceiptDgHeaderInfos, h => h.sName == columnText);

            if (index < 0)
            {
                issues |= CEnum_DgValidationIssue.InvalidColumn;
                Debug.WriteLine($"[{m_Context.AppName}] ValidateDatagridState - 유효하지 않은 컬럼[{x}]: '{columnText}'");
                continue;
            }

            // 3-2. 컬럼 순서 확인
            if (index != x)
            {
                issues |= CEnum_DgValidationIssue.WrongOrder;
                Debug.WriteLine($"[{m_Context.AppName}] ValidateDatagridState - 컬럼 순서 불일치[{x}]: '{columnText}' (예상={index})");
            }
        }

        if (issues != CEnum_DgValidationIssue.None)
        {
            Debug.WriteLine($"[!!!] ValidateDatagridState 검증 실패: {issues}");
        }
        else
        {
            Debug.WriteLine($"[*] ValidateDatagridState 검증 통과 (정상)");
        }

        return issues;
    }

    // 너비 검증 실패 시 상세 로그 출력 보강 (for 루프 내부 수정)

    public async Task<StdResult_Int> GetValidRowCountAsync(Draw.Bitmap bmpPage)
    {
        await Task.CompletedTask;

        Draw.Rectangle[,] rects = m_RcptPage.DG오더_RelChildRects;
        int nBackgroundBright = m_RcptPage.DG오더_nBackgroundBright;

        if (rects == null)
            return new StdResult_Int(0, "RelChildRects가 null입니다", "GetValidRowCountAsync_01");

        if (nBackgroundBright <= 0 || nBackgroundBright > 255)
            return new StdResult_Int(0, $"배경 밝기 값 유효하지 않음: {nBackgroundBright}", "GetValidRowCountAsync_02");

        int nThreshold = nBackgroundBright - 1;
        int nValidRows = 0;

        for (int y = 2; y < rects.GetLength(1); y++)
        {
            int nCurBright = OfrService.GetPixelBrightnessFrmWndHandle(m_RcptPage.DG오더_hWnd, rects[0, y].Right + 1, rects[0, y].Top + 6);

            if (nCurBright < nThreshold)
                nValidRows++;
            else
                break;
        }

        return new StdResult_Int(nValidRows);
    }

    public async Task<StdResult_String> GetRowSeqnoAsync(Draw.Bitmap bmpPage, Draw.Rectangle rectSeqno, bool bInvertRgb, CancelTokenControl ctrl)
    {
        await ctrl.WaitIfPausedOrCancelledAsync();
        return await OfrWork_Common.OfrStr_SeqCharAsync(bmpPage, rectSeqno, bInvertRgb, c_dOfrWeight);
    }

    public async Task<StdResult_String> GetRowStatusAsync(Draw.Bitmap bmpPage, Draw.Rectangle rectStatus, bool bInvertRgb, bool bText, CancelTokenControl ctrl)
    {
        await ctrl.WaitIfPausedOrCancelledAsync();
        return await OfrWork_Common.OfrStr_ComplexCharSetAsync(bmpPage, rectStatus, bInvertRgb, bText, dWeight: c_dOfrWeight);
    }

    public async Task<StdResult_String> GetRowDriverPhNoAsync(Draw.Bitmap bmpPage, Draw.Rectangle rectDriverPhNo, bool bInvertRgb, CancelTokenControl ctrl)
    {
        await ctrl.WaitIfPausedOrCancelledAsync();
        return await OfrWork_Common.OfrStr_SeqCharAsync(bmpPage, rectDriverPhNo, bInvertRgb, c_dOfrWeight);
    }

    public static int GetExpectedFirstRowNum(int nTotRows, int pageIdx)
    {
        int nRowsPerPage = InsungsInfo_File.접수등록Page_DG오더_dataRowCount;

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

        if (nTotRows % nRowsPerPage == 0) return nNum;
        else return nNum - nRowsPerPage + (nTotRows % nRowsPerPage);
    }

    public async Task<int> ReadFirstRowNumAsync(bool bEdit = false)
    {
        IntPtr hWndDG = m_RcptPage.DG오더_hWnd;
        Draw.Rectangle[,] rects = m_RcptPage.DG오더_RelChildRects;
        int firstNum = -1;

        for (int y = 2; y < 2 + InsungsInfo_File.접수등록Page_DG오더_dataRowCount; y++)
        {
            Draw.Rectangle rcNo = rects[0, y];
            Draw.Bitmap bmpNo = OfrService.CaptureScreenRect_InWndHandle(hWndDG, rcNo);
            if (bmpNo == null) continue;

            StdResult_String resultNo = await OfrWork_Common.OfrStr_SeqCharAsync(bmpNo, c_dOfrWeight, bEdit);
            bmpNo.Dispose();

            if (!string.IsNullOrEmpty(resultNo.strResult))
            {
                int curNum = StdConvert.StringToInt(resultNo.strResult, -1);
                if (curNum >= 1)
                {
                    firstNum = curNum - (y - 2);
                    break;
                }
            }
        }

        return firstNum;
    }

    public async Task<StdResult_Status> VerifyAndAdjustPageAsync(int nExpectedFirstNum, CancelTokenControl ctrl, int nRetryCount = CommonVars.c_nRepeatShort)
    {
        for (int retry = 0; retry < nRetryCount; retry++)
        {
            await ctrl.WaitIfPausedOrCancelledAsync();

            int nActualFirstNum = await ReadFirstRowNumAsync(bEdit: true);

            if (nExpectedFirstNum == nActualFirstNum)
            {
                return new StdResult_Status(StdResult.Success);
            }

            Debug.WriteLine($"[{m_Context.AppName}] VerifyAndAdjustPageAsync - 페이지 검증 실패 (시도 {retry + 1}/{nRetryCount}, 예상={nExpectedFirstNum}, 실제={nActualFirstNum})");

            if (retry < nRetryCount - 1)
            {
                int diff = nActualFirstNum - nExpectedFirstNum;
                int absDiff = Math.Abs(diff);
                int dataRowCount = InsungsInfo_File.접수등록Page_DG오더_dataRowCount;

                int pageClicks = absDiff / dataRowCount;
                int rowClicks = absDiff % dataRowCount;

                bool bReverse = false;
                if (rowClicks > dataRowCount / 2)
                {
                    pageClicks += 1;
                    rowClicks = dataRowCount - rowClicks;
                    bReverse = true;
                }

                Draw.Point ptPage, ptRow;
                if ((diff > 0 && !bReverse) || (diff < 0 && bReverse))
                {
                    ptPage = m_FileInfo.접수등록Page_DG오더_페이지Up_ptClkRelL;
                    ptRow = m_FileInfo.접수등록Page_DG오더_버튼Up_ptClkRelL;
                }
                else
                {
                    ptPage = m_FileInfo.접수등록Page_DG오더_페이지Down_ptClkRelL;
                    ptRow = m_FileInfo.접수등록Page_DG오더_버튼Down_ptClkRelL;
                }

                for (int i = 0; i < pageClicks; i++)
                {
                    await Std32Mouse_Post.MousePostAsync_ClickLeft_ptRel(m_RcptPage.DG오더_hWnd수직스크롤, ptPage);
                }

                for (int i = 0; i < rowClicks; i++)
                {
                    await Std32Mouse_Post.MousePostAsync_ClickLeft_ptRel(m_RcptPage.DG오더_hWnd수직스크롤, ptRow);
                }

                await Task.Delay(CommonVars.c_nWaitNormal, ctrl.Token);
            }
        }

        return new StdResult_Status(StdResult.Fail, $"페이지 조정 {nRetryCount}회 모두 실패", "VerifyAndAdjustPageAsync");
    }

    

    private async Task<(InsungsInfo_Mem.RcptWnd_Edit wnd, StdResult_Error error)> OpenEditPopupAsync(int rowIndex, CancelTokenControl ctrl)
    {
        try
        {
            await ctrl.WaitIfPausedOrCancelledAsync();

            int selIndex = rowIndex + 2;
            Draw.Point ptRel = StdUtil.GetCenterDrawPoint(m_RcptPage.DG오더_RelChildRects[c_nColForClick, selIndex]);

            Debug.WriteLine($"[{m_Context.AppName}] OpenEditPopupAsync - 팝업 열기 시도 (rowIndex={rowIndex})");

            bool bFind = false;
            IntPtr hWndPopup = IntPtr.Zero;

            for (int j = 0; j < CommonVars.c_nRepeatVeryShort; j++)
            {
                await ctrl.WaitIfPausedOrCancelledAsync();

                // 더블클릭 (BlockInput 사용)
                await Simulation_Mouse.SafeMouseSend_DblClickLeft_ptRelAsync(m_RcptPage.DG오더_hWnd, ptRel);
                Debug.WriteLine($"[{m_Context.AppName}] 더블클릭 실행 (시도 {j + 1}/{CommonVars.c_nRepeatVeryShort})");

                for (int k = 0; k < 100; k++)
                {
                    await Task.Delay(50, ctrl.Token);

                    hWndPopup = Std32Window.FindStartWithMainWindow_NotTransparent(m_MemInfo.Splash.TopWnd_uProcessId, m_FileInfo.접수등록Wnd_TopWnd_sWndStartsWith);
                    if (hWndPopup == IntPtr.Zero) continue;

                    IntPtr hWndClose = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_신규버튼그룹_닫기_ptChkRelT);
                    if (hWndClose == IntPtr.Zero) continue;

                    string closeBtnText = Std32Window.GetWindowCaption(hWndClose);
                    if (closeBtnText.StartsWith(m_FileInfo.접수등록Wnd_공용버튼그룹_닫기_sWndName))
                    {
                        bFind = true;
                        Debug.WriteLine($"[{m_Context.AppName}] 팝업창 찾음: hWnd={hWndPopup:X}");
                        break;
                    }
                }

                if (bFind) break;
                await Task.Delay(100, ctrl.Token);
            }

            if (!bFind)
            {
                return (null, new StdResult_Error($"[{m_Context.AppName}] 주문번호 클릭 후 팝업창이 열리지 않음", "OpenEditPopupAsync_01"));
            }

            await Task.Delay(100, ctrl.Token);
            var wndRcpt = new InsungsInfo_Mem.RcptWnd_Edit(hWndPopup, m_FileInfo);

            return (wndRcpt, null);
        }
        catch (Exception ex)
        {
            return (null, new StdResult_Error(StdUtil.GetExceptionMessage(ex), "OpenEditPopupAsync_999"));
        }
    }

    private async Task<bool> ClickFirstRowAsync(CancelTokenControl ctrl, int retryCount = 3)
    {
        try
        {
            Draw.Rectangle rectClickCell = m_RcptPage.DG오더_RelChildRects[c_nColForClick, 2];
            Draw.Rectangle rectVerifyCell = m_RcptPage.DG오더_RelChildRects[1, 2];

            for (int i = 1; i <= retryCount; i++)
            {
                await ctrl.WaitIfPausedOrCancelledAsync();
                Debug.WriteLine($"[{m_Context.AppName}] ClickFirstRowAsync - 첫 로우 클릭 시도 {i}/{retryCount}");

                Draw.Point ptRelDG = StdUtil.GetDrawPoint(rectClickCell, 3, 3);
                await Std32Mouse_Post.MousePostAsync_ClickLeft_ptRel(m_RcptPage.DG오더_hWnd, ptRelDG);
                await Task.Delay(200, ctrl.Token);

                Draw.Bitmap bmpCell = OfrService.CaptureScreenRect_InWndHandle(m_RcptPage.DG오더_hWnd, rectVerifyCell);
                if (bmpCell == null)
                {
                    if (i < retryCount) await Task.Delay(200, ctrl.Token);
                    continue;
                }

                try
                {
                    Draw.Rectangle rectInBitmap = new Draw.Rectangle(0, 0, rectVerifyCell.Width, rectVerifyCell.Height);
                    bool isSelected = OfrService.IsInvertedSelection(bmpCell, rectInBitmap);

                    if (isSelected)
                    {
                        Debug.WriteLine($"[{m_Context.AppName}] ClickFirstRowAsync - 첫 로우 선택 성공");
                        return true;
                    }
                }
                finally
                {
                    bmpCell?.Dispose();
                }

                if (i < retryCount) await Task.Delay(200, ctrl.Token);
            }

            Debug.WriteLine($"[{m_Context.AppName}] ClickFirstRowAsync - 첫 로우 선택 검증 실패");
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{m_Context.AppName}] ClickFirstRowAsync 예외: {ex.Message}");
            return false;
        }
    }

    private async Task<StdResult_String> GetSeqnoAsync(int rowIndex, bool bInvertRgb, CancelTokenControl ctrl, int retryCount = 3)
    {
        try
        {
            int seqnoColIndex = c_nCol주문번호;
            int yIndex = rowIndex + 2;

            for (int i = 1; i <= retryCount; i++)
            {
                await ctrl.WaitIfPausedOrCancelledAsync();

                Draw.Rectangle rectSeqnoCell = m_RcptPage.DG오더_RelChildRects[seqnoColIndex, yIndex];
                Draw.Bitmap bmpCell = OfrService.CaptureScreenRect_InWndHandle(m_RcptPage.DG오더_hWnd, rectSeqnoCell);

                if (bmpCell == null)
                {
                    if (i < retryCount) await Task.Delay(200, ctrl.Token);
                    continue;
                }

                Draw.Bitmap bmpForOcr = null;
                try
                {
                    if (bInvertRgb)
                    {
                        bmpForOcr = OfrService.InvertBitmap(bmpCell);
                        bmpCell.Dispose();
                    }
                    else
                    {
                        bmpForOcr = bmpCell;
                    }

                    StdResult_String resultSeqno = await OfrWork_Common.OfrStr_SeqCharAsync(bmpForOcr, c_dOfrWeight);

                    if (!string.IsNullOrEmpty(resultSeqno.strResult))
                    {
                        Debug.WriteLine($"[{m_Context.AppName}] GetSeqnoAsync - Seqno 획득 성공: '{resultSeqno.strResult}'");
                        return new StdResult_String(resultSeqno.strResult);
                    }
                }
                finally
                {
                    if (bInvertRgb) bmpForOcr?.Dispose();
                    else bmpForOcr?.Dispose(); // or keep the reference and dispose after
                }

                if (i < retryCount) await Task.Delay(200, ctrl.Token);
            }

            return new StdResult_String($"Seqno OFR 실패 ({retryCount}회 시도)", "GetSeqnoAsync_99");
        }
        catch (Exception ex)
        {
            return new StdResult_String(StdUtil.GetExceptionMessage(ex), "GetSeqnoAsync_999");
        }
    }

    //private async Task<StdResult_String> GetDriverPhNoAsync(int rowIndex, bool bInvertRgb, CancelTokenControl ctrl, int retryCount = 3)
    //{
    //    try
    //    {
    //        int driverPhNoColIndex = c_nCol기사전번;
    //        int yIndex = rowIndex + 2;

    //        for (int i = 1; i <= retryCount; i++)
    //        {
    //            await ctrl.WaitIfPausedOrCancelledAsync();

    //            Draw.Rectangle rectCell = m_RcptPage.DG오더_RelChildRects[driverPhNoColIndex, yIndex];
    //            Draw.Bitmap bmpCell = OfrService.CaptureScreenRect_InWndHandle(m_RcptPage.DG오더_hWnd, rectCell);

    //            if (bmpCell == null)
    //            {
    //                if (i < retryCount) await Task.Delay(200, ctrl.Token);
    //                continue;
    //            }

    //            Draw.Bitmap bmpForOcr = null;
    //            try
    //            {
    //                if (bInvertRgb)
    //                {
    //                    bmpForOcr = OfrService.InvertBitmap(bmpCell);
    //                    bmpCell.Dispose();
    //                }
    //                else
    //                {
    //                    bmpForOcr = bmpCell;
    //                }

    //                StdResult_String resultDriverPhNo = await OfrWork_Common.OfrStr_SeqCharAsync(bmpForOcr, c_dOfrWeight);

    //                if (!string.IsNullOrEmpty(resultDriverPhNo.strResult))
    //                {
    //                    Debug.WriteLine($"[{m_Context.AppName}] GetDriverPhNoAsync - 기사전번 획득 성공: '{resultDriverPhNo.strResult}'");
    //                    return new StdResult_String(resultDriverPhNo.strResult);
    //                }
    //            }
    //            finally
    //            {
    //                if (bInvertRgb) bmpForOcr?.Dispose();
    //                else bmpForOcr?.Dispose();
    //            }

    //            if (i < retryCount) await Task.Delay(200, ctrl.Token);
    //        }

    //        return new StdResult_String($"기사전번 OFR 실패 ({retryCount}회 시도)", "GetDriverPhNoAsync_99");
    //    }
    //    catch (Exception ex)
    //    {
    //        return new StdResult_String(StdUtil.GetExceptionMessage(ex), "GetDriverPhNoAsync_999");
    //    }
    //}

    //     // 셀 영역을 화면에 그려서 시각화 (테스트용)
    //     private void Test_DrawCellRects(int columns, int rowCount, int startRow = 0)
    //     {
    //         try
    //         {
    //             Debug.WriteLine($"[{m_Context.AppName}] Test_DrawCellRects 시작");
    // 
    //             IntPtr hWndDG = m_RcptPage.DG오더_hWnd;
    //             if (hWndDG == IntPtr.Zero)
    //             {
    //                 System.Windows.MessageBox.Show("DG오더_hWnd가 초기화되지 않았습니다.", "오류");
    //                 return;
    //             }
    // 
    //             Draw.Rectangle[,] rects = m_RcptPage.DG오더_RelChildRects;
    //             if (rects == null)
    //             {
    //                 System.Windows.MessageBox.Show("DG오더_RelChildRects가 초기화되지 않았습니다.", "오류");
    //                 return;
    //             }
    // 
    //             Debug.WriteLine($"[{m_Context.AppName}] Cell 배열: {columns}열 x {rowCount}행 (startRow={startRow})");
    // 
    //             // TransparantWnd 오버레이 생성
    //             TransparantWnd.CreateOverlay(hWndDG);
    //             TransparantWnd.ClearBoxes();
    // 
    //             // 데이터 셀 영역만 그리기 (startRow부터)
    //             int cellCount = 0;
    //             for (int col = 0; col < columns; col++)
    //             {
    //                 for (int row = startRow; row < rowCount; row++)
    //                 {
    //                     Draw.Rectangle rc = rects[col, row];
    //                     TransparantWnd.DrawBoxAsync(rc, strokeColor: System.Windows.Media.Colors.Red, thickness: 1);
    //                     cellCount++;
    //                 }
    //             }
    // 
    //             Debug.WriteLine($"[{m_Context.AppName}] {cellCount}개 셀 영역 그리기 완료");
    // 
    //             // MsgBox 표시
    //             System.Windows.MessageBox.Show(
    //                 $"인성 DG오더 셀 영역 테스트\n\n" +
    //                 $"열: {columns}\n" +
    //                 $"행: {rowCount}\n" +
    //                 $"총 셀: {cellCount}개\n\n" +
    //                 $"확인을 누르면 오버레이가 제거됩니다.",
    //                 "셀 영역 테스트");
    // 
    //             // 오버레이 삭제
    //             TransparantWnd.DeleteOverlay();
    //             Debug.WriteLine($"[{m_Context.AppName}] Test_DrawCellRects 완료");
    //         }
    //         catch (Exception ex)
    //         {
    //             Debug.WriteLine($"[{m_Context.AppName}] 예외 발생: {ex.Message}");
    //             System.Windows.MessageBox.Show($"테스트 중 오류 발생:\n{ex.Message}", "오류");
    //             TransparantWnd.DeleteOverlay();
    //         }
    //     }
    #endregion

    public static async Task<bool> DragAsync_Vertical_Smooth(IntPtr hWnd, Draw.Point ptStartRel, int dy, int nMiliSec = 100)
    {
        for (int retry = 1; retry <= 5; retry++)
        {
            try
            {
                Std32Window.SetForegroundWindow(hWnd);
                Std32Cursor.SetCursorPos_RelDrawPt(hWnd, ptStartRel);
                IntPtr hStartCursor = Std32Cursor.GetCurrentCursorHandle();
                Draw.Point ptStartAbs = Std32Cursor.GetCursorPos_AbsDrawPt();
                Draw.Point ptEndAbs = new Draw.Point(ptStartAbs.X, ptStartAbs.Y + dy);

                Std32Mouse_Event.MouseEvent_LeftBtnDown();
                await Task.Delay(50);

                Stopwatch sw = Stopwatch.StartNew();
                while (sw.ElapsedMilliseconds < nMiliSec)
                {
                    double ratio = (double)sw.ElapsedMilliseconds / nMiliSec;
                    int currentDy = (int)(dy * ratio);
                    Std32Cursor.SetCursorPos_AbsDrawPt(new Draw.Point(ptStartAbs.X, ptStartAbs.Y + currentDy));
                    await Task.Delay(10);
                }

                Std32Cursor.SetCursorPos_AbsDrawPt(ptEndAbs);
                await Task.Delay(150); // 커서 변경 대기

                // 커서 확인 (시작과 달라졌거나, IDC_NO: 32648 이거나)
                IntPtr hCurrent = Std32Cursor.GetCurrentCursorHandle();
                IntPtr hNo = StdWin32.LoadCursor(IntPtr.Zero, StdCommon32.IDC_NO);

                if (hCurrent != hStartCursor || hCurrent == hNo)
                {
                    Std32Mouse_Event.MouseEvent_LeftBtnUp();
                    await Task.Delay(200);
                    return true;
                }

                Debug.WriteLine($"[DragAsync_Vertical_Smooth] 커서 변화 없음 (시도 {retry}/5): {hCurrent:X} == {hStartCursor:X}");
                Std32Mouse_Event.MouseEvent_LeftBtnUp();
                await Task.Delay(300);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DragAsync_Vertical_Smooth] 예외 발생 (시도 {retry}/5): {ex.Message}");
                Std32Mouse_Event.MouseEvent_LeftBtnUp();
                await Task.Delay(300);
            }
        }
        return false;
    }

    public static async Task<bool> DragAsync_Horizon_Smooth(IntPtr hWnd, Draw.Point ptStartRel, Draw.Point ptEndRel, int nMiliSec = 100)
    {
        try
        {
            Std32Window.SetForegroundWindow(hWnd);
            
            // 시작점 이동 및 절대 좌표 확보
            Std32Cursor.SetCursorPos_RelDrawPt(hWnd, ptStartRel);
            Draw.Point ptStartAbs = Std32Cursor.GetCursorPos_AbsDrawPt();
            
            // 끝점 절대 좌표 확보
            Draw.Point ptEndAbs = StdUtil.GetAbsDrawPointFromRel(hWnd, ptEndRel);

            Std32Mouse_Event.MouseEvent_LeftBtnDown();
            await Task.Delay(50);

            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < nMiliSec)
            {
                double ratio = (double)sw.ElapsedMilliseconds / nMiliSec;
                int curX = ptStartAbs.X + (int)((ptEndAbs.X - ptStartAbs.X) * ratio);
                int curY = ptStartAbs.Y + (int)((ptEndAbs.Y - ptStartAbs.Y) * ratio);
                Std32Cursor.SetCursorPos_AbsDrawPt(new Draw.Point(curX, curY));
                await Task.Delay(10);
            }

            Std32Cursor.SetCursorPos_AbsDrawPt(ptEndAbs);
            await Task.Delay(50);
            Std32Mouse_Event.MouseEvent_LeftBtnUp();
            await Task.Delay(200);
            return true;
        }
        catch { return false; }
    }
}

#nullable restore
