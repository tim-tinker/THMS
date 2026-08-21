using Going.Plaid.Entity;

namespace THMS.External.Plaid
{
    public static class TransactionExtension
    {
        public static PlaidTransactionDto ToDto(this Transaction tx)
        {
            return new PlaidTransactionDto
            {
                TransactionId = tx.TransactionId ?? "",
                AccountId = tx.AccountId ?? "",
                Amount = tx.Amount ?? 0m,
                Date =  tx.AuthorizedDate?.ToDateTime(TimeOnly.MinValue) ?? tx.Date?.ToDateTime(TimeOnly.MinValue),
                Name = tx.MerchantName ?? tx.OriginalDescription ?? "",
                Category = tx.PersonalFinanceCategory?.Primary
                    ?? tx.PersonalFinanceCategory?.Detailed,
                Pending = tx.Pending ?? false
            };
        }
    }
}
