using THMS.Data.Stores;
using THMS.Domain.Transportation;

namespace THMS.UI.ViewModels.Transportation
{
    public class MileageEntryViewModel
    {
        private readonly TransportationDataStore _store;

        public MileageEntryViewModel(TransportationDataStore store, Guid vehicleId)
        {
            _store = store;
            VehicleId = vehicleId;
        }

        public Guid VehicleId { get; }

        public DateTime Date { get; set; } = DateTime.Now;
        public decimal OdometerMiles { get; set; }

        public void Save()
        {
            var record = new MileageRecordBase
            {
                VehicleId = VehicleId,
                Date = Date,
                OdometerMiles = OdometerMiles
            };

            _store.AddMileageRecord(record);
        }
    }
}
