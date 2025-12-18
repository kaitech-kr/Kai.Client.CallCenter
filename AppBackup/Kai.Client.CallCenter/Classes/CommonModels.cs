using Draw = System.Drawing;

namespace Kai.Client.CallCenter.Classes;

//public class CommonModel_RadioBtn
//{
//    public Draw.Point ptPos { get; set; }
//    public string sName { get; set; }

//    public CommonModel_RadioBtn(Draw.Point ptPos, string sName)
//    {
//        this.ptPos = ptPos;
//        this.sName = sName;
//    }
//}

public class CommonModel_ComboBox
{
    public string sMyName { get; set; }
    public string sYourName { get; set; }
    public Draw.Point ptPos { get; set; }

    public CommonModel_ComboBox(string sMyName, string sYourName, Draw.Point ptPos)
    {
        this.sMyName = sMyName;
        this.sYourName = sYourName;
        this.ptPos = ptPos;
    }
}


