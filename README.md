<p align="center">
  <img src="preview.png" width="300"/>
</p>
<h1 align="center">Last Day Lost Camera Fix</h1>

[![GitHub Repository](https://img.shields.io/badge/GitHub-Repository-blue?style=for-the-badge&logo=github)](https://github.com/artkopt/Last_Day_Lost_Camera_Fix.git)

[![Steam Subscriptions](https://img.shields.io/steam/subscriptions/3435908403?style=for-the-badge&logo=steam)](https://steamcommunity.com/sharedfiles/filedetails/?id=3435908403)
[![Steam Views](https://img.shields.io/steam/views/3435908403?style=for-the-badge&logo=steam)](https://steamcommunity.com/sharedfiles/filedetails/?id=3435908403)
[![Steam Favorites](https://img.shields.io/steam/favorites/3435908403?style=for-the-badge&logo=steam)](https://steamcommunity.com/sharedfiles/filedetails/?id=3435908403)
[![Steam Updated](https://img.shields.io/steam/update-date/3435908403?style=for-the-badge&logo=steam)](https://steamcommunity.com/sharedfiles/filedetails/?id=3435908403)

[![Thunderstore Downloads](https://img.shields.io/thunderstore/dt/artkopt/LastDayLostCameraFix?style=for-the-badge&logo=thunderstore&label=Downloads)](https://thunderstore.io/c/content-warning/p/artkopt/LastDayLostCameraFix/)
[![Thunderstore Version](https://img.shields.io/thunderstore/v/artkopt/LastDayLostCameraFix?style=for-the-badge&logo=thunderstore&label=Version)](https://thunderstore.io/c/content-warning/p/artkopt/LastDayLostCameraFix/)

[![License: Apache 2.0](https://img.shields.io/badge/License-Apache%202.0-blue?style=for-the-badge)](LICENSE-APACHE)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue?style=for-the-badge)](LICENSE-MIT)

## Description
In the game `Content Warning` on version `1.19.e` there is a bug that appears on the third day of the quota. If you leave the camera on the floor of the bell in the `Underground` scene and start climbing to the surface, the game will mistakenly count the quota as a defeat, even if the camera is still in the bell.

## The cause of the bug:
- The condition for checking the presence of a camera in the bell was triggered before the `Surface` scene was loaded, which led to a false detection of the absence of a camera.

## Solution:
The mod adds a delay before checking for a camera to make sure that the `Surface` scene is fully loaded. The [UnityMainThreadDispatcher](https://github.com/PimDeWitte/UnityMainThreadDispatcher) library is used for this, which allows you to perform a check in the main stream of the game after the loading of the scene is completed.

## Licenses
This project uses the following licenses:
- **Apache License 2.0**: Applies to code taken from the [UnityMainThreadDispatcher](https://github.com/PimDeWitte/UnityMainThreadDispatcher) repository.
- **MIT License**: Applies to the original code of the mod.

## Support
If you have any problems, questions, or suggestions for improving the code, create an [issue on GitHub](https://github.com/artkopt/Last_Day_Lost_Camera_Fix/issues).

---

# Guide to Building a Mod for the Game Content Warning

This guide is suitable for any IDE or even for building via the command line. You can use `Visual Studio`, `JetBrains Rider`, `VS Code`, or any other tool that supports working with `.NET projects`.

## 1. Installing Required Software
Before starting, make sure you have installed:
- `.NET SDK` (a version that supports `netstandard2.1`).
- `Git` (for cloning the repository).
- Any text editor or IDE (e.g., `Visual Studio`, `JetBrains Rider`, `VS Code`, etc.).

## 2. Cloning the Repository
Open a terminal and run the following command to clone the repository:
```shell
git clone https://github.com/artkopt/Last_Day_Lost_Camera_Fix.git
```

## 3. Setting the Game Path
Navigate to the project folder:
```shell
cd Last_Day_Lost_Camera_Fix
```
Open the file `LastDayLostCameraFix.csproj` in any text editor.  
Find the line with the `<CWDir>` parameter and change the path to where your `Content Warning` game is installed:
```shell
<CWDir Condition=" '$(CWDir)' == '' ">D:\SteamLibrary\steamapps\common\Content Warning</CWDir>
```
Replace `D:\SteamLibrary\steamapps\common\Content Warning` with the path to your game installation.

## 4. Restoring Dependencies
Before building the project, you need to restore the dependencies. To do this, run in the terminal:
```shell
dotnet restore
```

## 5. Building the Project
To build the project, run the command:
```shell
dotnet build
```

## 6. Copying Files to the Game Folder
After building, the file `LastDayLostCameraFix.dll` will automatically be copied to the `Plugins` folder of the game, specified in the `<CWDir>` parameter. If the copying did not occur, check the path in `LastDayLostCameraFix.csproj` and make sure the `Plugins` folder exists.