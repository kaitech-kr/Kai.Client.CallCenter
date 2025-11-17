using System.Diagnostics;

using Kai.Common.StdDll_Common;

using Kai.Client.CallCenter.Classes;

namespace Kai.Client.CallCenter.Networks.NwCargo24s;
#nullable disable

/// <summary>
/// 화물24시 접수등록 페이지 초기화 및 제어 담당 클래스
/// Context 패턴 사용: Cargo24Context를 통해 모든 정보에 접근
/// </summary>
public class Cargo24sAct_RcptRegPage
{
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
    /// TODO: TopWnd 찾기, 팝업 처리, StatusBtn 찾기, CmdBtn 찾기
    /// </summary>
    public async Task<StdResult_Error> InitializeAsync(bool bEdit = true, bool bWrite = true, bool bMsgBox = true)
    {
        try
        {
            Debug.WriteLine($"[Cargo24sAct_RcptRegPage] 초기화 시작");

            // TODO: 구현 예정

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
