using System;
using System.Drawing;
using Draw = System.Drawing;

using Kai.Common.NetDll_WpfCtrl.NetOFR;
using Kai.Common.StdDll_Common;
using Kai.Server.Main.KaiWork.DBs.Postgres.CharDB.Models;

using static Kai.Common.StdDll_Common.StdConst_FuncName;
#nullable disable
namespace Kai.Client.CallCenter.OfrWorks;

/// <summary>
/// TbText 테이블 조회 결과 + 이미지 분석 정보
/// </summary>
[Serializable]
public class OfrResult_TbText : StdResult_Error
{
    public TbText tbText { get; set; } = null;
    public OfrModel_BitmapAnalysis analyText { get; set; } = null;

    /// <summary>
    /// tbText에서 Text 값을 가져오는 편의 속성
    /// </summary>
    public string _sResult => tbText?.Text ?? null;

    #region Constructor
    /// <summary>
    /// 기본 생성자
    /// </summary>
    public OfrResult_TbText() : base()
    {
    }

    /// <summary>
    /// 성공 생성자 - DB에서 찾았을 때
    /// </summary>
    public OfrResult_TbText(TbText tb, OfrModel_BitmapAnalysis analy = null) : base()
    {
        tbText = tb;
        analyText = analy;
    }

    /// <summary>
    /// 실패 생성자 - DB에서 못 찾았거나 에러 발생시
    /// </summary>
    public OfrResult_TbText(OfrModel_BitmapAnalysis analy, string err, string pos, string logPath = "")
        : base(err, pos, logPath)
    {
        analyText = analy;
    }
    #endregion

    public override string ToString()
    {
        string str = "";

        if (tbText != null) str += tbText.ToString();

        if (!string.IsNullOrEmpty(sPos))
        {
            if (str.Length > 0) str += Environment.NewLine;
            str += $"[sPos]: {sPos}";
        }

        if (!string.IsNullOrEmpty(sErr))
        {
            if (str.Length > 0) str += Environment.NewLine;
            str += $"[sErr]: {sErr}";
        }

        return str;
    }
}

//[Serializable]
//public class OfrResult_TbCharInSet : StdResult_Error
//{
//    public TbChar tbChar { get; set; } = null;
//    public OfrModel_BmpCharAnalysis analyChar { get; set; } = null;
//
//    #region Constructor
//    public OfrResult_TbCharInSet() { }
//    public OfrResult_TbCharInSet(OfrModel_BmpCharAnalysis analy, TbChar tb)
//    {
//        tbChar = tb;
//        analyChar = analy;
//    }
//    public OfrResult_TbCharInSet(string err, string pos, string logPath = "")
//    : base(err, pos, logPath)
//    {
//    }
//    #endregion
//
//    public override string ToString()
//    {
//        string str = "";
//
//        if (!string.IsNullOrEmpty(sPos)) str = $"\n[sPos]: {sPos}";
//
//        if (!string.IsNullOrEmpty(sErr))
//        {
//            if (str.Length > 0) str += $"{Environment.NewLine}";
//            str += $"[sErr]: {sErr}";
//        }
//
//        return str;
//    }
//}

/// <summary>
/// TbCharSet 리스트 조회 결과 - 복합 문자 인식용
/// </summary>
[Serializable]
public class OfrResult_TbCharInSet : StdResult_Error
{
    public TbChar tbChar { get; set; } = null;
    public OfrModel_BitmapAnalysis analyChar { get; set; } = null;

    #region Constructor
    /// <summary>
    /// 기본 생성자
    /// </summary>
    public OfrResult_TbCharInSet() { }

    /// <summary>
    /// 성공 생성자
    /// </summary>
    public OfrResult_TbCharInSet(OfrModel_BitmapAnalysis analy, TbChar tb)
    {
        tbChar = tb;
        analyChar = analy;
    }

