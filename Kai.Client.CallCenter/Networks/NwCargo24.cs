using System.Diagnostics;
using Draw = System.Drawing;

using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;
using Kai.Common.NetDll_WpfCtrl.NetOFR;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Services;
using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgWnd;

using Kai.Client.CallCenter.Networks.NwCargo24s;
using Kai.Client.CallCenter.Classes;
using Kai.Client.CallCenter.Classes.Class_Master;
using Kai.Client.CallCenter.OfrWorks;
using Kai.Client.CallCenter.Windows;
using static Kai.Client.CallCenter.Classes.CommonVars;

namespace Kai.Client.CallCenter.Networks;
#nullable disable

/// <summary>
/// 화물24시 앱 (IExternalApp 구현)
/// </summary>
public class NwCargo24 : IExternalApp
{
    #region Static Configuration (appsettings.json에서 로드)
    public static bool s_Use { get; set; } = false;
    public static string s_Id { get; set; } = string.Empty;
    public static string s_Pw { get; set; } = string.Empty;
    public static string s_AppPath { get; set; } = string.Empty;
    #endregion

    #region Private Fields
    private Cargo24Context m_Context;
    private long m_lRestCount = 0; // 자동배차 할일 없을 때 카운터 (주기적 조회용)
    private int m_nDatagridFailCount = 0; // Datagrid 연속 실패 카운터
    private const int MAX_DATAGRID_FAIL_COUNT = 3; // 3회 연속 실패 시 비활성화

    /// <summary>
    /// Cargo24 SeqNo 가져오기
    /// </summary>
    private string GetCargo24Seqno(AutoAllocModel item) => item.NewOrder.Cargo24;
    #endregion

    #region IExternalApp 구현
    public bool IsUsed => s_Use;
    public string AppName => StdConst_Network.CARGO24;

