using System;
using System.Reflection;

namespace Vishnu.Interchange
{
    /// <summary>
    /// Basisklasse für spezifische Trigger - muss abgeleitet werden.
    /// Löst abhängig von der jeweiligen Implementierung das Event 'triggerIt' aus.
    /// Implementiert die Schnittstelle 'INodeTrigger' aus 'Vishnu.Interchange.dll', über
    /// die sich der LogicalTaskTree von 'Vishnu' in das Event einhängen und den Trigger
    /// starten und stoppen kann.
    /// </summary>
    /// <remarks>
    /// Autor: Erik Nagel
    ///
    /// 26.06.2021 Erik Nagel: erstellt.
    /// </remarks>
    public abstract class TriggerBase : INodeTrigger
    {
        #region public members

        #region INodeTrigger Implementation

        /// <summary>
        /// Enthält weitergehende Informationen zum Trigger.
        /// Implementiert sind NextRun und NextRunInfo. Für das Hinzufügen weiterer
        /// Informationen kann diese Property und/oder die Klasse TriggerInfo
        /// abgeleitet werden.
        /// </summary>
        public virtual TriggerInfo Info
        {
            get
            {
                string info = null;
                if (this._triggerActive)
                {
                    info = this._nextStart.ToString();
                    this._info.NextRun = this._nextStart;
                }
                else
                {
                    this._info.NextRun = DateTime.MinValue;
                }
                this._info.NextRunInfo = info;
                return this._info;
            }
            set
            {
            }
        }

        /// <summary>
        /// Startet den Trigger; vorher sollte sich der Consumer in triggerIt eingehängt haben.
        /// </summary>
        /// <param name="triggerController">Das Objekt, das den Trigger definiert.</param>
        /// <param name="triggerParameters">Trigger-spezifische Übergabeparameter. Werden als String von der
        /// JobDescription.xml übernommen und im Prinzip unverändert an den implementierten Trigger übergeben.
        /// Ausnahme: Vishnu versucht für in %-Zeichen eingeschlossene Zeichenketten eine Parameterersetzung.</param>
        /// <param name="triggerIt">Die aufzurufende Callback-Routine, wenn der Trigger feuert.</param>
        /// <returns>True, wenn der Trigger durch diesen Aufruf tatsächlich gestartet wurde (hier: immer true).</returns>
        public virtual bool Start(object triggerController, object triggerParameters, Action<TreeEvent> triggerIt)
        {
            this.EvaluateParametersOrFail(ref triggerParameters, triggerController);
            this._triggerIt += triggerIt;
            this._lastStart = DateTime.MinValue;
            this._nextStart = DateTime.MinValue;
            this._triggerActive = true;
            return true;
        }

        /// <summary>
        /// Stoppt den Trigger.
        /// </summary>
        /// <param name="triggerController">Das Objekt, das den Trigger definiert.</param>
        /// <param name="triggerIt">Die aufzurufende Callback-Routine, wenn der Trigger feuert.</param>
        public virtual void Stop(object triggerController, Action<TreeEvent> triggerIt)
        {
            this._triggerActive = false;
            this._lastStart = DateTime.Now;
            this._nextStart = DateTime.MinValue;
            this._triggerIt -= triggerIt;
        }

        #endregion INodeTrigger Implementation

        /// <summary>
        /// Standard Konstruktor.
        /// </summary>
        public TriggerBase()
        {
            this._info = new TriggerInfo() { NextRun = DateTime.MinValue, NextRunInfo = null };
            this._lastStart = DateTime.MinValue;
            this._nextStart = DateTime.MinValue;
            this._triggerActive = false;
            this._syntaxInformation = null;
        }

        #endregion public members

        #region protected members

        /// <summary>
        /// Die von TriggerBase abgeleitete Klasse kann diesen Namen setzen.
        /// Er wird dann später in OnTriggerFired im TreeEvent mitgegeben.
        /// </summary>
        protected string TriggerName { get; set; }

        /// <summary>
        /// Interne Repräsentation der Property "Info". 
        /// </summary>
        protected TriggerInfo _info;

