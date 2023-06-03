using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows;

namespace Vishnu.WPF_UI.ValueConverter
{
  /// <summary>
  /// ValueConverter, wandelt einen nullable bool in Farben:
  /// null: gelb, true: grün, false: rot.
  /// </summary>
  /// <remarks>
  /// File: NullableBoolToBarBrush.cs
  /// Autor: Erik Nagel
  ///
  /// 05.01.2013 Erik Nagel: erstellt
  /// </remarks>
  [ValueConversion(typeof(bool?), typeof(Brush))]
  public class NullableBoolToBarBrush : IValueConverter
  {
    /// <summary>
    /// Wandelt einen nullable bool in Farben (SolidColorBrush):
    /// null: gelb, true: grün, false: rot.
    /// </summary>
    /// <param name="value">Nullable-Bool</param>
    /// <param name="targetType">Brush-Typ</param>
    /// <param name="parameter">Konvertierparameter</param>
    /// <param name="culture">Kultur</param>
    /// <returns>Color (gelb, grün, rot)</returns>
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if ((bool?)value == true)
      {
        //return new SolidColorBrush(Color.FromArgb(176, 0, 255, 0));
        return new SolidColorBrush((Color)Application.Current.Resources["TrueColor"]);
      }
      else
      {
        if ((bool?)value == false)
        {
          //return new SolidColorBrush(Color.FromArgb(176, 255, 0, 0));
          return new SolidColorBrush((Color)Application.Current.Resources["FalseColor"]);
        }
        else
        {
          //return new SolidColorBrush(Color.FromArgb(176, 0, 255, 0));
          return new SolidColorBrush((Color)Application.Current.Resources["NullBarColor"]);
        }
      }
    }

    /// <summary>
    /// Wandelt die Farben gelb, grün, rot (Color)
    /// in null, true, false.
    /// </summary>
    /// <param name="value">Color (gelb, grün, rot)</param>
    /// <param name="targetType">Color-Typ</param>
    /// <param name="parameter">Konvertierparameter</param>
    /// <param name="culture">Kultur</param>
    /// <returns>Nullable-Bool</returns>
    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
