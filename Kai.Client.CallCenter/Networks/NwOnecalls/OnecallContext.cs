using Kai.Common.StdDll_Common;
using Kai.Client.CallCenter.Classes;

namespace Kai.Client.CallCenter.Networks.NwOnecalls;

#nullable disable
/// <summary>
/// 원콜 앱의 모든 공용 데이터를 담는 Context
/// </summary>
public class OnecallContext
{
    #region 기본 정보
    ///// <summary>
    ///// 앱 이름 (ONECALL)
    ///// </summary>
    //public string AppName { get; set; } = string.Empty;

    ///// <summary>
    ///// 로그인 ID
    ///// </summary>
    //public string Id { get; set; } = string.Empty;

    ///// <summary>
    ///// 로그인 PW
    ///// </summary>
    //public string Pw { get; set; } = string.Empty;

    ///// <summary>
    ///// 앱 상태
    ///// </summary>
    //public CEnum_AppUsing AppStatus { get; set; } = CEnum_AppUsing.NotUse;
    #endregion

    #region 정보 객체들
    ///// <summary>
    ///// 파일 정보 (UI 좌표, 윈도우 이름 등 설정)
    ///// </summary>
    //public OnecallInfo_File FileInfo { get; set; } = null;

    ///// <summary>
    ///// 메모리 정보 (윈도우 핸들, 프로세스 정보 등 런타임 상태)
    ///// </summary>
    //public OnecallInfo_Mem MemInfo { get; set; } = null;
    #endregion

    #region 액션 객체들
    ///// <summary>
    ///// 앱 제어 (프로세스 시작, 스플래시 처리 등)
    ///// </summary>
    //public OnecallAct_App AppAct { get; set; } = null;

    ///// <summary>
    ///// 메인 윈도우 제어
    ///// </summary>
    //public OnecallAct_MainWnd MainWndAct { get; set; } = null;

    ///// <summary>
    ///// 접수등록 페이지 제어
    ///// </summary>
    //public OnecallAct_RcptRegPage RcptRegPageAct { get; set; } = null;
    #endregion

    #region 생성자
    ///// <summary>
    ///// 생성자
    ///// </summary>
    //public OnecallContext(string appName, string id, string pw)
    //{
    //    AppName = appName;
    //    Id = id;
    //    Pw = pw;
    //    AppStatus = CEnum_AppUsing.NotUse;

    //    // FileInfo 생성
    //    FileInfo = new OnecallInfo_File();

    //    // MemInfo 생성
    //    MemInfo = new OnecallInfo_Mem();
    //}
    #endregion
}
#nullable restore
