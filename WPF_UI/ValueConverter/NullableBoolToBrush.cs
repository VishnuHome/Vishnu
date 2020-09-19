using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

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
    [ValueConversion(typeof(bool?), typeof(Brush))]
    public class NullableBoolToBrush : IMultiValueConverter
    {
        /// <summary>
        /// Wandelt einen nullable bool in Farben (SolidColorBrush):
        /// null: gelb, true: grün, false: rot.
        /// </summary>
        /// <param name="values">Nullable-Bool und besitzendes FrameworkElement.</param>
        /// <param name="targetType">Brush-Typ</param>
        /// <param name="parameter">Konvertierparameter</param>
        /// <param name="culture">Kultur</param>
        /// <returns>SolidColorBrush (gelb, grün, rot)</returns>
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                return new SolidColorBrush(Colors.Orange);
            }
            FrameworkElement owner = values[1] as FrameworkElement;
            try
            {
                if (!(owner is NetEti.CustomControls.CustomProgressBar))
                {
                    if ((bool?)values[0] == true)
                    {
                        return new SolidColorBrush((Color)owner.FindResource("TrueColor"));
                    }
                    else
                    {
                        if ((bool?)values[0] == false)
                        {
                            return new SolidColorBrush((Color)owner.FindResource("FalseColor"));
                        }
                        else
                        {
                            return new SolidColorBrush((Color)owner.FindResource("NullColor"));
                        }
                    }
                }
                else
                {
                    if ((bool?)values[0] == true)
                    {
                        return new SolidColorBrush((Color)owner.FindResource("TrueBarColor"));
                    }
                    else
                    {
                        if ((bool?)values[0] == false)
                        {
                            return new SolidColorBrush((Color)owner.FindResource("FalseBarColor"));
                        }
                        else
                        {
                            return new SolidColorBrush((Color)owner.FindResource("NullBarColor"));
                        }
                    }
                }
            }
            catch (ResourceReferenceKeyNotFoundException)
            {
                return new SolidColorBrush(Colors.LightGray);
            }
        }

        /// <summary>
        /// Wandelt die Farben gelb, grün, rot (SolidColorBrush)
        /// in null, true, false.
        /// </summary>
        /// <param name="value">SolidColorBrush (gelb, grün, rot)</param>
        /// <param name="targetTypes">Nullable-Bool und besitzendes FrameworkElement.</param>
        /// <param name="parameter">Konvertierparameter</param>
        /// <param name="culture">Kultur</param>
        /// <returns>Nullable-Bool</returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }
}
