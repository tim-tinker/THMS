using System;
using System.Windows.Forms;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Investments;
using THMS.Logic.Finance.Model;
using THMS.Logic.Orchestrators.Finance;
using THMS.Logic.ViewModels.Finance;

namespace THMS.UI.WinForms.Controls
{
    public partial class AccountEditForm : Form
    {
        private readonly List<Account> _accounts;
        private readonly Account _opened;
        private readonly bool _persisted;

        public Account Account { get; private set; }

        public bool DiscardActivity { get; private set; }

        private readonly ToolTip _leaseTips;

        private const string ChargeTip = "Enter a negative number. This value is a charge to you.";

        public AccountEditForm(Account? existing, IEnumerable<Account>? accounts = null, bool persisted = false)
        {
            InitializeComponent();
            _leaseTips = new ToolTip(components)
            {
                AutoPopDelay = 12000,
                InitialDelay = 400,
                ReshowDelay = 200,
                ShowAlways = true
            };

            _accounts = accounts?.ToList() ?? [];
            _persisted = persisted;
            _opened = existing ?? new BankAccount { Type = AccountType.Checking };
            Account = _opened;

            PopulateAccountTypeCombo();
            chkAutoPay.CheckedChanged += (_, _) => UpdateAutoPayEnabled();
            BindFields();
            ShowCorrectPanel();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ShowCorrectPanel();
        }

        private void PopulateAccountTypeCombo()
        {
            cmbAccountType.SelectedIndexChanged -= OnAccountTypeChanged;
            cmbAccountType.Items.Clear();
            cmbAccountType.Items.Add("Bank");
            cmbAccountType.Items.Add("Credit");
            cmbAccountType.Items.Add("Loan");
            cmbAccountType.Items.Add("Lease");
            cmbAccountType.Items.Add("Mortgage");
            cmbAccountType.Items.Add("Investment");
            cmbAccountType.Items.Add("Internal");
            cmbAccountType.Items.Add("Utility");
            cmbAccountType.Items.Add("Service");
            cmbAccountType.Items.Add("Insurance");
            cmbAccountType.Items.Add("Tax");
            cmbAccountType.SelectedItem = ComboLabel(Account);
            cmbAccountType.SelectedIndexChanged += OnAccountTypeChanged;
        }

        private static string ComboLabel(Account account) => AccountKinds.Of(account) switch
        {
            AccountKinds.Bank => "Bank",
            AccountKinds.Credit => "Credit",
            AccountKinds.Loan => "Loan",
            AccountKinds.Lease => "Lease",
            AccountKinds.Mortgage => "Mortgage",
            AccountKinds.Investment => "Investment",
            AccountKinds.Internal => "Internal",
            AccountKinds.Utility => "Utility",
            AccountKinds.Service => "Service",
            AccountKinds.Insurance => "Insurance",
            AccountKinds.Tax => "Tax",
            _ => "Bank"
        };

