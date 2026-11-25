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

        public AccountEditForm(Account? existing, IEnumerable<Account>? accounts = null, bool persisted = false)
        {
            InitializeComponent();

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
                        numLeaseGross.Value = Clamp(numLeaseGross, lease.GrossCapitalizedCost);
                        numLeaseReduction.Value = Clamp(numLeaseReduction, lease.CapitalizedCostReduction);
                        numLeaseAdjusted.Value = Clamp(numLeaseAdjusted, lease.AdjustedCapitalizedCost);
                        numLeaseResidual.Value = Clamp(numLeaseResidual, lease.Residual);
                        numLeaseRent.Value = Clamp(numLeaseRent, lease.RentCharge);
                        numLeaseAmount.Value = Clamp(numLeaseAmount, lease.LeaseAmount);
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
                    lease.GrossCapitalizedCost = numLeaseGross.Value;
                    lease.CapitalizedCostReduction = numLeaseReduction.Value;
                    lease.AdjustedCapitalizedCost = numLeaseAdjusted.Value;
                    lease.Residual = numLeaseResidual.Value;
                    lease.RentCharge = numLeaseRent.Value;
                    lease.LeaseAmount = numLeaseAmount.Value;
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
            ApplyRateEditor(lease);
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
            lblLeaseHint.Visible = lease;
            RefreshLeaseApr();
            pnlLoan.Height = lease ? 516 : 120;
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
                if (sender == numLeaseGross || sender == numLeaseReduction)
                    FillAdjustedCapCost();
                ApplyRentChargeFactor();
            }
            finally
            {
                _leaseMath = false;
            }

            RefreshLeaseApr();
        }

        private void FillAdjustedCapCost()
        {
            if (numLeaseGross.Value <= 0)
                return;

            var adjusted = numLeaseGross.Value - numLeaseReduction.Value;
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
                numLeaseRent.Value,
                numLeaseAdjusted.Value,
                numLeaseResidual.Value,
                (int)numLoanTerm.Value);
            if (factor is not decimal value)
                return;

            if (value > numLoanRate.Maximum)
                value = numLoanRate.Maximum;
            var shown = Math.Round(value, numLoanRate.DecimalPlaces, MidpointRounding.AwayFromZero);
            if (numLoanRate.Value != shown)
                numLoanRate.Value = shown;
        }

        private void RefreshLeaseApr()
        {
            if (lblLeaseHint is null || Account is not LeaseAccount)
                return;

            var rate = numLoanRate.Value > 0
                ? $" About {numLoanRate.Value * LeaseAccount.AprMultiplier:0.###}% APR."
                : "";
            var check = "";
            if (numLeaseAdjusted.Value > numLeaseResidual.Value && numLeaseRent.Value > 0)
            {
                var depreciation = numLeaseAdjusted.Value - numLeaseResidual.Value;
                var basePayments = depreciation + numLeaseRent.Value;
                check = $" Depreciation {depreciation:c2} plus the rent charge is {basePayments:c2}.";
            }

            lblLeaseHint.Text =
                "Balance is the remaining payments. Adjusted cap cost is gross cap cost minus cap reduction. " +
                "Money factor = rent charge ÷ (term × (adjusted cap cost + residual))." +
                rate + check +
                " Lease amount is the total of the payments and is not part of net worth. " +
                "Residual is the buyout. Keeping the vehicle is its value, minus the balance, minus the residual.";
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
                        nextLease.GrossCapitalizedCost = numLeaseGross.Value;
                        nextLease.CapitalizedCostReduction = numLeaseReduction.Value;
                        nextLease.AdjustedCapitalizedCost = numLeaseAdjusted.Value;
                        nextLease.RentCharge = numLeaseRent.Value;
                        nextLease.LeaseAmount = numLeaseAmount.Value;
                        nextLease.Residual = numLeaseResidual.Value;
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

        private static DateTime PickerDate(DateTimePicker picker, DateTime? value)
        {
            var date = value ?? DateTime.Today;
            if (date < picker.MinDate || date > picker.MaxDate)
                return DateTime.Today;
            return date;
        }
    }
}
