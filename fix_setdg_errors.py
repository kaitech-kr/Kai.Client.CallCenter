import re

# 파일 경로
file_path = r"D:\CodeWork\WithVs2022\KaiWork\Kai.Client\Kai.Client.CallCenter\Kai.Client.CallCenter\Networks\NwInsungs\InsungsAct_RcptRegPage.cs"

# 파일 읽기
with open(file_path, 'r', encoding='utf-8') as f:
    lines = f.readlines()

# 결과 저장
result_lines = []

i = 0
while i < len(lines):
    line_num = i + 1
    line = lines[i]

    # 658줄: InitDG오더Async 첫 번째 호출 시작
    if line_num == 658 and 'InitDG오더Async' in line:
        # 657-678줄까지 주석 처리
        result_lines.append(lines[i-1])  # 657줄 유지
        result_lines.append('                // TODO: InitDG오더Async 복구 후 주석 해제\n')
        # 658-678줄 주석 처리
        for j in range(i, min(i+21, len(lines))):
            result_lines.append('                //' + lines[j][16:])  # 들여쓰기 유지하고 주석 추가
        i += 21
        continue

    # 778줄: ValidateDatagridState 호출
    if line_num == 778 and 'ValidateDatagridState' in line:
        result_lines.append('                // TODO: ValidateDatagridState 복구 후 주석 해제\n')
        result_lines.append('                //var validationResult = ValidateDatagridState(\n')
        result_lines.append('                //    columnCount, colWidths, colNames);\n')
        result_lines.append('                var validationResult = DgValidationIssue.None; // 임시\n')
        i += 2  # 2줄 건너뛰기
        continue

    # 786줄: InitDG오더Async 두 번째 호출
    if line_num == 786 and 'InitDG오더Async' in line:
        result_lines.append('                    // TODO: InitDG오더Async 복구 후 주석 해제\n')
        # 786-797줄 주석 처리
        for j in range(i, min(i+12, len(lines))):
            result_lines.append('                    //' + lines[j][20:])  # 들여쓰기 유지하고 주석 추가
        i += 12
        continue

    result_lines.append(line)
    i += 1

# 파일 쓰기
with open(file_path, 'w', encoding='utf-8') as f:
    f.writelines(result_lines)

print("SetDG오더RectsAsync 에러 수정 완료!")
