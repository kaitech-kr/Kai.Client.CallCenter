using System;
using System.Diagnostics;
using System.Collections.Generic;
using Draw = System.Drawing;
using Kai.Common.NetDll_WpfCtrl.NetOFR;

namespace Kai.Client.CallCenter.Test;

/// <summary>
/// Threshold별 HexString 중복 테스트
/// </summary>
public class TestThresholdDuplication
{
    public static void TestDuplication()
    {
        // 테스트용 간단한 비트맵 생성 (예: "신규" 버튼과 유사한 크기)
        Draw.Bitmap testBmp = CreateSampleBitmap();

        HashSet<string> uniqueHexStrings = new HashSet<string>();
        Dictionary<string, List<byte>> hexToThresholds = new Dictionary<string, List<byte>>();

        byte minThreshold = 65;
        byte maxThreshold = 254;

        Debug.WriteLine($"=== Threshold 중복 테스트 시작 ({minThreshold}~{maxThreshold}) ===");
        Debug.WriteLine($"테스트 이미지 크기: {testBmp.Width}x{testBmp.Height}");

        // 각 threshold별로 HexString 생성
        for (byte threshold = minThreshold; threshold <= maxThreshold; threshold++)
        {
            OfrModel_BitmapAnalysis analysis = OfrService.GetBitmapAnalysisFast(testBmp, threshold);

            if (analysis != null && analysis.sHexArray != null)
            {
                string hexStr = analysis.sHexArray;
                uniqueHexStrings.Add(hexStr);

                if (!hexToThresholds.ContainsKey(hexStr))
                {
                    hexToThresholds[hexStr] = new List<byte>();
                }
                hexToThresholds[hexStr].Add(threshold);
            }
        }

        Debug.WriteLine($"\n=== 테스트 결과 ===");
        Debug.WriteLine($"전체 Threshold 개수: {maxThreshold - minThreshold + 1}");
        Debug.WriteLine($"고유 HexString 개수: {uniqueHexStrings.Count}");
        Debug.WriteLine($"중복 제거율: {(1 - (double)uniqueHexStrings.Count / (maxThreshold - minThreshold + 1)) * 100:F1}%");

        Debug.WriteLine($"\n=== 각 HexString별 Threshold 범위 ===");
        int index = 1;
        foreach (var kvp in hexToThresholds)
        {
            var thresholds = kvp.Value;
            Debug.WriteLine($"#{index} ({thresholds.Count}개): {thresholds[0]}~{thresholds[thresholds.Count - 1]}");
            index++;
        }

        testBmp.Dispose();
    }

    private static Draw.Bitmap CreateSampleBitmap()
    {
        // 92x40 크기의 "신규" 버튼과 유사한 샘플 이미지 생성
        int width = 92;
        int height = 40;
        Draw.Bitmap bmp = new Draw.Bitmap(width, height);

        using (Draw.Graphics g = Draw.Graphics.FromImage(bmp))
        {
            // 배경 (밝은 회색)
            g.Clear(Draw.Color.FromArgb(240, 240, 240));

            // 텍스트 영역 시뮬레이션 (검은색 픽셀들)
            Draw.SolidBrush blackBrush = new Draw.SolidBrush(Draw.Color.Black);

            // 간단한 글자 모양 시뮬레이션 (몇 개의 사각형으로)
            g.FillRectangle(blackBrush, 10, 10, 3, 20);  // |
            g.FillRectangle(blackBrush, 10, 10, 10, 3);  // ㄱ 위
            g.FillRectangle(blackBrush, 10, 27, 10, 3);  // ㄱ 아래

            g.FillRectangle(blackBrush, 30, 10, 3, 20);  // |
            g.FillRectangle(blackBrush, 30, 18, 10, 3);  // -

            g.FillRectangle(blackBrush, 50, 10, 10, 3);  // ㄱ
            g.FillRectangle(blackBrush, 57, 10, 3, 10);
            g.FillRectangle(blackBrush, 50, 20, 10, 3);
            g.FillRectangle(blackBrush, 50, 20, 3, 10);

            blackBrush.Dispose();
        }

        return bmp;
    }
}
