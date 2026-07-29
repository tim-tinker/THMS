using System;
using System.ComponentModel;
using THMS.Domain.Transportation;
using THMS.Domain.Energy;
using THMS.Data.Stores;

namespace THMS.Logic.ViewModels
{
    public class ChargingSessionViewModel : INotifyPropertyChanged
    {
        private readonly IVehicleDataStore _vehicleStore;
        private readonly IFinanceDataStore _financeStore;
        private readonly IEnergyDataStore _energyStore;

        public event PropertyChangedEventHandler? PropertyChanged;

        public Guid Id { get; private set; }
        public Guid VehicleId { get; private set; }

        // ---------------------------------------------------------
        // BASIC SESSION FIELDS
        // ---------------------------------------------------------

        private DateTime _startTime;
        public DateTime StartTime
        {
            get => _startTime;
            set { _startTime = value; OnChanged(nameof(StartTime)); }
        }

        private DateTime _endTime;
        public DateTime EndTime
        {
            get => _endTime;
            set { _endTime = value; OnChanged(nameof(EndTime)); }
        }

        private decimal _kwhAdded;
        public decimal KwhAdded
        {
            get => _kwhAdded;
            set { _kwhAdded = value; OnChanged(nameof(KwhAdded)); }
        }

        private bool _isHomeCharging;
        public bool IsHomeCharging
        {
            get => _isHomeCharging;
            set { _isHomeCharging = value; OnChanged(nameof(IsHomeCharging)); }
        }

        private decimal? _chargingCost;
        public decimal? ChargingCost
        {
            get => _chargingCost;
            set { _chargingCost = value; OnChanged(nameof(ChargingCost)); }
        }

        // ---------------------------------------------------------
        // VEHICLE DATA (SOC + ODOMETER)
        // ---------------------------------------------------------

        public Guid? VehicleDataId { get; private set; }

        private int _startSoc;
        public int StartSoc
        {
            get => _startSoc;
            set { _startSoc = value; OnChanged(nameof(StartSoc)); }
        }

        private int _endSoc;
        public int EndSoc
        {
            get => _endSoc;
            set { _endSoc = value; OnChanged(nameof(EndSoc)); }
        }

        private decimal _odometerMiles;
        public decimal OdometerMiles
        {
            get => _odometerMiles;
            set { _odometerMiles = value; OnChanged(nameof(OdometerMiles)); }
        }

        // ---------------------------------------------------------
        // COMMERCIAL SESSION FIELDS
        // ---------------------------------------------------------

        private string? _vendorSessionId;
        public string? VendorSessionId
        {
            get => _vendorSessionId;
            set { _vendorSessionId = value; OnChanged(nameof(VendorSessionId)); }
        }

        private string? _location;
        public string? Location
        {
            get => _location;
            set { _location = value; OnChanged(nameof(Location)); }
        }

        // ---------------------------------------------------------
        // CONSTRUCTORS
        // ---------------------------------------------------------

        public ChargingSessionViewModel(
            IVehicleDataStore vehicleStore,
            IFinanceDataStore financeStore,
            IEnergyDataStore energyStore,
            EvChargingSession session)
        {
            _vehicleStore = vehicleStore;
            _financeStore = financeStore;
            _energyStore = energyStore;

            Id = session.Id;
            VehicleDataId = session.VehicleDataId;

            StartTime = session.StartTime;
            EndTime = session.EndTime;
            KwhAdded = session.KwhAdded;
            IsHomeCharging = session.IsHomeCharging;
            ChargingCost = session.ChargingCost;

            // Load vehicle data if present
            if (session.VehicleDataId != null)
            {
                var vd = _vehicleStore.GetEvChargingSessionVehicleDataById(session.VehicleDataId.Value);
                if (vd != null)
                {
                    VehicleId = vd.VehicleId;
                    StartSoc = vd.StartSocPercent ?? 0;
                    EndSoc = vd.EndSocPercent ?? 0;
                    OdometerMiles = vd.OdometerMiles ?? 0;
                }
            }
        }

        // ---------------------------------------------------------
        // SAVE / UPDATE
        // ---------------------------------------------------------

        public void Save()
        {
            var session = new EvChargingSession
            {
                Id = Id,
                StartTime = StartTime,
                EndTime = EndTime,
                KwhAdded = KwhAdded,
                IsHomeCharging = IsHomeCharging,
                ChargingCost = ChargingCost,
                VehicleDataId = VehicleDataId
            };

            _vehicleStore.AddEvChargingSession(session);

            if (!IsHomeCharging && ChargingCost.HasValue)
            {
                _financeStore.UpdateEvChargingSessionCost(Id, ChargingCost.Value);
            }
        }

        private void OnChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
