using System.Xml.Linq;

namespace Vishnu.Interchange
{
    /// <summary>
    /// Interface für eine Klasse, die ein Fremdformat in ein XDocument
    /// im Vishnu-Job-Format konvertiert und retourniert.
    /// </summary>
    /// <remarks>
    /// File: IVishnuJobProvider.cs
    /// Autor: Erik Nagel
    ///
    /// 15.02.2015 Erik Nagel: erstellt
    /// </remarks>
    public interface IVishnuJobProvider
    {
        /// <summary>
        /// Lädt eine Jobbeschreibung in einem Fremdformat, konvertiert diese
        /// in eine Vishnu JobDescription.xml und retourniert diese als XDocument.
        /// </summary>
        /// <param name="jobDirectory">Das Job-Verzeichnis.</param>
        /// <returns>Job-Beschreibung im Vishnu-Format als XDocument.</returns>
        XDocument GetVishnuJobXml(string jobDirectory);
    }
}
