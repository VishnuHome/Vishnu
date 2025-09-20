﻿using System.Windows;
using LogicalTaskTree;
using System.Windows.Input;
using NetEti.MVVMini;
using Vishnu.Interchange;
using System.Text;
using System;
using System.Text.RegularExpressions;

namespace Vishnu.ViewModel
{
    /// <summary>
    /// ViewModel für einen JobList-Knoten in der TreeView in LogicalTaskTreeControl
    /// Auch wenn hier keine eigene Funktionalität codiert ist, wird diese
    /// Ableitung von LogicalNodeViewModel benötigt, um in der TreeView die
    /// unterschiedliche Darstellung der verschiedenen Knotentypen realisieren
    /// zu können.
    /// </summary>
    /// <remarks>
    /// File: JobListViewModel.cs
    /// Autor: Erik Nagel
    ///
    /// 05.01.2013 Erik Nagel: erstellt
    /// </remarks>
    public class JobListViewModel : NodeListViewModel // LogicalNodeViewModel
    {
        #region public members

        #region published members

        /// <summary>
        /// Das Format, in dem dieser Job gespeichert wurde (Json oder Xml).
        /// </summary>
        public JobDescriptionFormat? Format
        {
            get
            {
                // return ((JobList)this._myLogicalNode.RootJobList).Format;
                return ((JobList)this._myLogicalNode).Format;
            }
        }

        /// <summary>
        /// Bei true befindet sich der Job in aktivem, gleich gestartetem Zustand.
        /// </summary>
        public bool IsActive
        {
            get
            {
                return ((JobList)this._myLogicalNode).IsActive;
            }
        }

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
        }

