# Region 5-3 구현 TODO

## 📋 개요
**목표**: NwInsung01.cs의 Region 5-3 - 기존 주문(listEtcGroup) 처리 완성

**핵심 알고리즘**: 페이지별로 순회하며 리스트 전체 검사
- O(n*m) → O(m+n) 효율 개선 (n=리스트 항목, m=페이지 수)
- 최신 주문이 1페이지 상단에 있어 조기 탈출 가능
- 리스트 삭제를 통한 처리 완료 항목 제거

---

## ✅ 완료된 작업

### 1. 기본 구조 구현
- [x] Region 5-3 페이지별 리스트 검사 루프 구조 (NwInsung01.cs Line 407-461)
- [x] 외부 루프: 페이지별 순회 (1 → nTotPage)
- [x] 내부 루프: listEtcGroup 역순 순회 (백업 방식, 삭제 안전)
- [x] 조기 탈출: listEtcGroup.Count == 0

### 2. 타입 변경
- [x] AutoAlloc → AutoAllocModel
- [x] TbNewOrder → NewOrder
- [x] TbOldOrder → OldOrder
- [x] AutoAllocQueueController → QueueController
- [x] CtrlExcel → ExcelController

### 3. 빌드
- [x] 빌드 성공 (경고만 있음, 에러 없음)
- [x] git 커밋 완료 (f99e928)

---

## 🔧 남은 작업

### 1. 페이지 이동 로직 구현
**위치**: NwInsung01.cs Line 412-415

**구현 내용**:
```csharp
if (pageIdx > 1)
{
    // TODO: 다음 페이지로 이동
    // - PageDown 키 전송 또는 페이지 버튼 클릭
    // - 페이지 이동 후 대기
    // - 페이지 이동 확인
}
```

**고려사항**:
- 첫 페이지는 이미 조회버튼으로 이동되어 있음
- 페이지 이동 방법: PageDown 키, 스크롤, 또는 페이지 버튼 클릭
- 이동 후 화면 안정화 대기 필요

**참고**:
- 백업: `D:\CodeWork\WithVs2022\Save\KaiWork_Backup002\...\NwInsung01.cs` Line 408-409
  - `FindDatagridPageNIndex` 메서드가 페이지 이동 포함

---

### 2. 현재 페이지에서 주문번호 찾기
**위치**: NwInsung01.cs Line 417, 429-432

**구현 내용**:
```csharp
// TODO: 현재 페이지 캡처
Draw.Bitmap bmpPage = /* 캡처 */;

// TODO: 현재 페이지에서 sSeqNo 찾기
bool bFoundInPage = false;
int nRowIndex = -1;

// 현재 페이지의 모든 행을 순회하며 주문번호 비교
for (int rowIdx = 2; rowIdx < nRowsPerPage + 2; rowIdx++) // y=0 헤더, y=1 빈행, y=2부터 데이터
{
    // OFR로 주문번호 읽기
    string sPageSeqno = OFR로_주문번호_읽기(bmpPage, rowIdx);

    if (sPageSeqno == sSeqNo)
    {
        bFoundInPage = true;
        nRowIndex = rowIdx;
        break;
    }
}
```

**고려사항**:
- Datagrid 구조: RelChildRects[x, y]
  - y=0: 헤더
  - y=1: 빈 행 (Empty Row)
  - y=2~: 데이터 행
- c_nCol주문번호 = 2 (InsungsAct_RcptRegPage.cs Line 90)
- OFR 메서드 필요: 주문번호 컬럼만 읽기

**참고**:
- InsungsAct_RcptRegPage.cs Line 2663: `GetFirstRowSeqnoAsync` 참고

---

### 3. OFR로 상태 읽기
**위치**: NwInsung01.cs Line 436

**구현 내용**:
```csharp
if (bFoundInPage)
{
    // TODO: OFR로 상태 읽기
    // 찾은 행(nRowIndex)에서 상태 컬럼 읽기
    string sStatus = OFR로_상태_읽기(bmpPage, nRowIndex);

    // 상태별 처리...
}
```

**고려사항**:
- c_nCol상태 = 1 (InsungsAct_RcptRegPage.cs Line 89)
- 상태 값: "접수", "배차", "취소" 등
- OFR 메서드 필요: 상태 컬럼 읽기

