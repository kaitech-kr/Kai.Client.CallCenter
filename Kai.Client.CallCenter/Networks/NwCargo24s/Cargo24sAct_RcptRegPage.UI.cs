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
using System.Threading;
using static Kai.Client.CallCenter.Classes.CommonVars;
using Draw = System.Drawing;

namespace Kai.Client.CallCenter.Networks.NwCargo24s;
#nullable disable

public partial class Cargo24sAct_RcptRegPage
{
    #region 1. Helpers - 공용 헬퍼

    // StatusBtn 찾기 및 검증 (취소 토큰 체크 포함)
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

    // 대화상자(TMessageForm) 대기 및 OK 버튼 클릭 헬퍼
    private async Task WaitAndConfirmDialogAsync()
    {
        for (int i = 0; i < c_nRepeatVeryMany; i++)
        {
            await Task.Delay(c_nWaitShort);
            List<IntPtr> lstMsg = Std32Window.FindMainWindows_SameProcessId(m_Splash.TopWnd_uProcessId);
            if (lstMsg == null || lstMsg.Count == 0) continue;

            foreach (IntPtr hWnd in lstMsg)
            {
                if (Std32Window.GetWindowClassName(hWnd) != DLG_MSG_CLASS) continue;
                if (!Std32Window.IsWindowVisible(hWnd)) continue;

                string caption = Std32Window.GetWindowCaption(hWnd) ?? "";
                if (!DLG_CAPTIONS.Contains(caption)) continue;

                IntPtr hWndBtn = IntPtr.Zero;
                foreach (var btnCaption in BTN_CAPTIONS)
                {
                    hWndBtn = Std32Window.FindChildWindow(hWnd, BTN_OK_CLASS, btnCaption);
                    if (hWndBtn != IntPtr.Zero) break;
                }

                if (hWndBtn != IntPtr.Zero)
                {
                    await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndBtn);
                    for (int j = 0; j < c_nRepeatShort; j++)
                    {
                        await Task.Delay(c_nWaitShort);
                        if (!Std32Window.IsWindow(hWnd)) break;
                    }
                    return;
                }
            }
        }
    }

    // 접수등록 페이지가 초기화되었는지 확인
    public bool IsInitialized()
    {
        if (m_RcptPage.TopWnd_hWnd == IntPtr.Zero || m_RcptPage.DG오더_hWnd == IntPtr.Zero) return false;
        if (m_RcptPage.DG오더_rcRelCells == null || m_RcptPage.DG오더_ColumnTexts == null) return false;
        return true;
    }

    // 접수등록 페이지 핸들 가져오기
    public IntPtr GetHandle() => m_RcptPage.TopWnd_hWnd;
    #endregion

    #region 2. DG State - DG오더 UI 상태
    // DG오더 유효 로우 수 반환 (배경 밝기 비교)
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

    // 분석된 컬럼 정보를 바탕으로 셀 좌표 일괄 계산 (헬퍼)
    private void CalculateCellRects(List<OfrModel_LeftWidth> listLW)
    {
        int columns = listLW.Count;
        int rowCount = m_FileInfo.접수등록Page_DG오더_rowCount;
        int rowHeight = m_FileInfo.접수등록Page_DG오더_rowHeight;
        int gab = m_FileInfo.접수등록Page_DG오더_dataGab;
        int textHeight = rowHeight - gab * 2;

        m_RcptPage.DG오더_rcRelCells = new Draw.Rectangle[columns, rowCount];
        m_RcptPage.DG오더_ptRelChkRows = new Draw.Point[rowCount];

        for (int row = 0; row < rowCount; row++)
        {
            int cellY = HEADER_HEIGHT + (row * rowHeight) + 1;
            m_RcptPage.DG오더_ptRelChkRows[row] = new Draw.Point(listLW[0].nLeft + (listLW[0].nWidth / 2), cellY + (rowHeight / 2));

            for (int col = 0; col < columns; col++)
            {
                m_RcptPage.DG오더_rcRelCells[col, row] = new Draw.Rectangle(listLW[col].nLeft, cellY + gab, listLW[col].nWidth, textHeight);
            }
        }
    }

    // 그리드 헤더 영역 픽셀 분석 헬퍼
    private async Task<List<OfrModel_LeftWidth>> AnalyzeGridHeadersAsync()
    {
        using var bmpHeader = OfrService.CaptureScreenRect_InWndHandle(m_RcptPage.DG오더_hWnd, new Draw.Rectangle(0, 0, m_RcptPage.DG오더_AbsRect.Width, HEADER_HEIGHT));
        if (bmpHeader == null) return null;

        byte maxBrightness = OfrService.GetMaxBrightnessAtRow_FromColorBitmapFast(bmpHeader, TARGET_ROW);
        if (maxBrightness == 0) return null;

        maxBrightness -= (byte)BRIGHTNESS_OFFSET;
        bool[] boolArr = OfrService.GetBoolArray_FromColorBitmapRowFast(bmpHeader, TARGET_ROW, maxBrightness, 2);
        var list = OfrService.GetLeftWidthList_FromBool1Array(boolArr, maxBrightness);

        if (list != null && list.Count > 0) list.RemoveAt(list.Count - 1);
        return list;
    }
    #endregion

    #region 9. 마우스 드래그 Methods
    public static async Task<bool> DragAsync_Horizontal_Smooth(IntPtr hWnd, Draw.Point ptStartRel, int dx, bool bCheckGrip = false, int nMiliSec = 100)
    {
        try
        {
            Std32Window.SetForegroundWindow(hWnd);
            Std32Cursor.SetCursorPos_RelDrawPt(hWnd, ptStartRel);
            Draw.Point ptStartAbs = Std32Cursor.GetCursorPos_AbsDrawPt();
            Draw.Point ptEndAbs = new Draw.Point(ptStartAbs.X + dx, ptStartAbs.Y);

            Std32Mouse_Event.MouseEvent_LeftBtnDown();
            await Task.Delay(50);

            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < nMiliSec)
            {
                double ratio = (double)sw.ElapsedMilliseconds / nMiliSec;
                int currentDx = (int)(dx * ratio);
                Std32Cursor.SetCursorPos_AbsDrawPt(new Draw.Point(ptStartAbs.X + currentDx, ptStartAbs.Y));
                await Task.Delay(10);
            }

            Std32Cursor.SetCursorPos_AbsDrawPt(ptEndAbs);
            await Task.Delay(50);
            Std32Mouse_Event.MouseEvent_LeftBtnUp();
            await Task.Delay(200);
            return true;
        }
        catch { return false; }
    }
    #endregion
}
#nullable restore
