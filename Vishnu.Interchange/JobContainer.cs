using NetEti.ObjectSerializer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.Serialization;

namespace Vishnu.Interchange
{
    /// <summary>
    /// Enum für die verschiedenen Formate, in denen Jobs gespeichert werden können.
    /// </summary>
    public enum JobDescriptionFormat
    {
        /// <summary>
        /// XML-Format.
        /// </summary>
        Xml,
        /// <summary>
        /// JSON-Format.
        /// </summary>
        Json
    }

    #region SnapshotJobContainer

    /// <summary>
    /// Klasse zum Laden und Speichern von Jobs in XML- und JSON-Format.
    /// </summary>
    [XmlRoot("Snapshot")]
    public class SnapshotJobContainer
    {
        /// <summary>
        /// Der Zeitpunkt, zu dem der Snapshot gespeichert wurde.
        /// </summary>
        [JsonPropertyName("TimeStamp")]
        public DateTime? TimeStamp { get; set; }

        /// <summary>
        /// Der Dateipfad zum gespeicherten Snapshot.
        /// </summary>
        [JsonPropertyName("Path")]
        public string? Path { get; set; }

        /// <summary>
        /// Der enthaltene Job.
        /// </summary>
        [JsonPropertyName("JobDescription")]
        public JobContainer? JobDescription { get; set; }
    }

    #endregion SnapshotJobContainer

    #region JobContainer

    /// <summary>
    /// Klasse zum Laden und Speichern von Jobs in XML- und JSON-Format.
    /// </summary>
    [XmlRoot("JobDescription")]
    public class JobContainer
    {
        /// <summary>
        /// Das Format, in dem dieser Job gespeichert wurde (Json oder Xml).
        /// </summary>
        [XmlIgnore]
        public JobDescriptionFormat Format { get; set; }

        /// <summary>
        /// Der Zeitpunkt, zu dem der Snapshot gespeichert wurde.
        /// </summary>
        [JsonPropertyName("TimeStamp")]
        public DateTime? TimeStamp { get; set; }

        /// <summary>
        /// Der Dateipfad zum gespeicherten Snapshot.
        /// </summary>
        [JsonPropertyName("Path")]
        public string? Path { get; set; }

        /// <summary>
        /// Der logische Name des Jobs.
        /// </summary>
        [JsonPropertyName("LogicalName")]
        public string? LogicalName { get; set; }

        /// <summary>
        /// Der logische Ausdruck, der durch eine JobList im LogicalTaskTree
        /// abgearbeitet wird. 
        /// </summary>
        [JsonPropertyName("LogicalExpression")]
        public string? LogicalExpression { get; set; }

        /// <summary>
        /// Der Dateipfad zum gespeicherten Job.
        /// </summary>
        [JsonPropertyName("PhysicalPath")]
        public string? PhysicalPath { get; set; }

