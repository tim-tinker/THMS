namespace THMS.UI.WinForms.Controls
{
    partial class HomeCircuitAttributionManagerControl
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();

            base.Dispose(disposing);
        }

        #region Designer generated code

        private void InitializeComponent()
        {
            splitAttribution = new SplitContainer();
            gridAttributionDays = new DataGridView();
            ColumnDate = new DataGridViewTextBoxColumn();
            ColumnDayEnergy = new DataGridViewTextBoxColumn();
            ColumnDaySolar = new DataGridViewTextBoxColumn();
            ColumnDayBattery = new DataGridViewTextBoxColumn();
            ColumnDayGrid = new DataGridViewTextBoxColumn();
            gridHomeCircuit = new DataGridView();
            ColumnTimestamp = new DataGridViewTextBoxColumn();
            ColumnEnergy = new DataGridViewTextBoxColumn();
            SolarColumn = new DataGridViewTextBoxColumn();
            BatteryColumn = new DataGridViewTextBoxColumn();
            GridColumn = new DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)splitAttribution).BeginInit();
            splitAttribution.Panel1.SuspendLayout();
            splitAttribution.Panel2.SuspendLayout();
            splitAttribution.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)gridAttributionDays).BeginInit();
            ((System.ComponentModel.ISupportInitialize)gridHomeCircuit).BeginInit();
            SuspendLayout();
            // 
            // splitAttribution
            // 
            splitAttribution.Dock = DockStyle.Fill;
            splitAttribution.Location = new Point(0, 0);
            splitAttribution.Margin = new Padding(0);
            splitAttribution.Name = "splitAttribution";
            splitAttribution.Orientation = Orientation.Vertical;
            splitAttribution.Panel1.Controls.Add(gridAttributionDays);
            splitAttribution.Panel2.Controls.Add(gridHomeCircuit);
            splitAttribution.Size = new Size(1080, 792);
            splitAttribution.TabIndex = 0;
            // 
            // gridAttributionDays
            // 
            gridAttributionDays.AllowUserToAddRows = false;
            gridAttributionDays.AllowUserToDeleteRows = false;
            gridAttributionDays.AllowUserToResizeRows = false;
            gridAttributionDays.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gridAttributionDays.Columns.AddRange(new DataGridViewColumn[] { ColumnDate, ColumnDayEnergy, ColumnDaySolar, ColumnDayBattery, ColumnDayGrid });
            gridAttributionDays.Dock = DockStyle.Fill;
            gridAttributionDays.Location = new Point(0, 0);
            gridAttributionDays.Margin = new Padding(4);
            gridAttributionDays.Name = "gridAttributionDays";
            gridAttributionDays.ReadOnly = true;
            gridAttributionDays.RowHeadersVisible = false;
            gridAttributionDays.RowHeadersWidth = 72;
            gridAttributionDays.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridAttributionDays.Size = new Size(540, 792);
            gridAttributionDays.TabIndex = 0;
            // 
            // ColumnDate
            // 
            ColumnDate.DataPropertyName = "Date";
            ColumnDate.DefaultCellStyle.Format = "d";
            ColumnDate.HeaderText = "Date";
            ColumnDate.MinimumWidth = 9;
            ColumnDate.Name = "ColumnDate";
            ColumnDate.ReadOnly = true;
            // 
            // ColumnDayEnergy
            // 
            ColumnDayEnergy.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            ColumnDayEnergy.DataPropertyName = "TotalWh";
            ColumnDayEnergy.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            ColumnDayEnergy.DefaultCellStyle.Format = "0.##";
            ColumnDayEnergy.HeaderText = "Energy (Wh)";
            ColumnDayEnergy.MinimumWidth = 9;
            ColumnDayEnergy.Name = "ColumnDayEnergy";
            ColumnDayEnergy.ReadOnly = true;
            // 
            // ColumnDaySolar
            // 
            ColumnDaySolar.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            ColumnDaySolar.DataPropertyName = "SolarWh";
            ColumnDaySolar.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            ColumnDaySolar.DefaultCellStyle.Format = "0.##";
            ColumnDaySolar.HeaderText = "Solar Energy (Wh)";
            ColumnDaySolar.MinimumWidth = 9;
            ColumnDaySolar.Name = "ColumnDaySolar";
            ColumnDaySolar.ReadOnly = true;
            // 
            // ColumnDayBattery
            // 
            ColumnDayBattery.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            ColumnDayBattery.DataPropertyName = "BatteryWh";
            ColumnDayBattery.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            ColumnDayBattery.DefaultCellStyle.Format = "0.##";
            ColumnDayBattery.HeaderText = "Battery Energy (Wh)";
            ColumnDayBattery.MinimumWidth = 9;
            ColumnDayBattery.Name = "ColumnDayBattery";
            ColumnDayBattery.ReadOnly = true;
            // 
            // ColumnDayGrid
            // 
            ColumnDayGrid.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            ColumnDayGrid.DataPropertyName = "GridWh";
            ColumnDayGrid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            ColumnDayGrid.DefaultCellStyle.Format = "0.##";
            ColumnDayGrid.HeaderText = "Grid Energy (Wh)";
            ColumnDayGrid.MinimumWidth = 9;
            ColumnDayGrid.Name = "ColumnDayGrid";
            ColumnDayGrid.ReadOnly = true;
            // 
            // gridHomeCircuit
            // 
            gridHomeCircuit.AllowUserToAddRows = false;
            gridHomeCircuit.AllowUserToDeleteRows = false;
            gridHomeCircuit.AllowUserToResizeRows = false;
            gridHomeCircuit.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gridHomeCircuit.Columns.AddRange(new DataGridViewColumn[] { ColumnTimestamp, ColumnEnergy, SolarColumn, BatteryColumn, GridColumn });
            gridHomeCircuit.Dock = DockStyle.Fill;
            gridHomeCircuit.Location = new Point(0, 0);
            gridHomeCircuit.Margin = new Padding(4);
            gridHomeCircuit.Name = "gridHomeCircuit";
            gridHomeCircuit.RowHeadersVisible = false;
            gridHomeCircuit.RowHeadersWidth = 72;
            gridHomeCircuit.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridHomeCircuit.Size = new Size(540, 792);
            gridHomeCircuit.TabIndex = 1;
            // 
            // ColumnTimestamp
            // 
            ColumnTimestamp.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            ColumnTimestamp.DataPropertyName = "Timestamp";
            ColumnTimestamp.HeaderText = "Timestamp";
            ColumnTimestamp.MinimumWidth = 9;
            ColumnTimestamp.Name = "ColumnTimestamp";
            // 
            // ColumnEnergy
            // 
            ColumnEnergy.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            ColumnEnergy.DataPropertyName = "TotalWh";
            ColumnEnergy.HeaderText = "Energy (Wh)";
            ColumnEnergy.MinimumWidth = 9;
            ColumnEnergy.Name = "ColumnEnergy";
            // 
            // SolarColumn
            // 
            SolarColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            SolarColumn.DataPropertyName = "SolarWh";
            SolarColumn.HeaderText = "Solar Energy (Wh)";
            SolarColumn.MinimumWidth = 9;
            SolarColumn.Name = "SolarColumn";
            // 
            // BatteryColumn
            // 
            BatteryColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            BatteryColumn.DataPropertyName = "BatteryWh";
            BatteryColumn.HeaderText = "Battery Energy (Wh)";
            BatteryColumn.MinimumWidth = 9;
            BatteryColumn.Name = "BatteryColumn";
            // 
            // GridColumn
            // 
            GridColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            GridColumn.DataPropertyName = "GridWh";
            GridColumn.HeaderText = "Grid Energy (Wh)";
            GridColumn.MinimumWidth = 9;
            GridColumn.Name = "GridColumn";
            // 
            // HomeCircuitAttributionManagerControl
            // 
            AutoScaleDimensions = new SizeF(12F, 30F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(splitAttribution);
            Margin = new Padding(4);
            Name = "HomeCircuitAttributionManagerControl";
            Size = new Size(1080, 792);
            Load += OnLoad;
            splitAttribution.Panel1.ResumeLayout(false);
            splitAttribution.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)gridAttributionDays).EndInit();
            ((System.ComponentModel.ISupportInitialize)gridHomeCircuit).EndInit();
            ((System.ComponentModel.ISupportInitialize)splitAttribution).EndInit();
            splitAttribution.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private SplitContainer splitAttribution;
        private DataGridView gridAttributionDays;
        private DataGridView gridHomeCircuit;
        private DataGridViewTextBoxColumn ColumnDate;
        private DataGridViewTextBoxColumn ColumnDayEnergy;
        private DataGridViewTextBoxColumn ColumnDaySolar;
        private DataGridViewTextBoxColumn ColumnDayBattery;
        private DataGridViewTextBoxColumn ColumnDayGrid;
        private DataGridViewTextBoxColumn ColumnTimestamp;
        private DataGridViewTextBoxColumn ColumnEnergy;
        private DataGridViewTextBoxColumn SolarColumn;
        private DataGridViewTextBoxColumn BatteryColumn;
        private DataGridViewTextBoxColumn GridColumn;
    }
}
