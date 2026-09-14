using System.ComponentModel;
using THMS.Data.Stores;
using THMS.Domain.Transportation;
using THMS.Logic.Orchestrators;

namespace THMS.Logic.ViewModels.Transportation
{
    public class VehicleDetailViewModel
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly IVehicleDataStore _vehicleStore;
        private readonly EvChargeSessionOrchestrator? _sessionOrchestrator;

        private string _historyPeriod = "Month";
        public string HistoryPeriod
        {
            get => _historyPeriod;
            set
            {
                var period = string.IsNullOrWhiteSpace(value) ? "Month" : value;
                if (_historyPeriod == period)
                    return;

                _historyPeriod = period;
                ApplyHistoryRange();
                RaiseChanged(nameof(HistoryPeriod));
                Refresh();
            }
        }

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

        public VehicleDetailViewModel(Guid vehicleId)
            : this(
                vehicleId,
                new DataStoreFactory().GetVehicleStore(),
                new EvChargeSessionOrchestrator())
        {
        }

        public VehicleDetailViewModel(Guid vehicleId, IVehicleDataStore vehicleStore)
            : this(vehicleId, vehicleStore, orchestrator: null)
        {
        }

        public VehicleDetailViewModel(
            Guid vehicleId,
            IVehicleDataStore vehicleStore,
            EvChargeSessionOrchestrator? orchestrator)
        {
            _vehicleStore = vehicleStore;
            _sessionOrchestrator = orchestrator;
            VehicleId = vehicleId;

            ApplyHistoryRange();
            Refresh();
        }

        public Guid VehicleId { get; }

        public VehicleBase? Vehicle { get; private set; }
        public decimal Mileage { get; private set; } = 0m;
        public BindingList<BaseEvChargeSession> ChargeSessions { get; } = new();
        public BindingList<VehicleChargeCostRow> ChargeCostRows { get; } = new();
        public IReadOnlyCollection<IceMileageRecord> FuelReceipts { get; private set; } = Array.Empty<IceMileageRecord>();
        public IReadOnlyCollection<MaintenanceInvoiceRecord> MaintenanceInvoices { get; private set; } = Array.Empty<MaintenanceInvoiceRecord>();

        public void Refresh()
        {
            Vehicle = _vehicleStore.GetVehicle(VehicleId);
            Mileage = _vehicleStore.GetMilesDrivenInPeriod(VehicleId, StartTime, EndTime);

            ChargeSessions.Clear();
            var sessions = _sessionOrchestrator is not null
                ? _sessionOrchestrator.GetCompletedSessions(VehicleId, StartTime, EndTime)
                : _vehicleStore.GetBaseEvChargeSessions(VehicleId, StartTime, EndTime);
            foreach (var session in sessions)
                ChargeSessions.Add(session);
            RebuildChargeCostRows();

            FuelReceipts = _vehicleStore.GetIceMileageRecords(VehicleId, StartTime, EndTime).ToList().AsReadOnly();
            MaintenanceInvoices = _vehicleStore.GetMaintenanceInvoices(VehicleId, StartTime, EndTime).ToList().AsReadOnly();
        }

        public BaseEvChargeSession? GetLatestChargeSession()
        {
            return _vehicleStore
                .GetBaseEvChargeSessions(VehicleId, DateTime.MinValue, DateTime.MaxValue)
                .OrderBy(s => s.StartTime)
                .LastOrDefault();
        }

        /// <summary>
        /// Inserts or replaces a session in the bound list when it falls inside the
        /// current date filter. Outside the filter, an existing row is removed.
        /// </summary>
        public void UpsertChargeSession(BaseEvChargeSession session)
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

            RebuildChargeCostRows();
        }

        private void ApplyHistoryRange()
        {
            var (start, end) = BaseOrchestrator.GetHistoryRange(_historyPeriod);
            _startTime = start;
            _endTime = end;
        }

        private void RebuildChargeCostRows()
        {
            ChargeCostRows.Clear();
            foreach (var session in ChargeSessions)
                ChargeCostRows.Add(VehicleChargeCostRow.FromSession(session));
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
