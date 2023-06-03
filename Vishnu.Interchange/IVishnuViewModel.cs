using System.ComponentModel;
using System.Windows;

namespace Vishnu.Interchange
{
    /// <summary>
    /// Interface für die ViewModels von dynamischen User-Controls.
    /// </summary>
    public interface IVishnuViewModel
    {
        /// <summary>
        /// Wird ausgelöst, wenn sich eine Property ändert.
        /// </summary>
        event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Das ReturnObject der zugeordneten LogicalNode.
        /// </summary>
        Result? Result { get; }

        /// <summary>
        /// Das Parent-Control.
        /// </summary>
        FrameworkElement? ParentView { get; set; }

        /// <summary>
        /// Bindung an ein optionales, spezifisches User-ViewModel.
        /// </summary>
        object? UserDataContext { get; set; }

    }
}
