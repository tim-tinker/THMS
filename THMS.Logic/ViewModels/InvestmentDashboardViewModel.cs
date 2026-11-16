using THMS.Data.Stores;
using THMS.Logic.Finance.Investments;
using THMS.Logic.ViewModels.Finance;

namespace THMS.Logic.ViewModels
{
    public class InvestmentDashboardViewModel : BaseDashboardViewModel
    {
        private readonly InvestmentDashboardComposer _composer;

        public InvestmentDashboardViewModel()
            : this(new InvestmentDashboardComposer())
        {
        }

        public InvestmentDashboardViewModel(IAccountDataStore accounts, IInvestmentBookStore books)
            : this(new InvestmentDashboardComposer(accounts, books))
        {
        }

        public InvestmentDashboardViewModel(InvestmentDashboardComposer composer)
        {
            _composer = composer;
        }

        public InvestmentDashboardSnapshot Snapshot { get; private set; } = new();

        public override void Initialize() => Refresh();

        public void Refresh() => Refresh(DateTime.Today);

        public void Refresh(DateTime asOf) => Snapshot = _composer.Compose(asOf);
    }
}
