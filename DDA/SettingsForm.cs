using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Management;
using System.Management.Automation;
// using System.Threading;
// using System.Diagnostics.Process;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Windows.Forms;
using DDA.Properties;
using Microsoft.Win32;

namespace DDA;

public partial class SettingsForm : Form // some functions split into IdleMonitorFunctions.cs
{
	private Timer t = new Timer(); // t is the overall timer

    private Timer CursorTrackerTimer = new Timer(); // should sit idle most of the time unless started

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
    private CheckBox checkBoxWakeCursorOnPrimaryDisplay;
    private bool checkBoxWakeCursorOnPrimaryDisplayChecked = true; // start checked 
    private LinkLabel linkLabel1;
    private NumericUpDown CursorIdleDelayNumericUpDown;
    private Label label1;

    // yes I know there are better ways to deal with the checkbox. this works but ugly

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
        checkBoxWakeCursorOnPrimaryDisplay.Checked = true; // make sure the wake cursor function is active by default

        // kick off the cursor monitoring, if the thing is checked
        CheckIfCursorIsOnPrimaryDisplayTask();

        Hide();
		t.Tick += new EventHandler(SetBrightnessAfterMakingAllChecks); // run SetBrightnessAfterMakingAllChecks() every 250 ms
        //t.Tick += new EventHandler(IsCursorOnPrimaryDisplay); // at the same time run IsCursorOnPrimaryDisplay() // best to not use the same timer, as you will be restarting the same timer again, which resets the IdleDelay()...
        t.Interval = 250; // make sure this timer is more frequent than the CursorTracket to ensure the correct brightness is restored
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

