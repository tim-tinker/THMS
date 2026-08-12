namespace THMS.UI.WinForms
{
    partial class DataManagerForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.ComboBox comboStores;
        private System.Windows.Forms.Panel panelHost;
        private System.Windows.Forms.Button btnClose;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.comboStores = new System.Windows.Forms.ComboBox();
            this.panelHost = new System.Windows.Forms.Panel();
            this.btnClose = new System.Windows.Forms.Button();
            this.SuspendLayout();

            // 
            // comboStores
            // 
            this.comboStores.Dock = System.Windows.Forms.DockStyle.Top;
            this.comboStores.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboStores.FormattingEnabled = true;
            this.comboStores.Location = new System.Drawing.Point(0, 0);
            this.comboStores.Margin = new System.Windows.Forms.Padding(10);
            this.comboStores.Name = "comboStores";
            this.comboStores.Size = new System.Drawing.Size(1100, 32);
            this.comboStores.TabIndex = 0;
            this.comboStores.SelectedIndexChanged += new System.EventHandler(this.comboStores_SelectedIndexChanged);

            // 
            // panelHost
            // 
            this.panelHost.AutoScroll = true;
            this.panelHost.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelHost.Location = new System.Drawing.Point(0, 32);
            this.panelHost.Margin = new System.Windows.Forms.Padding(10);
            this.panelHost.Name = "panelHost";
            this.panelHost.Padding = new System.Windows.Forms.Padding(10);
            this.panelHost.Size = new System.Drawing.Size(1100, 728);
            this.panelHost.TabIndex = 1;

            // 
            // btnClose
            // 
            this.btnClose.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.btnClose.Location = new System.Drawing.Point(0, 760);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(1100, 40);
            this.btnClose.TabIndex = 2;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.OnClickClose);

            // 
            // DataManagerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1100, 800);
            this.Controls.Add(this.panelHost);
            this.Controls.Add(this.comboStores);
            this.Controls.Add(this.btnClose);
            this.MinimumSize = new System.Drawing.Size(900, 600);
            this.Name = "DataManagerForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Data Manager";
            this.ResumeLayout(false);
        }

        #endregion
    }
}
