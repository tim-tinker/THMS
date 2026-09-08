using THMS.Domain.Finance.Transactions;

namespace THMS.Data.Stores
{
    public interface ICategoryDataStore
    {
        void AddCategory(ExpenseCategory category);
        void UpdateCategory(ExpenseCategory category);
        void DeleteCategory(Guid categoryId);
        void DeactivateCategory(Guid categoryId);
        void MergeCategories(Guid keepCategoryId, Guid retireCategoryId);
        ExpenseCategory? GetCategory(Guid categoryId);
        IEnumerable<ExpenseCategory> GetAllCategories(bool includeInactive = false);
        IEnumerable<ExpenseCategory> GetChildCategories(Guid parentCategoryId);
        IEnumerable<ExpenseCategory> GetCategoryTree(bool includeInactive = true);
        CategoryUsage CountCategoryUsage(Guid categoryId);
        void EnsureDefaultCategories();

        void UpsertAssignment(string normalizedDescription, Guid categoryId);
        CategoryAssignment? GetAssignment(string normalizedDescription);
    }
}
