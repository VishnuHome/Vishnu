using System;
using System.IO;
using System.Diagnostics;
using Vishnu.Interchange;
using NetEti.Globals;

namespace LogicalTaskTree
{
    /// <summary>
    /// Kapselt den Aufruf einer externen Arbeitsroutine,
    /// die dynamisch als Dll-Plugin geladen wird.
    /// </summary>
    /// <remarks>
    /// File: CheckerShell.cs
    /// Autor: Erik Nagel
    ///
    /// 01.03.2013 Erik Nagel: erstellt
    /// </remarks>
    public class CheckerShell : NodeCheckerBase, IDisposable
    {
        #region public members

        /// <summary>
        /// Der Pfad zum aktuell dynamisch zu ladenden UserControl.
        /// </summary>
        public override string UserControlPath { get; set; }

        #region INodeChecker members

        /// <summary>
        /// Hier wird der (normalerweise externe) Arbeitsprozess ausgeführt (oder beobachtet).
        /// </summary>
        /// <param name="checkerParameters">Spezifische Aufrufparameter oder null.</param>
        /// <param name="treeParameters">Für den gesamten Tree gültige Parameter oder null.</param>
        /// <param name="source">Auslösendes TreeEvent oder null.</param>
        /// <returns>True, False oder null</returns>
        public override bool? Run(object checkerParameters, TreeParameters treeParameters, TreeEvent source)
        {
            if (this.CanRunLoadingException == null)
            {
                object actCheckerParameters = checkerParameters ?? this.CheckerParameters;
                if (this.CanRunDll == null || this.CanRunDll.CanRun(ref actCheckerParameters, treeParameters, source))
                {
                    if (actCheckerParameters != this._checkerParameters)
                    {
                        this._checkerParameters = GenericSingletonProvider.GetInstance<AppSettings>().ReplaceWildcards(actCheckerParameters.ToString());
                    }
                    if (this.ThreadLocked)
                    {
                        bool? res = null;
                        string lockName = this.LockName == null ? this._slaveName : this.LockName;
                        try
                        {
                            this.IsInvalid = false;
                            ThreadLocker.LockNameGlobal(lockName);
                            if (!this.IsInvalid)
                            {
                                res = this.runIt(checkerParameters, treeParameters, source);
                            }
                            else
                            {
                                this.IsInvalid = false;
                            }
                        }
                        finally
                        {
                            ThreadLocker.UnlockNameGlobal(lockName);
                        }
                        return res;
                    }
                    else
                    {
                        return this.runIt(checkerParameters, treeParameters, source);
                    }
                }
                else
                {
                    return this.LastReturned;
                }
            }
            else
            {
                this.ReturnObject = this.CanRunLoadingException;
                return null;
            }
        }

        #endregion INodeChecker members

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
                            (this._slave as IDisposable).Dispose();
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
        ~CheckerShell()
        {
            this.Dispose(false);
        }

        #endregion IDisposable Implementation

        /// <summary>
        /// Bei true spiegelt dieser Knoten die Werte eines referenzierten
        /// Knoten 1:1 wieder.
        /// </summary>
        internal override bool IsMirror
        {
            get
            {
                return this._isMirror;
            }
            set
            {
                this._isMirror = value;
                if (this._isMirror)
                {
                    if (this.CheckerTrigger != null && this.CheckerTrigger.HasTreeEventTrigger)
                    {
                        if (this.CheckerTrigger.TriggerShellReference.Contains("LogicalResultChanged"))
                        {
                            this.ReferencedNodeName = this.CheckerTrigger.ReferencedNodeName;
                        }
                    }
                }
                else
                {
                    this.ReferencedNodeName = null;
                }
            }
        }

        /// <summary>
        /// Pfad zur Dll der externen Checker-Routine.
        /// </summary>
        public string SlavePathName
        {
            get
            {
                return this._slavePathName;
            }
            set
            {
                this._slavePathName = value;
                this._slaveName = Path.GetFileName(this._slavePathName);
            }
        }

