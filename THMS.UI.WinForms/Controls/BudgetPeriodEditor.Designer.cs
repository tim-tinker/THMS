namespace THMS.UI.WinForms.Controls
{
    partial class BudgetPeriodEditor
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
            lblPeriodCaption = new Label();
            lblPeriod = new Label();
            lblStarting = new Label();
            numStarting = new NumericUpDown();
            btnResetStarting = new Button();
            btnTransfer = new Button();
            lblBudgetAmount = new Label();
            numBudgetAmount = new NumericUpDown();
            lblActual = new Label();
            txtActual = new TextBox();
            lblRemaining = new Label();
            txtRemaining = new TextBox();
            lblEnding = new Label();
            txtEnding = new TextBox();
            lblRecommended = new Label();
            txtRecommended = new TextBox();
            pnlButtons = new Panel();
            btnSave = new Button();
            btnClosePeriod = new Button();
            btnRollForward = new Button();
            btnClose = new Button();
            ((System.ComponentModel.ISupportInitialize)numStarting).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numBudgetAmount).BeginInit();
            pnlButtons.SuspendLayout();
            SuspendLayout();
            lblPeriodCaption.AutoSize = true;
            lblPeriodCaption.Location = new Point(20, 20);
            lblPeriodCaption.Text = "Period:";
            lblPeriod.AutoSize = true;
            lblPeriod.Location = new Point(150, 20);
            lblPeriod.Name = "lblPeriod";
            lblStarting.AutoSize = true;
            lblStarting.Location = new Point(20, 56);
            lblStarting.Text = "Starting Balance:";
            numStarting.DecimalPlaces = 2;
            numStarting.Location = new Point(150, 52);
            numStarting.Maximum = new decimal(new int[] { 100000000, 0, 0, 0 });
            numStarting.Minimum = new decimal(new int[] { 100000000, 0, 0, -2147483648 });
            numStarting.Name = "numStarting";
            numStarting.Size = new Size(140, 23);
            btnResetStarting.Location = new Point(296, 50);
            btnResetStarting.Name = "btnResetStarting";
            btnResetStarting.Size = new Size(90, 26);
            btnResetStarting.Text = "Reset to 0";
            btnResetStarting.Click += OnResetStarting;
            btnTransfer.Location = new Point(392, 50);
            btnTransfer.Name = "btnTransfer";
            btnTransfer.Size = new Size(90, 26);
            btnTransfer.Text = "Transfer...";
            btnTransfer.Click += OnTransfer;
            lblBudgetAmount.AutoSize = true;
            lblBudgetAmount.Location = new Point(20, 92);
            lblBudgetAmount.Text = "Budget Amount:";
            numBudgetAmount.DecimalPlaces = 2;
            numBudgetAmount.Location = new Point(150, 88);
            numBudgetAmount.Maximum = new decimal(new int[] { 100000000, 0, 0, 0 });
            numBudgetAmount.Minimum = new decimal(new int[] { 100000000, 0, 0, -2147483648 });
            numBudgetAmount.Name = "numBudgetAmount";
            numBudgetAmount.Size = new Size(220, 23);
            lblActual.AutoSize = true;
            lblActual.Location = new Point(20, 128);
            lblActual.Text = "Actual Expenses:";
            txtActual.Location = new Point(150, 124);
            txtActual.Name = "txtActual";
            txtActual.ReadOnly = true;
            txtActual.Size = new Size(220, 23);
            lblRemaining.AutoSize = true;
            lblRemaining.Location = new Point(20, 164);
            lblRemaining.Text = "Remaining:";
            txtRemaining.Location = new Point(150, 160);
            txtRemaining.Name = "txtRemaining";
            txtRemaining.ReadOnly = true;
            txtRemaining.Size = new Size(220, 23);
            lblEnding.AutoSize = true;
            lblEnding.Location = new Point(20, 200);
            lblEnding.Text = "Ending Balance:";
            txtEnding.Location = new Point(150, 196);
            txtEnding.Name = "txtEnding";
            txtEnding.ReadOnly = true;
            txtEnding.Size = new Size(220, 23);
            lblRecommended.AutoSize = true;
            lblRecommended.Location = new Point(20, 236);
            lblRecommended.Text = "Recommended:";
            txtRecommended.Location = new Point(150, 232);
            txtRecommended.Name = "txtRecommended";
            txtRecommended.ReadOnly = true;
            txtRecommended.Size = new Size(220, 23);
            pnlButtons.Controls.Add(btnSave);
            pnlButtons.Controls.Add(btnClosePeriod);
            pnlButtons.Controls.Add(btnRollForward);
            pnlButtons.Controls.Add(btnClose);
            pnlButtons.Dock = DockStyle.Bottom;
            pnlButtons.Height = 60;
            btnSave.Location = new Point(12, 12);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(90, 36);
            btnSave.Text = "Override";
            btnSave.Click += OnSave;
            btnClosePeriod.Location = new Point(108, 12);
            btnClosePeriod.Name = "btnClosePeriod";
            btnClosePeriod.Size = new Size(110, 36);
            btnClosePeriod.Text = "Close Period";
            btnClosePeriod.Click += OnClosePeriod;
            btnRollForward.Location = new Point(224, 12);
            btnRollForward.Name = "btnRollForward";
            btnRollForward.Size = new Size(110, 36);
            btnRollForward.Text = "Roll Forward";
            btnRollForward.Click += OnRollForward;
            btnClose.Location = new Point(400, 12);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(80, 36);
            btnClose.Text = "Close";
            btnClose.Click += OnClose;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnClose;
            ClientSize = new Size(500, 340);
            Controls.Add(lblPeriodCaption);
            Controls.Add(lblPeriod);
            Controls.Add(lblStarting);
            Controls.Add(numStarting);
            Controls.Add(btnResetStarting);
            Controls.Add(btnTransfer);
            Controls.Add(lblBudgetAmount);
            Controls.Add(numBudgetAmount);
            Controls.Add(lblActual);
            Controls.Add(txtActual);
            Controls.Add(lblRemaining);
            Controls.Add(txtRemaining);
            Controls.Add(lblEnding);
            Controls.Add(txtEnding);
            Controls.Add(lblRecommended);
            Controls.Add(txtRecommended);
            Controls.Add(pnlButtons);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "BudgetPeriodEditor";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Budget Period";
            ((System.ComponentModel.ISupportInitialize)numStarting).EndInit();
            ((System.ComponentModel.ISupportInitialize)numBudgetAmount).EndInit();
            pnlButtons.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblPeriodCaption;
        private Label lblPeriod;
        private Label lblStarting;
        private NumericUpDown numStarting;
        private Button btnResetStarting;
        private Button btnTransfer;
        private Label lblBudgetAmount;
        private NumericUpDown numBudgetAmount;
        private Label lblActual;
        private TextBox txtActual;
        private Label lblRemaining;
        private TextBox txtRemaining;
        private Label lblEnding;
        private TextBox txtEnding;
        private Label lblRecommended;
        private TextBox txtRecommended;
        private Panel pnlButtons;
        private Button btnSave;
        private Button btnClosePeriod;
        private Button btnRollForward;
        private Button btnClose;
    }
}
