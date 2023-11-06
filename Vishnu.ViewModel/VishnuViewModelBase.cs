using LogicalTaskTree;
using NetEti.ApplicationControl;
using NetEti.MVVMini;
using System;
using System.Windows;
using System.Windows.Input;
using Vishnu.Interchange;

namespace Vishnu.ViewModel
{
    /// <summary>
    /// Basisklasse für alle Vishnu-ViewModels.
    /// Erbt von ObservableObject u.a. INotifyPropertyChanged
    /// und implementiert IVishnuViewModel.
    /// </summary>
    /// <remarks>
    /// File: VishnuViewModelBase.cs
    /// Autor: Erik Nagel
    ///
    /// 22.05.2015 Erik Nagel: erstellt
    /// </remarks>
    public abstract class VishnuViewModelBase : ObservableObject, IVishnuViewModel, IVishnuRenderWatcher
    {
        /// <summary>
        /// Command für das ContextMenuItem "Reload" im ContextMenu für das "MainGrid" des Controls.
        /// </summary>
        public ICommand ReloadLogicalTaskTree { get { return this._btnReloadTaskTreeRelayCommand; } }

        /// <summary>
        /// Command für das ContextMenuItem "Log Tree" im ContextMenu für das "MainGrid" des Controls.
        /// </summary>
        public ICommand LogLogicalTaskTree { get { return this._btnLogTaskTreeRelayCommand; } }

        /// <summary>
        /// Command für das ContextMenuItem "Show Log" im ContextMenu für das "MainGrid" des Controls.
        /// </summary>
        public ICommand ShowLogLogicalTaskTree { get { return this._btnShowLogTaskTreeRelayCommand; } }

        /// <summary>
        /// Command für das ContextMenuItem "Pause Tree" im ContextMenu für das "MainGrid" des Controls.
        /// </summary>
        public ICommand PauseResumeLogicalTaskTree { get { return this._btnPauseResumeTaskTreeRelayCommand; } }

        #region IVishnuViewModel

        /// <summary>
        /// Das ReturnObject der zugeordneten LogicalNode.
        /// </summary>
        public virtual Result? Result
        {
            get
            {
                return this._result;
            }
            set
            {
                if (this._result != value)
                {
                    this._result = value;
                    this.RaisePropertyChanged("Result");
                }
            }
        }

        /// <summary>
        /// Das Parent-Control.
        /// </summary>
        public FrameworkElement? ParentView
        {
            get
            {
                return this._parentView;
            }
            set
            {
                if (this._parentView != value)
                {
                    this._parentView = value;
                    if (this._parentView != null)
                    {
                        this.ParentViewToBL(this._parentView);
                    }
                    this.RaisePropertyChanged("ParentView");
                }
            }
        }

        /// <summary>
        /// Bindung an ein optionales, spezifisches User-ViewModel.
        /// </summary>
        public object? UserDataContext
        {
            get
            {
                return this._userDataContext;
            }
            set
            {
                if (this._userDataContext != value)
                {
                    this._userDataContext = value;
                    this.RaisePropertyChanged("UserDataContext");
                }
            }
        }

        #endregion IVishnuViewModel

        /// <summary>
        /// Eindeutiger GlobalUniqueIdentifier.
        /// Wird im Konstruktor vergeben und fließt in die überschriebene Equals-Methode ein.
        /// Dadurch wird erreicht, dass nach Reload von Teilen des LogicalTaskTree und erneutem
        /// Reload von vorherigen Ständen des LogicalTaskTree Elemente des ursprünglich 
        /// gecachten VisualTree fälschlicherweise anstelle der neu geladenen Elemente in den
        /// neuen VisualTree übernommen werden.
        /// </summary>
        public string VisualTreeCacheBreaker
        {
            get
            {
                return this._visualTreeCacheBreaker;
            }
            private set
            {
                if (this._visualTreeCacheBreaker != value)
                {
                    this._visualTreeCacheBreaker = value;
                    this.RaisePropertyChanged("VisualTreeCacheBreaker");
                    this.RaisePropertyChanged("DebugNodeInfos");
                }
            }
        }