**참고**:
- 백업: `OfrWork_Insungs.cs` Line 151-165
  - `OfrAnyResultFrom_From접수등록DatagridAsync` 메서드
  - 현재 프로젝트에는 없음 (빌드 에러 발생했던 이유)

**해결 방법**:
1. OfrWork_Insungs.cs에 메서드 추가
2. 또는 InsungsAct_RcptRegPage.cs에 helper 메서드 추가
3. OfrWork_Common의 기존 메서드 활용

---

### 4. StateFlag별 처리 및 재적재
**위치**: NwInsung01.cs Line 438-449

**구현 내용**:
```csharp
// TODO: StateFlag별 처리
if (kaiFlag == PostgService_Common_OrderState.NotChanged)
{
    // 변경 없으면 비교 후 재적재
    // resultAuto = await m_RcptRegPage.CheckIsOrderAsync_KaiSameInsungIfChanged(...);
}
else if (kaiFlag == PostgService_Common_OrderState.Change_ToCancel_DoDelete)
{
    // 취소 처리 후 삭제
    // resultAuto = await m_RcptRegPage.Command_ChaneTo취소AndDoDelete(...);
}
else if ((kaiFlag & PostgService_Common_OrderState.Existed_WithSeqno) != 0)
{
    // 기존 주문 업데이트 확인
    // resultAuto = await m_RcptRegPage.CheckIsOrderAsync_AssumeKaiUpdated(...);
}
else if ((kaiFlag & PostgService_Common_OrderState.Updated_Assume) != 0)
{
    // 업데이트된 주문 확인
    // resultAuto = await m_RcptRegPage.CheckIsOrderAsync_AssumeKaiUpdated(...);
}
else
{
    // 예상치 못한 플래그
    return error;
}

// TODO: 처리 결과에 따라 재적재 및 삭제
if (resultAuto.Result == AutoAlloc_StateResult.Done_DoDelete)
{
    // 재적재 안함, 리스트에서 삭제
    listEtcGroup.RemoveAt(index);
}
else if ((resultAuto.Result & AutoAlloc_StateResult.Done_NoDelete) != 0)
{
    // 재적재, 리스트에서 삭제
    ExternalAppController.QueueManager.ReEnqueue(kaiCopy, StdConst_Network.INSUNG1);
    listEtcGroup.RemoveAt(index);
}

// Refresh 필요 시
if ((resultAuto.Result & AutoAlloc_StateResult.Done_NeedRefresh) != 0)
{
    // 빈 행 클릭으로 Datagrid 갱신
    await m_RcptRegPage.ClickEmptyRowAsync(ctrl);
}
```

**고려사항**:
- StateFlag별 처리 메서드 구현 필요:
  - `CheckIsOrderAsync_KaiSameInsungIfChanged` (상태 비교)
  - `Command_ChaneTo취소AndDoDelete` (취소 처리)
  - `CheckIsOrderAsync_AssumeKaiUpdated` (업데이트 확인)
- 재적재 판단:
  - Done_DoDelete: 완료, 재적재 안함
  - Done_NoDelete: 완료, 재적재 필요
- Refresh 필요 시 빈 행 클릭

**참고**:
- 백업: NwInsung01.cs Line 423-471
- AutoAlloc_StateResult enum: 백업 AutoAllocCtrl.cs Line 40-49
- InsungsAct_RcptRegPage.cs Line 2534: `ClickFirstRowAsync` 참고

---

## 📁 참고 파일

### 현재 프로젝트
- `NwInsung01.cs` (Line 407-461): Region 5-3 구조
- `InsungsAct_RcptRegPage.cs`:
  - Line 88-90: Datagrid 컬럼 상수
  - Line 2534: ClickFirstRowAsync
  - Line 2612: InvertBitmap
  - Line 2663: GetFirstRowSeqnoAsync
- `AutoAllocModel.cs`: AutoAlloc 타입 정의
- `QueueController.cs`: 큐 관리

### 백업 파일 (참고용)
- `D:\CodeWork\WithVs2022\Save\KaiWork_Backup002\...\NwInsung01.cs`
  - Line 397-473: Region 5-3 원본 구현 (비효율적)
- `D:\CodeWork\WithVs2022\Save\KaiWork_Backup002\...\AutoAllocCtrl.cs`
  - Line 40-49: AutoAlloc_StateResult enum
  - Line 72-91: AutoAllocResult_Datagrid
  - Line 94-118: AutoAlloc class
