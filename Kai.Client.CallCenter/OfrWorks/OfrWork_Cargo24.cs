using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Draw = System.Drawing;

using Kai.Common.StdDll_Common;
using Kai.Common.NetDll_WpfCtrl.NetOFR;

using Kai.Server.Main.KaiWork.DBs.Postgres.CharDB.Models;
using Kai.Server.Main.KaiWork.DBs.Postgres.CharDB.Results;
using Kai.Server.Main.KaiWork.DBs.Postgres.CharDB.Services;

using Kai.Client.CallCenter.Windows;
//using Kai.Client.CallCenter.Networks.NwInsungs;
//using Kai.Client.CallCenter.Networks.NwCargo24s;
using static Kai.Client.CallCenter.Class_Common.CommonVars;
using Kai.Common.StdDll_Common.StdWin32;

namespace Kai.Client.CallCenter.OfrWorks;
#nullable disable
public class OfrWork_Cargo24 : OfrWork_Common
{
    //public static async Task<OfrResult_TbCharSet> OfrAnyResultFrom_From접수등록DatagridAsync(
    //    Cargo24Act_ReceiptRegPage c, Bitmap bmpOrg, int x, int y, bool bSaveTextIfNotFind, bool bSaveCharIfNotFind, int nCountFind = 10)
    //{
    //    Cargo24Info_Mem mInfo = c.mInfo;
    //    Cargo24Info_Mem.RcptRegPage mRcpt = mInfo.RcptPage;

    //    NwCommon_DgColumnHeader colHeader = c.m_ReceiptDgHeaderInfos[x] as NwCommon_DgColumnHeader;
    //    string sName = c.m_ReceiptDgHeaderInfos[x].sName;
    //    Draw.Rectangle rc = mRcpt.DG오더_rcRelCells[x, y];

    //    if (colHeader == null)
    //    {
    //        return new OfrResult_TbCharSet($"[{x}]: {sName} is null", "OfrWork_Cargo24/OfrAnyResultFrom_From접수등록DatagridAsync_01", s_sLogDir);
    //    }
    //    //Debug.WriteLine($"[{x}]: {sName}, {c.m_ReceiptDgHeaderInfos[x].bSaveObject}"); // Test

    //    if (colHeader.bOfrSeq) return await OfrStrResultFrom_SeqCharBitmapAsync(bmpOrg, rc, bSaveTextIfNotFind, bSaveCharIfNotFind);
    //    else return await OfrStrResultFrom_ComplexMultiCharBitmapAsync(bmpOrg, rc, bSaveTextIfNotFind, bSaveCharIfNotFind);
    //    //return await OfrStrResultFrom_ComplexMultiCharBitmapAsync(bmpOrg, rc, bSaveTextIfNotFind, bSaveCharIfNotFind); // Test
    //}

    //public static int OfrRealDataRowCount(Bitmap bmpDG, Cargo24Info_Mem mInfo)
    //{
    //    Cargo24Info_Mem.RcptRegPage mRcpt = mInfo.RcptPage;
    //    int nCount = 0;
    //    Draw.Rectangle[,] rects = mRcpt.DG오더_rcRelCells;
    //    int x = rects[0, 1].Left + 5; // 5는 여유
    //    int nThreshold = mRcpt.DG오더_nBackgroundBright - 1; // 1은 여유 - 우측 경계선 ForeColor
    //    int nCurBright = nThreshold;

    //    for (int y = 1; y < rects.GetLength(1); y++) // Empty Row 없음.
    //    {
    //        nCurBright = OfrService.GetBrightness_PerPixel(bmpDG, mRcpt.DG오더_ptRelChkRows[y]); // Bitmap과 hWnd의 Right가 1만큼의 차이가 있는것 같음
    //        #region Test
    //        //Debug.WriteLine($"y={y}, cur={nCurBright}, nThreshold={nThreshold}"); // Test
    //        //Std32Cursor.SetCursorPos_RelDrawPt(mRcpt.DG오더_hWnd, mRcpt.DG오더_ptRelChkRows[y]); // Test 
    //        #endregion

    //        if (nCurBright < nThreshold) // 6은 여유 - 우측 경계선 ForeColor
    //        {
    //            nCount += 1;
    //        }
    //        else
    //        {
    //            break;
    //        }
    //    }

    //    return nCount;
    //}
}
#nullable enable