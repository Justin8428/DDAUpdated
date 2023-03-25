using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Management;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
// using System.Threading;
// using System.Diagnostics.Process;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using DDA.Properties;
using Microsoft.Win32;

namespace DDA;

public class SettingsForm : Form
{
	private struct LASTINPUTINFO
	{
		public static readonly int SizeOf = Marshal.SizeOf(typeof(LASTINPUTINFO));

		[MarshalAs(UnmanagedType.U4)]
		public uint cbSize;

		[MarshalAs(UnmanagedType.U4)]
		public uint dwTime;
	}

	private System.Windows.Forms.Timer t = new System.Windows.Forms.Timer(); // t is the overall timer

	private bool isDimmed = false;

	private int restoreTo = 50;

    private IContainer components = null;

	private NotifyIcon notifyIcon;

	private ContextMenuStrip contextMenuStrip;

	private ToolStripMenuItem settingsToolStripMenuItem;

	private ToolStripMenuItem exitToolStripMenuItem;

	// private CheckBox checkBoxAutoStart;

    private TrackBar trackBarIdleDelay;

	private GroupBox groupBoxIdleDelay;

	private GroupBox groupBoxBrightness;
	private CheckBox checkBoxMonitorVideo;
	private bool checkBoxMonitorVideoChecked = true; // start checked
    private TrackBar trackBarBrightness;

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

	public SettingsForm()
	{
		InitializeComponent();
		//RegistryKey currentUser = Registry.CurrentUser;
		//RegistryKey registryKey = currentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", writable: true);
		//checkBoxAutoStart.Checked = registryKey.GetValue("DDA") != null;
		trackBarIdleDelay.Value = Settings.Default.IdleDelay;
		trackBarBrightness.Value = Settings.Default.Brightness;
		updateIdleDelayLabel();
		updateBrightnessLabel();
		base.WindowState = FormWindowState.Minimized;
		base.ShowInTaskbar = false;
		Hide();
		t.Tick += TimerEventProcessor;
		t.Interval = 250;
		t.Start();
	}

