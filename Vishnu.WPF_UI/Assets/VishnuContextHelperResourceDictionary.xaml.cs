using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Data;
using System.Windows.Input;

namespace ResourceDictionaryCodeBehind
{
    /// <summary>
    /// CodeBehind für VishnuContextHelperResourceDictionary.xaml.
    /// Steuert das Verhalten der ToolTips.
    /// </summary>
    public partial class CustomResources : ResourceDictionary
    {
        void whenToolTipOpens(object sender, RoutedEventArgs e)
        {
            if (sender.GetType().FullName?.Equals("System.Windows.Controls.ToolTip") == true)
            {
                ToolTip toolTip = (ToolTip)sender;
                toolTip.Focusable = true;
                toolTip.Focus();

                var copyToClipboardBinding = new KeyBinding { Key = Key.C, Modifiers = ModifierKeys.Control };
                BindingOperations.SetBinding(
                    copyToClipboardBinding,
                    InputBinding.CommandProperty,
                    new Binding { Path = new PropertyPath("CopyToolTipInfoToClipboard") }
                );
                toolTip.InputBindings.Add(copyToClipboardBinding);
            }
        }

        void whenToolTipCloses(object sender, RoutedEventArgs e)
        {
            if (sender.GetType().FullName?.Equals("System.Windows.Controls.ToolTip") == true)
            {
                ToolTip toolTip = (ToolTip)sender;
                toolTip.InputBindings.Clear();
            }
        }
    }
}