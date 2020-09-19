using System;
using System.Windows.Data;
using System.Windows.Controls;

namespace Vishnu.WPF_UI.ValueConverter
{
    /// <summary>
    /// ValueConverter, wandelt Die Child-Anordnung eines Knotens (Horizontal, Vertical)
    ///           in eine ExpandDirection für den anzeigenden Expander (Down, Right).
    /// </summary>
    /// <remarks>
    /// File: ChildOrientationToExpandDirection.cs
    /// Autor: Erik Nagel
    ///
    /// 20.05.2013 Erik Nagel: erstellt
    /// </remarks>
    [ValueConversion(typeof(Orientation), typeof(ExpandDirection))]
    public class ChildOrientationToExpandDirection : IValueConverter
    {
        /// <summary>
        /// Wandelt Orientation in ExpandDirection
        /// ("Horizontal" in "Down" und "Vertical" in "Right").
        /// </summary>
        /// <param name="value">Orientation</param>
        /// <param name="targetType">ExpandDirection</param>
        /// <param name="parameter">Konvertierparameter</param>
        /// <param name="culture">Kultur</param>
        /// <returns>ExpandDirection (Down, Right)</returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((Orientation)value == Orientation.Horizontal)
            {
                return ExpandDirection.Down;
            }
            else
            {
                return ExpandDirection.Right;
            }
        }

        /// <summary>
        /// Wandelt ExpandDirection in Orientation.
        /// </summary>
        /// <param name="value">ExpandDirection</param>
        /// <param name="targetType">Orientation</param>
        /// <param name="parameter">Konvertierparameter</param>
        /// <param name="culture">Kultur</param>
        /// <returns>Orientation (Horizontal, Vertical)</returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
