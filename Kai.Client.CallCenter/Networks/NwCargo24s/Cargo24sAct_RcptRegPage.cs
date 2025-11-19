using System.Diagnostics;
using System.Linq;
using System.Windows.Media;
using Draw = System.Drawing;

using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;
using Kai.Common.NetDll_WpfCtrl.NetOFR;

using Kai.Client.CallCenter.Classes;
using Kai.Client.CallCenter.Windows;
using static Kai.Client.CallCenter.Classes.CommonVars;

namespace Kai.Client.CallCenter.Networks.NwCargo24s;
#nullable disable

/// <summary>
/// 화물24시 접수등록 페이지 초기화 및 제어 담당 클래스
/// Context 패턴 사용: Cargo24Context를 통해 모든 정보에 접근
/// </summary>
public class Cargo24sAct_RcptRegPage
{
    #region Datagrid Column Header Info
    /// <summary>
    /// Datagrid 컬럼 헤더 정보 배열 (22개)
    /// </summary>
    public readonly NwCommon_DgColumnHeader[] m_ReceiptDgHeaderInfos = new NwCommon_DgColumnHeader[]
    {
        new NwCommon_DgColumnHeader() { sName = "0", bOfrSeq = true, nWidth = 0 },
        new NwCommon_DgColumnHeader() { sName = "상태", bOfrSeq = false, nWidth = 40 },
        new NwCommon_DgColumnHeader() { sName = "화물번호", bOfrSeq = true, nWidth = 80 },
        new NwCommon_DgColumnHeader() { sName = "처리시간", bOfrSeq = true, nWidth = 70 },
        new NwCommon_DgColumnHeader() { sName = "고객명", bOfrSeq = false, nWidth = 120 },
        new NwCommon_DgColumnHeader() { sName = "고객전화", bOfrSeq = true, nWidth = 90 },
        new NwCommon_DgColumnHeader() { sName = "차주전화", bOfrSeq = true, nWidth = 90 },
        new NwCommon_DgColumnHeader() { sName = "상차지", bOfrSeq = false, nWidth = 100 },
        new NwCommon_DgColumnHeader() { sName = "하차지", bOfrSeq = false, nWidth = 100 },
        new NwCommon_DgColumnHeader() { sName = "운송료", bOfrSeq = true, nWidth = 50 },
        new NwCommon_DgColumnHeader() { sName = "수수료", bOfrSeq = true, nWidth = 50 },
        new NwCommon_DgColumnHeader() { sName = "공유", bOfrSeq = false, nWidth = 40 },
        new NwCommon_DgColumnHeader() { sName = "SMS", bOfrSeq = false, nWidth = 40 },
        new NwCommon_DgColumnHeader() { sName = "혼적", bOfrSeq = false, nWidth = 40 },
        new NwCommon_DgColumnHeader() { sName = "요금구분", bOfrSeq = false, nWidth = 60 },
        new NwCommon_DgColumnHeader() { sName = "계산서금액", bOfrSeq = true, nWidth = 70 },
        new NwCommon_DgColumnHeader() { sName = "차량톤수", bOfrSeq = true, nWidth = 60 },
        new NwCommon_DgColumnHeader() { sName = "톤수", bOfrSeq = false, nWidth = 50 },
        new NwCommon_DgColumnHeader() { sName = "차종", bOfrSeq = false, nWidth = 60 },
        new NwCommon_DgColumnHeader() { sName = "적재옵션", bOfrSeq = false, nWidth = 100 },
        new NwCommon_DgColumnHeader() { sName = "차량종류", bOfrSeq = false, nWidth = 60 },
        new NwCommon_DgColumnHeader() { sName = "화물정보", bOfrSeq = false, nWidth = 150 },
    };
    #endregion

    #region Context Reference
    /// <summary>
    /// Context에 대한 읽기 전용 참조
    /// </summary>
    private readonly Cargo24Context m_Context;

