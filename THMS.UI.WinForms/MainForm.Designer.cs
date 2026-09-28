namespace THMS.UI
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private FlowLayoutPanel navigationPanel;
        private Panel dashboardHostPanel;
        private Panel statusStrip;
        private Label lblPlaidStatus;
        private ProgressBar syncProgress;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            navigationPanel = new FlowLayoutPanel();
            dashboardHostPanel = new Panel();
            statusStrip = new Panel();
            lblPlaidStatus = new Label();
            syncProgress = new ProgressBar();
            statusStrip.SuspendLayout();
            SuspendLayout();
            // 
            // navigationPanel
            // 
            navigationPanel.AutoScroll = true;
            navigationPanel.BackColor = Color.LightGray;
            navigationPanel.Dock = DockStyle.Left;
            navigationPanel.FlowDirection = FlowDirection.TopDown;
            navigationPanel.Location = new Point(0, 0);
            navigationPanel.Name = "navigationPanel";
            navigationPanel.Padding = new Padding(12);
            navigationPanel.Size = new Size(291, 1099);
            navigationPanel.TabIndex = 1;
            navigationPanel.WrapContents = false;
            navigationPanel.Resize += OnNavigationPanelResize;
            // 
            // dashboardHostPanel
            // 
            dashboardHostPanel.BackColor = Color.White;
            dashboardHostPanel.Dock = DockStyle.Fill;
            dashboardHostPanel.Location = new Point(291, 0);
            dashboardHostPanel.Name = "dashboardHostPanel";
            dashboardHostPanel.Size = new Size(2409, 1099);
            dashboardHostPanel.TabIndex = 0;
            // 
            // statusStrip
            // 
            statusStrip.BackColor = Color.FromArgb(245, 245, 245);
            statusStrip.Controls.Add(lblPlaidStatus);
            statusStrip.Controls.Add(syncProgress);
            statusStrip.Dock = DockStyle.Bottom;
            statusStrip.Location = new Point(0, 1099);
            statusStrip.Name = "statusStrip";
            statusStrip.Padding = new Padding(16, 8, 16, 8);
            statusStrip.Size = new Size(2700, 48);
            statusStrip.TabIndex = 2;
            statusStrip.Paint += OnStatusStripPaint;
            // 
            // lblPlaidStatus
            // 
            lblPlaidStatus.Dock = DockStyle.Fill;
            lblPlaidStatus.Font = new Font("Segoe UI", 10F);
            lblPlaidStatus.Location = new Point(16, 8);
            lblPlaidStatus.Name = "lblPlaidStatus";
            lblPlaidStatus.Size = new Size(2488, 32);
            lblPlaidStatus.TabIndex = 0;
            lblPlaidStatus.Text = "Ready.";
            lblPlaidStatus.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // syncProgress
            // 
            syncProgress.Dock = DockStyle.Right;
            syncProgress.Location = new Point(2504, 8);
            syncProgress.Margin = new Padding(8, 12, 0, 12);
            syncProgress.Name = "syncProgress";
            syncProgress.Size = new Size(180, 32);
            syncProgress.Style = ProgressBarStyle.Marquee;
            syncProgress.TabIndex = 1;
            syncProgress.Visible = false;
            // 
            // MainForm
            // 
            ClientSize = new Size(2700, 1147);
            Controls.Add(dashboardHostPanel);
            Controls.Add(navigationPanel);
            Controls.Add(statusStrip);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "THMS Dashboard";
            FormClosing += OnFormClosing;
            Load += OnLoad;
            Shown += OnShown;
            statusStrip.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
    }
}
