using System.ComponentModel;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Transactions;

namespace THMS.UI.WinForms.Controls
{
    public class SplitTransactionEditor : Form
    {
        private readonly decimal _parentAmount;
        private readonly IReadOnlyList<ExpenseCategory> _categories = [];
        private readonly IReadOnlyList<Account> _accounts = [];
        private readonly BindingList<SplitRowEdit> _rows = [];
        private readonly DataGridView _grid = new();
        private readonly Label _lblParent = new();
        private readonly Label _lblSummary = new();

        public List<SplitTransactionRow> Result { get; private set; } = [];

        public SplitTransactionEditor()
        {
            InitializeLayout("Transaction", 0);
        }

        public SplitTransactionEditor(
            string parentDescription,
            decimal parentAmount,
            IEnumerable<SplitTransactionRow> existing,
            IReadOnlyList<ExpenseCategory> categories,
            IReadOnlyList<Account> accounts)
        {
            _parentAmount = parentAmount;
            _categories = categories;
            _accounts = accounts;
            InitializeLayout(parentDescription, parentAmount);

            var seed = existing.ToList();
            if (seed.Count == 0)
            {
                _rows.Add(NewRow(parentAmount, InferType(parentAmount)));
            }
            else
            {
                foreach (var split in seed)
                    _rows.Add(ToEdit(split));
            }

            UpdateSummary();
        }

        public List<SplitTransactionRow> ToSplitRows() =>
            _rows.Select(FromEdit).ToList();

        private void InitializeLayout(string parentDescription, decimal parentAmount)
        {
            Text = "Split Transaction";
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = false;
            MaximizeBox = true;
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(860, 420);
            MinimumSize = new Size(720, 320);

            _lblParent.AutoEllipsis = true;
            _lblParent.Dock = DockStyle.Fill;
            _lblParent.Text = $"{parentDescription}  •  {parentAmount:c2}";
            _lblParent.TextAlign = ContentAlignment.MiddleLeft;

            _lblSummary.AutoSize = true;
            _lblSummary.Anchor = AnchorStyles.Right;
            _lblSummary.Padding = new Padding(12, 0, 0, 0);
            _lblSummary.TextAlign = ContentAlignment.MiddleRight;

            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 36,
                ColumnCount = 2,
                Padding = new Padding(12, 6, 12, 0)
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header.Controls.Add(_lblParent, 0, 0);
            header.Controls.Add(_lblSummary, 1, 0);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(12, 12, 12, 16),
                WrapContents = true
            };
            var btnCancel = new ThmsButton { Text = "Cancel", DialogResult = DialogResult.Cancel };
            var btnOk = new ThmsButton { Text = "OK" };
            var btnBalance = new ThmsButton { Text = "Auto-Balance" };
            var btnRemove = new ThmsButton { Text = "Remove Split", Destructive = true };
            var btnAdd = new ThmsButton { Text = "Add Split" };
            btnOk.Click += (_, _) => OnOk();
            btnBalance.Click += (_, _) => AutoBalance();
            btnRemove.Click += (_, _) => RemoveSelected();
            btnAdd.Click += (_, _) => AddRow();
            buttons.Controls.Add(btnCancel);
            buttons.Controls.Add(btnRemove);
            buttons.Controls.Add(btnOk);
            buttons.Controls.Add(btnBalance);
            buttons.Controls.Add(btnAdd);

            ConfigureGrid();

            Controls.Add(_grid);
            Controls.Add(buttons);
            Controls.Add(header);
            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        private void ConfigureGrid()
        {
            _grid.Dock = DockStyle.Fill;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.AutoGenerateColumns = false;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _grid.RowHeadersVisible = false;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.MultiSelect = false;
            _grid.DataSource = _rows;

            var amount = new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(SplitRowEdit.Amount),
                HeaderText = "Amount",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
            };
            amount.DefaultCellStyle.Format = "c2";

            var categories = new List<CategoryOption> { new("(none)", Guid.Empty) };
            categories.AddRange(_categories.OrderBy(c => c.Name).Select(c => new CategoryOption(c.Name, c.Id)));
            var category = new DataGridViewComboBoxColumn
            {
                DataPropertyName = nameof(SplitRowEdit.CategoryId),
                HeaderText = "Category",
                DisplayMember = nameof(CategoryOption.Name),
                ValueMember = nameof(CategoryOption.Id),
                DataSource = categories,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FlatStyle = FlatStyle.Flat
            };

            var type = new DataGridViewComboBoxColumn
            {
                DataPropertyName = nameof(SplitRowEdit.Type),
                HeaderText = "Type",
                DataSource = Enum.GetValues<SplitType>(),
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 120,
                FlatStyle = FlatStyle.Flat
            };

