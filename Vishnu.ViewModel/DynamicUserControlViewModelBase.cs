using System;
using NetEti.MVVMini;
using Vishnu.Interchange;

namespace Vishnu.ViewModel
{
    /// <summary>
    /// Basisklasse für ViewModels von dynamischen UserControls für Vishnu.
    /// Stellt den Zugriff auf User-Properties innerhalb des ReturnObjects des
    /// zugeordneten Checkers zur Verfügung.
    /// </summary>
    /// <remarks>
    /// File: DynamicUserControlViewModelBase.cs
    /// Autor: Erik Nagel
    ///
    /// 10.01.2015 Erik Nagel: erstellt
    /// </remarks>
    public class DynamicUserControlViewModelBase : ObservableObject
    {
        /// <summary>
        /// Das ViewModel des besitzenden Vishnu-Knoten.
        /// </summary>
        protected IVishnuViewModel? ParentViewModel;

        /// <summary>
        /// Holt eine Property mit Namen propertyName vom Typ T
        /// aus dem ReturnObject des besitzenden Vishnu-Knotens.
        /// Das ReturnObject muss vom Typ requiredReturnObjectType sein.
        /// </summary>
        /// <typeparam name="T">Typ der gesuchten Property.</typeparam>
        /// <param name="requiredReturnObjectType">Typ des ReturnObjects des besitzenden Knoten.</param>
        /// <param name="propertyName">Name der gesuchten Property oder null. Bei null wird das ReturnObject selbst zurückgegeben.</param>
        /// <returns>Property aus dem ReturnObject des besitzenden Vishnu-Knotens.</returns>
        protected T? GetResultProperty<T>(Type requiredReturnObjectType, string propertyName)
        {
            if (this.ParentViewModel != null && this.ParentViewModel.Result != null && this.ParentViewModel.Result.ReturnObject != null)
            {
                if (!String.IsNullOrEmpty(propertyName))
                {
                    // && this._parentViewModel.Result.ReturnObject is ...) // geht nicht!
                    if (this.ParentViewModel.Result.ReturnObject.GetType().Name.Equals(requiredReturnObjectType.Name))
                    {
                        return Vishnu.Interchange.GenericPropertyGetter.GetProperty<T>(this.ParentViewModel.Result.ReturnObject, propertyName);
                        //return new Vishnu.Interchange.GenericPropertyGetter().GetProperty<T>(this.ParentViewModel.Result.ReturnObject, propertyName);
                    }
                }
                else
                {
                    return (T)this.ParentViewModel.Result.ReturnObject;
                }
            }
            return default(T);
        }

    }
}
