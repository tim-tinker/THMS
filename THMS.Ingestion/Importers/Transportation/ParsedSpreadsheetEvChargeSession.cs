namespace THMS.Ingestion.Importers.Transportation
{
    public sealed class ParsedSpreadsheetEvChargeSession
    {
        public DateTime StartTime { get; init; }
        public DateTime EndTime { get; init; }
        public decimal OdometerMiles { get; init; }
        public decimal StartSoc { get; init; }
        public decimal EndSoc { get; init; }
        public decimal KwhAdded { get; init; }
        public decimal KwhDrawn { get; init; }
        public decimal CostAdded { get; init; }
        public bool IsHomeCharge { get; init; }
        public string Charger { get; init; } = "";
    }
}
