using System.Diagnostics;
using THMS.Domain.Assets;

namespace THMS.UI.WinForms.Controls
{
    /// <summary>
    /// Source editor for a valuation. The list stays a plain dropdown, because a combo
    /// item cannot host a hyperlink. When the chosen source has a website, its name is
    /// drawn over the text as the link that opens that site.
    /// </summary>
    internal sealed class ValuationSourceField : UserControl
    {
        private readonly IReadOnlyList<ValuationSource> _sources;
        private readonly ComboBox _names = new()
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDown,
            AutoCompleteMode = AutoCompleteMode.SuggestAppend,
            AutoCompleteSource = AutoCompleteSource.ListItems
        };
        private readonly LinkLabel _nameLink = new()
        {
            AutoSize = false,
            AutoEllipsis = true,
            Visible = false,
            TextAlign = ContentAlignment.MiddleLeft,
            LinkBehavior = LinkBehavior.HoverUnderline,
            BackColor = SystemColors.Window,
            LinkColor = DataGridViewUtil.LinkFore,
            ActiveLinkColor = DataGridViewUtil.LinkActiveFore,
            VisitedLinkColor = DataGridViewUtil.LinkFore,
            Padding = new Padding(1, 0, 0, 0)
        };
        private bool _editing;

        public ValuationSourceField(IEnumerable<ValuationSource> sources, string? current)
        {
            _sources = sources.OrderBy(source => source.Name, StringComparer.OrdinalIgnoreCase).ToList();
            _names.Items.AddRange(_sources.Select(source => source.Name).Cast<object>().ToArray());
            _names.Text = current ?? "";
            _names.TextChanged += (_, _) => UpdateLink();
            _names.Enter += (_, _) => BeginEdit();
            _names.Leave += (_, _) => EndEdit();
            _nameLink.Click += (_, _) => OpenWebsite();
            _nameLink.KeyPress += OnLinkKeyPress;
            _nameLink.KeyDown += (_, e) =>
            {
                if (e.KeyCode is not (Keys.Down or Keys.F4 or Keys.Up))
                    return;
                BeginEdit();
                _names.Focus();
                _names.DroppedDown = true;
                e.Handled = true;
            };

            Margin = Padding.Empty;
            Controls.Add(_names);
            Controls.Add(_nameLink);
            UpdateLink();
        }

        public string SourceName => _names.Text.Trim();

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            PlaceLink();
        }

        private void BeginEdit()
        {
            _editing = true;
            _nameLink.Visible = false;
            _names.TabStop = true;
        }

        private void EndEdit()
        {
            _editing = false;
            UpdateLink();
        }

        private void OnLinkKeyPress(object? sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar) && e.KeyChar != '\b')
                return;

            BeginEdit();
            _names.Focus();
            _names.Text = e.KeyChar == '\b' ? "" : e.KeyChar.ToString();
            _names.SelectionStart = _names.Text.Length;
            e.Handled = true;
        }

        private void UpdateLink()
        {
            var source = Selected();
            var show = !_editing && source?.Website is not null;
            _nameLink.Text = source?.Name ?? "";
            _nameLink.LinkVisited = false;
            _nameLink.Visible = show;
            _nameLink.TabStop = show;
            _names.TabStop = !show;
            if (show)
            {
                PlaceLink();
                _nameLink.BringToFront();
            }
        }

        private void PlaceLink()
        {
            if (!_nameLink.Visible)
                return;

            var button = SystemInformation.VerticalScrollBarWidth + LogicalToDeviceUnits(4);
            var left = _names.Left + LogicalToDeviceUnits(3);
            var top = _names.Top + LogicalToDeviceUnits(2);
            var width = Math.Max(0, _names.Width - button - LogicalToDeviceUnits(4));
            var height = Math.Max(0, _names.Height - LogicalToDeviceUnits(4));
            _nameLink.SetBounds(left, top, width, height);
        }

        private ValuationSource? Selected() =>
            _sources.FirstOrDefault(source => string.Equals(source.Name, _names.Text.Trim(), StringComparison.OrdinalIgnoreCase));

        private void OpenWebsite()
        {
            var website = Selected()?.Website;
            if (website is null)
                return;
            Process.Start(new ProcessStartInfo
            {
                FileName = website.ToString(),
                UseShellExecute = true
            });
        }
    }
}
