using THMS.Domain.Finance.Transactions;

namespace THMS.Logic.Finance.Categories
{
    public static class CategoryRoleResolver
    {
        public static CategoryRole Resolve(Guid categoryId, IEnumerable<ExpenseCategory> categories) =>
            Resolve(categoryId, Map(categories));

        public static CategoryRole Resolve(Guid categoryId, IReadOnlyDictionary<Guid, CategoryRole> roles) =>
            roles.TryGetValue(categoryId, out var role) ? role : CategoryRole.Expense;

        public static Dictionary<Guid, CategoryRole> Map(IEnumerable<ExpenseCategory> categories)
        {
            var byId = new Dictionary<Guid, ExpenseCategory>();
            foreach (var category in categories)
                byId[category.Id] = category;

            var resolved = new Dictionary<Guid, CategoryRole>(byId.Count);
            var stack = new HashSet<Guid>();
            foreach (var id in byId.Keys)
                Walk(id, byId, resolved, stack);
            return resolved;
        }

        public static CategoryRole? UniformRole(
            IEnumerable<Guid> categoryIds,
            IReadOnlyList<ExpenseCategory> categories)
        {
            return UniformRole(categoryIds, Map(categories));
        }

        public static CategoryRole? UniformRole(
            IEnumerable<Guid> categoryIds,
            IReadOnlyDictionary<Guid, CategoryRole> roles)
        {
            var ids = categoryIds.Where(id => id != Guid.Empty).Distinct().ToList();
            if (ids.Count == 0)
                return null;

            CategoryRole? role = null;
            foreach (var id in ids)
            {
                var resolved = Resolve(id, roles);
                if (role is null)
                    role = resolved;
                else if (role != resolved)
                    return null;
            }

            return role;
        }

        private static CategoryRole Walk(
            Guid id,
            Dictionary<Guid, ExpenseCategory> byId,
            Dictionary<Guid, CategoryRole> resolved,
            HashSet<Guid> stack)
        {
            if (resolved.TryGetValue(id, out var cached))
                return cached;
            if (!stack.Add(id) || !byId.TryGetValue(id, out var category))
                return CategoryRole.Expense;

            CategoryRole role;
            if (category.Role is CategoryRole explicitRole)
                role = explicitRole;
            else if (category.ParentCategoryId is Guid parent)
                role = Walk(parent, byId, resolved, stack);
            else
                role = CategoryRole.Expense;

            stack.Remove(id);
            resolved[id] = role;
            return role;
        }
    }
}
