using THMS.Data.Stores;
using THMS.Domain.Finance.Transactions;
using THMS.Ingestion.Importers.Finance;
using THMS.Logic.Finance.Categories;
using THMS.Logic.ViewModels;
using THMS.Logic.ViewModels.Finance;

namespace THMS.Logic.Orchestrators
{
    public class CategoryImportOrchestrator
    {
        private readonly ICategoryDataStore _categories;
        private readonly SpreadsheetCategoryImporter _importer;

        public CategoryImportOrchestrator()
            : this(new DataStoreFactory().GetCategoryStore())
        {
        }

        public CategoryImportOrchestrator(ICategoryDataStore categories)
            : this(categories, new SpreadsheetCategoryImporter())
        {
        }

        public CategoryImportOrchestrator(ICategoryDataStore categories, SpreadsheetCategoryImporter importer)
        {
            _categories = categories;
            _importer = importer;
        }

        public List<CategoryImportPreview> LoadCategoriesFromFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("A file path is required.");
            if (!File.Exists(path))
                throw new FileNotFoundException("The selected file was not found.", path);

            return _importer.Parse(path)
                .Select(row => new CategoryImportPreview
                {
                    Name = row.Name,
                    Parent = row.ParentName ?? ""
                })
                .ToList();
        }

        public ImportResult ImportCategories(IEnumerable<CategoryImportPreview> previewRows) =>
            ImportCategories(previewRows, progress: null);

        public ImportResult ImportCategories(
            IEnumerable<CategoryImportPreview> previewRows,
            IProgress<ImportProgress>? progress)
        {
            ArgumentNullException.ThrowIfNull(previewRows);
            var rows = previewRows as IReadOnlyList<CategoryImportPreview> ?? previewRows.ToList();
            _categories.EnsureDefaultCategories();
            var catalog = _categories.GetAllCategories(includeInactive: true).ToList();
            var total = rows.Count;
            ImportProgressReporter.Report(progress, 0, total, stride: 1);

            foreach (var parentName in rows
                         .Select(r => r.Parent?.Trim())
                         .Where(name => !string.IsNullOrWhiteSpace(name))
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                Upsert(catalog, parentName!, parentId: null, updateParent: false);
            }

            for (var i = 0; i < rows.Count; i++)
            {
                var name = rows[i].Name?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(name))
                {
                    ImportProgressReporter.Report(progress, i + 1, total, stride: 1);
                    continue;
                }

                Guid? parentId = null;
                var parentName = rows[i].Parent?.Trim() ?? "";
                if (!string.IsNullOrWhiteSpace(parentName) &&
                    !string.Equals(parentName, name, StringComparison.OrdinalIgnoreCase))
                {
                    var parent = Upsert(catalog, parentName, parentId: null, updateParent: false);
                    parentId = parent.Id;
                }

                Upsert(catalog, name, parentId, updateParent: true);
                ImportProgressReporter.Report(progress, i + 1, total, stride: 1);
            }

            return ImportResult.CountOnly(rows.Count(r => !string.IsNullOrWhiteSpace(r.Name)));
        }

        private ExpenseCategory Upsert(
            List<ExpenseCategory> catalog,
            string name,
            Guid? parentId,
            bool updateParent)
        {
            var existing = Find(catalog, name);
            if (existing is null)
            {
                var created = new ExpenseCategory
                {
                    Name = DefaultExpenseCategories.CanonicalName(name),
                    ParentCategoryId = parentId,
                    IsActive = true,
                    DisplayOrder = NextDisplayOrder(catalog)
                };
                _categories.AddCategory(created);
                catalog.Add(created);
                return created;
            }

            var changed = false;
            if (!existing.IsActive)
            {
                existing.IsActive = true;
                changed = true;
            }

            if (updateParent &&
                existing.Id != DefaultExpenseCategories.UncategorizedId &&
                existing.ParentCategoryId != parentId &&
                !ExpenseCategoryTree.WouldCreateCycle(catalog, existing.Id, parentId))
            {
                existing.ParentCategoryId = parentId;
                changed = true;
            }

            if (changed)
                _categories.UpdateCategory(existing);

            return existing;
        }

        private static ExpenseCategory? Find(IEnumerable<ExpenseCategory> catalog, string name)
        {
            var trimmed = name.Trim();
            var canonical = DefaultExpenseCategories.CanonicalName(trimmed);
            return catalog.FirstOrDefault(c =>
                string.Equals(c.Name, trimmed, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(c.Name, canonical, StringComparison.OrdinalIgnoreCase));
        }

        private static int NextDisplayOrder(IEnumerable<ExpenseCategory> catalog)
        {
            var max = catalog.Select(c => c.DisplayOrder).DefaultIfEmpty(0).Max();
            return Math.Max(max + 10, 200);
        }
    }
}
