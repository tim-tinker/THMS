using THMS.Domain;
using System.ComponentModel;
using THMS.Domain.Transportation;

namespace THMS.Logic.ViewModels;

public class TransportationDashboardViewModel : BaseDashboardViewModel, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    public List<Vehicle> Vehicles { get; } = new();

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
        Vehicles = DemoData.CreateVehicles();
    }
}
