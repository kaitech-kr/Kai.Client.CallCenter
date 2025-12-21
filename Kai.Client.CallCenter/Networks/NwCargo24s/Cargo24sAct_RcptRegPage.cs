using System.Linq;
using System.Windows.Media;
using System.Diagnostics;
using System.Windows.Controls;
using Draw = System.Drawing;
using Microsoft.VisualBasic;

using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;
using Kai.Common.NetDll_WpfCtrl.NetOFR;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models;
using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgWnd;

using Kai.Client.CallCenter.Windows;
using Kai.Client.CallCenter.OfrWorks;
using Kai.Client.CallCenter.Classes;
using Kai.Client.CallCenter.Classes.Class_Master;
using static Kai.Client.CallCenter.Classes.CommonVars;

namespace Kai.Client.CallCenter.Networks.NwCargo24s;
#nullable disable

// // 화물24시 접수등록 페이지 초기화 및 제어 (Context 패턴)
public partial class Cargo24sAct_RcptRegPage
{
    #region 1. Variables - 변수
    // // OFR 가중치
    private const double c_dOfrWeight = 0.7;

    // // Datagrid 컬럼 헤더 정보 배열 (22개)
    public readonly CModel_DgColumnHeader[] m_ReceiptDgHeaderInfos = new CModel_DgColumnHeader[]
    {
        new CModel_DgColumnHeader() { sName = "0", bOfrSeq = true, nWidth = 0 },
        new CModel_DgColumnHeader() { sName = "상태", bOfrSeq = false, nWidth = 40 },
        new CModel_DgColumnHeader() { sName = "화물번호", bOfrSeq = true, nWidth = 80 },
        new CModel_DgColumnHeader() { sName = "처리시간", bOfrSeq = true, nWidth = 70 },
        new CModel_DgColumnHeader() { sName = "고객명", bOfrSeq = false, nWidth = 120 },
        new CModel_DgColumnHeader() { sName = "고객전화", bOfrSeq = true, nWidth = 90 },
        new CModel_DgColumnHeader() { sName = "차주전화", bOfrSeq = true, nWidth = 90 },
        new CModel_DgColumnHeader() { sName = "상차지", bOfrSeq = false, nWidth = 100 },
        new CModel_DgColumnHeader() { sName = "하차지", bOfrSeq = false, nWidth = 100 },
        new CModel_DgColumnHeader() { sName = "운송료", bOfrSeq = true, nWidth = 50 },
        new CModel_DgColumnHeader() { sName = "수수료", bOfrSeq = true, nWidth = 50 },
        new CModel_DgColumnHeader() { sName = "공유", bOfrSeq = false, nWidth = 40 },
        new CModel_DgColumnHeader() { sName = "SMS", bOfrSeq = false, nWidth = 40 },
        new CModel_DgColumnHeader() { sName = "혼적", bOfrSeq = false, nWidth = 40 },
        new CModel_DgColumnHeader() { sName = "요금구분", bOfrSeq = false, nWidth = 60 },
        new CModel_DgColumnHeader() { sName = "계산서금액", bOfrSeq = true, nWidth = 70 },
        new CModel_DgColumnHeader() { sName = "차량톤수", bOfrSeq = true, nWidth = 60 },
        new CModel_DgColumnHeader() { sName = "톤수", bOfrSeq = false, nWidth = 50 },
        new CModel_DgColumnHeader() { sName = "차종", bOfrSeq = false, nWidth = 60 },
        new CModel_DgColumnHeader() { sName = "적재옵션", bOfrSeq = false, nWidth = 100 },
        new CModel_DgColumnHeader() { sName = "차량종류", bOfrSeq = false, nWidth = 60 },
        new CModel_DgColumnHeader() { sName = "화물정보", bOfrSeq = false, nWidth = 150 },
    };

    #region 1-1. Constants - 상수
    // 컬럼 인덱스
    public const int c_nCol순번 = 0;
    public const int c_nCol상태 = 1;
    public const int c_nCol화물번호 = 2;
    public const int c_nColForClick = 3;  // 클릭용 컬럼 (처리시간)

    private const int COLUMN_WIDTH_TOLERANCE = 1;

