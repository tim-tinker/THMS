using THMS.Domain.Finance.Accounts;
using THMS.External;

namespace THMS.Logic.Mapping
{
    public static class AccountExtension
    {
        public static AccountDto ToDto(this Account account)
        {
            var link = account.ExternalLink;

            return new AccountDto
            {
                // ExternalLink-derived fields (the important ones)
                Provider = link?.Provider ?? "",
                ItemId = link?.ItemId ?? "",
                AccessToken = link?.AccessToken ?? "",
                PlaidAccountId = link?.PlaidAccountId ?? "",
                InstitutionId = link?.InstitutionId ?? "",
                Mask = link?.AccountMask ?? "",

                // Optional fields already defined on AccountDto
                // but not required for Plaid fetching
                Name = account.Name,
                Type = "",     // leave blank until domain Account supports it
                Subtype = "",  // leave blank until domain Account supports it
                Available = null,
                Current = null,
                Limit = null
            };
        }
    }
}
