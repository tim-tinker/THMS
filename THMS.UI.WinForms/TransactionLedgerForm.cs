using THMS.UI.WinForms.Controls;

namespace THMS.UI.WinForms
{
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
