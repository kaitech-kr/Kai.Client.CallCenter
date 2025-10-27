using System.Linq;
using System.Windows;
using System.Collections;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Draw = System.Drawing;
using Wnd = System.Windows;

using Kai.Common.StdDll_Common;
using Kai.Client.CallCenter.OfrWorks;
using Kai.Common.NetDll_WpfCtrl.NetOFR;
using static Kai.Common.FrmDll_FormCtrl.FormFuncs;
using Kai.Server.Main.KaiWork.DBs.Postgres.CharDB.Models;

using Kai.Server.Main.KaiWork.DBs.Postgres.CharDB.Services;

using static Kai.Client.CallCenter.Classes.CommonVars;

namespace Kai.Client.CallCenter.Windows;
#nullable disable
public partial class ImageToMatchedTextWnd : Window
{
    #region Variables
    public StdResult_Bool resultBool = null;
    public OfrResult_TbText Result = null;
    #endregion

    #region Basic
    /// <summary>
    /// OfrResult_TbText를 받는 생성자
    /// </summary>
    public ImageToMatchedTextWnd(string sPos, OfrResult_TbText result)
    {
        InitializeComponent();

        this.Topmost = true;
        this.TBoxPos.Text = sPos;
        this.Result = result;

        if (Result != null && Result.analyText != null && Result.analyText.bmpExact != null)
        {
            ImgDisplay.Source = OfrService.ConvertBitmap_ToBitmapImage(Result.analyText.bmpExact);
            Debug.WriteLine($"[ImageToMatchedTextWnd] 이미지 로드: {Result.analyText.nWidth}x{Result.analyText.nHeight}");
        }
    }

