using System;
using THMS.Data.Stores;
using THMS.Domain.Energy;
using THMS.Logic.Energy;

namespace THMS.Logic.ViewModels.Energy
{
    public class EnergyDashboardViewModel
    {
        private EnergyAggregationService _service;

        protected DateTime CurrentDate { get; private set; } = DateTime.Now;

        public EnergyTab SelectedTab { get; set; }


        // ---------------------------------------------------------
        // Summary (always visible)
        // ---------------------------------------------------------
        public EnergySummaryViewModel Summary { get; private set; }

        // ---------------------------------------------------------
        // Time-range view models
        // ---------------------------------------------------------
        public EnergyPeriodViewModel Day { get; private set; }
        public EnergyPeriodViewModel Week { get; private set; }
        public EnergyPeriodViewModel Month { get; private set; }
        public EnergyPeriodViewModel Year { get; private set; }
        public EnergyPeriodViewModel Custom { get; private set; }

        // ---------------------------------------------------------
        // Constructor
        // ---------------------------------------------------------
        public EnergyDashboardViewModel() { }

        public void SetStores(IEnergyDataStore energyStore)
        {
            _service = new EnergyAggregationService(energyStore);
        }

        public void Refresh()
        {
            Day = new EnergyPeriodViewModel(_service.GetDay(CurrentDate));
            Week = new EnergyPeriodViewModel(_service.GetWeek(CurrentDate));
            Month = new EnergyPeriodViewModel(_service.GetMonth(CurrentDate));
            Year = new EnergyPeriodViewModel(_service.GetYear(CurrentDate));
            // Custom starts empty until user selects a range
            Custom = new EnergyPeriodViewModel();
            //Custom = new EnergyPeriodViewModel(_service.GetRange(start, end));

            // Select correct summary based on active tab
            Summary = SelectedTab switch
            {
                EnergyTab.Day => new EnergySummaryViewModel(_service.GetPeriodSummary(_service.GetDay(CurrentDate))),
                EnergyTab.Week => new EnergySummaryViewModel(_service.GetPeriodSummary(_service.GetWeek(CurrentDate))),
                EnergyTab.Month => new EnergySummaryViewModel(_service.GetPeriodSummary(_service.GetMonth(CurrentDate))),
                EnergyTab.Year => new EnergySummaryViewModel(_service.GetPeriodSummary(_service.GetYear(CurrentDate))),
                //EnergyTab.Custom => _service.GetPeriodSummary(_service.GetRange(start, end)),
                _ => new EnergySummaryViewModel(_service.GetPeriodSummary(_service.GetDay(CurrentDate)))
            };
        }

        public void MoveDay(int delta)
        {
            CurrentDate = CurrentDate.AddDays(delta);
            Refresh();
        }

        public void JumpToDay(DateTime date)
        {
            CurrentDate = date.Date;
            Refresh();
        }

        public void MoveWeek(int delta)
        {
            CurrentDate = CurrentDate.AddDays(7 * delta);
            Refresh();
        }

        public void JumpToWeek(DateTime date)
        {
            CurrentDate = date.Date;
            Refresh();
        }

        public void MoveMonth(int delta)
        {
            CurrentDate = CurrentDate.AddMonths(delta);
            Refresh();
        }

        public void JumpToMonth(DateTime date)
        {
            CurrentDate = date.Date;
            Refresh();
        }
        public void MoveYear(int delta)
        {
            CurrentDate = CurrentDate.AddYears(delta);
            Refresh();
        }

        public void JumpToYear(DateTime date)
        {
            CurrentDate = date.Date;
            Refresh();
        }

    }
}
