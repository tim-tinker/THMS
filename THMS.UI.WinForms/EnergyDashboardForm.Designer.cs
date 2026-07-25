namespace THMS.UI.WinForms
{
    partial class EnergyDashboardForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.energyGrid = new System.Windows.Forms.DataGridView();
            this.lblStart = new System.Windows.Forms.Label();
            this.lblEnd = new System.Windows.Forms.Label();
            this.dtpStart = new System.Windows.Forms.DateTimePicker();
            this.dtpEnd = new System.Windows.Forms.DateTimePicker();
            this.btnRefresh = new System.Windows.Forms.Button();

            ((System.ComponentModel.ISupportInitialize)(this.energyGrid)).BeginInit();
            this.SuspendLayout();

            // energyGrid
            this.energyGrid.AllowUserToAddRows = false;
            this.energyGrid.AllowUserToDeleteRows = false;
            this.energyGrid.AllowUserToResizeRows = false;
            this.energyGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.energyGrid.Location = new System.Drawing.Point(12, 60);
            this.energyGrid.MultiSelect = false;
            this.energyGrid.Name = "energyGrid";
            this.energyGrid.ReadOnly = true;
            this.energyGrid.RowHeadersVisible = false;
            this.energyGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.energyGrid.Size = new System.Drawing.Size(760, 380);

            // lblStart
            this.lblStart.Location = new System.Drawing.Point(12, 15);
            this.lblStart.Name = "lblStart";
            this.lblStart.Size = new System.Drawing.Size(80, 25);
            this.lblStart.Text = "Start:";

            // dtpStart
            this.dtpStart.Location = new System.Drawing.Point(90, 12);
            this.dtpStart.Name = "dtpStart";
            this.dtpStart.Size = new System.Drawing.Size(200, 27);

            // lblEnd
            this.lblEnd.Location = new System.Drawing.Point(310, 15);
            this.lblEnd.Name = "lblEnd";
            this.lblEnd.Size = new System.Drawing.Size(80, 25);
            this.lblEnd.Text = "End:";

            // dtpEnd
            this.dtpEnd.Location = new System.Drawing.Point(390, 12);
            this.dtpEnd.Name = "dtpEnd";
            this.dtpEnd.Size = new System.Drawing.Size(200, 27);

            // btnRefresh
            this.btnRefresh.Location = new System.Drawing.Point(610, 10);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(120, 35);
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);

            // EnergyDashboardForm
            this.ClientSize = new System.Drawing.Size(784, 461);
            this.Controls.Add(this.btnRefresh);
            this.Controls.Add(this.dtpEnd);
            this.Controls.Add(this.lblEnd);
            this.Controls.Add(this.dtpStart);
            this.Controls.Add(this.lblStart);
            this.Controls.Add(this.energyGrid);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "EnergyDashboardForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Energy Dashboard";

            ((System.ComponentModel.ISupportInitialize)(this.energyGrid)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.DataGridView energyGrid;
        private System.Windows.Forms.Label lblStart;
        private System.Windows.Forms.Label lblEnd;
        private System.Windows.Forms.DateTimePicker dtpStart;
        private System.Windows.Forms.DateTimePicker dtpEnd;
        private System.Windows.Forms.Button btnRefresh;
    }
}