    public async Task<StdResult_Status> InitializeAsync()
    {
        try
        {
            Debug.WriteLine($"[{AppName}] InitializeAsync 시작: Id={s_Id}");

            // 1. UpdaterWorkAsync - 앱 실행 및 Splash 윈도우 찾기
            Debug.WriteLine($"[{AppName}] UpdaterWorkAsync 호출...");
            var resultErr = await m_Context.AppAct.UpdaterWorkAsync(s_AppPath, bEdit: true, bWrite: true, bMsgBox: true);
            if (resultErr != null)
            {
                Debug.WriteLine($"[{AppName}] UpdaterWorkAsync 실패: {resultErr.sErrNPos}");
                return new StdResult_Status(StdResult.Fail, resultErr.sErrNPos, $"{AppName}/InitializeAsync_01");
            }
            Debug.WriteLine($"[{AppName}] UpdaterWorkAsync 성공");

            // 2. SplashWorkAsync - 로그인 처리
            Debug.WriteLine($"[{AppName}] SplashWorkAsync 호출...");
            resultErr = await m_Context.AppAct.SplashWorkAsync(bEdit: true, bWrite: true, bMsgBox: true);
            if (resultErr != null)
            {
                Debug.WriteLine($"[{AppName}] SplashWorkAsync 실패: {resultErr.sErrNPos}");
                return new StdResult_Status(StdResult.Fail, resultErr.sErrNPos, $"{AppName}/InitializeAsync_02");
            }
            Debug.WriteLine($"[{AppName}] SplashWorkAsync 성공");

            // 3. MainWndAct 생성
            m_Context.MainWndAct = new Cargo24sAct_MainWnd(m_Context);
            Debug.WriteLine($"[{AppName}] MainWndAct 생성 완료");

            // 4. InitializeAsync - 메인 윈도우 초기화 (찾기 + 이동 + 최대화 + 자식 윈도우)
            Debug.WriteLine($"[{AppName}] InitializeAsync 호출...");
            resultErr = await m_Context.MainWndAct.InitializeAsync(bEdit: true, bWrite: true, bMsgBox: true);
            if (resultErr != null)
            {
                Debug.WriteLine($"[{AppName}] InitializeAsync 실패: {resultErr.sErrNPos}");
                return new StdResult_Status(StdResult.Fail, resultErr.sErrNPos, $"{AppName}/InitializeAsync_03");
            }
            Debug.WriteLine($"[{AppName}] InitializeAsync 성공");

            // 5. RcptRegPageAct 생성 및 초기화
            m_Context.RcptRegPageAct = new Cargo24sAct_RcptRegPage(m_Context);
            //Debug.WriteLine($"[{AppName}] RcptRegPageAct 생성 완료");

            resultErr = await m_Context.RcptRegPageAct.InitializeAsync(bEdit: true, bWrite: true, bMsgBox: true);
            if (resultErr != null)
            {
                Debug.WriteLine($"[{AppName}] RcptRegPage InitializeAsync 실패: {resultErr.sErrNPos}");
                return new StdResult_Status(StdResult.Fail, resultErr.sErrNPos, $"{AppName}/InitializeAsync_04");
            }
            Debug.WriteLine($"[{AppName}] RcptRegPage InitializeAsync 성공");

            Debug.WriteLine($"[{AppName}] InitializeAsync 완료");
            return new StdResult_Status(StdResult.Success);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{AppName}] InitializeAsync 예외 발생: {ex.Message}");
            return new StdResult_Status(StdResult.Fail, $"예외 발생: {ex.Message}", $"{AppName}/InitializeAsync_999");
        }
    }

    public async Task<StdResult_Status> AutoAllocAsync(long lAllocCount, CancelTokenControl ctrl)
    {
        Draw.Bitmap bmpPage = null;

        try
        {
            Debug.WriteLine($"\n-----------------[{AppName}] AutoAllocAsync 시작 - Count={lAllocCount}--------------------------");

            // Cancel/Pause 체크 - Region 2 진입 전
            await ctrl.WaitIfPausedOrCancelledAsync();

            #region 1. 사전작업
            // TopMost 설정 - 화물24시 메인 창을 최상위로
            await Std32Window.SetWindowTopMostAndReleaseAsync(m_Context.MemInfo.Main.TopWnd_hWnd, CommonVars.c_nWaitShort);
            Debug.WriteLine($"[{AppName}] TopMost 설정 완료");
            #endregion

            #region 2. Local Variables 초기화
            // 컨트롤러 큐에서 주문 리스트 가져오기 (DequeueAllToList로 큐 비우기)
            string queueName = StdConst_Network.CARGO24;
            List<AutoAllocModel> listFromController = ExternalAppController.QueueManager.DequeueAllToList(queueName);
            Debug.WriteLine($"[{AppName}] 큐에서 가져온 주문 개수: {listFromController.Count}");

            // 작업잔량 파악 리스트 (원본 복사)
            var listCargo24 = new List<AutoAllocModel>(listFromController);

            // 처리 완료된 항목을 담을 리스트 (Region 4, 5에서 사용)
            // Kai에서 대기 -> 접수를 신규로 평가
            //Func<AutoAllocModel, bool> isNewFromWaiting = item => item.OldOrder?.OrderState == "대기" && item.NewOrder.OrderState == "접수" && string.IsNullOrEmpty(item.NewOrder.Cargo24);
            Func<AutoAllocModel, bool> isNewFromWaiting = item => item.NewOrder.OrderState == "접수" && string.IsNullOrEmpty(item.NewOrder.Cargo24);

            var listCreated = listCargo24
                .Where(item => item.StateFlag.HasFlag(PostgService_Common_OrderState.Created) ||
                               item.StateFlag.HasFlag(PostgService_Common_OrderState.Existed_NonSeqno) ||
                               isNewFromWaiting(item))
                .OrderByDescending(item => GetCargo24Seqno(item)) // Cargo24 SeqNo 역순 정렬 (큰 값 우선)
                .Select(item => item.Clone())
                .ToList();

            var listEtcGroup = listCargo24
                .Where(item => !(item.StateFlag.HasFlag(PostgService_Common_OrderState.Created) ||
                               item.StateFlag.HasFlag(PostgService_Common_OrderState.Existed_NonSeqno) ||
                               isNewFromWaiting(item)))
                .OrderByDescending(item => GetCargo24Seqno(item)) // Cargo24 SeqNo 역순 정렬 (큰 값 우선)
                .Select(item => item.Clone())
                .ToList();

            // ===== 상세 로깅: listCreated 내용 출력 =====
            Debug.WriteLine($"[{AppName}] ===== listCreated (신규접수용) 상세 정보 =====");
            Debug.WriteLine($"[{AppName}] listCreated 개수: {listCreated.Count}");
            for (int i = 0; i < listCreated.Count; i++)
            {
                var item = listCreated[i];
                Debug.WriteLine($"[{AppName}]   [{i}] KeyCode={item.KeyCode}, " +
                              $"StateFlag={item.StateFlag}, " +
                              $"SeqNo={GetCargo24Seqno(item) ?? "(없음)"}, " +
                              $"CarType={item.NewOrder.CarType}, " +
                              $"CallCustFrom={item.NewOrder.CallCustFrom}, " +
                              $"출발={item.NewOrder.StartDongBasic}");
            }
            Debug.WriteLine($"[{AppName}] ==========================================");

            // ===== 상세 로깅: listEtcGroup 내용 출력 =====
            Debug.WriteLine($"[{AppName}] ===== listEtcGroup (기존주문관리용) 상세 정보 =====");
            Debug.WriteLine($"[{AppName}] listEtcGroup 개수: {listEtcGroup.Count}");
            for (int i = 0; i < listEtcGroup.Count; i++)
            {
                var item = listEtcGroup[i];
                Debug.WriteLine($"[{AppName}]   [{i}] KeyCode={item.KeyCode}, " +
                              $"StateFlag={item.StateFlag}, " +
                              $"SeqNo={GetCargo24Seqno(item) ?? "(없음)"}, " +
                              $"CarType={item.NewOrder.CarType}");
            }
            Debug.WriteLine($"[{AppName}] ==========================================");

            // 할일 갯수 체크
            int tot = listCreated.Count + listEtcGroup.Count;

            if (tot == 0)
            {
                m_lRestCount += 1;
                if (m_lRestCount % 60 == 0) // 5 ~ 10분 정도
                {
                    Debug.WriteLine($"[{AppName}] 주기적 조회 버튼 클릭 시작");
                    var resultClick = await m_Context.RcptRegPageAct.Click조회버튼Async(ctrl);
                    Debug.WriteLine($"[{AppName}] 조회 버튼 클릭 결과: {resultClick.Result}, {resultClick.sErr}");
                    await Task.Delay(c_nWaitLong, ctrl.Token);
                }

                Debug.WriteLine($"[{AppName}] 자동배차 할일 없음: lAllocCount={lAllocCount}");
                return new StdResult_Status(StdResult.Success); // 할일 없으면 돌아간다
            }
            else
            {
                m_lRestCount = 0;
                Debug.WriteLine($"[{AppName}] 자동배차 할일 있음: lAllocCount={lAllocCount}, tot={tot}, listCreated={listCreated.Count}, listEtcGroup={listEtcGroup.Count}");
            }
            #endregion

            #region 3. Check Datagrid
            // Datagrid 윈도우 존재 확인 (최대 c_nRepeatShort회 재시도)
            bool bDatagridExists = false;
            for (int i = 0; i < c_nRepeatShort; i++)
            {
                await ctrl.WaitIfPausedOrCancelledAsync();

                // Datagrid 핸들이 유효하고 윈도우가 존재하는지 확인
                if (m_Context.MemInfo.RcptPage.DG오더_hWnd != IntPtr.Zero && Std32Window.IsWindow(m_Context.MemInfo.RcptPage.DG오더_hWnd))
                {
                    bDatagridExists = true;
                    break;
                }

                // 마지막 시도가 아닐 때만 대기 (불필요한 지연 방지)
                if (i < c_nRepeatShort - 1)
                {
                    await Task.Delay(c_nWaitNormal, ctrl.Token);
                }
            }

            if (!bDatagridExists)
            {
                m_nDatagridFailCount++;
                Debug.WriteLine($"[{AppName}] Datagrid 윈도우를 찾을 수 없음 (연속 실패: {m_nDatagridFailCount}/{MAX_DATAGRID_FAIL_COUNT})");

                // 큐 비우기
                ExternalAppController.QueueManager.ClearQueue(queueName);
                Debug.WriteLine($"[{AppName}] 큐 비움");

                // N회 연속 실패 시 앱 비활성화
                if (m_nDatagridFailCount >= MAX_DATAGRID_FAIL_COUNT)
                {
                    Debug.WriteLine($"[{AppName}] {MAX_DATAGRID_FAIL_COUNT}회 연속 실패 → 앱 비활성화");
                    s_Use = false;
                    return new StdResult_Status(StdResult.Fail, $"Datagrid {MAX_DATAGRID_FAIL_COUNT}회 연속 실패로 앱 비활성화", $"{AppName}/AutoAllocAsync_03_Disabled");
                }

                return new StdResult_Status(StdResult.Skip, "Datagrid 윈도우를 찾을 수 없습니다.", $"{AppName}/AutoAllocAsync_03");
            }

            // Datagrid 찾았으면 실패 카운터 리셋
            m_nDatagridFailCount = 0;
            #endregion

            #region 4. Created Order 처리 (신규)
            if (listCreated.Count > 0)
            {
                Debug.WriteLine($"[{AppName}] Region 4: 신규 주문 처리 시작 (총 {listCreated.Count}건)");

                // 역순으로 처리 (삭제를 위해)
                for (int i = listCreated.Count; i > 0; i--)
                {
                    await ctrl.WaitIfPausedOrCancelledAsync();

                    int index = i - 1;
                    if (index < 0) break;

                    AutoAllocModel item = listCreated[index];
                    Debug.WriteLine($"[{AppName}]   [{i}/{listCreated.Count}] 신규 주문 처리: KeyCode={item.KeyCode}, 상태={item.NewOrder.OrderState}");

                    // 신규 주문 등록 시도
                    CommonResult_AutoAllocProcess resultAuto = await m_Context.RcptRegPageAct.CheckCg24OrderAsync_AssumeKaiNewOrder(item, ctrl);

                    switch (resultAuto.ResultType)
                    {
                        case CEnum_AutoAllocProcessResult.SuccessAndReEnqueue:
                            Debug.WriteLine($"[{AppName}]   [{i}] 신규 주문 등록 성공: {item.KeyCode}");
                            ExternalAppController.QueueManager.ReEnqueue(item, queueName, item.StateFlag);
                            break;

                        case CEnum_AutoAllocProcessResult.SuccessAndComplete:
                            Debug.WriteLine($"[{AppName}]   [{i}] 신규 주문 비적재: {item.KeyCode}");
                            break;

                        case CEnum_AutoAllocProcessResult.FailureAndDiscard:
                            Debug.WriteLine($"[{AppName}]   [{i}] 신규 주문 등록 실패 (치명적): {item.KeyCode} - {resultAuto.sErr}");
                            Environment.Exit(1);
                            break;

                        default:
                            Debug.WriteLine($"[{AppName}]   [{i}] 예상 못한 결과: {resultAuto.ResultType}");
                            Environment.Exit(1);
                            break;
                    }
                }

                Debug.WriteLine($"[{AppName}] Region 4 완료");
                listCreated.Clear();
            }
            #endregion

            #region 5. 기존주문 처리 (listEtcGroup)
            if (listEtcGroup.Count > 0)
            {
                #region 5-1. 조회버튼 클릭 + 총계 확인
                int nThisTotCount = -1;
                for (int i = 1; i <= c_nRepeatNormal; i++)
                {
                    await ctrl.WaitIfPausedOrCancelledAsync();

                    StdResult_Status resultQuery = await m_Context.RcptRegPageAct.Click조회버튼Async(ctrl);
                    if (resultQuery.Result == StdResult.Fail) continue;

                    // 헤더 첫 컬럼에서 총계 읽기 (데이터 셀[0,0]의 x,width 사용, y는 헤더 영역)
                    var rcDataCell = m_Context.MemInfo.RcptPage.DG오더_rcRelCells[0, 0];
                    Draw.Rectangle rcTotCell = new Draw.Rectangle(rcDataCell.X, 4, rcDataCell.Width, 20); // 헤더 영역
                    var bmpTot = OfrService.CaptureScreenRect_InWndHandle(m_Context.MemInfo.RcptPage.DG오더_hWnd, rcTotCell);
                    if (bmpTot != null)
                    {
                        var result = await OfrWork_Common.OfrStr_SeqCharAsync(bmpTot, 0.7, bEdit: false);
                        nThisTotCount = int.TryParse(new string(result.strResult?.Where(char.IsDigit).ToArray() ?? Array.Empty<char>()), out int n) ? n : -1;
                        bmpTot.Dispose();

                        if (nThisTotCount >= 0)
                        {
                            Debug.WriteLine($"[{AppName}] 총계 읽기 성공 (시도 {i}회): {nThisTotCount}");
                            break;
                        }
                    }

                    await Task.Delay(c_nWaitNormal, ctrl.Token);
                }

                if (nThisTotCount < 0)
                {
                    Debug.WriteLine($"[{AppName}] 총계 읽기 실패 ({c_nRepeatNormal}회 시도)");
                    return new StdResult_Status(StdResult.Retry, "총계 읽기 실패", $"{AppName}/AutoAllocAsync_51");
                }

                if (nThisTotCount == 0)
                {
                    Debug.WriteLine($"[{AppName}] 데이터 없음 (총계: 0) - Region 5 스킵");
                    return new StdResult_Status(StdResult.Success);
                }

                Debug.WriteLine($"[{AppName}] 데이터 있음 (총계: {nThisTotCount}건)");
                #endregion

                #region 5-2. 페이지 산정
                await ctrl.WaitIfPausedOrCancelledAsync();

                int nTotPage = 1;
                int nRowCount = m_Context.FileInfo.접수등록Page_DG오더_rowCount;

                // 페이지 계산
                if (nThisTotCount > nRowCount)
                {
                    nTotPage = nThisTotCount / nRowCount;
                    if ((nThisTotCount % nRowCount) > 0) nTotPage += 1;
                }

                Debug.WriteLine($"[{AppName}] 페이지 산정: 총계={nThisTotCount}, 페이지당={nRowCount}, 총페이지={nTotPage}");

                #endregion

                #region 5-3. 페이지별 리스트 검사 및 처리
                // 청크(100행) 단위는 다루지 않음 - 현재 로드된 데이터(최대 4페이지)만 처리
                for (int pageIdx = 0; pageIdx < nTotPage; pageIdx++)
                {
                    #region 1. 사전작업
                    await ctrl.WaitIfPausedOrCancelledAsync();

                    // 유효 로우 수 확인
                    StdResult_Int resultValidRow = m_Context.RcptRegPageAct.GetValidRowCount();
                    if (resultValidRow.nResult <= 0)
                    {
                        Debug.WriteLine($"[{AppName}] 페이지[{pageIdx}] 유효 로우 없음: {resultValidRow.sErr}");
                        break;
                    }
                    int nValidRowCount = resultValidRow.nResult;

                    // 페이지 검증 (5-3-1)
                    int expectedFirstNum = Cargo24sAct_RcptRegPage.GetExpectedFirstRowNum(nThisTotCount, nRowCount, pageIdx);
                    int actualFirstNum = await m_Context.RcptRegPageAct.ReadFirstRowNumAsync(nValidRowCount);

                    if (actualFirstNum != expectedFirstNum)
                    {
                        // TODO: 페이지 이동 처리
                       ErrMsgBox($"[{AppName}] 페이지 불일치 → 이동 필요");
                    }

                    // 페이지 전체 캡처
                    bmpPage?.Dispose();
                    bmpPage = OfrService.CaptureScreenRect_InWndHandle(m_Context.MemInfo.RcptPage.DG오더_hWnd);
                    if (bmpPage == null)
                    {
                        ErrMsgBox($"[{AppName}] 페이지[{pageIdx}] 캡처 실패");
                        break;
                    }
                    #endregion

                    #region 2. 로우별 처리
                    for (int rowIdx = 0; rowIdx < nValidRowCount; rowIdx++)
                    {
                        #region 찾기
                        await ctrl.WaitIfPausedOrCancelledAsync();

                        // 화물번호 읽기 (bmpPage 기반)
                        StdResult_String resultSeqno = await m_Context.RcptRegPageAct.Get화물번호Async(bmpPage, rowIdx);
                        if (string.IsNullOrEmpty(resultSeqno.strResult))
                        {
                            Debug.WriteLine($"[{AppName}] 페이지[{pageIdx}] 로우[{rowIdx}] 화물번호 읽기 실패");
                            return new StdResult_Status(StdResult.Fail, $"화물번호 읽기 실패: 페이지[{pageIdx}] 로우[{rowIdx}]", $"{AppName}/AutoAllocAsync_5-3_SeqnoFail");
                        }

                        string seqno = resultSeqno.strResult;

                        // listEtcGroup에서 아이템 찾기
                        var foundItem = listEtcGroup.FirstOrDefault(item => GetCargo24Seqno(item) == seqno);
                        if (foundItem == null) continue;

                        Debug.WriteLine($"[{AppName}] ★ 매칭 성공! 로우[{rowIdx}] seqno={seqno}, KeyCode={foundItem.KeyCode}");

                        // 상태 읽기
                        StdResult_String resultStatus = await m_Context.RcptRegPageAct.Get상태Async(bmpPage, rowIdx);
                        string status = resultStatus.strResult ?? resultStatus.sErr;
                        Debug.WriteLine($"[{AppName}] 상태 OFR: {status}");
                        #endregion

                        #region Race condition 방지: 큐에서 최신 StateFlag 확인
                        // AutoAllocAsync 시작 후 SignalR 업데이트가 발생할 수 있음 - 처리 직전에 큐에서 최신 상태를 확인하여 반영
                        var latestItem = ExternalAppController.QueueManager.FindLatestInQueue(queueName, foundItem.KeyCode);
                        if (latestItem != null)
                        {
                            foundItem.StateFlag = latestItem.StateFlag;
                            foundItem.NewOrder = latestItem.NewOrder;
                            Debug.WriteLine($"[{AppName}] 최신 StateFlag 적용: KeyCode={foundItem.KeyCode}, StateFlag={foundItem.StateFlag}");
                        }
                        #endregion

                        #region StateFlag별 분기
                        CommonResult_AutoAllocProcess resultAuto;

                        if (foundItem.StateFlag == PostgService_Common_OrderState.NotChanged)
                        {
                            // TODO: Cargo24 상태 변경 확인
                            Debug.WriteLine($"[{AppName}] [TODO] NotChanged 처리: KeyCode={foundItem.KeyCode}");
                            resultAuto = CommonResult_AutoAllocProcess.SuccessAndReEnqueue();
                        }
                        else if ((foundItem.StateFlag & PostgService_Common_OrderState.Existed_WithSeqno) != 0 ||
                                 (foundItem.StateFlag & PostgService_Common_OrderState.Updated_Assume) != 0)
                        {
                            // Kai 기준으로 Cargo24 업데이트
                            var dgInfo = new CommonResult_AutoAllocDatagrid(rowIdx, status, false, bmpPage);
                            resultAuto = await m_Context.RcptRegPageAct.CheckIsOrderAsync_AssumeKaiUpdated(foundItem, dgInfo, ctrl);
                        }
                        else
                        {
                            Debug.WriteLine($"[{AppName}] 알 수 없는 StateFlag: {foundItem.StateFlag}");
                            resultAuto = CommonResult_AutoAllocProcess.FailureAndDiscard($"알 수 없는 StateFlag", "5-3-3");
                        }
                        #endregion

                        #region 통합 결과 처리
                        switch (resultAuto.ResultType)
                        {
                            case CEnum_AutoAllocProcessResult.SuccessAndReEnqueue:
                            case CEnum_AutoAllocProcessResult.FailureAndRetry:
                                ExternalAppController.QueueManager.ReEnqueue(foundItem, queueName, PostgService_Common_OrderState.NotChanged);
                                Debug.WriteLine($"[{AppName}] 재적재: KeyCode={foundItem.KeyCode}");
                                break;

                            case CEnum_AutoAllocProcessResult.SuccessAndComplete:
                            case CEnum_AutoAllocProcessResult.FailureAndDiscard:
                                Debug.WriteLine($"[{AppName}] 비적재: KeyCode={foundItem.KeyCode}");
                                break;

                            default:
                                Debug.WriteLine($"[{AppName}] [TODO] 미처리 ResultType: {resultAuto.ResultType}");
                                break;
                        }
                        #endregion

                        #region 정리작업
                        listEtcGroup.RemoveAll(item => item.KeyCode == foundItem.KeyCode);

                        if (listEtcGroup.Count == 0)
                        {
                            Debug.WriteLine($"[{AppName}] listEtcGroup 모두 처리 → 루프 종료");
                            break;
                        }
                        #endregion
                    }
                    #endregion

                    #region 3. 완료작업
                    bmpPage?.Dispose();

                    #endregion
                }
                #endregion

                #region 5-4. 통합 결과 처리
                #endregion

                #region 5-5. 정리작업
                #endregion
            }
            #endregion

            return new StdResult_Status(StdResult.Success);
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

            // AppAct.CloseAsync() 호출 - Cargo24 앱 종료
            if (m_Context?.AppAct != null)
            {
                StdResult_Error resultClose = m_Context.AppAct.Close();
                if (resultClose != null)
                {
                    Debug.WriteLine($"[{AppName}] Close 실패: {resultClose.sErrNPos}");
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

    #region Dispose
    private bool disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                Debug.WriteLine($"[{AppName}] Dispose 호출");
                // 관리형 리소스 해제
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

    #region 생성자
    /// <summary>
    /// 화물24시 생성자
    /// </summary>
    public NwCargo24()
    {
        Debug.WriteLine($"[{AppName}] 생성자 호출");
        m_Context = new Cargo24Context(StdConst_Network.CARGO24, s_Id, s_Pw);
        m_Context.AppAct = new Cargo24sAct_App(m_Context);
    }
    #endregion

    #region Test Methods (개발/디버깅용)
    /// <summary>
    /// UpdaterWorkAsync 테스트
    /// </summary>
    public async Task<StdResult_Error> TestUpdaterWorkAsync(bool bEdit = true, bool bWrite = true, bool bMsgBox = true)
    {
        Debug.WriteLine($"[{AppName}] TestUpdaterWorkAsync 시작");
        return await Cargo24Test.TestUpdaterWorkAsync(s_AppPath, bEdit, bWrite, bMsgBox);
    }

    /// <summary>
    /// 전체 초기화 테스트 (UpdaterWork + SplashWork)
    /// </summary>
    public async Task<StdResult_Error> TestFullInitAsync(bool bEdit = true, bool bWrite = true, bool bMsgBox = true)
    {
        Debug.WriteLine($"[{AppName}] TestFullInitAsync 시작");
        return await Cargo24Test.TestFullInitAsync(s_AppPath, s_Id, s_Pw, bEdit, bWrite, bMsgBox);
    }

    /// <summary>
    /// DG오더 셀 영역 시각화 테스트
    /// InitializeAsync 완료 후 호출하여 셀 영역이 올바르게 설정되었는지 확인
    /// </summary>
    public void Test_DrawDGCellRects()
    {
        Debug.WriteLine($"[{AppName}] Test_DrawDGCellRects 시작");
        m_Context.RcptRegPageAct?.Test_DrawAllCellRects();
    }
    #endregion
}
#nullable restore
