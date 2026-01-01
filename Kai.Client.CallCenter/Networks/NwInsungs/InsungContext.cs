using Kai.Common.StdDll_Common;
using Kai.Client.CallCenter.Classes;

namespace Kai.Client.CallCenter.Networks.NwInsungs;

#nullable disable
// 인성 앱의 모든 공용 데이터를 담는 Context
public class InsungContext
{
    #region 기본 정보
    // 앱 이름 (INSUNG1, INSUNG2)
    public string AppName { get; set; } = string.Empty;

    // FileInfo 파일 이름 (JSON)
    public string InfoFileName { get; set; } = string.Empty;

    // 로그인 ID
    public string Id { get; set; } = string.Empty;

    // 로그인 PW
    public string Pw { get; set; } = string.Empty;

    // 앱 경로
    public string AppPath { get; set; } = string.Empty;

    // 사용 여부
    public bool IsUse { get; set; } = false;

    // 앱 상태
    public CEnum_AppUsing AppStatus { get; set; } = CEnum_AppUsing.NotUse;
    #endregion

    #region 정보 객체들
    // 파일 정보 (UI 좌표, 윈도우 이름 등 설정)
    public InsungsInfo_File FileInfo { get; set; } = null;

    // 메모리 정보 (윈도우 핸들, 프로세스 정보 등 런타임 상태)
    public InsungsInfo_Mem MemInfo { get; set; } = null;
    #endregion

    #region 액션 객체들
    // 앱 제어 (프로세스 시작, 스플래시 처리 등)
    public InsungsAct_App AppAct { get; set; } = null;

    // 메인 윈도우 제어
    public InsungsAct_MainWnd MainWndAct { get; set; } = null;

    // 접수등록 페이지 제어
    public InsungsAct_RcptRegPage RcptRegPageAct { get; set; } = null;
    #endregion

    #region 생성자
    // 생성자
    public InsungContext(string appName, string infoFileName, string id, string pw, string appPath, bool isUse)
    {
        AppName = appName;
        InfoFileName = infoFileName;
        Id = id;
        Pw = pw;
        AppPath = appPath;
        IsUse = isUse;
        AppStatus = CEnum_AppUsing.NotUse;

        // FileInfo 생성
        FileInfo = new InsungsInfo_File();

        // MemInfo 생성
        MemInfo = new InsungsInfo_Mem();
    }
    #endregion
}
#nullable restore
