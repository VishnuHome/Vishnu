using System;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Windows;
using Vishnu.ViewModel;

namespace Vishnu.WPF_UI.ValueConverter
{

    /// <summary>
    /// Setzt einen Enum-Typ in ein BitmapImage um.
    /// </summary>
    /// <remarks>
    /// File: VisualNodeStatToBitmapImage
    /// Autor: Erik Nagel
    ///
    /// 27.02.2013 Erik Nagel: erstellt
    /// </remarks>
    public class VisualNodeStateToBitmapImage : IMultiValueConverter
    {
        /// <summary>
        /// Übersetzt eine Knoten-Zustand (Enum) in ein Bild.
        /// </summary>
        /// <param name="values">Array: [Status des Knotens][ResourceDictionary mit BitmapImages].</param>
        /// <param name="targetType">Der Zieltyp (BitmapImage).</param>
        /// <param name="parameter">Wird nicht genutzt.</param>
        /// <param name="culture">Sprache, Sonderzeichen</param>
        /// <returns>BitmapImage.</returns>
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            VisualNodeState status = VisualNodeState.None;
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                return new BitmapImage(new Uri("../Media/checkmark_small.png", UriKind.Relative));
            }
            //try
            //{
                status = (VisualNodeState)(values[0]); // hier Exception, da values[0] == null ist;
            //}
            //catch
            //{
            //    // TODO: hier die eigentliche Ursache ermitteln
            //    // return DependencyProperty.UnsetValue;
            //}
            ResourceDictionary targets = (ResourceDictionary)(values[1]);
            switch (status)
            {
                case VisualNodeState.None:
                case VisualNodeState.Scheduled:
                case VisualNodeState.EventTriggered:
                case VisualNodeState.Waiting:
                case VisualNodeState.Working:
                case VisualNodeState.Error:
                case VisualNodeState.Aborted:
                case VisualNodeState.Done:
                case VisualNodeState.InternalError:
                    return (BitmapImage)(targets[status.ToString()]);
                /* Eine mögliche Alternative zum IMultiValueConverter wäre ein IValueConverter, der sich die
                   Referenz auf die UI selber holt und darüber die Referenz auf das entsprechende ResourceDictionary:
                  return (BitmapImage)(
                              ((ResourceDictionary)((FrameworkElement)Application.Current.MainWindow.FindName("LogicalTaskTreeControl1"))
                               .FindResource("BitmapImageDictionary")))
                              [((Vishnu.ViewModel.LogicalNodeViewModel.VisualNodeState)value).ToString()];
                */
                default:
                    return DependencyProperty.UnsetValue;
            }
        }

        /// <summary>
        /// Ist nicht implementiert.
        /// </summary>
        /// <param name="value">BitmapImage</param>
        /// <param name="targetTypes">Array: [Status des Knotens][ResourceDictionary mit BitmapImages].</param>
        /// <param name="parameter">Wird nicht genutzt.</param>
        /// <param name="culture">Sprache, Sonderzeichen</param>
        /// <returns>VisualNodeState als Object.</returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
