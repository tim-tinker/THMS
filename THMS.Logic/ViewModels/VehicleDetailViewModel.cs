using System.ComponentModel;
using THMS.Data.Stores;
using THMS.Domain.Transportation;

namespace THMS.Logic.ViewModels.Transportation
{
    public class VehicleDetailViewModel
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public IVehicleDataStore VehicleStore { get; }
        public IEnergyDataStore EnergyStore { get; }

        private DateTime _startTime;
        public DateTime StartTime
        {
            get => _startTime;
            set
            {
                if (_startTime == value)
                    return;

                _startTime = value;
                RaiseChanged(nameof(StartTime));
                Refresh();
            }
        }

        private DateTime _endTime;
        public DateTime EndTime
        {
            get => _endTime;
            set
            {
                if (_endTime == value)
                    return;

                _endTime = value;
                RaiseChanged(nameof(EndTime));
                Refresh();
            }
        }

        public VehicleDetailViewModel(IVehicleDataStore store, IEnergyDataStore energyStore, Guid vehicleId)
        {
            VehicleStore = store;
            EnergyStore = energyStore;
            VehicleId = vehicleId;

            // Assign the backing fields so the range is complete before the first
            // Refresh; the setters would each trigger a redundant load.
            _startTime = VehicleStore.GetEarliestIceMileageRecord(VehicleId)?.EndTime
                ?? DateTime.MinValue;
            _endTime = DateTime.MaxValue;

            Refresh();
        }

        public Guid VehicleId { get; }

        public VehicleBase? Vehicle { get; private set; }
        public decimal Mileage { get; private set; } = 0m;
        public BindingList<EvChargeSession> ChargeSessions { get; } = new();
        public IReadOnlyCollection<IceMileageRecord> FuelReceipts { get; private set; } = Array.Empty<IceMileageRecord>();
        public IReadOnlyCollection<MaintenanceInvoiceRecord> MaintenanceInvoices { get; private set; } = Array.Empty<MaintenanceInvoiceRecord>();

        public void Refresh()
        {
            Vehicle = VehicleStore.GetVehicle(VehicleId);
            Mileage = VehicleStore.GetMilesDrivenInPeriod(VehicleId, StartTime, EndTime);

            ChargeSessions.Clear();
            foreach (var session in VehicleStore.GetEvChargeSessions(VehicleId, StartTime, EndTime))
                ChargeSessions.Add(session);

            FuelReceipts = VehicleStore.GetIceMileageRecords(VehicleId, StartTime, EndTime).ToList().AsReadOnly();
            MaintenanceInvoices = VehicleStore.GetMaintenanceInvoices(VehicleId, StartTime, EndTime).ToList().AsReadOnly();
        }

        public EvChargeSession? GetLatestChargeSession()
        {
            return VehicleStore
                .GetEvChargeSessions(VehicleId, DateTime.MinValue, DateTime.MaxValue)
                .OrderBy(s => s.StartTime)
                .LastOrDefault();
        }

        /// <summary>
        /// Inserts or replaces a session in the bound list when it falls inside the
        /// current date filter. Outside the filter, an existing row is removed.
        /// </summary>
        public void UpsertChargeSession(EvChargeSession session)
        {
            var index = IndexOfSession(session.Id);
            var inRange = session.StartTime >= StartTime && session.StartTime <= EndTime;

            if (index >= 0)
            {
                if (inRange)
                    ChargeSessions[index] = session;
                else
                    ChargeSessions.RemoveAt(index);
            }
            else if (inRange)
            {
                ChargeSessions.Add(session);
            }
        }

        private int IndexOfSession(Guid sessionId)
        {
            for (var i = 0; i < ChargeSessions.Count; i++)
            {
                if (ChargeSessions[i].Id == sessionId)
                    return i;
            }

            return -1;
        }

        private void RaiseChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
