using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;

namespace THMS.UI.WinForms.Controls
{
    public partial class AccountUpdaterControl : UserControl
    {
        private readonly IAccountDataStore _accountStore;

        public AccountUpdaterControl(IAccountDataStore accountStore)
        {
            InitializeComponent();
            _accountStore = accountStore;

            LoadAccounts();
        }

        private void LoadAccounts()
        {
            var accounts = _accountStore.GetAllAccounts();
            gridAccounts.DataSource = accounts;
        }

        private void OnAddAccount(object sender, EventArgs e)
        {
            var dlg = new AccountEditForm(null);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _accountStore.UpsertAccount(dlg.Account);
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
                _accountStore.UpsertAccount(dlg.Account);
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
                _accountStore.DeleteAccount(acct.Id);
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
