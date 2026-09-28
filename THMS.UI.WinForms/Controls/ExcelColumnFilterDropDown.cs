namespace THMS.UI.WinForms.Controls
{
    internal static class ExcelColumnFilterDropDown
    {
        public const string BlankLabel = "(blank)";

        public static string Normalize(string? value) =>
            string.IsNullOrWhiteSpace(value) ? BlankLabel : value.Trim();

        public static void Show(
            Control owner,
            Rectangle screenAnchor,
            IEnumerable<string> allValues,
            IReadOnlySet<string>? selected,
            Action<HashSet<string>?> applied)
        {
            ArgumentNullException.ThrowIfNull(owner);
            ArgumentNullException.ThrowIfNull(allValues);
            ArgumentNullException.ThrowIfNull(applied);

            var unique = allValues
                .Select(Normalize)
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(value => value == BlankLabel ? 0 : 1)
                .ThenBy(value => value, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            var list = new CheckedListBox
            {
                CheckOnClick = true,
                IntegralHeight = false,
                BorderStyle = BorderStyle.None,
                Width = Math.Max(220, screenAnchor.Width),
                Height = Math.Clamp(28 + (unique.Count + 1) * 20, 80, 280)
            };
            list.Items.Add("(Select All)");
            foreach (var value in unique)
                list.Items.Add(value);

            var allOn = selected is null;
            list.SetItemChecked(0, allOn);
            for (var i = 0; i < unique.Count; i++)
                list.SetItemChecked(i + 1, allOn || selected!.Contains(unique[i]));

            var syncing = false;
            list.ItemCheck += (_, e) =>
            {
                if (syncing)
                    return;
                var check = e.NewValue == CheckState.Checked;
                var index = e.Index;
                owner.BeginInvoke(() =>
                {
                    syncing = true;
                    try
                    {
                        if (index == 0)
                        {
                            for (var i = 1; i < list.Items.Count; i++)
                                list.SetItemChecked(i, check);
                            return;
                        }

                        var allChecked = true;
                        for (var i = 1; i < list.Items.Count; i++)
                        {
                            var on = i == index ? check : list.GetItemChecked(i);
                            if (!on)
                            {
                                allChecked = false;
                                break;
                            }
                        }

                        list.SetItemChecked(0, allChecked);
                    }
                    finally
                    {
                        syncing = false;
                    }
                });
            };

            var ok = new Button
            {
                Text = "OK",
                AutoSize = true,
                DialogResult = DialogResult.OK
            };
            var cancel = new Button
            {
                Text = "Cancel",
                AutoSize = true,
                DialogResult = DialogResult.Cancel
            };
            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(4),
                WrapContents = false
            };
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(ok);

            var panel = new Panel
            {
                Width = list.Width,
                Height = list.Height + 44,
                Padding = new Padding(0)
            };
            list.Dock = DockStyle.Fill;
            panel.Controls.Add(list);
            panel.Controls.Add(buttons);

            var drop = new ToolStripDropDown
            {
                Padding = Padding.Empty,
                AutoClose = true
            };
            var host = new ToolStripControlHost(panel)
            {
                AutoSize = false,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                Size = panel.Size
            };
            drop.Items.Add(host);

            ok.Click += (_, _) =>
            {
                var chosen = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
                for (var i = 0; i < unique.Count; i++)
                {
                    if (list.GetItemChecked(i + 1))
                        chosen.Add(unique[i]);
                }

                drop.Close();
                applied(chosen.Count == unique.Count ? null : chosen);
            };
            cancel.Click += (_, _) => drop.Close();

            drop.Show(new Point(screenAnchor.Left, screenAnchor.Bottom));
        }
    }
}
