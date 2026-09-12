using Going.Plaid.Entity;
using Going.Plaid.Institutions;
using Going.Plaid.Item;
using Going.Plaid.Link;
using Going.Plaid.Sandbox;

namespace THMS.External.Plaid
{
    public class PlaidLinkManager : IPlaidLinkSession
    {
        public const string SandboxInstitutionId = "ins_109508";

        private readonly PlaidServiceClient _client;

        public PlaidLinkManager(PlaidServiceClient client)
        {
            _client = client;
        }

        public virtual async Task<string> CreateLinkTokenAsync(string userId)
        {
            var response = await _client.Raw.LinkTokenCreateAsync(new LinkTokenCreateRequest
            {
                User = new LinkTokenCreateRequestUser { ClientUserId = userId },
                ClientName = "THMS",
                Products = [Products.Transactions],
                CountryCodes = [CountryCode.Us],
                Language = Language.English
            });

            return response.LinkToken ?? throw new InvalidOperationException(
                response.Error?.ErrorMessage ?? "Plaid link token create failed.");
        }

        public virtual async Task<string> ExchangePublicTokenAsync(string publicToken)
        {
            var connection = await ExchangeForItemAsync(publicToken);
            return connection.AccessToken;
        }

        public virtual async Task<PlaidItemConnection> ExchangeForItemAsync(string publicToken)
        {
            var response = await _client.Raw.ItemPublicTokenExchangeAsync(new ItemPublicTokenExchangeRequest
            {
                PublicToken = publicToken
            });

            return new PlaidItemConnection
            {
                AccessToken = response.AccessToken ?? throw new InvalidOperationException(
                    response.Error?.ErrorMessage ?? "Plaid public token exchange failed."),
                ItemId = response.ItemId ?? ""
            };
        }

        public virtual async Task<string> CreateSandboxPublicTokenAsync(string institutionId)
        {
            var response = await _client.Raw.SandboxPublicTokenCreateAsync(new SandboxPublicTokenCreateRequest
            {
                InstitutionId = institutionId,
                InitialProducts = [Products.Transactions]
            });

            return response.PublicToken ?? throw new InvalidOperationException(
                response.Error?.ErrorMessage ?? "Plaid sandbox public token create failed.");
        }

        public virtual async Task<PlaidInstitutionInfo> GetInstitutionAsync(string accessToken)
        {
            var item = await _client.Raw.ItemGetAsync(new ItemGetRequest { AccessToken = accessToken });
            if (item.Error is not null)
                throw new InvalidOperationException(item.Error.ErrorMessage);

            var institutionId = item.Item?.InstitutionId ?? "";
            var itemId = item.Item?.ItemId ?? "";
            var name = institutionId;

            if (!string.IsNullOrWhiteSpace(institutionId))
            {
                var institution = await _client.Raw.InstitutionsGetByIdAsync(new InstitutionsGetByIdRequest
                {
                    InstitutionId = institutionId,
                    CountryCodes = [CountryCode.Us]
                });
                if (institution.Error is null && !string.IsNullOrWhiteSpace(institution.Institution?.Name))
                    name = institution.Institution.Name;
            }

            return new PlaidInstitutionInfo
            {
                InstitutionId = institutionId,
                ItemId = itemId,
                Name = name
            };
        }
    }
}
