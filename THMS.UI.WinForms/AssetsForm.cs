using THMS.Domain.Assets;
using THMS.Logic.Orchestrators;
using THMS.Logic.ViewModels.Assets;
using THMS.UI.WinForms.Controls;

namespace THMS.UI.WinForms
{
    public class AssetsForm : BaseEmbeddedForm
    {
        private readonly AssetOrchestrator _orchestrator;
        private readonly DataGridView _assets = Grid();
        private readonly DataGridView _valuations = Grid();
        private readonly DataGridView _sources = Grid();
        private readonly BindingSource _assetSource = new();
        private readonly BindingSource _valuationSource = new();
        private readonly BindingSource _sourceCatalog = new();
        private bool _binding;
        private bool _valuationBreakdown;

        public AssetsForm()
            : this(new AssetOrchestrator())
        {
        }

        internal AssetsForm(AssetOrchestrator orchestrator)
        {
            _orchestrator = orchestrator;
            Text = "Assets";
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.White;

            _assets.DataSource = _assetSource;
            _valuations.DataSource = _valuationSource;
            _sources.DataSource = _sourceCatalog;
            _sources.Columns.AddRange(
                LinkFill(nameof(ValuationSource.Name), "Source"),
                Fill(nameof(ValuationSource.AppraisesLabel), "Appraises"),
                Fill(nameof(ValuationSource.WebsiteUrl), "Website"));
            _sources.CellContentClick += (_, e) => OpenSourceName(e);
            _sources.CellFormatting += (_, e) => FormatSourceName(e);
            _sources.CellDoubleClick += (_, e) =>
            {
                if (e.RowIndex < 0 || SourceHasWebsite(e))
                    return;
                EditSource(SelectedSource());
            };
            UseMenu(_sources,
                ("Edit", () => EditSource(SelectedSource())),
                ("Delete", () => DeleteSource()));
            _assets.Columns.AddRange(
                Fill(nameof(AssetListRow.Name), "Name"),
                Column(nameof(AssetListRow.Kind), "Kind"),
                Column(nameof(AssetListRow.Category), "Category"),
                Column(nameof(AssetListRow.AcquiredOn), "Acquired"),
                Money(nameof(AssetListRow.CostBasis), "Cost basis"),
                Fill(nameof(AssetListRow.LatestValue), "Latest value"),
                Fill(nameof(AssetListRow.SecuredAccount), "Loan, lease, or mortgage"));
            _valuations.Columns.AddRange(
                Fill(nameof(AssetValuation.ValueType), "Value"),
                Money(nameof(AssetValuation.Amount), "Amount"),
                Date(nameof(AssetValuation.AsOf), "As of"),
                Fill(nameof(AssetValuation.Source), "Source"));

            _assets.SelectionChanged += (_, _) => LoadChildren();
            _assets.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) EditAsset(SelectedAsset()); };
            _valuations.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) EditValuation(SelectedValuation()); };
            UseMenu(_assets,
                ("Edit", () => EditAsset(SelectedAsset())),
                ("Delete", () => DeleteAsset()));
            UseMenu(_valuations,
                ("Edit", () => EditValuation(SelectedValuation())),
                ("Add property tax statement", () => AddTaxStatementFromValuation()));
            _valuations.ContextMenuStrip!.Opening += (_, _) =>
            {
                var property = SelectedAsset() is PropertyAsset;
                foreach (ToolStripItem item in _valuations.ContextMenuStrip.Items)
                {
                    if (item.Text != "Add property tax statement")
                        continue;
                    item.Visible = property;
                    item.Enabled = property && SelectedValuation() is PropertyAppraisal;
                }
            };

            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
            split.Panel1.Controls.Add(Section("Assets", _assets, () => EditAsset(null)));
            split.Panel2.Controls.Add(Section("Valuations", _valuations, () => EditValuation(null)));

            var tabs = new ThmsTabControl { Dock = DockStyle.Fill };
            var assetsPage = new TabPage("Assets");
            var sourcesPage = new TabPage("Sources");
            assetsPage.Controls.Add(split);
            sourcesPage.Controls.Add(Section("Valuation sources", _sources, () => EditSource(null)));
            tabs.TabPages.Add(assetsPage);
            tabs.TabPages.Add(sourcesPage);
            Controls.Add(tabs);
            void LayoutSplits()
            {
                if (split.Height > 240)
                    split.SplitterDistance = split.Height / 2;
            }
            Load += (_, _) =>
            {
                LayoutSplits();
                Reload();
                ReloadSources();
            };
            Resize += (_, _) => LayoutSplits();
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
            var breakdown = asset is PropertyAsset;
            if (breakdown != _valuationBreakdown)
            {
                _valuationBreakdown = breakdown;
                _valuationSource.DataSource = null;
                _valuations.Columns.Clear();
                _valuations.Columns.AddRange(breakdown ? PropertyValuationColumns() : GenericValuationColumns());
            }

            if (asset is null)
                _valuationSource.DataSource = Array.Empty<object>();
            else if (breakdown)
                _valuationSource.DataSource = _orchestrator.GetPropertyAppraisals(asset.Id).ToList();
            else
                _valuationSource.DataSource = _orchestrator.GetValuations(asset.Id).ToList();
        }

        private void ReloadSources(Guid? keepId = null)
        {
            keepId ??= SelectedSource()?.Id;
            _sourceCatalog.DataSource = _orchestrator.GetValuationSources().ToList();
            if (keepId is null)
                return;
            foreach (DataGridViewRow row in _sources.Rows)
            {
                if (row.DataBoundItem is ValuationSource source && source.Id == keepId)
                {
                    row.Selected = true;
                    _sources.CurrentCell = row.Cells[0];
                    return;
                }
            }
        }

        private void EditSource(ValuationSource? source)
        {
            using var dialog = new ValuationSourceDialog(_orchestrator, source);
            if (dialog.ShowDialog(this) == DialogResult.OK)
                ReloadSources(source?.Id);
        }

        private void DeleteSource()
        {
            var source = SelectedSource();
            if (source is null)
                return;
            if (MessageBox.Show(this, $"Delete {source.Name}?", "Delete Source",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;
            _orchestrator.DeleteValuationSource(source.Id);
            ReloadSources();
        }

        private void OpenSourceName(DataGridViewCellEventArgs e)
        {
            if (!SourceHasWebsite(e))
                return;
            OpenSourceWebsite(_sources.Rows[e.RowIndex].DataBoundItem as ValuationSource);
        }

        private void FormatSourceName(DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;
            if (_sources.Columns[e.ColumnIndex].Name != nameof(ValuationSource.Name))
                return;
            if (_sources.Rows[e.RowIndex].Cells[e.ColumnIndex] is not DataGridViewLinkCell cell)
                return;
            if (e.CellStyle is null)
                return;
            if (_sources.Rows[e.RowIndex].DataBoundItem is not ValuationSource source)
                return;

            var plain = e.CellStyle.ForeColor;
            if (plain.IsEmpty)
                plain = _sources.ForeColor;
            DataGridViewUtil.ApplyLinkAppearance(cell, e.CellStyle, source.Website is not null, plain);
        }

        private bool SourceHasWebsite(DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return false;
            if (_sources.Columns[e.ColumnIndex].Name != nameof(ValuationSource.Name))
                return false;
            return (_sources.Rows[e.RowIndex].DataBoundItem as ValuationSource)?.Website is not null;
        }

        private static void OpenSourceWebsite(ValuationSource? source)
        {
            var website = source?.Website;
            if (website is null)
                return;
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = website.ToString(),
                UseShellExecute = true
            });
        }

        private ValuationSource? SelectedSource() =>
            _sources.CurrentRow?.DataBoundItem as ValuationSource;

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

        private void EditValuation(object? existing)
        {
            var asset = SelectedAsset();
            if (asset is null)
            {
                MessageBox.Show(this, "Select an asset first.", "Valuation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dialog = asset switch
            {
                PropertyAsset property => (Form)new PropertyValuationDialog(_orchestrator, property, existing as PropertyAppraisal),
                VehicleAsset vehicle => new VehicleValuationDialog(_orchestrator, vehicle, existing as AssetValuation),
                PersonalAsset personal => new PersonalValuationDialog(_orchestrator, personal, existing as AssetValuation),
                _ => throw new InvalidOperationException($"No valuation editor for {asset.Name}.")
            };
            if (dialog.ShowDialog(this) == DialogResult.OK)
                Reload(asset.Id);
        }

        private void AddTaxStatementFromValuation()
        {
            if (SelectedAsset() is not PropertyAsset property || SelectedValuation() is not PropertyAppraisal appraisal)
                return;

            using var dialog = new PropertyTaxStatementDialog(_orchestrator, null, collector: null, property, appraisal);
            if (dialog.ShowDialog(this) == DialogResult.OK)
                Reload(property.Id);
        }

        private Asset? SelectedAsset()
        {
            if (_assets.CurrentRow?.DataBoundItem is not AssetListRow row)
                return null;
            return _orchestrator.GetAsset(row.Id);
        }

        private object? SelectedValuation() => _valuations.CurrentRow?.DataBoundItem;

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

        private static DataGridViewTextBoxColumn[] GenericValuationColumns() =>
        [
            Fill(nameof(AssetValuation.ValueType), "Value"),
            Money(nameof(AssetValuation.Amount), "Amount"),
            Date(nameof(AssetValuation.AsOf), "As of"),
            Fill(nameof(AssetValuation.Source), "Source")
        ];

        private static DataGridViewTextBoxColumn[] PropertyValuationColumns() =>
        [
            Date(nameof(PropertyAppraisal.AsOf), "As of"),
            Money(nameof(PropertyAppraisal.LandMarketValue), "Land market"),
            Money(nameof(PropertyAppraisal.ImprovementMarketValue), "Impr market"),
            Money(nameof(PropertyAppraisal.TotalMarketValue), "Total market"),
            Money(nameof(PropertyAppraisal.HomesteadCap), "Homestead cap"),
            Money(nameof(PropertyAppraisal.AssessedValue), "Assessed"),
            Fill(nameof(PropertyAppraisal.Source), "Source")
        ];

        private static DataGridViewTextBoxColumn Column(string property, string header) =>
            new() { DataPropertyName = property, HeaderText = header, Name = property, ReadOnly = true };

        private static DataGridViewLinkColumn LinkFill(string property, string header) =>
            new()
            {
                DataPropertyName = property,
                HeaderText = header,
                Name = property,
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                LinkBehavior = LinkBehavior.HoverUnderline,
                TrackVisitedState = false
            };

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
