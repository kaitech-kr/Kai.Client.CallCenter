using System.IO;
using Newtonsoft.Json;
using System.Diagnostics;

using Kai.Common.StdDll_Common;
using Kai.Common.NetDll_WpfCtrl.NetWnds;

using Kai.Client.CallCenter.Class_Common;
using Kai.Client.CallCenter.Networks.NwInsungs;
using static Kai.Client.CallCenter.Class_Common.CommonVars;

namespace Kai.Client.CallCenter.Networks;
#nullable disable
public class NwInsung01 : IExternalApp
{
    #region Static Configuration (appsettings.json에서 로드)
    public static bool s_Use { get; set; } = false;
    public static string s_Id { get; set; } = string.Empty;
    public static string s_Pw { get; set; } = string.Empty;
    public static string s_AppPath { get; set; } = string.Empty;
    #endregion

    #region Constants
    public const string APP_NAME = StdConst_Network.INSUNG1;
    public const string INFO_FILE_NAME = "Insung01_FileInfo.txt";
    #endregion

    #region Context
    /// <summary>
    /// 인성1의 모든 공용 데이터를 담는 Context
    /// </summary>
    private InsungContext m_Context = null;

    /// <summary>
    /// Context 읽기 전용 접근
    /// </summary>
    public InsungContext Context => m_Context;
    #endregion

    #region Dispose
    private bool disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
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

    #region IExternalApp 구현
    public bool IsUsed => s_Use;
    public string AppName => "Insung01";

