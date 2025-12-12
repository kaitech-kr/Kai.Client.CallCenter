using System;
using System.Diagnostics;
using System.Threading.Tasks;

private async Task<StdResult_Status> SetCheckBoxAsync(IntPtr hWndCheckBox, Draw.Rectangle rcOfrRelS, bool bWantChecked, string name)
{
    for (int i = 1; i <= c_nRepeatShort; i++)
    {
        await Task.Delay(c_nWaitNormal);

        // 1. 현재 상태 읽기
        var resultChk = await OfrWork_Insungs.OfrImgReChkValue_RectInHWndAsync(mRcpt.접수섹션_hWndTop, rcOfrRelS, i == c_nRepeatShort);
        if (resultChk.bResult == null)
        {
            if (i < c_nRepeatShort) continue;
            else return new StdResult_Status(StdResult.Fail, $"{name} 인식 실패");
        }

        // 2. 이미 원하는 상태면 성공
        if (resultChk.bResult == bWantChecked)
        {
            Debug.WriteLine($"[{AppName}] SetCheckBoxAsync: {name} 이미 {(bWantChecked ? "Checked" : "Unchecked")}");
            return new StdResult_Status(StdResult.Success);
        }

        await Std32Mouse_Post.MousePostAsync_ClickLeft(hWndCheckBox);
        for (int j = 1; j <= c_nRepeatShort; j++)
        {
            await Task.Delay(c_nWaitNormal);

            // 상태 변경 확인
            resultChk = await OfrWork_Insungs.OfrImgUntilChkValue_RectInHWndAsync(mRcpt.접수섹션_hWndTop, bWantChecked, rcOfrRelS);
            if (resultChk.bResult == true)
            {
                Debug.WriteLine($"[{AppName}] SetCheckBoxAsync: {name} → {(bWantChecked ? "Checked" : "Unchecked")} 성공");
                return new StdResult_Status(StdResult.Success);
            }
        }
    }

    return new StdResult_Status(StdResult.Fail, $"{name} 상태 변경 실패");
}