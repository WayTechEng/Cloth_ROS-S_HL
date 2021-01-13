# Augmented Reality with Haptic Feedback and Virtual Barriers for improved HRI - Virtual Barrier system

## Introduction
The repository contains the code used by Steven Lay and Steven Hoang for their Final Year Projects. .        .

## Prerequisites 
* [Unity 2019.4 LTS](https://unity.com/releases/2019-lts)
* [Microsoft Visual Studio 2019](https://visualstudio.microsoft.com/vs/)
* [Windows SDK 18362 or higher](https://developer.microsoft.com/en-us/windows/downloads/sdk-archive/)

## Unity HoloLens build instructions
1. Clone the repository.
2. Checkout to rossharp branch
3. Open the project inside Unity.
4. Open the Panda_Scene scene inside the Assets/Scenes folder.
5. Get the IP address of the ROS machine connecting with the robot
6. Modify the IP address in ROSConnector GameObject properties. Leave the socket field untouched
7. Next, go to File -> Build Settings. 
8. Switch to Universal Windows Platform. Set Architecture to x86 if building for HoloLens 1, ARM64 if building for HoloLens 2.
9. Hit Build. Save it to a folder or leave it in the App Folder.
10. Head into the folder you saved the built solution to, and open the VS Solution.
11. Change the Solution Configuration to Release, Solution platform to x86 if building for HoloLens 1, ARM64 if building for HoloLens 2.
12. Set Device to Device (Ensure Hololens is connected via USB to your Computer).
13. Ensure the Hololens is not asleep/turned off, and then hit the 'Play' button beside the Device tab.
14. Wait for the solution to build and it should be deployed and launched on your HoloLens when it's ready.