        private void BindFields()
        {
            txtName.Text = Account.Name;
            txtInstitution.Text = Account.Institution;
            txtAccountNumber.Text = Account.AccountNumber;
            txtUrl.Text = Account.WebsiteUrl;
            BindFundingAccounts();
            chkAutoPay.Checked = Account.AutoPay;

            var openingAsOf = PickerDate(dtBankOpeningAsOf, Account.BalanceAsOf);
            dtBankOpeningAsOf.Value = openingAsOf;
            dtCreditOpeningAsOf.Value = PickerDate(dtCreditOpeningAsOf, Account.BalanceAsOf);

            // Subtype-specific binding
            switch (Account)
            {
                case BankAccount bank:
                    numBankStarting.Value = bank.StartingBalance;
                    numBankOverdraft.Value = bank.OverdraftLimit;
                    break;

                case CreditAccount credit:
                    numCreditLimit.Value = credit.CreditLimit;
                    numCreditApr.Value = credit.APR;
                    dtCreditStatement.Value = PickerDate(dtCreditStatement, credit.StatementDate);
                    dtCreditDue.Value = PickerDate(dtCreditDue, credit.DueDate);
                    numCreditStarting.Value = Clamp(numCreditStarting, PostedBalanceCalculator.ToDisplayBalance(credit, credit.StartingBalance));
                    break;

                case LeaseAccount lease:
                    _leaseMath = true;
                    try
                    {
                        ApplyRateEditor(moneyFactor: true);
                        numLoanPrincipal.Value = Clamp(numLoanPrincipal, PostedBalanceCalculator.ToDisplayBalance(lease, lease.Principal));
                        numLoanTerm.Value = lease.TermMonths;
                        numLeaseGross.Value = Clamp(numLeaseGross, Charge(lease.GrossCapitalizedCost));
                        numLeaseReduction.Value = Clamp(numLeaseReduction, lease.CapitalizedCostReduction);
                        numLeaseAdjusted.Value = Clamp(numLeaseAdjusted, Charge(lease.AdjustedCapitalizedCost));
                        numLeaseResidual.Value = Clamp(numLeaseResidual, Charge(lease.Residual));
                        numLeaseRent.Value = Clamp(numLeaseRent, Charge(lease.RentCharge));
                        numLeaseAmount.Value = Clamp(numLeaseAmount, Charge(lease.LeaseAmount));
                        numLoanRate.Value = Clamp(numLoanRate, lease.MoneyFactor);
                        if (numLeaseAdjusted.Value <= 0)
                            FillAdjustedCapCost();
                    }
                    finally
                    {
                        _leaseMath = false;
                    }
                    ApplyRentChargeFactor();
                    break;

                case LoanAccount loan:
                    ApplyRateEditor(moneyFactor: false);
                    numLoanPrincipal.Value = Clamp(numLoanPrincipal, PostedBalanceCalculator.ToDisplayBalance(loan, loan.Principal));
                    numLoanRate.Value = Clamp(numLoanRate, loan.InterestRate);
                    numLoanTerm.Value = loan.TermMonths;
                    break;

                case MortgageAccount mortgage:
                    numMortPrincipal.Value = Clamp(numMortPrincipal, PostedBalanceCalculator.ToDisplayBalance(mortgage, mortgage.Principal));
                    numMortRate.Value = mortgage.InterestRate;
                    numMortTerm.Value = mortgage.TermMonths;
                    dtMortNext.Value = PickerDate(dtMortNext, mortgage.NextPaymentDate);
                    break;

                case InvestmentAccount invest:
                    numInvestCash.Enabled = !invest.TracksPositions;
                    chkInvestImportOnly.Checked = invest.QuoteAccess == QuoteAccess.ImportOnly;
                    if (invest.CashBalance >= numInvestCash.Minimum && invest.CashBalance <= numInvestCash.Maximum)
                        numInvestCash.Value = invest.CashBalance;
                    break;

                case InternalAccount internalAcct:
                    txtInternalPurpose.Text = internalAcct.Purpose;
                    break;
            }
        }

