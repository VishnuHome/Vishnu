using System;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Vishnu.Interchange
{
    /// <summary>
    /// Hier werden Erweiterungen für bestehende Klassen gesammelt.
    /// Bisher implementiert: GetAbsolutePlacement für FrameworkElement.
    /// </summary>
    internal static class Extensions
    {
        /// <summary>
        /// Erweiterungsmethode für FrameworkElement: liefert Position und Maße
        /// des beinhaltenden Controls als Rect mit Left und Top jeweils als
        /// absoluten Bildschirmkoordinaten. 
        /// </summary>
        /// <returns>Position und Maße des beinhaltenden Controls als Rect mit Left und Top jeweils als absoluten Bildschirmkoordinaten.</returns>
        public static Rect GetAbsolutePlacement(this FrameworkElement element, bool relativeToScreen = true)
        {
            if (!element.Dispatcher.CheckAccess())
            {
                return (Rect)element.Dispatcher.Invoke(new Func<FrameworkElement, bool, Rect>(GetAbsolutePlacement), 
                    new object[] { element, relativeToScreen });
            }

            try
            {
                // 18.01.2024 Nagel/Chappy+ System.Windows.Point absolutePos = element.PointToScreen(new System.Windows.Point(0, 0));
                Matrix matrix = PresentationSource.FromVisual(element).CompositionTarget.TransformToDevice;
                System.Windows.Point topLeft = element.PointToScreen(new System.Windows.Point(0, 0));
                // System.Windows.Point scaledTopLeft = matrix.Transform(topLeft);
                System.Windows.Point scaledTopLeft =
                    new System.Windows.Point(topLeft.X / matrix.M11, topLeft.Y / matrix.M22);
                double scaledX = scaledTopLeft.X;
                double scaledY = scaledTopLeft.Y;
                System.Windows.Point absolutePos = new System.Windows.Point(scaledX, scaledY);
                // 18.01.2024 Nagel/Chappy-
                if (relativeToScreen)
                {
                    return new Rect(absolutePos.X, absolutePos.Y, element.ActualWidth, element.ActualHeight);
                }
                // 18.01.2024 Nagel+ TODO: der nachfolgende Teil (3 Zeilen) ist ungetestet
                // und muss höchstwahrscheinlich auch angepasst werden (siehe matrix oben).
                var posMW = Application.Current.MainWindow.PointToScreen(new System.Windows.Point(0, 0));
                absolutePos = new System.Windows.Point(absolutePos.X - posMW.X, absolutePos.Y - posMW.Y);
                return new Rect(absolutePos.X, absolutePos.Y, element.ActualWidth, element.ActualHeight);
            }
            catch (System.NullReferenceException)
            {
                // Kann auftreten, wenn bei pausiertem Tree zum Jobs-Tab und wieder zurück geschaltet wurde.
                // TODO: bessere Lösung finden.
                return VisualTreeHelper.GetContentBounds(element);
            }
            catch (Exception ex)
            when (ex is System.Reflection.TargetInvocationException || ex is System.InvalidOperationException)
            {
                return VisualTreeHelper.GetContentBounds(element);
            }
        }

    }
}
