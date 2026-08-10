using System;
using System.Windows.Forms;
using THMS.Data.Stores;
using THMS.Logic.DataCenter;
using THMS.Logic.ViewModels;
using THMS.UI.WinForms.Updates;

namespace THMS.UI.WinForms
{
    public partial class DataCenterForm : Form
    {
        const float _rowHeight = 50f;

        private readonly DataAvailabilityService _availabilityService;
        private readonly Dictionary<string, IDataSourceUpdater> _dataSourceUpdaters = [];
        private DataCenterViewModel _vm;

        public DataCenterForm(
            IEnergyDataStore energyStore,
            IFinanceDataStore financeStore,
            IVehicleDataStore vehicleStore,
            IEnumerable<IDataSourceUpdater> updaters)
        {
            InitializeComponent();

            _availabilityService = new DataAvailabilityService(
                energyStore,
                financeStore,
                vehicleStore);

            ConfigureDataSources(updaters.ToArray());
        }

        private void OnLoadForm(object sender, EventArgs e)
        {
            LoadAvailability();
        }

        /// <summary>
        /// Call from MainForm before adding this form to a host panel.
        /// Kept out of the constructor so the Designer can open derived forms.
        /// </summary>
        public void ConfigureAsEmbeddedForm()
        {
            TopLevel = false;
            FormBorderStyle = FormBorderStyle.None;
            Dock = DockStyle.Fill;
            BackColor = Color.White;
            DoubleBuffered = true;
        }

        private void LoadAvailability()
        {
            foreach (var updater in _dataSourceUpdaters.Values)
            {
                updater.Status.QueryStatus();
                DisplayDataSourceStatus(updater.Status);
            }
        }

        private void ConfigureDataSources(IDataSourceUpdater[] updaters)
        {
            tblDynamicSources.SuspendLayout();

            ConfigureDataSourceTableHeader(updaters.Length);

            for (int i = 0; i < updaters.Length; i++)
            {
                IDataSourceUpdater? updater = updaters[i];
                ConfigureDataSource(updater);
                ConfigureDataSourceStatus(i + 1, updater);
            }

            tblDynamicSources.ResumeLayout();
        }

        private void ConfigureDataSourceTableHeader(int dataSourceCount)
        {
            tblDynamicSources.Controls.Clear();
            tblDynamicSources.RowStyles.Clear();
            tblDynamicSources.RowCount = dataSourceCount + 1;

            for (int i = 0; i < tblDynamicSources.RowCount; i++)
                tblDynamicSources.RowStyles.Add(new RowStyle(SizeType.Absolute, _rowHeight));

            // Header row
            tblDynamicSources.Controls.Add(CreateCellLabel("Data Source", bold: true), 0, 0);
            tblDynamicSources.Controls.Add(CreateCellLabel("Status", bold: true), 1, 0);
            tblDynamicSources.Controls.Add(CreateCellLabel("Last", bold: true), 2, 0);
            tblDynamicSources.Controls.Add(CreateCellLabel("Expected", bold: true), 3, 0);
            tblDynamicSources.Controls.Add(CreateCellLabel("Action", bold: true), 4, 0);
        }

        private void ConfigureDataSource(IDataSourceUpdater updater)
        {
            _dataSourceUpdaters[updater.Name] = updater;
        }

        private void ConfigureDataSourceStatus(int rowIndex, IDataSourceUpdater updater)
        {
            var status = updater.Status;
            if (status == null)
                return;

            var lblName = CreateCellLabel(status.DataSourceName);

            var lblStatus = CreateCellLabel("status");

            var lblLast = CreateCellLabel("Last Retrieved");

            var lblExpected = CreateCellLabel("When Expected");

            var btnAction = new Button
            {
                Text = "Update",
                Dock = DockStyle.Fill,
                Margin = new Padding(4, 4, 4, 4)
            };
            btnAction.Click += (s, e) => OnClickUpdateDataSource(status);

            tblDynamicSources.Controls.Add(lblName, 0, rowIndex);
            tblDynamicSources.Controls.Add(lblStatus, 1, rowIndex);
            tblDynamicSources.Controls.Add(lblLast, 2, rowIndex);
            tblDynamicSources.Controls.Add(lblExpected, 3, rowIndex);
            tblDynamicSources.Controls.Add(btnAction, 4, rowIndex);
        }

        private static Label CreateCellLabel(string text, bool bold = false)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0),
                Font = bold
                    ? new Font("Segoe UI", 10F, FontStyle.Bold)
                    : new Font("Segoe UI", 10F, FontStyle.Regular)
            };
        }

        private void DisplayDataSourceStatus(IDataSourceStatus status)
        {
            if (status == null)
                return;

            var rowIndex = GetRowIndexForDataSource(status.DataSourceName);
            if (rowIndex == -1) return;

            var statusText = status.LastRetrieval == null ? "Missing" : "OK";
            var lblStatus = GetTableLabel(rowIndex, 1);
            if (lblStatus is not null)
            {
                lblStatus.Text = statusText;
                ApplyStatusColor(lblStatus, statusText);
            }

            var lastText = status.LastRetrieval?.ToString("g") ?? "N/A";
            var lblLast = GetTableLabel(rowIndex, 2);
            if (lblLast is not null)
            {
                lblLast.Text = lastText;
            }

            string expectedText = "—";
            if (status is IPeriodicDataSourceStatus periodic)
            {
                expectedText = periodic.NextExpectedRetrieval.ToString("yyyy-MM-dd");
            }
            else if (status is IUpdateDataSourceStatus update)
            {
                expectedText = update.IsReadyForUpdate ? "Ready" : "—";
            }

            var lblExpected = GetTableLabel(rowIndex, 3);
            if (lblExpected is not null)
            {
                lblExpected.Text = expectedText;
            }
        }

        private int GetRowIndexForDataSource(string dataSourceName)
        {
            for (int i = 1; i < tblDynamicSources.RowCount; i++)
            {
                var control = tblDynamicSources.GetControlFromPosition(0, i);
                if (control is Label lbl && lbl.Text == dataSourceName)
                {
                    return i;
                }
            }
            return -1;
        }

        private Label? GetTableLabel(int rowIndex, int columnIndex)
        {
            var control = tblDynamicSources.GetControlFromPosition(columnIndex, rowIndex);
            return control as Label;
        }

        private string FormatDate(DateTime? dt)
        {
            return dt?.ToString("yyyy-MM-dd") ?? "—";
        }

        private string FormatDateTime(DateTime? dt)
        {
            return dt?.ToString("yyyy-MM-dd HH:mm") ?? "—";
        }

        private void ApplyStatusColor(Label label, string status)
        {
            switch (status)
            {
                case "OK":
                    label.ForeColor = Color.DarkGreen;
                    break;

                case "Warning":
                    label.ForeColor = Color.Goldenrod;
                    break;

                case "Missing":
                    label.ForeColor = Color.Red;
                    break;

                case "Missing Month":
                    label.ForeColor = Color.OrangeRed;
                    break;

                default:
                    label.ForeColor = Color.Black;
                    break;
            }
        }

        private void OnClickUpdateDataSource(IDataSourceStatus status)
        {
            if (_dataSourceUpdaters.TryGetValue(status.DataSourceName, out var updater))
            {
                updater.UpdateDataSource();
                LoadAvailability();
            }
            else
            {
                MessageBox.Show($"No manager registered for {status.DataSourceName}");
            }
        }
    }
}