        private void SaveFields()
        {
            Account.Name = txtName.Text;
            Account.Institution = txtInstitution.Text;
            Account.AccountNumber = txtAccountNumber.Text;
            Account.WebsiteUrl = txtUrl.Text;
            SaveAutoPay();

            // Subtype-specific save
            switch (Account)
            {
                case BankAccount bank:
                    ShiftStartingBalance(bank, numBankStarting.Value);
                    bank.OverdraftLimit = numBankOverdraft.Value;
                    bank.BalanceAsOf = dtBankOpeningAsOf.Value;
                    break;

                case CreditAccount credit:
                    credit.CreditLimit = numCreditLimit.Value;
                    credit.APR = numCreditApr.Value;
                    credit.StatementDate = dtCreditStatement.Value;
                    credit.DueDate = dtCreditDue.Value;
                    var openingOwed = PostedBalanceCalculator.ToLedgerBalance(credit, numCreditStarting.Value);
                    ShiftStartingBalance(credit, openingOwed);
                    credit.BalanceAsOf = dtCreditOpeningAsOf.Value;
                    break;

                case LeaseAccount lease:
                    lease.Principal = PostedBalanceCalculator.ToLedgerBalance(lease, numLoanPrincipal.Value);
                    lease.TermMonths = (int)numLoanTerm.Value;
                    lease.GrossCapitalizedCost = Magnitude(numLeaseGross.Value);
                    lease.CapitalizedCostReduction = numLeaseReduction.Value;
                    lease.AdjustedCapitalizedCost = Magnitude(numLeaseAdjusted.Value);
                    lease.Residual = Magnitude(numLeaseResidual.Value);
                    lease.RentCharge = Magnitude(numLeaseRent.Value);
                    lease.LeaseAmount = Magnitude(numLeaseAmount.Value);
                    lease.MoneyFactor = LeaseAccount.MoneyFactorFromRentCharge(
                        lease.RentCharge,
                        lease.AdjustedCapitalizedCost,
                        lease.Residual,
                        lease.TermMonths) ?? numLoanRate.Value;
                    lease.InterestRate = 0;
                    break;

                case LoanAccount loan:
                    loan.Principal = PostedBalanceCalculator.ToLedgerBalance(loan, numLoanPrincipal.Value);
                    loan.InterestRate = numLoanRate.Value;
                    loan.TermMonths = (int)numLoanTerm.Value;
                    break;

                case MortgageAccount mortgage:
                    mortgage.Principal = PostedBalanceCalculator.ToLedgerBalance(mortgage, numMortPrincipal.Value);
                    mortgage.InterestRate = numMortRate.Value;
                    mortgage.TermMonths = (int)numMortTerm.Value;
                    mortgage.NextPaymentDate = dtMortNext.Value;
                    break;

                case InvestmentAccount invest:
                    invest.QuoteAccess = chkInvestImportOnly.Checked ? QuoteAccess.ImportOnly : QuoteAccess.Service;
                    if (!invest.TracksPositions)
                        invest.CashBalance = numInvestCash.Value;
                    break;

                case InternalAccount internalAcct:
                    internalAcct.Purpose = txtInternalPurpose.Text;
                    break;
            }
        }

        private void HideAllPanels()
        {
            pnlBank.Visible = false;
            pnlCredit.Visible = false;
            pnlLoan.Visible = false;
            pnlMortgage.Visible = false;
            pnlInvestment.Visible = false;
            pnlInternal.Visible = false;
            pnlUntracked.Visible = false;
        }

        private void ShowCorrectPanel()
        {
            HideAllPanels();

            Panel? panel = Account switch
            {
                BankAccount => pnlBank,
                CreditAccount => pnlCredit,
                LoanAccount => pnlLoan,
                MortgageAccount => pnlMortgage,
                InvestmentAccount => pnlInvestment,
                InternalAccount => pnlInternal,
                UntrackedAccount => pnlUntracked,
                _ => null
            };

            if (panel is null)
                return;

            panel.Visible = true;
            if (panel == pnlLoan)
                ShowLeaseFields();
            LayoutAutoPay(panel);

            // Sizing before the handle exists (and AutoScale runs) clips the button bar.
            if (IsHandleCreated)
                ResizeFormForPanel(panel);
        }

        private void ShowLeaseFields()
        {
            var lease = Account is LeaseAccount;
            lblLoanPrincipal.Text = lease ? "Balance:" : "Principal:";
            lblLeaseGross.Visible = lease;
            numLeaseGross.Visible = lease;
            lblLeaseReduction.Visible = lease;
            numLeaseReduction.Visible = lease;
            lblLeaseAdjusted.Visible = lease;
            numLeaseAdjusted.Visible = lease;
            lblLeaseResidual.Visible = lease;
            numLeaseResidual.Visible = lease;
            lblLeaseRent.Visible = lease;
            numLeaseRent.Visible = lease;
            lblLeaseAmount.Visible = lease;
            numLeaseAmount.Visible = lease;
            if (lease)
                ArrangeLeaseFields();
            else
                ArrangeLoanFields();
            ApplyRateEditor(lease);
            if (lease)
                LockCalculated(numLoanRate);
            LockCalculated(numLeaseAdjusted);
            var bottom = 0;
            foreach (Control child in pnlLoan.Controls)
            {
                if (child.Visible)
                    bottom = Math.Max(bottom, child.Bottom);
            }
            pnlLoan.Height = bottom + LogicalToDeviceUnits(8);
        }

