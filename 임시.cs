using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

public static async Task<StdResult_String> OfrStr_SeqCharAsync(Draw.Bitmap bmpSource, bool bEdit = true)
{
    Stopwatch sw = Stopwatch.StartNew();

    try
    {
        byte avgBright = OfrService.GetAverageBrightness_FromColorBitmapFast(bmpSource);
        Draw.Rectangle rcFull = new Draw.Rectangle(0, 0, bmpSource.Width, bmpSource.Height);
        Draw.Rectangle rcFore = OfrService.GetForeGroundDrawRectangle_FromColorBitmapRectFast(bmpSource, rcFull, avgBright, 0);

        if (rcFore == StdUtil.s_rcDrawEmpty) return new StdResult_String("전경 영역 없음", "OfrStr_SeqCharAsync_01");

        // ========================================
        // 1. 텍스트 캐시 조회 (전체 전경 영역 기준)
        // ========================================
        Draw.Bitmap bmpFore = OfrService.GetBitmapInBitmapFast(bmpSource, rcFore);
        if (bmpFore == null)
            return new StdResult_String("전경 비트맵 추출 실패", "OfrStr_SeqCharAsync_01_2");

        byte avgBright2 = OfrService.GetAverageBrightness_FromColorBitmapFast(bmpFore);
        OfrModel_BitmapAnalysis analyText = OfrService.GetBitmapAnalysisFast(bmpFore, avgBright2);
        bmpFore.Dispose();

        if (analyText == null || string.IsNullOrEmpty(analyText.sHexArray))
            return new StdResult_String("전경 비트맵 분석 실패", "OfrStr_SeqCharAsync_01_3");

        // 캐시 HIT
        if (TryGetTextCache(analyText, out string cachedText))
        {
            sw.Stop();
            Debug.WriteLine($"[Cache HIT] OfrStr_SeqCharAsync: '{cachedText}' ({sw.ElapsedMilliseconds}ms) - Cache: {s_TextCache.Count}/{MAX_TEXT_CACHE_SIZE}");
            return new StdResult_String(cachedText);
        }

        // ========================================
        // 2. 캐시 MISS → 전경/배경 방식으로 문자 분리
        // ========================================
        List<OfrModel_StartEnd> charList = OfrService.GetStartEndList_FromColorBitmap(bmpSource, avgBright, rcFore);
        if (charList == null || charList.Count == 0)
            return new StdResult_String("문자 분리 실패", "OfrStr_SeqCharAsync_02");

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < charList.Count; i++)
        {
            StdConst_IndexRect rcIndex = OfrService.GetIndexRect_FromColorBitmapByIndex(bmpSource, avgBright, rcFore, charList, i, i);
            if (rcIndex == null)
                return new StdResult_String($"문자{i + 1} 영역 실패", "OfrStr_SeqCharAsync_03");

            Draw.Rectangle rcChar = rcIndex.GetDrawRectangle();
            OfrModel_BitmapAnalysis modelChar = OfrService.GetBitmapAnalysisFast(bmpSource, rcChar, avgBright);
            if (modelChar == null)
                return new StdResult_String($"문자{i + 1} 분석 실패", "OfrStr_SeqCharAsync_04");

            string? character = await SelectCharByBasicAsync(modelChar.nWidth, modelChar.nHeight, modelChar.sHexArray);
            if (character == null)
            {
                if (bEdit && s_bDebugMode)
                {
                    // 수동 입력
                    string manualChar = await ShowImageToCharDialog(bmpSource, rcChar, $"문자{i + 1} DB 검색 실패");

                    if (!string.IsNullOrEmpty(manualChar))
                    {
                        await SaveToTbCharBackup(bmpSource, rcChar, manualChar);
                        sb.Append(manualChar);
                        continue;
                    }
                }

                // 디버그 아니거나 건너뜀 → ☒
                sb.Append("☒");
                await SaveToTbCharFail(modelChar, "☒");
            }
            else
            {
                sb.Append(character);
            }
        }

        if (sb.Length == 0)
            return new StdResult_String("인식 문자 없음", "OfrStr_SeqCharAsync_06");

        // ========================================
        // 3. 성공 → 텍스트 캐시에 저장
        // ========================================
        string resultText = sb.ToString();
        SaveTextCache(analyText, resultText);

        sw.Stop();
        // Debug.WriteLine($"[Cache MISS] OfrStr_SeqCharAsync: '{resultText}' ({sw.ElapsedMilliseconds}ms) - Cache: {s_TextCache.Count}/{MAX_TEXT_CACHE_SIZE}");

        return new StdResult_String(resultText);
    }
    catch (Exception ex)
    {
        return new StdResult_String(StdUtil.GetExceptionMessage(ex), "OfrStr_SeqCharAsync_999");
    }
}




