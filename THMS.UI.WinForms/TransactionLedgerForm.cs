using THMS.UI.WinForms.Controls;

namespace THMS.UI.WinForms
{
    // TODO: Not used. Other forms or updaters appear to have replaced this class. Remove later once that is confirmed.
    public class TransactionLedgerForm : Form
    {
        public TransactionLedgerForm()
        {
            Text = "Accounts and Transactions";
            StartPosition = FormStartPosition.CenterParent;
            WindowState = FormWindowState.Maximized;
            MinimizeBox = true;
            MaximizeBox = true;
            ShowInTaskbar = false;
            Controls.Add(new TransactionManagerControl { Dock = DockStyle.Fill });
        }
    }
}
