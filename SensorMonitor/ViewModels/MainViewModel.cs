using Microsoft.Extensions.Options;
using Microsoft.WindowsAPICodePack.Dialogs;
using ModernWpf.Controls;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using OxyPlot.Wpf;
using SensorMonitor.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;



namespace SensorMonitor.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public PlotModel TemperatureModel { get; private set; }
        public PlotModel PressureModel { get; private set; }
        public PlotModel WeightModel { get; private set; }
        public PlotModel TemperatureModelCommon { get; private set; }
        public PlotModel PressureModelCommon { get; private set; }
        public PlotModel WeightModelCommon { get; private set; }
        private CommonOpenFileDialog dialog = new CommonOpenFileDialog();
        private readonly PLCConnectionService _plcConnectionService;
        private readonly DataBaseService _dataBaseService;
        private LineSeries[] _TemperatureSeries = new LineSeries[12]; // Tablica serii dla 12 temperatur
        private LineSeries[] _PressureSeries = new LineSeries[2]; // Tablica serii dla 12 ciśnień
        private LineSeries _WeightSeries = new(); // Seria dla wagi
        public ICommand ClickStartCommand { get; }
        public ICommand ClickStopCommand { get; }
        public ICommand ClickPlotFormatCommand { get; }
        public ICommand ClickOpenFileCommand { get; }
        private string _CycleDurationText = "00:00:00";
        private string _CycleStartText = "00:00:00";
        private string _CycleStartTextFileName;
        private string _ConnectionStatusText = "Brak połączenia";
        private DispatcherTimer _timer = new DispatcherTimer();
        private DateTime _startTime;
        public ObservableCollection<string> Temperature { get; } =
        new ObservableCollection<string> { "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0" };
        public ObservableCollection<string> Pressure { get; } =
          new ObservableCollection<string> { "0.0", "0.0" };
        public string _Weight = "0.0";
        private bool _DBWriteActive;
        private uint _DBWriteTicksCounter;
        private uint _DBWriteTicksNeeded;
        private readonly AppSettings _settings;
        private bool _StopButtonEnabled;
        public bool StopButtonEnabled
        {
            get => _StopButtonEnabled;
            set
            {
                _StopButtonEnabled = value;
                OnPropertyChanged(nameof(StopButtonEnabled));
                CommandManager.InvalidateRequerySuggested();// Powiadom WPF, że CanExecute mogło się zmienić
            }
        }

        private bool _StartButtonEnabled = false;
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
            get { return _CycleDurationText; }
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

        public string WeightText
        {
            get { return _Weight; }
            set
            {
                _Weight = value;
                OnPropertyChanged();
            }
        }

        public string AssemblyVersion
        {
            get { return BuildInformation.AssemblyVersion; }
        }

        public string AssemblyBuildDate
        {
            get { return BuildInformation.BuildAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm"); }
        }

        public MainViewModel(PLCConnectionService _PLCConnectionService, DataBaseService Data, IOptionsMonitor<AppSettings> options)
        {
            _settings = options.CurrentValue; // Pobranie aktualnych ustawień z IOptionsMonitor

            ClickStartCommand = new RelayCommand(OnStartClick);
            ClickStopCommand = new RelayCommand(OnStopClick);
            ClickPlotFormatCommand = new RelayCommand(OnClickPlotFormat);
            ClickOpenFileCommand = new RelayCommand(OnClickOpenFile);
            _dataBaseService = Data; // Iniekcja zależności usługi bazy danych                                    

            InitializePressurePlot();
            InitializeTemeraturePlot();
            InitializeWeightPlot();
            InitializeTemperatureSeries(); // Inicjalizacja serii temperatur z danych z bazy
            InitializePressureSeries();
            InitializeWeightSeries();
            PressureModelCommon = OxyPlotCloner.CloneModel(PressureModel);
            WeightModelCommon = OxyPlotCloner.CloneModel(WeightModel);
            TemperatureModelCommon = OxyPlotCloner.CloneModel(TemperatureModel);
            // TemperatureModelCommon.Axes[0].IsAxisVisible = false;
            //  PressureModelCommon.Axes[0].IsAxisVisible = false;
            WeightModelCommon.Axes[0].TextColor = OxyColors.Transparent;
            PressureModelCommon.Axes[0].TextColor = OxyColors.Transparent;

            _plcConnectionService = _PLCConnectionService; // Iniekcja zależności usługi PLC
            _plcConnectionService.OnDataReceived += PlcService_OnDataReceived; // Subskrypcja na zdarzenie otrzymania danych
            _plcConnectionService.ConnectionStatusChanged += PlcService_OnConnectionStatusChanged; // Subskrypcja na zdarzenie zmiany statusu połączenia

            _DBWriteTicksNeeded = (uint)(_settings.SaveToDBInterval / _settings.PLCPollingInterval); // Obliczenie, ile cykli odczytu PLC musi minąć, zanim dane zostaną zapisane do bazy danych

        }

        #region OnClick Events
        // Metody obsługujące kliknięcia przycisków

        private void OnStartClick(object parameter)
        {
            StartButtonEnabled = false;
            StopButtonEnabled = true;
            if (_DBWriteActive)
                return;

            ResetPlotAndClearPoints(TemperatureModel, true);
            ResetPlotAndClearPoints(TemperatureModelCommon, true);
            ResetPlotAndClearPoints(PressureModel, true);
            ResetPlotAndClearPoints(PressureModelCommon, true);
            ResetPlotAndClearPoints(WeightModel, true);
            ResetPlotAndClearPoints(WeightModelCommon, true);

            _startTime = DateTime.Now;
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, e) =>
            {
                TimeSpan _CycleTime = DateTime.Now - _startTime;
                CycleDurationText = _CycleTime.ToString(@"hh\:mm\:ss");
            };
            _timer.Start();
            _dataBaseService.CreateNewFile(); // Tworzenie nowego pliku bazy danych przy każdym rozpoczęciu pomiaru
            _DBWriteActive = true;
            CycleStartText = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            _CycleStartTextFileName = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        }

        private async void OnStopClick(object parameter)
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

                var pngExporter = new PngExporter { Width = 1280, Height = 720 };
                pngExporter.ExportToFile(TemperatureModel, $@"c:\Raporty\Obrazy\{_CycleStartTextFileName}_Temperatura.png");
                pngExporter.ExportToFile(PressureModel, $@"c:\Raporty\Obrazy\{_CycleStartTextFileName}_Ciśnienie.png");
                pngExporter.ExportToFile(WeightModel, $@"c:\Raporty\Obrazy\{_CycleStartTextFileName}_Waga.png");

            }
        }


        private void OnClickPlotFormat(object obj) // Resetowanie osi wykresów do wartości domyślnych
        {
            TemperatureModel.ResetAllAxes();
            PressureModel.ResetAllAxes();
            WeightModel.ResetAllAxes();
            TemperatureModelCommon.ResetAllAxes();
            PressureModelCommon.ResetAllAxes();
            WeightModelCommon.ResetAllAxes();

            TemperatureModel.InvalidatePlot(true);
            PressureModel.InvalidatePlot(true);
            WeightModel.InvalidatePlot(true);
            TemperatureModelCommon.InvalidatePlot(true);
            PressureModelCommon.InvalidatePlot(true);
            WeightModelCommon.InvalidatePlot(true);
        }

        private void OnClickOpenFile(object obj) // Otwieranie pliku bazy danych
        {
            //var dialog = new CommonOpenFileDialog();
            dialog.Title = "Wybierz plik";
            dialog.Filters.Add(new CommonFileDialogFilter("Pliki JSON", "*.json"));
            dialog.InitialDirectory = @"C:\Raporty";
            dialog.Multiselect = false;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                string path = dialog.FileName;
                LoadDataFromDatabase(path);
            }
        }

        #endregion


        public void AddPointToBoth(PlotModel original, PlotModel clone, double x, double y, int SerieNumber)
        {
            var p = new DataPoint(x, y);

            var s1 = (LineSeries)original.Series[SerieNumber];
            var s2 = (LineSeries)clone.Series[SerieNumber];

            s1.Points.Add(p);
            s2.Points.Add(new DataPoint(x, y));

            //  original.InvalidatePlot(true);
            //  clone.InvalidatePlot(true);
        }

        public void ResetPlotAndClearPoints(PlotModel model, bool invalidate)
        {
            foreach (var s in model.Series.OfType<LineSeries>())
                s.Points.Clear();

            //  
            if (invalidate)
            {
                model.ResetAllAxes();
                model.InvalidatePlot(true);
            }
        }


        private void LoadDataFromDatabase(string filePath)
        {
            // Implementacja wczytywania danych z pliku bazy danych
            // Po wczytaniu danych, zaktualizuj wykresy
            ResetPlotAndClearPoints(TemperatureModel, false);
            ResetPlotAndClearPoints(PressureModel, false);
            ResetPlotAndClearPoints(WeightModel, false);
            ResetPlotAndClearPoints(TemperatureModelCommon, false);
            ResetPlotAndClearPoints(PressureModelCommon, false);
            ResetPlotAndClearPoints(WeightModelCommon, false);

            if (_dataBaseService.OpenExistingFile(filePath)) // Wczytanie danych z wybranego pliku bazy danych
            {
                for (int seriesIndex = 0; seriesIndex < _TemperatureSeries.Length; seriesIndex++)
                {
                    //  _TemperatureSeries[seriesIndex] = new LineSeries { Title = $"Temperature {seriesIndex}", IsVisible = true };

                    // _TemperatureSeries[seriesIndex].Points.Clear(); // Wyczyść istniejące punkty przed dodaniem nowych danych z bazy
                    foreach (var pomiar in _dataBaseService.Collection.AsQueryable())
                    {
                        var temperatureValue = (float)pomiar.GetType().GetProperty($"Temp{seriesIndex + 1}").GetValue(pomiar);
                        // _TemperatureSeries[seriesIndex].Points.Add(DateTimeAxis.CreateDataPoint(pomiar.Data, temperatureValue)
                        AddPointToBoth(TemperatureModel, TemperatureModelCommon, DateTimeAxis.ToDouble(pomiar.Data), temperatureValue, seriesIndex);
                    }

                }


                for (int seriesIndex = 0; seriesIndex < _PressureSeries.Length; seriesIndex++)
                {
                    _PressureSeries[seriesIndex].Points.Clear(); // Wyczyść istniejące punkty przed dodaniem nowych danych z bazy
                    foreach (var pomiar in _dataBaseService.Collection.AsQueryable())
                    {
                        var pressureValue = (float)pomiar.GetType().GetProperty($"Pressure{seriesIndex + 1}").GetValue(pomiar);
                        //_PressureSeries[seriesIndex].Points.Add( DateTimeAxis.CreateDataPoint(pomiar.Data, pressureValue)
                        AddPointToBoth(PressureModel, PressureModelCommon, DateTimeAxis.ToDouble(pomiar.Data), pressureValue, seriesIndex);
                    }
                }


                foreach (var pomiar in _dataBaseService.Collection.AsQueryable())
                {
                    var weightValue = (float)pomiar.GetType().GetProperty($"Weight").GetValue(pomiar);
                    // _WeightSeries.Points.Add( DateTimeAxis.CreateDataPoint(pomiar.Data, weightValue)
                    AddPointToBoth(WeightModel, WeightModelCommon, DateTimeAxis.ToDouble(pomiar.Data), weightValue, 0);

                }

                TemperatureModel.InvalidatePlot(true);
                TemperatureModelCommon.InvalidatePlot(true);
                PressureModel.InvalidatePlot(true);
                PressureModelCommon.InvalidatePlot(true);
                WeightModel.InvalidatePlot(true);
                WeightModelCommon.InvalidatePlot(true);

            }
        }

        private void InitializeTemperatureSeries()
        {
            for (int seriesIndex = 0; seriesIndex < _TemperatureSeries.Length; seriesIndex++)
            {
                _TemperatureSeries[seriesIndex] = new LineSeries { Title = $"Temp. {seriesIndex + 1}", IsVisible = true };
                this.TemperatureModel.Series.Add(_TemperatureSeries[seriesIndex]);
            }
        }

        private void InitializePressureSeries()
        {
            for (int seriesIndex = 0; seriesIndex < _PressureSeries.Length; seriesIndex++)
            {
                _PressureSeries[seriesIndex] = new LineSeries { Title = $"Ciśnienie {seriesIndex + 1}", IsVisible = true };
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
                Title = "Ciśnienie [Bar]",
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
                IsLegendVisible = false,
            };

            this.WeightModel.Axes.Add(new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                StringFormat = "dd.MM HH:mm"
            });
            this.WeightModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Waga [kg]",
                Maximum = _settings.ScaleFactors.WeightMax,
                Minimum = _settings.ScaleFactors.WeightMin,
                IsZoomEnabled = false,
                IsPanEnabled = false
            });
        }

        private void PlcService_OnConnectionStatusChanged(string status) // Aktualizacja statusu połączenia z PLC
        {
            // Możesz tutaj zaktualizować interfejs użytkownika, np. poprzez powiadomienie o zmianie statusu połączenia
            ConnectionStatusText = status;
            if (_plcConnectionService.IsConnected)
            {
                StartButtonEnabled = true;
                StopButtonEnabled = false;
            }
            else
            {
                StartButtonEnabled = false;
                StopButtonEnabled = false;
            }
        }

        private void PlcService_OnDataReceived() // Aktualizacja danych z PLC i odświeżenie wykresów
        {
            UpdateSensorFields();

            if (!_DBWriteActive && !(_dataBaseService == null))
                return;

            if (_DBWriteTicksCounter == 0)
            {
                UpdatePlotsAfterPLCDataRcv();
                UpdateDBWrite();
            }
            _DBWriteTicksCounter++;

            if (_DBWriteTicksCounter >= _DBWriteTicksNeeded)
            {
                _DBWriteTicksCounter = 0; // Reset licznika
            }
        }

        private void UpdatePlotsAfterPLCDataRcv()
        {
            for (int i = 0; i < _TemperatureSeries.Length; i++)
            {
                //_TemperatureSeries[i].Points.Add(new DataPoint( DateTimeAxis.ToDouble(DateTime.Now), _plcConnectionService.Temperature[i] ));
                AddPointToBoth(TemperatureModel, TemperatureModelCommon, DateTimeAxis.ToDouble(DateTime.Now), _plcConnectionService.Temperature[i], i);

            }
            this.TemperatureModel.InvalidatePlot(true);
            TemperatureModelCommon.InvalidatePlot(true);

            for (int i = 0; i < _PressureSeries.Length; i++)
            {
                // _PressureSeries[i].Points.Add(new DataPoint(DateTimeAxis.ToDouble(DateTime.Now),_plcConnectionService.Pressure[i] ));
                AddPointToBoth(PressureModel, PressureModelCommon, DateTimeAxis.ToDouble(DateTime.Now), _plcConnectionService.Pressure[i], i);
            }
            this.PressureModel.InvalidatePlot(true);
            this.PressureModelCommon.InvalidatePlot(true);

            AddPointToBoth(WeightModel, WeightModelCommon, DateTimeAxis.ToDouble(DateTime.Now), _plcConnectionService.Weight, 0);

            this.WeightModel.InvalidatePlot(true);
            this.WeightModelCommon.InvalidatePlot(true);
        }

        private void UpdateDBWrite()
        {
            _dataBaseService.SavePomiar(new DataBaseService.Pomiar
            {
                Data = DateTime.Now,
                Temp1 = (float)Math.Round(_plcConnectionService.Temperature[0], 1),
                Temp2 = (float)Math.Round(_plcConnectionService.Temperature[1], 1),
                Temp3 = (float)Math.Round(_plcConnectionService.Temperature[2], 1),
                Temp4 = (float)Math.Round(_plcConnectionService.Temperature[3], 1),
                Temp5 = (float)Math.Round(_plcConnectionService.Temperature[4], 1),
                Temp6 = (float)Math.Round(_plcConnectionService.Temperature[5], 1),
                Temp7 = (float)Math.Round(_plcConnectionService.Temperature[6], 1),
                Temp8 = (float)Math.Round(_plcConnectionService.Temperature[7], 1),
                Temp9 = (float)Math.Round(_plcConnectionService.Temperature[8], 1),
                Temp10 = (float)Math.Round(_plcConnectionService.Temperature[9], 1),
                Temp11 = (float)Math.Round(_plcConnectionService.Temperature[10], 1),
                Temp12 = (float)Math.Round(_plcConnectionService.Temperature[11], 1),
                Pressure1 = (float)Math.Round(_plcConnectionService.Pressure[0], 1),
                Pressure2 = (float)Math.Round(_plcConnectionService.Pressure[1], 1),
                Weight = (float)Math.Round(_plcConnectionService.Weight, 1)
            });
        }





        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void UpdateSensorFields()
        {
            for (int i = 0; i < Temperature.Count; i++)
            {
                Temperature[i] = _plcConnectionService.Temperature[i].ToString("F1");
            }

            for (int i = 0; i < Pressure.Count; i++)
            {
                Pressure[i] = _plcConnectionService.Pressure[i].ToString("F1");
            }

            WeightText = _plcConnectionService.Weight.ToString("F1");
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

    public class NotConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b ? !b : value;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b ? !b : value;
    }

}
