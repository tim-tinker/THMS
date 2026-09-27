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
            navigationPanel.Size = new Size(291, 999);
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
            dashboardHostPanel.Size = new Size(1322, 999);
            dashboardHostPanel.TabIndex = 0;
            // 
            // statusStrip
            // 
            statusStrip.BackColor = Color.FromArgb(245, 245, 245);
            statusStrip.Controls.Add(lblPlaidStatus);
            statusStrip.Controls.Add(syncProgress);
            statusStrip.Dock = DockStyle.Bottom;
            statusStrip.Height = 48;
            statusStrip.Name = "statusStrip";
            statusStrip.Padding = new Padding(16, 8, 16, 8);
            statusStrip.Paint += OnStatusStripPaint;
            // 
            // lblPlaidStatus
            // 
            lblPlaidStatus.Dock = DockStyle.Fill;
            lblPlaidStatus.Font = new Font("Segoe UI", 10F);
            lblPlaidStatus.Name = "lblPlaidStatus";
            lblPlaidStatus.Text = "Ready.";
            lblPlaidStatus.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // syncProgress
            // 
            syncProgress.Dock = DockStyle.Right;
            syncProgress.Margin = new Padding(8, 12, 0, 12);
            syncProgress.Name = "syncProgress";
            syncProgress.Size = new Size(180, 24);
            syncProgress.Style = ProgressBarStyle.Marquee;
            syncProgress.Visible = false;
            // 
            // MainForm
            // 
            ClientSize = new Size(1613, 999);
            Controls.Add(dashboardHostPanel);
            Controls.Add(navigationPanel);
            Controls.Add(statusStrip);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "THMS Dashboard";
            Load += OnLoad;
            Shown += OnShown;
            FormClosing += OnFormClosing;
            statusStrip.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
    }
}
