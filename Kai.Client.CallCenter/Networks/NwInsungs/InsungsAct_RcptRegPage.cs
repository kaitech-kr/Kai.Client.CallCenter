using Kai.Client.CallCenter.Classes;
using Kai.Client.CallCenter.Classes.Class_Master;
using Kai.Client.CallCenter.Networks.NwCargo24s;
using Kai.Client.CallCenter.OfrWorks;
using Kai.Common.NetDll_WpfCtrl.NetOFR;
using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Services;
using System.Diagnostics;
using static Kai.Client.CallCenter.Classes.CommonVars;
using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgWnd;
//using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgBox;
using Draw = System.Drawing;
using DrawImg = System.Drawing.Imaging;

namespace Kai.Client.CallCenter.Networks.NwInsungs;
#nullable disable

public partial class InsungsAct_RcptRegPage
{
    #region 1. Variables - 변수
    // OFR 가중치
    private const double c_dOfrWeight = 0.8;

    // 컬럼 너비 허용 오차 (픽셀)
    private const int COLUMN_WIDTH_TOLERANCE = 1;

    // Datagrid 헤더 상단 여백 (텍스트 없는 영역)
    private const int HEADER_GAB = 7;

    // Datagrid 헤더 텍스트 영역 높이
    private const int HEADER_TEXT_HEIGHT = 18;

    // Datagrid 컬럼 인덱스 상수 (변경 시 수동으로 같이 변경 필요)
    public const int c_nCol번호 = 0;
    public const int c_nCol상태 = 1;
    public const int c_nCol주문번호 = 2;
    public const int c_nCol기사전번 = 18;
    public const int c_nCol오더메모 = 19;
    public const int c_nColForClick = 3;  // 클릭용 컬럼

    // 접수등록 Datagrid 컬럼 헤더 정보 (20개 컬럼)
    public readonly CModel_DgColumnHeader[] m_ReceiptDgHeaderInfos = new CModel_DgColumnHeader[]
    {
        new CModel_DgColumnHeader() { sName = "No", bOfrSeq = false, nWidth = 60 },
        new CModel_DgColumnHeader() { sName = "상태", bOfrSeq = false, nWidth = 70 },
        new CModel_DgColumnHeader() { sName = "주문번호", bOfrSeq = true, nWidth = 80  },
        new CModel_DgColumnHeader() { sName = "최초접수시간", bOfrSeq = true, nWidth = 90 },
        new CModel_DgColumnHeader() { sName = "접수시간", bOfrSeq = true, nWidth = 90  },
        new CModel_DgColumnHeader() { sName = "고객명", bOfrSeq = false, nWidth = 100 },
        new CModel_DgColumnHeader() { sName = "담당자", bOfrSeq = false, nWidth = 100 },
        new CModel_DgColumnHeader() { sName = "전화번호", bOfrSeq = true, nWidth = 100 },
        new CModel_DgColumnHeader() { sName = "출발동", bOfrSeq = false, nWidth = 120 },
        new CModel_DgColumnHeader() { sName = "도착동", bOfrSeq = false, nWidth = 120 },
        new CModel_DgColumnHeader() { sName = "요금", bOfrSeq = true, nWidth = 62 },
        new CModel_DgColumnHeader() { sName = "지급", bOfrSeq = false, nWidth = 40 },
        new CModel_DgColumnHeader() { sName = "형태", bOfrSeq = false, nWidth = 40 },
        new CModel_DgColumnHeader() { sName = "차량", bOfrSeq = false, nWidth = 60 },
        new CModel_DgColumnHeader() { sName = "계산서", bOfrSeq = false, nWidth = 50 },
        new CModel_DgColumnHeader() { sName = "왕복", bOfrSeq = false, nWidth = 40 },
        new CModel_DgColumnHeader() { sName = "공유", bOfrSeq = false, nWidth = 40 },
        new CModel_DgColumnHeader() { sName = "라이더", bOfrSeq = false, nWidth = 120 },
        new CModel_DgColumnHeader() { sName = "기사전번", bOfrSeq = true, nWidth = 100 },
        new CModel_DgColumnHeader() { sName = "오더메모", bOfrSeq = false, nWidth = 100 },
        new CModel_DgColumnHeader() { sName = "적요", bOfrSeq = false, nWidth = 240 },
    };
    #endregion

    #region 2. Context Reference - 컨텍스트 참조
    // Context 참조
    private readonly InsungContext m_Context;

    // 편의를 위한 로컬 참조들
    private InsungsInfo_File m_FileInfo => m_Context.FileInfo;
    private InsungsInfo_Mem m_MemInfo => m_Context.MemInfo;
    private InsungsInfo_Mem.MainWnd m_Main => m_MemInfo.Main;
    private InsungsInfo_Mem.RcptRegPage m_RcptPage => m_MemInfo.RcptPage;
    #endregion

    #region 3. Constructor - 생성자
    // 생성자 - Context 초기화
    public InsungsAct_RcptRegPage(InsungContext context)
    {
        m_Context = context ?? throw new ArgumentNullException(nameof(context));
    }
    #endregion

