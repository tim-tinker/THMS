namespace THMS.UI
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private FlowLayoutPanel navigationPanel;
        private Panel dashboardHostPanel;

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
            navigationPanel.SuspendLayout();
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
            // MainForm
            // 
            ClientSize = new Size(1613, 999);
            Controls.Add(dashboardHostPanel);
            Controls.Add(navigationPanel);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "THMS Dashboard";
            navigationPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
    }
}
