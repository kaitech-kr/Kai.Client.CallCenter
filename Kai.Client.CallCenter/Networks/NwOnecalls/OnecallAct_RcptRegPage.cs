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

/// <summary>
/// 원콜 접수등록 페이지 제어
/// </summary>
public partial class OnecallAct_RcptRegPage
{
    #region Datagrid Column Header Info
    /// <summary>
    /// Datagrid 컬럼 헤더 정보 배열 (21개)
    /// </summary>
    public readonly NwCommon_DgColumnHeader[] m_ReceiptDgHeaderInfos = new NwCommon_DgColumnHeader[]
    {
        new NwCommon_DgColumnHeader() { sName = "순번", bOfrSeq = true, nWidth = 50 },
        new NwCommon_DgColumnHeader() { sName = "처리상태", bOfrSeq = false, nWidth = 65 },
        new NwCommon_DgColumnHeader() { sName = "오더번호", bOfrSeq = true, nWidth = 100 },
        new NwCommon_DgColumnHeader() { sName = "처리일자", bOfrSeq = true, nWidth = 105 },
        new NwCommon_DgColumnHeader() { sName = "처리시간", bOfrSeq = true, nWidth = 85 },
        new NwCommon_DgColumnHeader() { sName = "상차지", bOfrSeq = false, nWidth = 125 },
        new NwCommon_DgColumnHeader() { sName = "하차지", bOfrSeq = false, nWidth = 125 },
        new NwCommon_DgColumnHeader() { sName = "결제방법", bOfrSeq = false, nWidth = 65 },
        new NwCommon_DgColumnHeader() { sName = "운임", bOfrSeq = true, nWidth = 80 },
        new NwCommon_DgColumnHeader() { sName = "수수료", bOfrSeq = true, nWidth = 75 },
        new NwCommon_DgColumnHeader() { sName = "차종", bOfrSeq = false, nWidth = 70 },
        new NwCommon_DgColumnHeader() { sName = "톤수", bOfrSeq = false, nWidth = 50 },
        new NwCommon_DgColumnHeader() { sName = "혼적", bOfrSeq = false, nWidth = 60 },
        new NwCommon_DgColumnHeader() { sName = "차주명", bOfrSeq = false, nWidth = 80 },
        new NwCommon_DgColumnHeader() { sName = "차주전화", bOfrSeq = true, nWidth = 135 },
        new NwCommon_DgColumnHeader() { sName = "담당자번호", bOfrSeq = true, nWidth = 135 },
        new NwCommon_DgColumnHeader() { sName = "적재옵션", bOfrSeq = false, nWidth = 130 },
        new NwCommon_DgColumnHeader() { sName = "화물정보", bOfrSeq = false, nWidth = 155 },
        new NwCommon_DgColumnHeader() { sName = "인수증", bOfrSeq = false, nWidth = 60 },
        new NwCommon_DgColumnHeader() { sName = "상차일", bOfrSeq = true, nWidth = 70 },
        new NwCommon_DgColumnHeader() { sName = "하차일", bOfrSeq = true, nWidth = 70 },
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

    /// <summary>
    /// 마지막으로 읽은 총계 (조회 딜레이 계산용)
    /// </summary>
    public int m_nLastTotalCount { get; set; } = 0;
    #endregion

    #region 생성자
    public OnecallAct_RcptRegPage(OnecallContext context)
    {
        m_Context = context;
    }
    #endregion

    #region InitializeAsync
    /// <summary>
    /// 접수등록 페이지 초기화
    /// </summary>
    public async Task<StdResult_Error> InitializeAsync()
    {
        try
        {
            #region 1. 접수등록Page 윈도우 찾기 (10초)
            for (int i = 0; i < c_nRepeatVeryMany; i++)
            {
                mRcpt.TopWnd_hWnd = Std32Window.FindWindowEx(mMain.WndInfo_MdiClient.hWnd, IntPtr.Zero, null, fInfo.접수등록Page_TopWnd_sWndName);
                if (mRcpt.TopWnd_hWnd != IntPtr.Zero) break;
                await Task.Delay(c_nWaitNormal);
            }

            if (mRcpt.TopWnd_hWnd == IntPtr.Zero)
                return new StdResult_Error($"[{AppName}] 접수등록Page 찾기실패: {fInfo.접수등록Page_TopWnd_sWndName}", "OnecallAct_RcptRegPage/InitializeAsync_01");
            #endregion

            #region 2. 검색섹션을 찾을때까지 기다린후 자식정보 찾기
            // 검색섹션 - Top정보
            bool bFind = false;
            for (int i = 0; i < c_nRepeatVeryMany; i++)
            {
                await Task.Delay(c_nWaitNormal);
                mRcpt.검색섹션_hWndTop = Std32Window.GetWndHandle_FromRelDrawPt(mMain.TopWnd_hWnd, fInfo.접수등록Page_검색섹션_ptChkRelT);
                if ("검색" == Std32Window.GetWindowCaption(mRcpt.검색섹션_hWndTop))
                {
                    bFind = true;
                    await Task.Delay(c_nWaitNormal);
                    break;
                }
            }
            Debug.WriteLine($"[{AppName}] 검색섹션_hWnd 찾음: {mRcpt.검색섹션_hWndTop:X}");
            if (!bFind) return new StdResult_Error($"[{AppName}] 검색섹션_hWnd 찾기실패", "OnecallAct_RcptRegPage/InitializeAsync_02");

            // 검색섹션 - 자식정보           
            mRcpt.검색섹션_hWnd포커스탈출 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.검색섹션_hWndTop, fInfo.접수등록Page_검색_포커Kill_ptChkRelM); // 포커스탈출

            mRcpt.검색섹션_hWnd자동조회 = 
                Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.검색섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_검색_자동조회_rcChkRelM)); // 자동조회

