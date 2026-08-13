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

        private System.Windows.Forms.Label lblHomeCircuitStatus;
        private System.Windows.Forms.Label lblHomeCircuitLast;
        private System.Windows.Forms.Button btnHomeCircuitRecalc;

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
            tblDynamicSources = new TableLayoutPanel();
            SuspendLayout();
            // 
            // tblDynamicSources
            // 
            tblDynamicSources.AutoSize = true;
            tblDynamicSources.CellBorderStyle = TableLayoutPanelCellBorderStyle.Single;
            tblDynamicSources.ColumnCount = 5;
            tblDynamicSources.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tblDynamicSources.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F));
            tblDynamicSources.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tblDynamicSources.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tblDynamicSources.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tblDynamicSources.Dock = DockStyle.Top;
            tblDynamicSources.Location = new Point(0, 0);
            tblDynamicSources.Margin = new Padding(5, 6, 5, 6);
            tblDynamicSources.Name = "tblDynamicSources";
            tblDynamicSources.RowCount = 1;
            tblDynamicSources.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tblDynamicSources.Size = new Size(1543, 22);
            tblDynamicSources.TabIndex = 0;
            // 
            // DataCenterForm
            // 
            AutoScaleDimensions = new SizeF(12F, 30F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(1543, 1200);
            Controls.Add(tblDynamicSources);
            Margin = new Padding(5, 6, 5, 6);
            Name = "DataCenterForm";
            Text = "Data Center";
            Load += OnLoadForm;
            ResumeLayout(false);
            PerformLayout();
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

            lblHomeCircuitStatus = new System.Windows.Forms.Label();
            lblHomeCircuitLast = new System.Windows.Forms.Label();
            btnHomeCircuitRecalc = new System.Windows.Forms.Button();

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
