namespace THMS.UI.WinForms.Controls
{
    partial class BudgetBalanceTransferDialog
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
            lblFrom = new Label();
            lblSource = new Label();
            lblTarget = new Label();
            cmbTarget = new ComboBox();
            lblAmount = new Label();
            numAmount = new NumericUpDown();
            btnTransfer = new Button();
            btnCancel = new Button();
            ((System.ComponentModel.ISupportInitialize)numAmount).BeginInit();
            SuspendLayout();
            lblHelp.Location = new Point(16, 16);
            lblHelp.MaximumSize = new Size(360, 0);
            lblHelp.Name = "lblHelp";
            lblHelp.Size = new Size(360, 48);
            lblHelp.Text = "Move starting balance from this budget's current period to another open budget on the same account.";
            lblFrom.AutoSize = true;
            lblFrom.Location = new Point(16, 72);
            lblFrom.Name = "lblFrom";
            lblFrom.Text = "From:";
            lblSource.Location = new Point(16, 92);
            lblSource.Name = "lblSource";
            lblSource.Size = new Size(360, 36);
            lblTarget.AutoSize = true;
            lblTarget.Location = new Point(16, 136);
            lblTarget.Name = "lblTarget";
            lblTarget.Text = "To:";
            cmbTarget.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTarget.Location = new Point(16, 156);
            cmbTarget.Name = "cmbTarget";
            cmbTarget.Size = new Size(360, 23);
            lblAmount.AutoSize = true;
            lblAmount.Location = new Point(16, 192);
            lblAmount.Name = "lblAmount";
            lblAmount.Text = "Amount:";
            numAmount.DecimalPlaces = 2;
            numAmount.Location = new Point(16, 212);
            numAmount.Maximum = new decimal(new int[] { 100000000, 0, 0, 0 });
            numAmount.Name = "numAmount";
            numAmount.Size = new Size(360, 23);
            btnTransfer.Location = new Point(186, 256);
            btnTransfer.Name = "btnTransfer";
            btnTransfer.Size = new Size(90, 32);
            btnTransfer.Text = "Transfer";
            btnTransfer.Click += OnTransfer;
            btnCancel.Location = new Point(286, 256);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(90, 32);
            btnCancel.Text = "Cancel";
            btnCancel.Click += OnCancel;
            AcceptButton = btnTransfer;
            CancelButton = btnCancel;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(392, 308);
            Controls.Add(lblHelp);
            Controls.Add(lblFrom);
            Controls.Add(lblSource);
            Controls.Add(lblTarget);
            Controls.Add(cmbTarget);
            Controls.Add(lblAmount);
            Controls.Add(numAmount);
            Controls.Add(btnTransfer);
            Controls.Add(btnCancel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "BudgetBalanceTransferDialog";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Transfer Balance";
            ((System.ComponentModel.ISupportInitialize)numAmount).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblHelp;
        private Label lblFrom;
        private Label lblSource;
        private Label lblTarget;
        private ComboBox cmbTarget;
        private Label lblAmount;
        private NumericUpDown numAmount;
        private Button btnTransfer;
        private Button btnCancel;
    }
}
