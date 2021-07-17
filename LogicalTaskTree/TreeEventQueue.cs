using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using NetEti.ApplicationControl;
using Vishnu.Interchange;

namespace LogicalTaskTree
{
    /// <summary>
    /// Statische Queue für TreeEvents. Ein TreeEvent für ein Ereignis und einen Knotentyp
    /// wird nur maximal einmal gleichzeitig in die Queue aufgenommen. Die Queue wird asynchron
    /// abgearbeitet, so dass eventuelle zirkuläre Event-Referenzen nicht zur System-Überlastung
    /// führen. Events, die gerade nicht in die Queue aufgenommen werden können, werden ersatzlos
    /// verworfen. Deshalb ist diese Queue auch nur für die Ablaufsteuerung, aber z.B. nicht für
    /// Logging geeignet.
    /// </summary>
    /// <remarks>
    /// File: TreeEventQueue.cs
    /// Autor: Erik Nagel
    ///
    /// 23.11.2013 Erik Nagel: erstellt
    /// </remarks>
    internal static class TreeEventQueue
    {
        #region public members

        public static void AddTreeEventTrigger(TreeEvent treeEvent, string nodeId, TreeEventTrigger treeEventTrigger)
        {
            string eventNode = treeEvent + ":" + nodeId;
            // Thread.Sleep(200); // DEBUG: steuert die Fehlerhäufigkeit
#if DEBUG
            //if (treeEvent.Name == "LogicalResultChanged" && treeEvent.SourceId == "SubJob1")
            //{
            //    InfoController.GetInfoPublisher().Publish(nodeId, String.Format($"TreeEventQueue.AddTreeEventTrigger {eventNode}"), InfoType.NoRegex);
            //}
#endif
            int counter = 1;
            bool done = false;
            while (!done)
            {
                ComposedQueueElement queueElement = new ComposedQueueElement()
                {
                    Key = eventNode + " (" + counter.ToString() + ")",
                    Source = treeEvent,
                    Trigger = treeEventTrigger
                };
                if (!TreeEventQueue._treeEventQueue.Contains(queueElement, new ComposedQueueElementCompare()))
                {
                    TreeEventQueue._treeEventQueue.Enqueue(queueElement);
                    if (!TreeEventQueue._isWorking)
                    {
                        TreeEventQueue._taskWorker.RunTask(new Action<TaskWorker>(TreeEventQueue.processQueue));
                    }
                    done = true;
                }
                else
                {
                    // InfoController.GetInfoPublisher().Publish(nodeId, String.Format($"TreeEventQueue.AddTreeEventTrigger {eventNode} - REFUSED({counter.ToString()})!"), InfoType.NoRegex);
                }
                counter++;
            }
        }

        #endregion public members

        #region private members

        private static ConcurrentQueue<ComposedQueueElement> _treeEventQueue;
        private static volatile bool _isWorking;
        private static TaskWorker _taskWorker;

        /// <summary>
        /// Statischer Konstruktor - inizialisiert die TreeEventQueue.
        /// </summary>
        static TreeEventQueue()
        {
            TreeEventQueue._isWorking = false;
            TreeEventQueue._taskWorker = new TaskWorker();
            TreeEventQueue._treeEventQueue = new ConcurrentQueue<ComposedQueueElement>();
        }

        private static void processQueue(TaskWorker taskWorker)
        {
            TreeEventQueue._isWorking = true;
            while (!TreeEventQueue._treeEventQueue.IsEmpty)
            {
                ComposedQueueElement composedQueueElement = null;
                if (TreeEventQueue._treeEventQueue.TryDequeue(out composedQueueElement))
                {
                    composedQueueElement.Trigger.OnTriggerFired(composedQueueElement.Source);
                }
            }
            TreeEventQueue._isWorking = false;
        }

        #endregion private members

    }

    /// <summary>
    /// Enthält zu einem Key (treeEvent + ":" + nodeId) einen TreeEventTrigger (Trigger).
    /// </summary>
    internal class ComposedQueueElement
    {
        #region public members

        /// <summary>
        /// treeEvent + ":" + nodeId.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Das auslösende TreeEvent.
        /// </summary>
        public TreeEvent Source { get; set; }

        /// <summary>
        /// TreeEventTrigger.
        /// </summary>
        public TreeEventTrigger Trigger { get; set; }

        #endregion public members

    }

    class ComposedQueueElementCompare : IEqualityComparer<ComposedQueueElement>
    {
        #region public members

        public bool Equals(ComposedQueueElement x, ComposedQueueElement y)
        {
            return x.Key == y.Key;
        }

        public int GetHashCode(ComposedQueueElement codeh)
        {
            return codeh.Key.GetHashCode();
        }

        #endregion public members

    }
}