        /// <summary>
        /// String-Property zur Vermeidung von Serialisierungsfehlern wegen Groß- Kleinschreibung
        /// </summary>
        [XmlElement("StartCollapsed")]
        public string StartCollapsedString
        {
            get => StartCollapsed.ToString().ToLowerInvariant();
            set => StartCollapsed = value.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Bei True wird der Job beim Start zusammengeklappt angezeigt, wenn die UI dies unterstützt.
        /// Default: False
        /// </summary>
        [XmlIgnore]
        [JsonPropertyName("StartCollapsed")]
        public bool StartCollapsed { get; set; }

        /// <summary>
        /// String-Property zur Vermeidung von Serialisierungsfehlern wegen Groß- Kleinschreibung
        /// </summary>
        [XmlElement("InitNodes")]
        public string InitNodesString
        {
            get => InitNodes.ToString().ToLowerInvariant();
            set => InitNodes = value.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Bei True werden alle Knoten im Tree resettet, wenn dieser Knoten gestartet wird.
        /// Kann für Loops in Controlled-Jobs verwendet werden.
        /// Default: false.
        /// </summary>
        [XmlIgnore]
        [JsonPropertyName("InitNodes")]
        public bool InitNodes { get; set; }


        /// <summary>
        /// String-Property zur Vermeidung von Serialisierungsfehlern wegen Int32 und String.
        /// </summary>
        [XmlElement("TriggeredRunDelay")]
        public string TriggeredRunDelayString
        {
            get => TriggeredRunDelay.ToString().ToLowerInvariant();
            set
            {
                if (int.TryParse(value, out int result))
                {
                    TriggeredRunDelay = result;
                }
                else
                {
                    TriggeredRunDelay = 0;
                }
            }
        }

        /// <summary>
        /// Verzögert den Start eines Knotens (und InitNodes).
        /// Kann für Loops in Controlled-Jobs verwendet werden.
        /// Default: 0 (Millisekunden).
        /// </summary>
        [XmlIgnore]
        [JsonPropertyName("TriggeredRunDelay")]
        public int TriggeredRunDelay { get; set; }

        /// <summary>
        /// String-Property zur Vermeidung von Serialisierungsfehlern wegen Groß- Kleinschreibung
        /// </summary>
        [XmlElement("IsVolatile")]
        public string IsVolatileString
        {
            get => IsVolatile.ToString().ToLowerInvariant();
            set => IsVolatile = value.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Bei True wird zur Ergebnisermittlung im Tree Logical benutzt,
        /// bei False LastNotNullLogical;
        /// Default: false.
        /// </summary>
        [XmlIgnore]
        [JsonPropertyName("IsVolatile")]
        public bool IsVolatile { get; set; }

        /// <summary>
        /// String-Property zur Vermeidung von Serialisierungsfehlern wegen Groß- Kleinschreibung
        /// </summary>
        [XmlElement("BreakWithResult")]
        public string BreakWithResultString
        {
            get => BreakWithResult.ToString().ToLowerInvariant();
            set => BreakWithResult = value.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Ein Job kann u.U. schon ein eindeutiges logisches Ergebnis haben,
        /// bevor alle Kinder ihre Verarbeitung beendet haben.
        /// Bei BreakWithResult=True werden diese dann abgebochen.
        /// Greift nicht, wenn ein JobTrigger gesetzt wurde.
        /// </summary>
        [XmlIgnore]
        [JsonPropertyName("BreakWithResult")]
        public bool BreakWithResult { get; set; }

        #region ThreadLocked

        /// <summary>
        /// Enthält eine Property "ThreadLocked" und einen optionalen "LockName"
        /// zur Differenzierung verschiedener Locking-Gruppen.
        /// Bei "ThreadLocked": True wird jeder Thread über die Klasse gesperrt,
        /// so dass nicht Thread-sichere Checker serialisiert werden;
        /// Ist darüber hinaus "LockName" gesetzt, wird nur gegen Checker mit dem
        /// gleichen LockName gesperrt.
        /// Default: False;
        /// </summary>
        public JobContainerThreadLock? StructuredThreadLock { get; set; }

        /// <summary>
        /// Wenn gesetzt, wird der Checker nicht gleichzeitig mit anderen Checkern
        /// ausgeführt, die auch ThreadLocked gesetzt haben.
        /// Wenn LockName gesetzt ist, wird nur gegen Checker mit dem gleichen
        /// LockName gesperrt.
        /// Default: False;
        /// </summary>
        [XmlIgnore]
        [JsonPropertyName("ThreadLocked")]
        [JsonConverter(typeof(CustomJsonConverter))]
        public string? ThreadLockedString {
            get
            {
                return this._threadLockedString;
            }
            set
            {
                if (value != null && value != this._threadLockedString)
                {
                    this._threadLockedString = value.Replace("true", "true", true, System.Globalization.CultureInfo.InvariantCulture);
                    this._threadLockedString = value.Replace("false", "false", true, System.Globalization.CultureInfo.InvariantCulture);
                    if (!string.IsNullOrEmpty(this._threadLockedString))
                    {
                        this.StructuredThreadLock = SerializationUtility.
                            DeserializeFromJsonOrXml<JobContainerThreadLock>(this._threadLockedString, noException: true);
                    }
                }
            }
        }
        private string? _threadLockedString;

        /// <summary>
        /// Ist für die XML-Serialisierung notwendig.
        /// </summary>
        [XmlAnyElement("ThreadLocked")]
        public XmlElement?[]? ThreadLockedXml
        {
            get
            {
                if (string.IsNullOrEmpty(ThreadLockedString))
                    return null;

                var doc = new XmlDocument();
                doc.LoadXml(ThreadLockedString);
                return new[] { doc.DocumentElement };
            }
            set
            {
                if (value != null && value.Length > 0)
                {
                    ThreadLockedString = value[0]?.OuterXml;
                    StructuredThreadLock =
                        SerializationUtility.DeserializeFromJsonOrXml<JobContainerThreadLock>(
                            ThreadLockedString ?? "", noException: true);
                    // Debug-Tipp von ChatGPT:
                    //   Wenn du sehen willst, was der Serializer eigentlich erwartet,
                    //   kannst du den erwarteten XML-Code mit XmlSerializer auch erzeugen:
                    //   <?xml version="1.0" encoding="utf-16"?>
                    //   <ThreadLocked xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
                    // 	   xmlns:xsd="http://www.w3.org/2001/XMLSchema"
                    // 	   LockName="ConcurrentTest">true</ThreadLocked>
                    //   var obj = new JobContainerThreadLock { LockName = "ConcurrentTest", ThreadLocked = true };
                    //   var xmlSerializer = new XmlSerializer(typeof(JobContainerThreadLock));
                    //   using TextWriter textWriter = new StringWriter();
                    //   xmlSerializer.Serialize(textWriter, obj);
                }
            }
        }

        #endregion ThreadLocked

        /// <summary>
        /// Ein optionaler Trigger, der den Job wiederholt aufruft
        /// oder null (setzt intern BreakWithResult auf false).
        /// </summary>
        [XmlElement("JobTrigger")]
        [JsonPropertyName("JobTrigger")]
        public JobContainerTrigger? JobTrigger { get; set; }

        /// <summary>
        /// Ein optionaler Trigger, der den Job wiederholt aufruft
        /// oder null (setzt intern BreakWithResult auf false).
        /// Wenn JobTrigger nicht gesetzt ist, wird dieser verwendet.
        /// Bei Snapshots enthält dieser Trigger den Snapshot-Trigger.
        /// </summary>
        [XmlElement("Trigger")]
        [JsonPropertyName("Trigger")]
        public JobContainerTrigger? Trigger { get; set; }

        /// <summary>
        /// Ein optionaler Trigger, der steuert, wann ein Snapshot des Jobs erstellt
        /// werden soll oder null.
        /// </summary>
        [XmlElement("JobSnapshotTrigger")]
        [JsonPropertyName("JobSnapshotTrigger")]
        public JobContainerTrigger? JobSnapshotTrigger { get; set; }

        /// <summary>
        /// Ein optionaler Logger, der dem Job zugeordnet
        /// ist oder null.
        /// </summary>
        [XmlElement("JobLogger")]
        [JsonPropertyName("JobLogger")]
        public JobContainerLogger? JobLogger { get; set; }

        /// <summary>
        /// Pfad zum dynamisch zu ladenden UserControl für eine JobList.
        /// </summary>
        [JsonPropertyName("UserControlPath")]
        public string? UserControlPath { get; set; }

        /// <summary>
        /// Pfad zum dynamisch zu ladenden UserControl für eine JobList.
        /// </summary>
        [JsonPropertyName("JobListUserControlPath")]
        public string? JobListUserControlPath { get; set; }

        /// <summary>
        /// Pfad zum dynamisch zu ladenden UserControl für einen Snapshot.
        /// </summary>
        [JsonPropertyName("SnapshotUserControlPath")]
        public string? SnapshotUserControlPath { get; set; }

        /// <summary>
        /// Pfad zum dynamisch zu ladenden UserControl für eine NodeList.
        /// </summary>
        [JsonPropertyName("NodeListUserControlPath")]
        public string? NodeListUserControlPath { get; set; }

        /// <summary>
        /// Pfad zum dynamisch zu ladenden UserControl für einen JobConnector.
        /// </summary>
        [JsonPropertyName("JobConnectorUserControlPath")]
        public string? JobConnectorUserControlPath { get; set; }

        /// <summary>
        /// Pfad zum dynamisch zu ladenden UserControl für eine SingleNode.
        /// </summary>
        [JsonPropertyName("SingleNodeUserControlPath")]
        public string? SingleNodeUserControlPath { get; set; }

        /// <summary>
        /// Pfad zum dynamisch zu ladenden UserControl für eine Constant-SingleNode.
        /// </summary>
        [JsonPropertyName("ConstantNodeUserControlPath")]
        public string? ConstantNodeUserControlPath { get; set; }

        /// <summary>
        /// Liste von externen Prüfroutinen für einen Job.
        /// </summary>
        public CheckersContainer Checkers { get; set; }

        /// <summary>
        /// Enthält eine Liste von Wertmodifikatoren für Checker.
        /// </summary>
        public ValueModifiersContainer ValueModifiers { get; set; }

        /// <summary>
        /// Enthält eine Liste von externen Triggern für einen Job.
        /// </summary>
        public TriggersContainer Triggers { get; set; }

        /// <summary>
        /// Enthält eine Liste von externen Loggern für einen Job.
        /// </summary>
        public LoggersContainer Loggers { get; set; }

        /// <summary>
        /// Enthält eine Liste von externen Workern für Checker eines Jobs.
        /// </summary>
        public WorkersContainer Workers { get; set; }

        /// <summary>
        /// Enthält eine Liste von SubJobs des aktuellen Jobs.
        /// </summary>
        public SubJobsContainer SubJobs { get; set; }

        /// <summary>
        /// Enthält eine Liste von Snapshots des aktuellen Jobs.
        /// </summary>
        public SnapshotsContainer Snapshots { get; set; }

        /// <summary>
        /// Standard Konstruktor.
        /// </summary>
        public JobContainer()
        {
            Checkers = new CheckersContainer();
            ValueModifiers = new ValueModifiersContainer();
            Triggers = new TriggersContainer();
            Loggers = new LoggersContainer();
            Workers = new WorkersContainer();
            SubJobs = new SubJobsContainer();
            Snapshots = new SnapshotsContainer();
        }
    }

    #endregion JobContainer

    #region ThreadLocked

    /// <summary>
    /// Enthält eine Property "ThreadLocked" und einen optionalen "LockName"
    /// zur Differenzierung verschiedener Locking-Gruppen.
    /// Bei "ThreadLocked": True wird jeder Thread über die Klasse gesperrt,
    /// so dass nicht Thread-sichere Checker serialisiert werden;
    /// Ist darüber hinaus "LockName" gesetzt, wird nur gegen Checker mit dem
    /// gleichen LockName gesperrt.
    /// Default: False;
    /// </summary>
    [XmlRoot("ThreadLocked", Namespace = "")]
    public class JobContainerThreadLock
    {
        /// <summary>
        /// Ein optionaler Key zur Differenzierung verschiedener Locking-Gruppen.
        /// </summary>
        [XmlAttribute("LockName")]
        [JsonPropertyName("@LockName")]
        public string? LockName { get; set; }

        /// <summary>
        /// Bei True wird jeder Thread über die Klasse gesperrt, so dass
        /// nicht Thread-sichere Checker serialisiert werden;
        /// Default: False;
        /// </summary>
        [XmlText]
        [JsonPropertyName("#Text")]
        public bool ThreadLocked { get; set; }

    }

    #endregion ThreadLocked

    #region JobContainerTrigger

    /// <summary>
    /// Kapselt einen Trigger für einen Job.
    /// </summary>
    public class JobContainerTrigger
    {
        /// <summary>
        /// Der logische Name des Triggers.
        /// </summary>
        [JsonPropertyName("LogicalName")]
        public string? LogicalName { get; set; }

        /// <summary>
        /// Name eines referenzierten Triggers oder null.
        /// </summary>
        [JsonPropertyName("Reference")]
        public string? Reference { get; set; }

        /// <summary>
        /// Der physiche Path zur Trigger-Dll.
        /// </summary>
        [JsonPropertyName("PhysicalPath")]
        public string? PhysicalPath { get; set; }

        /// <summary>
        /// Optionale Trigger-Parameter.
        /// </summary>
        [JsonPropertyName("Parameters")]
        public string? Parameters { get; set; }
    }

    #endregion JobContainerTrigger

    #region JobContainerLogger

    /// <summary>
    /// Kapselt einen Logger für einen Job.
    /// </summary>
    public class JobContainerLogger
    {
        /// <summary>
        /// Der logische Name des Loggers.
        /// </summary>
        [JsonPropertyName("LogicalName")]
        public string? LogicalName { get; set; }

        /// <summary>
        /// Der physiche Path zur Logger-Dll.
        /// </summary>
        [JsonPropertyName("PhysicalPath")]
        public string? PhysicalPath { get; set; }

        /// <summary>
        /// Logger Parameter - enthalten auslösende Bedingungen.
        /// </summary>
        [JsonPropertyName("Parameters")]
        public string? Parameters { get; set; }

        /// <summary>
        /// Name eines referenzierten Loggers oder null.
        /// </summary>
        [JsonPropertyName("Reference")]
        public string? Reference { get; set; }
    }

    #endregion

    #region JobContainerChecker

    /// <summary>
    /// Kapselt den Aufruf einer externen Arbeitsroutine,
    /// die dynamisch als Dll-Plugin geladen wird.
    /// </summary>
    public class JobContainerChecker
    {
        /// <summary>
        /// Der logische Name des Checkers.
        /// </summary>
        [JsonPropertyName("LogicalName")]
        public string? LogicalName { get; set; }
        
        /// <summary>
        /// Der physiche Path zur Checker-Dll.
        /// </summary>
        [JsonPropertyName("PhysicalPath")]
        public string? PhysicalPath { get; set; }
        
        /// <summary>
        /// Optionale Checker-Parameter.
        /// </summary>
        [JsonPropertyName("Parameters")]
        public string? Parameters { get; set; }

        /// <summary>
        /// String-Property zur Vermeidung von Serialisierungsfehlern wegen Groß- Kleinschreibung
        /// </summary>
        [XmlElement("IsMirror")]
        public string IsMirrorString
        {
            get => IsMirror.ToString().ToLowerInvariant();
            set => IsMirror = value.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// True, wenn der Checker einen anderen Checker widerspiegelt.
        /// </summary>
        [XmlIgnore]
        [JsonPropertyName("IsMirror")]
        public bool IsMirror { get; set; }

        /// <summary>
        /// Ein optionaler Trigger, der den Checker wiederholt aufruft
        /// oder null.
        /// </summary>
        [XmlElement("Trigger")]
        [JsonPropertyName("Trigger")]
        public JobContainerTrigger? Trigger { get; set; }

        /// <summary>
        /// Ein optionaler Logger, der dem Checker zugeordnet ist.
        /// oder null.
        /// </summary>
        [XmlElement("Logger")]
        [JsonPropertyName("Logger")]
        public JobContainerLogger? Logger { get; set; }

        /// <summary>
        /// Pfad zu einem otionalen, dynamisch zu ladenden UserControl.
        /// </summary>
        [JsonPropertyName("UserControlPath")]
        public string? UserControlPath { get; set; }

        /// <summary>
        /// Pfad zu einem otionalen, dynamisch zu ladenden UserControl.
        /// </summary>
        [JsonPropertyName("SingleNodeUserControlPath")]
        public string? SingleNodeUserControlPath { get; set; }

        #region ThreadLocked

        /// <summary>
        /// Enthält eine Property "ThreadLocked" und einen optionalen "LockName"
        /// zur Differenzierung verschiedener Locking-Gruppen.
        /// Bei "ThreadLocked": True wird jeder Thread über die Klasse gesperrt,
        /// so dass nicht Thread-sichere Checker serialisiert werden;
        /// Ist darüber hinaus "LockName" gesetzt, wird nur gegen Checker mit dem
        /// gleichen LockName gesperrt.
        /// Default: False;
        /// </summary>
        public JobContainerThreadLock? StructuredThreadLock { get; set; }

        /// <summary>
        /// Wenn gesetzt, wird der Checker nicht gleichzeitig mit anderen Checkern
        /// ausgeführt, die auch ThreadLocked gesetzt haben.
        /// Wenn LockName gesetzt ist, wird nur gegen Checker mit dem gleichen
        /// LockName gesperrt.
        /// Default: False;
        /// </summary>
        [XmlIgnore]
        [JsonPropertyName("ThreadLocked")]
        [JsonConverter(typeof(CustomJsonConverter))]
        public string? ThreadLockedString
        {
            get
            {
                return this._threadLockedString;
            }
            set
            {
                if (value != this._threadLockedString)
                {
                    this._threadLockedString = value;
                    if (!string.IsNullOrEmpty(value))
                    {
                        this.StructuredThreadLock = SerializationUtility.
                            DeserializeFromJsonOrXml<JobContainerThreadLock>(value, noException: true);
                    }
                }
            }
        }
        private string? _threadLockedString;

        /// <summary>
        /// Ist für die XML-Serialisierung notwendig.
        /// </summary>
        [XmlAnyElement("ThreadLocked")]
        public XmlElement?[]? ThreadLockedXml
        {
            get
            {
                if (string.IsNullOrEmpty(ThreadLockedString))
                    return null;

                var doc = new XmlDocument();
                doc.LoadXml(ThreadLockedString);
                return new[] { doc.DocumentElement };
            }
            set
            {
                if (value != null && value.Length > 0)
                {
                    ThreadLockedString = value[0]?.OuterXml;
                    StructuredThreadLock =
                        SerializationUtility.DeserializeFromJsonOrXml<JobContainerThreadLock>(
                            ThreadLockedString ?? "", noException: true);
                    // Debug-Tipp von ChatGPT:
                    //   Wenn du sehen willst, was der Serializer eigentlich erwartet,
                    //   kannst du den erwarteten XML-Code mit XmlSerializer auch erzeugen:
                    //   <?xml version="1.0" encoding="utf-16"?>
                    //   <ThreadLocked xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
                    // 	   xmlns:xsd="http://www.w3.org/2001/XMLSchema"
                    // 	   LockName="ConcurrentTest">true</ThreadLocked>
                    //   var obj = new JobContainerThreadLock { LockName = "ConcurrentTest", ThreadLocked = true };
                    //   var xmlSerializer = new XmlSerializer(typeof(JobContainerThreadLock));
                    //   using TextWriter textWriter = new StringWriter();
                    //   xmlSerializer.Serialize(textWriter, obj);
                }
            }
        }

        #endregion ThreadLocked

        /// <summary>
        /// String-Property zur Vermeidung von Serialisierungsfehlern wegen Groß- Kleinschreibung
        /// </summary>
        [XmlElement("InitNodes")]
        public string InitNodesString
        {
            get => InitNodes.ToString().ToLowerInvariant();
            set => InitNodes = value.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Bei True werden alle Knoten im Tree resettet, wenn dieser Knoten gestartet wird.
        /// Kann für Loops in Controlled-Jobs verwendet werden.
        /// Default: false.
        /// </summary>
        [XmlIgnore]
        [JsonPropertyName("InitNodes")]
        public bool InitNodes { get; set; }

        /// <summary>
        /// String-Property zur Vermeidung von Serialisierungsfehlern wegen Groß- Kleinschreibung
        /// </summary>
        [XmlElement("IsGlobal")]
        public string IsGlobalString
        {
            get => IsGlobal.ToString().ToLowerInvariant();
            set => IsGlobal = value.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Bei true wird dieser Knoten als Referenzknoten angelegt, wenn irgendwo im Tree
        /// (nicht nur im aktuellen Job) der Name des Knotens schon gefunden wurde.
        /// Bei false wird nur im aktuellen Job nach gleichnamigen Knoten gesucht.
        /// </summary>
        [XmlIgnore]
        [JsonPropertyName("IsGlobal")]
        public bool IsGlobal { get; set; }

        /// <summary>
        /// Verzögert den Start eines Knotens (und InitNodes).
        /// Kann für Loops in Controlled-Jobs verwendet werden.
        /// Default: 0 (Millisekunden).
        /// </summary>
        [JsonPropertyName("TriggeredRunDelay")]
        public int TriggeredRunDelay { get; set; }

        /// <summary>
        /// Pfad zu einer optionalen Dll, die eine ICanRun-Instanz zur Verfügung stellt.
        /// Wenn vorhanden, wird vor jedem Run des zugehörigen Checkers oder Workers
        /// CanRun aufgerufen. Liefert CanRun false zurück, wird der Start abgebrochen.
        /// Zusätzlich können in CanRun die übergebenen Parameter noch modifiziert werden.
        /// </summary>
        [JsonPropertyName("CanRunDllPath")]
        public string? CanRunDllPath { get; set; }
    }

    #endregion JobContainerChecker

    #region JobContainerValueModifier

    /// <summary>
    /// Kapselt einen Wertmodifikator für einen Checker.
    /// </summary>
    public class JobContainerValueModifier
    {
        /// <summary>
        /// Der logische Name des ValueModifiers.
        /// </summary>
        [JsonPropertyName("LogicalName")]
        public string? LogicalName { get; set; }
        
        /// <summary>
        /// Name eines referenzierten ValueModifiers oder null.
        /// </summary>
        [JsonPropertyName("Reference")]
        public string? Reference { get; set; }
        
        /// <summary>
        /// Der physiche Path zur ValueModifier-Dll.
        /// </summary>
        [JsonPropertyName("PhysicalPath")]
        public string? PhysicalPath { get; set; }
        
        /// <summary>
        /// Formatangabe für den ValueModifier.
        /// </summary>
        [JsonPropertyName("Format")]
        public string? Format { get; set; }
        
        /// <summary>
        /// Typ des ValueModifiers.
        /// </summary>
        [JsonPropertyName("Type")]
        public string? Type { get; set; }

        /// <summary>
        /// String-Property zur Vermeidung von Serialisierungsfehlern wegen Groß- Kleinschreibung
        /// </summary>
        [XmlElement("IsGlobal")]
        public string IsGlobalString
        {
            get => IsGlobal.ToString().ToLowerInvariant();
            set => IsGlobal = value.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Bei true wird dieser ValueModifier global für alle Jobs verwendet.
        /// Bei false nur im aktuellen Job.
        /// </summary>
        [XmlIgnore]
        [JsonPropertyName("IsGlobal")]
        public bool IsGlobal { get; set; }
        
        /// <summary>
        /// Pfad zu einem otionalen, dynamisch zu ladenden UserControl.
        /// </summary>
        [JsonPropertyName("UserControlPath")]
        public string? UserControlPath { get; set; }
        
        /// <summary>
        /// Pfad zu einem otionalen, dynamisch zu ladenden UserControl für eine SingleNode.
        /// </summary>
        [JsonPropertyName("SingleNodeUserControlPath")]
        public string? SingleNodeUserControlPath { get; set; }
    }

    #endregion JobContainerValueModifier

    #region JobContainerWorker

    /// <summary>
    /// Kapselt einen Worker, der externe Arbeitsroutinen ausführt.
    /// </summary>
    public class JobContainerWorker
    {
        /// <summary>
        /// Der logische Ausdruck, der durch einen Worker abgearbeitet wird.
        /// </summary>
        [JsonPropertyName("LogicalExpression")]
        public string? LogicalExpression { get; set; }

        /// <summary>
        /// Enthält eine Liste von SubWorkern des aktuellen Workers.
        /// </summary>
        public SubWorkersContainer SubWorkers { get; set; }

        /// <summary>
        /// Standard Konstruktor.
        /// </summary>
        public JobContainerWorker()
        {
            SubWorkers = new SubWorkersContainer();
        }
    }

    #endregion JobContainerWorker

    #region JobContainerSubWorker

    /// <summary>
    /// Kapselt einen SubWorker für einen Worker.
    /// </summary>
    public class JobContainerSubWorker
    {
        /// <summary>
        /// Der physiche Path zur SubWorker-Dll.
        /// </summary>
        [JsonPropertyName("PhysicalPath")]
        public string? PhysicalPath { get; set; }

        /// <summary>
        /// Optionale SubWorker-Parameter.
        /// </summary>
        [XmlIgnore]
        [JsonPropertyName("Parameters")]
        [JsonConverter(typeof(CustomJsonConverter))]
        public string? Parameters { get; set; }

        /// <summary>
        /// Ist für die XML-Serialisierung notwendig.
        /// </summary>
        [XmlAnyElement("Parameters")]
        public XmlElement?[]? ParametersXml
        {
            get
            {
                if (string.IsNullOrEmpty(Parameters))
                    return null;

                var doc = new XmlDocument();
                doc.LoadXml(Parameters);
                return new[] { doc.DocumentElement };
            }
            set
            {
                if (value != null && value.Length > 0)
                {
                    Parameters = value[0]?.OuterXml;
                }
            }
        }

        /// <summary>
        /// Optionale SubWorker-Parameter.
        /// </summary>
        [JsonPropertyName("StructuredParameters")]
        public StructuredParameters? StructuredParameters { get; set; }

        /// <summary>
        /// Ein optionaler Trigger, der den SubWorker wiederholt aufruft
        /// oder null.
        /// </summary>
        [XmlElement("Trigger")]
        [JsonPropertyName("Trigger")]
        public JobContainerTrigger? Trigger { get; set; }

    }

    #endregion JobContainerSubWorker

    #region JobContainerListContainers

    /// <summary>
    /// Enthält eine Liste von SubJobs des aktuellen Jobs.
    /// </summary>
    public class SubJobsContainer
    {
        // Im JSON ist das Array in einer Eigenschaft "SubJob" eingebettet.
        /// <summary>
        /// Die eigentliche Liste.
        /// </summary>
        [JsonPropertyName("SubJob")]
        [XmlElement("SubJob")]
        public System.Collections.Generic.List<JobContainer> SubJobsList { get; set; }

        /// <summary>
        /// Konstruktor - initialisiert die Liste.
        /// </summary>
        public SubJobsContainer()
        {
            this.SubJobsList = new List<JobContainer>();
        }
    }

    /// <summary>
    /// Enthält eine Liste von Snapshots des aktuellen Jobs.
    /// </summary>
    public class SnapshotsContainer
    {
        // Im JSON ist das Array in einer Eigenschaft "Snapshot" eingebettet.
        /// <summary>
        /// Die eigentliche Liste.
        /// </summary>
        [JsonPropertyName("Snapshot")]
        [XmlElement("Snapshot")]
        public System.Collections.Generic.List<JobContainer> SnapshotsList { get; set; }

        /// <summary>
        /// Konstruktor - initialisiert die Liste.
        /// </summary>
        public SnapshotsContainer()
        {
            this.SnapshotsList = new List<JobContainer>();
        }
    }

    /// <summary>
    /// Enthält eine Liste von Workern des aktuellen Jobs.
    /// </summary>
    public class WorkersContainer
    {
        // Im JSON ist das Array in einer Eigenschaft "Worker" eingebettet.
        /// <summary>
        /// Die eigentliche Liste.
        /// </summary>
        [JsonPropertyName("Worker")]
        [XmlElement("Worker")]
        public System.Collections.Generic.List<JobContainerWorker> WorkersList { get; set; }

        /// <summary>
        /// Konstruktor - initialisiert die Liste.
        /// </summary>
        public WorkersContainer()
        {
            this.WorkersList = new List<JobContainerWorker>();
        }
    }

    /// <summary>
    /// Enthält eine Liste von SubWorkern des aktuellen Workers.
    /// </summary>
    public class SubWorkersContainer
    {
        // Im JSON ist das Array in einer Eigenschaft "SubWorker" eingebettet.
        /// <summary>
        /// Die eigentliche Liste.
        /// </summary>
        [JsonPropertyName("SubWorker")]
        [XmlElement("SubWorker")]
        public System.Collections.Generic.List<JobContainerSubWorker> SubWorkersList { get; set; }

        /// <summary>
        /// Konstruktor - initialisiert die Liste.
        /// </summary>
        public SubWorkersContainer()
        {
            this.SubWorkersList = new List<JobContainerSubWorker>();
        }
    }

    /// <summary>
    /// Enthält eine Liste von Loggern des aktuellen Jobs.
    /// </summary>
    public class LoggersContainer
    {
        // Im JSON ist das Array in einer Eigenschaft "Logger" eingebettet.
        /// <summary>
        /// Die eigentliche Liste.
        /// </summary>
        [JsonPropertyName("Logger")]
        [XmlElement("Logger")]
        public System.Collections.Generic.List<JobContainerLogger> LoggersList { get; set; }

        /// <summary>
        /// Konstruktor - initialisiert die Liste.
        /// </summary>
        public LoggersContainer()
        {
            this.LoggersList = new List<JobContainerLogger>();
        }
    }

    /// <summary>
    /// Enthält eine Liste von Triggern des aktuellen Jobs.
    /// </summary>
    public class TriggersContainer
    {
        // Im JSON ist das Array in einer Eigenschaft "Trigger" eingebettet.
        /// <summary>
        /// Die eigentliche Liste.
        /// </summary>
        [JsonPropertyName("Trigger")]
        [XmlElement("Trigger")]
        public System.Collections.Generic.List<JobContainerTrigger> TriggersList { get; set; }

        /// <summary>
        /// Konstruktor - initialisiert die Liste.
        /// </summary>
        public TriggersContainer()
        {
            this.TriggersList = new List<JobContainerTrigger>();
        }
    }

    /// <summary>
    /// Enthält eine Liste von ValueModifiern des aktuellen Jobs.
    /// </summary>
    public class ValueModifiersContainer
    {
        // Im JSON ist das Array in einer Eigenschaft "ValueModifier" eingebettet.
        /// <summary>
        /// Die eigentliche Liste.
        /// </summary>
        [JsonPropertyName("ValueModifier")]
        [XmlElement("ValueModifier")]
        public System.Collections.Generic.List<JobContainerValueModifier> ValueModifiersList { get; set; }

        /// <summary>
        /// Konstruktor - initialisiert die Liste.
        /// </summary>
        public ValueModifiersContainer()
        {
            this.ValueModifiersList = new List<JobContainerValueModifier>();
        }
    }

    /// <summary>
    /// Enthält eine Liste von Checkern des aktuellen Jobs.
    /// </summary>
    public class CheckersContainer
    {
        // Im JSON ist das Array in einer Eigenschaft "Checker" eingebettet.
        /// <summary>
        /// Die eigentliche Liste.
        /// </summary>
        [JsonPropertyName("Checker")]
        [XmlElement("Checker")]
        public System.Collections.Generic.List<JobContainerChecker> CheckersList { get; set; }

        /// <summary>
        /// Konstruktor - initialisiert die Liste.
        /// </summary>
        public CheckersContainer()
        {
            this.CheckersList = new List<JobContainerChecker>();
        }
    }

    #endregion JobContainerListContainers

    /// <summary>
    /// Klasse, die einen strukturierten Parameter repräsentiert, wie er z.B.
    /// bei SubJobs von Escalatoren Verwendung findet.
    /// </summary>
    [XmlRoot("Parameters", Namespace = "")]
    public class StructuredParameters
    {
        /// <summary>
        /// Optionales Attribut; wenn gesetzt = "File".
        /// </summary>
        [XmlAttribute("Transport")]
        [JsonPropertyName("@Transport")]
        public string? Transport { get; set; }

        /// <summary>
        /// Liste von SubWorkern des aktuellen Workers.
        /// </summary>
        public SubWorkersContainer SubWorkers { get; set; }

        /// <summary>
        /// Standard Konstruktor.
        /// </summary>
        public StructuredParameters()
        {
            SubWorkers = new SubWorkersContainer();
        }
    }

    /// <summary>
    /// Custom-JsonConverter - deserialisiert eine Json-Struktur in einen String.
    /// </summary>
    public class CustomJsonConverter : JsonConverter<string>
    {
        /// <summary>
        /// Liest eine Json-Struktur oder einen Boolean-Skalar und gibt diese als String zurück.
        /// Diese Routine wird beim Deserialisieren direkt von der betroffenen Property
        /// aufgerufen (über das Attribut [JsonConverter(typeof(CustomJsonConverter))]).
        /// </summary>
        /// <param name="reader">Der Reader.</param>
        /// <param name="typeToConvert">Zu konvertierender Typ, hier immer String.</param>
        /// <param name="options">Serializer Optionen, z.B. PropertyNameCaseInsensitive.</param>
        /// <returns>Die in einen String konvertierte Json Struktur oder ein Boolean als String.</returns>
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.True:
                case JsonTokenType.False:
                    // Direkt übergebenen bool-Wert in json-String umwandeln
                    return "{ \"#text\": " + reader.GetBoolean().ToString().ToLower() + " }";
                case JsonTokenType.StartObject:
                case JsonTokenType.StartArray:
                case JsonTokenType.String:
                    using (var doc = JsonDocument.ParseValue(ref reader))
                    {
                        return doc.RootElement.GetRawText();
                    }
                default:
                    throw new JsonException($"Unsupported token type: {reader.TokenType}");
            }
        }

        /// <summary>
        /// Schreibt einen String in eine Json-Struktur.
        /// </summary>
        /// <param name="writer">Der Writer.</param>
        /// <param name="value">Der Json-String.</param>
        /// <param name="options">Serializer Optionen, z.B. PropertyNameCaseInsensitive.</param>
        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            // Wenn "true"/"false", als Boolean schreiben
            if (value == "true" || value == "false")
            {
                writer.WriteBooleanValue(bool.Parse(value));
            }
            else
            {
                using var jsonDoc = JsonDocument.Parse(value);
                jsonDoc.RootElement.WriteTo(writer);
            }
        }
    }
}