namespace THMS.UI.WinForms.Controls
{
    partial class AccountUpdaterControl
    {
        private System.ComponentModel.IContainer components = null;
        private DataGridView gridAccounts;
        private Panel pnlButtons;
        private Button btnAdd;
        private Button btnEdit;
        private Button btnDelete;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            gridAccounts = new DataGridView();
            pnlButtons = new Panel();
            btnAdd = new Button();
            btnEdit = new Button();
            btnDelete = new Button();
            ((System.ComponentModel.ISupportInitialize)gridAccounts).BeginInit();
            pnlButtons.SuspendLayout();
            SuspendLayout();
            // 
            // gridAccounts
            // 
            gridAccounts.AllowUserToAddRows = false;
            gridAccounts.AllowUserToDeleteRows = false;
            gridAccounts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridAccounts.ColumnHeadersHeight = 40;
            gridAccounts.Dock = DockStyle.Fill;
            gridAccounts.Location = new Point(0, 0);
            gridAccounts.Name = "gridAccounts";
            gridAccounts.ReadOnly = true;
            gridAccounts.RowHeadersWidth = 72;
            gridAccounts.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridAccounts.TabIndex = 0;
            // 
            // pnlButtons
            // 
            pnlButtons.Controls.Add(btnAdd);
            pnlButtons.Controls.Add(btnEdit);
            pnlButtons.Controls.Add(btnDelete);
            pnlButtons.Dock = DockStyle.Bottom;
            pnlButtons.Location = new Point(0, 0);
            pnlButtons.Name = "pnlButtons";
            pnlButtons.Size = new Size(800, 64);
            pnlButtons.TabIndex = 1;
            // 
            // btnAdd
            // 
            btnAdd.Location = new Point(12, 12);
            btnAdd.Name = "btnAdd";
            btnAdd.Size = new Size(120, 40);
            btnAdd.TabIndex = 0;
            btnAdd.Text = "Add Account";
            btnAdd.Click += OnAddAccount;
            // 
            // btnEdit
            // 
            btnEdit.Location = new Point(144, 12);
            btnEdit.Name = "btnEdit";
            btnEdit.Size = new Size(120, 40);
            btnEdit.TabIndex = 1;
            btnEdit.Text = "Edit Account";
            btnEdit.Click += OnEditAccount;
            // 
            // btnDelete
            // 
            btnDelete.Location = new Point(276, 12);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(120, 40);
            btnDelete.TabIndex = 2;
            btnDelete.Text = "Delete Account";
            btnDelete.Click += OnDeleteAccount;
            // 
            // AccountUpdaterControl
            // 
            Controls.Add(gridAccounts);
            Controls.Add(pnlButtons);
            Name = "AccountUpdaterControl";
            Size = new Size(800, 600);
            ((System.ComponentModel.ISupportInitialize)gridAccounts).EndInit();
            pnlButtons.ResumeLayout(false);
            ResumeLayout(false);
        }
    }
}
