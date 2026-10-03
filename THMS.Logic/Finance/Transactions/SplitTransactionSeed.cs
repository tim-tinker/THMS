using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Transactions;

namespace THMS.Logic.Finance.Transactions
{
    public static class SplitTransactionSeed
    {
        public const int MatchDayWindow = 4;

        public static SplitTransactionRow Create(
            BaseTransaction parent,
            Guid? transferAccountId,
            IReadOnlyList<ExpenseCategory> categories)
        {
            var categoryId = parent.CategoryId;
            var categoryName = parent.Category;
            if ((categoryId is null || categoryId == Guid.Empty) && !string.IsNullOrWhiteSpace(categoryName))
            {
                var match = categories.FirstOrDefault(c =>
                    string.Equals(c.Name, categoryName.Trim(), StringComparison.OrdinalIgnoreCase));
                if (match is not null)
                {
                    categoryId = match.Id;
                    categoryName = match.Name;
                }
            }
            else if (categoryId is Guid known)
            {
                var match = categories.FirstOrDefault(c => c.Id == known);
                if (match is not null)
                    categoryName = match.Name;
            }

            var type = InferType(parent);
            var (fromAccountId, toAccountId) = TransferAccounts(parent, transferAccountId);
            return new SplitTransactionRow
            {
                Id = Guid.NewGuid(),
                Amount = parent.Amount,
                CategoryId = categoryId is Guid { } category && category != Guid.Empty ? category : null,
                Category = categoryName,
                Type = type,
                FromAccountId = type == SplitType.Transfer ? fromAccountId : null,
                ToAccountId = type == SplitType.Transfer ? toAccountId : null,
                TransferAccountId = type == SplitType.Transfer ? toAccountId : null
            };
        }

        private static (Guid? From, Guid? To) TransferAccounts(BaseTransaction parent, Guid? otherAccountId)
        {
            if (parent is PostedTransferTransaction transfer)
            {
                var from = EmptyToNull(transfer.FromAccountId);
                var to = EmptyToNull(transfer.ToAccountId);
                if (transfer.Direction == TransferDirection.Outgoing)
                {
                    from ??= transfer.AccountId == Guid.Empty ? null : transfer.AccountId;
                    to ??= otherAccountId;
                }
                else
                {
                    to ??= transfer.AccountId == Guid.Empty ? null : transfer.AccountId;
                    from ??= otherAccountId;
                }

                return (from, to);
            }

            if (parent is FutureTransferTransaction future)
                return (EmptyToNull(future.FromAccountId), EmptyToNull(future.ToAccountId));
            if (parent is RecurringTransferRule rule)
                return (EmptyToNull(rule.FromAccountId), EmptyToNull(rule.ToAccountId));
            return (null, null);
        }

        private static Guid? EmptyToNull(Guid id) => id == Guid.Empty ? null : id;

        private static SplitType InferType(BaseTransaction parent) =>
            parent is PostedTransferTransaction or FutureTransferTransaction or RecurringTransferRule
                ? SplitType.Transfer
                : parent.Amount >= 0 ? SplitType.Income : SplitType.Expense;

        public static Guid? MatchCounterpartAccount(
            BaseTransaction parent,
            Guid thisAccountId,
            IReadOnlyList<Account> accounts,
            IEnumerable<PostedTransaction> otherPosted)
        {
            var names = accounts.ToDictionary(a => a.Id, a => a.Name ?? "");
            var thisName = names.GetValueOrDefault(thisAccountId);
            var target = -parent.Amount;
            var candidates = otherPosted
                .Where(t => t.Id != parent.Id
                    && t.AccountId != thisAccountId
                    && t.AccountId != Guid.Empty
                    && t.Amount == target
                    && Math.Abs((t.Date.Date - parent.Date.Date).TotalDays) <= MatchDayWindow
                    && LooksLikeTransferPair(parent.Description, t.Description, names.GetValueOrDefault(t.AccountId), thisName))
                .ToList();

            if (candidates.Count == 1)
                return candidates[0].AccountId;

            var named = candidates
                .Where(t => Mentions(parent.Description, names.GetValueOrDefault(t.AccountId))
                    || Mentions(t.Description, thisName))
                .Select(t => t.AccountId)
                .Distinct()
                .ToList();
            return named.Count == 1 ? named[0] : null;
        }

        private static bool LooksLikeTransferPair(
            string? left,
            string? right,
            string? otherAccountName,
            string? thisAccountName) =>
            ($"{left} {right}").Contains("TRANSFER", StringComparison.OrdinalIgnoreCase)
            || Mentions(left, otherAccountName)
            || Mentions(right, thisAccountName);

        private static bool Mentions(string? description, string? accountName)
        {
            if (string.IsNullOrWhiteSpace(description) || string.IsNullOrWhiteSpace(accountName))
                return false;

            var text = description;
            var name = accountName.Trim();
            if (text.Contains(name, StringComparison.OrdinalIgnoreCase))
                return true;

            foreach (var token in name.Split([' ', '-', '/'], StringSplitOptions.RemoveEmptyEntries))
            {
                if (token.Length >= 5 && text.Contains(token, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
