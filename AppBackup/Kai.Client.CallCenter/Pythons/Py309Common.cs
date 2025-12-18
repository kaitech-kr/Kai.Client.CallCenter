using Python.Runtime; // pythonnet 누겟 설치
using Draw = System.Drawing;

using Kai.Common.StdDll_Common;

using static Kai.Client.CallCenter.Classes.CommonVars;

namespace Kai.Client.CallCenter.Pythons;
#nullable disable
public class Py309Common : IDisposable
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
            PythonEngine.Shutdown();
        }
    }

    // // TODO: 비관리형 리소스를 해제하는 코드가 'Dispose(bool disposing)'에 포함된 경우에만 종료자를 재정의합니다.
    // ~Py309Common()
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
    public static string sErr = "";
    public static Py309Common s_Py309Common;
    public static dynamic s_Sys;
    public static dynamic s_PyTest;
    public static dynamic s_PyMouse;
    public static dynamic s_PyImage;
    #endregion

    #region Basic
    public Py309Common()
    {
        Runtime.PythonDLL = s_sCurDir + @"\Python\python39.dll";// @"C:\Users\gsqui\miniconda3\python39.dll"; // 파이썬 DLL경로
        //PythonEngine.PythonHome = @"C:\Users\gsqui\miniconda3\envs\env309_kai"; // 파이썬 설치경로
        PythonEngine.PythonHome = s_sCurDir + @"\Python\env309_kai"; // 파이썬 설치경로
        PythonEngine.Initialize();
    }

    public static void Destroy()
    {
        if (s_Py309Common != null) s_Py309Common.Dispose();
    }

    public static StdResult_Bool Create()
    {
        s_Py309Common = new Py309Common();

        // sys 모듈을 가져옵니다.
        s_Sys = Py.Import("sys");
        if (s_Sys == null)
            return new StdResult_Bool("sys 모듈을 가져올수 없읍니다.", "Py309Common/Create_01");

        // path 에 모듈 경로를 추가합니다.
        //s_Sys.path.append(@"C:\Users\gsqui\miniconda3\envs\env309_kai\Lib\site-packages");  // 파이썬 환경모듈 경로
        s_Sys.path.append(s_sCurDir + @"\Python\env309_kai\Lib\site-packages");  // 파이썬 환경모듈 경로

        //s_Sys.path.append(@"D:\CodeWork\WithVs2022\KaiWork\Kai.Common\Kai.Common.Python309Works"); // 파이썬 작업 경로
        s_Sys.path.append(s_sCurDir + @"\Python"); // 파이썬 작업 경로

        using (Py.GIL()) // Acquire the Python Global Interpreter Lock
        {
            // Python 모듈을 가져옵니다.
            s_PyTest = Py.Import("PyTest");
            s_PyMouse = Py.Import("PyMouse");
            //s_PyImage = Py.Import("PyImage");
        }

        if (s_PyTest == null)
            return new StdResult_Bool("PyTest 모듈을 가져올수 없읍니다.", "Py309Common/Create_02");

        if (s_PyMouse == null)
            return new StdResult_Bool("PyMouse 모듈을 가져올수 없읍니다.", "Py309Common/Create_03");

        //if (s_PyImage == null)
        //    return new StdResult_Bool(new Exception("PyImage 모듈을 가져올수 없읍니다."));

        return new StdResult_Bool(true);
    }
    #endregion

    public static PyResult_Object GetPyResult_Object(PyObject pyObj)
    {
        PyResult_Object result = new PyResult_Object();

        result.bSuccess = pyObj[0].As<bool>();
        result.pyObjData = pyObj[1];
        result.sData = pyObj[2].As<string>();

        return result;
    }

    public static PyResult_Bool GetPyResult_Bool(PyObject pyObj)
    {
        PyResult_Bool result = new PyResult_Bool();

        result.bSuccess = pyObj[0].As<bool>();
        result.bData = pyObj[1].As<bool>();
        result.sData = pyObj[2].As<string>();

        return result;
    }

    public static PyResult_Double GetPyResult_Double(PyObject pyObj)
    {
        PyResult_Double result = new PyResult_Double();

        result.bSuccess = pyObj[0].As<bool>();
        result.dData = pyObj[1].As<double>();
        result.sData = pyObj[2].As<string>();

        return result;
    }

    public static PyResult_MonitorNum_Coordinate GetPyResult_MonitorNum_Coordinate(PyObject pyObj)
    {
        PyResult_MonitorNum_Coordinate result = new PyResult_MonitorNum_Coordinate();

        result.bSuccess = pyObj[0].As<bool>();
        if (pyObj[1] != null)
        {
            result.nMonitorNum = pyObj[1][0].As<int>();
            if (pyObj[1][1] != null) result.ptCoordinate = new Draw.Point(pyObj[1][1][0].As<int>(), pyObj[1][1][1].As<int>());
        }

        result.sData = pyObj[2].As<string>();

        return result;
    }

    public static PyResult_More_Similar GetPyResult_More_Similar(PyObject pyObj)
    {
        PyResult_More_Similar result = new PyResult_More_Similar();

        result.bSuccess = pyObj[0].As<bool>();
        result.nIndex = pyObj[1].As<int>();
        result.dMaxSimil = pyObj[2].As<double>();
        result.dGabSimil = pyObj[3].As<double>();
        result.sData = pyObj[4].As<string>();

        return result;
    }
}

