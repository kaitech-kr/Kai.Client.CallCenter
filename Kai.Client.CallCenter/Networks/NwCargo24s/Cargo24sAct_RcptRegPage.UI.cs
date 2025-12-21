using Kai.Client.CallCenter.Classes;
using Kai.Client.CallCenter.Classes.Class_Master;
using Kai.Client.CallCenter.OfrWorks;
using Kai.Client.CallCenter.Windows;
using Kai.Common.NetDll_WpfCtrl.NetOFR;
using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models;
using System.Diagnostics;
using System.Windows.Media;
using static Kai.Client.CallCenter.Classes.CommonVars;
using Draw = System.Drawing;

namespace Kai.Client.CallCenter.Networks.NwCargo24s;
#nullable disable

public partial class Cargo24sAct_RcptRegPage
{
    #region 1. Helpers - 공용 헬퍼
    // // StatusBtn 찾기 및 검증 (취소 토큰 체크 포함)
    private async Task<(IntPtr hWnd, StdResult_Status status)> FindStatusButtonAsync(
        string buttonName, Draw.Point checkPoint, string errorCode, bool withTextValidation = true)
    {
        for (int i = 0; i < c_nRepeatNormal; i++)
        {
            if (s_GlobalCancelToken.Token.IsCancellationRequested)
                return (IntPtr.Zero, new StdResult_Status(StdResult.Fail, "작업 취소됨", errorCode + "_Cancel"));

            IntPtr hWnd = Std32Window.GetWndHandle_FromRelDrawPt(m_Main.TopWnd_hWnd, checkPoint);

            if (hWnd != IntPtr.Zero)
            {
                if (withTextValidation)
                {
                    string text = Std32Window.GetWindowCaption(hWnd);
                    if (text != null && text.Contains(buttonName)) return (hWnd, new StdResult_Status(StdResult.Success));
                }
                else return (hWnd, new StdResult_Status(StdResult.Success));
            }

            await Task.Delay(c_nWaitShort);
        }

        return (IntPtr.Zero, new StdResult_Status(StdResult.Fail, 
            $"[{m_Context.AppName}/RcptRegPage] {buttonName} 버튼 찾기 실패: {checkPoint}", errorCode));
    }

     // 접수등록 페이지가 초기화되었는지 확인 (간단 체크)
    public bool IsInitialized()
    {
        if (m_RcptPage.TopWnd_hWnd == IntPtr.Zero || m_RcptPage.DG오더_hWnd == IntPtr.Zero) return false;
        if (m_RcptPage.DG오더_rcRelCells == null || m_RcptPage.DG오더_ColumnTexts == null) return false;
        return true;
    }

    // // 접수등록 페이지 핸들 가져오기
    public IntPtr GetHandle()
    {
        return m_RcptPage.TopWnd_hWnd;
    }
    #endregion

    #region 2. DG State - DG오더 UI 상태
    // // DG오더 유효 로우 수 반환 (배경 밝기 비교)
    public StdResult_Int GetValidRowCount()
    {
        try
        {
            Draw.Rectangle[,] rects = m_RcptPage.DG오더_rcRelCells;
            if (rects == null)
                return new StdResult_Int("DG오더_rcRelCells 미초기화", "GetValidRowCount_01");

            int nBackgroundBright = 50;
            int nThreshold = nBackgroundBright - 1;
            int nValidRows = 0;

            for (int y = 0; y < m_FileInfo.접수등록Page_DG오더_rowCount; y++)
            {
                int nCurBright = OfrService.GetPixelBrightnessFrmWndHandle(
                    m_RcptPage.DG오더_hWnd,
                    rects[0, y].Right,
                    rects[0, y].Top + 6);

                if (nCurBright < nThreshold)
                    nValidRows++;
                else
                    break;
            }

            return new StdResult_Int(nValidRows);
        }
        catch (Exception ex)
        {
            return new StdResult_Int(StdUtil.GetExceptionMessage(ex), "GetValidRowCount_999");
        }
    }
    #endregion
}
#nullable restore
