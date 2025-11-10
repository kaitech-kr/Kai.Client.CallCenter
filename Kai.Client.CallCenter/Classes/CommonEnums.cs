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

/// <summary>
/// Datagrid 검증 이슈 플래그 (비트 조합 가능)
/// </summary>
[Flags]
public enum CEnum_DgValidationIssue
{
    None = 0,               // 문제 없음
    InvalidColumnCount = 1, // 컬럼 개수 틀림
    InvalidColumn = 2,      // 필요 없는 컬럼 존재
    WrongOrder = 4,         // 컬럼 순서 틀림
    WrongWidth = 8          // 컬럼 너비 틀림 (허용 오차 초과)
}

/// <summary>
/// 고객 검색 결과 타입
/// </summary>
public enum CEnum_CustSearchCount
{
    Null = 0,   // 검색 실패
    None = 1,   // 검색 결과 없음 (신규 고객)
    One = 2,    // 검색 결과 1개 (정상)
    Multi = 3   // 검색 결과 복수 (수동 처리 필요)
}
#nullable restore

