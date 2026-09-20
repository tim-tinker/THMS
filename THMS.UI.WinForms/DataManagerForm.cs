using THMS.Data.Stores;
using THMS.UI.WinForms.Controls;

namespace THMS.UI.WinForms
{
    public partial class DataManagerForm : BaseEmbeddedForm
    {
        private static readonly string[] TabLabels =
        [
            "EV Charge",
            "Home Circuit",
            "Solar",
            "Circuit Attribution",
            "Electric Contracts"
        ];

        private readonly Func<UserControl>[] _tabFactories =
        [
            () => new EvChargeSessionManagerControl(),
            () => new HomeCircuitManagerControl(),
            () => new SolarIntervalManagerControl(),
            () => new HomeCircuitAttributionManagerControl(),
            () => new ElectricContractManagerControl()
        ];

        private string? _appliedTab;
        private string? _appliedPeriod;
        private int _appliedRevision = int.MinValue;

        public DataManagerForm()
        {
            InitializeComponent();
            CreateTabs();
            historyBar.SelectedPeriodChanged += (_, _) => ApplyHistoryToCurrentTab();
            tabs.SelectedIndexChanged += OnTabSelected;
        }

        private void CreateTabs()
        {
            foreach (var label in TabLabels)
            {
                tabs.TabPages.Add(new TabPage(label)
                {
                    Padding = new Padding(4),
                    UseVisualStyleBackColor = false
                });
            }

            tabs.RecalculateItemSize();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible && !Disposing)
                ApplyHistoryToCurrentTab();
        }

        private void OnTabSelected(object? sender, EventArgs e)
        {
            if (IsHandleCreated)
                BeginInvoke(ApplyHistoryToCurrentTab);
            else
                ApplyHistoryToCurrentTab();
        }

        private bool EnsureCurrentTabControl()
        {
            var page = tabs.SelectedTab;
            if (page is null || tabs.SelectedIndex < 0 || tabs.SelectedIndex >= _tabFactories.Length)
                return false;
            if (page.Controls.Count > 0)
                return false;

            var control = _tabFactories[tabs.SelectedIndex]();
            control.Dock = DockStyle.Fill;
            page.Controls.Add(control);
            return true;
        }

        private void ApplyHistoryToCurrentTab()
        {
            var created = EnsureCurrentTabControl();
            var page = tabs.SelectedTab;
            if (page?.Controls.Count is not > 0 || page.Controls[0] is not IDataManagerControl manager)
                return;

            var period = historyBar.SelectedPeriod;
            var revision = FinanceDataRevision.Current;
            if (!created
                && _appliedTab == page.Text
                && _appliedPeriod == period
                && _appliedRevision == revision)
            {
                return;
            }

            manager.SetGridDataSource(period);
            _appliedTab = page.Text;
            _appliedPeriod = period;
            _appliedRevision = revision;
        }
    }
}
