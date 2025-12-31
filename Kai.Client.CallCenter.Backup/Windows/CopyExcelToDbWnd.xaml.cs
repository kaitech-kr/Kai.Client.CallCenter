using System.IO;
using System.Windows;

using Kai.Common.StdDll_Common;
using static Kai.Common.NetDll_WpfCtrl.NetMsgs.NetMsgBox;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models;

using static Kai.Client.CallCenter.Classes.CommonVars;
using System.Diagnostics;
// using Kai.Common.FrmDll_FormCtrl;

namespace Kai.Client.CallCenter.Windows;
#nullable disable
public partial class CopyExcelToDbWnd : Window
{
    private string[] m_sFiles; // Excel Files - 우선은 파일이 1개만 들어온다고 가정하지만 아니면 수정, 보완해야 함
    private string m_sBeforeBelong;
    public MessageBoxResult m_msgResult = MessageBoxResult.None;
    //private bool m_bCancel = false;
    private Thread m_Thread = null;
    private CancellationTokenSource m_CancellationTokenSource;

    public CopyExcelToDbWnd(string[] sFiles, string beforebelong)
    {
        InitializeComponent();
        // TmpHide
        //this.Owner = s_MainWnd;

        //m_sFiles = sFiles;
        //m_sBeforeBelong = beforebelong;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // TmpHide
        //LblDatas.Content = "데이터정보: 데이터를 읽고 있습니다. 잠시만 기다려 주세요.";
        //LblFiles.Content = $"파일정보: {1}/{m_sFiles.Length} {Path.GetFileName(m_sFiles[0])}";

        //m_CancellationTokenSource = new CancellationTokenSource();
        //m_Thread = new Thread(() => Thread_Exec(m_CancellationTokenSource.Token));
        //m_Thread.IsBackground = true;
        //m_Thread.Start();
    }
    //private async void Thread_Exec(CancellationToken token) // Thread함수만 써야함 UI조심
    //{
    //    int success = 0;
    //    int exist = 0;

    //    try
    //    {
    //        using (CtrlExcel excel = new CtrlExcel())
    //        {
    //            //MsgBox($"m_sFiles.Length={m_sFiles.Length}"); // Test

    //            for (int i = 0; i < m_sFiles.Length; i++)
    //            {
    //                if (token.IsCancellationRequested) return;

    //                StdResult_Object resultObj = excel.ReadFromFile(m_sFiles[i], m_sBeforeBelong);

    //                //Debug.WriteLine($"[{i}]: {resultObj.objResult}");
    //                if (resultObj.objResult == null)
    //                {
    //                    await Dispatcher.BeginInvoke((Action)(() =>
    //                    {
    //                        ErrMsgBox($"엑셀파일 읽기실패: {resultObj.sErr}");
    //                        this.Close();
    //                    }));
    //                    return;
    //                }

    //                List<TbCustMain> listCust = (List<TbCustMain>)resultObj.objResult;
    //                //MsgBox($"listCust={listCust.Count}"); // Test

    //                await Dispatcher.BeginInvoke((Action)(() =>
    //                {
    //                    LblDatas.Content = $"데이터정보: 0/{listCust.Count} {0}%";
    //                    PrgBarDatas.Maximum = listCust.Count;
    //                    PrgBarDatas.Value = 0;
    //                    LblFiles.Content = $"파일정보: {i + 1}/{m_sFiles.Length} {Path.GetFileName(m_sFiles[i])}";
    //                    PrgBarFiles.Maximum = m_sFiles.Length;
    //                    PrgBarFiles.Value = i + 1;
    //                }));

    //                //if (listCust.Count > 0) // 한꺼번에???
    //                //{
    //                //    DbResult_Int result = await s_SrGClient.SrResult_CustMain_InsertListByCopy(listCust);
    //                //}

    //                int limit = (int)((double)listCust.Count * 0.95);
    //                for (int j = 0; j < listCust.Count; j++)
    //                {
    //                    if (token.IsCancellationRequested) return;

    //                    TbCustMain cust = listCust[j];
    //                    //Debug.WriteLine($"[{j}]: {cust.Alive}"); // Test
    //                    StdResult_Long resultLong = await s_SrGClient.SrResult_CustMain_InsertRowAsync_ByCopy(cust);

    //                    if (resultLong.lResult > 0) // 1 = 정상
    //                    {
    //                        success++;
    //                    }
    //                    else if (resultLong.lResult == 0) // 중복
    //                    {
    //                        exist++;
    //                    }
    //                    else
    //                    {
    //                        await Dispatcher.BeginInvoke((Action)(() =>
    //                        {
    //                            FormFuncs.ErrMsgBox($"데이터베이스 쓰기실패: {resultLong.lResult}, {resultLong.sErr}");
    //                            BtnCancel_Click(null, null);
    //                        }));
    //                        return;
    //                    }

    //                    if (j < limit && j % 10 != 0) continue;

    //                    await Dispatcher.BeginInvoke((Action)(() =>
    //                    {
    //                        double percent = (double)j / (double)listCust.Count * 100;
    //                        LblDatas.Content = $"데이터정보({listCust.Count:N0}): 진행({percent:0.00}% {j}건) 저장({success:N0}건) 건너뜀({exist:N0}건)";
    //                        PrgBarDatas.Value = j;
    //                    }));
    //                }
    //            }
    //        }
    //    }
    //    finally
    //    {
    //        await Dispatcher.BeginInvoke((Action)(() =>
    //        {
    //            m_msgResult = MessageBoxResult.OK;
    //            Close();
    //        }));
    //    }
    //}

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        // m_CancellationTokenSource.Cancel();

        // m_msgResult = MessageBoxResult.Cancel;
        // Close();
    }
}
#nullable restore