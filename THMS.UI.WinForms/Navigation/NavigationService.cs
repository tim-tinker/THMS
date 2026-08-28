namespace THMS.UI.WinForms.Navigation;

public class NavigationService
{
    private Panel? _host;
    private Control? _currentView;

    public void Initialize(Panel host)
    {
        _host = host;
    }

    public void Navigate(Control view)
    {
        if (_host is null)
        {
            throw new InvalidOperationException("NavigationService has not been initialized.");
        }

        _host.SuspendLayout();

        try
        {
            if (_currentView is not null)
            {
                _host.Controls.Remove(_currentView);
                _currentView.Dispose();
                _currentView = null;
            }

            view.Dock = DockStyle.Fill;
            _host.Controls.Add(view);
            _currentView = view;
        }
        finally
        {
            _host.ResumeLayout();
        }
    }

    public void Navigate<TView>() where TView : Control
    {
        var view = Activator.CreateInstance<TView>();
        Navigate(view);
    }
}
