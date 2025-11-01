# RightSliding 재귀 로직 개선 작업

## 목적
"123456789"처럼 많은 문자가 붙어있을 때 재귀적으로 처리하도록 개선

## 현재 문제
- RightSliding 2단계에서 나머지 영역을 통째로 DB 검색
- 여러 문자가 붙어있으면 실패 → "☒9" 같은 부분 실패 결과

## 해결 방안
**가로/세로 비율 기준으로 처리 방식 결정 (1.25배)**

```csharp
나머지 영역: width x height

if (width <= height * 1.25)
{
    // 단일 문자 가능성 → 전경/배경 방식 먼저 시도
    // 실패 시 → 재귀 RightSliding
}
else
{
    // 여러 문자 확실 (width > height * 1.25) → 바로 재귀 RightSliding
}
```

**예시:**
- 세로 10, 가로 12 이하 → 전경/배경 먼저
- 세로 10, 가로 13 이상 → RightSliding

## 수정 위치
**파일:** `D:\CodeWork\WithVs2022\KaiWork\Kai.Common\Kai.Common.NetDll_WpfCtrl\Kai.Common.NetDll_WpfCtrl\NetOFR\Ofr_CharSet_Core.cs`

**함수:** `RecognizeCharSetAsync_RightSliding`

**Line:** 584-655 (2단계 부분)

## 수정 내용

### 1. Line 584 이후에 비율 체크 추가
```csharp
if (rcForeground2 != null && rcForeground2.Value.Width >= 1 && rcForeground2.Value.Height >= 1)
{
    // 2-1. 가로/세로 비율 확인 (단일 문자 vs 복수 문자)
    bool isSingleChar = rcForeground2.Value.Width <= rcForeground2.Value.Height * 1.25;
    Debug.WriteLine($"[RightSliding 2단계] 나머지 영역 크기: {rcForeground2.Value.Width}x{rcForeground2.Value.Height}, 비율: {(double)rcForeground2.Value.Width / rcForeground2.Value.Height:F2}, 단일문자 가능성: {isSingleChar}");
```

### 2. 단일 문자 가능성 (width <= height * 1.25)
```csharp
if (isSingleChar)
{
    // 2-2-A. 단일 문자 가능성 → 전경/배경 방식 먼저 시도
    // (기존 코드 유지: Line 586-633)

    // 전경/배경 실패 시 재귀 추가 (Line 634-640 대체)
    if (!searchResult2.Found)
    {
        Debug.WriteLine($"[RightSliding 2단계-A 실패] 전경/배경 방식 DB 검색 실패 → RightSliding 재시도");

        OfrResult_Recognition recursiveResult = await RecognizeCharSetAsync_RightSliding(bmpRemain, searchFunc);

        if (!string.IsNullOrEmpty(recursiveResult.strResult))
        {
            result.strResult = recursiveResult.strResult + lastChar.ToString();
            result.CharRects.AddRange(recursiveResult.CharRects);
            result.CharRects.Add(lastRect);
            Debug.WriteLine($"[RightSliding 2단계-A 재귀 성공] RightSliding으로 '{recursiveResult.strResult}' 인식");
        }
        else
        {
            result.strResult = "☒" + lastChar.ToString();
            result.CharRects.Add(new Rectangle(remainX, 0, remainWidth, totalHeight));
            result.CharRects.Add(lastRect);
            Debug.WriteLine($"[RightSliding 2단계-A 재귀 실패] RightSliding도 실패");
        }
    }
}
```

### 3. 여러 문자 확실 (width > height * 1.25)
```csharp
else
{
    // 2-2-B. 여러 문자 확실 → 바로 재귀 RightSliding
    Debug.WriteLine($"[RightSliding 2단계-B] 여러 문자 확실 → 재귀 RightSliding 호출");

    OfrResult_Recognition recursiveResult = await RecognizeCharSetAsync_RightSliding(bmpRemain, searchFunc);

    if (!string.IsNullOrEmpty(recursiveResult.strResult))
    {
        result.strResult = recursiveResult.strResult + lastChar.ToString();
        result.CharRects.AddRange(recursiveResult.CharRects);
        result.CharRects.Add(lastRect);
        Debug.WriteLine($"[RightSliding 2단계-B 성공] 재귀 RightSliding으로 '{recursiveResult.strResult}' 인식");
    }
    else
    {
        result.strResult = "☒" + lastChar.ToString();
        result.CharRects.Add(new Rectangle(remainX, 0, remainWidth, totalHeight));
        result.CharRects.Add(lastRect);
        Debug.WriteLine($"[RightSliding 2단계-B 실패] 재귀 RightSliding 실패");
    }
}
```

## 참고
- Edit 도구로 수정 시도했으나 한글 인코딩 문제로 실패
- 수동으로 코드 수정 필요
- 기본수칙: 한 번에 한 개의 함수만 작업

## 다음 단계
1. Ofr_CharSet_Core.cs 파일 수정
2. 빌드 및 테스트
3. OfrStr_SeqCharAsync에 RightSliding fallback 추가 (문자 분리 실패 시)
