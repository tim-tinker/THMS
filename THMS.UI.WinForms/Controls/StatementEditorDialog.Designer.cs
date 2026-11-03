namespace THMS.UI.WinForms.Controls
{
    partial class StatementEditorDialog
    {
        private System.ComponentModel.IContainer components = null;
        private TableLayoutPanel layout;
        private TableLayoutPanel accountBar;
        private Label lblAccount;
        private ComboBox cboAccount;
        private ThmsButton btnNewAccount;
        private Panel pnlDocument;
        private FlowLayoutPanel pnlButtons;
        private ThmsButton btnSave;
        private ThmsButton btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            layout = new TableLayoutPanel();
            accountBar = new TableLayoutPanel();
            lblAccount = new Label();
            cboAccount = new ComboBox();
            btnNewAccount = new ThmsButton();
            pnlDocument = new Panel();
            pnlButtons = new FlowLayoutPanel();
            btnCancel = new ThmsButton();
            btnSave = new ThmsButton();
            layout.SuspendLayout();
            accountBar.SuspendLayout();
            pnlButtons.SuspendLayout();
            SuspendLayout();
            // 
            // layout
            // 
            layout.BackColor = SystemColors.Control;
            layout.ColumnCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.Controls.Add(accountBar, 0, 0);
            layout.Controls.Add(pnlDocument, 0, 1);
            layout.Controls.Add(pnlButtons, 0, 2);
            layout.Dock = DockStyle.Fill;
            layout.Location = new Point(0, 0);
            layout.Margin = new Padding(0);
            layout.Name = "layout";
            layout.Padding = new Padding(18, 12, 18, 12);
            layout.RowCount = 3;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 84F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle());
            layout.Size = new Size(1260, 1200);
            layout.TabIndex = 0;
            // 
            // accountBar
            // 
            accountBar.ColumnCount = 3;
            accountBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 192F));
            accountBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            accountBar.ColumnStyles.Add(new ColumnStyle());
            accountBar.Controls.Add(lblAccount, 0, 0);
            accountBar.Controls.Add(cboAccount, 1, 0);
            accountBar.Controls.Add(btnNewAccount, 2, 0);
            accountBar.Dock = DockStyle.Fill;
            accountBar.Location = new Point(18, 12);
            accountBar.Margin = new Padding(0, 0, 0, 8);
            accountBar.Name = "accountBar";
            accountBar.RowCount = 1;
            accountBar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            accountBar.Size = new Size(1224, 76);
            accountBar.TabIndex = 0;
            // 
            // lblAccount
            // 
            lblAccount.Dock = DockStyle.Fill;
            lblAccount.Location = new Point(5, 0);
            lblAccount.Margin = new Padding(5, 0, 5, 0);
            lblAccount.Name = "lblAccount";
            lblAccount.Size = new Size(182, 76);
            lblAccount.TabIndex = 0;
            lblAccount.Text = "Account";
            lblAccount.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // cboAccount
            // 
            cboAccount.Dock = DockStyle.Fill;
            cboAccount.DrawMode = DrawMode.OwnerDrawFixed;
            cboAccount.DropDownStyle = ComboBoxStyle.DropDownList;
            cboAccount.IntegralHeight = false;
            cboAccount.Location = new Point(197, 5);
            cboAccount.Margin = new Padding(5);
            cboAccount.Name = "cboAccount";
            cboAccount.Size = new Size(812, 36);
            cboAccount.TabIndex = 1;
            cboAccount.SelectedIndexChanged += OnStatementAccountChanged;
            // 
            // btnNewAccount
            // 
            btnNewAccount.AutoSize = true;
            btnNewAccount.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnNewAccount.BackColor = Color.FromArgb(39, 137, 71);
            btnNewAccount.FlatStyle = FlatStyle.Flat;
            btnNewAccount.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnNewAccount.ForeColor = Color.White;
            btnNewAccount.Location = new Point(1024, 7);
            btnNewAccount.Margin = new Padding(10, 7, 0, 7);
            btnNewAccount.MinimumSize = new Size(0, 63);
            btnNewAccount.Name = "btnNewAccount";
            btnNewAccount.Padding = new Padding(21, 10, 21, 10);
            btnNewAccount.Size = new Size(200, 63);
            btnNewAccount.TabIndex = 2;
            btnNewAccount.Text = "New Account";
            btnNewAccount.UseVisualStyleBackColor = false;
            btnNewAccount.Click += OnNewAccount;
            // 
            // pnlDocument
            // 
            pnlDocument.AutoScroll = true;
            pnlDocument.BackColor = Color.White;
            pnlDocument.Dock = DockStyle.Fill;
            pnlDocument.Location = new Point(18, 96);
            pnlDocument.Margin = new Padding(0);
            pnlDocument.Name = "pnlDocument";
            pnlDocument.Size = new Size(1224, 1007);
            pnlDocument.TabIndex = 1;
            // 
            // pnlButtons
            // 
            pnlButtons.AutoSize = true;
            pnlButtons.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            pnlButtons.Controls.Add(btnCancel);
            pnlButtons.Controls.Add(btnSave);
            pnlButtons.Dock = DockStyle.Fill;
            pnlButtons.FlowDirection = FlowDirection.RightToLeft;
            pnlButtons.Location = new Point(18, 1103);
            pnlButtons.Margin = new Padding(0);
            pnlButtons.Name = "pnlButtons";
            pnlButtons.Padding = new Padding(0, 12, 0, 0);
            pnlButtons.Size = new Size(1224, 85);
            pnlButtons.TabIndex = 2;
            pnlButtons.WrapContents = false;
            // 
            // btnCancel
            // 
            btnCancel.AutoSize = true;
            btnCancel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnCancel.BackColor = Color.FromArgb(39, 137, 71);
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnCancel.ForeColor = Color.White;
            btnCancel.Location = new Point(1093, 17);
            btnCancel.Margin = new Padding(5, 5, 0, 5);
            btnCancel.MinimumSize = new Size(0, 63);
            btnCancel.Name = "btnCancel";
            btnCancel.Padding = new Padding(21, 10, 21, 10);
            btnCancel.Size = new Size(131, 63);
            btnCancel.TabIndex = 0;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = false;
            btnCancel.Click += OnCancel;
            // 
            // btnSave
            // 
            btnSave.AutoSize = true;
            btnSave.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnSave.BackColor = Color.FromArgb(39, 137, 71);
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnSave.ForeColor = Color.White;
            btnSave.Location = new Point(971, 17);
            btnSave.Margin = new Padding(5);
            btnSave.MinimumSize = new Size(0, 63);
            btnSave.Name = "btnSave";
            btnSave.Padding = new Padding(21, 10, 21, 10);
            btnSave.Size = new Size(112, 63);
            btnSave.TabIndex = 1;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = false;
            btnSave.Click += OnSave;
            // 
            // StatementEditorDialog
            // 
            AcceptButton = btnSave;
            AutoScaleDimensions = new SizeF(168F, 168F);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = SystemColors.Control;
            CancelButton = btnCancel;
            ClientSize = new Size(1260, 1200);
            Controls.Add(layout);
            Margin = new Padding(5);
            MinimizeBox = false;
            MinimumSize = new Size(962, 900);
            Name = "StatementEditorDialog";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Statement";
            layout.ResumeLayout(false);
            layout.PerformLayout();
            accountBar.ResumeLayout(false);
            accountBar.PerformLayout();
            pnlButtons.ResumeLayout(false);
            pnlButtons.PerformLayout();
            ResumeLayout(false);
        }
    }
}
