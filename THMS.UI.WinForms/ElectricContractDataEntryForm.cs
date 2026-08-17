using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using THMS.Domain.Finance;

namespace THMS.UI.WinForms
{
    public partial class ElectricContractDataEntryForm : Form
    {
        public ElectricContract Contract { get; private set; }

        public ElectricContractDataEntryForm()
        {
            InitializeComponent();
        }

        private void OnClickCancel(object sender, EventArgs e)
        {
            Contract = null;
            Close();
        }

        private void OnClickSave(object sender, EventArgs e)
        {
            Contract = new ElectricContract
            {
                Name = _textName.Text,
                StartDate = _dateStart.Value,
                EndDate = _dateEnd.Value,
                BaseEnergyCharge = _numBaseEnergyCharge.Value,
                EnergyChargeRate = _numEnergyRate.Value,
                BaseDeliveryCharge = _numDeliveryBaseCharge.Value,
                DeliveryChargeRate = _numDeliveryRate.Value,
                ExportCreditRate = _numSolarCreditRate.Value
            };
            Close();
        }
    }
}
