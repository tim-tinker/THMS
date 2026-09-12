namespace THMS.UI.WinForms.Controls
{
    partial class RecurringRuleEditor
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();

            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            lblRuleType = new Label();
            cmbRuleType = new ComboBox();
            lblAccount = new Label();
            cmbAccount = new ComboBox();
            lblFromAccount = new Label();
            cmbFromAccount = new ComboBox();
            lblToAccount = new Label();
            cmbToAccount = new ComboBox();
            lblDescription = new Label();
            txtDescription = new TextBox();
            lblAmount = new Label();
            numAmount = new NumericUpDown();
            lblCategory = new Label();
            cmbCategory = new ComboBox();
            lblFrequency = new Label();
            cmbFrequency = new ComboBox();
            lblNextOccurrence = new Label();
            dtNextOccurrence = new DateTimePicker();
            pnlButtons = new Panel();
            btnAdd = new Button();
            btnSave = new Button();
            btnDelete = new Button();
            btnClose = new Button();
            btnEditSplits = new Button();
            ((System.ComponentModel.ISupportInitialize)numAmount).BeginInit();
            pnlButtons.SuspendLayout();
            SuspendLayout();
            // 
            // lblRuleType
            // 
            lblRuleType.AutoSize = true;
            lblRuleType.Location = new Point(20, 20);
            lblRuleType.Name = "lblRuleType";
            lblRuleType.Size = new Size(80, 15);
            lblRuleType.TabIndex = 0;
            lblRuleType.Text = "Rule Type:";
            // 
            // cmbRuleType
            // 
            cmbRuleType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbRuleType.FormattingEnabled = true;
            cmbRuleType.Location = new Point(150, 16);
            cmbRuleType.Name = "cmbRuleType";
            cmbRuleType.Size = new Size(220, 23);
            cmbRuleType.TabIndex = 1;
            cmbRuleType.SelectedIndexChanged += OnRuleTypeChanged;
            // 
            // lblAccount
            // 
            lblAccount.AutoSize = true;
            lblAccount.Location = new Point(20, 60);
            lblAccount.Name = "lblAccount";
            lblAccount.Size = new Size(53, 15);
            lblAccount.TabIndex = 2;
            lblAccount.Text = "Account:";
            // 
            // cmbAccount
            // 
            cmbAccount.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbAccount.FormattingEnabled = true;
            cmbAccount.Location = new Point(150, 56);
            cmbAccount.Name = "cmbAccount";
            cmbAccount.Size = new Size(220, 23);
            cmbAccount.TabIndex = 3;
            // 
            // lblFromAccount
            // 
            lblFromAccount.AutoSize = true;
            lblFromAccount.Location = new Point(20, 60);
            lblFromAccount.Name = "lblFromAccount";
            lblFromAccount.Size = new Size(85, 15);
            lblFromAccount.TabIndex = 4;
            lblFromAccount.Text = "From Account:";
            lblFromAccount.Visible = false;
            // 
            // cmbFromAccount
            // 
            cmbFromAccount.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFromAccount.FormattingEnabled = true;
            cmbFromAccount.Location = new Point(150, 56);
            cmbFromAccount.Name = "cmbFromAccount";
            cmbFromAccount.Size = new Size(220, 23);
            cmbFromAccount.TabIndex = 5;
            cmbFromAccount.Visible = false;
            // 
            // lblToAccount
            // 
            lblToAccount.AutoSize = true;
            lblToAccount.Location = new Point(20, 100);
            lblToAccount.Name = "lblToAccount";
            lblToAccount.Size = new Size(71, 15);
            lblToAccount.TabIndex = 6;
            lblToAccount.Text = "To Account:";
            lblToAccount.Visible = false;
            // 
            // cmbToAccount
            // 
            cmbToAccount.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbToAccount.FormattingEnabled = true;
            cmbToAccount.Location = new Point(150, 96);
            cmbToAccount.Name = "cmbToAccount";
            cmbToAccount.Size = new Size(220, 23);
            cmbToAccount.TabIndex = 7;
            cmbToAccount.Visible = false;
            // 
            // lblDescription
            // 
            lblDescription.AutoSize = true;
            lblDescription.Location = new Point(20, 140);
            lblDescription.Name = "lblDescription";
            lblDescription.Size = new Size(70, 15);
            lblDescription.TabIndex = 8;
            lblDescription.Text = "Description:";
            // 
            // txtDescription
            // 
            txtDescription.Location = new Point(150, 136);
            txtDescription.Name = "txtDescription";
            txtDescription.Size = new Size(220, 23);
            txtDescription.TabIndex = 9;
            // 
            // lblAmount
            // 
            lblAmount.AutoSize = true;
            lblAmount.Location = new Point(20, 180);
            lblAmount.Name = "lblAmount";
            lblAmount.Size = new Size(54, 15);
            lblAmount.TabIndex = 10;
            lblAmount.Text = "Amount:";
            // 
            // numAmount
            // 
            numAmount.DecimalPlaces = 2;
            numAmount.Location = new Point(150, 176);
            numAmount.Maximum = new decimal(new int[] { 100000000, 0, 0, 0 });
            numAmount.Minimum = new decimal(new int[] { 100000000, 0, 0, -2147483648 });
            numAmount.Name = "numAmount";
            numAmount.Size = new Size(220, 23);
            numAmount.TabIndex = 11;
            // 
            // lblCategory
            // 
            lblCategory.AutoSize = true;
            lblCategory.Location = new Point(20, 220);
            lblCategory.Name = "lblCategory";
            lblCategory.Size = new Size(58, 15);
            lblCategory.TabIndex = 12;
            lblCategory.Text = "Category:";
            // 
            // cmbCategory
            // 
            cmbCategory.FormattingEnabled = true;
            cmbCategory.Location = new Point(150, 216);
            cmbCategory.Name = "cmbCategory";
            cmbCategory.Size = new Size(220, 23);
            cmbCategory.TabIndex = 13;
            // 
            // lblFrequency
            // 
            lblFrequency.AutoSize = true;
            lblFrequency.Location = new Point(20, 260);
            lblFrequency.Name = "lblFrequency";
            lblFrequency.Size = new Size(65, 15);
            lblFrequency.TabIndex = 14;
            lblFrequency.Text = "Frequency:";
            // 
            // cmbFrequency
            // 
            cmbFrequency.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFrequency.FormattingEnabled = true;
            cmbFrequency.Location = new Point(150, 256);
            cmbFrequency.Name = "cmbFrequency";
            cmbFrequency.Size = new Size(220, 23);
            cmbFrequency.TabIndex = 15;
            // 
            // lblNextOccurrence
            // 
            lblNextOccurrence.AutoSize = true;
            lblNextOccurrence.Location = new Point(20, 300);
            lblNextOccurrence.Name = "lblNextOccurrence";
            lblNextOccurrence.Size = new Size(104, 15);
            lblNextOccurrence.TabIndex = 16;
            lblNextOccurrence.Text = "Next Occurrence:";
            // 
            // dtNextOccurrence
            // 
            dtNextOccurrence.Format = DateTimePickerFormat.Short;
            dtNextOccurrence.Location = new Point(150, 296);
            dtNextOccurrence.Name = "dtNextOccurrence";
            dtNextOccurrence.Size = new Size(220, 23);
            dtNextOccurrence.TabIndex = 17;
            // 
            // btnEditSplits
            // 
            btnEditSplits.Location = new Point(150, 332);
            btnEditSplits.Name = "btnEditSplits";
            btnEditSplits.Size = new Size(220, 28);
            btnEditSplits.TabIndex = 18;
            btnEditSplits.Text = "Edit Splits";
            btnEditSplits.UseVisualStyleBackColor = true;
            btnEditSplits.Click += OnEditSplits;
            // 
            // pnlButtons
            // 
            pnlButtons.Controls.Add(btnAdd);
            pnlButtons.Controls.Add(btnSave);
            pnlButtons.Controls.Add(btnDelete);
            pnlButtons.Controls.Add(btnClose);
            pnlButtons.Dock = DockStyle.Bottom;
            pnlButtons.Location = new Point(0, 371);
            pnlButtons.Name = "pnlButtons";
            pnlButtons.Size = new Size(414, 60);
            pnlButtons.TabIndex = 19;
            // 
            // btnAdd
            // 
            btnAdd.Location = new Point(12, 12);
            btnAdd.Name = "btnAdd";
            btnAdd.Size = new Size(90, 36);
            btnAdd.TabIndex = 0;
            btnAdd.Text = "Add";
            btnAdd.UseVisualStyleBackColor = true;
            btnAdd.Click += OnAdd;
            // 
            // btnSave
            // 
            btnSave.Location = new Point(110, 12);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(90, 36);
            btnSave.TabIndex = 1;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += OnSave;
            // 
            // btnDelete
            // 
            btnDelete.Location = new Point(208, 12);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(90, 36);
            btnDelete.TabIndex = 2;
            btnDelete.Text = "Delete";
            btnDelete.UseVisualStyleBackColor = true;
            btnDelete.Click += OnDelete;
            // 
            // btnClose
            // 
            btnClose.Location = new Point(306, 12);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(90, 36);
            btnClose.TabIndex = 3;
            btnClose.Text = "Close";
            btnClose.UseVisualStyleBackColor = true;
            btnClose.Click += OnClose;
            // 
            // RecurringRuleEditor
            // 
            AcceptButton = btnSave;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnClose;
            ClientSize = new Size(414, 431);
            Controls.Add(lblRuleType);
            Controls.Add(cmbRuleType);
            Controls.Add(lblAccount);
            Controls.Add(cmbAccount);
            Controls.Add(lblFromAccount);
            Controls.Add(cmbFromAccount);
            Controls.Add(lblToAccount);
            Controls.Add(cmbToAccount);
            Controls.Add(lblDescription);
            Controls.Add(txtDescription);
            Controls.Add(lblAmount);
            Controls.Add(numAmount);
            Controls.Add(lblCategory);
            Controls.Add(cmbCategory);
            Controls.Add(lblFrequency);
            Controls.Add(cmbFrequency);
            Controls.Add(lblNextOccurrence);
            Controls.Add(dtNextOccurrence);
            Controls.Add(btnEditSplits);
            Controls.Add(pnlButtons);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "RecurringRuleEditor";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Recurring Rule";
            ((System.ComponentModel.ISupportInitialize)numAmount).EndInit();
            pnlButtons.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblRuleType;
        private ComboBox cmbRuleType;
        private Label lblAccount;
        private ComboBox cmbAccount;
        private Label lblFromAccount;
        private ComboBox cmbFromAccount;
        private Label lblToAccount;
        private ComboBox cmbToAccount;
        private Label lblDescription;
        private TextBox txtDescription;
        private Label lblAmount;
        private NumericUpDown numAmount;
        private Label lblCategory;
        private ComboBox cmbCategory;
        private Label lblFrequency;
        private ComboBox cmbFrequency;
        private Label lblNextOccurrence;
        private DateTimePicker dtNextOccurrence;
        private Button btnEditSplits;
        private Panel pnlButtons;
        private Button btnAdd;
        private Button btnSave;
        private Button btnDelete;
        private Button btnClose;
    }
}
