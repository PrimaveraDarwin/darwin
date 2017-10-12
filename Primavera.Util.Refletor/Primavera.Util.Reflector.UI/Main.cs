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
using Primavera.Util.Refletor.Utils;
using Primavera.Util.Refletor.Entities;
using Primavera.Util.Injector;
using System.IO;

namespace Primavera.Util.Reflector.UI
{
    public partial class Main : Form
    {
        private List<Method> methodsList;

        public Main()
        {
            this.methodsList = new List<Method>();

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
        /// Handles the TextChanged event of the txtFilePath control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void txtFilePath_TextChanged(object sender, EventArgs e)
        {
            filesDataGridView.Rows.Clear();

            foreach (DataGridViewColumn column in this.filesDataGridView.Columns)
            {
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            }

            if (!string.IsNullOrEmpty(txtFilePath.Text))
            {
                if (rdbPath.Checked)
                {
                    var listOfFiles = FileHelper.GetAllFilesFromPath(txtFilePath.Text, txtFileExtension.Text, txtFileFilter.Text);

                    listOfFiles.ForEach(x => filesDataGridView.Rows.Add(bool.TrueString, x));
                }
                else
                {
                    filesDataGridView.Rows.Add(bool.TrueString, txtFilePath.Text);
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the btProcess control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void injectCodeButton_Click(object sender, EventArgs e)
        {
            FileInjector fileInjector = new FileInjector();
            IEnumerable<Method> methodsToInject = methodsList.Where(m => m.inject);

            foreach (Method method in methodsToInject)
            {
                MethodEntity entity = method.methodEntity;
                string preLine = method.GenerateFullPreLine();
                string posLine = method.GenerateFullPosLine();

                fileInjector.Inject(entity, preLine, posLine);
            }

            if (methodsToInject.Count() > 0)
            {
                MessageBox.Show("The code was successufully injected in the selected files.", "Primavera Injector", MessageBoxButtons.OK);
            }
        }

        private void loadMethodsButton_Click(object sender, EventArgs e)
        {
            Decompile decompile = new Decompile();
            int methodId = 1;

            methodsList.Clear();

            foreach (DataGridViewRow row in filesDataGridView.Rows)
            {
                string dllFile = row.Cells["File"].Value.ToString();
                DataGridViewCheckBoxCell loadCheckBox = (DataGridViewCheckBoxCell) row.Cells["Load"];

                if (loadCheckBox.Value == bool.TrueString)
                {
                    try
                    {
                        ModuleEntity moduleEntity = decompile.DecompileAssembly(dllFile);


                        foreach (TypeEntity typeEntity in moduleEntity.Types)
                        {
                            foreach (MethodEntity methodEntity in typeEntity.Methods)
                            {
                                if (methodEntity.Location != null && methodEntity.Exceptions.Count > 0 && (methodEntity.Name.Equals("Actualiza") || methodEntity.Name.Equals("Edita")))
                                {
                                    string file = Path.GetFileName(methodEntity.Location.Url);
                                    Method data = new Method(methodId, methodEntity.Name, file, methodEntity, true);

                                    methodsList.Add(data);
                                    methodId++;
                                }
                            }
                        }
                    }
                    catch (System.IO.FileNotFoundException ex)
                    {
                        MessageBox.Show(string.Format("There was an error while decompiling {0}: {1}. This dll will be ignored.", dllFile, ex.Message), "Primavera Injector", MessageBoxButtons.OK);
                    }
                }
            }

            populateMethodsGrid();
            this.tabMain.SelectedIndex = 1;
        }

        private void populateMethodsGrid()
        {
            List<Method> list = new List<Method>();

            this.methodsList.ForEach(m => list.Add(m));

            var bindingList = new BindingList<Method>(list);
            var source = new BindingSource(bindingList, null);

            methodsGrid.DataSource = source;

            foreach (DataGridViewColumn column in this.methodsGrid.Columns)
            {
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            }
        }

        private void searchMethodsTextBox_TextChanged(object sender, EventArgs e)
        {
            List<Method> list = new List<Method>();
            string text = this.searchMethodsTextBox.Text;

            foreach(var method in this.methodsList)
            {
                if (this.exactMatchCheckBox.Checked)
                {
                    if (method.name.Equals(text))
                    {
                        list.Add(method);
                    }
                }
                else
                {
                    if (method.name.ToLower().Contains(text.ToLower()))
                    {
                        list.Add(method);
                    }
                }
            }

            var bindingList = new BindingList<Method>(list);
            var source = new BindingSource(bindingList, null);

            methodsGrid.DataSource = source;
        }

        private void exactMatchCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            this.searchMethodsTextBox_TextChanged(sender, e);
        }

        private void methodsGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if ((e.ColumnIndex == 0) && (e.RowIndex >= 0))
            {
                this.methodsGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);

                bool value = (bool) this.methodsGrid.CurrentCell.Value;

                if (value)
                {
                    DataGridViewCell preLine = this.methodsGrid.CurrentRow.Cells[3];
                    DataGridViewCell posLine = this.methodsGrid.CurrentRow.Cells[4];

                    if (String.IsNullOrEmpty(preLine.Value as string))
                    {
                        preLine.Value = "AntesActualizar";
                    }
                    if (String.IsNullOrEmpty(posLine.Value as string))
                    {
                        posLine.Value = "DepoisActualizar";
                    }
                }
            }
        }
    }
}
