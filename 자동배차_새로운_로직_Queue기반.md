# 자동배차 새로운 로직 - Queue 기반 리팩토링

**작성일**: 2025-10-23
**목표**: List 기반 복잡한 로직을 Queue 기반으로 단순화

---

## 📋 목차

1. [현재 문제점](#1-현재-문제점)
2. [새로운 아키텍처](#2-새로운-아키텍처)
3. [Queue 기반 설계](#3-queue-기반-설계)
4. [데이터 흐름](#4-데이터-흐름)
5. [단계별 구현 계획](#5-단계별-구현-계획)
6. [구현 상세](#6-구현-상세)
7. [테스트 계획](#7-테스트-계획)

---

## 1. 현재 문제점

### 🔴 기존 로직의 복잡성

```csharp
// 1. 3중 복사
List<AutoAlloc> listOrg = AutoAllocCtrl.listForInsung01;
var listInsung = new List<AutoAlloc>(listOrg);  // 1차 복사
listOrg.Clear();

var listCreated = listInsung
    .Where(...)
    .OrderByDescending(item => item.NewOrder.Insung1)  // 정렬
    .Select(item => AutoAllocCtrl.CopyItemFromOrg(item))  // 2차 복사
    .ToList();

// 2. 역순 순회 + 인덱스 관리
for (int i = listCreated.Count; i > 0; i--)
{
    int index = i - 1;  // 인덱스 변환
    if (index < 0) break;

    // 처리
    listCreated.RemoveAt(index);  // O(n) 위험
}

// 3. listOrg에 다시 추가
listOrg.AddRange(processed);
```

**문제점:**
- ❌ 3중 복사로 메모리 낭비
- ❌ 역순 순회 + 인덱스 관리 복잡
- ❌ RemoveAt(index)의 O(n) 비용
- ❌ 30줄 이상의 복잡한 코드
- ❌ 유지보수 어려움

---

## 2. 새로운 아키텍처

### ✅ Queue 기반 단순화

```
┌─────────────────────────────────────────────────────────┐
│                   SignalR 이벤트                         │
│  - SrReport_Order_InsertedRowAsync_Today (신규)         │
│  - SrReport_Order_UpdatedRowAsync_Today (변경)          │
└────────────────┬────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────┐
│              ExternalAppController                       │
│  - AddNewOrder(order)           ← 신규 주문             │
│  - UpdateOrder(order)           ← 주문 변경             │
│  - LoadExistingOrders(orders)  ← 앱 시작 시            │
└────────────────┬────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────┐
│           AutoAllocQueueManager                          │
│  ┌─────────────────────────────────────────────┐        │
│  │  Queue<AutoAlloc> _newOrders                │        │
│  │  Queue<AutoAlloc> _existingOrders           │        │
│  └─────────────────────────────────────────────┘        │
│                                                           │
│  - AddNewOrder(order)                                    │
│  - LoadExistingOrders(orders)                           │
│  - ProcessNewOrdersAsync()                              │
│  - ProcessExistingOrdersAsync()                         │
└────────────────┬────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────┐
│              NwInsung01/02                               │
│  - AutoAllocAsync()                                      │
│    ├─ ProcessNewOrdersAsync()                           │
│    └─ ProcessExistingOrdersAsync()                      │
└─────────────────────────────────────────────────────────┘
```

---

## 3. Queue 기반 설계

### 📦 Queue 특징

**FIFO (First In First Out)**
```
[출구] ← [주문3] [주문2] [주문1] ← [입구]
         Dequeue()          Enqueue()
```

**장점:**
- ✅ O(1) 추가/제거 보장
- ✅ 인덱스 관리 불필요
- ✅ 정렬 불필요 (처리 순서 무관)
- ✅ 재시도 간단 (다시 Enqueue)
- ✅ 복사 불필요

### 🎯 2개 큐 분리

```csharp
// 신규 주문 큐 (우선 처리)
Queue<AutoAlloc> _newOrders;

// 기존 주문 큐 (순차 처리)
Queue<AutoAlloc> _existingOrders;
```

**분리 이유:**
1. 신규 주문 우선 처리 (고객 대기 시간 최소화)
2. 기존 주문은 천천히 처리 (이미 등록된 주문)
3. 각각 독립적인 처리 로직

---

## 4. 데이터 흐름

### 📊 전체 흐름

```
┌─────────────────────────────────────────────────────────┐
│                    앱 시작                               │
└────────────────┬────────────────────────────────────────┘
                 │
                 ▼
        ┌────────────────┐
        │  DB에서 조회   │
        │  오늘의 주문   │
        └────────┬───────┘
                 │
                 ▼
     LoadExistingOrders(orders)
                 │
    ┌────────────┴────────────┐
    │ 각 주문의 SeqNo 확인    │
    │ - Insung1 있음/없음     │
    │ - Insung2 있음/없음     │
    └────────────┬────────────┘
                 │
                 ▼
    ┌─────────────────────────┐
    │  큐에 적재               │
    │  _existingOrders.Enqueue│
    └─────────────────────────┘


┌─────────────────────────────────────────────────────────┐
│              실행 중 - SignalR 이벤트                    │
└────────────────┬────────────────────────────────────────┘
                 │
     ┌───────────┴───────────┐
     │                       │
     ▼                       ▼
  [신규 주문]            [주문 변경]
     │                       │
     ▼                       ▼
AddNewOrder()         UpdateOrder()
     │                       │
     ▼                       ▼
_newOrders.Enqueue   _existingOrders.Enqueue
                              (또는 기존 주문 수정)


┌─────────────────────────────────────────────────────────┐
│         자동배차 루프 (5초마다)                          │
└────────────────┬────────────────────────────────────────┘
                 │
                 ▼
    ┌─────────────────────┐
    │ AutoAllocAsync()    │
    └─────────┬───────────┘
              │
    ┌─────────┴─────────┐
    │                   │
    ▼                   ▼
[신규 처리]        [기존 처리]
    │                   │
    ▼                   ▼
while (TryDequeue)  while (TryDequeue)
    │                   │
    ▼                   ▼
ProcessAsync()      ProcessAsync()
    │                   │
    ├─ Success → 완료
    ├─ Retry → 다시 Enqueue
    └─ KeepInQueue → 다시 Enqueue
```

---

## 5. 단계별 구현 계획

### ✅ Phase 1: 기초 구조 (현재 단계)

- [ ] 1-1. `AutoAllocQueueManager.cs` 클래스 생성
- [ ] 1-2. 기본 Queue 정의
- [ ] 1-3. `LoadExistingOrders()` 메서드 구현
- [ ] 1-4. 큐 상태 조회 메서드 구현
- [ ] 1-5. 단위 테스트 (2개 오더 적재 확인)

**목표**: 기존 오더를 큐에 적재하고 확인

---

### ⏳ Phase 2: 기존 오더 처리

- [ ] 2-1. `ProcessExistingOrdersAsync()` 메서드 구현
- [ ] 2-2. StateFlag별 처리 로직 연결
  - [ ] Existed_NonSeqno 처리
  - [ ] Existed_WithSeqno 처리
  - [ ] NotChanged 처리
- [ ] 2-3. 결과 처리 (Success, Retry, KeepInQueue)
- [ ] 2-4. 테스트 (2개 오더 처리 확인)

**목표**: 큐에서 오더를 꺼내서 처리

---

### ⏳ Phase 3: ExternalAppController 연동

- [ ] 3-1. ExternalAppController에 QueueManager 추가
- [ ] 3-2. `LoadExistingOrders()` 구현
- [ ] 3-3. 앱 시작 시 DB에서 오더 로드
- [ ] 3-4. MainWnd에서 호출 구현
- [ ] 3-5. 통합 테스트

**목표**: 앱 시작 시 자동으로 기존 오더 적재

---

### ⏳ Phase 4: 신규 오더 처리

- [ ] 4-1. `AddNewOrder()` 메서드 구현
- [ ] 4-2. `ProcessNewOrdersAsync()` 메서드 구현
- [ ] 4-3. SignalR 이벤트 연동 확인
- [ ] 4-4. 신규 오더 테스트

**목표**: 실시간 신규 오더 처리

---

### ⏳ Phase 5: 주문 변경 처리

- [ ] 5-1. `UpdateOrder()` 메서드 구현
- [ ] 5-2. 큐에서 기존 주문 찾기/수정 로직
- [ ] 5-3. SignalR 변경 이벤트 연동
- [ ] 5-4. 변경 테스트

**목표**: 주문 변경 실시간 반영

---

### ⏳ Phase 6: 최적화 및 안정화

- [ ] 6-1. 로깅 강화
- [ ] 6-2. 에러 처리 강화
- [ ] 6-3. 성능 모니터링
- [ ] 6-4. 문서화

---

## 6. 구현 상세

### 📁 Phase 1-1: AutoAllocQueueManager 클래스 생성

**파일 위치**: `Kai.Client.CallCenter/Classes/Class_Master/AutoAllocQueueManager.cs`

```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Services;

namespace Kai.Client.CallCenter.Classes.Class_Master;

#nullable disable

/// <summary>
/// 자동배차 주문 큐 관리자
/// Queue 기반으로 주문을 관리하여 복잡성 제거
/// </summary>
public class AutoAllocQueueManager
{
    #region Queues
    /// <summary>
    /// 신규 주문 큐 (Created)
    /// 우선 처리됨
    /// </summary>
    private Queue<AutoAlloc> _newOrders = new();

    /// <summary>
    /// 기존 주문 큐 (Existed_NonSeqno, Existed_WithSeqno, NotChanged 등)
    /// 순차 처리됨
    /// </summary>
    private Queue<AutoAlloc> _existingOrders = new();
    #endregion

    #region Properties
    /// <summary>
    /// 신규 주문 큐 크기
    /// </summary>
    public int NewOrderCount => _newOrders.Count;

    /// <summary>
    /// 기존 주문 큐 크기
    /// </summary>
    public int ExistingOrderCount => _existingOrders.Count;

    /// <summary>
    /// 전체 주문 큐 크기
    /// </summary>
    public int TotalCount => NewOrderCount + ExistingOrderCount;
    #endregion

    #region 큐 적재 - Phase 1
    /// <summary>
    /// 앱 시작 시 기존 주문 목록을 큐에 적재
    /// </summary>
    /// <param name="orders">DB에서 조회한 오늘의 주문 목록</param>
    /// <param name="networkName">네트워크 이름 (Insung1, Insung2 등)</param>
    public void LoadExistingOrders(List<TbOrder> orders, string networkName)
    {
        if (orders == null || orders.Count == 0)
        {
            Debug.WriteLine($"[AutoAllocQueue] 로드할 기존 주문이 없습니다: {networkName}");
            return;
        }

        Debug.WriteLine($"[AutoAllocQueue] 기존 주문 로드 시작: {networkName}, {orders.Count}개");

        int addedCount = 0;
        foreach (var order in orders)
        {
            // 네트워크별 SeqNo 필드 확인
            string seqNo = GetSeqNoByNetwork(order, networkName);
            bool hasSeqNo = !string.IsNullOrEmpty(seqNo);

            // StateFlag 결정
            var stateFlag = hasSeqNo
                ? PostgService_Common_OrderState.Existed_WithSeqno
                : PostgService_Common_OrderState.Existed_NonSeqno;

            // AutoAlloc 객체 생성 및 큐에 추가
            var autoAlloc = new AutoAlloc
            {
                StateFlag = stateFlag,
                NewOrder = order,
                OldOrder = null
            };

            _existingOrders.Enqueue(autoAlloc);
            addedCount++;

            Debug.WriteLine($"[AutoAllocQueue] 추가: KeyCode={order.KeyCode}, SeqNo={seqNo ?? "(없음)"}, Flag={stateFlag}");
        }

        Debug.WriteLine($"[AutoAllocQueue] 기존 주문 로드 완료: {networkName}, {addedCount}개 추가, 큐 크기={_existingOrders.Count}");
    }

    /// <summary>
    /// 네트워크별 SeqNo 필드 가져오기
    /// </summary>
    private string GetSeqNoByNetwork(TbOrder order, string networkName)
    {
        return networkName switch
        {
            StdConst_Network.INSUNG1 => order.Insung1,
            StdConst_Network.INSUNG2 => order.Insung2,
            StdConst_Network.CARGO24 => order.Cargo24,
            StdConst_Network.ONECALL => order.Onecall,
            _ => null
        };
    }
    #endregion

    #region 큐 조회
    /// <summary>
    /// 큐 상태 출력 (디버깅용)
    /// </summary>
    public void PrintQueueStatus()
    {
        Debug.WriteLine($"[AutoAllocQueue] 큐 상태: 신규={NewOrderCount}, 기존={ExistingOrderCount}, 전체={TotalCount}");
    }

    /// <summary>
    /// 기존 주문 큐의 내용 반환 (UI 표시용)
    /// </summary>
    public List<AutoAlloc> GetExistingOrdersForUI()
    {
        return new List<AutoAlloc>(_existingOrders);
    }

    /// <summary>
    /// 신규 주문 큐의 내용 반환 (UI 표시용)
    /// </summary>
    public List<AutoAlloc> GetNewOrdersForUI()
    {
        return new List<AutoAlloc>(_newOrders);
    }
    #endregion

    #region 큐 초기화
    /// <summary>
    /// 모든 큐 클리어
    /// </summary>
    public void Clear()
    {
        _newOrders.Clear();
        _existingOrders.Clear();
        Debug.WriteLine($"[AutoAllocQueue] 모든 큐 클리어 완료");
    }
    #endregion
}

#nullable restore
```

---

### 📋 Phase 1-5: 단위 테스트 (수동)

**테스트 시나리오:**

```csharp
// 1. AutoAllocQueueManager 생성
var queueMgr = new AutoAllocQueueManager();

// 2. 테스트 오더 2개 준비
var testOrders = new List<TbOrder>
{
    new TbOrder
    {
        KeyCode = 1,
        Insung1 = "12345",  // SeqNo 있음
        // ... 기타 필드
    },
    new TbOrder
    {
        KeyCode = 2,
        Insung1 = "",  // SeqNo 없음
        // ... 기타 필드
    }
};

// 3. 큐에 적재
queueMgr.LoadExistingOrders(testOrders, StdConst_Network.INSUNG1);

// 4. 확인
queueMgr.PrintQueueStatus();
// 출력: 큐 상태: 신규=0, 기존=2, 전체=2

// 5. 큐 내용 확인
var orders = queueMgr.GetExistingOrdersForUI();
foreach (var order in orders)
{
    Debug.WriteLine($"KeyCode={order.NewOrder.KeyCode}, Flag={order.StateFlag}");
}
// 출력:
// KeyCode=1, Flag=Existed_WithSeqno
// KeyCode=2, Flag=Existed_NonSeqno
```

**기대 결과:**
- ✅ 큐에 2개 오더 적재 완료
- ✅ SeqNo 유무에 따라 StateFlag 올바르게 설정
- ✅ 큐 크기 정상 표시
- ✅ 큐 내용 조회 가능

---

## 7. 테스트 계획

### ✅ Phase 1 테스트

**목표**: 기존 오더 2개를 큐에 적재하고 확인

**테스트 케이스:**

| 항목 | 입력 | 기대 결과 |
|------|------|----------|
| TC-1 | SeqNo 있는 오더 1개 | Existed_WithSeqno 적재 |
| TC-2 | SeqNo 없는 오더 1개 | Existed_NonSeqno 적재 |
| TC-3 | 오더 2개 (SeqNo 혼합) | 2개 모두 정상 적재 |
| TC-4 | 빈 리스트 | 큐 크기 0 유지 |
| TC-5 | null 리스트 | 에러 없이 처리 |

**검증 방법:**
1. Debug.WriteLine 출력 확인
2. queueMgr.ExistingOrderCount 확인
3. GetExistingOrdersForUI()로 내용 확인

---

## 8. 다음 단계 준비

### Phase 2에서 구현할 메서드 (미리 계획)

```csharp
/// <summary>
/// 기존 주문 처리 (Phase 2에서 구현)
/// </summary>
public async Task<ProcessStats> ProcessExistingOrdersAsync(
    CancelTokenControl ctrl,
    IOrderProcessor processor)
{
    var stats = new ProcessStats();

    // 한 번에 최대 10개 처리
    int maxProcess = Math.Min(_existingOrders.Count, 10);

    for (int i = 0; i < maxProcess; i++)
    {
        if (!_existingOrders.TryDequeue(out var order))
            break;

        await ctrl.WaitIfPausedOrCancelledAsync();

        var result = await processor.ProcessAsync(order);

        switch (result)
        {
            case ProcessResult.Success:
                stats.Completed++;
                break;

            case ProcessResult.Retry:
                _existingOrders.Enqueue(order);
                stats.Retried++;
                break;

            case ProcessResult.KeepInQueue:
                _existingOrders.Enqueue(order);
                stats.Kept++;
                break;
        }
    }

    return stats;
}
```

---

## 9. 참고 자료

### 관련 파일

- **백업 코드**: `Backup/AutoAllocCtrl.cs`
- **현재 구현**: `Kai.Client.CallCenter/Classes/Class_Master/ExternalAppController.cs`
- **네트워크**: `Kai.Client.CallCenter/Networks/NwInsung01.cs`
- **SignalR**: `Kai.Client.CallCenter/Classes/SrGlobalClient.cs`

### 주요 클래스

- `AutoAlloc`: 자동배차 주문 데이터 (NewOrder, OldOrder, StateFlag)
- `TbOrder`: 주문 테이블 모델
- `PostgService_Common_OrderState`: 주문 상태 플래그 (Enum)
- `CancelTokenControl`: 취소 토큰 제어

---

## 10. 체크리스트

### ✅ Phase 1 완료 조건

- [ ] AutoAllocQueueManager.cs 파일 생성
- [ ] LoadExistingOrders() 메서드 구현
- [ ] GetSeqNoByNetwork() 메서드 구현
- [ ] 큐 상태 조회 메서드 구현 (Count, PrintStatus, GetForUI)
- [ ] 테스트 오더 2개로 적재 테스트 성공
- [ ] Debug 로그로 적재 과정 확인
- [ ] 코드 리뷰 및 확인

---

**다음 작업**: Phase 1-1 AutoAllocQueueManager.cs 클래스 생성