        /// <summary>
        /// Spezifische Aufrufparameter für die Checker-Dll oder null.
        /// </summary>
        public string CheckerParameters
        {
            get
            {
                if (this._checkerParameters != null)
                {
                    return this._checkerParameters.ToString();
                }
                else
                {
                    return null;
                }
            }
            set
            {
                this._checkerParameters = value;
            }
        }

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="slavePathName">Dateipfad und Name einer Dll, die INodeChecker implementiert.</param>
        public CheckerShell(string slavePathName) : this(slavePathName, null, null, null, false) { }

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="slavePathName">Dateipfad und Name einer Dll, die INodeChecker implementiert.</param>
        /// <param name="alwaysReload">Bei True wird die Dll für jeden Run neu instanziiert (für den Debug von Memory Leaks).</param>
        public CheckerShell(string slavePathName, bool alwaysReload) : this(slavePathName, null, null, null, alwaysReload) { }

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="slavePathName">Dateipfad und Name einer Dll, die INodeChecker implementiert.</param>
        /// <param name="checkerParameters">Spezifische Aufrufparameter.</param>
        public CheckerShell(string slavePathName, object checkerParameters) : this(slavePathName, checkerParameters, null, null, false) { }

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="slavePathName">Dateipfad und Name einer Dll, die INodeChecker implementiert.</param>
        /// <param name="checkerParameters">Spezifische Aufrufparameter.</param>
        /// <param name="alwaysReload">Bei True wird die Dll für jeden Run neu instanziiert (für den Debug von Memory Leaks).</param>
        public CheckerShell(string slavePathName, object checkerParameters, bool alwaysReload) : this(slavePathName, checkerParameters, null, null, alwaysReload) { }

        /// <summary>
        /// Konstruktor.
        /// </summary>
        /// <param name="slavePathName">Dateipfad und Name einer Dll, die INodeChecker implementiert.</param>
        /// <param name="checkerTrigger">Ein Trigger, der den Job wiederholt aufruft.</param>
        public CheckerShell(string slavePathName, TriggerShell checkerTrigger) : this(slavePathName, null, checkerTrigger, null, false) { }

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="slavePathName">Dateipfad und Name einer Dll, die INodeChecker implementiert.</param>
        /// <param name="checkerTrigger">Ein Trigger, der den Job wiederholt aufruft.</param>
        /// <param name="alwaysReload">Bei True wird die Dll für jeden Run neu instanziiert (für den Debug von Memory Leaks).</param>
        public CheckerShell(string slavePathName, TriggerShell checkerTrigger, bool alwaysReload) : this(slavePathName, null, checkerTrigger, null, alwaysReload) { }

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="slavePathName">Dateipfad und Name einer Dll, die INodeChecker implementiert.</param>
        /// <param name="checkerParameters">Spezifische Aufrufparameter.</param>
        /// <param name="checkerTrigger">Ein Trigger, der den Job wiederholt aufruft.</param>
        public CheckerShell(string slavePathName, object checkerParameters, TriggerShell checkerTrigger)
          : this(slavePathName, checkerParameters, checkerTrigger, null, false) { }

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="slavePathName">Dateipfad und Name einer Dll, die INodeChecker implementiert.</param>
        /// <param name="checkerParameters">Spezifische Aufrufparameter.</param>
        /// <param name="checkerTrigger">Ein Trigger, der den Job wiederholt aufruft.</param>
        /// <param name="alwaysReload">Bei True wird die Dll für jeden Run neu instanziiert (für den Debug von Memory Leaks).</param>
        public CheckerShell(string slavePathName, object checkerParameters, TriggerShell checkerTrigger, bool alwaysReload)
          : this(slavePathName, checkerParameters, checkerTrigger, null, alwaysReload) { }

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="slavePathName">Dateipfad und Name einer Dll, die INodeChecker implementiert.</param>
        /// <param name="checkerLogger">Ein Logger, der Logging-Informationen weiterverarbeitet.</param>
        public CheckerShell(string slavePathName, LoggerShell checkerLogger) : this(slavePathName, null, null, checkerLogger, false) { }

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="slavePathName">Dateipfad und Name einer Dll, die INodeChecker implementiert.</param>
        /// <param name="checkerLogger">Ein Logger, der Logging-Informationen weiterverarbeitet.</param>
        /// <param name="alwaysReload">Bei True wird die Dll für jeden Run neu instanziiert (für den Debug von Memory Leaks).</param>
        public CheckerShell(string slavePathName, LoggerShell checkerLogger, bool alwaysReload) : this(slavePathName, null, null, checkerLogger, alwaysReload) { }

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="slavePathName">Dateipfad und Name einer Dll, die INodeChecker implementiert.</param>
        /// <param name="checkerParameters">Spezifische Aufrufparameter.</param>
        /// <param name="checkerLogger">Ein Logger, der Logging-Informationen weiterverarbeitet.</param>
        public CheckerShell(string slavePathName, object checkerParameters, LoggerShell checkerLogger)
          : this(slavePathName, checkerParameters, null, checkerLogger, false) { }

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="slavePathName">Dateipfad und Name einer Dll, die INodeChecker implementiert.</param>
        /// <param name="checkerParameters">Spezifische Aufrufparameter.</param>
        /// <param name="checkerLogger">Ein Logger, der Logging-Informationen weiterverarbeitet.</param>
        /// <param name="alwaysReloadChecker">Bei True wird die Dll für jeden Run neu instanziiert (für den Debug von Memory Leaks).</param>
        public CheckerShell(string slavePathName, object checkerParameters, LoggerShell checkerLogger, bool alwaysReloadChecker)
          : this(slavePathName, checkerParameters, null, checkerLogger, alwaysReloadChecker) { }

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="slavePathName">Dateipfad und Name einer Dll, die INodeChecker implementiert.</param>
        /// <param name="checkerParameters">Spezifische Aufrufparameter.</param>
        /// <param name="checkerTrigger">Ein Trigger, der den Job wiederholt aufruft oder null.</param>
        /// <param name="checkerLogger">Ein Logger, der Logging-Informationen weiterverarbeitet.</param>
        /// <param name="alwaysReload">Bei True wird die Dll für jeden Run neu instanziiert (für den Debug von Memory Leaks).</param>
        public CheckerShell(string slavePathName, object checkerParameters, TriggerShell checkerTrigger, LoggerShell checkerLogger, bool alwaysReload)
          : base()
        {
            this.LastReturned = null;
            this.SlavePathName = slavePathName;
            this._checkerParameters = checkerParameters;
            this.CheckerTrigger = checkerTrigger;
            this.CheckerLogger = checkerLogger;
            this._lastDllWriteTime = DateTime.MinValue;
            this._runProcessLocker = new object();
            this._assemblyLoader = VishnuAssemblyLoader.GetAssemblyLoader();
            this._alwaysReload = alwaysReload;
        }

