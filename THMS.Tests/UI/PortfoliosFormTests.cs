using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Investments;
using THMS.Logic.Finance.Investments;
using THMS.UI.WinForms;

namespace THMS.Tests.UI
{
    [TestFixture]
    public class PortfoliosFormTests
    {
        [Test]
        [Apartment(ApartmentState.STA)]
        public void PortfolioColumns_FitEveryCashAmountAndPriceSource()
        {
            var accounts = new InMemoryAccountDataStore();
            var transactions = new InMemoryTransactionDataStore();
            accounts.UpsertAccount(new InvestmentAccount
            {
                Name = "Fidelity HSA",
                Institution = "Fidelity",
                AccountNumber = "257460702",
                CashBalance = 43418.22m,
                QuoteAccess = QuoteAccess.ImportOnly
            });
            accounts.UpsertAccount(new InvestmentAccount
            {
                Name = "Fidelity IRA",
                Institution = "Fidelity",
                AccountNumber = "257297693",
                CashBalance = 74.53m,
                QuoteAccess = QuoteAccess.Service
            });
            var catalog = new InvestmentPortfolioCatalog(accounts, new InMemoryInvestmentBookStore());
            var import = new InvestmentImportOrchestrator(
                accounts,
                new InMemoryInvestmentBookStore(),
                transactions,
                transactions);

            using var form = new PortfoliosForm(catalog, import);
            form.ClientSize = new Size(900, 600);
            form.Show();
            try
            {
                form.Refresh();
                Application.DoEvents();

                var grid = All(form).OfType<DataGridView>().Single(item =>
                    item.Columns.Cast<DataGridViewColumn>().Any(column => column.HeaderText == "Prices"));
                var cash = grid.Columns[nameof(InvestmentPortfolioRow.Cash)]!;
                var prices = grid.Columns[nameof(InvestmentPortfolioRow.PriceSource)]!;

                Assert.That(cash.AutoSizeMode, Is.EqualTo(DataGridViewAutoSizeColumnMode.AllCells));
                Assert.That(prices.AutoSizeMode, Is.EqualTo(DataGridViewAutoSizeColumnMode.AllCells));
                AssertFits(grid, cash, 43418.22m.ToString("c2"));
                AssertFits(grid, prices, "Import only");
                AssertFits(grid, prices, "Queryable");
            }
            finally
            {
                form.Close();
            }
        }

        private static void AssertFits(DataGridView grid, DataGridViewColumn column, string text)
        {
            var needed = TextRenderer.MeasureText(text, grid.Font, Size.Empty, TextFormatFlags.NoPadding).Width;
            Assert.That(column.Width, Is.GreaterThanOrEqualTo(needed), $"{column.HeaderText} clips '{text}'.");
        }

        private static IEnumerable<Control> All(Control root)
        {
            foreach (Control child in root.Controls)
            {
                yield return child;
                foreach (var nested in All(child))
                    yield return nested;
            }
        }
    }
}
