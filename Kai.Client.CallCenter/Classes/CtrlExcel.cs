using System.IO;
using ExcelDataReader; // Nuget: ExcelDataReader, ExcelDataReader.DataSet

using Kai.Common.StdDll_Common;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models;
using static Kai.Client.CallCenter.Classes.CommonVars;

namespace Kai.Client.CallCenter.Classes;
#nullable disable
public class CtrlExcel : IDisposable
{
    #region Dispose
    private bool disposedValue;
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
            }
            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    #endregion

    #region Methods
    public StdResult_Object ReadFromFile(string filePath, string beforebelong)
    {
        var dataList = new List<TbCustMain>();

        // null 문자를 제거하거나 공백으로 대체
        string CleanString(string input) =>
            input?.Replace("\0", " ") ?? string.Empty;

        try
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    var result = reader.AsDataSet();
                    var worksheet = result.Tables[0];

                    for (int row = 1; row < worksheet.Rows.Count; row++)
                    {
                        try
                        {
                            var data = new TbCustMain
                            {
                                MemberCode = s_CenterCharge.MemberCode,
                                CenterCode = s_CenterCharge.CenterCode,
                                CompCode = 0,
                                CustName = CleanString(worksheet.Rows[row][4].ToString()),
                                TelNo1 = CleanString(worksheet.Rows[row][9].ToString()),
                                DongBasic = CleanString(worksheet.Rows[row][17].ToString()),
                                DongAddr = CleanString(worksheet.Rows[row][19].ToString()),
                                DetailAddr = CleanString(worksheet.Rows[row][22].ToString()),
                                DeptName = CleanString(worksheet.Rows[row][7].ToString()),
                                ChargeName = CleanString(worksheet.Rows[row][8].ToString()),
                                TradeType = CleanString(worksheet.Rows[row][13].ToString()),
                                Remarks = CleanString(worksheet.Rows[row][23].ToString()),
                                Memo = CleanString(worksheet.Rows[row][32].ToString()),
                                Register = CleanString(worksheet.Rows[row][25].ToString()),
                                RegDate = DateTime.Now,
                                EditDate = DateTime.Now,
                                Lon = 0,
                                Lat = 0,
                                SiDoName = CleanString(worksheet.Rows[row][15].ToString()),
                                GunGuName = CleanString(worksheet.Rows[row][16].ToString()),
                                DongRiName = CleanString(worksheet.Rows[row][18].ToString()),
                                DiscountType = CleanString(worksheet.Rows[row][14].ToString()),
                                FaxNo = CleanString(worksheet.Rows[row][30].ToString()),
                                Email = CleanString(worksheet.Rows[row][31].ToString()),
                                CustId = CleanString(worksheet.Rows[row][5].ToString()),
                                CustPw = CleanString(worksheet.Rows[row][6].ToString()),
                                Etc01 = CleanString(worksheet.Rows[row][20].ToString()),
                                Etc02 = CleanString(worksheet.Rows[row][21].ToString()),
                                HappyCall = false,
                                Using = true,
                                BeforeCustKey = StdConvert.StringToLong(worksheet.Rows[row][3].ToString()),
                                BeforeBelong = beforebelong,
                                BeforeCompName = ""
                            };

                            dataList.Add(data);
                        }
                        catch (Exception ex)
                        {
                            return new StdResult_Object(null, ex.Message);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            return new StdResult_Object(null, ex.Message);
        }

        return new StdResult_Object(dataList);
    }
    #endregion
}
#nullable enable
