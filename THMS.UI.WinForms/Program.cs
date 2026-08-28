using THMS.UI;

namespace THMS.UI.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        var mainForm = new MainForm();
        mainForm.LoadModules();
        Application.Run(mainForm);
    }
}
