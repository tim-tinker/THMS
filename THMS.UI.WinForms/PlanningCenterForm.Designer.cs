namespace THMS.UI.WinForms
{
    partial class PlanningCenterForm
    {
        private System.ComponentModel.IContainer components = null;
        private TableLayoutPanel layout;
        private Controls.UpcomingObligationsControl upcomingObligations;
        private Controls.PlannedPaymentsControl plannedPayments;
        private Controls.CashFlowForecastControl cashFlowForecast;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            layout = new TableLayoutPanel();
            upcomingObligations = new Controls.UpcomingObligationsControl();
            plannedPayments = new Controls.PlannedPaymentsControl();
            cashFlowForecast = new Controls.CashFlowForecastControl();
            layout.SuspendLayout();
            SuspendLayout();
            layout.ColumnCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.Controls.Add(upcomingObligations, 0, 0);
            layout.Controls.Add(plannedPayments, 0, 1);
            layout.Controls.Add(cashFlowForecast, 0, 2);
            layout.Dock = DockStyle.Fill;
            layout.Name = "layout";
            layout.RowCount = 3;
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 38F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 42F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            upcomingObligations.Dock = DockStyle.Fill;
            plannedPayments.Dock = DockStyle.Fill;
            cashFlowForecast.AutoSize = true;
            cashFlowForecast.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            cashFlowForecast.Dock = DockStyle.Fill;
            ClientSize = new Size(1100, 760);
            Controls.Add(layout);
            Name = "PlanningCenterForm";
            Text = "Planning Center";
            layout.ResumeLayout(false);
            ResumeLayout(false);
        }
    }
}
