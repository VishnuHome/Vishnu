using System.Windows;
using LogicalTaskTree;
using NetEti.MVVMini;
using System.Windows.Input;
using Vishnu.Interchange;
using System;
using NetEti.ApplicationControl;
using System.Text;

namespace Vishnu.ViewModel
{
    /// <summary>
    /// ViewModel für einen SingleNode-Knoten in der TreeView in LogicalTaskTreeControl
    /// Auch wenn hier keine eigene Funktionalität codiert ist, wird diese
    /// Ableitung von LogicalNodeViewModel benötigt, um in der TreeView die
    /// unterschiedliche Darstellung der verschiedenen Knotentypen realisieren
    /// zu können.
    /// </summary>
    /// <remarks>
    /// File: SingleNodeViewModel.cs
    /// Autor: Erik Nagel
    ///
    /// 05.12.2013 Erik Nagel: erstellt
    /// </remarks>
    public class SingleNodeViewModel : LogicalNodeViewModel
    {
        #region public members

        #region published members

        /// <summary>
        /// Veröffentlicht einen ButtonText entsprechend this._myLogicalNode.CanTreeStart.
        /// </summary>
        public string ButtonRunText
        {
            get
            {
                if (this._myLogicalNode.CanTreeStart)
                {
                    return "Run";
                }
                else
                {
                    return "Rerun";
                }
            }
            set
            {
                this.RaisePropertyChanged("ButtonRunText");
            }
        }

        /// <summary>
        /// Veröffentlicht einen ButtonText entsprechend this._myLogicalNode.CanTreeStart.
        /// </summary>
        public string ButtonRunBreakText
        {
            get
            {
                if (this._myLogicalNode.CanTreeStart)
                {
                    return "Run";
                }
                else
                {
                    return "Break";
                }
            }
            set
            {
                this.RaisePropertyChanged("ButtonRunBreakText");
            }
        }

        /// <summary>
        /// Veröffentlicht einen ButtonText entsprechend this._myLogicalNode.IsActive.
        /// </summary>
        public string ButtonBreakText
        {
            get
            {
                return "Break";
            }
            set
            {
                this.RaisePropertyChanged("ButtonBreakText");
            }
        }

        /// <summary>
        /// Command für den Run-Button im LogicalTaskTreeControl.
        /// </summary>
        public ICommand RunLogicalTaskTree { get { return this._btnRunTaskTreeRelayCommand; } }

        /// <summary>
        /// Command für den Run-or-Break-Button im LogicalTaskTreeControl.
        /// </summary>
        public ICommand RunOrBreakLogicalTaskTree { get { return this._btnRunOrBreakTaskTreeRelayCommand; } }

        /// <summary>
        /// Command für den Break-Button im LogicalTaskTreeControl.
        /// </summary>
        public ICommand BreakLogicalTaskTree { get { return this._btnBreakTaskTreeRelayCommand; } }

        #endregion published members

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="logicalTaskTreeViewModel">ViewModel des übergeordneten LogicalTaskTree.</param>
        /// <param name="parent">Der übergeordnete ViewModel-Knoten.</param>
        /// <param name="singleNode">Der zugeordnete Knoten aus dem LogicalTaskTree.</param>
        /// <param name="uIMain">Das Root-FrameworkElement zu diesem ViewModel.</param>
        public SingleNodeViewModel(OrientedTreeViewModelBase logicalTaskTreeViewModel, LogicalNodeViewModel parent, LogicalNode singleNode, FrameworkElement uIMain)
          : base(logicalTaskTreeViewModel, parent, singleNode, false, uIMain)
        {
            this._btnRunTaskTreeRelayCommand = new RelayCommand(runTaskTreeExecute, canRunTaskTreeExecute);
            this._btnRunOrBreakTaskTreeRelayCommand = new RelayCommand(runOrBreakTaskTreeExecute, canRunOrBreakTaskTreeExecute);
            this._btnBreakTaskTreeRelayCommand = new RelayCommand(breakTaskTreeExecute, canBreakTaskTreeExecute);
            //this._myLogicalNode.NodeStateChanged -= this.treeElementStateChanged;
            //this._myLogicalNode.NodeStateChanged += this.treeElementStateChanged;
            //this._myLogicalNode.NodeLogicalStateChanged -= this.TreeElementLogicalStateChanged;
            //this._myLogicalNode.NodeLogicalStateChanged += this.TreeElementLogicalStateChanged;
        }

