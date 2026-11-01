using System.ComponentModel;
using THMS.Domain.Energy;
using THMS.Logic.Orchestrators;
using THMS.Logic.ViewModels.Energy;

namespace THMS.UI.WinForms.Controls
{
    public partial class HomeCircuitAttributionManagerControl : UserControl, IDataManagerControl
    {
        private readonly HomeCircuitAttributionOrchestrator _orchestrator = new();
        private List<HomeCircuitAttributionDaySummary> _days = [];
        private List<HomeCircuitAttribution> _attributions = [];
        private string _currentPeriod = "Month";
        private DateTime? _shownDay;
        private bool _binding;
        private bool _splitApplied;

        public HomeCircuitAttributionManagerControl()
        {
            InitializeComponent();
            SplitContainerUtil.MakeSplitterVisible(splitAttribution);
            DataGridViewUtil.EnableDoubleBuffering(gridAttributionDays);
            DataGridViewUtil.EnableDoubleBuffering(gridHomeCircuit);
            gridAttributionDays.AutoGenerateColumns = false;
            gridHomeCircuit.AutoGenerateColumns = false;
            gridAttributionDays.SelectionChanged += (_, _) => ShowSelectedDay();
            splitAttribution.SizeChanged += (_, _) => ApplySplitLayout();
        }

        public Control GetControl() => this;

        public void SetGridDataSource(string period)
        {
            _currentPeriod = period;
            _attributions = _orchestrator.GetHomeCircuitAttributions(period).ToList();
            var index = IndexOfDay(SelectedDay()?.Date);

            _binding = true;
            try
            {
                _days = HomeCircuitAttributionDaySummary.From(_attributions).ToList();
                gridAttributionDays.DataSource = new BindingList<HomeCircuitAttributionDaySummary>(_days);
            }
            finally
            {
                _binding = false;
            }

            BindDetail(index < _days.Count ? _days[index] : null, force: true);
            DataGridViewUtil.SelectCellWhenReady(gridAttributionDays, index, 0);
        }

        private void OnLoad(object sender, EventArgs e)
        {
            ApplySplitLayout();
            SetGridDataSource(_currentPeriod);
        }

        private void ApplySplitLayout()
        {
            if (_splitApplied)
                return;
            if (SplitContainerUtil.TrySetLeadingRatio(splitAttribution, leadingParts: 1, trailingParts: 1))
                _splitApplied = true;
        }

        private HomeCircuitAttributionDaySummary? SelectedDay() =>
            gridAttributionDays.CurrentRow?.DataBoundItem as HomeCircuitAttributionDaySummary;

        private int IndexOfDay(DateTime? date)
        {
            if (date is DateTime day)
            {
                for (var i = 0; i < _days.Count; i++)
                {
                    if (_days[i].Date == day.Date)
                        return i;
                }
            }

            return 0;
        }

        private void ShowSelectedDay()
        {
            if (_binding || SelectedDay() is not HomeCircuitAttributionDaySummary day)
                return;

            BindDetail(day);
        }

        private void BindDetail(HomeCircuitAttributionDaySummary? day, bool force = false)
        {
            var date = day?.Date;
            if (!force && _shownDay == date)
                return;

            _shownDay = date;
            var rows = date is null
                ? []
                : _attributions.Where(attribution => attribution.Timestamp.Date == date.Value).ToList();
            gridHomeCircuit.DataSource = new BindingList<HomeCircuitAttribution>(rows);
        }
    }
}
