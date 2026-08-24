namespace THMS.UI.WinForms
{
    partial class FinanceDataCenterForm
    {
        private System.ComponentModel.IContainer components = null;
        private ToolStrip toolStrip;
        private ToolStripDropDownButton dataTypeToolStripMenuItem;
        private Panel panelHost;
        private Button btnClose;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            toolStrip = new ToolStrip();
            dataTypeToolStripMenuItem = new ToolStripDropDownButton();
            panelHost = new Panel();
            btnClose = new Button();
            toolStrip.SuspendLayout();
            SuspendLayout();
            // 
            // toolStrip
            // 
            toolStrip.Dock = DockStyle.Top;
            toolStrip.ImageScalingSize = new Size(28, 28);
            toolStrip.Items.AddRange(new ToolStripItem[] { dataTypeToolStripMenuItem });
            toolStrip.Location = new Point(0, 0);
            toolStrip.Name = "toolStrip";
            toolStrip.Size = new Size(876, 44);
            toolStrip.TabIndex = 2;
            // 
            // dataTypeToolStripMenuItem
            // 
            dataTypeToolStripMenuItem.Name = "dataTypeToolStripMenuItem";
            dataTypeToolStripMenuItem.Size = new Size(127, 38);
            dataTypeToolStripMenuItem.Text = "Data Type";
            // 
            // panelHost
            // 
            panelHost.Dock = DockStyle.Fill;
            panelHost.Location = new Point(0, 0);
            panelHost.Name = "panelHost";
            panelHost.Size = new Size(876, 596);
            panelHost.TabIndex = 0;
            // 
            // btnClose
            // 
            btnClose.Dock = DockStyle.Bottom;
            btnClose.Location = new Point(0, 596);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(876, 40);
            btnClose.TabIndex = 1;
            btnClose.Text = "Close";
            btnClose.Click += OnClickClose;
            // 
            // FinanceDataCenterForm
            // 
            ClientSize = new Size(876, 636);
            Controls.Add(panelHost);
            Controls.Add(toolStrip);
            Controls.Add(btnClose);
            Name = "FinanceDataCenterForm";
            Text = "Finance Data Center";
            toolStrip.ResumeLayout(false);
            toolStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
