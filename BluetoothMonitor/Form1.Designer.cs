using System.Windows.Forms;

namespace BluetoothMonitor;

partial class Form1
{
    private const string DeviceName = "Baseus Bowie D05";

    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    private NotifyIcon notifyIcon1;
    private ContextMenuStrip contextMenu;
    private ToolStripMenuItem menuItem;

    private BluetoothService bluetoothService;

    // private ToolStrip toolStrip;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        this.bluetoothService = new BluetoothService();
        this.components = new System.ComponentModel.Container();
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(800, 450);
        this.Text = "Form1";

        // Initialize contextMenu1
        this.menuItem = new ToolStripMenuItem();
        this.contextMenu = new ContextMenuStrip(this.components);
        this.contextMenu.Items.AddRange(new ToolStripItem[]{
            this.menuItem
        });

        // Initialize menuItem1
        // this.menuItem.In = 0;
        this.menuItem.Text = "E&xit";
        this.menuItem.Click += new System.EventHandler(this.menuItem1_Click);

        // Set up how the form should be displayed.
        this.ClientSize = new System.Drawing.Size(292, 266);
        this.Text = "Notify Icon Example";

        // Create the NotifyIcon.
        this.notifyIcon1 = new NotifyIcon(this.components);

        // The Icon property sets the icon that will appear
        // in the systray for this application.
        notifyIcon1.Icon = new Icon("assets\\cake_slice_dessert_food_icon.ico");

        // The ContextMenu property sets the menu that will
        // appear when the systray icon is right clicked.
        notifyIcon1.ContextMenuStrip = this.contextMenu;

        // The Text property sets the text that will be displayed,
        // in a tooltip, when the mouse hovers over the systray icon.
        notifyIcon1.Text = "Form1 (NotifyIcon example)";
        notifyIcon1.Visible = true;

        // Handle the DoubleClick event to activate the form.
        notifyIcon1.DoubleClick += new System.EventHandler(this.notifyIcon1_DoubleClick);
    }


    private void InitializeTimer()
    {
        System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        timer.Interval = 300000; // 5 minutes
        timer.Tick += new EventHandler(Timer_Tick);
        timer.Start();
    }


    private async void Timer_Tick(object sender, EventArgs e)
    {
        // await CheckBatteryLevelForAllDevicesAsync();
    }


    private async void notifyIcon1_DoubleClick(object Sender, EventArgs e)
    {
        var allDevices = await this.bluetoothService.ListDevicesAsync();
        var deviceId = await this.bluetoothService.FindDeviceIdAsync(DeviceName);
        if (deviceId != null)
        {
            await this.bluetoothService.ConnectToClassicBluetoothDeviceAsync(deviceId);
            var batteryLevel = 10;
            MessageBox.Show($"Battery level is {batteryLevel}%");
        }
        // Show the form when the user double clicks on the notify icon.

        // Set the WindowState to normal if the form is minimized.
        if (this.WindowState == FormWindowState.Minimized)
            this.WindowState = FormWindowState.Normal;

        // Activate the form.
        this.Activate();
    }

    private void menuItem1_Click(object Sender, EventArgs e)
    {
        // Close the form, which closes the application.
        this.Close();
    }

    #endregion
}
