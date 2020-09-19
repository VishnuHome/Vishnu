using System;

namespace Vishnu.Interchange
{
    /// <summary>
    /// Interface für Klassen, die Parameter-Werte
    /// (string-&gt;string) zur Verfügung stellen.
    /// </summary>
    /// <remarks>
    /// File: IParameterReader.cs
    /// Autor: Erik Nagel
    ///
    /// 30.05.2015 Erik Nagel: erstellt
    /// </remarks>
    public interface IParameterReader
    {
        /// <summary>
        /// Event, das ausgelöst wird, wenn die Parameter neu geladen wurden.
        /// </summary>
        event EventHandler ParametersReloaded;

        /// <summary>
        /// Routine, die Startparameter übernimmt und den ParameterReader
        /// entsprechend konfiguriert; muss direkt nach Instanziierung
        /// aufgerufen werden.
        /// </summary>
        /// <param name="parameters">Ein Objekt zur Parameterübergabe.</param>
        void Init(object parameters);

        /// <summary>
        /// Liefert zu einem String-Parameter einen String-Wert.
        /// </summary>
        /// <param name="parameterName">Parameter-Name.</param>
        /// <returns>Parameter-Value.</returns>
        string ReadParameter(string parameterName);
    }
}
