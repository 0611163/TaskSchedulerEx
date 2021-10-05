using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Utils
{
    /// <summary>
    /// 写日志类
    /// </summary>
    public class LogUtil
    {
        #region 字段
        private static string _path = null;

        private static Mutex _mutexDebug = new Mutex(false, "LogUtil.Mutex.Debug.252F8025254D4DAA8EFB7FFE177F13E0");
        private static Mutex _mutexInfo = new Mutex(false, "LogUtil.Mutex.Info.180740C3B1C44D428683D35F84F97E22");
        private static Mutex _mutexError = new Mutex(false, "LogUtil.Mutex.Error.81273C1400774A3B8310C2EC1C3AFFFF");

        private static ConcurrentDictionary<string, int> _dictIndex = new ConcurrentDictionary<string, int>();
        private static ConcurrentDictionary<string, long> _dictSize = new ConcurrentDictionary<string, long>();
        private static ConcurrentDictionary<string, FileStream> _dictStream = new ConcurrentDictionary<string, FileStream>();
        private static ConcurrentDictionary<string, StreamWriter> _dictWriter = new ConcurrentDictionary<string, StreamWriter>();

        private static ConcurrentDictionary<string, string> _dictPathFolders = new ConcurrentDictionary<string, string>();

        private static TaskSchedulerEx _scheduler = new TaskSchedulerEx(2, 2);

        private static int _fileSize = 10 * 1024 * 1024; //日志分隔文件大小
        #endregion

        #region 写文件
        /// <summary>
        /// 写文件
        /// </summary>
        private static void WriteFile(LogType logType, string log, string path)
        {
            try
            {
                FileStream fs = null;
                StreamWriter sw = null;

                if (!(_dictStream.TryGetValue(logType.ToString() + path, out fs) && _dictWriter.TryGetValue(logType.ToString() + path, out sw)))
                {
                    foreach (string key in _dictWriter.Keys)
                    {
                        if (key.StartsWith(logType.ToString()))
                        {
                            StreamWriter item;
                            _dictWriter.TryRemove(key, out item);
                            item.Close();
                        }
                    }

                    foreach (string key in _dictStream.Keys)
                    {
                        if (key.StartsWith(logType.ToString()))
                        {
                            FileStream item;
                            _dictStream.TryRemove(key, out item);
                            item.Close();
                        }
                    }

                    if (!Directory.Exists(Path.GetDirectoryName(path)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                    }

                    fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                    sw = new StreamWriter(fs);
                    _dictWriter.TryAdd(logType.ToString() + path, sw);
                    _dictStream.TryAdd(logType.ToString() + path, fs);
                }

                fs.Seek(0, SeekOrigin.End);
                sw.WriteLine(log);
                sw.Flush();
                fs.Flush();
            }
            catch (Exception ex)
            {
                string str = ex.Message;
            }
        }
        #endregion

        #region 生成日志文件路径
        /// <summary>
        /// 生成日志文件路径
        /// </summary>
        private static string CreateLogPath(LogType logType, string log)
        {
            try
            {
                if (_path == null)
                {
                    UriBuilder uri = new UriBuilder(Assembly.GetExecutingAssembly().CodeBase);
                    _path = Path.GetDirectoryName(Uri.UnescapeDataString(uri.Path));
                }

                string pathFolder = Path.Combine(_path, "Log\\" + logType.ToString() + "\\");
                if (!_dictPathFolders.ContainsKey(pathFolder))
                {
                    if (!Directory.Exists(Path.GetDirectoryName(pathFolder)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(pathFolder));
                    }
                    _dictPathFolders.TryAdd(pathFolder, pathFolder);
                }

                int currentIndex;
                long size;
                string strNow = DateTime.Now.ToString("yyyyMMdd");
                string strKey = pathFolder + strNow;
                if (!(_dictIndex.TryGetValue(strKey, out currentIndex) && _dictSize.TryGetValue(strKey, out size)))
                {
                    _dictIndex.Clear();
                    _dictSize.Clear();

                    GetIndexAndSize(pathFolder, strNow, out currentIndex, out size);
                    if (size >= _fileSize) currentIndex++;
                    _dictIndex.TryAdd(strKey, currentIndex);
                    _dictSize.TryAdd(strKey, size);
                }

                int index = _dictIndex[strKey];
                string logPath = Path.Combine(pathFolder, strNow + (index == 1 ? "" : "_" + index.ToString()) + ".txt");

                _dictSize[strKey] += Encoding.UTF8.GetByteCount(log);
                if (_dictSize[strKey] > _fileSize)
                {
                    _dictIndex[strKey]++;
                    _dictSize[strKey] = 0;
                }

                return logPath;
            }
            catch (Exception ex)
            {
                string str = ex.Message;
                return null;
            }
        }
        #endregion

        #region 拼接日志内容
        /// <summary>
        /// 拼接日志内容
        /// </summary>
        private static string CreateLogString(LogType logType, string log)
        {
            return string.Format(@"{0} {1} {2}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), ("[" + logType.ToString() + "]").PadRight(7, ' '), log);
        }
        #endregion

        #region 获取初始Index和Size
        /// <summary>
        /// 获取初始Index和Size
        /// </summary>
        private static void GetIndexAndSize(string pathFolder, string strNow, out int index, out long size)
        {
            index = 1;
            size = 0;
            Regex regex = new Regex(strNow + "_*(\\d*).txt");
            string[] fileArr = Directory.GetFiles(pathFolder);
            string currentFile = null;
            foreach (string file in fileArr)
            {
                Match match = regex.Match(file);
                if (match.Success)
                {
                    string str = match.Groups[1].Value;
                    if (!string.IsNullOrWhiteSpace(str))
                    {
                        int temp = Convert.ToInt32(str);
                        if (temp > index)
                        {
                            index = temp;
                            currentFile = file;
                        }
                    }
                    else
                    {
                        index = 1;
                        currentFile = file;
                    }
                }
            }

            if (currentFile != null)
            {
                FileInfo fileInfo = new FileInfo(currentFile);
                size = fileInfo.Length;
            }
        }
        #endregion

        #region 写调试日志
        /// <summary>
        /// 写调试日志
        /// </summary>
        public static Task Debug(string log)
        {
            return Task.Factory.StartNew(() =>
            {
                try
                {
                    _mutexDebug.WaitOne();

                    log = CreateLogString(LogType.Debug, log);
                    string path = CreateLogPath(LogType.Debug, log);
                    WriteFile(LogType.Debug, log, path);
                }
                catch (Exception ex)
                {
                    string str = ex.Message;
                }
                finally
                {
                    _mutexDebug.ReleaseMutex();
                }
            }, CancellationToken.None, TaskCreationOptions.None, _scheduler);
        }
        #endregion

        #region 写错误日志
        public static Task Error(Exception ex, string log = null)
        {
            return Error(string.IsNullOrEmpty(log) ? ex.Message + "\r\n" + ex.StackTrace : (log + "：") + ex.Message + "\r\n" + ex.StackTrace);
        }

        /// <summary>
        /// 写错误日志
        /// </summary>
        public static Task Error(string log)
        {
            return Task.Factory.StartNew(() =>
            {
                try
                {
                    _mutexError.WaitOne();

                    log = CreateLogString(LogType.Error, log);
                    string path = CreateLogPath(LogType.Error, log);
                    WriteFile(LogType.Error, log, path);
                }
                catch (Exception ex)
                {
                    string str = ex.Message;
                }
                finally
                {
                    _mutexError.ReleaseMutex();
                }
            }, CancellationToken.None, TaskCreationOptions.None, _scheduler);
        }
        #endregion

        #region 写操作日志
        /// <summary>
        /// 写操作日志
        /// </summary>
        public static Task Log(string log)
        {
            return Task.Factory.StartNew(() =>
            {
                try
                {
                    _mutexInfo.WaitOne();

                    log = CreateLogString(LogType.Info, log);
                    string path = CreateLogPath(LogType.Info, log);
                    WriteFile(LogType.Info, log, path);
                }
                catch (Exception ex)
                {
                    string str = ex.Message;
                }
                finally
                {
                    _mutexInfo.ReleaseMutex();
                }
            }, CancellationToken.None, TaskCreationOptions.None, _scheduler);
        }
        #endregion

    }

    #region 日志类型
    /// <summary>
    /// 日志类型
    /// </summary>
    public enum LogType
    {
        Debug,

        Info,

        Error
    }
    #endregion

}
