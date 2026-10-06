using System.Diagnostics;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Planning;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Orchestrators;
using THMS.Logic.Orchestrators.Finance;
using THMS.Logic.ViewModels.Finance;

namespace THMS.UI.WinForms.Controls
{
    public class BillsControl : UserControl
    {
        private readonly BillsOrchestrator _orchestrator;
        private readonly RecurringRuleOrchestrator _rules = new();
        private readonly BindingSource _source = new();
        private readonly DataGridView _grid = new();
        private readonly Label _lblCash = new();
        private readonly ContextMenuStrip _menu = new();
        private ThmsButton _delete = null!;
        private Guid? _accountId;
        private bool _suppressCash;

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
            }
            finally
            {
                _suppressCash = false;
            }

            UpdateCashRemaining();
            UpdateDelete();
        }

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

            var toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = true,
                Padding = new Padding(8)
            };
            toolbar.Controls.Add(ActionButton("Match import", OnMatchImport));
            toolbar.Controls.Add(ActionButton("Add", OnAddBill));
            toolbar.Controls.Add(ActionButton("Projection", OnProjection));
            _delete = ActionButton("Delete", OnDelete);
            _delete.Destructive = true;
            _delete.Enabled = false;
            toolbar.Controls.Add(_delete);

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
            _grid.SelectionChanged += (_, _) => UpdateDelete();
            _grid.CellDoubleClick += OnBillDoubleClick;
            _grid.MouseDown += OnGridMouseDown;
            _grid.ContextMenuStrip = _menu;
            _menu.Opening += OnMenuOpening;

            Controls.Add(_grid);
            Controls.Add(toolbar);
            Controls.Add(cashBar);
        }

        private void ConfigureGrid()
        {
            _grid.Columns.Clear();
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
                ToolTipText = "Optional memo. Recurring bills use this description (for example Comcast or HSA)."
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
            var remaining = _orchestrator.CashRemaining(_accountId, CurrentRows());
            _lblCash.Text = $"{CashLabel()} remaining: {remaining:c2}";
            _lblCash.ForeColor = remaining < 0 ? Color.Firebrick : Color.FromArgb(32, 32, 32);
        }

        private void OnGridMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            var hit = _grid.HitTest(e.X, e.Y);
            if (hit.RowIndex < 0)
                return;

            _grid.ClearSelection();
            _grid.Rows[hit.RowIndex].Selected = true;
            var cell = _grid.Rows[hit.RowIndex].Cells.Cast<DataGridViewCell>()
                .FirstOrDefault(c => c.Visible);
            if (cell is not null)
                _grid.CurrentCell = cell;
        }

        private void OnMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _menu.Items.Clear();
            if (_grid.CurrentRow?.DataBoundItem is not BillRow row || row.DestinationAccountId == Guid.Empty)
            {
                e.Cancel = true;
                return;
            }

            var scheduled = row.Status == BillStatuses.Scheduled;
            var pay = new ToolStripMenuItem("Pay");
            pay.Enabled = !scheduled;
            pay.Click += (_, _) => OnPay(row);
            _menu.Items.Add(pay);

            var unschedule = new ToolStripMenuItem("Unschedule");
            unschedule.Enabled = scheduled && row.IntentId is Guid;
            unschedule.Click += (_, _) => OnUnschedule(row);
            _menu.Items.Add(unschedule);
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
                DataGridViewUtil.ApplyLinkAppearance(
                    link,
                    e.CellStyle,
                    !string.IsNullOrWhiteSpace(row.OtherWebsiteUrl),
                    _grid.DefaultCellStyle.ForeColor);
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

        private void OnPay(BillRow row)
        {
            EndEdit();
            try
            {
                row.Pay = true;
                _orchestrator.Schedule([row]);
                Reload();
                DataChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show(FindForm(), ex.Message, "Pay",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OnUnschedule(BillRow row)
        {
            if (row.IntentId is not Guid intentId)
                return;

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

        private void OnAddBill(object? sender, EventArgs e) =>
            ShowBillDialog(existing: null);

        private void OnProjection(object? sender, EventArgs e)
        {
            if (_accountId is not Guid accountId)
            {
                MessageBox.Show(FindForm(), "Select an account first.", "Projection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var account = _orchestrator.GetAccounts().FirstOrDefault(item => item.Id == accountId);
            var projection = _orchestrator.GetProjection(accountId);
            using var dialog = new BalanceProjectionDialog(account?.Name ?? "Account", projection);
            dialog.ShowDialog(FindForm());
        }

        private void OnBillDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;
            var column = _grid.Columns[e.ColumnIndex].Name;
            if (column == nameof(BillRow.FundingAccountId))
                return;
            if (_grid.Rows[e.RowIndex].DataBoundItem is not BillRow row)
                return;

            _grid.EndEdit();
            var draft = _orchestrator.LoadBill(row);
            if (draft is null)
                return;
            ShowBillDialog(draft);
        }

        private void ShowBillDialog(BillDraft? existing)
        {
            using var dialog = new AddBillDialog(
                _orchestrator.GetAccounts(),
                _orchestrator.GetCategories(),
                existing?.BillAccountId ?? _accountId,
                existing);
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            try
            {
                if (existing is null)
                {
                    _orchestrator.AddEnteredBill(
                        dialog.BillAccountId,
                        dialog.Description,
                        dialog.Amount,
                        dialog.BillCategoryId,
                        dialog.Date,
                        dialog.CreditAccountId,
                        dialog.CreditCategoryId,
                        dialog.Frequency);
                }
                else
                {
                    _orchestrator.SaveEditedBill(
                        existing,
                        dialog.BillAccountId,
                        dialog.Description,
                        dialog.Amount,
                        dialog.BillCategoryId,
                        dialog.Date,
                        dialog.CreditAccountId,
                        dialog.CreditCategoryId,
                        dialog.Frequency);
                }

                Reload();
                DataChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show(FindForm(), ex.Message, "Bill Details",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OnDelete(object? sender, EventArgs e)
        {
            if (SelectedDeletable() is not BillRow row)
                return;

            var name = string.IsNullOrWhiteSpace(row.Notes) ? row.Kind : row.Notes;
            if (MessageBox.Show(
                    FindForm(),
                    $"Delete '{name}'?",
                    "Delete",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            if (row.Source == PaymentIntentSource.RecurringSingle && row.SourceId is Guid singleId)
                _rules.DeleteSingleRule(singleId);
            else if (row.Source == PaymentIntentSource.RecurringTransfer && row.SourceId is Guid transferRuleId)
                _rules.DeleteTransferRule(transferRuleId);
            else if (row.IntentId is Guid expectedId)
                _orchestrator.DeleteEnteredBill(expectedId);

            Reload();
            DataChanged?.Invoke(this, EventArgs.Empty);
        }

        private BillRow? SelectedDeletable() =>
            _grid.CurrentRow?.DataBoundItem is BillRow row && CanDelete(row) ? row : null;

        private static bool CanDelete(BillRow row) =>
            (row.Source is PaymentIntentSource.RecurringSingle or PaymentIntentSource.RecurringTransfer
                && row.SourceId is Guid)
            || (row.Source == PaymentIntentSource.Manual && row.IntentId is Guid);

        private void UpdateDelete() =>
            _delete.Enabled = SelectedDeletable() is not null;

        private string CashLabel()
        {
            if (_accountId is Guid id)
            {
                var account = _orchestrator.GetAccounts().FirstOrDefault(a => a.Id == id);
                if (account is BankAccount)
                    return account.Name;
            }

            return "Bank";
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

    internal sealed class AddBillDialog : Form
    {
        private const string OneTime = "One Time";

        private readonly ComboBox _billAccount = new();
        private readonly TextBox _description = new();
        private readonly NumericUpDown _amount = new();
        private readonly ComboBox _billCategory = new();
        private readonly DateTimePicker _date = new();
        private readonly ComboBox _creditAccount = new();
        private readonly Label _creditCategoryLabel;
        private readonly ComboBox _creditCategory = new();
        private readonly ComboBox _frequency = new();
        private readonly ThmsButton _save = new() { Text = "Save" };
        private readonly ThmsButton _cancel = new() { Text = "Cancel" };

        private readonly Guid? _billAccountId;
        private readonly Guid? _billCategoryId;
        private readonly Guid? _creditAccountId;
        private readonly Guid? _creditCategoryId;

        public Guid BillAccountId { get; private set; }
        public string Description { get; private set; } = "";
        public decimal Amount { get; private set; }
        public Guid BillCategoryId { get; private set; }
        public DateTime Date { get; private set; }
        public Guid? CreditAccountId { get; private set; }
        public Guid? CreditCategoryId { get; private set; }
        public RecurrenceFrequency? Frequency { get; private set; }

        public AddBillDialog(
            IReadOnlyList<Account> accounts,
            IReadOnlyList<ExpenseCategory> categories,
            Guid? selectedAccountId = null,
            BillDraft? existing = null)
        {
            Text = "Bill Details";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.None;
            AutoSize = false;
            AutoScroll = false;

            var ordered = accounts.OrderBy(a => a.Name).ToList();
            var categoryList = categories.OrderBy(c => c.Name).ToList();
            _billAccountId = existing?.BillAccountId is Guid billId && billId != Guid.Empty
                ? billId
                : selectedAccountId;
            _billCategoryId = existing?.BillCategoryId;
            _creditAccountId = existing?.CreditAccountId;
            _creditCategoryId = existing?.CreditCategoryId;
            BindAccounts(_billAccount, ordered);
            BindAccounts(_creditAccount, BlankFirst(ordered));
            BindCategories(_billCategory, categoryList);
            BindCategories(_creditCategory, categoryList);

            _description.Width = 280;
            _amount.Width = 280;
            _amount.DecimalPlaces = 2;
            _amount.Minimum = -1_000_000;
            _amount.Maximum = 1_000_000;
            _date.Format = DateTimePickerFormat.Short;
            _date.Value = DateTime.Today;
            _frequency.Width = 280;
            _frequency.DropDownStyle = ComboBoxStyle.DropDownList;
            _frequency.Items.AddRange([OneTime, "Weekly", "Biweekly", "Monthly", "Quarterly", "Yearly"]);
            _frequency.SelectedIndex = 0;

            _creditCategoryLabel = LabelAt("Credit Category");
            _creditCategory.Visible = false;
            _creditCategoryLabel.Visible = false;

            AddRow(LabelAt("Account to Bill"), _billAccount);
            AddRow(LabelAt("Description"), _description);
            AddRow(LabelAt("Amount"), _amount);
            AddRow(LabelAt("Bill Category"), _billCategory);
            AddRow(LabelAt("Date"), _date);
            AddRow(LabelAt("Account to Credit"), _creditAccount);
            AddRow(_creditCategoryLabel, _creditCategory);
            AddRow(LabelAt("Frequency"), _frequency);
            _save.Click += OnSave;
            _cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(_save);
            Controls.Add(_cancel);
            AcceptButton = _save;
            CancelButton = _cancel;
            if (existing is not null)
                ApplyDraft(existing);
            ApplySelections();
            LayoutFields();
            _creditAccount.SelectedIndexChanged += (_, _) => ShowCreditCategory(HasCreditAccount());
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ApplySelections();
            LayoutFields();
            BeginInvoke(new Action(ApplySelections));
        }

        private void ApplySelections()
        {
            SelectAccount(_billAccount, _billAccountId);
            SelectCategory(_billCategory, _billCategoryId);
            SelectAccount(_creditAccount, _creditAccountId);
            if (HasCreditAccount())
                SelectCategory(_creditCategory, _creditCategoryId);
        }

        private readonly List<(Label Label, Control Editor)> _rows = [];

        private void AddRow(Label label, Control editor)
        {
            _rows.Add((label, editor));
            Controls.Add(label);
            Controls.Add(editor);
        }

        private void ShowCreditCategory(bool show)
        {
            _creditCategoryLabel.Visible = show;
            _creditCategory.Visible = show;
            LayoutFields();
        }

        private bool HasCreditAccount() =>
            _creditAccount.SelectedItem is Account account && account.Id != Guid.Empty;

        private void ApplyDraft(BillDraft draft)
        {
            _description.Text = draft.Description;
            var amount = draft.Amount;
            if (amount < _amount.Minimum)
                amount = _amount.Minimum;
            if (amount > _amount.Maximum)
                amount = _amount.Maximum;
            _amount.Value = amount;
            _date.Value = draft.Date.Year > 1 ? draft.Date.Date : DateTime.Today;
            _frequency.SelectedItem = draft.Frequency switch
            {
                RecurrenceFrequency.Weekly => "Weekly",
                RecurrenceFrequency.BiWeekly => "Biweekly",
                RecurrenceFrequency.Monthly => "Monthly",
                RecurrenceFrequency.Quarterly => "Quarterly",
                RecurrenceFrequency.Yearly => "Yearly",
                _ => OneTime
            };
        }

        private static void SelectAccount(ComboBox combo, Guid? id)
        {
            if (id is not Guid accountId || combo.DataSource is not IList<Account> accounts)
                return;

            for (var i = 0; i < accounts.Count; i++)
            {
                if (accounts[i].Id != accountId)
                    continue;
                combo.SelectedIndex = i;
                return;
            }
        }

        private static void SelectCategory(ComboBox combo, Guid? id)
        {
            if (combo.DataSource is not IList<ExpenseCategory> categories || categories.Count == 0)
                return;

            var index = -1;
            if (id is Guid categoryId)
            {
                for (var i = 0; i < categories.Count; i++)
                {
                    if (categories[i].Id != categoryId)
                        continue;
                    index = i;
                    break;
                }
            }

            if (index < 0)
            {
                for (var i = 0; i < categories.Count; i++)
                {
                    if (categories[i].Id != DefaultExpenseCategories.UncategorizedId)
                        continue;
                    index = i;
                    break;
                }
            }

            combo.SelectedIndex = index >= 0 ? index : 0;
        }

        private void LayoutFields()
        {
            const int labelLeft = 16;
            const int editorLeft = 190;
            const int editorWidth = 340;
            const int rowHeight = 52;
            var y = 24;
            foreach (var (label, editor) in _rows)
            {
                var show = editor != _creditCategory || HasCreditAccount();
                label.Visible = show;
                editor.Visible = show;
                if (!show)
                    continue;
                label.Left = labelLeft;
                label.Top = y + 8;
                editor.Left = editorLeft;
                editor.Top = y;
                editor.Width = editorWidth;
                editor.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                y += rowHeight;
            }

            y += 16;
            var buttonHeight = Math.Max(40, Math.Max(_save.Height, _save.PreferredSize.Height));
            _cancel.Top = y;
            _save.Top = y;
            _cancel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            _save.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            var cancelWidth = Math.Max(110, Math.Max(_cancel.Width, _cancel.PreferredSize.Width));
            var saveWidth = Math.Max(110, Math.Max(_save.Width, _save.PreferredSize.Width));
            _cancel.Left = editorLeft + editorWidth - cancelWidth;
            _save.Left = _cancel.Left - saveWidth - 12;
            var outer = SizeFromClientSize(new Size(editorLeft + editorWidth + 24, y + buttonHeight + 24));
            outer = new Size(Math.Max(640, outer.Width), Math.Max(560, outer.Height));
            MinimumSize = outer;
            Size = outer;
        }

        private static Label LabelAt(string text) =>
            new() { Text = text, AutoSize = true };

        private static void BindAccounts(ComboBox combo, IReadOnlyList<Account> accounts)
        {
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.DisplayMember = nameof(Account.Name);
            combo.ValueMember = nameof(Account.Id);
            combo.DataSource = accounts.ToList();
        }

        private static void BindCategories(ComboBox combo, List<ExpenseCategory> categories)
        {
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.DisplayMember = nameof(ExpenseCategory.Name);
            combo.ValueMember = nameof(ExpenseCategory.Id);
            combo.DataSource = categories.ToList();
        }

        private static List<Account> BlankFirst(IReadOnlyList<Account> accounts)
        {
            var list = new List<Account> { new BankAccount { Id = Guid.Empty, Name = "" } };
            list.AddRange(accounts);
            return list;
        }

        private void OnSave(object? sender, EventArgs e)
        {
            if (_billAccount.SelectedItem is not Account bill || bill.Id == Guid.Empty)
            {
                MessageBox.Show(this, "Select an account to bill.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(_description.Text))
            {
                MessageBox.Show(this, "Description is required.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_amount.Value == 0)
            {
                MessageBox.Show(this, "Amount cannot be zero.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_billCategory.SelectedItem is not ExpenseCategory billCategory)
            {
                MessageBox.Show(this, "Select a bill category.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Guid? creditAccountId = null;
            Guid? creditCategoryId = null;
            if (HasCreditAccount() && _creditAccount.SelectedItem is Account credit)
            {
                if (credit.Id == bill.Id)
                {
                    MessageBox.Show(this, "Account to credit must be a different account.", Text,
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (_creditCategory.SelectedItem is not ExpenseCategory creditCategory)
                {
                    MessageBox.Show(this, "Select a credit category.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                creditAccountId = credit.Id;
                creditCategoryId = creditCategory.Id;
            }

            BillAccountId = bill.Id;
            Description = _description.Text.Trim();
            Amount = _amount.Value;
            BillCategoryId = billCategory.Id;
            Date = _date.Value.Date;
            CreditAccountId = creditAccountId;
            CreditCategoryId = creditCategoryId;
            Frequency = _frequency.SelectedItem?.ToString() switch
            {
                "Weekly" => RecurrenceFrequency.Weekly,
                "Biweekly" => RecurrenceFrequency.BiWeekly,
                "Monthly" => RecurrenceFrequency.Monthly,
                "Quarterly" => RecurrenceFrequency.Quarterly,
                "Yearly" => RecurrenceFrequency.Yearly,
                _ => null
            };
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