    #region 4. Initialize - 초기화
    // 접수등록 페이지 초기화 (전체 버튼 클릭 및 상태 검증)
    public async Task<StdResult_Error> InitializeAsync()
    {
        try
        {
            Debug.WriteLine($"[{m_Context.AppName}/RcptRegPage] 초기화 시작");

            // 1. 바메뉴 클릭 - 접수등록 페이지 열기
            await m_Context.MainWndAct.ClickAsync접수등록();
            await Task.Delay(c_nWaitVeryLong); // 페이지 로딩 대기

            // 2. TopWnd 찾기 (MDI 자식 중 "접수현황")
            for (int i = 0; i < c_nRepeatNormal; i++)
            {
                await s_GlobalCancelToken.WaitIfPausedOrCancelledAsync(); // ESC 중단 체크
                m_RcptPage.TopWnd_hWnd = Std32Window.FindWindowEx(
                    m_Main.WndInfo_MdiClient.hWnd, IntPtr.Zero, null, m_FileInfo.접수등록Page_TopWnd_sWndName);

                if (m_RcptPage.TopWnd_hWnd != IntPtr.Zero) break;
                await Task.Delay(c_nWaitNormal);
            }

            if (m_RcptPage.TopWnd_hWnd == IntPtr.Zero)
            {
                // 디버깅: MDI Client의 모든 자식 윈도우 목록 출력
                var childWnds = Std32Window.GetChildWindows_FirstLayer(m_Main.WndInfo_MdiClient.hWnd);
                string childList = childWnds != null ? string.Join(", ", childWnds.Select(x => $"'{Std32Window.GetWindowCaption(x.hWnd)}'")) : "None";
                Debug.WriteLine($"[{m_Context.AppName}/RcptRegPage] 페이지 찾기 실패. 현재 자식 창 목록: {childList}");

                return new StdResult_Error(
                    $"[{m_Context.AppName}/RcptRegPage] 페이지 찾기 실패: {m_FileInfo.접수등록Page_TopWnd_sWndName} (자식창: {childList})",
                    "InsungsAct_RcptRegPage/InitializeAsync_01");
            }

            // 3. 상태 버튼 핸들 수집 및 텍스트 검증 (접수 버튼 기준)
            await Task.Delay(c_nWaitVeryLong);
            bool bBtnFound = false;
            for (int i = 0; i < c_nRepeatNormal; i++)
            {
                await Task.Delay(c_nWaitNormal);

                m_RcptPage.StatusBtn_hWnd접수 = Std32Window.GetWndHandle_FromRelDrawPt(m_Main.TopWnd_hWnd, m_FileInfo.접수등록Page_StatusBtn_접수_ptChkRelTT);
                string sText = Std32Window.GetWindowText(m_RcptPage.StatusBtn_hWnd접수);

                if (sText == "접수")
                {
                    bBtnFound = true;
                    break;
                }
            }

            if (!bBtnFound)
            {
                return new StdResult_Error($"[{m_Context.AppName}/RcptRegPage] 접수 버튼 텍스트 확인 실패", "InsungsAct_RcptRegPage/InitializeAsync_02");
            }

            // 나머지 상태 버튼 핸들 확정
            m_RcptPage.StatusBtn_hWnd배차 = Std32Window.GetWndHandle_FromRelDrawPt(m_Main.TopWnd_hWnd, m_FileInfo.접수등록Page_StatusBtn_ptChkRel배차T);
            m_RcptPage.StatusBtn_hWnd운행 = Std32Window.GetWndHandle_FromRelDrawPt(m_Main.TopWnd_hWnd, m_FileInfo.접수등록Page_StatusBtn_운행_ptChkRelTT);
            m_RcptPage.StatusBtn_hWnd완료 = Std32Window.GetWndHandle_FromRelDrawPt(m_Main.TopWnd_hWnd, m_FileInfo.접수등록Page_StatusBtn_완료_ptChkRelTT);
            m_RcptPage.StatusBtn_hWnd취소 = Std32Window.GetWndHandle_FromRelDrawPt(m_Main.TopWnd_hWnd, m_FileInfo.접수등록Page_StatusBtn_취소_ptChkRelTM);
            m_RcptPage.StatusBtn_hWnd전체 = Std32Window.GetWndHandle_FromRelDrawPt(m_Main.TopWnd_hWnd, m_FileInfo.접수등록Page_StatusBtn_전체_ptChkRelTT);

            // 4. 상태 버튼 Up 이미지 매칭 (접수, 배차, 운행, 완료, 취소, 전체)
            await Task.Delay(c_nWaitNormal);
            StdResult_NulBool resultNulBool;

            await Task.Delay(c_nWaitNormal);
            string[] btnNames = { "접수", "배차", "운행", "완료", "취소", "전체" };
            IntPtr[] hWnds = { m_RcptPage.StatusBtn_hWnd접수, m_RcptPage.StatusBtn_hWnd배차, 
                m_RcptPage.StatusBtn_hWnd운행, m_RcptPage.StatusBtn_hWnd완료, m_RcptPage.StatusBtn_hWnd취소, m_RcptPage.StatusBtn_hWnd전체 };

            for (int i = 0; i < btnNames.Length; i++)
            {
                await s_GlobalCancelToken.WaitIfPausedOrCancelledAsync(); // ESC 중단 체크
                resultNulBool = await OfrWork_Insungs.OfrIsMatchedImage_DrawRelRectAsync(hWnds[i], HEADER_GAB, $"Img_{btnNames[i]}버튼_Up", true);
                if (!StdConvert.NullableBoolToBool(resultNulBool.bResult))
                    return new StdResult_Error(
                        $"[{m_Context.AppName}/RcptRegPage] {btnNames[i]}버튼 Up 이미지 매칭 실패", $"InsungsAct_RcptRegPage/InitializeAsync_UpMatch_{i}");
            }

            // 5. 전체 버튼 클릭 → 접수 버튼 Down 상태 확인 루프
            await Std32Mouse_Post.MousePostAsync_ClickLeft_Center(m_RcptPage.StatusBtn_hWnd전체);
            bool bStateChanged = false;
            for (int i = 1; i <= c_nRepeatVeryShort; i++)
            {
                bool bEdit = i == c_nRepeatVeryShort;
                await Task.Delay(c_nWaitShort);
                resultNulBool = await OfrWork_Insungs.
                    OfrIsMatchedImage_DrawRelRectAsync(m_RcptPage.StatusBtn_hWnd접수, HEADER_GAB, "Img_접수버튼_Down", bEdit);

                if (StdConvert.NullableBoolToBool(resultNulBool.bResult)) { bStateChanged = true; break; }
            }

            if (!bStateChanged)
                return new StdResult_Error($"[{m_Context.AppName}/RcptRegPage] 전체버튼 클릭 후 상태 변경 실패", "InsungsAct_RcptRegPage/InitializeAsync_09");

            // 나머지 버튼 Down 상태 확인 (비교 루프)
            for (int i = 1; i < btnNames.Length; i++) // 0(접수)은 위에서 확인됨
            {
                resultNulBool = await OfrWork_Insungs.OfrIsMatchedImage_DrawRelRectAsync(hWnds[i], HEADER_GAB, $"Img_{btnNames[i]}버튼_Down", true);
                if (!StdConvert.NullableBoolToBool(resultNulBool.bResult))
                    return new StdResult_Error(
                        $"[{m_Context.AppName}/RcptRegPage] {btnNames[i]}버튼 Down 이미지 매칭 실패", $"InsungsAct_RcptRegPage/InitializeAsync_DownMatch_{i}");
            }

            // 6. 커맨드 버튼 찾기 및 OFR 검증 (신규, 조회, 기사)
            var (hWnd신규, error신규) = await FindCommandButtonWithOfrAsync(
                "신규", m_FileInfo.접수등록Page_CmdBtn_신규_ptChkRelT, "Img_신규버튼", "InsungsAct_RcptRegPage/InitializeAsync_08", true, true, true);
            if (error신규 != null) return error신규;
            m_RcptPage.CmdBtn_hWnd신규 = hWnd신규;

            var (hWnd조회, error조회) = await FindCommandButtonWithOfrAsync(
                "조회", m_FileInfo.접수등록Page_CmdBtn_조회_ptChkRelT, "Img_조회버튼", "InsungsAct_RcptRegPage/InitializeAsync_09", true, true, true);
            if (error조회 != null) return error조회;
            m_RcptPage.CmdBtn_hWnd조회 = hWnd조회;

            var (hWnd기사, error기사) = await FindCommandButtonWithOfrAsync(
                "기사", m_FileInfo.접수등록Page_CmdBtn_ptChkRel기사T, "Img_기사버튼", "InsungsAct_RcptRegPage/InitializeAsync_10", true, true, true);
            if (error기사 != null) return error기사;
            m_RcptPage.CmdBtn_hWnd기사 = hWnd기사;

            // 7. CallCount 핸들 찾기
            string[] countNames = { "접수", "운행", "취소", "완료", "총계" };
            Draw.Point[] countPts = { m_FileInfo.접수등록Page_CallCount_ptChkRel접수T, m_FileInfo.접수등록Page_CallCount_ptChkRel운행T, m_FileInfo.접수등록Page_CallCount_ptChkRel취소T, m_FileInfo.접수등록Page_CallCount_ptChkRel완료T, m_FileInfo.접수등록Page_CallCount_ptChkRel총계T };
            IntPtr[] countHWnds = new IntPtr[5];

            for (int i = 0; i < countNames.Length; i++)
            {
                countHWnds[i] = FindCallCountControl(countNames[i], countPts[i], $"InsungsAct_RcptRegPage/InitializeAsync_Count_{i}", out StdResult_Error err);
                if (err != null) return err;
            }
            m_RcptPage.CallCount_hWnd접수 = countHWnds[0];
            m_RcptPage.CallCount_hWnd운행 = countHWnds[1];
            m_RcptPage.CallCount_hWnd취소 = countHWnds[2];
            m_RcptPage.CallCount_hWnd완료 = countHWnds[3];
            m_RcptPage.CallCount_hWnd총계 = countHWnds[4];
            Debug.WriteLine($"[{m_Context.AppName}/RcptRegPage] CallCount 핸들 찾기 성공");

            // 8. 오더 Datagrid 초기화
            m_RcptPage.DG오더_hWnd = Std32Window.GetWndHandle_FromRelDrawPt(m_Main.TopWnd_hWnd, m_FileInfo.접수등록Page_DG오더_ptCenterRelT);
            if (m_RcptPage.DG오더_hWnd == IntPtr.Zero)
                return new StdResult_Error($"[{m_Context.AppName}/RcptRegPage] Datagrid 찾기 실패", "InsungsAct_RcptRegPage/InitializeAsync_16");

            m_RcptPage.DG오더_AbsRect = Std32Window.GetWindowRect_DrawAbs(m_RcptPage.DG오더_hWnd);
            if (m_RcptPage.DG오더_AbsRect.Width != m_FileInfo.접수등록Page_DG오더_rcRelT.Width || m_RcptPage.DG오더_AbsRect.Height != m_FileInfo.접수등록Page_DG오더_rcRelT.Height)
                return new StdResult_Error($"[{m_Context.AppName}/RcptRegPage] Datagrid 크기 불일치", "InsungsAct_RcptRegPage/InitializeAsync_17");

             m_RcptPage.DG오더_hWnd수직스크롤 = Std32Window.GetWndHandle_FromRelDrawPt(m_Main.TopWnd_hWnd, m_FileInfo.접수등록Page_DG오더_ptChkRel수직스크롤T);

            // 9. Datagrid 상세 정보 설정
            StdResult_Error resultDG = await SetDG오더RectsAsync();
            if (resultDG != null) return resultDG;

            Debug.WriteLine($"[{m_Context.AppName}/RcptRegPage] 초기화 완료");
            return null;
        }
        catch (OperationCanceledException)
        {
            throw; // 상위에서 Skip으로 처리하도록 예외 전파
        }
        catch (Exception ex)
        {
            return new StdResult_Error($"[{m_Context.AppName}/RcptRegPage] 예외 발생: {ex.Message}", "InsungsAct_RcptRegPage/InitializeAsync_999");
        }
    }
    // Datagrid 상세 영역 설정 (컬럼 헤더 읽기 + RelChildRects 계산 + 상태 검증)
    private async Task<StdResult_Error> SetDG오더RectsAsync()
    {
        Draw.Bitmap bmpDG = null;
        try
        {
            Debug.WriteLine($"[{m_Context.AppName}/RcptRegPage] SetDG오더RectsAsync 시작");

            // 재시도 루프
            for (int retry = 1; retry <= c_nRepeatShort; retry++)
            {
                await s_GlobalCancelToken.WaitIfPausedOrCancelledAsync(); // ESC 중단 체크

                if (retry > 1)
                {
                    Debug.WriteLine($"[{m_Context.AppName}/RcptRegPage] Datagrid 재시도 {retry}/{c_nRepeatShort}");
                    await Task.Delay(c_nWaitVeryLong);
                }

                // 1. DG오더_hWnd 기준으로 헤더 영역만 캡처
                Draw.Rectangle rcDG_Abs = Std32Window.GetWindowRect_DrawAbs(m_RcptPage.DG오더_hWnd);
                int headerHeight = m_FileInfo.접수등록Page_DG오더_headerHeight;
                Draw.Rectangle rcHeader = new Draw.Rectangle(0, 0, rcDG_Abs.Width, headerHeight);

                bmpDG = OfrService.CaptureScreenRect_InWndHandle(m_RcptPage.DG오더_hWnd, rcHeader);

                if (bmpDG == null)
                {
                    if (retry != c_nRepeatShort)
                    {
                        Debug.WriteLine($"[{m_Context.AppName}/RcptRegPage] DG오더 캡처 실패 (재시도 {retry}/{c_nRepeatShort})");
                        await Task.Delay(200);
                        continue;
                    }
                    return new StdResult_Error($"[{m_Context.AppName}/RcptRegPage] DG오더 캡처 실패", "InsungsAct_RcptRegPage/SetDG오더RectsAsync_01");
                }
                //Debug.WriteLine($"[{m_Context.AppName}/RcptRegPage] DG오더 캡처 성공: {bmpDG.Width}x{bmpDG.Height}");

                // 2. 컬럼 경계 검출 (상단 여백 중간에서 검출)
                int targetRow = HEADER_GAB / 2;  // 상단 여백 중간 (3)
                byte minBrightness = OfrService.GetMinBrightnessAtRow_FromColorBitmapFast(bmpDG, targetRow);

                if (minBrightness == 255)
                {
                    return new StdResult_Error($"[{m_Context.AppName}/RcptRegPage] 최소 밝기 검출 실패", "InsungsAct_RcptRegPage/SetDG오더RectsAsync_02");
                }
                //Debug.WriteLine($"[{m_Context.AppName}/RcptRegPage] 최소 밝기 검출 성공");

                minBrightness += 2;
                bool[] boolArr = OfrService.GetBoolArray_FromColorBitmapRowFast(bmpDG, targetRow, minBrightness, 2);

                if (boolArr == null || boolArr.Length == 0)
                {
                    return new StdResult_Error($"[{m_Context.AppName}/RcptRegPage] Bool 배열 생성 실패", "InsungsAct_RcptRegPage/SetDG오더RectsAsync_03");
                }

                // 2-3. 컬럼 경계 리스트 추출
                List<OfrModel_LeftWidth> listLW = OfrService.GetLeftWidthList_FromBool1Array(boolArr, minBrightness);

                if (listLW == null || listLW.Count < 2)
                {
                    return new StdResult_Error(
                        $"[{m_Context.AppName}/RcptRegPage] 컬럼 경계 검출 실패: Count={listLW?.Count ?? 0}", "InsungsAct_RcptRegPage/SetDG오더RectsAsync_04");
                }

                // 마지막 항목 제거 (오른쪽 끝 경계)
                listLW.RemoveAt(listLW.Count - 1);
                int columns = listLW.Count;

                // 3. 물리적 검증 Step 1: 컬럼 갯수 체크
                if (columns != m_ReceiptDgHeaderInfos.Length)
                {
                    Debug.WriteLine($"[{m_Context.AppName}/RcptRegPage] 검증 실패: 컬럼 갯수 불일치 (검출={columns}, 예상={m_ReceiptDgHeaderInfos.Length}) → 즉시 초기화 진입");
                    
                    bmpDG?.Dispose();
                    bmpDG = null;

                    StdResult_Error initResult = await InitDG오더Async(CEnum_DgValidationIssue.InvalidColumnCount);
                    if (initResult != null && initResult.sErr.Contains("취소")) return initResult;
                    
                    await Task.Delay(200);
                    continue; 
                }

                // 4. 물리적 검증 Step 2: 컬럼 너비 체크
                CEnum_DgValidationIssue widthIssue = CEnum_DgValidationIssue.None;
                for (int x = 0; x < columns; x++)
                {
                    if (Math.Abs(listLW[x].nWidth - m_ReceiptDgHeaderInfos[x].nWidth) > COLUMN_WIDTH_TOLERANCE)
                    {
                        widthIssue = CEnum_DgValidationIssue.WrongWidth;
                        Debug.WriteLine($"[{m_Context.AppName}/RcptRegPage] 검증 실패: 컬럼 너비 불일치 (인덱스={x}, 실제={listLW[x].nWidth}, 예상={m_ReceiptDgHeaderInfos[x].nWidth}) → 즉시 초기화 진입");
                        break;
                    }
                }

                if (widthIssue != CEnum_DgValidationIssue.None)
                {
                    bmpDG?.Dispose();
                    bmpDG = null;

                    StdResult_Error initResult = await InitDG오더Async(widthIssue);
                    if (initResult != null && initResult.sErr.Contains("취소")) return initResult;
                    
                    await Task.Delay(200);
                    continue;
                }

                // 5. 모든 물리적 검증 통과 시에만 OCR(OFR) 수행
                m_RcptPage.DG오더_ColumnTexts = new string[columns];
                for (int i = 0; i < columns; i++)
                {
                    Draw.Rectangle rcColHeader = new Draw.Rectangle(listLW[i].nLeft, HEADER_GAB, listLW[i].nWidth, HEADER_TEXT_HEIGHT);
                    var result = await OfrWork_Common.OfrStr_ComplexCharSetAsync(bmpDG, rcColHeader, bInvertRgb: false, bTextSave: true, dWeight: c_dOfrWeight);
                    m_RcptPage.DG오더_ColumnTexts[i] = result?.strResult ?? string.Empty;
                }

                // 6. 내용 및 순서 최종 검증
                CEnum_DgValidationIssue validationIssues = ValidateDatagridState(m_RcptPage.DG오더_ColumnTexts, listLW);
                if (validationIssues != CEnum_DgValidationIssue.None)
                {
                    Debug.WriteLine($"[{m_Context.AppName}/RcptRegPage] 검증 실패: 데이터그리드 순서/내용 불일치 ({validationIssues}) → 초기화 진입");

                    bmpDG?.Dispose();
                    bmpDG = null;

                    StdResult_Error initResult = await InitDG오더Async(validationIssues);
                    if (initResult != null && initResult.sErr.Contains("취소")) return initResult;

                    await Task.Delay(200);
                    continue;
                }

                // 7. RelChildRects 생성
                bmpDG?.Dispose();
                bmpDG = null;

                int rows = InsungsInfo_File.접수등록Page_DG오더_dataRowCount;
                int dataRowHeight = m_FileInfo.접수등록Page_DG오더_dataRowHeight;
                int emptyRowHeight = m_FileInfo.접수등록Page_DG오더_emptyRowHeight;
                const int dataHeight = 15;

                m_RcptPage.DG오더_RelChildRects = new Draw.Rectangle[columns, rows + 2]; // +2: 헤더, Empty

                for (int col = 0; col < columns; col++)
                {
                    // Row 0: Header
                    m_RcptPage.DG오더_RelChildRects[col, 0] = new Draw.Rectangle(listLW[col].nLeft + 1, HEADER_GAB, listLW[col].nWidth - 2, HEADER_TEXT_HEIGHT);
                    // Row 1: Empty
                    m_RcptPage.DG오더_RelChildRects[col, 1] = new Draw.Rectangle(listLW[col].nLeft + 1, headerHeight + 1, listLW[col].nWidth - 2, dataHeight);

                    // Row 2~: Data rows
                    for (int row = 2; row < rows + 2; row++)
                    {
                        int cellY = headerHeight + emptyRowHeight + ((row - 2) * dataRowHeight) + 1;
                        m_RcptPage.DG오더_RelChildRects[col, row] = new Draw.Rectangle(listLW[col].nLeft + 1, cellY, listLW[col].nWidth - 2, dataHeight);
                    }
                }

                // 8. Background Brightness 계산
                m_RcptPage.DG오더_nBackgroundBright = OfrService.GetCenterPixelBrightnessFrmWndHandle(m_RcptPage.DG오더_hWnd);
                break;
            }

            return null;
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine($"[{m_Context.AppName}/RcptRegPage] SetDG오더RectsAsync - 사용자에 의해 작업이 취소되었습니다.");
            throw; // 상위에서 Skip으로 처리하도록 예외 전파
        }
        catch (Exception ex)
        {
            string err = $"[{m_Context.AppName}/RcptRegPage] SetDG오더RectsAsync 예외발생: {ex.Message}";
            Debug.WriteLine(err);
            Debug.WriteLine(ex.StackTrace);
            return new StdResult_Error(err, "InsungsAct_RcptRegPage/SetDG오더RectsAsync_999");
        }
        finally
        {
            bmpDG?.Dispose();
        }
    }

