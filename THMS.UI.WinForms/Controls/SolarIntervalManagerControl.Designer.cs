namespace THMS.UI.WinForms.Controls
{
    partial class SolarIntervalManagerControl
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.DataGridView gridSolarIntervals;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();

            base.Dispose(disposing);
        }

        #region Designer generated code

        private void InitializeComponent()
        {
            gridSolarIntervals = new DataGridView();
            ColumnTimestamp = new DataGridViewTextBoxColumn();
            ColumnProduced = new DataGridViewTextBoxColumn();
            ColumnConsumed = new DataGridViewTextBoxColumn();
            ColumnExported = new DataGridViewTextBoxColumn();
            ColumnImported = new DataGridViewTextBoxColumn();
            ColumnCharged = new DataGridViewTextBoxColumn();
            ColumnDischarged = new DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)gridSolarIntervals).BeginInit();
            SuspendLayout();
            // 
            // gridSolarIntervals
            // 
            gridSolarIntervals.AllowUserToAddRows = false;
            gridSolarIntervals.AllowUserToDeleteRows = false;
            gridSolarIntervals.AllowUserToResizeRows = false;
            gridSolarIntervals.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gridSolarIntervals.Columns.AddRange(new DataGridViewColumn[] { ColumnTimestamp, ColumnProduced, ColumnConsumed, ColumnExported, ColumnImported, ColumnCharged, ColumnDischarged });
            gridSolarIntervals.Dock = DockStyle.Fill;
            gridSolarIntervals.Location = new Point(0, 0);
            gridSolarIntervals.Margin = new Padding(4);
            gridSolarIntervals.MultiSelect = false;
            gridSolarIntervals.Name = "gridSolarIntervals";
            gridSolarIntervals.RowHeadersVisible = false;
            gridSolarIntervals.RowHeadersWidth = 72;
            gridSolarIntervals.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridSolarIntervals.Size = new Size(1080, 792);
            gridSolarIntervals.TabIndex = 0;
            // 
            // ColumnTimestamp
            // 
            ColumnTimestamp.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            ColumnTimestamp.DataPropertyName = "Timestamp";
            ColumnTimestamp.HeaderText = "Timestamp";
            ColumnTimestamp.MinimumWidth = 9;
            ColumnTimestamp.Name = "ColumnTimestamp";
            // 
            // ColumnProduced
            // 
            ColumnProduced.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            ColumnProduced.DataPropertyName = "EnergyProducedWh";
            ColumnProduced.HeaderText = "Produced";
            ColumnProduced.MinimumWidth = 9;
            ColumnProduced.Name = "ColumnProduced";
            ColumnProduced.Width = 142;
            // 
            // ColumnConsumed
            // 
            ColumnConsumed.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            ColumnConsumed.DataPropertyName = "EnergyConsumedWh";
            ColumnConsumed.HeaderText = "Consumed";
            ColumnConsumed.MinimumWidth = 9;
            ColumnConsumed.Name = "ColumnConsumed";
            ColumnConsumed.Width = 153;
            // 
            // ColumnExported
            // 
            ColumnExported.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            ColumnExported.DataPropertyName = "ExportedToGridWh";
            ColumnExported.HeaderText = "Exported";
            ColumnExported.MinimumWidth = 9;
            ColumnExported.Name = "ColumnExported";
            ColumnExported.Width = 136;
            // 
            // ColumnImported
            // 
            ColumnImported.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            ColumnImported.DataPropertyName = "ImportedFromGridWh";
            ColumnImported.HeaderText = "Imported";
            ColumnImported.MinimumWidth = 9;
            ColumnImported.Name = "ColumnImported";
            ColumnImported.Width = 139;
            // 
            // ColumnCharged
            // 
            ColumnCharged.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            ColumnCharged.DataPropertyName = "StoredInBatteriesWh";
            ColumnCharged.HeaderText = "Charged";
            ColumnCharged.MinimumWidth = 9;
            ColumnCharged.Name = "ColumnCharged";
            ColumnCharged.Width = 132;
            // 
            // ColumnDischarged
            // 
            ColumnDischarged.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            ColumnDischarged.DataPropertyName = "DischargedFromBatteriesWh";
            ColumnDischarged.HeaderText = "Discharged";
            ColumnDischarged.MinimumWidth = 9;
            ColumnDischarged.Name = "ColumnDischarged";
            ColumnDischarged.Width = 158;
            // 
            // SolarIntervalManagerControl
            // 
            AutoScaleDimensions = new SizeF(12F, 30F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(gridSolarIntervals);
            Margin = new Padding(4);
            Name = "SolarIntervalManagerControl";
            Size = new Size(1080, 792);
            Load += OnLoad;
            ((System.ComponentModel.ISupportInitialize)gridSolarIntervals).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private DataGridViewTextBoxColumn ColumnTimestamp;
        private DataGridViewTextBoxColumn ColumnProduced;
        private DataGridViewTextBoxColumn ColumnConsumed;
        private DataGridViewTextBoxColumn ColumnExported;
        private DataGridViewTextBoxColumn ColumnImported;
        private DataGridViewTextBoxColumn ColumnCharged;
        private DataGridViewTextBoxColumn ColumnDischarged;
    }
}
