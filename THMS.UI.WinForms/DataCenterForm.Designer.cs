using THMS.UI.WinForms.Controls;

namespace THMS.UI.WinForms
{
    partial class DataCenterForm
    {
        private System.ComponentModel.IContainer components = null;
        private Panel panelHost;
        private HistoryPeriodBar historyBar;
        private ThmsTabControl tabs;

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
            historyBar = new HistoryPeriodBar();
            tabs = new ThmsTabControl();
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
            panelHost.Name = "panelHost";
            panelHost.Padding = new Padding(12);
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
            // DataCenterForm
            // 
            AutoScaleDimensions = new SizeF(12F, 30F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(1320, 960);
            Controls.Add(panelHost);
            Margin = new Padding(4);
            MinimumSize = new Size(1075, 707);
            Name = "DataCenterForm";
            Text = "Energy";
            panelHost.ResumeLayout(false);
            panelHost.PerformLayout();
            tabs.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
    }
}
