import re

# 파일 경로
file_path = r"D:\CodeWork\WithVs2022\KaiWork\Kai.Client\Kai.Client.CallCenter\Kai.Client.CallCenter\Networks\NwInsungs\InsungsAct_RcptRegPage.cs"

# 파일 읽기
with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

# InitDG오더Async 호출 부분 주석 처리 (658-670줄 근처)
content = content.replace(
    '''                // InitDG오더Async 호출하여 Datagrid 강제 초기화
                StdResult_Error initResult = await InitDG오더Async(
                    DgValidationIssue.InvalidColumnCount,
                    bEdit, bWrite,
                    bMsgBox: false  // 중간 에러는 메시지박스 표시 안 함
                );

                if (initResult != null)
                {
                    Debug.WriteLine($"[InsungsAct_RcptRegPage] InitDG오더 실패 (재시도 {retry + 1}/{MAX_DG_INIT_RETRY})");
                    continue; // 재시도
                }''',
    '''                // TODO: InitDG오더Async 복구 후 주석 해제
                //// InitDG오더Async 호출하여 Datagrid 강제 초기화
                //StdResult_Error initResult = await InitDG오더Async(
                //    DgValidationIssue.InvalidColumnCount,
                //    bEdit, bWrite,
                //    bMsgBox: false  // 중간 에러는 메시지박스 표시 안 함
                //);
                //
                //if (initResult != null)
                //{
                //    Debug.WriteLine($"[InsungsAct_RcptRegPage] InitDG오더 실패 (재시도 {retry + 1}/{MAX_DG_INIT_RETRY})");
                //    continue; // 재시도
                //}'''
)

# ValidateDatagridState 호출 부분 주석 처리 (778줄 근처)
content = content.replace(
    '''                var validationResult = ValidateDatagridState(
                    columnCount, colWidths, colNames);''',
    '''                // TODO: ValidateDatagridState 복구 후 주석 해제
                //var validationResult = ValidateDatagridState(
                //    columnCount, colWidths, colNames);
                var validationResult = DgValidationIssue.None; // 임시'''
)

# InitDG오더Async 호출 부분 2 (786줄 근처)
content = content.replace(
    '''                    StdResult_Error initResult2 = await InitDG오더Async(
                        validationResult,
                        bEdit, bWrite,
                        bMsgBox: false
                    );

                    if (initResult2 != null)
                    {
                        Debug.WriteLine($"[InsungsAct_RcptRegPage] InitDG오더 실패 (재시도 {retry + 1}/{MAX_DG_INIT_RETRY})");
                        continue; // 재시도
                    }''',
    '''                    // TODO: InitDG오더Async 복구 후 주석 해제
                    //StdResult_Error initResult2 = await InitDG오더Async(
                    //    validationResult,
                    //    bEdit, bWrite,
                    //    bMsgBox: false
                    //);
                    //
                    //if (initResult2 != null)
                    //{
                    //    Debug.WriteLine($"[InsungsAct_RcptRegPage] InitDG오더 실패 (재시도 {retry + 1}/{MAX_DG_INIT_RETRY})");
                    //    continue; // 재시도
                    //}'''
)

# 파일 쓰기
with open(file_path, 'w', encoding='utf-8') as f:
    f.write(content)

print("누락된 함수 호출 부분 주석 처리 완료!")
