namespace THMS.UI.WinForms.Controls
{
    partial class ElectricContractManagerControl
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
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle4 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle5 = new DataGridViewCellStyle();
            _gridContracts = new DataGridView();
            NameColumn = new DataGridViewTextBoxColumn();
            StartColumn = new DataGridViewTextBoxColumn();
            EndColumn = new DataGridViewTextBoxColumn();
            BaseEnergyChargeColumn = new DataGridViewTextBoxColumn();
            EnergyChargeColumn = new DataGridViewTextBoxColumn();
            BaseDeliveryColumn = new DataGridViewTextBoxColumn();
            DeliveryChargeColumn = new DataGridViewTextBoxColumn();
            ExportCreditColumn = new DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)_gridContracts).BeginInit();
            SuspendLayout();
            // 
            // _gridContracts
            // 
            _gridContracts.AllowUserToAddRows = false;
            _gridContracts.AllowUserToDeleteRows = false;
            _gridContracts.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            _gridContracts.Columns.AddRange(new DataGridViewColumn[] { NameColumn, StartColumn, EndColumn, BaseEnergyChargeColumn, EnergyChargeColumn, BaseDeliveryColumn, DeliveryChargeColumn, ExportCreditColumn });
            _gridContracts.Dock = DockStyle.Fill;
            _gridContracts.Location = new Point(0, 0);
            _gridContracts.Name = "_gridContracts";
            _gridContracts.ReadOnly = true;
            _gridContracts.RowHeadersVisible = false;
            _gridContracts.RowHeadersWidth = 72;
            _gridContracts.Size = new Size(1318, 730);
            _gridContracts.TabIndex = 0;
            // 
            // NameColumn
            // 
            NameColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            NameColumn.DataPropertyName = "Name";
            NameColumn.HeaderText = "Contract Name";
            NameColumn.MinimumWidth = 9;
            NameColumn.Name = "NameColumn";
            NameColumn.ReadOnly = true;
            // 
            // StartColumn
            // 
            StartColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            StartColumn.DataPropertyName = "StartDate";
            StartColumn.HeaderText = "Effective";
            StartColumn.MinimumWidth = 9;
            StartColumn.Name = "StartColumn";
            StartColumn.ReadOnly = true;
            StartColumn.Width = 133;
            // 
            // EndColumn
            // 
            EndColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            EndColumn.DataPropertyName = "EndDate";
            EndColumn.HeaderText = "Expires";
            EndColumn.MinimumWidth = 9;
            EndColumn.Name = "EndColumn";
            EndColumn.ReadOnly = true;
            EndColumn.Width = 119;
            // 
            // BaseEnergyChargeColumn
            // 
            BaseEnergyChargeColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            BaseEnergyChargeColumn.DataPropertyName = "BaseEnergyCharge";
            dataGridViewCellStyle1.Format = "C2";
            dataGridViewCellStyle1.NullValue = null;
            BaseEnergyChargeColumn.DefaultCellStyle = dataGridViewCellStyle1;
            BaseEnergyChargeColumn.HeaderText = "Base Charge";
            BaseEnergyChargeColumn.MinimumWidth = 9;
            BaseEnergyChargeColumn.Name = "BaseEnergyChargeColumn";
            BaseEnergyChargeColumn.ReadOnly = true;
            BaseEnergyChargeColumn.Width = 156;
            // 
            // EnergyChargeColumn
            // 
            EnergyChargeColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            EnergyChargeColumn.DataPropertyName = "EnergyChargeRate";
            dataGridViewCellStyle2.Format = "C6";
            EnergyChargeColumn.DefaultCellStyle = dataGridViewCellStyle2;
            EnergyChargeColumn.HeaderText = "Energy Charge";
            EnergyChargeColumn.MinimumWidth = 9;
            EnergyChargeColumn.Name = "EnergyChargeColumn";
            EnergyChargeColumn.ReadOnly = true;
            EnergyChargeColumn.Width = 174;
            // 
            // BaseDeliveryColumn
            // 
            BaseDeliveryColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            BaseDeliveryColumn.DataPropertyName = "BaseDeliveryCharge";
            dataGridViewCellStyle3.Format = "C2";
            BaseDeliveryColumn.DefaultCellStyle = dataGridViewCellStyle3;
            BaseDeliveryColumn.HeaderText = "Delivery Base";
            BaseDeliveryColumn.MinimumWidth = 9;
            BaseDeliveryColumn.Name = "BaseDeliveryColumn";
            BaseDeliveryColumn.ReadOnly = true;
            BaseDeliveryColumn.Width = 163;
            // 
            // DeliveryChargeColumn
            // 
            DeliveryChargeColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            DeliveryChargeColumn.DataPropertyName = "DeliveryChargeRate";
            dataGridViewCellStyle4.Format = "C6";
            DeliveryChargeColumn.DefaultCellStyle = dataGridViewCellStyle4;
            DeliveryChargeColumn.HeaderText = "Delivery Charge";
            DeliveryChargeColumn.MinimumWidth = 9;
            DeliveryChargeColumn.Name = "DeliveryChargeColumn";
            DeliveryChargeColumn.ReadOnly = true;
            DeliveryChargeColumn.Width = 184;
            // 
            // ExportCreditColumn
            // 
            ExportCreditColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            ExportCreditColumn.DataPropertyName = "ExportCreditRate";
            dataGridViewCellStyle5.Format = "C6";
            ExportCreditColumn.DefaultCellStyle = dataGridViewCellStyle5;
            ExportCreditColumn.HeaderText = "Credit Rate";
            ExportCreditColumn.MinimumWidth = 9;
            ExportCreditColumn.Name = "ExportCreditColumn";
            ExportCreditColumn.ReadOnly = true;
            ExportCreditColumn.Width = 145;
            // 
            // ElectricContractManagerControl
            // 
            AutoScaleDimensions = new SizeF(12F, 30F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(_gridContracts);
            Name = "ElectricContractManagerControl";
            Size = new Size(1318, 730);
            VisibleChanged += OnVisibleChanged;
            ((System.ComponentModel.ISupportInitialize)_gridContracts).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private DataGridView _gridContracts;
        private DataGridViewTextBoxColumn NameColumn;
        private DataGridViewTextBoxColumn StartColumn;
        private DataGridViewTextBoxColumn EndColumn;
        private DataGridViewTextBoxColumn BaseEnergyChargeColumn;
        private DataGridViewTextBoxColumn EnergyChargeColumn;
        private DataGridViewTextBoxColumn BaseDeliveryColumn;
        private DataGridViewTextBoxColumn DeliveryChargeColumn;
        private DataGridViewTextBoxColumn ExportCreditColumn;
    }
}
