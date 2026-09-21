using THMS.Data.Stores;
using THMS.Domain.Finance.Transactions;

namespace THMS.Logic.Orchestrators.Finance
{
    public class AccountActivityOrchestrator
    {
        private readonly ITransactionDataStore _transactions;
        private readonly ICategoryDataStore _categories;

        public AccountActivityOrchestrator()
            : this(new DataStoreFactory().GetTransactionStore(), new DataStoreFactory().GetCategoryStore())
        {
        }

        public AccountActivityOrchestrator(ITransactionDataStore transactions, ICategoryDataStore categories)
        {
            _transactions = transactions;
            _categories = categories;
        }

        public FutureSingleTransaction AddPending(
            Guid accountId,
            DateTime date,
            decimal amount,
            string? description,
            Guid? categoryId)
        {
            EnsureAccount(accountId);
            EnsureAmount(amount);
            var transaction = new FutureSingleTransaction
            {
                AccountId = accountId,
                Date = date.Date,
                Amount = amount,
                Description = Normalize(description),
                IsUserCreated = true,
                Origin = ExpectedOrigin.Manual,
                Status = ExpectedStatus.Planned
            };
            ApplyCategory(transaction, categoryId);
            _transactions.AddFutureSingleTransaction(transaction);
            return transaction;
        }

        public PostedTransaction AddPosted(
            Guid accountId,
            DateTime date,
            decimal amount,
            string? description,
            Guid? categoryId)
        {
            EnsureAccount(accountId);
            EnsureAmount(amount);
            var transaction = new PostedTransaction
            {
                AccountId = accountId,
                Date = date.Date,
                Amount = amount,
                Description = Normalize(description)
            };
            ApplyCategory(transaction, categoryId);
            _transactions.AddPostedTransaction(transaction);
            return transaction;
        }

        public FutureTransferTransaction AddTransfer(
            Guid fromAccountId,
            Guid toAccountId,
            DateTime date,
            decimal amount,
            string? description)
        {
            EnsureAccount(fromAccountId);
            EnsureAccount(toAccountId);
            if (fromAccountId == toAccountId)
                throw new InvalidOperationException("Choose a different account for the other side of the transfer.");
            if (amount <= 0)
                throw new InvalidOperationException("Amount must be greater than zero.");

            var transfer = new FutureTransferTransaction
            {
                FromAccountId = fromAccountId,
                ToAccountId = toAccountId,
                Date = date.Date,
                Amount = amount,
                Description = Normalize(description),
                IsUserCreated = true,
                Origin = ExpectedOrigin.Manual,
                Status = ExpectedStatus.Planned
            };
            ApplyCategory(transfer, DefaultExpenseCategories.PaymentId);
            _transactions.AddFutureTransferTransaction(transfer);
            return transfer;
        }

        private void ApplyCategory(BaseTransaction transaction, Guid? categoryId)
        {
            _categories.EnsureDefaultCategories();
            var category = categoryId is Guid id
                ? _categories.GetCategory(id)
                : _categories.GetCategory(DefaultExpenseCategories.UncategorizedId);
            category ??= DefaultExpenseCategories.All.First(c => c.Id == DefaultExpenseCategories.UncategorizedId);
            transaction.ApplyCategory(category);
        }

        private static void EnsureAccount(Guid accountId)
        {
            if (accountId == Guid.Empty)
                throw new InvalidOperationException("An account is required.");
        }

        private static void EnsureAmount(decimal amount)
        {
            if (amount == 0)
                throw new InvalidOperationException("Amount cannot be zero.");
        }

        private static string? Normalize(string? description) =>
            string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }
}
