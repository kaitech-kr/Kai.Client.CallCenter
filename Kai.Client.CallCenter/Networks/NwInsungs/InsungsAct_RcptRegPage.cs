using Kai.Client.CallCenter.Classes;
using Kai.Client.CallCenter.Classes.Class_Master;
using Kai.Client.CallCenter.OfrWorks;
using Kai.Common.NetDll_WpfCtrl.NetOFR;
using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Services;
using System.Diagnostics;
using static Kai.Client.CallCenter.Classes.CommonVars;
using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgWnd;
using Draw = System.Drawing;
using DrawImg = System.Drawing.Imaging;

namespace Kai.Client.CallCenter.Networks.NwInsungs;
#nullable disable

public partial class InsungsAct_RcptRegPage
{
    #region Variables
    /// <summary>
    /// 컬럼 너비 허용 오차 (픽셀)
    /// </summary>
    private const int COLUMN_WIDTH_TOLERANCE = 1;

    /// <summary>
    /// Datagrid 헤더 상단 여백 (텍스트 없는 영역)
    /// </summary>
    private const int HEADER_GAB = 7;

    /// <summary>
    /// Datagrid 헤더 텍스트 영역 높이
    /// </summary>
    private const int HEADER_TEXT_HEIGHT = 18;

    /// <summary>
    /// Datagrid 컬럼 인덱스 상수 (변경 시 수동으로 같이 변경 필요)
    /// </summary>
    public const int c_nCol번호 = 0;
    public const int c_nCol상태 = 1;
    public const int c_nCol주문번호 = 2;
    public const int c_nCol기사전번 = 18;
    public const int c_nCol오더메모 = 19;
    public const int c_nColForClick = 3;  // 클릭용 컬럼

    /// <summary>
    /// 접수등록 Datagrid 컬럼 헤더 정보 (20개 컬럼)
    /// </summary>
    public readonly NwCommon_DgColumnHeader[] m_ReceiptDgHeaderInfos = new NwCommon_DgColumnHeader[]
    {
        new NwCommon_DgColumnHeader() { sName = "No", bOfrSeq = false, nWidth = 60 },
        new NwCommon_DgColumnHeader() { sName = "상태", bOfrSeq = false, nWidth = 70 },

        new NwCommon_DgColumnHeader() { sName = "주문번호", bOfrSeq = true, nWidth = 80  },
        new NwCommon_DgColumnHeader() { sName = "최초접수시간", bOfrSeq = true, nWidth = 90 },
        new NwCommon_DgColumnHeader() { sName = "접수시간", bOfrSeq = true, nWidth = 90  },

        new NwCommon_DgColumnHeader() { sName = "고객명", bOfrSeq = false, nWidth = 100 },
        new NwCommon_DgColumnHeader() { sName = "담당자", bOfrSeq = false, nWidth = 100 },

        new NwCommon_DgColumnHeader() { sName = "전화번호", bOfrSeq = true, nWidth = 100 },

        new NwCommon_DgColumnHeader() { sName = "출발동", bOfrSeq = false, nWidth = 120 },
        new NwCommon_DgColumnHeader() { sName = "도착동", bOfrSeq = false, nWidth = 120 },

        new NwCommon_DgColumnHeader() { sName = "요금", bOfrSeq = true, nWidth = 62 },

        new NwCommon_DgColumnHeader() { sName = "지급", bOfrSeq = false, nWidth = 40 },
        new NwCommon_DgColumnHeader() { sName = "형태", bOfrSeq = false, nWidth = 40 },
        new NwCommon_DgColumnHeader() { sName = "차량", bOfrSeq = false, nWidth = 60 },
        new NwCommon_DgColumnHeader() { sName = "계산서", bOfrSeq = false, nWidth = 50 },
        new NwCommon_DgColumnHeader() { sName = "왕복", bOfrSeq = false, nWidth = 40 },
        new NwCommon_DgColumnHeader() { sName = "공유", bOfrSeq = false, nWidth = 40 },
        new NwCommon_DgColumnHeader() { sName = "라이더", bOfrSeq = false, nWidth = 120 },
        new NwCommon_DgColumnHeader() { sName = "기사전번", bOfrSeq = true, nWidth = 100 },

        new NwCommon_DgColumnHeader() { sName = "오더메모", bOfrSeq = false, nWidth = 100 },

        new NwCommon_DgColumnHeader() { sName = "적요", bOfrSeq = false, nWidth = 240 },

    #region Temp Save
    //new NwCommon_DgColumnHeader() { sName = "배차", bOfrSeq = true },
    //new NwCommon_DgColumnHeader() { sName = "픽업", bOfrSeq = true },
    //new NwCommon_DgColumnHeader() { sName = "완료", bOfrSeq = true },

    //new NwCommon_DgColumnHeader() { sName = "기본", bOfrSeq = true },
    //new NwCommon_DgColumnHeader() { sName = "추가", bOfrSeq = true },
    //new NwCommon_DgColumnHeader() { sName = "할인", bOfrSeq = true },
    //new NwCommon_DgColumnHeader() { sName = "탁송", bOfrSeq = true },
    //new NwCommon_DgColumnHeader() { sName = "부가세", bOfrSeq = true },
    //new NwCommon_DgColumnHeader() { sName = "지급일", bOfrSeq = true },
    //new NwCommon_DgColumnHeader() { sName = "Km", bOfrSeq = true },
    //new NwCommon_DgColumnHeader() { sName = "국선번호", bOfrSeq = true },
    //new NwCommon_DgColumnHeader() { sName = "오더일자", bOfrSeq = true },
    //new NwCommon_DgColumnHeader() { sName = "산재보험", bOfrSeq = true },
    //new NwCommon_DgColumnHeader() { sName = "묶음", bOfrSeq = true },
    //new NwCommon_DgColumnHeader() { sName = "출", bOfrSeq = false },
    //new NwCommon_DgColumnHeader() { sName = "착",  bOfrSeq = false },

    //new NwCommon_DgColumnHeader() { sName = "접수자", bOfrSeq = false },
    //new NwCommon_DgColumnHeader() { sName = "부서명", bOfrSeq = false },
    //new NwCommon_DgColumnHeader() { sName = "출발지역", bOfrSeq = false },
    //new NwCommon_DgColumnHeader() { sName = "도착지역", bOfrSeq = false },
    //new NwCommon_DgColumnHeader() { sName = "기사그룹", bOfrSeq = false },
    //new NwCommon_DgColumnHeader() { sName = "결재", bOfrSeq = false },
    //new NwCommon_DgColumnHeader() { sName = "출발지", bOfrSeq = false },
    //new NwCommon_DgColumnHeader() { sName = "도착지", bOfrSeq = false },
    //new NwCommon_DgColumnHeader() { sName = "거래처명", bOfrSeq = false },
    //new NwCommon_DgColumnHeader() { sName = "고객메모", bOfrSeq = false },
    //new NwCommon_DgColumnHeader() { sName = "접수처", bOfrSeq = false },
    //new NwCommon_DgColumnHeader() { sName = "인수증", bOfrSeq = false },
    //new NwCommon_DgColumnHeader() { sName = "차량번호", bOfrSeq = false },
    #endregion Temp Save 끝
    };
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

    #region Constructor
    /// <summary>
    /// 생성자 - Context를 받아서 초기화
    /// </summary>
    public InsungsAct_RcptRegPage(InsungContext context)
    {
        m_Context = context ?? throw new ArgumentNullException(nameof(context));
        //Debug.WriteLine($"[InsungsAct_RcptRegPage] 생성자 호출: AppName={m_Context.AppName}");
    }
    #endregion

