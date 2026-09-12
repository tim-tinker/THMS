using THMS.UI.WinForms;

namespace THMS.UI
{
    public partial class MainForm : Form
    {
        private const int NavButtonHeight = 40;
        private static readonly Color NavSelectedBack = Color.FromArgb(0, 99, 177);
        private static readonly Color NavSelectedFore = Color.White;
        private static readonly Color NavIdleBack = Color.FromArgb(245, 245, 245);
        private static readonly Color NavIdleFore = Color.FromArgb(32, 32, 32);

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
            AddDashboard("Finance", new FinanceDashboardForm());
            AddDashboard("Transportation", new TransportationDashboardForm());
            AddDashboard("Vehicles", new VehicleListDashboardForm());
            AddDashboard("Energy", new EnergyDashboardForm());

            AddSectionLabel("Data Management");
            AddEmbeddedForm("Planning Center", new PlanningCenterForm());
            AddEmbeddedForm("Register", new RegisterForm());
            AddEmbeddedForm("Data Manager", new DataManagerForm());
            AddEmbeddedForm("Data Center", new DataCenterForm());
        }

        private void OnLoad(object sender, EventArgs e)
        {
            ShowDashboard("Finance");
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
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                BackColor = NavIdleBack,
                ForeColor = NavIdleFore,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 8, 0)
            };
            button.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            button.FlatAppearance.BorderSize = 1;
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

            ShowDashboard(button.Text);
        }

        private void OnClickNavigateToEmbedded(object? sender, EventArgs e)
        {
            if (sender is not Button button) return;

            ShowFormInMainPanel(button.Text);
        }

        private void ShowDashboard(string moduleName)
        {
            HideAllEmbeddedForms();

            var dashboard = _dashboards[moduleName];
            dashboard.Visible = true;
            dashboard.RefreshDashboard();
            HighlightNavButton(moduleName);
        }

        private void ShowFormInMainPanel(string formName)
        {
            HideAllEmbeddedForms();

            var dashboard = _embeddedForms[formName];
            dashboard.Visible = true;
            HighlightNavButton(formName);
        }

        private void HighlightNavButton(string label)
        {
            foreach (Control control in navigationPanel.Controls)
            {
                if (control is not Button button)
                    continue;

                var selected = string.Equals(button.Text, label, StringComparison.Ordinal);
                button.BackColor = selected ? NavSelectedBack : NavIdleBack;
                button.ForeColor = selected ? NavSelectedFore : NavIdleFore;
                button.FlatAppearance.BorderColor = selected
                    ? NavSelectedBack
                    : Color.FromArgb(200, 200, 200);
            }
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
