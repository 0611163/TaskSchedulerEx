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
    internal class LogWriter
    {
        #region 字段属性

        private LogType _logType;

        private string _basePath;

        private int _fileSize = 10 * 1024 * 1024; //日志分隔文件大小

        private LogStream _currentStream = new LogStream();

        private string _dateFormat = "yyyyMMdd"; //日志文件名日期格式化

        private string _rootFolder = "Log"; //日志文件夹名称

        private object _lockWriter = new object();

        #endregion

        #region LogWriter
        public LogWriter(LogType logType)
        {
            _logType = logType;

            Init();
        }
        #endregion

        #region Init
        /// <summary>
        /// 初始化
        /// </summary>
        private void Init()
        {
            //初始化 _basePath
            InitBasePath();

            //创建目录
            CreateLogDir();

            //更新日志写入流
            UpdateCurrentStream();
        }
        #endregion

        #region 初始化 _basePath
        /// <summary>
        /// 初始化 _basePath
        /// </summary>
        private void InitBasePath()
        {
            UriBuilder uri = new UriBuilder(Assembly.GetExecutingAssembly().CodeBase);
            _basePath = Path.GetDirectoryName(Uri.UnescapeDataString(uri.Path));
        }
        #endregion

        #region 初始化 _currentArchiveIndex
        /// <summary>
        /// 初始化 _currentArchiveIndex
        /// </summary>
        private void InitCurrentArchiveIndex()
        {
            Regex regex = new Regex(_currentStream.CurrentDateStr + "_*(\\d*).txt");
            string[] fileArr = Directory.GetFiles(_currentStream.CurrentLogFileDir, _currentStream.CurrentDateStr + "*");
            foreach (string file in fileArr)
            {
                Match match = regex.Match(file);
                if (match.Success)
                {
                    string str = match.Groups[1].Value;
                    if (!string.IsNullOrWhiteSpace(str))
                    {
                        int temp = Convert.ToInt32(str);
                        if (temp > _currentStream.CurrentArchiveIndex)
                        {
                            _currentStream.CurrentArchiveIndex = temp;
                        }
                    }
                    else
                    {
                        _currentStream.CurrentArchiveIndex = -1;
                    }
                }
            }
        }
        #endregion

        #region 初始化 _currentFileSize
        /// <summary>
        /// 初始化 _currentFileSize
        /// </summary>
        private void InitCurrentFileSize()
        {
            FileInfo fileInfo = new FileInfo(_currentStream.CurrentLogFilePath);
            _currentStream.CurrentFileSize = fileInfo.Length;
        }
        #endregion

        #region CreateLogDir()
        /// <summary>
        /// 创建日志目录
        /// </summary>
        private void CreateLogDir()
        {
            string logDir = Path.Combine(_basePath, _rootFolder + "\\" + _logType.ToString());
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
        }
        #endregion

        #region CreateStream
        /// <summary>
        /// 创建日志写入流
        /// </summary>
        private void CreateStream()
        {
            _currentStream.CurrentFileStream = new FileStream(_currentStream.CurrentLogFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            _currentStream.CurrentStreamWriter = new StreamWriter(_currentStream.CurrentFileStream, Encoding.UTF8);
        }
        #endregion

        #region CloseStream
        /// <summary>
        /// 关闭日志写入流
        /// </summary>
        private void CloseStream()
        {
            if (_currentStream.CurrentStreamWriter != null)
            {
                _currentStream.CurrentStreamWriter.Close();
            }

            if (_currentStream.CurrentFileStream != null)
            {
                _currentStream.CurrentFileStream.Close();
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

        #region 写文件
        /// <summary>
        /// 写文件
        /// </summary>
        private void WriteFile(string log)
        {
            try
            {
                lock (_lockWriter)
                {
                    //判断是否更新Stream
                    string dateStr = DateTime.Now.ToString(_dateFormat);
                    if (_currentStream.CurrentDateStr != dateStr)
                    {
                        _currentStream.CurrentDateStr = dateStr;
                        UpdateCurrentStream();
                    }

                    //判断是否创建Archive
                    int byteCount = Encoding.UTF8.GetByteCount(log) + 2;
                    _currentStream.CurrentFileSize += byteCount;
                    if (_currentStream.CurrentFileSize >= _fileSize)
                    {
                        _currentStream.CurrentFileSize = byteCount;
                        CreateArchive();
                    }

                    //日志内容写入文件
                    _currentStream.CurrentStreamWriter.WriteLine(log);
                    _currentStream.CurrentStreamWriter.Flush();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + "\r\n" + ex.StackTrace);
            }
        }
        #endregion

        #region CreateArchive
        /// <summary>
        /// 创建日志存档
        /// </summary>
        private void CreateArchive()
        {
            string fileName = Path.GetFileNameWithoutExtension(_currentStream.CurrentLogFilePath);

            CloseStream(); //关闭日志写入流
            File.Move(_currentStream.CurrentLogFilePath, Path.Combine(_currentStream.CurrentLogFileDir, fileName + "_" + (++_currentStream.CurrentArchiveIndex) + ".txt")); //存档
            CreateStream(); //创建日志写入流
        }
        #endregion

        #region UpdateCurrentStream
        /// <summary>
        /// 更新日志写入流
        /// </summary>
        private void UpdateCurrentStream()
        {
            try
            {
                //关闭日志写入流
                CloseStream();

                //创建新的日志路径
                _currentStream.CurrentDateStr = DateTime.Now.ToString(_dateFormat);
                _currentStream.CurrentLogFileDir = Path.Combine(_basePath, _rootFolder + "\\" + _logType.ToString());
                _currentStream.CurrentLogFilePath = Path.Combine(_currentStream.CurrentLogFileDir, _currentStream.CurrentDateStr + ".txt");

                //创建日志写入流
                CreateStream();

                //初始化 _currentArchiveIndex
                InitCurrentArchiveIndex();

                //初始化 _currentFileSize
                InitCurrentFileSize();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + "\r\n" + ex.StackTrace);
            }
        }
        #endregion

        #region 写日志
        /// <summary>
        /// 写日志
        /// </summary>
        /// <param name="log">日志内容</param>
        public void WriteLog(string log)
        {
            try
            {
                log = CreateLogString(_logType, log);
                WriteFile(log);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + "\r\n" + ex.StackTrace);
            }
        }
        #endregion

    }
}
