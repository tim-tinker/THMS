using THMS.Domain.Finance.Accounts;
using THMS.Logic.Orchestrators.Finance;

namespace THMS.UI.WinForms.Controls
{
    public partial class AccountUpdaterControl : UserControl
    {
        private readonly AccountOrchestrator _accountOrchestrator = new();

        public AccountUpdaterControl()
        {
            InitializeComponent();

            LoadAccounts();
        }

        private void LoadAccounts()
        {
            var accounts = _accountOrchestrator.GetAllAccounts();
            gridAccounts.DataSource = accounts;
        }

        private void OnAddAccount(object sender, EventArgs e)
        {
            var dlg = new AccountEditForm(null);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _accountOrchestrator.Save(dlg.Account);
                LoadAccounts();
            }
        }

        private void OnEditAccount(object sender, EventArgs e)
        {
            var acct = GetSelectedAccount();
            if (acct == null)
            {
                MessageBox.Show("Please select an account to edit.");
                return;
            }

            var dlg = new AccountEditForm(acct);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _accountOrchestrator.Save(dlg.Account);
                LoadAccounts();
            }
        }

        private void OnDeleteAccount(object sender, EventArgs e)
        {
            var acct = GetSelectedAccount();
            if (acct == null)
            {
                MessageBox.Show("Please select an account to delete.");
                return;
            }

            var confirm = MessageBox.Show(
                $"Delete account '{acct.Name}'?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm == DialogResult.Yes)
            {
                _accountOrchestrator.Delete(acct.Id);
                LoadAccounts();
            }
        }

        private Account? GetSelectedAccount()
        {
            if (gridAccounts.CurrentRow?.DataBoundItem is Account acct)
                return acct;

            return null;
        }
    }
}
