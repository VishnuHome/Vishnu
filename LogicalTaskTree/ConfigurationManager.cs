using NetEti.Globals;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Linq;
using Vishnu.Interchange;

namespace LogicalTaskTree
{
    /// <summary>
    /// Statische Klasse für die Speicherung von JobList-Zuständen auf User-Anforderung.
    /// Es wird für jede JobList gespeichert, ob sie in der Bildschirmdarstellung
    /// zusammengefaltet ist (collapsed) oder ihre Unterknoten sichtbar sind.
    /// </summary>
    /// <remarks>
    /// File: ConfigurationManager.cs
    /// Autor: Erik Nagel
    ///
    /// 10.02.2018 Erik Nagel: erstellt
    /// </remarks>
    public static class ConfigurationManager
    {
        #region public members

        /// <summary>
        /// Speichert die aktuelle Konfiguration im LocalConfigurationDirectory.
        /// Die aktuelle Konfiguration enthält die relevanten aktuellen UI-Eigenschaften (WindowAspects)
        /// und für jedes LogicalNodeViewModel, ob sein Knoten in der Bildschirmdarstellung
        /// zusammengefaltet ist (collapsed) oder dessen Unterknoten sichtbar sind.
        /// </summary>
        /// <param name="tree">Root-LogicalNodeViewModel des zu speichernden (Teil-)Trees.</param>
        /// <param name="treeOrientationState">Aktuelle Ausrichtung des LogicalTaskTree<br></br>
        ///   AlternatingHorizontal: Alternierender Aufbau, waagerecht beginnend.
        ///   Vertical: Senkrechter Aufbau.
        ///   Horizontal: Waagerechter Aufbau.
        ///   AlternatingVertical: Alternierender Aufbau, senkrecht beginnend.</param>
        /// <param name="windowAspects">Aktuelle UI-Eigenschaften (z.B. WindowTop, WindowWidth, ...).</param>
        public static void SaveLocalConfiguration(IExpandableNode tree, TreeOrientation treeOrientationState, WindowAspects windowAspects)
        {
            string? localConfigurationDirectory = GenericSingletonProvider.GetInstance<AppSettings>().LocalConfigurationDirectory;
            if (!String.IsNullOrEmpty(localConfigurationDirectory) && !Directory.Exists(Path.GetDirectoryName(localConfigurationDirectory)))
            {
                Directory.CreateDirectory(localConfigurationDirectory);
            }
            string localConfigurationPath = Path.Combine(localConfigurationDirectory ?? "", tree.Id + "_Configuration.xml");
            XElement xmlTree = ConfigurationManager.Tree2XML(tree);
            XElement configXml = new XElement("Configuration");
            configXml.Add(new XElement("Path", localConfigurationPath),
                          new XElement("Timestamp", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")),
                          new XElement("StartTreeOrientation", treeOrientationState.ToString()),
                          new XElement("WindowCenterX", windowAspects.WindowCenterX.ToString()),
                          new XElement("WindowCenterY", windowAspects.WindowCenterY.ToString()),
                          new XElement("WindowWidth", windowAspects.WindowWidth.ToString()),
                          new XElement("WindowHeight", windowAspects.WindowHeight.ToString()),
                          new XElement("WindowScrollLeft", windowAspects.WindowScrollLeft.ToString()),
                          new XElement("WindowScrollTop", windowAspects.WindowScrollTop.ToString()),
                          new XElement("WindowZoom", windowAspects.WindowZoom.ToString()),
                          new XElement("IsScrollbarVisible", windowAspects.IsScrollbarVisible.ToString()),
                          new XElement("ActTabControlTab", windowAspects.ActTabControlTab.ToString()),
                          new XElement("ActScreenIndex", windowAspects.ActScreenIndex.ToString()),
                          xmlTree);
            XDocument? xmlDoc = new XDocument();
            xmlDoc.Add(configXml);
            FileStream? xmlStream = null;
            try
            {
                xmlStream = new FileStream(localConfigurationPath, FileMode.Create, FileAccess.Write, FileShare.None);
                {
                    xmlDoc.Save(xmlStream);
                }
            }
            finally
            {
                if (xmlStream != null)
                {
                    xmlStream.Dispose();
                }
            }
            xmlDoc = null;
            ConfigurationManager.LoadLocalConfiguration(tree.Id);
        }

        /// <summary>
        /// Lädt die aktuelle Konfiguration aus LocalConfigurationPath.
        /// Die aktuelle Konfiguration speichert für jedes LogicalNodeViewModel,
        /// ob sein Knoten in der Bildschirmdarstellung zusammengefaltet ist (collapsed)
        /// oder dessen Unterknoten sichtbar sind.
        /// Die Sichtbarkeit der Knoten kann dann gemäß der geladenen
        /// Konfiguration wiederhergestellt werden.
        /// </summary>
        /// <param name="jobName">Job-Name des zu ladenden Jobs.</param>
        /// <returns>True, wenn eine lokale Konfiguration geladen werden konnte.</returns>
        public static bool LoadLocalConfiguration(string jobName)
        {
            string localConfigurationDirectory = GenericSingletonProvider.GetInstance<AppSettings>().LocalConfigurationDirectory ?? "";
            string localConfigurationPath = Path.Combine(localConfigurationDirectory, jobName + "_Configuration.xml");

            if (File.Exists(localConfigurationPath))
            {
                XDocument xDoc = XDocument.Load(localConfigurationPath);

                XNamespace? nameSpace = xDoc.Root?.Name?.Namespace;
                //XNamespace nameSpace = "";
                if (nameSpace != null)
                {
                    IEnumerable<XElement> nodes = xDoc.Descendants(nameSpace + "Node");
                    XElement? startTreeOrientation = xDoc.Descendants(nameSpace + "StartTreeOrientation")?.FirstOrDefault();
                    if (!String.IsNullOrEmpty(startTreeOrientation?.Value))
                    {
                        GenericSingletonProvider.GetInstance<AppSettings>().StartTreeOrientation
                            = (TreeOrientation)Enum.Parse(typeof(TreeOrientation), startTreeOrientation.Value);
                    }

                    XElement? xWindowCenter = xDoc.Descendants(nameSpace + "WindowCenterX")?.FirstOrDefault();
                    double windowCenterX =
                        String.IsNullOrEmpty(xWindowCenter?.Value) ? 0.0 : Convert.ToDouble(xWindowCenter.Value);
                    XElement? yWindowCenter = xDoc.Descendants(nameSpace + "WindowCenterY")?.FirstOrDefault();
                    double windowCenterY =
                        String.IsNullOrEmpty(yWindowCenter?.Value) ? 0.0 : Convert.ToDouble(yWindowCenter.Value);
                    XElement? xWindowWidth = xDoc.Descendants(nameSpace + "WindowWidth")?.FirstOrDefault();
                    double windowWidth = String.IsNullOrEmpty(xWindowWidth?.Value) ? 0.0 : Convert.ToDouble(xWindowWidth.Value);
                    XElement? xWindowHeight = xDoc.Descendants(nameSpace + "WindowHeight")?.FirstOrDefault();
                    double windowHeight = String.IsNullOrEmpty(xWindowHeight?.Value) ? 0.0 : Convert.ToDouble(xWindowHeight.Value);
                    XElement? xWindowScrollLeft = xDoc.Descendants(nameSpace + "WindowScrollLeft")?.FirstOrDefault();
                    double windowScrollLeft = String.IsNullOrEmpty(xWindowScrollLeft?.Value) ? 0.0 : Convert.ToDouble(xWindowScrollLeft.Value);
                    XElement? xWindowScrollTop = xDoc.Descendants(nameSpace + "WindowScrollTop")?.FirstOrDefault();
                    double windowScrollTop = String.IsNullOrEmpty(xWindowScrollTop?.Value) ? 0.0 : Convert.ToDouble(xWindowScrollTop.Value);
                    XElement? xWindowZoom = xDoc.Descendants(nameSpace + "WindowZoom")?.FirstOrDefault();
                    double windowZoom = String.IsNullOrEmpty(xWindowZoom?.Value) ? 1.0 : Convert.ToDouble(xWindowZoom.Value);
                    XElement? xIsScrollbarVisible = xDoc.Descendants(nameSpace + "IsScrollbarVisible")?.FirstOrDefault();
                    bool isScrollbarVisible = String.IsNullOrEmpty(xIsScrollbarVisible?.Value) ? true : Convert.ToBoolean(xIsScrollbarVisible.Value);
                    XElement? xActTabControlTab = xDoc.Descendants(nameSpace + "ActTabControlTab")?.FirstOrDefault();
                    int actTabControlTab = String.IsNullOrEmpty(xActTabControlTab?.Value) ? 0 : Convert.ToInt32(xActTabControlTab.Value);
                    XElement? xActScreenIndex = xDoc.Descendants(nameSpace + "ActScreenIndex")?.FirstOrDefault();
                    int actScreenIndex = String.IsNullOrEmpty(xActScreenIndex?.Value) ? 0 : Convert.ToInt32(xActScreenIndex.Value);
                    GenericSingletonProvider.GetInstance<AppSettings>().VishnuWindowAspects
                        = new WindowAspects()
                        {
                            WindowCenterX = windowCenterX,
                            WindowCenterY = windowCenterY,
                            WindowWidth = windowWidth,
                            WindowHeight = windowHeight,
                            WindowScrollLeft = windowScrollLeft,
                            WindowScrollTop = windowScrollTop,
                            WindowZoom = windowZoom,
                            IsScrollbarVisible = isScrollbarVisible,
                            ActTabControlTab = actTabControlTab,
                            ActScreenIndex = actScreenIndex
                        };

                    ConfigurationManager._nodeConfiguration.Clear();
                    for (int nodeIndex = 0; nodeIndex < nodes.Count(); nodeIndex++)
                    {
                        XElement? xNode = nodes.ElementAt(nodeIndex);
                        string? nodePath = xNode.Descendants(nameSpace + "Path").FirstOrDefault()?.Value.ToString();
                        bool? isExpanded = Convert.ToBoolean(xNode.Descendants(nameSpace + "IsExpanded")?.FirstOrDefault()?.Value);
                        if (isExpanded != null && nodePath != null)
                        {
                            ConfigurationManager._nodeConfiguration.Add(nodePath, isExpanded == true);
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Liefert für einen Knoten (nodePath) die Information, ob er
        /// zusammengefaltet oder expandiert dargestellt werden soll.
        /// Liefert dann null zurück, wenn entweder der Knoten keine
        /// NodeList ist oder keine lokale Konfiguration geladen wurde.
        /// </summary>
        /// <param name="nodePath">Pfad zum Knoten im Tree.</param>
        /// <returns>False, wenn der Knoten zusammengefaltet dargestellt werden soll;<br></br>
        /// True, wenn der Knoten nicht zusammengefaltet dargestellt werden soll;<br></br>
        /// Null, wenn der Knoten keine NodeList ist oder keine lokale Konfiguration geladen wurde.</returns>
        public static bool? IsExpanded(string nodePath)
        {
            if (ConfigurationManager._nodeConfiguration.ContainsKey(nodePath))
            {
                return ConfigurationManager._nodeConfiguration[nodePath];
            }
            return null;
        }

        /// <summary>
        /// Standard Konstruktor.
        /// </summary>
        static ConfigurationManager()
        {
            ConfigurationManager._nodeConfiguration = new Dictionary<string, bool>();
        }

        #endregion public members

        #region private members

        private static Dictionary<string, bool> _nodeConfiguration;

        /// <summary>
        /// Gibt den (Teil-)Baum in eine XML-Struktur aus.
        /// </summary>
        /// <returns>Baumdarstellung in einer XML-Struktur (XElement).</returns>
        private static XElement Tree2XML(IExpandableNode tree)
        {
            return (XElement?)tree.Traverse(Node2XML) ?? throw new ArgumentException($"Branch {tree.Name} konnte nicht in Xml übertragen werden.");
        }

        /// <summary>
        /// Erstellt für das übergebene LogicalNodeViewModel eine XML-Beschreibung.
        /// </summary>
        /// <param name="depth">Nullbasierter Zähler der Rekursionstiefe eines Knotens im LogicalTaskTree.</param>
        /// <param name="node">Basisklasse eines Knotens im LogicalTaskTree.</param>
        /// <param name="parent">Elternelement des User-Objekts.</param>
        /// <returns>XElement mit Knoten-Informationen.</returns>
        private static object? Node2XML(int depth, IExpandableNode node, object? parent)
        {
            XElement nodeXML = new XElement("Node");
            if (parent != null)
            {
                XElement xParent = (XElement)parent;
                xParent.Add(nodeXML);
            }
            nodeXML.Add(new XAttribute("Id", node.Id));
            nodeXML.Add(new XAttribute("Name", node.Name));
            nodeXML.Add(new XElement("Path", node.Path));
            nodeXML.Add(new XElement("IsExpanded", node.IsExpanded.ToString()));
            return nodeXML;
        }

        #endregion private members

    }
}
