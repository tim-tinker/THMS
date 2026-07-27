using System;
using System.ComponentModel;
using THMS.Data.Stores;
using THMS.Domain.Finance;
using THMS.Domain.Transportation;
using THMS.Logic.Transportation;
using THMS.Logic.ViewModels;

namespace THMS.Logic.ViewModels
{
    public class VehicleDetailViewModel : INotifyPropertyChanged
    {
        private readonly IVehicleDataStore _vehicleStore;
        private readonly IFinanceDataStore _financeStore;
        private readonly IEnergyDataStore _energyStore;
        private readonly TransportationCostAggregator _aggregator;

        public event PropertyChangedEventHandler? PropertyChanged;

        // ---------------------------------------------------------
        // VEHICLE METADATA
        // ---------------------------------------------------------

        public Guid VehicleId { get; }
        public string VehicleName { get; }

        private bool _isEv;
        public bool IsEv
        {
            get => _isEv;
            private set { _isEv = value; OnChanged(nameof(IsEv)); }
        }

        // ---------------------------------------------------------
        // CHILD VIEWMODELS
        // ---------------------------------------------------------

        public ChargingSessionListViewModel ChargingSessions { get; }
        public GasPurchaseListViewModel GasPurchases { get; }

        // ---------------------------------------------------------
        // COST SUMMARY (EV or ICE)
        // ---------------------------------------------------------

        private EvTransportationCostSummary? _evSummary;
        public EvTransportationCostSummary? EvSummary
        {
            get => _evSummary;
            private set { _evSummary = value; OnChanged(nameof(EvSummary)); }
        }

        private IceTransportationCostSummary? _iceSummary;
        public IceTransportationCostSummary? IceSummary
        {
            get => _iceSummary;
            private set { _iceSummary = value; OnChanged(nameof(IceSummary)); }
        }

        // ---------------------------------------------------------
        // PERIOD SELECTION
        // ---------------------------------------------------------

        private DateTime _periodStart;
        public DateTime PeriodStart
        {
            get => _periodStart;
            set { _periodStart = value; OnChanged(nameof(PeriodStart)); RefreshSummary(); }
        }

        private DateTime _periodEnd;
        public DateTime PeriodEnd
        {
            get => _periodEnd;
            set { _periodEnd = value; OnChanged(nameof(PeriodEnd)); RefreshSummary(); }
        }

        // ---------------------------------------------------------
        // CONSTRUCTOR
        // ---------------------------------------------------------

        public VehicleDetailViewModel(
            IVehicleDataStore vehicleStore,
            IFinanceDataStore financeStore,
            IEnergyDataStore energyStore,
            TransportationCostAggregator aggregator,
            Guid vehicleId)
        {
            _vehicleStore = vehicleStore;
            _financeStore = financeStore;
            _energyStore = energyStore;
            _aggregator = aggregator;

            VehicleId = vehicleId;

            var vehicle = _vehicleStore.GetVehicle(vehicleId)
                ?? throw new InvalidOperationException("Vehicle not found.");

            VehicleName = vehicle.Name;
            IsEv = vehicle is VehicleEv;

            // Default period: last 30 days
            PeriodStart = DateTime.Today.AddDays(-30);
            PeriodEnd = DateTime.Today;

            // Child ViewModels
            ChargingSessions = new ChargingSessionListViewModel(
                _vehicleStore,
                _financeStore,
                _energyStore,
                vehicleId);

            GasPurchases = new GasPurchaseListViewModel(
                _financeStore,
                vehicleId);

            RefreshSummary();
        }

        // ---------------------------------------------------------
        // SUMMARY REFRESH
        // ---------------------------------------------------------

        public void RefreshSummary()
        {
            var summary = _aggregator.GetCostSummary(
                VehicleId,
                PeriodStart,
                PeriodEnd);

            if (summary is EvTransportationCostSummary ev)
            {
                EvSummary = ev;
                IceSummary = null;
            }
            else if (summary is IceTransportationCostSummary ice)
            {
                IceSummary = ice;
                EvSummary = null;
            }
        }

        // ---------------------------------------------------------
        // PROPERTY CHANGED
        // ---------------------------------------------------------

        private void OnChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
