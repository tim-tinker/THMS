namespace THMS.UI.WinForms
{
    partial class RegisterForm
    {
        private System.ComponentModel.IContainer components = null;
        private SplitContainer split;
        private Label lblAccounts;
        private Controls.AccountUpdaterControl accountUpdater;
        private Panel pnlTransactions;
        private Label lblTransactions;
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
            pnlTransactions = new Panel();
            lblTransactions = new Label();
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
            ((System.ComponentModel.ISupportInitialize)split).BeginInit();
            split.Panel1.SuspendLayout();
            split.Panel2.SuspendLayout();
            split.SuspendLayout();
            pnlTransactions.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)gridTransactions).BeginInit();
            pnlTxButtons.SuspendLayout();
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
            split.Panel2.Controls.Add(pnlTransactions);
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
            // pnlTransactions
            //
            pnlTransactions.Controls.Add(gridTransactions);
            pnlTransactions.Controls.Add(lblTxStatus);
            pnlTransactions.Controls.Add(pnlTxButtons);
            pnlTransactions.Controls.Add(lblTransactions);
            pnlTransactions.Dock = DockStyle.Fill;
            pnlTransactions.Name = "pnlTransactions";
            //
            // lblTransactions
            //
            lblTransactions.Dock = DockStyle.Top;
            lblTransactions.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblTransactions.Height = 32;
            lblTransactions.Name = "lblTransactions";
            lblTransactions.Padding = new Padding(8, 4, 8, 0);
            lblTransactions.Text = "Posted Transactions";
            lblTransactions.TextAlign = ContentAlignment.MiddleLeft;
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
            pnlTransactions.ResumeLayout(false);
            pnlTransactions.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)gridTransactions).EndInit();
            pnlTxButtons.ResumeLayout(false);
            ResumeLayout(false);
        }
    }
}
