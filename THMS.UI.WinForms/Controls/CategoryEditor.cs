using System.ComponentModel;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Controls
{
    public partial class CategoryEditor : Form
    {
        private CategoryOrchestrator? _orchestrator;

        public ExpenseCategory? CreatedCategory { get; private set; }

        public CategoryEditor()
        {
            InitializeComponent();
        }

        public CategoryEditor(CategoryOrchestrator orchestrator, Guid? preferredParentId = null)
            : this()
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;

            _orchestrator = orchestrator;
            BindParents(preferredParentId);
        }

        private CategoryOrchestrator Orchestrator =>
            _orchestrator ??= new CategoryOrchestrator();

        private void BindParents(Guid? preferredParentId)
        {
            var items = CategoryTreeUi.ParentOptions(Orchestrator.GetActiveCategories()).ToList();
            cmbParent.DisplayMember = nameof(CategoryParentOption.Name);
            cmbParent.ValueMember = nameof(CategoryParentOption.Id);
            cmbParent.DataSource = items;
            if (preferredParentId is Guid id)
            {
                var match = items.FirstOrDefault(i => i.Id == id);
                if (match is not null)
                    cmbParent.SelectedItem = match;
            }
        }

        private void OnSave(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show(this, "Category name is required.", "Category", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var parentId = (cmbParent.SelectedItem as CategoryParentOption)?.Id;
            CreatedCategory = Orchestrator.CreateCategory(txtName.Text.Trim(), parentId);
            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnCancel(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
