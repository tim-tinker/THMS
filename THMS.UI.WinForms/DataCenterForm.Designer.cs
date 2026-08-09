using static System.Net.Mime.MediaTypeNames;

namespace THMS.UI.WinForms
{
    partial class DataCenterForm
    {
        private System.ComponentModel.IContainer components = null;

        // Dynamic table for IDataSourceStatus rows
        private System.Windows.Forms.TableLayoutPanel tblDynamicSources;

        // Existing fixed controls (you may remove later)
        private System.Windows.Forms.Label lblSolarStatus;
        private System.Windows.Forms.Label lblSolarLast;
        private System.Windows.Forms.Label lblSolarExpected;
        private System.Windows.Forms.Button btnSolarImport;

        private System.Windows.Forms.Label lblBillStatus;
        private System.Windows.Forms.Label lblBillLast;
        private System.Windows.Forms.Label lblBillExpected;
        private System.Windows.Forms.Button btnBillImport;

        private System.Windows.Forms.Label lblEvCircuitStatus;
        private System.Windows.Forms.Label lblEvCircuitLast;
        private System.Windows.Forms.Button btnEvCircuitRecalc;

        private System.Windows.Forms.Label lblEvCommercialStatus;
        private System.Windows.Forms.Label lblEvCommercialLast;
        private System.Windows.Forms.Button btnEvCommercialImport;

        private System.Windows.Forms.Label lblAttrStatus;
        private System.Windows.Forms.Label lblAttrLast;
        private System.Windows.Forms.Button btnAttrRecalc;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            //
            // tblDynamicSources
            //
            tblDynamicSources = new System.Windows.Forms.TableLayoutPanel();
            tblDynamicSources.ColumnCount = 5;
            tblDynamicSources.RowCount = 1;
            tblDynamicSources.Dock = System.Windows.Forms.DockStyle.Top;
            tblDynamicSources.AutoSize = true;
            tblDynamicSources.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Single;

            tblDynamicSources.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            tblDynamicSources.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15F));
            tblDynamicSources.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            tblDynamicSources.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            tblDynamicSources.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));

            // Header row
            tblDynamicSources.Controls.Add(new System.Windows.Forms.Label
            {
                Text = "Data Source",
                Dock = System.Windows.Forms.DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold)
            }, 0, 0);

            tblDynamicSources.Controls.Add(new System.Windows.Forms.Label
            {
                Text = "Status",
                Dock = System.Windows.Forms.DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold)
            }, 1, 0);

            tblDynamicSources.Controls.Add(new System.Windows.Forms.Label
            {
                Text = "Last",
                Dock = System.Windows.Forms.DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold)
            }, 2, 0);

            tblDynamicSources.Controls.Add(new System.Windows.Forms.Label
            {
                Text = "Expected",
                Dock = System.Windows.Forms.DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold)
            }, 3, 0);

            tblDynamicSources.Controls.Add(new System.Windows.Forms.Label
            {
                Text = "Action",
                Dock = System.Windows.Forms.DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold)
            }, 4, 0);

            //
            // DataCenterForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(900, 600);
            this.Controls.Add(tblDynamicSources);
            this.Name = "DataCenterForm";
            this.Text = "Data Center";

            // Initialize fixed controls (optional)
            InitializeFixedControls();
        }

        private void InitializeFixedControls()
        {
            // You can remove this entire method later once all data sources
            // are converted to IDataSourceStatus implementations.

            lblSolarStatus = new System.Windows.Forms.Label();
            lblSolarLast = new System.Windows.Forms.Label();
            lblSolarExpected = new System.Windows.Forms.Label();
            btnSolarImport = new System.Windows.Forms.Button();

            lblBillStatus = new System.Windows.Forms.Label();
            lblBillLast = new System.Windows.Forms.Label();
            lblBillExpected = new System.Windows.Forms.Label();
            btnBillImport = new System.Windows.Forms.Button();

            lblEvCircuitStatus = new System.Windows.Forms.Label();
            lblEvCircuitLast = new System.Windows.Forms.Label();
            btnEvCircuitRecalc = new System.Windows.Forms.Button();

            lblEvCommercialStatus = new System.Windows.Forms.Label();
            lblEvCommercialLast = new System.Windows.Forms.Label();
            btnEvCommercialImport = new System.Windows.Forms.Button();

            lblAttrStatus = new System.Windows.Forms.Label();
            lblAttrLast = new System.Windows.Forms.Label();
            btnAttrRecalc = new System.Windows.Forms.Button();

            // These controls are not added to the form anymore.
            // They remain here only to avoid breaking your existing code.
        }
    }
}
