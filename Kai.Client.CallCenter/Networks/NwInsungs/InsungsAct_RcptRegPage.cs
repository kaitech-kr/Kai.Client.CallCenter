using System.Linq;
using System.Diagnostics;
using Draw = System.Drawing;

using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;
using Kai.Common.NetDll_WpfCtrl.NetOFR;

using Kai.Client.CallCenter.Classes;
using Kai.Client.CallCenter.Classes.Class_Master;
using Kai.Client.CallCenter.OfrWorks;
using Kai.Client.CallCenter.Windows;

using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models;

namespace Kai.Client.CallCenter.Networks.NwInsungs;
#nullable disable

/// <summary>
/// Datagrid 검증 이슈 플래그 (비트 조합 가능)
/// </summary>
[Flags]
public enum DgValidationIssue
{
    None = 0,               // 문제 없음
    InvalidColumnCount = 1, // 컬럼 개수 틀림
    InvalidColumn = 2,      // 필요 없는 컬럼 존재
    WrongOrder = 4,         // 컬럼 순서 틀림
    WrongWidth = 8          // 컬럼 너비 틀림 (허용 오차 초과)
}

/// <summary>
/// 인성 앱 접수등록 페이지 초기화 및 제어 담당 클래스
/// Context 패턴 사용: InsungContext를 통해 모든 정보에 접근
/// </summary>
public class InsungsAct_RcptRegPage
{
    #region Constants
    /// <summary>
    /// 컬럼 너비 허용 오차 (픽셀)
    /// </summary>
    private const int COLUMN_WIDTH_TOLERANCE = 2;

    /// <summary>
    /// Datagrid 초기화 최대 재시도 횟수
    /// </summary>
    private const int MAX_DG_INIT_RETRY = 3;

    /// <summary>
    /// Datagrid 헤더 상단 여백 (텍스트 없는 영역)
    /// </summary>
    private const int HEADER_GAB = 7;

    /// <summary>
    /// Datagrid 헤더 텍스트 영역 높이
    /// </summary>
    private const int HEADER_TEXT_HEIGHT = 18;
    #endregion

    #region Context Reference
    /// <summary>
    /// Context에 대한 읽기 전용 참조
    /// </summary>
    private readonly InsungContext m_Context;

    /// <summary>
    /// 편의를 위한 로컬 참조들
    /// </summary>
    private InsungsInfo_File m_FileInfo => m_Context.FileInfo;
    private InsungsInfo_Mem m_MemInfo => m_Context.MemInfo;
    private InsungsInfo_Mem.MainWnd m_Main => m_MemInfo.Main;
    private InsungsInfo_Mem.RcptRegPage m_RcptPage => m_MemInfo.RcptPage;
    #endregion

    #region Column Header Definitions
    /// <summary>
    /// 접수등록 Datagrid 컬럼 헤더 정보 (20개 컬럼)
    /// </summary>
    public readonly NwCommon_DgColumnHeader[] m_ReceiptDgHeaderInfos = new NwCommon_DgColumnHeader[]
    {
        new NwCommon_DgColumnHeader() { sName = "No", bOfrSeq = false, bSaveObject = true, nWidth = 60 },
        new NwCommon_DgColumnHeader() { sName = "상태", bOfrSeq = false, bSaveObject = true,  nWidth = 70 },

        new NwCommon_DgColumnHeader() { sName = "주문번호", bOfrSeq = true, bSaveObject = true, nWidth = 80  },
        new NwCommon_DgColumnHeader() { sName = "최초접수시간", bOfrSeq = true, bSaveObject = true, nWidth = 90 },
        new NwCommon_DgColumnHeader() { sName = "접수시간", bOfrSeq = true, bSaveObject = true, nWidth = 90  },

        new NwCommon_DgColumnHeader() { sName = "고객명", bOfrSeq = false, bSaveObject = true, nWidth = 100 },
        new NwCommon_DgColumnHeader() { sName = "담당자", bOfrSeq = false, bSaveObject = true, nWidth = 100 },

        new NwCommon_DgColumnHeader() { sName = "전화번호", bOfrSeq = true, bSaveObject = true, nWidth = 100 },

        new NwCommon_DgColumnHeader() { sName = "출발동", bOfrSeq = false, bSaveObject = true, nWidth = 120 },
        new NwCommon_DgColumnHeader() { sName = "도착동", bOfrSeq = false, bSaveObject = true, nWidth = 120 },

        new NwCommon_DgColumnHeader() { sName = "요금", bOfrSeq = true, bSaveObject = true, nWidth = 62 },

        new NwCommon_DgColumnHeader() { sName = "지급", bOfrSeq = false, bSaveObject = true, nWidth = 40 },
        new NwCommon_DgColumnHeader() { sName = "형태", bOfrSeq = false, bSaveObject = true, nWidth = 40 },
        new NwCommon_DgColumnHeader() { sName = "차량", bOfrSeq = false, bSaveObject = true, nWidth = 60 },
        new NwCommon_DgColumnHeader() { sName = "계산서", bOfrSeq = false, bSaveObject = true, nWidth = 50 },
        new NwCommon_DgColumnHeader() { sName = "왕복", bOfrSeq = false, bSaveObject = true, nWidth = 40 },
        new NwCommon_DgColumnHeader() { sName = "공유", bOfrSeq = false, bSaveObject = true, nWidth = 40 },
        new NwCommon_DgColumnHeader() { sName = "라이더", bOfrSeq = false, bSaveObject = true, nWidth = 120 },
        new NwCommon_DgColumnHeader() { sName = "오더메모", bOfrSeq = false, bSaveObject = true, nWidth = 100 },
        new NwCommon_DgColumnHeader() { sName = "적요", bOfrSeq = false, bSaveObject = true, nWidth = 340 },
    };
    #endregion

    #region Constructor
    /// <summary>
    /// 생성자 - Context를 받아서 초기화
    /// </summary>
    public InsungsAct_RcptRegPage(InsungContext context)
    {
        m_Context = context ?? throw new ArgumentNullException(nameof(context));
        Debug.WriteLine($"[InsungsAct_RcptRegPage] 생성자 호출: AppName={m_Context.AppName}");
    }
    #endregion

    #region RcptRegPage Initialize
    /// <summary>
    /// 접수등록 페이지 초기화
    /// </summary>
    //public async Task<StdResult_Error> InitializeAsync(bool bEdit = true, bool bWrite = true, bool bMsgBox = true)
    //{
    //    try
    //    {
    //        Debug.WriteLine($"[InsungsAct_RcptRegPage] 초기화 시작");

    //        // 1. 바메뉴 클릭 - 접수등록 페이지 열기
    //        await m_Context.MainWndAct.ClickAsync접수등록();
    //        await Task.Delay(500); // 페이지가 열릴 때까지 대기
    //        Debug.WriteLine($"[InsungsAct_RcptRegPage] 접수등록 바메뉴 클릭 완료");

    //        // 2. TopWnd 찾기 - MdiClient의 자식으로 "접수현황" 찾기
    //        for (int i = 0; i < 100; i++) // 10초 동안 대기
    //        {
    //            m_RcptPage.TopWnd_hWnd = Std32Window.FindWindowEx(
    //                m_Main.WndInfo_MdiClient.hWnd,
    //                IntPtr.Zero,
    //                null,
    //                m_FileInfo.접수등록Page_TopWnd_sWndName
    //            );

    //            if (m_RcptPage.TopWnd_hWnd != IntPtr.Zero) break;
    //            await Task.Delay(100);
    //        }

    //        if (m_RcptPage.TopWnd_hWnd == IntPtr.Zero)
    //        {
    //            return CommonFuncs_StdResult.ErrMsgResult_Error(
    //                $"[{m_Context.AppName}/RcptRegPage]접수등록Page 찾기실패: {m_FileInfo.접수등록Page_TopWnd_sWndName}",
    //                "InsungsAct_RcptRegPage/InitializeAsync_01", bWrite, bMsgBox);
    //        }
    //        Debug.WriteLine($"[InsungsAct_RcptRegPage] 접수등록Page 찾음: {m_RcptPage.TopWnd_hWnd:X}");

    //        // 3. StatusBtn 찾기 - 첫/마지막 버튼으로 로딩 확인
    //        // 3-1. 접수 버튼 찾기 (텍스트 검증으로 페이지 로딩 시작 확인)
    //        for (int i = 0; i < CommonVars.c_nRepeatVeryMany; i++)
    //        {
    //            m_RcptPage.StatusBtn_hWnd접수 = Std32Window.GetWndHandle_FromRelDrawPt(
    //                m_Main.TopWnd_hWnd, m_FileInfo.접수등록Page_StatusBtn_ptChkRel접수M
    //            );

    //            if (m_RcptPage.StatusBtn_hWnd접수 != IntPtr.Zero)
    //            {
    //                string text = Std32Window.GetWindowText(m_RcptPage.StatusBtn_hWnd접수);
    //                if (text.Contains("접수"))
    //                {
    //                    Debug.WriteLine($"[InsungsAct_RcptRegPage] 접수버튼 찾음: {m_RcptPage.StatusBtn_hWnd접수:X}, 텍스트: {text}");
    //                    break;
    //                }
    //            }

    //            await Task.Delay(CommonVars.c_nWaitNormal);
    //        }

    //        if (m_RcptPage.StatusBtn_hWnd접수 == IntPtr.Zero)
    //        {
    //            return CommonFuncs_StdResult.ErrMsgResult_Error(
    //                $"[{m_Context.AppName}/RcptRegPage]접수버튼 찾기실패: {m_FileInfo.접수등록Page_StatusBtn_ptChkRel접수M}",
    //                "InsungsAct_RcptRegPage/InitializeAsync_02", bWrite, bMsgBox);
    //        }

    //        // 3-2. 버튼 로딩 대기
    //        await Task.Delay(CommonVars.c_nWaitNormal);

    //        // 3-3. 전체 버튼 찾기 (텍스트 검증으로 페이지 로딩 완료 확인)
    //        for (int i = 0; i < CommonVars.c_nRepeatVeryMany; i++)
    //        {
    //            m_RcptPage.StatusBtn_hWnd전체 = Std32Window.GetWndHandle_FromRelDrawPt(
    //                m_Main.TopWnd_hWnd, m_FileInfo.접수등록Page_StatusBtn_ptChkRel전체M
    //            );

    //            if (m_RcptPage.StatusBtn_hWnd전체 != IntPtr.Zero)
    //            {
    //                string text = Std32Window.GetWindowText(m_RcptPage.StatusBtn_hWnd전체);
    //                if (text.Contains("전체"))
    //                {
    //                    Debug.WriteLine($"[InsungsAct_RcptRegPage] 전체버튼 찾음: {m_RcptPage.StatusBtn_hWnd전체:X}, 텍스트: {text}");
    //                    break;
    //                }
    //            }

    //            await Task.Delay(CommonVars.c_nWaitNormal);
    //        }

    //        if (m_RcptPage.StatusBtn_hWnd전체 == IntPtr.Zero)
    //        {
    //            return CommonFuncs_StdResult.ErrMsgResult_Error(
    //                $"[{m_Context.AppName}/RcptRegPage]전체버튼 찾기실패: {m_FileInfo.접수등록Page_StatusBtn_ptChkRel전체M}",
    //                "InsungsAct_RcptRegPage/InitializeAsync_03", bWrite, bMsgBox);
    //        }

    //        // 3-4. 중간 StatusBtn 찾기 (페이지 로딩 완료됨)
    //        m_RcptPage.StatusBtn_hWnd배차 = Std32Window.GetWndHandle_FromRelDrawPt(
    //            m_Main.TopWnd_hWnd, m_FileInfo.접수등록Page_StatusBtn_ptChkRel배차M
    //        );
    //        if (m_RcptPage.StatusBtn_hWnd배차 == IntPtr.Zero)
    //        {
    //            return CommonFuncs_StdResult.ErrMsgResult_Error(
    //                $"[{m_Context.AppName}/RcptRegPage]배차버튼 찾기실패: {m_FileInfo.접수등록Page_StatusBtn_ptChkRel배차M}",
    //                "InsungsAct_RcptRegPage/InitializeAsync_04", bWrite, bMsgBox);
    //        }
    //        Debug.WriteLine($"[InsungsAct_RcptRegPage] 배차버튼 찾음: {m_RcptPage.StatusBtn_hWnd배차:X}");

    //        m_RcptPage.StatusBtn_hWnd운행 = Std32Window.GetWndHandle_FromRelDrawPt(
    //            m_Main.TopWnd_hWnd, m_FileInfo.접수등록Page_StatusBtn_ptChkRel운행M
    //        );
    //        if (m_RcptPage.StatusBtn_hWnd운행 == IntPtr.Zero)
    //        {
    //            return CommonFuncs_StdResult.ErrMsgResult_Error(
    //                $"[{m_Context.AppName}/RcptRegPage]운행버튼 찾기실패: {m_FileInfo.접수등록Page_StatusBtn_ptChkRel운행M}",
    //                "InsungsAct_RcptRegPage/InitializeAsync_05", bWrite, bMsgBox);
    //        }
    //        Debug.WriteLine($"[InsungsAct_RcptRegPage] 운행버튼 찾음: {m_RcptPage.StatusBtn_hWnd운행:X}");

    //        m_RcptPage.StatusBtn_hWnd완료 = Std32Window.GetWndHandle_FromRelDrawPt(
    //            m_Main.TopWnd_hWnd, m_FileInfo.접수등록Page_StatusBtn_ptChkRel완료M
    //        );
    //        if (m_RcptPage.StatusBtn_hWnd완료 == IntPtr.Zero)
    //        {
    //            return CommonFuncs_StdResult.ErrMsgResult_Error(
    //                $"[{m_Context.AppName}/RcptRegPage]완료버튼 찾기실패: {m_FileInfo.접수등록Page_StatusBtn_ptChkRel완료M}",
    //                "InsungsAct_RcptRegPage/InitializeAsync_06", bWrite, bMsgBox);
    //        }
    //        Debug.WriteLine($"[InsungsAct_RcptRegPage] 완료버튼 찾음: {m_RcptPage.StatusBtn_hWnd완료:X}");

    //        m_RcptPage.StatusBtn_hWnd취소 = Std32Window.GetWndHandle_FromRelDrawPt(
    //            m_Main.TopWnd_hWnd, m_FileInfo.접수등록Page_StatusBtn_ptChkRel취소M
    //        );
    //        if (m_RcptPage.StatusBtn_hWnd취소 == IntPtr.Zero)
    //        {
    //            return CommonFuncs_StdResult.ErrMsgResult_Error(
    //                $"[{m_Context.AppName}/RcptRegPage]취소버튼 찾기실패: {m_FileInfo.접수등록Page_StatusBtn_ptChkRel취소M}",
    //                "InsungsAct_RcptRegPage/InitializeAsync_07", bWrite, bMsgBox);
    //        }
    //        Debug.WriteLine($"[InsungsAct_RcptRegPage] 취소버튼 찾음: {m_RcptPage.StatusBtn_hWnd취소:X}");

    //        // TODO: 3-1. StatusBtn 이미지 매칭으로 확인 (Up 상태) - OCR 사용시 BlockInput 필요

    //        // 4. StatusBtn - 전체버튼 클릭
    //        await Std32Mouse_Post.MousePostAsync_ClickLeft(m_RcptPage.StatusBtn_hWnd전체);
    //        await Task.Delay(300); // 클릭 반영 대기
    //        Debug.WriteLine($"[InsungsAct_RcptRegPage] 전체버튼 클릭 완료");

    //        // 4-1. 클릭 후 간단 검증 (핸들 유효성 재확인)
    //        string textCheck = Std32Window.GetWindowText(m_RcptPage.StatusBtn_hWnd전체);
    //        if (!textCheck.Contains("전체"))
    //        {
    //            return CommonFuncs_StdResult.ErrMsgResult_Error(
    //                $"[{m_Context.AppName}/RcptRegPage]전체버튼 클릭 후 검증 실패: 텍스트={textCheck}",
    //                "InsungsAct_RcptRegPage/InitializeAsync_04_1", bWrite, bMsgBox);
    //        }
    //        Debug.WriteLine($"[InsungsAct_RcptRegPage] 전체버튼 클릭 검증 완료");

    //        // 4-2. StatusBtn Down 상태 OFR 확인 (전체버튼 클릭 후 UI 상태 변화 대기)
    //        // 4-2-1. 접수버튼 Down (첫 버튼) - OFR 루프로 Down 상태 대기
    //        bool bFoundDown접수 = false;
    //        for (int i = 0; i < CommonVars.c_nRepeatShort; i++)
    //        {
    //            StdResult_NulBool resultOfrStatus접수 = await OfrWork_Insungs.OfrIsMatchedImage_DrawRelRectAsync(
    //                m_RcptPage.StatusBtn_hWnd접수, HEADER_GAB, "Img_접수버튼_Down", false, false, false);

    //            if (StdConvert.NullableBoolToBool(resultOfrStatus접수.bResult))
    //            {
    //                bFoundDown접수 = true;
    //                Debug.WriteLine($"[InsungsAct_RcptRegPage] 접수버튼 Down 상태 확인 완료");
    //                break;
    //            }
    //            await Task.Delay(100);
    //        }
    //        if (!bFoundDown접수)
    //            Debug.WriteLine($"[InsungsAct_RcptRegPage] 접수버튼 Down 상태 OFR 실패 (무시)");

    //        // 4-2-2. 중간 버튼들 Down - 딜레이 후 OFR 바로
    //        await Task.Delay(300);

    //        // 배차버튼 Down
    //        StdResult_NulBool resultOfrStatus배차 = await OfrWork_Insungs.OfrIsMatchedImage_DrawRelRectAsync(
    //            m_RcptPage.StatusBtn_hWnd배차, HEADER_GAB, "Img_배차버튼_Down", false, false, false);
    //        if (!StdConvert.NullableBoolToBool(resultOfrStatus배차.bResult))
    //            Debug.WriteLine($"[InsungsAct_RcptRegPage] 배차버튼 Down 상태 OFR 실패 (무시)");
    //        else
    //            Debug.WriteLine($"[InsungsAct_RcptRegPage] 배차버튼 Down 상태 확인 완료");

    //        // 운행버튼 Down
    //        StdResult_NulBool resultOfrStatus운행 = await OfrWork_Insungs.OfrIsMatchedImage_DrawRelRectAsync(
    //            m_RcptPage.StatusBtn_hWnd운행, HEADER_GAB, "Img_운행버튼_Down", false, false, false);
    //        if (!StdConvert.NullableBoolToBool(resultOfrStatus운행.bResult))
    //            Debug.WriteLine($"[InsungsAct_RcptRegPage] 운행버튼 Down 상태 OFR 실패 (무시)");
    //        else
    //            Debug.WriteLine($"[InsungsAct_RcptRegPage] 운행버튼 Down 상태 확인 완료");

