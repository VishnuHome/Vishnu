using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;

namespace Vishnu.Interchange
{
    /// <summary>
    /// Hier werden Erweiterungen für bestehende Klassen gesammelt.
    /// Bisher implementiert: GetAbsolutePlacement für FrameworkElement.
    /// </summary>
    internal static class Extensions
    {
        /// <summary>
        /// Erweiterungsmethode für FrameworkElement: liefert die absolute Bildschirmposition des beinhaltenden Controls.
        /// </summary>
        /// <returns>Absolute Bildschirmposition der linken oberen Ecke des Parent-Controls.</returns>
        public static Rect GetAbsolutePlacement(this FrameworkElement element, bool relativeToScreen = true)
        {
            if (!element.Dispatcher.CheckAccess())
            {
                return (Rect)element.Dispatcher.Invoke(new Func<FrameworkElement, bool, Rect>(GetAbsolutePlacement), 
                    new object[] { element, relativeToScreen });
            }

            var absolutePos = element.PointToScreen(new System.Windows.Point(0, 0));
            if (relativeToScreen)
            {
                return new Rect(absolutePos.X, absolutePos.Y, element.ActualWidth, element.ActualHeight);
            }
            var posMW = Application.Current.MainWindow.PointToScreen(new System.Windows.Point(0, 0));
            absolutePos = new System.Windows.Point(absolutePos.X - posMW.X, absolutePos.Y - posMW.Y);
            return new Rect(absolutePos.X, absolutePos.Y, element.ActualWidth, element.ActualHeight);
        }

    }
}