    #region 초기화용 함수들
    public async Task<StdResult_Error> InitializeAsync(bool bEdit = true, bool bWrite = true, bool bMsgBox = true)
    {
        try
        {
            Debug.WriteLine($"[InsungsAct_RcptRegPage] 초기화 시작");

            // 1. 바메뉴 클릭 - 접수등록 페이지 열기
            await m_Context.MainWndAct.ClickAsync접수등록();
            await Task.Delay(c_nWaitVeryLong); // 페이지가 열릴 때까지 대기
            Debug.WriteLine($"[InsungsAct_RcptRegPage] 접수등록 바메뉴 클릭 완료");

            // 2. TopWnd 찾기 - MdiClient의 자식으로 "접수현황" 찾기
            for (int i = 0; i < c_nRepeatVeryMany; i++) // 10초 동안 대기
            {
                m_RcptPage.TopWnd_hWnd = Std32Window.FindWindowEx(
                    m_Main.WndInfo_MdiClient.hWnd,
                    IntPtr.Zero,
                    null,
                    m_FileInfo.접수등록Page_TopWnd_sWndName
                );

                if (m_RcptPage.TopWnd_hWnd != IntPtr.Zero) break;
                await Task.Delay(c_nWaitNormal);
            }

            if (m_RcptPage.TopWnd_hWnd == IntPtr.Zero)
            {
                return CommonFuncs_StdResult.ErrMsgResult_Error(
                    $"[{m_Context.AppName}/RcptRegPage]접수등록Page 찾기실패: {m_FileInfo.접수등록Page_TopWnd_sWndName}",
                    "InsungsAct_RcptRegPage/InitializeAsync_01", bWrite, bMsgBox);
            }
            Debug.WriteLine($"[InsungsAct_RcptRegPage] 접수등록Page 찾음: {m_RcptPage.TopWnd_hWnd:X}");

            // 3. StatusBtn 찾기 - 텍스트 비교로 먼저 찾기
            await Task.Delay(c_nWaitVeryLong);
            for (int i = 1; i <= c_nRepeatVeryMany; i++)
            {
                await Task.Delay(c_nWaitNormal);

                m_RcptPage.StatusBtn_hWnd접수 = Std32Window.GetWndHandle_FromRelDrawPt(m_Main.TopWnd_hWnd, m_FileInfo.접수등록Page_StatusBtn_ptChkRel접수M);
                string sText = Std32Window.GetWindowText(m_RcptPage.StatusBtn_hWnd접수);
                if (sText == "접수") break;

                if (i == c_nRepeatVeryMany)
                    return CommonFuncs_StdResult.ErrMsgResult_Error($"접수버튼 텍스트 찾기 실패: [{sText}]", "InsungsAct_RcptRegPage/InitializeAsync_02", bWrite, bMsgBox);
            }

            // 나머지 버튼 핸들 찾기
            m_RcptPage.StatusBtn_hWnd배차 = Std32Window.GetWndHandle_FromRelDrawPt(m_Main.TopWnd_hWnd, m_FileInfo.접수등록Page_StatusBtn_ptChkRel배차M);
            m_RcptPage.StatusBtn_hWnd운행 = Std32Window.GetWndHandle_FromRelDrawPt(m_Main.TopWnd_hWnd, m_FileInfo.접수등록Page_StatusBtn_ptChkRel운행M);
            m_RcptPage.StatusBtn_hWnd완료 = Std32Window.GetWndHandle_FromRelDrawPt(m_Main.TopWnd_hWnd, m_FileInfo.접수등록Page_StatusBtn_ptChkRel완료M);
            m_RcptPage.StatusBtn_hWnd취소 = Std32Window.GetWndHandle_FromRelDrawPt(m_Main.TopWnd_hWnd, m_FileInfo.접수등록Page_StatusBtn_ptChkRel취소M);
            m_RcptPage.StatusBtn_hWnd전체 = Std32Window.GetWndHandle_FromRelDrawPt(m_Main.TopWnd_hWnd, m_FileInfo.접수등록Page_StatusBtn_ptChkRel전체M);

            // 4. 딜레이 후 이미지 비교로 버튼 Up/Down 상태 확인
            await Task.Delay(c_nWaitNormal);
            StdResult_NulBool resultNulBool;

            // 접수버튼 Up 상태 확인
            resultNulBool = await OfrWork_Insungs.OfrIsMatchedImage_DrawRelRectAsync(m_RcptPage.StatusBtn_hWnd접수, HEADER_GAB, "Img_접수버튼_Up", true, true, true);
            if (!StdConvert.NullableBoolToBool(resultNulBool.bResult))
                return CommonFuncs_StdResult.ErrMsgResult_Error("접수버튼 Up 이미지 매칭 실패", "InsungsAct_RcptRegPage/InitializeAsync_03", bWrite, bMsgBox);

            // 배차버튼 Up 상태 확인
            resultNulBool = await OfrWork_Insungs.OfrIsMatchedImage_DrawRelRectAsync(m_RcptPage.StatusBtn_hWnd배차, HEADER_GAB, "Img_배차버튼_Up", true, true, true);
            if (!StdConvert.NullableBoolToBool(resultNulBool.bResult))
                return CommonFuncs_StdResult.ErrMsgResult_Error("배차버튼 Up 이미지 매칭 실패", "InsungsAct_RcptRegPage/InitializeAsync_04", bWrite, bMsgBox);

            // 운행버튼 Up 상태 확인
            resultNulBool = await OfrWork_Insungs.OfrIsMatchedImage_DrawRelRectAsync(m_RcptPage.StatusBtn_hWnd운행, HEADER_GAB, "Img_운행버튼_Up", true, true, true);
            if (!StdConvert.NullableBoolToBool(resultNulBool.bResult))
                return CommonFuncs_StdResult.ErrMsgResult_Error("운행버튼 Up 이미지 매칭 실패", "InsungsAct_RcptRegPage/InitializeAsync_05", bWrite, bMsgBox);

            // 완료버튼 Up 상태 확인
            resultNulBool = await OfrWork_Insungs.OfrIsMatchedImage_DrawRelRectAsync(m_RcptPage.StatusBtn_hWnd완료, HEADER_GAB, "Img_완료버튼_Up", true, true, true);
            if (!StdConvert.NullableBoolToBool(resultNulBool.bResult))
                return CommonFuncs_StdResult.ErrMsgResult_Error("완료버튼 Up 이미지 매칭 실패", "InsungsAct_RcptRegPage/InitializeAsync_06", bWrite, bMsgBox);

            // 취소버튼 Up 상태 확인
            resultNulBool = await OfrWork_Insungs.OfrIsMatchedImage_DrawRelRectAsync(m_RcptPage.StatusBtn_hWnd취소, HEADER_GAB, "Img_취소버튼_Up", true, true, true);
            if (!StdConvert.NullableBoolToBool(resultNulBool.bResult))
                return CommonFuncs_StdResult.ErrMsgResult_Error("취소버튼 Up 이미지 매칭 실패", "InsungsAct_RcptRegPage/InitializeAsync_07", bWrite, bMsgBox);

            // 전체버튼 Up 상태 확인
            resultNulBool = await OfrWork_Insungs.OfrIsMatchedImage_DrawRelRectAsync(m_RcptPage.StatusBtn_hWnd전체, HEADER_GAB, "Img_전체버튼_Up", true, true, true);
            if (!StdConvert.NullableBoolToBool(resultNulBool.bResult))
                return CommonFuncs_StdResult.ErrMsgResult_Error("전체버튼 Up 이미지 매칭 실패", "InsungsAct_RcptRegPage/InitializeAsync_08", bWrite, bMsgBox);

            // 5. 전체버튼 클릭 → 접수버튼 Down 상태 확인 루프
            await Std32Mouse_Post.MousePostAsync_ClickLeft_Center(m_RcptPage.StatusBtn_hWnd전체);
            for (int i = 1; i <= c_nRepeatNormal; i++)
            {
                await Task.Delay(c_nWaitShort);

                resultNulBool = await OfrWork_Insungs.OfrIsMatchedImage_DrawRelRectAsync(
                    m_RcptPage.StatusBtn_hWnd접수, HEADER_GAB, "Img_접수버튼_Down",
                    i == c_nRepeatNormal, i == c_nRepeatNormal, i == c_nRepeatNormal);

                if (StdConvert.NullableBoolToBool(resultNulBool.bResult)) break;

                if (i == c_nRepeatNormal)
                    return CommonFuncs_StdResult.ErrMsgResult_Error(
                        "전체버튼 클릭 후 상태 변경 실패", "InsungsAct_RcptRegPage/InitializeAsync_09", bWrite, bMsgBox);
            }

            // 6. 나머지 버튼 Down 상태 확인
            // 배차버튼 Down 상태 확인
            resultNulBool = await OfrWork_Insungs.OfrIsMatchedImage_DrawRelRectAsync(m_RcptPage.StatusBtn_hWnd배차, HEADER_GAB, "Img_배차버튼_Down", true, true, true);
            if (!StdConvert.NullableBoolToBool(resultNulBool.bResult))
                return CommonFuncs_StdResult.ErrMsgResult_Error("배차버튼 Down 이미지 매칭 실패", "InsungsAct_RcptRegPage/InitializeAsync_10", bWrite, bMsgBox);

            // 운행버튼 Down 상태 확인
            resultNulBool = await OfrWork_Insungs.OfrIsMatchedImage_DrawRelRectAsync(m_RcptPage.StatusBtn_hWnd운행, HEADER_GAB, "Img_운행버튼_Down", true, true, true);
            if (!StdConvert.NullableBoolToBool(resultNulBool.bResult))
                return CommonFuncs_StdResult.ErrMsgResult_Error("운행버튼 Down 이미지 매칭 실패", "InsungsAct_RcptRegPage/InitializeAsync_11", bWrite, bMsgBox);

            // 완료버튼 Down 상태 확인
            resultNulBool = await OfrWork_Insungs.OfrIsMatchedImage_DrawRelRectAsync(m_RcptPage.StatusBtn_hWnd완료, HEADER_GAB, "Img_완료버튼_Down", true, true, true);
            if (!StdConvert.NullableBoolToBool(resultNulBool.bResult))
                return CommonFuncs_StdResult.ErrMsgResult_Error("완료버튼 Down 이미지 매칭 실패", "InsungsAct_RcptRegPage/InitializeAsync_12", bWrite, bMsgBox);

            // 취소버튼 Down 상태 확인
            resultNulBool = await OfrWork_Insungs.OfrIsMatchedImage_DrawRelRectAsync(m_RcptPage.StatusBtn_hWnd취소, HEADER_GAB, "Img_취소버튼_Down", true, true, true);
            if (!StdConvert.NullableBoolToBool(resultNulBool.bResult))
                return CommonFuncs_StdResult.ErrMsgResult_Error("취소버튼 Down 이미지 매칭 실패", "InsungsAct_RcptRegPage/InitializeAsync_13", bWrite, bMsgBox);

            // 전체버튼 Down 상태 확인
            resultNulBool = await OfrWork_Insungs.OfrIsMatchedImage_DrawRelRectAsync(m_RcptPage.StatusBtn_hWnd전체, HEADER_GAB, "Img_전체버튼_Down", true, true, true);
            if (!StdConvert.NullableBoolToBool(resultNulBool.bResult))
                return CommonFuncs_StdResult.ErrMsgResult_Error("전체버튼 Down 이미지 매칭 실패", "InsungsAct_RcptRegPage/InitializeAsync_14", bWrite, bMsgBox);

            // 5. CommandBtn 찾기 및 OFR 검증 (신규, 조회, 기사)
            var (hWnd신규, error신규) = await FindCommandButtonWithOfrAsync(
                "신규", m_FileInfo.접수등록Page_CmdBtn_ptChkRel신규M, "Img_신규버튼",
                "InsungsAct_RcptRegPage/InitializeAsync_08", bEdit, bWrite, bMsgBox);
            if (error신규 != null) return error신규;
            m_RcptPage.CmdBtn_hWnd신규 = hWnd신규;

            var (hWnd조회, error조회) = await FindCommandButtonWithOfrAsync(
                "조회", m_FileInfo.접수등록Page_CmdBtn_ptChkRel조회M, "Img_조회버튼",
                "InsungsAct_RcptRegPage/InitializeAsync_09", bEdit, bWrite, bMsgBox);
            if (error조회 != null) return error조회;
            m_RcptPage.CmdBtn_hWnd조회 = hWnd조회;

            var (hWnd기사, error기사) = await FindCommandButtonWithOfrAsync(
                "기사", m_FileInfo.접수등록Page_CmdBtn_ptChkRel기사M, "Img_기사버튼",
                "InsungsAct_RcptRegPage/InitializeAsync_10", bEdit, bWrite, bMsgBox);
            if (error기사 != null) return error기사;
            m_RcptPage.CmdBtn_hWnd기사 = hWnd기사;

            // 6. CallCount 핸들 찾기 (접수, 운행, 취소, 완료, 총계)
            m_RcptPage.CallCount_hWnd접수 = FindCallCountControl(
                "접수", m_FileInfo.접수등록Page_CallCount_ptChkRel접수M,
                "InsungsAct_RcptRegPage/InitializeAsync_11", bWrite, bMsgBox, out StdResult_Error error접수Count);
            if (error접수Count != null) return error접수Count;

            m_RcptPage.CallCount_hWnd운행 = FindCallCountControl(
                "운행", m_FileInfo.접수등록Page_CallCount_ptChkRel운행M,
                "InsungsAct_RcptRegPage/InitializeAsync_12", bWrite, bMsgBox, out StdResult_Error error운행Count);
            if (error운행Count != null) return error운행Count;

            m_RcptPage.CallCount_hWnd취소 = FindCallCountControl(
                "취소", m_FileInfo.접수등록Page_CallCount_ptChkRel취소M,
                "InsungsAct_RcptRegPage/InitializeAsync_13", bWrite, bMsgBox, out StdResult_Error error취소Count);
            if (error취소Count != null) return error취소Count;

            m_RcptPage.CallCount_hWnd완료 = FindCallCountControl(
                "완료", m_FileInfo.접수등록Page_CallCount_ptChkRel완료M,
                "InsungsAct_RcptRegPage/InitializeAsync_14", bWrite, bMsgBox, out StdResult_Error error완료Count);
            if (error완료Count != null) return error완료Count;

            m_RcptPage.CallCount_hWnd총계 = FindCallCountControl(
                "총계", m_FileInfo.접수등록Page_CallCount_ptChkRel총계M,
                "InsungsAct_RcptRegPage/InitializeAsync_15", bWrite, bMsgBox, out StdResult_Error error총계Count);
            if (error총계Count != null) return error총계Count;

            // 7. 오더 Datagrid 초기화
            // 7-1. Datagrid 핸들 찾기
            m_RcptPage.DG오더_hWnd = Std32Window.GetWndHandle_FromRelDrawPt(
                m_Main.TopWnd_hWnd, m_FileInfo.접수등록Page_DG오더_ptCenterRelM
            );
            if (m_RcptPage.DG오더_hWnd == IntPtr.Zero)
            {
                return CommonFuncs_StdResult.ErrMsgResult_Error(
                    $"[{m_Context.AppName}/RcptRegPage]Datagrid 찾기실패: {m_FileInfo.접수등록Page_DG오더_ptCenterRelM}",
                    "InsungsAct_RcptRegPage/InitializeAsync_16", bWrite, bMsgBox);
            }
            //Debug.WriteLine($"[InsungsAct_RcptRegPage] Datagrid 찾음: {m_RcptPage.DG오더_hWnd:X}");

            // 7-2. Datagrid 크기 확인
            m_RcptPage.DG오더_AbsRect = Std32Window.GetWindowRect_DrawAbs(m_RcptPage.DG오더_hWnd);
            if (m_RcptPage.DG오더_AbsRect.Width != m_FileInfo.접수등록Page_DG오더_rcRel.Width ||
                m_RcptPage.DG오더_AbsRect.Height != m_FileInfo.접수등록Page_DG오더_rcRel.Height)
            {
                return CommonFuncs_StdResult.ErrMsgResult_Error(
                    $"[{m_Context.AppName}/RcptRegPage]Datagrid 크기 불일치: " +
                    $"Expected({m_FileInfo.접수등록Page_DG오더_rcRel.Width}x{m_FileInfo.접수등록Page_DG오더_rcRel.Height}), " +
                    $"Actual({m_RcptPage.DG오더_AbsRect.Width}x{m_RcptPage.DG오더_AbsRect.Height})",
                    "InsungsAct_RcptRegPage/InitializeAsync_17", bWrite, bMsgBox);
            }
            //Debug.WriteLine($"[InsungsAct_RcptRegPage] Datagrid 크기 확인 완료: {m_RcptPage.DG오더_AbsRect.Width}x{m_RcptPage.DG오더_AbsRect.Height}");

            // 7-3. 수직스크롤바 핸들 찾기 (MainWnd 기준 상대좌표)
            m_RcptPage.DG오더_hWnd수직스크롤 = Std32Window.GetWndHandle_FromRelDrawPt(
                m_Main.TopWnd_hWnd, m_FileInfo.접수등록Page_DG오더_ptChkRel수직스크롤M
            );

            if (m_RcptPage.DG오더_hWnd수직스크롤 == IntPtr.Zero)
            {
                Debug.WriteLine($"[InsungsAct_RcptRegPage] 수직스크롤바 찾기 실패 (경고): {m_FileInfo.접수등록Page_DG오더_ptChkRel수직스크롤M}");
                // 스크롤바가 없을 수도 있으므로 경고만 출력하고 진행
            }
            else
            {
                Debug.WriteLine($"[InsungsAct_RcptRegPage] 수직스크롤바 찾음: {m_RcptPage.DG오더_hWnd수직스크롤:X}");
            }

            // 7-4. Datagrid 상세 정보 설정 (컬럼 헤더, RelChildRects)
            StdResult_Error resultDG = await SetDG오더RectsAsync(bEdit, bWrite, bMsgBox);
            if (resultDG != null)
                return resultDG; // 에러 발생 시 반환

            Debug.WriteLine($"[InsungsAct_RcptRegPage] 초기화 완료");
            return null; // 성공
        }
        catch (Exception ex)
        {
            return CommonFuncs_StdResult.ErrMsgResult_Error(
                $"[{m_Context.AppName}/RcptRegPage]예외발생: {ex.Message}",
                "InsungsAct_RcptRegPage/InitializeAsync_999", bWrite, bMsgBox);
        }
    }

