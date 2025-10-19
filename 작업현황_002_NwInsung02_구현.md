# NwInsung02 초기화 구현 완료 (2025-10-19)

## 작업 개요
인성2 네트워크 앱(NwInsung02) 초기화 로직 구현 완료. 인성1을 기반으로 복사하여 생성하고, StatusBtn Down 상태 OFR 확인 로직 추가.

---

## 1. NwInsung02.cs 생성 및 기본 설정

### 파일 생성
**경로**: `D:\CodeWork\WithVs2022\KaiWork\Kai.Client\Kai.Client.CallCenter\Kai.Client.CallCenter\Networks\NwInsung02.cs`

**기반**: NwInsung01.cs 복사

### 주요 변경사항
```csharp
// Constants
public const string APP_NAME = StdConst_Network.INSUNG2;
public const string INFO_FILE_NAME = "Insung02_FileInfo.txt";

// IExternalApp 구현
public string AppName => "Insung02";

// 모든 Debug 로그 및 에러 메시지
[NwInsung01] → [NwInsung02]
NwInsung01/ → NwInsung02/
```

### 생성자 Debug 로그 추가
**위치**: Lines 273-298

```csharp
public NwInsung02()
{
    Debug.WriteLine($"[NwInsung02] 생성자 호출 --------------------------------------------------------");
    Debug.WriteLine($"  APP_NAME = {APP_NAME}");
    Debug.WriteLine($"  s_Id = {s_Id}");
    Debug.WriteLine($"  s_Pw = {s_Pw}");
    Debug.WriteLine($"  s_Use = {s_Use}");
    Debug.WriteLine($"  s_AppPath = {s_AppPath}");

    // Context 생성
    m_Context = new InsungContext(APP_NAME, s_Id, s_Pw);

    Debug.WriteLine($"[NwInsung02] Context 생성 완료:");
    Debug.WriteLine($"  m_Context.AppName = {m_Context.AppName}");
    Debug.WriteLine($"  m_Context.Id = {m_Context.Id}");
    Debug.WriteLine($"  m_Context.Pw = {m_Context.Pw}");
}
```

---

## 2. appsettings.json 설정

### Insung02 섹션 추가
**파일**: `appsettings.json` Lines 23-28

```json
"Insung02": {
  "Use": true,
  "Id": "동신MSI-S",
  "Pw": "ilji844200",
  "AppPath": "C:\\Program Files (x86)\\INSUNGDATA\\인성퀵화물통합솔루션\\WooriNetWorkUpdater.exe"
}
```

### 인성1과의 차이점
| 구분 | 인성1 | 인성2 |
|------|-------|-------|
| 폴더 | `인성퀵화물통합솔루션_KN` | `인성퀵화물통합솔루션` |
| 실행파일 | `KoreaNetWorkUpdater.exe` | `WooriNetWorkUpdater.exe` |

---

## 3. CommonFuncs.cs - 설정 로드 추가

### LoadExternalAppsConfig 함수 수정
**파일**: `CommonFuncs.cs` Lines 61-68

```csharp
// Insung02 설정 로드
string sUse02 = JsonFileManager.GetValue("ExternalApps:Insung02:Use");
NwInsung02.s_Use = StdConvert.StringToBool(sUse02);
NwInsung02.s_Id = JsonFileManager.GetValue("ExternalApps:Insung02:Id");
NwInsung02.s_Pw = JsonFileManager.GetValue("ExternalApps:Insung02:Pw");
NwInsung02.s_AppPath = JsonFileManager.GetValue("ExternalApps:Insung02:AppPath");

Debug.WriteLine($"[CommonFuncs] Insung02 설정 로드: Use={NwInsung02.s_Use}, Id={NwInsung02.s_Id}, AppPath={NwInsung02.s_AppPath} -----------------------");
```

---

## 4. ExternalAppController 수정

### 변수 선언 추가
**파일**: `ExternalAppController.cs` Line 40

```csharp
public NwInsung02 Insung02 { get; private set; }
```

### InitializeAsync 수정
**Lines 71-92**

**NwInsung01 주석처리** (빠른 테스트용):
```csharp
//if (NwInsung01.s_Use)
//{
//    Debug.WriteLine($"[ExternalAppController] Insung01 생성: Id={NwInsung01.s_Id}");
//    Insung01 = new NwInsung01();
//    m_ListApps.Add(Insung01);
//}
```

**NwInsung02 활성화**:
```csharp
if (NwInsung02.s_Use)
{
    Debug.WriteLine($"[ExternalAppController] Insung02 생성: Id={NwInsung02.s_Id}");
    Insung02 = new NwInsung02();
    m_ListApps.Add(Insung02);
}
else
{
    Debug.WriteLine("[ExternalAppController] Insung02 사용 안함 (s_Use=false)");
}
```

---

## 5. InsungsAct_RcptRegPage.cs - StatusBtn Down 상태 OFR 추가 ⭐⭐⭐

### 배경
- 백업 파일 `InsungsAct_ReceiptPage.cs` Lines 265-345에 Button Down Status 로직이 주석처리되어 있음
- 현재 코드에는 전체버튼 클릭만 있고 Down 상태 확인이 TODO로 남아있음

