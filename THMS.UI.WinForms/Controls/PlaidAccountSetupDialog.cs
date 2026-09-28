using System.ComponentModel;
using THMS.Logic.Orchestrators.Finance;
using THMS.Logic.ViewModels.Finance;

namespace THMS.UI.WinForms.Controls
{
    public sealed class PlaidAccountSetupDialog : Form
    {
        private readonly PlaidAccountOrchestrator _orchestrator;
        private readonly PlaidTransactionOrchestrator _transactionOrchestrator;
        private BindingList<PlaidAccountViewModel> _rows = [];
        private readonly DataGridView _grid = new();
        private readonly Label _status = new();
        private readonly ThmsButton _btnLink = new();
        private readonly ThmsButton _btnSave = new();

        public PlaidAccountSetupDialog()
            : this(new PlaidAccountOrchestrator(), new PlaidTransactionOrchestrator())
        {
        }

        public PlaidAccountSetupDialog(PlaidAccountOrchestrator orchestrator)
            : this(orchestrator, new PlaidTransactionOrchestrator())
        {
        }

        public PlaidAccountSetupDialog(
            PlaidAccountOrchestrator orchestrator,
            PlaidTransactionOrchestrator transactionOrchestrator)
        {
            _orchestrator = orchestrator;
            _transactionOrchestrator = transactionOrchestrator;

            Text = "Plaid Account Setup";
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            ShowInTaskbar = false;
            Size = new Size(980, 520);
            MinimumSize = new Size(640, 360);

            var heading = new Label
            {
                AutoSize = true,
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Padding = new Padding(12, 12, 12, 8),
                Text = "Connect a Plaid institution, then map each Plaid account to a THMS account."
            };

            _btnLink.Text = "Link Institution (Plaid Link)";
            _btnLink.Click += OnLinkInstitution;
            _btnSave.Text = "Save Mapping";
            _btnSave.Click += OnSaveMapping;
            var btnClose = new ThmsButton { Text = "Close", DialogResult = DialogResult.OK };

            var buttons = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(12, 12, 12, 16),
                WrapContents = false
            };
            buttons.Controls.Add(btnClose);
            buttons.Controls.Add(_btnSave);
            buttons.Controls.Add(_btnLink);

            _status.Dock = DockStyle.Bottom;
            _status.Height = 24;
            _status.Padding = new Padding(8, 0, 8, 0);
            _status.Text = "Link an institution to load Plaid accounts.";
            _status.TextAlign = ContentAlignment.MiddleLeft;

            ConfigureGrid();

            Controls.Add(_grid);
            Controls.Add(_status);
            Controls.Add(buttons);
            Controls.Add(heading);
            CancelButton = btnClose;
            AcceptButton = btnClose;
            _grid.CellValueChanged += (_, _) => UpdateActionButtons();
            UpdateActionButtons();
        }

        private void ConfigureGrid()
        {
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.AutoGenerateColumns = false;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _grid.Dock = DockStyle.Fill;
            _grid.EditMode = DataGridViewEditMode.EditOnEnter;
            _grid.RowHeadersVisible = false;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.DataError += (_, e) => e.ThrowException = false;
            DataGridViewUtil.EnableDoubleBuffering(_grid);
            _grid.CurrentCellDirtyStateChanged += (_, _) =>
            {
                if (_grid.IsCurrentCellDirty)
                    _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
                UpdateActionButtons();
            };
            _grid.Columns.AddRange(
                TextColumn(nameof(PlaidAccountViewModel.Institution), "Institution"),
                TextColumn(nameof(PlaidAccountViewModel.PlaidAccountId), "PlaidAccountId"),
                TextColumn(nameof(PlaidAccountViewModel.Mask), "Mask"),
                TextColumn(nameof(PlaidAccountViewModel.Subtype), "Subtype"),
                new DataGridViewComboBoxColumn
                {
                    Name = nameof(PlaidAccountViewModel.SuggestedThmsAccountId),
                    DataPropertyName = nameof(PlaidAccountViewModel.SuggestedThmsAccountId),
                    HeaderText = "Suggested THMS Account",
                    DisplayMember = nameof(AccountMappingChoice.Name),
                    ValueMember = nameof(AccountMappingChoice.Id),
                    FlatStyle = FlatStyle.Flat,
                    DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
                });
            RefreshAccountChoices();
        }