//#region Region 6: 기존 주문 상태 전이 (CheckIsOrderAsync_AssumeKaiUpdated) - 주석처리 (NotChanged용으로 재설계 필요)

///// <summary>
///// 상태 전이 규칙 테이블 (Kai 상태, 인성 상태) → (목표 상태, Repeat 여부)
///// - 인성1, 인성2 공용
///// - 목표 상태가 빈 문자열("")이면 현재 상태 유지 (단순 업데이트)
///// </summary>
//private static readonly Dictionary<(string kaiState, string insungState), (string targetState, bool useRepeat)> StateTransitionRules = new()
//{
//    // ===== Kai 상태 = 접수 =====
//    { ("접수", "취소"), ("접수", true) },   // 취소 → 접수 (Repeat)
//    { ("접수", "대기"), ("접수", true) },   // 대기 → 접수 (Repeat)
//    { ("접수", "접수"), ("접수", true) },   // 접수 → 접수 (Repeat, 상태 동일)

//    // ===== Kai 상태 = 대기 =====
//    { ("대기", "취소"), ("대기", false) },  // 취소 → 대기
//    { ("대기", "접수"), ("대기", true) },   // 접수 → 대기 (Repeat)
//    { ("대기", "배차"), ("대기", true) },   // 배차 → 대기 (Repeat)
//    { ("대기", "대기"), ("", false) },      // 대기 → 대기 (상태 동일, 단순 업데이트)

//    // ===== Kai 상태 = 취소 =====
//    { ("취소", "접수"), ("취소", true) },   // 접수 → 취소 (Repeat)
//    { ("취소", "배차"), ("취소", true) },   // 배차 → 취소 (Repeat)
//    { ("취소", "운행"), ("취소", true) },   // 운행 → 취소 (Repeat)
//    { ("취소", "예약"), ("취소", false) },  // 예약 → 취소
//    { ("취소", "완료"), ("취소", false) },  // 완료 → 취소
//    { ("취소", "대기"), ("취소", false) },  // 대기 → 취소
//    { ("취소", "취소"), ("", false) },      // 취소 → 취소 (상태 동일, 단순 업데이트)

//    // ===== Kai 상태 = 배차 =====
//    { ("배차", "배차"), ("접수", true) },   // 배차 → 배차 (Repeat, 접수로 변경하여 업데이트)
//};

///// <summary>
///// 기존 주문 상태 체크 및 업데이트 (Kai 업데이트 가정)
///// - 인성1, 인성2 공용 함수
///// - Phase 1: 구조만 구현 (실제 업데이트 로직은 Phase 2에서 추가)
///// </summary>
///// <param name="item">자동배차 주문 정보</param>
///// <param name="dgInfo">Datagrid에서 찾은 주문 정보</param>
///// <param name="ctrl">취소 토큰 컨트롤</param>
///// <returns>처리 결과 (SuccessAndReEnqueue, SuccessAndComplete, FailureAndRetry, FailureAndDiscard)</returns>
//public async Task<CommonResult_AutoAllocProcess> CheckIsOrderAsync_AssumeKaiUpdated(AutoAllocModel item, CommonResult_AutoAllocDatagrid dgInfo, CancelTokenControl ctrl)
//{
//    await ctrl.WaitIfPausedOrCancelledAsync();

//    string kaiState = item.NewOrder.OrderState;
//    string insungState = dgInfo.sStatus;

//    Debug.WriteLine($"[{m_Context.AppName}] CheckIsOrderAsync_AssumeKaiUpdated - Kai상태={kaiState}, 인성상태={insungState}");

//    // 1. 상태 전이 규칙 조회
//    if (!StateTransitionRules.TryGetValue((kaiState, insungState), out var rule))
//    {
//        // 정의되지 않은 상태 조합
//        string errorMsg = $"미정의 상태 전이: Kai={kaiState}, 인성={insungState}";
//        Debug.WriteLine($"[{m_Context.AppName}] {errorMsg}");

//        return CommonResult_AutoAllocProcess.FailureAndDiscard(errorMsg, $"{nameof(CheckIsOrderAsync_AssumeKaiUpdated)}_UndefinedState");
//    }

//    // 2. Phase 1: 임시 구현 (모든 케이스에서 SuccessAndReEnqueue 반환)
//    Debug.WriteLine($"[{m_Context.AppName}] 상태 전이 규칙 찾음: 목표={rule.targetState}, Repeat={rule.useRepeat}");
//    Debug.WriteLine($"[{m_Context.AppName}] [임시] Phase 1 구현 - 실제 업데이트 없이 SuccessAndReEnqueue 반환");

//    // TODO: Phase 2에서 실제 업데이트 로직 추가
//    // - rule.targetState와 rule.useRepeat에 따라 인성 앱에서 주문 상태 업데이트
//    // - 업데이트 성공/실패에 따라 적절한 결과 반환

