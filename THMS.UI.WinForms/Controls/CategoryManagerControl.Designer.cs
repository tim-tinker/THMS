namespace THMS.UI.WinForms.Controls
{
    partial class CategoryManagerControl
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                components?.Dispose();
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            splitMain = new SplitContainer();
            gridCategories = new DataGridView();
            txtSearch = new TextBox();
            tabsDetail = new ThmsTabControl();
            tabPeriods = new TabPage();
            tabTransactions = new TabPage();
            gridPeriods = new DataGridView();
            lblPeriodHint = new Label();
            gridTransactions = new DataGridView();
            pnlTransactionBar = new FlowLayoutPanel();
            btnSplitTransaction = new ThmsButton();
            pnlBottom = new FlowLayoutPanel();
            btnAdd = new ThmsButton();
            btnMerge = new ThmsButton();
            btnAddBudget = new ThmsButton();
            btnImport = new ThmsButton();
            btnClose = new ThmsButton();
            lblStatus = new Label();
            ((System.ComponentModel.ISupportInitialize)splitMain).BeginInit();
            splitMain.Panel1.SuspendLayout();
            splitMain.Panel2.SuspendLayout();
            splitMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)gridCategories).BeginInit();
            ((System.ComponentModel.ISupportInitialize)gridPeriods).BeginInit();
            ((System.ComponentModel.ISupportInitialize)gridTransactions).BeginInit();
            tabsDetail.SuspendLayout();
            tabPeriods.SuspendLayout();
            tabTransactions.SuspendLayout();
            pnlTransactionBar.SuspendLayout();
            pnlBottom.SuspendLayout();
            SuspendLayout();
            splitMain.Dock = DockStyle.Fill;
            splitMain.Margin = new Padding(0);
            splitMain.Name = "splitMain";
            splitMain.Orientation = Orientation.Horizontal;
            splitMain.SplitterWidth = 7;
            splitMain.Panel1.Controls.Add(gridCategories);
            splitMain.Panel1.Controls.Add(txtSearch);
            splitMain.Panel2.Controls.Add(tabsDetail);
            txtSearch.Dock = DockStyle.Top;
            txtSearch.Name = "txtSearch";
            txtSearch.PlaceholderText = "Search categories";
            txtSearch.TextChanged += OnSearchChanged;
            gridCategories.AllowUserToAddRows = false;
            gridCategories.AllowUserToDeleteRows = false;
            gridCategories.AllowUserToResizeRows = false;
            gridCategories.AutoGenerateColumns = false;
            gridCategories.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            gridCategories.Dock = DockStyle.Fill;
            gridCategories.MultiSelect = false;
            gridCategories.Name = "gridCategories";
            gridCategories.ReadOnly = true;
            gridCategories.RowHeadersVisible = false;
            gridCategories.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            tabsDetail.Dock = DockStyle.Fill;
            tabsDetail.MaxTabWidth = 220;
            tabsDetail.Name = "tabsDetail";
            tabsDetail.TabPages.Add(tabPeriods);
            tabsDetail.TabPages.Add(tabTransactions);
            tabPeriods.Name = "tabPeriods";
            tabPeriods.Padding = new Padding(4);
            tabPeriods.Text = "Budget Periods";
            tabPeriods.Controls.Add(gridPeriods);
            tabPeriods.Controls.Add(lblPeriodHint);
            tabTransactions.Name = "tabTransactions";
            tabTransactions.Padding = new Padding(4);
            tabTransactions.Text = "Transactions";
            tabTransactions.Controls.Add(gridTransactions);
            tabTransactions.Controls.Add(pnlTransactionBar);
            lblPeriodHint.AutoSize = false;
            lblPeriodHint.Dock = DockStyle.Top;
            lblPeriodHint.Height = 24;
            lblPeriodHint.Name = "lblPeriodHint";
            lblPeriodHint.Padding = new Padding(4, 2, 4, 2);
            lblPeriodHint.Text = "Select a category.";
            lblPeriodHint.TextAlign = ContentAlignment.MiddleLeft;
            lblPeriodHint.UseMnemonic = false;
            gridPeriods.AllowUserToAddRows = false;
            gridPeriods.AllowUserToDeleteRows = false;
            gridPeriods.AllowUserToResizeRows = false;
            gridPeriods.AutoGenerateColumns = false;
            gridPeriods.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            gridPeriods.Dock = DockStyle.Fill;
            gridPeriods.MultiSelect = false;
            gridPeriods.Name = "gridPeriods";
            gridPeriods.ReadOnly = true;
            gridPeriods.RowHeadersVisible = false;
            gridPeriods.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            pnlTransactionBar.AutoSize = true;
            pnlTransactionBar.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            pnlTransactionBar.Dock = DockStyle.Top;
            pnlTransactionBar.Name = "pnlTransactionBar";
            pnlTransactionBar.Padding = new Padding(4, 4, 4, 4);
            pnlTransactionBar.WrapContents = true;
            pnlTransactionBar.Controls.Add(btnSplitTransaction);
            btnSplitTransaction.Name = "btnSplitTransaction";
            btnSplitTransaction.Text = "Split Transaction";
            btnSplitTransaction.Click += OnSplitTransaction;
            gridTransactions.AllowUserToAddRows = false;
            gridTransactions.AllowUserToDeleteRows = false;
            gridTransactions.AllowUserToResizeRows = false;
            gridTransactions.AutoGenerateColumns = false;
            gridTransactions.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            gridTransactions.Dock = DockStyle.Fill;
            gridTransactions.EditMode = DataGridViewEditMode.EditProgrammatically;
            gridTransactions.MultiSelect = false;
            gridTransactions.Name = "gridTransactions";
            gridTransactions.ReadOnly = true;
            gridTransactions.RowHeadersVisible = false;
            gridTransactions.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            pnlBottom.AutoSize = true;
            pnlBottom.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            pnlBottom.Dock = DockStyle.Bottom;
            pnlBottom.Name = "pnlBottom";
            pnlBottom.Padding = new Padding(12, 12, 12, 16);
            pnlBottom.WrapContents = true;
            pnlBottom.Controls.Add(btnAdd);
            pnlBottom.Controls.Add(btnMerge);
            pnlBottom.Controls.Add(btnAddBudget);
            pnlBottom.Controls.Add(btnImport);
            pnlBottom.Controls.Add(btnClose);
            btnAdd.Name = "btnAdd";
            btnAdd.Text = "Add Category";
            btnAdd.Click += OnAdd;
            btnMerge.Name = "btnMerge";
            btnMerge.Text = "Merge Categories";
            btnMerge.Click += OnMerge;
            btnAddBudget.Name = "btnAddBudget";
            btnAddBudget.Text = "Add Budget";
            btnAddBudget.Click += OnAddBudget;
            btnImport.Name = "btnImport";
            btnImport.Text = "Import";
            btnImport.Click += OnImport;
            btnClose.Name = "btnClose";
            btnClose.Text = "Close";
            btnClose.Click += OnClose;
            lblStatus.Dock = DockStyle.Bottom;
            lblStatus.Height = 24;
            lblStatus.Name = "lblStatus";
            lblStatus.Padding = new Padding(8, 0, 8, 0);
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            lblStatus.Visible = false;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSize = false;
            Controls.Add(splitMain);
            Controls.Add(lblStatus);
            Controls.Add(pnlBottom);
            Dock = DockStyle.Fill;
            Name = "CategoryManagerControl";
            Size = new Size(780, 480);
            splitMain.Panel1.ResumeLayout(false);
            splitMain.Panel1.PerformLayout();
            splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitMain).EndInit();
            splitMain.ResumeLayout(false);
            tabPeriods.ResumeLayout(false);
            tabTransactions.ResumeLayout(false);
            tabsDetail.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)gridCategories).EndInit();
            ((System.ComponentModel.ISupportInitialize)gridPeriods).EndInit();
            ((System.ComponentModel.ISupportInitialize)gridTransactions).EndInit();
            pnlTransactionBar.ResumeLayout(false);
            pnlBottom.ResumeLayout(false);
            pnlBottom.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private SplitContainer splitMain;
        private TextBox txtSearch;
        private DataGridView gridCategories;
        private ThmsTabControl tabsDetail;
        private TabPage tabPeriods;
        private TabPage tabTransactions;
        private Label lblPeriodHint;
        private DataGridView gridPeriods;
        private FlowLayoutPanel pnlTransactionBar;
        private ThmsButton btnSplitTransaction;
        private DataGridView gridTransactions;
        private FlowLayoutPanel pnlBottom;
        private ThmsButton btnAdd;
        private ThmsButton btnMerge;
        private ThmsButton btnAddBudget;
        private ThmsButton btnImport;
        private ThmsButton btnClose;
        private Label lblStatus;
    }
}
