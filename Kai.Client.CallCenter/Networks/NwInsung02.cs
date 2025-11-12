using System.IO;
using Newtonsoft.Json;
using System.Diagnostics;
using Draw = System.Drawing;
using System.Windows.Media;

using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;
using Kai.Common.NetDll_WpfCtrl.NetWnds;
using Kai.Common.NetDll_WpfCtrl.NetOFR;
using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgBox;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Services;

using Kai.Client.CallCenter.Classes;
using Kai.Client.CallCenter.Classes.Class_Master;
using Kai.Client.CallCenter.Networks.NwInsungs;
using Kai.Client.CallCenter.Windows;
using Kai.Client.CallCenter.OfrWorks;
using static Kai.Client.CallCenter.Classes.CommonVars;

namespace Kai.Client.CallCenter.Networks;
#nullable disable
public class NwInsung02 : IExternalApp
{
    #region Static Configuration (appsettings.json에서 로드)
    public static bool s_Use { get; set; } = false;
    public static string s_Id { get; set; } = string.Empty;
    public static string s_Pw { get; set; } = string.Empty;
    public static string s_AppPath { get; set; } = string.Empty;
    #endregion

    #region Constants
    public const string APP_NAME = StdConst_Network.INSUNG2;
    public const string INFO_FILE_NAME = "Insung02_FileInfo.txt";
    #endregion

    #region Context
    /// <summary>
    /// 인성2의 모든 공용 데이터를 담는 Context
    /// </summary>
    private InsungContext m_Context = null;

    /// <summary>
    /// Context 읽기 전용 접근
    /// </summary>
    public InsungContext Context => m_Context;
    #endregion

    #region AutoAlloc Variables
    /// <summary>
    /// 자동배차 할일 없음 카운터 (60회마다 조회버튼 클릭)
    /// </summary>
    private long m_lRestCount = 0;
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
    public string AppName => "Insung02";