//    return CommonResult_AutoAllocProcess.SuccessAndReEnqueue($"[임시] 상태 전이 규칙 확인 완료: {kaiState}→{rule.targetState}");
//}

//#endregion

#region Tmp

/// <summary>
/// 공통 함수 #2: 고객 검색 및 선택 (의뢰자, 출발지, 도착지 공통)
/// TODO: GetCustSearchTypeAsync, 고객검색Wnd 등 복잡한 로직 통합 필요
/// </summary>
//     private async Task<RegistResult> SearchAndSelectCustomerAsync(
//         IntPtr hWnd고객명,
//         IntPtr hWnd동명,
//         string custName,
//         string chargeName,
//         string fieldName,
//         CancelTokenControl ctrl)
//     {
//         // 1. 검색어 생성 (상호/담당)
//         string searchText = NwCommon.GetInsungTextForSearch(custName, chargeName);

//         // 2. 고객명 EditBox에 입력 (OfrWork_Common 사용 - 기존 로직과 동일)
//         var resultBool = await OfrWork_Common.WriteEditBox_ToHndleAsync(hWnd고객명, searchText);
//         if (resultBool == null || !resultBool.bResult)
//             return RegistResult.ErrorResult($"{fieldName} 고객명 입력 실패");

//         // 3. 검색 실행 및 결과 타입 확인 (GetCustSearchTypeAsync - Enter 키 포함)
//         var searchResult = await GetCustSearchTypeAsync(hWnd고객명, hWnd동명);

//         // 4. 검색 결과 처리
//         switch (searchResult.resultTye)
//         {
//             case AutoAlloc_CustSearch.One:
//                 // 1개 검색 성공 - 그대로 진행
//                 Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 검색 성공 (1개)");
//                 return RegistResult.SuccessResult();

//             case AutoAlloc_CustSearch.Multi:
//                 // TODO: 복수 고객 검색창 처리 필요
//                 Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 복수 검색 결과 - TODO 처리 필요");
//                 return RegistResult.ErrorResult($"{fieldName} 복수 검색됨 (TODO: 고객검색창 처리 필요)");

//             case AutoAlloc_CustSearch.None:
//                 // TODO: 신규 고객 등록창 처리 필요
//                 Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 신규 고객 - TODO 처리 필요");
//                 return RegistResult.ErrorResult($"{fieldName} 신규 고객 (TODO: 고객등록창 처리 필요)");

//             case AutoAlloc_CustSearch.Null:
//             default:
//                 return RegistResult.ErrorResult($"{fieldName} 검색 타임아웃");
//         }
//     }

/// <summary>
/// 공통 함수 #3: CheckBox 설정 및 검증
/// TODO: OFR 기반 체크박스 찾기 및 클릭 구현 필요
/// </summary>
//     private async Task<RegistResult> SetAndVerifyCheckBoxAsync(
//         IntPtr hWndParent,
//         string checkBoxName,
//         bool shouldCheck,
//         string fieldName,
//         CancelTokenControl ctrl)
//     {
//         // TODO: OFR로 체크박스 이미지 찾아서 상태 확인 후 클릭
//         Debug.WriteLine($"[{m_Context.AppName}] TODO: SetAndVerifyCheckBoxAsync 구현 필요 - {fieldName}");
//         return RegistResult.SuccessResult(); // 임시
//     }

/// <summary>
/// 공통 함수 #4: RadioButton 설정 및 검증
/// TODO: OFR 기반 라디오버튼 찾기 및 클릭 구현 필요
/// </summary>
//     private async Task<RegistResult> SetAndVerifyRadioButtonAsync(
//         IntPtr hWndParent,
//         string radioGroupName,
//         string radioValue,
//         string fieldName,
//         CancelTokenControl ctrl)
//     {
//         // TODO: OFR로 라디오버튼 그룹 찾아서 원하는 값 클릭
//         Debug.WriteLine($"[{m_Context.AppName}] TODO: SetAndVerifyRadioButtonAsync 구현 필요 - {fieldName}={radioValue}");
//         return RegistResult.SuccessResult(); // 임시
//     }

/// <summary>
/// 공통 함수 #5: ComboBox 아이템 선택
/// TODO: OFR 기반 콤보박스 열기 및 아이템 선택 구현 필요
/// </summary>
//     private async Task<RegistResult> SelectComboBoxItemAsync(
//         IntPtr hWndParent,
//         string comboBoxName,
//         string itemValue,
//         string fieldName,
//         CancelTokenControl ctrl)
//     {
//         // TODO: OFR로 콤보박스 찾아서 열고 아이템 클릭
//         Debug.WriteLine($"[{m_Context.AppName}] TODO: SelectComboBoxItemAsync 구현 필요 - {fieldName}={itemValue}");
//         return RegistResult.SuccessResult(); // 임시
//     }

