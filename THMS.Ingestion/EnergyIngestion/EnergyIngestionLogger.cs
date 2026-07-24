namespace THMS.Ingestion.EnergyIngestion
{
    public class EnergyIngestionLogger
    {
        public void LogInfo(string message)
        {
            // Later: write to file, DB, or diagnostics console
            Console.WriteLine($"[INFO] {message}");
        }

        public void LogError(string message)
        {
            Console.WriteLine($"[ERROR] {message}");
        }
    }
}
