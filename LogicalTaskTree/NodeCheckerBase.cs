using NetEti.Globals;
using Vishnu.Interchange;

namespace LogicalTaskTree
{
    /// <summary>
    /// Basisklasse für NodeChecker und ValueModifier;
    ///           implementiert INodeChecker; muss abgeleitet werden.
    /// </summary>
    /// <remarks>
    /// File: NodeCheckerBase.cs
    /// Autor: Erik Nagel
    ///
    /// 28.05.2013 Erik Nagel: erstellt
    /// </remarks>
    public abstract class NodeCheckerBase : NodeShellBase, INodeChecker, IValueModifier
    {
        #region public members

        #region INodeChecker members

        /// <summary>
        /// Wird aufgerufen, wenn sich der Verarbeitungs-Fortschritt des Checkers geändert hat.
        /// </summary>
        public event CommonProgressChangedEventHandler NodeProgressChanged;

        /// <summary>
        /// Rückgabe-Objekt des Checkers, kann null sein.
        /// </summary>
        public virtual object ReturnObject { get; set; }

        /// <summary>
        /// Der Pfad zum aktuell dynamisch zu ladenden UserControl.
        /// </summary>
        public abstract string UserControlPath { get; set; }

        /// <summary>
        /// Bei true spiegelt dieser Knoten die Werte eines referenzierten
        /// Knoten 1:1 wieder.
        /// </summary>
        internal virtual bool IsMirror
        {
            get
            {
                return this._isMirror;
            }
            set
            {
                this._isMirror = value;
            }
        }

        /// <summary>
        /// Bei True werden alle Knoten im Tree resettet, wenn dieser Knoten gestartet wird.
        /// Kann für Loops in Controlled-Jobs verwendet werden.
        /// Default: false.
        /// </summary>
        public bool InitNodes { get; set; }

        /// <summary>
        /// Verzögert den Start eines Knotens (und InitNodes).
        /// Kann für Loops in Controlled-Jobs verwendet werden.
        /// Default: 0 (Millisekunden).
        /// </summary>
        public int TriggeredRunDelay { get; set; }

        /// <summary>
        /// Name eines ursprünglich referenzierten Knotens oder null.
        /// </summary>
        public string ReferencedNodeName { get; internal set; }

        /// <summary>
        /// Bei True wird nach erfolgreichem globalen Locking
        /// kein weiterer Startversuch unternommen und das Locking
        /// aufgehoben.
        /// </summary>
        public bool IsInvalid { get; set; }

        /// <summary>
        /// Konstruktor.
        /// </summary>
        public NodeCheckerBase()
        {
            this.IsMirror = false;
            this.ReferencedNodeName = null;
            this.ThreadLocked = false;
            this.LockName = null;
            this.IsInvalid = false;
        }

        /// <summary>
        /// Hier wird der (normalerweise externe) Arbeitsprozess ausgeführt (oder beobachtet).
        /// </summary>
        /// <param name="checkerParameters">Spezifische Aufrufparameter oder null.</param>
        /// <param name="treeParameters">Für den gesamten Tree gültige Parameter oder null.</param>
        /// <param name="source">Auslösendes TreeEvent oder null.</param>
        /// <returns>True, False oder null</returns>
        public abstract bool? Run(object checkerParameters, TreeParameters treeParameters, TreeEvent source);

        /// <summary>
        /// Konvertiert einen Wert in ein gegebenes Format.
        /// Muss überschrieben werden.
        /// </summary>
        /// <param name="toConvert">Zu konvertierender Wert</param>
        /// <returns>Konvertierter Wert.</returns>
        public abstract object ModifyValue(object toConvert);

        #endregion INodeChecker members

        /// <summary>
        /// Ein optionaler Trigger, der den Job wiederholt aufruft
        /// oder null (setzt intern BreakWithResult auf false).
        /// Wird vom IJobProvider bei der Instanziierung mitgegeben.
        /// </summary>
        public virtual TriggerShell CheckerTrigger { get; set; }

        /// <summary>
        /// Ein optionaler Logger, der vom Knoten aufgerufen wird
        /// oder null.
        /// Wird vom IJobProvider bei der Instanziierung mitgegeben.
        /// </summary>
        public virtual LoggerShell CheckerLogger { get; set; }

        /// <summary>
        /// Das zuletzt zurückgegebene Ergebnis.
        /// </summary>
        public bool? LastReturned { get; set; }

        /// <summary>
        /// Bei true wird dieser Knoten als Referenzknoten angelegt, wenn irgendwo im Tree
        /// (nicht nur im aktuellen Job) der Name des Knotens schon gefunden wurde.
        /// Bei false wird nur im aktuellen Job nach gleichnamigen Knoten gesucht.
        /// Default: false.
        /// </summary>
        public bool IsGlobal { get; set; }

        /// <summary>
        /// Liefert den Namen des Checkers, der einem ValueConverter zugeordnet werden soll.
        /// </summary>
        /// <returns>Namen des Checkers, der dem ValueConverter zugeordnet werden soll oder null.</returns>
        public virtual string GetCheckerReference()
        {
            return null;
        }

        /// <summary>
        /// Übernimmt den Checker bei ValueConvertern.
        /// </summary>
        /// <param name="checker">Instanz des Checkers, der dem ValueConverter zugeordnet werden soll.</param>
        public virtual void SetChecker(NodeCheckerBase checker) { }

        #endregion public members

        #region internal members

        /// <summary>
        /// Bei True wird jeder Thread über die Klasse gesperrt, so dass
        /// nicht Thread-sichere Checker serialisiert werden;
        /// Default: False.
        /// </summary>
        internal bool ThreadLocked { get; set; }

        /// <summary>
        /// Optionaler zum globalen Sperren verwendeter Name.
        /// Wird verwendet, wenn ThreadLocked gesetzt ist.
        /// </summary>
        internal string LockName { get; set; }

        #endregion internal members

        #region protected members

        /// <summary>
        /// Bei true spiegelt dieser Knoten die Werte eines referenzierten
        /// Knoten 1:1 wieder.
        /// </summary>
        internal bool _isMirror;

        /// <summary>
        /// Wird angesprungen, wenn sich der Verarbeitungsfortschritt des Checkers geändert hat.
        /// </summary>
        /// <param name="sender">Der Checker-Prozess des Anwenders.</param>
        /// <param name="args">CommonProgressChangedEventArgs.</param>
        protected void SubNodeProgressChanged(object sender, CommonProgressChangedEventArgs args)
        {
            if (NodeProgressChanged != null)
            {
                NodeProgressChanged(sender, args);
            }
        }

        #endregion protected members

    }
}
