using System;
using System.ComponentModel;
using THMS.Domain.Finance;
using THMS.Logic.Transportation;

namespace THMS.Logic.ViewModels
{
    public class VehicleListItemViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public Guid VehicleId { get; }
        public string Name { get; }
        public bool IsEv { get; }

        private decimal _costPerMile;
        public decimal CostPerMile
        {
            get => _costPerMile;
            set { _costPerMile = value; OnChanged(nameof(CostPerMile)); }
        }

        private decimal _totalMiles;
        public decimal TotalMiles
        {
            get => _totalMiles;
            set { _totalMiles = value; OnChanged(nameof(TotalMiles)); }
        }

        private decimal _totalCost;
        public decimal TotalCost
        {
            get => _totalCost;
            set { _totalCost = value; OnChanged(nameof(TotalCost)); }
        }

        public VehicleListItemViewModel(
            Guid vehicleId,
            string name,
            bool isEv)
        {
            VehicleId = vehicleId;
            Name = name;
            IsEv = isEv;
        }

        public void ApplySummary(object summary)
        {
            if (summary is EvTransportationCostSummary ev)
            {
                CostPerMile = ev.CostPerMile;
                TotalMiles = ev.TotalMiles;
                TotalCost = ev.TotalCost;
            }
            else if (summary is IceTransportationCostSummary ice)
            {
                CostPerMile = ice.CostPerMile;
                TotalMiles = ice.TotalMiles;
                TotalCost = ice.TotalCost;
            }
        }

        private void OnChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