            mRcpt.검색섹션_hWnd새로고침버튼 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.검색섹션_hWndTop, fInfo.접수등록Page_검색_새로고침Btn_ptChkRelM);
            mRcpt.검색섹션_hWnd확장버튼 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.검색섹션_hWndTop, fInfo.접수등록Page_검색ExpandBtn_ptChkRelM);
            #endregion

            #region 3. 접수섹션
            // 3. 접수섹션 Top핸들찾기
            mRcpt.접수섹션_hWndTop = Std32Window.GetWndHandle_FromRelDrawPt(mMain.TopWnd_hWnd, fInfo.접수등록Page_접수섹션_ptChkRelT);

            // 버튼들
            mRcpt.접수섹션_hWnd신규버튼 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, fInfo.접수등록Page_접수_신규Btn_ptChkRelM);
            mRcpt.접수섹션_hWnd저장버튼 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, fInfo.접수등록Page_접수_저장Btn_ptChkRelM);
            mRcpt.접수섹션_hWnd취소버튼 = StdWin32.FindWindowEx(mRcpt.접수섹션_hWndTop, IntPtr.Zero, null, "화물취소"); // 검증용으로 위치가 필요할까?
            mRcpt.접수섹션_hWnd복사버튼 = StdWin32.FindWindowEx(mRcpt.접수섹션_hWndTop, IntPtr.Zero, null, "화물복사"); // 검증용으로 위치가 필요할까?

            // 상차지
            mRcpt.접수섹션_hWnd상차지권역 = Std32Window.GetWndHandle_FromRelDrawPt(
                mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_접수_상차지권역_rcChkRelM));
            mRcpt.접수섹션_hWnd상차지주소 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, fInfo.접수등록Page_접수_상차지주소_ptChkRelM);

            // 하차지
            mRcpt.접수섹션_hWnd하차지권역 = Std32Window.GetWndHandle_FromRelDrawPt(
                mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_접수_하차지권역_rcChkRelM));
            mRcpt.접수섹션_hWnd하차지주소 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, fInfo.접수등록Page_접수_하차지주소_ptChkRelM);

            // 화물정보
            mRcpt.접수섹션_hWnd화물정보 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, fInfo.접수등록Page_접수_화물정보_ptChkRelM);

            // 운임
            mRcpt.접수섹션_hWnd총운임 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, fInfo.접수등록Page_접수_총운임_ptChkRelM);
            mRcpt.접수섹션_hWnd수수료 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, fInfo.접수등록Page_접수_수수료_ptChkRelM);

            // 차량정보
            mRcpt.접수섹션_차량_hWnd톤수 = Std32Window.GetWndHandle_FromRelDrawPt(
                mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_접수_톤수_rcChkRelM)); // 차량톤수

            mRcpt.접수섹션_차량_hWnd차종 = Std32Window.GetWndHandle_FromRelDrawPt(
                mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_접수_차종_rcChkRelM)); // 차종

            mRcpt.접수섹션_차량_hWnd대수 = Std32Window.GetWndHandle_FromRelDrawPt(
                mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_접수_대수_rcChkRelM)); // 차량대수

            mRcpt.접수섹션_차량_hWnd결재 = Std32Window.GetWndHandle_FromRelDrawPt(
                mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_접수_결재_rcChkRelM)); // 결재

            // 화물중량
            mRcpt.접수섹션_hWnd화물중량 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, fInfo.접수등록Page_접수_화물중량_ptChkRelM);

            // 구분
            mRcpt.접수섹션_구분_hWnd독차 = Std32Window.GetWndHandle_FromRelDrawPt(
                mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_구분_독차Part_rcChkRelM));

            mRcpt.접수섹션_구분_hWnd혼적 = Std32Window.GetWndHandle_FromRelDrawPt(
                mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_구분_혼적Part_rcChkRelM));

            mRcpt.접수섹션_구분_hWnd긴급 = Std32Window.GetWndHandle_FromRelDrawPt(
                mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_구분_긴급Part_rcChkRelM));

            mRcpt.접수섹션_구분_hWnd왕복 = Std32Window.GetWndHandle_FromRelDrawPt(
                mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_구분_왕복Part_rcChkRelM));

            mRcpt.접수섹션_구분_hWnd경유 = Std32Window.GetWndHandle_FromRelDrawPt(
                mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_구분_경유Part_rcChkRelM));

            // 상차방법
            mRcpt.접수섹션_상차방법_hWnd지게차 = Std32Window.GetWndHandle_FromRelDrawPt(
                mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_상차방법_지게차Part_rcChkRelM));

            mRcpt.접수섹션_상차방법_hWn호이스트 = Std32Window.GetWndHandle_FromRelDrawPt(
                mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_상차방법_호이스트Part_rcChkRelM));

            mRcpt.접수섹션_상차방법_hWnd수해줌 = Std32Window.GetWndHandle_FromRelDrawPt(
                mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_상차방법_수해줌Part_rcChkRelM));

            mRcpt.접수섹션_상차방법_hWnd수작업 = Std32Window.GetWndHandle_FromRelDrawPt(
                mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_상차방법_수작업Part_rcChkRelM));

            mRcpt.접수섹션_상차방법_hWnd크레인 = Std32Window.GetWndHandle_FromRelDrawPt(
                mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_상차방법_크레인Part_rcChkRelM));

            // 상차일시
            mRcpt.접수섹션_상차일시_hWnd당상 = Std32Window.GetWndHandle_FromRelDrawPt(
               mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_상차일시_당상Part_rcChkRelM));

            mRcpt.접수섹션_상차일시_hWnd낼상 = Std32Window.GetWndHandle_FromRelDrawPt(
               mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_상차일시_낼상Part_rcChkRelM));

            mRcpt.접수섹션_상차일시_hWnd월상 = Std32Window.GetWndHandle_FromRelDrawPt(
               mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_상차일시_월상Part_rcChkRelM));

            // 하차방법
            mRcpt.접수섹션_하차방법_hWnd지게차 = Std32Window.GetWndHandle_FromRelDrawPt(
               mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_하차방법_지게차Part_rcChkRelM));

            mRcpt.접수섹션_하차방법_hWn호이스트 = Std32Window.GetWndHandle_FromRelDrawPt(
               mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_하차방법_호이스트Part_rcChkRelM));

            mRcpt.접수섹션_하차방법_hWnd수해줌 = Std32Window.GetWndHandle_FromRelDrawPt(
               mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_하차방법_수해줌Part_rcChkRelM));

            mRcpt.접수섹션_하차방법_hWnd수작업 = Std32Window.GetWndHandle_FromRelDrawPt(
               mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_하차방법_수작업Part_rcChkRelM));

            mRcpt.접수섹션_하차방법_hWnd크레인 = Std32Window.GetWndHandle_FromRelDrawPt(
               mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_하차방법_크레인Part_rcChkRelM));

            // 하차일시
            mRcpt.접수섹션_하차일시_hWnd당착 = Std32Window.GetWndHandle_FromRelDrawPt(
                mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_하차일시_당착Part_rcChkRelM));

            mRcpt.접수섹션_하차일시_hWnd낼착 = Std32Window.GetWndHandle_FromRelDrawPt(
                mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_하차일시_낼착Part_rcChkRelM));

            mRcpt.접수섹션_하차일시_hWnd월착 = Std32Window.GetWndHandle_FromRelDrawPt(
                mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_하차일시_월착Part_rcChkRelM));

            mRcpt.접수섹션_하차일시_hWnd당_내착 = Std32Window.GetWndHandle_FromRelDrawPt(
                mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_하차일시_당_내착Part_rcChkRelM));

            // 화물메모
            mRcpt.접수섹션_hWnd화물메모 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, fInfo.접수등록Page_접수_화물메모_ptChkRelM);

            // 의뢰자
            mRcpt.접수섹션_의뢰자_hWnd상호 = Std32Window.GetWndHandle_FromRelDrawPt( // 상호
                mRcpt.접수섹션_hWndTop, StdUtil.GetCenterDrawPoint(fInfo.접수등록Page_의뢰자_상호_rcChkRelM));

            mRcpt.접수섹션_의뢰자_hWnd전화 = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, fInfo.접수등록Page_의뢰자_전화번호_ptChkRelM); // 전화
            #endregion

            #region 4. DG오더 섹션
            // DG오더 찾기
            mRcpt.DG오더_hWndTop = Std32Window.GetWndHandle_FromRelDrawPt(mMain.TopWnd_hWnd, fInfo.접수등록Page_DG오더_ptChkRelT);

            // Background Brightness 계산 (데이터 로드 전에 측정, +10 마진 적용 - 이보다 밝으면 데이터)
            int nBkBright = OfrService.GetCenterPixelBrightnessFrmWndHandle(mRcpt.DG오더_hWndTop);
            mRcpt.DG오더_nBkMarginedBright = nBkBright + 10;
            Debug.WriteLine($"[{AppName}] Background Brightness: {nBkBright}, Margined: {mRcpt.DG오더_nBkMarginedBright}");

            // 확장 전 컬럼 검증/초기화
            var (listLW, error) = await SetDG오더ColumnHeaderAsync();
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
            Debug.WriteLine($"[{AppName}] Small Rects 생성 완료: {columns}열 x {smallRowCount}행");

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
            Debug.WriteLine($"[{AppName}] Large Rects 생성 완료: {columns}열 x {largeRowCount}행");
            #endregion

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
        catch (Exception ex)
        {
            return new StdResult_Error($"[{AppName}] RcptRegPage 예외: {ex.Message}", "OnecallAct_RcptRegPage/InitializeAsync_99");
        }
    }
    
    // SetDG오더 섹션
    private async Task<(List<OfrModel_LeftWidth> listLW, StdResult_Error error)> SetDG오더ColumnHeaderAsync(bool bEdit = true)
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

                    StdResult_Error initResult = await InitDG오더Async(CEnum_DgValidationIssue.InvalidColumnCount);

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
                    var result = await OfrWork_Common.OfrStr_ComplexCharSetAsync(bmpDG, rcTmp, bInvertRgb: false, bTextSave: true, dWeight: 0.9, bEdit: bEdit);
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

                    StdResult_Error initResult = await InitDG오더Async(validationIssues);

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
        catch (Exception ex)
        {
            return (null, new StdResult_Error($"[{AppName}] SetDG오더ColumnHeaderAsync 예외: {ex.Message}", "SetDG오더ColumnHeaderAsync_99"));
        }
        finally
        {
            bmpDG?.Dispose();
        }
    }

    /// <summary>
    /// Datagrid 강제 초기화 (항목초기화 → 컬럼 삭제 → 순서 조정 → 폭 조정)
    /// </summary>
    private async Task<StdResult_Error> InitDG오더Async(CEnum_DgValidationIssue issues)
    {
        // 마우스 커서 위치 백업 (작업 완료 후 복원용)
        Draw.Point ptCursorBackup = Std32Cursor.GetCursorPos_AbsDrawPt();

        try
        {
            Debug.WriteLine($"[{AppName}] InitDG오더Async 시작: issues={issues}");

            // 상수 정의
            const int headerGab = 6;
            int headerHeight = fInfo.접수등록Page_DG오더_headerHeight;
            int textHeight = headerHeight - (headerGab * 2);
            int targetRow = headerGab + textHeight;

            // DG오더_hWnd 기준으로 헤더 영역 정의
            Draw.Rectangle rcDG = Std32Window.GetWindowRect_DrawAbs(mRcpt.DG오더_hWndTop);
            Draw.Rectangle rcHeader = new Draw.Rectangle(0, 0, rcDG.Width, headerHeight);

            #region Step 1 - 목록초기화
            Debug.WriteLine($"[{AppName}] Step 1: 목록초기화 시작");
            await Std32Mouse_Post.MousePostAsync_ClickLeft(
                Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.접수섹션_hWndTop, fInfo.접수등록Page_접수_목록초기화Btn_ptRelS));
            await Task.Delay(c_nWaitVeryLong);
            Debug.WriteLine($"[{AppName}] Step 1 완료");
            #endregion

            #region Step 2 - 컬럼 폭 조정 및 불필요한 컬럼 삭제
            Debug.WriteLine($"[{AppName}] Step 2: 컬럼 폭 조정 및 삭제 시작");

            int center = headerGab + (textHeight / 2);

            for (int iteration = 0; iteration < 5; iteration++)
            {
                await Task.Delay(c_nWaitNormal);

                // 2-1. 헤더 캡처 및 컬럼 경계 검출
                var (bmpHeader, listLW, columns) = CaptureAndDetectColumnBoundaries(rcHeader, targetRow);
                if (bmpHeader == null)
                {
                    return new StdResult_Error($"[{AppName}] Step 2 헤더 캡처 실패 (반복 {iteration + 1})",
                        "OnecallAct_RcptRegPage/InitDG오더Async_01");
                }

                Debug.WriteLine($"[{AppName}] 반복 {iteration + 1}: 컬럼 {columns}개 검출 (목표: {m_ReceiptDgHeaderInfos.Length}개)");

                // 2-2. 각 컬럼 텍스트 인식
                string[] texts = await OfrAllColumnsAsync(bmpHeader, listLW, columns, headerGab, textHeight, bEdit: true);
                Draw.Rectangle[] rcHeaders = new Draw.Rectangle[columns];
                for (int x = 0; x < columns; x++)
                {
                    rcHeaders[x] = new Draw.Rectangle(listLW[x].nLeft, headerGab, listLW[x].nWidth, textHeight);
                }

                // 2-3. 불필요한 컬럼 삭제 (우측에서 좌측으로)
                int removedCount = 0;
                for (int x = columns - 1; x >= 0; x--)
                {
                    bool isNeeded = Array.Exists(m_ReceiptDgHeaderInfos, h => h.sName == texts[x]);
                    if (!isNeeded)
                    {
                        removedCount++;
                        Debug.WriteLine($"[{AppName}] 불필요 컬럼 발견: [{x}]'{texts[x]}' → 제거");

                        // 수직 드래그로 제거 (위로 드래그)
                        Draw.Point ptCenter = StdUtil.GetCenterDrawPoint(rcHeaders[x]);
                        await Simulation_Mouse.SafeMouseEvent_DragLeft_Smooth_VerticalAsync(
                            mRcpt.DG오더_hWndTop, ptCenter, -50, false);

                        await Task.Delay(c_nWaitShort);
                    }
                }

                if (removedCount > 0)
                {
                    Debug.WriteLine($"[{AppName}] {removedCount}개 불필요 컬럼 제거 완료");
                    await Task.Delay(c_nWaitLong);

                    // 컬럼 제거 후 다시 캡처
                    bmpHeader.Dispose();
                    var result = CaptureAndDetectColumnBoundaries(rcHeader, targetRow);
                    bmpHeader = result.bmpHeader;
                    listLW = result.listLW;
                    columns = result.columns;

                    if (bmpHeader == null)
                    {
                        return new StdResult_Error($"[{AppName}] 컬럼 제거 후 재캡처 실패",
                            "OnecallAct_RcptRegPage/InitDG오더Async_01b");
                    }

                    texts = await OfrAllColumnsAsync(bmpHeader, listLW, columns, headerGab, textHeight, bEdit: true);
                    Debug.WriteLine($"[{AppName}] 재캡처 후 컬럼 수: {columns}");
                }

                // 2-4. 폭 조정 (우측에서 좌측으로) - 숨겨진 컬럼 드러내기
                for (int x = columns - 1; x >= 0; x--)
                {
                    // 경계선 위치 = (x+1)번 경계선의 Left
                    int boundaryX = listLW[x + 1].nLeft;

                    // 컬럼명으로 문자수 구하기
                    int textLength = 0;
                    string columnName = texts[x];

                    if (!string.IsNullOrEmpty(columnName))
                    {
                        var matched = m_ReceiptDgHeaderInfos.FirstOrDefault(h => h.sName == columnName);
                        if (matched != null)
                        {
                            textLength = matched.sName.Length;
                        }
                        else
                        {
                            textLength = columnName.Length;
                        }
                    }
                    else
                    {
                        textLength = 3; // 기본값
                    }

                    // 목표 폭 = 컬럼 문자수 * 18
                    int targetWidth = textLength * 18;
                    int targetX = listLW[x].nLeft + targetWidth;

                    // 드래그 거리 계산
                    int dx = targetX - boundaryX;

                    Draw.Point ptStart = new Draw.Point(boundaryX, center);

                    // 드래그로 폭 조정
                    await Simulation_Mouse.SafeMouseEvent_DragLeft_Smooth_HorizonAsync(
                        mRcpt.DG오더_hWndTop, ptStart, dx, bBkCursor: false, nMiliSec: 100);

                    await Task.Delay(c_nWaitNormal);
                }

                bmpHeader.Dispose();
                Debug.WriteLine($"[{AppName}] {columns}개 컬럼 폭 조정 완료");

                // 2-5. 폭 조정 후 원하는 컬럼 개수 확인
                await Task.Delay(c_nWaitShort);
                var (bmpHeader2, listLW2, columns2) = CaptureAndDetectColumnBoundaries(rcHeader, targetRow);
                if (bmpHeader2 == null)
                {
                    return new StdResult_Error($"[{AppName}] 폭 조정 후 캡처 실패",
                        "OnecallAct_RcptRegPage/InitDG오더Async_02");
                }

                string[] texts2 = await OfrAllColumnsAsync(bmpHeader2, listLW2, columns2, headerGab, textHeight, bEdit: true);

                // 원하는 컬럼 개수 확인
                int matchedCount = 0;
                List<string> missingColumns = new List<string>();

                for (int i = 0; i < m_ReceiptDgHeaderInfos.Length; i++)
                {
                    bool found = false;
                    for (int x = 0; x < columns2; x++)
                    {
                        if (texts2[x] == m_ReceiptDgHeaderInfos[i].sName)
                        {
                            matchedCount++;
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        missingColumns.Add(m_ReceiptDgHeaderInfos[i].sName);
                    }
                }

                Debug.WriteLine($"[{AppName}] 원하는 컬럼 획득: {matchedCount}/{m_ReceiptDgHeaderInfos.Length}");
                if (missingColumns.Count > 0)
                {
                    Debug.WriteLine($"[{AppName}] - 누락 컬럼({missingColumns.Count}): {string.Join(", ", missingColumns)}");
                }

                bmpHeader2.Dispose();

                // 원하는 컬럼을 모두 얻었으면 종료
                if (matchedCount >= m_ReceiptDgHeaderInfos.Length)
                {
                    Debug.WriteLine($"[{AppName}] Step 2 완료: 목표 컬럼 모두 획득");
                    break;
                }
            }
            #endregion

            #region Step 3 - 컬럼 순서 조정
            // Step 3: 컬럼 순서 조정
            Debug.WriteLine($"[{AppName}] Step 3: 컬럼 순서 조정 시작");

            for (int x = 0; x < m_ReceiptDgHeaderInfos.Length; x++)
            {
                await Task.Delay(250);  // 캡처 전 안정화

                // 3-1. 헤더 전역 캡처 → 경계선 검출
                var (bmpHeader3, listLW3, columns3) = CaptureAndDetectColumnBoundaries(rcHeader, targetRow);
                if (bmpHeader3 == null)
                {
                    return new StdResult_Error($"[{AppName}] Step 3 헤더 캡처 실패 (컬럼 {x})", "OnecallAct_RcptRegPage/InitDG오더Async_03");
                }

                // 3-2. 전체 컬럼 OFR
                string[] texts3 = await OfrAllColumnsAsync(bmpHeader3, listLW3, columns3, headerGab, textHeight, bEdit: true);
                bmpHeader3.Dispose();

                // 3-3. 목표 컬럼 위치 찾기
                string targetText = m_ReceiptDgHeaderInfos[x].sName;
                int find = Array.IndexOf(texts3, targetText);

                if (find < 0)
                {
                    return new StdResult_Error($"[{AppName}] 목표 컬럼 '{targetText}' 찾기 실패", "OnecallAct_RcptRegPage/InitDG오더Async_04");
                }

                if (find == x) continue; // 같은 위치면 패스

                Debug.WriteLine($"[{AppName}] Step 3. 순서 조정: [{find}]'{targetText}' → [{x}]");

                // 3-4. 드래그
                Draw.Point ptStart = new Draw.Point(listLW3[find].nLeft + 10, headerGab + (textHeight / 2));
                Draw.Point ptTarget = new Draw.Point(listLW3[x].nLeft, ptStart.Y);

                await Simulation_Mouse.SafeMouseEvent_DragLeft_SmoothAsync(mRcpt.DG오더_hWndTop, ptStart, ptTarget, bBkCursor: false, nMiliSec: 150);

                await Task.Delay(50);  // 드래그 후
            }

            Debug.WriteLine($"[{AppName}] Step 3 완료");
            #endregion

            #region Step 4 - 컬럼 너비 조정
            // Step 4: 컬럼 너비 조정
            Debug.WriteLine($"[{AppName}] Step 4: 컬럼 너비 조정 시작");

            await Task.Delay(250);  // 캡처 전 안정화

            // 4-1. 헤더 캡처 → 경계선 검출
            var (bmpHeader4, listLW4, columns4) = CaptureAndDetectColumnBoundaries(rcHeader, targetRow);
            if (bmpHeader4 == null)
            {
                return new StdResult_Error($"[{AppName}] Step 4 헤더 캡처 실패", "OnecallAct_RcptRegPage/InitDG오더Async_05");
            }
            bmpHeader4.Dispose();

            // 4-2. 뒤에서 앞으로 폭 조정
            for (int x = m_ReceiptDgHeaderInfos.Length; x > 0; x--)
            {
                int col = x - 1;
                Draw.Point ptStart = new Draw.Point(listLW4[col].nLeft + listLW4[col].nWidth, headerGab + (textHeight / 2));
                int dx = m_ReceiptDgHeaderInfos[col].nWidth - listLW4[col].nWidth;

                Debug.WriteLine($"[{AppName}] Step 4. 너비 조정: [{col}]'{m_ReceiptDgHeaderInfos[col].sName}' {listLW4[col].nWidth}px → {m_ReceiptDgHeaderInfos[col].nWidth}px (dx={dx})");

                await Simulation_Mouse.SafeMouseEvent_DragLeft_Smooth_HorizonAsync(mRcpt.DG오더_hWndTop, ptStart, dx, bBkCursor: false, nMiliSec: 100);
                await Task.Delay(c_nWaitNormal);
            }

            Debug.WriteLine($"[{AppName}] Step 4 완료");
            #endregion
            Debug.WriteLine($"[{AppName}] InitDG오더Async 완료");
            return null;
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
    #endregion

    #region 자동배차 - Kai신규 관련함수들
    /// <summary>
    /// 신규 주문 처리 (Kai 신규 → 원콜 등록)
    /// </summary>
    public async Task<CommonResult_AutoAllocProcess> CheckOcOrderAsync_AssumeKaiNewOrder(AutoAllocModel item, CancelTokenControl ctrl)
    {
        await ctrl.WaitIfPausedOrCancelledAsync();

        string kaiState = item.NewOrder.OrderState;
        Debug.WriteLine($"[{AppName}] CheckOcOrderAsync_AssumeKaiNewOrder: KeyCode={item.KeyCode}, kaiState={kaiState}");

        switch (kaiState)
        {
            case "접수": // 신규등록모드 → 입력 → 저장
                return await RegistOrderModeAsync(item, ctrl);

            case "취소": // 무시 - 비적재
            case "대기":
                return CommonResult_AutoAllocProcess.SuccessAndDestroy(item);

            case "배차":
            case "운행":
            case "완료":
            case "예약":
                Debug.WriteLine($"[{AppName}] 미구현 상태: {kaiState}");
                return CommonResult_AutoAllocProcess.FailureAndDiscard($"미구현 상태: {kaiState}", "CheckOcOrderAsync_AssumeKaiNewOrder_TODO");

            default:
                Debug.WriteLine($"[{AppName}] 알 수 없는 Kai 주문 상태: {kaiState}");
                return CommonResult_AutoAllocProcess.FailureAndDiscard($"알 수 없는 Kai 주문 상태: {kaiState}", "CheckOcOrderAsync_AssumeKaiNewOrder_800");
        }
    }

    /// <summary>
    /// 신규등록모드: 신규버튼 클릭 → 정보 입력 → 저장
    /// </summary>
    public async Task<CommonResult_AutoAllocProcess> RegistOrderModeAsync(AutoAllocModel item, CancelTokenControl ctrl)
    {
        try
        {
            #region 1. 사전작업
            Debug.WriteLine($"[{AppName}] RegistOrderModeAsync 진입: KeyCode={item.KeyCode}");

            // 로컬 변수
            TbOrder tbOrder = item.NewOrder;
            bool bTmp = false;

            // 데이터그리드 축소 (신규버튼 접근 위해)
            await CollapseDG오더Async();

            // 신규 버튼 클릭
            await ctrl.WaitIfPausedOrCancelledAsync();
            await Std32Mouse_Post.MousePostAsync_ClickLeft(mRcpt.접수섹션_hWnd신규버튼);
            Debug.WriteLine($"[{AppName}] 신규버튼 클릭");

            // 신규등록모드 진입 확인 (상차지주소 캡션이 빌때까지 대기)
            bool bNewMode = false;
            for (int i = 0; i < c_nRepeatMany; i++)
            {
                await Task.Delay(c_nWaitShort);
                string caption = Std32Window.GetWindowCaption(mRcpt.접수섹션_hWnd상차지주소);
                if (string.IsNullOrEmpty(caption))
                {
                    bNewMode = true;
                    Debug.WriteLine($"[{AppName}] 신규등록모드 진입 확인");
                    break;
                }
            }
            if (!bNewMode)
            {
                return CommonResult_AutoAllocProcess.FailureAndDiscard("신규등록모드 진입 실패", "RegistOrderModeAsync_01");
            }
            #endregion

            #region 2. 주문 정보 입력
            Debug.WriteLine($"[{AppName}] #region 2 시작: 상차={tbOrder.StartDetailAddr}, 하차={tbOrder.DestDetailAddr}");

            // 상차지 입력
            var result상차 = await Set상세주소Async(mRcpt.접수섹션_hWnd상차지주소, fInfo.접수등록Page_접수_상차지권역_rcChkRelM, tbOrder.StartDetailAddr, ctrl);
            Debug.WriteLine($"[{AppName}] 상차지 결과: {result상차.Result}, {result상차.sErr}");
            if (result상차.Result != StdResult.Success)
                return CommonResult_AutoAllocProcess.FailureAndDiscard($"상차지 입력실패: {result상차.sErr}", "RegistOrderModeAsync_02");

            // 하차지 입력
            var result하차 = await Set상세주소Async(mRcpt.접수섹션_hWnd하차지주소, fInfo.접수등록Page_접수_하차지권역_rcChkRelM, tbOrder.DestDetailAddr, ctrl);
            Debug.WriteLine($"[{AppName}] 하차지 결과: {result하차.Result}, {result하차.sErr}");
            if (result하차.Result != StdResult.Success)
                return CommonResult_AutoAllocProcess.FailureAndDiscard($"하차지 입력실패: {result하차.sErr}", "RegistOrderModeAsync_03");

            // 화물정보 - 디비에 적요가 있으면 쓰고, 없으면 없음을 쓴다
            if (string.IsNullOrEmpty(tbOrder.OrderRemarks)) Std32Window.SetWindowCaption(mRcpt.접수섹션_hWnd화물정보, "없음");
            else Std32Window.SetWindowCaption(mRcpt.접수섹션_hWnd화물정보, tbOrder.OrderRemarks);
            await Task.Delay(c_nWaitShort, ctrl.Token);
            Std32Key_Msg.KeyPost_Click(mRcpt.접수섹션_hWnd화물정보, StdCommon32.VK_RETURN);

            // 운임
            if (tbOrder.FeeTotal > 0) // 총운임
            {
                bTmp = await Simulation_Keyboard.PostFeeWithVerifyAsync(mRcpt.접수섹션_hWnd총운임, tbOrder.FeeTotal);
                if (!bTmp) return CommonResult_AutoAllocProcess.FailureAndDiscard($"총운임 입력실패: {tbOrder.FeeTotal}", "RegistOrderModeAsync_04");
            }
            if (tbOrder.FeeCharge > 0) // 수수료
            {
                bTmp = await Simulation_Keyboard.PostFeeWithVerifyAsync(mRcpt.접수섹션_hWnd수수료, tbOrder.FeeCharge);
                if (!bTmp) return CommonResult_AutoAllocProcess.FailureAndDiscard($"총운임 입력실패: {tbOrder.FeeTotal}", "RegistOrderModeAsync_05");
            }

            // 차량 - 톤수
            await EscapeFocusAsync();
            CommonModel_ComboBox result톤수 = GetCarWeightResult(tbOrder.CarType, tbOrder.CarWeight);
            StdResult_Status resultSts = await SelectComboBoxItemAsync(mRcpt.접수섹션_차량_hWnd톤수, result톤수, mRcpt.접수섹션_hWndTop, fInfo.접수등록Page_접수_톤수_rcChkRelM);
            if (resultSts.Result != StdResult.Success)
                return CommonResult_AutoAllocProcess.FailureAndDiscard($"톤수 선택실패: {resultSts.sErr}", "RegistOrderModeAsync_06");

            // 차량 - 차종
            await EscapeFocusAsync();
            CommonModel_ComboBox result차종 = GetTruckDetailResult(tbOrder.CarType, tbOrder.TruckDetail);
            resultSts = await SelectComboBoxItemAsync(mRcpt.접수섹션_차량_hWnd차종, result차종, mRcpt.접수섹션_hWndTop, fInfo.접수등록Page_접수_차종_rcChkRelM);
            if (resultSts.Result != StdResult.Success)
                return CommonResult_AutoAllocProcess.FailureAndDiscard($"차종 선택실패: {resultSts.sErr}", "RegistOrderModeAsync_07");

            // 차량 - 결재
            await EscapeFocusAsync();
            CommonModel_ComboBox result결재 = GetFeeTypeResult(tbOrder.FeeType);
            resultSts = await SelectComboBoxItemAsync(mRcpt.접수섹션_차량_hWnd결재, result결재, mRcpt.접수섹션_hWndTop, fInfo.접수등록Page_접수_결재_rcChkRelM);
            if (resultSts.Result != StdResult.Success)
                return CommonResult_AutoAllocProcess.FailureAndDiscard($"결재 선택실패: {resultSts.sErr}", "RegistOrderModeAsync_08");

            // 화물중량
            string maxWeight = GetMaxCarWeight(result톤수);
            if (maxWeight != "0.00")
            {
                // 자리수마다 갯수 파학해서 입력해야 하므로 복잡해서 확인 메세지박스 처리로...
            }

            // 구분
            switch (tbOrder.DeliverType)
            {
                case "왕복":
                    resultSts = await SetCheckBoxAsync(mRcpt.접수섹션_구분_hWnd왕복, fInfo.접수등록Page_구분_왕복Part_rcChkRelM, true, "왕복");
                    break;

                case "경유":
                    resultSts = await SetCheckBoxAsync(mRcpt.접수섹션_구분_hWnd경유, fInfo.접수등록Page_구분_경유Part_rcChkRelM, true, "경유");
                    break;

                case "긴급":
                    resultSts = await SetCheckBoxAsync(mRcpt.접수섹션_구분_hWnd긴급, fInfo.접수등록Page_구분_긴급Part_rcChkRelM, true, "긴급");
                    break;

                default:
                    resultSts = new StdResult_Status(StdResult.Success);
                    break;
            }
            if (resultSts.Result != StdResult.Success)
                return CommonResult_AutoAllocProcess.FailureAndDiscard($"배송방법 선택실패: {resultSts.sErr}", "RegistOrderModeAsync_09");

            // 상차방법 - 개선해야함(우선은 수작업)
            string sLoadMethod = "수작업";
            resultSts = await SetCheckBoxAsync(mRcpt.접수섹션_상차방법_hWnd수작업, fInfo.접수등록Page_상차방법_수작업Part_rcChkRelM, true, "수작업");
            if (resultSts.Result != StdResult.Success)
                return CommonResult_AutoAllocProcess.FailureAndDiscard($"상차방법 선택실패: {resultSts.sErr}", "RegistOrderModeAsync_10");

            // 상차일시 - 개선해야함(우선은 당상)
            string sLoadDate = "당상";
            resultSts = await SetCheckBoxAsync(mRcpt.접수섹션_상차일시_hWnd당상, fInfo.접수등록Page_상차일시_당상Part_rcChkRelM, true, "당상");
            if (resultSts.Result != StdResult.Success)
                return CommonResult_AutoAllocProcess.FailureAndDiscard($"상차일시 선택실패: {resultSts.sErr}", "RegistOrderModeAsync_11");

            // 하차방법 - 개선해야함(우선은 수작업)
            string sUnloadMethod = "수작업";
            resultSts = await SetCheckBoxAsync(mRcpt.접수섹션_하차방법_hWnd수작업, fInfo.접수등록Page_하차방법_수작업Part_rcChkRelM, true, "수작업");
            if (resultSts.Result != StdResult.Success)
                return CommonResult_AutoAllocProcess.FailureAndDiscard($"하차방법 선택실패: {resultSts.sErr}", "RegistOrderModeAsync_12");

            // 하차일시 - 개선해야함(우선은 당상)
            string sUnloadDate = "당착";
            resultSts = await SetCheckBoxAsync(mRcpt.접수섹션_하차일시_hWnd당착, fInfo.접수등록Page_하차일시_당착Part_rcChkRelM, true, "당착");
            if (resultSts.Result != StdResult.Success)
                return CommonResult_AutoAllocProcess.FailureAndDiscard($"하차일시 선택실패: {resultSts.sErr}", "RegistOrderModeAsync_13");

            // 화물메모
            await EscapeFocusAsync(ctrl.Token, 100);
            string sMemo = "화물메모 테스트"; // tbOrder.OrderMemo
            Std32Window.SetWindowCaption(mRcpt.접수섹션_hWnd화물메모, sMemo);

            // 의뢰자
            await EscapeFocusAsync(ctrl.Token, 100);
            string sTelNo = StdConvert.ToPhoneNumberFormat(tbOrder.CallTelNo);
            Std32Window.SetWindowCaption(mRcpt.접수섹션_의뢰자_hWnd전화, sTelNo); // 전화번호

            await EscapeFocusAsync(ctrl.Token, 100);
            Std32Window.SetWindowCaption(mRcpt.접수섹션_의뢰자_hWnd상호, tbOrder.CallCustName); // 전화번호

            // 사업자번호
            #endregion

            #region 3. 저장 버튼 클릭
            Debug.WriteLine($"[{AppName}] #region 3 시작: 저장 버튼 클릭");
            await ctrl.WaitIfPausedOrCancelledAsync();
            await Std32Mouse_Post.MousePostAsync_ClickLeft(mRcpt.접수섹션_hWnd저장버튼);

            // 화물중량 확인창 찾기
            (IntPtr hWndParent, IntPtr hWndYesBtn) = (IntPtr.Zero, IntPtr.Zero);
            for (int i = 0; i < c_nRepeatMany; i++)
            {
                await Task.Delay(c_nWaitShort, ctrl.Token);
                (hWndParent, hWndYesBtn) = Std32Window.FindMainWindow_EmptyCaption_HavingChildButton(mInfo.Splash.TopWnd_uProcessId, "예");
                if (hWndYesBtn != IntPtr.Zero) break;
            }

            if (hWndParent == IntPtr.Zero || hWndYesBtn == IntPtr.Zero)
                return CommonResult_AutoAllocProcess.FailureAndDiscard($"화물중량 확인창 찾기 실패: {resultSts.sErr}", "RegistOrderModeAsync_14");

            // 예 버튼클릭
            await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndYesBtn);

            // hWndParent가 없어질때까지 대기
            for (int i = 0; i < c_nRepeatMany; i++)
            {
                await Task.Delay(c_nWaitShort, ctrl.Token);
                if (!Std32Window.IsWindowVisible(hWndParent)) break;
            }

            // 상(하)차지 캡션이 클리어 됬나 체크 - 총갯수를 체크해도 됨.
            bool bSaved = false;
            for (int i = 0; i < c_nRepeatMany; i++)
            {
                await Task.Delay(c_nWaitShort, ctrl.Token);
                string caption상차 = Std32Window.GetWindowCaption(mRcpt.접수섹션_hWnd상차지주소);
                string caption하차 = Std32Window.GetWindowCaption(mRcpt.접수섹션_hWnd하차지주소);
                if (string.IsNullOrEmpty(caption상차) && string.IsNullOrEmpty(caption하차))
                {
                    bSaved = true;
                    Debug.WriteLine($"[{AppName}] 저장 성공 확인");
                    break;
                }
            }
            if (!bSaved)
            {
                return CommonResult_AutoAllocProcess.FailureAndDiscard("저장 확인 실패", "RegistOrderModeAsync_09");
            }
            #endregion

            #region 4. 저장 성공 확인
            await Task.Delay(c_nWaitLong, ctrl.Token);

            // 4-1. 오더번호 OFR
            StdResult_String resultSeqno = await Get오더번호Async(0, ctrl);
            if (string.IsNullOrEmpty(resultSeqno.strResult))
            {
                return CommonResult_AutoAllocProcess.FailureAndDiscard($"오더번호 획득 실패: {resultSeqno.sErr}", "RegistOrderModeAsync_10");
            }
            Debug.WriteLine($"[{AppName}] 주문 등록 완료 - 오더번호: {resultSeqno.strResult}");

            // 4-2. 사전 체크
            if (item.NewOrder.KeyCode <= 0)
            {
                Debug.WriteLine($"[{AppName}] KeyCode 없음 - Kai DB에 없는 주문");
                return CommonResult_AutoAllocProcess.FailureAndDiscard("Kai DB에 없는 주문입니다", "RegistOrderModeAsync_11");
            }

            if (!string.IsNullOrEmpty(item.NewOrder.Onecall))
            {
                Debug.WriteLine($"[{AppName}] 이미 등록된 오더번호: {item.NewOrder.Onecall}");
                return CommonResult_AutoAllocProcess.SuccessAndDestroy(item);
            }

            if (s_SrGClient == null || !s_SrGClient.m_bLoginSignalR)
            {
                Debug.WriteLine($"[{AppName}] SignalR 연결 안됨");
                return CommonResult_AutoAllocProcess.FailureAndDiscard("서버 연결이 끊어졌습니다", "RegistOrderModeAsync_12");
            }

            // 4-3. Kai DB 업데이트
            item.NewOrder.Onecall = resultSeqno.strResult;
            StdResult_Int resultUpdate = await s_SrGClient.SrResult_Order_UpdateRowAsync_Today_WithRequestId(item.NewOrder);

            if (resultUpdate.nResult < 0 || !string.IsNullOrEmpty(resultUpdate.sErr))
            {
                Debug.WriteLine($"[{AppName}] Kai DB 업데이트 실패: {resultUpdate.sErr}");
                return CommonResult_AutoAllocProcess.FailureAndDiscard($"Kai DB 업데이트 실패: {resultUpdate.sErr}", "RegistOrderModeAsync_13");
            }

            Debug.WriteLine($"[{AppName}] Kai DB 업데이트 성공 - Onecall: {resultSeqno.strResult}");
            #endregion

            Debug.WriteLine($"[{AppName}] RegistOrderModeAsync 완료: KeyCode={item.KeyCode}");
            return CommonResult_AutoAllocProcess.SuccessAndReEnqueue(item, PostgService_Common_OrderState.NotChanged);
        }
        catch (Exception ex)
        {
            return CommonResult_AutoAllocProcess.FailureAndDiscard(StdUtil.GetExceptionMessage(ex), "RegistOrderModeAsync_999");
        }
    }
    #endregion

    #region 자동배차 - Kai변경 관련함수들
    /// <summary>
    /// Kai DB에서 업데이트된 주문을 원콜 앱에 반영 (Existed_WithSeqno | Updated_Assume)
    /// </summary>
    public async Task<CommonResult_AutoAllocProcess> CheckIsOrderAsync_AssumeKaiUpdated(AutoAllocModel item, CommonResult_AutoAllocDatagrid dgInfo, CancelTokenControl ctrl)
    {
        string kaiState = item.NewOrder?.OrderState ?? "";
        string ocState = dgInfo.sStatus;
        Debug.WriteLine($"[CheckIsOrderAsync_AssumeKaiUpdated] KeyCode={item.KeyCode}, Kai={kaiState}, Onecall={ocState}");

        await ctrl.WaitIfPausedOrCancelledAsync();

        switch (kaiState)
        {
            case "대기":
            //case "취소": // 취소가 아니면 화물취소 시키고 비적재 - 테스트모드만 주석처리 끝나면 원복...
                if (ocState != "취소") return await UpdateOrderModeAsync(item, dgInfo.nIndex, "취소", null, ctrl);
                else return CommonResult_AutoAllocProcess.SuccessAndDestroy(item);

            case "취소": // 테스트용으로 임시작업
            case "접수": // 같은 접수상태면 
                if (ocState == "접수")
                {
                    return await UpdateOrderModeAsync(item, dgInfo.nIndex, null, item.NewOrder, ctrl);
                }
                else
                {
                    //MsgBox("화물24시가 접수이외의 상태이니, 연구가 필요합니다");
                    //return CommonResult_AutoAllocProcess.SuccessAndDestroy(item);

                    // For Test
                    return await UpdateOrderModeAsync(item, dgInfo.nIndex, null, item.NewOrder, ctrl);
                }

            default:
                System.Windows.MessageBox.Show($"[TODO] kaiState={kaiState}, Cg24State={ocState}", "CheckIsOrderAsync_AssumeKaiUpdated");
                break;
        }

        return CommonResult_AutoAllocProcess.SuccessAndReEnqueue();
    }

    /// <summary>
    /// 수정모드: 로우 셀렉트 → 상태변경/정보수정 → 저장
    /// - targetState: 상태 변경 ("취소" 등, null이면 스킵)
    /// - order: 정보 수정 (null이면 스킵)
    /// </summary>
    public async Task<CommonResult_AutoAllocProcess> UpdateOrderModeAsync(
        AutoAllocModel item, int nRowIndex, string targetState, TbOrder order, CancelTokenControl ctrl)
    {
        try
        {
            Debug.WriteLine($"[{AppName}] UpdateOrderModeAsync 진입: KeyCode={item.KeyCode}, RowIndex={nRowIndex}, targetState={targetState}, order={(order != null ? "있음" : "없음")}");

            // 0. 파라미터 검증
            if (string.IsNullOrEmpty(targetState) && order == null)
                return CommonResult_AutoAllocProcess.FailureAndDiscard("targetState와 order 둘 다 null입니다.", "UpdateOrderModeAsync_00");

            // 1. 해당 로우 클릭 (클릭만으로 수정모드 진입)
            bool bSelected = await ClickDatagridRowAsync(nRowIndex);
            Debug.WriteLine($"[{AppName}] 로우 클릭 완료: nRowIndex={nRowIndex}, 선택됨={bSelected}");
            if (!bSelected)
                return CommonResult_AutoAllocProcess.FailureAndRetry("로우 선택 실패", "UpdateOrderModeAsync_01");

            // 2. 오더번호 검증 (선택한 로우가 올바른지 확인)
            string expectedSeqno = item.NewOrder.Onecall;
            var resultSeqno = await Get오더번호Async(nRowIndex, ctrl);
            if (!string.IsNullOrEmpty(resultSeqno.sErr))
                return CommonResult_AutoAllocProcess.FailureAndRetry($"오더번호 읽기 실패: {resultSeqno.sErr}", "UpdateOrderModeAsync_02");
            if (resultSeqno.strResult != expectedSeqno)
                return CommonResult_AutoAllocProcess.FailureAndRetry($"오더번호 불일치: 예상={expectedSeqno}, 실제={resultSeqno.strResult}", "UpdateOrderModeAsync_02B");
            Debug.WriteLine($"[{AppName}] 오더번호 검증 성공: {resultSeqno.strResult}");

            // 3. 상태 변경 (targetState가 있으면)
            if (!string.IsNullOrEmpty(targetState))
            {
                // TODO: 취소 등 상태 버튼 클릭

                // TODO: 6. 취소 성공 확인
            }

            // TODO: 4. 정보 수정 (order가 있으면)
            if (order != null)
            {
                // TODO: 화면값 vs DB값 비교 → 선택적 수정

                // TODO: 5. 저장 버튼 클릭

                // TODO: 6. 저장 성공 확인
            }

            return CommonResult_AutoAllocProcess.FailureAndDiscard("TODO: UpdateOrderModeAsync 미구현", "UpdateOrderModeAsync_TODO");
        }
        catch (Exception ex)
        {
            return CommonResult_AutoAllocProcess.FailureAndDiscard(StdUtil.GetExceptionMessage(ex), "UpdateOrderModeAsync_999");
        }
    }
    #endregion

    #region 자동배차 - Insung상태관리 관련함수들
    /// <summary>
    /// 원콜 주문 상태 관리 및 모니터링 (NotChanged 상황 처리)
    /// </summary>
    public async Task<CommonResult_AutoAllocProcess> CheckIsOrderAsync_OnecallOrderManage(AutoAllocModel item, CommonResult_AutoAllocDatagrid dgInfo, CancelTokenControl ctrl)
    {
        await ctrl.WaitIfPausedOrCancelledAsync();

        string kaiState = item.NewOrder?.OrderState ?? "";
        string ocState = dgInfo.sStatus;

        Debug.WriteLine($"[CheckIsOrderAsync_OnecallOrderManage] KeyCode={item.KeyCode}, Kai={kaiState}, Onecall={ocState}");

        // TODO: 실제 처리 구현
        return CommonResult_AutoAllocProcess.SuccessAndReEnqueue();
    }
    #endregion
}

#nullable restore
