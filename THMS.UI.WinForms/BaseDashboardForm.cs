using THMS.Logic.ViewModels;

namespace THMS.UI.WinForms
{
    public abstract class BaseDashboardForm : Form
    {
        protected BaseDashboardViewModel? ViewModel { get; private set; }

        protected BaseDashboardForm()
        {
            // All dashboards are embedded inside MainForm
            this.TopLevel = false;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Dock = DockStyle.Fill;

            // Modern dashboard background
            this.BackColor = System.Drawing.Color.White;

            // Prevent flicker on charts and panels
            this.DoubleBuffered = true;
        }

        /// <summary>
        /// Called by MainForm to inject the dashboard's ViewModel.
        /// Each dashboard must implement this.
        /// </summary>
        public virtual void BindViewModel(BaseDashboardViewModel viewModel)
        {
            this.ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            OnBindViewModel(ViewModel);
            ViewModel.Initialize();
        }

        protected abstract void OnBindViewModel(BaseDashboardViewModel viewModel);

        /// <summary>
        /// Optional: dashboards can override this to refresh their UI
        /// when MainForm switches to them.
        /// </summary>
        public virtual void OnActivated()
        {
            ViewModel?.Activate();
        }
    }
}
