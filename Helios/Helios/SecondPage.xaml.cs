using System.Diagnostics;
using Windows.Phone.UI.Input;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Shapes;

namespace Helios
{
    public sealed partial class SecondPage
    {
        public SecondPage()
        {
            InitializeComponent();
            this.Loaded += SecondPage_Loaded;
        }

        void SecondPage_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            Debug.WriteLine("W: " + this.ActualWidth + " H: " + this.ActualHeight);
        }
    }
}