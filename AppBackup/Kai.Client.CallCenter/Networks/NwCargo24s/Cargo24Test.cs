using System.Diagnostics;
using Kai.Common.StdDll_Common;

namespace Kai.Client.CallCenter.Networks.NwCargo24s;

#nullable disable

/// <summary>
/// 화물24시 테스트용 클래스 - UpdaterWorkAsync 테스트
/// </summary>
public class Cargo24Test
{
    /// <summary>
    /// UpdaterWorkAsync 테스트
    /// </summary>
    public static async Task<StdResult_Error> TestUpdaterWorkAsync(string sExePath, bool bEdit = true, bool bWrite = true, bool bMsgBox = true)
    {
        try
        {
            Debug.WriteLine("[Cargo24Test] UpdaterWorkAsync 테스트 시작");
            Debug.WriteLine($"[Cargo24Test] 실행 경로: {sExePath}");

            // 1. Context 생성
            var context = new Cargo24Context("CARGO24", "testid", "testpw");
            Debug.WriteLine("[Cargo24Test] Context 생성 완료");

            // 2. AppAct 생성
            context.AppAct = new Cargo24sAct_App(context);
            Debug.WriteLine("[Cargo24Test] AppAct 생성 완료");

            // 3. UpdaterWorkAsync 호출
            Debug.WriteLine("[Cargo24Test] UpdaterWorkAsync 호출...");
            var result = await context.AppAct.UpdaterWorkAsync(sExePath, bEdit, bWrite, bMsgBox);

            if (result != null)
            {
                Debug.WriteLine($"[Cargo24Test] 실패: {result.sErr}");
                return result;
            }
            else
            {
                Debug.WriteLine("[Cargo24Test] 성공: Splash 윈도우 찾음");
                Debug.WriteLine($"[Cargo24Test] Splash hWnd: {context.MemInfo.Splash.TopWnd_hWnd}");
                Debug.WriteLine($"[Cargo24Test] ProcessId: {context.MemInfo.Splash.TopWnd_uProcessId}");
                Debug.WriteLine($"[Cargo24Test] ThreadId: {context.MemInfo.Splash.TopWnd_uThreadId}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Cargo24Test] 예외 발생: {ex.Message}");
            return new StdResult_Error { sErr = $"예외 발생: {ex.Message}", sPos = "Cargo24Test/TestUpdaterWorkAsync" };
        }
    }

    /// <summary>
    /// 전체 초기화 테스트 (UpdaterWork + SplashWork)
    /// </summary>
    public static async Task<StdResult_Error> TestFullInitAsync(string sExePath, string sId, string sPw, bool bEdit = true, bool bWrite = true, bool bMsgBox = true)
    {
        try
        {
            Debug.WriteLine("[Cargo24Test] 전체 초기화 테스트 시작");

            // 1. Context 생성
            var context = new Cargo24Context("CARGO24", sId, sPw);
            context.AppAct = new Cargo24sAct_App(context);
            Debug.WriteLine("[Cargo24Test] Context, AppAct 생성 완료");

            // 2. UpdaterWorkAsync
            Debug.WriteLine("[Cargo24Test] UpdaterWorkAsync 호출...");
            var result = await context.AppAct.UpdaterWorkAsync(sExePath, bEdit, bWrite, bMsgBox);
            if (result != null)
            {
                Debug.WriteLine($"[Cargo24Test] UpdaterWorkAsync 실패: {result.sErr}");
                return result;
            }
            Debug.WriteLine("[Cargo24Test] UpdaterWorkAsync 성공");

            // 3. SplashWorkAsync
            Debug.WriteLine("[Cargo24Test] SplashWorkAsync 호출...");
            result = await context.AppAct.SplashWorkAsync(bEdit, bWrite, bMsgBox);
            if (result != null)
            {
                Debug.WriteLine($"[Cargo24Test] SplashWorkAsync 실패: {result.sErr}");
                return result;
            }
            Debug.WriteLine("[Cargo24Test] SplashWorkAsync 성공");
            Debug.WriteLine("[Cargo24Test] 전체 초기화 테스트 완료");

            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Cargo24Test] 예외 발생: {ex.Message}");
            return new StdResult_Error { sErr = $"예외 발생: {ex.Message}", sPos = "Cargo24Test/TestFullInitAsync" };
        }
    }
}

#nullable restore
