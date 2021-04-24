using System;
using System.Collections.Generic;
using System.Linq;
using NetEti.Globals;
using System.IO;
using System.Text;

namespace Vishnu.Interchange
{
    /// <summary>
    /// Klasse mit diversen Informationen für Ereignisse im LogicalTaskTree.
    /// </summary>
    /// <remarks>
    /// File: TreeEvent.cs
    /// Autor: Erik Nagel
    ///
    /// 25.07.2013 Erik Nagel: erstellt
    /// </remarks>
    public class TreeEvent
    {
        #region public members

        /// <summary>
        /// Mappt einen normalisierten String mit entsprechendeninternen Ereignis-Namen
        /// auf einen String mit durch Pipe ('|') getrennte Benutzer-freundliche Ereignis-Namen.
        /// </summary>
        /// <param name="internalEventNames">String mit durch '|' getrennten Programm-seitigen Event-Namen.</param>
        /// <returns>String mit durch '|' getrennten entsprechenden Benutzer-freundlichen Event-Namen.</returns>
        public static string GetUserEventNamesForInternalEventNames(string internalEventNames)
        {
            string userEventsString;
            if (_internalEventNamesToUserEventNamesCache.ContainsKey(internalEventNames.ToUpper()))
            {
                userEventsString = _internalEventNamesToUserEventNamesCache[internalEventNames.ToUpper()];
            }
            else
            {
                List<string> userEvents = new List<string>();
                foreach (string item in internalEventNames.ToUpper().Split(new char[] { '|' }))
                {
                    string userEvent = GetUserEventNameForInternalEventName(item);
                    if (!String.IsNullOrEmpty(userEvent))
                    {
                        userEvents.Add(userEvent);
                    }
                }
                userEvents.Sort();
                userEventsString = string.Join("|", userEvents.ToArray());
                if (userEventsString.Length > 0)
                {
                    _internalEventNamesToUserEventNamesCache.Add(internalEventNames.ToUpper(), userEventsString);
                }
            }
            return userEventsString;
        }

        /// <summary>
        /// Mappt einen internen Ereignis-Namen auf einen Benutzer-freundlichen Ereignis-Namen.
        /// </summary>
        /// <param name="internalEventName">Der Programm-seitige Event-Name.</param>
        /// <returns>Der Benutzer-freundliche Event-Name.</returns>
        public static string GetUserEventNameForInternalEventName(string internalEventName)
        {
            if (_internalEventNames_userEventNames.ContainsKey(internalEventName.ToUpper()))
            {
                return _internalEventNames_userEventNames.SingleOrDefault(c => c.Key == internalEventName.ToUpper()).Value;
            }
            return null;
        }

        /// <summary>
        /// Mappt einen String mit durch Pipe ('|') getrennte Benutzer-freundliche Ereignis-Namen
        /// auf einen normalisierten String mit entsprechendenb internen Ereignis-Namen.
        /// </summary>
        /// <param name="userEventNames">String mit durch '|' getrennten Benutzer-freundlichen Event-Namen.</param>
        /// <returns>Der normalisierte String mit durch '|' getrennten entsprechenden Programm-seitigen Event-Namen.</returns>
        public static string GetInternalEventNamesForUserEventNames(string userEventNames)
        {
            string internalEventsString;
            if (_userEventNamesToInternalEventNamesCache.ContainsKey(userEventNames.ToUpper()))
            {
                internalEventsString = _userEventNamesToInternalEventNamesCache[userEventNames.ToUpper()];
            }
            else
            {
                List<string> internalEvents = new List<string>();
                foreach (string item in userEventNames.ToUpper().Split(new char[] { '|' }))
                {
                    string internalEvent = GetInternalEventNameForUserEventName(item);
                    if (!String.IsNullOrEmpty(internalEvent))
                    {
                        internalEvents.Add(internalEvent);
                    }
                }
                internalEvents.Sort();
                internalEventsString = string.Join("|", internalEvents.ToArray());
                if (internalEventsString.Length > 0)
                {
                    _userEventNamesToInternalEventNamesCache.Add(userEventNames.ToUpper(), internalEventsString);
                }
            }
            return internalEventsString;
        }

        /// <summary>
        /// Mappt  einen Benutzer-freundlichen Ereignis-Namen auf einen internen Ereignis-Namen.
        /// </summary>
        /// <param name="userEventName">Der Benutzer-freundliche Event-Name.</param>
        /// <returns>Der Programm-seitige Event-Name.</returns>
        public static string GetInternalEventNameForUserEventName(string userEventName)
        {
            if (_userEventNames_internalEventNames.ContainsKey(userEventName.ToUpper()))
            {
                return _userEventNames_internalEventNames[userEventName.ToUpper()];
            }
            return null;
            //return userEventName;
        }

