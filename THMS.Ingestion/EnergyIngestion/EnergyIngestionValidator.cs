using THMS.Domain.Energy;

namespace THMS.Ingestion.EnergyIngestion
{
    public class EnergyIngestionValidator
    {
        public bool ValidateSolarRecord(SolarProductionRecord record, out string error)
        {
            error = string.Empty;

            if (record.KilowattHours < 0)
            {
                error = "Solar kWh cannot be negative.";
                return false;
            }

            if (record.Date == default)
            {
                error = "Solar record has an invalid date.";
                return false;
            }

            return true;
        }

        public bool ValidateChargingSession(EnergySource session, out string error)
        {
            error = string.Empty;

            if (session.KilowattHours < 0)
            {
                error = "Charging session kWh cannot be negative.";
                return false;
            }

            if (session.EndTime <= session.StartTime)
            {
                error = "Charging session end time must be after start time.";
                return false;
            }

            return true;
        }
    }
}
