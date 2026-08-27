namespace THMS.External
{
    public interface IExternalAccountFetcher
    {
        Task<List<AccountDto>> FetchAccountsAsync(string accessToken);

    }
}
