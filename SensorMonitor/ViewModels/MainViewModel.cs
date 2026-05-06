using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using SensorMonitor.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using System.IO;

namespace SensorMonitor.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly PLCConnectionService _plcConnectionService;
        private readonly DataBaseService _dataBaseService;
        private LineSeries[] _TemperatureSeries = new LineSeries[12]; // Tablica serii dla 12 temperatur
        public ICommand ClickStartCommand { get; }
        public ICommand ClickStopCommand { get; }
        private string _CycleDurationText="00:00:00";
        private DispatcherTimer _timer = new DispatcherTimer();
        private DateTime _startTime;
        public ObservableCollection<string> Temperature { get; } =
        new ObservableCollection<string> { "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0" };
        private bool _DBWriteActive;

        private bool _StopButtonEnabled;
        public bool StopButtonEnabled
        {
            get => _StopButtonEnabled  ;
            set
            {
                _StopButtonEnabled = value;
                OnPropertyChanged(nameof(StopButtonEnabled));

                // Powiadom WPF, że CanExecute mogło się zmienić
                CommandManager.InvalidateRequerySuggested();
            }
        }


        private bool _StartButtonEnabled=true;
        public bool StartButtonEnabled
        {
            get => _StartButtonEnabled;
            set
            {
                _StartButtonEnabled = value;
                OnPropertyChanged(nameof(StartButtonEnabled));
                // Powiadom WPF, że CanExecute mogło się zmienić
                CommandManager.InvalidateRequerySuggested();
            }
        }







        public string CycleDurationText
        {
            get        {     return _CycleDurationText;            }
            set
            {             
                _CycleDurationText = value;
                OnPropertyChanged();
            }
        }



        public  MainViewModel( PLCConnectionService _PLCConnectionService, DataBaseService Data)
        {
            ClickStartCommand = new RelayCommand(OnStartClick);
            ClickStopCommand = new RelayCommand(OnStopClick);

            _dataBaseService = Data; // Iniekcja zależności usługi bazy danych
           _dataBaseService.CreateFile(); // Tworzenie pliku bazy danych

            InitializePressurePlot();
            InitializeTemeraturePlot();
            InitializeWeightPlot();
            InitializeTemperatureSeries(); // Inicjalizacja serii temperatur z danych z bazy

            //  if (File.Exists(@"C:\Raporty\Aktualny_Raport.json"))

            //   {
            //       _dataBaseService.CreateFile(); // Tworzenie pliku bazy danych
            //       InitializeTemperatureSeries(); // Inicjalizacja serii temperatur z danych z bazy
            //   }

            _plcConnectionService = _PLCConnectionService; // Iniekcja zależności usługi PLC
            _plcConnectionService.OnDataReceived += PlcService_OnDataReceived; // Subskrypcja na zdarzenie otrzymania danych

        }

        private void InitializeTemperatureSeries()
        {
            for (int seriesIndex = 0; seriesIndex < _TemperatureSeries.Length; seriesIndex++)
            {
                _TemperatureSeries[seriesIndex] = new LineSeries { Title = $"Temperature {seriesIndex}", IsVisible = true };
                
                //foreach (var pomiar in _dataBaseService.Collection.AsQueryable())
                //{
                //    var temperatureValue = (double)pomiar.GetType().GetProperty($"Temperatura{seriesIndex}").GetValue(pomiar);
                //    _TemperatureSeries[seriesIndex].Points.Add(
                //        DateTimeAxis.CreateDataPoint(pomiar.Data, temperatureValue)
                //    );
                //}
                
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
            UpdateSensorFields();

            if (!_DBWriteActive && !(_dataBaseService == null))
                return;

            for (int i = 0; i < _TemperatureSeries.Length; i++)
            {
                _TemperatureSeries[i].Points.Add(new DataPoint(
                    DateTimeAxis.ToDouble(DateTime.Now),
                    _plcConnectionService.Temperature[i]
                ));
            }
            this.TemperatureModel.InvalidatePlot(true);

            _dataBaseService.SavePomiar(new DataBaseService.Pomiar
            {
                Data = DateTime.Now,
                Temperatura0 = _plcConnectionService.Temperature[0],
                Temperatura1 = _plcConnectionService.Temperature[1],
                Temperatura2 = _plcConnectionService.Temperature[2],
                Temperatura3 = _plcConnectionService.Temperature[3],
                Temperatura4 = _plcConnectionService.Temperature[4],
                Temperatura5 = _plcConnectionService.Temperature[5],
                Temperatura6 = _plcConnectionService.Temperature[6],
                Temperatura7 = _plcConnectionService.Temperature[7],
                Temperatura8 = _plcConnectionService.Temperature[8],
                Temperatura9 = _plcConnectionService.Temperature[9],
                Temperatura10 = _plcConnectionService.Temperature[10],
                Temperatura11 = _plcConnectionService.Temperature[11]
            }); // Przykładowe zapisanie pomiaru do bazy danych

            

        }

        public PlotModel TemperatureModel { get; private set; }
        public PlotModel PressureModel { get; private set; }
        public PlotModel WeightModel { get; private set; }


        private void OnStartClick(object? parameter)
        {   
          StartButtonEnabled = false;
StopButtonEnabled = true;
            if (_DBWriteActive)
                return;
           
           foreach( var series in _TemperatureSeries)
            {
                series.Points.Clear();
            }


            _startTime = DateTime.Now;          
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, e) =>
            {
               TimeSpan _CycleTime = DateTime.Now - _startTime;
                CycleDurationText = _CycleTime.ToString(@"hh\:mm\:ss");
            };
            _timer.Start();
            _dataBaseService.CreateFile(); // Tworzenie nowego pliku bazy danych przy każdym rozpoczęciu pomiaru
            _DBWriteActive = true;

        }

        private void OnStopClick(object? parameter)
        {
            StartButtonEnabled = true;
            StopButtonEnabled = false;

            if (!_DBWriteActive) return;
            _DBWriteActive = false;

            _timer.Stop();
            _dataBaseService.DatabaseClose(); // Zamknięcie bazy danych i przeniesienie pliku do folderu Raporty

        }


       

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void UpdateSensorFields ()
            {
            for (int i = 0; i < Temperature.Count; i++)
            {
                Temperature[i] = _plcConnectionService.Temperature[i].ToString("F1");
            }
        }


    }
}
