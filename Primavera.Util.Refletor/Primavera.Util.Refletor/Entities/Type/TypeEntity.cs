using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primavera.Util.Refletor.Entities
{
    public class TypeEntity
    {
        public string Name { get; set; }
        public string SourceLocation { get; set; }
        public bool IsPublic { get; set; }
        public bool IsSealed { get; set; }
        public bool IsAbstract { get; set; }
        public bool IsInterface { get; set; }
        public bool IsEnum { get; set; }
        public bool IsClass { get; set; }
        public bool HasGenericParameters { get; set; }
        public string GenericParameters { get; set; }

        public List<MethodEntity> Methods { get; set; }

        public TypeEntity()
        {
            this.Methods = new List<MethodEntity>();
        }

        public void SetTypeDeclaration(TypeDefinition type)
        {
            this.IsPublic = type.IsPublic;
            this.IsSealed = type.IsSealed;
            this.IsAbstract = type.IsAbstract;
            this.IsInterface = type.IsInterface;
            this.IsEnum = type.IsEnum;
            this.IsClass = type.IsClass;
            this.Name = type.Name;
            this.HasGenericParameters = type.HasGenericParameters;

            if (type.HasGenericParameters)
            {
                var parameters = type.GenericParameters.Select(t => t.Name).ToList();
                if (parameters.Count > 0)
                {
                    this.GenericParameters = type.Name.Replace("`1", "") + "<" + string.Join(",", parameters) + ">";
                }
            }
        }
    }
}