    //        // 완료버튼 Down
    //        StdResult_NulBool resultOfrStatus완료 = await OfrWork_Insungs.OfrIsMatchedImage_DrawRelRectAsync(
    //            m_RcptPage.StatusBtn_hWnd완료, HEADER_GAB, "Img_완료버튼_Down", false, false, false);
    //        if (!StdConvert.NullableBoolToBool(resultOfrStatus완료.bResult))
    //            Debug.WriteLine($"[InsungsAct_RcptRegPage] 완료버튼 Down 상태 OFR 실패 (무시)");
    //        else
    //            Debug.WriteLine($"[InsungsAct_RcptRegPage] 완료버튼 Down 상태 확인 완료");

    //        // 취소버튼 Down
    //        StdResult_NulBool resultOfrStatus취소 = await OfrWork_Insungs.OfrIsMatchedImage_DrawRelRectAsync(
    //            m_RcptPage.StatusBtn_hWnd취소, HEADER_GAB, "Img_취소버튼_Down", false, false, false);
    //        if (!StdConvert.NullableBoolToBool(resultOfrStatus취소.bResult))
    //            Debug.WriteLine($"[InsungsAct_RcptRegPage] 취소버튼 Down 상태 OFR 실패 (무시)");
    //        else
    //            Debug.WriteLine($"[InsungsAct_RcptRegPage] 취소버튼 Down 상태 확인 완료");

    //        // 4-2-3. 전체버튼 Down (마지막 버튼) - OFR 루프로 Down 상태 대기
    //        bool bFoundDown전체 = false;
    //        for (int i = 0; i < CommonVars.c_nRepeatShort; i++)
    //        {
    //            StdResult_NulBool resultOfrStatus전체 = await OfrWork_Insungs.OfrIsMatchedImage_DrawRelRectAsync(
    //                m_RcptPage.StatusBtn_hWnd전체, HEADER_GAB, "Img_전체버튼_Down", false, false, false);

    //            if (StdConvert.NullableBoolToBool(resultOfrStatus전체.bResult))
    //            {
    //                bFoundDown전체 = true;
    //                Debug.WriteLine($"[InsungsAct_RcptRegPage] 전체버튼 Down 상태 확인 완료");
    //                break;
    //            }
    //            await Task.Delay(100);
    //        }
    //        if (!bFoundDown전체)
    //            Debug.WriteLine($"[InsungsAct_RcptRegPage] 전체버튼 Down 상태 OFR 실패 (무시)");

    //        Debug.WriteLine($"[InsungsAct_RcptRegPage] StatusBtn Down 상태 확인 완료");

    //        // 5. CommandBtn 찾기 및 OFR 검증 (신규, 조회, 기사)
    //        // 5-1. 신규버튼
    //        m_RcptPage.CmdBtn_hWnd신규 = Std32Window.GetWndHandle_FromRelDrawPt(
    //            m_Main.TopWnd_hWnd, m_FileInfo.접수등록Page_CmdBtn_ptChkRel신규M
    //        );
    //        if (m_RcptPage.CmdBtn_hWnd신규 == IntPtr.Zero)
    //        {
    //            return CommonFuncs_StdResult.ErrMsgResult_Error(
    //                $"[{m_Context.AppName}/RcptRegPage]신규버튼 찾기실패: {m_FileInfo.접수등록Page_CmdBtn_ptChkRel신규M}",
    //                "InsungsAct_RcptRegPage/InitializeAsync_08", bWrite, bMsgBox);
    //        }
    //        Debug.WriteLine($"[InsungsAct_RcptRegPage] 신규버튼 찾음: {m_RcptPage.CmdBtn_hWnd신규:X}");

    //        // OFR 이미지 매칭으로 검증
    //        StdResult_NulBool resultOfrCmd신규 = await OfrWork_Insungs.OfrIsMatchedImage_DrawRelRectAsync(
    //            m_RcptPage.CmdBtn_hWnd신규, 0, "Img_신규버튼", bEdit, bWrite, false);
    //        if (!StdConvert.NullableBoolToBool(resultOfrCmd신규.bResult))
    //        {
    //            Debug.WriteLine($"[InsungsAct_RcptRegPage] 신규버튼 OFR 검증 실패 (무시): {resultOfrCmd신규.sErr}");
    //            // OFR 검증 실패는 경고만 출력 (실패해도 진행)
    //        }

    //        // 5-2. 조회버튼
    //        m_RcptPage.CmdBtn_hWnd조회 = Std32Window.GetWndHandle_FromRelDrawPt(
    //            m_Main.TopWnd_hWnd, m_FileInfo.접수등록Page_CmdBtn_ptChkRel조회M
    //        );
    //        if (m_RcptPage.CmdBtn_hWnd조회 == IntPtr.Zero)
    //        {
    //            return CommonFuncs_StdResult.ErrMsgResult_Error(
    //                $"[{m_Context.AppName}/RcptRegPage]조회버튼 찾기실패: {m_FileInfo.접수등록Page_CmdBtn_ptChkRel조회M}",
    //                "InsungsAct_RcptRegPage/InitializeAsync_09", bWrite, bMsgBox);
    //        }
    //        Debug.WriteLine($"[InsungsAct_RcptRegPage] 조회버튼 찾음: {m_RcptPage.CmdBtn_hWnd조회:X}");

    //        // OFR 이미지 매칭으로 검증
    //        StdResult_NulBool resultOfrCmd조회 = await OfrWork_Insungs.OfrIsMatchedImage_DrawRelRectAsync(
    //            m_RcptPage.CmdBtn_hWnd조회, 0, "Img_조회버튼", bEdit, bWrite, false);
    //        if (!StdConvert.NullableBoolToBool(resultOfrCmd조회.bResult))
    //        {
    //            Debug.WriteLine($"[InsungsAct_RcptRegPage] 조회버튼 OFR 검증 실패 (무시): {resultOfrCmd조회.sErr}");
    //            // OFR 검증 실패는 경고만 출력 (실패해도 진행)
    //        }

    //        // 5-3. 기사버튼
    //        m_RcptPage.CmdBtn_hWnd기사 = Std32Window.GetWndHandle_FromRelDrawPt(
    //            m_Main.TopWnd_hWnd, m_FileInfo.접수등록Page_CmdBtn_ptChkRel기사M
    //        );
    //        if (m_RcptPage.CmdBtn_hWnd기사 == IntPtr.Zero)
    //        {
    //            return CommonFuncs_StdResult.ErrMsgResult_Error(
    //                $"[{m_Context.AppName}/RcptRegPage]기사버튼 찾기실패: {m_FileInfo.접수등록Page_CmdBtn_ptChkRel기사M}",
    //                "InsungsAct_RcptRegPage/InitializeAsync_10", bWrite, bMsgBox);
    //        }
    //        Debug.WriteLine($"[InsungsAct_RcptRegPage] 기사버튼 찾음: {m_RcptPage.CmdBtn_hWnd기사:X}");

    //        // OFR 이미지 매칭으로 검증
    //        StdResult_NulBool resultOfrCmd기사 = await OfrWork_Insungs.OfrIsMatchedImage_DrawRelRectAsync(
    //            m_RcptPage.CmdBtn_hWnd기사, 0, "Img_기사버튼", bEdit, bWrite, false);
    //        if (!StdConvert.NullableBoolToBool(resultOfrCmd기사.bResult))
    //        {
    //            Debug.WriteLine($"[InsungsAct_RcptRegPage] 기사버튼 OFR 검증 실패 (무시): {resultOfrCmd기사.sErr}");
    //            // OFR 검증 실패는 경고만 출력 (실패해도 진행)
    //        }

    //        // 6. CallCount 핸들 찾기 (접수, 운행, 취소, 완료, 총계)
    //        m_RcptPage.CallCount_hWnd접수 = Std32Window.GetWndHandle_FromRelDrawPt(
    //            m_Main.TopWnd_hWnd, m_FileInfo.접수등록Page_CallCount_ptChkRel접수M
    //        );
    //        if (m_RcptPage.CallCount_hWnd접수 == IntPtr.Zero)
    //        {
    //            return CommonFuncs_StdResult.ErrMsgResult_Error(
    //                $"[{m_Context.AppName}/RcptRegPage]접수CallCount 찾기실패: {m_FileInfo.접수등록Page_CallCount_ptChkRel접수M}",
    //                "InsungsAct_RcptRegPage/InitializeAsync_11", bWrite, bMsgBox);
    //        }
    //        Debug.WriteLine($"[InsungsAct_RcptRegPage] 접수CallCount 찾음: {m_RcptPage.CallCount_hWnd접수:X}");

    //        m_RcptPage.CallCount_hWnd운행 = Std32Window.GetWndHandle_FromRelDrawPt(
    //            m_Main.TopWnd_hWnd, m_FileInfo.접수등록Page_CallCount_ptChkRel운행M
    //        );
    //        if (m_RcptPage.CallCount_hWnd운행 == IntPtr.Zero)
    //        {
    //            return CommonFuncs_StdResult.ErrMsgResult_Error(
    //                $"[{m_Context.AppName}/RcptRegPage]운행CallCount 찾기실패: {m_FileInfo.접수등록Page_CallCount_ptChkRel운행M}",
    //                "InsungsAct_RcptRegPage/InitializeAsync_12", bWrite, bMsgBox);
    //        }
    //        Debug.WriteLine($"[InsungsAct_RcptRegPage] 운행CallCount 찾음: {m_RcptPage.CallCount_hWnd운행:X}");

    //        m_RcptPage.CallCount_hWnd취소 = Std32Window.GetWndHandle_FromRelDrawPt(
    //            m_Main.TopWnd_hWnd, m_FileInfo.접수등록Page_CallCount_ptChkRel취소M
    //        );
    //        if (m_RcptPage.CallCount_hWnd취소 == IntPtr.Zero)
    //        {
    //            return CommonFuncs_StdResult.ErrMsgResult_Error(
    //                $"[{m_Context.AppName}/RcptRegPage]취소CallCount 찾기실패: {m_FileInfo.접수등록Page_CallCount_ptChkRel취소M}",
    //                "InsungsAct_RcptRegPage/InitializeAsync_13", bWrite, bMsgBox);
    //        }
    //        Debug.WriteLine($"[InsungsAct_RcptRegPage] 취소CallCount 찾음: {m_RcptPage.CallCount_hWnd취소:X}");

    //        m_RcptPage.CallCount_hWnd완료 = Std32Window.GetWndHandle_FromRelDrawPt(
    //            m_Main.TopWnd_hWnd, m_FileInfo.접수등록Page_CallCount_ptChkRel완료M
    //        );
    //        if (m_RcptPage.CallCount_hWnd완료 == IntPtr.Zero)
    //        {
    //            return CommonFuncs_StdResult.ErrMsgResult_Error(
    //                $"[{m_Context.AppName}/RcptRegPage]완료CallCount 찾기실패: {m_FileInfo.접수등록Page_CallCount_ptChkRel완료M}",
    //                "InsungsAct_RcptRegPage/InitializeAsync_14", bWrite, bMsgBox);
    //        }
    //        Debug.WriteLine($"[InsungsAct_RcptRegPage] 완료CallCount 찾음: {m_RcptPage.CallCount_hWnd완료:X}");

    //        m_RcptPage.CallCount_hWnd총계 = Std32Window.GetWndHandle_FromRelDrawPt(
    //            m_Main.TopWnd_hWnd, m_FileInfo.접수등록Page_CallCount_ptChkRel총계M
    //        );
    //        if (m_RcptPage.CallCount_hWnd총계 == IntPtr.Zero)
    //        {
    //            return CommonFuncs_StdResult.ErrMsgResult_Error(
    //                $"[{m_Context.AppName}/RcptRegPage]총계CallCount 찾기실패: {m_FileInfo.접수등록Page_CallCount_ptChkRel총계M}",
    //                "InsungsAct_RcptRegPage/InitializeAsync_15", bWrite, bMsgBox);
    //        }
    //        Debug.WriteLine($"[InsungsAct_RcptRegPage] 총계CallCount 찾음: {m_RcptPage.CallCount_hWnd총계:X}");

    //        // 7. 오더 Datagrid 초기화
    //        // 7-1. Datagrid 핸들 찾기
    //        m_RcptPage.DG오더_hWnd = Std32Window.GetWndHandle_FromRelDrawPt(
    //            m_Main.TopWnd_hWnd, m_FileInfo.접수등록Page_DG오더_ptCenterRelM
    //        );
    //        if (m_RcptPage.DG오더_hWnd == IntPtr.Zero)
    //        {
    //            return CommonFuncs_StdResult.ErrMsgResult_Error(
    //                $"[{m_Context.AppName}/RcptRegPage]Datagrid 찾기실패: {m_FileInfo.접수등록Page_DG오더_ptCenterRelM}",
    //                "InsungsAct_RcptRegPage/InitializeAsync_16", bWrite, bMsgBox);
    //        }
    //        Debug.WriteLine($"[InsungsAct_RcptRegPage] Datagrid 찾음: {m_RcptPage.DG오더_hWnd:X}");

    //        // 7-2. Datagrid 크기 확인
    //        m_RcptPage.DG오더_AbsRect = Std32Window.GetWindowRect_DrawAbs(m_RcptPage.DG오더_hWnd);
    //        if (m_RcptPage.DG오더_AbsRect.Width != m_FileInfo.접수등록Page_DG오더_rcRel.Width ||
    //            m_RcptPage.DG오더_AbsRect.Height != m_FileInfo.접수등록Page_DG오더_rcRel.Height)
    //        {
    //            return CommonFuncs_StdResult.ErrMsgResult_Error(
    //                $"[{m_Context.AppName}/RcptRegPage]Datagrid 크기 불일치: " +
    //                $"Expected({m_FileInfo.접수등록Page_DG오더_rcRel.Width}x{m_FileInfo.접수등록Page_DG오더_rcRel.Height}), " +
    //                $"Actual({m_RcptPage.DG오더_AbsRect.Width}x{m_RcptPage.DG오더_AbsRect.Height})",
    //                "InsungsAct_RcptRegPage/InitializeAsync_17", bWrite, bMsgBox);
    //        }
    //        Debug.WriteLine($"[InsungsAct_RcptRegPage] Datagrid 크기 확인 완료: {m_RcptPage.DG오더_AbsRect.Width}x{m_RcptPage.DG오더_AbsRect.Height}");

    //        // 7-3. 수직스크롤바 핸들 찾기 (MainWnd 기준 상대좌표)
    //        m_RcptPage.DG오더_hWnd수직스크롤 = Std32Window.GetWndHandle_FromRelDrawPt(
    //            m_Main.TopWnd_hWnd, m_FileInfo.접수등록Page_DG오더_ptChkRel수직스크롤M
    //        );

    //        if (m_RcptPage.DG오더_hWnd수직스크롤 == IntPtr.Zero)
    //        {
    //            Debug.WriteLine($"[InsungsAct_RcptRegPage] 수직스크롤바 찾기 실패 (경고): {m_FileInfo.접수등록Page_DG오더_ptChkRel수직스크롤M}");
    //            // 스크롤바가 없을 수도 있으므로 경고만 출력하고 진행
    //        }
    //        else
    //        {
    //            Debug.WriteLine($"[InsungsAct_RcptRegPage] 수직스크롤바 찾음: {m_RcptPage.DG오더_hWnd수직스크롤:X}");
    //        }

    //        // 7-4. Datagrid 상세 정보 설정 (컬럼 헤더, RelChildRects)
    //        StdResult_Error resultDG = await SetDG오더RectsAsync(bEdit, bWrite, bMsgBox);
    //        if (resultDG != null)
    //            return resultDG; // 에러 발생 시 반환

