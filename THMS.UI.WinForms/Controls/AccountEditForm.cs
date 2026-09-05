using System;
using System.Windows.Forms;
using THMS.Domain.Finance.Accounts;

namespace THMS.UI.WinForms.Controls
{
    public partial class AccountEditForm : Form
    {
        public Account Account { get; private set; }

        public AccountEditForm(Account? existing)
        {
            InitializeComponent();

            // If creating new, default to BankAccount
            Account = existing ?? new BankAccount();

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
            cmbAccountType.Items.Clear();
            cmbAccountType.Items.Add("Bank");
            cmbAccountType.Items.Add("Credit");
            cmbAccountType.Items.Add("Loan");
            cmbAccountType.Items.Add("Mortgage");
            cmbAccountType.Items.Add("Investment");
            cmbAccountType.Items.Add("Internal");

            cmbAccountType.SelectedItem = Account.Type.ToString();
        }

        private void BindFields()
        {
            txtName.Text = Account.Name;
            txtInstitution.Text = Account.Institution;
            txtAccountNumber.Text = Account.AccountNumber;
            txtUrl.Text = Account.WebsiteUrl;
            dtBalanceAsOf.Value = Account.BalanceAsOf ?? DateTime.Today;

            // Subtype-specific binding
            switch (Account)
            {
                case BankAccount bank:
                    numBankPosted.Value = bank.PostedBalance;
                    numBankOverdraft.Value = bank.OverdraftLimit;
                    break;

                case CreditAccount credit:
                    numCreditLimit.Value = credit.CreditLimit;
                    numCreditApr.Value = credit.APR;
                    dtCreditStatement.Value = credit.StatementDate;
                    dtCreditDue.Value = credit.DueDate;
                    numCreditPosted.Value = credit.PostedBalance;
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
                    dtMortNext.Value = mortgage.NextPaymentDate;
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
            Account.BalanceAsOf = dtBalanceAsOf.Value;
            Account.WebsiteUrl = txtUrl.Text;

            // Subtype-specific save
            switch (Account)
            {
                case BankAccount bank:
                    bank.PostedBalance = numBankPosted.Value;
                    bank.OverdraftLimit = numBankOverdraft.Value;
                    break;

                case CreditAccount credit:
                    credit.CreditLimit = numCreditLimit.Value;
                    credit.APR = numCreditApr.Value;
                    credit.StatementDate = dtCreditStatement.Value;
                    credit.DueDate = dtCreditDue.Value;
                    credit.PostedBalance = numCreditPosted.Value;
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

            // Replace Account with new subtype instance
            Account = selected switch
            {
                "Bank" => new BankAccount(),
                "Credit" => new CreditAccount
                {
                    StatementDate = DateTime.Today,
                    DueDate = DateTime.Today
                },
                "Loan" => new LoanAccount(),
                "Mortgage" => new MortgageAccount
                {
                    NextPaymentDate = DateTime.Today
                },
                "Investment" => new InvestmentAccount(),
                "Internal" => new InternalAccount(),
                _ => Account
            };

            BindFields();
            ShowCorrectPanel();
        }

        private void OnSave(object sender, EventArgs e)
        {
            SaveFields();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnCancel(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
