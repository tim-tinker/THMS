namespace THMS.External
{
    public class AccountDto
    {
        // Existing fields
        public string PlaidAccountId { get; set; } = "";
        public string Name { get; set; } = "";
        public string Mask { get; set; } = "";
        public string Type { get; set; } = "";
        public string Subtype { get; set; } = "";
        public decimal? Available { get; set; }
        public decimal? Current { get; set; }
        public decimal? Limit { get; set; }

        // ExternalLink-derived fields
        public string Provider { get; set; } = "";
        public string ItemId { get; set; } = "";
        public string AccessToken { get; set; } = "";
        public string InstitutionId { get; set; } = "";
    }
}
