using TPFS.Domain;
using System.ComponentModel;

namespace TPFS.Logic.ViewModels;

public class TransportationDashboardViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    public BindingList<Vehicle> Vehicles { get; } = new();

    private Vehicle _selectedVehicle;
    public Vehicle SelectedVehicle
    {
        get => _selectedVehicle;
        set
        {
            _selectedVehicle = value;
            PropertyChanged?.Invoke(this, new(nameof(SelectedVehicle)));
        }
    }

    public TransportationDashboardViewModel()
    {
        // Sample data for UI testing
        Vehicles.Add(new Vehicle
        {
            Name = "Ford Escape",
            Type = "ICE",
            AnnualCost = 2134,
            Energy = new EnergyAttribution { HomeCharging = 0, PublicCharging = 0, Regen = 0 },
            MonthlyCosts = new()
            {
                new MonthlyCost { Month = 1, Cost = 180 },
                new MonthlyCost { Month = 2, Cost = 160 },
                new MonthlyCost { Month = 3, Cost = 175 }
            }
        });

        Vehicles.Add(new Vehicle
        {
            Name = "Mach-E",
            Type = "EV",
            AnnualCost = 987,
            Energy = new EnergyAttribution { HomeCharging = 62, PublicCharging = 28, Regen = 10 },
            MonthlyCosts = new()
            {
                new MonthlyCost { Month = 1, Cost = 70 },
                new MonthlyCost { Month = 2, Cost = 82 },
                new MonthlyCost { Month = 3, Cost = 65 }
            }
        });
    }
}
