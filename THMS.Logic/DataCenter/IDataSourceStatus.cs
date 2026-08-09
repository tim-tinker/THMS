using System;
using System.Collections.Generic;
using System.Text;

namespace THMS.Logic.DataCenter
{
    public interface IDataSourceStatus
    {
        string DataSourceName { get; }
        DateTime? LastRetrieval { get; }

        void QueryStatus();
    }
}
