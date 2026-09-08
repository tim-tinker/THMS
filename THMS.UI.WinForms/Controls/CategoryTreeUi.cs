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
            item.Click += (_, _) => onAssign(category);
            foreach (var child in ExpenseCategoryTree.Children(categories, category.Id))
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
