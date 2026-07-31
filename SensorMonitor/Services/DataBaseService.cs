using JsonFlatFileDataStore;
using ModernWpf.Controls;
using System.IO;
using Microsoft.Extensions.Options;

namespace SensorMonitor.Services
{
    public class DataBaseService
    {
        public DataStore _dataStore;
        public IDocumentCollection<Pomiar> Collection;
        //    private bool _StorageInitialized = false;
        private readonly AppSettings _settings;

        public class Pomiar
        {
            public DateTime Data { get; set; }
            public string TimeSinceStart { get; set; }
            public float Temp1 { get; set; }
            public float Temp2 { get; set; }
            public float Temp3 { get; set; }
            public float Temp4 { get; set; }
            public float Temp5 { get; set; }
            public float Temp6 { get; set; }
            public float Temp7 { get; set; }
            public float Temp8 { get; set; }
            public float Temp9 { get; set; }
            public float Temp10 { get; set; }
            public float Temp11 { get; set; }
            public float Temp12 { get; set; }
            public float Temp13 { get; set; }
            public float Temp14 { get; set; }
            public float Temp15 { get; set; }
            public float Temp16 { get; set; }
            public float Pressure1 { get; set; }
            public float Pressure2 { get; set; }
            public float Weight { get; set; }
            public bool DegassingActive { get; set; } = false;
            public bool SaturationActive { get; set; } = false;
            public bool HardeningActive { get; set; } = false;  

        }

        public DataBaseService(IOptionsMonitor<AppSettings> options)
        {
            _settings = options.CurrentValue;
            Directory.CreateDirectory(_settings.ReportsPath);            
        }

        public void CreateNewFile(string orderName)
        {
            Directory.CreateDirectory(_settings.ReportsPath);
            _dataStore = new DataStore($@"{_settings.ReportsPath}\{DateTime.Now:yyyyMMddHHmmss}_{orderName}.json");
            Collection = _dataStore.GetCollection<Pomiar>("Pomiary");
        }

        public Boolean OpenExistingFile(string filePath)
        {
            try
            {
                _dataStore = new DataStore(filePath);
                Collection = _dataStore.GetCollection<Pomiar>("Pomiary");
                return true;
            }
            catch (Exception)
            {

                var dialog = new ContentDialog
                {
                    Title = "Błąd",
                    Content = "Nie udało się otworzyć pliku.",
                    PrimaryButtonText = "OK",
                    DefaultButton = ContentDialogButton.Primary
                };
                dialog.ShowAsync();
                return false;
            }
        }

        public void SavePomiar(Pomiar pomiar)
        {
            Collection?.InsertOneAsync(pomiar);
        }

        public void DatabaseClose()
        {
            //_dataStore?.Dispose();
            if (_dataStore != null)
            {
                Task.Run(() => _dataStore.Dispose());
            }
        }
    }
}
