namespace THMS.UI.WinForms
{
    partial class HouseholdDashboardForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TableLayoutPanel mainLayout;
        private System.Windows.Forms.ListBox expenseListBox;
        private System.Windows.Forms.GroupBox expenseDetailsGroup;
        private System.Windows.Forms.Label lblExpenseName;
        private System.Windows.Forms.Label lblMonthlyCost;
        private System.Windows.Forms.Label lblCategory;
        private System.Windows.Forms.Label lblSharedWith;
        private System.Windows.Forms.DataVisualization.Charting.Chart householdChart;

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
            this.expenseListBox = new System.Windows.Forms.ListBox();
            this.expenseDetailsGroup = new System.Windows.Forms.GroupBox();
            this.lblExpenseName = new System.Windows.Forms.Label();
            this.lblMonthlyCost = new System.Windows.Forms.Label();
            this.lblCategory = new System.Windows.Forms.Label();
            this.lblSharedWith = new System.Windows.Forms.Label();
            this.householdChart = new System.Windows.Forms.DataVisualization.Charting.Chart();

            ((System.ComponentModel.ISupportInitialize)(this.householdChart)).BeginInit();
            this.mainLayout.SuspendLayout();
            this.expenseDetailsGroup.SuspendLayout();
            this.SuspendLayout();

            // mainLayout
            this.mainLayout.ColumnCount = 2;
            this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 220F));
            this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainLayout.RowCount = 2;
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 160F));
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainLayout.Controls.Add(this.expenseListBox, 0, 0);
            this.mainLayout.SetRowSpan(this.expenseListBox, 2);
            this.mainLayout.Controls.Add(this.expenseDetailsGroup, 1, 0);
            this.mainLayout.Controls.Add(this.householdChart, 1, 1);

            // expenseListBox
            this.expenseListBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.expenseListBox.SelectedIndexChanged += this.ExpenseListBox_SelectedIndexChanged;

            // expenseDetailsGroup
            this.expenseDetailsGroup.Dock = System.Windows.Forms.DockStyle.Fill;
            this.expenseDetailsGroup.Text = "Expense Details";
            this.expenseDetailsGroup.Controls.Add(this.lblExpenseName);
            this.expenseDetailsGroup.Controls.Add(this.lblMonthlyCost);
            this.expenseDetailsGroup.Controls.Add(this.lblCategory);
            this.expenseDetailsGroup.Controls.Add(this.lblSharedWith);

            // lblExpenseName
            this.lblExpenseName.AutoSize = true;
            this.lblExpenseName.Location = new System.Drawing.Point(16, 28);
            this.lblExpenseName.Text = "Expense:";

            // lblMonthlyCost
            this.lblMonthlyCost.AutoSize = true;
            this.lblMonthlyCost.Location = new System.Drawing.Point(16, 52);
            this.lblMonthlyCost.Text = "Monthly Cost:";

            // lblCategory
            this.lblCategory.AutoSize = true;
            this.lblCategory.Location = new System.Drawing.Point(16, 80);
            this.lblCategory.Text = "Category:";

            // lblSharedWith
            this.lblSharedWith.AutoSize = true;
            this.lblSharedWith.Location = new System.Drawing.Point(16, 104);
            this.lblSharedWith.Text = "Shared With:";

            // householdChart
            this.householdChart.Dock = System.Windows.Forms.DockStyle.Fill;
            this.householdChart.ChartAreas.Add(new System.Windows.Forms.DataVisualization.Charting.ChartArea("Default"));
            var series = new System.Windows.Forms.DataVisualization.Charting.Series("MonthlyExpense");
            series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Column;
            series.ChartArea = "Default";
            this.householdChart.Series.Add(series);

            // HouseholdDashboardForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.mainLayout);
            this.Name = "HouseholdDashboardForm";
            this.Text = "Household Dashboard";

            this.mainLayout.ResumeLayout(false);
            this.expenseDetailsGroup.ResumeLayout(false);
            this.expenseDetailsGroup.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.householdChart)).EndInit();
            this.ResumeLayout(false);
        }
    }
}
