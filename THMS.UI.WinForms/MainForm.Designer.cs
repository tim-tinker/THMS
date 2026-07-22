#nullable enable

namespace THMS.UI.WinForms;

partial class MainForm
{
    private System.ComponentModel.IContainer components;
    private Panel contentPanel = null!;
    private Panel navPanel = null!;
    private Button btnTransportation = null!;
    private Button btnEnergy = null!;
    private Button btnFinance = null!;
    private Button btnHousehold = null!;
    private Button btnSettings = null!;
    private System.Windows.Forms.ImageList navIcons;

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
        components = new System.ComponentModel.Container();

        navPanel = new Panel();
        contentPanel = new Panel();
        btnTransportation = new Button();
        btnEnergy = new Button();
        btnFinance = new Button();
        btnHousehold = new Button();
        btnSettings = new Button();

        navPanel.SuspendLayout();
        contentPanel.SuspendLayout();
        SuspendLayout();

        // navIcons
        this.navIcons = new System.Windows.Forms.ImageList(this.components);
        this.navIcons.ColorDepth = ColorDepth.Depth32Bit;
        this.navIcons.ImageSize = new Size(24, 24);
        this.navIcons.TransparentColor = Color.Transparent;
        this.navIcons.Images.Add("transportation", Properties.Resources.MoveUp);
        this.navIcons.Images.Add("energy", Properties.Resources.MoveUp);
        this.navIcons.Images.Add("finance", Properties.Resources.MoveUp);
        this.navIcons.Images.Add("household", Properties.Resources.MoveUp);
        this.navIcons.Images.Add("settings", Properties.Resources.MoveUp);

        //
        // navPanel
        //
        navPanel.BackColor = Color.FromArgb(32, 32, 32);
        navPanel.Controls.Add(btnSettings);
        navPanel.Controls.Add(btnHousehold);
        navPanel.Controls.Add(btnFinance);
        navPanel.Controls.Add(btnEnergy);
        navPanel.Controls.Add(btnTransportation);
        navPanel.Dock = DockStyle.Left;
        navPanel.Name = "navPanel";
        navPanel.Width = 220;
        //
        // Buttons are DockStyle.Top. WinForms docks highest z-order first,
        // so Controls.Add order is reverse of visual top-to-bottom order:
        // last added (Transportation) appears at the top.
        //
        ConfigureNavButton(btnTransportation, "Transportation", btnTransportation_Click, navIcons.Images["transportation"]);
        ConfigureNavButton(btnEnergy, "Energy", btnEnergy_Click, navIcons.Images["energy"]);
        ConfigureNavButton(btnFinance, "Finance", btnFinance_Click, navIcons.Images["finance"]);
        ConfigureNavButton(btnHousehold, "Household", btnHousehold_Click, navIcons.Images["household"]);
        ConfigureNavButton(btnSettings, "Settings", btnSettings_Click, navIcons.Images["settings"]);
        //
        // contentPanel
        //
        contentPanel.BackColor = SystemColors.Window;
        contentPanel.Dock = DockStyle.Fill;
        contentPanel.Name = "contentPanel";
        contentPanel.TabIndex = 0;

        //
        // MainForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1100, 700);
        // Fill first, then Left — so the nav panel is laid out correctly.
        Controls.Add(contentPanel);
        Controls.Add(navPanel);
        MinimumSize = new Size(800, 500);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Personal Finance System";

        navPanel.ResumeLayout(false);
        contentPanel.ResumeLayout(false);
        ResumeLayout(false);
    }

    private static void ConfigureNavButton(Button button, string text, EventHandler onClick, Image? image)
    {
        button.BackColor = Color.FromArgb(45, 45, 45);
        button.Dock = DockStyle.Top;
        button.FlatAppearance.BorderSize = 0;
        button.FlatStyle = FlatStyle.Flat;
        button.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        button.ForeColor = Color.White;
        button.Height = 48;
        button.Name = "btn" + text.Replace(" ", string.Empty);
        button.Image = image;
        button.ImageAlign = ContentAlignment.MiddleLeft;
        button.TextAlign = ContentAlignment.MiddleLeft;
        button.Padding = new Padding(50, 0, 0, 0);
        button.Text = text;
        button.TextAlign = ContentAlignment.MiddleLeft;
        button.UseVisualStyleBackColor = false;
        button.Click += onClick;
        button.MouseEnter += (_, _) => button.BackColor = Color.FromArgb(60, 60, 60);
        button.MouseLeave += (_, _) => button.BackColor = Color.FromArgb(45, 45, 45);
    }
}
