using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Planning;
using THMS.Domain.Finance.Transactions;

namespace THMS.Logic.ViewModels.Finance
{
    public class BillRow
    {
        public bool Pay { get; set; }
        public Guid? IntentId { get; set; }
        public PaymentIntentSource Source { get; set; }
        public Guid? SourceId { get; set; }
        public Guid DestinationAccountId { get; set; }
        public string DestinationName { get; set; } = "";
        public Guid FundingAccountId { get; set; }
        public string FundingName { get; set; } = "";
        public string Kind { get; set; } = "";
        public DateTime DueDate { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = BillStatuses.Due;
        public string Notes { get; set; } = "";
        public Guid? StatementId { get; set; }
    }

    public sealed class PayFromChoice
    {
        public static PayFromChoice Unspecified { get; } = new() { Id = Guid.Empty, Name = "" };

        public Guid Id { get; init; }
        public string Name { get; init; } = "";

        public static PayFromChoice From(Account account) =>
            new() { Id = account.Id, Name = account.Name };
    }

    public static class BillStatuses
    {
        public const string Due = "Due";
        public const string Scheduled = "Scheduled";
    }

    public static class BillKinds
    {
        public const string Statement = "Statement";
        public const string Recurring = "Recurring";
        public const string Transfer = "Transfer";
        public const string Manual = "Manual";
    }
}
