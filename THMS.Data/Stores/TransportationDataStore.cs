using THMS.Domain.Transportation;

namespace THMS.Data.Stores
{
    /// <summary>
    /// Stores all transportation-related domain objects:
    /// - Vehicles
    /// - Mileage records
    /// - Charging cost records
    /// - Gas receipts (ICE fuel)
    /// - Maintenance invoices
    ///
    /// This is the single source of truth for transportation data.
    /// </summary>
    public class TransportationDataStore
    {
        // -----------------------------
        // VEHICLES
        // -----------------------------

        private readonly Dictionary<Guid, Vehicle> _vehicles =
            new Dictionary<Guid, Vehicle>();

        public void AddVehicle(Vehicle vehicle)
        {
            _vehicles[vehicle.Id] = vehicle;

            // Ensure collections exist for this vehicle
            if (!_mileage.ContainsKey(vehicle.Id))
                _mileage[vehicle.Id] = new List<MileageRecord>();

            if (!_chargingCosts.ContainsKey(vehicle.Id))
                _chargingCosts[vehicle.Id] = new List<ChargingCostRecord>();

            if (!_gasReceipts.ContainsKey(vehicle.Id))
                _gasReceipts[vehicle.Id] = new List<GasReceiptRecord>();

            if (!_maintenanceInvoices.ContainsKey(vehicle.Id))
                _maintenanceInvoices[vehicle.Id] = new List<MaintenanceInvoiceRecord>();
        }

        public IReadOnlyCollection<Vehicle> GetAllVehicles() =>
            _vehicles.Values.ToList().AsReadOnly();

        public Vehicle? GetVehicle(Guid id)
        {
            _vehicles.TryGetValue(id, out var vehicle);
            return vehicle;
        }

        // -----------------------------
        // MILEAGE RECORDS
        // -----------------------------

        private readonly Dictionary<Guid, List<MileageRecord>> _mileage =
            new Dictionary<Guid, List<MileageRecord>>();

        public void AddMileageRecord(MileageRecord record)
        {
            if (!_mileage.ContainsKey(record.VehicleId))
                _mileage[record.VehicleId] = new List<MileageRecord>();

            _mileage[record.VehicleId].Add(record);
        }

        public IReadOnlyCollection<MileageRecord> GetMileage(Guid vehicleId)
        {
            if (_mileage.TryGetValue(vehicleId, out var list))
                return list.OrderBy(r => r.Date).ToList().AsReadOnly();

            return Array.Empty<MileageRecord>();
        }

        public decimal? GetMilesDrivenInPeriod(Guid vehicleId, DateTime start, DateTime end)
        {
            var records = GetMileage(vehicleId)
                .Where(r => r.Date >= start && r.Date <= end)
                .OrderBy(r => r.Date)
                .ToList();

            if (records.Count < 2)
                return null;

            return records.Last().OdometerMiles - records.First().OdometerMiles;
        }

        // -----------------------------
        // CHARGING COST RECORDS (EV)
        // -----------------------------

        private readonly Dictionary<Guid, List<ChargingCostRecord>> _chargingCosts =
            new Dictionary<Guid, List<ChargingCostRecord>>();

        public void AddChargingCostRecord(ChargingCostRecord record)
        {
            if (!_chargingCosts.ContainsKey(record.VehicleId))
                _chargingCosts[record.VehicleId] = new List<ChargingCostRecord>();

            _chargingCosts[record.VehicleId].Add(record);
        }

        public IReadOnlyCollection<ChargingCostRecord> GetChargingCosts(Guid vehicleId)
        {
            if (_chargingCosts.TryGetValue(vehicleId, out var list))
                return list.OrderBy(r => r.Timestamp).ToList().AsReadOnly();

            return Array.Empty<ChargingCostRecord>();
        }

        public decimal GetChargingCostInPeriod(Guid vehicleId, DateTime start, DateTime end)
        {
            return GetChargingCosts(vehicleId)
                .Where(r => r.Timestamp >= start && r.Timestamp <= end)
                .Sum(r => r.Cost);
        }

        // -----------------------------
        // GAS RECEIPTS (ICE fuel)
        // -----------------------------

        private readonly Dictionary<Guid, List<GasReceiptRecord>> _gasReceipts =
            new Dictionary<Guid, List<GasReceiptRecord>>();

        public void AddGasReceipt(GasReceiptRecord record)
        {
            if (!_gasReceipts.ContainsKey(record.VehicleId))
                _gasReceipts[record.VehicleId] = new List<GasReceiptRecord>();

            _gasReceipts[record.VehicleId].Add(record);
        }

        public IReadOnlyCollection<GasReceiptRecord> GetFuelReceipts(Guid vehicleId)
        {
            if (_gasReceipts.TryGetValue(vehicleId, out var list))
                return list.OrderBy(r => r.Date).ToList().AsReadOnly();

            return Array.Empty<GasReceiptRecord>();
        }

        public decimal GetFuelCostInPeriod(Guid vehicleId, DateTime start, DateTime end)
        {
            return GetFuelReceipts(vehicleId)
                .Where(r => r.Date >= start && r.Date <= end)
                .Sum(r => r.Cost);
        }

        public decimal GetTotalFuelCost(Guid vehicleId)
        {
            return GetFuelReceipts(vehicleId).Sum(r => r.Cost);
        }

        // -----------------------------
        // MAINTENANCE INVOICES
        // -----------------------------

        private readonly Dictionary<Guid, List<MaintenanceInvoiceRecord>> _maintenanceInvoices =
            new Dictionary<Guid, List<MaintenanceInvoiceRecord>>();

        public void AddMaintenanceInvoice(MaintenanceInvoiceRecord record)
        {
            if (!_maintenanceInvoices.ContainsKey(record.VehicleId))
                _maintenanceInvoices[record.VehicleId] = new List<MaintenanceInvoiceRecord>();

            _maintenanceInvoices[record.VehicleId].Add(record);
        }

        public IReadOnlyCollection<MaintenanceInvoiceRecord> GetMaintenanceInvoices(Guid vehicleId)
        {
            if (_maintenanceInvoices.TryGetValue(vehicleId, out var list))
                return list.OrderBy(r => r.Date).ToList().AsReadOnly();

            return Array.Empty<MaintenanceInvoiceRecord>();
        }

        public decimal GetMaintenanceCostInPeriod(Guid vehicleId, DateTime start, DateTime end)
        {
            return GetMaintenanceInvoices(vehicleId)
                .Where(r => r.Date >= start && r.Date <= end)
                .Sum(r => r.Cost);
        }

        public decimal GetTotalMaintenanceCost(Guid vehicleId)
        {
            return GetMaintenanceInvoices(vehicleId).Sum(r => r.Cost);
        }
    }
}
