import pyautogui

pyautogui.FAILSAFE = False # 마우스 커서를 화면 왼쪽 위로 이동하면 예외 발생을 방지합니다.

def MoveTo(x, y, delay):
    pyautogui.moveTo(x, y, delay)

# 주석처리 안하면 참조하는 쪽에서 실행됨.
# MoveTo(0, 0, 2)  
