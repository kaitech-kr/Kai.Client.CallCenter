import tkinter as tk
from tkinter import messagebox
# from skimage.metrics import structural_similarity as ssim # import못함.
# import skimage.metrics
# ssim = skimage.metrics.structural_similarity - import못함.
# import json

def TestParam(value):  

    root = tk.Tk()
    root.withdraw()  
    
    # 메시지 박스 표시
    messagebox.showinfo("제목", value)

    root.destroy()  

     
# TestParam(10)
# TestParam("테스트")  



