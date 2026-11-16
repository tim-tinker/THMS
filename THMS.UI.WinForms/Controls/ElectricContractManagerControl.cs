using System.ComponentModel;
using THMS.Domain.Finance;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Controls
{
    public partial class ElectricContractManagerControl : UserControl, IDataManagerControl
    {
        private readonly ElectricContractOrchestrator _orchestrator = new();
        private string _currentPeriod = "Month";
        private BindingList<ElectricContract> _contracts;

        public event EventHandler? SelectionChanged;

        public bool HasSelection => SelectedContract() is not null;

        public ElectricContractManagerControl()
        {
            InitializeComponent();
            DataGridViewUtil.EnableDoubleBuffering(_gridContracts);
            _gridContracts.AutoGenerateColumns = false;
            _gridContracts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            _gridContracts.CellDoubleClick += OnCellDoubleClick;
            _gridContracts.MouseDown += OnGridMouseDown;
            _gridContracts.KeyDown += OnGridKeyDown;
            _gridContracts.SelectionChanged += (_, _) => OnSelectionChanged();
            ApplyColumnAutoSize();
        }

        public Control GetControl() => this;

        public void SetGridDataSource(string period)
        {
            _currentPeriod = period;
            _contracts = new BindingList<ElectricContract>(_orchestrator.GetElectricContracts(period).ToList());
            _gridContracts.DataSource = _contracts;
            ApplyColumnAutoSize();
            OnSelectionChanged();
        }

        public void EditSelected()
        {
            if (SelectedContract() is ElectricContract contract)
                EditContract(contract);
        }

        public void DeleteSelected()
        {
            if (SelectedContract() is ElectricContract contract)
                DeleteContract(contract);
        }

        private void ApplyColumnAutoSize()
        {
            foreach (DataGridViewColumn column in _gridContracts.Columns)
            {
                if (column == NameColumn)
                    continue;
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            }

            NameColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            NameColumn.MinimumWidth = Math.Max(NameColumn.Width, 80);
            NameColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            DataGridViewUtil.FitDisplayedDateAndMoneyColumns(_gridContracts);
        }

        private ElectricContract? SelectedContract() =>
            _gridContracts.CurrentRow?.DataBoundItem as ElectricContract;

        private void OnSelectionChanged() =>
            SelectionChanged?.Invoke(this, EventArgs.Empty);

        private void OnCellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;
            if (_gridContracts.Rows[e.RowIndex].DataBoundItem is not ElectricContract contract)
                return;

            EditContract(contract);
        }

        private void OnGridKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Delete)
                return;

            e.Handled = true;
            e.SuppressKeyPress = true;
            DeleteSelected();
        }

        private void OnGridMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            var hit = _gridContracts.HitTest(e.X, e.Y);
            if (hit.RowIndex < 0)
                return;
            if (!DataGridViewUtil.CellIsSelected(_gridContracts, hit.RowIndex, hit.ColumnIndex)
                && !DataGridViewUtil.SelectOnlyCell(_gridContracts, hit.RowIndex, hit.ColumnIndex))
                return;

            var rowIndex = hit.RowIndex;
            var location = e.Location;
            _gridContracts.BeginInvoke(() => ShowContractMenu(rowIndex, location));
        }

        private void ShowContractMenu(int rowIndex, Point location)
        {
            if (_gridContracts.IsDisposed || rowIndex < 0 || rowIndex >= _gridContracts.RowCount)
                return;
            if (_gridContracts.Rows[rowIndex].DataBoundItem is not ElectricContract contract)
                return;

            var menu = new ContextMenuStrip();
            var edit = new ToolStripMenuItem("Edit");
            edit.Click += (_, _) => EditContract(contract);
            menu.Items.Add(edit);

            var delete = new ToolStripMenuItem("Delete");
            delete.Click += (_, _) => DeleteContract(contract);
            menu.Items.Add(delete);

            GridCsvExport.AddCommand(menu, _gridContracts);
            menu.Closed += (_, _) => BeginInvoke(menu.Dispose);
            menu.Show(_gridContracts, location);
        }

        private void EditContract(ElectricContract contract)
        {
            using var form = new ElectricContractDataEntryForm(contract);
            if (form.ShowDialog(FindForm()) != DialogResult.OK || form.Contract is null)
                return;

            try
            {
                _orchestrator.Save(form.Contract);
            }
            catch (Exception ex)
            {
                ExceptionDialog.Show(FindForm(), "Edit Electric Contract", ex);
                return;
            }

            SetGridDataSource(_currentPeriod);
            SelectContract(form.Contract.Id);
        }

        private void DeleteContract(ElectricContract contract)
        {
            var label = string.IsNullOrWhiteSpace(contract.Name)
                ? "this electric contract"
                : $"'{contract.Name.Trim()}'";
            if (MessageBox.Show(
                    FindForm(),
                    $"Delete {label}?",
                    "Delete Electric Contract",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            try
            {
                _orchestrator.Delete(contract.Id);
            }
            catch (Exception ex)
            {
                ExceptionDialog.Show(FindForm(), "Delete Electric Contract", ex);
                return;
            }

            SetGridDataSource(_currentPeriod);
        }

        private void SelectContract(Guid contractId)
        {
            for (var i = 0; i < _gridContracts.Rows.Count; i++)
            {
                if (_gridContracts.Rows[i].DataBoundItem is ElectricContract contract && contract.Id == contractId)
                {
                    DataGridViewUtil.SelectCellWhenReady(_gridContracts, i, 0);
                    return;
                }
            }
        }

        private void OnLoad(object sender, EventArgs e)
        {
            SetGridDataSource(_currentPeriod);
        }

        private void OnVisibleChanged(object sender, EventArgs e)
        {
            if (Visible)
                SetGridDataSource(_currentPeriod);
        }
    }
}
