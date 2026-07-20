namespace TPFS.UI.WinForms.Views;

/// <summary>
/// Temporary view used for sections that do not have a dedicated page yet.
/// </summary>
public class PlaceholderView : UserControl
{
    public PlaceholderView(string sectionName)
    {
        Dock = DockStyle.Fill;
        BackColor = SystemColors.Window;

        var title = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold),
            Location = new Point(24, 24),
            Text = sectionName
        };

        var message = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 11F),
            Location = new Point(24, 72),
            Text = $"{sectionName} content will go here."
        };

        Controls.Add(message);
        Controls.Add(title);
    }
}
