namespace THMS.Logic.ViewModels.Energy
{
    public class SolarIntervalImportPreview
    {
        public DateTime Timestamp { get; set; }
        public decimal EnergyProducedWh { get; set; }
        public decimal EnergyConsumedWh { get; set; }
        public decimal ExportedToGridWh { get; set; }
        public decimal ImportedFromGridWh { get; set; }
        public decimal StoredInBatteriesWh { get; set; }
        public decimal DischargedFromBatteriesWh { get; set; }
    }
}
