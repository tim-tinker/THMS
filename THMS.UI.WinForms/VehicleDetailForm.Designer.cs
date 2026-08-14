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
            _dateStart = new DateTimePicker();
            groupBox1 = new GroupBox();
            label5 = new Label();
            _dateEnd = new DateTimePicker();
            label1 = new Label();
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
            groupBox1.SuspendLayout();
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
            _splitFuelMaintenance.Location = new Point(12, 186);
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
            _splitFuelMaintenance.Size = new Size(1330, 758);
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
            // _dateStart
            // 
            _dateStart.Format = DateTimePickerFormat.Short;
            _dateStart.Location = new Point(77, 34);
            _dateStart.Name = "_dateStart";
            _dateStart.Size = new Size(183, 35);
            _dateStart.TabIndex = 20;
            _dateStart.ValueChanged += OnValueChangedStart;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(label5);
            groupBox1.Controls.Add(_dateEnd);
            groupBox1.Controls.Add(label1);
            groupBox1.Controls.Add(_dateStart);
            groupBox1.Location = new Point(12, 91);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(516, 89);
            groupBox1.TabIndex = 21;
            groupBox1.TabStop = false;
            groupBox1.Text = "Date Range";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(266, 39);
            label5.Name = "label5";
            label5.Size = new Size(39, 30);
            label5.TabIndex = 22;
            label5.Text = "To:";
            // 
            // _dateEnd
            // 
            _dateEnd.Format = DateTimePickerFormat.Short;
            _dateEnd.Location = new Point(311, 35);
            _dateEnd.Name = "_dateEnd";
            _dateEnd.Size = new Size(183, 35);
            _dateEnd.TabIndex = 21;
            _dateEnd.ValueChanged += OnValueChangedEnd;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(6, 38);
            label1.Name = "label1";
            label1.Size = new Size(65, 30);
            label1.TabIndex = 0;
            label1.Text = "From:";
            // 
            // VehicleDetailForm
            // 
            ClientSize = new Size(1354, 956);
            Controls.Add(groupBox1);
            Controls.Add(_splitFuelMaintenance);
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
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
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
        private DateTimePicker _dateStart;
        private GroupBox groupBox1;
        private Label label5;
        private DateTimePicker _dateEnd;
        private Label label1;
    }
}
