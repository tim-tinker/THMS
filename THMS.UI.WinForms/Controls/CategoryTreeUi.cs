using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Categories;

namespace THMS.UI.WinForms.Controls
{
    internal static class CategoryTreeUi
    {
        public static void Fill(
            TreeView tree,
            IEnumerable<ExpenseCategory> categories,
            HashSet<Guid>? checkedIds = null,
            bool includeInactive = false,
            Guid? selectedId = null)
        {
            var all = categories.ToList();
            tree.BeginUpdate();
            tree.Nodes.Clear();
            foreach (var root in ExpenseCategoryTree.Roots(all, includeInactive))
                tree.Nodes.Add(CreateNode(all, root, checkedIds, includeInactive));

            tree.ExpandAll();
            if (selectedId is Guid id)
            {
                var node = Find(tree.Nodes, id);
                if (node is not null)
                {
                    tree.SelectedNode = node;
                    node.EnsureVisible();
                }
            }

            tree.EndUpdate();
        }

        public static IEnumerable<Guid> CheckedIds(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Checked && node.Tag is Guid id)
                    yield return id;
                foreach (var childId in CheckedIds(node.Nodes))
                    yield return childId;
            }
        }

        public static List<CategoryListItem> ComboItems(
            IEnumerable<ExpenseCategory> categories,
            bool includeSplit)
        {
            var all = categories.ToList();
            var items = new List<CategoryListItem>();
            if (includeSplit)
                items.Add(CategoryListItem.Split);

            foreach (var category in ExpenseCategoryTree.Flatten(all, includeInactive: false))
            {
                items.Add(new CategoryListItem(
                    ExpenseCategoryTree.IndentedName(all, category),
                    category.Id,
                    CategoryListAction.None));
            }

            items.Add(CategoryListItem.NewCategory);
            items.Add(CategoryListItem.ManageCategories);
            return items;
        }

        public static ContextMenuStrip CreateAssignMenu(
            IReadOnlyList<ExpenseCategory> categories,
            Guid? currentId,
            Guid? suggestedId,
            Action<ExpenseCategory> onAssign,
            Action onNewCategory,
            Action onManageCategories,
            Action? onSplit = null,
            bool splitSelected = false)
        {
            var menu = new ContextMenuStrip();
            if (onSplit is not null)
            {
                var split = new ToolStripMenuItem("Split")
                {
                    Checked = splitSelected
                };
                split.Click += (_, _) => onSplit();
                menu.Items.Add(split);
                menu.Items.Add(new ToolStripSeparator());
            }

            foreach (var root in ExpenseCategoryTree.Roots(categories))
                menu.Items.Add(CreateMenuItem(categories, root, currentId, suggestedId, onAssign));

            menu.Items.Add(new ToolStripSeparator());
            var newItem = new ToolStripMenuItem("New Category…");
            newItem.Click += (_, _) => onNewCategory();
            menu.Items.Add(newItem);
            var manageItem = new ToolStripMenuItem("Manage Categories…");
            manageItem.Click += (_, _) => onManageCategories();
            menu.Items.Add(manageItem);
            return menu;
        }

        public static ToolStripMenuItem CreateSetCategoryItem(
            IReadOnlyList<ExpenseCategory> categories,
            int count,
            Guid? currentId,
            Action<ExpenseCategory> onAssign,
            Action? onNewCategory,
            Action? onManageCategories)
        {
            var item = new ToolStripMenuItem(count > 1 ? $"Set Category ({count})" : "Set Category");
            foreach (var root in ExpenseCategoryTree.Roots(categories))
                item.DropDownItems.Add(CreateMenuItem(categories, root, currentId, suggestedId: null, onAssign));

            if (onNewCategory is not null || onManageCategories is not null)
                item.DropDownItems.Add(new ToolStripSeparator());
            if (onNewCategory is not null)
            {
                var create = new ToolStripMenuItem("New Category…");
                create.Click += (_, _) => onNewCategory();
                item.DropDownItems.Add(create);
            }

            if (onManageCategories is not null)
            {
                var manage = new ToolStripMenuItem("Manage Categories…");
                manage.Click += (_, _) => onManageCategories();
                item.DropDownItems.Add(manage);
            }

            return item;
        }

        public static ToolStripMenuItem CreateMenuItem(
            IReadOnlyList<ExpenseCategory> categories,
            ExpenseCategory category,
            Guid? currentId,
            Guid? suggestedId,
            Action<ExpenseCategory> onAssign)
        {
            var text = category.Name;
            if (currentId is null && suggestedId == category.Id)
                text += " (suggested)";

            var item = new ToolStripMenuItem(text)
            {
                Tag = category.Id,
                Checked = currentId == category.Id
            };
            var children = ExpenseCategoryTree.Children(categories, category.Id).ToList();
            if (children.Count == 0)
            {
                item.Click += (_, _) => onAssign(category);
                return item;
            }

            // A parent item with a submenu does not raise Click. Offer the parent
            // itself as the first entry so it can be assigned.
            var self = new ToolStripMenuItem(category.Name)
            {
                Tag = category.Id,
                Checked = currentId == category.Id
            };
            self.Click += (_, _) => onAssign(category);
            item.DropDownItems.Add(self);
            item.DropDownItems.Add(new ToolStripSeparator());
            foreach (var child in children)
                item.DropDownItems.Add(CreateMenuItem(categories, child, currentId, suggestedId, onAssign));
            return item;
        }

        public static IReadOnlyList<CategoryParentOption> ParentOptions(
            IEnumerable<ExpenseCategory> categories,
            Guid? excludeId = null)
        {
            var all = categories.ToList();
            var items = new List<CategoryParentOption> { new("(none)", null) };
            foreach (var category in ExpenseCategoryTree.Flatten(all, includeInactive: false))
            {
                if (excludeId is Guid id &&
                    (category.Id == id || ExpenseCategoryTree.IsAncestor(all, ancestorId: id, nodeId: category.Id)))
                    continue;

                items.Add(new CategoryParentOption(ExpenseCategoryTree.IndentedName(all, category), category.Id));
            }

            return items;
        }

        private static TreeNode CreateNode(
            IReadOnlyList<ExpenseCategory> categories,
            ExpenseCategory category,
            HashSet<Guid>? checkedIds,
            bool includeInactive)
        {
            var text = category.IsActive ? category.Name : $"{category.Name} (inactive)";
            var node = new TreeNode(text)
            {
                Tag = category.Id,
                Checked = checkedIds?.Contains(category.Id) == true,
                ImageKey = category.IsActive ? "active" : "inactive",
                SelectedImageKey = category.IsActive ? "active" : "inactive",
                ForeColor = category.IsActive ? SystemColors.ControlText : Color.Gray
            };
            foreach (var child in ExpenseCategoryTree.Children(categories, category.Id, includeInactive))
                node.Nodes.Add(CreateNode(categories, child, checkedIds, includeInactive));
            return node;
        }

        private static TreeNode? Find(TreeNodeCollection nodes, Guid id)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Tag is Guid nodeId && nodeId == id)
                    return node;
                var child = Find(node.Nodes, id);
                if (child is not null)
                    return child;
            }

            return null;
        }
    }

    internal enum CategoryListAction
    {
        None,
        Split,
        NewCategory,
        ManageCategories
    }

    internal sealed class CategoryListItem
    {
        public static CategoryListItem Split { get; } = new("Split", null, CategoryListAction.Split);
        public static CategoryListItem NewCategory { get; } = new("New Category…", null, CategoryListAction.NewCategory);
        public static CategoryListItem ManageCategories { get; } = new("Manage Categories…", null, CategoryListAction.ManageCategories);

        public CategoryListItem(string label, Guid? categoryId, CategoryListAction action)
        {
            Label = label;
            CategoryId = categoryId;
            Action = action;
        }

        public string Label { get; }
        public Guid? CategoryId { get; }
        public CategoryListAction Action { get; }
    }

    internal sealed class CategoryParentOption
    {
        public CategoryParentOption(string name, Guid? id)
        {
            Name = name;
            Id = id;
        }

        public string Name { get; }
        public Guid? Id { get; }
    }
}
