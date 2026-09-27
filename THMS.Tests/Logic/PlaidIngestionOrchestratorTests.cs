using THMS.Data.Stores;
using THMS.Data.Stores.SQLite;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Transactions;
using THMS.External;
using THMS.External.Plaid;
using Going.Plaid.Entity;
using Going.Plaid.Link;
using THMS.Logic.Orchestrators;
using THMS.Logic.Orchestrators.Finance;
using THMS.Logic.ViewModels;
using THMS.Logic.ViewModels.Finance;
using THMS.Tests.Logic.TestSupport;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class PlaidAccountOrchestratorTests
    {
        [Test]
        public async Task StartLinkFlow_LoadsAccountsAndSaveMappingsWritesExternalLink()
        {
            var accounts = new InMemoryAccountDataStore();
            var checking = new BankAccount
            {
                Name = "Checking",
                Institution = "",
                AccountNumber = "1234",
                WebsiteUrl = ""
            };
            accounts.UpsertAccount(checking);
            var link = new FakePlaidLinkSession();
            var fetcher = new FakeAccountFetcher
            {
                Accounts =
                [
                    new AccountDto
                    {
                        PlaidAccountId = "plaid-checking",
                        Name = "Checking",
                        Mask = "1234",
                        Subtype = "checking"
                    }
                ]
            };
            var orchestrator = new PlaidAccountOrchestrator(accounts, fetcher, link, "Sandbox");

            await orchestrator.StartLinkFlow();

            Assert.That(link.CreatedLinkToken, Is.False);
            Assert.That(link.CreatedSandboxToken, Is.True);
            Assert.That(link.LastPublicToken, Is.EqualTo(link.SandboxPublicToken));
            Assert.That(fetcher.LastAccessToken, Is.EqualTo(link.AccessToken));

            var rows = orchestrator.GetPlaidAccounts();
            Assert.That(rows, Has.Count.EqualTo(1));
            Assert.That(rows[0].Institution, Is.EqualTo("First Platypus Bank"));
            Assert.That(rows[0].SuggestedThmsAccountId, Is.EqualTo(checking.Id));

            var saved = orchestrator.SaveAccountMappings(rows);
            Assert.That(saved, Is.EqualTo(1));

            var linked = accounts.GetAccount("Checking")!;
            Assert.That(linked.ExternalLink, Is.Not.Null);
            Assert.That(linked.ExternalLink!.PlaidAccountId, Is.EqualTo("plaid-checking"));
            Assert.That(linked.ExternalLink.AccessToken, Is.EqualTo(link.AccessToken));
            Assert.That(linked.ExternalLink.ItemId, Is.EqualTo(link.ItemId));
            Assert.That(linked.ExternalLink.AccountMask, Is.EqualTo("1234"));
            Assert.That(linked.ExternalLink.InstitutionName, Is.EqualTo("First Platypus Bank"));
            Assert.That(linked.Institution, Is.EqualTo("First Platypus Bank"));
            var itemState = accounts.GetPlaidItemSyncState(link.ItemId);
            Assert.That(itemState, Is.Not.Null);
            Assert.That(itemState!.HasCursor, Is.False);
            Assert.That(itemState.Status, Is.EqualTo(PlaidItemSyncStatus.NotSynced));
        }

        [Test]
        public void SaveAccountMappings_UpdatesExistingExternalLinkObject()
        {
            var accounts = new InMemoryAccountDataStore();
            var checking = NewBank("Checking", "1234");
            accounts.UpsertAccount(checking);
            var orchestrator = Create(accounts);
            var row = MappedRow(checking.Id, "plaid-checking", "1234");

            orchestrator.SaveAccountMappings([row]);
            var firstLink = accounts.GetAccount("Checking")!.ExternalLink;
            Assert.That(firstLink, Is.Not.Null);

            row.AccessToken = "tok-2";
            row.ItemId = "item-2";
            var saved = orchestrator.SaveAccountMappings([row]);
            Assert.That(saved, Is.EqualTo(1));

            var relinked = accounts.GetAccount("Checking")!;
            Assert.That(relinked.ExternalLink, Is.SameAs(firstLink));
            Assert.That(relinked.ExternalLink!.AccessToken, Is.EqualTo("tok-2"));
            Assert.That(relinked.ExternalLink.ItemId, Is.EqualTo("item-2"));
        }

        [Test]
        public void SaveAccountMappings_ClearsStaleLinkWhenPlaidAccountMoves()
        {
            var accounts = new InMemoryAccountDataStore();
            var checking = NewBank("Checking", "1234");
            var savings = NewBank("Savings", "5678");
            accounts.UpsertAccount(checking);
            accounts.UpsertAccount(savings);
            var orchestrator = Create(accounts);
            var row = MappedRow(checking.Id, "plaid-checking", "1234");

            orchestrator.SaveAccountMappings([row]);
            Assert.That(accounts.GetAccount("Checking")!.ExternalLink, Is.Not.Null);

            row.SuggestedThmsAccountId = savings.Id;
            var saved = orchestrator.SaveAccountMappings([row]);
            Assert.That(saved, Is.EqualTo(1));
            Assert.That(accounts.GetAccount("Checking")!.ExternalLink, Is.Null);
            Assert.That(accounts.GetAccount("Savings")!.ExternalLink, Is.Not.Null);
            Assert.That(accounts.GetAccount("Savings")!.ExternalLink!.PlaidAccountId, Is.EqualTo("plaid-checking"));
        }

        [Test]
        public void ItemsNeedingInitialHistory_ReturnsMappedItemsWithoutCursor()
        {
            var accounts = new InMemoryAccountDataStore();
            var checking = NewBank("Checking", "1234");
            accounts.UpsertAccount(checking);
            var orchestrator = Create(accounts);
            var row = MappedRow(checking.Id, "plaid-checking", "1234");

            orchestrator.SaveAccountMappings([row]);
            var needing = orchestrator.ItemsNeedingInitialHistory([row]);
            Assert.That(needing, Has.Count.EqualTo(1));
            Assert.That(needing[0].ItemId, Is.EqualTo("item"));

            accounts.UpsertPlaidItemSyncState(new PlaidItemSyncState
            {
                ItemId = "item",
                Cursor = "cursor-1",
                Status = PlaidItemSyncStatus.Synced
            });
            Assert.That(orchestrator.ItemsNeedingInitialHistory([row]), Is.Empty);
        }

        [Test]
        public void SaveAccountMappings_RemovesStaleLinkFromSqliteStore()
        {
            var path = Path.Combine(Path.GetTempPath(), $"thms-plaid-remap-{Guid.NewGuid():N}.db");
            try
            {
                var accounts = new SQLiteAccountDataStore(path);
                var checking = NewBank("Checking", "1234");
                var savings = NewBank("Savings", "5678");
                accounts.UpsertAccount(checking);
                accounts.UpsertAccount(savings);
                var orchestrator = Create(accounts);
                var row = MappedRow(checking.Id, "plaid-checking", "1234");

                orchestrator.SaveAccountMappings([row]);
                row.SuggestedThmsAccountId = savings.Id;
                orchestrator.SaveAccountMappings([row]);

                Assert.That(accounts.GetAccount("Checking")!.ExternalLink, Is.Null);
                Assert.That(accounts.GetAccount("Savings")!.ExternalLink!.PlaidAccountId, Is.EqualTo("plaid-checking"));
            }
            finally
            {
                TryDeleteSqlite(path);
            }
        }

        [Test]
        public async Task StartLinkFlow_RequiresPublicTokenOutsideSandbox()
        {
            var orchestrator = new PlaidAccountOrchestrator(
                new InMemoryAccountDataStore(),
                new FakeAccountFetcher(),
                new FakePlaidLinkSession(),
                "Production");

            Assert.That(
                async () => await orchestrator.StartLinkFlow(),
                Throws.InvalidOperationException);
        }

        [Test]
        public async Task StartLinkFlow_WithPublicToken_SkipsSandboxAndUnusedLinkToken()
        {
            var accounts = new InMemoryAccountDataStore();
            var link = new FakePlaidLinkSession();
            var fetcher = new FakeAccountFetcher
            {
                Accounts =
                [
                    new AccountDto
                    {
                        PlaidAccountId = "plaid-checking",
                        Name = "Checking",
                        Mask = "1234",
                        Subtype = "checking"
                    }
                ]
            };
            var orchestrator = new PlaidAccountOrchestrator(accounts, fetcher, link, "Production");

            await orchestrator.StartLinkFlow("public-real");

            Assert.That(link.CreatedLinkToken, Is.False);
            Assert.That(link.CreatedSandboxToken, Is.False);
            Assert.That(link.LastPublicToken, Is.EqualTo("public-real"));
            Assert.That(orchestrator.GetPlaidAccounts(), Has.Count.EqualTo(1));
        }

        [Test]
        public async Task CreateHostedLinkSession_ReturnsHostedUrl()
        {
            var link = new FakePlaidLinkSession();
            var orchestrator = Create(new InMemoryAccountDataStore(), link);

            var session = await orchestrator.CreateHostedLinkSessionAsync();

            Assert.That(link.CreatedLinkToken, Is.True);
            Assert.That(link.HostedLinkRequested, Is.True);
            Assert.That(session.LinkToken, Is.EqualTo(link.LinkToken));
            Assert.That(session.HostedLinkUrl, Is.EqualTo(link.HostedLinkUrl));
        }

        [Test]
        public void CreateHostedLinkSession_ThrowsWhenHostedUrlMissing()
        {
            var link = new FakePlaidLinkSession { HostedLinkUrl = "" };
            var orchestrator = Create(new InMemoryAccountDataStore(), link);

            Assert.That(
                async () => await orchestrator.CreateHostedLinkSessionAsync(),
                Throws.InvalidOperationException);
        }

        [Test]
        public async Task GetPublicTokenFromLinkSession_ReturnsSessionToken()
        {
            var link = new FakePlaidLinkSession();
            var orchestrator = Create(new InMemoryAccountDataStore(), link);

            var token = await orchestrator.GetPublicTokenFromLinkSessionAsync(link.LinkToken);

            Assert.That(token, Is.EqualTo(link.SessionPublicToken));
            Assert.That(link.LastLinkTokenGet, Is.EqualTo(link.LinkToken));
        }

        [Test]
        public async Task WaitForPublicTokenAsync_RetriesUntilAvailable()
        {
            var link = new FakePlaidLinkSession { PublicTokenLookupsUntilAvailable = 3 };
            var orchestrator = Create(new InMemoryAccountDataStore(), link);

            var token = await orchestrator.WaitForPublicTokenAsync(link.LinkToken, attempts: 5, delayMs: 1);

            Assert.That(token, Is.EqualTo(link.SessionPublicToken));
            Assert.That(link.PublicTokenLookups, Is.EqualTo(3));
        }

        [Test]
        public void ExtractPublicToken_ReadsItemAddResultsThenLegacyOnSuccess()
        {
            var fromResults = new LinkTokenGetResponse
            {
                LinkSessions =
                [
                    new LinkTokenGetSessionsResponse
                    {
                        Results = new LinkSessionResults
                        {
                            ItemAddResults =
                            [
                                new LinkSessionItemAddResult { PublicToken = "public-live" }
                            ]
                        }
                    }
                ]
            };
            Assert.That(PlaidLinkManager.ExtractPublicToken(fromResults), Is.EqualTo("public-live"));

#pragma warning disable CS0612
            var fromLegacy = new LinkTokenGetResponse
            {
                LinkSessions =
                [
                    new LinkTokenGetSessionsResponse
                    {
                        OnSuccess = new LinkSessionSuccess { PublicToken = "public-legacy" }
                    }
                ]
            };
#pragma warning restore CS0612
            Assert.That(PlaidLinkManager.ExtractPublicToken(fromLegacy), Is.EqualTo("public-legacy"));

            var exited = new LinkTokenGetResponse
            {
                LinkSessions = [new LinkTokenGetSessionsResponse()]
            };
            Assert.That(PlaidLinkManager.ExtractPublicToken(exited), Is.Null);
        }

        [Test]
        public void IsHostedLinkCompletion_MatchesThmsUri()
        {
            Assert.That(
                PlaidLinkManager.IsHostedLinkCompletion(new Uri("thms://plaid-link-complete")),
                Is.True);
            Assert.That(
                PlaidLinkManager.IsHostedLinkCompletion(new Uri("thms://plaid-link-complete/")),
                Is.True);
            Assert.That(
                PlaidLinkManager.IsHostedLinkCompletion(new Uri("https://cdn.plaid.com/link")),
                Is.False);
        }

        private static PlaidAccountOrchestrator Create(IAccountDataStore accounts) =>
            Create(accounts, new FakePlaidLinkSession());

        private static PlaidAccountOrchestrator Create(IAccountDataStore accounts, FakePlaidLinkSession link) =>
            new(accounts, new FakeAccountFetcher(), link, "Sandbox");

        private static BankAccount NewBank(string name, string accountNumber) =>
            new()
            {
                Name = name,
                Institution = "",
                AccountNumber = accountNumber,
                WebsiteUrl = ""
            };

        private static PlaidAccountViewModel MappedRow(Guid thmsAccountId, string plaidAccountId, string mask) =>
            new()
            {
                Institution = "First Platypus Bank",
                PlaidAccountId = plaidAccountId,
                Mask = mask,
                Subtype = "checking",
                SuggestedThmsAccountId = thmsAccountId,
                Name = "Checking",
                AccessToken = "tok",
                ItemId = "item",
                InstitutionId = "ins_1"
            };

        private static void TryDeleteSqlite(string path)
        {
            foreach (var file in new[] { path, path + "-wal", path + "-shm" })
            {
                try
                {
                    if (File.Exists(file))
                        File.Delete(file);
                }
                catch (IOException)
                {
                }
            }
        }
    }

    [TestFixture]
    public class TransactionFileImportOrchestratorTests
    {
        [Test]
        public void LoadTransactionsFromFiles_ThrowsWhenMissing()
        {
            var orchestrator = new TransactionImportOrchestrator(
                new FakeTransactionFetcher(),
                new InMemoryTransactionDataStore(),
                new InMemoryAccountDataStore());

            Assert.That(
                () => orchestrator.LoadTransactionsFromFiles([@"C:\missing\transactions.xlsx"]),
                Throws.TypeOf<FileNotFoundException>());
        }

        [Test]
        public void ImportTransactions_PersistsCategorizesAndSkipsDuplicates()
        {
            var accounts = new InMemoryAccountDataStore();
            var txs = new InMemoryTransactionDataStore();
            var checking = new BankAccount
            {
                Name = "Checking",
                Institution = "Bank",
                AccountNumber = "1",
                WebsiteUrl = ""
            };
            accounts.UpsertAccount(checking);
            var orchestrator = new TransactionImportOrchestrator(
                new FakeTransactionFetcher(),
                txs,
                accounts);
            var rows = new List<TransactionImportPreview>
            {
                new()
                {
                    Date = new DateTime(2026, 3, 1),
                    Description = "Coffee",
                    Amount = -4.50m,
                    Account = "Checking",
                    Category = "Restaurants",
                    AccountId = checking.Id
                },
                new()
                {
                    Date = new DateTime(2026, 3, 1),
                    Description = "Coffee",
                    Amount = -4.50m,
                    Account = "Checking",
                    Category = "Restaurants",
                    AccountId = checking.Id
                }
            };

            var reports = new List<ImportProgress>();
            var imported = orchestrator.ImportTransactions(
                rows,
                new CollectingProgress<ImportProgress>(reports));

            Assert.That(imported.Count, Is.EqualTo(1));
            Assert.That(imported.Start, Is.EqualTo(new DateTime(2026, 3, 1)));
            Assert.That(imported.End, Is.EqualTo(new DateTime(2026, 3, 1)));
            Assert.That(reports.Select(r => r.Total).Distinct().ToList(), Is.EqualTo(new[] { 1 }));
            Assert.That(reports.Last().Completed, Is.EqualTo(imported.Count));
            var posted = txs.GetPostedTransactions(checking.Id).ToList();
            Assert.That(posted, Has.Count.EqualTo(1));
            Assert.That(posted[0].Category, Is.EqualTo("Restaurants"));
            Assert.That(posted[0].CategoryId, Is.Not.Null);
        }

        [Test]
        public void ImportTransactions_SkipsDuplicateExternalTransactionId()
        {
            var accounts = new InMemoryAccountDataStore();
            var txs = new InMemoryTransactionDataStore();
            var checking = new BankAccount
            {
                Name = "Checking",
                Institution = "Bank",
                AccountNumber = "1",
                WebsiteUrl = ""
            };
            accounts.UpsertAccount(checking);
            var orchestrator = new TransactionImportOrchestrator(
                new FakeTransactionFetcher(),
                txs,
                accounts);
            var first = new TransactionImportPreview
            {
                Date = new DateTime(2026, 3, 1),
                Description = "Coffee",
                Amount = -4.50m,
                Account = "Checking",
                AccountId = checking.Id,
                ExternalTransactionId = "plaid-tx-1"
            };
            var second = new TransactionImportPreview
            {
                Date = new DateTime(2026, 3, 2),
                Description = "Coffee again",
                Amount = -9m,
                Account = "Checking",
                AccountId = checking.Id,
                ExternalTransactionId = "plaid-tx-1"
            };

            orchestrator.ImportTransactions([first]);
            var imported = orchestrator.ImportTransactions([second]);

            Assert.That(imported.Count, Is.EqualTo(0));
            Assert.That(txs.GetPostedTransactions(checking.Id).ToList(), Has.Count.EqualTo(1));
        }
    }

    [TestFixture]
    public class PlaidTransactionOrchestratorTests
    {
        [Test]
        public async Task DownloadNewTransactions_MapsByPlaidAccountId_AndImportSkipsPending()
        {
            var accounts = new InMemoryAccountDataStore();
            var txs = new InMemoryTransactionDataStore();
            var checking = new BankAccount
            {
                Name = "Checking",
                Institution = "Bank",
                AccountNumber = "1",
                WebsiteUrl = "",
                ExternalLink = new ExternalAccountLink
                {
                    AccessToken = "tok",
                    PlaidAccountId = "plaid-checking",
                    ItemId = "item",
                    InstitutionId = "ins_1",
                    AccountMask = "1111"
                }
            };
            accounts.UpsertAccount(checking);
            var fetcher = new FakeTransactionFetcher
            {
                Transactions =
                [
                    new TransactionDto
                    {
                        AccountId = "plaid-checking",
                        Amount = -12.34m,
                        Date = new DateTime(2026, 4, 2),
                        Name = "Market",
                        Category = "Groceries",
                        Pending = false
                    },
                    new TransactionDto
                    {
                        AccountId = "plaid-checking",
                        Amount = -1m,
                        Date = new DateTime(2026, 4, 3),
                        Name = "Hold",
                        Pending = true
                    },
                    new TransactionDto
                    {
                        AccountId = "other-plaid",
                        Amount = -9m,
                        Date = new DateTime(2026, 4, 2),
                        Name = "Other bank"
                    }
                ]
            };
            var orchestrator = new PlaidTransactionOrchestrator(accounts, fetcher, txs);

            var preview = await orchestrator.DownloadNewTransactions(
                new DateTime(2026, 4, 1),
                new DateTime(2026, 4, 30));

            Assert.That(preview, Has.Count.EqualTo(2));
            Assert.That(preview.Any(r => r.Description == "Market" && r.AccountId == checking.Id), Is.True);
            Assert.That(preview.Any(r => r.Pending && r.Description == "Hold"), Is.True);
            Assert.That(preview.Any(r => r.Description == "Other bank"), Is.False);

            var imported = orchestrator.ImportTransactions(preview);
            Assert.That(imported.Count, Is.EqualTo(1));
            Assert.That(txs.GetPostedTransactions(checking.Id).Single().Description, Is.EqualTo("Market"));
        }

        [Test]
        public void DownloadNewTransactions_ThrowsWhenNoLinkedAccounts()
        {
            var orchestrator = new PlaidTransactionOrchestrator(
                new InMemoryAccountDataStore(),
                new FakeTransactionFetcher(),
                new InMemoryTransactionDataStore());

            Assert.That(
                async () => await orchestrator.DownloadNewTransactions(DateTime.Today.AddDays(-30), DateTime.Today),
                Throws.InvalidOperationException);
        }

        [Test]
        public async Task SyncInitial_ImportsUnreconciledSkipsPendingAndOlderThanHistoryStart()
        {
            var (accounts, txs, checking, fetcher) = Linked();
            fetcher.SyncPages.Enqueue(new TransactionSyncPage
            {
                Added =
                [
                    Tx("tx-old", checking, new DateTime(2026, 1, 1), "Old", -5m),
                    Tx("tx-ok", checking, new DateTime(2026, 4, 2), "Market", -12.34m),
                    Tx("tx-pending", checking, new DateTime(2026, 4, 3), "Hold", -1m, pending: true),
                    Tx("tx-other", "other-plaid", new DateTime(2026, 4, 2), "Other bank", -9m)
                ],
                NextCursor = "cursor-1",
                HasMore = false
            });
            var orchestrator = new PlaidTransactionOrchestrator(accounts, fetcher, txs);

            var result = await orchestrator.SyncInitialAsync("item", new DateTime(2026, 3, 1));

            Assert.That(result.Imported, Is.EqualTo(1));
            Assert.That(result.ItemsSynced, Is.EqualTo(1));
            var posted = txs.GetPostedTransactions(checking.Id).Single();
            Assert.That(posted.Description, Is.EqualTo("Market"));
            Assert.That(posted.ExternalTransactionId, Is.EqualTo("tx-ok"));
            Assert.That(posted.ImportedStatus, Is.EqualTo(ImportedStatus.Unreconciled));
            var state = accounts.GetPlaidItemSyncState("item");
            Assert.That(state, Is.Not.Null);
            Assert.That(state!.Cursor, Is.EqualTo("cursor-1"));
            Assert.That(state.Status, Is.EqualTo(PlaidItemSyncStatus.Synced));
            Assert.That(state.HistoryStartDate, Is.EqualTo(new DateTime(2026, 3, 1)));
            Assert.That(fetcher.LastSyncCursor, Is.EqualTo(""));
        }

        [Test]
        public async Task SyncIncremental_SkipsItemsWithoutCursor()
        {
            var (accounts, txs, _, fetcher) = Linked();
            fetcher.SyncPages.Enqueue(new TransactionSyncPage
            {
                Added = [Tx("tx-1", "plaid-checking", new DateTime(2026, 4, 2), "Market", -12m)],
                NextCursor = "cursor-1",
                HasMore = false
            });
            var orchestrator = new PlaidTransactionOrchestrator(accounts, fetcher, txs);

            var result = await orchestrator.SyncIncrementalAsync();

            Assert.That(result.Imported, Is.EqualTo(0));
            Assert.That(result.ItemsSkipped, Is.EqualTo(1));
            Assert.That(result.Summary, Does.Contain("Import history"));
            Assert.That(txs.GetPostedTransactions(DateTime.MinValue, DateTime.MaxValue), Is.Empty);
            Assert.That(fetcher.LastSyncAccessToken, Is.Null);
        }

        [Test]
        public async Task SyncIncremental_AfterCursor_ImportsAdded()
        {
            var (accounts, txs, checking, fetcher) = Linked();
            accounts.UpsertPlaidItemSyncState(new PlaidItemSyncState
            {
                ItemId = "item",
                Cursor = "cursor-1",
                Status = PlaidItemSyncStatus.Synced
            });
            fetcher.SyncPages.Enqueue(new TransactionSyncPage
            {
                Added = [Tx("tx-2", checking, new DateTime(2026, 4, 4), "Payroll", 100m)],
                NextCursor = "cursor-2",
                HasMore = false
            });
            var orchestrator = new PlaidTransactionOrchestrator(accounts, fetcher, txs);

            var result = await orchestrator.SyncIncrementalAsync();

            Assert.That(result.Imported, Is.EqualTo(1));
            Assert.That(result.ItemsSynced, Is.EqualTo(1));
            Assert.That(fetcher.LastSyncCursor, Is.EqualTo("cursor-1"));
            Assert.That(accounts.GetPlaidItemSyncState("item")!.Cursor, Is.EqualTo("cursor-2"));
            Assert.That(txs.GetPostedTransactions(checking.Id).Single().Description, Is.EqualTo("Payroll"));
        }

        [Test]
        public async Task SyncIncremental_SetsNeedsAuthOnLoginRequired()
        {
            var (accounts, txs, _, fetcher) = Linked();
            accounts.UpsertPlaidItemSyncState(new PlaidItemSyncState
            {
                ItemId = "item",
                Cursor = "cursor-1",
                Status = PlaidItemSyncStatus.Synced
            });
            fetcher.Exception = new PlaidItemLoginRequiredException("ITEM_LOGIN_REQUIRED");
            var orchestrator = new PlaidTransactionOrchestrator(accounts, fetcher, txs);

            var result = await orchestrator.SyncIncrementalAsync();

            Assert.That(result.ItemsNeedingAuth, Is.EqualTo(1));
            Assert.That(result.ItemsSynced, Is.EqualTo(0));
            var state = accounts.GetPlaidItemSyncState("item")!;
            Assert.That(state.Status, Is.EqualTo(PlaidItemSyncStatus.NeedsAuth));
            Assert.That(state.Cursor, Is.EqualTo("cursor-1"));
            Assert.That(orchestrator.HasItemsReadyForIncrementalSync(), Is.False);
        }

        [Test]
        public async Task Sync_RemovesUnreconciledButKeepsMatched()
        {
            var (accounts, txs, checking, fetcher) = Linked();
            accounts.UpsertPlaidItemSyncState(new PlaidItemSyncState
            {
                ItemId = "item",
                Cursor = "cursor-1",
                Status = PlaidItemSyncStatus.Synced
            });
            var keep = new PostedTransaction
            {
                AccountId = checking.Id,
                Date = new DateTime(2026, 4, 1),
                Description = "Keep",
                Amount = -3m,
                ExternalTransactionId = "tx-keep",
                ImportedStatus = ImportedStatus.Matched
            };
            var drop = new PostedTransaction
            {
                AccountId = checking.Id,
                Date = new DateTime(2026, 4, 1),
                Description = "Drop",
                Amount = -4m,
                ExternalTransactionId = "tx-drop",
                ImportedStatus = ImportedStatus.Unreconciled
            };
            txs.AddPostedTransaction(keep);
            txs.AddPostedTransaction(drop);
            fetcher.SyncPages.Enqueue(new TransactionSyncPage
            {
                RemovedIds = ["tx-keep", "tx-drop"],
                NextCursor = "cursor-2",
                HasMore = false
            });
            var orchestrator = new PlaidTransactionOrchestrator(accounts, fetcher, txs);

            await orchestrator.SyncIncrementalAsync();

            var remaining = txs.GetPostedTransactions(checking.Id).ToList();
            Assert.That(remaining, Has.Count.EqualTo(1));
            Assert.That(remaining[0].ExternalTransactionId, Is.EqualTo("tx-keep"));
        }

        [Test]
        public async Task Sync_SavesCursorAfterEachPage()
        {
            var (accounts, txs, checking, fetcher) = Linked();
            fetcher.SyncPages.Enqueue(new TransactionSyncPage
            {
                Added = [Tx("tx-1", checking, new DateTime(2026, 4, 2), "One", -1m)],
                NextCursor = "cursor-a",
                HasMore = true
            });
            fetcher.SyncPages.Enqueue(new TransactionSyncPage
            {
                Added = [Tx("tx-2", checking, new DateTime(2026, 4, 3), "Two", -2m)],
                NextCursor = "cursor-b",
                HasMore = false
            });
            var orchestrator = new PlaidTransactionOrchestrator(accounts, fetcher, txs);

            var result = await orchestrator.SyncInitialAsync("item", new DateTime(2026, 1, 1));

            Assert.That(result.Imported, Is.EqualTo(2));
            Assert.That(accounts.GetPlaidItemSyncState("item")!.Cursor, Is.EqualTo("cursor-b"));
            Assert.That(txs.GetPostedTransactions(checking.Id).ToList(), Has.Count.EqualTo(2));
        }

        private static (InMemoryAccountDataStore Accounts, InMemoryTransactionDataStore Txs, BankAccount Checking, FakeTransactionFetcher Fetcher) Linked()
        {
            var accounts = new InMemoryAccountDataStore();
            var txs = new InMemoryTransactionDataStore();
            var checking = new BankAccount
            {
                Name = "Checking",
                Institution = "Bank",
                AccountNumber = "1",
                WebsiteUrl = "",
                ExternalLink = new ExternalAccountLink
                {
                    AccessToken = "tok",
                    PlaidAccountId = "plaid-checking",
                    ItemId = "item",
                    InstitutionId = "ins_1",
                    AccountMask = "1111",
                    InstitutionName = "Bank"
                }
            };
            accounts.UpsertAccount(checking);
            return (accounts, txs, checking, new FakeTransactionFetcher());
        }

        private static TransactionDto Tx(
            string id,
            BankAccount account,
            DateTime date,
            string name,
            decimal amount,
            bool pending = false) =>
            Tx(id, account.ExternalLink!.PlaidAccountId, date, name, amount, pending);

        private static TransactionDto Tx(
            string id,
            string plaidAccountId,
            DateTime date,
            string name,
            decimal amount,
            bool pending = false) =>
            new()
            {
                TransactionId = id,
                AccountId = plaidAccountId,
                Date = date,
                Name = name,
                Amount = amount,
                Pending = pending
            };
    }

    file sealed class CollectingProgress<T>(List<T> items) : IProgress<T>
    {
        public void Report(T value) => items.Add(value);
    }
}
