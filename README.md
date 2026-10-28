## Boid Rhythm Game!
Author: Akiro Licitra

### Demo


https://github.com/user-attachments/assets/cf511323-a7ef-4e0e-9811-de0212e2b926


Use your left hand to click on Z/X in time, and your right hand as an open palm in the direction of the flock to track it.



### Important Files
  Assets/Resources/song.mp3 -> Put your that you want to play here!
  
  Assets/Scripts/GameConfig.cs -> Change fields like Bpm to match your song and experience

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


  ## Video Tutorial


  https://github.com/user-attachments/assets/144b1938-91af-4d28-9405-c099dd233553
