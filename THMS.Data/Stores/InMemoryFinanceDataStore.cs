using System;
using System.Collections.Generic;
using System.Linq;
using THMS.Domain.Energy;
using THMS.Domain.Finance;
using THMS.Domain.Finance.Billing;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores
{
    public class InMemoryFinanceDataStore : IFinanceDataStore
    {
        private readonly List<ElectricUtilityBill> _utilityBills = new();
        private readonly List<CommercialChargeCostRecord> _commercialCosts = new();
        private readonly List<GasPurchase> _gasPurchases = new();
        private readonly List<EvChargeSession> _evChargeSessions = new();

        public InMemoryFinanceDataStore()
        {
            // ---------------------------------------------------------
            // ELECTRIC UTILITY BILLS
            // ---------------------------------------------------------
            AddElectricUtilityBill(new ElectricUtilityBill
            {
                Id = Guid.NewGuid(),
                StartDate = DateTime.Today.AddMonths(-1).AddDays(-5),
                EndDate = DateTime.Today.AddMonths(-1).AddDays(25),
                GridImportCost = 85.00m,
                GridExportCredit = 12.50m,
                DeliveryCharges = 22.00m,
                FixedCharges = 10.00m,
                TaxesAndFees = 8.75m,
                TotalKwh = 650
            });

            // ---------------------------------------------------------
            // GAS PURCHASES
            // ---------------------------------------------------------
            AddGasPurchase(new GasPurchase
            {
                Id = Guid.NewGuid(),
                VehicleId = Guid.Empty, // replace with ICE vehicle ID in UI
                Date = DateTime.Today.AddDays(-7),
                Gallons = 11.2m,
                FuelCost = 34.80m,
                Station = "Shell"
            });

            AddGasPurchase(new GasPurchase
            {
                Id = Guid.NewGuid(),
                VehicleId = Guid.Empty,
                Date = DateTime.Today.AddDays(-2),
                Gallons = 12.0m,
                FuelCost = 38.10m,
                Station = "Chevron"
            });

            // ---------------------------------------------------------
            // COMMERCIAL CHARGING COST RECORDS
            // ---------------------------------------------------------
            AddCommercialChargeCostRecord(new CommercialChargeCostRecord
            {
                Id = Guid.NewGuid(),
                SessionId = "EA-2026-07-22-ABC123",
                Date = DateTime.Today.AddDays(-2),
                Cost = 11.99m,
                Vendor = "Electrify America"
            });

            AddCommercialChargeCostRecord(new CommercialChargeCostRecord
            {
                Id = Guid.NewGuid(),
                SessionId = "CP-2026-07-20-XYZ789",
                Date = DateTime.Today.AddDays(-5),
                Cost = 9.50m,
                Vendor = "ChargePoint"
            });
        }

        // ---------------------------------------------------------
        // ELECTRIC UTILITY BILLS
        // ---------------------------------------------------------

        public void AddElectricUtilityBill(ElectricUtilityBill bill)
        {
            _utilityBills.Add(bill);
        }

        public IEnumerable<ElectricUtilityBill> GetElectricUtilityBills(DateTime start, DateTime end)
        {
            return _utilityBills
                .Where(b => b.StartDate >= start && b.EndDate <= end)
                .OrderBy(b => b.StartDate);
        }

        // ---------------------------------------------------------
        // COMMERCIAL CHARGING COST RECORDS
        // ---------------------------------------------------------

        public void AddCommercialChargeCostRecord(CommercialChargeCostRecord record)
        {
            _commercialCosts.Add(record);
        }

        public IEnumerable<CommercialChargeCostRecord> GetCommercialChargeCostRecords(DateTime start, DateTime end)
        {
            return _commercialCosts
                .Where(c => c.Date >= start && c.Date <= end)
                .OrderBy(c => c.Date);
        }

        public IEnumerable<CommercialChargeCostRecord> GetCommercialChargeCostRecordsByVendor(
            string vendor,
            DateTime start,
            DateTime end)
        {
            return _commercialCosts
                .Where(c => c.Vendor.Equals(vendor, StringComparison.OrdinalIgnoreCase))
                .Where(c => c.Date >= start && c.Date <= end)
                .OrderBy(c => c.Date);
        }

        // ---------------------------------------------------------
        // GAS PURCHASES
        // ---------------------------------------------------------

        public void AddGasPurchase(GasPurchase purchase)
        {
            _gasPurchases.Add(purchase);
        }

        public IEnumerable<GasPurchase> GetGasPurchases(Guid vehicleId, DateTime start, DateTime end)
        {
            return _gasPurchases
                .Where(g => g.VehicleId == vehicleId &&
                            g.Date >= start &&
                            g.Date <= end)
                .OrderBy(g => g.Date);
        }

        // ---------------------------------------------------------
        // INCOMPLETE COST RECORDS
        // ---------------------------------------------------------

        public IEnumerable<EvChargeSession> GetEvChargeSessionsWithMissingCost()
        {
            return _evChargeSessions
                .Where(s => s.SessionCost == null)
                .OrderBy(s => s.StartTime);
        }

        public IEnumerable<GasPurchase> GetGasPurchasesWithMissingCost()
        {
            return _gasPurchases
                .Where(g => g.FuelCost == 0)
                .OrderBy(g => g.Date);
        }

        // ---------------------------------------------------------
        // COST UPDATES
        // ---------------------------------------------------------

        public void UpdateEvChargeSessionCost(Guid sessionId, decimal cost)
        {
            var session = _evChargeSessions.FirstOrDefault(s => s.Id == sessionId);
            if (session != null)
                session.SessionCost = cost;
        }

        public void UpdateGasPurchaseCost(Guid purchaseId, decimal cost)
        {
            var purchase = _gasPurchases.FirstOrDefault(g => g.Id == purchaseId);
            if (purchase != null)
                purchase.FuelCost = cost;
        }

        // ---------------------------------------------------------
        // EV SESSION STORAGE (FinanceStore needs cost access)
        // ---------------------------------------------------------

        public void AddEvChargeSession(EvChargeSession session)
        {
            _evChargeSessions.Add(session);
        }
    }
}
