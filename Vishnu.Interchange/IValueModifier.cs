namespace Vishnu.Interchange
{
    /// <summary>
    /// Helper für einen ValueModifier; konvertiert einen Wert
    /// in einen anderen Wert und/oder ein anderes Format.
    /// </summary>
    /// <remarks>
    /// File: IValueModifier
    /// Autor: Erik Nagel
    ///
    /// 23.06.2013 Erik Nagel: erstellt
    /// </remarks>
    public interface IValueModifier
    {
        /// <summary>
        /// Konvertiert einen Wert in das für diesen ValueModifier gültige Format.
        /// </summary>
        /// <param name="toConvert">Zu konvertierender Wert</param>
        /// <returns>Konvertierter Wert.</returns>
        object? ModifyValue(object? toConvert);
    }
}
