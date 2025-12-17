using System.IO;
using ExcelDataReader; // Nuget: ExcelDataReader, ExcelDataReader.DataSet(여기에 전거 따라오니 이것만 설치)

using Kai.Common.StdDll_Common;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models;

using Kai.Client.CallCenter.Classes;
namespace Kai.Client.CallCenter.Classes.Class_Master;
#nullable disable
public class ExcelController : IDisposable
{
    #region Dispose
    private bool disposedValue;
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: 관리형 상태(관리형 개체)를 삭제합니다.
            }

            // TODO: 비관리형 리소스(비관리형 개체)를 해제하고 종료자를 재정의합니다.
            // TODO: 큰 필드를 null로 설정합니다.
            disposedValue = true;
        }
    }

    // // TODO: 비관리형 리소스를 해제하는 코드가 'Dispose(bool disposing)'에 포함된 경우에만 종료자를 재정의합니다.
    // ~CtrlExcel()
    // {
    //     // 이 코드를 변경하지 마세요. 'Dispose(bool disposing)' 메서드에 정리 코드를 입력합니다.
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // 이 코드를 변경하지 마세요. 'Dispose(bool disposing)' 메서드에 정리 코드를 입력합니다.
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    #endregion

    #region Constructor
    public ExcelController()
    {
        //ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // Set the license context here - 필요없음.
    }
    #endregion

    #region Excel Column Character
    //int custName = 0;
    #endregion

    #region Methods
    //public StdResult_Object ReadFromFile(string filePath, string beforebelong)
    //{
    //    var dataList = new List<TbCustMain>();
    //    List<string> listCols = new List<string>();
    //    StdResult_Object resultObj = new StdResult_Object();

    //    // null 문자를 제거하거나 공백으로 대체하는 메서드
    //    string CleanString(string input) =>
    //        input?.Replace("\0", " ") ?? string.Empty; // null 문자 제거 후 공백으로 대체, null일 경우 빈 문자열 반환

    //    try
    //    {
    //        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

    //        using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
    //        {
    //            using (var reader = ExcelReaderFactory.CreateReader(stream))
    //            {
    //                var result = reader.AsDataSet();
    //                var worksheet = result.Tables[0]; // 첫 번째 시트를 가져옵니다.

    //                #region Column Index Check
    //                for (int col = 0; col < worksheet.Columns.Count; col++)
    //                {
    //                    listCols.Add(worksheet.Rows[0][col].ToString());
    //                    //Debug.WriteLine($"{listCols[col]}");
    //                }

    //                int 고객코드 = listCols.IndexOf("고객코드");
    //                int 고객명 = listCols.IndexOf("고객명");
    //                int 거래처명 = listCols.IndexOf("거래처명");
    //                int 부서명 = listCols.IndexOf("부서명");
    //                int 담당직위 = listCols.IndexOf("담당/직위");
    //                int 전화번호 = listCols.IndexOf("전화번호");
    //                int 전화번호2 = listCols.IndexOf("전화번호2");
    //                int 사용구분 = listCols.IndexOf("사용구분");
    //                int 인터넷ID = listCols.IndexOf("인터넷ID");
    //                int 거래 = listCols.IndexOf("거래");
    //                int 요금구분 = listCols.IndexOf("요금구분");
    //                int 시도 = listCols.IndexOf("시도");
    //                int 군구 = listCols.IndexOf("군구");
    //                int 기준동 = listCols.IndexOf("기준동");
    //                int 리명 = listCols.IndexOf("리명");
    //                int 마일리지 = listCols.IndexOf("마일리지");
    //                int 사용건수 = listCols.IndexOf("사용건수");
    //                int 상세위치 = listCols.IndexOf("상세위치");
    //                int 적요 = listCols.IndexOf("적요");
    //                int 등록자명 = listCols.IndexOf("등록자명");
    //                int 등록일자 = listCols.IndexOf("등록일자");
    //                int 최종이용일자 = listCols.IndexOf("최종이용일자");
    //                int 해피콜 = listCols.IndexOf("해피콜");
    //                int 팩스번호 = listCols.IndexOf("팩스번호");
    //                int 업체구분 = listCols.IndexOf("업체구분");
    //                int 이메일 = listCols.IndexOf("이메일");
    //                int 메모 = listCols.IndexOf("메모");
    //                #endregion

    //                for (int row = 1; row < worksheet.Rows.Count; row++) // 0번째 행은 헤더이므로 1부터 시작합니다.
    //                {
    //                    try
    //                    {
    //                        var data = new TbCustMain
    //                        {
    //                            MemberCode = CommonVars.s_CenterCharge.MemberCode,
    //                            CenterCode = CommonVars.s_CenterCharge.CenterCode,
    //                            CompCode = 0,
    //                            CustName = CleanString(worksheet.Rows[row][고객명].ToString()),  // 열 E (4번째 열)
    //                            TelNo1 = StdConvert.MakePhoneNumberToDigit(worksheet.Rows[row][전화번호].ToString()),   // 열 J (9번째 열)
    //                            TelNo2 = StdConvert.MakePhoneNumberToDigit(worksheet.Rows[row][전화번호2].ToString()),   // 열 K (10번째 열)
    //                            DongBasic = CleanString(worksheet.Rows[row][기준동].ToString()), // 열 R (17번째 열)
    //                            DongAddr = "", // 열 T (19번째 열)
    //                            DetailAddr = CleanString(worksheet.Rows[row][상세위치].ToString()), // 열 W (22번째 열)
    //                            DeptName = CleanString(worksheet.Rows[row][부서명].ToString()),   // 열 H (7번째 열)
    //                            ChargeName = CleanString(worksheet.Rows[row][담당직위].ToString()), // 열 I (8번째 열)
    //                            TradeType = CleanString(worksheet.Rows[row][거래].ToString()), // 열 N (13번째 열)
    //                            Remarks = CleanString(worksheet.Rows[row][적요].ToString()),  // 열 X (23번째 열)
    //                            Memo = CleanString(worksheet.Rows[row][메모].ToString()),     // 열 AG (32번째 열)
    //                            Register = CleanString(worksheet.Rows[row][등록자명].ToString()), // 열 Z (25번째 열)
    //                            RegDate = DateTime.Now,
    //                            EditDate = DateTime.Now,
    //                            Lon = 0,
    //                            Lat = 0,
    //                            SiDoName = CleanString(worksheet.Rows[row][시도].ToString()), // 열 P (15번째 열)
    //                            GunGuName = CleanString(worksheet.Rows[row][군구].ToString()), // 열 Q (16번째 열)
    //                            DongRiName = CleanString(worksheet.Rows[row][리명].ToString()), // 열 S (18번째 열)
    //                            DiscountType = "", // 열 O (14번째 열)
    //                            FaxNo = CleanString(worksheet.Rows[row][팩스번호].ToString()),     // 열 AD (30번째 열)
    //                            Email = CleanString(worksheet.Rows[row][이메일].ToString()),     // 열 AF (31번째 열)
    //                            CustId = CleanString(worksheet.Rows[row][인터넷ID].ToString()), // 열 M 
    //                            CustPw = "", // 열 G (6번째 열)
    //                            Etc01 = "", // 열 U (20번째 열)
    //                            Etc02 = "", // 열 V (21번째 열)
    //                            HappyCall = false,// worksheet.Rows[row][29].ToString(), // 열 AC (29번째 열)
    //                            BeforeCustKey = StdConvert.StringToLong(worksheet.Rows[row][고객코드].ToString()), // 열 D (3번째 열)
    //                            BeforeBelong = beforebelong,
    //                            BeforeCompName = CleanString(worksheet.Rows[row][거래처명].ToString()),
    //                            Using = worksheet.Rows[row][사용구분].ToString() == "Y" ? true : false, // 열 L (11번째 열)
    //                        };

    //                        dataList.Add(data);
    //                        //Debug.WriteLine($"[{row}]: {data.CustName}");
    //                    }
    //                    catch (Exception ex)
    //                    {
    //                        return new StdResult_Object(StdUtil.GetExceptionMessage(ex), "ReadFromFile_01");
    //                    }
    //                }
    //            }
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        return new StdResult_Object(StdUtil.GetExceptionMessage(ex), "ReadFromFile_02");
    //    }
    //    //Debug.WriteLine($"{dataList.Count}"); // Test

    //    return new StdResult_Object(dataList);
    //}
    #endregion
}
#nullable enable