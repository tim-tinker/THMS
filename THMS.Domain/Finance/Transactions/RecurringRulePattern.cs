namespace THMS.Domain.Finance.Transactions
{
    public static class RecurringRulePattern
    {
        public const decimal AmountTolerance = 0.01m;

        public static string NormalizeDescription(string? description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return "";

            return string.Join(' ', description.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
                .ToUpperInvariant();
        }

        public static bool AmountsEqual(decimal left, decimal right) =>
            Math.Abs(left - right) <= AmountTolerance;

        public static bool Matches(
            RecurringSingleTransactionRule rule,
            Guid accountId,
            string? description,
            decimal amount,
            RecurrenceFrequency frequency) =>
            rule.AccountId == accountId &&
            NormalizeDescription(rule.Description) == NormalizeDescription(description) &&
            AmountsEqual(rule.Amount, amount) &&
            rule.Frequency == frequency;

        public static bool Matches(RecurringSingleTransactionRule existing, RecurringSingleTransactionRule candidate) =>
            Matches(existing, candidate.AccountId, candidate.Description, candidate.Amount, candidate.Frequency);

        public static bool MatchesMerchantSchedule(
            RecurringSingleTransactionRule rule,
            Guid accountId,
            string? description,
            RecurrenceFrequency frequency) =>
            rule.AccountId == accountId &&
            NormalizeDescription(rule.Description) == NormalizeDescription(description) &&
            rule.Frequency == frequency;

        public static bool Matches(
            RecurringTransferRule rule,
            Guid fromAccountId,
            string? description,
            decimal amount,
            RecurrenceFrequency frequency) =>
            rule.FromAccountId == fromAccountId &&
            NormalizeDescription(rule.Description) == NormalizeDescription(description) &&
            AmountsEqual(rule.Amount, amount) &&
            rule.Frequency == frequency;

        public static bool Matches(RecurringTransferRule existing, RecurringTransferRule candidate) =>
            Matches(existing, candidate.FromAccountId, candidate.Description, candidate.Amount, candidate.Frequency);

        public static bool MatchesMerchantSchedule(
            RecurringTransferRule rule,
            Guid fromAccountId,
            string? description,
            RecurrenceFrequency frequency) =>
            rule.FromAccountId == fromAccountId &&
            NormalizeDescription(rule.Description) == NormalizeDescription(description) &&
            rule.Frequency == frequency;
    }
}
