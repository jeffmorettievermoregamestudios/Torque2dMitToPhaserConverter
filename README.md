# Torque2dMitToPhaserConverter

Current Version:  v0.0.5

CHANGELOG:

v0.0.5
- fixes for MathConvertUtil / Sprite Positioning / X and Y logic
- now inverts the values for the 'depth' of sprites (when converting from T2D to Phaser).  This properly maps
the depth / layering of sprites

CHANGELOG:

v0.0.4
- stability fixes for loading/starting a scene and loading/displaying sprites
- 'alpha' support for TextSprite/BitmapText now available
- bugfix that was causing an 'assets' folder to be created in the root folder of the drive when
converting a project




The following guide/tips have been taken directly from http://www.torque2dmittophaserconverter.com/tips.html

<h3><b>TIPS ON USING THE TORQUE2D MIT TO PHASER CONVERTER</b></h3>

<p>
The following tips will help guide you with getting started with the Torque2dMitToPhaserConverter.
</p>

<br />

<p><b>1. DOWNLOAD VISUAL STUDIO</b></p>

<p>Visit <a href="https://visualstudio.microsoft.com">https://visualstudio.microsoft.com</a> to install Visual Studio on your machine
if you haven't already.  Note that I recommend Visual Studio Community 2019 for anyone using a Windows environment, and Visual Studio
Code for any other environment (ie MacOS, Linux).
</p>
<p>Note that while you are installing Visual Studio, you will want to make sure the ".NET desktop development" option (aka
module/workload) is selected.</p>
<p>If you are new to .NET Framework / C# development, you may need to go through some tutorials and learn some things.  You may
want to try this URL as a starting point for learning how to work with .NET Applications in C#:</p>
<p><a href="https://docs.microsoft.com/en-us/visualstudio/ide/step-1-create-a-windows-forms-application-project?view=vs-2019">
https://docs.microsoft.com/en-us/visualstudio/ide/step-1-create-a-windows-forms-application-project?view=vs-2019
</a></p>

<br />

<p><b>2. DOWNLOAD (OR GIT CLONE) PHASER</b></p>
<p>
You will need Phaser in order for your output project from Torque2dMitToPhaserConverter to work.  Find out more about Phaser here:
</p>
<p><a href="https://phaser.io/">https://phaser.io</a></p>

<br />

<p><b>3. NAVIGATE TO THE CORRECT FOLDER FOR THE TORQUE 2D MIT PROJECT SOURCE (IE THE 'modules' FOLDER)</b></p>
<p>
It is important that when you run the Torque2dMitToPhaserConverter that you pick the correct source folder for the
Torque2D MIT project.  You want to specifically pick the 'modules' folder.  In other words, pick the modules folder,
the one that will contain the 'AppCore' folder, and likely one (or maybe multiple) other folder(s) that contains
the actual video game you are developing (ie in my case, the 'PuzzleGalaxies' folder).
</p>

<br />

<p><b>4. INCLUDE THE phaser.js FILE INTO THE ROOT FOLDER OF YOUR OUTPUT DIRECTORY</b></p>
<p>
After you have successfully converted a project, you should see in your 'output' folder a few folders
(ie 'assets', 'scripts', 'util', etc) along with some files (ie 'index.html', etc).  In this folder,
you need to copy into it the 'phaser.js' file from the Phaser repo (should be located in the 'dist' folder).
Without this file, the Phaser project will not function correctly.
</p>

<br />

<p><b>5. PLACE THE ITEMS IN preload.js FILE INTO THEIR RESPECTIVE SCENE preload() METHOD(S)</b></p>
<p>
The Torque2dMitToPhaserConverter cannot determine where to place the preload items by itself.  You will
need to copy/paste the items generated in preload.js into their respective Scene 'preload' method(s).
As you would imagine, the items that should go into the preload method of a scene are the ones that are
associated with the assets that the respective scene requires.
</p>

<br />

<p><b>6. THE ORDER OF THE SCENES IN THE index.html FILE MATTERS</b></p>
<p>
From what I have seen so far, it seems the order of the scenes listed in the Phaser.Game 'config' matter.  You
may want to try re-ordering the scenes (ie in the order in which they execute) to try to get your Phaser output project
to work.  If this still doesn't work, then consider removing all scenes except one and seeing if this works and then
add the others back in.
</p>

<br />

<p><b>7. THE PleaseWait FORM CAN BE DISABLED IF YOU WISH</b></p>
<p>
Sometimes the 'Please Wait' form that displays while the Torque2dMitToPhaserConverter is converting a project is
undesirable.  If you want to disable it, set the 'enablePleaseWaitDialog' boolean to false in the 
FormTorque2dToPhaserConverter class file.
</p>

<br />

<p><b>8. SET THE enabled PROPERTY OF SCENE TO 'true' IN ORDER TO ENABLE/TURN ON A SCENE</b></p>
<p>
Since Scenes that are output by the Torque2dMitToPhaserConverter derive themselves from the SceneBaseClass,
you will want to turn on/off scenes by setting the 'enabled' property to true (ie when you actually want
a scene to be displayed).  You will need to modify your output project (manually) in order to implement this.
</p>

<br />

<p><b>9. THE Torque2dMitToPhaserConverter PROJECT IS IN AN 'ALPHA' STAGE OF DEVELOPMENT.  DO NOT EXPECT IT TO FULLY
CONVERT YOUR PROJECT.</b></p>
<p>
Finally, you cannot expect your project to be fully converted and work first try after converting it and setting it up on your
local web server.  The Torque2dMitToPhaserConverter is not really a complete project and it will take some more time and development
before it is able to convert 'first try'.
</p>

<br />

<p><b>10. FOLLOW THE PHASER GUIDE ABOUT SETTING UP A (LOCAL) WEB SERVER.  THIS IS SOMETHING YOU NEED TO DO!</b></p>
<p>
Setting up a web server to run your Phaser project is required!  It is tempting to simply open your web project up by
double-clicking the index.html file (ie opening it from the file system) and thus opening it in Firefox/Chrome/Edge/etc to 
see how it runs, but it won't run properly.  Phaser requires that you run your project on a webserver (ie http://localhost) 
in order to run properly.  I used the recommended 'WAMP Server' that Phaser recommends, and it worked nicely for me (but feel
free to try other web server options if you wish).
</p>

<br />

<p>Hopefully the tips above will answer some of the questions you may have when running the Torque2dMitToPhaserConverter.
However, if you still need more help feel free to email me at <span style="text-decoration: underline;">jeffmoretti@evermoregamestudios.com</span>.
</p>