    public async Task<StdResult_Status> InitializeAsync()
    {
        try
        {
            Debug.WriteLine($"[NwInsung01] InitializeAsync 시작: Id={s_Id}");

            // 1. FileInfo 파일에서 설정 로드
            StdResult_Error resultErr = await ReadInfoFileAsync();
            if (resultErr != null)
            {
                Debug.WriteLine($"[NwInsung01] FileInfo 로드 실패: {resultErr.sErrNPos}");
                return new StdResult_Status(StdResult.Fail, resultErr.sErrNPos, "NwInsung01/InitializeAsync_01");
            }

            // FileInfo 로드 확인 (테스트용)
            Debug.WriteLine($"[NwInsung01] ===== FileInfo 로드 확인 =====");
            Debug.WriteLine($"  App_sPredictFolder: {m_Context.FileInfo.App_sPredictFolder}");
            Debug.WriteLine($"  App_sExeFileName: {m_Context.FileInfo.App_sExeFileName}");
            Debug.WriteLine($"  Splash_TopWnd_sWndName: {m_Context.FileInfo.Splash_TopWnd_sWndName}");
            Debug.WriteLine($"  Splash_IdWnd_ptChk: {m_Context.FileInfo.Splash_IdWnd_ptChk}");
            Debug.WriteLine($"  Main_TopWnd_sWndNameReduct: {m_Context.FileInfo.Main_TopWnd_sWndNameReduct}");
            Debug.WriteLine($"[NwInsung01] ================================");

            // 2. 앱 경로 확인
            if (string.IsNullOrEmpty(s_AppPath))
            {
                Debug.WriteLine($"[NwInsung01] AppPath가 설정되지 않았습니다.");
                return new StdResult_Status(StdResult.Fail, "AppPath가 appsettings.json에 설정되지 않았습니다.", "NwInsung01/InitializeAsync_02");
            }
            Debug.WriteLine($"[NwInsung01] 앱 경로: {s_AppPath}");

            // Show Loding
            if (s_Screens.m_WorkingMonitor != s_Screens.m_PrimaryMonitor) // 작업 모니터가 기본 모니터면 LoadingPanel을 사용하지 않는다.
                NetLoadingWnd.ShowLoading(s_MainWnd, "   인성1 초기화 작업중입니다, \n     입력작업을 하지 마세요...   ");

            // 3. UpdaterWork - Updater 실행 및 종료 대기
            StdResult_Error resultUpdater = await m_Context.AppAct.UpdaterWorkAsync(s_AppPath);
            if (resultUpdater != null)
            {
                Debug.WriteLine($"[NwInsung01] UpdaterWork 실패: {resultUpdater.sErrNPos}");
                return new StdResult_Status(StdResult.Fail, resultUpdater.sErrNPos, "NwInsung01/InitializeAsync_03");
            }
            Debug.WriteLine($"[NwInsung01] Updater 종료 완료");

            // 4. SplashWork - 스플래시 창 처리 및 로그인
            StdResult_Error resultSplash = await m_Context.AppAct.SplashWorkAsync();
            if (resultSplash != null)
            {
                Debug.WriteLine($"[NwInsung01] SplashWork 실패: {resultSplash.sErrNPos}");
                return new StdResult_Status(StdResult.Fail, resultSplash.sErrNPos, "NwInsung01/InitializeAsync_04");
            }
            Debug.WriteLine($"[NwInsung01] 스플래시 로그인 완료");

            // 5. MainWnd 초기화
            StdResult_Error resultMainWnd = await m_Context.MainWndAct.InitializeAsync();
            if (resultMainWnd != null)
            {
                Debug.WriteLine($"[NwInsung01] MainWnd 초기화 실패: {resultMainWnd.sErrNPos}");
                return new StdResult_Status(StdResult.Fail, resultMainWnd.sErrNPos, "NwInsung01/InitializeAsync_05");
            }
            Debug.WriteLine($"[NwInsung01] MainWnd 초기화 완료");

            // 6. RcptRegPage 초기화
            StdResult_Error resultRcptRegPage = await m_Context.RcptRegPageAct.InitializeAsync();
            if (resultRcptRegPage != null)
            {
                Debug.WriteLine($"[NwInsung01] RcptRegPage 초기화 실패: {resultRcptRegPage.sErrNPos}");
                return new StdResult_Status(StdResult.Fail, resultRcptRegPage.sErrNPos, "NwInsung01/InitializeAsync_06");
            }
            Debug.WriteLine($"[NwInsung01] RcptRegPage 초기화 완료");

            Debug.WriteLine("[NwInsung01] InitializeAsync 완료");
            return new StdResult_Status(StdResult.Success);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NwInsung01] InitializeAsync 실패: {ex.Message}");
            return new StdResult_Status(StdResult.Fail, ex.Message, "NwInsung01/InitializeAsync_99");
        }
        finally
        {
            NetLoadingWnd.HideLoading();
        }
    }

    public void Shutdown()
    {
        try
        {
            Debug.WriteLine("[NwInsung01] Shutdown 시작");

            // AppAct.Close() 호출 - 인성 앱 종료
            if (m_Context?.AppAct != null)
            {
                StdResult_Error resultClose = m_Context.AppAct.Close();
                if (resultClose != null)
                {
                    Debug.WriteLine($"[NwInsung01] Close 실패: {resultClose.sErrNPos}");
                }
                else
                {
                    Debug.WriteLine($"[NwInsung01] Close 성공");
                }
            }

            Debug.WriteLine("[NwInsung01] Shutdown 완료");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NwInsung01] Shutdown 실패: {ex.Message}");
        }
    }
    #endregion

    #region Helper Methods
    /// <summary>
    /// FileInfo를 JSON 파일에서 읽어서 Context.FileInfo에 로드
    /// </summary>
    private async Task<StdResult_Error> ReadInfoFileAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                string sFilePath = Path.Combine(s_sDataDir, INFO_FILE_NAME);

                // 파일 존재 체크
                if (!File.Exists(sFilePath))
                {
                    return new StdResult_Error(
                        $"[{APP_NAME}] FileInfo 파일이 없습니다: {sFilePath}",
                        "NwInsung01/ReadInfoFileAsync_01");
                }

                // 파일 읽기
                string jsonContent;
                using (StreamReader reader = new StreamReader(sFilePath))
                {
                    jsonContent = reader.ReadToEnd();
                }

                // JSON 역직렬화
                InsungsInfo_File fileInfo = JsonConvert.DeserializeObject<InsungsInfo_File>(jsonContent);
                if (fileInfo == null)
                {
                    return new StdResult_Error(
                        $"[{APP_NAME}] FileInfo 파일 역직렬화 실패: {sFilePath}",
                        "NwInsung01/ReadInfoFileAsync_02");
                }

                // Context의 FileInfo에 덮어씌우기
                m_Context.FileInfo = fileInfo;

                Debug.WriteLine($"[NwInsung01] FileInfo 파일 로드 완료: {sFilePath}");
                return null; // 성공
            }
            catch (Exception ex)
            {
                return new StdResult_Error(
                    $"[{APP_NAME}] FileInfo 파일 읽기 예외: {ex.Message}",
                    "NwInsung01/ReadInfoFileAsync_99");
            }
        });
    }

    /// <summary>
    /// Context.FileInfo를 JSON 파일로 저장 (테스트/디버깅용)
    /// </summary>
    private void WriteInfoToFile_AtFirst()
    {
        try
        {
            // 이미 Context.FileInfo가 초기화되어 있으므로 그대로 사용
            InsungsInfo_File info = m_Context.FileInfo;

            // TODO: 필요시 기본값 설정 (현재는 InsungsInfo_File 생성자에서 설정됨)
            // 예: info.App_sPredictFolder = @"C:\Program Files (x86)\INSUNGDATA\인성퀵화물통합솔루션_KN";

            // JSON 직렬화
            string json = JsonConvert.SerializeObject(info, Formatting.Indented);
            string sFilePath = Path.Combine(s_sDataDir, INFO_FILE_NAME);

            // Data 폴더 생성 (없을 경우)
            string dataDir = Path.GetDirectoryName(sFilePath);
            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }

            // 파일 저장
            using (StreamWriter writer = new StreamWriter(sFilePath))
            {
                writer.Write(json);
            }

            Debug.WriteLine($"[NwInsung01] FileInfo 파일 저장 완료: {sFilePath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NwInsung01] FileInfo 파일 저장 실패: {ex.Message}");
        }
    }
    #endregion

    #region 생성자
    public NwInsung01()
    {
        Debug.WriteLine($"[NwInsung01] 생성자 호출: Id={s_Id}, Use={s_Use} --------------------------------------------------------");

        // Context 생성
        m_Context = new InsungContext(APP_NAME, s_Id, s_Pw);

        // AppAct 생성
        m_Context.AppAct = new InsungsAct_App(m_Context);

        // MainWndAct 생성
        m_Context.MainWndAct = new InsungsAct_MainWnd(m_Context);

        // RcptRegPageAct 생성
        m_Context.RcptRegPageAct = new InsungsAct_RcptRegPage(m_Context);

        Debug.WriteLine($"[NwInsung01] Context 생성 완료: AppName={m_Context.AppName}");
    }
    #endregion
}
#nullable restore
