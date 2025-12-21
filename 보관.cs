using Kai.Common.StdDll_Common;
using Kai.Common.StdDll_Common.StdWin32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgBox;
using static Kai.Common.StdDll_Common.StdConst_FuncName;
//using System.Drawing.Imaging; // Nuget System.Drawing.Common
using Draw = System.Drawing;
using DrawImg = System.Drawing.Imaging;
using Medias = System.Windows.Media;

namespace Kai.Common.NetDll_WpfCtrl.NetOFR;
#nullable disable
public class OfrService
{
    #region Get - Normal
    // 사각형의 긴변 길이 반환
    public static int GetLongerLen(Draw.Bitmap bmp)
    {
        return bmp.Width > bmp.Height ? bmp.Width : bmp.Height;
    }
    public static int GetLongerLen(Draw.Rectangle rc)
    {
        return rc.Width > rc.Height ? rc.Width : rc.Height;
    }
    public static int GetLongerLen(int Width, int Height)
    {
        return Width > Height ? Width : Height;
    }

    // 큰쪽 기반 가로세로 비율 구하기 (Rectangle의 긴 변을 기준으로 스케일 비율 계산)
    // rc: 대상 Rectangle, nTargetLength: 목표 길이, returns: 스케일 비율
    public static double GetScaleRateFromDrawRectangle(Draw.Rectangle rc, int nTargetLength)
    {
        // 파라미터 검증
        if (rc.Width <= 0 || rc.Height <= 0)
        {
            Debug.WriteLine("[ERROR] GetScaleRateFromDrawRectangle: Rectangle의 크기가 유효하지 않습니다.");
            return 1.0; // 기본값 반환
        }

        if (nTargetLength <= 0)
        {
            Debug.WriteLine("[ERROR] GetScaleRateFromDrawRectangle: nTargetLength는 0보다 커야 합니다.");
            return 1.0; // 기본값 반환
        }

        int nBigger = rc.Width > rc.Height ? rc.Width : rc.Height;
        return (double)nTargetLength / (double)nBigger;
    }

    // 전경 영역 찾기 (색상 비트맵에서 threshold 기반으로 전경 영역 추출)
    // bmpColor: 소스 비트맵, threshold: 명도 임계값, nMargin: 마진, returns: 전경 영역 Rectangle
    public static Draw.Rectangle GetForeGroundDrawRectangle_FromColorBitmapFast(Draw.Bitmap bmpColor, byte threshold, int nMargin)
    {
        if (bmpColor == null)
            return StdUtil.s_rcDrawEmpty;

        int width = bmpColor.Width;
        int height = bmpColor.Height;
        int lastX = width - 1;
        int lastY = height - 1;

        // 최소/최대 좌표 초기화
        int x1 = width, y1 = height, x2 = -1, y2 = -1;

        var rect = new Draw.Rectangle(0, 0, width, height);
        DrawImg.BitmapData bmpData = bmpColor.LockBits(rect, DrawImg.ImageLockMode.ReadOnly, bmpColor.PixelFormat);

        try
        {
            int bytesPerPixel = Draw.Image.GetPixelFormatSize(bmpColor.PixelFormat) / 8;
            int stride = bmpData.Stride;
            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;

                for (int y = 0; y < height; y++)
                {
                    byte* row = ptr + (y * stride);
                    for (int x = 0; x < width; x++)
                    {
                        byte* pixel = row + (x * bytesPerPixel);

                        // Format32bppArgb 가정: [0]=B, [1]=G, [2]=R, [3]=A
                        int b = pixel[0];
                        int g = pixel[1];
                        int r = pixel[2];

                        // OfrService.ConvertColorPixelToBool의 로직: 그레이스케일 계산 후 threshold 비교
                        int gray = (int)(r * 0.3 + g * 0.59 + b * 0.11);
                        bool bBlack = gray < threshold;

                        if (bBlack)
                        {
                            if (x < x1) x1 = x;
                            if (x > x2) x2 = x;
                            if (y < y1) y1 = y;
                            if (y > y2) y2 = y;
                        }
                    }
                }
            }
        }
        finally
        {
            bmpColor.UnlockBits(bmpData);
        }

        // 전경 픽셀이 하나도 없으면 빈 사각형 반환
        if (x2 < x1 || y2 < y1)
            return StdUtil.s_rcDrawEmpty;

        // 마진 적용 (이미지 경계를 벗어나지 않도록 보정)
        x1 = Math.Max(0, x1 - nMargin);
        y1 = Math.Max(0, y1 - nMargin);
        x2 = Math.Min(lastX, x2 + nMargin);
        y2 = Math.Min(lastY, y2 + nMargin);

        int rectWidth = x2 - x1 + 1;
        int rectHeight = y2 - y1 + 1;
        if (rectWidth <= 0) rectWidth = 1;
        if (rectHeight <= 0) rectHeight = 1;

