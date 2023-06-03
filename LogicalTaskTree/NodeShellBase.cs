using System;
using Vishnu.Interchange;

namespace LogicalTaskTree
{
    /// <summary>
    /// Basisklasse für CheckerShell, TriggerShell, WorkerShell.
    /// Stellt Parameter-Ersetzung zur Verfügung.
    /// Berücksichtigt eine lokale IParameterReplacer-Dll oder eine solche
    /// im UserAssemblies Verzeichnis.
    /// Stellt bool CanRun() zur Verfügung.
    /// Berücksichtigt eine lokale ICanRun-Dll oder eine solche
    /// im UserAssemblies Verzeichnis (Hinweis: IParameterReplacer und
    /// ICanRun können auch von einer gemeinsamen dll implementiert werden).
    /// </summary>
    /// <remarks>
    /// File: NodeShellBase.cs
    /// Autor: Erik Nagel
    ///
    /// 30.05.2015 Erik Nagel: erstellt
    /// </remarks>
    public class NodeShellBase : ICanRun
    {
        #region public members

        #region ICanRun members

        /// <summary>
        /// Wird von Vishnu vor jedem Run eines Checkers, Workers oder vor
        /// Start eines Triggers aufgerufen.
        /// Returnt true, wenn der Run/Start ausgewführt werden kann.
        /// </summary>
        /// <param name="parameters">Aufrufparameter des Benutzers.</param>
        /// <param name="treeParameters">Interne Parameter des Trees.</param>
        /// <param name="source">Aufrufewndes TreeEvent.</param>
        /// <returns>True, wenn der Run/Start ausgeführt werden kann.</returns>
        public bool CanRun(ref object? parameters, TreeParameters treeParameters, TreeEvent source)
        {
            if (this.CanRunDll == null)
            {
                return true;
            }
            else
            {
                return this.CanRunDll.CanRun(ref parameters, treeParameters, source);
            }
        }

        #endregion ICanRun members

        /// <summary>
        /// Pfad zu einer optionalen Dll, die eine ICanRun-Instanz zur Verfügung stellt.
        /// Wenn vorhanden, wird vor jedem Run des zugehörigen Checkers oder Workers
        /// CanRun aufgerufen. Liefert CanRun false zurück, wird der Start abgebrochen.
        /// Zusätzlich können in CanRun die übergebenen Parameter noch modifiziert werden.
        /// </summary>
        public string? CanRunDllPath
        {
            get
            {
                return this._canRunDllPath;
            }
            set
            {
                if (this._canRunDllPath != value)
                {
                    this._canRunDllPath = value;
                    this.loadCanRunDll();
                }
            }
        }

        /// <summary>
        /// Standard-Konstruktor: prüft, ob ICanRun-dll und/oder IParameterReader-dll
        /// im aktuellen- oder UserAssemblies-Verzeichnis existieren und lädt diese.
        /// </summary>
        public NodeShellBase()
        {
            this._canRunDllPath = null;
            this.CanRunDll = null;
            this.CanRunLoadingException = null;
        }

        #endregion public members

        #region internal members

        /// <summary>
        /// Wenn ungleich null, dann ist beim dynamischen Laden der Dll ein Fehler aufgetreten.
        /// </summary>
        internal ApplicationException? CanRunLoadingException { get; set; }

        #endregion internal members

        #region protected members

        /// <summary>
        /// Dll mit der Instanz von ICanRun. Wenn vorhanden, wird CanRun vor jedem Start
        /// eines Knotens aufgerufen. Liefert CanRun false zurück, wir der Start abgebrochen.
        /// In CanRun können darüber hinaus die per ref übergebenen Parameter noch
        /// modifiziert werden.
        /// </summary>
        protected ICanRun? CanRunDll { get; private set; }

        #endregion protected members

        #region private members

        private string? _canRunDllPath;

        private void loadCanRunDll()
        {
            if (!String.IsNullOrEmpty(this.CanRunDllPath) && this.CanRunDll == null)
            {
                this.CanRunDll = (ICanRun?)VishnuAssemblyLoader.GetAssemblyLoader()
                        .DynamicLoadObjectOfTypeFromAssembly(this.CanRunDllPath, typeof(ICanRun));
                if (this.CanRunDll == null)
                {
                    this.CanRunLoadingException = new ApplicationException(String.Format("Fehler beim Laden von {0}.", this.CanRunDllPath));
                }
            }
        }

        #endregion private members
    }
}
