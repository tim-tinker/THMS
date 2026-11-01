using System.ComponentModel;
using THMS.Domain.Energy;
using THMS.Logic.Orchestrators;
using THMS.Logic.ViewModels.Energy;

namespace THMS.UI.WinForms.Controls
{
    public partial class HomeCircuitManagerControl : UserControl, IDataManagerControl
    {
        private readonly HomeCircuitReadingOrchestrator _orchestrator = new();
        private List<HomeCircuitDaySummary> _days = [];
        private List<HomeCircuitReading> _readings = [];
        private string _currentPeriod = "Month";
        private DateTime? _shownDay;
        private bool _binding;
        private bool _splitApplied;

        public HomeCircuitManagerControl()
        {
            InitializeComponent();
            SplitContainerUtil.MakeSplitterVisible(splitHomeCircuit);
            DataGridViewUtil.EnableDoubleBuffering(gridHomeCircuitDays);
            DataGridViewUtil.EnableDoubleBuffering(gridHomeCircuit);
            gridHomeCircuitDays.AutoGenerateColumns = false;
            gridHomeCircuit.AutoGenerateColumns = false;
            gridHomeCircuitDays.SelectionChanged += (_, _) => ShowSelectedDay();
            splitHomeCircuit.SizeChanged += (_, _) => ApplySplitLayout();
        }

        public Control GetControl() => this;

        public void SetGridDataSource(string period)
        {
            _currentPeriod = period;
            _readings = _orchestrator.GetHomeCircuitReadings(period).ToList();
            var index = IndexOfDay(SelectedDay()?.Date);

            _binding = true;
            try
            {
                _days = HomeCircuitDaySummary.From(_readings).ToList();
                gridHomeCircuitDays.DataSource = new BindingList<HomeCircuitDaySummary>(_days);
            }
            finally
            {
                _binding = false;
            }

            BindDetail(index < _days.Count ? _days[index] : null, force: true);
            DataGridViewUtil.SelectCellWhenReady(gridHomeCircuitDays, index, 0);
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
            if (SplitContainerUtil.TrySetLeadingRatio(splitHomeCircuit, leadingParts: 2, trailingParts: 3))
                _splitApplied = true;
        }

        private HomeCircuitDaySummary? SelectedDay() =>
            gridHomeCircuitDays.CurrentRow?.DataBoundItem as HomeCircuitDaySummary;

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
            if (_binding || SelectedDay() is not HomeCircuitDaySummary day)
                return;

            BindDetail(day);
        }

        private void BindDetail(HomeCircuitDaySummary? day, bool force = false)
        {
            var date = day?.Date;
            if (!force && _shownDay == date)
                return;

            _shownDay = date;
            var rows = date is null
                ? []
                : _readings.Where(reading => reading.Timestamp.Date == date.Value).ToList();
            gridHomeCircuit.DataSource = new BindingList<HomeCircuitReading>(rows);
        }
    }
}
