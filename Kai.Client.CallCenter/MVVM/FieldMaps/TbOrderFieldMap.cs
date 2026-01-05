using System;
using System.Collections.Generic;
using System.Linq;

namespace Kai.Client.CallCenter.MVVM.FieldMaps;

/// <summary>
/// TbOrder DB 필드 ↔ UI(ViewModel) 필드 매핑 정의
/// </summary>
public static class TbOrderFieldMap
{
    #region DB → UI 매핑 (이름이 다른 필드만 - 형식 변환 필요)
    /// <summary>
    /// DB 속성명 → UI 속성명 매핑 (형식 변환이 있는 필드)
    /// </summary>
    public static readonly Dictionary<string, string> DbToUi = new()
    {
        // 형식 변환 필드
        ["DtRegist"] = "RegTime",      // DateTime → string (포맷)
        ["FeeTotal"] = "sFeeTotal",    // int → string (천단위 구분)
    };
    #endregion

    #region UI → DB 매핑 (역방향)
    /// <summary>
    /// UI 속성명 → DB 속성명 매핑
    /// </summary>
    public static readonly Dictionary<string, string> UiToDb =
        DbToUi.ToDictionary(x => x.Value, x => x.Key);
    #endregion

    #region 동일 이름 필드 (DB = UI)
    /// <summary>
    /// DB와 UI에서 동일한 이름을 사용하는 필드들
    /// </summary>
    public static readonly HashSet<string> SameNameFields = new()
    {
        // 기본 정보
        "KeyCode",
        "OrderState",
        "CallCustFrom",
        "CallCompName",
        "CallCustName",
        "CallDeptName",
        "CallChargeName",
        "CallTelNo",
        "StartDongBasic",
        "DestDongBasic",

        // 요금
        "FeeCommi",
        "FeeType",
        "TaxBill",

        // 시간
        "ReceiptTime",
        "AllocTime",
        "RunTime",
        "FinishTime",

        // 외부 네트워크
        "Insung1SeqNo",
        "Insung2SeqNo",
        "Cargo24SeqNo",
        "OnecallSeqNo",

        // Flag 필드
        "MovilityFlag",
        "CarWeightFlag",
        "DeliverFlag",

        // 메모
        "DeliverMemo",
        "CallMemo",

        // 기타
        "Share",
    };
    #endregion

    #region 계산 필드 (DB에 없고 UI에서 계산)
    /// <summary>
    /// UI에서 계산되는 필드들 (DB 직접 매핑 없음)
    /// </summary>
    public static readonly HashSet<string> CalculatedFields = new()
    {
        "FeeNet",  // FeeTotal - FeeCommi
    };
    #endregion

    #region 검증 메서드
    /// <summary>
    /// TbOrder와 ViewModel 간 매핑 검증
    /// 누락된 속성이 있으면 오류 메시지 반환
    /// </summary>
    public static List<string> ValidateMapping(Type dbType, Type vmType)
    {
        var errors = new List<string>();

        var dbProps = dbType.GetProperties().Select(p => p.Name).ToHashSet();
        var vmProps = vmType.GetProperties().Select(p => p.Name).ToHashSet();

        // VM에 있는데 DB에 매핑되지 않은 속성 검출
        foreach (var vmProp in vmProps)
        {
            // 계산 필드는 제외
            if (CalculatedFields.Contains(vmProp))
                continue;

            // 동일 이름이면 OK
            if (SameNameFields.Contains(vmProp) && dbProps.Contains(vmProp))
                continue;

            // 매핑된 DB 필드가 있으면 OK
            if (UiToDb.TryGetValue(vmProp, out var dbProp) && dbProps.Contains(dbProp))
                continue;

            // 동일 이름으로 DB에 존재하면 OK (SameNameFields에 미등록)
            if (dbProps.Contains(vmProp))
            {
                errors.Add($"[경고] VM.{vmProp}는 DB에 존재하지만 SameNameFields에 미등록");
                continue;
            }

            errors.Add($"[오류] VM.{vmProp}에 대응하는 DB 속성 없음");
        }

        // DB에 있는데 VM에 매핑되지 않은 속성 검출 (참고용)
        foreach (var dbProp in dbProps)
        {
            if (SameNameFields.Contains(dbProp) && vmProps.Contains(dbProp))
                continue;

            if (DbToUi.TryGetValue(dbProp, out var vmProp) && vmProps.Contains(vmProp))
                continue;

            if (vmProps.Contains(dbProp))
                continue;

            // DB에만 있는 속성 (UI에서 사용 안 함) - 경고 레벨
            // errors.Add($"[참고] DB.{dbProp}는 VM에 매핑 없음");
        }

        return errors;
    }

    /// <summary>
    /// 검증 실행 및 결과 로그 출력
    /// </summary>
    public static bool ValidateAndLog(Type dbType, Type vmType, Action<string> logger = null)
    {
        var errors = ValidateMapping(dbType, vmType);

        if (errors.Count == 0)
        {
            logger?.Invoke($"[TbOrderFieldMap] {dbType.Name} ↔ {vmType.Name} 매핑 검증 통과");
            return true;
        }

        foreach (var error in errors)
        {
            logger?.Invoke($"[TbOrderFieldMap] {error}");
        }

        return false;
    }
    #endregion

    #region 헬퍼 메서드
    /// <summary>
    /// DB 속성명 → UI 속성명 변환
    /// </summary>
    public static string ToUiName(string dbName)
    {
        return DbToUi.TryGetValue(dbName, out var uiName) ? uiName : dbName;
    }

    /// <summary>
    /// UI 속성명 → DB 속성명 변환
    /// </summary>
    public static string ToDbName(string uiName)
    {
        return UiToDb.TryGetValue(uiName, out var dbName) ? dbName : uiName;
    }

    /// <summary>
    /// 해당 UI 필드가 계산 필드인지 확인
    /// </summary>
    public static bool IsCalculatedField(string uiName)
    {
        return CalculatedFields.Contains(uiName);
    }

    /// <summary>
    /// 모든 UI 필드명 반환 (매핑 + 동일이름 + 계산)
    /// </summary>
    public static IEnumerable<string> GetAllUiFieldNames()
    {
        return DbToUi.Values
            .Concat(SameNameFields)
            .Concat(CalculatedFields);
    }

    /// <summary>
    /// 모든 DB 필드명 반환 (매핑 + 동일이름)
    /// </summary>
    public static IEnumerable<string> GetAllDbFieldNames()
    {
        return DbToUi.Keys
            .Concat(SameNameFields);
    }
    #endregion
}
