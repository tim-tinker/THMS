using THMS.Domain.Finance.Transactions;
using THMS.External;

namespace THMS.Logic.Mapping
{
    public static class TransactionExtension
    {
        public static PostedTransaction ToPostedTransaction(
            this TransactionDto dto,
            Guid accountId)
        {
            return new PostedTransaction
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                Date = dto.Date ?? DateTime.Today,
                Amount = dto.Amount,
                Description = dto.Name,
                PlaidCategory = dto.Category,
                Category = null
            };
        }
    }
}
