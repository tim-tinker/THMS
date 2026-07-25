using THMS.Data.Stores;
using THMS.Domain.Transportation;

namespace THMS.Logic.ViewModels.Transportation
{
    public class VehicleDetailViewModel
    {
        private readonly TransportationDataStore _store;
        public TransportationDataStore Store => _store;

        public VehicleDetailViewModel(TransportationDataStore store, Guid vehicleId)
        {
            _store = store;
            VehicleId = vehicleId;
            Refresh();
        }

        public Guid VehicleId { get; }

        public Vehicle? Vehicle { get; private set; }
        public IReadOnlyCollection<MileageRecordBase> Mileage { get; private set; } = Array.Empty<MileageRecordBase>();
        public IReadOnlyCollection<ChargingCostRecord> ChargingCosts { get; private set; } = Array.Empty<ChargingCostRecord>();
        public IReadOnlyCollection<GasReceiptRecord> FuelReceipts { get; private set; } = Array.Empty<GasReceiptRecord>();
        public IReadOnlyCollection<MaintenanceInvoiceRecord> MaintenanceInvoices { get; private set; } = Array.Empty<MaintenanceInvoiceRecord>();

        public void Refresh()
        {
            Vehicle = _store.GetVehicle(VehicleId);
            Mileage = _store.GetMileage(VehicleId);
            ChargingCosts = _store.GetChargingCosts(VehicleId);
            FuelReceipts = _store.GetFuelReceipts(VehicleId);
            MaintenanceInvoices = _store.GetMaintenanceInvoices(VehicleId);
        }
    }
}
