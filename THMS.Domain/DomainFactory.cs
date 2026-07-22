using System;
using System.Collections.Generic;

namespace THMS.Domain
{
    public static class DomainFactory
    {
        // Vehicles
        public static Vehicle CreateVehicle(
            string name,
            EnergyBreakdown energy,
            IEnumerable<MonthlyValue> monthlyCosts)
        {
            return new Vehicle
            {
                Name = name,
                Energy = energy,
                MonthlyCosts = new List<MonthlyValue>(monthlyCosts)
            };
        }

        // Energy Sources
        public static EnergySource CreateEnergySource(
            string name,
            decimal monthlyKwh,
            decimal costPerKwh,
            IEnumerable<MonthlyValue> monthlyCosts)
        {
            return new EnergySource
            {
                Name = name,
                MonthlyKwh = monthlyKwh,
                CostPerKwh = costPerKwh,
                MonthlyCosts = new List<MonthlyValue>(monthlyCosts)
            };
        }

        // Finance Accounts
        public static FinanceAccount CreateFinanceAccount(
            string name,
            decimal balance,
            decimal monthlyIncome,
            decimal monthlyExpenses,
            IEnumerable<MonthlyValue> monthlyNet)
        {
            return new FinanceAccount
            {
                Name = name,
                Balance = balance,
                MonthlyIncome = monthlyIncome,
                MonthlyExpenses = monthlyExpenses,
                MonthlyNet = new List<MonthlyValue>(monthlyNet)
            };
        }

        // Household Expenses
        public static HouseholdExpense CreateHouseholdExpense(
            string name,
            decimal monthlyCost,
            string category,
            string sharedWith,
            IEnumerable<MonthlyValue> monthlyBreakdown)
        {
            return new HouseholdExpense
            {
                Name = name,
                MonthlyCost = monthlyCost,
                Category = category,
                SharedWith = sharedWith,
                MonthlyBreakdownInternal = new List<MonthlyValue>(monthlyBreakdown)
            };
        }
    }
}
