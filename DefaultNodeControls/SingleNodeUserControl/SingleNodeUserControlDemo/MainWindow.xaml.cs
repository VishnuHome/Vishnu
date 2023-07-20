using System.Windows;

namespace Vishnu.DemoApplications.SingleNodeUserControlDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new DemoSingleNodeViewModel();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            (this.DataContext as DemoSingleNodeViewModel)?.RaiseAllProperties();
        }
    }
}
