using System;
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
    public class LogicalTaskTree : IDisposable
    {
        #region public members

        /// <summary>
        /// Pro Tree eindeutiger ID-Zusatz.
        /// </summary>
        public static int TreeId;

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
                                      (n.Trigger as IDisposable).Dispose();
                                  }
                                  catch { };
                              }
                              if (n is SingleNode)
                              {
                                  SingleNode node = n as SingleNode;
                                  if (node.Checker != null && node.Checker is IDisposable)
                                  {
                                      try
                                      {
                                          (node.Checker as IDisposable).Dispose();
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

        /// <summary>
        /// Der LogicalTaskTree - liefert die Wurzel des LogicalTaskTrees. 
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
            //treeParams.BusinessLogicRoot = this;
            this._rootJobList = new JobList(treeParams, jobProvider);
        }

        static LogicalTaskTree()
        {
            LogicalTaskTree.TreeId = 0; // Pro Tree eindeutiger ID-Zusatz.
        }

        #endregion public members

        #region private members

        JobList _rootJobList;

        #endregion private members

    }
}
