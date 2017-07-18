using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.IO;

namespace Primavera.Util
{
    public static class LogHelper
    {
        #region Constants
        
        private static object shared = new object();
        private static string FILE_NAME = @"\temp\Primavera.Util.log";

        #endregion

        #region Members

        private static bool? fileWriterIsActive;

        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Logs the specified message into the file log writer. 
        /// This is the most simple and effective log writer, because it does not depends on any configuration that could fail if not properly loaded.
        /// </summary>
        /// <param name="message">The message.</param>
        public static void FileTrace(string message)
        {
            try
            {
                fileWriterIsActive = fileWriterIsActive ?? File.Exists(FILE_NAME);

                if (!(bool)fileWriterIsActive)
                {
                    return;
                }

                // Get default log file name

                string fileName = FILE_NAME;

                // Get call stack information

                StackTrace stackTrace = new StackTrace();

                if (stackTrace != null)
                {
                    // Display the previous function call in the stack
                    // or, if not available, the most recent function call

                    int fraIndex = (stackTrace.FrameCount > 1) ? 1 : 0;

                    MethodBase methodBase = stackTrace.GetFrame(fraIndex).GetMethod();

                    string assemblyName = methodBase.Module.Assembly.GetName().Name;
                    string typeName = methodBase.ReflectedType.Name;
                    string methodName = methodBase.Name;

                    // Trace message

                    FileTrace(message, assemblyName, typeName, methodName, fileName);
                }
            }
            catch (Exception)
            {
                // Ignore exceptions when logging
            }
        }

        /// <summary>
        /// Files the trace.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="assemblyName">Name of the assembly.</param>
        /// <param name="typeName">Name of the type.</param>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="file">The file.</param>
        private static void FileTrace(string message, string assemblyName, string typeName, string methodName, string file)
        {
            lock(shared)
            {
                int processId = Process.GetCurrentProcess().Id;
                try
                {
                    System.IO.File.AppendAllText(file, string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\r\n", processId, DateTime.Now, assemblyName, typeName, methodName, message));
                }
                catch { }
            }
        }

        #endregion
                
    }
}
