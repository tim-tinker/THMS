using System.Collections.Generic;
using THMS.Domain;

namespace THMS.Logic.ViewModels
{
    public class HouseholdDashboardViewModel : BaseDashboardViewModel
    {
        public List<HouseholdExpense> Expenses { get; }
        public HouseholdExpense? SelectedExpense { get; set; }

        public HouseholdDashboardViewModel()
        {
            Expenses = DemoData.CreateHouseholdExpenses();
        }
    }
}
