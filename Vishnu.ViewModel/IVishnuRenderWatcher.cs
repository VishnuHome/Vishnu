namespace Vishnu.ViewModel
{
    /// <summary>
    /// Interface für die Weiterverarbeitung von ContentRendered-Events.
    /// </summary>
    public interface IVishnuRenderWatcher
    {
        /// <summary>
        /// Wird von DynamicUserControlBase angesprungen, wenn das UserControl vollständig gezeichnet wurde.
        /// </summary>
        /// <param name="dynamicUserControl">Das aufrufende DynamicUserControlBase als Object.</param>
        void UserControlContentRendered(object dynamicUserControl);
    }
}
