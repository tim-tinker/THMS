namespace THMS.UI.WinForms.Controls
{
    partial class HomeCircuitManagerControl
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
            splitHomeCircuit = new SplitContainer();
            gridHomeCircuitDays = new DataGridView();
            ColumnDate = new DataGridViewTextBoxColumn();
            ColumnIntervalCount = new DataGridViewTextBoxColumn();
            ColumnDayEnergy = new DataGridViewTextBoxColumn();
            gridHomeCircuit = new DataGridView();
            ColumnTimestamp = new DataGridViewTextBoxColumn();
            ColumnEnergy = new DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)splitHomeCircuit).BeginInit();
            splitHomeCircuit.Panel1.SuspendLayout();
            splitHomeCircuit.Panel2.SuspendLayout();
            splitHomeCircuit.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)gridHomeCircuitDays).BeginInit();
            ((System.ComponentModel.ISupportInitialize)gridHomeCircuit).BeginInit();
            SuspendLayout();
            // 
            // splitHomeCircuit
            // 
            splitHomeCircuit.Dock = DockStyle.Fill;
            splitHomeCircuit.Location = new Point(0, 0);
            splitHomeCircuit.Margin = new Padding(0);
            splitHomeCircuit.Name = "splitHomeCircuit";
            splitHomeCircuit.Orientation = Orientation.Vertical;
            splitHomeCircuit.Panel1.Controls.Add(gridHomeCircuitDays);
            splitHomeCircuit.Panel2.Controls.Add(gridHomeCircuit);
            splitHomeCircuit.Size = new Size(1080, 792);
            splitHomeCircuit.TabIndex = 0;
            // 
            // gridHomeCircuitDays
            // 
            gridHomeCircuitDays.AllowUserToAddRows = false;
            gridHomeCircuitDays.AllowUserToDeleteRows = false;
            gridHomeCircuitDays.AllowUserToResizeRows = false;
            gridHomeCircuitDays.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gridHomeCircuitDays.Columns.AddRange(new DataGridViewColumn[] { ColumnDate, ColumnIntervalCount, ColumnDayEnergy });
            gridHomeCircuitDays.Dock = DockStyle.Fill;
            gridHomeCircuitDays.Location = new Point(0, 0);
            gridHomeCircuitDays.Margin = new Padding(4);
            gridHomeCircuitDays.Name = "gridHomeCircuitDays";
            gridHomeCircuitDays.ReadOnly = true;
            gridHomeCircuitDays.RowHeadersVisible = false;
            gridHomeCircuitDays.RowHeadersWidth = 72;
            gridHomeCircuitDays.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridHomeCircuitDays.Size = new Size(1080, 280);
            gridHomeCircuitDays.TabIndex = 0;
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
            // ColumnIntervalCount
            // 
            ColumnIntervalCount.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            ColumnIntervalCount.DataPropertyName = "IntervalCount";
            ColumnIntervalCount.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            ColumnIntervalCount.HeaderText = "Intervals";
            ColumnIntervalCount.MinimumWidth = 9;
            ColumnIntervalCount.Name = "ColumnIntervalCount";
            ColumnIntervalCount.ReadOnly = true;
            // 
            // ColumnDayEnergy
            // 
            ColumnDayEnergy.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            ColumnDayEnergy.DataPropertyName = "KiloWattHours";
            ColumnDayEnergy.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            ColumnDayEnergy.DefaultCellStyle.Format = "0.##";
            ColumnDayEnergy.HeaderText = "Energy (kWh)";
            ColumnDayEnergy.MinimumWidth = 9;
            ColumnDayEnergy.Name = "ColumnDayEnergy";
            ColumnDayEnergy.ReadOnly = true;
            // 
            // gridHomeCircuit
            // 
            gridHomeCircuit.AllowUserToAddRows = false;
            gridHomeCircuit.AllowUserToDeleteRows = false;
            gridHomeCircuit.AllowUserToResizeRows = false;
            gridHomeCircuit.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gridHomeCircuit.Columns.AddRange(new DataGridViewColumn[] { ColumnTimestamp, ColumnEnergy });
            gridHomeCircuit.Dock = DockStyle.Fill;
            gridHomeCircuit.Location = new Point(0, 0);
            gridHomeCircuit.Margin = new Padding(4);
            gridHomeCircuit.Name = "gridHomeCircuit";
            gridHomeCircuit.RowHeadersVisible = false;
            gridHomeCircuit.RowHeadersWidth = 72;
            gridHomeCircuit.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridHomeCircuit.Size = new Size(1080, 480);
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
            ColumnEnergy.DataPropertyName = "KiloWattHours";
            ColumnEnergy.HeaderText = "Energy (kWh)";
            ColumnEnergy.MinimumWidth = 9;
            ColumnEnergy.Name = "ColumnEnergy";
            // 
            // HomeCircuitManagerControl
            // 
            AutoScaleDimensions = new SizeF(12F, 30F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(splitHomeCircuit);
            Margin = new Padding(4);
            Name = "HomeCircuitManagerControl";
            Size = new Size(1080, 792);
            Load += OnLoad;
            splitHomeCircuit.Panel1.ResumeLayout(false);
            splitHomeCircuit.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)gridHomeCircuitDays).EndInit();
            ((System.ComponentModel.ISupportInitialize)gridHomeCircuit).EndInit();
            ((System.ComponentModel.ISupportInitialize)splitHomeCircuit).EndInit();
            splitHomeCircuit.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private SplitContainer splitHomeCircuit;
        private DataGridView gridHomeCircuitDays;
        private DataGridView gridHomeCircuit;
        private DataGridViewTextBoxColumn ColumnDate;
        private DataGridViewTextBoxColumn ColumnIntervalCount;
        private DataGridViewTextBoxColumn ColumnDayEnergy;
        private DataGridViewTextBoxColumn ColumnTimestamp;
        private DataGridViewTextBoxColumn ColumnEnergy;
    }
}
