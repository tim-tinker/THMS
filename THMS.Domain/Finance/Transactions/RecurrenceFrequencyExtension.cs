using System;
using System.Collections.Generic;
using System.Text;

namespace THMS.Domain.Finance.Transactions
{
    public static class RecurrenceFrequencyExtension
    {
        public static DateTime AddFrequency(this DateTime date, RecurrenceFrequency freq)
        {
            return freq switch
            {
                RecurrenceFrequency.Weekly => date.AddDays(7),
                RecurrenceFrequency.BiWeekly => date.AddDays(14),
                RecurrenceFrequency.Monthly => date.AddMonths(1),
                RecurrenceFrequency.Quarterly => date.AddMonths(3),
                RecurrenceFrequency.Yearly => date.AddYears(1),
                _ => throw new ArgumentOutOfRangeException(nameof(freq))
            };
        }
    }
}
