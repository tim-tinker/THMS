using THMS.Data.Stores;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Orchestrators;

namespace THMS.Logic.Orchestrators.Finance
{
    public class AccountActivityOrchestrator
    {
        private readonly ITransactionDataStore _transactions;
        private readonly ICategoryDataStore _categories;
        private readonly IAccountDataStore? _accounts;

        public AccountActivityOrchestrator()
            : this(
                new DataStoreFactory().GetTransactionStore(),
                new DataStoreFactory().GetCategoryStore(),
                new DataStoreFactory().GetAccountStore())
        {
        }

        public AccountActivityOrchestrator(ITransactionDataStore transactions, ICategoryDataStore categories)
            : this(transactions, categories, accounts: null)
        {
        }

        public AccountActivityOrchestrator(
            ITransactionDataStore transactions,
            ICategoryDataStore categories,
            IAccountDataStore? accounts)
        {
            _transactions = transactions;
            _categories = categories;
            _accounts = accounts;
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
                Description = Normalize(description),
                ImportedStatus = ImportedStatus.Matched
            };
            ApplyCategory(transaction, categoryId);
            _transactions.AddPostedTransaction(transaction);
            return transaction;
        }

        public void UpdateLedgerEntry(
            Guid id,
            DateTime date,
            decimal amount,
            string? description,
            Guid? categoryId)
        {
            if (_transactions.GetPostedTransferTransaction(id) is PostedTransferTransaction transfer)
            {
                UpdatePostedTransfer(transfer, date, amount, description, categoryId);
                return;
            }

            if (_transactions.GetPostedTransaction(id) is PostedTransaction posted)
            {
                UpdatePosted(posted, date, amount, description, categoryId);
                return;
            }

            if (_transactions.GetFutureSingleTransaction(id) is FutureSingleTransaction futureSingle)
            {
                UpdateFutureSingle(futureSingle, date, amount, description, categoryId);
                return;
            }

            if (_transactions.GetFutureTransferTransaction(id) is FutureTransferTransaction futureTransfer)
            {
                UpdateFutureTransfer(futureTransfer, date, amount, description, categoryId);
                return;
            }

            throw new InvalidOperationException("That transaction cannot be edited.");
        }

        public void DeleteLedgerEntry(Guid id)
        {
            if (_transactions.GetPostedTransferTransaction(id) is PostedTransferTransaction transfer)
            {
                DeletePostedTransfer(transfer);
                return;
            }

            if (_transactions.GetPostedTransaction(id) is PostedTransaction posted)
            {
                ReleaseMatch(posted.Id);
                _transactions.DeletePostedTransaction(posted.Id);
                new BudgetOrchestrator(_transactions, _accounts).RefreshAffected([posted]);
                return;
            }

            if (_transactions.GetFutureSingleTransaction(id) is not null)
            {
                _transactions.DeleteFutureSingleTransaction(id);
                return;
            }

            if (_transactions.GetFutureTransferTransaction(id) is not null)
            {
                _transactions.DeleteFutureTransferTransaction(id);
                return;
            }

            throw new InvalidOperationException("That transaction cannot be deleted.");
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

        private void DeletePostedTransfer(PostedTransferTransaction transfer)
        {
            var relatedId = transfer.RelatedPostedTransactionId;
            var affected = new List<PostedTransaction> { transfer };
            ReleaseMatch(transfer.Id);
            _transactions.DeletePostedTransferTransaction(transfer.Id);

            if (relatedId != Guid.Empty && relatedId != transfer.Id)
            {
                ReleaseMatch(relatedId);
                if (_transactions.GetPostedTransferTransaction(relatedId) is PostedTransferTransaction otherTransfer)
                {
                    affected.Add(otherTransfer);
                    _transactions.DeletePostedTransferTransaction(relatedId);
                }
                else if (_transactions.GetPostedTransaction(relatedId) is PostedTransaction otherPosted)
                {
                    affected.Add(otherPosted);
                    _transactions.DeletePostedTransaction(relatedId);
                }
            }

            new BudgetOrchestrator(_transactions, _accounts).RefreshAffected(affected);
        }

        private void ReleaseMatch(Guid importedId)
        {
            if (_transactions.GetReconciliationByImported(importedId) is null)
                return;
            new ReconciliationOrchestrator(_transactions).UndoMatch(importedId);
        }

        private void UpdatePosted(
            PostedTransaction posted,
            DateTime date,
            decimal amount,
            string? description,
            Guid? categoryId)
        {
            EnsureAmount(amount);
            EnsureSplitEdit(posted, amount, categoryId);
            posted.Date = date.Date;
            posted.Amount = amount;
            posted.Description = Normalize(description);
            if (!posted.HasSplits)
                ApplyCategory(posted, categoryId);
            _transactions.UpdatePostedTransaction(posted);
            new BudgetOrchestrator(_transactions, _accounts).RefreshAffected([posted]);
        }

        private void UpdatePostedTransfer(
            PostedTransferTransaction transfer,
            DateTime date,
            decimal amount,
            string? description,
            Guid? categoryId)
        {
            EnsureAmount(amount);
            EnsureSplitEdit(transfer, amount, categoryId);
            var previousAmount = transfer.Amount;
            transfer.Date = date.Date;
            transfer.Amount = amount;
            transfer.Description = Normalize(description);
            if (!transfer.HasSplits)
                ApplyCategory(transfer, categoryId);
            _transactions.UpdatePostedTransferTransaction(transfer);
            SyncTransferCounterpart(transfer, previousAmount);
            new BudgetOrchestrator(_transactions, _accounts).RefreshAffected([transfer]);
        }

        private void SyncTransferCounterpart(PostedTransferTransaction transfer, decimal previousAmount)
        {
            if (transfer.RelatedPostedTransactionId == Guid.Empty)
                return;

            var other = _transactions.GetPostedTransferTransaction(transfer.RelatedPostedTransactionId);
            if (other is null || other.HasSplits)
                return;

            other.Date = transfer.Date;
            other.Description = transfer.Description;
            other.CategoryId = transfer.CategoryId;
            other.Category = transfer.Category;
            if (previousAmount != 0 && other.Amount == -previousAmount)
                other.Amount = -transfer.Amount;
            _transactions.UpdatePostedTransferTransaction(other);
        }

        private void UpdateFutureSingle(
            FutureSingleTransaction future,
            DateTime date,
            decimal amount,
            string? description,
            Guid? categoryId)
        {
            EnsureAmount(amount);
            EnsureSplitEdit(future, amount, categoryId);
            future.Date = date.Date;
            future.Amount = amount;
            future.Description = Normalize(description);
            if (!future.HasSplits)
                ApplyCategory(future, categoryId);
            _transactions.UpdateFutureSingleTransaction(future);
        }

        private void UpdateFutureTransfer(
            FutureTransferTransaction transfer,
            DateTime date,
            decimal amount,
            string? description,
            Guid? categoryId)
        {
            if (amount <= 0)
                throw new InvalidOperationException("Amount must be greater than zero.");
            EnsureSplitEdit(transfer, amount, categoryId);
            transfer.Date = date.Date;
            transfer.Amount = amount;
            transfer.Description = Normalize(description);
            if (!transfer.HasSplits)
                ApplyCategory(transfer, categoryId);
            _transactions.UpdateFutureTransferTransaction(transfer);
        }

        private static void EnsureSplitEdit(BaseTransaction existing, decimal amount, Guid? categoryId)
        {
            if (!existing.HasSplits)
                return;
            if (existing.Amount != amount || existing.CategoryId != categoryId)
                throw new InvalidOperationException(
                    "This transaction is split. Change the amount and category from Split Transaction.");
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
