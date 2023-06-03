using System;
using System.IO;
using NetEti.ApplicationControl;
using Vishnu.Interchange;

namespace LogicalTaskTree
{
    /// <summary>
    /// Filtert das ReturnObject eines INodeCheckers nach Typ und ggf. Format-String.
    /// </summary>
    /// <typeparam name="T">Konkreter Typ der Instanz (Boolean, Int16, Int32, Int64, DateTime, Object).</typeparam>
    /// <remarks>
    /// File: ValueModifier.cs
    /// Autor: Erik Nagel
    ///
    /// 27.05.2013 Erik Nagel: erstellt
    /// </remarks>
    public class ValueModifier<T> : NodeCheckerBase
    {
        #region public members

        #region INodeChecker members

        /// <summary>
        /// Das modifizierte ReturnObject des zugeordneten INodeCheckers.
        /// </summary>
        public override object? ReturnObject
        {
            get
            {
                if (this._nodeCheckerBase?.ReturnObject != null)
                {
                    return (this.ModifyValue(this._nodeCheckerBase.ReturnObject));
                }
                return this._nodeCheckerBase?.ReturnObject;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Der Pfad zum aktuell dynamisch zu ladenden UserControl.
        /// </summary>
        public override string UserControlPath
        {
            get
            {
                if (this._nodeCheckerBase != null)
                {
                    return this._nodeCheckerBase.UserControlPath;
                }
                else
                {
                    return this._userControlPath;
                }
            }
            set
            {
                this._userControlPath = value;
            }
        }

        /// <summary>
        /// Hier wird der (normalerweise externe) Arbeitsprozess ausgeführt (oder beobachtet).
        /// </summary>
        /// <param name="checkerParameters">Spezifische Aufrufparameter oder null.</param>
        /// <param name="treeParameters">Für den gesamten Tree gültige Parameter oder null.</param>
        /// <param name="source">Auslösendes TreeEvent oder null.</param>
        /// <returns>True, False oder null</returns>
        public override bool? Run(object? checkerParameters, TreeParameters treeParameters, TreeEvent source)
        {
            //Logger.Log("ValueModifier.Run: " + this._formatString + ", " + ThreadInfos.GetThreadInfos());
            if (this._ownsChecker)
            {
                return this._nodeCheckerBase?.Run(checkerParameters, treeParameters, source);
            }
            else
            {
                throw new ApplicationException("ValueConverter.Run: Versuch, fremden NodeChecker auszuführen.");
            }
        }

        #endregion INodeChecker members

        /// <summary>
        /// Pfad zum externen ValueModifier. 
        /// </summary>
        public string? SlavePathName
        {
            get
            {
                if (this._formatStringOrValueModifierPath != null &&
                    this._formatStringOrValueModifierPath.ToLower().EndsWith(".dll"))
                {
                    return this._formatStringOrValueModifierPath;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                this._formatStringOrValueModifierPath = value;
            }
        }

        /// <summary>
        /// Formatierungs-String. 
        /// </summary>
        public string? FormatString
        {
            get
            {
                if (this._formatStringOrValueModifierPath != null &&
                    !this._formatStringOrValueModifierPath.ToLower().EndsWith(".dll"))
                {
                    return this._formatStringOrValueModifierPath;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                this._formatStringOrValueModifierPath = value;
            }
        }

        /// <summary>
        /// Ausführender Checker oder ValueModifier.
        /// </summary>
        public NodeCheckerBase? NodeCheckerBase
        {
            get
            {
                return this._nodeCheckerBase;
            }
            set
            {
                this._nodeCheckerBase = value;
            }
        }

        /// <summary>
        /// Referenz auf eine ausführende NodeCheckerBase.
        /// </summary>
        public string? CheckerBaseReferenceName
        {
            get
            {
                return this._checkerBaseReferenceName;
            }
            set
            {
                this._checkerBaseReferenceName = value;
            }
        }

        /// <summary>
        /// Ein optionaler Trigger, der den Job wiederholt aufruft
        /// oder null (setzt intern BreakWithResult auf false).
        /// Wird vom IJobProvider bei der Instanziierung mitgegeben.
        /// </summary>
        public override TriggerShell? CheckerTrigger
        {
            get
            {
                return this._nodeCheckerBase?.CheckerTrigger;
            }
            set
            {
                if (this._nodeCheckerBase != null)
                {
                    this._nodeCheckerBase.CheckerTrigger = value;
                }
            }
        }

        /// <summary>
        /// Konvertiert einen Wert in das für diesen
        /// ValueModifier gültige Format.
        /// </summary>
        /// <param name="toConvert">Zu konvertierender Wert</param>
        /// <returns>Konvertierter Wert.</returns>
        public override object? ModifyValue(object? toConvert)
        {
            if (toConvert != null && !(toConvert is Exception))
            {
                if (this._formatStringOrValueModifierPath != null)
                {
                    if (this._formatStringOrValueModifierPath.ToLower().EndsWith(".dll"))
                    {
                        this._slavePathName = this._formatStringOrValueModifierPath;
                        // hot plug
                        if (!File.Exists(this._slavePathName) || (File.GetLastWriteTime(this._slavePathName) != this._lastDllWriteTime))
                        {
                            this._valueModifierSlave = null;
                        }
                        if (this._valueModifierSlave == null)
                        {
                            this._valueModifierSlave = this.dynamicLoadSlaveDll(this._slavePathName);
                        }
                        if (this._valueModifierSlave != null)
                        {
                            this._lastDllWriteTime = File.GetLastWriteTime(this._slavePathName);
                        }
                        else
                        {
                            throw new ApplicationException(String.Format("Load failure on assembly '{0}'", this._slavePathName));
                        }
                    }
                }
                try
                {
                    if (this._valueModifierSlave != null)
                    {
                        return toConvert = this._valueModifierSlave.ModifyValue(toConvert);
                    }
                    else
                    {
                        if (!String.IsNullOrEmpty(this._formatStringOrValueModifierPath))
                        {
                            toConvert = String.Format(this._formatStringOrValueModifierPath, toConvert);
                        }
                    }
                    switch (typeof(T).ToString())
                    {
                        case "System.Boolean": return (T)(Object)Convert.ToBoolean(toConvert);
                        case "System.Int16": return (T)(Object)Convert.ToInt16(toConvert);
                        case "System.Int32": return (T)(Object)Convert.ToInt32(toConvert);
                        case "System.Int64": return (T)(Object)Convert.ToInt64(toConvert);
                        case "System.DateTime": return (T)(Object)Convert.ToDateTime(toConvert);
                        default: return (T)(Object)(toConvert.ToString() ?? "");
                    }
                }
                catch
                {
#if DEBUG
                    InfoController.Say(String.Format("{0}: Konvertierungsfehler bei Wert '{1}' in DatenTyp '{2}'.",
                        toConvert?.ToString() ?? "", "?", typeof(T).FullName));
#endif
                    throw new InvalidCastException(String.Format("Konvertierungsfehler bei Wert '{0}' in DatenTyp '{1}'.",
                        toConvert?.ToString() ?? "", typeof(T).FullName));
                }
            }
            else
            {
                return toConvert;
            }
        }

        /// <summary>
        /// Konstruktor - übernimmt die zugeordnete NodeCheckerBase.
        /// </summary>
        /// <param name="nodeChecker">NodeCheckerBase, deren ReturnObject gefiltert oder verändert werden soll.</param>
        public ValueModifier(NodeCheckerBase nodeChecker) : this(null, nodeChecker) { }

        /// <summary>
        /// Konstruktor - übernimmt den Namen einer bereits definierten NodeCheckerBase.
        /// </summary>
        /// <param name="checkerReference">Name der NodeCheckerBase, dessen ReturnObject gefiltert oder verändert werden soll.</param>
        public ValueModifier(string checkerReference) : this(null, checkerReference) { }

        /// <summary>
        /// Konstruktor - übernimmt einen Format-String oder den Pfad einer externen
        /// IValueModifier-Dll und die zugeordnete NodeCheckerBase.
        /// </summary>
        /// <param name="formatStringOrValueModifierPath">Zusätzlicher Format-String (analog String.Format) oder Pfad zu einer IValueModifier-Dll</param>
        /// <param name="nodeCheckerBase">NodeCheckerBase, deren ReturnObject gefiltert oder verändert werden soll.</param>
        public ValueModifier(string? formatStringOrValueModifierPath, NodeCheckerBase nodeCheckerBase)
          : this()
        {
            this._formatStringOrValueModifierPath = (formatStringOrValueModifierPath ?? "").Trim();
            this.NodeCheckerBase = nodeCheckerBase;
            this._ownsChecker = true;
            if (this._nodeCheckerBase != null)
            {
                this._nodeCheckerBase.NodeProgressChanged -= this.SubNodeProgressChanged;
                this._nodeCheckerBase.NodeProgressChanged += this.SubNodeProgressChanged;
            }
        }

        /// <summary>
        /// Konstruktor - übernimmt einen Format-String oder den Pfad einer externen
        /// IValueModifier-Dll und den Namen einer bereits definierten NodeCheckerBase.
        /// </summary>
        /// <param name="formatStringOrValueModifierPath">Zusätzlicher Format-String (analog String.Format) oder Pfad zu einer IValueModifier-Dll</param>
        /// <param name="checkerReference">Name der NodeCheckerBase, deren ReturnObject gefiltert oder verändert werden soll.</param>
        public ValueModifier(string? formatStringOrValueModifierPath, string checkerReference)
          : this()
        {
            this._formatStringOrValueModifierPath = (formatStringOrValueModifierPath ?? "").Trim();
            this._checkerBaseReferenceName = checkerReference;
        }

        /// <summary>
        /// Liefert den Namen des Checkers, der diesem ValueConverter zugeordnet werden soll.
        /// </summary>
        /// <returns>Namen des Checkers, der diesem ValueConverter zugeordnet werden soll oder null.</returns>
        public override string? GetCheckerReference()
        {
            return this._checkerBaseReferenceName;
        }

        /*
        /// <summary>
        /// Übernimmt den Checker für diesen ValueConverter.
        /// </summary>
        /// <param>Instanz des Checkers, der diesem ValueConverter zugeordnet werden soll.</param>
        public override void SetChecker(NodeCheckerBase checker)
        {
          this._nodeCheckerBase = checker;
          this._nodeCheckerBase.NodeProgressChanged -= this.SubNodeProgressChanged;
          this._nodeCheckerBase.NodeProgressChanged += this.SubNodeProgressChanged;
        }
        */

        #endregion public members

        #region private members

        private string? _checkerBaseReferenceName;
        private NodeCheckerBase? _nodeCheckerBase;
        private string? _formatStringOrValueModifierPath;
        private bool _ownsChecker;
        private string _slavePathName;
        private IValueModifier? _valueModifierSlave;
        private DateTime _lastDllWriteTime;

        private VishnuAssemblyLoader _assemblyLoader;
        private string _userControlPath;

        private ValueModifier()
        {
            this._formatStringOrValueModifierPath = null;
            this.NodeCheckerBase = null;
            this._valueModifierSlave = null;
            this.CheckerBaseReferenceName = null;
            this._ownsChecker = false;
            this._assemblyLoader = VishnuAssemblyLoader.GetAssemblyLoader();
            this._lastDllWriteTime = DateTime.MinValue;
            this._slavePathName = String.Empty;
            this._userControlPath = String.Empty;
        }

        // Lädt eine Plugin-Dll dynamisch. Muss keine Namespaces oder Klassennamen
        // aus der Dll kennen, Bedingung ist nur, dass eine Klasse in der Dll das
        // Interface IValueModifier implementiert. Die erste gefundene Klasse wird
        // als Instanz von IValueModifier zurückgegeben.
        private IValueModifier? dynamicLoadSlaveDll(string slavePathName)
        {
            return (IValueModifier?)this._assemblyLoader.DynamicLoadObjectOfTypeFromAssembly(slavePathName, typeof(IValueModifier));
        }

        #endregion private members

    }
}
