using THMS.Domain.Finance;

namespace THMS.UI.WinForms
{
    public partial class ElectricContractDataEntryForm : Form
    {
        private readonly Guid? _existingId;

        public ElectricContract? Contract { get; private set; }

        public ElectricContractDataEntryForm()
        {
            InitializeComponent();
            _dateStart.Value = DateTime.Today;
            _dateEnd.Value = DateTime.Today;
        }

        public ElectricContractDataEntryForm(ElectricContract contract)
            : this()
        {
            _existingId = contract.Id == Guid.Empty ? null : contract.Id;
            Text = "Edit Electric Contract";
            _textName.Text = contract.Name ?? string.Empty;
            _dateStart.Value = ClampDate(contract.StartDate);
            _dateEnd.Value = ClampDate(contract.EndDate);
            SetDecimal(_numBaseEnergyCharge, contract.BaseEnergyCharge);
            SetDecimal(_numEnergyRate, contract.EnergyChargeRate);
            SetDecimal(_numSolarCreditRate, contract.ExportCreditRate);
            SetDecimal(_numDeliveryBaseCharge, contract.BaseDeliveryCharge);
            SetDecimal(_numDeliveryRate, contract.DeliveryChargeRate);
        }

        private void OnClickCancel(object sender, EventArgs e)
        {
            Contract = null;
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void OnClickSave(object sender, EventArgs e)
        {
            if (_dateEnd.Value.Date < _dateStart.Value.Date)
            {
                MessageBox.Show(
                    this,
                    "Expiration date must be on or after the effective date.",
                    Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            Contract = new ElectricContract
            {
                Id = _existingId ?? Guid.NewGuid(),
                Name = _textName.Text,
                StartDate = _dateStart.Value.Date,
                EndDate = _dateEnd.Value.Date,
                BaseEnergyCharge = _numBaseEnergyCharge.Value,
                EnergyChargeRate = _numEnergyRate.Value,
                BaseDeliveryCharge = _numDeliveryBaseCharge.Value,
                DeliveryChargeRate = _numDeliveryRate.Value,
                ExportCreditRate = _numSolarCreditRate.Value
            };
            DialogResult = DialogResult.OK;
            Close();
        }

        private static void SetDecimal(NumericUpDown control, decimal value)
        {
            if (value < control.Minimum)
                value = control.Minimum;
            if (value > control.Maximum)
                control.Maximum = value;
            control.Value = value;
        }

        private static DateTime ClampDate(DateTime value)
        {
            if (value < DateTimePicker.MinimumDateTime)
                return DateTimePicker.MinimumDateTime;
            if (value > DateTimePicker.MaximumDateTime)
                return DateTimePicker.MaximumDateTime;
            return value;
        }
    }
}
