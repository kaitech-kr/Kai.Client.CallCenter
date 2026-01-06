using Kai.Client.CallCenter.Windows;
using Kai.Common.NetDll_WpfCtrl.NetOFR;
using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;
using Kai.Server.Main.KaiWork.DBs.Postgres.CharDB.Models;
using Kai.Server.Main.KaiWork.DBs.Postgres.CharDB.Results;
using Kai.Server.Main.KaiWork.DBs.Postgres.CharDB.Services;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Windows;
using static Kai.Client.CallCenter.Classes.CommonVars;
using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgWnd;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using Draw = System.Drawing;
using Medias = System.Windows.Media;
using Wnd = System.Windows;

namespace Kai.Client.CallCenter.OfrWorks;
#nullable disable
public class OfrWork_Common
{
    #region Config
    // 텍스트 캐시 최대 크기 (하루 업무량 기준)
    private const int MAX_TEXT_CACHE_SIZE = 20000;

    // 텍스트 캐시 (전체 텍스트용) - Key: "{width}x{height}_{hexArray}", Value: Text
    private static ConcurrentDictionary<string, string> s_TextCache = new ConcurrentDictionary<string, string>();
    #endregion

    #region Cache Management
    // 텍스트 캐시 초기화
    public static void ClearTextCache() => s_TextCache.Clear();

    // 텍스트 캐시 조회 (HIT 시 캐시된 텍스트 반환)
    private static bool TryGetTextCache(OfrModel_BitmapAnalysis analyText, out string cachedText)
    {
        string cacheKey = $"{analyText.nWidth}x{analyText.nHeight}_{analyText.sHexArray}";
        return s_TextCache.TryGetValue(cacheKey, out cachedText);
    }

    // 텍스트 캐시 저장 (크기 제한 체크 포함)
    private static void SaveTextCache(OfrModel_BitmapAnalysis analyText, string text)
    {
        string cacheKey = $"{analyText.nWidth}x{analyText.nHeight}_{analyText.sHexArray}";

        // 캐시 크기 제한 체크
        if (s_TextCache.Count >= MAX_TEXT_CACHE_SIZE)
        {
            Debug.WriteLine($"[Cache LIMIT] TextCache 한계 도달! 현재: {s_TextCache.Count}/{MAX_TEXT_CACHE_SIZE}");
            MsgBox($"텍스트 캐시가 한계({MAX_TEXT_CACHE_SIZE}개)에 도달했습니다.\n\n개발자에게 문의하세요.");
            return; // 저장하지 않음
        }

        s_TextCache[cacheKey] = text;
    }

    private static async Task SaveToTbChar(Draw.Bitmap bmpSource, Draw.Rectangle rcChar, string charValue)
    {
        // 명도별(60~254)로 TbChar 저장
        Draw.Bitmap bmpChar = OfrService.GetBitmapInBitmapFast(bmpSource, rcChar);
        if (bmpChar == null)
        {
            Debug.WriteLine($"[TbChar 저장 실패] 비트맵 추출 실패");
            return;
        }

        byte minThreshold = 60;
        byte maxThreshold = 254;
        HashSet<string> savedHexStrings = new HashSet<string>(); // 중복 방지
        int savedCount = 0;

        for (byte threshold = minThreshold; threshold <= maxThreshold; threshold++)
        {
            OfrModel_BitmapAnalysis analysis = OfrService.GetBitmapAnalysisFast(bmpChar, threshold);

            // trueRate > 0: 전부 흰색(0)만 제외, 작은 문자도 저장 가능
            if (analysis != null && analysis.sHexArray != null && analysis.trueRate > 0)
            {
                // 중복된 HexString은 건너뛰기
                if (savedHexStrings.Contains(analysis.sHexArray))
                    continue;

                savedHexStrings.Add(analysis.sHexArray);

                // DB에 이미 존재하는지 확인 (중복 키 예외 방지)
                var existingResult = await PgService_TbChar.SelectRowByBasicAsync(analysis.nWidth, analysis.nHeight, analysis.sHexArray);
                if (existingResult?.tbChar != null)
                    continue;

                TbChar newChar = new TbChar
                {
                    Character = charValue,
                    Width = analysis.nWidth,
                    Height = analysis.nHeight,
                    HexStrValue = analysis.sHexArray,
                    Threshold = threshold
                };

                StdResult_Long saveResult = await PgService_TbChar.InsertRowAsync(newChar);
                if (saveResult.lResult > 0)
                {
                    savedCount++;
                }
            }
        }

        bmpChar.Dispose();

        Debug.WriteLine($"[TbChar 저장 완료] '{charValue}' ({savedCount}개 명도, 고유={savedHexStrings.Count}개)");
    }

    // TbTextFail에 인식 실패 텍스트 저장 (중복 체크 포함)
    //private static async Task SaveToTbTextFail(OfrModel_BitmapAnalysis modelText, string failReason)
    //{
    //    try
    //    {
    //        // DB에 이미 존재하는지 확인 (중복 방지)
    //        var existingResult = await PgService_TbTextFail.SelectRowByBasicAsync(modelText.nWidth, modelText.nHeight, modelText.sHexArray);
    //        if (existingResult?.tbTextFail != null)
    //            return; // 이미 존재하면 저장 안 함

    //        // TbTextFail에 저장 (인식 실패 기록)
    //        TbTextFail newTextFail = new TbTextFail
    //        {
    //            Text = null,
    //            Width = modelText.nWidth,
    //            Height = modelText.nHeight,
    //            HexStrValue = modelText.sHexArray,
    //            Threshold = modelText.threshold,
    //            Searched = 1
    //        };

    //        StdResult_Long saveResult = await PgService_TbTextFail.InsertRowAsync(newTextFail);
    //        if (saveResult.lResult > 0)
    //        {
    //            Debug.WriteLine($"[TbTextFail 저장 성공] '{failReason}' ({modelText.nWidth}x{modelText.nHeight})");
    //        }
    //        else
    //        {
    //            Debug.WriteLine($"[TbTextFail 저장 실패] {saveResult.sErr}");
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.WriteLine($"[TbTextFail 저장 예외] {ex.Message}");
    //    }
    //}