        /// <summary>
        /// Überschriebene ToString()-Methode.
        /// </summary>
        /// <returns>Verkettete Properties als String.</returns>
        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder(base.ToString());
            string slavePathName = "";
            string referencedNodeName = "";
            string checkerParameters = "";
            LogicalNode logicalNode = this.GetLogicalNode();
            if (logicalNode != null)
            {
                if ((logicalNode is SingleNode))
                {
                    if ((logicalNode as SingleNode).Checker != null)
                    {
                        if ((logicalNode as SingleNode).Checker is CheckerShell)
                        {
                            slavePathName = ((logicalNode as SingleNode).Checker as CheckerShell).SlavePathName ?? "";
                            checkerParameters = ((logicalNode as SingleNode).Checker as CheckerShell).CheckerParameters ?? "";
                            referencedNodeName = ((logicalNode as SingleNode).Checker as CheckerShell).ReferencedNodeName ?? "";
                        }
                        else
                        {
                            slavePathName = ((logicalNode as SingleNode).Checker as ValueModifier<object>).SlavePathName ?? "";
                            referencedNodeName = ((logicalNode as SingleNode).Checker as ValueModifier<object>).ReferencedNodeName ?? "";
                        }
                    }
                }
                else
                {
                    if ((logicalNode is NodeConnector))
                    {
                        if ((logicalNode as NodeConnector).Checker != null)
                        {
                            if ((logicalNode as NodeConnector).Checker is CheckerShell)
                            {
                                slavePathName = ((logicalNode as NodeConnector).Checker as CheckerShell).SlavePathName ?? "";
                                checkerParameters = ((logicalNode as NodeConnector).Checker as CheckerShell).CheckerParameters ?? "";
                                referencedNodeName = ((logicalNode as NodeConnector).Checker as CheckerShell).ReferencedNodeName ?? "";
                            }
                            else
                            {
                                slavePathName = ((logicalNode as NodeConnector).Checker as ValueModifier<object>).SlavePathName ?? "";
                                referencedNodeName = ((logicalNode as NodeConnector).Checker as ValueModifier<object>).ReferencedNodeName ?? "";
                            }
                        }
                    }
                }
            }
            if (!String.IsNullOrEmpty(slavePathName))
            {
                stringBuilder.AppendLine(String.Format($"    SlavePathName: {slavePathName}"));
            }
            if (!String.IsNullOrEmpty(referencedNodeName))
            {
                stringBuilder.AppendLine(String.Format($"    ReferencedNodeName: {referencedNodeName}"));
            }
            if (!String.IsNullOrEmpty(checkerParameters))
            {
                // stringBuilder.AppendLine(String.Format($"    CheckerParameters: {checkerParameters}"));
                stringBuilder.AppendLine("    CheckerParameters: " + checkerParameters);
            }
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Vergleicht den Inhalt dieses SingleNodeViewModels nach logischen Gesichtspunkten
        /// mit dem Inhalt eines übergebenen SingleNodeViewModels.
        /// </summary>
        /// <param name="obj">Das SingleNodeViewModel zum Vergleich.</param>
        /// <returns>True, wenn das übergebene SingleNodeViewModel inhaltlich gleich diesem ist.</returns>
        public override bool Equals(object obj)
        {
            if (!base.Equals(obj))
            {
                return false;
            }
            if (Object.ReferenceEquals(this, obj))
            {
                return true;
            }
            return this.ToString() == (obj as SingleNodeViewModel).ToString();
        }

        /// <summary>
        /// Erzeugt einen Hashcode für dieses SingleNodeViewModel.
        /// </summary>
        /// <returns>Integer mit Hashwert.</returns>
        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        #endregion public members

        #region internal members

        /// <summary>
        /// Führt im Verlauf eines externen Tree-Relod einen technischen Run
        /// für die korrekte Initialisierung des JobListViewModels aus.
        /// </summary>
        internal void TechnicalRun()
        {
            this.runTaskTreeExecute(null);
        }

        #endregion internal members

        #region protected members

        /// <summary>
        /// Wird angesprungen, wenn sich der logische Zustand des zugeordneten
        /// Knotens aus der Business-Logic geändert hat.
        /// </summary>
        /// <param name="sender">Der zugeordnete Knoten aus der Business-Logic.</param>
        /// <param name="state">Logischer Zustand des Knotens aus der Business-Logic.</param>
        protected virtual void TreeElementLogicalStateChanged(object sender, NodeLogicalState state)
        {
            //NodeState tmpState = (sender as LogicalNode).LastState;
            //NodeState lastState = tmpState;
            //this.RaisePropertyChanged("Result"); // kann sich evtl. geändert haben. // Constructor + Finished
            //this.RaisePropertyChanged("Results"); // kann sich evtl. geändert haben. // Constructor + Finished
            //this.RaisePropertyChanged("ShortResult"); // kann sich evtl. geändert haben. // Constructor + Finished
            //this.RaisePropertyChanged("LastRun"); // Constructor + Finished
            //this.RaisePropertyChanged("LastRunInfo"); // Constructor + Finished
            //this.RaisePropertyChanged("NextRun"); // Constructor + Finished
            //this.RaisePropertyChanged("NextRunInfo"); // Constructor + Finished
            //this.RaisePropertyChanged("NextRunInfoAndResult"); // Constructor + Finished
        }

