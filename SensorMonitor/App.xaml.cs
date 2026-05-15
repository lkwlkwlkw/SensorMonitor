using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Win32.TaskScheduler;
using SensorMonitor.Services;
using SensorMonitor.ViewModels;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;

namespace SensorMonitor
{

    public partial class App : Application
    {
        private static Mutex _mutex;
        public static IHost Host { get; private set; }

        public App()
        {
            // Budowa hosta DI
            Host = Microsoft.Extensions.Hosting.Host
                .CreateDefaultBuilder()
                .ConfigureAppConfiguration(config =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    // Wczytanie sekcji AppSettings
                    services.Configure<AppSettings>(
                    context.Configuration.GetSection("AppSettings"));

                    // ViewModel
                    services.AddTransient<MainViewModel>();

                    // Okna
                    services.AddSingleton<MainWindow>();
                    services.AddSingleton<PLCConnectionService>();
                    services.AddSingleton<DataBaseService>();
                })
                .Build();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("pl-PL");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("pl-PL");
            const string mutexName = "SensorMonitor";
            bool createdNew;
            _mutex = new Mutex(true, mutexName, out createdNew);
            if (!createdNew)
            {
                // Aplikacja już działa
                MessageBox.Show("Aplikacja już działa.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }
            Directory.CreateDirectory(@"C:\Raporty\Obrazy");
            base.OnStartup(e);
            var mainWindow = Host!.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
            var plcService = Host!.Services.GetRequiredService<PLCConnectionService>();
            plcService.ConnectPLC();
            var appSettings = Host!.Services.GetRequiredService<IOptions<AppSettings>>().Value;

            if (appSettings.StartWithWindows)
            {
                RegisterOnStartup();
            }
            else
            {
                RemoveTask();
            }

        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (Host is not null)
                await Host.StopAsync();

            var plcService = Host!.Services.GetService<PLCConnectionService>();
            plcService?.DisconnectPLC();

            var DBService = Host!.Services.GetService<DataBaseService>();
            DBService?.DatabaseClose();

            base.OnExit(e);
        }

        private static void RegisterOnStartup(string taskName = "Sensor monitor")
        {
            string exePath = Process.GetCurrentProcess().MainModule.FileName;
            string exeDir = Path.GetDirectoryName(exePath);
            using (TaskService ts = new TaskService())
            {
                try
                {
                    // Tworzymy definicję zadania
                    TaskDefinition td = ts.NewTask();
                    td.RegistrationInfo.Description = "Automatyczne uruchamianie aplikacji przy starcie Windows";
                    td.Settings.DisallowStartIfOnBatteries = false; // pozwala uruchomić na baterii
                    td.Settings.StopIfGoingOnBatteries = false; // nie zatrzymuje na baterii
                    td.Settings.RunOnlyIfIdle = false;
                    td.Settings.RunOnlyIfNetworkAvailable = false;
                    td.Settings.Enabled = true;
                    td.Settings.ExecutionTimeLimit = TimeSpan.Zero; // brak limitu czasu wykonania                    
                    td.Triggers.Add(new LogonTrigger
                    {
                        UserId = Environment.UserName
                    });
                    // Uruchamianie jako aktualny użytkownik z GUI
                    td.Principal.UserId = Environment.UserName;
                    td.Principal.LogonType = TaskLogonType.InteractiveToken;
                    td.Principal.RunLevel = TaskRunLevel.Highest;
                    // Akcja: uruchom aplikację
                    td.Actions.Add(new ExecAction(exePath, null, exeDir));
                    // Rejestracja zadania (nadpisuje jeśli istnieje)

                    ts.RootFolder.RegisterTaskDefinition(taskName, td);
                }
                catch (Exception ex)
                {
                    // obsługa błędu
                    MessageBox.Show($"Błąd: {ex.Message}");
                }




            }
        }

        private static bool RemoveTask(string taskName = "Sensor monitor")
        {
            try
            {
                using (TaskService ts = new TaskService())
                {
                    var task = ts.GetTask(taskName);

                    if (task == null)
                        return false; // zadanie nie istnieje

                    ts.RootFolder.DeleteTask(taskName, false);
                    return true; // usunięto
                }
            }
            catch (Exception ex)
            {
                // tu możesz dodać logowanie błędów
                Debug.WriteLine("Błąd podczas usuwania zadania: " + ex.Message);
                return false;
            }
        }

    }

}
