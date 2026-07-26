using System;
using System.Collections.Generic;
using System.Linq;
using THMS.Domain.Transportation;

namespace THMS.Logic.Transportation
{
    public class MpgeEngine
    {
        /// <summary>
        /// Computes MPGe and related EV analytics from enriched charging sessions.
        /// Sessions must contain both power data (EvChargingSession)
        /// and vehicle data (EvChargingSessionVehicleData).
        /// </summary>
        public IEnumerable<MpgeResult> ComputeMpge(
            IEnumerable<EvChargingSession> sessions,
            IEnumerable<EvChargingSessionVehicleData> vehicleData,
            decimal batteryCapacityKwh)
        {
            var results = new List<MpgeResult>();

            // Join sessions with their vehicle data
            var enriched = sessions
                .Where(s => s.VehicleDataId.HasValue)
                .Join(
                    vehicleData,
                    s => s.VehicleDataId!.Value,
                    vd => vd.Id,
                    (s, vd) => new { Session = s, Vehicle = vd }
                )
                .OrderBy(x => x.Session.StartTime)
                .ToList();

            if (enriched.Count < 2)
                return results;

            decimal batteryCapacityWh = batteryCapacityKwh * 1000m;

            for (int i = 1; i < enriched.Count; i++)
            {
                var prev = enriched[i - 1];
                var curr = enriched[i];

                // Miles driven between charging sessions
                var milesDriven = curr.Vehicle.OdometerMiles - prev.Vehicle.OdometerMiles;
                if (milesDriven <= 0)
                    continue;

                // SOC delta for driving segment
                var socUsedFraction =
                    (prev.Vehicle.EndSocPercent - curr.Vehicle.StartSocPercent) / 100m;

                if (socUsedFraction <= 0)
                    continue;

                // Energy used for driving
                var whUsed = socUsedFraction * batteryCapacityWh;

                // Effective battery capacity from current charging session
                var socAddedFraction =
                    (curr.Vehicle.EndSocPercent - curr.Vehicle.StartSocPercent) / 100m;

                decimal effectiveCapacityWh = 0;

                if (socAddedFraction > 0 && curr.Session.KwhAdded > 0)
                {
                    effectiveCapacityWh = (curr.Session.KwhAdded * 1000m) / socAddedFraction;
                }

                results.Add(new MpgeResult
                {
                    Date = curr.Session.EndTime,
                    MilesDriven = milesDriven,
                    WhUsed = whUsed,
                    EffectiveCapacityWh = effectiveCapacityWh
                });
            }

            return results;
        }
    }
}
