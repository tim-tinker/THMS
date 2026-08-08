using System;
using System.Collections.Generic;
using System.Linq;
using THMS.Domain.Transportation;
using THMS.Domain.Energy;
using THMS.Data.Stores;
using THMS.Logic.Energy;

namespace THMS.Logic.Transportation
{
    public class MpgeEngine
    {
        private readonly IVehicleDataStore _vehicleStore;
        private readonly IEnergyDataStore _energyStore;
        private readonly EvAttributionEngine _energyAttributionEngine;

        // EPA conversion constant
        private const decimal KwhPerGallonEquivalent = 33.7m;

        public MpgeEngine(IVehicleDataStore vehicleStore, IEnergyDataStore energyStore)
        {
            _vehicleStore = vehicleStore;
            _energyStore = energyStore;
            _energyAttributionEngine = new EvAttributionEngine(energyStore);
        }

        public MpgeResult Compute(Guid vehicleId, DateTime start, DateTime end)
        {
            var result = new MpgeResult();

            // ---------------------------------------------------------
            // 1. Get EV mileage records (only those with odometer)
            // ---------------------------------------------------------
            var evMileage = _vehicleStore
                .GetEvChargeSessions(vehicleId, start, end)
                .OrderBy(r => r.StartTime)
                .ToList();

            if (evMileage.Count < 2)
            {
                result = new MpgeResult
                {
                    VehicleId = vehicleId,
                    StartDate = start,
                    EndDate = end,
                    MilesDriven = 0m,
                    WhUsed = 0m,
                };
            }
            else
            {
                decimal startMiles = evMileage.First().OdometerMiles;
                decimal endMiles = evMileage.Last().OdometerMiles;
                decimal milesDriven = endMiles - startMiles;

                // ---------------------------------------------------------
                // 2. Get EV energy attribution in the date range
                // ---------------------------------------------------------
                _energyAttributionEngine.Compute(start, end);
                var energyAttr = _energyStore.GetEvAttribution(start, end);

                decimal totalWh = energyAttr.Sum(a => a.EvChargeWh);
                decimal totalKwh = totalWh / 1000m;

                // ---------------------------------------------------------
                // 3. Convert kWh → gallon equivalent
                // ---------------------------------------------------------
                decimal gallonEquivalent = totalKwh / KwhPerGallonEquivalent;

                // ---------------------------------------------------------
                // 4. Compute MPGe
                // ---------------------------------------------------------
                decimal mpge = gallonEquivalent > 0m
                    ? milesDriven / gallonEquivalent
                    : 0m;

                // ---------------------------------------------------------
                // 6. Return result
                // ---------------------------------------------------------
                result = new MpgeResult
                {
                    VehicleId = vehicleId,
                    StartDate = start,
                    EndDate = end,
                    MilesDriven = milesDriven,
                    WhUsed = totalWh,
                };
            }

            return result;
        }
    }
}
