using NetEti.MVVMini;
using System.Collections.ObjectModel;
using Vishnu.Interchange;

namespace Vishnu.ViewModel
{
    /// <summary>
    /// ViewModel für die Darstellung eines logicalTaskTree
    /// als gruppierte und gefilterte Liste von Knoten.
    /// </summary>
    /// <remarks>
    /// File: JobGroupViewModel.cs
    /// Autor: Erik Nagel
    ///
    /// 01.09.2014 Erik Nagel: erstellt
    /// </remarks>
    public class JobGroupViewModel : ObservableObject
    {
        /// <summary>
        /// ViewModel für den LogicalTaskTree.
        /// </summary>
        public JobListViewModel GroupJobList { get; set; }

        /// <summary>
        /// ItemsSource für eine einfache Auflistung von Endknoten des Trees.
        /// </summary>
        public ObservableCollection<LogicalNodeViewModel> FlatNodeViewModelList
        {
            get
            {
                return this._flatNodeViewModelList;
            }
            private set
            {
                this._flatNodeViewModelList = value;
            }
        }

        /// <summary>
        /// Konstruktor - übernimmt das anzuzeigende JobListViewModel und
        /// einen Filter für anzuzeigende NodeTypes.
        /// </summary>
        /// <param name="groupJobList">Anzuzeigendes JobListViewModel.</param>
        /// <param name="flatNodeListFilter">Filter für anzuzeigende NodeTypes.</param>
        public JobGroupViewModel(JobListViewModel groupJobList, NodeTypes flatNodeListFilter)
        {
            this.GroupJobList = groupJobList;
            this._flatNodeListFilter = flatNodeListFilter;
            this.FlatNodeViewModelList = LogicalTaskTreeViewModel.FlattenTree(this.GroupJobList, new ObservableCollection<LogicalNodeViewModel>(), this._flatNodeListFilter, false);
            this.RaisePropertyChanged("FlatNodeViewModelList");
        }

        private ObservableCollection<LogicalNodeViewModel> _flatNodeViewModelList;
        private NodeTypes _flatNodeListFilter;

    }
}
