using System.Diagnostics;
using Draw = System.Drawing;

using Kai.Common.StdDll_Common.StdWin32;

using Kai.Client.CallCenter.Classes.Class_Master;
using static Kai.Client.CallCenter.Classes.CommonVars;

namespace Kai.Client.CallCenter.Networks.NwOnecalls;
#nullable disable

public partial class OnecallAct_RcptRegPage
{
    #region Helper Methods
    /// <summary>
    /// Onecall SeqNo 가져오기
    /// </summary>
    private string GetOnecallSeqno(AutoAllocModel item) => item.NewOrder.Onecall;
    #endregion

    #region DG오더 확장/축소 상태 관리
    /// <summary>
    /// 데이터그리드 확장 상태 확인 (높이 기준)
    /// </summary>
    public bool IsDG오더Expanded()
    {
        Draw.Rectangle rc = Std32Window.GetWindowRect_DrawAbs(mRcpt.DG오더_hWndTop);
        bool bExpanded = rc.Height >= fInfo.접수등록Page_DG오더_nExpandedHeight;
        Debug.WriteLine($"[{AppName}] IsDG오더Expanded: Height={rc.Height}, Threshold={fInfo.접수등록Page_DG오더_nExpandedHeight}, Result={bExpanded}");
        return bExpanded;
    }

    /// <summary>
    /// 데이터그리드 확장 (축소 상태일 때만)
    /// </summary>
    public async Task<bool> ExpandDG오더Async()
    {
        if (IsDG오더Expanded())
        {
            Debug.WriteLine($"[{AppName}] ExpandDG오더Async: 이미 확장 상태");
            return true;
        }

        Debug.WriteLine($"[{AppName}] ExpandDG오더Async: 확장 시도");
        await Std32Mouse_Post.MousePostAsync_ClickLeft(mRcpt.검색섹션_hWnd확장버튼);

        // 확장 대기 (높이 체크)
        for (int i = 0; i < c_nRepeatNormal; i++)
        {
            await Task.Delay(c_nWaitShort);
            if (IsDG오더Expanded())
            {
                Debug.WriteLine($"[{AppName}] ExpandDG오더Async: 확장 성공");
                return true;
            }
        }
        Debug.WriteLine($"[{AppName}] ExpandDG오더Async: 확장 실패");
        return false;
    }

    /// <summary>
    /// 데이터그리드 축소 (확장 상태일 때만)
    /// </summary>
    public async Task<bool> CollapseDG오더Async()
    {
        if (!IsDG오더Expanded())
        {
            Debug.WriteLine($"[{AppName}] CollapseDG오더Async: 이미 축소 상태");
            return true;
        }

        Debug.WriteLine($"[{AppName}] CollapseDG오더Async: 축소 시도");
        await Std32Mouse_Post.MousePostAsync_ClickLeft(mRcpt.검색섹션_hWnd확장버튼);

        // 축소 대기 (높이 체크)
        for (int i = 0; i < c_nRepeatNormal; i++)
        {
            await Task.Delay(c_nWaitShort);
            if (!IsDG오더Expanded())
            {
                Debug.WriteLine($"[{AppName}] CollapseDG오더Async: 축소 성공");
                return true;
            }
        }
        Debug.WriteLine($"[{AppName}] CollapseDG오더Async: 축소 실패");
        return false;
    }
    #endregion
}
#nullable restore
