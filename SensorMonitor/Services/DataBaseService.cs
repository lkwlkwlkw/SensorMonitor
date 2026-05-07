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
            public float Temperatura0 { get; set; }
            public float Temperatura1 { get; set; }
            public float Temperatura2 { get; set; }
            public float Temperatura3 { get; set; }
            public float Temperatura4 { get; set; }
            public float Temperatura5 { get; set; }
            public float Temperatura6 { get; set; }
            public float Temperatura7 { get; set; }
            public float Temperatura8 { get; set; }
            public float Temperatura9 { get; set; }
            public float Temperatura10 { get; set; }
            public float Temperatura11 { get; set; }
            public float Pressure0 { get; set; }
            public float Pressure1 { get; set; }
            public float Weight { get; set; }
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
