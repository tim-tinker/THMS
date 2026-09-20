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
        public const string HostedLinkCompletionUri = "thms://plaid-link-complete";

        private readonly PlaidServiceClient _client;

        public PlaidLinkManager(PlaidServiceClient client)
        {
            _client = client;
        }

        public virtual async Task<PlaidLinkTokenResult> CreateLinkTokenAsync(string userId, bool hostedLink = false)
        {
            var request = new LinkTokenCreateRequest
            {
                User = new LinkTokenCreateRequestUser { ClientUserId = userId },
                ClientName = "THMS",
                Products = [Products.Transactions],
                CountryCodes = [CountryCode.Us],
                Language = Language.English
            };
            if (hostedLink)
            {
                request.HostedLink = new LinkTokenCreateHostedLink
                {
                    CompletionRedirectUri = HostedLinkCompletionUri
                };
            }

            var response = await _client.Raw.LinkTokenCreateAsync(request);
            var linkToken = response.LinkToken ?? throw new InvalidOperationException(
                response.Error?.ErrorMessage ?? "Plaid link token create failed.");

            if (!hostedLink)
            {
                return new PlaidLinkTokenResult { LinkToken = linkToken };
            }

            if (string.IsNullOrWhiteSpace(response.HostedLinkUrl))
            {
                throw new InvalidOperationException(
                    response.Error?.ErrorMessage ?? "Plaid did not return a Hosted Link URL.");
            }

            return new PlaidLinkTokenResult
            {
                LinkToken = linkToken,
                HostedLinkUrl = response.HostedLinkUrl
            };
        }

        public virtual async Task<string?> GetPublicTokenFromLinkSessionAsync(string linkToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(linkToken);
            var response = await _client.Raw.LinkTokenGetAsync(new LinkTokenGetRequest
            {
                LinkToken = linkToken
            });
            if (response.Error is not null)
                throw new InvalidOperationException(response.Error.ErrorMessage);

            return ExtractPublicToken(response);
        }

        public static string? ExtractPublicToken(LinkTokenGetResponse response)
        {
            ArgumentNullException.ThrowIfNull(response);
            if (response.LinkSessions is null)
                return null;

            foreach (var session in response.LinkSessions)
            {
                var fromResults = session.Results?.ItemAddResults?
                    .Select(result => result.PublicToken)
                    .FirstOrDefault(token => !string.IsNullOrWhiteSpace(token));
                if (!string.IsNullOrWhiteSpace(fromResults))
                    return fromResults;

#pragma warning disable CS0612
                if (!string.IsNullOrWhiteSpace(session.OnSuccess?.PublicToken))
                    return session.OnSuccess.PublicToken;
#pragma warning restore CS0612
            }

            return null;
        }

        public static bool IsHostedLinkCompletion(Uri uri)
        {
            ArgumentNullException.ThrowIfNull(uri);
            if (!string.Equals(uri.Scheme, "thms", StringComparison.OrdinalIgnoreCase))
                return false;

            return string.Equals(uri.Host, "plaid-link-complete", StringComparison.OrdinalIgnoreCase)
                || string.Equals(uri.AbsolutePath.Trim('/'), "plaid-link-complete", StringComparison.OrdinalIgnoreCase);
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
