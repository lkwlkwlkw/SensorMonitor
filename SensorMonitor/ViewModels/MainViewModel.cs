using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using SensorMonitor.Services;


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

            InitializePressurePlot();
            InitializeTemeraturePlot();
            InitializeWeightPlot();
            
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
                
                this.TemperatureModel.Series.Add(_TemperatureSeries[seriesIndex]);
            }
        }

        private void InitializeTemeraturePlot()
        {
            this.TemperatureModel = new PlotModel
            {
               // Title = "Temperatury",
                IsLegendVisible = true,
                Legends = { new Legend { LegendPosition = LegendPosition.TopRight } }
            };

            this.TemperatureModel.Axes.Add(new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                StringFormat = "dd.MM HH:mm"
            });
            this.TemperatureModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Wartość"
            });
        }


        private void InitializePressurePlot()
        {
            this.PressureModel = new PlotModel
            {
              //  Title = "Temperatury",
                IsLegendVisible = true,
                Legends = { new Legend { LegendPosition = LegendPosition.TopRight } }
            };

            this.PressureModel.Axes.Add(new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                StringFormat = "dd.MM HH:mm"
            });
            this.PressureModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Wartość"
            });
        }

        private void InitializeWeightPlot()
        {
            this.WeightModel = new PlotModel
            {
                //  Title = "Temperatury",
                IsLegendVisible = true,
                Legends = { new Legend { LegendPosition = LegendPosition.TopRight } }
            };

            this.WeightModel.Axes.Add(new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                StringFormat = "dd.MM HH:mm"
            });
            this.WeightModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Wartość"
            });
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

            this.TemperatureModel.InvalidatePlot(true);



            _dataBaseService.SavePomiar(new DataBaseService.Pomiar
            {
                Data = DateTime.Now,
                Temperatura0 = _plcService.Temperature[0],
                Temperatura1 = _plcService.Temperature[1],
                Temperatura2 = _plcService.Temperature[2],
                Temperatura3 = _plcService.Temperature[3],
                Temperatura4 = _plcService.Temperature[4],
                Temperatura5 = _plcService.Temperature[5],
                Temperatura6 = _plcService.Temperature[6],
                Temperatura7 = _plcService.Temperature[7],
                Temperatura8 = _plcService.Temperature[8],
                Temperatura9 = _plcService.Temperature[9],
                Temperatura10 = _plcService.Temperature[10],
                Temperatura11 = _plcService.Temperature[11]
            }); // Przykładowe zapisanie pomiaru do bazy danych



        }

        public PlotModel TemperatureModel { get; private set; }
        public PlotModel PressureModel { get; private set; }
        public PlotModel WeightModel { get; private set; }

    }
}
