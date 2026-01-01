using System.Diagnostics;
using Draw = System.Drawing;

using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;

using Kai.Client.CallCenter.Classes;
using static Kai.Client.CallCenter.Classes.CommonVars;

namespace Kai.Client.CallCenter.Networks.NwCargo24s;
#nullable disable

// 화물24시 앱 메인 윈도우 초기화 및 제어 담당 클래스
public class Cargo24sAct_MainWnd
{
    // Context 참조
    private readonly Cargo24Context m_Context;

    // 편의를 위한 로컬 참조들
    private Cargo24sInfo_File m_FileInfo => m_Context.FileInfo;
    private Cargo24sInfo_Mem m_MemInfo => m_Context.MemInfo;
    private Cargo24sInfo_Mem.MainWnd m_Main => m_MemInfo.Main;
    private Cargo24sInfo_Mem.SplashWnd m_Splash => m_MemInfo.Splash;

    // 생성자 - Context를 받아서 초기화
    public Cargo24sAct_MainWnd(Cargo24Context context)
    {
        m_Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    #region MainWnd Initialize
    // 메인 윈도우 초기화 (핸들 취득, 모니터 이동, 최대화, 자식 윈도우 구성)
    public async Task<StdResult_Status> InitializeAsync(bool bEdit = true, bool bWrite = true, bool bMsgBox = true)
    {
        try
        {
            Debug.WriteLine($"[{m_Context.AppName}/MainWnd] 초기화 시작");

            // 1. 메인 윈도우 찾기 (최대 10초 대기)
            for (int i = 0; i < c_nRepeatNormal; i++)
            {
                if (s_GlobalCancelToken.Token.IsCancellationRequested) return new StdResult_Status(StdResult.Fail, "작업 취소됨", "Cargo24sAct_MainWnd/InitializeAsync_Cancel1");

                await Task.Run(() =>
                {
                    m_Main.TopWnd_hWnd = Std32Window.FindMainWindow_Reduct(
                        m_Splash.TopWnd_uProcessId,
                        m_FileInfo.Main_TopWnd_sClassName,
                        m_FileInfo.Main_TopWnd_sWndNameReduct);
                });

                if (m_Main.TopWnd_hWnd != IntPtr.Zero) break;
                await Task.Delay(c_nWaitShort);
            }

            if (m_Main.TopWnd_hWnd == IntPtr.Zero)
            {
                return new StdResult_Status(StdResult.Fail, $"[{m_Context.AppName}/MainWnd] 메인 윈도우 찾기 실패", "Cargo24sAct_MainWnd/InitializeAsync_01");
            }
            Debug.WriteLine($"[{m_Context.AppName}/MainWnd] 메인 윈도우 확인: {m_Main.TopWnd_hWnd}");

            // 2. 초기화 과정 중 창을 전면에 고정 (다른 창에 가려짐 방지)
            Std32Window.SetWindowTopMost(m_Main.TopWnd_hWnd, true);

            // 3. 모니터 이동 및 최대화 (화물24시는 SW_NORMAL 상태에서 이동해야 안전함)
            StdWin32.ShowWindow(m_Main.TopWnd_hWnd, (int)StdCommon32.SW_NORMAL);
            await Task.Delay(c_nWaitShort);

            Draw.Rectangle rcMain = Std32Window.GetWindowRect_DrawAbs(m_Main.TopWnd_hWnd);
            StdWin32.MoveWindow(m_Main.TopWnd_hWnd, 
                s_Screens.m_WorkingMonitor.PositionX, s_Screens.m_WorkingMonitor.PositionY, rcMain.Width, rcMain.Height, true);

            await Task.Delay(c_nWaitLong); // 300ms 안정화

            if (s_GlobalCancelToken.Token.IsCancellationRequested) 
                return new StdResult_Status(StdResult.Fail, "작업 취소됨", "Cargo24sAct_MainWnd/InitializeAsync_Cancel2");

            // 최대화 전송
            StdWin32.PostMessage(m_Main.TopWnd_hWnd, StdCommon32.WM_SYSCOMMAND, (uint)StdCommon32.SC_MAXIMIZE, IntPtr.Zero);
            await Task.Delay(c_nWaitShort);

            // 4. 이동 결과 확인 (최대 약 3초 대기)
            bool bMoved = false;
            for (int i = 0; i < c_nRepeatShort; i++) // 50회 * 50ms = 2.5초 (근사값)
            {
                if (s_GlobalCancelToken.Token.IsCancellationRequested) return new StdResult_Status(StdResult.Fail, "작업 취소됨", "Cargo24sAct_MainWnd/InitializeAsync_Cancel_Moved");

                if (Std32Window.GetParentWndHandle_FromAbsDrawPt(s_Screens.m_WorkingMonitor._ptLeftTop) == m_Main.TopWnd_hWnd)
                {
                    bMoved = true;
                    break;
                }
                await Task.Delay(c_nWaitShort);
            }

            if (!bMoved)
            {
                return new StdResult_Status(StdResult.Fail, $"[{m_Context.AppName}/MainWnd] 이동 확인 실패 (작업 모니터 안착 실패)", "Cargo24sAct_MainWnd/InitializeAsync_02");
            }
            Debug.WriteLine($"[{m_Context.AppName}/MainWnd] 메인 윈도우 이동 및 안착 확인 완료");

            // 5. 자식 윈도우 및 메뉴 구성 요소 찾기 (최대 5초 대기)
            for (int i = 0; i < c_nRepeatNormal; i++) // 100회 * 50ms = 5초
            {
                if (s_GlobalCancelToken.Token.IsCancellationRequested) return new StdResult_Status(StdResult.Fail, "작업 취소됨", "Cargo24sAct_MainWnd/InitializeAsync_Cancel3");

                m_Main.FirstLayer_ChildWnds = Std32Window.GetChildWindows_FirstLayer(m_Main.TopWnd_hWnd);
                if (m_Main.FirstLayer_ChildWnds != null && m_Main.FirstLayer_ChildWnds.Count > 0)
                {
                    m_Main.WndInfo_MainMenu = m_Main.FirstLayer_ChildWnds.FirstOrDefault(x => x.rcRel == m_FileInfo.Main_MainMenu_rcRelF);
                    m_Main.WndInfo_BarMenu = m_Main.FirstLayer_ChildWnds.FirstOrDefault(x => x.className == m_FileInfo.Main_BarMenu_ClassName);
                    m_Main.WndInfo_MdiClient = m_Main.FirstLayer_ChildWnds.FirstOrDefault(x => x.className == m_FileInfo.Main_MdiClient_ClassName);

                    if (m_Main.WndInfo_MainMenu != null && m_Main.WndInfo_BarMenu != null && m_Main.WndInfo_MdiClient != null) break;
                }
                await Task.Delay(c_nWaitShort);
            }

            // 필수 하위 핸들 체크
            if (m_Main.WndInfo_MainMenu == null) return new StdResult_Status(StdResult.Fail, "MainMenu를 찾을 수 없습니다.", "Cargo24sAct_MainWnd/InitializeAsync_03");
            if (m_Main.WndInfo_BarMenu == null) return new StdResult_Status(StdResult.Fail, "BarMenu를 찾을 수 없습니다.", "Cargo24sAct_MainWnd/InitializeAsync_04");
            if (m_Main.WndInfo_MdiClient == null) return new StdResult_Status(StdResult.Fail, "MdiClient를 찾을 수 없습니다.", "Cargo24sAct_MainWnd/InitializeAsync_05");

            Debug.WriteLine($"[{m_Context.AppName}/MainWnd] 초기화 완료");
            return new StdResult_Status(StdResult.Success);
        }
        catch (Exception ex)
        {
            return new StdResult_Status(StdResult.Fail, $"[{m_Context.AppName}/MainWnd] 예외발생: {ex.Message}", "Cargo24sAct_MainWnd/InitializeAsync_99");
        }
        finally
        {
            if (m_Main.TopWnd_hWnd != IntPtr.Zero)
            {
                Std32Window.SetWindowTopMost(m_Main.TopWnd_hWnd, false);
            }
        }
    }
    #endregion

    #region Utility Methods
    // 메인 윈도우가 초기화되었는지 확인
    public bool IsInitialized()
    {
        return m_Main.TopWnd_hWnd != IntPtr.Zero;
    }

    public IntPtr GetHandle()
    {
        return m_Main.TopWnd_hWnd;
    }
    #endregion
}
#nullable restore
