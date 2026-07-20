#nullable enable

using System.Windows.Forms.DataVisualization.Charting;

namespace TPFS.UI.WinForms;

partial class MainForm
{
    private System.ComponentModel.IContainer? components = null;
    private SplitContainer splitContainer = null!;
    private Panel contentPanel = null!;
    private Label appTitleLabel = null!;
    private Button btnTransportation = null!;
    private Panel navPanel;
    protected override void Dispose(bool disposing)
    {
        if (disposing && components is not null)
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        splitContainer = new SplitContainer();
        appTitleLabel = new Label();
        btnTransportation = new System.Windows.Forms.Button();
        contentPanel = new Panel();
        navPanel = new Panel();

        ((System.ComponentModel.ISupportInitialize)splitContainer).BeginInit();
        splitContainer.Panel1.SuspendLayout();
        splitContainer.Panel2.SuspendLayout();
        splitContainer.SuspendLayout();
        contentPanel.SuspendLayout();
        SuspendLayout();
        //
        // splitContainer
        //
        splitContainer.Dock = DockStyle.Fill;
        splitContainer.FixedPanel = FixedPanel.Panel1;
        splitContainer.Location = new Point(0, 0);
        splitContainer.Name = "splitContainer";
        splitContainer.Panel1.BackColor = Color.FromArgb(243, 243, 243);
        splitContainer.Panel1.Controls.Add(navPanel);
        splitContainer.Panel1.Controls.Add(appTitleLabel);
        splitContainer.Panel1MinSize = 180;
        splitContainer.Panel2.Controls.Add(contentPanel);
        splitContainer.Size = new Size(1100, 700);
        splitContainer.SplitterDistance = 220;
        splitContainer.TabIndex = 0;
        //
        // appTitleLabel
        //
        appTitleLabel.Dock = DockStyle.Top;
        appTitleLabel.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold);
        appTitleLabel.Location = new Point(0, 0);
        appTitleLabel.Name = "appTitleLabel";
        appTitleLabel.Padding = new Padding(12, 16, 12, 8);
        appTitleLabel.Size = new Size(220, 52);
        appTitleLabel.TabIndex = 0;
        appTitleLabel.Text = "TPFS";

        // navPanel
        this.navPanel.Dock = System.Windows.Forms.DockStyle.Left;
        this.navPanel.Width = 180;
        this.navPanel.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
        this.navPanel.Padding = new Padding(10);
        navPanel.Controls.Add(btnTransportation);

        //
        // btnTransportation
        //
        btnTransportation.Dock = DockStyle.Top;
        btnTransportation.FlatStyle = FlatStyle.Flat;
        btnTransportation.Font = new Font("Segoe UI", 10F);
        btnTransportation.Height = 40;
        btnTransportation.Name = "btnTransportation";
        btnTransportation.TabIndex = 1;
        btnTransportation.Text = "Transportation Dashboard";
        btnTransportation.UseVisualStyleBackColor = true;
        btnTransportation.Click += btnTransportation_Click;

        //
        // contentPanel
        //
        contentPanel.BackColor = SystemColors.Window;
        contentPanel.Dock = DockStyle.Fill;
        contentPanel.Location = new Point(0, 0);
        contentPanel.Name = "contentPanel";
        contentPanel.Size = new Size(876, 700);
        contentPanel.TabIndex = 0;
        //
        // MainForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1100, 700);
        Controls.Add(splitContainer);
        MinimumSize = new Size(800, 500);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Personal Finance System";
        contentPanel.ResumeLayout(false);
        contentPanel.PerformLayout();
        splitContainer.Panel1.ResumeLayout(false);
        splitContainer.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)splitContainer).EndInit();
        splitContainer.ResumeLayout(false);
        ResumeLayout(false);
    }
}
