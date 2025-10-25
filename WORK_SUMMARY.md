# CheckBox OFR 시스템 복구 및 개선 작업 요약

## 작업 일자
2025-10-25

## 작업 목표
신규 주문 등록 기능 중 **공유(Share) CheckBox** 처리를 위한 OFR(Optical/Image Recognition) 시스템 복구

## 완료된 작업

### 1. CheckBox OFR 함수 복구 (OfrWork_Insungs.cs)
주석 처리된 3개의 CheckBox 인식 함수 복구:

- **OfrImgChkValue_RectInBitmapAsync**: Bitmap에서 체크박스 상태 인식
- **OfrImgReChkValue_RectInHWndAsync**: 윈도우 핸들에서 반복 체크박스 인식
- **OfrImgUntilChkValue_RectInHWndAsync**: 특정 상태가 될 때까지 대기

**개선 사항:**
- Try-catch 추가로 안정성 향상
- 디버그 모드에서 ImageToCheckState 대화상자 자동 표시
- Async/await 패턴 적용

### 2. OfrService 저수준 함수 복구 (OfrService.cs)
4개의 비트맵 분석 함수 복구 및 개선:

- **GetMaxBrightness_FromColorBitmapFast**: 최대 밝기 계산
- **GetMaxBrightness_FromColorBitmapRectFast**: 특정 영역 최대 밝기
- **GetForeGroundDrawRectangle_FromColorBitmapRectFast**: 전경 영역 추출
- **GetAverageBrightness_FromColorBitmapRectFast**: 평균 밝기 계산

**버그 수정:**
- `<= 255` → `< 255`: 255(흰 배경) 제외 로직 수정
- Try-catch-finally 블록 추가
- UnlockBits을 finally 블록으로 이동

**밝기 범위 로직:**
- 0: 검은 배경 (제외)
- 1-254: 전경 (텍스트, 체크박스 테두리 등)
- 255: 흰 배경 (제외)

### 3. ImageToCheckState 대화상자 복구 (ImageToCheckState.xaml.cs)
사용자 학습 인터페이스 복구:

**기능:**
- 체크박스 이미지 표시
- "Checked" / "Unchecked" 선택
- DB 저장 (Postgres TbText 테이블)
- 분석 정보 표시 (Width, Height, trueRate, HexArray)

**개선 사항:**
- OfrModel_BmpTextAnalysis → OfrModel_BitmapAnalysis 전환
- Threshold 필드 제거 (현재 모델에 없음)
- InsertRow → InsertRowAsync 변경
- null-safe 연산자 사용 (?.연산자)
- KeyCode 설정 추가

### 4. SetCheckBox_StatusAsync 함수 추가 (OfrWork_Commons.cs)
범용 체크박스 제어 함수:

**기능:**
1. 현재 상태 읽기
2. 원하는 상태면 성공 반환
3. 클릭 후 상태 확인 (루프)
4. 최종 확인

**사용법:**
```csharp
StdResult_Error result = await OfrWork_Common.SetCheckBox_StatusAsync(
    hWndTop, rcCheckBox, true, "공유");
```

### 5. Dual Brightness 함수 개선 (OfrWork_Commons.cs)
2개의 Dual Brightness 함수 복구:

- **OfrImage_DrawRelSpareRect_ByDualBrightnessAsync**: 특정 영역 인식
- **OfrImage_InSparedBitmapt_ByDualBrightnessAsync**: 전체 Bitmap 인식

**알고리즘:**
1. MaxBrightness 기반 전경 영역 찾기
2. 전경 영역 추출 (bmpExact)
3. AvgBrightness 기반 BitArray 생성
4. DB 검색 (Width, Height, HexArray)

**버그 수정:**
- DB에 없을 때 `modelText` 반환하도록 수정 (이미지 표시를 위해)

### 6. OfrModel 정리
- OfrModel_BmpTextAnalysis 삭제 (구버전)
- OfrModel_BitmapAnalysis 사용 (현재 버전)

### 7. 디버그 로그 추가
Dual Brightness 각 단계별 로그:
- Step1: MaxBrightness, rcForeground
- Step2: AvgBrightness, Size, trueRate
- Step3: DB 검색 결과
- Step4: DB에 없음

### 8. 신규 주문 등록 - 공유 CheckBox 구현 (InsungsAct_RcptRegPage.cs)
**구현 내용:**
```csharp
// 4-2. 공유 (Share) - CheckBox
Draw.Bitmap bmpWnd = OfrService.CaptureScreenRect_InWndHandle(wndRcpt.TopWnd_hWnd, 0);
StdResult_NulBool resultShare = await OfrWork_Insungs.OfrImgChkValue_RectInBitmapAsync(
    bmpWnd, m_FileInfo.접수등록Wnd_우측상단_rcChkRel공유);

if (order.Share != currentShare)
{
    StdResult_Error resultError = await OfrWork_Common.SetCheckBox_StatusAsync(
        wndRcpt.TopWnd_hWnd,
        m_FileInfo.접수등록Wnd_우측상단_rcChkRel공유,
        order.Share, "공유");
}
```

