using System.ComponentModel;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Controls
{
    public partial class CategoryEditor : Form
    {
        private CategoryOrchestrator? _orchestrator;
        private ExpenseCategory? _existing;

        public ExpenseCategory? CreatedCategory { get; private set; }

        public CategoryEditor()
        {
            InitializeComponent();
        }

        public CategoryEditor(CategoryOrchestrator orchestrator, Guid? preferredParentId = null)
            : this(orchestrator, existing: null, preferredParentId)
        {
        }

        public CategoryEditor(CategoryOrchestrator orchestrator, ExpenseCategory existing)
            : this(orchestrator, existing, preferredParentId: existing.ParentCategoryId)
        {
        }

        private CategoryEditor(
            CategoryOrchestrator orchestrator,
            ExpenseCategory? existing,
            Guid? preferredParentId)
            : this()
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;

            _orchestrator = orchestrator;
            _existing = existing;
            Bind(preferredParentId);
        }

        private CategoryOrchestrator Orchestrator =>
            _orchestrator ??= new CategoryOrchestrator();

        private void Bind(Guid? preferredParentId)
        {
            var excludeId = _existing?.Id;
            var items = CategoryTreeUi.ParentOptions(Orchestrator.GetActiveCategories(), excludeId).ToList();
            if (_existing?.ParentCategoryId is Guid parentId &&
                items.All(i => i.Id != parentId) &&
                Orchestrator.GetCategory(parentId) is ExpenseCategory parent)
            {
                items.Add(new CategoryParentOption(parent.Name, parent.Id));
            }

            cmbParent.DisplayMember = nameof(CategoryParentOption.Name);
            cmbParent.ValueMember = nameof(CategoryParentOption.Id);
            cmbParent.DataSource = items;
            cmbParent.SelectedIndexChanged += (_, _) => BindRole();

            if (_existing is null)
            {
                Text = "New Category";
                chkActive.Checked = true;
                chkActive.Visible = false;
                btnDeactivate.Visible = false;
                if (preferredParentId is Guid id)
                {
                    var match = items.FirstOrDefault(i => i.Id == id);
                    if (match is not null)
                        cmbParent.SelectedItem = match;
                }

                BindRole();
                return;
            }

            Text = "Edit Category";
            txtName.Text = _existing.Name;
            chkActive.Visible = true;
            chkActive.Checked = _existing.IsActive;
            chkActive.Enabled = _existing.Id != DefaultExpenseCategories.UncategorizedId;
            btnDeactivate.Visible = true;
            btnDeactivate.Enabled = _existing.IsActive && _existing.Id != DefaultExpenseCategories.UncategorizedId;
            var selectedParent = items.FirstOrDefault(i => i.Id == _existing.ParentCategoryId);
            cmbParent.SelectedItem = selectedParent ?? items[0];
            BindRole();
        }

        private void BindRole()
        {
            var hasParent = (cmbParent.SelectedItem as CategoryParentOption)?.Id is Guid;
            var options = new List<RoleChoice>();
            if (hasParent)
                options.Add(new RoleChoice(null, "Inherit from parent"));
            options.Add(new RoleChoice(CategoryRole.Expense, "Expense"));
            options.Add(new RoleChoice(CategoryRole.Income, "Income"));
            options.Add(new RoleChoice(CategoryRole.Transfer, "Transfer"));
            options.Add(new RoleChoice(CategoryRole.Investment, "Investment"));

            var selected = _existing?.Role;
            if (cmbRole.SelectedItem is RoleChoice current && options.Any(option => option.Role == current.Role))
                selected = current.Role;
            if (!hasParent && selected is null)
                selected = CategoryRole.Expense;

            cmbRole.DisplayMember = nameof(RoleChoice.Label);
            cmbRole.DataSource = options;
            cmbRole.SelectedItem = options.FirstOrDefault(option => option.Role == selected) ?? options[0];
        }

        private CategoryRole? SelectedRole() =>
            (cmbRole.SelectedItem as RoleChoice)?.Role;

        private void OnSave(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show(this, "Category name is required.", "Category", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var parentId = (cmbParent.SelectedItem as CategoryParentOption)?.Id;
                var role = SelectedRole();
                if (_existing is null)
                {
                    CreatedCategory = Orchestrator.CreateCategory(txtName.Text.Trim(), parentId, role);
                }
                else
                {
                    _existing.Name = txtName.Text.Trim();
                    _existing.ParentCategoryId = parentId;
                    _existing.IsActive = chkActive.Checked;
                    _existing.Role = role;
                    CreatedCategory = Orchestrator.UpdateCategory(_existing);
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                ExceptionDialog.Show(this, "Category", ex);
            }
        }

        private void OnDeactivate(object? sender, EventArgs e)
        {
            if (_existing is null)
                return;

            if (MessageBox.Show(this,
                    $"Deactivate '{_existing.Name}'? It will stay on existing transactions but cannot be assigned to new ones.",
                    "Deactivate Category",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                Orchestrator.DeactivateCategory(_existing.Id);
                CreatedCategory = Orchestrator.GetCategory(_existing.Id);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                ExceptionDialog.Show(this, "Category", ex);
            }
        }

        private void OnCancel(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private sealed class RoleChoice(CategoryRole? role, string label)
        {
            public CategoryRole? Role { get; } = role;
            public string Label { get; } = label;
        }
    }
}
