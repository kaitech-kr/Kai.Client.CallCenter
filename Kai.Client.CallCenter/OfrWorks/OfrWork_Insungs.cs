using System.Diagnostics;
using Draw = System.Drawing;
using Wnd = System.Windows;

using Kai.Common.StdDll_Common;
using Kai.Common.NetDll_WpfCtrl.NetOFR;

using Kai.Client.CallCenter.Windows;
using Kai.Client.CallCenter.Networks;
//using Kai.Client.CallCenter.Networks.NwInsungs;
using static Kai.Client.CallCenter.Classes.CommonVars;
using static Kai.Client.CallCenter.Classes.CommonFuncs_StdResult;

namespace Kai.Client.CallCenter.OfrWorks;
#nullable disable
public class OfrWork_Insungs : OfrWork_Common
{
    #region OFR - 이미지 매칭 및 정보 얻기
    /// <summary>
    /// Window Handle에서 캡처한 이미지가 원하는 텍스트와 일치하는지 확인 (개선된 버전 - 중복 다이얼로그 제거)
    /// </summary>
    public static async Task<StdResult_NulBool> OfrIsMatchedImage_DrawRelRectAsync(
        IntPtr hWnd, int offset, string sWantedStr, bool bEdit = true, bool bWrite = true, bool bMsgBox = true)
    {
        Draw.Bitmap bmpOrg = null;
        OfrResult_TbText result = null;

        // UI가 완전히 로드될 때까지 반복 시도 (마지막 시도에만 에러 처리 활성화)
        for (int i = 1; i <= c_nRepeatNormal; i++)
        {
            bool bLastAttempt = (i == c_nRepeatNormal); // 마지막 시도 여부

            bmpOrg = OfrService.CaptureScreenRect_InWndHandle(hWnd, offset);
            // 마지막 시도에만 bEdit, bWrite, bMsgBox 활성화
            result = await OfrWork_Common.OfrImage_ExactDrawRelRectAsync(bmpOrg, bLastAttempt && bEdit, bLastAttempt && bWrite, bLastAttempt && bMsgBox);

            if (result._sResult != null && result._sResult == sWantedStr)
            {
                Debug.WriteLine($"[OfrWork_Insungs] OFR 매칭 성공: {sWantedStr} (시도={i}/{c_nRepeatNormal})");
                return new StdResult_NulBool(true);
            }

            if (!bLastAttempt)
                await Task.Delay(c_nWaitNormal);
        }

        // 모든 시도 실패
        Debug.WriteLine($"[OfrWork_Insungs] OFR 매칭 실패: 원하는={sWantedStr}, 실제={result?._sResult}, Err={result?.sErr}");
        return new StdResult_NulBool(result?.sErr ?? "알 수 없는 오류", result?.sPos ?? "OfrWork_Insungs/OfrIsMatchedImage_DrawRelRectAsync");
    }


//    //public static async Task<StdResult_Bool> OfrFindMacthedImage_ByHandleAsync(string sImgName, IntPtr hWnd, int nGab, bool bSaveIfNotFind)
//    //{
//    //    OfrResult_TbText result = await OfrImage_DrawRelRectAsync(hWnd, nGab); // TbText에서 이미지 찾기

//    //    if (string.IsNullOrEmpty(result.strResult) || result.strResult != sImgName) // DB에 없거나, 다르면 // 임시 왜곡
//    //    {
//    //        if (bSaveIfNotFind && s_bDebugMode)
//    //        {
//    //            ImageToTextWnd wnd = new ImageToTextWnd("OfrWork_Insungs/OfrFindMacthedImage_ByHandleAsync_01", result);
//    //            wnd.ShowDialog();

//    //            if (wnd.bResult == true)
//    //            {
//    //                //string sText = wnd.TBoxText.Text;
//    //                //string sReserved = wnd.TBoxReserved.Text;

//    //                //StdResult_Bool resultBool = await SaveObjectUnitAsync(result.bmpFail, sText, sReserved);
//    //                //if (resultBool.bResult) MsgBox($"{sImgName} DB저장 성공");
//    //                //else MsgBox($"{sImgName} DB저장 실패", "OfrWork_Insungs/OfrFindMacthedImage_ByHandleAsync_01");

//    //                MsgBox("코드 작성해야 합니다,", "OfrWork_Insungs/OfrFindMacthedImage_ByHandleAsync_01_1");
//    //            }
//    //        }
//    //        else
//    //            return LocalCommon_StdResult.ErrMsgResult_Bool($"[{sImgName}] 이미지가 DB에 없습니다.", "OfrWork_Insungs/OfrFindMacthedImage_ByHandleAsync_02");
//    //    }

//    //    return null;
//    //}

//    //public static int OfrRealDataRowCount(Bitmap bmpDG, InsungsInfo_Mem mInfo)
//    //{
//    //    InsungsInfo_Mem.RcptRegPage mRcpt = mInfo.RcptPage;
//    //    int nCount = 0;
//    //    Draw.Rectangle[,] rects = mRcpt.DG오더_RelChildRects;
//    //    int x = rects[0, 1].Left + 5; // 5는 여유
//    //    int nThreshold = mRcpt.DG오더_nBackgroundBright - 1; // 1은 여유 - 우측 경계선 ForeColor
//    //    int nCurBright = nThreshold;

//    //    for (int y = 2; y < rects.GetLength(1); y++)
//    //    {
//    //        nCurBright = OfrService.GetBrightness_PerPixel(bmpDG, rects[0, y].Right, rects[0, y].Top + 6); // Bitmap과 hWnd의 Right가 1만큼의 차이가 있는것 같음
//    //        #region Test
//    //        //Debug.WriteLine($"y={y}, cur={nCurBright}, nThreshold={nThreshold}"); // Test
//    //        //MyWin32.SetCursorPos_RelDrawPt(mInfo.접수등록Page_DG오더_hWnd, rects[0, y].Right + 1, rects[0, y].Top + 6); // Test 
//    //        #endregion

//    //        if (nCurBright < nThreshold) // 6은 여유 - 우측 경계선 ForeColor
//    //        {
//    //            nCount += 1;
//    //        }
//    //        else
//    //        {
//    //            break;
//    //        }
//    //    }

//    //    return nCount;
//    //}

//    //public static async Task<int> OfrRealDataRowCountAsync(Draw.Bitmap bmpDG, InsungsInfo_Mem mInfo)
//    //{
//    //    InsungsInfo_Mem.RcptRegPage mRcpt = mInfo.RcptPage;
//    //    int nCount = 0;
//    //    Draw.Rectangle[,] rects = mRcpt.DG오더_RelChildRects;
//    //    int x = rects[0, 1].Left + 5; // 5는 여유
//    //    int nThreshold = mRcpt.DG오더_nBackgroundBright - 1; // 1은 여유 - 우측 경계선 ForeColor
//    //    int nCurBright = nThreshold;

//    //    await Wnd.Application.Current.Dispatcher.InvokeAsync(() =>
//    //    {
//    //        for (int y = 2; y < rects.GetLength(1); y++)
//    //        {
//    //            nCurBright = OfrService.GetBrightness_PerPixel(bmpDG, rects[0, y].Right, rects[0, y].Top + 6); // Bitmap과 hWnd의 Right가 1만큼의 차이가 있는것 같음
//    //            #region Test
//    //            //Debug.WriteLine($"y={y}, cur={nCurBright}, nThreshold={nThreshold}"); // Test
//    //            //MyWin32.SetCursorPos_RelDrawPt(mInfo.접수등록Page_DG오더_hWnd, rects[0, y].Right + 1, rects[0, y].Top + 6); // Test 
//    //            #endregion

//    //            if (nCurBright < nThreshold) // 6은 여유 - 우측 경계선 ForeColor
//    //            {
//    //                nCount += 1;
//    //            }
//    //            else
//    //            {
//    //                break;
//    //            }
//    //        }
//    //    });

//    //    return nCount;
//    //}

//    //public static async Task<OfrResult_TbCharSetList> OfrAnyResultFrom_From접수등록DatagridAsync(InsungsAct_ReceiptPage c, Draw.Bitmap bmpOrg, int x, int y, bool bEdit = true, bool bWrite = true, bool bMsgBox = true)
//    //{
//    //    InsungsInfo_Mem mInfo = c.mInfo;
//    //    InsungsInfo_Mem.RcptRegPage mRcpt = mInfo.RcptPage;

//    //    NwCommon_DgColumnHeader colHeader = c.m_ReceiptDgHeaderInfos[x] as NwCommon_DgColumnHeader;
//    //    string sName = mRcpt.DG오더_ColumnTexts[x];
//    //    Draw.Rectangle rc = mRcpt.DG오더_RelChildRects[x, y];

//    //    if (colHeader == null) return LocalCommon_OfrResult
//    //        .ErrMsgResult_TbCharSetList(null, $"[{x}]: {colHeader.sName} is null", "OfrWork_Insungs/OfrAnyResultFrom_From접수등록DatagridAsync_01");

//    //    if (colHeader.bOfrSeq) return await OfrWork_Common.OfrStrResultFrom_SeqCharBitmapAsync(bmpOrg, rc, bEdit, bWrite, bMsgBox);
//    //    else return await OfrWork_Common.OfrStrResultFrom_ComplexMultiCharBitmapAsync(bmpOrg, rc, bEdit, bWrite, bMsgBox);
//    //}
    #endregion

//    //public static async Task<StdResult_NulBool> OfrImgReChkValue_RectInHWndAsync(IntPtr hWndTop, Draw.Rectangle rcRelChkBox, bool bEdit = true)
//    //{
//    //    try
//    //    {
//    //        Draw.Bitmap bmpChkBox = null;
//    //        OfrResult_TbText resultOfr = null;

//    //        for (int i = 0; i < 50; i++)
//    //        {
//    //            bmpChkBox = OfrService.CaptureScreenRect_InWndHandle(hWndTop, rcRelChkBox);//, "Test001.png");
//    //            resultOfr = await OfrWork_Common.OfrImage_InSparedBitmapt_ByDualBrightnessAsync(bmpChkBox);

//    //            //Debug.WriteLine($"OfrImgReChkValue_RectInHWndAsync [{i}]: {resultOfr._sResult}");

//    //            if (resultOfr != null && resultOfr.tbText != null) break;

//    //            await Task.Delay(c_nWaitNormal);
//    //        }

//    //        // DB에 있으면
//    //        if (!string.IsNullOrEmpty(resultOfr._sResult)) return new StdResult_NulBool(resultOfr._sResult == "Checked");

//    //        // DB에 없으면
//    //        if (s_bDebugMode)
//    //        {
//    //            ImageToCheckState wnd = null;

//    //            await Wnd.Application.Current.Dispatcher.InvokeAsync(() =>
//    //            {
//    //                wnd = new ImageToCheckState("OfrWork_Insungs/OfrImgChkValue_RectInHWndAsync_01", resultOfr);
//    //                wnd.ShowDialog();
//    //            });

//    //            resultOfr = wnd.ofrResult; // 고대역폭의 저장이 필요함

//    //            return new StdResult_NulBool(resultOfr._sResult == "Checked");
//    //        }

//    //        return new StdResult_NulBool();
//    //    }
//    //    catch (Exception ex)
//    //    {
//    //        return LocalCommon_StdResult
//    //            .ErrMsgResult_NulBool(StdUtil.GetExceptionMessage(ex), "OfrWork_Insungs/OfrImgChkValue_RectInHWndAsync_999");
//    //    }
//    //}

//    //public static async Task<StdResult_NulBool> OfrImgUntilChkValue_RectInHWndAsync(IntPtr hWndTop, bool bWantedValue, Draw.Rectangle rcRelChkBox) // 되도록 한번만 찍는 용도로 
//    //{
//    //    string sWanted = "Checked";
//    //    if (!bWantedValue) sWanted = "Unchecked";

//    //    try
//    //    {
//    //        Draw.Bitmap bmpChkBox = null;
//    //        OfrResult_TbText resultOfr = null;

//    //        for (int i = 0; i < 20; i++)
//    //        {
//    //            await Task.Delay(c_nWaitNormal);

//    //            bmpChkBox = OfrService.CaptureScreenRect_InWndHandle(hWndTop, rcRelChkBox);//, "Test001.png");
//    //            resultOfr = await OfrWork_Common.OfrImage_InSparedBitmapt_ByDualBrightnessAsync(bmpChkBox);
//    //            //Debug.WriteLine($"OfrImgUntilChkValue_RectInHWndAsync [{i}]: {resultOfr._sResult}, {hWndTop}"); // Test

//    //            if (resultOfr == null || resultOfr.tbText == null) continue;
//    //            if (!string.IsNullOrEmpty(resultOfr._sResult) && resultOfr._sResult == sWanted) return new StdResult_NulBool(true);
//    //        }

//    //        // DB에 있으면
//    //        if (!string.IsNullOrEmpty(resultOfr._sResult)) return new StdResult_NulBool(resultOfr._sResult == sWanted);

//    //        // DB에 없으면
//    //        if (s_bDebugMode)
//    //        {
//    //            ImageToCheckState wnd = null;

//    //            await Wnd.Application.Current.Dispatcher.InvokeAsync(() =>
//    //            {
//    //                wnd = new ImageToCheckState("OfrWork_Insungs/OfrImgChkValue_RectInHWndAsync_01", resultOfr);
//    //                wnd.ShowDialog();
//    //            });

//    //            resultOfr = wnd.ofrResult; // 고대역폭의 저장이 필요함

//    //            return new StdResult_NulBool(resultOfr._sResult == sWanted);
//    //        }

//    //        return new StdResult_NulBool();
//    //    }
//    //    catch (Exception ex)
//    //    {
//    //        return LocalCommon_StdResult
//    //            .ErrMsgResult_NulBool(StdUtil.GetExceptionMessage(ex), "OfrWork_Insungs/OfrImgChkValue_RectInHWndAsync_999");
//    //    }
//    //}


//    //public static async Task<StdResult_NulBool> OfrImgChkValue_RectInBitmapAsync(Draw.Bitmap bmpOrg, Draw.Rectangle rcRelChkBox, bool bEdit = true)
//    //{
//    //    try
//    //    {
//    //        OfrResult_TbText resultImg = await OfrWork_Common.OfrImage_DrawRelSpareRect_ByDualBrightnessAsync(bmpOrg, rcRelChkBox);
//    //        if (!string.IsNullOrEmpty(resultImg._sResult)) return new StdResult_NulBool(resultImg._sResult == "Checked");
//    //        //MsgBox($"OfrImgChkValue_DrawRelRectAsync: {resultImg._sResult}"); // Test

//    //        if (s_bDebugMode)
//    //        {
//    //            return await Wnd.Application.Current.Dispatcher.InvokeAsync(() =>
//    //            {
//    //                ImageToCheckState wnd = new ImageToCheckState("OfrWork_Insungs/OfrImgChkValue_RectInBitmapAsync_01", resultImg);
//    //                wnd.ShowDialog();

//    //                if (wnd.ofrResult._sResult != null) return new StdResult_NulBool(resultImg._sResult == "Checked");

//    //                return LocalCommon_StdResult
//    //                    .ErrMsgResult_NulBool($"DB저장 실패", "OfrWork_Insungs/OfrImgChkValue_DrawRelRectAsync_02");
//    //            });
//    //        }
//    //        else
//    //        {
//    //            return LocalCommon_StdResult
//    //                .ErrMsgResult_NulBool($"DB에 없읍니다.", "OfrWork_Insungs/OfrImgChkValue_DrawRelRectAsync_03");
//    //        }
//    //    }
//    //    catch (Exception ex)
//    //    {
//    //        return LocalCommon_StdResult
//    //            .ErrMsgResult_NulBool(StdUtil.GetExceptionMessage(ex), "OfrWork_Insungs/OfrImgChkValue_DrawRelRectAsync_999");
//    //    }
//    //}

//    //public static async Task<StdResult_String> OfrImgChkName_DrawRelRectAsync(IntPtr hWndTop, Draw.Rectangle rcRelChkBox, bool bMsgBox = true)
//    //{
//    //    try
//    //    {
//    //        //Draw.Rectangle rcBoxSpare = new Rectangle(rcRelChkBox.Left + nGab, rcRelChkBox.Top + nGab, rcRelChkBox.Width - nGab - nGab, rcRelChkBox.Height - nGab - nGab);
//    //        Draw.Bitmap bmpChkBox = OfrService.CaptureScreenRect_InWndHandle(hWndTop, rcRelChkBox);

//    //        OfrResult_TbText resultImg = await OfrImage_DrawRelSpareRect_ByDualBrightnessAsync(bmpChkBox);
//    //        if (!string.IsNullOrEmpty(resultImg.sErr))
//    //        {
//    //            ErrMsgBox(resultImg.sErr, "OfrImgChkValue_DrawRelRectAsync_01");
//    //            return new StdResult_String(null, "OfrImage_DrawRelSpareRect_ByDualBrightnessAsync 실패", "OfrWork_Insungs/OfrImgChkValue_DrawRelRectAsync_01", s_sLogDir);
//    //        }
//    //        //Debug.WriteLine($"OfrImgChkValue_DrawRelRectAsync: {resultImg.strResult}"); // Test

//    //        return await Application.Current.Dispatcher.Invoke(async () =>
//    //        {
//    //            //if (string.IsNullOrEmpty(resultImg.strResult) && bMsgBox) // DB에 없으면
//    //            //{
//    //            //    ImageToTextWnd wnd = new ImageToTextWnd("OfrWork_Insungs/OfrImgChkValue_DrawRelRectAsync_00", resultImg.bmpFail, "");
//    //            //    wnd.ShowDialog();

//    //            //    if (wnd.m_bResult == true)
//    //            //    {
//    //            //        string sText = wnd.TBoxText.Text;
//    //            //        string sReserved = wnd.TBoxReserved.Text;

//    //            //        StdResult_Bool resultBool = await SaveObjectUnitAsync(resultImg.bmpFail, sText, sReserved);
//    //            //        if (resultBool.bResult)
//    //            //        {
//    //            //            MsgBox($"[{wnd.TBoxText.Text}] DB저장 성공: ");
//    //            //            resultImg.strResult = sText;
//    //            //            return new StdResult_NulBool(resultImg.strResult == "Checked");
//    //            //        }
//    //            //        else
//    //            //        {
//    //            //            return new StdResult_NulBool(null,
//    //            //                $"{wnd.TBoxText} DB저장 실패", "OfrWork_Insungs/OfrImgChkValue_DrawRelRectAsync_01", s_sLogDir);
//    //            //        }
//    //            //    }

//    //            //    return new StdResult_NulBool(null,
//    //            //        $"{wnd.TBoxText} DB저장 실패", "OfrWork_Insungs/OfrImgChkValue_DrawRelRectAsync_02", s_sLogDir);
//    //            //}

//    //            return new StdResult_String(resultImg.strResult);
//    //        });
//    //    }
//    //    catch (Exception ex)
//    //    {
//    //        return new StdResult_String(null, ex, "OfrWork_Insungs/OfrImgChkValue_DrawRelRectAsync_999", s_sLogDir);
//    //    }
//    //}

//    //public static async Task<StdResult_String> OfrChkedImgName_DrawRelRectAsync(Bitmap bmpOrg, OfrModel_CheckBtn[] btns)
//    //{
//    //    try
//    //    {
//    //        if (Application.Current == null || Application.Current.Dispatcher == null)
//    //            return new StdResult_String("");

//    //        return await Application.Current.Dispatcher.Invoke(async () =>
//    //        {
//    //            for (int i = 0; i < btns.Length; i++)
//    //            {
//    //                OfrResult_TbText resultImg = await OfrImage_DrawRelSpareRect_ByDualBrightnessAsync(bmpOrg, btns[i].rcRelPartsT);

//    //                if (resultImg.strResult == "Checked") // Checked를 만나면 탈출
//    //                {
//    //                    return new StdResult_String(btns[i].sBtnName);
//    //                }
//    //                else if (resultImg.strResult == "Unchecked")
//    //                {
//    //                    continue;
//    //                }

//    //                // Checked도아니고, Unchecked도 아니면 DB에 없거나 다른것
//    //                if (string.IsNullOrEmpty(resultImg.strResult)) // DB에 없으면
//    //                {
//    //                    ImageToTextWnd wnd = new ImageToTextWnd("OfrWork_Insungs/OfrChkedImgName_DrawRelRectAsync_00", resultImg.bmpFail, resultImg.strResult);
//    //                    wnd.ShowDialog();

//    //                    if (wnd.m_bResult == true)
//    //                    {
//    //                        string sText = wnd.TBoxText.Text;
//    //                        string sReserved = wnd.TBoxReserved.Text;

//    //                        StdResult_Bool resultBool = await SaveObjectUnitAsync(resultImg.bmpFail, sText, sReserved);
//    //                        if (resultBool.bResult)
//    //                        {
//    //                            MsgBox($"{sText} DB저장 성공");
//    //                            if (sText == "Checked") return new StdResult_String(sText);
//    //                            else if (sText == "Unchecked") continue;
//    //                            else return new StdResult_String("",
//    //                                $"[{sText}]: 라디오버튼은 Checked 또는 Unchecked이어야 합니다.",
//    //                                "OfrWork_Insungs/OfrChkedImgName_DrawRelRectAsync_01",
//    //                                s_sLogDir);
//    //                        }
//    //                        else
//    //                        {
//    //                            MsgBox($"{sText} DB저장 실패");
//    //                            return new StdResult_String("",
//    //                                $"{sText} DB저장 실패", "OfrWork_Insungs/OfrChkedImgName_DrawRelRectAsync_02", s_sLogDir);
//    //                        }
//    //                    }
//    //                    else // 작업포기
//    //                    {
//    //                        return new StdResult_String("",
//    //                            $"{wnd.TBoxText} DB저장 실패", "OfrWork_Insungs/OfrChkedImgName_DrawRelRectAsync_01", s_sLogDir);
//    //                    }
//    //                }
//    //                else
//    //                {
//    //                    return new StdResult_String("", $"[{resultImg.strResult}]: 라디오버튼은 Checked 또는 Unchecked이어야 합니다.",
//    //                        "OfrWork_Insungs/OfrChkedImgName_DrawRelRectAsync_01", s_sLogDir);
//    //                }
//    //            }

//    //            return new StdResult_String("", "Checked 가 없습니다", "OfrWork_Insungs/OfrChkedImgName_DrawRelRectAsync_01", s_sLogDir);
//    //        });
//    //    }
//    //    catch (Exception ex)
//    //    {
//    //        return new StdResult_String("", ex, "OfrWork_Insungs/OfrChkedImgName_DrawRelRectAsync_999", s_sLogDir);
//    //    }
//    //}
//    //public static async Task<StdResult_NulBool> OfrImgChkValue_InBtnGroupAsync(OfrModel_RadioBtns btns, int index, bool bMsgBox = true)
//    //{
//    //    try
//    //    {
//    //        Draw.Bitmap bmpChkBox = OfrService.CaptureScreenRect_InWndHandle(btns.hWndTop, btns.Btns[index].rcRelPartsT);//, "Test001.png");
//    //        OfrResult_TbText resultOfr = await OfrWork_Common.OfrImage_InSparedBitmapt_ByDualBrightnessAsync(bmpChkBox, bMsgBox);

//    //        // DB에 있으면
//    //        if (!string.IsNullOrEmpty(resultOfr._sResult))
//    //            return new StdResult_NulBool(resultOfr._sResult == "Checked");

//    //        // DB에 없으면
//    //        if (bMsgBox && s_bDebugMode)
//    //        {
//    //            ImageToCheckState wnd = null;

//    //            await Wnd.Application.Current.Dispatcher.InvokeAsync(() =>
//    //            {
//    //                wnd = new ImageToCheckState("OfrWork_Insungs/OfrImgChkValue_RectInHWndAsync", resultOfr);
//    //                wnd.ShowDialog();
//    //            });

//    //            resultOfr = wnd.ofrResult; // 고대역폭의 저장이 필요함

//    //            return new StdResult_NulBool(resultOfr._sResult == "Checked");
//    //        }
//    //    }
//    //    catch (Exception ex)
//    //    {
//    //        return LocalCommon_StdResult
//    //            .ErrMsgResult_NulBool(StdUtil.GetExceptionMessage(ex), "OfrWork_Insungs/OfrImgChkValue_RectInHWndAsync_999");
//    //    }

//    //    return new StdResult_NulBool("없는 이미지 입니다.", "OfrWork_Insungs/OfrImgChkValue_RectInHWndAsync_02");
//    //}

//    //public static async Task<StdResult_String> OfrRadioBtnName_DrawRelRectAsync(IntPtr hWndTop, OfrModel_CheckBtn[] btns, int index)
//    //{
//    //    try
//    //    {
//    //        return await Application.Current.Dispatcher.Invoke(async () =>
//    //        {
//    //            Bitmap bmpName = OfrService.CaptureScreenRect_InWndHandle(hWndTop, btns[index].rcRelBtnName);

//    //            OfrResult_TbText resultImg = await OfrImage_DrawRelSpareRect_ByDualBrightnessAsync(bmpName);
//    //            if (!string.IsNullOrEmpty(resultImg.sErr))
//    //            {
//    //                ErrMsgBox(resultImg.sErr, "OfrImgChkValue_DrawRelRectAsync_01");
//    //                return new StdResult_String(null, "OfrImage_DrawRelSpareRect_ByDualBrightnessAsync 실패", "OfrWork_Insungs/OfrImgChkValue_DrawRelRectAsync_01", s_sLogDir);
//    //            }

//    //            return new StdResult_String(resultImg.tbText.Text);
//    //        });
//    //    }
//    //    catch (Exception ex)
//    //    {
//    //        return new StdResult_String("", ex, "OfrWork_Insungs/OfrChkedImgName_DrawRelRectAsync_999", s_sLogDir);
//    //    }
//    //}
}
#nullable enable
