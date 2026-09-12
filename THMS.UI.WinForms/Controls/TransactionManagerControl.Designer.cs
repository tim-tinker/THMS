namespace THMS.UI.WinForms.Controls
{
    partial class TransactionManagerControl
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.DataGridView masterGrid;
        private System.Windows.Forms.DataGridView detailGrid;
        private System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.FlowLayoutPanel forecastPanel;
        private System.Windows.Forms.Label lblHistory;
        private System.Windows.Forms.ComboBox cmbHistory;
        private System.Windows.Forms.Label lblForecastPeriod;
        private System.Windows.Forms.ComboBox cmbForecastPeriod;
        private System.Windows.Forms.Label lblShow;
        private System.Windows.Forms.ComboBox cmbShow;
        private System.Windows.Forms.Button btnAddRule;
        private System.Windows.Forms.Button btnDeleteRule;
        private System.Windows.Forms.Button btnSplitTransaction;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();

            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            DataGridViewCellStyle dataGridViewCellStyle7 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle8 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle9 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle4 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle5 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle6 = new DataGridViewCellStyle();
            splitContainer = new SplitContainer();
            masterGrid = new DataGridView();
            detailGrid = new DataGridView();
            DateColumn = new DataGridViewTextBoxColumn();
            AmountColumn = new DataGridViewTextBoxColumn();
            ForecastColumn = new DataGridViewTextBoxColumn();
            CategoryColumn = new DataGridViewTextBoxColumn();
            TypeColumn = new DataGridViewTextBoxColumn();
            DescriptionColumn = new DataGridViewTextBoxColumn();
            forecastPanel = new FlowLayoutPanel();
            lblHistory = new Label();
            cmbHistory = new ComboBox();
            lblForecastPeriod = new Label();
            cmbForecastPeriod = new ComboBox();
            lblShow = new Label();
            cmbShow = new ComboBox();
            btnAddRule = new Button();
            btnDeleteRule = new Button();
            btnSplitTransaction = new Button();
            NameColumn = new DataGridViewTextBoxColumn();
            AccountTypeColumn = new DataGridViewTextBoxColumn();
            AsOfDateColumn = new DataGridViewTextBoxColumn();
            BalanceColumn = new DataGridViewTextBoxColumn();
            DueDateColumn = new DataGridViewTextBoxColumn();
            AvailableColumn = new DataGridViewTextBoxColumn();
            AprColumn = new DataGridViewTextBoxColumn();
            CreditLimitColumn = new DataGridViewTextBoxColumn();
            WebsiteColumn = new DataGridViewLinkColumn();
            ((System.ComponentModel.ISupportInitialize)splitContainer).BeginInit();
            splitContainer.Panel1.SuspendLayout();
            splitContainer.Panel2.SuspendLayout();
            splitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)masterGrid).BeginInit();
            ((System.ComponentModel.ISupportInitialize)detailGrid).BeginInit();
            forecastPanel.SuspendLayout();
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
            splitContainer.Panel2.Controls.Add(forecastPanel);
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
            masterGrid.Columns.AddRange(new DataGridViewColumn[] { NameColumn, AccountTypeColumn, AsOfDateColumn, BalanceColumn, DueDateColumn, AvailableColumn, AprColumn, CreditLimitColumn, WebsiteColumn });
            masterGrid.Dock = DockStyle.Fill;
            masterGrid.Location = new Point(0, 0);
            masterGrid.MultiSelect = false;
            masterGrid.Name = "masterGrid";
            masterGrid.RowHeadersVisible = false;
            masterGrid.RowHeadersWidth = 72;
            masterGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            masterGrid.Size = new Size(1260, 200);
            masterGrid.TabIndex = 1;
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
            detailGrid.Location = new Point(0, 42);
            detailGrid.MultiSelect = false;
            detailGrid.Name = "detailGrid";
            detailGrid.ReadOnly = true;
            detailGrid.RowHeadersVisible = false;
            detailGrid.RowHeadersWidth = 72;
            detailGrid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            detailGrid.Size = new Size(1260, 354);
            detailGrid.TabIndex = 2;
            // 
            // DateColumn
            // 
            DateColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            DateColumn.DataPropertyName = "Date";
            dataGridViewCellStyle7.Format = "d";
            dataGridViewCellStyle7.NullValue = null;
            DateColumn.DefaultCellStyle = dataGridViewCellStyle7;
            DateColumn.HeaderText = "Date";
            DateColumn.MinimumWidth = 9;
            DateColumn.Name = "DateColumn";
            DateColumn.ReadOnly = true;
            DateColumn.Width = 98;
            // 
            // AmountColumn
            // 
            AmountColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            AmountColumn.DataPropertyName = "Amount";
            dataGridViewCellStyle8.Format = "c2";
            AmountColumn.DefaultCellStyle = dataGridViewCellStyle8;
            AmountColumn.HeaderText = "Amount";
            AmountColumn.MinimumWidth = 9;
            AmountColumn.Name = "AmountColumn";
            AmountColumn.ReadOnly = true;
            AmountColumn.Width = 129;
            // 
            // ForecastColumn
            // 
            ForecastColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            ForecastColumn.DataPropertyName = "ForecastBalance";
            dataGridViewCellStyle9.Format = "c2";
            ForecastColumn.DefaultCellStyle = dataGridViewCellStyle9;
            ForecastColumn.HeaderText = "Balance";
            ForecastColumn.MinimumWidth = 9;
            ForecastColumn.Name = "ForecastColumn";
            ForecastColumn.ReadOnly = true;
            ForecastColumn.Width = 126;
            // 
            // CategoryColumn
            // 
            CategoryColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            CategoryColumn.DataPropertyName = "Category";
            CategoryColumn.HeaderText = "Category";
            CategoryColumn.MinimumWidth = 9;
            CategoryColumn.Name = "CategoryColumn";
            CategoryColumn.ReadOnly = true;
            CategoryColumn.Width = 137;
            // 
            // TypeColumn
            // 
            TypeColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            TypeColumn.DataPropertyName = "TypeLabel";
            TypeColumn.HeaderText = "Type";
            TypeColumn.MinimumWidth = 9;
            TypeColumn.Name = "TypeColumn";
            TypeColumn.ReadOnly = true;
            TypeColumn.Width = 97;
            // 
            // DescriptionColumn
            // 
            DescriptionColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            DescriptionColumn.DataPropertyName = "Description";
            DescriptionColumn.HeaderText = "Description";
            DescriptionColumn.MinimumWidth = 9;
            DescriptionColumn.Name = "DescriptionColumn";
            DescriptionColumn.ReadOnly = true;
            // 
            // forecastPanel
            // 
            forecastPanel.Controls.Add(lblHistory);
            forecastPanel.Controls.Add(cmbHistory);
            forecastPanel.Controls.Add(lblForecastPeriod);
            forecastPanel.Controls.Add(cmbForecastPeriod);
            forecastPanel.Controls.Add(lblShow);
            forecastPanel.Controls.Add(cmbShow);
            forecastPanel.Controls.Add(btnAddRule);
            forecastPanel.Controls.Add(btnDeleteRule);
            forecastPanel.Controls.Add(btnSplitTransaction);
            forecastPanel.AutoSize = true;
            forecastPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            forecastPanel.Dock = DockStyle.Top;
            forecastPanel.FlowDirection = FlowDirection.LeftToRight;
            forecastPanel.Location = new Point(0, 0);
            forecastPanel.Name = "forecastPanel";
            forecastPanel.Padding = new Padding(8, 6, 8, 6);
            forecastPanel.Size = new Size(1260, 50);
            forecastPanel.TabIndex = 0;
            forecastPanel.WrapContents = true;
            // 
            // lblHistory
            // 
            lblHistory.Anchor = AnchorStyles.Left;
            lblHistory.AutoSize = true;
            lblHistory.Location = new Point(12, 12);
            lblHistory.Margin = new Padding(4, 8, 8, 4);
            lblHistory.Name = "lblHistory";
            lblHistory.Size = new Size(80, 30);
            lblHistory.TabIndex = 0;
            lblHistory.Text = "History:";
            // 
            // cmbHistory
            // 
            cmbHistory.Anchor = AnchorStyles.Left;
            cmbHistory.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbHistory.DropDownWidth = 120;
            cmbHistory.Location = new Point(100, 10);
            cmbHistory.Margin = new Padding(4, 4, 8, 4);
            cmbHistory.Name = "cmbHistory";
            cmbHistory.Size = new Size(120, 38);
            cmbHistory.TabIndex = 1;
            // 
            // lblForecastPeriod
            // 
            lblForecastPeriod.Anchor = AnchorStyles.Left;
            lblForecastPeriod.AutoSize = true;
            lblForecastPeriod.Location = new Point(12, 12);
            lblForecastPeriod.Margin = new Padding(4, 8, 8, 4);
            lblForecastPeriod.Name = "lblForecastPeriod";
            lblForecastPeriod.Size = new Size(159, 30);
            lblForecastPeriod.TabIndex = 2;
            lblForecastPeriod.Text = "Forecast Period:";
            // 
            // cmbForecastPeriod
            // 
            cmbForecastPeriod.Anchor = AnchorStyles.Left;
            cmbForecastPeriod.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbForecastPeriod.DropDownWidth = 140;
            cmbForecastPeriod.Location = new Point(183, 10);
            cmbForecastPeriod.Margin = new Padding(4, 4, 8, 4);
            cmbForecastPeriod.Name = "cmbForecastPeriod";
            cmbForecastPeriod.Size = new Size(140, 38);
            cmbForecastPeriod.TabIndex = 3;
            // 
            // lblShow
            // 
            lblShow.Anchor = AnchorStyles.Left;
            lblShow.AutoSize = true;
            lblShow.Location = new Point(335, 12);
            lblShow.Margin = new Padding(4, 8, 8, 4);
            lblShow.Name = "lblShow";
            lblShow.Size = new Size(60, 30);
            lblShow.TabIndex = 4;
            lblShow.Text = "Show:";
            // 
            // cmbShow
            // 
            cmbShow.Anchor = AnchorStyles.Left;
            cmbShow.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbShow.DropDownWidth = 220;
            cmbShow.Location = new Point(407, 10);
            cmbShow.Margin = new Padding(4, 4, 8, 4);
            cmbShow.Name = "cmbShow";
            cmbShow.Size = new Size(200, 38);
            cmbShow.TabIndex = 5;
            // 
            // btnAddRule
            // 
            btnAddRule.AutoSize = true;
            btnAddRule.Location = new Point(619, 10);
            btnAddRule.Margin = new Padding(4, 4, 8, 4);
            btnAddRule.Name = "btnAddRule";
            btnAddRule.Size = new Size(120, 34);
            btnAddRule.TabIndex = 6;
            btnAddRule.Text = "Add Rule";
            btnAddRule.UseVisualStyleBackColor = true;
            // 
            // btnDeleteRule
            // 
            btnDeleteRule.AutoSize = true;
            btnDeleteRule.Enabled = false;
            btnDeleteRule.Location = new Point(751, 10);
            btnDeleteRule.Margin = new Padding(4, 4, 8, 4);
            btnDeleteRule.Name = "btnDeleteRule";
            btnDeleteRule.Size = new Size(120, 34);
            btnDeleteRule.TabIndex = 7;
            btnDeleteRule.Text = "Delete Rule";
            btnDeleteRule.UseVisualStyleBackColor = true;
            // 
            // btnSplitTransaction
            // 
            btnSplitTransaction.AutoSize = true;
            btnSplitTransaction.Location = new Point(883, 10);
            btnSplitTransaction.Margin = new Padding(4, 4, 8, 4);
            btnSplitTransaction.Name = "btnSplitTransaction";
            btnSplitTransaction.Size = new Size(140, 34);
            btnSplitTransaction.TabIndex = 8;
            btnSplitTransaction.Text = "Split Transaction";
            btnSplitTransaction.UseVisualStyleBackColor = true;
            // 
            // NameColumn
            // 
            NameColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            NameColumn.DataPropertyName = "Name";
            NameColumn.HeaderText = "Account";
            NameColumn.MinimumWidth = 9;
            NameColumn.Name = "NameColumn";
            NameColumn.ReadOnly = true;
            NameColumn.Width = 131;
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
            AsOfDateColumn.Width = 106;
            // 
            // BalanceColumn
            // 
            BalanceColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            BalanceColumn.DataPropertyName = "Balance";
            dataGridViewCellStyle2.Format = "c2";
            dataGridViewCellStyle2.NullValue = "N/A";
            BalanceColumn.DefaultCellStyle = dataGridViewCellStyle2;
            BalanceColumn.HeaderText = "Balance";
            BalanceColumn.MinimumWidth = 9;
            BalanceColumn.Name = "BalanceColumn";
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
            DueDateColumn.Width = 178;
            // 
            // AvailableColumn
            // 
            AvailableColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            AvailableColumn.DataPropertyName = "BankCreditAvailable";
            dataGridViewCellStyle4.Format = "c2";
            dataGridViewCellStyle4.NullValue = "N/A";
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
            AprColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            AprColumn.DataPropertyName = "APR";
            dataGridViewCellStyle5.Format = "N2";
            dataGridViewCellStyle5.NullValue = null;
            AprColumn.DefaultCellStyle = dataGridViewCellStyle5;
            AprColumn.HeaderText = "APR";
            AprColumn.MinimumWidth = 9;
            AprColumn.Name = "AprColumn";
            AprColumn.ReadOnly = true;
            AprColumn.Width = 93;
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
            CreditLimitColumn.Width = 160;
            // 
            // WebsiteColumn
            // 
            WebsiteColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            WebsiteColumn.DataPropertyName = "WebsiteUrl";
            WebsiteColumn.HeaderText = "Web Site";
            WebsiteColumn.LinkBehavior = LinkBehavior.HoverUnderline;
            WebsiteColumn.MinimumWidth = 9;
            WebsiteColumn.Name = "WebsiteColumn";
            WebsiteColumn.ReadOnly = true;
            WebsiteColumn.TrackVisitedState = false;
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
            forecastPanel.ResumeLayout(false);
            forecastPanel.PerformLayout();
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
        private DataGridViewLinkColumn WebsiteColumn;
    }
}
