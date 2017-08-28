using Microsoft.Office.Interop.Excel;
using Primavera.Util.Injector.Helpers;
using Primavera.Util.Refletor.Entities;
using Primavera.Util.Refletor.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Excel = Microsoft.Office.Interop.Excel;



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
                string bs_name = result.Substring(0, result.Length - 7);
                //System.Diagnostics.Debug.Print(bs_name);


                if (File.Exists(inputPath))
                {
                    if (!Directory.Exists(inputPath))
                        System.IO.Directory.CreateDirectory(outputPath);
                }
                else if (Directory.Exists(inputPath))
                {
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
                ArrayList classes = new ArrayList();
                dictionary.Add("BasBS", "Base");
                dictionary.Add("VndBS", "Vendas");
                dictionary.Add("AdmBS", "Administrador");
                dictionary.Add("StdPlatBS", "Plataforma");
                dictionary.Add("ErpBS", "Erp");

                ModuleEntity moduleEntity = decompile.DecompileAssembly(inputPath);

                foreach (TypeEntity typeEntity in moduleEntity.Types)
                {
                    if (typeEntity.Name.Substring(0, 3).ToLower().Equals("frm"))
                         continue;
                    string entityTypeName = typeEntity.Name;




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
                            tw.WriteLine($"namespace Primavera.{dictionary[bs_name]}.Tests"); // mudar aqui
                            tw.WriteLine("{");


                            tw.WriteLine("    [TestClass]");
                            tw.WriteLine(string.Format("    public class {0}Test : BaseTest", entityTypeName));
                            classes.Add(entityTypeName);
                            tw.WriteLine("    {");

                            foreach (MethodEntity methodEntity in typeEntity.Methods)
                            {
                                System.Diagnostics.Debug.Print(entityTypeName + "  " + methodEntity.Name + " " + !entityTypeName.Substring(0, 3).ToLower().Equals("frm"));

                                if (!methodEntity.IsConstructor && methodEntity.IsPublic && !used_methods.Contains(methodEntity.Name))
                                {
                                    tw.WriteLine($"        [Ignore, TestMethod, DataSource(BaseUtils.XMLPROVIDER, Utils{bs_name}.FILE_XML_{entityTypeName.ToUpper()}, BaseUtils.TABLE, BaseUtils.DATAACCESS)]");
                                    tw.WriteLine("        public void {0}_{1}()", entityTypeName, methodEntity.Name);
                                    tw.WriteLine("        {");

                                    string call = "(";

                                    foreach (MethodParameter atr in methodEntity.Parameters)
                                    {
                                        tw.WriteLine("    " + processa_tipos(atr));
                                        call += (atr.ParameterType.IsByReference ? "ref " : string.Empty) + atr.Name + ",";
                                    }

                                    var index = call.LastIndexOf(',');
                                    if (index >= 0) call = call.Substring(0, index);

                                    call += ");\n";

                                    if(bs_name.Equals("StdPlatBS"))
                                        tw.WriteLine(string.Format("\n            {0}.{1}.{2}{3}", dictionary[bs_name], entityTypeName.Substring(5), methodEntity.Name, call));
                                    else
                                        tw.WriteLine(string.Format("\n            MotorLE.{0}.{1}.{2}{3}", dictionary[bs_name], entityTypeName.Substring(5), methodEntity.Name, call));
                                    tw.WriteLine("            // Insert your assert here!");

                                    tw.WriteLine("        }" + Environment.NewLine);
                                    used_methods.Add(methodEntity.Name);
                                }
                            }

                            used_methods.Clear();
                            tw.WriteLine("    }");
                            tw.WriteLine("}");
                            tw.Close();
                        }
                    }
                }
                createBasUtils(classes, outputPath, bs_name, dictionary[bs_name]);
            }
            catch (Exception ex)
            {
                LogHelper.FileTrace(ex.ToString());
            }
        }




        private string processa_tipos(MethodParameter atr)
        {
            if (atr.ParameterType.Name.ToLower() == ("object[]") || atr.ParameterType.Name.ToLower() == ("object[]&"))
                return ($"        dynamic[] {atr.Name} = null; // Create params array according to implementation of method");

            if (atr.ParameterType.Name.ToLower().EndsWith("[]") || atr.ParameterType.Name.ToLower().EndsWith("[]&"))
                return ($"        {atr.ParameterType.Name.Replace("&", "")} {atr.Name} = null;");

            switch (atr.ParameterType.Name.ToLower())
            {
                case "string":
                case "string&":
                    if (atr.Name == "Atributo")
                        return ($"        string {atr.Name} = GetString(\"{atr.Name}\");" + Environment.NewLine +
                        $"            string Val{atr.Name} = GetString(\"Val{atr.Name}\"); // Create element in XML file when it's possible (when its not an object)");
                    else
                        return ($"        string {atr.Name} = GetString(\"{atr.Name}\");");
                case "short":
                case "short&":
                    if (atr.Name == "Atributo")
                        return ($"        short {atr.Name} = GetShort(\"{atr.Name}\");" + Environment.NewLine +
                        $"            short Val{atr.Name} = GetShort(\"Val{atr.Name}\"); // Create element in XML file when it's possible (when its not an object)");
                    else
                        return ($"        short {atr.Name} = GetShort(\"{atr.Name}\");");
                case "int":
                case "int&":
                    if (atr.Name == "Atributo")
                        return ($"        int {atr.Name} = GetInt(\"{atr.Name}\");" + Environment.NewLine +
                        $"            int Val{atr.Name} = GetInt(\"Val{atr.Name}\"); // Create element in XML file when it's possible (when its not an object)");
                    else
                        return ($"        int {atr.Name} = GetInt(\"{atr.Name}\");");
                case "float":
                case "float&":
                    if (atr.Name == "Atributo")
                        return ($"        float {atr.Name} = GetFloat(\"{atr.Name}\");" + Environment.NewLine +
                        $"            float Val{atr.Name} = GetFloat(\"Val{atr.Name}\"); // Create element in XML file when it's possible (when its not an object)");
                    else
                        return ($"        float {atr.Name} = GetFloat(\"{atr.Name}\");");
                case "double":
                case "double&":
                    if (atr.Name == "Atributo")
                        return ($"        double {atr.Name} = GetDouble(\"{atr.Name}\");" + Environment.NewLine +
                        $"            double Val{atr.Name} = GetDouble(\"Val{atr.Name}\"); // Create element in XML file when it's possible (when its not an object)");
                    else
                        return ($"        double {atr.Name} = GetDouble(\"{atr.Name}\");");
                case "long":
                case "long&":
                    if (atr.Name == "Atributo")
                        return ($"        long {atr.Name} = GetLong(\"{atr.Name}\");" + Environment.NewLine +
                        $"            long Val{atr.Name} = GetLong(\"Val{atr.Name}\"); // Create element in XML file when it's possible (when its not an object)");
                    else
                        return ($"        long {atr.Name} = GetLong(\"{atr.Name}\");");
                case "boolean":
                case "boolean&":
                    if (atr.Name == "Atributo")
                        return ($"        bool {atr.Name} = GetBool(\"{atr.Name}\");" + Environment.NewLine +
                        $"            bool Val{atr.Name} = GetBool(\"Val{atr.Name}\"); // Create element in XML file when it's possible (when its not an object)");
                    else
                        return ($"        bool {atr.Name} = GetBool(\"{atr.Name}\");");
                case "bool":
                case "bool&":
                    if (atr.Name == "Atributo")
                        return ($"        bool {atr.Name} = GetBool(\"{atr.Name}\");" + Environment.NewLine +
                        $"            bool Val{atr.Name} = GetBool(\"Val{atr.Name}\"); // Create element in XML file when it's possible (when its not an object)");
                    else
                        return ($"        bool {atr.Name} = GetBool(\"{atr.Name}\");");
                case "object":
                case "object&":
                    return ($"        dynamic {atr.Name} = null; // Create this object on the XML file ");
                case "datetime":
                case "datetime&":
                    if (atr.Name == "Atributo")
                        return ($"        DateTime {atr.Name} = GetDateTime(\"{atr.Name}\");" + Environment.NewLine +
                        $"            DateTime Val{atr.Name} = GetDateTime(\"Val{atr.Name}\"); // Create element in XML file when it's possible (when its not an object)");
                    else
                        return ($"        DateTime {atr.Name} = GetDateTime(\"{atr.Name}\");");

                default:
                    return ($"        {atr.ParameterType.Name.Replace("&", "")} {atr.Name} = new { atr.ParameterType.Name.Replace("&", "")}();");
            }
        }

        private void createBasUtils(ArrayList dic, string outputPath, string name, string bs)
        {
            System.IO.Directory.CreateDirectory(outputPath + "\\DataFiles");
            dic.Sort();
            foreach (string file in dic)
                System.IO.File.WriteAllText(outputPath + $"\\DataFiles\\{file}.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n<Rows>\r\n  <Row>\r\n  </Row>\r\n</Rows>\n");

            TextWriter tw = new StreamWriter(outputPath + $"\\Utils{name}.cs");
            tw.WriteLine("using Primavera.Framework.Tests;");
            tw.WriteLine("");
            tw.WriteLine($"namespace Primavera.{bs}.Tests");
            tw.WriteLine("{");
            tw.WriteLine($"    public static class Utils{name}");
            tw.WriteLine("    {");
            foreach (string filename in dic)
                tw.WriteLine($"        public const string FILE_XML_{filename.ToUpper()} = BaseUtils.FILES_PATH + \"{filename}.xml\";");
            tw.WriteLine("    }");
            tw.WriteLine("}");
            tw.Close();
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

