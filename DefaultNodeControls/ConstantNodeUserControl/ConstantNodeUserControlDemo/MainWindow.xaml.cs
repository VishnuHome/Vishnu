using System.Windows;

namespace Vishnu.DemoApplications.ConstantNodeUserControlDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new DemoConstantNodeViewModel();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ((DemoConstantNodeViewModel)this.DataContext)?.RaiseAllProperties();
        }
    }
}
