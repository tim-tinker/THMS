using THMS.Logic.ViewModels.Transportation;

namespace THMS.UI.WinForms
{
    public partial class EvCircuitDataForm : Form
    {
        private readonly EvChargeSessionViewModel _sessionVM = null!;
        private bool _gridBound;

        /// <summary>
        /// Default ctor for designer support. Do not use this constructor in production code.
        /// </summary>
        public EvCircuitDataForm()
        {
            InitializeComponent();
        }

        public EvCircuitDataForm(EvChargeSessionViewModel sessionVM)
            : this()
        {
            _sessionVM = sessionVM ?? throw new ArgumentNullException(nameof(sessionVM));
        }

        private void OnLoadForm(object sender, EventArgs e)
        {
            BindGrid();
            RefreshTotals();
            UpdateLoadButtonText();
        }

        private void BindGrid()
        {
            if (_gridBound)
                return;

            _gridCircuitData.AutoGenerateColumns = false;
            _gridCircuitData.DataSource = _sessionVM.CircuitSegments;
            _gridBound = true;
        }

        private void RefreshTotals()
        {
            _textTotalKwh.Text = $"{_sessionVM.KwhAdded:0.000}";
            _textGridKwh.Text = $"{_sessionVM.GridKwh:0.000}";
            _textSolarKwh.Text = $"{_sessionVM.SolarKwh:0.000}";
            _textBatteryKwh.Text = $"{_sessionVM.BatteryKwh:0.000}";
        }

        private void UpdateLoadButtonText()
        {
            _btnLoad.Text = _sessionVM.CircuitSegments.Count == 0
                ? "Load Data"
                : "Reload Data";
        }

        private void OnClickLoad(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog();
            dialog.Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*";

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            _sessionVM.LoadCircuitData(dialog.FileName);

            // CircuitSegments is a BindingList; the grid updates without rebinding.
            RefreshTotals();
            UpdateLoadButtonText();
        }

        private void OnClickClose(object sender, EventArgs e)
        {
            Close();
        }
    }
}