    // Datagrid 강제 초기화 (Context 메뉴 → "접수화면초기화" 클릭 → 컬럼 조정)
    private (Draw.Bitmap bmpHeader, List<OfrModel_LeftWidth> listLW, int columns) CaptureAndDetectColumnBoundaries(IntPtr hWnd, Draw.Rectangle rcHeader, int targetRow)
    {
        Draw.Bitmap bmpHeader = OfrService.CaptureScreenRect_InWndHandle(hWnd, rcHeader);
        if (bmpHeader == null) return (null, null, 0);

        byte minBrightness = OfrService.GetMinBrightnessAtRow_FromColorBitmapFast(bmpHeader, targetRow);
        minBrightness += 2;

        bool[] boolArr = OfrService.GetBoolArray_FromColorBitmapRowFast(bmpHeader, targetRow, minBrightness, 2);
        
        // [수정] 두 번째 인자는 '최소 픽셀 길이(nMinLen)'이므로 밝기값이 아닌 작은 고정값(2)을 사용해야 함
        List<OfrModel_LeftWidth> listLW = OfrService.GetLeftWidthList_FromBool1Array(boolArr, 2);

        if (listLW == null || listLW.Count < 2)
            return (bmpHeader, listLW, 0);

        // 호출부에서 x + 1 인덱스 접근(우측 경계)을 하므로 리스트 구성은 유지하고 컬럼 수만 반환
        int columns = listLW.Count - 1;
        return (bmpHeader, listLW, columns);
    }