    //        Debug.WriteLine($"[InsungsAct_RcptRegPage] 초기화 완료");
    //        return null; // 성공
    //    }
    //    catch (Exception ex)
    //    {
    //        return CommonFuncs_StdResult.ErrMsgResult_Error(
    //            $"[{m_Context.AppName}/RcptRegPage]예외발생: {ex.Message}",
    //            "InsungsAct_RcptRegPage/InitializeAsync_999", bWrite, bMsgBox);
    //    }
    //}
    /// <summary>
    /// Datagrid 상세 영역 설정 (컬럼 헤더 읽기 + RelChildRects 계산 + 상태 검증)
    /// 상태가 올바르지 않으면 InitDG오더Async 호출 후 재시도
    /// </summary>
//     private async Task<StdResult_Error> SetDG오더RectsAsync(bool bEdit = true, bool bWrite = true, bool bMsgBox = true)
//     {
//         Draw.Bitmap bmpDG = null;

//         try
//         {
//             Debug.WriteLine($"[InsungsAct_RcptRegPage] SetDG오더RectsAsync 시작");

//             // 재시도 루프 (goto 대신 for 사용)
//             for (int retry = 0; retry < MAX_DG_INIT_RETRY; retry++)
//             {
//                 // 중간 재시도에서는 메시지박스 표시 안 함, 마지막 재시도에서만 표시
//                 bool bShowMsgBox = (retry >= MAX_DG_INIT_RETRY - 1) && bMsgBox;

//                 if (retry > 0)
//                 {
//                     Debug.WriteLine($"[InsungsAct_RcptRegPage] Datagrid 재시도 {retry}/{MAX_DG_INIT_RETRY - 1}");
//                     await Task.Delay(500); // 재시도 전 대기
//                 }

//             // 1. Datagrid 비트맵 캡처 (MainWnd 기준 상대좌표로 캡처)
//             bmpDG = OfrService.CaptureScreenRect_InWndHandle(
//                 m_Main.TopWnd_hWnd,
//                 m_FileInfo.접수등록Page_DG오더_rcRel);

//             if (bmpDG == null)
//             {
//                 if (retry < MAX_DG_INIT_RETRY - 1)
//                 {
//                     Debug.WriteLine($"[InsungsAct_RcptRegPage] DG오더 캡처 실패 (재시도 {retry + 1}/{MAX_DG_INIT_RETRY})");
//                     await Task.Delay(200);
//                     continue; // 재시도
//                 }
//                 return CommonFuncs_StdResult.ErrMsgResult_Error(
//                     $"[{m_Context.AppName}/RcptRegPage]DG오더 캡처 실패: rcRel={m_FileInfo.접수등록Page_DG오더_rcRel}",
//                     "InsungsAct_RcptRegPage/SetDG오더RectsAsync_01", bWrite, bShowMsgBox);
//             }

//             Debug.WriteLine($"[InsungsAct_RcptRegPage] DG오더 캡처 성공: {bmpDG.Width}x{bmpDG.Height}");

//             // 2. 컬럼 경계 검출
//             // 2-1. 헤더 상단 여백(텍스트 없는 영역)에서 최소 밝기 검출
//             const int headerGab = 7; // 헤더 상단 여백
//             int headerHeight = m_FileInfo.접수등록Page_DG오더_headerHeight;
//             int targetRow = headerGab; // 텍스트가 없는 Y 위치 (경계선만 검출)

//             byte minBrightness = OfrService.GetMinBrightnessAtRow_FromColorBitmapFast(
//                 bmpDG, targetRow);

//             if (minBrightness == 255) // 검출 실패
//             {
//                 bmpDG?.Dispose();
//                 if (retry < MAX_DG_INIT_RETRY - 1)
//                 {
//                     Debug.WriteLine($"[InsungsAct_RcptRegPage] 헤더 행 최소 밝기 검출 실패 (재시도 {retry + 1}/{MAX_DG_INIT_RETRY})");
//                     await Task.Delay(200);
//                     continue; // 재시도
//                 }
//                 return CommonFuncs_StdResult.ErrMsgResult_Error(
//                     $"[{m_Context.AppName}/RcptRegPage]헤더 행 최소 밝기 검출 실패: targetRow={targetRow}",
//                     "InsungsAct_RcptRegPage/SetDG오더RectsAsync_02", bWrite, bShowMsgBox);
//             }

//             minBrightness += 2; // 확실한 경계를 위해 약간 밝게 조정 (백업 파일 방식)

//             Debug.WriteLine($"[InsungsAct_RcptRegPage] 헤더 행 최소 밝기 검출: targetRow={targetRow}, minBrightness={minBrightness}");

//             // 2-2. Bool 배열 생성 (true=검은색, false=흰색)
//             bool[] boolArr = OfrService.GetBoolArray_FromColorBitmapRowFast(
//                 bmpDG, targetRow, minBrightness, 2); // 마진 2픽셀 (백업 파일 방식)

//             if (boolArr == null || boolArr.Length == 0)
//             {
//                 bmpDG?.Dispose();
//                 if (retry < MAX_DG_INIT_RETRY - 1)
//                 {
//                     Debug.WriteLine($"[InsungsAct_RcptRegPage] Bool 배열 생성 실패 (재시도 {retry + 1}/{MAX_DG_INIT_RETRY})");
//                     await Task.Delay(200);
//                     continue; // 재시도
//                 }
//                 return CommonFuncs_StdResult.ErrMsgResult_Error(
//                     $"[{m_Context.AppName}/RcptRegPage]Bool 배열 생성 실패: targetRow={targetRow}",
//                     "InsungsAct_RcptRegPage/SetDG오더RectsAsync_03", bWrite, bShowMsgBox);
//             }

//             Debug.WriteLine($"[InsungsAct_RcptRegPage] Bool 배열 생성 완료: Length={boolArr.Length}");

//             // 2-3. 컬럼 경계 리스트 추출
//             List<OfrModel_LeftWidth> listLW =
//                 OfrService.GetLeftWidthList_FromBool1Array(boolArr, minBrightness);

//             if (listLW == null || listLW.Count == 0)
//             {
//                 bmpDG?.Dispose();
//                 if (retry < MAX_DG_INIT_RETRY - 1)
//                 {
//                     Debug.WriteLine($"[InsungsAct_RcptRegPage] 컬럼 경계 검출 실패 (재시도 {retry + 1}/{MAX_DG_INIT_RETRY})");
//                     await Task.Delay(200);
//                     continue; // 재시도
//                 }
//                 return CommonFuncs_StdResult.ErrMsgResult_Error(
//                     $"[{m_Context.AppName}/RcptRegPage]컬럼 경계 검출 실패: 검출된 리스트 수=0",
//                     "InsungsAct_RcptRegPage/SetDG오더RectsAsync_04", bWrite, bShowMsgBox);
//             }

//             // 첫 번째와 마지막 항목 제거 (테두리 명도 + 오른쪽 끝 경계)
//             if (listLW.Count >= 2)
//             {
//                 listLW.RemoveAt(0); // 첫 번째 제거 (테두리 명도 섞임)
//                 listLW.RemoveAt(listLW.Count - 1); // 마지막 제거 (오른쪽 끝 경계)
//                 Debug.WriteLine($"[InsungsAct_RcptRegPage] 첫/마지막 항목 제거 완료");
//             }

//             int columns = listLW.Count; // 제거 후 남은 개수 = 실제 컬럼 개수

//             Debug.WriteLine($"[InsungsAct_RcptRegPage] 컬럼 경계 검출 완료: 실제 컬럼={columns}");

//             // 컬럼 개수가 20개가 아니면 즉시 InitDG오더Async 호출하여 강제 초기화
//             if (columns != 20)
//             {
//                 Debug.WriteLine($"[InsungsAct_RcptRegPage] 컬럼 개수 불일치: 검출={columns}개, 예상=20개 (재시도 {retry + 1}/{MAX_DG_INIT_RETRY})");

//                 bmpDG?.Dispose();

//                 // InitDG오더Async 호출하여 Datagrid 강제 초기화
//                 StdResult_Error initResult = await InitDG오더Async(
//                     DgValidationIssue.InvalidColumnCount,
//                     bEdit, bWrite,
//                     bMsgBox: false  // 중간 에러는 메시지박스 표시 안 함
//                 );

//                 if (initResult != null)
//                 {
//                     Debug.WriteLine($"[InsungsAct_RcptRegPage] InitDG오더Async 실패: {initResult.sErr}");

//                     // 최대 재시도 횟수 도달 시에만 에러 반환
//                     if (retry >= MAX_DG_INIT_RETRY - 1)
//                     {
//                         return CommonFuncs_StdResult.ErrMsgResult_Error(
//                             $"[{m_Context.AppName}/RcptRegPage]컬럼 개수 불일치: 검출={columns}개, 예상=20개\n상세: {initResult.sErr}\n(재시도 {MAX_DG_INIT_RETRY}회 초과)",
//                             "InsungsAct_RcptRegPage/SetDG오더RectsAsync_05", bWrite, bShowMsgBox);
//                     }
//                 }

//                 await Task.Delay(200);
//                 continue; // 재시도
//             }

//             // 3. 컬럼 헤더 OFR 인식 (전체 20개 컬럼)
//             m_RcptPage.DG오더_ColumnTexts = new string[listLW.Count];

//             for (int i = 0; i < listLW.Count; i++)
//             {
//                 try
//                 {
//                     // 3-1. 컬럼 헤더 영역 Rectangle 생성 (테두리 제외 - 텍스트 영역만)
//                     Draw.Rectangle rcColHeader = new Draw.Rectangle(
//                         listLW[i].nLeft,
//                         headerGab,                                  // 상단 여백만큼 아래
//                         listLW[i].nWidth,
//                         headerHeight - (headerGab * 2)              // 상하 여백 제외
//                     );

//                     //Debug.WriteLine($"[InsungsAct_RcptRegPage] 컬럼[{i}] 헤더 영역: {rcColHeader}");

//                     // 3-2. 컬럼 헤더 비트맵 추출
//                     Draw.Bitmap bmpColHeader = OfrService.GetBitmapInBitmapFast(
//                         bmpDG, rcColHeader
//                     );

//                     if (bmpColHeader == null)
//                     {
//                         //Debug.WriteLine($"[InsungsAct_RcptRegPage] 컬럼[{i}] 비트맵 추출 실패");
//                         m_RcptPage.DG오더_ColumnTexts[i] = null;
//                         continue;
//                     }

//                     // 3-3. 평균 밝기 계산
//                     byte avgBrightness = OfrService.GetAverageBrightness_FromColorBitmapFast(bmpColHeader);

//                     // 3-4. 전경 영역 추출
//                     Draw.Rectangle? rcForeground = OfrService.GetForeGroundDrawRectangle_FromColorBitmapFast(
//                         bmpColHeader, avgBrightness, 0
//                     );

//                     if (rcForeground == null || rcForeground.Value.Width < 1 || rcForeground.Value.Height < 1)
//                     {
//                         //Debug.WriteLine($"[InsungsAct_RcptRegPage] 컬럼[{i}] 전경 영역 추출 실패");
//                         bmpColHeader?.Dispose();
//                         m_RcptPage.DG오더_ColumnTexts[i] = null;
//                         continue;
//                     }

//                     // 3-5. Exact 비트맵 추출
//                     Draw.Bitmap bmpExact = OfrService.GetBitmapInBitmapFast(
//                         bmpColHeader, rcForeground.Value
//                     );
//                     bmpColHeader?.Dispose();

//                     if (bmpExact == null)
//                     {
//                         //Debug.WriteLine($"[InsungsAct_RcptRegPage] 컬럼[{i}] Exact 비트맵 추출 실패");
//                         m_RcptPage.DG오더_ColumnTexts[i] = null;
//                         continue;
//                     }

//                     // 3-6. TEXT OFR 수행 - OfrStr_ComplexCharSetAsync (범용 함수 - 한글/영문/숫자 모두 처리)
//                     // bmpExact는 이미 전경 영역만 추출된 상태, rcSpare는 전체 영역
//                     Draw.Rectangle rcSpare = new Draw.Rectangle(0, 0, bmpExact.Width, bmpExact.Height);

//                     OfrResult_TbCharSetList resultCharSet = await OfrWork_Common.OfrStr_ComplexCharSetAsync(
//                         bmpExact,
//                         rcSpare,
//                         bSaveToTbText: false,  // 컬럼 헤더는 앱마다 달라 저장 안 함
//                         bEdit,
//                         bWrite,
//                         bMsgBox: false         // 에러 메시지 표시 안 함
//                     );

//                     bmpExact?.Dispose();

//                     // 3-7. 결과 저장
//                     if (resultCharSet != null && !string.IsNullOrEmpty(resultCharSet.strResult))
//                     {
//                         m_RcptPage.DG오더_ColumnTexts[i] = resultCharSet.strResult;
//                         //Debug.WriteLine($"[InsungsAct_RcptRegPage] 컬럼[{i}] OFR 성공: '{m_RcptPage.DG오더_ColumnTexts[i]}'");
//                     }
//                     else
//                     {
//                         m_RcptPage.DG오더_ColumnTexts[i] = null;
//                         //Debug.WriteLine($"[InsungsAct_RcptRegPage] 컬럼[{i}] OFR 실패: {resultCharSet?.sErr}");
//                     }
//                 }
//                 catch (Exception ex)
//                 {
//                     //Debug.WriteLine($"[InsungsAct_RcptRegPage] 컬럼[{i}] OFR 예외: {ex.Message}");
//                     m_RcptPage.DG오더_ColumnTexts[i] = null;
//                 }
//             }

//             // 3-8. 컬럼 헤더 OFR 결과 요약
//             int successCount = m_RcptPage.DG오더_ColumnTexts.Count(t => !string.IsNullOrEmpty(t));
//             Debug.WriteLine($"[InsungsAct_RcptRegPage] 컬럼 헤더 OFR 완료: 성공={successCount}/{listLW.Count}");

//             // 3-9. Datagrid 상태 검증
//             DgValidationIssue validationIssues = ValidateDatagridState(m_RcptPage.DG오더_ColumnTexts, listLW);

//             if (validationIssues != DgValidationIssue.None)
//             {
//                 Debug.WriteLine($"[InsungsAct_RcptRegPage] Datagrid 상태 검증 실패: {validationIssues} (재시도 {retry + 1}/{MAX_DG_INIT_RETRY})");

//                 // InitDG오더Async 호출하여 Datagrid 강제 초기화
//                 // 중간 단계에서는 메시지박스 표시 안 함 (조용히 재시도)
//                 StdResult_Error initResult = await InitDG오더Async(
//                     validationIssues,
//                     bEdit, bWrite,
//                     bMsgBox: false  // 중간 에러는 메시지박스 표시 안 함
//                 );

//                 if (initResult != null)
//                 {
//                     Debug.WriteLine($"[InsungsAct_RcptRegPage] InitDG오더Async 실패: {initResult.sErr}");

//                     // 최대 재시도 횟수 도달 시에만 에러 반환 (메시지박스 표시)
//                     if (retry >= MAX_DG_INIT_RETRY - 1)
//                     {
//                         bmpDG?.Dispose();
//                         return CommonFuncs_StdResult.ErrMsgResult_Error(
//                             $"[{m_Context.AppName}/RcptRegPage]Datagrid 초기화 실패: {validationIssues}\n상세: {initResult.sErr}\n(재시도 {MAX_DG_INIT_RETRY}회 초과)",
//                             "InsungsAct_RcptRegPage/SetDG오더RectsAsync_Validation", bWrite, bShowMsgBox);
//                     }
//                 }

//                 bmpDG?.Dispose(); // 재시도 전 비트맵 해제
//                 bmpDG = null;
//                 continue; // 재시도
//             }

//             // 검증 성공 - 계속 진행
//             Debug.WriteLine($"[InsungsAct_RcptRegPage] Datagrid 상태 검증 성공");

//             // 4. RelChildRects 계산
//             // 4-1. 행 정보 계산 (Header + EmptyRow + DataRows)
//             List<Kai.Common.NetDll_WpfCtrl.NetOFR.OfrModel_TopHeight> listTH =
//                 new List<Kai.Common.NetDll_WpfCtrl.NetOFR.OfrModel_TopHeight>();

//             int curRowTop = 0;
//             int dataRowHeight = m_FileInfo.접수등록Page_DG오더_dataRowHeight - 2; // 실제 텍스트 영역 높이 (테두리 제외)

//             // 4-1-1. 헤더 행
//             listTH.Add(new Kai.Common.NetDll_WpfCtrl.NetOFR.OfrModel_TopHeight(
//                 curRowTop + headerGab,
//                 headerHeight - (headerGab * 2)
//             ));
//             Debug.WriteLine($"[InsungsAct_RcptRegPage] 헤더 행 추가: Top={curRowTop + headerGab}, Height={headerHeight - (headerGab * 2)}");

//             // 4-1-2. Empty Row
//             curRowTop += m_FileInfo.접수등록Page_DG오더_headerHeight;
//             listTH.Add(new Kai.Common.NetDll_WpfCtrl.NetOFR.OfrModel_TopHeight(
//                 curRowTop + 1,
//                 dataRowHeight
//             ));
//             Debug.WriteLine($"[InsungsAct_RcptRegPage] Empty Row 추가: Top={curRowTop + 1}, Height={dataRowHeight}");

//             // 4-1-3. Data Rows
//             curRowTop += m_FileInfo.접수등록Page_DG오더_emptyRowHeight;
//             for (int i = 0; i < m_FileInfo.접수등록Page_DG오더_dataRowCount; i++)
//             {
//                 listTH.Add(new Kai.Common.NetDll_WpfCtrl.NetOFR.OfrModel_TopHeight(
//                     curRowTop + 1,
//                     dataRowHeight
//                 ));
//                 curRowTop += m_FileInfo.접수등록Page_DG오더_dataRowHeight;
//             }
//             Debug.WriteLine($"[InsungsAct_RcptRegPage] Data Rows 추가 완료: 총 {m_FileInfo.접수등록Page_DG오더_dataRowCount}개");

//             // 4-2. RelChildRects 2차원 배열 생성 [열, 행]
//             int rows = listTH.Count;
//             m_RcptPage.DG오더_RelChildRects = new Draw.Rectangle[columns, rows];

//             Draw.Rectangle rcDG_Rel = m_FileInfo.접수등록Page_DG오더_rcRel; // MainWnd 기준 상대좌표

//             for (int y = 0; y < rows; y++)
//             {
//                 for (int x = 0; x < columns; x++)
//                 {
//                     if (x == 0)
//                     {
//                         // 첫 번째 컬럼은 약간 다르게 처리 (순번 컬럼)
//                         m_RcptPage.DG오더_RelChildRects[x, y] = new Draw.Rectangle(
//                             listLW[x].nLeft + rcDG_Rel.Left + 1,
//                             listTH[y].nTop + rcDG_Rel.Top,
//                             listLW[x].nWidth - 1,
//                             listTH[y].nHeight
//                         );
//                     }
//                     else
//                     {
//                         m_RcptPage.DG오더_RelChildRects[x, y] = new Draw.Rectangle(
//                             listLW[x].nLeft + rcDG_Rel.Left,
//                             listTH[y].nTop + rcDG_Rel.Top,
//                             listLW[x].nWidth,
//                             listTH[y].nHeight
//                         );
//                     }
//                 }
//             }

//             Debug.WriteLine($"[InsungsAct_RcptRegPage] RelChildRects 생성 완료: {columns}열 x {rows}행");

//             // 4-3. Background Brightness 계산 (첫 번째 데이터 행의 한 점에서 측정)
//             if (rows >= 2)
//             {
//                 // Empty Row의 샘플 포인트 (비트맵 기준 상대 좌표)
//                 Draw.Point ptSampleRel = new Draw.Point(
//                     m_RcptPage.DG오더_RelChildRects[0, 1].Left - rcDG_Rel.Left + 3,
//                     m_RcptPage.DG오더_RelChildRects[0, 1].Top - rcDG_Rel.Top + 3
//                 );

//                 m_RcptPage.DG오더_nBackgroundBright =
//                     OfrService.GetPixelBrightness(bmpDG, ptSampleRel);

//                 Debug.WriteLine($"[InsungsAct_RcptRegPage] Background Brightness: {m_RcptPage.DG오더_nBackgroundBright} (샘플 위치: {ptSampleRel})");
//             }

//             // 모든 처리 완료 - 루프 탈출
//             Debug.WriteLine($"[InsungsAct_RcptRegPage] SetDG오더RectsAsync 완료");
//             break;

//             } // for (retry) 끝

//             // 성공
//             return null;
//         }
//         catch (Exception ex)
//         {
//             return CommonFuncs_StdResult.ErrMsgResult_Error(
//                 $"[{m_Context.AppName}/RcptRegPage]SetDG오더RectsAsync 예외발생: {ex.Message}",
//                 "InsungsAct_RcptRegPage/SetDG오더RectsAsync_999", bWrite, bMsgBox);
//         }
//         finally
//         {
//             // 리소스 정리
//             bmpDG?.Dispose();
//         }
//     }
    #endregion

