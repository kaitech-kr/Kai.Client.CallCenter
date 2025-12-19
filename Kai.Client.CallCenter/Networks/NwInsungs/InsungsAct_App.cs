using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Draw = System.Drawing;

using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;
using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgWnd;

using Kai.Client.CallCenter.Classes;
using static Kai.Client.CallCenter.Classes.CommonVars;

namespace Kai.Client.CallCenter.Networks.NwInsungs;
#nullable disable

// 인성 앱 실행 및 스플래시 처리 담당 클래스 (Context 패턴 사용)
public class InsungsAct_App
{
    #region Context Reference
    // Context에 대한 읽기 전용 참조
    private readonly InsungContext m_Context;

    // 편의를 위한 로컬 참조들
    private InsungsInfo_File m_FileInfo => m_Context.FileInfo;
    private InsungsInfo_Mem m_MemInfo => m_Context.MemInfo;
    #endregion

    #region Constructor
    // 생성자 - Context를 받아서 초기화
    public InsungsAct_App(InsungContext context)
    {
        m_Context = context ?? throw new ArgumentNullException(nameof(context));
    }
    #endregion

    #region BeforeWork
    // 앱 실행 전 준비 작업 (Registry 경로 검색 및 기존 인성 프로세스 정리)
    public async Task<StdResult_String> BeforeWorkAsync(string sRegNameOfAppPath, string sFolder, string sExecFile)
    {
        Debug.WriteLine($"[{m_Context.AppName}/BeforeWork] 앱 경로 검색 시작: {sExecFile}");

        // Registry 또는 표준 설치 경로에서 AppPath 획득
        StdResult_String result = await Task.Run(() => NwCommon.GetAppPath(sRegNameOfAppPath, sFolder, sExecFile));

        // 경로를 찾지 못한 경우 (저장소 검색 대신 에러 메시지 처리)
        if (string.IsNullOrEmpty(result?.strResult))
        {
            string errMsg = $"[{m_Context.AppName}/BeforeWork] 설치 경로를 찾을 수 없습니다. 프로그램을 수동으로 확인해 주세요.";
            Debug.WriteLine(errMsg);

            return new StdResult_String(errMsg, "InsungsAct_App/BeforeWork_PathNotFound");
        }

        // 기존에 열려있는 인성 메인 윈도우가 있다면 우아하게 종료 시도
        IntPtr hWndMain = Std32Window.FindMainWindow_Reduct(null, m_FileInfo.Main_TopWnd_sWndNameReduct);
        if (hWndMain != IntPtr.Zero)
        {
            Debug.WriteLine($"[{m_Context.AppName}/BeforeWork] 기존 인성 앱 감지 - 종료 메시지 전송");
            Std32Window.PostCloseTwiceWindow(hWndMain);
            await Task.Delay(c_nWaitLong); // 안정적인 종료를 위해 1초 대기
        }

        return result;
    }
    #endregion

    #region UpdaterWork
    // Updater 실행 및 종료 대기 (개선 버전)
    public async Task<StdResult_Error> UpdaterWorkAsync(string sPath, bool bEdit = true, bool bWrite = true, bool bMsgBox = true)
    {
        try
        {
            Debug.WriteLine($"[{m_Context.AppName}/UpdaterWork] 업데이터 실행 시도: {sPath}");

            // 1. 실행 파일 존재 확인
            if (!System.IO.File.Exists(sPath))
            {
                string err = $"업데이터 경로가 존재하지 않습니다: {sPath}";
                Debug.WriteLine($"[{m_Context.AppName}/UpdaterWork] {err}");
                return new StdResult_Error(err, "InsungsAct_App/UpdaterWorkAsync_NotFound");
            }

            ProcessStartInfo processInfo = new ProcessStartInfo
            {
                FileName = sPath,
                UseShellExecute = true,
                Verb = "open" // 관리자 권한 필요한 경우 대비
            };

            using (Process procExec = Process.Start(processInfo))
            {
                if (procExec == null)
                {
                    Debug.WriteLine($"[{m_Context.AppName}/UpdaterWork] 실행 실패: Process.Start 결과가 null입니다.");
                    return new StdResult_Error("업데이터 실행 실패(null)", "InsungsAct_App/UpdaterWorkAsync_ProcessNull");
                }

                // 전용 취소 토큰 (5분 타임아웃)
                using (var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5)))
                {
                    try
                    {
                        Debug.WriteLine($"[{m_Context.AppName}/UpdaterWork] 프로세스 종료 대기 시작...");
                        await procExec.WaitForExitAsync(cts.Token);
                        Debug.WriteLine($"[{m_Context.AppName}/UpdaterWork] 업데이터 정상 종료 확인");
                    }
                    catch (OperationCanceledException)
                    {
                        Debug.WriteLine($"[{m_Context.AppName}/UpdaterWork] 업데이터 종료 대기 타임아웃 (5분)");
                        return new StdResult_Error("업데이터가 5분 안에 종료되지 않았습니다.", "InsungsAct_App/UpdaterWorkAsync_Timeout");
                    }
                }
            }