/// <summary>
/// 공통 함수: 숫자 필드 입력 및 검증 (포커스 설정 + SetWindowText 시도 → 실패 시 키보드 폴백)
/// - 0차: SetFocusWithForegroundAsync로 포커스 설정 (재시도 포함)
/// - 1차: SetWindowText로 값 설정 시도
/// - 2차: 실패 시 키보드 시뮬레이션 (HOME + DELETE + 숫자 입력)
/// - Enter 키로 인성 앱의 합계 재계산 트리거
/// </summary>

/// <summary>
/// Datagrid 컬럼 너비 조정 (드래그 방식)
/// </summary>
/// <param name="rcHeader">헤더 영역 Rectangle (MainWnd 기준 상대좌표)</param>
/// <param name="ptDgTopLeft">Datagrid 좌상단 좌표 (절대좌표)</param>
/// <param name="indexCol">조정할 컬럼 인덱스</param>
/// <returns>에러 발생 시 StdResult_Error, 성공 시 null</returns>
//     private StdResult_Error AdjustColumnWidth(
//         Draw.Rectangle rcHeader,
//         Draw.Point ptDgTopLeft,
//         int indexCol)
//     {
//         Draw.Bitmap bmpHeader = null;

//         try
//         {
//             // 1. 헤더 캡처
//             bmpHeader = OfrService.CaptureScreenRect_InWndHandle(
//                 m_Main.TopWnd_hWnd,
//                 rcHeader
//             );

//             if (bmpHeader == null)
//             {
//                 return new StdResult_Error(
//                     $"헤더 캡처 실패: indexCol={indexCol}",
//                     "InsungsAct_RcptRegPage/AdjustColumnWidth_01");
//             }

//             // 2. 컬럼 경계 검출
//             byte minBrightness = OfrService.GetMinBrightnessAtRow_FromColorBitmapFast(
//                 bmpHeader, HEADER_GAB
//             );
//             minBrightness += 2; // 확실한 경계를 위해

//             bool[] boolArr = OfrService.GetBoolArray_FromColorBitmapRowFast(
//                 bmpHeader, HEADER_GAB, minBrightness, 2
//             );

//             List<OfrModel_LeftWidth> listLW =
//                 OfrService.GetLeftWidthList_FromBool1Array(boolArr, minBrightness);

//             if (listLW == null || indexCol >= listLW.Count)
//             {
//                 bmpHeader?.Dispose();
//                 return new StdResult_Error(
//                     $"컬럼 경계 검출 실패: indexCol={indexCol}, listLW.Count={listLW?.Count}",
//                     "InsungsAct_RcptRegPage/AdjustColumnWidth_02");
//             }

//             // 3. 너비 차이 계산
//             int actualWidth = listLW[indexCol].nWidth;
//             int expectedWidth = m_ReceiptDgHeaderInfos[indexCol].nWidth;
//             int dx = expectedWidth - actualWidth;

//             if (dx == 0)
//             {
//                 Debug.WriteLine($"[AdjustColumnWidth] 컬럼[{indexCol}] 너비 조정 불필요: {actualWidth}px");
//                 bmpHeader?.Dispose();
//                 return null; // 이미 맞음
//             }

//             Debug.WriteLine($"[AdjustColumnWidth] 컬럼[{indexCol}] '{m_ReceiptDgHeaderInfos[indexCol].sName}' 너비 조정: {actualWidth} → {expectedWidth} (dx={dx})");

//             // 4. 드래그로 너비 조정
//             // ptStart: 컬럼 오른쪽 경계 (헤더 기준 상대좌표)
//             Draw.Point ptStartRel = new Draw.Point(listLW[indexCol]._nRight + 1, HEADER_GAB);
//             Draw.Point ptEndRel = new Draw.Point(ptStartRel.X + dx, ptStartRel.Y);

//             // MainWnd 기준 상대좌표로 변환
//             Draw.Point ptStartMainRel = new Draw.Point(
//                 rcHeader.Left + ptStartRel.X,
//                 rcHeader.Top + ptStartRel.Y
//             );
//             Draw.Point ptEndMainRel = new Draw.Point(
//                 rcHeader.Left + ptEndRel.X,
//                 rcHeader.Top + ptEndRel.Y
//             );

//             // 절대좌표로 변환
//             Draw.Point ptStartAbs = new Draw.Point(
//                 ptDgTopLeft.X + ptStartMainRel.X,
//                 ptDgTopLeft.Y + ptStartMainRel.Y
//             );
//             Draw.Point ptEndAbs = new Draw.Point(
//                 ptDgTopLeft.X + ptEndMainRel.X,
//                 ptDgTopLeft.Y + ptEndMainRel.Y
//             );

//             // Drag 수행 (Simulation.cs:457-478 참고, 라이브러리 조합 방식)
//             Std32Cursor.SetCursorPos_AbsDrawPt(ptStartAbs);
//             Std32Mouse_Event.MouseEvent_LeftBtnDown();
//             Std32Mouse_Send.MouseSet_MoveSmooth_ptAbs(ptStartAbs, ptEndAbs, 150);
//             Std32Mouse_Event.MouseEvent_LeftBtnUp();

