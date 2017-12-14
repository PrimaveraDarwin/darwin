using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primavera.Util.Injector.Helpers
{
    public class FileHelper
    {
        /// <summary>
        /// Exists the line.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="firstLine">The first line.</param>
        /// <param name="lastLine">The last line.</param>
        /// <returns></returns>
        public static bool ExistLine(string filePath, int firstLine, int lastLine, string containingPattern)
        {
            int linePosition = 1;
            StreamReader streamReader = new StreamReader(filePath);
            string textLine = string.Empty;

            if (streamReader != null)
            {
                while ((textLine = streamReader.ReadLine()) != null)
                {
                    if (linePosition >= firstLine && linePosition <= lastLine)
                    {
                        if (textLine.ToLower().Contains(containingPattern.ToLower()))
                        {
                            streamReader.Close();
                            return true;
                        }
                    }

                    linePosition++;
                }
            }

            streamReader.Close();
            return false;
        }

        /// <summary>
        /// Inserts the line.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="line">The line.</param>
        /// <param name="position">The position.</param>
        public static void InsertLine(string filename, string line, int position)
        {
            int linePosition = 0;
            string tempfile = Path.GetTempFileName();

            StreamWriter streamWriter = new StreamWriter(tempfile, false, Encoding.GetEncoding("ISO-8859-1"));
            StreamReader streamReader = new StreamReader(filename, Encoding.GetEncoding("ISO-8859-1"), true);

            while (!streamReader.EndOfStream)
            {
                if (linePosition == position)
                {
                    streamWriter.WriteLine(line);
                }

                streamWriter.WriteLine(streamReader.ReadLine());

                linePosition++;
            }

            streamWriter.Close();
            streamReader.Close();

            // Replace the original file

            File.Copy(tempfile, filename, true);

            // Delete the temporary file

            File.Delete(tempfile);
        }

        public static void ReplaceText(string fileName, string text, string newText)
        {
            var fileText = File.ReadAllText(fileName, Encoding.Default);

            fileText = fileText.Replace(text, newText);

            File.WriteAllText(fileName, fileText, Encoding.Default);
        }
    }
}
