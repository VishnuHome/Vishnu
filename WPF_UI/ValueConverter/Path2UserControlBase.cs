using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Data;
using Vishnu.Interchange;
using System.Windows;
using System.Collections.Concurrent;
using Vishnu.ViewModel;

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
    public class Path2UserControlBase : IMultiValueConverter
    {
        /// <summary>
        /// Instanziiert nach einer Pfadangabe aus einer dynamisch geladenen,
        /// externen dll ein Objekt vom Typ DynamicUserControlBase.
        /// </summary>
        /// <param name="values">Pfad zur externen Dll und aktueller DataContext.</param>
        /// <param name="targetType">DynamicUserControl.DynamicUserControlBase</param>
        /// <param name="parameter">Konvertierparameter</param>
        /// <param name="culture">Kultur</param>
        /// <returns>Instanz vom Typ DynamicUserControl.DynamicUserControlBase oder null.</returns>
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values != null && values.Count() > 0 && values[0] != null)
            {
                if (!String.IsNullOrEmpty((string)(values[0])))
                {
                    string path = "|" + (string)(values[0]);
                    object currentDatacontext = values[1];
                    if (values.Count() > 2 && values[2] != null)
                    {
                        path = values[2].ToString() + path;
                    }
                    KeyValuePair<string, object> dictionaryKey = new KeyValuePair<string, object>(path, currentDatacontext);
                    if (Path2UserControlBase.loadedControls.ContainsKey(dictionaryKey))
                    {
                        return Path2UserControlBase.loadedControls[dictionaryKey];
                    }
                    //var x = new DynamicUserControls.Interchange.DynamicUserControlBase();
                    //Type castingTarget = Type.GetType("DynamicUserControls.Interchange.DynamicUserControlBase, DynamicUserControls.Interchange");
                    Type castingTarget = typeof(DynamicUserControlBase);
                    object control = null;
                    if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                    {
                        try
                        {
                            control = VishnuAssemblyLoader.GetAssemblyLoader().DynamicLoadObjectOfTypeFromAssembly(path.Split('|')[1], castingTarget);
                        }
                        catch (TypeLoadException)
                        {
                            // Hier nur abfangen, da an dieser Stelle ansonsten "Vishnu funktioniert nicht mehr" folgen würde.
                        }
                        if (control == null)
                        {
                            //control = VishnuAssemblyLoader.GetAssemblyLoader().DynamicLoadObjectOfTypeFromAssembly(@"DefaultNodeControls\ConstantNodeUserControl.dll", castingTarget);
                            control = new NodeErrorControl() { DllPath = path.Split('|')[1] };
                        }
                    }
                    else
                    {
                        control = new DynamicUserControlBase();
                    }
                    if (control != null && control is DynamicUserControlBase)
                    {
                        (control as DynamicUserControlBase).DataContext = currentDatacontext;
                        Path2UserControlBase.loadedControls.TryAdd(dictionaryKey, control as DynamicUserControlBase);
                    }
                    return control;
                }
            }
            return null;
        }

        /// <summary>
        /// Ist nicht implenmentiert.
        /// </summary>
        /// <param name="value">Instanz vom Typ DynamicUserControl.DynamicUserControlBase.</param>
        /// <param name="targetType">String und object.</param>
        /// <param name="parameter">Konvertierparameter</param>
        /// <param name="culture">Kultur</param>
        /// <returns>Exception</returns>
        public object[] ConvertBack(object value, Type[] targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
        private static ConcurrentDictionary<KeyValuePair<string, object>, object> loadedControls;
        static Path2UserControlBase()
        {
            loadedControls = new ConcurrentDictionary<KeyValuePair<string, object>, object>();
        }
    }
}