        /// <summary>
        /// Konvertiert einen Wert in ein gegebenes Format.
        /// Muss überschrieben werden, deshalb hier ohne Funktion
        /// (spiegelt den Eingabewert zurück) implementiert.
        /// </summary>
        /// <param name="toConvert">Zu konvertierender Wert</param>
        /// <returns>Unveränderter Wert.</returns>
        public override object ModifyValue(object toConvert)
        {
            return toConvert;
        }

        #endregion public members

        #region private members

        // Instanz vom INodeChecker. Macht die Prüf-Arbeit.
        private INodeChecker Slave
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

        private string _slavePathName;
        private object _checkerParameters;
        private INodeChecker _slave;
        private DateTime _lastDllWriteTime;
        private VishnuAssemblyLoader _assemblyLoader;

        private object _runProcessLocker;

        private static object _runProcessClassLocker = new object();
        private bool _alwaysReload;
        private string _slaveName;

        /// <summary>
        /// Hier wird der (normalerweise externe) Arbeitsprozess ausgeführt (oder beobachtet).
        /// </summary>
        /// <param name="checkerParameters">Spezifische Aufrufparameter oder null.</param>
        /// <param name="treeParameters">Für den gesamten Tree gültige Parameter oder null.</param>
        /// <param name="source">Auslösendes TreeEvent oder null.</param>
        /// <returns>True, False oder null</returns>
        private bool? runIt(object checkerParameters, TreeParameters treeParameters, TreeEvent source)
        {
            lock (this._runProcessLocker)
            {
                if (checkerParameters != null)
                {
                    this._checkerParameters = checkerParameters;
                }
                bool? rtn = false;
                // hot plug
                // TODO: Wenn der nachfolgende 1. IF auskommentiert wird, gibt es massive Memoryleaks, warum? Unteruchen!
                // Geklärt: passierte in AssemblyLoader.DynamicLoadAssembly beim mehrfachen Laden einer Assembly. Workaround dort:
                //          geladene Assemblies werden gecached und nach einer festlegbaren Anzahl Ladevorgängen aus dem Cache
                //          zurückgegeben (neues Flag alwaysReload).
                if (this._alwaysReload || !File.Exists(this._slavePathName) || (File.GetLastWriteTime(this._slavePathName) != this._lastDllWriteTime))
                {
                    if (this._slave != null)
                    {
                        this.Slave.NodeProgressChanged -= this.SubNodeProgressChanged;
                        this.ReturnObject = null;
                        this._slave = null;
                    }
                }
                if (this._slave == null)
                {
                    if (this._slavePathName.ToLower().EndsWith(".dll"))
                    {
                        this.Slave = this.dynamicLoadSlaveDll(this._slavePathName);
                    }
                    else
                    {
                        this.Slave = this.dynamicLoadSlaveExe(this._slavePathName);
                    }
                    if (this._slave != null)
                    {
                        this._lastDllWriteTime = File.GetLastWriteTime(this._slavePathName);
                        this.Slave.NodeProgressChanged += this.SubNodeProgressChanged;
                    }
                }
                if (this._slave != null)
                {
                    try
                    {
                        treeParameters.CheckerDllDirectory = Path.GetDirectoryName(this._slavePathName);
                        rtn = this.Slave.Run(this._checkerParameters, treeParameters, source);
                    }
                    finally
                    {
                    }
                    this.ReturnObject = this.Slave.ReturnObject;
                }
                else
                {
                    throw new ApplicationException(String.Format("Die Assembly '{0}' konnte nicht geladen werden!", this._slavePathName));
                }
                this.LastReturned = rtn;
                return rtn;
            }
        }

