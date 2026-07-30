namespace THMS.UI.WinForms;

/// <summary>
/// Shared base for embedded dashboard forms.
/// Lives in THMS.UI.Common (not the WinExe) so the WinForms Designer can
/// instantiate it when designing derived forms.
/// </summary>
public class BaseDashboardForm : Form
{
    public BaseDashboardForm()
    {
    }

    /// <summary>
    /// Call from MainForm before adding this form to a host panel.
    /// Kept out of the constructor so the Designer can open derived forms.
    /// </summary>
    public void ConfigureAsEmbeddedDashboard()
    {
        TopLevel = false;
        FormBorderStyle = FormBorderStyle.None;
        Dock = DockStyle.Fill;
        BackColor = Color.White;
        DoubleBuffered = true;
    }

    /// <summary>
    /// Create/bind the ViewModel. Override in each dashboard.
    /// </summary>
    public virtual void InitializeDashboard()
    {
    }

    public virtual void RefreshDashboard()
    {
    }
}
