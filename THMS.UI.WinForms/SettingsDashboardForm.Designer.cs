namespace THMS.UI.WinForms
{
    partial class SettingsDashboardForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TableLayoutPanel mainLayout;
        private System.Windows.Forms.GroupBox settingsGroup;
        private System.Windows.Forms.CheckBox chkDarkMode;
        private System.Windows.Forms.CheckBox chkAutoSave;
        private System.Windows.Forms.CheckBox chkShowTooltips;

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
            this.settingsGroup = new System.Windows.Forms.GroupBox();
            this.chkDarkMode = new System.Windows.Forms.CheckBox();
            this.chkAutoSave = new System.Windows.Forms.CheckBox();
            this.chkShowTooltips = new System.Windows.Forms.CheckBox();

            this.mainLayout.SuspendLayout();
            this.settingsGroup.SuspendLayout();
            this.SuspendLayout();

            // mainLayout
            this.mainLayout.ColumnCount = 1;
            this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainLayout.RowCount = 1;
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainLayout.Controls.Add(this.settingsGroup, 0, 0);

            // settingsGroup
            this.settingsGroup.Dock = System.Windows.Forms.DockStyle.Fill;
            this.settingsGroup.Text = "Application Settings";
            this.settingsGroup.Controls.Add(this.chkDarkMode);
            this.settingsGroup.Controls.Add(this.chkAutoSave);
            this.settingsGroup.Controls.Add(this.chkShowTooltips);

            // chkDarkMode
            this.chkDarkMode.AutoSize = true;
            this.chkDarkMode.Location = new System.Drawing.Point(20, 40);
            this.chkDarkMode.Text = "Enable Dark Mode";
            chkDarkMode.CheckedChanged += OnDarkModeCheckChanged;

            // chkAutoSave
            this.chkAutoSave.AutoSize = true;
            this.chkAutoSave.Location = new System.Drawing.Point(20, 70);
            this.chkAutoSave.Text = "Enable Auto-Save";
            chkAutoSave.CheckedChanged += OnAutoSaveCheckChanged;

            // chkShowTooltips
            this.chkShowTooltips.AutoSize = true;
            this.chkShowTooltips.Location = new System.Drawing.Point(20, 100);
            this.chkShowTooltips.Text = "Show Tooltips";
            chkShowTooltips.CheckedChanged += OnShowTooltipsCheckChanged;

            // SettingsDashboardForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.mainLayout);
            this.Name = "SettingsDashboardForm";
            this.Text = "Settings";

            this.mainLayout.ResumeLayout(false);
            this.settingsGroup.ResumeLayout(false);
            this.settingsGroup.PerformLayout();
            this.ResumeLayout(false);
        }
    }
}
