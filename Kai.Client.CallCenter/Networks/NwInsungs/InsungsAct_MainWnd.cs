using System.Diagnostics;
using Draw = System.Drawing;

using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;
using static Kai.Common.FrmDll_FormCtrl.FormFuncs;

using Kai.Client.CallCenter.Classes;
using static Kai.Client.CallCenter.Classes.CommonVars;
using System.Threading.Tasks;

namespace Kai.Client.CallCenter.Networks.NwInsungs;
#nullable disable

/// <summary>
/// 인성 앱 메인 윈도우 초기화 및 제어 담당 클래스
/// Context 패턴 사용: InsungContext를 통해 모든 정보에 접근
/// </summary>
public class InsungsAct_MainWnd
{
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
    #endregion

    #region Constructor
    /// <summary>
    /// 생성자 - Context를 받아서 초기화
    /// </summary>
    public InsungsAct_MainWnd(InsungContext context)
    {
        m_Context = context ?? throw new ArgumentNullException(nameof(context));
        Debug.WriteLine($"[InsungsAct_MainWnd] 생성자 호출: AppName={m_Context.AppName}");
    }
    #endregion

    #region MainWnd Initialize
    /// <summary>
    /// 메인 윈도우 초기화
    /// </summary>
    public async Task<StdResult_Error> InitializeAsync(bool bEdit = true, bool bWrite = true, bool bMsgBox = true)
    {
        try
        {
            Debug.WriteLine($"[InsungsAct_MainWnd] 초기화 시작");

            // 1. 메인 윈도우 찾기 (60초 대기)
            for (int i = 0; i < 600; i++)
            {
                await Task.Run(() =>
                {
                    m_Main.TopWnd_hWnd = Std32Window.FindMainWindow_Reduct(m_MemInfo.Splash.TopWnd_uProcessId,
                        null, m_FileInfo.Main_TopWnd_sWndNameReduct);
                });

                await Task.Delay(100);
                if (m_Main.TopWnd_hWnd != IntPtr.Zero) break;
            }

            if (m_Main.TopWnd_hWnd == IntPtr.Zero)
            {
                return CommonFuncs_StdResult.ErrMsgResult_Error(
                    $"[{m_Context.AppName}/MainWnd]메인윈도 찾기실패",
                    "InsungsAct_MainWnd/InitializeAsync_01", bWrite, bMsgBox);
            }
            Debug.WriteLine($"[InsungsAct_MainWnd] 메인 윈도우 찾음: {m_Main.TopWnd_hWnd}");

            // 2. TopMost 설정 및 해제
            await Std32Window.SetWindowTopMostAndReleaseAsync(m_Main.TopWnd_hWnd);
            Debug.WriteLine($"[InsungsAct_MainWnd] TopMost 설정 및 해제 완료");

            // 3. Minimize All New Windows - If in 3Sec Find New Window
            Std32Process.GetWindowThreadProcessId(m_Main.TopWnd_hWnd, out uint uProcessId);
            DateTime last = DateTime.Now;
            DateTime now = DateTime.Now;
            TimeSpan gab = now - last;
            IntPtr hWndMulti = IntPtr.Zero;
            IntPtr hWndMain = m_Main.TopWnd_hWnd;

            for (int i = 0; i < 100; i++) // 10초 동안 Explorer Wnd 찾기
            {
                Thread.Sleep(100); // 무조건 대기
                now = DateTime.Now;
                gab = now - last;
                if (gab.TotalSeconds > 3) break; // 3초 이상 지나면

                List<IntPtr> lstCurWnds = Std32Window.FindMainWindows_SameProcessId(uProcessId);

                foreach (IntPtr hWnd in lstCurWnds) // Explorer Wnd 찾기
                {
                    if (hWnd == m_Main.TopWnd_hWnd || hWnd == m_MemInfo.Splash.TopWnd_hWnd) continue; // 메인, 스플래쉬는 패스
                    string sCurCaption = Std32Window.GetWindowCaption(hWnd); // 캡션을 얻는다

                    if (!StdUtil.ContainsHangul(sCurCaption)) continue; // 한글이 없으면 패스

                    Thread t = new Thread(() =>
                    {
                        Thread.Sleep(2000); // 2초 보여주고 죽인다
                        StdWin32.PostMessage(hWnd, StdCommon32.WM_SYSCOMMAND, StdCommon32.SC_CLOSE, 0); // Close
                    });
                    t.IsBackground = true;
                    t.Start();

                    last = DateTime.Now;
                }

                // 중복접속창이 뜨면 닫는다
                await Task.Delay(100);
                hWndMulti = Std32Window.FindMainWindow(m_MemInfo.Splash.TopWnd_uProcessId, "#32770", "");
                if (hWndMulti != IntPtr.Zero)
                {
                    // 중복접속 Window
                    if (Std32Window.FindWindowEx(hWndMulti, IntPtr.Zero, null, "중복접속 입니다.") != IntPtr.Zero)
                    {
                        Std32Window.PostCloseTwiceWindow(hWndMain);
                        return CommonFuncs_StdResult.ErrMsgResult_Error(
                            $"[{m_Context.AppName}/MainWnd]중복접속창 발견", "InsungsAct_MainWnd/InitializeAsync_02", bWrite, bMsgBox);
                    }
                }
            }
            Debug.WriteLine($"[InsungsAct_MainWnd] 새 팝업창 처리 완료");

            // 4. 이동 및 최대화
            await Task.Run(() =>
            {
                Draw.Rectangle rcMain = Std32Window.GetWindowRect_DrawAbs(m_Main.TopWnd_hWnd);
                StdWin32.MoveWindow(m_Main.TopWnd_hWnd,
                    s_Screens.m_WorkingMonitor.PositionX, s_Screens.m_WorkingMonitor.PositionY, rcMain.Width, rcMain.Height, true);
                StdWin32.PostMessage(m_Main.TopWnd_hWnd, StdCommon32.WM_SYSCOMMAND, StdCommon32.SC_MAXIMIZE, IntPtr.Zero);
            });

            // 5. 이동 확인 대기
            IntPtr hWndFind = IntPtr.Zero;
            Draw.Point ptTarget = s_Screens.m_WorkingMonitor._ptLeftTop;
            for (int i = 0; i < 300; i++) // 30초
            {
                hWndFind = Std32Window.GetParentWndHandle_FromAbsDrawPt(ptTarget);
                if (hWndFind == m_Main.TopWnd_hWnd) break;
                Thread.Sleep(100);
            }

            if (hWndFind != m_Main.TopWnd_hWnd)
            {
                string capMain = Std32Window.GetWindowCaption(m_Main.TopWnd_hWnd);
                string capFind = Std32Window.GetWindowCaption(hWndFind);
                return CommonFuncs_StdResult.ErrMsgResult_Error(
                    $"[{m_Context.AppName}/MainWnd]이동실패: {m_Main.TopWnd_hWnd:X}, {hWndFind:X}, {ptTarget}, {capMain}, {capFind}",
                    "InsungsAct_MainWnd/InitializeAsync_03", bWrite, bMsgBox);
            }
            await Task.Delay(500);
            Debug.WriteLine($"[InsungsAct_MainWnd] 메인 윈도우 이동 및 최대화 완료");

            // 6. 차일드 윈도우 정보 수집
            m_Main.FirstLayer_ChildWnds = Std32Window.GetChildWindows_FirstLayer(m_Main.TopWnd_hWnd);
            if (m_Main.FirstLayer_ChildWnds.Count == 0)
            {
                return CommonFuncs_StdResult.ErrMsgResult_Error(
                    $"[{m_Context.AppName}/MainWnd]자식윈도 못찾음", "InsungsAct_MainWnd/InitializeAsync_04", bWrite, bMsgBox);
            }
            Debug.WriteLine($"[InsungsAct_MainWnd] 차일드 윈도우 개수: {m_Main.FirstLayer_ChildWnds.Count}");

            // 7. MainMenu, BarMenu, MdiClient 찾기
            m_Main.WndInfo_MainMenu = m_Main.FirstLayer_ChildWnds.FirstOrDefault(x => x.rcRel == m_FileInfo.Main_MainMenu_rcRel);
            if (m_Main.WndInfo_MainMenu == null)
            {
                return CommonFuncs_StdResult.ErrMsgResult_Error(
                    $"[{m_Context.AppName}/MainWnd]메인메뉴 못찾음", "InsungsAct_MainWnd/InitializeAsync_05", bWrite, bMsgBox);
            }

            m_Main.WndInfo_BarMenu = m_Main.FirstLayer_ChildWnds.FirstOrDefault(x => x.rcRel == m_FileInfo.Main_BarMenu_rcRel);
            if (m_Main.WndInfo_BarMenu == null)
            {
                return CommonFuncs_StdResult.ErrMsgResult_Error(
                    $"[{m_Context.AppName}/MainWnd]바메뉴 못찾음", "InsungsAct_MainWnd/InitializeAsync_06", bWrite, bMsgBox);
            }

            m_Main.WndInfo_MdiClient = m_Main.FirstLayer_ChildWnds.FirstOrDefault(x => x.rcRel == m_FileInfo.Main_MdiClient_rcRel);
            if (m_Main.WndInfo_MdiClient == null)
            {
                return CommonFuncs_StdResult.ErrMsgResult_Error(
                    $"[{m_Context.AppName}/MainWnd]MdiClient 못찾음", "InsungsAct_MainWnd/InitializeAsync_07", bWrite, bMsgBox);
            }
            Debug.WriteLine($"[InsungsAct_MainWnd] MainMenu, BarMenu, MdiClient 찾기 완료");

            Debug.WriteLine($"[InsungsAct_MainWnd] 초기화 완료");
            return null; // 성공
        }
        catch (Exception ex)
        {
            return CommonFuncs_StdResult.ErrMsgResult_Error(
                $"[{m_Context.AppName}/MainWnd]예외발생: {ex.Message}", "InsungsAct_MainWnd/InitializeAsync_999", bWrite, bMsgBox);
        }
    }
    #endregion

    #region BarMenu Click Methods
    /// <summary>
    /// 바메뉴 - 접수등록 클릭
    /// </summary>
    public async Task ClickAsync접수등록()
    {
        if (m_Main.WndInfo_BarMenu == null || m_Main.WndInfo_BarMenu.hWnd == IntPtr.Zero)
        {
            Debug.WriteLine($"[InsungsAct_MainWnd] BarMenu 핸들이 없습니다.");
            return;
        }

        // BarMenu의 상대 좌표로 마우스 클릭
        await Std32Mouse_Post.MousePostAsync_ClickLeft_ptRel(m_Main.WndInfo_BarMenu.hWnd, m_FileInfo.Main_BarMenu_pt접수등록);
        Debug.WriteLine($"[InsungsAct_MainWnd] 접수등록 바메뉴 클릭: {m_FileInfo.Main_BarMenu_pt접수등록}");
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
#nullable enable
