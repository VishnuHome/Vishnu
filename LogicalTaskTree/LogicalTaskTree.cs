using NetEti.Globals;
using NetEti.ObjectSerializer;
using System;
using System.IO;
using System.Xml;
using Vishnu.Interchange;

namespace LogicalTaskTree
{
    /// <summary>
    /// Enthält einen nach erweiterten logischen Ausdrücken augebauten,
    /// hierarchisch strukturierten Tree mit Blättern, in denen benutzerspezifische
    /// Verarbeitungsknoten dynamisch eingehängt werden können.
    /// Dient als Framework zur Prozess-Überwachung und -Steuerung.
    /// </summary>
    /// <remarks>
    /// File: LogicalTaskTree.cs
    /// Autor: Erik Nagel
    ///
    /// 18.08.2014 Erik Nagel: erstellt
    /// </remarks>
    public class LogicalTaskTree : IDisposable, IBusinessLogicRoot
    {
        #region public members

        /// <summary>
        /// Pro Tree eindeutiger ID-Zusatz.
        /// </summary>
        public static int TreeId;

        /// <summary>
        /// Zusätzliche Parameter, die für den gesamten Tree Gültigkeit haben oder null.
        /// </summary>
        public TreeParameters TreeParams { get; private set; }

        #region IDisposable Implementation

        private bool _disposed; // = false wird vom System vorbelegt;

        /// <summary>
        /// Öffentliche Methode zum Aufräumen.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Hier wird aufgeräumt: ruft für alle User-Elemente, die Disposable sind, Dispose() auf.
        /// </summary>
        /// <param name="disposing">Bei true wurde diese Methode von der öffentlichen Dispose-Methode
        /// aufgerufen; bei false vom internen Destruktor.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposed)
            {
                if (disposing)
                {
                    if (this.Tree != null)
                    {
                        try
                        {
                            this.Tree.UserBreak();
                        }
                        catch { }
                        this.Tree.Traverse(new Action<int, LogicalNode>((i, n)
                          =>
                          {
                              if (n.Logger != null)
                              {
                                  try
                                  {
                                      n.Logger.Dispose();
                                  }
                                  catch { };
                              }
                              if (n.Trigger != null && n.Trigger is IDisposable)
                              {
                                  try
                                  {
                                      (n.Trigger as IDisposable)?.Dispose();
                                  }
                                  catch { };
                              }
                              if (n is SingleNode)
                              {
                                  SingleNode? node = n as SingleNode;
                                  if (node?.Checker != null && node.Checker is IDisposable)
                                  {
                                      try
                                      {
                                          (node.Checker as IDisposable)?.Dispose();
                                      }
                                      catch { };
                                  }
                              }
                          }));
                    }
                }
                this._disposed = true;
            }
        }

        /// <summary>
        /// Finalizer: wird vom GarbageCollector aufgerufen.
        /// </summary>
        ~LogicalTaskTree()
        {
            this.Dispose(false);
        }

        #endregion IDisposable Implementation

        #region IBusinessLogicRoot Implementation

        /// <summary>
        /// Liefert die oberste JobList des Trees als IVishnuNode.
        /// </summary>
        /// <returns>Die oberste JobList des Trees.</returns>
        public IVishnuNode GetTopJobList()
        {
            return this.Tree;
        }

        /// <summary>
        /// Setzt die oberste JobList des Trees.
        /// Returnt die bisher oberste JobList.
        /// </summary>
        /// <param name="topJobList">Die neue oberste JobList des Trees.</param>
        /// <returns>Die bisher oberste JobList des Trees.</returns>
        public IVishnuNode SetTopJobList(IVishnuNode topJobList)
        {
            JobList oldTopJobList = this.Tree;
            this.Tree = (JobList)topJobList;
            return oldTopJobList;
        }

        #endregion IBusinessLogicRoot Implementation

        /// <summary>
        /// Liefert die RootJobList des LogicalTaskTrees inklusive Setter.
        /// </summary>
        public JobList Tree
        {
            get
            {
                return this._rootJobList;
            }
            set
            {
                this._rootJobList = value;
            }
        }

        /// <summary>
        /// Konstruktor - übernimmt die globalen Tree-Parameter und die Datenquelle für den Job.
        /// </summary>
        /// <param name="treeParams">Für den gesamten Tree gültige Parameter oder null.</param>
        /// <param name="jobProvider">Die Datenquelle für den Job</param>
        public LogicalTaskTree(TreeParameters treeParams, IJobProvider jobProvider)
        {
            treeParams.BusinessLogicRoot = this;
            this.TreeParams = treeParams;
            this._appSettings = GenericSingletonProvider.GetInstance<AppSettings>();
            this._rootJobList = new JobList(treeParams, jobProvider, this._appSettings.RootJobPackagePath);
            if (this._appSettings.DumpLoadedJobs)
            {
                string jsonJobLog = Path.Combine(this._appSettings.WorkingDirectory, "LoadedJobs.json");
                SerializationUtility.SaveToJsonFile<JobList>(this._rootJobList, jsonJobLog, includeFields: true);
            }
        }

        static LogicalTaskTree()
        {
            LogicalTaskTree.TreeId = 0; // Pro Tree eindeutiger ID-Zusatz.
        }

        #endregion public members

        #region private members

        private JobList _rootJobList;

        private AppSettings _appSettings;

        #endregion private members

    }
}