        private void ArrangeLoanFields()
        {
            btnLeaseHelp.Visible = false;
            ArrangeColumn(
            [
                (lblLoanPrincipal, numLoanPrincipal),
                (lblLoanRate, numLoanRate),
                (lblLoanTerm, numLoanTerm)
            ]);
            UnlockEntry(numLoanRate);
            SetChargeTip(lblLoanPrincipal, numLoanPrincipal, false);
        }

        private void ArrangeLeaseFields()
        {
            ArrangeColumn(
            [
                (lblLoanTerm, numLoanTerm),
                (lblLeaseGross, numLeaseGross),
                (lblLeaseReduction, numLeaseReduction),
                (lblLeaseAdjusted, numLeaseAdjusted),
                (lblLeaseResidual, numLeaseResidual),
                (lblLeaseRent, numLeaseRent),
                (lblLoanRate, numLoanRate),
                (lblLeaseAmount, numLeaseAmount),
                (lblLoanPrincipal, numLoanPrincipal)
            ]);
            var size = LogicalToDeviceUnits(28);
            btnLeaseHelp.SetBounds(
                numLoanPrincipal.Right - size,
                numLoanPrincipal.Bottom + LogicalToDeviceUnits(8),
                size,
                size);
            btnLeaseHelp.Visible = true;
            _leaseTips.SetToolTip(btnLeaseHelp, "How these lease amounts work");
            SetChargeTip(lblLeaseGross, numLeaseGross, true);
            SetChargeTip(lblLeaseResidual, numLeaseResidual, true);
            SetChargeTip(lblLeaseRent, numLeaseRent, true);
            SetChargeTip(lblLeaseAmount, numLeaseAmount, true);
            SetChargeTip(lblLoanPrincipal, numLoanPrincipal, true);
        }

        private void ArrangeColumn((Label Label, Control Field)[] rows)
        {
            var gap = LogicalToDeviceUnits(12);
            var textWidth = 0;
            foreach (var (label, _) in rows)
            {
                label.Left = 0;
                textWidth = Math.Max(textWidth, TextRenderer.MeasureText(label.Text, label.Font).Width);
            }

            var boxLeft = Math.Max(txtName.Left - pnlLoan.Left, textWidth + gap);
            var boxRight = txtName.Right - pnlLoan.Left;
            if (boxRight < boxLeft + LogicalToDeviceUnits(140))
                boxRight = boxLeft + txtName.Width;
            var boxWidth = boxRight - boxLeft;
            if (pnlLoan.Width < boxRight + LogicalToDeviceUnits(4))
                pnlLoan.Width = boxRight + LogicalToDeviceUnits(4);

            var top = LogicalToDeviceUnits(8);
            var step = LogicalToDeviceUnits(36);
            for (var i = 0; i < rows.Length; i++)
            {
                var (label, field) = rows[i];
                label.Top = top;
                label.Width = Math.Max(textWidth, boxLeft - gap);
                field.SetBounds(boxLeft, top, boxWidth, field.Height);
                field.TabIndex = i;
                if (field is NumericUpDown number)
                    number.TextAlign = HorizontalAlignment.Right;
                top += step;
            }
        }

