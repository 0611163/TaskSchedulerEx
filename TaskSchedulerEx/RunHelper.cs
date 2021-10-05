using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Utils
{
    /// <summary>
    /// 线程工具类
    /// </summary>
    public static class RunHelper
    {
        #region 变量属性事件

        #endregion

        #region 线程中执行
        /// <summary>
        /// 线程中执行
        /// </summary>
        public static Task Run(this TaskScheduler scheduler, Action<object> doWork, object arg = null, Action<Exception> errorAction = null)
        {
            return Task.Factory.StartNew((obj) =>
            {
                try
                {
                    doWork(obj);
                }
                catch (Exception ex)
                {
                    if (errorAction != null) errorAction(ex);
                    LogUtil.Error(ex, "RunHelper.Run错误");
                }
            }, arg, CancellationToken.None, TaskCreationOptions.None, scheduler);
        }
        #endregion

        #region 线程中执行
        /// <summary>
        /// 线程中执行
        /// </summary>
        public static Task Run(this TaskScheduler scheduler, Action doWork, Action<Exception> errorAction = null)
        {
            return Task.Factory.StartNew(() =>
            {
                try
                {
                    doWork();
                }
                catch (Exception ex)
                {
                    if (errorAction != null) errorAction(ex);
                    LogUtil.Error(ex, "RunHelper.Run错误");
                }
            }, CancellationToken.None, TaskCreationOptions.None, scheduler);
        }
        #endregion

        #region 线程中执行
        /// <summary>
        /// 线程中执行
        /// </summary>
        public static Task<T> Run<T>(this TaskScheduler scheduler, Func<object, T> doWork, object arg = null, Action<Exception> errorAction = null)
        {
            return Task.Factory.StartNew<T>((obj) =>
            {
                try
                {
                    return doWork(obj);
                }
                catch (Exception ex)
                {
                    if (errorAction != null) errorAction(ex);
                    LogUtil.Error(ex, "RunHelper.Run错误");
                    return default(T);
                }
            }, arg, CancellationToken.None, TaskCreationOptions.None, scheduler);
        }
        #endregion

        #region 线程中执行
        /// <summary>
        /// 线程中执行
        /// </summary>
        public static Task<T> Run<T>(this TaskScheduler scheduler, Func<T> doWork, Action<Exception> errorAction = null)
        {
            return Task.Factory.StartNew<T>(() =>
            {
                try
                {
                    return doWork();
                }
                catch (Exception ex)
                {
                    if (errorAction != null) errorAction(ex);
                    LogUtil.Error(ex, "RunHelper.Run错误");
                    return default(T);
                }
            }, CancellationToken.None, TaskCreationOptions.None, scheduler);
        }
        #endregion

        #region 线程中执行
        /// <summary>
        /// 线程中执行
        /// </summary>
        public static async Task<T> RunAsync<T>(this TaskScheduler scheduler, Func<object, T> doWork, object arg = null, Action<Exception> errorAction = null)
        {
            return await Task.Factory.StartNew<T>((obj) =>
            {
                try
                {
                    return doWork(obj);
                }
                catch (Exception ex)
                {
                    if (errorAction != null) errorAction(ex);
                    LogUtil.Error(ex, "RunHelper.RunAsync错误");
                    return default(T);
                }
            }, arg, CancellationToken.None, TaskCreationOptions.None, scheduler);
        }
        #endregion

        #region 线程中执行
        /// <summary>
        /// 线程中执行
        /// </summary>
        public static async Task<T> RunAsync<T>(this TaskScheduler scheduler, Func<T> doWork, Action<Exception> errorAction = null)
        {
            return await Task.Factory.StartNew<T>(() =>
            {
                try
                {
                    return doWork();
                }
                catch (Exception ex)
                {
                    if (errorAction != null) errorAction(ex);
                    LogUtil.Error(ex, "RunHelper.RunAsync错误");
                    return default(T);
                }
            }, CancellationToken.None, TaskCreationOptions.None, scheduler);
        }
        #endregion

        #region 线程中执行
        /// <summary>
        /// 线程中执行
        /// </summary>
        public static async Task RunAsync(this TaskScheduler scheduler, Action<object> doWork, object arg = null, Action<Exception> errorAction = null)
        {
            await Task.Factory.StartNew((obj) =>
            {
                try
                {
                    doWork(obj);
                }
                catch (Exception ex)
                {
                    if (errorAction != null) errorAction(ex);
                    LogUtil.Error(ex, "RunHelper.RunAsync错误");
                }
            }, arg, CancellationToken.None, TaskCreationOptions.None, scheduler);
        }
        #endregion

        #region 线程中执行
        /// <summary>
        /// 线程中执行
        /// </summary>
        public static async Task RunAsync(this TaskScheduler scheduler, Action doWork, Action<Exception> errorAction = null)
        {
            await Task.Factory.StartNew(() =>
            {
                try
                {
                    doWork();
                }
                catch (Exception ex)
                {
                    if (errorAction != null) errorAction(ex);
                    LogUtil.Error(ex, "RunHelper.RunAsync错误");
                }
            }, CancellationToken.None, TaskCreationOptions.None, scheduler);
        }
        #endregion

    }
}
