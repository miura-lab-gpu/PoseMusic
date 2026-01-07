import os
os.environ["OPENCV_VIDEOIO_MSMF_ENABLE_HW_TRANSFORMS"] = "0"
import cv2
import mediapipe as mp
import numpy as np
from mediapipe.tasks import python
from mediapipe.tasks.python import vision
from mediapipe import solutions
from mediapipe.framework.formats import landmark_pb2
import socket, struct, json, time
import argparse

parser = argparse.ArgumentParser()

parser.add_argument("--not_show_image", action="store_true")
parser.add_argument("--not_print_debug_image", action="store_true")

args = parser.parse_args()

server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
server.bind(("127.0.0.1", 9000))
server.settimeout(0.5)
server.listen(1)
connection = None
try:
    connection, _ = server.accept()
except socket.timeout:
    print("No connection established.")
udp = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
udp.settimeout(0.5)
udp_addr = ("127.0.0.1", 9001)

def send_image(img):
    if not connection:
        return
    _, jpg = cv2.imencode(".jpg", img, [int(cv2.IMWRITE_JPEG_QUALITY), 90])
    data = jpg.tobytes()
    connection.sendall(struct.pack(">I", len(data)))
    connection.sendall(data)

def send_data(pose_result, gesture_result, frame_id, img_height, img_width):
    data = {
        "frame_id": frame_id,
        "timestamp": int(time.time() * 1000),
        "poses": [],
        "gestures": [],
        "hands": []
    }

    if pose_result.pose_landmarks:
        data["poses"] = []
        for pose_landmarks in pose_result.pose_landmarks:
            landmarks = []
            for landmark in pose_landmarks:
                landmarks.append({
                    "x": landmark.x * img_width,
                    "y": landmark.y * img_height,
                    "z": landmark.z * img_width
                })
            data["poses"].append(landmarks)
    if gesture_result.gestures:
        data["gestures"] = []
        for hand_gestures in gesture_result.gestures:
            hand_data = []
            for gesture in hand_gestures:
                hand_data.append({
                    "category_name": gesture.category_name,
                    "score": gesture.score
                })
            data["gestures"].append(hand_data)
    if gesture_result.hand_landmarks:
        data["hands"] = []
        for hand_landmarks in gesture_result.hand_landmarks:
            landmarks = []
            for landmark in hand_landmarks:
                landmarks.append({
                    "x": landmark.x * img_width,
                    "y": landmark.y * img_height,
                    "z": landmark.z * img_width
                })
            data["hands"].append(landmarks)
    udp.sendto(json.dumps(data).encode('utf-8'), udp_addr)

pose_model_path = r"./Model/pose_landmarker_full.task"
gesture_model_path = r"./Model/gesture_recognizer.task"

BaseOptions = python.BaseOptions
PoseLandmarker = vision.PoseLandmarker
PoseLandmarkerOptions = vision.PoseLandmarkerOptions
PoseLandmarkerResult = vision.PoseLandmarkerResult
GestureRecognizer = vision.GestureRecognizer
GestureRecognizerOptions = vision.GestureRecognizerOptions
GestureRecognizerResult = vision.GestureRecognizerResult
VisionRunningMode = vision.RunningMode


with open(pose_model_path, "rb") as f:
    pose_model_buffer = f.read()
with open(gesture_model_path, "rb") as f:
    gesture_model_buffer = f.read()

def draw_pose_landmarks_on_image(rgb_image, detection_result):
    pose_landmarks_list = detection_result.pose_landmarks
    annotated_image = np.copy(rgb_image)

    # Loop through the detected poses to visualize.
    for idx in range(len(pose_landmarks_list)):
        pose_landmarks = pose_landmarks_list[idx]

        # Draw the pose landmarks.
        pose_landmarks_proto = landmark_pb2.NormalizedLandmarkList()
        pose_landmarks_proto.landmark.extend([
            landmark_pb2.NormalizedLandmark(x=landmark.x, y=landmark.y, z=landmark.z) for landmark in pose_landmarks
        ])
        solutions.drawing_utils.draw_landmarks(
            annotated_image,
            pose_landmarks_proto,
            solutions.pose.POSE_CONNECTIONS,
            solutions.drawing_styles.get_default_pose_landmarks_style())
    return annotated_image

def draw_hand_landmarks_on_image(rgb_image, detection_result):
    hand_landmarks_list = detection_result.hand_landmarks
    annotated_image = np.copy(rgb_image)

    # Loop through the detected poses to visualize.
    for idx in range(len(hand_landmarks_list)):
        hand_landmarks = hand_landmarks_list[idx]

        # Draw the pose landmarks.
        hand_landmarks_proto = landmark_pb2.NormalizedLandmarkList()
        hand_landmarks_proto.landmark.extend([
            landmark_pb2.NormalizedLandmark(x=landmark.x, y=landmark.y, z=landmark.z) for landmark in hand_landmarks
        ])
        solutions.drawing_utils.draw_landmarks(
            annotated_image,
            hand_landmarks_proto,
            solutions.hands.HAND_CONNECTIONS,
            solutions.drawing_styles.get_default_hand_landmarks_style())
    return annotated_image


pose_options = PoseLandmarkerOptions(
    base_options=BaseOptions(model_asset_buffer=pose_model_buffer),
    running_mode=VisionRunningMode.VIDEO,
    num_poses=3)
hand_options = GestureRecognizerOptions(
    base_options=BaseOptions(model_asset_buffer=gesture_model_buffer),
    running_mode=VisionRunningMode.VIDEO,
    num_hands=6)

cap = cv2.VideoCapture(0)
frame_id = 0
with PoseLandmarker.create_from_options(pose_options) as pose_landmarker, GestureRecognizer.create_from_options(hand_options) as recognizer:
    while True:
        ret, frame = cap.read()
        if (not ret):
            break

        rgb_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb_frame)
        
        pose_result = pose_landmarker.detect_for_video(mp_image, frame_id)
        gesture_result = recognizer.recognize_for_video(mp_image, frame_id)
        frame_id += 1
        if (not args.not_print_debug_image):
            result_frame = draw_pose_landmarks_on_image(frame, pose_result)
            result_frame = draw_hand_landmarks_on_image(result_frame, gesture_result)

            gesture_text = ""
            if gesture_result.gestures:
                for i, hand_gestures in enumerate(gesture_result.gestures):
                    if hand_gestures:
                        # Get the top gesture from the list.
                        top_gesture = hand_gestures[0]
                        gesture_text += f"Hand {i}: {top_gesture.category_name} ({top_gesture.score:.2f})  "
            cv2.putText(result_frame, f"{gesture_text}", (10, 30), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 0, 255), 1, cv2.LINE_AA)
        else:
            result_frame = frame

        if (not args.not_show_image): 
            cv2.imshow('Landmarker', result_frame)

        height, width = frame.shape[:2]
        send_image(result_frame)
        send_data(pose_result, gesture_result, frame_id, height, width)

        if (cv2.waitKey(1) & 0xFF == ord('q')):
            break
cap.release()
cv2.destroyAllWindows()