using System.Collections.Concurrent;
using Vishnu.Interchange;

namespace LogicalTaskTree
{
    /// <summary>
    /// Ein Thread-safes Dictionary mit Results; Keys sind die jeweiligen NodeIDs.
    /// </summary>
    internal class ResultList : ConcurrentDictionary<string, Result?> { }
}