        /// <summary>
        /// Liefert oder setzt die Zeile im enthaltenden Grid für das aktuelle Element.
        /// </summary>
        /// <returns>die Zeile im enthaltenden Grid.</returns>
        public int GridRow
        {
            get
            {
                return this._gridRow;
            }
            set
            {
                if (this._gridRow != value)
                {
                    this._gridRow = value;
                    this.RaisePropertyChanged("GridRow");
                }
            }
        }

        /// <summary>
        /// Liefert oder setzt die Spalte im enthaltenden Grid für das aktuelle Element.
        /// </summary>
        /// <returns>die Spalte im enthaltenden Grid.</returns>
        public int GridColumn
        {
            get
            {
                return this._gridColumn;
            }
            set
            {
                if (this._gridColumn != value)
                {
                    this._gridColumn = value;
                    this.RaisePropertyChanged("GridColumn");
                }
            }
        }

        /// <summary>
        /// Zeigt an, ob das zugehörige Control vollständig gezeichnet wurde.
        /// </summary>
        public bool IsRendered
        {
            get
            {
                return this._isRendered;
            }
            private set
            {
                if (this._isRendered != value)
                {
                    this._isRendered = value;
                    this.RaisePropertyChanged("IsRendered");
                }
            }
        }


    /// <summary>
    /// Liefert oder setzt die Zeilennummer des zugehörigen Controls
    /// in einer quadratischen Matrix.
    /// Dieser Wert wird zu einem geeigneten Zeitpunkt in die Property GridRow geschoben,
    /// um die WPF-GUI zu informieren.
    /// </summary>
    /// <returns>Die Zeilennummer des zugehörigen Controls in einer quadratischen Matrix.</returns>
    public int RowNumber;

        /// <summary>
        /// Liefert oder setzt die Spaltennummer des zugehörigen Controls
        /// in einer quadratischen Matrix.
        /// Dieser Wert wird zu einem geeigneten Zeitpunkt in die Property GridColumn geschoben,
        /// um die WPF-GUI zu informieren.
        /// </summary>
        /// <returns>Die Spaltennummer des zugehörigen Controls in einer quadratischen Matrix.</returns>
        public int ColumnNumber;

