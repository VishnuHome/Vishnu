using NetEti.ApplicationControl;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;

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
        #region find childs

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
                {
                    return child as T;
                }

                if (child is FrameworkElement)
                {
                    T childFound = FindFirstLogicalChildOfTypeAndName<T>(child as FrameworkElement, name);
                    if (childFound != null)
                    {
                        return childFound;
                    }
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

        /// <summary>
        /// Sucht im VisualTree vom FrameworkElement element nach dem ersten Kindeskind-Element vom Typ T,
        /// welches auf ein Kindelement vom Typ 'predecessorType' mit dem Namen 'predecessorName' folgt.
        /// </summary>
        /// <typeparam name="T">Typ des gesuchten Kindeskind-Elements.</typeparam>
        /// <typeparam name="U">Typ des Kindelements, dessen Kind gesucht wird.</typeparam>
        /// <param name="element">FrameworkElement, dessen VisualTree durchsucht werden soll.</param>
        /// <param name="predecessorName">Name des dem gesuchten Elements vorausgehenden Elementes.</param>
        /// <returns></returns>
        public static T FindFirstVisualChildOfTypeAfterVisualChildOfTypeAndName<T, U>(FrameworkElement element, string predecessorName) where T : FrameworkElement where U : FrameworkElement
        {

            FrameworkElement predecessor = FindFirstVisualChildOfTypeAndName<U>(element, predecessorName);
            if (predecessor != null)
            {
                return FindFirstVisualChildOfType<T>(predecessor);
            }
            return null;
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
                    // InfoController.Say(String.Format($"#TreeHelper# {child.GetType()}, '{child.Name??""}'"));
                    if (!alreadyFound.Contains(child))
                    {
                        alreadyFound.Add(child);
                    }
                    else
                    {
                        return null;
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
            {
                if (children[i] != null)
                {
                    T subChild = findFirstChild<T>(children[i], name, alreadyFound); // Rekursion
                    if (subChild != null)
                    {
                        return subChild;
                    }
                }
            }
            return null;
        }

        #endregion find childs

        #region find parents

        /// <summary>
        /// Sucht im VisualTree vom FrameworkElement element
        /// aufwärts nach dem ersten Elternelement vom Typ T.
        /// </summary>
        /// <typeparam name="T">Typ des gesuchten Elternelements.</typeparam>
        /// <param name="element">FrameworkElement, dessen VisualTree durchsucht werden soll.</param>
        /// <returns>Elternelement vom Typ T oder null.</returns>
        public static T FindFirstVisualParentOfType<T>(FrameworkElement element) where T : FrameworkElement
        {
            return FindFirstVisualParentOfTypeAndName<T>(element, null);
        }

        /// <summary>
        /// Sucht im VisualTree vom FrameworkElement element aufwärts
        /// nach dem ersten Elternelement vom Typ T mit Name == name.
        /// </summary>
        /// <typeparam name="T">Typ des gesuchten Elternelements.</typeparam>
        /// <param name="element">FrameworkElement, dessen VisualTree durchsucht werden soll.</param>
        /// <param name="name">Name des gesuchten Elternelements oder null.</param>
        /// <returns>Elternelement vom Typ T (und wenn angegeben, mit Name == name) oder null.</returns>
        public static T FindFirstVisualParentOfTypeAndName<T>(FrameworkElement element, string name) where T : FrameworkElement
        {
            FrameworkElement parent = (FrameworkElement)GetNearestParent(element);
            while (parent != null)
            {
                if (parent is T)
                {
                    string attachedName = (string)parent.GetValue(DynamicUserControlBase.AttachedNameProperty);
                    string parentName = String.IsNullOrEmpty(attachedName) ? parent.Name : attachedName;
                    if (String.IsNullOrEmpty(name) || parentName == name)
                    {
                        return (T)parent;
                    }
                }
                parent = (FrameworkElement)GetNearestParent(parent);
            }

            return null;
        }

        private static DependencyObject GetNearestParent(FrameworkElement child)
        {
            // Folgende Ansätze sind nicht ausreichend, da sie entweder an der DLL-Grenze
            // beim Übergang aus dynamisch geladenen UserControls zum übergeordneten Control
            // stoppen, oder aber in Resourcen-Templates definierte Stufen überspringen:
            //     DependencyObject parent = child.Parent;
            //     DependencyObject logicalParent = LogicalTreeHelper.GetParent(child);
            //     DependencyObject templateParent = child.TemplatedParent;

            // Das geht auch nicht, VisualParent ist protected:
            //     parentCandidate = parent.VisualParent;
            
            // Das ist endlich erfolgreich:
            PropertyInfo propertyInfo = child.GetType().GetProperty("VisualParent",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );
            return propertyInfo.GetValue(child) as DependencyObject;
        }

        #endregion find parents

    }
}