            var accounts = new List<AccountOption> { new("(none)", Guid.Empty) };
            accounts.AddRange(_accounts.OrderBy(a => a.Name).Select(a => new AccountOption(a.Name, a.Id)));
            var transfer = new DataGridViewComboBoxColumn
            {
                DataPropertyName = nameof(SplitRowEdit.TransferAccountId),
                HeaderText = "Transfer Account",
                DisplayMember = nameof(AccountOption.Name),
                ValueMember = nameof(AccountOption.Id),
                DataSource = accounts,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FlatStyle = FlatStyle.Flat
            };

            var notes = new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(SplitRowEdit.Notes),
                HeaderText = "Notes",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            };

            _grid.Columns.AddRange(amount, category, type, transfer, notes);
            _grid.DataError += (_, e) =>
            {
                e.ThrowException = false;
            };
            _grid.CellValueChanged += (_, _) => UpdateSummary();
            _grid.CurrentCellDirtyStateChanged += (_, _) =>
            {
                if (!_grid.IsCurrentCellDirty)
                    return;
                if (_grid.CurrentCell is DataGridViewComboBoxCell)
                    _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            _rows.ListChanged += (_, _) => UpdateSummary();
        }

        private void AddRow()
        {
            _rows.Add(NewRow(SplitTransactionMath.Remaining(_parentAmount, _rows.Select(FromEdit)), InferType(_parentAmount)));
        }

        private void RemoveSelected()
        {
            if (_grid.CurrentRow?.DataBoundItem is not SplitRowEdit row)
                return;
            _rows.Remove(row);
            if (_rows.Count == 0)
                AddRow();
        }

        private void AutoBalance()
        {
            if (_rows.Count == 0)
                AddRow();

            var target = _grid.CurrentRow?.DataBoundItem as SplitRowEdit ?? _rows[^1];
            var others = _rows.Where(r => r != target).Select(FromEdit);
            target.Amount = SplitTransactionMath.Remaining(_parentAmount, others);
            _rows.ResetItem(_rows.IndexOf(target));
            UpdateSummary();
        }

        private void OnOk()
        {
            _grid.EndEdit();
            try
            {
                var splits = ToSplitRows();
                SplitTransactionValidator.Validate(_parentAmount, splits);
                Result = splits;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(this, ex.Message, "Split Transaction", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void UpdateSummary()
        {
            var sum = _rows.Sum(r => r.Amount);
            var remaining = _parentAmount - sum;
            _lblSummary.Text = $"Sum {sum:c2}  •  Remaining {remaining:c2}";
            _lblSummary.ForeColor = SplitTransactionMath.AmountsMatch(_parentAmount, _rows.Select(FromEdit))
                ? SystemColors.ControlText
                : Color.Firebrick;
        }

        private static SplitType InferType(decimal amount) =>
            amount >= 0 ? SplitType.Income : SplitType.Expense;

        private static SplitRowEdit NewRow(decimal amount, SplitType type) =>
            new()
            {
                Id = Guid.NewGuid(),
                Amount = amount,
                Type = type
            };

        private static SplitRowEdit ToEdit(SplitTransactionRow split) =>
            new()
            {
                Id = split.Id == Guid.Empty ? Guid.NewGuid() : split.Id,
                Amount = split.Amount,
                CategoryId = split.CategoryId ?? Guid.Empty,
                Type = split.Type,
                TransferAccountId = split.TransferAccountId ?? Guid.Empty,
                Notes = split.Notes ?? ""
            };

        private SplitTransactionRow FromEdit(SplitRowEdit row)
        {
            var category = _categories.FirstOrDefault(c => c.Id == row.CategoryId);
            return new SplitTransactionRow
            {
                Id = row.Id == Guid.Empty ? Guid.NewGuid() : row.Id,
                Amount = row.Amount,
                CategoryId = row.CategoryId == Guid.Empty ? null : row.CategoryId,
                Category = category?.Name,
                Type = row.Type,
                TransferAccountId = row.TransferAccountId == Guid.Empty ? null : row.TransferAccountId,
                Notes = string.IsNullOrWhiteSpace(row.Notes) ? null : row.Notes.Trim()
            };
        }

        private sealed class SplitRowEdit
        {
            public Guid Id { get; set; }
            public decimal Amount { get; set; }
            public Guid CategoryId { get; set; }
            public SplitType Type { get; set; }
            public Guid TransferAccountId { get; set; }
            public string Notes { get; set; } = "";
        }

        private sealed record CategoryOption(string Name, Guid Id);
        private sealed record AccountOption(string Name, Guid Id);
    }
}
