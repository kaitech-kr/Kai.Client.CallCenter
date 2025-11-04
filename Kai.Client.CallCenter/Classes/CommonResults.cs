using Kai.Common.StdDll_Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kai.Client.CallCenter.Classes;
#nullable disable

/// <summary>
/// 자동배차 처리 결과
/// - ResultType: 처리 결과 타입 (성공/실패 × 재적재/비적재)
/// </summary>
[Serializable]
public class CommonResult_AutoAllocProcess : StdResult_Error
{
    #region Properties

    /// <summary>
    /// 처리 결과 타입
    /// </summary>
    public CEnum_AutoAllocProcessResult ResultType { get; set; }

    #endregion

    #region Constructors

    /// <summary>
    /// 기본 생성자 (역직렬화용)
    /// </summary>
    public CommonResult_AutoAllocProcess() : base()
    {
        ResultType = CEnum_AutoAllocProcessResult.FailureAndDiscard;
    }

    /// <summary>
    /// 매개변수 생성자
    /// </summary>
    /// <param name="resultType">처리 결과 타입</param>
    /// <param name="errorMessage">에러 메시지</param>
    /// <param name="errorPosition">에러 위치</param>
    /// <param name="logDirPath">로그 디렉토리 경로</param>
    public CommonResult_AutoAllocProcess(CEnum_AutoAllocProcessResult resultType, string errorMessage = "", string errorPosition = "", string logDirPath = "")
        : base(errorMessage, errorPosition, logDirPath)
    {
        ResultType = resultType;
    }

    #endregion

    #region Static Factory Methods

    /// <summary>
    /// 성공 + 재적재 (계속 관리)
    /// </summary>
    public static CommonResult_AutoAllocProcess SuccessAndReEnqueue(string message = "")
    {
        return new CommonResult_AutoAllocProcess(CEnum_AutoAllocProcessResult.SuccessAndReEnqueue, message);
    }

    /// <summary>
    /// 성공 + 비적재 (완료)
    /// </summary>
    public static CommonResult_AutoAllocProcess SuccessAndComplete(string message = "")
    {
        return new CommonResult_AutoAllocProcess(CEnum_AutoAllocProcessResult.SuccessAndComplete, message);
    }

    /// <summary>
    /// 실패 + 재적재 (재시도)
    /// </summary>
    public static CommonResult_AutoAllocProcess FailureAndRetry(string errorMessage = "", string errorPosition = "", string logDirPath = "")
    {
        return new CommonResult_AutoAllocProcess(CEnum_AutoAllocProcessResult.FailureAndRetry, errorMessage, errorPosition, logDirPath);
    }

    /// <summary>
    /// 실패 + 비적재 (복구 불가능)
    /// </summary>
    public static CommonResult_AutoAllocProcess FailureAndDiscard(string errorMessage = "", string errorPosition = "", string logDirPath = "")
    {
        return new CommonResult_AutoAllocProcess(CEnum_AutoAllocProcessResult.FailureAndDiscard, errorMessage, errorPosition, logDirPath);
    }

    #endregion

    #region Override

    /// <summary>
    /// 디버그용 문자열
    /// </summary>
    public override string ToString()
    {
        return $"[AutoAllocProcess] Type={ResultType}, Err={sErr}, Pos={sPos}";
    }

    #endregion
}

/// <summary>
/// Datagrid 검색 결과 타입
/// </summary>
[Serializable]
public class CommonResult_AutoAllocDatagrid : StdResult_Error
{
    public int nIndex { get; set; } = -1;
    public string sStatus { get; set; }

    public CommonResult_AutoAllocDatagrid() : base() { }
    public CommonResult_AutoAllocDatagrid(int index, string status) : base()
    {
        this.nIndex = index;
        this.sStatus = status;
    }
    public CommonResult_AutoAllocDatagrid(string err, string pos, string logDirPath = "") : base(err, pos, logDirPath)
    {
    }
}
#nullable enable