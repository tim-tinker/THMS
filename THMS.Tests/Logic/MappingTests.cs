using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Transactions;
using THMS.External;
using THMS.Logic.Mapping;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class AccountExtensionTests
    {
        [Test]
        public void ToDto_CopiesLinkFields_AndAccountName()
        {
            var account = new BankAccount
            {
                Name = "Checking",
                ExternalLink = new ExternalAccountLink
                {
                    Provider = "Plaid",
                    ItemId = "item",
                    AccessToken = "token",
                    PlaidAccountId = "plaid-1",
                    InstitutionId = "ins",
                    AccountMask = "1234"
                }
            };

            var dto = account.ToDto();
            Assert.That(dto.Name, Is.EqualTo("Checking"));
            Assert.That(dto.Provider, Is.EqualTo("Plaid"));
            Assert.That(dto.ItemId, Is.EqualTo("item"));
            Assert.That(dto.AccessToken, Is.EqualTo("token"));
            Assert.That(dto.PlaidAccountId, Is.EqualTo("plaid-1"));
            Assert.That(dto.InstitutionId, Is.EqualTo("ins"));
            Assert.That(dto.Mask, Is.EqualTo("1234"));
            Assert.That(dto.Type, Is.EqualTo(""));
            Assert.That(dto.Available, Is.Null);
        }

        [Test]
        public void ToDto_WithoutLink_UsesEmptyExternalFields()
        {
            var dto = new BankAccount { Name = "Cash" }.ToDto();
            Assert.That(dto.Provider, Is.EqualTo(""));
            Assert.That(dto.AccessToken, Is.EqualTo(""));
            Assert.That(dto.Name, Is.EqualTo("Cash"));
        }
    }

    [TestFixture]
    public class TransactionExtensionTests
    {
        [Test]
        public void ToPostedTransaction_MapsFields_AndDefaultsNullDate()
        {
            var accountId = Guid.NewGuid();
            var dated = new TransactionDto
            {
                Date = new DateTime(2026, 2, 2),
                Amount = 12.34m,
                Name = "Coffee",
                Category = "Food"
            }.ToPostedTransaction(accountId);

            Assert.That(dated.AccountId, Is.EqualTo(accountId));
            Assert.That(dated.Date, Is.EqualTo(new DateTime(2026, 2, 2)));
            Assert.That(dated.Amount, Is.EqualTo(12.34m));
            Assert.That(dated.Description, Is.EqualTo("Coffee"));
            Assert.That(dated.PlaidCategory, Is.EqualTo("Food"));
            Assert.That(dated.Category, Is.Null);
            Assert.That(dated.Id, Is.Not.EqualTo(Guid.Empty));

            var undated = new TransactionDto { Amount = 1, Name = "X" }.ToPostedTransaction(accountId);
            Assert.That(undated.Date.Date, Is.EqualTo(DateTime.Today));
        }
    }
}
