# 자동배차 시스템 - Queue 기반 구현 완료

**최종 업데이트**: 2025-10-23
**작성자**: Claude Code
**상태**: ✅ Phase 1 완료 (Queue 기반 리팩토링)

---

## 📋 목차

1. [현재 완료 상태](#1-현재-완료-상태)
2. [시스템 아키텍처](#2-시스템-아키텍처)
3. [Queue 구조 설계](#3-queue-구조-설계)
4. [주문 분류 로직](#4-주문-분류-로직)
5. [데이터 흐름](#5-데이터-흐름)
6. [구현 상세](#6-구현-상세)
7. [다음 작업](#7-다음-작업)

---

## 1. 현재 완료 상태

### ✅ 완료된 작업

#### Phase 1: Queue 기반 인프라 구축 (완료)
- [x] AutoAllocQueueManager.cs 클래스 생성
  - 4개 앱별 개별 Queue (_ordersInsung1, _ordersInsung2, _ordersCargo24, _ordersOnecall)
  - LoadExistingOrders() - 앱 시작 시 기존 주문 적재
  - Enqueue(), DequeueAllToList(), ReEnqueue() 메서드
  - 큐 상태 조회 메서드 (Count, PrintStatus)

- [x] ExternalAppController 확장
  - QueueManager static 속성 추가
  - ClassifyAndEnqueueOrder() - 차량 타입별 자동 분류
  - LoadExistingOrders() 구현
  - AddNewOrder() 구현 (SignalR 연동)

- [x] 주문 분류 로직 구현
  - CarType == "오토" → 인성1, 인성2만
  - CarType == "트럭" && CarWeight in ["1t", "1.4t"] → 모든 앱
  - CarType == "트럭" && 기타 → 화물24시, 원콜만
  - 신용업체 상호 제외 로직 유지

- [x] NwInsung01.AutoAllocAsync Region 2, 6 수정
  - Region 2: DequeueAllToList()로 큐에서 주문 가져오기
  - Region 6: ReEnqueue()로 처리완료 주문 재적재

- [x] 빌드 성공 (오류 0개)

### 📊 주요 개선 사항

**Before (List 기반)**:
```csharp
// ❌ 복잡한 3중 복사
List<AutoAlloc> listOrg = AutoAllocCtrl.listForInsung01;
var listInsung = new List<AutoAlloc>(listOrg);  // 1차 복사
listOrg.Clear();

var listCreated = listInsung.Where(...).Select(item => item.Clone()).ToList();  // 2차 복사

// ❌ 역순 순회 + 인덱스 관리
for (int i = listCreated.Count - 1; i >= 0; i--)
{
    listCreated.RemoveAt(i);  // O(n) 비용
}
```

**After (Queue 기반)**:
```csharp
// ✅ 큐에서 바로 List로 변환 (O(1) Dequeue)
List<AutoAlloc> listFromController = QueueManager.DequeueAllToList(INSUNG1);

// ✅ 처리 후 재적재 (O(1) Enqueue)
foreach (var item in listProcessed)
{
    QueueManager.ReEnqueue(item, INSUNG1);
}
```

**성능 향상**:
- Dequeue: O(1) (vs List.RemoveAt: O(n))
- 메모리 복사 감소
- 코드 가독성 향상
- 인덱스 관리 불필요

---

## 2. 시스템 아키텍처

### 2.1 전체 구조

```
┌─────────────────────────────────────────────────────────┐
│                   Kai 시스템 (우리)                       │
├─────────────────────────────────────────────────────────┤
│                                                           │
│  [앱 시작] → DB에서 오늘 주문 로드                         │
│     ↓                                                     │
│  Order_StatusPage.SearchTodayOrdersAsync()               │
│     └─ ExternalAppController.LoadExistingOrders()       │
│            └─ ClassifyAndEnqueueOrder()                  │
│                   └─ QueueManager.Enqueue()              │
│                                                           │
│  [SignalR 이벤트 - 실시간]                                │
│     ↓                                                     │
│  SrReport_Order_InsertedRowAsync_Today                   │
│     └─ ExternalAppController.AddNewOrder()               │
│            └─ ClassifyAndEnqueueOrder()                  │
│                   └─ QueueManager.Enqueue()              │
│                                                           │
└────────────────┬────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────┐
│           AutoAllocQueueManager (4개 큐)                 │
├─────────────────────────────────────────────────────────┤
│                                                           │
│  ┌──────────────────────────────────────────┐           │
│  │ Queue<AutoAlloc> _ordersInsung1          │           │
│  │ Queue<AutoAlloc> _ordersInsung2          │           │
│  │ Queue<AutoAlloc> _ordersCargo24          │           │
│  │ Queue<AutoAlloc> _ordersOnecall          │           │
│  └──────────────────────────────────────────┘           │
│                                                           │
│  Methods:                                                 │
│  - Enqueue(order, networkName)                           │
│  - DequeueAllToList(networkName) → List<AutoAlloc>      │
│  - ReEnqueue(order, networkName)                         │
│  - LoadExistingOrders(orders, networkName)              │
│                                                           │
└────────────────┬────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────┐
│         자동배차 루프 (ExternalAppController)             │
├─────────────────────────────────────────────────────────┤
│                                                           │
│  매 5초마다 AutoAllocLoopAsync() 실행                     │
│     └─ foreach (IExternalApp app in m_ListApps)         │
│            └─ app.AutoAllocAsync()                       │
│                   ├─ NwInsung01.AutoAllocAsync()        │
│                   └─ NwInsung02.AutoAllocAsync()        │
│                                                           │
└────────────────┬────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────┐
│          인성1/2 앱 (외부 프로그램)                        │
├─────────────────────────────────────────────────────────┤
│                                                           │
│  접수등록 페이지 데이터그리드:                             │
│  - 신규오더 (SeqNo 없음) → 최상단                         │
│  - 기존오더 (SeqNo 있음) → 페이징 필요                    │
│                                                           │
└─────────────────────────────────────────────────────────┘
```

### 2.2 핵심 개념

**FIFO (First In First Out)**:
```
[출구] ← [주문3] [주문2] [주문1] ← [입구]
         Dequeue()          Enqueue()
```

**Queue 장점**:
- ✅ O(1) 추가/제거 보장
- ✅ 인덱스 관리 불필요
- ✅ 재시도 간단 (다시 Enqueue)
- ✅ 복사 불필요

---

## 3. Queue 구조 설계

### 3.1 4개 앱별 개별 Queue

```csharp
public class AutoAllocQueueManager
{
    // 인성1 주문 큐
    private Queue<AutoAlloc> _ordersInsung1 = new();

    // 인성2 주문 큐
    private Queue<AutoAlloc> _ordersInsung2 = new();

    // 화물24시 주문 큐
    private Queue<AutoAlloc> _ordersCargo24 = new();

    // 원콜 주문 큐
    private Queue<AutoAlloc> _ordersOnecall = new();
}
```

**왜 4개로 분리?**
1. 각 외부앱이 독립적으로 처리
2. 한 앱이 느려도 다른 앱 영향 없음
3. 앱별 큐 크기 모니터링 가능
4. 앱별 우선순위 조정 가능

### 3.2 Queue 사용 패턴

**입구 (Enqueue)**:
```csharp
// 1. 앱 시작 시 - 기존 주문 적재
QueueManager.LoadExistingOrders(orders, "Insung1");

// 2. SignalR 이벤트 - 신규 주문
QueueManager.Enqueue(order, "Insung1");

// 3. 처리 완료 후 - 재적재 (StateFlag = NotChanged)
QueueManager.ReEnqueue(order, "Insung1");
```

**출구 (Dequeue)**:
```csharp
// 큐에서 모든 주문을 List로 변환 (기존 로직 호환)
List<AutoAlloc> listFromController = QueueManager.DequeueAllToList("Insung1");

// 기존 Where, OrderBy, Select 로직 그대로 사용 가능
var listCreated = listFromController.Where(...).ToList();
```

---

## 4. 주문 분류 로직

### 4.1 분류 기준 (절대 규칙)

**Step 1: 차량 타입 판단**

| 조건 | 판단 결과 |
|------|----------|
| `CarType == "오토"` | 오토바이 |
| `CarType == "트럭" && CarWeight in ["1t", "1.4t"]` | 1.4톤 이하 트럭 |
| `CarType == "트럭" && 기타` | 1.4톤 초과 트럭 |

**Step 2: 외부앱별 분배**

| 차량 타입 | 인성1 | 인성2 | 화물24시 | 원콜 |
|-----------|-------|-------|----------|------|
| **오토바이** | ✅ (필터링) | ✅ (필터링) | ❌ | ❌ |
| **1.4톤 이하** | ✅ (필터링) | ✅ (필터링) | ✅ | ✅ |
| **1.4톤 초과** | ❌ | ❌ | ✅ | ✅ |

**신용업체 필터링 (인성1, 인성2만)**:
- 인성1: `!(CallCustFrom == "인성2" && FeeType == "신용")`
- 인성2: `!(CallCustFrom == "인성1" && FeeType == "신용")`
- 화물24시, 원콜: 필터링 없음

### 4.2 구체적인 예시

**예시 1: 오토바이 (일반)**
```
CarType: "오토", CallCustFrom: "직접접수", FeeType: "현금"
→ 인성1 ✅, 인성2 ✅, 화물24시 ❌, 원콜 ❌
```

**예시 2: 오토바이 (인성2 신용업체)**
```
CarType: "오토", CallCustFrom: "인성2", FeeType: "신용"
→ 인성1 ❌, 인성2 ✅, 화물24시 ❌, 원콜 ❌
```

**예시 3: 1톤 트럭 (일반)**
```
CarType: "트럭", CarWeight: "1t", CallCustFrom: "직접접수"
→ 인성1 ✅, 인성2 ✅, 화물24시 ✅, 원콜 ✅ (모두!)
```

**예시 4: 1.4톤 트럭 (인성1 신용업체)**
```
CarType: "트럭", CarWeight: "1.4t", CallCustFrom: "인성1", FeeType: "신용"
→ 인성1 ✅, 인성2 ❌, 화물24시 ✅, 원콜 ✅
```

**예시 5: 2.5톤 트럭**
```
CarType: "트럭", CarWeight: "2.5t"
→ 인성1 ❌, 인성2 ❌, 화물24시 ✅, 원콜 ✅
```

### 4.3 구현 코드

**ExternalAppController.cs:263-311**
```csharp
private void ClassifyAndEnqueueOrder(TbOrder order, bool isNewOrder)
{
    // Step 1: 차량 타입 판단
    bool isMotorcycle = order.CarType == "오토";
    bool isSmallTruck = order.CarType == "트럭" &&
                        (order.CarWeight == "1t" || order.CarWeight == "1.4t");
    bool isLargeTruck = order.CarType == "트럭" &&
                        order.CarWeight != "1t" &&
                        order.CarWeight != "1.4t";

    // Step 2-1: 오토바이 또는 1.4톤 이하 → 인성1, 인성2
    if (isMotorcycle || isSmallTruck)
    {
        // 인성1: 인성2 신용업체 제외
        if (!(order.CallCustFrom == INSUNG2 && order.FeeType == "신용"))
            EnqueueToApp(order, INSUNG1, isNewOrder);

        // 인성2: 인성1 신용업체 제외
        if (!(order.CallCustFrom == INSUNG1 && order.FeeType == "신용"))
            EnqueueToApp(order, INSUNG2, isNewOrder);
    }

    // Step 2-2: 1.4톤 이하 또는 초과 → 화물24시, 원콜
    if (isSmallTruck || isLargeTruck)
    {
        EnqueueToApp(order, CARGO24, isNewOrder);
        EnqueueToApp(order, ONECALL, isNewOrder);
    }
}
```

---

## 5. 데이터 흐름

### 5.1 앱 시작 시 (기존 주문 로드)

```
[MainWnd 초기화]
    ↓
Order_StatusPage.SearchTodayOrdersAsync()
    ↓
DB에서 오늘 주문 조회 (TbOrder 리스트)
    ↓
VsOrder_StatusPage.s_listTbOrderToday에 저장
    ↓
InitializeAfterFirstSearch()
    └─ MakeExistedAutoAlloc()
           ↓
ExternalAppController.LoadExistingOrders(orders)
    ↓
    ├─ foreach (order in orders)
    │      └─ ClassifyAndEnqueueOrder(order, isNewOrder: false)
    │             ├─ 차량 타입 판단
    │             ├─ 신용업체 필터링
    │             └─ 각 앱 큐에 Enqueue
    │
    └─ QueueManager.LoadExistingOrders()
           ├─ SeqNo 유무 확인
           ├─ StateFlag 결정 (Existed_WithSeqno / Existed_NonSeqno)
           └─ 큐에 적재

결과:
- _ordersInsung1: 15개 (Existed_NonSeqno: 3, Existed_WithSeqno: 12)
- _ordersInsung2: 8개
- _ordersCargo24: 20개
- _ordersOnecall: 18개
```

### 5.2 실행 중 (SignalR 신규 주문)

```
[SignalR 이벤트 발생]
    ↓
SrReport_Order_InsertedRowAsync_Today(newOrder)
    ↓
ExternalAppController.AddNewOrder(order)
    ↓
ClassifyAndEnqueueOrder(order, isNewOrder: true)
    ├─ StateFlag = Created
    └─ 각 앱 큐에 Enqueue

결과:
- _ordersInsung1에 1개 추가 (Created)
- _ordersInsung2에 1개 추가 (Created)
```

### 5.3 자동배차 루프 (매 5초)

```
[AutoAllocLoopAsync 시작]
    ↓
foreach (app in m_ListApps)
    ↓
NwInsung01.AutoAllocAsync(lAllocCount, ctrl)
    ↓
┌─────────────────────────────────────────────────┐
│ Region 2: Queue → List 변환                      │
└─────────────────────────────────────────────────┘
    ↓
List<AutoAlloc> list = QueueManager.DequeueAllToList("Insung1")
    ↓
큐 비어짐 → List로 변환 (15개)
    ↓
listCreated = list.Where(Created | Existed_NonSeqno)  // 3개
listEtcGroup = list.Where(기타)                       // 12개
    ↓
┌─────────────────────────────────────────────────┐
│ Region 4: listCreated 처리 (신규오더)             │
└─────────────────────────────────────────────────┘
    ↓
for (listCreated 역순)
    ├─ CheckIsOrderAsync_AssumeKaiNewOrder()
    ├─ Success → listProcessed에 추가
    └─ Fail → 에러 처리
    ↓
┌─────────────────────────────────────────────────┐
│ Region 5: listEtcGroup 처리 (기존오더)            │
└─────────────────────────────────────────────────┘
    ↓
Click조회버튼Async()
    ↓
for (listEtcGroup 역순)
    ├─ FindDatagridPageNIndex(SeqNo)
    ├─ StateFlag별 처리
    ├─ Success → listProcessed에 추가
    └─ Delete → 제거
    ↓
┌─────────────────────────────────────────────────┐
│ Region 6: 재적재                                  │
└─────────────────────────────────────────────────┘
    ↓
foreach (item in listProcessed)
    └─ QueueManager.ReEnqueue(item, "Insung1")
           └─ StateFlag = NotChanged로 변경

결과:
- _ordersInsung1: 12개 (NotChanged: 12)
```

---

## 6. 구현 상세

### 6.1 AutoAllocQueueManager.cs

**위치**: `Classes/Class_Master/AutoAllocQueueManager.cs`

**주요 메서드**:

```csharp
/// <summary>
/// 주문을 큐에 추가
/// </summary>
public void Enqueue(AutoAlloc order, string networkName)
{
    var queue = GetQueue(networkName);
    queue.Enqueue(order);
}

/// <summary>
/// 큐에서 모든 주문을 꺼내서 List로 반환
/// </summary>
public List<AutoAlloc> DequeueAllToList(string networkName)
{
    var queue = GetQueue(networkName);
    var list = new List<AutoAlloc>();

    while (queue.Count > 0)
    {
        list.Add(queue.Dequeue());
    }

    return list;
}

/// <summary>
/// 처리 완료 후 큐에 재적재
/// </summary>
public void ReEnqueue(AutoAlloc order, string networkName)
{
    // StateFlag를 NotChanged로 변경
    order.StateFlag = PostgService_Common_OrderState.NotChanged;

    var queue = GetQueue(networkName);
    queue.Enqueue(order);
}

/// <summary>
/// 앱 시작 시 기존 주문 목록을 큐에 적재
/// </summary>
public void LoadExistingOrders(List<TbOrder> orders, string networkName)
{
    foreach (var order in orders)
    {
        string seqNo = GetSeqNoByNetwork(order, networkName);
        bool hasSeqNo = !string.IsNullOrEmpty(seqNo);

        var stateFlag = hasSeqNo
            ? PostgService_Common_OrderState.Existed_WithSeqno
            : PostgService_Common_OrderState.Existed_NonSeqno;

        var autoAlloc = new AutoAlloc(stateFlag, order);
        GetQueue(networkName).Enqueue(autoAlloc);
    }
}
```

### 6.2 ExternalAppController.cs

**위치**: `Classes/Class_Master/ExternalAppController.cs`

**주요 추가/수정**:

```csharp
// Static QueueManager
public static AutoAllocQueueManager QueueManager { get; private set; }
    = new AutoAllocQueueManager();

// 기존 주문 로드
public void LoadExistingOrders(List<TbOrder> orders)
{
    foreach (var order in orders)
    {
        ClassifyAndEnqueueOrder(order, isNewOrder: false);
    }
}

// 신규 주문 추가 (SignalR 연동)
public void AddNewOrder(TbOrder order)
{
    ClassifyAndEnqueueOrder(order, isNewOrder: true);
}

// 주문 분류 및 큐 적재
private void ClassifyAndEnqueueOrder(TbOrder order, bool isNewOrder)
{
    // 차량 타입 판단 → 앱별 분배 → 신용업체 필터링
    // (위 섹션 참조)
}

private void EnqueueToApp(TbOrder order, string networkName, bool isNewOrder)
{
    var stateFlag = isNewOrder
        ? PostgService_Common_OrderState.Created
        : DetermineExistingStateFlag(order, networkName);

    var autoAlloc = new AutoAlloc(stateFlag, order);
    QueueManager.Enqueue(autoAlloc, networkName);
}
```

### 6.3 NwInsung01.AutoAllocAsync

**위치**: `Networks/NwInsung01.cs`

**Region 2 수정**:
```csharp
#region 2. Local Variables 초기화
// 컨트롤러 큐에서 주문 리스트 가져오기 (DequeueAllToList로 큐 비우기)
List<AutoAlloc> listFromController = ExternalAppController.QueueManager.DequeueAllToList(StdConst_Network.INSUNG1);

// 작업잔량 파악 리스트 (원본 복사)
var listInsung = new List<AutoAlloc>(listFromController);
// 큐에서 이미 꺼냈으므로 Clear 불필요

// 처리 완료된 항목을 담을 리스트 (Region 4, 5에서 사용)
var listProcessed = new List<AutoAlloc>();

// 이후 기존 Where, OrderBy 로직 그대로...
#endregion
```

**Region 6 수정**:
```csharp
#region 6. 처리 완료된 항목을 큐에 재적재
if (listProcessed.Count > 0)
{
    foreach (var item in listProcessed)
    {
        ExternalAppController.QueueManager.ReEnqueue(item, StdConst_Network.INSUNG1);
    }
    Debug.WriteLine($"[{APP_NAME}] 처리 완료된 항목 {listProcessed.Count}개를 큐에 재적재");
}
#endregion
```

---

## 7. 다음 작업

### 7.1 즉시 필요한 작업

#### Region 4, 5 Helper 메서드 구현
```
[ ] CheckIsOrderAsync_AssumeKaiNewOrder() - 신규 주문 처리
[ ] Click조회버튼Async() - 조회 버튼 클릭
[ ] ClickEmptyRowAsync() - Empty Row 클릭
[ ] FindDatagridPageNIndex() - SeqNo로 데이터그리드 검색
[ ] CheckIsOrderAsync_KaiSameInsungIfChanged() - 변경 여부 체크
[ ] CheckIsOrderAsync_AssumeKaiUpdated() - 업데이트 처리
```

#### 인성2, 화물24시, 원콜 확장
```
[ ] NwInsung02.AutoAllocAsync 동일하게 수정
[ ] NwCargo24 구현 (인성과 다른 UI 구조)
[ ] NwOnecall 구현 (인성과 다른 UI 구조)
```

### 7.2 향후 개선 사항

#### SignalR 업데이트 이벤트 완전 연동
```
[ ] SrReport_Order_UpdatedRowAsync_Today 구현
[ ] ExternalAppController.UpdateOrder() 완성
[ ] 큐에서 기존 주문 찾기/수정 로직
```

#### 완전 Queue 기반 리팩토링 (Option B)
```
[ ] Region 4, 5를 List 없이 Queue로만 처리
[ ] Where, OrderBy 로직을 Queue 처리로 전환
[ ] 메모리 효율 극대화
```

#### 모니터링 및 로깅
```
[ ] 큐 크기 실시간 모니터링 UI
[ ] 처리 속도 측정 및 로깅
[ ] 병목 지점 분석
```

### 7.3 테스트 계획

```
[ ] TC-1: 앱 시작 시 기존 주문 2개 로드 확인
[ ] TC-2: SignalR 신규 주문 1개 추가 확인
[ ] TC-3: AutoAllocAsync 1회 실행 - 큐 순환 확인
[ ] TC-4: 처리 완료 후 재적재 확인 (NotChanged)
[ ] TC-5: 4개 앱 동시 처리 확인
[ ] TC-6: 신용업체 필터링 정확도 확인
```

---

## 8. 주요 파일 위치

```
Kai.Client.CallCenter/
├─ Classes/Class_Master/
│  ├─ AutoAllocQueueManager.cs          ✅ 큐 관리자
│  ├─ AutoAlloc.cs                      ✅ 주문 데이터 클래스
│  └─ ExternalAppController.cs          ✅ 외부앱 컨트롤러
│
├─ Networks/
│  ├─ NwInsung01.cs                     ✅ 인성1 자동배차
│  ├─ NwInsung02.cs                     🔄 인성2 자동배차 (예정)
│  └─ NwInsungs/
│     └─ InsungsAct_RcptRegPage.cs      🔄 Helper 메서드 구현 필요
│
└─ Pages/
   └─ Order_StatusPage.xaml.cs          ✅ 주문 조회 및 로드
```

**범례**:
- ✅ 완료
- 🔄 진행 중 또는 예정
- ❌ 미작업

---

## 9. FAQ

### Q1: 왜 4개 큐로 분리했나요?
**A**: 각 외부앱이 독립적으로 처리되어야 하며, 한 앱의 처리 속도가 다른 앱에 영향을 주지 않도록 하기 위함입니다.

### Q2: 왜 Queue를 List로 변환하나요?
**A**: 기존 검증된 로직(Where, OrderBy, Select)을 최대한 재사용하고, 리팩토링 위험을 최소화하기 위한 Hybrid 접근 방식입니다. 향후 완전 Queue 기반으로 전환 가능합니다.

### Q3: ReEnqueue 시 왜 StateFlag를 NotChanged로 바꾸나요?
**A**: 처리 완료된 주문은 다음 루프에서 빠르게 스킵하기 위함입니다. NotChanged 주문은 변경 여부만 체크합니다.

### Q4: 신규 주문(Created)과 기존 주문(Existed_NonSeqno)의 차이는?
**A**:
- Created: SignalR로 방금 들어온 신규 주문
- Existed_NonSeqno: 앱 시작 시 DB에서 로드한 주문 중 SeqNo가 없는 주문
- 둘 다 데이터그리드 최상단에 위치하여 동일하게 처리됩니다.

### Q5: 빌드는 성공했는데 실제로 동작하나요?
**A**: Region 4, 5의 Helper 메서드들이 TODO 상태이므로 실제 주문 처리는 아직 동작하지 않습니다. 다음 작업에서 Helper 메서드들을 구현해야 합니다.

---

**작성 완료**: 2025-10-23
**다음 세션**: Helper 메서드 구현 (Region 4, 5)
