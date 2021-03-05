using System;
using System.Globalization;
using System.Windows.Data;

namespace Vishnu.WPF_UI.ValueConverter
{

    /// <summary>
    /// Compares two Properties (Thanks to Jason Tyler).
    /// </summary>
    /// <remarks>
    /// Author: Jason Tyler on https://stackoverflow.com/questions/37302270/comparing-two-dynamic-values-in-datatrigger
    ///
    /// 03.03.2021 Erik Nagel, original from Jason Tyler.
    /// </remarks>
    public class EqualityConverter: IMultiValueConverter
    {
        /// <summary>
        /// MultiValueConverter - returns true, if two values are equal.
        /// </summary>
        /// <param name="values">Array of two Property-values.</param>
        /// <param name="targetType">Object.</param>
        /// <param name="parameter">Optional convert-parameters, null here.</param>
        /// <param name="culture">Globalization.CultureInfo.</param>
        /// <returns></returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2)
                return false;

            return values[0].Equals(values[1]);

        }

        /// <summary>
        /// There is no way back => not implemented.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetTypes"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
