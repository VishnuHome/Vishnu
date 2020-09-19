using System.Windows.Controls;

namespace Vishnu.WPF_UI
{
    /// <summary>
    /// Interaktionslogik für LogicalTaskJobGroupsControl.xaml
    /// </summary>
    /// <remarks>
    /// File: LogicalTaskJobGroupsControl.xaml.cs
    /// Autor: Erik Nagel
    ///
    /// 01.10.2014 Erik Nagel: erstellt
    /// </remarks>
    public partial class LogicalTaskJobGroupsControl : UserControl
    {
        /// <summary>
        /// Konstruktor: erzeugt eine Instanz von LogicalTaskTreeViewModel
        /// und setzt den DataContext darauf.
        /// </summary>
        public LogicalTaskJobGroupsControl()
        {
            InitializeComponent();
            //this.ScreenBorder.MaxWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
        }

    }
}
