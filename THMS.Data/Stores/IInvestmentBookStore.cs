using THMS.Domain.Finance.Investments;

namespace THMS.Data.Stores
{
    public interface IInvestmentBookStore
    {
        IReadOnlyList<InvestmentAction> GetActions(Guid accountId);
        void UpsertAction(InvestmentAction action);

        IReadOnlyList<SecurityQuote> GetQuotes(Guid accountId);
        void UpsertQuote(SecurityQuote quote);

        IReadOnlyList<InvestmentSecurity> GetSecurities();
        void UpsertSecurity(InvestmentSecurity security);

        void DeleteAccount(Guid accountId);
    }
}