        private void OnLeaseHelp(object? sender, EventArgs e)
        {
            MessageBox.Show(
                this,
                LeaseHelpText(),
                "Lease amounts",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        internal string LeaseHelpText()
        {
            var rate = numLoanRate.Value > 0
                ? $" About {numLoanRate.Value * LeaseAccount.AprMultiplier:0.###}% APR."
                : "";
            var check = "";
            var adjusted = Magnitude(numLeaseAdjusted.Value);
            var residual = Magnitude(numLeaseResidual.Value);
            var rent = Magnitude(numLeaseRent.Value);
            if (adjusted > residual && rent > 0)
            {
                var depreciation = adjusted - residual;
                var basePayments = depreciation + rent;
                check = $" Depreciation {depreciation:c2} plus the rent charge is {basePayments:c2}.";
            }

            return
                "Gross cap cost, residual, rent charge, lease amount, and balance are charges, so enter them as negative numbers. Cap reduction stays positive." +
                Environment.NewLine + Environment.NewLine +
                "Adjusted cap cost is calculated from the gross cap cost and the cap reduction." +
                Environment.NewLine + Environment.NewLine +
                "Money factor = rent charge ÷ (term × (adjusted cap cost + residual)), using the positive amounts." +
                rate + check +
                Environment.NewLine + Environment.NewLine +
                "Lease amount is the total of the payments and is not part of net worth. Residual is the buyout. Keeping the vehicle is its value, minus the remaining payments, minus the residual.";
        }

        private void SetChargeTip(Control label, Control field, bool charge)
        {
            var text = charge ? ChargeTip : "";
            _leaseTips.SetToolTip(label, text);
            _leaseTips.SetToolTip(field, text);
        }

        private static void LockCalculated(NumericUpDown box)
        {
            box.ReadOnly = true;
            box.TabStop = false;
            box.InterceptArrowKeys = false;
            box.Increment = 0;
            box.BackColor = SystemColors.Control;
            box.TextAlign = HorizontalAlignment.Right;
            foreach (Control child in box.Controls)
            {
                if (child is TextBox text)
                {
                    text.BackColor = SystemColors.Control;
                    text.TextAlign = HorizontalAlignment.Right;
                    text.SetBounds(0, text.Top, Math.Max(0, box.ClientSize.Width - 2), text.Height);
                }
                else
                    child.Visible = false;
            }
        }

        private static void UnlockEntry(NumericUpDown box)
        {
            box.ReadOnly = false;
            box.TabStop = true;
            box.InterceptArrowKeys = true;
            box.BackColor = SystemColors.Window;
            foreach (Control child in box.Controls)
            {
                if (child is TextBox text)
                    text.BackColor = SystemColors.Window;
                else
                    child.Visible = true;
            }
        }

        private void ApplyRateEditor(bool moneyFactor)
        {
            if (moneyFactor)
            {
                if (numLoanRate.Value > 1m)
                    numLoanRate.Value = 1m;
                numLoanRate.Maximum = 1m;
                numLoanRate.DecimalPlaces = 8;
                numLoanRate.Increment = 0.00000001m;
                lblLoanRate.Text = "Money factor:";
                return;
            }

            numLoanRate.Maximum = 100m;
            numLoanRate.DecimalPlaces = 2;
            numLoanRate.Increment = 0.01m;
            lblLoanRate.Text = "Interest Rate (%):";
        }

        private bool _leaseMath;

        private void OnLeaseContractChanged(object? sender, EventArgs e)
        {
            if (_leaseMath || Account is not LeaseAccount)
                return;

            _leaseMath = true;
            try
            {
                if (sender == numLeaseGross || sender == numLeaseResidual || sender == numLeaseRent
                    || sender == numLeaseAmount || sender == numLoanPrincipal)
                    ShowAsCharge((NumericUpDown)sender);
                if (sender == numLeaseGross || sender == numLeaseReduction)
                    FillAdjustedCapCost();
                ApplyRentChargeFactor();
            }
            finally
            {
                _leaseMath = false;
            }
        }

        private void FillAdjustedCapCost()
        {
            var gross = Magnitude(numLeaseGross.Value);
            if (gross == 0)
                return;

            var adjusted = -(gross - numLeaseReduction.Value);
            if (adjusted > 0)
                adjusted = 0;
            if (adjusted < numLeaseAdjusted.Minimum)
                adjusted = numLeaseAdjusted.Minimum;
            if (adjusted > numLeaseAdjusted.Maximum)
                adjusted = numLeaseAdjusted.Maximum;
            if (numLeaseAdjusted.Value != adjusted)
                numLeaseAdjusted.Value = adjusted;
        }

        private void ApplyRentChargeFactor()
        {
            var factor = LeaseAccount.MoneyFactorFromRentCharge(
                Magnitude(numLeaseRent.Value),
                Magnitude(numLeaseAdjusted.Value),
                Magnitude(numLeaseResidual.Value),
                (int)numLoanTerm.Value);
            if (factor is not decimal value)
                return;

            if (value > numLoanRate.Maximum)
                value = numLoanRate.Maximum;
            var shown = Math.Round(value, numLoanRate.DecimalPlaces, MidpointRounding.AwayFromZero);
            if (numLoanRate.Value != shown)
                numLoanRate.Value = shown;
        }

        private void LayoutAutoPay(Panel subtypePanel)
        {
            pnlAutoPay.Visible = Account.SupportsAutoPay;
            subtypePanel.Top = pnlAutoPay.Visible ? pnlAutoPay.Bottom + 8 : pnlAutoPay.Top;
            UpdateAutoPayEnabled();
        }

        private void BindFundingAccounts()
        {
            var choices = new List<PayFromChoice> { PayFromChoice.Unspecified };
            choices.AddRange(_accounts
                .Where(a => a is BankAccount or CreditAccount)
                .Where(a => a.Id != Account.Id)
                .OrderBy(a => a.Name)
                .Select(PayFromChoice.From));

            cmbAutoPayFrom.DisplayMember = nameof(PayFromChoice.Name);
            cmbAutoPayFrom.ValueMember = nameof(PayFromChoice.Id);
            cmbAutoPayFrom.DataSource = choices;
            var selected = Account.AutoPayFromAccountId ?? Guid.Empty;
            cmbAutoPayFrom.SelectedItem = choices.FirstOrDefault(c => c.Id == selected) ?? PayFromChoice.Unspecified;
        }

        private void SaveAutoPay()
        {
            if (!Account.SupportsAutoPay)
            {
                Account.AutoPay = false;
                Account.AutoPayFromAccountId = null;
                return;
            }

            Account.AutoPay = chkAutoPay.Checked;
            Account.AutoPayFromAccountId = cmbAutoPayFrom.SelectedItem is PayFromChoice choice && choice.Id != Guid.Empty
                ? choice.Id
                : null;
        }

        private void UpdateAutoPayEnabled()
        {
            cmbAutoPayFrom.Enabled = pnlAutoPay.Visible;
        }

        private void ResizeFormForPanel(Panel panel)
        {
            int panelPadding = LogicalToDeviceUnits(10);
            int formPadding = LogicalToDeviceUnits(16);

            int contentBottom = 0;
            foreach (Control child in panel.Controls)
            {
                if (!child.Visible)
                    continue;
                contentBottom = Math.Max(contentBottom, child.Bottom);
            }

            panel.Height = contentBottom + panelPadding;
            var maxPanelHeight = LogicalToDeviceUnits(420);
            if (panel.Height > maxPanelHeight)
            {
                panel.AutoScroll = true;
                panel.Height = maxPanelHeight;
            }
            else
            {
                panel.AutoScroll = false;
            }

            ClientSize = new Size(
                ClientSize.Width,
                panel.Bottom + formPadding + pnlButtons.Height);
        }

        private void OnAccountTypeChanged(object sender, EventArgs e)
        {
            var selected = cmbAccountType.SelectedItem?.ToString();
            if (selected == null) return;

            var name = txtName.Text;
            var institution = txtInstitution.Text;
            var accountNumber = txtAccountNumber.Text;
            var url = txtUrl.Text;
            var autoPay = chkAutoPay.Checked;
            var autoPayFrom = cmbAutoPayFrom.SelectedItem is PayFromChoice choice && choice.Id != Guid.Empty
                ? choice.Id
                : (Guid?)null;

            var fromLease = Account is LeaseAccount;
            var rateBox = numLoanRate.Value;
            var carriedLoan = Account is LoanAccount currentLoan
                && selected is "Loan" or "Lease"
                ? currentLoan
                : null;

            if (selected == ComboLabel(_opened))
            {
                Account = _opened;
            }
            else
            {
                Account = selected switch
                {
                    "Bank" => new BankAccount { Type = AccountType.Checking },
                    "Credit" => new CreditAccount
                    {
                        Type = AccountType.CreditCard,
                        StatementDate = DateTime.Today,
                        DueDate = DateTime.Today
                    },
                    "Loan" => new LoanAccount { Type = AccountType.Loan },
                    "Lease" => new LeaseAccount { Type = AccountType.Lease },
                    "Mortgage" => new MortgageAccount
                    {
                        Type = AccountType.Mortgage,
                        NextPaymentDate = DateTime.Today
                    },
                    "Investment" => new InvestmentAccount { Type = AccountType.Investment },
                    "Internal" => new InternalAccount { Type = AccountType.Internal },
                    "Utility" => new UntrackedAccount { Type = AccountType.Utility },
                    "Service" => new UntrackedAccount { Type = AccountType.Service },
                    "Insurance" => new UntrackedAccount { Type = AccountType.Insurance },
                    "Tax" => new UntrackedAccount { Type = AccountType.Tax },
                    _ => Account
                };
                Account.Id = _opened.Id;
                Account.CreatedOn = _opened.CreatedOn;
                Account.ModifiedOn = _opened.ModifiedOn;
                Account.ExternalLink = _opened.ExternalLink;
                Account.BalanceAsOf = _opened.BalanceAsOf;
                if (carriedLoan is not null && Account is LoanAccount loan)
                {
                    loan.Principal = PostedBalanceCalculator.ToLedgerBalance(loan, numLoanPrincipal.Value);
                    loan.TermMonths = (int)numLoanTerm.Value;
                    loan.SecuredAssetId = carriedLoan.SecuredAssetId;
                    loan.StartDate = carriedLoan.StartDate;
                    loan.ScheduleType = carriedLoan.ScheduleType;
                    if (loan is LeaseAccount nextLease)
                    {
                        nextLease.MoneyFactor = fromLease
                            ? rateBox
                            : LeaseAccount.MoneyFactorFromAprPercent(rateBox);
                        nextLease.GrossCapitalizedCost = Magnitude(numLeaseGross.Value);
                        nextLease.CapitalizedCostReduction = numLeaseReduction.Value;
                        nextLease.AdjustedCapitalizedCost = Magnitude(numLeaseAdjusted.Value);
                        nextLease.RentCharge = Magnitude(numLeaseRent.Value);
                        nextLease.LeaseAmount = Magnitude(numLeaseAmount.Value);
                        nextLease.Residual = Magnitude(numLeaseResidual.Value);
                    }
                    else
                    {
                        loan.InterestRate = fromLease
                            ? rateBox * LeaseAccount.AprMultiplier
                            : rateBox;
                    }
                }
            }

            Account.Name = name;
            Account.Institution = institution;
            Account.AccountNumber = accountNumber;
            Account.WebsiteUrl = url;
            Account.AutoPay = autoPay;
            Account.AutoPayFromAccountId = autoPayFrom;
            BindFields();
            ShowCorrectPanel();
        }

        private void OnSave(object sender, EventArgs e)
        {
            DiscardActivity = false;
            SaveFields();
            if (string.IsNullOrWhiteSpace(Account.Name))
            {
                MessageBox.Show(this,
                    "Name is required.",
                    "Account Editor",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (Account.AutoPay &&
                (Account.AutoPayFromAccountId is not Guid funding || funding == Guid.Empty))
            {
                MessageBox.Show(this,
                    "Choose the account the institution should debit for auto-pay.",
                    "Account Editor",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (_persisted
                && !AccountConversion.SameKind(_opened, Account)
                && !AccountConversion.CanCarryTransactions(_opened, Account))
            {
                var answer = MessageBox.Show(
                    this,
                    AccountConversion.LossWarning,
                    "Account Editor",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);
                if (answer != DialogResult.Yes)
                    return;

                DiscardActivity = true;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnCancel(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private static void ShiftStartingBalance(Account account, decimal newStartingBalance)
        {
            var current = PostedBalanceCalculator.GetStartingBalance(account);
            PostedBalanceCalculator.AdjustStartingBalance(account, newStartingBalance - current);
        }

        private static decimal Clamp(NumericUpDown box, decimal value)
        {
            if (value < box.Minimum)
                return box.Minimum;
            if (value > box.Maximum)
                return box.Maximum;
            return value;
        }

        /// <summary>
        /// Contract amounts are stored as positive figures. The editor shows an amount owed as a negative charge.
        /// Balance keeps the ledger sign and is not passed through here.
        /// </summary>
        private static decimal Charge(decimal amount) => amount > 0 ? -amount : amount;

        private static decimal Magnitude(decimal amount) => decimal.Abs(amount);

        private static void ShowAsCharge(NumericUpDown box)
        {
            if (box.Value > 0)
                box.Value = -box.Value;
        }

        private static DateTime PickerDate(DateTimePicker picker, DateTime? value)
        {
            var date = value ?? DateTime.Today;
            if (date < picker.MinDate || date > picker.MaxDate)
                return DateTime.Today;
            return date;
        }
    }
}
