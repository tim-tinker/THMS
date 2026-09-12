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
        private readonly Button _btnLink = new();
        private readonly Button _btnSave = new();

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
            _btnLink.AutoSize = true;
            _btnLink.Click += OnLinkInstitution;
            _btnSave.Text = "Save Mapping";
            _btnSave.AutoSize = true;
            _btnSave.Click += OnSaveMapping;
            var btnClose = new Button { Text = "Close", DialogResult = DialogResult.OK, AutoSize = true };

            var toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(8, 4, 8, 4),
                WrapContents = false
            };
            toolbar.Controls.Add(_btnLink);
            toolbar.Controls.Add(_btnSave);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 48,
                Padding = new Padding(8, 8, 8, 8)
            };
            buttons.Controls.Add(btnClose);

            _status.Dock = DockStyle.Bottom;
            _status.Height = 24;
            _status.Padding = new Padding(8, 0, 8, 0);
            _status.Text = "Link an institution to load Plaid accounts.";
            _status.TextAlign = ContentAlignment.MiddleLeft;

            ConfigureGrid();

            Controls.Add(_grid);
            Controls.Add(_status);
            Controls.Add(buttons);
            Controls.Add(toolbar);
            Controls.Add(heading);
            CancelButton = btnClose;
            AcceptButton = btnClose;
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
            using var dialog = new PlaidLinkDialog();
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            _btnLink.Enabled = false;
            _status.Text = "Linking institution...";
            try
            {
                await _orchestrator.StartLinkFlow(dialog.PublicToken);
                _rows = new BindingList<PlaidAccountViewModel>(_orchestrator.GetPlaidAccounts());
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
            }
        }

        private void OnSaveMapping(object? sender, EventArgs e)
        {
            _grid.EndEdit();
            if (_rows.Count == 0)
            {
                MessageBox.Show(this, "Link an institution before saving mappings.", "Plaid Account Setup",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

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
        }
    }
}
