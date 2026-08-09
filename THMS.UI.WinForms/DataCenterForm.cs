using System;
using System.Windows.Forms;
using THMS.Data.Stores;
using THMS.Logic.DataCenter;
using THMS.Logic.ViewModels;

namespace THMS.UI.WinForms
{
    public partial class DataCenterForm : Form
    {
        private readonly DataAvailabilityService _availabilityService;
        private DataCenterViewModel _vm;

        public DataCenterForm(
            IEnergyDataStore energyStore,
            IFinanceDataStore financeStore,
            IVehicleDataStore vehicleStore)
        {
            InitializeComponent();

            _availabilityService = new DataAvailabilityService(
                energyStore,
                financeStore,
                vehicleStore);

            LoadAvailability();
            RenderDataSourceStatuses();
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
            _vm = _availabilityService.GetAvailability();
        }

        private void RenderDataSourceStatuses()
        {
            const float rowHeight = 50f;

            tblDynamicSources.SuspendLayout();
            tblDynamicSources.Controls.Clear();
            tblDynamicSources.RowStyles.Clear();
            tblDynamicSources.RowCount = _vm._dataSourceStatuses.Count + 1;

            for (int i = 0; i < tblDynamicSources.RowCount; i++)
                tblDynamicSources.RowStyles.Add(new RowStyle(SizeType.Absolute, rowHeight));

            // Header row
            tblDynamicSources.Controls.Add(CreateCellLabel("Data Source", bold: true), 0, 0);
            tblDynamicSources.Controls.Add(CreateCellLabel("Status", bold: true), 1, 0);
            tblDynamicSources.Controls.Add(CreateCellLabel("Last", bold: true), 2, 0);
            tblDynamicSources.Controls.Add(CreateCellLabel("Expected", bold: true), 3, 0);
            tblDynamicSources.Controls.Add(CreateCellLabel("Action", bold: true), 4, 0);

            int row = 1;

            foreach (var status in _vm._dataSourceStatuses)
            {
                var lblName = CreateCellLabel(status.DataSourceName);

                var statusText = status.LastRetrieval == null ? "Missing" : "OK";
                var lblStatus = CreateCellLabel(statusText);
                ApplyStatusColor(lblStatus, statusText);

                var lblLast = CreateCellLabel(status.LastRetrieval?.ToString("yyyy-MM-dd") ?? "—");

                string expectedText = "—";
                if (status is IPeriodicDataSourceStatus periodic)
                {
                    expectedText = periodic.NextExpectedRetrieval.ToString("yyyy-MM-dd");
                }
                else if (status is IUpdateDataSourceStatus update)
                { 
                    expectedText = update.IsReadyForUpdate ? "Ready" : "—";
                }

                var lblExpected = CreateCellLabel(expectedText);

                var btnAction = new Button
                {
                    Text = "Manage",
                    Dock = DockStyle.Fill,
                    Margin = new Padding(4, 4, 4, 4)
                };
                btnAction.Click += (s, e) => OnClickManageDataSource(status);

                tblDynamicSources.Controls.Add(lblName, 0, row);
                tblDynamicSources.Controls.Add(lblStatus, 1, row);
                tblDynamicSources.Controls.Add(lblLast, 2, row);
                tblDynamicSources.Controls.Add(lblExpected, 3, row);
                tblDynamicSources.Controls.Add(btnAction, 4, row);

                row++;
            }

            tblDynamicSources.ResumeLayout();
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

        private string GetStatusText(bool hasData, bool warning, bool missing)
        {
            if (!hasData) return "Missing";
            if (missing) return "Missing Month";
            if (warning) return "Warning";
            return "OK";
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

        private void OnClickManageDataSource(IDataSourceStatus status)
        {
            MessageBox.Show($"Manage action for {status.DataSourceName} not implemented yet.");
        }

    }
}
