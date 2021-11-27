using System;
using System.Windows.Controls;
using System.Windows;
using NetEti.ApplicationControl;
using System.Threading;
using NetEti.Globals;
using NetEti.MultiScreen;

namespace Vishnu.Interchange
{
    /// <summary>
    /// Basisklasse für die dynamische Einbindung von UserControls
    /// in externen Dlls.
    /// </summary>
    /// <remarks>
    /// File: DynamicUserControlBase
    /// Autor: Erik Nagel
    ///
    /// 01.08.2014 Erik Nagel: erstellt
    /// </remarks>
    public class DynamicUserControlBase : UserControl
    {
        /// <summary>
        /// Wird ausgelöst, wenn das dynamisch geladene Control vollständig gezeichnet wurde.
        /// Hier wird das User-Event deklariert und registriert.
        /// </summary>
        public static readonly RoutedEvent DynamicUserControl_ContentRenderedEvent =
            EventManager.RegisterRoutedEvent("DynamicUserControl_ContentRenderedEvent", RoutingStrategy.Direct,
            typeof(RoutedEventHandler), typeof(DynamicUserControlBase));

        /// <summary>
        /// Standard Handler für das Einhängen des WPF Event-Systems
        /// in das registrierte Event DynamicUserControl_ContentRenderedEvent.
        /// </summary>
        public event RoutedEventHandler DynamicUserControl_ContentRendered
        {
            add { AddHandler(DynamicUserControl_ContentRenderedEvent, value); }
            remove { RemoveHandler(DynamicUserControl_ContentRenderedEvent, value); }
        }

        /// <summary>
        /// Zusätzlicher Name, über den das Control im VisualTree gesucht
        /// werden kann (wichtig bei UserControls).
        /// </summary>
        /// 
        /// <AttachedPropertyComments>
        /// <summary>
        /// Attached Property (String) für einen zusätzlichen Namen des UserControls.
        /// </summary>
        /// </AttachedPropertyComments>
        public static readonly DependencyProperty AttachedNameProperty =
           DependencyProperty.RegisterAttached("AttachedName",
           typeof(string),
           typeof(DynamicUserControlBase));

        /// <summary>
        /// WPF-Getter für die AttachedNameProperty.
        /// </summary>
        /// <param name="obj">Das besitzende Control.</param>
        /// <returns>Zusätzlicher Name des Controls, z.B. für die Suche im VisualTree.</returns>
        public static string GetAttachedName(DependencyObject obj)
        {
            return (string)obj.GetValue(AttachedNameProperty);
        }

        /// <summary>
        /// WPF-Setter für die AttachedNameProperty.
        /// </summary>
        /// <param name="obj">Das besitzende Control.</param>
        /// <param name="value">Zusätzlicher Name des Controls, z.B. für die Suche im VisualTree.</param>
        public static void SetAttachedName(
           DependencyObject obj,
           string value)
        {
            obj.SetValue(AttachedNameProperty, value);
        }

        /// <summary>
        /// Konstruktor - hängt sich in das LoadedEvent ein.
        /// </summary>
        public DynamicUserControlBase()
        {
            this.Loaded += new System.Windows.RoutedEventHandler(this.contentControl_Loaded);
            // Ist ein absoluter Performance-Killer, deshalb deaktiviert!
            // this.LayoutUpdated += DynamicUserControlBase_LayoutUpdated;
        }

