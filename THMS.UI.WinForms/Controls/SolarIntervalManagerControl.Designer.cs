namespace THMS.UI.WinForms.Controls
{
    partial class SolarIntervalManagerControl
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
            splitSolar = new SplitContainer();
            gridSolarMonths = new DataGridView();
            ColumnMonth = new DataGridViewTextBoxColumn();
            ColumnDayCount = new DataGridViewTextBoxColumn();
            ColumnMonthProduced = new DataGridViewTextBoxColumn();
            ColumnMonthConsumed = new DataGridViewTextBoxColumn();
            ColumnMonthExported = new DataGridViewTextBoxColumn();
            ColumnMonthImported = new DataGridViewTextBoxColumn();
            ColumnMonthCharged = new DataGridViewTextBoxColumn();
            ColumnMonthDischarged = new DataGridViewTextBoxColumn();
            gridSolarIntervals = new DataGridView();
            ColumnTimestamp = new DataGridViewTextBoxColumn();
            ColumnProduced = new DataGridViewTextBoxColumn();
            ColumnConsumed = new DataGridViewTextBoxColumn();
            ColumnExported = new DataGridViewTextBoxColumn();
            ColumnImported = new DataGridViewTextBoxColumn();
            ColumnCharged = new DataGridViewTextBoxColumn();
            ColumnDischarged = new DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)splitSolar).BeginInit();
            splitSolar.Panel1.SuspendLayout();
            splitSolar.Panel2.SuspendLayout();
            splitSolar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)gridSolarMonths).BeginInit();
            ((System.ComponentModel.ISupportInitialize)gridSolarIntervals).BeginInit();
            SuspendLayout();
            // 
            // splitSolar
            // 
            splitSolar.Dock = DockStyle.Fill;
            splitSolar.Location = new Point(0, 0);
            splitSolar.Margin = new Padding(0);
            splitSolar.Name = "splitSolar";
            splitSolar.Orientation = Orientation.Vertical;
            splitSolar.Panel1.Controls.Add(gridSolarMonths);
            splitSolar.Panel2.Controls.Add(gridSolarIntervals);
            splitSolar.Size = new Size(1080, 792);
            splitSolar.TabIndex = 0;
            // 
            // gridSolarMonths
            // 
            gridSolarMonths.AllowUserToAddRows = false;
            gridSolarMonths.AllowUserToDeleteRows = false;
            gridSolarMonths.AllowUserToResizeRows = false;
            gridSolarMonths.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gridSolarMonths.Columns.AddRange(new DataGridViewColumn[] { ColumnMonth, ColumnDayCount, ColumnMonthProduced, ColumnMonthConsumed, ColumnMonthExported, ColumnMonthImported, ColumnMonthCharged, ColumnMonthDischarged });
            gridSolarMonths.Dock = DockStyle.Fill;
            gridSolarMonths.Location = new Point(0, 0);
            gridSolarMonths.Margin = new Padding(4);
            gridSolarMonths.Name = "gridSolarMonths";
            gridSolarMonths.ReadOnly = true;
            gridSolarMonths.RowHeadersVisible = false;
            gridSolarMonths.RowHeadersWidth = 72;
            gridSolarMonths.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridSolarMonths.Size = new Size(1080, 280);
            gridSolarMonths.TabIndex = 0;
            // 
            // ColumnMonth
            // 
            ColumnMonth.DataPropertyName = "Month";
            ColumnMonth.DefaultCellStyle.Format = "y";
            ColumnMonth.HeaderText = "Month";
            ColumnMonth.MinimumWidth = 9;
            ColumnMonth.Name = "ColumnMonth";
            ColumnMonth.ReadOnly = true;
            // 
            // ColumnDayCount
            // 
            ColumnDayCount.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            ColumnDayCount.DataPropertyName = "DayCount";
            ColumnDayCount.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            ColumnDayCount.HeaderText = "Days";
            ColumnDayCount.MinimumWidth = 9;
            ColumnDayCount.Name = "ColumnDayCount";
            ColumnDayCount.ReadOnly = true;
            // 
            // ColumnMonthProduced
            // 
            ColumnMonthProduced.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            ColumnMonthProduced.DataPropertyName = "EnergyProducedWh";
            ColumnMonthProduced.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            ColumnMonthProduced.DefaultCellStyle.Format = "0.##";
            ColumnMonthProduced.HeaderText = "Produced";
            ColumnMonthProduced.MinimumWidth = 9;
            ColumnMonthProduced.Name = "ColumnMonthProduced";
            ColumnMonthProduced.ReadOnly = true;
            // 
            // ColumnMonthConsumed
            // 
            ColumnMonthConsumed.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            ColumnMonthConsumed.DataPropertyName = "EnergyConsumedWh";
            ColumnMonthConsumed.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            ColumnMonthConsumed.DefaultCellStyle.Format = "0.##";
            ColumnMonthConsumed.HeaderText = "Consumed";
            ColumnMonthConsumed.MinimumWidth = 9;
            ColumnMonthConsumed.Name = "ColumnMonthConsumed";
            ColumnMonthConsumed.ReadOnly = true;
            // 
            // ColumnMonthExported
            // 
            ColumnMonthExported.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            ColumnMonthExported.DataPropertyName = "ExportedToGridWh";
            ColumnMonthExported.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            ColumnMonthExported.DefaultCellStyle.Format = "0.##";
            ColumnMonthExported.HeaderText = "Exported";
            ColumnMonthExported.MinimumWidth = 9;
            ColumnMonthExported.Name = "ColumnMonthExported";
            ColumnMonthExported.ReadOnly = true;
            // 
            // ColumnMonthImported
            // 
            ColumnMonthImported.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            ColumnMonthImported.DataPropertyName = "ImportedFromGridWh";
            ColumnMonthImported.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            ColumnMonthImported.DefaultCellStyle.Format = "0.##";
            ColumnMonthImported.HeaderText = "Imported";
            ColumnMonthImported.MinimumWidth = 9;
            ColumnMonthImported.Name = "ColumnMonthImported";
            ColumnMonthImported.ReadOnly = true;
            // 
            // ColumnMonthCharged
            // 
            ColumnMonthCharged.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            ColumnMonthCharged.DataPropertyName = "StoredInBatteriesWh";
            ColumnMonthCharged.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            ColumnMonthCharged.DefaultCellStyle.Format = "0.##";
            ColumnMonthCharged.HeaderText = "Charged";
            ColumnMonthCharged.MinimumWidth = 9;
            ColumnMonthCharged.Name = "ColumnMonthCharged";
            ColumnMonthCharged.ReadOnly = true;
            // 
            // ColumnMonthDischarged
            // 
            ColumnMonthDischarged.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            ColumnMonthDischarged.DataPropertyName = "DischargedFromBatteriesWh";
            ColumnMonthDischarged.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            ColumnMonthDischarged.DefaultCellStyle.Format = "0.##";
            ColumnMonthDischarged.HeaderText = "Discharged";
            ColumnMonthDischarged.MinimumWidth = 9;
            ColumnMonthDischarged.Name = "ColumnMonthDischarged";
            ColumnMonthDischarged.ReadOnly = true;
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
            gridSolarIntervals.Name = "gridSolarIntervals";
            gridSolarIntervals.RowHeadersVisible = false;
            gridSolarIntervals.RowHeadersWidth = 72;
            gridSolarIntervals.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridSolarIntervals.Size = new Size(1080, 480);
            gridSolarIntervals.TabIndex = 1;
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
            Controls.Add(splitSolar);
            Margin = new Padding(4);
            Name = "SolarIntervalManagerControl";
            Size = new Size(1080, 792);
            Load += OnLoad;
            splitSolar.Panel1.ResumeLayout(false);
            splitSolar.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)gridSolarMonths).EndInit();
            ((System.ComponentModel.ISupportInitialize)gridSolarIntervals).EndInit();
            ((System.ComponentModel.ISupportInitialize)splitSolar).EndInit();
            splitSolar.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private SplitContainer splitSolar;
        private DataGridView gridSolarMonths;
        private DataGridView gridSolarIntervals;
        private DataGridViewTextBoxColumn ColumnMonth;
        private DataGridViewTextBoxColumn ColumnDayCount;
        private DataGridViewTextBoxColumn ColumnMonthProduced;
        private DataGridViewTextBoxColumn ColumnMonthConsumed;
        private DataGridViewTextBoxColumn ColumnMonthExported;
        private DataGridViewTextBoxColumn ColumnMonthImported;
        private DataGridViewTextBoxColumn ColumnMonthCharged;
        private DataGridViewTextBoxColumn ColumnMonthDischarged;
        private DataGridViewTextBoxColumn ColumnTimestamp;
        private DataGridViewTextBoxColumn ColumnProduced;
        private DataGridViewTextBoxColumn ColumnConsumed;
        private DataGridViewTextBoxColumn ColumnExported;
        private DataGridViewTextBoxColumn ColumnImported;
        private DataGridViewTextBoxColumn ColumnCharged;
        private DataGridViewTextBoxColumn ColumnDischarged;
    }
}