    // 헤더/캡처 관련
    private const int HEADER_HEIGHT = 30;
    private const int TARGET_ROW = 2;
    private const int HEADER_GAB = 7;
    private const int OFR_HEIGHT = 20;
    private const int BRIGHTNESS_OFFSET = 2;

    // 재시도 및 대기 관련
    private const int MAX_RETRY = 3;
    private const int DELAY_RETRY = 500;
    private const int DELAY_AFTER_INIT = 1000;
    private const int DELAY_AFTER_DRAG = 150;

    // Step 2 특수 컬럼 처리 (백업 기준)
    private const int MIN_COLUMN_WIDTH = 30;
    private const int SPECIAL_COL_START = 24;
    private const int SPECIAL_COL_END = 26;
    private const int SPECIAL_COL_OFFSET = 30;

    // 대화상자 관련 (백업 기준)
    private const string DLG_MSG_CLASS = "TMessageForm";
    private static readonly string[] DLG_CAPTIONS = { "Information", "Confirm", "Cargo24" };
    private const string BTN_OK_CLASS = "TButton";
    private static readonly string[] BTN_CAPTIONS = { "OK", "Yes", "&Yes", "예" };
    #endregion
    #endregion

    #region 2. Context Reference - 컨텍스트 참조
    private readonly Cargo24Context m_Context;
    private Cargo24sInfo_File m_FileInfo => m_Context.FileInfo;
    private Cargo24sInfo_Mem m_MemInfo => m_Context.MemInfo;
    private Cargo24sInfo_Mem.SplashWnd m_Splash => m_MemInfo.Splash;
    private Cargo24sInfo_Mem.MainWnd m_Main => m_MemInfo.Main;
    private Cargo24sInfo_Mem.RcptRegPage m_RcptPage => m_MemInfo.RcptPage;
    #endregion

    #region 3. Constructor - 생성자
    public Cargo24sAct_RcptRegPage(Cargo24Context context)
    {
        m_Context = context ?? throw new ArgumentNullException(nameof(context));
    }
    #endregion