    #region Datagrid Initialization
    /// <summary>
    /// Datagrid 강제 초기화 (Context 메뉴 → "접수화면초기화" 클릭 → 컬럼 조정)
    /// </summary>
    /// <param name="issues">검증 이슈 플래그</param>
    /// <param name="bEdit">편집 허용 여부</param>
    /// <param name="bWrite">로그 작성 여부</param>
    /// <param name="bMsgBox">메시지박스 표시 여부</param>
    /// <returns>에러 발생 시 StdResult_Error, 성공 시 null</returns>
//     private async Task<StdResult_Error> InitDG오더Async(
//         DgValidationIssue issues,
//         bool bEdit = true,
//         bool bWrite = true,
//         bool bMsgBox = true)
//     {
//         // 마우스 커서 위치 백업 (작업 완료 후 복원용)
//         Draw.Point ptCursorBackup = Std32Cursor.GetCursorPos_AbsDrawPt();

//         try
//         {
//             // 초기화 전체 기간 동안 외부 입력 차단
//             Simulation_Mouse.SafeBlockInputStart();

//             // DG오더_hWnd 기준으로 헤더 영역 정의
//             Draw.Rectangle rcDG = Std32Window.GetWindowRect_DrawAbs(m_RcptPage.DG오더_hWnd);
//             Draw.Rectangle rcHeader = new Draw.Rectangle(
//                 0, 0,  // DG 윈도우 내부 시작점
//                 rcDG.Width,
//                 m_FileInfo.접수등록Page_DG오더_headerHeight
//             );
//             //Debug.WriteLine($"[InitDG오더] rcHeader: {rcHeader}");

//             // Step 1: 사전작업 - "접수화면초기화" 클릭
//             Debug.WriteLine("[InitDG오더] Step 1: 접수화면초기화 시작");

//             // 1-1. 우클릭
//             await Std32Mouse_Post.MousePostAsync_ClickRight(m_RcptPage.DG오더_hWnd);
//             //Debug.WriteLine("[InitDG오더] 1-1. 우클릭 완료");

//             // 1-2. Context 메뉴 대기 (100회 폴링, 2초)
//             IntPtr hWndMenu = IntPtr.Zero;
//             for (int i = 0; i < 100; i++)
//             {
//                 await Task.Delay(20);
//                 hWndMenu = Std32Window.FindMainWindow_StartsWith(
//                     m_FileInfo.Main_AnyMenu_sClassName,
//                     m_FileInfo.Main_AnyMenu_sWndName);
//                 if (hWndMenu != IntPtr.Zero) break;
//             }

//             if (hWndMenu == IntPtr.Zero)
//             {
//                 return CommonFuncs_StdResult.ErrMsgResult_Error(
//                     "[InitDG오더]Context 메뉴 찾기 실패",
//                     "InsungsAct_RcptRegPage/InitDG오더Async_01", bWrite, true);
//             }

//             //Debug.WriteLine($"[InitDG오더] 1-2. Context 메뉴 찾음: {hWndMenu:X}");

//             // 1-3. "접수화면초기화" 메뉴 클릭 (2개 서브메뉴 중 위쪽, 좌클릭)
//             await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndMenu, 10, 12);
//             //Debug.WriteLine("[InitDG오더] 1-3. 접수화면초기화 메뉴 클릭 완료");

//             // 1-4. 확인 다이얼로그 대기 (10회 폴링, 1초)
//             IntPtr hWndDialog = IntPtr.Zero;
//             for (int i = 0; i < 10; i++)
//             {
//                 await Task.Delay(100);
//                 hWndDialog = Std32Window.FindWindow("#32770", "확인");
//                 if (hWndDialog != IntPtr.Zero)
//                 {
//                     // "예(&Y)" 버튼 찾기
//                     IntPtr hWndBtn = Std32Window.FindWindowEx(
//                         hWndDialog, IntPtr.Zero, "Button", "예(&Y)");
//                     if (hWndBtn != IntPtr.Zero)
//                     {
//                         //Debug.WriteLine("[InitDG오더] 1-4. '예' 버튼 클릭");
//                         // 10회 재시도 (기존 로직과 동일)
//                         for (int j = 0; j < 10; j++)
//                         {
//                             await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndBtn);
//                             await Task.Delay(50);

//                             // 다이얼로그가 사라졌는지 확인
//                             if (Std32Window.FindWindow("#32770", "확인") == IntPtr.Zero)
//                                 break;
//                         }
//                         break;
//                     }
//                     else
//                     {
//                         return CommonFuncs_StdResult.ErrMsgResult_Error(
//                             "[InitDG오더]'예' 버튼 찾기 실패",
//                             "InsungsAct_RcptRegPage/InitDG오더Async_02",
//                             bWrite, true);
//                     }
//                 }
//             }

//             if (hWndDialog == IntPtr.Zero)
//             {
//                 return CommonFuncs_StdResult.ErrMsgResult_Error(
//                     "[InitDG오더]확인 다이얼로그 찾기 실패",
//                     "InsungsAct_RcptRegPage/InitDG오더Async_03", bWrite, true);
//             }

//             // 1-5. 확인 다이얼로그 사라질 때까지 대기 (10회 폴링, 1초)
//             for (int i = 0; i < 10; i++)
//             {
//                 await Task.Delay(100);
//                 hWndDialog = Std32Window.FindWindow("#32770", "확인");
//                 if (hWndDialog == IntPtr.Zero) break;
//             }

//             if (hWndDialog != IntPtr.Zero)
//             {
//                 return CommonFuncs_StdResult.ErrMsgResult_Error(
//                     "[InitDG오더]확인 다이얼로그 사라지기 실패",
//                     "InsungsAct_RcptRegPage/InitDG오더Async_04", bWrite, true);
//             }

//             //Debug.WriteLine("[InitDG오더] 1-5. 접수화면초기화 완료");

//             // 1-6. 초기화 반영 대기
//             await Task.Delay(500);

//             // Step 2: 불필요한 컬럼을 우측으로 이동 (15회 반복)
//             Debug.WriteLine("[InitDG오더] Step 2: 불필요한 컬럼 우측 이동 시작");

//             const int gab = 7;      // 헤더에서 텍스트 추출할 y 위치
//             const int height = 18;  // 헤더 텍스트 높이

//             for (int iteration = 0; iteration < 15; iteration++)
//             {
//                 //Debug.WriteLine($"[InitDG오더] 2-{iteration+1}. 불필요 컬럼 검사 (반복 {iteration+1}/15)");

//                 // 2-1. 헤더 캡처
//                 Draw.Bitmap bmpHeader = OfrService.CaptureScreenRect_InWndHandle(
//                     m_RcptPage.DG오더_hWnd, rcHeader);
//                 if (bmpHeader == null)
//                 {
//                     return CommonFuncs_StdResult.ErrMsgResult_Error(
//                         $"[InitDG오더]헤더 캡처 실패 (반복 {iteration+1})",
//                         "InsungsAct_RcptRegPage/InitDG오더Async_05", bWrite, true);
//                 }

//                 // 2-2. 컬럼 경계 검출
//                 byte byteMinBrightness = OfrService.GetMinBrightnessAtRow_FromColorBitmapFast(
//                     bmpHeader, gab);
//                 byteMinBrightness += 2; // 확실한 경계를 위해 +2

//                 bool[] resultArr = OfrService.GetBoolArray_FromColorBitmapRowFast(
//                     bmpHeader, gab, byteMinBrightness, 2);
//                 List<OfrModel_LeftWidth> listLW = OfrService.GetLeftWidthList_FromBool1Array(
//                     resultArr, byteMinBrightness);
//                 int columns = listLW.Count - 1; // 마지막 컬럼은 잘리므로 제외

//                 //Debug.WriteLine($"[InitDG오더] 2-{iteration+1}. 검출된 컬럼 개수: {columns}");

//                 // 2-3. 각 컬럼 텍스트 인식 (SetDG오더RectsAsync 방식 사용)
//                 string[] texts = new string[columns];
//                 Draw.Rectangle[] rcHeaders = new Draw.Rectangle[columns];

//                 for (int x = 0; x < columns; x++)
//                 {
//                     // 컬럼 헤더 영역 (상하 여백 제외)
//                     rcHeaders[x] = new Draw.Rectangle(
//                         listLW[x].nLeft, gab, listLW[x].nWidth, height);
//                     Draw.Rectangle rcColHeader = rcHeaders[x];

//                     // 비트맵 추출
//                     Draw.Bitmap bmpColHeader = OfrService.GetBitmapInBitmapFast(
//                         bmpHeader, rcColHeader);

//                     if (bmpColHeader == null)
//                     {
//                         texts[x] = null;
//                         continue;
//                     }

//                     // 평균 밝기
//                     byte avgBrightness = OfrService.GetAverageBrightness_FromColorBitmapFast(
//                         bmpColHeader);

//                     // 전경 영역 추출
//                     Draw.Rectangle? rcForeground = OfrService.GetForeGroundDrawRectangle_FromColorBitmapFast(
//                         bmpColHeader, avgBrightness, 0);

//                     if (rcForeground == null || rcForeground.Value.Width < 1)
//                     {
//                         bmpColHeader?.Dispose();
//                         texts[x] = null;
//                         continue;
//                     }

//                     // Exact 비트맵 추출
//                     Draw.Bitmap bmpExact = OfrService.GetBitmapInBitmapFast(
//                         bmpColHeader, rcForeground.Value);
//                     bmpColHeader?.Dispose();

//                     if (bmpExact == null)
//                     {
//                         texts[x] = null;
//                         continue;
//                     }

//                     // OFR 수행
//                     Draw.Rectangle rcSpare = new Draw.Rectangle(0, 0, bmpExact.Width, bmpExact.Height);
//                     OfrResult_TbCharSetList resultCharSet = await OfrWork_Common.OfrStr_ComplexCharSetAsync(
//                         bmpExact, rcSpare, bSaveToTbText: false, bEdit, bWrite, bMsgBox: false);

//                     bmpExact?.Dispose();

//                     texts[x] = resultCharSet?.strResult;
//                     //Debug.WriteLine($"[InitDG오더] 2-{iteration+1}. 컬럼[{x}] 인식: '{texts[x]}'");
//                 }

//                 // 2-4. 우측에서 좌측으로 검사하여 불필요한 컬럼 이동
//                 int workCount = 0;
//                 for (int x = columns - 1; x >= 0; x--)
//                 {
//                     // m_ReceiptDgHeaderInfos에 존재하는지 확인
//                     int index = m_ReceiptDgHeaderInfos
//                         .Select((value, idx) => new { value, idx })
//                         .Where(z => z.value.sName == texts[x])
//                         .Select(z => z.idx)
//                         .DefaultIfEmpty(-1)
//                         .First();

//                     if (index < 0) // 필요없는 컬럼 발견
//                     {
//                         workCount++;
//                         //Debug.WriteLine(
//                         //    $"[InitDG오더] 2-{iteration+1}. 불필요 컬럼 발견: [{x}]{texts[x]} → 우측 이동");

//                         // 수직 드래그로 우측 이동 (-50픽셀, 위로 드래그)
//                         Draw.Point ptCenter = StdUtil.GetCenterDrawPoint(rcHeaders[x]);
//                         await Simulation_Mouse.SafeMouseEvent_DragLeft_Smooth_VerticalAsync(
//                             m_RcptPage.DG오더_hWnd, ptCenter, -50, false);

//                         await Task.Delay(50);
//                     }
//                 }

//                 bmpHeader.Dispose();

//                 // 2-5. 이동한 컬럼이 없으면 종료
//                 if (workCount == 0)
//                 {
//                     //Debug.WriteLine($"[InitDG오더] 2-{iteration+1}. 이동할 컬럼 없음, Step 2 완료");
//                     break;
//                 }

//                 //Debug.WriteLine($"[InitDG오더] 2-{iteration+1}. {workCount}개 컬럼 이동 완료");
//             }

//             Debug.WriteLine("[InitDG오더] Step 2 완료");

//             // Step 3: 컬럼 순서 조정
//             Debug.WriteLine("[InitDG오더] Step 3: 컬럼 순서 조정 시작");

//             for (int x = 0; x < m_ReceiptDgHeaderInfos.Length; x++)
//             {
//                 //Debug.WriteLine($"[InitDG오더] 3-{x+1}. 목표 컬럼: [{x}]{m_ReceiptDgHeaderInfos[x].sName}");

//                 // 3-1. 헤더 캡처
//                 Draw.Bitmap bmpHeader = OfrService.CaptureScreenRect_InWndHandle(
//                     m_RcptPage.DG오더_hWnd, rcHeader);
//                 if (bmpHeader == null)
//                 {
//                     return CommonFuncs_StdResult.ErrMsgResult_Error(
//                         $"[InitDG오더]Step 3 헤더 캡처 실패 (컬럼 {x})",
//                         "InsungsAct_RcptRegPage/InitDG오더Async_07", bWrite, true);
//                 }

//                 // 3-2. 컬럼 경계 검출
//                 byte byteMinBrightness = OfrService.GetMinBrightnessAtRow_FromColorBitmapFast(
//                     bmpHeader, gab);
//                 byteMinBrightness += 2;

//                 bool[] resultArr = OfrService.GetBoolArray_FromColorBitmapRowFast(
//                     bmpHeader, gab, byteMinBrightness, 2);
//                 List<OfrModel_LeftWidth> listLW = OfrService.GetLeftWidthList_FromBool1Array(
//                     resultArr, byteMinBrightness);
//                 int columns = listLW.Count - 1;

//                 // 3-3. 목표 컬럼 텍스트 찾기
//                 string targetText = m_ReceiptDgHeaderInfos[x].sName;
//                 int index = -1;

//                 for (int tx = 0; tx < columns; tx++)
//                 {
//                     Draw.Rectangle rcColHeader = new Draw.Rectangle(
//                         listLW[tx].nLeft, gab, listLW[tx].nWidth, height);

//                     // OFR 인식 (Step 2-3과 동일)
//                     Draw.Bitmap bmpColHeader = OfrService.GetBitmapInBitmapFast(
//                         bmpHeader, rcColHeader);
//                     if (bmpColHeader == null) continue;

//                     byte avgBrightness = OfrService.GetAverageBrightness_FromColorBitmapFast(
//                         bmpColHeader);
//                     Draw.Rectangle? rcForeground = OfrService.GetForeGroundDrawRectangle_FromColorBitmapFast(
//                         bmpColHeader, avgBrightness, 0);

//                     if (rcForeground == null || rcForeground.Value.Width < 1)
//                     {
//                         bmpColHeader?.Dispose();
//                         continue;
//                     }

//                     Draw.Bitmap bmpExact = OfrService.GetBitmapInBitmapFast(
//                         bmpColHeader, rcForeground.Value);
//                     bmpColHeader?.Dispose();

//                     if (bmpExact == null) continue;

//                     Draw.Rectangle rcSpare = new Draw.Rectangle(0, 0, bmpExact.Width, bmpExact.Height);
//                     OfrResult_TbCharSetList resultCharSet = await OfrWork_Common.OfrStr_ComplexCharSetAsync(
//                         bmpExact, rcSpare, bSaveToTbText: false, bEdit, bWrite, bMsgBox: false);

//                     bmpExact?.Dispose();

//                     if (resultCharSet?.strResult == targetText)
//                     {
//                         index = tx;
//                         //Debug.WriteLine($"[InitDG오더] 3-{x+1}. 찾음: '{targetText}' at [{tx}]");
//                         break;
//                     }
//                 }

//                 bmpHeader.Dispose();

//                 // 3-4. 에러 체크
//                 if (index < 0)
//                 {
//                     return CommonFuncs_StdResult.ErrMsgResult_Error(
//                         $"[InitDG오더]목표 컬럼 '{targetText}' 찾기 실패",
//                         "InsungsAct_RcptRegPage/InitDG오더Async_08", bWrite, true);
//                 }

//                 // 3-5. 컬럼 순서가 틀리면 드래그로 이동
//                 if (index != x)
//                 {
//                     //Debug.WriteLine($"[InitDG오더] 3-{x+1}. 이동: [{index}] → [{x}]");

//                     Draw.Rectangle rcStart = new Draw.Rectangle(
//                         listLW[index].nLeft, gab, listLW[index].nWidth, height);
//                     Draw.Rectangle rcTarget = new Draw.Rectangle(
//                         listLW[x].nLeft, gab, listLW[x].nWidth, height);

//                     Draw.Point ptStart = StdUtil.GetCenterDrawPoint(rcStart);
//                     Draw.Point ptTarget = new Draw.Point(rcTarget.Left, ptStart.Y);

//                     await Simulation_Mouse.SafeMouseEvent_DragLeft_SmoothAsync(
//                         m_RcptPage.DG오더_hWnd, ptStart, ptTarget, bBkCursor: false, nMiliSec: 150);

//                     await Task.Delay(150);
//                 }
//             }

//             Debug.WriteLine("[InitDG오더] Step 3 완료");

//             // Step 4: 컬럼 너비 조정
//             Debug.WriteLine("[InitDG오더] Step 4: 컬럼 너비 조정 시작");

//             for (int x = 0; x < m_ReceiptDgHeaderInfos.Length; x++)
//             {
//                 //Debug.WriteLine($"[InitDG오더] 4-{x+1}. 컬럼 너비 조정: [{x}]{m_ReceiptDgHeaderInfos[x].sName}");

//                 // 4-1. 헤더 캡처
//                 Draw.Bitmap bmpHeader = OfrService.CaptureScreenRect_InWndHandle(
//                     m_RcptPage.DG오더_hWnd, rcHeader);
//                 if (bmpHeader == null)
//                 {
//                     return CommonFuncs_StdResult.ErrMsgResult_Error(
//                         $"[InitDG오더]Step 4 헤더 캡처 실패 (컬럼 {x})",
//                         "InsungsAct_RcptRegPage/InitDG오더Async_09", bWrite, true);
//                 }

//                 // 4-2. 컬럼 경계 검출
//                 byte byteMinBrightness = OfrService.GetMinBrightnessAtRow_FromColorBitmapFast(
//                     bmpHeader, gab);
//                 byteMinBrightness += 2;

//                 bool[] resultArr = OfrService.GetBoolArray_FromColorBitmapRowFast(
//                     bmpHeader, gab, byteMinBrightness, 2);
//                 List<OfrModel_LeftWidth> listLW = OfrService.GetLeftWidthList_FromBool1Array(
//                     resultArr, byteMinBrightness);

//                 bmpHeader.Dispose();

//                 // 4-3. 현재 너비와 목표 너비 비교
//                 int currentWidth = listLW[x].nWidth;
//                 int targetWidth = m_ReceiptDgHeaderInfos[x].nWidth;
//                 int dx = targetWidth - currentWidth;

//                 if (dx == 0)
//                 {
//                     //Debug.WriteLine($"[InitDG오더] 4-{x+1}. 너비 일치: {currentWidth}px");
//                     continue; // 이미 원하는 너비
//                 }

//                 //Debug.WriteLine($"[InitDG오더] 4-{x+1}. 너비 조정: {currentWidth}px → {targetWidth}px (dx={dx})");

//                 // 4-4. 컬럼 오른쪽 경계를 dx만큼 드래그
//                 Draw.Point ptStart = new Draw.Point(listLW[x]._nRight + 1, gab);
//                 Draw.Point ptTarget = new Draw.Point(ptStart.X + dx, ptStart.Y);

//                 await Simulation_Mouse.SafeMouseEvent_DragLeft_SmoothAsync(
//                     m_RcptPage.DG오더_hWnd, ptStart, ptTarget, bBkCursor: false, nMiliSec: 150);

//                 await Task.Delay(150);
//             }

//             Debug.WriteLine("[InitDG오더] Step 4 완료");

//             return null; // 성공
//         }
//         catch (Exception ex)
//         {
//             return CommonFuncs_StdResult.ErrMsgResult_Error(
//                 $"[InitDG오더]예외발생: {ex.Message}",
//                 "InsungsAct_RcptRegPage/InitDG오더Async_999", bWrite, true);
//         }
//         finally
//         {
//             // 외부 입력 차단 강제 해제 (예외 발생 시에도 보장, 중첩 카운터 무시)
//             Simulation_Mouse.SafeBlockInputForceStop();

//             // 마우스 커서 위치 복원
//             Std32Cursor.SetCursorPos_AbsDrawPt(ptCursorBackup);
//             //Debug.WriteLine("[InitDG오더] 커서 위치 복원 완료");
//         }

//         #region 주석처리 - 단계별 확인 중
//         /*
//         Draw.Bitmap bmpHeader = null;
//         Draw.Point ptCursorBackup = Std32Cursor.GetCursorPos_AbsDrawPt(); // 커서 위치 백업
//         bool initSuccess = false;

//         try
//         {
//             Debug.WriteLine($"[InitDG오더Async] Datagrid 강제 초기화 시작: issues={issues}");

//             // 1. 접수화면초기화 시도 (최대 3회 재시도)
//             const int MAX_INIT_RETRY = 3;
//             for (int initRetry = 0; initRetry < MAX_INIT_RETRY; initRetry++)
//             {
//                 if (initRetry > 0)
//                 {
//                     Debug.WriteLine($"[InitDG오더Async] 접수화면초기화 재시도 {initRetry + 1}/{MAX_INIT_RETRY}");
//                     await Task.Delay(300); // 재시도 전 대기
//                 }

//                 // 1-1. Context 메뉴 → "접수화면초기화" 클릭
//                 await Std32Mouse_Post.MousePostAsync_ClickRight(m_RcptPage.DG오더_hWnd);
//                 await Task.Delay(100);

//                 // Context 메뉴 대기 (원하는 결과가 나올 때까지 폴링)
//                 IntPtr hWndMenu = IntPtr.Zero;
//                 for (int i = 0; i < 100; i++) // 2초 대기
//                 {
//                     await Task.Delay(20);
//                     hWndMenu = Std32Window.FindMainWindow_StartsWith(
//                         m_FileInfo.Main_AnyMenu_sClassName,
//                         m_FileInfo.Main_AnyMenu_sWndName
//                     );
//                     if (hWndMenu != IntPtr.Zero) break;
//                 }

//                 if (hWndMenu == IntPtr.Zero)
//                 {
//                     Debug.WriteLine($"[InitDG오더Async] Context 메뉴 찾기 실패 (시도 {initRetry + 1}/{MAX_INIT_RETRY})");
//                     continue; // 다시 우클릭부터 재시도
//                 }

//                 Debug.WriteLine($"[InitDG오더Async] Context 메뉴 찾음: {hWndMenu:X}");

//                 // "접수화면초기화" 메뉴 클릭 (2개 서브메뉴 중 위쪽)
//                 await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndMenu, 10, 12);
//                 await Task.Delay(100);

//                 // 확인 다이얼로그 대기 (원하는 결과가 나올 때까지 폴링)
//                 IntPtr hWndDialog = IntPtr.Zero;
//                 bool dialogHandled = false;

//                 for (int i = 0; i < 10; i++) // 1초 대기
//                 {
//                     await Task.Delay(100);
//                     hWndDialog = Std32Window.FindWindow("#32770", "확인");
//                     if (hWndDialog != IntPtr.Zero)
//                     {
//                         // "예(&Y)" 버튼 찾기
//                         IntPtr hWndBtn = Std32Window.FindWindowEx(hWndDialog, IntPtr.Zero, "Button", "예(&Y)");
//                         if (hWndBtn != IntPtr.Zero)
//                         {
//                             Debug.WriteLine($"[InitDG오더Async] 확인 다이얼로그 '예' 버튼 클릭");
//                             await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndBtn);
//                             dialogHandled = true;
//                             break;
//                         }
//                     }
//                 }

//                 if (!dialogHandled)
//                 {
//                     Debug.WriteLine($"[InitDG오더Async] 확인 다이얼로그 처리 실패 (시도 {initRetry + 1}/{MAX_INIT_RETRY})");
//                     continue; // 다시 우클릭부터 재시도
//                 }

//                 // 확인 다이얼로그 사라질 때까지 대기 (원하는 결과가 나올 때까지 폴링)
//                 for (int i = 0; i < 10; i++) // 1초 대기
//                 {
//                     await Task.Delay(100);
//                     hWndDialog = Std32Window.FindWindow("#32770", "확인");
//                     if (hWndDialog == IntPtr.Zero) break;
//                 }

//                 if (hWndDialog != IntPtr.Zero)
//                 {
//                     Debug.WriteLine($"[InitDG오더Async] 확인 다이얼로그가 사라지지 않음 (시도 {initRetry + 1}/{MAX_INIT_RETRY})");
//                     continue; // 다시 우클릭부터 재시도
//                 }

//                 Debug.WriteLine($"[InitDG오더Async] 접수화면초기화 완료");
//                 initSuccess = true;
//                 await Task.Delay(500); // 초기화 반영 대기
//                 break; // 성공 시 탈출
//             }

//             // 접수화면초기화 실패 시 경고만 출력 (컬럼 조정은 시도)
//             if (!initSuccess)
//             {
//                 Debug.WriteLine($"[InitDG오더Async] 경고: 접수화면초기화 {MAX_INIT_RETRY}회 실패 - 컬럼 조정만 시도");
//             }

//             // 2. 컬럼 조정 (WrongOrder 또는 WrongWidth 이슈가 있을 때만)
//             if ((issues & DgValidationIssue.WrongOrder) != 0 ||
//                 (issues & DgValidationIssue.WrongWidth) != 0)
//             {
//                 Debug.WriteLine($"[InitDG오더Async] 컬럼 조정 시작");

//                 // Datagrid 절대좌표
//                 Draw.Point ptDgTopLeft = new Draw.Point(
//                     m_RcptPage.DG오더_AbsRect.Left,
//                     m_RcptPage.DG오더_AbsRect.Top
//                 );

//                 // m_ReceiptDgHeaderInfos.Length만큼 반복 (20개 컬럼)
//                 for (int repeatIdx = 0; repeatIdx < m_ReceiptDgHeaderInfos.Length; repeatIdx++)
//                 {
//                     // 2-1. 헤더 캡처
//                     bmpHeader = OfrService.CaptureScreenRect_InWndHandle(
//                         m_Main.TopWnd_hWnd,
//                         rcHeader
//                     );

//                     if (bmpHeader == null)
//                     {
//                         Debug.WriteLine($"[InitDG오더Async] 경고: 헤더 캡처 실패 (repeat={repeatIdx}) - 다음 반복 시도");
//                         continue; // return 하지 않고 다음 반복
//                     }

//                     // 2-2. 컬럼 경계 검출
//                     byte minBrightness = OfrService.GetMinBrightnessAtRow_FromColorBitmapFast(
//                         bmpHeader, HEADER_GAB
//                     );
//                     minBrightness += 2; // 확실한 경계를 위해

//                     bool[] boolArr = OfrService.GetBoolArray_FromColorBitmapRowFast(
//                         bmpHeader, HEADER_GAB, minBrightness, 2
//                     );

//                     List<OfrModel_LeftWidth> listLW =
//                         OfrService.GetLeftWidthList_FromBool1Array(boolArr, minBrightness);

//                     if (listLW == null || listLW.Count == 0)
//                     {
//                         bmpHeader?.Dispose();
//                         continue; // 다음 반복
//                     }

//                     int columns = listLW.Count - 1; // 오른쪽 끝 경계 제외
//                     if (columns <= 0)
//                     {
//                         bmpHeader?.Dispose();
//                         continue;
//                     }

//                     // 2-3. 컬럼 헤더 OFR
//                     string[] columnTexts = new string[columns];
//                     Draw.Rectangle[] rcHeaders = new Draw.Rectangle[columns];

//                     for (int x = 0; x < columns; x++)
//                     {
//                         rcHeaders[x] = new Draw.Rectangle(
//                             listLW[x].nLeft,
//                             HEADER_GAB,
//                             listLW[x].nWidth,
//                             HEADER_TEXT_HEIGHT
//                         );

//                         // OFR 수행
//                         OfrResult_TbCharSetList resultChSet = await OfrWork_Common.OfrStr_ComplexCharSetAsync(
//                             bmpHeader,
//                             rcHeaders[x],
//                             bSaveToTbText: false,
//                             bEdit,
//                             bWrite,
//                             bMsgBox: false
//                         );

//                         if (resultChSet != null && !string.IsNullOrEmpty(resultChSet.strResult))
//                         {
//                             columnTexts[x] = resultChSet.strResult;
//                         }
//                         else
//                         {
//                             columnTexts[x] = "";
//                         }
//                     }

//                     bmpHeader?.Dispose();
//                     bmpHeader = null;

//                     // 2-4. 각 컬럼 위치 확인 및 조정
//                     for (int x = 0; x < columns && x < m_ReceiptDgHeaderInfos.Length; x++)
//                     {
//                         string targetText = m_ReceiptDgHeaderInfos[x].sName;

//                         // 현재 컬럼 찾기
//                         int index = Array.FindIndex(columnTexts, t => t == targetText);

//                         if (index < 0)
//                         {
//                             Debug.WriteLine($"[InitDG오더Async] 컬럼[{x}] '{targetText}' 찾기 실패");
//                             continue; // 다음 컬럼
//                         }

//                         // 컬럼 순서가 틀리면 드래그로 이동
//                         if (index != x)
//                         {
//                             Debug.WriteLine($"[InitDG오더Async] 컬럼[{x}] '{targetText}' 순서 조정: {index} → {x}");

//                             Draw.Point ptStart = StdUtil.GetCenterDrawPoint(rcHeaders[index]);
//                             Draw.Point ptEnd = new Draw.Point(rcHeaders[x].Left, ptStart.Y);

//                             // 절대좌표로 변환
//                             Draw.Point ptStartAbs = new Draw.Point(
//                                 ptDgTopLeft.X + rcHeader.Left + ptStart.X,
//                                 ptDgTopLeft.Y + rcHeader.Top + ptStart.Y
//                             );
//                             Draw.Point ptEndAbs = new Draw.Point(
//                                 ptDgTopLeft.X + rcHeader.Left + ptEnd.X,
//                                 ptDgTopLeft.Y + rcHeader.Top + ptEnd.Y
//                             );

//                             // Drag 수행 (Simulation.cs:457-478 참고, 라이브러리 조합 방식)
//                             Std32Cursor.SetCursorPos_AbsDrawPt(ptStartAbs);
//                             Std32Mouse_Event.MouseEvent_LeftBtnDown();
//                             Std32Mouse_Send.MouseSet_MoveSmooth_ptAbs(ptStartAbs, ptEndAbs, 150);
//                             Std32Mouse_Event.MouseEvent_LeftBtnUp();
//                             await Task.Delay(150);
//                         }
//                     }

//                     // 2-5. 컬럼 너비 조정
//                     for (int x = 0; x < columns && x < m_ReceiptDgHeaderInfos.Length; x++)
//                     {
//                         StdResult_Error adjustResult = AdjustColumnWidth(rcHeader, ptDgTopLeft, x);
//                         if (adjustResult != null)
//                         {
//                             Debug.WriteLine($"[InitDG오더Async] 컬럼[{x}] 너비 조정 실패: {adjustResult.sErr}");
//                             // 실패해도 계속 진행
//                         }

//                         await Task.Delay(150);
//                     }

//                     // 모든 컬럼 처리 완료 - 루프 탈출
//                     Debug.WriteLine($"[InitDG오더Async] 컬럼 조정 완료");
//                     break;
//                 }
//             }

//             Debug.WriteLine($"[InitDG오더Async] Datagrid 강제 초기화 완료");
//             return null; // 성공
//         }
//         catch (Exception ex)
//         {
//             return CommonFuncs_StdResult.ErrMsgResult_Error(
//                 $"[{m_Context.AppName}/InitDG오더]예외발생: {ex.Message}",
//                 "InsungsAct_RcptRegPage/InitDG오더Async_999", bWrite, bMsgBox);
//         }
//         finally
//         {
//             bmpHeader?.Dispose();
//             Std32Cursor.SetCursorPos_AbsDrawPt(ptCursorBackup); // 커서 원위치
//         }
//         */
//         #endregion
//     }
    #endregion

