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

        public static IHost? Host { get; private set; }

        public App()
        {
            // Budowa hosta DI
            Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {  
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
