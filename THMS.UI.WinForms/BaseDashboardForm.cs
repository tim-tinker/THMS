using System;
using System.Windows.Forms;
using THMS.Logic.ViewModels;

namespace THMS.UI.WinForms
{
    public abstract class BaseDashboardForm : Form
    {
        public abstract void RefreshDashboard();
    }

    public abstract class BaseDashboardForm<TViewModel> : BaseDashboardForm
        where TViewModel : BaseDashboardViewModel, new()
    {
        protected TViewModel ViewModel { get; private set; } = null!;

        protected BaseDashboardForm()
        {
            this.TopLevel = false;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.White;
            this.DoubleBuffered = true;

            this.Load += DashboardForm_Load;
        }

        private void DashboardForm_Load(object? sender, EventArgs e)
        {
            ViewModel = new TViewModel();
            BindControlsToViewModel();
            ViewModel.Initialize();
            RefreshDashboard();
        }

        protected abstract void BindControlsToViewModel();
    }
}