    #region Utility Methods
    /// <summary>
    /// 접수등록 페이지가 초기화되었는지 확인 (간단 체크)
    /// </summary>
    public bool IsInitialized()
    {
        // 핵심 핸들 체크
        if (m_RcptPage.TopWnd_hWnd == IntPtr.Zero) return false;
        if (m_RcptPage.DG오더_hWnd == IntPtr.Zero) return false;

        // 핵심 데이터 구조 체크
        if (m_RcptPage.DG오더_RelChildRects == null) return false;
        if (m_RcptPage.DG오더_ColumnTexts == null) return false;

        return true;
    }

    /// <summary>
    /// Datagrid 상태 검증 (컬럼 개수, 순서, 너비 확인)
    /// </summary>
    /// <param name="columnTexts">현재 읽은 컬럼 헤더 텍스트 배열</param>
    /// <param name="listLW">컬럼 Left/Width 리스트</param>
    /// <returns>검증 이슈 플래그 (None이면 정상)</returns>
//     private DgValidationIssue ValidateDatagridState(
//         string[] columnTexts,
//         List<OfrModel_LeftWidth> listLW)
//     {
//         DgValidationIssue issues = DgValidationIssue.None;

//         // 1. 컬럼 개수 체크
//         if (columnTexts == null || columnTexts.Length != m_ReceiptDgHeaderInfos.Length)
//         {
//             issues |= DgValidationIssue.InvalidColumnCount;
//             Debug.WriteLine($"[ValidateDatagridState] 컬럼 개수 불일치: 실제={columnTexts?.Length}, 예상={m_ReceiptDgHeaderInfos.Length}");
//             return issues; // 개수가 다르면 더 이상 체크 불가
//         }

//         // 2. 각 컬럼 검증
//         for (int x = 0; x < columnTexts.Length; x++)
//         {
//             string columnText = columnTexts[x];

//             // 2-1. 컬럼명이 유효한지 (m_ReceiptDgHeaderInfos에 존재하는지)
//             int index = Array.FindIndex(m_ReceiptDgHeaderInfos, h => h.sName == columnText);

//             if (index < 0) // 존재하지 않는 컬럼
//             {
//                 issues |= DgValidationIssue.InvalidColumn;
//                 Debug.WriteLine($"[ValidateDatagridState] 유효하지 않은 컬럼[{x}]: '{columnText}'");
//                 continue; // 다음 컬럼 검사
//             }

//             // 2-2. 컬럼 순서가 맞는지
//             if (index != x)
//             {
//                 issues |= DgValidationIssue.WrongOrder;
//                 Debug.WriteLine($"[ValidateDatagridState] 컬럼 순서 불일치[{x}]: '{columnText}' (예상 위치={index})");
//             }

//             // 2-3. 컬럼 너비가 맞는지 (허용 오차 이내인지)
//             int actualWidth = listLW[x].nWidth;
//             int expectedWidth = m_ReceiptDgHeaderInfos[index].nWidth;
//             int widthDiff = Math.Abs(actualWidth - expectedWidth);

//             if (widthDiff > COLUMN_WIDTH_TOLERANCE)
//             {
//                 issues |= DgValidationIssue.WrongWidth;
//                 Debug.WriteLine($"[ValidateDatagridState] 컬럼 너비 불일치[{x}]: '{columnText}', 실제={actualWidth}, 예상={expectedWidth}, 오차={widthDiff}");
//             }
//         }

//         if (issues == DgValidationIssue.None)
//         {
//             Debug.WriteLine($"[ValidateDatagridState] Datagrid 상태 정상");
//         }
//         else
//         {
//             Debug.WriteLine($"[ValidateDatagridState] Datagrid 검증 실패: {issues}");
//         }

//         return issues;
//     }

    /// <summary>
    /// Datagrid 컬럼 너비 조정 (드래그 방식)
    /// </summary>
    /// <param name="rcHeader">헤더 영역 Rectangle (MainWnd 기준 상대좌표)</param>
    /// <param name="ptDgTopLeft">Datagrid 좌상단 좌표 (절대좌표)</param>
    /// <param name="indexCol">조정할 컬럼 인덱스</param>
    /// <returns>에러 발생 시 StdResult_Error, 성공 시 null</returns>
//     private StdResult_Error AdjustColumnWidth(
//         Draw.Rectangle rcHeader,
//         Draw.Point ptDgTopLeft,
//         int indexCol)
//     {
//         Draw.Bitmap bmpHeader = null;

//         try
//         {
//             // 1. 헤더 캡처
//             bmpHeader = OfrService.CaptureScreenRect_InWndHandle(
//                 m_Main.TopWnd_hWnd,
//                 rcHeader
//             );

//             if (bmpHeader == null)
//             {
//                 return new StdResult_Error(
//                     $"헤더 캡처 실패: indexCol={indexCol}",
//                     "InsungsAct_RcptRegPage/AdjustColumnWidth_01");
//             }

//             // 2. 컬럼 경계 검출
//             byte minBrightness = OfrService.GetMinBrightnessAtRow_FromColorBitmapFast(
//                 bmpHeader, HEADER_GAB
//             );
//             minBrightness += 2; // 확실한 경계를 위해

//             bool[] boolArr = OfrService.GetBoolArray_FromColorBitmapRowFast(
//                 bmpHeader, HEADER_GAB, minBrightness, 2
//             );

//             List<OfrModel_LeftWidth> listLW =
//                 OfrService.GetLeftWidthList_FromBool1Array(boolArr, minBrightness);

//             if (listLW == null || indexCol >= listLW.Count)
//             {
//                 bmpHeader?.Dispose();
//                 return new StdResult_Error(
//                     $"컬럼 경계 검출 실패: indexCol={indexCol}, listLW.Count={listLW?.Count}",
//                     "InsungsAct_RcptRegPage/AdjustColumnWidth_02");
//             }

//             // 3. 너비 차이 계산
//             int actualWidth = listLW[indexCol].nWidth;
//             int expectedWidth = m_ReceiptDgHeaderInfos[indexCol].nWidth;
//             int dx = expectedWidth - actualWidth;

//             if (dx == 0)
//             {
//                 Debug.WriteLine($"[AdjustColumnWidth] 컬럼[{indexCol}] 너비 조정 불필요: {actualWidth}px");
//                 bmpHeader?.Dispose();
//                 return null; // 이미 맞음
//             }

//             Debug.WriteLine($"[AdjustColumnWidth] 컬럼[{indexCol}] '{m_ReceiptDgHeaderInfos[indexCol].sName}' 너비 조정: {actualWidth} → {expectedWidth} (dx={dx})");

//             // 4. 드래그로 너비 조정
//             // ptStart: 컬럼 오른쪽 경계 (헤더 기준 상대좌표)
//             Draw.Point ptStartRel = new Draw.Point(listLW[indexCol]._nRight + 1, HEADER_GAB);
//             Draw.Point ptEndRel = new Draw.Point(ptStartRel.X + dx, ptStartRel.Y);

//             // MainWnd 기준 상대좌표로 변환
//             Draw.Point ptStartMainRel = new Draw.Point(
//                 rcHeader.Left + ptStartRel.X,
//                 rcHeader.Top + ptStartRel.Y
//             );
//             Draw.Point ptEndMainRel = new Draw.Point(
//                 rcHeader.Left + ptEndRel.X,
//                 rcHeader.Top + ptEndRel.Y
//             );

//             // 절대좌표로 변환
//             Draw.Point ptStartAbs = new Draw.Point(
//                 ptDgTopLeft.X + ptStartMainRel.X,
//                 ptDgTopLeft.Y + ptStartMainRel.Y
//             );
//             Draw.Point ptEndAbs = new Draw.Point(
//                 ptDgTopLeft.X + ptEndMainRel.X,
//                 ptDgTopLeft.Y + ptEndMainRel.Y
//             );

//             // Drag 수행 (Simulation.cs:457-478 참고, 라이브러리 조합 방식)
//             Std32Cursor.SetCursorPos_AbsDrawPt(ptStartAbs);
//             Std32Mouse_Event.MouseEvent_LeftBtnDown();
//             Std32Mouse_Send.MouseSet_MoveSmooth_ptAbs(ptStartAbs, ptEndAbs, 150);
//             Std32Mouse_Event.MouseEvent_LeftBtnUp();

//             bmpHeader?.Dispose();
//             return null; // 성공
//         }
//         catch (Exception ex)
//         {
//             bmpHeader?.Dispose();
//             return new StdResult_Error(
//                 $"AdjustColumnWidth 예외: {ex.Message}",
//                 "InsungsAct_RcptRegPage/AdjustColumnWidth_999");
//         }
//     }
    #endregion

