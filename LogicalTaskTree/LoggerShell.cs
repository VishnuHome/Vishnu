using System;
using System.IO;
using Vishnu.Interchange;

namespace LogicalTaskTree
{
    /// <summary>
    /// Kapselt den Aufruf eines externen Loggers,
    /// der dynamisch als Dll-Plugin geladen wird.
    /// </summary>
    /// <remarks>
    /// File: LoggerShell.cs
    /// Autor: Erik Nagel
    ///
    /// 25.07.2013 Erik Nagel: erstellt
    /// </remarks>
    public class LoggerShell : INodeLogger, IDisposable
    {
        #region public members

        #region INodeLogger members

        /// <summary>
        /// Übergibt LogInfos an den externen Logger.
        /// </summary>
        /// <param name="loggerParameters">Spezifische Aufrufparameter oder null.</param>
        /// <param name="treeParameters">Für den gesamten Tree gültige Parameter oder null.</param>
        /// <param name="treeEvent">Informationen über den Knoten, in dem das Ereignis auftritt.</param>
        /// <param name="additionalEventArgs">Enthält z.B. beim Event 'Exception' die zugehörige Exception.</param>
        public void Log(object? loggerParameters, TreeParameters? treeParameters, TreeEvent treeEvent, object? additionalEventArgs)
        {
            if (this.LoggerParameters != null)
            {
                loggerParameters = this.LoggerParameters;
            }
            if (this._slaveLoggerShell != null)
            {
                this._slaveLoggerShell.Log(loggerParameters, treeParameters, treeEvent, additionalEventArgs);
            }
            else
            {
                lock (this._loggerLocker)
                {
                    // hot plug
                    if (!String.IsNullOrEmpty(this._slavePathName))
                    {
                        if (!File.Exists(this._slavePathName) || (File.GetLastWriteTime(this._slavePathName) != this._lastDllWriteTime))
                        {
                            this._slave = null;
                        }
                        if (this._slave == null)
                        {
                            this.Slave = this.dynamicLoadSlaveDll(this._slavePathName);
                        }
                        if (this._slave != null)
                        {
                            this._lastDllWriteTime = File.GetLastWriteTime(this._slavePathName);
                            try
                            {
                                this._slave.Log(loggerParameters, treeParameters, treeEvent, additionalEventArgs);
                            }
                            finally
                            {
                            }
                        }
                        else
                        {
                            throw new ApplicationException(String.Format("Load failure on assembly '{0}'", this._slavePathName));
                        }
                    }
                }
            }
        }

        #endregion INodeLogger members

        #region IDisposable Implementation

        private bool _disposed; // = false wird vom System vorbelegt;

        /// <summary>
        /// Öffentliche Methode zum Aufräumen.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Hier wird aufgeräumt: ruft für alle User-Elemente, die Disposable sind, Dispose() auf.;
        /// </summary>
        /// <param name="disposing">Bei true wurde diese Methode von der öffentlichen Dispose-Methode
        /// aufgerufen; bei false vom internen Destruktor.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposed)
            {
                if (disposing)
                {
                    if (this._slave != null && this._slave is IDisposable)
                    {
                        try
                        {
                            ((IDisposable)this._slave).Dispose();
                        }
                        catch { };
                    }
                }
                this._disposed = true;
            }
        }

        /// <summary>
        /// Finalizer: wird vom GarbageCollector aufgerufen.
        /// </summary>
        ~LoggerShell()
        {
            this.Dispose(false);
        }

        #endregion IDisposable Implementation

        /// <summary>
        /// Liefert einen String mit Pipe-separierten Log-Event-Namen.
        /// Delegiert ggf. an die SlaveLoggerShell.
        /// </summary>
        /// <returns>String mit Pipe-separierten Log-Event-Namen</returns>
        public string GetLogEvents()
        {
            if (this._slaveLoggerShell != null)
            {
                return this._slaveLoggerShell.GetLogEvents();
            }
            else
            {
                return this.LogEvents;
            }
        }

        /// <summary>
        /// String mit durch '|' separierten Logging-Event-Namen.
        /// </summary>
        public string LogEvents { protected get; set; }

        /// <summary>
        /// String mit zusätzlichen Logger-Parametern.
        /// </summary>
        public string? LoggerParameters { get; set; }

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="slavePathName">Dateipfad und Name einer Dll, die INodeLogger implementiert.</param>
        /// <param name="loggerParameters">String mit durch '|' separierten Logging-Event Namen und optional ',' gefolgt von weiteren Logger-Parametern.</param>
        public LoggerShell(string slavePathName, string loggerParameters)
        {
            this._slavePathName = slavePathName;
            string[] para = (loggerParameters + ",").Split(',');
            this.LogEvents = TreeEvent.GetInternalEventNamesForUserEventNames(para[0]);
            this.LoggerParameters = null;
            if (!String.IsNullOrEmpty(para[1]))
            {
                this.LoggerParameters = para[1];
            }
            this._loggerLocker = new object();
            this._lastDllWriteTime = DateTime.MinValue;
            this._assemblyLoader = VishnuAssemblyLoader.GetAssemblyLoader();
        }

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="loggerShellReference">Name eines benannten Loggers.</param>
        public LoggerShell(string loggerShellReference)
        {
            this._assemblyLoader = VishnuAssemblyLoader.GetAssemblyLoader();
            this._loggerLocker = new object();
            this.LoggerShellReference = loggerShellReference;
            this._slaveLoggerShell = null;
            this.LogEvents = String.Empty;
        }

        /// <summary>
        /// Liefert den Namen des Loggers, der einem Knoten zugeordnet werden soll.
        /// </summary>
        /// <returns>Namen des Loggers, der dem Knoten zugeordnet werden soll oder null.</returns>
        public string? GetLoggerReference()
        {
            return this.LoggerShellReference;
        }

        /// <summary>
        /// Bei LoggerShells, die bei der Instanziierung nur eine Namensreferenz
        /// mitbekommen haben, wird hier nachträglich der Logger übergeben.
        /// </summary>
        /// <param name="loggerShell">Referenz auf den tatsächlichen Logger bei LoggerShells, die bei der Instanziierung
        /// nur eine Namensreferenz mitbekommen haben.</param>
        public void SetSlaveLoggerShell(LoggerShell loggerShell)
        {
            this._slaveLoggerShell = loggerShell;
        }

        #endregion public members

        #region internal members
        
        internal string? LoggerShellReference { get; set; }
        
        #endregion internal members

        #region private members

        // Instanz vom INodeLogger. Macht die Prüf-Arbeit.
        private INodeLogger? Slave
        {
            get
            {
                return this._slave;
            }
            set
            {
                this._slave = value;
            }
        }

        private string? _slavePathName;
        private INodeLogger? _slave;
        private DateTime _lastDllWriteTime;
        private VishnuAssemblyLoader _assemblyLoader;
        private object _loggerLocker;
        private LoggerShell? _slaveLoggerShell;

        // Lädt eine Plugin-Dll dynamisch. Muss keine Namespaces oder Klassennamen
        // aus der Dll kennen, Bedingung ist nur, dass eine Klasse in der Dll das
        // Interface INodeLogger implementiert. Die erste gefundene Klasse wird
        // als Instanz von INodeLogger zurückgegeben.
        private INodeLogger? dynamicLoadSlaveDll(string slavePathName)
        {
            return (INodeLogger?)this._assemblyLoader.DynamicLoadObjectOfTypeFromAssembly(slavePathName, typeof(INodeLogger));
        }

        #endregion private members

    }
}