//             bmpHeader?.Dispose();
//             return null; // 성공
//         }
//         catch (Exception ex)
//         {
//             bmpHeader?.Dispose();
//             return new StdResult_Error(
//                 $"AdjustColumnWidth 예외: {ex.Message}",
//                 "InsungsAct_RcptRegPage/AdjustColumnWidth_999");
//         }
//     }

/// <summary>
/// 텍스트 입력 (재시도 포함)
/// </summary>
//     private async Task<bool> InputTextAsync(IntPtr hWnd, string text)
//     {
//         const int MAX_RETRY = 3;
//         const int WAIT_MS = 100;

//         for (int i = 0; i < MAX_RETRY; i++)
//         {
//             await OfrWork_Common.WriteEditBox_ToHndleAsync(hWnd, text);

//             string current = Std32Window.GetWindowCaption(hWnd);
//             if (current == text)
//                 return true;

//             await Task.Delay(WAIT_MS);
//         }

//         Debug.WriteLine($"[{m_Context.AppName}] 텍스트 입력 실패: 원하는={text}, 현재={Std32Window.GetWindowCaption(hWnd)}");
//         return false;
//     }

/// <summary>
/// 전화번호 입력 (Enter 키 포함, 재시도)
/// </summary>
//     private async Task<bool> InputPhoneAsync(IntPtr hWnd, string phoneNo)
//     {
//         const int MAX_RETRY = 3;
//         const int WAIT_MS = 100;

//         for (int i = 0; i < MAX_RETRY; i++)
//         {
//             // 전화번호 입력
//             Std32Window.SetWindowCaption(hWnd, phoneNo);
//             await Task.Delay(WAIT_MS);

//             // Enter 키 전송
//             await Std32Key_Msg.KeyPostAsync_MouseClickNDown(hWnd, StdCommon32.VK_RETURN);
//             await Task.Delay(WAIT_MS);

//             string current = StdConvert.MakePhoneNumberToDigit(
//                 Std32Window.GetWindowCaption(hWnd));

//             if (current == phoneNo)
//                 return true;

//             await Task.Delay(WAIT_MS);
//         }

//         Debug.WriteLine($"[{m_Context.AppName}] 전화번호 입력 실패: 원하는={phoneNo}, 현재={StdConvert.MakePhoneNumberToDigit(Std32Window.GetWindowCaption(hWnd))}");
//         return false;
//     }

/// <summary>
/// 고객명 입력 (검색 포함)
/// </summary>
//     private async Task<CustomerResult> InputCustomerAsync(
//         IntPtr hWndName, IntPtr hWndDong,
//         string custName, string chargeName, string region)
//     {
//         // 고객명 입력
//         string searchText = NwCommon.GetInsungTextForSearch(custName, chargeName);
//         bool inputOk = await InputTextAsync(hWndName, searchText);

//         if (!inputOk)
//             return CustomerResult.ErrorResult($"{region} 고객명 입력 실패");

//         // 검색 결과 확인
//         var searchResult = await GetCustSearchTypeAsync(hWndName, hWndDong);

//         switch (searchResult.resultTye)
//         {
//             case AutoAlloc_CustSearch.One:
//                 // 1개 검색 → 성공
//                 Debug.WriteLine($"[{m_Context.AppName}] {region} 고객 검색 성공: {custName}");
//                 return CustomerResult.SuccessResult();

//             case AutoAlloc_CustSearch.Multi:
//                 // 여러 개 → 수동 처리 필요
//                 Debug.WriteLine($"[{m_Context.AppName}] {region} 고객 복수 검색됨: {custName} (수동 처리 필요)");
//                 return CustomerResult.ErrorResult($"{region} 고객 복수 검색됨 (수동 처리 필요)");

//             case AutoAlloc_CustSearch.None:
//                 // 없음 → 고객 등록 필요
//                 Debug.WriteLine($"[{m_Context.AppName}] {region} 고객 없음: {custName} (등록 필요)");
//                 return CustomerResult.ErrorResult($"{region} 고객 없음 (등록 필요)");

//             default:
//                 return CustomerResult.ErrorResult($"{region} 고객 검색 실패");
//         }
//     }

/// <summary>
/// 고객 검색 결과 타입 확인
/// - 검색 결과가 1개인지, 복수인지, 없는지 확인
/// </summary>
//     private async Task<AutoAlloc_SearchTypeResult> GetCustSearchTypeAsync(IntPtr hWnd고객명, IntPtr hWnd동명)
//     {
//         IntPtr hWndTmp = IntPtr.Zero;
//         string sTmp = "";

