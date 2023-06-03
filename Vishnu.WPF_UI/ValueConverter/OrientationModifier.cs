using System;
using System.Linq;
using System.Windows.Data;
using System.Windows.Controls;
using Vishnu.Interchange;
using System.Windows;

namespace Vishnu.WPF_UI.ValueConverter
{
    /// <summary>
    /// ValueConverter, wandelt eine Orientation (Horizontal, Vertical)
    ///           abhängig von einem zusätzlichen Schalter vom Typ OrientationSwitch um:
    ///           Unchanged=unverändert, Switched=umgedreht, Horizontal=horizontal, Vertical=vertikal.
    /// </summary>
    /// <remarks>
    /// File: OrientationModifier.cs
    /// Autor: Erik Nagel
    ///
    /// 05.10.2014 Erik Nagel: erstellt
    /// 23.07.2022 Erik Nagel: Im DesignMode wird jetzt ein definierter Wert (Horizontal) zurückgegeben.
    ///                        Visual Studio 2022 zeigte vorher den Fehler "XDG0062 Die angegebene Umwandlung ist ungültig."
    ///                        auf dem besitzenden DataTemplate.
    /// </remarks>
    [ValueConversion(typeof(Orientation), typeof(ExpandDirection))]
    public class OrientationModifier : IMultiValueConverter
    {
        /// <summary>
        /// Wandelt eine Orientation (Horizontal, Vertical)
        ///           abhängig von einem zusätzlichen Schalter vom Typ OrientationSwitch um:
        ///           Unchanged=unverändert, Switched=umgedreht, Horizontal=horizontal, Vertical=vertikal.
        /// </summary>
        /// <param name="values">Array: [Orientation][OrientationSwitch].</param>
        /// <param name="targetType">Orientation</param>
        /// <param name="parameter">Konvertierparameter</param>
        /// <param name="culture">Kultur</param>
        /// <returns>ExpandDirection (Down, Right)</returns>
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                return Orientation.Horizontal;
            }
            if (values != null)
            {
                if (values.Count() > 0)
                {
                    Orientation inValue = (Orientation)values[0];
                    if (values.Count() > 1)
                    {
                        FrameworkElement owner = (FrameworkElement)values[1];
                        OrientationSwitch orientationSwitch = (OrientationSwitch)owner.FindResource("OrientationModificationSwitch");
                        switch (orientationSwitch)
                        {
                            case OrientationSwitch.Unchanged:
                                return inValue;
                            case OrientationSwitch.Switched:
                                return inValue == Orientation.Horizontal ? Orientation.Vertical : Orientation.Horizontal;
                            case OrientationSwitch.Horizontal:
                                return Orientation.Horizontal;
                            case OrientationSwitch.Vertical:
                                return Orientation.Vertical;
                            default:
                                break;
                        }
                    }
                    else
                    {
                        return inValue;
                    }
                }
            }
            throw new ArgumentException("OrientationModifier-Parameters are: Orientation, OrientationSwitch");
        }

        /// <summary>
        /// Wandelt ExpandDirection in Orientation.
        /// </summary>
        /// <param name="value">Orientation.</param>
        /// <param name="targetTypes">Array: [Orientation][OrientationSwitch].</param>
        /// <param name="parameter">Konvertierparameter</param>
        /// <param name="culture">Kultur</param>
        /// <returns>Array: [Orientation][OrientationSwitch].</returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
