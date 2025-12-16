using System.IO;
using Microsoft.WindowsAPICodePack.Dialogs;
using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;
using static Kai.Client.CallCenter.Classes.CommonVars;

namespace Kai.Client.CallCenter.Networks;
#nullable disable

/// <summary>
/// 외부 네트워크 앱 공통 유틸리티 클래스
/// </summary>
public class NwCommon
{
    /// <summary>
    /// 앱 실행 경로 찾기
    /// - Registry에서 경로 읽기
    /// - 없으면 예측 경로 사용
    /// - 예측 경로도 없으면 사용자에게 폴더 선택 다이얼로그 표시
    /// </summary>
    public static StdResult_String GetAppPath(string sRegName, string sPredictFolder, string sExeFile)
    {
        string sAppFolder = string.Empty;
        try
        {
            //sAppFolder = s_KaiReg.GetStringValue(sRegName); // Registry에 등록된 경로를 읽는다
            sAppFolder = "";

            if (string.IsNullOrEmpty(sAppFolder)) // 정보가 없을시..
            {
                if (Directory.Exists(sPredictFolder)) // 예측 경로가 존재하면
                {
                    sAppFolder = sPredictFolder;
                    //s_KaiReg.SetValue(sRegName, sAppFolder); // 실행경로를 등록한다
                }
                else // 예측 경로가 존재하지 않으면 - FolderDialog를 띄워서 경로를 입력받는다
                {
                    CommonOpenFileDialog dialog = new CommonOpenFileDialog();
                    dialog.IsFolderPicker = true; // 폴더 선택 모드 활성화
                    dialog.Title = "폴더를 선택하세요";

                    if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        sAppFolder = dialog.FileName;
                        //s_KaiReg.SetValue(sRegName, sAppFolder); // 실행경로를 등록한다
                    }
                }
            }
        }
        catch (Exception e)
        {
            return new StdResult_String(StdUtil.GetExceptionMessage(e), "NwCommon/GetAppPath_999");
        }

        if (string.IsNullOrEmpty(sAppFolder))
        {
            return new StdResult_String("등록된 경로가 없습니다.", "NwCommon/GetAppPath_01");
        }

        string sAppPath = Path.Combine(sAppFolder, sExeFile);
        if (!File.Exists(sAppPath))
        {
            return new StdResult_String(
                $"등록된 경로에 실행파일이 존재하지 않습니다: {sAppFolder}, {sExeFile}", "NwCommon/GetAppPath_02");
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

    /// <summary>
    /// 스플래시 윈도우 닫기
    /// </summary>
    public static bool CloseSplash(string sClassName, string sWndName)
    {
        // SplashWnd가 떠있으면 종료
        IntPtr hWnd = StdWin32.FindWindow(sClassName, sWndName);

        if (hWnd != IntPtr.Zero)
        {
            new Thread(() =>
            {
                Std32Window.PostCloseTwiceWindow(hWnd);
            }).Start();

            // 3초 동안 죽은거 확인..
            for (int i = 0; i < 30; i++)
            {
                hWnd = StdWin32.FindWindow(sClassName, sWndName);
                if (hWnd == IntPtr.Zero) break;
                Thread.Sleep(100);
            }

            if (hWnd != IntPtr.Zero) return false;
            Thread.Sleep(1000);
        }

        return true;
    }
}

/// <summary>
/// Datagrid 컬럼 헤더 정보
/// </summary>
public class NwCommon_DgColumnHeader
{
    public string sName = string.Empty;
    public bool bOfrSeq = false; // Ofr시 Multi, Seq처리 여부
    public int nWidth = 0;
}
#nullable enable
