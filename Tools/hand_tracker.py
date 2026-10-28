import json, math, socket, sys
import cv2
import mediapipe as mp

UNITY = ("127.0.0.1", 5005)
cam_index = int(sys.argv[1]) if len(sys.argv) > 1 else 0

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
hands = mp.solutions.hands.Hands(max_num_hands=1, model_complexity=0,
                                 min_detection_confidence=0.6, min_tracking_confidence=0.5)
draw = mp.solutions.drawing_utils
cap = cv2.VideoCapture(cam_index)

TIPS, PIPS = (8, 12, 16, 20), (6, 10, 14, 18)

def dist(a, b):
    return math.hypot(a.x - b.x, a.y - b.y)

while cap.isOpened():
    ok, frame = cap.read()
    if not ok:
        break
    frame = cv2.flip(frame, 1)
    res = hands.process(cv2.cvtColor(frame, cv2.COLOR_BGR2RGB))
    pkt = {"present": False, "x": 0.5, "y": 0.5, "open": False}

    if res.multi_hand_landmarks:
        lm = res.multi_hand_landmarks[0].landmark
        ids = (0, 5, 9, 13, 17)
        x = sum(lm[i].x for i in ids) / len(ids)
        y = sum(lm[i].y for i in ids) / len(ids)
        extended = sum(dist(lm[t], lm[0]) > dist(lm[p], lm[0]) * 1.1 for t, p in zip(TIPS, PIPS))
        pkt = {"present": True, "x": x, "y": y, "open": extended >= 3}
        draw.draw_landmarks(frame, res.multi_hand_landmarks[0], mp.solutions.hands.HAND_CONNECTIONS)

    sock.sendto(json.dumps(pkt).encode(), UNITY)

    h, w = frame.shape[:2]
    cv2.line(frame, (w // 2, 0), (w // 2, h), (255, 255, 255), 1)
    cv2.putText(frame, "OPEN" if pkt["open"] else ("FIST" if pkt["present"] else "no hand"),
                (10, 30), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 0), 2)
    cv2.imshow("hand_tracker (q to quit)", frame)
    if cv2.waitKey(1) & 0xFF == ord("q"):
        break

cap.release()