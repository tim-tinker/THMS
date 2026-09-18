using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Categories;

namespace THMS.Logic.ViewModels.Finance
{
    public sealed class CategoryFilterChoice
    {
        public CategoryFilterChoice(string name, Guid? categoryId, bool uncategorizedOnly = false)
        {
            Name = name;
            CategoryId = categoryId;
            UncategorizedOnly = uncategorizedOnly;
        }

        public string Name { get; }
        public Guid? CategoryId { get; }
        public bool UncategorizedOnly { get; }

        public static CategoryFilterChoice All { get; } = new("All categories", null);
        public static CategoryFilterChoice Uncategorized { get; } = new("Uncategorized", null, true);

        public static List<CategoryFilterChoice> ForCategories(IEnumerable<ExpenseCategory> categories)
        {
            var all = categories.ToList();
            var items = new List<CategoryFilterChoice> { All, Uncategorized };
            foreach (var category in ExpenseCategoryTree.Flatten(all, includeInactive: false))
                items.Add(new CategoryFilterChoice(ExpenseCategoryTree.IndentedName(all, category), category.Id));
            return items;
        }
    }
}