    private static async Task<string> ShowImageToCharDialog(Draw.Bitmap bmpSource, Draw.Rectangle rcChar, string failReason)
    {
        string result = null;

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            ImageToCharWnd wnd = new ImageToCharWnd(bmpSource, rcChar, failReason);
            wnd.ShowDialog();

            if (wnd.IsConfirmed && !string.IsNullOrEmpty(wnd.UserInput))
            {
                result = wnd.UserInput;
            }
        });

        return result;
    }
    #endregion

    // TbChar에서 문자 검색 (검색 성공 시 Searched 카운트 증가)
    private static async Task<string> SelectCharByBasicAsync(int width, int height, string hexString)
    {
        PgResult_TbChar result = await PgService_TbChar.SelectRowByBasicAsync(width, height, hexString);
        if (result?.tbChar != null)
        {
            // Searched가 10보다 작으면 1 증가하여 업데이트
            if (result.tbChar.Searched < 10)
            {
                result.tbChar.Searched++;
                _ = PgService_TbChar.UpdateRowAsync(result.tbChar);
            }
            return result.tbChar.Character;
        }
        return null;
    }

    // TbText
    //public static async Task<OfrResult_TbText> OfrImage_LooseDrawRelRectAsync(Draw.Bitmap bmpLoose)
    //{
    //    byte byteAvgBrightness = 0;
    //    OfrModel_BmpTextAnalysis analy = null;
    //    PgResult_TbText resultPg = null;

    //    for (int i = 0; i < c_nRepeatShort; i++)
    //    {
    //        // Get Basic Hex Info
    //        byteAvgBrightness = OfrService.GetAverageBrightness_FromColorBitmapFast(bmpLoose);
    //        analy = OfrService.GetOfrModel_TextAnalysis_InLooseBitmapFast(bmpLoose, byteAvgBrightness);

    //        if (analy.trueRate == 0 || analy.trueRate == 1) goto REPEAT;

    //        resultPg = await PgService_TbText.SelectRowByBasicAsync(analy.nWidth, analy.nHeight, analy.sHexArray);
    //        if (resultPg.tbText != null) return new OfrResult_TbText(resultPg.tbText, analy);
    //        //MsgBox($"{analy.nWidth}, {analy.nHeight}, {analy.sHexArray} -> {resultPg.tbText.Text}"); // Test

    //    REPEAT:;
    //        await Task.Delay(100);
    //    }

    //    // Check TrueRate
    //    if (analy.trueRate == 0 || analy.trueRate == 1) return LocalCommon_OfrResult
    //            .ErrMsgResult_TbText(analy, $"info.trueRate가 {analy.trueRate}입니다", "OfrWork_Common/OfrImage_DrawRelRectAsync_02");

    //    ImageToMatchedTextWnd wnd = null;
    //    await Wnd.Application.Current.Dispatcher.InvokeAsync(() =>
    //    {
    //        wnd = new ImageToMatchedTextWnd("OfrWork_Insungs/OfrFindMacthedImage_ByHandleAsync_03", analy);
    //        wnd.ShowDialog();
    //    });

    //    if (wnd.Result != null) return wnd.Result;

    //    return LocalCommon_OfrResult.ErrMsgResult_TbText(analy, 
    //        $"tb 널 입니다: {analy.nWidth}, {analy.nHeight}, {analy.sHexArray} ->", "OfrWork_Common/OfrImage_DrawRelRectAsync_03");
    //}

    // Exact Bitmap에서 TbText를 찾는 함수 (반복하지 않음, 호출 측에서 반복 수행 필요)
    public static async Task<OfrResult_TbText> OfrImage_ExactDrawRelRectAsync(Draw.Bitmap bmpExact, bool bEdit, string sWantedStr = null)
    {
        // Get Basic Hex Info (한 번만 분석)
        byte byteAvgBrightness = OfrService.GetAverageBrightness_FromColorBitmapFast(bmpExact);
        OfrModel_BitmapAnalysis analyText = OfrService.GetBitmapAnalysisFast(bmpExact, byteAvgBrightness);

        // Check TrueRate (조기 검증)
        if (analyText == null || analyText.trueRate == 0)
        {
            string sErr = $"trueRate가 {analyText?.trueRate}입니다 (0 은 유효하지 않음)";
            string sPos = "OfrWork_Common/OfrImage_ExactDrawRelRectAsync_01";
            return new OfrResult_TbText(analyText, sErr, sPos, s_sLogDir);
        }

        // DB에서 찾기 (한 번만)
        PgResult_TbText resultText = await PgService_TbText.SelectRowByBasicAsync(analyText.nWidth, analyText.nHeight, analyText.sHexArray);

        if (resultText != null && resultText.tbText != null && resultText.tbText.Text != null)
        {
            // Searched가 10보다 작으면 1 증가하여 업데이트
            if (resultText.tbText.Searched < 10)
            {
                resultText.tbText.Searched++;
                _ = PgService_TbText.UpdateRowAsync(resultText.tbText);
            }
            return new OfrResult_TbText(resultText.tbText, analyText);
        }

        // Debug 모드 + bEdit=true일 때만 수동 입력 다이얼로그 표시
        if (s_bDebugMode && bEdit)
        {
            await Wnd.Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                ImageToMatchedTextWnd wnd = new ImageToMatchedTextWnd("OfrWork_Common/OfrImage_ExactDrawRelRectAsync_02", analyText, sWantedStr);
                wnd.ShowDialog();

                if (wnd.resultBool != null && wnd.resultBool.bResult)
                {
                    // DB 저장 성공 - DB에서 다시 찾기
                    resultText = await PgService_TbText.SelectRowByBasicAsync(analyText.nWidth, analyText.nHeight, analyText.sHexArray);
                }
            });

            if (resultText != null && resultText.tbText != null && resultText.tbText.Text != null)
            {
                Debug.WriteLine($"[OfrWork_Common] 수동 입력 후 DB 재조회 성공: {resultText.tbText.Text}");
                return new OfrResult_TbText(resultText.tbText, analyText);
            }
        }

        string sFinalErr = $"DB에서 텍스트를 찾을 수 없습니다: {analyText.nWidth}x{analyText.nHeight}, Hex={analyText.sHexArray}";
        string sFinalPos = "OfrWork_Common/OfrImage_ExactDrawRelRectAsync_03";

        return new OfrResult_TbText(analyText, sFinalErr, sFinalPos, s_sLogDir);
    }

    // GetStartEndList 기반 복합문자 인식 (우측→좌측 방향, 부분 성공 허용)
    private static async Task<(string[] results, bool[] processed, List<OfrModel_StartEnd> listStartEnd)> RecognizeByStartEndList_RightToLeftAsync(
        Draw.Bitmap bmpSource, byte avgBright, Draw.Rectangle rcFore)
    {
        // 1. 음소 분리
        List<OfrModel_StartEnd> listStartEnd = OfrService.GetStartEndList_FromColorBitmap(bmpSource, avgBright, rcFore);
        if (listStartEnd == null || listStartEnd.Count == 0)
            return (null, null, null);

        int count = listStartEnd.Count;
        string[] results = new string[count]; // 각 인덱스별 인식 결과
        bool[] processed = new bool[count];   // 처리 완료 마킹

        // 2. 우측→좌측 방향으로 처리
        for (int x = count - 1; x >= 0; x--)
        {
            if (processed[x]) continue; // 이미 처리됨

            string foundChar = null;
            int consumed = 0;

            // 3→2→1개 순서로 시도 (우측에서 좌측으로 확장)
            for (int len = Math.Min(3, x + 1); len >= 1; len--)
            {
                int startIdx = x - len + 1;

                // 이미 처리된 인덱스가 포함되어 있으면 건너뜀
                bool hasProcessed = false;
                for (int i = startIdx; i <= x; i++)
                {
                    if (processed[i]) { hasProcessed = true; break; }
                }
                if (hasProcessed) continue;

                StdConst_IndexRect rcIndex = OfrService.GetIndexRect_FromColorBitmapByIndex(
                    bmpSource, avgBright, rcFore, listStartEnd, startIdx, x);

                if (rcIndex == null) continue;

                Draw.Rectangle rcChar = rcIndex.GetDrawRectangle();

                // 최소 너비 체크 (너무 작은 영역은 무시) - '1' 같은 좁은 문자 허용
                if (rcChar.Width < 2) continue;

                OfrModel_BitmapAnalysis model = OfrService.GetBitmapAnalysisFast(bmpSource, rcChar, avgBright);

                if (model == null) continue;

                // DB 검색
                Debug.WriteLine($"[StartEndList 검색] len={len}, idx={startIdx}~{x}, rcChar={rcChar}, W={model.nWidth},H={model.nHeight},Hex={model.sHexArray}");
                PgResult_TbChar result = await PgService_TbChar.SelectRowByBasicAsync(
                    model.nWidth, model.nHeight, model.sHexArray);
                Debug.WriteLine($"[StartEndList 검색결과] {(result?.tbChar != null ? $"찾음: '{result.tbChar.Character}'" : "없음")}");

                if (result?.tbChar != null && !string.IsNullOrEmpty(result.tbChar.Character))
                {
                    foundChar = result.tbChar.Character;
                    consumed = len;
                    break;
                }
            }

            if (foundChar != null)
            {
                // 처리된 인덱스 마킹
                int startIdx = x - consumed + 1;
                Debug.WriteLine($"[StartEndList 마킹] foundChar='{foundChar}', x={x}, consumed={consumed}, startIdx={startIdx}");
                for (int i = startIdx; i <= x; i++)
                {
                    processed[i] = true;
                    Debug.WriteLine($"  processed[{i}]=true");
                }
                results[startIdx] = foundChar; // 시작 인덱스에 결과 저장
                x -= (consumed - 1); // 소비한 만큼 인덱스 감소
            }
            else
            {
                Debug.WriteLine($"[StartEndList] x={x}, foundChar=null → 마킹 안 함");
                // ========================================
                // 백트래킹: 실패 시 인접 처리된 인덱스 취소 후 재시도
                // ========================================
                // 우측에 처리된 인덱스가 있으면 취소하고 합쳐서 재시도
                if (x + 1 < count && processed[x + 1])
                {
                    // 우측에서 연속으로 처리된 범위 찾기
                    int rightEnd = x + 1;
                    while (rightEnd + 1 < count && processed[rightEnd + 1])
                        rightEnd++;

                    // 원래 값 백업
                    var backupProcessed = new bool[rightEnd - x];
                    var backupResults = new string[rightEnd - x];
                    for (int i = x + 1; i <= rightEnd; i++)
                    {
                        backupProcessed[i - x - 1] = processed[i];
                        backupResults[i - x - 1] = results[i];
                    }

                    // 처리 취소
                    for (int i = x + 1; i <= rightEnd; i++)
                    {
                        processed[i] = false;
                        results[i] = null;
                    }

                    // x부터 rightEnd까지 합쳐서 재시도 (최대 3개)
                    bool backtrackSuccess = false;
                    int totalLen = rightEnd - x + 1;
                    for (int len = Math.Min(3, totalLen); len >= 2; len--)
                    {
                        int endIdx = x + len - 1;
                        if (endIdx > rightEnd) continue;

                        StdConst_IndexRect rcIndex = OfrService.GetIndexRect_FromColorBitmapByIndex(
                            bmpSource, avgBright, rcFore, listStartEnd, x, endIdx);

                        if (rcIndex == null) continue;

                        Draw.Rectangle rcChar = rcIndex.GetDrawRectangle();

                        // 최소 너비 체크 (너무 작은 영역은 무시)
                        if (rcChar.Width < 3) continue;

                        OfrModel_BitmapAnalysis model = OfrService.GetBitmapAnalysisFast(bmpSource, rcChar, avgBright);

                        if (model == null) continue;

                        // DB 검색
                        PgResult_TbChar result = await PgService_TbChar.SelectRowByBasicAsync(
                            model.nWidth, model.nHeight, model.sHexArray);

                        if (result?.tbChar != null && !string.IsNullOrEmpty(result.tbChar.Character))
                        {
                            // 백트래킹 성공
                            Debug.WriteLine($"[백트래킹 성공] x={x}, len={len}, char='{result.tbChar.Character}'");
                            for (int i = x; i <= endIdx; i++)
                                processed[i] = true;
                            results[x] = result.tbChar.Character;
                            backtrackSuccess = true;

                            // 나머지 우측 영역은 처리 안 된 상태로 유지 (다음 반복에서 처리)
                            break;
                        }
                    }

                    // 백트래킹 실패 시 원래 값 복원
                    if (!backtrackSuccess)
                    {
                        Debug.WriteLine($"[백트래킹 실패] x={x}, 원래 값 복원");
                        for (int i = x + 1; i <= rightEnd; i++)
                        {
                            processed[i] = backupProcessed[i - x - 1];
                            results[i] = backupResults[i - x - 1];
                        }
                    }
                }
            }
            // 실패해도 계속 진행 (부분 성공 허용)
        }

        return (results, processed, listStartEnd);
    }

    // 순차 문자 인식 (단음소) - 숫자 문자열 전용, 텍스트 캐시 적용
    public static async Task<StdResult_String> OfrStr_SeqCharAsync(Draw.Bitmap bmpSource, double weightBrightness, bool bEdit = true)
    {
        Stopwatch sw = Stopwatch.StartNew();

        try
        {
            byte avgBright = OfrService.GetAverageBrightness_FromColorBitmapFast(bmpSource);
            Draw.Rectangle rcFull = new Draw.Rectangle(0, 0, bmpSource.Width, bmpSource.Height);
            Draw.Rectangle rcFore = OfrService.GetForeGroundDrawRectangle_FromColorBitmapRectFast(bmpSource, rcFull, avgBright, 0);

            if (rcFore == StdUtil.s_rcDrawEmpty)
                return new StdResult_String("전경 영역 없음", "OfrStr_SeqCharAsync_01");

            // ========================================
            // 1. 텍스트 캐시 조회 (전체 전경 영역 기준)
            // ========================================
            Draw.Bitmap bmpFore = OfrService.GetBitmapInBitmapFast(bmpSource, rcFore);
            if (bmpFore == null)
                return new StdResult_String("전경 비트맵 추출 실패", "OfrStr_SeqCharAsync_01_2");

            byte avgBright2 = OfrService.GetAverageBrightness_FromColorBitmapFast(bmpFore, weightBrightness);
            OfrModel_BitmapAnalysis analyText = OfrService.GetBitmapAnalysisFast(bmpFore, avgBright2);
            bmpFore.Dispose();

            if (analyText == null || string.IsNullOrEmpty(analyText.sHexArray))
                return new StdResult_String("전경 비트맵 분석 실패", "OfrStr_SeqCharAsync_01_3");

            // 캐시 HIT
            if (TryGetTextCache(analyText, out string cachedText))
            {
                sw.Stop();
                //Debug.WriteLine($"[Cache HIT] OfrStr_SeqCharAsync: '{cachedText}' ({sw.ElapsedMilliseconds}ms) - Cache: {s_TextCache.Count}/{MAX_TEXT_CACHE_SIZE}");
                return new StdResult_String(cachedText);
            }

            // ========================================
            // 2. 캐시 MISS → 전경/배경 방식으로 문자 분리
            // ========================================
            List<OfrModel_StartEnd> charList = OfrService.GetStartEndList_FromColorBitmap(bmpSource, avgBright2, rcFore);
            if (charList == null || charList.Count == 0)
                return new StdResult_String("문자 분리 실패", "OfrStr_SeqCharAsync_02");

            int charCount = charList.Count;
            string[] results = new string[charCount];
            bool[] processed = new bool[charCount];

            for (int i = 0; i < charCount; i++)
            {
                StdConst_IndexRect rcIndex = OfrService.GetIndexRect_FromColorBitmapByIndex(bmpSource, avgBright2, rcFore, charList, i, i);
                if (rcIndex == null)
                    return new StdResult_String($"문자{i + 1} 영역 실패", "OfrStr_SeqCharAsync_03");

                Draw.Rectangle rcChar = rcIndex.GetDrawRectangle();
                OfrModel_BitmapAnalysis modelChar = OfrService.GetBitmapAnalysisFast(bmpSource, rcChar, avgBright2);
                if (modelChar == null)
                    return new StdResult_String($"문자{i + 1} 분석 실패", "OfrStr_SeqCharAsync_04");

                string character = await SelectCharByBasicAsync(modelChar.nWidth, modelChar.nHeight, modelChar.sHexArray);
                if (character != null)
                {
                    results[i] = character;
                    processed[i] = true;
                }
            }

            // ========================================
            // 3. 미처리 영역에 RightSliding 시도
            // ========================================
            bool hasUnprocessed = processed.Any(p => !p);

            if (hasUnprocessed)
            {
                // 미처리 그룹 계산
                List<(int start, int end)> unprocessedGroups = new List<(int, int)>();
                int groupStart = -1;
                for (int i = 0; i < charCount; i++)
                {
                    if (!processed[i])
                    {
                        if (groupStart == -1) groupStart = i;
                    }
                    else
                    {
                        if (groupStart != -1)
                        {
                            unprocessedGroups.Add((groupStart, i - 1));
                            groupStart = -1;
                        }
                    }
                }
                if (groupStart != -1)
                    unprocessedGroups.Add((groupStart, charCount - 1));

                // RightSliding용 델리게이트
                OfrCharSearchDelegate searchFunc = async (int width, int height, string hexString) =>
                {
                    PgResult_TbChar result = await PgService_TbChar.SelectRowByBasicAsync(width, height, hexString);
                    if (result?.tbChar != null && !string.IsNullOrEmpty(result.tbChar.Character) && result.tbChar.Character.Length == 1)
                        return new OfrCharSearchResult(result.tbChar.Character[0], width, height);
                    return new OfrCharSearchResult();
                };

                // 각 미처리 그룹에 RightSliding 수행
                foreach (var group in unprocessedGroups)
                {
                    StdConst_IndexRect rcGroup = OfrService.GetIndexRect_FromColorBitmapByIndex(
                        bmpSource, avgBright2, rcFore, charList, group.start, group.end);
                    if (rcGroup == null) continue;

                    Draw.Rectangle rcGroupRect = rcGroup.GetDrawRectangle();
                    Draw.Bitmap bmpGroup = OfrService.GetBitmapInBitmapFast(bmpSource, rcGroupRect);
                    if (bmpGroup == null) continue;

                    OfrResult_Recognition groupResult = await Ofr_CharSet_Core.RecognizeCharSetAsync_RightSliding(bmpGroup, searchFunc, null, avgBright2);
                    bmpGroup.Dispose();

                    if (groupResult != null && !string.IsNullOrEmpty(groupResult.strResult) && !groupResult.strResult.Contains('☒'))
                    {
                        results[group.start] = groupResult.strResult;
                        for (int i = group.start; i <= group.end; i++)
                            processed[i] = true;
                        Debug.WriteLine($"[RightSliding 성공] 그룹[{group.start}~{group.end}]: '{groupResult.strResult}'");
                    }
                    groupResult?.SourceBitmap?.Dispose();
                }

                // ========================================
                // 4. 여전히 미처리인 영역 → 대화상자 (디버그 모드)
                // ========================================
                unprocessedGroups.Clear();
                groupStart = -1;
                for (int i = 0; i < charCount; i++)
                {
                    if (!processed[i])
                    {
                        if (groupStart == -1) groupStart = i;
                    }
                    else
                    {
                        if (groupStart != -1)
                        {
                            unprocessedGroups.Add((groupStart, i - 1));
                            groupStart = -1;
                        }
                    }
                }
                if (groupStart != -1)
                    unprocessedGroups.Add((groupStart, charCount - 1));

                if (unprocessedGroups.Count > 0 && bEdit && s_bDebugMode)
                {
                    Debug.WriteLine($"[미처리 그룹] {unprocessedGroups.Count}개 → 대화상자 호출");

                    foreach (var group in unprocessedGroups)
                    {
                        for (int i = group.start; i <= group.end; i++)
                        {
                            if (processed[i]) continue;

                            StdConst_IndexRect rcIndex = OfrService.GetIndexRect_FromColorBitmapByIndex(
                                bmpSource, avgBright2, rcFore, charList, i, i);
                            if (rcIndex == null)
                            {
                                results[i] = "☒";
                                processed[i] = true;
                                continue;
                            }

                            Draw.Rectangle rcChar = rcIndex.GetDrawRectangle();
                            Draw.Bitmap bmpChar = OfrService.GetBitmapInBitmapFast(bmpSource, rcChar);
                            if (bmpChar == null)
                            {
                                results[i] = "☒";
                                processed[i] = true;
                                continue;
                            }

                            Debug.WriteLine($"[단음소 실패] rcChar={rcChar}, bmpSource={bmpSource.Width}x{bmpSource.Height}");
                            //bmpSource.Save($"C:\\Users\\gsqui\\OneDrive\\사진\\스크린샷\\test_bmpSource_{i}.png");
                            //bmpChar.Save($"C:\\Users\\gsqui\\OneDrive\\사진\\스크린샷\\test_bmpChar_{i}.png");
                            string manualChar = await ShowImageToCharDialog(bmpSource, rcChar, "단음소 실패");
                            if (!string.IsNullOrEmpty(manualChar))
                            {
                                await SaveToTbChar(bmpChar, new Draw.Rectangle(0, 0, bmpChar.Width, bmpChar.Height), manualChar);
                                results[i] = manualChar;
                            }
                            else
                            {
                                results[i] = "☒";
                            }
                            processed[i] = true;
                            bmpChar.Dispose();
                        }
                    }
                }
                else if (unprocessedGroups.Count > 0)
                {
                    foreach (var group in unprocessedGroups)
                    {
                        for (int i = group.start; i <= group.end; i++)
                        {
                            if (!processed[i])
                            {
                                results[i] = "☒";
                                processed[i] = true;
                            }
                        }
                    }
                }
            }

            // 결과 조합
            StringBuilder sbFinal = new StringBuilder();
            for (int i = 0; i < charCount; i++)
            {
                if (!string.IsNullOrEmpty(results[i]))
                    sbFinal.Append(results[i]);
            }
            string resultText = sbFinal.ToString();

            if (string.IsNullOrEmpty(resultText) || resultText.Length == 0)
                return new StdResult_String("인식 문자 없음", "OfrStr_SeqCharAsync_06");

            // ========================================
            // 4. 성공 → 텍스트 캐시에 저장 (☒ 없을 때만)
            // ========================================
            if (!resultText.Contains('☒'))
            {
                SaveTextCache(analyText, resultText);
            }

            sw.Stop();
            // Debug.WriteLine($"[Cache MISS] OfrStr_SeqCharAsync: '{resultText}' ({sw.ElapsedMilliseconds}ms) - Cache: {s_TextCache.Count}/{MAX_TEXT_CACHE_SIZE}");

            return new StdResult_String(resultText);
        }
        catch (Exception ex)
        {
            return new StdResult_String(StdUtil.GetExceptionMessage(ex), "OfrStr_SeqCharAsync_999");
        }
        finally
        {
        }
    }

    // 페이지 비트맵 Rectangle 영역 crop + RGB 반전 후 OFR 인식 (오버로드)
    public static async Task<StdResult_String> OfrStr_SeqCharAsync(Draw.Bitmap bmpPage, Draw.Rectangle rect, bool bInvertRgb, double dWeight, bool bEdit = true)
    {
        Draw.Bitmap bmpCell = null;

        try
        {
            // 1. bmpPage에서 rect 영역 crop
            bmpCell = OfrService.GetBitmapInBitmapFast(bmpPage, rect);
            if (bmpCell == null) return new StdResult_String("셀 crop 실패", "OfrStr_SeqCharAsync_Rect_01");

            // 2. RGB 반전 필요하면
            if (bInvertRgb)
            {
                Draw.Bitmap bmpOriginal = bmpCell;
                bmpCell = OfrService.InvertBitmap(bmpOriginal);
                bmpOriginal.Dispose();
                //Debug.WriteLine($"[OfrStr_SeqCharAsync] RGB 반전 실행: {bmpCell.Width}x{bmpCell.Height}");
            }

            // 3. 기존 오버로드 호출
            return await OfrStr_SeqCharAsync(bmpCell, dWeight, bEdit);
        }
        finally
        {
            bmpCell?.Dispose();
        }
    }

    public static async Task<StdResult_String> OfrStr_ComplexCharSetAsync(Draw.Bitmap bmpSource, bool bTextSave, double dWeight, int maxCharCount = 0)
    {
        Stopwatch sw = Stopwatch.StartNew();

        try
        {
            byte avgBright = OfrService.GetAverageBrightness_FromColorBitmapFast(bmpSource);
            Draw.Rectangle rcFull = new Draw.Rectangle(0, 0, bmpSource.Width, bmpSource.Height);
            Draw.Rectangle rcFore = OfrService.GetForeGroundDrawRectangle_FromColorBitmapRectFast(bmpSource, rcFull, avgBright, 0);

            if (rcFore == StdUtil.s_rcDrawEmpty) return new StdResult_String("전경 영역 없음", "OfrStr_ComplexCharSetAsync_01");

            // ========================================
            // 1. 텍스트 캐시 조회 (전체 전경 영역 기준)
            // ========================================
            Draw.Bitmap bmpFore = OfrService.GetBitmapInBitmapFast(bmpSource, rcFore);
            if (bmpFore == null)
                return new StdResult_String("전경 비트맵 추출 실패", "OfrStr_ComplexCharSetAsync_01_2");

            byte avgBright2 = OfrService.GetAverageBrightness_FromColorBitmapFast(bmpFore, dWeight);
            OfrModel_BitmapAnalysis analyText = OfrService.GetBitmapAnalysisFast(bmpFore, avgBright2);

            if (analyText == null || string.IsNullOrEmpty(analyText.sHexArray) || analyText.trueRate == 0 || analyText.trueRate == 1)
            {
                bmpFore.Dispose();
                return new StdResult_String("전경 비트맵 분석 실패", "OfrStr_ComplexCharSetAsync_01_3");
            }

            // 캐시 HIT
            if (TryGetTextCache(analyText, out string cachedText))
            {
                bmpFore.Dispose();
                sw.Stop();
                //Debug.WriteLine($"[Cache HIT] OfrStr_ComplexCharSetAsync: '{cachedText}' ({sw.ElapsedMilliseconds}ms) - Cache: {s_TextCache.Count}/{MAX_TEXT_CACHE_SIZE}");
                return new StdResult_String(cachedText);
            }

            // ========================================
            // 2. 캐시 MISS → TbText 검색
            // ========================================
            PgResult_TbText findResult = await PgService_TbText.SelectRowByBasicAsync(analyText.nWidth, analyText.nHeight, analyText.sHexArray);

            if (findResult != null && findResult.tbText != null && !string.IsNullOrEmpty(findResult.tbText.Text))
            {
                // Searched가 10보다 작으면 1 증가하여 업데이트
                if (findResult.tbText.Searched < 10)
                {
                    findResult.tbText.Searched++;
                    _ = PgService_TbText.UpdateRowAsync(findResult.tbText);
                }

                // TbText HIT → 캐시에 저장
                string resultText = findResult.tbText.Text;
                SaveTextCache(analyText, resultText);
                bmpFore.Dispose();

                sw.Stop();
                //Debug.WriteLine($"[TbText HIT] OfrStr_ComplexCharSetAsync: '{resultText}' ({sw.ElapsedMilliseconds}ms)");
                return new StdResult_String(resultText);
            }

            // ========================================
            // 3. TbText MISS → StartEndList 기반 인식
            // ========================================
            Draw.Rectangle rcForeFull = new Draw.Rectangle(0, 0, bmpFore.Width, bmpFore.Height);
            var (results, processed, listStartEnd) = await RecognizeByStartEndList_RightToLeftAsync(bmpFore, avgBright2, rcForeFull);

            // 결과 확인 - 모든 인덱스가 처리되었는지
            bool allProcessed = true;
            int processedCount = 0;
            if (processed != null)
            {
                Debug.WriteLine($"[StartEndList] 음소 개수={processed.Length}");
                for (int i = 0; i < processed.Length; i++)
                {
                    if (processed[i]) processedCount++;
                    else allProcessed = false;
                    Debug.WriteLine($"  [{i}] processed={processed[i]}, result={results?[i] ?? "null"}");
                }
            }
            else
            {
                allProcessed = false;
                Debug.WriteLine($"[StartEndList] processed가 null");
            }

            // 모든 인덱스가 처리되었으면 완료
            if (allProcessed && results != null)
            {
                StringBuilder sbResult = new StringBuilder();
                for (int i = 0; i < results.Length; i++)
                {
                    if (!string.IsNullOrEmpty(results[i]))
                        sbResult.Append(results[i]);
                }

                string startEndResult = sbResult.ToString();
                if (!string.IsNullOrEmpty(startEndResult))
                {
                    SaveTextCache(analyText, startEndResult);

                    // DB에 이미 존재하는지 확인 (중복 키 예외 방지)
                    if (bTextSave)
                    {
                        var existingResult = await PgService_TbText.SelectRowByBasicAsync(analyText.nWidth, analyText.nHeight, analyText.sHexArray);
                        if (existingResult?.tbText == null)
                        {
                            TbText newTbText = new TbText
                            {
                                Text = startEndResult,
                                Width = analyText.nWidth,
                                Height = analyText.nHeight,
                                HexStrValue = analyText.sHexArray,
                                Threshold = 0,
                                Searched = 1,
                                Reserved = ""
                            };
                            await PgService_TbText.InsertRowAsync(newTbText);
                        }
                    }
                    bmpFore.Dispose();
                    sw.Stop();
                    Debug.WriteLine($"[StartEndList 완전성공] OfrStr_ComplexCharSetAsync: '{startEndResult}' ({sw.ElapsedMilliseconds}ms)");
                    return new StdResult_String(startEndResult);
                }
            }

            // ========================================
            // 4. 미처리 영역에 RightSliding
            // ========================================
            OfrCharSearchDelegate searchFunc = async (int width, int height, string hexString) =>
            {
                PgResult_TbChar charResult = await PgService_TbChar.SelectRowByBasicAsync(width, height, hexString);
                if (charResult?.tbChar != null && !string.IsNullOrEmpty(charResult.tbChar.Character) && charResult.tbChar.Character.Length == 1)
                    return new OfrCharSearchResult(charResult.tbChar.Character[0], width, height);
                return new OfrCharSearchResult();
            };

            if (processed != null && listStartEnd != null && results != null)
            {
                // 미처리 인덱스들을 연속 그룹으로 묶기
                List<(int start, int end)> unprocessedGroups = new List<(int, int)>();
                int groupStart = -1;
                for (int i = 0; i < processed.Length; i++)
                {
                    if (!processed[i])
                    {
                        if (groupStart == -1) groupStart = i;
                    }
                    else
                    {
                        if (groupStart != -1)
                        {
                            unprocessedGroups.Add((groupStart, i - 1));
                            groupStart = -1;
                        }
                    }
                }
                if (groupStart != -1)
                    unprocessedGroups.Add((groupStart, processed.Length - 1));

                // 미처리 그룹 로그
                Debug.WriteLine($"[미처리 그룹] 총 {unprocessedGroups.Count}개: {string.Join(", ", unprocessedGroups.Select(g => $"[{g.start}~{g.end}]"))}");
                for (int i = 0; i < processed.Length; i++)
                    Debug.WriteLine($"  processed[{i}]={processed[i]}, result={results[i] ?? "null"}");

                // 각 미처리 그룹에 대해 RightSliding 수행
                foreach (var group in unprocessedGroups)
                {
                    StdConst_IndexRect rcGroup = OfrService.GetIndexRect_FromColorBitmapByIndex(
                        bmpFore, avgBright2, rcForeFull, listStartEnd, group.start, group.end);
                    if (rcGroup == null) continue;

                    Draw.Rectangle rcGroupRect = rcGroup.GetDrawRectangle();
                    Draw.Bitmap bmpGroup = OfrService.GetBitmapInBitmapFast(bmpFore, rcGroupRect);
                    if (bmpGroup == null) continue;

                    OfrResult_Recognition groupResult = await Ofr_CharSet_Core.RecognizeCharSetAsync_RightSliding(bmpGroup, searchFunc, null, avgBright2);
                    bmpGroup.Dispose();

                    if (groupResult != null && !string.IsNullOrEmpty(groupResult.strResult) && !groupResult.strResult.Contains('☒'))
                    {
                        results[group.start] = groupResult.strResult;
                        for (int i = group.start; i <= group.end; i++)
                            processed[i] = true;
                    }
                    groupResult?.SourceBitmap?.Dispose();
                }

                // ========================================
                // 5. 여전히 미처리인 영역 처리 (모드별)
                // ========================================
                // 미처리 그룹 재계산
                unprocessedGroups.Clear();
                groupStart = -1;
                for (int i = 0; i < processed.Length; i++)
                {
                    if (!processed[i])
                    {
                        if (groupStart == -1) groupStart = i;
                    }
                    else
                    {
                        if (groupStart != -1)
                        {
                            unprocessedGroups.Add((groupStart, i - 1));
                            groupStart = -1;
                        }
                    }
                }
                if (groupStart != -1)
                    unprocessedGroups.Add((groupStart, processed.Length - 1));

                // maxCharCount > 0이면 좌측부터 N개 문자가 이미 인식됐는지 확인
                if (maxCharCount > 0 && unprocessedGroups.Count > 0)
                {
                    // 현재까지 인식된 결과 합치기 (공백/☒ 제외)
                    StringBuilder sbTemp = new StringBuilder();
                    for (int i = 0; i < results.Length; i++)
                    {
                        if (!string.IsNullOrEmpty(results[i]))
                            sbTemp.Append(results[i]);
                    }
                    string tempResult = sbTemp.ToString().Replace(" ", "").Replace("☒", "");

                    // 이미 N개 이상 인식됐으면 미처리 영역 무시 (대화상자 안 띄움)
                    if (tempResult.Length >= maxCharCount)
                    {
                        unprocessedGroups.Clear();
                    }
                }

                // 미처리 영역이 있으면 모드별 처리
                if (unprocessedGroups.Count > 0)
                {
                    if (!s_bDebugMode)
                    {
                        // UI 인식 모드: 에러 반환
                        bmpFore.Dispose();
                        return new StdResult_String("인식 실패 (UI 모드)", "OfrStr_ComplexCharSetAsync_UI_Fail");
                    }

                    // 좌→우 순서로 처리
                    foreach (var group in unprocessedGroups)
                    {
                        if (s_bDebugMode)
                        {
                            // 디버그 모드: 대화상자로 DB 작업 (1→2→3 순서)
                            int currentIdx = group.start;
                            while (currentIdx <= group.end)
                            {
                                bool found = false;

                                // 1→2→3 순서로 시도
                                for (int len = 1; len <= Math.Min(3, group.end - currentIdx + 1); len++)
                                {
                                    int endIdx = currentIdx + len - 1;

                                    StdConst_IndexRect rcIndex = OfrService.GetIndexRect_FromColorBitmapByIndex(
                                        bmpFore, avgBright2, rcForeFull, listStartEnd, currentIdx, endIdx);
                                    if (rcIndex == null) continue;

                                    Draw.Rectangle rcChar = rcIndex.GetDrawRectangle();
                                    Debug.WriteLine($"[대화상자] len={len}, currentIdx={currentIdx}, endIdx={endIdx}, rcChar={rcChar}");
                                    string manualInput = await ShowImageToCharDialog(bmpFore, rcChar, $"인식 실패 ({len}개 음소)");

                                    if (!string.IsNullOrEmpty(manualInput))
                                    {
                                        // DB 저장 (다음소 영역추출과 동일한 방식으로 HexString 생성)
                                        Debug.WriteLine($"[TbChar 저장 시도] manualInput='{manualInput}', rcChar={rcChar}, avgBright2={avgBright2}");

                                        OfrModel_BitmapAnalysis model = OfrService.GetBitmapAnalysisFast(bmpFore, rcChar, avgBright2);

                                        Debug.WriteLine($"[TbChar] model={(model != null ? $"W={model.nWidth},H={model.nHeight},Hex={model.sHexArray}" : "null")}");
                                        if (model != null)
                                        {
                                            // DB에 이미 존재하는지 확인 (중복 키 예외 방지)
                                            var existingChar = await PgService_TbChar.SelectRowByBasicAsync(model.nWidth, model.nHeight, model.sHexArray);
                                            Debug.WriteLine($"[TbChar] existingChar={(existingChar?.tbChar != null ? "이미 존재" : "없음")}");
                                            if (existingChar?.tbChar == null)
                                            {
                                                TbChar newChar = new TbChar
                                                {
                                                    Character = manualInput,
                                                    Width = model.nWidth,
                                                    Height = model.nHeight,
                                                    HexStrValue = model.sHexArray,
                                                    Threshold = avgBright2,
                                                    Searched = 1
                                                };
                                                var insertResult = await PgService_TbChar.InsertRowAsync(newChar);
                                                Debug.WriteLine($"[TbChar 저장 완료] '{manualInput}' → result={insertResult?.lResult}");
                                            }
                                        }
                                        else
                                        {
                                            Debug.WriteLine($"[TbChar 저장 실패] model이 null");
                                        }

                                        results[currentIdx] = manualInput;
                                        for (int i = currentIdx; i <= endIdx; i++)
                                            processed[i] = true;

                                        currentIdx = endIdx + 1;
                                        found = true;
                                        break;
                                    }
                                }

                                if (!found)
                                {
                                    // 1→2→3 모두 취소: 1개 음소로 ☒ 처리
                                    results[currentIdx] = "☒";
                                    processed[currentIdx] = true;
                                    currentIdx++;
                                }
                            }
                        }
                        else
                        {
                            // 편집 모드 + 비디버그 모드: ☒ 처리 + TbCharFail 저장
                            for (int i = group.start; i <= group.end; i++)
                            {
                                if (!processed[i])
                                {
                                    results[i] = "☒";
                                    processed[i] = true;
                                }
                            }
                        }
                    }
                }

                // 최종 결과 합치기
                StringBuilder sbFinal = new StringBuilder();
                for (int i = 0; i < results.Length; i++)
                {
                    if (!string.IsNullOrEmpty(results[i]))
                        sbFinal.Append(results[i]);
                }

                string finalResult = sbFinal.ToString();

                // 성공 시 캐시/TbText 저장
                if (!string.IsNullOrEmpty(finalResult) && !finalResult.Contains('☒'))
                {
                    SaveTextCache(analyText, finalResult);

                    // DB에 이미 존재하는지 확인 (중복 키 예외 방지)
                    var existingResult = await PgService_TbText.SelectRowByBasicAsync(analyText.nWidth, analyText.nHeight, analyText.sHexArray);
                    if (existingResult?.tbText == null)
                    {
                        TbText newTbText = new TbText
                        {
                            Text = finalResult,
                            Width = analyText.nWidth,
                            Height = analyText.nHeight,
                            HexStrValue = analyText.sHexArray,
                            Threshold = 0,
                            Searched = 1,
                            Reserved = ""
                        };
                        await PgService_TbText.InsertRowAsync(newTbText);
                    }
                }

                bmpFore.Dispose();
                sw.Stop();
                Debug.WriteLine($"[최종 결과] OfrStr_ComplexCharSetAsync: '{finalResult}' ({sw.ElapsedMilliseconds}ms)");
                return new StdResult_String(finalResult);
            }

            // 폴백: 전체 RightSliding (listStartEnd가 없는 경우)
            OfrCharFailDelegate failFunc = null;
            if (s_bDebugMode)
            {
                failFunc = async (Draw.Bitmap bmpSrc, Draw.Rectangle rcFail) =>
                {
                    string manualInput = await ShowImageToCharDialog(bmpFore, rcFail, "인식 실패 영역");
                    return manualInput;
                };
            }

            OfrResult_Recognition recognitionResult = await Ofr_CharSet_Core.RecognizeCharSetAsync_RightSliding(bmpFore, searchFunc, failFunc);

            bmpFore.Dispose();

            if (recognitionResult != null && !string.IsNullOrEmpty(recognitionResult.strResult))
            {
                string resultText = recognitionResult.strResult;

                // SourceBitmap 해제
                recognitionResult.SourceBitmap?.Dispose();

                // 완전 성공(☒ 없음) 시 캐시 + TbText 저장
                if (!resultText.Contains('☒'))
                {
                    SaveTextCache(analyText, resultText);

                    // DB에 이미 존재하는지 확인 (중복 키 예외 방지)
                    var existingResult = await PgService_TbText.SelectRowByBasicAsync(analyText.nWidth, analyText.nHeight, analyText.sHexArray);
                    if (existingResult?.tbText == null)
                    {
                        TbText newTbText = new TbText
                        {
                            Text = resultText,
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
                            Debug.WriteLine($"[OfrStr_ComplexCharSetAsync] TbText 저장 성공: {resultText}");
                        }
                    }
                }

                sw.Stop();
                Debug.WriteLine($"[RightSliding 성공] OfrStr_ComplexCharSetAsync: '{resultText}' ({sw.ElapsedMilliseconds}ms)");
                return new StdResult_String(resultText);
            }

            // ========================================
            // 5. 모든 단계 실패
            // ========================================
            return new StdResult_String(recognitionResult?.sErr ?? "RightSliding 실패", "OfrStr_ComplexCharSetAsync_05");
        }
        catch (Exception ex)
        {
            return new StdResult_String(StdUtil.GetExceptionMessage(ex), "OfrStr_ComplexCharSetAsync_999");
        }
    }

    // 페이지 비트맵 Rectangle 영역 crop + RGB 반전 후 한글 OFR 인식 (최대 문자 수 지정 가능)
    public static async Task<StdResult_String> OfrStr_ComplexCharSetAsync(
        Draw.Bitmap bmpPage, Draw.Rectangle rect, bool bInvertRgb, bool bTextSave, double dWeight, int maxCharCount = 0)
    {
        Draw.Bitmap bmpCell = null;

        try
        {
            // 1. bmpPage에서 rect 영역 crop
            bmpCell = OfrService.GetBitmapInBitmapFast(bmpPage, rect);
            if (bmpCell == null) return new StdResult_String("셀 crop 실패", "OfrStr_ComplexCharSetAsync_Rect_01");

            // 2. RGB 반전 필요하면
            if (bInvertRgb)
            {
                Draw.Bitmap bmpOriginal = bmpCell;
                bmpCell = OfrService.InvertBitmap(bmpOriginal);
                bmpOriginal.Dispose();
            }

            // 3. 기존 오버로드 호출 (maxCharCount 전달)
            var result = await OfrStr_ComplexCharSetAsync(bmpCell, bTextSave, dWeight, maxCharCount);

            // 4. maxCharCount > 0이면 공백/실패마커 제외하고 좌측부터 N개 문자만
            if (maxCharCount > 0 && !string.IsNullOrEmpty(result.strResult))
            {
                // 공백과 실패마커(☒) 제거 후 좌측 N개 문자만
                string cleaned = result.strResult.Replace(" ", "").Replace("☒", "");
                if (cleaned.Length >= maxCharCount)
                {
                    result.strResult = cleaned.Substring(0, maxCharCount);
                    result.sErr = null; // 성공으로 처리
                }
                else if (cleaned.Length > 0)
                {
                    result.strResult = cleaned;
                    result.sErr = null; // 부분 성공도 성공으로
                }
            }

            return result;
        }
        finally
        {
            bmpCell?.Dispose();
        }
    }

    // 컬럼 헤더 등 복합 문자열 인식 (Stage 1 매칭, Stage 2-4 통합 예정)
    //public static async Task<OfrResult_TbCharSetList> OfrStr_ComplexCharSetAsync(
    //    Draw.Bitmap bmpOrg, Draw.Rectangle rcSpare, bool bSaveToTbText = false, bool bEdit = true, bool bWrite = true, bool bMsgBox = true)
    //{
    //    if (bmpOrg == null)
    //        return new OfrResult_TbCharSetList(null, "bmpOrg이 null입니다", "OfrWork_Common/OfrStr_ComplexCharSetAsync_01");

    //    OfrResult_TbCharSetList result = new OfrResult_TbCharSetList(bmpOrg);

    //    // Stage 1: TbText 전체 매칭 시도
    //    // rcSpare 영역 추출
    //    Draw.Bitmap bmpSpare = OfrService.GetBitmapInBitmapFast(bmpOrg, rcSpare);
    //    if (bmpSpare == null)
    //    {
    //        result.sErr = "rcSpare 비트맵 추출 실패";
    //        result.sPos = "OfrWork_Common/OfrStr_ComplexCharSetAsync_02";
    //        return ErrMsgResult_TbCharSetList(result, bWrite, bMsgBox);
    //    }

    //    byte byteAvgBrightness = OfrService.GetAverageBrightness_FromColorBitmapFast(bmpSpare);

    //    Draw.Rectangle? rcForeground =
    //        OfrService.GetForeGroundDrawRectangle_FromColorBitmapFast(bmpSpare, byteAvgBrightness, 0);

    //    if (rcForeground == null || rcForeground.Value.Width < 1 || rcForeground.Value.Height < 1)
    //    {
    //        bmpSpare.Dispose();
    //        result.sErr = "rcForeground가 비어있습니다";
    //        result.sPos = "OfrWork_Common/OfrStr_ComplexCharSetAsync_03";
    //        return ErrMsgResult_TbCharSetList(result, bWrite, bMsgBox);
    //    }

    //    // 전경 영역에서 비트맵 분석
    //    Draw.Bitmap bmpForeground = OfrService.GetBitmapInBitmapFast(bmpSpare, rcForeground.Value);
    //    bmpSpare.Dispose();

    //    if (bmpForeground == null)
    //    {
    //        result.sErr = "전경 비트맵 추출 실패";
    //        result.sPos = "OfrWork_Common/OfrStr_ComplexCharSetAsync_04";
    //        return ErrMsgResult_TbCharSetList(result, bWrite, bMsgBox);
    //    }

    //    byte byteAvgBrightness2 = OfrService.GetAverageBrightness_FromColorBitmapFast(bmpForeground);
    //    OfrModel_BitmapAnalysis analyText = OfrService.GetBitmapAnalysisFast(bmpForeground, byteAvgBrightness2);

    //    if (analyText == null || analyText.trueRate == 0 || analyText.trueRate == 1)
    //    {
    //        bmpForeground.Dispose();
    //        result.sErr = $"trueRate가 {analyText?.trueRate}입니다 (0 또는 1은 유효하지 않음)";
    //        result.sPos = "OfrWork_Common/OfrStr_ComplexCharSetAsync_04";
    //        return ErrMsgResult_TbCharSetList(result, bWrite, bMsgBox);
    //    }

    //    // TbText에서 전체 문자열 검색
    //    PgResult_TbText findResult = await PgService_TbText.SelectRowByBasicAsync(
    //        analyText.nWidth, analyText.nHeight, analyText.sHexArray);

    //    if (findResult != null && findResult.tbText != null && findResult.tbText.Text != null)
    //    {
    //        // Stage 1 성공!
    //        bmpForeground.Dispose();
    //        result.strResult = findResult.tbText.Text;
    //        //Debug.WriteLine($"[OfrWork_Common] Stage 1 성공: {result.strResult}");
    //        return result;
    //    }

    //    // Stage 2-4: RightSliding 알고리즘으로 fallback
    //    //Debug.WriteLine($"[OfrWork_Common] Stage 1 실패. Stage 2-4 (RightSliding) 시작");

    //    // 1. 델리게이트 함수 생성 (TbChar 단일 문자 검색용 - TbChar rebuilding 중)
    //    OfrCharSearchDelegate searchFunc = async (int width, int height, string hexString) =>
    //    {
    //        PgResult_TbChar charResult = await PgService_TbChar.SelectRowByBasicAsync(width, height, hexString);

    //        if (charResult != null && charResult.tbChar != null &&
    //            !string.IsNullOrEmpty(charResult.tbChar.Character) &&
    //            charResult.tbChar.Character.Length == 1)
    //        {
    //            return new OfrCharSearchResult(charResult.tbChar.Character[0], width, height);
    //        }

    //        return new OfrCharSearchResult(); // Found=false
    //    };

    //    // 2. RecognizeCharSetAsync_RightSliding 호출
    //    OfrResult_Recognition recognitionResult =
    //        await Ofr_CharSet_Core.RecognizeCharSetAsync_RightSliding(bmpForeground, searchFunc);

    //    bmpForeground.Dispose(); // 사용 완료

    //    // 3. 결과를 OfrResult_TbCharSetList로 변환
    //    if (recognitionResult != null && !string.IsNullOrEmpty(recognitionResult.strResult))
    //    {
    //        result.strResult = recognitionResult.strResult;
    //        Debug.WriteLine($"[OfrWork_Common] Stage 2-4 성공: {result.strResult}");

    //        // 4. bSaveToTbText=true이고 완전 성공(☒ 없음) 시 TbText에 저장
    //        if (bSaveToTbText && !result.strResult.Contains('☒'))
    //        {
    //            TbText newTbText = new TbText
    //            {
    //                Text = result.strResult,
    //                Width = analyText.nWidth,
    //                Height = analyText.nHeight,
    //                HexStrValue = analyText.sHexArray,
    //                Threshold = 0,
    //                Searched = 1,
    //                Reserved = ""
    //            };

    //            StdResult_Long saveResult = await PgService_TbText.InsertRowAsync(newTbText);
    //            if (saveResult.lResult > 0)
    //            {
    //                Debug.WriteLine($"[OfrWork_Common] TbText 저장 성공: {result.strResult}");
    //            }
    //            else
    //            {
    //                Debug.WriteLine($"[OfrWork_Common] TbText 저장 실패: {saveResult.sErr}");
    //            }
    //        }

    //        return result;
    //    }

    //    // Stage 2-4 실패
    //    result.sErr = $"Stage 2-4 실패: {recognitionResult?.sErr}";
    //    result.sPos = "OfrWork_Common/OfrStr_ComplexCharSetAsync_05";
    //    //Debug.WriteLine($"[OfrWork_Common] Stage 2-4 실패: {result.sErr}");

    //    return ErrMsgResult_TbCharSetList(result, bWrite, bMsgBox);
    //}

    // 단음소(숫자/영문) 전용 OFR 핵심 함수 (전경/배경 방식, Stage 1-2 연동)
    //     private static async Task<OfrResult_TbCharSetList> OfrStr_SeqCharCore(
    //         Draw.Bitmap bmpOrg,
    //         Draw.Rectangle rcSpare,
    //         bool bUseTbTextStage1,
    //         bool bEdit = true,
    //         bool bWrite = true,
    //         bool bMsgBox = true)
    //     {
    //         if (bmpOrg == null)
    //             return new OfrResult_TbCharSetList(null, "bmpOrg이 null입니다", "OfrWork_Common/OfrStr_SeqCharCore_01");

    //         OfrResult_TbCharSetList result = new OfrResult_TbCharSetList(bmpOrg);

    //         // Stage 1: TbText 전체 매칭 (bUseTbTextStage1=true일 때만)
    //         if (bUseTbTextStage1)
    //         {
    //             Draw.Bitmap bmpSpareForStage1 = OfrService.GetBitmapInBitmapFast(bmpOrg, rcSpare);
    //             if (bmpSpareForStage1 != null)
    //             {
    //                 byte byteAvgBrightness1 = OfrService.GetAverageBrightness_FromColorBitmapFast(bmpSpareForStage1);
    //                 Draw.Rectangle? rcForeground1 =
    //                     OfrService.GetForeGroundDrawRectangle_FromColorBitmapFast(bmpSpareForStage1, byteAvgBrightness1, 0);

    //                 if (rcForeground1 != null && rcForeground1.Value.Width > 0 && rcForeground1.Value.Height > 0)
    //                 {
    //                     Draw.Bitmap bmpForeground1 = OfrService.GetBitmapInBitmapFast(bmpSpareForStage1, rcForeground1.Value);
    //                     if (bmpForeground1 != null)
    //                     {
    //                         byte byteAvgBrightness2 = OfrService.GetAverageBrightness_FromColorBitmapFast(bmpForeground1);
    //                         OfrModel_BitmapAnalysis analyText = OfrService.GetBitmapAnalysisFast(bmpForeground1, byteAvgBrightness2);
    //                         bmpForeground1.Dispose();

    //                         if (analyText != null && analyText.trueRate > 0 && analyText.trueRate < 1)
    //                         {
    //                             // TbText에서 검색
    //                             PgResult_TbText findResult = await PgService_TbText.SelectRowByBasicAsync(
    //                                 analyText.nWidth, analyText.nHeight, analyText.sHexArray);

    //                             if (findResult != null && findResult.tbText != null && findResult.tbText.Text != null)
    //                             {
    //                                 // Stage 1 성공!
    //                                 bmpSpareForStage1.Dispose();
    //                                 result.strResult = findResult.tbText.Text;
    //                                 //Debug.WriteLine($"[OfrStr_SeqCharCore] Stage 1 성공: {result.strResult}");
    //                                 return result;
    //                             }
    //                         }
    //                     }
    //                 }
    //                 bmpSpareForStage1.Dispose();
    //             }

    //             //Debug.WriteLine($"[OfrStr_SeqCharCore] Stage 1 실패, Stage 2 시작");
    //         }

    //         // Stage 2: 전경/배경 방식으로 문자 영역 추출 + TbChar 검색
    //         // Step 1: rcSpare 영역 추출 및 평균 밝기 계산
    //         Draw.Bitmap bmpSpare = OfrService.GetBitmapInBitmapFast(bmpOrg, rcSpare);
    //         if (bmpSpare == null)
    //         {
    //             result.sErr = "rcSpare 비트맵 추출 실패";
    //             result.sPos = "OfrWork_Common/OfrStr_SeqCharCore_02";
    //             return ErrMsgResult_TbCharSetList(result, bWrite, bMsgBox);
    //         }

    //         byte byteAvgBrightness = OfrService.GetAverageBrightness_FromColorBitmapFast(bmpSpare);
    //         Debug.WriteLine($"[OfrStr_SeqCharCore] Step 1: byteAvgBrightness={byteAvgBrightness}");

    //         // Step 2: 전경 영역 추출
    //         Draw.Rectangle? rcForeground =
    //             OfrService.GetForeGroundDrawRectangle_FromColorBitmapFast(bmpSpare, byteAvgBrightness, 0);

    //         if (rcForeground == null || rcForeground.Value.Width < 1 || rcForeground.Value.Height < 1)
    //         {
    //             bmpSpare.Dispose();
    //             result.sErr = "rcForeground가 비어있습니다";
    //             result.sPos = "OfrWork_Common/OfrStr_SeqCharCore_03";
    //             return ErrMsgResult_TbCharSetList(result, bWrite, bMsgBox);
    //         }

    //         Debug.WriteLine($"[OfrStr_SeqCharCore] Step 2: rcForeground={rcForeground.Value}");

    //         // Step 3: 문자 영역 분리 (GetStartEndList)
    //         List<OfrModel_StartEnd> listStartEnd =
    //             OfrService.GetStartEndList_FromColorBitmap(bmpSpare, byteAvgBrightness, rcForeground.Value);

    //         if (listStartEnd == null || listStartEnd.Count == 0)
    //         {
    //             bmpSpare.Dispose();
    //             result.sErr = "문자 영역 분리 실패 (listStartEnd가 비어있음)";
    //             result.sPos = "OfrWork_Common/OfrStr_SeqCharCore_04";
    //             return ErrMsgResult_TbCharSetList(result, bWrite, bMsgBox);
    //         }

    //         Debug.WriteLine($"[OfrStr_SeqCharCore] Step 3: 문자 영역 수={listStartEnd.Count}");

    //         // Step 4-5: 각 문자 영역마다 TbChar에서 검색
    //         string sResult = "";
    //         const int nMaxCountFind = 10; // Searched 카운트 업데이트 임계값

    //         for (int i = 0; i < listStartEnd.Count; i++)
    //         {
    //             // 문자 Rectangle 구하기
    //             StdConst_IndexRect rcIndex =
    //                 OfrService.GetIndexRect_FromColorBitmapByIndex(
    //                     bmpSpare, byteAvgBrightness, rcForeground.Value, listStartEnd, i, i);

    //             if (rcIndex == null)
    //             {
    //                 Debug.WriteLine($"[OfrStr_SeqCharCore] Warning: rcIndex[{i}] is null, skipping");
    //                 sResult += '☒'; // 실패 문자 표시
    //                 continue;
    //             }

    //             // Rectangle에서 비트맵 추출 및 분석
    //             Draw.Rectangle rcChar = rcIndex.GetDrawRectangle();
    //             Draw.Bitmap bmpChar = OfrService.GetBitmapInBitmapFast(bmpSpare, rcChar);

    //             if (bmpChar == null)
    //             {
    //                 Debug.WriteLine($"[OfrStr_SeqCharCore] Warning: bmpChar[{i}] is null, skipping");
    //                 sResult += '☒';
    //                 continue;
    //             }

    //             // 전체 텍스트의 평균 밝기(byteAvgBrightness) 재사용 (같은 텍스트 안에서는 threshold 동일)
    //             OfrModel_BitmapAnalysis analyChar = OfrService.GetBitmapAnalysisFast(bmpChar, byteAvgBrightness);
    //             bmpChar.Dispose(); // 즉시 리소스 정리

    //             if (analyChar == null || analyChar.trueRate == 0 || analyChar.trueRate == 1)
    //             {
    //                 Debug.WriteLine($"[OfrStr_SeqCharCore] Warning: analyChar[{i}] invalid trueRate={analyChar?.trueRate}");
    //                 sResult += '☒';
    //                 continue;
    //             }

    //             // TbChar에서 검색
    //             PgResult_TbChar resultBackup =
    //                 await PgService_TbChar.SelectRowByBasicAsync(analyChar.nWidth, analyChar.nHeight, analyChar.sHexArray);

    //             if (resultBackup != null && resultBackup.tbChar != null &&
    //                 !string.IsNullOrEmpty(resultBackup.tbChar.Character))
    //             {
    //                 // 찾음!
    //                 sResult += resultBackup.tbChar.Character;
    //                 Debug.WriteLine($"[OfrStr_SeqCharCore] 문자[{i}] 인식 성공: {resultBackup.tbChar.Character}");

    //                 // Searched 카운트 업데이트
    //                 if (nMaxCountFind < resultBackup.tbChar.Searched)
    //                 {
    //                     resultBackup.tbChar.Searched += 1;
    //                     await PgService_TbChar.UpdateRowAsync(resultBackup.tbChar);
    //                 }
    //             }
    //             else
    //             {
    //                 // Step 6: TbChar에서 못 찾으면 - Debug 모드에서 다이얼로그 표시
    //                 if (s_bDebugMode && bEdit)
    //                 {
    //                     // TODO: ImageToCharWnd 다이얼로그 통합 (필요 시)
    //                     Debug.WriteLine($"[OfrStr_SeqCharCore] 문자[{i}] 인식 실패: Width={analyChar.nWidth}, Height={analyChar.nHeight}");
    //                     sResult += '☒';
    //                 }
    //                 else
    //                 {
    //                     sResult += '☒';
    //                 }
    //             }
    //         }

    //         bmpSpare.Dispose();
    //         result.strResult = sResult;

    //         Debug.WriteLine($"[OfrStr_SeqCharCore] 최종 결과: {sResult}");

    //         // 성공 시 (☒ 없으면) TbText에 저장
    //         if (!string.IsNullOrEmpty(sResult) && !sResult.Contains('☒'))
    //         {
    //             // 전체 문자열을 다시 분석하여 TbText에 저장 (threshold 재사용)
    //             OfrModel_BitmapAnalysis analyText = OfrService.GetBitmapAnalysisFast(bmpOrg, byteAvgBrightness);

    //             if (analyText != null && analyText.trueRate > 0 && analyText.trueRate < 1)
    //             {
    //                 TbText newTbText = new TbText
    //                 {
    //                     Text = sResult,
    //                     Width = analyText.nWidth,
    //                     Height = analyText.nHeight,
    //                     HexStrValue = analyText.sHexArray,
    //                     Threshold = 0,
    //                     Searched = 1,
    //                     Reserved = ""
    //                 };

    //                 StdResult_Long saveResult = await PgService_TbText.InsertRowAsync(newTbText);
    //                 if (saveResult.lResult > 0)
    //                 {
    //                     Debug.WriteLine($"[OfrStr_SeqCharCore] TbText 저장 성공: {sResult}");
    //                 }
    //             }
    //         }

    //         return result;
    //     }

    // 반복되는 단음소 단어 인식 (Stage 1 캐싱 검색 -> Stage 2 분리 검색)
    //public static async Task<OfrResult_TbCharSetList> OfrStr_SeqWordAsync(
    //    Draw.Bitmap bmpOrg,
    //    Draw.Rectangle rcSpare,
    //    bool bEdit = true,
    //    bool bWrite = true,
    //    bool bMsgBox = true)
    //{
    //    return await OfrStr_SeqCharCore(bmpOrg, rcSpare, bUseTbTextStage1: true, bEdit, bWrite, bMsgBox);
    //}

    // 가변 단음소 문자열 인식 함수 (예: 전화번호, 주문번호 등) - Stage 1 스킵
    //public static async Task<OfrResult_TbCharSetList> OfrStr_SeqCharAsync(
    //    Draw.Bitmap bmpOrg,
    //    Draw.Rectangle rcSpare,
    //    bool bEdit = true,
    //    bool bWrite = true,
    //    bool bMsgBox = true)
    //{
    //    return await OfrStr_SeqCharCore(bmpOrg, rcSpare, bUseTbTextStage1: false, bEdit, bWrite, bMsgBox);
    //}

    //public static async Task<OfrResult_TbText> OfrImage_ExactDrawRelRectAsync(Draw.Bitmap bmpExact)
    //{
    //    byte byteAvgBrightness = 0;
    //    OfrModel_BmpTextAnalysis modelText = null;
    //    PgResult_TbText resultPg = null;

    //    for (int i = 0; i < c_nRepeatVeryShort; i++)
    //    {
    //        // Get Basic Hex Info
    //        byteAvgBrightness = OfrService.GetAverageBrightness_FromColorBitmapFast(bmpExact);
    //        modelText = OfrService.GetOfrModel_TextAnalysis_InExactBitmapFast(bmpExact, byteAvgBrightness);

    //        if (modelText.trueRate == 0 || modelText.trueRate == 1) goto REPEAT;

    //        resultPg = await PgService_TbText.SelectRowByBasicAsync(modelText.nWidth, modelText.nHeight, modelText.sHexArray);
    //        if (resultPg.tbText != null) return new OfrResult_TbText(resultPg.tbText, modelText);
    //        //MsgBox($"{modelText.nWidth}, {modelText.nHeight}, {modelText.sHexArray} -> {resultPg.tbText.Text}"); // Test

    //        REPEAT: await Task.Delay(c_nWaitNormal);
    //    }

    //    return new OfrResult_TbText(null, modelText);
    //}

    //public static async Task<OfrResult_TbText> OfrImage_DrawRelSpareRect_ByMaxBrightnessAsync(Bitmap bmpOrg, Draw.Rectangle rcRelSpare)
    //{
    //    //Get Basic Hex Info
    //    Draw.Rectangle rcForeground = StdUtil.s_rcDrawEmpty;
    //    byte byteMaxBrightness = 0;
    //    for (int i = 0; i < c_nRepeatShort; i++)
    //    {
    //        byteMaxBrightness = OfrService.GetMaxBrightness_FromColorBitmapRectFast(bmpOrg, rcRelSpare);
    //        byteMaxBrightness -= 1; // 밝기 조정(약간 어둡게) += 1; // 밝기 조정(약간 밝게)

    //        rcForeground = // Foreground 영역을 MaxBrightness로 구한다
    //            OfrService.GetForeGroundDrawRectangle_FromColorBitmapRectFast(bmpOrg, rcRelSpare, byteMaxBrightness, 0);
    //        if (rcForeground != StdUtil.s_rcDrawEmpty) break;
    //    }
    //    if (rcForeground == StdUtil.s_rcDrawEmpty)
    //        return new OfrResult_TbText(
    //            "rcForeground이 비어있습니다", "OfrWork_Common/OfrImage_DrawRelSpareRect_ByDualBrightnessAsync_01", bmpOrg, s_sLogDir, false);

    //    OfrModel_BitmapAnalysis info = OfrService.GetOfrModelAnalysis_InBitmapFast(bmpOrg, rcForeground, byteMaxBrightness);

    //    if (info.trueRate != 0 && info.trueRate != 1) // 정보가 있을것 같으면
    //    {
    //        PgResult_TbText resultTb = await PgService_TbText.SelectRowByBasicAsync(info.nWidth, info.nHeight, info.sHexArray);

    //        if (resultTb.tbText != null)
    //        {
    //            return new OfrResult_TbText
    //            {
    //                tbText = resultTb!.tbText!,
    //                strResult = resultTb!.tbText!.Text!
    //            };
    //        }
    //        else
    //        {
    //            return new OfrResult_TbText(
    //                resultTb.sErr, "OfrWork_Common/OfrImage_DrawRelSpareRect_ByMaxBrightnessAsync_02", bmpOrg, s_sLogDir);
    //        }
    //    }

    //    return new OfrResult_TbText(
    //        "tb 널 입니다", "OfrWork_Common/OfrImage_DrawRelSpareRect_ByMaxBrightnessAsync_03", bmpOrg, s_sLogDir);
    //}
    //public static async Task<OfrResult_TbText> OfrImage_DrawRelSpareRect_ByMinBrightnessAsync(Bitmap bmpOrg, Draw.Rectangle rcRelSpare)
    //{
    //    //Get Basic Hex Info
    //    Draw.Rectangle rcForeground = StdUtil.s_rcDrawEmpty;
    //    byte byteMinBrightness = 0;
    //    for (int i = 0; i < c_nRepeatShort; i++)
    //    {
    //        byteMinBrightness = OfrService.GetMaxBrightness_FromColorBitmapRectFast(bmpOrg, rcRelSpare);
    //        byteMinBrightness += 1; // 밝기 조정(약간 밝게)

    //        rcForeground = // Foreground 영역을 MaxBrightness로 구한다
    //            OfrService.GetForeGroundDrawRectangle_FromColorBitmapRectFast(bmpOrg, rcRelSpare, byteMinBrightness, 0);
    //        if (rcForeground != StdUtil.s_rcDrawEmpty) break;
    //    }
    //    if (rcForeground == StdUtil.s_rcDrawEmpty)
    //        return new OfrResult_TbText(
    //            "rcForeground이 비어있습니다", "OfrWork_Common/OfrImage_DrawRelSpareRect_ByDualBrightnessAsync_01", bmpOrg, s_sLogDir, false);

    //    //byte byteAvgBrightness = OfrService.GetAverageBrightness_FromColorBitmapRectFast(bmpOrg, rcForeground);
    //    OfrModel_BitmapAnalysis info = OfrService.GetOfrModelAnalysis_InBitmapFast(bmpOrg, rcForeground, byteMinBrightness); // ~ 밝기로 구한다.

    //    if (info.trueRate != 0 && info.trueRate != 1) // 정보가 있을것 같으면
    //    {
    //        PgResult_TbText resultTb = await PgService_TbText.SelectRowByBasicAsync(info.nWidth, info.nHeight, info.sHexArray);

    //        if (resultTb.tbText != null)
    //            return new OfrResult_TbText
    //            {
    //                tbText = resultTb!.tbText!,
    //                strResult = resultTb!.tbText!.Text!
    //            };
    //        else
    //        {
    //            return new OfrResult_TbText(
    //                resultTb.sErr, "OfrWork_Common/OfrImage_DrawRelSpareRect_ByDualBrightnessAsync_02", bmpOrg, s_sLogDir, false);
    //        }
    //    }

    //    return new OfrResult_TbText(
    //        "tb 널 입니다", "OfrWork_Common/OfrImage_DrawRelSpareRect_ByDualBrightnessAsync_03", bmpOrg, s_sLogDir, false);
    //}

    //public static async Task<OfrResult_TbText> OfrImage_DrawRelSpareRect_ByAvgBrightnessAsync(Bitmap bmpCapture, Draw.Rectangle rcRelSpare)
    //{
    //    // Get Basic Hex Info
    //    Draw.Rectangle rcForeground = StdUtil.s_rcDrawEmpty;
    //    byte byteAvgBrightness = 0;
    //    for (int i = 0; i < c_nRepeatShort; i++)
    //    {
    //        byteAvgBrightness = OfrService.GetAverageBrightness_FromColorBitmapRectFast(bmpOrg, rcRelSpare);
    //        rcForeground = OfrService.GetForeGroundDrawRectangle_FromColorBitmapRectFast(bmpOrg, rcRelSpare, byteAvgBrightness, 0);
    //        if (rcForeground != StdUtil.s_rcDrawEmpty) break;
    //    }

    //    if (rcForeground == StdUtil.s_rcDrawEmpty)
    //        return new OfrResult_TbText(
    //            "rcForeground이 비어있습니다", "OfrWork_Common/OfrImage_DrawRelSpareRect_ByAvgBrightnessAsync_01", bmpOrg, s_sLogDir, false);

    //    OfrModel_BitmapAnalysis info = OfrService.GetOfrModelAnalysis_InBitmapFast(bmpOrg, rcForeground, byteAvgBrightness);

    //    if (info.trueRate != 0 && info.trueRate != 1) // 정보가 있을것 같으면
    //    {
    //        PgResult_TbText resultTb = await PgService_TbText.SelectRowByBasicAsync(info.nWidth, info.nHeight, info.sHexArray);

    //        if (resultTb.tbText != null)
    //        {
    //            return new OfrResult_TbText
    //            {
    //                tbText = resultTb!.tbText!,
    //                strResult = resultTb!.tbText!.Text!
    //            };
    //        }
    //        else
    //        {
    //            return new OfrResult_TbText(
    //                resultTb.sErr, "OfrWork_Common/OfrImage_DrawRelSpareRect_ByAvgBrightnessAsync_02", bmpOrg, s_sLogDir);
    //        }
    //    }

    //    return new OfrResult_TbText(
    //        "tb 널 입니다", "OfrWork_Common/OfrImage_DrawRelSpareRect_ByAvgBrightnessAsync_03", bmpOrg, s_sLogDir, false);
    //}

    // Bitmap의 특정 영역에서 Dual Brightness로 이미지(CheckBox 등) 인식
    public static async Task<OfrResult_TbText> OfrImage_DrawRelSpareRect_ByDualBrightnessAsync(Draw.Bitmap bmpOrg, Draw.Rectangle rcRelSpare)
    {
        // 1. 지정 영역에서 전경 영역 찾기 (최대 밝기 기반)
        Draw.Rectangle rcForeground = StdUtil.s_rcDrawEmpty;
        byte byteMaxBrightness = 0;

        for (int i = 0; i < c_nRepeatShort; i++)
        {
            byteMaxBrightness = OfrService.GetMaxBrightness_FromColorBitmapRectFast(bmpOrg, rcRelSpare);
            //Debug.WriteLine($"[OfrWork_Common] Dual Brightness Step1: MaxBrightness={byteMaxBrightness}, rcRelSpare={rcRelSpare}");
            byteMaxBrightness -= 1; // 밝기 조정(약간 어둡게)
            rcForeground = OfrService.GetForeGroundDrawRectangle_FromColorBitmapRectFast(bmpOrg, rcRelSpare, byteMaxBrightness, 0);
            //Debug.WriteLine($"[OfrWork_Common] Dual Brightness Step1: rcForeground={rcForeground}");
            if (rcForeground != StdUtil.s_rcDrawEmpty) break;

            await Task.Delay(c_nWaitNormal);
        }

        if (rcForeground == StdUtil.s_rcDrawEmpty)
            return ErrMsgResult_TbText(
                null, null, "전경 영역을 찾을 수 없습니다", "OfrWork_Common/OfrImage_DrawRelSpareRect_ByDualBrightnessAsync_01");

        // 2. 정확한 영역 추출
        Draw.Bitmap bmpExact = OfrService.GetBitmapInBitmapFast(bmpOrg, rcForeground);

        // 3. 평균 밝기 기반으로 이미지 분석
        byte byteAvgBrightness = OfrService.GetAverageBrightness_FromColorBitmapFast(bmpExact);
        OfrModel_BitmapAnalysis info = OfrService.GetBitmapAnalysisFast(bmpExact, byteAvgBrightness);
        //Debug.WriteLine($"[OfrWork_Common] Dual Brightness Step2: AvgBrightness={byteAvgBrightness}, Size={info.nWidth}x{info.nHeight}, trueRate={info.trueRate}");

        // 4. DB 검색 (정보가 있을 것 같으면)
        if (info.trueRate != 0 && info.trueRate != 1)
        {
            PgResult_TbText resultTb = await PgService_TbText.SelectRowByBasicAsync(info.nWidth, info.nHeight, info.sHexArray);
            //Debug.WriteLine($"[OfrWork_Common] Dual Brightness Step3: DB 검색 결과={resultTb.tbText?.Text ?? "null"}");
            if (resultTb.tbText != null) return new OfrResult_TbText(resultTb.tbText, info);
        }

        // 5. DB에 없음 - 분석 정보는 반환
        //Debug.WriteLine($"[OfrWork_Common] Dual Brightness Step4: DB에 없음 (trueRate={info.trueRate})");
        return new OfrResult_TbText(info, null, "OfrWork_Common/OfrImage_DrawRelSpareRect_ByDualBrightnessAsync_02");
    }

    ////public static async Task<OfrResult_TbText> Tmp_OfrImage_DrawRelSpareRect_ByDualBrightnessAsync(Bitmap bmpOrg, Draw.Rectangle rcRelSpare)
    ////{
    ////    //Get Basic Hex Info
    ////    Draw.Rectangle rcForeground = StdUtil.s_rcDrawEmpty;
    ////    byte byteMaxBrightness = 0;
    ////    for (int i = 0; i < c_nRepeatShort; i++)
    ////    {
    ////        byteMaxBrightness = OfrService.GetMaxBrightness_FromColorBitmapRectFast(bmpOrg, rcRelSpare);
    ////        byteMaxBrightness -= 1; // 밝기 조정(약간 어둡게)
    ////        rcForeground = OfrService.GetForeGroundDrawRectangle_FromColorBitmapRectFast(bmpOrg, rcRelSpare, byteMaxBrightness, 0);
    ////        if (rcForeground != StdUtil.s_rcDrawEmpty) break;
    ////    }
    ////    if (rcForeground == StdUtil.s_rcDrawEmpty)
    ////    {
    ////        return new OfrResult_TbText(
    ////            "rcForeground이 비어있습니다", "OfrWork_Common/OfrImage_DrawRelSpareRect_ByDualBrightnessAsync_01", bmpOrg, s_sLogDir, false);
    ////    }

    ////    byte byteAvgBrightness = OfrService.GetAverageBrightness_FromColorBitmapRectFast(bmpOrg, rcForeground);
    ////    OfrModel_BitmapAnalysis info = OfrService.GetOfrModelAnalysis_InBitmapFast(bmpOrg, rcForeground, byteAvgBrightness); // 평균 밝기로 구한다.
    ////    //FormFuncs.MsgBox($"info={info.trueRate}"); // Test

    ////    if (info.trueRate != 0 && info.trueRate != 1) // 정보가 있을것 같으면
    ////    {
    ////        PgResult_TbText resultTb = await PgService_TbText.SelectRowByBasicAsync(info.nWidth, info.nHeight, info.sHexArray);
    ////        FormFuncs.MsgBox(resultTb.ToString()); // Test

    ////        if (resultTb.tbText != null)
    ////            return new OfrResult_TbText
    ////            {
    ////                tbText = resultTb!.tbText!,
    ////                strResult = resultTb!.tbText!.Text!
    ////            };
    ////        else
    ////        {
    ////            return new OfrResult_TbText(
    ////                resultTb.sErr, "OfrWork_Common/OfrImage_DrawRelSpareRect_ByDualBrightnessAsync_02", bmpOrg, s_sLogDir, false);
    ////        }
    ////    }

    ////    return new OfrResult_TbText(
    ////        "tb 널 입니다", "OfrWork_Common/OfrImage_DrawRelSpareRect_ByDualBrightnessAsync_03", bmpOrg, s_sLogDir, false);
    ////}

    // Bitmap 전체에서 Dual Brightness로 이미지(CheckBox 등) 인식
    public static async Task<OfrResult_TbText> OfrImage_InSparedBitmapt_ByDualBrightnessAsync(Draw.Bitmap bmpOrg)
    {
        // 1. 전경 영역 찾기 (최대 밝기 기반)
        Draw.Rectangle rcForeground = StdUtil.s_rcDrawEmpty;
        byte byteMaxBrightness = 0;

        for (int i = 0; i < c_nRepeatShort; i++)
        {
            byteMaxBrightness = OfrService.GetMaxBrightness_FromColorBitmapFast(bmpOrg);
            byteMaxBrightness -= 1; // 밝기 조정(약간 어둡게)
            rcForeground = OfrService.GetForeGroundDrawRectangle_FromColorBitmapFast(bmpOrg, byteMaxBrightness, 0);
            if (rcForeground != StdUtil.s_rcDrawEmpty) break;
        }

        if (rcForeground == StdUtil.s_rcDrawEmpty)
            return ErrMsgResult_TbText(
                null, null, "전경 영역을 찾을 수 없습니다", "OfrWork_Common/OfrImage_InSparedBitmapt_ByDualBrightnessAsync_01");

        // 2. 평균 밝기 기반으로 이미지 분석
        byte byteAvgBrightness = OfrService.GetAverageBrightness_FromColorBitmapRectFast(bmpOrg, rcForeground);
        OfrModel_BitmapAnalysis modelText = OfrService.GetBitmapAnalysisFast(bmpOrg, rcForeground, byteAvgBrightness);

        // 3. DB 검색 (정보가 있을 것 같으면)
        if (modelText.trueRate != 0 && modelText.trueRate != 1)
        {
            PgResult_TbText resultTb = await PgService_TbText.SelectRowByBasicAsync(modelText.nWidth, modelText.nHeight, modelText.sHexArray);
            if (resultTb.tbText != null) return new OfrResult_TbText(resultTb.tbText, modelText);
        }

        // 4. DB에 없음 - 분석 정보는 반환
        return new OfrResult_TbText(modelText, null, "OfrWork_Common/OfrImage_InSparedBitmapt_ByDualBrightnessAsync_02");
    }

    //// TbCharSet
    ///// <summary>
    ///// 
    ///// </summary>
    ///// <param name="bmpOrg" 원래 비트맵 (웬만하면 복사하지 말고 영역으로 처리하자) ></param>
    ///// <param name="rcSpare" 목표보다 약간 크게 잡는다></param>
    ///// <param name="bMsgBox 실패시 MsgBox 여부></param>
    ///// <param name="nCountFind" 이숫자보다 적으면 카운트를 늘려서 업데이트한다></param>
    ///// <returns> TbText에 없으면 </returns>
    ////public static async Task<OfrResult_TbCharSetList> OfrStrResultFrom_ComplexMultiCharBitmapAsync(Bitmap bmpOrg, Draw.Rectangle rcSpare, bool bEdit = true, bool bWrite = true, bool bMsgBox = true)
    ////{
    ////    const int nCountFind = 10;

    ////    if (bmpOrg == null || Application.Current == null) return LocalCommon_OfrResult.ErrMsgResult_TbCharSetList(null, 
    ////        "bmpOrg 널 이거나, Application.Current이 널 입니다.", "OfrwORK_Common/OfrStrResultFrom_ComplexMultiCharBitmapAsync_01", bWrite, bMsgBox);

    ////    OfrResult_TbCharSetList result = new OfrResult_TbCharSetList(bmpOrg); // 리스트 초기화 했음.

    ////    #region Text로 찾기
    ////    byte byteAvgBrightness = OfrService.GetAverageBrightness_FromColorBitmapRectFast(bmpOrg, rcSpare);
    ////    //MsgBox($"byteAvgBrightness={byteAvgBrightness}"); // Test
    ////    Draw.Rectangle rcForeground =
    ////        OfrService.GetForeGroundDrawRectangle_FromColorBitmapRectFast(bmpOrg, rcSpare, byteAvgBrightness, 0);

    ////    if (rcForeground == StdUtil.s_rcDrawEmpty)
    ////    {
    ////        result.sErr = "rcForeground가 비었읍니다.";
    ////        result.sPos = "OfrwORK_Common/OfrStrResultFrom_ComplexMultiCharBitmapAsync_02";

    ////        return LocalCommon_OfrResult.ErrMsgResult_TbCharSetList(result, bWrite, bMsgBox);
    ////    }

    ////    //FormFuncs.MsgBox($"rcSpare={rcSpare}, rcForeground={rcForeground}"); // Test
    ////    OfrModel_BmpTextAnalysis analyText = OfrService.GetOfrModel_TextAnalysis_RectInBitmapFast(bmpOrg, rcForeground, byteAvgBrightness);

    ////    // TbText 에서 찾는다
    ////    PgResult_TbText findResult = PgService_TbText.SelectRowByBasic(analyText.nWidth, analyText.nHeight, analyText.sHexArray);

    ////    if (findResult.tbText != null && findResult.tbText.Text != null)
    ////    {
    ////        result.strResult = findResult.tbText.Text;
    ////        //Debug.WriteLine(result.strResult); // Test

    ////        goto EXIT_SUCCESS; // 찾으면 탈출
    ////    }
    ////    #endregion

    ////    // 못찾으면 TbChar에서 찾는다
    ////    List<OfrModel_StartEnd> listStartEnd =
    ////        OfrService.GetStartEndList_FromColorBitmap(bmpOrg, byteAvgBrightness, rcForeground);

    ////    string sResult = "";
    ////    int nLastIndex = listStartEnd.Count - 1;
    ////    int nBefLastIndex = nLastIndex - 1;
    ////    //MsgBox("코딩해야 합니다", "OfrWork_Common/OfrStrResultFrom_ComplexMultiCharBitmapAsync_01");

    ////    for (int x = 0; x < listStartEnd.Count; x++)
    ////    {
    ////        // 첫번째 음소구하기
    ////        StdConst_IndexRect rcIndex1 =
    ////            OfrService.GetIndexRect_FromColorBitmapByIndex(bmpOrg, byteAvgBrightness, rcForeground, listStartEnd, x, x);

    ////        // 에러만 추가한다.
    ////        if (rcIndex1 == null)
    ////        {
    ////            result.sErr = "rcIndex1이 널 입니다";
    ////            result.sPos = "OfrwORK_Common/OfrStrResultFrom_ComplexMultiCharBitmapAsync_03";

    ////            return LocalCommon_OfrResult.ErrMsgResult_TbCharSetList(result, bWrite, bMsgBox);
    ////        }

    ////        // Get BitmapAnalysis
    ////        Draw.Rectangle rcTmp1 = rcIndex1.GetDrawRectangle();
    ////        OfrModel_BmpCharAnalysis model1 = OfrService.GetOfrModel_CharAnalysis_RectInBitmapFast(bmpOrg, rcTmp1, byteAvgBrightness);

    ////        // DB에서 찾는다
    ////        PgResult_TbChar resultTb1 = await PgService_TbChar.SelectRowByBasicAsync(model1.nWidth, model1.nHeight, model1.sHexArray);

    ////        // 있건, 없건 
    ////        result.listCharResult.Add(new OfrResult_TbCharInSet(model1, resultTb1.tbChar));

    ////        if (resultTb1.tbChar != null) // 찾으면
    ////        {
    ////            if (x < nLastIndex) // 인덱스 여유가 있으면
    ////            {
    ////                // 두번째 음소구하기
    ////                StdConst_IndexRect rcIndex2 =
    ////                    OfrService.GetIndexRect_FromColorBitmapByIndex(bmpOrg, byteAvgBrightness, rcForeground, listStartEnd, x, x + 1);

    ////                // 에러만 추가한다.
    ////                if (rcIndex2 == null)
    ////                {
    ////                    result.sErr = "rcIndex2가 널 입니다";
    ////                    result.sPos = "OfrwORK_Common/OfrStrResultFrom_ComplexMultiCharBitmapAsync_04";

    ////                    return LocalCommon_OfrResult.ErrMsgResult_TbCharSetList(result, bWrite, bMsgBox);
    ////                }

    ////                // Get BitmapAnalysis
    ////                Draw.Rectangle rcTmp2 = rcIndex2.GetDrawRectangle();
    ////                OfrModel_BmpCharAnalysis model2 =
    ////                    OfrService.GetOfrModel_CharAnalysis_RectInBitmapFast(bmpOrg, rcTmp2, byteAvgBrightness);

    ////                // DB에서 찾는다
    ////                PgResult_TbChar resultTb2 =
    ////                    await PgService_TbChar.SelectRowByBasicAsync(model2.nWidth, model2.nHeight, model2.sHexArray);

    ////                // 있건, 없건 
    ////                result.listCharResult.Add(new OfrResult_TbCharInSet(model2, resultTb2.tbChar));

    ////                // 있으면
    ////                if (resultTb2.tbChar != null) // 찾으면 - 확장건를 체택하고 인덱스 증가
    ////                {
    ////                    sResult += resultTb2.tbChar.Character;
    ////                    x += 1;
    ////                    if (nCountFind < resultTb2.tbChar.Searched)
    ////                    {
    ////                        resultTb2.tbChar.Searched += 1;
    ////                        await PgService_TbChar.UpdateRowAsync(resultTb2.tbChar);
    ////                    }
    ////                }
    ////                else // 못찾으면 - 전건을 체택
    ////                {
    ////                    sResult += resultTb1.tbChar.Character;
    ////                    if (nCountFind < resultTb1.tbChar.Searched)
    ////                    {
    ////                        resultTb1.tbChar.Searched += 1;
    ////                        await PgService_TbChar.UpdateRowAsync(resultTb1.tbChar);
    ////                    }
    ////                }
    ////            }
    ////            else // 인덱스 여유가 없으면 인정
    ////            {
    ////                sResult += resultTb1.tbChar.Character;
    ////            }
    ////        }
    ////        else // 디비에서 못찾았으면 - MAX 2건더 체크
    ////        {
    ////            if (x < nLastIndex) // 한번더할 인덱스면
    ////            {
    ////                // 두번째 음소구하기
    ////                StdConst_IndexRect rcIndex2 =
    ////                    OfrService.GetIndexRect_FromColorBitmapByIndex(bmpOrg, byteAvgBrightness, rcForeground, listStartEnd, x, x + 1);

    ////                // 에러만 추가한다.
    ////                if (rcIndex2 == null)
    ////                {
    ////                    result.sErr = "rcIndex2가 널 입니다";
    ////                    result.sPos = "OfrwORK_Common/OfrStrResultFrom_ComplexMultiCharBitmapAsync_05";

    ////                    return LocalCommon_OfrResult.ErrMsgResult_TbCharSetList(result, bWrite, bMsgBox);
    ////                }

    ////                // Get BitmapAnalysis
    ////                Draw.Rectangle rcTmp2 = rcIndex2.GetDrawRectangle();
    ////                OfrModel_BmpCharAnalysis model2 =
    ////                    OfrService.GetOfrModel_CharAnalysis_RectInBitmapFast(bmpOrg, rcTmp2, byteAvgBrightness);

    ////                // DB에서 찾는다
    ////                PgResult_TbChar resultTb2 =
    ////                    await PgService_TbChar.SelectRowByBasicAsync(model2.nWidth, model2.nHeight, model2.sHexArray);

    ////                // 있건, 없건 
    ////                result.listCharResult.Add(new OfrResult_TbCharInSet(model2, resultTb2.tbChar));

    ////                // 있으면
    ////                if (resultTb2.tbChar != null) // 찾으면 - 확장건를 체택하고 인덱스 증가
    ////                {
    ////                    sResult += resultTb2.tbChar.Character;
    ////                    x += 1;
    ////                    resultTb2.tbChar.Searched += 1;
    ////                    await PgService_TbChar.UpdateRowAsync(resultTb2.tbChar);
    ////                }
    ////                else // 못찾으면 - 한번더 마지막시도
    ////                {
    ////                    if (x < nBefLastIndex) // 두번더할 인덱스면
    ////                    {
    ////                        // 세번째 음소구하기
    ////                        StdConst_IndexRect rcIndex3 =
    ////                            OfrService.GetIndexRect_FromColorBitmapByIndex(bmpOrg, byteAvgBrightness, rcForeground, listStartEnd, x, x + 2);

    ////                        if (rcIndex3 == null)
    ////                        {
    ////                            result.sErr = "rcIndex3가 널 입니다";
    ////                            result.sPos = "OfrwORK_Common/OfrStrResultFrom_ComplexMultiCharBitmapAsync_06";

    ////                            return LocalCommon_OfrResult.ErrMsgResult_TbCharSetList(result, bWrite, bMsgBox);
    ////                        }

    ////                        // Get BitmapAnalysis
    ////                        Draw.Rectangle rcTmp3 = rcIndex3.GetDrawRectangle();
    ////                        OfrModel_BmpCharAnalysis model3 = OfrService.GetOfrModel_CharAnalysis_RectInBitmapFast(bmpOrg, rcTmp3, byteAvgBrightness);

    ////                        // DB에서 찾는다
    ////                        PgResult_TbChar resultTb3 =
    ////                            await PgService_TbChar.SelectRowByBasicAsync(model3.nWidth, model3.nHeight, model3.sHexArray);

    ////                        // 있건, 없건 
    ////                        result.listCharResult.Add(new OfrResult_TbCharInSet(model3, resultTb3.tbChar));

    ////                        if (resultTb3.tbChar != null) // 찾으면 - 확장건를 체택하고 인덱스 2증가
    ////                        {
    ////                            sResult += resultTb3.tbChar.Character;
    ////                            x += 2;
    ////                            if (nCountFind < resultTb3.tbChar.Searched)
    ////                            {
    ////                                resultTb3.tbChar.Searched += 1;
    ////                                await PgService_TbChar.UpdateRowAsync(resultTb3.tbChar);
    ////                            }
    ////                        }
    ////                        else // 못찾으면 - 자료저장
    ////                        {
    ////                            sResult += '☒';  // 임시로 보기위해 ...
    ////                        }
    ////                    }
    ////                    else
    ////                    {
    ////                        sResult += '☒';  // 임시로 보기위해 ...
    ////                    }
    ////                }
    ////            }
    ////            else
    ////            {
    ////                sResult += '☒';  // 임시로 보기위해 ...
    ////            }
    ////        }
    ////    }

    ////    if (sResult.Contains('☒'))
    ////    {
    ////        ErrMsgBox("코딩 해야함.", "OfrStrResultFrom_ComplexMultiCharBitmapAsync_003");
    ////    }
    ////    else // Char추출 성공하면 TbText에 저장
    ////    {
    ////        result.strResult = sResult;

    ////        TbText tbText = new TbText()
    ////        {
    ////            Text = sResult,
    ////            Width = analyText.nWidth,
    ////            Height = analyText.nHeight,
    ////            HexStrValue = analyText.sHexArray,
    ////            Threshold = 0,
    ////            Searched = 1,
    ////            Reserved = null
    ////        };

    ////        StdResult_Long resultLong = await PgService_TbText.InsertRowAsync(tbText);
    ////        if (resultLong.lResult <= 0) // 일단은 만약 실패해도 넘어가자.
    ////        {
    ////        }
    ////    }

    ////#region Tmp
    //////if (listFail.Count > 0) // 실패한 이미지가 있으면
    //////{
    //////    if (bSaveCharIfNotFind)
    //////        return new OfrResult_TbCharSet
    //////        {
    //////            strResult = "",
    //////            listFail = listFail,
    //////            sPos = "OfrWork_Common/OfrStrResultFrom_ComplexMultiCharBitmapAsync_12",
    //////        };

    //////    //MsgBox(str);
    //////    int lastIndex = sResult.Length - 1;
    //////    if (lastIndex > 2 && sResult[lastIndex] == '☒' && sResult[lastIndex - 1] != '☒')
    //////    {
    //////        sResult.Remove(lastIndex);
    //////        lastIndex = listFail.Count - 1;
    //////        listFail.RemoveAt(lastIndex);
    //////        if (listFail.Count == 0) goto EXIT_SUCCESS;
    //////    }

    //////    Bitmap bmpCharSet = OfrService.GetBitmapInBitmapFast(bmpOrg, rcForeground); // 전체이미지 에서 전경이미지 딴다
    //////    foreach (Bitmap bmp in listFail)
    //////    {
    //////        //ImageToCharWnd wnd = new ImageToCharWnd(bmpCharSet, bmp);
    //////        //wnd.ShowDialog();

    //////        //if (wnd.m_bResult == null) continue;
    //////        //else if (wnd.m_bResult == false)
    //////        //{
    //////        //    string msg = "사용자가 취소하였습니다";

    //////        //    return new OfrResult_TbCharSet
    //////        //    {
    //////        //        sErr = msg,
    //////        //        sPos = "OfrWork_Common/OfrStrResultFrom_ComplexMultiCharBitmapAsync_04",
    //////        //    };
    //////        //}

    //////        //StdResult_Bool resultBool =
    //////        //    await SaveAnySizeCharImage_ToPGAsync(bmp, wnd.m_sCharacter, wnd.m_sCharType);
    //////        //if (!resultBool.bResult)
    //////        //{
    //////        //    return new OfrResult_TbCharSet
    //////        //    {
    //////        //        sErr = resultBool.sErr,
    //////        //        sPos = "OfrWork_Common/OfrStrResultFrom_ComplexMultiCharBitmapAsync_05",
    //////        //    };
    //////        //}
    //////    }

    //////    listTbChar.Clear();
    //////    listFail.Clear();
    //////    for (int x = 0; x < listStartEnd.Count; x++)
    //////    {
    //////        // 첫번째 음소구하기
    //////        StdConst_IndexRect rcIndex1 =
    //////            OfrService.GetIndexRect_FromColorBitmapByIndex(bmpOrg, byteAvgBrightness, rcForeground, listStartEnd, x, x);
    //////        Draw.Rectangle rcTmp1 = rcIndex1.GetDrawRectangle();

    //////        // Get BitmapAnalysis
    //////        OfrModel_BitmapAnalysis model1 = OfrService.GetOfrModelAnalysis_InBitmapFast(bmpOrg, rcTmp1, byteAvgBrightness);

    //////        // DB에서 찾는다
    //////        PgResult_TbChar resultTb1 = await PgService_TbChar.SelectRowByBasicAsync(model1.nWidth, model1.nHeight, model1.sHexArray);

    //////        if (resultTb1.tbChar != null) // 찾으면
    //////        {
    //////            if (x < nLastIndex)
    //////            {
    //////                // 두번째 음소구하기
    //////                StdConst_IndexRect rcIndex2 =
    //////                    OfrService.GetIndexRect_FromColorBitmapByIndex(bmpOrg, byteAvgBrightness, rcForeground, listStartEnd, x, x + 1);
    //////                Draw.Rectangle rcTmp2 = rcIndex2.GetDrawRectangle();

    //////                // Get BitmapAnalysis
    //////                OfrModel_BitmapAnalysis model2 = OfrService.GetOfrModelAnalysis_InBitmapFast(bmpOrg, rcTmp2, byteAvgBrightness);

    //////                // DB에서 찾는다
    //////                PgResult_TbChar resultTb2 =
    //////                    await PgService_TbChar.SelectRowByBasicAsync(model2.nWidth, model2.nHeight, model2.sHexArray);

    //////                // 있으면
    //////                if (resultTb2.tbChar != null) // 찾으면 - 확장건를 체택하고 인덱스 증가
    //////                {
    //////                    listTbChar.Add(resultTb2.tbChar);

    //////                    sResult += resultTb2.tbChar.Character;
    //////                    x += 1;
    //////                }
    //////                else // 못찾으면 - 전건을 체택
    //////                {
    //////                    sResult += resultTb1.tbChar.Character;
    //////                }
    //////            }
    //////            else // 인덱스 여유가 없으면 인정
    //////            {
    //////                sResult += resultTb1.tbChar.Character;
    //////            }
    //////        }
    //////        else // 디비에서 못찾았으면 - MAX 2건더 체크
    //////        {
    //////            if (x < nLastIndex) // 한번더할 인덱스면
    //////            {
    //////                // 두번째 음소구하기
    //////                StdConst_IndexRect rcIndex2 =
    //////                    OfrService.GetIndexRect_FromColorBitmapByIndex(bmpOrg, byteAvgBrightness, rcForeground, listStartEnd, x, x + 1);
    //////                Draw.Rectangle rcTmp2 = rcIndex2.GetDrawRectangle();

    //////                // Get BitmapAnalysis
    //////                OfrModel_BitmapAnalysis model2 = OfrService.GetOfrModelAnalysis_InBitmapFast(bmpOrg, rcTmp2, byteAvgBrightness);

    //////                // DB에서 찾는다
    //////                PgResult_TbChar resultTb2 =
    //////                    await PgService_TbChar.SelectRowByBasicAsync(model2.nWidth, model2.nHeight, model2.sHexArray);

    //////                // 있으면
    //////                if (resultTb2.tbChar != null) // 찾으면 - 확장건를 체택하고 인덱스 증가
    //////                {
    //////                    listTbChar.Add(resultTb2.tbChar);

    //////                    sResult += resultTb2.tbChar.Character;
    //////                    x += 1;
    //////                }
    //////                else // 못찾으면 - 한번더 마지막시도
    //////                {
    //////                    if (x < nBefLastIndex) // 두번더할 인덱스가 아니면
    //////                    {
    //////                        // 세번째 음소구하기
    //////                        StdConst_IndexRect rcIndex3 =
    //////                            OfrService.GetIndexRect_FromColorBitmapByIndex(bmpOrg, byteAvgBrightness, rcForeground, listStartEnd, x, x + 2);
    //////                        Draw.Rectangle rcTmp3 = rcIndex2.GetDrawRectangle();

    //////                        // Get BitmapAnalysis
    //////                        OfrModel_BitmapAnalysis model3 = OfrService.GetOfrModelAnalysis_InBitmapFast(bmpOrg, rcTmp3, byteAvgBrightness);

    //////                        // DB에서 찾는다
    //////                        PgResult_TbChar resultTb3 =
    //////                            await PgService_TbChar.SelectRowByBasicAsync(model3.nWidth, model3.nHeight, model3.sHexArray);

    //////                        if (resultTb3.tbChar != null) // 찾으면 - 확장건를 체택하고 인덱스 2증가
    //////                        {
    //////                            listTbChar.Add(resultTb3.tbChar);
    //////                            sResult += resultTb3.tbChar.Character;
    //////                            x += 2;
    //////                        }
    //////                        else // 못찾으면 - 자료저장
    //////                        {
    //////                            goto EXIT_SUCCESS;
    //////                        }
    //////                    }
    //////                    else
    //////                    {
    //////                        goto EXIT_SUCCESS;
    //////                    }
    //////                }
    //////            }
    //////            else
    //////            {
    //////                goto EXIT_SUCCESS;
    //////            }
    //////        }
    //////    }
    //////}
    //////else // 실패한 이미지가 없으면 - 너무 복잡해질까봐, 성공한 이미지만 TbText에 저장
    //////{
    //////    if (bSaveTextIfNotFind)
    //////    {
    //////        TbText tbText = new TbText()
    //////        {
    //////            Text = sResult,
    //////            Width = model.nWidth,
    //////            Height = model.nHeight,
    //////            HexStrValue = model.sHexArray,
    //////            Threshold = 0,
    //////            Searched = 1,
    //////            Reserved = null
    //////        };

    //////        StdResult_Long resultLong = await PgService_TbText.InsertRowAsync(tbText);
    //////        if (resultLong.lResult <= 0)
    //////        {
    //////            return new OfrResult_TbCharSet()
    //////            {
    //////                sErr = resultLong.sErr,
    //////                sPos = "OfrWork_Common/OfrStrResultFrom_ComplexMultiCharBitmapAsync_06",
    //////            };
    //////        }
    //////    }
    //////} 

    //////if (string.IsNullOrEmpty(result.sErr)) // 성공이면 - Test를 디비에 저장
    //////{
    //////    TbText tbText = new TbText()
    //////    {
    //////        Text = sResult,
    //////        Width = model.nWidth,
    //////        Height = model.nHeight,
    //////        HexStrValue = model.sHexArray,
    //////        Threshold = 0,
    //////        Searched = 1,
    //////        Reserved = null
    //////    };

    //////    StdResult_Long resultLong = await PgService_TbText.InsertRowAsync(tbText);
    //////    if (resultLong.lResult <= 0) // 일단은 만약 실패해도 넘어가자.
    //////    {
    //////    }
    //////}
    ////#endregion

    ////EXIT_SUCCESS: return result;
    ////}

    ////public static async Task<OfrResult_TbCharSetList> OfrStrResultFrom_SeqCharBitmapAsync(Draw.Bitmap bmpCapture, Draw.Rectangle rcSpare, bool bEdit = true, bool bWrite = true, bool bMsgBox = true)
    ////{
    ////    OfrResult_TbCharSetList result = new OfrResult_TbCharSetList(bmpCapture);
    ////    string sResult = "";
    ////    byte byteAvgBrightness = OfrService.GetAverageBrightness_FromColorBitmapRectFast(bmpCapture, rcSpare);
    ////    OfrModel_BmpTextAnalysis modelText = null;
    ////    const int nMaxCountFind = 10;

    ////    #region Test
    ////    //if (byteAvgBrightness == 180) // Test
    ////    //{
    ////    //    byte max = OfrService.GetMaxBrightness_FromColorBitmapRectFas;t(bmpCapture, rcSpare);
    ////    //    byte min = OfrService.GetMinBrightness_FromColorBitmapRectFast(bmpCapture, rcSpare);
    ////    //    Debug.WriteLine($"AvgBrightness={byteAvgBrightness}, Max={max}, Min={min}"); // Test
    ////    //} 
    ////    #endregion

    ////    Draw.Rectangle rcForeground =
    ////        OfrService.GetForeGroundDrawRectangle_FromColorBitmapRectFast(bmpCapture, rcSpare, byteAvgBrightness, 0);

    ////    if (rcForeground == StdUtil.s_rcDrawEmpty)
    ////    {
    ////        result.sErr = "rcForeground Empty";
    ////        result.sPos = "OfrWork_Common/OfrStrResultFrom_SeqCharBitmapAsync_01";

    ////        return LocalCommon_OfrResult.ErrMsgResult_TbCharSetList(result, bWrite, bMsgBox);
    ////    }

    ////    //MsgBox($"rcSpare={rcSpare}, rcForeground={rcForeground}"); // Test
    ////    modelText = OfrService.GetOfrModel_TextAnalysis_RectInBitmapFast(bmpCapture, rcForeground, byteAvgBrightness);
    ////    PgResult_TbText findResult = PgService_TbText.SelectRowByBasic(modelText.nWidth, modelText.nHeight, modelText.sHexArray);

    ////    if (!string.IsNullOrEmpty(findResult.sErr))
    ////    {
    ////        result.sErr = findResult.sErr + findResult.sPos;
    ////        result.sPos = "OfrWork_Common/OfrStrResultFrom_SeqCharBitmapAsync_02";

    ////        return LocalCommon_OfrResult.ErrMsgResult_TbCharSetList(result, bWrite, bMsgBox);
    ////    }

    ////    if (findResult.tbText != null)
    ////    {
    ////        result.strResult = findResult.tbText.Text;
    ////        goto EXIT_SUCCESS; // 찾으면 탈출
    ////    }

    ////    List<OfrModel_StartEnd> listStartEnd = OfrService.GetStartEndList_FromColorBitmap(bmpCapture, byteAvgBrightness, rcForeground);

    ////    int nLastIndex = listStartEnd.Count - 1;
    ////    int nBefLastIndex = nLastIndex - 1;

    ////    for (int x = 0; x < listStartEnd.Count; x++)
    ////    {
    ////        // 음소구하기
    ////        StdConst_IndexRect rcIndex =
    ////            OfrService.GetIndexRect_FromColorBitmapByIndex(bmpCapture, byteAvgBrightness, rcForeground, listStartEnd, x, x);
    ////        if (rcIndex == null)
    ////        {
    ////            result.sErr = "rcIndex이 널 입니다";
    ////            result.sPos = "OfrWork_Common/OfrStrResultFrom_SeqCharBitmapAsync_03";

    ////            return LocalCommon_OfrResult.ErrMsgResult_TbCharSetList(result, bWrite, bMsgBox);
    ////        }
    ////        Draw.Rectangle rcTmp = rcIndex.GetDrawRectangle();
    ////        //MsgBox($"rcTmp={rcTmp}"); // Test

    ////        // Get BitmapAnalysis
    ////        OfrModel_BmpCharAnalysis modelChar = OfrService.GetOfrModel_CharAnalysis_RectInBitmapFast(bmpCapture, rcTmp, byteAvgBrightness);
    ////        //MsgBox($"{modelChar.nWidth}, {modelChar.nHeight}, {modelChar.sHexArray}"); // Test

    ////        // DB에서 찾는다
    ////        PgResult_TbChar resultTb = await PgService_TbChar.SelectRowByBasicAsync(modelChar.nWidth, modelChar.nHeight, modelChar.sHexArray);
    ////        //if (resultTb.tbChar != null) MsgBox($"resultTbChar={resultTb.tbChar.Character}"); // Test
    ////        //else MsgBox($"resultTbChar=널"); // Test

    ////        if (resultTb.tbChar != null) // 찾으면
    ////        {
    ////            sResult += resultTb.tbChar.Character;

    ////            if (nMaxCountFind < resultTb.tbChar.Searched)
    ////            {
    ////                resultTb.tbChar.Searched += 1;
    ////                await PgService_TbChar.UpdateRowAsync(resultTb.tbChar);
    ////            }

    ////            result.listCharResult.Add(new OfrResult_TbCharInSet(modelChar, resultTb.tbChar));
    ////        }
    ////        else // 디비에서 못찾았으면
    ////        {
    ////            if (s_bDebugMode && bEdit)
    ////            {
    ////                await Wnd.Application.Current.Dispatcher.InvokeAsync(() =>
    ////                {
    ////                    ImageToCharWnd wnd = new ImageToCharWnd(modelChar);
    ////                    wnd.ShowDialog();

    ////                    if (wnd.tbChar == null) sResult += '☒';
    ////                    else sResult += wnd.tbChar.Character;

    ////                    result.listCharResult.Add(new OfrResult_TbCharInSet(modelChar, wnd.tbChar));
    ////                });
    ////            }
    ////            else
    ////            {
    ////                sResult += '☒';
    ////                result.listCharResult.Add(new OfrResult_TbCharInSet(modelChar, null));
    ////            }
    ////        }
    ////    }

    ////    result.strResult = sResult;

    ////    // 정상적인 문자열이면
    ////    if (!string.IsNullOrEmpty(sResult) && !sResult.Contains('☒'))
    ////    {
    ////        TbText tbText = new TbText()
    ////        {
    ////            Text = sResult,
    ////            Width = modelText.nWidth,
    ////            Height = modelText.nHeight,
    ////            HexStrValue = modelText.sHexArray,
    ////            Threshold = 0,
    ////            Searched = 1,
    ////            Reserved = null
    ////        };

    ////        StdResult_Long resultLong = await PgService_TbText.InsertRowAsync(tbText);

    ////        if (!string.IsNullOrEmpty(resultLong.sErr))
    ////        {
    ////            result.sErr = resultLong.sErr + resultLong.sPos;
    ////            result.sPos = "OfrWork_Common/OfrStrResultFrom_SeqCharBitmapAsync_02";

    ////            return LocalCommon_OfrResult.ErrMsgResult_TbCharSetList(result, bWrite, bMsgBox);
    ////        }
    ////    }

    ////EXIT_SUCCESS: return result;
    ////}

    //////public static async Task<OfrResult_TbCharSet> OfrStrResultFrom_SeqCharBitmapAsync(
    //////   IntPtr hWndTop, Draw.Rectangle rcRel, bool bSaveTextIfNotFind, bool bSaveCharIfNotFind, int nCountFind = 10)
    //////{
    //////    if (Application.Current == null) return LocalCommon_OfrResult.ErrMsgResult_TbCharSet(
    //////        "Application.Current이 널 입니다.", "OfrwORK_Common/OfrStrResultFrom_SeqCharBitmapAsync_01");

    //////    Bitmap bmpOrg = OfrService.CaptureScreenRect_InWndHandle(hWndTop, rcRel);
    //////    if (bmpOrg == null) return LocalCommon_OfrResult
    //////        .ErrMsgResult_TbCharSet("bmpOrg이 널 입니다.", "OfrwORK_Common/OfrStrResultFrom_SeqCharBitmapAsync_02");

    //////    return await OfrStrResultFrom_SeqCharBitmapAsync(bmpOrg, rcRel, bSaveTextIfNotFind, bSaveCharIfNotFind, nCountFind);
    //////}

    //////public static async Task<OfrResult_TbCharSet> OfrStrResultFrom_Test()
    //////{
    //////    return await Application.Current.Dispatcher.Invoke(async () =>
    //////    {
    //////        OfrResult_TbCharSet resultChSet = new OfrResult_TbCharSet();
    //////        string str = "";

    //////        str += "가";
    //////        resultChSet.strResult = str;
    //////        await Task.Delay(1);

    //////        return resultChSet;
    //////    }, System.Windows.Threading.DispatcherPriority.Normal); // 우선순위 지정
    //////}

    //// Wait
    ////public static async Task<bool> OfrWaitUntil_ImgNotMatchedAsync(string sImgName, IntPtr hWnd, int offset, int nDelay = 50, int nMiliSec = 3000)
    ////{
    ////    int count = nMiliSec / nDelay;
    ////    Draw.Bitmap bmpLoose = null;

    ////    for (int i = 0; i < count; i++)
    ////    {
    ////        await Task.Delay(nDelay);

    ////        // Gab 빼고 Capture
    ////        bmpLoose = OfrService.CaptureScreenRect_InWndHandle(hWnd, offset);
    ////        if (bmpLoose == null) continue;

    ////        OfrResult_TbText resultImg = await OfrImage_ExactDrawRelRectAsync(bmpLoose); // TbText에서 이미지 찾기
    ////        if (resultImg._sResult != sImgName) return true;
    ////    }

    ////    return false;
    ////}

    ///// <summary>
    ///// 목표 이미지가 나타날 때까지 대기 (개선된 버전)
    ///// </summary>
    ///// <param name="sTargetImage">목표 이미지 이름 (예: "Img_전체버튼_Down")</param>
    ///// <param name="hWnd">대상 윈도우 핸들</param>
    ///// <param name="offset">오프셋 (GAB)</param>
    ///// <param name="bEdit">DB에 없을 시 수동 입력 다이얼로그 표시 여부</param>
    ///// <param name="checkInterval">확인 간격 (ms)</param>
    ///// <param name="maxWaitTime">최대 대기 시간 (ms)</param>
    ///// <returns>성공 시 true, 실패 시 에러 정보 포함</returns>
    //public static async Task<StdResult_NulBool> OfrWaitUntilImageAppearsAsync(
    //    string sTargetImage, IntPtr hWnd, int offset, bool bEdit = false, int checkInterval = 50, int maxWaitTime = 3000)
    //{
    //    int checkCount = maxWaitTime / checkInterval;
    //    Draw.Bitmap bmp = null;
    //    bool bLastAttempt = false;

    //    for (int i = 0; i < checkCount; i++)
    //    {
    //        await Task.Delay(checkInterval);
    //        bLastAttempt = (i == checkCount - 1); // 마지막 시도만 다이얼로그 표시

    //        // 캡처
    //        bmp = OfrService.CaptureScreenRect_InWndHandle(hWnd, offset);
    //        if (bmp == null)
    //        {
    //            Debug.WriteLine($"[OfrWork_Common] 캡처 실패 ({i + 1}/{checkCount})");
    //            continue;
    //        }

    //        // OFR 이미지 확인 (마지막 시도에만 bEdit 활성화)
    //        OfrResult_TbText result = await OfrImage_ExactDrawRelRectAsync(bmp, bLastAttempt && bEdit, false, false);
    //        if (result._sResult == sTargetImage)
    //        {
    //            Debug.WriteLine($"[OfrWork_Common] 목표 이미지 확인: {sTargetImage} (시도 {i + 1}/{checkCount})");
    //            return new StdResult_NulBool(true);
    //        }

    //        Debug.WriteLine($"[OfrWork_Common] 이미지 대기 중: 현재={result._sResult}, 목표={sTargetImage} ({i + 1}/{checkCount})");
    //    }

    //    // 타임아웃
    //    string errMsg = $"타임아웃: {sTargetImage} 이미지가 {maxWaitTime}ms 동안 나타나지 않음";
    //    Debug.WriteLine($"[OfrWork_Common] {errMsg}");
    //    return new StdResult_NulBool(errMsg, "OfrWork_Common/OfrWaitUntilImageAppearsAsync");
    //}

    // Save
    //public static async Task<StdResult_Bool> SaveObjectUnitAsync(
    //    Bitmap bmpOrg, string sText, string sReserved = "", byte MaxBrightness = 254, byte MinBrightness = 64)
    //{
    //    List<OfrModel_BitmapAnalysis> listInfoAnaly = new List<OfrModel_BitmapAnalysis>(); // 중복제거용
    //    List<TbText> listTbText = new List<TbText>();

    //    try
    //    {
    //        // 밝기별로
    //        for (byte curBrightness = MaxBrightness; curBrightness > MinBrightness; curBrightness--)
    //        {
    //            // 문자영역의 배열정보 추출
    //            OfrModel_BitmapAnalysis infoAlaly = OfrService.GetOfrModelAnalysis_InBitmapFast(bmpOrg!, curBrightness);
    //            if (infoAlaly == null)
    //                return new StdResult_Bool(false, "문자영역의 배열정보 추출 실패", "OfrWork_Common/SaveObjectUnitAsync_01");

    //            if (infoAlaly.bitArray == null) continue;
    //            if (infoAlaly.trueRate < 0.01) continue; // 1% 미만은 제외
    //            if (OfrService.IsExistBasicAnalyInfo(listInfoAnaly, infoAlaly)) continue;

    //            listInfoAnaly.Add(infoAlaly); // 중복제거용

    //            // DB에서 찾기
    //            PgResult_TbText findResult =
    //                await PgService_TbText.SelectRowByBasicAsync(infoAlaly.nWidth, infoAlaly.nHeight, infoAlaly.sHexArray);

    //            // DB에서 발견 안되었으면
    //            if (findResult.tbText == null)
    //            {
    //                TbText tbText = new TbText();

    //                tbText.Text = sText;
    //                tbText.Width = infoAlaly.nWidth;
    //                tbText.Height = infoAlaly.nHeight;
    //                tbText.HexStrValue = infoAlaly.sHexArray;
    //                tbText.Threshold = curBrightness;
    //                tbText.Searched = 0;
    //                tbText.Reserved = sReserved;

    //                listTbText.Add(tbText);
    //            }
    //        }

    //        // DB에 입력
    //        if (listTbText.Count > 0)
    //        {
    //            StdResult_Int resultInt = await PgService_TbText.InsertRowsAsync(listTbText);
    //            if (resultInt.nResult != listTbText.Count)
    //            {
    //                return new StdResult_Bool(false, "DB에 입력 실패", "OfrWork_Common/SaveObjectUnitAsync_02");
    //            }
    //        }

    //        return new StdResult_Bool(true);
    //    }
    //    catch (Exception ex)
    //    {
    //        return new StdResult_Bool(false, ex.Message, "OfrWork_Common/SaveObjectUnitAsync_999");
    //    }
    //}

    //public static Task<StdResult_Bool> SaveAnySizeCharImage_ToPGAsync(Bitmap bmpForground, string sCharValue, string sCharType)
    //{
    //    Draw.Rectangle rcForeground = new Rectangle(0, 0, bmpForground.Width, bmpForground.Height);
    //    int nForegroundLen = OfrService.GetLongerLen(rcForeground);
    //    //MsgBox($"{nOrgForegroundLen}");
    //    double ratioScale = 1;

    //    if (nForegroundLen > 32)
    //    {
    //        ratioScale = OfrService.큰쪽기반가로세로비율구하기(rcForeground, 32); // 비율계산
    //        bmpForground = OfrService.ConvertSizeBitmap(bmpForground, ratioScale); // 리사이즈
    //    }

    //    return SaveRealCharImage_ToPGAsync(bmpForground, sCharValue, sCharType, ratioScale);//, byteBrightMargin);
    //}

    //private static async Task<StdResult_Bool> SaveRealCharImage_ToPGAsync(
    //    Bitmap bmpAjusted, string sCharValue, string sCharType, double dRatioScale)
    //{
    //    //StdResult_Bool result = new StdResult_Bool(true);
    //    List<OfrModel_BitmapAnalysis> listInfo = new List<OfrModel_BitmapAnalysis>(); // 중복제거용
    //    byte bMax = 254;
    //    byte bMin = 64;
    //    float ratioWH = 1;

    //    try
    //    {
    //        // 밝기별로
    //        for (byte bCur = bMax; bCur >= bMin; bCur--)
    //        {
    //            //Debug.WriteLine($"curBrightness={bCur}"); // Test

    //            // 밝기별로 문자영역 추출
    //            Draw.Rectangle rcForeground =
    //                OfrService.GetForeGroundDrawRectangle_FromColorBitmapFast(bmpAjusted, bCur, 0);
    //            if (rcForeground == StdUtil.s_rcDrawEmpty) continue;
    //            if (sCharType == "H") // 한글이면 - 가로, 세로 비율이 이상하면
    //            {
    //                ratioWH = (float)rcForeground.Width / rcForeground.Height;
    //                if (ratioWH < 0.5 || ratioWH > 1.5) continue;
    //            }

    //            // 문자영역의 배열정보 추출
    //            OfrModel_BitmapAnalysis infoAnaly = OfrService.GetOfrModelAnalysis_InBitmapFast(bmpAjusted, rcForeground, bCur);
    //            //Debug.WriteLine($"OfrModel_BitmapAnalysis[{bCur}]: {infoAnaly}"); // Test

    //            if (infoAnaly == null) continue;
    //            if (infoAnaly.trueRate < 0.2) continue;
    //            if (OfrService.IsExistBasicAnalyInfo(listInfo, infoAnaly)) continue;

    //            listInfo.Add(infoAnaly); // 중복제거용

    //            #region Test
    //            #endregion

    //            // DB에서 찾기
    //            PgResult_TbChar findResult = await PgService_TbChar.SelectRowByExtendedBasicAsync(
    //                infoAnaly.nWidth, infoAnaly.nHeight, infoAnaly.sHexArray, sCharType);

    //            // DB에서 발견되었으면
    //            if (findResult.tbChar != null)
    //            {
    //                if (findResult.tbChar.Character != sCharValue)// 다른문자면 - 삭제
    //                {
    //                    long keyCode = findResult.tbChar.KeyCode;

    //                    // DB에서 삭제
    //                    StdResult_Bool resultDel = PgService_TbChar.DeleteRow(keyCode);
    //                    if (!resultDel.bResult) 
    //                        return new StdResult_Bool(resultDel.sErr, "OfrWork_Common/SaveRealCharImage_ToPGAsync_01");
    //                }
    //            }
    //            else // DB에 없으면 - 추가
    //            {
    //                // 새로운 문자 추가
    //                TbChar tb = new TbChar()
    //                {
    //                    Character = sCharValue,
    //                    FontSize = 0,
    //                    FontWeight = "",
    //                    FontFamily = "",
    //                    Width = infoAnaly.nWidth,
    //                    Height = infoAnaly.nHeight,
    //                    WidhHeightRate = (float)infoAnaly.nWidth / infoAnaly.nHeight,
    //                    HexStrValue = infoAnaly.sHexArray,
    //                    SizeScale = dRatioScale,
    //                    Threshold = bCur,
    //                    Searched = 0,
    //                    CharType = sCharType
    //                };

    //                // DB에 추가
    //                StdResult_Long resultLong = PgService_TbChar.InsertRow(tb);
    //                if (resultLong.lResult <= 0)  // 실패
    //                    return new StdResult_Bool(resultLong.sErr, "OfrWork_Common/SaveRealCharImage_ToPGAsync_02");
    //            }
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        return new StdResult_Bool(ex.Message, "OfrWork_Common/SaveRealCharImage_ToPGAsync_999");
    //    }

    //    return new StdResult_Bool(true);
    //}

    #region Read - Pv/Invoke를 이용한 정보얻기
    //public static StdResult_String ReadEditBox_FromRelPoint(IntPtr hWndBasic, Draw.Point ptRel)
    //{
    //    string str = Std32Window.GetWindowCaption_FromRelDrawPt(hWndBasic, ptRel);
    //    return new StdResult_String(str);
    //}

    public static async Task<StdResult_Bool> WriteEditBox_ToHndleAsync(IntPtr hWnd, string sData, int nDelay = 30)
    {
        int nResult = Std32Window.SetWindowCaption(hWnd, sData);
        if (nResult != 1) nResult = Std32Window.SetWindowText(hWnd, sData);

        await Task.Delay(nDelay);

        return new StdResult_Bool(nResult == 1);
    }
    public static async Task<string> WriteEditBox_ToHndleAsyncWait(IntPtr hWnd, string sData, int nDelay = 50, int nRepeat = 20)
    {
        int nResult = Std32Window.SetWindowCaption(hWnd, sData);
        if (nResult != 1) nResult = Std32Window.SetWindowText(hWnd, sData);

        string sTmp = null;
        for (int i = 0; i < nRepeat; i++)
        {
            await Task.Delay(nDelay);
            if ((sTmp = Std32Window.GetWindowCaption(hWnd)) == sData) break;
        }

        return sTmp;
    }
    ///// <summary>
    ///// EditBox에 텍스트 쓰기 + 화면 갱신 (SetFocus + Write + InvalidateRect + UpdateWindow)
    ///// </summary>
    //public static async Task<StdResult_Bool> WriteEditBox_ToHndleAsyncUpdate(IntPtr hWnd, string sData, int nDelay = 50, int nRepeat = 20)
    //{
    //    // 1. 포커스 설정
    //    await Std32Window.SetFocusWithForegroundAsync(hWnd);

    //    // 2. 텍스트 쓰기 (대기+검증 포함)
    //    string writtenValue = await WriteEditBox_ToHndleAsyncWait(hWnd, sData, nDelay, nRepeat);

    //    // 3. 강제 화면 갱신
    //    StdWin32.InvalidateRect(hWnd, IntPtr.Zero, true);
    //    StdWin32.UpdateWindow(hWnd);

    //    return new StdResult_Bool(writtenValue == sData);
    //}

    ////[Obsolete("Use WriteEditBox_ToHndleAsync_WithEnterKeyWait")]
    ////public static async Task WriteEditBox_ToHndleAsync_WithEnterKey(IntPtr hWnd, string sData, int nDelay1 = 30, int nDelay2 = c_nWaitLong)
    ////{
    ////    int nResult = Std32Window.SetWindowCaption(hWnd, sData);
    ////    if (nResult != 1) nResult = Std32Window.SetWindowText(hWnd, sData);

    ////    await Task.Delay(nDelay1);
    ////    await Std32Key_Msg.KeyPostAsync_MouseClickNDown(hWnd, StdCommon32.VK_RETURN);
    ////    await Task.Delay(nDelay2);
    ////}
    ////public static async Task WriteEditBox_ToHndleAsync_WithEnterKeyWait(IntPtr hWnd, string sData, int nDelay = 50, int nRepeat = 20)
    ////{
    ////    int nResult = Std32Window.SetWindowCaption(hWnd, sData);
    ////    if (nResult != 1) nResult = Std32Window.SetWindowText(hWnd, sData);

    ////    for (int i = 0; i < nRepeat; i++)
    ////    {
    ////        await Task.Delay(nDelay);
    ////        if (Std32Window.GetWindowCaption(hWnd) == sData) break;
    ////    }

    ////    await Task.Delay(nDelay);
    ////    await Std32Key_Msg.KeyPostAsync_MouseClickNDown(hWnd, StdCommon32.VK_RETURN);
    ////    await Task.Delay(nDelay);
    ////}

    ////public static async Task<StdResult_Bool> WriteEditBox_ToRelPointAsync(IntPtr hWndBasic, Draw.Point ptRel, string sData, int nDelay = 30)
    ////{
    ////    IntPtr hWndEdit = Std32Window.GetWndHandle_FromRelDrawPt(hWndBasic, ptRel);
    ////    if (hWndEdit == IntPtr.Zero) return null;

    ////    return await WriteEditBox_ToHndleAsync(hWndEdit, sData, nDelay);
    ////}


    //public static StdResult_String ReadEditBox_FromChildWndIndex(IntPtr hWndBasic, Draw.Point ptRel, int[] sons)
    //{
    //    IntPtr hWndFind = Std32Window.GetWndHandle_FromRelDrawPt(hWndBasic, ptRel);
    //    IntPtr hWndChild = IntPtr.Zero;

    //    for (int i = 0; i < sons.Length; i++)
    //    {
    //        hWndChild = Std32Window.FindChildWindow(hWndFind, sons[i]);
    //        //if (hWndChild == IntPtr.Zero) break;
    //        hWndFind = hWndChild;
    //    }
    //    //MsgBox($"hWndChild={hWndChild:X}, {MyWin32.GetWindowText(hWndChild)}");

    //    if (hWndChild == IntPtr.Zero)
    //        return new StdResult_String("hWndChild is null", "OfrWork_Common/ReadEditBox_FromChildWndIndex_01");

    //    return new StdResult_String(Std32Window.GetWindowCaption(hWndChild));
    //}

    #region ErrMsgResult Helper Functions
    // 에러 메시지를 출력하고 StdResult_Error를 반환
    protected static StdResult_Error ErrMsgResult_Error(string sErr, string sPos)
    {
        Debug.WriteLine($"[ErrMsgResult_Error] {sPos}: {sErr}");
        return new StdResult_Error(sErr, sPos);
    }

    // 에러 메시지를 출력하고 StdResult_NulBool을 반환
    protected static StdResult_NulBool ErrMsgResult_NulBool(string sErr, string sPos)
    {
        Debug.WriteLine($"[ErrMsgResult_NulBool] {sPos}: {sErr}");
        return new StdResult_NulBool(sErr, sPos);
    }

    // 에러 메시지를 출력하고 OfrResult_TbText를 반환
    protected static OfrResult_TbText ErrMsgResult_TbText(OfrModel_BitmapAnalysis info, TbText tbText, string sErr, string sPos)
    {
        Debug.WriteLine($"[ErrMsgResult_TbText] {sPos}: {sErr}");
        return new OfrResult_TbText(info, sErr, sPos) { tbText = tbText };
    }
    #endregion

    #region CheckBox 상태 변경
    // CheckBox 클릭 및 상태 변경 (현재 상태 확인 → 클릭 → 변경 확인)
    public static async Task<StdResult_Error> SetCheckBox_StatusAsync(
        IntPtr hWndTop,
        Draw.Rectangle rcRelM,
        bool bCheck,
        string checkBoxName = "CheckBox")
    {
        try
        {
            IntPtr hWnd = IntPtr.Zero;
            Draw.Point ptRelM = StdUtil.GetCenterDrawPoint(rcRelM);

            // 1. 현재 상태 읽기
            StdResult_NulBool resultChkBox = await OfrWork_Insungs.OfrImgReChkValue_RectInHWndAsync(hWndTop, rcRelM);
            if (resultChkBox.bResult == null)
                return ErrMsgResult_Error(
                    $"{checkBoxName} 인식 실패", "OfrWork_Common/SetCheckBox_StatusAsync_01");

            // 2. 이미 원하는 상태면 성공
            if (resultChkBox.bResult == bCheck) return null;

            // 3. 클릭 루프
            for (int j = 0; j < c_nRepeatShort; j++)
            {
                await Task.Delay(100);

                hWnd = Std32Window.GetWndHandle_FromRelDrawPt(hWndTop, ptRelM);
                await Std32Mouse_Post.MousePostAsync_ClickLeft(hWnd);

                resultChkBox = await OfrWork_Insungs.OfrImgUntilChkValue_RectInHWndAsync(hWndTop, bCheck, rcRelM);

                if (resultChkBox.bResult == true) break;
            }

            // 4. 최종 확인
            if (resultChkBox.bResult == true) return null;

            return ErrMsgResult_Error(
                $"{checkBoxName} 상태 변경 실패 (원하는 상태: {(bCheck ? "Checked" : "Unchecked")})",
                "OfrWork_Common/SetCheckBox_StatusAsync_04");
        }
        catch (Exception ex)
        {
            return ErrMsgResult_Error(
                StdUtil.GetExceptionMessage(ex), "OfrWork_Common/SetCheckBox_StatusAsync_999");
        }
    }
    #endregion
    #endregion
}
#nullable enable