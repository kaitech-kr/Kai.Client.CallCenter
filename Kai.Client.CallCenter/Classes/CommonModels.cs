using Draw = System.Drawing;

namespace Kai.Client.CallCenter.Classes;

// ViewModel 기본 인터페이스
public interface IViewModelBase
{
    bool IsChanged { get; }
}

public class CModel_ComboBox
{
    public string sMyName { get; set; }
    public string sYourName { get; set; }
    public Draw.Point ptPos { get; set; }

    public CModel_ComboBox(string sMyName, string sYourName, Draw.Point ptPos)
    {
        this.sMyName = sMyName;
        this.sYourName = sYourName;
        this.ptPos = ptPos;
    }
}

// Datagrid 컬럼 헤더 정보
public class CModel_DgColumnHeader
{
    public string sName = string.Empty;
    public bool bOfrSeq = false; // Ofr시 Multi, Seq처리 여부
    public int nWidth = 0;
}
