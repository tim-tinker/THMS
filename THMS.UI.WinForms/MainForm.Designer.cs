namespace THMS.UI
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Panel navigationPanel;
        private System.Windows.Forms.Button _btnFinance;
        private System.Windows.Forms.Button _btnVehicles;
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
            navigationPanel = new Panel();
            _btnFinance = new Button();
            _btnTransportation = new Button();
            _btnEnergy = new Button();
            _btnVehicles = new Button();
            dashboardHostPanel = new Panel();
            navigationPanel.SuspendLayout();
            SuspendLayout();
            // 
            // navigationPanel
            // 
            navigationPanel.BackColor = Color.LightGray;
            navigationPanel.Controls.Add(_btnFinance);
            navigationPanel.Controls.Add(_btnTransportation);
            navigationPanel.Controls.Add(_btnEnergy);
            navigationPanel.Controls.Add(_btnVehicles);
            navigationPanel.Dock = DockStyle.Left;
            navigationPanel.Location = new Point(0, 0);
            navigationPanel.Name = "navigationPanel";
            navigationPanel.Size = new Size(180, 600);
            navigationPanel.TabIndex = 1;
            // 
            // _btnFinance
            // 
            _btnFinance.Location = new Point(10, 20);
            _btnFinance.Name = "_btnFinance";
            _btnFinance.Size = new Size(160, 40);
            _btnFinance.TabIndex = 0;
            _btnFinance.Text = "Finance";
            _btnFinance.UseVisualStyleBackColor = true;
            _btnFinance.Click += btnFinance_Click;
            // 
            // _btnTransportation
            // 
            _btnTransportation.Location = new Point(10, 162);
            _btnTransportation.Name = "_btnTransportation";
            _btnTransportation.Size = new Size(160, 40);
            _btnTransportation.TabIndex = 1;
            _btnTransportation.Text = "Transportation";
            _btnTransportation.UseVisualStyleBackColor = true;
            _btnTransportation.Click += btnTransportation_Click;
            // 
            // _btnEnergy
            // 
            _btnEnergy.Location = new Point(10, 116);
            _btnEnergy.Name = "_btnEnergy";
            _btnEnergy.Size = new Size(160, 40);
            _btnEnergy.TabIndex = 1;
            _btnEnergy.Text = "Energy";
            _btnEnergy.UseVisualStyleBackColor = true;
            _btnEnergy.Click += btnEnergy_Click;
            // 
            // _btnVehicles
            // 
            _btnVehicles.Location = new Point(10, 70);
            _btnVehicles.Name = "_btnVehicles";
            _btnVehicles.Size = new Size(160, 40);
            _btnVehicles.TabIndex = 1;
            _btnVehicles.Text = "Vehicles";
            _btnVehicles.UseVisualStyleBackColor = true;
            _btnVehicles.Click += OnClickVehicles;
            // 
            // dashboardHostPanel
            // 
            dashboardHostPanel.BackColor = Color.White;
            dashboardHostPanel.Dock = DockStyle.Fill;
            dashboardHostPanel.Location = new Point(180, 0);
            dashboardHostPanel.Name = "dashboardHostPanel";
            dashboardHostPanel.Size = new Size(900, 600);
            dashboardHostPanel.TabIndex = 0;
            // 
            // MainForm
            // 
            ClientSize = new Size(1080, 600);
            Controls.Add(dashboardHostPanel);
            Controls.Add(navigationPanel);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "THMS Dashboard";
            navigationPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Button _btnTransportation;
        private Button _btnEnergy;
    }
}
