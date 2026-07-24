using System.Collections.Generic;
using THMS.Domain;

namespace THMS.Logic.ViewModels
{
    public class FinanceDashboardViewModel : BaseDashboardViewModel
    {
        public List<FinanceAccount> Accounts { get; }
        public FinanceAccount? SelectedAccount { get; set; }

        public FinanceDashboardViewModel()
        {
            Accounts = DemoData.CreateFinanceAccounts();
        }
    }
}
