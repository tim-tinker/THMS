namespace THMS.UI.WinForms.Controls
{
    partial class TransactionManagerControl
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.DataGridView masterGrid;
        private System.Windows.Forms.DataGridView detailGrid;
        private System.Windows.Forms.SplitContainer splitContainer;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();

            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle4 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle5 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle6 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle7 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle8 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle9 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle10 = new DataGridViewCellStyle();
            splitContainer = new SplitContainer();
            masterGrid = new DataGridView();
            NameColumn = new DataGridViewTextBoxColumn();
            AccountTypeColumn = new DataGridViewTextBoxColumn();
            AsOfDateColumn = new DataGridViewTextBoxColumn();
            BalanceColumn = new DataGridViewTextBoxColumn();
            DueDateColumn = new DataGridViewTextBoxColumn();
            AvailableColumn = new DataGridViewTextBoxColumn();
            AprColumn = new DataGridViewTextBoxColumn();
            CreditLimitColumn = new DataGridViewTextBoxColumn();
            MarketValueColumn = new DataGridViewTextBoxColumn();
            detailGrid = new DataGridView();
            DateColumn = new DataGridViewTextBoxColumn();
            AmountColumn = new DataGridViewTextBoxColumn();
            ForecastColumn = new DataGridViewTextBoxColumn();
            CategoryColumn = new DataGridViewTextBoxColumn();
            TypeColumn = new DataGridViewTextBoxColumn();
            DescriptionColumn = new DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)splitContainer).BeginInit();
            splitContainer.Panel1.SuspendLayout();
            splitContainer.Panel2.SuspendLayout();
            splitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)masterGrid).BeginInit();
            ((System.ComponentModel.ISupportInitialize)detailGrid).BeginInit();
            SuspendLayout();
            // 
            // splitContainer
            // 
            splitContainer.Dock = DockStyle.Fill;
            splitContainer.Location = new Point(0, 0);
            splitContainer.Name = "splitContainer";
            splitContainer.Orientation = Orientation.Horizontal;
            // 
            // splitContainer.Panel1
            // 
            splitContainer.Panel1.Controls.Add(masterGrid);
            // 
            // splitContainer.Panel2
            // 
            splitContainer.Panel2.Controls.Add(detailGrid);
            splitContainer.Size = new Size(1260, 600);
            splitContainer.SplitterDistance = 200;
            splitContainer.TabIndex = 0;
            // 
            // masterGrid
            // 
            masterGrid.AllowUserToAddRows = false;
            masterGrid.AllowUserToDeleteRows = false;
            masterGrid.AllowUserToResizeRows = false;
            masterGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            masterGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            masterGrid.Columns.AddRange(new DataGridViewColumn[] { NameColumn, AccountTypeColumn, AsOfDateColumn, BalanceColumn, DueDateColumn, AvailableColumn, AprColumn, CreditLimitColumn, MarketValueColumn });
            masterGrid.Dock = DockStyle.Fill;
            masterGrid.Location = new Point(0, 0);
            masterGrid.MultiSelect = false;
            masterGrid.Name = "masterGrid";
            masterGrid.ReadOnly = true;
            masterGrid.RowHeadersVisible = false;
            masterGrid.RowHeadersWidth = 72;
            masterGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            masterGrid.Size = new Size(1260, 200);
            masterGrid.TabIndex = 1;
            // 
            // NameColumn
            // 
            NameColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            NameColumn.DataPropertyName = "Name";
            NameColumn.HeaderText = "Account";
            NameColumn.MinimumWidth = 9;
            NameColumn.Name = "NameColumn";
            NameColumn.ReadOnly = true;
            // 
            // AccountTypeColumn
            // 
            AccountTypeColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            AccountTypeColumn.DataPropertyName = "AccountType";
            AccountTypeColumn.HeaderText = "Type";
            AccountTypeColumn.MinimumWidth = 9;
            AccountTypeColumn.Name = "AccountTypeColumn";
            AccountTypeColumn.ReadOnly = true;
            AccountTypeColumn.ToolTipText = "Type of Account";
            AccountTypeColumn.Width = 97;
            // 
            // AsOfDateColumn
            // 
            AsOfDateColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            AsOfDateColumn.DataPropertyName = "AsOfDate";
            dataGridViewCellStyle1.Format = "d";
            AsOfDateColumn.DefaultCellStyle = dataGridViewCellStyle1;
            AsOfDateColumn.HeaderText = "As Of";
            AsOfDateColumn.MinimumWidth = 9;
            AsOfDateColumn.Name = "AsOfDateColumn";
            AsOfDateColumn.ReadOnly = true;
            AsOfDateColumn.ToolTipText = "Date for the balance";
            AsOfDateColumn.Width = 78;
            // 
            // BalanceColumn
            // 
            BalanceColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            BalanceColumn.DataPropertyName = "Balance";
            dataGridViewCellStyle2.Format = "c2";
            BalanceColumn.DefaultCellStyle = dataGridViewCellStyle2;
            BalanceColumn.HeaderText = "Balance";
            BalanceColumn.MinimumWidth = 9;
            BalanceColumn.Name = "BalanceColumn";
            BalanceColumn.ReadOnly = true;
            BalanceColumn.Width = 126;
            // 
            // DueDateColumn
            // 
            DueDateColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            DueDateColumn.DataPropertyName = "DueDate";
            dataGridViewCellStyle3.Format = "d";
            DueDateColumn.DefaultCellStyle = dataGridViewCellStyle3;
            DueDateColumn.HeaderText = "Payment Due";
            DueDateColumn.MinimumWidth = 9;
            DueDateColumn.Name = "DueDateColumn";
            DueDateColumn.ReadOnly = true;
            DueDateColumn.Width = 164;
            // 
            // AvailableColumn
            // 
            AvailableColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            AvailableColumn.DataPropertyName = "BankCreditAvailable";
            dataGridViewCellStyle4.Format = "c2";
            AvailableColumn.DefaultCellStyle = dataGridViewCellStyle4;
            AvailableColumn.HeaderText = "Available";
            AvailableColumn.MinimumWidth = 9;
            AvailableColumn.Name = "AvailableColumn";
            AvailableColumn.ReadOnly = true;
            AvailableColumn.ToolTipText = "For bank or credit accounts";
            AvailableColumn.Width = 138;
            // 
            // AprColumn
            // 
            AprColumn.DataPropertyName = "APR";
            dataGridViewCellStyle5.Format = "N2";
            dataGridViewCellStyle5.NullValue = null;
            AprColumn.DefaultCellStyle = dataGridViewCellStyle5;
            AprColumn.HeaderText = "APR";
            AprColumn.MinimumWidth = 9;
            AprColumn.Name = "AprColumn";
            AprColumn.ReadOnly = true;
            // 
            // CreditLimitColumn
            // 
            CreditLimitColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            CreditLimitColumn.DataPropertyName = "CreditLimit";
            dataGridViewCellStyle6.Format = "c2";
            CreditLimitColumn.DefaultCellStyle = dataGridViewCellStyle6;
            CreditLimitColumn.HeaderText = "Credit Limit";
            CreditLimitColumn.MinimumWidth = 9;
            CreditLimitColumn.Name = "CreditLimitColumn";
            CreditLimitColumn.ReadOnly = true;
            CreditLimitColumn.Width = 148;
            // 
            // MarketValueColumn
            // 
            MarketValueColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            MarketValueColumn.DataPropertyName = "MarketValue";
            dataGridViewCellStyle7.Format = "c2";
            MarketValueColumn.DefaultCellStyle = dataGridViewCellStyle7;
            MarketValueColumn.HeaderText = "Market Value";
            MarketValueColumn.MinimumWidth = 9;
            MarketValueColumn.Name = "MarketValueColumn";
            MarketValueColumn.ReadOnly = true;
            MarketValueColumn.Width = 161;
            // 
            // detailGrid
            // 
            detailGrid.AllowUserToAddRows = false;
            detailGrid.AllowUserToDeleteRows = false;
            detailGrid.AllowUserToResizeRows = false;
            detailGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            detailGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            detailGrid.Columns.AddRange(new DataGridViewColumn[] { DateColumn, AmountColumn, ForecastColumn, CategoryColumn, TypeColumn, DescriptionColumn });
            detailGrid.Dock = DockStyle.Fill;
            detailGrid.Location = new Point(0, 0);
            detailGrid.MultiSelect = false;
            detailGrid.Name = "detailGrid";
            detailGrid.ReadOnly = true;
            detailGrid.RowHeadersVisible = false;
            detailGrid.RowHeadersWidth = 72;
            detailGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            detailGrid.Size = new Size(1260, 396);
            detailGrid.TabIndex = 2;
            // 
            // DateColumn
            // 
            DateColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dataGridViewCellStyle8.Format = "d";
            dataGridViewCellStyle8.NullValue = null;
            DateColumn.DefaultCellStyle = dataGridViewCellStyle8;
            DateColumn.HeaderText = "Date";
            DateColumn.MinimumWidth = 9;
            DateColumn.Name = "DateColumn";
            DateColumn.ReadOnly = true;
            DateColumn.Width = 98;
            // 
            // AmountColumn
            // 
            AmountColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dataGridViewCellStyle9.Format = "c2";
            AmountColumn.DefaultCellStyle = dataGridViewCellStyle9;
            AmountColumn.HeaderText = "Amount";
            AmountColumn.MinimumWidth = 9;
            AmountColumn.Name = "AmountColumn";
            AmountColumn.ReadOnly = true;
            AmountColumn.Width = 129;
            // 
            // ForecastColumn
            // 
            ForecastColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dataGridViewCellStyle10.Format = "c2";
            ForecastColumn.DefaultCellStyle = dataGridViewCellStyle10;
            ForecastColumn.HeaderText = "Balance";
            ForecastColumn.MinimumWidth = 9;
            ForecastColumn.Name = "ForecastColumn";
            ForecastColumn.ReadOnly = true;
            ForecastColumn.Width = 126;
            // 
            // CategoryColumn
            // 
            CategoryColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            CategoryColumn.HeaderText = "Category";
            CategoryColumn.MinimumWidth = 9;
            CategoryColumn.Name = "CategoryColumn";
            CategoryColumn.ReadOnly = true;
            CategoryColumn.Width = 137;
            // 
            // TypeColumn
            // 
            TypeColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            TypeColumn.HeaderText = "Type";
            TypeColumn.MinimumWidth = 9;
            TypeColumn.Name = "TypeColumn";
            TypeColumn.ReadOnly = true;
            TypeColumn.Width = 97;
            // 
            // DescriptionColumn
            // 
            DescriptionColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            DescriptionColumn.HeaderText = "Description";
            DescriptionColumn.MinimumWidth = 9;
            DescriptionColumn.Name = "DescriptionColumn";
            DescriptionColumn.ReadOnly = true;
            // 
            // TransactionManagerControl
            // 
            AutoScaleDimensions = new SizeF(12F, 30F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(splitContainer);
            Name = "TransactionManagerControl";
            Size = new Size(1260, 600);
            splitContainer.Panel1.ResumeLayout(false);
            splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer).EndInit();
            splitContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)masterGrid).EndInit();
            ((System.ComponentModel.ISupportInitialize)detailGrid).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private DataGridViewTextBoxColumn DateColumn;
        private DataGridViewTextBoxColumn AmountColumn;
        private DataGridViewTextBoxColumn ForecastColumn;
        private DataGridViewTextBoxColumn CategoryColumn;
        private DataGridViewTextBoxColumn TypeColumn;
        private DataGridViewTextBoxColumn DescriptionColumn;
        private DataGridViewTextBoxColumn NameColumn;
        private DataGridViewTextBoxColumn AccountTypeColumn;
        private DataGridViewTextBoxColumn AsOfDateColumn;
        private DataGridViewTextBoxColumn BalanceColumn;
        private DataGridViewTextBoxColumn DueDateColumn;
        private DataGridViewTextBoxColumn AvailableColumn;
        private DataGridViewTextBoxColumn AprColumn;
        private DataGridViewTextBoxColumn CreditLimitColumn;
        private DataGridViewTextBoxColumn MarketValueColumn;
    }
}
