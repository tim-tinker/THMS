using System.ComponentModel;
using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Planning;
using THMS.Logic.Orchestrators.Finance;
using THMS.Logic.ViewModels.Finance;

namespace THMS.UI.WinForms.Controls
{
    public partial class AccountUpdaterControl : UserControl
    {
        private readonly AccountOrchestrator _accountOrchestrator = new();
        private readonly AccountImportOrchestrator _importOrchestrator = new();
        private readonly IAccountStatementDataStore _statements = new DataStoreFactory().GetAccountStatementStore();
        private bool _suppressSelectionEvents;

        public event EventHandler? SelectedAccountChanged;

        public Account? SelectedAccount => GetSelectedAccount();

        public AccountUpdaterControl()
        {
            InitializeComponent();
            DataGridViewUtil.EnableDoubleBuffering(gridAccounts);
            gridAccounts.SelectionChanged += (_, _) => OnAccountSelectionChanged();
            gridAccounts.DataBindingComplete += (_, _) => OnAccountSelectionChanged();
            gridAccounts.CellDoubleClick += OnAccountCellDoubleClick;
            LoadAccounts();
        }

        public void RefreshAccounts() => LoadAccounts();

        private void LoadAccounts()
        {
            var selectedId = GetSelectedAccount()?.Id;
            var accounts = _accountOrchestrator.GetAllAccounts()
                .Select(account => AccountRegisterRow.From(
                    account,
                    LatestStatement(_statements.GetForAccount(account.Id))))
                .ToList();
            _suppressSelectionEvents = true;
            try
            {
                gridAccounts.DataSource = null;
                gridAccounts.DataSource = new BindingList<AccountRegisterRow>(accounts);
                SelectAccount(selectedId);
            }
            finally
            {
                _suppressSelectionEvents = false;
            }

            UpdateActionButtons();
            SelectedAccountChanged?.Invoke(this, EventArgs.Empty);
        }

        private static AccountStatement? LatestStatement(IEnumerable<AccountStatement> statements) =>
            statements
                .OrderByDescending(s => s.StatementDate)
                .ThenByDescending(s => s.DueDate)
                .FirstOrDefault();

        public void SetImportStatus(string message) => lblStatus.Text = message;

        private void SelectAccount(Guid? id)
        {
            if (id is null)
                return;

            foreach (DataGridViewRow row in gridAccounts.Rows)
            {
                if (row.DataBoundItem is AccountRegisterRow item && item.Id == id)
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
            btnDelete.Enabled = hasSelection;
        }

        private void OnImportAccounts(object sender, EventArgs e)
        {
            using var fileDialog = new OpenFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*",
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
                if (preview.ShowDialog(FindForm()) != DialogResult.OK)
                    return;

                LoadAccounts();
                SetImportStatus(ImportStatusText.Imported(preview.Result, "account", "accounts"));
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

        private void OnAccountCellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            EditSelectedAccount();
        }

        private void OnAddAccount(object sender, EventArgs e)
        {
            using var dlg = new AccountEditForm(null);
            if (dlg.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            SaveAndReload(dlg.Account);
        }

        private void EditSelectedAccount()
        {
            var acct = GetSelectedAccount();
            if (acct == null)
                return;

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
            if (gridAccounts.CurrentRow?.DataBoundItem is AccountRegisterRow row)
                return row.Account;

            return null;
        }
    }
}
