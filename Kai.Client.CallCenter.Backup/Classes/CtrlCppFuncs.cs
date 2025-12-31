using System.Windows;
using System.Windows.Interop;
using System.Runtime.InteropServices;

namespace Kai.Client.CallCenter.Classes;
public class CtrlCppFuncs
{
    public const string c_sHookDllPath =
        @"D:\CodeWork\WithVs2022\KaiWork\Kai.Common\Kai.Common.CppDll_Common\Kai.Common.CppDll_Common\bin\Kai.Common.CppDll_Common.dll";

    //// Test
    //[DllImport(c_sHookDllPath, CallingConvention = CallingConvention.Cdecl)]
    //public static extern bool Test();

    // Mouse Hook
    [DllImport(c_sHookDllPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool SetMouseHook(IntPtr hWnd, uint uMsg);

    [DllImport(c_sHookDllPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ReleaseMouseHook();

    [DllImport(c_sHookDllPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetMouseLock(bool bLock);

    [DllImport(c_sHookDllPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetHookMouseMove(bool bHook);

    // Keyboard Hook
    [DllImport(c_sHookDllPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool SetKeyboardHook(IntPtr hWnd, uint uMsg);

    [DllImport(c_sHookDllPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ReleaseKeyboardHook();

    [DllImport(c_sHookDllPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetKeyboardLock(bool bLock);

    #region Staic Methods
    public static HwndSource GetHWndSource(Window wnd)
    {
        WindowInteropHelper helper = new WindowInteropHelper(wnd);
        return HwndSource.FromHwnd(helper.Handle);
    }
    #endregion
}


