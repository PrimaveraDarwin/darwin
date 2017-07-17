using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primavera.Util.Refletor.Entities
{
    public class MethodParameter
    {
        public TypeReference ParameterType { get; set; }
        public string ParameterTypeName { get; set; }
        public string Name { get; set; }
    }
}
