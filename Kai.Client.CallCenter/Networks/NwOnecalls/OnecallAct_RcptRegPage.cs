using System.Diagnostics;
using Draw = System.Drawing;

using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;

using static Kai.Client.CallCenter.Classes.CommonVars;

namespace Kai.Client.CallCenter.Networks.NwOnecalls;
#nullable disable

/// <summary>
/// 원콜 접수등록 페이지 제어
/// </summary>
public class OnecallAct_RcptRegPage
{
    #region Private Fields
    private readonly OnecallContext m_Context;
    private OnecallInfo_File fInfo => m_Context.FileInfo;
    private OnecallInfo_Mem mInfo => m_Context.MemInfo;
    private OnecallInfo_Mem.MainWnd mMain => mInfo.Main;
    private OnecallInfo_Mem.RcptRegPage mRcpt => mInfo.RcptPage;
    private string AppName => m_Context.AppName;
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
            Debug.WriteLine($"[{AppName}] RcptRegPage InitializeAsync 시작");

            // 1. 접수등록Page 윈도우 찾기 (10초)
            for (int i = 0; i < c_nRepeatVeryMany; i++)
            {
                mRcpt.TopWnd_hWnd = Std32Window.FindWindowEx(mMain.WndInfo_MdiClient.hWnd, IntPtr.Zero, null, fInfo.접수등록Page_TopWnd_sWndName);
                if (mRcpt.TopWnd_hWnd != IntPtr.Zero) break;
                await Task.Delay(c_nWaitNormal);
            }

            if (mRcpt.TopWnd_hWnd == IntPtr.Zero)
                return new StdResult_Error($"[{AppName}] 접수등록Page 찾기실패: {fInfo.접수등록Page_TopWnd_sWndName}", "OnecallAct_RcptRegPage/InitializeAsync_01");

            Debug.WriteLine($"[{AppName}] 접수등록Page 찾음: {mRcpt.TopWnd_hWnd:X}");

            // 2. 자식윈도우 찾기
            List<StdCommon32_WndInfo> lst = Std32Window.GetChildWindows_FirstLayer(mRcpt.TopWnd_hWnd);
            if (lst.Count == 0)
                return new StdResult_Error($"[{AppName}] 접수등록Page 자식윈도 찾기실패", "OnecallAct_RcptRegPage/InitializeAsync_02");
            Debug.WriteLine($"[{AppName}] 접수등록Page 자식윈도 찾음: {lst.Count}개");

            // 3. 접수영역 찾기
            StdCommon32_WndInfo item = lst.FirstOrDefault(x => x.rcRel == fInfo.접수등록Page_접수영역_rcChkRel);
            if (item == null)
                return new StdResult_Error($"[{AppName}] 접수영역 찾기실패: {fInfo.접수등록Page_접수영역_rcChkRel}", "OnecallAct_RcptRegPage/InitializeAsync_03");
            mRcpt.접수영역_hWnd = item.hWnd;
            Debug.WriteLine($"[{AppName}] 접수영역 찾음: {mRcpt.접수영역_hWnd:X}");

            // 4. 검색영역 찾기
            item = lst.FirstOrDefault(x => x.wndName == fInfo.접수등록Page_검색영역_sWndName);
            if (item == null)
                return new StdResult_Error($"[{AppName}] 검색영역 찾기실패: {fInfo.접수등록Page_검색영역_sWndName}", "OnecallAct_RcptRegPage/InitializeAsync_04");
            mRcpt.검색영역_hWnd = item.hWnd;
            Debug.WriteLine($"[{AppName}] 검색영역 찾음: {mRcpt.검색영역_hWnd:X}");

            // 5. DG오더 찾기
            item = lst.FirstOrDefault(x => x.rcRel == fInfo.접수등록Page_DG오더_rcRelFirst);
            if (item == null)
                return new StdResult_Error($"[{AppName}] DG오더 찾기실패: {fInfo.접수등록Page_DG오더_rcRelFirst}", "OnecallAct_RcptRegPage/InitializeAsync_05");
            mRcpt.DG오더_hWnd = item.hWnd;
            Debug.WriteLine($"[{AppName}] DG오더 찾음: {mRcpt.DG오더_hWnd:X}");

            // 6. 검색영역 확장버튼 찾기
            mRcpt.접수영역_확장버튼_hWnd = Std32Window.GetWndHandle_FromRelDrawPt(mRcpt.검색영역_hWnd, fInfo.접수등록Page_검색ExpandBtn_ptChkRelL);
            if (mRcpt.접수영역_확장버튼_hWnd == IntPtr.Zero)
                return new StdResult_Error($"[{AppName}] 확장버튼 찾기실패: {fInfo.접수등록Page_검색ExpandBtn_ptChkRelL}", "OnecallAct_RcptRegPage/InitializeAsync_06");
            Debug.WriteLine($"[{AppName}] 확장버튼 찾음: {mRcpt.접수영역_확장버튼_hWnd:X}");

            // 7. 확장버튼 클릭 및 DG오더 확장 대기
            await Std32Mouse_Post.MousePostAsync_ClickLeft(mRcpt.접수영역_확장버튼_hWnd);
            Draw.Rectangle rcTmp = StdUtil.s_rcDrawEmpty;
            for (int i = 0; i < c_nRepeatNormal; i++)
            {
                rcTmp = Std32Window.GetWindowRect_DrawAbs(mRcpt.DG오더_hWnd);
                if (rcTmp.Height == fInfo.접수등록Page_DG오더_nExpandedHeight) break;
                await Task.Delay(c_nWaitShort);
            }
            if (rcTmp.Height != fInfo.접수등록Page_DG오더_nExpandedHeight)
                return new StdResult_Error($"[{AppName}] DG오더 확장실패: {rcTmp.Height} != {fInfo.접수등록Page_DG오더_nExpandedHeight}", "OnecallAct_RcptRegPage/InitializeAsync_07");
            Debug.WriteLine($"[{AppName}] DG오더 확장완료: {rcTmp.Height}");

            Debug.WriteLine($"[{AppName}] RcptRegPage InitializeAsync 완료");
            return null;
        }
        catch (Exception ex)
        {
            return new StdResult_Error($"[{AppName}] RcptRegPage 예외: {ex.Message}", "OnecallAct_RcptRegPage/InitializeAsync_99");
        }
    }
    #endregion
}
#nullable restore
