using THMS.Domain.Assets;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Controls
{
    internal sealed class ValuationSourceDialog : Form
    {
        private readonly AssetOrchestrator _orchestrator;
        private readonly ValuationSource _source;
        private readonly TextBox _name = new() { Name = "SourceName" };
        private readonly TextBox _website = new() { Name = "Website" };
        private readonly CheckBox _property = new() { Text = "Property", AutoSize = true };
        private readonly CheckBox _vehicles = new() { Text = "Vehicles", AutoSize = true };
        private readonly CheckBox _personal = new() { Text = "Personal property", AutoSize = true };

        public ValuationSourceDialog(AssetOrchestrator orchestrator, ValuationSource? existing)
        {
            _orchestrator = orchestrator;
            _source = existing is null
                ? new ValuationSource()
                : new ValuationSource
                {
                    Id = existing.Id,
                    Name = existing.Name,
                    WebsiteUrl = existing.WebsiteUrl,
                    Kinds = existing.Kinds,
                    CreatedOn = existing.CreatedOn
                };

            Text = existing is null ? "Add Valuation Source" : "Edit Valuation Source";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = false;
            MaximizeBox = true;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Segoe UI", 9F);
            ClientSize = new Size(720, 420);
            MinimumSize = new Size(680, 400);

            _name.Text = _source.Name;
            _website.Text = _source.WebsiteUrl;
            _property.Checked = _source.Kinds.HasFlag(AppraisedAssetKind.Property);
            _vehicles.Checked = _source.Kinds.HasFlag(AppraisedAssetKind.Vehicle);
            _personal.Checked = _source.Kinds.HasFlag(AppraisedAssetKind.Personal);

            var kinds = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                Padding = new Padding(0, 4, 0, 0)
            };
            kinds.Controls.AddRange([_property, _vehicles, _personal]);

            var hint = new Label
            {
                Text = "The website is where you view or request the valuation.",
                Dock = DockStyle.Fill,
                AutoSize = false,
                TextAlign = ContentAlignment.TopLeft,
                UseMnemonic = false,
                Margin = new Padding(0, 8, 0, 0)
            };

            var stack = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4,
                Padding = new Padding(20, 16, 20, 8)
            };
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            AddRow(stack, 0, "Name", _name, 44);
            AddRow(stack, 1, "Website", _website, 44);
            AddRow(stack, 2, "Appraises", kinds, 76);
            stack.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            stack.Controls.Add(hint, 1, 3);

            var cancel = new ThmsButton { Text = "Cancel", DialogResult = DialogResult.Cancel };
            var save = new ThmsButton { Text = "Save" };
            save.Click += (_, _) => OnSave();
            AcceptButton = save;
            CancelButton = cancel;
            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = false,
                Padding = new Padding(16, 8, 16, 16)
            };
            buttons.Height = Math.Max(save.PreferredSize.Height, cancel.PreferredSize.Height) + buttons.Padding.Vertical;
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(save);

            Controls.Add(stack);
            Controls.Add(buttons);
        }

        private void OnSave()
        {
            try
            {
                _source.Name = _name.Text;
                _source.WebsiteUrl = _website.Text;
                _source.Kinds = AppraisedAssetKind.None;
                if (_property.Checked)
                    _source.Kinds |= AppraisedAssetKind.Property;
                if (_vehicles.Checked)
                    _source.Kinds |= AppraisedAssetKind.Vehicle;
                if (_personal.Checked)
                    _source.Kinds |= AppraisedAssetKind.Personal;
                _orchestrator.SaveValuationSource(_source);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static void AddRow(TableLayoutPanel stack, int row, string caption, Control value, float height)
        {
            stack.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            stack.Controls.Add(new Label
            {
                Text = caption,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 0, 12, 0)
            }, 0, row);
            value.Dock = DockStyle.Fill;
            value.Margin = new Padding(0, 6, 0, 6);
            stack.Controls.Add(value, 1, row);
        }
    }
}