    /// <summary>
    /// OfrModel_BitmapAnalysis를 받는 생성자
    /// </summary>
    public ImageToMatchedTextWnd(string sPos, OfrModel_BitmapAnalysis analyText)
    {
        InitializeComponent();

        this.Topmost = true;
        this.TBoxPos.Text = sPos;
        this.Result = new OfrResult_TbText(null, analyText);

        if (Result != null && Result.analyText != null && Result.analyText.bmpExact != null)
        {
            ImgDisplay.Source = OfrService.ConvertBitmap_ToBitmapImage(Result.analyText.bmpExact);
            Debug.WriteLine($"[ImageToMatchedTextWnd] 이미지 로드: {Result.analyText.nWidth}x{Result.analyText.nHeight}");
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
         Debug.WriteLine("[ImageToMatchedTextWnd] Window_Loaded 시작");

        // 분석 정보 표시
        OfrModel_BitmapAnalysis analyText = Result?.analyText;
        if (analyText != null)
        {
            TBoxInfo01.Text = $"Width: {analyText.nWidth}\n" +
            $"Height: {analyText.nHeight}\n" +
            $"TrueRate: {analyText.trueRate:F4}\n" +
            $"HexArray: {analyText.sHexArray}";
            Debug.WriteLine($"[ImageToMatchedTextWnd] 분석 정보 로드: {analyText.nWidth}x{analyText.nHeight}");
        }

        // DB 정보 표시
        TbText tbText = Result?.tbText;
        if (tbText != null)
        {
            TBoxInfo02.Text = $"KeyCode: {tbText.KeyCode}\n" +
            $"Text: {tbText.Text}\n" +
            $"HexStrValue: {tbText.HexStrValue}\n" +
            $"Threshold: {tbText.Threshold}\n" +
            $"Searched: {tbText.Searched}\n" +
            $"Width: {tbText.Width}\n" +
            $"Height: {tbText.Height}\n" +
            $"Reserved: {tbText.Reserved}";

            TBoxShow.Text = tbText.Text;
            Debug.WriteLine($"[ImageToMatchedTextWnd] DB 정보 로드: Text={tbText.Text}");
        }

        // 이미지 영역에 사각형 오버레이 추가
        if (ImgDisplay.Source != null)
        {
            var rect = new System.Windows.Shapes.Rectangle
            {
                Width = ImgDisplay.ActualWidth,
                Height = ImgDisplay.ActualHeight,
                Stroke = Brushes.Red,
                StrokeThickness = 1,
                Fill = Brushes.Transparent
            };

            Canvas.SetLeft(rect, 0);
            Canvas.SetTop(rect, 0);
            OverlayCanvas.Children.Add(rect);
            Debug.WriteLine($"[ImageToMatchedTextWnd] 오버레이 사각형 추가: {ImgDisplay.ActualWidth}x{ImgDisplay.ActualHeight}");
        }
    }
    #endregion

    #region Buttons
    private async void BtnExex_Click(object sender, RoutedEventArgs e)
    {
         Debug.WriteLine("[ImageToMatchedTextWnd] BtnExex_Click 시작");

        // 입력 검증
        if (string.IsNullOrEmpty(TBoxSave.Text))
        {
            ErrMsgBox("저장할 텍스트가 없습니다.", "ImageToMatchedTextWnd/BtnExex_Click");
            return;
        }

        TbText tbText = Result?.tbText;
        if (tbText == null || tbText.Text == null) // Insert: DB에 없는 경우
        {
            if (Result?.analyText == null || Result.analyText.bmpExact == null)
            {
                ErrMsgBox("DB에 저장할 분석 정보가 없습니다.", "ImageToMatchedTextWnd/BtnExex_Click_01");
                return;
            }

            OfrModel_BitmapAnalysis analyText = Result.analyText;
            Draw.Bitmap bmpExact = analyText.bmpExact;

            //여러 threshold로 저장(65~254)
             byte minThreshold = 65;
            byte maxThreshold = 254;
            HashSet<string> savedHexStrings = new HashSet<string>(); // 중복 방지
            int savedCount = 0;
            int failedCount = 0;

            Debug.WriteLine($"[ImageToMatchedTextWnd] 여러 threshold로 DB Insert 시작: Text={TBoxSave.Text}, Size={bmpExact.Width}x{bmpExact.Height}");

            for (byte threshold = minThreshold; threshold <= maxThreshold; threshold++)
            {
                OfrModel_BitmapAnalysis analysis = OfrService.GetBitmapAnalysisFast(bmpExact, threshold);

                if (analysis != null && analysis.sHexArray != null && analysis.trueRate > 0 && analysis.trueRate < 1)
                {
                    // 중복된 HexString은 건너뛰기
                    if (savedHexStrings.Contains(analysis.sHexArray))
                        continue;

                    savedHexStrings.Add(analysis.sHexArray);

                    TbText newTbText = new TbText
                    {
                        Text = TBoxSave.Text,
                        Width = analysis.nWidth,
                        Height = analysis.nHeight,
                        HexStrValue = analysis.sHexArray,
                        Threshold = threshold,
                        Searched = 1,
                        Reserved = ""
                    };

                    StdResult_Long resultLong = await PgService_TbText.InsertRowAsync(newTbText);
                    if (resultLong.lResult > 0)
                    {
                        savedCount++;
                        if (savedCount == 1) // 첫 번째 저장 성공 시 UI 업데이트용으로 저장
                            tbText = newTbText;
                    }
                    else
                    {
                        failedCount++;
                    }
                }
            }

             Debug.WriteLine($"[ImageToMatchedTextWnd] DB Insert 완료: 성공={savedCount}개, 실패={failedCount}개, 고유HexString={savedHexStrings.Count}개");

            if (savedCount > 0)
            {
                resultBool = new StdResult_Bool(true);

                //DB 정보 업데이트(첫 번째 저장된 정보로)
                TBoxInfo02.Text = $"KeyCode: {tbText.KeyCode}\n" +
                $"Text: {tbText.Text}\n" +
                $"HexStrValue: {tbText.HexStrValue}\n" +
                $"Threshold: {tbText.Threshold}\n" +
                $"Searched: {tbText.Searched}\n" +
                $"Width: {tbText.Width}\n" +
                $"Height: {tbText.Height}\n" +
                $"Reserved: {tbText.Reserved}";

                TBoxShow.Text = tbText.Text;

                ErrMsgBox($"DB 저장 성공: {tbText.Text} ({savedCount}개 threshold)", "ImageToMatchedTextWnd/BtnExex_Click");
            }
            else
            {
                resultBool = new StdResult_Bool("모든 threshold에서 DB 저장 실패", "ImageToMatchedTextWnd/BtnExex_Click_02");
                ErrMsgBox($"DB 저장 실패: 저장된 항목 없음", "ImageToMatchedTextWnd/BtnExex_Click_02");
            }
        }
        else // Update: DB에 이미 있는 경우
        {
            tbText.Text = TBoxSave.Text;
            Debug.WriteLine($"[ImageToMatchedTextWnd] DB Update 시도: KeyCode={tbText.KeyCode}, Text={tbText.Text}");

            resultBool = await PgService_TbText.UpdateRowAsync(tbText);

            if (resultBool.bResult)
            {
                Debug.WriteLine($"[ImageToMatchedTextWnd] DB Update 성공");
                ErrMsgBox($"DB 업데이트 성공: {tbText.Text}", "ImageToMatchedTextWnd/BtnExex_Click");
                Close();
            }
            else
            {
                Debug.WriteLine($"[ImageToMatchedTextWnd] DB Update 실패: {resultBool.sErr}");
                ErrMsgBox(resultBool.sErr, "ImageToMatchedTextWnd/BtnExex_Click_03");
            }
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
         Close();
    }
    #endregion
}
#nullable enable