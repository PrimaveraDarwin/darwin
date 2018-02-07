#region References

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using MoreLinq;
using Primavera.Util.Injector;
using Primavera.Util.Refletor.Entities;
using Primavera.Util.Refletor.Utils;

#endregion

namespace Primavera.Util.Reflector.UI
{
    public partial class Main : Form
    {
        private string ModuleName;

        private readonly List<Method> methodsList;

        public Main()
        {
            methodsList = new List<Method>();

            InitializeComponent();

            checkBoxFirst.Checked = true;
        }

        /// <summary>
        ///     Handles the Click event of the btGetFilePath control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void btGetFilePath_Click(object sender, EventArgs e)
        {
            txtFilePath.Text = rdbPath.Checked ? FileHelper.GetPath() : FileHelper.GetFile();
        }

        private void exactMatchCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            searchMethodsTextBox_TextChanged(sender, e);
        }

        /// <summary>
        ///     Handles the Click event of the btProcess control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void injectCodeButton_Click(object sender, EventArgs e)
        {
            var fileInjector = new FileInjector();
            var methodsToInject = methodsList.Where(m => m.inject).Reverse();

            foreach (var method in methodsToInject)
            {
                var entity = method.methodEntity;
                var preLine = method.GenerateFullPreLine(ModuleName);
                var posLine = method.GenerateFullPosLine(ModuleName);

                if (!string.IsNullOrEmpty(preLine) || !string.IsNullOrEmpty(posLine))
                {
                    fileInjector.Inject(entity, preLine, posLine, checkBoxFirst.Checked);
                }
            }

            if (methodsToInject.Count() > 0)
                MessageBox.Show("The code was successufully injected in the selected files.", "Primavera Injector",
                    MessageBoxButtons.OK);
        }

        private void loadMethodsButton_Click(object sender, EventArgs e)
        {
            var decompile = new Decompile();

            methodsList.Clear();



            foreach (DataGridViewRow row in filesDataGridView.Rows)
            {
                var dllFile = row.Cells["File"].Value.ToString();
                var loadCheckBox = (DataGridViewCheckBoxCell)row.Cells["Load"];

                if (loadCheckBox.Value == bool.TrueString)
                    try
                    {
                        var moduleEntity = decompile.DecompileAssembly(dllFile);


                        foreach (var typeEntity in moduleEntity.Types)
                        {
                            var uniqMethodsList = typeEntity.Methods.DistinctBy(o => o.Name);
                            var longestParametersMethodList = new List<MethodEntity>();

                            foreach (var method in uniqMethodsList)
                            {
                                var max = typeEntity.Methods.Where(p => p.Name == method.Name).MaxBy(p => p.Parameters.Count);

                                longestParametersMethodList.Add(max);
                            }

                            foreach (var methodEntity in longestParametersMethodList)
                            {


                                if (methodEntity.Location != null && methodEntity.Exceptions.Count > 0 &&
                                    (methodEntity.Name.Equals("Atualiza")
                                     || methodEntity.Name.Equals("AtualizaId")
                                     || methodEntity.Name.Equals("AtualizaID")
                                     || methodEntity.Name.Equals("Actualiza")
                                     || methodEntity.Name.Equals("ActualizaId")
                                     || methodEntity.Name.Equals("ActualizaID")
                                     || methodEntity.Name.Equals("Edita")
                                     || methodEntity.Name.Equals("EditaId")
                                     || methodEntity.Name.Equals("EditaID")
                                     || methodEntity.Name.Equals("Remove")
                                     || methodEntity.Name.Equals("RemoveID")
                                     || methodEntity.Name.Equals("RemoveId")))
                                {
                                    var file = Path.GetFileName(methodEntity.Location.Url);
                                    var data = new Method(methodEntity.Name, file, methodEntity, true);

                                    methodsList.Add(data);
                                }
                            }
                        }
                    }
                    catch (FileNotFoundException ex)
                    {
                        MessageBox.Show(
                            string.Format("There was an error while decompiling {0}: {1}. This dll will be ignored.",
                                dllFile, ex.Message), "Primavera Injector", MessageBoxButtons.OK);
                    }
            }

            populateMethodsGrid();
            tabMain.SelectedIndex = 1;
            if (checkBoxFirst.Checked)
            {
                labelInjectType.Text = "Tipo de Injeção 1º Passagem";
            }
            else
            {
                labelInjectType.Text = "Tipo de Injeção 2º Passagem";

            }
        }

        private void methodsGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 0 && e.RowIndex >= 0)
            {
                methodsGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);

                var value = (bool)methodsGrid.CurrentCell.Value;

                if (value)
                {
                    var preLine = methodsGrid.CurrentRow.Cells[3];
                    var posLine = methodsGrid.CurrentRow.Cells[4];

                    if (string.IsNullOrEmpty(preLine.Value as string))
                        preLine.Value = "AntesActualizar";
                    if (string.IsNullOrEmpty(posLine.Value as string))
                        posLine.Value = "DepoisActualizar";
                }
            }
        }

        private void populateMethodsGrid()
        {
            var list = new List<Method>();

            methodsList.ForEach(m => list.Add(m));

            var bindingList = new BindingList<Method>(list);
            var source = new BindingSource(bindingList, null);

            methodsGrid.DataSource = source;

            foreach (DataGridViewColumn column in methodsGrid.Columns)
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
        }

        /// <summary>
        ///     Handles the CheckedChanged event of the rdbPath control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void rdbPath_CheckedChanged(object sender, EventArgs e)
        {
            txtFilePath.AutoCompleteSource =
                rdbPath.Checked ? AutoCompleteSource.FileSystemDirectories : AutoCompleteSource.FileSystem;
        }

        private void searchMethodsTextBox_TextChanged(object sender, EventArgs e)
        {
            var list = new List<Method>();
            var text = searchMethodsTextBox.Text;

            foreach (var method in methodsList)
                if (exactMatchCheckBox.Checked)
                {
                    if (method.name.Equals(text))
                        list.Add(method);
                }
                else
                {
                    if (method.name.ToLower().Contains(text.ToLower()))
                        list.Add(method);
                }

            var bindingList = new BindingList<Method>(list);
            var source = new BindingSource(bindingList, null);

            methodsGrid.DataSource = source;
        }

        /// <summary>
        ///     Handles the TextChanged event of the txtFilePath control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void txtFilePath_TextChanged(object sender, EventArgs e)
        {
            filesDataGridView.Rows.Clear();

            foreach (DataGridViewColumn column in filesDataGridView.Columns)
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;

            if (!string.IsNullOrEmpty(txtFilePath.Text))
                if (rdbPath.Checked)
                {
                    var listOfFiles =
                        FileHelper.GetAllFilesFromPath(txtFilePath.Text, txtFileExtension.Text, txtFileFilter.Text);

                    listOfFiles.ForEach(x => filesDataGridView.Rows.Add(bool.TrueString, x));

                    ModuleName = new DirectoryInfo(Path.GetDirectoryName(txtFilePath.Text)).Name;
                }
                else
                {
                    filesDataGridView.Rows.Add(bool.TrueString, txtFilePath.Text);
                }
        }

    }
}