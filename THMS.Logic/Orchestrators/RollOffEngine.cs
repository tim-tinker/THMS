using System.Collections.Generic;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Transactions;

namespace THMS.Logic.Orchestrators
{
    public class RollOffEngine
    {
        public bool RollOffRealized(List<Account> accounts, List<BaseTransaction> tx)
        {
            // TODO: remove realized future items
            return true;
        }
    }
}
