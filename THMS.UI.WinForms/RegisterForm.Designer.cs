namespace THMS.UI.WinForms
{
    partial class RegisterForm
    {
        private System.ComponentModel.IContainer components = null;
        private MenuStrip menu;
        private ToolStripMenuItem importMenu;
        private ToolStripMenuItem importTransactionsItem;
        private ToolStripMenuItem importAccountsItem;
        private ToolStripMenuItem importCategoriesItem;
        private ToolStripMenuItem importStatementsItem;
        private ToolStripMenuItem importRecurringTransactionsItem;
        private ToolStripMenuItem importRecurringTransfersItem;
        private ToolStripMenuItem plaidMenu;
        private ToolStripMenuItem plaidLinkAccountsItem;
        private ToolStripMenuItem plaidSyncDataItem;
        private SplitContainer split;
        private Controls.ThmsTabControl tabsTop;
        private TabPage tabAccounts;
        private TabPage tabCategories;
        private Controls.AccountUpdaterControl accountUpdater;
        private Panel pnlAccountTabs;
        private Label lblSelectedAccount;
        private Controls.ThmsTabControl tabs;
        private TabPage tabBills;
        private TabPage tabLedger;
        private TabPage tabStatements;
        private Controls.BillsControl billsControl;
        private Panel pnlLedger;
        private Controls.TransactionManagerControl ledger;
        private Label lblLedgerStatus;
        private Panel pnlStatements;
        private DataGridView gridStatements;
        private FlowLayoutPanel pnlStatementButtons;
        private Controls.ThmsButton btnAddStatement;
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
            menu = new MenuStrip();
            importMenu = new ToolStripMenuItem();
            importTransactionsItem = new ToolStripMenuItem();
            importAccountsItem = new ToolStripMenuItem();
            importCategoriesItem = new ToolStripMenuItem();
            importStatementsItem = new ToolStripMenuItem();
            importRecurringTransactionsItem = new ToolStripMenuItem();
            importRecurringTransfersItem = new ToolStripMenuItem();
            plaidMenu = new ToolStripMenuItem();
            plaidLinkAccountsItem = new ToolStripMenuItem();
            plaidSyncDataItem = new ToolStripMenuItem();
            split = new SplitContainer();
            tabsTop = new Controls.ThmsTabControl();
            tabAccounts = new TabPage();
            tabCategories = new TabPage();
            accountUpdater = new Controls.AccountUpdaterControl();
            pnlAccountTabs = new Panel();
            lblSelectedAccount = new Label();
            tabs = new Controls.ThmsTabControl();
            tabBills = new TabPage();
            tabLedger = new TabPage();
            tabStatements = new TabPage();
            billsControl = new Controls.BillsControl();
            pnlLedger = new Panel();
            ledger = new Controls.TransactionManagerControl();
            lblLedgerStatus = new Label();
            pnlStatements = new Panel();
            gridStatements = new DataGridView();
            pnlStatementButtons = new FlowLayoutPanel();
            btnAddStatement = new Controls.ThmsButton();
            lblStatementStatus = new Label();
            categoryManager = new Controls.CategoryManagerControl();
            ((System.ComponentModel.ISupportInitialize)split).BeginInit();
            split.Panel1.SuspendLayout();
            split.Panel2.SuspendLayout();
            split.SuspendLayout();
            tabsTop.SuspendLayout();
            tabAccounts.SuspendLayout();
            tabCategories.SuspendLayout();
            pnlAccountTabs.SuspendLayout();
            tabs.SuspendLayout();
            tabBills.SuspendLayout();
            tabLedger.SuspendLayout();
            tabStatements.SuspendLayout();
            pnlLedger.SuspendLayout();
            pnlStatements.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)gridStatements).BeginInit();
            pnlStatementButtons.SuspendLayout();
            menu.SuspendLayout();
            SuspendLayout();
            //
            // menu
            //
            menu.Items.AddRange(new ToolStripItem[] { importMenu, plaidMenu });
            menu.Name = "menu";
            importMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                importTransactionsItem,
                importAccountsItem,
                importCategoriesItem,
                importStatementsItem,
                importRecurringTransactionsItem,
                importRecurringTransfersItem
            });
            importMenu.Name = "importMenu";
            importMenu.Text = "&Import";
            importTransactionsItem.Name = "importTransactionsItem";
            importTransactionsItem.Text = "&Transactions";
            importTransactionsItem.Click += OnImportFromFile;
            importAccountsItem.Name = "importAccountsItem";
            importAccountsItem.Text = "&Accounts";
            importAccountsItem.Click += OnImportAccounts;
            importCategoriesItem.Name = "importCategoriesItem";
            importCategoriesItem.Text = "&Categories";
            importCategoriesItem.Click += OnImportCategories;
            importStatementsItem.Name = "importStatementsItem";
            importStatementsItem.Text = "&Statements";
            importStatementsItem.Click += OnImportStatements;
            importRecurringTransactionsItem.Name = "importRecurringTransactionsItem";
            importRecurringTransactionsItem.Text = "&Recurring Transactions";
            importRecurringTransactionsItem.Click += OnImportTransactionRules;
            importRecurringTransfersItem.Name = "importRecurringTransfersItem";
            importRecurringTransfersItem.Text = "Recurring T&ransfers";
            importRecurringTransfersItem.Click += OnImportTransferRules;
            plaidMenu.DropDownItems.AddRange(new ToolStripItem[] { plaidLinkAccountsItem, plaidSyncDataItem });
            plaidMenu.Name = "plaidMenu";
            plaidMenu.Text = "&Plaid";
            plaidLinkAccountsItem.Name = "plaidLinkAccountsItem";
            plaidLinkAccountsItem.Text = "&Link Accounts";
            plaidLinkAccountsItem.Click += OnLinkPlaidAccounts;
            plaidSyncDataItem.Name = "plaidSyncDataItem";
            plaidSyncDataItem.Text = "&Sync Data";
            plaidSyncDataItem.Click += OnImportFromPlaid;
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
            split.Panel2.Controls.Add(pnlAccountTabs);
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
            // pnlAccountTabs
            //
            pnlAccountTabs.Controls.Add(tabs);
            pnlAccountTabs.Controls.Add(lblSelectedAccount);
            pnlAccountTabs.Dock = DockStyle.Fill;
            pnlAccountTabs.Name = "pnlAccountTabs";
            //
            // lblSelectedAccount
            //
            lblSelectedAccount.AutoSize = false;
            lblSelectedAccount.Dock = DockStyle.Top;
            lblSelectedAccount.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblSelectedAccount.Name = "lblSelectedAccount";
            lblSelectedAccount.Padding = new Padding(8, 10, 8, 8);
            lblSelectedAccount.Text = "Account:";
            lblSelectedAccount.TextAlign = ContentAlignment.MiddleLeft;
            lblSelectedAccount.UseMnemonic = false;
            //
            // tabs
            //
            tabs.Dock = DockStyle.Fill;
            tabs.MaxTabWidth = 220;
            tabs.Name = "tabs";
            tabs.TabPages.Add(tabBills);
            tabs.TabPages.Add(tabLedger);
            tabs.TabPages.Add(tabStatements);
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
            pnlLedger.Dock = DockStyle.Fill;
            pnlLedger.Name = "pnlLedger";
            //
            // ledger
            //
            ledger.Dock = DockStyle.Fill;
            ledger.HostProvidesHistory = true;
            ledger.Name = "ledger";
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
            categoryManager.ShowImportButton = false;
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
            pnlStatementButtons.Dock = DockStyle.Bottom;
            pnlStatementButtons.Name = "pnlStatementButtons";
            pnlStatementButtons.Padding = new Padding(12, 12, 12, 16);
            pnlStatementButtons.WrapContents = true;
            //
            // btnAddStatement
            //
            btnAddStatement.Name = "btnAddStatement";
            btnAddStatement.Text = "Add";
            btnAddStatement.Click += OnAddStatement;
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
            Controls.Add(menu);
            MainMenuStrip = menu;
            Name = "RegisterForm";
            Text = "Register";
            split.Panel1.ResumeLayout(false);
            split.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)split).EndInit();
            split.ResumeLayout(false);
            tabAccounts.ResumeLayout(false);
            tabCategories.ResumeLayout(false);
            tabsTop.ResumeLayout(false);
            tabBills.ResumeLayout(false);
            tabLedger.ResumeLayout(false);
            tabStatements.ResumeLayout(false);
            tabs.ResumeLayout(false);
            pnlAccountTabs.ResumeLayout(false);
            pnlLedger.ResumeLayout(false);
            pnlLedger.PerformLayout();
            pnlStatements.ResumeLayout(false);
            pnlStatements.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)gridStatements).EndInit();
            pnlStatementButtons.ResumeLayout(false);
            menu.ResumeLayout(false);
            menu.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
