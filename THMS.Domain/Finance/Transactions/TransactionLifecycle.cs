namespace THMS.Domain.Finance.Transactions
{
    public enum ExpectedOrigin
    {
        Manual = 0,
        RecurringSingle = 1,
        RecurringTransfer = 2,
        StatementPay = 3,
        Pay = 4
    }

    public enum ExpectedStatus
    {
        Planned = 0,
        Scheduled = 1
    }

    public enum ImportedStatus
    {
        Unreconciled = 0,
        Matched = 1,
        AcceptedNew = 2
    }

    public static class TransactionStatuses
    {
        public const string Planned = "Planned";
        public const string Scheduled = "Scheduled";
        public const string Pending = "Pending";
        public const string Unreconciled = "Unreconciled";
        public const string Reconciled = "Reconciled";
        public const string New = "New";

        public static string ForExpected(DateTime date, bool realized, ExpectedStatus status, DateTime? asOf = null)
        {
            if (realized)
                return Reconciled;
            if (date.Date < (asOf ?? DateTime.Today).Date)
                return Pending;
            return status == ExpectedStatus.Scheduled ? Scheduled : Planned;
        }

        public static string ForImported(ImportedStatus status) =>
            status switch
            {
                ImportedStatus.Matched => Reconciled,
                ImportedStatus.AcceptedNew => New,
                _ => Unreconciled
            };

        public static bool IsLedgerImported(ImportedStatus status) =>
            status is ImportedStatus.Matched or ImportedStatus.AcceptedNew;
    }
}
