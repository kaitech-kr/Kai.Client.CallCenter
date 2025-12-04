using System;
using System.Diagnostics;
using System.Threading.Tasks;

public async Task<StdResult_Status> OpenNewOrderPopupAsync(AutoAllocModel item, CancelTokenControl ctrl)
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
/// - TODO: 입력 작업
/// - 닫기 버튼 클릭
/// - 창 닫힘 확인 (성공 판단)
/// </summary>
public async Task<StdResult_Status> RegistOrderToPopupAsync(AutoAllocModel item, IntPtr hWndPopup, CancelTokenControl ctrl)
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
            return new StdResult_Status(StdResult.Fail,
                "상차지 주소가 없습니다.", "RegistOrderToPopupAsync_Pre01");

        // 하차지 주소 체크 (상세주소 기준)
        if (string.IsNullOrEmpty(tbOrder.DestDetailAddr))
            return new StdResult_Status(StdResult.Fail,
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
            return result;

        // 의뢰자 전화
        IntPtr hWnd의뢰자전화 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_의뢰자_ptRel고객전화);
        result = await WriteAndVerifyEditBoxAsync(hWnd의뢰자전화, tbOrder.CallTelNo ?? "", "의뢰자_전화", ctrl);
        if (result.Result != StdResult.Success)
            return result;

        // 의뢰자 담당자
        IntPtr hWnd의뢰자담당 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_의뢰자_ptRel담당자);
        result = await WriteAndVerifyEditBoxAsync(hWnd의뢰자담당, tbOrder.CallChargeName ?? "", "의뢰자_담당자", ctrl);
        if (result.Result != StdResult.Success)
            return result;

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
            return result;

        // 상차지 위치 (상세주소)
        IntPtr hWnd상차위치 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_상차지_ptRel위치);
        result = await WriteAndVerifyEditBoxAsync(hWnd상차위치, tbOrder.StartDetailAddr ?? "", "상차지_위치", ctrl);
        if (result.Result != StdResult.Success)
            return result;

        // 상차지 고객명
        IntPtr hWnd상차고객명 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_상차지_ptRel고객명);
        result = await WriteAndVerifyEditBoxAsync(hWnd상차고객명, tbOrder.StartCustName ?? "", "상차지_고객명", ctrl);
        if (result.Result != StdResult.Success)
            return result;

        // 상차지 전화
        IntPtr hWnd상차전화 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_상차지_ptRel전화);
        result = await WriteAndVerifyEditBoxAsync(hWnd상차전화, tbOrder.StartTelNo ?? "", "상차지_전화", ctrl);
        if (result.Result != StdResult.Success)
            return result;

        // 상차지 부서명
        IntPtr hWnd상차부서 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_상차지_ptRel부서명);
        result = await WriteAndVerifyEditBoxAsync(hWnd상차부서, tbOrder.StartDeptName ?? "", "상차지_부서명", ctrl);
        if (result.Result != StdResult.Success)
            return result;

        // 상차지 담당자
        IntPtr hWnd상차담당 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_상차지_ptRel담당자);
        result = await WriteAndVerifyEditBoxAsync(hWnd상차담당, tbOrder.StartChargeName ?? "", "상차지_담당자", ctrl);
        if (result.Result != StdResult.Success)
            return result;

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
            return result;

        // 하차지 위치 (상세주소)
        IntPtr hWnd하차위치 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_하차지_ptRel위치);
        result = await WriteAndVerifyEditBoxAsync(hWnd하차위치, tbOrder.DestDetailAddr ?? "", "하차지_위치", ctrl);
        if (result.Result != StdResult.Success)
            return result;

        // 하차지 고객명
        IntPtr hWnd하차고객명 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_하차지_ptRel고객명);
        result = await WriteAndVerifyEditBoxAsync(hWnd하차고객명, tbOrder.DestCustName ?? "", "하차지_고객명", ctrl);
        if (result.Result != StdResult.Success)
            return result;

        // 하차지 전화
        IntPtr hWnd하차전화 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_하차지_ptRel전화);
        result = await WriteAndVerifyEditBoxAsync(hWnd하차전화, tbOrder.DestTelNo ?? "", "하차지_전화", ctrl);
        if (result.Result != StdResult.Success)
            return result;

        // 하차지 부서명
        IntPtr hWnd하차부서 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_하차지_ptRel부서명);
        result = await WriteAndVerifyEditBoxAsync(hWnd하차부서, tbOrder.DestDeptName ?? "", "하차지_부서명", ctrl);
        if (result.Result != StdResult.Success)
            return result;

        // 하차지 담당자
        IntPtr hWnd하차담당 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_하차지_ptRel담당자);
        result = await WriteAndVerifyEditBoxAsync(hWnd하차담당, tbOrder.DestChargeName ?? "", "하차지_담당자", ctrl);
        if (result.Result != StdResult.Success)
            return result;

        Debug.WriteLine($"[{m_Context.AppName}] 2. 하차지 정보 입력 완료");
        #endregion

        #region ===== 3. 배송타입 (체크박스 - 복수 선택 가능) =====
        Debug.WriteLine($"[{m_Context.AppName}] 3. 배송타입 입력... DeliverType={tbOrder.DeliverType}");

        // 오더번호, 오더상태를 얻으려면 여기에서...
        CEnum_Cg24OrderStatus status = Get오더타입FlagsFromKaiTable(tbOrder);

        result = await Set오더타입Async(hWndPopup, status, ctrl);
        if (result.Result != StdResult.Success) return result;

        Debug.WriteLine($"[{m_Context.AppName}] 3. 배송타입 입력 완료");
        #endregion

        #region ===== 4. 차량정보 =====
        Debug.WriteLine($"[{m_Context.AppName}] 4. 차량정보 입력... CarType={tbOrder.CarType}, CarWeight={tbOrder.CarWeight}, TruckDetail={tbOrder.TruckDetail}");

        // 4-1. 차량톤수 (라디오버튼)
        var ptTon = GetCarWeightWithPoint(tbOrder.CarType, tbOrder.CarWeight);
        if (!string.IsNullOrEmpty(ptTon.sErr))
            return new StdResult_Status(StdResult.Fail, ptTon.sErr, "RegistOrderToPopupAsync_04_01");

        IntPtr hWndTon = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, ptTon.ptResult);
        if (hWndTon == IntPtr.Zero)
            return new StdResult_Status(StdResult.Fail, "톤수 라디오버튼 핸들 획득 실패", "RegistOrderToPopupAsync_04_02");

        result = await Simulation_Mouse.SetCheckBtnStatusAsync(hWndTon, true);
        if (result.Result != StdResult.Success)
            return new StdResult_Status(StdResult.Fail, "톤수 라디오버튼 설정 실패", "RegistOrderToPopupAsync_04_03");

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
            return new StdResult_Status(StdResult.Fail, strTruck.sErr, "RegistOrderToPopupAsync_04_04");

        IntPtr hWndTruck = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_차종Combo_ptRel차종확인);
        if (hWndTruck == IntPtr.Zero)
            return new StdResult_Status(StdResult.Fail, "차종 콤보박스 핸들 획득 실패", "RegistOrderToPopupAsync_04_05");

        Std32Window.SetWindowCaption(hWndTruck, strTruck.strResult);

        Debug.WriteLine($"[{m_Context.AppName}] 4-2. 차종 설정 완료: {tbOrder.TruckDetail} → {strTruck.strResult}");
        Debug.WriteLine($"[{m_Context.AppName}] 4. 차량정보 입력 완료");
        #endregion

        #region ===== 5. 운송비 =====
        Debug.WriteLine($"[{m_Context.AppName}] 5. 운송비 입력... FeeType={tbOrder.FeeType}, FeeTotal={tbOrder.FeeTotal}");

        // 5-1. 운송비구분 (라디오버튼)
        var ptFee = GetFeeTypePoint(tbOrder.FeeType);
        if (!string.IsNullOrEmpty(ptFee.sErr))
            return new StdResult_Status(StdResult.Fail, ptFee.sErr, "RegistOrderToPopupAsync_05_01");

        IntPtr hWndFeeType = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, ptFee.ptResult);
        if (hWndFeeType == IntPtr.Zero)
            return new StdResult_Status(StdResult.Fail, "운송비구분 라디오버튼 핸들 획득 실패", "RegistOrderToPopupAsync_05_02");

        result = await Simulation_Mouse.SetCheckBtnStatusAsync(hWndFeeType, true);
        if (result.Result != StdResult.Success)
            return new StdResult_Status(StdResult.Fail, "운송비구분 라디오버튼 설정 실패", "RegistOrderToPopupAsync_05_03");

        Debug.WriteLine($"[{m_Context.AppName}] 5-1. 운송비구분 설정 완료: {tbOrder.FeeType}");

        // 5-2. 운송비 합계 (Edit) - 합계만 입력하면 나머지 자동계산
        IntPtr hWndFeeTotal = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_운송비Edit_ptRel합계);
        if (hWndFeeTotal == IntPtr.Zero)
            return new StdResult_Status(StdResult.Fail, "운송비합계 Edit 핸들 획득 실패", "RegistOrderToPopupAsync_05_04");

        //Std32Key_Msg.KeyPost_Digit(hWndFeeTotal, (uint)tbOrder.FeeTotal); // 2 입력 - 이걸로 해도 됨
        await OfrWork_Common.WriteEditBox_ToHndleAsyncUpdate(hWndFeeTotal, tbOrder.FeeTotal.ToString());
        Std32Key_Msg.KeyPost_Click(hWndFeeTotal, StdCommon32.VK_RETURN); // Enter → 자동계산 트리거

        // 수수료 - 추후 구현예정

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
        // 저장 버튼 선택: OrderState가 "접수"이면 접수저장, 나머지는 대기저장
        bool bReceiptState = (tbOrder.OrderState == "접수");
        Draw.Point ptRelSave = bReceiptState
            ? m_FileInfo.접수등록Wnd_CmnBtn_ptRel접수저장
            : m_FileInfo.접수등록Wnd_CmnBtn_ptRel대기저장;

        Debug.WriteLine($"[{m_Context.AppName}] 종료 작업... OrderState={tbOrder.OrderState}, 저장모드={(bReceiptState ? "접수저장" : "대기저장")}");

        // 저장 버튼 핸들 얻기
        IntPtr hWndSave = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, ptRelSave);
        if (hWndSave == IntPtr.Zero)
        {
            Debug.WriteLine($"[{m_Context.AppName}] 저장 버튼 핸들 못찾음");
            return new StdResult_Status(StdResult.Fail, "저장 버튼 핸들 못찾음", "RegistOrderToPopupAsync_06_01");
        }

        // 저장 버튼 클릭 및 창 닫힘 확인
        bool bClosed = await ClickNWaitWindowClosedAsync(hWndSave, hWndPopup, ctrl, bReceiptState);

        if (bClosed)
        {
            Debug.WriteLine($"[{m_Context.AppName}] 저장 완료, 팝업창 닫힘 확인");
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

            // 3. 화물번호 OFR
            StdResult_String resultSeqno = await Get화물번호Async(0, ctrl);
            if (string.IsNullOrEmpty(resultSeqno.strResult))
            {
                return new StdResult_Status(StdResult.Fail, $"화물번호 획득 실패: {resultSeqno.sErr}");
            }

            Debug.WriteLine($"[{m_Context.AppName}] 주문 등록 완료 - 화물번호: {resultSeqno.strResult}");

            // 4. Kai DB의 Cargo24 필드 업데이트
            // 4-1. 사전 체크
            if (item.NewOrder.KeyCode <= 0)
            {
                Debug.WriteLine($"[{m_Context.AppName}] KeyCode 없음 - Kai DB에 없는 주문");
                return new StdResult_Status(StdResult.Fail, "Kai DB에 없는 주문입니다");
            }

            if (!string.IsNullOrEmpty(item.NewOrder.Cargo24))
            {
                Debug.WriteLine($"[{m_Context.AppName}] 이미 등록된 화물번호: {item.NewOrder.Cargo24}");
                return new StdResult_Status(StdResult.Skip, "이미 Cargo24 번호가 등록되어 있습니다");
            }

            if (CommonVars.s_SrGClient == null || !CommonVars.s_SrGClient.m_bLoginSignalR)
            {
                Debug.WriteLine($"[{m_Context.AppName}] SignalR 연결 안됨");
                return new StdResult_Status(StdResult.Fail, "서버 연결이 끊어졌습니다");
            }

            // 4-2. 업데이트 실행
            item.NewOrder.Cargo24 = resultSeqno.strResult;

            StdResult_Int resultUpdate = await CommonVars.s_SrGClient.SrResult_Order_UpdateRowAsync_Today_WithRequestId(item.NewOrder);

            if (resultUpdate.nResult < 0 || !string.IsNullOrEmpty(resultUpdate.sErr))
            {
                Debug.WriteLine($"[{m_Context.AppName}] Kai DB 업데이트 실패: {resultUpdate.sErr}");
                return new StdResult_Status(StdResult.Fail, $"Kai DB 업데이트 실패: {resultUpdate.sErr}");
            }

            Debug.WriteLine($"[{m_Context.AppName}] Kai DB 업데이트 성공 - Cargo24: {resultSeqno.strResult}");

            return new StdResult_Status(StdResult.Success, $"저장 완료 (화물번호: {resultSeqno.strResult})");
        }
        else
        {
            Debug.WriteLine($"[{m_Context.AppName}] 팝업창 닫기 실패");
            return new StdResult_Status(StdResult.Fail, "팝업창 닫기 실패", "RegistOrderToPopupAsync_06_02");
        }

        //MsgBox("Here");
        return new StdResult_Status(StdResult.Success);
        #endregion
    }
    catch (Exception ex)
    {
        return new StdResult_Status(StdResult.Fail, StdUtil.GetExceptionMessage(ex), "RegistOrderToPopupAsync_999");
    }
}