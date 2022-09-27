# DDAUpdated
Updated version of DDA

Original exe from mk77ch "Mike" at http://www.surfaceforums.net/threads/wheres-the-dim-display-after-power-setting-in-windows-8-1.6577/

Decompiled using ILSpy

TODO:
 - fix "Start with Windows" option (auto starting a Windows app as admin cannot be done via the usual registry edits, you need to run a batch script that points to the app)
 - find a more memory efficient way to query powercfg /requests (currently the app opens a powershell instance in the background, maybe worth investigating if there is a native WMI for it)
 - retain option to set brightness manually
 - better icon
 - make it monitor independent (record events only if on particular monitor), not sure if possible or easy in Windows (native is LASTINPUTINFO). This does not matter too much because WMI Brightness only deals with the internal laptop monitor and not external monitors (even over DDC) but would be cleaner