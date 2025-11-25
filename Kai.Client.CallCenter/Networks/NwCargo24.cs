using System.Diagnostics;

using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;
using Kai.Client.CallCenter.Networks.NwCargo24s;
using Kai.Client.CallCenter.Classes;
using Kai.Client.CallCenter.Classes.Class_Master;
using static Kai.Client.CallCenter.Classes.CommonVars;

namespace Kai.Client.CallCenter.Networks;
#nullable disable

/// <summary>
/// 화물24시 앱 (IExternalApp 구현)
/// </summary>
public class NwCargo24 : IExternalApp
{
    #region Static Configuration (appsettings.json에서 로드)
    public static bool s_Use { get; set; } = false;
    public static string s_Id { get; set; } = string.Empty;
    public static string s_Pw { get; set; } = string.Empty;
    public static string s_AppPath { get; set; } = string.Empty;
    #endregion

    #region Private Fields
    private Cargo24Context m_Context;
    #endregion

    #region IExternalApp 구현
    public bool IsUsed => s_Use;
    public string AppName => StdConst_Network.CARGO24;

    public async Task<StdResult_Status> InitializeAsync()
    {
        try
        {
            Debug.WriteLine($"[{AppName}] InitializeAsync 시작: Id={s_Id}");

            // 1. UpdaterWorkAsync - 앱 실행 및 Splash 윈도우 찾기
            Debug.WriteLine($"[{AppName}] UpdaterWorkAsync 호출...");
            var resultErr = await m_Context.AppAct.UpdaterWorkAsync(s_AppPath, bEdit: true, bWrite: true, bMsgBox: true);
            if (resultErr != null)
            {
                Debug.WriteLine($"[{AppName}] UpdaterWorkAsync 실패: {resultErr.sErrNPos}");
                return new StdResult_Status(StdResult.Fail, resultErr.sErrNPos, $"{AppName}/InitializeAsync_01");
            }
            Debug.WriteLine($"[{AppName}] UpdaterWorkAsync 성공");

            // 2. SplashWorkAsync - 로그인 처리
            Debug.WriteLine($"[{AppName}] SplashWorkAsync 호출...");
            resultErr = await m_Context.AppAct.SplashWorkAsync(bEdit: true, bWrite: true, bMsgBox: true);
            if (resultErr != null)
            {
                Debug.WriteLine($"[{AppName}] SplashWorkAsync 실패: {resultErr.sErrNPos}");
                return new StdResult_Status(StdResult.Fail, resultErr.sErrNPos, $"{AppName}/InitializeAsync_02");
            }
            Debug.WriteLine($"[{AppName}] SplashWorkAsync 성공");

            // 3. MainWndAct 생성
            m_Context.MainWndAct = new Cargo24sAct_MainWnd(m_Context);
            Debug.WriteLine($"[{AppName}] MainWndAct 생성 완료");

            // 4. InitializeAsync - 메인 윈도우 초기화 (찾기 + 이동 + 최대화 + 자식 윈도우)
            Debug.WriteLine($"[{AppName}] InitializeAsync 호출...");
            resultErr = await m_Context.MainWndAct.InitializeAsync(bEdit: true, bWrite: true, bMsgBox: true);
            if (resultErr != null)
            {
                Debug.WriteLine($"[{AppName}] InitializeAsync 실패: {resultErr.sErrNPos}");
                return new StdResult_Status(StdResult.Fail, resultErr.sErrNPos, $"{AppName}/InitializeAsync_03");
            }
            Debug.WriteLine($"[{AppName}] InitializeAsync 성공");

            // 5. RcptRegPageAct 생성 및 초기화
            m_Context.RcptRegPageAct = new Cargo24sAct_RcptRegPage(m_Context);
            Debug.WriteLine($"[{AppName}] RcptRegPageAct 생성 완료");

            resultErr = await m_Context.RcptRegPageAct.InitializeAsync(bEdit: true, bWrite: true, bMsgBox: true);
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
        try
        {
            Debug.WriteLine($"\n-----------------[{AppName}] AutoAllocAsync 시작 - Count={lAllocCount}--------------------------");

            // Cancel/Pause 체크 - Region 2 진입 전
            await ctrl.WaitIfPausedOrCancelledAsync();

            #region 1. 사전작업
            // TopMost 설정 - 화물24시 메인 창을 최상위로
            await Std32Window.SetWindowTopMostAndReleaseAsync(m_Context.MemInfo.Main.TopWnd_hWnd, CommonVars.c_nWaitShort);
            Debug.WriteLine($"[{AppName}] TopMost 설정 완료");
            #endregion

            // TODO: 나머지 자동배차 로직 구현
            await Task.Delay(1000); // 임시

            return new StdResult_Status(StdResult.Success, string.Empty, $"{AppName}/AutoAllocAsync");
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine($"[{AppName}] AutoAllocAsync 취소됨");
            return new StdResult_Status(StdResult.Skip, "작업 취소됨", $"{AppName}/AutoAllocAsync_Cancel");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{AppName}] AutoAllocAsync 예외 발생: {ex.Message}");
            return new StdResult_Status(StdResult.Fail, $"예외 발생: {ex.Message}", $"{AppName}/AutoAllocAsync_999");
        }
    }

    public void Shutdown()
    {
        try
        {
            Debug.WriteLine($"[{AppName}] Shutdown 시작");

            // AppAct.CloseAsync() 호출 - Cargo24 앱 종료
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
    /// 화물24시 생성자
    /// </summary>
    public NwCargo24()
    {
        Debug.WriteLine($"[{AppName}] 생성자 호출");
        m_Context = new Cargo24Context(StdConst_Network.CARGO24, s_Id, s_Pw);
        m_Context.AppAct = new Cargo24sAct_App(m_Context);
    }
    #endregion

    #region Test Methods (개발/디버깅용)
    /// <summary>
    /// UpdaterWorkAsync 테스트
    /// </summary>
    public async Task<StdResult_Error> TestUpdaterWorkAsync(bool bEdit = true, bool bWrite = true, bool bMsgBox = true)
    {
        Debug.WriteLine($"[{AppName}] TestUpdaterWorkAsync 시작");
        return await Cargo24Test.TestUpdaterWorkAsync(s_AppPath, bEdit, bWrite, bMsgBox);
    }

    /// <summary>
    /// 전체 초기화 테스트 (UpdaterWork + SplashWork)
    /// </summary>
    public async Task<StdResult_Error> TestFullInitAsync(bool bEdit = true, bool bWrite = true, bool bMsgBox = true)
    {
        Debug.WriteLine($"[{AppName}] TestFullInitAsync 시작");
        return await Cargo24Test.TestFullInitAsync(s_AppPath, s_Id, s_Pw, bEdit, bWrite, bMsgBox);
    }
    #endregion
}
#nullable restore