    /// <summary>
    /// 실패 생성자
    /// </summary>
    public OfrResult_TbCharInSet(string err, string pos, string logPath = "")
    : base(err, pos, logPath)
    {
    }
    #endregion

    public override string ToString()
    {
        string str = "";

        if (!string.IsNullOrEmpty(sPos))
        {
            if (str.Length > 0) str += Environment.NewLine;
            str += $"[sPos]: {sPos}";
        }

        if (!string.IsNullOrEmpty(sErr))
        {
            if (str.Length > 0) str += Environment.NewLine;
            str += $"[sErr]: {sErr}";
        }

        return str;
    }
}
//
//[Serializable]
//public class OfrResult_TbCharSetList : StdResult_Error
//{
//    public string strResult { get; set; } = null;
//    public Draw.Bitmap bmpCapture { get; set; } = null;
//    public List<OfrResult_TbCharInSet> listCharResult { get; set; } = null;
//
//    #region Constructor
//    public OfrResult_TbCharSetList() : base()
//    {
//        listCharResult = new List<OfrResult_TbCharInSet>();
//    }
//    public OfrResult_TbCharSetList(Draw.Bitmap bmpCapture)
//    {
//        listCharResult = new List<OfrResult_TbCharInSet>();
//    }
//
//    // 오류
//    public OfrResult_TbCharSetList(Draw.Bitmap bmpCapture, string err, string pos, string logDirPath = "")
//        : base(err, pos, logDirPath)
//    {
//        this.bmpCapture = bmpCapture;
//        listCharResult = new List<OfrResult_TbCharInSet>();
//    }
//    //public OfrResult_TbCharSetList(string err, string pos, bool escape = false, string logDirPath = "")
//    //    : base(err, pos, logDirPath)
//    //{
//    //    listCharResult = new List<OfrResult_TbCharInSet>();
//    //}
//    #endregion
//
//    public override string ToString()
//    {
//        string str = strResult;
//
//        if (!string.IsNullOrEmpty(sPos)) str = $"\n[sPos]: {sPos}";
//
//        if (!string.IsNullOrEmpty(sErr))
//        {
//            if (str.Length > 0) str += $"{Environment.NewLine}";
//            str += $"[sErr]: {sErr}";
//        }
//
//        return str;
//    }
//}

/// <summary>
/// TbCharSetList 조회 결과 - 복합 문자열 인식용
/// </summary>
[Serializable]
public class OfrResult_TbCharSetList : StdResult_Error
{
    public string strResult { get; set; } = null;
    public Draw.Bitmap bmpCapture { get; set; } = null;
    public List<OfrResult_TbCharInSet> listCharResult { get; set; } = null;

    #region Constructor
    /// <summary>
    /// 기본 생성자
    /// </summary>
    public OfrResult_TbCharSetList() : base()
    {
        listCharResult = new List<OfrResult_TbCharInSet>();
    }

    /// <summary>
    /// Bitmap만 받는 생성자
    /// </summary>
    public OfrResult_TbCharSetList(Draw.Bitmap bmpCapture)
    {
        this.bmpCapture = bmpCapture;
        listCharResult = new List<OfrResult_TbCharInSet>();
    }

    /// <summary>
    /// 에러 생성자
    /// </summary>
    public OfrResult_TbCharSetList(Draw.Bitmap bmpCapture, string err, string pos, string logDirPath = "")
        : base(err, pos, logDirPath)
    {
        this.bmpCapture = bmpCapture;
        listCharResult = new List<OfrResult_TbCharInSet>();
    }
    #endregion

    public override string ToString()
    {
        string str = strResult ?? "";

        if (!string.IsNullOrEmpty(sPos))
        {
            if (str.Length > 0) str += Environment.NewLine;
            str += $"[sPos]: {sPos}";
        }

        if (!string.IsNullOrEmpty(sErr))
        {
            if (str.Length > 0) str += Environment.NewLine;
            str += $"[sErr]: {sErr}";
        }

        return str;
    }
}
#nullable enable
