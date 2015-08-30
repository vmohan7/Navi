# Navi

**Navi** is an innovative input solution for Virtual Reality. Instead of strapping a phone to your face, why not use your smart device as a way to interact with a virtual environment?

**Navi** is a spin-off of my prototype that I wrote for the Google Project Tango contest. It is written in Unity 5.1 uses the Unity 5 Transport API to transfer rotational (and positional, if supported in the case of Project Tango Dev Kit) data from the smart device to your PC. In addition, it also supports sending touch data. All the code is licensed under the GPLv3

## Try it out

If you have an Oculus DK2 and an Android powered device (iOS coming shortly), download the demos (source code coming soon) that are a part of this repository on to your PC and install [the Navi App](https://play.google.com/store/apps/details?id=com.navi.io) from Google Play onto your Android device. Make sure that both devices are connected to the same wireless network and they will automatically pair up. 

## Quick Start

> 1. The NaviSDK folder is a Unity 5.1 project. After forking the project, you can open the project in Unity (both free and pro versions should work). 
> 2. Once you have the project open in Unity, open PCMainScene located in Assets/Scenes
> 3. Download & Install [the Navi App](https://play.google.com/store/apps/details?id=com.navi.io) from Google Play onto your Android Device.
> 4. Run the Navi App and Run the scene in Unity. The app in Unity should run in the Game View as well as in a DK2 if connected. Assuming both devices are on the Wi-Fi network, the searching instruction should disappear from view on the Navi app. The Unity app should start walking you through how to use your device to control a virtual object.
> 5. Congratualations! You have succesfully paired the devices and can now begin development. 
> 6. To develop your own content, create a new scene and add it to the build settings. Rerun the PCMainScene, it should now load your scene once you complete all the instructions (or whenever you tap with 5 fingers to reset).

## Contributing

We welcome pull requests from everyone in the community. Pick an issue nobody is working and go for it! If you find an issue, go ahead and report it or better yet, if you think you can fix it, be my guest :) . When your pull request gets merged into the project, we will add you name to the contributors list.

