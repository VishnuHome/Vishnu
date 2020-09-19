using System;
using NetEti.ApplicationControl;
using System.IO;
using System.Linq;
using NetEti.Globals;
using System.Xml.Linq;
using System.Threading;
using Vishnu.Interchange;
using System.Collections.Generic;
using NetEti.ObjectSerializer;
using System.Runtime.CompilerServices;

namespace LogicalTaskTree
{
    /// <summary>
    /// Statische Klasse für die asynchrone, thread-sichere Erzeugung von JobSnapsots.
    /// Bei mehrfach hintereinander eintreffenden Anforderungen wird eine gerade laufende
    /// Snapshot-Erzeugung nicht unterbrochen, sondern nur über ein Flag geregelt, dass direkt
    /// nach Fertigstellung des aktuellen Snapshots der nächste begonnen wird. Zwischenzeitliche
    /// Anforderungen werden verworfen.
    /// </summary>
    /// <remarks>
    /// File: SnapshotManager.cs
    /// Autor: Erik Nagel
    ///
    /// 26.12.2013 Erik Nagel: erstellt
    /// 14.07.2016 Erik Nagel: _taskWorker_TaskProgressFinished zum Loggen von Exceptions implementiert.
    /// </remarks>
    internal static class SnapshotManager
    {
        #region public members

        /// <summary>
        /// Fordert einen Snapshot des trees an.
        /// </summary>
        /// <param name="tree">Root des Trees</param>
        public static void RequestSnapshot(LogicalNode tree)
        {
            SnapshotManager._snapshotRequested = true;
            if (!SnapshotManager._isWorking)
            {
                try
                {
                    SnapshotManager._taskWorker.RunTask(SnapshotManager.processSnapshot, tree);
                }
#if DEBUG
                catch (System.InvalidOperationException)// ex)
                {
                    //InfoController.Say("SnapshotManager._taskWorker.RunTask: " + ex.Message);
                }
#else
                catch (System.InvalidOperationException) { }
#endif
            }
        }

        /// <summary>
        /// Löscht am Ende der Verarbeitung alle stehengebliebenen Snapshots.
        /// </summary>
        public static void CleanUp()
        {
            SnapshotManager.CleanUpSnapshots(true);
        }

        #endregion public members

        #region private members

        private static volatile bool _snapshotRequested;
        private static volatile bool _isWorking;
        private static TaskWorker _taskWorker;
        private static object _snapshotLocker;
        private static object _saveLocker;
        private static IInfoPublisher _publisher;

        /// <summary>
        /// Statischer Konstruktor - inizialisiert den SnapshotManager.
        /// </summary>
        static SnapshotManager()
        {
            SnapshotManager._snapshotLocker = new object();
            SnapshotManager._saveLocker = new object();
            SnapshotManager._isWorking = false;
            SnapshotManager._taskWorker = new TaskWorker();
            SnapshotManager._taskWorker.TaskProgressFinished += _taskWorker_TaskProgressFinished;
            SnapshotManager._publisher = InfoController.GetInfoPublisher();
        }

        private static void _taskWorker_TaskProgressFinished(object sender, Exception threadException)
        {
            SnapshotManager._publisher.Publish("SnapshotManager", threadException.ToString(), InfoType.Exception);
        }