- `D:\CodeWork\WithVs2022\Save\KaiWork_Backup002\...\OfrWork_Insungs.cs`
  - Line 151-165: OfrAnyResultFrom_From접수등록DatagridAsync

---

## 🎯 다음 단계

### 우선순위 1: OFR 메서드 구현
OFR 메서드가 없으면 주문번호/상태를 읽을 수 없으므로 가장 우선

**옵션**:
1. 백업의 OfrWork_Insungs.cs 참고하여 새로 구현
2. InsungsAct_RcptRegPage.cs에 간단한 helper 추가
3. 기존 OFR 메서드 활용 (GetFirstRowSeqnoAsync 참고)

**구현 항목**:
- [ ] Datagrid 주문번호 읽기 메서드
- [ ] Datagrid 상태 읽기 메서드
- [ ] 빌드 및 테스트

---

### 우선순위 2: 페이지 이동 구현
OFR 다음으로 중요, 페이지 순회를 위해 필요

**구현 항목**:
- [ ] PageDown 키 또는 스크롤 구현
- [ ] 페이지 이동 후 대기 로직
- [ ] 페이지 이동 확인 메커니즘

---

### 우선순위 3: 주문번호 찾기 루프
OFR과 페이지 이동이 완성되면 구현 가능

**구현 항목**:
- [ ] 현재 페이지 캡처
- [ ] 행별 주문번호 읽기 루프
- [ ] 찾았을 때 행 인덱스 저장

---

### 우선순위 4: StateFlag 처리
마지막 단계, 실제 업무 로직

**구현 항목**:
- [ ] CheckIsOrderAsync_KaiSameInsungIfChanged 구현
- [ ] Command_ChaneTo취소AndDoDelete 구현
- [ ] CheckIsOrderAsync_AssumeKaiUpdated 구현
- [ ] 재적재 로직 구현
- [ ] Refresh 로직 구현

---

## 📌 핵심 알고리즘 요약

```csharp
// 페이지별 순회
for (int pageIdx = 1; pageIdx <= nTotPage; pageIdx++)
{
    // 1. 페이지 이동 (pageIdx > 1)
    // 2. 페이지 캡처

    // 리스트 전체 검사 (역순)
    for (int no = listEtcGroup.Count; no > 0; no--)
    {
        int index = no - 1;
        if (index < 0) break;

        AutoAllocModel item = listEtcGroup[index];
        string sSeqNo = item.NewOrder.Insung1;

        // 3. 현재 페이지에서 주문번호 찾기
        bool found = 현재페이지에서찾기(sSeqNo);

        if (found)
        {
            // 4. OFR로 상태 읽기
            string status = OFR상태읽기();

            // 5. StateFlag별 처리
            var result = StateFlag처리(item, status);

            // 6. 재적재 또는 삭제
            if (result == Done_NoDelete)
                QueueManager.ReEnqueue(item);

            listEtcGroup.RemoveAt(index);
        }
        // 못찾으면 다음 페이지에서 찾기 위해 리스트 유지
    }

    // 7. 조기 탈출
    if (listEtcGroup.Count == 0) break;
}
```

---

## 💡 주의사항

1. **삭제 안전성**: 역순 루프 사용 (`for (int no = listEtcGroup.Count; no > 0; no--)`)
2. **인덱스 검증**: `if (index < 0) break;` 필수
3. **리스트 vs 큐**:
   - 못찾은 항목: 리스트에 자동 유지 (명시적 처리 불필요)
   - 찾은 항목: 재적재 판단 후 명시적 삭제
4. **조기 탈출**: `listEtcGroup.Count == 0` 체크로 불필요한 페이지 순회 방지
5. **최신 우선**: 최신 주문이 1페이지 상단에 있어 빠른 처리 가능

---

## 🔗 관련 커밋

- `f99e928` - feat: Region 5-3 페이지별 검색 루프 구조 구현
- `40a2161` - feat: Region 5-1, 5-2 구현 (기존 주문 관리)
- `caad2f6` - feat: SignalR로 Kai DB Insung1 업데이트 구현

---

**작성일**: 2025-11-01
**상태**: 진행 중 (구조 완성, 세부 구현 필요)
