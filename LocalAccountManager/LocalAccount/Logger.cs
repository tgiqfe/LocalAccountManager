using System.Text;

namespace LocalAccountManager.LocalAccount
{
    internal class Logger
    {
        /// <summary>
        /// Log file name without extension.
        /// </summary>
        const string LOGFILENAME = "LocalAccount";

        /// <summary>
        /// Log title. (Displayed in each log line, same as the log file name)
        /// </summary>
        const string LOGTITLE = "LocalAccount";

        private static object _logLock = new object();
        private static string _logFilePath = null;

        /// <summary>
        /// Initialize log file path. Create Logs folder if not exists.
        /// </summary>
        private static void Initialize()
        {
            if (_logFilePath == null)
            {
                string parent = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath), "Logs");
                if (!Directory.Exists(parent))
                {
                    Directory.CreateDirectory(parent);
                }
                _logFilePath = Path.Combine(parent, $"{LOGFILENAME}_{DateTime.Now:yyyyMMdd}.log");
            }
        }

        /// <summary>
        /// Write log line to log file.
        /// </summary>
        /// <param name="level">Info,Warn,Error,Debug,None</param>
        /// <param name="title"></param>
        /// <param name="message"></param>
        public static void WriteLine(string level, string message)
        {
            if (_logFilePath == null) Initialize();
            string dd = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            lock (_logLock)
            {
                using (var sw = new StreamWriter(_logFilePath, true, Encoding.UTF8))
                {
                    sw.WriteLine($"[{dd}][{level}]{LOGTITLE}: {message}");
                }
            }
        }

        /// <summary>
        /// Write log line to log file with Info level and GeneralLog title.
        /// </summary>
        /// <param name="message"></param>
        public static void WriteLine(string message)
        {
            WriteLine("Info", message);
        }

        /// <summary>
        /// Write log raw message to log file. (without date,time,level,title)
        /// </summary>
        /// <param name="message"></param>
        public static void WriteRaw(string message)
        {
            if (_logFilePath == null) Initialize();
            lock (_logLock)
            {
                using (var sw = new StreamWriter(_logFilePath, true, Encoding.UTF8))
                {
                    sw.WriteLine(message);
                }
            }
        }
    }
}