        /// <summary>
        /// Name + (Id + gegebenenfalls ReferencedNodeId) der ursprünglich referenzierten SingleNode.
        /// </summary>
        public override string DebugNodeInfos
        {
            get
            {
                string debugNodeInfos = base.DebugNodeInfos
                + ", " + this.Format;
                return debugNodeInfos;
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
        /// <param name="parent">Der übergeordnete ViewModel-Knoten.</param>
        /// <param name="jobList">Der zugeordnete Knoten aus dem LogicalTaskTree.</param>
        /// <param name="lazyLoadChildren">Bei True werden die Kinder erst beim Öffnen des TreeView-Knotens nachgeladen.</param>
        /// <param name="uIMain">Das Root-FrameworkElement zu diesem ViewModel.</param>
        /// <param name="logicalTaskTreeViewModel">Das dem Root-Knoten übergeordnete ViewModel (nur beim Root-Job ungleich null).</param>
        public JobListViewModel(OrientedTreeViewModelBase logicalTaskTreeViewModel, LogicalNodeViewModel? parent,
            JobList jobList, bool lazyLoadChildren, FrameworkElement uIMain)
          : base(logicalTaskTreeViewModel, parent, jobList, lazyLoadChildren, uIMain)
        {
            this._btnRunTaskTreeRelayCommand = new RelayCommand(runTaskTreeExecute, canRunTaskTreeExecute);
            this._btnRunOrBreakTaskTreeRelayCommand = new RelayCommand(runOrBreakTaskTreeExecute, canRunOrBreakTaskTreeExecute);
            this._btnBreakTaskTreeRelayCommand = new RelayCommand(breakTaskTreeExecute, canBreakTaskTreeExecute);
            this._myLogicalNode.NodeStateChanged -= this.treeElementStateChanged;
            this._myLogicalNode.NodeStateChanged += this.treeElementStateChanged;
        }

        /// <summary>
        /// Überschriebene ToString()-Methode.
        /// </summary>
        /// <returns>Verkettete Properties als String.</returns>
        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder(base.ToString());
            string logicalExpression = "";
            LogicalNode logicalNode = this.GetLogicalNode();
            if (logicalNode != null)
            {
                logicalExpression = ((JobList)logicalNode).LogicalExpression;
            }
            string logicalExpressionReducedSpaces = Regex.Replace(Regex.Replace(logicalExpression, "\\r?\\n", " "), @"\s{2,}", " ");
            stringBuilder.AppendLine(String.Format($"    LogicalExpression: {logicalExpressionReducedSpaces}"));
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Vergleicht den Inhalt dieses JobListViewModels nach logischen Gesichtspunkten
        /// mit dem Inhalt eines übergebenen JobListViewModels.
        /// </summary>
        /// <param name="obj">Das JobListViewModel zum Vergleich.</param>
        /// <returns>True, wenn das übergebene JobListViewModel inhaltlich gleich diesem ist.</returns>
        public override bool Equals(object? obj)
        {
            if (!base.Equals(obj))
            {
                return false;
            }
            if (Object.ReferenceEquals(this, obj))
            {
                return true;
            }
            LogicalNode logicalNode = this.GetLogicalNode();
            LogicalNode objLogicalNode = ((LogicalNodeViewModel)obj).GetLogicalNode();
            if (logicalNode != null && objLogicalNode != null)
            {
                return ((JobList)logicalNode).LogicalExpression == ((JobList)objLogicalNode).LogicalExpression;
            }
            return false;
        }

        /// <summary>
        /// Erzeugt einen Hashcode für dieses JobListViewModel.
        /// </summary>
        /// <returns>Integer mit Hashwert.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion public members

        #region internal members

        #endregion internal members

        #region private members

        private RelayCommand _btnRunTaskTreeRelayCommand;
        private RelayCommand _btnRunOrBreakTaskTreeRelayCommand;
        private RelayCommand _btnBreakTaskTreeRelayCommand;

        private void runTaskTreeExecute(object? parameter)
        {
            this._myLogicalNode.UserRun();
            this.RaisePropertyChanged("IsActive");
        }

        private bool canRunTaskTreeExecute()
        {
            if (LogicalNode.IsTreeFlushing || LogicalNode.IsTreePaused)
            {
                return false;
            }
            return true;
        }

        private void runOrBreakTaskTreeExecute(object? parameter)
        {
            if (this._myLogicalNode.CanTreeStart)
            {
                this._myLogicalNode.UserRun();
                this.RaisePropertyChanged("IsActive");
            }
            else
            {
                this._myLogicalNode.UserBreak();
                this.RaisePropertyChanged("IsActive");
            }
        }

        private bool canRunOrBreakTaskTreeExecute()
        {
            // bool canStart = this._myLogicalNode.CanTreeStart; // Falsch, da der Button die Bedeutung wechselt
            // und daher immer gestartet werden können muss.
            // return canStart;
            if (LogicalNode.IsTreeFlushing || LogicalNode.IsTreePaused)
            {
                return false;
            }
            return true;
        }

        private void breakTaskTreeExecute(object? parameter)
        {
            // 16+- this._myLogicalNode.ProcessTreeEvent("UserBreak", null);
            this._myLogicalNode.UserBreak();
        }

        private bool canBreakTaskTreeExecute()
        {
            //bool canStart = this._myLogicalNode.CanTreeStart;
            //return !canStart;
            if (LogicalNode.IsTreeFlushing || LogicalNode.IsTreePaused)
            {
                return false;
            }
            return true;
        }

        private void treeElementStateChanged(object sender, NodeState state)
        {
            // Buttons müssen zum Update gezwungen werden, da die Verarbeitung in einem
            // anderen Thread läuft:
            this.RaisePropertyChanged("ButtonRunText");
            this.RaisePropertyChanged("ButtonRunBreakText");
            this.RaisePropertyChanged("ButtonBreakText");
            this._btnRunTaskTreeRelayCommand.UpdateCanExecuteState(this.Dispatcher);
            this._btnRunOrBreakTaskTreeRelayCommand.UpdateCanExecuteState(this.Dispatcher);
            this._btnBreakTaskTreeRelayCommand.UpdateCanExecuteState(this.Dispatcher);
            this.RaisePropertyChanged("IsActive");
        }

        #endregion private members

    }
}
