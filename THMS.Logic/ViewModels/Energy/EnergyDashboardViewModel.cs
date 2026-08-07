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

        // ---------------------------------------------------------
        // Summary (always visible)
        // ---------------------------------------------------------
        public EnergySummaryViewModel Summary { get; private set; }

        // ---------------------------------------------------------
        // Time-range view models
        // ---------------------------------------------------------
        public EnergyDayViewModel Day { get; private set; }
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
            LoadSummary();
            LoadDay();
            LoadWeek();
            LoadMonth();
            LoadYear();
            // Custom starts empty until user selects a range
            Custom = new EnergyPeriodViewModel();
        }

        // ---------------------------------------------------------
        // Loaders
        // ---------------------------------------------------------
        private void LoadSummary()
        {
            var summary = _service.GetSummary(CurrentDate);
            Summary = new EnergySummaryViewModel(summary);
        }

        private void LoadDay()
        {
            var day = _service.GetDay(CurrentDate);
            Day = new EnergyDayViewModel(day);
        }

        private void LoadWeek()
        {
            var week = _service.GetWeek(CurrentDate);
            Week = new EnergyPeriodViewModel(week);
        }

        private void LoadMonth()
        {
            var month = _service.GetMonth(CurrentDate);
            Month = new EnergyPeriodViewModel(month);
        }

        private void LoadYear()
        {
            var year = _service.GetYear(CurrentDate);
            Year = new EnergyPeriodViewModel(year);
        }

        // ---------------------------------------------------------
        // Custom range loader
        // ---------------------------------------------------------
        public void LoadCustom(DateTime start, DateTime end)
        {
            var custom = _service.GetRange(start, end);
            Custom = new EnergyPeriodViewModel(custom);
        }

        public void MoveDay(int delta)
        {
            CurrentDate = CurrentDate.AddDays(delta);
            LoadDay();
        }

        public void JumpToDay(DateTime date)
        {
            CurrentDate = date.Date;
            LoadDay();
        }

        public void MoveWeek(int delta)
        {
            CurrentDate = CurrentDate.AddDays(7 * delta);
            LoadWeek();
        }

        public void JumpToWeek(DateTime date)
        {
            CurrentDate = date.Date;
            LoadWeek();
        }

        public void MoveMonth(int delta)
        {
            CurrentDate = CurrentDate.AddMonths(delta);
            LoadMonth();
        }

        public void JumpToMonth(DateTime date)
        {
            CurrentDate = date.Date;
            LoadMonth();
        }
        public void MoveYear(int delta)
        {
            CurrentDate = CurrentDate.AddYears(delta);
            LoadYear();
        }

        public void JumpToYear(DateTime date)
        {
            CurrentDate = date.Date;
            LoadYear();
        }

    }
}
