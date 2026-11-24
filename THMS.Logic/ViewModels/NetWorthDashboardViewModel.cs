using THMS.Logic.Finance.NetWorth;
using THMS.Logic.ViewModels.Finance;

namespace THMS.Logic.ViewModels
{
    public class NetWorthDashboardViewModel : BaseDashboardViewModel
    {
        private readonly NetWorthComposer _composer;

        public NetWorthDashboardViewModel()
            : this(new NetWorthComposer())
        {
        }

        public NetWorthDashboardViewModel(NetWorthComposer composer)
        {
            _composer = composer;
        }

        public NetWorthSnapshot Snapshot { get; private set; } = new();

        public override void Initialize() => Refresh();

        public void Refresh() => Refresh(DateTime.Today);

        public void Refresh(DateTime asOf) => Snapshot = _composer.Compose(asOf);
    }
}