        /// <summary>
        /// Absolute Bildschirmposition der Mitte des beinhaltenden Controls.
        /// </summary>
        /// <returns>Absolute Bildschirmposition der linken oberen Ecke des Parent-Controls.</returns>
        public Point GetParentViewAbsoluteScreenPosition()
        {
            Point absoluteScreenPosition = new Point(0, 0);
            try
            {
                this.Dispatcher.Invoke(new Action(() => { absoluteScreenPosition = this.PointToScreen(absoluteScreenPosition); }));
            }
            catch (InvalidOperationException) { /* Dieses Visual-Objekt ist nicht mit einer "PresentationSource" verbunden. */ }
            absoluteScreenPosition.X += this.ActualWidth / 2;
            absoluteScreenPosition.Y += this.ActualHeight / 2;
            ScreenInfo lastScreen = ScreenInfo.GetLastActualScreenInfo();
            double marginX = 10;
            if (marginX > lastScreen.Bounds.Width / 2 - 5)
            {
                marginX = lastScreen.Bounds.Width / 2 - 5;
            }
            double marginY = 10;
            if (marginY > lastScreen.Bounds.Height / 2 - 5)
            {
                marginY = lastScreen.Bounds.Height / 2 - 5;
            }
            Point screenPosition = lastScreen.GetNextPointWithinActualScreenCoordinates(absoluteScreenPosition, marginX, marginY);
            screenPosition.X -= this.ActualWidth / 2;
            screenPosition.Y -= this.ActualHeight / 2;
            return screenPosition;
        }

        // Ist ein absoluter Performance-Killer, deshalb deaktiviert!
        //private void DynamicUserControlBase_LayoutUpdated(object sender, EventArgs e)
        //{
        //  try
        //  {
        //    Point newPosition = this.PointToScreen(new Point(0, 0));
        //    if (this._lastPosition != newPosition)
        //    {
        //      this._lastPosition = newPosition;
        //      if (this.DataContext != null)
        //      {
        //        (this.DataContext as IVishnuViewModel).ParentViewAbsoluteScreenPosition
        //          = newPosition;
        //      }
        //    }
        //  }
        //  catch (InvalidOperationException) { /* Dieses Visual-Objekt ist nicht mit einer "PresentationSource" verbunden. */ }
        //}

        /// <summary>
        /// Löst das OnDynamicUserControl_ContentRendered-Ereignis aus.
        /// </summary>
        protected virtual void OnDynamicUserControl_ContentRendered()
        {
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                (this.DataContext as IVishnuViewModel).ParentView = this;
            }
            this.RaiseEvent(new RoutedEventArgs(DynamicUserControlBase.DynamicUserControl_ContentRenderedEvent));
        }

        private void contentControl_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action<ContentControl>(this.waitForContentRendered)
              , System.Windows.Threading.DispatcherPriority.ApplicationIdle, new object[] { e.Source as ContentControl });
        }

        private void waitForContentRendered(ContentControl contentControl)
        {
            int emergencyHalt = 0;
            while (this.ActualWidth + this.ActualHeight == 0)
            {
                Thread.Sleep(1);               
                InfoController.Say("DynamicUserControlBase.waitForContentRendered(1)");

                Thread.Sleep(10);

                if (emergencyHalt++ > 1000)
                {
                    string msg = String.Format("Vishnu: möglicher Endlosloop in DynamicUserControlBase.waitForContentRendered(1)."
                      + "\nEventuell leeres UserControl?");
                    if (GenericSingletonProvider.GetInstance<AppSettings>().DebugMode)
                    {
                        throw new ApplicationException(msg);
                    }
                    else
                    {
                        InfoController.Say(msg);
                        break;
                    }
                }
            }
            emergencyHalt = 0;
            double width;
            double height;
            do
            {
                width = this.ActualWidth;
                height = this.ActualHeight;

                InfoController.Say("DynamicUserControlBase.waitForContentRendered(2)");
                Thread.Sleep(10);

                Thread.Sleep(10);
                if (emergencyHalt++ > 1000)
                {
                    string msg = String.Format("Vishnu: möglicher Endlosloop in DynamicUserControlBase.waitForContentRendered(2).");
                    if (GenericSingletonProvider.GetInstance<AppSettings>().DebugMode)
                    {
                        throw new ApplicationException(msg);
                    }
                    else
                    {
                        InfoController.Say(msg);
                        break;
                    }
                }
            } while (this.ActualWidth != width || this.ActualHeight != height);

            this.OnDynamicUserControl_ContentRendered();
        }
    }
}
