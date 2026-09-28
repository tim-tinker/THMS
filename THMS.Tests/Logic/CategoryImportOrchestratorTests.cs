using THMS.Data.Stores;
using THMS.Domain.Finance.Transactions;
using THMS.Ingestion.Importers.Finance;
using THMS.Logic.Finance.Budget;
using THMS.Logic.Orchestrators;
using THMS.Logic.ViewModels.Finance;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class CategoryImportOrchestratorTests
    {
        [Test]
        public void LoadCategoriesFromFile_ParsesOutlineCsv()
        {
            var path = Path.Combine(Path.GetTempPath(), $"categories-{Guid.NewGuid():N}.csv");
            File.WriteAllText(path, """
                Category,Child
                Income,
                ,Paycheck
                ,Interest Income
                Food,
                ,Groceries
                ,Restaurants
                Utility,
                ,Electricity
                """);
            try
            {
                var rows = new CategoryImportOrchestrator(new InMemoryTransactionDataStore())
                    .LoadCategoriesFromFile(path);

                Assert.That(rows.Select(r => r.Name), Is.EqualTo(new[]
                {
                    "Income", "Paycheck", "Interest Income", "Food", "Groceries", "Restaurants", "Utility", "Electricity"
                }));
                Assert.That(rows.Single(r => r.Name == "Income").Parent, Is.EqualTo(""));
                Assert.That(rows.Single(r => r.Name == "Paycheck").Parent, Is.EqualTo("Income"));
                Assert.That(rows.Single(r => r.Name == "Groceries").Parent, Is.EqualTo("Food"));
                Assert.That(rows.Single(r => r.Name == "Electricity").Parent, Is.EqualTo("Utility"));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void ParseRows_NameParentAllowsNestedLevelsAndBudgetColumns()
        {
            var rows = SpreadsheetCategoryImporter.ParseRows(
            [
                ["Category", "Parent", "Frequency", "Amount", "Period Start"],
                ["Food", null, "Monthly", "400", null],
                ["Groceries", "Food", null, null, null],
                ["Produce", "Groceries", null, null, null],
                ["Paycheck", "Income", "Biweekly", "150", "9/25/2026"]
            ]);

            Assert.That(rows.Single(r => r.Name == "Food").ParentName, Is.Null);
            Assert.That(rows.Single(r => r.Name == "Groceries").ParentName, Is.EqualTo("Food"));
            Assert.That(rows.Single(r => r.Name == "Produce").ParentName, Is.EqualTo("Groceries"));
            Assert.That(rows.Single(r => r.Name == "Food").Frequency, Is.EqualTo(BudgetFrequency.Monthly));
            Assert.That(rows.Single(r => r.Name == "Food").Amount, Is.EqualTo(400m));
            var paycheck = rows.Single(r => r.Name == "Paycheck");
            Assert.That(paycheck.ParentName, Is.EqualTo("Income"));
            Assert.That(paycheck.Frequency, Is.EqualTo(BudgetFrequency.Biweekly));
            Assert.That(paycheck.Amount, Is.EqualTo(150m));
            Assert.That(paycheck.PeriodStart, Is.EqualTo(new DateTime(2026, 9, 25)));
        }

        [Test]
        public void LoadCategoriesFromFile_ParsesNameParentCsv()
        {
            var path = Path.Combine(Path.GetTempPath(), $"categories-{Guid.NewGuid():N}.csv");
            File.WriteAllText(path, """
                Name,Parent
                Travel,
                Travel Food,Travel
                """);
            try
            {
                var rows = new CategoryImportOrchestrator(new InMemoryTransactionDataStore())
                    .LoadCategoriesFromFile(path);

                Assert.That(rows, Has.Count.EqualTo(2));
                Assert.That(rows[0].Name, Is.EqualTo("Travel"));
                Assert.That(rows[0].Parent, Is.EqualTo(""));
                Assert.That(rows[1].Name, Is.EqualTo("Travel Food"));
                Assert.That(rows[1].Parent, Is.EqualTo("Travel"));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void LoadCategoriesFromFile_ThrowsWhenMissing()
        {
            var orchestrator = new CategoryImportOrchestrator(new InMemoryTransactionDataStore());
            Assert.That(
                () => orchestrator.LoadCategoriesFromFile(@"C:\missing\categories.xlsx"),
                Throws.TypeOf<FileNotFoundException>());
        }

        [Test]
        public void ImportCategories_CreatesParentsAndChildrenAndReusesDefaults()
        {
            var store = new InMemoryTransactionDataStore();
            var orchestrator = new CategoryImportOrchestrator(store);
            orchestrator.ImportCategories(
            [
                new() { Name = "Food" },
                new() { Name = "Groceries", Parent = "Food" },
                new() { Name = "Electric", Parent = "Utility" },
                new() { Name = "Travel" },
                new() { Name = "Travel Food", Parent = "Travel" }
            ]);

            var catalog = store.GetAllCategories(includeInactive: true).ToList();
            var food = catalog.Single(c => c.Name == "Food");
            var groceries = catalog.Single(c => c.Id == DefaultExpenseCategories.GroceriesId);
            var electricity = catalog.Single(c => c.Id == DefaultExpenseCategories.ElectricId);
            var travel = catalog.Single(c => c.Name == "Travel");
            var travelFood = catalog.Single(c => c.Name == "Travel Food");

            Assert.That(food.ParentCategoryId, Is.Null);
            Assert.That(groceries.ParentCategoryId, Is.EqualTo(food.Id));
            Assert.That(electricity.ParentCategoryId, Is.EqualTo(DefaultExpenseCategories.UtilityId));
            Assert.That(travelFood.ParentCategoryId, Is.EqualTo(travel.Id));
        }

        [Test]
        public void ImportCategories_CreatesNestedParentsAndParentBudgetIncludesChildren()
        {
            var store = new InMemoryTransactionDataStore();
            var orchestrator = new CategoryImportOrchestrator(store);
            orchestrator.ImportCategories(
            [
                new() { Name = "Food", Frequency = "Monthly", Amount = 400 },
                new() { Name = "Groceries", Parent = "Food" },
                new() { Name = "Produce", Parent = "Groceries" },
                new() { Name = "Paycheck", Parent = "Income", Frequency = "Biweekly", Amount = 150, PeriodStart = new DateTime(2026, 9, 25) },
                new() { Name = "Produce", Parent = "Groceries", Frequency = "Weekly", Amount = 50 }
            ]);

            var catalog = store.GetAllCategories(includeInactive: true).ToList();
            var food = catalog.Single(c => c.Name == "Food");
            var groceries = catalog.Single(c => c.Name == "Groceries");
            var produce = catalog.Single(c => c.Name == "Produce");
            Assert.That(groceries.ParentCategoryId, Is.EqualTo(food.Id));
            Assert.That(produce.ParentCategoryId, Is.EqualTo(groceries.Id));

            var rules = store.GetAllExpenseBudgetRules().ToList();
            Assert.That(rules, Has.Count.EqualTo(2));
            var foodBudget = rules.Single(r => r.BudgetName == "Food");
            Assert.That(foodBudget.BudgetFrequency, Is.EqualTo(BudgetFrequency.Monthly));
            Assert.That(foodBudget.DefaultBudgetAmount, Is.EqualTo(400m));
            Assert.That(foodBudget.IncludedCategoryIds, Is.EquivalentTo(new[] { food.Id, groceries.Id, produce.Id }));
            Assert.That(rules.Any(r => r.BudgetName == "Produce"), Is.False);

            var paycheck = rules.Single(r => r.BudgetName == "Paycheck");
            Assert.That(paycheck.BudgetFrequency, Is.EqualTo(BudgetFrequency.Biweekly));
            Assert.That(paycheck.PeriodStart, Is.EqualTo(new DateTime(2026, 9, 25)));
            var expected = BudgetPeriodCalculator.PeriodContaining(
                DateTime.Today,
                BudgetFrequency.Biweekly,
                paycheck.PeriodStart);
            var period = store.GetActiveBudgetHistory(paycheck.Id);
            Assert.That(period, Is.Not.Null);
            Assert.That(period!.PeriodStart, Is.EqualTo(expected.Start));
            Assert.That(period.PeriodEnd, Is.EqualTo(expected.End));
        }

        [Test]
        public void ImportCategories_ReactivatesInactiveCategory()
        {
            var store = new InMemoryTransactionDataStore();
            store.EnsureDefaultCategories();
            store.AddCategory(new ExpenseCategory { Name = "Pets", IsActive = false, DisplayOrder = 200 });
            var orchestrator = new CategoryImportOrchestrator(store);

            orchestrator.ImportCategories([new() { Name = "Pets" }]);

            Assert.That(store.GetAllCategories().Single(c => c.Name == "Pets").IsActive, Is.True);
        }

        [Test]
        public void LoadCategoriesFromFile_ParsesHouseholdCategoryListWorkbook()
        {
            var path = new[]
            {
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "My Household Management System",
                    "Records",
                    "Budget",
                    "Category List.xlsx"),
                @"c:\Users\timti\OneDrive\My Documents\My Household Management System\Records\Budget\Category List.xlsx"
            }.FirstOrDefault(File.Exists);
            if (path is null)
                Assert.Ignore("Category List.xlsx was not found.");

            List<CategoryImportPreview> rows;
            try
            {
                rows = new CategoryImportOrchestrator(new InMemoryTransactionDataStore())
                    .LoadCategoriesFromFile(path);
            }
            catch (IOException)
            {
                Assert.Ignore("Category List.xlsx is in use.");
                return;
            }

            Assert.That(rows, Has.Count.GreaterThan(50));
            Assert.That(rows.Any(r => r.Name == "Income" && r.Parent == ""), Is.True);
            Assert.That(rows.Any(r => r.Name == "Paycheck" && r.Parent == "Income"), Is.True);
            Assert.That(rows.Any(r => r.Name == "Groceries" && r.Parent == "Food"), Is.True);
        }
    }
}
