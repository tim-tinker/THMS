#nullable enable

namespace TPFS.UI.WinForms.Views.Home;

partial class HomeView
{
    private System.ComponentModel.IContainer? components = null;
    private Label titleLabel = null!;
    private Label welcomeLabel = null!;

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
        titleLabel = new Label();
        welcomeLabel = new Label();
        SuspendLayout();
        //
        // titleLabel
        //
        titleLabel.AutoSize = true;
        titleLabel.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold);
        titleLabel.Location = new Point(24, 24);
        titleLabel.Name = "titleLabel";
        titleLabel.Size = new Size(86, 32);
        titleLabel.TabIndex = 0;
        titleLabel.Text = "Home";
        //
        // welcomeLabel
        //
        welcomeLabel.AutoSize = true;
        welcomeLabel.Font = new Font("Segoe UI", 11F);
        welcomeLabel.Location = new Point(24, 72);
        welcomeLabel.Name = "welcomeLabel";
        welcomeLabel.Size = new Size(0, 20);
        welcomeLabel.TabIndex = 1;
        //
        // HomeView
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = SystemColors.Window;
        Controls.Add(welcomeLabel);
        Controls.Add(titleLabel);
        Name = "HomeView";
        Size = new Size(800, 450);
        ResumeLayout(false);
        PerformLayout();
    }
}