**영역 설정:**
```csharp
public Draw.Rectangle 접수등록Wnd_우측상단_rcChkRel공유 { get; set; }
    = new Draw.Rectangle(471, 252, 18, 18);
```

## 테스트 결과

### 성공 케이스
```
[OfrWork_Common] Dual Brightness Step1: MaxBrightness=254, rcRelSpare={X=471,Y=252,Width=18,Height=18}
[OfrWork_Common] Dual Brightness Step1: rcForeground={X=471,Y=252,Width=18,Height=18}
[OfrWork_Common] Dual Brightness Step2: AvgBrightness=202, Size=18x18, trueRate=0.16049382716049382
[OfrWork_Common] Dual Brightness Step3: DB 검색 결과=Unchecked
[INSUNG1]   공유: True
[INSUNG1] ===== 모든 입력 완료 =====
```

**분석:**
- ✅ 체크박스 영역 정확히 캡처 (18x18)
- ✅ 전경 영역 추출 성공
- ✅ trueRate=0.16 (16% 전경 = 체크박스 테두리)
- ✅ Unchecked 상태 인식
- ✅ 클릭 후 Checked 상태로 변경
- ✅ ImageToCheckState 대화상자에서 학습 성공

## 기술적 세부사항

### Dual Brightness 알고리즘
1. **MaxBrightness 단계**:
   - 영역 내 최대 밝기 찾기 (배경 밝기)
   - threshold = MaxBrightness - 1
   - 전경 영역 추출 (threshold보다 어두운 픽셀)

2. **AvgBrightness 단계**:
   - 전경 영역의 평균 밝기 계산
   - BitArray 생성 (AvgBrightness 기준)
   - HexString 변환 (DB 저장/검색)

### DB 구조 (TbText)
- **Width, Height**: 이미지 크기
- **HexStrValue**: BitArray의 Hex 문자열
- **Text**: "Checked" 또는 "Unchecked"
- **Searched**: 검색 횟수 (학습 품질 지표)

### 사용된 기술
- **LockBits/UnlockBits**: 고속 비트맵 메모리 접근
- **Unsafe code**: 포인터 연산으로 성능 최적화
- **PostMessage API**: Windows 메시지 기반 마우스 클릭
- **Dispatcher.Invoke**: UI 스레드 동기화
- **Async/await**: 비동기 패턴

## 남은 작업

### 다음 UI 요소 구현
1. **요금종류 (FeeType)** - RadioButton
2. **차량종류 (CarType)** - RadioButton
3. **배송타입 (DeliverType)** - RadioButton
4. **계산서 (TaxBill)** - CheckBox
5. **요금 그룹 (Fee Group)** - Numeric inputs

### 향후 개선 사항
- RadioButton OFR 함수 복구
- ImageToRadioState 대화상자 구현
- Numeric input OFR 함수 개선
- SQLite 로컬 DB 동기화

## 파일 변경 내역

### 수정된 파일
1. `OfrWork_Insungs.cs` - CheckBox OFR 함수 3개 복구
2. `OfrWork_Commons.cs` - Dual Brightness 함수 2개 복구, SetCheckBox_StatusAsync 추가
3. `OfrService.cs` - 저수준 비트맵 함수 4개 복구 및 버그 수정
4. `ImageToCheckState.xaml.cs` - 대화상자 복구 및 개선
5. `InsungsAct_RcptRegPage.cs` - 공유 CheckBox 구현
6. `OfrModels.cs` - OfrModel_BmpTextAnalysis 삭제

### 변경 사항 요약
- **추가**: 약 200줄
- **수정**: 약 150줄
- **삭제**: 약 50줄 (주석 제거, 구버전 코드)

## 참고사항

### 디버그 모드 활성화
```csharp
CommonVars.s_bDebugMode = true;
```

### 체크박스 학습 방법
1. 디버그 모드로 실행
2. CheckBox 인식 실패 시 ImageToCheckState 대화상자 자동 표시
3. ComboBox에서 "Checked" 또는 "Unchecked" 선택
4. "실행" 버튼 클릭하여 DB 저장

### 영역 설정 팁
- 체크박스 테두리 포함 (Unchecked 인식 위해)
- 18x18 픽셀 크기 (인성 접수등록 시스템 기준)
- 상대 좌표 사용 (윈도우 위치 독립)
