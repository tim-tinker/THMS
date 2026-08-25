using Going.Plaid.Entity;

namespace THMS.External.Plaid
{
    public static class AccountExtension
    {
        public static AccountDto ToDto(this Account account)
        {
            return new AccountDto
            {
                PlaidAccountId = account.AccountId ?? "",
                Name = account.Name ?? "",
                Mask = account.Mask ?? "",
                Type = account.Type.ToString(),
                Subtype = account.Subtype?.ToString() ?? "",
                Available = account.Balances?.Available,
                Current = account.Balances?.Current,
                Limit = account.Balances?.Limit
            };
        }
    }
}
