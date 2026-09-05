namespace THMS.UI.WinForms.Controls
{
    partial class AccountEditForm
    {
        private System.ComponentModel.IContainer components = null;

        // Shared fields
        private Label lblAccountType;
        private ComboBox cmbAccountType;

        private Label lblName;
        private TextBox txtName;

        private Label lblInstitution;
        private TextBox txtInstitution;

        private Label lblAccountNumber;
        private TextBox txtAccountNumber;

        private Label lblUrl;
        private TextBox txtUrl;

        private Label lblBalanceAsOf;
        private DateTimePicker dtBalanceAsOf;

        // Panels
        private Panel pnlBank;
        private Panel pnlCredit;
        private Panel pnlLoan;
        private Panel pnlMortgage;
        private Panel pnlInvestment;
        private Panel pnlInternal;

        // Bank fields
        private Label lblBankPosted;
        private NumericUpDown numBankPosted;

        private Label lblBankOverdraft;
        private NumericUpDown numBankOverdraft;

        // Credit fields
        private Label lblCreditLimit;
        private NumericUpDown numCreditLimit;

        private Label lblCreditApr;
        private NumericUpDown numCreditApr;

        private Label lblCreditStatement;
        private DateTimePicker dtCreditStatement;

        private Label lblCreditDue;
        private DateTimePicker dtCreditDue;

        private Label lblCreditPosted;
        private NumericUpDown numCreditPosted;

        // Loan fields
        private Label lblLoanPrincipal;
        private NumericUpDown numLoanPrincipal;

        private Label lblLoanRate;
        private NumericUpDown numLoanRate;

        private Label lblLoanTerm;
        private NumericUpDown numLoanTerm;

        // Mortgage fields
        private Label lblMortPrincipal;
        private NumericUpDown numMortPrincipal;

        private Label lblMortRate;
        private NumericUpDown numMortRate;

        private Label lblMortTerm;
        private NumericUpDown numMortTerm;

        private Label lblMortNext;
        private DateTimePicker dtMortNext;

        // Investment fields
        private Label lblInvestCash;
        private NumericUpDown numInvestCash;

        // Internal fields
        private Label lblInternalPurpose;
        private TextBox txtInternalPurpose;

        // Buttons
        private Panel pnlButtons;
        private Button btnSave;
        private Button btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            // Shared labels + fields
            lblAccountType = new Label { Text = "Account Type:", Left = 20, Top = 20, Width = 120 };
            cmbAccountType = new ComboBox { Left = 150, Top = 20, Width = 200 };
            cmbAccountType.SelectedIndexChanged += OnAccountTypeChanged;

            lblName = new Label { Text = "Name:", Left = 20, Top = 60, Width = 120 };
            txtName = new TextBox { Left = 150, Top = 60, Width = 250 };

            lblInstitution = new Label { Text = "Institution:", Left = 20, Top = 100, Width = 120 };
            txtInstitution = new TextBox { Left = 150, Top = 100, Width = 250 };

            lblAccountNumber = new Label { Text = "Account Number:", Left = 20, Top = 140, Width = 120 };
            txtAccountNumber = new TextBox { Left = 150, Top = 140, Width = 250 };

            lblUrl = new Label { Text = "Website URL:", Left = 20, Top = 180, Width = 120 };
            txtUrl = new TextBox { Left = 150, Top = 180, Width = 250 };

            lblBalanceAsOf = new Label { Text = "Balance As Of:", Left = 20, Top = 220, Width = 120 };
            dtBalanceAsOf = new DateTimePicker { Left = 150, Top = 220, Width = 200, Format = DateTimePickerFormat.Short };

            // Panels start at 260
            pnlBank = new Panel { Left = 20, Top = 260, Width = 380, Height = 120 };
            pnlCredit = new Panel { Left = 20, Top = 260, Width = 380, Height = 200 };
            pnlLoan = new Panel { Left = 20, Top = 260, Width = 380, Height = 120 };
            pnlMortgage = new Panel { Left = 20, Top = 260, Width = 380, Height = 160 };
            pnlInvestment = new Panel { Left = 20, Top = 260, Width = 380, Height = 80 };
            pnlInternal = new Panel { Left = 20, Top = 260, Width = 380, Height = 80 };

            // Bank panel
            lblBankPosted = new Label { Text = "Posted Balance:", Left = 10, Top = 10, Width = 120 };
            numBankPosted = new NumericUpDown { Left = 150, Top = 10, Width = 150, DecimalPlaces = 2, Maximum = 1000000 };

            lblBankOverdraft = new Label { Text = "Overdraft Limit:", Left = 10, Top = 50, Width = 120 };
            numBankOverdraft = new NumericUpDown { Left = 150, Top = 50, Width = 150, DecimalPlaces = 2, Maximum = 1000000 };

            pnlBank.Controls.AddRange(new Control[] {
                lblBankPosted, numBankPosted,
                lblBankOverdraft, numBankOverdraft
            });

            // Credit panel
            lblCreditLimit = new Label { Text = "Credit Limit:", Left = 10, Top = 10, Width = 120 };
            numCreditLimit = new NumericUpDown { Left = 150, Top = 10, Width = 150, DecimalPlaces = 2, Maximum = 1000000 };

            lblCreditApr = new Label { Text = "APR (%):", Left = 10, Top = 50, Width = 120 };
            numCreditApr = new NumericUpDown { Left = 150, Top = 50, Width = 150, DecimalPlaces = 2, Maximum = 100 };

            lblCreditStatement = new Label { Text = "Statement Date:", Left = 10, Top = 90, Width = 120 };
            dtCreditStatement = new DateTimePicker { Left = 150, Top = 90, Width = 150, Format = DateTimePickerFormat.Short };

            lblCreditDue = new Label { Text = "Due Date:", Left = 10, Top = 130, Width = 120 };
            dtCreditDue = new DateTimePicker { Left = 150, Top = 130, Width = 150, Format = DateTimePickerFormat.Short };

            lblCreditPosted = new Label { Text = "Posted Balance:", Left = 10, Top = 170, Width = 120 };
            numCreditPosted = new NumericUpDown { Left = 150, Top = 170, Width = 150, DecimalPlaces = 2, Maximum = 1000000 };

            pnlCredit.Controls.AddRange(new Control[] {
                lblCreditLimit, numCreditLimit,
                lblCreditApr, numCreditApr,
                lblCreditStatement, dtCreditStatement,
                lblCreditDue, dtCreditDue,
                lblCreditPosted, numCreditPosted
            });

            // Loan panel
            lblLoanPrincipal = new Label { Text = "Principal:", Left = 10, Top = 10, Width = 120 };
            numLoanPrincipal = new NumericUpDown { Left = 150, Top = 10, Width = 150, DecimalPlaces = 2, Maximum = 1000000 };

            lblLoanRate = new Label { Text = "Interest Rate (%):", Left = 10, Top = 50, Width = 120 };
            numLoanRate = new NumericUpDown { Left = 150, Top = 50, Width = 150, DecimalPlaces = 2, Maximum = 100 };

            lblLoanTerm = new Label { Text = "Term (months):", Left = 10, Top = 90, Width = 120 };
            numLoanTerm = new NumericUpDown { Left = 150, Top = 90, Width = 150, Maximum = 480 };

            pnlLoan.Controls.AddRange(new Control[] {
                lblLoanPrincipal, numLoanPrincipal,
                lblLoanRate, numLoanRate,
                lblLoanTerm, numLoanTerm
            });

            // Mortgage panel
            lblMortPrincipal = new Label { Text = "Principal:", Left = 10, Top = 10, Width = 120 };
            numMortPrincipal = new NumericUpDown { Left = 150, Top = 10, Width = 150, DecimalPlaces = 2, Maximum = 1000000 };

            lblMortRate = new Label { Text = "Interest Rate (%):", Left = 10, Top = 50, Width = 120 };
            numMortRate = new NumericUpDown { Left = 150, Top = 50, Width = 150, DecimalPlaces = 2, Maximum = 100 };

            lblMortTerm = new Label { Text = "Term (months):", Left = 10, Top = 90, Width = 120 };
            numMortTerm = new NumericUpDown { Left = 150, Top = 90, Width = 150, Maximum = 480 };

            lblMortNext = new Label { Text = "Next Payment:", Left = 10, Top = 130, Width = 120 };
            dtMortNext = new DateTimePicker { Left = 150, Top = 130, Width = 150, Format = DateTimePickerFormat.Short };

            pnlMortgage.Controls.AddRange(new Control[] {
                lblMortPrincipal, numMortPrincipal,
                lblMortRate, numMortRate,
                lblMortTerm, numMortTerm,
                lblMortNext, dtMortNext
            });

            // Investment panel
            lblInvestCash = new Label { Text = "Cash Balance:", Left = 10, Top = 10, Width = 120 };
            numInvestCash = new NumericUpDown { Left = 150, Top = 10, Width = 150, DecimalPlaces = 2, Maximum = 1000000 };

            pnlInvestment.Controls.AddRange(new Control[] {
                lblInvestCash, numInvestCash
            });

            // Internal panel
            lblInternalPurpose = new Label { Text = "Purpose:", Left = 10, Top = 10, Width = 120 };
            txtInternalPurpose = new TextBox { Left = 150, Top = 10, Width = 200 };

            pnlInternal.Controls.AddRange(new Control[] {
                lblInternalPurpose, txtInternalPurpose
            });

            // Save / Cancel sit in a bottom-docked bar so they stay fully visible
            btnSave = new Button { Text = "Save", Left = 20, Top = 12, Width = 120, Height = 40 };
            btnSave.Click += OnSave;

            btnCancel = new Button { Text = "Cancel", Left = 160, Top = 12, Width = 120, Height = 40 };
            btnCancel.Click += OnCancel;

            pnlButtons = new Panel { Dock = DockStyle.Bottom, Height = 64 };
            pnlButtons.Controls.Add(btnSave);
            pnlButtons.Controls.Add(btnCancel);

            // Add everything to form
            Controls.AddRange(new Control[] {
                lblAccountType, cmbAccountType,
                lblName, txtName,
                lblInstitution, txtInstitution,
                lblAccountNumber, txtAccountNumber,
                lblUrl, txtUrl,
                lblBalanceAsOf, dtBalanceAsOf,

                pnlBank, pnlCredit, pnlLoan, pnlMortgage, pnlInvestment, pnlInternal,

                pnlButtons
            });

            Text = "Account Editor";
            Width = 450;
            Height = 520;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
        }
    }
}
