using Microsoft.Extensions.Options;
using ModernWpf.Controls;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using SensorMonitor.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace SensorMonitor.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly PLCConnectionService _plcConnectionService;
        private readonly DataBaseService _dataBaseService;
        private LineSeries[] _TemperatureSeries = new LineSeries[12]; // Tablica serii dla 12 temperatur
        private LineSeries[] _PressureSeries = new LineSeries[2]; // Tablica serii dla 12 ciśnień
        private LineSeries _WeightSeries=new(); // Seria dla wagi
        public ICommand ClickStartCommand { get; }
        public ICommand ClickStopCommand { get; }
        public ICommand ClickPlotFormatCommand { get; }
        private string _CycleDurationText="00:00:00";
        private string _CycleStartText = "00:00:00";
        private string _ConnectionStatusText = "Brak połączenia";
        private DispatcherTimer _timer = new DispatcherTimer();
        private DateTime _startTime;
        public ObservableCollection<string> Temperature { get; } =
        new ObservableCollection<string> { "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0" };
        private bool _DBWriteActive;
        private uint _DBWriteTicksCounter;
        private uint _DBWriteTicksNeeded;

        private readonly AppSettings _settings;

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


        public string CycleStartText
        {
            get { return _CycleStartText; }
            set
            {
                _CycleStartText = value;
                OnPropertyChanged();
            }
        }

        public string ConnectionStatusText
        {
            get { return _ConnectionStatusText; }
            set
            {
                _ConnectionStatusText = value;
                OnPropertyChanged();
            }
        }




        public  MainViewModel( PLCConnectionService _PLCConnectionService, DataBaseService Data, IOptionsMonitor<AppSettings> options)
        {
            _settings = options.CurrentValue;

            ClickStartCommand = new RelayCommand(OnStartClick);
            ClickStopCommand = new RelayCommand(OnStopClick);
            ClickPlotFormatCommand = new RelayCommand(OnClickPlotFormat);

            _dataBaseService = Data; // Iniekcja zależności usługi bazy danych
           _dataBaseService.CreateFile(); // Tworzenie pliku bazy danych

            InitializePressurePlot();
            InitializeTemeraturePlot();
            InitializeWeightPlot();
            InitializeTemperatureSeries(); // Inicjalizacja serii temperatur z danych z bazy
            InitializePressureSeries();
            InitializeWeightSeries();

           

            _plcConnectionService = _PLCConnectionService; // Iniekcja zależności usługi PLC
            _plcConnectionService.OnDataReceived += PlcService_OnDataReceived; // Subskrypcja na zdarzenie otrzymania danych
            _plcConnectionService.ConnectionStatusChanged += PlcService_ConnectionStatusChanged; // Subskrypcja na zdarzenie zmiany statusu połączenia


            
            _DBWriteTicksNeeded = (uint)(_settings.SaveToDBInterval / _settings.PLCPollingInterval); // Zakładając, że dane są odbierane co sekundę
            
        }

        private void OnClickPlotFormat(object? obj)
        {
            TemperatureModel.ResetAllAxes();
            PressureModel.ResetAllAxes();
            WeightModel.ResetAllAxes();

            //throw new NotImplementedException();
        }

        private void InitializeTemperatureSeries()
        {
            for (int seriesIndex = 0; seriesIndex < _TemperatureSeries.Length; seriesIndex++)
            {
                _TemperatureSeries[seriesIndex] = new LineSeries { Title = $"Temperature {seriesIndex}", IsVisible = true };
                
                
                
                this.TemperatureModel.Series.Add(_TemperatureSeries[seriesIndex]);
            }
        }


        private void InitializePressureSeries()
        {
            for (int seriesIndex = 0; seriesIndex < _PressureSeries.Length; seriesIndex++)
            {
                _PressureSeries[seriesIndex] = new LineSeries { Title = $"Pressure {seriesIndex}", IsVisible = true };
              

                this.PressureModel.Series.Add(_PressureSeries[seriesIndex]);
            }
        }


        private void InitializeWeightSeries()
        {
            
                _WeightSeries = new LineSeries { Title = "Waga", IsVisible = true };
             

                this.WeightModel.Series.Add(_WeightSeries);
            
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
                Title = "Temperatura [°C]",
                Maximum = _settings.ScaleFactors.TemperatureMax,
                Minimum = _settings.ScaleFactors.TemperatureMin,
                IsZoomEnabled = false,
                IsPanEnabled = false
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
                Title = "Ciśnienie",
                Maximum = _settings.ScaleFactors.PressureMax,
                Minimum = _settings.ScaleFactors.PressureMin,
                IsZoomEnabled = false,
                IsPanEnabled = false
            });
        }

        private void InitializeWeightPlot()
        {
            this.WeightModel = new PlotModel
            {
                //  Title = "Temperatury",
                IsLegendVisible = true,
             
            };

            this.WeightModel.Axes.Add(new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                StringFormat = "dd.MM HH:mm"
            });
            this.WeightModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Waga",
                Maximum = _settings.ScaleFactors.WeightMax,
                Minimum = _settings.ScaleFactors.WeightMin,
                IsZoomEnabled = false,
                IsPanEnabled = false
            });
        }

        private void PlcService_ConnectionStatusChanged(string status)
        {
            // Możesz tutaj zaktualizować interfejs użytkownika, np. poprzez powiadomienie o zmianie statusu połączenia
            ConnectionStatusText = status;
        }

        private void PlcService_OnDataReceived()
        {
            UpdateSensorFields();

            if (!_DBWriteActive && !(_dataBaseService == null))
                return;

            if (_DBWriteTicksCounter == 0)
            {
                UpdatePlots();
                UpdateDBWrite();

            }
            _DBWriteTicksCounter++;

            if (_DBWriteTicksCounter >= _DBWriteTicksNeeded)
            {
                _DBWriteTicksCounter = 0; // Reset licznika
            }






            

        }

        private void UpdatePlots()
        {
            

            for (int i = 0; i < _TemperatureSeries.Length; i++)
            {
                _TemperatureSeries[i].Points.Add(new DataPoint(
                    DateTimeAxis.ToDouble(DateTime.Now),
                    _plcConnectionService.Temperature[i]
                ));
            }
            this.TemperatureModel.InvalidatePlot(true);

            for (int i = 0; i < _PressureSeries.Length; i++)
            {
                _PressureSeries[i].Points.Add(new DataPoint(
                    DateTimeAxis.ToDouble(DateTime.Now),
                    _plcConnectionService.Pressure[i]
                ));
            }
            this.PressureModel.InvalidatePlot(true);

            _WeightSeries.Points.Add(new DataPoint(
                    DateTimeAxis.ToDouble(DateTime.Now),
                    _plcConnectionService.Weight
                ));

            this.WeightModel.InvalidatePlot(true);
        }

        private void UpdateDBWrite()
        {
                       
           
                _dataBaseService.SavePomiar(new DataBaseService.Pomiar
                {
                    Data = DateTime.Now,
                    Temperatura0 = (float)Math.Round(_plcConnectionService.Temperature[0],1),
                    Temperatura1 = (float)Math.Round(_plcConnectionService.Temperature[1],1),
                    Temperatura2 = (float)Math.Round(_plcConnectionService.Temperature[2],1),
                    Temperatura3 = (float)Math.Round(_plcConnectionService.Temperature[3],1),
                    Temperatura4 = (float)Math.Round(_plcConnectionService.Temperature[4],1),
                    Temperatura5 = (float)Math.Round(_plcConnectionService.Temperature[5],1),
                    Temperatura6 = (float)Math.Round(_plcConnectionService.Temperature[6],1),

                    Temperatura7 = (float)Math.Round(_plcConnectionService.Temperature[7],1),
                    Temperatura8 = (float)Math.Round(_plcConnectionService.Temperature[8],1),
                    Temperatura9 = (float)Math.Round(_plcConnectionService.Temperature[9],1),
                    Temperatura10 = (float)Math.Round(_plcConnectionService.Temperature[10],1),
                    Temperatura11 = (float)Math.Round(_plcConnectionService.Temperature[11],1),
                    Pressure0 = (float)Math.Round(_plcConnectionService.Pressure[0],1),
                    Pressure1 = (float)Math.Round(_plcConnectionService.Pressure[1],1),
                    Weight = (float)Math.Round(_plcConnectionService.Weight,1)
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
            CycleStartText = DateTime.Now.ToString("dd.MM.yyyy HH:mm");

        }

        private async void OnStopClick(object? parameter)
        {
            var dialog = new ContentDialog
            {
                Title = "Zatrzymanie logowania",
                Content = "Jesteś pewien?",
                PrimaryButtonText = "Tak",
                SecondaryButtonText = "Nie",
                DefaultButton = ContentDialogButton.Secondary
            };

            ContentDialogResult result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                StartButtonEnabled = true;
                StopButtonEnabled = false;

                if (!_DBWriteActive) return;
                _DBWriteActive = false;

                _timer.Stop();
                _dataBaseService.DatabaseClose(); // Zamknięcie bazy danych i przeniesienie pliku do folderu Raporty

            }


           

           
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

        private bool _closingHandled = false;

        public async void OnWindowClosing(object sender, CancelEventArgs e)
        {
            if (_closingHandled)
                return;

            e.Cancel = true; // zatrzymaj zamykanie

            var dialog = new ContentDialog
            {
                Title = "Zamykanie",
                Content = "Jesteś pewien?",
                PrimaryButtonText = "Tak",
                SecondaryButtonText = "Nie",
                DefaultButton = ContentDialogButton.Secondary
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                _closingHandled = true;
                (sender as Window).Close(); // zamknij ponownie
            }
        }



    }
}
