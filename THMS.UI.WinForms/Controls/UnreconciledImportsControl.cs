using THMS.Data.Stores;
using THMS.Logic.Orchestrators.Finance;
using THMS.Logic.ViewModels;
using THMS.Logic.ViewModels.Finance;
using THMS.UI.WinForms;

namespace THMS.UI.WinForms.Controls
{
    internal sealed class UnreconciledImportsControl : UserControl
    {
        private readonly ReconciliationOrchestrator _reconciliation = new();
        private readonly IAccountStatementDataStore _statements = new DataStoreFactory().GetAccountStatementStore();
        private readonly BindingSource _source = new();
        private readonly DataGridView _grid = new();
        private readonly Label _header = new();
        private Guid? _accountId;

        public event EventHandler? Reconciled;

        public int RowCount => _source.Count;

        public UnreconciledImportsControl()
        {
            var headerHost = new Panel { Dock = DockStyle.Top, Height = 36 };
            _header.Dock = DockStyle.Fill;
            _header.Font = new Font(Font, FontStyle.Bold);
            _header.Padding = new Padding(8, 0, 8, 0);
            _header.Text = "Unreconciled";
            _header.TextAlign = ContentAlignment.MiddleLeft;
            headerHost.Controls.Add(_header);

            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.AllowUserToResizeRows = false;
            _grid.AutoGenerateColumns = false;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            _grid.Dock = DockStyle.Fill;
            _grid.MultiSelect = true;
            _grid.Name = "gridUnreconciled";
            _grid.ReadOnly = true;
            _grid.RowHeadersVisible = false;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.ScrollBars = ScrollBars.Both;
            DataGridViewUtil.EnableDoubleBuffering(_grid);
            _grid.Columns.AddRange(DateColumn(), AmountColumn(), TextColumn(nameof(ImportedTransactionView.Description), "Description"), TextColumn(nameof(ImportedTransactionView.RecommendedMatch), "Recommended Match"), TextColumn(nameof(ImportedTransactionView.Status), "Status"));
            _grid.DataError += (_, e) => e.ThrowException = false;
            _grid.Resize += (_, _) => SizeColumns();
            _grid.DataSource = _source;
            _grid.MouseDown += OnMouseDown;

            Controls.Add(_grid);
            Controls.Add(headerHost);
        }

        public void ShowAccount(Guid? accountId)
        {
            _accountId = accountId;
            var rows = accountId is Guid id ? _reconciliation.GetUnreconciled(id) : [];
            _source.DataSource = rows;
            _header.Text = rows.Count == 0 ? "Unreconciled" : $"Unreconciled — {rows.Count}";
        }

        private void OnMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            var hit = _grid.HitTest(e.X, e.Y);
            if (hit.RowIndex >= 0 && !_grid.Rows[hit.RowIndex].Selected)
            {
                _grid.ClearSelection();
                _grid.Rows[hit.RowIndex].Selected = true;
                if (hit.ColumnIndex >= 0)
                    _grid.CurrentCell = _grid[hit.ColumnIndex, hit.RowIndex];
            }

            var location = e.Location;
            BeginInvoke(() => ShowMenu(location));
        }

        private void ShowMenu(Point location)
        {
            if (IsDisposed)
                return;

            var selected = Selected().ToList();
            var menu = new ContextMenuStrip();
            var accept = new ToolStripMenuItem("Accept Match");
            accept.Enabled = selected.Count > 0 && selected.All(row => row.RecommendedExpectedId is Guid);
            accept.Click += (_, _) => AcceptMatches(selected);
            var change = new ToolStripMenuItem("Change Match");
            change.Enabled = selected.Count == 1;
            change.Click += (_, _) => ChangeMatch(selected[0]);
            var asNew = new ToolStripMenuItem("Accept as New");
            asNew.Enabled = selected.Count > 0;
            asNew.Click += (_, _) => AcceptAsNew(selected);
            var before = new ToolStripMenuItem("Accept all unreconciled up to last statement");
            before.Enabled = _accountId is Guid;
            before.Click += (_, _) => AcceptUpToLastStatement();
            menu.Items.Add(accept);
            menu.Items.Add(change);
            menu.Items.Add(asNew);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(before);
            menu.Closed += (_, _) => BeginInvoke(menu.Dispose);
            menu.Show(_grid, location);
        }

        private IEnumerable<ImportedTransactionView> Selected()
        {
            foreach (DataGridViewRow row in _grid.SelectedRows)
            {
                if (row.DataBoundItem is ImportedTransactionView view)
                    yield return view;
            }
        }

        private void AcceptMatches(IReadOnlyList<ImportedTransactionView> rows) =>
            ReconcileMany(rows, row => _reconciliation.AcceptMatch(row.Id, refreshBudgets: false), "Accept Match");

        private void AcceptAsNew(IReadOnlyList<ImportedTransactionView> rows) =>
            ReconcileMany(rows, row => _reconciliation.AcceptAsNew(row.Id, refreshBudgets: false), "Accept as New");

