using THMS.Domain.Assets;
using THMS.Domain.Finance.Planning;
using THMS.Logic.Orchestrators;
using THMS.Logic.ViewModels.Assets;
using THMS.UI.WinForms.Controls;

namespace THMS.UI.WinForms
{
    public class AssetsForm : BaseEmbeddedForm
    {
        private readonly AssetOrchestrator _orchestrator = new();
        private readonly DataGridView _assets = Grid();
        private readonly DataGridView _valuations = Grid();
        private readonly DataGridView _taxes = Grid();
        private readonly BindingSource _assetSource = new();
        private readonly BindingSource _valuationSource = new();
        private readonly BindingSource _taxSource = new();
        private bool _binding;

        public AssetsForm()
        {
            Text = "Assets";
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.White;

            _assets.DataSource = _assetSource;
            _valuations.DataSource = _valuationSource;
            _taxes.DataSource = _taxSource;
            _assets.Columns.AddRange(
                Fill(nameof(AssetListRow.Name), "Name"),
                Column(nameof(AssetListRow.Kind), "Kind"),
                Column(nameof(AssetListRow.Category), "Category"),
                Column(nameof(AssetListRow.AcquiredOn), "Acquired"),
                Money(nameof(AssetListRow.CostBasis), "Cost basis"),
                Fill(nameof(AssetListRow.LatestValue), "Latest value"),
                Fill(nameof(AssetListRow.SecuredAccount), "Loan or mortgage"));
            _valuations.Columns.AddRange(
                Fill(nameof(AssetValuation.ValueType), "Value"),
                Money(nameof(AssetValuation.Amount), "Amount"),
                Date(nameof(AssetValuation.AsOf), "As of"),
                Fill(nameof(AssetValuation.Source), "Source"));
            _taxes.Columns.AddRange(
                Column(nameof(PropertyTaxListRow.TaxYear), "Tax year"),
                Date(nameof(PropertyTaxListRow.ProcessDate), "Process date"),
                Date(nameof(PropertyTaxListRow.DueDate), "Due date"),
                Money(nameof(PropertyTaxListRow.AmountDue), "Taxes due"),
                Fill(nameof(PropertyTaxListRow.Collector), "Collector"));

            _assets.SelectionChanged += (_, _) => LoadChildren();
            _assets.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) EditAsset(SelectedAsset()); };
            _valuations.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) EditValuation(SelectedValuation()); };
            _taxes.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) EditTaxStatement(SelectedTax()); };
            UseMenu(_assets,
                ("Edit", () => EditAsset(SelectedAsset())),
                ("Delete", () => DeleteAsset()));
            UseMenu(_valuations, ("Edit", () => EditValuation(SelectedValuation())));
            UseMenu(_taxes,
                ("Edit", () => EditTaxStatement(SelectedTax())),
                ("Delete", () => DeleteTaxStatement()));

            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
            var lower = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical };
            split.Panel1.Controls.Add(Section("Assets", _assets, () => EditAsset(null)));
            lower.Panel1.Controls.Add(Section("Valuations", _valuations, () => EditValuation(null)));
            lower.Panel2.Controls.Add(Section("Property tax statements", _taxes, () => EditTaxStatement(null)));
            split.Panel2.Controls.Add(lower);

            Controls.Add(split);
            Load += (_, _) =>
            {
                if (split.Height > 240)
                    split.SplitterDistance = split.Height / 2;
                if (lower.Width > 320)
                    lower.SplitterDistance = lower.Width / 2;
                Reload();
            };
        }

        private void Reload(Guid? keepAssetId = null)
        {
            keepAssetId ??= SelectedAsset()?.Id;
            _binding = true;
            _assetSource.DataSource = _orchestrator.GetAssetRows();
            _binding = false;
            SelectAsset(keepAssetId);
            LoadChildren();
        }

        private void LoadChildren()
        {
            if (_binding)
                return;
            var asset = SelectedAsset();
            _valuationSource.DataSource = asset is null
                ? []
                : _orchestrator.GetValuations(asset.Id).ToList();
            _taxSource.DataSource = asset is PropertyAsset
                ? _orchestrator.GetPropertyTaxRows(asset.Id).ToList()
                : [];
        }

        private void EditAsset(Asset? asset)
        {
            using var dialog = new AssetEditorDialog(_orchestrator, asset);
            if (dialog.ShowDialog(this) == DialogResult.OK)
                Reload(asset?.Id);
        }

        private void DeleteAsset()
        {
            var asset = SelectedAsset();
            if (asset is null)
                return;
            if (MessageBox.Show(this, $"Delete {asset.Name}?", "Delete Asset",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;
            try
            {
                _orchestrator.DeleteAsset(asset.Id);
                Reload();
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(this, ex.Message, "Delete Asset", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void EditValuation(AssetValuation? existing)
        {
            var asset = SelectedAsset();
            if (asset is null)
            {
                MessageBox.Show(this, "Select an asset first.", "Valuation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dialog = new ValuationEditorDialog(_orchestrator, asset.Id, existing);
            if (dialog.ShowDialog(this) == DialogResult.OK)
                Reload(asset.Id);
        }

        private void EditTaxStatement(PropertyTaxStatement? existing)
        {
            var asset = SelectedAsset() as PropertyAsset;
            if (asset is null && existing is null)
            {
                MessageBox.Show(this, "Select a property first.", "Property Tax",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (existing is not null && _orchestrator.GetAsset(existing.AssetId) is PropertyAsset billed)
                asset = billed;

            using var dialog = new PropertyTaxStatementDialog(_orchestrator, existing, collector: null, asset);
            if (dialog.ShowDialog(this) == DialogResult.OK)
                Reload(asset?.Id);
        }

        private void DeleteTaxStatement()
        {
            var row = _taxes.CurrentRow?.DataBoundItem as PropertyTaxListRow;
            if (row is null)
                return;
            if (MessageBox.Show(this, $"Delete the {row.TaxYear} property tax statement?", "Delete Statement",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;
            _orchestrator.DeletePropertyTaxStatement(row.Id);
            Reload();
        }

        private Asset? SelectedAsset()
        {
            if (_assets.CurrentRow?.DataBoundItem is not AssetListRow row)
                return null;
            return _orchestrator.GetAsset(row.Id);
        }

        private AssetValuation? SelectedValuation() =>
            _valuations.CurrentRow?.DataBoundItem as AssetValuation;

        private PropertyTaxStatement? SelectedTax()
        {
            if (_taxes.CurrentRow?.DataBoundItem is not PropertyTaxListRow row)
                return null;
            return _orchestrator.GetPropertyTaxStatement(row.Id);
        }

        private void SelectAsset(Guid? id)
        {
            if (id is null)
                return;
            foreach (DataGridViewRow row in _assets.Rows)
            {
                if (row.DataBoundItem is AssetListRow item && item.Id == id)
                {
                    row.Selected = true;
                    _assets.CurrentCell = row.Cells[0];
                    return;
                }
            }
        }

        private static void UseMenu(DataGridView grid, params (string Text, Action Command)[] commands)
        {
            var menu = new ContextMenuStrip();
            foreach (var command in commands)
            {
                var item = new ToolStripMenuItem(command.Text);
                var action = command.Command;
                item.Click += (_, _) => action();
                menu.Items.Add(item);
            }

            menu.Opening += (_, _) =>
            {
                var enabled = grid.CurrentRow?.DataBoundItem is not null;
                foreach (ToolStripItem item in menu.Items)
                    item.Enabled = enabled;
            };
            grid.MouseDown += (_, e) => SelectRow(grid, e);
            grid.ContextMenuStrip = menu;
        }

        private static void SelectRow(DataGridView grid, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;
            var hit = grid.HitTest(e.X, e.Y);
            if (hit.RowIndex < 0 || hit.ColumnIndex < 0)
                return;
            grid.CurrentCell = grid.Rows[hit.RowIndex].Cells[hit.ColumnIndex];
        }

        private static Panel Section(string title, DataGridView grid, Action add)
        {
            var label = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Padding = new Padding(8, 4, 8, 4),
                BackColor = Color.White,
                UseMnemonic = false
            };
            label.Height = TextRenderer.MeasureText(title, label.Font).Height + label.Padding.Vertical + 6;

            var button = new ThmsButton { Text = "Add", Margin = new Padding(0) };
            button.Click += (_, _) => add();
            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(8, 8, 8, 8),
                BackColor = Color.White
            };
            buttons.Controls.Add(button);
            buttons.Height = button.PreferredSize.Height + buttons.Padding.Vertical + 4;

            grid.Dock = DockStyle.Fill;
            var host = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            host.Controls.Add(grid);
            host.Controls.Add(buttons);
            host.Controls.Add(label);
            return host;
        }

        private static DataGridView Grid()
        {
            var grid = new DataGridView
            {
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoGenerateColumns = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells
            };
            DataGridViewUtil.EnableDoubleBuffering(grid);
            return grid;
        }

        private static DataGridViewTextBoxColumn Column(string property, string header) =>
            new() { DataPropertyName = property, HeaderText = header, Name = property, ReadOnly = true };

        private static DataGridViewTextBoxColumn Fill(string property, string header)
        {
            var column = Column(property, header);
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            return column;
        }

        private static DataGridViewTextBoxColumn Money(string property, string header)
        {
            var column = Column(property, header);
            column.DefaultCellStyle.Format = "c2";
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            return column;
        }

        private static DataGridViewTextBoxColumn Date(string property, string header)
        {
            var column = Column(property, header);
            column.DefaultCellStyle.Format = "d";
            return column;
        }
    }
}
