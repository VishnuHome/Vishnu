using System;
using System.Windows.Data;
using Vishnu.ViewModel;

namespace Vishnu.WPF_UI.ValueConverter
{
    /// <summary>
    /// ValueConverter, wandelt einen nullable bool in Farben:
    /// null: gelb, true: grün, false: rot.
    /// </summary>
    /// <remarks>
    /// File: NullableBoolToBrush.cs
    /// Autor: Erik Nagel
    ///
    /// 05.01.2013 Erik Nagel: erstellt
    /// </remarks>
    [ValueConversion(typeof(VisualNodeWorkerState), typeof(string))]
    public class VisualNodeWorkerStateToText : IValueConverter
    {
        /// <summary>
        /// Wandelt einen VisualNodeWorkerState in Text:
        /// none: " ", Valid: "W", Invalid: "X".
        /// </summary>
        /// <param name="value">VisualNodeWorkerState</param>
        /// <param name="targetType">string-Typ</param>
        /// <param name="parameter">Konvertierparameter</param>
        /// <param name="culture">Kultur</param>
        /// <returns>Text (" ", "W", "X")</returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((VisualNodeWorkerState)value == VisualNodeWorkerState.None)
            {
                return " ";
            }
            else
            {
                if ((VisualNodeWorkerState)value == VisualNodeWorkerState.Valid)
                {
                    return "W";
                }
                else
                {
                    return "X";
                }
            }
        }

        /// <summary>
        /// Wandelt die Farben gelb, grün, rot (SolidColorBrush)
        /// in null, true, false.
        /// </summary>
        /// <param name="value">SolidColorBrush (gelb, grün, rot)</param>
        /// <param name="targetType">Brush-Typ</param>
        /// <param name="parameter">Konvertierparameter</param>
        /// <param name="culture">Kultur</param>
        /// <returns>Nullable-Bool</returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
