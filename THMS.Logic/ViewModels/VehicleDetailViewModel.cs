using System.ComponentModel;
using THMS.Data.Stores;
using THMS.Domain.Transportation;

namespace THMS.Logic.ViewModels.Transportation
{
    public class VehicleDetailViewModel
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly IVehicleDataStore _store;
        public IVehicleDataStore Store => _store;

        private DateTime _startTime;
        public DateTime StartTime
        {
            get => _startTime;
            set { _startTime = value; RaiseChanged(nameof(StartTime)); Refresh(); }
        }

        private DateTime _endTime;
        public DateTime EndTime
        {
            get => _endTime;
            set { _endTime = value; RaiseChanged(nameof(EndTime)); Refresh(); }
        }

        public VehicleDetailViewModel(IVehicleDataStore store, Guid vehicleId)
        {
            _store = store;
            VehicleId = vehicleId;
            Refresh();
        }

        public Guid VehicleId { get; }

        public VehicleBase? Vehicle { get; private set; }
        public decimal Mileage { get; private set; } = 0m;
        public IReadOnlyCollection<ChargingCostRecord> ChargingCosts { get; private set; } = Array.Empty<ChargingCostRecord>();
        public IReadOnlyCollection<EvChargingSession> ChargingSessions { get; private set; } = Array.Empty<EvChargingSession>();
        public IReadOnlyCollection<IceMileageRecord> FuelReceipts { get; private set; } = Array.Empty<IceMileageRecord>();
        public IReadOnlyCollection<MaintenanceInvoiceRecord> MaintenanceInvoices { get; private set; } = Array.Empty<MaintenanceInvoiceRecord>();

        public void Refresh()
        {
            Vehicle = _store.GetVehicle(VehicleId);
            Mileage = _store.GetMilesDrivenInPeriod(VehicleId, StartTime, EndTime);
            ChargingCosts = _store.GetChargingCosts(VehicleId, StartTime, EndTime).ToList().AsReadOnly();
            ChargingSessions = _store.GetEvChargingSessions(VehicleId, StartTime, EndTime).ToList().AsReadOnly();
            FuelReceipts = _store.GetIceMileageRecords(VehicleId, StartTime, EndTime).ToList().AsReadOnly();
            MaintenanceInvoices = _store.GetMaintenanceInvoices(VehicleId, StartTime, EndTime).ToList().AsReadOnly();
        }

        private void RaiseChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
