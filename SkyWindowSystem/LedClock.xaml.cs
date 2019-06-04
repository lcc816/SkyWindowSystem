using System;
using System.Timers;
using System.Windows.Controls;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;
/// <summary>
/// codedajie 201609242014
/// </summary>
namespace clock
{
    [ValueConversion(typeof(String), typeof(String))]
    public class StringToResnameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return @"res\" + (string)value + ".png";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class DateTimeClockPropertyChanged : INotifyPropertyChanged
    {
        string hour1; public string Hour1 { get { return hour1; } set { hour1 = value; OnPropertyChanged(new PropertyChangedEventArgs("Hour1")); } }
        string hour2; public string Hour2 { get { return hour2; } set { hour2 = value; OnPropertyChanged(new PropertyChangedEventArgs("Hour2")); } }
        string minute1; public string Minute1 { get { return minute1; } set { minute1 = value; OnPropertyChanged(new PropertyChangedEventArgs("Minute1")); } }
        string minute2; public string Minute2 { get { return minute2; } set { minute2 = value; OnPropertyChanged(new PropertyChangedEventArgs("Minute2")); } }
        string second1; public string Second1 { get { return second1; } set { second1 = value; OnPropertyChanged(new PropertyChangedEventArgs("Second1")); } }
        string second2; public string Second2 { get { return second2; } set { second2 = value; OnPropertyChanged(new PropertyChangedEventArgs("Second2")); } }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(PropertyChangedEventArgs e) { PropertyChanged.Invoke(this, e); }
    }
    /// <summary>
    /// UserControl1.xaml 的交互逻辑
    /// </summary>
    public partial class LedClockControl : UserControl
    {
        public TimeSpan Span { set { _span = value; } }

        private TimeSpan _span=new TimeSpan();

        DateTimeClockPropertyChanged _dtcpc = new DateTimeClockPropertyChanged();

        private Timer _timer;

        public LedClockControl()
        {
            InitializeComponent();

            this._stackPanel.DataContext = _dtcpc;
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            _timer = new Timer();
            _timer.Interval = 1000;
            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            DateTime now = DateTime.Now + _span;
            string dtstring = now.Hour.ToString("00") + now.Minute.ToString("00") + now.Second.ToString("00");

          
            _dtcpc.Hour1 = dtstring.Substring(8, 1);
            _dtcpc.Hour2 = dtstring.Substring(9, 1);
            _dtcpc.Minute1 = dtstring.Substring(10, 1);
            _dtcpc.Minute2 = dtstring.Substring(11, 1);
            _dtcpc.Second1 = dtstring.Substring(12, 1);
            _dtcpc.Second2 = dtstring.Substring(13, 1);
        }
    }
}
