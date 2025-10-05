# PapayaEditorModdingTools

## Usage
Lightweight modding tools for Unity games. (Mostly 2D but I have successfully modded 3D before; I just don't have the time to implement the scripts, maybe will do in the future)

English tutorial isn't available yet. You can see the current ones on Bilibili (Mandarin). Notice that this repo includes all tools presented in the videos. You may follow the instruction in the videos to download them from BaiduNetDisk as well. However, I still recommend you to install them via UPM for a simpler update workflow.

* 【01 手把手带你解决像素空间不够的问题 （持续更新）| 战魂铭人】
(Texture replacement with size not being limited to the original border)
 https://www.bilibili.com/video/BV1oTN7zuEm3/?share_source=copy_web&vd_source=7386bc2c7dbfc678277dc2383823cbbb

* 【02 无自动图集的扩容教程（持续更新）| 战魂铭人】(The case without SpriteAtlas) https://www.bilibili.com/video/BV1d3N7ziEPr/?share_source=copy_web&vd_source=7386bc2c7dbfc678277dc2383823cbbb

* 【03 有自动图集的扩容教程 - 情况一（持续更新）| 战魂铭人】(The case with SpriteAtlas) https://www.bilibili.com/video/BV1ySKFz6Ekg/?share_source=copy_web&vd_source=7386bc2c7dbfc678277dc2383823cbbb

* 【04 依赖项图集的扩容教程 - 情况二（持续更新）| 战魂铭人】(SpriteAtlas + dependency) https://www.bilibili.com/video/BV1UQKBzXEFi/?share_source=copy_web&vd_source=7386bc2c7dbfc678277dc2383823cbbb

* 【【教学】在元气和战魂中自定义背景音乐】(Customize background music or audio in general) https://www.bilibili.com/video/BV1tNeXz8Ehf/?share_source=copy_web&vd_source=7386bc2c7dbfc678277dc2383823cbbb

* 【【教学】05 多图集切框】(Spritesheet slicing in bundles with multiple SpriteAtlas) https://www.bilibili.com/video/BV1a7nxzbEXy/?share_source=copy_web

## Installation
You can install this package via UPM (Unity Package Manager). **I highly recommend you to start with a fresh Unity project.**

First, open UPM.

![](/Documentation~/ins1.png)

Second, click the plus sign in the top-left corner and choose "Add package from git URL".

![](/Documentation~/ins2.png)

Third, fill in the following and click "Add".

```
https://github.com/Kolyn090/PapayaEditorModdingTools.git
```

The next step is tricky. Find folder "Papaya Editor Modding Tools" under Packages.

![](/Documentation~/ins3.png)

Find "CopyEditorTools.cs" under "Editor".

![](/Documentation~/ins4.png)

Create a new folder under "Assets", name it "Editor". Copy and paste "CopyEditorTool.cs" in this folder. You should see a new menu option "Tools" in the top tool bar after you did this.

![](/Documentation~/ins5.png)

In the top tool bar, find "Tools/Update Papaya Editor Modding Tools" and click it.

![](/Documentation~/ins6.png)

The installation has been completed. Check if you have the following:

![](/Documentation~/ins7.png)

![](/Documentation~/ins8.png)

## Dependencies
Dependencies will be automatically downloaded to your Unity project by UPM.

The dependencies used by this project:

* Newtonsoft Json
* Sprite2D

You also very likely need [UABEA](https://github.com/nesrak1/UABEA/releases/tag/v8) (software) to make actual modifications.

## Uninstallation
To uninstall this project, go to UPM and select this package, then click "Remove". Also remove all relevant Editor scripts if you need to.

## Acknowledgement
© 2025 [Kolyn090].  
This code is licensed under the MIT License.  
You are free to use and modify it under the terms of the license, but attribution is required.
