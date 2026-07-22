namespace THMS.UI.WinForms
{
    partial class FinanceDashboardForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TableLayoutPanel mainLayout;
        private System.Windows.Forms.ListBox accountListBox;
        private System.Windows.Forms.GroupBox accountDetailsGroup;
        private System.Windows.Forms.Label lblAccountName;
        private System.Windows.Forms.Label lblBalance;
        private System.Windows.Forms.Label lblMonthlyIncome;
        private System.Windows.Forms.Label lblMonthlyExpenses;
        private System.Windows.Forms.DataVisualization.Charting.Chart financeChart;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            this.mainLayout = new System.Windows.Forms.TableLayoutPanel();
            this.accountListBox = new System.Windows.Forms.ListBox();
            this.accountDetailsGroup = new System.Windows.Forms.GroupBox();
            this.lblAccountName = new System.Windows.Forms.Label();
            this.lblBalance = new System.Windows.Forms.Label();
            this.lblMonthlyIncome = new System.Windows.Forms.Label();
            this.lblMonthlyExpenses = new System.Windows.Forms.Label();
            this.financeChart = new System.Windows.Forms.DataVisualization.Charting.Chart();

            ((System.ComponentModel.ISupportInitialize)(this.financeChart)).BeginInit();
            this.mainLayout.SuspendLayout();
            this.accountDetailsGroup.SuspendLayout();
            this.SuspendLayout();

            // mainLayout
            this.mainLayout.ColumnCount = 2;
            this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 220F));
            this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainLayout.RowCount = 2;
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 160F));
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainLayout.Controls.Add(this.accountListBox, 0, 0);
            this.mainLayout.SetRowSpan(this.accountListBox, 2);
            this.mainLayout.Controls.Add(this.accountDetailsGroup, 1, 0);
            this.mainLayout.Controls.Add(this.financeChart, 1, 1);

            // accountListBox
            this.accountListBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.accountListBox.SelectedIndexChanged += this.AccountListBox_SelectedIndexChanged;

            // accountDetailsGroup
            this.accountDetailsGroup.Dock = System.Windows.Forms.DockStyle.Fill;
            this.accountDetailsGroup.Text = "Account Details";
            this.accountDetailsGroup.Controls.Add(this.lblAccountName);
            this.accountDetailsGroup.Controls.Add(this.lblBalance);
            this.accountDetailsGroup.Controls.Add(this.lblMonthlyIncome);
            this.accountDetailsGroup.Controls.Add(this.lblMonthlyExpenses);

            // lblAccountName
            this.lblAccountName.AutoSize = true;
            this.lblAccountName.Location = new System.Drawing.Point(16, 28);
            this.lblAccountName.Text = "Account:";

            // lblBalance
            this.lblBalance.AutoSize = true;
            this.lblBalance.Location = new System.Drawing.Point(16, 52);
            this.lblBalance.Text = "Balance:";

            // lblMonthlyIncome
            this.lblMonthlyIncome.AutoSize = true;
            this.lblMonthlyIncome.Location = new System.Drawing.Point(16, 80);
            this.lblMonthlyIncome.Text = "Monthly Income:";

            // lblMonthlyExpenses
            this.lblMonthlyExpenses.AutoSize = true;
            this.lblMonthlyExpenses.Location = new System.Drawing.Point(16, 104);
            this.lblMonthlyExpenses.Text = "Monthly Expenses:";

            // financeChart
            this.financeChart.Dock = System.Windows.Forms.DockStyle.Fill;
            this.financeChart.ChartAreas.Add(new System.Windows.Forms.DataVisualization.Charting.ChartArea("Default"));
            var series = new System.Windows.Forms.DataVisualization.Charting.Series("MonthlyNet");
            series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Column;
            series.ChartArea = "Default";
            this.financeChart.Series.Add(series);

            // FinanceDashboardForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.mainLayout);
            this.Name = "FinanceDashboardForm";
            this.Text = "Finance Dashboard";

            this.mainLayout.ResumeLayout(false);
            this.accountDetailsGroup.ResumeLayout(false);
            this.accountDetailsGroup.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.financeChart)).EndInit();
            this.ResumeLayout(false);
        }
    }
}
