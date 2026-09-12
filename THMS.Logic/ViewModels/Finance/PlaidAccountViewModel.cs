namespace THMS.Logic.ViewModels.Finance
{
    public class PlaidAccountViewModel
    {
        public string Institution { get; set; } = "";
        public string PlaidAccountId { get; set; } = "";
        public string Mask { get; set; } = "";
        public string Subtype { get; set; } = "";
        public Guid SuggestedThmsAccountId { get; set; }
        public string Name { get; set; } = "";
        public string AccessToken { get; set; } = "";
        public string ItemId { get; set; } = "";
        public string InstitutionId { get; set; } = "";
    }

    public class AccountMappingChoice
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
    }
}