    #region 자동배차 Helper 함수들 (간단)
    /// <summary>
    /// Datagrid 로딩 완료 대기 (Pan 상태 변화 감지)
    /// </summary>
    /// <param name="hWndDG">Datagrid 윈도우 핸들</param>
    /// <param name="Elpase">최대 대기 시간 (밀리초, 기본 500ms)</param>
    /// <returns>성공: Success, 시간 초과: Fail, Pan 없음: Skip</returns>
//     private async Task<StdResult_Status> WaitPanLoadedAsync(IntPtr hWndDG, int Elpase = 500)
//     {
//         IntPtr hWndFind = IntPtr.Zero;

//         // 1. Pan이 나타날 때까지 대기 (최대 100ms)
//         for (int i = 0; i < 100; i++)
//         {
//             hWndFind = Std32Window.GetWndHandle_FromRelDrawPt(hWndDG, m_FileInfo.접수등록Page_DG오더_ptChkRelPanL);
//             if (hWndFind != hWndDG) break;  // Pan이 나타남
//             await Task.Delay(1);
//         }

//         if (hWndFind == hWndDG)  // Pan이 안 나타남 (이미 로딩 완료)
//             return new StdResult_Status(StdResult.Skip);

//         // 2. Pan이 사라질 때까지 대기 (로딩 완료)
//         for (int i = 0; i < Elpase; i++)
//         {
//             hWndFind = Std32Window.GetWndHandle_FromRelDrawPt(hWndDG, m_FileInfo.접수등록Page_DG오더_ptChkRelPanL);
//             if (hWndFind == hWndDG) break;  // Pan이 사라짐 (로딩 완료)
//             await Task.Delay(100);
//         }

//         if (hWndFind != hWndDG)
//         {
//             Debug.WriteLine($"[{m_Context.AppName}/RcptRegPage] WaitPanLoadedAsync 시간 초과");
//             return CommonFuncs_StdResult.ErrMsgResult_Status(StdResult.Fail,
//                 "Datagrid 로딩 대기 시간 초과",
//                 $"{m_Context.AppName}/RcptRegPage/WaitPanLoadedAsync_01");
//         }

//         return new StdResult_Status(StdResult.Success);
//     }

    // TODO: Simulation_Mouse 메서드 구현 후 주석 해제
    ///// <summary>
    ///// 조회 버튼 클릭 후 총계 읽기
    ///// </summary>
    ///// <param name="ctrl">취소 토큰 컨트롤</param>
    ///// <returns>총계 문자열 (실패 시 빈 문자열 또는 null)</returns>
    //public async Task<StdResult_String> Click조회버튼Async(CancelTokenControl ctrl)
    //{
    //    try
    //    {
    //        string str = "";
    //        StdResult_Status resultSts = null;

    //        // 조회 버튼 클릭 후 총계 읽기 반복 시도
    //        for (int i = 0; i < CommonVars.c_nRepeatShort; i++)
    //        {
    //            await ctrl.WaitIfPausedOrCancelledAsync();

    //            // 1. 조회 버튼 클릭
    //            Simulation_Mouse.SafeMousePost_ClickLeft(m_RcptPage.CmdBtn_hWnd조회);

    //            // 2. Datagrid 로딩 대기
    //            resultSts = await WaitPanLoadedAsync(m_RcptPage.DG오더_hWnd);
    //            if (resultSts.Result == StdResult.Fail) continue;

    //            // 3. 총계 읽기
    //            str = Std32Window.GetWindowCaption(m_RcptPage.CallCount_hWnd총계);
    //            if (!string.IsNullOrEmpty(str))
    //            {
    //                Debug.WriteLine($"[{m_Context.AppName}/RcptRegPage] 조회 버튼 클릭 완료, 총계: {str}");
    //                break;
    //            }

    //            await Task.Delay(CommonVars.c_nWaitNormal, ctrl.Token);
    //        }

    //        return new StdResult_String(str);
    //    }
    //    catch (Exception ex)
    //    {
    //        return new StdResult_String(StdUtil.GetExceptionMessage(ex),
    //            $"{m_Context.AppName}/RcptRegPage/Click조회버튼Async_999");
    //    }
    //}

    ///// <summary>
    ///// Empty Row 클릭 (선택 해제용)
    ///// </summary>
    ///// <param name="ctrl">취소 토큰 컨트롤</param>
    ///// <returns>클릭 성공 여부</returns>
    //public async Task<bool> ClickEmptyRowAsync(CancelTokenControl ctrl)
    //{
    //    bool bClicked = false;

    //    try
    //    {
    //        // Empty Row는 [0, 1] 셀 (첫 번째 컬럼, 두 번째 행)
    //        Draw.Point ptRel = StdUtil.GetDrawPoint(m_RcptPage.DG오더_RelChildRects[0, 1], 3, 3);

    //        for (int i = 0; i < CommonVars.c_nRepeatShort; i++)
    //        {
    //            await ctrl.WaitIfPausedOrCancelledAsync();

    //            // 밝기 변화 감지로 클릭 확인
    //            bClicked = await Simulation_Mouse
    //                .SafeMousePost_ClickLeft_ptRel_WaitBrightChange(
    //                    m_RcptPage.DG오더_hWnd,
    //                    ptRel,
    //                    ptRel,
    //                    m_RcptPage.DG오더_nBackgroundBright);

    //            if (bClicked)
    //            {
    //                Debug.WriteLine($"[{m_Context.AppName}/RcptRegPage] Empty Row 클릭 완료");
    //                break;
    //            }

    //            await Task.Delay(100, ctrl.Token);
    //        }

    //        return bClicked;
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.WriteLine($"[{m_Context.AppName}/RcptRegPage] ClickEmptyRowAsync 예외: {ex.Message}");
    //        return false;
    //    }
    //}
    #endregion

    #region 테스트: Selection 유무에 따른 OFR 비교
    /// <summary>
    /// 테스트: Selection 있는 상태 vs 없는 상태 OFR 비교
    /// 목적: EmptyRow 클릭 없이도 OFR이 정상 작동하는지 확인
    /// </summary>
    public async Task<StdResult_String> Test_CompareOFR_WithAndWithoutSelectionAsync()
    {
        try
        {
            Debug.WriteLine("[Test_CompareOFR] ===== 테스트 시작 =====");

            // 1. 현재 상태 그대로 캡처 (Selection 있을 수 있음)
            Debug.WriteLine("[Test_CompareOFR] 1. 현재 상태 캡처 (Selection 있을 수 있음)");
            Draw.Bitmap bmpWithSelection = OfrService.CaptureScreenRect_InWndHandle(m_RcptPage.DG오더_hWnd);
            if (bmpWithSelection == null)
            {
                return new StdResult_String("캡처 실패 (Selection 있는 상태)", "Test_CompareOFR/01");
            }

            // 2. 현재 상태에서 밝기 측정
            Draw.Rectangle[,] rects = m_RcptPage.DG오더_RelChildRects;
            if (rects == null || rects.GetLength(1) < 3)
            {
                return new StdResult_String("RelChildRects 없음", "Test_CompareOFR/02");
            }

            // 첫 번째 데이터 행의 밝기 측정 (y=2, 헤더는 0,1)
            int x = rects[0, 2].Left + 5;
            int y = rects[0, 2].Top + 6;
            int brightness_WithSel = OfrService.GetBrightness_PerPixel(bmpWithSelection, x, y);

            Debug.WriteLine($"[Test_CompareOFR] 2. Selection 있는 상태 밝기: {brightness_WithSel}");
            Debug.WriteLine($"[Test_CompareOFR]    배경 밝기 기준: {m_RcptPage.DG오더_nBackgroundBright}");

            // 3. 첫 번째 행 클릭 (Selection 이동)
            Debug.WriteLine("[Test_CompareOFR] 3. 첫 번째 행 클릭 (Selection 이동)");
            Draw.Point ptFirstRow = StdUtil.GetDrawPoint(rects[0, 2], 3, 3);
            await Simulation_Mouse.SafeMouseEvent_ClickLeft_ptRelAsync(m_RcptPage.DG오더_hWnd, ptFirstRow, true, 100);
            await Task.Delay(200);

            // 4. 첫 번째 행 선택 상태에서 캡처
            Debug.WriteLine("[Test_CompareOFR] 4. 첫 번째 행 선택 상태 캡처");
            Draw.Bitmap bmpFirstRowSel = OfrService.CaptureScreenRect_InWndHandle(m_RcptPage.DG오더_hWnd);
            if (bmpFirstRowSel == null)
            {
                return new StdResult_String("캡처 실패 (첫 행 선택)", "Test_CompareOFR/03");
            }

            int brightness_FirstRowSel = OfrService.GetBrightness_PerPixel(bmpFirstRowSel, x, y);
            Debug.WriteLine($"[Test_CompareOFR]    첫 행 선택 밝기: {brightness_FirstRowSel}");

            // 5. 두 번째 행의 밝기 비교 (선택되지 않은 행)
            int x2 = rects[0, 3].Left + 5;
            int y2 = rects[0, 3].Top + 6;
            int brightness_SecondRow = OfrService.GetBrightness_PerPixel(bmpFirstRowSel, x2, y2);
            Debug.WriteLine($"[Test_CompareOFR]    두 번째 행 밝기 (선택 안됨): {brightness_SecondRow}");

            // 6. 결과 분석
            string result = $"===== OFR Selection 테스트 결과 =====\n";
            result += $"배경 밝기 기준: {m_RcptPage.DG오더_nBackgroundBright}\n";
            result += $"현재 상태 밝기: {brightness_WithSel}\n";
            result += $"첫 행 선택 밝기: {brightness_FirstRowSel}\n";
            result += $"두 번째 행 밝기: {brightness_SecondRow}\n";
            result += $"\n";
            result += $"선택된 행 vs 배경: {Math.Abs(brightness_FirstRowSel - m_RcptPage.DG오더_nBackgroundBright)}\n";
            result += $"선택 안된 행 vs 배경: {Math.Abs(brightness_SecondRow - m_RcptPage.DG오더_nBackgroundBright)}\n";
            result += $"\n";

            // 7. 결론
            int threshold = 10; // 밝기 차이 임계값
            bool isFirstRowSelected = Math.Abs(brightness_FirstRowSel - m_RcptPage.DG오더_nBackgroundBright) > threshold;
            bool isSecondRowNormal = Math.Abs(brightness_SecondRow - m_RcptPage.DG오더_nBackgroundBright) < threshold;

            if (isFirstRowSelected && isSecondRowNormal)
            {
                result += "✅ 결론: 선택된 행만 밝기가 다릅니다.\n";
                result += "   → OFR 시 선택되지 않은 행들은 정상 인식 가능!\n";
                result += "   → EmptyRow 클릭 불필요 (첫 행 선택으로 대체 가능)\n";
            }
            else if (!isFirstRowSelected && !isSecondRowNormal)
            {
                result += "⚠️ 경고: 선택 여부와 무관하게 밝기 차이가 없습니다.\n";
                result += "   → 추가 테스트 필요\n";
            }
            else
            {
                result += "❌ 문제: 예상과 다른 밝기 패턴입니다.\n";
                result += "   → 수동 확인 필요\n";
            }

            Debug.WriteLine($"[Test_CompareOFR] {result}");
            Debug.WriteLine("[Test_CompareOFR] ===== 테스트 종료 =====");

            // Bitmap 해제
            bmpWithSelection?.Dispose();
            bmpFirstRowSel?.Dispose();

            return new StdResult_String(result);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Test_CompareOFR] 예외 발생: {ex.Message}");
            return new StdResult_String(ex.Message, "Test_CompareOFR/999");
        }
    }
    #endregion

    #region PostMessage 기반 클릭 (BlockInput 없음)
    /// <summary>
    /// 첫 번째 행 클릭 (PostMessage 기반, BlockInput 없음)
    /// 목적: EmptyRow 클릭 대체, Selection을 첫 행으로 이동
    /// </summary>
//     private async Task ClickFirstRowAsync_PostMessage(CancelTokenControl ctrl)
//     {
//         try
//         {
//             Debug.WriteLine("[ClickFirstRowAsync_PostMessage] 첫 번째 행 클릭 시작");

//             // 첫 번째 데이터 행 위치 계산 (y=2, 헤더는 0,1)
//             Draw.Rectangle[,] rects = m_RcptPage.DG오더_RelChildRects;
//             if (rects == null || rects.GetLength(1) < 3)
//             {
//                 throw new Exception("RelChildRects 없음 또는 데이터 행 부족");
//             }

//             Draw.Point ptFirstRow = StdUtil.GetDrawPoint(rects[0, 2], 3, 3);

//             // PostMessage 기반 클릭 (BlockInput 없음!)
//             await Std32Mouse_Post.MousePostAsync_ClickLeft_ptRel(m_RcptPage.DG오더_hWnd, ptFirstRow);

//             // 클릭 완료 대기
//             await Task.Delay(100, ctrl.Token);

//             Debug.WriteLine("[ClickFirstRowAsync_PostMessage] 첫 번째 행 클릭 완료");
//         }
//         catch (Exception ex)
//         {
//             Debug.WriteLine($"[ClickFirstRowAsync_PostMessage] 예외: {ex.Message}");
//             throw;
//         }
//     }
    #endregion

    #region Region 4 Helper Methods - Result Classes
    /// <summary>
    /// 팝업창 열기 결과
    /// </summary>
    public class PopupResult
    {
        public bool Success { get; }
        public IntPtr hWndPopup { get; }
        public string ErrorMessage { get; }

        public PopupResult(bool success, IntPtr hWnd, string error = "")
        {
            Success = success;
            hWndPopup = hWnd;
            ErrorMessage = error;
        }
    }

    /// <summary>
    /// 주문 등록 결과
    /// </summary>
    public class RegistResult
    {
        public bool Success { get; }
        public string ErrorMessage { get; }

        public static RegistResult SuccessResult() => new RegistResult(true, "");
        public static RegistResult ErrorResult(string msg) => new RegistResult(false, msg);

        private RegistResult(bool success, string error)
        {
            Success = success;
            ErrorMessage = error;
        }
    }

    /// <summary>
    /// 고객 입력 결과
    /// </summary>
    public class CustomerResult
    {
        public bool Success { get; }
        public string ErrorMessage { get; }

        public static CustomerResult SuccessResult() => new CustomerResult(true, "");
        public static CustomerResult ErrorResult(string msg) => new CustomerResult(false, msg);

        private CustomerResult(bool success, string error)
        {
            Success = success;
            ErrorMessage = error;
        }
    }

    /// <summary>
    /// 고객 검색 결과 타입
    /// </summary>
    private enum AutoAlloc_CustSearch
    {
        Null = 0,   // 검색 실패
        None = 1,   // 검색 결과 없음 (신규 고객)
        One = 2,    // 검색 결과 1개 (정상)
        Multi = 3   // 검색 결과 복수 (수동 처리 필요)
    }

    /// <summary>
    /// 고객 검색 타입 결과
    /// </summary>
    private class AutoAlloc_SearchTypeResult
    {
        public AutoAlloc_CustSearch resultTye { get; }
        public IntPtr hWndResult { get; }

        public AutoAlloc_SearchTypeResult(AutoAlloc_CustSearch resultTye, IntPtr hWndResult)
        {
            this.resultTye = resultTye;
            this.hWndResult = hWndResult;
        }
    }
    #endregion

    #region Region 4 Helper Methods - Input Methods
    /// <summary>
    /// 텍스트 입력 (재시도 포함)
    /// </summary>
//     private async Task<bool> InputTextAsync(IntPtr hWnd, string text)
//     {
//         const int MAX_RETRY = 3;
//         const int WAIT_MS = 100;

//         for (int i = 0; i < MAX_RETRY; i++)
//         {
//             await OfrWork_Common.WriteEditBox_ToHndleAsync(hWnd, text);

//             string current = Std32Window.GetWindowCaption(hWnd);
//             if (current == text)
//                 return true;

//             await Task.Delay(WAIT_MS);
//         }

//         Debug.WriteLine($"[{m_Context.AppName}] 텍스트 입력 실패: 원하는={text}, 현재={Std32Window.GetWindowCaption(hWnd)}");
//         return false;
//     }

    /// <summary>
    /// 전화번호 입력 (Enter 키 포함, 재시도)
    /// </summary>
//     private async Task<bool> InputPhoneAsync(IntPtr hWnd, string phoneNo)
//     {
//         const int MAX_RETRY = 3;
//         const int WAIT_MS = 100;

//         for (int i = 0; i < MAX_RETRY; i++)
//         {
//             // 전화번호 입력
//             Std32Window.SetWindowCaption(hWnd, phoneNo);
//             await Task.Delay(WAIT_MS);

//             // Enter 키 전송
//             await Std32Key_Msg.KeyPostAsync_MouseClickNDown(hWnd, StdCommon32.VK_RETURN);
//             await Task.Delay(WAIT_MS);

//             string current = StdConvert.MakePhoneNumberToDigit(
//                 Std32Window.GetWindowCaption(hWnd));

//             if (current == phoneNo)
//                 return true;

//             await Task.Delay(WAIT_MS);
//         }

//         Debug.WriteLine($"[{m_Context.AppName}] 전화번호 입력 실패: 원하는={phoneNo}, 현재={StdConvert.MakePhoneNumberToDigit(Std32Window.GetWindowCaption(hWnd))}");
//         return false;
//     }

    /// <summary>
    /// 고객명 입력 (검색 포함)
    /// </summary>
//     private async Task<CustomerResult> InputCustomerAsync(
//         IntPtr hWndName, IntPtr hWndDong,
//         string custName, string chargeName, string region)
//     {
//         // 고객명 입력
//         string searchText = NwCommon.GetInsungTextForSearch(custName, chargeName);
//         bool inputOk = await InputTextAsync(hWndName, searchText);

//         if (!inputOk)
//             return CustomerResult.ErrorResult($"{region} 고객명 입력 실패");

//         // 검색 결과 확인
//         var searchResult = await GetCustSearchTypeAsync(hWndName, hWndDong);

//         switch (searchResult.resultTye)
//         {
//             case AutoAlloc_CustSearch.One:
//                 // 1개 검색 → 성공
//                 Debug.WriteLine($"[{m_Context.AppName}] {region} 고객 검색 성공: {custName}");
//                 return CustomerResult.SuccessResult();

//             case AutoAlloc_CustSearch.Multi:
//                 // 여러 개 → 수동 처리 필요
//                 Debug.WriteLine($"[{m_Context.AppName}] {region} 고객 복수 검색됨: {custName} (수동 처리 필요)");
//                 return CustomerResult.ErrorResult($"{region} 고객 복수 검색됨 (수동 처리 필요)");

