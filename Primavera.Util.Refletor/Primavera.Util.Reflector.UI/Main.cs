using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Primavera.Util;

namespace Primavera.Util.Reflector.UI
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles the CheckedChanged event of the rdbPath control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void rdbPath_CheckedChanged(object sender, EventArgs e)
        {
            txtFilePath.AutoCompleteSource = rdbPath.Checked ? AutoCompleteSource.FileSystemDirectories : AutoCompleteSource.FileSystem;
        }

        /// <summary>
        /// Handles the Click event of the btGetFilePath control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void btGetFilePath_Click(object sender, EventArgs e)
        {
            txtFilePath.Text = rdbPath.Checked ? FileHelper.GetPath() : FileHelper.GetFile();
        }
        
        /// <summary>
        /// Handles the Click event of the btProcess control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void btProcess_Click(object sender, EventArgs e)
        {
            try
            {

            }
            catch (Exception ex)
            {
                LogHelper.FileTrace(ex.ToString());
            }
        }
        
        private void txtFilePath_Leave(object sender, EventArgs e)
        {
            LoadFilesToListView();
        }

        /// <summary>
        /// Loads the files to ListView.
        /// </summary>
        private void LoadFilesToListView()
        {
            try
            {
                // TODO : ERRO AQUI...
                lstFiles.Items.Clear();
                if (!string.IsNullOrEmpty(txtFilePath.Text))
                {
                    if (rdbPath.Checked)
                    {
                        var listOfFiles = FileHelper.GetAllFilesFromPath(txtFilePath.Text, txtFileExtension.Text, txtFileFilter.Text);
                        listOfFiles.ForEach(x => lstFiles.Items.Add(x));
                    }
                    else
                    {
                        lstFiles.Items.Add(txtFilePath.Text);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.FileTrace(ex.ToString());
            }
        }

    }
}
