using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Planning;
using THMS.Logic.Finance.Planning;
using THMS.Logic.ViewModels.Finance;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class StatementBalanceValidationTests
    {
        [Test]
        public void Build_AddsEachAmountToThePreviousStatementBalance()
        {
            var account = new BankAccount { Name = "Checking" };
            var previous = new BankStatement
            {
                StatementDate = new DateTime(2026, 9, 8),
                StatementBalance = 1000m
            };
            var statement = new BankStatement
            {
                StatementDate = new DateTime(2026, 10, 8),
                StatementBalance = 900m
            };
            var deposit = Transaction(new DateTime(2026, 10, 1), 40m, "Deposit");
            var groceries = Transaction(new DateTime(2026, 10, 1), -100m, "Groceries");
            var later = Transaction(new DateTime(2026, 10, 3), -20m, "Fuel");

            var validation = StatementBalanceValidation.Build(
                account, statement, previous, [groceries, later, deposit]);

            Assert.That(validation.OpeningBalance, Is.EqualTo(1000m));
            Assert.That(validation.Rows.Select(row => row.Description), Is.EqualTo(new[] { "Deposit", "Groceries", "Fuel" }));
            Assert.That(validation.Rows.Select(row => row.Balance), Is.EqualTo(new[] { 1040m, 940m, 920m }));
            Assert.That(validation.EndingBalance, Is.EqualTo(920m));
            Assert.That(validation.Difference, Is.EqualTo(20m));
        }

        [Test]
        public void Build_InvertsCreditAmountsSoChargesIncreaseTheAmountOwed()
        {
            var account = new CreditAccount { Name = "Card" };
            var previous = new CreditCardStatement
            {
                StatementDate = new DateTime(2026, 9, 8),
                StatementBalance = 200m
            };
            var statement = new CreditCardStatement
            {
                StatementDate = new DateTime(2026, 10, 8),
                StatementBalance = 215m
            };
            var charge = Transaction(new DateTime(2026, 9, 20), -40m, "Store");
            var payment = Transaction(new DateTime(2026, 9, 20), 25m, "Payment");

            var validation = StatementBalanceValidation.Build(
                account, statement, previous, [payment, charge]);

            Assert.That(validation.Rows.Select(row => (row.Description, row.Amount, row.Balance)), Is.EqualTo(new[]
            {
                ("Store", 40m, 240m),
                ("Payment", -25m, 215m)
            }));
            Assert.That(validation.Difference, Is.EqualTo(0m));
        }

        [Test]
        public void Build_WithNoTransactions_ComparesTheStartingBalanceToTheStatement()
        {
            var account = new LoanAccount { Name = "Auto" };
            var previous = new LoanStatement
            {
                StatementDate = new DateTime(2026, 8, 1),
                StatementBalance = 8000m
            };
            var statement = new LoanStatement
            {
                StatementDate = new DateTime(2026, 9, 1),
                StatementBalance = 7900m
            };

            var validation = StatementBalanceValidation.Build(account, statement, previous, []);

            Assert.That(validation.Rows, Is.Empty);
            Assert.That(validation.EndingBalance, Is.EqualTo(8000m));
            Assert.That(validation.Difference, Is.EqualTo(100m));
        }

        private static UnifiedTransactionView Transaction(DateTime date, decimal amount, string description) => new()
        {
            Id = Guid.NewGuid(),
            Date = date,
            Amount = amount,
            Description = description
        };
    }
}
