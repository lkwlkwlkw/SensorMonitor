using JsonFlatFileDataStore;
using System.IO;

namespace SensorMonitor.Services
{
    public class DataBaseService
    {
        private DataStore? _dataStore;
        public IDocumentCollection<Pomiar>? Collection;
    //    private bool _StorageInitialized = false;

        public class Pomiar
        {
            public DateTime Data { get; set; }
            public double Temperatura0 { get; set; }
            public double Temperatura1 { get; set; }
            public double Temperatura2 { get; set; }
            public double Temperatura3 { get; set; }
            public double Temperatura4 { get; set; }
            public double Temperatura5 { get; set; }
            public double Temperatura6 { get; set; }
            public double Temperatura7 { get; set; }
            public double Temperatura8 { get; set; }
            public double Temperatura9 { get; set; }
            public double Temperatura10 { get; set; }
            public double Temperatura11 { get; set; }
            public double Pressure0 { get; set; }
            public double Pressure1 { get; set; }
            public double Weight { get; set; }
        }

        public void CreateFile()
        {
           // if (_StorageInitialized)
           //     return;
            Directory.CreateDirectory(@"C:\Raporty");
           // _dataStore = new DataStore(@"C:\Raporty\"+ $"Aktualny_Raport.json");
            _dataStore = new DataStore( $@"C:\Raporty\Raport_{DateTime.Now:yyyyMMddHHmm}.json");
            Collection = _dataStore.GetCollection<Pomiar>("Pomiary");
          //  _StorageInitialized = true;
        }

        public void SavePomiar(Pomiar pomiar)
        {
            Collection?.InsertOneAsync(pomiar);
        }

        //  public void DatabaseClose(DateTime startDate)
        public void DatabaseClose()
        {
            _dataStore?.Dispose();
           // File.Move($@"C:\Raporty\Aktualny_Raport.json", $@"C:\Raporty\Raport_{startDate:yyyyMMddHHmm}.json", true);
         //   _StorageInitialized = false;
        }
    }
}
