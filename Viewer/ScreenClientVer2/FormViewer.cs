using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScreenClientVer2
{
    internal class FormViewer : Form
    {

        private FormFullScreenViewer? fullScreenForm;
        private string? selectedMachine;
        private FormFullScreenViewer? activeRDForm = null;


        // ==============================
        // STATE
        // ==============================
        private string? currentFullScreenClient = null;
        private bool isPaused = false;

        private ToolStripComboBox comboBoxLabs;
        private ToolStripMenuItem btnMenuConnect;

        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        private ToolStripStatusLabel copyrightLabel;
        private ToolStripProgressBar statusProgress;

        private bool isSwitchingLab = false;

        private FlowLayoutPanel flowPanelThumbnails;

        private Panel panelFull;
        private PictureBox pictureBoxFull;
        private ToolStripMenuItem menuBtnPause;


        private SignalRManager signalR;

        private ConcurrentDictionary<string, ScreenClientInfo> clients = new();
        private ConcurrentDictionary<string, ThumbnailControl> thumbs = new();

        private readonly TimeSpan offlineTimeout = TimeSpan.FromSeconds(30);
        private System.Windows.Forms.Timer statusTimer;

        private string? lastSubscribedLab = null;
        private MenuStrip menuStrip;
        private ToolStripMenuItem menuConnection;
        private ToolStripMenuItem menuBtnConnect;
        private System.ComponentModel.IContainer components;
        private ToolStripMenuItem remoteControlToolStripMenuItem;
        private ToolStripMenuItem menuRemoteControl;
        private readonly string[] allLabs =
                    {
            "ATR1","ATR2","ATR3","ATR4","BTR1","BTR2",
            "MTR1","MTR2","MTR3","MTR5","TESTROOM"
        };


        // ==============================
        // CONSTRUCTOR
        // ==============================
        public FormViewer()
        {
            InitializeComponent();

            // Detect this viewer's lab from its own PC name
            string machineName = Environment.MachineName;
            string userLab = GetLabFromMachine(machineName);

            // Put only that lab in the ComboBox
            comboBoxLabs.Items.Clear();
            comboBoxLabs.Items.Add(userLab);
            comboBoxLabs.SelectedIndex = 0;

            // Disable ComboBox (user cannot switch labs)
            comboBoxLabs.Enabled = false;

            // Optional: Hide the Connect button from students
            // btnMenuConnect.Visible = false;

            signalR = new SignalRManager("http://your-server-url:Port/screenhub");
            signalR.ClientListUpdated += OnClientListUpdated;
            signalR.ReceiveScreenshot += OnReceiveScreenshot;
        }


        // ==============================
        // INITIALIZE UI
        // ==============================
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            menuStrip = new MenuStrip();
            menuConnection = new ToolStripMenuItem();
            comboBoxLabs = new ToolStripComboBox();
            menuBtnConnect = new ToolStripMenuItem();
            menuBtnPause = new ToolStripMenuItem();
            remoteControlToolStripMenuItem = new ToolStripMenuItem();
            menuRemoteControl = new ToolStripMenuItem();
            statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel();
            statusProgress = new ToolStripProgressBar();
            copyrightLabel = new ToolStripStatusLabel();
            flowPanelThumbnails = new FlowLayoutPanel();
            panelFull = new Panel();
            pictureBoxFull = new PictureBox();
            statusTimer = new System.Windows.Forms.Timer(components);
            menuStrip.SuspendLayout();
            statusStrip.SuspendLayout();
            panelFull.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxFull).BeginInit();
            SuspendLayout();
            // 
            // menuStrip
            // 
            menuStrip.Items.AddRange(new ToolStripItem[] { menuConnection, menuBtnPause, remoteControlToolStripMenuItem });
            menuStrip.Location = new Point(0, 0);
            menuStrip.Name = "menuStrip";
            menuStrip.Size = new Size(367, 24);
            menuStrip.TabIndex = 0;
            // 
            // menuConnection
            // 
            menuConnection.DropDownItems.AddRange(new ToolStripItem[] { comboBoxLabs, menuBtnConnect });
            menuConnection.Name = "menuConnection";
            menuConnection.Size = new Size(81, 20);
            menuConnection.Text = "Connection";
            // 
            // comboBoxLabs
            // 
            comboBoxLabs.Enabled = false;
            comboBoxLabs.Name = "comboBoxLabs";
            comboBoxLabs.Size = new Size(121, 23);
            // 
            // menuBtnConnect
            // 
            menuBtnConnect.Name = "menuBtnConnect";
            menuBtnConnect.Size = new Size(181, 22);
            menuBtnConnect.Text = "Connect";
            menuBtnConnect.Click += BtnConnect_Click;
            // 
            // menuBtnPause
            // 
            menuBtnPause.Enabled = false;
            menuBtnPause.Name = "menuBtnPause";
            menuBtnPause.Size = new Size(50, 20);
            menuBtnPause.Text = "Pause";
            menuBtnPause.Click += MenuBtnPause_Click;
            // 
            // remoteControlToolStripMenuItem
            // 
            remoteControlToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { menuRemoteControl });
            remoteControlToolStripMenuItem.Name = "remoteControlToolStripMenuItem";
            remoteControlToolStripMenuItem.Size = new Size(59, 20);
            remoteControlToolStripMenuItem.Text = "Control";
            // 
            // menuRemoteControl
            // 
            menuRemoteControl.Name = "menuRemoteControl";
            menuRemoteControl.Size = new Size(180, 22);
            menuRemoteControl.Text = "Remote Control";
            menuRemoteControl.Click += menuRemoteControl_Click;
            // 
            // statusStrip
            // 
            statusStrip.Items.AddRange(new ToolStripItem[] { statusLabel, statusProgress, copyrightLabel });
            statusStrip.Location = new Point(0, 272);
            statusStrip.Name = "statusStrip";
            statusStrip.Size = new Size(367, 22);
            statusStrip.TabIndex = 1;
            // 
            // statusLabel
            // 
            statusLabel.BackColor = Color.Transparent;
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(79, 17);
            statusLabel.Text = "Disconnected";
            // 
            // statusProgress
            // 
            statusProgress.Name = "statusProgress";
            statusProgress.Size = new Size(100, 16);
            // 
            // copyrightLabel
            // 
            copyrightLabel.BackColor = Color.Transparent;
            copyrightLabel.Name = "copyrightLabel";
            copyrightLabel.Size = new Size(171, 17);
            copyrightLabel.Spring = true;
            copyrightLabel.Text = "© Syed Shahin";
            copyrightLabel.TextAlign = ContentAlignment.MiddleRight;
            // 
            // flowPanelThumbnails
            // 
            flowPanelThumbnails.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            flowPanelThumbnails.Location = new Point(0, 24);
            flowPanelThumbnails.Name = "flowPanelThumbnails";
            flowPanelThumbnails.Size = new Size(367, 294);
            flowPanelThumbnails.TabIndex = 2;
            // 
            // panelFull
            // 
            panelFull.Controls.Add(pictureBoxFull);
            panelFull.Dock = DockStyle.Fill;
            panelFull.Location = new Point(0, 0);
            panelFull.Name = "panelFull";
            panelFull.Size = new Size(367, 294);
            panelFull.TabIndex = 3;
            panelFull.Visible = false;
            // 
            // pictureBoxFull
            // 
            pictureBoxFull.BackColor = Color.Black;
            pictureBoxFull.Dock = DockStyle.Fill;
            pictureBoxFull.Location = new Point(0, 0);
            pictureBoxFull.Name = "pictureBoxFull";
            pictureBoxFull.Size = new Size(367, 294);
            pictureBoxFull.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxFull.TabIndex = 0;
            pictureBoxFull.TabStop = false;
            pictureBoxFull.DoubleClick += PictureBoxFull_DoubleClick;
            // 
            // statusTimer
            // 
            statusTimer.Enabled = true;
            statusTimer.Interval = 2000;
            statusTimer.Tick += StatusTimer_Tick;
            // 
            // FormViewer
            // 
            BackColor = Color.Gray;
            ClientSize = new Size(367, 294);
            Controls.Add(menuStrip);
            Controls.Add(statusStrip);
            Controls.Add(flowPanelThumbnails);
            Controls.Add(panelFull);
            Name = "FormViewer";
            Text = "Classroom Monitor";
            WindowState = FormWindowState.Maximized;
            menuStrip.ResumeLayout(false);
            menuStrip.PerformLayout();
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            panelFull.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBoxFull).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        //private void InitializeComponent()
        //{
        //    this.Text = "Classroom Monitor";
        //    this.WindowState = FormWindowState.Maximized;

        //    // ============================================
        //    // MENU STRIP
        //    // ============================================
        //    // MenuStrip
        //    var menuStrip = new MenuStrip();
        //    var menuConnection = new ToolStripMenuItem("Connection");

        //    // Combo inside menu
        //    comboBoxLabs = new ToolStripComboBox();
        //    comboBoxLabs.DropDownStyle = ComboBoxStyle.DropDownList;
        //    comboBoxLabs.Items.Add("ALL");
        //    comboBoxLabs.Items.AddRange(allLabs);
        //    comboBoxLabs.SelectedIndex = 0;

        //    // Connect button inside menu
        //    var menuBtnConnect = new ToolStripMenuItem("Connect");
        //    menuBtnConnect.Click += BtnConnect_Click;

        //    // ⭐ Pause/Resume button ⭐
        //    menuBtnPause = new ToolStripMenuItem("Pause");
        //    menuBtnPause.Enabled = false; // disabled until viewing fullscreen
        //    menuBtnPause.Click += MenuBtnPause_Click;

        //    // Add items to the menu
        //    menuConnection.DropDownItems.Add(comboBoxLabs);   // ✔ Correct
        //    menuConnection.DropDownItems.Add(menuBtnConnect);
        //    menuStrip.Items.Add(menuConnection);
        //    menuStrip.Items.Add(menuBtnPause);

        //    // Add to form
        //    Controls.Add(menuStrip);

        //    // ============================================
        //    // STATUS STRIP
        //    // ============================================
        //    statusStrip = new StatusStrip();

        //    statusLabel = new ToolStripStatusLabel("Disconnected");
        //    statusProgress = new ToolStripProgressBar
        //    {
        //        Visible = false,
        //        Style = ProgressBarStyle.Marquee,
        //        MarqueeAnimationSpeed = 30
        //    };

        //    statusStrip.Items.Add(statusLabel);
        //    statusStrip.Items.Add(statusProgress);

        //    Controls.Add(statusStrip);

        //    // ============================================
        //    // THUMBNAIL AREA
        //    // ============================================
        //    flowPanelThumbnails = new FlowLayoutPanel
        //    {
        //        Dock = DockStyle.Fill,
        //        AutoScroll = true
        //    };
        //    flowPanelThumbnails.Location = new Point(0, menuStrip.Height);
        //    flowPanelThumbnails.Size = new Size(
        //        ClientSize.Width,
        //        ClientSize.Height - menuStrip.Height - statusStrip.Height
        //    );
        //    flowPanelThumbnails.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        //    Controls.Add(flowPanelThumbnails);

        //    // ============================================
        //    // FULL SCREEN PANEL
        //    // ============================================
        //    panelFull = new Panel
        //    {
        //        Dock = DockStyle.Fill,
        //        BackColor = Color.Black,
        //        Visible = false
        //    };
        //    Controls.Add(panelFull);

        //    pictureBoxFull = new PictureBox
        //    {
        //        Dock = DockStyle.Fill,
        //        SizeMode = PictureBoxSizeMode.Zoom,
        //        BackColor = Color.Black
        //    };
        //    pictureBoxFull.DoubleClick += PictureBoxFull_DoubleClick;



        //    panelFull.Controls.Add(pictureBoxFull);


        //    // TIMER
        //    statusTimer = new System.Windows.Forms.Timer();
        //    statusTimer.Interval = 2000;
        //    statusTimer.Tick += StatusTimer_Tick;
        //    statusTimer.Start();
        //}
        private string GetLabFromMachine(string machineName)
        {
            var parts = machineName.Split('-');
            return parts.Length > 0 ? parts[0] : "UNKNOWN";
        }


        // ==============================
        // CONNECT BUTTON
        // ==============================


        private async void BtnConnect_Click(object? sender, EventArgs e)
        {
            try
            {
                statusLabel.Text = "Connecting...";
                statusProgress.Visible = true;

                await signalR.ConnectAsync();

                string fixedLab = comboBoxLabs.Items[0].ToString(); // ALWAYS the user's lab

                // Unsubscribe previous
                if (lastSubscribedLab != null)
                    await signalR.UnsubscribeLab(lastSubscribedLab);

                // Subscribe only to this user's lab
                await signalR.SubscribeLab(fixedLab);

                lastSubscribedLab = fixedLab;

                statusLabel.Text = $"Connected → {fixedLab}";
                statusProgress.Visible = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Connect failed: " + ex.Message);
            }
        }

        private void MenuBtnPause_Click(object? sender, EventArgs e)
        {
            isPaused = !isPaused;

            menuBtnPause.Text = isPaused ? "Resume" : "Pause";

            if (isPaused)
                statusLabel.Text = "Paused";
            else
                statusLabel.Text = "Live";
        }


        // ==============================
        // CLIENT LIST UPDATED
        // ==============================
        private void OnClientListUpdated(IEnumerable<string> allClients)
        {
            string selectedLab = "";

            // Read combo safely
            if (comboBoxLabs.GetCurrentParent().InvokeRequired)
            {
                comboBoxLabs.GetCurrentParent().Invoke(new Action(() =>
                {
                    selectedLab = comboBoxLabs.SelectedItem?.ToString() ?? "";
                }));
            }
            else
            {
                selectedLab = comboBoxLabs.SelectedItem?.ToString() ?? "";
            }

            // FILTERING
            List<string> labClients =
                selectedLab == "ALL"
                    ? allClients.ToList()
                    : allClients.Where(c => c.StartsWith(selectedLab + "-", StringComparison.OrdinalIgnoreCase)).ToList();

            // If switching → clean older thumbnails
            if (isSwitchingLab)
            {
                this.Invoke(() =>
                {
                    flowPanelThumbnails.Controls.Clear();
                    thumbs.Clear();
                    clients.Clear();

                    statusProgress.Visible = false;
                    statusLabel.Text = "Connected";

                    isSwitchingLab = false;
                });
            }

            // POPULATE UI
            this.Invoke(() =>
            {
                // REMOVE non-existing thumbnails
                var toRemove = thumbs.Keys.Except(labClients).ToList();
                foreach (var rm in toRemove)
                {
                    if (thumbs.TryRemove(rm, out var tc))
                    {
                        flowPanelThumbnails.Controls.Remove(tc);
                        tc.Dispose();
                        clients.TryRemove(rm, out _);
                    }
                }

                // ADD new thumbnails
                foreach (var m in labClients)
                {
                    if (!thumbs.ContainsKey(m))
                    {
                        clients[m] = new ScreenClientInfo { MachineName = m };
                        var tc = new ThumbnailControl(m);
                        tc.ThumbnailDoubleClicked += Tc_ThumbnailDoubleClicked;

                        thumbs[m] = tc;
                        flowPanelThumbnails.Controls.Add(tc);

                        tc.RemoteDesktopRequested += (s, machine) =>
                        {
                            OpenRemoteDesktop(machine);
                        };

                    }
                }
                // --- SORT THUMBNAILS IN ASCENDING ORDER ---
                //var ordered = thumbs
                //    .OrderBy(t => t.Key, StringComparer.OrdinalIgnoreCase)
                //    .Select(t => t.Value)
                //    .ToList();

                var comparer = new NaturalStringComparer();

                var ordered = thumbs
                    .OrderBy(t => t.Key, comparer)
                    .Select(t => t.Value)
                    .ToList();


                // Rebuild flow layout in correct order
                flowPanelThumbnails.SuspendLayout();
                flowPanelThumbnails.Controls.Clear();

                foreach (var ctrl in ordered)
                {
                    flowPanelThumbnails.Controls.Add(ctrl);
                }

                flowPanelThumbnails.ResumeLayout();

            });
        }

        private void OpenRemoteDesktop(string machine)
        {
            // Prevent multiple RD windows
            if (activeRDForm != null && !activeRDForm.IsDisposed)
            {
                activeRDForm.Focus();
                return;
            }

            activeRDForm = new FormFullScreenViewer(signalR, machine);

            activeRDForm.FormClosed += (s, e) =>
            {
                activeRDForm = null;
            };

            activeRDForm.Show();
        }




        // rest of your filtering + thumbnail logic continues as before...



        // ==============================
        // RECEIVE SCREENSHOT
        // ==============================
        private void OnReceiveScreenshot(string machine, string base64)
        {
            Task.Run(() =>
            {
                try
                {
                    // 1️ Decode image from agent
                    byte[] bytes = Convert.FromBase64String(base64);
                    using var ms = new MemoryStream(bytes);
                    var img = Image.FromStream(ms);

                    // 2️ Update internal cache
                    var info = clients.GetOrAdd(machine, m => new ScreenClientInfo());
                    info.LastImage?.Dispose();
                    info.LastImage = (Image)img.Clone();
                    info.LastSeenUtc = DateTime.UtcNow;

                    // 3️ Update thumbnail if exists
                    if (thumbs.TryGetValue(machine, out var tc))
                    {
                        this.Invoke(() =>
                        {
                            tc.SetOnlineNoImage();
                            tc.UpdateImage(info.LastImage);

                            if (fullScreenForm != null &&
                                !fullScreenForm.IsDisposed &&
                                currentFullScreenClient == machine &&
                                !isPaused)
                            {
                                fullScreenForm.UpdateImage(info.LastImage);
                            }


                            // 🔥 THIS IS FULL SCREEN FORWARDING 🔥
                            if (panelFull.Visible &&
                                currentFullScreenClient == machine &&
                                !isPaused)
                            {
                                //pictureBoxFull.Image?.Dispose();
                                //pictureBoxFull.Image = new Bitmap(info.LastImage);

                                pictureBoxFull.Image?.Dispose();
                                pictureBoxFull.Image = (Image)info.LastImage.Clone();
                                statusLabel.Text = $"Full view → {machine} (LIVE)";
                            }

                            // 🔥 Forward image to Remote Desktop form if active
                            if (activeRDForm != null &&
                                !activeRDForm.IsDisposed &&
                                activeRDForm.machineName == machine)
                            {
                                activeRDForm.UpdateImage(info.LastImage);
                            }
                        });
                    }
                }
                catch { }
            });
        }


        // ==============================
        // DOUBLE-CLICK THUMBNAIL → FULLSCREEN
        // ==============================
        private void Tc_ThumbnailDoubleClicked(object? sender, string machine)
        {
            if (clients.TryGetValue(machine, out var info) && info.LastImage != null)
            {
                currentFullScreenClient = machine;
                menuBtnPause.Enabled = true;
                menuBtnPause.Text = "Pause";


                //pictureBoxFull.Image?.Dispose();
                //pictureBoxFull.Image = new Bitmap(info.LastImage);

                pictureBoxFull.Image?.Dispose();
                pictureBoxFull.Image = (Image)info.LastImage.Clone();

                panelFull.Visible = true;
                panelFull.BringToFront();
                flowPanelThumbnails.Visible = false;

                statusLabel.Text = $"Full view → {machine}";

                //selectedMachine = ((Control)sender).Tag.ToString();

                //fullScreenForm = new FormFullScreenViewer(signalR, selectedMachine);
                //fullScreenForm.Show();
            }
        }


        // ==============================
        // EXIT FULLSCREEN
        // ==============================
        private void PictureBoxFull_DoubleClick(object? sender, EventArgs e)
        {
            panelFull.Visible = false;
            flowPanelThumbnails.Visible = true;
            currentFullScreenClient = null;
            menuBtnPause.Enabled = false;
            menuBtnPause.Text = "Pause";

            statusLabel.Text = "Viewing thumbnails";
        }


        // ==============================
        // PAUSE BUTTON
        // ==============================
        private void BtnPause_Click(object? sender, EventArgs e)
        {
            isPaused = !isPaused;
            menuBtnPause.Enabled = false;
            menuBtnPause.Text = "Pause";

        }


        // ==============================
        // OFFLINE CHECK
        // ==============================
        private void StatusTimer_Tick(object? sender, EventArgs e)
        {
            var now = DateTime.UtcNow;

            foreach (var kvp in clients)
            {
                if (thumbs.TryGetValue(kvp.Key, out var tc))
                {
                    if ((now - kvp.Value.LastSeenUtc) > offlineTimeout)
                    {
                        this.Invoke(() => tc.SetOffline());
                    }
                }
            }
        }

        private void connectToolStripMenuItem_Click(object sender, EventArgs e)
        {
           
        }

        private void menuRemoteControl_Click(object sender, EventArgs e)
        {
            
            if (currentFullScreenClient == null)
            {
                MessageBox.Show("Open a machine in fullscreen first");
                return;
            }

            OpenRemoteDesktop(currentFullScreenClient);

        }
    }

    public class NaturalStringComparer : IComparer<string>
    {
        private static readonly Regex _regex = new Regex(@"\d+|\D+", RegexOptions.Compiled);

        public int Compare(string x, string y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            var xParts = _regex.Matches(x);
            var yParts = _regex.Matches(y);

            int count = Math.Min(xParts.Count, yParts.Count);

            for (int i = 0; i < count; i++)
            {
                string xp = xParts[i].Value;
                string yp = yParts[i].Value;

                bool xn = int.TryParse(xp, out int xi);
                bool yn = int.TryParse(yp, out int yi);

                int cmp;
                if (xn && yn)
                    cmp = xi.CompareTo(yi);
                else
                    cmp = string.Compare(xp, yp, StringComparison.OrdinalIgnoreCase);

                if (cmp != 0)
                    return cmp;
            }

            return xParts.Count.CompareTo(yParts.Count);
        }
    }

}
