# 작업 현황 - AutoAlloc 분석 및 개선

**날짜**: 2025-10-22
**작업자**: Claude Code

---

## 오늘 완료한 작업

### 1. SplashWnd 로그인 재시도 로직 수정 ✅
- `SrGlobalClient_RetryEvent` 추가 (IntEventHandler)
- 연결 재시도와 로그인 재시도 분리
- 메시지 중첩 방지
- 5초 대기 추가 (OnClosedAsync)

### 2. 누락된 파일/폴더 복구 ✅
**원인**: bin 폴더가 Clean/Rebuild 되면서 파일 삭제됨
**해결**: MustCopy 폴더 생성 및 .csproj 설정

#### MustCopy 구조:
```
Kai.Client.CallCenter\
└── MustCopy\
    ├── Data\
    ├── Python\
    ├── usbmmidd_v2\
    ├── Kai.Common.CppDll_Common.dll
    └── Kai.Client.X86ComBroker.exe
```

#### .csproj 설정 (243-268줄):
```xml
<!-- MustCopy 폴더에서 모든 필수 파일/폴더 복사 (Debug/Release 공통) -->
<ItemGroup>
    <!-- 폴더들 -->
    <Content Include="..\MustCopy\Data\**\*" Link="Data\%(RecursiveDir)%(Filename)%(Extension)">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>

    <!-- DLL 및 EXE 파일들 -->
    <Content Include="..\MustCopy\Kai.Common.CppDll_Common.dll">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
        <Link>Kai.Common.CppDll_Common.dll</Link>
    </Content>
</ItemGroup>
```

### 3. NwInsung01.AutoAllocAsync - Region 3 구현 ✅
**위치**: `Networks\NwInsung01.cs:246-270`

```csharp
#region 3. Check Datagrid
// Datagrid 윈도우 존재 확인 (최대 c_nRepeatShort회 재시도)
bool bDatagridExists = false;
for (int i = 0; i < c_nRepeatShort; i++)
{
    await ctrl.WaitIfPausedOrCancelledAsync();

    // Datagrid 핸들이 유효하고 윈도우가 존재하는지 확인
    if (m_Context.MemInfo.RcptPage.DG오더_hWnd != IntPtr.Zero &&
        Std32Window.IsWindow(m_Context.MemInfo.RcptPage.DG오더_hWnd))
    {
        bDatagridExists = true;
        Debug.WriteLine($"[{APP_NAME}] Datagrid 윈도우 확인 완료 (시도 {i + 1}회)");
        break;
    }

    await Task.Delay(c_nWaitNormal, ctrl.Token);
}

if (!bDatagridExists)
{
    Debug.WriteLine($"[{APP_NAME}] Datagrid 윈도우를 찾을 수 없음");
    return new StdResult_Status(StdResult.Fail, "Datagrid 윈도우를 찾을 수 없습니다.", "NwInsung01/AutoAllocAsync_03");
}
#endregion
```

---

## AutoAllocAsync 기존 로직 분석

### ✅ 잘 된 부분:
1. **구조화된 Region 분리** - 명확한 단계별 처리
2. **CancelToken 지원** - `ctrl.WaitIfPausedOrCancelledAsync()` 사용
3. **디버그 로깅** - 각 단계마다 상태 출력
4. **에러 처리** - StdResult_Status로 실패 원인 추적 가능

### ⚠️ 개선이 필요한 부분:

#### 🔴 High Priority

##### 1. 정렬 방향 혼란 (Line 212, 219)
**현재 코드**:
```csharp
.OrderByDescending(item => item.NewOrder.Insung1) // Insung1 KeyCode 정순 정렬
```

**문제**:
- 주석은 "정순 정렬"인데 코드는 `OrderByDescending` (역순)
- 혼란 유발

**개선안**:
```csharp
// Option 1: 정순이 맞다면
.OrderBy(item => item.NewOrder.Insung1) // Insung1 KeyCode 정순 정렬

// Option 2: 역순이 맞다면
.OrderByDescending(item => item.NewOrder.Insung1) // Insung1 KeyCode 역순 정렬 (최신 우선)
```

##### 2. listOrg 관리 불명확 (Line 203-207)
**현재 코드**:
```csharp
List<AutoAlloc> listOrg = ExternalAppController.listForInsung01;
var listInsung = new List<AutoAlloc>(listOrg);
listOrg.Clear(); // ← listOrg를 바로 클리어
```

**문제**:
- `listOrg`를 복사한 후 바로 Clear
- 나중에 다시 listOrg에 추가한다고 했는데 (TODO 주석), 참조가 끊어짐
- `ExternalAppController.listForInsung01`을 직접 수정하려는 의도인지 불명확

**개선안**:
```csharp
// 명확하게 분리
List<AutoAlloc> listFromController = ExternalAppController.listForInsung01;
var listInsung = new List<AutoAlloc>(listFromController);
listFromController.Clear(); // 원본 클리어 (처리 완료 후 다시 채울 예정)

// 처리 완료된 항목을 담을 리스트
var listProcessed = new List<AutoAlloc>();

// Region 4, 5에서:
// listProcessed에 추가하고, 마지막에:
// listFromController.AddRange(listProcessed);
```

#### 🟡 Medium Priority

##### 3. RestCount 로직 개선 (Line 228-234)
**현재 코드**:
```csharp
if (m_lRestCount % 60 == 0) // 5 ~ 10분 정도
{
    // TODO: Helper 함수 구현 필요
    await Task.Delay(c_nWaitLong, ctrl.Token);
}
```

**문제**:
- 주석에 "5~10분"이라고 했는데 60회가 얼마나 걸리는지 불명확
- AutoAllocAsync가 몇 초마다 호출되는지에 따라 달라짐

