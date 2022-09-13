using System.Windows;

namespace Vishnu.Interchange
{
    /// <summary>
    /// Parameter, die Checkern bei jedem Aufruf
    /// von Vishnu mitgegeben werden (nicht User-spezifisch).
    /// </summary>
    /// <remarks>
    /// File: TreeParameters.cs
    /// Autor: Erik Nagel
    ///
    /// 16.06.2015 Erik Nagel: erstellt
    /// </remarks>
    public class TreeParameters
    {
        /// <summary>
        /// Name des Trees.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Pfad zum Verzeichnis der Checker.dll.
        /// </summary>
        public string CheckerDllDirectory { get; set; }

        /// <summary>
        /// Das Parent-Control.
        /// </summary>
        public FrameworkElement ParentView { get; set; }

        /*
        /// <summary>
        /// Absolute Bildschirmposition des beinhaltenden Controls.
        /// </summary>
        /// <returns>Absolute Bildschirmposition der linken oberen Ecke des Parent-Controls.</returns>
        public virtual Point GetParentViewAbsoluteScreenPosition()
        {
            if (this.ParentView != null)
            {
                return this.ParentView.GetParentViewAbsoluteScreenPosition();
            }
            else
            {
                return new Point(System.Windows.SystemParameters.PrimaryScreenWidth / 2.0 - 150,
                  System.Windows.SystemParameters.PrimaryScreenHeight / 2.0 - 75);
            }
        }
        */

        /// <summary>
        /// Absolute Bildschirmposition des beinhaltenden Controls.
        /// </summary>
        /// <returns>Absolute Bildschirmposition der linken oberen Ecke des Parent-Controls.</returns>
        public virtual Point GetParentViewAbsoluteScreenPosition()
        {
            if (this.ParentView != null)
            {
                Rect rect = this.ParentView.GetAbsolutePlacement();
                return new Point(rect.Left, rect.Top);
            }
            else
            {
                return new Point(System.Windows.SystemParameters.PrimaryScreenWidth / 2.0 - 150,
                  System.Windows.SystemParameters.PrimaryScreenHeight / 2.0 - 75);
            }
        }

        /// <summary>
        /// Konstruktor - übernimmt das Parent-Control.
        /// </summary>
        /// <param name="name">Der logische Name des Trees.</param>
        /// <param name="parentView">Das Parent-Control.</param>
        public TreeParameters(string name, FrameworkElement parentView)
        {
            this.Name = name;
            this.ParentView = parentView;
        }

        /// <summary>
        /// Überschriebene ToString-Methode - returniert nur den Namen.
        /// </summary>
        /// <returns>Name-Property der TreeParameters.</returns>
        public override string ToString()
        {
            return this.Name;
        }

        /// <summary>
        /// Überschriebene Equals-Methode.
        /// </summary>
        /// <param name="obj">Vergleichs-TreeParameters.</param>
        /// <returns>True, wenn beide Objekte Instanzen von TreeParameters sind und ihr Hashcode übereinstimmt.</returns>
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        /// <summary>
        /// Überschriebene GetHashCode-Methode.
        /// </summary>
        /// <returns>Ein möglichst eindeutiger Integer für alle Properties einer Instanz zusammen.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Interner, Assembly-übergreifender Parameter.
        /// Enthält den LogicalTaskTree als IBusinessLogicRoot, um hier einen direkten
        /// Assembly-Verweis auf LogicalTaskTree zu vermeiden.
        /// Wird innerhalb der Assembly Vishnu.ViewModel wieder aufgelöst.
        /// Dort existiert ein Verweis auf LogicalTaskTree.
        /// </summary>
        public IBusinessLogicRoot BusinessLogicRoot { get; set; }

        /// <summary>
        /// Interner, Assembly-übergreifender Parameter.
        /// Enthält das LogicalTaskTreeViewModel als IViewModelRoot, um hier einen direkten
        /// Assembly-Verweis auf Vishnu.ViewModel zu vermeiden.
        /// Wird innerhalb der Assembly Vishnu.ViewModel wieder aufgelöst.
        /// </summary>
        public IViewModelRoot ViewModelRoot { get; set; }
    }
}
