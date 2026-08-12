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
            menuStrip1 = new MenuStrip();
            loadDataToolStripMenuItem = new ToolStripMenuItem();
            _menuItemMonth = new ToolStripMenuItem();
            _menuItemYear = new ToolStripMenuItem();
            _menuItemLifetime = new ToolStripMenuItem();
            editToolStripMenuItem = new ToolStripMenuItem();
            _menuItemAdd = new ToolStripMenuItem();
            _menuItemEdit = new ToolStripMenuItem();
            _menuItemDelete = new ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)gridSolarIntervals).BeginInit();
            menuStrip1.SuspendLayout();
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
            gridSolarIntervals.Location = new Point(0, 42);
            gridSolarIntervals.Margin = new Padding(4);
            gridSolarIntervals.MultiSelect = false;
            gridSolarIntervals.Name = "gridSolarIntervals";
            gridSolarIntervals.RowHeadersVisible = false;
            gridSolarIntervals.RowHeadersWidth = 72;
            gridSolarIntervals.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridSolarIntervals.Size = new Size(1080, 750);
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
            // menuStrip1
            // 
            menuStrip1.ImageScalingSize = new Size(28, 28);
            menuStrip1.Items.AddRange(new ToolStripItem[] { loadDataToolStripMenuItem, editToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(1080, 42);
            menuStrip1.TabIndex = 5;
            menuStrip1.Text = "menuStrip1";
            // 
            // loadDataToolStripMenuItem
            // 
            loadDataToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { _menuItemMonth, _menuItemYear, _menuItemLifetime });
            loadDataToolStripMenuItem.Name = "loadDataToolStripMenuItem";
            loadDataToolStripMenuItem.Size = new Size(126, 34);
            loadDataToolStripMenuItem.Text = "Load Data";
            // 
            // _menuItemMonth
            // 
            _menuItemMonth.Name = "_menuItemMonth";
            _menuItemMonth.Size = new Size(315, 40);
            _menuItemMonth.Text = "Month";
            _menuItemMonth.Click += OnClickMonth;
            // 
            // _menuItemYear
            // 
            _menuItemYear.Name = "_menuItemYear";
            _menuItemYear.Size = new Size(315, 40);
            _menuItemYear.Text = "Year";
            _menuItemYear.Click += OnClickYear;
            // 
            // _menuItemLifetime
            // 
            _menuItemLifetime.Name = "_menuItemLifetime";
            _menuItemLifetime.Size = new Size(315, 40);
            _menuItemLifetime.Text = "Lifetime";
            _menuItemLifetime.Click += OnClickLifetime;
            // 
            // editToolStripMenuItem
            // 
            editToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { _menuItemAdd, _menuItemEdit, _menuItemDelete });
            editToolStripMenuItem.Name = "editToolStripMenuItem";
            editToolStripMenuItem.Size = new Size(66, 34);
            editToolStripMenuItem.Text = "Edit";
            // 
            // _menuItemAdd
            // 
            _menuItemAdd.Name = "_menuItemAdd";
            _menuItemAdd.Size = new Size(191, 40);
            _menuItemAdd.Text = "Add";
            _menuItemAdd.Click += OnClickAdd;
            // 
            // _menuItemEdit
            // 
            _menuItemEdit.Name = "_menuItemEdit";
            _menuItemEdit.Size = new Size(191, 40);
            _menuItemEdit.Text = "Edit";
            _menuItemEdit.Click += OnClickEdit;
            // 
            // _menuItemDelete
            // 
            _menuItemDelete.Name = "_menuItemDelete";
            _menuItemDelete.Size = new Size(191, 40);
            _menuItemDelete.Text = "Delete";
            _menuItemDelete.Click += OnClickDelete;
            // 
            // SolarIntervalManagerControl
            // 
            AutoScaleDimensions = new SizeF(12F, 30F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(gridSolarIntervals);
            Controls.Add(menuStrip1);
            Margin = new Padding(4);
            Name = "SolarIntervalManagerControl";
            Size = new Size(1080, 792);
            Load += OnLoad;
            ((System.ComponentModel.ISupportInitialize)gridSolarIntervals).EndInit();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DataGridViewTextBoxColumn ColumnTimestamp;
        private DataGridViewTextBoxColumn ColumnProduced;
        private DataGridViewTextBoxColumn ColumnConsumed;
        private DataGridViewTextBoxColumn ColumnExported;
        private DataGridViewTextBoxColumn ColumnImported;
        private DataGridViewTextBoxColumn ColumnCharged;
        private DataGridViewTextBoxColumn ColumnDischarged;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem loadDataToolStripMenuItem;
        private ToolStripMenuItem _menuItemMonth;
        private ToolStripMenuItem _menuItemYear;
        private ToolStripMenuItem _menuItemLifetime;
        private ToolStripMenuItem editToolStripMenuItem;
        private ToolStripMenuItem _menuItemAdd;
        private ToolStripMenuItem _menuItemEdit;
        private ToolStripMenuItem _menuItemDelete;
    }
}
