using System;
using System.Threading;
using System.Collections.Concurrent;

namespace LogicalTaskTree
{
    /// <summary>
    /// Verwaltet ein statisches Dictionary von Namen und Lock-Objekten.
    /// Sperrt und entsperrt das einem Namen zugeordnete Objekt global.
    /// </summary>
    /// <remarks>
    /// File: ThreadLocker.cs
    /// Autor: Erik Nagel
    ///
    /// 28.09.2013 Erik Nagel: erstellt
    /// </remarks>
    public static class ThreadLocker
    {
        #region public members

        /// <summary>
        /// Sperrt ein dem übergebenen Namen intern zugeordnetes Objekt.
        /// Der Eintrag mit Namen und Objekt wird ggf. vorher neu erzeugt.
        /// </summary>
        /// <param name="name">Ein Name, der über ein zugeordnetes Objekt global gesperrt werden soll.</param>
        public static void LockNameGlobal(string name)
        {
            if (!ThreadLocker._lockers.ContainsKey(name))
            {
                ThreadLocker._lockers.AddOrUpdate(name, new object(), new Func<string, object, object>((a, b) => { return new object(); }));
            }
            bool lockWasTaken = false;
            Monitor.Enter(ThreadLocker._lockers[name], ref lockWasTaken);
        }

        /// <summary>
        /// Entsperrt ein dem übergebenen Namen intern zugeordnetes Objekt.
        /// </summary>
        /// <param name="name">Ein Name, dessen zugeordnetes Objekt global entsperrt werden soll.</param>
        public static void UnlockNameGlobal(string name)
        {
            if (!ThreadLocker._lockers.ContainsKey(name))
            {
                throw new ArgumentNullException(String.Format("ThreadLocker.UnlockNameGlobal: Der Name {0} existiert nicht.", name));
            }
            Monitor.Exit(ThreadLocker._lockers[name]);
        }

        #endregion public members

        #region private members

        private static ConcurrentDictionary<string, object> _lockers;

        static ThreadLocker()
        {
            ThreadLocker._lockers = new ConcurrentDictionary<string, object>();
        }

        #endregion private members

    }
}
