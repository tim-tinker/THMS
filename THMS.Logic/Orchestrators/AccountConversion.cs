using THMS.Domain.Finance.Accounts;

namespace THMS.Logic.Orchestrators.Finance
{
    public static class AccountConversion
    {
        public const string LossWarning =
            "Changing to or from an investment account cannot move this account's transactions. Transactions, statements, and investment holdings on this account will be removed.";

        public static bool SameKind(Account current, Account updated) =>
            AccountKinds.Of(current) == AccountKinds.Of(updated);

        public static bool CanCarryTransactions(Account current, Account updated) =>
            AccountKinds.Of(current) != AccountKinds.Investment
            && AccountKinds.Of(updated) != AccountKinds.Investment;
    }
}