//         // EnterKey 전송
//         Std32Key_Msg.KeyPost_Down(hWnd고객명, StdCommon32.VK_RETURN);

//         // 검색 결과 확인 (최대 50번, 1.5초)
//         for (int j = 0; j < 50; j++)
//         {
//             await Task.Delay(30);

//             // 1개 검색됨 - 동명에 텍스트가 들어옴
//             sTmp = Std32Window.GetWindowCaption(hWnd동명);
//             if (!string.IsNullOrEmpty(sTmp))
//             {
//                 return new AutoAlloc_SearchTypeResult(AutoAlloc_CustSearch.One, IntPtr.Zero);
//             }

//             // 신규 고객 - 고객등록창이 뜸
//             hWndTmp = Std32Window.FindMainWindow(m_Context.MemInfo.Splash.TopWnd_uProcessId, null,
//                 m_Context.FileInfo.고객등록Wnd_TopWnd_sWndName);
//             if (hWndTmp != IntPtr.Zero)
//             {
//                 return new AutoAlloc_SearchTypeResult(AutoAlloc_CustSearch.None, hWndTmp);
//             }

//             // 복수 고객 - 고객검색창이 뜸
//             hWndTmp = Std32Window.FindMainWindow(m_Context.MemInfo.Splash.TopWnd_uProcessId, null,
//                 m_Context.FileInfo.고객검색Wnd_TopWnd_sWndName);
//             if (hWndTmp != IntPtr.Zero)
//             {
//                 // 고객검색창이 떴을 때, 단수 고객이면 자동으로 닫힘
//                 // 1.5초 동안 기다리면서 창이 닫히는지 확인
//                 for (int k = 0; k < 30; k++)
//                 {
//                     hWndTmp = Std32Window.FindMainWindow(m_Context.MemInfo.Splash.TopWnd_uProcessId, null,
//                         m_Context.FileInfo.고객검색Wnd_TopWnd_sWndName);
//                     if (hWndTmp == IntPtr.Zero) break; // 창이 닫힘 → 단수 고객

//                     await Task.Delay(50);
//                 }

//                 // 30번 반복 후에도 창이 살아있으면 복수 고객
//                 if (hWndTmp != IntPtr.Zero)
//                 {
//                     return new AutoAlloc_SearchTypeResult(AutoAlloc_CustSearch.Multi, hWndTmp);
//                 }
//                 // 창이 닫혔으면 계속 루프 (동명 확인으로)
//             }
//         }

//         // 검색 실패
//         Debug.WriteLine($"[{m_Context.AppName}] GetCustSearchTypeAsync 실패: 타임아웃");
//         return new AutoAlloc_SearchTypeResult(AutoAlloc_CustSearch.Null, IntPtr.Zero);
//     }
/// <summary>
/// 테스트: Selection 있는 상태 vs 없는 상태 OFR 비교
/// 목적: EmptyRow 클릭 없이도 OFR이 정상 작동하는지 확인
/// </summary>
//public async Task<StdResult_String> Test_CompareOFR_WithAndWithoutSelectionAsync()
//{
//    try
//    {
//        Debug.WriteLine("[Test_CompareOFR] ===== 테스트 시작 =====");

//        // 1. 현재 상태 그대로 캡처 (Selection 있을 수 있음)
//        Debug.WriteLine("[Test_CompareOFR] 1. 현재 상태 캡처 (Selection 있을 수 있음)");
//        Draw.Bitmap bmpWithSelection = OfrService.CaptureScreenRect_InWndHandle(m_RcptPage.DG오더_hWnd);
//        if (bmpWithSelection == null)
//        {
//            return new StdResult_String("캡처 실패 (Selection 있는 상태)", "Test_CompareOFR/01");
//        }

//        // 2. 현재 상태에서 밝기 측정
//        Draw.Rectangle[,] rects = m_RcptPage.DG오더_RelChildRects;
//        if (rects == null || rects.GetLength(1) < 3)
//        {
//            return new StdResult_String("RelChildRects 없음", "Test_CompareOFR/02");
//        }

//        // 첫 번째 데이터 행의 밝기 측정 (y=2, 헤더는 0,1)
//        int x = rects[0, 2].Left + 5;
//        int y = rects[0, 2].Top + 6;
//        int brightness_WithSel = OfrService.GetBrightness_PerPixel(bmpWithSelection, x, y);

//        Debug.WriteLine($"[Test_CompareOFR] 2. Selection 있는 상태 밝기: {brightness_WithSel}");
//        Debug.WriteLine($"[Test_CompareOFR]    배경 밝기 기준: {m_RcptPage.DG오더_nBackgroundBright}");

