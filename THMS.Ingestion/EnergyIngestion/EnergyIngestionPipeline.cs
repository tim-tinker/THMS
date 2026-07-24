using THMS.Domain.Energy;
using THMS.Data.Stores;
using THMS.Energy.Attribution;

namespace THMS.Ingestion.EnergyIngestion
{
    public interface IEnergyIngestionPipeline
    {
        EnergyIngestionResult IngestSolar(IEnumerable<SolarProductionRecord> solarRecords);
        EnergyIngestionResult IngestChargingSessions(IEnumerable<EnergySource> chargingRecords);
    }

    public class EnergyIngestionPipeline : IEnergyIngestionPipeline
    {
        private readonly EnergyDataStore _energyStore;
        private readonly EnergyIngestionValidator _validator;
        private readonly EnergyIngestionLogger _logger;
        private readonly EnergyAttributionEngine _attributionEngine;

        public EnergyIngestionPipeline(
            EnergyDataStore energyStore,
            EnergyIngestionValidator validator,
            EnergyIngestionLogger logger,
            EnergyAttributionEngine attributionEngine)
        {
            _energyStore = energyStore;
            _validator = validator;
            _logger = logger;
            _attributionEngine = attributionEngine;
        }

        public EnergyIngestionResult IngestSolar(IEnumerable<SolarProductionRecord> solarRecords)
        {
            var result = new EnergyIngestionResult();

            foreach (var record in solarRecords)
            {
                if (!_validator.ValidateSolarRecord(record, out var error))
                {
                    result.Errors.Add(error);
                    _logger.LogError(error);
                    continue;
                }

                _energyStore.AddSolarRecord(record);
                _logger.LogInfo($"Solar record ingested: {record.Date} = {record.KilowattHours} kWh");
            }

            // Trigger attribution after solar ingestion
            var attribution = _attributionEngine.ComputeAttribution();
            result.Attribution = attribution;

            return result;
        }

        public EnergyIngestionResult IngestChargingSessions(IEnumerable<EnergySource> chargingRecords)
        {
            var result = new EnergyIngestionResult();

            foreach (var session in chargingRecords)
            {
                if (!_validator.ValidateChargingSession(session, out var error))
                {
                    result.Errors.Add(error);
                    _logger.LogError(error);
                    continue;
                }

                _energyStore.AddChargingSession(session);
                _logger.LogInfo($"Charging session ingested: {session.StartTime} → {session.EndTime}, {session.KilowattHours} kWh");
            }

            // Trigger attribution after charging ingestion
            var attribution = _attributionEngine.ComputeAttribution();
            result.Attribution = attribution;

            return result;
        }
    }
}
