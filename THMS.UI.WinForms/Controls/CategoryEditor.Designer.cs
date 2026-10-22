namespace THMS.UI.WinForms.Controls
{
    partial class CategoryEditor
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
            lblParent = new Label();
            cmbParent = new ComboBox();
            lblRole = new Label();
            cmbRole = new ComboBox();
            chkActive = new CheckBox();
            btnSave = new ThmsButton();
            btnDeactivate = new ThmsButton();
            btnCancel = new ThmsButton();
            SuspendLayout();
            lblName.AutoSize = true;
            lblName.Location = new Point(20, 20);
            lblName.Name = "lblName";
            lblName.Text = "Name:";
            txtName.Location = new Point(130, 16);
            txtName.Name = "txtName";
            txtName.Size = new Size(220, 23);
            lblParent.AutoSize = true;
            lblParent.Location = new Point(20, 56);
            lblParent.Name = "lblParent";
            lblParent.Text = "Parent:";
            cmbParent.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbParent.Location = new Point(130, 52);
            cmbParent.Name = "cmbParent";
            cmbParent.Size = new Size(220, 23);
            lblRole.AutoSize = true;
            lblRole.Location = new Point(20, 92);
            lblRole.Name = "lblRole";
            lblRole.Text = "Role:";
            cmbRole.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbRole.Location = new Point(130, 88);
            cmbRole.Name = "cmbRole";
            cmbRole.Size = new Size(220, 23);
            chkActive.AutoSize = true;
            chkActive.Location = new Point(130, 124);
            chkActive.Name = "chkActive";
            chkActive.Text = "Active";
            btnSave.Location = new Point(130, 164);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(90, 36);
            btnSave.Text = "Save";
            btnSave.Click += OnSave;
            btnDeactivate.Destructive = true;
            btnDeactivate.Location = new Point(20, 164);
            btnDeactivate.Name = "btnDeactivate";
            btnDeactivate.Size = new Size(100, 36);
            btnDeactivate.Text = "Deactivate";
            btnDeactivate.Visible = false;
            btnDeactivate.Click += OnDeactivate;
            btnCancel.Location = new Point(230, 164);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(90, 36);
            btnCancel.Text = "Cancel";
            btnCancel.Click += OnCancel;
            AcceptButton = btnSave;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(380, 224);
            Controls.Add(lblName);
            Controls.Add(txtName);
            Controls.Add(lblParent);
            Controls.Add(cmbParent);
            Controls.Add(lblRole);
            Controls.Add(cmbRole);
            Controls.Add(chkActive);
            Controls.Add(btnDeactivate);
            Controls.Add(btnSave);
            Controls.Add(btnCancel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "CategoryEditor";
            StartPosition = FormStartPosition.CenterParent;
            Text = "New Category";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblName;
        private TextBox txtName;
        private Label lblParent;
        private ComboBox cmbParent;
        private Label lblRole;
        private ComboBox cmbRole;
        private CheckBox chkActive;
        private ThmsButton btnSave;
        private ThmsButton btnDeactivate;
        private ThmsButton btnCancel;
    }
}