        private static void processSnapshot(TaskWorker taskWorker, object tree)
        {
            // throw new ApplicationException("Test Exception zum Test des Loggings"); // TEST 14.07.2016 Nagel+-
            SnapshotManager._isWorking = true;
            while (SnapshotManager._snapshotRequested)
            {
                SnapshotManager._snapshotRequested = false;
                Exception exception = null;
                try
                {
                    lock (SnapshotManager._saveLocker) // 10.08.2016 Nagel+-
                    {
                        SnapshotManager.saveSnapshot((LogicalNode)tree);
                    }
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
                if (exception != null)
                {
                    SnapshotManager._isWorking = false;
                    throw exception;
                }
            }
            SnapshotManager._isWorking = false;
        }

        private static void saveSnapshot(LogicalNode tree)
        {
            AppSettings appSettings = GenericSingletonProvider.GetInstance<AppSettings>();
            string snapshotDirectory = Path.Combine(appSettings.ResolvedSnapshotDirectory, appSettings.MainJobName);
            if (!Directory.Exists(snapshotDirectory))
            {
                Directory.CreateDirectory(snapshotDirectory);
            }
            string snapshotName = String.Format($"JobSnapshot_{DateTime.Now:dd_HHmmss}.xml");
            string snapshotPath = Path.Combine(snapshotDirectory, snapshotName);
            string snapshotInfoPath = Path.Combine(snapshotDirectory, "JobSnapshot.info");
            XElement xmlTree = SnapshotManager.Tree2XML(tree);
            XElement snapShotXML = new XElement("Snapshot");
            snapShotXML.Add(new XAttribute("Type", "Snapshot"));
            if (appSettings.IsInSleepTime)
            {
                snapShotXML.Add(new XElement("SleepTimeTo", appSettings.SleepTimeTo.ToString()));
            }
            snapShotXML.Add(new XElement("Timestamp", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")),
                            new XElement("Path", Path.GetFullPath(snapshotPath)),
                            xmlTree);

            XDocument xmlDoc = new XDocument();
            xmlDoc.Add(snapShotXML);
            try
            {
                using (FileStream xmlStream = new FileStream(snapshotPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    xmlDoc.Save(xmlStream);
                }
                File.WriteAllText(snapshotInfoPath, snapshotPath);
            }
            catch (IOException)
            {
#if DEBUG
                InfoController.Say("Snapshot creation failed!");
#endif
            }
            xmlDoc = null;
            SnapshotManager.CleanUpSnapshots();
        }

        private static void CleanUpSnapshots(bool all = false)
        {
            AppSettings appSettings = GenericSingletonProvider.GetInstance<AppSettings>();
            string snapshotDirectory = Path.Combine(appSettings.ResolvedSnapshotDirectory, appSettings.MainJobName);
            if (Directory.Exists(snapshotDirectory))
            {
                string lastXmlFilePath = "";
                try
                {
                    lastXmlFilePath = File.ReadAllText(Path.Combine(snapshotDirectory, "JobSnapshot.info"));
                }
                catch { }
                FileInfo[] files = new DirectoryInfo(snapshotDirectory).GetFiles("JobSnapshot_*");
                foreach (FileInfo file in files)
                {
                    if (all)
                    {
                        try
                        {
                            File.Delete(Path.Combine(snapshotDirectory, "JobSnapshot.info"));
                        }
                        catch { }
                    }
                    if (all || (DateTime.UtcNow - file.CreationTimeUtc > appSettings.SnapshotSustain
                                && !lastXmlFilePath.Equals(file.FullName)))
                    {
                        try
                        {
                            File.Delete(file.FullName);
                        }
                        catch { }
                    }
                }
            }
        }

        /// <summary>
        /// Gibt den (Teil-)Baum in eine XML-Struktur aus.
        /// </summary>
        /// <returns>Baumdarstellung in einer XML-Struktur (XElement).</returns>
        private static XElement Tree2XML(LogicalNode tree)
        {
            return (XElement)tree.Traverse(Node2XML);
        }

        /// <summary>
        /// Erstellt für die übergebene LogicalNode einen XML-Snapshot.
        /// </summary>
        /// <param name="depth">Nullbasierter Zähler der Rekursionstiefe eines Knotens im LogicalTaskTree.</param>
        /// <param name="node">Basisklasse eines Knotens im LogicalTaskTree.</param>
        /// <param name="parent">Elternelement des User-Objekts.</param>
        /// <returns>User-Objekt.</returns>
        private static XElement Node2XML(int depth, LogicalNode node, object parent)
        {
            bool? LastMinuteNotNullLogical = node.LastNotNullLogical;
            /* // 19.08.2018 auskommentiert+
            LogicalNode treeEventSource = node.GetlastEventSourceIfIsTreeEventTriggered();
            if (treeEventSource != null)
            {
              // Da das TreeEvent, das dieses Speichern des Snapshots auslöst, u.U.
              // schneller sein kann, als das letzte Trigger-Event für den zu speichernden
              // Knoten, wird der logische Wert des Knotens vor dem Wegschreiben hier
              // noch einmal aktualisiert.
              // node.LastNotNullLogical = treeEventSource.LastNotNullLogical;
              LastMinuteNotNullLogical = treeEventSource.LastNotNullLogical;
            }
            */ // 19.08.2018 auskommentiert-
            XElement nodeXML;
            if (depth < 1)
            {
                nodeXML = new XElement("JobDescription");
            }
            else
            {
                nodeXML = new XElement("LogicalNode");
            }
            if (parent != null)
            {
                XElement xParent = (XElement)parent;
                xParent.Add(nodeXML);
            }
            nodeXML.Add(new XAttribute("Type", node.GetType().Name));
            nodeXML.Add(new XAttribute("NodeType", node.NodeType));
            nodeXML.Add(new XAttribute("Id", node.Id));
            nodeXML.Add(new XAttribute("Name", node.Name));
            nodeXML.Add(new XAttribute("LastNotNullLogical", LastMinuteNotNullLogical == null ? "" : LastMinuteNotNullLogical.ToString()));
            nodeXML.Add(new XAttribute("LastRun", node.LastRun.ToString()));
            nodeXML.Add(new XAttribute("NextRunInfo", node.NextRunInfo ?? ""));
            if (node.GetType().Name != "Snapshot")
            {
                nodeXML.Add(new XAttribute("Path", node.Path == null ? "" : node.Path));
            }
            else
            {
                nodeXML.Add(new XElement("Path", ((Snapshot)node).Path));
            }
            if (!String.IsNullOrEmpty(node.UserControlPath) && !node.UserControlPath.StartsWith("DefaultNodeControls"))
            {
                nodeXML.Add(new XElement("UserControlPath", node.UserControlPath));
            }
            if (node.LastExceptions.Count > 0)
            {
                try
                {
                    XElement lastExceptions = new XElement("LastExceptions");
                    // 06.07.2016 Nagel+ foreach (LogicalNode source in node.LastExceptions.Keys)
                    foreach (LogicalNode source in node.LastExceptions.Keys.ToList()) // 06.07.2016 Nagel-
                    {
                        lastExceptions.Add(new XElement(node.LastExceptions[source].GetType().Name, new XAttribute("Message", node.LastExceptions[source].Message)));
                    }
                    nodeXML.Add(lastExceptions);
                }
                catch (Exception ex)
                {
                    InfoController.Say(String.Format("Node2XML_a: {0}", ex.Message));
                    XElement result = new XElement("Result", new XCData(XMLSerializationUtility.SerializeObjectToBase64(
                        new Result(ex.GetType().Name, null, NodeState.Finished, NodeLogicalState.Fault, ex))));
                    nodeXML.Add(result);
                }
            }
            if (node is NodeConnector)
            {
                nodeXML.Add(new XElement("ReferencedNodeId", node.ReferencedNodeId));
                nodeXML.Add(new XElement("ReferencedNodeName", node.ReferencedNodeName));
                nodeXML.Add(new XElement("ReferencedNodePath", node.ReferencedNodePath));
                // 23.08.2018+
                try
                {
                    NodeConnector tmpNode = node as NodeConnector;
                    XElement result = new XElement("Result", new XCData(XMLSerializationUtility.SerializeObjectToBase64(tmpNode.LastResult)));
                    nodeXML.Add(result);
                }
                catch (Exception ex)
                {
                    InfoController.Say(String.Format("Node2XML_b: {0}", ex.Message));
                    XElement result = new XElement("Result", new XCData(XMLSerializationUtility.SerializeObjectToBase64(
                        new Result(ex.GetType().Name, null, NodeState.Finished, NodeLogicalState.Fault, ex))));
                    nodeXML.Add(result);
                }
                // 23.08.2018-
            }
            if (node is JobConnector)
            {
                nodeXML.Add(new XElement("LogicalExpression", (node as JobConnector).LogicalExpression));
            }
            if (node is SingleNode)
            {
                try
                {
                    SingleNode tmpNode = node as SingleNode;
                    XElement result = new XElement("Result", new XCData(XMLSerializationUtility.SerializeObjectToBase64(tmpNode.LastResult)));
                    nodeXML.Add(result);

                }
                catch (Exception ex)
                {
                    InfoController.Say(String.Format("Node2XML_b: {0}", ex.Message));
                    XElement result = new XElement("Result", new XCData(XMLSerializationUtility.SerializeObjectToBase64(
                        new Result(ex.GetType().Name, null, NodeState.Finished, NodeLogicalState.Fault, ex))));
                    nodeXML.Add(result);
                }
            }
            if (node is NodeList)
            {
                NodeList tmpNode = node as NodeList;
                List<object> xParamsList = new List<object>();
                xParamsList.Add(new XElement("LogicalName", node.Id.ToString()));
                if (node is JobList)
                {
                    XElement xmlLogicalExpression = new XElement("LogicalExpression", "");
                    xmlLogicalExpression.ReplaceNodes(new XCData(((JobList)node).LogicalExpression));
                    xParamsList.Add(xmlLogicalExpression);
                }
                if (node is Snapshot)
                {
                    xParamsList.Add(new XElement("Timestamp", ((Snapshot)node).Timestamp.ToString("dd.MM.yyyy HH:mm:ss")));
                    if (((Snapshot)node).IsDefaultSnapshot)
                    {
                        xParamsList.Add(new XElement("IsDefaultSnapshot", ((Snapshot)node).IsDefaultSnapshot.ToString()));
                    }
                }
                if (tmpNode.StartCollapsed)
                {
                    xParamsList.Add(new XElement("StartCollapsed", tmpNode.StartCollapsed.ToString()));
                }
                if (tmpNode.BreakWithResult)
                {
                    xParamsList.Add(new XElement("BreakWithResult", tmpNode.BreakWithResult.ToString()));
                }
                if (tmpNode.ThreadLocked)
                {
                    xParamsList.Add(new XElement("ThreadLocked", tmpNode.ThreadLocked.ToString()));
                }
                if (tmpNode.IsVolatile)
                {
                    xParamsList.Add(new XElement("IsVolatile", tmpNode.IsVolatile.ToString()));
                }
                object[] xParams = xParamsList.ToArray();
                nodeXML.Add(xParams);
            }
            return nodeXML;
        }

        #endregion private members

    }
}
