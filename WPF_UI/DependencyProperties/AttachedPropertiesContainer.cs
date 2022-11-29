using NetEti.ApplicationControl;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Vishnu.ViewModel;

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
    /// 28.11.2022 Erik Nagel: LastNotNullLogicalProperty.
    /// </remarks>
    public static class AttachedPropertiesContainer
    {
        static AttachedPropertiesContainer()
        {
            LastNotNullLogicalProperty = ConstructLastNotNullLogicalProperty();
        }

        #region LastNotNullLogical

        /// <summary>
        /// Attached Property für einen nullable Boolean zur Weitergabe des logischen Zustands
        /// des dem Control zugeordneten Checkers (wird z.B. zur abhängigen Farbgebung
        /// (false=rot, true=grün) einer übergeordneten Border in einem ControlTemplate genutzt):
        /// Nach diversen Fehlversuchen hat sich als einzige gangbare Lösung folgende Vorgehensweise
        /// herauskristallisiert: einem dem ControlTemplate übergeordneten Control oder
        /// DataTemplate wird die AttachedPropery "LastNotNullLogical" zugeordnet:
        ///     &lt;Expander Name="Exp" Template="{StaticResource ExpanderStyleHeaderCentered}"
        ///                   ...
        ///                   attached:AttachedPropertiesContainer.LastNotNullLogical="{Binding LastNotNullLogical, diag:PresentationTraceSources.TraceLevel=High}"
        ///     &gt;
        /// Die AttachedPropery "LastNotNullLogical" wird dabei direkt an "LastNotNullLogical" aus
        /// dem DataContext, hier "LogicalNodeViewModel" gebunden.
        /// Im untergeordneten ControlTemplate, hier &lt;ControlTemplate TargetType="ToggleButton"&gt;
        /// in LogicalTaskTreeControlStaticResourceDictionary.xaml werden DataTrigger an das übergeornete
        /// Control mit Path auf die AttachedPropery "LastNotNullLogical" gebunden:
        ///    &lt;DataTrigger Binding="{Binding RelativeSource={RelativeSource FindAncestor, AncestorType={x:Type Expander}},
        ///                    Path=(attached:AttachedPropertiesContainer.LastNotNullLogical)}" Value="True"&gt;
        ///        &lt;Setter Property = "Border.BorderBrush" TargetName="ToggleButtonBorder" Value="{StaticResource ItemBorderBrushGreen}" /&gt;
        ///    &lt;/DataTrigger&gt;
        ///    &lt;DataTrigger Binding = "{Binding RelativeSource={RelativeSource FindAncestor, AncestorType={x:Type Expander}},
        ///                    Path=(attached:AttachedPropertiesContainer.LastNotNullLogical)}" Value="False"&gt;
        ///        &lt;Setter Property = "Border.BorderBrush" TargetName = "ToggleButtonBorder" Value = "{StaticResource ItemBorderBrushRed}" /&gt;
        ///    &lt;/ DataTrigger&gt;
        ///    
        ///    Wichtiger Hinweis: andere Lösungsansätze scheiterten spätestens bei Umschaltung der Tree-Orientierung.
        /// </summary>
        public static readonly DependencyProperty LastNotNullLogicalProperty;

        private static FrameworkPropertyMetadata _lastNotNullLogicalMetadata;

        private static DependencyProperty ConstructLastNotNullLogicalProperty()
        {
            _lastNotNullLogicalMetadata = new FrameworkPropertyMetadata();
            _lastNotNullLogicalMetadata.AffectsRender = true;
            _lastNotNullLogicalMetadata.PropertyChangedCallback = new PropertyChangedCallback(
                (sender, eventargs) =>
                {
                    if (sender is FrameworkElement)
                    {
                        // InfoController.Say(string.Format($"{(sender as FrameworkElement).Name} LastNotNullLogicalProperty changed."));
                    }
                }
            );
            return DependencyProperty.RegisterAttached("LastNotNullLogical", typeof(bool?), typeof(AttachedPropertiesContainer), _lastNotNullLogicalMetadata);
        }

        /// <summary>
        /// WPF-Setter für die LastNotNullLogicalProperty.
        /// </summary>
        /// <param name="obj">Das besitzende Control.</param>
        /// <param name="val">Ein nullable Boolean, der dem letzten logischen Zustand des Controls entspricht.</param>
        public static void SetLastNotNullLogical(DependencyObject obj, bool? val)
        {
            obj.SetValue(LastNotNullLogicalProperty, val);
        }

        /// <summary>
        /// WPF-Getter für die LastNotNullLogicalProperty.
        /// </summary>
        /// <param name="obj">Das besitzende Control.</param>
        /// <returns>Ein nullable Boolean, der dem letzten logischen Zustand des Controls entspricht.</returns>
        public static bool? GetLastNotNullLogical(DependencyObject obj)
        {
            return (bool?)obj.GetValue(LastNotNullLogicalProperty);
        }

        #endregion LastNotNullLogical

        #region HasParentProperty

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

        #endregion HasParentProperty

        #region ParentChildOrientationProperty

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

        #endregion ParentChildOrientationProperty

    }
}