        /// <summary>
        /// Enthält den Zeitpunkt des letzten Trigger-Starts oder DateTime.MinValue.
        /// </summary>
        protected DateTime _lastStart;

        /// <summary>
        /// Enthält den Zeitpunkt des nächsten Trigger-Starts, wenn dieser überhaupt
        /// vorhersehbar ist, ansonsten DateTime.MinValue.
        /// </summary>
        protected DateTime _nextStart;

        /// <summary>
        /// Kann mit Trigger-spezifischen Syntax-Informationen ausgestattet werden,
        /// wird dann im Fehlerfall im Zuge einer Exception ausgegeben, Default: null.
        /// </summary>
        protected string _syntaxInformation;

        /// <summary>
        /// Wird automatisch auf true gesetzt, wenn der besitzende Knoten im Vishnu-Tree
        /// vom Benutzer manuell gestartet wurde. Kann für die Steuerung spezifischen
        /// Trigger-Verhaltens genutzt werden.
        /// </summary>
        protected bool _isUserRun;

        /// <summary>
        /// Diese Routine löst das Trigger-Event aus.
        /// Für ein Setzen der Variablen "_lastStart" und "_nextStart" kann diese Routine
        /// überschrieben werden.
        /// </summary>
        /// <param name="dummy">Aus Kompatibilitätsgründen, wird hier nicht genutzt.</param>
        protected virtual void OnTriggerFired(long dummy)
        {
            string assemblyName = String.IsNullOrEmpty(this.TriggerName) ? "Trigger" : this.TriggerName;
            if (this._triggerIt != null)
            {
                this._triggerIt(new TreeEvent(TriggerName, TriggerName, this.Info.NextRunInfo,
                                  "", "", null, 0, null, null));
            }
        }

        /// <summary>
        /// Diese Routine wird von der Routine "Start" angesprungen, bevor der Trigger gestartet wird.
        /// Hier wird nur der Parameter "|UserRun" ausgewertet und die Variable "_isUserRun" entsprechend gesetzt.
        /// Für die Auswertung der eigentlichen Trigger-Parameter muss diese Routine überschrieben werden.
        /// Bei Fehlern in der Parameterauswertung kann die Routine "ThrowSyntaxException(string errorMessage)"
        /// aufgerufen werden.
        /// </summary>
        /// <param name="triggerParameters">Die von Vishnu weitergeleiteten Parameter aus der JobDescription.xml.</param>
        /// <param name="triggerController">Der Knoten, dem dieser Trigger zugeordnet ist.</param>
        protected virtual void EvaluateParametersOrFail(ref object triggerParameters, object triggerController)
        {
            this._isUserRun = false;
            if (triggerParameters == null || String.IsNullOrEmpty(triggerParameters.ToString()))
            {
                this.ThrowSyntaxException("Es wurden keine Parameter übergeben.");
            }
            string triggerParametersString = triggerParameters.ToString();
            if (triggerParametersString.EndsWith("|UserRun"))
            {
                this._isUserRun = true;
                triggerParametersString = triggerParametersString.Substring(0, triggerParametersString.Length - 8);
            }
            triggerParameters = triggerParametersString;
        }

        /// <summary>
        /// Wird aufgerufen, wenn die übergebenen Parameter fehlerhaft waren.
        /// </summary>
        /// <param name="errorMessage">Auslösender Fehler der Parameterprüfung.</param>
        protected void ThrowSyntaxException(string errorMessage)
        {
            string assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            string fullMessage = assemblyName + ": " + errorMessage;
            if (!String.IsNullOrEmpty(this._syntaxInformation))
            {
                fullMessage += Environment.NewLine + this._syntaxInformation;

            }
            throw new ArgumentException(fullMessage);
        }

        #endregion protected members

        #region private members

        /// <summary>
        /// Wird ausgelöst, wenn das Trigger-Ereignis (z.B. Timer) eintritt. 
        /// </summary>
        private event Action<TreeEvent> _triggerIt;
        private bool _triggerActive;

        #endregion private members
    }
}