    /// <summary>
    /// 편의를 위한 로컬 참조들
    /// </summary>
    private Cargo24sInfo_File m_FileInfo => m_Context.FileInfo;
    private Cargo24sInfo_Mem m_MemInfo => m_Context.MemInfo;
    private Cargo24sInfo_Mem.MainWnd m_Main => m_MemInfo.Main;
    private Cargo24sInfo_Mem.SplashWnd m_Splash => m_MemInfo.Splash;
    private Cargo24sInfo_Mem.RcptRegPage m_RcptPage => m_MemInfo.RcptPage;
    #endregion

    #region Constructor
    /// <summary>
    /// 생성자 - Context를 받아서 초기화
    /// </summary>
    public Cargo24sAct_RcptRegPage(Cargo24Context context)
    {
        m_Context = context ?? throw new ArgumentNullException(nameof(context));
        Debug.WriteLine($"[Cargo24sAct_RcptRegPage] 생성자 호출: AppName={m_Context.AppName}");
    }
    #endregion

    #region RcptRegPage Initialize
    /// <summary>
    /// 접수등록 페이지 초기화
    /// Cargo24는 로그인 후 자동으로 접수등록Page를 열므로 바메뉴 클릭 불필요
    /// 1. 팝업 처리 (안내문 - "오늘하루동안 감추기" 클릭) - 메인윈도우 기준
    /// 2. StatusBtn 찾기 (접수/운행/취소/완료/정산/전체)
    /// 3. CmdBtn 찾기 (신규/조회)
    /// 4. Datagrid 찾기 및 초기화
    /// </summary>
    public async Task<StdResult_Error> InitializeAsync(bool bEdit = true, bool bWrite = true, bool bMsgBox = true)
    {
        IntPtr hWndMain = m_Main.TopWnd_hWnd; // 메인윈도우 기준으로 모든 UI 요소 찾기
        IntPtr hWndTmp = IntPtr.Zero;
        string sTmp = string.Empty;

        try
        {
            Debug.WriteLine($"[Cargo24sAct_RcptRegPage] 초기화 시작");

            #region 오늘하루동안감추기 처리
            // 메인윈도우 기준으로 "오늘하루동안 감추기" 버튼 찾기
            hWndTmp = Std32Window.GetWndHandle_FromRelDrawPt(hWndMain, m_FileInfo.접수등록Page_안내문_ptChkRel오늘하루동안감추기);
            if (hWndTmp == IntPtr.Zero)
            {
                Debug.WriteLine($"[Cargo24sAct_RcptRegPage] 오늘하루동안감추기 버튼 없음 - 스킵 (정상)");
            }
            else
            {
                // 버튼 텍스트 확인
                sTmp = Std32Window.GetWindowCaption(hWndTmp);
                Debug.WriteLine($"[Cargo24sAct_RcptRegPage] 버튼 찾음: {hWndTmp:X}, 텍스트={sTmp}");

                if (sTmp == m_FileInfo.접수등록Page_안내문_sWndName오늘하루동안감추기)
                {
                    Debug.WriteLine($"[Cargo24sAct_RcptRegPage] 버튼 클릭 시도");

                    // 클릭
                    await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndTmp);

                    // 창 닫힘 대기 (최대 5초)
                    for (int i = 0; i < c_nRepeatMany; i++)
                    {
                        if (!StdWin32.IsWindow(hWndTmp)) break;
                        await Task.Delay(c_nWaitShort);
                    }

                    await Task.Delay(c_nWaitNormal);
                    Debug.WriteLine($"[Cargo24sAct_RcptRegPage] 팝업 처리 완료");
                }
            }
            #endregion

            #region StatusBtn 찾기 (접수/운행/취소/완료/정산/전체)
            Debug.WriteLine($"[Cargo24sAct_RcptRegPage] StatusBtn 찾기 시작");

            // 접수 버튼
            var (hWnd접수, err접수) = await FindStatusButtonAsync(m_FileInfo.접수등록Page_StatusBtn_sWndName접수, m_FileInfo.접수등록Page_StatusBtn_ptChkRel접수, "Cargo24sAct_RcptRegPage/InitializeAsync_01", bWrite, bMsgBox);
            if (err접수 != null) return err접수;
            m_RcptPage.StatusBtn_hWnd접수 = hWnd접수;

            // 운행 버튼
            var (hWnd운행, err운행) = await FindStatusButtonAsync(m_FileInfo.접수등록Page_StatusBtn_sWndName운행, m_FileInfo.접수등록Page_StatusBtn_ptChkRel운행, "Cargo24sAct_RcptRegPage/InitializeAsync_02", bWrite, bMsgBox);
            if (err운행 != null) return err운행;
            m_RcptPage.StatusBtn_hWnd운행 = hWnd운행;

            // 취소 버튼
            var (hWnd취소, err취소) = await FindStatusButtonAsync(m_FileInfo.접수등록Page_StatusBtn_sWndName취소, m_FileInfo.접수등록Page_StatusBtn_ptChkRel취소, "Cargo24sAct_RcptRegPage/InitializeAsync_03", bWrite, bMsgBox);
            if (err취소 != null) return err취소;
            m_RcptPage.StatusBtn_hWnd취소 = hWnd취소;

            // 완료 버튼
            var (hWnd완료, err완료) = await FindStatusButtonAsync(m_FileInfo.접수등록Page_StatusBtn_sWndName완료, m_FileInfo.접수등록Page_StatusBtn_ptChkRel완료, "Cargo24sAct_RcptRegPage/InitializeAsync_04", bWrite, bMsgBox);
            if (err완료 != null) return err완료;
            m_RcptPage.StatusBtn_hWnd완료 = hWnd완료;

            // 정산 버튼
            var (hWnd정산, err정산) = await FindStatusButtonAsync(m_FileInfo.접수등록Page_StatusBtn_sWndName정산, m_FileInfo.접수등록Page_StatusBtn_ptChkRel정산, "Cargo24sAct_RcptRegPage/InitializeAsync_05", bWrite, bMsgBox);
            if (err정산 != null) return err정산;
            m_RcptPage.StatusBtn_hWnd정산 = hWnd정산;

            // 전체 버튼
            var (hWnd전체, err전체) = await FindStatusButtonAsync(m_FileInfo.접수등록Page_StatusBtn_sWndName전체, m_FileInfo.접수등록Page_StatusBtn_ptChkRel전체, "Cargo24sAct_RcptRegPage/InitializeAsync_06", bWrite, bMsgBox);
            if (err전체 != null) return err전체;
            m_RcptPage.StatusBtn_hWnd전체 = hWnd전체;

            Debug.WriteLine($"[Cargo24sAct_RcptRegPage] StatusBtn 찾기 완료");
            #endregion

            #region CmdBtn 찾기 (신규/조회)
            Debug.WriteLine($"[Cargo24sAct_RcptRegPage] CmdBtn 찾기 시작");

            // 신규 버튼
            var (hWnd신규, err신규) = await FindStatusButtonAsync(m_FileInfo.접수등록Page_CmdBtn_sWndName신규, m_FileInfo.접수등록Page_CmdBtn_ptChkRel신규M, "Cargo24sAct_RcptRegPage/InitializeAsync_07", bWrite, bMsgBox);
            if (err신규 != null) return err신규;
            m_RcptPage.CmdBtn_hWnd신규 = hWnd신규;

            // 조회 버튼
            var (hWnd조회, err조회) = await FindStatusButtonAsync(m_FileInfo.접수등록Page_CmdBtn_sWndName조회, m_FileInfo.접수등록Page_CmdBtn_ptChkRel조회M, "Cargo24sAct_RcptRegPage/InitializeAsync_08", bWrite, bMsgBox);
            if (err조회 != null) return err조회;
            m_RcptPage.CmdBtn_hWnd조회 = hWnd조회;

            Debug.WriteLine($"[Cargo24sAct_RcptRegPage] CmdBtn 찾기 완료");
            #endregion

            #region 리스트항목 버튼 찾기 (순서저장/원래대로)
            Debug.WriteLine($"[Cargo24sAct_RcptRegPage] 리스트항목 버튼 찾기 시작");

            // 순서저장 버튼
            var (hWnd순서저장, err순서저장) = await FindStatusButtonAsync(m_FileInfo.접수등록Page_리스트항목_sWndName순서저장, m_FileInfo.접수등록Page_리스트항목_ptChkRel순서저장, "Cargo24sAct_RcptRegPage/InitializeAsync_09", bWrite, bMsgBox);
            if (err순서저장 != null) return err순서저장;
            m_RcptPage.리스트항목_hWnd순서저장 = hWnd순서저장;

            // 원래대로 버튼
            var (hWnd원래대로, err원래대로) = await FindStatusButtonAsync(m_FileInfo.접수등록Page_리스트항목_sWndName원래대로, m_FileInfo.접수등록Page_리스트항목_ptChkRel원래대로, "Cargo24sAct_RcptRegPage/InitializeAsync_10", bWrite, bMsgBox);
            if (err원래대로 != null) return err원래대로;
            m_RcptPage.리스트항목_hWnd원래대로 = hWnd원래대로;

            Debug.WriteLine($"[Cargo24sAct_RcptRegPage] 리스트항목 버튼 찾기 완료");
            #endregion

            #region Datagrid 찾기 및 초기화
            Debug.WriteLine($"[Cargo24sAct_RcptRegPage] Datagrid 초기화 시작");

            var resultErr = await SetDG오더RectsAsync(bEdit, bWrite, bMsgBox);
            if (resultErr != null) return resultErr;

            Debug.WriteLine($"[Cargo24sAct_RcptRegPage] Datagrid 초기화 완료");
            #endregion

            Debug.WriteLine($"[Cargo24sAct_RcptRegPage] 초기화 완료");
            return null; // 성공
        }
        catch (Exception ex)
        {
            return CommonFuncs_StdResult.ErrMsgResult_Error(
                $"[{m_Context.AppName}/RcptRegPage]예외발생: {ex.Message}",
                "Cargo24sAct_RcptRegPage/InitializeAsync_999", bWrite, bMsgBox);
        }
    }
    #endregion

    #region Datagrid Methods
    /// <summary>
    /// Datagrid 초기화 및 Cell 좌표 계산
    /// Step 3: Datagrid 핸들 찾기 및 기본 검증
    /// Step 4-1, 4-2: Datagrid 캡처 및 헤더 최소 밝기 검출
    /// </summary>
    public async Task<StdResult_Error> SetDG오더RectsAsync(bool bEdit = true, bool bWrite = true, bool bMsgBox = true)
    {
        IntPtr hWndMain = m_Main.TopWnd_hWnd;
        string sTmp = string.Empty;
        Draw.Bitmap bmpDG = null;
        bool bOverlayCreated = false;

        try
        {
            Debug.WriteLine($"[Cargo24sAct_RcptRegPage] SetDG오더RectsAsync 시작");

            // Datagrid 핸들 찾기
            m_RcptPage.DG오더_hWnd = Std32Window.GetWndHandle_FromRelDrawPt(hWndMain, m_FileInfo.접수등록Page_DG오더_ptChkRel);
            if (m_RcptPage.DG오더_hWnd == IntPtr.Zero)
            {
                return CommonFuncs_StdResult.ErrMsgResult_Error($"[{m_Context.AppName}/SetDG오더]Datagrid 핸들 못찾음", "Cargo24sAct_RcptRegPage/SetDG오더RectsAsync_01", bWrite, bMsgBox);
            }
            Debug.WriteLine($"[Cargo24sAct_RcptRegPage] Datagrid 핸들 찾음: {m_RcptPage.DG오더_hWnd:X}");

            // ClassName 검증
            sTmp = Std32Window.GetWindowClassName(m_RcptPage.DG오더_hWnd);
            if (sTmp != m_FileInfo.접수등록Page_DG오더_sClassName)
            {
                return CommonFuncs_StdResult.ErrMsgResult_Error($"[{m_Context.AppName}/SetDG오더]Datagrid 클래스명 불일치: {sTmp} != {m_FileInfo.접수등록Page_DG오더_sClassName}", "Cargo24sAct_RcptRegPage/SetDG오더RectsAsync_02", bWrite, bMsgBox);
            }
            Debug.WriteLine($"[Cargo24sAct_RcptRegPage] Datagrid ClassName 검증 완료: {sTmp}");

            // AbsRect 계산
            m_RcptPage.DG오더_AbsRect = Std32Window.GetWindowRect_DrawAbs(m_RcptPage.DG오더_hWnd);
            Debug.WriteLine($"[Cargo24sAct_RcptRegPage] Datagrid AbsRect: {m_RcptPage.DG오더_AbsRect}");

            // Step 4-1: 헤더 영역만 캡처 (Datagrid 핸들 + Datagrid 기준 상대좌표)
            Draw.Rectangle rcHeaderInDG = new Draw.Rectangle(0, 0, m_RcptPage.DG오더_AbsRect.Width, m_FileInfo.접수등록Page_DG오더_headerHeight);

            // [디버깅] 캡처 영역 시각화 (DG 핸들 기준 오버레이 + DG 기준 상대좌표)
            if (bEdit)
            {
                TransparantWnd.CreateOverlay(m_RcptPage.DG오더_hWnd);
                bOverlayCreated = true;
                TransparantWnd.DrawBoxAsync(rcHeaderInDG, Colors.Red, 1);
            }

            bmpDG = OfrService.CaptureScreenRect_InWndHandle(m_RcptPage.DG오더_hWnd, rcHeaderInDG);
            if (bmpDG == null)
            {
                return CommonFuncs_StdResult.ErrMsgResult_Error($"[{m_Context.AppName}/SetDG오더]헤더 영역 캡처 실패: rcHeaderInDG={rcHeaderInDG}", "Cargo24sAct_RcptRegPage/SetDG오더RectsAsync_03", bWrite, bMsgBox);
            }
            Debug.WriteLine($"[Cargo24sAct_RcptRegPage] 헤더 영역 캡처 성공: {bmpDG.Width}x{bmpDG.Height}");

            // Step 4-2: 헤더 상단 여백에서 최소 밝기 검출
            const int headerGab = 7; // 헤더 상단 여백
            int targetRow = headerGab; // 텍스트가 없는 Y 위치 (경계선만 검출)

            byte minBrightness = OfrService.GetMinBrightnessAtRow_FromColorBitmapFast(bmpDG, targetRow);
            if (minBrightness == 255) // 검출 실패
            {
                return CommonFuncs_StdResult.ErrMsgResult_Error($"[{m_Context.AppName}/SetDG오더]헤더 행 최소 밝기 검출 실패: targetRow={targetRow}", "Cargo24sAct_RcptRegPage/SetDG오더RectsAsync_04", bWrite, bMsgBox);
            }

            minBrightness -= 2; // 경계를 더 정확히 잡기 위해 어둡게 조정 (경계선이 어두우므로)
            Debug.WriteLine($"[Cargo24sAct_RcptRegPage] 헤더 행 최소 밝기 검출: targetRow={targetRow}, minBrightness={minBrightness}");

            // Step 4-3: Bool 배열 생성 및 컬럼 경계 검출
            // 4-3-1. Bool 배열 생성 (true=검은색, false=흰색)
            bool[] boolArr = OfrService.GetBoolArray_FromColorBitmapRowFast(bmpDG, targetRow, minBrightness, 2); // 마진 2픽셀
            if (boolArr == null || boolArr.Length == 0)
            {
                return CommonFuncs_StdResult.ErrMsgResult_Error($"[{m_Context.AppName}/SetDG오더]Bool 배열 생성 실패: targetRow={targetRow}", "Cargo24sAct_RcptRegPage/SetDG오더RectsAsync_05", bWrite, bMsgBox);
            }
            Debug.WriteLine($"[Cargo24sAct_RcptRegPage] Bool 배열 생성 완료: Length={boolArr.Length}");

            // 4-3-2. 컬럼 경계 리스트 추출
            List<OfrModel_LeftWidth> listLW = OfrService.GetLeftWidthList_FromBool1Array(boolArr, minBrightness);
            if (listLW == null || listLW.Count == 0)
            {
                return CommonFuncs_StdResult.ErrMsgResult_Error($"[{m_Context.AppName}/SetDG오더]컬럼 경계 검출 실패: 검출된 리스트 수=0", "Cargo24sAct_RcptRegPage/SetDG오더RectsAsync_06", bWrite, bMsgBox);
            }

            // 4-3-3. 첫 번째와 마지막 항목 제거 (테두리 명도 + 오른쪽 끝 경계)
            if (listLW.Count >= 2)
            {
                listLW.RemoveAt(0); // 첫 번째 제거 (테두리 명도 섞임)
                listLW.RemoveAt(listLW.Count - 1); // 마지막 제거 (오른쪽 끝 경계)
                Debug.WriteLine($"[Cargo24sAct_RcptRegPage] 첫/마지막 항목 제거 완료");
            }

            int columns = listLW.Count; // 제거 후 남은 개수 = 실제 컬럼 개수
            Debug.WriteLine($"[Cargo24sAct_RcptRegPage] 컬럼 경계 검출 완료: 실제 컬럼={columns}개");

            // [디버깅] listLW 첫 3개 값 출력
            string debugInfo = "[디버깅] listLW 첫 3개:\n";
            for (int dbg = 0; dbg < Math.Min(3, listLW.Count); dbg++)
            {
                debugInfo += $"  [{dbg}] Left={listLW[dbg].nLeft}, Width={listLW[dbg].nWidth}\n";
            }
            Debug.WriteLine(debugInfo);

            // TODO: Step 4-4 - 컬럼 헤더 OFR
            // TODO: Step 5 - Cell 좌표 배열 생성

            Debug.WriteLine($"[Cargo24sAct_RcptRegPage] SetDG오더RectsAsync 완료 (Step 3 + Step 4-1,2,3)");
            return null; // 성공
        }
        catch (Exception ex)
        {
            return CommonFuncs_StdResult.ErrMsgResult_Error($"[{m_Context.AppName}/SetDG오더]예외발생: {ex.Message}", "Cargo24sAct_RcptRegPage/SetDG오더RectsAsync_999", bWrite, bMsgBox);
        }
        finally
        {
            // 리소스 정리
            bmpDG?.Dispose();
            if (bOverlayCreated)
            {
                TransparantWnd.DeleteOverlay();
            }
        }
    }
    #endregion

    #region Helper Methods
    /// <summary>
    /// StatusBtn 찾기 헬퍼 메서드 (인성 패턴 참고)
    /// </summary>
    private async Task<(IntPtr hWnd, StdResult_Error error)> FindStatusButtonAsync(string buttonName, Draw.Point checkPoint, string errorCode, bool bWrite, bool bMsgBox, bool withTextValidation = true)
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
                        Debug.WriteLine($"[Cargo24sAct_RcptRegPage] {buttonName}버튼 찾음: {hWnd:X}, 텍스트: {text}");
                        return (hWnd, null);
                    }
                }
                else
                {
                    Debug.WriteLine($"[Cargo24sAct_RcptRegPage] {buttonName}버튼 찾음: {hWnd:X}");
                    return (hWnd, null);
                }
            }

            await Task.Delay(c_nWaitNormal);
        }

        // 찾기 실패
        var error = CommonFuncs_StdResult.ErrMsgResult_Error(
            $"[{m_Context.AppName}/RcptRegPage]{buttonName}버튼 찾기실패: {checkPoint}",
            errorCode, bWrite, bMsgBox);
        return (IntPtr.Zero, error);
    }
    #endregion

    #region Utility Methods
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
}
#nullable restore