    // 모든 컬럼 OFR 헬퍼
    private async Task<string[]> OfrAllColumnsAsync(
        Draw.Bitmap bmpHeader, List<OfrModel_LeftWidth> listLW, int columns, int gab, int height, bool bTextSave = true)
    {
        string[] texts = new string[columns];

        for (int x = 0; x < columns; x++)
        {
            Draw.Rectangle rcColHeader = new Draw.Rectangle(listLW[x].nLeft, gab, listLW[x].nWidth, height);
            var result = await OfrWork_Common.
                OfrStr_ComplexCharSetAsync(bmpHeader, rcColHeader, bInvertRgb: false, bTextSave: bTextSave, dWeight: c_dOfrWeight);

            texts[x] = result?.strResult;
        }

        return texts;
    }

    //Datagrid 강제 초기화(Context 메뉴 → "접수화면초기화" 클릭 → 컬럼 조정)
    private async Task<StdResult_Error> InitDG오더Async(CEnum_DgValidationIssue issues)
    {
        // 마우스 커서 위치 백업 (작업 완료 후 복원용)
        Draw.Point ptCursorBackup = Std32Cursor.GetCursorPos_AbsDrawPt();

        try
        {
            // Step 1: 사전작업 
            // 입력 제한 및 ESC 취소 활성화
            CommonFuncs.SetKeyboardHook();
            Kai.Common.StdDll_Common.StdWin32.StdWin32.BlockInput(true);
            Debug.WriteLine($"[{m_Context.AppName}] BlockInput(true) & KeyboardHook - 데이터그리드 초기화 시작");

            Debug.WriteLine($"[{m_Context.AppName}] 데이터그리드 수동 초기화 강제 실행 (Step 1)");
            Draw.Rectangle rcDG = Std32Window.GetWindowRect_DrawAbs(m_RcptPage.DG오더_hWnd);
            Draw.Rectangle rcHeader = new Draw.Rectangle(0, 0, rcDG.Width, m_FileInfo.접수등록Page_DG오더_headerHeight);

            // Step 2: "접수화면초기화" 클릭
            Debug.WriteLine("[InitDG오더] Step 1: 접수화면초기화 시작");
            await Std32Mouse_Post.MousePostAsync_ClickRight(m_RcptPage.DG오더_hWnd);

            IntPtr hWndMenu = IntPtr.Zero;
            for (int i = 0; i < 100; i++)
            {
                await CommonFuncs.CheckCancelAndThrowAsync();
                await Task.Delay(20);
                hWndMenu = Std32Window.FindMainWindow_StartsWith(m_Context.MemInfo.Splash.TopWnd_uProcessId, m_FileInfo.Main_AnyMenu_sClassName, m_FileInfo.Main_AnyMenu_sWndName);
                if (hWndMenu != IntPtr.Zero) break;
            }

            if (hWndMenu == IntPtr.Zero) return new StdResult_Error("[InitDG오더]Context 메뉴 찾기 실패", "InitDG오더Async_01");
            await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndMenu, 10, 12, 50);

            IntPtr hWndDialog = IntPtr.Zero;
            for (int i = 0; i < CommonVars.c_nRepeatVeryMany; i++)
            {
                await CommonFuncs.CheckCancelAndThrowAsync();
                await Task.Delay(CommonVars.c_nWaitShort);
                hWndDialog = Std32Window.FindMainWindow(m_Context.MemInfo.Splash.TopWnd_uProcessId, "#32770", "확인");
                if (hWndDialog != IntPtr.Zero) break;
            }

            if (hWndDialog == IntPtr.Zero)
            {
                await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndMenu, 10, 12, 50);
                for (int i = 0; i < CommonVars.c_nRepeatVeryMany; i++)
                {
                    await CommonFuncs.CheckCancelAndThrowAsync();
                    await Task.Delay(CommonVars.c_nWaitShort);
                    hWndDialog = Std32Window.FindMainWindow(m_Context.MemInfo.Splash.TopWnd_uProcessId, "#32770", "확인");
                    if (hWndDialog != IntPtr.Zero) break;
                }
            }

