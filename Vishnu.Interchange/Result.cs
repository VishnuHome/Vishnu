using System.Collections.Generic;
using System;
using System.Runtime.Serialization;
using NetEti.ObjectSerializer;

namespace Vishnu.Interchange
{
    /// <summary>
    /// Nimmt die Ergebnisse eines Knotens des LogicalTaskTree auf.
    /// </summary>
    /// <remarks>
    /// File: Result.cs
    /// Autor: Erik Nagel
    ///
    /// 01.12.2012 Erik Nagel: erstellt
    /// </remarks>
    [DataContract] //[Serializable()]
    public class Result // : ISerializable
    {
        #region public members

        /// <summary>
        /// Eindeutige Kennung des Knotens, zu dem dieses Result gehört.
        /// </summary>
        [DataMember]
        public string Id { get; set; }

        /// <summary>
        /// Das logische Ergebnis der Verarbeitung des Teilbaums dieses Knotens.
        /// </summary>
        [DataMember]
        public bool? Logical { get; set; }

        /// <summary>
        /// Der Verarbeitungszustand des Knotens:
        /// None, Waiting, Working, Finished, Busy (= Waiting | Working) oder CanStart (= None|Finished).
        /// </summary>
        [DataMember]
        public NodeState? State { get; set; }

        /// <summary>
        /// Der Ergebnis-Zustand des Knotens:
        /// None, Done, Fault, Timeout, UserAbort.
        /// </summary>
        [DataMember]
        public NodeLogicalState? LogicalState { get; set; }

        /// <summary>
        /// Zeitpunkt der Entstehung des Results.
        /// </summary>
        [DataMember]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Ein eventuelles Rückgabeobjekt des Knoten (Checker).
        /// </summary>
        [AnonymousDataMember]
        public object? ReturnObject { get; set; }

        /// <summary>
        /// Standard Konstruktor.
        /// </summary>
        public Result()
        {
            this.Id = "anonymus";
        }

        /// <summary>
        /// Parametrisierter Konstruktor.
        /// </summary>
        /// <param name="id">Id des besitzenden Knoten.</param>
        /// <param name="logical">Logisches Ergebnis des besitzenden Knoten (true, false, null).</param>
        /// <param name="state">Verarbeitungszustand des besitzenden Knoten: None, Waiting, Working, Finished,
        /// Busy (= Waiting | Working) oder CanStart (= None|Finished).</param>
        /// <param name="logicalState">Logischer Zustand des besitzenden Knoten: None, Done, Fault, Timeout, UserAbort</param>
        /// <param name="returnObject">Ein beliebiges Object.</param>
        public Result(string id, bool? logical, NodeState? state, NodeLogicalState? logicalState, object? returnObject)
        {
            this.Id = id;
            this.Timestamp = DateTime.Now;
            this.Logical = logical;
            this.State = state;
            this.LogicalState = logicalState;
            this.ReturnObject = returnObject;
        }

        /// <summary>
        /// Deserialisierungs-Konstruktor.
        /// </summary>
        /// <param name="info">Property-Container.</param>
        /// <param name="context">Übertragungs-Kontext.</param>
        protected Result(SerializationInfo info, StreamingContext context)
        {
            Id = info.GetString("Id") ?? "anonymus";
            Timestamp = info.GetDateTime("Timestamp");
            Logical = (bool?)info.GetValue("Logical", typeof(bool?));
            State = (NodeState?)info.GetValue("State", typeof(NodeState));
            LogicalState = (NodeLogicalState?)info.GetValue("LogicalState", typeof(NodeLogicalState));
            ReturnObject = info.GetValue("ReturnObject", typeof(object));
        }

        /// <summary>
        /// Serialisierungs-Hilfsroutine: holt die Objekt-Properties in den Property-Container.
        /// </summary>
        /// <param name="info">Property-Container.</param>
        /// <param name="context">Serialisierungs-Kontext.</param>
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Id", Id);
            info.AddValue("Timestamp", Timestamp);
            info.AddValue("Logical", Logical);
            info.AddValue("State", State);
            info.AddValue("LogicalState", LogicalState);
            info.AddValue("ReturnObject", ReturnObject);
        }

        /// <summary>
        /// Überschriebene ToString()-Methode.
        /// </summary>
        /// <returns>Id des Knoten + ":" + ReturnObject.ToString()</returns>
        public override string ToString()
        {
            if (!String.IsNullOrEmpty(this.Id))
            {
                string rtn = (this.ReturnObject ?? "null").ToString() ?? "null";
                return this.Id + ": " + rtn;
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// Vergleicht Dieses Result mit einem übergebenen Result nach Inhalt.
        /// Der Timestamp wird bewusst nicht in den Vergleich einbezogen.
        /// </summary>
        /// <param name="obj">Vergleichs-Result.</param>
        /// <returns>True, wenn das übergebene Result inhaltlich (ohne Timestamp) gleich diesem Result ist.</returns>
        public override bool Equals(object? obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            if (Object.ReferenceEquals(this, obj))
            {
                return true;
            }
            Result? res = obj as Result;
            if (res?.Id == this.Id && res.Logical == this.Logical && res.LogicalState == this.LogicalState
              && (res.ReturnObject == null && this.ReturnObject == null || (res?.ReturnObject?.Equals(this.ReturnObject) == true)))
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
            return (this.Id + this.Logical.ToString() + this.LogicalState.ToString()
              + (this.ReturnObject == null ? "null" : this.ReturnObject.ToString())).GetHashCode();
        }
        #endregion public members

    }

    /// <summary>
    /// Typisierte Liste von Results.
    /// </summary>
    [DataContract]//[Serializable]
    public class ResultDictionary : Dictionary<string, Result?>
    {
        /// <summary>
        /// Standard Konstruktor.
        /// Dictionary&lt;string, Result&gt;.
        /// </summary>
        public ResultDictionary()
        {
        }

        /// <summary>
        /// Konstruktor für die Serializable-Funktionalität.
        /// </summary>
        /// <param name="info">Property-Container.</param>
        /// <param name="context">Serialisierungs-Kontext.</param>
        protected ResultDictionary(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

}
