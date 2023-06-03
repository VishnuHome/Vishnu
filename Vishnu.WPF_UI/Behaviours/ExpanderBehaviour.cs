using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

namespace Vishnu.WPF_UI.Behaviours
{
    /// <summary>
    /// Mappt Expander-Events auf Commands.
    /// </summary>
    /// <remarks>
    /// File: ExpanderBehaviour.cs
    /// Autor: Erik Nagel
    ///
    /// 29.06.2013 Erik Nagel: erstellt
    /// </remarks>
    public static class ExpanderBehaviour
    {
        #region Expanded Event

        private static DependencyProperty? _expandedProperty;
        private static readonly RoutedEvent _expandedEvent = Expander.ExpandedEvent;

        /// <summary>
        /// Attached Property (ICommand) für das Expanded-Event eines Expanders.
        /// </summary>
        /// 
        /// <AttachedPropertyComments>
        /// <summary>
        /// Attached Property (ICommand) für das Expanded-Event eines Expanders.
        /// </summary>
        /// </AttachedPropertyComments>
        public static readonly DependencyProperty ExpandedCommandProperty =
            DependencyProperty.RegisterAttached("ExpandedCommand",
            typeof(ICommand), typeof(ExpanderBehaviour),
            new PropertyMetadata(new PropertyChangedCallback(ExpandedCallBack)));

        /// <summary>
        /// WPF-Setter für das ExpandedCommand.
        /// </summary>
        /// <param name="obj">Das besitzende Control.</param>
        /// <param name="value">Das Command.</param>
        public static void SetExpandedCommand(UIElement obj, ICommand value)
        {
            obj.SetValue(ExpandedCommandProperty, value);
        }

        /// <summary>
        /// WPF-Getter für das ExpandedCommand.
        /// </summary>
        /// <param name="obj">Das besitzende Control.</param>
        /// <returns>Das Command.</returns>
        public static ICommand GetExpandedCommand(UIElement obj)
        {
            return (ICommand)obj.GetValue(ExpandedCommandProperty);
        }

        static void ExpandedCallBack(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            UIElement element = (UIElement)obj;

            _expandedProperty = args.Property;

            if (element != null)
            {
                if (args.OldValue != null)
                {
                    element.AddHandler(_expandedEvent, new RoutedEventHandler(ExpandedEventHandler));
                }

                if (args.NewValue != null)
                {
                    element.AddHandler(_expandedEvent, new RoutedEventHandler(ExpandedEventHandler));
                }
            }
        }

        /// <summary>
        /// Event-Handler für das Expanded-Event des Expanders.
        /// </summary>
        /// <param name="sender">Das Command.</param>
        /// <param name="e">Argumente.</param>
        public static void ExpandedEventHandler(object sender, RoutedEventArgs e)
        {
            DependencyObject obj = (DependencyObject)sender;

            if (obj != null && obj == e.OriginalSource)
            {
                ICommand command = (ICommand)obj.GetValue(_expandedProperty);

                if (command != null)
                {
                    if (command.CanExecute(e))
                    {
                        Expander exp = (Expander)e.OriginalSource;

                        if (exp != null)
                        {
                            command.Execute("expanded");
                        }
                    }
                }
            }
            e.Handled = true;
        }

        #endregion Expanded Event

        #region Collapsed Event

        private static DependencyProperty? _collapsedProperty;
        private static readonly RoutedEvent _collapsedEvent = Expander.CollapsedEvent;

        //private static readonly RoutedEvent _collapsedEvent = Expander.CollapsedEvent;

        /// <summary>
        /// Attached Property für das Collapsed-Event eines Expanders.
        /// </summary>
        /// 
        /// <AttachedPropertyComments>
        /// <summary>
        /// Attached Property (ICommand) für das Collapsed-Event eines Expanders.
        /// </summary>
        /// </AttachedPropertyComments>
        public static readonly DependencyProperty CollapsedCommandProperty =
            DependencyProperty.RegisterAttached("CollapsedCommand",
            typeof(ICommand), typeof(ExpanderBehaviour),
            new PropertyMetadata(new PropertyChangedCallback(CollapsedCallBack)));

        /// <summary>
        /// WPF-Setter für das CollapsedCommand.
        /// </summary>
        /// <param name="obj">Das besitzende Control.</param>
        /// <param name="value">Das Command.</param>
        public static void SetCollapsedCommand(UIElement obj, ICommand value)
        {
            obj.SetValue(CollapsedCommandProperty, value);
        }

        /// <summary>
        /// WPF-Getter für das CollapsedCommand.
        /// </summary>
        /// <param name="obj">Das besitzende Control.</param>
        /// <returns>Das Command.</returns>
        public static ICommand GetCollapsedCommand(UIElement obj)
        {
            return (ICommand)obj.GetValue(CollapsedCommandProperty);
        }

