using Mono.Cecil;
using Mono.Cecil.Cil;
using Primavera.Util.Refletor.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primavera.Util.Refletor.Utils
{
    public class Decompile
    {
        public ModuleEntity DecompileAssembly(string fileName)
        {
            var readerParameters = new ReaderParameters { ReadSymbols = true };
            var assemblyDefinition = Mono.Cecil.AssemblyDefinition.ReadAssembly(fileName, readerParameters);
            ModuleEntity moduleEntity = new ModuleEntity();

            foreach (var module in assemblyDefinition.Modules)
            {
                var types = module.GetTypes();
                foreach (var type in types)
                {
                    if (!type.Name.StartsWith("<"))
                    { 
                        string typeSourceLocation = FindFileName(type);
                        if(!string.IsNullOrEmpty(typeSourceLocation))
                        {
                            TypeEntity typeEntity = new TypeEntity();
                            typeEntity.SetTypeDeclaration(type);
                            moduleEntity.Types.Add(typeEntity);

                            var methods = type.Methods;
                            foreach (var method in methods)
                            {
                                if(method.Name == "Actualiza")
                                { 
                                    MethodEntity methodEntity = new MethodEntity();
                                    methodEntity.SetMethodDeclaration(method);
                                    typeEntity.Methods.Add(methodEntity);
                                }
                            }

                            //// Same goes with Fields, fields are basically just a 
                            //// globally declared variable (within the scope of the Type).
                            //var fields = type.Fields;
                            //foreach (var field in fields)
                            //{
                            //    //var fieldDecleration = GetFieldDecleration(field);
                            //    //Console.WriteLine("\tField\t- {0}", fieldDecleration);
                            //}
                        }
                    }
                }
            }

            return moduleEntity;
        }


        public string FindFileName(TypeDefinition typedef)
        {
            foreach (var method in typedef.Methods)
            {
                if (method.Body != null && method.Body.Instructions != null)
                {
                    foreach (var instruction in method.Body.Instructions)
                    {
                        if (instruction.SequencePoint != null && instruction.SequencePoint.Document != null && instruction.SequencePoint.Document.Url != null)
                        { 
                            return instruction.SequencePoint.Document.Url;
                        }
                    }
                }
            }
            
            return null;
        }        
    }
}
