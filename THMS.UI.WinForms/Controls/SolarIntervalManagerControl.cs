using System.ComponentModel;
using THMS.Domain.Energy;
using THMS.Logic.Orchestrators;
using THMS.Logic.ViewModels.Energy;

namespace THMS.UI.WinForms.Controls
{
    public partial class SolarIntervalManagerControl : UserControl, IDataManagerControl
    {
        private readonly SolarIntervalOrchestrator _orchestrator = new();
        private List<SolarMonthSummary> _months = [];
        private List<SolarProductionInterval> _intervals = [];
        private string _currentPeriod = "Month";
        private DateTime? _shownMonth;
        private bool _binding;
        private bool _splitApplied;

        public SolarIntervalManagerControl()
        {
            InitializeComponent();
            SplitContainerUtil.MakeSplitterVisible(splitSolar);
            DataGridViewUtil.EnableDoubleBuffering(gridSolarMonths);
            DataGridViewUtil.EnableDoubleBuffering(gridSolarIntervals);
            gridSolarMonths.AutoGenerateColumns = false;
            gridSolarIntervals.AutoGenerateColumns = false;
            gridSolarMonths.SelectionChanged += (_, _) => ShowSelectedMonth();
            splitSolar.SizeChanged += (_, _) => ApplySplitLayout();
        }

        public Control GetControl() => this;

        public void SetGridDataSource(string period)
        {
            _currentPeriod = period;
            _intervals = _orchestrator.GetSolarIntervals(period).ToList();
            var index = IndexOfMonth(SelectedMonth()?.Month);

            _binding = true;
            try
            {
                _months = SolarMonthSummary.From(_intervals).ToList();
                gridSolarMonths.DataSource = new BindingList<SolarMonthSummary>(_months);
            }
            finally
            {
                _binding = false;
            }

            BindDetail(index < _months.Count ? _months[index] : null, force: true);
            DataGridViewUtil.SelectCellWhenReady(gridSolarMonths, index, 0);
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
            if (SplitContainerUtil.TrySetLeadingRatio(splitSolar, leadingParts: 1, trailingParts: 1))
                _splitApplied = true;
        }

        private SolarMonthSummary? SelectedMonth() =>
            gridSolarMonths.CurrentRow?.DataBoundItem as SolarMonthSummary;

        private int IndexOfMonth(DateTime? month)
        {
            if (month is DateTime value)
            {
                for (var i = 0; i < _months.Count; i++)
                {
                    if (_months[i].Month.Year == value.Year && _months[i].Month.Month == value.Month)
                        return i;
                }
            }

            return 0;
        }

        private void ShowSelectedMonth()
        {
            if (_binding || SelectedMonth() is not SolarMonthSummary month)
                return;

            BindDetail(month);
        }

        private void BindDetail(SolarMonthSummary? month, bool force = false)
        {
            if (!force && SameMonth(_shownMonth, month?.Month))
                return;

            _shownMonth = month?.Month;
            var rows = month is null
                ? []
                : _intervals.Where(interval => interval.Timestamp.Year == month.Month.Year
                    && interval.Timestamp.Month == month.Month.Month).ToList();
            gridSolarIntervals.DataSource = new BindingList<SolarProductionInterval>(rows);
        }

        private static bool SameMonth(DateTime? left, DateTime? right) =>
            left is DateTime a && right is DateTime b
                ? a.Year == b.Year && a.Month == b.Month
                : left is null && right is null;
    }
}
