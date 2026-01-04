using System.Diagnostics;
using Draw = System.Drawing;

using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;
using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgBox;

using Kai.Client.CallCenter.Classes;
using static Kai.Client.CallCenter.Classes.CommonVars;

namespace Kai.Client.CallCenter.Networks.NwOnecalls;
#nullable disable

// 원콜 메인 윈도우 제어 (안정성 복구 버전)
public class OnecallAct_MainWnd
{
    #region Private Fields
    private readonly OnecallContext m_Context;
    private OnecallInfo_File fInfo => m_Context.FileInfo;
    private OnecallInfo_Mem mInfo => m_Context.MemInfo;
    private OnecallInfo_Mem.MainWnd mMain => mInfo.Main;
    private OnecallInfo_Mem.SplashWnd mSplash => mInfo.Splash;
    private string AppName => m_Context.AppName;
    #endregion

    #region 생성자
    public OnecallAct_MainWnd(OnecallContext context)
    {
        m_Context = context ?? throw new ArgumentNullException(nameof(context));
    }
    #endregion

    #region InitAsync
    public async Task<StdResult_Status> InitAsync()
    {
        try
        {
            Debug.WriteLine($"[{AppName}] MainWnd InitAsync 시작 (안정성 복구)");

            // 1. 메인 윈도우 찾기 (안정적인 FindMainWindow_Reduct 사용)
            for (int i = 0; i < 100; i++)
            {
                if (s_GlobalCancelToken.Token.IsCancellationRequested) return new StdResult_Status(StdResult.Skip, "사용자 요청 취소", "OnecallAct_MainWnd/Init_Cancel");
                mMain.TopWnd_hWnd = Std32Window.FindMainWindow_Reduct(mSplash.TopWnd_uProcessId, null, "(주)원콜");

                if (mMain.TopWnd_hWnd != IntPtr.Zero) break;
                await Task.Delay(c_nWaitShort);
            }

            if (mMain.TopWnd_hWnd == IntPtr.Zero)
                return new StdResult_Status(StdResult.Fail, $"[{AppName}] 메인윈도 찾기 실패", "OnecallAct_MainWnd/InitAsync_01");

            // 2. 초기화 과정 중 TopMost 유지
            Std32Window.SetWindowTopMost(mMain.TopWnd_hWnd, true);

            try
            {
                // 3. 안정적인 이동 및 최대화 실행
                var moveResult = await MoveAndMaximizeMainWindowAsync();
                if (moveResult.Result != StdResult.Success) return moveResult;

                // 4. 자식 윈도우 찾기
                mMain.FirstLayer_ChildWnds = Std32Window.GetChildWindows_FirstLayer(mMain.TopWnd_hWnd);
                if (mMain.FirstLayer_ChildWnds == null || mMain.FirstLayer_ChildWnds.Count == 0)
                    return new StdResult_Status(StdResult.Fail, $"[{AppName}] 자식윈도 못찾음", "OnecallAct_MainWnd/InitAsync_03");

                mMain.WndInfo_MdiClient = mMain.FirstLayer_ChildWnds.FirstOrDefault(x => x.className.ToUpper().Contains("MDICLIENT"));
                if (mMain.WndInfo_MdiClient == null)
                    return new StdResult_Status(StdResult.Fail, $"[{AppName}] MdiClient 못찾음", "OnecallAct_MainWnd/InitAsync_04");

                return new StdResult_Status(StdResult.Success);
            }
            finally
            {
                // 초기화 완료 후 TopMost 해제
                Std32Window.SetWindowTopMost(mMain.TopWnd_hWnd, false);
            }
        }
        catch (Exception ex)
        {
            return new StdResult_Status(StdResult.Fail, $"[{AppName}] MainWnd 예외: {ex.Message}", "OnecallAct_MainWnd/InitAsync_99");
        }
    }
    #endregion

    #region Private Methods
    // 메인 윈도우 이동 및 최대화 (안정성 중시)
    private async Task<StdResult_Status> MoveAndMaximizeMainWindowAsync()
    {
        try
        {
            // 1. Restore를 먼저 확실히 수행
            StdWin32.ShowWindow(mMain.TopWnd_hWnd, (int)StdCommon32.SW_RESTORE);
            await Task.Delay(100); // 0.1초 대기 (최대화 실패 방지 핵심)

            // 2. 목표 모니터로 이동
            StdWin32.MoveWindow(mMain.TopWnd_hWnd,
                s_Screens.m_WorkingMonitor.PositionX, s_Screens.m_WorkingMonitor.PositionY,
                1100, 800, true);
            await Task.Delay(50);

            // 3. 최대화 명령
            StdWin32.ShowWindow(mMain.TopWnd_hWnd, (int)StdCommon32.SW_MAXIMIZE);

            // 4. 최대화 안착 확인 (보다 넉넉하게 감시)
            bool bSuccess = false;
            for (int i = 0; i < 20; i++)
            {
                if (Std32Window.IsWindow(mMain.TopWnd_hWnd) && Std32Window.IsWindowVisible(mMain.TopWnd_hWnd))
                {
                    Std32Window.SetForegroundWindow(mMain.TopWnd_hWnd);
                    bSuccess = true;
                    break;
                }
                await Task.Delay(100);
            }

            if (!bSuccess) return new StdResult_Status(StdResult.Fail, "최대화 안착 감지 실패", "OnecallAct_MainWnd/Move_01");

            // 5. 스플래시 최종 유배 (-20000)
            if (Std32Window.IsWindow(mSplash.TopWnd_hWnd))
            {
                StdWin32.ShowWindow(mSplash.TopWnd_hWnd, (int)StdCommon32.SW_HIDE);
                StdWin32.MoveWindow(mSplash.TopWnd_hWnd, -20000, -20000, 100, 100, true);
            }

            return new StdResult_Status(StdResult.Success);
        }
        catch (Exception ex)
        {
            return new StdResult_Status(StdResult.Fail, $"이동 예외: {ex.Message}", "OnecallAct_MainWnd/Move_99");
        }
    }
    #endregion
}
#nullable restore
