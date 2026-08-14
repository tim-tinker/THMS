namespace THMS.UI.WinForms.Controls
{
    partial class HomeCircuitAttributionManagerControl
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.DataGridView gridHomeCircuit;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();

            base.Dispose(disposing);
        }

        #region Designer generated code

        private void InitializeComponent()
        {
            gridHomeCircuit = new DataGridView();
            ColumnTimestamp = new DataGridViewTextBoxColumn();
            ColumnEnergy = new DataGridViewTextBoxColumn();
            SolarColumn = new DataGridViewTextBoxColumn();
            BatteryColumn = new DataGridViewTextBoxColumn();
            GridColumn = new DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)gridHomeCircuit).BeginInit();
            SuspendLayout();
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
            gridHomeCircuit.MultiSelect = false;
            gridHomeCircuit.Name = "gridHomeCircuit";
            gridHomeCircuit.RowHeadersVisible = false;
            gridHomeCircuit.RowHeadersWidth = 72;
            gridHomeCircuit.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridHomeCircuit.Size = new Size(1080, 792);
            gridHomeCircuit.TabIndex = 0;
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
            ColumnEnergy.HeaderText = "Energy (kWh)";
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
            Controls.Add(gridHomeCircuit);
            Margin = new Padding(4);
            Name = "HomeCircuitAttributionManagerControl";
            Size = new Size(1080, 792);
            Load += OnLoad;
            ((System.ComponentModel.ISupportInitialize)gridHomeCircuit).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private DataGridViewTextBoxColumn ColumnTimestamp;
        private DataGridViewTextBoxColumn ColumnEnergy;
        private DataGridViewTextBoxColumn SolarColumn;
        private DataGridViewTextBoxColumn BatteryColumn;
        private DataGridViewTextBoxColumn GridColumn;
    }
}
