using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Management;
using System.Management.Automation;
using System.Runtime.InteropServices;
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

	private Timer t = new Timer();

	private bool isDimmed = false;

	private int restoreTo = 50;

    private IContainer components = null;

	private NotifyIcon notifyIcon;

	private ContextMenuStrip contextMenuStrip;

	private ToolStripMenuItem settingsToolStripMenuItem;

	private ToolStripMenuItem exitToolStripMenuItem;

	private CheckBox checkBoxAutoStart;

	private TrackBar trackBarIdleDelay;

	private GroupBox groupBoxIdleDelay;

	private GroupBox groupBoxBrightness;

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
		RegistryKey currentUser = Registry.CurrentUser;
		RegistryKey registryKey = currentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", writable: true);
		checkBoxAutoStart.Checked = registryKey.GetValue("DDA") != null;
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
  //      PowerShell ps = PowerShell.Create();
		//string script = "(powercfg /requests | Select-String -Pattern 'DISPLAY:' -Context 0,1).Context.DisplayPostContext"; // should return 'None.' if display not used
		//ps.AddScript(script);

  //      // invoke execution on the pipeline (collecting output)
  //      Collection<PSObject> PSOutput = ps.Invoke();

  //      // loop through each output object item
  //      foreach (PSObject outputItem in PSOutput)
  //      {
  //          // if null object was dumped to the pipeline during the script then a null object may be present here
  //          if (outputItem != null)
  //          {
  //              System.Diagnostics.Debug.WriteLine($"Output line: [{outputItem}]"); // view outputs
  //          }
  //      }

		return false; } 

	// actual logic to set and unset brightness after given time
	private void TimerEventProcessor(object myObject, EventArgs myEventArgs)
	{
		TimeSpan idleTime = GetIdleTime();
		if ((Settings.Default.IdleDelay == 0 && idleTime.TotalSeconds >= 10.0) || (Settings.Default.IdleDelay >= 1 && idleTime.TotalMinutes >= (double)Settings.Default.IdleDelay))
		{
			bool isPlayingVideo = IsPlayingVideo(); // currently unimplemented
			if (!isDimmed && !isPlayingVideo) // if display is not dimmed and not playing video
			{
				restoreTo = GetBrightness();
				// SetBrightness(Settings.Default.Brightness); // set the brightness directly specified by user on form
				decimal setBrightnessTo = (decimal)restoreTo * ( (decimal)Settings.Default.Brightness / 100 ); // set to original * (specified by user / 100), i.e. % of original
				int setBrightnessToRounded = (int)Math.Round(setBrightnessTo, 0); // round the calculated value. because C# doesn't let you do operations on int or double directly
                SetBrightness(setBrightnessToRounded);

                isDimmed = true;
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

	private void checkBox1_CheckedChanged(object sender, EventArgs e)
	{
		if (checkBoxAutoStart.Checked)
		{
			RegistryKey currentUser = Registry.CurrentUser;
			RegistryKey registryKey = currentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", writable: true);
			if (registryKey.GetValue("DDA") == null)
			{
				registryKey.SetValue("DDA", Application.ExecutablePath, RegistryValueKind.ExpandString);
			}
		}
		else
		{
			RegistryKey currentUser2 = Registry.CurrentUser;
			RegistryKey registryKey2 = currentUser2.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", writable: true);
			if (registryKey2.GetValue("DDA") != null)
			{
				registryKey2.DeleteValue("DDA");
			}
		}
	}

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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DDA.SettingsForm));
		this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
		this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
		this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.checkBoxAutoStart = new System.Windows.Forms.CheckBox();
		this.trackBarIdleDelay = new System.Windows.Forms.TrackBar();
		this.groupBoxIdleDelay = new System.Windows.Forms.GroupBox();
		this.groupBoxBrightness = new System.Windows.Forms.GroupBox();
		this.trackBarBrightness = new System.Windows.Forms.TrackBar();
		this.contextMenuStrip.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.trackBarIdleDelay).BeginInit();
		this.groupBoxIdleDelay.SuspendLayout();
		this.groupBoxBrightness.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.trackBarBrightness).BeginInit();
		base.SuspendLayout();
		this.notifyIcon.ContextMenuStrip = this.contextMenuStrip;
		this.notifyIcon.Icon = (System.Drawing.Icon)resources.GetObject("notifyIcon.Icon");
		this.notifyIcon.Text = "DDA";
		this.notifyIcon.Visible = true;
		this.notifyIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(notifyIcon_MouseDoubleClick);
		this.contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[2] { this.settingsToolStripMenuItem, this.exitToolStripMenuItem });
		this.contextMenuStrip.Name = "contextMenuStrip";
		this.contextMenuStrip.Size = new System.Drawing.Size(117, 48);
		this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
		this.settingsToolStripMenuItem.Size = new System.Drawing.Size(116, 22);
		this.settingsToolStripMenuItem.Text = "Settings";
		this.settingsToolStripMenuItem.Click += new System.EventHandler(settingsToolStripMenuItem_Click);
		this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
		this.exitToolStripMenuItem.Size = new System.Drawing.Size(116, 22);
		this.exitToolStripMenuItem.Text = "Exit";
		this.exitToolStripMenuItem.Click += new System.EventHandler(exitToolStripMenuItem_Click);
		this.checkBoxAutoStart.Location = new System.Drawing.Point(23, 214);
		this.checkBoxAutoStart.Name = "checkBoxAutoStart";
		this.checkBoxAutoStart.Size = new System.Drawing.Size(117, 17);
		this.checkBoxAutoStart.TabIndex = 1;
		this.checkBoxAutoStart.Text = "Start with Windows";
		this.checkBoxAutoStart.UseVisualStyleBackColor = true;
		this.checkBoxAutoStart.CheckedChanged += new System.EventHandler(checkBox1_CheckedChanged);
		this.trackBarIdleDelay.Dock = System.Windows.Forms.DockStyle.Bottom;
		this.trackBarIdleDelay.Location = new System.Drawing.Point(3, 27);
		this.trackBarIdleDelay.Maximum = 100;
		this.trackBarIdleDelay.Name = "trackBarIdleDelay";
		this.trackBarIdleDelay.Size = new System.Drawing.Size(314, 45);
		this.trackBarIdleDelay.TabIndex = 2;
		this.trackBarIdleDelay.TickFrequency = 10;
		this.trackBarIdleDelay.TickStyle = System.Windows.Forms.TickStyle.Both;
		this.trackBarIdleDelay.Value = 2; // default value of 2 mins for idle delay
		this.trackBarIdleDelay.Scroll += new System.EventHandler(trackBarIdleDelay_Scroll);
		this.groupBoxIdleDelay.Controls.Add(this.trackBarIdleDelay);
		this.groupBoxIdleDelay.Location = new System.Drawing.Point(20, 20);
		this.groupBoxIdleDelay.Name = "groupBoxIdleDelay";
		this.groupBoxIdleDelay.Size = new System.Drawing.Size(320, 75);
		this.groupBoxIdleDelay.TabIndex = 4;
		this.groupBoxIdleDelay.TabStop = false;
		this.groupBoxIdleDelay.Text = "Idle Delay - 15 Minutes";
		this.groupBoxBrightness.Controls.Add(this.trackBarBrightness);
		this.groupBoxBrightness.Location = new System.Drawing.Point(20, 115);
		this.groupBoxBrightness.Name = "groupBoxBrightness";
		this.groupBoxBrightness.Size = new System.Drawing.Size(320, 75);
		this.groupBoxBrightness.TabIndex = 5;
		this.groupBoxBrightness.TabStop = false;
		this.groupBoxBrightness.Text = "Brightness - 15% of original brightness"; // original text
        this.trackBarBrightness.Dock = System.Windows.Forms.DockStyle.Bottom;
		this.trackBarBrightness.Location = new System.Drawing.Point(3, 27);
		this.trackBarBrightness.Maximum = 100;
		this.trackBarBrightness.Name = "trackBarBrightness";
		this.trackBarBrightness.Size = new System.Drawing.Size(314, 45);
		this.trackBarBrightness.TabIndex = 2;
		this.trackBarBrightness.TickFrequency = 5; // ticks 5 apart
		this.trackBarBrightness.LargeChange = 5; // move 5 positions if the bar is clicked on either side of the slider
		this.trackBarBrightness.TickStyle = System.Windows.Forms.TickStyle.Both;
		this.trackBarBrightness.Value = 15; // default value
		this.trackBarBrightness.Scroll += new System.EventHandler(trackBarBrightness_Scroll);
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.BackColor = System.Drawing.SystemColors.Control;
		base.ClientSize = new System.Drawing.Size(364, 251);
		base.Controls.Add(this.groupBoxBrightness);
		base.Controls.Add(this.groupBoxIdleDelay);
		base.Controls.Add(this.checkBoxAutoStart);
		base.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
		base.Name = "SettingsForm";
		this.Text = "Dim Display After Updated";
		base.FormClosing += new System.Windows.Forms.FormClosingEventHandler(SettingsForm_FormClosing);
		this.contextMenuStrip.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)this.trackBarIdleDelay).EndInit();
		this.groupBoxIdleDelay.ResumeLayout(false);
		this.groupBoxIdleDelay.PerformLayout();
		this.groupBoxBrightness.ResumeLayout(false);
		this.groupBoxBrightness.PerformLayout();
		((System.ComponentModel.ISupportInitialize)this.trackBarBrightness).EndInit();
		base.ResumeLayout(false);
	}
}
