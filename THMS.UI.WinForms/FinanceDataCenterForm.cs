namespace THMS.UI.WinForms
{
    public partial class FinanceDataCenterForm : BaseEmbeddedForm
    {
        private static readonly Font TabHeaderFont = new("Segoe UI", 12F, FontStyle.Bold);
        private static readonly Color IdleTabBack = Color.FromArgb(92, 45, 145);
        private static readonly Color IdleTabFore = Color.White;
        private static readonly Color ActiveTabBack = Color.FromArgb(255, 213, 0);
        private static readonly Color ActiveTabFore = Color.FromArgb(48, 20, 80);

        public FinanceDataCenterForm()
        {
            InitializeComponent();
            ConfigureTabs();
        }

        private void ConfigureTabs()
        {
            tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabs.SizeMode = TabSizeMode.Fixed;
            tabs.ItemSize = new Size(LogicalToDeviceUnits(180), LogicalToDeviceUnits(42));
            tabs.Padding = new Point(18, 8);
            tabs.DrawItem += OnDrawTab;
            tabs.SelectedIndexChanged += (_, _) => tabs.Invalidate();

            var pageTint = Color.FromArgb(255, 252, 235);
            tabAccounts.BackColor = pageTint;
            tabTransactions.BackColor = pageTint;
            tabDiagnostics.BackColor = pageTint;
        }

        private void OnDrawTab(object? sender, DrawItemEventArgs e)
        {
            var selected = e.Index == tabs.SelectedIndex;
            var bounds = e.Bounds;
            if (selected)
                bounds.Inflate(0, 2);

            using var back = new SolidBrush(selected ? ActiveTabBack : IdleTabBack);
            e.Graphics.FillRectangle(back, bounds);

            TextRenderer.DrawText(
                e.Graphics,
                tabs.TabPages[e.Index].Text,
                TabHeaderFont,
                bounds,
                selected ? ActiveTabFore : IdleTabFore,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }
}
