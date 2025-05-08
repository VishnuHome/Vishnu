using System.Xml.Linq;

namespace Vishnu.Interchange
{
    /// <summary>
    /// Interface für eine Klasse, die ein Fremdformat in einen
    /// Vishnu-JobContainer konvertiert und retourniert.
    /// </summary>
    /// <remarks>
    /// File: IVishnuJobProvider.cs
    /// Autor: Erik Nagel
    ///
    /// 15.02.2015 Erik Nagel: erstellt
    /// 07.04.2025 Erik Nagel: auf JobContainer umgestellt.
    /// </remarks>
    public interface IVishnuJobProvider
    {
        /// <summary>
        /// Lädt eine Jobbeschreibung in einem Fremdformat und
        /// konvertiert diese in einen Vishnu JobContainer.
        /// </summary>
        /// <param name="jobDirectory">Das Job-Verzeichnis.</param>
        /// <returns>Job-Beschreibung im Vishnu-Format als JobContainer.</returns>
        JobContainer GetVishnuJobDescription(string jobDirectory);
    }
}
