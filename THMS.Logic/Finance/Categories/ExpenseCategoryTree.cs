using THMS.Domain.Finance.Transactions;

namespace THMS.Logic.Finance.Categories
{
    public static class ExpenseCategoryTree
    {
        public static IEnumerable<ExpenseCategory> Roots(
            IEnumerable<ExpenseCategory> categories,
            bool includeInactive = false)
        {
            var visible = Visible(categories, includeInactive);
            var ids = visible.Select(c => c.Id).ToHashSet();
            return visible
                .Where(c => c.ParentCategoryId is null || !ids.Contains(c.ParentCategoryId.Value))
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name);
        }

        public static IEnumerable<ExpenseCategory> Children(
            IEnumerable<ExpenseCategory> categories,
            Guid parentId,
            bool includeInactive = false) =>
            Visible(categories, includeInactive)
                .Where(c => c.ParentCategoryId == parentId)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name);

        public static List<ExpenseCategory> Flatten(
            IEnumerable<ExpenseCategory> categories,
            bool includeInactive = true)
        {
            var all = categories.ToList();
            var result = new List<ExpenseCategory>();
            var seen = new HashSet<Guid>();

            void Visit(ExpenseCategory node)
            {
                if (!seen.Add(node.Id))
                    return;

                result.Add(node);
                foreach (var child in Children(all, node.Id, includeInactive))
                    Visit(child);
            }

            foreach (var root in Roots(all, includeInactive))
                Visit(root);

            foreach (var leftover in Visible(all, includeInactive).Where(c => !seen.Contains(c.Id)))
                Visit(leftover);

            return result;
        }

        public static HashSet<Guid> ExpandWithDescendants(
            IEnumerable<Guid> selectedIds,
            IEnumerable<ExpenseCategory> categories)
        {
            var all = categories.ToList();
            var expanded = selectedIds.ToHashSet();
            bool added;
            do
            {
                added = false;
                foreach (var category in all)
                {
                    if (category.ParentCategoryId is Guid parent &&
                        expanded.Contains(parent) &&
                        expanded.Add(category.Id))
                    {
                        added = true;
                    }
                }
            } while (added);

            return expanded;
        }

        public static bool WouldCreateCycle(
            IEnumerable<ExpenseCategory> categories,
            Guid categoryId,
            Guid? newParentId)
        {
            if (newParentId is not Guid parentId)
                return false;
            if (parentId == categoryId)
                return true;

            return IsAncestor(categories, ancestorId: categoryId, nodeId: parentId);
        }

        public static bool IsAncestor(
            IEnumerable<ExpenseCategory> categories,
            Guid ancestorId,
            Guid nodeId)
        {
            var byId = categories.ToDictionary(c => c.Id);
            var seen = new HashSet<Guid>();
            var currentId = nodeId;
            while (byId.TryGetValue(currentId, out var current) && seen.Add(current.Id))
            {
                if (current.ParentCategoryId is not Guid parentId)
                    return false;
                if (parentId == ancestorId)
                    return true;
                currentId = parentId;
            }

            return false;
        }

        public static int Depth(IEnumerable<ExpenseCategory> categories, ExpenseCategory category)
        {
            var byId = categories.ToDictionary(c => c.Id);
            var depth = 0;
            var seen = new HashSet<Guid>();
            var current = category;
            while (current.ParentCategoryId is Guid parentId &&
                   seen.Add(current.Id) &&
                   byId.TryGetValue(parentId, out current!))
            {
                depth++;
            }

            return depth;
        }

        public static string IndentedName(IEnumerable<ExpenseCategory> categories, ExpenseCategory category) =>
            new string(' ', Depth(categories, category) * 2) + category.Name;

        public static ExpenseCategory GetOrCreate(
            IEnumerable<ExpenseCategory> existing,
            Func<ExpenseCategory, ExpenseCategory> add,
            string name,
            Guid? parentId = null)
        {
            var canonical = DefaultExpenseCategories.CanonicalName(name);
            var match = existing
                .Where(c => string.Equals(c.Name, canonical, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(c => c.IsActive)
                .FirstOrDefault();
            if (match is not null)
                return match;

            var created = new ExpenseCategory
            {
                Name = canonical,
                ParentCategoryId = parentId,
                IsActive = true,
                DisplayOrder = 200
            };
            return add(created);
        }

        private static List<ExpenseCategory> Visible(IEnumerable<ExpenseCategory> categories, bool includeInactive) =>
            categories.Where(c => includeInactive || c.IsActive).ToList();
    }
}
