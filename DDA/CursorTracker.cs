using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Cryptography;

namespace DDA;
public partial class SettingsForm : Form
{
    bool IsCursorOnPrimaryDisplayBool = true; // cursor should always be on primary display by default
    async Task CheckIfCursorIsOnPrimaryDisplayTask() // entry point of this feature
    {

        while (checkBoxWakeCursorOnPrimaryDisplayChecked)// checkBoxWakeCursorOnPrimaryDisplayChecked = true by default, if unchecked, don't run anything
        {
            IsCursorOnPrimaryDisplayBool = await IsCursorOnPrimaryDisplayTask(); // get the up to date info of whether the cursor is on the primary display
            Debug.WriteLine(string.Format("Is Cursor On Primary Display: {0}", IsCursorOnPrimaryDisplayBool.ToString())); // https://stackoverflow.com/questions/22819117/why-does-debug-writeline-incorrectly-format-strings

            // normally, when there is activity on any display, the primary display will un-dim
            // here, when there is activity on displays that are not the primary display, we want to dim the primary display. 
            // then, when the cursor goes back onto the primary display, we want to un-dim the primary display and reset the timer.

            // TL;DR this variable IsCursorOnPrimaryDisplayBool is also used by SetBrightnessAfterMakingAllChecks

            await Task.Delay(250); // "sleep" the while loop so it doesn't blow up the computer
        }

        // IsCursorOnPrimaryDisplayBool = true;
    }
    async Task<bool> IsCursorOnPrimaryDisplayTask()
    {
        Screen s = Screen.FromPoint(Cursor.Position); // get the screen which contains the cursor
                                                      //Console.WriteLine(s.DeviceName.ToString());
                                                      // Console.WriteLine("Is Cursor On Primary Display: {0}", s.Primary.ToString());
                                                      // 1000ms polling
                                                      // Thread.Sleep(1000);
        await Task.Delay(250); // more memory efficient delay https://stackoverflow.com/questions/23340894/polling-the-right-way // poll every 1s
        return s.Primary;
    }

    //private async void IsCursorOnPrimaryDisplay(object myObject, EventArgs myEventArgs)
    //{

    //    t.Stop();
    //    Screen s = Screen.FromPoint(Cursor.Position); // get the screen which contains the cursor
    //    bool IsCursorOnPrimaryDisplayVariable = s.Primary;
    //    Debug.WriteLine(s.Primary.ToString()); // is cursor on primary display
    //    await Task.Delay(250);
    //    t.Start();
    //}


}