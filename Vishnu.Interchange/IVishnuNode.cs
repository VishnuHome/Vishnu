using NetEti.MVVMini;

namespace Vishnu.Interchange
{
    /// <summary>
    /// Bietet informativen Zugriff auf eine LogicalNode von Vishnu.
    /// </summary>
    /// <remarks>
    /// File: IVishnuNode
    /// Autor: Erik Nagel, NetEti
    ///
    /// 06.08.2016 Erik Nagel, NetEti: erstellt
    /// </remarks>
    public interface IVishnuNode : INotifyPropertiesChanged
    {
        /// <summary>
        /// Die eindeutige Kennung des Knotens.
        /// </summary>
        string IdInfo { get; }

        /// <summary>
        /// "Menschenfreundliche" Darstellung des Knotens.
        /// </summary>
        string NameInfo { get; }

        /// <summary>
        /// Der Pfad zum Knoten.
        /// </summary>
        string PathInfo { get; }

        /// <summary>
        /// Der Knotentyp:
        ///   None, NodeConnector, ValueModifier, Constant, Checker.
        /// </summary>
        NodeTypes TypeInfo { get; }

        /// <summary>
        /// Die Hierarchie-Ebene des Knotens.
        /// </summary>
        int LevelInfo { get; }

    }
}
