namespace THMS.UI.WinForms.Controls
{
    public class HistoryPeriodBar : FlowLayoutPanel
    {
        public const string Month = "Month";
        public const string Year = "Year";
        public const string Lifetime = "Lifetime";

        private readonly ComboBox _combo;
        private bool _suspend;

        public event EventHandler? SelectedPeriodChanged;

        public HistoryPeriodBar()
        {
            WrapContents = false;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(0, 4, 0, 4);

            var label = new Label
            {
                Text = "History:",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 8, 8, 4)
            };

            _combo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 120,
                DropDownWidth = 120,
                IntegralHeight = false,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 4, 8, 4)
            };
            _combo.Items.AddRange([Month, Year, Lifetime]);
            _combo.SelectedItem = Month;
            _combo.SelectedIndexChanged += OnComboSelectedIndexChanged;

            Controls.Add(label);
            Controls.Add(_combo);
        }

        public string SelectedPeriod =>
            _combo.SelectedItem?.ToString()
            ?? (_combo.Items.Count > 0 ? _combo.Items[0]?.ToString() : null)
            ?? Month;

        public void SetAllowedPeriods(IReadOnlyList<string> periods, string fallback)
        {
            if (periods.Count == 0)
                throw new ArgumentException("At least one history period is required.", nameof(periods));

            var current = SelectedPeriod;
            var next = periods.Contains(current) ? current : fallback;

            _suspend = true;
            _combo.Items.Clear();
            foreach (var period in periods)
                _combo.Items.Add(period);
            _combo.SelectedItem = next;
            _suspend = false;
        }

        public void SelectPeriod(string period)
        {
            var allowed = _combo.Items.Cast<object>().Select(o => o.ToString()).ToList();
            var item = allowed.Contains(period)
                ? period
                : allowed.Find(p => p == Year) ?? allowed.FirstOrDefault() ?? Month;
            if (Equals(_combo.SelectedItem, item))
                return;

            _suspend = true;
            _combo.SelectedItem = item;
            _suspend = false;
        }

        private void OnComboSelectedIndexChanged(object? sender, EventArgs e)
        {
            if (!_suspend)
                SelectedPeriodChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
