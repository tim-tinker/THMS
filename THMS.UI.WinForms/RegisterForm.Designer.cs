namespace THMS.UI.WinForms
{
    partial class RegisterForm
    {
        private System.ComponentModel.IContainer components = null;
        private SplitContainer split;
        private Controls.ThmsTabControl tabsTop;
        private TabPage tabAccounts;
        private Controls.AccountUpdaterControl accountUpdater;
        private Controls.ThmsTabControl tabs;
        private TabPage tabPosted;
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
            tabsTop = new Controls.ThmsTabControl();
            tabAccounts = new TabPage();
            accountUpdater = new Controls.AccountUpdaterControl();
            tabs = new Controls.ThmsTabControl();
            tabPosted = new TabPage();
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
            tabsTop.SuspendLayout();
            tabAccounts.SuspendLayout();
            tabs.SuspendLayout();
            tabPosted.SuspendLayout();
            tabStatements.SuspendLayout();
            tabCategories.SuspendLayout();
            pnlTransactions.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)gridTransactions).BeginInit();
            pnlTxButtons.SuspendLayout();
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
            split.Panel1.Controls.Add(tabsTop);
            split.Panel2.Controls.Add(tabs);
            //
            // tabsTop
            //
            tabsTop.Dock = DockStyle.Fill;
            tabsTop.MaxTabWidth = 220;
            tabsTop.Name = "tabsTop";
            tabsTop.TabPages.Add(tabAccounts);
            tabsTop.TabPages.Add(tabCategories);
            //
            // tabAccounts
            //
            tabAccounts.Controls.Add(accountUpdater);
            tabAccounts.Name = "tabAccounts";
            tabAccounts.Padding = new Padding(4);
            tabAccounts.Text = "Accounts";
            tabAccounts.UseVisualStyleBackColor = false;
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
            tabs.TabPages.Add(tabStatements);
            //
            // tabPosted
            //
            tabPosted.Controls.Add(pnlTransactions);
            tabPosted.Name = "tabPosted";
            tabPosted.Padding = new Padding(4);
            tabPosted.Text = "Posted Transactions";
            tabPosted.UseVisualStyleBackColor = false;
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
            tabAccounts.ResumeLayout(false);
            tabsTop.ResumeLayout(false);
            tabPosted.ResumeLayout(false);
            tabStatements.ResumeLayout(false);
            tabCategories.ResumeLayout(false);
            tabs.ResumeLayout(false);
            pnlTransactions.ResumeLayout(false);
            pnlTransactions.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)gridTransactions).EndInit();
            pnlTxButtons.ResumeLayout(false);
            pnlStatements.ResumeLayout(false);
            pnlStatements.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)gridStatements).EndInit();
            pnlStatementButtons.ResumeLayout(false);
            ResumeLayout(false);
        }
    }
}
