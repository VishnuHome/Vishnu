using System.Windows;
using System.Windows.Controls;

namespace Vishnu.WPF_UI.DependencyProperties
{
    /// <summary>
    /// Statischer Container für Attached Properties.
    /// </summary>
    /// <remarks>
    /// File: AttachedPropertiesContainer.cs
    /// Autor: Erik Nagel
    ///
    /// 22.07.2013 Erik Nagel: erstellt
    /// </remarks>
    public static class AttachedPropertiesContainer
    {
        /// <summary>
        /// Bei True hat der Knoten einen Eltern-Knoten,
        /// bei False ist er die Root.
        /// </summary>
        /// 
        /// <AttachedPropertyComments>
        /// <summary>
        /// Attached Property (bool). Bei true hat der Knoten Child-Knoten.
        /// </summary>
        /// <value>Default: false.</value>
        /// </AttachedPropertyComments>
        public static readonly DependencyProperty HasParentProperty =
           DependencyProperty.RegisterAttached("HasParent",
           typeof(bool),
           typeof(AttachedPropertiesContainer),
            //new PropertyMetadata(CallBackWhenPropertyIsChanged));
            new FrameworkPropertyMetadata(
            false,
            FrameworkPropertyMetadataOptions.AffectsRender,
            CallBackWhenHasParentPropertyIsChanged));

        /// <summary>
        /// WPF-Getter für die HasParentProperty.
        /// </summary>
        /// <param name="obj">Das besitzende Control.</param>
        /// <returns>True, wenn der Knoten einen Eltern-Knoten besitzt.</returns>
        public static bool GetHasParent(DependencyObject obj)
        {
            return (bool)obj.GetValue(HasParentProperty);
        }

        /// <summary>
        /// WPF-Setter für die HasParentProperty.
        /// </summary>
        /// <param name="obj">Das besitzende Control.</param>
        /// <param name="value">Boolescher Wert.</param>
        public static void SetHasParent(
           DependencyObject obj,
           bool value)
        {
            obj.SetValue(HasParentProperty, value);
        }

        // Wird aufgerufen, wenn die Property gesetzt wurde (i.d.Regel einmalig).
        private static void CallBackWhenHasParentPropertyIsChanged(
         object sender,
         DependencyPropertyChangedEventArgs args)
        {
            Expander attachedObject = sender as Expander;
            if (attachedObject != null)
            {
                //Logger.Log(GetHasParent(attachedObject).ToString());
                // sonstige Verarbeitung, zum Beispiel
                // attachedObject.CallSomeMethod( 
                // args.NewValue as TargetPropertyType);
            }
        }

        /// <summary>
        /// Ausrichtung der Kind-Knoten, horizontal oder vertikal.
        /// </summary>
        /// 
        /// <AttachedPropertyComments>
        /// <summary>
        /// Attached Property (Orientation), Horizontal oder Vertical.
        /// </summary>
        /// <value>Default: Vertical.</value>
        /// </AttachedPropertyComments>
        public static readonly DependencyProperty ParentChildOrientationProperty =
           DependencyProperty.RegisterAttached("ParentChildOrientation",
           typeof(Orientation),
           typeof(AttachedPropertiesContainer),
            //new PropertyMetadata(CallBackWhenPropertyIsChanged));
            new FrameworkPropertyMetadata(
            Orientation.Vertical,
            FrameworkPropertyMetadataOptions.AffectsRender,
            CallBackWhenParentChildOrientationPropertyIsChanged));

        /// <summary>
        /// WPF-Getter für die ParentChildOrientationProperty.
        /// </summary>
        /// <param name="obj">Das besitzende Control.</param>
        /// <returns>Ausrichtung der Kind-Knoten: Horizontal oder Vertical.</returns>
        public static Orientation GetParentChildOrientation(DependencyObject obj)
        {
            return (Orientation)obj.GetValue(ParentChildOrientationProperty);
        }

        /// <summary>
        /// WPF-Setter für die ParentChildOrientationProperty.
        /// </summary>
        /// <param name="obj">Das besitzende Control.</param>
        /// <param name="value">Ausrichtung der Kind-Knoten: Horizontal oder Vertical.</param>
        public static void SetParentChildOrientation(
           DependencyObject obj,
           Orientation value)
        {
            obj.SetValue(ParentChildOrientationProperty, value);
        }

        // Wird aufgerufen, wenn die Property gesetzt wurde (i.d.Regel einmalig).
        private static void CallBackWhenParentChildOrientationPropertyIsChanged(
         object sender,
         DependencyPropertyChangedEventArgs args)
        {
            Expander attachedObject = sender as Expander;
            if (attachedObject != null)
            {
                //Logger.Log(GetParentChildOrientation(attachedObject).ToString());
                // sonstige Verarbeitung, zum Beispiel
                // attachedObject.CallSomeMethod( 
                // args.NewValue as TargetPropertyType);
            }
        }
    }
}
