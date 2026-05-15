using SensorMonitor.ViewModels;
using System.Windows;



namespace SensorMonitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel _ViewModel)
        {
            InitializeComponent();
            DataContext = _ViewModel;
        }

    }
}