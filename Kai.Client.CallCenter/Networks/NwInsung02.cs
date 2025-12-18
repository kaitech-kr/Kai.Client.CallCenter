using System.Diagnostics;

using Kai.Common.StdDll_Common;
using Kai.Client.CallCenter.Networks.NwInsungs;

namespace Kai.Client.CallCenter.Networks;
#nullable disable

/// <summary>
/// 인성2 자동배차 앱 (InsungAutoAllocBase 상속)
/// </summary>
public class NwInsung02 : InsungAutoAllocBase
{
    #region 1. Static Configuration - 정적 설정
    public static bool s_Use { get; set; } = false;
    public static string s_Id { get; set; } = string.Empty;
    public static string s_Pw { get; set; } = string.Empty;
    public static string s_AppPath { get; set; } = string.Empty;
    #endregion

    #region 3. Constructor - 생성자
    // 인성2 생성자
    public NwInsung02() : base()
    {
        m_Context = new InsungContext(StdConst_Network.INSUNG2, "Insung02_FileInfo.txt", s_Id, s_Pw, s_AppPath, s_Use);
        m_Context.AppAct = new InsungsAct_App(m_Context);
    }
    #endregion
}
#nullable restore
