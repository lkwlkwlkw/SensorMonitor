using Force.DeepCloner;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAPICodePack.Dialogs;
using ModernWpf.Controls;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using OxyPlot.Wpf;
using SensorMonitor.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using static SensorMonitor.Services.DataBaseService;



namespace SensorMonitor.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public PlotModel TemperatureModel { get; private set; }
        public PlotModel PressureModel { get; private set; }
        public PlotModel WeightModel { get; private set; }
        public PlotModel TemperatureModelCommon { get; private set; } //model na wspólnej zakładce, który będzie klonem modelu z głównej zakładki, ale bez widocznych osi X i Y, żeby wyglądał jakby był częścią tego samego wykresu, a nie osobnym modelem
        public PlotModel PressureModelCommon { get; private set; }
        public PlotModel WeightModelCommon { get; private set; }
        public PlotModel TemperatureModelMain { get; private set; }
        private CommonOpenFileDialog dialog = new CommonOpenFileDialog();
        private readonly PLCConnectionService _plcConnectionService;
        private readonly DataBaseService _dataBaseService;
        private LineSeries[] _TemperatureSeries = new LineSeries[16]; // Tablica serii dla 16 temperatur
        private LineSeries[] _PressureSeries = new LineSeries[2]; // Tablica serii dla 2 ciśnień
        private LineSeries _WeightSeries = new(); // Seria dla wagi
        public ICommand ClickStartCommand { get; }
        public ICommand ClickStopCommand { get; }
        public ICommand ClickPlotFormatCommand { get; }
        public ICommand ClickOpenFileCommand { get; }        
        private string _CycleDurationText = "00:00:00";
        private string _CycleStartText = "00:00:00";
        private string _CycleStartTextFileName;
        private string _ConnectionStatusText = "Brak połączenia";
        private string _OrderNameText = "Zlecenie";
        private float _WeightChange = 0;
        private float _oldWeight = 0;
        private UInt32 _DegassingTime = 30;
        private DispatcherTimer _timer = new DispatcherTimer();
        private DateTime _startTime;
        public ObservableCollection<string> Temperature { get; } =
        new ObservableCollection<string> { "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0" };
        public ObservableCollection<string> Pressure { get; } =
          new ObservableCollection<string> { "0", "0" };
        public string _Weight = "0.0";
        private bool _DBWriteActive;
        private uint _DBWriteTicksCounter;
        private uint _DBWriteTicksNeeded;
        private readonly AppSettings _settings;
        private bool _StopButtonEnabled;
        private bool _DegassingButtonEnabled;
        private bool _SaturationButtonEnabled;
        private bool _HardeningButtonEnabled;
        private List<string> AlarmsTextList;
        private List<string> WarningsTextList;
        private ObservableCollection<string> _AlarmsAndWarningsTextList = new();
        private UInt32 _AlarmsOld;
        private UInt32 _WarningsOld;
       // private bool _DegassingInProgress = false;
        private Int32 _DegassingTimeRemained = 0;

        private bool _saturationIsChecked = false;
        private bool _utwardzanieIsChecked = false;
        private bool _degassingIsChecked = false;

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

        public bool DegassingButtonEnabled
        {
            get => _DegassingButtonEnabled;
            set
            {
                _DegassingButtonEnabled = value;
                //OnPropertyChanged(nameof(DegassingButtonEnabled));
               OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();// Powiadom WPF, że CanExecute mogło się zmienić
            }
        }

        public bool HardeningButtonEnabled
        {
            get => _HardeningButtonEnabled;
            set
            {
                _HardeningButtonEnabled = value;
                // OnPropertyChanged(nameof(DegassingButtonEnabled));
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();// Powiadom WPF, że CanExecute mogło się zmienić
            }
        }

        public bool SaturationButtonEnabled
        {
            get => _SaturationButtonEnabled;
            set
            {
                _SaturationButtonEnabled = value;
                // OnPropertyChanged(nameof(DegassingButtonEnabled));
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();// Powiadom WPF, że CanExecute mogło się zmienić
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

        public string OrderNameText
        {
            get { return _OrderNameText; }
            set
            {
                _OrderNameText = value;
                OnPropertyChanged();
            }
        }

        public string ZmianaWagiText
        {
            get { return _WeightChange.ToString("F0"); }
            set
            {
                _WeightChange = float.Parse(value);
                OnPropertyChanged();
            }
        }

        public UInt32 DegassingTime
        {
            get { return _DegassingTime; }
            set
            {
                _DegassingTime = value;
                OnPropertyChanged();
            }
        }

        public Int32 DegassingTimeRemained
        {
            get { return _DegassingTimeRemained; }
            set
            {
                _DegassingTimeRemained = value;
                OnPropertyChanged();
            }
        }


        public ObservableCollection<string> AlarmsAndWarningsTextList
        {
            get { return _AlarmsAndWarningsTextList; }
            set
            {
                _AlarmsAndWarningsTextList = value;
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

        private bool ButtonTogglerEnabled = true;

        public bool DegassingIsChecked
        {
            get => _degassingIsChecked;
            set
            {
                _degassingIsChecked = value;
                OnPropertyChanged();   

                var itemsToSend = new List<(string nodeId, object value)> //przesłanie stanu nasycenia do PLC
                        {
                         (_settings.NodeIds.DegassingStart, value),
                         (_settings.NodeIds.DegassingTime, _DegassingTime),
                        };
                _plcConnectionService.WriteData(itemsToSend);          

              if (ButtonTogglerEnabled)
              {
                  ButtonToggler("DegassingIsChecked");
              }
            }
        }

        public bool SaturationIsChecked
        {
            get => _saturationIsChecked;
            set
            {
                _saturationIsChecked = value;
                OnPropertyChanged();           
              
                var itemsToSend = new List<(string nodeId, object value)> //przesłanie stanu nasycenia do PLC
                        {
                         (_settings.NodeIds.SaturationActive, value),
                        };
                    _plcConnectionService.WriteData(itemsToSend);

                if (ButtonTogglerEnabled)
                {
                    ButtonToggler("SaturationIsChecked");
                }
            }
        }

        public bool HardeningIsChecked
        {
            get => _utwardzanieIsChecked;
            set
            {
                _utwardzanieIsChecked = value;
                OnPropertyChanged();
                var itemsToSend = new List<(string nodeId, object value)> //przesłanie stanu nasycenia do PLC
                        {
                         (_settings.NodeIds.HardeningActive, value),
                        };
                _plcConnectionService.WriteData(itemsToSend);               

                if (ButtonTogglerEnabled)
                {
                    ButtonToggler("HardeningIsChecked");
                }
            }
        }

        private void ButtonToggler(string _caller)
            {
                ButtonTogglerEnabled = false;
            //  DegassingButtonEnabled = !SaturationIsChecked;
            // SaturationButtonEnabled = (DegassingIsChecked || SaturationIsChecked)& !HardeningIsChecked;
            //  HardeningButtonEnabled = SaturationIsChecked || HardeningIsChecked;

            // if (SaturationIsChecked) {DegassingIsChecked = false; }
            //  if (HardeningIsChecked) { SaturationIsChecked = false; }
            if (_caller == "DegassingIsChecked" && DegassingIsChecked) { SaturationIsChecked = false; HardeningIsChecked = false; goto End; }
            if (_caller == "SaturationIsChecked" && SaturationIsChecked) { DegassingIsChecked = false; HardeningIsChecked = false; goto End; }
            if (_caller == "HardeningIsChecked" && HardeningIsChecked) { DegassingIsChecked = false; SaturationIsChecked = false; goto End; }
        End:
            ButtonTogglerEnabled = true;
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
            TemperatureModelMain = OxyPlotCloner.CloneModel(TemperatureModel);
            // TemperatureModelCommon.Axes[0].IsAxisVisible = false;
            //  PressureModelCommon.Axes[0].IsAxisVisible = false;
            WeightModelCommon.Axes[0].TextColor = OxyColors.Transparent;
            PressureModelCommon.Axes[0].TextColor = OxyColors.Transparent;

            _plcConnectionService = _PLCConnectionService; // Iniekcja zależności usługi PLC
            _plcConnectionService.OnDataReceived += PlcService_OnDataReceived; // Subskrypcja na zdarzenie otrzymania danych
            _plcConnectionService.ConnectionStatusChanged += PlcService_OnConnectionStatusChanged; // Subskrypcja na zdarzenie zmiany statusu połączenia

            _DBWriteTicksNeeded = (uint)(_settings.SaveToDBInterval / _settings.PLCPollingInterval); // Obliczenie, ile cykli odczytu PLC musi minąć, zanim dane zostaną zapisane do bazy danych

            LoadAlarmsAndWarningsText(); // Wczytanie treści alarmów i ostrzeżeń z plików tekstowych do listy, która będzie używana do aktualizacji interfejsu użytkownika
        }

        private void LoadAlarmsAndWarningsText() // Metoda do wczytywania treści alarmów i ostrzeżeń z plików tekstowych, jeśli chcesz odświeżyć treść bez ponownego uruchamiania aplikacji
        {
            try
            {
                AlarmsTextList = File.ReadAllLines("Alarms.txt").ToList();
                WarningsTextList = File.ReadAllLines("Warnings.txt").ToList();
            }
            catch
            {
                var dialog = new ContentDialog
                {
                    Title = "Błąd",
                    Content = "Nie można odczytać pliku treści alarmów lub ostrzeżeń.",
                    PrimaryButtonText = "OK",
                    DefaultButton = ContentDialogButton.Primary
                };
                dialog.ShowAsync();
            }
        }

        #region OnClick Events
        // Metody obsługujące kliknięcia przycisków

       
        private TimeSpan CycleTime;
        private void OnStartClick(object parameter)
        {
            degassingActiveLastState = false; //nie bieżemy pod uwagę stanu przy starcie, żeby nie dodawać adnotacji na wykresie przy starcie pomiaru
            saturationActiveLastState = false;
            hardeningActiveLastState = false;


            StartButtonEnabled = false;
            StopButtonEnabled = true;
           // DegassingButtonEnabled = true;
            if (_DBWriteActive)
                return;
            _oldWeight = 0; // Reset zmiany wagi przy każdym rozpoczęciu pomiaru
            _WeightChange = 0;
            ResetPlotAndClearPoints(TemperatureModel, true);
            ResetPlotAndClearPoints(TemperatureModelCommon, true);
            ResetPlotAndClearPoints(TemperatureModelMain, true);
            ResetPlotAndClearPoints(PressureModel, true);
            ResetPlotAndClearPoints(PressureModelCommon, true);
            ResetPlotAndClearPoints(WeightModel, true);
            ResetPlotAndClearPoints(WeightModelCommon, true);

            _startTime = DateTime.Now;
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, e) =>
            {
                CycleTime = DateTime.Now - _startTime;
                CycleDurationText = CycleTime.ToString(@"hh\:mm\:ss");
            };
            _timer.Start();
            _dataBaseService.CreateNewFile(_OrderNameText); // Tworzenie nowego pliku bazy danych przy każdym rozpoczęciu pomiaru
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
                
              //  DegassingIsChecked = false;
              //  SaturationIsChecked = false;
              //  HardeningIsChecked = false;
                
               // SaturationButtonEnabled = false;
              //  HardeningButtonEnabled = false;
              //  DegassingButtonEnabled = false;
                if (!_DBWriteActive) return;
                _DBWriteActive = false;
                _timer.Stop();

                // Zamknij bazę (DataBaseService już używa Task.Run przy dispose)
                
                
                _dataBaseService.DatabaseClose();


                // Klonowanie modeli wykresów — aby nie operować na UI-modelach w tle
                var tempClone = OxyPlotCloner.CloneModel(TemperatureModel);
                var pressClone = OxyPlotCloner.CloneModel(PressureModel);
                var weightClone = OxyPlotCloner.CloneModel(WeightModel);

                // Wykonaj eksport w tle, by nie blokować UI
                try
                {
                    // Uruchom eksport na nowym wątku STA i poczekaj asynchronicznie na jego zakończenie
                    var tcs = new TaskCompletionSource<bool>();
                    var staThread = new Thread(() =>
                    {
                        try
                        {
                            // Utworzenie Dispatcher dla wątku STA (potrzebne dla niektórych komponentów WPF)
                            var dispatcher = Dispatcher.CurrentDispatcher;

                            var pngExporter = new PngExporter { Width = 1280, Height = 720 };
                            Directory.CreateDirectory(_settings.ReportsPath + @"\Obrazy");

                            pngExporter.ExportToFile(tempClone, $@"{_settings.ReportsPath}\Obrazy\{_CycleStartTextFileName}_Temperatura.png");
                            pngExporter.ExportToFile(pressClone, $@"{_settings.ReportsPath}\Obrazy\{_CycleStartTextFileName}_Ciśnienie.png");
                            pngExporter.ExportToFile(weightClone, $@"{_settings.ReportsPath}\Obrazy\{_CycleStartTextFileName}_Waga.png");

                            // zakończ dispatcher i sygnalizuj powodzenie
                            tcs.SetResult(true);
                        }
                        catch (Exception ex)
                        {
                            tcs.SetException(ex);
                        }
                        finally
                        {
                            // zamknięcie Dispatcher'a wątku STA
                            Dispatcher.CurrentDispatcher.InvokeShutdown();
                        }
                    });

                    staThread.IsBackground = true;
                    staThread.SetApartmentState(ApartmentState.STA);
                    staThread.Start();

                    await tcs.Task; // asynchronicznie czekamy bez blokowania UI
                }
                catch (Exception ex)
                {
                    var err = new ContentDialog
                    {
                        Title = "Błąd",
                        Content = $"Eksport obrazów nie powiódł się: {ex.Message}",
                        PrimaryButtonText = "OK",
                        DefaultButton = ContentDialogButton.Primary
                    };
                    _ = err.ShowAsync();
                }

               // Wyłącz wszelkie stany do PLC
                    var itemsToSend = new List<(string nodeId, object value)>
                    {
                        (_settings.NodeIds.DegassingStart, false),
                        (_settings.NodeIds.HardeningActive, false),
                        (_settings.NodeIds.SaturationActive, false),
                    };
                    _plcConnectionService.WriteData(itemsToSend);
               
            }
        }

        public void OnClickPlotFormat(object obj) // Resetowanie osi wykresów do wartości domyślnych
        {
            TemperatureModel.ResetAllAxes();
            PressureModel.ResetAllAxes();
            WeightModel.ResetAllAxes();
            TemperatureModelCommon.ResetAllAxes();
            PressureModelCommon.ResetAllAxes();
            WeightModelCommon.ResetAllAxes();
            TemperatureModelMain.ResetAllAxes();

            TemperatureModel.InvalidatePlot(true);
            PressureModel.InvalidatePlot(true);
            WeightModel.InvalidatePlot(true);
            TemperatureModelCommon.InvalidatePlot(true);
            PressureModelCommon.InvalidatePlot(true);
            WeightModelCommon.InvalidatePlot(true);
            TemperatureModelMain.InvalidatePlot(true);
        }

        private void OnClickOpenFile(object obj) // Otwieranie pliku bazy danych
        {
            //var dialog = new CommonOpenFileDialog();
            dialog.Title = "Wybierz plik";
            dialog.Filters.Add(new CommonFileDialogFilter("Pliki JSON", "*.json"));
            dialog.InitialDirectory = _settings.ReportsPath;
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
            
        }

        private  void AddAnnotation (PlotModel model, double x, string text)
        {
            var annotation = new LineAnnotation
            {
                Type = LineAnnotationType.Vertical,
                X = x,
                MinimumX = double.NegativeInfinity,
                MaximumX = double.PositiveInfinity,
                MinimumY = double.NegativeInfinity,
                MaximumY = double.PositiveInfinity,
                Color = OxyColors.Green,
                StrokeThickness = 2,
                Text = text,
                TextColor = OxyColors.Red,
                TextOrientation = AnnotationTextOrientation.Vertical
            };
            model.Annotations.Add(annotation);
        }

        private bool degassingActiveLastState = false;
        private bool saturationActiveLastState = false;
        private bool hardeningActiveLastState = false;
        private string CreateAnnotationText(bool _degassingActive, bool _saturationActive, bool _hardeningActive, String _cycleTime)
        {
           var degassingStart = _degassingActive && !degassingActiveLastState;
           var degassingStop = !_degassingActive && degassingActiveLastState;
            var saturationStart = _saturationActive && !saturationActiveLastState;
            var saturationStop = !_saturationActive && saturationActiveLastState;
            var hardeningStart = _hardeningActive && !hardeningActiveLastState;
            var hardeningStop = !_hardeningActive && hardeningActiveLastState;
            if (!degassingStart && !degassingStop && !saturationStart && !saturationStop && !hardeningStart && !hardeningStop)
            {
                return null; // Nie dodawaj adnotacji, jeśli nie ma zmiany stanu
            }
            var annotationText = _cycleTime + " ";
            if (degassingStart) { annotationText += "Start odgazowanie\n"; }
            if (degassingStop) { annotationText += "Stop odgazowanie\n"; }
            if (saturationStart) { annotationText += "Start nasycenie\n"; }
            if (saturationStop) { annotationText += "Stop nasycenie\n"; }
            if (hardeningStart) { annotationText += "Start utwardzanie\n"; }           
            if (hardeningStop) { annotationText += "Stop utwardzanie\n"; }
            degassingActiveLastState = _degassingActive;
            saturationActiveLastState = _saturationActive;
            hardeningActiveLastState = _hardeningActive;
            return annotationText;
        }


        public void ResetPlotAndClearPoints(PlotModel model, bool invalidate)
        {
            foreach (var s in model.Series.OfType<LineSeries>())
                s.Points.Clear();
            if (invalidate)
            {
                model.ResetAllAxes();
                model.InvalidatePlot(true);
            }
        }

        private void LoadDataFromDatabase(string filePath)
        {
            degassingActiveLastState = false;
            saturationActiveLastState = false;
            hardeningActiveLastState = false;


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
                    foreach (var pomiar in _dataBaseService.Collection.AsQueryable())
                    {
                        var temperatureValue = (float)pomiar.GetType().GetProperty($"Temp{seriesIndex + 1}").GetValue(pomiar);
                        AddPointToBoth(TemperatureModel, TemperatureModelCommon, DateTimeAxis.ToDouble(pomiar.Data), temperatureValue, seriesIndex); 
                    }
                }

                for (int seriesIndex = 0; seriesIndex < _PressureSeries.Length; seriesIndex++)
                {
                    _PressureSeries[seriesIndex].Points.Clear(); // Wyczyść istniejące punkty przed dodaniem nowych danych z bazy
                    foreach (var pomiar in _dataBaseService.Collection.AsQueryable())
                    {
                        var pressureValue = (float)pomiar.GetType().GetProperty($"Pressure{seriesIndex + 1}").GetValue(pomiar);
                        AddPointToBoth(PressureModel, PressureModelCommon, DateTimeAxis.ToDouble(pomiar.Data), pressureValue, seriesIndex);
                    }
                }

                foreach (var pomiar in _dataBaseService.Collection.AsQueryable())
                {
                    var weightValue = (float)pomiar.GetType().GetProperty($"Weight").GetValue(pomiar);
                    AddPointToBoth(WeightModel, WeightModelCommon, DateTimeAxis.ToDouble(pomiar.Data), weightValue, 0);
                }

                //dodaj znaczniki
                foreach (var pomiar in _dataBaseService.Collection.AsQueryable())
                {
                    var AnnotationText = CreateAnnotationText((bool)pomiar.GetType().GetProperty($"DegassingActive").GetValue(pomiar),
                        (bool)pomiar.GetType().GetProperty($"SaturationActive").GetValue(pomiar),
                        (bool)pomiar.GetType().GetProperty($"HardeningActive").GetValue(pomiar),
                        (string)pomiar.GetType().GetProperty($"TimeSinceStart").GetValue(pomiar));

                    if (AnnotationText != null)
                    {
                        AddAnnotation(TemperatureModel, DateTimeAxis.ToDouble(pomiar.Data), AnnotationText);
                        AddAnnotation(TemperatureModelCommon, DateTimeAxis.ToDouble(pomiar.Data), AnnotationText);
                        AddAnnotation(PressureModel, DateTimeAxis.ToDouble(pomiar.Data), AnnotationText);
                        AddAnnotation(PressureModelCommon, DateTimeAxis.ToDouble(pomiar.Data), AnnotationText);
                        AddAnnotation(WeightModel, DateTimeAxis.ToDouble(pomiar.Data), AnnotationText);
                        AddAnnotation(WeightModelCommon, DateTimeAxis.ToDouble(pomiar.Data), AnnotationText);
                    }
                }

                // Po wczytaniu danych z bazy, odśwież wykresy
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
                Legends = { new Legend { LegendPosition = LegendPosition.TopRight }
                }
            };

            this.TemperatureModel.Axes.Add(new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                StringFormat = "HH:mm"
            });
            this.TemperatureModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Temperatura [°C]",
                Maximum = _settings.ScaleFactors.TemperatureMax,
                Minimum = _settings.ScaleFactors.TemperatureMin,
                IsZoomEnabled = false,
                IsPanEnabled = false,
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = OxyColors.LightGray,
                MajorGridlineThickness = 1,
                MinorGridlineStyle = LineStyle.Dot,
                MinorGridlineColor = OxyColors.LightGray,
                MinorGridlineThickness = 0.5
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
                StringFormat = "HH:mm"
            });
            this.PressureModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Ciśnienie [Bar]",
                Maximum = _settings.ScaleFactors.PressureMax,
                Minimum = _settings.ScaleFactors.PressureMin,
                IsZoomEnabled = false,
                IsPanEnabled = false,
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = OxyColors.LightGray,
                MajorGridlineThickness = 1,
                MinorGridlineStyle = LineStyle.Dot,
                MinorGridlineColor = OxyColors.LightGray,
                MinorGridlineThickness = 0.5
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
                StringFormat = "HH:mm"
            });
            this.WeightModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Waga [kg]",
                Maximum = _settings.ScaleFactors.WeightMax,
                Minimum = _settings.ScaleFactors.WeightMin,
                IsZoomEnabled = false,
                IsPanEnabled = false,
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = OxyColors.LightGray,
                MajorGridlineThickness = 1,
                MinorGridlineStyle = LineStyle.Dot,
                MinorGridlineColor = OxyColors.LightGray,
                MinorGridlineThickness = 0.5
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
                DegassingButtonEnabled = true;
                SaturationButtonEnabled = true;
                HardeningButtonEnabled = true;
            }
            else
            {
                StartButtonEnabled = false;
                StopButtonEnabled = false;
                DegassingButtonEnabled = false;
                SaturationButtonEnabled = false;
                HardeningButtonEnabled = false;
            }
        }

        private void PlcService_OnDataReceived() // Aktualizacja danych z PLC i odświeżenie wykresów
        {
            UpdateSensorFields();
            //UpdateWeightChange(); // Za często, pokazuje się waga chwilowa, a nie zmiana wagi od początku pomiaru
            UpdateAlarmsView();
            UpdateWarningsView();
            UpdateDegassingTimeRemaining();

            if (!_DBWriteActive || (_dataBaseService == null) || (_dataBaseService._dataStore == null))
                return;

            if (_DBWriteTicksCounter == 0) //wyliczony na podstawie nastawy odczytu PLC i nastawy interwału zapisu do DB
            {
                UpdatePlotsAfterPLCDataRcv();
                UpdateDBWrite();
                UpdateWeightChange();
            }
            _DBWriteTicksCounter++;

            if (_DBWriteTicksCounter >= _DBWriteTicksNeeded) //Liczba przy której nastąpi wpis do DB
            {
                _DBWriteTicksCounter = 0; // Reset licznika
            }
        }


        public void UpdateAlarmsView()
        {
            UInt32 _changedErrors = _AlarmsOld ^ _plcConnectionService.Alarms; // bity które się zmieniły
            UInt32 _alarmsToAdd = _changedErrors & _plcConnectionService.Alarms;
            UInt32 _alarmsToRemove = _changedErrors & _AlarmsOld;

            foreach (var bit in GetSetBits(_alarmsToRemove))
            {
                for (int i = 0; i < AlarmsAndWarningsTextList.Count; i++)
                {
                    if (AlarmsAndWarningsTextList[i].Contains(AlarmsTextList[bit]))
                    {
                        AlarmsAndWarningsTextList.RemoveAt(i);
                        var _textLine = DateTime.Now.ToString("yy.dd.MM HH:mm:ss") + " - " + AlarmsTextList[bit];
                        SaveAlarmsAndWarningsToFile(_textLine + " (Outgoing)");
                    }
                }
            }

            foreach (var bit in GetSetBits(_alarmsToAdd))
            {
                var _textLine = DateTime.Now.ToString("yy.dd.MM HH:mm:ss") + " - " + AlarmsTextList[bit];
                AlarmsAndWarningsTextList.Add(_textLine);
                SaveAlarmsAndWarningsToFile(_textLine + " (Incoming)");
            }
            _AlarmsOld = _plcConnectionService.Alarms;
        }


        public void UpdateWarningsView()
        {
            UInt32 _changedErrors = _WarningsOld ^ _plcConnectionService.Warnings; // bity które się zmieniły
            UInt32 _warningsToAdd = _changedErrors & _plcConnectionService.Warnings;
            UInt32 _warningsToRemove = _changedErrors & _WarningsOld;

            foreach (var bit in GetSetBits(_warningsToRemove))
            {
                for (int i = 0; i < AlarmsAndWarningsTextList.Count; i++)
                {
                    if (AlarmsAndWarningsTextList[i].Contains(WarningsTextList[bit]))
                    {
                        AlarmsAndWarningsTextList.RemoveAt(i);
                        var _textLine = DateTime.Now.ToString("yy.dd.MM HH:mm:ss") + " - " + WarningsTextList[bit];
                        SaveAlarmsAndWarningsToFile(_textLine + " (Outgoing)");
                    }
                }
            }

            foreach (var bit in GetSetBits(_warningsToAdd))
            {
                var _textLine = DateTime.Now.ToString("yy.dd.MM HH:mm:ss") + " - " + WarningsTextList[bit];
                AlarmsAndWarningsTextList.Add(_textLine);
                SaveAlarmsAndWarningsToFile(_textLine + " (Incoming)");
            }
            _WarningsOld = _plcConnectionService.Warnings;
        }

        private void SaveAlarmsAndWarningsToFile(string _textLine) // Metoda do zapisywania aktualnych alarmów i ostrzeżeń do pliku tekstowego, jeśli chcesz mieć historię alarmów i ostrzeżeń
        {
            try
            {
                Directory.CreateDirectory(_settings.ReportsPath + @"\Errors Log");
                string fileName = _settings.ReportsPath + @"\Errors Log\" + $"AlarmsAndWarnings_{DateTime.Now:yyyyMM}.txt";
                File.AppendAllText(fileName, _textLine + Environment.NewLine);
            }
            catch
            { }
        }


        private IEnumerable<int> GetSetBits(uint value) //zamienia wartość bitową alarmów i ostrzeżeń na listę indeksów bitów, które są ustawione na 1, co pozwala na łatwe mapowanie tych indeksów do tekstu alarmów i ostrzeżeń z plików tekstowych
        {
            while (value != 0)
            {
                int bit = System.Numerics.BitOperations.TrailingZeroCount(value);
                yield return bit;
                value &= value - 1;
            }
        }

        private void UpdatePlotsAfterPLCDataRcv()
        {
            for (int i = 0; i < _TemperatureSeries.Length; i++)
            {
                AddPointToBoth(TemperatureModel, TemperatureModelCommon, DateTimeAxis.ToDouble(DateTime.Now), _plcConnectionService.Temperature[i], i);
                var s1 = (LineSeries)TemperatureModelMain.Series[i];
                s1.Points.Add(new DataPoint(DateTimeAxis.ToDouble(DateTime.Now), _plcConnectionService.Temperature[i]));
            }           

            for (int i = 0; i < _PressureSeries.Length; i++)
            {
                // _PressureSeries[i].Points.Add(new DataPoint(DateTimeAxis.ToDouble(DateTime.Now),_plcConnectionService.Pressure[i] ));
                AddPointToBoth(PressureModel, PressureModelCommon, DateTimeAxis.ToDouble(DateTime.Now), _plcConnectionService.Pressure[i], i);

            }            

            AddPointToBoth(WeightModel, WeightModelCommon, DateTimeAxis.ToDouble(DateTime.Now), _plcConnectionService.Weight, 0);

            var annotationText = CreateAnnotationText(DegassingIsChecked, SaturationIsChecked, HardeningIsChecked, CycleTime.ToString(@"hh\:mm\:ss") + " ");
            if (annotationText != null)
            {
                AddAnnotation(TemperatureModel, DateTimeAxis.ToDouble(DateTime.Now), annotationText);
                AddAnnotation(TemperatureModelCommon, DateTimeAxis.ToDouble(DateTime.Now), annotationText);
                AddAnnotation(TemperatureModelMain, DateTimeAxis.ToDouble(DateTime.Now), annotationText);
                AddAnnotation(PressureModel, DateTimeAxis.ToDouble(DateTime.Now), annotationText);
                AddAnnotation(PressureModelCommon, DateTimeAxis.ToDouble(DateTime.Now), annotationText);
                AddAnnotation(WeightModel, DateTimeAxis.ToDouble(DateTime.Now), annotationText);
                AddAnnotation(WeightModelCommon, DateTimeAxis.ToDouble(DateTime.Now), annotationText);
            }

            this.TemperatureModel.InvalidatePlot(true);
            TemperatureModelCommon.InvalidatePlot(true);
            TemperatureModelMain.InvalidatePlot(true);
            this.PressureModel.InvalidatePlot(true);
            this.PressureModelCommon.InvalidatePlot(true);
            this.WeightModel.InvalidatePlot(true);
            this.WeightModelCommon.InvalidatePlot(true);
        }

        private void UpdateDBWrite()
        {
            _dataBaseService.SavePomiar(new DataBaseService.Pomiar
            {
                Data = DateTime.Now,
                TimeSinceStart = (DateTime.Now - _startTime).ToString(@"hh\:mm\:ss"),
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
                Temp13 = (float)Math.Round(_plcConnectionService.Temperature[12], 1),
                Temp14 = (float)Math.Round(_plcConnectionService.Temperature[13], 1),
                Temp15 = (float)Math.Round(_plcConnectionService.Temperature[14], 1),
                Temp16 = (float)Math.Round(_plcConnectionService.Temperature[15], 1),
                Pressure1 = (float)Math.Round(_plcConnectionService.Pressure[0], 1),
                Pressure2 = (float)Math.Round(_plcConnectionService.Pressure[1], 1),
                Weight = (float)Math.Round(_plcConnectionService.Weight, 3),
                DegassingActive = DegassingIsChecked,
                SaturationActive = SaturationIsChecked,
                HardeningActive = HardeningIsChecked
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void UpdateSensorFields()
        {
            for (int i = 0; i < Temperature.Count; i++)
            {
                Temperature[i] = _plcConnectionService.Temperature[i].ToString("F0");
            }

            for (int i = 0; i < Pressure.Count; i++)
            {
                Pressure[i] = _plcConnectionService.Pressure[i].ToString("F0");
            }

            WeightText = _plcConnectionService.Weight.ToString("F3");
        }

        private void UpdateWeightChange()
        {
            var _weightChangeBetweenPLCReadings = _plcConnectionService.Weight * 1000 - _oldWeight; //przeskalowanie z kg na gramy
            _WeightChange = 60 / _settings.PLCPollingInterval * _weightChangeBetweenPLCReadings; //przeskalowanie zmiany wagi między odczytami PLC do zmiany wagi na minutę, czyli (60 sekund / nastawa odczytu PLC) * zmiana wagi między odczytami PLC
            OnPropertyChanged(nameof(ZmianaWagiText));
            _oldWeight = _plcConnectionService.Weight * 1000;

        }

        private void UpdateDegassingTimeRemaining()
        {
            // Implementacja aktualizacji czasu pozostałego do zakończenia odgazowania na podstawie wartości otrzymanych z PLC
            // Możesz tutaj przetłumaczyć wartość na tekst i zaktualizować interfejs użytkownika
            DegassingTimeRemained = _plcConnectionService.DegassingTimeRemained;
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

    public class ContainsAlarmConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s)
                return s.Contains("Alarm", StringComparison.OrdinalIgnoreCase);

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }


    public class SecondsToTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Int32 seconds)
            {
                TimeSpan time = TimeSpan.FromSeconds(seconds);
                string formatted = (time < TimeSpan.Zero ? "-" : "+") + ((int)time.Duration().TotalMinutes).ToString("000") + time.Duration().ToString(@"\:ss");
                // return time.ToString(@"mm\:ss");
                return formatted;
            }
            return value;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }


}
