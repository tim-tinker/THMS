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
        private readonly EnergyAttributionEngine _energyAttributionEngine;

        // EPA conversion constant
        private const decimal KwhPerGallonEquivalent = 33.7m;

        public MpgeEngine(IVehicleDataStore vehicleStore, IEnergyDataStore energyStore)
        {
            _vehicleStore = vehicleStore;
            _energyAttributionEngine = new EnergyAttributionEngine(energyStore);
        }

        public MpgeResult Compute(Guid vehicleId, DateTime start, DateTime end)
        {
            // ---------------------------------------------------------
            // 1. Get EV mileage records (only those with odometer)
            // ---------------------------------------------------------
            var evMileage = _vehicleStore
                .GetEvChargingSessionVehicleData(vehicleId, start, end)
                .Where(r => r.VehicleId == vehicleId && r.OdometerMiles.HasValue)
                .OrderBy(r => r.Date)
                .ToList();

            if (evMileage.Count < 2)
            {
                return new MpgeResult
                {
                    VehicleId = vehicleId,
                    StartDate = start,
                    EndDate = end,
                    MilesDriven = 0m,
                    WhUsed = 0m,
                };
            }

            decimal startMiles = evMileage.First().OdometerMiles!.Value;
            decimal endMiles = evMileage.Last().OdometerMiles!.Value;
            decimal milesDriven = endMiles - startMiles;

            // ---------------------------------------------------------
            // 2. Get EV energy attribution in the date range
            // ---------------------------------------------------------
            var energyAttr = _energyAttributionEngine
                .ComputeAttribution(start, end)
                .ToList();

            decimal totalWh = energyAttr.Sum(a => a.EvChargingWh);
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
            return new MpgeResult
            {
                VehicleId = vehicleId,
                StartDate = start,
                EndDate = end,
                MilesDriven = milesDriven,
                WhUsed = totalWh,
            };
        }
    }
}