        /// <summary>User-Name des Ereignisses</summary>
        public string Name { get; set; }

        /// <summary>Datum und Uhrzeit des Ereignisses</summary>
        public DateTime Timestamp { get; internal set; }

        /// <summary>Id des zugehörigen Threads</summary>
        public string ThreadId { get; internal set; }

        /// <summary>Id des Knotens, in dem das Ereignis auftritt.</summary>
        public string SourceId { get; set; }

        /// <summary>Id des Knotens, der das Ereignis meldet.</summary>
        public string SenderId { get; set; }
        
        /// <summary>Name des Knotens, der das Ereignis meldet.</summary>
        public string NodeName { get; internal set; }
        
        /// <summary>Pfad zum Knoten, der das Ereignis meldet.</summary>
        public string NodePath { get; internal set; }
        
        /// <summary>Logischer Wert des Knotens, der das Ereignis meldet (true, false oder null).</summary>
        public bool? Logical { get; internal set; }
        
        /// <summary>Verarbeitungszustand des Knotens, der das Ereignis meldet (None, Start, Done, Fault, Timeout, UserAbort.).</summary>
        public string State { get; internal set; }
        
        /// <summary>Liste mit Verarbeitungsergebnissen des Knotens, der das Ereignis meldet.</summary>
        public ResultDictionary Results { get; set; }
        
        /// <summary>Liste mit Verarbeitungsergebnissen der Vorläufer des Knotens, der das Ereignis meldet.</summary>
        public ResultDictionary Environment { get; set; }

        /// <summary>
        /// Konstruktor: übernimmt und erzeugt diverse Informationen für das TreeEvent.
        /// </summary>
        /// <param name="name">User-freundlicher Name des Events.</param>
        /// <param name="sourceId">Id des Knotens, in dem das Ereignis auftritt.</param>
        /// <param name="senderId">Id des feuernden Knoten.</param>
        /// <param name="nodeName">Name des feuernden Knoten.</param>
        /// <param name="nodePath">Pfad zum feuernden Knoten.</param>
        /// <param name="lastLogical">Letztes gültiges logisches Ergebnis des Knotens (True oder False).</param>
        /// <param name="logicalState">Verarbeitungszustand des Knotens (None, Start, Done, Fault, Timeout, UserAbort).</param>
        /// <param name="results">List of Result (Verarbeitungsergebnisse der untergeordneten INodeChecker).</param>
        /// <param name="environment">List of Result (Verarbeitungsergebnisse der untergeordneten INodeChecker der vorhergehenden Knoten).</param>
        public TreeEvent(string name, string sourceId, string senderId, string nodeName, string nodePath, bool? lastLogical, NodeLogicalState logicalState, ResultDictionary results, ResultDictionary environment)
        {
            this.Name = name;
            this.SourceId = sourceId;
        
            this.SenderId = senderId;
            this.NodeName = nodeName;
            this.NodePath = nodePath;
            this.Logical = lastLogical; // == null ? "null" : lastLogical.ToString();
            this.State = logicalState.ToString();
            this.Results = results;
            this.Environment = environment;
            this.Timestamp = DateTime.Now;
            this.ThreadId = ThreadInfos.GetThreadInfos();
        }

        // Alter Kommentar: Achtung: diese String-Ersetzung verursacht Memory-Leaks! Deshalb nicht in Loops verwenden.
        // 14.01.2018 Nagel: Neue Tests konnten keine Memory-Leaks aufzeigen (siehe BasicAppSettingsDemo).
        /// <summary>
        /// Ersetzt definierte Wildcards durch ihre Laufzeit-Werte:
        /// '%HOME%': '...bin\Debug'.
        /// </summary>
        /// <param name="inString">Wildcard</param>
        /// <returns>Laufzeit-Ersetzung</returns>
        public string ReplaceWildcards(string inString)
        {
            string replaced = GenericSingletonProvider.GetInstance<AppSettings>().GetStringValue("__NOPPES__", inString);
            // Regex.Replace(inString, @"%HOME%", AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\'), RegexOptions.IgnoreCase);
            return replaced;
        }

        /// <summary>
        /// Löst den übergebenen Pfad unter Berücksichtigung der Suchreihenfolge
        /// in einen gesicherten Pfad auf, wenn möglich.
        /// </summary>
        /// <param name="path">Pfad zur Dll/Exe.</param>
        /// <returns>Gesicherter Pfad zur Dll/Exe</returns>
        /// <exception cref="IOException" />
        public string GetResolvedPath(string path)
        {
            return AppSettings.GetResolvedPath(path);
        }

