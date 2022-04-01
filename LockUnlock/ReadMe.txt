La documentazione in italiano è nella cartella della libreria

------------------------------------------------------------------


[Troubleshooting] 

To run the application the LockUnlock library must be compiled as a 64bit release.
Otherwise, there will be an error.

------------------------------------------------------------------


[General information]

(key logger) To prevent the user from pressing buttons for special combinations [ex control + alt + ...], we use the KeyboardListener class.
This listens to the key pressed by the user. 
If the key is special, the event is not propagated to the operating system. (it stops)
To insert the key log just create a variable for example: keyboardListener = new KeyboardListener();
Then to disable it call the method keyboardListener.Dispose();
The disable key list is set in DefaultKeysToAvoid. It can be overwritten if the user needs even more control

*********************

(task-bar) It is impossible to disable the task bar in windows because it is part of the user interface.
As a hack, you can hide the taskbar and prevent the user from entering with the mouse. 

*********************

(devcon) devcon.exe is a Microsoft tool to manage devices. It must be placed in the current application folder (bin/debug/devcon.exe)
for the shell to execute the script.

*********************

