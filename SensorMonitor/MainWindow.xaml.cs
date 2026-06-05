using SensorMonitor.ViewModels;
using System.Text.RegularExpressions;
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

        private void TextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // tylko litery i cyfry
            Regex regex = new Regex("^[a-zA-Z0-9]+$");

            e.Handled = !regex.IsMatch(e.Text);
        }
    }
}