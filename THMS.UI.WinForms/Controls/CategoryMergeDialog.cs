using System.ComponentModel;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Categories;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Controls
{
    public partial class CategoryMergeDialog : Form
    {
        private CategoryOrchestrator? _orchestrator;
        private Guid? _preferredSourceId;

        public Guid? KeepCategoryId { get; private set; }

        public CategoryMergeDialog()
        {
            InitializeComponent();
        }

        public CategoryMergeDialog(CategoryOrchestrator orchestrator, Guid? preferredSourceId = null)
            : this()
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;

            _orchestrator = orchestrator;
            _preferredSourceId = preferredSourceId;
            Bind();
        }

        private CategoryOrchestrator Orchestrator =>
            _orchestrator ??= new CategoryOrchestrator();

        private void Bind()
        {
            var all = Orchestrator.GetAllCategories(includeInactive: true).ToList();
            var items = ExpenseCategoryTree.Flatten(all, includeInactive: true)
                .Select(c => new MergeOption(ExpenseCategoryTree.IndentedName(all, c) + (c.IsActive ? "" : " (inactive)"), c.Id))
                .ToList();
            var sources = items.Where(i => i.Id != DefaultExpenseCategories.UncategorizedId).ToList();

            cmbSource.DisplayMember = nameof(MergeOption.Name);
            cmbSource.ValueMember = nameof(MergeOption.Id);
            cmbSource.DataSource = sources;

            cmbTarget.DisplayMember = nameof(MergeOption.Name);
            cmbTarget.ValueMember = nameof(MergeOption.Id);
            cmbTarget.DataSource = items.ToList();

            if (_preferredSourceId is Guid sourceId)
            {
                var source = sources.FirstOrDefault(i => i.Id == sourceId);
                if (source is not null)
                    cmbSource.SelectedItem = source;
            }
        }

        private void OnMerge(object? sender, EventArgs e)
        {
            if (cmbSource.SelectedItem is not MergeOption source || cmbTarget.SelectedItem is not MergeOption target)
            {
                MessageBox.Show(this, "Select both a source and a target category.", "Merge Categories",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (source.Id == target.Id)
            {
                MessageBox.Show(this, "Choose two different categories.", "Merge Categories",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var sourceName = Orchestrator.GetCategory(source.Id)?.Name ?? source.Name;
            var targetName = Orchestrator.GetCategory(target.Id)?.Name ?? target.Name;
            if (MessageBox.Show(this,
                    $"Merge '{sourceName}' into '{targetName}'? Existing transactions keep their dates and amounts; only the category changes.",
                    "Merge Categories",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                Orchestrator.MergeCategories(target.Id, source.Id);
                KeepCategoryId = target.Id;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Merge Categories", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OnCancel(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private sealed class MergeOption
        {
            public MergeOption(string name, Guid id)
            {
                Name = name;
                Id = id;
            }

            public string Name { get; }
            public Guid Id { get; }
        }
    }
}
