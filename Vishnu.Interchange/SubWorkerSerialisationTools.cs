using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Xml;
using NetEti.ObjectSerializer;
using System.Text.RegularExpressions;

namespace Vishnu.Interchange
{
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
        [JsonConverter(typeof(RawJsonConverter))]
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

}