            if (hWndDialog == IntPtr.Zero) return new StdResult_Error("[InitDG오더]확인 다이얼로그 찾기 실패", "InitDG오더Async_03");

            IntPtr hWndBtn = Std32Window.FindWindowEx(hWndDialog, IntPtr.Zero, "Button", "예(&Y)");
            if (hWndBtn == IntPtr.Zero) hWndBtn = Std32Window.FindWindowEx(hWndDialog, IntPtr.Zero, "Button", "예"); // mnemonic 없는 경우 대비
            if (hWndBtn == IntPtr.Zero) return new StdResult_Error("[InitDG오더]'예' 버튼 찾기 실패", "InitDG오더Async_02");

            // Step 2.2: "예" 버튼 클릭 및 창 닫힘 대기 (백업 로직 기반 강화)
            for (int i = 0; i < 3; i++)
            {
                await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndBtn, 5, 5, 100);
                await Std32Key_Msg.KeyPost_DownAsync(hWndDialog, StdCommon32.VK_RETURN); // 엔터키 병행
                await Task.Delay(CommonVars.c_nWaitNormal);
                if (!Std32Window.IsWindow(hWndDialog)) break;
            }

            // Step 2.3: 최종 "초기화되었습니다" 확인창 처리 (백업본 팝업 로직 참조)
            await Task.Delay(CommonVars.c_nWaitNormal);
            IntPtr hWndFinal = Std32Window.FindMainWindow(m_Context.MemInfo.Splash.TopWnd_uProcessId, "#32770", "확인");
            if (hWndFinal != IntPtr.Zero)
            {
                IntPtr hWndFinalBtn = Std32Window.FindWindowEx(hWndFinal, IntPtr.Zero, "Button", "확인");
                if (hWndFinalBtn != IntPtr.Zero)
                {
                    await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndFinalBtn, 10, 10, 50);
                    await Std32Key_Msg.KeyPost_DownAsync(hWndFinal, StdCommon32.VK_RETURN);
                    await Task.Delay(CommonVars.c_nWaitNormal);
                }
            }

            await Task.Delay(c_nWaitVeryLong);

            // Step 3: 컬럼 삭제 및 21개 확보 (폭 조정을 통한 발견)
            Debug.WriteLine("[InitDG오더] Step 2: 컬럼 확보 시작");
            int headerHeight = m_FileInfo.접수등록Page_DG오더_headerHeight;
            const int headerGab = 7;
            int textHeight = headerHeight - (headerGab * 2);
            int targetRow = headerGab / 2;
            const int center = 15;

            // Step 2: 컬럼 확보 시작
            for (int widthIter = 0; widthIter < 5; widthIter++)
            {
                await CommonFuncs.CheckCancelAndThrowAsync();
                await Task.Delay(CommonVars.c_nWaitNormal);
                var (bmpHeader, listLW, columns) = CaptureAndDetectColumnBoundaries(m_RcptPage.DG오더_hWnd, rcHeader, targetRow);
                if (bmpHeader == null) return new StdResult_Error("헤더 캡쳐 실패", "InitDG오더Async_Step2_01");

                string[] texts = await OfrAllColumnsAsync(bmpHeader, listLW, columns, headerGab, textHeight, true);
                
                // 불필요 제거
                for (int x = columns - 1; x >= 0; x--)
                {
                    if (!m_ReceiptDgHeaderInfos.Any(h => h.sName == texts[x]))
                    {
                        Draw.Rectangle rcCol = new Draw.Rectangle(listLW[x].nLeft, headerGab, listLW[x].nWidth, textHeight);
                        Draw.Point ptCenter = StdUtil.GetCenterDrawPoint(rcCol);
                        
                        await InsungsAct_RcptRegPage.DragAsync_Vertical_Smooth(m_RcptPage.DG오더_hWnd, ptCenter, -50, 50); 
                        await Task.Delay(c_nWaitShort);
                    }
                }
                
                if (bmpHeader != null) { bmpHeader.Dispose(); bmpHeader = null; }

                // 폭 조정 (끌어오기용 축소)
                var (bmpHeaderReload, listLWReload, columnsReload) = CaptureAndDetectColumnBoundaries(m_RcptPage.DG오더_hWnd, rcHeader, targetRow);
                string[] textsReload = await OfrAllColumnsAsync(bmpHeaderReload, listLWReload, columnsReload, headerGab, textHeight, true);
                for (int x = columnsReload - 1; x >= 0; x--)
                {
                    int currentWidth = listLWReload[x + 1].nLeft - listLWReload[x].nLeft;
                    var matched = m_ReceiptDgHeaderInfos.FirstOrDefault(h => h.sName == textsReload[x]);
                    int targetWidth = (matched?.sName.Length ?? (textsReload[x]?.Length ?? 3)) * 18;

                    // 2픽셀 이하 차이면 건너뜀 (이미 비슷한 폭)
                    if (Math.Abs(currentWidth - targetWidth) <= 2) continue;

                    int boundaryX = listLWReload[x + 1].nLeft;
                    int dx = (listLWReload[x].nLeft + targetWidth) - boundaryX;
                    
                    // [원복] 시간 50ms
                    // [변경] dx 대신 ptEndRel 계산하여 호출
                    Draw.Point ptStartRel = new Draw.Point(boundaryX, center);
                    Draw.Point ptEndRel = new Draw.Point(boundaryX + dx, center);
                    await InsungsAct_RcptRegPage.DragAsync_Horizon_Smooth(m_RcptPage.DG오더_hWnd, ptStartRel, ptEndRel, 50);
                    await Task.Delay(c_nWaitShort);
                }
                bmpHeaderReload.Dispose();

                // 확인
                var (bmpFinal, listFinal, countFinal) = CaptureAndDetectColumnBoundaries(m_RcptPage.DG오더_hWnd, rcHeader, targetRow);
                string[] textsFinal = await OfrAllColumnsAsync(bmpFinal, listFinal, countFinal, headerGab, textHeight, true);
                bmpFinal.Dispose();
                if (m_ReceiptDgHeaderInfos.All(h => textsFinal.Contains(h.sName))) break;
            }

            // Step 4: 컬럼 순서 조정
            Debug.WriteLine("[InitDG오더] Step 3: 컬럼 순서 조정 시작");

            for (int x = 0; x < m_ReceiptDgHeaderInfos.Length; x++)
            {
                await CommonFuncs.CheckCancelAndThrowAsync();
                await Task.Delay(CommonVars.c_nWaitNormal);
                var (bmpHeader, listLW, columns) = CaptureAndDetectColumnBoundaries(m_RcptPage.DG오더_hWnd, rcHeader, targetRow);
                if (bmpHeader == null) break;

                string[] texts = await OfrAllColumnsAsync(bmpHeader, listLW, columns, headerGab, textHeight, true);
                string targetText = m_ReceiptDgHeaderInfos[x].sName;
                int index = Array.IndexOf(texts, targetText);

                if (index >= 0 && index != x)
                {
                    Draw.Point ptStart = StdUtil.GetCenterDrawPoint(new Draw.Rectangle(listLW[index].nLeft, headerGab, listLW[index].nWidth, textHeight));
                    Draw.Point ptTarget = new Draw.Point(listLW[x].nLeft + 3, ptStart.Y);
                    await InsungsAct_RcptRegPage.DragAsync_Horizon_Smooth(m_RcptPage.DG오더_hWnd, ptStart, ptTarget, 50);
                    await Task.Delay(CommonVars.c_nWaitLong);
                }
                bmpHeader.Dispose();
            }

            // Step 5: 최종 규격 너비 조정
            Debug.WriteLine("[InitDG오더] Step 4: 규격 너비 조정 시작");
            for (int iter = 0; iter < 2; iter++)
            {
                await CommonFuncs.CheckCancelAndThrowAsync();
                await Task.Delay(CommonVars.c_nWaitNormal);
                var (bmpHeader, listLW, columns) = CaptureAndDetectColumnBoundaries(m_RcptPage.DG오더_hWnd, rcHeader, targetRow);
                if (bmpHeader == null) break;

                string[] texts = await OfrAllColumnsAsync(bmpHeader, listLW, columns, headerGab, textHeight, true);
                for (int x = columns - 1; x >= 0; x--)
                {
                    var matched = m_ReceiptDgHeaderInfos.FirstOrDefault(h => h.sName == texts[x]);
                    if (matched == null) continue;
                    int boundaryX = listLW[x + 1].nLeft;
                    int targetX = listLW[x].nLeft + matched.nWidth;
                    int dx = targetX - boundaryX;
                    if (Math.Abs(dx) > 1)
                    {
                        // [변경] dx 대신 ptEndRel 계산하여 호출
                        Draw.Point ptStartRel = new Draw.Point(boundaryX, center);
                        Draw.Point ptEndRel = new Draw.Point(boundaryX + dx, center);
                        await InsungsAct_RcptRegPage.DragAsync_Horizon_Smooth(m_RcptPage.DG오더_hWnd, ptStartRel, ptEndRel, 50);
                        await Task.Delay(c_nWaitNormal);
                    }
                }
                bmpHeader.Dispose();
            }

            return null;
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("[InitDG오더] ESC 키에 의해 자동화 작업이 취소되었습니다.");
            return new StdResult_Error("사용자 중단 (ESC)", "InitDG오더Async_Cancelled");
        }
        catch (Exception ex)
        {
            return new StdResult_Error($"[{m_Context.AppName}/InitDG오더] 예외발생: {ex.Message}", "InitDG오더Async_999");
        }
        finally
        {
            Debug.WriteLine($"[{m_Context.AppName}] 데이터그리드 초기화 로직 종료 (입력제한 및 후킹 해제)");
            Std32Cursor.SetCursorPos_AbsDrawPt(ptCursorBackup);
            CommonFuncs.ReleaseKeyboardHook();
            Kai.Common.StdDll_Common.StdWin32.StdWin32.BlockInput(false);
        }
    }

    // 접수등록 페이지가 초기화되었는지 확인 (간단 체크)
    public bool IsInitialized()
    {
        if (m_RcptPage.TopWnd_hWnd == IntPtr.Zero || m_RcptPage.DG오더_hWnd == IntPtr.Zero) return false;
        if (m_RcptPage.DG오더_RelChildRects == null || m_RcptPage.DG오더_ColumnTexts == null) return false;
        return true;
    }
    #endregion

    // #region 5. AutoAlloc NewOrder - Kai신규 자동배차
    // 신규 주문 등록 확인 (Kai 전용)
    // public async Task<CommonResult_AutoAllocProcess> CheckIsOrderAsync_AssumeKaiNewOrder(AutoAllocModel item, CancelTokenControl ctrl)
    // {
    //     // Cancel/Pause 체크 - 긴 작업 전
    //     await ctrl.WaitIfPausedOrCancelledAsync();
    // 
    //     string kaiState = item.NewOrder.OrderState;
    // 
    //     switch (kaiState)
    //     {
    //         case "접수":
    //         case "취소":
    //         case "대기":
    //             // 신규 주문 팝업창 열기 → 입력 → 닫기 → 성공 확인
    //             StdResult_Status result = await OpenNewOrderPopupAsync(item, ctrl);
    // 
    //             if (result.Result == StdResult.Success)
    //             {
    //                 // 성공: NotChanged로 재적재 (다음 사이클에서 관리 대상으로 분류됨)
    //                 return CommonResult_AutoAllocProcess.SuccessAndReEnqueue(item, PostgService_Common_OrderState.NotChanged);
    //             }
    //             else
    //             {
    //                 // 실패: 치명적 에러 (신규 등록 실패)
    //                 return CommonResult_AutoAllocProcess.FailureAndDiscard(result.sErr, result.sPos);
    //             }
    // 
    //         case "배차":
    //         case "운행":
    //         case "완료":
    //         case "예약":
    //             return CommonResult_AutoAllocProcess.FailureAndDiscard($"미구현 상태: {kaiState}", "CheckIsOrderAsync_AssumeKaiNewOrder_TODO");
    // 
    //         default:
    //             return CommonResult_AutoAllocProcess.FailureAndDiscard($"알 수 없는 Kai 주문 상태: {kaiState}", "CheckIsOrderAsync_AssumeKaiNewOrder_800");
    //     }
    // }
    // #endregion
