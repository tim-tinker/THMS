namespace THMS.UI.WinForms.Controls
{
    partial class CategoryMergeDialog
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
            lblHelp = new Label();
            lblSource = new Label();
            cmbSource = new ComboBox();
            lblTarget = new Label();
            cmbTarget = new ComboBox();
            btnMerge = new ThmsButton();
            btnCancel = new ThmsButton();
            SuspendLayout();
            lblHelp.AutoSize = true;
            lblHelp.Location = new Point(16, 16);
            lblHelp.MaximumSize = new Size(360, 0);
            lblHelp.Name = "lblHelp";
            lblHelp.Text = "Move transactions, budget rules, and learned mappings from the source category into the target. The source becomes inactive.";
            lblSource.AutoSize = true;
            lblSource.Location = new Point(16, 72);
            lblSource.Name = "lblSource";
            lblSource.Text = "Merge this category:";
            cmbSource.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbSource.Location = new Point(16, 92);
            cmbSource.Name = "cmbSource";
            cmbSource.Size = new Size(360, 23);
            lblTarget.AutoSize = true;
            lblTarget.Location = new Point(16, 128);
            lblTarget.Name = "lblTarget";
            lblTarget.Text = "Into this category:";
            cmbTarget.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTarget.Location = new Point(16, 148);
            cmbTarget.Name = "cmbTarget";
            cmbTarget.Size = new Size(360, 23);
            btnMerge.Location = new Point(186, 220);
            btnMerge.Name = "btnMerge";
            btnMerge.Size = new Size(90, 36);
            btnMerge.Text = "Merge";
            btnMerge.Click += OnMerge;
            btnCancel.Location = new Point(286, 220);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(90, 36);
            btnCancel.Text = "Cancel";
            btnCancel.Click += OnCancel;
            AcceptButton = btnMerge;
            CancelButton = btnCancel;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(392, 288);
            Controls.Add(lblHelp);
            Controls.Add(lblSource);
            Controls.Add(cmbSource);
            Controls.Add(lblTarget);
            Controls.Add(cmbTarget);
            Controls.Add(btnMerge);
            Controls.Add(btnCancel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "CategoryMergeDialog";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Merge Categories";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblHelp;
        private Label lblSource;
        private ComboBox cmbSource;
        private Label lblTarget;
        private ComboBox cmbTarget;
        private ThmsButton btnMerge;
        private ThmsButton btnCancel;
    }
}
