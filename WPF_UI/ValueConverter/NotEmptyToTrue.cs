using System;
using System.Windows.Data;

namespace Vishnu.WPF_UI.ValueConverter
{
    /// <summary>
    /// ValueConverter, wandelt einen nicht leeren String in Boolean True.
    /// </summary>
    /// <remarks>
    /// File: NotEmptyToTrue.cs
    /// Autor: Erik Nagel
    ///
    /// 30.05.2013 Erik Nagel: erstellt
    /// </remarks>
    [ValueConversion(typeof(String), typeof(Boolean))]
    public class NotEmptyToTrue : IValueConverter
    {
        /// <summary>
        /// Wandelt einen nicht leeren String in Boolean true.
        /// ("" oder Null: true, alles andere: false).
        /// </summary>
        /// <param name="value">Zu prüfenderString</param>
        /// <param name="targetType">Boolean</param>
        /// <param name="parameter">Konvertierparameter</param>
        /// <param name="culture">Kultur</param>
        /// <returns>True, wenn der String nicht leer ist.</returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!String.IsNullOrEmpty((string)value))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Wandelt ExpandDirection in Orientation.
        /// </summary>
        /// <param name="value">Boolean</param>
        /// <param name="targetType">String</param>
        /// <param name="parameter">Konvertierparameter</param>
        /// <param name="culture">Kultur</param>
        /// <returns>Orientation (Horizontal, Vertical)</returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
