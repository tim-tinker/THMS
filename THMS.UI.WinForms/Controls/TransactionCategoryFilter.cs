using THMS.Logic.ViewModels.Finance;

namespace THMS.UI.WinForms.Controls
{
    /// <summary>
    /// Category header click shows the categories in the grid. Choosing one shows only those rows.
    /// All shows every row again.
    /// </summary>
    internal sealed class TransactionCategoryFilter
    {
        private readonly DataGridView _grid;
        private readonly BindingSource _source;
        private readonly DataGridViewColumn _categoryColumn;
        private readonly string _header;
        private List<UnifiedTransactionView> _all = [];
        private string? _selected;
        private bool _applying;

        private TransactionCategoryFilter(DataGridView grid, BindingSource source, DataGridViewColumn categoryColumn)
        {
            _grid = grid;
            _source = source;
            _categoryColumn = categoryColumn;
            _header = categoryColumn.HeaderText;
            _categoryColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            _grid.ColumnHeaderMouseClick += OnHeaderClick;
            _source.DataSourceChanged += (_, _) => Capture();
            Capture();
        }

        public static void Attach(DataGridView grid, BindingSource source)
        {
            ArgumentNullException.ThrowIfNull(grid);
            ArgumentNullException.ThrowIfNull(source);
            var column = grid.Columns
                .Cast<DataGridViewColumn>()
                .FirstOrDefault(item => item.DataPropertyName == nameof(UnifiedTransactionView.Category));
            if (column is null)
                return;

            _ = new TransactionCategoryFilter(grid, source, column);
        }

        private void OnHeaderClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex != -1 || e.ColumnIndex != _categoryColumn.Index || e.Button != MouseButtons.Left)
                return;

            var header = _grid.GetCellDisplayRectangle(e.ColumnIndex, -1, cutOverflow: false);
            ExcelColumnFilterDropDown.ShowSingle(
                _grid,
                _grid.RectangleToScreen(header),
                _all.Select(CategoryKey),
                _selected,
                chosen =>
                {
                    _selected = chosen;
                    UpdateHeader();
                    Apply();
                });
        }

        private void Capture()
        {
            if (_applying)
                return;

            _all = _source.List.Cast<UnifiedTransactionView>().ToList();
            if (_selected is not null && _all.All(row => !Matches(row, _selected)))
                _selected = null;
            UpdateHeader();
            if (_selected is not null)
                Apply();
        }

        private void Apply()
        {
            _applying = true;
            try
            {
                _source.DataSource = _selected is null
                    ? _all
                    : _all.Where(row => Matches(row, _selected)).ToList();
            }
            finally
            {
                _applying = false;
            }
        }

        private void UpdateHeader() =>
            _categoryColumn.HeaderText = _selected is null ? _header : _header + " ▼";

        private static bool Matches(UnifiedTransactionView row, string category) =>
            string.Equals(CategoryKey(row), category, StringComparison.CurrentCultureIgnoreCase);

        private static string CategoryKey(UnifiedTransactionView view) =>
            ExcelColumnFilterDropDown.Normalize(view.Category);
    }
}