namespace THMS.UI.WinForms
{
    partial class ImporterForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label lblFile;
        private System.Windows.Forms.TextBox txtFilePath;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.Label lblImporterType;
        private System.Windows.Forms.ComboBox comboImporterType;
        private System.Windows.Forms.Button btnImport;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.ProgressBar progressBar;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblFile = new System.Windows.Forms.Label();
            this.txtFilePath = new System.Windows.Forms.TextBox();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.lblImporterType = new System.Windows.Forms.Label();
            this.comboImporterType = new System.Windows.Forms.ComboBox();
            this.btnImport = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.SuspendLayout();

            // lblFile
            this.lblFile.AutoSize = true;
            this.lblFile.Location = new System.Drawing.Point(20, 20);
            this.lblFile.Name = "lblFile";
            this.lblFile.Size = new System.Drawing.Size(58, 15);
            this.lblFile.Text = "CSV File:";

            // txtFilePath
            this.txtFilePath.Location = new System.Drawing.Point(20, 40);
            this.txtFilePath.Name = "txtFilePath";
            this.txtFilePath.Size = new System.Drawing.Size(400, 23);

            // btnBrowse
            this.btnBrowse.Location = new System.Drawing.Point(430, 40);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(75, 23);
            this.btnBrowse.Text = "Browse...";
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);

            // lblImporterType
            this.lblImporterType.AutoSize = true;
            this.lblImporterType.Location = new System.Drawing.Point(20, 80);
            this.lblImporterType.Name = "lblImporterType";
            this.lblImporterType.Size = new System.Drawing.Size(89, 15);
            this.lblImporterType.Text = "Importer Type:";

            // comboImporterType
            this.comboImporterType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboImporterType.Location = new System.Drawing.Point(20, 100);
            this.comboImporterType.Name = "comboImporterType";
            this.comboImporterType.Size = new System.Drawing.Size(200, 23);

            // btnImport
            this.btnImport.Location = new System.Drawing.Point(20, 140);
            this.btnImport.Name = "btnImport";
            this.btnImport.Size = new System.Drawing.Size(100, 30);
            this.btnImport.Text = "Import";
            this.btnImport.Click += new System.EventHandler(this.btnImport_Click);

            // lblStatus
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(20, 190);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(42, 15);
            this.lblStatus.Text = "Ready";

            // progressBar
            this.progressBar.Location = new System.Drawing.Point(20, 210);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(485, 10);
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBar.Visible = false;

            // ImporterForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.ClientSize = new System.Drawing.Size(530, 250);
            this.Controls.Add(this.lblFile);
            this.Controls.Add(this.txtFilePath);
            this.Controls.Add(this.btnBrowse);
            this.Controls.Add(this.lblImporterType);
            this.Controls.Add(this.comboImporterType);
            this.Controls.Add(this.btnImport);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.progressBar);
            this.Name = "ImporterForm";
            this.Text = "Energy Data Importer";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
