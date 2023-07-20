using System;
using System.Text;
using System.Reflection;
using NetEti.ObjectSerializer;

namespace Vishnu.Interchange
{
    /// <summary>
    /// Holt Properties aus einer vormals serialisierten Klasse,
    /// bei der ein Typecast (in dieselbe Klasse) nicht funktioniert.
    /// Wird bei eigenen Klassen benötigt, die innerhalb eines Vishnu-Trees
    /// als ReturnObject verwendet werden sollen und über das Environment
    /// von anderen Knoten als dem Erzeuger genutzt werden sollen.
    /// Tritt auf, wenn Assemblies einerseits deserialisiert geladen werden,
    /// andererseits aber in dynamisch geladenen Assemblies zu deren
    /// Compilezeit referenziert wurden.
    /// </summary>
    /// <remarks>
    /// File: GenericPropertyGetter.cs
    /// Autor: Erik Nagel
    ///
    /// 06.06.2014 Erik Nagel: erstellt
    /// </remarks>
    ///public static class GenericPropertyGetter
    public static class GenericPropertyGetter
    {
        /// <summary>
        /// Liefert Properties aus einer Klasse, die nach vormaliger Serialisierung nicht in dieselbe
        /// Klasse umgewandelt werden kann. Tritt bei dynamisch geladener Assembly auf.
        /// </summary>
        /// <typeparam name="T">Typ einer Property einer Instanz (instance), die wegen einer vormaligen Serialisierung
        /// nicht in ihren Ursprungstyp gecastet werden kann.</typeparam>
        /// <param name="instance">Instanz der Klasse, bei der ein direkter Typecast fehlschlägt.</param>
        /// <param name="name">Name der auszulesenden Property.</param>
        /// <returns>Gewünschte Property des Typs T.</returns>
        public static T? GetProperty<T>(object instance, string name)
        {
            object? propertyValue = null;
            bool found = false;
            Type type = instance.GetType();
            PropertyInfo[] propertyInfos = type.GetProperties();
            foreach (PropertyInfo propertyInfo in propertyInfos)
            {
                if (propertyInfo.Name == name)
                {
                    propertyValue = propertyInfo.GetValue(instance, null);
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                throw new TypeLoadException(String.Format("Die Property {0} wurde nicht gefunden.", name));
            }
            object? obj = null;
            if (propertyValue != null)
            {
                try
                {
                    string encoded = SerializationUtility.SerializeObjectToBase64String(propertyValue);
                    obj = SerializationUtility.DeserializeObjectFromBase64String(encoded);
                }
                catch (Exception ex)
                {
                    throw new TypeLoadException(String.Format("Die Property {0} ist nicht vom erwarteten Typ.", name), ex);
                }
            }
            return (T?)obj;
        }
    }
}
