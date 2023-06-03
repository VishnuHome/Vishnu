using NetEti.MVVMini;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Vishnu.Interchange;
using Vishnu.ViewModel;

namespace Vishnu.DemoApplications.JobListUserControlDemo
{
    /// <summary>
    /// Minimale gemeinsame Basisklasse für LogicalTaskTreeViewModel
    /// und DummyLogicalTaskTreeViewModel.
    /// </summary>
    public class MainWindowViewModel : VishnuViewModelBase
    {
        /// <summary>
        /// Definiert, ob die Kind-Elemente dieses Knotens
        /// horizontal oder vertikal angeordnet werden sollen.
        /// </summary>
        public Orientation ChildOrientation
        {
            get
            {
                return Orientation.Horizontal;
            }
        }

        /// <summary>
        /// Standard-Konstruktor.
        /// </summary>
        public MainWindowViewModel()
        {
            this.Items = new List<VishnuViewModelBase>() { new DemoJobListViewModel() };
        }

        /// <summary>
        /// ItemsSource für die TreeView im MainWindow.
        /// </summary>
        public List<VishnuViewModelBase> Items { get; protected set; }

    }
}
