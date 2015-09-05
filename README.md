# Navi

**Navi** is a new innovative input solution for Virtual Reality. Use your tablet or smartphone as your controller in your virtual environment. Potential use cases include pairing with a GearVR, Google Cardboard with straps, or even an Oculus Rift DK2. While I think that this usage of smart devices is great for VR, this solution is generic enough to work with PC based games. 

**Navi** is a spin-off of my prototype that I wrote for the Google Project Tango contest. It is written in Unity 5.1.3 and uses the Unity 5.1.3 Transport API to transfer rotational (and positional, if supported in the case of Project Tango Dev Kit) data from a smart device to your PC. In addition, it also supports sending touch data. All the code is licensed under GPLv3.

## Try it out

If you have an Oculus DK2 and an Android powered device (iOS coming shortly - if you are feeling up to it, make a build by compiling the NaviMobile Unity project for iOS), download the demos on to your PC and install [the Navi App](https://play.google.com/store/apps/details?id=com.navi.io) from Google Play onto your Android device. Currently, I have developed two demos: [Pirate Defense](https://github.com/vmohan7/NaviPirateDemo) and [Jelly Fling](https://github.com/vmohan7/NaviSpaceDemo) (Download links for the .exe are located in the README of each respective github page). Make sure that both devices are connected to the same wireless network and they will automatically pair up. 

Here is a short video I made that steps through the process of setting it up: 
[https://www.youtube.com/watch?v=32SZAMq16QY](
https://www.youtube.com/watch?v=32SZAMq16QY)

## Quick Start

> 1. The NaviSDK folder is a Unity 5.1.3 project. After forking the project, you can open the project in Unity 5.1.3 or greater (both free and pro versions should work). 
> 2. Once you have the project open in Unity, open PCMainScene located in Assets/Scenes
> 3. Download & Install [the Navi App](https://play.google.com/store/apps/details?id=com.navi.io) from Google Play onto your Android Device.
> 4. Run the Navi App and Run the scene in Unity. The app in Unity should run in the Game View as well as in a DK2 if connected. Assuming both devices are on the Wi-Fi network, the searching instruction should disappear from view on the Navi app. The Unity app should start walking you through how to use your device to control a virtual object.
> 5. Congratualations! You have succesfully paired the devices and can now begin development. 
> 6. To develop your own content, create a new scene and add it to the build settings. Rerun the PCMainScene, it should now load your scene once you complete all the instructions (or whenever you tap with 5 fingers to reset).

## Documentation

All the code has been commented in the XML format recommended for C#. I am hoping to put some of that information on [the Wiki](https://github.com/vmohan7/Navi/wiki) as well as more generic documentation on how setup works (i.e. [how the networking is implemented](https://github.com/vmohan7/Navi/wiki/High-Level-Overview-of-Network-Handshake-between-SDK-and-Mobile-App)).  Feel free to add any information that you think is missing to the wiki. 

## Contributing

We welcome pull requests from everyone in the community. Pick an issue nobody is working and go for it! If you find an issue, go ahead and report it or better yet, if you think you can fix it, be our guest :) . When your pull request gets merged into the project, we will add you name to the contributors list. Feel free to join the [Google Group](https://groups.google.com/forum/#!forum/navi-io) to discuss the possibilites of this input solution.

## Coming Soon

- Upcoming features list
- iOS build
- Website