//             case AutoAlloc_CustSearch.None:
//                 // 없음 → 고객 등록 필요
//                 Debug.WriteLine($"[{m_Context.AppName}] {region} 고객 없음: {custName} (등록 필요)");
//                 return CustomerResult.ErrorResult($"{region} 고객 없음 (등록 필요)");

//             default:
//                 return CustomerResult.ErrorResult($"{region} 고객 검색 실패");
//         }
//     }

    /// <summary>
    /// 고객 검색 결과 타입 확인
    /// - 검색 결과가 1개인지, 복수인지, 없는지 확인
    /// </summary>
//     private async Task<AutoAlloc_SearchTypeResult> GetCustSearchTypeAsync(IntPtr hWnd고객명, IntPtr hWnd동명)
//     {
//         IntPtr hWndTmp = IntPtr.Zero;
//         string sTmp = "";

//         // EnterKey 전송
//         Std32Key_Msg.KeyPost_Down(hWnd고객명, StdCommon32.VK_RETURN);

//         // 검색 결과 확인 (최대 50번, 1.5초)
//         for (int j = 0; j < 50; j++)
//         {
//             await Task.Delay(30);

//             // 1개 검색됨 - 동명에 텍스트가 들어옴
//             sTmp = Std32Window.GetWindowCaption(hWnd동명);
//             if (!string.IsNullOrEmpty(sTmp))
//             {
//                 return new AutoAlloc_SearchTypeResult(AutoAlloc_CustSearch.One, IntPtr.Zero);
//             }

//             // 신규 고객 - 고객등록창이 뜸
//             hWndTmp = Std32Window.FindMainWindow(m_Context.MemInfo.Splash.TopWnd_uProcessId, null,
//                 m_Context.FileInfo.고객등록Wnd_TopWnd_sWndName);
//             if (hWndTmp != IntPtr.Zero)
//             {
//                 return new AutoAlloc_SearchTypeResult(AutoAlloc_CustSearch.None, hWndTmp);
//             }

//             // 복수 고객 - 고객검색창이 뜸
//             hWndTmp = Std32Window.FindMainWindow(m_Context.MemInfo.Splash.TopWnd_uProcessId, null,
//                 m_Context.FileInfo.고객검색Wnd_TopWnd_sWndName);
//             if (hWndTmp != IntPtr.Zero)
//             {
//                 // 고객검색창이 떴을 때, 단수 고객이면 자동으로 닫힘
//                 // 1.5초 동안 기다리면서 창이 닫히는지 확인
//                 for (int k = 0; k < 30; k++)
//                 {
//                     hWndTmp = Std32Window.FindMainWindow(m_Context.MemInfo.Splash.TopWnd_uProcessId, null,
//                         m_Context.FileInfo.고객검색Wnd_TopWnd_sWndName);
//                     if (hWndTmp == IntPtr.Zero) break; // 창이 닫힘 → 단수 고객

//                     await Task.Delay(50);
//                 }

//                 // 30번 반복 후에도 창이 살아있으면 복수 고객
//                 if (hWndTmp != IntPtr.Zero)
//                 {
//                     return new AutoAlloc_SearchTypeResult(AutoAlloc_CustSearch.Multi, hWndTmp);
//                 }
//                 // 창이 닫혔으면 계속 루프 (동명 확인으로)
//             }
//         }

//         // 검색 실패
//         Debug.WriteLine($"[{m_Context.AppName}] GetCustSearchTypeAsync 실패: 타임아웃");
//         return new AutoAlloc_SearchTypeResult(AutoAlloc_CustSearch.Null, IntPtr.Zero);
//     }
    #endregion

    #region Region 4 Main Methods - New Order Processing
    /// <summary>
    /// 신규 주문 팝업창 열기
    /// - 신규 버튼 클릭
    /// - 팝업창 대기 및 검증
    /// </summary>
    public async Task<PopupResult> OpenNewOrderPopupAsync(CancelTokenControl ctrl)
    {
        const int MAX_RETRY = 3;
        const int MAX_WAIT_LOOP = 100;
        const int WAIT_MS = 50;

        IntPtr hWndPopup = IntPtr.Zero;

        try
        {
            // 신규 버튼 클릭 시도 (최대 3번)
            for (int retry = 0; retry < MAX_RETRY; retry++)
            {
                await ctrl.WaitIfPausedOrCancelledAsync();

                // 신규 버튼 클릭
                await Std32Mouse_Post.MousePostAsync_ClickLeft(m_RcptPage.CmdBtn_hWnd신규);
                Debug.WriteLine($"[{m_Context.AppName}] 신규버튼 클릭 완료 (시도 {retry + 1}/{MAX_RETRY})");

                // 팝업창 대기 (100번 * 50ms = 5초)
                bool bFound = false;
                for (int k = 0; k < MAX_WAIT_LOOP; k++)
                {
                    await Task.Delay(WAIT_MS);

                    // 팝업창 찾기
                    hWndPopup = Std32Window.FindMainWindow_NotTransparent(
                        m_Context.MemInfo.Splash.TopWnd_uProcessId,
                        m_Context.FileInfo.접수등록Wnd_TopWnd_sWndName_Reg);

                    if (hWndPopup == IntPtr.Zero) continue;

                    // 닫기 버튼 검증
                    IntPtr hWndClose = Std32Window.GetWndHandle_FromRelDrawPt(
                        hWndPopup, m_Context.FileInfo.접수등록Wnd_신규버튼그룹_ptChkRel닫기);

                    if (hWndClose == IntPtr.Zero) continue;

                    string closeText = Std32Window.GetWindowCaption(hWndClose);
                    if (closeText.StartsWith(m_Context.FileInfo.접수등록Wnd_버튼그룹_sWndName닫기))
                    {
                        bFound = true;
                        Debug.WriteLine($"[{m_Context.AppName}] 신규주문 팝업창 열림: {hWndPopup:X}");
                        break;
                    }
                }

                if (bFound)
                {
                    await Task.Delay(100); // 팝업창 안정화 대기
                    return new PopupResult(true, hWndPopup);
                }

                // 재시도 전 대기
                await Task.Delay(100);
            }

            // 모든 재시도 실패
            return new PopupResult(false, IntPtr.Zero, "신규 버튼 클릭 후 팝업창이 열리지 않음");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{m_Context.AppName}] OpenNewOrderPopupAsync 예외: {ex.Message}");
            return new PopupResult(false, IntPtr.Zero, $"팝업창 열기 예외: {ex.Message}");
        }
    }

    #region 공통 함수 (반복 패턴 제거)

    /// <summary>
    /// 공통 함수 #1: EditBox 입력 및 검증 (재시도 포함)
    /// </summary>
    /// <param name="hWnd">EditBox 핸들</param>
    /// <param name="expectedValue">입력할 값</param>
    /// <param name="fieldName">필드명 (에러 메시지용)</param>
    /// <param name="normalizeFunc">정규화 함수 (전화번호 등)</param>
//     private async Task<RegistResult> WriteAndVerifyEditBoxAsync(
//         IntPtr hWnd,
//         string expectedValue,
//         string fieldName,
//         CancelTokenControl ctrl,
//         Func<string, string>? normalizeFunc = null)
//     {
//         const int MAX_RETRY = 3;
//         const int WAIT_MS = 200;

//         for (int i = 0; i < MAX_RETRY; i++)
//         {
//             await ctrl.WaitIfPausedOrCancelledAsync();

//             // 쓰기 (대기 포함 버전 사용)
//             string writtenValue = await OfrWork_Common.WriteEditBox_ToHndleAsyncWait(hWnd, expectedValue);

//             // 정규화 (필요시)
//             if (normalizeFunc != null)
//                 writtenValue = normalizeFunc(writtenValue);

//             // 검증
//             if (expectedValue == writtenValue)
//                 return RegistResult.SuccessResult();

//             Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 불일치 (재시도 {i + 1}/{MAX_RETRY}): " +
//                           $"예상=\"{expectedValue}\", 실제=\"{writtenValue}\"");

//             await Task.Delay(WAIT_MS);
//         }

//         // 최종 실패
//         string finalValue = Std32Window.GetWindowCaption(hWnd);
//         if (normalizeFunc != null)
//             finalValue = normalizeFunc(finalValue);

//         return RegistResult.ErrorResult(
//             $"{fieldName} 입력 실패: 예상=\"{expectedValue}\", 실제=\"{finalValue}\"");
//     }

    /// <summary>
    /// 공통 함수 #1-2: 적요 전용 키보드 시뮬레이션 입력
    /// - 인성 앱의 적요 필드는 SetWindowText 시 앞에 공백이 삽입되는 문제가 있음
    /// - HOME + DELETE로 기존값 삭제 후 클립보드로 붙여넣기
    /// </summary>
//     private async Task<RegistResult> WriteAndVerifyEditBox_KeyboardAsync(
//         IntPtr hWnd,
//         string expectedValue,
//         string fieldName,
//         CancelTokenControl ctrl)
//     {
//         await ctrl.WaitIfPausedOrCancelledAsync();

//         // ===== 1단계: 클립보드에 값 복사 =====
//         System.Windows.Clipboard.SetText(expectedValue);

//         // ===== 2단계: HOME 키로 커서를 처음으로 =====
//         Std32Key_Msg.KeyPost_Down(hWnd, StdCommon32.VK_HOME);

//         // ===== 3단계: Shift + End로 전체 선택 =====
//         Std32Key_Msg.KeyPost_Down(hWnd, StdCommon32.VK_SHIFT);
//         Std32Key_Msg.KeyPost_Down(hWnd, StdCommon32.VK_END);

//         // ===== 4단계: Ctrl+V로 붙여넣기 =====
//         await Simulation_Keyboard.KeyPost_CtrlV_PasteAsync(hWnd);

//         // ===== 5단계: MsgBox로 수동 확인 대기 (딜레이 대신) =====
//         System.Windows.MessageBox.Show(
//             $"{fieldName} 입력 완료\n\n" +
//             $"입력값: \"{expectedValue}\"\n\n" +
//             $"인성 앱에서 값을 확인해주세요.",
//             "입력 확인 필요",
//             System.Windows.MessageBoxButton.OK,
//             System.Windows.MessageBoxImage.Information);

//         // ===== 6단계: 검증 =====
//         string readValue = Std32Window.GetWindowCaption(hWnd);
//         if (readValue != expectedValue)
//         {
//             Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 키보드 입력 실패: 기대값=\"{expectedValue}\", 실제값=\"{readValue}\"");
//             return RegistResult.ErrorResult($"{fieldName} 입력 실패 (기대: \"{expectedValue}\", 실제: \"{readValue}\")");
//         }

//         Debug.WriteLine($"[{m_Context.AppName}]   {fieldName}: \"{expectedValue}\" (키보드 입력 성공)");
//         return RegistResult.SuccessResult();
//     }

    /// <summary>
    /// 공통 함수 #2: 고객 검색 및 선택 (의뢰자, 출발지, 도착지 공통)
    /// TODO: GetCustSearchTypeAsync, 고객검색Wnd 등 복잡한 로직 통합 필요
    /// </summary>
//     private async Task<RegistResult> SearchAndSelectCustomerAsync(
//         IntPtr hWnd고객명,
//         IntPtr hWnd동명,
//         string custName,
//         string chargeName,
//         string fieldName,
//         CancelTokenControl ctrl)
//     {
//         // 1. 검색어 생성 (상호/담당)
//         string searchText = NwCommon.GetInsungTextForSearch(custName, chargeName);

//         // 2. 고객명 EditBox에 입력 (OfrWork_Common 사용 - 기존 로직과 동일)
//         var resultBool = await OfrWork_Common.WriteEditBox_ToHndleAsync(hWnd고객명, searchText);
//         if (resultBool == null || !resultBool.bResult)
//             return RegistResult.ErrorResult($"{fieldName} 고객명 입력 실패");

//         // 3. 검색 실행 및 결과 타입 확인 (GetCustSearchTypeAsync - Enter 키 포함)
//         var searchResult = await GetCustSearchTypeAsync(hWnd고객명, hWnd동명);

//         // 4. 검색 결과 처리
//         switch (searchResult.resultTye)
//         {
//             case AutoAlloc_CustSearch.One:
//                 // 1개 검색 성공 - 그대로 진행
//                 Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 검색 성공 (1개)");
//                 return RegistResult.SuccessResult();

//             case AutoAlloc_CustSearch.Multi:
//                 // TODO: 복수 고객 검색창 처리 필요
//                 Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 복수 검색 결과 - TODO 처리 필요");
//                 return RegistResult.ErrorResult($"{fieldName} 복수 검색됨 (TODO: 고객검색창 처리 필요)");

//             case AutoAlloc_CustSearch.None:
//                 // TODO: 신규 고객 등록창 처리 필요
//                 Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 신규 고객 - TODO 처리 필요");
//                 return RegistResult.ErrorResult($"{fieldName} 신규 고객 (TODO: 고객등록창 처리 필요)");

//             case AutoAlloc_CustSearch.Null:
//             default:
//                 return RegistResult.ErrorResult($"{fieldName} 검색 타임아웃");
//         }
//     }

    /// <summary>
    /// 공통 함수 #3: CheckBox 설정 및 검증
    /// TODO: OFR 기반 체크박스 찾기 및 클릭 구현 필요
    /// </summary>
//     private async Task<RegistResult> SetAndVerifyCheckBoxAsync(
//         IntPtr hWndParent,
//         string checkBoxName,
//         bool shouldCheck,
//         string fieldName,
//         CancelTokenControl ctrl)
//     {
//         // TODO: OFR로 체크박스 이미지 찾아서 상태 확인 후 클릭
//         Debug.WriteLine($"[{m_Context.AppName}] TODO: SetAndVerifyCheckBoxAsync 구현 필요 - {fieldName}");
//         return RegistResult.SuccessResult(); // 임시
//     }

    /// <summary>
    /// 공통 함수 #4: RadioButton 설정 및 검증
    /// TODO: OFR 기반 라디오버튼 찾기 및 클릭 구현 필요
    /// </summary>
//     private async Task<RegistResult> SetAndVerifyRadioButtonAsync(
//         IntPtr hWndParent,
//         string radioGroupName,
//         string radioValue,
//         string fieldName,
//         CancelTokenControl ctrl)
//     {
//         // TODO: OFR로 라디오버튼 그룹 찾아서 원하는 값 클릭
//         Debug.WriteLine($"[{m_Context.AppName}] TODO: SetAndVerifyRadioButtonAsync 구현 필요 - {fieldName}={radioValue}");
//         return RegistResult.SuccessResult(); // 임시
//     }

    /// <summary>
    /// 공통 함수 #5: ComboBox 아이템 선택
    /// TODO: OFR 기반 콤보박스 열기 및 아이템 선택 구현 필요
    /// </summary>
//     private async Task<RegistResult> SelectComboBoxItemAsync(
//         IntPtr hWndParent,
//         string comboBoxName,
//         string itemValue,
//         string fieldName,
//         CancelTokenControl ctrl)
//     {
//         // TODO: OFR로 콤보박스 찾아서 열고 아이템 클릭
//         Debug.WriteLine($"[{m_Context.AppName}] TODO: SelectComboBoxItemAsync 구현 필요 - {fieldName}={itemValue}");
//         return RegistResult.SuccessResult(); // 임시
//     }

    /// <summary>
    /// 공통 함수 #6: 숫자 필드 입력 및 검증 (SetWindowText 시도 → 실패 시 키보드 폴백)
    /// - 1차: SetWindowText로 값 설정 시도 (포커스 불필요)
    /// - 2차: 실패 시 키보드 시뮬레이션 (HOME + DELETE + 숫자 입력)
    /// - Enter 키로 인성 앱의 합계 재계산 트리거
    /// </summary>
//     private async Task<RegistResult> WriteAndVerifyNumericAsync(
//         IntPtr hWnd,
//         int value,
//         string fieldName,
//         CancelTokenControl ctrl)
//     {
//         await ctrl.WaitIfPausedOrCancelledAsync();

//         string targetValue = value.ToString();

//         // ===== 1차 시도: SetWindowText 방식 =====
//         int nResult = Std32Window.SetWindowCaption(hWnd, targetValue);
//         if (nResult != 1) nResult = Std32Window.SetWindowText(hWnd, targetValue);
//         await Task.Delay(100);

//         // 검증
//         string readValue = Std32Window.GetWindowCaption(hWnd);

//         if (readValue != targetValue)
//         {
//             // ===== 2차 시도: 키보드 시뮬레이션 방식 (HOME + DELETE + 키 입력) =====
//             Debug.WriteLine($"[{m_Context.AppName}] {fieldName}: SetWindowText 실패 → 키보드 방식으로 재시도");

//             // HOME 키로 커서를 처음으로 (hWnd에 직접 전송, SetFocus 불필요)
//             Std32Key_Msg.KeyPost_Down(hWnd, StdCommon32.VK_HOME);
//             await Task.Delay(30);

//             // DELETE 50번 (기존값 전체 삭제)
//             for (int i = 0; i < 50; i++)
//                 Std32Key_Msg.KeyPost_Down(hWnd, StdCommon32.VK_DELETE);
//             await Task.Delay(50);

//             // 숫자 한 글자씩 입력
//             foreach (char ch in targetValue)
//             {
//                 if (char.IsDigit(ch))
//                 {
//                     // VkKeyScan으로 문자를 VK 코드로 변환
//                     short vkCode = StdWin32.VkKeyScan(ch);
//                     uint vk = (uint)(vkCode & 0xFF);

//                     Std32Key_Msg.KeyPost_Down(hWnd, vk);
//                     await Task.Delay(30);
//                 }
//             }
//             await Task.Delay(50);
//         }
//         else
//         {
//             Debug.WriteLine($"[{m_Context.AppName}] {fieldName}: SetWindowText 성공");
//         }

//         // ===== Enter 키로 합계 재계산 트리거 =====
//         await Std32Key_Msg.KeyPostAsync_MouseClickNDown(hWnd, StdCommon32.VK_RETURN);
//         await Task.Delay(100);

//         // ===== 최종 검증 (원화 포맷 변환) =====
//         readValue = Std32Window.GetWindowCaption(hWnd);
//         int readNumeric = StdConvert.StringWonFormatToInt(readValue);

//         if (readNumeric != value)
//         {
//             Debug.WriteLine($"[{m_Context.AppName}] {fieldName} 입력 실패: 기대값={value}, 실제값={readNumeric}");
//             return RegistResult.ErrorResult($"{fieldName} 입력 실패 (기대: {value}, 실제: {readNumeric})");
//         }

