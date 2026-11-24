using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Orchestrators.Finance;
using THMS.UI.WinForms.Controls;

namespace THMS.Tests.UI
{
    [TestFixture]
    public class BillsControlTests
    {
        [Test]
        [Apartment(ApartmentState.STA)]
        public void SeveralBillsStaySelectedForDelete()
        {
            var accounts = new InMemoryAccountDataStore();
            var transactions = new InMemoryTransactionDataStore();
            var statements = new InMemoryAccountStatementDataStore();
            var checking = new BankAccount
            {
                Name = "Checking",
                Institution = "X",
                AccountNumber = "1",
                WebsiteUrl = "",
                PostedBalance = 0
            };
            accounts.UpsertAccount(checking);
            var orchestrator = new BillsOrchestrator(accounts, transactions, statements);
            var due = DateTime.Today;
            orchestrator.AddEnteredBill(
                checking.Id, "Comcast", -80m, DefaultExpenseCategories.UtilityId, due,
                creditAccountId: null, creditCategoryId: null, frequency: null);
            orchestrator.AddEnteredBill(
                checking.Id, "Water", -40m, DefaultExpenseCategories.UtilityId, due,
                creditAccountId: null, creditCategoryId: null, frequency: null);

            using var host = new Form { ClientSize = new Size(900, 500) };
            var bills = new BillsControl(orchestrator) { Dock = DockStyle.Fill };
            host.Controls.Add(bills);
            bills.SelectAccount(checking.Id);
            host.Show();
            try
            {
                var grid = All(bills).OfType<DataGridView>().Single(item => item.Name == "gridBills");
                Assert.That(grid.MultiSelect, Is.True);
                Assert.That(grid.SelectionMode, Is.EqualTo(DataGridViewSelectionMode.FullRowSelect));
                Assert.That(grid.Rows.Count, Is.EqualTo(2));

                grid.ClearSelection();
                grid.Rows[0].Selected = true;
                grid.Rows[1].Selected = true;
                Assert.That(grid.SelectedRows.Count, Is.EqualTo(2));
                Assert.That(All(bills).OfType<ThmsButton>().Single(button => button.Text == "Delete").Enabled, Is.True);

                var menu = grid.ContextMenuStrip!;
                menu.Show(grid, new Point(8, grid.ColumnHeadersHeight + 4));
                try
                {
                    Assert.That(menu.Items.OfType<ToolStripMenuItem>().Select(item => item.Text),
                        Is.EquivalentTo(new[] { "Delete 2 bills" }));
                }
                finally
                {
                    menu.Close();
                }

                grid.ClearSelection();
                grid.Rows[0].Selected = true;
                menu.Show(grid, new Point(8, grid.ColumnHeadersHeight + 4));
                try
                {
                    Assert.That(menu.Items.OfType<ToolStripMenuItem>().Select(item => item.Text),
                        Does.Contain("Delete"));
                    Assert.That(menu.Items.OfType<ToolStripMenuItem>().Select(item => item.Text),
                        Does.Contain("Edit"));
                }
                finally
                {
                    menu.Close();
                }
            }
            finally
            {
                host.Close();
            }
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
