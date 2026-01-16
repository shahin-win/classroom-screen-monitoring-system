using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace ScreenClientVer2
{
    public partial class FormFullScreenViewer : Form
    {
        private readonly SignalRManager signalR;
        public string machineName { get; set; }
        public FormFullScreenViewer(SignalRManager hub, string machine)
        {
            InitializeComponent();

            this.Activated += (s, e) =>
            {
                this.Focus();
                pbFull.Focus();
            };


            this.KeyPreview = true;
            this.Focus();
            pbFull.TabStop = true;
            pbFull.Focus();

            signalR = hub;
            machineName = machine;

            // Mouse events
            pbFull.MouseMove += Viewer_MouseMove;
            pbFull.MouseDown += Viewer_MouseDown;
            pbFull.MouseUp += Viewer_MouseUp;
            pbFull.MouseWheel += Viewer_MouseWheel;
            


            // Keyboard events
            this.KeyDown += Viewer_KeyDown;
            this.KeyUp += Viewer_KeyUp;
        }
        // Called by FormViewer when image arrives
        public void UpdateImage(Image img)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<Image>(UpdateImage), img);
                return;
            }

            pbFull.Image?.Dispose();
            pbFull.Image = (Image)img.Clone();   // 👈 CLONE (important)
            pbFull.Focus();
        }

        // ---------------- MOUSE ----------------

        private void Viewer_MouseMove(object sender, MouseEventArgs e)
        {
            SendMouse(RemoteInputType.MouseMove, e);
        }

        private void Viewer_MouseDown(object sender, MouseEventArgs e)
        {
            pbFull.Focus();
            SendMouse(RemoteInputType.MouseDown, e);
        }

        private void Viewer_MouseUp(object sender, MouseEventArgs e)
        {
            SendMouse(RemoteInputType.MouseUp, e);
        }

        private void Viewer_MouseWheel(object sender, MouseEventArgs e)
        {
            signalR.SendRemoteInput(machineName, new RemoteInputCommand
            {
                Type = RemoteInputType.MouseWheel,
                Delta = e.Delta
            });
        }

        private int NormalizeButton(MouseButtons btn)
        {
            return btn switch
            {
                MouseButtons.Left => 1,
                MouseButtons.Right => 2,
                MouseButtons.Middle => 3,
                _ => 0
            };
        }

        private void SendMouse(RemoteInputType type, MouseEventArgs e)
        {
            if (pbFull.Image == null) return;

            float scaleX = (float)pbFull.Image.Width / pbFull.Width;
            float scaleY = (float)pbFull.Image.Height / pbFull.Height;

            signalR.SendRemoteInput(machineName, new RemoteInputCommand
            {
                Type = type,
                X = (int)(e.X * scaleX),
                Y = (int)(e.Y * scaleY),
                Button = NormalizeButton(e.Button)
            });
        }

        // ---------------- KEYBOARD ----------------

        private void Viewer_KeyDown(object sender, KeyEventArgs e)
        {

            // Prevent Viewer OS from using Windows key
            if (e.KeyCode == Keys.LWin || e.KeyCode == Keys.RWin)
            {
                e.SuppressKeyPress = true;
            }

            signalR.SendRemoteInput(machineName, new RemoteInputCommand
            {
                Type = RemoteInputType.KeyDown,
                KeyCode = (int)e.KeyCode
            });
        }

        private void Viewer_KeyUp(object sender, KeyEventArgs e)
        {

            if (e.KeyCode == Keys.LWin || e.KeyCode == Keys.RWin)
            {
                e.SuppressKeyPress = true;
            }

            signalR.SendRemoteInput(machineName, new RemoteInputCommand
            {
                Type = RemoteInputType.KeyUp,
                KeyCode = (int)e.KeyCode
            });
        }

        private void ReleaseAllModifiers()
        {
            // ALT
            signalR.SendRemoteInput(machineName, new RemoteInputCommand
            {
                Type = RemoteInputType.KeyUp,
                KeyCode = (int)Keys.Menu
            });

            // CTRL
            signalR.SendRemoteInput(machineName, new RemoteInputCommand
            {
                Type = RemoteInputType.KeyUp,
                KeyCode = (int)Keys.ControlKey
            });

            // SHIFT
            signalR.SendRemoteInput(machineName, new RemoteInputCommand
            {
                Type = RemoteInputType.KeyUp,
                KeyCode = (int)Keys.ShiftKey
            });

            // LWIN
            signalR.SendRemoteInput(machineName, new RemoteInputCommand
            {
                Type = RemoteInputType.KeyUp,
                KeyCode = (int)Keys.LWin
            });

            // RWIN
            signalR.SendRemoteInput(machineName, new RemoteInputCommand
            {
                Type = RemoteInputType.KeyUp,
                KeyCode = (int)Keys.RWin
            });
        }

        private void ExitRemoteDesktopCleanly()
        {
            // 🔴 VERY IMPORTANT:
            // Release ALL modifier keys on the AGENT before exit
            ReleaseAllModifiers();

            this.Close();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Block Alt+F4 completely
            if (keyData == (Keys.Alt | Keys.F4))
                return true;

            if (keyData == Keys.LWin || keyData == Keys.RWin)
                return true;

            // Ctrl + Shift + Q → Exit Remote Desktop
            if (keyData == (Keys.Control | Keys.Shift | Keys.Q))
            {
                ExitRemoteDesktopCleanly();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

    }

}
