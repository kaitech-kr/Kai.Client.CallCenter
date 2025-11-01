using Kai.Common.StdDll_Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kai.Client.CallCenter.Classes;
#nullable disable

/// <summary>
/// Datagrid 검색 결과 타입
/// </summary>
[Serializable]
public class CommonResult_AutoAllocDatagrid : StdResult_Error
{
    public int nIndex { get; set; } = -1;
    public string sStatus { get; set; }

    public CommonResult_AutoAllocDatagrid() : base() { }
    public CommonResult_AutoAllocDatagrid(int index, string status) : base()
    {
        this.nIndex = index;
        this.sStatus = status;
    }
    public CommonResult_AutoAllocDatagrid(string err, string pos, string logDirPath = "") : base(err, pos, logDirPath)
    {
    }
}
#nullable enable