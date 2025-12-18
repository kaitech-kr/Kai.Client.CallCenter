using Kai.Client.CallCenter.Classes;
using Kai.Client.CallCenter.Classes.Class_Master;
using Kai.Client.CallCenter.OfrWorks;
using Kai.Client.CallCenter.Windows;
using Kai.Common.NetDll_WpfCtrl.NetOFR;
using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models;
using System.Diagnostics;
using System.Windows.Media;
using static Kai.Client.CallCenter.Classes.CommonVars;
using Draw = System.Drawing;

namespace Kai.Client.CallCenter.Networks.NwCargo24s;
#nullable disable

public partial class Cargo24sAct_RcptRegPage
{
    #region 1. Helpers - 공용 헬퍼
    /// <summary>
    /// StatusBtn ã�� ���� �޼��� (�μ� ���� ����)
    /// </summary>
    private async Task<(IntPtr hWnd, StdResult_Error error)> FindStatusButtonAsync(
        string buttonName, Draw.Point checkPoint, string errorCode, bool bWrite, bool bMsgBox, bool withTextValidation = true)
    {
        for (int i = 0; i < c_nRepeatVeryMany; i++)
        {
            IntPtr hWnd = Std32Window.GetWndHandle_FromRelDrawPt(m_Main.TopWnd_hWnd, checkPoint);

            if (hWnd != IntPtr.Zero)
            {
                if (withTextValidation)
                {
                    string text = Std32Window.GetWindowCaption(hWnd);
                    if (text.Contains(buttonName))
                    {
                        //Debug.WriteLine($"[Cargo24sAct_RcptRegPage] {buttonName}��ư ã��: {hWnd:X}, �ؽ�Ʈ: {text}");
                        return (hWnd, null);
                    }
                }
                else
                {
                    //Debug.WriteLine($"[Cargo24sAct_RcptRegPage] {buttonName}��ư ã��: {hWnd:X}");
                    return (hWnd, null);
                }
            }

            await Task.Delay(c_nWaitNormal);
        }

        // ã�� ����
        var error = CommonFuncs_StdResult.ErrMsgResult_Error(
            $"[{m_Context.AppName}/RcptRegPage]{buttonName}��ư ã�����: {checkPoint}",
            errorCode, bWrite, bMsgBox);
        return (IntPtr.Zero, error);
    }

    /// <summary>
    /// Datagrid
    /// </summary>
    /// <param name="columnTexts">OFR�� �о�� �÷� ��� �ؽ�Ʈ �迭</param>
    /// <param name="listLW">�÷� Left/Width ����Ʈ</param>
    /// <returns>���� �̽� �÷��� (None�̸� ����)</returns>
    private CEnum_DgValidationIssue ValidateDatagridState(string[] columnTexts, List<OfrModel_LeftWidth> listLW)
    {
        CEnum_DgValidationIssue issues = CEnum_DgValidationIssue.None;

        // �� �÷� ���� (1���� ���� - 0�� �̻��)
        for (int x = 1; x < m_ReceiptDgHeaderInfos.Length; x++)
        {
            string columnText = columnTexts[x];

            // 1. �÷����� m_ReceiptDgHeaderInfos�� �����ϴ���
            int index = Array.FindIndex(m_ReceiptDgHeaderInfos, h => h.sName == columnText);

            if (index < 0)
            {
                issues |= CEnum_DgValidationIssue.InvalidColumn;
                Debug.WriteLine($"[ValidateDatagridState] ��ȿ���� ���� �÷�[{x}]: '{columnText}'");
                continue;
            }

            // 2. �÷� ������ �´���
            if (index != x)
            {
                issues |= CEnum_DgValidationIssue.WrongOrder;
                Debug.WriteLine($"[ValidateDatagridState] ���� ����ġ[{x}]: '{columnText}' (���� ��ġ={index})");
            }

            // 3. �÷� �ʺ� �´���
            int actualWidth = listLW[x].nWidth;
            int expectedWidth = m_ReceiptDgHeaderInfos[index].nWidth;
            int widthDiff = Math.Abs(actualWidth - expectedWidth);

            if (widthDiff > COLUMN_WIDTH_TOLERANCE)
            {
                issues |= CEnum_DgValidationIssue.WrongWidth;
            }
        }

        if (issues == CEnum_DgValidationIssue.None)
        {
            Debug.WriteLine($"[ValidateDatagridState] Datagrid ���� ����");
        }

        return issues;
    }
    /// <summary>
    /// 접수등록 페이지가 초기화되었는지 확인
    /// </summary>
    public bool IsInitialized()
    {
        return m_RcptPage.TopWnd_hWnd != IntPtr.Zero;
    }

    /// <summary>
    /// 접수등록 페이지 핸들 가져오기
    /// </summary>
    public IntPtr GetHandle()
    {
        return m_RcptPage.TopWnd_hWnd;
    }
    #endregion

