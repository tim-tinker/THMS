using System;
using System.Collections.Generic;
using System.Linq;
using THMS.Domain.Transportation;

namespace THMS.Logic.Transportation
{
    /// <summary>
    /// Computes MPG values for ICE vehicles, correctly aggregating
    /// partial fill-ups into the next full fill-up.
    /// </summary>
    public class MpgEngine
    {
        public IEnumerable<MpgResult> ComputeMpg(IEnumerable<IceMileageRecord> records)
        {
            var ordered = records.OrderBy(r => r.EndTime).ToList();

            // Identify all full fill-ups
            var fullFillUps = ordered.Where(r => r.IsFullFillUp).ToList();

            var results = new List<MpgResult>();

            for (int i = 1; i < fullFillUps.Count; i++)
            {
                var previousFull = fullFillUps[i - 1];
                var currentFull = fullFillUps[i];

                // All records between the two full fill-ups
                var between = ordered
                    .Where(r => r.EndTime > previousFull.EndTime && r.EndTime <= currentFull.EndTime)
                    .ToList();

                // Sum gallons from partial + current full
                var totalGallons = between.Sum(r => r.GallonsAdded);

                // Miles driven
                var milesDriven = currentFull.OdometerMiles - previousFull.OdometerMiles;

                results.Add(new MpgResult
                {
                    Date = currentFull.EndTime,
                    MilesDriven = milesDriven,
                    GallonsUsed = totalGallons
                });
            }

            return results;
        }

        public decimal ComputeAverageMpg(IEnumerable<IceMileageRecord> records)
        {
            var mpgResults = ComputeMpg(records).ToList();

            var totalMiles = mpgResults.Sum(r => r.MilesDriven);
            var totalGallons = mpgResults.Sum(r => r.GallonsUsed);

            return totalGallons > 0 ? totalMiles / totalGallons : 0;
        }

        public decimal ComputeMonthlyMpg(IEnumerable<IceMileageRecord> records, DateTime monthStart, DateTime monthEnd)
        {
            var monthlyRecords = records
                .Where(r => r.EndTime >= monthStart && r.EndTime <= monthEnd)
                .ToList();

            return ComputeAverageMpg(monthlyRecords);
        }
    }
}
