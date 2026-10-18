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

        public const string AllLabel = "All";

        /// <summary>
        /// One click applies. All clears the filter.
        /// </summary>
        public static void ShowSingle(
            Control owner,
            Rectangle screenAnchor,
            IEnumerable<string> allValues,
            string? selected,
            Action<string?> applied)
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

            var list = new ActivatingListBox
            {
                Width = Math.Max(220, screenAnchor.Width),
                Height = Math.Clamp((unique.Count + 1) * 22, 44, 320)
            };
            list.Items.Add(AllLabel);
            foreach (var value in unique)
                list.Items.Add(value);

            var selectedIndex = 0;
            if (selected is not null)
            {
                var match = unique.FindIndex(value =>
                    string.Equals(value, selected, StringComparison.CurrentCultureIgnoreCase));
                if (match >= 0)
                    selectedIndex = match + 1;
            }

            list.SelectedIndex = selectedIndex;

            var drop = new FilterDropDown
            {
                Padding = Padding.Empty,
                AutoClose = true
            };
            var host = new ToolStripControlHost(list)
            {
                AutoSize = false,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                Size = list.Size
            };
            drop.Items.Add(host);

            void Apply(int index)
            {
                if (index < 0 || index >= list.Items.Count)
                    return;

                string? chosen = index == 0 ? null : unique[index - 1];
                drop.Close(ToolStripDropDownCloseReason.ItemClicked);
                applied(chosen);
                var form = owner.FindForm();
                if (form is { IsDisposed: false } && Form.ActiveForm != form)
                    form.Activate();
            }

            list.ItemChosen += Apply;

            // Own the popup to this window. An unowned popup that closes on click
            // hands the foreground to whatever application is behind it.
            drop.Show(owner, owner.PointToClient(new Point(screenAnchor.Left, screenAnchor.Bottom)));
        }

        /// <summary>
        /// Stays in front of the owner without becoming the foreground window.
        /// </summary>
        private sealed class FilterDropDown : ToolStripDropDown
        {
            private const int WsExNoActivate = 0x08000000;

            protected override CreateParams CreateParams
            {
                get
                {
                    var parameters = base.CreateParams;
                    parameters.ExStyle |= WsExNoActivate;
                    return parameters;
                }
            }
        }

        /// <summary>
        /// A list hosted in a dropdown does not receive the click that activates it.
        /// Accept the click without activating the popup, so the main window stays in front.
        /// </summary>
        private sealed class ActivatingListBox : ListBox
        {
            private const int WmMouseActivate = 0x0021;
            private const int MaNoActivate = 3;

            public event Action<int>? ItemChosen;

            private bool _chose;

            public ActivatingListBox()
            {
                BorderStyle = BorderStyle.None;
                IntegralHeight = false;
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WmMouseActivate)
                {
                    m.Result = (IntPtr)MaNoActivate;
                    return;
                }

                base.WndProc(ref m);
            }

            protected override void OnMouseUp(MouseEventArgs e)
            {
                base.OnMouseUp(e);
                if (_chose || e.Button != MouseButtons.Left)
                    return;

                var index = IndexFromPoint(e.Location);
                if (index < 0)
                    return;

                _chose = true;
                ItemChosen?.Invoke(index);
            }
        }
    }
}
