using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Transactions;
using THMS.External.Plaid;

namespace THMS.Logic.Orchestrators
{
    public class TransactionImportOrchestrator
    {
        private readonly PlaidTransactionFetcher _transactionFetcher;
        private readonly ITransactionDataStore _txStore;
        private double _dateWindowSize = 3; // use three because of weekends

        public TransactionImportOrchestrator(
            PlaidClient plaidClient,
            ITransactionDataStore txStore)
        {
            _transactionFetcher = new PlaidTransactionFetcher(plaidClient);
            _txStore = txStore;
        }

        public async Task<TransactionImportResult> ImportAsync(Account account)
        {
            if (account.ExternalLink is null)
                throw new InvalidOperationException("Account is not linked to Plaid.");

            var link = account.ExternalLink;

            // 1. Fetch Plaid transactions
            var plaidTxs = await _transactionFetcher.FetchTransactionsAsync(
                link.AccessToken,
                DateTime.UtcNow.AddDays(-30),
                DateTime.UtcNow);

            // 2. Filter out pending
            var postedDtos = plaidTxs.Where(t => !t.Pending).ToList();

            // 3. Map to domain
            var posted = postedDtos.Select(dto => MapPosted(dto, account.Id)).ToList();

            // 4. Detect transfers
            var transfers = DetectTransfers(posted);

            // 5. Categorize
            Categorize(posted);

            // 6. Insert posted transactions
            foreach (var p in posted)
                _txStore.AddPostedTransaction(p);

            return new TransactionImportResult
            {
                AccountId = account.Id,
                PostedImported = posted.Count,
                TransfersDetected = transfers.Count
            };
        }

        private PostedTransaction MapPosted(PlaidTransactionDto dto, Guid accountId)
        {
            return new PostedTransaction
            {
                AccountId = accountId,
                Amount = dto.Amount,
                Date = dto.Date ?? DateTime.UtcNow,
                Description = dto.Name,
                PlaidCategory = dto.Category
            };
        }

        private List<TransferTransaction> DetectTransfers(
            List<PostedTransaction> imported)
        {
            var results = new List<TransferTransaction>();

            if (0 < imported.Count)
            {
                var existing = GetExistingTransactions(imported);

                // Combine imported + existing for matching
                var all = existing.Concat(imported).ToList();

                // Group by absolute amount for fast lookup
                var byAmount = all
                    .GroupBy(t => Math.Abs(t.Amount))
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (var t in imported)
                {
                    var amount = Math.Abs(t.Amount);

                    if (!byAmount.TryGetValue(amount, out var candidates))
                        continue;

                    foreach (var c in candidates)
                    {
                        // Skip self
                        if (IsTransactionMatched(t, c))
                        {
                            // We found a transfer pair
                            var debit = t.Amount < 0 ? t : c;
                            var credit = t.Amount > 0 ? t : c;

                            // Convert both to PostedTransferTransaction
                            var debitTransfer = new PostedTransferTransaction(debit, credit.Id, TransferDirection.Outgoing);

                            var creditTransfer = new PostedTransferTransaction(credit, debit.Id, TransferDirection.Incoming);

                            // Replace in ledger
                            _txStore.ReplacePostedTransaction(debitTransfer);
                            _txStore.ReplacePostedTransaction(creditTransfer);

                            // Create conceptual TransferTransaction
                            results.Add(new TransferTransaction
                            {
                                FromAccountId = debit.AccountId,
                                ToAccountId = credit.AccountId,
                                Amount = Math.Abs(debit.Amount),
                                Date = debit.Date
                            });

                            // Once matched, stop scanning this t
                            break;
                        }
                    }
                }
            }

            return results;
        }

        private IEnumerable<PostedTransaction> GetExistingTransactions(List<PostedTransaction> imported)
        {
            var sortedImported = (from tx in imported orderby tx.Date select tx).ToList();
            var start = sortedImported.First().Date.AddDays(-_dateWindowSize);
            var end = sortedImported.Last().Date.AddDays(_dateWindowSize);

            return _txStore.GetPostedTransactions(start, end);
        }

        private bool IsTransactionMatched(PostedTransaction t, PostedTransaction c)
        {
            var isMatched = false;
            // Skip self
            if (t.Id != c.Id)
            {
                // Must be opposite signs
                if (0 < t.Amount * c.Amount)
                {
                    // Must be different accounts
                    if (t.AccountId != c.AccountId)
                    {
                        // Must be within date window
                        if (IsWithinDateWindow(t.Date, c.Date))
                        {
                            // Optional: description heuristic
                            if (LooksLikeTransfer(t.Description, c.Description))
                            {
                                isMatched = true;
                            }
                        }
                    }
                }
            }

            return isMatched;
        }

        private bool IsWithinDateWindow(DateTime d1, DateTime d2)
        {
            return Math.Abs((d1.Date - d2.Date).TotalDays) <= _dateWindowSize;
        }

        private bool LooksLikeTransfer(string d1, string d2)
        {
            var text = (d1 + " " + d2).ToUpperInvariant();
            return text.Contains("TRANSFER")
                || text.Contains("ACH")
                || text.Contains("ONLINE")
                || text.Contains("PAYMENT");
        }

        private void Categorize(IEnumerable<PostedTransaction> txs)
        {
            foreach (var tx in txs)
            {
                Categorize(tx);
            }
        }

        private void Categorize(PostedTransaction tx)
        {
            // 1. If user already assigned a category, do nothing
            if (string.IsNullOrWhiteSpace(tx.Category))
            {
                // 2. If Plaid provided a category, use it
                if (!string.IsNullOrWhiteSpace(tx.PlaidCategory))
                {
                    tx.Category = tx.PlaidCategory;
                }
                else
                {
                    var categoryByDescription = GetCategoryByDescription(tx.Description);
                    if (categoryByDescription is not null)
                    {
                        tx.Category = categoryByDescription;
                    }
                    else
                    {
                        // 5. Default category (optional)
                        tx.Category = "Uncategorized";
                    }
                }
            }
        }

        private string? GetCategoryByDescription(string description)
        {
            string category = null;

            // 3. Try merchant-based rules (optional future feature)
            //var merchantCategory = MerchantCategoryRules.TryGetCategory(description);
            //if (merchantCategory is not null)
            //{
            //    category = merchantCategory;
            //}
            //else
            //{
            //    // 4. Try description-based rules (optional future feature)
            //    var descriptionCategory = DescriptionCategoryRules.TryGetCategory(description);
            //    if (descriptionCategory is not null)
            //    {
            //        category = descriptionCategory;
            //    }
            //}

            return category;
        }

        public class TransactionImportResult
        {
            public Guid AccountId { get; set; }
            public int PostedImported { get; set; }
            public int TransfersDetected { get; set; }
        }
    }
}
