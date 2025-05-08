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
        /// Verzögert den Start eines Knotens (und InitNodes).
        /// Kann für Loops in Controlled-Jobs verwendet werden.
        /// Default: 0 (Millisekunden).
        /// </summary>
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

        /// <summary>
        /// Bei True wird jeder Thread über die Klasse gesperrt, so dass
        /// nicht Thread-sichere Checker serialisiert werden;
        /// Default: False;
        /// </summary>
        [XmlElement("ThreadLocked")]
        [JsonPropertyName("ThreadLocked")]
        public JobContainerThreadLock? ThreadLocked { get; set; }

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

    /// <summary>
    /// Klasse, die einen ThreadLock repräsentiert mit der optionalen Property LockName.
    /// </summary>
    public class JobContainerThreadLock
    {
        /// <summary>
        /// Ein optionaler Key zur Differenzierung verschiedener Locking-Gruppen.
        /// </summary>
        [JsonPropertyName("LockName")]
        public string? LockName { get; set; }
    }

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

        /// <summary>
        /// Wenn gesetzt, wird der Checker nicht gleichzeitig mit anderen
        /// Checkern ausgeführt, die auch ThreadLocked gesetzt haben.
        /// Wenn LockName gesetzt ist, wird nur gegen Checker mit dem gleichen
        /// LockName gesperrt.
        /// </summary>
        [XmlElement("ThreadLocked")]
        [JsonPropertyName("ThreadLocked")]
        public JobContainerThreadLock? ThreadLocked { get; set; }

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
    
    /// <summary>
    /// Hilfsklasse, die einen RawJsonConverter implementiert.
    /// Deserialisiert eine Json-Struktur in einen String.
    /// </summary>
    public class RawJsonConverter : JsonConverter<string>
    {
        /// <summary>
        /// Liest eine Json-Struktur und gibt diese als String zurück.
        /// Diese Routine wird beim Deserialisieren direkt von der betroffenen Property
        /// aufgerufen (über das Attribut [JsonConverter(typeof(RawJsonConverter))]).
        /// </summary>
        /// <param name="reader">Der Reader.</param>
        /// <param name="typeToConvert">Zu konvertierender Typ, hier immer String.</param>
        /// <param name="options">Serializer Optionen, z.B. PropertyNameCaseInsensitive.</param>
        /// <returns>Die in einen String konvertierte Json Struktur.</returns>
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var jsonDoc = JsonDocument.ParseValue(ref reader);
            return jsonDoc.RootElement.GetRawText();
        }

        /// <summary>
        /// Schreibt einen String in eine Json-Struktur.
        /// </summary>
        /// <param name="writer">Der Writer.</param>
        /// <param name="value">Der Json-String.</param>
        /// <param name="options">Serializer Optionen, z.B. PropertyNameCaseInsensitive.</param>
        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            using var jsonDoc = JsonDocument.Parse(value);
            jsonDoc.RootElement.WriteTo(writer);
        }
    }    
    
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

}