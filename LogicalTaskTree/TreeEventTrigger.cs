using NetEti.ApplicationControl;
using System;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Timers;
using Vishnu.Interchange;

namespace LogicalTaskTree
{
    /// <summary>
    /// Trigger für interne Events aus dem Tree.
    /// Macht Tree-Events für Trigger "von außen" nutzbar und erweitert
    /// so das Trigger-Einsatzspektrum.
    /// Ein TreeEventTrigger triggert 1 bis n Checker.
    /// </summary>
    /// <remarks>
    /// File: TreeEventTrigger.cs
    /// Autor: Erik Nagel
    ///
    /// 19.11.2013 Erik Nagel: erstellt
    /// </remarks>
    public class TreeEventTrigger : INodeTrigger
    {
        #region public members

        #region INodeTrigger members

        /// <summary>
        /// Enthält Informationen zum besitzenden Trigger.
        /// Implementiert sind NextRun und NextRunInfo. Für das Hinzufügen weiterer
        /// Informationen kann diese Klasse abgeleitet werden.
        /// </summary>
        public TriggerInfo? Info
        {
            get
            {
                string info = TreeEvent.GetUserEventNamesForInternalEventNames(this.InternalEvents);
                if (!String.IsNullOrEmpty(this.ReferencedNodeId))
                {
                    info = info + " (Knoten: " + this.ReferencedNodeId + ")";
                }
                if (this._eventTimer != null && this._eventTimer.Enabled)
                {
                    info = info + " oder: " + this._nextTimerStart.ToString();
                    this._info.NextRun = this._nextTimerStart;
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
        /// Setzt den internen Trigger auf aktiv.
        /// </summary>
        /// <param name="triggerController">Das Objekt, das den Trigger aufruft.</param>
        /// <param name="triggerParameters">Spezifische Aufrufparameter oder null.</param>
        /// <param name="triggerIt">Die aufzurufende Callback-Routine, wenn der Trigger feuert.</param>
        /// <returns>True, wenn der Trigger durch diesen Aufruf tatsächlich gestartet wurde.</returns>
        public bool Start(object? triggerController, object? triggerParameters, Action<TreeEvent> triggerIt)
        {
            this.TriggerIt += triggerIt;
            if (this._eventTimer != null)
            {
                this._lastTimerStart = DateTime.Now;
                this._nextTimerStart = this._lastTimerStart.AddMilliseconds(this._timerInterval);
                this._eventTimer.Start();
            }
            this.IsActive = true;
            return true;
        }

        /// <summary>
        /// Setzt den internen Trigger auf deaktiv.
        /// </summary>
        /// <param name="triggerController">Das Objekt, das den Trigger aufruft.</param>
        /// <param name="triggerIt">Die aufzurufende Callback-Routine, wenn der Trigger feuert.</param>
        public void Stop(object? triggerController, Action<TreeEvent> triggerIt)
        {
            this.IsActive = false;
            this._eventTimer?.Stop();
            this.TriggerIt -= triggerIt;
            this.LastTreeEvent = null;
        }

        #endregion INodeTrigger members

        /// <summary>
        /// Ersetzt das Starten und Stoppen bei internen Triggern.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Ein normalisierter String mit durch '|' getrennten internen Event-Namen.
        /// </summary>
        public string InternalEvents { get; set; }

        /// <summary>
        /// Id des Knotens, der von dem Event getriggert wird
        /// (nicht des Knotens, der das Event auslöst).
        /// </summary>
        public string OwnerId { get; set; }

        /// <summary>
        /// Das letzte aufgetretene TreeEvent für diesen Trigger oder null.
        /// </summary>
        public TreeEvent? LastTreeEvent { get; set; }

        /// <summary>
        /// Enthält den Namen des referenzierten Originalknotens.
        /// </summary>
        public string ReferencedNodeId { get; internal set; }

        /// <summary>
        /// Konstruktor - übernimmt und erzeugt diverse Informationen für den TreeEventTrigger.
        /// </summary>
        /// <param name="internalEvents">String mit durch '|' getrennten Programm-seitigen Event-Namen.</param>
        /// <param name="ownerId">Id des besitzenden Knoten.</param>
        /// <param name="originalReference">Wenn mit einer gültigen Zeitdauer gefüllt, dann feuert der Trigger nach
        /// Ablauf dieser Zeit auf jeden Fall. Gültige Zeitangaben sind (Regex): "(?:MS|S|M|H|D):\d+".</param>
        /// <param name="referencedNodeId">Id des Knoten, auf dessen Ereignisse der Trigger hört.</param>
        public TreeEventTrigger(string internalEvents, string ownerId, string originalReference, string referencedNodeId)
        {
            this._eventTimer = null;
            this._lastTimerStart = DateTime.MinValue;
            this._nextTimerStart = DateTime.MinValue;
            this._info = new TriggerInfo() { NextRun = DateTime.MinValue, NextRunInfo = null };
            this._textPattern = @"(?:MS|S|M|H|D):\d+";
            this._compiledPattern = new Regex(_textPattern);
            this.InternalEvents = internalEvents;
            this.ReferencedNodeId = referencedNodeId;
            this.LastTreeEvent = null;

            MatchCollection alleTreffer;
            alleTreffer = _compiledPattern.Matches(originalReference);
            this._timerInterval = 0;
            if (alleTreffer.Count > 0)
            {
                string subKey = alleTreffer[0].Groups[0].Value;
                switch (subKey.Split(':')[0])
                {
                    case "MS": this._timerInterval = Convert.ToInt32(subKey.Split(':')[1]); break;
                    case "S": this._timerInterval = Convert.ToInt32(subKey.Split(':')[1]) * 1000; break; ;
                    case "M": this._timerInterval = Convert.ToInt32(subKey.Split(':')[1]) * 1000 * 60; break; ;
                    case "H": this._timerInterval = Convert.ToInt32(subKey.Split(':')[1]) * 1000 * 60 * 60; break; ;
                    case "D": this._timerInterval = Convert.ToInt32(subKey.Split(':')[1]) * 1000 * 60 * 60 * 24; break; ;
                    default:
                        throw new ArgumentException("Falsche Einheit, zulässig sind: MS=Millisekunden, S=Sekunden, M=Minuten, H=Stunden, D=Tage.");
                }
                this._eventTimer = new System.Timers.Timer(this._timerInterval);
                this._eventTimer.Elapsed += new ElapsedEventHandler(eventTimer_Elapsed);
                this._eventTimer.Stop();
            }
            this.OwnerId = ownerId;
            this.IsActive = false;
        }

        private void eventTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            this.OnTriggerFired(null);
        }

        #endregion public members

        #region internal members

        /// <summary>
        /// Trigger-Event auslösen.
        /// </summary>
        /// <param name="source">Der Auslöser des Ereignisses oder null (bei Timer-Induzierung).</param>
        internal void OnTriggerFired(TreeEvent? source)
        {
            this.LastTreeEvent = source;
            this._eventTimer?.Stop();
            if (this.IsActive && this.TriggerIt != null)
            {
                this.TriggerIt(source);
                // InfoController.GetInfoPublisher().Publish(this, String.Format($"TreeEventTrigger.OnTriggerFired {source.SourceId}/{source.SenderId} ({source.Name})"), InfoType.NoRegex);
            }
            if (this._eventTimer != null)
            {
                this._lastTimerStart = DateTime.Now;
                this._nextTimerStart = this._lastTimerStart.AddMilliseconds(this._timerInterval);
                try
                {
                    this._eventTimer.Start();
                }
                catch (NullReferenceException ex)
                {
                    InfoController.GetInfoPublisher().Publish(source, String.Format($"TreeEventTrigger.OnTriggerFired: {ex.Message}"), InfoType.Exception);
                    // Trat bisher nur im Debugger (und selten) auf, TODO: genaue Analyse und endgültige Lösung.
                }
            }
        }

        #endregion internal members

        #region private members

        private System.Timers.Timer? _eventTimer;
        private int _timerInterval;
        private string _textPattern;
        private Regex _compiledPattern;
        private DateTime _lastTimerStart;
        private DateTime _nextTimerStart;
        private TriggerInfo _info;

        /// <summary>
        /// Wird ausgelöst, wenn das Trigger-Ereignis eintritt. 
        /// </summary>
        private event Action<TreeEvent?>? TriggerIt;

        #endregion private members
    }
}
