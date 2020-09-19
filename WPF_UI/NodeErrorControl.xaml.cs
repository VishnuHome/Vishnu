using System.Windows;
using Vishnu.Interchange;
using Vishnu.ViewModel;

namespace Vishnu.WPF_UI
{
    /// <summary>
    /// Interaktionslogik für NodeErrorControl.xaml
    /// </summary>
    public partial class NodeErrorControl : DynamicUserControlBase
    {
        /// <summary>
        /// Pfad zur Dll, die nicht geladen werden konnte.
        /// </summary>
        public string DllPath { get; set; }

        /// <summary>
        /// Standard Konstruktor.
        /// </summary>
        public NodeErrorControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// ViewModel für einen Ladefehler-Knoten.
        /// </summary>
        public NodeErrorViewModel UserResultViewModel { get; set; }

        private void ContentControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.UserResultViewModel = new NodeErrorViewModel((IVishnuViewModel)this.DataContext, this.DllPath);
            ((IVishnuViewModel)this.DataContext).UserDataContext = this.UserResultViewModel;
        }

    }
}