    public async Task<StdResult_Status> InitializeAsync()
    {
        try
        {
            Debug.WriteLine($"[NwInsung02] InitializeAsync 시작: Id={s_Id}");

            // 1. FileInfo 파일에서 설정 로드
            StdResult_Error resultErr = await ReadInfoFileAsync();
            if (resultErr != null)
            {
                Debug.WriteLine($"[NwInsung02] FileInfo 로드 실패: {resultErr.sErrNPos}");
                return new StdResult_Status(StdResult.Fail, resultErr.sErrNPos, "NwInsung02/InitializeAsync_01");
            }

            // 2. 앱 경로 확인
            if (string.IsNullOrEmpty(s_AppPath))
            {
                Debug.WriteLine($"[NwInsung02] AppPath가 설정되지 않았습니다.");
                return new StdResult_Status(StdResult.Fail, "AppPath가 appsettings.json에 설정되지 않았습니다.", "NwInsung02/InitializeAsync_02");
            }
            //Debug.WriteLine($"[NwInsung02] 앱 경로: {s_AppPath}");

            // Show Loding
            if (s_Screens.m_WorkingMonitor != s_Screens.m_PrimaryMonitor) // 작업 모니터가 기본 모니터면 LoadingPanel을 사용하지 않는다.
                NetLoadingWnd.ShowLoading(s_MainWnd, "   인성2 초기화 작업중입니다, \n     입력작업을 하지 마세요...   ");

            // 3. UpdaterWork - Updater 실행 및 종료 대기
            StdResult_Error resultUpdater = await m_Context.AppAct.UpdaterWorkAsync(s_AppPath);
            if (resultUpdater != null)
            {
                Debug.WriteLine($"[NwInsung02] UpdaterWork 실패: {resultUpdater.sErrNPos}");
                return new StdResult_Status(StdResult.Fail, resultUpdater.sErrNPos, "NwInsung02/InitializeAsync_03");
            }
            //Debug.WriteLine($"[NwInsung02] Updater 종료 완료");

            // 4. SplashWork - 스플래시 창 처리 및 로그인
            StdResult_Error resultSplash = await m_Context.AppAct.SplashWorkAsync();
            if (resultSplash != null)
            {
                Debug.WriteLine($"[NwInsung02] SplashWork 실패: {resultSplash.sErrNPos}");
                return new StdResult_Status(StdResult.Fail, resultSplash.sErrNPos, "NwInsung02/InitializeAsync_04");
            }
            //Debug.WriteLine($"[NwInsung02] 스플래시 로그인 완료");

            // 5. MainWnd 초기화
            StdResult_Error resultMainWnd = await m_Context.MainWndAct.InitializeAsync();
            if (resultMainWnd != null)
            {
                Debug.WriteLine($"[NwInsung02] MainWnd 초기화 실패: {resultMainWnd.sErrNPos}");
                return new StdResult_Status(StdResult.Fail, resultMainWnd.sErrNPos, "NwInsung02/InitializeAsync_05");
            }
            //Debug.WriteLine($"[NwInsung02] MainWnd 초기화 완료");

            // 6. RcptRegPage 초기화
            StdResult_Error resultRcptRegPage = await m_Context.RcptRegPageAct.InitializeAsync();
            if (resultRcptRegPage != null)
            {
                Debug.WriteLine($"[NwInsung02] RcptRegPage 초기화 실패: {resultRcptRegPage.sErrNPos}");
                return new StdResult_Status(StdResult.Fail, resultRcptRegPage.sErrNPos, "NwInsung02/InitializeAsync_06");
            }
            //Debug.WriteLine($"[NwInsung02] RcptRegPage 초기화 완료");

            Debug.WriteLine("[NwInsung02] InitializeAsync 완료");
            return new StdResult_Status(StdResult.Success);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NwInsung02] InitializeAsync 실패: {ex.Message}");
            return new StdResult_Status(StdResult.Fail, ex.Message, "NwInsung02/InitializeAsync_99");
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
            Debug.WriteLine("[NwInsung02] Shutdown 시작");

            // AppAct.Close() 호출 - 인성 앱 종료
            if (m_Context?.AppAct != null)
            {
                StdResult_Error resultClose = m_Context.AppAct.Close();
                if (resultClose != null)
                {
                    Debug.WriteLine($"[NwInsung02] Close 실패: {resultClose.sErrNPos}");
                }
                else
                {
                    Debug.WriteLine($"[NwInsung02] Close 성공");
                }
            }

            Debug.WriteLine("[NwInsung02] Shutdown 완료");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NwInsung02] Shutdown 실패: {ex.Message}");
        }
    }

    public async Task<StdResult_Status> AutoAllocAsync(long lAllocCount, CancelTokenControl ctrl)
    {
        try
        {
            Debug.WriteLine($"[NwInsung02] AutoAllocAsync 시작 - Count={lAllocCount}");

            // TODO: 실제 자동배차 로직 구현
            // (Stub for now - will implement after build test)

            Debug.WriteLine($"[NwInsung02] AutoAllocAsync 완료 - Count={lAllocCount}");
            return new StdResult_Status(StdResult.Success);
        }
        catch (OperationCanceledException)
        {
            return new StdResult_Status(StdResult.Skip, "작업 취소됨", "NwInsung02/AutoAllocAsync_Cancel");
        }
        catch (Exception ex)
        {
            return new StdResult_Status(StdResult.Fail, ex.Message, "NwInsung02/AutoAllocAsync_999");
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
                        "NwInsung02/ReadInfoFileAsync_01");
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
                        "NwInsung02/ReadInfoFileAsync_02");
                }

                // Context의 FileInfo에 덮어씌우기
                m_Context.FileInfo = fileInfo;

                Debug.WriteLine($"[{APP_NAME}] FileInfo 파일 로드 완료: {sFilePath}");
                return null; // 성공
            }
            catch (Exception ex)
            {
                return new StdResult_Error(
                    $"[{APP_NAME}] FileInfo 파일 읽기 예외: {ex.Message}",
                    "NwInsung02/ReadInfoFileAsync_99");
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
            // 예: info.App_sPredictFolder = @"C:\Program Files (x86)\INSUNGDATA\인성퀵화물통합솔루션";

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

            Debug.WriteLine($"[{APP_NAME}] FileInfo 파일 저장 완료: {sFilePath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{APP_NAME}] FileInfo 파일 저장 실패: {ex.Message}");
        }
    }
    #endregion

    #region 생성자
    public NwInsung02()
    {
        Debug.WriteLine($"[NwInsung02] 생성자 호출: Id={s_Id}, Use={s_Use} --------------------------------------------------------");

        // Context 생성
        m_Context = new InsungContext(APP_NAME, s_Id, s_Pw);

        // AppAct 생성
        m_Context.AppAct = new InsungsAct_App(m_Context);

        // MainWndAct 생성
        m_Context.MainWndAct = new InsungsAct_MainWnd(m_Context);

        // RcptRegPageAct 생성
        m_Context.RcptRegPageAct = new InsungsAct_RcptRegPage(m_Context);

        Debug.WriteLine($"[NwInsung02] Context 생성 완료: AppName={m_Context.AppName}");
    }
    #endregion
}
#nullable restore
