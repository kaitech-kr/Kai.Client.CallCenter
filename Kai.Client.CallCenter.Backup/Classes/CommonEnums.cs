using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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

[Flags]
public enum CEnum_Cg24OrderStatus
{
    None = 0,
    공유 = 1,
    중요오더 = 2,
    예약 = 4,
    긴급 = 8,
    왕복 = 16,
    경유 = 32
}
#nullable restore

// ==========================================================
// [신규 추가] 오더 모델용 Enums (TbOrder)
// ==========================================================

// [확장 메서드] Helper Class (Enum 설명 반환용)
public static class EnumExtensions
{
    // Enum의 [Description] 어트리뷰트 값을 가져옴
    public static string ToDesc(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        if (field == null) return value.ToString();
        var attr = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
        return attr?.Description ?? value.ToString();
    }

    // Flags Enum을 "긴급, 왕복" 형태의 문자열로 변환
    public static string ToFlagString(this Enum value)
    {
        var result = new List<string>();
        foreach (Enum item in Enum.GetValues(value.GetType()))
        {
            int iVal = Convert.ToInt32(item);
            if (iVal == 0) continue; // None(0) 제외
            
            if (value.HasFlag(item))
            {
                result.Add(item.ToDesc());
            }
        }
        return result.Count == 0 ? "" : string.Join(", ", result);
    }
}

public enum CarWts : int
{
    [Description("오토바이")] Motorcycle = 0,
    [Description("다마스")] Damas = 1,
    [Description("라보")] Labo = 2,
    [Description("1톤")] W1_0 = 10,
    [Description("1.4톤")] W1_4 = 14,
    [Description("2.5톤")] W2_5 = 25,
    [Description("3.5톤")] W3_5 = 35,
    [Description("5톤")] W5_0 = 50,
    [Description("5톤축")] W5_0_Plus = 51,
    [Description("11톤")] W11_0 = 110,
    [Description("18톤")] W18_0 = 180,
    [Description("25톤")] W25_0 = 250
}

public enum CarTypes : int
{
    [Description("일반")] Normal = 0,
    [Description("카고")] Cargo = 1,
    [Description("탑")] Box = 2,
    [Description("윙바디")] Wing = 3,
    [Description("호로")] Horo = 4, 
    [Description("냉동")] Frozen = 5,
    [Description("냉장")] Refri = 6,
    [Description("리프트")] LiftType = 7,
    [Description("플렉스")] Flex = 8
}

[Flags] // 중복 선택 가능
public enum CarOpts : int
{
    None = 0,
    [Description("리프트")] Lift = 1 << 0,
    [Description("무진동")] NoVib = 1 << 1,
    [Description("항온")] TempCtrl = 1 << 2,
    [Description("장축")] LongBody = 1 << 3,
    [Description("냉동")] FrozenOpt = 1 << 4,
    [Description("냉장")] RefriOpt = 1 << 5
}

[Flags] // 운행 및 화물 성격
public enum RunOpts : int
{
    None = 0,
    [Description("긴급")] Urgent = 1 << 0,
    [Description("왕복")] Round = 1 << 1,
    [Description("예약")] Reserve = 1 << 2,
    [Description("혼적")] Mixed = 1 << 3,
    [Description("동승")] Ride = 1 << 4,
    [Description("경유")] Stopover = 1 << 5,
    [Description("익일")] Tomorrow = 1 << 6 
}

[Flags] // 상/하차 작업 옵션
public enum LoadOpts : int
{
    None = 0,
    [Description("수작업")] Manual = 1 << 0,
    [Description("지게차")] Forklift = 1 << 1,
    [Description("호이스트")] Hoist = 1 << 2,
    [Description("사다리")] Ladder = 1 << 3,
    [Description("계단")] Stairs = 1 << 4,
    [Description("크레인")] Crane = 1 << 5
}

// 오더 상태 (int 매핑용)
public enum OrderStateEnum : int
{
    [Description("접수")] Receipt = 0,
    [Description("대기")] Wait = 1,
    [Description("배차")] Alloc = 2,
    [Description("운행")] Run = 3,
    [Description("완료")] Finish = 4,
    [Description("취소")] Cancel = 9
}
