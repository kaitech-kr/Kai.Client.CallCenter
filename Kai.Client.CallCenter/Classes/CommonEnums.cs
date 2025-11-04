namespace Kai.Client.CallCenter.Classes;
#nullable disable
public enum CEnum_KaiAppMode
{
    Master,  // 접수 + 배차
    Sub      // 접수만
}

public enum CEnum_AppUsing
{
    NotUse,  // 사용 안 함
    Use      // 사용
}

/// <summary>
/// 자동배차 처리 결과 타입
/// </summary>
public enum CEnum_AutoAllocProcessResult
{
    /// <summary>성공 + 재적재 (계속 관리)</summary>
    SuccessAndReEnqueue,

    /// <summary>성공 + 비적재 (완료)</summary>
    SuccessAndComplete,

    /// <summary>실패 + 재적재 (재시도)</summary>
    FailureAndRetry,

    /// <summary>실패 + 비적재 (복구 불가능)</summary>
    FailureAndDiscard
}
#nullable restore