    /// <summary>
    /// Datagrid 상세 영역 설정 (컬럼 헤더 읽기 + RelChildRects 계산 + 상태 검증)
    /// 원콜 방식: DG오더_hWnd 기준 헤더 영역만 캡처
    /// </summary>
    private async Task<StdResult_Error> SetDG오더RectsAsync(bool bEdit = true, bool bWrite = true, bool bMsgBox = true)
    {
        Draw.Bitmap bmpDG = null;

        try
        {
            Debug.WriteLine($"[InsungsAct_RcptRegPage] SetDG오더RectsAsync 시작");

            // 재시도 루프
            for (int retry = 1; retry <= c_nRepeatShort; retry++)
            {
                bool bShowMsgBox = (retry == c_nRepeatShort) && bMsgBox;

                if (retry > 1)
                {
                    Debug.WriteLine($"[InsungsAct_RcptRegPage] Datagrid 재시도 {retry}/{c_nRepeatShort}");
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
                        Debug.WriteLine($"[InsungsAct_RcptRegPage] DG오더 캡처 실패 (재시도 {retry}/{c_nRepeatShort})");
                        await Task.Delay(200);
                        continue;
                    }
                    return CommonFuncs_StdResult.ErrMsgResult_Error(
                        $"[{m_Context.AppName}/RcptRegPage]DG오더 캡처 실패",
                        "InsungsAct_RcptRegPage/SetDG오더RectsAsync_01", bWrite, bShowMsgBox);
                }

                Debug.WriteLine($"[InsungsAct_RcptRegPage] DG오더 캡처 성공: {bmpDG.Width}x{bmpDG.Height}");

                // 2. 컬럼 경계 검출 (상단 여백 중간에서 검출)
                const int headerGab = 7;
                int textHeight = headerHeight - (headerGab * 2);
                int targetRow = headerGab / 2;  // 상단 여백 중간 (3)

                byte minBrightness = OfrService.GetMinBrightnessAtRow_FromColorBitmapFast(bmpDG, targetRow);

                if (minBrightness == 255)
                {
                    return CommonFuncs_StdResult.ErrMsgResult_Error(
                        $"[{m_Context.AppName}/RcptRegPage]최소 밝기 검출 실패",
                        "InsungsAct_RcptRegPage/SetDG오더RectsAsync_02", bWrite, bMsgBox);
                }

                minBrightness += 2;

                // 2-2. Bool 배열 생성
                bool[] boolArr = OfrService.GetBoolArray_FromColorBitmapRowFast(bmpDG, targetRow, minBrightness, 2);

                if (boolArr == null || boolArr.Length == 0)
                {
                    return CommonFuncs_StdResult.ErrMsgResult_Error(
                        $"[{m_Context.AppName}/RcptRegPage]Bool 배열 생성 실패",
                        "InsungsAct_RcptRegPage/SetDG오더RectsAsync_03", bWrite, bMsgBox);
                }

                // 2-3. 컬럼 경계 리스트 추출
                List<OfrModel_LeftWidth> listLW = OfrService.GetLeftWidthList_FromBool1Array(boolArr, minBrightness);

                if (listLW == null || listLW.Count < 2)
                {
                    return CommonFuncs_StdResult.ErrMsgResult_Error(
                        $"[{m_Context.AppName}/RcptRegPage]컬럼 경계 검출 실패: Count={listLW?.Count ?? 0}",
                        "InsungsAct_RcptRegPage/SetDG오더RectsAsync_04", bWrite, bMsgBox);
                }

                // 마지막 항목 제거 (오른쪽 끝 경계)
                listLW.RemoveAt(listLW.Count - 1);

                int columns = listLW.Count;
                //Debug.WriteLine($"[InsungsAct_RcptRegPage] 컬럼 검출: {columns}개 (목표: {m_ReceiptDgHeaderInfos.Length}개)");

                // 컬럼 개수 확인
                if (columns != m_ReceiptDgHeaderInfos.Length)
                {
                    Debug.WriteLine($"[InsungsAct_RcptRegPage] 컬럼 개수 불일치: 검출={columns}개, 예상={m_ReceiptDgHeaderInfos.Length}개 (재시도 {retry}/{c_nRepeatShort})");

                    bmpDG?.Dispose();

                    StdResult_Error initResult = await InitDG오더Async(
                        CEnum_DgValidationIssue.InvalidColumnCount,
                        bEdit, bWrite, bMsgBox: false);

                    if (initResult != null)
                    {
                        if (retry == c_nRepeatShort)
                        {
                            return CommonFuncs_StdResult.ErrMsgResult_Error(
                                $"[{m_Context.AppName}/RcptRegPage]컬럼 개수 불일치: 검출={columns}개, 예상={m_ReceiptDgHeaderInfos.Length}개 (재시도 {c_nRepeatShort}회 초과)",
                                "InsungsAct_RcptRegPage/SetDG오더RectsAsync_05", bWrite, bShowMsgBox);
                        }
                    }

                    await Task.Delay(200);
                    continue;
                }

                // 3. 컬럼 헤더 OFR (원콜 방식: OfrStr_ComplexCharSetAsync 사용)
                m_RcptPage.DG오더_ColumnTexts = new string[columns];

                for (int i = 0; i < columns; i++)
                {
                    Draw.Rectangle rcColHeader = new Draw.Rectangle(listLW[i].nLeft, headerGab, listLW[i].nWidth, textHeight);
                    var result = await OfrWork_Common.OfrStr_ComplexCharSetAsync(bmpDG, rcColHeader, bInvertRgb: false, bTextSave: true, dWeight: 0.9, bEdit: bEdit);

                    m_RcptPage.DG오더_ColumnTexts[i] = result?.strResult ?? string.Empty;
                }

                Debug.WriteLine($"[InsungsAct_RcptRegPage] OFR 완료: {string.Join(", ", m_RcptPage.DG오더_ColumnTexts)}");

                // 4. Datagrid 상태 검증
                Debug.WriteLine($"[InsungsAct_RcptRegPage] Datagrid 상태 검증 시작");
                CEnum_DgValidationIssue validationIssues = ValidateDatagridState(m_RcptPage.DG오더_ColumnTexts, listLW);

                if (validationIssues != CEnum_DgValidationIssue.None)
                {
                    Debug.WriteLine($"[InsungsAct_RcptRegPage] Datagrid 상태 검증 실패: {validationIssues} (재시도 {retry}/{c_nRepeatShort})");

                    bmpDG?.Dispose();

                    StdResult_Error initResult = await InitDG오더Async(validationIssues, bEdit, bWrite, bMsgBox: false);

                    if (initResult != null)
                    {
                        if (retry == c_nRepeatShort)
                        {
                            return CommonFuncs_StdResult.ErrMsgResult_Error(
                                $"[{m_Context.AppName}/RcptRegPage]Datagrid 상태 검증 실패: {validationIssues} (재시도 {c_nRepeatShort}회 초과)",
                                "InsungsAct_RcptRegPage/SetDG오더RectsAsync_Validation", bWrite, bShowMsgBox);
                        }
                    }

                    await Task.Delay(200);
                    continue;
                }

                //Debug.WriteLine($"[InsungsAct_RcptRegPage] 모든 컬럼 검증 완료!");

                // 5. RelChildRects 생성
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
                    m_RcptPage.DG오더_RelChildRects[col, 0] = new Draw.Rectangle(
                        listLW[col].nLeft + 1,
                        headerGab,
                        listLW[col].nWidth - 2,
                        headerHeight - (headerGab * 2)
                    );

                    // Row 1: Empty
                    m_RcptPage.DG오더_RelChildRects[col, 1] = new Draw.Rectangle(
                        listLW[col].nLeft + 1,
                        headerHeight + 1,
                        listLW[col].nWidth - 2,
                        dataHeight
                    );

                    // Row 2~29: Data rows
                    for (int row = 2; row < rows + 2; row++)
                    {
                        int cellY = headerHeight + emptyRowHeight + ((row - 2) * dataRowHeight) + 1;

                        m_RcptPage.DG오더_RelChildRects[col, row] = new Draw.Rectangle(
                            listLW[col].nLeft + 1,
                            cellY,
                            listLW[col].nWidth - 2,
                            dataHeight
                        );
                    }
                }

                //Debug.WriteLine($"[InsungsAct_RcptRegPage] RelChildRects 생성 완료: {columns}열 x {rows}행, headerGab={headerGab}, dataHeight={dataHeight}");

                // 6. Background Brightness 계산 (데이터그리드 중심 위치)
                m_RcptPage.DG오더_nBackgroundBright = OfrService.GetCenterPixelBrightnessFrmWndHandle(m_RcptPage.DG오더_hWnd);
                //Debug.WriteLine($"[InsungsAct_RcptRegPage] Background Brightness: {m_RcptPage.DG오더_nBackgroundBright}");

                //Debug.WriteLine($"[InsungsAct_RcptRegPage] SetDG오더RectsAsync 완료");
                break;
            }

