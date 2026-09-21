namespace THMS.UI.WinForms.Controls
{
    partial class StatementEditorDialog
    {
        private System.ComponentModel.IContainer components = null;
        private TableLayoutPanel layout;
        private Panel pnlTypeSelector;
        private TableLayoutPanel typeSelectorLayout;
        private Label lblStatementType;
        private ComboBox cboStatementType;
        private Label lblAccount;
        private ComboBox cboAccount;
        private ThmsButton btnNewAccount;
        private TableLayoutPanel pnlCommon;
        private Label lblStatementDate;
        private DateTimePicker dtStatementDate;
        private Label lblDueDate;
        private DateTimePicker dtDueDate;
        private Label lblAmountDue;
        private TextBox txtAmountDue;
        private Label lblNotes;
        private TextBox txtNotes;
        private Panel pnlTypeSpecific;
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
            pnlTypeSelector = new Panel();
            typeSelectorLayout = new TableLayoutPanel();
            lblStatementType = new Label();
            cboStatementType = new ComboBox();
            lblAccount = new Label();
            cboAccount = new ComboBox();
            btnNewAccount = new ThmsButton();
            pnlCommon = new TableLayoutPanel();
            lblStatementDate = new Label();
            dtStatementDate = new DateTimePicker();
            lblDueDate = new Label();
            dtDueDate = new DateTimePicker();
            lblAmountDue = new Label();
            txtAmountDue = new TextBox();
            lblNotes = new Label();
            txtNotes = new TextBox();
            pnlTypeSpecific = new Panel();
            pnlButtons = new FlowLayoutPanel();
            btnSave = new ThmsButton();
            btnCancel = new ThmsButton();
            layout.SuspendLayout();
            pnlTypeSelector.SuspendLayout();
            typeSelectorLayout.SuspendLayout();
            pnlCommon.SuspendLayout();
            pnlButtons.SuspendLayout();
            SuspendLayout();

            layout.ColumnCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.Controls.Add(pnlTypeSelector, 0, 0);
            layout.Controls.Add(pnlCommon, 0, 1);
            layout.Controls.Add(pnlTypeSpecific, 0, 2);
            layout.Controls.Add(pnlButtons, 0, 3);
            layout.Dock = DockStyle.Fill;
            layout.Name = "layout";
            layout.Padding = new Padding(12);
            layout.RowCount = 4;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 96F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            typeSelectorLayout.ColumnCount = 3;
            typeSelectorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
            typeSelectorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            typeSelectorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            typeSelectorLayout.RowCount = 2;
            typeSelectorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            typeSelectorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            typeSelectorLayout.Dock = DockStyle.Fill;
            typeSelectorLayout.Name = "typeSelectorLayout";
            typeSelectorLayout.Controls.Add(lblStatementType, 0, 0);
            typeSelectorLayout.Controls.Add(cboStatementType, 1, 0);
            typeSelectorLayout.SetColumnSpan(cboStatementType, 2);
            typeSelectorLayout.Controls.Add(lblAccount, 0, 1);
            typeSelectorLayout.Controls.Add(cboAccount, 1, 1);
            typeSelectorLayout.Controls.Add(btnNewAccount, 2, 1);

            pnlTypeSelector.Controls.Add(typeSelectorLayout);
            pnlTypeSelector.Dock = DockStyle.Fill;
            pnlTypeSelector.Name = "pnlTypeSelector";

            lblStatementType.AutoSize = false;
            lblStatementType.Dock = DockStyle.Fill;
            lblStatementType.Name = "lblStatementType";
            lblStatementType.Text = "Statement Type";
            lblStatementType.TextAlign = ContentAlignment.MiddleLeft;
            cboStatementType.DropDownStyle = ComboBoxStyle.DropDownList;
            cboStatementType.Dock = DockStyle.Fill;
            cboStatementType.DrawMode = DrawMode.OwnerDrawFixed;
            cboStatementType.IntegralHeight = false;
            cboStatementType.Name = "cboStatementType";
            cboStatementType.SelectedIndexChanged += OnStatementTypeChanged;
            lblAccount.AutoSize = false;
            lblAccount.Dock = DockStyle.Fill;
            lblAccount.Name = "lblAccount";
            lblAccount.Text = "Account";
            lblAccount.TextAlign = ContentAlignment.MiddleLeft;
            cboAccount.DropDownStyle = ComboBoxStyle.DropDownList;
            cboAccount.Dock = DockStyle.Fill;
            cboAccount.DrawMode = DrawMode.OwnerDrawFixed;
            cboAccount.IntegralHeight = false;
            cboAccount.Name = "cboAccount";
            cboAccount.SelectedIndexChanged += OnStatementAccountChanged;
            btnNewAccount.AutoSize = true;
            btnNewAccount.Margin = new Padding(6, 4, 0, 4);
            btnNewAccount.Name = "btnNewAccount";
            btnNewAccount.Text = "New Account";
            btnNewAccount.Click += OnNewAccount;

            pnlCommon.ColumnCount = 4;
            pnlCommon.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            pnlCommon.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            pnlCommon.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            pnlCommon.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            pnlCommon.RowCount = 3;
            pnlCommon.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            pnlCommon.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            pnlCommon.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            pnlCommon.AutoSize = true;
            pnlCommon.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            pnlCommon.Dock = DockStyle.Fill;
            pnlCommon.Name = "pnlCommon";
            pnlCommon.Padding = new Padding(0, 0, 0, 4);
            pnlCommon.Controls.Add(lblStatementDate, 0, 0);
            pnlCommon.Controls.Add(dtStatementDate, 1, 0);
            pnlCommon.Controls.Add(lblDueDate, 2, 0);
            pnlCommon.Controls.Add(dtDueDate, 3, 0);
            pnlCommon.Controls.Add(lblAmountDue, 0, 1);
            pnlCommon.Controls.Add(txtAmountDue, 1, 1);
            pnlCommon.SetColumnSpan(txtAmountDue, 3);
            pnlCommon.Controls.Add(lblNotes, 0, 2);
            pnlCommon.Controls.Add(txtNotes, 1, 2);
            pnlCommon.SetColumnSpan(txtNotes, 3);

            lblStatementDate.AutoSize = false;
            lblStatementDate.Dock = DockStyle.Fill;
            lblStatementDate.Name = "lblStatementDate";
            lblStatementDate.Text = "Statement Date";
            lblStatementDate.TextAlign = ContentAlignment.MiddleLeft;
            dtStatementDate.Dock = DockStyle.Fill;
            dtStatementDate.Format = DateTimePickerFormat.Short;
            dtStatementDate.Margin = new Padding(0, 4, 8, 4);
            dtStatementDate.Name = "dtStatementDate";
            lblDueDate.AutoSize = false;
            lblDueDate.Dock = DockStyle.Fill;
            lblDueDate.Name = "lblDueDate";
            lblDueDate.Text = "Due Date";
            lblDueDate.TextAlign = ContentAlignment.MiddleLeft;
            dtDueDate.Dock = DockStyle.Fill;
            dtDueDate.Format = DateTimePickerFormat.Short;
            dtDueDate.Margin = new Padding(0, 4, 0, 4);
            dtDueDate.Name = "dtDueDate";
            lblAmountDue.AutoSize = false;
            lblAmountDue.Dock = DockStyle.Fill;
            lblAmountDue.Name = "lblAmountDue";
            lblAmountDue.Text = "Amount Due";
            lblAmountDue.TextAlign = ContentAlignment.MiddleLeft;
            txtAmountDue.Dock = DockStyle.Fill;
            txtAmountDue.Margin = new Padding(0, 4, 0, 4);
            txtAmountDue.Name = "txtAmountDue";
            lblNotes.AutoSize = false;
            lblNotes.Dock = DockStyle.Fill;
            lblNotes.Name = "lblNotes";
            lblNotes.Text = "Notes";
            lblNotes.TextAlign = ContentAlignment.MiddleLeft;
            txtNotes.Dock = DockStyle.Fill;
            txtNotes.Margin = new Padding(0, 4, 0, 4);
            txtNotes.Multiline = true;
            txtNotes.Name = "txtNotes";
            txtNotes.ScrollBars = ScrollBars.Vertical;

            pnlTypeSpecific.AutoScroll = true;
            pnlTypeSpecific.Dock = DockStyle.Fill;
            pnlTypeSpecific.Name = "pnlTypeSpecific";

            pnlButtons.AutoSize = true;
            pnlButtons.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            pnlButtons.Dock = DockStyle.Fill;
            pnlButtons.FlowDirection = FlowDirection.RightToLeft;
            pnlButtons.Name = "pnlButtons";
            pnlButtons.Padding = new Padding(12, 12, 12, 16);
            pnlButtons.WrapContents = false;
            btnSave.AutoSize = true;
            btnSave.Name = "btnSave";
            btnSave.Text = "Save";
            btnSave.Click += OnSave;
            btnCancel.AutoSize = true;
            btnCancel.Name = "btnCancel";
            btnCancel.Text = "Cancel";
            btnCancel.Click += OnCancel;
            pnlButtons.Controls.Add(btnCancel);
            pnlButtons.Controls.Add(btnSave);

            AcceptButton = btnSave;
            CancelButton = btnCancel;
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(720, 540);
            Controls.Add(layout);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = false;
            MinimumSize = new Size(560, 420);
            Name = "StatementEditorDialog";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Statement";
            layout.ResumeLayout(false);
            typeSelectorLayout.ResumeLayout(false);
            typeSelectorLayout.PerformLayout();
            pnlTypeSelector.ResumeLayout(false);
            pnlTypeSelector.PerformLayout();
            pnlCommon.ResumeLayout(false);
            pnlCommon.PerformLayout();
            pnlButtons.ResumeLayout(false);
            ResumeLayout(false);
        }
    }
}
