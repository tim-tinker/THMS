using THMS.Logic.ViewModels.Home;

namespace THMS.UI.WinForms.Views.Home;

public partial class HomeView : UserControl
{
    private readonly HomeViewModel _viewModel;

    public HomeView(HomeViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        BindViewModel();
    }

    private void BindViewModel()
    {
        welcomeLabel.Text = _viewModel.WelcomeMessage;
    }
}
