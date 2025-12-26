using System.Diagnostics;
using System.Windows;
using Draw = System.Drawing;
using Media = System.Windows.Media;

using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;
using Kai.Common.NetDll_WpfCtrl.NetOFR;
using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgBox;

using Kai.Client.CallCenter.Classes;
using Kai.Client.CallCenter.Classes.Class_Master;
using Kai.Client.CallCenter.OfrWorks;
using Kai.Client.CallCenter.Windows;
using static Kai.Client.CallCenter.Classes.CommonVars;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Services;

namespace Kai.Client.CallCenter.Networks.NwOnecalls;
#nullable disable

// 원콜 접수등록 페이지 제어
public partial class OnecallAct_RcptRegPage
{
    #region Constants
    private const double c_dOfrWeight = 0.7;
    #endregion // Constants

    #region Datagrid Column Header Info
    // Datagrid 컬럼 헤더 정보 배열 (21개)
    public readonly CModel_DgColumnHeader[] m_ReceiptDgHeaderInfos = new CModel_DgColumnHeader[]
    {
        new CModel_DgColumnHeader() { sName = "순번", bOfrSeq = true, nWidth = 50 },
        new CModel_DgColumnHeader() { sName = "처리상태", bOfrSeq = false, nWidth = 65 },
        new CModel_DgColumnHeader() { sName = "오더번호", bOfrSeq = true, nWidth = 100 },
        new CModel_DgColumnHeader() { sName = "처리일자", bOfrSeq = true, nWidth = 105 },
        new CModel_DgColumnHeader() { sName = "처리시간", bOfrSeq = true, nWidth = 85 },
        new CModel_DgColumnHeader() { sName = "상차지", bOfrSeq = false, nWidth = 125 },
        new CModel_DgColumnHeader() { sName = "하차지", bOfrSeq = false, nWidth = 125 },
        new CModel_DgColumnHeader() { sName = "결제방법", bOfrSeq = false, nWidth = 65 },
        new CModel_DgColumnHeader() { sName = "운임", bOfrSeq = true, nWidth = 80 },
        new CModel_DgColumnHeader() { sName = "수수료", bOfrSeq = true, nWidth = 75 },
        new CModel_DgColumnHeader() { sName = "차종", bOfrSeq = false, nWidth = 70 },
        new CModel_DgColumnHeader() { sName = "톤수", bOfrSeq = false, nWidth = 50 },
        new CModel_DgColumnHeader() { sName = "혼적", bOfrSeq = false, nWidth = 60 },
        new CModel_DgColumnHeader() { sName = "차주명", bOfrSeq = false, nWidth = 80 },
        new CModel_DgColumnHeader() { sName = "차주전화", bOfrSeq = true, nWidth = 135 },
        new CModel_DgColumnHeader() { sName = "담당자번호", bOfrSeq = true, nWidth = 135 },
        new CModel_DgColumnHeader() { sName = "적재옵션", bOfrSeq = false, nWidth = 130 },
        new CModel_DgColumnHeader() { sName = "화물정보", bOfrSeq = false, nWidth = 155 },
        new CModel_DgColumnHeader() { sName = "인수증", bOfrSeq = false, nWidth = 60 },
        new CModel_DgColumnHeader() { sName = "상차일", bOfrSeq = true, nWidth = 70 },
        new CModel_DgColumnHeader() { sName = "하차일", bOfrSeq = true, nWidth = 70 },
    };
    #endregion

    #region Constants
    private const int COLUMN_WIDTH_TOLERANCE = 1;  // 컬럼 너비 허용 오차 (픽셀)
    private const int MAX_RETRY = 3;               // 최대 재시도 횟수
    private const int DELAY_RETRY = 500;           // 재시도 대기 시간 (ms)

    public const int c_nCol순번 = 0;
    public const int c_nCol처리상태 = 1;
    public const int c_nCol오더번호 = 2;
    public const int c_nCol클릭 = 0; // 로우 클릭용 (처리일자) - 원콜은 로우전체에 테두리 그려저서 무의미
    #endregion

    #region Private Fields
    private readonly OnecallContext m_Context;
    private OnecallInfo_File fInfo => m_Context.FileInfo;
    private OnecallInfo_Mem mInfo => m_Context.MemInfo;
    private OnecallInfo_Mem.MainWnd mMain => mInfo.Main;
    private OnecallInfo_Mem.RcptRegPage mRcpt => mInfo.RcptPage;
    private string AppName => m_Context.AppName;

    // 마지막으로 읽은 총계 (조회 딜레이 계산용)
    public int m_nLastTotalCount { get; set; } = 0;
    #endregion // Private Fields

    #region 생성자
    public OnecallAct_RcptRegPage(OnecallContext context)
    {
        m_Context = context;
    }
    #endregion // 생성자

