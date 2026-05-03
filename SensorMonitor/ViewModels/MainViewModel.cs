using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using SensorMonitor.Services;

using System.Diagnostics;


namespace SensorMonitor.ViewModels
{
    public class MainViewModel
    {
        private readonly PLCConnectionService _plcService;
        private readonly DataBaseService _dataBaseService;
        private LineSeries[] _TemperatureSeries = new LineSeries[12]; // Tablica serii dla 12 temperatur

        public MainViewModel(PLCConnectionService _PLCConnectionService, DataBaseService Data)
        {
           
            _dataBaseService = Data; // Iniekcja zależności usługi bazy danych
            _dataBaseService.CreateFile(); // Tworzenie pliku bazy danych
           

            this.MyModel = new PlotModel
            {
                Title = "Temperatury",
                IsLegendVisible = true,
                Legends = { new Legend { LegendPosition = LegendPosition.TopRight } }
            };

            this.MyModel.Axes.Add(new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                StringFormat = "dd.MM HH:mm"
            });
            this.MyModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Wartość"
            });

            InitializeTemperatureSeries();

            _plcService = _PLCConnectionService; // Iniekcja zależności usługi PLC
            _plcService.OnDataReceived += PlcService_OnDataReceived; // Subskrypcja na zdarzenie otrzymania danych
        }

        private void InitializeTemperatureSeries()
        {
            for (int seriesIndex = 0; seriesIndex < _TemperatureSeries.Length; seriesIndex++)
            {
                _TemperatureSeries[seriesIndex] = new LineSeries { Title = $"Temperature {seriesIndex}", IsVisible = true };
                
                foreach (var pomiar in _dataBaseService.Collection.AsQueryable())
                {
                    var temperatureValue = (double)pomiar.GetType().GetProperty($"Temperatura{seriesIndex}").GetValue(pomiar);
                    _TemperatureSeries[seriesIndex].Points.Add(
                        DateTimeAxis.CreateDataPoint(pomiar.Data, temperatureValue)
                    );
                }
                
                this.MyModel.Series.Add(_TemperatureSeries[seriesIndex]);
            }
        }

        private void PlcService_OnDataReceived()
        {             
            for (int i = 0; i < _TemperatureSeries.Length; i++)
            {
                _TemperatureSeries[i].Points.Add(new DataPoint(
                    DateTimeAxis.ToDouble(DateTime.Now),
                    _plcService.Temperature[i]
                ));
            }

            this.MyModel.InvalidatePlot(true);



            _dataBaseService.SavePomiar(new DataBaseService.Pomiar
            {
                Data = DateTime.Now,
                Temperatura0 = 25.5,
                Temperatura1 = 26.0,
                Temperatura2 = 24.8,
                Temperatura3 = 27.1,
                Temperatura4 = 23.9,
                Temperatura5 = 22.5,
                Temperatura6 = 28.3,
                Temperatura7 = 24.2,
                Temperatura8 = 26.7,
                Temperatura9 = 25.0,
                Temperatura10 = 27.5,
                Temperatura11 = 24.6
            }); // Przykładowe zapisanie pomiaru do bazy danych



        }

        public PlotModel MyModel { get; private set; }


    }
}
