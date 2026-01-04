using System.Diagnostics;
using Draw = System.Drawing;

using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;
using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgWnd;

using Kai.Client.CallCenter.Classes;
using static Kai.Client.CallCenter.Classes.CommonVars;
using System.Threading.Tasks;

namespace Kai.Client.CallCenter.Networks.NwInsungs;
#nullable disable

// 인성 앱 메인 윈도우 초기화 및 제어 담당 클래스 (Context 패턴 사용)
public class InsungsAct_MainWnd
{
    // Context 참조
    private readonly InsungContext m_Context;

    // 편의를 위한 로컬 참조들
    private InsungsInfo_File m_FileInfo => m_Context.FileInfo;
    private InsungsInfo_Mem m_MemInfo => m_Context.MemInfo;
    private InsungsInfo_Mem.MainWnd m_Main => m_MemInfo.Main;

    // 생성자 - Context를 받아서 초기화
    public InsungsAct_MainWnd(InsungContext context)
    {
        m_Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    #region MainWnd Initialize
    // 메인 윈도우 초기화
    public async Task<StdResult_Error> InitializeAsync(bool bEdit = true, bool bWrite = true, bool bMsgBox = true)
    {
        try
        {
            Debug.WriteLine($"[{m_Context.AppName}/MainWnd] 초기화 시작");

            // 1. 메인 윈도우 찾기 (최대 60초)
            for (int i = 0; i < 600; i++)
            {
                m_Main.TopWnd_hWnd = Std32Window.FindMainWindow_Reduct(m_MemInfo.Splash.TopWnd_uProcessId,
                    null, m_FileInfo.Main_TopWnd_sWndNameReduct);

                if (m_Main.TopWnd_hWnd != IntPtr.Zero) break;
                await Task.Delay(c_nWaitNormal);
            }

            if (m_Main.TopWnd_hWnd == IntPtr.Zero)
            {
                return new StdResult_Error(
                    $"[{m_Context.AppName}/MainWnd] 메인윈도우를 찾을 수 없습니다: {m_FileInfo.Main_TopWnd_sWndNameReduct}", 
                    "InsungsAct_MainWnd/InitializeAsync_01");
            }

            // 2. 초기화 과정 중 TopMost 유지
            Std32Window.SetWindowTopMost(m_Main.TopWnd_hWnd, true);

            try
            {
                // 3. 중복 접속 및 불필요한 새 창 정리
                Std32Process.GetWindowThreadProcessId(m_Main.TopWnd_hWnd, out uint uProcessId);
                DateTime last = DateTime.Now;

                for (int i = 0; i < c_nRepeatNormal; i++) // 약 10초간 감시
                {
                    await Task.Delay(c_nWaitNormal);
                    
                    if ((DateTime.Now - last).TotalSeconds > 3) break;

                    List<IntPtr> lstCurWnds = Std32Window.FindMainWindows_SameProcessId(uProcessId);
                    foreach (IntPtr hWnd in lstCurWnds)
                    {
                        if (hWnd == m_Main.TopWnd_hWnd || hWnd == m_MemInfo.Splash.TopWnd_hWnd) continue;

                        string sCurCaption = Std32Window.GetWindowCaption(hWnd);
                        if (!StdUtil.ContainsHangul(sCurCaption)) continue;

                        // 2초 후 자동 닫기 전용 테스크
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(2000);
                            StdWin32.PostMessage(hWnd, StdCommon32.WM_SYSCOMMAND, StdCommon32.SC_CLOSE, 0);
                        });
                        last = DateTime.Now;
                    }

                    // 중복접속 팝업 체크
                    IntPtr hWndMulti = Std32Window.FindMainWindow(uProcessId, "#32770", "");
                    if (hWndMulti != IntPtr.Zero && Std32Window.FindWindowEx(hWndMulti, IntPtr.Zero, null, "중복접속 입니다.") != IntPtr.Zero)
                    {
                        Std32Window.PostCloseTwiceWindow(m_Main.TopWnd_hWnd);
                        return new StdResult_Error(
                            $"[{m_Context.AppName}/MainWnd] 중복 접속이 감지되어 종료합니다.", "InsungsAct_MainWnd/InitializeAsync_02");
                    }
                }

                // 4. 작업 모니터로 이동 및 최대화
                Draw.Rectangle rcMain = Std32Window.GetWindowRect_DrawAbs(m_Main.TopWnd_hWnd);
                StdWin32.MoveWindow(m_Main.TopWnd_hWnd,
                    s_Screens.m_WorkingMonitor.PositionX, s_Screens.m_WorkingMonitor.PositionY, rcMain.Width, rcMain.Height, true);
                StdWin32.PostMessage(m_Main.TopWnd_hWnd, StdCommon32.WM_SYSCOMMAND, StdCommon32.SC_MAXIMIZE, IntPtr.Zero);

                // 5. 이동 완료 대기 (최대 30초)
                Draw.Point ptTarget = s_Screens.m_WorkingMonitor._ptLeftTop;
                bool bMoved = false;
                for (int i = 0; i < 300; i++)
                {
                    if (Std32Window.GetParentWndHandle_FromAbsDrawPt(ptTarget) == m_Main.TopWnd_hWnd)
                    {
                        bMoved = true;
                        break;
                    }
                    await Task.Delay(c_nWaitNormal);
                }

                if (!bMoved)
                {
                    return new StdResult_Error($"[{m_Context.AppName}/MainWnd] 이동 확인 실패", "InsungsAct_MainWnd/InitializeAsync_03");
                }
                await Task.Delay(c_nWaitVeryLong);

                // 6. 차일드 윈도우(컨테이너) 수집
                m_Main.FirstLayer_ChildWnds = Std32Window.GetChildWindows_FirstLayer(m_Main.TopWnd_hWnd);
                if (m_Main.FirstLayer_ChildWnds == null || m_Main.FirstLayer_ChildWnds.Count == 0)
                {
                    return new StdResult_Error(
                        $"[{m_Context.AppName}/MainWnd] 차일드 윈도우를 찾을 수 없습니다.", "InsungsAct_MainWnd/InitializeAsync_04");
                }

                // 7. 핵심 영역(MainMenu, BarMenu, MdiClient) 핸들 확정
                m_Main.WndInfo_MainMenu = m_Main.FirstLayer_ChildWnds.FirstOrDefault(x => x.rcRel == m_FileInfo.Main_MainMenu_rcRelF);
                m_Main.WndInfo_BarMenu = m_Main.FirstLayer_ChildWnds.FirstOrDefault(x => x.rcRel == m_FileInfo.Main_BarMenu_rcRelF);
                m_Main.WndInfo_MdiClient = m_Main.FirstLayer_ChildWnds.FirstOrDefault(x => x.rcRel == m_FileInfo.Main_MdiClient_rcRelF);

                if (m_Main.WndInfo_MainMenu == null || m_Main.WndInfo_BarMenu == null || m_Main.WndInfo_MdiClient == null)
                {
                    return new StdResult_Error(
                        $"[{m_Context.AppName}/MainWnd] 필수 UI 영역(Menu/MDI) 찾기 실패", "InsungsAct_MainWnd/InitializeAsync_05");
                }

                Debug.WriteLine($"[{m_Context.AppName}/MainWnd] 초기화 성공");
                return null;
            }
            finally
            {
                // 8. 초기화 완료 후 TopMost 해제
                Std32Window.SetWindowTopMost(m_Main.TopWnd_hWnd, false);
            }
        }
        catch (Exception ex)
        {
            return new StdResult_Error(
                $"[{m_Context.AppName}/MainWnd] 초기화 중 예외: {ex.Message}", "InsungsAct_MainWnd/InitializeAsync_999");
        }
    }
    #endregion

    #region Utility Methods
    //바메뉴 - 접수등록 클릭
    public async Task ClickAsync접수등록()
    {
        if (m_Main.WndInfo_BarMenu == null || m_Main.WndInfo_BarMenu.hWnd == IntPtr.Zero)
        {
            Debug.WriteLine($"[InsungsAct_MainWnd] BarMenu 핸들이 없습니다.");
            return;
        }

        // BarMenu의 상대 좌표로 마우스 클릭
        await Std32Mouse_Post.MousePostAsync_ClickLeft_ptRel(m_Main.WndInfo_BarMenu.hWnd, m_FileInfo.Main_BarMenu_접수등록_ptRelL);
        //Debug.WriteLine($"[InsungsAct_MainWnd] 접수등록 바메뉴 클릭: {m_FileInfo.Main_BarMenu_접수등록_ptRelL}");
    }

    //메인 윈도우가 초기화되었는지 확인
    public bool IsInitialized()
    {
        return m_Main.TopWnd_hWnd != IntPtr.Zero;
    }

    //메인 윈도우 핸들 가져오기
    public IntPtr GetHandle()
    {
        return m_Main.TopWnd_hWnd;
    }
    #endregion
}
#nullable enable
