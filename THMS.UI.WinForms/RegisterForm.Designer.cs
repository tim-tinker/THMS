namespace THMS.UI.WinForms
{
    partial class RegisterForm
    {
        private System.ComponentModel.IContainer components = null;
        private SplitContainer split;
        private Label lblAccounts;
        private Controls.AccountUpdaterControl accountUpdater;
        private Controls.ThmsTabControl tabs;
        private TabPage tabPosted;
        private TabPage tabByCategory;
        private TabPage tabStatements;
        private TabPage tabCategories;
        private Panel pnlTransactions;
        private DataGridView gridTransactions;
        private DataGridViewTextBoxColumn DateColumn;
        private DataGridViewTextBoxColumn AmountColumn;
        private DataGridViewTextBoxColumn BalanceColumn;
        private DataGridViewTextBoxColumn CategoryColumn;
        private DataGridViewTextBoxColumn TypeColumn;
        private DataGridViewTextBoxColumn DescriptionColumn;
        private FlowLayoutPanel pnlTxButtons;
        private Controls.ThmsButton btnImport;
        private Controls.ThmsButton btnImportPlaid;
        private Controls.ThmsButton btnSplit;
        private Label lblTxStatus;
        private Panel pnlCategory;
        private Panel pnlCategoryFilter;
        private Label lblCategoryFilter;
        private ComboBox cmbCategoryFilter;
        private DataGridView gridCategory;
        private DataGridViewTextBoxColumn CategoryDateColumn;
        private DataGridViewTextBoxColumn CategoryAmountColumn;
        private DataGridViewTextBoxColumn CategoryCategoryColumn;
        private DataGridViewTextBoxColumn CategoryTypeColumn;
        private DataGridViewTextBoxColumn CategoryDescriptionColumn;
        private FlowLayoutPanel pnlCategoryButtons;
        private Controls.ThmsButton btnCategorySplit;
        private Label lblCategoryStatus;
        private Panel pnlStatements;
        private DataGridView gridStatements;
        private FlowLayoutPanel pnlStatementButtons;
        private Controls.ThmsButton btnAddStatement;
        private Controls.ThmsButton btnImportStatements;
        private Label lblStatementStatus;
        private Controls.CategoryManagerControl categoryManager;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            split = new SplitContainer();
            lblAccounts = new Label();
            accountUpdater = new Controls.AccountUpdaterControl();
            tabs = new Controls.ThmsTabControl();
            tabPosted = new TabPage();
            tabByCategory = new TabPage();
            tabStatements = new TabPage();
            tabCategories = new TabPage();
            pnlTransactions = new Panel();
            gridTransactions = new DataGridView();
            DateColumn = new DataGridViewTextBoxColumn();
            AmountColumn = new DataGridViewTextBoxColumn();
            BalanceColumn = new DataGridViewTextBoxColumn();
            CategoryColumn = new DataGridViewTextBoxColumn();
            TypeColumn = new DataGridViewTextBoxColumn();
            DescriptionColumn = new DataGridViewTextBoxColumn();
            pnlTxButtons = new FlowLayoutPanel();
            btnImport = new Controls.ThmsButton();
            btnImportPlaid = new Controls.ThmsButton();
            btnSplit = new Controls.ThmsButton();
            lblTxStatus = new Label();
            pnlCategory = new Panel();
            pnlCategoryFilter = new Panel();
            lblCategoryFilter = new Label();
            cmbCategoryFilter = new ComboBox();
            gridCategory = new DataGridView();
            CategoryDateColumn = new DataGridViewTextBoxColumn();
            CategoryAmountColumn = new DataGridViewTextBoxColumn();
            CategoryCategoryColumn = new DataGridViewTextBoxColumn();
            CategoryTypeColumn = new DataGridViewTextBoxColumn();
            CategoryDescriptionColumn = new DataGridViewTextBoxColumn();
            pnlCategoryButtons = new FlowLayoutPanel();
            btnCategorySplit = new Controls.ThmsButton();
            lblCategoryStatus = new Label();
            pnlStatements = new Panel();
            gridStatements = new DataGridView();
            pnlStatementButtons = new FlowLayoutPanel();
            btnAddStatement = new Controls.ThmsButton();
            btnImportStatements = new Controls.ThmsButton();
            lblStatementStatus = new Label();
            categoryManager = new Controls.CategoryManagerControl();
            ((System.ComponentModel.ISupportInitialize)split).BeginInit();
            split.Panel1.SuspendLayout();
            split.Panel2.SuspendLayout();
            split.SuspendLayout();
            tabs.SuspendLayout();
            tabPosted.SuspendLayout();
            tabByCategory.SuspendLayout();
            tabStatements.SuspendLayout();
            tabCategories.SuspendLayout();
            pnlTransactions.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)gridTransactions).BeginInit();
            pnlTxButtons.SuspendLayout();
            pnlCategory.SuspendLayout();
            pnlCategoryFilter.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)gridCategory).BeginInit();
            pnlCategoryButtons.SuspendLayout();
            pnlStatements.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)gridStatements).BeginInit();
            pnlStatementButtons.SuspendLayout();
            SuspendLayout();
            //
            // split
            //
            split.Dock = DockStyle.Fill;
            split.Name = "split";
            split.Orientation = Orientation.Horizontal;
            split.Size = new Size(1100, 760);
            split.Panel1MinSize = 140;
            split.Panel2MinSize = 180;
            split.SplitterDistance = 260;
            split.Panel1.Controls.Add(accountUpdater);
            split.Panel1.Controls.Add(lblAccounts);
            split.Panel2.Controls.Add(tabs);
            //
            // lblAccounts
            //
            lblAccounts.Dock = DockStyle.Top;
            lblAccounts.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblAccounts.Height = 32;
            lblAccounts.Name = "lblAccounts";
            lblAccounts.Padding = new Padding(8, 4, 8, 0);
            lblAccounts.Text = "Accounts";
            lblAccounts.TextAlign = ContentAlignment.MiddleLeft;
            //
            // accountUpdater
            //
            accountUpdater.Dock = DockStyle.Fill;
            accountUpdater.Name = "accountUpdater";
            //
            // tabs
            //
            tabs.Dock = DockStyle.Fill;
            tabs.MaxTabWidth = 220;
            tabs.Name = "tabs";
            tabs.TabPages.Add(tabPosted);
            tabs.TabPages.Add(tabByCategory);
            tabs.TabPages.Add(tabStatements);
            tabs.TabPages.Add(tabCategories);
            //
            // tabPosted
            //
            tabPosted.Controls.Add(pnlTransactions);
            tabPosted.Name = "tabPosted";
            tabPosted.Padding = new Padding(4);
            tabPosted.Text = "Posted Transactions";
            tabPosted.UseVisualStyleBackColor = false;
            //
            // tabByCategory
            //
            tabByCategory.Controls.Add(pnlCategory);
            tabByCategory.Name = "tabByCategory";
            tabByCategory.Padding = new Padding(4);
            tabByCategory.Text = "By Category";
            tabByCategory.UseVisualStyleBackColor = false;
            //
            // tabStatements
            //
            tabStatements.Controls.Add(pnlStatements);
            tabStatements.Name = "tabStatements";
            tabStatements.Padding = new Padding(4);
            tabStatements.Text = "Statements";
            tabStatements.UseVisualStyleBackColor = false;
            //
            // tabCategories
            //
            tabCategories.Controls.Add(categoryManager);
            tabCategories.Name = "tabCategories";
            tabCategories.Padding = new Padding(4);
            tabCategories.Text = "Categories";
            tabCategories.UseVisualStyleBackColor = false;
            //
            // categoryManager
            //
            categoryManager.Dock = DockStyle.Fill;
            categoryManager.Name = "categoryManager";
            categoryManager.ShowCloseButton = false;
            //
            // pnlTransactions
            //
            pnlTransactions.Controls.Add(gridTransactions);
            pnlTransactions.Controls.Add(lblTxStatus);
            pnlTransactions.Controls.Add(pnlTxButtons);
            pnlTransactions.Dock = DockStyle.Fill;
            pnlTransactions.Name = "pnlTransactions";
            //
            // gridTransactions
            //
            gridTransactions.AllowUserToAddRows = false;
            gridTransactions.AllowUserToDeleteRows = false;
            gridTransactions.AllowUserToResizeRows = false;
            gridTransactions.AutoGenerateColumns = false;
            gridTransactions.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridTransactions.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gridTransactions.Columns.AddRange(DateColumn, AmountColumn, BalanceColumn, CategoryColumn, TypeColumn, DescriptionColumn);
            gridTransactions.Dock = DockStyle.Fill;
            gridTransactions.EditMode = DataGridViewEditMode.EditProgrammatically;
            gridTransactions.MultiSelect = false;
            gridTransactions.Name = "gridTransactions";
            gridTransactions.ReadOnly = true;
            gridTransactions.RowHeadersVisible = false;
            gridTransactions.SelectionMode = DataGridViewSelectionMode.CellSelect;
            //
            // DateColumn
            //
            DateColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            DateColumn.DataPropertyName = "Date";
            DateColumn.DefaultCellStyle = new DataGridViewCellStyle { Format = "d" };
            DateColumn.HeaderText = "Date";
            DateColumn.Name = "DateColumn";
            DateColumn.ReadOnly = true;
            //
            // AmountColumn
            //
            AmountColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            AmountColumn.DataPropertyName = "Amount";
            AmountColumn.DefaultCellStyle = new DataGridViewCellStyle { Format = "c2" };
            AmountColumn.HeaderText = "Amount";
            AmountColumn.Name = "AmountColumn";
            AmountColumn.ReadOnly = true;
            //
            // BalanceColumn
            //
            BalanceColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            BalanceColumn.DataPropertyName = "ForecastBalance";
            BalanceColumn.DefaultCellStyle = new DataGridViewCellStyle { Format = "c2" };
            BalanceColumn.HeaderText = "Balance";
            BalanceColumn.Name = "BalanceColumn";
            BalanceColumn.ReadOnly = true;
            //
            // CategoryColumn
            //
            CategoryColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            CategoryColumn.DataPropertyName = "Category";
            CategoryColumn.HeaderText = "Category";
            CategoryColumn.Name = "CategoryColumn";
            CategoryColumn.ReadOnly = true;
            //
            // TypeColumn
            //
            TypeColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            TypeColumn.DataPropertyName = "TypeLabel";
            TypeColumn.HeaderText = "Type";
            TypeColumn.Name = "TypeColumn";
            TypeColumn.ReadOnly = true;
            //
            // DescriptionColumn
            //
            DescriptionColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            DescriptionColumn.DataPropertyName = "Description";
            DescriptionColumn.HeaderText = "Description";
            DescriptionColumn.Name = "DescriptionColumn";
            DescriptionColumn.ReadOnly = true;
            //
            // pnlTxButtons
            //
            pnlTxButtons.AutoSize = true;
            pnlTxButtons.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            pnlTxButtons.Controls.Add(btnImport);
            pnlTxButtons.Controls.Add(btnImportPlaid);
            pnlTxButtons.Controls.Add(btnSplit);
            pnlTxButtons.Dock = DockStyle.Bottom;
            pnlTxButtons.Name = "pnlTxButtons";
            pnlTxButtons.Padding = new Padding(8);
            pnlTxButtons.WrapContents = true;
            //
            // btnImport
            //
            btnImport.Enabled = false;
            btnImport.Name = "btnImport";
            btnImport.Text = "Import";
            btnImport.Click += OnImportFromFile;
            //
            // btnImportPlaid
            //
            btnImportPlaid.Enabled = false;
            btnImportPlaid.Name = "btnImportPlaid";
            btnImportPlaid.Text = "Import from Plaid";
            btnImportPlaid.Click += OnImportFromPlaid;
            //
            // btnSplit
            //
            btnSplit.Name = "btnSplit";
            btnSplit.Text = "Split Transaction";
            btnSplit.Click += OnSplitTransaction;
            //
            // lblTxStatus
            //
            lblTxStatus.Dock = DockStyle.Bottom;
            lblTxStatus.Height = 24;
            lblTxStatus.Name = "lblTxStatus";
            lblTxStatus.Padding = new Padding(8, 0, 8, 0);
            lblTxStatus.Text = "Select an account to view posted transactions.";
            lblTxStatus.TextAlign = ContentAlignment.MiddleLeft;
            //
            // pnlCategory
            //
            pnlCategory.Controls.Add(gridCategory);
            pnlCategory.Controls.Add(lblCategoryStatus);
            pnlCategory.Controls.Add(pnlCategoryButtons);
            pnlCategory.Controls.Add(pnlCategoryFilter);
            pnlCategory.Dock = DockStyle.Fill;
            pnlCategory.Name = "pnlCategory";
            //
            // pnlCategoryFilter
            //
            pnlCategoryFilter.Controls.Add(cmbCategoryFilter);
            pnlCategoryFilter.Controls.Add(lblCategoryFilter);
            pnlCategoryFilter.Dock = DockStyle.Top;
            pnlCategoryFilter.Height = 40;
            pnlCategoryFilter.Name = "pnlCategoryFilter";
            pnlCategoryFilter.Padding = new Padding(8, 6, 8, 4);
            //
            // lblCategoryFilter
            //
            lblCategoryFilter.AutoSize = false;
            lblCategoryFilter.Dock = DockStyle.Left;
            lblCategoryFilter.Name = "lblCategoryFilter";
            lblCategoryFilter.Text = "Category:";
            lblCategoryFilter.TextAlign = ContentAlignment.MiddleLeft;
            lblCategoryFilter.Width = 80;
            //
            // cmbCategoryFilter
            //
            cmbCategoryFilter.Dock = DockStyle.Fill;
            cmbCategoryFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbCategoryFilter.Name = "cmbCategoryFilter";
            //
            // gridCategory
            //
            gridCategory.AllowUserToAddRows = false;
            gridCategory.AllowUserToDeleteRows = false;
            gridCategory.AllowUserToResizeRows = false;
            gridCategory.AutoGenerateColumns = false;
            gridCategory.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridCategory.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gridCategory.Columns.AddRange(CategoryDateColumn, CategoryAmountColumn, CategoryCategoryColumn, CategoryTypeColumn, CategoryDescriptionColumn);
            gridCategory.Dock = DockStyle.Fill;
            gridCategory.EditMode = DataGridViewEditMode.EditProgrammatically;
            gridCategory.MultiSelect = false;
            gridCategory.Name = "gridCategory";
            gridCategory.ReadOnly = true;
            gridCategory.RowHeadersVisible = false;
            gridCategory.SelectionMode = DataGridViewSelectionMode.CellSelect;
            //
            // CategoryDateColumn
            //
            CategoryDateColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            CategoryDateColumn.DataPropertyName = "Date";
            CategoryDateColumn.DefaultCellStyle = new DataGridViewCellStyle { Format = "d" };
            CategoryDateColumn.HeaderText = "Date";
            CategoryDateColumn.Name = "CategoryDateColumn";
            CategoryDateColumn.ReadOnly = true;
            //
            // CategoryAmountColumn
            //
            CategoryAmountColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            CategoryAmountColumn.DataPropertyName = "Amount";
            CategoryAmountColumn.DefaultCellStyle = new DataGridViewCellStyle { Format = "c2" };
            CategoryAmountColumn.HeaderText = "Amount";
            CategoryAmountColumn.Name = "CategoryAmountColumn";
            CategoryAmountColumn.ReadOnly = true;
            //
            // CategoryCategoryColumn
            //
            CategoryCategoryColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            CategoryCategoryColumn.DataPropertyName = "Category";
            CategoryCategoryColumn.HeaderText = "Category";
            CategoryCategoryColumn.Name = "CategoryCategoryColumn";
            CategoryCategoryColumn.ReadOnly = true;
            //
            // CategoryTypeColumn
            //
            CategoryTypeColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            CategoryTypeColumn.DataPropertyName = "TypeLabel";
            CategoryTypeColumn.HeaderText = "Type";
            CategoryTypeColumn.Name = "CategoryTypeColumn";
            CategoryTypeColumn.ReadOnly = true;
            //
            // CategoryDescriptionColumn
            //
            CategoryDescriptionColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            CategoryDescriptionColumn.DataPropertyName = "Description";
            CategoryDescriptionColumn.HeaderText = "Description";
            CategoryDescriptionColumn.Name = "CategoryDescriptionColumn";
            CategoryDescriptionColumn.ReadOnly = true;
            //
            // pnlCategoryButtons
            //
            pnlCategoryButtons.AutoSize = true;
            pnlCategoryButtons.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            pnlCategoryButtons.Controls.Add(btnCategorySplit);
            pnlCategoryButtons.Dock = DockStyle.Bottom;
            pnlCategoryButtons.Name = "pnlCategoryButtons";
            pnlCategoryButtons.Padding = new Padding(8);
            pnlCategoryButtons.WrapContents = true;
            //
            // btnCategorySplit
            //
            btnCategorySplit.Name = "btnCategorySplit";
            btnCategorySplit.Text = "Split Transaction";
            btnCategorySplit.Click += OnCategorySplitTransaction;
            //
            // lblCategoryStatus
            //
            lblCategoryStatus.Dock = DockStyle.Bottom;
            lblCategoryStatus.Height = 24;
            lblCategoryStatus.Name = "lblCategoryStatus";
            lblCategoryStatus.Padding = new Padding(8, 0, 8, 0);
            lblCategoryStatus.Text = "Select an account to view transactions by category.";
            lblCategoryStatus.TextAlign = ContentAlignment.MiddleLeft;
            //
            // pnlStatements
            //
            pnlStatements.Controls.Add(gridStatements);
            pnlStatements.Controls.Add(lblStatementStatus);
            pnlStatements.Controls.Add(pnlStatementButtons);
            pnlStatements.Dock = DockStyle.Fill;
            pnlStatements.Name = "pnlStatements";
            //
            // gridStatements
            //
            gridStatements.AllowUserToAddRows = false;
            gridStatements.AllowUserToDeleteRows = false;
            gridStatements.AllowUserToResizeRows = false;
            gridStatements.AutoGenerateColumns = false;
            gridStatements.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            gridStatements.ScrollBars = ScrollBars.Both;
            gridStatements.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gridStatements.Dock = DockStyle.Fill;
            gridStatements.MultiSelect = false;
            gridStatements.Name = "gridStatements";
            gridStatements.ReadOnly = true;
            gridStatements.RowHeadersVisible = false;
            gridStatements.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            //
            // pnlStatementButtons
            //
            pnlStatementButtons.AutoSize = true;
            pnlStatementButtons.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            pnlStatementButtons.Controls.Add(btnAddStatement);
            pnlStatementButtons.Controls.Add(btnImportStatements);
            pnlStatementButtons.Dock = DockStyle.Bottom;
            pnlStatementButtons.Name = "pnlStatementButtons";
            pnlStatementButtons.Padding = new Padding(8);
            pnlStatementButtons.WrapContents = true;
            //
            // btnAddStatement
            //
            btnAddStatement.Name = "btnAddStatement";
            btnAddStatement.Text = "Add";
            btnAddStatement.Click += OnAddStatement;
            //
            // btnImportStatements
            //
            btnImportStatements.Name = "btnImportStatements";
            btnImportStatements.Text = "Import";
            btnImportStatements.Click += OnImportStatements;
            //
            // lblStatementStatus
            //
            lblStatementStatus.Dock = DockStyle.Bottom;
            lblStatementStatus.Height = 24;
            lblStatementStatus.Name = "lblStatementStatus";
            lblStatementStatus.Padding = new Padding(8, 0, 8, 0);
            lblStatementStatus.Text = "Select an account to view statements.";
            lblStatementStatus.TextAlign = ContentAlignment.MiddleLeft;
            //
            // RegisterForm
            //
            ClientSize = new Size(1100, 760);
            Controls.Add(split);
            Name = "RegisterForm";
            Text = "Register";
            split.Panel1.ResumeLayout(false);
            split.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)split).EndInit();
            split.ResumeLayout(false);
            tabPosted.ResumeLayout(false);
            tabByCategory.ResumeLayout(false);
            tabStatements.ResumeLayout(false);
            tabCategories.ResumeLayout(false);
            tabs.ResumeLayout(false);
            pnlTransactions.ResumeLayout(false);
            pnlTransactions.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)gridTransactions).EndInit();
            pnlTxButtons.ResumeLayout(false);
            pnlCategory.ResumeLayout(false);
            pnlCategory.PerformLayout();
            pnlCategoryFilter.ResumeLayout(false);
            pnlCategoryFilter.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)gridCategory).EndInit();
            pnlCategoryButtons.ResumeLayout(false);
            pnlStatements.ResumeLayout(false);
            pnlStatements.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)gridStatements).EndInit();
            pnlStatementButtons.ResumeLayout(false);
            ResumeLayout(false);
        }
    }
}