	private void exitToolStripMenuItem_Click(object sender, EventArgs e) // delete Timer when application exits ??
	{
		notifyIcon.Visible = false;

        // dispose timer t
		t.Stop();
		t.Tick -= SetBrightnessAfterMakingAllChecks;
		t.Dispose();
		t = null;

        // dispose timer CursorTrackerTimer
        CursorTrackerTimer.Stop();
        CursorTrackerTimer.Dispose();
        CursorTrackerTimer = null;

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

    private void checkBoxWakeCursorOnPrimaryDisplay_CheckedChanged(object sender, EventArgs e) // deal with the useless checkbox shit when it is pressed
    {
        if (checkBoxWakeCursorOnPrimaryDisplay.Checked)
        {
            checkBoxWakeCursorOnPrimaryDisplayChecked = true;
            CheckIfCursorIsOnPrimaryDisplayTask(); // start the monitoring, again
        }
        else
        {
            checkBoxWakeCursorOnPrimaryDisplayChecked = false;
        }
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
            this.checkBoxWakeCursorOnPrimaryDisplay = new System.Windows.Forms.CheckBox();
            this.linkLabel1 = new System.Windows.Forms.LinkLabel();
            this.label1 = new System.Windows.Forms.Label();
            this.CursorIdleDelayNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.contextMenuStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarIdleDelay)).BeginInit();
            this.groupBoxIdleDelay.SuspendLayout();
            this.groupBoxBrightness.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarBrightness)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.CursorIdleDelayNumericUpDown)).BeginInit();
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
            this.contextMenuStrip.Size = new System.Drawing.Size(161, 76);
            // 
            // settingsToolStripMenuItem
            // 
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(160, 36);
            this.settingsToolStripMenuItem.Text = "Settings";
            this.settingsToolStripMenuItem.Click += new System.EventHandler(this.settingsToolStripMenuItem_Click);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(160, 36);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // trackBarIdleDelay
            // 
            this.trackBarIdleDelay.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.trackBarIdleDelay.Location = new System.Drawing.Point(6, 52);
            this.trackBarIdleDelay.Margin = new System.Windows.Forms.Padding(6);
            this.trackBarIdleDelay.Maximum = 100;
            this.trackBarIdleDelay.Name = "trackBarIdleDelay";
            this.trackBarIdleDelay.Size = new System.Drawing.Size(575, 80);
            this.trackBarIdleDelay.TabIndex = 2;
            this.trackBarIdleDelay.TickFrequency = 10;
            this.trackBarIdleDelay.TickStyle = System.Windows.Forms.TickStyle.Both;
            this.trackBarIdleDelay.Value = 2;
            this.trackBarIdleDelay.Scroll += new System.EventHandler(this.trackBarIdleDelay_Scroll);
            // 
            // groupBoxIdleDelay
            // 
            this.groupBoxIdleDelay.Controls.Add(this.trackBarIdleDelay);
            this.groupBoxIdleDelay.Location = new System.Drawing.Point(37, 38);
            this.groupBoxIdleDelay.Margin = new System.Windows.Forms.Padding(6);
            this.groupBoxIdleDelay.Name = "groupBoxIdleDelay";
            this.groupBoxIdleDelay.Padding = new System.Windows.Forms.Padding(6);
            this.groupBoxIdleDelay.Size = new System.Drawing.Size(587, 138);
            this.groupBoxIdleDelay.TabIndex = 4;
            this.groupBoxIdleDelay.TabStop = false;
            this.groupBoxIdleDelay.Text = "Idle Delay - 15 Minutes";
            // 
            // groupBoxBrightness
            // 
            this.groupBoxBrightness.Controls.Add(this.trackBarBrightness);
            this.groupBoxBrightness.Location = new System.Drawing.Point(37, 212);
            this.groupBoxBrightness.Margin = new System.Windows.Forms.Padding(6);
            this.groupBoxBrightness.Name = "groupBoxBrightness";
            this.groupBoxBrightness.Padding = new System.Windows.Forms.Padding(6);
            this.groupBoxBrightness.Size = new System.Drawing.Size(587, 138);
            this.groupBoxBrightness.TabIndex = 5;
            this.groupBoxBrightness.TabStop = false;
            this.groupBoxBrightness.Text = "Brightness - 15% of original brightness";
            // 
            // trackBarBrightness
            // 
            this.trackBarBrightness.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.trackBarBrightness.Location = new System.Drawing.Point(6, 52);
            this.trackBarBrightness.Margin = new System.Windows.Forms.Padding(6);
            this.trackBarBrightness.Maximum = 100;
            this.trackBarBrightness.Name = "trackBarBrightness";
            this.trackBarBrightness.Size = new System.Drawing.Size(575, 80);
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
            this.checkBoxMonitorVideo.Location = new System.Drawing.Point(37, 374);
            this.checkBoxMonitorVideo.Name = "checkBoxMonitorVideo";
            this.checkBoxMonitorVideo.Size = new System.Drawing.Size(471, 29);
            this.checkBoxMonitorVideo.TabIndex = 6;
            this.checkBoxMonitorVideo.Text = "Do not dim when video is playing (requires Admin)";
            this.checkBoxMonitorVideo.UseVisualStyleBackColor = true;
            this.checkBoxMonitorVideo.CheckedChanged += new System.EventHandler(this.checkBoxMonitorVideo_CheckedChanged);
            // 
            // checkBoxWakeCursorOnPrimaryDisplay
            // 
            this.checkBoxWakeCursorOnPrimaryDisplay.AutoSize = true;
            this.checkBoxWakeCursorOnPrimaryDisplay.Checked = true;
            this.checkBoxWakeCursorOnPrimaryDisplay.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxWakeCursorOnPrimaryDisplay.Location = new System.Drawing.Point(37, 412);
            this.checkBoxWakeCursorOnPrimaryDisplay.Margin = new System.Windows.Forms.Padding(4);
            this.checkBoxWakeCursorOnPrimaryDisplay.Name = "checkBoxWakeCursorOnPrimaryDisplay";
            this.checkBoxWakeCursorOnPrimaryDisplay.Size = new System.Drawing.Size(399, 54);
            this.checkBoxWakeCursorOnPrimaryDisplay.TabIndex = 7;
            this.checkBoxWakeCursorOnPrimaryDisplay.Text = "Dim Primary Display after \r\nwhen Cursor is not on the Primary Display";
            this.checkBoxWakeCursorOnPrimaryDisplay.UseVisualStyleBackColor = true;
            this.checkBoxWakeCursorOnPrimaryDisplay.CheckedChanged += new System.EventHandler(this.checkBoxWakeCursorOnPrimaryDisplay_CheckedChanged);
            // 
            // linkLabel1
            // 
            this.linkLabel1.AutoSize = true;
            this.linkLabel1.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.linkLabel1.Location = new System.Drawing.Point(33, 490);
            this.linkLabel1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.linkLabel1.Name = "linkLabel1";
            this.linkLabel1.Size = new System.Drawing.Size(127, 25);
            this.linkLabel1.TabIndex = 8;
            this.linkLabel1.TabStop = true;
            this.linkLabel1.Text = "Version 2.4.1";
            this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel1_LinkClicked);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(358, 412);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(86, 25);
            this.label1.TabIndex = 10;
            this.label1.Text = "seconds";
            this.label1.Click += new System.EventHandler(this.label1_Click_1);
            // 
            // CursorIdleDelayNumericUpDown
            // 
            this.CursorIdleDelayNumericUpDown.Location = new System.Drawing.Point(290, 408);
            this.CursorIdleDelayNumericUpDown.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.CursorIdleDelayNumericUpDown.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.CursorIdleDelayNumericUpDown.Name = "CursorIdleDelayNumericUpDown";
            this.CursorIdleDelayNumericUpDown.Size = new System.Drawing.Size(62, 29);
            this.CursorIdleDelayNumericUpDown.TabIndex = 9;
            this.CursorIdleDelayNumericUpDown.Value = new decimal(new int[] {
            30,
            0,
            0,
            0});
            this.CursorIdleDelayNumericUpDown.ValueChanged += new System.EventHandler(this.numericUpDown1_ValueChanged);
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(11F, 24F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(667, 528);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.CursorIdleDelayNumericUpDown);
            this.Controls.Add(this.linkLabel1);
            this.Controls.Add(this.checkBoxWakeCursorOnPrimaryDisplay);
            this.Controls.Add(this.checkBoxMonitorVideo);
            this.Controls.Add(this.groupBoxBrightness);
            this.Controls.Add(this.groupBoxIdleDelay);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(6);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsForm";
            this.Text = "Dim Display After Updated";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SettingsForm_FormClosing);
            this.Load += new System.EventHandler(this.SettingsForm_Load);
            this.contextMenuStrip.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.trackBarIdleDelay)).EndInit();
            this.groupBoxIdleDelay.ResumeLayout(false);
            this.groupBoxIdleDelay.PerformLayout();
            this.groupBoxBrightness.ResumeLayout(false);
            this.groupBoxBrightness.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarBrightness)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.CursorIdleDelayNumericUpDown)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

	}

    private void SettingsForm_Load(object sender, EventArgs e)
    {

    }

    private void label1_Click(object sender, EventArgs e)
    {

    }

    private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        string AuthorURL = "https://github.com/Justin8428/DDAUpdated";
        System.Diagnostics.Process.Start(AuthorURL);
    }

    private void label1_Click_1(object sender, EventArgs e)
    {

    }

    private void numericUpDown1_ValueChanged(object sender, EventArgs e)
    {

    }
}