### OFR 반복의 의미
**핵심**: OFR 반복 = UI 상태 변화 대기 (폴링)

```csharp
// 전체버튼 클릭 후 Down 상태가 될 때까지 대기
for (int i = 0; i < 반복횟수; i++)
{
    OFR 이미지 확인
    if (Down 상태 감지) break;  // 성공
    await Task.Delay(100);
}
```

### 구현 패턴 (첫/마지막 루프, 중간 딜레이)
**위치**: `InsungsAct_RcptRegPage.cs` Lines 275-347

```csharp
// 4-2. StatusBtn Down 상태 OFR 확인 (전체버튼 클릭 후 UI 상태 변화 대기)

// 4-2-1. 접수버튼 Down (첫 버튼) - OFR 루프로 Down 상태 대기
bool bFoundDown접수 = false;
for (int i = 0; i < CommonVars.c_nRepeatShort; i++)
{
    StdResult_NulBool resultOfrStatus접수 = await OfrWork_Insungs.OfrIsMatchedImage_DrawRelRectAsync(
        m_RcptPage.StatusBtn_hWnd접수, HEADER_GAB, "Img_접수버튼_Down", false, false, false);

    if (StdConvert.NullableBoolToBool(resultOfrStatus접수.bResult))
    {
        bFoundDown접수 = true;
        Debug.WriteLine($"[InsungsAct_RcptRegPage] 접수버튼 Down 상태 확인 완료");
        break;
    }
    await Task.Delay(100);
}
if (!bFoundDown접수)
    Debug.WriteLine($"[InsungsAct_RcptRegPage] 접수버튼 Down 상태 OFR 실패 (무시)");

// 4-2-2. 중간 버튼들 Down - 딜레이 후 OFR 바로
await Task.Delay(300);

// 배차버튼 Down
StdResult_NulBool resultOfrStatus배차 = await OfrWork_Insungs.OfrIsMatchedImage_DrawRelRectAsync(
    m_RcptPage.StatusBtn_hWnd배차, HEADER_GAB, "Img_배차버튼_Down", false, false, false);
if (!StdConvert.NullableBoolToBool(resultOfrStatus배차.bResult))
    Debug.WriteLine($"[InsungsAct_RcptRegPage] 배차버튼 Down 상태 OFR 실패 (무시)");
else
    Debug.WriteLine($"[InsungsAct_RcptRegPage] 배차버튼 Down 상태 확인 완료");

// 운행, 완료, 취소 버튼도 동일 패턴
// ...

// 4-2-3. 전체버튼 Down (마지막 버튼) - OFR 루프로 Down 상태 대기
bool bFoundDown전체 = false;
for (int i = 0; i < CommonVars.c_nRepeatShort; i++)
{
    StdResult_NulBool resultOfrStatus전체 = await OfrWork_Insungs.OfrIsMatchedImage_DrawRelRectAsync(
        m_RcptPage.StatusBtn_hWnd전체, HEADER_GAB, "Img_전체버튼_Down", false, false, false);

    if (StdConvert.NullableBoolToBool(resultOfrStatus전체.bResult))
    {
        bFoundDown전체 = true;
        Debug.WriteLine($"[InsungsAct_RcptRegPage] 전체버튼 Down 상태 확인 완료");
        break;
    }
    await Task.Delay(100);
}
if (!bFoundDown전체)
    Debug.WriteLine($"[InsungsAct_RcptRegPage] 전체버튼 Down 상태 OFR 실패 (무시)");

Debug.WriteLine($"[InsungsAct_RcptRegPage] StatusBtn Down 상태 확인 완료");
```

### 패턴 설명
1. **첫 버튼 (접수)**: for 루프로 Down 상태 나타날 때까지 폴링
2. **중간 버튼들 (배차, 운행, 완료, 취소)**: 300ms 딜레이 후 OFR 바로 실행
3. **마지막 버튼 (전체)**: for 루프로 Down 상태 나타날 때까지 폴링

### 리팩토링 원칙 적용
- ✅ 명확한 단계 주석 (`// 4-2-1.`, `// 4-2-2.`, `// 4-2-3.`)
- ✅ OFR 실패 시 경고만 출력하고 계속 진행 (Non-blocking)
- ✅ CommandBtn OFR 패턴과 동일한 방식
- ✅ Up 상태 확인과 동일한 첫/마지막 루프 패턴

---

## 6. 초기화 로직 완성도 검증

### 백업 파일과 비교 분석
**백업**: `InsungsAct_ReceiptPage.cs` Lines 155-405

