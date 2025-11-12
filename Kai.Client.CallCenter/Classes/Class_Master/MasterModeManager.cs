using System.Diagnostics;
using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;
using Kai.Common.FrmDll_WpfCtrl;
using Kai.Common.NetDll_WpfCtrl.NetWnds;
using static Kai.Common.FrmDll_FormCtrl.FormFuncs;
using static Kai.Common.FrmDll_WpfCtrl.FrmSystemDisplays;
using static Kai.Client.CallCenter.Classes.CommonVars;
using Kai.Client.CallCenter.Windows;

namespace Kai.Client.CallCenter.Classes.Class_Master;
#nullable disable
public class MasterModeManager : IDisposable
{
    #region Dispose
    private bool disposedValue;
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: 관리형 상태(관리형 개체)를 삭제합니다.
            }

            // TODO: 비관리형 리소스(비관리형 개체)를 해제하고 종료자를 재정의합니다.
            // TODO: 큰 필드를 null로 설정합니다.
            disposedValue = true;
        }
    }

    // // TODO: 비관리형 리소스를 해제하는 코드가 'Dispose(bool disposing)'에 포함된 경우에만 종료자를 재정의합니다.
    // ~CtrlExcel()
    // {
    //     // 이 코드를 변경하지 마세요. 'Dispose(bool disposing)' 메서드에 정리 코드를 입력합니다.
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // 이 코드를 변경하지 마세요. 'Dispose(bool disposing)' 메서드에 정리 코드를 입력합니다.
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    #endregion

    #region Variables
    private VirtualMonitorWnd m_VirtualMonitorWnd = null;
    private ExternalAppController m_ExternalAppController = null;

    /// <summary>
    /// ExternalAppController 접근 (읽기 전용)
    /// </summary>
    public ExternalAppController ExternalAppController => m_ExternalAppController;
    #endregion

    #region 생성자
    public MasterModeManager()
    {
    }
    #endregion

    #region Shutdown
    public async Task ShutdownAsync()
    {
        try
        {
            Debug.WriteLine("[MasterModeManager] Shutdown 시작");

            // ExternalAppController 정리
            if (m_ExternalAppController != null)
            {
                try
                {
                    Debug.WriteLine("[MasterModeManager] ExternalAppController 정리 중");
                    await m_ExternalAppController.ShutdownAsync();
                    m_ExternalAppController.Dispose();
                    m_ExternalAppController = null;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MasterModeManager] ExternalAppController 정리 실패 (무시): {ex.Message}");
                }
            }

            // 가상모니터 윈도 닫기
            if (m_VirtualMonitorWnd != null)
            {
                try
                {
                    // 윈도우가 아직 열려있고 유효한 경우에만 닫기
                    if (m_VirtualMonitorWnd.IsLoaded)
                    {
                        m_VirtualMonitorWnd.Close();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MasterModeManager] VirtualMonitorWnd 닫기 실패 (무시): {ex.Message}");
                }
                finally
                {
                    m_VirtualMonitorWnd = null;
                }
            }

            // 가상모니터 제거
            if (s_Screens?.m_VirtualMonitor != null)
            {
                FrmVirtualMonitor.DeleteVirtualMonitor();
            }

            Debug.WriteLine("[MasterModeManager] Shutdown 완료");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MasterModeManager] Shutdown 실패: {ex.Message}");
        }
    }
    #endregion

    public async Task<StdResult_Status> InitializeAsync()
    {
        // 1. 가상모니터 생성
        List<MonitorInfo> listOrg = await s_Screens.MonitorInfosToListAsync();
        if (s_Screens.m_VirtualMonitor == null) // 가상모니터가 없으면 생성
        {
            // ShowLoading
            NetLoadingWnd.ShowLoading(s_MainWnd, "가상모니터를 생성중 입니다.");

            // 커서 위치 백업 (가상모니터 생성 시 Windows가 자동으로 마우스를 이동시키는 것을 방지)
            var cursorBackup = Std32Cursor.GetCursorPos_AbsDrawPt();

            int nOldCount = s_Screens.m_ListMonitorInfo.Count;

            StdResult_Bool resultBool = await FrmVirtualMonitor.MakeVirtualMonitorAsync();
            if (!resultBool.bResult)
            {
                Std32Cursor.SetCursorPos_AbsDrawPt(cursorBackup);  // 커서 복원
                NetLoadingWnd.HideLoading();
                return new StdResult_Status(StdResult.Fail, "가상모니터 생성실패", "MasterModeManager/InitializeAsync_01");
            }

            // 가상 모니터 생성 후 시스템이 인식할 때까지 재시도 (최대 3초, 500ms 간격)
            int retryCount = 0;
            int maxRetries = 6; // 6번 * 500ms = 3초
            List<MonitorInfo> listNew = null;
            while (retryCount < maxRetries)
            {
                await Task.Delay(500);
                listNew = await s_Screens.MonitorInfosToListAsync();

                if (s_Screens.m_VirtualMonitor != null)
                {
                    Debug.WriteLine($"[MasterModeManager] 가상모니터 인식 성공 (재시도 {retryCount + 1}회)");
                    break;
                }

                retryCount++;
                Debug.WriteLine($"[MasterModeManager] 가상모니터 미인식, 재시도 중... ({retryCount}/{maxRetries})");
            }

            int nNewCount = s_Screens.m_ListMonitorInfo.Count;
            if (s_Screens.m_VirtualMonitor == null)  // 개수가 아닌 m_VirtualMonitor로 체크
            {
                Std32Cursor.SetCursorPos_AbsDrawPt(cursorBackup);  // 커서 복원
                NetLoadingWnd.HideLoading();
                return new StdResult_Status(StdResult.Fail,
                    $"가상모니터 생성실패: s_Screens.m_VirtualMonitor == null (전: {nOldCount}개, 후: {nNewCount}개)",
                    "MasterModeManager/InitializeAsync_02");
            }

            // Check Virtual Monitor Resolution
            if (!FrmVirtualMonitor.AdjustVirtualMonitorPosAndSize(s_Screens))
            {
                Std32Cursor.SetCursorPos_AbsDrawPt(cursorBackup);  // 커서 복원
                NetLoadingWnd.HideLoading();
                return new StdResult_Status(StdResult.Fail, "가상모니터 해상도 조정실패", "MasterModeManager/InitializeAsync_03");
            }

            // 원상복귀
            int lastX = 0;
            for (int i = 0; i < listOrg.Count; i++)
            {
                lastX = i * 1920;
                s_Screens.ChangePosition(listOrg[i].DeviceName, lastX, 0);
                int index = listNew.FindIndex(x => x.DeviceName == listOrg[i].DeviceName);
                if (index >= 0) listNew.RemoveAt(index);
            }

            if (listNew.Count != 1)
            {
                Std32Cursor.SetCursorPos_AbsDrawPt(cursorBackup);  // 커서 복원
                NetLoadingWnd.HideLoading();
                return new StdResult_Status(StdResult.Fail, "가상모니터 생성실패", "MasterModeManager/InitializeAsync_04");
            }

            lastX += (1920);
            s_Screens.ChangePosition(listNew[0].DeviceName, lastX, 1080);
            await s_Screens.MonitorInfosToListAsync();

            // 커서 위치 복원
            Std32Cursor.SetCursorPos_AbsDrawPt(cursorBackup);

            NetLoadingWnd.HideLoading();

            // 가상모니터 뷰 윈도 생성
            m_VirtualMonitorWnd = new VirtualMonitorWnd();
        }
        else
        {
            Debug.WriteLine("[MasterModeManager] 가상모니터가 이미 존재합니다.");
        }

        // WorkingMonitor 설정 - 가상모니터가 있다는 전제하에 설정 (신규 생성 or 기존 사용 모두 여기서 설정)
        if (s_Screens.m_VirtualMonitor != null)
        {
            //s_Screens.m_WorkingMonitor = s_Screens.m_ListMonitorInfo[1];  // m_VirtualMonitor, m_PrimaryMonitor, m_ListMonitorInfo[0]
            s_Screens.m_WorkingMonitor = s_Screens.m_VirtualMonitor;  // m_VirtualMonitor, m_PrimaryMonitor, m_ListMonitorInfo[0]
            Debug.WriteLine($"[MasterModeManager] m_WorkingMonitor 설정 완료: {s_Screens.m_WorkingMonitor}");
        }
        else
        {
            return new StdResult_Status(StdResult.Fail, "가상모니터가 없습니다. Master 모드는 가상모니터가 필수입니다.", "MasterModeManager/InitializeAsync_05");
        }

        // 2. ExternalAppController 초기화 (New - Refactored)
        Debug.WriteLine("[MasterModeManager] ExternalAppController 초기화 시작");
        m_ExternalAppController = new ExternalAppController();
        StdResult_Status resultExternalApp = await m_ExternalAppController.InitializeAsync();
        if (resultExternalApp.Result != StdResult.Success)
        {
            Debug.WriteLine($"[MasterModeManager] ExternalAppController 초기화 실패: {resultExternalApp}");
            return new StdResult_Status(StdResult.Fail, resultExternalApp.sErrNPos, "MasterModeManager/InitializeAsync_ExternalApp");
        }
        Debug.WriteLine("[MasterModeManager] ExternalAppController 초기화 완료");

        return new StdResult_Status(StdResult.Success);
    }
}
#nullable restore