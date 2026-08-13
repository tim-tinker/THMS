using THMS.Domain.Energy;

namespace THMS.Ingestion.EnergyIngestion
{
    public class EnergyIngestionResult
    {
        public List<string> Errors { get; set; } = new();
        public HomeCircuitAttribution? Attribution { get; set; }
    }
}
