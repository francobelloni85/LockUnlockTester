[Troubleshooting] 

In order to run the application the LockUnlock library must be compiled as 64bit release.
Otherwise where will be an error.


------------------------------------------------------------------


[General information]

To prevent the user from pressing buttons for special combinations [ex control + alt + ...], we use the KeyboardListener class.
This listens the key pressed by the user. 
If the key is a special key, the event is not propagated to the operating system. (it stops)

*********************

It is impossible to disable the task-bar in windows because it is part of the user interface.
As a hack you can hide the taskbar and prevent the user from entering with the mouse. 