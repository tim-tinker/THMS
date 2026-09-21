using THMS.Logic.Orchestrators.Finance;
using THMS.Logic.ViewModels.Finance;

namespace THMS.UI.WinForms.Controls
{
    public sealed class MatchImportedDialog : Form
    {
        public const int InitialDays = 5;
        public const int ExpandDays = 7;

        private readonly BillRow _bill;
        private readonly Guid? _recommendedImportedId;
        private readonly List<ImportedTransactionView> _all;
        private readonly ListBox _choices = new();
        private readonly Label _description = new();
        private readonly ThmsButton _match = new() { Name = "match", Text = "Match Selected" };
        private readonly ThmsButton _cancel = new() { Name = "cancel", Text = "Cancel", DialogResult = DialogResult.Cancel };
        private readonly ThmsButton _showMore = new() { Text = "Show more" };
        private int _days = InitialDays;

        public Guid? SelectedImportedId { get; private set; }

        public MatchImportedDialog(
            BillRow bill,
            IReadOnlyList<ImportedTransactionView> imported,
            Guid? recommendedImportedId = null)
        {
            _bill = bill;
            _all = imported.ToList();
            _recommendedImportedId = recommendedImportedId;

            Text = "Match Import";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = false;
            MaximizeBox = true;
            ShowInTaskbar = false;
            MinimumSize = new Size(920, 560);
            Size = new Size(980, 620);
            Padding = new Padding(8);

            var header = BuildHeader();
            var buttons = BuildButtons();

            _choices.Dock = DockStyle.Fill;
            _choices.IntegralHeight = false;
            _choices.DrawMode = DrawMode.OwnerDrawVariable;
            _choices.MeasureItem += OnMeasureChoice;
            _choices.DrawItem += OnDrawChoice;
            _choices.DoubleClick += (_, _) => AcceptMatch();

            Controls.Add(_choices);
            Controls.Add(buttons);
            Controls.Add(header);
            AcceptButton = _match;
            CancelButton = _cancel;

            Shown += (_, _) => ReloadChoices();
        }

        private Control BuildHeader()
        {
            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                Padding = new Padding(12, 8, 12, 12)
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            header.Controls.Add(Heading("Bill"));
            header.Controls.Add(Body(_bill.DueDate.ToString("d")));
            _description.AutoSize = true;
            _description.Margin = new Padding(0, 8, 0, 0);
            _description.Text = string.IsNullOrWhiteSpace(_bill.Notes) ? "(no description)" : _bill.Notes;
            header.Controls.Add(_description);
            var amount = Heading(_bill.Amount.ToString("c2"));
            amount.Margin = new Padding(0, 8, 0, 0);
            header.Controls.Add(amount);
            var hint = Body("Select the imported transaction that fulfills this bill.");
            hint.Margin = new Padding(0, 16, 0, 8);
            header.Controls.Add(hint);
            return header;
        }

        private Control BuildButtons()
        {
            var bottom = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Bottom,
                ColumnCount = 2,
                Padding = new Padding(12, 12, 12, 16)
            };
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            var left = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                Margin = new Padding(0, 0, 12, 0),
                WrapContents = false
            };
            _showMore.Click += (_, _) =>
            {
                _days += ExpandDays;
                ReloadChoices();
            };
            left.Controls.Add(_showMore);

            var right = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };
            _match.Click += (_, _) => AcceptMatch();
            right.Controls.Add(_cancel);
            right.Controls.Add(_match);

            bottom.Controls.Add(left, 0, 0);
            bottom.Controls.Add(right, 1, 0);
            return bottom;
        }

        private void ReloadChoices()
        {
            var visible = _all
                .Where(row => Math.Abs((row.Date.Date - _bill.DueDate.Date).TotalDays) <= _days)
                .OrderBy(row => row.Date)
                .ThenBy(row => row.Description)
                .ToList();
            var hidden = _all.Count - visible.Count;

            _choices.BeginUpdate();
            _choices.Items.Clear();
            foreach (var row in visible)
                _choices.Items.Add(row);
            _choices.EndUpdate();

            var selectedId = _recommendedImportedId ?? visible.FirstOrDefault()?.Id;
            if (selectedId is Guid id)
            {
                for (var i = 0; i < _choices.Items.Count; i++)
                {
                    if (_choices.Items[i] is ImportedTransactionView row && row.Id == id)
                    {
                        _choices.SelectedIndex = i;
                        break;
                    }
                }
            }

            _showMore.Enabled = hidden > 0;
            _showMore.Text = hidden > 0
                ? $"Show more ({hidden} hidden)"
                : "Show more";
        }

        private void OnMeasureChoice(object? sender, MeasureItemEventArgs e)
        {
            e.ItemHeight = (Font.Height * 2) + 14;
        }

        private void OnDrawChoice(object? sender, DrawItemEventArgs e)
        {
            e.DrawBackground();
            if (e.Index < 0 || e.Index >= _choices.Items.Count)
                return;
            if (_choices.Items[e.Index] is not ImportedTransactionView row)
                return;

            var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            var titleColor = selected ? e.ForeColor : ForeColor;
            var detailColor = selected ? e.ForeColor : SystemColors.GrayText;
            var bounds = Rectangle.Inflate(e.Bounds, -8, -4);
            var titleBounds = new Rectangle(bounds.X, bounds.Y, bounds.Width, Font.Height + 2);
            var detailBounds = new Rectangle(bounds.X, titleBounds.Bottom + 2, bounds.Width, Font.Height + 2);
            TextRenderer.DrawText(
                e.Graphics,
                $"{row.Date:d}  {row.Amount:c2}  ({row.Status})",
                Font,
                titleBounds,
                titleColor,
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            TextRenderer.DrawText(
                e.Graphics,
                string.IsNullOrWhiteSpace(row.Description) ? "(no description)" : row.Description,
                Font,
                detailBounds,
                detailColor,
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            e.DrawFocusRectangle();
        }

        private void AcceptMatch()
        {
            if (_choices.SelectedItem is not ImportedTransactionView row)
                return;
            SelectedImportedId = row.Id;
            DialogResult = DialogResult.OK;
            Close();
        }

        private static Label Heading(string text) =>
            new()
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Margin = new Padding(0),
                Text = text
            };

        private static Label Body(string text) =>
            new()
            {
                AutoSize = true,
                Margin = new Padding(0, 4, 0, 0),
                Text = text
            };
    }
}
