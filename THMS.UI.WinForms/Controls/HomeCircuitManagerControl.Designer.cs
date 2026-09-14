namespace THMS.UI.WinForms.Controls
{
    partial class HomeCircuitManagerControl
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
            lblStatus = new Label();
            ((System.ComponentModel.ISupportInitialize)gridHomeCircuit).BeginInit();
            SuspendLayout();
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
            ColumnEnergy.DataPropertyName = "KiloWattHours";
            ColumnEnergy.HeaderText = "Energy (kWh)";
            ColumnEnergy.MinimumWidth = 9;
            ColumnEnergy.Name = "ColumnEnergy";
            // 
            // lblStatus
            // 
            lblStatus.Dock = DockStyle.Bottom;
            lblStatus.Height = 24;
            lblStatus.Name = "lblStatus";
            lblStatus.Padding = new Padding(8, 0, 8, 0);
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // HomeCircuitManagerControl
            // 
            AutoScaleDimensions = new SizeF(12F, 30F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(gridHomeCircuit);
            Controls.Add(lblStatus);
            Margin = new Padding(4);
            Name = "HomeCircuitManagerControl";
            Size = new Size(1080, 792);
            Load += OnLoad;
            ((System.ComponentModel.ISupportInitialize)gridHomeCircuit).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private DataGridViewTextBoxColumn ColumnTimestamp;
        private DataGridViewTextBoxColumn ColumnEnergy;
        private Label lblStatus;
    }
}
