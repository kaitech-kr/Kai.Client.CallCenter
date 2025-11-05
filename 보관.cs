using System.Diagnostics;
using System.Threading.Tasks;

public static async Task<OfrResult_TbCharSetList> OfrStr_ComplexCharSetAsync(
    Draw.Bitmap bmpOrg, Draw.Rectangle rcSpare, bool bSaveToTbText = false, bool bEdit = true, bool bWrite = true, bool bMsgBox = true)
{
    if (bmpOrg == null)
        return new OfrResult_TbCharSetList(null, "bmpOrg이 null입니다", "OfrWork_Common/OfrStr_ComplexCharSetAsync_01");

    OfrResult_TbCharSetList result = new OfrResult_TbCharSetList(bmpOrg);

    // Stage 1: TbText 전체 매칭 시도
    // rcSpare 영역 추출
    Draw.Bitmap bmpSpare = OfrService.GetBitmapInBitmapFast(bmpOrg, rcSpare);
    if (bmpSpare == null)
    {
        result.sErr = "rcSpare 비트맵 추출 실패";
        result.sPos = "OfrWork_Common/OfrStr_ComplexCharSetAsync_02";
        return ErrMsgResult_TbCharSetList(result, bWrite, bMsgBox);
    }

    byte byteAvgBrightness = OfrService.GetAverageBrightness_FromColorBitmapFast(bmpSpare);

    Draw.Rectangle? rcForeground =
        OfrService.GetForeGroundDrawRectangle_FromColorBitmapFast(bmpSpare, byteAvgBrightness, 0);

    if (rcForeground == null || rcForeground.Value.Width < 1 || rcForeground.Value.Height < 1)
    {
        bmpSpare.Dispose();
        result.sErr = "rcForeground가 비어있습니다";
        result.sPos = "OfrWork_Common/OfrStr_ComplexCharSetAsync_03";
        return ErrMsgResult_TbCharSetList(result, bWrite, bMsgBox);
    }

    // 전경 영역에서 비트맵 분석
    Draw.Bitmap bmpForeground = OfrService.GetBitmapInBitmapFast(bmpSpare, rcForeground.Value);
    bmpSpare.Dispose();

    if (bmpForeground == null)
    {
        result.sErr = "전경 비트맵 추출 실패";
        result.sPos = "OfrWork_Common/OfrStr_ComplexCharSetAsync_04";
        return ErrMsgResult_TbCharSetList(result, bWrite, bMsgBox);
    }

    byte byteAvgBrightness2 = OfrService.GetAverageBrightness_FromColorBitmapFast(bmpForeground);
    OfrModel_BitmapAnalysis analyText = OfrService.GetBitmapAnalysisFast(bmpForeground, byteAvgBrightness2);

    if (analyText == null || analyText.trueRate == 0 || analyText.trueRate == 1)
    {
        bmpForeground.Dispose();
        result.sErr = $"trueRate가 {analyText?.trueRate}입니다 (0 또는 1은 유효하지 않음)";
        result.sPos = "OfrWork_Common/OfrStr_ComplexCharSetAsync_04";
        return ErrMsgResult_TbCharSetList(result, bWrite, bMsgBox);
    }

    // TbText에서 전체 문자열 검색
    PgResult_TbText findResult = await PgService_TbText.SelectRowByBasicAsync(
        analyText.nWidth, analyText.nHeight, analyText.sHexArray);

    if (findResult != null && findResult.tbText != null && findResult.tbText.Text != null)
    {
        // Stage 1 성공!
        bmpForeground.Dispose();
        result.strResult = findResult.tbText.Text;
        //Debug.WriteLine($"[OfrWork_Common] Stage 1 성공: {result.strResult}");
        return result;
    }

    // Stage 2-4: RightSliding 알고리즘으로 fallback
    //Debug.WriteLine($"[OfrWork_Common] Stage 1 실패. Stage 2-4 (RightSliding) 시작");

    // 1. 델리게이트 함수 생성 (TbCharBackup 단일 문자 검색용 - TbChar rebuilding 중)
    OfrCharSearchDelegate searchFunc = async (int width, int height, string hexString) =>
    {
        PgResult_TbCharBackup charResult = await PgService_TbCharBackup.SelectRowByBasicAsync(width, height, hexString);

        if (charResult != null && charResult.tbCharBackup != null &&
            !string.IsNullOrEmpty(charResult.tbCharBackup.Character) &&
            charResult.tbCharBackup.Character.Length == 1)
        {
            return new OfrCharSearchResult(charResult.tbCharBackup.Character[0], width, height);
        }

        return new OfrCharSearchResult(); // Found=false
    };

    // 2. RecognizeCharSetAsync_RightSliding 호출
    OfrResult_Recognition recognitionResult =
        await Ofr_CharSet_Core.RecognizeCharSetAsync_RightSliding(bmpForeground, searchFunc);

    bmpForeground.Dispose(); // 사용 완료

    // 3. 결과를 OfrResult_TbCharSetList로 변환
    if (recognitionResult != null && !string.IsNullOrEmpty(recognitionResult.strResult))
    {
        result.strResult = recognitionResult.strResult;
        Debug.WriteLine($"[OfrWork_Common] Stage 2-4 성공: {result.strResult}");

        // 4. bSaveToTbText=true이고 완전 성공(☒ 없음) 시 TbText에 저장
        if (bSaveToTbText && !result.strResult.Contains('☒'))
        {
            TbText newTbText = new TbText
            {
                Text = result.strResult,
                Width = analyText.nWidth,
                Height = analyText.nHeight,
                HexStrValue = analyText.sHexArray,
                Threshold = 0,
                Searched = 1,
                Reserved = ""
            };

            StdResult_Long saveResult = await PgService_TbText.InsertRowAsync(newTbText);
            if (saveResult.lResult > 0)
            {
                Debug.WriteLine($"[OfrWork_Common] TbText 저장 성공: {result.strResult}");
            }
            else
            {
                Debug.WriteLine($"[OfrWork_Common] TbText 저장 실패: {saveResult.sErr}");
            }
        }

        return result;
    }

    // Stage 2-4 실패
    result.sErr = $"Stage 2-4 실패: {recognitionResult?.sErr}";
    result.sPos = "OfrWork_Common/OfrStr_ComplexCharSetAsync_05";
    //Debug.WriteLine($"[OfrWork_Common] Stage 2-4 실패: {result.sErr}");

    return ErrMsgResult_TbCharSetList(result, bWrite, bMsgBox);
}
