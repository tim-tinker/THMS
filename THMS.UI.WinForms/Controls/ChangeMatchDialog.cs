using THMS.Logic.Orchestrators.Finance;

namespace THMS.UI.WinForms.Controls
{
    public sealed class ChangeMatchDialog : Form
    {
        public const int InitialDays = 5;
        public const int ExpandDays = 7;

        private readonly ImportedTransactionView _imported;
        private readonly List<ExpectedChoice> _all;
        private readonly ListBox _choices = new();
        private readonly Label _description = new();
        private readonly ThmsButton _match = new() { Name = "match", Text = "Match Selected" };
        private readonly ThmsButton _cancel = new() { Name = "cancel", Text = "Cancel", DialogResult = DialogResult.Cancel };
        private readonly ThmsButton _showMore = new() { Text = "Show more" };
        private int _days = InitialDays;

        public Guid? SelectedExpectedId { get; private set; }
        public bool TreatAsNew { get; private set; }

        public ChangeMatchDialog(
            ImportedTransactionView imported,
            IReadOnlyList<ExpectedChoice> expected)
        {
            _imported = imported;
            _all = expected.ToList();

            Text = "Change Match";
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

            Resize += (_, _) => UpdateWrapWidth();
            Shown += (_, _) =>
            {
                UpdateWrapWidth();
                ReloadChoices(selectRecommended: true);
            };
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

            header.Controls.Add(Heading("Imported"));
            header.Controls.Add(Body(_imported.Date.ToString("d")));
            _description.AutoSize = true;
            _description.Margin = new Padding(0, 8, 0, 0);
            _description.Text = string.IsNullOrWhiteSpace(_imported.Description)
                ? "(no description)"
                : _imported.Description;
            header.Controls.Add(_description);
            var amount = Heading(_imported.Amount.ToString("c2"));
            amount.Margin = new Padding(0, 8, 0, 0);
            header.Controls.Add(amount);

            var hint = Body("Select a pending transaction, or treat this import as new.");
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
            _showMore.Click += (_, _) => ShowMore();
            left.Controls.Add(_showMore);

            var right = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };
            var cancel = _cancel;
            var match = _match;
            var asNew = new ThmsButton { Text = "Treat as New" };
            match.Click += (_, _) => AcceptMatch();
            asNew.Click += (_, _) =>
            {
                TreatAsNew = true;
                SelectedExpectedId = null;
                DialogResult = DialogResult.OK;
                Close();
            };
            right.Controls.Add(cancel);
            right.Controls.Add(match);
            right.Controls.Add(asNew);

            bottom.Controls.Add(left, 0, 0);
            bottom.Controls.Add(right, 1, 0);
            return bottom;
        }

        private void ShowMore()
        {
            _days += ExpandDays;
            ReloadChoices(selectRecommended: false);
        }

        private void ReloadChoices(bool selectRecommended)
        {
            var visible = VisibleChoices().ToList();
            var hidden = _all.Count - visible.Count;
            var selectedId = selectRecommended
                ? _imported.RecommendedExpectedId
                : (_choices.SelectedItem as ExpectedChoice)?.Id;

            _choices.BeginUpdate();
            _choices.Items.Clear();
            foreach (var choice in visible)
                _choices.Items.Add(choice);
            _choices.EndUpdate();

            if (selectedId is Guid id)
            {
                for (var i = 0; i < _choices.Items.Count; i++)
                {
                    if (_choices.Items[i] is ExpectedChoice choice && choice.Id == id)
                    {
                        _choices.SelectedIndex = i;
                        break;
                    }
                }
            }
            else if (_choices.Items.Count > 0)
                _choices.SelectedIndex = 0;

            _showMore.Enabled = hidden > 0;
            _showMore.Text = hidden > 0
                ? $"Show more ({hidden} hidden)"
                : "Show more";
        }

        private IEnumerable<ExpectedChoice> VisibleChoices() =>
            _all.Where(choice => Math.Abs((choice.Date.Date - _imported.Date.Date).TotalDays) <= _days)
                .OrderBy(choice => choice.Date)
                .ThenBy(choice => choice.Description);

        private void UpdateWrapWidth()
        {
            var width = Math.Max(120, ClientSize.Width - 48);
            _description.MaximumSize = new Size(width, 0);
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
            if (_choices.Items[e.Index] is not ExpectedChoice choice)
                return;

            var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            var titleColor = selected ? e.ForeColor : ForeColor;
            var detailColor = selected ? e.ForeColor : SystemColors.GrayText;
            var bounds = Rectangle.Inflate(e.Bounds, -8, -4);
            var titleBounds = new Rectangle(bounds.X, bounds.Y, bounds.Width, Font.Height + 2);
            var detailBounds = new Rectangle(bounds.X, titleBounds.Bottom + 2, bounds.Width, Font.Height + 2);
            TextRenderer.DrawText(e.Graphics, choice.TitleLine, Font, titleBounds, titleColor,
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            TextRenderer.DrawText(e.Graphics, choice.DetailLine, Font, detailBounds, detailColor,
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            e.DrawFocusRectangle();
        }

        private void AcceptMatch()
        {
            if (_choices.SelectedItem is not ExpectedChoice choice)
                return;
            SelectedExpectedId = choice.Id;
            TreatAsNew = false;
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
