using System.ComponentModel;
using THMS.Domain.Finance.Accounts;
using THMS.Logic.Orchestrators.Finance;

namespace THMS.UI.WinForms.Controls
{
    public partial class AccountUpdaterControl : UserControl
    {
        private readonly AccountOrchestrator _accountOrchestrator = new();
        private readonly AccountImportOrchestrator _importOrchestrator = new();
        private bool _suppressSelectionEvents;

        public event EventHandler? SelectedAccountChanged;

        public Account? SelectedAccount => GetSelectedAccount();

        public AccountUpdaterControl()
        {
            InitializeComponent();
            gridAccounts.SelectionChanged += (_, _) => OnAccountSelectionChanged();
            gridAccounts.DataBindingComplete += (_, _) => OnAccountSelectionChanged();
            LoadAccounts();
        }

        public void RefreshAccounts() => LoadAccounts();

        private void LoadAccounts()
        {
            var selectedId = GetSelectedAccount()?.Id;
            var accounts = _accountOrchestrator.GetAllAccounts().ToList();
            _suppressSelectionEvents = true;
            try
            {
                gridAccounts.DataSource = null;
                gridAccounts.DataSource = new BindingList<Account>(accounts);
                SelectAccount(selectedId);
            }
            finally
            {
                _suppressSelectionEvents = false;
            }

            UpdateActionButtons();
            SelectedAccountChanged?.Invoke(this, EventArgs.Empty);
        }

        private void SelectAccount(Guid? id)
        {
            if (id is null)
                return;

            foreach (DataGridViewRow row in gridAccounts.Rows)
            {
                if (row.DataBoundItem is Account account && account.Id == id)
                {
                    row.Selected = true;
                    if (row.Cells.Count > 0 && row.Cells[0].Visible)
                        gridAccounts.CurrentCell = row.Cells[0];
                    return;
                }
            }
        }

        private void OnAccountSelectionChanged()
        {
            UpdateActionButtons();
            if (!_suppressSelectionEvents)
                SelectedAccountChanged?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateActionButtons()
        {
            var hasSelection = GetSelectedAccount() is not null;
            btnEdit.Enabled = hasSelection;
            btnDelete.Enabled = hasSelection;
        }

        private void OnImportAccounts(object sender, EventArgs e)
        {
            using var fileDialog = new OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*",
                Title = "Select account spreadsheet"
            };
            if (fileDialog.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            try
            {
                var rows = _importOrchestrator.LoadAccountsFromFile(fileDialog.FileName);
                if (rows.Count == 0)
                {
                    MessageBox.Show(FindForm(), "The selected file did not contain any accounts.", "Import Accounts",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using var preview = new AccountImportPreviewDialog(rows, _importOrchestrator);
                if (preview.ShowDialog(FindForm()) == DialogResult.OK)
                    LoadAccounts();
            }
            catch (Exception ex)
            {
                MessageBox.Show(FindForm(), $"Could not parse the file.\n{ex.Message}", "Import Accounts",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OnConnectToPlaid(object sender, EventArgs e)
        {
            using var dialog = new PlaidAccountSetupDialog();
            dialog.ShowDialog(FindForm());
            LoadAccounts();
        }

        private void OnRunDiagnostics(object sender, EventArgs e)
        {
            using var dialog = new AccountDiagnosticsDialog();
            dialog.ShowDialog(FindForm());
        }

        private void OnAddAccount(object sender, EventArgs e)
        {
            using var dlg = new AccountEditForm(null);
            if (dlg.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            SaveAndReload(dlg.Account);
        }

        private void OnEditAccount(object sender, EventArgs e)
        {
            var acct = GetSelectedAccount();
            if (acct == null)
            {
                MessageBox.Show(this, "Please select an account to edit.", "Accounts",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dlg = new AccountEditForm(acct);
            if (dlg.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            SaveAndReload(dlg.Account);
        }

        private void OnDeleteAccount(object sender, EventArgs e)
        {
            var acct = GetSelectedAccount();
            if (acct == null)
            {
                MessageBox.Show(this, "Please select an account to delete.", "Accounts",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show(
                this,
                $"Delete account '{acct.Name}'?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes)
                return;

            try
            {
                _accountOrchestrator.Delete(acct.Id);
                LoadAccounts();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Accounts", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void SaveAndReload(Account account)
        {
            try
            {
                _accountOrchestrator.Save(account);
                LoadAccounts();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Accounts", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
