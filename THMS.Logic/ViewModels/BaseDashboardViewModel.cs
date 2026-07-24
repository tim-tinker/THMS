namespace THMS.Logic.ViewModels
{
    public abstract class BaseDashboardViewModel
    {
        /// <summary>
        /// Called when the dashboard is first created.
        /// Use this for initial data loading.
        /// </summary>
        public virtual void Initialize()
        {
        }

        /// <summary>
        /// Called when the dashboard becomes active (user switches to it).
        /// Use this to refresh UI data.
        /// </summary>
        public virtual void Activate()
        {
        }

        /// <summary>
        /// Called when the dashboard is no longer active.
        /// </summary>
        public virtual void Deactivate()
        {
        }

        /// <summary>
        /// Called when the application is shutting down or the module is removed.
        /// </summary>
        public virtual void Dispose()
        {
        }
    }
}
