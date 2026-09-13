namespace THMS.UI.WinForms
{
    partial class DataManagerForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Panel panelHost;
        private Controls.HistoryPeriodBar historyBar;
        private Controls.ThmsTabControl tabs;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            panelHost = new Panel();
            historyBar = new Controls.HistoryPeriodBar();
            tabs = new Controls.ThmsTabControl();
            panelHost.SuspendLayout();
            tabs.SuspendLayout();
            SuspendLayout();
            // 
            // panelHost
            // 
            panelHost.AutoScroll = true;
            panelHost.BorderStyle = BorderStyle.FixedSingle;
            panelHost.Controls.Add(tabs);
            panelHost.Controls.Add(historyBar);
            panelHost.Dock = DockStyle.Fill;
            panelHost.Location = new Point(0, 0);
            panelHost.Margin = new Padding(12);
            panelHost.Name = "panelHost";
            panelHost.Padding = new Padding(12);
            panelHost.Size = new Size(1320, 960);
            panelHost.TabIndex = 1;
            // 
            // historyBar
            // 
            historyBar.Dock = DockStyle.Top;
            historyBar.Name = "historyBar";
            historyBar.Padding = new Padding(0, 4, 0, 8);
            historyBar.TabIndex = 0;
            // 
            // tabs
            // 
            tabs.Dock = DockStyle.Fill;
            tabs.Multiline = true;
            tabs.Name = "tabs";
            tabs.TabIndex = 1;
            // 
            // DataManagerForm
            // 
            AutoScaleDimensions = new SizeF(12F, 30F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1320, 960);
            Controls.Add(panelHost);
            Margin = new Padding(4);
            MinimumSize = new Size(1075, 707);
            Name = "DataManagerForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Data Manager";
            panelHost.ResumeLayout(false);
            panelHost.PerformLayout();
            tabs.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
    }
}
