using THMS.Domain.Assets;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Controls
{
    internal sealed class ValuationEditorDialog : Form
    {
        private readonly AssetOrchestrator _orchestrator;
        private readonly Guid _assetId;
        private readonly AssetValuation? _existing;
        private readonly ComboBox _type = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly NumericUpDown _amount = new()
        {
            DecimalPlaces = 2,
            Maximum = 100_000_000m,
            ThousandsSeparator = true,
            TextAlign = HorizontalAlignment.Right,
            Width = 160
        };
        private readonly DateTimePicker _asOf = new() { Format = DateTimePickerFormat.Short };
        private readonly TextBox _source = new() { Width = 260 };

        public ValuationEditorDialog(AssetOrchestrator orchestrator, Guid assetId, AssetValuation? existing)
        {
            _orchestrator = orchestrator;
            _assetId = assetId;
            _existing = existing;
            Text = existing is null ? "Add Valuation" : "Edit Valuation";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.White;
            ClientSize = new Size(420, 220);
            Font = new Font("Segoe UI", 9F);

            _type.Items.AddRange(ValuationTypes.All);
            _type.SelectedItem = existing?.ValueType ?? ValuationTypes.Market;
            if (_type.SelectedIndex < 0)
                _type.SelectedIndex = 0;
            if (existing is not null)
            {
                _amount.Value = existing.Amount;
                _asOf.Value = existing.AsOf;
                _source.Text = existing.Source;
            }

            AddRow(16, "Value", _type);
            AddRow(52, "Amount", _amount);
            AddRow(88, "As of", _asOf);
            AddRow(124, "Source", _source);

            var save = new Button { Text = "Save", Left = 196, Top = 172, Width = 96, Height = 32 };
            var cancel = new Button { Text = "Cancel", Left = 304, Top = 172, Width = 96, Height = 32 };
            save.Click += (_, _) => OnSave();
            cancel.Click += (_, _) => DialogResult = DialogResult.Cancel;
            AcceptButton = save;
            CancelButton = cancel;
            Controls.Add(save);
            Controls.Add(cancel);
        }

        private void OnSave()
        {
            try
            {
                var valuation = _existing ?? new AssetValuation();
                valuation.AssetId = _assetId;
                valuation.ValueType = _type.SelectedItem as string ?? "";
                valuation.Amount = _amount.Value;
                valuation.AsOf = _asOf.Value.Date;
                valuation.Source = _source.Text.Trim();
                _orchestrator.SaveValuation(valuation);
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void AddRow(int y, string label, Control field)
        {
            Controls.Add(new Label { Text = label, Left = 16, Top = y + 4, Width = 70 });
            field.Left = 96;
            field.Top = y;
            Controls.Add(field);
        }
    }
}