        private void ChangeMatch(ImportedTransactionView imported)
        {
            if (_accountId is not Guid accountId)
                return;

            var choices = _reconciliation.GetUnmatchedExpected(accountId).ToList();
            using var dialog = new ChangeMatchDialog(imported, choices);
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            try
            {
                if (dialog.TreatAsNew)
                    _reconciliation.AcceptAsNew(imported.Id);
                else if (dialog.SelectedExpectedId is Guid expectedId)
                    _reconciliation.AcceptMatch(imported.Id, expectedId);
                FinishReconcile();
            }
            catch (InvalidOperationException ex)
            {
                ExceptionDialog.Show(FindForm(), "Change Match", ex, icon: MessageBoxIcon.Information);
            }
        }

        private void AcceptUpToLastStatement()
        {
            if (_accountId is not Guid accountId)
                return;

            var lastStatement = _statements.GetForAccount(accountId)
                .OrderByDescending(statement => statement.StatementDate)
                .Select(statement => statement.StatementDate.Date)
                .FirstOrDefault();
            if (lastStatement == default)
            {
                MessageBox.Show(FindForm(),
                    "This account has no statements. Add a statement before accepting unreconciled transactions up to the last statement date.",
                    "Accept Unreconciled",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dialog = new AcceptBeforeDateDialog(lastStatement);
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            try
            {
                AppStatus.Set("Reconciling transactions...", busy: true);
                var result = _reconciliation.AcceptAsNewOnOrBefore(accountId, dialog.OnOrBeforeDate, AppStatus.ForImport());
                FinishReconcile();
                AppStatus.Set(ImportStatusText.Reconciled(result));
            }
            catch (Exception ex)
            {
                AppStatus.Set("Reconcile failed.");
                ExceptionDialog.Show(FindForm(), "Accept Unreconciled", ex);
            }
        }

        private void ReconcileMany(
            IReadOnlyList<ImportedTransactionView> rows,
            Action<ImportedTransactionView> accept,
            string title)
        {
            if (rows.Count == 0)
                return;

            var bulk = rows.Count > 1;
            try
            {
                if (bulk)
                    AppStatus.Set("Reconciling transactions...", busy: true);
                var progress = bulk ? AppStatus.ForImport() : null;
                ImportProgressReporter.Report(progress, 0, rows.Count, activity: "Reconciling", stride: 1);
                for (var i = 0; i < rows.Count; i++)
                {
                    accept(rows[i]);
                    ImportProgressReporter.Report(progress, i + 1, rows.Count, activity: "Reconciling", stride: 1);
                }

                _reconciliation.RefreshBudgetsFor(rows.Select(row => row.Id));
                FinishReconcile();
                if (bulk)
                    AppStatus.Set(ImportStatusText.Reconciled(ImportResult.FromDates(rows.Count, rows.Select(row => row.Date))));
            }
            catch (InvalidOperationException ex)
            {
                if (bulk)
                    AppStatus.Set("Reconcile failed.");
                ExceptionDialog.Show(FindForm(), title, ex, icon: MessageBoxIcon.Information);
            }
        }

        private void FinishReconcile()
        {
            ShowAccount(_accountId);
            Reconciled?.Invoke(this, EventArgs.Empty);
        }

        private void SizeColumns()
        {
            if (_grid.IsDisposed || _grid.Columns.Count < 5)
                return;

            const int dateWidth = 100;
            const int amountWidth = 110;
            const int statusWidth = 120;
            var scrollbar = _grid.Controls.OfType<VScrollBar>().Any(bar => bar.Visible)
                ? SystemInformation.VerticalScrollBarWidth
                : 0;
            var remaining = _grid.ClientSize.Width - dateWidth - amountWidth - statusWidth - scrollbar;
            if (remaining < 200)
                return;

            var description = Math.Max(120, remaining * 5 / 11);
            var match = Math.Max(140, remaining - description);
            _grid.Columns[nameof(ImportedTransactionView.Date)].Width = dateWidth;
            _grid.Columns[nameof(ImportedTransactionView.Amount)].Width = amountWidth;
            _grid.Columns[nameof(ImportedTransactionView.Description)].Width = description;
            _grid.Columns[nameof(ImportedTransactionView.RecommendedMatch)].Width = match;
            _grid.Columns[nameof(ImportedTransactionView.Status)].Width = statusWidth;
        }

        private static DataGridViewTextBoxColumn DateColumn()
        {
            var column = TextColumn(nameof(ImportedTransactionView.Date), "Date");
            column.DefaultCellStyle.Format = "d";
            column.Width = 100;
            return column;
        }

        private static DataGridViewTextBoxColumn AmountColumn()
        {
            var column = TextColumn(nameof(ImportedTransactionView.Amount), "Amount");
            column.DefaultCellStyle.Format = "c2";
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            column.Width = 110;
            return column;
        }

        private static DataGridViewTextBoxColumn TextColumn(string property, string header) =>
            new()
            {
                DataPropertyName = property,
                HeaderText = header,
                Name = property,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Width = 160
            };
    }
}
