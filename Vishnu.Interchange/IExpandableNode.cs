using System;
using System.Windows.Controls;

namespace Vishnu.Interchange
{
    /// <summary>
    /// Definiert die notwendigen Eigenschaften eines expandierbaren, respektive
    /// zusammenklappbaren Knotens in einem Tree (aus Sicht der Klasse ConfigurationManager).
    /// </summary>
    /// <remarks>
    /// File: IExpandableNode.cs
    /// Autor: Erik Nagel
    ///
    /// 10.02.2018 Erik Nagel: erstellt
    /// </remarks>
    public interface IExpandableNode
    {
        /// <summary>
        /// Die Kennung des zugehörigen LogicalTaskTree-Knotens
        /// für die UI verfügbar gemacht.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Der Name des zugehörigen LogicalTaskTree-Knotens
        /// für die UI verfügbar gemacht.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Der eindeutige Pfad des zugehörigen LogicalTaskTree-Knotens.
        /// </summary>
        string Path { get; }

        /// <summary>
        /// True, wenn der TreeView-Knoten, welcher mit diesem Knoten
        /// assoziiert ist, ausgeklappt ist.
        /// </summary>
        bool IsExpanded { get; set; }

        /// <summary>
        /// Definiert, ob die Kind-Elemente dieses Knotens
        /// horizontal oder vertikal angeordnet werden sollen.
        /// </summary>
        Orientation ChildOrientation { get; }

        /// <summary>
        /// Geht rekursiv durch den Baum und ruft für jeden Knoten die Action auf.
        /// </summary>
        /// <param name="callback">Der für jeden Knoten aufzurufende Callback vom Typ Func&lt;int, IExpandableNode, object, object&gt;.</param>
        /// <returns>Das oberste UserObjekt für den Tree oder null.</returns>
        object? Traverse(Func<int, IExpandableNode, object?, object?> callback);

    }
}