    #region 4. Initialize - 초기화
    // // 접수등록 페이지 초기화 (안내문, 버튼, 그리드 초기화)
    public async Task<StdResult_Status> InitializeAsync()
    {
        IntPtr hWndMain = m_Main.TopWnd_hWnd; 
        IntPtr hWndTmp = IntPtr.Zero;

        try
        {
            Debug.WriteLine($"[{m_Context.AppName}/RcptRegPage] 초기화 시작");

            if (s_GlobalCancelToken.Token.IsCancellationRequested) 
                return new StdResult_Status(StdResult.Fail, "작업 취소됨", "InitializeAsync_Cancel");

            #region 1. 오늘하루동안감추기 처리
            hWndTmp = Std32Window.GetWndHandle_FromRelDrawPt(hWndMain, m_FileInfo.접수등록Page_안내문_오늘하루동안감추기_ptChkRelT);
            if (hWndTmp != IntPtr.Zero)
            {
                string sCap = Std32Window.GetWindowCaption(hWndTmp);
                if (sCap == m_FileInfo.접수등록Page_안내문_오늘하루동안감추기_sWndName)
                {
                    Debug.WriteLine($"[{m_Context.AppName}/RcptRegPage] 안내문 팝업 닫기 클릭");
                    await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndTmp);
                    for (int i = 0; i < c_nRepeatShort; i++)
                    {
                        if (!Std32Window.IsWindow(hWndTmp)) break;
                        await Task.Delay(c_nWaitShort);
                    }
                }
            }
            #endregion

            #region 2. StatusBtn 찾기
            var btnList = new (string name, Draw.Point pt, Action<IntPtr> setter, string code)[]
            {
                (m_FileInfo.접수등록Page_StatusBtn_접수_sWndName, m_FileInfo.접수등록Page_StatusBtn_접수_ptChkRelT, h => m_RcptPage.StatusBtn_hWnd접수 = h, "01"),
                (m_FileInfo.접수등록Page_StatusBtn_운행_sWndName, m_FileInfo.접수등록Page_StatusBtn_운행_ptChkRelT, h => m_RcptPage.StatusBtn_hWnd운행 = h, "02"),
                (m_FileInfo.접수등록Page_StatusBtn_취소_sWndName, m_FileInfo.접수등록Page_StatusBtn_취소_ptChkRelT, h => m_RcptPage.StatusBtn_hWnd취소 = h, "03"),
                (m_FileInfo.접수등록Page_StatusBtn_완료_sWndName, m_FileInfo.접수등록Page_StatusBtn_완료_ptChkRelT, h => m_RcptPage.StatusBtn_hWnd완료 = h, "04"),
                (m_FileInfo.접수등록Page_StatusBtn_정산_sWndName, m_FileInfo.접수등록Page_StatusBtn_정산_ptChkRelT, h => m_RcptPage.StatusBtn_hWnd정산 = h, "05"),
                (m_FileInfo.접수등록Page_StatusBtn_전체_sWndName, m_FileInfo.접수등록Page_StatusBtn_전체_ptChkRelT, h => m_RcptPage.StatusBtn_hWnd전체 = h, "06"),
            };

            foreach (var btn in btnList)
            {
                var (hWnd, status) = await FindStatusButtonAsync(btn.name, btn.pt, "InitializeAsync_" + btn.code);
                if (status.Result != StdResult.Success) return status;
                btn.setter(hWnd);
            }
            #endregion

            #region 3. CmdBtn 찾기 (신규/조회)
            var (hWndNew, resNew) = await FindStatusButtonAsync(
                m_FileInfo.접수등록Page_CmdBtn_신규_sWndName, m_FileInfo.접수등록Page_CmdBtn_신규_ptChkRelT, "InitializeAsync_07");
            if (resNew.Result != StdResult.Success) return resNew;
            m_RcptPage.CmdBtn_hWnd신규 = hWndNew;

            var (hWndQuery, resQuery) = await FindStatusButtonAsync(
                m_FileInfo.접수등록Page_CmdBtn_조회_sWndName, m_FileInfo.접수등록Page_CmdBtn_조회_ptChkRelT, "InitializeAsync_08");
            if (resQuery.Result != StdResult.Success) return resQuery;
            m_RcptPage.CmdBtn_hWnd조회 = hWndQuery;

            m_RcptPage.CmdBtn_조회_nBrightness = OfrService.GetPixelBrightnessFrmWndHandle(hWndQuery, m_FileInfo.접수등록Page_CmdBtn_조회명도_ptChkRelL);
            #endregion

            #region 4. 리스트항목 버튼 찾기 (그리드 설정 복구용)
            var (hWndSave, resSave) = await FindStatusButtonAsync(
                m_FileInfo.접수등록Page_리스트항목_sWndName순서저장, m_FileInfo.접수등록Page_리스트항목_ptChkRel순서저장, "InitializeAsync_09");
            if (resSave.Result == StdResult.Success) m_RcptPage.리스트항목_hWnd순서저장 = hWndSave;

            var (hWndOrig, resOrig) = await FindStatusButtonAsync(
                m_FileInfo.접수등록Page_리스트항목_sWndName원래대로, m_FileInfo.접수등록Page_리스트항목_ptChkRel원래대로, "InitializeAsync_10");
            if (resOrig.Result == StdResult.Success) m_RcptPage.리스트항목_hWnd원래대로 = hWndOrig;
            #endregion

            #region 5. Datagrid 찾기 및 초기화
            var resDg = await SetDG오더RectsAsync();
            if (resDg.Result != StdResult.Success) return resDg;
            #endregion

            Debug.WriteLine($"[{m_Context.AppName}/RcptRegPage] 초기화 완료");
            return new StdResult_Status(StdResult.Success);
        }
        catch (OperationCanceledException)
        {
            return new StdResult_Status(StdResult.Skip, "사용자 요청으로 취소됨", "InitializeAsync_Cancel");
        }
        catch (Exception ex)
        {
            return new StdResult_Status(StdResult.Fail, $"[{m_Context.AppName}/RcptRegPage] 예외발생: {ex.Message}", "InitializeAsync_999");
        }
    }

    // Datagrid 초기화 및 Cell 좌표 계산 (분석 루프 리팩토링)
    public async Task<StdResult_Status> SetDG오더RectsAsync()
    {
        try
        {
            Debug.WriteLine($"[{m_Context.AppName}/SetDG오더] SetDG오더RectsAsync 시작");

            // 1. 그리드 기본 정보 확인
            m_RcptPage.DG오더_hWnd = Std32Window.GetWndHandle_FromRelDrawPt(m_Main.TopWnd_hWnd, m_FileInfo.접수등록Page_DG오더_ptChkRel);
            if (m_RcptPage.DG오더_hWnd == IntPtr.Zero)
                return new StdResult_Status(StdResult.Fail, "그리드 핸들을 찾을 수 없습니다.");

            string sClassName = Std32Window.GetWindowClassName(m_RcptPage.DG오더_hWnd);
            if (sClassName != m_FileInfo.접수등록Page_DG오더_sClassName)
                return new StdResult_Status(StdResult.Fail, $"클래스명 불일치: {sClassName}");

            m_RcptPage.DG오더_AbsRect = Std32Window.GetWindowRect_DrawAbs(m_RcptPage.DG오더_hWnd);

            // 2. 분석 및 검증 루프 (BlockInput 및 Hook 적용)
            CommonFuncs.SetKeyboardHook();
            StdWin32.BlockInput(true);

            List<OfrModel_LeftWidth> listLW = null;
            try
            {
                for (int retry = 1; retry <= MAX_RETRY; retry++)
                {
                    await s_GlobalCancelToken.WaitIfPausedOrCancelledAsync();
                    listLW = await AnalyzeGridHeadersAsync();

                    CEnum_DgValidationIssue issues = CEnum_DgValidationIssue.None;

                    // 평가 1: 컬럼 개수 확인
                    if (listLW == null || listLW.Count < m_ReceiptDgHeaderInfos.Length)
                    {
                        issues |= CEnum_DgValidationIssue.InvalidColumnCount;
                    }
                    else
                    {
                        // 평가 2 & 3: 헤더 OFR 및 너비 검증
                        using var bmpHeader = OfrService.CaptureScreenRect_InWndHandle(m_RcptPage.DG오더_hWnd, new Draw.Rectangle(0, 0, m_RcptPage.DG오더_AbsRect.Width, HEADER_HEIGHT));
                        if (bmpHeader != null)
                        {
                            for (int i = 1; i < m_ReceiptDgHeaderInfos.Length; i++)
                            {
                                if (Math.Abs(listLW[i].nWidth - m_ReceiptDgHeaderInfos[i].nWidth) > COLUMN_WIDTH_TOLERANCE)
                                {
                                    issues |= CEnum_DgValidationIssue.WrongWidth;
                                    Debug.WriteLine($"[{m_Context.AppName}/SetDG오더] 너비 오차: 컬럼[{i}] {listLW[i].nWidth} != {m_ReceiptDgHeaderInfos[i].nWidth}");
                                    break;
                                }

                                Draw.Rectangle rcOfr = new Draw.Rectangle(listLW[i].nLeft, HEADER_GAB, listLW[i].nWidth, OFR_HEIGHT);
                                var resOfr = await OfrWork_Common.OfrStr_ComplexCharSetAsync(bmpHeader, rcOfr, false, true, c_dOfrWeight);
                                string sDetected = resOfr?.strResult ?? "";

                                if (sDetected != m_ReceiptDgHeaderInfos[i].sName)
                                {
                                    issues |= CEnum_DgValidationIssue.WrongOrder;
                                    Debug.WriteLine($"[{m_Context.AppName}/SetDG오더] 이름 불일치: 컬럼[{i}] {sDetected} != {m_ReceiptDgHeaderInfos[i].sName}");
                                    break;
                                }
                            }
                        }
                        else issues |= CEnum_DgValidationIssue.None; // 캡처 실패 시 재분석
                    }

                    if (issues == CEnum_DgValidationIssue.None) break;

                    Debug.WriteLine($"[{m_Context.AppName}/SetDG오더] 검증 실패({issues}), 초기화 시도 {retry}/{MAX_RETRY}");
                    var resInit = await InitDG오더Async(issues);
                    if (resInit.Result != StdResult.Success) return resInit;

                    await Task.Delay(DELAY_RETRY);
                }
            }
            finally
            {
                StdWin32.BlockInput(false);
                CommonFuncs.ReleaseKeyboardHook();
            }

            if (listLW == null || listLW.Count < m_ReceiptDgHeaderInfos.Length)
                return new StdResult_Status(StdResult.Fail, "그리드 분석 실패 (물리 구조 안정화 실패)");

            // 3. 성공 처리
            CalculateCellRects(listLW);
            await SaveDG오더Async(); // 상태 저장 (순서저장 버튼 클릭)

            Debug.WriteLine($"[{m_Context.AppName}/SetDG오더] 설정 성공");
            return new StdResult_Status(StdResult.Success);
        }
        catch (OperationCanceledException)
        {
            return new StdResult_Status(StdResult.Skip, "사용자 요청으로 취소됨", "SetDG오더RectsAsync_Cancel");
        }
        catch (Exception ex)
        {
            return new StdResult_Status(StdResult.Fail, $"[{m_Context.AppName}/SetDG오더] 예외: {ex.Message}");
        }
    }
    #endregion

    #region 5. Init (강제 초기화)
    // Datagrid 강제 초기화 (원래대로 -> 컬럼 제거 -> 순서 조정 -> 너비 조정)
    public async Task<StdResult_Status> InitDG오더Async(CEnum_DgValidationIssue issues)
    {
        try
        {
            Debug.WriteLine($"[{m_Context.AppName}/InitDG] InitDG오더Async 시작 (이슈: {issues})");

            // Step 1. "원래대로" 클릭
            if (m_RcptPage.리스트항목_hWnd원래대로 != IntPtr.Zero)
            {
                Debug.WriteLine($"[{m_Context.AppName}/InitDG] Step 1: '원래대로' 클릭");
                await Std32Mouse_Post.MousePostAsync_ClickLeft(m_RcptPage.리스트항목_hWnd원래대로);
                await WaitAndConfirmDialogAsync();
                await Task.Delay(DELAY_AFTER_INIT);
            }

            // Step 2. 모든 컬럼 폭 줄이기 (한 화면에 다 보이게 밀기)
            Debug.WriteLine($"[{m_Context.AppName}/InitDG] Step 2: 모든 컬럼 폭 줄이기 시작");
            
            var listLW = await AnalyzeGridHeadersAsync();
            if (listLW == null || listLW.Count == 0)
                return new StdResult_Status(StdResult.Fail, "그리드 분석 실패 (Step 2)");

            int columns = listLW.Count;
            // 역순으로 훑으며 폭 축소 (1번부터 끝까지)
            for (int x = columns - 1; x > 0; x--)
            {
                await s_GlobalCancelToken.WaitIfPausedOrCancelledAsync();
                Draw.Point ptStart = new Draw.Point(listLW[x].nLeft, HEADER_GAB);
                Draw.Point ptEnd = new Draw.Point(listLW[x - 1].nLeft + MIN_COLUMN_WIDTH, ptStart.Y);
                
                // 특수 상차일 관련 컬럼 보정
                if (x >= SPECIAL_COL_START && x <= SPECIAL_COL_END) ptEnd.X += SPECIAL_COL_OFFSET;

                int dx = ptEnd.X - ptStart.X;
                await Simulation_Mouse.SafeMouseEvent_DragLeft_Smooth_HorizonAsync(m_RcptPage.DG오더_hWnd, ptStart, dx, false, 100);
                await Task.Delay(DELAY_AFTER_DRAG);
            }
            Debug.WriteLine($"[{m_Context.AppName}/InitDG] Step 2 완료: {columns}개 컬럼 폭 축소됨");

            // Step 3. 컬럼 순서 조정
            Debug.WriteLine($"[{m_Context.AppName}/InitDG] Step 3: 컬럼 순서 조정 시작");
            string[] orgColArr = (string[])m_FileInfo.접수등록Page_DG오더_colOrgTexts.Clone();

            for (int targetIdx = 1; targetIdx < m_ReceiptDgHeaderInfos.Length; targetIdx++)
            {
                await s_GlobalCancelToken.WaitIfPausedOrCancelledAsync();

                // 실시간 헤더 분석
                listLW = await AnalyzeGridHeadersAsync();
                if (listLW == null) return new StdResult_Status(StdResult.Fail, "그리드 분석 실패 (Step 3)");

                string targetName = m_ReceiptDgHeaderInfos[targetIdx].sName;
                int currentPos = Array.IndexOf(orgColArr, targetName);

                if (currentPos < 0) continue;
                if (currentPos == targetIdx) continue;

                Debug.WriteLine($"[{m_Context.AppName}/InitDG] 컬럼 이동: [{targetName}] {currentPos} -> {targetIdx}");

                Draw.Point ptStart = new Draw.Point(listLW[currentPos].nLeft + 10, HEADER_GAB);
                Draw.Point ptEnd = new Draw.Point(listLW[targetIdx].nLeft + 10, ptStart.Y);
                int dx = ptEnd.X - ptStart.X;

                await Simulation_Mouse.SafeMouseEvent_DragLeft_Smooth_HorizonAsync(m_RcptPage.DG오더_hWnd, ptStart, dx, false, 100);

                // 배열 상태 업데이트 (회전식 이동)
                string temp = orgColArr[currentPos];
                for (int m = currentPos; m > targetIdx; m--) orgColArr[m] = orgColArr[m - 1];
                orgColArr[targetIdx] = temp;

                await Task.Delay(DELAY_AFTER_DRAG);
            }
            Debug.WriteLine($"[{m_Context.AppName}/InitDG] Step 3 완료: 컬럼 순서 재배치됨");

            // Step 4. 컬럼 폭 확장 (목표 너비로 조정)
            Debug.WriteLine($"[{m_Context.AppName}/InitDG] Step 4: 컬럼 폭 확장 시작");
            listLW = await AnalyzeGridHeadersAsync();
            if (listLW == null) return new StdResult_Status(StdResult.Fail, "그리드 분석 실패 (Step 4)");

            // 뒤에서 앞으로 확장
            for (int x = m_ReceiptDgHeaderInfos.Length - 1; x > 0; x--)
            {
                await s_GlobalCancelToken.WaitIfPausedOrCancelledAsync();

                if (x + 1 >= listLW.Count) continue;
                
                Draw.Point ptHandle = new Draw.Point(listLW[x + 1].nLeft, HEADER_GAB);
                int targetWidth = m_ReceiptDgHeaderInfos[x].nWidth;
                int currentWidth = listLW[x].nWidth;
                int dx = targetWidth - currentWidth;

                if (Math.Abs(dx) > COLUMN_WIDTH_TOLERANCE)
                {
                    await Simulation_Mouse.SafeMouseEvent_DragLeft_Smooth_HorizonAsync(m_RcptPage.DG오더_hWnd, ptHandle, dx, false, 100);
                    await Task.Delay(DELAY_AFTER_DRAG);
                }
            }
            Debug.WriteLine($"[{m_Context.AppName}/InitDG] Step 4 완료: 컬럼 폭 조정됨");

            // Step 5. 순서 저장 (설정 영구 반영)
            Debug.WriteLine($"[{m_Context.AppName}/InitDG] Step 5: 순서 저장 시작");
            if (m_RcptPage.리스트항목_hWnd순서저장 != IntPtr.Zero)
            {
                await Std32Mouse_Post.MousePostAsync_ClickLeft(m_RcptPage.리스트항목_hWnd순서저장);
                await WaitAndConfirmDialogAsync();
                await Task.Delay(DELAY_AFTER_INIT);
            }

            Debug.WriteLine($"[{m_Context.AppName}/InitDG] InitDG오더Async 전체 완료");
            return new StdResult_Status(StdResult.Success);
        }
        catch (OperationCanceledException)
        {
            return new StdResult_Status(StdResult.Skip, "사용자 요청으로 취소됨", "InitDG오더Async_Cancel");
        }
        catch (Exception ex)
        {
            return new StdResult_Status(StdResult.Fail, $"[{m_Context.AppName}/InitDG] 예외: {ex.Message}");
        }
    }
    #endregion

    #region 6. Helper 함수들
    // 분석된 컬럼 정보를 바탕으로 셀 좌표 일괄 계산
    private void CalculateCellRects(List<OfrModel_LeftWidth> listLW)
    {
        int columns = listLW.Count;
        int rowCount = m_FileInfo.접수등록Page_DG오더_rowCount;
        int rowHeight = m_FileInfo.접수등록Page_DG오더_rowHeight;
        int gab = m_FileInfo.접수등록Page_DG오더_dataGab;
        int textHeight = rowHeight - gab * 2;

        m_RcptPage.DG오더_rcRelCells = new Draw.Rectangle[columns, rowCount];
        m_RcptPage.DG오더_ptRelChkRows = new Draw.Point[rowCount];

        for (int row = 0; row < rowCount; row++)
        {
            int cellY = HEADER_HEIGHT + (row * rowHeight) + 1;
            m_RcptPage.DG오더_ptRelChkRows[row] = new Draw.Point(listLW[0].nLeft + (listLW[0].nWidth / 2), cellY + (rowHeight / 2));

            for (int col = 0; col < columns; col++)
            {
                m_RcptPage.DG오더_rcRelCells[col, row] = new Draw.Rectangle(listLW[col].nLeft, cellY + gab, listLW[col].nWidth, textHeight);
            }
        }
    }

    // 그리드 상태 저장 (합격 시점에만 호출)
    public async Task<StdResult_Status> SaveDG오더Async()
    {
        try
        {
            if (m_RcptPage.리스트항목_hWnd순서저장 != IntPtr.Zero)
            {
                Debug.WriteLine($"[{m_Context.AppName}/InitDG] '순서저장' 클릭");
                await Std32Mouse_Post.MousePostAsync_ClickLeft(m_RcptPage.리스트항목_hWnd순서저장);
                await WaitAndConfirmDialogAsync();
            }
            return new StdResult_Status(StdResult.Success);
        }
        catch (Exception ex)
        {
            return new StdResult_Status(StdResult.Fail, $"[{m_Context.AppName}/SaveDG] 예외 발생: {ex.Message}");
        }
    }

    // 대화상자(TMessageForm) 대기 및 OK 버튼 클릭 헬퍼
    private async Task WaitAndConfirmDialogAsync()
    {
        for (int i = 0; i < CommonVars.c_nRepeatVeryMany; i++)
        {
            await Task.Delay(CommonVars.c_nWaitShort);
            List<IntPtr> lstMsg = Std32Window.FindMainWindows_SameProcessId(m_Splash.TopWnd_uProcessId);
            if (lstMsg == null || lstMsg.Count == 0) continue;

            foreach (IntPtr hWnd in lstMsg)
            {
                if (Std32Window.GetWindowClassName(hWnd) != DLG_MSG_CLASS) continue;
                if (!Std32Window.IsWindowVisible(hWnd)) continue;

                string caption = Std32Window.GetWindowCaption(hWnd) ?? "";
                if (!DLG_CAPTIONS.Contains(caption)) continue;

                IntPtr hWndBtn = IntPtr.Zero;
                foreach (var btnCaption in BTN_CAPTIONS)
                {
                    hWndBtn = Std32Window.FindChildWindow(hWnd, BTN_OK_CLASS, btnCaption);
                    if (hWndBtn != IntPtr.Zero) break;
                }

                if (hWndBtn != IntPtr.Zero)
                {
                    await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndBtn);
                    for (int j = 0; j < CommonVars.c_nRepeatShort; j++)
                    {
                        await Task.Delay(CommonVars.c_nWaitShort);
                        if (!Std32Window.IsWindow(hWnd)) break;
                    }
                    return;
                }
            }
        }
    }

    // 그리드 헤더 영역 픽셀 분석 헬퍼
    private async Task<List<OfrModel_LeftWidth>> AnalyzeGridHeadersAsync()
    {
        using var bmpHeader = OfrService.CaptureScreenRect_InWndHandle(m_RcptPage.DG오더_hWnd, new Draw.Rectangle(0, 0, m_RcptPage.DG오더_AbsRect.Width, HEADER_HEIGHT));
        if (bmpHeader == null) return null;

        byte maxBrightness = OfrService.GetMaxBrightnessAtRow_FromColorBitmapFast(bmpHeader, TARGET_ROW);
        if (maxBrightness == 0) return null;

        maxBrightness -= (byte)BRIGHTNESS_OFFSET;
        bool[] boolArr = OfrService.GetBoolArray_FromColorBitmapRowFast(bmpHeader, TARGET_ROW, maxBrightness, 2);
        var list = OfrService.GetLeftWidthList_FromBool1Array(boolArr, maxBrightness);

        if (list != null && list.Count > 0) list.RemoveAt(list.Count - 1);
        return list;
    }
    #endregion
}
