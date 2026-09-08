using THMS.Data.Stores;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Categories;

namespace THMS.Logic.Orchestrators
{
    public class CategoryOrchestrator
    {
        private readonly ICategoryDataStore _categories;
        private readonly ITransactionDataStore _transactions;
        private readonly Categorizer _categorizer;
        private readonly BudgetOrchestrator _budgets;

        public CategoryOrchestrator()
            : this(
                new DataStoreFactory().GetCategoryStore(),
                new DataStoreFactory().GetTransactionStore())
        {
        }

        public CategoryOrchestrator(ICategoryDataStore categories, ITransactionDataStore transactions)
        {
            _categories = categories;
            _transactions = transactions;
            _categorizer = new Categorizer(categories);
            _budgets = new BudgetOrchestrator(transactions);
        }

        public IReadOnlyList<ExpenseCategory> GetActiveCategories()
        {
            _categories.EnsureDefaultCategories();
            return _categories.GetAllCategories().ToList();
        }

        public IReadOnlyList<ExpenseCategory> GetAllCategories(bool includeInactive = true)
        {
            _categories.EnsureDefaultCategories();
            return _categories.GetAllCategories(includeInactive).ToList();
        }

        public IReadOnlyList<ExpenseCategory> GetCategoryTree(bool includeInactive = true)
        {
            _categories.EnsureDefaultCategories();
            return ExpenseCategoryTree.Flatten(_categories.GetCategoryTree(includeInactive), includeInactive);
        }

        public ExpenseCategory? GetCategory(Guid categoryId) =>
            _categories.GetCategory(categoryId);

        public CategoryUsage GetUsage(Guid categoryId) =>
            _categories.CountCategoryUsage(categoryId);

        public CategorySuggestion Suggest(PostedTransaction transaction) =>
            _categorizer.Suggest(transaction);

        public ExpenseCategory CreateCategory(string name, Guid? parentCategoryId = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Category name is required.");

            ValidateParent(Guid.Empty, parentCategoryId);

            var created = new ExpenseCategory
            {
                Name = name.Trim(),
                ParentCategoryId = parentCategoryId,
                IsActive = true,
                DisplayOrder = 200
            };
            _categories.AddCategory(created);
            return created;
        }

        public ExpenseCategory UpdateCategory(ExpenseCategory category)
        {
            ArgumentNullException.ThrowIfNull(category);
            if (string.IsNullOrWhiteSpace(category.Name))
                throw new ArgumentException("Category name is required.");

            ValidateParent(category.Id, category.ParentCategoryId);
            category.Name = category.Name.Trim();
            _categories.UpdateCategory(category);
            return category;
        }

        public void DeactivateCategory(Guid categoryId)
        {
            if (categoryId == DefaultExpenseCategories.UncategorizedId)
                throw new InvalidOperationException("The Uncategorized category cannot be deactivated.");

            _categories.DeactivateCategory(categoryId);
        }

        public void MergeCategories(Guid keepCategoryId, Guid retireCategoryId)
        {
            if (keepCategoryId == retireCategoryId)
                throw new InvalidOperationException("A category cannot be merged into itself.");
            if (retireCategoryId == DefaultExpenseCategories.UncategorizedId)
                throw new InvalidOperationException("The Uncategorized category cannot be merged away.");

            var keep = _categories.GetCategory(keepCategoryId)
                ?? throw new InvalidOperationException($"Category {keepCategoryId} was not found.");
            _ = _categories.GetCategory(retireCategoryId)
                ?? throw new InvalidOperationException($"Category {retireCategoryId} was not found.");

            _categories.MergeCategories(keep.Id, retireCategoryId);
            _budgets.RefreshAllActive();
        }

        public void AssignToPosted(Guid transactionId, Guid categoryId, bool learn = true)
        {
            var category = _categories.GetCategory(categoryId)
                ?? throw new InvalidOperationException($"Category {categoryId} was not found.");
            if (!category.IsActive)
                throw new InvalidOperationException($"Category '{category.Name}' is inactive and cannot be assigned.");

            var posted = _transactions.GetPostedTransaction(transactionId);
            if (posted is not null)
            {
                posted.ApplyCategory(category);
                _transactions.UpdatePostedTransaction(posted);
                if (learn)
                    _categorizer.Learn(posted.Description, category.Id);
                _budgets.RefreshAccount(posted.AccountId);
                return;
            }

            var transfer = _transactions.GetPostedTransferTransaction(transactionId);
            if (transfer is null)
                return;

            transfer.ApplyCategory(category);
            _transactions.UpdatePostedTransferTransaction(transfer);
            if (learn)
                _categorizer.Learn(transfer.Description, category.Id);
            _budgets.RefreshAccount(transfer.AccountId);
        }

        public ExpenseCategory GetOrCreate(string name, Guid? parentId = null) =>
            ExpenseCategoryTree.GetOrCreate(
                GetAllCategories(includeInactive: true),
                created =>
                {
                    _categories.AddCategory(created);
                    return created;
                },
                name,
                parentId);

        public HashSet<Guid> Expand(IEnumerable<Guid> selectedIds) =>
            ExpenseCategoryTree.ExpandWithDescendants(selectedIds, GetAllCategories(includeInactive: true));

        private void ValidateParent(Guid categoryId, Guid? parentCategoryId)
        {
            if (parentCategoryId is not Guid parentId)
                return;

            var parent = _categories.GetCategory(parentId)
                ?? throw new InvalidOperationException($"Parent category {parentId} was not found.");

            if (categoryId == Guid.Empty)
                return;

            var catalog = _categories.GetAllCategories(includeInactive: true).ToList();
            if (ExpenseCategoryTree.WouldCreateCycle(catalog, categoryId, parent.Id))
                throw new InvalidOperationException("A category cannot be nested under itself or one of its descendants.");
        }
    }
}