	private void SettingsForm_FormClosing(object sender, FormClosingEventArgs e)
	{
		if (e.CloseReason == CloseReason.UserClosing)
		{
			Hide();
			e.Cancel = true;
		}
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

        //PowerShell ps = PowerShell.Create();
        //string script = "(powercfg /requests | Select-String -Pattern 'DISPLAY:' -Context 0,1).Context.DisplayPostContext | Out-String"; // should return 'None.' if display not used
        //// string script = "powercfg /requests"; // alternative is to get the full output and then handle the line reading in C#. Unlikely to be any more memory efficient as we are still opening a PS instance
        //ps.AddScript(script);

        //// invoke execution on the pipeline (collecting output)
        //Collection<PSObject> PSOutput = ps.Invoke();

        // define result variable to prevent code path issues
        bool resultVar = false; // ignore the video monitoring as failsafe

		// start querying and parsing powershell
        Runspace powercfg_runspace = RunspaceFactory.CreateRunspace();// Create a new runspace
        powercfg_runspace.Open();// Open the runspace
		using (PowerShell ps = PowerShell.Create()) // Create a PowerShell object to execute a command
		{
			// Set the runspace for the PowerShell object
			ps.Runspace = powercfg_runspace;

			// Add relevant commands
			ps.AddCommand("powercfg");
			ps.AddCommand("Select-String");
			ps.AddCommand("Out-String");

			// Invoke the command and get the results
			string script = "(powercfg /requests | Select-String -Pattern 'DISPLAY:' -Context 0,1).Context.DisplayPostContext | Out-String"; // should return 'None.' if display not used
																																			 // string script = "powercfg /requests"; // alternative is to get the full output and then handle the line reading in C#. Unlikely to be any more memory efficient as we are still opening a PS instance
			ps.AddScript(script);

			// invoke execution on the pipeline (collecting output)
			Collection<PSObject> PSOutput = ps.Invoke();



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
            
        }
        powercfg_runspace.Close(); // close powershell instance
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

    // get the goddamn checkbox to work
    private void checkBoxMonitorVideo_CheckedChanged(object sender, EventArgs e)
    {
        if (checkBoxMonitorVideo.Checked)
        {
			checkBoxMonitorVideoChecked = true;
        }
        else
        {
            checkBoxMonitorVideoChecked = false;
        }
    }

    //// improved wait function https://stackoverflow.com/questions/10458118/wait-one-second-in-running-program
    //public void wait(int milliseconds)
    //{
    //    var timer1 = new System.Windows.Forms.Timer();
    //    if (milliseconds == 0 || milliseconds < 0) return;

    //    // Console.WriteLine("start wait timer");
    //    timer1.Interval = milliseconds;
    //    timer1.Enabled = true;
    //    timer1.Start();

    //    timer1.Tick += (s, e) =>
    //    {
    //        timer1.Enabled = false;
    //        timer1.Stop();
    //        // Console.WriteLine("stop wait timer");
    //    };

    //    while (timer1.Enabled)
    //    {
    //        Application.DoEvents();
    //    }
    //}

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

	private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
	{
		base.WindowState = FormWindowState.Normal;
		Show();
	}

	private void exitToolStripMenuItem_Click(object sender, EventArgs e)
	{
		notifyIcon.Visible = false;
		t.Stop();
		t.Tick -= TimerEventProcessor;
		t.Dispose();
		t = null;
		Application.Exit();
	}

	// remove the checkbox, it doesn't work. Create a Scheduled task in Task Scheduler instead
	//private void checkBox1_CheckedChanged(object sender, EventArgs e)
	//{
	//	if (checkBoxAutoStart.Checked)
	//	{
	//		RegistryKey currentUser = Registry.CurrentUser;
	//		RegistryKey registryKey = currentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", writable: true);
	//		if (registryKey.GetValue("DDA") == null)
	//		{
	//			registryKey.SetValue("DDA", Application.ExecutablePath, RegistryValueKind.ExpandString);
	//		}
	//	}
	//	else
	//	{
	//		RegistryKey currentUser2 = Registry.CurrentUser;
	//		RegistryKey registryKey2 = currentUser2.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", writable: true);
	//		if (registryKey2.GetValue("DDA") != null)
	//		{
	//			registryKey2.DeleteValue("DDA");
	//		}
	//	}
	//}

	private void updateIdleDelayLabel()
	{
		if (trackBarIdleDelay.Value == 0)
		{
			groupBoxIdleDelay.Text = "Idle Delay - 10 Seconds";
		}
		else
		{
			groupBoxIdleDelay.Text = "Idle Delay - " + trackBarIdleDelay.Value + " Minute(s)";
		}
	}

	private void updateBrightnessLabel() // update the brightness label when settings changed
	{
		groupBoxBrightness.Text = "Brightness - " + trackBarBrightness.Value + "% of original brightness";
	}

	private void trackBarIdleDelay_Scroll(object sender, EventArgs e)
	{
		updateIdleDelayLabel();
		saveSettings();
	}

	private void trackBarBrightness_Scroll(object sender, EventArgs e)
	{
		updateBrightnessLabel();
		saveSettings();
	}

	private void saveSettings()
	{
		Settings.Default.IdleDelay = trackBarIdleDelay.Value;
		Settings.Default.Brightness = trackBarBrightness.Value;
		Settings.Default.Save();
	}

	private void buttonClose_Click(object sender, EventArgs e)
	{
		Hide();
	}

	private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
	{
		base.WindowState = FormWindowState.Normal;
		Show();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsForm));
            this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            // this.checkBoxAutoStart = new System.Windows.Forms.CheckBox();
            this.trackBarIdleDelay = new System.Windows.Forms.TrackBar();
            this.groupBoxIdleDelay = new System.Windows.Forms.GroupBox();
            this.groupBoxBrightness = new System.Windows.Forms.GroupBox();
            this.trackBarBrightness = new System.Windows.Forms.TrackBar();
            this.checkBoxMonitorVideo = new System.Windows.Forms.CheckBox();
            this.contextMenuStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarIdleDelay)).BeginInit();
            this.groupBoxIdleDelay.SuspendLayout();
            this.groupBoxBrightness.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarBrightness)).BeginInit();
            this.SuspendLayout();
            // 
            // notifyIcon
            // 
            this.notifyIcon.ContextMenuStrip = this.contextMenuStrip;
            this.notifyIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon.Icon")));
            this.notifyIcon.Text = "DDA";
            this.notifyIcon.Visible = true;
            this.notifyIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon_MouseDoubleClick);
            // 
            // contextMenuStrip
            // 
            this.contextMenuStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.settingsToolStripMenuItem,
            this.exitToolStripMenuItem});
            this.contextMenuStrip.Name = "contextMenuStrip";
            this.contextMenuStrip.Size = new System.Drawing.Size(132, 52);
            // 
            // settingsToolStripMenuItem
            // 
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(131, 24);
            this.settingsToolStripMenuItem.Text = "Settings";
            this.settingsToolStripMenuItem.Click += new System.EventHandler(this.settingsToolStripMenuItem_Click);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(131, 24);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // checkBoxAutoStart
            // 
            //this.checkBoxAutoStart.Location = new System.Drawing.Point(0, 0);
            //this.checkBoxAutoStart.Name = "checkBoxAutoStart";
            //this.checkBoxAutoStart.Size = new System.Drawing.Size(104, 24);
            //this.checkBoxAutoStart.TabIndex = 7;
            // 
            // trackBarIdleDelay
            // 
            this.trackBarIdleDelay.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.trackBarIdleDelay.Location = new System.Drawing.Point(4, 32);
            this.trackBarIdleDelay.Margin = new System.Windows.Forms.Padding(4);
            this.trackBarIdleDelay.Maximum = 100;
            this.trackBarIdleDelay.Name = "trackBarIdleDelay";
            this.trackBarIdleDelay.Size = new System.Drawing.Size(419, 56);
            this.trackBarIdleDelay.TabIndex = 2;
            this.trackBarIdleDelay.TickFrequency = 10;
            this.trackBarIdleDelay.TickStyle = System.Windows.Forms.TickStyle.Both;
            this.trackBarIdleDelay.Value = 2;
            this.trackBarIdleDelay.Scroll += new System.EventHandler(this.trackBarIdleDelay_Scroll);
            // 
            // groupBoxIdleDelay
            // 
            this.groupBoxIdleDelay.Controls.Add(this.trackBarIdleDelay);
            this.groupBoxIdleDelay.Location = new System.Drawing.Point(27, 25);
            this.groupBoxIdleDelay.Margin = new System.Windows.Forms.Padding(4);
            this.groupBoxIdleDelay.Name = "groupBoxIdleDelay";
            this.groupBoxIdleDelay.Padding = new System.Windows.Forms.Padding(4);
            this.groupBoxIdleDelay.Size = new System.Drawing.Size(427, 92);
            this.groupBoxIdleDelay.TabIndex = 4;
            this.groupBoxIdleDelay.TabStop = false;
            this.groupBoxIdleDelay.Text = "Idle Delay - 15 Minutes";
            // 
            // groupBoxBrightness
            // 
            this.groupBoxBrightness.Controls.Add(this.trackBarBrightness);
            this.groupBoxBrightness.Location = new System.Drawing.Point(27, 141);
            this.groupBoxBrightness.Margin = new System.Windows.Forms.Padding(4);
            this.groupBoxBrightness.Name = "groupBoxBrightness";
            this.groupBoxBrightness.Padding = new System.Windows.Forms.Padding(4);
            this.groupBoxBrightness.Size = new System.Drawing.Size(427, 92);
            this.groupBoxBrightness.TabIndex = 5;
            this.groupBoxBrightness.TabStop = false;
            this.groupBoxBrightness.Text = "Brightness - 15% of original brightness";
            // 
            // trackBarBrightness
            // 
            this.trackBarBrightness.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.trackBarBrightness.Location = new System.Drawing.Point(4, 32);
            this.trackBarBrightness.Margin = new System.Windows.Forms.Padding(4);
            this.trackBarBrightness.Maximum = 100;
            this.trackBarBrightness.Name = "trackBarBrightness";
            this.trackBarBrightness.Size = new System.Drawing.Size(419, 56);
            this.trackBarBrightness.TabIndex = 2;
            this.trackBarBrightness.TickFrequency = 5;
            this.trackBarBrightness.TickStyle = System.Windows.Forms.TickStyle.Both;
            this.trackBarBrightness.Value = 15;
            this.trackBarBrightness.Scroll += new System.EventHandler(this.trackBarBrightness_Scroll);
            // 
            // checkBoxMonitorVideo
            // 
            this.checkBoxMonitorVideo.AutoSize = true;
            this.checkBoxMonitorVideo.Checked = true;
            this.checkBoxMonitorVideo.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxMonitorVideo.Location = new System.Drawing.Point(27, 257);
            this.checkBoxMonitorVideo.Margin = new System.Windows.Forms.Padding(2);
            this.checkBoxMonitorVideo.Name = "checkBoxMonitorVideo";
            this.checkBoxMonitorVideo.Size = new System.Drawing.Size(325, 20);
            this.checkBoxMonitorVideo.TabIndex = 6;
            this.checkBoxMonitorVideo.Text = "Do not dim when video is playing (requires Admin)";
            this.checkBoxMonitorVideo.UseVisualStyleBackColor = true;
            this.checkBoxMonitorVideo.CheckedChanged += new System.EventHandler(this.checkBoxMonitorVideo_CheckedChanged);
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(485, 309);
            this.Controls.Add(this.checkBoxMonitorVideo);
            this.Controls.Add(this.groupBoxBrightness);
            this.Controls.Add(this.groupBoxIdleDelay);
            // this.Controls.Add(this.checkBoxAutoStart);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsForm";
            this.Text = "Dim Display After Updated";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SettingsForm_FormClosing);
            this.contextMenuStrip.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.trackBarIdleDelay)).EndInit();
            this.groupBoxIdleDelay.ResumeLayout(false);
            this.groupBoxIdleDelay.PerformLayout();
            this.groupBoxBrightness.ResumeLayout(false);
            this.groupBoxBrightness.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarBrightness)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

	}
}
