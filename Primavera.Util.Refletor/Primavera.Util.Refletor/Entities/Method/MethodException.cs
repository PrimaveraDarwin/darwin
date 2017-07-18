using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primavera.Util.Refletor.Entities
{
    public class MethodException
    {
        public Instruction TryEnd { get; set; }
        public Instruction TryStart { get; set; }
    }
}
