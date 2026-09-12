using THMS.Domain.Finance.Transactions;

namespace THMS.Logic.Finance.Transactions
{
    public static class SplitTransactionValidator
    {
        public static void Validate(decimal parentAmount, IReadOnlyList<SplitTransactionRow> splits)
        {
            if (splits.Count == 0)
                throw new InvalidOperationException("At least one split row is required.");

            if (!SplitTransactionMath.AmountsMatch(parentAmount, splits))
            {
                var sum = splits.Sum(s => s.Amount);
                throw new InvalidOperationException(
                    $"Split amounts ({sum:c2}) must equal the parent amount ({parentAmount:c2}).");
            }

            foreach (var split in splits)
            {
                if (split.Type == SplitType.Transfer)
                {
                    if (split.TransferAccountId is not Guid accountId || accountId == Guid.Empty)
                        throw new InvalidOperationException("Transfer splits require a destination account.");
                    continue;
                }

                if (SplitTransactionMath.RequiresCategory(split.Type) &&
                    SplitTransactionMath.IsUncategorized(split.CategoryId, split.Category))
                {
                    throw new InvalidOperationException(
                        $"{split.Type} splits require a category.");
                }
            }
        }
    }
}
