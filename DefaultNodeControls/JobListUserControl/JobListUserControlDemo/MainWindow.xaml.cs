using System.Windows;

namespace Vishnu.DemoApplications.JobListUserControlDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new DemoJobListViewModel();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ((DemoJobListViewModel)this.DataContext)?.RaiseAllProperties();
        }
    }
}
