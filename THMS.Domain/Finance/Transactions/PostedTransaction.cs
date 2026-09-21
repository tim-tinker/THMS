namespace THMS.Domain.Finance.Transactions
{
    public class PostedTransaction : BaseSingleAccountTransaction
    {
        public string? PlaidCategory { get; set; }
        public ImportedStatus ImportedStatus { get; set; } = ImportedStatus.Unreconciled;
        public Guid? RecommendedExpectedId { get; set; }

        public string DisplayStatus => TransactionStatuses.ForImported(ImportedStatus);
    }
}
