using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace RMS.Prototype.NGFM
{
    public class TaskManager
    {
        private ConcurrentQueue<Task> taskPool;
        private CancellationTokenSource cts;

        private static object lockObj = new object();
        private static ConcurrentDictionary<string, TaskManager> CacheTaskManagers;
        private static TaskFactory factory;
        private static bool isInitialized;

        public static int NumberThreads { get { return Process.GetCurrentProcess().Threads.Count; } }
        public static int NumberTasks { get { return GetNumberOfRunningTasksAndRemoveCompleted(); } }

        // Constructor 
        private TaskManager()
        {
            taskPool = new ConcurrentQueue<Task>();
            cts = new CancellationTokenSource();
        }

        /// <summary>
        /// Initialization (only once)
        /// </summary>
        /// <param name="maxDegreeOfParallelism">It could be defined only once (by default = 2).</param>
        /// <returns>is initialized</returns>
        public static bool Initialize(int maxDegreeOfParallelism)
        {
            lock (lockObj)
            {
                if (isInitialized)
                    return isInitialized;

                var scheduler = new LimitedConcurrencyLevelTaskScheduler(maxDegreeOfParallelism);
                factory = new TaskFactory(scheduler);
                CacheTaskManagers = new ConcurrentDictionary<string, TaskManager>();
                isInitialized = true;
            }

            return isInitialized;
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public static void Dispose()
        {
            lock (lockObj)
            {
                isInitialized = false;

                if (CacheTaskManagers != null)
                    CacheTaskManagers.Clear();

                CacheTaskManagers = null;
                factory = null;
            }
        }

        // Cancel all tasks of given instance of Task Manager
        private void Cancel()
        {
            if (cts != null)
                cts.Cancel();
            cts = new CancellationTokenSource();
        }

        /// <summary>
        /// Start new Task or continue with processing Task
        /// </summary>
        /// <param name="contractId">Contract Id</param>
        /// <param name="eventId">Event Id</param>
        /// <param name="action">What to do...</param>
        /// <param name="obj">action's argument</param>
        /// <returns>Task</returns>
        public static Task Start(long contractId, int eventId, Action<object> action, object obj)
        {
            if (!isInitialized) return null;

            string key = string.Format("{0},{1}", contractId, eventId);

            var tp = new TaskManager();

            if (tp == CacheTaskManagers.GetOrAdd(key, tp))
            { // Create new Task

                Task task = null;

                lock (lockObj)
                    task = factory.StartNew(() => action(obj), tp.cts.Token);

                if (task != null)
                    tp.taskPool.Enqueue(task);

                return task;
            }
            else // Continue with running
            {
                if (CacheTaskManagers.TryGetValue(key, out tp))
                {
                    Task task = null;

                    if (tp.taskPool.Count > 0)
                    {
                        Task task1 = tp.taskPool.Last();
                        task = task1.ContinueWith((antecendent) => action(obj), tp.cts.Token);
                    }
                    else
                    {
                        lock (lockObj)
                        {
                            task = factory.StartNew(() => action(obj), tp.cts.Token);
                        }
                    }

                    tp.taskPool.Enqueue(task);

                    tp.RemoveCompletedTasksFromQueue();

                    return task;
                }
            }

            return null;
        }

        public static void WaitAll(int eventId, params long[] contractIDs)
        {
            if(contractIDs.Length == 0)
            {
                string key = string.Format(",{0}", eventId);

                Task.WaitAll(CacheTaskManagers.Where(kv => kv.Key.EndsWith(key) && kv.Value.taskPool.Count > 0)
                    .Select(kv => kv.Value.taskPool.Last()).ToArray());
            }
            else
            {
                var keys = contractIDs.Select(contractId => string.Format("{0},{1}", contractId, eventId)).ToArray();

                Task.WaitAll(CacheTaskManagers.Where(kv => keys.Contains(kv.Key) && kv.Value.taskPool.Count > 0)
                    .Select(kv => kv.Value.taskPool.Last()).ToArray());
            }
        }

        public static void WaitAll()
        {
            Task.WaitAll(CacheTaskManagers.Where(kv => kv.Value.taskPool.Count > 0)
                    .Select(kv => kv.Value.taskPool.Last()).ToArray());
        }

        private void RemoveCompletedTasksFromQueue()
        {
            Task t = null;
            while (taskPool.TryPeek(out t) && IsTaskCompleted(t))
                taskPool.TryDequeue(out t); 
        }

        /// <summary>
        /// Cancel all sub-Tasks for given contractIDs
        /// </summary>
        /// <param name="contractIDs">if empty, cancel all Tasks of all contracts</param>
        public static void CancelAll(params long[] contractIDs)
        {
            if (!isInitialized) return;

            if (contractIDs.Length == 0)
            {
                foreach (var tm in CacheTaskManagers.Values)
                    tm.Cancel();

                CacheTaskManagers.Clear();
            }
            else
            {
                var rmvs = new List<string>();

                foreach (string key in contractIDs.Select(id => string.Format("{0},", id)))
                {
                    foreach (var p in CacheTaskManagers.Where(kv => kv.Key.StartsWith(key)))
                    {
                        p.Value.Cancel();
                        rmvs.Add(p.Key);
                    }
                }

                TaskManager rm;
                foreach (string key in rmvs)
                    CacheTaskManagers.TryRemove(key, out rm);
            }
        }

        public static bool IsTaskCompleted(Task task)
        {
            return (task == null
                || task.Status == TaskStatus.Canceled
                || task.Status == TaskStatus.Faulted
                || task.Status == TaskStatus.RanToCompletion);
        }

        /// <summary>
        /// Get the number of running Tasks and remove completed tasks from Cache.
        /// </summary>
        /// <returns></returns>
        public static int GetNumberOfRunningTasksAndRemoveCompleted()
        {
            int nRunningTasks = 0;
            if (isInitialized && CacheTaskManagers.Count > 0)
            {
                foreach (var tp in CacheTaskManagers.Values)
                {
                    tp.RemoveCompletedTasksFromQueue();
                    nRunningTasks += tp.taskPool.Count;
                }
            }

            return nRunningTasks;
        }
    }

    /// <summary>
    /// Provides a task scheduler that ensures a maximum concurrency level while
    /// running on top of the thread pool. 
    /// Example: http://msdn.microsoft.com/en-us/library/ee789351(v=vs.110).aspx
    /// </summary>
    public class LimitedConcurrencyLevelTaskScheduler : TaskScheduler
    {
        // Indicates whether the current thread is processing work items.
        [ThreadStatic]
        private static bool _currentThreadIsProcessingItems;

        // The list of tasks to be executed  
        private readonly LinkedList<Task> _tasks = new LinkedList<Task>(); // protected by lock(_tasks) 

        // The maximum concurrency level allowed by this scheduler.  
        private readonly int _maxDegreeOfParallelism;

        // Indicates whether the scheduler is currently processing work items.  
        private int _delegatesQueuedOrRunning = 0;

        // Creates a new instance with the specified degree of parallelism.  
        public LimitedConcurrencyLevelTaskScheduler(int maxDegreeOfParallelism)
        {
            if (maxDegreeOfParallelism < 1) throw new ArgumentOutOfRangeException("maxDegreeOfParallelism");
            _maxDegreeOfParallelism = maxDegreeOfParallelism;
        }

        // Queues a task to the scheduler.  
        protected sealed override void QueueTask(Task task)
        {
            // Add the task to the list of tasks to be processed.  If there aren't enough  
            // delegates currently queued or running to process tasks, schedule another.  
            lock (_tasks)
            {
                _tasks.AddLast(task);
                if (_delegatesQueuedOrRunning < _maxDegreeOfParallelism)
                {
                    ++_delegatesQueuedOrRunning;
                    NotifyThreadPoolOfPendingWork();
                }
            }
        }

        // Inform the ThreadPool that there's work to be executed for this scheduler.  
        private void NotifyThreadPoolOfPendingWork()
        {
            ThreadPool.UnsafeQueueUserWorkItem(_ =>
            {
                // Note that the current thread is now processing work items. 
                // This is necessary to enable inlining of tasks into this thread.
                _currentThreadIsProcessingItems = true;
                try
                {
                    // Process all available items in the queue. 
                    while (true)
                    {
                        Task item;
                        lock (_tasks)
                        {
                            // When there are no more items to be processed, 
                            // note that we're done processing, and get out. 
                            if (_tasks.Count == 0)
                            {
                                --_delegatesQueuedOrRunning;
                                break;
                            }

                            // Get the next item from the queue
                            item = _tasks.First.Value;
                            _tasks.RemoveFirst();
                        }

                        // Execute the task we pulled out of the queue 
                        base.TryExecuteTask(item);
                    }
                }
                // We're done processing items on the current thread 
                finally { _currentThreadIsProcessingItems = false; }
            }, null);
        }

        // Attempts to execute the specified task on the current thread.  
        protected sealed override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            // If this thread isn't already processing a task, we don't support inlining 
            if (!_currentThreadIsProcessingItems) return false;

            // If the task was previously queued, remove it from the queue 
            if (taskWasPreviouslyQueued)
                // Try to run the task.  
                if (TryDequeue(task))
                    return base.TryExecuteTask(task);
                else
                    return false;
            else
                return base.TryExecuteTask(task);
        }

        // Attempt to remove a previously scheduled task from the scheduler.  
        protected sealed override bool TryDequeue(Task task)
        {
            lock (_tasks) return _tasks.Remove(task);
        }

        // Gets the maximum concurrency level supported by this scheduler.  
        public sealed override int MaximumConcurrencyLevel { get { return _maxDegreeOfParallelism; } }

        // Gets an enumerable of the tasks currently scheduled on this scheduler.  
        protected sealed override IEnumerable<Task> GetScheduledTasks()
        {
            bool lockTaken = false;
            try
            {
                Monitor.TryEnter(_tasks, ref lockTaken);
                if (lockTaken) return _tasks;
                else throw new NotSupportedException();
            }
            finally
            {
                if (lockTaken) Monitor.Exit(_tasks);
            }
        }
    }
}
