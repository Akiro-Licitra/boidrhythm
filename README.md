## Boid Rhythm Game!
Author: Akiro Licitra



Instructions to run MeidaPipe gesture component:

  1) Navigate to Tools folder in Terminal
     
  2) Enter a virtual environment:
     
      py -3.12 -m venv .venv
     
  3) Install/upgrade dependencies:

      .\.venv\Scripts\python.exe -m pip install --upgrade pip
     
      .\.venv\Scripts\python.exe -m pip install mediapipe==0.10.21 opencv-contrib-python

      .\.venv\Scripts\python.exe -m pip install -r requirements.txt

  4) Verify hand tracker:

      .\.venv\Scripts\python.exe -c "import cv2, mediapipe as mp; print('OpenCV:', cv2.__version__); print('MediaPipe:', mp.__version__); print('Hands API:', mp.solutions.hands)"

     5) Run the hand tracker!
    
       .\.venv\Scripts\python.exe .\hand_tracker.py