        private static DataGridViewTextBoxColumn TextColumn(string property, string header) =>
            new()
            {
                DataPropertyName = property,
                HeaderText = header,
                Name = property,
                ReadOnly = true
            };

        private void RefreshAccountChoices()
        {
            if (_grid.Columns[nameof(PlaidAccountViewModel.SuggestedThmsAccountId)]
                is not DataGridViewComboBoxColumn combo)
                return;

            combo.DataSource = _orchestrator.GetThmsAccountChoices().ToList();
        }

        private async void OnLinkInstitution(object? sender, EventArgs e)
        {
            using var dialog = new PlaidLinkDialog(_orchestrator);
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            _btnLink.Enabled = false;
            _status.Text = "Linking institution...";
            try
            {
                await _orchestrator.StartLinkFlow(dialog.PublicToken);
                _rows = new BindingList<PlaidAccountViewModel>(_orchestrator.GetPlaidAccounts());
                _rows.ListChanged += (_, _) => UpdateActionButtons();
                RefreshAccountChoices();
                _grid.DataSource = _rows;
                _status.Text = $"Linked {_rows.Count} Plaid account{(_rows.Count == 1 ? "" : "s")}.";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Plaid Link failed.\n{ex.Message}", "Plaid Account Setup",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _status.Text = "Plaid Link failed.";
            }
            finally
            {
                _btnLink.Enabled = true;
                UpdateActionButtons();
            }
        }

        private async void OnSaveMapping(object? sender, EventArgs e)
        {
            _grid.EndEdit();
            if (!CanSaveMappings())
                return;

            _btnSave.Enabled = false;
            _btnLink.Enabled = false;
            try
            {
                var saved = _orchestrator.SaveAccountMappings(_rows);
                RefreshAccountChoices();
                if (saved == 0)
                {
                    _status.Text = "No mappings saved. Choose a THMS account for each Plaid account.";
                    return;
                }

                _status.Text = $"Saved {saved} Plaid account mapping{(saved == 1 ? "" : "s")}.";
                await ImportHistoryForNewItemsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Save mapping failed.\n{ex.Message}", "Plaid Account Setup",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _status.Text = "Save mapping failed.";
            }
            finally
            {
                _btnLink.Enabled = true;
                UpdateActionButtons();
            }
        }

        private async Task ImportHistoryForNewItemsAsync()
        {
            var items = _orchestrator.ItemsNeedingInitialHistory(_rows);
            if (items.Count == 0)
                return;

            var imported = 0;
            foreach (var item in items)
            {
                using var dialog = new PlaidHistoryStartDialog(item.Institution);
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    _status.Text = "History not imported yet. Use Link Accounts later, or Sync Data after a first import.";
                    AppStatus.Set("Plaid is linked. Import history from Link Accounts before auto-sync can run.");
                    continue;
                }

                _status.Text = $"Importing Plaid history ({item.Institution})...";
                AppStatus.Set($"Importing Plaid history ({item.Institution})...", busy: true);
                var progress = new Progress<PlaidSyncProgress>(AppStatus.Report);
                var result = await _transactionOrchestrator.SyncInitialAsync(
                    item.ItemId,
                    dialog.HistoryStart,
                    progress: progress);
                imported += result.Imported;
                AppStatus.Set(result.Summary);
                _status.Text = result.Summary;
            }

            if (imported > 0)
                _status.Text = $"Imported {imported:N0} Plaid transaction{(imported == 1 ? "" : "s")} from history.";
        }

        private void UpdateActionButtons()
        {
            _btnSave.Enabled = CanSaveMappings();
        }

        private bool CanSaveMappings() =>
            _rows.Any(row => row.SuggestedThmsAccountId != Guid.Empty);
    }
}