    #region InitializeAsync
    // 접수등록 페이지 초기화
    public async Task<StdResult_Error> InitializeAsync(CancelTokenControl ctrl)
    {
        try
        {
            // 0. 사전 작업 - 입력 제한 및 ESC 취소 활성화
            CommonFuncs.SetKeyboardHook();
            StdWin32.BlockInput(true);
            Debug.WriteLine($"[{AppName}] 초기화 시작: BlockInput(true) & KeyboardHook 활성화");

            #region 1. 접수등록Page 윈도우 찾기 및 메인창 안착 대기 (10초)
            for (int i = 0; i < c_nRepeatNormal; i++)
            {
                await CommonFuncs.CheckCancelAndThrowAsync();
                
                // 1. 접수등록Page 윈도우(화물등록) 찾기
                mRcpt.TopWnd_hWnd = Std32Window.FindWindowEx(mMain.WndInfo_MdiClient.hWnd, IntPtr.Zero, null, fInfo.접수등록Page_TopWnd_sWndName);
                
                // 2. 메인 창이 최대화되어 안착했는지 확인 (너비 기준)
                Draw.Rectangle rcMain = Std32Window.GetWindowRect_DrawAbs(mMain.TopWnd_hWnd);
                bool bMainStable = rcMain.Width >= s_Screens.m_WorkingMonitor.Width - 20;

                if (mRcpt.TopWnd_hWnd != IntPtr.Zero && bMainStable) break;

                await Task.Delay(c_nWaitNormal, ctrl.Token);
            }

            if (mRcpt.TopWnd_hWnd == IntPtr.Zero)
                return new StdResult_Error($"[{AppName}/Initialize] 접수등록Page 찾기실패: {fInfo.접수등록Page_TopWnd_sWndName}", "OnecallAct_RcptRegPage/InitializeAsync_01");
       
            #endregion // 1. 접수등록Page 윈도우 찾기

            #region 2. 검색섹션을 찾을때까지 기다린후 자식정보 찾기
            bool bFind = false;
            for (int i = 0; i < c_nRepeatNormal; i++)
            {
                await CommonFuncs.CheckCancelAndThrowAsync();
                await Task.Delay(c_nWaitNormal, ctrl.Token);
                mRcpt.검색섹션_hWndTop = Std32Window.GetWndHandle_FromRelDrawPt(mMain.TopWnd_hWnd, fInfo.접수등록Page_검색섹션_ptChkRelT);
                if ("검색" == Std32Window.GetWindowCaption(mRcpt.검색섹션_hWndTop))
                {
                    bFind = true;
                    await Task.Delay(c_nWaitNormal, ctrl.Token);
                    break;
                }
            }
            if (!bFind) return new StdResult_Error($"[{AppName}/Initialize] 검색섹션_hWnd 찾기실패", "OnecallAct_RcptRegPage/InitializeAsync_02");

            mRcpt.검색섹션_hWnd포커스탈출 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.검색섹션_hWndTop, fInfo.접수등록Page_검색_포커Kill_ptChkRelM);
            mRcpt.검색섹션_hWnd자동조회 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.검색섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_검색_자동조회_rcChkRelM));
            mRcpt.검색섹션_hWnd새로고침버튼 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.검색섹션_hWndTop, fInfo.접수등록Page_검색_새로고침Btn_ptChkRelM);
            mRcpt.검색섹션_hWnd확장버튼 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.검색섹션_hWndTop, fInfo.접수등록Page_검색ExpandBtn_ptChkRelM);

            #endregion // 2. 검색섹션

            #region 3. 접수섹션
            // 3. 접수섹션 Top핸들찾기
            mRcpt.접수섹션_hWndTop = Std32Window.GetWndHandle_FromRelDrawPt(mMain.TopWnd_hWnd, fInfo.접수등록Page_접수섹션_ptChkRelT);

            // 버튼들
            mRcpt.접수섹션_hWnd신규버튼 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, fInfo.접수등록Page_접수_신규Btn_ptChkRelM);
            mRcpt.접수섹션_hWnd저장버튼 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, fInfo.접수등록Page_접수_저장Btn_ptChkRelM);
            mRcpt.접수섹션_hWnd화물취소버튼 = StdWin32.FindWindowEx(mRcpt.접수섹션_hWndTop, IntPtr.Zero, null, "화물취소");
            mRcpt.접수섹션_hWnd화물복사버튼 = StdWin32.FindWindowEx(mRcpt.접수섹션_hWndTop, IntPtr.Zero, null, "화물복사");
            mRcpt.접수섹션_hWnd재접수버튼 = StdWin32.FindWindowEx(mRcpt.접수섹션_hWndTop, IntPtr.Zero, null, "재접수");

            // 상차지/하차지
            mRcpt.접수섹션_hWnd상차지권역 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_접수_상차지권역_rcChkRelM));
            mRcpt.접수섹션_hWnd상차지주소 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, fInfo.접수등록Page_접수_상차지주소_ptChkRelM);
            mRcpt.접수섹션_hWnd하차지권역 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_접수_하차지권역_rcChkRelM));
            mRcpt.접수섹션_hWnd하차지주소 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, fInfo.접수등록Page_접수_하차지주소_ptChkRelM);

            // 화물/운임 정보
            mRcpt.접수섹션_hWnd화물정보 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, fInfo.접수등록Page_접수_화물정보_ptChkRelM);
            mRcpt.접수섹션_hWnd총운임 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, fInfo.접수등록Page_접수_총운임_ptChkRelM);
            mRcpt.접수섹션_hWnd수수료 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, fInfo.접수등록Page_접수_수수료_ptChkRelM);

            // 차량정보
            mRcpt.접수섹션_차량_hWnd톤수 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_접수_톤수_rcChkRelM));
            mRcpt.접수섹션_차량_hWnd차종 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_접수_차종_rcChkRelM));
            mRcpt.접수섹션_차량_hWnd대수 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_접수_대수_rcChkRelM));
            mRcpt.접수섹션_차량_hWnd결재 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_접수_결재_rcChkRelM));

            // 화물중량 및 구분
            mRcpt.접수섹션_hWnd화물중량 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, fInfo.접수등록Page_접수_화물중량_ptChkRelM);
            mRcpt.접수섹션_구분_hWnd독차 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_구분_독차Part_rcChkRelM));
            mRcpt.접수섹션_구분_hWnd혼적 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_구분_혼적Part_rcChkRelM));
            mRcpt.접수섹션_구분_hWnd긴급 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_구분_긴급Part_rcChkRelM));
            mRcpt.접수섹션_구분_hWnd왕복 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_구분_왕복Part_rcChkRelM));
            mRcpt.접수섹션_구분_hWnd경유 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_구분_경유Part_rcChkRelM));

            // 상차/하차 방법 및 일시
            mRcpt.접수섹션_상차방법_hWnd지게차 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_상차방법_지게차Part_rcChkRelM));
            mRcpt.접수섹션_상차방법_hWn호이스트 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_상차방법_호이스트Part_rcChkRelM));
            mRcpt.접수섹션_상차방법_hWnd수해줌 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_상차방법_수해줌Part_rcChkRelM));
            mRcpt.접수섹션_상차방법_hWnd수작업 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_상차방법_수작업Part_rcChkRelM));
            mRcpt.접수섹션_상차방법_hWnd크레인 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_상차방법_크레인Part_rcChkRelM));

            mRcpt.접수섹션_상차일시_hWnd당상 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_상차일시_당상Part_rcChkRelM));
            mRcpt.접수섹션_상차일시_hWnd낼상 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_상차일시_낼상Part_rcChkRelM));
            mRcpt.접수섹션_상차일시_hWnd월상 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_상차일시_월상Part_rcChkRelM));

            mRcpt.접수섹션_하차방법_hWnd지게차 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_하차방법_지게차Part_rcChkRelM));
            mRcpt.접수섹션_하차방법_hWn호이스트 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_하차방법_호이스트Part_rcChkRelM));
            mRcpt.접수섹션_하차방법_hWnd수해줌 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_하차방법_수해줌Part_rcChkRelM));
            mRcpt.접수섹션_하차방법_hWnd수작업 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_하차방법_수작업Part_rcChkRelM));
            mRcpt.접수섹션_하차방법_hWnd크레인 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_하차방법_크레인Part_rcChkRelM));

            mRcpt.접수섹션_하차일시_hWnd당착 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_하차일시_당착Part_rcChkRelM));
            mRcpt.접수섹션_하차일시_hWnd낼착 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_하차일시_낼착Part_rcChkRelM));
            mRcpt.접수섹션_하차일시_hWnd월착 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_하차일시_월착Part_rcChkRelM));
            mRcpt.접수섹션_하차일시_hWnd당_내착 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_하차일시_당_내착Part_rcChkRelM));

            // 화물메모 및 의뢰자
            mRcpt.접수섹션_hWnd화물메모 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, fInfo.접수등록Page_접수_화물메모_ptChkRelM);
            mRcpt.접수섹션_의뢰자_hWnd상호 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_의뢰자_상호_rcChkRelM));
            mRcpt.접수섹션_의뢰자_hWnd전화 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, fInfo.접수등록Page_의뢰자_전화번호_ptChkRelM);

            #endregion // 3. 접수섹션

            #region 4. DG오더 섹션
            // DG오더 찾기
            mRcpt.DG오더_hWndTop = Std32Window.GetWndHandle_FromRelDrawPt(mMain.TopWnd_hWnd, fInfo.접수등록Page_DG오더_ptChkRelT);

            // Background Brightness 계산 (데이터 로드 전에 측정, +10 마진 적용 - 이보다 밝으면 데이터)
            int nBkBright = OfrService.GetCenterPixelBrightnessFrmWndHandle(mRcpt.DG오더_hWndTop);
            mRcpt.DG오더_nBkMarginedBright = nBkBright + 10;
            Debug.WriteLine($"[{AppName}/Initialize] Background Brightness: {nBkBright}, Margined: {mRcpt.DG오더_nBkMarginedBright}");

            // 컬럼 검증/초기화
            var (listLW, error) = await SetDG오더ColumnHeaderAsync(ctrl);
            if (error != null) return error;

            // 공통 변수
            int headerHeight = fInfo.접수등록Page_DG오더_headerHeight;
            int rowHeight = fInfo.접수등록Page_DG오더_dataRowHeight;
            int gab = fInfo.접수등록Page_DG오더_dataGab;
            int dataTextHeight = rowHeight - gab - gab - 1; // 경계 점선 피할려고...
            int columns = listLW.Count;

            // Small Rects (19행) - [col, row] 순서 (화물24시와 통일)
            int smallRowCount = fInfo.접수등록Page_DG오더Small_RowsCount;
            mRcpt.DG오더_rcRelSmallCells = new Draw.Rectangle[columns, smallRowCount];
            mRcpt.DG오더_ptRelChkSmallRows = new Draw.Point[smallRowCount];
            for (int row = 0; row < smallRowCount; row++)
            {
                int cellY = headerHeight + (row * rowHeight) - 2;
                mRcpt.DG오더_ptRelChkSmallRows[row] = new Draw.Point(listLW[0].nLeft + (listLW[0].nWidth / 2), cellY + (rowHeight / 2));
                for (int col = 0; col < columns; col++)
                {
                    mRcpt.DG오더_rcRelSmallCells[col, row] = new Draw.Rectangle(listLW[col].nLeft, cellY + gab, listLW[col].nWidth, dataTextHeight);
                }
            }

            // Large Rects (34행) - [col, row] 순서 (화물24시와 통일)
            int largeRowCount = fInfo.접수등록Page_DG오더Large_RowsCount;
            mRcpt.DG오더_rcRelLargeCells = new Draw.Rectangle[columns, largeRowCount];
            mRcpt.DG오더_ptRelChkLargeRows = new Draw.Point[largeRowCount];
            for (int row = 0; row < largeRowCount; row++)
            {
                int cellY = headerHeight + (row * rowHeight) - 2;
                mRcpt.DG오더_ptRelChkLargeRows[row] = new Draw.Point(listLW[0].nLeft + (listLW[0].nWidth / 2), cellY + (rowHeight / 2));
                for (int col = 0; col < columns; col++)
                {
                    mRcpt.DG오더_rcRelLargeCells[col, row] = new Draw.Rectangle(listLW[col].nLeft, cellY + gab, listLW[col].nWidth, dataTextHeight);
                }
            }

            Debug.WriteLine($"[{AppName}/Initialize] DG오더 섹션 찾음: {mRcpt.DG오더_hWndTop:X} (Cell 생성 완료) ======>");
            #endregion // 4. DG오더 섹션

            #region 6. 초기 총계 읽기 (조회 딜레이 계산용)
            var resultTotal = await Get총계Async(new CancelTokenControl());
            if (resultTotal.nResult >= 0)
            {
                m_nLastTotalCount = resultTotal.nResult;
            }
            #endregion

            #region 7. 자동조회 콤보박스 5초 설정
            await EscapeFocusAsync();
            var result자동조회 = GetAutoRefreshResult("5초");
            var resultSts = await SelectComboBoxItemAsync(mRcpt.검색섹션_hWnd자동조회, result자동조회, mRcpt.검색섹션_hWndTop, fInfo.접수등록Page_검색_자동조회_rcChkRelM);
            #endregion

            return null;
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine($"[{AppName}/Initialize] 사용자 요청으로 초기화 종료 (ESC)");
            return new StdResult_Error("Skip", "OnecallAct_RcptRegPage/InitializeAsync_Skip");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{AppName}/Initialize] 예외: {ex.Message}");
            return new StdResult_Error($"[{AppName}/Initialize] 예외: {ex.Message}", "OnecallAct_RcptRegPage/InitializeAsync_99");
        }
        finally
        {
            Debug.WriteLine($"[{AppName}] 초기화 종료 (KeyboardHook & BlockInput 해제)");
            CommonFuncs.ReleaseKeyboardHook();
            StdWin32.BlockInput(false);
        }
    }
    #endregion // InitializeAsync

    // SetDG오더 섹션
    private async Task<(List<OfrModel_LeftWidth> listLW, StdResult_Error error)> SetDG오더ColumnHeaderAsync(CancelTokenControl ctrl, bool bEdit = true)
    {
        Draw.Bitmap bmpDG = null;
        List<OfrModel_LeftWidth> listLW = null;
        int columns = 0;

        try
        {
            Debug.WriteLine($"[{AppName}] SetDG오더ColumnHeaderAsync 시작");

            // 재시도 루프
            for (int retry = 1; retry <= MAX_RETRY; retry++)
            {
                await CommonFuncs.CheckCancelAndThrowAsync();
                
                // 1. DG 헤더 캡처
                Draw.Rectangle rcDG_Abs = Std32Window.GetWindowRect_DrawAbs(mRcpt.DG오더_hWndTop);
                int headerHeight = fInfo.접수등록Page_DG오더_headerHeight;
                Draw.Rectangle rcHeader = new Draw.Rectangle(0, 0, rcDG_Abs.Width, headerHeight);
                bmpDG = OfrService.CaptureScreenRect_InWndHandle(mRcpt.DG오더_hWndTop, rcHeader);
                if (bmpDG == null)
                    return (null, new StdResult_Error($"[{AppName}] DG 캡처 실패", "SetDG오더ColumnHeaderAsync_01"));
                Debug.WriteLine($"[{AppName}] DG 캡처 완료: {rcHeader.Width}x{rcHeader.Height}");

                // 2. 컬럼 경계 검출 (MinBrightness 방식)
                const int headerGab = 6;
                int textHeight = fInfo.접수등록Page_DG오더_headerHeight - (headerGab * 2);
                int targetRow = headerGab + textHeight;

                byte minBrightness = OfrService.GetMinBrightnessAtRow_FromColorBitmapFast(bmpDG, targetRow);
                if (minBrightness == 255)
                    return (null, new StdResult_Error($"[{AppName}] 최소 밝기 검출 실패", "SetDG오더ColumnHeaderAsync_02"));
                minBrightness += 2;

                bool[] boolArr = OfrService.GetBoolArray_FromColorBitmapRowFast(bmpDG, targetRow, minBrightness, 2);
                if (boolArr == null || boolArr.Length == 0)
                    return (null, new StdResult_Error($"[{AppName}] Bool 배열 생성 실패", "SetDG오더ColumnHeaderAsync_03"));

                listLW = OfrService.GetLeftWidthList_FromBool1Array(boolArr, minBrightness);
                if (listLW == null || listLW.Count < 2)
                    return (null, new StdResult_Error($"[{AppName}] 컬럼 경계 검출 실패: Count={listLW?.Count ?? 0}", "SetDG오더ColumnHeaderAsync_04"));

                // 마지막 항목 제거 (오른쪽 끝 경계)
                listLW.RemoveAt(listLW.Count - 1);

                columns = listLW.Count;
                Debug.WriteLine($"[{AppName}] 컬럼 검출: {columns}개 (목표: {m_ReceiptDgHeaderInfos.Length}개)");

                // 평가 1: 컬럼 개수 확인
                if (columns < m_ReceiptDgHeaderInfos.Length)
                {
                    Debug.WriteLine($"[{AppName}] 컬럼 개수 불일치: 검출={columns}개, 예상={m_ReceiptDgHeaderInfos.Length}개 (재시도 {retry}/{MAX_RETRY})");

                    bmpDG?.Dispose();
                    bmpDG = null;

                    StdResult_Error initResult = await InitDG오더Async(ctrl, CEnum_DgValidationIssue.InvalidColumnCount);

                    if (initResult != null)
                    {
                        if (retry == MAX_RETRY)
                            return (null, new StdResult_Error(
                                $"[{AppName}] 컬럼 개수 부족: 검출={columns}개, 예상={m_ReceiptDgHeaderInfos.Length}개 (재시도 {MAX_RETRY}회 초과)",
                                "SetDG오더ColumnHeaderAsync_05"));

                        await Task.Delay(DELAY_RETRY);
                        continue;
                    }

                    await Task.Delay(DELAY_RETRY);
                    continue;
                }

                // 평가 2: 컬럼 헤더 OFR
                Debug.WriteLine($"[{AppName}] 평가 2: 컬럼 헤더 OFR 시작");
                string[] columnTexts = new string[m_ReceiptDgHeaderInfos.Length];

                for (int i = 0; i < m_ReceiptDgHeaderInfos.Length; i++)
                {
                    Draw.Rectangle rcTmp = new Draw.Rectangle(listLW[i].nLeft + 1, headerGab, listLW[i].nWidth - 2, textHeight);
                    var result = await OfrWork_Common.OfrStr_ComplexCharSetAsync(bmpDG, rcTmp, bInvertRgb: false, bTextSave: true, dWeight: c_dOfrWeight);
                    columnTexts[i] = result?.strResult ?? string.Empty;
                }

                // 평가 3: Datagrid 상태 검증
                Debug.WriteLine($"[{AppName}] 평가 3: Datagrid 상태 검증 시작");
                CEnum_DgValidationIssue validationIssues = ValidateDatagridState(columnTexts, listLW);

                if (validationIssues != CEnum_DgValidationIssue.None)
                {
                    Debug.WriteLine($"[{AppName}] Datagrid 상태 검증 실패: {validationIssues} (재시도 {retry}/{MAX_RETRY})");

                    bmpDG?.Dispose();
                    bmpDG = null;

                    StdResult_Error initResult = await InitDG오더Async(ctrl, validationIssues);

                    if (initResult != null)
                    {
                        if (retry == MAX_RETRY)
                            return (null, new StdResult_Error(
                                $"[{AppName}] Datagrid 상태 검증 실패: {validationIssues} (재시도 {MAX_RETRY}회 초과)",
                                "SetDG오더ColumnHeaderAsync_Validation"));
                    }

                    await Task.Delay(DELAY_RETRY);
                    continue;
                }

                // 모든 평가 통과
                Debug.WriteLine($"[{AppName}] SetDG오더ColumnHeaderAsync 완료: {columns}개 컬럼");
                bmpDG?.Dispose();
                return (listLW, null); // 성공
            }

            // 최대 재시도 초과
            return (null, new StdResult_Error(
                $"[{AppName}] 컬럼헤더 초기화 실패 (재시도 {MAX_RETRY}회 초과)",
                "SetDG오더ColumnHeaderAsync_MaxRetry"));
        }
        catch (OperationCanceledException)
        {
            return (null, new StdResult_Error("Skip", "SetDG오더ColumnHeaderAsync_Skip"));
        }
        catch (Exception ex)
        {
            return (null, new StdResult_Error($"[{AppName}] SetDG오더ColumnHeaderAsync 예외: {ex.Message}", "SetDG오더ColumnHeaderAsync_99"));
        }
        finally
        {
            bmpDG?.Dispose();
        }
    }

    // Datagrid 강제 초기화 (항목초기화 → 컬럼 삭제 → 순서 조정 → 폭 조정)
    private async Task<StdResult_Error> InitDG오더Async(CancelTokenControl ctrl, CEnum_DgValidationIssue issues)
    {
        // 마우스 커서 위치 백업 (작업 완료 후 복원용)
        Draw.Point ptCursorBackup = Std32Cursor.GetCursorPos_AbsDrawPt();

        try
        {
            Debug.WriteLine($"[{AppName}] InitDG오더Async 시작: issues={issues}");

            // 사전 작업 - 이미 상위에서 설정했겠지만 혹시 모르니 보강

            CommonFuncs.SetKeyboardHook();
            StdWin32.BlockInput(true);

            // 상수 정의
            const int headerGab = 6;
            int headerHeight = fInfo.접수등록Page_DG오더_headerHeight;
            int textHeight = headerHeight - (headerGab * 2);
            int targetRow = headerGab + textHeight;

            // DG오더_hWnd 기준으로 헤더 영역 정의
            Draw.Rectangle rcDG = Std32Window.GetWindowRect_DrawAbs(mRcpt.DG오더_hWndTop);
            Draw.Rectangle rcHeader = new Draw.Rectangle(0, 0, rcDG.Width, headerHeight);

            #region Step 1 - 목록초기화
            await CommonFuncs.CheckCancelAndThrowAsync();

            await Std32Mouse_Post.MousePostAsync_ClickLeft(
                Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, fInfo.접수등록Page_접수_목록초기화Btn_ptRelS));
            await Task.Delay(c_nWaitUltraLong, ctrl.Token);
            #endregion

            #region Step 2 - 컬럼 확보 및 50px 축소 (낚시형)
            List<OfrModel_LeftWidth> listLW = null;
            int columns = 0;
            int waitTime = c_nWaitLong;
            bool bResult = false;

            for (int iter = 0; iter < 5; iter++) 
            {
                await Task.Delay(waitTime, ctrl.Token);
                waitTime = 50; 

                // 컬럼영역 캡쳐 및 경계추출
                var analysis = CaptureAndDetectColumnBoundaries(rcHeader, targetRow);
                if (analysis.bmpHeader == null) break;
                analysis.bmpHeader.Dispose();

                listLW = analysis.listLW;
                columns = analysis.columns;

                // 너비 합계 및 마지막 컬럼 위치 계산
                int totalWidth = 0;
                for (int i = 0; i < columns; i++) totalWidth += listLW[i].nWidth;
                int lastRight = (columns > 0) ? (listLW[columns - 1].nLeft + listLW[columns - 1].nWidth) : 0;

                Debug.WriteLine($"[{AppName}] Step 2 반복 {iter + 1}: 검출={columns}개, LastRight={lastRight}px / Grid={rcHeader.Width}px");
                
                // [탈출 조건] 34개 완벽 확보 AND 마지막 컬럼이 그리드 우측 경계 안쪽으로 충분히 들어옴
                if (columns >= 34 && lastRight < rcHeader.Width - 50) 
                {
                    Debug.WriteLine($"[{AppName}] Step 2 목표 달성 (안착 완료)");
                    bResult = true;
                    break;
                }

                // Step 2 핵심: 32개 이상의 컬럼을 시야에 넣기 위해 모든 가용 경계선을 50px로 압축
                for (int x = columns - 1; x >= 0; x--)
                {
                    await CommonFuncs.CheckCancelAndThrowAsync();

                    int boundaryX = listLW[x + 1].nLeft - 1;
                    int targetX = listLW[x].nLeft + 50;
                    int dx = targetX - boundaryX;

                    if (listLW[x].nWidth > 60 || Math.Abs(dx) > 5) 
                    {
                        Std32Cursor.SetCursorPos_RelDrawPt(mRcpt.DG오더_hWndTop, new Draw.Point(boundaryX, 15));
                        await Task.Delay(10, ctrl.Token);

                        if (!OnecallAct_RcptRegPage.IsHorizontalResizeCursor()) continue;

                        await OnecallAct_RcptRegPage.DragAsync_Horizontal_FromBoundary(
                            hWnd: mRcpt.DG오더_hWndTop, 
                            ptStartRel: new Draw.Point(boundaryX, 15), 
                            dx: dx, 
                            gripCheck: OnecallAct_RcptRegPage.IsHorizontalResizeCursor, 
                            nRetryCount: 3, 
                            nMiliSec: 50, 
                            nSafetyMargin: 5, 
                            nDelayAtSafety: 20);
                            
                        await Task.Delay(100, ctrl.Token); 
                    }
                }
            }
            if (!bResult) return new StdResult_Error(
                $"[{AppName}] Step 2 실패: 목표 상태(34개 컬럼 및 안착) 도달 불가", "OnecallAct_RcptRegPage/InitDG오더Async_001");
            #endregion

            #region Step 3 - 컬럼 순서 조정 (정밀 좌표 로직 리팩토링)
            // currentGridOrder: 현재 물리적으로 그리드에 배치된 컬럼 순서 (최초에는 원본 순서로 시작)
            List<string> currentGridOrder = new List<string>(fInfo.접수등록Page_DG오더_colOrgTexts);
            
            // 가상 레이아웃 헬퍼: 현재 listLW의 순서와 너비를 기준으로 특정 슬롯의 시작 X좌표를 계산 (누적 오차 방지)
            int GetVirtualSlotLeft(int index)
            {
                if (index <= 0) return listLW[0].nLeft;
                int left = listLW[0].nLeft;
                for (int i = 0; i < index; i++) left += listLW[i].nWidth + 1; // 1px 경계선(Grid Line) 포함
                return left;
            }

            Debug.WriteLine($"[{AppName}/Step3] 컬럼 순서 조정 시작 (대상: {m_ReceiptDgHeaderInfos.Length}개)");

            for (int targetIdx = 0; targetIdx < m_ReceiptDgHeaderInfos.Length; targetIdx++)
            {
                await CommonFuncs.CheckCancelAndThrowAsync();

                string targetName = m_ReceiptDgHeaderInfos[targetIdx].sName;
                int currentIdx = currentGridOrder.IndexOf(targetName);

                if (currentIdx < 0)
                {
                    Debug.WriteLine($"[{AppName}/Step3] 경고: 컬럼 '{targetName}'을 찾을 수 없음");
                    continue;
                }

                if (currentIdx == targetIdx) 
                {
                    Debug.WriteLine($"[{AppName}/Step3] [{targetName}] 이미 정위치 ({targetIdx})");
                    continue;
                }

                // [Refactored] 정밀 좌표 산정
                // 1. 출발지(Grab Point): 이동할 컬럼 슬롯의 정중앙 (50%)
                int startX = GetVirtualSlotLeft(currentIdx) + (listLW[currentIdx].nWidth / 2);
                
                // 2. 도착지(Drop Sweet-Spot): 목표 슬롯의 1/4 지점 (삽입 인디케이터 유도를 위한 최적 좌표)
                int targetSlotLeft = GetVirtualSlotLeft(targetIdx);
                int endX = targetSlotLeft + (listLW[targetIdx].nWidth / 2) - 3; // 중앙에서 좌측으로 3px (안전한 드롭)

                Debug.WriteLine($"[{AppName}/Step3] 순서 조정 실행: [{targetName}] (슬롯 {currentIdx} -> {targetIdx}), 좌표 {startX} -> {endX}");

                // 3. 드래그 실행 (심해 전용 엔진)
                bool success = await OnecallAct_RcptRegPage.DragAsync_Horizontal_FromCenter(
                    hWnd: mRcpt.DG오더_hWndTop,
                    ptStartRel: new Draw.Point(startX, 15),
                    ptTargetRel: new Draw.Point(endX, 15),
                    nRetryCount: 5);

                if (success)
                {
                    // 4. 상태 동기화 (물리적 너비 리스트와 논리적 순서 리스트를 함께 재배치)
                    var movedLW = listLW[currentIdx];
                    listLW.RemoveAt(currentIdx);
                    listLW.Insert(targetIdx, movedLW);

                    currentGridOrder.RemoveAt(currentIdx);
                    currentGridOrder.Insert(targetIdx, targetName);

                    // MsgBox($"[{targetName}] 이동 완료 ({currentIdx} -> {targetIdx})\n현재 순서: {string.Join(", ", currentGridOrder)}");
                    await Task.Delay(200, ctrl.Token); // 그리드 재배치 애니메이션 대기
                }
                else
                {
                    Debug.WriteLine($"[{AppName}/Step3] [{targetName}] 드래그 실패 또는 스킵됨");
                    // MsgBox($"[{targetName}] 이동 실패 또는 스킵됨");
                }
            }

            #endregion // Step 3



            #region Step 4 - 컬럼 너비 조정 (최종 정밀 조정)
            // 1. 현재 헤더 상태 파악
            var preLayout = CaptureAndDetectColumnBoundaries(rcHeader, targetRow);
            preLayout.bmpHeader?.Dispose();

            if (preLayout.listLW != null)
            {
                // 조정 루프 (과정 로그는 사용자 요청으로 제거)
                for (int x = m_ReceiptDgHeaderInfos.Length - 1; x >= 0; x--)
                {
                    await CommonFuncs.CheckCancelAndThrowAsync();

                    if (x + 1 >= preLayout.listLW.Count) continue;

                    int currentWidth = preLayout.listLW[x].nWidth;
                    int targetWidth = m_ReceiptDgHeaderInfos[x].nWidth;
                    int dx = targetWidth - currentWidth;

                    // 2픽셀 이상 차이날 때만 보정
                    if (Math.Abs(dx) >= 2)
                    {
                        int boundaryX = preLayout.listLW[x + 1].nLeft - 1;
                        int dragY = 15;

                        await OnecallAct_RcptRegPage.DragAsync_Horizontal_FromBoundary(
                            hWnd: mRcpt.DG오더_hWndTop,
                            ptStartRel: new Draw.Point(boundaryX, dragY),
                            dx: dx,
                            gripCheck: OnecallAct_RcptRegPage.IsHorizontalResizeCursor,
                            nRetryCount: 5,
                            nMiliSec: 100,
                            nSafetyMargin: 5,
                            nDelayAtSafety: 20);

                        await Task.Delay(100, ctrl.Token);
                    }
                }

                // 2. 조정 완료 후 최종 상태 다시 캡처 및 검증 로그 (요청 사항)
                var postLayout = CaptureAndDetectColumnBoundaries(rcHeader, targetRow);
                postLayout.bmpHeader?.Dispose();

                if (postLayout.listLW != null)
                {
                    Debug.WriteLine($"[{AppName}/Step4] === 컬럼 너비 최종 조정 결과 검증 (Tolerance: {COLUMN_WIDTH_TOLERANCE}px) ===");
                    for (int i = 0; i < m_ReceiptDgHeaderInfos.Length; i++)
                    {
                        if (i >= postLayout.listLW.Count) break;

                        int actW = postLayout.listLW[i].nWidth;
                        int expW = m_ReceiptDgHeaderInfos[i].nWidth;
                        int diff = actW - expW;
                        string status = (Math.Abs(diff) <= COLUMN_WIDTH_TOLERANCE) ? "[OK]" : "[CHECK]";
                        
                        Debug.WriteLine($"{status} {i:D2}. {m_ReceiptDgHeaderInfos[i].sName,-10} : 실측={actW} / 정석={expW} (오차={diff})");
                    }
                    Debug.WriteLine($"[{AppName}/Step4] ===================================================================");
                }
            }

            #endregion

            MsgBox($"[{AppName}] 데이터그리드 정석 초기화 완료 (Step 1~4)");

            Debug.WriteLine($"[{AppName}] InitDG오더Async 완료");

            return null;
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine($"[{AppName}/InitDG오더] ESC 키에 의해 자동화 작업이 취소되었습니다.");
            throw;
        }
        catch (Exception ex)
        {
            return new StdResult_Error($"[{AppName}] InitDG오더Async 예외: {ex.Message}", "OnecallAct_RcptRegPage/InitDG오더Async_99");
        }
        finally
        {
            // 마우스 커서 위치 복원
            Std32Cursor.SetCursorPos_AbsDrawPt(ptCursorBackup);
        }
    }
    // #endregion

    //#region 자동배차 - Kai신규 관련함수들
    // 신규 주문 처리 (Kai 신규 → 원콜 등록)
    //public async Task<CommonResult_AutoAllocProcess> CheckOcOrderAsync_AssumeKaiNewOrder(AutoAllocModel item, CancelTokenControl ctrl)
    //{
    //    await ctrl.WaitIfPausedOrCancelledAsync();
    //
    //    string kaiState = item.NewOrder.OrderState;
    //    Debug.WriteLine($"[{AppName}] CheckOcOrderAsync_AssumeKaiNewOrder: KeyCode={item.KeyCode}, kaiState={kaiState}");
    //
    //    switch (kaiState)
    //    {
    //        case "접수": // 신규등록모드 → 입력 → 저장
    //            return await RegistOrderModeAsync(item, ctrl);
    //
    //        case "취소": // 무시 - 비적재
    //        case "대기":
    //            return CommonResult_AutoAllocProcess.SuccessAndDestroy(item);
    //
    //        case "배차":
    //        case "운행":
    //        case "완료":
    //        case "예약":
    //            Debug.WriteLine($"[{AppName}] 미구현 상태: {kaiState}");
    //            return CommonResult_AutoAllocProcess.FailureAndDiscard($"미구현 상태: {kaiState}", "CheckOcOrderAsync_AssumeKaiNewOrder_TODO");
    //
    //        default:
    //            Debug.WriteLine($"[{AppName}] 알 수 없는 Kai 주문 상태: {kaiState}");
    //            return CommonResult_AutoAllocProcess.FailureAndDiscard($"알 수 없는 Kai 주문 상태: {kaiState}", "CheckOcOrderAsync_AssumeKaiNewOrder_800");
    //    }
    //}

    // 신규등록모드: 신규버튼 클릭 → 정보 입력 → 저장
    //public async Task<CommonResult_AutoAllocProcess> RegistOrderModeAsync(AutoAllocModel item, CancelTokenControl ctrl)
    //{
    //    try
    //    {
    //        #region 1. 사전작업
    //        Debug.WriteLine($"[{AppName}] RegistOrderModeAsync 진입: KeyCode={item.KeyCode}");
    //
    //        // 로컬 변수
    //        TbOrder tbOrder = item.NewOrder;
    //        bool bTmp = false;
    //
    //        // 데이터그리드 축소 (신규버튼 접근 위해)
    //        await CollapseDG오더Async();
    //
    //        // 신규 버튼 클릭
    //        await ctrl.WaitIfPausedOrCancelledAsync();
    //        await Std32Mouse_Post.MousePostAsync_ClickLeft(mRcpt.접수섹션_hWnd신규버튼);
    //        Debug.WriteLine($"[{AppName}] 신규버튼 클릭");
    //
    //        // 신규등록모드 진입 확인 (상차지주소 캡션이 빌때까지 대기)
    //        bool bNewMode = false;
    //        for (int i = 0; i < c_nRepeatShort; i++)
    //        {
    //            await Task.Delay(c_nWaitShort);
    //            string caption = Std32Window.GetWindowCaption(mRcpt.접수섹션_hWnd상차지주소);
    //            if (string.IsNullOrEmpty(caption))
    //            {
    //                bNewMode = true;
    //                Debug.WriteLine($"[{AppName}] 신규등록모드 진입 확인");
    //                break;
    //            }
    //        }
    //        if (!bNewMode)
    //        {
    //            return CommonResult_AutoAllocProcess.FailureAndDiscard("신규등록모드 진입 실패", "RegistOrderModeAsync_01");
    //        }
    //        #endregion
    //
    //        #region 2. 주문 정보 입력
    //        Debug.WriteLine($"[{AppName}] #region 2 시작: 상차={tbOrder.StartDetailAddr}, 하차={tbOrder.DestDetailAddr}");
    //
    //        // 상차지 입력
    //        var result상차 = await Set상세주소Async(mRcpt.접수섹션_hWnd상차지주소, fInfo.접수등록Page_접수_상차지권역_rcChkRelM, tbOrder.StartDetailAddr, ctrl);
    //        Debug.WriteLine($"[{AppName}] 상차지 결과: {result상차.Result}, {result상차.sErr}");
    //        if (result상차.Result != StdResult.Success)
    //            return CommonResult_AutoAllocProcess.FailureAndDiscard($"상차지 입력실패: {result상차.sErr}", "RegistOrderModeAsync_02");
    //
    //        // 하차지 입력
    //        var result하차 = await Set상세주소Async(mRcpt.접수섹션_hWnd하차지주소, fInfo.접수등록Page_접수_하차지권역_rcChkRelM, tbOrder.DestDetailAddr, ctrl);
    //        Debug.WriteLine($"[{AppName}] 하차지 결과: {result하차.Result}, {result하차.sErr}");
    //        if (result하차.Result != StdResult.Success)
    //            return CommonResult_AutoAllocProcess.FailureAndDiscard($"하차지 입력실패: {result하차.sErr}", "RegistOrderModeAsync_03");
    //
    //        // 화물정보 - 디비에 적요가 있으면 쓰고, 없으면 없음을 쓴다
    //        if (string.IsNullOrEmpty(tbOrder.OrderRemarks)) Std32Window.SetWindowCaption(mRcpt.접수섹션_hWnd화물정보, "없음");
    //        else Std32Window.SetWindowCaption(mRcpt.접수섹션_hWnd화물정보, tbOrder.OrderRemarks);
    //        await Task.Delay(c_nWaitShort, ctrl.Token);
    //        Std32Key_Msg.KeyPost_Click(mRcpt.접수섹션_hWnd화물정보, StdCommon32.VK_RETURN);
    //
    //        // 운임
    //        if (tbOrder.FeeTotal > 0) // 총운임
    //        {
    //            //bTmp = await Simulation_Keyboard.PostFeeWithVerifyAsync(mRcpt.접수섹션_hWnd총운임, tbOrder.FeeTotal);
    //            if (!bTmp) return CommonResult_AutoAllocProcess.FailureAndDiscard($"총운임 입력실패: {tbOrder.FeeTotal}", "RegistOrderModeAsync_04");
    //        }
    //        if (tbOrder.FeeCharge > 0) // 수수료
    //        {
    //            //bTmp = await Simulation_Keyboard.PostFeeWithVerifyAsync(mRcpt.접수섹션_hWnd수수료, tbOrder.FeeCharge);
    //            if (!bTmp) return CommonResult_AutoAllocProcess.FailureAndDiscard($"총운임 입력실패: {tbOrder.FeeTotal}", "RegistOrderModeAsync_05");
    //        }
    //
    //        // 차량 - 톤수
    //        await EscapeFocusAsync();
    //        CModel_ComboBox result톤수 = GetCarWeightResult(tbOrder.CarType, tbOrder.CarWeight);
    //        StdResult_Status resultSts = await SelectComboBoxItemAsync(mRcpt.접수섹션_차량_hWnd톤수, result톤수, mRcpt.접수섹션_hWndTop, fInfo.접수등록Page_접수_톤수_rcChkRelM);
    //        if (resultSts.Result != StdResult.Success)
    //            return CommonResult_AutoAllocProcess.FailureAndDiscard($"톤수 선택실패: {resultSts.sErr}", "RegistOrderModeAsync_06");
    //
    //        // 차량 - 차종
    //        await EscapeFocusAsync();
    //        CModel_ComboBox result차종 = GetTruckDetailResult(tbOrder.CarType, tbOrder.TruckDetail);
    //        resultSts = await SelectComboBoxItemAsync(mRcpt.접수섹션_차량_hWnd차종, result차종, mRcpt.접수섹션_hWndTop, fInfo.접수등록Page_접수_차종_rcChkRelM);
    //        if (resultSts.Result != StdResult.Success)
    //            return CommonResult_AutoAllocProcess.FailureAndDiscard($"차종 선택실패: {resultSts.sErr}", "RegistOrderModeAsync_07");
    //
    //        // 차량 - 결재
    //        await EscapeFocusAsync();
    //        CModel_ComboBox result결재 = GetFeeTypeResult(tbOrder.FeeType);
    //        resultSts = await SelectComboBoxItemAsync(mRcpt.접수섹션_차량_hWnd결재, result결재, mRcpt.접수섹션_hWndTop, fInfo.접수등록Page_접수_결재_rcChkRelM);
    //        if (resultSts.Result != StdResult.Success)
    //            return CommonResult_AutoAllocProcess.FailureAndDiscard($"결재 선택실패: {resultSts.sErr}", "RegistOrderModeAsync_08");
    //
    //        // 화물중량
    //        string maxWeight = GetMaxCarWeight(result톤수);
    //        if (maxWeight != "0.00")
    //        {
    //            var result화물중량 = await Set화물중량Async(maxWeight, ctrl);
    //            if (result화물중량.Result != StdResult.Success)
    //                return CommonResult_AutoAllocProcess.FailureAndDiscard($"화물중량 설정실패: {result화물중량.sErr}", "RegistOrderModeAsync_09");
    //        }
    //
    //        // 구분
    //        switch (tbOrder.DeliverType)
    //        {
    //            case "왕복":
    //                resultSts = await SetCheckBoxAsync(mRcpt.접수섹션_구분_hWnd왕복, fInfo.접수등록Page_구분_왕복Part_rcChkRelM, true, "왕복");
    //                break;
    //
    //            case "경유":
    //                resultSts = await SetCheckBoxAsync(mRcpt.접수섹션_구분_hWnd경유, fInfo.접수등록Page_구분_경유Part_rcChkRelM, true, "경유");
    //                break;
    //
    //            case "긴급":
    //                resultSts = await SetCheckBoxAsync(mRcpt.접수섹션_구분_hWnd긴급, fInfo.접수등록Page_구분_긴급Part_rcChkRelM, true, "긴급");
    //                break;
    //
    //            default:
    //                resultSts = new StdResult_Status(StdResult.Success);
    //                break;
    //        }
    //        if (resultSts.Result != StdResult.Success)
    //            return CommonResult_AutoAllocProcess.FailureAndDiscard($"배송방법 선택실패: {resultSts.sErr}", "RegistOrderModeAsync_09");
    //
    //        // 상차방법 - 개선해야함(우선은 수작업)
    //        string sLoadMethod = "수작업";
    //        resultSts = await SetCheckBoxAsync(mRcpt.접수섹션_상차방법_hWnd수작업, fInfo.접수등록Page_상차방법_수작업Part_rcChkRelM, true, "수작업");
    //        if (resultSts.Result != StdResult.Success)
    //            return CommonResult_AutoAllocProcess.FailureAndDiscard($"상차방법 선택실패: {resultSts.sErr}", "RegistOrderModeAsync_10");
    //
    //        // 상차일시 - 개선해야함(우선은 당상)
    //        string sLoadDate = "당상";
    //        resultSts = await SetCheckBoxAsync(mRcpt.접수섹션_상차일시_hWnd당상, fInfo.접수등록Page_상차일시_당상Part_rcChkRelM, true, "당상");
    //        if (resultSts.Result != StdResult.Success)
    //            return CommonResult_AutoAllocProcess.FailureAndDiscard($"상차일시 선택실패: {resultSts.sErr}", "RegistOrderModeAsync_11");
    //
    //        // 하차방법 - 개선해야함(우선은 수작업)
    //        string sUnloadMethod = "수작업";
    //        resultSts = await SetCheckBoxAsync(mRcpt.접수섹션_하차방법_hWnd수작업, fInfo.접수등록Page_하차방법_수작업Part_rcChkRelM, true, "수작업");
    //        if (resultSts.Result != StdResult.Success)
    //            return CommonResult_AutoAllocProcess.FailureAndDiscard($"하차방법 선택실패: {resultSts.sErr}", "RegistOrderModeAsync_12");
    //
    //        // 하차일시 - 개선해야함(우선은 당상)
    //        string sUnloadDate = "당착";
    //        resultSts = await SetCheckBoxAsync(mRcpt.접수섹션_하차일시_hWnd당착, fInfo.접수등록Page_하차일시_당착Part_rcChkRelM, true, "당착");
    //        if (resultSts.Result != StdResult.Success)
    //            return CommonResult_AutoAllocProcess.FailureAndDiscard($"하차일시 선택실패: {resultSts.sErr}", "RegistOrderModeAsync_13");
    //
    //        // 화물메모
    //        await EscapeFocusAsync(ctrl.Token, 100);
    //        string sMemo = "화물메모 테스트"; // tbOrder.OrderMemo
    //        Std32Window.SetWindowCaption(mRcpt.접수섹션_hWnd화물메모, sMemo);
    //
    //        // 의뢰자
    //        await EscapeFocusAsync(ctrl.Token, 100);
    //        string sTelNo = StdConvert.ToPhoneNumberFormat(tbOrder.CallTelNo);
    //        Std32Window.SetWindowCaption(mRcpt.접수섹션_의뢰자_hWnd전화, sTelNo); // 전화번호
    //
    //        await EscapeFocusAsync(ctrl.Token, 100);
    //        Std32Window.SetWindowCaption(mRcpt.접수섹션_의뢰자_hWnd상호, tbOrder.CallCustName); // 전화번호
    //
    //        // 사업자번호
    //        #endregion
    //
    //        #region 3. 저장 버튼 클릭
    //        Debug.WriteLine($"[{AppName}] #region 3 시작: 저장 버튼 클릭");
    //        var resultSave = await SaveOrderAsync(ctrl);
    //        if (resultSave.Result != StdResult.Success)
    //            return CommonResult_AutoAllocProcess.FailureAndDiscard($"저장 실패: {resultSave.sErr}", "RegistOrderModeAsync_10");
    //        #endregion
    //
    //        #region 4. 저장 성공 확인
    //        await Task.Delay(c_nWaitLong, ctrl.Token);
    //
    //        // 4-1. 오더번호 OFR
    //        StdResult_String resultSeqno = await Get오더번호Async(0, ctrl);
    //        if (string.IsNullOrEmpty(resultSeqno.strResult))
    //        {
    //            return CommonResult_AutoAllocProcess.FailureAndDiscard($"오더번호 획득 실패: {resultSeqno.sErr}", "RegistOrderModeAsync_10");
    //        }
    //        Debug.WriteLine($"[{AppName}] 주문 등록 완료 - 오더번호: {resultSeqno.strResult}");
    //
    //        // 4-2. 사전 체크
    //        if (item.NewOrder.KeyCode <= 0)
    //        {
    //            Debug.WriteLine($"[{AppName}] KeyCode 없음 - Kai DB에 없는 주문");
    //            return CommonResult_AutoAllocProcess.FailureAndDiscard("Kai DB에 없는 주문입니다", "RegistOrderModeAsync_11");
    //        }
    //
    //        if (!string.IsNullOrEmpty(item.NewOrder.Onecall))
    //        {
    //            Debug.WriteLine($"[{AppName}] 이미 등록된 오더번호: {item.NewOrder.Onecall}");
    //            return CommonResult_AutoAllocProcess.SuccessAndDestroy(item);
    //        }
    //
    //        if (s_SrGClient == null || !s_SrGClient.m_bLoginSignalR)
    //        {
    //            Debug.WriteLine($"[{AppName}] SignalR 연결 안됨");
    //            return CommonResult_AutoAllocProcess.FailureAndDiscard("서버 연결이 끊어졌습니다", "RegistOrderModeAsync_12");
    //        }
    //
    //        // 4-3. Kai DB 업데이트
    //        item.NewOrder.Onecall = resultSeqno.strResult;
    //        //StdResult_Int resultUpdate = await s_SrGClient.SrResult_Order_UpdateRowAsync_Today_WithRequestId(item.NewOrder);
    //        StdResult_Int resultUpdate = new StdResult_Int(1);
    //
    //        //if (resultUpdate.nResult < 0 || !string.IsNullOrEmpty(resultUpdate.sErr))
    //        //{
    //        //    Debug.WriteLine($"[{AppName}] Kai DB 업데이트 실패: {resultUpdate.sErr}");
    //        //    return CommonResult_AutoAllocProcess.FailureAndDiscard($"Kai DB 업데이트 실패: {resultUpdate.sErr}", "RegistOrderModeAsync_13");
    //        //}
    //
    //        Debug.WriteLine($"[{AppName}] Kai DB 업데이트 성공 - Onecall: {resultSeqno.strResult}");
    //        #endregion
    //
    //        Debug.WriteLine($"[{AppName}] RegistOrderModeAsync 완료: KeyCode={item.KeyCode}");
    //        return CommonResult_AutoAllocProcess.SuccessAndReEnqueue(item, PostgService_Common_OrderState.NotChanged);
    //    }
    //    catch (Exception ex)
    //    {
    //        return CommonResult_AutoAllocProcess.FailureAndDiscard(StdUtil.GetExceptionMessage(ex), "RegistOrderModeAsync_999");
    //    }
    //}
    // #endregion

    #region 자동배차 - Kai변경 관련함수들
    // Kai DB에서 업데이트된 주문을 원콜 앱에 반영 (Existed_WithSeqno | Updated_Assume)
    //public async Task<CommonResult_AutoAllocProcess> CheckIsOrderAsync_AssumeKaiUpdated(AutoAllocModel item, CommonResult_AutoAllocDatagrid dgInfo, CancelTokenControl ctrl)
    //{
    //    string kaiState = item.NewOrder?.OrderState ?? "";
    //    string ocState = dgInfo.sStatus;
    //    TbOrder tbOrder = item.NewOrder;
    //    Debug.WriteLine($"[CheckIsOrderAsync_AssumeKaiUpdated] KeyCode={item.KeyCode}, Kai={kaiState}, Onecall={ocState}");

    //    await ctrl.WaitIfPausedOrCancelledAsync();

    //    switch (kaiState)
    //    {
    //        case "대기":
    //            case "취소": // 취소가 아니면 화물취소 시키고 비적재 - 테스트모드만 주석처리 끝나면 원복...
    //            if (ocState != "취소")
    //                return await UpdateOrderModeAsync(item, dgInfo.nIndex, "취소", null, ctrl);
    //            else return CommonResult_AutoAllocProcess.SuccessAndDestroy(item);

    //        //case "취소": // 테스트용으로 임시작업
    //        case "접수": // 같은 접수상태라도 kaiState의 공유상태에 따라 다르게 반응해야함
    //            if (ocState == "접수")
    //            {
    //                if(tbOrder.Share) return await UpdateOrderModeAsync(item, dgInfo.nIndex, null, item.NewOrder, ctrl);
    //                else return await UpdateOrderModeAsync(item, dgInfo.nIndex, "취소", null, ctrl);
    //            }
    //            else
    //            {
    //                MsgBox("화물24시가 접수이외의 상태이니, 연구가 필요합니다");
    //                break;

    //                //return CommonResult_AutoAllocProcess.SuccessAndDestroy(item);

    //                // For Test
    //                //return await UpdateOrderModeAsync(item, dgInfo.nIndex, null, item.NewOrder, ctrl);
    //            }

    //        default:
    //            System.Windows.MessageBox.Show($"[TODO] kaiState={kaiState}, Cg24State={ocState}", "CheckIsOrderAsync_AssumeKaiUpdated");
    //            break;
    //    }

    //    return CommonResult_AutoAllocProcess.SuccessAndReEnqueue();
    //}

    // 수정모드: 로우 셀렉트 → 상태변경/정보수정 → 저장
    // - targetState: 상태 변경 ("취소" 등, null이면 스킵)
    // - order: 정보 수정 (null이면 스킵)
    //public async Task<CommonResult_AutoAllocProcess> UpdateOrderModeAsync(
        //AutoAllocModel item, int nRowIndex, string targetState, TbOrder order, CancelTokenControl ctrl)
    //{
    //    try
    //    {
    //        #region 1. 사전작업
    //        Debug.WriteLine($"[{AppName}] UpdateOrderModeAsync 진입: KeyCode={item.KeyCode}, RowIndex={nRowIndex}, targetState={targetState}, order={(order != null ? "있음" : "없음")}");

    //        // 파라미터 검증
    //        if (string.IsNullOrEmpty(targetState) && order == null)
    //            return CommonResult_AutoAllocProcess.FailureAndDiscard("targetState와 order 둘 다 null입니다.", "UpdateOrderModeAsync_00");

    //        // 해당 로우 클릭 (클릭만으로 수정모드 진입)
    //        bool bSelected = await ClickDatagridRowAsync(nRowIndex);
    //        Debug.WriteLine($"[{AppName}] 로우 클릭 완료: nRowIndex={nRowIndex}, 선택됨={bSelected}");
    //        if (!bSelected)
    //            return CommonResult_AutoAllocProcess.FailureAndRetry("로우 선택 실패", "UpdateOrderModeAsync_01");

    //        // 오더번호 검증 (선택한 로우가 올바른지 확인)
    //        string expectedSeqno = item.NewOrder.Onecall;
    //        var resultSeqno = await Get오더번호Async(nRowIndex, ctrl);
    //        if (!string.IsNullOrEmpty(resultSeqno.sErr))
    //            return CommonResult_AutoAllocProcess.FailureAndRetry($"오더번호 읽기 실패: {resultSeqno.sErr}", "UpdateOrderModeAsync_02");
    //        if (resultSeqno.strResult != expectedSeqno)
    //            return CommonResult_AutoAllocProcess.FailureAndRetry($"오더번호 불일치: 예상={expectedSeqno}, 실제={resultSeqno.strResult}", "UpdateOrderModeAsync_02B");
    //        Debug.WriteLine($"[{AppName}] 오더번호 검증 성공: {resultSeqno.strResult}");
    //        #endregion

    //        #region 2. 상태 변경
    //        if (!string.IsNullOrEmpty(targetState))
    //        {
    //            if (targetState == "취소")
    //            {
    //                bool bCancelled = await Click버튼WaitDisableAsync(mRcpt.접수섹션_hWnd화물취소버튼, "화물취소", ctrl);
    //                if (!bCancelled)
    //                    return CommonResult_AutoAllocProcess.FailureAndRetry("화물취소 실패", "UpdateOrderModeAsync_03");
    //                return CommonResult_AutoAllocProcess.SuccessAndDestroy(item);
    //            }
    //            else if (targetState == "접수")
    //            {
    //                bool bReRegistered = await Click버튼WaitDisableAsync(mRcpt.접수섹션_hWnd재접수버튼, "재접수", ctrl);
    //                if (!bReRegistered)
    //                    return CommonResult_AutoAllocProcess.FailureAndRetry("재접수 실패", "UpdateOrderModeAsync_04");
    //                return CommonResult_AutoAllocProcess.SuccessAndReEnqueue(item, PostgService_Common_OrderState.NotChanged);
    //            }
    //        }
    //        #endregion

    //        #region 3. 정보 수정
    //        if (order != null)
    //        {
    //            int changeCount = 0;

    //            // 상차지 입력
    //            string current상차 = Std32Window.GetWindowCaption(mRcpt.접수섹션_hWnd상차지주소) ?? "";
    //            if (current상차 != order.StartDetailAddr)
    //            {
    //                var result상차 = await Set상세주소Async(mRcpt.접수섹션_hWnd상차지주소, fInfo.접수등록Page_접수_상차지권역_rcChkRelM, order.StartDetailAddr, ctrl);
    //                if (result상차.Result != StdResult.Success)
    //                    return CommonResult_AutoAllocProcess.FailureAndRetry($"상차지 입력실패: {result상차.sErr}", "UpdateOrderModeAsync_10");
    //                changeCount++;
    //                Debug.WriteLine($"[{AppName}] 상차지 수정: {current상차} → {order.StartDetailAddr}");
    //            }

    //            // 하차지 입력
    //            string current하차 = Std32Window.GetWindowCaption(mRcpt.접수섹션_hWnd하차지주소) ?? "";
    //            if (current하차 != order.DestDetailAddr)
    //            {
    //                var result하차 = await Set상세주소Async(mRcpt.접수섹션_hWnd하차지주소, fInfo.접수등록Page_접수_하차지권역_rcChkRelM, order.DestDetailAddr, ctrl);
    //                if (result하차.Result != StdResult.Success)
    //                    return CommonResult_AutoAllocProcess.FailureAndRetry($"하차지 입력실패: {result하차.sErr}", "UpdateOrderModeAsync_11");
    //                changeCount++;
    //                Debug.WriteLine($"[{AppName}] 하차지 수정: {current하차} → {order.DestDetailAddr}");
    //            }

    //            // 화물정보
    //            string db화물정보 = string.IsNullOrEmpty(order.OrderRemarks) ? "없음" : order.OrderRemarks;
    //            var (changed화물, result화물) = await UpdateEditIfChangedAsync(mRcpt.접수섹션_hWnd화물정보, db화물정보, "화물정보", ctrl);
    //            if (result화물.Result != StdResult.Success)
    //                return CommonResult_AutoAllocProcess.FailureAndRetry($"화물정보 입력실패: {result화물.sErr}", "UpdateOrderModeAsync_12");
    //            if (changed화물) changeCount++;

    //            // 운임 - 총운임
    //            string current총운임 = Std32Window.GetWindowCaption(mRcpt.접수섹션_hWnd총운임) ?? "";
    //            string db총운임 = order.FeeTotal > 0 ? order.FeeTotal.ToString() : "";
    //            {
    //                //bool bTmp = await Simulation_Keyboard.PostFeeWithVerifyAsync(mRcpt.접수섹션_hWnd총운임, order.FeeTotal);
    //                bool bTmp = true;
    //                if (!bTmp)
    //                    return CommonResult_AutoAllocProcess.FailureAndRetry($"총운임 입력실패: {order.FeeTotal}", "UpdateOrderModeAsync_13");
    //                changeCount++;
    //                Debug.WriteLine($"[{AppName}] 총운임 수정: {current총운임} → {db총운임}");
    //            }

    //            // 운임 - 수수료
    //            string current수수료 = Std32Window.GetWindowCaption(mRcpt.접수섹션_hWnd수수료) ?? "";
    //            string db수수료 = order.FeeCharge > 0 ? order.FeeCharge.ToString() : "";
    //            {
    //                //bool bTmp = await Simulation_Keyboard.PostFeeWithVerifyAsync(mRcpt.접수섹션_hWnd수수료, order.FeeCharge);
    //                bool bTmp = true;
    //                if (!bTmp)
    //                    return CommonResult_AutoAllocProcess.FailureAndRetry($"수수료 입력실패: {order.FeeCharge}", "UpdateOrderModeAsync_14");
    //                changeCount++;
    //                Debug.WriteLine($"[{AppName}] 수수료 수정: {current수수료} → {db수수료}");
    //            }

    //            // 차량 - 톤수
    //            CModel_ComboBox model톤수 = GetCarWeightResult(order.CarType, order.CarWeight);
    //            var (changed톤수, result톤수) = await UpdateComboIfChangedAsync(
    //                mRcpt.접수섹션_차량_hWnd톤수, model톤수, mRcpt.접수섹션_hWndTop, fInfo.접수등록Page_접수_톤수_rcChkRelM, "톤수", ctrl);
    //            if (result톤수.Result != StdResult.Success)
    //                return CommonResult_AutoAllocProcess.FailureAndRetry($"톤수 선택실패: {result톤수.sErr}", "UpdateOrderModeAsync_15");
    //            if (changed톤수) changeCount++;

    //            // 차량 - 차종
    //            CModel_ComboBox model차종 = GetTruckDetailResult(order.CarType, order.TruckDetail);
    //            var (changed차종, result차종) = await UpdateComboIfChangedAsync(
    //                mRcpt.접수섹션_차량_hWnd차종, model차종, mRcpt.접수섹션_hWndTop, fInfo.접수등록Page_접수_차종_rcChkRelM, "차종", ctrl);
    //            if (result차종.Result != StdResult.Success)
    //                return CommonResult_AutoAllocProcess.FailureAndRetry($"차종 선택실패: {result차종.sErr}", "UpdateOrderModeAsync_16");
    //            if (changed차종) changeCount++;

    //            // 차량 - 결재
    //            CModel_ComboBox model결재 = GetFeeTypeResult(order.FeeType);
    //            var (changed결재, result결재) = await UpdateComboIfChangedAsync(
    //                mRcpt.접수섹션_차량_hWnd결재, model결재, mRcpt.접수섹션_hWndTop, fInfo.접수등록Page_접수_결재_rcChkRelM, "결재", ctrl);
    //            if (result결재.Result != StdResult.Success)
    //                return CommonResult_AutoAllocProcess.FailureAndRetry($"결재 선택실패: {result결재.sErr}", "UpdateOrderModeAsync_17");
    //            if (changed결재) changeCount++;

    //            // 화물중량
    //            string maxWeight = GetMaxCarWeight(model톤수);
    //            string curWeight = Std32Window.GetWindowCaption(mRcpt.접수섹션_hWnd화물중량) ?? "";
    //            if (maxWeight != curWeight)
    //            {
    //                var result화물중량 = await Set화물중량Async(maxWeight, ctrl);
    //                if (result화물중량.Result != StdResult.Success)
    //                    return CommonResult_AutoAllocProcess.FailureAndRetry($"화물중량 설정실패: {result화물중량.sErr}", "UpdateOrderModeAsync_17_1");
    //                changeCount++;
    //            }

    //            // 구분 (왕복/경유/긴급)
    //            bool db왕복 = order.DeliverType == "왕복";
    //            bool db경유 = order.DeliverType == "경유";
    //            bool db긴급 = order.DeliverType == "긴급";

    //            var (changed왕복, result왕복) = await UpdateCheckBoxIfChangedAsync(
    //                mRcpt.접수섹션_구분_hWnd왕복, fInfo.접수등록Page_구분_왕복Part_rcChkRelM, db왕복, "왕복", ctrl);
    //            if (result왕복.Result != StdResult.Success)
    //                return CommonResult_AutoAllocProcess.FailureAndRetry($"왕복 설정실패: {result왕복.sErr}", "UpdateOrderModeAsync_18");
    //            if (changed왕복) changeCount++;

    //            var (changed경유, result경유) = await UpdateCheckBoxIfChangedAsync(
    //                mRcpt.접수섹션_구분_hWnd경유, fInfo.접수등록Page_구분_경유Part_rcChkRelM, db경유, "경유", ctrl);
    //            if (result경유.Result != StdResult.Success)
    //                return CommonResult_AutoAllocProcess.FailureAndRetry($"경유 설정실패: {result경유.sErr}", "UpdateOrderModeAsync_19");
    //            if (changed경유) changeCount++;

    //            var (changed긴급, result긴급) = await UpdateCheckBoxIfChangedAsync(
    //                mRcpt.접수섹션_구분_hWnd긴급, fInfo.접수등록Page_구분_긴급Part_rcChkRelM, db긴급, "긴급", ctrl);
    //            if (result긴급.Result != StdResult.Success)
    //                return CommonResult_AutoAllocProcess.FailureAndRetry($"긴급 설정실패: {result긴급.sErr}", "UpdateOrderModeAsync_20");
    //            if (changed긴급) changeCount++;

    //            // 상차방법 (수작업 고정)
    //            var (changed상차방법, result상차방법) = await UpdateCheckBoxIfChangedAsync(
    //                mRcpt.접수섹션_상차방법_hWnd수작업, fInfo.접수등록Page_상차방법_수작업Part_rcChkRelM, true, "수작업", ctrl);
    //            if (result상차방법.Result != StdResult.Success)
    //                return CommonResult_AutoAllocProcess.FailureAndRetry($"상차방법 설정실패: {result상차방법.sErr}", "UpdateOrderModeAsync_21");
    //            if (changed상차방법) changeCount++;

    //            // 상차일시 (당상 고정)
    //            var (changed상차일시, result상차일시) = await UpdateCheckBoxIfChangedAsync(
    //                mRcpt.접수섹션_상차일시_hWnd당상, fInfo.접수등록Page_상차일시_당상Part_rcChkRelM, true, "당상", ctrl);
    //            if (result상차일시.Result != StdResult.Success)
    //                return CommonResult_AutoAllocProcess.FailureAndRetry($"상차일시 설정실패: {result상차일시.sErr}", "UpdateOrderModeAsync_22");
    //            if (changed상차일시) changeCount++;

    //            // 하차방법 (수작업 고정)
    //            var (changed하차방법, result하차방법) = await UpdateCheckBoxIfChangedAsync(
    //                mRcpt.접수섹션_하차방법_hWnd수작업, fInfo.접수등록Page_하차방법_수작업Part_rcChkRelM, true, "수작업", ctrl);
    //            if (result하차방법.Result != StdResult.Success)
    //                return CommonResult_AutoAllocProcess.FailureAndRetry($"하차방법 설정실패: {result하차방법.sErr}", "UpdateOrderModeAsync_23");
    //            if (changed하차방법) changeCount++;

    //            // 하차일시 (당착 고정)
    //            var (changed하차일시, result하차일시) = await UpdateCheckBoxIfChangedAsync(
    //                mRcpt.접수섹션_하차일시_hWnd당착, fInfo.접수등록Page_하차일시_당착Part_rcChkRelM, true, "당착", ctrl);
    //            if (result하차일시.Result != StdResult.Success)
    //                return CommonResult_AutoAllocProcess.FailureAndRetry($"하차일시 설정실패: {result하차일시.sErr}", "UpdateOrderModeAsync_24");
    //            if (changed하차일시) changeCount++;

    //            // 화물메모
    //            string db화물메모 = order.OrderMemo ?? "";
    //            var (changed화물메모, result화물메모) = await UpdateEditIfChangedAsync(
    //                mRcpt.접수섹션_hWnd화물메모, db화물메모, "화물메모", ctrl);
    //            if (result화물메모.Result != StdResult.Success)
    //                return CommonResult_AutoAllocProcess.FailureAndRetry($"화물메모 입력실패: {result화물메모.sErr}", "UpdateOrderModeAsync_25");
    //            if (changed화물메모) changeCount++;

    //            // 의뢰자 - 전화
    //            string db의뢰자전화 = StdConvert.ToPhoneNumberFormat(order.CallTelNo);
    //            var (changed의뢰자전화, result의뢰자전화) = await UpdateEditIfChangedAsync(
    //                mRcpt.접수섹션_의뢰자_hWnd전화, db의뢰자전화, "의뢰자전화", ctrl);
    //            if (result의뢰자전화.Result != StdResult.Success)
    //                return CommonResult_AutoAllocProcess.FailureAndRetry($"의뢰자전화 입력실패: {result의뢰자전화.sErr}", "UpdateOrderModeAsync_26");
    //            if (changed의뢰자전화) changeCount++;

    //            // 의뢰자 - 상호
    //            string db의뢰자상호 = order.CallCustName ?? "";
    //            var (changed의뢰자상호, result의뢰자상호) = await UpdateEditIfChangedAsync(
    //                mRcpt.접수섹션_의뢰자_hWnd상호, db의뢰자상호, "의뢰자상호", ctrl);
    //            if (result의뢰자상호.Result != StdResult.Success)
    //                return CommonResult_AutoAllocProcess.FailureAndRetry($"의뢰자상호 입력실패: {result의뢰자상호.sErr}", "UpdateOrderModeAsync_27");
    //            if (changed의뢰자상호) changeCount++;

    //            Debug.WriteLine($"[{AppName}] 정보 수정 완료: {changeCount}개 필드 변경");
    //        }
    //        #endregion

    //        #region 4. 저장 및 확인
    //        var resultSave = await SaveOrderAsync(ctrl);
    //        if (resultSave.Result != StdResult.Success)
    //            return CommonResult_AutoAllocProcess.FailureAndRetry($"저장 실패: {resultSave.sErr}", "UpdateOrderModeAsync_29");

    //        Debug.WriteLine($"[{AppName}] UpdateOrderModeAsync 완료: KeyCode={item.KeyCode}");
    //        #endregion

    //        return CommonResult_AutoAllocProcess.SuccessAndReEnqueue(item, PostgService_Common_OrderState.NotChanged);
    //    }
    //    catch (Exception ex)
    //    {
    //        return CommonResult_AutoAllocProcess.FailureAndDiscard(StdUtil.GetExceptionMessage(ex), "UpdateOrderModeAsync_999");
    //    }
    //}
    #endregion

    #region 자동배차 - Insung상태관리 관련함수들
    // 원콜 주문 상태 관리 및 모니터링 (NotChanged 상황 처리)
    //public async Task<CommonResult_AutoAllocProcess> CheckIsOrderAsync_OnecallOrderManage(AutoAllocModel item, CommonResult_AutoAllocDatagrid dgInfo, CancelTokenControl ctrl)
    //{
    //    await ctrl.WaitIfPausedOrCancelledAsync();
    //
    //    string kaiState = item.NewOrder?.OrderState ?? "";
    //    string ocState = dgInfo.sStatus;
    //
    //    Debug.WriteLine($"[CheckIsOrderAsync_OnecallOrderManage] KeyCode={item.KeyCode}, Kai={kaiState}, Onecall={ocState}");
    //
    //    // TODO: 실제 처리 구현
    //    return CommonResult_AutoAllocProcess.SuccessAndReEnqueue();
    //}
    #endregion
}

#nullable restore
