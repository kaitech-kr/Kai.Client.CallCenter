using System;
using System.Diagnostics;
using System.Threading.Tasks;

//Update 오더
public async Task<StdResult_NulBool> 접수Wnd_UpdateOrderAsync(
    string sWantState, AutoAlloc autoAlloc, AutoAllocResult_Datagrid dgInfo, CancelTokenControl ctrl)
{

    try
    {



        #region 출발지
        await ctrl.WaitIfPausedOrCancelledAsync();

        // 고객명
        if (tbOrder.StartCustName != (sTmp = Std32Window.GetWindowCaption(wndRcpt.출발지_hWnd고객명)))
        {
            nChanged = nChanged == 0 ? 10 : nChanged;

            // 고객명
            sSearch = NwCommon.GetInsungTextForSearch(tbOrder.StartCustName, tbOrder.StartChargeName);
            resultBool = await OfrWork_Common.WriteEditBox_ToHndleAsync(wndRcpt.출발지_hWnd고객명, sSearch);
            if (resultBool == null || !resultBool.bResult)
                return new StdResult_NulBool($"고객명 입력실패: {tbOrder.StartCustName} <=> {sTmp}", "NwIsAct_ReceiptPage/접수Wnd_UpdateOrderAsync_20");

            // 검색
            resultSearch = await GetCustSearchTypeAsync(wndRcpt.출발지_hWnd고객명, wndRcpt.출발지_hWnd동명);
            if (resultSearch.resultTye == AutoAlloc_CustSearch.Null)
                return new StdResult_NulBool($"고객명 검색실패: {tbOrder.StartCustName} <=> {sTmp}", "NwIsAct_ReceiptPage/접수Wnd_UpdateOrderAsync_21");


            // 결과
            switch (resultSearch.resultTye)
            {
                case AutoAlloc_CustSearch.One: break;

                case AutoAlloc_CustSearch.Multi: // 고객명 복수
                    resultError = await 고객검색Wnd_RegistOrderAsync(resultSearch, ctrl);

                    custSearch = new CustSearch(resultSearch.hWndResult, fInfo);
                    //await Task.Delay(1000); // ~초 대기
                    MsgBox($"resultSearch: {resultSearch.resultTye}", "resultSearch_출발지_001");
                    Simulation_Mouse.SafeMousePost_ClickLeft(custSearch.hWndBtnClose);
                    throw new OperationCanceledException();

                case AutoAlloc_CustSearch.None:
                    custRegWnd = new CustRegWnd(resultSearch.hWndResult, fInfo);
                    await Task.Delay(1000); // ~초 대기
                    MsgBox($"resultSearch: {resultSearch.resultTye}", "resultSearch_출발지_002");
                    Simulation_Mouse.SafeMousePost_ClickLeft(custRegWnd.hWndBtnClose);
                    throw new OperationCanceledException();
            }
        }

        // 고객명
        if (tbOrder.StartCustName != (sTmp = Std32Window.GetWindowCaption(wndRcpt.출발지_hWnd고객명)))
        {
            nChanged = nChanged == 0 ? 11 : nChanged;

            for (int i = 0; i < c_nRepeatShort; i++)
            {
                await OfrWork_Common.WriteEditBox_ToHndleAsync(wndRcpt.출발지_hWnd고객명, tbOrder.StartCustName); // 출발지고객명
                if (tbOrder.StartCustName == (sTmp = Std32Window.GetWindowCaption(wndRcpt.출발지_hWnd고객명))) break;

                await Task.Delay(c_nWaitNormal);
            }

            if (tbOrder.StartCustName != sTmp)
                return new StdResult_NulBool($"출발지고객명 입력실패: {tbOrder.StartCustName}", "NwIsAct_ReceiptPage/접수Wnd_UpdateOrderAsync_23");
        }

        // 출발지동명
        if (tbOrder.StartDongBasic != (sTmp = Std32Window.GetWindowCaption(wndRcpt.출발지_hWnd동명)))
        {
            nChanged = nChanged == 0 ? 12 : nChanged;

            for (int i = 0; i < c_nRepeatShort; i++)
            {
                await OfrWork_Common.WriteEditBox_ToHndleAsync(wndRcpt.출발지_hWnd동명, tbOrder.StartDongBasic); // 출발지동명 
                if (tbOrder.StartDongBasic == (sTmp = Std32Window.GetWindowCaption(wndRcpt.출발지_hWnd동명))) break;

                await Task.Delay(c_nWaitNormal);
            }

            if (tbOrder.StartDongBasic != sTmp)
                return new StdResult_NulBool($"출발지고객명 입력실패: {tbOrder.StartCustName}", "NwIsAct_ReceiptPage/접수Wnd_UpdateOrderAsync_24");
        }

        // 전화1
        if (tbOrder.StartTelNo != (sTmp = StdConvert.MakePhoneNumberToDigit(Std32Window.GetWindowCaption(wndRcpt.출발지_hWnd전화1))))
        {
            nChanged = nChanged == 0 ? 13 : nChanged;

            for (int i = 0; i < c_nRepeatShort; i++)
            {
                await OfrWork_Common.WriteEditBox_ToHndleAsync_WithEnterKeyWait(wndRcpt.출발지_hWnd전화1, tbOrder.StartTelNo);
                if (tbOrder.StartTelNo == (sTmp = StdConvert.MakePhoneNumberToDigit(Std32Window.GetWindowCaption(wndRcpt.출발지_hWnd전화1)))) break;

                await Task.Delay(c_nWaitNormal);
            }

            if (tbOrder.StartTelNo != sTmp)
                return new StdResult_NulBool($"전화1 입력실패: {tbOrder.StartTelNo}", "NwIsAct_ReceiptPage/접수Wnd_UpdateOrderAsync_25");
        }

        // 전화2
        if (tbOrder.StartTelNo2 != (sTmp = StdConvert.MakePhoneNumberToDigit(Std32Window.GetWindowCaption(wndRcpt.출발지_hWnd전화2))))
        {
            nChanged = nChanged == 0 ? 14 : nChanged;

            for (int i = 0; i < c_nRepeatShort; i++)
            {
                await OfrWork_Common.WriteEditBox_ToHndleAsync_WithEnterKeyWait(wndRcpt.출발지_hWnd전화2, tbOrder.StartTelNo2);
                if (tbOrder.StartTelNo2 == (sTmp = StdConvert.MakePhoneNumberToDigit(Std32Window.GetWindowCaption(wndRcpt.출발지_hWnd전화2)))) break;

                await Task.Delay(c_nWaitNormal);
            }

            if (tbOrder.StartTelNo2 != sTmp)
                return new StdResult_NulBool($"전화2 입력실패: {tbOrder.StartTelNo2}", "NwIsAct_ReceiptPage/접수Wnd_UpdateOrderAsync_26");
        }

        // 출발지부서
        if (tbOrder.StartDeptName != (sTmp = Std32Window.GetWindowCaption(wndRcpt.출발지_hWnd부서))) // 출발지_hWnd부서
        {
            nChanged = nChanged == 0 ? 15 : nChanged;

            for (int i = 0; i < c_nRepeatShort; i++)
            {
                await OfrWork_Common.WriteEditBox_ToHndleAsync_WithEnterKeyWait(wndRcpt.출발지_hWnd부서, tbOrder.StartDeptName);
                if (tbOrder.StartDeptName == (sTmp = Std32Window.GetWindowCaption(wndRcpt.출발지_hWnd부서))) break;

                await Task.Delay(c_nWaitNormal);
            }

            if (tbOrder.StartDeptName != sTmp)
                return new StdResult_NulBool($"출발지부서 입력실패: {tbOrder.StartDeptName}", "NwIsAct_ReceiptPage/접수Wnd_UpdateOrderAsync_27");
        }

        // 출발지담당
        if (tbOrder.StartChargeName != (sTmp = Std32Window.GetWindowCaption(wndRcpt.출발지_hWnd담당))) // 출발지담당
        {
            nChanged = nChanged == 0 ? 16 : nChanged;

            for (int i = 0; i < c_nRepeatShort; i++)
            {
                await OfrWork_Common.WriteEditBox_ToHndleAsync_WithEnterKeyWait(wndRcpt.출발지_hWnd담당, tbOrder.StartChargeName);
                if (tbOrder.StartChargeName == (sTmp = Std32Window.GetWindowCaption(wndRcpt.출발지_hWnd담당))) break;

                await Task.Delay(c_nWaitNormal);
            }

            await OfrWork_Common.WriteEditBox_ToHndleAsync_WithEnterKeyWait(wndRcpt.출발지_hWnd담당, tbOrder.StartChargeName);
            if (tbOrder.StartChargeName != sTmp)
                return new StdResult_NulBool($"출발지담당 입력실패: {tbOrder.StartChargeName}", "NwIsAct_ReceiptPage/접수Wnd_UpdateOrderAsync_28");
        }

        // (주소) 꼭 쓰고 싶다면 고객수정에서...

        // 출발지위치
        if (tbOrder.StartDetailAddr != (sTmp = Std32Window.GetWindowCaption(wndRcpt.출발지_hWnd위치)))
        {
            nChanged = nChanged == 0 ? 17 : nChanged;

            for (int i = 0; i < c_nRepeatShort; i++)
            {
                await OfrWork_Common.WriteEditBox_ToHndleAsync(wndRcpt.출발지_hWnd위치, tbOrder.StartDetailAddr);
                if (tbOrder.StartDetailAddr == (sTmp = Std32Window.GetWindowCaption(wndRcpt.출발지_hWnd위치))) break;

                await Task.Delay(c_nWaitNormal);
            }

            if (tbOrder.StartDetailAddr != sTmp)
                return new StdResult_NulBool($"출발지위치 입력실패: {tbOrder.StartDetailAddr}", "NwIsAct_ReceiptPage/접수Wnd_UpdateOrderAsync_29");
        }
        #endregion

        #region 도착지
        await ctrl.WaitIfPausedOrCancelledAsync();

        // 고객명
        if (tbOrder.DestCustName != (sTmp = Std32Window.GetWindowCaption(wndRcpt.도착지_hWnd고객명)))
        {
            nChanged = nChanged == 0 ? 20 : nChanged;

            // 고객명
            sSearch = NwCommon.GetInsungTextForSearch(tbOrder.DestCustName, tbOrder.DestChargeName);
            resultBool = await OfrWork_Common.WriteEditBox_ToHndleAsync(wndRcpt.도착지_hWnd고객명, sSearch);
            if (resultBool == null || !resultBool.bResult)
                return new StdResult_NulBool($"고객명 입력실패: {tbOrder.DestCustName}", "NwIsAct_ReceiptPage/접수Wnd_UpdateOrderAsync_30");

            // 검색
            resultSearch = await GetCustSearchTypeAsync(wndRcpt.도착지_hWnd고객명, wndRcpt.도착지_hWnd동명);
            if (resultSearch.resultTye == AutoAlloc_CustSearch.Null)
                return new StdResult_NulBool($"고객명 검색실패: {tbOrder.DestCustName}", "NwIsAct_ReceiptPage/접수Wnd_UpdateOrderAsync_31");

            // 결과
            switch (resultSearch.resultTye)
            {
                case AutoAlloc_CustSearch.One: break;

                case AutoAlloc_CustSearch.Multi: // 고객명 복수
                    resultError = await 고객검색Wnd_RegistOrderAsync(resultSearch, ctrl);

                    custSearch = new CustSearch(resultSearch.hWndResult, fInfo);
                    //await Task.Delay(1000); // ~초 대기
                    MsgBox($"resultSearch: {resultSearch.resultTye}", "resultSearch_도착지_001");
                    Simulation_Mouse.SafeMousePost_ClickLeft(custSearch.hWndBtnClose);
                    throw new OperationCanceledException();

                case AutoAlloc_CustSearch.None:
                    custRegWnd = new CustRegWnd(resultSearch.hWndResult, fInfo);
                    await Task.Delay(1000); // ~초 대기
                    MsgBox($"resultSearch: {resultSearch.resultTye}", "resultSearch_도착지_002");
                    Simulation_Mouse.SafeMousePost_ClickLeft(custRegWnd.hWndBtnClose);
                    throw new OperationCanceledException();
            }
        }

        // 고객명
        if (tbOrder.DestCustName != (sTmp = Std32Window.GetWindowCaption(wndRcpt.도착지_hWnd고객명)))
        {
            nChanged = nChanged == 0 ? 21 : nChanged;

            for (int i = 0; i < c_nRepeatShort; i++)
            {
                await OfrWork_Common.WriteEditBox_ToHndleAsync(wndRcpt.도착지_hWnd고객명, tbOrder.DestCustName); // 출발지_hWnd동명 
                if (tbOrder.DestCustName == (sTmp = Std32Window.GetWindowCaption(wndRcpt.도착지_hWnd고객명))) break;

                await Task.Delay(c_nWaitNormal);
            }

            if (tbOrder.DestCustName != sTmp)
                return new StdResult_NulBool($"고객명 입력실패: {tbOrder.DestCustName}", "NwIsAct_ReceiptPage/접수Wnd_UpdateOrderAsync_33");
        }

        // 동명
        if (tbOrder.DestDongBasic != (sTmp = Std32Window.GetWindowCaption(wndRcpt.도착지_hWnd동명)))
        {
            nChanged = nChanged == 0 ? 22 : nChanged;

            for (int i = 0; i < c_nRepeatShort; i++)
            {
                await OfrWork_Common.WriteEditBox_ToHndleAsync(wndRcpt.도착지_hWnd동명, tbOrder.DestDongBasic); // 도착지_hWnd동명 
                if (tbOrder.DestDongBasic == (sTmp = Std32Window.GetWindowCaption(wndRcpt.도착지_hWnd동명))) break;

                await Task.Delay(c_nWaitNormal);
            }

            if (tbOrder.DestDongBasic != sTmp)
                return new StdResult_NulBool($"동명 입력실패: {tbOrder.DestDongBasic}", "NwIsAct_ReceiptPage/접수Wnd_UpdateOrderAsync_34");
        }

        // 전화1
        if (tbOrder.DestTelNo != (sTmp = StdConvert.MakePhoneNumberToDigit(Std32Window.GetWindowCaption(wndRcpt.도착지_hWnd전화1))))
        {
            nChanged = nChanged == 0 ? 23 : nChanged;

            for (int i = 0; i < c_nRepeatShort; i++)
            {
                await OfrWork_Common.WriteEditBox_ToHndleAsync_WithEnterKeyWait(wndRcpt.도착지_hWnd전화1, tbOrder.DestTelNo);
                if (tbOrder.DestTelNo == (sTmp = StdConvert.MakePhoneNumberToDigit(Std32Window.GetWindowCaption(wndRcpt.도착지_hWnd전화1)))) break;

                await Task.Delay(c_nWaitNormal);
            }

            if (tbOrder.DestTelNo != sTmp)
                return new StdResult_NulBool($"전화1 입력실패: {tbOrder.DestTelNo}", "NwIsAct_ReceiptPage/접수Wnd_UpdateOrderAsync_35");
        }

        // 전화2
        if (tbOrder.DestTelNo2 != (sTmp = StdConvert.MakePhoneNumberToDigit(Std32Window.GetWindowCaption(wndRcpt.도착지_hWnd전화2)))) // 도착지_hWnd전화2
        {
            nChanged = nChanged == 0 ? 24 : nChanged;

            for (int i = 0; i < c_nRepeatShort; i++)
            {
                await OfrWork_Common.WriteEditBox_ToHndleAsync_WithEnterKeyWait(wndRcpt.도착지_hWnd전화2, tbOrder.DestTelNo2);
                if (tbOrder.DestTelNo2 == (sTmp = StdConvert.MakePhoneNumberToDigit(Std32Window.GetWindowCaption(wndRcpt.도착지_hWnd전화2)))) break;

                await Task.Delay(c_nWaitNormal);
            }

            if (tbOrder.DestTelNo2 != sTmp)
                return new StdResult_NulBool($"전화2 입력실패: {tbOrder.DestTelNo2}", "NwIsAct_ReceiptPage/접수Wnd_UpdateOrderAsync_36");
        }

        // 도착지부서
        if (tbOrder.DestDeptName != (sTmp = Std32Window.GetWindowCaption(wndRcpt.도착지_hWnd부서)))
        {
            nChanged = nChanged == 0 ? 25 : nChanged;

            for (int i = 0; i < c_nRepeatShort; i++)
            {
                await OfrWork_Common.WriteEditBox_ToHndleAsync_WithEnterKeyWait(wndRcpt.도착지_hWnd부서, tbOrder.DestDeptName);
                if (tbOrder.DestDeptName == (sTmp = Std32Window.GetWindowCaption(wndRcpt.도착지_hWnd부서))) break;

                await Task.Delay(c_nWaitNormal);
            }

            if (tbOrder.DestDeptName != sTmp)
                return new StdResult_NulBool($"도착지부서 입력실패: {tbOrder.DestDeptName}", "NwIsAct_ReceiptPage/접수Wnd_UpdateOrderAsync_37");
        }

        // 도착지담당
        if (tbOrder.DestChargeName != (sTmp = Std32Window.GetWindowCaption(wndRcpt.도착지_hWnd담당)))
        {
            nChanged = nChanged == 0 ? 26 : nChanged;

            for (int i = 0; i < c_nRepeatShort; i++)
            {
                await OfrWork_Common.WriteEditBox_ToHndleAsync(wndRcpt.도착지_hWnd담당, tbOrder.DestChargeName);
                if (tbOrder.DestChargeName == (sTmp = Std32Window.GetWindowCaption(wndRcpt.도착지_hWnd담당))) break;

                await Task.Delay(c_nWaitNormal);
            }

            if (tbOrder.DestChargeName != sTmp)
                return new StdResult_NulBool($"도착지담당 입력실패: {tbOrder.DestChargeName}", "NwIsAct_ReceiptPage/접수Wnd_UpdateOrderAsync_38");
        }

        // (주소) 꼭 쓰고 싶다면 고객수정에서...

        // 도착지위치
        if (tbOrder.DestDetailAddr != (sTmp = Std32Window.GetWindowCaption(wndRcpt.도착지_hWnd위치)))
        {
            nChanged = nChanged == 0 ? 27 : nChanged;

            for (int i = 0; i < c_nRepeatShort; i++)
            {
                await OfrWork_Common.WriteEditBox_ToHndleAsync(wndRcpt.도착지_hWnd위치, tbOrder.DestDetailAddr);
                if (tbOrder.DestDetailAddr == (sTmp = Std32Window.GetWindowCaption(wndRcpt.도착지_hWnd위치))) break;

                await Task.Delay(c_nWaitNormal);
            }

            if (tbOrder.DestDetailAddr != sTmp)
                return new StdResult_NulBool($"도착지위치 입력실패: {tbOrder.DestDetailAddr}", "NwIsAct_ReceiptPage/접수Wnd_UpdateOrderAsync_39");
        }
        #endregion

        #region Region.예약, SMS, 적요, 공유...
        await ctrl.WaitIfPausedOrCancelledAsync();

        // 적요
        if (tbOrder.OrderRemarks != (sTmp = Std32Window.GetWindowCaption(wndRcpt.우측상단_hWnd적요))) // 우측상단_hWnd적요
        {
            nChanged = nChanged == 0 ? 30 : nChanged;

            for (int i = 0; i < c_nRepeatShort; i++)
            {
                await OfrWork_Common.WriteEditBox_ToHndleAsync(wndRcpt.우측상단_hWnd적요, tbOrder.OrderRemarks);
                if (tbOrder.OrderRemarks == (sTmp = Std32Window.GetWindowCaption(wndRcpt.우측상단_hWnd적요))) break;

                await Task.Delay(c_nWaitNormal);
            }

            if (tbOrder.OrderRemarks != sTmp)
                return new StdResult_NulBool($"적요 입력실패: {tbOrder.OrderRemarks}", "NwIsAct_ReceiptPage/접수Wnd_UpdateOrderAsync_40");
        }

        // 공유
        resultNulBool = await OfrWork_Insungs.OfrImgChkValue_RectInBitmapAsync(bmpWnd, fInfo.접수등록Wnd_우측상단_rcChkRel공유);
        if (resultNulBool.bResult == null) return resultNulBool;

        if (tbOrder.Share != (bTmp = StdConvert.NullableBoolToBool(resultNulBool.bResult)))
        {
            nChanged = nChanged == 0 ? 31 : nChanged;

            for (int i = 0; i < c_nRepeatShort; i++)
            {
                resultError = await this.SetCheckBox_StatusAsync(wndRcpt.TopWnd_hWnd, fInfo.접수등록Wnd_우측상단_rcChkRel공유, tbOrder.Share);
                if (resultError == null) break;

                await Task.Delay(c_nWaitNormal);
            }

            if (resultError != null)
                return new StdResult_NulBool($"공유체크 입력실패: {resultNulBool.bResult}", "NwIsAct_ReceiptPage/접수Wnd_UpdateOrderAsync_41");
        }

        // 요금
        resultNulBool = await IsChecked요금종류_InGroupAsync(bmpWnd, wndRcpt.우측상단_btns요금종류, tbOrder.FeeType);
        if (resultNulBool.bResult == null) return resultNulBool;

        if (!StdConvert.NullableBoolToBool(resultNulBool.bResult))
        {
            nChanged = nChanged == 0 ? 32 : nChanged;

            for (int i = 0; i < c_nRepeatShort; i++)
            {
                resultError = await this.SetGroupFeeTypeAsync(bmpWnd, wndRcpt.우측상단_btns요금종류, tbOrder.FeeType);
                if (resultError == null) break;

                await Task.Delay(c_nWaitNormal);
            }

            if (resultError != null)
                return new StdResult_NulBool($"공유체크 입력실패: {resultNulBool.bResult}", "NwIsAct_ReceiptPage/접수Wnd_UpdateOrderAsync_42");
        }

        // 차량
        resultNulBool = await IsChecked차량종류_InGroupAsync(bmpWnd, wndRcpt.우측상단_btns차량종류, tbOrder.CarType);
        if (resultNulBool.bResult == null) return resultNulBool;

        if (!StdConvert.NullableBoolToBool(resultNulBool.bResult)) // 체크가 안되있으면
        {
            nChanged = nChanged == 0 ? 33 : nChanged;

            for (int i = 0; i < c_nRepeatShort; i++)
            {
                resultError = await this.SetGroupCarTypeAsync(bmpWnd, wndRcpt.우측상단_btns차량종류, tbOrder.CarType);
                if (resultError == null) break;

                await Task.Delay(c_nWaitNormal);
            }

            if (resultError != null)
                return new StdResult_NulBool(resultError.sErr + resultError.sPos, "NwIsAct_ReceiptPage/접수Wnd_UpdateOrderAsync_42");


            if (tbOrder.CarType == "트럭")
            {
                try
                {
                    // 트럭톤수 설정
                    for (int i = 0; i < 100; i++)
                    {
                        await Task.Delay(c_nWaitVeryShort);
                        hWndTmp = Std32Window.GetWndHandle_FromRelDrawPt(wndRcpt.TopWnd_hWnd, fInfo.접수등록Wnd_우측상단_ptChkRel차량톤수);
                        //Debug.WriteLine($"[{i}]: {hWndTmp}, {wndRcpt.우측상단_hWnd플럭제외}"); // Test

                        if (hWndTmp != wndRcpt.우측상단_hWnd플럭제외) // 우측상단_hWnd플럭제외 창이 가려지면
                        {
                            Simulation_Mouse.SafeBlockInputStart();
                            break;
                        }
                    }
                    //Debug.WriteLine($"2[]: {hWndTmp}, {wndRcpt.우측상단_hWnd플럭제외}"); // Test

                    // 펼쳐젔나 체크
                    if (hWndTmp == wndRcpt.우측상단_hWnd플럭제외)
                        return new StdResult_NulBool(resultError.sErr + resultError.sPos, "NwIsAct_ReceiptPage/접수Wnd_UpdateOrderAsync_43");

                    int index = Get차량톤수Index(tbOrder.CarWeight);
                    //Std32Cursor.SetCursorPos_RelDrawPos(hWndTmp, 0, 0); // Test
                    //Std32Cursor.SetCursorPos_RelDrawPt(hWndTmp, fInfo.접수등록Wnd_Common_ptComboBox[index]); // Test
                    Std32Mouse_Post.MouseMix_ClickLeft_ptRel(hWndTmp, fInfo.접수등록Wnd_Common_ptComboBox[index]); // 체크박스 클릭
                    Simulation_Mouse.SafeBlockInputStop();
                    await Task.Delay(100);

                    // 트럭상세 설정
                    for (int i = 0; i < 50; i++)
                    {
                        await Task.Delay(30);
                        hWndTmp = Std32Window.GetWndHandle_FromRelDrawPt(wndRcpt.TopWnd_hWnd, fInfo.접수등록Wnd_우측상단_ptChkRel트럭상세);
                        if (hWndTmp != wndRcpt.우측상단_hWnd플럭제외)
                        {
                            Simulation_Mouse.SafeBlockInputStart();
                            break;
                        }
                    }

                    // 펼쳐젔나 체크
                    if (hWndTmp == wndRcpt.우측상단_hWnd인수증필)
                        return new StdResult_NulBool("트럭상세 입력실패", "NwIsAct_ReceiptPage /접수Wnd_UpdateOrderAsync_44");

                    index = Get트럭상세Index(tbOrder.TruckDetail);
                    //Std32Cursor.SetCursorPos_RelDrawPos(hWndTmp, 0, 0); // Test
                    Std32Mouse_Post.MouseMix_ClickLeft_ptRel(hWndTmp, fInfo.접수등록Wnd_Common_ptComboBox[index]);
                    Simulation_Mouse.SafeBlockInputStop();
                    await Task.Delay(100);
                }
                finally
                {
                    Simulation_Mouse.SafeBlockInputStop();
                }
            }
        }
        else
        {
            if (tbOrder.CarType == "트럭")
            {
                Debug.WriteLine($"코딩 해야함(지금은 패쓰)", "NwIsAct_ReceiptPage/접수Wnd_UpdateOrderAsync_48"); // Test - 콤보박스 헤더를 읽어야함.
            }
        }

        // 배송타입
        resultNulBool = await IsChecked배송타입_InGroupAsync(bmpWnd, wndRcpt.우측상단_btns배송종류, tbOrder.DeliverType);
        if (resultNulBool.bResult == null)
            return new StdResult_NulBool(resultError.sErr + resultError.sPos, "NwIsAct_ReceiptPage/접수Wnd_UpdateOrderAsync_44");

        if (!StdConvert.NullableBoolToBool(resultNulBool.bResult))
        {
            nChanged = nChanged == 0 ? 34 : nChanged;

            for (int i = 0; i < c_nRepeatShort; i++)
            {
                resultError = await this.SetGroupDeliverTypeAsync(bmpWnd, wndRcpt.우측상단_btns배송종류, tbOrder.DeliverType);
                if (resultError == null) break;

                await Task.Delay(c_nWaitNormal);
            }

            if (resultError != null)
                return new StdResult_NulBool(resultError.sErr + resultError.sPos, "NwIsAct_ReceiptPage/접수Wnd_UpdateOrderAsync_45");
        }

        // 계산서발행 - 차량 인성프로그램 에서 설정후에 처리
        resultNulBool = await OfrWork_Insungs.OfrImgChkValue_RectInBitmapAsync(bmpWnd, fInfo.접수등록Wnd_우측상단_rcChkRel계산서);
        if (resultNulBool.bResult == null)
            return new StdResult_NulBool(resultNulBool.sErr + resultNulBool.sPos, "NwIsAct_ReceiptPage/접수Wnd_UpdateOrderAsync_46");

        if (tbOrder.TaxBill != (bTmp = StdConvert.NullableBoolToBool(resultNulBool.bResult)))
        {
            nChanged = nChanged == 0 ? 35 : nChanged;

            for (int i = 0; i < c_nRepeatShort; i++)
            {
                resultError = await this.SetCheckBox_StatusAsync(wndRcpt.TopWnd_hWnd, fInfo.접수등록Wnd_우측상단_rcChkRel계산서, tbOrder.TaxBill);
                if (resultError == null) break;

                await Task.Delay(c_nWaitNormal);
            }

            if (resultError != null)
                return new StdResult_NulBool(resultError.sErr + resultError.sPos, "NwIsAct_ReceiptPage/접수Wnd_UpdateOrderAsync_47");
        }
        #endregion Region.End -예약, SMS, 적요, 공유...

        #region Region.요금    
        await ctrl.WaitIfPausedOrCancelledAsync();

        // 기본요금
        nTmp = StdConvert.StringWonFormatToInt(Std32Window.GetWindowCaption(wndRcpt.요금그룹_hWnd기본요금));
        if (tbOrder.FeeBasic != nTmp)
        {
            nChanged = nChanged == 0 ? 40 : nChanged;

            for (int i = 0; i < c_nRepeatShort; i++)
            {
                await Simulation_Keyboard.KeyPost_DeleteNAsciiTextAsync(wndRcpt.요금그룹_hWnd기본요금, tbOrder.FeeBasic.ToString(), true); // 기본요금
                nTmp = StdConvert.StringWonFormatToInt(Std32Window.GetWindowCaption(wndRcpt.요금그룹_hWnd기본요금));
                if (tbOrder.FeeBasic == nTmp) break;

                await Task.Delay(c_nWaitNormal);
            }

            if (tbOrder.FeeBasic != nTmp)
                return new StdResult_NulBool("트럭중량 입력실패", "NwIsAct_ReceiptPage/접수Wnd_UpdateOrderAsync_50");
        }

        // 추가금액
        nTmp = StdConvert.StringWonFormatToInt(Std32Window.GetWindowCaption(wndRcpt.요금그룹_hWnd추가금액));
        if (tbOrder.FeePlus != nTmp)
        {
            nChanged = nChanged == 0 ? 41 : nChanged;

            for (int i = 0; i < c_nRepeatShort; i++)
            {
                await Simulation_Keyboard.KeyPost_DeleteNAsciiTextAsync(wndRcpt.요금그룹_hWnd추가금액, tbOrder.FeePlus.ToString(), true); // 추가금액
                nTmp = StdConvert.StringWonFormatToInt(Std32Window.GetWindowCaption(wndRcpt.요금그룹_hWnd추가금액));
                if (tbOrder.FeePlus == nTmp) break;

                await Task.Delay(c_nWaitNormal);
            }

            if (tbOrder.FeePlus != nTmp)
                return new StdResult_NulBool("추가금액 입력실패", "NwIsAct_ReceiptPage/접수Wnd_UpdateOrderAsync_51");
        }

        // 할인금액
        nTmp = StdConvert.StringWonFormatToInt(Std32Window.GetWindowCaption(wndRcpt.요금그룹_hWnd할인금액));
        if (tbOrder.FeeMinus != nTmp)
        {
            nChanged = nChanged == 0 ? 42 : nChanged;

            for (int i = 0; i < c_nRepeatShort; i++)
            {
                await Simulation_Keyboard.KeyPost_DeleteNAsciiTextAsync(wndRcpt.요금그룹_hWnd할인금액, tbOrder.FeeMinus.ToString(), true); // 할인금액
                nTmp = StdConvert.StringWonFormatToInt(Std32Window.GetWindowCaption(wndRcpt.요금그룹_hWnd할인금액));
                if (tbOrder.FeeMinus == nTmp) break;

                await Task.Delay(c_nWaitNormal);
            }

            if (tbOrder.FeeMinus != nTmp)
                return new StdResult_NulBool("할인금액 입력실패", "NwIsAct_ReceiptPage/접수Wnd_UpdateOrderAsync_52");
        }

        // 탁송료
        nTmp = StdConvert.StringWonFormatToInt(Std32Window.GetWindowCaption(wndRcpt.요금그룹_hWnd탁송료));
        if (tbOrder.FeeConn != nTmp)
        {
            nChanged = nChanged == 0 ? 43 : nChanged;

            for (int i = 0; i < c_nRepeatShort; i++)
            {
                await Simulation_Keyboard.KeyPost_DeleteNAsciiTextAsync(wndRcpt.요금그룹_hWnd탁송료, tbOrder.FeeConn.ToString(), true); // 탁송료
                nTmp = StdConvert.StringWonFormatToInt(Std32Window.GetWindowCaption(wndRcpt.요금그룹_hWnd탁송료));
                if (tbOrder.FeeConn == nTmp) break;

                await Task.Delay(c_nWaitNormal);
            }

            if (tbOrder.FeeConn != nTmp)
                return new StdResult_NulBool("탁송료 입력실패", "NwIsAct_ReceiptPage/접수Wnd_UpdateOrderAsync_53");
        }
        #endregion Region.End - 요금

        #region Region.기사 - 직접입력 불가
        bkDrvCode = tbOrder.DriverCode;
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

        #region Region.오더메모
        await ctrl.WaitIfPausedOrCancelledAsync();

        string newMemo = $"{tbOrder.KeyCode}/{tbOrder.OrderMemo}";
        if (newMemo != (sTmp = Std32Window.GetWindowCaption(wndRcpt.우측하단_hWnd오더메모))) // 오더메모
        {
            nChanged = nChanged == 0 ? 50 : nChanged;

            for (int i = 0; i < c_nRepeatShort; i++)
            {
                sTmp = await OfrWork_Common.WriteEditBox_ToHndleAsyncWait(wndRcpt.우측하단_hWnd오더메모, newMemo); // 오더메모
                if (sTmp == newMemo) break;

                await Task.Delay(c_nWaitNormal);
            }

            if (newMemo != sTmp) // 오더메모
                return new StdResult_NulBool("오더메모 입력실패", "NwIsAct_ReceiptPage/접수Wnd_UpdateOrderAsync_60");
        }
        #endregion Region.오더메모 끝

        // 원하는 오더상태가 있으면
        if (!string.IsNullOrEmpty(sWantState))
        {
            switch (sWantState)
            {
                case "접수": await Simulation_Mouse.SafeMousePost_ChkNclickLeft_CenterAsync(wndRcpt.Btn_hWnd접수상태); nChanged = nChanged == 0 ? 101 : nChanged; break;
                case "완료": await Simulation_Mouse.SafeMousePost_ChkNclickLeft_CenterAsync(wndRcpt.Btn_hWnd처리완료); nChanged = nChanged == 0 ? 102 : nChanged; break;
                case "대기": await Simulation_Mouse.SafeMousePost_ChkNclickLeft_CenterAsync(wndRcpt.Btn_hWnd대기); nChanged = nChanged == 0 ? 103 : nChanged; break;
                case "취소": await Simulation_Mouse.SafeMousePost_ChkNclickLeft_CenterAsync(wndRcpt.Btn_hWnd주문취소); nChanged = nChanged == 0 ? 104 : nChanged; break;
                default: ErrMsgBox($"모르는 스위치 케이스: {sWantState}", "NwIsAct_ReceiptPage/접수Wnd_UpdateOrderAsync_900"); break;
            }
            //MsgBox($"sWantState={sWantState}"); // Test
        }

        // Update
        if (nChanged != 0) // 변경내용이 있으니 저장
        {
            Debug.WriteLine($"변경내용이 있읍니다: nChanged={nChanged}"); // Test
            await Task.Delay(c_nWaitNormal);
            bClosed = await this.ClickNWaitWindowChangedAsync_OrFind확인창(wndRcpt.Btn_hWnd저장, wndRcpt.TopWnd_hWnd);

            await Task.Delay(100); // 그래도 확실히
            return new StdResult_NulBool(bClosed); // Test
        }
        else // 변경내용이 없으니 그냥 닫기
        {
            Debug.WriteLine($"변경내용이 없읍니다: nChanged={nChanged}"); // Test
            bClosed = await this.ClickNWaitWindowChangedAsync(wndRcpt.Btn_hWnd닫기, wndRcpt.TopWnd_hWnd);

            await Task.Delay(100); // 그래도 확실히
            return new StdResult_NulBool(bClosed); // Test
        }

        #region Test
        //Debug.WriteLine("테스트 닫기입니다."); // Test
        ////await Task.Delay(1000); 
        //bClosed = await this.ClickNWaitWindowChangedAsync(wndRcpt.Btn_hWnd닫기, wndRcpt.TopWnd_hWnd);
        #endregion
    }
    catch (OperationCanceledException)
    {
        if (!bClosed)
        {
            bClosed = await this.ClickNWaitWindowChangedAsync(wndRcpt.Btn_hWnd닫기, wndRcpt.TopWnd_hWnd);
            if (!bClosed) return new StdResult_NulBool("닫기 실패", "NwIsAct_ReceiptPage/접수Wnd_UpdateOrderAsync_997");
        }

        return new StdResult_NulBool("OperationCanceledException 에러", "NwIsAct_ReceiptPage/접수Wnd_UpdateOrderAsync_998");
    }
    catch (Exception ex)
    {
        return new StdResult_NulBool(StdUtil.GetExceptionMessage(ex), "NwIsAct_ReceiptPage/접수Wnd_UpdateOrderAsync_999");
    }
    finally
    {
        // 누수 방지
        bmpWnd?.Dispose();
    }
}