# DDAUpdated
Updated version of DDA -- Dim Display After settings

Full credits to the author of the original exe (mk77ch "Mike") at http://www.surfaceforums.net/threads/wheres-the-dim-display-after-power-setting-in-windows-8-1.6577/. Decompiled using ILSpy, and then I made a number of changes to the app to make it more suitable for use on modern OLED displays.

To run as startup create an admin task in Task Scheduler, see https://superuser.com/questions/770420/schedule-a-task-with-admin-privileges-without-a-user-prompt-in-windows-7

Why does this app exist: 
 - Since Windows 8 Microsoft has apparently put a "design limitation" to prevent users on Modern Standby systems from setting a custom timeout to dim the screen, instead forcefully setting the screen to dim 15s before the "Turn the screen off" option.
 - This is a problem for OLED display users as it has been shown that dimming the screen on OLED displays can significantly improve the longevity of the panel. https://www.rtings.com/tv/learn/real-life-oled-burn-in-test.
 - The "easy" solution would be to disable Modern Standby and revert the system to regular S3 ACPI sleep. However newer laptops (Intel 11th Gen / AMD Ryzen 6000 series or newer) have started outright removing S3 support from the platform firmware! https://www.reddit.com/r/Dell/comments/h0r56s/getting_back_s3_sleep_and_disabling_modern/ https://www.reddit.com/r/Fedora/comments/qmle9b/whats_with_s3_sleep_behavior_on_tigerlake/
 - Hence, this project aims to "restore" the dimming settings in a battery / CPU / memory efficient manner.

**NEW** This app also detects when the cursor is NOT on the OLED display and automatically dims the OLED display after the set time. To use this feature make sure your primary display is the OLED display you want to dim (should be the default for laptop users).

TODO:
 - fix "Start with Windows" option (auto starting a Windows app as admin cannot be done via the usual registry edits, you need to run a batch script that points to the app)
 - find a more memory efficient way to query powercfg /requests (currently the app opens a powershell instance in the background, maybe worth investigating if there is a native WMI for it)
 - retain option to set brightness manually
 - better icon
 - maybe incorporate a taskbar dimmer, could take some inspiration from https://github.com/thomasverelst/PressureSpotFixer
 - DONE (partially) make it monitor independent (record events only if on particular monitor), not sure if possible or easy in Windows (native is LASTINPUTINFO). This does not matter too much because WMI Brightness only deals with the internal laptop monitor and not external monitors (even over DDC) but would be cleaner
	- TODO allow the user to select the OLED monitor instead of assuming the primary monitor is the OLED monitor (non trivial!! Windows may not have an API to map e.g. .\\DISPLAY1\ to a particular monitors HardwareID)