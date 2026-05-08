using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModernWpf.Controls;
using SensorMonitor.Services;
using SensorMonitor.ViewModels;
using System.Windows;
using Workstation.ServiceModel.Ua;

namespace SensorMonitor
{    

    public partial class App : Application
    {
        private UaApplication application;
        private static Mutex? _mutex;

        public static IHost? Host { get; private set; }

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





            base.OnStartup(e);      
            var mainWindow = Host!.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
            var plcService = Host!.Services.GetRequiredService<PLCConnectionService>();           
            plcService.ConnectPLC();
          
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
    }

}