        static void CollapsedCallBack(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            UIElement element = (UIElement)obj;

            _collapsedProperty = args.Property;

            if (element != null)
            {
                if (args.OldValue != null)
                {
                    element.AddHandler(_collapsedEvent, new RoutedEventHandler(CollapsedEventHandler));
                }

                if (args.NewValue != null)
                {
                    element.AddHandler(_collapsedEvent, new RoutedEventHandler(CollapsedEventHandler));
                }
            }
        }

        /// <summary>
        /// Event-Handler für das Collapsed-Event des Expanders.
        /// </summary>
        /// <param name="sender">Das Command.</param>
        /// <param name="e">Argumente.</param>
        public static void CollapsedEventHandler(object sender, RoutedEventArgs e)
        {
            DependencyObject obj = (DependencyObject)sender;

            if (obj != null && obj == e.OriginalSource)
            {
                ICommand command = (ICommand)obj.GetValue(_collapsedProperty);

                if (command != null)
                {
                    if (command.CanExecute(e))
                    {
                        Expander exp = (Expander)e.OriginalSource;

                        if (exp != null)
                        {
                            //if (sender is UIElement)
                            //{
                            //  (sender as UIElement).RaiseEvent(new RoutedEventArgs(LogicalTaskTreeControl.TreeExpandingStateChangedEvent));
                            //}
                            command.Execute("collapsed");
                        }
                    }
                }
            }
            e.Handled = true;
        }

        #endregion Collapsed Event

        #region SizeChanged Event

        private static DependencyProperty? _sizeChangedProperty;
        private static readonly RoutedEvent _sizeChangedEvent = Expander.SizeChangedEvent;

        //private static readonly RoutedEvent _SizeChangedEvent = Expander.SizeChangedEvent;

        /// <summary>
        /// Attached Property für das SizeChanged-Event eines Expanders.
        /// </summary>
        /// 
        /// <AttachedPropertyComments>
        /// <summary>
        /// Attached Property (ICommand) für das SizeChanged-Event eines Expanders.
        /// </summary>
        /// </AttachedPropertyComments>
        public static readonly DependencyProperty SizeChangedCommandProperty =
            DependencyProperty.RegisterAttached("SizeChangedCommand",
            typeof(ICommand), typeof(ExpanderBehaviour),
            new PropertyMetadata(new PropertyChangedCallback(SizeChangedCallBack)));

        /// <summary>
        /// WPF-Setter für das SizeChangedCommand.
        /// </summary>
        /// <param name="obj">Das besitzende Control.</param>
        /// <param name="value">Das Command.</param>
        public static void SetSizeChangedCommand(UIElement obj, ICommand value)
        {
            obj.SetValue(SizeChangedCommandProperty, value);
        }

        /// <summary>
        /// WPF-Getter für das SizeChangedCommand.
        /// </summary>
        /// <param name="obj">Das besitzende Control.</param>
        /// <returns>Das Command.</returns>
        public static ICommand GetSizeChangedCommand(UIElement obj)
        {
            return (ICommand)obj.GetValue(SizeChangedCommandProperty);
        }

        static void SizeChangedCallBack(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            UIElement element = (UIElement)obj;

            _sizeChangedProperty = args.Property;

            if (element != null)
            {
                if (args.OldValue != null)
                {
                    element.AddHandler(_sizeChangedEvent, new RoutedEventHandler(SizeChangedEventHandler));
                }

                if (args.NewValue != null)
                {
                    element.AddHandler(_sizeChangedEvent, new RoutedEventHandler(SizeChangedEventHandler));
                }
            }
        }

        /// <summary>
        /// Event-Handler für das SizeChanged-Event des Expanders.
        /// </summary>
        /// <param name="sender">Das Command.</param>
        /// <param name="e">Argumente.</param>
        public static void SizeChangedEventHandler(object sender, RoutedEventArgs e)
        {
            DependencyObject obj = (DependencyObject)sender;

            if (obj != null && obj == e.OriginalSource)
            {
                ICommand? command = obj.GetValue(_sizeChangedProperty) as ICommand;

                if (command != null)
                {
                    if (command.CanExecute(e))
                    {
                        Expander exp = (Expander)e.OriginalSource;

                        if (exp != null)
                        {
                            if (sender is UIElement)
                            {
                                //(sender as UIElement).RaiseEvent(new RoutedEventArgs(LogicalTaskTreeControl.TreeExpandingStateChangedEvent));
                            }
                            command.Execute("sizeChanged");
                        }
                    }
                }
            }
            e.Handled = true;
        }

        #endregion SizeChanged Event
    }
}
