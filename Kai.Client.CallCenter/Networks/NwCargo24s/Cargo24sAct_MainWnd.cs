using System.Diagnostics;
using Draw = System.Drawing;

using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;

using Kai.Client.CallCenter.Classes;
using static Kai.Client.CallCenter.Classes.CommonVars;

namespace Kai.Client.CallCenter.Networks.NwCargo24s;
#nullable disable

/// <summary>
/// 화물24시 앱 메인 윈도우 초기화 및 제어 담당 클래스
/// Context 패턴 사용: Cargo24Context를 통해 모든 정보에 접근
/// </summary>
public class Cargo24sAct_MainWnd
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
    private Cargo24sInfo_Mem.SplashWnd m_Splash => m_MemInfo.Splash;
    #endregion

    #region Constructor
    /// <summary>
    /// 생성자 - Context를 받아서 초기화
    /// </summary>
    public Cargo24sAct_MainWnd(Cargo24Context context)
    {
        m_Context = context ?? throw new ArgumentNullException(nameof(context));
        Debug.WriteLine($"[Cargo24sAct_MainWnd] 생성자 호출: AppName={m_Context.AppName}");
    }
    #endregion

    #region MainWnd Initialize
    /// <summary>
    /// 메인 윈도우 초기화 (백업 로직 완전 복원)
    /// 1. 메인 윈도우 찾기
    /// 2. TopMost 설정/해제
    /// 3. ShowWindow(SW_NORMAL) - 화물24시 필수!
    /// 4. 작업 모니터로 이동 및 최대화
    /// 5. 이동 확인
    /// 6. 차일드 윈도우 찾기
    /// 7. MainMenu, BarMenu, MdiClient 찾기
    /// </summary>
    public async Task<StdResult_Error> InitializeAsync(bool bEdit = true, bool bWrite = true, bool bMsgBox = true)
    {
        try
        {
            Debug.WriteLine($"[Cargo24sAct_MainWnd] 초기화 시작");

            // 1. 메인 윈도우 찾기 (10초 대기)
            for (int i = 0; i < c_nRepeatVeryMany; i++) // 10초 동안 (100회 * 100ms)
            {
                await Task.Run(() =>
                {
                    // Main Window
                    m_Main.TopWnd_hWnd = Std32Window.FindMainWindow_Reduct(
                        m_Splash.TopWnd_uProcessId,
                        m_FileInfo.Main_TopWnd_sClassName,
                        m_FileInfo.Main_TopWnd_sWndNameReduct);
                });

                if (m_Main.TopWnd_hWnd != IntPtr.Zero) break;
                await Task.Delay(c_nWaitNormal);
            }

            if (m_Main.TopWnd_hWnd == IntPtr.Zero)
            {
                return CommonFuncs_StdResult.ErrMsgResult_Error(
                    $"[{m_Context.AppName}/MainWnd]메인윈도 찾기실패",
                    "Cargo24sAct_MainWnd/InitializeAsync_01", bWrite, bMsgBox);
            }
            Debug.WriteLine($"[Cargo24sAct_MainWnd] 메인 윈도우 찾음: {m_Main.TopWnd_hWnd}");

            // 2. TopMost 설정 및 해제
            await Std32Window.SetWindowTopMostAndReleaseAsync(m_Main.TopWnd_hWnd);
            Debug.WriteLine($"[Cargo24sAct_MainWnd] TopMost 설정 및 해제 완료");

            // 3. 이동 및 최대화 (화물24시는 ShowWindow(SW_NORMAL) 필수!)
            await Task.Run(async () =>
            {
                // ShowWindow(SW_NORMAL) - NormalSize Main Window For Move
                StdWin32.ShowWindow(m_Main.TopWnd_hWnd, (int)StdCommon32.SW_NORMAL);

                Draw.Rectangle rcMain = Std32Window.GetWindowRect_DrawAbs(m_Main.TopWnd_hWnd);
                StdWin32.MoveWindow(m_Main.TopWnd_hWnd,
                    s_Screens.m_WorkingMonitor.PositionX,
                    s_Screens.m_WorkingMonitor.PositionY,
                    rcMain.Width, rcMain.Height, true);

                await Task.Delay(c_nWaitVeryLong); // 500ms

                // Maximize Main Window
                StdWin32.PostMessage(m_Main.TopWnd_hWnd, StdCommon32.WM_SYSCOMMAND,
                    StdCommon32.SC_MAXIMIZE, IntPtr.Zero);
            });

            // 4. 이동 확인 대기 (10초)
            IntPtr hWndFind = IntPtr.Zero;
            Draw.Point ptTarget = s_Screens.m_WorkingMonitor._ptLeftTop;
            for (int i = 0; i < c_nRepeatVeryMany; i++) // 10초 (100회 * 100ms)
            {
                hWndFind = Std32Window.GetParentWndHandle_FromAbsDrawPt(ptTarget);
                if (hWndFind == m_Main.TopWnd_hWnd) break;
                Thread.Sleep(c_nWaitNormal); // 100ms
            }

            if (hWndFind != m_Main.TopWnd_hWnd)
            {
                string capMain = Std32Window.GetWindowCaption(m_Main.TopWnd_hWnd);
                string capFind = Std32Window.GetWindowCaption(hWndFind);
                return CommonFuncs_StdResult.ErrMsgResult_Error(
                    $"[{m_Context.AppName}/MainWnd]이동실패: {m_Main.TopWnd_hWnd:X}, {hWndFind:X}, {ptTarget}, {capMain}, {capFind}",
                    "Cargo24sAct_MainWnd/InitializeAsync_02", bWrite, bMsgBox);
            }
            await Task.Delay(c_nWaitVeryLong); // 500ms
            Debug.WriteLine($"[Cargo24sAct_MainWnd] 메인 윈도우 이동 및 최대화 완료");

            // 5. 차일드 윈도우 정보 수집
            m_Main.FirstLayer_ChildWnds = Std32Window.GetChildWindows_FirstLayer(m_Main.TopWnd_hWnd);
            if (m_Main.FirstLayer_ChildWnds.Count == 0)
            {
                return CommonFuncs_StdResult.ErrMsgResult_Error(
                    $"[{m_Context.AppName}/MainWnd]자식윈도 못찾음",
                    "Cargo24sAct_MainWnd/InitializeAsync_03", bWrite, bMsgBox);
            }
            Debug.WriteLine($"[Cargo24sAct_MainWnd] 차일드 윈도우 개수: {m_Main.FirstLayer_ChildWnds.Count}");

            // Debug: 모든 차일드 윈도우 정보 출력
            for (int i = 0; i < m_Main.FirstLayer_ChildWnds.Count; i++)
            {
                var wnd = m_Main.FirstLayer_ChildWnds[i];
                Debug.WriteLine($"[Cargo24sAct_MainWnd] Child[{i}]: className={wnd.className}, rcRel={wnd.rcRel}");
            }

            // 6. MainMenu 찾기 - Rect으로 찾기
            m_Main.WndInfo_MainMenu = m_Main.FirstLayer_ChildWnds.FirstOrDefault(
                x => x.rcRel == m_FileInfo.Main_MainMenu_rcRel);
            if (m_Main.WndInfo_MainMenu == null)
            {
                return CommonFuncs_StdResult.ErrMsgResult_Error(
                    $"[{m_Context.AppName}/MainWnd]메인메뉴 못찾음",
                    "Cargo24sAct_MainWnd/InitializeAsync_04", bWrite, bMsgBox);
            }

            // 7. BarMenu 찾기 - ClassName으로 찾기
            m_Main.WndInfo_BarMenu = m_Main.FirstLayer_ChildWnds.FirstOrDefault(
                x => x.className == m_FileInfo.Main_BarMenu_ClassName);
            if (m_Main.WndInfo_BarMenu == null)
            {
                return CommonFuncs_StdResult.ErrMsgResult_Error(
                    $"[{m_Context.AppName}/MainWnd]바메뉴 못찾음",
                    "Cargo24sAct_MainWnd/InitializeAsync_05", bWrite, bMsgBox);
            }

            // 8. MdiClient 찾기 - ClassName으로 찾기
            m_Main.WndInfo_MdiClient = m_Main.FirstLayer_ChildWnds.FirstOrDefault(
                x => x.className == m_FileInfo.Main_MdiClient_ClassName);
            if (m_Main.WndInfo_MdiClient == null)
            {
                return CommonFuncs_StdResult.ErrMsgResult_Error(
                    $"[{m_Context.AppName}/MainWnd]MdiClient 못찾음",
                    "Cargo24sAct_MainWnd/InitializeAsync_06", bWrite, bMsgBox);
            }
            Debug.WriteLine($"[Cargo24sAct_MainWnd] MainMenu, BarMenu, MdiClient 찾기 완료");

            Debug.WriteLine($"[Cargo24sAct_MainWnd] 초기화 완료");
            return null; // 성공
        }
        catch (Exception ex)
        {
            return CommonFuncs_StdResult.ErrMsgResult_Error(
                $"[{m_Context.AppName}/MainWnd]예외발생: {ex.Message}",
                "Cargo24sAct_MainWnd/InitializeAsync_999", bWrite, bMsgBox);
        }
    }
    #endregion

    #region Utility Methods
    /// <summary>
    /// 메인 윈도우가 초기화되었는지 확인
    /// </summary>
    public bool IsInitialized()
    {
        return m_Main.TopWnd_hWnd != IntPtr.Zero;
    }

    /// <summary>
    /// 메인 윈도우 핸들 가져오기
    /// </summary>
    public IntPtr GetHandle()
    {
        return m_Main.TopWnd_hWnd;
    }
    #endregion
}
#nullable restore
