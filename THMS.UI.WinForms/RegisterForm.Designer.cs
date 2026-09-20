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
        private TabPage tabBills;
        private TabPage tabLedger;
        private TabPage tabStatements;
        private TabPage tabCategories;
        private Controls.BillsControl billsControl;
        private Panel pnlLedger;
        private Controls.TransactionManagerControl ledger;
        private FlowLayoutPanel pnlTxButtons;
        private Controls.ThmsButton btnImport;
        private Controls.ThmsButton btnImportPlaid;
        private Controls.ThmsButton btnImportRules;
        private Controls.ThmsButton btnImportTransfers;
        private Label lblLedgerStatus;
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
            tabBills = new TabPage();
            tabLedger = new TabPage();
            tabStatements = new TabPage();
            tabCategories = new TabPage();
            billsControl = new Controls.BillsControl();
            pnlLedger = new Panel();
            ledger = new Controls.TransactionManagerControl();
            pnlTxButtons = new FlowLayoutPanel();
            btnImport = new Controls.ThmsButton();
            btnImportPlaid = new Controls.ThmsButton();
            btnImportRules = new Controls.ThmsButton();
            btnImportTransfers = new Controls.ThmsButton();
            lblLedgerStatus = new Label();
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
            tabBills.SuspendLayout();
            tabLedger.SuspendLayout();
            tabStatements.SuspendLayout();
            tabCategories.SuspendLayout();
            pnlLedger.SuspendLayout();
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
            tabs.TabPages.Add(tabBills);
            tabs.TabPages.Add(tabLedger);
            tabs.TabPages.Add(tabStatements);
            tabs.TabPages.Add(tabCategories);
            //
            // tabBills
            //
            tabBills.Controls.Add(billsControl);
            tabBills.Name = "tabBills";
            tabBills.Padding = new Padding(4);
            tabBills.Text = "Bills";
            tabBills.UseVisualStyleBackColor = false;
            //
            // billsControl
            //
            billsControl.Dock = DockStyle.Fill;
            billsControl.Name = "billsControl";
            //
            // tabLedger
            //
            tabLedger.Controls.Add(pnlLedger);
            tabLedger.Name = "tabLedger";
            tabLedger.Padding = new Padding(4);
            tabLedger.Text = "Ledger";
            tabLedger.UseVisualStyleBackColor = false;
            //
            // pnlLedger
            //
            pnlLedger.Controls.Add(ledger);
            pnlLedger.Controls.Add(lblLedgerStatus);
            pnlLedger.Controls.Add(pnlTxButtons);
            pnlLedger.Dock = DockStyle.Fill;
            pnlLedger.Name = "pnlLedger";
            //
            // ledger
            //
            ledger.Dock = DockStyle.Fill;
            ledger.HostProvidesHistory = true;
            ledger.Name = "ledger";
            //
            // pnlTxButtons
            //
            pnlTxButtons.AutoSize = true;
            pnlTxButtons.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            pnlTxButtons.Controls.Add(btnImport);
            pnlTxButtons.Controls.Add(btnImportPlaid);
            pnlTxButtons.Controls.Add(btnImportRules);
            pnlTxButtons.Controls.Add(btnImportTransfers);
            pnlTxButtons.Dock = DockStyle.Bottom;
            pnlTxButtons.Name = "pnlTxButtons";
            pnlTxButtons.Padding = new Padding(8);
            pnlTxButtons.WrapContents = true;
            //
            // btnImport
            //
            btnImport.Name = "btnImport";
            btnImport.Text = "Import";
            btnImport.Click += OnImportFromFile;
            //
            // btnImportPlaid
            //
            btnImportPlaid.Name = "btnImportPlaid";
            btnImportPlaid.Text = "Import from Plaid";
            btnImportPlaid.Click += OnImportFromPlaid;
            //
            // btnImportRules
            //
            btnImportRules.Name = "btnImportRules";
            btnImportRules.Text = "Import Transaction Rules";
            btnImportRules.Click += OnImportTransactionRules;
            //
            // btnImportTransfers
            //
            btnImportTransfers.Name = "btnImportTransfers";
            btnImportTransfers.Text = "Import Transfer Rules";
            btnImportTransfers.Click += OnImportTransferRules;
            //
            // lblLedgerStatus
            //
            lblLedgerStatus.Dock = DockStyle.Bottom;
            lblLedgerStatus.Height = 24;
            lblLedgerStatus.Name = "lblLedgerStatus";
            lblLedgerStatus.Padding = new Padding(8, 0, 8, 0);
            lblLedgerStatus.Text = "Posted, forecast, and recurring rules for the selected account.";
            lblLedgerStatus.TextAlign = ContentAlignment.MiddleLeft;
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
            tabBills.ResumeLayout(false);
            tabLedger.ResumeLayout(false);
            tabStatements.ResumeLayout(false);
            tabCategories.ResumeLayout(false);
            tabs.ResumeLayout(false);
            pnlLedger.ResumeLayout(false);
            pnlLedger.PerformLayout();
            pnlTxButtons.ResumeLayout(false);
            pnlStatements.ResumeLayout(false);
            pnlStatements.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)gridStatements).EndInit();
            pnlStatementButtons.ResumeLayout(false);
            ResumeLayout(false);
        }
    }
}
