using THMS.UI.WinForms;

namespace THMS.UI
{
    public partial class MainForm : Form
    {
        private const int NavButtonHeight = 40;

        private readonly Dictionary<string, BaseDashboardForm> _dashboards = [];
        private readonly Dictionary<string, BaseEmbeddedForm> _embeddedForms = [];

        /// <summary>Designer only.</summary>
        public MainForm()
        {
            InitializeComponent();
        }

        public void LoadModules()
        {
            AddSectionLabel("Dashboards");
            AddDashboard("Transportation", new TransportationDashboardForm());
            AddDashboard("Energy", new EnergyDashboardForm());
            AddDashboard("Finance", new FinanceDashboardForm());
            AddDashboard("Vehicles", new VehicleListDashboardForm());

            AddSectionLabel("Data Management");
            AddEmbeddedForm("Data Manager", new DataManagerForm());
            AddEmbeddedForm("Data Center", new DataCenterForm());
            AddEmbeddedForm("Finance Data Center", new FinanceDataCenterForm());
        }

        private void AddSectionLabel(string text)
        {
            var isFirst = navigationPanel.Controls.Count == 0;
            var label = new Label
            {
                Text = text,
                AutoSize = true,
                Font = new Font(Font, FontStyle.Bold),
                Margin = new Padding(0, isFirst ? 0 : 16, 0, 8),
            };
            navigationPanel.Controls.Add(label);
        }

        private void AddDashboard(string label, BaseDashboardForm dashboardForm)
        {
            _dashboards[label] = dashboardForm;
            dashboardForm.ConfigureAsEmbeddedDashboard();
            dashboardForm.InitializeDashboard();

            var button = CreateNavButton(label);
            button.Click += OnClickNavigateToDashboard;
            ConfigureChildForm(dashboardForm);
        }

        private void AddEmbeddedForm(string label, BaseEmbeddedForm embeddedForm)
        {
            _embeddedForms[label] = embeddedForm;
            embeddedForm.ConfigureAsEmbeddedForm();

            var button = CreateNavButton(label);
            button.Click += OnClickNavigateToEmbedded;
            ConfigureChildForm(embeddedForm);
        }

        private Button CreateNavButton(string label)
        {
            var button = new Button
            {
                Text = label,
                Height = NavButtonHeight,
                Width = GetNavButtonWidth(),
                Margin = new Padding(0, 0, 0, 8),
                UseVisualStyleBackColor = true,
            };
            navigationPanel.Controls.Add(button);
            return button;
        }

        private int GetNavButtonWidth()
        {
            return Math.Max(50, navigationPanel.ClientSize.Width - navigationPanel.Padding.Horizontal);
        }

        private void OnNavigationPanelResize(object? sender, EventArgs e)
        {
            var width = GetNavButtonWidth();
            foreach (Control control in navigationPanel.Controls)
            {
                if (control is Button)
                {
                    control.Width = width;
                }
            }
        }

        private void ConfigureChildForm(Form form)
        {
            form.Visible = false;
            dashboardHostPanel.Controls.Add(form);
        }

        private void OnClickNavigateToDashboard(object? sender, EventArgs e)
        {
            if (sender is not Button button) return;

            ShowModule(button.Text);
        }

        private void OnClickNavigateToEmbedded(object? sender, EventArgs e)
        {
            if (sender is not Button button) return;

            ShowFormInMainPanel(button.Text);
        }

        private void ShowModule(string moduleName)
        {
            HideAllEmbeddedForms();

            var dashboard = _dashboards[moduleName];
            dashboard.Visible = true;
            dashboard.RefreshDashboard();
        }

        private void ShowFormInMainPanel(string formName)
        {
            HideAllEmbeddedForms();

            var dashboard = _embeddedForms[formName];
            dashboard.Visible = true;
        }

        private void HideAllEmbeddedForms()
        {
            foreach (var form in _dashboards.Values)
            {
                form.Visible = false;
            }

            foreach (var form in _embeddedForms.Values)
            {
                form.Visible = false;
            }
        }
    }
}