//        // 3. 첫 번째 행 클릭 (Selection 이동)
//        Debug.WriteLine("[Test_CompareOFR] 3. 첫 번째 행 클릭 (Selection 이동)");
//        Draw.Point ptFirstRow = StdUtil.GetDrawPoint(rects[0, 2], 3, 3);
//        await Simulation_Mouse.SafeMouseEvent_ClickLeft_ptRelAsync(m_RcptPage.DG오더_hWnd, ptFirstRow, true, 100);
//        await Task.Delay(200);

//        // 4. 첫 번째 행 선택 상태에서 캡처
//        Debug.WriteLine("[Test_CompareOFR] 4. 첫 번째 행 선택 상태 캡처");
//        Draw.Bitmap bmpFirstRowSel = OfrService.CaptureScreenRect_InWndHandle(m_RcptPage.DG오더_hWnd);
//        if (bmpFirstRowSel == null)
//        {
//            return new StdResult_String("캡처 실패 (첫 행 선택)", "Test_CompareOFR/03");
//        }

//        int brightness_FirstRowSel = OfrService.GetBrightness_PerPixel(bmpFirstRowSel, x, y);
//        Debug.WriteLine($"[Test_CompareOFR]    첫 행 선택 밝기: {brightness_FirstRowSel}");

//        // 5. 두 번째 행의 밝기 비교 (선택되지 않은 행)
//        int x2 = rects[0, 3].Left + 5;
//        int y2 = rects[0, 3].Top + 6;
//        int brightness_SecondRow = OfrService.GetBrightness_PerPixel(bmpFirstRowSel, x2, y2);
//        Debug.WriteLine($"[Test_CompareOFR]    두 번째 행 밝기 (선택 안됨): {brightness_SecondRow}");

//        // 6. 결과 분석
//        string result = $"===== OFR Selection 테스트 결과 =====\n";
//        result += $"배경 밝기 기준: {m_RcptPage.DG오더_nBackgroundBright}\n";
//        result += $"현재 상태 밝기: {brightness_WithSel}\n";
//        result += $"첫 행 선택 밝기: {brightness_FirstRowSel}\n";
//        result += $"두 번째 행 밝기: {brightness_SecondRow}\n";
//        result += $"\n";
//        result += $"선택된 행 vs 배경: {Math.Abs(brightness_FirstRowSel - m_RcptPage.DG오더_nBackgroundBright)}\n";
//        result += $"선택 안된 행 vs 배경: {Math.Abs(brightness_SecondRow - m_RcptPage.DG오더_nBackgroundBright)}\n";
//        result += $"\n";

//        // 7. 결론
//        int threshold = 10; // 밝기 차이 임계값
//        bool isFirstRowSelected = Math.Abs(brightness_FirstRowSel - m_RcptPage.DG오더_nBackgroundBright) > threshold;
//        bool isSecondRowNormal = Math.Abs(brightness_SecondRow - m_RcptPage.DG오더_nBackgroundBright) < threshold;

//        if (isFirstRowSelected && isSecondRowNormal)
//        {
//            result += "✅ 결론: 선택된 행만 밝기가 다릅니다.\n";
//            result += "   → OFR 시 선택되지 않은 행들은 정상 인식 가능!\n";
//            result += "   → EmptyRow 클릭 불필요 (첫 행 선택으로 대체 가능)\n";
//        }
//        else if (!isFirstRowSelected && !isSecondRowNormal)
//        {
//            result += "⚠️ 경고: 선택 여부와 무관하게 밝기 차이가 없습니다.\n";
//            result += "   → 추가 테스트 필요\n";
//        }
//        else
//        {
//            result += "❌ 문제: 예상과 다른 밝기 패턴입니다.\n";
//            result += "   → 수동 확인 필요\n";
//        }

//        Debug.WriteLine($"[Test_CompareOFR] {result}");
//        Debug.WriteLine("[Test_CompareOFR] ===== 테스트 종료 =====");

//        // Bitmap 해제
//        bmpWithSelection?.Dispose();
//        bmpFirstRowSel?.Dispose();

//        return new StdResult_String(result);
//    }
//    catch (Exception ex)
//    {
//        Debug.WriteLine($"[Test_CompareOFR] 예외 발생: {ex.Message}");
//        return new StdResult_String(ex.Message, "Test_CompareOFR/999");
//    }
//}

/// <summary>
/// Datagrid 로딩 완료 대기 (Pan 상태 변화 감지)
/// </summary>
/// <param name="hWndDG">Datagrid 윈도우 핸들</param>
/// <param name="Elpase">최대 대기 시간 (밀리초, 기본 500ms)</param>
/// <returns>성공: Success, 시간 초과: Fail, Pan 없음: Skip</returns>
//     private async Task<StdResult_Status> WaitPanLoadedAsync(IntPtr hWndDG, int Elpase = 500)
//     {
//         IntPtr hWndFind = IntPtr.Zero;

