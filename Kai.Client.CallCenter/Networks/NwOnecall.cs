using System.Diagnostics;

using Kai.Common.StdDll_Common;
using Kai.Client.CallCenter.Networks.NwOnecalls;
using Kai.Client.CallCenter.Classes;
using Kai.Client.CallCenter.Classes.Class_Master;

namespace Kai.Client.CallCenter.Networks;
#nullable disable

/// <summary>
/// 원콜 앱 (IExternalApp 구현)
/// </summary>
public class NwOnecall : IExternalApp
{
    #region Static Configuration (appsettings.json에서 로드)
    public static bool s_Use { get; set; } = false;
    public static string s_Id { get; set; } = string.Empty;
    public static string s_Pw { get; set; } = string.Empty;
    public static string s_AppPath { get; set; } = string.Empty;
    #endregion

    #region Private Fields
    private OnecallContext m_Context;
    #endregion

    #region IExternalApp 구현
    public bool IsUsed => s_Use;
    public string AppName => StdConst_Network.ONECALL;

    public async Task<StdResult_Status> InitializeAsync()
    {
        try
        {
            Debug.WriteLine($"[{AppName}] InitializeAsync 시작: Id={s_Id}");

            // 1. UpdaterWorkAsync - 앱 실행 및 Splash 윈도우 찾기
            Debug.WriteLine($"[{AppName}] UpdaterWorkAsync 호출...");
            var resultErr = await m_Context.AppAct.UpdaterWorkAsync(s_AppPath);
            if (resultErr != null)
            {
                Debug.WriteLine($"[{AppName}] UpdaterWorkAsync 실패: {resultErr.sErrNPos}");
                return new StdResult_Status(StdResult.Fail, resultErr.sErrNPos, $"{AppName}/InitializeAsync_01");
            }
            Debug.WriteLine($"[{AppName}] UpdaterWorkAsync 성공");

            // 2. SplashWorkAsync - 로그인 처리
            Debug.WriteLine($"[{AppName}] SplashWorkAsync 호출...");
            resultErr = await m_Context.AppAct.SplashWorkAsync(AppName, s_Id, s_Pw);
            if (resultErr != null)
            {
                Debug.WriteLine($"[{AppName}] SplashWorkAsync 실패: {resultErr.sErrNPos}");
                return new StdResult_Status(StdResult.Fail, resultErr.sErrNPos, $"{AppName}/InitializeAsync_02");
            }
            Debug.WriteLine($"[{AppName}] SplashWorkAsync 성공");

            // 3. MainWndAct 생성
            m_Context.MainWndAct = new OnecallAct_MainWnd(m_Context);
            Debug.WriteLine($"[{AppName}] MainWndAct 생성 완료");

            // 4. InitAsync - 메인 윈도우 초기화 (찾기 + 이동 + 최대화 + 자식 윈도우)
            Debug.WriteLine($"[{AppName}] MainWnd InitAsync 호출...");
            resultErr = await m_Context.MainWndAct.InitAsync();
            if (resultErr != null)
            {
                Debug.WriteLine($"[{AppName}] MainWnd InitAsync 실패: {resultErr.sErrNPos}");
                return new StdResult_Status(StdResult.Fail, resultErr.sErrNPos, $"{AppName}/InitializeAsync_03");
            }
            Debug.WriteLine($"[{AppName}] MainWnd InitAsync 성공");

            // 5. RcptRegPageAct 생성 및 초기화
            m_Context.RcptRegPageAct = new OnecallAct_RcptRegPage(m_Context);
            Debug.WriteLine($"[{AppName}] RcptRegPageAct 생성 완료");

            Debug.WriteLine($"[{AppName}] RcptRegPage InitializeAsync 호출...");
            resultErr = await m_Context.RcptRegPageAct.InitializeAsync();
            if (resultErr != null)
            {
                Debug.WriteLine($"[{AppName}] RcptRegPage InitializeAsync 실패: {resultErr.sErrNPos}");
                return new StdResult_Status(StdResult.Fail, resultErr.sErrNPos, $"{AppName}/InitializeAsync_04");
            }
            Debug.WriteLine($"[{AppName}] RcptRegPage InitializeAsync 성공");

            Debug.WriteLine($"[{AppName}] InitializeAsync 완료");
            return new StdResult_Status(StdResult.Success, string.Empty, $"{AppName}/InitializeAsync");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{AppName}] InitializeAsync 예외 발생: {ex.Message}");
            return new StdResult_Status(StdResult.Fail, $"예외 발생: {ex.Message}", $"{AppName}/InitializeAsync_999");
        }
    }

    public async Task<StdResult_Status> AutoAllocAsync(long lAllocCount, CancelTokenControl ctrl)
    {
        // TODO: 원콜 자동배차 로직 구현
        Debug.WriteLine($"[{AppName}] AutoAllocAsync 호출됨 (미구현): Count={lAllocCount}");
        await Task.Delay(100); // 임시
        return new StdResult_Status(StdResult.Success, string.Empty, $"{AppName}/AutoAllocAsync");
    }

    public void Shutdown()
    {
        try
        {
            Debug.WriteLine($"[{AppName}] Shutdown 시작");

            // AppAct.Close() 호출 - Onecall 앱 종료
            if (m_Context?.AppAct != null)
            {
                StdResult_Error resultClose = m_Context.AppAct.Close();
                if (resultClose != null)
                {
                    Debug.WriteLine($"[{AppName}] Close 실패: {resultClose.sErrNPos}");
                }
                else
                {
                    Debug.WriteLine($"[{AppName}] Close 성공");
                }
            }

            Debug.WriteLine($"[{AppName}] Shutdown 완료");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{AppName}] Shutdown 예외 발생: {ex.Message}");
        }
    }
    #endregion

    #region Dispose
    private bool disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                Debug.WriteLine($"[{AppName}] Dispose 호출");
                // 관리형 리소스 해제
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    #endregion

    #region 생성자
    /// <summary>
    /// 원콜 생성자
    /// </summary>
    public NwOnecall()
    {
        Debug.WriteLine($"[{AppName}] 생성자 호출");
        m_Context = new OnecallContext(StdConst_Network.ONECALL, s_Id, s_Pw);
        m_Context.AppAct = new OnecallAct_App(m_Context);
    }
    #endregion
}
#nullable restore
