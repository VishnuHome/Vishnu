using System;
using System.Reflection;
using System.IO;
using System.Linq;
using NetEti.ApplicationControl;
using NetEti.Globals;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Vishnu.Interchange
{
    /// <summary>
    /// Stellt Methoden für das dynamische Laden von
    /// Assemblies und das Instanziieren darin enthaltener Klassen
    /// zur Verfügung.
    /// </summary>
    /// <remarks>
    /// File: VishnuAssemblyLoader.cs
    /// Autor: Erik Nagel
    ///
    /// 10.04.2013 Erik Nagel: erstellt
    /// </remarks>
    public class VishnuAssemblyLoader
    {
        #region public members

        /// <summary>
        /// Singleton-Provider - übernimmt Pfade zu Verzeichnissen, in denen zusätzlich
        /// nach Assemblies gesucht werden soll.
        /// </summary>
        /// <param name="assemblyDirectories">Eine Liste von Verzeichnissen, in denen
        /// nach der Assembly gesucht werden soll.</param>
        /// <returns>Singleton-Instanz von AssemblyLoader</returns>
        public static VishnuAssemblyLoader GetAssemblyLoader(List<string> assemblyDirectories)
        {
            VishnuAssemblyLoader loader = NestedInstance.itsMe;
            loader._assemblyDirectories = assemblyDirectories;
            return (loader);
        }

        /// <summary>
        /// Singleton-Provider - übernimmt Pfade zu Verzeichnissen, in denen zusätzlich
        /// nach Assemblies gesucht werden soll.
        /// </summary>
        /// <returns>Singleton-Instanz von AssemblyLoader</returns>
        public static VishnuAssemblyLoader GetAssemblyLoader()
        {
            return VishnuAssemblyLoader.GetAssemblyLoader(
                GenericSingletonProvider.GetInstance<AppSettings>().AssemblyDirectories);
        }

        /// <summary>
        /// Löscht den internen Assembly-Cache, sodass alle Assemblies beim nächsten Aufruf neu geladen werden.
        /// </summary>
        public static void ClearCache()
        {
            try
            {
                ThreadLocker.LockNameGlobal("AssemblyLoader");
                VishnuAssemblyLoader.AssembliesToBeReloadedNext.Clear();
            }
            finally
            {
                ThreadLocker.UnlockNameGlobal("AssemblyLoader");
            }
        }

        /// <summary>
        /// Registriert Pfade von dynamisch zu ladenden Assemblies, die beim nächsten Ladevorgang auf jeden
        /// Fall neu von der Festplatte und nicht aus einem eventuell gecachtem Image geladen werden sollen.
        /// </summary>
        /// <param name="assemblyPathName">Der Pfad der Assembly, die beim nächsten Ladevorgang nicht aus dem Cache genommen werden soll.</param>
        public static void RegisterAssemblyPathForForcedNextReloading(string assemblyPathName)
        {
            VishnuAssemblyLoader.AssembliesToBeReloadedNext.TryAdd(assemblyPathName, true);
        }

        /// <summary>
        /// Lädt ein Objekt vom übergebenen Typ aus der angegebenen Assembly dynamisch.
        /// Alle von der angegebenen Assembly referenzierten Assemblies werden zusätzlich
        /// auch in assemblyDirectories gesucht.
        /// </summary>
        /// <param name="assemblyPathName">Die Assembly, die das zu ladende Objekt publiziert.</param>
        /// <param name="objectType">Der Typ des aus der Assembly zu instanzierenden Objekts</param>
        /// <param name="force">Optional - bei true wird die Assembly nicht aus dem Cache genomen, default: false</param>
        /// <returns>Instanz aus der übergebenen Assembly vom übergebenen Typ oder null</returns>
        public object DynamicLoadObjectOfTypeFromAssembly(string assemblyPathName, Type objectType, bool force = false)
        {
            if (VishnuAssemblyLoader.AssembliesToBeReloadedNext.ContainsKey(assemblyPathName))
            {
                force = true;
                bool success;
                VishnuAssemblyLoader.AssembliesToBeReloadedNext.TryRemove(assemblyPathName, out success);
            }
            Exception lastException = null;
            try
            {
                ThreadLocker.LockNameGlobal("AssemblyLoader");
                object candidate = null;
                Assembly slave = dynamicLoadAssembly(assemblyPathName, false, force);
                if (slave != null)
                {
                    Type[] exports = slave.GetExportedTypes();
                    foreach (Type type in exports)
                    {
                        if (objectType.IsAssignableFrom(type))
                        {
                            try
                            {
                                candidate = Activator.CreateInstance(type);
                            }
                            catch (Exception ex)
                            {
                                lastException = ex;
                            }
                        }
                        if (candidate != null)
                        {
                            return candidate;
                        }
                    }
                }
                else
                {
                    lastException = new Exception(String.Format("Die Assembly {0} konnte nicht geladen werden.", assemblyPathName));
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
            }
            finally
            {
                ThreadLocker.UnlockNameGlobal("AssemblyLoader");
            }
            String msg = lastException.Message;
            if (lastException.InnerException != null)
            {
                msg += Environment.NewLine + lastException.InnerException.Message;
            }
            throw new TypeLoadException(msg);
        }

        #endregion public members

        #region private members

        private List<string> _assemblyDirectories;

        static ConcurrentDictionary<string, KeyValuePair<Assembly, int>> LoadedAssembliesByNameAndChangeCount;

        static ConcurrentDictionary<string, bool> AssembliesToBeReloadedNext;

        const int MAXASSEMBLYRELOADS = 0; // 2;

        /// <summary>
        /// Statischer Konstruktor.
        /// </summary>
        static VishnuAssemblyLoader()
        {
            LoadedAssembliesByNameAndChangeCount = new ConcurrentDictionary<string, KeyValuePair<Assembly, int>>();
            AssembliesToBeReloadedNext = new ConcurrentDictionary<string, bool>();
        }

        /// <summary>
        /// Lädt die Assembly vom übergebenen Pfad.
        /// </summary>
        /// <param name="slavePathName">Pfad der zu ladenden Assembly.</param>
        /// <param name="quiet">Keine Meldung bei Misserfolg.</param>
        /// <param name="force">Optional - bei true wird die Assembly nicht aus dem Cache genomen, default: false</param>
        /// <returns>Geladene Assembly oder null</returns>
        private Assembly dynamicLoadAssembly(string slavePathName, bool quiet, bool force = false)
        {
            Assembly candidate = null;
            foreach (string assemblyDirectory in (new List<string> { "" }).Union(this._assemblyDirectories))
            {
                string dllPath = Path.Combine(assemblyDirectory, slavePathName).Replace(@"Plugin\Plugin", "Plugin");
                if (!dllPath.ToLower().EndsWith(".dll"))
                {
                    dllPath += ".dll";
                }
                candidate = this.directLoadAssembly(dllPath, quiet, force);
                if (candidate != null)
                {
                    break;
                }
            }
            if (candidate == null)
            {
                if (!quiet)
                {
                    InfoController.Say("Could not embed " + slavePathName);
                }
            }
            return candidate;
        }

        private Assembly directLoadAssembly(string dllPath, bool quiet, bool force = false)
        {
            Assembly candidate = null;
            string currentDir = Environment.CurrentDirectory;
            string currentDir2 = Directory.GetCurrentDirectory();
            if (File.Exists(dllPath))
            {
                candidate = null;
                // Gibt die Dll im Filesystem direkt nach Laden wieder frei.
                string dllName = Path.GetFileName(dllPath);
                if (!force && VishnuAssemblyLoader.LoadedAssembliesByNameAndChangeCount.ContainsKey(dllName)
                  && (VishnuAssemblyLoader.LoadedAssembliesByNameAndChangeCount[dllName].Value) > MAXASSEMBLYRELOADS)
                {
                    candidate = (Assembly)VishnuAssemblyLoader.LoadedAssembliesByNameAndChangeCount[dllName].Key;
                }
                else
                {
                    candidate = System.Reflection.Assembly.Load(System.IO.File.ReadAllBytes(dllPath));
                    int loadedCounter = 0;
                    if (force)
                    {
                        KeyValuePair<Assembly, int> success;
                        VishnuAssemblyLoader.LoadedAssembliesByNameAndChangeCount.TryRemove(dllName, out success);
                    }
                    if (!VishnuAssemblyLoader.LoadedAssembliesByNameAndChangeCount.ContainsKey(dllName))
                    {
                        VishnuAssemblyLoader.LoadedAssembliesByNameAndChangeCount.TryAdd(dllName, new KeyValuePair<Assembly, int>(candidate, 0));
                    }
                    else
                    {
                        loadedCounter = VishnuAssemblyLoader.LoadedAssembliesByNameAndChangeCount[dllName].Value;
                    }
                    VishnuAssemblyLoader.LoadedAssembliesByNameAndChangeCount[dllName] = new KeyValuePair<Assembly, int>(candidate, ++loadedCounter);
                }
                // Blockt die Dll im Filesystem nach Laden solange, wie die Applikation läuft.
                // candidate = Assembly.LoadFrom(slavePathName);
            }
            return candidate;
        }

        /// <summary>
        /// Privater Konstruktor.
        /// </summary>
        private VishnuAssemblyLoader()
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.AssemblyResolve += new ResolveEventHandler(myResolveEventHandler);
        }

        private Assembly myResolveEventHandler(object sender, ResolveEventArgs args)
        {
            return dynamicLoadAssembly(args.Name.Split(',')[0], true);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "due to lazyness of the singleton instance")]
        private class NestedInstance
        {
            internal static readonly VishnuAssemblyLoader itsMe;

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline", Justification = "due to lazyness of the singleton instance")]
            static NestedInstance()
            {
                itsMe = new VishnuAssemblyLoader();
            }
        }

        #endregion private members

    }
}
