using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Vishnu.Interchange;

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
    public partial class LogicalTaskJobGroupsControl : DynamicUserControlBase
    {
        /// <summary>
        /// Konstruktor: erzeugt eine Instanz von LogicalTaskTreeViewModel
        /// und setzt den DataContext darauf.
        /// </summary>
        public LogicalTaskJobGroupsControl()
        {
            InitializeComponent();
        }
    }
}