**개선안**:
```csharp
// 클래스 상수로 정의
private const int c_nRestCountThreshold = 60; // 60회마다 조회

if (m_lRestCount % c_nRestCountThreshold == 0)
{
    Debug.WriteLine($"[{APP_NAME}] {m_lRestCount}회 대기 후 조회버튼 클릭 시도");
    // TODO: await m_Context.RcptRegPageAct.Click조회버튼Async(ctrl);
}
```

##### 4. Datagrid 체크 최적화 (Line 249-262)
**현재 코드**:
```csharp
for (int i = 0; i < c_nRepeatShort; i++)
{
    await ctrl.WaitIfPausedOrCancelledAsync();

    if (/* Datagrid 존재 */) { break; }

    await Task.Delay(c_nWaitNormal, ctrl.Token); // ← 마지막에도 대기
}
```

**문제**:
- break 전에도 Task.Delay 실행됨
- 불필요한 100ms 지연

**개선안**:
```csharp
for (int i = 0; i < c_nRepeatShort; i++)
{
    await ctrl.WaitIfPausedOrCancelledAsync();

    if (m_Context.MemInfo.RcptPage.DG오더_hWnd != IntPtr.Zero &&
        Std32Window.IsWindow(m_Context.MemInfo.RcptPage.DG오더_hWnd))
    {
        bDatagridExists = true;
        Debug.WriteLine($"[{APP_NAME}] Datagrid 윈도우 확인 완료 (시도 {i + 1}회)");
        break;
    }

    // 마지막 시도가 아닐 때만 대기
    if (i < c_nRepeatShort - 1)
    {
        await Task.Delay(c_nWaitNormal, ctrl.Token);
    }
}
```

---

## 다음 작업 계획

### Region 4: Created Order 처리 (신규)
**위치**: Line 272-283

**필요한 작업**:
1. `CheckIsOrderAsync_AssumeKaiNewOrder` 메서드 구현
   - InsungsAct_RcptRegPage에 추가
   - AutoAlloc 객체를 받아서 신규 주문 처리
   - 결과 반환 (Error, Done_NoDelete 등)

2. listCreated 순회 로직 구현
   ```csharp
   for (int i = listCreated.Count - 1; i >= 0; i--)
   {
       var item = listCreated[i];
       var resultAuto = await m_Context.RcptRegPageAct.CheckIsOrderAsync_AssumeKaiNewOrder(item, ctrl);

       switch (resultAuto.Result)
       {
           case StdResult.Error:
               return new StdResult_Status(StdResult.Fail, resultAuto.sErrNPos);

           case StdResult.Done_NoDelete:
               listProcessed.Add(item); // listOrg 대신 listProcessed
               listCreated.RemoveAt(i);
               break;
       }
   }
   ```

### Region 5: Updated, NotChanged Order 처리 (기존)
**위치**: Line 285-309

**필요한 작업**:
1. `Click조회버튼Async` 구현
2. `ClickEmptyRowAsync` 구현
3. `FindDatagridPageNIndex` 구현
4. StateFlag별 처리 로직 구현
   - NotChanged → CheckIsOrderAsync_KaiSameInsungIfChanged
   - Change_ToCancel_DoDelete → Command_ChaneTo취소AndDoDelete
   - Existed_WithSeqno → CheckIsOrderAsync_AssumeKaiUpdated
   - Updated_Assume → CheckIsOrderAsync_AssumeKaiUpdated

---

## 주요 파일 위치

### 자동배차 관련:
- `Networks\NwInsung01.cs` - 인성1 자동배차 로직
- `Networks\NwInsung02.cs` - 인성2 자동배차 로직
- `Networks\NwInsungs\InsungContext.cs` - 공통 컨텍스트
- `Networks\NwInsungs\InsungsAct_RcptRegPage.cs` - 접수등록 페이지 액션
- `Classes\Class_Master\ExternalAppController.cs` - 외부 앱 컨트롤러
- `Classes\Class_Master\MasterModeManager.cs` - Master 모드 관리자

### 모니터 설정:
- **MasterModeManager.cs:207** - `s_Screens.m_WorkingMonitor` 설정
- **MainWnd.xaml.cs:313-329** - Master 모드 초기화

### 빌드 설정:
- **Kai.Client.CallCenter.csproj:243-268** - MustCopy 폴더 복사 규칙

---

## 메모

### Clean/Rebuild 주의사항
- MustCopy 폴더가 제대로 설정되어 있으면 Clean 후에도 자동 복사됨
- 하지만 MustCopy 원본 폴더는 반드시 유지해야 함
- Git에 MustCopy 폴더를 커밋하는 것을 권장

### 개선 우선순위:
1. 🔴 **정렬 방향 명확화** - 혼란 방지
2. 🔴 **listOrg 참조 관리** - 버그 방지
3. 🟡 **Datagrid 체크 최적화** - 성능 개선
4. 🟢 **RestCount 상수화** - 가독성 개선

### 다음 세션에서 할 일:
1. 개선점 1, 2 먼저 수정
2. Region 4 구현 시작 (또는 기존 메서드들 먼저 구현)
3. Helper 메서드들 우선순위 결정

---

## 참고: ExternalAppController 구조

```csharp
public class ExternalAppController
{
    public static List<AutoAlloc> listForInsung01 = new List<AutoAlloc>();
    public static List<AutoAlloc> listForInsung02 = new List<AutoAlloc>();

    // NwInsung01.AutoAllocAsync에서:
    // 1. listForInsung01을 복사
    // 2. 원본 Clear
    // 3. 처리
    // 4. 처리 완료된 항목을 다시 추가 (TODO)
}
```

---

**작성일**: 2025-10-22
**다음 작업 예정일**: TBD
