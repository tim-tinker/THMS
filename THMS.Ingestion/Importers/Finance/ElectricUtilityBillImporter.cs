using CsvHelper;
using System.Globalization;
using THMS.Data.Stores;
using THMS.Domain.Finance.Billing;

namespace THMS.Ingestion.Importers.Finance
{
    public class ElectricUtilityBillImporter
    {
        private readonly SQLLiteFinanceDataStore _store;

        public ElectricUtilityBillImporter(SQLLiteFinanceDataStore store)
        {
            _store = store;
        }

        public void Import(string csvPath)
        {
            using var reader = new StreamReader(csvPath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            csv.Read();
            csv.ReadHeader();

            while (csv.Read())
            {
                var interval = new ElectricUtilityBill
                {
                    Start = csv.GetField<DateTime>("Start"),
                    End = csv.GetField<DateTime>("End"),
                    GridImportCost = csv.GetField<decimal>("Grid Import Cost"),
                    GridExportCredit = csv.GetField<decimal>("Grid Export Credit"),
                    FixedCharges = csv.GetField<decimal>("Fixed Charges"),
                    DeliveryCharges = csv.GetField<decimal>("Delivery Charges"),
                    TaxesAndFees = csv.GetField<decimal>("Taxes and Fees")
                };

                _store.AddElectricUtilityBillCostInterval(interval);
            }
        }
    }
}
