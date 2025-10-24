import mss
import mss.tools
import numpy as np
import cv2

import PyCommon

from screeninfo import get_monitors
# # from skimage.metrics import structural_similarity as ssim

########## GetMonitorNumAndCoordinates ########## 
def GetMonitorNumAndCoordinates(x, y):

    try:
        monitors = get_monitors()
        for index, monitor in enumerate(monitors):
            if (monitor.x <= x < monitor.x + monitor.width) and (monitor.y <= y < monitor.y + monitor.height):
                return (True, (index + 1, (monitor.x, monitor.y)), '없음')

        return (False, (None, (None, None)), '좌표가 어떤 모니터에도 속하지 않음.')  # 좌표가 어떤 모니터에도 속하지 않는 경우
    except Exception as e:
        return (False, (None, (None, None)), f'예외 발생: {e}')

# # 함수를 사용하여 결과를 확인합니다.
# result = GetMonitorNumAndCoordinates(100, 100)  # 예시로 100, 100 좌표를 사용

# 결과 출력
# if result[0]:
#     print(f"{result[0]}, {result[1]}, {result[2]}")  # 좌표가 어떤 모니터에 속하는지 출력합니다.    
# else:
#     print(f"{result[0]}, {result[2]}")  # 좌표가 어떤 모니터에도 속하지 않는 경우에 대한 메시지를 출력합니다.


########## GetMonitorNumAndCoordinates ########## 
def ScreenShotOfMonitor(monitor_index, left, top, width, height, output_filename=None):

    try:
        with mss.mss() as sct:
            # 모니터 리스트에서 캡처할 모니터의 정보를 얻습니다.
            monitor = sct.monitors[monitor_index]

            # 캡처할 영역을 설정합니다.
            bbox = {
                "left": monitor["left"] + left,
                "top": monitor["top"] + top,
                "width": width,
                "height": height
            }

            # 설정한 영역에 대해 스크린샷을 찍습니다.
            sct_img = sct.grab(bbox)

            # 스크린샷 파일로 저장합니다.
            if output_filename:
                mss.tools.to_png(sct_img.rgb, sct_img.size, output=output_filename)

            return (True, sct_img, '없음')   

    except Exception as e:
        return (False, None, f'예외 발생: {e}')

# ScreenShotOfMonitor(0, 0, 0, 1000, 1000, 'screenshot.png')    

     
########## ScreenShotByAbsCoordinate ########## 
def ScreenShotByAbsCoordinate(left, top, width, height, output_filename=None):
    try:
        result = GetMonitorNumAndCoordinates(left, top)  
        print(result)

        if result[0] == False:
            return (False, None, result[2])
       
        else:
            return ScreenShotOfMonitor(result[1][0], left - result[1][1][0], top - result[1][1][1], width, height, output_filename)

    except Exception as e:
        return (False, None, f'예외 발생: {e}')

# ScreenShotByAbsCoordinate(1920, 0, 1000, 1000, 'screenshot2.png')  

def GetMaxSimilarity(screenShot1, screenShot2):

    try:
        # 스크린샷 캡처
        source_image = np.array(screenShot1)
        source_image = cv2.cvtColor(source_image, cv2.COLOR_BGR2GRAY)
        target_image = np.array(screenShot2)
        target_image = cv2.cvtColor(target_image, cv2.COLOR_BGR2GRAY)

        # 템플릿 매칭
        result = cv2.matchTemplate(source_image, target_image, cv2.TM_CCOEFF_NORMED)

        # 유사도 계산
        _, max_val, _, _ = cv2.minMaxLoc(result)

        # max_val 값을 0에서 1 사이의 값으로 변환
        converted_val = (max_val + 1) / 2

        return (True, converted_val, '없음')
    
    except Exception as e:
        return (False, None, f'예외 발생: {e}')


def GetMoreSimilarIndex(imgSource, imgTarget1, imgTarget2):
    try:
        # 스크린샷 캡처
        source_image = np.array(imgSource)
        source_image = cv2.cvtColor(source_image, cv2.COLOR_BGR2GRAY)
        target_image1 = np.array(imgTarget1)
        target_image1 = cv2.cvtColor(target_image1, cv2.COLOR_BGR2GRAY)
        target_image2 = np.array(imgTarget2)
        target_image2 = cv2.cvtColor(target_image2, cv2.COLOR_BGR2GRAY) 

        # 템플릿 매칭
        result1 = cv2.matchTemplate(source_image, target_image1, cv2.TM_CCOEFF_NORMED)
        result2 = cv2.matchTemplate(source_image, target_image2, cv2.TM_CCOEFF_NORMED)  

        # 유사도 계산
        _, max_val1, _, _ = cv2.minMaxLoc(result1)
        _, max_val2, _, _ = cv2.minMaxLoc(result2)  

        absolute_gab = abs(max_val1 - max_val2)
        max_val = max_val1
        index = 0
        if max_val2 > max_val1:
            max_val = max_val2
            index  = 1

        # max_val 값을 0에서 1 사이의 값으로 변환
        converted_val = (max_val + 1) / 2

        if max_val1 == max_val2:
            return (False, -1, converted_val, absolute_gab, '두 이미지의 유사도가 동일합니다.')
        return (True, index, converted_val, absolute_gab, '없음')
        # return (True, index, 0.0, 0.0, 'Test')
    
    except Exception as e:
        return (False, -1, 0.0, 0.0, f'예외 발생: {e}')  