public class PyResult_Object
{
    public bool bSuccess = true;
    public PyObject pyObjData = null;
    public string sData = "";

    override public string ToString()
    {
        string str = $"bSuccess: {bSuccess}   {Environment.NewLine}";
        str += $"pyObjData: {pyObjData}   {Environment.NewLine}";
        str += $"sData: {sData}";

        return str;
    }
}

public class PyResult_Bool
{
    public bool bSuccess = true;
    public bool bData = false;
    public string sData = "";

    override public string ToString()
    {
        string str = $"bSuccess: {bSuccess}   {Environment.NewLine}";
        str += $"bData: {bData}   {Environment.NewLine}";
        str += $"sData: {sData}";

        return str;
    }
}

public class PyResult_Int
{
    public bool bSuccess = true;
    public int nData = -1;
    public string sData = "";

    override public string ToString()
    {
        string str = $"bSuccess: {bSuccess}   {Environment.NewLine}";
        str += $"nData: {nData}   {Environment.NewLine}";
        str += $"sData: {sData}";

        return str;
    }
}

public class PyResult_Double
{
    public bool bSuccess = true;
    public double dData = 0.0;
    public string sData = "";

    override public string ToString()
    {
        string str = $"bSuccess: {bSuccess}   {Environment.NewLine}";
        str += $"dData: {dData}   {Environment.NewLine}";
        str += $"sData: {sData}";

        return str;
    }
}

public class PyResult_MonitorNum_Coordinate
{
    public bool bSuccess = true;
    public int nMonitorNum = 0;
    public Draw.Point ptCoordinate = StdUtil.s_ptDrawInvalid;
    public string sData = "";

    override public string ToString()
    {
        string str = $"bSuccess: {bSuccess}   {Environment.NewLine}";
        str += $"nMonitorNum: {nMonitorNum}   {Environment.NewLine}";
        str += $"ptCoordinate: {ptCoordinate}   {Environment.NewLine}";
        str += $"sData: {sData}";

        return str;
    }
}

public class PyResult_More_Similar
{
    public bool bSuccess = true;
    public int nIndex = -1;
    public double dMaxSimil = 0.0;
    public double dGabSimil = 0.0;
    public string sData = "";

    override public string ToString()
    {
        string str = $"bSuccess: {bSuccess}   {Environment.NewLine}";
        str += $"nIndex: {nIndex}   {Environment.NewLine}";
        str += $"dMaxSimil: {dMaxSimil}   {Environment.NewLine}";
        str += $"dGabSimil: {dGabSimil}   {Environment.NewLine}";
        str += $"sData: {sData}";

        return str;
    }
}
#nullable enable
