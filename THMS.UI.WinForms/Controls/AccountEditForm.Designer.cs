namespace THMS.UI.WinForms.Controls
{
    partial class AccountEditForm
    {
        private System.ComponentModel.IContainer components = null;

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

        private Panel pnlBank;
        private Panel pnlCredit;
        private Panel pnlLoan;
        private Panel pnlMortgage;
        private Panel pnlInvestment;
        private Panel pnlInternal;
        private Panel pnlUntracked;

        private Label lblBankOverdraft;
        private NumericUpDown numBankOverdraft;
        private Label lblBankStarting;
        private NumericUpDown numBankStarting;
        private Label lblBankOpeningAsOf;
        private DateTimePicker dtBankOpeningAsOf;
        private Label lblBankHint;

        private Label lblCreditLimit;
        private NumericUpDown numCreditLimit;

        private Label lblCreditApr;
        private NumericUpDown numCreditApr;

        private Label lblCreditStatement;
        private DateTimePicker dtCreditStatement;

        private Label lblCreditDue;
        private DateTimePicker dtCreditDue;

        private Label lblCreditStarting;
        private NumericUpDown numCreditStarting;
        private Label lblCreditOpeningAsOf;
        private DateTimePicker dtCreditOpeningAsOf;
        private Label lblCreditHint;

        private Label lblLoanPrincipal;
        private NumericUpDown numLoanPrincipal;

        private Label lblLoanRate;
        private NumericUpDown numLoanRate;

        private Label lblLoanTerm;
        private NumericUpDown numLoanTerm;

        private Label lblMortPrincipal;
        private NumericUpDown numMortPrincipal;

        private Label lblMortRate;
        private NumericUpDown numMortRate;

        private Label lblMortTerm;
        private NumericUpDown numMortTerm;

        private Label lblMortNext;
        private DateTimePicker dtMortNext;

        private Label lblInvestCash;
        private NumericUpDown numInvestCash;

        private Label lblInternalPurpose;
        private TextBox txtInternalPurpose;
        private Label lblUntrackedHint;

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

            const int labelWidth = 160;
            const int fieldLeft = 180;
            const int fieldWidth = 220;

            lblAccountType = FieldLabel("Account Type:", 20, 20, labelWidth);
            cmbAccountType = new ComboBox
            {
                Left = fieldLeft,
                Top = 20,
                Width = fieldWidth,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbAccountType.SelectedIndexChanged += OnAccountTypeChanged;

            lblName = FieldLabel("Name:", 20, 56, labelWidth);
            txtName = new TextBox { Left = fieldLeft, Top = 56, Width = fieldWidth };

            lblInstitution = FieldLabel("Institution:", 20, 92, labelWidth);
            txtInstitution = new TextBox { Left = fieldLeft, Top = 92, Width = fieldWidth };

            lblAccountNumber = FieldLabel("Account Number:", 20, 128, labelWidth);
            txtAccountNumber = new TextBox { Left = fieldLeft, Top = 128, Width = fieldWidth };

            lblUrl = FieldLabel("Website URL:", 20, 164, labelWidth);
            txtUrl = new TextBox { Left = fieldLeft, Top = 164, Width = fieldWidth };

            pnlBank = new Panel { Left = 20, Top = 204, Width = 400, Height = 220 };
            pnlCredit = new Panel { Left = 20, Top = 204, Width = 400, Height = 280 };
            pnlLoan = new Panel { Left = 20, Top = 204, Width = 400, Height = 120 };
            pnlMortgage = new Panel { Left = 20, Top = 204, Width = 400, Height = 160 };
            pnlInvestment = new Panel { Left = 20, Top = 204, Width = 400, Height = 80 };
            pnlInternal = new Panel { Left = 20, Top = 204, Width = 400, Height = 80 };
            pnlUntracked = new Panel { Left = 20, Top = 204, Width = 400, Height = 96 };

            const int panelFieldLeft = 160;

            lblBankOverdraft = FieldLabel("Overdraft Limit:", 10, 8, 150);
            numBankOverdraft = MoneyBox(panelFieldLeft, 8);
            numBankOverdraft.Minimum = 0;
            numBankOverdraft.Maximum = 1000000;

            lblBankStarting = FieldLabel("Opening balance:", 10, 44, 150);
            numBankStarting = MoneyBox(panelFieldLeft, 44);

            lblBankOpeningAsOf = FieldLabel("Opening as of:", 10, 80, 150);
            dtBankOpeningAsOf = new DateTimePicker
            {
                Left = panelFieldLeft,
                Top = 80,
                Width = 150,
                Format = DateTimePickerFormat.Short
            };

            lblBankHint = HintLabel(
                "Opening balance is the ledger seed before any THMS transactions. " +
                "Posted balance is calculated on the ledger (opening plus activity). " +
                "Do not enter statement period totals here. Overdraft is your bank's protection limit.",
                116,
                380,
                88);

            pnlBank.Controls.AddRange(new Control[] {
                lblBankOverdraft, numBankOverdraft,
                lblBankStarting, numBankStarting,
                lblBankOpeningAsOf, dtBankOpeningAsOf,
                lblBankHint
            });

            lblCreditLimit = FieldLabel("Credit Limit:", 10, 8, 150);
            numCreditLimit = MoneyBox(panelFieldLeft, 8);
            numCreditLimit.Minimum = 0;
            numCreditLimit.Maximum = 1000000;

            lblCreditApr = FieldLabel("APR (%):", 10, 44, 150);
            numCreditApr = MoneyBox(panelFieldLeft, 44);
            numCreditApr.Minimum = 0;
            numCreditApr.Maximum = 100;

            lblCreditStatement = FieldLabel("Statement Date:", 10, 80, 150);
            dtCreditStatement = new DateTimePicker
            {
                Left = panelFieldLeft,
                Top = 80,
                Width = 150,
                Format = DateTimePickerFormat.Short
            };

            lblCreditDue = FieldLabel("Due Date:", 10, 116, 150);
            dtCreditDue = new DateTimePicker
            {
                Left = panelFieldLeft,
                Top = 116,
                Width = 150,
                Format = DateTimePickerFormat.Short
            };

            lblCreditStarting = FieldLabel("Opening (owed):", 10, 152, 150);
            numCreditStarting = MoneyBox(panelFieldLeft, 152);

            lblCreditOpeningAsOf = FieldLabel("Opening as of:", 10, 188, 150);
            dtCreditOpeningAsOf = new DateTimePicker
            {
                Left = panelFieldLeft,
                Top = 188,
                Width = 150,
                Format = DateTimePickerFormat.Short
            };

            lblCreditHint = HintLabel(
                "Opening is the ledger seed before THMS transactions. Posted (owed) is calculated on the ledger. " +
                "A specific statement's balance and due amount go in the Statement Editor.",
                224,
                380,
                64);

            pnlCredit.Controls.AddRange(new Control[] {
                lblCreditLimit, numCreditLimit,
                lblCreditApr, numCreditApr,
                lblCreditStatement, dtCreditStatement,
                lblCreditDue, dtCreditDue,
                lblCreditStarting, numCreditStarting,
                lblCreditOpeningAsOf, dtCreditOpeningAsOf,
                lblCreditHint
            });

            lblLoanPrincipal = FieldLabel("Principal:", 10, 8, 150);
            numLoanPrincipal = MoneyBox(panelFieldLeft, 8);
            numLoanPrincipal.Minimum = 0;
            numLoanPrincipal.Maximum = 1000000;

            lblLoanRate = FieldLabel("Interest Rate (%):", 10, 44, 150);
            numLoanRate = MoneyBox(panelFieldLeft, 44);
            numLoanRate.Minimum = 0;
            numLoanRate.Maximum = 100;

            lblLoanTerm = FieldLabel("Term (months):", 10, 80, 150);
            numLoanTerm = new NumericUpDown
            {
                Left = panelFieldLeft,
                Top = 80,
                Width = 150,
                Maximum = 480
            };

            pnlLoan.Controls.AddRange(new Control[] {
                lblLoanPrincipal, numLoanPrincipal,
                lblLoanRate, numLoanRate,
                lblLoanTerm, numLoanTerm
            });

            lblMortPrincipal = FieldLabel("Principal:", 10, 8, 150);
            numMortPrincipal = MoneyBox(panelFieldLeft, 8);
            numMortPrincipal.Minimum = 0;
            numMortPrincipal.Maximum = 1000000;

            lblMortRate = FieldLabel("Interest Rate (%):", 10, 44, 150);
            numMortRate = MoneyBox(panelFieldLeft, 44);
            numMortRate.Minimum = 0;
            numMortRate.Maximum = 100;

            lblMortTerm = FieldLabel("Term (months):", 10, 80, 150);
            numMortTerm = new NumericUpDown
            {
                Left = panelFieldLeft,
                Top = 80,
                Width = 150,
                Maximum = 480
            };

            lblMortNext = FieldLabel("Next Payment:", 10, 116, 150);
            dtMortNext = new DateTimePicker
            {
                Left = panelFieldLeft,
                Top = 116,
                Width = 150,
                Format = DateTimePickerFormat.Short
            };

            pnlMortgage.Controls.AddRange(new Control[] {
                lblMortPrincipal, numMortPrincipal,
                lblMortRate, numMortRate,
                lblMortTerm, numMortTerm,
                lblMortNext, dtMortNext
            });

            lblInvestCash = FieldLabel("Cash Balance:", 10, 8, 150);
            numInvestCash = MoneyBox(panelFieldLeft, 8);
            numInvestCash.Minimum = 0;
            numInvestCash.Maximum = 1000000;

            pnlInvestment.Controls.AddRange(new Control[] {
                lblInvestCash, numInvestCash
            });

            lblInternalPurpose = FieldLabel("Purpose:", 10, 8, 150);
            txtInternalPurpose = new TextBox { Left = panelFieldLeft, Top = 8, Width = 200 };

            pnlInternal.Controls.AddRange(new Control[] {
                lblInternalPurpose, txtInternalPurpose
            });

            lblUntrackedHint = HintLabel(
                "This is a biller without a tracked ledger. Add statements on the Planning Center. " +
                "Payments come from a bank account.",
                8, 380, 72);
            pnlUntracked.Controls.Add(lblUntrackedHint);

            btnSave = new Button { Text = "Save", Left = 20, Top = 12, Width = 120, Height = 40 };
            btnSave.Click += OnSave;

            btnCancel = new Button { Text = "Cancel", Left = 160, Top = 12, Width = 120, Height = 40 };
            btnCancel.Click += OnCancel;

            pnlButtons = new Panel { Dock = DockStyle.Bottom, Height = 64 };
            pnlButtons.Controls.Add(btnSave);
            pnlButtons.Controls.Add(btnCancel);

            Controls.AddRange(new Control[] {
                lblAccountType, cmbAccountType,
                lblName, txtName,
                lblInstitution, txtInstitution,
                lblAccountNumber, txtAccountNumber,
                lblUrl, txtUrl,
                pnlBank, pnlCredit, pnlLoan, pnlMortgage, pnlInvestment, pnlInternal, pnlUntracked,
                pnlButtons
            });

            Text = "Account Editor";
            Width = 470;
            Height = 560;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
        }

        private static Label FieldLabel(string text, int left, int top, int width) => new()
        {
            Text = text,
            Left = left,
            Top = top,
            Width = width,
            Height = 28,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft
        };

        private static Label HintLabel(string text, int top, int width, int height) => new()
        {
            Text = text,
            Left = 10,
            Top = top,
            Width = width,
            Height = height,
            AutoSize = false
        };

        private static NumericUpDown MoneyBox(int left, int top) => new()
        {
            Left = left,
            Top = top,
            Width = 150,
            DecimalPlaces = 2,
            Minimum = -100000000,
            Maximum = 100000000
        };
    }
}