        return new Draw.Rectangle(x1, y1, rectWidth, rectHeight);
    }

    public static Draw.Rectangle GetForeGroundDrawRectangle_FromColorBitmapRectFast(
        Draw.Bitmap bmpColor, Draw.Rectangle rcSpare, byte threshold, int nMargin)
    {
        if (bmpColor == null || rcSpare.Width <= 0 || rcSpare.Height <= 0 || rcSpare.X < 0 || rcSpare.Y < 0 ||
            rcSpare.X + rcSpare.Width > bmpColor.Width || rcSpare.Y + rcSpare.Height > bmpColor.Height)
            return StdUtil.s_rcDrawEmpty;

        // 최소/최대 좌표 초기화
        int x1 = rcSpare.X + rcSpare.Width, y1 = rcSpare.Y + rcSpare.Height, x2 = -1, y2 = -1;

        var rect = new Draw.Rectangle(rcSpare.X, rcSpare.Y, rcSpare.Width, rcSpare.Height);
        System.Drawing.Imaging.BitmapData bmpData = bmpColor.LockBits(rect,
            System.Drawing.Imaging.ImageLockMode.ReadOnly, bmpColor.PixelFormat);

        try
        {
            int bytesPerPixel = Draw.Image.GetPixelFormatSize(bmpColor.PixelFormat) / 8;
            int stride = bmpData.Stride;
            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;

                for (int y = 0; y < rcSpare.Height; y++)
                {
                    byte* row = ptr + (y * stride);
                    for (int x = 0; x < rcSpare.Width; x++)
                    {
                        byte* pixel = row + (x * bytesPerPixel);

                        // Format32bppArgb 가정: [0]=B, [1]=G, [2]=R, [3]=A
                        int b = pixel[0];
                        int g = pixel[1];
                        int r = pixel[2];

                        // 그레이스케일 계산 후 threshold 비교
                        int gray = (int)(r * 0.3 + g * 0.59 + b * 0.11);
                        bool bBlack = gray < threshold;

                        if (bBlack)
                        {
                            int globalX = x + rcSpare.X;
                            int globalY = y + rcSpare.Y;

                            if (globalX < x1) x1 = globalX;
                            if (globalX > x2) x2 = globalX;
                            if (globalY < y1) y1 = globalY;
                            if (globalY > y2) y2 = globalY;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"GetForeGroundDrawRectangle_FromColorBitmapRectFast 실패: {ex.Message}");
            return StdUtil.s_rcDrawEmpty;
        }
        finally
        {
            bmpColor.UnlockBits(bmpData);
        }

        // 전경 픽셀이 하나도 없으면 빈 사각형 반환
        if (x2 < x1 || y2 < y1)
            return StdUtil.s_rcDrawEmpty;

        // 마진 적용 (이미지 경계를 벗어나지 않도록 보정)
        int lastX = bmpColor.Width - 1;
        int lastY = bmpColor.Height - 1;
        x1 = Math.Max(0, x1 - nMargin);
        y1 = Math.Max(0, y1 - nMargin);
        x2 = Math.Min(lastX, x2 + nMargin);
        y2 = Math.Min(lastY, y2 + nMargin);

        int rectWidth = x2 - x1 + 1;
        int rectHeight = y2 - y1 + 1;
        if (rectWidth <= 0) rectWidth = 1;
        if (rectHeight <= 0) rectHeight = 1;

        return new Draw.Rectangle(x1, y1, rectWidth, rectHeight);
    }

    // 비트맵의 특정 행에서 Bool 배열을 생성합니다 (Unsafe 코드 사용)
    // - true: 밝기 <= Brightness (전경), false: 밝기 > Brightness (배경)
    // bmpColor: 소스 비트맵, targetRow: 대상 행, Brightness: 임계값, xStartIndex: 시작 X, returns: Bool 배열
    public static bool[] GetBoolArray_FromColorBitmapRowFast(Draw.Bitmap bmpColor, int targetRow, byte Brightness, int xStartIndex = 0)
    {
        // 매개변수 검증
        if (bmpColor == null)
        {
            Debug.WriteLine("[ERROR] GetBoolArray_FromColorBitmapRowFast: bmpColor가 null입니다.");
            return null;
        }

        int width = bmpColor.Width;
        int height = bmpColor.Height;

        if (targetRow < 0 || targetRow >= height)
        {
            Debug.WriteLine($"[ERROR] GetBoolArray_FromColorBitmapRowFast: targetRow가 범위를 벗어났습니다. (targetRow={targetRow}, height={height})");
            return null;
        }

        if (xStartIndex < 0 || xStartIndex > width)
        {
            Debug.WriteLine($"[ERROR] GetBoolArray_FromColorBitmapRowFast: xStartIndex가 범위를 벗어났습니다. (xStartIndex={xStartIndex}, width={width})");
            return null;
        }

        bool[] boolArray = new bool[width];

        // 전체 영역에 대해 LockBits를 통해 메모리 접근
        Draw.Rectangle rect = new Draw.Rectangle(0, 0, width, height);
        System.Drawing.Imaging.BitmapData bmpData = bmpColor.LockBits(rect,
            System.Drawing.Imaging.ImageLockMode.ReadOnly, bmpColor.PixelFormat);

        try
        {
            int bytesPerPixel = Draw.Image.GetPixelFormatSize(bmpColor.PixelFormat) / 8;
            int stride = bmpData.Stride;
            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;
                byte* row = ptr + (targetRow * stride);

                // xStartIndex 이전은 true로 채움 (검은색으로 간주)
                for (int x = 0; x < xStartIndex; x++)
                    boolArray[x] = true;

                // xStartIndex부터 실제 밝기 검사
                for (int x = xStartIndex; x < width; x++)
                {
                    byte* pixel = row + (x * bytesPerPixel);
                    // Format32bppArgb의 경우: [0]=B, [1]=G, [2]=R, [3]=A
                    int b = pixel[0];
                    int g = pixel[1];
                    int r = pixel[2];

                    // 그레이스케일 밝기 계산
                    int grayScaleValue = (int)((r * 0.3) + (g * 0.59) + (b * 0.11));
                    boolArray[x] = grayScaleValue <= (int)Brightness;
                }
            }

            //Debug.WriteLine($"[OfrService] GetBoolArray_FromColorBitmapRowFast: targetRow={targetRow}, Brightness={Brightness}, xStartIndex={xStartIndex}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] GetBoolArray_FromColorBitmapRowFast: {StdUtil.GetExceptionMessage(ex)}");
            return null;
        }
        finally
        {
            bmpColor.UnlockBits(bmpData);
        }

        return boolArray;
    }
    #endregion

    #region Get - OfrModel
    //[Obsolete("체크 해야함 그전에는 GetOfrModel_TextAnalysis_InExactBitmapFast를 사용하시오")]
    //public static OfrModel_BmpTextAnalysis GetOfrModel_TextAnalysis_InLooseBitmapFast(Draw.Bitmap bmpLoose, byte threshold)
    //{
    //    OfrModel_BmpTextAnalysis result = new OfrModel_BmpTextAnalysis(bmpLoose, threshold);

    //    try
    //    {
    //        //Bitmap의 너비와 높이 가져오기
    //        int width = bmpLoose.Width;
    //        int height = bmpLoose.Height;
    //        int trueCount = 0;

    //        BitArray bitArray = new BitArray(width * height);

    //        //Lock the bitmap's bits
    //        Draw.Rectangle rect = new Draw.Rectangle(0, 0, width, height);
    //        DrawImg.BitmapData bmpData = bmpLoose.LockBits(rect, DrawImg.ImageLockMode.ReadOnly, DrawImg.PixelFormat.Format24bppRgb);

    //        //Get the address of the first line
    //        IntPtr ptr = bmpData.Scan0;

    //        //Declare an array to hold the bytes of the bitmap
    //        int bytes = Math.Abs(bmpData.Stride) * height;
    //        byte[] rgbValues = new byte[bytes];

    //        //Copy the RGB values into the array
    //        System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

    //        int minX = width, minY = height, maxX = 0, maxY = 0;

    //        for (int y = 0; y < height; y++)
    //        {
    //            for (int x = 0; x < width; x++)
    //            {
    //                int i = (y * bmpData.Stride) + x * 3;
    //                byte b = rgbValues[i];
    //                byte g = rgbValues[i + 1];
    //                byte r = rgbValues[i + 2];

    //                bool isForeground = (0.3 * r + 0.59 * g + 0.11 * b) < threshold;
    //                bitArray[y * width + x] = isForeground;

    //                if (isForeground)
    //                {
    //                    trueCount++;

    //                    if (x < minX) minX = x;
    //                    if (y < minY) minY = y;
    //                    if (x > maxX) maxX = x;
    //                    if (y > maxY) maxY = y;
    //                }
    //            }
    //        }

    //        //Unlock the bits
    //        bmpLoose.UnlockBits(bmpData);

    //        Byte[] byteArray = new Byte[(bitArray.Length + 7) / 8];
    //        bitArray.CopyTo(byteArray, 0);

    //        // rcForeground 설정
    //        if (trueCount > 0)
    //        {
    //            result.rcText = Draw.Rectangle.FromLTRB(minX, minY, maxX + 1, maxY + 1);

    //            result.nWidth = result.rcText.Width;
    //            result.nHeight = result.rcText.Height;
    //            result.trueRate = (double)trueCount / bitArray.Count;
    //            result.byteArray = byteArray;
    //            result.bitArray = bitArray;
    //            //BitArray를 HexString으로 변환
    //            result.sHexArray = ConvertByteArray_ToHexString(byteArray); // 가정: 해당 메소드는 Byte 배열을 HexString으로 변환

    //            return result;
    //        }
    //        else
    //        {
    //            return null;
    //        }
    //    }
    //    catch
    //    {
    //        return null;
    //    }
    //}
    //public static OfrModel_BmpTextAnalysis GetOfrModel_TextAnalysis_InExactBitmapFast(Draw.Bitmap bmpExact, byte threshold)
    //{
    //    OfrModel_BmpTextAnalysis result = new OfrModel_BmpTextAnalysis(bmpExact, threshold);

    //    try
    //    {
    //        //Bitmap의 너비와 높이 가져오기
    //        int width = bmpExact.Width;
    //        int height = bmpExact.Height;
    //        int trueCount = 0;

    //        BitArray bitArray = new BitArray(width * height);

    //        //Lock the bitmap's bits
    //        Draw.Rectangle rect = new Draw.Rectangle(0, 0, width, height);
    //        DrawImg.BitmapData bmpData = bmpExact.LockBits(rect, DrawImg.ImageLockMode.ReadOnly, DrawImg.PixelFormat.Format24bppRgb);

    //        //Get the address of the first line
    //        IntPtr ptr = bmpData.Scan0;

    //        //Declare an array to hold the bytes of the bitmap
    //        int bytes = Math.Abs(bmpData.Stride) * height;
    //        byte[] rgbValues = new byte[bytes];

    //        //Copy the RGB values into the array
    //        System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

    //        for (int y = 0; y < height; y++)
    //        {
    //            for (int x = 0; x < width; x++)
    //            {
    //                int i = (y * bmpData.Stride) + x * 3;
    //                byte b = rgbValues[i];
    //                byte g = rgbValues[i + 1];
    //                byte r = rgbValues[i + 2];

    //                //픽셀을 바이너리 값으로 변환(0 또는 1)
    //                bool isForeground = (0.3 * r + 0.59 * g + 0.11 * b) < threshold;
    //                bitArray[y * width + x] = isForeground;

    //                //true 값 카운트 증가
    //                if (isForeground) trueCount++;
    //            }
    //        }

    //        //Unlock the bits
    //        bmpExact.UnlockBits(bmpData);

    //        //BitArray를 Byte 배열로 변환
    //        Byte[] byteArray = new Byte[(bitArray.Length + 7) / 8];
    //        bitArray.CopyTo(byteArray, 0);

    //        //결과 정보 설정
    //        result.nWidth = width;
    //        result.nHeight = height;
    //        result.trueRate = (double)trueCount / bitArray.Count;
    //        result.byteArray = byteArray;
    //        result.bitArray = bitArray;
    //        //BitArray를 HexString으로 변환
    //        result.sHexArray = ConvertByteArray_ToHexString(byteArray);

    //        return result;
    //    }
    //    catch
    //    {
    //        return null;
    //    }
    //}

    //public static OfrModel_BmpTextAnalysis GetOfrModel_TextAnalysis_RectInBitmapFast(Draw.Bitmap bmpCapture, Draw.Rectangle rcForeground, byte threshold)
    //{
    //    if (bmpCapture == null) return null;
    //    if (rcForeground == StdUtil.s_rcDrawEmpty) return null;

    //    try
    //    {
    //        //지정된 영역의 너비와 높이 가져오기
    //        int width = rcForeground.Width;
    //        int height = rcForeground.Height;
    //        int trueCount = 0;

    //        //BitArray 생성
    //        BitArray bitArray = new BitArray(width * height);

    //        //Lock the bitmap's bits of the specified rectangle
    //        DrawImg.BitmapData bmpData = bmpCapture.LockBits(rcForeground, DrawImg.ImageLockMode.ReadOnly, DrawImg.PixelFormat.Format24bppRgb);

    //        //Get the address of the first line of the locked area
    //        IntPtr ptr = bmpData.Scan0;

    //        //Declare an array to hold the bytes of the bitmap of the specified area
    //        int bytes = Math.Abs(bmpData.Stride) * height;
    //        byte[] rgbValues = new byte[bytes];

    //        //Copy the RGB values of the specified rectangle into the array
    //        System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

    //        for (int y = 0; y < height; y++) // 아래와 교체해야 하는지 체크요함
    //        {
    //            for (int x = 0; x < width; x++)
    //            {
    //                int i = (y * bmpData.Stride) + x * 3;
    //                byte b = rgbValues[i];
    //                byte g = rgbValues[i + 1];
    //                byte r = rgbValues[i + 2];

    //                //픽셀을 바이너리 값으로 변환(0 또는 1) -임계값 로직은 필요에 따라 적용
    //                bool isForeground = (0.3 * r + 0.59 * g + 0.11 * b) < threshold;
    //                bitArray[y * width + x] = isForeground;

    //                //true 값 카운트 증가
    //                if (isForeground) trueCount++;
    //            }
    //        }
    //        //int minX = width, minY = height, maxX = 0, maxY = 0;

    //        //for (int y = 0; y < height; y++)
    //        //{
    //        //    for (int x = 0; x < width; x++)
    //        //    {
    //        //        int i = (y * bmpData.Stride) + x * 3;
    //        //        byte b = rgbValues[i];
    //        //        byte g = rgbValues[i + 1];
    //        //        byte r = rgbValues[i + 2];

    //        //        bool isForeground = (0.3 * r + 0.59 * g + 0.11 * b) < threshold;
    //        //        bitArray[y * width + x] = isForeground;

    //        //        if (isForeground)
    //        //        {
    //        //            trueCount++;

    //        //            if (x < minX) minX = x;
    //        //            if (y < minY) minY = y;
    //        //            if (x > maxX) maxX = x;
    //        //            if (y > maxY) maxY = y;
    //        //        }
    //        //    }
    //        //}

    //        //Unlock the bits
    //        bmpCapture.UnlockBits(bmpData);

    //        //BitArray를 Byte 배열로 변환
    //        Byte[] byteArray = new Byte[(bitArray.Length + 7) / 8];
    //        bitArray.CopyTo(byteArray, 0);

    //        OfrModel_BmpTextAnalysis result = new OfrModel_BmpTextAnalysis(bmpCapture, threshold);

    //        //결과 정보 설정
    //        result.nWidth = width;
    //        result.nHeight = height;
    //        result.trueRate = (double)trueCount / bitArray.Count;
    //        result.byteArray = byteArray;
    //        result.bitArray = bitArray;
    //        //BitArray를 HexString으로 변환
    //        result.sHexArray = ConvertByteArray_ToHexString(byteArray); // ByteArrayToHexString 메소드는 Byte 배열을 HexString으로 변환

    //        return result;
    //        //return null; // 이함수를 사용할때 체크를 위해
    //    }
    //    catch
    //    {
    //        return null;
    //    }
    //}
    // 비트맵 분석 - 전체 비트맵에서 BitArray 생성 (단일 문자용)
    // bmpOrg: 원본 비트맵, byteAvgBrightness: 평균 밝기(threshold), returns: 비트맵 분석 결과
    public static OfrModel_BitmapAnalysis GetBitmapAnalysisFast(Draw.Bitmap bmpOrg, byte byteAvgBrightness)
    {
        if (bmpOrg == null) return null;

        int width = bmpOrg.Width;
        int height = bmpOrg.Height;
        int trueCount = 0;

        BitArray bitArray = new BitArray(width * height);

        DrawImg.BitmapData bmpData = bmpOrg.LockBits(
            new Draw.Rectangle(0, 0, width, height),
            DrawImg.ImageLockMode.ReadOnly,
            DrawImg.PixelFormat.Format24bppRgb);

        try
        {
            IntPtr ptr = bmpData.Scan0;
            int bytes = Math.Abs(bmpData.Stride) * height;
            byte[] rgbValues = new byte[bytes];
            System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int i = (y * bmpData.Stride) + x * 3;
                    byte b = rgbValues[i];
                    byte g = rgbValues[i + 1];
                    byte r = rgbValues[i + 2];

                    int gray = (r * 30 + g * 59 + b * 11) / 100;
                    bool isForeground = gray < byteAvgBrightness;

                    bitArray[y * width + x] = isForeground;
                    if (isForeground) trueCount++;
                }
            }
        }
        finally
        {
            bmpOrg.UnlockBits(bmpData);
        }

        byte[] byteArray = new byte[(bitArray.Length + 7) / 8];
        bitArray.CopyTo(byteArray, 0);

        OfrModel_BitmapAnalysis result = new OfrModel_BitmapAnalysis(bmpOrg, byteAvgBrightness)
        {
            nWidth = width,
            nHeight = height,
            trueRate = (double)trueCount / bitArray.Count,
            byteArray = byteArray,
            bitArray = bitArray,
            sHexArray = ConvertByteArray_ToHexString(byteArray),
            threshold = byteAvgBrightness
        };

        return result;
    }

    // 비트맵 분석 - 특정 영역에서 BitArray 생성 (단일 문자용)
    // bmpCapture: 비트맵, rcForeground: 전경 영역, byteAvgBrightness: 평균 밝기(threshold), returns: 비트맵 분석 결과
    public static OfrModel_BitmapAnalysis GetBitmapAnalysisFast(Draw.Bitmap bmpCapture, Draw.Rectangle rcForeground, byte byteAvgBrightness)
    {
        if (bmpCapture == null) return null;
        if (rcForeground == StdUtil.s_rcDrawEmpty) return null;

        try
        {
            Draw.Bitmap bmpExact = GetBitmapInBitmapFast(bmpCapture, rcForeground);
            if (bmpExact == null) return null;

            // 잘라낸 비트맵 전체 분석
            return GetBitmapAnalysisFast(bmpExact, byteAvgBrightness);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] GetBitmapAnalysisFast: {StdUtil.GetExceptionMessage(ex)}");
            return null;
        }
    }
    #endregion

    #region Compare
    //public static bool IsSameBitmap_ByFastType(Draw.Bitmap bitmap1, Draw.Bitmap bitmap2)
    //{
    //    // 크기 비교
    //    if (bitmap1.Width != bitmap2.Width || bitmap1.Height != bitmap2.Height)
    //    {
    //        return false;
    //    }

    //    // LockBits로 비트맵 데이터 접근
    //    DrawImg.BitmapData data1 = bitmap1.LockBits(
    //        new Draw.Rectangle(0, 0, bitmap1.Width, bitmap1.Height), DrawImg.ImageLockMode.ReadOnly, DrawImg.PixelFormat.Format32bppArgb);
    //    DrawImg.BitmapData data2 = bitmap2.LockBits(
    //        new Draw.Rectangle(0, 0, bitmap2.Width, bitmap2.Height), DrawImg.ImageLockMode.ReadOnly, DrawImg.PixelFormat.Format32bppArgb);

    //    int bytesPerPixel = 4; // ARGB 포맷의 경우, 픽셀 당 4바이트
    //    bool areIdentical = true;

    //    unsafe
    //    {
    //        for (int y = 0; y < bitmap1.Height; y++)
    //        {
    //            byte* row1 = (byte*)data1.Scan0 + (y * data1.Stride);
    //            byte* row2 = (byte*)data2.Scan0 + (y * data2.Stride);

    //            for (int x = 0; x < bitmap1.Width * bytesPerPixel; x += bytesPerPixel)
    //            {
    //                // 각 픽셀의 ARGB 값을 비교
    //                if (*(int*)(row1 + x) != *(int*)(row2 + x))
    //                {
    //                    areIdentical = false;
    //                    break;
    //                }
    //            }

    //            if (!areIdentical)
    //                break;
    //        }
    //    }

    //    // Lock 해제
    //    bitmap1.UnlockBits(data1);
    //    bitmap2.UnlockBits(data2);

    //    return areIdentical;
    //}

    //public static bool FindSameBitmap_ByFastType(List<Draw.Bitmap> listBitmap, Draw.Bitmap bitmap2)
    //{
    //    foreach (Draw.Bitmap bitmap1 in listBitmap)
    //    {
    //        if (IsSameBitmap_ByFastType(bitmap1, bitmap2))
    //        {
    //            return true;
    //        }
    //    }

    //    return false;
    //}

    //public static bool? IsAnyTrueInBitmapColumn_ByFastType(Draw.Bitmap bmpOrg, byte byteBrightness, int curCol, int yStartIndex, int yEndIndex)
    //{
    //    // Bitmap의 크기와 범위 확인
    //    if (curCol < 0 || curCol >= bmpOrg.Width || yStartIndex < 0 || yEndIndex >= bmpOrg.Height)
    //        return null;

    //    // Bitmap을 Lock
    //    Draw.Rectangle rect = new Draw.Rectangle(curCol, yStartIndex, 1, yEndIndex - yStartIndex + 1);
    //    DrawImg.BitmapData bmpData = bmpOrg.LockBits(rect, DrawImg.ImageLockMode.ReadOnly, bmpOrg.PixelFormat);

    //    int bytesPerPixel = Draw.Image.GetPixelFormatSize(bmpOrg.PixelFormat) / 8;
    //    int height = yEndIndex - yStartIndex + 1;
    //    byte[] pixelValues = new byte[bytesPerPixel * height];

    //    // 픽셀 데이터 복사
    //    IntPtr ptrFirstPixel = bmpData.Scan0;
    //    for (int y = 0; y < height; y++)
    //    {
    //        IntPtr ptrPixel = ptrFirstPixel + y * bmpData.Stride;
    //        System.Runtime.InteropServices.Marshal.Copy(ptrPixel, pixelValues, y * bytesPerPixel, bytesPerPixel);

    //        // 픽셀 색상 추출
    //        Draw.Color color;
    //        switch (bytesPerPixel)
    //        {
    //            case 4:
    //                color = Draw.Color.FromArgb(Marshal.ReadInt32(ptrPixel));
    //                break;

    //            case 3:
    //                color = Draw.Color.FromArgb(255, pixelValues[y * bytesPerPixel + 2], pixelValues[y * bytesPerPixel + 1], pixelValues[y * bytesPerPixel]);
    //                break;

    //            default:
    //                color = Draw.Color.FromArgb(255, pixelValues[y], pixelValues[y], pixelValues[y]);
    //                break;
    //        }

    //        // 밝기를 기반으로 boolean 값 계산
    //        byte grayScaleValue = (byte)(int)((color.R * 0.3) + (color.G * 0.59) + (color.B * 0.11));
    //        if (grayScaleValue < byteBrightness)
    //        {
    //            bmpOrg.UnlockBits(bmpData);
    //            //Debug.WriteLine($"True: column={curCol}, row={y + yStartIndex}, Aver={byteBrightness}, Find={grayScaleValue}");
    //            return true;
    //        }
    //    }

    //    // Unlock the bits
    //    bmpOrg.UnlockBits(bmpData);
    //    return false;
    //}

    //public static bool IsAnyTrueInBitmapRow_ByFastType(Draw.Bitmap bmpOrg, byte byteBrightness, int curRow, int xStartIndex, int xEndIndex)
    //{
    //    // Bitmap의 크기와 범위 확인
    //    if (curRow < 0 || curRow >= bmpOrg.Height || xStartIndex < 0 || xEndIndex >= bmpOrg.Width)
    //        return false;

    //    // Bitmap을 Lock
    //    Draw.Rectangle rect = new Draw.Rectangle(xStartIndex, curRow, xEndIndex - xStartIndex + 1, 1);
    //    DrawImg.BitmapData bmpData = bmpOrg.LockBits(rect, DrawImg.ImageLockMode.ReadOnly, bmpOrg.PixelFormat);

    //    int bytesPerPixel = Draw.Image.GetPixelFormatSize(bmpOrg.PixelFormat) / 8;
    //    int width = xEndIndex - xStartIndex + 1;
    //    byte[] pixelValues = new byte[bytesPerPixel * width];

    //    // 픽셀 데이터 복사
    //    IntPtr ptrFirstPixel = bmpData.Scan0;
    //    for (int x = 0; x < width; x++)
    //    {
    //        IntPtr ptrPixel = ptrFirstPixel + x * bytesPerPixel;
    //        System.Runtime.InteropServices.Marshal.Copy(ptrPixel, pixelValues, x * bytesPerPixel, bytesPerPixel);

    //        // 픽셀 색상 추출
    //        Draw.Color color;
    //        switch (bytesPerPixel)
    //        {
    //            case 4:
    //                color = Draw.Color.FromArgb(Marshal.ReadInt32(ptrPixel));
    //                break;

    //            case 3:
    //                color = Draw.Color.FromArgb(255, pixelValues[x * bytesPerPixel + 2], pixelValues[x * bytesPerPixel + 1], pixelValues[x * bytesPerPixel]);
    //                break;

    //            default:
    //                color = Draw.Color.FromArgb(255, pixelValues[x], pixelValues[x], pixelValues[x]);
    //                break;
    //        }

    //        // 밝기를 기반으로 boolean 값 계산
    //        if (ConvertColorPixelToBool(color, byteBrightness))
    //        {
    //            bmpOrg.UnlockBits(bmpData);
    //            return true;
    //        }
    //    }

    //    // Unlock the bits
    //    bmpOrg.UnlockBits(bmpData);
    //    return false;
    //}

    // 비트맵의 특정 컬럼(세로줄)에 전경 픽셀이 있는지 검사합니다 (unsafe 코드 사용)
    // bmpOrg: 원본 비트맵, byteBrightness: 임계값, curCol: X, yStart: 시작 Y, yEnd: 끝 Y, returns: 발견 여부(bool?)
    public static bool? IsAnyTrueInBitmapColumn_ByFastType(Draw.Bitmap bmpOrg, byte byteBrightness, int curCol, int yStartIndex, int yEndIndex)
    {
        // 매개변수 검증
        if (bmpOrg == null || curCol < 0 || curCol >= bmpOrg.Width ||
            yStartIndex < 0 || yEndIndex >= bmpOrg.Height || yStartIndex > yEndIndex)
            return null;

        int width = bmpOrg.Width;
        int height = bmpOrg.Height;

        DrawImg.BitmapData bmpData = bmpOrg.LockBits(
            new Draw.Rectangle(0, 0, width, height),
            DrawImg.ImageLockMode.ReadOnly,
            bmpOrg.PixelFormat);

        try
        {
            int bytesPerPixel = Draw.Image.GetPixelFormatSize(bmpOrg.PixelFormat) / 8;
            int stride = bmpData.Stride;

            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;

                for (int y = yStartIndex; y <= yEndIndex; y++)
                {
                    byte* row = ptr + (y * stride);
                    byte* pixel = row + (curCol * bytesPerPixel);

                    // Format32bppArgb: [0]=B, [1]=G, [2]=R, [3]=A
                    int b = pixel[0];
                    int g = pixel[1];
                    int r = pixel[2];

                    // 그레이스케일 밝기 계산
                    int grayScaleValue = (int)((r * 0.3) + (g * 0.59) + (b * 0.11));

                    if (grayScaleValue < byteBrightness)
                        return true; // 전경 픽셀 발견
                }
            }
        }
        finally
        {
            bmpOrg.UnlockBits(bmpData);
        }

        return false; // 전경 픽셀 미발견
    }

    // 비트맵의 특정 행(가로줄)에 전경 픽셀이 있는지 검사합니다 (unsafe 코드 사용)
    // bmpOrg: 원본 비트맵, byteBrightness: 임계값, curRow: Y, xStart: 시작 X, xEnd: 끝 X, returns: 발견 여부(bool)
    public static bool IsAnyTrueInBitmapRow_ByFastType(Draw.Bitmap bmpOrg, byte byteBrightness, int curRow, int xStartIndex, int xEndIndex)
    {
        // 매개변수 검증
        if (bmpOrg == null || curRow < 0 || curRow >= bmpOrg.Height ||
            xStartIndex < 0 || xEndIndex >= bmpOrg.Width || xStartIndex > xEndIndex)
            return false;

        int width = bmpOrg.Width;
        int height = bmpOrg.Height;

        DrawImg.BitmapData bmpData = bmpOrg.LockBits(
            new Draw.Rectangle(0, 0, width, height),
            DrawImg.ImageLockMode.ReadOnly,
            bmpOrg.PixelFormat);

        try
        {
            int bytesPerPixel = Draw.Image.GetPixelFormatSize(bmpOrg.PixelFormat) / 8;
            int stride = bmpData.Stride;

            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;
                byte* row = ptr + (curRow * stride);

                for (int x = xStartIndex; x <= xEndIndex; x++)
                {
                    byte* pixel = row + (x * bytesPerPixel);

                    // Format32bppArgb: [0]=B, [1]=G, [2]=R, [3]=A
                    int b = pixel[0];
                    int g = pixel[1];
                    int r = pixel[2];

                    // 그레이스케일 밝기 계산
                    int grayScaleValue = (int)((r * 0.3) + (g * 0.59) + (b * 0.11));

                    if (grayScaleValue < byteBrightness)
                        return true; // 전경 픽셀 발견
                }
            }
        }
        finally
        {
            bmpOrg.UnlockBits(bmpData);
        }

        return false; // 전경 픽셀 미발견
    }
    #endregion

    #region Convert
    //// 비트맵/비트맵이미지
    //public static BitmapImage ConvertBitmap_ToBitmapImage(Draw.Bitmap bitmap)
    //{
    //    if (bitmap == null) return null;

    //    using (MemoryStream memory = new MemoryStream())
    //    {
    //        // Save the Bitmap to the MemoryStream as PNG or other format
    //        bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
    //        memory.Position = 0;

    //        // Create a new BitmapImage and load the stream
    //        BitmapImage bitmapImage = new BitmapImage();
    //        bitmapImage.BeginInit();
    //        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
    //        bitmapImage.StreamSource = memory;
    //        bitmapImage.EndInit();

    //        return bitmapImage;
    //    }
    //}

    //// Pixcel
    //public static bool ConvertColorPixelToBool(Draw.Color color, byte threshold = 128)
    //{
    //    //그레이스케일 밝기 계산
    //    int grayScaleValue = (int)((color.R * 0.3) + (color.G * 0.59) + (color.B * 0.11));
    //    return (byte)grayScaleValue < threshold;
    //}

    // Byte 배열을 HexString으로 변환합니다 (0x 접두사 포함)
    // byteArrValue: Byte 배열, returns: HexString (예: "0x1A2B3C")
    public static string ConvertByteArray_ToHexString(byte[] byteArrValue)
    {
        string str = "0x" + BitConverter.ToString(byteArrValue).Replace("-", "");

        return str.Trim();
    }
    //public static byte[] ConvertHexString_ToByteArray(string hexString)
    //{
    //    //Remove "0x" prefix if present
    //    if (hexString.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
    //    {
    //        hexString = hexString.Substring(2);
    //    }

    //    //Validate hex string length
    //    if (hexString.Length % 2 != 0)
    //    {
    //        throw new ArgumentException("Hex string must have an even length.");
    //    }

    //    //Convert to byte array
    //    byte[] byteArray = new byte[hexString.Length / 2];
    //    for (int i = 0; i < hexString.Length; i += 2)
    //    {
    //        byteArray[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
    //    }
    //    return byteArray;
    //}

    // double 스케일로 비트맵 크기 조정 (고품질 Bicubic 보간법 사용)
    // originalBitmap: 원본, scale: 비율, returns: 조정된 비트맵
    public static Draw.Bitmap ConvertSizeBitmap(Draw.Bitmap originalBitmap, double scale)
    {
        // 파라미터 검증
        if (originalBitmap == null)
        {
            Debug.WriteLine("[ERROR] ConvertSizeBitmap: originalBitmap이 null입니다.");
            return null;
        }

        if (scale <= 0)
        {
            Debug.WriteLine($"[ERROR] ConvertSizeBitmap: scale은 0보다 커야 합니다. (입력값: {scale})");
            return null;
        }

        try
        {
            // 스케일을 적용한 새로운 비트맵 크기 계산
            int newWidth = (int)(originalBitmap.Width * scale);
            int newHeight = (int)(originalBitmap.Height * scale);

            // 계산된 크기 검증
            if (newWidth <= 0 || newHeight <= 0)
            {
                Debug.WriteLine($"[ERROR] ConvertSizeBitmap: 계산된 크기가 유효하지 않습니다. (Width: {newWidth}, Height: {newHeight})");
                return null;
            }

            // 새로운 크기의 비트맵 생성
            Draw.Bitmap resizedBitmap = new Draw.Bitmap(newWidth, newHeight);

            // Graphics를 사용해 원본 비트맵을 새로운 비트맵 크기에 맞게 그리기
            using (Draw.Graphics g = Draw.Graphics.FromImage(resizedBitmap))
            {
                // 고품질 렌더링 설정
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

                g.DrawImage(originalBitmap, 0, 0, newWidth, newHeight);
            }

            return resizedBitmap;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] ConvertSizeBitmap: 비트맵 크기 조정 중 예외 발생 - {StdUtil.GetExceptionMessage(ex)}");
            return null;
        }
    }
    #endregion

    #region Bitmap / BitmapImage
    // 비트맵에서 특정 영역을 잘라내어 새 비트맵을 생성합니다 (unsafe 코드 사용)
    // bmpOrg: 원본 비트맵, rcTarget: 잘라낼 영역, returns: 잘라낸 비트맵
    // TODO_IMPROVE: 성능 개선 고려 - 픽셀 단위 복사 대신 Buffer.MemoryCopy로 행 단위 복사 (2-3배 빠름)
    public static Draw.Bitmap GetBitmapInBitmapFast(Draw.Bitmap bmpOrg, Draw.Rectangle rcTarget)
    {
        if (bmpOrg == null) return null; // 명백한 버그 수정

        int widthOrg = bmpOrg.Width;
        int heightOrg = bmpOrg.Height;
        int widthTarget = rcTarget.Width;
        int heightTarget = rcTarget.Height;

        if (widthTarget == 0 || heightTarget == 0)
            return null;

        // 원본과 동일한 PixelFormat으로 새 Bitmap 생성
        Draw.Bitmap bmpTarget = new Draw.Bitmap(widthTarget, heightTarget, bmpOrg.PixelFormat);

        // 원본과 대상의 전체 영역 설정
        Draw.Rectangle rectSrc = new Draw.Rectangle(0, 0, widthOrg, heightOrg);
        Draw.Rectangle rectTarget = new Draw.Rectangle(0, 0, widthTarget, heightTarget);

        System.Drawing.Imaging.BitmapData bmpDataSrc = bmpOrg.LockBits(rectSrc,
            System.Drawing.Imaging.ImageLockMode.ReadOnly, bmpOrg.PixelFormat);
        System.Drawing.Imaging.BitmapData bmpDataTarget = bmpTarget.LockBits(rectTarget,
            System.Drawing.Imaging.ImageLockMode.WriteOnly, bmpTarget.PixelFormat);

        try
        {
            int bytesPerPixel = Draw.Image.GetPixelFormatSize(bmpOrg.PixelFormat) / 8;
            int strideSrc = bmpDataSrc.Stride;
            int strideTarget = bmpDataTarget.Stride;
            unsafe
            {
                byte* srcPtr = (byte*)bmpDataSrc.Scan0;
                byte* targetPtr = (byte*)bmpDataTarget.Scan0;
                for (int y = 0; y < heightTarget; y++)
                {
                    byte* targetRow = targetPtr + y * strideTarget;
                    for (int x = 0; x < widthTarget; x++)
                    {
                        // 원본에서의 좌표 계산
                        int xOrg = rcTarget.X + x;
                        int yOrg = rcTarget.Y + y;

                        // 원본 범위를 벗어나면 건너뜀
                        if (xOrg < 0 || xOrg >= widthOrg || yOrg < 0 || yOrg >= heightOrg)
                            continue;

                        byte* srcPixel = srcPtr + yOrg * strideSrc + xOrg * bytesPerPixel;
                        byte* targetPixel = targetRow + x * bytesPerPixel;

                        // 픽셀 데이터 복사 (각 바이트 단위)
                        for (int i = 0; i < bytesPerPixel; i++)
                            targetPixel[i] = srcPixel[i];
                    }
                }
            }
        }
        finally
        {
            bmpOrg.UnlockBits(bmpDataSrc);
            bmpTarget.UnlockBits(bmpDataTarget);
        }

        return bmpTarget;
    }

    // 비트맵의 RGB 값을 반전시킵니다 (네거티브 효과)
    // source: 원본 비트맵, returns: RGB 반전된 새 비트맵
    public static Draw.Bitmap InvertBitmap(Draw.Bitmap source)
    {
        int width = source.Width;
        int height = source.Height;

        // PixelFormat을 명시적으로 지정하여 생성
        Draw.Bitmap result = new Draw.Bitmap(width, height, DrawImg.PixelFormat.Format24bppRgb);

        DrawImg.BitmapData srcData = source.LockBits(new Draw.Rectangle(0, 0, width, height), DrawImg.ImageLockMode.ReadOnly, DrawImg.PixelFormat.Format24bppRgb);

        DrawImg.BitmapData dstData = result.LockBits(new Draw.Rectangle(0, 0, width, height), DrawImg.ImageLockMode.WriteOnly, DrawImg.PixelFormat.Format24bppRgb);

        try
        {
            int bytes = Math.Abs(srcData.Stride) * height;
            byte[] srcBytes = new byte[bytes];
            byte[] dstBytes = new byte[bytes];

            System.Runtime.InteropServices.Marshal.Copy(srcData.Scan0, srcBytes, 0, bytes);

            // RGB 반전: 255 - 원래값
            for (int i = 0; i < bytes; i++)
            {
                dstBytes[i] = (byte)(255 - srcBytes[i]);
            }

            System.Runtime.InteropServices.Marshal.Copy(dstBytes, 0, dstData.Scan0, bytes);
        }
        finally
        {
            source.UnlockBits(srcData);
            result.UnlockBits(dstData);
        }

        return result;
    }

    // 화면의 특정 영역을 캡처합니다
    // x, y, width, height, outputPath: 저장경로(선택), returns: 캡처된 비트맵
    public static Draw.Bitmap CaptureScreen_InArea(int x, int y, int width, int height, string outputPath = "")
    {
        if (width <= 0 || height <= 0)
        {
            Debug.WriteLine($"[ERROR] CaptureScreen_InArea: 유효하지 않은 크기 (width={width}, height={height})");
            return null;
        }

        try
        {
            Draw.Bitmap bmp = new Draw.Bitmap(width, height);

            using (Draw.Graphics g = Draw.Graphics.FromImage(bmp))
            {
                // 지정된 영역(x, y, width, height)을 캡처
                g.CopyFromScreen(new Draw.Point(x, y), Draw.Point.Empty, new Draw.Size(width, height));
            }

            // 이미지 파일로 저장
            if (!string.IsNullOrEmpty(outputPath))
            {
                bmp.Save(outputPath, DrawImg.ImageFormat.Png);
                Debug.WriteLine($"[OfrService] 화면 캡처 저장: {outputPath}");
            }

            return bmp;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] CaptureScreen_InArea: {StdUtil.GetExceptionMessage(ex)}");
            return null;
        }
    }

    /// <summary>
    /// Rectangle 영역을 캡처합니다
    /// </summary>
    public static Draw.Bitmap CaptureScreen_InRectangle(Draw.Rectangle rc, string outputPath = "")
    {
        return CaptureScreen_InArea(rc.X, rc.Y, rc.Width, rc.Height, outputPath);
    }

    /// <summary>
    /// 윈도우 핸들 기준 상대 좌표로 화면을 캡처합니다
    /// </summary>
    /// <param name="hWnd">윈도우 핸들</param>
    /// <param name="rcRel">상대 좌표 Rectangle</param>
    /// <param name="outputPath">저장 경로 (선택)</param>
    /// <returns>캡처된 비트맵</returns>
    public static Draw.Bitmap CaptureScreenRect_InWndHandle(IntPtr hWnd, Draw.Rectangle rcRel, string outputPath = "")
    {
        if (hWnd == IntPtr.Zero)
        {
            Debug.WriteLine("[ERROR] CaptureScreenRect_InWndHandle: 유효하지 않은 윈도우 핸들");
            return null;
        }

        if (!StdWin32.IsWindowVisible(hWnd))
        {
            Debug.WriteLine($"[ERROR] CaptureScreenRect_InWndHandle: 윈도우가 보이지 않음 (hWnd=0x{hWnd:X})");
            return null;
        }

        Draw.Rectangle rcWnd = Std32Window.GetWindowRect_DrawAbs(hWnd);
        Draw.Rectangle rcTarget = new Draw.Rectangle(
            rcWnd.X + rcRel.X,
            rcWnd.Y + rcRel.Y,
            rcRel.Width,
            rcRel.Height);

        //Debug.WriteLine($"[OfrService] 윈도우 상대 좌표 캡처: rcWnd={rcWnd}, rcRel={rcRel}, rcTarget={rcTarget}");

        return CaptureScreen_InRectangle(rcTarget, outputPath);
    }

    // 윈도우 핸들 기준 offset을 적용하여 화면을 캡처합니다
    // hWnd: 핸들, offset: 오프셋, outputPath: 저장경로, returns: 캡처된 비트맵
    public static Draw.Bitmap CaptureScreenRect_InWndHandle(IntPtr hWnd, int offset, string outputPath = "")
    {
        if (hWnd == IntPtr.Zero)
        {
            Debug.WriteLine("[ERROR] CaptureScreenRect_InWndHandle: 유효하지 않은 윈도우 핸들");
            return null;
        }

        if (!StdWin32.IsWindowVisible(hWnd))
        {
            Debug.WriteLine($"[ERROR] CaptureScreenRect_InWndHandle: 윈도우가 보이지 않음 (hWnd=0x{hWnd:X})");
            return null;
        }

        Draw.Rectangle rcWnd = Std32Window.GetWindowRect_DrawAbs(hWnd);
        Draw.Rectangle rcTarget = new Draw.Rectangle(
            rcWnd.X + offset,
            rcWnd.Y + offset,
            rcWnd.Width - offset - offset,
            rcWnd.Height - offset - offset);

        //Debug.WriteLine($"[OfrService] 윈도우 오프셋 캡처: rcWnd={rcWnd}, offset={offset}, rcTarget={rcTarget}");

        return CaptureScreen_InRectangle(rcTarget, outputPath);
    }

    /// <summary>
    /// 윈도우 핸들의 전체 영역을 캡처합니다
    /// </summary>
    /// <param name="hWnd">윈도우 핸들</param>
    /// <param name="outputPath">저장 경로 (선택)</param>
    /// <returns>캡처된 비트맵</returns>
    public static Draw.Bitmap CaptureScreenRect_InWndHandle(IntPtr hWnd, string outputPath = "")
    {
        if (hWnd == IntPtr.Zero)
        {
            Debug.WriteLine("[ERROR] CaptureScreenRect_InWndHandle: 유효하지 않은 윈도우 핸들");
            return null;
        }

        if (!StdWin32.IsWindowVisible(hWnd))
        {
            Debug.WriteLine($"[ERROR] CaptureScreenRect_InWndHandle: 윈도우가 보이지 않음 (hWnd=0x{hWnd:X})");
            return null;
        }

        Draw.Rectangle rcWnd = Std32Window.GetWindowRect_DrawAbs(hWnd);
        //Debug.WriteLine($"[OfrService] 윈도우 전체 캡처: rcWnd={rcWnd}");

        return CaptureScreen_InRectangle(rcWnd, outputPath);
    }

    //public static Draw.Bitmap CaptureScreenRect_FromBitmapImg(Draw.Rectangle rc, string outputPath = "")
    //{
    //    Draw.Bitmap screenshot = new Draw.Bitmap(rc.Width, rc.Height, DrawImg.PixelFormat.Format32bppArgb);
    //    Draw.Graphics graphics = Draw.Graphics.FromImage(screenshot);
    //    graphics.CopyFromScreen(rc.X, rc.Y, 0, 0, rc.Size, Draw.CopyPixelOperation.SourceCopy);

    //    //이미지 파일로 저장
    //    if (!string.IsNullOrEmpty(outputPath)) screenshot.Save(outputPath, DrawImg.ImageFormat.Png);

    //    return screenshot;
    //}

    //public static Draw.Color? GetPixelColorFrmWndHandle(IntPtr hWnd, Draw.Point ptChk)
    //{
    //    if (hWnd == IntPtr.Zero) return null;
    //    if (!StdWin32.IsWindowVisible(hWnd)) return null;

    //    Draw.Bitmap bmp = CaptureScreenRect_InWndHandle(hWnd, new Draw.Rectangle(ptChk.X, ptChk.Y, 1, 1));
    //    Draw.Color color = bmp.GetPixel(0, 0);

    //    return color;
    //}
    public static int GetPixelBrightnessFrmWndHandle(IntPtr hWnd, Draw.Point ptRel)
    {
        if (hWnd == IntPtr.Zero) return -1;
        if (!StdWin32.IsWindowVisible(hWnd)) return -1;

        Draw.Bitmap bmp = CaptureScreenRect_InWndHandle(hWnd, new Draw.Rectangle(ptRel.X, ptRel.Y, 1, 1));
        Draw.Color color = bmp.GetPixel(0, 0);

        byte r = color.R;
        byte g = color.G;
        byte b = color.B;

        //그레이스케일 밝기 계산
        return (int)((r * 0.3) + (g * 0.59) + (b * 0.11));
    }
    public static int GetPixelBrightnessFrmWndHandle(IntPtr hWnd, int x, int y)
    {
        if (hWnd == IntPtr.Zero) return -1;
        if (!StdWin32.IsWindowVisible(hWnd)) return -1;

        Draw.Bitmap bmp = CaptureScreenRect_InWndHandle(hWnd, new Draw.Rectangle(x, y, 1, 1));
        Draw.Color color = bmp.GetPixel(0, 0);

        byte r = color.R;
        byte g = color.G;
        byte b = color.B;

        //그레이스케일 밝기 계산
        return (int)((r * 0.3) + (g * 0.59) + (b * 0.11));
    }

    public static int GetCenterPixelBrightnessFrmWndHandle(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero) return -1;
        if (!StdWin32.IsWindowVisible(hWnd)) return -1;

        Draw.Rectangle rcWnd = Std32Window.GetWindowRect_DrawAbs(hWnd);
        int centerX = rcWnd.Width / 2;
        int centerY = rcWnd.Height / 2;

        return GetPixelBrightnessFrmWndHandle(hWnd, centerX, centerY);
    }
    #endregion

    #region Brightness Utils
    // 비트맵의 평균 밝기를 계산합니다 (unsafe 코드 사용)
    // bmpColor: 소스 비트맵, weight: 가중치(0.9), returns: 평균 밝기(1-254)
    public static byte GetAverageBrightness_FromColorBitmapFast(Draw.Bitmap bmpColor, double weight = 0.9)
    {
        if (bmpColor == null) return 0;  // Null 비트맵 처리

        int width = bmpColor.Width;
        int height = bmpColor.Height;

        // Bitmap의 PixelFormat을 직접 지정하지 않고, bmpColor의 PixelFormat 사용
        DrawImg.BitmapData bmpData = bmpColor.LockBits(new Draw.Rectangle(0, 0, width, height), DrawImg.ImageLockMode.ReadOnly, bmpColor.PixelFormat);

        IntPtr ptr = bmpData.Scan0;
        int bytes = Math.Abs(bmpData.Stride) * height;
        byte[] rgbValues = new byte[bytes];
        System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

        long sum = 0;
        int count = 0;

        // PixelFormat에 따라 바이트 당 증가량을 조절
        int bytesPerPixel = Draw.Image.GetPixelFormatSize(bmpColor.PixelFormat) / 8;

        // Calculate brightness
        for (int i = 0; i < bytes; i += bytesPerPixel)
        {
            if (i + 2 >= rgbValues.Length) break;  // 배열 범위 초과 방지

            byte b = rgbValues[i];
            byte g = rgbValues[i + 1];
            byte r = rgbValues[i + 2];

            // 그레이스케일 밝기 계산
            int grayScaleValue = (int)((r * 0.3) + (g * 0.59) + (b * 0.11));
            sum += grayScaleValue;
            count++;
        }

        bmpColor.UnlockBits(bmpData);

        // Calculate weighted average
        int averageBrightness = (int)(sum / count * weight);

        if (averageBrightness <= 0) return 1; // Extremely dark or black
        if (averageBrightness >= 255) return 254; // Extremely bright or white

        return (byte)averageBrightness;
    }
    public static byte GetAverageBrightness_FromColorBitmapRectFast(Draw.Bitmap bmpColor, Draw.Rectangle rcSpare, double weight = 0.9)
    {
        if (bmpColor == null) return 1;
        if (rcSpare.Width <= 0 || rcSpare.Height <= 0 || rcSpare.X < 0 || rcSpare.Y < 0 ||
            rcSpare.X + rcSpare.Width > bmpColor.Width || rcSpare.Y + rcSpare.Height > bmpColor.Height)
            return 1;

        System.Drawing.Imaging.BitmapData bmpData = bmpColor.LockBits(rcSpare,
            System.Drawing.Imaging.ImageLockMode.ReadOnly, bmpColor.PixelFormat);

        IntPtr ptr = bmpData.Scan0;
        int bytes = Math.Abs(bmpData.Stride) * bmpData.Height;
        byte[] rgbValues = new byte[bytes];
        System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

        long sum = 0;
        int count = 0;

        try
        {
            int bytesPerPixel = Draw.Image.GetPixelFormatSize(bmpColor.PixelFormat) / 8;

            for (int i = 0; i < bytes; i += bytesPerPixel)
            {
                if (i + 2 >= rgbValues.Length) break;

                byte b = rgbValues[i];
                byte g = rgbValues[i + 1];
                byte r = rgbValues[i + 2];

                // 그레이스케일 밝기 계산
                int grayScaleValue = (int)((r * 0.3) + (g * 0.59) + (b * 0.11));
                sum += grayScaleValue;
                count++;
            }

            // Calculate weighted average
            int averageBrightness = (int)(sum / count * weight);

            if (averageBrightness <= 0) return 1;
            if (averageBrightness >= 255) return 254;

            return (byte)averageBrightness;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"GetAverageBrightness_FromColorBitmapRectFast 실패: {ex.Message}");
            return 1;
        }
        finally
        {
            bmpColor.UnlockBits(bmpData);
        }
    }

    public static byte GetMaxBrightness_FromColorBitmapRectFast(Draw.Bitmap bmpColor, Draw.Rectangle rcSpare)
    {
        if (bmpColor == null) return 1;
        if (rcSpare.Width <= 0 || rcSpare.Height <= 0 || rcSpare.X < 0 || rcSpare.Y < 0 ||
            rcSpare.X + rcSpare.Width > bmpColor.Width || rcSpare.Y + rcSpare.Height > bmpColor.Height)
            return 1;

        System.Drawing.Imaging.BitmapData bmpData = bmpColor.LockBits(rcSpare,
            System.Drawing.Imaging.ImageLockMode.ReadOnly, bmpColor.PixelFormat);

        IntPtr ptr = bmpData.Scan0;
        int bytes = Math.Abs(bmpData.Stride) * bmpData.Height;
        byte[] rgbValues = new byte[bytes];
        System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);
        byte max = 1;

        try
        {
            int bytesPerPixel = Draw.Image.GetPixelFormatSize(bmpColor.PixelFormat) / 8;

            for (int i = 0; i < bytes; i += bytesPerPixel)
            {
                if (i + 2 >= rgbValues.Length) break;

                byte b = rgbValues[i];
                byte g = rgbValues[i + 1];
                byte r = rgbValues[i + 2];

                // 그레이스케일 밝기 계산
                int grayScaleValue = (int)((r * 0.3) + (g * 0.59) + (b * 0.11));
                // 최대값 갱신 (1~254 범위, 0과 255 제외)
                if (grayScaleValue > max && grayScaleValue < 255)
                    max = (byte)grayScaleValue;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"GetMaxBrightness_FromColorBitmapRectFast 실패: {ex.Message}");
            return 1;
        }
        finally
        {
            bmpColor.UnlockBits(bmpData);
        }

        return max;
    }

    public static byte GetMaxBrightness_FromColorBitmapFast(Draw.Bitmap bmpColor)
    {
        int width = bmpColor.Width;
        int height = bmpColor.Height;
        byte max = 1;

        // 전체 영역에 대해 LockBits를 통해 메모리 접근
        Draw.Rectangle rect = new Draw.Rectangle(0, 0, width, height);
        System.Drawing.Imaging.BitmapData bmpData = bmpColor.LockBits(rect,
            System.Drawing.Imaging.ImageLockMode.ReadOnly, bmpColor.PixelFormat);

        try
        {
            int bytesPerPixel = Draw.Image.GetPixelFormatSize(bmpColor.PixelFormat) / 8;
            int stride = bmpData.Stride;
            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;
                for (int y = 0; y < height; y++)
                {
                    byte* row = ptr + (y * stride);
                    for (int x = 0; x < width; x++)
                    {
                        byte* pixel = row + (x * bytesPerPixel);
                        // Format32bppArgb의 경우: [0]=B, [1]=G, [2]=R, [3]=A
                        int b = pixel[0];
                        int g = pixel[1];
                        int r = pixel[2];

                        // 그레이스케일 밝기 계산
                        int grayScaleValue = (int)((r * 0.3) + (g * 0.59) + (b * 0.11));
                        // 최대값 갱신 (1~254 범위, 0과 255 제외)
                        if (grayScaleValue > max && grayScaleValue < 255)
                            max = (byte)grayScaleValue;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"GetMaxBrightness_FromColorBitmapFast 실패: {ex.Message}");
            return 1; // 기본값 반환
        }
        finally
        {
            bmpColor.UnlockBits(bmpData);
        }

        return max;
    }

    /// <summary>
    /// 비트맵의 특정 행에서 최소 밝기값을 찾습니다 (Unsafe 코드 사용)
    /// </summary>
    /// <param name="bmpColor">소스 비트맵</param>
    /// <param name="targetRow">대상 행 번호 (0-based)</param>
    /// <returns>최소 밝기값 (1-255, 실패 시 255)</returns>
    public static byte GetMinBrightnessAtRow_FromColorBitmapFast(Draw.Bitmap bmpColor, int targetRow)
    {
        // 매개변수 검증
        if (bmpColor == null)
        {
            Debug.WriteLine("[ERROR] GetMinBrightnessAtRow_FromColorBitmapFast: bmpColor가 null입니다.");
            return 255;
        }

        int width = bmpColor.Width;
        int height = bmpColor.Height;

        if (targetRow < 0 || targetRow >= height)
        {
            Debug.WriteLine($"[ERROR] GetMinBrightnessAtRow_FromColorBitmapFast: targetRow가 범위를 벗어났습니다. (targetRow={targetRow}, height={height})");
            return 255;
        }

        byte min = 255;

        // 전체 영역에 대해 LockBits를 통해 메모리 접근
        Draw.Rectangle rect = new Draw.Rectangle(0, 0, width, height);
        System.Drawing.Imaging.BitmapData bmpData = bmpColor.LockBits(rect,
            System.Drawing.Imaging.ImageLockMode.ReadOnly, bmpColor.PixelFormat);

        try
        {
            int bytesPerPixel = Draw.Image.GetPixelFormatSize(bmpColor.PixelFormat) / 8;
            int stride = bmpData.Stride;
            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;
                byte* row = ptr + (targetRow * stride);
                for (int x = 0; x < width; x++)
                {
                    byte* pixel = row + (x * bytesPerPixel);
                    // Format32bppArgb의 경우: [0]=B, [1]=G, [2]=R, [3]=A
                    int b = pixel[0];
                    int g = pixel[1];
                    int r = pixel[2];

                    // 그레이스케일 밝기 계산
                    int grayScaleValue = (int)((r * 0.3) + (g * 0.59) + (b * 0.11));
                    // 최소값 갱신
                    if (grayScaleValue < min)
                    {
                        min = (byte)grayScaleValue;
                    }
                }
            }

            //Debug.WriteLine($"[OfrService] GetMinBrightnessAtRow: targetRow={targetRow}, min={min}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] GetMinBrightnessAtRow_FromColorBitmapFast: {StdUtil.GetExceptionMessage(ex)}");
            return 255;
        }
        finally
        {
            bmpColor.UnlockBits(bmpData);
        }

        return min;
    }

    // 비트맵 중간 행의 최소 밝기값 + offset을 반환합니다. (threshold 계산용)
    // bmpColor: 소스 비트맵, offset: 여유값(5), returns: 최소 밝기 + offset
    public static byte GetMinBrightnessAtMidRow_FromColorBitmapFast(Draw.Bitmap bmpColor, int offset = 5)
    {
        if (bmpColor == null || bmpColor.Height == 0)
            return 255;

        int midY = bmpColor.Height / 2;
        byte minBright = GetMinBrightnessAtRow_FromColorBitmapFast(bmpColor, midY);
        return (byte)Math.Min(255, minBright + offset);
    }

    // 비트맵 중간 행의 최대 밝기값 - offset을 반환합니다. (threshold 계산용)
    // bmpColor: 소스 비트맵, offset: 여유값(5), returns: 최대 밝기 - offset
    public static byte GetMaxBrightnessAtMidRow_FromColorBitmapFast(Draw.Bitmap bmpColor, int offset = 5)
    {
        if (bmpColor == null || bmpColor.Height == 0)
            return 0;

        int midY = bmpColor.Height / 2;
        byte maxBright = GetMaxBrightnessAtRow_FromColorBitmapFast(bmpColor, midY);
        return (byte)Math.Max(0, maxBright - offset);
    }

    // 비트맵 중간 행의 평균 밝기값을 반환합니다.
    // bmpColor: 소스 비트맵, returns: 중간 행 평균 밝기 (0-255)
    public static byte GetAverageBrightnessAtMidRow_FromColorBitmapFast(Draw.Bitmap bmpColor)
    {
        if (bmpColor == null || bmpColor.Height == 0)
            return 128;

        int midY = bmpColor.Height / 2;
        return GetAverageBrightnessAtRow_FromColorBitmapFast(bmpColor, midY);
    }

    // 비트맵의 특정 행에서 평균 밝기값을 계산합니다 (Unsafe 코드 사용)
    // bmpColor: 소스 비트맵, targetRow: 대상 행, returns: 평균 밝기값 (0-255, 실패 시 128)
    public static byte GetAverageBrightnessAtRow_FromColorBitmapFast(Draw.Bitmap bmpColor, int targetRow)
    {
        // 매개변수 검증
        if (bmpColor == null)
        {
            Debug.WriteLine("[ERROR] GetAverageBrightnessAtRow_FromColorBitmapFast: bmpColor가 null입니다.");
            return 128;
        }

        int width = bmpColor.Width;
        int height = bmpColor.Height;

        if (targetRow < 0 || targetRow >= height)
        {
            Debug.WriteLine($"[ERROR] GetAverageBrightnessAtRow_FromColorBitmapFast: targetRow가 범위를 벗어났습니다. (targetRow={targetRow}, height={height})");
            return 128;
        }

        if (width == 0)
            return 128;

        long sum = 0;

        // 전체 영역에 대해 LockBits를 통해 메모리 접근
        Draw.Rectangle rect = new Draw.Rectangle(0, 0, width, height);
        System.Drawing.Imaging.BitmapData bmpData = bmpColor.LockBits(rect,
            System.Drawing.Imaging.ImageLockMode.ReadOnly, bmpColor.PixelFormat);

        try
        {
            int bytesPerPixel = Draw.Image.GetPixelFormatSize(bmpColor.PixelFormat) / 8;
            int stride = bmpData.Stride;
            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;
                byte* row = ptr + (targetRow * stride);
                for (int x = 0; x < width; x++)
                {
                    byte* pixel = row + (x * bytesPerPixel);
                    // Format32bppArgb의 경우: [0]=B, [1]=G, [2]=R, [3]=A
                    int b = pixel[0];
                    int g = pixel[1];
                    int r = pixel[2];

                    // 그레이스케일 밝기 계산
                    int grayScaleValue = (int)((r * 0.3) + (g * 0.59) + (b * 0.11));
                    sum += grayScaleValue;
                }
            }

            byte avg = (byte)(sum / width);
            Debug.WriteLine($"[OfrService] GetAverageBrightnessAtRow: targetRow={targetRow}, avg={avg}");
            return avg;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] GetAverageBrightnessAtRow_FromColorBitmapFast: {StdUtil.GetExceptionMessage(ex)}");
            return 128;
        }
        finally
        {
            bmpColor.UnlockBits(bmpData);
        }
    }

    // 비트맵의 특정 행에서 최대 밝기값을 찾습니다 (Unsafe 코드 사용)
    // bmpColor: 소스 비트맵, targetRow: 대상 행, returns: 최대 밝기값 (0-254, 실패 시 0)
    public static byte GetMaxBrightnessAtRow_FromColorBitmapFast(Draw.Bitmap bmpColor, int targetRow)
    {
        // 매개변수 검증
        if (bmpColor == null)
        {
            Debug.WriteLine("[ERROR] GetMaxBrightnessAtRow_FromColorBitmapFast: bmpColor가 null입니다.");
            return 0;
        }

        int width = bmpColor.Width;
        int height = bmpColor.Height;

        if (targetRow < 0 || targetRow >= height)
        {
            Debug.WriteLine($"[ERROR] GetMaxBrightnessAtRow_FromColorBitmapFast: targetRow가 범위를 벗어났습니다. (targetRow={targetRow}, height={height})");
            return 0;
        }

        byte max = 0;

        // 전체 영역에 대해 LockBits를 통해 메모리 접근
        Draw.Rectangle rect = new Draw.Rectangle(0, 0, width, height);
        System.Drawing.Imaging.BitmapData bmpData = bmpColor.LockBits(rect,
            System.Drawing.Imaging.ImageLockMode.ReadOnly, bmpColor.PixelFormat);

        try
        {
            int bytesPerPixel = Draw.Image.GetPixelFormatSize(bmpColor.PixelFormat) / 8;
            int stride = bmpData.Stride;
            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;
                byte* row = ptr + (targetRow * stride);
                for (int x = 0; x < width; x++)
                {
                    byte* pixel = row + (x * bytesPerPixel);
                    // Format32bppArgb의 경우: [0]=B, [1]=G, [2]=R, [3]=A
                    int b = pixel[0];
                    int g = pixel[1];
                    int r = pixel[2];

                    // 그레이스케일 밝기 계산
                    int grayScaleValue = (int)((r * 0.3) + (g * 0.59) + (b * 0.11));
                    // 최대값 갱신 (255보다 작고 현재 max보다 큰 값)
                    if (grayScaleValue > max && grayScaleValue < 255)
                    {
                        max = (byte)grayScaleValue;
                    }
                }
            }

            Debug.WriteLine($"[OfrService] GetMaxBrightnessAtRow: targetRow={targetRow}, max={max}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] GetMaxBrightnessAtRow_FromColorBitmapFast: {StdUtil.GetExceptionMessage(ex)}");
            return 0;
        }
        finally
        {
            bmpColor.UnlockBits(bmpData);
        }

        return max;
    }
    #endregion

    #region My Classes

    // 비트맵에서 문자 영역의 Start/End X 좌표 리스트를 추출합니다
    // bmpOrg: 비트맵, byteAvgBrightness: 임계값, rcForeground: 전경 영역, returns: StartEnd 리스트
    public static List<OfrModel_StartEnd> GetStartEndList_FromColorBitmap(Draw.Bitmap bmpOrg, byte byteAvgBrightness, Draw.Rectangle rcForeground)
    {
        List<OfrModel_StartEnd> listStartEnd = new List<OfrModel_StartEnd>();

        if (bmpOrg == null || rcForeground.Width <= 0 || rcForeground.Height <= 0)
            return listStartEnd;

        int x2 = rcForeground.Left + rcForeground.Width - 1;
        int y2 = rcForeground.Top + rcForeground.Height - 1;

        OfrModel_StartEnd startEnd = new OfrModel_StartEnd(-1, -1);
        bool? bBlack;

        for (int x = rcForeground.Left; x <= x2; x++)
        {
            bBlack = IsAnyTrueInBitmapColumn_ByFastType(bmpOrg, byteAvgBrightness, x, rcForeground.Top, y2);

            if (bBlack == null) continue; // Null 에러 처리

            if (startEnd.nStart == -1) // 시작점이 없으면
            {
                if ((bool)bBlack) // 검은색이면
                {
                    startEnd.nStart = x; // 시작점 설정
                }
            }
            else // 시작점이 있으면
            {
                if (!(bool)bBlack) // 흰색이면
                {
                    startEnd.nEnd = x - 1; // 끝점 설정
                    listStartEnd.Add(startEnd); // 리스트에 추가
                    startEnd = new OfrModel_StartEnd(-1, -1); // 초기화
                }
            }
        }

        // 마지막 문자 처리 (끝점이 없으면)
        if (startEnd.nStart != -1 && startEnd.nEnd == -1)
        {
            startEnd.nEnd = x2; // 끝점 설정
            listStartEnd.Add(startEnd); // 리스트에 추가
        }

        return listStartEnd;
    }

    ////OfrModel_StartEnd
    //public static List<OfrModel_StartEnd> GetStartEndList_FromColorBitmap(Draw.Bitmap bmpOrg, byte byteAvgBrightness, Draw.Rectangle rcForeground)
    //{
    //    List<OfrModel_StartEnd> listStartEnd = new List<OfrModel_StartEnd>();
    //    int x2 = rcForeground.Left + rcForeground.Width - 1;
    //    int y2 = rcForeground.Top + rcForeground.Height - 1;

    //    OfrModel_StartEnd startEnd = new OfrModel_StartEnd(-1, -1);
    //    bool? bBlack;
    //    for (int x = rcForeground.Left; x <= x2; x++)
    //    {
    //        bBlack = IsAnyTrueInBitmapColumn_ByFastType(bmpOrg, byteAvgBrightness, x, rcForeground.Top, y2);
    //        if (bBlack == null) continue; // Null 처리

    //        if (startEnd.nStart == -1) // 시작점이 없으면
    //        {
    //            if ((bool)bBlack) // 검은색이면
    //            {
    //                startEnd.nStart = x; // 시작점 설정
    //            }
    //        }
    //        else // 시작점이 있으면
    //        {
    //            if (!(bool)bBlack) // 흰색이면
    //            {
    //                startEnd.nEnd = x - 1; // 끝점 설정
    //                listStartEnd.Add(startEnd); // 리스트에 추가
    //                startEnd = new OfrModel_StartEnd(-1, -1); // 시작점 초기화
    //            }
    //        }
    //    }
    //    if (startEnd.nStart != -1 && startEnd.nEnd == -1) // 끝점이 없으면
    //    {
    //        startEnd.nEnd = x2; // 끝점 설정
    //        listStartEnd.Add(startEnd); // 리스트에 추가
    //    }

    //    return listStartEnd;
    //}

    // Bool 배열에서 LeftWidth 리스트를 생성합니다 (Datagrid 컬럼 경계 검출용)
    // boolArr: Bool 배열, MaxBrightness: 임계값, minWidth: 최소너비(4), returns: LeftWidth 리스트
    public static List<OfrModel_LeftWidth> GetLeftWidthList_FromBool1Array(bool[] boolArr, byte MaxBrightness, int minWidth = 4)
    {
        // 매개변수 검증
        if (boolArr == null || boolArr.Length == 0)
        {
            Debug.WriteLine("[ERROR] GetLeftWidthList_FromBool1Array: boolArr가 null이거나 비어있습니다.");
            return new List<OfrModel_LeftWidth>();
        }

        List<OfrModel_LeftWidth> listLW = new List<OfrModel_LeftWidth>();
        OfrModel_LeftWidth lw = new OfrModel_LeftWidth(-1, -1);

        try
        {
            for (int x = 0; x < boolArr.Length; x++)
            {
                if (lw.nLeft == -1) // 시작점이 없으면
                {
                    if (!boolArr[x]) // 흰색이면 (컬럼 시작)
                    {
                        lw.nLeft = x; // 시작점 설정
                    }
                }
                else // 시작점이 있으면
                {
                    if (boolArr[x]) // 검은색이면 (컬럼 끝)
                    {
                        lw.nWidth = x - lw.nLeft; // 너비 계산
                        listLW.Add(lw); // 리스트에 추가
                        lw = new OfrModel_LeftWidth(-1, -1); // 초기화
                    }
                }
            }

            // 마지막 컬럼 처리 (끝점이 배열 끝까지인 경우)
            if (lw.nLeft != -1 && lw.nWidth == -1)
            {
                lw.nWidth = boolArr.Length - lw.nLeft; // 너비 설정
                if (lw.nWidth >= minWidth) // minWidth 이상이면 컬럼으로 인정
                    listLW.Add(lw);
            }

            //Debug.WriteLine($"[OfrService] GetLeftWidthList_FromBool1Array: 검출된 컬럼 수={listLW.Count}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] GetLeftWidthList_FromBool1Array: {StdUtil.GetExceptionMessage(ex)}");
            return new List<OfrModel_LeftWidth>();
        }

        return listLW;
    }

    // 문자 영역의 정확한 Rectangle을 계산합니다 (Top/Bottom 경계 검출)
    // bmpOrg: 비트맵, byteAvgBrightness: 임계값, rcForeground: 전경영역, listStartEnd: 좌표리스트, returns: IndexRect
    public static StdConst_IndexRect GetIndexRect_FromColorBitmapByIndex(
        Draw.Bitmap bmpOrg,
        byte byteAvgBrightness,
        Draw.Rectangle rcForeground,
        List<OfrModel_StartEnd> listStartEnd,
        int nStartIndex,
        int nEndIndex)
    {
        // 매개변수 검증
        if (bmpOrg == null || listStartEnd == null ||
            nStartIndex < 0 || nEndIndex >= listStartEnd.Count || nStartIndex > nEndIndex)
            return null;

        int top = rcForeground.Top;
        int bottom = rcForeground.Bottom - 1; // Bottom은 Height-1
        int left = listStartEnd[nStartIndex].nStart;
        int right = listStartEnd[nEndIndex].nEnd;

        bool bBlack;

        // From Top: 위에서부터 검색
        int findTop = -1;
        for (int y = top; y <= bottom; y++)
        {
            bBlack = IsAnyTrueInBitmapRow_ByFastType(bmpOrg, byteAvgBrightness, y, left, right);
            if (bBlack)
            {
                findTop = y;
                break;
            }
        }
        if (findTop == -1) return null;

        // From Bottom: 아래에서부터 검색
        int findBottom = -1;
        for (int y = bottom; y >= top; y--)
        {
            bBlack = IsAnyTrueInBitmapRow_ByFastType(bmpOrg, byteAvgBrightness, y, left, right);
            if (bBlack)
            {
                findBottom = y;
                break;
            }
        }
        if (findBottom == -1) return null;

        return new StdConst_IndexRect(left, findTop, right, findBottom);
    }

    //// OfrModel_IndexRect
    //public static StdConst_IndexRect GetIndexRect_FromColorBitmapByIndex(Draw.Bitmap bmpOrg,
    //    byte byteAvgBrightness, Draw.Rectangle rcForeground, List<OfrModel_StartEnd> listStartEnd, int nStartIndex, int nEndIndex)
    //{
    //    //OfrModel_TopBottom topBottom = new OfrModel_TopBottom(-1, -1);
    //    int top = rcForeground.Top;
    //    int bottom = rcForeground.Bottom;
    //    int harf = (top + bottom) / 2;
    //    int left = listStartEnd[nStartIndex].nStart;
    //    int right = listStartEnd[nEndIndex].nEnd;

    //    bool bBlack;

    //    // From Top
    //    int findTop = -1;
    //    for (int y = top; y <= bottom; y++)
    //    {
    //        bBlack = IsAnyTrueInBitmapRow_ByFastType(bmpOrg, byteAvgBrightness, y, left, right);
    //        if (bBlack)
    //        {
    //            findTop = y;
    //            break;
    //        }
    //    }
    //    if (findTop == -1) return null;

    //    // From Bottom
    //    int findBottom = -1;
    //    for (int y = bottom; y >= top; y--)
    //    {
    //        bBlack = IsAnyTrueInBitmapRow_ByFastType(bmpOrg, byteAvgBrightness, y, left, right);
    //        if (bBlack)
    //        {
    //            findBottom = y;
    //            break;
    //        }
    //    }
    //    if (findBottom == -1) return null;

    //    return new StdConst_IndexRect(left, findTop, right, findBottom);
    //}

    public static int GetBrightness_PerPixel(Draw.Bitmap bmp, int x, int y)
    {
        if (bmp == null) return 0; // Null 비트맵 처리

        Draw.Color color = bmp.GetPixel(x, y);
        byte r = color.R;
        byte g = color.G;
        byte b = color.B;

        //그레이스케일 밝기 계산
        return (int)((r * 0.3) + (g * 0.59) + (b * 0.11));
    }
    public static int GetBrightness_PerPixel(Draw.Bitmap bmp, Draw.Point pt)
    {
        if (bmp == null) return -1; // Null 비트맵 처리

        Draw.Color color = bmp.GetPixel(pt.X, pt.Y);
        //Debug.WriteLine($"GetBrightness_PerPixel: {pt.X}, {pt.Y} = {color.R}, {color.G}, {color.B}");
        byte r = color.R;
        byte g = color.G;
        byte b = color.B;

        //그레이스케일 밝기 계산
        return (int)((r * 0.3) + (g * 0.59) + (b * 0.11));
    }

    // 비트맵의 특정 픽셀에서 밝기를 계산합니다 (그레이스케일 변환 공식 사용)
    // bmp: 비트맵, x, y: 좌표, returns: 밝기값(0-255)
    public static byte GetPixelBrightness(Draw.Bitmap bmp, int x, int y)
    {
        if (bmp == null)
        {
            Debug.WriteLine($"[OfrService] GetPixelBrightness 실패: bmp=null");
            return 0;
        }

        if (x < 0 || x >= bmp.Width || y < 0 || y >= bmp.Height)
        {
            Debug.WriteLine($"[OfrService] GetPixelBrightness 실패: 범위 초과 ({x}, {y}), 비트맵 크기=({bmp.Width}, {bmp.Height})");
            return 0;
        }

        Draw.Color color = bmp.GetPixel(x, y);

        // 그레이스케일 밝기 계산 (ITU-R BT.601 표준)
        // Y = 0.299*R + 0.587*G + 0.114*B
        byte brightness = (byte)((color.R * 0.299) + (color.G * 0.587) + (color.B * 0.114));

        //Debug.WriteLine($"[OfrService] GetPixelBrightness: ({x}, {y}) = RGB({color.R}, {color.G}, {color.B}) → {brightness}");

        return brightness;
    }

    // 비트맵의 특정 픽셀에서 밝기를 계산합니다 (그레이스케일 변환 공식 사용)
    // bmp: 비트맵, pt: 좌표, returns: 밝기값(0-255)
    public static byte GetPixelBrightness(Draw.Bitmap bmp, Draw.Point pt)
    {
        return GetPixelBrightness(bmp, pt.X, pt.Y);
    }

    // Rectangle 영역이 색상 반전(선택) 상태인지 검증 (중심 vs 코너 비교)
    // bmp: 비트맵, rect: 영역, returns: true(선택됨), false(비선택)
    public static bool IsInvertedSelection(Draw.Bitmap bmp, Draw.Rectangle rect)
    {
        if (bmp == null) return false;
        if (rect.Width <= 4 || rect.Height <= 4) return false;

        try
        {
            // 1. 4코너 평균 (배경)
            byte corner1 = GetPixelBrightness(bmp, rect.Left, rect.Top);
            byte corner2 = GetPixelBrightness(bmp, rect.Right - 1, rect.Top);
            byte corner3 = GetPixelBrightness(bmp, rect.Left, rect.Bottom - 1);
            byte corner4 = GetPixelBrightness(bmp, rect.Right - 1, rect.Bottom - 1);

            byte cornerAvg = (byte)((corner1 + corner2 + corner3 + corner4) / 4);

            // 2. 중심 라인 설정
            int centerY = rect.Top + rect.Height / 2;
            int halfWidth = rect.Width / 2 - 2;

            // 가로가 너무 작으면 높이 3픽셀로 안전성 확보
            int sampleHeight = halfWidth < 10 ? 3 : 1;

            Draw.Rectangle rcCenter = new Draw.Rectangle(
                rect.Left + 1,
                centerY,
                halfWidth,
                sampleHeight
            );

            byte centerAvg = GetAverageBrightness_FromColorBitmapRectFast(bmp, rcCenter, 1.0);

            // 3. 비교 및 판단
            if (centerAvg > cornerAvg)
            {
                // 중심이 더 밝음 → 선택됨 (흰색 전경)
                return true;
            }
            else if (cornerAvg < 100)
            {
                // 코너(배경)가 어두움 → 선택됨 (짙은 파랑 배경, 빈 셀)
                return true;
            }
            else
            {
                // 비선택
                return false;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[OfrService] IsInvertedSelection 실패: {ex.Message}");
            return false;
        }
    }

    // Screen Capture - OfrModel_BitmapAndImage
    public static OfrModel_BitmapAndImage CaptureScreenRect_BitmapNImage(int x, int y, int width, int height, string outputPath = "")
    {
        OfrModel_BitmapAndImage info = new OfrModel_BitmapAndImage(width, height);

        using (Draw.Graphics g = Draw.Graphics.FromImage(info.bitMap!))
        {
            // 지정된 영역(x, y, width, height)을 캡처
            g.CopyFromScreen(new Draw.Point(x, y), Draw.Point.Empty, new Draw.Size(width, height));
        }

        // 이미지 파일로 저장
        if (!string.IsNullOrEmpty(outputPath)) info.bitMap!.Save(outputPath, DrawImg.ImageFormat.Png);

        info.bmpImg = ConvertBitmap_ToBitmapImage(info.bitMap!);

        return info;
    }
    public static async Task<OfrModel_BitmapAndImage> CaptureScreenRect_BitmapNImageAsync(int x, int y, int width, int height, string outputPath = "")
    {
        OfrModel_BitmapAndImage info = new OfrModel_BitmapAndImage(width, height);

        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            using (Draw.Graphics g = Draw.Graphics.FromImage(info.bitMap!))
            {
                // 지정된 영역(x, y, width, height)을 캡처
                g.CopyFromScreen(new Draw.Point(x, y), Draw.Point.Empty, new Draw.Size(width, height));
            }

            // 이미지 파일로 저장
            if (!string.IsNullOrEmpty(outputPath)) info.bitMap!.Save(outputPath, DrawImg.ImageFormat.Png);

            info.bmpImg = ConvertBitmap_ToBitmapImage(info.bitMap!);
        });

        return info;
    }

    public static OfrModel_BitmapAndImage CaptureScreenRect_BitmapNImage(Draw.Rectangle rc, string outputPath = "")
    {
        int x = rc.X;
        int y = rc.Y;
        int width = rc.Width;
        int height = rc.Height;

        return CaptureScreenRect_BitmapNImage(x, y, width, height, outputPath);
    }
    public static async Task<OfrModel_BitmapAndImage> CaptureScreenRect_BitmapNImageAsync(Draw.Rectangle rc, string outputPath = "")
    {
        int x = rc.X;
        int y = rc.Y;
        int width = rc.Width;
        int height = rc.Height;

        return await CaptureScreenRect_BitmapNImageAsync(x, y, width, height, outputPath);
    }

    public static async Task<OfrModel_BitmapAndImage>
        WaitCaptureScreenRect_BitmapNImageAsync(Draw.Rectangle rc, Draw.Bitmap bmpOld, int nRepeat = 20, int nGab = 25)
    {
        OfrModel_BitmapAndImage info = new OfrModel_BitmapAndImage();

        for (int j = 0; j < nRepeat; j++)
        {
            info = OfrService.CaptureScreenRect_BitmapNImage(rc); // 캡쳐
            if (info.bitMap == null)
            {
                await Task.Delay(nGab);
                continue;
            }

            if (!OfrService.IsSameBitmap_ByFastType(bmpOld, info.bitMap))
            {
                return info;
            }

            await Task.Delay(nGab);
        }

        return info;
    }

    // Convert Helper
    public static BitmapImage ConvertBitmap_ToBitmapImage(Draw.Bitmap bitmap)
    {
        if (bitmap == null) return null;

        using (MemoryStream memory = new MemoryStream())
        {
            // Save the Bitmap to the MemoryStream as PNG or other format
            bitmap.Save(memory, DrawImg.ImageFormat.Png);
            memory.Position = 0;

            // Create a new BitmapImage and load the stream
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = memory;
            bitmapImage.EndInit();

            return bitmapImage;
        }
    }

    // BitmapImage를 Bitmap으로 변환합니다
    public static Draw.Bitmap ConvertBitmapImage_ToBitmap(BitmapImage bitmapImage)
    {
        if (bitmapImage == null) return null;

        using (MemoryStream outStream = new MemoryStream())
        {
            BitmapEncoder enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(bitmapImage));
            enc.Save(outStream);
            Draw.Bitmap bitmap = new Draw.Bitmap(outStream);

            return new Draw.Bitmap(bitmap);
        }
    }

    public static bool IsSameBitmap_ByFastType(Draw.Bitmap bitmap1, Draw.Bitmap bitmap2)
    {
        // 크기 비교
        if (bitmap1.Width != bitmap2.Width || bitmap1.Height != bitmap2.Height)
        {
            return false;
        }

        // LockBits로 비트맵 데이터 접근
        DrawImg.BitmapData data1 = bitmap1.LockBits(
            new Draw.Rectangle(0, 0, bitmap1.Width, bitmap1.Height), DrawImg.ImageLockMode.ReadOnly, DrawImg.PixelFormat.Format32bppArgb);
        DrawImg.BitmapData data2 = bitmap2.LockBits(
            new Draw.Rectangle(0, 0, bitmap2.Width, bitmap2.Height), DrawImg.ImageLockMode.ReadOnly, DrawImg.PixelFormat.Format32bppArgb);

        int bytesPerPixel = 4; // ARGB 포맷의 경우, 픽셀 당 4바이트
        bool areIdentical = true;

        unsafe
        {
            for (int y = 0; y < bitmap1.Height; y++)
            {
                byte* row1 = (byte*)data1.Scan0 + (y * data1.Stride);
                byte* row2 = (byte*)data2.Scan0 + (y * data2.Stride);

                for (int x = 0; x < bitmap1.Width * bytesPerPixel; x += bytesPerPixel)
                {
                    // 각 픽셀의 ARGB 값을 비교
                    if (*(int*)(row1 + x) != *(int*)(row2 + x))
                    {
                        areIdentical = false;
                        break;
                    }
                }

                if (!areIdentical)
                    break;
            }
        }

        // Lock 해제
        bitmap1.UnlockBits(data1);
        bitmap2.UnlockBits(data2);

        return areIdentical;
    }
    #endregion
}
#nullable restore