namespace THMS.UI
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Panel navigationPanel;
        private System.Windows.Forms.Button btnTransportation;
        private System.Windows.Forms.Button btnEnergy;
        private System.Windows.Forms.Panel dashboardHostPanel;

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
            this.navigationPanel = new System.Windows.Forms.Panel();
            this.btnTransportation = new System.Windows.Forms.Button();
            this.btnEnergy = new System.Windows.Forms.Button();
            this.dashboardHostPanel = new System.Windows.Forms.Panel();

            this.navigationPanel.SuspendLayout();
            this.SuspendLayout();

            // 
            // navigationPanel
            // 
            this.navigationPanel.BackColor = System.Drawing.Color.LightGray;
            this.navigationPanel.Dock = System.Windows.Forms.DockStyle.Left;
            this.navigationPanel.Width = 180;
            this.navigationPanel.Controls.Add(this.btnTransportation);
            this.navigationPanel.Controls.Add(this.btnEnergy);

            // 
            // btnTransportation
            // 
            this.btnTransportation.Location = new System.Drawing.Point(10, 20);
            this.btnTransportation.Name = "btnTransportation";
            this.btnTransportation.Size = new System.Drawing.Size(160, 40);
            this.btnTransportation.Text = "Transportation";
            this.btnTransportation.UseVisualStyleBackColor = true;
            this.btnTransportation.Click += new System.EventHandler(this.btnTransportation_Click);

            // 
            // btnEnergy
            // 
            this.btnEnergy.Location = new System.Drawing.Point(10, 70);
            this.btnEnergy.Name = "btnEnergy";
            this.btnEnergy.Size = new System.Drawing.Size(160, 40);
            this.btnEnergy.Text = "Energy";
            this.btnEnergy.UseVisualStyleBackColor = true;
            this.btnEnergy.Click += new System.EventHandler(this.btnEnergy_Click);

            // 
            // dashboardHostPanel
            // 
            this.dashboardHostPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dashboardHostPanel.Location = new System.Drawing.Point(180, 0);
            this.dashboardHostPanel.Name = "dashboardHostPanel";
            this.dashboardHostPanel.Size = new System.Drawing.Size(900, 600);
            this.dashboardHostPanel.BackColor = System.Drawing.Color.White;

            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(1080, 600);
            this.Controls.Add(this.dashboardHostPanel);
            this.Controls.Add(this.navigationPanel);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "THMS Dashboard";

            this.navigationPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion
    }
}
