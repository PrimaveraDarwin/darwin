using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Primavera.Util
{
    /// <summary>
    /// Helper class to manage files.
    /// </summary>
    public static class FileHelper
    {

        /// <summary>
        /// Gets the file.
        /// </summary>
        /// <returns></returns>
        public static string GetFile()
        {
            var result = string.Empty;
            using (OpenFileDialog openFile = new OpenFileDialog())
            {
                if (openFile.ShowDialog() == DialogResult.OK)
                {
                    result = openFile.FileName;
                }
            }
            return result;
        }

        /// <summary>
        /// Gets the path.
        /// </summary>
        /// <returns></returns>
        public static string GetPath()
        {
            string path = string.Empty;
            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    path = dlg.SelectedPath;
                }
            }
            return path;
        }

        /// <summary>
        /// Gets all files from path.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="fileFormat">The file format.</param>
        /// <param name="fileFilter">The file filter.</param>
        /// <returns></returns>
        public static List<string> GetAllFilesFromPath(string filePath, string fileFormat, string fileFilter)
        {
            return (List<string>)Directory.EnumerateFiles(filePath, fileFormat, SearchOption.AllDirectories).Where(s => s.Contains(fileFilter)).ToList<String>();
        }
    }
}
