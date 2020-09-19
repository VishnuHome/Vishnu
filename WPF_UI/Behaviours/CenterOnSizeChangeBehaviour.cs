using System.Windows;

namespace Vishnu.WPF_UI.Behaviours
{
    /// <summary>
    /// Zentriert das Fenster auf dem Screen, wenn sich dessen Größe geändert hat.
    /// </summary>
    /// <remarks>
    /// File: CenterOnSizeChangeBehaviour
    /// Autor: Erik - geklaut von Oger
    ///
    /// 28.02.2014 Erik Nagel: erstellt
    /// </remarks>
    public static class CenterOnSizeChangeBehaviour
    {
        /// <summary>
        /// Attached Property für das SizeChanged-Event eines Controls.
        /// </summary>
        /// 
        /// <AttachedPropertyComments>
        /// <summary>
        /// Attached Property (bool). Bei true soll sich das Control
        /// auf dem Bildschirm zentrieren.
        /// </summary>
        /// <value>Default: false.</value>
        /// </AttachedPropertyComments>
        public static readonly DependencyProperty CenterOnSizeChangeProperty =
          DependencyProperty.RegisterAttached
            (
              "CenterOnSizeChange",
              typeof(bool),
              typeof(CenterOnSizeChangeBehaviour),
              new UIPropertyMetadata(false, OnCenterOnSizeChangePropertyChanged)
            );

        /// <summary>
        /// WPF-Getter für das ExpandedCommand.
        /// </summary>
        /// <param name="obj">Das besitzende Control.</param>
        /// <returns>Das Command.</returns>
        public static bool GetCenterOnSizeChange(DependencyObject obj)
        {
            return (bool)obj.GetValue(CenterOnSizeChangeProperty);
        }

        /// <summary>
        /// WPF-Setter für das ExpandedCommand.
        /// </summary>
        /// <param name="obj">Das besitzende Control.</param>
        /// <param name="value">Das Command.</param>
        public static void SetCenterOnSizeChange(DependencyObject obj, bool value)
        {
            obj.SetValue(CenterOnSizeChangeProperty, value);
        }

        private static void OnCenterOnSizeChangePropertyChanged(DependencyObject dpo, DependencyPropertyChangedEventArgs args)
        {
            System.Windows.Window window = dpo as System.Windows.Window;
            if (window != null)
            {
                if ((bool)args.NewValue)
                {
                    window.SizeChanged += OnWindowSizeChanged;
                }
                else
                {
                    window.SizeChanged -= OnWindowSizeChanged;
                }
            }
        }

        private static void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            System.Windows.Window window = (System.Windows.Window)sender;
            if (window.Left >= SystemParameters.WorkArea.Left && window.Left + window.ActualWidth < SystemParameters.WorkArea.Right
                || window.Left > SystemParameters.WorkArea.Left && window.Left + window.ActualWidth <= SystemParameters.WorkArea.Right)
            {
                window.WindowStartupLocation = WindowStartupLocation.Manual;
                window.Left = (SystemParameters.WorkArea.Width - window.ActualWidth) / 2 + SystemParameters.WorkArea.Left;
            }
            if (window.Top >= SystemParameters.WorkArea.Top && window.Top + window.ActualHeight < SystemParameters.WorkArea.Bottom
                || window.Top > SystemParameters.WorkArea.Top && window.Top + window.ActualHeight <= SystemParameters.WorkArea.Bottom)
            {
                window.WindowStartupLocation = WindowStartupLocation.Manual;
                window.Top = (SystemParameters.WorkArea.Height - window.ActualHeight) / 2 + SystemParameters.WorkArea.Top;
            }
        }
    }
}
