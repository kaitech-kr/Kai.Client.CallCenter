using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Services;

namespace Kai.Client.CallCenter.Classes.Class_Master;

// 자동배차 주문 정보 클래스
public class AutoAllocModel
{
    #region Properties

    // 주문 상태 플래그
    // - Created: 새로 생성된 주문 (배차 대기)
    // - Existed_NonSeqno: 기존 주문 (Seqno 없음)
    // - Existed_WithSeqno: 기존 주문 (Seqno 있음, 배차 완료)
    // - Updated_Assume: 업데이트된 주문 (재배차 필요)
    // - NotChanged: 처리 완료 (다음 루프에서 스킵)
    // - CompletedExternal: 외부에서 완료됨 (다른 외주사에서 처리완료, 취소 필요)
    public PostgService_Common_OrderState StateFlag { get; set; }

    // 현재(새) 주문 정보
    // - Created: NewOrder만 사용 (OldOrder는 null)
    // - Updated: NewOrder = 업데이트된 정보
    public TbOrder NewOrder { get; set; }

    // 이전 주문 정보 (업데이트 비교용)
    // - Created: null
    // - Updated: OldOrder = 업데이트 이전 정보
    public TbOrder? OldOrder { get; set; }

    // 주문 키코드 (편의 속성)
    // - NewOrder.KeyCode를 직접 반환
    public long KeyCode => NewOrder?.KeyCode ?? 0;

    // 인성 "운행" 상태 진입 시간 (40초 타이머용)
    // - null: 운행 상태 아님 또는 40초 경과됨
    // - DateTime: 운행 진입 시간 (40초 경과 체크용)
    public DateTime? RunStartTime { get; set; } = null;

    // 기사 전화번호 (운행 진입 시 DG에서 OFR로 획득)
    // - 40초 타이머 경과 후 Kai DB 업데이트에 사용
    public string? DriverPhone { get; set; } = null;

    #endregion

    #region Constructors

    // 새로 생성된 주문용 생성자
    // newOrder: SignalR OnOrderCreated에서 받은 주문
    public AutoAllocModel(TbOrder newOrder)
    {
        StateFlag = PostgService_Common_OrderState.Created;
        NewOrder = newOrder;
        OldOrder = null;
    }

    // 업데이트된 주문용 생성자
    // oldOrder: 기존 주문 정보
    // newOrder: 업데이트된 주문 정보
    public AutoAllocModel(TbOrder oldOrder, TbOrder newOrder)
    {
        StateFlag = PostgService_Common_OrderState.Updated_Assume;
        OldOrder = oldOrder;
        NewOrder = newOrder;
    }

    // 기존 주문 로드용 생성자 (앱 시작 시 DB에서 로드)
    // stateFlag: 상태 플래그 (Existed_NonSeqno 또는 Existed_WithSeqno)
    // newOrder: 로드된 주문 정보
    public AutoAllocModel(PostgService_Common_OrderState stateFlag, TbOrder newOrder)
    {
        StateFlag = stateFlag;
        NewOrder = newOrder;
        OldOrder = null;
    }

    // 복사 생성자 (Clone용)
    private AutoAllocModel(AutoAllocModel source)
    {
        StateFlag = source.StateFlag;
        NewOrder = source.NewOrder;
        OldOrder = source.OldOrder;
        RunStartTime = source.RunStartTime;
        DriverPhone = source.DriverPhone;
    }

    #endregion

    #region Methods

    // 깊은 복사 (리스트 작업용)
    // - 같은 TbOrder 인스턴스를 참조 (TbOrder 자체는 복사하지 않음)
    public AutoAllocModel Clone()
    {
        return new AutoAllocModel(this);
    }

    // 디버그용 문자열
    public override string ToString()
    {
        return $"[AutoAlloc] KeyCode={KeyCode}, State={StateFlag}";
    }

    #endregion
}


