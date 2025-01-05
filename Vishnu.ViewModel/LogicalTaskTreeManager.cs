using LogicalTaskTree;
using NetEti.ApplicationControl;
using NetEti.CustomControls;
using NetEti.FileTools;
using NetEti.Globals;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Shapes;
using System.Windows.Threading;
using Vishnu.Interchange;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Vishnu.ViewModel
{
    /// <summary>
    /// LogicalTaskTreeManager bietet Funktionen zum Verwalten von LogicalTaskTrees:
    ///   - Mergen eines neu geladenen Trees in einen bestehenden aktiven Tree,
    ///   - Rekursives Loggen wichtiger Eigenschaften von Teilbäumen eines LogicalTaskTrees.
    /// </summary>
    /// <remarks>
    /// Author: Erik Nagel, NetEti
    ///
    /// 01.01.2021 Erik Nagel, NetEti: created.
    /// </remarks>
    internal static class LogicalTaskTreeManager
    {

        /// <summary>
        /// Loggt den für Debug-Zwecke zum aktuellen Zeitpunkt.
        /// Das Logging beginnt ab dem übergebenen logicalNodeViewModel, wenn es ein JobListViewModel ist,
        /// ansonsten ab der nächsten diesem übergeordneten Joblist.
        /// Wenn der Zusatzparameter "fromTop" auf true steht, wird der gesamte Tree geloggt.
        /// </summary>
        /// <param name="logicalNodeViewModel">Ein LogicalNodeViewModel innerhalb des Trees.</param>
        /// <param name="fromTop">Bei true wird der gesamte Tree geloggt, default: false.</param>
        /// <param name="header">Optionale Überschritf.</param>
        public static void LogTaskTree(LogicalNodeViewModel logicalNodeViewModel, bool fromTop = false,
            string header = "--- V I S H N U - T R E E -----------------------------------------------------------------------------------------------------")
        {
            JobListViewModel? rootJobListViewModel;
            if (!fromTop)
            {
                if (logicalNodeViewModel is JobListViewModel)
                {
                    rootJobListViewModel = (JobListViewModel)logicalNodeViewModel;
                }
                else
                {
                    rootJobListViewModel = logicalNodeViewModel.RootJobListViewModel;
                }
            }
            else
            {
                rootJobListViewModel = logicalNodeViewModel.GetTopRootJobListViewModel() ?? ((JobListViewModel)logicalNodeViewModel);
            }
            JobList? rootJobList = (JobList?)rootJobListViewModel?.GetLogicalNode();
            List<object> allReferencedObjects = new List<object>();
            rootJobListViewModel?.Traverse(CollectReferencedObjects, allReferencedObjects);

            List<string> allTreeInfos = new List<string>();
            object? result1 = rootJobListViewModel?.Traverse(ListTreeElement, allTreeInfos);
            string bigMessage = string.Join(System.Environment.NewLine, allTreeInfos);
            InfoController.GetInfoPublisher().Publish(rootJobListViewModel, Environment.NewLine
                + header, InfoType.NoRegex);
            Thread.Sleep(100);
            InfoController.GetInfoPublisher().Publish(rootJobListViewModel, Environment.NewLine + bigMessage, InfoType.NoRegex);
            Thread.Sleep(100);
            InfoController.GetInfoPublisher().Publish(rootJobListViewModel, Environment.NewLine
                + "-------------------------------------------------------------------------------------------------------------------------------", InfoType.NoRegex);
            InfoController.FlushAll();
        }

        /// <summary>
        /// Öffnet die Logdatei im Standardeditor.
        /// </summary>
        public static void ShowLogTaskTree()
        {
            InfoController.GetInfoController().Show();
        }

        /// <summary>
        /// Gibt die Vishnu-Parameter im NotePad-Editor aus.
        /// </summary>
        public static void ShowSettingsTaskTree(SortedDictionary<string, string> settings)
        {
            EditorCaller editorCaller = new EditorCaller();
            List<string>? content = new List<string>();

            foreach (string parameter in settings.Keys)
            {
                if (parameter != "__NOPPES__")
                {
                    content.Add(parameter + ": " + settings[parameter]);
                }
            }
            editorCaller.Edit(System.IO.Path.Combine(Environment.GetEnvironmentVariable("TEMP") ?? "", "VishnuSettings.log"), content);
        }

        /// <summary>
        /// Zeigt die Vishnu Hilfe an.
        /// Wenn der Parameter "HelpPreference" gleich "local" gesetzt ist,
        /// wird versucht lokal "Vishnu_doc.de.chm" zu laden,
        /// andernfalls wird versucht, die Vishnu Onlinehilfe zu laden.
        /// </summary>
        public static void ShowVishnuHelp()
        {
            if (GenericSingletonProvider.GetInstance<AppSettings>().HelpPreference == "local")
            {
                ShowLocalVishnuHelp();
            }
            else
            {
                ShowVishnuOnlineHelp();
            }
        }

        /// <summary>
        /// Zeigt die Vishnu Onlinehilfe an.
        /// </summary>
        /// <param name="redirected">True, wenn schon vorher die lokale CHM-Datei versucht wurde; Default: false</param>
        public static void ShowVishnuOnlineHelp(bool redirected = false)
        {
            string vishnuHelpBrowserPath = System.IO.Path.Combine(
                GenericSingletonProvider.GetInstance<AppSettings>().UserAssemblyDirectory, "VishnuHelpBrowser.exe");
            try { Process.Start(vishnuHelpBrowserPath, GenericSingletonProvider.GetInstance<AppSettings>().Language); }
            catch (Exception ex)
            {
                if (!redirected)
                {
                    System.Windows.MessageBox.Show(ex.Message, "Kann die Vishnu Onlinehilfe nicht aufrufen, versuche die lokale Hilfe ...",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    ShowLocalVishnuHelp(redirected=true);
                }
                else
                {
                    System.Windows.MessageBox.Show(ex.Message, "Kann die Vishnu Onlinehilfe nicht aufrufen, gebe auf.",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Zeigt die lokale Vishnu Hilfe (chm) an.
        /// </summary>
        /// <param name="redirected">True, wenn schon vorher die Onlinehilfe versucht wurde; Default: false</param>
        public static void ShowLocalVishnuHelp(bool redirected = false)
        {
            string vishnuDocPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(
                GenericSingletonProvider.GetInstance<AppSettings>().VishnuSourceRoot, @"Vishnu_doc.de.chm"));
            if (!File.Exists(vishnuDocPath))
            {
                vishnuDocPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(
                    GenericSingletonProvider.GetInstance<AppSettings>().VishnuRoot, @"Vishnu_doc.de.chm"));
            }
            if (File.Exists(vishnuDocPath))
            {
                System.Windows.Forms.Help.ShowHelp(null, vishnuDocPath);
            }
            else
            {
                if (!redirected)
                {
                    System.Windows.MessageBox.Show("Kann die lokale Vishnu Hilfe nicht aufrufen, versuche die Onlinehilfe ...",
                        "Fehler",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    ShowVishnuOnlineHelp(redirected = true);
                }
                else
                {
                    System.Windows.MessageBox.Show("Kann die lokale Vishnu Hilfe nicht aufrufen, gebe auf.",
                        "Fehler",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            // Der Teil unten funktioniert nicht, sondern wirft folgende Exception:
            // An error occurred trying to start process
            // 'C:\Users\micro\Documents\private4\WPF\openVishnu8\Vishnu_doc.de.chm' with working directory ...
            // The specified executable is not a valid application for this OS platform.
            /*
            try { Process.Start(vishnuDocPath); }
                catch (Exception ex)
                {
                    if (!redirected)
                    {
                        System.Windows.MessageBox.Show(ex.Message, "Kann die lokale Vishnu Hilfe nicht aufrufen, versuche die online Hilfe ...",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    ShowVishnuOnlineHelp(redirected = true);
                }
                else
                {
                    System.Windows.MessageBox.Show(ex.Message, "Kann die lokale Vishnu Hilfe nicht aufrufen, gebe auf.",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            */
        }

        static LogicalTaskTreeManager()
        {
            LogicalTaskTreeManager._reloadedSingleNodes = new List<string>();
            LogicalTaskTreeManager._shadowVMFinder = new Dictionary<string, LogicalNodeViewModel>();
            LogicalTaskTreeManager._activeVMFinder = new Dictionary<string, LogicalNodeViewModel>();
        }

        private static Dictionary<string, LogicalNodeViewModel> _shadowVMFinder;
        private static Dictionary<string, LogicalNodeViewModel> _activeVMFinder;
        private static Dispatcher? _dispatcher;

        // Enthält Pfade von SingleNodes, die im Zuge eines Reload des LogicalTaskTrees neu eingebaut wurden.
        // Bei der späteren Ausführung der Run-Methode dieser Nodes dürfen dynamisch geladene DLLs einmalig
        // nicht aus dem Cache genommen werden.
        private static List<string> _reloadedSingleNodes { get; set; }

        /// <summary>
        /// Verarbeitet zwei übergebene Trees aus LogicNodeViewModels:
        /// "activeTree" und "newTree".
        /// Im Prinzip soll nach der Verarbeitung in LogicalTaskTreeMerger Vishnu ohne Unterbrechung mit
        /// einer Repräsentation des newTrees weiterarbeiten. Hierbei sollen nur neu hinzugekommene oder
        /// grundlegend veränderte Knoten neu gestartet werden müssen.
        /// Aktive oder unveränderte Knoten im activeTree, die auch im newTree vorkommen, sollen möglichst
        /// unangetastet bleiben.
        /// Das heißt, dass ihre aktuellen Verarbeitungszustände, logischen Werte, Trigger, Logger, etc.
        /// erhalten bleiben (sie insbesondere nicht neu gestartet werden müssen).
        /// Aktive Knoten im activeTree, die nicht mehr im newTree vorkommen, müssen geordnet beendet und
        /// freigegeben werden.
        /// Besonders zu beachten:
        ///   - Differenzierte Verarbeitung von JobLists, bei denen sich die LogicalExpression geändert hat;
        ///     Hier müssen die JobLists getauscht werden, aber evtl. gleich gebliebene Kinder unangetastet
        ///     bleiben.
        ///   - Merging von für JobLists und Tree globalen Arrays und Dictionaries:
        ///     Job
        ///       ...
        ///           #region tree globals
        ///   
        ///           /// Liste von internen Triggern für einen jobPackage.Job.
        ///           public Dictionary «string, Dictionary«string, TriggerShell»» EventTriggers { get; set; }
        ///                                 |                  |
        ///                                 |                  +-> Quelle, z.B SubJob1 oder CheckTreeEvents
        ///                                 |
        ///                                 +-> Events string, z.B. "AnyException|LastNotNullLogicalChanged"
        ///             EventTriggers werden nach oben in die TopRootJobList propagiert.
        ///   
        ///           /// Liste von externen Arbeitsroutinen für einen jobPackage.Job.
        ///           /// Ist ein Dictionary mit WorkerShell-Arrays zu aus
        ///           /// Knoten-Id + ":" + TreeEvents-String gebildeten Keys.
        ///           public Workers Workers { get; private set; }
        ///             public Dictionary«string, Dictionary«string, WorkerShell[]»» WorkersDictionary { get; set; }
        ///                                |                  |
        ///                                |                  +-> Quelle, z.B. SQLSERVER_Job
        ///                                |
        ///                                +-> einzelnes Event, z.B. "AnyException" oder "LastNotNullLogicalChanged"
        ///             Workers werden nicht nach oben in die TopRootJobList propagiert,
        ///             sondern sind für jede JobList spezifisch.
        ///   
        ///           #endregion tree globals
        ///           ...
        ///   
        ///     JobList
        ///       ...
        ///           #region tree globals
        ///   
        ///           // Der externe Job mit logischem Ausdruck und u.a. Dictionary der Worker.
        ///           public Job Job { get; set; }
        ///   
        ///           nur temporär /// Dictionary von externen Prüfroutinen für einen jobPackage.Job mit Namen als Key.
        ///                        /// Wird als Lookup für unaufgelöste JobConnector-Referenzen genutzt.
        ///                        public Dictionary«string, NodeCheckerBase» AllCheckersForUnreferencingNodeConnectors { get; set; }
        ///                                           |
        ///                                           +-> Checker-Name, z.B. "Datum"
        ///                        AllCheckersForUnreferencingNodeConnectors werden nach oben in die TopRootJobList propagiert,
        ///                        aber bei der Auflösung von unreferenzierenden NodeConnectoren nach und nach entfernt.
        ///                        Das heißt, dass in der TopRootJobList AllCheckersForUnreferencingNodeConnectors nach der initialen
        ///                        Verarbeitung leer sein muss, ansonsten sind NodeConnectoren ohne gültige Referenz übrig geblieben.
        ///   
        ///           nur temporär /// Liste von NodeConnectoren, die beim Parsen der Jobs noch nicht aufgelöst
        ///                        /// werden konnten.
        ///                        public List«NodeConnector» UnsatisfiedNodeConnectors;
        ///   
        ///           /// Dictionary von externen Prüfroutinen für eine JobList, die nicht in
        ///           /// der LogicalExpression referenziert werden; Checker, die ausschließlich
        ///           /// über ValueModifier angesprochen werden.
        ///           public Dictionary«string, NodeCheckerBase» TreeExternalCheckers { get; set; }
        ///                              |
        ///                              +-> Checker-Id, z.B. "Datum" (NodeName wird zur Id)
        ///             TreeExternalCheckers werden nicht nach oben in die TopRootJobList propagiert,
        ///             sondern sind für jede JobList spezifisch.
        ///   
        ///           /// Liste von externen SingleNodes für die TopRootJobList, die in keiner
        ///           /// der LogicalExpressions referenziert werden; Nodes, die ausschließlich
        ///           /// über NodeConnectoren angesprochen werden.
        ///           public List«SingleNode» TreeExternalSingleNodes { get; set; }
        ///                        |
        ///                        +-> SingleNode Binary
        ///             TreeExternalSingleNodes werden nicht nach oben in die TopRootJobList propagiert,
        ///             sondern sind für jede JobList spezifisch.
        ///   
        ///           /// Cache zur Beschleunigung der Verarbeitung von TreeEvents
        ///           /// bezogen auf EventTrigger.
        ///           public List«string» TriggerRelevantEventCache;
        ///                        |
        ///                        +-> einzelnes Event, z.B. "AnyException" oder "LastNotNullLogicalChanged"
        ///             TriggerRelevantEventCache wird nach oben in die TopRootJobList propagiert.
        ///             
        ///           /// Cache zur Beschleunigung der Verarbeitung von TreeEvents
        ///           /// bezogen auf Worker.
        ///           public List«string» WorkerRelevantEventCache;
        ///                        |
        ///                        +-> einzelnes Event, z.B. "AnyException" oder "LastNotNullLogicalChanged"
        ///             Hinweis: Vishnu fügt allen JobLists in WorkerRelevantEventCache das Event 'Breaked' hinzu. 
        ///             WorkerRelevantEventCache wird nach oben in die TopRootJobList propagiert.
        ///   
        ///           /// Cache zur Beschleunigung der Verarbeitung von TreeEvents
        ///           /// bezogen auf Logger.
        ///           public List«string» LoggerRelevantEventCache;
        ///                        |
        ///                        +-> einzelnes Event, z.B. "AnyException" oder "LastNotNullLogicalChanged"
        ///             LoggerRelevantEventCache wird nach oben in die TopRootJobList propagiert.
        ///   
        ///           /// Dictionary von JobLists mit ihren Namen als Keys.
        ///           public Dictionary«string, JobList> JobsByName;
        ///                              |
        ///                              +-> einzelne Job-Id, z.B. "CheckServers" oder "SubJob1"
        ///             JobsByName werden nach oben in die TopRootJobList propagiert.
        ///   
        ///           /// Dictionary von LogicalNodes mit ihren Namen als Keys.
        ///           public Dictionary«string, LogicalNode» NodesByName;
        ///                              |
        ///                              +-> einzelne Knoten-Id, z.B. "Datum" oder "Check_D"
        ///             NodesByName werden nicht nach oben in die TopRootJobList propagiert,
        ///             sondern sind für jede JobList spezifisch.
        ///   
        ///           /// Dictionary von LogicalNodes mit ihren Namen als Keys.
        ///           public Dictionary«string, LogicalNode» TreeRootLastChanceNodesByName;
        ///                              |
        ///                              +-> einzelne Knoten-Id, z.B. "Datum" oder "Check_D"
        ///             TreeRootLastChanceNodesByName werden nach oben in die TopRootJobList propagiert.
        ///   
        ///           /// Dictionary von LogicalNodes mit ihren Ids als Keys.
        ///           public Dictionary«string, LogicalNode» NodesById;
        ///                              |
        ///                              +-> einzelne Knoten-Id, z.B. "Datum" oder "Check_D" oder "Child_1_@26"
        ///             NodesById werden nach oben in die TopRootJobList propagiert.
        ///   
        ///           #endregion tree globals
        /// 
        /// </summary>
        internal static void MergeTaskTrees(JobListViewModel activeTree, JobListViewModel newTree, Dispatcher? dispatcher)
        {
            LogicalTaskTreeManager._dispatcher = dispatcher;
            try
            {
                // Trees indizieren.
                _shadowVMFinder.Clear();
                newTree.Traverse(DoIndexTreeElement, LogicalTaskTreeManager._shadowVMFinder);
                _activeVMFinder.Clear();
                activeTree.Traverse(DoIndexTreeElement, LogicalTaskTreeManager._activeVMFinder);

                // Da im aktiven Tree Knoten ausgetauscht werden, kann es zu null-Referenzen kommen.
                // Deshalb werden ab hier alle kritischen Aktionen pausiert, bis der Reload durch ist.
                LogicalNode.ProhibitSnapshots();

                // Durchläuft den aktiven Tree und prüft parallel den neu geladenen "ShadowTree" auf Gleichheit, bzw. Ungleichheit.
                // Da hinzugekommene oder ausgetauschte Knoten immer auch über Unterschiede in den LogicalExpressions der
                // übergeordneten JobLists gefunden werden, bleiben keine Veränderungen unbemerkt.
                // Bei differierenden JobLists wird aus pragmatischen Erwägungen zusätzlich noch versucht, eventuell gleich
                // gebliebene Children zu erhalten, damit bei großen Jobs nicht alle Children neu gestartet werden müssen.
                // Darüber hinaus möglicherweise noch gleiche Teilbäume an unterschiedlichen Hierarchie-Ebenen werden nicht
                // gesucht, der Nutzen wäre eher gering bei ungleich höherem Aufwand (Ehrlich gesagt, ist mir das zu kompliziert).
                activeTree.Traverse(DiffTreeElement, LogicalTaskTreeManager._shadowVMFinder);


                // Abschließend muss noch die Jobs-Ansicht aktualisiert werden (LogicalTaskJobGroupsControl).
                InfoController.GetInfoPublisher().Publish(activeTree, Environment.NewLine
                    + "--- R E F R E S H  J O B G R O U P S ------------------------------------------------------------------------------------------", InfoType.NoRegex);
                // Thread.Sleep(100);
                activeTree.TreeParams.ViewModelRoot?.RefreshDependentAlternativeViewModels();
                // Thread.Sleep(100);
                //InfoController.GetInfoPublisher().Publish(activeTree, Environment.NewLine
                //    + "-------------------------------------------------------------------------------------------------------------------------------", InfoType.NoRegex);
                InfoController.FlushAll();
            }
            finally
            {
                newTree.Dispose();
                VishnuAssemblyLoader.ClearCache(); // Sorgt dafür, dass alle DLLs neu geladen werden.
                LogicalNode.AllowSnapshots();
            }
        }

        /// <summary>
        /// Vergleicht die übergebene ExpandableNode mit ihrem Pendant in dem Shadow-Tree.
        /// </summary>
        /// <param name="depth">Nullbasierter Zähler der Rekursionstiefe eines Knotens im LogicalTaskTree.</param>
        /// <param name="expandableNode">Basisklasse eines ViewModel-Knotens im LogicalTaskTree.</param>
        /// <param name="userObject">Ein beliebiges durchgeschliffenes UserObject (hier: Dictionary&lt;string, LogicalNodeViewModel&gt;).</param>
        /// <returns>Das Dictionary&lt;string, LogicalNodeViewModel&gt; oder null.</returns>
        internal static object? DiffTreeElement(int depth, IExpandableNode expandableNode, object? userObject)
        {
            Dictionary<string, LogicalNodeViewModel>? vmFinder = (Dictionary<string, LogicalNodeViewModel>?)userObject;
            if (vmFinder != null && vmFinder.ContainsKey(expandableNode.Path))
            {
                LogicalNodeViewModel shadowNodeVM = vmFinder[expandableNode.Path];
                LogicalNodeViewModel activeNodeVM = (LogicalNodeViewModel)expandableNode;
                LogicalNode shadowNode = shadowNodeVM.GetLogicalNode();
                LogicalNode activeNode = activeNodeVM.GetLogicalNode();
                if (shadowNode is JobList && activeNode is JobList)
                {
                    LogicalTaskTreeManager.TransferGlobals((JobListViewModel)shadowNodeVM, (JobListViewModel)activeNodeVM, (JobList)shadowNode, (JobList)activeNode);
                }
                if (shadowNode.Equals(activeNode))
                {
                    return userObject; // Weiter im Tree
                }
                else
                {
                    LogicalTaskTreeManager.TransferNode(shadowNodeVM, activeNodeVM, shadowNode, activeNode);
                }
            }
            else
            {
                throw new ApplicationException(string.Format($"Unerwarteter Suchfehler auf {expandableNode.Path}."));
            }

            return null; // Bricht die Rekursion für diesen Zweig ab.
        }


        delegate void TransferNodeDelegate(LogicalNodeViewModel shadowNodeVM, LogicalNodeViewModel activeNodeVM, LogicalNode shadowNode, LogicalNode activeNode);
        private static void TransferNode(LogicalNodeViewModel shadowNodeVM, LogicalNodeViewModel activeNodeVM, LogicalNode shadowNode, LogicalNode activeNode)
        {
            if (!LogicalTaskTreeManager._dispatcher?.CheckAccess() == true)
            {
                LogicalTaskTreeManager._dispatcher?.Invoke(DispatcherPriority.Background,
                    new TransferNodeDelegate(TransferNode), shadowNodeVM, activeNodeVM, shadowNode, activeNode);
                return;
            }

            if (activeNode.Mother == null || shadowNode.Mother == null)
            {
                // IBusinessLogicRoot xxx = activeNode.TreeParams.BusinessLogicRoot;
                // IViewModelRoot yyy = activeNode.TreeParams.ViewModelRoot;

                // throw new ApplicationException("Der Top-Job kann nicht im laufenden Betrieb ausgetauscht werden.");
                // TODO: dies auch ermöglichen. Ansatz: oberhalb des Top-Knoten schon beim Laden der JobDescription.xml einen Pseudo-Knoten erzeugen.
                System.Windows.MessageBox.Show("Der oberste Job kann nicht im laufenden Betrieb geändert werden.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Hand);
                InfoController.Say(string.Format($"#RELOAD# Not transferring Root-Node {shadowNodeVM.Path} from ShadowTree to Tree."));
                return;
            }

            LogicalNodeViewModel? activeParentVM = activeNodeVM.Parent;
            NodeParent activeParent = (NodeParent)activeNode.Mother;
            LogicalNodeViewModel? shadowParentVM = shadowNodeVM.Parent;
            NodeParent shadowParent = (NodeParent)shadowNode.Mother;
            if (activeParentVM == null || shadowParentVM == null)
            {
                throw new ApplicationException("activeParentVM == null || shadowNodeVM == null");
            }
            int nodeIndex = GetParentNodeIndex(activeNode, activeParent);

            // commonChildIndices: Key=activeNode.Children, Value=shadowNode.Children
            Dictionary<int, int>? commonChildIndices = null;
            if (shadowNode is JobList && activeNode is JobList)
            {
                commonChildIndices = LogicalTaskTreeManager.FindEqualJobListChildrenAndSetDummyConstants(
                    (JobListViewModel)shadowNodeVM, (JobListViewModel)activeNodeVM, (JobList)shadowNode, (JobList)activeNode);
            }

            LogTaskTree(activeNodeVM, fromTop: true,
                header: "--- V I S H N U - T R E E   1   -----------------------------------------------------------------------------------------------");
            Thread.Sleep(10);

            try
            {
                // Knoten aus dem aktiven Tree sichern ist nicht nötig, da schon durch activeNode und activeNodeVM referenziert.

                // Knoten im aktiven Tree durch Knoten im Shadow-Tree ersetzen.
                shadowNodeVM.Parent = activeParentVM;
                shadowNodeVM.RootLogicalTaskTreeViewModel = activeNodeVM.RootLogicalTaskTreeViewModel;

                shadowNode.Mother = activeParent;
                shadowNode.RootJobList = activeNode.RootJobList;
                shadowNode.TreeRootJobList = activeNode.TreeRootJobList;

                activeParentVM.Children[nodeIndex].ReleaseBLNode(); // nur freigeben, nicht disposen
                activeParent.ReleaseChildAt(nodeIndex); // nur freigeben, nicht disposen

                activeParent.SetChildAt(nodeIndex, shadowNode);
                //activeParent.Children.RemoveAt(nodeIndex);
                //activeParent.Children.Insert(nodeIndex, shadowNode);
                //activeParent.SetChildAt(nodeIndex, shadowNode);

                // activeParentVM.Children[nodeIndex] = shadowNodeVM;
                activeParentVM.Children.RemoveAt(nodeIndex);
                activeParentVM.Children.Insert(nodeIndex, shadowNodeVM);
                activeParentVM.Children[nodeIndex].SetBLNode(shadowNode, true);

                // Der Run auf die übernommene ShadowNode darf erst an dieser Stelle erfolgen, also wenn die shadowNode schon im aktiven
                // Tree hängt. Anderenfalls bekommt der übergeordnete Knoten den Lauf des neuen Knoten und sein LogicalResult nicht mit.
                activeParent.LastLogical = null; // Neu-Auswertung erzwingen.

                shadowParentVM.ReleaseBLNode();
                /*
                if (shadowParentVM.Children.Count <= nodeIndex)
                {
                    LogTaskTree(activeParentVM.GetTopRootJobListViewModel(), true);
                    Thread.Sleep(1005);
                    LogTaskTree(shadowParentVM.GetTopRootJobListViewModel(), true);
                    Thread.Sleep(1005);
                }
                */
                shadowParent.ReleaseChildAt(nodeIndex);

                //LogTaskTree(shadowNodeVM, fromTop: true,
                //    header: "--- V I S H N U - T R E E  shadow ---------------------------------------------------------------------------------------------");
                //Thread.Sleep(10);

                shadowNode.Run(new TreeEvent("UserRun", shadowNode.Id, shadowNode.Id, shadowNode.Name, shadowNode.Path, null, NodeLogicalState.None, null, null));

                Thread.Sleep(300); // Der Run braucht etwas, deshalb muss hier eine Wartezeit eingebaut werden.

                // Werte vom shadowParent in den activeParent übernehmen, die sonst nur bei der Erstinitialisierung gesetzt werden (primär LastSingleNodes).
                activeParentVM.InitFromNode(shadowParentVM);
                activeParentVM.Invalidate();

                // Das shadowParentVM und der shadowParent halten noch Referenzen aufeinander
                // und auf den Shadow-Knoten, der gerade in den activeTree hinüberwandert.
                // Der Shadow-Tree wird in einem abschließenden Schritt in LogicalNodeViewModel disposed.
                // Deshalb müssen seine Verbindungen zu dem in den active-Tree wechselnden Knoten
                // vorher gekappt werden (sonst würde dieser gleich wieder mit disposed).
                // shadowParentVM.Children[nodeIndex].ReleaseBLNode(); // nicht erforderlich.

                if (commonChildIndices != null)
                {
                    // Hier wurde geade eine JobList in den aktiven Tree übernommen und gestartet, die an den Stellen, wo sie mit der
                    // ursprünglichen JobList gemeinsame Kinder hat, noch Dummy-Knoten enthält. Diese Dummy-Knoten müssen jetzt durch
                    // die ursprünglichen, noch laufenden Kinder der Original-JobList (von vor dem Austausch durch die Shadow-JobList)
                    // des aktiven Trees wieder ersetzt werden.
                    JobList activeJobList = (JobList)activeNode;
                    JobList shadowJobList = (JobList)shadowNode;
                    JobListViewModel shadowJobListVM = (JobListViewModel)shadowNodeVM;
                    JobListViewModel activeJobListVM = (JobListViewModel)activeNodeVM;
                    int removedNodesCounter = 0;
                    for (int i = 0; i < activeJobList.Children.Count; i++) // activeNode (JobList) zeigt hier noch auf den ursprünglichen Knoten des activeTree
                    {
                        int k = i + removedNodesCounter;
                        if (commonChildIndices.ContainsKey(k))
                        {
                            // Dieser Knoten ist beiden Trees gemeinsam, wird also übernommen
                            int j = commonChildIndices[k];

                            // Nicht abbrechen, die ShadowNode hängt schon im active-Tree und der Break würde sich nach oben fortpflanzen,
                            //                       shadowJobList.Children[j].Break(true);
                            shadowJobListVM.Children[j].Dispose(); // Ersatzkonstante freigeben.
                            shadowJobList.ReleaseChildAt(j); // Ersatzkonstante freigeben.

                            activeJobListVM.Children[i].Parent = shadowJobListVM;
                            activeJobListVM.Children[i].RootLogicalTaskTreeViewModel = shadowJobListVM.RootLogicalTaskTreeViewModel;

                            activeNode.Children[i].Mother = shadowJobList;
                            activeNode.Children[i].RootJobList = shadowJobList;
                            // activeNode.Children[i].TreeRootJobList = shadowJobList.TreeRootJobList; // ?

                            shadowJobList.SetChildAt(j, activeNode.Children[i]); // Referenziert den noch im activeTree laufenden
                                                                                 // Knoten und hängt sich in dessen Events ein.
                                                                                 //shadowJobList.Children.RemoveAt(j);
                                                                                 //shadowJobList.Children.Insert(j, activeNode.Children[i]); // Referenziert den noch im activeTree laufenden
                                                                                 //                                                          // Knoten
                                                                                 //shadowJobList.SetChildAt(j, activeNode.Children[i]);      // und hängt sich in dessen Events ein.

                            //shadowJobListVM.Children[j] = activeJobListVM.Children[i]; // Referenziert das noch im activeTree laufende ViewModel.
                            //shadowJobListVM.Children[j].SetBLNode(shadowJobList.Children[j], true);
                            shadowJobListVM.Children.RemoveAt(j);
                            shadowJobListVM.Children.Insert(j, activeJobListVM.Children[i]); // Referenziert das noch im activeTree laufende ViewModel.

                            // shadowJobListVM.Children[j].SetBLNode(shadowJobList.Children[j], true); // ?
                            shadowJobListVM.Children[j].SetBLNode(activeJobList.Children[i], true); // ?

                            shadowJobListVM.Children[j].Invalidate();

                            activeJobListVM.Children.RemoveAt(i); // Bei laufendem activeTree träten hier Exceptions in anderen Threads auf.
                            activeJobList.ReleaseChildAt(i);
                            activeJobList.Children.RemoveAt(i);

                            removedNodesCounter++;
                            i--;

                        }
                    }
                }

                try
                {
                    LogicalTaskTreeManager.AdjustBranchRootJobListGlobals(shadowNode);
                }
                catch (Exception)
                {
                    throw;
                }

                LogTaskTree(activeNodeVM, fromTop: true,
                    header: "--- V I S H N U - T R E E   2   -----------------------------------------------------------------------------------------------");
                Thread.Sleep(10);

                shadowParentVM.Children.RemoveAt(nodeIndex); // 18.04.2021 Erik Nagel
                shadowParent.Children.RemoveAt(nodeIndex); // 18.04.2021 Erik Nagel

                // Der früher aktive Knoten sollte jetzt abgebrochen und isoliert sein, also komplett freigeben.
                activeNodeVM.ReleaseBLNode();

                //                LogicalNode.ResumeTree();

                activeNode.Break(false);

                activeParent.Refresh();
                shadowNode.Refresh();
                activeParentVM.Invalidate();
            }
            finally
            {
                JobListViewModel? jobListViewModel = activeNodeVM.GetTopRootJobListViewModel() ?? throw new NullReferenceException($"Internal Error: TransferNode().GetTopRootJobListViewModel()");
                LogTaskTree(jobListViewModel, fromTop: true,
                    header: "--- V I S H N U - T R E E final -----------------------------------------------------------------------------------------------");
                Thread.Sleep(10);
                LogicalNode.ResumeTree();
            }
        }

        private static int GetParentNodeIndex(LogicalNode activeNode, NodeParent activeParent)
        {
            int nodeIndex = -1;
            for (int i = 0; i < activeParent.Children.Count; i++)
            {
                if (activeParent.Children[i].Equals(activeNode))
                {
                    nodeIndex = i;
                    break;
                }
            }
            if (nodeIndex < 0)
            {
                throw new ApplicationException(string.Format($"Parent-Index nicht gefunden, Knoten: {activeNode.Path}"));
            }
            return nodeIndex;
        }

        private static void TransferGlobals(JobListViewModel shadowNodeVM, JobListViewModel activeNodeVM, JobList shadowNode, JobList activeNode)
        {
            LogicalTaskTreeManager.AddNewJobListGlobals(shadowNode, activeNode);
            LogicalTaskTreeManager.RemoveOldJobListGlobals(shadowNode, activeNode);
        }

        private static Dictionary<int, int> FindEqualJobListChildrenAndSetDummyConstants(JobListViewModel shadowJobListVM, JobListViewModel activeJobListVM, JobList shadowJobList, JobList activeJobList)
        {
            Dictionary<int, int> commonChildIndices = new Dictionary<int, int>();  // Key=activeNode.Children, Value=shadowNode.Children

            // überflüssig, da die ViewModels jeweils eine Referenz auf ihre Business-Nodes halten:
            //     JobList tempNode = new JobList(shadowNode, shadowNode.GetTopRootJobList());
            //     tempVM.SetBLNode(tempNode, true);

            // Eventuelle gleichgebliebene direkte Nachkommen von logicalNode in shadowNode durch
            // Dummy-Konstanten mit gleichen LastNotNullLogical ersetzen (wegen späterem Run auf shadowNode).
            // Die gesamte shadowNode inklusive Children wird in einem späteren Schritt auf den aktiven Tree übertragen.
            // In einem noch späteren Schritt werden dann die Dummy-Konstanten wieder durch die Originalknoten ersetzt.
            for (int i = 0; i < shadowJobList.Children.Count; i++)
            {
                for (int j = 0; j < activeJobList.Children.Count; j++)
                {
                    // if (activeJobList.Children[j].Equals(shadowJobList.Children[i]))
                    if (LogicalTaskTreeManager.BranchesAreEqual(activeJobList.Children[j], shadowJobList.Children[i]))
                    {
                        commonChildIndices.Add(j, i); // Key=activeNode.Children, Value=shadowNode.Children
                        SingleNode constantDummyNode = new SingleNode("@BOOL." + activeJobList.Children[j].LastNotNullLogical.ToString(),
                            shadowJobList, shadowJobList.Children[i].RootJobList, shadowJobList.Children[i].TreeParams);

                        shadowJobListVM.Children[i].UnsetBLNode();
                        shadowJobList.FreeChildAt(i);
                        shadowJobList.SetChildAt(i, constantDummyNode);
                        shadowJobListVM.Children[i].SetBLNode(constantDummyNode, true);

                        break;
                    }
                }
            }
            return commonChildIndices;
        }

        private static bool BranchesAreEqual(LogicalNode logicalNode1, LogicalNode logicalNode2)
        {
            if (!logicalNode1.Equals(logicalNode2))
            {
                return false;
            }
            if (logicalNode1.Children.Count != logicalNode2.Children.Count)
            {
                return false;
            }
            // TODO: Bei untergeordneten JobLists kann hier eine frühere Rückkehr erfolgen,
            //       da diese später ihrerseits auch wieder geprüft werden.
            for (int i = 0; i < logicalNode1.Children.Count; i++)
            {
                if (!LogicalTaskTreeManager.BranchesAreEqual(logicalNode1.Children[i], logicalNode2.Children[i]))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Ändert bei aus dem ShadowTree in den aktiven Tree übernommenen Knoten Referenzen des activeTree auf den früheren Knoten
        /// unter gleichem Pfad auf den neuen Knoten. Muss auch für alle eventuellen Children des Knoten aufgerufen werden.
        /// Der Knoten muss vorher in den aktiven Tree übernommen worden sein, da die Verarbeitung im aktiven Tree erfolgen
        /// muss.
        /// </summary>
        /// <param name="transferredShadowNode">Die neu in den Tree übernommene LogicalNode.</param>
        private static void AdjustBranchRootJobListGlobals(LogicalNode transferredShadowNode)
        {
            if (transferredShadowNode.TreeParams.BusinessLogicRoot == null)
            {
                // nur beim Original(Active)-Tree ist BusinessLogicRoot gefüllt.
                // Übertragen werden darf aber nur von Shadow nach Active.
                transferredShadowNode.Traverse(AdjustRootJobListGlobals);
            }
        }

        /// <summary>
        /// Ändert bei aus dem ShadowTree in den aktiven Tree übernommenen Knoten Referenzen auf den früheren Knoten
        /// unter gleichem Pfad auf den neuen Knoten.
        /// </summary>
        /// <param name="depth">Hierarchie-Tiefe im Tree, wird hier nicht verwendet.</param>
        /// <param name="transferredShadowNode">Die neu in den Tree übernommene LogicalNode.</param>
        /// <param name="userObject">Ein optionales user-Objekt, wird hier nicht verwendet.</param>
        private static object? AdjustRootJobListGlobals(int depth, LogicalNode transferredShadowNode, object? userObject)
        {
            LogicalNode changingNode = transferredShadowNode;
            do
            {
                transferredShadowNode = transferredShadowNode.RootJobList;
                LogicalTaskTreeManager.ChangeOldReferences(changingNode, (JobList)transferredShadowNode);
            } while (transferredShadowNode.Mother != null);
            return null;
        }

        private static void ChangeOldReferences(LogicalNode changingNode, JobList treeJobList)
        {
            // Generell funktioniert foreach hier nicht, da u.U. die Auflistungen geändert werden.

            LogicalNodeViewModel shadowTreeVM = LogicalTaskTreeManager._shadowVMFinder[treeJobList.IdPath];
            JobList shadowJobList = (JobList)shadowTreeVM.GetLogicalNode();
            LogicalNodeViewModel activeTreeVM = LogicalTaskTreeManager._activeVMFinder[treeJobList.IdPath];
            JobList activeJobList = (JobList)activeTreeVM.GetLogicalNode();

            List<string> keys;
            try
            {
                keys = new List<string>(activeJobList.Job.EventTriggers.Keys);
                foreach (string key in keys)
                {
                    List<string> keyKeys = new List<string>(activeJobList.Job.EventTriggers[key].Keys);
                    foreach (string keyKey in keyKeys)
                    {
                        // if (changingNode.Id == keyKey)
                        TriggerShell? nodeTriggerShell = (TriggerShell?)changingNode.Trigger;
                        if (nodeTriggerShell != null && nodeTriggerShell.HasTreeEventTrigger
                            && nodeTriggerShell.GetTriggerParameters()?.ToString() == keyKey)
                        {
                            if (shadowJobList != null && shadowJobList.Job.EventTriggers.ContainsKey(key) == true
                                && shadowJobList.Job.EventTriggers[key].ContainsKey(keyKey))
                            {
                                TreeEvent? lastTreeEvent = activeJobList.Job.EventTriggers[key][keyKey].GetTreeEventTrigger()?.LastTreeEvent;
                                TriggerShell shadowTriggerShell = shadowJobList.Job.EventTriggers[key][keyKey];
                                // shadowTriggerShell.GetTreeEventTrigger().LastTreeEvent = lastTreeEvent;
                                TreeEvent? lastShadowTreeEvent = shadowTriggerShell.GetTreeEventTrigger()?.LastTreeEvent;
                                if (lastShadowTreeEvent != null)
                                {
                                    lastShadowTreeEvent = lastTreeEvent;
                                }
                                activeJobList.Job.EventTriggers[key][keyKey] = shadowTriggerShell;
                                if (lastTreeEvent != null)
                                {
                                    string? referencedNodeId = shadowTriggerShell.GetTreeEventTrigger()?.ReferencedNodeId;
                                    if (referencedNodeId != null)
                                    {
                                        LogicalNode? source = changingNode.FindNodeById(referencedNodeId) ?? throw new NullReferenceException($"Internal Error: {referencedNodeId}");
                                        changingNode.LastResult = source.LastResult;
                                        changingNode.LogicalState = source.LogicalState;
                                        changingNode.Logical = source.Logical;
                                        changingNode.OnNodeProgressFinished(changingNode.Name, 100, source.SingleNodesFinished);
                                        //activeParentVM.InitFromNode(shadowParentVM);
                                        // changingNode.State = source.State;
                                        //changingNode.LastNotNullLogical = source.LastNotNullLogical;
                                    }
                                }
                            }
                        }
                    }
                }

            }
            catch // (Exception ex1)
            {
                throw;
            }
            try
            {
                keys = new List<string>(activeJobList.Job.WorkersDictionary.Keys);
                foreach (string key in keys)
                {
                    List<string> keyKeys = new List<string>(activeJobList.Job.WorkersDictionary[key].Keys);
                    foreach (string keyKey in keyKeys)
                    {
                        if (changingNode.Id == keyKey)
                        {
                            if (shadowJobList != null && shadowJobList.Job.WorkersDictionary?.ContainsKey(key) == true
                                && shadowJobList.Job.WorkersDictionary[key].ContainsKey(keyKey))
                            {
                                activeJobList.Job.WorkersDictionary[key][keyKey] = shadowJobList.Job.WorkersDictionary[key][keyKey];
                            }
                        }
                    }
                }

            }
            catch // (Exception ex2)
            {
                throw;
            }
            try
            {
                keys = new List<string>(activeJobList.AllCheckersForUnreferencingNodeConnectors.Keys);
                foreach (string key in keys)
                {
                    if (changingNode.Name == key)
                    {
                        if (shadowJobList != null && shadowJobList.AllCheckersForUnreferencingNodeConnectors.ContainsKey(key))
                        {
                            activeJobList.AllCheckersForUnreferencingNodeConnectors[key] = shadowJobList.AllCheckersForUnreferencingNodeConnectors[key];
                        }
                    }
                }

            }
            catch // (Exception ex3)
            {
                throw;
            }
            try
            {
                keys = new List<string>(activeJobList.TreeExternalCheckers.Keys);
                foreach (string key in keys)
                {
                    if (changingNode.Id == key)
                    {
                        if (shadowJobList != null && shadowJobList.TreeExternalCheckers.ContainsKey(key))
                        {
                            activeJobList.TreeExternalCheckers[key] = shadowJobList.TreeExternalCheckers[key];
                        }
                    }
                }

            }
            catch // (Exception ex4)
            {
                throw;
            }
            /*
            foreach (SingleNode shadowNode in activeJobList.TreeExternalSingleNodes)
            {
                bool found = false;
                foreach (SingleNode treeNode in activeJobList.TreeExternalSingleNodes)
                {
                    if (treeNode.Equals(shadowNode))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    activeJobList.TreeExternalSingleNodes.Add(shadowNode);
                }
            }
            foreach (string key in activeJobList.TriggerRelevantEventCache)
            {
                if (!activeJobList.TriggerRelevantEventCache.Contains(key))
                {
                    activeJobList.TriggerRelevantEventCache.Add(key);
                }
            }
            foreach (string key in activeJobList.LoggerRelevantEventCache)
            {
                if (!activeJobList.LoggerRelevantEventCache.Contains(key))
                {
                    activeJobList.LoggerRelevantEventCache.Add(key);
                }
            }
            foreach (string key in activeJobList.WorkerRelevantEventCache)
            {
                if (!activeJobList.WorkerRelevantEventCache.Contains(key))
                {
                    activeJobList.WorkerRelevantEventCache.Add(key);
                }
            }
            */
            try
            {
                keys = new List<string>(activeJobList.JobsByName.Keys);
                foreach (string key in keys)
                {
                    if (changingNode.Id == key)
                    {
                        if (shadowJobList != null && shadowJobList.JobsByName.ContainsKey(key))
                        {
                            activeJobList.JobsByName[key] = shadowJobList.JobsByName[key];
                        }
                    }
                }

            }
            catch // (Exception ex5)
            {
                throw;
            }
            try
            {
                keys = new List<string>(activeJobList.NodesByName.Keys);
                foreach (string key in keys)
                {
                    if (changingNode.Id == key)
                    {
                        if (shadowJobList != null && shadowJobList.NodesByName.ContainsKey(key))
                        {
                            activeJobList.NodesByName[key] = shadowJobList.NodesByName[key];
                        }
                    }
                }

            }
            catch // (Exception ex6)
            {
                throw;
            }
            try
            {
                keys = new List<string>(activeJobList.TreeRootLastChanceNodesByName.Keys);
                foreach (string key in keys)
                {
                    if (changingNode.Id == key)
                    {
                        if (shadowJobList != null && shadowJobList.TreeRootLastChanceNodesByName.ContainsKey(key))
                        {
                            activeJobList.TreeRootLastChanceNodesByName[key] = shadowJobList.TreeRootLastChanceNodesByName[key];
                        }
                    }
                }

            }
            catch // (Exception ex7)
            {
                throw;
            }
            try
            {
                keys = new List<string>(activeJobList.NodesById.Keys);
                foreach (string key in keys)
                {
                    if (changingNode.Id == key)
                    {
                        if (shadowJobList != null && shadowJobList.NodesById.ContainsKey(key))
                        {
                            activeJobList.NodesById[key] = shadowJobList.NodesById[key];
                        }
                    }
                }

            }
            catch // (Exception ex8)
            {
                throw;
            }
        }

        private static void AddNewJobListGlobals(JobList shadowJobList, JobList activeJobList)
        {
            Job shadowJob = shadowJobList.Job;
            Job activeJob = activeJobList.Job;
            foreach (string key in shadowJobList.Job.EventTriggers.Keys)
            {
                if (!activeJobList.Job.EventTriggers.ContainsKey(key))
                {
                    activeJobList.Job.EventTriggers.Add(key, shadowJobList.Job.EventTriggers[key]);
                }
                else
                {
                    foreach (string keyKey in shadowJobList.Job.EventTriggers[key].Keys)
                    {
                        if (!activeJobList.Job.EventTriggers[key].ContainsKey(keyKey))
                        {
                            activeJobList.Job.EventTriggers[key].Add(keyKey, shadowJobList.Job.EventTriggers[key][keyKey]);
                        }
                    }
                }
            }
            foreach (string key in shadowJobList.Job.WorkersDictionary.Keys)
            {
                if (!activeJobList.Job.WorkersDictionary.ContainsKey(key))
                {
                    activeJobList.Job.WorkersDictionary.Add(key, shadowJobList.Job.WorkersDictionary[key]);
                }
                else
                {
                    foreach (string keyKey in shadowJobList.Job.WorkersDictionary[key].Keys)
                    {
                        if (!activeJobList.Job.WorkersDictionary[key].ContainsKey(keyKey))
                        {
                            activeJobList.Job.WorkersDictionary[key].Add(keyKey, shadowJobList.Job.WorkersDictionary[key][keyKey]);
                        }
                        else
                        {
                            List<WorkerShell> combinedTreeWorkers = activeJobList.Job.WorkersDictionary[key][keyKey].ToList();
                            foreach (WorkerShell keyKeyShadowWorker in shadowJobList.Job.WorkersDictionary[key][keyKey])
                            {
                                string? shadowSlave = keyKeyShadowWorker.SlavePathName;
                                bool found = false;
                                foreach (WorkerShell keyKeyTreeWorker in activeJobList.Job.WorkersDictionary[key][keyKey])
                                {
                                    string? treeSlave = keyKeyTreeWorker.SlavePathName;
                                    if (treeSlave == shadowSlave)
                                    {
                                        found = true;
                                        break;
                                    }
                                }
                                if (!found)
                                {
                                    combinedTreeWorkers.Add(keyKeyShadowWorker);
                                }
                            }
                            if (activeJobList.Job.WorkersDictionary[key][keyKey].Length < combinedTreeWorkers.Count)
                            {
                                activeJobList.Job.WorkersDictionary[key][keyKey] = combinedTreeWorkers.ToArray();
                            }
                        }
                    }
                }
            }
            foreach (string key in shadowJobList.AllCheckersForUnreferencingNodeConnectors.Keys)
            {
                if (!activeJobList.AllCheckersForUnreferencingNodeConnectors.ContainsKey(key))
                {
                    activeJobList.AllCheckersForUnreferencingNodeConnectors.Add(key, shadowJobList.AllCheckersForUnreferencingNodeConnectors[key]);
                }
            }
            foreach (string key in shadowJobList.TreeExternalCheckers.Keys)
            {
                if (!activeJobList.TreeExternalCheckers.ContainsKey(key))
                {
                    activeJobList.TreeExternalCheckers.Add(key, shadowJobList.AllCheckersForUnreferencingNodeConnectors[key]);
                }
            }
            foreach (SingleNode shadowNode in shadowJobList.TreeExternalSingleNodes)
            {
                bool found = false;
                foreach (SingleNode treeNode in activeJobList.TreeExternalSingleNodes)
                {
                    if (treeNode.Equals(shadowNode))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    activeJobList.TreeExternalSingleNodes.Add(shadowNode);
                }
            }
            foreach (string key in shadowJobList.TriggerRelevantEventCache)
            {
                if (!activeJobList.TriggerRelevantEventCache.Contains(key))
                {
                    activeJobList.TriggerRelevantEventCache.Add(key);
                }
            }
            foreach (string key in shadowJobList.LoggerRelevantEventCache)
            {
                if (!activeJobList.LoggerRelevantEventCache.Contains(key))
                {
                    activeJobList.LoggerRelevantEventCache.Add(key);
                }
            }
            foreach (string key in shadowJobList.WorkerRelevantEventCache)
            {
                if (!activeJobList.WorkerRelevantEventCache.Contains(key))
                {
                    activeJobList.WorkerRelevantEventCache.Add(key);
                }
            }
            foreach (string key in shadowJobList.JobsByName.Keys)
            {
                if (!activeJobList.JobsByName.ContainsKey(key))
                {
                    activeJobList.JobsByName.Add(key, shadowJobList.JobsByName[key]);
                }
            }
            foreach (string key in shadowJobList.NodesByName.Keys)
            {
                if (!activeJobList.NodesByName.ContainsKey(key))
                {
                    activeJobList.NodesByName.Add(key, shadowJobList.NodesByName[key]);
                }
            }
            foreach (string key in shadowJobList.TreeRootLastChanceNodesByName.Keys)
            {
                if (!activeJobList.TreeRootLastChanceNodesByName.ContainsKey(key))
                {
                    activeJobList.TreeRootLastChanceNodesByName.Add(key, shadowJobList.TreeRootLastChanceNodesByName[key]);
                }
            }
            foreach (string key in shadowJobList.NodesById.Keys)
            {
                if (!activeJobList.NodesById.ContainsKey(key))
                {
                    activeJobList.NodesById.Add(key, shadowJobList.NodesById[key]);
                }
            }
        }

        private static void RemoveOldJobListGlobals(JobList shadowJobList, JobList activeJobList)
        {

            List<string> keys;
            try
            {
                keys = new List<string>(activeJobList.Job.EventTriggers.Keys);
                foreach (string key in keys)
                {
                    if (!shadowJobList.Job.EventTriggers.ContainsKey(key) == true)
                    {
                        activeJobList.Job.EventTriggers.Remove(key);
                    }
                    else
                    {
                        List<string> keyKeys = new List<string>(activeJobList.Job.EventTriggers[key].Keys);
                        foreach (string keyKey in keyKeys)
                        {
                            if (!shadowJobList.Job.EventTriggers[key].ContainsKey(keyKey) == true)
                            {
                                activeJobList.Job.EventTriggers[key].Remove(keyKey);
                            }
                        }
                    }
                }

            }
            catch (Exception ex1)
            {
                InfoController.Say(string.Format($"#RELOAD# RemoveOldJobListGlobals Exception 1: {ex1.Message}."));
                throw;
            }
            try
            {
                keys = new List<string>(activeJobList.Job.WorkersDictionary.Keys);
                foreach (string key in keys)
                {
                    if (!shadowJobList.Job.WorkersDictionary.ContainsKey(key) == true)
                    {
                        activeJobList.Job.WorkersDictionary.Remove(key);
                    }
                    else
                    {
                        List<string> keyKeys = new List<string>(activeJobList.Job.WorkersDictionary[key].Keys);
                        foreach (string keyKey in keyKeys)
                        {
                            if (!shadowJobList.Job.WorkersDictionary[key].ContainsKey(keyKey) == true)
                            {
                                activeJobList.Job.WorkersDictionary[key].Remove(keyKey);
                            }
                            else
                            {
                                List<WorkerShell> combinedTreeWorkers = new List<WorkerShell>();
                                foreach (WorkerShell keyKeyTreeWorker in activeJobList.Job.WorkersDictionary[key][keyKey])
                                {
                                    string? treeSlave = keyKeyTreeWorker.SlavePathName;
                                    if (treeSlave != null)
                                    {
                                        bool found = false;
                                        foreach (WorkerShell keyKeyShadowWorker in shadowJobList.Job.WorkersDictionary[key][keyKey])
                                        {
                                            string? shadowSlave = keyKeyShadowWorker.SlavePathName;
                                            if (treeSlave == shadowSlave)
                                            {
                                                found = true;
                                                break;
                                            }
                                        }
                                        if (found)
                                        {
                                            combinedTreeWorkers.Add(keyKeyTreeWorker);
                                        }
                                    }
                                }
                                if (activeJobList.Job.WorkersDictionary[key][keyKey].Length > combinedTreeWorkers.Count)
                                {
                                    activeJobList.Job.WorkersDictionary[key][keyKey] = combinedTreeWorkers.ToArray();
                                }
                            }
                        }
                    }
                }

            }
            catch // (Exception ex2)
            {
                throw;
            }
            try
            {
                keys = new List<string>(activeJobList.AllCheckersForUnreferencingNodeConnectors.Keys);
                foreach (string key in keys)
                {
                    if (!shadowJobList.AllCheckersForUnreferencingNodeConnectors.ContainsKey(key))
                    {
                        activeJobList.AllCheckersForUnreferencingNodeConnectors.Remove(key);
                    }
                }
            }
            catch // (Exception ex3)
            {
                throw;
            }
            try
            {
                keys = new List<string>(activeJobList.TreeExternalCheckers.Keys);
                foreach (string key in keys)
                {
                    if (!shadowJobList.TreeExternalCheckers.ContainsKey(key))
                    {
                        activeJobList.TreeExternalCheckers.Remove(key);
                    }
                }
            }
            catch // (Exception ex4)
            {
                throw;
            }
            try
            {
                List<SingleNode> remainingSingleNodes = new List<SingleNode>();
                foreach (SingleNode treeNode in activeJobList.TreeExternalSingleNodes)
                {
                    bool found = false;
                    foreach (SingleNode shadowNode in shadowJobList.TreeExternalSingleNodes)
                    {
                        if (treeNode.Equals(shadowNode))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (found)
                    {
                        remainingSingleNodes.Add(treeNode);
                    }
                }
                if (activeJobList.TreeExternalSingleNodes.Count > remainingSingleNodes.Count)
                {
                    activeJobList.TreeExternalSingleNodes = remainingSingleNodes;
                }
            }
            catch // (Exception ex5)
            {
                throw;
            }
            try
            {
                keys = new List<string>(activeJobList.TriggerRelevantEventCache);
                foreach (string key in keys)
                {
                    if (!shadowJobList.TriggerRelevantEventCache.Contains(key))
                    {
                        activeJobList.TriggerRelevantEventCache.Remove(key);
                    }
                }
            }
            catch // (Exception ex6)
            {
                throw;
            }
            try
            {
                keys = new List<string>(activeJobList.LoggerRelevantEventCache);
                foreach (string key in keys)
                {
                    if (!shadowJobList.LoggerRelevantEventCache.Contains(key))
                    {
                        activeJobList.LoggerRelevantEventCache.Remove(key);
                    }
                }
            }
            catch // (Exception ex7)
            {
                throw;
            }
            try
            {
                keys = new List<string>(activeJobList.WorkerRelevantEventCache);
                foreach (string key in keys)
                {
                    if (!shadowJobList.WorkerRelevantEventCache.Contains(key))
                    {
                        activeJobList.WorkerRelevantEventCache.Remove(key);
                    }
                }
            }
            catch // (Exception ex8)
            {
                throw;
            }
            try
            {
                keys = new List<string>(activeJobList.JobsByName.Keys);
                foreach (string key in keys)
                {
                    if (!shadowJobList.JobsByName.ContainsKey(key))
                    {
                        activeJobList.JobsByName.Remove(key);
                    }
                }
            }
            catch // (Exception ex9)
            {
                throw;
            }
            try
            {
                keys = new List<string>(activeJobList.NodesByName.Keys);
                foreach (string key in keys)
                {
                    if (!shadowJobList.NodesByName.ContainsKey(key))
                    {
                        activeJobList.NodesByName.Remove(key);
                    }
                }
            }
            catch // (Exception ex10)
            {
                throw;
            }
            try
            {
                keys = new List<string>(activeJobList.TreeRootLastChanceNodesByName.Keys);
                foreach (string key in keys)
                {
                    if (!shadowJobList.TreeRootLastChanceNodesByName.ContainsKey(key))
                    {
                        activeJobList.TreeRootLastChanceNodesByName.Remove(key);
                    }
                }
            }
            catch // (Exception ex11)
            {
                throw;
            }
            try
            {
                keys = new List<string>(activeJobList.NodesById.Keys);
                foreach (string key in keys)
                {
                    if (!shadowJobList.NodesById.ContainsKey(key))
                    {
                        activeJobList.NodesById.Remove(key);
                    }
                }
            }
            catch // (Exception ex12)
            {
                throw;
            }
        }

        /// <summary>
        /// Fügt den Path der ExpandableNode als Key und die ExpandableNode als Value in ein als object übergebenes Dictionary ein.
        /// </summary>
        /// <param name="depth">Nullbasierter Zähler der Rekursionstiefe eines Knotens im LogicalTaskTree.</param>
        /// <param name="expandableNode">Basisklasse eines ViewModel-Knotens im LogicalTaskTree.</param>
        /// <param name="userObject">Ein beliebiges durchgeschliffenes UserObject (hier: Dictionary&lt;string, LogicalNodeViewModel&gt;).</param>
        /// <returns>Das bisher gefüllte Dictionary mit Path als Key und dem LogicalNodeViewModel als Value.</returns>
        internal static object? DoIndexTreeElement(int depth, IExpandableNode expandableNode, object? userObject)
        {
            Dictionary<string, LogicalNodeViewModel>? vmFinder = (Dictionary<string, LogicalNodeViewModel>?)userObject;
            if (vmFinder != null)
            {
                if (expandableNode is LogicalNodeViewModel)
                {
                    LogicalNodeViewModel logicalNodeViewModel = (LogicalNodeViewModel)expandableNode;
                    // string key = Regex.Replace(logicalNodeViewModel.Path, @"Internal_\d+", "Internal");
                    string key = logicalNodeViewModel.Path;
                    vmFinder.Add(key, logicalNodeViewModel);
                }
            }
            return userObject;
        }

        /// <summary>
        /// Fügt tatsächlich referenzierte Objekte in eine als object übergebene List ein.
        /// </summary>
        /// <param name="depth">Nullbasierter Zähler der Rekursionstiefe eines Knotens im LogicalTaskTree.</param>
        /// <param name="expandableNode">Basisklasse eines ViewModel-Knotens im LogicalTaskTree.</param>
        /// <param name="userObject">Ein beliebiges durchgeschliffenes UserObject (hier: Dictionary&lt;string, LogicalNodeViewModel&gt;).</param>
        /// <returns>Das bisher gefüllte Dictionary mit Path als Key und dem LogicalNodeViewModel als Value.</returns>
        private static object? CollectReferencedObjects(int depth, IExpandableNode expandableNode, object? userObject)
        {
            List<object>? allReferencedObjects = (List<object>?)userObject ?? new();
            if (expandableNode is LogicalNodeViewModel)
            {
                LogicalNodeViewModel logicalNodeViewModel = (LogicalNodeViewModel)expandableNode;
                LogicalNode logicalNode = logicalNodeViewModel.GetLogicalNode();

                // --- Trigger ---------------------------------------------------------------------------------
                if (logicalNode.Trigger != null)
                {
                    if (!allReferencedObjects.Contains(logicalNode.Trigger))
                    {
                        allReferencedObjects.Add(logicalNode.Trigger);
                    }
                }
                SingleNode? singleNode = null;
                if (logicalNode is SingleNode)
                {
                    singleNode = (SingleNode?)logicalNode;
                }
                if (singleNode?.Checker != null && singleNode.Checker.CheckerTrigger != null)
                {
                    if (!allReferencedObjects.Contains(singleNode.Checker.CheckerTrigger))
                    {
                        allReferencedObjects.Add(singleNode.Checker.CheckerTrigger);
                    }
                }
                Job? job = null;
                if (logicalNode is JobList)
                {
                    job = ((JobList?)logicalNode)?.Job;
                }
                if (job != null)
                {
                    if (job.JobTrigger != null)
                    {
                        if (!allReferencedObjects.Contains(job.JobTrigger))
                        {
                            allReferencedObjects.Add(job.JobTrigger);
                        }
                    }
                    if (job.JobSnapshotTrigger != null)
                    {
                        if (!allReferencedObjects.Contains(job.JobSnapshotTrigger))
                        {
                            allReferencedObjects.Add(job.JobSnapshotTrigger);
                        }
                    }
                    // --- Worker-Trigger -------------------------------------------------------------------------
                    if (job.Workers != null)
                    {
                        Workers workers = job.Workers;
                        List<string> keys = new List<string>(workers.Keys);
                        Dictionary<string, Dictionary<string, WorkerShell[]>>.ValueCollection values = workers.Values;
                        foreach (Dictionary<string, WorkerShell[]> element in values)
                        {
                            Dictionary<string, WorkerShell[]>.ValueCollection valuesValues = element.Values;
                            foreach (WorkerShell[] workerShellArray in valuesValues)
                            {
                                for (int i = 0; i < workerShellArray.Length; i++)
                                {
                                    INodeTrigger? nodeTrigger = workerShellArray[i].Trigger;
                                    if (nodeTrigger != null)
                                    {
                                        if (!allReferencedObjects.Contains(nodeTrigger))
                                        {
                                            allReferencedObjects.Add(nodeTrigger);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                // --- Logger ---------------------------------------------------------------------------------
                if (logicalNode?.Logger != null)
                {
                    if (!allReferencedObjects.Contains(logicalNode.Logger))
                    {
                        allReferencedObjects.Add(logicalNode.Logger);
                    }
                }
                LoggerShell? logger = null;
                if (singleNode?.Checker != null)
                {
                    logger = singleNode.Checker.CheckerLogger;
                    if (logger != null)
                    {
                        if (!allReferencedObjects.Contains(logger))
                        {
                            allReferencedObjects.Add(logger);
                        }
                    }
                }
                if (job?.JobLogger != null)
                {
                    if (!allReferencedObjects.Contains(job.JobLogger))
                    {
                        allReferencedObjects.Add(job.JobLogger);
                    }
                }
            }
            return userObject;
        }

        /// <summary>
        /// Fügt Informationen über die übergebene ExpandableNode in eine als object übergebene Stringlist ein.
        /// </summary>
        /// <param name="depth">Nullbasierter Zähler der Rekursionstiefe eines Knotens im LogicalTaskTree.</param>
        /// <param name="expandableNode">Basisklasse eines ViewModel-Knotens im LogicalTaskTree.</param>
        /// <param name="userObject">Ein beliebiges durchgeschliffenes UserObject (hier: List&lt;string&gt;).</param>
        /// <returns>Die bisher gefüllte Stringlist mit Knoteninformationen.</returns>
        private static object? ListTreeElement(int depth, IExpandableNode expandableNode, object? userObject)
        {
            string depthString = depth > 0 ? new string(' ', depth * 4) : "";
            List<string> allTreeInfos = (List<string>?)userObject ?? new List<string>();
            LogicalNodeViewModel logicalNodeViewModel = (LogicalNodeViewModel)expandableNode;
            string viewModelToStringReducedSpaces = Regex.Replace(logicalNodeViewModel.ToString().Replace(Environment.NewLine, ", "), @"\s{2,}", " ");
            viewModelToStringReducedSpaces += logicalNodeViewModel.VisualTreeCacheBreaker;
            if (logicalNodeViewModel.Parent != null)
            {
                viewModelToStringReducedSpaces += Environment.NewLine + depthString
                    + "                          Parent: " + logicalNodeViewModel.Parent.TreeParams.Name + ": " + logicalNodeViewModel.Parent.Id;
            }
            else
            {
                viewModelToStringReducedSpaces += Environment.NewLine + depthString
                    + "                          Parent: NULL";
            }
            viewModelToStringReducedSpaces += GetViewModelChildrenString(logicalNodeViewModel);
            viewModelToStringReducedSpaces += Environment.NewLine + depthString
                    + "                          hooked to: " + logicalNodeViewModel.HookedTo;
            allTreeInfos.Add(depthString + logicalNodeViewModel.TreeParams.ToString() + ": " + viewModelToStringReducedSpaces);

            if (expandableNode is JobListViewModel)
            {
                LogicalTaskTreeManager.AddJobListGlobals(depthString + "    ", (JobListViewModel)expandableNode, allTreeInfos);
            }

            LogicalTaskTreeManager.AddBusinessNodeDetails(depthString + "    ", (LogicalNodeViewModel)expandableNode, allTreeInfos);

            return userObject;
        }

        private static string GetViewModelChildrenString(LogicalNodeViewModel parent)
        {
            string children = "";
            if (!(parent is SingleNodeViewModel))
            {
                children = " Children: ";
                string delimiter = "";
                foreach (LogicalNodeViewModel child in parent.Children)
                {
                    children += delimiter + child.TreeParams.Name + ": " + child.Id;
                    delimiter = ", ";
                }
            }
            return children;
        }

        private static void AddBusinessNodeDetails(string depthString, LogicalNodeViewModel logicalNodeViewModel, List<string> allTreeInfos)
        {
            LogicalNode blNode = logicalNodeViewModel.GetLogicalNode();
            if (blNode != null)
            {
                string motherInfo;
                if (blNode.Mother != null)
                {
                    motherInfo = "Mother: " + ((LogicalNode)blNode.Mother).TreeParams.Name + ": " + ((LogicalNode)blNode.Mother).IdInfo;
                }
                else
                {
                    motherInfo = "Mother: NULL";
                }
                string childrenInfo = "";
                string hookInfo = "";
                if (blNode is NodeParent)
                {
                    childrenInfo = GetNodeChildrenString((NodeParent)blNode);
                    hookInfo = Environment.NewLine + depthString
                        + "                          hooked to: " + ((NodeParent)blNode).HookedTo;
                }
                string blNodeToString = depthString + blNode.TreeParams.ToString() + ": "
                    + blNode.ToString().Replace(Environment.NewLine, Environment.NewLine
                    + depthString).TrimEnd().TrimEnd(Environment.NewLine.ToCharArray())
                    + hookInfo;
                allTreeInfos.Add(blNodeToString + ", " + motherInfo + childrenInfo + Environment.NewLine);
            }
            else
            {
                allTreeInfos.Add(depthString + "NOTHING" + Environment.NewLine);
            }
        }

        private static string GetNodeChildrenString(NodeParent parent)
        {
            string children = " Children: ";
            string delimiter = "";
            foreach (LogicalNode child in parent.Children)
            {
                if (child != null)
                {
                    children += delimiter + child.TreeParams.Name + ": " + child.IdInfo;
                    delimiter = ", ";
                }
            }
            return children;
        }

        private static void AddJobListGlobals(string depthString, JobListViewModel rootJobListVM, List<string> allTreeInfos)
        {
            StringBuilder stringBuilder;
            JobList rootJobList = (JobList)rootJobListVM.GetLogicalNode();
            if (rootJobList != null)
            {
                stringBuilder = new StringBuilder(depthString + string.Format($"--- Globals von {rootJobList.NameId} ---"));
                List<string> keys;
                stringBuilder.Append(Environment.NewLine + depthString + "EventTriggers" + Environment.NewLine);
                keys = new List<string>(rootJobList.Job.EventTriggers.Keys);
                string delimiter = depthString + "    ";
                foreach (string key in keys)
                {
                    stringBuilder.Append(delimiter + key + ": ");
                    delimiter = " | ";
                    List<string> keyKeys = new List<string>(rootJobList.Job.EventTriggers[key].Keys);
                    string delimiter2 = "";
                    foreach (string keyKey in keyKeys)
                    {
                        stringBuilder.Append(delimiter2 + keyKey);
                        delimiter2 = ", ";
                    }
                }

                stringBuilder.Append(Environment.NewLine + depthString + "Workers" + Environment.NewLine);
                keys = new List<string>(rootJobList.Job.WorkersDictionary.Keys);
                delimiter = depthString + "    ";
                foreach (string key in keys)
                {
                    // Event
                    stringBuilder.Append(delimiter + key + ": ");
                    delimiter = " | ";
                    List<string> keyKeys = new List<string>(rootJobList.Job.WorkersDictionary[key].Keys);
                    string delimiter2 = "";
                    foreach (string keyKey in keyKeys)
                    {
                        stringBuilder.Append(delimiter2 + keyKey);
                        delimiter2 = ", ";
                        // Knoten
                        List<WorkerShell> combinedTreeWorkers = new List<WorkerShell>();
                        string delimiter3 = ": ";
                        foreach (WorkerShell keyKeyTreeWorker in rootJobList.Job.WorkersDictionary[key][keyKey])
                        {
                            // Exe
                            stringBuilder.Append(delimiter3 + System.IO.Path.GetFileName(keyKeyTreeWorker.SlavePathName));
                            delimiter3 = ", ";
                        }
                    }
                }

                stringBuilder.Append(Environment.NewLine + depthString + "AllCheckers" + Environment.NewLine);
                keys = new List<string>(rootJobList.AllCheckersForUnreferencingNodeConnectors.Keys);
                delimiter = depthString + "    ";
                foreach (string key in keys)
                {
                    stringBuilder.Append(delimiter + key);
                    delimiter = ", ";
                }

                stringBuilder.Append(Environment.NewLine + depthString + "TreeExternalCheckers" + Environment.NewLine);
                keys = new List<string>(rootJobList.TreeExternalCheckers.Keys);
                delimiter = depthString + "    ";
                foreach (string key in keys)
                {
                    stringBuilder.Append(delimiter + key);
                    delimiter = ", ";
                }

                stringBuilder.Append(Environment.NewLine + depthString + "TreeExternalSingleNodes" + Environment.NewLine);
                delimiter = depthString + "    ";
                foreach (SingleNode treeNode in rootJobList.TreeExternalSingleNodes)
                {
                    stringBuilder.Append(delimiter + treeNode.NameId);
                    delimiter = ", ";
                }

                stringBuilder.Append(Environment.NewLine + depthString + "TriggerRelevantEventCache" + Environment.NewLine);
                keys = new List<string>(rootJobList.TriggerRelevantEventCache);
                delimiter = depthString + "    ";
                foreach (string key in keys)
                {
                    stringBuilder.Append(delimiter + key);
                    delimiter = ", ";
                }

                stringBuilder.Append(Environment.NewLine + depthString + "LoggerRelevantEventCache" + Environment.NewLine);
                keys = new List<string>(rootJobList.LoggerRelevantEventCache);
                delimiter = depthString + "    ";
                foreach (string key in keys)
                {
                    stringBuilder.Append(delimiter + key);
                    delimiter = ", ";
                }

                stringBuilder.Append(Environment.NewLine + depthString + "WorkerRelevantEventCache" + Environment.NewLine);
                keys = new List<string>(rootJobList.WorkerRelevantEventCache);
                delimiter = depthString + "    ";
                foreach (string key in keys)
                {
                    stringBuilder.Append(delimiter + key);
                    delimiter = ", ";
                }

                stringBuilder.Append(Environment.NewLine + depthString + "JobsByName" + Environment.NewLine);
                keys = new List<string>(rootJobList.JobsByName.Keys);
                delimiter = depthString + "    ";
                foreach (string key in keys)
                {
                    stringBuilder.Append(delimiter + key);
                    delimiter = ", ";
                }

                stringBuilder.Append(Environment.NewLine + depthString + "NodesByName" + Environment.NewLine);
                keys = new List<string>(rootJobList.NodesByName.Keys);
                delimiter = depthString + "    ";
                foreach (string key in keys)
                {
                    stringBuilder.Append(delimiter + key);
                    delimiter = ", ";
                }

                stringBuilder.Append(Environment.NewLine + depthString + "TreeRootLastChanceNodesByName" + Environment.NewLine);
                keys = new List<string>(rootJobList.TreeRootLastChanceNodesByName.Keys);
                delimiter = depthString + "    ";
                foreach (string key in keys)
                {
                    stringBuilder.Append(delimiter + key);
                    delimiter = ", ";
                }

                stringBuilder.Append(Environment.NewLine + depthString + "NodesById" + Environment.NewLine);
                keys = new List<string>(rootJobList.NodesById.Keys);
                delimiter = depthString + "    ";
                foreach (string key in keys)
                {
                    stringBuilder.Append(delimiter + key);
                    delimiter = ", ";
                }

                stringBuilder.AppendLine(Environment.NewLine + depthString + $"----------------" + new string('-', rootJobList.NameId.Length) + "----");
            }
            else
            {
                stringBuilder = new StringBuilder(depthString + string.Format($"--- Globals von {rootJobListVM.Name} ---"));
                allTreeInfos.Add(depthString + "NOTHING" + Environment.NewLine);
                stringBuilder.AppendLine(Environment.NewLine + depthString + $"----------------" + new string('-', rootJobListVM.Name.Length) + "----");
            }
            allTreeInfos.Add(stringBuilder.ToString());
        }
    }
}
