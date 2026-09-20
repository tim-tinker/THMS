using THMS.Configuration;
using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.External;
using THMS.External.Plaid;
using THMS.Logic.ViewModels.Finance;

namespace THMS.Logic.Orchestrators.Finance
{
    public class PlaidAccountOrchestrator
    {
        private readonly IAccountDataStore _accounts;
        private readonly IExternalAccountFetcher _accountFetcher;
        private readonly IPlaidLinkSession _linkSession;
        private readonly bool _sandbox;
        private List<PlaidAccountViewModel> _linkedAccounts = [];

        public PlaidAccountOrchestrator()
        {
            var factory = new ExternalFetcherFactory();
            _accounts = new DataStoreFactory().GetAccountStore();
            _accountFetcher = factory.GetAccountFetcher();
            _linkSession = factory.GetLinkSession();
            _sandbox = string.Equals(
                AppConfig.Instance.PlaidEnvironment,
                "Sandbox",
                StringComparison.OrdinalIgnoreCase);
        }

        public PlaidAccountOrchestrator(
            IAccountDataStore accounts,
            IExternalAccountFetcher accountFetcher,
            IPlaidLinkSession linkSession,
            string? plaidEnvironment = null)
        {
            _accounts = accounts;
            _accountFetcher = accountFetcher;
            _linkSession = linkSession;
            _sandbox = string.Equals(
                plaidEnvironment ?? AppConfig.Instance.PlaidEnvironment,
                "Sandbox",
                StringComparison.OrdinalIgnoreCase);
        }

        public bool IsSandbox => _sandbox;

        public async Task<PlaidLinkTokenResult> CreateHostedLinkSessionAsync()
        {
            var result = await _linkSession.CreateLinkTokenAsync("thms-user", hostedLink: true);
            if (string.IsNullOrWhiteSpace(result.HostedLinkUrl))
                throw new InvalidOperationException("Plaid did not return a Hosted Link URL.");
            return result;
        }

        public Task<string?> GetPublicTokenFromLinkSessionAsync(string linkToken) =>
            _linkSession.GetPublicTokenFromLinkSessionAsync(linkToken);

        public async Task<string?> WaitForPublicTokenAsync(
            string linkToken,
            int attempts = 10,
            int delayMs = 500,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(linkToken);
            if (attempts < 1)
                throw new ArgumentOutOfRangeException(nameof(attempts));

            string? token = null;
            for (var attempt = 0; attempt < attempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                token = await _linkSession.GetPublicTokenFromLinkSessionAsync(linkToken);
                if (!string.IsNullOrWhiteSpace(token))
                    return token;
                if (attempt < attempts - 1)
                    await Task.Delay(delayMs, cancellationToken);
            }

            return token;
        }

        public async Task StartLinkFlow(string? publicToken = null)
        {
            if (string.IsNullOrWhiteSpace(publicToken))
            {
                if (!_sandbox)
                    throw new InvalidOperationException(
                        "A Plaid public token is required outside sandbox. Complete Plaid Link.");

                publicToken = await _linkSession.CreateSandboxPublicTokenAsync(PlaidLinkManager.SandboxInstitutionId);
            }

            var connection = await _linkSession.ExchangeForItemAsync(publicToken);
            var institution = await _linkSession.GetInstitutionAsync(connection.AccessToken);
            var itemId = string.IsNullOrWhiteSpace(connection.ItemId) ? institution.ItemId : connection.ItemId;
            var dtos = await _accountFetcher.FetchAccountsAsync(connection.AccessToken);
            var thmsAccounts = _accounts.GetAllAccounts().ToList();

            _linkedAccounts = dtos.Select(dto => new PlaidAccountViewModel
            {
                Institution = institution.Name,
                PlaidAccountId = dto.PlaidAccountId,
                Mask = dto.Mask,
                Subtype = dto.Subtype,
                Name = dto.Name,
                AccessToken = connection.AccessToken,
                ItemId = itemId,
                InstitutionId = institution.InstitutionId,
                SuggestedThmsAccountId = SuggestThmsAccount(dto, thmsAccounts)
            }).ToList();
        }

        public List<PlaidAccountViewModel> GetPlaidAccounts() => _linkedAccounts;

        public IReadOnlyList<AccountMappingChoice> GetThmsAccountChoices()
        {
            var choices = new List<AccountMappingChoice>
            {
                new() { Id = Guid.Empty, Name = "(none)" }
            };
            choices.AddRange(_accounts.GetAllAccounts()
                .OrderBy(a => a.Name)
                .Select(a => new AccountMappingChoice { Id = a.Id, Name = a.Name }));
            return choices;
        }

        public int SaveAccountMappings(IEnumerable<PlaidAccountViewModel> mappedRows)
        {
            ArgumentNullException.ThrowIfNull(mappedRows);
            var accounts = _accounts.GetAllAccounts().ToDictionary(a => a.Id);
            var saved = 0;

            foreach (var row in mappedRows)
            {
                if (row.SuggestedThmsAccountId == Guid.Empty)
                    continue;
                if (!accounts.TryGetValue(row.SuggestedThmsAccountId, out var account))
                    throw new InvalidOperationException($"THMS account '{row.SuggestedThmsAccountId}' was not found.");

                ClearStaleLinks(accounts, account.Id, row.PlaidAccountId);
                ApplyLink(account, row);
                if (string.IsNullOrWhiteSpace(account.Institution) && !string.IsNullOrWhiteSpace(row.Institution))
                    account.Institution = row.Institution;

                _accounts.UpsertAccount(account);
                saved++;
            }

            return saved;
        }

        private void ClearStaleLinks(IReadOnlyDictionary<Guid, Account> accounts, Guid keepAccountId, string plaidAccountId)
        {
            if (string.IsNullOrWhiteSpace(plaidAccountId))
                return;

            foreach (var other in accounts.Values)
            {
                if (other.Id == keepAccountId)
                    continue;
                if (!HasPlaidAccount(other, plaidAccountId))
                    continue;

                other.ExternalLink = null;
                _accounts.UpsertAccount(other);
            }
        }

        private static bool HasPlaidAccount(Account account, string plaidAccountId) =>
            account.ExternalLink is not null
            && string.Equals(account.ExternalLink.PlaidAccountId, plaidAccountId, StringComparison.Ordinal);

        private static void ApplyLink(Account account, PlaidAccountViewModel row)
        {
            var link = account.ExternalLink;
            if (link is null)
            {
                link = new ExternalAccountLink();
                account.ExternalLink = link;
            }

            link.Provider = "Plaid";
            link.ItemId = row.ItemId;
            link.AccessToken = row.AccessToken;
            link.PlaidAccountId = row.PlaidAccountId;
            link.InstitutionId = row.InstitutionId;
            link.AccountMask = row.Mask;
            link.InstitutionName = row.Institution;
        }

        private static Guid SuggestThmsAccount(AccountDto dto, IReadOnlyList<Account> accounts)
        {
            if (!string.IsNullOrWhiteSpace(dto.Mask))
            {
                var byMask = accounts.FirstOrDefault(a =>
                    !string.IsNullOrWhiteSpace(a.AccountNumber)
                    && a.AccountNumber.EndsWith(dto.Mask, StringComparison.Ordinal));
                if (byMask is not null)
                    return byMask.Id;
            }

            var byName = accounts.FirstOrDefault(a =>
                string.Equals(a.Name, dto.Name, StringComparison.OrdinalIgnoreCase));
            return byName?.Id ?? Guid.Empty;
        }
    }
}
