namespace THMS.UI.WinForms
{
    partial class VehicleDetailForm
    {
        private System.ComponentModel.IContainer components = null;

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
            lblName = new Label();
            lblMakeModelYear = new Label();
            chargingGrid = new DataGridView();
            fuelGrid = new DataGridView();
            maintenanceGrid = new DataGridView();
            _splitFuelMaintenance = new SplitContainer();
            _splitFuelCharge = new SplitContainer();
            label3 = new Label();
            label4 = new Label();
            label2 = new Label();
            historyBar = new Controls.HistoryPeriodBar();
            ((System.ComponentModel.ISupportInitialize)chargingGrid).BeginInit();
            ((System.ComponentModel.ISupportInitialize)fuelGrid).BeginInit();
            ((System.ComponentModel.ISupportInitialize)maintenanceGrid).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_splitFuelMaintenance).BeginInit();
            _splitFuelMaintenance.Panel1.SuspendLayout();
            _splitFuelMaintenance.Panel2.SuspendLayout();
            _splitFuelMaintenance.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_splitFuelCharge).BeginInit();
            _splitFuelCharge.Panel1.SuspendLayout();
            _splitFuelCharge.Panel2.SuspendLayout();
            _splitFuelCharge.SuspendLayout();
            SuspendLayout();
            // 
            // lblName
            // 
            lblName.Location = new Point(12, 15);
            lblName.Name = "lblName";
            lblName.Size = new Size(100, 35);
            lblName.TabIndex = 0;
            lblName.Text = "Name";
            // 
            // lblMakeModelYear
            // 
            lblMakeModelYear.AutoSize = true;
            lblMakeModelYear.Location = new Point(12, 58);
            lblMakeModelYear.Name = "lblMakeModelYear";
            lblMakeModelYear.Size = new Size(184, 30);
            lblMakeModelYear.TabIndex = 1;
            lblMakeModelYear.Text = "Year, Make, Model";
            // 
            // chargingGrid
            // 
            chargingGrid.AllowUserToAddRows = false;
            chargingGrid.AllowUserToDeleteRows = false;
            chargingGrid.ColumnHeadersHeight = 40;
            chargingGrid.Dock = DockStyle.Fill;
            chargingGrid.Location = new Point(0, 30);
            chargingGrid.Name = "chargingGrid";
            chargingGrid.ReadOnly = true;
            chargingGrid.RowHeadersVisible = false;
            chargingGrid.RowHeadersWidth = 72;
            chargingGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            chargingGrid.Size = new Size(1330, 191);
            chargingGrid.TabIndex = 9;
            // 
            // fuelGrid
            // 
            fuelGrid.ColumnHeadersHeight = 40;
            fuelGrid.Dock = DockStyle.Fill;
            fuelGrid.Location = new Point(0, 30);
            fuelGrid.Name = "fuelGrid";
            fuelGrid.ReadOnly = true;
            fuelGrid.RowHeadersWidth = 72;
            fuelGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            fuelGrid.Size = new Size(1330, 177);
            fuelGrid.TabIndex = 10;
            // 
            // maintenanceGrid
            // 
            maintenanceGrid.ColumnHeadersHeight = 40;
            maintenanceGrid.Dock = DockStyle.Fill;
            maintenanceGrid.Location = new Point(0, 30);
            maintenanceGrid.Name = "maintenanceGrid";
            maintenanceGrid.ReadOnly = true;
            maintenanceGrid.RowHeadersWidth = 72;
            maintenanceGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            maintenanceGrid.Size = new Size(1330, 182);
            maintenanceGrid.TabIndex = 11;
            // 
            // _splitFuelMaintenance
            // 
            _splitFuelMaintenance.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _splitFuelMaintenance.Location = new Point(12, 148);
            _splitFuelMaintenance.Name = "_splitFuelMaintenance";
            _splitFuelMaintenance.Orientation = Orientation.Horizontal;
            // 
            // _splitFuelMaintenance.Panel1
            // 
            _splitFuelMaintenance.Panel1.Controls.Add(_splitFuelCharge);
            // 
            // _splitFuelMaintenance.Panel2
            // 
            _splitFuelMaintenance.Panel2.Controls.Add(maintenanceGrid);
            _splitFuelMaintenance.Panel2.Controls.Add(label2);
            _splitFuelMaintenance.Size = new Size(1330, 796);
            _splitFuelMaintenance.SplitterDistance = 502;
            _splitFuelMaintenance.TabIndex = 19;
            // 
            // _splitFuelCharge
            // 
            _splitFuelCharge.Dock = DockStyle.Fill;
            _splitFuelCharge.Location = new Point(0, 0);
            _splitFuelCharge.Name = "_splitFuelCharge";
            _splitFuelCharge.Orientation = Orientation.Horizontal;
            // 
            // _splitFuelCharge.Panel1
            // 
            _splitFuelCharge.Panel1.Controls.Add(fuelGrid);
            _splitFuelCharge.Panel1.Controls.Add(label3);
            // 
            // _splitFuelCharge.Panel2
            // 
            _splitFuelCharge.Panel2.Controls.Add(chargingGrid);
            _splitFuelCharge.Panel2.Controls.Add(label4);
            _splitFuelCharge.Size = new Size(1330, 502);
            _splitFuelCharge.SplitterDistance = 242;
            _splitFuelCharge.TabIndex = 0;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Dock = DockStyle.Top;
            label3.Location = new Point(0, 0);
            label3.Name = "label3";
            label3.Size = new Size(111, 30);
            label3.TabIndex = 13;
            label3.Text = "Gas Fillups";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Dock = DockStyle.Top;
            label4.Location = new Point(0, 0);
            label4.Name = "label4";
            label4.Size = new Size(118, 30);
            label4.TabIndex = 14;
            label4.Text = "EV Charges";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Dock = DockStyle.Top;
            label2.Location = new Point(0, 0);
            label2.Name = "label2";
            label2.Size = new Size(134, 30);
            label2.TabIndex = 12;
            label2.Text = "Maintenance";
            // 
            // historyBar
            // 
            historyBar.Location = new Point(12, 96);
            historyBar.Name = "historyBar";
            historyBar.Padding = new Padding(0, 4, 0, 4);
            historyBar.TabIndex = 20;
            // 
            // VehicleDetailForm
            // 
            ClientSize = new Size(1354, 956);
            Controls.Add(_splitFuelMaintenance);
            Controls.Add(historyBar);
            Controls.Add(lblName);
            Controls.Add(lblMakeModelYear);
            Name = "VehicleDetailForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Vehicle Details";
            Load += OnLoad;
            ((System.ComponentModel.ISupportInitialize)chargingGrid).EndInit();
            ((System.ComponentModel.ISupportInitialize)fuelGrid).EndInit();
            ((System.ComponentModel.ISupportInitialize)maintenanceGrid).EndInit();
            _splitFuelMaintenance.Panel1.ResumeLayout(false);
            _splitFuelMaintenance.Panel2.ResumeLayout(false);
            _splitFuelMaintenance.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_splitFuelMaintenance).EndInit();
            _splitFuelMaintenance.ResumeLayout(false);
            _splitFuelCharge.Panel1.ResumeLayout(false);
            _splitFuelCharge.Panel1.PerformLayout();
            _splitFuelCharge.Panel2.ResumeLayout(false);
            _splitFuelCharge.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_splitFuelCharge).EndInit();
            _splitFuelCharge.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.Label lblMakeModelYear;
        private System.Windows.Forms.DataGridView chargingGrid;
        private System.Windows.Forms.DataGridView fuelGrid;
        private System.Windows.Forms.DataGridView maintenanceGrid;

        private SplitContainer _splitFuelMaintenance;
        private Label label2;
        private SplitContainer _splitFuelCharge;
        private Label label3;
        private Label label4;
        private Controls.HistoryPeriodBar historyBar;
    }
}