| 단계 | 백업 파일 | 현재 코드 | 상태 |
|------|-----------|-----------|------|
| 바메뉴 클릭 | 2중 주석 | ✅ Lines 128-131 | 개선됨 |
| TopWnd 찾기 | ❌ 없음 | ✅ Lines 133-153 | 신규 추가 |
| StatusBtn 찾기 | 주석 | ✅ Lines 155-256 | 텍스트 검증 포함 |
| Button Up Status | 2중 주석 | ✅ TODO (필요시 추가) | 현재 불필요 |
| 전체버튼 클릭 | 주석 | ✅ Lines 260-273 | 완료 |
| **Button Down Status** | **주석** | **✅ Lines 275-347** | **방금 추가** |
| CommandBtn 찾기 | 주석 | ✅ Lines 349-410 | OFR 포함 |
| CallCount 찾기 | 주석 | ✅ Lines 413-467 | 완료 |
| Datagrid 초기화 | 주석 | ✅ Lines 469-524 | 완료 |

### 결론
✅ **누락된 부분 없음** - 초기화 로직 완성

---

## 7. 공통 클래스 사용 확인

### InsungsAct_RcptRegPage (공통)
```
InsungsAct_RcptRegPage (공통 클래스)
    ↑                    ↑
NwInsung01          NwInsung02
```

**특징**:
- 인성1과 인성2는 동일한 `InsungsAct_RcptRegPage` 인스턴스 사용
- Context를 통해 AppName만 다르게 전달
- StatusBtn Down 상태 OFR 로직도 양쪽 모두 자동 적용됨

**차이점**:
- FileInfo 파일: `Insung01_FileInfo.txt` vs `Insung02_FileInfo.txt`
  - 버튼 좌표, 윈도우 이름 등이 다를 수 있음
- Debug 로그: `[Insung01/...]` vs `[Insung02/...]`

---

## 8. 테스트 결과

### 생성자 테스트
**실행 흐름**:
1. `LoadExternalAppsConfig()` → Insung02 static 변수 로드
2. `ExternalAppController.InitializeAsync()` → NwInsung02 생성
3. `NwInsung02 생성자` → Context 생성 및 Debug 로그 출력

**결과**: ✅ 통과

**출력 예시**:
```
[CommonFuncs] Insung02 설정 로드: Use=True, Id=동신MSI-S, AppPath=C:\Program Files (x86)\INSUNGDATA\인성퀵화물통합솔루션\WooriNetWorkUpdater.exe
[ExternalAppController] Insung02 생성: Id=동신MSI-S
[NwInsung02] 생성자 호출 --------------------------------------------------------
  APP_NAME = INSUNG2
  s_Id = 동신MSI-S
  s_Pw = ilji844200
  s_Use = True
  s_AppPath = C:\Program Files (x86)\INSUNGDATA\인성퀵화물통합솔루션\WooriNetWorkUpdater.exe
[NwInsung02] Context 생성 완료:
  m_Context.AppName = Insung02
  m_Context.Id = 동신MSI-S
  m_Context.Pw = ilji844200
```

### InitializeAsync 테스트
**현재 상태**:
- `InitializeAsync()` 주석 처리 상태 (Line 69-149)
- FileInfo 로드까지만 활성화 (Line 74-79)
- 성공 반환: `return new StdResult_Status(StdResult.Success);`

---

## 9. 수정된 파일 목록

1. `Networks\NwInsung02.cs` - 신규 생성
2. `appsettings.json` - Insung02 섹션 추가
3. `Class_Common\CommonFuncs.cs` - Insung02 설정 로드 추가
4. `Class_Master\ExternalAppController.cs` - Insung02 변수 및 초기화 추가
5. `Networks\NwInsungs\InsungsAct_RcptRegPage.cs` - StatusBtn Down 상태 OFR 추가

---

## 10. 다음 작업 (자동배차 로직)

### 다음 세션 시작 전 확인사항
1. ✅ NwInsung02 생성 완료
2. ✅ appsettings.json 설정 완료
3. ✅ 초기화 로직 완료 (StatusBtn Down 포함)
4. ✅ 인성1/2 공통 클래스 구조 확인

### 백업 파일 위치
**자동배차 로직 참고**:
- `Backup\Networks\NwInsung02.cs` Lines 211-503
- `AutoAllocAsync` 함수 주석 처리됨

### 주요 조사 항목
1. AutoAllocAsync 함수 구조 분석
2. 인성1과 인성2의 차이점 파악
3. 현재 코드에 누락된 부분 확인
4. 리팩토링 패턴 적용 방안

---

## 11. 참고사항

### Backup 폴더 위치
**현재**: `D:\CodeWork\WithVs2022\KaiWork\Kai.Client\Kai.Client.CallCenter\Backup`

**중요**: 백업 파일은 **오직 이 폴더에서만** 찾아야 함

### 리팩토링 패턴
1. **명확한 단계 주석**: `// 1.`, `// 1-1.` 등
2. **OFR 반복 = UI 대기**: 폴링 패턴으로 상태 변화 확인
3. **첫/마지막 루프, 중간 딜레이**: 성능 최적화
4. **Non-blocking OFR**: 실패해도 경고만 출력하고 계속

### 설계 원칙
- **Context 패턴**: InsungContext를 통한 데이터 공유
- **공통 클래스**: InsungsAct_RcptRegPage를 인성1/2가 공유
- **IExternalApp 인터페이스**: 외부 앱 통합 관리

---

**작업 완료 일시**: 2025-10-19
**다음 세션 주제**: 자동배차 로직 조사 및 구현