        /// <summary>
        /// Vergibt einen neuen GlobalUniqueIdentifier für den VisualTreeCacheBreaker.
        /// </summary>
        public virtual void Invalidate()
        {
            this.VisualTreeCacheBreaker = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Liefert einen string für Debug-Zwecke.
        /// </summary>
        /// <returns>Ein String für Debug-Zwecke.</returns>
        public virtual string GetDebugNodeInfos()
        {
            return this.VisualTreeCacheBreaker;
        }

        #region IVishnuRenderWatcher Implementation

        /// <summary>
        /// Wird von DynamicUserControlBase angesprungen, wenn das UserControl vollständig gerendered wurde.
        /// </summary>
        /// <param name="dynamicUserControl">Das aufrufende DynamicUserControlBase als Object.</param>
        public virtual void UserControlContentRendered(object dynamicUserControl)
        {
            this.GridColumn = this.ColumnNumber;
            this.GridRow = this.RowNumber;
            this.IsRendered = true;
        }

        #endregion IVishnuRenderWatcher Implementation

        #region context menu

        /// <summary>
        /// Ist zum Neu-Laden des Trees an geeigneter Stelle nach Änderung
        /// der JobDescriptions vorgesehen. Kann dafür überschrieben werden.
        /// </summary>
        /// <param name="parameter">Optionaler Parameter oder null.</param>
        public virtual void ReloadTaskTreeExecute(object? parameter) { }

        /// <summary>
        /// Liefert true, wenn die Funktion ausführbar ist, hier immer false.
        /// Kann an geeigneter Stelle überschrieben werden.
        /// </summary>
        /// <returns>True, wenn die Funktion ausführbar ist.</returns>
        public virtual bool CanReloadTaskTreeExecute()
        {
            return false;
        }

        /// <summary>
        /// Ist zum Loggen des Trees an geeigneter Stelle vorgesehen.
        /// Kann dafür überschrieben werden.
        /// </summary>
        /// <param name="parameter">Optionaler Parameter oder null.</param>
        public virtual void LogTaskTreeExecute(object? parameter) { }

        /// <summary>
        /// Liefert true, wenn die Funktion ausführbar ist, hier immer false.
        /// Kann an geeigneter Stelle überschrieben werden.
        /// </summary>
        /// <returns>True, wenn die Funktion ausführbar ist.</returns>
        public virtual bool CanLogTaskTreeExecute()
        {
            return false;
        }

        /// <summary>
        /// Ist zum Anzeigen des Vishnu-Logs vorgesehen.
        /// Kann dafür an geeigneter Stelle überschrieben werden.
        /// </summary>
        /// <param name="parameter">Optionaler Parameter oder null.</param>
        public virtual void ShowLogExecute(object? parameter) { }

        /// <summary>
        /// Liefert true, wenn die Funktion ausführbar ist, hier immer false.
        /// Kann an geeigneter Stelle überschrieben werden.
        /// </summary>
        /// <returns>True, wenn die Funktion ausführbar ist.</returns>
        public virtual bool CanShowLogExecute()
        {
            return false;
        }

        /// <summary>
        /// Wechselschalter - hält den Tree an oder lässt ihn weiterlaufen.
        /// </summary>
        /// <param name="parameter">Optionaler Parameter, wird hier nicht genutzt.</param>
        public virtual void PauseResumeTaskTreeExecute(object? parameter)
        {
            if (!LogicalNode.IsTreeFlushing && !LogicalNode.IsTreePaused)
            {
                LogicalNode.PauseTree();
            }
            else
            {
                LogicalNode.ResumeTree();
            }
        }

        /// <summary>
        /// Liefert true, wenn die Funktion ausführbar ist, hier immer true.
        /// Kann an geeigneter Stelle überschrieben werden.
        /// </summary>
        /// <returns>True, wenn die Funktion ausführbar ist.</returns>
        public virtual bool CanPauseResumeTaskTreeExecute()
        {
            return true;
        }

        #endregion context menu

        /// <summary>
        /// Konstruktor - setzt den VisualTreeCacheBreaker.
        /// </summary>
        public VishnuViewModelBase()
        {
            this._btnReloadTaskTreeRelayCommand = new RelayCommand(ReloadTaskTreeExecute, CanReloadTaskTreeExecute);
            this._btnLogTaskTreeRelayCommand = new RelayCommand(LogTaskTreeExecute, CanLogTaskTreeExecute);
            this._btnShowLogTaskTreeRelayCommand = new RelayCommand(ShowLogExecute, CanShowLogExecute);
            this._btnPauseResumeTaskTreeRelayCommand = new RelayCommand(PauseResumeTaskTreeExecute, CanPauseResumeTaskTreeExecute);
            this._visualTreeCacheBreaker = Guid.NewGuid().ToString();
            this.IsRendered = false;
        }

        /// <summary>
        /// Vergleicht den Inhalt dieses LogicalNodeViewModels nach logischen Gesichtspunkten
        /// mit dem Inhalt eines übergebenen LogicalNodeViewModels.
        /// </summary>
        /// <param name="obj">Das LogicalNodeViewModel zum Vergleich.</param>
        /// <returns>True, wenn das übergebene LogicalNodeViewModel inhaltlich gleich diesem ist.</returns>
        public override bool Equals(object? obj)
        {
            if (obj == null || this.GetType() != obj.GetType())
            {
                return false;
            }
            return (Object.ReferenceEquals(this, obj));
        }

        /// <summary>
        /// Erzeugt einen Hashcode für dieses LogicalNodeViewModel.
        /// </summary>
        /// <returns>Integer mit Hashwert.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode() + this.VisualTreeCacheBreaker.GetHashCode();
        }

        /// <summary>
        /// Kann überschrieben werden, um das Parent-Control
        /// in der Geschäftslogik zu speichern.
        /// </summary>
        /// <param name="parentView">Das Parent-Control.</param>
        protected virtual void ParentViewToBL(FrameworkElement parentView) { }

        private RelayCommand _btnReloadTaskTreeRelayCommand;
        private RelayCommand _btnLogTaskTreeRelayCommand;
        private RelayCommand _btnShowLogTaskTreeRelayCommand;
        private RelayCommand _btnPauseResumeTaskTreeRelayCommand;
        private Result? _result;
        private object? _userDataContext;
        private FrameworkElement? _parentView;
        private string _visualTreeCacheBreaker;
        private int _gridRow;
        private int _gridColumn;
        private bool _isRendered;
    }
}
