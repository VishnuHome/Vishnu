using System;
using System.Collections.Generic;
using System.Windows;
using Vishnu.Interchange;
using System.Windows.Markup;
using System.ComponentModel;

namespace Vishnu.WPF_UI.DependencyProperties
{
    /// <summary>
    /// DependencyObject-Hilfsklasse für die DependencyProperty
    /// OrientationModification.
    /// </summary>
    /// <remarks>
    /// File: BinaryModificationSwitch
    /// Autor: Erik Nagel
    ///
    /// 07.10.2014 Erik Nagel: erstellt
    /// </remarks>
    [ContentProperty("OrientationModification"), TypeConverter(typeof(OrientationSwitchTypeConverter))]
    public class BinaryModificationSwitch : DependencyObject
    {
        /// <summary>
        /// HilfsProperty für die Parametrisierung des ValueConverters 'OrientationModifier':
        /// Unchanged=unverändert, Switched=umgedreht (Horizontal in => Vertical out und umgekehrt),
        /// Horizontal=immer horizontal, Vertical=immer vertikal.
        /// </summary>
        public static readonly DependencyProperty OrientationModificationProperty =
           DependencyProperty.Register("OrientationModification",
           typeof(OrientationSwitch),
           typeof(BinaryModificationSwitch));

        /// <summary>
        /// Property für die OrientationModificationProperty. 
        /// </summary>
        public OrientationSwitch OrientationModification
        {
            get
            {
                return (OrientationSwitch)GetValue(OrientationModificationProperty);
            }
            set
            {
                SetValue(OrientationModificationProperty, value);
            }
        }
    }

    /// <summary>
    /// Converter-Hilfsklasse für BinaryModificationSwitch.
    /// Konvertiert OrientationSwitch zu string und umgekehrt.
    /// </summary>
    /// <remarks>
    /// File: BinaryModificationSwitch.cs
    /// Autor: Erik Nagel
    ///
    /// 12.10.2014 Erik Nagel: erstellt
    /// </remarks>
    public class OrientationSwitchTypeConverter : TypeConverter
    {
        private static List<OrientationSwitch> defaultValues = new List<OrientationSwitch>();

        /// <summary>
        /// Statischer Konstruktor - füllt defaultValues mit OrientationSwitch.Unchanged.
        /// </summary>
        static OrientationSwitchTypeConverter()
        {
            defaultValues.Add(OrientationSwitch.Unchanged);
        }

        /// <summary>
        /// Liefert True, wenn der übergebene sourceType konvertiert werden könnte
        /// (also vom Typ string ist).
        /// </summary>
        /// <param name="context">WPF-Kontext.</param>
        /// <param name="sourceType">Auf Konvertierbarkeit zu prüfender Source-Typ</param>
        /// <returns>True, wenn sourceType von diesem TypeConverter konvertiert werden kann (also bei string).</returns>
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }
            return base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        /// Liefert True, wenn in den übergebenen destinationType konvertiert werden könnte
        /// (der destinationType also vom Typ string ist).
        /// </summary>
        /// <param name="context">WPF-Kontext.</param>
        /// <param name="destinationType">Auf Konvertierbarkeit zu prüfender Source-Typ</param>
        /// <returns>True, wenn dieser TypeConverter in den destinationType konvertieren kann (also bei string).</returns>
        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
        {
            if (destinationType == typeof(string))
            {
                return true;
            }
            return base.CanConvertTo(context, destinationType);
        }

        /// <summary>
        /// Konvertiert von string nach OrientationSwitch.
        /// </summary>
        /// <param name="context">WPF-Kontext.</param>
        /// <param name="culture">Länder-Info.</param>
        /// <param name="value">Der zu konvertierende Wert (string).</param>
        /// <returns>Der konvertierte Wert (OrientationSwitch).</returns>
        public override object? ConvertFrom(
            ITypeDescriptorContext? context,
            System.Globalization.CultureInfo? culture,
            object value)
        {
            string? text = value.ToString();
            if (text != null)
            {
                try
                {
                    return Enum.Parse(typeof(OrientationSwitch), text);
                }
                catch (Exception e)
                {
                    throw new Exception(
                        String.Format("Cannot convert '{0}' ({1}) because {2}",
                                        value,
                                        value.GetType(),
                                        e.Message), e);
                }
            }
            return base.ConvertFrom(context, culture, value);
        }

        /// <summary>
        /// Konvertiert von OrientationSwitch nach string.
        /// </summary>
        /// <param name="context">WPF-Kontext.</param>
        /// <param name="culture">Länder-Info.</param>
        /// <param name="value">Der zu konvertierende Wert (OrientationSwitch).</param>
        /// <param name="destinationType">Der Typ, in den konvertiert werden soll (string).</param>
        /// <returns>Der konvertierte Wert (string).</returns>
        public override object? ConvertTo(
            ITypeDescriptorContext? context,
            System.Globalization.CultureInfo? culture,
            object? value,
            Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException("destinationType");
            }
            if (value != null)
            {
                OrientationSwitch os = (OrientationSwitch)value;
                return os.ToString();
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <summary>
        /// Liefert True, wenn Standartwerte für die Konvertierung angeboten werden.
        /// </summary>
        /// <param name="context">WPF-Kontext.</param>
        /// <returns>True, wenn Standartwerte für die Konvertierung angeboten werden.</returns>
        public override bool GetStandardValuesSupported(ITypeDescriptorContext? context)
        {
            return true;
        }

        /// <summary>
        /// Eine optionale Sammlung von Standardwerten für die Konvertierung.
        /// </summary>
        /// <param name="context">WPF-Kontext.</param>
        /// <returns>Sammlung von Standardwerten für die Konvertierung.</returns>
        public override TypeConverter.StandardValuesCollection GetStandardValues(
            ITypeDescriptorContext? context)
        {
            StandardValuesCollection svc = new StandardValuesCollection(defaultValues);
            return svc;
        }
    }

}
