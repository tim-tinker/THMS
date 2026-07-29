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
            lblMake = new Label();
            lblModel = new Label();
            lblYear = new Label();
            lblNameValue = new Label();
            lblMakeValue = new Label();
            lblModelValue = new Label();
            lblYearValue = new Label();
            chargingGrid = new DataGridView();
            fuelGrid = new DataGridView();
            maintenanceGrid = new DataGridView();
            btnAddMileage = new Button();
            btnAddCharging = new Button();
            _splitFuelMaintenance = new SplitContainer();
            _splitFuelCharge = new SplitContainer();
            label3 = new Label();
            label4 = new Label();
            _btnAddInvoice = new Button();
            label2 = new Label();
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
            lblName.Text = "Name:";
            // 
            // lblMake
            // 
            lblMake.Location = new Point(12, 58);
            lblMake.Name = "lblMake";
            lblMake.Size = new Size(100, 33);
            lblMake.TabIndex = 1;
            lblMake.Text = "Make:";
            // 
            // lblModel
            // 
            lblModel.Location = new Point(12, 99);
            lblModel.Name = "lblModel";
            lblModel.Size = new Size(100, 34);
            lblModel.TabIndex = 2;
            lblModel.Text = "Model:";
            // 
            // lblYear
            // 
            lblYear.Location = new Point(12, 141);
            lblYear.Name = "lblYear";
            lblYear.Size = new Size(100, 32);
            lblYear.TabIndex = 3;
            lblYear.Text = "Year:";
            // 
            // lblNameValue
            // 
            lblNameValue.Location = new Point(140, 15);
            lblNameValue.Name = "lblNameValue";
            lblNameValue.Size = new Size(100, 23);
            lblNameValue.TabIndex = 4;
            // 
            // lblMakeValue
            // 
            lblMakeValue.Location = new Point(140, 45);
            lblMakeValue.Name = "lblMakeValue";
            lblMakeValue.Size = new Size(100, 23);
            lblMakeValue.TabIndex = 5;
            // 
            // lblModelValue
            // 
            lblModelValue.Location = new Point(140, 75);
            lblModelValue.Name = "lblModelValue";
            lblModelValue.Size = new Size(100, 23);
            lblModelValue.TabIndex = 6;
            // 
            // lblYearValue
            // 
            lblYearValue.Location = new Point(140, 105);
            lblYearValue.Name = "lblYearValue";
            lblYearValue.Size = new Size(100, 23);
            lblYearValue.TabIndex = 7;
            // 
            // chargingGrid
            // 
            chargingGrid.ColumnHeadersHeight = 40;
            chargingGrid.Dock = DockStyle.Fill;
            chargingGrid.Location = new Point(0, 30);
            chargingGrid.Name = "chargingGrid";
            chargingGrid.ReadOnly = true;
            chargingGrid.RowHeadersWidth = 72;
            chargingGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            chargingGrid.Size = new Size(996, 47);
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
            fuelGrid.Size = new Size(996, 114);
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
            maintenanceGrid.Size = new Size(996, 226);
            maintenanceGrid.TabIndex = 11;
            // 
            // btnAddMileage
            // 
            btnAddMileage.Dock = DockStyle.Bottom;
            btnAddMileage.Location = new Point(0, 144);
            btnAddMileage.Name = "btnAddMileage";
            btnAddMileage.Size = new Size(996, 35);
            btnAddMileage.TabIndex = 12;
            btnAddMileage.Text = "Add Gas";
            btnAddMileage.Click += btnAddMileage_Click;
            // 
            // btnAddCharging
            // 
            btnAddCharging.Dock = DockStyle.Bottom;
            btnAddCharging.Location = new Point(0, 77);
            btnAddCharging.Name = "btnAddCharging";
            btnAddCharging.Size = new Size(996, 35);
            btnAddCharging.TabIndex = 13;
            btnAddCharging.Text = "Add Charging Cost";
            btnAddCharging.Click += btnAddCharging_Click;
            // 
            // _splitFuelMaintenance
            // 
            _splitFuelMaintenance.Location = new Point(12, 196);
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
            _splitFuelMaintenance.Panel2.Controls.Add(_btnAddInvoice);
            _splitFuelMaintenance.Panel2.Controls.Add(label2);
            _splitFuelMaintenance.Size = new Size(996, 595);
            _splitFuelMaintenance.SplitterDistance = 295;
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
            _splitFuelCharge.Panel1.Controls.Add(btnAddMileage);
            // 
            // _splitFuelCharge.Panel2
            // 
            _splitFuelCharge.Panel2.Controls.Add(chargingGrid);
            _splitFuelCharge.Panel2.Controls.Add(label4);
            _splitFuelCharge.Panel2.Controls.Add(btnAddCharging);
            _splitFuelCharge.Size = new Size(996, 295);
            _splitFuelCharge.SplitterDistance = 179;
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
            // _btnAddInvoice
            // 
            _btnAddInvoice.Dock = DockStyle.Bottom;
            _btnAddInvoice.Location = new Point(0, 256);
            _btnAddInvoice.Name = "_btnAddInvoice";
            _btnAddInvoice.Size = new Size(996, 40);
            _btnAddInvoice.TabIndex = 13;
            _btnAddInvoice.Text = "Add Invoice";
            _btnAddInvoice.UseVisualStyleBackColor = true;
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
            // VehicleDetailForm
            // 
            ClientSize = new Size(1020, 803);
            Controls.Add(_splitFuelMaintenance);
            Controls.Add(lblName);
            Controls.Add(lblMake);
            Controls.Add(lblModel);
            Controls.Add(lblYear);
            Controls.Add(lblNameValue);
            Controls.Add(lblMakeValue);
            Controls.Add(lblModelValue);
            Controls.Add(lblYearValue);
            FormBorderStyle = FormBorderStyle.FixedDialog;
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
        }

        #endregion

        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.Label lblMake;
        private System.Windows.Forms.Label lblModel;
        private System.Windows.Forms.Label lblYear;

        private System.Windows.Forms.Label lblNameValue;
        private System.Windows.Forms.Label lblMakeValue;
        private System.Windows.Forms.Label lblModelValue;
        private System.Windows.Forms.Label lblYearValue;
        private System.Windows.Forms.DataGridView chargingGrid;
        private System.Windows.Forms.DataGridView fuelGrid;
        private System.Windows.Forms.DataGridView maintenanceGrid;

        private System.Windows.Forms.Button btnAddMileage;
        private System.Windows.Forms.Button btnAddCharging;
        private SplitContainer _splitFuelMaintenance;
        private Button _btnAddInvoice;
        private Label label2;
        private SplitContainer _splitFuelCharge;
        private Label label3;
        private Label label4;
    }
}