    #region 2. DG State - DG오더 UI 상태
    /// <summary>
    /// DG오더의 유효 로우 수 반환
    /// - 배경 밝기(50)보다 어두우면 데이터 있는 로우로 판단
    /// </summary>
    public StdResult_Int GetValidRowCount()
    {
        try
        {
            Draw.Rectangle[,] rects = m_RcptPage.DG오더_rcRelCells;
            if (rects == null)
                return new StdResult_Int("DG오더_rcRelCells 미초기화", "GetValidRowCount_01");

            int nBackgroundBright = 50;
            int nThreshold = nBackgroundBright - 1;
            int nValidRows = 0;

            for (int y = 0; y < m_FileInfo.접수등록Page_DG오더_rowCount; y++)
            {
                int nCurBright = OfrService.GetPixelBrightnessFrmWndHandle(
                    m_RcptPage.DG오더_hWnd,
                    rects[0, y].Right,
                    rects[0, y].Top + 6);

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

    #region 3. Input Helpers - 입력 공용함수
    /// <summary>
    /// 오더상태 체크박스 일괄 설정 (Flags enum 사용)
    /// </summary>
    private async Task<StdResult_Status> Set오더타입Async(IntPtr hWndTop, CEnum_Cg24OrderStatus status, CancelTokenControl ctrl)
    {
        var checkItems = new (CEnum_Cg24OrderStatus flag, Draw.Point pt, string name)[]
        {
            (CEnum_Cg24OrderStatus.공유, m_FileInfo.접수등록Wnd_배송ChkBoxes_ptRel공유, "공유"),
            (CEnum_Cg24OrderStatus.중요오더, m_FileInfo.접수등록Wnd_배송ChkBoxes_ptRel중요오더, "중요오더"),
            (CEnum_Cg24OrderStatus.예약, m_FileInfo.접수등록Wnd_배송ChkBoxes_ptRel예약, "예약"),
            (CEnum_Cg24OrderStatus.긴급, m_FileInfo.접수등록Wnd_배송ChkBoxes_ptRel긴급, "긴급"),
            (CEnum_Cg24OrderStatus.왕복, m_FileInfo.접수등록Wnd_배송ChkBoxes_ptRel왕복, "왕복"),
            (CEnum_Cg24OrderStatus.경유, m_FileInfo.접수등록Wnd_배송ChkBoxes_ptRel경유, "경유"),
        };

        foreach (var (flag, pt, name) in checkItems)
        {
            await ctrl.WaitIfPausedOrCancelledAsync();

            IntPtr hWnd = Std32Window.GetWndHandle_FromRelDrawPt(hWndTop, pt);
            if (hWnd == IntPtr.Zero)
                return new StdResult_Status(StdResult.Fail, $"{name} CheckBox 핸들 획득 실패", "Set오더상태StatusAsync_00");

            bool shouldCheck = status.HasFlag(flag);
            var result = await Simulation_Mouse.SetCheckBtnStatusAsync(hWnd, shouldCheck);
            if (result.Result != StdResult.Success)
                return new StdResult_Status(StdResult.Fail, $"{name} 체크박스 설정 실패", "Set오더상태StatusAsync_01");

            Debug.WriteLine($"[{m_Context.AppName}] {name} 체크박스 설정 완료 (목표={shouldCheck})");
        }

        return new StdResult_Status(StdResult.Success);
    }

    /// <summary>
    /// 오더상태 체크박스 선택적 설정 (isUpdate=true: 현재값 비교 후 변경)
    /// </summary>
    private async Task<(int changeCount, StdResult_Status result)> Set오더타입Async(IntPtr hWndTop, CEnum_Cg24OrderStatus status, bool isUpdate, CancelTokenControl ctrl)
    {
        int changeCount = 0;
        var checkItems = new (CEnum_Cg24OrderStatus flag, Draw.Point pt, string name)[]
        {
            (CEnum_Cg24OrderStatus.공유, m_FileInfo.접수등록Wnd_배송ChkBoxes_ptRel공유, "공유"),
            (CEnum_Cg24OrderStatus.중요오더, m_FileInfo.접수등록Wnd_배송ChkBoxes_ptRel중요오더, "중요오더"),
            (CEnum_Cg24OrderStatus.예약, m_FileInfo.접수등록Wnd_배송ChkBoxes_ptRel예약, "예약"),
            (CEnum_Cg24OrderStatus.긴급, m_FileInfo.접수등록Wnd_배송ChkBoxes_ptRel긴급, "긴급"),
            (CEnum_Cg24OrderStatus.왕복, m_FileInfo.접수등록Wnd_배송ChkBoxes_ptRel왕복, "왕복"),
            (CEnum_Cg24OrderStatus.경유, m_FileInfo.접수등록Wnd_배송ChkBoxes_ptRel경유, "경유"),
        };

        foreach (var (flag, pt, name) in checkItems)
        {
            await ctrl.WaitIfPausedOrCancelledAsync();

            IntPtr hWnd = Std32Window.GetWndHandle_FromRelDrawPt(hWndTop, pt);
            if (hWnd == IntPtr.Zero)
                return (changeCount, new StdResult_Status(StdResult.Fail, $"{name} CheckBox 핸들 획득 실패", "Set오더타입Async_00"));

            bool shouldCheck = status.HasFlag(flag);

            if (isUpdate)
            {
                bool currentCheck = Std32Msg_Send.GetCheckStatus(hWnd) == 1;
                Debug.WriteLine($"[{m_Context.AppName}] 배송_{name} 비교: 화면={currentCheck}, DB={shouldCheck}");
                if (shouldCheck != currentCheck)
                {
                    var result = await Simulation_Mouse.SetCheckBtnStatusAsync(hWnd, shouldCheck);
                    if (result.Result != StdResult.Success)
                        return (changeCount, new StdResult_Status(StdResult.Fail, $"{name} 체크박스 설정 실패", "Set오더타입Async_01"));
                    changeCount++;
                }
            }
            else
            {
                var result = await Simulation_Mouse.SetCheckBtnStatusAsync(hWnd, shouldCheck);
                if (result.Result != StdResult.Success)
                    return (changeCount, new StdResult_Status(StdResult.Fail, $"{name} 체크박스 설정 실패", "Set오더타입Async_02"));
                Debug.WriteLine($"[{m_Context.AppName}] {name} 체크박스 설정 완료 (목표={shouldCheck})");
            }
        }

        return (changeCount, new StdResult_Status(StdResult.Success));
    }

    /// <summary>
    /// 운송비 설정 (운송비구분 라디오버튼 + 운송비합계 Edit)
    /// </summary>
    private async Task<(int changeCount, StdResult_Status result)> Set운송비Async(IntPtr hWndPopup, TbOrder order, bool isUpdate, CancelTokenControl ctrl)
    {
        int changeCount = 0;

        // 1. 운송비구분 (라디오버튼)
        var ptFee = GetFeeTypePoint(order.FeeType);
        if (!string.IsNullOrEmpty(ptFee.sErr))
            return (changeCount, new StdResult_Status(StdResult.Fail, ptFee.sErr, "Set운송비Async_01"));

        IntPtr hWndFeeType = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, ptFee.ptResult);
        if (hWndFeeType == IntPtr.Zero)
            return (changeCount, new StdResult_Status(StdResult.Fail, "운송비구분 라디오버튼 핸들 획득 실패", "Set운송비Async_02"));

        if (isUpdate)
        {
            bool currentFeeChecked = Std32Msg_Send.GetCheckStatus(hWndFeeType) == StdCommon32.BST_CHECKED;
            Debug.WriteLine($"[{m_Context.AppName}] 운송비구분 비교: 화면체크={currentFeeChecked}, DB={order.FeeType}");

            if (!currentFeeChecked)
            {
                var resultFee = await Simulation_Mouse.SetCheckBtnStatusAsync(hWndFeeType, true);
                if (resultFee.Result != StdResult.Success)
                    return (changeCount, new StdResult_Status(StdResult.Fail, "운송비구분 라디오버튼 설정 실패", "Set운송비Async_03"));
                changeCount++;
                Debug.WriteLine($"[{m_Context.AppName}] 운송비구분 변경: {order.FeeType}");
            }
        }
        else
        {
            var resultFee = await Simulation_Mouse.SetCheckBtnStatusAsync(hWndFeeType, true);
            if (resultFee.Result != StdResult.Success)
                return (changeCount, new StdResult_Status(StdResult.Fail, "운송비구분 라디오버튼 설정 실패", "Set운송비Async_04"));
            Debug.WriteLine($"[{m_Context.AppName}] 운송비구분 설정 완료: {order.FeeType}");
        }

        // 2. 운송비합계 (Edit)
        IntPtr hWndFeeTotal = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, m_FileInfo.접수등록Wnd_운송비Edit_ptRel합계);
        if (hWndFeeTotal == IntPtr.Zero)
            return (changeCount, new StdResult_Status(StdResult.Fail, "운송비합계 Edit 핸들 획득 실패", "Set운송비Async_05"));

        string targetFeeTotal = order.FeeTotal.ToString();

        if (isUpdate)
        {
            string currentFeeTotal = Std32Window.GetWindowCaption(hWndFeeTotal) ?? "";
            Debug.WriteLine($"[{m_Context.AppName}] 운송비합계 비교: 화면=\"{currentFeeTotal}\", DB=\"{targetFeeTotal}\"");

            if (currentFeeTotal != targetFeeTotal)
            {
                await OfrWork_Common.WriteEditBox_ToHndleAsyncUpdate(hWndFeeTotal, targetFeeTotal);
                Std32Key_Msg.KeyPost_Click(hWndFeeTotal, StdCommon32.VK_RETURN);
                changeCount++;
                Debug.WriteLine($"[{m_Context.AppName}] 운송비합계 변경: {currentFeeTotal} → {targetFeeTotal}");
            }
        }
        else
        {
            await OfrWork_Common.WriteEditBox_ToHndleAsyncUpdate(hWndFeeTotal, targetFeeTotal);
            Std32Key_Msg.KeyPost_Click(hWndFeeTotal, StdCommon32.VK_RETURN);
            Debug.WriteLine($"[{m_Context.AppName}] 운송비합계 설정 완료: {targetFeeTotal}");
        }

        return (changeCount, new StdResult_Status(StdResult.Success));
    }

    /// <summary>
    /// 주소 검색창 열기 → 검색 → 선택 헬퍼
    /// </summary>
    private async Task<StdResult_Status> SearchAndSelectAddressAsync(
        IntPtr hWndPopup, Draw.Point ptRel조회버튼, string searchAddr, string fieldName, CancelTokenControl ctrl)
    {
        try
        {
            await ctrl.WaitIfPausedOrCancelledAsync();

            // 1. 조회버튼 클릭
            IntPtr hWnd조회버튼 = Std32Window.GetWndHandle_FromRelDrawPt(hWndPopup, ptRel조회버튼);
            if (hWnd조회버튼 == IntPtr.Zero)
            {
                return new StdResult_Status(StdResult.Fail,
                    $"{fieldName} 조회버튼을 찾을 수 없습니다.",
                    "SearchAndSelectAddressAsync_01");
            }

            await Std32Mouse_Post.MousePostAsync_ClickLeft(hWnd조회버튼);
            Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 조회버튼 클릭");

            // 2. 주소검색창 찾기 (최대 50회 대기)
            IntPtr hWndSearch = IntPtr.Zero;
            for (int i = 0; i < c_nRepeatMany; i++)
            {
                await Task.Delay(c_nWaitNormal, ctrl.Token);
                hWndSearch = Std32Window.FindMainWindow_NotTransparent(
                    m_Splash.TopWnd_uProcessId, m_FileInfo.주소검색Wnd_TopWnd_sWndName);

                if (hWndSearch != IntPtr.Zero)
                {
                    string className = Std32Window.GetWindowClassName(hWndSearch);
                    if (className == m_FileInfo.주소검색Wnd_TopWnd_sClassName)
                        break;
                    hWndSearch = IntPtr.Zero;
                }
            }

            if (hWndSearch == IntPtr.Zero)
            {
                return new StdResult_Status(StdResult.Fail,
                    $"{fieldName} 주소검색창이 열리지 않았습니다.",
                    "SearchAndSelectAddressAsync_02");
            }

            Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 주소검색창 열림: {hWndSearch:X}");

            // 3. 검색어 입력
            IntPtr hWnd검색어 = Std32Window.GetWndHandle_FromRelDrawPt(hWndSearch, m_FileInfo.주소검색Wnd_Search_ptRel검색어);
            if (hWnd검색어 == IntPtr.Zero)
            {
                return new StdResult_Status(StdResult.Fail,
                    $"{fieldName} 검색어 입력창을 찾을 수 없습니다.",
                    "SearchAndSelectAddressAsync_03");
            }

            await OfrWork_Common.WriteEditBox_ToHndleAsyncWait(hWnd검색어, searchAddr);
            Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 검색어 입력: {searchAddr}");

            // 4. 조회버튼 클릭
            IntPtr hWnd검색조회 = Std32Window.GetWndHandle_FromRelDrawPt(hWndSearch, m_FileInfo.주소검색Wnd_Search_ptRel조회버튼);
            if (hWnd검색조회 == IntPtr.Zero)
            {
                return new StdResult_Status(StdResult.Fail,
                    $"{fieldName} 검색 조회버튼을 찾을 수 없습니다.",
                    "SearchAndSelectAddressAsync_04");
            }

            await Std32Mouse_Post.MousePostAsync_ClickLeft(hWnd검색조회);
            await Task.Delay(c_nWaitLong, ctrl.Token); // 검색 결과 대기
            Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 검색 조회버튼 클릭");

            // 5. 결과 선택 (첫 번째 행 더블클릭)
            IntPtr hWndDg = Std32Window.GetWndHandle_FromRelDrawPt(hWndSearch, m_FileInfo.주소검색Wnd_Datagrid_ptRelChk);
            if (hWndDg == IntPtr.Zero)
            {
                return new StdResult_Status(StdResult.Fail,
                    $"{fieldName} 검색결과 Datagrid를 찾을 수 없습니다.",
                    "SearchAndSelectAddressAsync_05");
            }

            // 첫 번째 행 더블클릭
            await Simulation_Mouse.SafeMouseSend_DblClickLeft_ptRelAsync(hWndDg, m_FileInfo.주소검색Wnd_Datagrid_rcRelFirstRow.Location);
            Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 첫 번째 행 더블클릭");

            // 6. 주소검색창 닫힘 대기
            for (int i = 0; i < c_nRepeatMany; i++)
            {
                await Task.Delay(c_nWaitNormal, ctrl.Token);
                if (!Std32Window.IsWindow(hWndSearch))
                {
                    Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 주소검색창 닫힘 확인");
                    return new StdResult_Status(StdResult.Success);
                }
            }

            // 창이 안 닫히면 닫기 버튼 클릭 시도
            IntPtr hWnd닫기 = Std32Window.GetWndHandle_FromRelDrawPt(hWndSearch, m_FileInfo.주소검색Wnd_Search_ptRel닫기버튼);
            if (hWnd닫기 != IntPtr.Zero)
            {
                await Std32Mouse_Post.MousePostAsync_ClickLeft(hWnd닫기);
                await Task.Delay(c_nWaitNormal, ctrl.Token);
            }

            return new StdResult_Status(StdResult.Success);
        }
        catch (Exception ex)
        {
            return new StdResult_Status(StdResult.Fail, StdUtil.GetExceptionMessage(ex),"SearchAndSelectAddressAsync_999");
        }
    }

    /// <summary>
    /// EditBox에 값 입력 및 검증 헬퍼 (INSUNG 패턴 참조)
    /// </summary>
    private async Task<StdResult_Status> WriteAndVerifyEditBoxAsync(IntPtr hWnd, string expectedValue, string fieldName, CancelTokenControl ctrl)
    {
        try
        {
            await ctrl.WaitIfPausedOrCancelledAsync();

            if (hWnd == IntPtr.Zero)
            {
                Debug.WriteLine($"[{m_Context.AppName}] {fieldName} EditBox 핸들이 유효하지 않습니다.");
                return new StdResult_Status(StdResult.Fail, $"{fieldName} EditBox를 찾을 수 없습니다.", "WriteAndVerifyEditBoxAsync_00");
            }

            if (expectedValue == null) expectedValue = "";

            // 재시도 루프 (최대 3번)
            for (int i = 0; i < c_nRepeatShort; i++)
            {
                await ctrl.WaitIfPausedOrCancelledAsync();

                // 쓰기 (대기 포함 버전 사용)
                string writtenValue = await OfrWork_Common.WriteEditBox_ToHndleAsyncWait(hWnd, expectedValue);

                // 검증
                if (expectedValue == writtenValue)
                {
                    Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 입력 성공: \"{expectedValue}\"");
                    return new StdResult_Status(StdResult.Success);
                }

                Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 불일치 (재시도 {i + 1}/{c_nRepeatShort}): " +
                              $"예상=\"{expectedValue}\", 실제=\"{writtenValue}\"");

                await Task.Delay(c_nWaitLong, ctrl.Token);
            }

            // 최대 재시도 초과
            Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 입력 실패: 최대 재시도 초과");
            return new StdResult_Status(StdResult.Fail,
                $"{fieldName} 입력 검증 실패: 최대 재시도 초과",
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
    /// 버튼 클릭 후 메인창 닫힐 때까지 확인창/보고창 처리
    /// - 확인창(TMessageForm + &Yes) 나타나면 Yes 클릭
    /// - 보고창(TMessageForm + OK) 나타나면 OK 클릭
    /// - 메인창 닫히면 완료
    /// </summary>
    private async Task<bool> SaveAndWaitClosedAsync(IntPtr hWndClick, IntPtr hWndOrg, string buttonName, CancelTokenControl ctrl)
    {
        await ctrl.WaitIfPausedOrCancelledAsync();

        // 1. 버튼 클릭
        await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndClick);
        Debug.WriteLine($"[{m_Context.AppName}] {buttonName} 버튼 클릭");
        await Task.Delay(c_nWaitLong, ctrl.Token);

        // 2. 메인창 닫힐 때까지 확인창/보고창 처리
        for (int i = 0; i < c_nRepeatVeryMany; i++)
        {
            await Task.Delay(c_nWaitShort, ctrl.Token);

            // 메인창이 닫혔으면 완료
            if (!Std32Window.IsWindow(hWndOrg))
            {
                Debug.WriteLine($"[{m_Context.AppName}] 등록창 닫힘 확인");
                return true;
            }

            // TMessageForm 찾기 (캡션 검증: Information 또는 Confirm)
            string[] validCaptions = { "Information", "Confirm" };
            List<IntPtr> lstMsg = Std32Window.FindMainWindows_SameProcessId(m_Splash.TopWnd_uProcessId);
            foreach (IntPtr hWnd in lstMsg)
            {
                string className = Std32Window.GetWindowClassName(hWnd);
                if (className != "TMessageForm") continue;
                if (!Std32Window.IsWindowVisible(hWnd)) continue;

                string caption = Std32Window.GetWindowCaption(hWnd) ?? "";
                if (!validCaptions.Contains(caption))
                {
                    Debug.WriteLine($"[{m_Context.AppName}] 잘못된 창 무시 (캡션=\"{caption}\")");
                    continue;
                }

                await Task.Delay(c_nWaitNormal, ctrl.Token);

                // &Yes 버튼 찾기 (확인창)
                IntPtr hWndYes = Std32Window.FindChildWindow(hWnd, "TButton", "&Yes");
                if (hWndYes != IntPtr.Zero)
                {
                    Debug.WriteLine($"[{m_Context.AppName}] 확인창 Yes 클릭 (캡션=\"{caption}\")");
                    await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndYes);
                    await Task.Delay(c_nWaitLong, ctrl.Token);
                    break;
                }

                // OK 버튼 찾기 (보고창)
                IntPtr hWndOk = Std32Window.FindChildWindow(hWnd, "TButton", "OK");
                if (hWndOk != IntPtr.Zero)
                {
                    Debug.WriteLine($"[{m_Context.AppName}] 보고창 OK 클릭 (캡션=\"{caption}\")");
                    await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndOk);
                    await Task.Delay(c_nWaitLong, ctrl.Token);
                    break;
                }
            }
        }