        /// <summary>
        /// Überschriebene ToString()-Methode.
        /// </summary>
        /// <returns>Id des Knoten + ":" + ReturnObject.ToString()</returns>
        public override string ToString()
        {
            StringBuilder rtn = new StringBuilder(this.Name);
            // Timestamp
            // ThreadId
            rtn.Append(": ");
            rtn.Append(this.SourceId);
            rtn.Append("/");
            rtn.Append(this.SenderId);
            rtn.Append(", ");
            rtn.Append(this.Logical);
            rtn.Append(", ");
            rtn.Append(this.State);
            //NodeName
            rtn.Append(", ");
            rtn.Append(this.NodePath);
            return rtn.ToString();
        }

        /// <summary>
        /// Vergleicht Dieses Result mit einem übergebenen Result nach Inhalt.
        /// Der Timestamp wird bewusst nicht in den Vergleich einbezogen.
        /// </summary>
        /// <param name="obj">Vergleichs-Result.</param>
        /// <returns>True, wenn das übergebene Result inhaltlich (ohne Timestamp) gleich diesem Result ist.</returns>
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            if (Object.ReferenceEquals(this, obj))
            {
                return true;
            }
            TreeEvent res = obj as TreeEvent;
            if (res.Name == this.Name && res.SourceId == this.SourceId && res.SenderId == this.SenderId
                && res.Logical == this.Logical && res.State == this.State
                && res.NodePath == this.NodePath)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Erzeugt einen eindeutigen Hashcode für dieses Result.
        /// Der Timestamp wird bewusst nicht in den Vergleich einbezogen.
        /// </summary>
        /// <returns>Ein möglichst eindeutiger Integer für alle Properties einer Instanz zusammen.</returns>
        public override int GetHashCode()
        {
            return (this.ToString()).GetHashCode();
        }

        #endregion public members

        #region private members

        // Achtung, die internalEventNames müssen so gewählt werden, dass sie sich nicht gegenseitig enthalten.
        private static Dictionary<string, string> _userEventNames_internalEventNames
              = new Dictionary<string, string>() {
        {"LOGICALCHANGED", "LastLogicalChanged"},
        {"RESULTCHANGED", "LastResultChanged"},
        {"LOGICALRESULTCHANGED", "LastNotNullLogicalChanged"},
        {"TRUE", "LastNotNullLogicalToTrue"},
        {"FALSE", "LastNotNullLogicalToFalse"},
        {"ANYLOGICALRESULTCHANGED", "AnyLastNotNullLogicalHasChanged"},
        {"STATECHANGED", "LogicalStateChanged"},
        {"PROGRESSCHANGED", "ProgressChanged"},
        {"PARAMETERSRELOADED", "ParametersReloaded"},
        {"USERSTART", "UserRun"},
        {"USERBREAK", "UserBreak"},
        {"STARTED", "Started"},
        {"BREAKED", "Breaked"},
        {"FINISHED", "Finished"},
        {"EXCEPTION", "AnyException"}
              };

        private static Dictionary<string, string> _internalEventNames_userEventNames
          = new Dictionary<string, string>() {
        {"LASTLOGICALCHANGED", "LogicalChanged"},
        {"LASTRESULTCHANGED", "ResultChanged"},
        {"LASTNOTNULLLOGICALCHANGED", "LogicalResultChanged"},
        {"LASTNOTNULLLOGICALTOTRUE", "True"},
        {"LASTNOTNULLLOGICALTOFALSE", "False"},
        {"ANYLASTNOTNULLLOGICALHASCHANGED", "AnyLogicalResultChanged"},
        {"LOGICALSTATECHANGED", "StateChanged"},
        {"PROGRESSCHANGED", "ProgressChanged"},
        {"PARAMETERSRELOADED", "ParametersReloaded"},
        {"USERRUN", "UserStart"},
        {"USERBREAK", "UserBreak"},
        {"STARTED", "Started"},
        {"BREAKED", "Breaked"},
        {"FINISHED", "Finished"},
        {"ANYEXCEPTION", "Exception"}
          };

        private static Dictionary<string, string> _internalEventNamesToUserEventNamesCache = new Dictionary<string, string>();
        private static Dictionary<string, string> _userEventNamesToInternalEventNamesCache = new Dictionary<string, string>();

        #endregion private members

    }
}