        #endregion protected members

        #region private members

        private RelayCommand _btnRunTaskTreeRelayCommand;
        private RelayCommand _btnRunOrBreakTaskTreeRelayCommand;
        private RelayCommand _btnBreakTaskTreeRelayCommand;

        private void runTaskTreeExecute(object parameter)
        {
            this._myLogicalNode.ProcessTreeEvent("UserRun", null);
            if (!(this._myLogicalNode is NodeConnector))
            {
                this._myLogicalNode.UserRun();
            }
            else
            {
                (this._myLogicalNode as NodeConnector).UserRun();
            }
        }

        private bool canRunTaskTreeExecute()
        {
            //bool canStart = this._myLogicalNode.CanTreeStart;
            return true;
        }

        private void runOrBreakTaskTreeExecute(object parameter)
        {
            if (this._myLogicalNode.CanTreeStart)
            {
                this._myLogicalNode.ProcessTreeEvent("UserRun", null);
                if (!(this._myLogicalNode is NodeConnector))
                {
                    this._myLogicalNode.UserRun();
                }
                else
                {
                    (this._myLogicalNode as NodeConnector).UserRun();
                }
            }
            else
            {
                this._myLogicalNode.ProcessTreeEvent("UserBreak", null);
                if (!(this._myLogicalNode is NodeConnector))
                {
                    this._myLogicalNode.UserBreak();
                }
                else
                {
                    (this._myLogicalNode as NodeConnector).UserBreak();
                }
            }
        }

        private bool canRunOrBreakTaskTreeExecute()
        {
            //bool canStart = this._myLogicalNode.CanTreeStart;
            return true;
        }

        private void breakTaskTreeExecute(object parameter)
        {
            this._myLogicalNode.ProcessTreeEvent("UserBreak", null);
            if (!(this._myLogicalNode is NodeConnector))
            {
                this._myLogicalNode.UserBreak();
            }
            else
            {
                (this._myLogicalNode as NodeConnector).UserBreak();
            }
        }

        private bool canBreakTaskTreeExecute()
        {
            bool canStart = this._myLogicalNode.CanTreeStart;
            //return !canStart;
            return true;
        }

        private void treeElementStateChanged(object sender, NodeState state)
        {
            // Buttons müssen zum Update gezwungen werden, da die Verarbeitung in einem
            // anderen Thread läuft:
            this.ButtonRunText = ""; // stößt dort RaisePropertyChanged an, der value wird verworfen.
            this.ButtonRunBreakText = ""; // stößt dort RaisePropertyChanged an, der value wird verworfen.
            this.ButtonBreakText = ""; // stößt dort RaisePropertyChanged an, der value wird verworfen.
            this._btnRunTaskTreeRelayCommand.UpdateCanExecuteState(this.Dispatcher);
            this._btnRunOrBreakTaskTreeRelayCommand.UpdateCanExecuteState(this.Dispatcher);
            this._btnBreakTaskTreeRelayCommand.UpdateCanExecuteState(this.Dispatcher);
        }

        /*
        // Wird angesprungen, wenn sich irgendwo in diesem Teilbaum ein Verarbeitungsfortschritt geändert hat.
        private void subNodeProgressChanged(object sender, CommonProgressChangedEventArgs args)
        {
          // an die UI weiterreichen
          //this.RaisePropertyChanged("LastExecutingTreeEvent");
          //this.SingleNodesFinished = (int)args.ProgressPercentage;
          this.RaisePropertyChanged("SingleNodesFinished");
        }

        // Wird angesprungen, wenn sich irgendwo in diesem Teilbaum eine SingleNode beendet hat.
        private void subNodeProgressFinished(object sender, CommonProgressChangedEventArgs args)
        {
          // an die UI weiterreichen
          //this.SingleNodesFinished = (int)args.ProgressPercentage;
          this.RaisePropertyChanged("SingleNodesFinished");
          //this.RaisePropertyChanged("LastExecutingTreeEvent");
        }
        */

        #endregion private members

    }
}
