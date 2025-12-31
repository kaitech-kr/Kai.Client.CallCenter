using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kai.Common.StdDll_Common;
//using Kai.Common.StdDll_Common.StdVar;

namespace Kai.Client.CallCenter.Classes
{
    public class Simulation_Keyboard
    {
        /*
        #region Send - Key
        /// <summary>
        /// 키 입력 시뮬레이션 (비동기)
        /// </summary>
        /// <param name="vKey">가상 키 코드 (예: StdCommon32.VK_RETURN)</param>
        /// <param name="nMiliSec">입력 전 대기 시간 (ms)</param>
        public static async Task SafeKeySend_Async(int vKey, int nMiliSec = CommonVars.c_nWaitVeryShort)
        {
            try
            {
                await Task.Delay(nMiliSec);
                Std32Key.KeyClick((byte)vKey);
            }
            catch (Exception ex)
            {
                // 로그 처리 필요 시 추가
                Debug.WriteLine($"SafeKeySend_Async 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// Modifier Key 포함 키 입력 시뮬레이션 (예: Ctrl+C)
        /// </summary>
        /// <param name="vModifier">제어 키 (예: StdCommon32.VK_CONTROL)</param>
        /// <param name="vKey">일반 키 (예: 'C')</param>
        /// <param name="nMiliSec">입력 전 대기 시간 (ms)</param>
        public static async Task SafeKeySend_WithAsync(int vModifier, int vKey, int nMiliSec = CommonVars.c_nWaitVeryShort)
        {
            try
            {
                await Task.Delay(nMiliSec);
                Std32Key.KeyClick_With((byte)vModifier, (byte)vKey);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SafeKeySend_WithAsync 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 문자열 입력 시뮬레이션 (클립보드 복사 + 붙여넣기 이용)
        /// </summary>
        /// <param name="sText">입력할 문자열</param>
        /// <param name="nMiliSec">입력 전 대기 시간 (ms)</param>
        public static async Task SafeKeySend_StringAsync(string sText, int nMiliSec = CommonVars.c_nWaitVeryShort)
        {
            try
            {
                await Task.Delay(nMiliSec);
                
                // UI 스레드에서 클립보드 접근 필요
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    System.Windows.Forms.Clipboard.SetText(sText);
                });

                // Ctrl + V
                Std32Key.KeyClick_With(StdCommon32.VK_CONTROL, (byte)'V');
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SafeKeySend_StringAsync 오류: {ex.Message}");
            }
        }
        #endregion

        #region Verification
        /// <summary>
        /// 현재 활성 윈도우의 캡션(제목)을 확인하여 올바른 창인지 검증
        /// </summary>
        /// <param name="sExpectedCaption">기대하는 창 제목 (일부분만 일치해도 통과)</param>
        /// <returns>일치 여부</returns>
        public static bool VerifyActiveWindow(string sExpectedCaption)
        {
            try
            {
                IntPtr hWnd = StdWin32.GetForegroundWindow();
                string sCaption = Std32Window.GetWindowText(hWnd);

                if (string.IsNullOrEmpty(sCaption)) return false;

                return sCaption.Contains(sExpectedCaption);
            }
            catch (Exception)
            {
                return false;
            }
        }
        #endregion
        */
    }
}
