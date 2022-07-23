using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Utils
{
    /// <summary>
    /// TaskScheduler扩展
    /// 每个实例都是独立线程池
    /// </summary>
    public class TaskSchedulerEx : TaskScheduler, IDisposable
    {
        #region 变量属性事件
        private ConcurrentQueue<Task> _tasks = new ConcurrentQueue<Task>();
        private int _coreThreadCount = 0;
        private int _maxThreadCount = 0;
        private int _auxiliaryThreadTimeOut = 20000; //辅助线程释放时间
        private int _activeThreadCount = 0;
        private System.Timers.Timer _timer;
        private object _lockCreateTimer = new object();
        private bool _run = true;
        private Semaphore _sem = null;
        private int _semMaxCount = int.MaxValue; //可以同时授予的信号量的最大请求数
        private int _semCount = 0; //可用信号量请求数
        private int _runCount = 0; //正在执行的和等待执行的任务数量

        /// <summary>
        /// 活跃线程数
        /// </summary>
        public int ActiveThreadCount
        {
            get { return _activeThreadCount; }
        }

        /// <summary>
        /// 核心线程数
        /// </summary>
        public int CoreThreadCount
        {
            get { return _coreThreadCount; }
        }

        /// <summary>
        /// 最大线程数
        /// </summary>
        public int MaxThreadCount
        {
            get { return _maxThreadCount; }
        }
        #endregion

        #region 构造函数
        /// <summary>
        /// TaskScheduler扩展
        /// 每个实例都是独立线程池
        /// </summary>
        /// <param name="coreThreadCount">核心线程数(大于或等于0，不宜过大)(如果是一次性使用，则设置为0比较合适)</param>
        /// <param name="maxThreadCount">最大线程数</param>
        public TaskSchedulerEx(int coreThreadCount = 10, int maxThreadCount = 20)
        {
            _sem = new Semaphore(0, _semMaxCount);
            _maxThreadCount = maxThreadCount;
            CreateCoreThreads(coreThreadCount);
        }
        #endregion

        #region override GetScheduledTasks
        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return _tasks;
        }
        #endregion

        #region override TryExecuteTaskInline
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return false;
        }
        #endregion

        #region override QueueTask
        protected override void QueueTask(Task task)
        {
            _tasks.Enqueue(task);

            while (_semCount >= _semMaxCount) //信号量已满，等待
            {
                Thread.Sleep(1);
            }

            _sem.Release();
            Interlocked.Increment(ref _semCount);

            Interlocked.Increment(ref _runCount);
            if (_activeThreadCount < _maxThreadCount && _activeThreadCount < _runCount)
            {
                CreateThread();
            }
        }
        #endregion

        #region 资源释放
        /// <summary>
        /// 资源释放
        /// 队列中尚未执行的任务不再执行
        /// </summary>
        public void Dispose()
        {
            _run = false;

            if (_timer != null)
            {
                _timer.Stop();
                _timer.Dispose();
                _timer = null;
            }

            while (_activeThreadCount > 0)
            {
                _sem.Release();
                Interlocked.Increment(ref _semCount);
            }

            _sem.Dispose();
        }
        #endregion

        #region 创建核心线程池
        /// <summary>
        /// 创建核心线程池
        /// </summary>
        private void CreateCoreThreads(int? coreThreadCount = null)
        {
            if (coreThreadCount != null) _coreThreadCount = coreThreadCount.Value;

            for (int i = 0; i < _coreThreadCount; i++)
            {
                Interlocked.Increment(ref _activeThreadCount);
                Thread thread = null;
                thread = new Thread(new ThreadStart(() =>
                {
                    Task task;
                    while (_run)
                    {
                        if (_tasks.TryDequeue(out task))
                        {
                            TryExecuteTask(task);
                            Interlocked.Decrement(ref _runCount);
                        }
                        else
                        {
                            _sem.WaitOne();
                            Interlocked.Decrement(ref _semCount);
                        }
                    }
                    Interlocked.Decrement(ref _activeThreadCount);
                }));
                thread.IsBackground = true;
                thread.Start();
            }
        }
        #endregion

        #region 创建辅助线程
        /// <summary>
        /// 创建辅助线程
        /// </summary>
        private void CreateThread()
        {
            Interlocked.Increment(ref _activeThreadCount);
            Thread thread = null;
            thread = new Thread(new ThreadStart(() =>
            {
                Task task;
                DateTime dt = DateTime.Now;
                while (_run && DateTime.Now.Subtract(dt).TotalMilliseconds < _auxiliaryThreadTimeOut)
                {
                    if (_tasks.TryDequeue(out task))
                    {
                        TryExecuteTask(task);
                        Interlocked.Decrement(ref _runCount);
                        dt = DateTime.Now;
                    }
                    else
                    {
                        _sem.WaitOne(_auxiliaryThreadTimeOut);
                        Interlocked.Decrement(ref _semCount);
                    }
                }
                Interlocked.Decrement(ref _activeThreadCount);
            }));
            thread.IsBackground = true;
            thread.Start();
        }
        #endregion

        #region 全部取消
        /// <summary>
        /// 全部取消
        /// 取消队列中尚未执行的任务
        /// </summary>
        public void CancelAll()
        {
            Task tempTask;
            while (_tasks.TryDequeue(out tempTask))
            {
                Interlocked.Decrement(ref _runCount);
            }
        }
        #endregion

    }
}