        // Lädt eine Plugin-Dll dynamisch. Muss keine Namespaces oder Klassennamen
        // aus der Dll kennen, Bedingung ist nur, dass eine Klasse in der Dll das
        // Interface INodeChecker implementiert. Die erste gefundene Klasse wird
        // als Instanz von INodeChecker zurückgegeben.
        private INodeChecker dynamicLoadSlaveDll(string slavePathName)
        {
            return (INodeChecker)this._assemblyLoader.DynamicLoadObjectOfTypeFromAssembly(slavePathName, typeof(INodeChecker));
        }

        // Kapselt eine Exe in eine Klasseninstanz, die InodeChecker implementiert.
        private INodeChecker dynamicLoadSlaveExe(string p)
        {
            try
            {
                return new EXEtoSlaveDllFake(this._slavePathName);
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion private members

    }

    class EXEtoSlaveDllFake : INodeChecker
    {
        #region public members

        public event NetEti.Globals.CommonProgressChangedEventHandler NodeProgressChanged;

        /// <summary>
        /// Konsolen-Ausgabe der Fremd-Exe.
        /// </summary>
        public object ReturnObject
        {
            get
            {
                return this._returnObject;
            }
            set
            {
                this._returnObject = value;
            }
        }

        internal bool IsMirror
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public EXEtoSlaveDllFake(string slaveExePath)
        {
            if (!File.Exists(slaveExePath))
            {
                throw new IOException(String.Format("Die Exe {0} wurde nicht gefunden!", slaveExePath));
            }
            this._slavePathName = slaveExePath;
        }

        public bool? Run(object checkerParameters, TreeParameters treeParameters, TreeEvent source)
        {
            this.onNodeProgressChanged(false);
            object exeReturnObject;
            bool rtn = this.exec(checkerParameters.ToString(), out exeReturnObject);
            if (rtn)
            {
                this.ReturnObject = exeReturnObject;
            }
            this.onNodeProgressChanged(true);
            return rtn;
        }

        #endregion public members

        #region private members

        private object _returnObject;
        private string _slavePathName;

        /// <summary>
        /// Hier wird der (normalerweise externe) Arbeitsprozess ausgeführt.
        /// </summary>
        /// <param name="slaveParameters">Aufrufparameter für die Exe.</param>
        /// <param name="returnObject">StdOut der Exe.</param>
        /// <returns>Exe-Returncode:0 = True, ungleich 0 = False.</returns>
        private bool exec(string slaveParameters, out object returnObject)
        {
            try
            {
                Process nPad = new Process();
                nPad.StartInfo.FileName = this._slavePathName;
                nPad.StartInfo.Arguments = slaveParameters;
                nPad.StartInfo.CreateNoWindow = true;
                nPad.StartInfo.RedirectStandardOutput = true;
                nPad.StartInfo.RedirectStandardInput = true;
                nPad.StartInfo.UseShellExecute = false;
                nPad.Start();
                nPad.WaitForExit();
                returnObject = nPad.StandardOutput.ReadToEnd();
                return nPad.ExitCode == 0 ? true : false;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void onNodeProgressChanged(bool finished)
        {
            if (this.NodeProgressChanged != null)
            {
                this.NodeProgressChanged(this, new NetEti.Globals.CommonProgressChangedEventArgs(finished ? 100 : 0, null));
            }
        }

        #endregion private members

    }
}
