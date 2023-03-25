﻿using DDA.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DDA;


public partial class SettingsForm : Form
{
    private struct LASTINPUTINFO
    {
        public static readonly int SizeOf = Marshal.SizeOf(typeof(LASTINPUTINFO));

        [MarshalAs(UnmanagedType.U4)]
        public uint cbSize;

        [MarshalAs(UnmanagedType.U4)]
        public uint dwTime;
    }

    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii); // native input detection
    private TimeSpan GetIdleTime()
    {
        LASTINPUTINFO plii = default(LASTINPUTINFO);
        plii.cbSize = (uint)LASTINPUTINFO.SizeOf;
        GetLastInputInfo(ref plii);
        int num = Environment.TickCount - (int)plii.dwTime;
        if (num > 0)
        {
            return new TimeSpan(0, 0, 0, 0, num);
        }
        return new TimeSpan(0L);
    }

    private int GetBrightness() // get current brightness
    {
        int result = -1;
        try
        {
            ManagementScope scope = new ManagementScope("root\\WMI");
            SelectQuery query = new SelectQuery("WmiMonitorBrightness");
            ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher(scope, query);
            ManagementObjectCollection managementObjectCollection = managementObjectSearcher.Get();
            using (ManagementObjectCollection.ManagementObjectEnumerator managementObjectEnumerator = managementObjectCollection.GetEnumerator())
            {
                if (managementObjectEnumerator.MoveNext())
                {
                    ManagementObject managementObject = (ManagementObject)managementObjectEnumerator.Current;
                    result = Convert.ToInt32(managementObject.GetPropertyValue("CurrentBrightness"));
                }
            }
            managementObjectCollection.Dispose();
            managementObjectSearcher.Dispose();
        }
        catch (ManagementException ex)
        {
            Console.Write(ex.Message);
        }
        return result;
    }

    private void SetBrightness(int targetBrightness)
    {
        try
        {
            ManagementScope scope = new ManagementScope("root\\WMI");
            SelectQuery query = new SelectQuery("WmiMonitorBrightnessMethods");
            using ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher(scope, query);
            using ManagementObjectCollection managementObjectCollection = managementObjectSearcher.Get();
            using ManagementObjectCollection.ManagementObjectEnumerator managementObjectEnumerator = managementObjectCollection.GetEnumerator();
            if (managementObjectEnumerator.MoveNext())
            {
                ManagementObject managementObject = (ManagementObject)managementObjectEnumerator.Current;
                managementObject.InvokeMethod("WmiSetBrightness", new object[2]
                {
                    1,
                    (byte)targetBrightness
                });
            }
        }
        catch (ManagementException ex)
        {
            Console.Write(ex.Message);
        }
    }

    private bool IsPlayingVideo() // determine if the display needs to be kept awake, e.g. playing a video
    {   // also see https://stackoverflow.com/questions/206323/how-to-execute-command-line-in-c-get-std-out-results 
        // and https://stackoverflow.com/questions/33654318/c-sharp-run-powershell-command-get-output-as-it-arrives
        // underlying: https://docs.microsoft.com/en-us/windows/win32/power/system-sleep-criteria
        // essentially, run the powershell script to see if DISPLAY is active, return true if so, otherwise return false
        // todo: create a custom runspace for better memory usage https://docs.microsoft.com/en-us/powershell/scripting/developer/hosting/windows-powershell-host-quickstart?view=powershell-7.2, or use code below
        // apparently for powercfg /requests to run properly you need to compile the exe to the particular platform you are running...!

        PowerShell ps = PowerShell.Create();
        string script = "(powercfg /requests | Select-String -Pattern 'DISPLAY:' -Context 0,1).Context.DisplayPostContext | Out-String"; // should return 'None.' if display not used
                                                                                                                                         // string script = "powercfg /requests"; // alternative is to get the full output and then handle the line reading in C#. Unlikely to be any more memory efficient as we are still opening a PS instance
        ps.AddScript(script);

        // invoke execution on the pipeline (collecting output)
        Collection<PSObject> PSOutput = ps.Invoke();

        // define result variable to prevent code path issues
        bool resultVar = false; // ignore the video monitoring as failsafe

        // loop through each output object item
        // if not run as admin, no objects are returned...!
        foreach (PSObject outputItem in PSOutput)
        {
            // if null object was dumped to the pipeline during the script then a null object may be present here
            if (outputItem != null)
            {
                // System.Diagnostics.Debug.WriteLine($"Output line: [{outputItem}]"); // view outputs
                string outputString = outputItem.BaseObject as string; // coerce into string https://stackoverflow.com/questions/37467404/how-to-cast-psobject-to-securestring-cannot-implicitly-convert
                outputString = outputString.Trim('\r', '\n'); // strip new line characters
                string noVideoPlaying = "None."; // response from powercfg if there is no video playing
                string noElevation = "This command requires administrator privileges and must be executed from an elevated command prompt."; // response from powercfg if not run as admin
                if (String.Equals(outputString, noVideoPlaying) || String.Equals(outputString, noElevation)) // if there is no hook, or powercfg is not run as admin, assume no video is playing (and dim screen)
                {
                    resultVar = false;
                }
                else
                {
                    resultVar = true; // the loop iterates over each line of the object. if video is playing, last line will be "Video Wake Lock" which will give rise to the final resultVar
                }
            }
            else
            {
                resultVar = false; // ignore the video monitoring as failsafe
            }
        }
        ps.Stop(); // probably redundant
        return resultVar;


        // taken from https://stackoverflow.com/questions/14981785/calling-powercfg-requests-from-c-sharp-gives-wrong-values
        // maybe refactor into this code, it looks way cleaner and maybe more memory efficient because all the parsing will be done in C# instead of powershell
        //Process p = new Process();
        //p.StartInfo.UseShellExecute = false;
        //p.StartInfo.RedirectStandardOutput = true;
        //p.StartInfo.FileName = "powercfg";
        //p.StartInfo.Arguments = "/requests";
        //p.Start();

        //string output = p.StandardOutput.ReadToEnd();
        //p.WaitForExit();
        //System.Diagnostics.Debug.WriteLine($"Output line: [{output}]");
        //return false;

        // using this guy's api project may also help https://github.com/diversenok/Powercfg

    }

    // actual logic to set and unset brightness after given time
    private async void TimerEventProcessor(object myObject, EventArgs myEventArgs)
    {
        TimeSpan idleTime = GetIdleTime();
        // set initial condition as 10s if IdleDelay is set to 0
        // here we check that the idle time has passed the time set by user
        if ((Settings.Default.IdleDelay == 0 && idleTime.TotalSeconds >= 10.0) || (Settings.Default.IdleDelay >= 1 && idleTime.TotalMinutes >= (double)Settings.Default.IdleDelay))
        {

            if (!isDimmed) // if display is not dimmed 
            {

                // only do the video check if user wants it 
                bool isPlayingVideo = false;
                if (checkBoxMonitorVideoChecked)
                {
                    isPlayingVideo = IsPlayingVideo(); // playing video check, put here so the check is performed only once
                }
                else
                {
                    isPlayingVideo = false;
                }

                if (!isPlayingVideo) // and not playing video
                {
                    restoreTo = GetBrightness();
                    // SetBrightness(Settings.Default.Brightness); // set the brightness directly specified by user on form

                    // dim the screen 
                    decimal setBrightnessTo = (decimal)restoreTo * ((decimal)Settings.Default.Brightness / 100); // set to original * (specified by user / 100), i.e. % of original
                    int setBrightnessToRounded = (int)Math.Round(setBrightnessTo, 0); // round the calculated value. because C# doesn't let you do operations on int or double directly
                    SetBrightness(setBrightnessToRounded);

                    isDimmed = true;
                }
                else // if display is not dimmed but video IS currently playing
                {
                    isDimmed = false; // just pass, wait for the next run

                    // wait(10000); 
                    // maybe use stopwatch https://stackoverflow.com/questions/62829899/async-sleep-in-loop
                    // var sw = Stopwatch.StartNew();

                    // this whole function is implemented using a Timer (see line 74)
                    // every 250ms, this function runs

                    // idleDelay is in minutes...!!
                    t.Stop(); // stop the timer
                              // System.Diagnostics.Debug.WriteLine($"{Settings.Default.IdleDelay}");
                    if (Settings.Default.IdleDelay == 0)
                    {
                        await Task.Delay(5000); // edge case where the IdleDelay is set to 10s (= 0 on the slider) so we set the waiting period between firing to be 5s
                    }
                    else
                    {
                        await Task.Delay(Settings.Default.IdleDelay * 30000); // suspend the timer for half the idle delay setting (e.g. if set to 1 min idle, wait 30s)
                                                                              // before resuming the timer.
                                                                              // Resuming the timer will cause this function TimerEventProcessor()
                                                                              // to fire again --> read lastinput info and trying to dim the screen again.
                                                                              // Because this is an await async loop the GUI functions are not locked up.
                                                                              // Here we need to convert idleDelay from minutes to milliseconds and then divide by 2,
                                                                              // i.e. 2 mins * (60 * 1000 / 2) = 2 mins * 30000
                    }

                    t.Start(); // restart timer
                }
            }
        }
        else if (isDimmed) // if display is dimmed
        {
            SetBrightness(restoreTo);
            isDimmed = false;
        }
    }

}
