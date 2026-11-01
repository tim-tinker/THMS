using System;
using System.ComponentModel;
using THMS.Domain.Finance;
using THMS.Logic.Transportation;

namespace THMS.Logic.ViewModels
{
    public class VehicleListItemViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        [Browsable(false)]
        public Guid VehicleId { get; }

        public string Name { get; }

        [DisplayName("Is EV")]
        public bool IsEv { get; }

        private decimal _mpg;

        [DisplayName("MPG")]
        public decimal Mpg
        {
            get => _mpg;
            set { _mpg = value; OnChanged(nameof(Mpg)); }
        }

        private decimal _costPerMile;

        [DisplayName("Cost/Mile")]
        public decimal CostPerMile
        {
            get => _costPerMile;
            set { _costPerMile = value; OnChanged(nameof(CostPerMile)); }
        }

        private decimal _totalCost;

        [DisplayName("Total Cost")]
        public decimal TotalCost
        {
            get => _totalCost;
            set { _totalCost = value; OnChanged(nameof(TotalCost)); }
        }

        private decimal _totalMiles;

        [DisplayName("Total Miles")]
        public decimal TotalMiles
        {
            get => _totalMiles;
            set { _totalMiles = value; OnChanged(nameof(TotalMiles)); }
        }

        private decimal _gallons;

        [DisplayName("Gallons")]
        public decimal Gallons
        {
            get => _gallons;
            set { _gallons = value; OnChanged(nameof(Gallons)); }
        }

        [Browsable(false)]
        public IReadOnlyList<EvChargeSessionContribution> ChargeSessions { get; private set; } = [];

        [Browsable(false)]
        public IReadOnlyList<IceFillUpContribution> FillUps { get; private set; } = [];

        [Browsable(false)]
        public IReadOnlyList<VehicleMonthContribution> Months { get; private set; } = [];

        public VehicleListItemViewModel(
            Guid vehicleId,
            string name,
            bool isEv)
        {
            VehicleId = vehicleId;
            Name = name;
            IsEv = isEv;
        }

        public void SetContributions(
            IReadOnlyList<EvChargeSessionContribution> chargeSessions,
            IReadOnlyList<IceFillUpContribution> fillUps,
            IReadOnlyList<VehicleMonthContribution> months)
        {
            ChargeSessions = chargeSessions;
            FillUps = fillUps;
            Months = months;
        }

        public void ApplySummary(object summary)
        {
            if (summary is EvTransportationCostSummary ev)
            {
                CostPerMile = ev.CostPerMile;
                TotalMiles = ev.TotalMiles;
                TotalCost = ev.TotalCost;
                Gallons = ev.Gallons;
                Mpg = ev.Mpg;
            }
            else if (summary is IceTransportationCostSummary ice)
            {
                CostPerMile = ice.CostPerMile;
                TotalMiles = ice.TotalMiles;
                TotalCost = ice.TotalCost;
                Gallons = ice.Gallons;
                Mpg = ice.Mpg;
            }
        }

        private void OnChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