        Debug.WriteLine($"[{m_Context.AppName}] 등록창 닫힘 실패");
        return false;
    }

    /// <summary>
    /// 화물취소 버튼 클릭 후 확인창/보고창 처리, 버튼 Disabled 대기 후 닫기 클릭
    /// - 확인창(TMessageForm + &Yes) 나타나면 Yes 클릭
    /// - 보고창(TMessageForm + OK) 나타나면 OK 클릭
    /// - 화물취소 또는 저장 버튼 Disabled 대기
    /// - 닫기 버튼 클릭
    /// </summary>
    private async Task<bool> CancelAndWaitClosedAsync(IntPtr hWndClick, IntPtr hWndPopup, IntPtr hWndCheckDisabled, IntPtr hWndClose, CancelTokenControl ctrl)
    {
        await ctrl.WaitIfPausedOrCancelledAsync();

        // 1. 화물취소 버튼 클릭
        await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndClick);
        Debug.WriteLine($"[{m_Context.AppName}] 화물취소 버튼 클릭");
        await Task.Delay(c_nWaitLong, ctrl.Token);

        // 2. 확인창/보고창 처리 + 버튼 Disabled 대기
        bool bDisabled = false;
        for (int i = 0; i < c_nRepeatVeryMany; i++)
        {
            await Task.Delay(c_nWaitShort, ctrl.Token);

            // 버튼이 Disabled 되었으면 완료
            if (!Std32Window.IsWindowEnabled(hWndCheckDisabled))
            {
                Debug.WriteLine($"[{m_Context.AppName}] 버튼 Disabled 확인");
                bDisabled = true;
                break;
            }

            // TMessageForm 찾기 (캡션 검증: Information 또는 Confirm)
            string[] validCaptions = { "Information", "Confirm" };
            List<IntPtr> lstMsg = Std32Window.FindMainWindows_SameProcessId(m_Splash.TopWnd_uProcessId);

            foreach (IntPtr hWnd in lstMsg)
            {
                string className = Std32Window.GetWindowClassName(hWnd);
                if (className != "TMessageForm") continue;
                if (!Std32Window.IsWindowVisible(hWnd)) continue;

                string caption = Std32Window.GetWindowCaption(hWnd) ?? "";
                if (!validCaptions.Contains(caption)) continue;

                await Task.Delay(c_nWaitNormal, ctrl.Token);

                // &Yes 버튼 찾기 (확인창)
                IntPtr hWndYes = Std32Window.FindChildWindow(hWnd, "TButton", "&Yes");
                if (hWndYes != IntPtr.Zero)
                {
                    Debug.WriteLine($"[{m_Context.AppName}] 확인창 Yes 클릭 (캡션=\"{caption}\")");
                    await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndYes);
                    await Task.Delay(c_nWaitLong, ctrl.Token);
                    break;
                }

                // OK 버튼 찾기 (보고창)
                IntPtr hWndOk = Std32Window.FindChildWindow(hWnd, "TButton", "OK");
                if (hWndOk != IntPtr.Zero)
                {
                    Debug.WriteLine($"[{m_Context.AppName}] 보고창 OK 클릭 (캡션=\"{caption}\")");
                    await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndOk);
                    await Task.Delay(c_nWaitLong, ctrl.Token);
                    break;
                }
            }
        }

        if (!bDisabled)
        {
            Debug.WriteLine($"[{m_Context.AppName}] 버튼 Disabled 대기 실패");
            return false;
        }

        // 3. 닫기 버튼 클릭
        await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndClose);
        Debug.WriteLine($"[{m_Context.AppName}] 닫기 버튼 클릭");

        // 4. 팝업창 닫힘 대기
        for (int i = 0; i < c_nRepeatMany; i++)
        {
            await Task.Delay(c_nWaitShort, ctrl.Token);
            if (!Std32Window.IsWindow(hWndPopup))
            {
                Debug.WriteLine($"[{m_Context.AppName}] 등록창 닫힘 확인");
                return true;
            }
        }

        Debug.WriteLine($"[{m_Context.AppName}] 등록창 닫힘 실패");
        return false;
    }

    /// <summary>
    /// 첫 로우 클릭 및 선택 검증 (명도 변화로 판단)
    /// </summary>
    private async Task<bool> ClickFirstRowAsync(CancelTokenControl ctrl)
    {
        const int COL_FOR_CLICK = 3; // 처리시간 컬럼 (OFR 컬럼 피함)
        const int COL_FOR_VERIFY = 1; // 상태 컬럼 (선택 시 명도 변화 확인용)
        const int DATA_ROW_INDEX = 0; // 첫 번째 데이터 로우 (헤더 바로 아래)

        try
        {
            Draw.Rectangle rectClickCell = m_RcptPage.DG오더_rcRelCells[COL_FOR_CLICK, DATA_ROW_INDEX];
            Draw.Rectangle rectVerifyCell = m_RcptPage.DG오더_rcRelCells[COL_FOR_VERIFY, DATA_ROW_INDEX];
            Draw.Point ptClick = StdUtil.GetDrawPoint(rectClickCell, 3, 3);

            // 검증용 셀 배경 좌표 (텍스트 피함)
            Draw.Point ptVerify = new Draw.Point(rectVerifyCell.X + 2, rectVerifyCell.Y + 2);

            // 클릭 전 명도 측정
            int brightBefore = OfrService.GetPixelBrightnessFrmWndHandle(m_RcptPage.DG오더_hWnd, ptVerify);
            Debug.WriteLine($"[{m_Context.AppName}] 클릭 전 명도: {brightBefore}");

            for (int i = 1; i <= c_nRepeatShort; i++)
            {
                await ctrl.WaitIfPausedOrCancelledAsync();
                Debug.WriteLine($"[{m_Context.AppName}] 첫 로우 클릭 시도 {i}/{c_nRepeatShort}");

                await Std32Mouse_Post.MousePostAsync_ClickLeft_ptRel(m_RcptPage.DG오더_hWnd, ptClick);

                // 명도 변화 대기 (어두워지면 선택됨)
                for (int j = 0; j < c_nRepeatMany; j++)
                {
                    await Task.Delay(c_nWaitShort, ctrl.Token);
                    int brightAfter = OfrService.GetPixelBrightnessFrmWndHandle(m_RcptPage.DG오더_hWnd, ptVerify);

                    if (brightAfter < brightBefore - 5)
                    {
                        Debug.WriteLine($"[{m_Context.AppName}] 첫 로우 선택 성공 (명도: {brightBefore} → {brightAfter})");
                        return true;
                    }
                }

                await Task.Delay(c_nWaitNormal, ctrl.Token);
            }

            Debug.WriteLine($"[{m_Context.AppName}] 첫 로우 선택 실패");
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{m_Context.AppName}] ClickFirstRowAsync 예외: {ex.Message}");
            return false;
        }
    }
    #endregion

    #region 4. Converters - 데이터 변환
    /// <summary>
    /// TbOrder에서 CEnum_Cg24OrderStatus 플래그 생성
    /// </summary>
    private static CEnum_Cg24OrderStatus Get오더타입FlagsFromKaiTable(TbOrder tbOrder)
    {
        CEnum_Cg24OrderStatus status = CEnum_Cg24OrderStatus.None;

        //if (tbOrder.Share) status |= CEnum_Cg24OrderStatus.공유; // 화물24시는 무조건 접수=공유, 대기=미공유
        if (tbOrder.OrderState == "접수" && tbOrder.Share) status |= CEnum_Cg24OrderStatus.공유;
        if (tbOrder.DtReserve != null) status |= CEnum_Cg24OrderStatus.예약;
        if (tbOrder.DeliverType == "긴급") status |= CEnum_Cg24OrderStatus.긴급;
        if (tbOrder.DeliverType == "왕복") status |= CEnum_Cg24OrderStatus.왕복;
        if (tbOrder.DeliverType == "경유") status |= CEnum_Cg24OrderStatus.경유;

        return status;
    }

    /// <summary>
    /// 차량톤수에 따른 라디오버튼 좌표 반환
    /// </summary>
    private StdResult_Point GetCarWeightWithPoint(string sCarType, string sCarWeight)
    {
        switch (sCarType)
        {
            case "다마": return new StdResult_Point(m_FileInfo.접수등록Wnd_톤수RdoBtns_ptRel0C3);
            case "라보": return new StdResult_Point(m_FileInfo.접수등록Wnd_톤수RdoBtns_ptRel0C5);
            case "트럭":
                switch (sCarWeight)
                {
                    case "1t":
                    case "1t화물": return new StdResult_Point(m_FileInfo.접수등록Wnd_톤수RdoBtns_ptRel1C0);

                    case "1.4t":
                    case "1.4t화물": return new StdResult_Point(m_FileInfo.접수등록Wnd_톤수RdoBtns_ptRel1C4);

                    case "2.5t": return new StdResult_Point(m_FileInfo.접수등록Wnd_톤수RdoBtns_ptRel2C5);
                    case "3.5t": return new StdResult_Point(m_FileInfo.접수등록Wnd_톤수RdoBtns_ptRel3C5);
                    case "5t": return new StdResult_Point(m_FileInfo.접수등록Wnd_톤수RdoBtns_ptRel5);
                    case "8t": return new StdResult_Point(m_FileInfo.접수등록Wnd_톤수RdoBtns_ptRel8);
                    case "11t": return new StdResult_Point(m_FileInfo.접수등록Wnd_톤수RdoBtns_ptRel11);
                    case "14t": return new StdResult_Point(m_FileInfo.접수등록Wnd_톤수RdoBtns_ptRel14);
                    case "15t": return new StdResult_Point(m_FileInfo.접수등록Wnd_톤수RdoBtns_ptRel15);
                    case "18t": return new StdResult_Point(m_FileInfo.접수등록Wnd_톤수RdoBtns_ptRel18);
                    case "25t": return new StdResult_Point(m_FileInfo.접수등록Wnd_톤수RdoBtns_ptRel25);

                    default: return new StdResult_Point($"모르는 톤수[{sCarWeight}]", "GetCarWeightWithPoint_01");
                }
            default: return new StdResult_Point($"모르는 차량종류[{sCarType}]", "GetCarWeightWithPoint_02");
        }
    }

    /// <summary>
    /// 인성 트럭종류 → 화물24시 트럭종류 변환
    /// </summary>
    private StdResult_String GetTruckDetailStringFromInsung(string sTruckDetail)
    {
        switch (sTruckDetail)
        {
            // 다른 텍스트
            case "전체": return new StdResult_String("전체");
            case "카고/윙": return new StdResult_String("카/윙");
            case "플러스카고": return new StdResult_String("플러스카");
            case "리프트카고": return new StdResult_String("리프트");
            case "플축리": return new StdResult_String("플축카리");
            case "축윙": return new StdResult_String("윙축");
            case "리프트호루": return new StdResult_String("리프트호");

            // 없는 차량종류
            case "자바라":
            case "리프트자바라":
            case "냉동플축리":
            case "냉장플축리":
            case "평카":
            case "로브이":
            case "츄레라":
            case "로베드":
            case "사다리":
            case "초장축":
                return new StdResult_String("차종확인");
                //return new StdResult_String($"화물24시에는 없는 트럭종류[{sTruckDetail}]", "GetTruckDetailStringFromInsung_01");

            // 같은 텍스트 (전체, 카고, 축카고, 플축카고, 윙바디, 탑, 호루, 냉동탑, 냉장탑 등)
            default: return new StdResult_String(sTruckDetail);
        }
    }

    /// <summary>
    /// 운송비구분에 따른 라디오버튼 좌표 반환
    /// </summary>
    private StdResult_Point GetFeeTypePoint(string sFeeType)
    {
        switch (sFeeType)
        {
            case "선불":
            case "착불": return new StdResult_Point(m_FileInfo.접수등록Wnd_운송비RdoBtns_ptRel선착불);



            case "카드": //return new StdResult_Point(m_FileInfo.접수등록Wnd_운송비RdoBtns_ptRel카드); // 하차일 지정해야 통과

            case "신용":
            case "송금": return new StdResult_Point(m_FileInfo.접수등록Wnd_운송비RdoBtns_ptRel인수증);

            // case "수수료확인": return new StdResult_Point(m_FileInfo.접수등록Wnd_운송비RdoBtns_ptRel수수료확인);

            default: return new StdResult_Point($"모르는 요금타입[{sFeeType}]", "GetFeeTypePoint_01");
        }
    }

    /// <summary>
    /// 핸들 캡션에서 최대적재량 계산 (1.1배, 소수점 2자리)
    /// </summary>
    private static string GetMaxCargoWeightString(IntPtr hWnd)
    {
        string sCaption = Std32Window.GetWindowCaption(hWnd);
        if (float.TryParse(sCaption, out float fWeight))
        {
            float fMax = fWeight * 1.1f;
            return fMax.ToString("F2");
        }
        return sCaption; // 변환 실패 시 원본 반환
    }
    #endregion

    #region 6. Refresh & Query - 새로고침/조회
    /// <summary>
    /// 조회 버튼 클릭 (재시도 루프 방식)
    /// - 조회버튼 밝기 변화로 로딩 완료 판단
    /// </summary>
    /// <param name="ctrl">취소 토큰</param>
    /// <param name="retryCount">재시도 횟수</param>
    /// <returns>Success: 조회 완료, Fail: 조회 실패</returns>
    public async Task<StdResult_Status> Click조회버튼Async(CancelTokenControl ctrl, int retryCount = 3)
    {
        try
        {
            for (int i = 1; i <= retryCount; i++)
            {
                await ctrl.WaitIfPausedOrCancelledAsync();

                // 조회 버튼 클릭
                await Std32Mouse_Post.MousePostAsync_ClickLeft(m_RcptPage.CmdBtn_hWnd조회);

                // 조회버튼 밝기 변화 대기
                StdResult_Status resultSts = await WaitBrightnessLoadedAsync(ctrl);

                if (resultSts.Result == StdResult.Success || resultSts.Result == StdResult.Skip)
                {
                    return new StdResult_Status(StdResult.Success, "조회 완료");
                }

                // Fail = 타임아웃 → 재시도
                Debug.WriteLine($"[{m_Context.AppName}] 조회 실패 (시도 {i}회): 타임아웃");
                await Task.Delay(c_nWaitNormal, ctrl.Token);
            }

            return new StdResult_Status(StdResult.Fail, $"조회 버튼 클릭 {retryCount}회 모두 실패", "Cargo24sAct_RcptRegPage/Click조회버튼Async_01");
        }
        catch (Exception ex)
        {
            return new StdResult_Status(StdResult.Fail, StdUtil.GetExceptionMessage(ex), "Cargo24sAct_RcptRegPage/Click조회버튼Async_999");
        }
    }

    /// <summary>
    /// 조회버튼 밝기 변화 대기 (로딩 완료 판단)
    /// Phase 1: 밝기 변화 대기 (로딩 시작 감지, 최대 250ms)
    /// Phase 2: 밝기 복원 대기 (로딩 완료 감지, 최대 timeoutSec초)
    /// </summary>
    private async Task<StdResult_Status> WaitBrightnessLoadedAsync(CancelTokenControl ctrl, int timeoutSec = 50)
    {
        try
        {
            int nOrigBrightness = m_RcptPage.CmdBtn_nBrightness조회;
            int nBrightnessTolerance = 10; // 밝기 허용 오차

            // Phase 1: 밝기 변화 대기 (로딩 시작 감지, 최대 250ms)
            bool bBrightnessChanged = false;
            for (int i = 0; i < c_nWaitLong; i++) // 250ms
            {
                await ctrl.WaitIfPausedOrCancelledAsync();

                int nCurrentBrightness = OfrService.GetPixelBrightnessFrmWndHandle(
                    m_RcptPage.CmdBtn_hWnd조회, m_FileInfo.접수등록Page_CmdBtn_ptChkRel조회L);

                if (Math.Abs(nCurrentBrightness - nOrigBrightness) > nBrightnessTolerance)
                {
                    bBrightnessChanged = true;
                    break;
                }
                await Task.Delay(1, ctrl.Token);
            }

            if (!bBrightnessChanged)
            {
                // 밝기 변화 없음 → Skip (이미 로딩 완료)
                return new StdResult_Status(StdResult.Skip);
            }

            // Phase 2: 밝기 복원 대기 (로딩 완료 감지, 최대 timeoutSec초)
            for (int i = 0; i < timeoutSec * 10; i++) // 100ms 간격
            {
                await ctrl.WaitIfPausedOrCancelledAsync();

                int nCurrentBrightness = OfrService.GetPixelBrightnessFrmWndHandle(
                    m_RcptPage.CmdBtn_hWnd조회, m_FileInfo.접수등록Page_CmdBtn_ptChkRel조회L);

                if (Math.Abs(nCurrentBrightness - nOrigBrightness) <= nBrightnessTolerance)
                {
                    return new StdResult_Status(StdResult.Success);
                }
                await Task.Delay(100, ctrl.Token);
            }

            return new StdResult_Status(StdResult.Fail, $"로딩 대기 시간 초과 ({timeoutSec}초)", "Cargo24sAct_RcptRegPage/WaitBrightnessLoadedAsync_01");
        }
        catch (Exception ex)
        {
            return new StdResult_Status(StdResult.Fail, StdUtil.GetExceptionMessage(ex), "Cargo24sAct_RcptRegPage/WaitBrightnessLoadedAsync_999");
        }
    }
    #endregion

    #region 7. Page Navigation - 페이지 관리
    /// <summary>
    /// Page Down 스크롤 (VK_NEXT)
    /// </summary>
    public async Task ScrollPageDownAsync(int count = 1, int delayMs = 100)
    {
        for (int i = 0; i < count; i++)
        {
            Std32Key_Msg.KeyPost_Click(m_RcptPage.DG오더_hWnd, StdCommon32.VK_NEXT);
            if (i < count - 1) await Task.Delay(delayMs);
        }
    }

    /// <summary>
    /// Page Up 스크롤 (VK_PRIOR)
    /// </summary>
    public async Task ScrollPageUpAsync(int count = 1, int delayMs = 100)
    {
        for (int i = 0; i < count; i++)
        {
            Std32Key_Msg.KeyPost_Click(m_RcptPage.DG오더_hWnd, StdCommon32.VK_PRIOR);
            if (i < count - 1) await Task.Delay(delayMs);
        }
    }

    /// <summary>
    /// Row Down 스크롤 (VK_DOWN)
    /// </summary>
    public async Task ScrollRowDownAsync(int count = 1, int delayMs = 100)
    {
        for (int i = 0; i < count; i++)
        {
            Std32Key_Msg.KeyPost_Click(m_RcptPage.DG오더_hWnd, StdCommon32.VK_DOWN);
            if (i < count - 1) await Task.Delay(delayMs);
        }
    }

    /// <summary>
    /// Row Up 스크롤 (VK_UP)
    /// </summary>
    public async Task ScrollRowUpAsync(int count = 1, int delayMs = 100)
    {
        for (int i = 0; i < count; i++)
        {
            Std32Key_Msg.KeyPost_Click(m_RcptPage.DG오더_hWnd, StdCommon32.VK_UP);
            if (i < count - 1) await Task.Delay(delayMs);
        }
    }

    /// <summary>
    /// 특정 로우가 셀렉트되었는지 확인 (순번 컬럼에 숫자가 없으면 셀렉트됨)
    /// </summary>
    /// <param name="rowIndex">로우 인덱스 (0-based)</param>
    /// <returns>true: 셀렉트됨, false: 셀렉트 안됨, null: 판단 불가</returns>
    public async Task<bool?> IsRowSelectedAsync(int rowIndex)
    {
        var rcCell = m_RcptPage.DG오더_rcRelCells[0, rowIndex]; // [col=0, row] 순번 컬럼
        var bmp = OfrService.CaptureScreenRect_InWndHandle(m_RcptPage.DG오더_hWnd, rcCell);
        if (bmp == null) return null;

        var result = await OfrWork_Common.OfrStr_SeqCharAsync(bmp, c_dOfrWeight, bEdit: false);
        bmp.Dispose();

        if (string.IsNullOrEmpty(result.strResult)) return null;

        // 숫자가 있으면 셀렉트 안됨, 없으면 셀렉트됨 (화살표)
        string digits = new string(result.strResult.Where(char.IsDigit).ToArray());
        return string.IsNullOrEmpty(digits);
    }

    /// <summary>
    /// 특정 로우를 클릭하여 셀렉트
    /// </summary>
    /// <param name="rowIndex">로우 인덱스 (0-based)</param>
    public async Task SelectRowAsync(int rowIndex)
    {
        var rcCell = m_RcptPage.DG오더_rcRelCells[0, rowIndex]; // [col=0, row]
        Draw.Point ptClick = new Draw.Point(rcCell.Left + rcCell.Width / 2, rcCell.Top + rcCell.Height / 2);
        await Std32Mouse_Post.MousePostAsync_ClickLeft_ptRel(m_RcptPage.DG오더_hWnd, ptClick);
        await Task.Delay(c_nWaitShort);
    }

    /// <summary>
    /// 첫 로우가 셀렉트되어 있는지 확인하고, 아니면 셀렉트
    /// </summary>
    public async Task EnsureFirstRowSelectedAsync()
    {
        bool? isSelected = await IsRowSelectedAsync(0);
        if (isSelected != true)
        {
            await SelectRowAsync(0);
        }
    }

    /// <summary>
    /// 마지막 로우가 셀렉트되어 있는지 확인하고, 아니면 셀렉트
    /// </summary>
    /// <param name="lastRowIndex">마지막 로우 인덱스 (0-based, 기본값 24)</param>
    public async Task EnsureLastRowSelectedAsync(int lastRowIndex = 24)
    {
        bool? isSelected = await IsRowSelectedAsync(lastRowIndex);
        if (isSelected != true)
        {
            await SelectRowAsync(lastRowIndex);
        }
    }

    /// <summary>
    /// 현재 페이지의 첫 로우 순번을 읽습니다.
    /// 선택된 로우는 화살표가 표시되므로, 다른 로우를 읽어서 계산합니다.
    /// </summary>
    /// <param name="nValidRowCount">현재 페이지의 유효 로우 수 (총계가 rowCount보다 작으면 총계)</param>
    /// <returns>첫 로우 순번 (-1: 실패)</returns>
    public async Task<int> ReadFirstRowNumAsync(int nValidRowCount)
    {
        // 1건만 있는 경우 → 첫 로우 = 1
        if (nValidRowCount == 1)
        {
            Debug.WriteLine($"[Cargo24/ReadFirstRowNum] 1건만 있음 → 첫 로우 = 1");
            return 1;
        }

        // 여러 행이 있는 경우 → 숫자가 있는 셀을 찾아서 계산
        for (int y = 0; y < nValidRowCount; y++)
        {
            var rcCell = m_RcptPage.DG오더_rcRelCells[0, y]; // [col=0, row=y] 순번 컬럼
            var bmp = OfrService.CaptureScreenRect_InWndHandle(m_RcptPage.DG오더_hWnd, rcCell);
            if (bmp == null) continue;

            var result = await OfrWork_Common.OfrStr_SeqCharAsync(bmp, c_dOfrWeight, bEdit: false);
            bmp.Dispose();

            if (string.IsNullOrEmpty(result.strResult)) continue;

            // 숫자만 추출
            string digits = new string(result.strResult.Where(char.IsDigit).ToArray());
            if (string.IsNullOrEmpty(digits)) continue; // 화살표 등 숫자가 아닌 경우 skip

            if (int.TryParse(digits, out int curNum))
            {
                // 첫 로우 순번 계산: curNum - y (y는 0-based index)
                int firstNum = curNum - y;
                Debug.WriteLine($"[Cargo24/ReadFirstRowNum] y={y}, curNum={curNum} → 첫 로우 = {firstNum}");
                return firstNum;
            }
        }

        Debug.WriteLine($"[Cargo24/ReadFirstRowNum] 유효한 순번을 찾지 못함");
        return -1;
    }

    /// <summary>
    /// 페이지별 예상 첫 로우 번호 계산 (0-based 페이지 인덱스)
    /// - 인성 로직 인용
    /// </summary>
    /// <param name="nTotRows">총 행 수</param>
    /// <param name="nRowsPerPage">페이지당 행 수</param>
    /// <param name="pageIdx">페이지 인덱스 (0-based)</param>
    /// <returns>예상 첫 로우 번호</returns>
    public static int GetExpectedFirstRowNum(int nTotRows, int nRowsPerPage, int pageIdx)
    {
        // 총 페이지 수 계산
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

        // 마지막 페이지 특수 처리: 나머지 행이 있는 경우
        if (nTotRows % nRowsPerPage == 0) return nNum;
        else return nNum - nRowsPerPage + (nTotRows % nRowsPerPage);
    }
    #endregion

    #region 8. Row OFR - DG Row 데이터 읽기
    /// <summary>
    /// 지정된 로우의 화물번호 OFR
    /// - 셀 캡처 후 단음소 OFR
    /// - 화물24시는 RGB 반전 불필요
    /// - 마지막 재시도에서만 수동 입력 대화상자 (bEdit=true)
    /// </summary>
    private async Task<StdResult_String> Get화물번호Async(int rowIndex, CancelTokenControl ctrl, int retryCount = c_nRepeatShort)
    {
        try
        {
            const int COL_화물번호 = 2;

            Draw.Rectangle rectSeqnoCell = m_RcptPage.DG오더_rcRelCells[COL_화물번호, rowIndex];
            Debug.WriteLine($"[{m_Context.AppName}] 화물번호 OFR - rowIndex={rowIndex}, 셀위치={rectSeqnoCell}");

            for (int i = 1; i <= retryCount; i++)
            {
                await ctrl.WaitIfPausedOrCancelledAsync();
                Debug.WriteLine($"[{m_Context.AppName}] ===== 화물번호 OFR 시도 {i}/{retryCount} =====");

                Draw.Bitmap bmpCell = OfrService.CaptureScreenRect_InWndHandle(m_RcptPage.DG오더_hWnd, rectSeqnoCell);
                if (bmpCell == null)
                {
                    Debug.WriteLine($"[{m_Context.AppName}] 화물번호 셀 캡처 실패 (시도 {i}/{retryCount})");
                    if (i < retryCount) await Task.Delay(c_nWaitLong, ctrl.Token);
                    continue;
                }

                try
                {
                    // 마지막 시도에서만 bEdit=true (수동 입력 대화상자)
                    StdResult_String resultSeqno = await OfrWork_Common.OfrStr_SeqCharAsync(bmpCell, c_dOfrWeight, i == retryCount);

                    // ☒ 없는 완전한 결과만 성공
                    if (!string.IsNullOrEmpty(resultSeqno.strResult) && !resultSeqno.strResult.Contains('☒'))
                    {
                        Debug.WriteLine($"[{m_Context.AppName}] 화물번호 획득 성공: '{resultSeqno.strResult}' (시도 {i}/{retryCount})");
                        return new StdResult_String(resultSeqno.strResult);
                    }
                    else
                    {
                        Debug.WriteLine($"[{m_Context.AppName}] OFR 실패: '{resultSeqno.strResult ?? resultSeqno.sErr}' (시도 {i}/{retryCount})");
                    }
                }
                finally
                {
                    bmpCell?.Dispose();
                }

                if (i < retryCount) await Task.Delay(c_nWaitLong, ctrl.Token);
            }

            return new StdResult_String($"화물번호 OFR 실패 ({retryCount}회 시도)", "Get화물번호Async_99");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{m_Context.AppName}] Get화물번호Async 예외: {ex.Message}");
            return new StdResult_String(StdUtil.GetExceptionMessage(ex), "Get화물번호Async_999");
        }
    }

    /// <summary>
    /// 캡처된 페이지 이미지에서 특정 로우의 화물번호 읽기
    /// </summary>
    public async Task<StdResult_String> Get화물번호Async(Draw.Bitmap bmpPage, int rowIdx)
    {
        Draw.Rectangle rectSeqno = m_RcptPage.DG오더_rcRelCells[c_nCol화물번호, rowIdx];
        return await OfrWork_Common.OfrStr_SeqCharAsync(bmpPage, rectSeqno, false, c_dOfrWeight);
    }

    /// <summary>
    /// 캡처된 페이지 이미지에서 특정 로우의 상태 읽기
    /// </summary>
    public async Task<StdResult_String> Get상태Async(Draw.Bitmap bmpPage, int rowIdx)
    {
        Draw.Rectangle rectStatus = m_RcptPage.DG오더_rcRelCells[c_nCol상태, rowIdx];
        return await OfrWork_Common.OfrStr_ComplexCharSetAsync(bmpPage, rectStatus, bInvertRgb: false, bTextSave: true, c_dOfrWeight, bEdit: true);
    }
    #endregion

    #region 9. Test Methods - 테스트 함수
    /// <summary>
    /// DG오더 셀 영역 시각화 테스트
    /// TransparantWnd를 사용하여 모든 셀 영역을 두께 1로 그리고 MsgBox 표시
    /// </summary>
    public void Test_DrawAllCellRects()
    {
        try
        {
            Debug.WriteLine($"[Cargo24/Test] Test_DrawAllCellRects 시작");

            // 1. DG오더 핸들 체크
            if (m_RcptPage.DG오더_hWnd == IntPtr.Zero)
            {
                System.Windows.MessageBox.Show("DG오더_hWnd가 초기화되지 않았습니다.", "오류");
                return;
            }

            // 2. Cell Rect 배열 체크
            if (m_RcptPage.DG오더_rcRelCells == null)
            {
                System.Windows.MessageBox.Show("DG오더_rcRelCells가 초기화되지 않았습니다.", "오류");
                return;
            }

            int colCount = m_RcptPage.DG오더_rcRelCells.GetLength(0);
            int rowCount = m_RcptPage.DG오더_rcRelCells.GetLength(1);
            Debug.WriteLine($"[Cargo24/Test] Cell 배열: {rowCount}행 x {colCount}열");

            // 3. TransparantWnd 오버레이 생성 (DG오더 위치 기준)
            TransparantWnd.CreateOverlay(m_RcptPage.DG오더_hWnd);
            TransparantWnd.ClearBoxes();

            // 4-1. 헤더 셀 그리기 (두께 1, 파란색)
            int cellCount = 0;
            for (int col = 0; col < colCount; col++)
            {
                var rcData = m_RcptPage.DG오더_rcRelCells[col, 0]; // 첫 데이터 로우에서 x, width 가져옴
                Draw.Rectangle rcHeader = new Draw.Rectangle(rcData.X, 4, rcData.Width, HEADER_HEIGHT - 8);
                TransparantWnd.DrawBoxAsync(rcHeader, strokeColor: Colors.Blue, thickness: 1);
                cellCount++;
            }

            // 4-2. 데이터 셀 그리기 (두께 1, 빨간색)
            for (int row = 0; row < rowCount; row++)
            {
                for (int col = 0; col < colCount; col++)
                {
                    Draw.Rectangle rc = m_RcptPage.DG오더_rcRelCells[col, row];
                    TransparantWnd.DrawBoxAsync(rc, strokeColor: Colors.Red, thickness: 1);
                    cellCount++;
                }
            }

            Debug.WriteLine($"[Cargo24/Test] {cellCount}개 셀 영역 그리기 완료");

            // 5. MsgBox 표시 (확인 후 오버레이 삭제)
            System.Windows.MessageBox.Show(
                $"화물24시 DG오더 셀 영역 테스트\n\n" +
                $"행: {rowCount}\n" +
                $"열: {colCount}\n" +
                $"총 셀: {cellCount}개\n\n" +
                $"확인을 누르면 오버레이가 제거됩니다.",
                "셀 영역 테스트");

            // 6. 오버레이 삭제
            TransparantWnd.DeleteOverlay();
            Debug.WriteLine($"[Cargo24/Test] Test_DrawAllCellRects 완료");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Cargo24/Test] 예외 발생: {ex.Message}");
            System.Windows.MessageBox.Show($"테스트 중 오류 발생:\n{ex.Message}", "오류");
            TransparantWnd.DeleteOverlay();
        }
    }
    #endregion
}

#nullable restore
