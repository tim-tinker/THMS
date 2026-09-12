using System;
using System.Windows.Forms;
using THMS.Domain.Finance.Accounts;
using THMS.Logic.Finance.Model;

namespace THMS.UI.WinForms.Controls
{
    public partial class AccountEditForm : Form
    {
        public Account Account { get; private set; }

        public AccountEditForm(Account? existing)
        {
            InitializeComponent();

            // If creating new, default to BankAccount
            Account = existing ?? new BankAccount { Type = AccountType.Checking };

            PopulateAccountTypeCombo();
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
            cmbAccountType.Items.Add("Mortgage");
            cmbAccountType.Items.Add("Investment");
            cmbAccountType.Items.Add("Internal");
            cmbAccountType.Items.Add("Utility");
            cmbAccountType.Items.Add("Service");
            cmbAccountType.Items.Add("Insurance");
            cmbAccountType.SelectedItem = ComboLabel(Account);
            cmbAccountType.SelectedIndexChanged += OnAccountTypeChanged;
        }

        private static string ComboLabel(Account account) => AccountKinds.Of(account) switch
        {
            AccountKinds.Bank => "Bank",
            AccountKinds.Credit => "Credit",
            AccountKinds.Loan => "Loan",
            AccountKinds.Mortgage => "Mortgage",
            AccountKinds.Investment => "Investment",
            AccountKinds.Internal => "Internal",
            AccountKinds.Utility => "Utility",
            AccountKinds.Service => "Service",
            AccountKinds.Insurance => "Insurance",
            _ => "Bank"
        };

        private void BindFields()
        {
            txtName.Text = Account.Name;
            txtInstitution.Text = Account.Institution;
            txtAccountNumber.Text = Account.AccountNumber;
            txtUrl.Text = Account.WebsiteUrl;

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

                case LoanAccount loan:
                    numLoanPrincipal.Value = loan.Principal;
                    numLoanRate.Value = loan.InterestRate;
                    numLoanTerm.Value = loan.TermMonths;
                    break;

                case MortgageAccount mortgage:
                    numMortPrincipal.Value = mortgage.Principal;
                    numMortRate.Value = mortgage.InterestRate;
                    numMortTerm.Value = mortgage.TermMonths;
                    dtMortNext.Value = PickerDate(dtMortNext, mortgage.NextPaymentDate);
                    break;

                case InvestmentAccount invest:
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

                case LoanAccount loan:
                    loan.Principal = numLoanPrincipal.Value;
                    loan.InterestRate = numLoanRate.Value;
                    loan.TermMonths = (int)numLoanTerm.Value;
                    break;

                case MortgageAccount mortgage:
                    mortgage.Principal = numMortPrincipal.Value;
                    mortgage.InterestRate = numMortRate.Value;
                    mortgage.TermMonths = (int)numMortTerm.Value;
                    mortgage.NextPaymentDate = dtMortNext.Value;
                    break;

                case InvestmentAccount invest:
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

            // Sizing before the handle exists (and AutoScale runs) clips the button bar.
            if (IsHandleCreated)
                ResizeFormForPanel(panel);
        }

        private void ResizeFormForPanel(Panel panel)
        {
            int panelPadding = LogicalToDeviceUnits(10);
            int formPadding = LogicalToDeviceUnits(16);

            int contentBottom = 0;
            foreach (Control child in panel.Controls)
                contentBottom = Math.Max(contentBottom, child.Bottom);

            panel.Height = contentBottom + panelPadding;

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
                _ => Account
            };

            Account.Name = name;
            Account.Institution = institution;
            Account.AccountNumber = accountNumber;
            Account.WebsiteUrl = url;
            BindFields();
            ShowCorrectPanel();
        }

        private void OnSave(object sender, EventArgs e)
        {
            SaveFields();
            if (string.IsNullOrWhiteSpace(Account.Name) ||
                string.IsNullOrWhiteSpace(Account.Institution) ||
                string.IsNullOrWhiteSpace(Account.AccountNumber))
            {
                MessageBox.Show(this,
                    "Name, institution, and account number are required.",
                    "Account Editor",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
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
