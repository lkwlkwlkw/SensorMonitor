using SensorMonitor.ViewModels;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;



namespace SensorMonitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DateTime _lastInput = DateTime.Now; //Wykrywanie bezczynności
        private readonly DispatcherTimer _idleTimer;
        private bool _idleActionExecuted = false;

        public MainWindow(MainViewModel _ViewModel) //W konstruktorze wstrzykujemy ViewModel, który został zarejestrowany w kontenerze DI
        {
            InitializeComponent();
            DataContext = _ViewModel; //Ustawiamy DataContext okna na wstrzyknięty ViewModel, dzieki temu możemy korzystać z powiązań danych (data binding) w XAML

            _idleTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _idleTimer.Tick += (s, e) =>
            {
                if ((DateTime.Now - _lastInput).TotalSeconds >= 300 && !_idleActionExecuted)
                {
                    _idleActionExecuted = true;   // blokada ponownego wywołania
                    _ViewModel.OnClickPlotFormat(this);
                }
            };
            _idleTimer.Start();


        }

        private void TextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // tylko litery i cyfry
            Regex regex = new Regex("^[a-zA-Z0-9]+$");

            e.Handled = !regex.IsMatch(e.Text);
        }


        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            _lastInput = DateTime.Now;
            _idleActionExecuted = false;   // reset po aktywności
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _lastInput = DateTime.Now;
            _idleActionExecuted = false;   // reset po aktywności
        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            _lastInput = DateTime.Now;
            _idleActionExecuted = false;
        }
    }
}