// 
    // #region 6. AutoAlloc UpdateOrder - Kai변경 자동배차
    // Kai DB에서 업데이트된 주문을 인성 앱에 반영
    // public async Task<CommonResult_AutoAllocProcess> CheckIsOrderAsync_AssumeKaiUpdated(AutoAllocModel item, CommonResult_AutoAllocDatagrid dgInfo, CancelTokenControl ctrl)
    // {
    //     await ctrl.WaitIfPausedOrCancelledAsync();
    // 
    //     string kaiState = item.NewOrder.OrderState;
    //     string isState = dgInfo.sStatus;
    // 
    //     // 상태가 같은 경우: 필드만 업데이트
    //     if (kaiState == isState) return await UpdateOrderSameStateAsync(item, dgInfo, ctrl);
    //     // 상태가 다른 경우: 필드 업데이트 + 상태 전환
    //     else return await UpdateOrderDiffStateAsync(item, dgInfo, kaiState, isState, ctrl);
    // }
    // 같은 상태: 필드만 선별 업데이트
    // private async Task<CommonResult_AutoAllocProcess> UpdateOrderSameStateAsync(AutoAllocModel item, CommonResult_AutoAllocDatagrid dgInfo, CancelTokenControl ctrl)
    // {
    //     string isState = dgInfo.sStatus;
    // 
    //     // 인성 앱 특성: 상태가 변경되면 저장 안 됨 → 같은 상태 버튼 클릭 필요
    //     // 대기/취소: 외부에서 상태 변경 불가 → 반복 불필요
    //     // 접수/배차: 외부에서 상태 변경 가능 → 타이밍 이슈 대비 반복 필요
    //     switch (dgInfo.sStatus)
    //     {
    //         case "대기":  // 외부 변경 없음 → 1번만
    //             return await UpdateOrderWidelyAsync("", item, dgInfo, false, ctrl);
    // 
    //         case "접수": // 외부 변경 가능 → 10번 재시도
    //         case "배차":
    //             return await UpdateOrderWidelyAsync("", item, dgInfo, true, ctrl);
    // 
    //         case "취소":
    //         case "완료":
    //             return CommonResult_AutoAllocProcess.SuccessAndReEnqueue(item, PostgService_Common_OrderState.NotChanged);
    // 
    //         default:
    //             return CommonResult_AutoAllocProcess.FailureAndDiscard($"미구현 상태(SameState): Kai={isState}, IS={isState}", "UpdateOrderSameStateAsync_999");
    //     }
    // }
    // 
    // 다른 상태: 필드 업데이트 + 상태 전환
    // private async Task<CommonResult_AutoAllocProcess> UpdateOrderDiffStateAsync(AutoAllocModel item, CommonResult_AutoAllocDatagrid dgInfo, string kaiState, string isState, CancelTokenControl ctrl)
    // {
    //     string wantState = kaiState; // Kai DB의 목표 상태로 전환
    //     bool useRepeat;
    // 
    //     // 상태 전환 규칙에 따라 반복 횟수 결정
    //     switch (kaiState)
    //     {
    //         case "접수":
    //             switch (isState)
    //             {
    //                 case "취소": // 취소 → 접수
    //                 case "대기": // 대기 → 접수
    //                     useRepeat = true; // 10번 재시도
    //                     break;
    // 
    //                 case "운행": // 운행 → 접수
    //                     Debug.WriteLine($"  → StateFlag를 NotChanged로 변경 후 재적재 요청");
    //                     return CommonResult_AutoAllocProcess.SuccessAndReEnqueue(item, PostgService_Common_OrderState.NotChanged);
    // 
    //                 default:
    //                     return CommonResult_AutoAllocProcess.FailureAndDiscard($"미구현 전환: Kai=접수, IS={isState}", "InsungsAct_RcptRegPage/pdateOrderDiffStateAsync_01");
    //             }
    //             break;
    // 
    //         case "대기":
    //             switch (isState)
    //             {
    //                 case "취소": // 취소 → 대기
    //                     useRepeat = false; // 1번만
    //                     break;
    //                 case "접수": // 접수 → 대기
    //                 case "배차": // 배차 → 대기
    //                     useRepeat = true; // 10번 재시도
    //                     break;
    //                 default:
    //                     return CommonResult_AutoAllocProcess.FailureAndDiscard($"미구현 전환: Kai=대기, IS={isState}", "InsungsAct_RcptRegPage/UpdateOrderDiffStateAsync_02");
    //             }
    //             break;
    // 
    //         case "취소":
    //             switch (isState)
    //             {
    //                 case "접수": // 접수 → 취소
    //                 case "배차": // 배차 → 취소
    //                 case "운행": // 운행 → 취소
    //                     return await UpdateOrderStateOnlyAsync(wantState, item, dgInfo, true, ctrl); // 10번 재시도
    // 
    //                 case "예약": // 예약 → 취소
    //                 case "완료": // 완료 → 취소
    //                 case "대기": // 대기 → 취소
    //                     return await UpdateOrderStateOnlyAsync(wantState, item, dgInfo, false, ctrl); // 1번만
    // 
    //                 default:
    //                     return CommonResult_AutoAllocProcess.FailureAndDiscard($"미구현 전환: Kai=취소, IS={isState}", "InsungsAct_RcptRegPage/UpdateOrderDiffStateAsync_03");
    //             }
    // 
    //         case "운행":
    //             switch (isState)
    //             {
    //                 case "완료": // 운행 → 완료
    //                     return await CommonVars.s_Order_StatusPage.Insung01운행To완료Async(item, ctrl);
    // 
    //                 default:
    //                     return CommonResult_AutoAllocProcess.FailureAndDiscard($"미구현 전환: Kai=취소, IS={isState}", "InsungsAct_RcptRegPage/UpdateOrderDiffStateAsync_03");
    //             }
    // 
    //         default:
    //             return CommonResult_AutoAllocProcess.FailureAndDiscard($"미구현 상태(DiffState): Kai={kaiState}, IS={isState}", "InsungsAct_RcptRegPage/UpdateOrderDiffStateAsync_04");
    //     }
    // 
    //     // 팝업 열기 → 필드 업데이트 → 상태 전환 → 저장/닫기
    //     return await UpdateOrderWidelyAsync(wantState, item, dgInfo, useRepeat, ctrl);
    // }
    // #endregion

    // #region 7. Status Management - Insung상태 관리
    // Insung 주문 상태 관리 및 모니터링 (NotChanged 상황 처리)
    // public async Task<CommonResult_AutoAllocProcess> CheckIsOrderAsync_InsungOrderManage(AutoAllocModel item, CommonResult_AutoAllocDatagrid dgInfo, CancelTokenControl ctrl)
    // {
    //     // Cancel/Pause 체크
    //     await ctrl.WaitIfPausedOrCancelledAsync();
    // 
    //     string kaiState = item.NewOrder.OrderState;
    //     string isState = dgInfo.sStatus;
    // 
    //     Debug.WriteLine($"[CheckIsOrderAsync_InsungOrderManage] KeyCode={item.KeyCode}, Kai={kaiState}, Insung={isState}");
    // 
    //     // Insung 상태별로 handler 함수 호출 (2중 switch 방지)
    //     switch (isState)
    //     {
    //         case "접수":
    //         case "배차":
    //             return await InsungOrderManage_접수Or배차Async(item, kaiState, dgInfo, ctrl);
    //         case "운행":
    //             return await InsungOrderManage_운행Async(item, kaiState, dgInfo, ctrl);
    //         case "완료":
    //             return await InsungOrderManage_완료Async(item, kaiState, dgInfo, ctrl);
    //         case "대기":
    //             return await InsungOrderManage_대기Async(item, kaiState, dgInfo, ctrl);
    //         case "취소":
    //             return await InsungOrderManage_취소Async(item, kaiState, dgInfo, ctrl);
    // 
    //         default:
    //             Debug.WriteLine($"  → 미정의 Insung 상태: {isState}");
    //             return CommonResult_AutoAllocProcess.SuccessAndReEnqueue(item, PostgService_Common_OrderState.NotChanged);
    //     }
    // }

    // Insung "접수" 또는 "배차" 상태 처리 - Kai 상태별 로깅
    // #pragma warning disable CS1998 // async method lacks await
    // private async Task<CommonResult_AutoAllocProcess> InsungOrderManage_접수Or배차Async(AutoAllocModel item, string kaiState, CommonResult_AutoAllocDatagrid dgInfo, CancelTokenControl ctrl)
    // {
    //     string insungState = dgInfo.sStatus;
    // 
    //     switch (kaiState)
    //     {
    //         case "접수":
    //         case "배차":
    //             Debug.WriteLine($"  → StateFlag를 NotChanged로 변경 후 재적재 요청");
    //             return CommonResult_AutoAllocProcess.SuccessAndReEnqueue(item, PostgService_Common_OrderState.NotChanged);
    // 
    //         default:
    //             Debug.WriteLine($"  → [{insungState}/?] 미정의 Kai 상태: {kaiState}");
    //             return CommonResult_AutoAllocProcess.FailureAndRetry($"미정의 Kai 상태: {kaiState}, Insung={insungState}", "InsungOrderManage_접수Or배차Async_999");
    //     }
    // }
    // #pragma warning restore CS1998
