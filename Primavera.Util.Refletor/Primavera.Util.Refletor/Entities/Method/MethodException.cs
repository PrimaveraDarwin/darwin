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
        public MethodExceptionTry TryEnd { get; set; }
        public MethodExceptionTry TryStart { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MethodException"/> class.
        /// </summary>
        public MethodException()
        {
            this.TryStart = new MethodExceptionTry();
            this.TryEnd = new MethodExceptionTry();
        }
    }
}
