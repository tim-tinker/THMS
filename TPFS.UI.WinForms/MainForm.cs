using TPFS.Domain;
using TPFS.Logic.ViewModels;

namespace TPFS.UI.WinForms;

public partial class MainForm : Form
{
    private readonly TransportationDashboardViewModel _vm;
    private TransportationDashboardForm _transportationDashboard;

    public MainForm()
    {
        InitializeComponent();
        InitializeModules();

        _vm = new TransportationDashboardViewModel();
        _transportationDashboard.BindViewModel(_vm);
    }

    private void InitializeModules()
    {
        // Create dashboard form
        _transportationDashboard = new TransportationDashboardForm
        {
            TopLevel = false,
            FormBorderStyle = FormBorderStyle.None,
            Dock = DockStyle.Fill,
            Visible = false        // IMPORTANT: hide until selected
        };

        // Add to the content panel
        contentPanel.Controls.Add(_transportationDashboard);
    }

    private void btnTransportation_Click(object sender, EventArgs e)
    {
        // Show dashboard
        _transportationDashboard.Show();
        _transportationDashboard.BringToFront();
    }
}
