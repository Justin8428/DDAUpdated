# DDAUpdated
Updated version of DDA

Original exe from mk77ch "Mike" at http://www.surfaceforums.net/threads/wheres-the-dim-display-after-power-setting-in-windows-8-1.6577/

Decompiled using ILSpy

TODO:
 - disable the dimming when the user is playing a video. You can obtain whether the display is active via powercfg /requests (effectively reading SetThreadExecutionState(ES_DISPLAY_REQUIRED)) but you need admin permissions to do so...
	 - either make the app as admin in the background all the time (bad!) or run it as a service https://stackoverflow.com/questions/36826733/possible-to-find-all-windows-processes-preventing-automatic-sleep-w-o-admin-rig
	  - apparently this is an unimplemented feature in windows
 - retain option to set brightness manually
 - better icon
 - make it monitor independent (record events only if on particular monitor), not sure if possible or easy in Windows (native is LASTINPUTINFO)