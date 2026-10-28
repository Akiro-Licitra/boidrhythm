## Boid Rhythm Game!
Author: Akiro Licitra



### Instructions to run MeidaPipe gesture component:

  1) Navigate to Tools folder in Terminal
     
  2) Create a virtual environment:

  ```powershell 
      py -3.12 -m venv .venv
  ```
   
  3) Install/upgrade dependencies:

  ```powershell
      .\.venv\Scripts\python.exe -m pip install --upgrade pip

      .\.venv\Scripts\python.exe -m pip install -r requirements.txt
  ```

  4) Run the hand tracker!
     
  ```powershell
       .\.venv\Scripts\python.exe .\hand_tracker.py
  ```

  Note that once dependencies are installed, you only need to run step (4) every time you start the program. Make sure to keep the MediaPipe window open while in use.