// 
    // Insung "운행" 상태 처리 - 40초 타이머 + Kai 상태별 로깅
    // Insung "운행" 상태 처리 - 40초 타이머 + Kai 상태별 로깅
    // private async Task<CommonResult_AutoAllocProcess> InsungOrderManage_운행Async(AutoAllocModel item, string kaiState, CommonResult_AutoAllocDatagrid dgInfo, CancelTokenControl ctrl)
    // {
    //     Debug.WriteLine($"  → [InsungOrderManage_운행Async] 진입 - KeyCode={item.KeyCode}, RunStartTime={item.RunStartTime?.ToString("HH:mm:ss") ?? "null"}, DriverPhone={item.DriverPhone ?? "null"}");
    // 
    //     switch (kaiState)
    //     {
    //         case "접수":
    //             // 타이머 시작 체크
    //             if (item.RunStartTime == null)
    //             {
    //                 item.RunStartTime = DateTime.Now;
    //                 Debug.WriteLine($"  → [운행/접수] 운행 진입 - 타이머 시작 ({item.RunStartTime:HH:mm:ss}) - 경과: 0.0초 / 40초");
    // 
    //                 // 기사전번 읽기 (캡처된 페이지 이미지 재사용)
    //                 if (dgInfo.BmpPage == null)
    //                 {
    //                     Debug.WriteLine($"  → [운행/접수] 심각한 오류: BmpPage가 null - 자동배차 루프에서 페이지 캡처 실패");
    //                     return CommonResult_AutoAllocProcess.FailureAndRetry("BmpPage가 null - 페이지 캡처 실패", "InsungOrderManage_운행Async_BmpPageNull");
    //                 }
    // 
    //                 int yIndex = dgInfo.nIndex + 2;  // 헤더 2줄 추가
    //                 Draw.Rectangle rectDriverPhNo = m_RcptPage.DG오더_RelChildRects[c_nCol기사전번, yIndex];
    //                 StdResult_String resultDriverPhNo = await GetRowDriverPhNoAsync(dgInfo.BmpPage, rectDriverPhNo, dgInfo.bInvertRgb, ctrl);
    // 
    //                 if (string.IsNullOrEmpty(resultDriverPhNo.strResult))
    //                 {
    //                     Debug.WriteLine($"  → [운행/접수] 심각한 오류: 기사전번 획득 실패 - 운행 상태인데 기사 정보 없음: {resultDriverPhNo.sErr}");
    //                     return CommonResult_AutoAllocProcess.FailureAndRetry($"기사전번 OFR 실패: {resultDriverPhNo.sErr}", "InsungOrderManage_운행Async_DriverPhNoFail");
    //                 }
    // 
    //                 item.DriverPhone = resultDriverPhNo.strResult;
    //                 Debug.WriteLine($"  → [운행/접수] 기사전번 획득: {item.DriverPhone}");
    // 
    //                 return CommonResult_AutoAllocProcess.SuccessAndReEnqueue(item);  // item 업데이트하며 재적재
    //             }
    // 
    //             // 타이머 체크
    //             TimeSpan elapsed = DateTime.Now - item.RunStartTime.Value;
    //             Debug.WriteLine($"  → [운행/접수] 운행 중 - 경과 시간: {elapsed.TotalSeconds:F1}초");
    // 
    //             // 기사전번 다시 OFR (기사 변경 감지용)
    //             if (dgInfo.BmpPage == null)
    //             {
    //                 Debug.WriteLine($"  → [운행/접수] 심각한 오류: BmpPage가 null - 자동배차 루프에서 페이지 캡처 실패");
    //                 return CommonResult_AutoAllocProcess.FailureAndRetry("BmpPage가 null - 페이지 캡처 실패", "InsungOrderManage_운행Async_BmpPageNull2");
    //             }
    // 
    //             int yIndexCheck = dgInfo.nIndex + 2;  // 헤더 2줄 추가
    //             Draw.Rectangle rectDriverPhNoCheck = m_RcptPage.DG오더_RelChildRects[c_nCol기사전번, yIndexCheck];
    //             StdResult_String resultDriverPhNoCheck = await GetRowDriverPhNoAsync(dgInfo.BmpPage, rectDriverPhNoCheck, dgInfo.bInvertRgb, ctrl);
    // 
    //             if (string.IsNullOrEmpty(resultDriverPhNoCheck.strResult))
    //             {
    //                 Debug.WriteLine($"  → [운행/접수] 심각한 오류: 기사전번 획득 실패 - 운행 상태인데 기사 정보 없음: {resultDriverPhNoCheck.sErr}");
    //                 return CommonResult_AutoAllocProcess.FailureAndRetry($"기사전번 OFR 실패: {resultDriverPhNoCheck.sErr}", "InsungOrderManage_운행Async_DriverPhNoFail2");
    //             }
    // 
    //             // 기사 변경 체크
    //             if (resultDriverPhNoCheck.strResult != item.DriverPhone)
    //             {
    //                 Debug.WriteLine($"  → [운행/접수] 기사 변경 감지! 기존: {item.DriverPhone} → 새: {resultDriverPhNoCheck.strResult}");
    //                 item.DriverPhone = resultDriverPhNoCheck.strResult;
    //                 item.RunStartTime = DateTime.Now;
    //                 Debug.WriteLine($"  → [운행/접수] 타이머 리셋 - 새로운 40초 대기 시작 ({item.RunStartTime:HH:mm:ss})");
    //                 return CommonResult_AutoAllocProcess.SuccessAndReEnqueue(item);  // item 업데이트하며 재적재
    //             }
    // 
    //             // 40초 경과 체크 (기사 변경 없음)
    //             if (elapsed.TotalSeconds < 40)
    //             {
    //                 // 40초 미만 - 계속 대기
    //                 Debug.WriteLine($"  → [운행/접수] 40초 대기 중 - 경과: {elapsed.TotalSeconds:F1}초 / 40초");
    //                 return CommonResult_AutoAllocProcess.SuccessAndReEnqueue(item);  // item 유지하며 재적재
    //             }
    // 
    //             // 40초 이상 경과 - 기사 확정
    //             Debug.WriteLine($"  → [운행/접수] 40초 경과! 기사 확정 상태 (기사전번: {item.DriverPhone})");
    // 
    //             // 타이머 파괴
    //             item.RunStartTime = null;
    // 
    //             // 1. 기사 정보 읽기용 팝업 열기 (DG 더블클릭 → 3초 대기 → 닫기)
    //             Debug.WriteLine($"  → [운행/접수] 기사 정보 읽기용 팝업 열기 시작");
    //             StdResult_Status resultPopup = await OpenReadPopupAsync(dgInfo.nIndex, item, ctrl);
    //             if (resultPopup.Result != StdResult.Success)
    //             {
    //                 Debug.WriteLine($"  → [운행/접수] 기사 정보 읽기 실패: {resultPopup.sErr}");
    //                 return CommonResult_AutoAllocProcess.FailureAndRetry(resultPopup.sErr, resultPopup.sPos);
    //             }
    //             Debug.WriteLine($"  → [운행/접수] 기사 정보 읽기 성공");
    // 
    //             // 2. Order_StatusPage에서 처리 (DB 업데이트, 다른 앱 취소)
    //             return await CommonVars.s_Order_StatusPage.Insung01배차To운행Async(item, ctrl);
    // 
    //         case "운행": // 같은상태 
    //             return CommonResult_AutoAllocProcess.SuccessAndReEnqueue();
    // 
    //         default:
    //             Debug.WriteLine($"  → [운행/{kaiState}] 미정의 Kai 상태: {kaiState}");
    //             return CommonResult_AutoAllocProcess.FailureAndRetry($"미정의 Kai 상태: {kaiState}, Insung=운행", "InsungOrderManage_운행Async_999");
    //     }
    // }
