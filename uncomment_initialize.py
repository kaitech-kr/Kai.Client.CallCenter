import re

# 파일 경로
file_path = r"D:\CodeWork\WithVs2022\KaiWork\Kai.Client\Kai.Client.CallCenter\Kai.Client.CallCenter\Networks\NwInsungs\InsungsAct_RcptRegPage.cs"

# 파일 읽기
with open(file_path, 'r', encoding='utf-8') as f:
    lines = f.readlines()

# 주석 해제 및 매직 넘버 대체
in_initialize_function = False
function_start = 0
result_lines = []

for i, line in enumerate(lines):
    line_num = i + 1

    # InitializeAsync 함수 시작 감지
    if 'public async Task<StdResult_Error> InitializeAsync' in line:
        in_initialize_function = True
        function_start = line_num
        result_lines.append(line)
        continue

    # 함수 끝 감지 (527줄 근처의 } )
    if in_initialize_function and line_num >= 527 and line.strip() == '}':
        in_initialize_function = False
        result_lines.append(line)
        continue

    # InitializeAsync 함수 내부에서만 처리
    if in_initialize_function and line_num > function_start:
        # 주석 제거 (4칸 들여쓰기 제거)
        if line.startswith('    //'):
            uncommented = line.replace('    //', '', 1)

            # 매직 넘버 대체
            uncommented = uncommented.replace('Task.Delay(500)', 'Task.Delay(CommonVars.c_nWaitVeryLong)')
            uncommented = uncommented.replace('Task.Delay(300)', 'Task.Delay(CommonVars.c_nWaitLong)')
            uncommented = uncommented.replace('Task.Delay(100)', 'Task.Delay(CommonVars.c_nWaitNormal)')
            uncommented = uncommented.replace('for (int i = 0; i < 100; i++)', 'for (int i = 0; i < CommonVars.c_nRepeatVeryMany; i++)')

            result_lines.append(uncommented)
        else:
            result_lines.append(line)
    else:
        result_lines.append(line)

# 파일 쓰기
with open(file_path, 'w', encoding='utf-8') as f:
    f.writelines(result_lines)

print(f"주석 해제 완료! {function_start}줄부터 527줄까지 처리됨")
