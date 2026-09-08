namespace THMS.UI.WinForms.Controls
{
    partial class BudgetRuleEditor
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
            lblName = new Label();
            txtName = new TextBox();
            lblAmount = new Label();
            numAmount = new NumericUpDown();
            lblFrequency = new Label();
            cmbFrequency = new ComboBox();
            chkActive = new CheckBox();
            lblCategories = new Label();
            treeCategories = new TreeView();
            btnManageCategories = new Button();
            pnlButtons = new Panel();
            btnAdd = new Button();
            btnSave = new Button();
            btnDelete = new Button();
            btnClose = new Button();
            ((System.ComponentModel.ISupportInitialize)numAmount).BeginInit();
            pnlButtons.SuspendLayout();
            SuspendLayout();
            lblName.AutoSize = true;
            lblName.Location = new Point(20, 20);
            lblName.Name = "lblName";
            lblName.Text = "Budget Name:";
            txtName.Location = new Point(150, 16);
            txtName.Name = "txtName";
            txtName.Size = new Size(240, 23);
            lblAmount.AutoSize = true;
            lblAmount.Location = new Point(20, 56);
            lblAmount.Name = "lblAmount";
            lblAmount.Text = "Default Amount:";
            numAmount.DecimalPlaces = 2;
            numAmount.Location = new Point(150, 52);
            numAmount.Maximum = new decimal(new int[] { 100000000, 0, 0, 0 });
            numAmount.Minimum = new decimal(new int[] { 100000000, 0, 0, -2147483648 });
            numAmount.Name = "numAmount";
            numAmount.Size = new Size(240, 23);
            lblFrequency.AutoSize = true;
            lblFrequency.Location = new Point(20, 92);
            lblFrequency.Name = "lblFrequency";
            lblFrequency.Text = "Frequency:";
            cmbFrequency.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFrequency.Location = new Point(150, 88);
            cmbFrequency.Name = "cmbFrequency";
            cmbFrequency.Size = new Size(240, 23);
            chkActive.AutoSize = true;
            chkActive.Checked = true;
            chkActive.CheckState = CheckState.Checked;
            chkActive.Location = new Point(150, 124);
            chkActive.Name = "chkActive";
            chkActive.Text = "Active";
            lblCategories.AutoSize = true;
            lblCategories.Location = new Point(20, 160);
            lblCategories.Name = "lblCategories";
            lblCategories.Text = "Categories:";
            treeCategories.CheckBoxes = true;
            treeCategories.Location = new Point(150, 160);
            treeCategories.Name = "treeCategories";
            treeCategories.Size = new Size(240, 160);
            btnManageCategories.Location = new Point(150, 328);
            btnManageCategories.Name = "btnManageCategories";
            btnManageCategories.Size = new Size(240, 28);
            btnManageCategories.Text = "Manage Categories…";
            btnManageCategories.Click += OnManageCategories;
            pnlButtons.Controls.Add(btnAdd);
            pnlButtons.Controls.Add(btnSave);
            pnlButtons.Controls.Add(btnDelete);
            pnlButtons.Controls.Add(btnClose);
            pnlButtons.Dock = DockStyle.Bottom;
            pnlButtons.Height = 60;
            pnlButtons.Name = "pnlButtons";
            btnAdd.Location = new Point(12, 12);
            btnAdd.Name = "btnAdd";
            btnAdd.Size = new Size(90, 36);
            btnAdd.Text = "Add";
            btnAdd.Click += OnAdd;
            btnSave.Location = new Point(110, 12);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(90, 36);
            btnSave.Text = "Save";
            btnSave.Click += OnSave;
            btnDelete.Location = new Point(208, 12);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(90, 36);
            btnDelete.Text = "Delete";
            btnDelete.Click += OnDelete;
            btnClose.Location = new Point(306, 12);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(90, 36);
            btnClose.Text = "Close";
            btnClose.Click += OnClose;
            AcceptButton = btnSave;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnClose;
            ClientSize = new Size(420, 430);
            Controls.Add(lblName);
            Controls.Add(txtName);
            Controls.Add(lblAmount);
            Controls.Add(numAmount);
            Controls.Add(lblFrequency);
            Controls.Add(cmbFrequency);
            Controls.Add(chkActive);
            Controls.Add(lblCategories);
            Controls.Add(treeCategories);
            Controls.Add(btnManageCategories);
            Controls.Add(pnlButtons);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "BudgetRuleEditor";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Budget Rule";
            ((System.ComponentModel.ISupportInitialize)numAmount).EndInit();
            pnlButtons.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblName;
        private TextBox txtName;
        private Label lblAmount;
        private NumericUpDown numAmount;
        private Label lblFrequency;
        private ComboBox cmbFrequency;
        private CheckBox chkActive;
        private Label lblCategories;
        private TreeView treeCategories;
        private Button btnManageCategories;
        private Panel pnlButtons;
        private Button btnAdd;
        private Button btnSave;
        private Button btnDelete;
        private Button btnClose;
    }
}
