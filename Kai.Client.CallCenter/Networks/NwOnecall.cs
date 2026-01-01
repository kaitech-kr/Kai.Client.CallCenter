using System.Diagnostics;
using Draw = System.Drawing;

using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;
using Kai.Common.NetDll_WpfCtrl.NetOFR;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Services;
using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgWnd;

using Kai.Client.CallCenter.Networks.NwOnecalls;
using Kai.Client.CallCenter.Classes;
using Kai.Client.CallCenter.Classes.Class_Master;
using Kai.Client.CallCenter.OfrWorks;
using Kai.Client.CallCenter.Windows;
using static Kai.Client.CallCenter.Classes.CommonVars;

namespace Kai.Client.CallCenter.Networks;
#nullable disable

// 원콜 앱 (IExternalApp 구현)
public class NwOnecall : IExternalApp
{
    #region Static Configuration (appsettings.json에서 로드)
    public static bool s_Use { get; set; } = false;
    public static string s_Id { get; set; } = string.Empty;
    public static string s_Pw { get; set; } = string.Empty;
    public static string s_AppPath { get; set; } = string.Empty;
    #endregion

    #region Private Fields
    private OnecallContext m_Context;
    private int m_nDatagridFailCount = 0;
    private const int MAX_DATAGRID_FAIL_COUNT = 3;

    // Onecall SeqNo 가져오기
    private string GetOnecallSeqno(AutoAllocModel item) => item.NewOrder.Onecall;
    #endregion

    #region 생성자
    // 원콜 생성자
    public NwOnecall()
    {
        Debug.WriteLine($"[{AppName}] 생성자 호출");
        m_Context = new OnecallContext(StdConst_Network.ONECALL, s_Id, s_Pw);
        m_Context.AppAct = new OnecallAct_App(m_Context);
        m_Context.MainWndAct = new OnecallAct_MainWnd(m_Context);
        m_Context.RcptRegPageAct = new OnecallAct_RcptRegPage(m_Context);
    }
    #endregion

    #region IExternalApp 구현
    public bool IsUsed => s_Use;
    public string AppName => StdConst_Network.ONECALL;

