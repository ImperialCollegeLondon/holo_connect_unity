﻿# Hololens PRL holo_connect_unity
---------------------------------


## HoloToolkit
--------------

You can find this software [here](https://github.com/Microsoft/MixedRealityToolkit-Unity).

Follow steps 1 to 3 from [here](https://github.com/Microsoft/MixedRealityToolkit-Unity/blob/master/GettingStarted.md) including the optional section in step 2:

* Open the folder you just cloned in Unity. Now, inside of Unity ensure you have the Assets folder selected in the project view, and export the package.

**IMPORTANT**: Make sure you select the root Assets folder in the Project. It contains important .rsp files like csc, gmcs and smcs.

* `Assets -> Export Package…`
* Name it HoloToolkit

* Open our project in Unity
* Then import the HoloToolkit asset using `Assets -> Import Package -> Custom Package…` [Navigate to the package you have exported above].

**NOTE**: The HoloToolkit-Examples and HoloToolkit-Test folders (and all its content and subfolders) are optional when you import the custom package. Uncheck those folders in the Import Unity Package window that shows all the contents of the package before performing the import (see image below).

![](Images/ImportPackage.PNG)



## Deploy the App
-----------------

### Export to the Visual Studio solution
----------------------------------------

1. Open File > Build Settings window
2. Click Add Open Scenes to add the scenes.

    **IMPORTANT**: Make sure Scenes/wheelChairPrime scene is selected.

![](Images/Deploy_App_Wheelchair.PNG)


3. Change Platform to Universal Windows Platform and click Switch Platform.
4. In Windows Store settings ensure, SDK is Universal 10.
5. For Target device switch to HoloLens.
6. UWP Build Type should be D3D.
7. UWP SDK could be left at Latest installed.
8. Check Unity C# Projects and Development Build under Debugging.
9. Click Build.
10. In the file explorer, click New Folder and name the folder "App".
11. With the App folder selected, click the Select Folder button.
12. When Unity is done building, a Windows File Explorer window will appear.
13. Open the App folder in file explorer.
14. Open the generated Visual Studio solution (Gaze.sln in this case)

### Compile the Visual Studio solution
--------------------------------------

Finally, we will compile the exported Visual Studio solution, deploy it, and try it out on the device.

1. Using the top toolbar in Visual Studio, change the target from Debug to Release and from ARM to X86.
2. Click on the arrow next to the Local Machine button, and change the deployment target to Remote Machine.
3. Enter the IP address of your mixed reality device and change Authentication Mode to Universal (Unencrypted Protocol).
4. Click Debug > Start without debugging.

If this is your first time deploying to the Hololens, you will need to pair using [Visual Studio](https://docs.microsoft.com/en-us/windows/mixed-reality/using-visual-studio).

### Try out the app
-------------------

**TODO