            return null;
        }
        catch (Exception ex)
        {
            return CommonFuncs_StdResult.ErrMsgResult_Error(
                $"[{m_Context.AppName}/RcptRegPage]SetDG오더RectsAsync 예외발생: {ex.Message}",
                "InsungsAct_RcptRegPage/SetDG오더RectsAsync_999", bWrite, bMsgBox);
        }
        finally
        {
            bmpDG?.Dispose();
        }
    }

    /// <summary>
    /// Datagrid 강제 초기화 (Context 메뉴 → "접수화면초기화" 클릭 → 컬럼 조정)
    /// </summary>
    /// <param name="issues">검증 이슈 플래그</param>
    /// <param name="bEdit">편집 허용 여부</param>
    /// <summary>
    /// 헤더 캡처 및 컬럼 경계 검출 헬퍼
    /// </summary>
    private (Draw.Bitmap bmpHeader, List<OfrModel_LeftWidth> listLW, int columns) CaptureAndDetectColumnBoundaries(Draw.Rectangle rcHeader, int targetRow)
    {
        Draw.Bitmap bmpHeader = OfrService.CaptureScreenRect_InWndHandle(m_RcptPage.DG오더_hWnd, rcHeader);
        if (bmpHeader == null) return (null, null, 0);

        byte minBrightness = OfrService.GetMinBrightnessAtRow_FromColorBitmapFast(bmpHeader, targetRow);
        minBrightness += 2;

        bool[] boolArr = OfrService.GetBoolArray_FromColorBitmapRowFast(bmpHeader, targetRow, minBrightness, 2);
        List<OfrModel_LeftWidth> listLW = OfrService.GetLeftWidthList_FromBool1Array(boolArr, minBrightness);

        if (listLW == null || listLW.Count < 2)
            return (bmpHeader, listLW, 0);

        int columns = listLW.Count - 1;
        return (bmpHeader, listLW, columns);
    }

    /// <summary>
    /// 모든 컬럼 OFR 헬퍼
    /// </summary>
    private async Task<string[]> OfrAllColumnsAsync(Draw.Bitmap bmpHeader, List<OfrModel_LeftWidth> listLW, int columns, int gab, int height, bool bEdit = false)
    {
        string[] texts = new string[columns];

        for (int x = 0; x < columns; x++)
        {
            Draw.Rectangle rcColHeader = new Draw.Rectangle(listLW[x].nLeft, gab, listLW[x].nWidth, height);
            var result = await OfrWork_Common.OfrStr_ComplexCharSetAsync(bmpHeader, rcColHeader, bInvertRgb: false, bTextSave: true, dWeight: 0.9, bEdit: bEdit);

            texts[x] = result?.strResult;
        }

        return texts;
    }

    /// <param name="bWrite">로그 작성 여부</param>
    /// <param name="bMsgBox">메시지박스 표시 여부</param>
    /// <returns>에러 발생 시 StdResult_Error, 성공 시 null</returns>
    private async Task<StdResult_Error> InitDG오더Async(CEnum_DgValidationIssue issues, bool bEdit = true, bool bWrite = true, bool bMsgBox = true)
    {
        // 마우스 커서 위치 백업 (작업 완료 후 복원용)
        Draw.Point ptCursorBackup = Std32Cursor.GetCursorPos_AbsDrawPt();

        try
        {
            // 초기화 전체 기간 동안 외부 입력 차단
            Simulation_Mouse.SafeBlockInputStart();

            // DG오더_hWnd 기준으로 헤더 영역 정의
            Draw.Rectangle rcDG = Std32Window.GetWindowRect_DrawAbs(m_RcptPage.DG오더_hWnd);
            Draw.Rectangle rcHeader = new Draw.Rectangle(
                0, 0,  // DG 윈도우 내부 시작점
                rcDG.Width,
                m_FileInfo.접수등록Page_DG오더_headerHeight
            );
            //Debug.WriteLine($"[InitDG오더] rcHeader: {rcHeader}");

            // Step 1: 사전작업 - "접수화면초기화" 클릭
            Debug.WriteLine("[InitDG오더] Step 1: 접수화면초기화 시작");

            // 1-1. 우클릭
            await Std32Mouse_Post.MousePostAsync_ClickRight(m_RcptPage.DG오더_hWnd);
            //Debug.WriteLine("[InitDG오더] 1-1. 우클릭 완료");

            // 1-2. Context 메뉴 대기 (100회 폴링, 2초) - 프로세스 ID로 구분
            IntPtr hWndMenu = IntPtr.Zero;
            for (int i = 0; i < 100; i++)
            {
                await Task.Delay(20);
                hWndMenu = Std32Window.FindMainWindow_StartsWith(
                    m_Context.MemInfo.Splash.TopWnd_uProcessId,
                    m_FileInfo.Main_AnyMenu_sClassName,
                    m_FileInfo.Main_AnyMenu_sWndName);
                if (hWndMenu != IntPtr.Zero) break;
            }

            if (hWndMenu == IntPtr.Zero)
            {
                return CommonFuncs_StdResult.ErrMsgResult_Error(
                    "[InitDG오더]Context 메뉴 찾기 실패",
                    "InsungsAct_RcptRegPage/InitDG오더Async_01", bWrite, true);
            }

            //Debug.WriteLine($"[InitDG오더] 1-2. Context 메뉴 찾음: {hWndMenu:X}");

            // 1-3. "접수화면초기화" 메뉴 클릭 (2개 서브메뉴 중 위쪽, 좌클릭)
            // 첫 번째 시도: DOWN-UP 간 딜레이 50ms
            await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndMenu, 10, 12, 50);

            // 1-4. 확인 다이얼로그 대기 (첫 번째 시도) - 프로세스 ID로 구분
            IntPtr hWndDialog = IntPtr.Zero;
            for (int i = 0; i < CommonVars.c_nRepeatVeryMany; i++)
            {
                await Task.Delay(CommonVars.c_nWaitShort);
                hWndDialog = Std32Window.FindMainWindow(m_Context.MemInfo.Splash.TopWnd_uProcessId, "#32770", "확인");
                if (hWndDialog != IntPtr.Zero)
                {
                    break;
                }
            }

            // 확인창이 안 뜨면 두 번째 시도
            if (hWndDialog == IntPtr.Zero)
            {
                Debug.WriteLine($"[InitDG오더] MousePost 첫 번째 실패, DOWN-UP 100ms 딜레이로 재시도");
                await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndMenu, 10, 12, 100);

                // 다시 확인 다이얼로그 대기
                for (int i = 0; i < CommonVars.c_nRepeatVeryMany; i++)
                {
                    await Task.Delay(CommonVars.c_nWaitShort);
                    hWndDialog = Std32Window.FindMainWindow(m_Context.MemInfo.Splash.TopWnd_uProcessId, "#32770", "확인");
                    if (hWndDialog != IntPtr.Zero)
                    {
                        break;
                    }
                }
            }

            // 확인창이 안 뜨면 에러
            if (hWndDialog == IntPtr.Zero)
            {
                return CommonFuncs_StdResult.ErrMsgResult_Error(
                    "[InitDG오더]확인 다이얼로그 찾기 실패",
                    "InsungsAct_RcptRegPage/InitDG오더Async_03", bWrite, true);
            }

            Debug.WriteLine($"[InitDG오더] 확인 다이얼로그 찾음: {hWndDialog:X}");

            // "예(&Y)" 버튼 찾기
            IntPtr hWndBtn = Std32Window.FindWindowEx(
                hWndDialog, IntPtr.Zero, "Button", "예(&Y)");
            if (hWndBtn == IntPtr.Zero)
            {
                return CommonFuncs_StdResult.ErrMsgResult_Error(
                    "[InitDG오더]'예' 버튼 찾기 실패",
                    "InsungsAct_RcptRegPage/InitDG오더Async_02",
                    bWrite, true);
            }

            Debug.WriteLine($"[InitDG오더] '예' 버튼 찾음: {hWndBtn:X}, PostMessage 방식으로 클릭");

            // PostMessage 방식 사용 (인성1, 인성2 공통)
            await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndBtn, 5, 5, 50);
            await Task.Delay(200);

            // 1-5. 확인 다이얼로그 사라질 때까지 대기 - 프로세스 ID로 구분
            for (int i = 0; i < CommonVars.c_nRepeatMany; i++)
            {
                await Task.Delay(CommonVars.c_nWaitShort);
                hWndDialog = Std32Window.FindMainWindow(m_Context.MemInfo.Splash.TopWnd_uProcessId, "#32770", "확인");
                if (hWndDialog == IntPtr.Zero) break;
            }

            if (hWndDialog != IntPtr.Zero)
            {
                return CommonFuncs_StdResult.ErrMsgResult_Error(
                    "[InitDG오더]확인 다이얼로그 사라지기 실패",
                    "InsungsAct_RcptRegPage/InitDG오더Async_04", bWrite, true);
            }

            //Debug.WriteLine("[InitDG오더] 1-5. 접수화면초기화 완료");

            // 1-6. 초기화 반영 대기
            await Task.Delay(c_nWaitVeryLong);

            // Step 2: 불필요한 컬럼을 우측으로 이동 (15회 반복)
            Debug.WriteLine("[InitDG오더] Step 2: 불필요한 컬럼 우측 이동 시작");

            int headerHeight = m_FileInfo.접수등록Page_DG오더_headerHeight;
            const int headerGab = 7;
            int textHeight = headerHeight - (headerGab * 2);
            int targetRow = headerGab / 2;  // 상단 여백 중간 (3)
            const int center = 15;     // 대충 가운데

            try
            {
            for (int iteration = 0; iteration < 5; iteration++)
            {
                // 2-1. 헤더 캡처 및 컬럼 경계 검출
                await Task.Delay(CommonVars.c_nWaitNormal);
                var (bmpHeader, listLW, columns) = CaptureAndDetectColumnBoundaries(rcHeader, targetRow);
                if (bmpHeader == null)
                {
                    return CommonFuncs_StdResult.ErrMsgResult_Error(
                        $"[InitDG오더]헤더 캡처 실패 (반복 {iteration + 1})",
                        "InsungsAct_RcptRegPage/InitDG오더Async_05", bWrite, true);
                }

                // 2-2. 각 컬럼 텍스트 인식
                string[] texts = await OfrAllColumnsAsync(bmpHeader, listLW, columns, headerGab, textHeight, bEdit);
                Draw.Rectangle[] rcHeaders = new Draw.Rectangle[columns];
                for (int x = 0; x < columns; x++)
                {
                    rcHeaders[x] = new Draw.Rectangle(listLW[x].nLeft, headerGab, listLW[x].nWidth, textHeight);
                }

                // 2-4. 우측에서 좌측으로 검사하여 불필요한 컬럼 제거
                int removedCount = 0;
                for (int x = columns - 1; x >= 0; x--)
                {
                    // m_ReceiptDgHeaderInfos에 존재하는지 확인
                    int index = m_ReceiptDgHeaderInfos
                        .Select((value, idx) => new { value, idx })
                        .Where(z => z.value.sName == texts[x])
                        .Select(z => z.idx)
                        .DefaultIfEmpty(-1)
                        .First();

                    if (index < 0) // 필요없는 컬럼 발견
                    {
                        removedCount++;
                        //Debug.WriteLine(
                        //    $"[InitDG오더] 2-{iteration+1}. 불필요 컬럼 발견: [{x}]{texts[x]} → 제거");

                        // 수직 드래그로 우측 이동 (위로 드래그하여 제거)
                        Draw.Point ptCenter = StdUtil.GetCenterDrawPoint(rcHeaders[x]);
                        await Simulation_Mouse.SafeMouseEvent_DragLeft_Smooth_VerticalAsync(
                            m_RcptPage.DG오더_hWnd, ptCenter, -50, false);

                        await Task.Delay(c_nWaitShort);
                    }
                }

                bmpHeader.Dispose();

                // 2-5. 종료 조건: 원하는 컬럼이 모두 화면에 보일 때
                bool allColumnsVisible = true;
                string missingColumn = "";
                for (int i = 0; i < m_ReceiptDgHeaderInfos.Length; i++)
                {
                    if (!texts.Contains(m_ReceiptDgHeaderInfos[i].sName))
                    {
                        allColumnsVisible = false;
                        missingColumn = m_ReceiptDgHeaderInfos[i].sName;
                        Debug.WriteLine($"[InitDG오더] 2-{iteration+1}. 누락 컬럼: '{missingColumn}'");
                        break;
                    }
                }

                if (allColumnsVisible)
                {
                    Debug.WriteLine($"[InitDG오더] 2-{iteration+1}. 원하는 컬럼 모두 표시됨, Step 2 완료");
                    break;
                }

                //Debug.WriteLine($"[InitDG오더] 2-{iteration+1}. {workCount}개 컬럼 이동 완료");
            }
            }
            finally
            {
                // 외부입력차단 강제 해제 (예외 발생 시 안전장치)
                Simulation_Mouse.SafeBlockInputForceStop();
            }

            // Step 2-끝: 컬럼 폭 드래그 조정 (반복 - 원하는 컬럼 수까지)
            Debug.WriteLine("[InitDG오더] Step 2-끝: 컬럼 폭 조정 시작");

            try
            {
            for (int widthIter = 0; widthIter < 5; widthIter++)
            {
                // 매 반복 시 캡처 전 안정화 대기
                await Task.Delay(CommonVars.c_nWaitNormal);

                // 1. 캡처 및 경계선 검출
                var (bmpHeader, listLW, columns) = CaptureAndDetectColumnBoundaries(rcHeader, targetRow);
                if (bmpHeader == null)
                {
                    return new StdResult_Error("헤더 캡쳐 실패", "InsungsAct_RcptRegPage/InitDG오더Async_Step2End_01");
                }

                Debug.WriteLine($"[InitDG오더] Step 2-끝-{widthIter + 1}. 현재 컬럼 수: {columns}, 목표: {m_ReceiptDgHeaderInfos.Length}");

                // 2. 모든 컬럼 텍스트 인식
                string[] texts = await OfrAllColumnsAsync(bmpHeader, listLW, columns, headerGab, textHeight, true);
                Draw.Rectangle[] rcHeaders = new Draw.Rectangle[columns];
                for (int x = 0; x < columns; x++)
                {
                    rcHeaders[x] = new Draw.Rectangle(listLW[x].nLeft, headerGab, listLW[x].nWidth, textHeight);
                    Debug.WriteLine($"[InitDG오더] Step 2-끝-{widthIter + 1}. 컬럼[{x}] 인식: '{texts[x]}'");
                }

                // 3. 불필요한 컬럼 제거
                int removedCount = 0;
                for (int x = columns - 1; x >= 0; x--)
                {
                    // m_ReceiptDgHeaderInfos에 존재하는지 확인
                    int index = m_ReceiptDgHeaderInfos
                        .Select((value, idx) => new { value, idx })
                        .Where(z => z.value.sName == texts[x])
                        .Select(z => z.idx)
                        .DefaultIfEmpty(-1)
                        .First();

                    if (index < 0) // 필요없는 컬럼 발견
                    {
                        removedCount++;
                        Debug.WriteLine($"[InitDG오더] Step 2-끝-{widthIter + 1}. 불필요 컬럼 발견: [{x}]{texts[x]} → 제거");

                        // 수직 드래그로 우측 이동 (위로 드래그하여 제거)
                        Draw.Point ptCenter = StdUtil.GetCenterDrawPoint(rcHeaders[x]);
                        await Simulation_Mouse.SafeMouseEvent_DragLeft_Smooth_VerticalAsync(
                            m_RcptPage.DG오더_hWnd, ptCenter, -50, false);

                        await Task.Delay(c_nWaitShort);
                    }
                }

                if (removedCount > 0)
                {
                    Debug.WriteLine($"[InitDG오더] Step 2-끝-{widthIter + 1}. {removedCount}개 불필요 컬럼 제거 완료");

                    // 컬럼 제거 후 화면 안정화 대기
                    await Task.Delay(CommonVars.c_nWaitLong);

                    // 컬럼 제거 후 다시 캡처 및 경계선 검출
                    bmpHeader.Dispose();
                    var result = CaptureAndDetectColumnBoundaries(rcHeader, targetRow);
                    bmpHeader = result.bmpHeader;
                    listLW = result.listLW;
                    columns = result.columns;

                    if (bmpHeader == null)
                    {
                        return new StdResult_Error("컬럼 제거 후 재캡처 실패", "InsungsAct_RcptRegPage/InitDG오더Async_Step2End_01b");
                    }

                    // 텍스트 재인식
                    texts = await OfrAllColumnsAsync(bmpHeader, listLW, columns, headerGab, textHeight, true);
                    Debug.WriteLine($"[InitDG오더] Step 2-끝-{widthIter + 1}. 재캡처 후 컬럼 수: {columns}");
                }

                // 4. 폭 조정
                // 우측에서 좌측으로 처리
                for (int x = columns - 1; x >= 0; x--)
                {
                    // 경계선 위치 = (x+1)번 경계선의 Left
                    int boundaryX = listLW[x + 1].nLeft;

                    // texts[x]로 m_ReceiptDgHeaderInfos에서 매칭하여 문자수 구하기
                    int textLength = 0;
                    string columnName = texts[x];

                    if (!string.IsNullOrEmpty(columnName))
                    {
                        var matched = m_ReceiptDgHeaderInfos
                            .FirstOrDefault(h => h.sName == columnName);

                        if (matched != null)
                        {
                            textLength = matched.sName.Length;
                        }
                        else
                        {
                            textLength = columnName.Length; // 매칭 안 되면 실제 텍스트 길이 사용
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
                        m_RcptPage.DG오더_hWnd, ptStart, dx, bBkCursor: false, nMiliSec: 100);

                    await Task.Delay(c_nWaitNormal);

                }

                bmpHeader.Dispose();
                Debug.WriteLine($"[InitDG오더] Step 2-끝-{widthIter + 1}. {columns}개 컬럼 폭 조정 완료");

                // 5. 폭 조정 후 다시 캡처하여 원하는 컬럼 개수 확인
                await Task.Delay(CommonVars.c_nWaitShort);
                var (bmpHeader2, listLW2, columns2) = CaptureAndDetectColumnBoundaries(rcHeader, targetRow);
                if (bmpHeader2 == null)
                {
                    return new StdResult_Error("폭 조정 후 헤더 캡쳐 실패", "InsungsAct_RcptRegPage/InitDG오더Async_Step2End_02");
                }

                string[] texts2 = await OfrAllColumnsAsync(bmpHeader2, listLW2, columns2, headerGab, textHeight, true);

                // 원하는 컬럼 개수 확인
                int matchedCount = 0;
                List<string> matchedColumns = new List<string>();
                List<string> missingColumns = new List<string>();

                for (int i = 0; i < m_ReceiptDgHeaderInfos.Length; i++)
                {
                    bool found = false;
                    for (int x = 0; x < columns2; x++)
                    {
                        if (texts2[x] == m_ReceiptDgHeaderInfos[i].sName)
                        {
                            matchedCount++;
                            matchedColumns.Add(m_ReceiptDgHeaderInfos[i].sName);
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        missingColumns.Add(m_ReceiptDgHeaderInfos[i].sName);
                    }
                }

                Debug.WriteLine($"[InitDG오더] Step 2-끝-{widthIter + 1}. 원하는 컬럼 획득: {matchedCount}/{m_ReceiptDgHeaderInfos.Length}");
                if (missingColumns.Count > 0)
                {
                    Debug.WriteLine($"[InitDG오더] - 누락 컬럼({missingColumns.Count}): {string.Join(", ", missingColumns)}");
                }
                Debug.WriteLine($"[InitDG오더] - 현재 검출된 모든 컬럼({columns2}): {string.Join(", ", texts2.Where(t => !string.IsNullOrEmpty(t)))}");

                bmpHeader2.Dispose();

                // 원하는 컬럼을 모두 얻었으면 종료
                if (matchedCount >= m_ReceiptDgHeaderInfos.Length)
                {
                    Debug.WriteLine($"[InitDG오더] Step 2-끝: 목표 컬럼 모두 획득, 종료");
                    break;
                }
            }

            // Step 2-끝 루프 종료 후 검증
            Debug.WriteLine("[InitDG오더] Step 2-끝: 반복 루프 종료");
            {
                // 최종 확인을 위해 다시 캡처 및 컬럼 검출
                await Task.Delay(CommonVars.c_nWaitShort);
                var (bmpCheck, listLW, checkColumns) = CaptureAndDetectColumnBoundaries(rcHeader, targetRow);
                if (bmpCheck == null)
                {
                    return new StdResult_Error("Step 2-끝 검증 캡처 실패", "InsungsAct_RcptRegPage/InitDG오더Async_Step2End_Check");
                }

                // OFR로 컬럼명 확인
                string[] checkTexts = await OfrAllColumnsAsync(bmpCheck, listLW, checkColumns, headerGab, textHeight, true);
                bmpCheck.Dispose();

                // 원하는 컬럼 개수 확인
                int checkMatchedCount = 0;
                List<string> checkMissingColumns = new List<string>();
                for (int i = 0; i < m_ReceiptDgHeaderInfos.Length; i++)
                {
                    bool found = false;
                    for (int x = 0; x < checkColumns; x++)
                    {
                        if (checkTexts[x] == m_ReceiptDgHeaderInfos[i].sName)
                        {
                            checkMatchedCount++;
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        checkMissingColumns.Add(m_ReceiptDgHeaderInfos[i].sName);
                    }
                }

                Debug.WriteLine($"[InitDG오더] Step 2-끝 검증: 원하는 컬럼 획득 {checkMatchedCount}/{m_ReceiptDgHeaderInfos.Length}");

                if (checkMatchedCount < m_ReceiptDgHeaderInfos.Length)
                {
                    string errorMsg = $"[InitDG오더] Step 2-끝: 5회 반복 후에도 목표 컬럼 획득 실패\n\n";
                    errorMsg += $"획득 컬럼: {checkMatchedCount}/{m_ReceiptDgHeaderInfos.Length}\n";
                    errorMsg += $"누락 컬럼({checkMissingColumns.Count}): {string.Join(", ", checkMissingColumns)}\n\n";
                    errorMsg += $"현재 검출된 컬럼({checkColumns}): {string.Join(", ", checkTexts.Where(t => !string.IsNullOrEmpty(t)))}";

                    return new StdResult_Error(errorMsg, "InsungsAct_RcptRegPage/InitDG오더Async_Step2End_Failed");
                }
            }
            }
            finally
            {
                // 외부입력차단 강제 해제 (예외 발생 시 안전장치)
                Simulation_Mouse.SafeBlockInputForceStop();
            }

            Debug.WriteLine("[InitDG오더] Step 2 완료");


            // Step 3: 컬럼 순서 조정
            Debug.WriteLine("[InitDG오더] Step 3: 컬럼 순서 조정 시작");

            for (int x = 0; x < m_ReceiptDgHeaderInfos.Length; x++)
            {
                // 매 반복 시 캡처 전 안정화 대기
                await Task.Delay(CommonVars.c_nWaitShort);

                //Debug.WriteLine($"[InitDG오더] 3-{x+1}. 목표 컬럼: [{x}]{m_ReceiptDgHeaderInfos[x].sName}");

                // 3-1. 헤더 캡처 및 컬럼 경계 검출
                var (bmpHeader, listLW, columns) = CaptureAndDetectColumnBoundaries(rcHeader, targetRow);
                if (bmpHeader == null)
                {
                    return CommonFuncs_StdResult.ErrMsgResult_Error(
                        $"[InitDG오더]Step 3 헤더 캡처 실패 (컬럼 {x})",
                        "InsungsAct_RcptRegPage/InitDG오더Async_07", bWrite, true);
                }

                // 3-3. 목표 컬럼 텍스트 찾기
                string targetText = m_ReceiptDgHeaderInfos[x].sName;
                int index = -1;

                // 디버그: 검출 컬럼 수 vs 기대 컬럼 수
                if (targetText == "기사전번")
                {
                    Debug.WriteLine($"[InitDG오더] Step 3-{x+1}. '{targetText}' 찾기 시작");
                    Debug.WriteLine($"[InitDG오더] - 검출 컬럼 수: {columns} (listLW.Count={listLW.Count})");
                    Debug.WriteLine($"[InitDG오더] - 기대 컬럼 수: {m_ReceiptDgHeaderInfos.Length}");
                    Debug.WriteLine($"[InitDG오더] - 스캔 범위: 0~{columns - 1}");
                }

                for (int tx = 0; tx < columns; tx++)
                {
                    Draw.Rectangle rcColHeader = new Draw.Rectangle(
                        listLW[tx].nLeft, headerGab, listLW[tx].nWidth, textHeight);

                    // OFR 인식 (Step 2-3과 동일)
                    Draw.Bitmap bmpColHeader = OfrService.GetBitmapInBitmapFast(
                        bmpHeader, rcColHeader);
                    if (bmpColHeader == null) continue;

                    byte avgBrightness = OfrService.GetAverageBrightness_FromColorBitmapFast(
                        bmpColHeader);
                    Draw.Rectangle? rcForeground = OfrService.GetForeGroundDrawRectangle_FromColorBitmapFast(
                        bmpColHeader, avgBrightness, 0);

                    if (rcForeground == null || rcForeground.Value.Width < 1)
                    {
                        bmpColHeader?.Dispose();
                        continue;
                    }

                    Draw.Bitmap bmpExact = OfrService.GetBitmapInBitmapFast(
                        bmpColHeader, rcForeground.Value);
                    bmpColHeader?.Dispose();

                    if (bmpExact == null) continue;

                    StdResult_String result = await OfrWork_Common.OfrStr_ComplexCharSetAsync(bmpExact, bEdit, dWeight: 0.9, bEdit);

                    bmpExact?.Dispose();

                    // 디버그: "기사전번" 찾기 중 상세 로그
                    if (targetText == "기사전번")
                    {
                        Debug.WriteLine($"[InitDG오더] - 컬럼[{tx}] OFR 결과: '{result?.strResult}'");
                    }

                    if (result?.strResult == targetText)
                    {
                        index = tx;
                        //Debug.WriteLine($"[InitDG오더] 3-{x+1}. 찾음: '{targetText}' at [{tx}]");
                        break;
                    }
                }

                bmpHeader.Dispose();

                // 3-4. 에러 체크
                if (index < 0)
                {
                    return CommonFuncs_StdResult.ErrMsgResult_Error(
                        $"[InitDG오더]목표 컬럼 '{targetText}' 찾기 실패",
                        "InsungsAct_RcptRegPage/InitDG오더Async_08", bWrite, true);
                }

                // 3-5. 컬럼 순서가 틀리면 드래그로 이동
                if (index != x)
                {
                    //Debug.WriteLine($"[InitDG오더] 3-{x+1}. 이동: [{index}] → [{x}]");

                    Draw.Rectangle rcStart = new Draw.Rectangle(
                        listLW[index].nLeft, headerGab, listLW[index].nWidth, textHeight);
                    Draw.Rectangle rcTarget = new Draw.Rectangle(
                        listLW[x].nLeft, headerGab, listLW[x].nWidth, textHeight);

                    Draw.Point ptStart = StdUtil.GetCenterDrawPoint(rcStart);
                    Draw.Point ptTarget = new Draw.Point(rcTarget.Left, ptStart.Y);

                    await Simulation_Mouse.SafeMouseEvent_DragLeft_SmoothAsync(
                        m_RcptPage.DG오더_hWnd, ptStart, ptTarget, bBkCursor: false, nMiliSec: 150);

                    await Task.Delay(150);
                }
            }

            Debug.WriteLine("[InitDG오더] Step 3 완료");

            // [확인용] Step 3 완료 후 컬럼 개수 확인
            {
                await Task.Delay(CommonVars.c_nWaitShort);
                var (bmpCheck, _, checkColumns) = CaptureAndDetectColumnBoundaries(rcHeader, targetRow);
                if (bmpCheck != null)
                {
                    Debug.WriteLine($"[InitDG오더] Step 3 완료 후 검출 컬럼 수: {checkColumns}");
                    bmpCheck.Dispose();
                }
            }

            // Step 4: 컬럼 너비 조정
            Debug.WriteLine("[InitDG오더] Step 4: 컬럼 너비 조정 시작");

            for (int x = 0; x < m_ReceiptDgHeaderInfos.Length; x++)
            {
                // 매 반복 시 캡처 전 안정화 대기
                await Task.Delay(CommonVars.c_nWaitShort);

                Debug.WriteLine($"[InitDG오더] 4-{x+1}. 컬럼 너비 조정 시작: [{x}]{m_ReceiptDgHeaderInfos[x].sName}");

                // 4-1. 헤더 캡처 및 컬럼 경계 검출
                var (bmpHeader, listLW, currentColumns) = CaptureAndDetectColumnBoundaries(rcHeader, targetRow);
                if (bmpHeader == null)
                {
                    return CommonFuncs_StdResult.ErrMsgResult_Error(
                        $"[InitDG오더]Step 4 헤더 캡처 실패 (컬럼 {x})",
                        "InsungsAct_RcptRegPage/InitDG오더Async_09", bWrite, true);
                }

                Debug.WriteLine($"[InitDG오더] 4-{x+1}. 조정 전 검출 컬럼 수: {currentColumns}");

                bmpHeader.Dispose();

                // 4-3. 현재 너비와 목표 너비 비교
                int currentWidth = listLW[x].nWidth;
                int targetWidth = m_ReceiptDgHeaderInfos[x].nWidth;
                int dx = targetWidth - currentWidth;

                if (dx == 0)
                {
                    Debug.WriteLine($"[InitDG오더] 4-{x+1}. 너비 일치: {currentWidth}px, 스킵");
                    continue; // 이미 원하는 너비
                }

                Debug.WriteLine($"[InitDG오더] 4-{x+1}. 너비 조정: {currentWidth}px → {targetWidth}px (dx={dx})");

                // 4-4. 컬럼 오른쪽 경계를 dx만큼 드래그
                Draw.Point ptStart = new Draw.Point(listLW[x]._nRight + 1, headerGab);
                Draw.Point ptTarget = new Draw.Point(ptStart.X + dx, ptStart.Y);

                await Simulation_Mouse.SafeMouseEvent_DragLeft_SmoothAsync(
                    m_RcptPage.DG오더_hWnd, ptStart, ptTarget, bBkCursor: false, nMiliSec: 150);

                await Task.Delay(150);

                // [확인용] 조정 후 컬럼 개수 확인
                await Task.Delay(CommonVars.c_nWaitShort);
                var (bmpAfter, _, afterColumns) = CaptureAndDetectColumnBoundaries(rcHeader, targetRow);
                if (bmpAfter != null)
                {
                    Debug.WriteLine($"[InitDG오더] 4-{x+1}. 조정 후 검출 컬럼 수: {afterColumns}");

                    if (afterColumns < currentColumns)
                    {
                        Debug.WriteLine($"[InitDG오더] 4-{x+1}. 경고: 컬럼 감소 {currentColumns} → {afterColumns}");
                    }

                    bmpAfter.Dispose();
                }
            }

            Debug.WriteLine("[InitDG오더] Step 4 완료");

            // [확인용] Step 4 완료 후 최종 상태 확인
            {
                Debug.WriteLine("[InitDG오더] Step 4 완료 후 최종 확인 시작");

                await Task.Delay(CommonVars.c_nWaitShort);
                var (bmpFinal, listLW, finalColumns) = CaptureAndDetectColumnBoundaries(rcHeader, targetRow);
                if (bmpFinal != null)
                {
                    Debug.WriteLine($"[InitDG오더] 최종 컬럼 수: {finalColumns}");

                    // 각 컬럼 OFR 확인
                    string[] finalTexts = await OfrAllColumnsAsync(bmpFinal, listLW, finalColumns, headerGab, textHeight, bEdit);

                    // 최종 컬럼 출력
                    Debug.WriteLine($"[InitDG오더] 최종 컬럼 목록({finalColumns}): {string.Join(", ", finalTexts.Where(t => !string.IsNullOrEmpty(t)))}");

                    // 원하는 컬럼과 비교
                    int matchCount = 0;
                    List<string> missingList = new List<string>();
                    for (int i = 0; i < m_ReceiptDgHeaderInfos.Length; i++)
                    {
                        bool found = false;
                        for (int x = 0; x < finalColumns; x++)
                        {
                            if (finalTexts[x] == m_ReceiptDgHeaderInfos[i].sName)
                            {
                                found = true;
                                matchCount++;
                                break;
                            }
                        }
                        if (!found)
                        {
                            missingList.Add(m_ReceiptDgHeaderInfos[i].sName);
                        }
                    }

                    Debug.WriteLine($"[InitDG오더] 원하는 컬럼 매칭: {matchCount}/{m_ReceiptDgHeaderInfos.Length}");
                    if (missingList.Count > 0)
                    {
                        Debug.WriteLine($"[InitDG오더] 누락 컬럼: {string.Join(", ", missingList)}");
                    }

                    // 순서 확인
                    bool orderCorrect = true;
                    for (int i = 0; i < m_ReceiptDgHeaderInfos.Length && i < finalColumns; i++)
                    {
                        if (finalTexts[i] != m_ReceiptDgHeaderInfos[i].sName)
                        {
                            Debug.WriteLine($"[InitDG오더] 순서 불일치 [{i}]: 실제='{finalTexts[i]}', 예상='{m_ReceiptDgHeaderInfos[i].sName}'");
                            orderCorrect = false;
                        }
                    }
                    if (orderCorrect)
                    {
                        Debug.WriteLine("[InitDG오더] 순서 일치: OK");
                    }

                    bmpFinal.Dispose();
                }

                Debug.WriteLine("[InitDG오더] Step 4 완료 후 최종 확인 종료");
            }

            return null; // 성공
        }
        catch (Exception ex)
        {
            return CommonFuncs_StdResult.ErrMsgResult_Error(
                $"[InitDG오더]예외발생: {ex.Message}",
                "InsungsAct_RcptRegPage/InitDG오더Async_999", bWrite, true);
        }
        finally
        {
            // 외부 입력 차단 강제 해제 (예외 발생 시에도 보장, 중첩 카운터 무시)
            Simulation_Mouse.SafeBlockInputForceStop();

            // 마우스 커서 위치 복원
            Std32Cursor.SetCursorPos_AbsDrawPt(ptCursorBackup);
            //Debug.WriteLine("[InitDG오더] 커서 위치 복원 완료");
        }

        #region 주석처리 - 단계별 확인 중
        /*
        Draw.Bitmap bmpHeader = null;
        Draw.Point ptCursorBackup = Std32Cursor.GetCursorPos_AbsDrawPt(); // 커서 위치 백업
        bool initSuccess = false;

        try
        {
            Debug.WriteLine($"[InitDG오더Async] Datagrid 강제 초기화 시작: issues={issues}");

            // 1. 접수화면초기화 시도 (최대 3회 재시도)
            const int MAX_INIT_RETRY = 3;
            for (int initRetry = 0; initRetry < MAX_INIT_RETRY; initRetry++)
            {
                if (initRetry > 0)
                {
                    Debug.WriteLine($"[InitDG오더Async] 접수화면초기화 재시도 {initRetry + 1}/{MAX_INIT_RETRY}");
                    await Task.Delay(300); // 재시도 전 대기
                }

                // 1-1. Context 메뉴 → "접수화면초기화" 클릭
                await Std32Mouse_Post.MousePostAsync_ClickRight(m_RcptPage.DG오더_hWnd);
                await Task.Delay(c_nWaitNormal);

                // Context 메뉴 대기 (원하는 결과가 나올 때까지 폴링)
                IntPtr hWndMenu = IntPtr.Zero;
                for (int i = 0; i < c_nRepeatVeryMany; i++) // 2초 대기
                {
                    await Task.Delay(c_nWaitUltraShort);
                    hWndMenu = Std32Window.FindMainWindow_StartsWith(
                        m_FileInfo.Main_AnyMenu_sClassName,
                        m_FileInfo.Main_AnyMenu_sWndName
                    );
                    if (hWndMenu != IntPtr.Zero) break;
                }

                if (hWndMenu == IntPtr.Zero)
                {
                    Debug.WriteLine($"[InitDG오더Async] Context 메뉴 찾기 실패 (시도 {initRetry + 1}/{MAX_INIT_RETRY})");
                    continue; // 다시 우클릭부터 재시도
                }

                Debug.WriteLine($"[InitDG오더Async] Context 메뉴 찾음: {hWndMenu:X}");

                // "접수화면초기화" 메뉴 클릭 (2개 서브메뉴 중 위쪽)
                await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndMenu, 10, 12);
                await Task.Delay(c_nWaitNormal);

                // 확인 다이얼로그 대기 (원하는 결과가 나올 때까지 폴링)
                IntPtr hWndDialog = IntPtr.Zero;
                bool dialogHandled = false;

                for (int i = 0; i < c_nRepeatNormal; i++) // 1초 대기
                {
                    await Task.Delay(c_nWaitNormal);
                    hWndDialog = Std32Window.FindWindow("#32770", "확인");
                    if (hWndDialog != IntPtr.Zero)
                    {
                        // "예(&Y)" 버튼 찾기
                        IntPtr hWndBtn = Std32Window.FindWindowEx(hWndDialog, IntPtr.Zero, "Button", "예(&Y)");
                        if (hWndBtn != IntPtr.Zero)
                        {
                            Debug.WriteLine($"[InitDG오더Async] 확인 다이얼로그 '예' 버튼 클릭");
                            await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndBtn);
                            dialogHandled = true;
                            break;
                        }
                    }
                }

                if (!dialogHandled)
                {
                    Debug.WriteLine($"[InitDG오더Async] 확인 다이얼로그 처리 실패 (시도 {initRetry + 1}/{MAX_INIT_RETRY})");
                    continue; // 다시 우클릭부터 재시도
                }

                // 확인 다이얼로그 사라질 때까지 대기 (원하는 결과가 나올 때까지 폴링)
                for (int i = 0; i < c_nRepeatNormal; i++) // 1초 대기
                {
                    await Task.Delay(c_nWaitNormal);
                    hWndDialog = Std32Window.FindWindow("#32770", "확인");
                    if (hWndDialog == IntPtr.Zero) break;
                }

                if (hWndDialog != IntPtr.Zero)
                {
                    Debug.WriteLine($"[InitDG오더Async] 확인 다이얼로그가 사라지지 않음 (시도 {initRetry + 1}/{MAX_INIT_RETRY})");
                    continue; // 다시 우클릭부터 재시도
                }

                Debug.WriteLine($"[InitDG오더Async] 접수화면초기화 완료");
                initSuccess = true;
                await Task.Delay(c_nWaitVeryLong); // 초기화 반영 대기
                break; // 성공 시 탈출
            }

            // 접수화면초기화 실패 시 경고만 출력 (컬럼 조정은 시도)
            if (!initSuccess)
            {
                Debug.WriteLine($"[InitDG오더Async] 경고: 접수화면초기화 {MAX_INIT_RETRY}회 실패 - 컬럼 조정만 시도");
            }

            // 2. 컬럼 조정 (WrongOrder 또는 WrongWidth 이슈가 있을 때만)
            if ((issues & CEnum_DgValidationIssue.WrongOrder) != 0 ||
                (issues & CEnum_DgValidationIssue.WrongWidth) != 0)
            {
                Debug.WriteLine($"[InitDG오더Async] 컬럼 조정 시작");

                // Datagrid 절대좌표
                Draw.Point ptDgTopLeft = new Draw.Point(
                    m_RcptPage.DG오더_AbsRect.Left,
                    m_RcptPage.DG오더_AbsRect.Top
                );

                // m_ReceiptDgHeaderInfos.Length만큼 반복 (20개 컬럼)
                for (int repeatIdx = 0; repeatIdx < m_ReceiptDgHeaderInfos.Length; repeatIdx++)
                {
                    // 2-1. 헤더 캡처
                    bmpHeader = OfrService.CaptureScreenRect_InWndHandle(
                        m_Main.TopWnd_hWnd,
                        rcHeader
                    );

                    if (bmpHeader == null)
                    {
                        Debug.WriteLine($"[InitDG오더Async] 경고: 헤더 캡처 실패 (repeat={repeatIdx}) - 다음 반복 시도");
                        continue; // return 하지 않고 다음 반복
                    }

                    // 2-2. 컬럼 경계 검출
                    byte minBrightness = OfrService.GetMinBrightnessAtRow_FromColorBitmapFast(
                        bmpHeader, HEADER_GAB
                    );
                    minBrightness += 2; // 확실한 경계를 위해

                    bool[] boolArr = OfrService.GetBoolArray_FromColorBitmapRowFast(
                        bmpHeader, HEADER_GAB, minBrightness, 2
                    );

                    List<OfrModel_LeftWidth> listLW =
                        OfrService.GetLeftWidthList_FromBool1Array(boolArr, minBrightness);

                    if (listLW == null || listLW.Count == 0)
                    {
                        bmpHeader?.Dispose();
                        continue; // 다음 반복
                    }

                    int columns = listLW.Count - 1; // 오른쪽 끝 경계 제외
                    if (columns <= 0)
                    {
                        bmpHeader?.Dispose();
                        continue;
                    }

                    // 2-3. 컬럼 헤더 OFR
                    string[] columnTexts = new string[columns];
                    Draw.Rectangle[] rcHeaders = new Draw.Rectangle[columns];

                    for (int x = 0; x < columns; x++)
                    {
                        rcHeaders[x] = new Draw.Rectangle(
                            listLW[x].nLeft,
                            HEADER_GAB,
                            listLW[x].nWidth,
                            HEADER_TEXT_HEIGHT
                        );

                        // OFR 수행
                        OfrResult_TbCharSetList resultChSet = await OfrWork_Common.OfrStr_ComplexCharSetAsync(
                            bmpHeader,
                            rcHeaders[x],
                            bSaveToTbText: false,
                            bEdit,
                            bWrite,
                            bMsgBox: false
                        );

                        if (resultChSet != null && !string.IsNullOrEmpty(resultChSet.strResult))
                        {
                            columnTexts[x] = resultChSet.strResult;
                        }
                        else
                        {
                            columnTexts[x] = "";
                        }
                    }

                    bmpHeader?.Dispose();
                    bmpHeader = null;

                    // 2-4. 각 컬럼 위치 확인 및 조정
                    for (int x = 0; x < columns && x < m_ReceiptDgHeaderInfos.Length; x++)
                    {
                        string targetText = m_ReceiptDgHeaderInfos[x].sName;

                        // 현재 컬럼 찾기
                        int index = Array.FindIndex(columnTexts, t => t == targetText);

                        if (index < 0)
                        {
                            Debug.WriteLine($"[InitDG오더Async] 컬럼[{x}] '{targetText}' 찾기 실패");
                            continue; // 다음 컬럼
                        }

                        // 컬럼 순서가 틀리면 드래그로 이동
                        if (index != x)
                        {
                            Debug.WriteLine($"[InitDG오더Async] 컬럼[{x}] '{targetText}' 순서 조정: {index} → {x}");

                            Draw.Point ptStart = StdUtil.GetCenterDrawPoint(rcHeaders[index]);
                            Draw.Point ptEnd = new Draw.Point(rcHeaders[x].Left, ptStart.Y);

                            // 절대좌표로 변환
                            Draw.Point ptStartAbs = new Draw.Point(
                                ptDgTopLeft.X + rcHeader.Left + ptStart.X,
                                ptDgTopLeft.Y + rcHeader.Top + ptStart.Y
                            );
                            Draw.Point ptEndAbs = new Draw.Point(
                                ptDgTopLeft.X + rcHeader.Left + ptEnd.X,
                                ptDgTopLeft.Y + rcHeader.Top + ptEnd.Y
                            );

                            // Drag 수행 (Simulation.cs:457-478 참고, 라이브러리 조합 방식)
                            Std32Cursor.SetCursorPos_AbsDrawPt(ptStartAbs);
                            Std32Mouse_Event.MouseEvent_LeftBtnDown();
                            Std32Mouse_Send.MouseSet_MoveSmooth_ptAbs(ptStartAbs, ptEndAbs, 150);
                            Std32Mouse_Event.MouseEvent_LeftBtnUp();
                            await Task.Delay(150);
                        }
                    }

                    // 2-5. 컬럼 너비 조정
                    for (int x = 0; x < columns && x < m_ReceiptDgHeaderInfos.Length; x++)
                    {
                        StdResult_Error adjustResult = AdjustColumnWidth(rcHeader, ptDgTopLeft, x);
                        if (adjustResult != null)
                        {
                            Debug.WriteLine($"[InitDG오더Async] 컬럼[{x}] 너비 조정 실패: {adjustResult.sErr}");
                            // 실패해도 계속 진행
                        }

                        await Task.Delay(150);
                    }

                    // 모든 컬럼 처리 완료 - 루프 탈출
                    Debug.WriteLine($"[InitDG오더Async] 컬럼 조정 완료");
                    break;
                }
            }

            Debug.WriteLine($"[InitDG오더Async] Datagrid 강제 초기화 완료");
            return null; // 성공
        }
        catch (Exception ex)
        {
            return CommonFuncs_StdResult.ErrMsgResult_Error(
                $"[{m_Context.AppName}/InitDG오더]예외발생: {ex.Message}",
                "InsungsAct_RcptRegPage/InitDG오더Async_999", bWrite, bMsgBox);
        }
        finally
        {
            bmpHeader?.Dispose();
            Std32Cursor.SetCursorPos_AbsDrawPt(ptCursorBackup); // 커서 원위치
        }
        */
        #endregion
    }

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
    #endregion

    #region 자동배차 - Kai신규 관련함수들
    /// <summary>
    /// 신규 주문 등록 확인 (Kai에만 존재, 인성에 없음)
    /// </summary>
    public async Task<CommonResult_AutoAllocProcess> CheckIsOrderAsync_AssumeKaiNewOrder(AutoAllocModel item, CancelTokenControl ctrl)
    {
        // Cancel/Pause 체크 - 긴 작업 전
        await ctrl.WaitIfPausedOrCancelledAsync();

        string kaiState = item.NewOrder.OrderState;

        switch (kaiState)
        {
            case "접수":
            case "취소":
            case "대기":
                // 신규 주문 팝업창 열기 → 입력 → 닫기 → 성공 확인
                StdResult_Status result = await OpenNewOrderPopupAsync(item, ctrl);

                if (result.Result == StdResult.Success)
                {
                    // 성공: NotChanged로 재적재 (다음 사이클에서 관리 대상으로 분류됨)
                    return CommonResult_AutoAllocProcess.SuccessAndReEnqueue(item, PostgService_Common_OrderState.NotChanged);
                }
                else
                {
                    // 실패: 치명적 에러 (신규 등록 실패)
                    return CommonResult_AutoAllocProcess.FailureAndDiscard(result.sErr, result.sPos);
                }

            case "배차":
            case "운행":
            case "완료":
            case "예약":
                return CommonResult_AutoAllocProcess.FailureAndDiscard($"미구현 상태: {kaiState}", "CheckIsOrderAsync_AssumeKaiNewOrder_TODO");

            default:
                return CommonResult_AutoAllocProcess.FailureAndDiscard($"알 수 없는 Kai 주문 상태: {kaiState}", "CheckIsOrderAsync_AssumeKaiNewOrder_800");
        }
    }
    #endregion

    #region 자동배차 - Kai변경 관련함수들
    /// <summary>
    /// Kai DB에서 업데이트된 주문을 인성 앱에 반영
    /// </summary>
    public async Task<CommonResult_AutoAllocProcess> CheckIsOrderAsync_AssumeKaiUpdated(AutoAllocModel item, CommonResult_AutoAllocDatagrid dgInfo, CancelTokenControl ctrl)
    {
        await ctrl.WaitIfPausedOrCancelledAsync();

        string kaiState = item.NewOrder.OrderState;
        string isState = dgInfo.sStatus;

        // 상태가 같은 경우: 필드만 업데이트
        if (kaiState == isState) return await UpdateOrderSameStateAsync(item, dgInfo, ctrl);
        // 상태가 다른 경우: 필드 업데이트 + 상태 전환
        else return await UpdateOrderDiffStateAsync(item, dgInfo, kaiState, isState, ctrl);
    }

    /// <summary>
    /// 같은 상태: 필드만 선별 업데이트
    /// </summary>
    private async Task<CommonResult_AutoAllocProcess> UpdateOrderSameStateAsync(AutoAllocModel item, CommonResult_AutoAllocDatagrid dgInfo, CancelTokenControl ctrl)
    {
        string isState = dgInfo.sStatus;

        // 인성 앱 특성: 상태가 변경되면 저장 안 됨 → 같은 상태 버튼 클릭 필요
        // 대기/취소: 외부에서 상태 변경 불가 → 반복 불필요
        // 접수/배차: 외부에서 상태 변경 가능 → 타이밍 이슈 대비 반복 필요
        switch (dgInfo.sStatus)
        {
            case "대기":  // 외부 변경 없음 → 1번만
                return await UpdateOrderWidelyAsync("", item, dgInfo, false, ctrl);

            case "접수": // 외부 변경 가능 → 10번 재시도
            case "배차":
                return await UpdateOrderWidelyAsync("", item, dgInfo, true, ctrl);

            case "취소":
            case "완료":
                return CommonResult_AutoAllocProcess.SuccessAndReEnqueue(item, PostgService_Common_OrderState.NotChanged);

            default:
                return CommonResult_AutoAllocProcess.FailureAndDiscard($"미구현 상태(SameState): Kai={isState}, IS={isState}", "UpdateOrderSameStateAsync_999");
        }
    }

    /// <summary>
    /// 다른 상태: 필드 업데이트 + 상태 전환
    /// </summary>
    private async Task<CommonResult_AutoAllocProcess> UpdateOrderDiffStateAsync(AutoAllocModel item, CommonResult_AutoAllocDatagrid dgInfo, string kaiState, string isState, CancelTokenControl ctrl)
    {
        string wantState = kaiState; // Kai DB의 목표 상태로 전환
        bool useRepeat;

        // 상태 전환 규칙에 따라 반복 횟수 결정
        switch (kaiState)
        {
            case "접수":
                switch (isState)
                {
                    case "취소": // 취소 → 접수
                    case "대기": // 대기 → 접수
                        useRepeat = true; // 10번 재시도
                        break;

                    case "운행": // 운행 → 접수
                        Debug.WriteLine($"  → StateFlag를 NotChanged로 변경 후 재적재 요청");
                        return CommonResult_AutoAllocProcess.SuccessAndReEnqueue(item, PostgService_Common_OrderState.NotChanged);

                    default:
                        return CommonResult_AutoAllocProcess.FailureAndDiscard($"미구현 전환: Kai=접수, IS={isState}", "InsungsAct_RcptRegPage/pdateOrderDiffStateAsync_01");
                }
                break;

            case "대기":
                switch (isState)
                {
                    case "취소": // 취소 → 대기
                        useRepeat = false; // 1번만
                        break;
                    case "접수": // 접수 → 대기
                    case "배차": // 배차 → 대기
                        useRepeat = true; // 10번 재시도
                        break;
                    default:
                        return CommonResult_AutoAllocProcess.FailureAndDiscard($"미구현 전환: Kai=대기, IS={isState}", "InsungsAct_RcptRegPage/UpdateOrderDiffStateAsync_02");
                }
                break;

            case "취소":
                switch (isState)
                {
                    case "접수": // 접수 → 취소
                    case "배차": // 배차 → 취소
                    case "운행": // 운행 → 취소
                        return await UpdateOrderStateOnlyAsync(wantState, item, dgInfo, true, ctrl); // 10번 재시도

                    case "예약": // 예약 → 취소
                    case "완료": // 완료 → 취소
                    case "대기": // 대기 → 취소
                        return await UpdateOrderStateOnlyAsync(wantState, item, dgInfo, false, ctrl); // 1번만

                    default:
                        return CommonResult_AutoAllocProcess.FailureAndDiscard($"미구현 전환: Kai=취소, IS={isState}", "InsungsAct_RcptRegPage/UpdateOrderDiffStateAsync_03");
                }

            case "운행":
                switch (isState)
                {
                    case "완료": // 운행 → 완료
                        return await CommonVars.s_Order_StatusPage.Insung01운행To완료Async(item, ctrl);

                    default:
                        return CommonResult_AutoAllocProcess.FailureAndDiscard($"미구현 전환: Kai=취소, IS={isState}", "InsungsAct_RcptRegPage/UpdateOrderDiffStateAsync_03");
                }

            default:
                return CommonResult_AutoAllocProcess.FailureAndDiscard($"미구현 상태(DiffState): Kai={kaiState}, IS={isState}", "InsungsAct_RcptRegPage/UpdateOrderDiffStateAsync_04");
        }

        // 팝업 열기 → 필드 업데이트 → 상태 전환 → 저장/닫기
        return await UpdateOrderWidelyAsync(wantState, item, dgInfo, useRepeat, ctrl);
    }
    #endregion

    #region 자동배차 - Insung상태관리 관련함수들
    /// <summary>
    /// Insung 주문 상태 관리 및 모니터링 (NotChanged 상황 처리)
    /// - Insung 상태를 primary switch로 분기
    /// - 각 Insung 상태별 handler 함수 호출
    /// - 로그만 출력 (DB 업데이트, 앱 취소 작업 없음)
    /// </summary>
    public async Task<CommonResult_AutoAllocProcess> CheckIsOrderAsync_InsungOrderManage(AutoAllocModel item, CommonResult_AutoAllocDatagrid dgInfo, CancelTokenControl ctrl)
    {
        // Cancel/Pause 체크
        await ctrl.WaitIfPausedOrCancelledAsync();

        string kaiState = item.NewOrder.OrderState;
        string isState = dgInfo.sStatus;

        Debug.WriteLine($"[CheckIsOrderAsync_InsungOrderManage] KeyCode={item.KeyCode}, Kai={kaiState}, Insung={isState}");

        // Insung 상태별로 handler 함수 호출 (2중 switch 방지)
        switch (isState)
        {
            case "접수":
            case "배차":
                return await InsungOrderManage_접수Or배차Async(item, kaiState, dgInfo, ctrl);
            case "운행":
                return await InsungOrderManage_운행Async(item, kaiState, dgInfo, ctrl);
            case "완료":
                return await InsungOrderManage_완료Async(item, kaiState, dgInfo, ctrl);
            case "대기":
                return await InsungOrderManage_대기Async(item, kaiState, dgInfo, ctrl);
            case "취소":
                return await InsungOrderManage_취소Async(item, kaiState, dgInfo, ctrl);

            default:
                Debug.WriteLine($"  → 미정의 Insung 상태: {isState}");
                return CommonResult_AutoAllocProcess.SuccessAndReEnqueue(item, PostgService_Common_OrderState.NotChanged);
        }
    }

    /// <summary>
    /// Insung "접수" 또는 "배차" 상태 처리 - Kai 상태별 로깅
    /// </summary>
#pragma warning disable CS1998 // async method lacks await
    private async Task<CommonResult_AutoAllocProcess> InsungOrderManage_접수Or배차Async(AutoAllocModel item, string kaiState, CommonResult_AutoAllocDatagrid dgInfo, CancelTokenControl ctrl)
    {
        string insungState = dgInfo.sStatus;

        switch (kaiState)
        {
            case "접수":
            case "배차":
                Debug.WriteLine($"  → StateFlag를 NotChanged로 변경 후 재적재 요청");
                return CommonResult_AutoAllocProcess.SuccessAndReEnqueue(item, PostgService_Common_OrderState.NotChanged);

            default:
                Debug.WriteLine($"  → [{insungState}/?] 미정의 Kai 상태: {kaiState}");
                return CommonResult_AutoAllocProcess.FailureAndRetry($"미정의 Kai 상태: {kaiState}, Insung={insungState}", "InsungOrderManage_접수Or배차Async_999");
        }
    }
#pragma warning restore CS1998

    /// <summary>
    /// Insung "운행" 상태 처리 - 40초 타이머 + Kai 상태별 로깅
    /// </summary>
    private async Task<CommonResult_AutoAllocProcess> InsungOrderManage_운행Async(AutoAllocModel item, string kaiState, CommonResult_AutoAllocDatagrid dgInfo, CancelTokenControl ctrl)
    {
        Debug.WriteLine($"  → [InsungOrderManage_운행Async] 진입 - KeyCode={item.KeyCode}, RunStartTime={item.RunStartTime?.ToString("HH:mm:ss") ?? "null"}, DriverPhone={item.DriverPhone ?? "null"}");

        switch (kaiState)
        {
            case "접수":
                // 타이머 시작 체크
                if (item.RunStartTime == null)
                {
                    item.RunStartTime = DateTime.Now;
                    Debug.WriteLine($"  → [운행/접수] 운행 진입 - 타이머 시작 ({item.RunStartTime:HH:mm:ss}) - 경과: 0.0초 / 40초");

                    // 기사전번 읽기 (캡처된 페이지 이미지 재사용)
                    if (dgInfo.BmpPage == null)
                    {
                        Debug.WriteLine($"  → [운행/접수] 심각한 오류: BmpPage가 null - 자동배차 루프에서 페이지 캡처 실패");
                        return CommonResult_AutoAllocProcess.FailureAndRetry("BmpPage가 null - 페이지 캡처 실패", "InsungOrderManage_운행Async_BmpPageNull");
                    }

                    int yIndex = dgInfo.nIndex + 2;  // 헤더 2줄 추가
                    Draw.Rectangle rectDriverPhNo = m_RcptPage.DG오더_RelChildRects[c_nCol기사전번, yIndex];
                    StdResult_String resultDriverPhNo = await GetRowDriverPhNoAsync(dgInfo.BmpPage, rectDriverPhNo, dgInfo.bInvertRgb, ctrl);

                    if (string.IsNullOrEmpty(resultDriverPhNo.strResult))
                    {
                        Debug.WriteLine($"  → [운행/접수] 심각한 오류: 기사전번 획득 실패 - 운행 상태인데 기사 정보 없음: {resultDriverPhNo.sErr}");
                        return CommonResult_AutoAllocProcess.FailureAndRetry($"기사전번 OFR 실패: {resultDriverPhNo.sErr}", "InsungOrderManage_운행Async_DriverPhNoFail");
                    }

                    item.DriverPhone = resultDriverPhNo.strResult;
                    Debug.WriteLine($"  → [운행/접수] 기사전번 획득: {item.DriverPhone}");

                    return CommonResult_AutoAllocProcess.SuccessAndReEnqueue(item);  // item 업데이트하며 재적재
                }

                // 타이머 체크
                TimeSpan elapsed = DateTime.Now - item.RunStartTime.Value;
                Debug.WriteLine($"  → [운행/접수] 운행 중 - 경과 시간: {elapsed.TotalSeconds:F1}초");

                // 기사전번 다시 OFR (기사 변경 감지용)
                if (dgInfo.BmpPage == null)
                {
                    Debug.WriteLine($"  → [운행/접수] 심각한 오류: BmpPage가 null - 자동배차 루프에서 페이지 캡처 실패");
                    return CommonResult_AutoAllocProcess.FailureAndRetry("BmpPage가 null - 페이지 캡처 실패", "InsungOrderManage_운행Async_BmpPageNull2");
                }

                int yIndexCheck = dgInfo.nIndex + 2;  // 헤더 2줄 추가
                Draw.Rectangle rectDriverPhNoCheck = m_RcptPage.DG오더_RelChildRects[c_nCol기사전번, yIndexCheck];
                StdResult_String resultDriverPhNoCheck = await GetRowDriverPhNoAsync(dgInfo.BmpPage, rectDriverPhNoCheck, dgInfo.bInvertRgb, ctrl);

                if (string.IsNullOrEmpty(resultDriverPhNoCheck.strResult))
                {
                    Debug.WriteLine($"  → [운행/접수] 심각한 오류: 기사전번 획득 실패 - 운행 상태인데 기사 정보 없음: {resultDriverPhNoCheck.sErr}");
                    return CommonResult_AutoAllocProcess.FailureAndRetry($"기사전번 OFR 실패: {resultDriverPhNoCheck.sErr}", "InsungOrderManage_운행Async_DriverPhNoFail2");
                }

                // 기사 변경 체크
                if (resultDriverPhNoCheck.strResult != item.DriverPhone)
                {
                    Debug.WriteLine($"  → [운행/접수] 기사 변경 감지! 기존: {item.DriverPhone} → 새: {resultDriverPhNoCheck.strResult}");
                    item.DriverPhone = resultDriverPhNoCheck.strResult;
                    item.RunStartTime = DateTime.Now;
                    Debug.WriteLine($"  → [운행/접수] 타이머 리셋 - 새로운 40초 대기 시작 ({item.RunStartTime:HH:mm:ss})");
                    return CommonResult_AutoAllocProcess.SuccessAndReEnqueue(item);  // item 업데이트하며 재적재
                }

                // 40초 경과 체크 (기사 변경 없음)
                if (elapsed.TotalSeconds < 40)
                {
                    // 40초 미만 - 계속 대기
                    Debug.WriteLine($"  → [운행/접수] 40초 대기 중 - 경과: {elapsed.TotalSeconds:F1}초 / 40초");
                    return CommonResult_AutoAllocProcess.SuccessAndReEnqueue(item);  // item 유지하며 재적재
                }

                // 40초 이상 경과 - 기사 확정
                Debug.WriteLine($"  → [운행/접수] 40초 경과! 기사 확정 상태 (기사전번: {item.DriverPhone})");

                // 타이머 파괴
                item.RunStartTime = null;

                // 1. 기사 정보 읽기용 팝업 열기 (DG 더블클릭 → 3초 대기 → 닫기)
                Debug.WriteLine($"  → [운행/접수] 기사 정보 읽기용 팝업 열기 시작");
                StdResult_Status resultPopup = await OpenReadPopupAsync(dgInfo.nIndex, item, ctrl);
                if (resultPopup.Result != StdResult.Success)
                {
                    Debug.WriteLine($"  → [운행/접수] 기사 정보 읽기 실패: {resultPopup.sErr}");
                    return CommonResult_AutoAllocProcess.FailureAndRetry(resultPopup.sErr, resultPopup.sPos);
                }
                Debug.WriteLine($"  → [운행/접수] 기사 정보 읽기 성공");

                // 2. Order_StatusPage에서 처리 (DB 업데이트, 다른 앱 취소)
                return await CommonVars.s_Order_StatusPage.Insung01배차To운행Async(item, ctrl);

            case "운행": // 같은상태 
                return CommonResult_AutoAllocProcess.SuccessAndReEnqueue();

            default:
                Debug.WriteLine($"  → [운행/{kaiState}] 미정의 Kai 상태: {kaiState}");
                return CommonResult_AutoAllocProcess.FailureAndRetry($"미정의 Kai 상태: {kaiState}, Insung=운행", "InsungOrderManage_운행Async_999");
        }
    }

    /// <summary>
    /// Insung "완료" 상태 처리 - Kai 상태별 로깅
    /// </summary>
    private async Task<CommonResult_AutoAllocProcess> InsungOrderManage_완료Async(AutoAllocModel item, string kaiState, CommonResult_AutoAllocDatagrid dgInfo, CancelTokenControl ctrl)
    {
        switch (kaiState)
        {
            case "운행":
                return await CommonVars.s_Order_StatusPage.Insung01운행To완료Async(item, ctrl);

            default:
                Debug.WriteLine($"  → [완료/{kaiState}] 미정의 Kai 상태: {kaiState}");
                return CommonResult_AutoAllocProcess.FailureAndRetry($"미정의 Kai 상태: {kaiState}, Insung=완료", "InsungOrderManage_완료Async_999");
        }
    }

    /// <summary>
    /// Insung "대기" 상태 처리 - Kai 상태별 로깅
    /// </summary>
#pragma warning disable CS1998 // async method lacks await
    private async Task<CommonResult_AutoAllocProcess> InsungOrderManage_대기Async(AutoAllocModel item, string kaiState, CommonResult_AutoAllocDatagrid dgInfo, CancelTokenControl ctrl)
    {
        switch (kaiState)
        {
            case "대기":
                return CommonResult_AutoAllocProcess.SuccessAndReEnqueue(item, PostgService_Common_OrderState.NotChanged);

            default:
                Debug.WriteLine($"  → [대기/{kaiState}] 미정의 Kai 상태: {kaiState}");
                return CommonResult_AutoAllocProcess.FailureAndRetry($"미정의 Kai 상태: {kaiState}, Insung=대기", "InsungOrderManage_대기Async_999");
        }
    }
#pragma warning restore CS1998

    /// <summary>
    /// Insung "취소" 상태 처리 - Kai 상태별 로깅
    /// </summary>
#pragma warning disable CS1998 // async method lacks await
    private async Task<CommonResult_AutoAllocProcess> InsungOrderManage_취소Async(AutoAllocModel item, string kaiState, CommonResult_AutoAllocDatagrid dgInfo, CancelTokenControl ctrl)
    {
        switch (kaiState)
        {
            case "취소":
                return CommonResult_AutoAllocProcess.SuccessAndDestroy(item);

            case "대기": // 취소 -> 대기
                return await UpdateOrderStateOnlyAsync("대기", item, dgInfo, false, ctrl); // 1번만

            default:
                Debug.WriteLine($"  → [취소/{kaiState}] 미정의 Kai 상태: {kaiState}");
                return CommonResult_AutoAllocProcess.FailureAndRetry($"미정의 Kai 상태: {kaiState}, Insung=취소", "InsungOrderManage_취소Async_999");
        }
    }
#pragma warning restore CS1998
    #endregion
}
#nullable enable
