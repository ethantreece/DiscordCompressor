using System;
using System.Windows.Forms;

namespace DiscordCompressor
{
    public partial class ProgressForm : Form
    {
        public void UpdateProgress(int progress, string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<int, string>(UpdateProgress), progress, message);
                return;
            }

            progressBar.Value = progress;
            progressLabel.Text = message;
        }



        private void InitializeComponent()
        {
            progressBar = new ProgressBar();
            progressLabel = new Label();
            SuspendLayout();
            // 
            // progressBar
            // 
            progressBar.Location = new Point(14, 33);
            progressBar.Margin = new Padding(3, 4, 3, 4);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(297, 31);
            progressBar.Style = ProgressBarStyle.Marquee;
            progressBar.TabIndex = 0;
            // 
            // progressLabel
            // 
            progressLabel.AutoSize = true;
            progressLabel.Location = new Point(14, 12);
            progressLabel.Name = "progressLabel";
            progressLabel.Size = new Size(70, 20);
            progressLabel.TabIndex = 1;
            progressLabel.Text = "Starting...";
            // 
            // ProgressForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(325, 81);
            Controls.Add(progressLabel);
            Controls.Add(progressBar);
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Margin = new Padding(3, 4, 3, 4);
            Name = "ProgressForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Compressing...";
            ResumeLayout(false);
            PerformLayout();
        }

        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label progressLabel;
    }
}