//         // 1. Pan이 나타날 때까지 대기 (최대 100ms)
//         for (int i = 0; i < 100; i++)
//         {
//             hWndFind = Std32Window.GetWndHandle_FromRelDrawPt(hWndDG, m_FileInfo.접수등록Page_DG오더_ptChkRelPanL);
//             if (hWndFind != hWndDG) break;  // Pan이 나타남
//             await Task.Delay(1);
//         }

//         if (hWndFind == hWndDG)  // Pan이 안 나타남 (이미 로딩 완료)
//             return new StdResult_Status(StdResult.Skip);

//         // 2. Pan이 사라질 때까지 대기 (로딩 완료)
//         for (int i = 0; i < Elpase; i++)
//         {
//             hWndFind = Std32Window.GetWndHandle_FromRelDrawPt(hWndDG, m_FileInfo.접수등록Page_DG오더_ptChkRelPanL);
//             if (hWndFind == hWndDG) break;  // Pan이 사라짐 (로딩 완료)
//             await Task.Delay(100);
//         }

//         if (hWndFind != hWndDG)
//         {
//             Debug.WriteLine($"[{m_Context.AppName}/RcptRegPage] WaitPanLoadedAsync 시간 초과");
//             return CommonFuncs_StdResult.ErrMsgResult_Status(StdResult.Fail,
//                 "Datagrid 로딩 대기 시간 초과",
//                 $"{m_Context.AppName}/RcptRegPage/WaitPanLoadedAsync_01");
//         }

//         return new StdResult_Status(StdResult.Success);
//     }

// TODO: Simulation_Mouse 메서드 구현 후 주석 해제
///// <summary>
///// 조회 버튼 클릭 후 총계 읽기
///// </summary>
///// <param name="ctrl">취소 토큰 컨트롤</param>
///// <returns>총계 문자열 (실패 시 빈 문자열 또는 null)</returns>
//public async Task<StdResult_String> Click조회버튼Async(CancelTokenControl ctrl)
//{
//    try
//    {
//        string str = "";
//        StdResult_Status resultSts = null;

//        // 조회 버튼 클릭 후 총계 읽기 반복 시도
//        for (int i = 0; i < CommonVars.c_nRepeatShort; i++)
//        {
//            await ctrl.WaitIfPausedOrCancelledAsync();

//            // 1. 조회 버튼 클릭
//            Simulation_Mouse.SafeMousePost_ClickLeft(m_RcptPage.CmdBtn_hWnd조회);

//            // 2. Datagrid 로딩 대기
//            resultSts = await WaitPanLoadedAsync(m_RcptPage.DG오더_hWnd);
//            if (resultSts.Result == StdResult.Fail) continue;

//            // 3. 총계 읽기
//            str = Std32Window.GetWindowCaption(m_RcptPage.CallCount_hWnd총계);
//            if (!string.IsNullOrEmpty(str))
//            {
//                Debug.WriteLine($"[{m_Context.AppName}/RcptRegPage] 조회 버튼 클릭 완료, 총계: {str}");
//                break;
//            }

//            await Task.Delay(CommonVars.c_nWaitNormal, ctrl.Token);
//        }

//        return new StdResult_String(str);
//    }
//    catch (Exception ex)
//    {
//        return new StdResult_String(StdUtil.GetExceptionMessage(ex),
//            $"{m_Context.AppName}/RcptRegPage/Click조회버튼Async_999");
//    }
//}

///// <summary>
///// Empty Row 클릭 (선택 해제용)
///// </summary>
///// <param name="ctrl">취소 토큰 컨트롤</param>
///// <returns>클릭 성공 여부</returns>
//public async Task<bool> ClickEmptyRowAsync(CancelTokenControl ctrl)
//{
//    bool bClicked = false;

//    try
//    {
//        // Empty Row는 [0, 1] 셀 (첫 번째 컬럼, 두 번째 행)
//        Draw.Point ptRel = StdUtil.GetDrawPoint(m_RcptPage.DG오더_RelChildRects[0, 1], 3, 3);

//        for (int i = 0; i < CommonVars.c_nRepeatShort; i++)
//        {
//            await ctrl.WaitIfPausedOrCancelledAsync();

//            // 밝기 변화 감지로 클릭 확인
//            bClicked = await Simulation_Mouse
//                .SafeMousePost_ClickLeft_ptRel_WaitBrightChange(
//                    m_RcptPage.DG오더_hWnd,
//                    ptRel,
//                    ptRel,
//                    m_RcptPage.DG오더_nBackgroundBright);

//            if (bClicked)
//            {
//                Debug.WriteLine($"[{m_Context.AppName}/RcptRegPage] Empty Row 클릭 완료");
//                break;
//            }

//            await Task.Delay(100, ctrl.Token);
//        }

//        return bClicked;
//    }
//    catch (Exception ex)
//    {
//        Debug.WriteLine($"[{m_Context.AppName}/RcptRegPage] ClickEmptyRowAsync 예외: {ex.Message}");
//        return false;
//    }
//}
#endregion