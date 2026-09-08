using THMS.Data.Stores;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Categories;

namespace THMS.Logic.Orchestrators
{
    public class CategorySuggestion
    {
        public Guid? CategoryId { get; init; }
        public string? CategoryName { get; init; }
        public double Confidence { get; init; }
        public string Reason { get; init; } = "";
    }

    public class Categorizer
    {
        private readonly ICategoryDataStore _store;

        public Categorizer()
            : this(new DataStoreFactory().GetCategoryStore())
        {
        }

        public Categorizer(ICategoryDataStore store)
        {
            _store = store;
        }

        public static string Normalize(string? description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return "";

            return string.Join(
                " ",
                description.Trim().ToUpperInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        }

        public CategorySuggestion Suggest(PostedTransaction transaction)
        {
            _store.EnsureDefaultCategories();
            var categories = _store.GetAllCategories(includeInactive: true).ToList();

            var key = Normalize(transaction.Description);
            if (!string.IsNullOrEmpty(key))
            {
                var learned = _store.GetAssignment(key);
                if (learned is not null)
                {
                    var category = _store.GetCategory(learned.CategoryId);
                    if (category is { IsActive: true })
                    {
                        return new CategorySuggestion
                        {
                            CategoryId = category.Id,
                            CategoryName = category.Name,
                            Confidence = 0.95,
                            Reason = "Learned from previous assignment"
                        };
                    }
                }
            }

            var patterned = MatchPattern(transaction, categories);
            if (patterned is not null)
                return patterned;

            if (!string.IsNullOrWhiteSpace(transaction.PlaidCategory))
            {
                var fromPlaid = GetOrCreate(transaction.PlaidCategory);
                return new CategorySuggestion
                {
                    CategoryId = fromPlaid.Id,
                    CategoryName = fromPlaid.Name,
                    Confidence = 0.7,
                    Reason = "Plaid category"
                };
            }

            var uncategorized = categories.First(c => c.Id == DefaultExpenseCategories.UncategorizedId);
            return new CategorySuggestion
            {
                CategoryId = uncategorized.Id,
                CategoryName = uncategorized.Name,
                Confidence = 0.2,
                Reason = "Default"
            };
        }

        public void ApplySuggestion(PostedTransaction transaction, bool overwriteExisting = false)
        {
            if (transaction.CategoryId is Guid && !overwriteExisting)
                return;

            var suggestion = Suggest(transaction);
            if (suggestion.CategoryId is not Guid id)
                return;

            var category = _store.GetCategory(id);
            transaction.ApplyCategory(category);
        }

        public void ApplyCategories(List<PostedTransaction> transactions)
        {
            foreach (var transaction in transactions)
                ApplySuggestion(transaction, overwriteExisting: true);
        }

        public void Learn(string? description, Guid categoryId)
        {
            var key = Normalize(description);
            if (string.IsNullOrEmpty(key))
                return;

            _store.UpsertAssignment(key, categoryId);
        }

        public ExpenseCategory GetOrCreate(string name, Guid? parentId = null)
        {
            _store.EnsureDefaultCategories();
            var existing = _store.GetAllCategories(includeInactive: true).ToList();
            return ExpenseCategoryTree.GetOrCreate(
                existing,
                created =>
                {
                    _store.AddCategory(created);
                    return created;
                },
                name,
                parentId);
        }

        private static CategorySuggestion? MatchPattern(
            PostedTransaction transaction,
            List<ExpenseCategory> categories)
        {
            var description = transaction.Description ?? "";

            if (Contains(description, "Amazon"))
                return Named(categories, DefaultExpenseCategories.Restaurants, 0.85, "Merchant pattern");
            if (Contains(description, "Walmart"))
                return Named(categories, DefaultExpenseCategories.Groceries, 0.85, "Merchant pattern");
            if (Contains(description, "Starbucks"))
                return Named(categories, DefaultExpenseCategories.Restaurants, 0.85, "Merchant pattern");
            if (Contains(description, "Green Mountain E") || Contains(description, "Green Mountain Power"))
                return Named(categories, DefaultExpenseCategories.Electricity, 0.85, "Merchant pattern");
            if (transaction.Amount < 0 && Contains(description, "Payment"))
                return Named(categories, DefaultExpenseCategories.Payment, 0.8, "Merchant pattern");

            return null;
        }

        private static CategorySuggestion? Named(
            List<ExpenseCategory> categories,
            string name,
            double confidence,
            string reason)
        {
            var category = FindOrHint(categories, name);
            if (category is null)
                return null;

            return new CategorySuggestion
            {
                CategoryId = category.Id,
                CategoryName = category.Name,
                Confidence = confidence,
                Reason = reason
            };
        }

        private static ExpenseCategory? FindOrHint(List<ExpenseCategory> categories, string name)
        {
            var canonical = DefaultExpenseCategories.CanonicalName(name);
            return categories
                .Where(c => string.Equals(c.Name, canonical, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(c => c.IsActive)
                .FirstOrDefault();
        }

        private static bool Contains(string description, string token) =>
            description.Contains(token, StringComparison.OrdinalIgnoreCase);
    }
}
