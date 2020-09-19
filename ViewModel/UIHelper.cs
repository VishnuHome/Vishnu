using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Vishnu.Interchange;

namespace Vishnu.ViewModel
{
    /// <summary>
    /// Bietet statische Routinen zur Durchsuchung des VisualTrees und des LogicalTrees.
    /// </summary>
    /// <remarks>
    /// Autor: Erik Nagel
    ///
    /// 25.03.2020 Erik Nagel: erstellt
    /// </remarks>
    public static class UIHelper
    {
        /// <summary>
        /// Sucht im LogicalTree vom FrameworkElement element
        /// nach dem ersten Kindelement vom Typ T.
        /// </summary>
        /// <typeparam name="T">Typ des gesuchten Kindelements.</typeparam>
        /// <param name="element">FrameworkElement, dessen LogicalTree durchsucht werden soll.</param>
        /// <returns>Kindelement vom Typ T oder null.</returns>
        public static T FindFirstLogicalChildOfType<T>(FrameworkElement element) where T : FrameworkElement
        {
            return FindFirstLogicalChildOfTypeAndName<T>(element, null);
        }

        /// <summary>
        /// Sucht im LogicalTree vom FrameworkElement element
        /// nach dem ersten Kindelement vom Typ T.
        /// </summary>
        /// <typeparam name="T">Typ des gesuchten Kindelements.</typeparam>
        /// <param name="element">FrameworkElement, dessen LogicalTree durchsucht werden soll.</param>
        /// <param name="name">Child.Name oder null.</param>
        /// <returns>Kindelement vom Typ T oder null.</returns>
        public static T FindFirstLogicalChildOfTypeAndName<T>(FrameworkElement element, string name) where T : FrameworkElement
        {
            foreach (object child in LogicalTreeHelper.GetChildren(element))
            {
                if ((child is T) && (String.IsNullOrEmpty(name) || (child as FrameworkElement).Name == name))
                    return child as T;

                if (child is FrameworkElement)
                {
                    T childFound = FindFirstLogicalChildOfTypeAndName<T>(child as FrameworkElement, name);
                    if (childFound != null)
                        return childFound;
                }
            }
            return null;
        }

        /// <summary>
        /// Sucht im VisualTree vom FrameworkElement element
        /// nach dem ersten Kindelement vom Typ T.
        /// </summary>
        /// <typeparam name="T">Typ des gesuchten Kindelements.</typeparam>
        /// <param name="element">FrameworkElement, dessen VisualTree durchsucht werden soll.</param>
        /// <returns>Kindelement vom Typ T oder null.</returns>
        public static T FindFirstVisualChildOfType<T>(FrameworkElement element) where T : FrameworkElement
        {
            return FindFirstVisualChildOfTypeAndName<T>(element, null);
        }

        /// <summary>
        /// Sucht im VisualTree vom FrameworkElement element
        /// nach dem ersten Kindelement vom Typ T mit Name == name.
        /// </summary>
        /// <typeparam name="T">Typ des gesuchten Kindelements.</typeparam>
        /// <param name="element">FrameworkElement, dessen VisualTree durchsucht werden soll.</param>
        /// <param name="name">Name des gesuchten Kindelements oder null.</param>
        /// <returns>Kindelement vom Typ T (und wenn angegeben, mit Name == name) oder null.</returns>
        public static T FindFirstVisualChildOfTypeAndName<T>(FrameworkElement element, string name) where T : FrameworkElement
        {
            return findFirstChild<T>(element, name, new List<FrameworkElement>());
        }

        private static T findFirstChild<T>(FrameworkElement element, string name, List<FrameworkElement> alreadyFound) where T : FrameworkElement
        {
            int childrenCount = VisualTreeHelper.GetChildrenCount(element);
            FrameworkElement[] children = new FrameworkElement[childrenCount];

            for (int i = 0; i < childrenCount; i++)
            {
                FrameworkElement child = VisualTreeHelper.GetChild(element, i) as FrameworkElement;
                if (child != null)
                {
                    if (!alreadyFound.Contains(child))
                    {
                        alreadyFound.Add(child);
                    }
                    else
                    {
                        return null; // Rekursion
                    }
                    children[i] = child;
                    if (child is T)
                    {
                        string attachedName = null;
                        attachedName = (string)child.GetValue(DynamicUserControlBase.AttachedNameProperty);
                        string childName = String.IsNullOrEmpty(attachedName) ? child.Name : attachedName;
                        if (String.IsNullOrEmpty(name) || childName == name)
                        {
                            return (T)child;
                        }
                    }
                }
            }

            for (int i = 0; i < childrenCount; i++)
                if (children[i] != null)
                {
                    T subChild = findFirstChild<T>(children[i], name, alreadyFound);
                    if (subChild != null)
                        return subChild;
                }

            return null;
        }

    }
}