// 
    // Insung "완료" 상태 처리 - Kai 상태별 로깅
    // Insung "완료" 상태 처리 - Kai 상태별 로깅
    // private async Task<CommonResult_AutoAllocProcess> InsungOrderManage_완료Async(AutoAllocModel item, string kaiState, CommonResult_AutoAllocDatagrid dgInfo, CancelTokenControl ctrl)
    // {
    //     switch (kaiState)
    //     {
    //         case "운행":
    //             return await CommonVars.s_Order_StatusPage.Insung01운행To완료Async(item, ctrl);
    // 
    //         default:
    //             Debug.WriteLine($"  → [완료/{kaiState}] 미정의 Kai 상태: {kaiState}");
    //             return CommonResult_AutoAllocProcess.FailureAndRetry($"미정의 Kai 상태: {kaiState}, Insung=완료", "InsungOrderManage_완료Async_999");
    //     }
    // }
// 
    // Insung "대기" 상태 처리 - Kai 상태별 로깅
//#pragma warning disable CS1998 // async method lacks await
    // Insung "대기" 상태 처리 - Kai 상태별 로깅
    // #pragma warning disable CS1998 // async method lacks await
    // private async Task<CommonResult_AutoAllocProcess> InsungOrderManage_대기Async(AutoAllocModel item, string kaiState, CommonResult_AutoAllocDatagrid dgInfo, CancelTokenControl ctrl)
    // {
    //     switch (kaiState)
    //     {
    //         case "대기":
    //             return CommonResult_AutoAllocProcess.SuccessAndReEnqueue(item, PostgService_Common_OrderState.NotChanged);
    // 
    //         default:
    //             Debug.WriteLine($"  → [대기/{kaiState}] 미정의 Kai 상태: {kaiState}");
    //             return CommonResult_AutoAllocProcess.FailureAndRetry($"미정의 Kai 상태: {kaiState}, Insung=대기", "InsungOrderManage_대기Async_999");
    //     }
    // }
    // #pragma warning restore CS1998
// 
    // Insung "취소" 상태 처리 - Kai 상태별 로깅
    // #pragma warning disable CS1998 // async method lacks await
    // private async Task<CommonResult_AutoAllocProcess> InsungOrderManage_취소Async(AutoAllocModel item, string kaiState, CommonResult_AutoAllocDatagrid dgInfo, CancelTokenControl ctrl)
    // {
    //     switch (kaiState)
    //     {
    //         case "취소":
    //             return CommonResult_AutoAllocProcess.SuccessAndDestroy(item);
    // 
    //         case "대기": // 취소 -> 대기
    //             return await UpdateOrderStateOnlyAsync("대기", item, dgInfo, false, ctrl); // 1번만
    // 
    //         default:
    //             Debug.WriteLine($"  → [취소/{kaiState}] 미정의 Kai 상태: {kaiState}");
    //             return CommonResult_AutoAllocProcess.FailureAndRetry($"미정의 Kai 상태: {kaiState}, Insung=취소", "InsungOrderManage_취소Async_999");
    //     }
    // }
    // #pragma warning restore CS1998
    // #endregion
}
#nullable enable
