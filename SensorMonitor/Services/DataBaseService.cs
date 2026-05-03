using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using JsonFlatFileDataStore;

namespace SensorMonitor.Services
{
    public class DataBaseService
    {
        private DataStore Store;
        public IDocumentCollection<Pomiar> Collection;


        public class Pomiar
        {          

            public DateTime Data { get; set; }
            public double Temperatura0  { get; set; }
            public double Temperatura1 { get; set; }
            public double Temperatura2 { get; set; }
            public double Temperatura3{ get; set; }
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
            Store = new DataStore("data.json");
            Collection = Store.GetCollection<Pomiar>("Pomiary");
            Debug.WriteLine(Collection.ToString());
        }

        public void SavePomiar(Pomiar pomiar)
        {
            
            Collection.InsertOneAsync(pomiar);
        }
    }
}