//         Debug.WriteLine($"[{m_Context.AppName}]   {fieldName}: {value} → {readNumeric} (검증 성공)");
//         return RegistResult.SuccessResult();
//     }

    #endregion

    /// <summary>
    /// 신규 주문 정보를 팝업창에 입력
    /// - 의뢰자, 출발지, 도착지 정보 입력
    /// - 저장 버튼 클릭
    /// </summary>
    //public async Task<RegistResult> RegistOrderToPopupAsync(
    //    AutoAlloc item,
    //    IntPtr hWndPopup,
    //    CancelTokenControl ctrl)
    //{
    //    try
    //    {
    //        TbOrder order = item.NewOrder;

    //        Debug.WriteLine($"[{m_Context.AppName}] ===== 신규 주문 입력 시작 =====");
    //        Debug.WriteLine($"[{m_Context.AppName}]   주문번호: {order.KeyCode}");
    //        Debug.WriteLine($"[{m_Context.AppName}]   의뢰자: {order.CallCustName}/{order.CallChargeName}");
    //        Debug.WriteLine($"[{m_Context.AppName}]   출발지: {order.StartCustName} ({order.StartDongBasic})");
    //        Debug.WriteLine($"[{m_Context.AppName}]   도착지: {order.DestCustName}");

    //        // RcptWnd_New 클래스로 팝업창 핸들 초기화
    //        var wndRcpt = new InsungsInfo_Mem.RcptWnd_New(hWndPopup, m_Context.FileInfo);

    //        // ===== 1. 의뢰자 정보 입력 =====
    //        Debug.WriteLine($"[{m_Context.AppName}] 1. 의뢰자 정보 입력...");
    //        var result = await SearchAndSelectCustomerAsync(
    //            wndRcpt.의뢰자_hWnd고객명,
    //            wndRcpt.의뢰자_hWnd동명,
    //            order.CallCustName,
    //            order.CallChargeName,
    //            "의뢰자",
    //            ctrl);
    //        if (!result.Success) return result;

    //        // 의뢰자 전화1
    //        result = await WriteAndVerifyEditBoxAsync(
    //            wndRcpt.의뢰자_hWnd전화1,
    //            order.CallTelNo ?? "",
    //            "의뢰자_전화1",
    //            ctrl);
    //        if (!result.Success) return result;

    //        // 의뢰자 전화2
    //        result = await WriteAndVerifyEditBoxAsync(
    //            wndRcpt.의뢰자_hWnd전화2,
    //            order.CallTelNo2 ?? "",
    //            "의뢰자_전화2",
    //            ctrl);
    //        if (!result.Success) return result;

    //        // 의뢰자 부서
    //        result = await WriteAndVerifyEditBoxAsync(
    //            wndRcpt.의뢰자_hWnd부서,
    //            order.CallDeptName ?? "",
    //            "의뢰자_부서",
    //            ctrl);
    //        if (!result.Success) return result;

    //        // 의뢰자 담당
    //        result = await WriteAndVerifyEditBoxAsync(
    //            wndRcpt.의뢰자_hWnd담당,
    //            order.CallChargeName ?? "",
    //            "의뢰자_담당",
    //            ctrl);
    //        if (!result.Success) return result;

    //        // ===== 2. 출발지 정보 입력 =====
    //        Debug.WriteLine($"[{m_Context.AppName}] 2. 출발지 정보 입력...");

    //        // 출발지 = 의뢰자인 경우 vs 다른 경우 분기
    //        if (order.StartCustCodeK == order.CallCustCodeK)
    //        {
    //            // 출발지 = 의뢰자: Enter만 치고 동명 확인
    //            Debug.WriteLine($"[{m_Context.AppName}]   출발지 = 의뢰자: Enter로 자동 입력");
    //            Std32Key_Msg.KeyPost_Down(wndRcpt.출발지_hWnd고객명, StdCommon32.VK_RETURN);
    //            await Task.Delay(100);

    //            string 동명 = Std32Window.GetWindowCaption(wndRcpt.출발지_hWnd동명);
    //            if (string.IsNullOrEmpty(동명))
    //            {
    //                Debug.WriteLine($"[{m_Context.AppName}] 출발지 동명 확인 실패");
    //                return RegistResult.ErrorResult("출발지 동명 확인 실패");
    //            }
    //        }
    //        else
    //        {
    //            // 출발지 ≠ 의뢰자: 고객 검색
    //            Debug.WriteLine($"[{m_Context.AppName}]   출발지 ≠ 의뢰자: 고객 검색");
    //            result = await SearchAndSelectCustomerAsync(
    //                wndRcpt.출발지_hWnd고객명,
    //                wndRcpt.출발지_hWnd동명,
    //                order.StartCustName,
    //                order.StartChargeName,
    //                "출발지",
    //                ctrl);
    //            if (!result.Success) return result;
    //        }

    //        // 출발지 동명
    //        result = await WriteAndVerifyEditBoxAsync(
    //            wndRcpt.출발지_hWnd동명,
    //            order.StartDongBasic ?? "",
    //            "출발지_동명",
    //            ctrl);
    //        if (!result.Success) return result;

    //        // 출발지 전화1
    //        result = await WriteAndVerifyEditBoxAsync(
    //            wndRcpt.출발지_hWnd전화1,
    //            order.StartTelNo ?? "",
    //            "출발지_전화1",
    //            ctrl);
    //        if (!result.Success) return result;

    //        // 출발지 전화2
    //        result = await WriteAndVerifyEditBoxAsync(
    //            wndRcpt.출발지_hWnd전화2,
    //            order.StartTelNo2 ?? "",
    //            "출발지_전화2",
    //            ctrl);
    //        if (!result.Success) return result;

    //        // 출발지 부서
    //        result = await WriteAndVerifyEditBoxAsync(
    //            wndRcpt.출발지_hWnd부서,
    //            order.StartDeptName ?? "",
    //            "출발지_부서",
    //            ctrl);
    //        if (!result.Success) return result;

    //        // 출발지 담당
    //        result = await WriteAndVerifyEditBoxAsync(
    //            wndRcpt.출발지_hWnd담당,
    //            order.StartChargeName ?? "",
    //            "출발지_담당",
    //            ctrl);
    //        if (!result.Success) return result;

    //        // 출발지 위치
    //        result = await WriteAndVerifyEditBoxAsync(
    //            wndRcpt.출발지_hWnd위치,
    //            order.StartDetailAddr ?? "",
    //            "출발지_위치",
    //            ctrl);
    //        if (!result.Success) return result;

    //        // ===== 3. 도착지 정보 입력 =====
    //        Debug.WriteLine($"[{m_Context.AppName}] 3. 도착지 정보 입력...");

    //        // 도착지 = 의뢰자인 경우 vs 다른 경우 분기
    //        if (order.DestCustCodeK == order.CallCustCodeK)
    //        {
    //            // 도착지 = 의뢰자: Enter만 치고 동명 확인
    //            Debug.WriteLine($"[{m_Context.AppName}]   도착지 = 의뢰자: Enter로 자동 입력");
    //            Std32Key_Msg.KeyPost_Down(wndRcpt.도착지_hWnd고객명, StdCommon32.VK_RETURN);
    //            await Task.Delay(100);

    //            string 동명 = Std32Window.GetWindowCaption(wndRcpt.도착지_hWnd동명);
    //            if (string.IsNullOrEmpty(동명))
    //            {
    //                Debug.WriteLine($"[{m_Context.AppName}] 도착지 동명 확인 실패");
    //                return RegistResult.ErrorResult("도착지 동명 확인 실패");
    //            }
    //        }
    //        else
    //        {
    //            // 도착지 ≠ 의뢰자: 고객 검색
    //            Debug.WriteLine($"[{m_Context.AppName}]   도착지 ≠ 의뢰자: 고객 검색");
    //            result = await SearchAndSelectCustomerAsync(
    //                wndRcpt.도착지_hWnd고객명,
    //                wndRcpt.도착지_hWnd동명,
    //                order.DestCustName,
    //                order.DestChargeName,
    //                "도착지",
    //                ctrl);
    //            if (!result.Success) return result;
    //        }

    //        // 도착지 동명
    //        result = await WriteAndVerifyEditBoxAsync(
    //            wndRcpt.도착지_hWnd동명,
    //            order.DestDongBasic ?? "",
    //            "도착지_동명",
    //            ctrl);
    //        if (!result.Success) return result;

    //        // 도착지 전화1
    //        result = await WriteAndVerifyEditBoxAsync(
    //            wndRcpt.도착지_hWnd전화1,
    //            order.DestTelNo ?? "",
    //            "도착지_전화1",
    //            ctrl);
    //        if (!result.Success) return result;

    //        // 도착지 전화2
    //        result = await WriteAndVerifyEditBoxAsync(
    //            wndRcpt.도착지_hWnd전화2,
    //            order.DestTelNo2 ?? "",
    //            "도착지_전화2",
    //            ctrl);
    //        if (!result.Success) return result;

    //        // 도착지 부서
    //        result = await WriteAndVerifyEditBoxAsync(
    //            wndRcpt.도착지_hWnd부서,
    //            order.DestDeptName ?? "",
    //            "도착지_부서",
    //            ctrl);
    //        if (!result.Success) return result;

    //        // 도착지 담당
    //        result = await WriteAndVerifyEditBoxAsync(
    //            wndRcpt.도착지_hWnd담당,
    //            order.DestChargeName ?? "",
    //            "도착지_담당",
    //            ctrl);
    //        if (!result.Success) return result;

    //        // 도착지 위치
    //        result = await WriteAndVerifyEditBoxAsync(
    //            wndRcpt.도착지_hWnd위치,
    //            order.DestDetailAddr ?? "",
    //            "도착지_위치",
    //            ctrl);
    //        if (!result.Success) return result;

    //        // ===== 4. 기타 정보 입력 =====
    //        Debug.WriteLine($"[{m_Context.AppName}] 4. 기타 정보 입력...");

    //        // 4-1. 적요 (OrderRemarks) - 백업 코드 방식 (단순 WriteEditBox)
    //        string sTmp적요 = Std32Window.GetWindowCaption(wndRcpt.우측상단_hWnd적요);
    //        if ((order.OrderRemarks ?? "") != sTmp적요)
    //        {
    //            for (int i = 0; i < 3; i++)
    //            {
    //                await OfrWork_Common.WriteEditBox_ToHndleAsync(wndRcpt.우측상단_hWnd적요, order.OrderRemarks ?? "");
    //                sTmp적요 = Std32Window.GetWindowCaption(wndRcpt.우측상단_hWnd적요);
    //                if ((order.OrderRemarks ?? "") == sTmp적요) break;

    //                await Task.Delay(200);
    //            }

    //            if ((order.OrderRemarks ?? "") != sTmp적요)
    //            {
    //                Debug.WriteLine($"[{m_Context.AppName}] 적요 입력 실패: 기대=\"{order.OrderRemarks}\", 실제=\"{sTmp적요}\"");
    //                return RegistResult.ErrorResult($"적요 입력 실패");
    //            }
    //        }
    //        Debug.WriteLine($"[{m_Context.AppName}]   적요: \"{order.OrderRemarks}\"");

    //        // 4-2. 공유 (Share) - CheckBox
    //        Debug.WriteLine($"[{m_Context.AppName}] 4-2. 공유 CheckBox 처리...");

    //        // Capture window
    //        Draw.Bitmap bmpWnd = OfrService.CaptureScreenRect_InWndHandle(wndRcpt.TopWnd_hWnd, 0);

    //        // Read current state
    //        StdResult_NulBool resultShare = await OfrWork_Insungs.OfrImgChkValue_RectInBitmapAsync(
    //            bmpWnd, m_FileInfo.접수등록Wnd_우측상단_rcChkRel공유);

    //        if (resultShare.bResult == null)
    //        {
    //            bmpWnd.Dispose();
    //            return RegistResult.ErrorResult("공유 CheckBox 인식 실패");
    //        }

    //        bool currentShare = StdConvert.NullableBoolToBool(resultShare.bResult);

    //        // Change state if needed
    //        if (order.Share != currentShare)
    //        {
    //            for (int i = 0; i < 3; i++)
    //            {
    //                StdResult_Error resultError = await OfrWork_Common.SetCheckBox_StatusAsync(
    //                    wndRcpt.TopWnd_hWnd,
    //                    m_FileInfo.접수등록Wnd_우측상단_rcChkRel공유,
    //                    order.Share,
    //                    "공유");

    //                if (resultError == null) break;
    //                if (i == 2)
    //                {
    //                    bmpWnd.Dispose();
    //                    return RegistResult.ErrorResult($"공유 CheckBox 변경 실패");
    //                }
    //                await Task.Delay(200);
    //            }
    //        }

    //        bmpWnd.Dispose();
    //        Debug.WriteLine($"[{m_Context.AppName}]   공유: {order.Share}");

    //        //// 4-3. 요금종류 (FeeType) - RadioButton OFR은 나중에 구현 - 필수!
    //        //Debug.WriteLine($"[{m_Context.AppName}]   요금종류: {order.FeeType} (TODO: OFR RadioButton)");

    //        //// 4-4. 차량종류 (CarType) - RadioButton OFR은 나중에 구현
    //        //Debug.WriteLine($"[{m_Context.AppName}]   차량종류: {order.CarType} (TODO: OFR RadioButton)");

    //        //// 4-5. 배송타입 (DeliverType) - RadioButton OFR은 나중에 구현
    //        //Debug.WriteLine($"[{m_Context.AppName}]   배송타입: {order.DeliverType} (TODO: OFR RadioButton)");

    //        //// 4-6. 계산서 (TaxBill) - CheckBox OFR은 나중에 구현
    //        //Debug.WriteLine($"[{m_Context.AppName}]   계산서: {order.TaxBill} (TODO: OFR CheckBox)");

    //        //// 4-7. 요금 그룹 (기본요금, 추가금액, 할인금액, 탁송료)
    //        //Debug.WriteLine($"[{m_Context.AppName}] 4-7. 요금 그룹 입력...");

    //        //// 기본요금
    //        //result = await WriteAndVerifyNumericAsync(
    //        //    wndRcpt.요금그룹_hWnd기본요금,
    //        //    order.FeeBasic,
    //        //    "기본요금",
    //        //    ctrl);
    //        //if (!result.Success) return result;

    //        //// 추가금액
    //        //result = await WriteAndVerifyNumericAsync(
    //        //    wndRcpt.요금그룹_hWnd추가금액,
    //        //    order.FeePlus,
    //        //    "추가금액",
    //        //    ctrl);
    //        //if (!result.Success) return result;

    //        //// 할인금액
    //        //result = await WriteAndVerifyNumericAsync(
    //        //    wndRcpt.요금그룹_hWnd할인금액,
    //        //    order.FeeMinus,
    //        //    "할인금액",
    //        //    ctrl);
    //        //if (!result.Success) return result;

    //        //// 탁송료
    //        //result = await WriteAndVerifyNumericAsync(
    //        //    wndRcpt.요금그룹_hWnd탁송료,
    //        //    order.FeeConn,
    //        //    "탁송료",
    //        //    ctrl);
    //        //if (!result.Success) return result;

    //        //Debug.WriteLine($"[{m_Context.AppName}]   기본요금: {order.FeeBasic}, 추가: {order.FeePlus}, 할인: {order.FeeMinus}, 탁송료: {order.FeeConn}");

    //        //// 4-8. 오더메모 (KeyCode/OrderMemo 형식)
    //        //string orderMemo = $"{order.KeyCode}/{order.OrderMemo}";
    //        //result = await WriteAndVerifyEditBoxAsync(
    //        //    wndRcpt.hWnd오더메모,
    //        //    orderMemo,
    //        //    "오더메모",
    //        //    ctrl);
    //        //if (!result.Success) return result;
    //        //Debug.WriteLine($"[{m_Context.AppName}]   오더메모: {orderMemo}");

    //        // ===== 5. 입력 완료 - 메시지박스로 확인 =====
    //        Debug.WriteLine($"[{m_Context.AppName}] ===== 모든 입력 완료 =====");
    //        Debug.WriteLine($"[{m_Context.AppName}] 저장 버튼을 클릭하지 않고 대기합니다.");
    //        Debug.WriteLine($"[{m_Context.AppName}] 사용자가 수동으로 확인 후 닫기 버튼으로 처리합니다.");

    //        // ===== 메시지박스 직전 적요 재검증 (지워지는지 확인) =====
    //        string 적요메시지박스직전 = Std32Window.GetWindowCaption(wndRcpt.우측상단_hWnd적요);
    //        Debug.WriteLine($"[{m_Context.AppName}] ★★★ 적요 메시지박스 직전: \"{적요메시지박스직전}\"");

    //        // 메시지박스로 입력 완료 알림
    //        System.Windows.MessageBox.Show(
    //            $"모든 필드 입력이 완료되었습니다.\n\n" +
    //            $"주문번호: {order.KeyCode}\n" +
    //            $"의뢰자: {order.CallCustName}\n" +
    //            $"출발지: {order.StartCustName}\n" +
    //            $"도착지: {order.DestCustName}\n\n" +
    //            $"입력 내용을 확인하신 후,\n" +
    //            $"직접 '닫기' 버튼으로 팝업을 닫아주세요.",
    //            "입력 완료 - 수동 확인 필요",
    //            System.Windows.MessageBoxButton.OK,
    //            System.Windows.MessageBoxImage.Information);

    //        Debug.WriteLine($"[{m_Context.AppName}] ===== 신규 주문 입력 완료 (저장 버튼 미클릭) =====");
    //        return RegistResult.SuccessResult();
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.WriteLine($"[{m_Context.AppName}] RegistOrderToPopupAsync 예외: {ex.Message}");
    //        return RegistResult.ErrorResult($"주문 입력 예외: {ex.Message}");
    //    }
    //}

    /// <summary>
    /// 신규 주문 처리 (Kai 주문이 인성에 없다고 가정)
    /// - 팝업창 열기
    /// - 주문 정보 입력
    /// </summary>
    //public async Task<RegistResult> CheckIsOrderAsync_AssumeKaiNewOrder(
    //    AutoAlloc item,
    //    CancelTokenControl ctrl)
    //{
    //    try
    //    {
    //        string orderState = item.NewOrder.OrderState;

    //        // 주문 상태 검증
    //        if (orderState != "접수" && orderState != "취소" && orderState != "대기")
    //        {
    //            return RegistResult.ErrorResult(
    //                $"처리할 수 없는 주문 상태: {orderState} (접수/취소/대기만 가능)");
    //        }

    //        Debug.WriteLine($"[{m_Context.AppName}] 신규 주문 처리 시작: KeyCode={item.KeyCode}, " +
    //                      $"상태={orderState}, 고객={item.NewOrder.CallCustName}");

    //        // 1. 팝업창 열기
    //        PopupResult popupResult = await OpenNewOrderPopupAsync(ctrl);
    //        if (!popupResult.Success)
    //        {
    //            return RegistResult.ErrorResult(
    //                $"팝업창 열기 실패: {popupResult.ErrorMessage}");
    //        }

    //        // 2. 주문 정보 입력
    //        RegistResult registResult = await RegistOrderToPopupAsync(
    //            item, popupResult.hWndPopup, ctrl);

    //        if (!registResult.Success)
    //        {
    //            // 실패 시 팝업창 닫기 시도
    //            // TODO: 팝업창 닫기 구현
    //            Debug.WriteLine($"[{m_Context.AppName}] 주문 입력 실패: {registResult.ErrorMessage}");
    //        }

    //        return registResult;
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.WriteLine($"[{m_Context.AppName}] CheckIsOrderAsync_AssumeKaiNewOrder 예외: {ex.Message}");
    //        return RegistResult.ErrorResult($"신규 주문 처리 예외: {ex.Message}");
    //    }
    //}
    #endregion
}
#nullable enable
