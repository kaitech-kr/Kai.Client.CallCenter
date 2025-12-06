using Draw = System.Drawing;

namespace Kai.Client.CallCenter.Classes;

public class CommonModel_RadioBtn
{
    public Draw.Point ptPos { get; set; }
    public string sName { get; set; }

    public CommonModel_RadioBtn(Draw.Point ptPos, string sName)
    {
        this.ptPos = ptPos;
        this.sName = sName;
    }
}


