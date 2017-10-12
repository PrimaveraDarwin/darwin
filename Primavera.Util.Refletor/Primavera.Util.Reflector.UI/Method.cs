using System.ComponentModel;
using Primavera.Util.Refletor.Entities;

namespace Primavera.Util.Reflector.UI
{
    public class Method
    {
        private const string LINE_PREFIX = "// MotorLE.Extensibilidade.";

        [Browsable(true)]
        public bool inject { get; set; }
        [Browsable(false)]
        public int id { get; }
        [Browsable(true)]
        public string name { get; }
        [Browsable(true)]
        public string file { get; }
        [Browsable(false)]
        public MethodEntity methodEntity { get; }
        [Browsable(true)]
        public string preLine { get; set; }
        [Browsable(true)]
        public string posLine { get; set; }

        public Method(int id, string name, string file, MethodEntity methodEntity, bool inject)
        {
            this.id = id;
            this.name = name;
            this.file = file;
            this.methodEntity = methodEntity;
            this.inject = inject;
        }

        public string GenerateDescription()
        {
            return this.name + "(" + this.file + ")";
        }

        public string GenerateFullPreLine()
        {
            //return LINE_PREFIX + this.preLine;
            if (this.name.Equals("Edita"))
            {
                return LINE_PREFIX + this.GenerateClassFriendlyName() + ".AntesDeEditar";
            }
            else
            {
                return LINE_PREFIX + this.GenerateClassFriendlyName() + ".AntesDeGravar";
            }
        }

        public string GenerateFullPosLine()
        {
            //return LINE_PREFIX + this.posLine;
            if (this.name.Equals("Edita"))
            {
                return LINE_PREFIX + this.GenerateClassFriendlyName() + ".DepoisDeEditar";
            }
            else
            {
                return LINE_PREFIX + this.GenerateClassFriendlyName() + ".DepoisDeGravar";
            }
        }

        private string GenerateClassFriendlyName()
        {
            return System.Text.RegularExpressions.Regex.Replace(file, "BasBS(?<name>.*)?\\.cs", "${name}");
        }
    }
}
