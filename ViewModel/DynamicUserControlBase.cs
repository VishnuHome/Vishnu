using System;
using System.Windows.Controls;
using System.Windows;
using NetEti.ApplicationControl;
using System.Threading;
using NetEti.Globals;
using NetEti.MultiScreen;
using System.Windows.Input;
using Vishnu.Interchange;
using System.Diagnostics;

namespace Vishnu.ViewModel
{
    /// <summary>
    /// Basisklasse für die dynamische Einbindung von UserControls
    /// in externen Dlls.
    /// </summary>
    /// <remarks>
    /// File: DynamicUserControlBase
    /// Autor: Erik Nagel
    ///
    /// 01.08.2014 Erik Nagel: erstellt.
    /// 11.09.2022 Erik Nagel: Behandlung des ContextMenu implementiert;
    ///                        virtuelle Methode GetUserResultViewModel eingebaut, abstract geht nicht wegen Path2UserControlBase.
    /// </remarks>
    public class DynamicUserControlBase : UserControl, IDisposable
    {
        /// <summary>
        /// Abstrakte Definition von GetUserResultViewModel: muss für konkrete Anwendung überschrieben werden.
        /// </summary>
        /// <param name="vishnuViewModel">Interface für die ViewModels von dynamischen User-Controls.</param>
        /// <returns>Hier: null, muss überschrieben werden.</returns>
        protected virtual DynamicUserControlViewModelBase GetUserResultViewModel(IVishnuViewModel vishnuViewModel) { return null; }

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
        /// Übernimmt den aktuellen, spezifischen DataContext für Vishnu als IVishnuViewModel.
        /// </summary>
        public DynamicUserControlViewModelBase UserResultViewModel { get; set; }

        /// <summary>
        /// Konstruktor - hängt sich in das LoadedEvent ein.
        /// </summary>
        public DynamicUserControlBase()
        {
            this.Loaded += new System.Windows.RoutedEventHandler(this.contentControl_Loaded);
            // Ist ein absoluter Performance-Killer, deshalb deaktiviert!
            // this.LayoutUpdated += DynamicUserControlBase_LayoutUpdated;

            // Die nachfolgenden zwei Zeilen sorgen dafür, dass beim Drücken der rechten Maustaste
            // nicht automatisch das Context-Menü öffnet, sondern vorher noch geprüft werden kann,
            // ob gleichzeitig Umschalt oder Strg gedrückt wurde und somit gar nicht das Context-Menü
            // gemeint war, sondern eine der Vishnu-Layoutfunktionen.
            this.MouseRightButtonUp += DynamicUserControlBase_MouseRightButtonUp;
            ContextMenuService.SetIsEnabled(this, false);
            //this.ContextMenu = (ContextMenu)this.Resources["cmContextMenu"];
        }

        #region IDisposable Implementation

        private bool _disposed; // = false wird vom System vorbelegt;
        private Border _contextMenuBorder;

        /// <summary>
        /// Öffentliche Methode zum Aufräumen.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Hier wird aufgeräumt: ruft für alle User-Elemente, die Disposable sind, Dispose() auf.
        /// </summary>
        /// <param name="disposing">Bei true wurde diese Methode von der öffentlichen Dispose-Methode
        /// aufgerufen; bei false vom internen Destruktor.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposed)
            {
                if (disposing)
                {
                    this.DoDispose();
                }
                this._disposed = true;
            }
        }

        /// <summary>
        /// Hier werden die beim disposing notwendigen Aktionen durchgeführt.
        /// </summary>
        protected virtual void DoDispose()
        {
            if (this._contextMenuBorder != null)
            {
                this._contextMenuBorder.MouseLeave -= Border_MouseLeave;
            }
            this.MouseRightButtonUp -= DynamicUserControlBase_MouseRightButtonUp;
        }

        /// <summary>
        /// Finalizer: wird vom GarbageCollector aufgerufen.
        /// </summary>
        ~DynamicUserControlBase()
        {
            this.Dispose(false);
        }

        #endregion IDisposable Implementation

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
                object dataContext = this.DataContext;
                if (dataContext is IVishnuViewModel)
                {
                    (dataContext as IVishnuViewModel).ParentView = this;
                }
                else
                {
                    if (dataContext is MainWindowViewModel)
                    {
                        dataContext = ((MainWindowViewModel)dataContext).JobGroupsVM[0];
                    }
                }
                if (dataContext is IVishnuRenderWatcher)
                {
                    (dataContext as IVishnuRenderWatcher).UserControlContentRendered(this);
                }
            }
            try
            {
                this.RaiseEvent(new RoutedEventArgs(DynamicUserControlBase.DynamicUserControl_ContentRenderedEvent));
            }
            catch
            {
                // Hier keine Exception im UI-Thread zulassen, da die Fehlermeldung sofort wieder verschwinden würde.
                // Die Exception wird später in LogicalNode.OnNodeProgressFinished sowieso gemeldet.
            }
        }

        private void contentControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= contentControl_Loaded;

            if (this.DataContext != null && !(this.DataContext is Vishnu.ViewModel.MainWindowViewModel))
            {
                this.UserResultViewModel = GetUserResultViewModel((IVishnuViewModel)this.DataContext);
                ((IVishnuViewModel)this.DataContext).UserDataContext = this.UserResultViewModel;

                // Zwingt die Controls nochmal zur Neuberechnung ihrer maximalen Größe. Ohne diese Neuberechnung
                // werden unter Umständen einzelne Zeilen des umgebenden, dynamischen Grids nicht dargestellt.
                this.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            }

            ContextMenu contextMenu = (ContextMenu)this.Resources["cmContextMenu"];
            contextMenu.Loaded += ContextMenu_Loaded;
            contextMenu.DataContext = this.DataContext;
            this.ContextMenu = contextMenu;

            Dispatcher.BeginInvoke(new Action<ContentControl>(this.waitForContentRendered),
                System.Windows.Threading.DispatcherPriority.Background /* geht nicht: Normal, ApplicationIdle */, new object[] { e.Source as ContentControl });
        }

        private void ContextMenu_Loaded(object sender, RoutedEventArgs e)
        {
            this.ContextMenu.Loaded -= ContextMenu_Loaded;
            this._contextMenuBorder = UIHelper.FindFirstVisualChildOfTypeAndName<Border>(this.ContextMenu, null);
            if (this._contextMenuBorder != null)
            {
                this._contextMenuBorder.MouseLeave += Border_MouseLeave;
            }
        }

        private void Border_MouseLeave(object sender, MouseEventArgs e)
        {
            if (this.ContextMenu != null)
            {
                this.ContextMenu.IsOpen = false;
            }
        }

        // Die nachfolgende Routine prüft, ob beim Drücken der rechten Maustaste das Context-Menü
        // geöffnet werden soll oder ob gleichzeitig Umschalt oder Strg gedrückt wurde und somit
        // gar nicht das Context-Menü gemeint war, sondern eine der Vishnu-Layoutfunktionen.
        private void DynamicUserControlBase_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == 0 && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
            {
                if (this.ContextMenu != null)
                {
                    this.ContextMenu.IsOpen = true;
                }
            }
        }

        private void waitForContentRendered(ContentControl contentControl)
        {
            if (!this.IsVisible)
            {
                return;
            }
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

            // this.ContextMenu = (ContextMenu)this.Resources["cmContextMenu"];

            this.OnDynamicUserControl_ContentRendered();
        }
    }
}
