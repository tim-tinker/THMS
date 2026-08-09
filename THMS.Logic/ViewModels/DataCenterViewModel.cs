using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using THMS.Logic.DataCenter;

namespace THMS.Logic.ViewModels
{
    public class DataCenterViewModel
    {
        public List<IDataSourceStatus> _dataSourceStatuses = [];

        public SolarDataStatus SolarStatus { get; set; }
        public BillDataStatus BillStatus { get; set; }
        public EvCircuitStatus EvCircuitStatus { get; set; }
        public EvCommercialStatus EvCommercialStatus { get; set; }
        public AttributionStatus AttributionStatus { get; set; }

        public ICommand DownloadSolarData { get; set; }
        public ICommand ImportElectricBill { get; set; }
        public ICommand ImportEvCommercial { get; set; }
        public ICommand RecalculateEvCharging { get; set; }

        public void AddDataSourceStatuses(List<IDataSourceStatus> dataSourceStatuses)
        {
            _dataSourceStatuses.AddRange(dataSourceStatuses);
        }
    }
}
