using System;
using System.ComponentModel;
using THMS.Domain.Transportation;
using THMS.Data.Stores;

namespace THMS.Logic.ViewModels
{
    public class GasPurchaseViewModel : INotifyPropertyChanged
    {
        private readonly IFinanceDataStore _financeStore;

        public event PropertyChangedEventHandler? PropertyChanged;

        public Guid Id { get; private set; }
        public Guid VehicleId { get; private set; }

        private DateTime _date;
        public DateTime Date
        {
            get => _date;
            set { _date = value; OnChanged(nameof(Date)); }
        }

        private decimal _gallons;
        public decimal Gallons
        {
            get => _gallons;
            set { _gallons = value; OnChanged(nameof(Gallons)); }
        }

        private decimal _fuelCost;
        public decimal FuelCost
        {
            get => _fuelCost;
            set { _fuelCost = value; OnChanged(nameof(FuelCost)); }
        }

        private string? _station;
        public string? Station
        {
            get => _station;
            set { _station = value; OnChanged(nameof(Station)); }
        }

        public GasPurchaseViewModel(IFinanceDataStore financeStore, GasPurchase purchase)
        {
            _financeStore = financeStore;

            Id = purchase.Id;
            VehicleId = purchase.VehicleId;
            Date = purchase.Date;
            Gallons = purchase.Gallons;
            FuelCost = purchase.FuelCost;
            Station = purchase.Station;
        }

        public GasPurchaseViewModel(IFinanceDataStore financeStore, Guid vehicleId)
        {
            _financeStore = financeStore;

            Id = Guid.NewGuid();
            VehicleId = vehicleId;
            Date = DateTime.Today;
            Gallons = 0;
            FuelCost = 0;
            Station = string.Empty;
        }

        public void Save()
        {
            var purchase = new GasPurchase
            {
                Id = Id,
                VehicleId = VehicleId,
                Date = Date,
                Gallons = Gallons,
                FuelCost = FuelCost,
                Station = Station
            };

            _financeStore.UpsertGasPurchase(purchase);
        }

        private void OnChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
