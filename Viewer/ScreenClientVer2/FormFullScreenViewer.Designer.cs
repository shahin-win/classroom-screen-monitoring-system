namespace ScreenClientVer2
{
    partial class FormFullScreenViewer
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            pbFull = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)pbFull).BeginInit();
            SuspendLayout();
            // 
            // pbFull
            // 
            pbFull.Dock = DockStyle.Fill;
            pbFull.Location = new Point(0, 0);
            pbFull.Name = "pbFull";
            pbFull.Size = new Size(800, 450);
            pbFull.SizeMode = PictureBoxSizeMode.Zoom;
            pbFull.TabIndex = 0;
            pbFull.TabStop = false;
            // 
            // FormFullScreenViewer
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Black;
            ClientSize = new Size(800, 450);
            Controls.Add(pbFull);
            FormBorderStyle = FormBorderStyle.None;
            KeyPreview = true;
            Name = "FormFullScreenViewer";
            Text = "FormFullScreenViewer";
            WindowState = FormWindowState.Maximized;
            ((System.ComponentModel.ISupportInitialize)pbFull).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private PictureBox pbFull;
    }
}