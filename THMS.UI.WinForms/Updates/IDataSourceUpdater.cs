using THMS.Logic.DataCenter;

namespace THMS.UI.WinForms.Updates
{
    public interface IDataSourceUpdater
    {
        IDataSourceStatus Status { get; }
        string Name => Status.DataSourceName;
        void UpdateDataSource();
    }
}
