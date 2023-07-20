using System;
using System.Runtime.Serialization;
using System.Text;

namespace Vishnu.DemoApplications.SingleNodeUserControlDemo
{
    /// <summary>
    /// Demo-Klasse zur Demonstration der Auflösung von komplexen
    /// Return-Objects in einem dynamisch geladenen UserNodeControl.
    /// </summary>
    /// <remarks>
    ///
    /// 12.03.2023 Erik Nagel: erstellt
    /// </remarks>
    [DataContract()] // [Serializable()]
    public class ComplexReturnObject
    {
        /// <summary>
        /// Testproperty für SingleNodeUserControl
        /// </summary>
        [DataMember]
        public string? TestProperty1 { get; set; }

        /// <summary>
        /// Testproperty für SingleNodeUserControl
        /// </summary>
        [DataMember]
        public int? TestProperty2 { get; set; }

        /// <summary>
        /// Standard Konstruktor.
        /// </summary>
        public ComplexReturnObject() { }

        /// <summary>
        /// Deserialisierungs-Konstruktor.
        /// </summary>
        /// <param name="info">Property-Container.</param>
        /// <param name="context">Übertragungs-Kontext.</param>
        protected ComplexReturnObject(SerializationInfo info, StreamingContext context)
        {
            this.TestProperty1 = info.GetString("TestProperty1");
            this.TestProperty2 = (int?)info.GetValue("TestProperty2", typeof(int));
        }

        /// <summary>
        /// Serialisierungs-Hilfsroutine: holt die Objekt-Properties in den Property-Container.
        /// </summary>
        /// <param name="info">Property-Container.</param>
        /// <param name="context">Serialisierungs-Kontext.</param>
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("TestProperty1", this.TestProperty1);
            info.AddValue("TestProperty2", this.TestProperty2);
        }

        /// <summary>
        /// Überschriebene ToString()-Methode - stellt alle öffentlichen Properties
        /// als einen (zweizeiligen) aufbereiteten String zur Verfügung.
        /// </summary>
        /// <returns>Alle öffentlichen Properties als ein String aufbereitet.</returns>
        public override string ToString()
        {
            StringBuilder str = new StringBuilder(String.Format("TestProperty1: {0}, TestProperty2: {1}",
                this.TestProperty1, this.TestProperty2));
            return str.ToString();
        }
    }
}
