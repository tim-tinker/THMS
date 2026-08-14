using System;
using System.ComponentModel;
using THMS.Ingestion;

namespace THMS.Logic.ViewModels
{
    public class ImporterViewModel : INotifyPropertyChanged
    {
        private readonly EnergyIngestionPipeline _pipeline;

        public event PropertyChangedEventHandler? PropertyChanged;

        // ---------------------------------------------------------
        // Selected File
        // ---------------------------------------------------------
        private string? _selectedFile;
        public string? SelectedFile
        {
            get => _selectedFile;
            set { _selectedFile = value; OnChanged(nameof(SelectedFile)); }
        }

        // ---------------------------------------------------------
        // Selected Importer Type (enum)
        // ---------------------------------------------------------
        private ImporterType _selectedImporter = ImporterType.ChargePoint;
        public ImporterType SelectedImporter
        {
            get => _selectedImporter;
            set { _selectedImporter = value; OnChanged(nameof(SelectedImporter)); }
        }

        // ---------------------------------------------------------
        // Status Message
        // ---------------------------------------------------------
        private string _statusMessage = "Ready";
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnChanged(nameof(StatusMessage)); }
        }

        // ---------------------------------------------------------
        // Busy Indicator
        // ---------------------------------------------------------
        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnChanged(nameof(IsBusy)); }
        }

        public ImporterViewModel(EnergyIngestionPipeline pipeline)
        {
            _pipeline = pipeline;
        }

        // ---------------------------------------------------------
        // Import Command
        // ---------------------------------------------------------
        public void Import()
        {
            if (string.IsNullOrWhiteSpace(SelectedFile))
            {
                StatusMessage = "No file selected.";
                return;
            }

            try
            {
                IsBusy = true;
                StatusMessage = "Importing…";

                switch (SelectedImporter)
                {
                    case ImporterType.ChargePoint:
                        _pipeline.IngestChargePointData(SelectedFile);
                        break;

                    case ImporterType.HomeCircuit:
                        _pipeline.IngestHomeCircuitData(SelectedFile);
                        break;

                    case ImporterType.SolarProduction:
                        _pipeline.IngestEnphaseSolarData(SelectedFile);
                        break;

                    default:
                        throw new InvalidOperationException(
                            $"Unknown importer type: {SelectedImporter}");
                }

                StatusMessage = "Import complete.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void OnChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
