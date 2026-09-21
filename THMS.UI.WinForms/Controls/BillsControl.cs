using System.Diagnostics;
using THMS.Domain.Finance.Accounts;
using THMS.Logic.Orchestrators.Finance;
using THMS.Logic.ViewModels.Finance;

namespace THMS.UI.WinForms.Controls
{
    public class BillsControl : UserControl
    {
        private readonly BillsOrchestrator _orchestrator;
        private readonly BindingSource _source = new();
        private readonly DataGridView _grid = new();
        private readonly Label _lblCash = new();
        private readonly Label _lblStatus = new();
        private Guid? _accountId;
        private bool _suppressCash;

        public event EventHandler<Guid?>? AddStatementClicked;
        public event EventHandler? DataChanged;

        public BillsControl()
            : this(new BillsOrchestrator())
        {
        }

        public BillsControl(BillsOrchestrator orchestrator)
        {
            _orchestrator = orchestrator;
            InitializeLayout();
            ConfigureGrid();
            Reload();
        }

        public void SelectAccount(Guid? accountId)
        {
            _accountId = accountId;
            Reload();
        }

        public void Reload()
        {
            _suppressCash = true;
            try
            {
                var rows = _accountId is Guid accountId
                    ? _orchestrator.GetBills(accountId)
                    : [];
                _source.DataSource = rows;
                RefreshFundingCombo();
                UpdateCashRemaining();
                _lblStatus.Text = _accountId is null
                    ? "Select an account to view bills."
                    : rows.Count == 0
                        ? "No expected activity for this account."
                        : $"{rows.Count} item{(rows.Count == 1 ? "" : "s")}.";
            }
            finally
            {
                _suppressCash = false;
            }
        }

        public void SetStatus(string message) => _lblStatus.Text = message;

        private void InitializeLayout()
        {
            var cashBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(8, 4, 8, 4)
            };
            _lblCash.Dock = DockStyle.Fill;
            _lblCash.Font = new Font(Font.FontFamily, Font.Size + 2f, FontStyle.Bold);
            _lblCash.TextAlign = ContentAlignment.MiddleLeft;
            cashBar.Controls.Add(_lblCash);

            _lblStatus.Dock = DockStyle.Bottom;
            _lblStatus.Height = 24;
            _lblStatus.Padding = new Padding(8, 0, 8, 0);
            _lblStatus.TextAlign = ContentAlignment.MiddleLeft;

            var toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = true,
                Padding = new Padding(8)
            };
            toolbar.Controls.Add(ActionButton("Mark paid at bank", OnMarkPaid));
            toolbar.Controls.Add(ActionButton("Unschedule", OnUnschedule));
            toolbar.Controls.Add(ActionButton("Match import", OnMatchImport));
            toolbar.Controls.Add(ActionButton("Add bill", OnAddBill));
            toolbar.Controls.Add(ActionButton("Add statement", OnAddStatement));

            DataGridViewUtil.EnableDoubleBuffering(_grid);
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.AllowUserToResizeRows = false;
            _grid.AutoGenerateColumns = false;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _grid.Dock = DockStyle.Fill;
            _grid.MultiSelect = false;
            _grid.Name = "gridBills";
            _grid.RowHeadersVisible = false;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.DataSource = _source;
            _grid.CurrentCellDirtyStateChanged += OnDirtyStateChanged;
            _grid.CellValueChanged += (_, _) => UpdateCashRemaining();
            _grid.CellContentClick += OnOtherAccountClicked;
            _grid.CellFormatting += OnBillCellFormatting;
            _grid.CellBeginEdit += OnPayFromBeginEdit;
            _grid.DataError += (_, e) => e.ThrowException = false;

