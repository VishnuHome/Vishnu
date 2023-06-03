using Vishnu.Interchange;
using Vishnu.ViewModel;

namespace Vishnu.DemoApplications.SingleNodeUserControlDemo
{
    /// <summary>
    /// Funktion: ViewModel für das User-spezifische Result.
    /// Löst das ReturnObject eines Checkers in Properties auf.
    /// </summary>
    /// <remarks>
    /// File: ResultViewModel
    /// Autor: Erik Nagel
    ///
    /// 13.03.2015 Erik Nagel: erstellt
    /// </remarks>
    public class ResultViewModel : DynamicUserControlViewModelBase
    {
        /// <summary>
        /// Testproperty für SingleNodeUserControl
        /// </summary>
        public string? TestProperty1
        {
            get
            {
                return this.GetResultProperty<string>(typeof(ComplexReturnObject), nameof(TestProperty1));
            }
        }

        /// <summary>
        /// Testproperty für SingleNodeUserControl
        /// </summary>
        public int TestProperty2
        {
            get
            {
                return this.GetResultProperty<int>(typeof(ComplexReturnObject), nameof(TestProperty2));
            }
        }

        /// <summary>
        /// Die speziell für die Darstellung des Results entwickelte IVishnuViewModel-Implementierung.
        /// </summary>
        /// <param name="parentViewModel">Von Vishnu übergebenes IVishnuViewModel.</param>
        public ResultViewModel(IVishnuViewModel parentViewModel)
        {
            this.ParentViewModel = parentViewModel;
            if (parentViewModel != null) // wg. ReferenceNullException im DesignMode
            {
                this.ParentViewModel.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(parentViewModel_PropertyChanged);
            }
        }

        void parentViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Result")
            {
                this.RaisePropertyChanged("TestProperty1");
                this.RaisePropertyChanged("TestProperty2");
            }
        }

    }
}
