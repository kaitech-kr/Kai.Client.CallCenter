using System.IO;
using Microsoft.WindowsAPICodePack.Dialogs;
using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;
using static Kai.Client.CallCenter.Classes.CommonVars;

namespace Kai.Client.CallCenter.Networks;
#nullable disable

// 외부 네트워크 앱 공통 유틸리티 클래스
public class NwCommon
{
    // 앱 실행 경로 찾기 (예측 폴더 우선 확인 및 결과 반환)
    public static StdResult_String GetAppPath(string sRegName, string sPredictFolder, string sExeFile)
    {
        string sAppFolder = string.Empty;
        try
        {
            // 1. 예측 경로 존재 여부 우선 확인
            if (Directory.Exists(sPredictFolder))
            {
                sAppFolder = sPredictFolder;
            }
            else
            {
                // 경로가 없으면 빈 결과와 함께 위치 정보 반환 (상위에서 에러 메시지 처리)
                return new StdResult_String(string.Empty, "NwCommon/GetAppPath_NotFound");
            }
        }
        catch (Exception e)
        {
            return new StdResult_String(StdUtil.GetExceptionMessage(e), "NwCommon/GetAppPath_Exception");
        }

        // 2. 실행 파일 존재 여부 최종 확인
        string sAppPath = Path.Combine(sAppFolder, sExeFile);
        if (!File.Exists(sAppPath))
        {
            return new StdResult_String(string.Empty, "NwCommon/GetAppPath_ExeNotFound");
        }

        return new StdResult_String(sAppPath);
    }

    /// <summary>
    /// 인성 앱용 고객명 검색 텍스트 생성
    /// - "고객명/담당자명" 형식으로 반환
    /// </summary>
    public static string GetInsungTextForSearch(string custName, string etcName)
    {
        if (string.IsNullOrEmpty(custName)) return custName;

        int index = custName.IndexOf('/');
        if (index >= 0) custName = custName.Substring(0, index);

        return custName + "/" + etcName;
    }

    ///// <summary>
    ///// 스플래시 윈도우 닫기
    ///// </summary>
    //public static bool CloseSplash(string sClassName, string sWndName)
    //{
    //    // SplashWnd가 떠있으면 종료
    //    IntPtr hWnd = StdWin32.FindWindow(sClassName, sWndName);

    //    if (hWnd != IntPtr.Zero)
    //    {
    //        new Thread(() =>
    //        {
    //            Std32Window.PostCloseTwiceWindow(hWnd);
    //        }).Start();

    //        // 3초 동안 죽은거 확인..
    //        for (int i = 0; i < 30; i++)
    //        {
    //            hWnd = StdWin32.FindWindow(sClassName, sWndName);
    //            if (hWnd == IntPtr.Zero) break;
    //            Thread.Sleep(100);
    //        }

    //        if (hWnd != IntPtr.Zero) return false;
    //        Thread.Sleep(1000);
    //    }

    //    return true;
    //}
}


#nullable enable
