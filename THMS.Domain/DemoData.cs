using System.Collections.Generic;

namespace THMS.Domain
{
    public static class DemoData
    {
        public static List<Vehicle> CreateVehicles()
        {
            return new List<Vehicle>
            {
                DomainFactory.CreateVehicle(
                    "Ford Escape",
                    new EnergyBreakdown(60, 30, 10),
                    MonthlyValues(12, 150, 200, 175, 160, 180, 190, 210, 220, 200, 195, 205)
                ),

                DomainFactory.CreateVehicle(
                    "Tesla Model Y",
                    new EnergyBreakdown(70, 20, 10),
                    MonthlyValues(12, 40, 45, 42, 38, 41, 43, 47, 50, 48, 46, 44)
                )
            };
        }

        public static List<EnergySource> CreateEnergySources()
        {
            return new List<EnergySource>
            {
                DomainFactory.CreateEnergySource(
                    "Home Charging",
                    320,
                    0.13m,
                    MonthlyValues(12, 40, 42, 41, 39, 38, 40, 43, 45, 44, 42, 41)
                ),

                DomainFactory.CreateEnergySource(
                    "Public Charging",
                    120,
                    0.25m,
                    MonthlyValues(12, 30, 32, 31, 29, 28, 30, 33, 35, 34, 32, 31)
                )
            };
        }

        public static List<FinanceAccount> CreateFinanceAccounts()
        {
            return new List<FinanceAccount>
            {
                DomainFactory.CreateFinanceAccount(
                    "Checking",
                    4200,
                    5000,
                    3800,
                    MonthlyValues(12, 1200, 1100, 1300, 1250, 1400, 1500, 1600, 1550, 1450, 1350, 1250)
                ),

                DomainFactory.CreateFinanceAccount(
                    "Savings",
                    18000,
                    0,
                    0,
                    MonthlyValues(12, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)
                )
            };
        }

        public static List<HouseholdExpense> CreateHouseholdExpenses()
        {
            return new List<HouseholdExpense>
            {
                DomainFactory.CreateHouseholdExpense(
                    "Cell Phone Plan",
                    120,
                    "Utilities",
                    "Julie + Anna",
                    MonthlyValues(12, 120, 120, 120, 120, 120, 120, 120, 120, 120, 120, 120)
                ),

                DomainFactory.CreateHouseholdExpense(
                    "Streaming Services",
                    45,
                    "Entertainment",
                    "Family",
                    MonthlyValues(12, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45)
                )
            };
        }

        private static List<MonthlyValue> MonthlyValues(int months, params decimal[] values)
        {
            var list = new List<MonthlyValue>();
            var monthNames = new[]
            {
                "Jan","Feb","Mar","Apr","May","Jun",
                "Jul","Aug","Sep","Oct","Nov","Dec"
            };

            for (int i = 0; i < months; i++)
            {
                list.Add(new MonthlyValue
                {
                    Month = monthNames[i],
                    Amount = values[i]
                });
            }

            return list;
        }
    }
}
