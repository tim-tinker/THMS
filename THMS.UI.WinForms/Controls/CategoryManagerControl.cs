using System.ComponentModel;
using System.Drawing.Drawing2D;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Controls
{
    public partial class CategoryManagerControl : UserControl
    {
        private CategoryOrchestrator? _orchestrator;
        private ImageList? _statusImages;
        private ExpenseCategory? _selected;
        private bool _loading;
        private bool _dirty;

        public event EventHandler? CatalogChanged;
        public event EventHandler? CloseRequested;

        public bool CatalogHasChanged { get; private set; }

        public IButtonControl SaveButton => btnSave;
        public IButtonControl CloseButton => btnClose;

        [DefaultValue(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool ShowCloseButton
        {
            get => btnClose.Visible;
            set => btnClose.Visible = value;
        }

        public CategoryManagerControl()
        {
            InitializeComponent();
        }

        public void Bind(CategoryOrchestrator orchestrator)
        {
            ArgumentNullException.ThrowIfNull(orchestrator);
            _orchestrator = orchestrator;
            BindStatusImages();
            ReloadTree();
            ClearEditor();
        }

        public void ReloadIfClean()
        {
            if (_dirty || _orchestrator is null)
                return;
            ReloadTree(_selected?.Id);
        }

        public void RefreshLayout()
        {
            PerformLayout();
            ApplyEditorLayout();
        }

        public bool TryLeave()
        {
            if (!_dirty)
                return true;
            if (!ConfirmDiscard())
                return false;
            _dirty = false;
            return true;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ApplyEditorLayout();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible)
                ApplyEditorLayout();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (IsHandleCreated && Visible)
                ApplyEditorLayout();
        }

        private void ApplyEditorLayout()
        {
            var minTree = LogicalToDeviceUnits(160);
            var minEditor = LogicalToDeviceUnits(280);
            splitMain.Panel1MinSize = 0;
            splitMain.Panel2MinSize = 0;

            var available = splitMain.Width - splitMain.SplitterWidth;
            if (available <= 0)
                return;

            minTree = Math.Min(minTree, Math.Max(0, available / 3));
            minEditor = Math.Min(minEditor, Math.Max(0, available - minTree));

            var maxTree = Math.Max(minTree, available - minEditor);
            var desiredTree = Math.Clamp(available * 2 / 5, minTree, maxTree);
            splitMain.SplitterDistance = desiredTree;
            splitMain.Panel1MinSize = minTree;
            splitMain.Panel2MinSize = minEditor;
        }

        private CategoryOrchestrator Orchestrator =>
            _orchestrator ??= new CategoryOrchestrator();

        private void BindStatusImages()
        {
            if (_statusImages is not null)
                return;

            _statusImages = new ImageList
            {
                ColorDepth = ColorDepth.Depth32Bit,
                ImageSize = new Size(16, 16)
            };
            _statusImages.Images.Add("active", CreateDot(Color.SeaGreen));
            _statusImages.Images.Add("inactive", CreateDot(Color.Gray));
            treeCategories.ImageList = _statusImages;
        }

        private void ReloadTree(Guid? selectId = null)
        {
            var selected = selectId ?? (_selected?.Id);
            var all = Orchestrator.GetAllCategories(includeInactive: true).ToList();
            var search = txtSearch.Text.Trim();
            if (!string.IsNullOrEmpty(search))
            {
                var include = all
                    .Where(c => c.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .Select(c => c.Id)
                    .ToHashSet();
                foreach (var match in all.Where(c => include.Contains(c.Id)).ToList())
                {
                    var current = match;
                    var seen = new HashSet<Guid>();
                    while (current.ParentCategoryId is Guid parentId && seen.Add(current.Id))
                    {
                        include.Add(parentId);
                        current = all.FirstOrDefault(c => c.Id == parentId);
                        if (current is null)
                            break;
                    }
                }

                all = all.Where(c => include.Contains(c.Id)).ToList();
            }

            _loading = true;
            CategoryTreeUi.Fill(treeCategories, all, includeInactive: true, selectedId: selected);
            _loading = false;

            if (treeCategories.SelectedNode?.Tag is Guid id)
                LoadEditor(id);
            else
                ClearEditor();
        }

        private void LoadEditor(Guid categoryId)
        {
            var category = Orchestrator.GetCategory(categoryId);
            if (category is null)
            {
                ClearEditor();
                return;
            }

            _loading = true;
            _selected = category;
            txtName.Text = category.Name;
            chkActive.Checked = category.IsActive;
            BindParents(category);
            var usage = Orchestrator.GetUsage(category.Id);
            lblUsage.Text =
                $"Usage: {usage.Total}  (transactions {usage.TransactionCount}, " +
                $"rules {usage.RecurringRuleCount}, budgets {usage.BudgetRuleCount}, learned {usage.LearnedMappingCount})";
            var isUncategorized = category.Id == DefaultExpenseCategories.UncategorizedId;
            btnDeactivate.Enabled = category.IsActive && !isUncategorized;
            btnSave.Enabled = true;
            btnMergeSelected.Enabled = !isUncategorized;
            SetEditorEnabled(true);
            _dirty = false;
            _loading = false;
        }

        private void BindParents(ExpenseCategory category)
        {
            var parents = Orchestrator.GetActiveCategories().ToList();
            if (category.ParentCategoryId is Guid parentId &&
                parents.All(c => c.Id != parentId) &&
                Orchestrator.GetCategory(parentId) is ExpenseCategory parent)
            {
                parents.Add(parent);
            }

            var items = CategoryTreeUi.ParentOptions(parents, excludeId: category.Id);
            cmbParent.DisplayMember = nameof(CategoryParentOption.Name);
            cmbParent.ValueMember = nameof(CategoryParentOption.Id);
            cmbParent.DataSource = items.ToList();
            var match = items.FirstOrDefault(i => i.Id == category.ParentCategoryId);
            cmbParent.SelectedItem = match ?? items[0];
        }

        private void ClearEditor()
        {
            _loading = true;
            _selected = null;
            txtName.Text = "";
            chkActive.Checked = false;
            cmbParent.DataSource = null;
            lblUsage.Text = "Usage: select a category";
            btnSave.Enabled = false;
            btnDeactivate.Enabled = false;
            btnMergeSelected.Enabled = false;
            SetEditorEnabled(false);
            _dirty = false;
            _loading = false;
        }

        private void SetEditorEnabled(bool enabled)
        {
            txtName.Enabled = enabled;
            cmbParent.Enabled = enabled;
            chkActive.Enabled = enabled && _selected?.Id != DefaultExpenseCategories.UncategorizedId;
        }

        private void OnSearchChanged(object? sender, EventArgs e) =>
            ReloadTree(_selected?.Id);

        private void OnTreeSelected(object? sender, TreeViewEventArgs e)
        {
            if (_loading)
                return;
            if (_dirty && !ConfirmDiscard())
            {
                _loading = true;
                ReloadTree(_selected?.Id);
                _loading = false;
                return;
            }

            if (e.Node?.Tag is Guid id)
                LoadEditor(id);
        }

        private void OnEditorChanged(object? sender, EventArgs e)
        {
            if (!_loading)
                _dirty = true;
        }

        private void OnSave(object? sender, EventArgs e)
        {
            if (_selected is null)
                return;
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show(this, "Category name is required.", "Category", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                _selected.Name = txtName.Text.Trim();
                _selected.ParentCategoryId = (cmbParent.SelectedItem as CategoryParentOption)?.Id;
                _selected.IsActive = chkActive.Checked;
                Orchestrator.UpdateCategory(_selected);
                NoteCatalogChanged();
                _dirty = false;
                ReloadTree(_selected.Id);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Category", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OnDeactivate(object? sender, EventArgs e)
        {
            if (_selected is null)
                return;

            if (MessageBox.Show(this,
                    $"Deactivate '{_selected.Name}'? It will stay on existing transactions but cannot be assigned to new ones.",
                    "Deactivate Category",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                Orchestrator.DeactivateCategory(_selected.Id);
                NoteCatalogChanged();
                _dirty = false;
                ReloadTree(_selected.Id);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Category", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OnAdd(object? sender, EventArgs e)
        {
            if (_dirty && !ConfirmDiscard())
                return;

            using var editor = new CategoryEditor(Orchestrator, _selected?.Id);
            if (editor.ShowDialog(FindForm()) != DialogResult.OK || editor.CreatedCategory is null)
                return;

            NoteCatalogChanged();
            ReloadTree(editor.CreatedCategory.Id);
        }

        private void OnImport(object? sender, EventArgs e)
        {
            if (_dirty && !ConfirmDiscard())
                return;

            using var fileDialog = new OpenFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx|CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Select category spreadsheet"
            };
            if (fileDialog.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            try
            {
                var import = new CategoryImportOrchestrator();
                var rows = import.LoadCategoriesFromFile(fileDialog.FileName);
                if (rows.Count == 0)
                {
                    MessageBox.Show(FindForm(), "The selected file did not contain any categories.", "Import Categories",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using var preview = new CategoryImportPreviewDialog(rows, import);
                if (preview.ShowDialog(FindForm()) != DialogResult.OK)
                    return;

                NoteCatalogChanged();
                ReloadTree(_selected?.Id);
                SetStatus(ImportStatusText.Imported(preview.Result, "category", "categories"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(FindForm(), $"Could not parse the file.\n{ex.Message}", "Import Categories",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OnMerge(object? sender, EventArgs e)
        {
            if (_dirty && !ConfirmDiscard())
                return;

            using var dialog = new CategoryMergeDialog(Orchestrator, _selected?.Id);
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            NoteCatalogChanged();
            ReloadTree(dialog.KeepCategoryId);
        }

        private void OnClose(object? sender, EventArgs e)
        {
            if (!TryLeave())
                return;

            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private void SetStatus(string text)
        {
            lblStatus.Text = text;
            lblStatus.Visible = !string.IsNullOrWhiteSpace(text);
        }

        private void NoteCatalogChanged()
        {
            CatalogHasChanged = true;
            CatalogChanged?.Invoke(this, EventArgs.Empty);
        }

        private bool ConfirmDiscard()
        {
            return MessageBox.Show(this, "Discard unsaved category changes?", "Category Manager",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        private static Bitmap CreateDot(Color color)
        {
            var bmp = new Bitmap(16, 16);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            using var brush = new SolidBrush(color);
            g.FillEllipse(brush, 3, 3, 10, 10);
            return bmp;
        }
    }
}
