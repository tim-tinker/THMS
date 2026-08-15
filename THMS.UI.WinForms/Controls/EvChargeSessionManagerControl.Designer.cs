namespace THMS.UI.WinForms.Controls
{
    partial class EvChargeSessionManagerControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            _gridSessions = new DataGridView();
            VehicleColumn = new DataGridViewTextBoxColumn();
            OdometerColumn = new DataGridViewTextBoxColumn();
            StartColumn = new DataGridViewTextBoxColumn();
            StartSocColumn = new DataGridViewTextBoxColumn();
            EndTimeColumn = new DataGridViewTextBoxColumn();
            EndSocColumn = new DataGridViewTextBoxColumn();
            ChargeKhwColumn = new DataGridViewTextBoxColumn();
            EnergyDrawKwhColumn = new DataGridViewTextBoxColumn();
            IsHomeChargeColumn = new DataGridViewCheckBoxColumn();
            SolarColumn = new DataGridViewTextBoxColumn();
            BatteryColumn = new DataGridViewTextBoxColumn();
            GridColumn = new DataGridViewTextBoxColumn();
            CostColumn = new DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)_gridSessions).BeginInit();
            SuspendLayout();
            // 
            // _gridSessions
            // 
            _gridSessions.AllowUserToAddRows = false;
            _gridSessions.AllowUserToDeleteRows = false;
            _gridSessions.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            _gridSessions.Columns.AddRange(new DataGridViewColumn[] { VehicleColumn, OdometerColumn, StartColumn, StartSocColumn, EndTimeColumn, EndSocColumn, ChargeKhwColumn, EnergyDrawKwhColumn, IsHomeChargeColumn, SolarColumn, BatteryColumn, GridColumn, CostColumn });
            _gridSessions.Dock = DockStyle.Fill;
            _gridSessions.Location = new Point(0, 0);
            _gridSessions.Name = "_gridSessions";
            _gridSessions.ReadOnly = true;
            _gridSessions.RowHeadersVisible = false;
            _gridSessions.RowHeadersWidth = 72;
            _gridSessions.Size = new Size(1887, 881);
            _gridSessions.TabIndex = 0;
            // 
            // VehicleColumn
            // 
            VehicleColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            VehicleColumn.DataPropertyName = "VehicleName";
            VehicleColumn.HeaderText = "Vehicle";
            VehicleColumn.MinimumWidth = 9;
            VehicleColumn.Name = "VehicleColumn";
            VehicleColumn.ReadOnly = true;
            // 
            // OdometerColumn
            // 
            OdometerColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            OdometerColumn.DataPropertyName = "OdometerMiles";
            OdometerColumn.HeaderText = "Odometer";
            OdometerColumn.MinimumWidth = 9;
            OdometerColumn.Name = "OdometerColumn";
            OdometerColumn.ReadOnly = true;
            OdometerColumn.Width = 148;
            // 
            // StartColumn
            // 
            StartColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            StartColumn.DataPropertyName = "StartTime";
            StartColumn.HeaderText = "Start Time";
            StartColumn.MinimumWidth = 9;
            StartColumn.Name = "StartColumn";
            StartColumn.ReadOnly = true;
            StartColumn.Width = 147;
            // 
            // StartSocColumn
            // 
            StartSocColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            StartSocColumn.DataPropertyName = "StartSoc";
            StartSocColumn.HeaderText = "Start SOC";
            StartSocColumn.MinimumWidth = 9;
            StartSocColumn.Name = "StartSocColumn";
            StartSocColumn.ReadOnly = true;
            StartSocColumn.Width = 142;
            // 
            // EndTimeColumn
            // 
            EndTimeColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            EndTimeColumn.DataPropertyName = "EndTime";
            EndTimeColumn.HeaderText = "End Time";
            EndTimeColumn.MinimumWidth = 9;
            EndTimeColumn.Name = "EndTimeColumn";
            EndTimeColumn.ReadOnly = true;
            EndTimeColumn.Width = 140;
            // 
            // EndSocColumn
            // 
            EndSocColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            EndSocColumn.DataPropertyName = "EndSoc";
            EndSocColumn.HeaderText = "End SOC";
            EndSocColumn.MinimumWidth = 9;
            EndSocColumn.Name = "EndSocColumn";
            EndSocColumn.ReadOnly = true;
            EndSocColumn.Width = 135;
            // 
            // ChargeKhwColumn
            // 
            ChargeKhwColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            ChargeKhwColumn.DataPropertyName = "KwhAdded";
            ChargeKhwColumn.HeaderText = "Charge kWh";
            ChargeKhwColumn.MinimumWidth = 9;
            ChargeKhwColumn.Name = "ChargeKhwColumn";
            ChargeKhwColumn.ReadOnly = true;
            ChargeKhwColumn.Width = 168;
            // 
            // EnergyDrawKwhColumn
            // 
            EnergyDrawKwhColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            EnergyDrawKwhColumn.DataPropertyName = "KwhDrawn";
            EnergyDrawKwhColumn.HeaderText = "Drawn kWh";
            EnergyDrawKwhColumn.MinimumWidth = 9;
            EnergyDrawKwhColumn.Name = "EnergyDrawKwhColumn";
            EnergyDrawKwhColumn.ReadOnly = true;
            EnergyDrawKwhColumn.Width = 162;
            // 
            // IsHomeChargeColumn
            // 
            IsHomeChargeColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            IsHomeChargeColumn.DataPropertyName = "IsHomeCharge";
            IsHomeChargeColumn.HeaderText = "Home";
            IsHomeChargeColumn.MinimumWidth = 9;
            IsHomeChargeColumn.Name = "IsHomeChargeColumn";
            IsHomeChargeColumn.ReadOnly = true;
            IsHomeChargeColumn.Resizable = DataGridViewTriState.True;
            IsHomeChargeColumn.SortMode = DataGridViewColumnSortMode.Automatic;
            IsHomeChargeColumn.Width = 110;
            // 
            // SolarColumn
            // 
            SolarColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            SolarColumn.DataPropertyName = "SolarKwh";
            SolarColumn.HeaderText = "Solar kWh";
            SolarColumn.MinimumWidth = 9;
            SolarColumn.Name = "SolarColumn";
            SolarColumn.ReadOnly = true;
            SolarColumn.Width = 148;
            // 
            // BatteryColumn
            // 
            BatteryColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            BatteryColumn.DataPropertyName = "BatteryKwh";
            BatteryColumn.HeaderText = "Battery kWh";
            BatteryColumn.MinimumWidth = 9;
            BatteryColumn.Name = "BatteryColumn";
            BatteryColumn.ReadOnly = true;
            BatteryColumn.Width = 167;
            // 
            // GridColumn
            // 
            GridColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            GridColumn.DataPropertyName = "GridKwh";
            GridColumn.HeaderText = "Grid kWh";
            GridColumn.MinimumWidth = 9;
            GridColumn.Name = "GridColumn";
            GridColumn.ReadOnly = true;
            GridColumn.Width = 140;
            // 
            // CostColumn
            // 
            CostColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            CostColumn.DataPropertyName = "SessionCost";
            CostColumn.HeaderText = "Cost";
            CostColumn.MinimumWidth = 9;
            CostColumn.Name = "CostColumn";
            CostColumn.ReadOnly = true;
            CostColumn.Width = 95;
            // 
            // EvChargingSessionManagerControl
            // 
            AutoScaleDimensions = new SizeF(12F, 30F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(_gridSessions);
            Name = "EvChargingSessionManagerControl";
            Size = new Size(1887, 881);
            Load += OnLoad;
            ((System.ComponentModel.ISupportInitialize)_gridSessions).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private DataGridView _gridSessions;
        private DataGridViewTextBoxColumn VehicleColumn;
        private DataGridViewTextBoxColumn OdometerColumn;
        private DataGridViewTextBoxColumn StartColumn;
        private DataGridViewTextBoxColumn StartSocColumn;
        private DataGridViewTextBoxColumn EndTimeColumn;
        private DataGridViewTextBoxColumn EndSocColumn;
        private DataGridViewTextBoxColumn ChargeKhwColumn;
        private DataGridViewTextBoxColumn EnergyDrawKwhColumn;
        private DataGridViewCheckBoxColumn IsHomeChargeColumn;
        private DataGridViewTextBoxColumn SolarColumn;
        private DataGridViewTextBoxColumn BatteryColumn;
        private DataGridViewTextBoxColumn GridColumn;
        private DataGridViewTextBoxColumn CostColumn;
    }
}
