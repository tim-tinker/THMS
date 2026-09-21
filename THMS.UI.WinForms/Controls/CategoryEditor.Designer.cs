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
            btnSave = new ThmsButton();
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
            btnSave.Location = new Point(130, 124);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(90, 36);
            btnSave.Text = "Save";
            btnSave.Click += OnSave;
            btnCancel.Location = new Point(230, 124);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(90, 36);
            btnCancel.Text = "Cancel";
            btnCancel.Click += OnCancel;
            AcceptButton = btnSave;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(380, 192);
            Controls.Add(lblName);
            Controls.Add(txtName);
            Controls.Add(lblParent);
            Controls.Add(cmbParent);
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
        private ThmsButton btnSave;
        private ThmsButton btnCancel;
    }
}
