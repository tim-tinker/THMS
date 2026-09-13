using THMS.Data.Stores;
using THMS.Domain.Transportation;
using THMS.Ingestion.Importers.Transportation;
using THMS.Logic.ViewModels.Transportation;

namespace THMS.Logic.Orchestrators
{
    public class EvChargeSessionImportOrchestrator
    {
        private readonly IVehicleDataStore _vehicles;
        private readonly SpreadsheetEvChargeSessionImporter _importer;
        private readonly EvChargeSessionOrchestrator _sessions;

        public EvChargeSessionImportOrchestrator()
            : this(new DataStoreFactory().GetVehicleStore())
        {
        }

        public EvChargeSessionImportOrchestrator(IVehicleDataStore vehicles)
            : this(vehicles, new SpreadsheetEvChargeSessionImporter(), new EvChargeSessionOrchestrator(vehicles,
                new DataStoreFactory().GetEnergyStore(),
                new DataStoreFactory().GetFinanceStore()))
        {
        }

        public EvChargeSessionImportOrchestrator(
            IVehicleDataStore vehicles,
            SpreadsheetEvChargeSessionImporter importer,
            EvChargeSessionOrchestrator sessions)
        {
            _vehicles = vehicles;
            _importer = importer;
            _sessions = sessions;
        }

        public IReadOnlyList<VehicleEv> GetEvVehicles() =>
            _vehicles.GetAllVehicles().OfType<VehicleEv>().OrderBy(v => v.Name).ToList();

        public List<EvChargeSessionImportPreview> LoadSessionsFromFiles(
            IEnumerable<string> paths,
            VehicleEv vehicle)
        {
            ArgumentNullException.ThrowIfNull(paths);
            ArgumentNullException.ThrowIfNull(vehicle);

            var rows = new List<EvChargeSessionImportPreview>();
            foreach (var path in paths)
            {
                if (string.IsNullOrWhiteSpace(path))
                    throw new ArgumentException("A file path is required.");
                if (!File.Exists(path))
                    throw new FileNotFoundException("The selected file was not found.", path);

                foreach (var parsed in _importer.Parse(path))
                {
                    rows.Add(ToPreview(parsed, vehicle));
                }
            }

            ApplyPreviousSessionContext(rows, vehicle.Id);
            return rows.OrderByDescending(row => row.StartTime).ToList();
        }

        public int ImportSessions(IEnumerable<EvChargeSessionImportPreview> previewRows)
        {
            ArgumentNullException.ThrowIfNull(previewRows);
            var rows = previewRows.Where(row => row.VehicleId != Guid.Empty).ToList();
            foreach (var group in rows.GroupBy(row => row.VehicleId))
                ApplyPreviousSessionContext(group.ToList(), group.Key);

            var imported = 0;
            var existingByVehicle = new Dictionary<Guid, List<BaseEvChargeSession>>();

            foreach (var row in rows.OrderBy(r => r.StartTime))
            {
                if (IsDuplicate(row, existingByVehicle))
                    continue;

                var session = ToSession(row);
                _sessions.Save(session);
                existingByVehicle[row.VehicleId].Add(session);
                imported++;
            }

            return imported;
        }

        private void ApplyPreviousSessionContext(List<EvChargeSessionImportPreview> rows, Guid vehicleId)
        {
            var known = _vehicles.GetBaseEvChargeSessions(
                    vehicleId,
                    DateTime.MinValue,
                    DateTime.MaxValue)
                .ToList();

            foreach (var row in rows.OrderBy(r => r.StartTime))
            {
                var previous = known
                    .Where(session => session.EndTime <= row.StartTime)
                    .OrderByDescending(session => session.EndTime)
                    .FirstOrDefault();

                if (previous is not null)
                {
                    row.LastOdometer = previous.OdometerMiles;
                    row.LastSoc = previous.EndSoc;
                }
                else
                {
                    row.LastOdometer = row.OdometerMiles;
                    row.LastSoc = row.StartSoc;
                }

                known.Add(ToSession(row));
            }
        }

        private bool IsDuplicate(
            EvChargeSessionImportPreview row,
            Dictionary<Guid, List<BaseEvChargeSession>> existingByVehicle)
        {
            if (!existingByVehicle.TryGetValue(row.VehicleId, out var existing))
            {
                existing = _vehicles.GetBaseEvChargeSessions(
                    row.VehicleId,
                    DateTime.MinValue,
                    DateTime.MaxValue).ToList();
                existingByVehicle[row.VehicleId] = existing;
            }

            return existing.Any(session =>
                session.StartTime == row.StartTime
                && session.OdometerMiles == row.OdometerMiles);
        }

        private static EvChargeSessionImportPreview ToPreview(
            ParsedSpreadsheetEvChargeSession parsed,
            VehicleEv vehicle) =>
            new()
            {
                StartTime = parsed.StartTime,
                EndTime = parsed.EndTime,
                OdometerMiles = parsed.OdometerMiles,
                StartSoc = parsed.StartSoc,
                EndSoc = parsed.EndSoc,
                KwhAdded = parsed.KwhAdded,
                KwhDrawn = parsed.KwhDrawn,
                SessionCost = parsed.CostAdded,
                IsHomeCharge = parsed.IsHomeCharge,
                Charger = parsed.Charger,
                VehicleName = vehicle.Name,
                VehicleId = vehicle.Id
            };

        private static BaseEvChargeSession ToSession(EvChargeSessionImportPreview row)
        {
            if (row.IsHomeCharge)
            {
                return new HomeEvChargeSession
                {
                    Id = Guid.NewGuid(),
                    VehicleId = row.VehicleId,
                    VehicleName = row.VehicleName,
                    LastOdometer = row.LastOdometer,
                    LastSoc = row.LastSoc,
                    OdometerMiles = row.OdometerMiles,
                    StartTime = row.StartTime,
                    EndTime = row.EndTime,
                    StartSoc = row.StartSoc,
                    EndSoc = row.EndSoc,
                    KwhAdded = row.KwhAdded,
                    KwhDrawn = row.KwhDrawn,
                    SessionCost = row.SessionCost
                };
            }

            return new CommercialEvChargeSession
            {
                Id = Guid.NewGuid(),
                VehicleId = row.VehicleId,
                VehicleName = row.VehicleName,
                LastOdometer = row.LastOdometer,
                LastSoc = row.LastSoc,
                OdometerMiles = row.OdometerMiles,
                StartTime = row.StartTime,
                EndTime = row.EndTime,
                StartSoc = row.StartSoc,
                EndSoc = row.EndSoc,
                KwhAdded = row.KwhAdded,
                KwhDrawn = row.KwhDrawn,
                SessionCost = row.SessionCost
            };
        }
    }
}
