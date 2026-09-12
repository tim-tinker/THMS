namespace THMS.UI.WinForms.Controls
{
    partial class AccountsIngestionControl
    {
        private System.ComponentModel.IContainer components = null;
        private AccountUpdaterControl accountUpdater;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            accountUpdater = new AccountUpdaterControl();
            SuspendLayout();
            accountUpdater.Dock = DockStyle.Fill;
            accountUpdater.Name = "accountUpdater";
            AutoScaleMode = AutoScaleMode.Dpi;
            Controls.Add(accountUpdater);
            Name = "AccountsIngestionControl";
            Size = new Size(1000, 720);
            ResumeLayout(false);
        }
    }
}
