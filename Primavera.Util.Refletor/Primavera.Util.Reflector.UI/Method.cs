#region References

using System;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using Primavera.Util.Refletor.Entities;

#endregion

namespace Primavera.Util.Reflector.UI
{
    public class Method
    {
        private const string LINE_PREFIX = "m_objErpBSO.Extensibility.TriggerEvent(this, ";

        public Method(string name, string file, MethodEntity methodEntity, bool inject)
        {
            this.name = name;
            this.file = file;
            this.methodEntity = methodEntity;
            this.inject = inject;
        }

        [Browsable(true)]
        public bool inject { get; set; }

        [Browsable(true)]
        public string name { get; }

        [Browsable(true)]
        public string file { get; }

        [Browsable(false)]
        public MethodEntity methodEntity { get; }

        public string GenerateFullPosLine(string ModuleName)
        {
            string generatedEvent = GetGeneratedEventVariableClass(name, "DepoisDe");

            if (!string.IsNullOrEmpty(generatedEvent))
            {
                return
                $"{LINE_PREFIX}{GenerateModuleSpecificGetter(ModuleName)}, {GetGeneratedEventVariableClass(name, "DepoisDe")}";
            }

            return string.Empty;
        }

        public string GenerateFullPreLine(string ModuleName)
        {
            string generatedEvent = GetGeneratedEventVariableClass(name, "AntesDe");

            if (!string.IsNullOrEmpty(generatedEvent))
            {
                return
                    $"{LINE_PREFIX}{GenerateModuleSpecificGetter(ModuleName)}, {GetGeneratedEventVariableClass(name, "AntesDe")}";
            }

            return string.Empty;
        }

        private string GenerateClassFriendlyName(string ModuleName)
        {
            return Regex.Replace(file, $"{ModuleName}(?<name>.*)?\\.cs", "${name}");
        }

        private string GenerateModuleSpecificGetter(string ModuleName)
        {
            return $"{ModuleName}.FuncoesComunsBS.ModuloActual";
        }

        private string GetGeneratedEventVariableClass(string eventName, string beginingString)
        {
            var regex =
                "\\s*public\\s+const\\s+string\\s+([a-zA-Z]+)\\s*\\=\\s*\\\"([a-zA-Z]+)\\\"\\;";

            var processedEventName = GetProcessedEventName(eventName, beginingString);
            var eventConstantsPath = "C:\\prjNET\\ERP10\\ERP\\Mainline\\Extensibility\\Core\\Extensibility.Constants\\ExtensibilityEvents.cs";

            foreach (var line in File.ReadAllLines(eventConstantsPath))
            {
                var match = Regex.Match(line, regex);

                if (match.Success)
                {
                    var varName = match.Groups[1].Value;
                    var varValue = match.Groups[2].Value;

                    if (processedEventName == varValue)
                    {
                        return "ExtensibilityEvents." + varName;
                        //return Path.GetFileNameWithoutExtension(file) + "." + varName;
                    }
                }

            }

            return string.Empty;

            #region Old_Not_Used
            //foreach (var file in Directory.EnumerateFiles(eventConstantsPath))
            //{
            //    foreach (var line in File.ReadAllLines(file))
            //    {
            //        var match = Regex.Match(line, regex);

            //        if (match.Success)
            //        {
            //            var varName = match.Groups[1].Value;
            //            var varValue = match.Groups[2].Value;

            //            if (processedEventName == varValue)
            //            {
            //                return Path.GetFileNameWithoutExtension(file) + "." + varName;
            //            }
            //        }

            //    }
            //}

            #endregion


            //throw new Exception("GetGeneratedEventVariableClass Error");
        }

        private string GetProcessedEventName(string eventName, string beginingString)
        {
            switch (eventName)
            {
                //case "Actualiza":
                //case "Atualiza": return $"{beginingString}Gravar";
                //case "ActualizaId":
                //case "AtualizaId": return $"{beginingString}GravarId";
                //case "ActualizaID":
                //case "AtualizaID": return $"{beginingString}GravarID";

                //case "Edita": return $"{beginingString}Editar";
                //case "EditaId": return $"{beginingString}EditarId";
                //case "EditaID": return $"{beginingString}EditarID";

                case "Remove": return $"{beginingString}Anular";
                case "RemoveId": return $"{beginingString}AnularId";
                case "RemoveID": return $"{beginingString}AnularID";


                default: return string.Empty;
            }
        }
    }
}