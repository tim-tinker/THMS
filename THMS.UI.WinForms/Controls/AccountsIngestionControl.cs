namespace THMS.UI.WinForms.Controls
{
    public partial class AccountsIngestionControl : UserControl
    {
        public AccountsIngestionControl()
        {
            InitializeComponent();
        }

        public void RefreshAccounts() => accountUpdater.RefreshAccounts();
    }
}
