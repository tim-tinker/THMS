namespace THMS.Domain.Finance.Accounts
{
    /// <summary>
    /// A biller without a tracked ledger (utility, service, or insurance).
    /// Statements and planned payments attach here; cash leaves a bank account.
    /// </summary>
    public class UntrackedAccount : Account
    {
    }
}
