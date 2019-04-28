# UnityARKitRecorder
You can record the face tracking data with ARKit remote.

## Install

1. Clone or download [Unity-ARKit-Plugin](https://bitbucket.org/Unity-Technologies/unity-arkit-plugin/) <img align="right" width="200" alt="UnityARKitRecorder_01" src="https://user-images.githubusercontent.com/2757441/56860350-bf977900-69d0-11e9-93d2-a7ed2d54dad4.png">
1. Put UnityARKitPlugin folder in it to this Assets folder. 
1. Need ARKitRemote. If you don't have it yet, you should build UnityARKitPlugin/ARKitRemote/UnityARKitRemote and install into your iPhone.

## How to use

### 1. Record

1. Open ARKitRecorder scene in ARKitRecorder/Scenes/ <img align="right" width="250" alt="Unity_2018_3_0f2_-_ARKitRecorder_unity_-_UnityARKitRecorder_-_iPhone__iPod_Touch_and_iPad__Metal_" src="https://user-images.githubusercontent.com/2757441/56860771-26b72c80-69d5-11e9-9f8e-c03c98131f97.png">
1. Click UnityARKitRecorder game object and see inspector. 
1. Change recording mode to 'RECORDING'.
1. Connect iPhoneX/XS to your Mac with USB.
1. Start ARKitRemote on your iPhoneX/XS. 
1. Select 'iPhonePlayer' on Console window Editor tab. <img align="right" width="350" alt="スクリーンショット_2019-04-28_16_27_51-2" src="https://user-images.githubusercontent.com/2757441/56860848-d55b6d00-69d5-11e9-9790-4eb460811f36.png">
1. Play on the Unity scene. 
1. Click 'Start Remote ARKit FaceTracking Session' Button. 
1. Record for your face tracking. then stop it.
1. You can see the file 'faceTrackingData.bytes' on the top of the project folder.

### 2. Play

1. Change recording mode to 'PLAYING' of UnityARKitRecorder game object.<a href="https://user-images.githubusercontent.com/2757441/56860961-228c0e80-69d7-11e9-9654-ead579b26c0a.gif"><img src="https://user-images.githubusercontent.com/2757441/56860961-228c0e80-69d7-11e9-9654-ead579b26c0a.gif" alt="Image from Gyazo" width="150" align="right"/></a>
1. Play on the Unity scene. 
1. You can see the recorded video and tracking data. 
