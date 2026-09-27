using Going.Plaid.Entity;
using Going.Plaid.Transactions;

namespace THMS.External.Plaid
{
    public class PlaidTransactionFetcher : IExternalTransactionFetcher
    {
        private readonly PlaidServiceClient _client;

        public PlaidTransactionFetcher(PlaidServiceClient client)
        {
            _client = client;
        }

        public virtual async Task<List<TransactionDto>> FetchTransactionsAsync(
            AccountDto account,
            DateTime start,
            DateTime end)
        {
            var response = await _client.Raw.TransactionsGetAsync(new TransactionsGetRequest
            {
                AccessToken = account.AccessToken,
                StartDate = DateOnly.FromDateTime(start),
                EndDate = DateOnly.FromDateTime(end)
            });

            if (response.Error is not null)
            {
                ThrowPlaidError(response.Error);
            }

            return response.Transactions.Select(t => t.ToDto()).ToList();
        }

        public virtual async Task<TransactionSyncPage> SyncTransactionsAsync(
            string accessToken,
            string? cursor,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var request = new TransactionsSyncRequest
            {
                AccessToken = accessToken,
                Count = 500
            };
            if (!string.IsNullOrWhiteSpace(cursor))
                request.Cursor = cursor;

            var response = await _client.Raw.TransactionsSyncAsync(request);
            cancellationToken.ThrowIfCancellationRequested();
            if (response.Error is not null)
                ThrowPlaidError(response.Error);

            return new TransactionSyncPage
            {
                Added = (response.Added ?? []).Select(t => t.ToDto()).ToList(),
                Modified = (response.Modified ?? []).Select(t => t.ToDto()).ToList(),
                RemovedIds = (response.Removed ?? [])
                    .Select(r => r.TransactionId ?? "")
                    .Where(id => id.Length > 0)
                    .ToList(),
                NextCursor = response.NextCursor ?? cursor ?? "",
                HasMore = response.HasMore
            };
        }

        private static void ThrowPlaidError(PlaidError error)
        {
            var message = error.ErrorMessage ?? "Plaid request failed.";
            var code = error.ErrorCode?.ToString() ?? "";
            if (code.Contains("ITEM_LOGIN_REQUIRED", StringComparison.OrdinalIgnoreCase)
                || code.Contains("ItemLoginRequired", StringComparison.OrdinalIgnoreCase))
            {
                throw new PlaidItemLoginRequiredException(message);
            }

            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(code) ? message : $"{code}: {message}");
        }
    }
}
