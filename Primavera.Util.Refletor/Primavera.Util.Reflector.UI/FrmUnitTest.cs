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
                inputPath = @"C:\prjNET\ERP10\ERP\Mainline\_Bin\VndBS100.dll";
                outputPath = "C:\\Users\\Primavera\\Desktop\\test2";

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
                List<string> actual_params = new List<string>();
                Dictionary<string, string> dictionary = new Dictionary<string, string>();
                dictionary.Add("BasBS", "Base");
                dictionary.Add("VndBS", "Vendas");

                ModuleEntity moduleEntity = decompile.DecompileAssembly(inputPath);

                foreach (TypeEntity typeEntity in moduleEntity.Types)
                {
                    if (!typeEntity.Name.Substring(0, 5).Equals(bs_name))
                        continue;
                    string entityTypeName = typeEntity.Name.Substring(5);

                    if (TypeHelper.HasPublicMethods(typeEntity) && entityTypeName.Length > 0)
                    {
                        if (!File.Exists(outputPath))
                        {
                            TextWriter tw = new StreamWriter(outputPath + "\\" + entityTypeName + "Test.cs");

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
                                    tw.WriteLine("    [DataSource(\"System.Data.Odbc\", \"Dsn=Excel Files;Driver={Microsoft Excel Driver (*.xls)};dbq=|DataDirectory|\\\\DataFiles\\\\" + entityTypeName + "_" + methodEntity.Name + ".xls;defaultdir=.\\\\DataFiles;\", \"Sheet1$\", DataAccessMethod.Sequential)]");
                                    tw.WriteLine("    public void {0}_{1}()", entityTypeName, methodEntity.Name);
                                    tw.WriteLine("    {");

                                    ///////////////////

                                    string call = "(";

                                    foreach (MethodParameter atr in methodEntity.Parameters)
                                    {
                                        System.Diagnostics.Debug.Print(processa_tipos(atr));
                                        tw.WriteLine(processa_tipos(atr));
                                        call += (atr.ParameterType.IsByReference ? "ref " : string.Empty) + atr.Name + ",";
                                    }
                                    
                                    var index = call.LastIndexOf(',');
                                    if (index >= 0) call = call.Substring(0, index);

                                    call += ");\n";

                                    System.Diagnostics.Debug.Print(string.Format("        MotorLE.{0}.{1}.{2}{3}", dictionary[bs_name], entityTypeName, methodEntity.Name, call));
                                    tw.WriteLine(string.Format("\n        MotorLE.{0}.{1}.{2}{3}", dictionary[bs_name], entityTypeName, methodEntity.Name, call));
                                    tw.WriteLine("        // Insert your assert here!");

                                    ///////////////////
                                   
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

        private string processa_tipos(MethodParameter atr)
        {
            if (atr.ParameterType.Name.ToLower() == ("object[]") || atr.ParameterType.Name.ToLower() == ("object[]&"))
                return ($"        dynamic[] {atr.Name} = null;");

            if (atr.ParameterType.Name.ToLower().EndsWith("[]") || atr.ParameterType.Name.ToLower().EndsWith("[]&"))
                return ($"        {atr.ParameterType.Name.Replace("&","")} {atr.Name} = null;");

            switch (atr.ParameterType.Name.ToLower())
            {
                case "string":
                case "string&":
                    return ($"        string {atr.Name} = this.TestContext.DataRow[\"{atr.Name}\"].ToString();");
                case "short":
                case "short&":
                    return ($"        short {atr.Name} = short.Parse(this.TestContext.DataRow[\"{atr.Name}\"].ToString());");
                case "int":
                case "int&":
                    return ($"        int {atr.Name} = int.Parse(this.TestContext.DataRow[\"{atr.Name}\"].ToString());");
                case "float":
                case "float&":
                    return ($"        float {atr.Name} = float.Parse(this.TestContext.DataRow[\"{atr.Name}\"].ToString());");
                case "double":
                case "double&":
                    return ($"        double {atr.Name} = double.Parse(this.TestContext.DataRow[\"{atr.Name}\"].ToString());");
                case "long":
                case "long&":
                    return ($"        long {atr.Name} = long.Parse(this.TestContext.DataRow[\"{atr.Name}\"].ToString());");
                case "boolean":
                case "boolean&":
                    return ($"        bool {atr.Name} = bool.Parse(this.TestContext.DataRow[\"{atr.Name}\"].ToString());");
                case "bool":
                case "bool&":
                    return ($"        bool {atr.Name} = bool.Parse(this.TestContext.DataRow[\"{atr.Name}\"].ToString());");
                case "object":
                case "object&":
                    return ($"        dynamic {atr.Name} = null;");
                case "datetime":
                case "datetime&":
                    return ($"        DateTime {atr.Name} = DateTime.Parse(this.TestContext.DataRow[\"{atr.Name}\"].ToString());");

                default:
                    return ($"        {atr.ParameterType.Name.Replace("&","")} {atr.Name} = new { atr.ParameterType.Name.Replace("&", "")}();");
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
