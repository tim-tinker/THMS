using THMS.UI;
using THMS.UI.WinForms.Controls;

namespace THMS.UI.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.AddMessageFilter(new GridMouseWheelFilter());
        var mainForm = new MainForm();
        mainForm.LoadModules();
        Application.Run(mainForm);
    }
}