            Controls.Add(_grid);
            Controls.Add(toolbar);
            Controls.Add(_lblStatus);
            Controls.Add(cashBar);
        }

        private void ConfigureGrid()
        {
            _grid.Columns.Clear();
            _grid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                DataPropertyName = nameof(BillRow.Pay),
                HeaderText = "Pay",
                Name = nameof(BillRow.Pay),
                Width = 50,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
            });
            _grid.Columns.Add(new CalendarColumn
            {
                DataPropertyName = nameof(BillRow.DueDate),
                HeaderText = "Due",
                Name = nameof(BillRow.DueDate),
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
            });
            var notes = new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(BillRow.Notes),
                HeaderText = "Notes",
                Name = nameof(BillRow.Notes),
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                ToolTipText = "Optional memo. Recurring bills and transfers use the rule description (for example Comcast or HSA)."
            };
            _grid.Columns.Add(notes);
            var amount = TextColumn(nameof(BillRow.Amount), "Amount", readOnly: false);
            amount.DefaultCellStyle.Format = "c2";
            amount.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            amount.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            _grid.Columns.Add(amount);
            _grid.Columns.Add(new DataGridViewLinkColumn
            {
                DataPropertyName = nameof(BillRow.OtherAccountName),
                HeaderText = "Other account",
                Name = nameof(BillRow.OtherAccountName),
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
                LinkBehavior = LinkBehavior.HoverUnderline,
                TrackVisitedState = false,
                SortMode = DataGridViewColumnSortMode.Automatic
            });
            _grid.Columns.Add(new DataGridViewComboBoxColumn
            {
                DataPropertyName = nameof(BillRow.FundingAccountId),
                HeaderText = "Pay from",
                Name = nameof(BillRow.FundingAccountId),
                DisplayMember = nameof(PayFromChoice.Name),
                ValueMember = nameof(PayFromChoice.Id),
                ValueType = typeof(Guid),
                FlatStyle = FlatStyle.Flat,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
            });
            _grid.Columns.Add(TextColumn(nameof(BillRow.Status), "Status", readOnly: true));
            _grid.Columns.Add(TextColumn(nameof(BillRow.Kind), "Kind", readOnly: true));
        }

        private static DataGridViewTextBoxColumn TextColumn(string property, string header, bool readOnly) =>
            new()
            {
                DataPropertyName = property,
                HeaderText = header,
                Name = property,
                ReadOnly = readOnly,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
            };

        private static ThmsButton ActionButton(string text, EventHandler onClick)
        {
            var button = new ThmsButton { Text = text, Margin = new Padding(0, 4, 8, 4) };
            button.Click += onClick;
            return button;
        }

        private void RefreshFundingCombo()
        {
            if (_grid.Columns[nameof(BillRow.FundingAccountId)] is not DataGridViewComboBoxColumn column)
                return;
            column.DataSource = _orchestrator.GetPayFromChoices().ToList();
        }

        private void OnDirtyStateChanged(object? sender, EventArgs e)
        {
            if (_grid.IsCurrentCellDirty)
                _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void UpdateCashRemaining()
        {
            if (_suppressCash)
                return;
            EndEdit();
            var remaining = _orchestrator.CashRemaining(CurrentRows());
            _lblCash.Text = $"Checking remaining: {remaining:c2}";
            _lblCash.ForeColor = remaining < 0 ? Color.Firebrick : Color.FromArgb(32, 32, 32);
        }

        private void OnOtherAccountClicked(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;
            if (_grid.Columns[e.ColumnIndex].Name != nameof(BillRow.OtherAccountName))
                return;
            if (_grid.Rows[e.RowIndex].DataBoundItem is not BillRow row)
                return;

            OpenWebsite(row.OtherWebsiteUrl);
        }

        private void OnBillCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;
            if (_grid.Rows[e.RowIndex].DataBoundItem is not BillRow row)
                return;
            var name = _grid.Columns[e.ColumnIndex].Name;
            if (name == nameof(BillRow.OtherAccountName)
                && _grid.Rows[e.RowIndex].Cells[e.ColumnIndex] is DataGridViewLinkCell link)
            {
                if (string.IsNullOrWhiteSpace(row.OtherWebsiteUrl))
                {
                    link.LinkBehavior = LinkBehavior.NeverUnderline;
                    link.LinkColor = _grid.DefaultCellStyle.ForeColor;
                    link.ActiveLinkColor = _grid.DefaultCellStyle.ForeColor;
                    link.VisitedLinkColor = _grid.DefaultCellStyle.ForeColor;
                }
                else
                {
                    link.LinkBehavior = LinkBehavior.HoverUnderline;
                    link.LinkColor = Color.FromArgb(0, 99, 177);
                    link.ActiveLinkColor = Color.FromArgb(0, 70, 127);
                    link.VisitedLinkColor = Color.FromArgb(0, 99, 177);
                }
            }

            if (name == nameof(BillRow.FundingAccountId) && !row.CanChoosePayFrom)
            {
                e.Value = "";
                e.FormattingApplied = true;
            }
        }

        private void OnPayFromBeginEdit(object? sender, DataGridViewCellCancelEventArgs e)
        {
            if (e.RowIndex < 0)
                return;
            if (_grid.Columns[e.ColumnIndex].Name != nameof(BillRow.FundingAccountId))
                return;
            if (_grid.Rows[e.RowIndex].DataBoundItem is not BillRow row || row.CanChoosePayFrom)
                return;
            e.Cancel = true;
        }

        private static void OpenWebsite(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                if (!Uri.TryCreate("https://" + url.Trim(), UriKind.Absolute, out uri))
                    return;
            }

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                return;

            Process.Start(new ProcessStartInfo
            {
                FileName = uri.ToString(),
                UseShellExecute = true
            });
        }

        private void OnMarkPaid(object? sender, EventArgs e)
        {
            EndEdit();
            try
            {
                var scheduled = _orchestrator.Schedule(CurrentRows());
                Reload();
                _lblStatus.Text = scheduled.Count == 0
                    ? "Check Pay on the bills you paid at the bank, then mark them paid."
                    : $"Marked {scheduled.Count} bill{(scheduled.Count == 1 ? "" : "s")} as scheduled. Cash is held until import matches the bank actual.";
                DataChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show(FindForm(), ex.Message, "Mark paid at bank",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OnUnschedule(object? sender, EventArgs e)
        {
            if (_grid.CurrentRow?.DataBoundItem is not BillRow row || row.IntentId is not Guid intentId)
            {
                MessageBox.Show(FindForm(), "Select a scheduled bill to unschedule.", "Unschedule",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                _orchestrator.Unschedule(intentId);
                Reload();
                DataChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show(FindForm(), ex.Message, "Unschedule",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OnMatchImport(object? sender, EventArgs e)
        {
            if (_accountId is not Guid accountId)
            {
                MessageBox.Show(FindForm(), "Select an account first.", "Match import",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (_grid.CurrentRow?.DataBoundItem is not BillRow row)
            {
                MessageBox.Show(FindForm(), "Select a bill to match.", "Match import",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var imported = _orchestrator.GetUnreconciledImports(accountId);
            if (imported.Count == 0)
            {
                MessageBox.Show(FindForm(), "There are no unreconciled imported transactions for this account.",
                    "Match import", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var recommended = imported.FirstOrDefault(item =>
                    row.IntentId is Guid expectedId && item.RecommendedExpectedId == expectedId)
                ?.Id;
            using var dialog = new MatchImportedDialog(row, imported, recommended);
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK || dialog.SelectedImportedId is not Guid importedId)
                return;

            try
            {
                _orchestrator.MatchToImported(row, importedId);
                Reload();
                DataChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show(FindForm(), ex.Message, "Match import",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OnAddBill(object? sender, EventArgs e)
        {
            using var dialog = new AddManualBillDialog(
                _orchestrator.GetAccounts(),
                _orchestrator.GetFundingAccounts(),
                _accountId);
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            try
            {
                _orchestrator.AddManual(
                    dialog.DestinationAccountId,
                    dialog.FundingAccountId,
                    dialog.Amount,
                    dialog.PayDate,
                    dialog.Notes);
                Reload();
                DataChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show(FindForm(), ex.Message, "Add bill",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OnAddStatement(object? sender, EventArgs e)
        {
            var accountId = _accountId
                ?? (_grid.CurrentRow?.DataBoundItem is BillRow row ? row.DestinationAccountId : (Guid?)null);
            AddStatementClicked?.Invoke(this, accountId);
        }

        private List<BillRow> CurrentRows()
        {
            EndEdit();
            return _source.List.Cast<BillRow>().ToList();
        }

        private void EndEdit()
        {
            _grid.EndEdit();
            _source.EndEdit();
        }
    }

    internal sealed class AddManualBillDialog : Form
    {
        private readonly ComboBox _cboDestination = new();
        private readonly ComboBox _cboFunding = new();
        private readonly NumericUpDown _amount = new();
        private readonly DateTimePicker _date = new();
        private readonly TextBox _notes = new();

        public Guid DestinationAccountId { get; private set; }
        public Guid FundingAccountId { get; private set; }
        public decimal Amount { get; private set; }
        public DateTime PayDate { get; private set; }
        public string Notes { get; private set; } = "";

        public AddManualBillDialog(
            IReadOnlyList<Account> destinations,
            IReadOnlyList<Account> funding,
            Guid? selectedAccountId = null)
        {
            Text = "Add bill";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ClientSize = new Size(420, 296);
            var y = 16;
            Controls.Add(LabelAt("Account", 16, y));
            BindCombo(_cboDestination, destinations, 140, y);
            y += 40;
            Controls.Add(LabelAt("Pay from", 16, y));
            BindCombo(_cboFunding, funding, 140, y);
            if (selectedAccountId is Guid id)
            {
                _cboDestination.SelectedValue = id;
                if (funding.Any(a => a.Id == id))
                    _cboFunding.SelectedValue = id;
            }
            y += 40;
            Controls.Add(LabelAt("Amount", 16, y));
            _amount.Left = 140;
            _amount.Top = y;
            _amount.Width = 240;
            _amount.DecimalPlaces = 2;
            _amount.Maximum = 1_000_000;
            _amount.Minimum = 0.01m;
            Controls.Add(_amount);
            y += 40;
            Controls.Add(LabelAt("Due date", 16, y));
            _date.Left = 140;
            _date.Top = y;
            _date.Width = 240;
            _date.Format = DateTimePickerFormat.Short;
            _date.Value = DateTime.Today;
            Controls.Add(_date);
            y += 40;
            Controls.Add(LabelAt("What", 16, y));
            _notes.Left = 140;
            _notes.Top = y;
            _notes.Width = 240;
            Controls.Add(_notes);

            var save = new ThmsButton { Text = "Save", Left = 140, Top = 220 };
            save.Click += OnSave;
            var cancel = new ThmsButton { Text = "Cancel", Left = 260, Top = 220 };
            cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(save);
            Controls.Add(cancel);
            AcceptButton = save;
            CancelButton = cancel;
        }

        private void BindCombo(ComboBox combo, IReadOnlyList<Account> accounts, int left, int top)
        {
            combo.Left = left;
            combo.Top = top;
            combo.Width = 240;
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.DisplayMember = nameof(Account.Name);
            combo.ValueMember = nameof(Account.Id);
            combo.DataSource = accounts.ToList();
            Controls.Add(combo);
        }

        private static Label LabelAt(string text, int left, int top) =>
            new() { Text = text, Left = left, Top = top + 4, AutoSize = true };

        private void OnSave(object? sender, EventArgs e)
        {
            if (_cboDestination.SelectedItem is not Account destination)
            {
                MessageBox.Show(this, "Select an account.", "Add bill", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_cboFunding.SelectedItem is not Account funding)
            {
                MessageBox.Show(this, "Select a pay-from account.", "Add bill", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DestinationAccountId = destination.Id;
            FundingAccountId = funding.Id;
            Amount = _amount.Value;
            PayDate = _date.Value.Date;
            Notes = _notes.Text;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
