using Primavera.Util.Injector.Helpers;
using Primavera.Util.Refletor.Entities;
using Primavera.Util.Refletor.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace Primavera.Util.Reflector.UI
{
    public partial class FrmUnitTest : Form
    {

        public FrmUnitTest()
        {
            InitializeComponent();
        }


        private void btProcess_Click(object sender, EventArgs e)
        {
            try
            {
                Decompile decompile = new Decompile();
                string inputPath = txtFilePath.Text.ToString();
                string outputPath = TxtDestiny.Text.ToString();
                System.Diagnostics.Debug.Print(inputPath);
                // Examples:
                // inputPath = "C:\\Users\\Primavera\\Desktop\\test folder\\BasBS100.dll";
                // outputPath = "C:\\Users\\Primavera\\Desktop\\test folder1";

                var result = inputPath.Substring(inputPath.LastIndexOf('\\') + 1);
                string bs_name = result.Substring(0, 5);
                System.Diagnostics.Debug.Print(bs_name);


                if (File.Exists(inputPath))
                {
                    // This path is a file
                    if (!Directory.Exists(inputPath))
                        System.IO.Directory.CreateDirectory(outputPath);
                }
                else if (Directory.Exists(inputPath))
                {
                    // This path is a directory
                    throw new NotImplementedException();
                }
                else
                {
                    System.Diagnostics.Debug.Print("{0} is not a valid file or directory.", (txtFilePath.ToString()));

                    throw new Exception();
                }
                List<string> used_methods = new List<string>();

                ModuleEntity moduleEntity = decompile.DecompileAssembly(inputPath);

                foreach (TypeEntity typeEntity in moduleEntity.Types)
                {
                    System.Diagnostics.Debug.Print(typeEntity.Name);
                    if (!typeEntity.Name.Substring(0, 5).Equals(bs_name))
                        continue;
                    string entityTypeName = typeEntity.Name.Substring(5);

                    if (TypeHelper.HasPublicMethods(typeEntity) && entityTypeName.Length > 0)
                    {
                        if (!File.Exists(outputPath))
                        {
                            TextWriter tw = new StreamWriter(outputPath + "\\" + entityTypeName + ".cs");

                            tw.WriteLine("using System;");
                            tw.WriteLine("using Microsoft.VisualStudio.TestTools.UnitTesting;");
                            tw.WriteLine("using StdBE100;");
                            tw.WriteLine("using Primavera.Framework.Tests;");
                            tw.WriteLine("");
                            tw.WriteLine("[TestClass]");
                            tw.WriteLine(string.Format("public class {0}Test : BaseTest", entityTypeName));
                            tw.WriteLine("{");

                            foreach (MethodEntity methodEntity in typeEntity.Methods)
                            {
                                if (!methodEntity.IsConstructor && methodEntity.IsPublic && !used_methods.Contains(methodEntity.Name))
                                {
                                    tw.WriteLine("    [TestMethod]");
                                    tw.WriteLine("    [DataSource(\"System.Data.Odbc\", \"Dsn=Excel Files;Driver={Microsoft Excel Driver (*.xls)};dbq=|DataDirectory|\\\\DataFiles\\\\" + methodEntity.Name + ".xls;defaultdir=.\\\\DataFiles;\", \"Sheet1$\", DataAccessMethod.Sequential)]");
                                    tw.WriteLine("    public void {0}_{1}()", entityTypeName, methodEntity.Name);
                                    tw.WriteLine("    {");
                                    tw.WriteLine("        // Insert your code here!");
                                    tw.WriteLine("    " + "    // MotorLE.AntesActualizar" + MethodHelper.MethodSignature(methodEntity) + ";");
                                    tw.WriteLine("    }" + Environment.NewLine);
                                    used_methods.Add(methodEntity.Name);
                                }
                            }
                            used_methods.Clear();
                            tw.WriteLine("}");
                            tw.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.FileTrace(ex.ToString());
            }
        }



        private void btGetFilePath_Click(object sender, EventArgs e)
        {
            txtFilePath.Text = FileHelper.GetFile();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            TxtDestiny.Text = FileHelper.GetPath();

        }
    }
}
