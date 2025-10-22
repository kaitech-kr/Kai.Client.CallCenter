using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Services;

namespace Kai.Client.CallCenter.Classes.Class_Master;

/// <summary>
/// 자동배차 주문 정보 클래스
/// - 메모리 기반 상태 추적 (DB 직접 쿼리 방식 대체)
/// - SignalR 실시간 업데이트와 연동
/// - ExternalAppController의 Dictionary 리스트에서 사용
/// </summary>
public class AutoAlloc
{
    #region Properties

    /// <summary>
    /// 주문 상태 플래그
    /// - Created: 새로 생성된 주문 (배차 대기)
    /// - Existed_NonSeqno: 기존 주문 (Seqno 없음)
    /// - Existed_WithSeqno: 기존 주문 (Seqno 있음, 배차 완료)
    /// - Updated_Assume: 업데이트된 주문 (재배차 필요)
    /// - NotChanged: 처리 완료 (다음 루프에서 스킵)
    /// - Change_ToCancel_DoDelete: 취소된 주문 (리스트에서 제거 예정)
    /// </summary>
    public PostgService_Common_OrderState StateFlag { get; set; }

    /// <summary>
    /// 현재(새) 주문 정보
    /// - Created: NewOrder만 사용 (OldOrder는 null)
    /// - Updated: NewOrder = 업데이트된 정보
    /// </summary>
    public TbOrder NewOrder { get; set; }

    /// <summary>
    /// 이전 주문 정보 (업데이트 비교용)
    /// - Created: null
    /// - Updated: OldOrder = 업데이트 이전 정보
    /// </summary>
    public TbOrder? OldOrder { get; set; }

    /// <summary>
    /// 주문 키코드 (편의 속성)
    /// - NewOrder.KeyCode를 직접 반환
    /// </summary>
    public long KeyCode => NewOrder?.KeyCode ?? 0;

    #endregion

    #region Constructors

    /// <summary>
    /// 새로 생성된 주문용 생성자
    /// </summary>
    /// <param name="newOrder">SignalR OnOrderCreated에서 받은 주문</param>
    public AutoAlloc(TbOrder newOrder)
    {
        StateFlag = PostgService_Common_OrderState.Created;
        NewOrder = newOrder;
        OldOrder = null;
    }

    /// <summary>
    /// 업데이트된 주문용 생성자
    /// </summary>
    /// <param name="oldOrder">기존 주문 정보</param>
    /// <param name="newOrder">업데이트된 주문 정보</param>
    public AutoAlloc(TbOrder oldOrder, TbOrder newOrder)
    {
        StateFlag = PostgService_Common_OrderState.Updated_Assume;
        OldOrder = oldOrder;
        NewOrder = newOrder;
    }

    /// <summary>
    /// 복사 생성자 (Clone용)
    /// </summary>
    private AutoAlloc(AutoAlloc source)
    {
        StateFlag = source.StateFlag;
        NewOrder = source.NewOrder;
        OldOrder = source.OldOrder;
    }

    #endregion

    #region Methods

    /// <summary>
    /// 깊은 복사 (리스트 작업용)
    /// - 같은 TbOrder 인스턴스를 참조 (TbOrder 자체는 복사하지 않음)
    /// </summary>
    public AutoAlloc Clone()
    {
        return new AutoAlloc(this);
    }

    /// <summary>
    /// 디버그용 문자열
    /// </summary>
    public override string ToString()
    {
        return $"[AutoAlloc] KeyCode={KeyCode}, State={StateFlag}";
    }

    #endregion
}
