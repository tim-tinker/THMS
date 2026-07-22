using System.Collections.Generic;
using THMS.Domain;

namespace THMS.Logic.ViewModels
{
    public class EnergyDashboardViewModel : BaseDashboardViewModel
    {
        public EnergyDashboardViewModel()
        {
            EnergyData = new List<EnergyBreakdown>();
            EnergySources = DemoData.CreateEnergySources();
        }
        public List<EnergyBreakdown> EnergyData { get; set; }
        public List<EnergySource> EnergySources { get; }
        public EnergySource? SelectedSource { get; set; }

    }
}
