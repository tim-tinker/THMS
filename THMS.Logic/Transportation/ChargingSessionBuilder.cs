using System;
using System.Collections.Generic;
using System.Linq;
using THMS.Domain.Energy;
using THMS.Domain.Transportation;

namespace THMS.Logic.Transportation
{
    public class ChargingSessionBuilder
    {
        /// <summary>
        /// Builds home charging sessions from raw EV circuit readings.
        /// Consecutive readings with no large gaps are grouped into one session.
        /// </summary>
        public IEnumerable<EvChargingSession> BuildHomeChargingSessions(
            IEnumerable<EvCircuitReading> readings,
            TimeSpan maxGap)
        {
            var sessions = new List<EvChargingSession>();
            var ordered = readings.OrderBy(r => r.Timestamp).ToList();

            if (!ordered.Any())
                return sessions;

            DateTime start = ordered.First().Timestamp;
            DateTime end = start;
            decimal whTotal = 0;

            for (int i = 1; i < ordered.Count; i++)
            {
                var prev = ordered[i - 1];
                var curr = ordered[i];

                var gap = curr.Timestamp - prev.Timestamp;

                if (gap <= maxGap)
                {
                    // Same session
                    end = curr.Timestamp;
                    whTotal += curr.WattHours;
                }
                else
                {
                    // End previous session
                    sessions.Add(new EvChargingSession
                    {
                        Id = Guid.NewGuid(),
                        StartTime = start,
                        EndTime = end,
                        KwhAdded = whTotal / 1000m,
                        IsHomeCharging = true,
                        ChargingCost = null,
                        VehicleDataId = null
                    });

                    // Start new session
                    start = curr.Timestamp;
                    end = start;
                    whTotal = curr.WattHours;
                }
            }

            // Add final session
            sessions.Add(new EvChargingSession
            {
                Id = Guid.NewGuid(),
                StartTime = start,
                EndTime = end,
                KwhAdded = whTotal / 1000m,
                IsHomeCharging = true,
                ChargingCost = null,
                VehicleDataId = null
            });

            return sessions;
        }

        /// <summary>
        /// Maps commercial charging sessions into unified EvChargingSession objects.
        /// </summary>
        public EvChargingSession BuildCommercialChargingSession(
            EvCommercialChargingSession commercial)
        {
            return new EvChargingSession
            {
                Id = commercial.Id,
                StartTime = commercial.StartTime,
                EndTime = commercial.EndTime,
                KwhAdded = commercial.KwhAdded,
                ChargingCost = commercial.ChargingCost,
                IsHomeCharging = false,
                VehicleDataId = null
            };
        }
    }
}
