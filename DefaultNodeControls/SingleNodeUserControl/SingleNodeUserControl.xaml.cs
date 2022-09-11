using Vishnu.Interchange;

namespace Vishnu.UserControls
{
    /// <summary>
    /// Interaktionslogik für SingleNodeUserControl.xaml
    /// </summary>
    public partial class SingleNodeUserControl : DynamicUserControlBase
    {
        /// <summary>
        /// Standard Konstruktor.
        /// </summary>
        public SingleNodeUserControl()
        {
            // ToolTipService.ShowDurationProperty.OverrideMetadata(typeof(DependencyObject), new FrameworkPropertyMetadata(Int32.MaxValue));
            InitializeComponent();
        }
    }
}