            return null; // 성공
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{m_Context.AppName}/UpdaterWork] 예외 발생: {ex.Message}");
            return new StdResult_Error(ex.Message, "InsungsAct_App/UpdaterWorkAsync_Exception");
        }
    }
    #endregion

    #region SplashWork
    // 스플래시 창 처리 및 로그인 (스플래시 찾기 -> 중앙 이동 -> 로그인 -> 배경 팝업 처리)
    public async Task<StdResult_Error> SplashWorkAsync(bool bEdit = true, bool bWrite = true, bool bMsgBox = true)
    {
        IntPtr hWndTmp = IntPtr.Zero;
        Draw.Rectangle rcCur = StdUtil.s_rcDrawEmpty;

        try
        {
            Debug.WriteLine($"[{m_Context.AppName}/SplashWork] 시작 (ID: {m_Context.Id})");

            // 1. 스플래시 윈도우 찾기 (최대 10초)
            for (int i = 0; i < c_nRepeatVeryMany; i++)
            {
                m_Context.MemInfo.Splash.TopWnd_hWnd = StdWin32.FindWindow(null, m_Context.FileInfo.Splash_TopWnd_sWndName);
                if (m_Context.MemInfo.Splash.TopWnd_hWnd != IntPtr.Zero) break;
                await Task.Delay(c_nWaitNormal);
            }

            if (m_Context.MemInfo.Splash.TopWnd_hWnd == IntPtr.Zero)
            {
                return new StdResult_Error(
                    $"[{m_Context.AppName}/SplashWork] 스플래시 윈도우를 찾을 수 없습니다: {m_Context.FileInfo.Splash_TopWnd_sWndName}",
                    "InsungsAct_App/SplashWorkAsync_01");
            }

            // 2. 화면 중앙으로 이동 및 최상위 설정
            Std32Window.SetWindowTopMost(m_Context.MemInfo.Splash.TopWnd_hWnd, true);
            rcCur = Std32Window.GetWindowRect_DrawAbs(m_Context.MemInfo.Splash.TopWnd_hWnd);
            Draw.Rectangle rcNew = s_Screens.m_WorkingMonitor.GetCenterDrawRectangle(rcCur);
            StdWin32.MoveWindow(m_Context.MemInfo.Splash.TopWnd_hWnd, rcNew.X, rcNew.Y, rcNew.Width, rcNew.Height, true);
            await Task.Delay(500);

            // 3. 프로세스 정보 획득
            m_Context.MemInfo.Splash.TopWnd_uThreadId = Std32Window.GetWindowThreadProcessId(
                m_Context.MemInfo.Splash.TopWnd_hWnd, out m_Context.MemInfo.Splash.TopWnd_uProcessId);

            // 4. 입력창 핸들 획득
            m_Context.MemInfo.Splash.IdWnd_hWnd = Std32Window.GetWndHandle_FromRelDrawPt(m_Context.MemInfo.Splash.TopWnd_hWnd, m_Context.FileInfo.Splash_IdWnd_ptChk);
            m_Context.MemInfo.Splash.PwWnd_hWnd = Std32Window.GetWndHandle_FromRelDrawPt(m_Context.MemInfo.Splash.TopWnd_hWnd, m_Context.FileInfo.Splash_PwWnd_ptChk);

            if (m_Context.MemInfo.Splash.IdWnd_hWnd == IntPtr.Zero || m_Context.MemInfo.Splash.PwWnd_hWnd == IntPtr.Zero)
            {
                return new StdResult_Error(
                    $"[{m_Context.AppName}/SplashWork] ID/PW 입력창 핸들 획득 실패", "InsungsAct_App/SplashWorkAsync_04");
            }

            // 5. 로그인 처리 (try-finally로 확실한 입력 보호 해제)
            bool bBlocked = false;
            try
            {
                bBlocked = StdWin32.BlockInput(true);
                
                string curId = Std32Window.GetWindowCaption(m_Context.MemInfo.Splash.IdWnd_hWnd);
                if (curId != m_Context.Id) Std32Window.SetWindowCaption(m_Context.MemInfo.Splash.IdWnd_hWnd, m_Context.Id);
                
                Std32Window.SetWindowCaption(m_Context.MemInfo.Splash.PwWnd_hWnd, m_Context.Pw);
                await Std32Key_Msg.KeyPost_DownAsync(m_Context.MemInfo.Splash.PwWnd_hWnd, StdCommon32.VK_RETURN);
            }
            finally
            {
                if (bBlocked) StdWin32.BlockInput(false);
            }

            Debug.WriteLine($"[{m_Context.AppName}/SplashWork] 로그인 입력 완료, 백업본 팝업 처리 로직 시작");

            // 6. 팝업 다이얼로그 처리 (백업본의 복잡한 로직 전체 포함)
            _ = Task.Run(async () =>
            {
                uint uProcId = m_Context.MemInfo.Splash.TopWnd_uProcessId;
                IntPtr hWndBtn = IntPtr.Zero;

                for (int i = 0; i < c_nRepeatVeryMany; i++) // 10초 동안 감시
                {
                    await Task.Delay(c_nWaitNormal);

                    // A. MapSDK/확인창 처리 (#32770 클래스 기반)
                    IntPtr hWndDlg = Std32Window.FindMainWindow(uProcId, "#32770", "");
                    if (hWndDlg != IntPtr.Zero)
                    {
                        // "확인" 버튼 찾기
                        hWndBtn = Std32Window.FindWindowEx(hWndDlg, IntPtr.Zero, "Button", "확인");
                        if (hWndBtn != IntPtr.Zero)
                        {
                            await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndBtn);
                            StdWin32.SendMessage(hWndDlg, StdCommon32.WM_CLOSE, 0, 0);
                        }
                    }

                    // B. "확인" 제목의 팝업 처리
                    IntPtr hWndConfirm = Std32Window.FindMainWindow(uProcId, "#32770", "확인");
                    if (hWndConfirm != IntPtr.Zero)
                    {
                        hWndBtn = StdWin32.FindWindowEx(hWndConfirm, IntPtr.Zero, "Button", "확인");
                        if (hWndBtn != IntPtr.Zero)
                        {
                            await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndBtn);

                            // C. 비밀번호변경 연쇄 팝업 처리 (내부 감시 시작)
                            _ = Task.Run(async () =>
                            {
                                for (int j = 0; j < 15; j++) // 1.5초 동안
                                {
                                    await Task.Delay(c_nWaitNormal);
                                    IntPtr hWndPwChange = StdWin32.FindWindow(null, "비밀번호변경");
                                    if (hWndPwChange != IntPtr.Zero)
                                    {
                                        IntPtr hWndLaterBtn = StdWin32.FindWindowEx(hWndPwChange, IntPtr.Zero, null, "나중에 변경");
                                        if (hWndLaterBtn != IntPtr.Zero)
                                        {
                                            await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndLaterBtn);
                                            // 최종 확인창까지 정리
                                            await Task.Delay(c_nWaitNormal);
                                            IntPtr hWndFinal = Std32Window.FindMainWindow(uProcId, "#32770", "확인");
                                            if (hWndFinal != IntPtr.Zero)
                                            {
                                                IntPtr hWndFinalBtn = StdWin32.FindWindowEx(hWndFinal, IntPtr.Zero, "Button", "확인");
                                                if (hWndFinalBtn != IntPtr.Zero) await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndFinalBtn);
                                            }
                                            break;
                                        }
                                    }
                                }
                            });
                        }
                    }

                    // D. 특정 공지/안내 팝업 닫기
                    IntPtr hWndNotice = StdWin32.FindWindow(null, "오토바이 신규기사 범죄이력 조회 업데이트 안내");
                    if (hWndNotice != IntPtr.Zero)
                    {
                        StdWin32.SendMessage(hWndNotice, StdCommon32.WM_SYSCOMMAND, StdCommon32.SC_CLOSE, 0);
                    }
                }
            });

            return null;
        }
        catch (Exception ex)
        {
            return new StdResult_Error($"[{m_Context.AppName}/SplashWork] 예외 발생: {ex.Message}", "InsungsAct_App/SplashWorkAsync_999");
        }
    }
    #endregion

    #region Close
    // 인성 앱 종료 - MainWindow 닫기 시도 후 최종적으로 프로세스 정리
    public StdResult_Error Close(int nDelayMiliSec = 100)
    {
        try
        {
            Debug.WriteLine($"[{m_Context.AppName}/Close] 종료 시퀀스 시작");

            // 1. 메인 윈도우 종료 시도
            if (m_Context.MemInfo.Main.TopWnd_hWnd != IntPtr.Zero && StdWin32.IsWindowVisible(m_Context.MemInfo.Main.TopWnd_hWnd))
            {
                Debug.WriteLine($"[{m_Context.AppName}/Close] 메인 윈도우 종료 메시지 전송");
                StdWin32.PostMessage(m_Context.MemInfo.Main.TopWnd_hWnd, StdCommon32.WM_SYSCOMMAND, Std32Window.SC_CLOSE, 0);
            }

            System.Threading.Thread.Sleep(nDelayMiliSec);

            // 2. 스플래시 윈도우 정리
            if (m_Context.MemInfo.Splash.TopWnd_hWnd != IntPtr.Zero)
            {
                Debug.WriteLine($"[{m_Context.AppName}/Close] 스플래시 윈도우 정리 전송");
                Std32Window.PostCloseTwiceWindow(m_Context.MemInfo.Splash.TopWnd_hWnd);

                // 3. 확실한 종료 확인 및 최후의 수단(Kill) 실행
                bool bExists = true;
                for (int i = 0; i < c_nRepeatMany; i++) // 5초 감시 (c_nRepeatMany=50, c_nWaitNormal=100)
                {
                    bExists = StdWin32.IsWindow(m_Context.MemInfo.Splash.TopWnd_hWnd);
                    if (!bExists) break;
                    System.Threading.Thread.Sleep(c_nWaitNormal);
                }

                if (bExists)
                {
                    // 정상 종료 실패 시 프로세스 Kill 실행 (확장자 제외한 파일명 전달)
                    string processName = System.IO.Path.GetFileNameWithoutExtension(m_Context.AppPath);
                    Debug.WriteLine($"[{m_Context.AppName}/Close] 정상 종료 실패. 프로세스 강제 종료 시도: {processName}");
                    
                    // Kai.Common.StdDll_Common.StdProcess.Kill 사용
                    Kai.Common.StdDll_Common.StdProcess.Kill(processName);
                }
            }

            return null; // 성공
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{m_Context.AppName}/Close] 예외 발생: {ex.Message}");
            return new StdResult_Error(ex.Message, "InsungsAct_App/Close_Exception");
        }
    }
    #endregion
}
#nullable enable
