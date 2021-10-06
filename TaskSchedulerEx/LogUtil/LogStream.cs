using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    internal class LogStream
    {
        public FileStream CurrentFileStream { get; set; }

        public StreamWriter CurrentStreamWriter { get; set; }

        public int CurrentArchiveIndex { get; set; }

        public long CurrentFileSize { get; set; }

        public string CurrentDateStr { get; set; }

        public string CurrentLogFilePath { get; set; }

        public string CurrentLogFileDir { get; set; }
    }
}
