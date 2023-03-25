using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Management;
using System.Management.Automation;
// using System.Threading;
// using System.Diagnostics.Process;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using DDA.Properties;
using Microsoft.Win32;

namespace DDA;

public partial class SettingsForm : Form // some functions split into IdleMonitorFunctions.cs
{
	private Timer t = new Timer(); // t is the overall timer

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


	public SettingsForm()
	{
		InitializeComponent();
		trackBarIdleDelay.Value = Settings.Default.IdleDelay;
		trackBarBrightness.Value = Settings.Default.Brightness;
		updateIdleDelayLabel();
		updateBrightnessLabel();
		base.WindowState = FormWindowState.Minimized;
		base.ShowInTaskbar = false;
		Hide();
		t.Tick += TimerEventProcessor; // run TimerEventProcessor() every 250 ms
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
