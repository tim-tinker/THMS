using System.ComponentModel;
using THMS.Logic.Orchestrators.Finance;
using THMS.Logic.ViewModels.Finance;

namespace THMS.UI.WinForms.Controls
{
    public sealed class PlaidAccountSetupDialog : Form
    {
        private readonly PlaidAccountOrchestrator _orchestrator;
        private BindingList<PlaidAccountViewModel> _rows = [];
        private readonly DataGridView _grid = new();
        private readonly Label _status = new();
        private readonly ThmsButton _btnLink = new();
        private readonly ThmsButton _btnSave = new();

        public PlaidAccountSetupDialog()
            : this(new PlaidAccountOrchestrator())
        {
        }

        public PlaidAccountSetupDialog(PlaidAccountOrchestrator orchestrator)
        {
            _orchestrator = orchestrator;

            Text = "Plaid Account Setup";
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            ShowInTaskbar = false;
            Size = new Size(980, 520);
            MinimumSize = new Size(640, 360);

            var heading = new Label
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Height = 36,
                Padding = new Padding(8, 8, 8, 0),
                Text = "Connect a Plaid institution, then map each Plaid account to a THMS account.",
                TextAlign = ContentAlignment.MiddleLeft
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
                Padding = new Padding(8, 8, 8, 12),
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

        private void OnSaveMapping(object? sender, EventArgs e)
        {
            _grid.EndEdit();
            if (!CanSaveMappings())
                return;

            try
            {
                var saved = _orchestrator.SaveAccountMappings(_rows);
                RefreshAccountChoices();
                _status.Text = saved == 0
                    ? "No mappings saved. Choose a THMS account for each Plaid account."
                    : $"Saved {saved} Plaid account mapping{(saved == 1 ? "" : "s")}.";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Save mapping failed.\n{ex.Message}", "Plaid Account Setup",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _status.Text = "Save mapping failed.";
            }
            finally
            {
                UpdateActionButtons();
            }
        }

        private void UpdateActionButtons()
        {
            _btnSave.Enabled = CanSaveMappings();
        }

        private bool CanSaveMappings() =>
            _rows.Any(row => row.SuggestedThmsAccountId != Guid.Empty);
    }
}
