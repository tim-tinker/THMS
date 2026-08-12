using THMS.Data.Stores;
using THMS.UI.WinForms.Controls;

namespace THMS.UI.WinForms
{
    public partial class DataManagerForm : BaseEmbeddedForm
    {
        private readonly IEnergyDataStore _energyStore;
        private IDataManagerControl? _currentControl;

        public DataManagerForm(IEnergyDataStore energyStore)
        {
            _energyStore = energyStore;
            InitializeComponent();
            LoadStoreList();
        }

        // ---------------------------------------------------------
        // Load available data stores into the combo box
        // ---------------------------------------------------------
        private void LoadStoreList()
        {
            //comboStores.Items.Add("EV Sessions");
            comboStores.Items.Add("Solar Intervals");
            //comboStores.Items.Add("Billing Records");
            //comboStores.Items.Add("Financial Transactions");

            if (comboStores.Items.Count > 0)
                comboStores.SelectedIndex = 0;
        }

        // ---------------------------------------------------------
        // Handle store selection change
        // ---------------------------------------------------------
        private void comboStores_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selected = comboStores.SelectedItem?.ToString();
            if (!string.IsNullOrWhiteSpace(selected))
                LoadManagerControl(selected);
        }

        // ---------------------------------------------------------
        // Load the appropriate manager control into the host panel
        // ---------------------------------------------------------
        private void LoadManagerControl(string storeName)
        {
            panelHost.Controls.Clear();
            _currentControl = null;

            IDataManagerControl control = storeName switch
            {
                //"EV Sessions" => new EVSessionManagerControl(_services),
                "Solar Intervals" => new SolarIntervalManagerControl(_energyStore),
                //"Billing Records" => new BillingRecordManagerControl(_services),
                //"Financial Transactions" => new FinanceManagerControl(_services),
                //    _ => throw new InvalidOperationException($"Unknown store type: {storeName}")
            };

            _currentControl = control;

            var ui = control.GetControl();
            ui.Dock = DockStyle.Fill;

            panelHost.Controls.Add(ui);

            control.LoadData();
        }

        private void OnClickClose(object sender, EventArgs e)
        {
            Close();
        }
    }
}
