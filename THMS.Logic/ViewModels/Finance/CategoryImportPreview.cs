namespace THMS.Logic.ViewModels.Finance
{
    public class CategoryImportPreview
    {
        public string Name { get; set; } = "";
        public string Parent { get; set; } = "";
        public string Frequency { get; set; } = "";
        public decimal? Amount { get; set; }
        public DateTime? PeriodStart { get; set; }
    }
}
