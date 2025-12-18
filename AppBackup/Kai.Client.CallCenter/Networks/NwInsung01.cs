using System.Diagnostics;

using Kai.Common.StdDll_Common;
using Kai.Client.CallCenter.Networks.NwInsungs;

namespace Kai.Client.CallCenter.Networks;
#nullable disable

/// <summary>
/// 인성1 자동배차 앱 (InsungAutoAllocBase 상속)
/// </summary>
public class NwInsung01 : InsungAutoAllocBase
{
    #region 1. Static Configuration - 정적 설정
    public static bool s_Use { get; set; } = false;
    public static string s_Id { get; set; } = string.Empty;
    public static string s_Pw { get; set; } = string.Empty;
    public static string s_AppPath { get; set; } = string.Empty;
    #endregion

    #region 2. Override Abstract Members - 추상 멤버 구현
    protected override string APP_NAME => StdConst_Network.INSUNG1;
    protected override string INFO_FILE_NAME => "Insung01_FileInfo.txt";
    protected override bool GetStaticUse() => s_Use;
    protected override void SetStaticUse(bool value) => s_Use = value;
    protected override string GetStaticId() => s_Id;
    protected override string GetStaticPw() => s_Pw;
    protected override string GetStaticAppPath() => s_AppPath;
    #endregion

    #region 3. Constructor - 생성자
    /// <summary>
    /// 인성1 생성자 (베이스 클래스 생성자 호출)
    /// </summary>
    public NwInsung01() : base()
    {
        // 베이스 클래스에서 모든 초기화 수행
    }
    #endregion
}
#nullable restore
