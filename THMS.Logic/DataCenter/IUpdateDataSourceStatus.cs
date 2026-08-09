using System;
using System.Collections.Generic;
using System.Text;

namespace THMS.Logic.DataCenter
{
    public interface IUpdateDataSourceStatus : IDataSourceStatus
    {
        bool IsReadyForUpdate { get; }
    }
}
