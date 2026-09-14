using System.ComponentModel;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Controls
{
    public partial class CategoryManager : Form
    {
        public bool CatalogChanged => manager.CatalogHasChanged;

        public CategoryManager()
        {
            InitializeComponent();
            manager.CloseRequested += (_, _) => Close();
        }

        public CategoryManager(CategoryOrchestrator orchestrator)
            : this()
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;

            manager.Bind(orchestrator);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            AcceptButton = manager.SaveButton;
            CancelButton = manager.CloseButton;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!manager.TryLeave())
            {
                e.Cancel = true;
                return;
            }

            if (DialogResult == DialogResult.None)
                DialogResult = CatalogChanged ? DialogResult.OK : DialogResult.Cancel;
            base.OnFormClosing(e);
        }
    }
}
