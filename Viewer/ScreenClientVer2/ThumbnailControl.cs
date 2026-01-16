using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScreenClientVer2
{
    public partial class ThumbnailControl : UserControl
    {
        private PictureBox pb;
        private Label lbl;
       

        public event EventHandler<string> RemoteDesktopRequested;


        public string MachineName { get; private set; } = "";

        public ThumbnailControl(string machineName)
        {
            MachineName = machineName;
            InitializeComponents();
            this.Tag = machineName;
        }
       
        private void InitializeComponents()
        {
            this.Width = 200;    // thumbnail width (adjust)
            this.Height = 140;   // thumbnail height + label
            this.Margin = new Padding(5);
            this.Padding = new Padding(0); ;

            pb = new PictureBox
            {
                Size = new Size(192, 108),
                Location = new Point(4, 4),
                SizeMode = PictureBoxSizeMode.Zoom,
               
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.Black
            };
            pb.DoubleClick += Pb_DoubleClick;

            lbl = new Label
            {
                Text = MachineName,
                Location = new Point(4, pb.Bottom + 3),
                Width = this.Width - 8,
                Height = 22,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9, FontStyle.Regular)
            };
            Button btnRD = new Button
            {
                Text = "RD-"+MachineName,
                Dock = DockStyle.Bottom,
                Height = 24
            };

            btnRD.Click += (s, e) =>
            {
                RemoteDesktopRequested?.Invoke(this, MachineName);
            };

            Controls.Add(btnRD);

            this.Controls.Add(pb);
            this.Controls.Add(lbl);
        }

        // Expose method to update image
        public void UpdateImage(Image img)
        {
            //// Replace image thread-safely if needed (caller should be UI thread)
            //pb.Image?.Dispose();
            //pb.Image = new Bitmap(img);
            //lbl.ForeColor = Color.Black;

            try
            {
                pb.Image?.Dispose();

                // CLONE the incoming image safely instead of using it directly
                pb.Image = (Image)img.Clone();

                lbl.ForeColor = Color.Black;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Thumbnail UpdateImage error: " + ex.Message);
            }
        }

        // Mark offline (no image)
        public void SetOffline()
        {
            pb.Image?.Dispose();
            pb.Image = null;
            lbl.ForeColor = Color.Red;
            lbl.Text = $"{MachineName} (OFFLINE)";
        }

        // Mark online and show label properly
        public void SetOnlineNoImage()
        {
            lbl.ForeColor = Color.Black;
            lbl.Text = MachineName;
        }

        // Event raised when thumbnail double-clicked
        public event EventHandler<string>? ThumbnailDoubleClicked;

        private void Pb_DoubleClick(object? sender, EventArgs e)
        {
            ThumbnailDoubleClicked?.Invoke(this, MachineName);
        }
    }
}
