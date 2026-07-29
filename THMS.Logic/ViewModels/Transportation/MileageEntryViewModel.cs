using THMS.Data.Stores;
using THMS.Domain.Transportation;

namespace THMS.Logic.ViewModels.Transportation
{
    public class MileageEntryViewModel
    {
        private readonly IVehicleDataStore _store;

        public MileageEntryViewModel(IVehicleDataStore store, Guid vehicleId)
        {
            _store = store;
            VehicleId = vehicleId;
        }

        public Guid VehicleId { get; }

        public DateTime Date { get; set; } = DateTime.Now;
        public decimal OdometerMiles { get; set; }

        public void Save()
        {
            var record = new IceMileageRecord
            {
                VehicleId = VehicleId,
                Date = Date,
                OdometerMiles = OdometerMiles
            };

            _store.AddIceMileageRecord(record);
        }
    }
}