    public async Task<StdResult_Status> InitializeAsync()
    {
        try
        {
            Debug.WriteLine($"[{AppName}] InitializeAsync 시작: Id={s_Id}");

            // 1. 앱 경로 확인 (인성/화물24시 로직 반영)
            if (string.IsNullOrEmpty(s_AppPath))
            {
                string err = "AppPath가 appsettings.json에 설정되지 않았습니다.";
                Debug.WriteLine($"[{AppName}] {err}");
                return new StdResult_Status(StdResult.Fail, err, $"{AppName}/InitializeAsync_AppPath_Null");
            }
            Debug.WriteLine($"[{AppName}] 확인된 앱 경로: {s_AppPath}");

            // 2. UpdaterWorkAsync - 앱 실행 및 Splash 윈도우 찾기
            var result = await m_Context.AppAct.UpdaterWorkAsync(s_AppPath);
            if (result.Result != StdResult.Success)
            {
                Debug.WriteLine($"[{AppName}] UpdaterWorkAsync 실패: {result.sErr}");
                return result;
            }
            Debug.WriteLine($"[{AppName}] UpdaterWorkAsync 성공");

            // 3. SplashWorkAsync - 로그인 처리 및 공지사항 닫기
            result = await m_Context.AppAct.SplashWorkAsync();
            if (result.Result != StdResult.Success)
            {
                Debug.WriteLine($"[{AppName}] SplashWorkAsync 실패: {result.sErr}");
                return result;
            }
            Debug.WriteLine($"[{AppName}] SplashWorkAsync 성공");

            // 4. 메인 윈도우 초기화 (찾기, 이동, 최대화)
            result = await m_Context.MainWndAct.InitAsync();
            if (result.Result != StdResult.Success)
            {
                Debug.WriteLine($"[{AppName}] MainWndAct.InitAsync 실패: {result.sErr}");
                return result;
            }
            Debug.WriteLine($"[{AppName}] MainWndAct.InitAsync 성공");

            // 5. 접수등록 페이지 초기화
            var resultRcpt = await m_Context.RcptRegPageAct.InitializeAsync(s_GlobalCancelToken);
            if (resultRcpt != null)
            {
                if (resultRcpt.sErr == "Skip")
                {
                    return new StdResult_Status(StdResult.Skip, "사용자 요청으로 취소됨", resultRcpt.sPos);
                }
                Debug.WriteLine($"[{AppName}] RcptRegPageAct.InitializeAsync 실패: {resultRcpt.sErr}");
                return new StdResult_Status(StdResult.Fail, resultRcpt.sErr, resultRcpt.sPos);
            }
            Debug.WriteLine($"[{AppName}] RcptRegPageAct.InitializeAsync 성공");

            Debug.WriteLine($"[{AppName}] InitializeAsync 모든 단계 완료");
            return new StdResult_Status(StdResult.Success);
        }
        catch (OperationCanceledException)
        {
            return new StdResult_Status(StdResult.Skip, "사용자 요청으로 취소됨", $"{AppName}/InitializeAsync_Cancel");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{AppName}] InitializeAsync 예외 발생: {ex.Message}");
            return new StdResult_Status(StdResult.Fail, $"예외 발생: {ex.Message}", $"{AppName}/InitializeAsync_999");
        }
    }

    public async Task<StdResult_Status> AutoAllocAsync(long lAllocCount, CancelTokenControl ctrl)
    {
        try
        {
            Debug.WriteLine($"\n-----------------[{AppName}] AutoAllocAsync 시작 - Count={lAllocCount}--------------------------");

            // Cancel/Pause 체크 - Region 2 진입 전
            await ctrl.WaitIfPausedOrCancelledAsync();

            #region 1. 사전작업
            //// TopMost 설정 - 원콜 메인 창을 최상위로
            //await Std32Window.SetWindowTopMostAndReleaseAsync(m_Context.MemInfo.Main.TopWnd_hWnd, CommonVars.c_nWaitShort);
            //Debug.WriteLine($"[{AppName}] TopMost 설정 완료");

            //// DG오더 축소모드로 변경 (Small 데이터그리드 영역 사용)
            //await m_Context.RcptRegPageAct.CollapseDG오더Async();
            #endregion

            #region 2. Local Variables 초기화
            //// 큐에서 주문 가져오기
            //string queueName = StdConst_Network.ONECALL;
            //List<AutoAllocModel> listFromController = ExternalAppController.QueueManager.DequeueAllToList(queueName);
            //Debug.WriteLine($"[{AppName}] 큐에서 가져온 주문 개수: {listFromController.Count}");

            //// 작업잔량 리스트 복사
            //var listOnecall = new List<AutoAllocModel>(listFromController);

            //// 신규(접수 포함) 판별 함수
            ////Func<AutoAllocModel, bool> isNewFromWaiting = item => item.OldOrder?.OrderState == "대기" && item.NewOrder.OrderState == "접수" && string.IsNullOrEmpty(item.NewOrder.Onecall);
            //Func<AutoAllocModel, bool> isNewFromWaiting = item => item.NewOrder.OrderState == "접수" && item.NewOrder.Share == true && string.IsNullOrEmpty(item.NewOrder.Onecall);

            //// listCreated 분류 (신규)
            //var listCreated = listOnecall
            //    .Where(item => item.StateFlag.HasFlag(PostgService_Common_OrderState.Created) ||
            //                   item.StateFlag.HasFlag(PostgService_Common_OrderState.Existed_NonSeqno) ||
            //                   isNewFromWaiting(item))
            //    .OrderByDescending(item => GetOnecallSeqno(item))
            //    .Select(item => item.Clone())
            //    .ToList();

            //// listEtcGroup 분류 (기존)
            //var listEtcGroup = listOnecall
            //    .Where(item => !(item.StateFlag.HasFlag(PostgService_Common_OrderState.Created) ||
            //                   item.StateFlag.HasFlag(PostgService_Common_OrderState.Existed_NonSeqno) ||
            //                   isNewFromWaiting(item)))
            //    .OrderByDescending(item => GetOnecallSeqno(item))
            //    .Select(item => item.Clone())
            //    .ToList();

            //// 상세 로깅
            //Debug.WriteLine($"[{AppName}] listCreated={listCreated.Count}, listEtcGroup={listEtcGroup.Count}");
            //foreach (var item in listCreated)
            //    Debug.WriteLine($"[{AppName}]   Created: KeyCode={item.KeyCode}, StateFlag={item.StateFlag}, Onecall={item.NewOrder?.Onecall}");
            //foreach (var item in listEtcGroup)
            //    Debug.WriteLine($"[{AppName}]   EtcGroup: KeyCode={item.KeyCode}, StateFlag={item.StateFlag}, Onecall={item.NewOrder?.Onecall}");

            //// 할일 체크
            //int tot = listCreated.Count + listEtcGroup.Count;
            //if (tot == 0)
            //{
            //    Debug.WriteLine($"[{AppName}] 자동배차 할일 없음");
            //    return new StdResult_Status(StdResult.Success);
            //}
            #endregion

            #region 3. Check Datagrid
            //bool bDatagridExists = false;
            //for (int i = 0; i < c_nRepeatShort; i++)
            //{
            //    await ctrl.WaitIfPausedOrCancelledAsync();

            //    await Task.Delay(c_nWaitNormal, ctrl.Token);

            //    if (m_Context.MemInfo.RcptPage.DG오더_hWndTop != IntPtr.Zero && Std32Window.IsWindow(m_Context.MemInfo.RcptPage.DG오더_hWndTop))
            //    {
            //        bDatagridExists = true;
            //        break;
            //    }
            //}

            //if (!bDatagridExists)
            //{
            //    m_nDatagridFailCount++;
            //    Debug.WriteLine($"[{AppName}] Datagrid 윈도우를 찾을 수 없음 (연속 실패: {m_nDatagridFailCount}/{MAX_DATAGRID_FAIL_COUNT})");

            //    ExternalAppController.QueueManager.ClearQueue(queueName);
            //    Debug.WriteLine($"[{AppName}] 큐 비움");

            //    if (m_nDatagridFailCount >= MAX_DATAGRID_FAIL_COUNT)
            //    {
            //        Debug.WriteLine($"[{AppName}] {MAX_DATAGRID_FAIL_COUNT}회 연속 실패 → 앱 비활성화");
            //        s_Use = false;
            //        return new StdResult_Status(StdResult.Fail, $"Datagrid {MAX_DATAGRID_FAIL_COUNT}회 연속 실패로 앱 비활성화", $"{AppName}/AutoAllocAsync_03_Disabled");
            //    }

            //    return new StdResult_Status(StdResult.Skip, "Datagrid 윈도우를 찾을 수 없습니다.", $"{AppName}/AutoAllocAsync_03");
            //}

            //m_nDatagridFailCount = 0;
            #endregion

            #region 4. Created Order 처리 (신규)
            //if (listCreated.Count > 0)
            //{
            //    Debug.WriteLine($"[{AppName}] Region 4: 신규 주문 처리 시작 (총 {listCreated.Count}건)");

            //    for (int i = listCreated.Count; i > 0; i--)
            //    {
            //        await ctrl.WaitIfPausedOrCancelledAsync();

            //        int index = i - 1;
            //        if (index < 0) break;

            //        AutoAllocModel item = listCreated[index];
            //        Debug.WriteLine($"[{AppName}]   [{i}/{listCreated.Count}] 신규 주문 처리: KeyCode={item.KeyCode}, 상태={item.NewOrder.OrderState}");

            //        CommonResult_AutoAllocProcess resultAuto = await m_Context.RcptRegPageAct.CheckOcOrderAsync_AssumeKaiNewOrder(item, ctrl);

            //        switch (resultAuto.ResultType)
            //        {
            //            case CEnum_AutoAllocProcessResult.SuccessAndReEnqueue:
            //                Debug.WriteLine($"[{AppName}]   [{i}] 신규 주문 등록 성공: {item.KeyCode}");
            //                ExternalAppController.QueueManager.ReEnqueue(item, queueName, item.StateFlag);
            //                break;

            //            case CEnum_AutoAllocProcessResult.SuccessAndComplete:
            //                Debug.WriteLine($"[{AppName}]   [{i}] 신규 주문 비적재: {item.KeyCode}");
            //                break;

            //            case CEnum_AutoAllocProcessResult.FailureAndDiscard:
            //                Debug.WriteLine($"[{AppName}]   [{i}] 신규 주문 등록 실패: {item.KeyCode} - {resultAuto.sErr}");
            //                // TODO: 치명적 오류 시 Environment.Exit(1) 호출 여부 결정
            //                break;

            //            default:
            //                Debug.WriteLine($"[{AppName}]   [{i}] 예상 못한 결과: {resultAuto.ResultType}");
            //                break;
            //        }
            //    }

            //    Debug.WriteLine($"[{AppName}] Region 4 완료");
            //    listCreated.Clear();
            //}
            #endregion

            #region 5. 기존주문 처리 (listEtcGroup)
            //if (listEtcGroup.Count > 0)
            //{
            //    Debug.WriteLine($"[{AppName}] Region 5: 기존 주문 처리 시작 (총 {listEtcGroup.Count}건)");

            //    #region 5-1. 조회버튼 클릭 + 총계 확인
            //    //int nThisTotCount = -1;
            //    //for (int i = 1; i <= c_nRepeatVeryShort; i++)
            //    //{
            //    //    await ctrl.WaitIfPausedOrCancelledAsync();

            //    //    StdResult_Status resultQuery = await m_Context.RcptRegPageAct.Click새로고침버튼Async(ctrl);
            //    //    if (resultQuery.Result == StdResult.Fail) continue;

            //    //    StdResult_Int resultTotal = await m_Context.RcptRegPageAct.Get총계Async(ctrl);
            //    //    if (resultTotal.nResult >= 0)
            //    //    {
            //    //        nThisTotCount = resultTotal.nResult;
            //    //        m_Context.RcptRegPageAct.m_nLastTotalCount = nThisTotCount; // 다음 조회 딜레이용
            //    //        Debug.WriteLine($"[{AppName}] 총계 읽기 성공 (시도 {i}회): {nThisTotCount}");
            //    //        break;
            //    //    }

            //    //    await Task.Delay(c_nWaitNormal, ctrl.Token);
            //    //}

            //    //if (nThisTotCount < 0)
            //    //{
            //    //    Debug.WriteLine($"[{AppName}] 총계 읽기 실패 ({c_nRepeatVeryShort}회 시도)");
            //    //    return new StdResult_Status(StdResult.Retry, "총계 읽기 실패", $"{AppName}/AutoAllocAsync_51");
            //    //}

            //    //if (nThisTotCount == 0)
            //    //{
            //    //    Debug.WriteLine($"[{AppName}] 데이터 없음 (총계: 0) - Region 5 스킵");
            //    //    return new StdResult_Status(StdResult.Success);
            //    //}

            //    //Debug.WriteLine($"[{AppName}] 데이터 있음 (총계: {nThisTotCount}건)");
            //    #endregion

            //    #region 5-2. 페이지 산정 (Small 모드 고정)
            //    //await ctrl.WaitIfPausedOrCancelledAsync();

            //    //int nRowCount = m_Context.FileInfo.접수등록Page_DG오더Small_RowsCount;
            //    //int nTotPage = 1;

            //    //// 페이지 계산
            //    //if (nThisTotCount > nRowCount)
            //    //{
            //    //    nTotPage = nThisTotCount / nRowCount;
            //    //    if ((nThisTotCount % nRowCount) > 0) nTotPage += 1;
            //    //}

            //    //Debug.WriteLine($"[{AppName}] 페이지 산정: 총계={nThisTotCount}, 페이지당={nRowCount}, 총페이지={nTotPage}");
            //    #endregion

            //    #region 5-3. 페이지별 리스트 검사 및 처리
            //    //// 여러 페이지면 스크롤 핸들 다시 얻어야함 (필요시 내부에서 처리)
            //    //Draw.Bitmap bmpPage = null;

            //    //for (int pageIdx = 0; pageIdx < nTotPage; pageIdx++)
            //    //{
            //    //    #region 1. 사전작업
            //    //    await ctrl.WaitIfPausedOrCancelledAsync();

            //    //    // 현재 페이지가 맞는지 체크
            //    //    int nExpectedFirstNum = OnecallAct_RcptRegPage.GetExpectedFirstRowNum(nThisTotCount, nRowCount, pageIdx);
            //    //    Debug.WriteLine($"[{AppName}] 페이지 {pageIdx + 1}/{nTotPage} → 예상 첫 번호: {nExpectedFirstNum}");

            //    //    // 페이지 검증 및 자동 조정
            //    //    StdResult_Status resultVerify = await m_Context.RcptRegPageAct.VerifyAndAdjustPageAsync(nExpectedFirstNum, ctrl);
            //    //    if (resultVerify.Result == StdResult.Fail)
            //    //        return resultVerify;

            //    //    // 페이지 캡처
            //    //    for (int j = 0; j < c_nRepeatShort; j++)
            //    //    {
            //    //        bmpPage = OfrService.CaptureScreenRect_InWndHandle(m_Context.MemInfo.RcptPage.DG오더_hWndTop);
            //    //        if (bmpPage != null) break;
            //    //        await Task.Delay(c_nWaitShort, ctrl.Token);
            //    //    }
            //    //    if (bmpPage == null)
            //    //    {
            //    //        Debug.WriteLine($"[{AppName}] 페이지 {pageIdx + 1} 캡처 실패 ({c_nRepeatShort}회 시도)");
            //    //        return new StdResult_Status(StdResult.Fail, $"페이지 {pageIdx + 1} DG 캡처 실패", $"{AppName}/AutoAllocAsync_Region5_3_Capture");
            //    //    }

            //    //    // 유효 로우 갯수 얻기
            //    //    StdResult_Int resultInt = m_Context.RcptRegPageAct.GetValidRowCount();
            //    //    if (resultInt.nResult == 0)
            //    //    {
            //    //        Debug.WriteLine($"[{AppName}] 페이지 {pageIdx + 1} 유효 로우 갯수 얻기 실패: {resultInt.sErr}");
            //    //        return new StdResult_Status(StdResult.Fail, resultInt.sErr, resultInt.sPos);
            //    //    }
            //    //    Debug.WriteLine($"[{AppName}] 페이지 {pageIdx + 1}/{nTotPage}: 유효 로우 {resultInt.nResult}개");

            //    //    Draw.Rectangle[,] rects = m_Context.MemInfo.RcptPage.DG오더_rcRelSmallCells;

            //    //    // 마지막 페이지의 경우 시작 인덱스 계산 (페이지가 2개 이상일 때만)
            //    //    int remainder = nThisTotCount % nRowCount;
            //    //    int startIndex = (nTotPage > 1 && pageIdx == nTotPage - 1 && remainder != 0) ? nRowCount - remainder : 0;
            //    //    #endregion

            //    //    #region 2. 로우별 처리
            //    //    // 원콜 DG오더_rcRelSmallCells는 row=0부터 데이터 (인성과 달리 헤더 행 없음)
            //    //    for (int y = startIndex; y < resultInt.nResult; y++)
            //    //    {
            //    //        #region 찾기
            //    //        // 첫 페이지 첫 로우면 클릭 + 포커스 탈출 후 재캡처
            //    //        if (pageIdx == 0 && y == startIndex)
            //    //        {
            //    //            await m_Context.RcptRegPageAct.ClickDatagridRowAsync(y);

            //    //            // 재캡처
            //    //            bmpPage?.Dispose();
            //    //            bmpPage = OfrService.CaptureScreenRect_InWndHandle(m_Context.MemInfo.RcptPage.DG오더_hWndTop);
            //    //            if (bmpPage == null)
            //    //            {
            //    //                Debug.WriteLine($"[{AppName}] 첫 로우 클릭 후 재캡처 실패");
            //    //                return new StdResult_Status(StdResult.Fail, "첫 로우 클릭 후 재캡처 실패", $"{AppName}/AutoAllocAsync_Region5_3_ReCapture");
            //    //            }
            //    //        }

            //    //        // 로우에서 오더번호 읽기
            //    //        Draw.Rectangle rectSeqno = rects[OnecallAct_RcptRegPage.c_nCol오더번호, y];

            //    //        // 오더번호 읽기 (숫자 - 단음소)
            //    //        StdResult_String resultSeqno = await m_Context.RcptRegPageAct.GetRowSeqnoAsync(bmpPage, rectSeqno, false, ctrl);
            //    //        if (string.IsNullOrEmpty(resultSeqno.strResult))
            //    //        {
            //    //            Debug.WriteLine($"[{AppName}] 페이지 {pageIdx + 1}, y={y}, 오더번호 읽기 실패: {resultSeqno.sErr}");
            //    //            return new StdResult_Status(StdResult.Fail, resultSeqno.sErr, resultSeqno.sPos);
            //    //        }

            //    //        string seqno = resultSeqno.strResult;

            //    //        // 오더번호로 listEtcGroup에서 아이템 찾기
            //    //        var foundItem = listEtcGroup.FirstOrDefault(item => GetOnecallSeqno(item) == seqno);
            //    //        if (foundItem == null) continue;

            //    //        // 찾았으면 상태 읽기 (모든 케이스에서 필요)
            //    //        StdResult_String resultStatus = await m_Context.RcptRegPageAct.GetRowStatusAsync(bmpPage, y);
            //    //        if (string.IsNullOrEmpty(resultStatus.strResult))
            //    //        {
            //    //            Debug.WriteLine($"[{AppName}] 페이지 {pageIdx + 1}, y={y}, 상태 읽기 실패: {resultStatus.sErr}");
            //    //            return new StdResult_Status(StdResult.Fail, resultStatus.sErr, resultStatus.sPos);
            //    //        }

            //    //        string status = resultStatus.strResult.Length >= 2 ? resultStatus.strResult.Substring(0, 2) : resultStatus.strResult;
            //    //        Debug.WriteLine($"[{AppName}] 페이지 {pageIdx + 1}, y={y}, seqno={seqno}, status={status}");
            //    //        #endregion

            //    //        #region Race condition 방지: 큐에서 최신 StateFlag 확인
            //    //        var latestItem = ExternalAppController.QueueManager.FindLatestInQueue(queueName, foundItem.KeyCode);
            //    //        if (latestItem != null)
            //    //        {
            //    //            foundItem.StateFlag = latestItem.StateFlag;
            //    //            foundItem.NewOrder = latestItem.NewOrder;
            //    //            Debug.WriteLine($"[{AppName}] 최신 StateFlag 적용: KeyCode={foundItem.KeyCode}, StateFlag={foundItem.StateFlag}");
            //    //        }
            //    //        #endregion

            //    //        #region StateFlag별 분기
            //    //        CommonResult_AutoAllocProcess resultAuto;
            //    //        var dgInfo = new CommonResult_AutoAllocDatagrid(y, status, false, bmpPage);

            //    //        if (foundItem.StateFlag == PostgService_Common_OrderState.NotChanged)
            //    //        {
            //    //            // Kai는 변화 없음 → Onecall 상태 변경 확인
            //    //            resultAuto = await m_Context.RcptRegPageAct.CheckIsOrderAsync_OnecallOrderManage(foundItem, dgInfo, ctrl);
            //    //        }
            //    //        else if ((foundItem.StateFlag & PostgService_Common_OrderState.Existed_WithSeqno) != 0 ||
            //    //            (foundItem.StateFlag & PostgService_Common_OrderState.Updated_Assume) != 0)
            //    //        {
            //    //            // Updated_Assume 플래그 포함 → Onecall을 Kai에 맞춰 업데이트
            //    //            resultAuto = await m_Context.RcptRegPageAct.CheckIsOrderAsync_AssumeKaiUpdated(foundItem, dgInfo, ctrl);
            //    //        }
            //    //        else
            //    //        {
            //    //            Debug.WriteLine($"[{AppName}] 알 수 없는 StateFlag: {foundItem.StateFlag}");
            //    //            return new StdResult_Status(StdResult.Fail, $"알 수 없는 StateFlag: {foundItem.StateFlag}", $"{AppName}/AutoAllocAsync_Region5_3_StateFlag");
            //    //        }
            //    //        #endregion

            //    //        #region 통합 결과 처리
            //    //        switch (resultAuto.ResultType)
            //    //        {
            //    //            case CEnum_AutoAllocProcessResult.SuccessAndReEnqueue:
            //    //                Debug.WriteLine($"[{AppName}] 처리 완료 (재적재): seqno={seqno}");
            //    //                ExternalAppController.QueueManager.ReEnqueue(foundItem, queueName, PostgService_Common_OrderState.NotChanged);
            //    //                break;

            //    //            case CEnum_AutoAllocProcessResult.SuccessAndComplete:
            //    //                Debug.WriteLine($"[{AppName}] 처리 완료 (비적재): seqno={seqno}");
            //    //                break;

            //    //            case CEnum_AutoAllocProcessResult.FailureAndRetry:
            //    //                Debug.WriteLine($"[{AppName}] 처리 실패 (재적재): seqno={seqno}, Error={resultAuto.sErrNPos}");
            //    //                ExternalAppController.QueueManager.ReEnqueue(foundItem, queueName, foundItem.StateFlag);
            //    //                break;

            //    //            case CEnum_AutoAllocProcessResult.FailureAndDiscard:
            //    //                Debug.WriteLine($"[{AppName}] 처리 실패 (버림): seqno={seqno}, Error={resultAuto.sErrNPos}");
            //    //                break;
            //    //        }
            //    //        #endregion
            //    //    }
            //    //    #endregion

            //    //    #region 3. 페이지 종료 후 정리
            //    //    //bmpPage?.Dispose();
            //    //    //bmpPage = null;

            //    //    //// 다음 페이지로 이동 (마지막 페이지가 아닐 때만)
            //    //    //if (pageIdx < nTotPage - 1)
            //    //    //{
            //    //    //    await m_Context.RcptRegPageAct.ScrollPageDownAsync();
            //    //    //    await Task.Delay(c_nWaitNormal, ctrl.Token);
            //    //    //}
            //    //    #endregion
            //    //}
            //    #endregion

            //    Debug.WriteLine($"[{AppName}] Region 5 완료");
            //    listEtcGroup.Clear();
            //}
            #endregion

            return new StdResult_Status(StdResult.Success, string.Empty, $"{AppName}/AutoAllocAsync");
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine($"[{AppName}] AutoAllocAsync 취소됨");
            return new StdResult_Status(StdResult.Skip, "작업 취소됨", $"{AppName}/AutoAllocAsync_Cancel");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{AppName}] AutoAllocAsync 예외 발생: {ex.Message}");
            return new StdResult_Status(StdResult.Fail, $"예외 발생: {ex.Message}", $"{AppName}/AutoAllocAsync_999");
        }
    }

    public void Shutdown()
    {
        try
        {
            Debug.WriteLine($"[{AppName}] Shutdown 시작");

            if (m_Context?.AppAct != null)
            {
                var resultClose = m_Context.AppAct.Close();
                if (resultClose.Result != StdResult.Success)
                {
                    Debug.WriteLine($"[{AppName}] Close 실패: {resultClose.sErr}");
                }
                else
                {
                    Debug.WriteLine($"[{AppName}] Close 성공");
                }
            }

            Debug.WriteLine($"[{AppName}] Shutdown 완료");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{AppName}] Shutdown 예외 발생: {ex.Message}");
        }
    }
    #endregion

    #region 4. Dispose - 리소스 해제
    private bool disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                Debug.WriteLine($"[{AppName}] Dispose 호출");
            }
            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    #endregion
}
#nullable restore
