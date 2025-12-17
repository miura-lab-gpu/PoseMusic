import cv2
import mediapipe as mp
import numpy as np
from pathlib import Path
from mediapipe.framework.formats import landmark_pb2

model_path = r"./Model/pose_landmarker_full.task"

BaseOptions = mp.tasks.BaseOptions
PoseLandmarker = mp.tasks.vision.PoseLandmarker
PoseLandmarkerOptions = mp.tasks.vision.PoseLandmarkerOptions
PoseLandmarkerResult = mp.tasks.vision.PoseLandmarkerResult
VisionRunningMode = mp.tasks.vision.RunningMode

mp_pose = mp.solutions.pose
pose = mp_pose.Pose()
mp_draw = mp.solutions.drawing_utils

with open(model_path, "rb") as f:
    model_buffer = f.read()

def draw_landmarks_on_image(rgb_image, detection_result):
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
        mp.solutions.drawing_utils.draw_landmarks(
        annotated_image,
        pose_landmarks_proto,
        mp.solutions.pose.POSE_CONNECTIONS,
        mp.solutions.drawing_styles.get_default_pose_landmarks_style())
    return annotated_image

result_frame = None
def print_result(result: PoseLandmarkerResult, output_image: mp.Image, timestamp_ms: int):
    global result_frame
    result_frame = draw_landmarks_on_image(output_image.numpy_view(), result)

options = PoseLandmarkerOptions(
    base_options=BaseOptions(model_asset_buffer=model_buffer),
    running_mode=VisionRunningMode.LIVE_STREAM,
    result_callback=print_result,
    num_poses=3)

cap = cv2.VideoCapture(0)
with PoseLandmarker.create_from_options(options) as landmarker:
    while True:
        ret, frame = cap.read()
        if (not ret):
            break

        rgb_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb_frame)

        landmarker.detect_async(mp_image, int(cap.get(cv2.CAP_PROP_POS_MSEC)))

        if result_frame is not None:
            cv2.imshow('Pose Landmarker', cv2.cvtColor(result_frame, cv2.COLOR_RGB2BGR))
        if (cv2.waitKey(100) & 0xFF == ord('q')):
            break
cap.release()
cv2.destroyAllWindows()