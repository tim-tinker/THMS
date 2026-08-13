namespace THMS.UI.WinForms
{
    partial class DataManagerForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Panel panelHost;
        private System.Windows.Forms.Button btnClose;

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
            menuStrip1 = new MenuStrip();
            dataTypeToolStripMenuItem = new ToolStripMenuItem();
            _menuSolarType = new ToolStripMenuItem();
            _menuHomeCircuitType = new ToolStripMenuItem();
            _menuEvSegmentType = new ToolStripMenuItem();
            _menuEvChargeSessionType = new ToolStripMenuItem();
            viewToolStripMenuItem = new ToolStripMenuItem();
            _menuViewMonth = new ToolStripMenuItem();
            _menuViewYear = new ToolStripMenuItem();
            _menuViewLifetime = new ToolStripMenuItem();
            editToolStripMenuItem = new ToolStripMenuItem();
            addToolStripMenuItem = new ToolStripMenuItem();
            editToolStripMenuItem1 = new ToolStripMenuItem();
            deleteToolStripMenuItem = new ToolStripMenuItem();
            btnClose = new Button();
            panelHost.SuspendLayout();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // panelHost
            // 
            panelHost.AutoScroll = true;
            panelHost.BorderStyle = BorderStyle.FixedSingle;
            panelHost.Controls.Add(menuStrip1);
            panelHost.Dock = DockStyle.Fill;
            panelHost.Location = new Point(0, 0);
            panelHost.Margin = new Padding(12);
            panelHost.Name = "panelHost";
            panelHost.Padding = new Padding(12);
            panelHost.Size = new Size(1320, 912);
            panelHost.TabIndex = 1;
            // 
            // menuStrip1
            // 
            menuStrip1.ImageScalingSize = new Size(28, 28);
            menuStrip1.Items.AddRange(new ToolStripItem[] { dataTypeToolStripMenuItem, viewToolStripMenuItem, editToolStripMenuItem });
            menuStrip1.Location = new Point(12, 12);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(1294, 42);
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";
            // 
            // dataTypeToolStripMenuItem
            // 
            dataTypeToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { _menuSolarType, _menuHomeCircuitType, _menuEvSegmentType, _menuEvChargeSessionType });
            dataTypeToolStripMenuItem.Name = "dataTypeToolStripMenuItem";
            dataTypeToolStripMenuItem.Size = new Size(124, 34);
            dataTypeToolStripMenuItem.Text = "Data Type";
            // 
            // _menuSolarType
            // 
            _menuSolarType.Name = "_menuSolarType";
            _menuSolarType.Size = new Size(315, 40);
            _menuSolarType.Text = "Solar";
            _menuSolarType.Click += OnClickSolarType;
            // 
            // _menuHomeCircuitType
            // 
            _menuHomeCircuitType.Name = "_menuHomeCircuitType";
            _menuHomeCircuitType.Size = new Size(315, 40);
            _menuHomeCircuitType.Text = "Home Circuit";
            _menuHomeCircuitType.Click += OnClickHomeCircuitType;
            // 
            // _menuEvSegmentType
            // 
            _menuEvSegmentType.Name = "_menuEvSegmentType";
            _menuEvSegmentType.Size = new Size(315, 40);
            _menuEvSegmentType.Text = "EV Segment";
            _menuEvSegmentType.Click += OnClickEvSegmentType;
            // 
            // _menuEvChargeSessionType
            // 
            _menuEvChargeSessionType.Name = "_menuEvChargeSessionType";
            _menuEvChargeSessionType.Size = new Size(315, 40);
            _menuEvChargeSessionType.Text = "EV Charge Session";
            _menuEvChargeSessionType.Click += OnClickEvChargeSessionType;
            // 
            // viewToolStripMenuItem
            // 
            viewToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { _menuViewMonth, _menuViewYear, _menuViewLifetime });
            viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            viewToolStripMenuItem.Size = new Size(75, 34);
            viewToolStripMenuItem.Text = "View";
            // 
            // _menuViewMonth
            // 
            _menuViewMonth.Name = "_menuViewMonth";
            _menuViewMonth.Size = new Size(315, 40);
            _menuViewMonth.Text = "Month";
            _menuViewMonth.Click += OnClickViewMonth;
            // 
            // _menuViewYear
            // 
            _menuViewYear.Name = "_menuViewYear";
            _menuViewYear.Size = new Size(315, 40);
            _menuViewYear.Text = "Year";
            _menuViewYear.Click += OnClickViewYear;
            // 
            // _menuViewLifetime
            // 
            _menuViewLifetime.Name = "_menuViewLifetime";
            _menuViewLifetime.Size = new Size(315, 40);
            _menuViewLifetime.Text = "Lifetime";
            _menuViewLifetime.Click += OnClickViewLifetime;
            // 
            // editToolStripMenuItem
            // 
            editToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { addToolStripMenuItem, editToolStripMenuItem1, deleteToolStripMenuItem });
            editToolStripMenuItem.Name = "editToolStripMenuItem";
            editToolStripMenuItem.Size = new Size(66, 34);
            editToolStripMenuItem.Text = "Edit";
            // 
            // addToolStripMenuItem
            // 
            addToolStripMenuItem.Name = "addToolStripMenuItem";
            addToolStripMenuItem.Size = new Size(191, 40);
            addToolStripMenuItem.Text = "Add";
            addToolStripMenuItem.Click += OnClickEditAddAction;
            // 
            // editToolStripMenuItem1
            // 
            editToolStripMenuItem1.Name = "editToolStripMenuItem1";
            editToolStripMenuItem1.Size = new Size(315, 40);
            editToolStripMenuItem1.Text = "Edit";
            editToolStripMenuItem1.Click += OnClickEditEditAction;
            // 
            // deleteToolStripMenuItem
            // 
            deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
            deleteToolStripMenuItem.Size = new Size(315, 40);
            deleteToolStripMenuItem.Text = "Delete";
            deleteToolStripMenuItem.Click += OnClickEditDeleteAction;
            // 
            // btnClose
            // 
            btnClose.Dock = DockStyle.Bottom;
            btnClose.Location = new Point(0, 912);
            btnClose.Margin = new Padding(4);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(1320, 48);
            btnClose.TabIndex = 2;
            btnClose.Text = "Close";
            btnClose.UseVisualStyleBackColor = true;
            btnClose.Click += OnClickClose;
            // 
            // DataManagerForm
            // 
            AutoScaleDimensions = new SizeF(12F, 30F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1320, 960);
            Controls.Add(panelHost);
            Controls.Add(btnClose);
            MainMenuStrip = menuStrip1;
            Margin = new Padding(4);
            MinimumSize = new Size(1075, 707);
            Name = "DataManagerForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Data Manager";
            panelHost.ResumeLayout(false);
            panelHost.PerformLayout();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private MenuStrip menuStrip1;
        private ToolStripMenuItem dataTypeToolStripMenuItem;
        private ToolStripMenuItem _menuSolarType;
        private ToolStripMenuItem _menuHomeCircuitType;
        private ToolStripMenuItem _menuEvSegmentType;
        private ToolStripMenuItem _menuEvChargeSessionType;
        private ToolStripMenuItem viewToolStripMenuItem;
        private ToolStripMenuItem _menuViewMonth;
        private ToolStripMenuItem _menuViewYear;
        private ToolStripMenuItem _menuViewLifetime;
        private ToolStripMenuItem editToolStripMenuItem;
        private ToolStripMenuItem addToolStripMenuItem;
        private ToolStripMenuItem editToolStripMenuItem1;
        private ToolStripMenuItem deleteToolStripMenuItem;
    }
}
