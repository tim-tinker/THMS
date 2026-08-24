using System;
using System.Collections.Generic;
using System.Windows.Forms;
using THMS.Data.Stores;
using THMS.UI.WinForms.Controls;

namespace THMS.UI.WinForms
{
    public partial class FinanceDataCenterForm : BaseEmbeddedForm
    {
        private readonly IAccountDataStore _accountStore;
        private readonly ITransactionDataStore _transactionStore;

        private Control? _currentControl;
        private readonly Dictionary<string, Control> _controls = new();

        public FinanceDataCenterForm(
            IAccountDataStore accountStore,
            ITransactionDataStore transactionStore)
        {
            InitializeComponent();

            _accountStore = accountStore;
            _transactionStore = transactionStore;

            CreateControlDictionary();
        }

        private void CreateControlDictionary()
        {
            AddControl(new AccountUpdaterControl(_accountStore), "Accounts");
            AddControl(new TransactionUpdaterControl(_transactionStore, _accountStore), "Transactions");
        }

        private void AddControl(UserControl control, string label)
        {
            control.Dock = DockStyle.Fill;
            _controls[label] = control;

            var menuItem = dataTypeToolStripMenuItem.DropDownItems.Add(label);
            menuItem.Click += OnClickTypeMenuItem;
        }

        private void ClearControls()
        {
            if (_currentControl != null)
            {
                panelHost.Controls.Remove(_currentControl);
                _currentControl = null;
            }
        }

        private void OnClickTypeMenuItem(object sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem menuItem)
                return;

            var label = menuItem.Text;
            if (!_controls.ContainsKey(label))
                return;

            ClearControls();

            var control = _controls[label];
            _currentControl = control;

            panelHost.Controls.Add(control);
            control.BringToFront();
        }

        private void OnClickClose(object sender, EventArgs e)
        {
            Close();
        }
    }
}
