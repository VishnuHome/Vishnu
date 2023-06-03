using System.Windows;
using Vishnu.DemoApplications.SingleNodeUserControlDemo;
using Vishnu.Interchange;
using Vishnu.ViewModel;

namespace Vishnu.UserControls
{
    /// <summary>
    /// Interaktionslogik für SingleNodeUserControl.xaml
    /// </summary>
    public partial class SingleNodeUserControlDerived : DynamicUserControlBase
    {
        /// <summary>
        /// Standard Konstruktor.
        /// </summary>
        public SingleNodeUserControlDerived()
        {
            // ToolTipService.ShowDurationProperty.OverrideMetadata(typeof(DependencyObject), new FrameworkPropertyMetadata(Int32.MaxValue));
            InitializeComponent();
        }

        /// <summary>
        /// Konkrete Überschreibung von GetUserResultViewModel, returnt ein spezifisches ResultViewModel.
        /// </summary>
        /// <param name="vishnuViewModel">Ein spezifisches ResultViewModel.</param>
        /// <returns></returns>
        protected override DynamicUserControlViewModelBase GetUserResultViewModel(IVishnuViewModel vishnuViewModel)
        {
            return new ResultViewModel((IVishnuViewModel)this.DataContext);
        }

        private delegate void NoArgDelegate();

        /// <summary>
        /// Sorgt im Kontext des aufrufenden Moduls durch das übergebene DependencyObject "obj"
        /// für ein neues Rendern des Controls.
        /// </summary>
        /// <param name="obj">Das aufrufende DependencyObject selbst.</param>
        public static void Refresh(DependencyObject obj)
        {
            obj.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, (NoArgDelegate)delegate { });
        }
    }
}
