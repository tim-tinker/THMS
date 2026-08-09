using System;
using System.Collections.Generic;
using System.Text;

namespace THMS.Logic.DataCenter
{
    public interface IPeriodicDataSourceStatus : IDataSourceStatus
    {
        DateTime NextExpectedRetrieval { get; }
    }
}
