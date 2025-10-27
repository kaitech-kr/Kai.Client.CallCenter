import re

# 파일 경로
file_path = r"D:\CodeWork\WithVs2022\KaiWork\Kai.Client\Kai.Client.CallCenter\Kai.Client.CallCenter\Networks\NwInsungs\InsungsAct_RcptRegPage.cs"

# 파일 읽기
with open(file_path, 'r', encoding='utf-8') as f:
    lines = f.readlines()

result_lines = []
in_validate_function = False

for i, line in enumerate(lines):
    line_num = i + 1

    # ValidateDatagridState 함수 시작 (이미 주석 해제됨)
    if 'private DgValidationIssue ValidateDatagridState' in line:
        in_validate_function = True
        result_lines.append(line)
        continue

    # 함수 끝 (1706줄 근처)
    if in_validate_function and line_num >= 1706 and line.strip() == '//     }':
        in_validate_function = False
        result_lines.append('    }\n')
        continue

    # 함수 내부 주석 해제
    if in_validate_function and line.startswith('//'):
        uncommented = line.replace('//', '', 1)

        # 매개변수 이름 변경
        uncommented = uncommented.replace('columnTexts', 'colNames')
        uncommented = uncommented.replace('listLW', 'colWidths')

        result_lines.append(uncommented)
    else:
        result_lines.append(line)

# 파일 쓰기
with open(file_path, 'w', encoding='utf-8') as f:
    f.writelines(result_lines)

print("ValidateDatagridState 주석 해제 및 매개변수 이름 변경 완료!")
